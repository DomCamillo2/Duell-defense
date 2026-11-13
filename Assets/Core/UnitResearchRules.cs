using System;
using System.Numerics;

namespace DefenseDuell.Core
{
    public readonly struct UnitStats
    {
        public BigInteger MaxHp { get; }
        public int SpeedPerSecond { get; }
        public int Armor { get; }
        public int BaseDamage { get; }
        public int Count { get; }
        public int CooldownTicks { get; }
        public int KillBounty { get; }
        public UnitStats(BigInteger maxHp,int speedPerSecond,int armor,int baseDamage,int count,int cooldownTicks,int killBounty)
        {MaxHp=maxHp;SpeedPerSecond=speedPerSecond;Armor=armor;BaseDamage=baseDamage;Count=count;CooldownTicks=cooldownTicks;KillBounty=killBounty;}
    }
    public readonly struct ResearchQuote
    {
        public ErrorCode Code { get; }
        public bool IsValid=>Code==ErrorCode.None;
        public int Kind { get; }
        public BigInteger Price { get; }
        public BigInteger Level { get; }
        public BigInteger NextLevel { get; }
        public UnitStats Current { get; }
        public UnitStats Next { get; }
        // Legacy API compatibility: there is no maximum; callers must show Current/Next.
        public UnitStats Maximum=>Next;
        public ResearchQuote(ErrorCode code,int kind,BigInteger price,BigInteger level,BigInteger nextLevel,UnitStats current,UnitStats next)
        {Code=code;Kind=kind;Price=price;Level=level;NextLevel=nextLevel;Current=current;Next=next;}
    }
    public static class UnitResearchRules
    {
        public static UnitStats Resolve(GameContent content,int kind,BigInteger level,UnitOrigin origin,BigInteger waveId,int neutralRecipeIndex=-1)
        {
            if(kind<0||kind>=content.UnitData.Length||!content.ContainsWave(waveId)||level<0||neutralRecipeIndex< -1
                ||(origin!=UnitOrigin.NeutralWave&&origin!=UnitOrigin.PaidPvP)||(origin==UnitOrigin.NeutralWave&&level!=0))
                throw new ArgumentException("Invalid immutable unit recipe.");
            var u=content.UnitData[kind];var research=content.ResearchData[kind];
            if(origin==UnitOrigin.PaidPvP&&neutralRecipeIndex!=-1)throw new ArgumentException("Paid recipe cannot claim a neutral ordinal.");
            var recipe=origin==UnitOrigin.NeutralWave?content.GetWave(waveId):null;
            if(recipe!=null&&(kind>=GameContent.NeutralClassCount||neutralRecipeIndex>=0&&(neutralRecipeIndex>=recipe.TotalCount||recipe.PlanData[neutralRecipeIndex]!=kind)))
                throw new ArgumentException("Neutral kind/ordinal does not match its recipe.");
            BigInteger hp=origin==UnitOrigin.NeutralWave?(recipe.HasPerKindHp?recipe.HpForKind(kind):((BigInteger)u.Hp*recipe.HpPercent+99)/100):
                u.Hp+research.HpPerLevel*level+research.HpQuadraticNumerator*level*(level-1)/research.HpQuadraticDenominator;
            int speed=u.SpeedPerSecond;
            if(kind==0){speed+=(int)(research.SpeedPerLevel*BigInteger.Min(10,level));if(level>10)speed+=(int)(20*(level-10)/(level+10));}
            else if(kind==1){var n=BigInteger.Min(10,level);speed+=(int)(research.SpeedPerLevel*n+n*(n-1)/2);if(level>10)speed+=(int)(41*(level-10)/(level+10));}
            else if(kind==4)speed+=(int)(90*level/(level+15));
            return new UnitStats(hp,speed,u.Armor,u.BaseDamage,u.Count,u.CooldownTicks,origin==UnitOrigin.NeutralWave?0:u.KillBounty);
        }
        public static BigInteger Price(GameContent content,int kind,BigInteger level)
        {
            if(kind<0||kind>=content.UnitData.Length||level<0)throw new ArgumentOutOfRangeException(nameof(level));
            var definition=content.ResearchData[kind];
            // Keep the first ten basic research prices. Beyond that, avoid a cubic
            // cumulative cost while defensive upgrades have quadratic total cost.
            var curved=kind<GameContent.NeutralClassCount?BigInteger.Min(10,level):level;
            return definition.BasePrice+definition.PriceStep*level+definition.QuadraticPrice*curved*(curved-1)/2;
        }
        public static ResearchQuote Quote(GameContent content,PlayerState player,int kind)
        {
            if(kind<0||kind>=content.UnitData.Length)return new ResearchQuote(ErrorCode.InvalidKind,kind,0,0,0,default,default);
            if(player==null||player.ResearchLevels==null||player.ResearchLevels.Length!=content.UnitData.Length)return new ResearchQuote(ErrorCode.InvalidValue,kind,0,0,0,default,default);
            var level=player.ResearchLevels[kind];if(level<0)return new ResearchQuote(ErrorCode.InvalidValue,kind,0,level,level,default,default);
            return new ResearchQuote(ErrorCode.None,kind,Price(content,kind,level),level,level+1,
                Resolve(content,kind,level,UnitOrigin.PaidPvP,1),Resolve(content,kind,level+1,UnitOrigin.PaidPvP,1));
        }
    }
}
