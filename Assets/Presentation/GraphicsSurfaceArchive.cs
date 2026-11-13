using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DefenseDuell.Presentation
{
    // Runtime data only: all ray sampling happens in the Editor before shipping.
    // This format is deliberately small and bounded, independent of gameplay/Wire.
    public sealed class GraphicsSurfaceArchive
    {
        const uint Magic=0x4F414444; // DDAO, little endian
        const int Version=1;
        public const int MaximumRecords=512,MaximumParts=3,MaximumPartVertices=500000,MaximumSamples=4000000;
        public sealed class Part
        {
            public string GeometrySha256;
            public float X,Y,Z;
            // 0 is neutral. 89/255 ~= .35 is the darkest allowed indirect-light loss.
            public byte[] Darkening;
        }
        public sealed class Record
        {
            public string Key;
            public Part[] Parts;
        }
        readonly Dictionary<string,Record> records;
        public int Count=>records.Count;
        GraphicsSurfaceArchive(Dictionary<string,Record> values){records=values;}
        public bool TryGet(string key,out Record record)=>records.TryGetValue(key,out record);
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static void Require(bool value,string message){if(!value)throw new InvalidDataException(message);}
        static bool ValidKey(string key)
        {
            if(string.IsNullOrEmpty(key)||key.Length>120)return false;
            foreach(char c in key)if(!(c>='a'&&c<='z'||c>='0'&&c<='9'||c=='/'||c=='_'||c=='-'))return false;
            return true;
        }
        static bool ValidHash(string hash)
        {
            if(hash==null||hash.Length!=64)return false;
            foreach(char c in hash)if(!(c>='a'&&c<='f'||c>='0'&&c<='9'))return false;
            return true;
        }
        static void Validate(Record record,ref int samples)
        {
            Require(record!=null&&ValidKey(record.Key),"Invalid AO group key");
            Require(record.Parts!=null&&record.Parts.Length>=1&&record.Parts.Length<=MaximumParts,"Invalid AO part count");
            foreach(var p in record.Parts)
            {
                Require(p!=null&&ValidHash(p.GeometrySha256),"Invalid AO geometry hash");
                Require(Finite(p.X)&&Finite(p.Y)&&Finite(p.Z),"Invalid AO offset");
                Require(p.Darkening!=null&&p.Darkening.Length>0&&p.Darkening.Length<=MaximumPartVertices,"Invalid AO sample count");
                samples=checked(samples+p.Darkening.Length);Require(samples<=MaximumSamples,"AO archive sample budget exceeded");
                foreach(byte value in p.Darkening)Require(value<=89,"AO darkness exceeds safe palette range");
            }
        }
        public static byte[] Write(IReadOnlyList<Record> source)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));
            Require(source.Count>0&&source.Count<=MaximumRecords,"Invalid AO record count");
            var ordered=new List<Record>(source.Count);var keys=new HashSet<string>(StringComparer.Ordinal);int samples=0;
            foreach(var r in source){Validate(r,ref samples);Require(keys.Add(r.Key),"Duplicate AO key");ordered.Add(r);}
            ordered.Sort((a,b)=>StringComparer.Ordinal.Compare(a.Key,b.Key));
            using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
            {
                writer.Write(Magic);writer.Write(Version);writer.Write(ordered.Count);
                foreach(var record in ordered)
                {
                    var key=Encoding.ASCII.GetBytes(record.Key);writer.Write((byte)key.Length);writer.Write(key);writer.Write((byte)record.Parts.Length);
                    foreach(var p in record.Parts)
                    {
                        writer.Write(Encoding.ASCII.GetBytes(p.GeometrySha256));writer.Write(p.X);writer.Write(p.Y);writer.Write(p.Z);
                        writer.Write(p.Darkening.Length);writer.Write(p.Darkening);
                    }
                }
                writer.Flush();return stream.ToArray();
            }
        }
        public static GraphicsSurfaceArchive Read(byte[] bytes)
        {
            if(bytes==null)throw new ArgumentNullException(nameof(bytes));
            Require(bytes.Length>=12&&bytes.Length<=MaximumSamples+MaximumRecords*MaximumParts*210,"AO archive byte budget exceeded");
            using(var stream=new MemoryStream(bytes,false))using(var reader=new BinaryReader(stream,Encoding.UTF8,true))
            {
                Require(reader.ReadUInt32()==Magic&&reader.ReadInt32()==Version,"Unknown AO archive format");
                int count=reader.ReadInt32();Require(count>0&&count<=MaximumRecords,"Invalid AO record count");
                var values=new Dictionary<string,Record>(count,StringComparer.Ordinal);int samples=0;
                for(int i=0;i<count;i++)
                {
                    int keyLength=reader.ReadByte();Require(keyLength>0&&keyLength<=120,"Invalid AO key length");
                    string key=Encoding.ASCII.GetString(ReadExact(reader,keyLength));
                    int partCount=reader.ReadByte();Require(partCount>0&&partCount<=MaximumParts,"Invalid AO part count");
                    var parts=new Part[partCount];
                    for(int p=0;p<partCount;p++)
                    {
                        string hash=Encoding.ASCII.GetString(ReadExact(reader,64));
                        float x=reader.ReadSingle(),y=reader.ReadSingle(),z=reader.ReadSingle();
                        int n=reader.ReadInt32();Require(n>0&&n<=MaximumPartVertices&&samples+n<=MaximumSamples,"Invalid AO sample length");
                        parts[p]=new Part{GeometrySha256=hash,X=x,Y=y,Z=z,Darkening=ReadExact(reader,n)};
                        // Validate increments the total for the complete record below.
                    }
                    var record=new Record{Key=key,Parts=parts};Validate(record,ref samples);
                    Require(!values.ContainsKey(key),"Duplicate AO key");values.Add(key,record);
                }
                Require(stream.Position==stream.Length,"Unexpected AO archive trailing bytes");
                return new GraphicsSurfaceArchive(values);
            }
        }
        static byte[] ReadExact(BinaryReader reader,int count)
        {var data=reader.ReadBytes(count);if(data.Length!=count)throw new EndOfStreamException("Truncated AO archive");return data;}
    }
}
