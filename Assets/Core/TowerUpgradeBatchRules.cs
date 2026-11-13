using System;
using System.Numerics;

namespace DefenseDuell.Core
{
    public enum UpgradeBatchMode { Single=0, Ten=1, Max=2 }

    public readonly struct UpgradeBatchQuote
    {
        public readonly ErrorCode Code;
        public bool IsValid=>Code==ErrorCode.None;
        public readonly BigInteger Count,Price,NextLevel,NextDamageLevel,NextRangeLevel;
        public readonly TowerStats Current,Next;
        public UpgradeBatchQuote(ErrorCode code,BigInteger count,BigInteger price,TowerStats current,TowerStats next,
            BigInteger nextLevel,BigInteger nextDamageLevel,BigInteger nextRangeLevel)
        {Code=code;Count=count;Price=price;Current=current;Next=next;NextLevel=nextLevel;NextDamageLevel=nextDamageLevel;NextRangeLevel=nextRangeLevel;}
    }

    /// <summary>Exact aggregate quotation; no per-level purchase loop and no state mutation.</summary>
    public static class TowerUpgradeBatchRules
    {
        public static BigInteger Cost(GameContent content,int kind,BigInteger level,BigInteger count)
        {
            if(level<0||count<0)throw new ArgumentOutOfRangeException();
            return TowerUpgradeRules.TotalInvestment(content,kind,level+count)-TowerUpgradeRules.TotalInvestment(content,kind,level);
        }
        public static UpgradeBatchQuote Quote(GameContent content,TowerState tower,TowerUpgradePath path,BigInteger gold,UpgradeBatchMode mode)
        {
            var error=TowerUpgradeRules.ValidateState(content,tower);
            if(error!=ErrorCode.None)return Invalid(error);
            if(!TowerUpgradeRules.IsAllowedPath(content,tower.Kind,path))return Invalid(ErrorCode.InvalidKind);
            if(gold<0||mode<UpgradeBatchMode.Single||mode>UpgradeBatchMode.Max)return Invalid(ErrorCode.InvalidValue);
            BigInteger count=mode==UpgradeBatchMode.Ten?10:1;
            if(mode==UpgradeBatchMode.Max)
            {
                BigInteger low=0,high=1;
                while(Cost(content,tower.Kind,tower.Level,high)<=gold){low=high;high*=2;}
                while(high-low>1)
                {
                    var mid=(high+low)/2;
                    if(Cost(content,tower.Kind,tower.Level,mid)<=gold)low=mid;else high=mid;
                }
                count=low;
            }
            bool economy=content.Towers[tower.Kind].IsEconomy;
            bool secondary=!economy&&path==TowerUpgradeRules.SecondaryPath(content,tower.Kind);
            var next=new TowerState{Kind=tower.Kind,Level=tower.Level+count,
                DamageLevel=tower.DamageLevel+(!economy&&!secondary?count:0),RangeLevel=tower.RangeLevel+(secondary?count:0)};
            var price=Cost(content,tower.Kind,tower.Level,count);
            return new UpgradeBatchQuote(count==0||price>gold?ErrorCode.InsufficientGold:ErrorCode.None,count,price,
                TowerUpgradeRules.Stats(content,tower),TowerUpgradeRules.Stats(content,next),next.Level,next.DamageLevel,next.RangeLevel);
        }
        static UpgradeBatchQuote Invalid(ErrorCode code)=>new UpgradeBatchQuote(code,0,0,default,default,0,0,0);
    }
}
