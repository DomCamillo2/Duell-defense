using System;
namespace DefenseDuell.Presentation
{
    /// <summary>Audio-only throttle. Never gates, delays or queues gameplay commands.</summary>
    public sealed class Audio09CueGate
    {
        static readonly double[] Cooldowns={.055,.09,.11,.18,.13,.12,.50,.23,.60,.20};
        readonly double[] nextCue=new double[10];
        double lastTime,tokens;
        bool initialized;
        public void Reset(){initialized=false;Array.Clear(nextCue,0,nextCue.Length);}
        public bool TryAccept(GameAudioCue cue,double now)
        {
            int index=(int)cue;
            if(index<0 || index>=nextCue.Length || double.IsNaN(now) || double.IsInfinity(now) || now<0)return false;
            if(!initialized){initialized=true;lastTime=now;tokens=4;}
            if(now<lastTime)return false;
            tokens=Math.Min(4,tokens+(now-lastTime)*8);lastTime=now;
            if(now+1e-9<nextCue[index] || tokens<1)return false;
            tokens-=1;nextCue[index]=now+Cooldowns[index];return true;
        }
    }
}
