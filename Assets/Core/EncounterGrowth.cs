using System;
using System.Numerics;

namespace DefenseDuell.Core
{
    public enum EncounterRank { Normal, Boss, SuperBoss }
    public sealed class GrownEncounter
    {
        readonly int[] counts;
        readonly BigInteger[] hp;
        public int[] Counts => (int[])counts.Clone();
        public BigInteger[] HpPerKind => (BigInteger[])hp.Clone();
        public EncounterRank Rank { get; }
        public BigInteger TotalHp { get; }
        public int TotalCount { get; }
        public int AddedCount { get; }
        public BigInteger Wave { get; }
        internal GrownEncounter(BigInteger wave,int[] counts,BigInteger[] hp,int original)
        {
            Wave=wave;this.counts=(int[])counts.Clone();this.hp=(BigInteger[])hp.Clone();
            Rank=wave%100==0?EncounterRank.SuperBoss:wave%10==0?EncounterRank.Boss:EncounterRank.Normal;
            for(int k=0;k<4;k++){TotalCount+=counts[k];TotalHp+=counts[k]*hp[k];}
            AddedCount=TotalCount-original;
        }
    }
    // Pure postprocessor of an already resolved neutral recipe; never apply to paid attacks.
    public static class EncounterGrowth
    {
        public const string Version="neutral-chapter-growth-2-cap64-super100-v1";
        public const int BodyBudget=64;
        public static GrownEncounter Apply(BigInteger wave,int[] counts,BigInteger[] hp)
        {
            if(wave<1||counts==null||hp==null||counts.Length!=4||hp.Length!=4)throw new ArgumentException("Four neutral classes and positive wave required");
            int original=0;bool boss=wave%10==0;
            for(int k=0;k<4;k++)
            {
                if(counts[k]<0||counts[k]>BodyBudget||hp[k]<0||counts[k]>0&&hp[k]<1)throw new ArgumentException("Invalid recipe");
                original+=counts[k];
            }
            if(original<1||original>BodyBudget||boss&&counts[2]!=1)throw new ArgumentException("Boss requires exactly one tank slot");
            var grown=(int[])counts.Clone();
            // Clamp BEFORE narrowing BigInteger. Cost and memory do not grow with wave index.
            int extra=(int)BigInteger.Min(BodyBudget-original,2*(BigInteger.Max(0,wave-10)/10));
            int weight=original-(boss?1:0);
            if(extra>0&&weight==0)throw new ArgumentException("A growing boss recipe needs escorts");
            // House-monotone highest averages allocation. Unlike largest remainders, increasing
            // the chapter budget can never remove a member from one of the previous class groups.
            var added=new int[4];
            for(int n=0;n<extra;n++)
            {
                int best=-1;
                for(int k=0;k<4;k++)
                {
                    if(counts[k]==0||boss&&k==2)continue;
                    if(best<0||(long)counts[k]*(added[best]+1)>(long)counts[best]*(added[k]+1))best=k;
                }
                if(best<0)throw new ArgumentException("No eligible class");
                grown[best]++;added[best]++;
            }
            // Per-body HP is retained exactly. More bodies increase aggregate pressure rather
            // than sharing and diluting the old fixed TotalHP budget. Caller hashes this policy.
            return new GrownEncounter(wave,grown,hp,original);
        }
    }
}
