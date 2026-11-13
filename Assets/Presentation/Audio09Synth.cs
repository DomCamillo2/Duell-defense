using System;

namespace DefenseDuell.Presentation
{
    public enum GameAudioCue { Ui, Back, Build, Upgrade, Sell, Send, Income, Error, Unlock, Research }
    public sealed class Audio09Pcm
    {
        public readonly string Name;
        public readonly int Rate, Channels;
        public readonly float[] Samples;
        public int Frames => Samples.Length / Channels;
        public double Duration => Frames / (double)Rate;
        public Audio09Pcm(string name,int rate,int channels,float[] samples)
        { Name=name;Rate=rate;Channels=channels;Samples=samples; }
    }
    /// <summary>Original deterministic PCM, no Unity, assets, network or realtime DSP dependency.</summary>
    public static class Audio09Synth
    {
        public const int SampleRate=32000;
        public const double MusicBpm=96,MusicSeconds=20;
        const double Tau=Math.PI*2;
        sealed class Canvas
        {
            public readonly float[] Data;
            public readonly int Rate,Channels,Frames;
            readonly bool wrap;
            public Canvas(double duration,int channels,int rate,bool loop=false)
            {Rate=rate;Channels=channels;Frames=(int)Math.Round(duration*rate);Data=new float[Frames*channels];wrap=loop;}
            public void Add(double at,double duration,double gain,double pan,Func<double,double> voice)
            {
                int start=(int)Math.Round(at*Rate),length=(int)Math.Round(duration*Rate);
                double left=Channels==1?1:Math.Sqrt((1-pan)*.5),right=Math.Sqrt((1+pan)*.5);
                for(int i=0;i<length;i++)
                {
                    int frame=start+i;if(wrap)frame=((frame%Frames)+Frames)%Frames;
                    else if(frame<0 || frame>=Frames)continue;
                    double t=i/(double)Rate,value=voice(t)*Envelope(t,duration)*gain;
                    Data[frame*Channels]+=(float)(value*left);
                    if(Channels==2)Data[frame*Channels+1]+=(float)(value*right);
                }
            }
            public void Finish(double targetPeak,bool zeroEdges)
            {
                for(int c=0;c<Channels;c++)
                {
                    double mean=0;for(int i=c;i<Data.Length;i+=Channels)mean+=Data[i];mean/=Frames;
                    for(int i=c;i<Data.Length;i+=Channels)Data[i]=(float)Math.Tanh((Data[i]-mean)*1.15);
                }
                double peak=0;for(int i=0;i<Data.Length;i++)peak=Math.Max(peak,Math.Abs(Data[i]));
                if(peak>0)for(int i=0;i<Data.Length;i++)Data[i]=(float)(Data[i]*targetPeak/peak);
                if(zeroEdges)for(int c=0;c<Channels;c++){Data[c]=0;Data[Data.Length-Channels+c]=0;}
            }
        }
        static double Envelope(double t,double duration)
        {
            if(t<=0 || t>=duration)return 0;
            double attack=Math.Min(.006,duration*.12),release=Math.Min(.045,duration*.22);
            double a=t<attack?.5-.5*Math.Cos(Math.PI*t/attack):1;
            double r=t>duration-release?.5-.5*Math.Cos(Math.PI*(duration-t)/release):1;
            return a*r;
        }
        static double Hz(int midi)=>440*Math.Pow(2,(midi-69)/12.0);
        static void Felt(Canvas c,double at,int note,double duration,double gain,double pan=0)
        {
            double f=Hz(note);
            c.Add(at,duration,gain,pan,t=>Math.Sin(Tau*f*t+.18*Math.Sin(Tau*f*2*t)*Math.Exp(-25*t))*Math.Exp(-5*t/duration)
                +.22*Math.Sin(Tau*f*2*t)*Math.Exp(-10*t/duration)+.07*Math.Sin(Tau*f*3.98*t)*Math.Exp(-35*t));
        }
        static void Glass(Canvas c,double at,int note,double duration,double gain,double pan=0)
        {
            double f=Hz(note);
            c.Add(at,duration,gain,pan,t=>Math.Sin(Tau*f*t)*Math.Exp(-5*t/duration)
                +.22*Math.Sin(Tau*f*2.76*t)*Math.Exp(-12*t/duration)+.08*Math.Sin(Tau*f*4.07*t)*Math.Exp(-28*t));
        }
        static void Wood(Canvas c,double at,double duration,double gain,double pitch=720,double pan=0)
        {c.Add(at,duration,gain,pan,t=>Math.Sin(Tau*pitch*t)*Math.Exp(-65*t)+.46*Math.Sin(Tau*pitch*1.48*t)*Math.Exp(-95*t)+.24*Math.Sin(Tau*190*t)*Math.Exp(-45*t));}
        static void Drum(Canvas c,double at,double duration,double gain,double pan=0)
        {c.Add(at,duration,gain,pan,t=>Math.Sin(Tau*(58*t+58*.022*(1-Math.Exp(-t/.022))))*Math.Exp(-28*t)+.12*Math.Sin(Tau*176*t)*Math.Exp(-55*t));}
        static void Brush(Canvas c,double at,double duration,double gain,uint seed,double pan=0)
        {
            uint random=seed==0?1:seed;double low=0,slow=0;
            c.Add(at,duration,gain,pan,t=>{random^=random<<13;random^=random>>17;random^=random<<5;
                double noise=(random/(double)uint.MaxValue)*2-1;low+=.30*(noise-low);slow+=.045*(noise-slow);
                return (low-slow)*Math.Exp(-7*t/duration);});
        }
        public static Audio09Pcm Effect(GameAudioCue cue)
        {
            if(cue<GameAudioCue.Ui || cue>GameAudioCue.Research)throw new ArgumentOutOfRangeException(nameof(cue));
            double[] durations={.085,.13,.38,.58,.32,.23,.30,.23,.88,.44};
            double[] peaks={.60,.52,.80,.78,.65,.64,.50,.55,.85,.72};
            var c=new Canvas(durations[(int)cue],1,SampleRate);
            switch(cue)
            {
                case GameAudioCue.Ui:Wood(c,0,.07,.30,680);Brush(c,0,.025,.08,17);break;
                case GameAudioCue.Back:Felt(c,0,62,.12,.20);Wood(c,0,.05,.11,420);break;
                case GameAudioCue.Build:
                    Drum(c,0,.15,.28);Wood(c,.015,.09,.18,510);Felt(c,.045,60,.24,.23);Felt(c,.11,64,.23,.16);Felt(c,.17,67,.20,.12);break;
                case GameAudioCue.Upgrade:
                    Wood(c,0,.06,.10,820);Felt(c,.025,64,.27,.21);Felt(c,.10,67,.32,.18);Glass(c,.18,72,.36,.13);Glass(c,.255,76,.30,.07);break;
                case GameAudioCue.Sell:Glass(c,0,79,.19,.12);Felt(c,.06,72,.20,.20);Wood(c,.14,.10,.16,610);break;
                case GameAudioCue.Send:Brush(c,0,.11,.28,73);Drum(c,.025,.16,.19);Wood(c,.075,.09,.16,960);break;
                case GameAudioCue.Income:Glass(c,0,76,.20,.12);Glass(c,.065,79,.22,.09);break;
                case GameAudioCue.Error:Wood(c,0,.10,.16,310);Felt(c,.065,50,.15,.12);break;
                case GameAudioCue.Unlock:
                    Felt(c,0,60,.44,.17);Felt(c,.07,67,.43,.16);Felt(c,.14,72,.44,.14);Glass(c,.23,76,.52,.09);Glass(c,.32,79,.52,.08);Drum(c,0,.13,.09);break;
                case GameAudioCue.Research:
                    Wood(c,0,.055,.12,560);Wood(c,.055,.055,.10,780);Wood(c,.11,.055,.08,1020);Felt(c,.16,69,.27,.14);Glass(c,.19,76,.23,.06);break;
            }
            c.Finish(peaks[(int)cue],true);return new Audio09Pcm("Defense Duell 09 "+cue,SampleRate,1,c.Data);
        }
        public static Audio09Pcm Music()
        {
            var c=new Canvas(MusicSeconds,2,SampleRate,true);double beat=60/MusicBpm,bar=beat*4;
            int[][] chords={new[]{48,52,55,62},new[]{45,48,52,55},new[]{41,45,48,52},new[]{43,47,50,55},
                new[]{48,52,55,62},new[]{40,43,47,50},new[]{41,45,48,52},new[]{43,48,50,55}};
            int[] bass={48,45,41,43,48,40,41,43};
            int[][] motif={new[]{76,79,74},new[]{72,76},new[]{72,69,76},new[]{74,71},new[]{76,79,81},new[]{79,76},new[]{77,76,72},new[]{74,72,67}};
            for(int b=0;b<8;b++)
            {
                double start=b*bar;
                for(int n=0;n<4;n++)Felt(c,start+.045*n,chords[b][n],1.9,.10-n*.011,(n-1.5)*.24);
                Felt(c,start,bass[b],.62,.19,-.06);Felt(c,start+beat*2.5,bass[b],.48,.105,.06);
                Drum(c,start,.22,.085);Drum(c,start+beat*2,.20,.055);
                for(int step=0;step<8;step++)Brush(c,start+step*beat*.5,.10,step%2==0?.023:.040,(uint)(200+b*17+step),step%2==0?-.33:.33);
                Wood(c,start+beat,.075,.027,930,-.23);Wood(c,start+beat*3,.075,.032,820,.23);
                for(int n=0;n<motif[b].Length;n++)Felt(c,start+beat*(.75+n*1.125),motif[b][n],.64,.048,n%2==0?.27:-.27);
            }
            // Wrapped pickup tail makes the loop start a continuation, not a global fade/reset.
            Felt(c,MusicSeconds-.22,62,.62,.034,.18);c.Finish(.45,false);
            return new Audio09Pcm("Defense Duell 09 Workshop Walk 96 BPM",SampleRate,2,c.Data);
        }
    }
}
