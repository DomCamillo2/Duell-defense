using System;

namespace DefenseDuell.Presentation
{
    // Pure managed policy. Visual limits never reject or delay gameplay commands.
    public struct ContactShadowSample
    {
        public long StableId;
        public int Priority;
        public float X,Y,Z,RadiusX,RadiusZ,Opacity,ViewportX,ViewportY;
        internal float DistanceSquared;
    }

    public sealed class ContactShadowSelection
    {
        public const int Columns=64,Rows=40,Capacity=256,PowerCapacity=64;
        readonly ContactShadowSample[] cells=new ContactShadowSample[Columns*Rows];
        readonly bool[] occupied=new bool[Columns*Rows];
        readonly int[] usedCells=new int[Columns*Rows];
        readonly ContactShadowSample[] heap=new ContactShadowSample[Capacity];
        int used,count,limit;
        public int Count=>count;
        public int Offered { get; private set; }
        public ContactShadowSample this[int index]=>index>=0&&index<count?heap[index]:throw new ArgumentOutOfRangeException(nameof(index));
        public void Begin(bool lowPower)
        {
            for(int i=0;i<used;i++)occupied[usedCells[i]]=false;
            used=count=Offered=0;limit=lowPower?PowerCapacity:Capacity;
        }
        static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
        static bool Better(ContactShadowSample a,ContactShadowSample b)
        {
            if(a.Priority!=b.Priority)return a.Priority>b.Priority;
            if(a.DistanceSquared!=b.DistanceSquared)return a.DistanceSquared<b.DistanceSquared;
            return a.StableId<b.StableId;
        }
        public bool Offer(ContactShadowSample value)
        {
            if(!Finite(value.X)||!Finite(value.Y)||!Finite(value.Z)||!Finite(value.RadiusX)||!Finite(value.RadiusZ)
                ||!Finite(value.Opacity)||!Finite(value.ViewportX)||!Finite(value.ViewportY)
                ||value.RadiusX<=0||value.RadiusZ<=0||value.Opacity<=0)return false;
            Offered++;
            value.RadiusX=Math.Min(value.RadiusX,1.2f);value.RadiusZ=Math.Min(value.RadiusZ,1.2f);value.Opacity=Math.Min(value.Opacity,.24f);
            float dx=value.ViewportX-.5f,dy=value.ViewportY-.5f;value.DistanceSquared=dx*dx+dy*dy;
            int x=Math.Max(0,Math.Min(Columns-1,(int)(value.ViewportX*Columns)));
            int y=Math.Max(0,Math.Min(Rows-1,(int)(value.ViewportY*Rows))),cell=y*Columns+x;
            if(!occupied[cell]){occupied[cell]=true;usedCells[used++]=cell;cells[cell]=value;}
            else if(Better(value,cells[cell]))cells[cell]=value;
            return true;
        }
        public void Select()
        {
            count=0;
            for(int i=0;i<used;i++)
            {
                var item=cells[usedCells[i]];
                if(count<limit)
                {
                    int slot=count++;heap[slot]=item;
                    while(slot>0)
                    {
                        int parent=(slot-1)/2;if(!Better(heap[parent],heap[slot]))break;
                        Swap(parent,slot);slot=parent;
                    }
                }
                else if(Better(item,heap[0]))
                {
                    heap[0]=item;int slot=0;
                    while(true)
                    {
                        int left=slot*2+1;if(left>=count)break;int right=left+1,worst=left;
                        if(right<count&&Better(heap[left],heap[right]))worst=right;
                        if(!Better(heap[slot],heap[worst]))break;Swap(slot,worst);slot=worst;
                    }
                }
            }
        }
        void Swap(int a,int b){var temp=heap[a];heap[a]=heap[b];heap[b]=temp;}
    }

    public enum ContactFxKind { Impact, Build, Upgrade, Explosion }
    public struct ContactFxEvent
    {
        public bool Active;
        public ContactFxKind Kind;
        public float X,Y,Z,R,G,B,Radius;
        public double Began;
        public float Duration;
    }
    public sealed class ContactFxEvents
    {
        public const int Capacity=24,PowerCapacity=8;
        readonly ContactFxEvent[] values=new ContactFxEvent[Capacity];
        double clock=-1,tokens=8;
        public ContactFxEvent this[int index]=>values[index];
        public int ActiveCount { get {int count=0;foreach(var value in values)if(value.Active)count++;return count;} }
        public void Clear(){Array.Clear(values,0,values.Length);clock=-1;tokens=8;}
        bool Advance(double now,bool reduced,bool power)
        {
            if(double.IsNaN(now)||double.IsInfinity(now)||now<0)return false;
            if(reduced){Clear();return false;}
            if(clock>=0&&now<clock){Clear();clock=now;return false;}
            if(clock>=0)tokens=Math.Min(8,tokens+(now-clock)*(power?10:24));clock=now;
            int limit=power?PowerCapacity:Capacity;
            for(int i=0;i<values.Length;i++)
                if(i>=limit||values[i].Active&&now-values[i].Began>=values[i].Duration)values[i].Active=false;
            return true;
        }
        public void Tick(double now,bool reduced,bool power)=>Advance(now,reduced,power);
        public bool Trigger(ContactFxKind kind,float x,float y,float z,float r,float g,float b,double now,bool reduced,bool power,float radius=0)
        {
            if(!Valid(radius)||(kind==ContactFxKind.Explosion&&radius<=0))return false;
            if((int)kind<0||(int)kind>3||!Valid(x)||!Valid(y)||!Valid(z)||!Valid(r)||!Valid(g)||!Valid(b))return false;
            if(!Advance(now,reduced,power)||tokens<1)return false;
            int limit=power?PowerCapacity:Capacity,slot=-1;
            for(int i=0;i<limit;i++)if(!values[i].Active){slot=i;break;}
            // Confirmed build/upgrade feedback can replace incidental impacts or explosions.
            if(slot<0&&kind!=ContactFxKind.Impact)
                for(int i=0;i<limit;i++)if((values[i].Kind==ContactFxKind.Impact||((kind==ContactFxKind.Build||kind==ContactFxKind.Upgrade)&&values[i].Kind==ContactFxKind.Explosion))
                    &&(slot<0||values[i].Began<values[slot].Began))slot=i;
            if(slot<0)return false;
            tokens-=1;
            values[slot]=new ContactFxEvent{Active=true,Kind=kind,X=x,Y=y,Z=z,R=Clamp(r),G=Clamp(g),B=Clamp(b),Radius=kind==ContactFxKind.Explosion?radius:0,Began=now,Duration=kind==ContactFxKind.Explosion?.38f:kind==ContactFxKind.Impact?.20f:kind==ContactFxKind.Build?.38f:.42f};return true;
        }
        static bool Valid(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        static float Clamp(float v)=>Math.Max(0,Math.Min(1,v));
    }
}
