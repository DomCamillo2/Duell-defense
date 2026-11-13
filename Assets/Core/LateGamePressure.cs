using System;
using System.Linq;
using System.Numerics;
using DefenseDuell.Core;

namespace DefenseDuell.Core
{
    public sealed class PressureRecipe
    {
        public BigInteger Wave { get; }
        public bool Boss { get; }
        public bool Century { get; }
        public BigInteger PressurePercent { get; }
        public BigInteger BudgetHp { get; }
        public int[] Counts => (int[])counts.Clone();
        public BigInteger[] HpPerKind => (BigInteger[])hp.Clone();
        readonly int[] counts;
        readonly BigInteger[] hp;
        internal PressureRecipe(BigInteger wave,bool boss,BigInteger pressure,BigInteger budget,int[] counts,BigInteger[] hp)
        {Wave=wave;Boss=boss;Century=boss&&wave%100==0;PressurePercent=pressure;BudgetHp=budget;this.counts=(int[])counts.Clone();this.hp=(BigInteger[])hp.Clone();}
    }
    // Canonical normalized neutral pressure; paid recipes never receive these modifiers.
    internal static class BossPressure
    {
        public const string Version="boss10-pressure-calibration-v1";
        public static BigInteger Ceil(BigInteger n,BigInteger d)=>(n+d-1)/d;
        public static BigInteger Trend(BigInteger wave)
        {
            if(wave<1)throw new ArgumentOutOfRangeException(nameof(wave));
            var late=BigInteger.Max(0,wave-10);
            return 100+12*(wave-1)+Ceil(2*late*late,5);
        }
        public static PressureRecipe At(GameContent content,BigInteger wave,int[] profileCounts=null,int profilePermille=1000)
        {
            if(content==null||wave<1||profilePermille<750||profilePermille>1200)throw new ArgumentException("Invalid wave/profile");
            bool boss=wave%10==0;
            var units=content.Units;
            int[] counts=profileCounts==null?new[]{8,14,10,16}:(int[])profileCounts.Clone();
            if(boss)counts=new[]{4,4,1,8};
            if(counts.Length!=4||counts.Any(n=>n<0)||counts.Sum()<1||counts.Sum()>64)throw new ArgumentException("Bounded four-class recipe required");
            BigInteger baseline=8*units[0].Hp+14*units[1].Hp+10*units[2].Hp+16*units[3].Hp;
            BigInteger trend=Trend(wave);
            // Boss chapter10:1.25x,20:1.40x,50:1.85x,100:2.60x normal-wave raw-HP budget.
            // Bounded extra boss premium after chapter100; base trend itself keeps growing forever.
            BigInteger premium=boss?1250+15*BigInteger.Min(90,wave-10):profilePermille;
            BigInteger budget=Ceil(baseline*trend*premium,100000);
            if(boss&&wave%100==0)budget=Ceil(budget*3,2);
            var hp=new BigInteger[4];
            if(boss)
            {
                hp[2]=Ceil(budget*45,100);
                BigInteger support=budget-hp[2],weight=0;
                for(int k=0;k<4;k++)if(k!=2)weight+=(BigInteger)counts[k]*units[k].Hp;
                for(int k=0;k<4;k++)if(k!=2)hp[k]=Ceil(support*units[k].Hp,weight);
            }
            else
            {
                BigInteger weight=0;for(int k=0;k<4;k++)weight+=(BigInteger)counts[k]*units[k].Hp;
                for(int k=0;k<4;k++)if(counts[k]>0)hp[k]=Ceil(budget*units[k].Hp,weight);
            }
            return new PressureRecipe(wave,boss,trend,budget,counts,hp);
        }
    }
}
