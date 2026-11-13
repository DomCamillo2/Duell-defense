using System;

namespace DefenseDuell.Presentation
{
    /// <summary>A delayed presentation clock, never a second simulation. At 5-Hz delivery
    /// it plays between complete authoritative samples instead of easing to a stop each packet.</summary>
    public sealed class SnapshotPlaybackClock
    {
        bool initialized;
        int lastTick;
        double latest,arrival,lastFrame,cursor,interval=.2;
        public double Time=>cursor;
        public double Delay=>Math.Min(.85,Math.Max(.25,interval*1.25));
        public void Reset(){initialized=false;lastTick=0;latest=arrival=lastFrame=cursor=0;interval=.2;}
        public bool Observe(int tick,int tickRate,double now)
        {
            if(tick<0 || tickRate<=0 || !Finite(now))return false;
            double control=tick/(double)tickRate;
            bool restart=!initialized || tick<lastTick || now-arrival>1.2;
            if(!restart && tick==lastTick)return false;
            if(restart)
            {initialized=true;interval=.2;cursor=control-.25;lastFrame=now;}
            else
            {
                double spacing=control-latest;
                if(spacing>0 && spacing<2)interval=interval*.8+spacing*.2;
            }
            lastTick=tick;latest=control;arrival=now;return restart;
        }
        public double Advance(double now)
        {
            if(!initialized || !Finite(now))return cursor;
            double elapsed=Math.Max(0,now-lastFrame);lastFrame=now;
            if(elapsed>.5){cursor=Math.Max(cursor,latest-Delay);elapsed=0;}
            double target=latest+Math.Min(Math.Max(0,now-arrival),Delay)-Delay;
            // Small clock corrections are spread over frames, never applied as position jumps.
            double correction=Math.Max(-.12,Math.Min(.12,(target-cursor)*.6));
            cursor=Math.Min(latest,cursor+elapsed*(1+correction));return cursor;
        }
        static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
    }

    /// <summary>Fixed-size per-visible-entity sample history. Interpolates path distance so
    /// units follow corners exactly; packet loss holds the last known position.</summary>
    public sealed class RouteMotionSamples
    {
        const int Capacity=16;
        readonly double[] times=new double[Capacity];
        readonly int[] distances=new int[Capacity];
        int first,count;
        public int Count=>count;
        public void Reset(){first=count=0;}
        public void Add(double time,int distance)
        {
            if(double.IsNaN(time)||double.IsInfinity(time)||distance<0)return;
            if(count>0)
            {
                int last=(first+count-1)%Capacity;
                if(time<times[last])return;
                if(time==times[last]){distances[last]=distance;return;}
                if(distance<distances[last])Reset(); // explicit teleport/reset, never animate backwards
            }
            if(count==Capacity){first=(first+1)%Capacity;count--;}
            int index=(first+count++)%Capacity;times[index]=time;distances[index]=distance;
        }
        public double Sample(double time)
        {
            if(count==0)return 0;
            if(time<=times[first])return distances[first];
            for(int i=1;i<count;i++)
            {
                int right=(first+i)%Capacity,left=(first+i-1)%Capacity;
                if(time>times[right])continue;
                double t=(time-times[left])/(times[right]-times[left]);
                return distances[left]+(distances[right]-distances[left])*Math.Max(0,Math.Min(1,t));
            }
            return distances[(first+count-1)%Capacity];
        }
    }
}
