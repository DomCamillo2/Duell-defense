using System;
using System.IO;
using System.Text;

namespace DefenseDuell.Presentation
{
    /// <summary>Original preview export. No Unity or platform audio APIs required.</summary>
    public static class Audio09WaveFile
    {
        public static void Write(string path,Audio09Pcm pcm)
        {
            if(pcm==null || pcm.Channels<1 || pcm.Channels>2 || pcm.Rate<=0 || pcm.Samples.Length%pcm.Channels!=0)
                throw new ArgumentException("Invalid PCM format.");
            foreach(float sample in pcm.Samples)
                if(float.IsNaN(sample)||float.IsInfinity(sample)||Math.Abs(sample)>1)throw new ArgumentException("Invalid PCM amplitude.");
            string folder=Path.GetDirectoryName(path);if(!string.IsNullOrEmpty(folder))Directory.CreateDirectory(folder);
            using(var stream=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.None))
            using(var w=new BinaryWriter(stream,Encoding.ASCII))
            {
                int bytes=checked(pcm.Samples.Length*2);
                w.Write(Encoding.ASCII.GetBytes("RIFF"));w.Write(36+bytes);w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16);w.Write((short)1);w.Write((short)pcm.Channels);w.Write(pcm.Rate);
                w.Write(pcm.Rate*pcm.Channels*2);w.Write((short)(pcm.Channels*2));w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data"));w.Write(bytes);
                foreach(float sample in pcm.Samples)w.Write((short)Math.Round(sample*32767));
            }
        }
    }
}
