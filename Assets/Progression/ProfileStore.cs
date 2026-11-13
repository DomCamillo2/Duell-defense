using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DefenseDuell.Progression
{
    /// <summary>Atomic local saves with a validated previous revision. Serialization is injected for Unity and harness use.</summary>
    public class ProfileStore
    {
        readonly string path;
        readonly Func<PlayerProfile,string> encode;
        readonly Func<string,PlayerProfile> decode;
        public ProfileStore(string path, Func<PlayerProfile,string> encode, Func<string,PlayerProfile> decode)
        { this.path=path; this.encode=encode; this.decode=decode; }
        public PlayerProfile Load(out bool recovered)
        {
            recovered=false;
            if (!File.Exists(path) && !File.Exists(path+".bak")) return new PlayerProfile();
            if (TryRead(path,out var profile)) return profile;
            if (TryRead(path+".bak",out profile)) { recovered=true; return profile; }
            throw new InvalidDataException("Profil und Sicherung sind nicht lesbar. Die Dateien bleiben erhalten.");
        }
        bool TryRead(string file,out PlayerProfile profile)
        {
            profile=null;
            try
            {
                if (!File.Exists(file) || new FileInfo(file).Length > 16384) return false;
                string text=File.ReadAllText(file); int line=text.IndexOf('\n');
                if (line!=64 || Hash(text.Substring(line+1))!=text.Substring(0,line)) return false;
                profile=decode(text.Substring(line+1)); ProfileRules.Validate(profile); return true;
            }
            catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException || e is FormatException) { return false; }
        }
        public virtual void Save(PlayerProfile profile)
        {
            ProfileRules.Validate(profile);
            string payload=encode(profile), text=Hash(payload)+"\n"+payload;
            string folder=Path.GetDirectoryName(path); if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            string temporary=path+".tmp";
            using (var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))
            {
                byte[] data=Encoding.UTF8.GetBytes(text); stream.Write(data,0,data.Length); stream.Flush(true);
            }
            if (File.Exists(path))
            {
                // Never replace a healthy backup with a known corrupt primary after recovery.
                if (TryRead(path,out _)) File.Replace(temporary,path,path+".bak");
                else File.Replace(temporary,path,null);
            }
            else File.Move(temporary,path);
        }
        static string Hash(string text)
        {
            using (var sha=SHA256.Create())
            {
                var b=new StringBuilder(64);
                foreach(byte value in sha.ComputeHash(Encoding.UTF8.GetBytes(text))) b.Append(value.ToString("x2"));
                return b.ToString();
            }
        }
    }
}
