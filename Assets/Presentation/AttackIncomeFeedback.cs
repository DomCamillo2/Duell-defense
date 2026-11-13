using System;
using BigInteger=System.Numerics.BigInteger;

namespace DefenseDuell.Presentation
{
    /// <summary>Presentation-only high-water mark. Purchases never feed this tracker.</summary>
    public sealed class AttackIncomeFeedback
    {
        bool initialized;
        ulong session;
        int slot,latestPlayTick;
        BigInteger highWater;
        double startedAt,visibleUntil;
        public BigInteger VisibleIncreaseUnits { get; private set; }
        public int NoticeCount { get; private set; }
        public bool IsVisible(double now)=>VisibleIncreaseUnits>0&&now<visibleUntil;
        public void Clear(){initialized=false;VisibleIncreaseUnits=0;visibleUntil=0;}
        public void Reset(ulong sessionId,int playerSlot,BigInteger incomeUnits,int playTick)
        {
            initialized=true;session=sessionId;slot=playerSlot;highWater=BigInteger.Max(0,incomeUnits);latestPlayTick=playTick;
            VisibleIncreaseUnits=0;startedAt=visibleUntil=0;
        }
        /// <returns>True only at the start of a new aggregate, for one restrained audio cue.</returns>
        public bool Observe(ulong sessionId,int playerSlot,BigInteger incomeUnits,int playTick,bool playing,double now)
        {
            if(!initialized||session!=sessionId||slot!=playerSlot||!playing)
            {Reset(sessionId,playerSlot,incomeUnits,playTick);return false;}
            // A repeated or older snapshot must never lower the baseline and replay old credit.
            if(playTick<latestPlayTick)return false;
            latestPlayTick=playTick;
            if(incomeUnits<=highWater)return false;
            BigInteger increase=incomeUnits-highWater;highWater=incomeUnits;
            bool fresh=!IsVisible(now);
            if(fresh){VisibleIncreaseUnits=0;startedAt=now;NoticeCount++;}
            VisibleIncreaseUnits=checked(VisibleIncreaseUnits+increase);
            visibleUntil=Math.Min(startedAt+2,now+1.4);
            return fresh;
        }
    }
}
