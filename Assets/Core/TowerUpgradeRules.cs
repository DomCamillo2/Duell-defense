using System;
using System.Collections.Generic;
using System.Numerics;

namespace DefenseDuell.Core
{
    public enum TowerUpgradePath : byte
    {
        Damage=0, Range=1, Income=2, Firepower=3, Blast=4, FrostDuration=5,
        ArmorPiercing=6, ChainPower=7, PiercePower=8, BlastRadius=9, JumpRange=10
    }
    public readonly struct TowerStats
    {
        public BigInteger Damage { get; }
        public int Range { get; }
        public int FireIntervalTicks { get; }
        public BigInteger IncomePerSecond { get; }
        public BigInteger IncomeUnitsPerSecond { get; }
        public BigInteger SlowDurationTicks { get; }
        public int SlowPermille { get; }
        public int SplashRadius { get; }
        public int ArmorPiercing { get; }
        public int MaxTargets { get; }
        public int JumpRange { get; }
        public int LineHalfWidth { get; }
        public TowerStats(BigInteger damage,int range,int interval,BigInteger income,BigInteger incomeUnits=default,
            BigInteger slowDuration=default,int slowPermille=1000,int splashRadius=0,int armorPiercing=0,int maxTargets=1,int jumpRange=0,int lineHalfWidth=0)
        {
            Damage=damage;Range=range;FireIntervalTicks=interval;
            IncomeUnitsPerSecond=incomeUnits==0?income*960:incomeUnits;IncomePerSecond=IncomeUnitsPerSecond/960;
            SlowDurationTicks=slowDuration;SlowPermille=slowPermille;SplashRadius=splashRadius;ArmorPiercing=armorPiercing;
            MaxTargets=maxTargets;JumpRange=jumpRange;LineHalfWidth=lineHalfWidth;
        }
    }
    public readonly struct UpgradeQuote
    {
        public ErrorCode Code { get; }
        public bool IsValid => Code==ErrorCode.None;
        public TowerUpgradePath Path { get; }
        public BigInteger Price { get; }
        public TowerStats Current { get; }
        public TowerStats Next { get; }
        public BigInteger NextLevel { get; }
        public BigInteger NextDamageLevel { get; }
        public BigInteger NextRangeLevel { get; }
        public UpgradeQuote(ErrorCode code,TowerUpgradePath path,BigInteger price,TowerStats current,TowerStats next,
            BigInteger nextLevel,BigInteger nextDamageLevel,BigInteger nextRangeLevel)
        {Code=code;Path=path;Price=price;Current=current;Next=next;NextLevel=nextLevel;NextDamageLevel=nextDamageLevel;NextRangeLevel=nextRangeLevel;}
    }
    public readonly struct UpgradeStatPreview
    {
        public string Label { get; }
        public string Unit { get; }
        public BigInteger Current { get; }
        public BigInteger Next { get; }
        public int Divisor { get; }
        public UpgradeStatPreview(string label,string unit,BigInteger current,BigInteger next,int divisor=1)
        {Label=label;Unit=unit;Current=current;Next=next;Divisor=divisor;}
    }
    /// <summary>Exact unlimited progression. Physical geometry is bounded; the clearly labelled
    /// secondary continuation after ten steps also increases the class's primary effectiveness.</summary>
    public static class TowerUpgradeRules
    {
        // Compatibility metadata only. Zero means there is no purchase maximum.
        public const int MaxPurchases=0;
        static readonly int[] PriceBase={10,18,8,20,0,18,20,24},PriceStep={5,8,3,9,0,8,8,10};
        static readonly int[] Linear={1,3,4,8,96,2,5,2},Numerator={1,1,1,1,12,1,1,1},Denominator={4,2,1,1,1,2,2,4};
        static readonly string[] Names={"Feuerkraft","Sprengkraft","Frostdauer","Durchschlagskraft","Förderung","Kettenenergie","Bolzenkraft","Laserleistung"};
        public static TowerUpgradePath PrimaryPath(GameContent content,int kind)
        {
            CheckKind(content,kind);
            return kind==0?TowerUpgradePath.Firepower:kind==1?TowerUpgradePath.Blast:kind==2?TowerUpgradePath.FrostDuration:
                kind==3?TowerUpgradePath.ArmorPiercing:kind==4?TowerUpgradePath.Income:kind==5?TowerUpgradePath.ChainPower:kind==7?TowerUpgradePath.Damage:TowerUpgradePath.PiercePower;
        }
        public static TowerUpgradePath SecondaryPath(GameContent content,int kind)
        {CheckKind(content,kind);return kind==1?TowerUpgradePath.BlastRadius:kind==5?TowerUpgradePath.JumpRange:kind==4?TowerUpgradePath.Income:TowerUpgradePath.Range;}
        public static string PathLabel(GameContent content,TowerState tower,TowerUpgradePath path)
        {
            CheckKind(content,tower.Kind);
            if(path==PrimaryPath(content,tower.Kind)||path==TowerUpgradePath.Damage)return Names[tower.Kind];
            string label=path==TowerUpgradePath.BlastRadius?"Explosionsradius":path==TowerUpgradePath.JumpRange?"Sprungreichweite":"Reichweite";
            return tower.RangeLevel>=10?label+" + "+Names[tower.Kind]:label;
        }
        public static bool IsAllowedPath(GameContent content,int kind,TowerUpgradePath path)
        {
            CheckKind(content,kind);
            return path==PrimaryPath(content,kind)||(!content.TowerData[kind].IsEconomy&&path==SecondaryPath(content,kind))
                ||(path==TowerUpgradePath.Damage&&kind!=2&&!content.TowerData[kind].IsEconomy);
        }
        public static ErrorCode ValidateState(GameContent content,TowerState tower)
        {
            if(content==null)throw new ArgumentNullException(nameof(content));
            if(tower==null)return ErrorCode.InvalidTarget;
            if(tower.Kind<0||tower.Kind>=content.TowerData.Length)return ErrorCode.InvalidKind;
            return ValidLevels(tower,content.TowerData[tower.Kind].IsEconomy)?ErrorCode.None:ErrorCode.InvalidValue;
        }
        public static UpgradeQuote Quote(GameContent content,TowerState tower,TowerUpgradePath path)
        {
            var error=ValidateState(content,tower);
            if(error!=ErrorCode.None)return new UpgradeQuote(error,path,0,default,default,0,0,0);
            var current=Stats(content,tower);
            if(!IsAllowedPath(content,tower.Kind,path))return new UpgradeQuote(ErrorCode.InvalidKind,path,0,current,current,tower.Level,tower.DamageLevel,tower.RangeLevel);
            bool economy=content.TowerData[tower.Kind].IsEconomy;
            bool secondary=!economy&&path==SecondaryPath(content,tower.Kind);
            var level=tower.Level+1;var primary=tower.DamageLevel+(!economy&&!secondary?1:0);var range=tower.RangeLevel+(secondary?1:0);
            return new UpgradeQuote(ErrorCode.None,path,Price(content,tower.Kind,level),current,StatsAt(content,tower.Kind,level,primary,range),level,primary,range);
        }
        public static BigInteger Price(GameContent content,int kind,BigInteger targetLevel)
        {
            CheckKind(content,kind);if(targetLevel<1)throw new ArgumentOutOfRangeException(nameof(targetLevel));
            return kind==4?(2*targetLevel*targetLevel+27*targetLevel+70)/8:PriceBase[kind]+PriceStep[kind]*(targetLevel-1);
        }
        public static BigInteger TotalInvestment(GameContent content,int kind,BigInteger level)
        {
            CheckKind(content,kind);if(level<0)throw new ArgumentOutOfRangeException(nameof(level));
            BigInteger cumulative;
            if(kind!=4)cumulative=PriceBase[kind]*level+PriceStep[kind]*level*(level-1)/2;
            else
            {
                BigInteger polynomial=level*(level+1)*(2*level+1)/3+27*level*(level+1)/2+63*level;
                int correctionPeriod=0,partial=0,remainder=(int)(level%8);
                for(int i=1;i<=8;i++){int residue=(8-(2*i*i+27*i+63)%8)%8;correctionPeriod+=residue;if(i<=remainder)partial+=residue;}
                cumulative=(polynomial+(level/8)*correctionPeriod+partial)/8;
            }
            return content.TowerData[kind].Price+cumulative;
        }
        public static TowerStats Stats(GameContent content,TowerState tower)
        {
            var error=ValidateState(content,tower);if(error!=ErrorCode.None)throw new ArgumentException("Invalid tower upgrade state: "+error,nameof(tower));
            return StatsAt(content,tower.Kind,tower.Level,tower.DamageLevel,tower.RangeLevel);
        }
        public static int VisualStage(BigInteger level)=>level<0?0:level>=10?10:(int)level;
        public static int VisualBranch(TowerState tower)=>tower.RangeLevel==0?0:tower.DamageLevel==0?1:2;
        public static int VisualVariant(TowerState tower)
        {
            if(tower==null||tower.Kind<0||tower.Kind>7||!ValidLevels(tower,tower.Kind==4))throw new ArgumentException("Invalid tower upgrade state.",nameof(tower));
            int level=(int)BigInteger.Min(2,tower.Level);
            return tower.Kind==4?level:level*(level+1)/2+(int)BigInteger.Min(level,tower.RangeLevel);
        }
        static bool ValidLevels(TowerState tower,bool economy)=>tower.Level>=0&&tower.DamageLevel>=0&&tower.RangeLevel>=0
            &&(economy?tower.DamageLevel==0&&tower.RangeLevel==0:tower.Level==tower.DamageLevel+tower.RangeLevel);
        static void CheckKind(GameContent content,int kind)
        {if(content==null)throw new ArgumentNullException(nameof(content));if(kind<0||kind>=content.TowerData.Length)throw new ArgumentOutOfRangeException(nameof(kind));}
        static BigInteger Growth(int kind,BigInteger level)=>Linear[kind]*level+Numerator[kind]*level*(level-1)/Denominator[kind];
        static int Geometry(int baseline,BigInteger level)
        {
            if(baseline<=0)return 0;
            if(level<=10)return baseline+(int)(baseline*(8*level+level*(level-1)/4)/100);
            int ten=baseline+baseline*102/100,cap=(5*baseline+1)/2;
            return ten+(int)((cap-ten)*(level-10)/(level+10));
        }
        static TowerStats StatsAt(GameContent content,int kind,BigInteger level,BigInteger primary,BigInteger secondary)
        {
            var d=content.TowerData[kind];if(d.IsEconomy)return new TowerStats(0,0,0,0,d.IncomeUnitsPerSecond+Growth(kind,level));
            BigInteger effective=primary+BigInteger.Max(0,secondary-10);
            BigInteger tail=BigInteger.Max(0,secondary-10);
            BigInteger gain=Growth(kind,primary)+Math.Max(1,Linear[kind]/2)*tail+Numerator[kind]*tail*(tail-1)/Denominator[kind];
            BigInteger damage=kind==2?d.Damage:d.Damage+gain;
            // These three classes previously lost late damage-per-gold to base-tower
            // spam. Specialization starts after four effective primary purchases.
            var specialization=BigInteger.Max(0,effective-4);
            if(kind==1)damage+=specialization*(specialization+1)/2;
            else if(kind==3||kind==6)damage+=specialization*(specialization+1);
            BigInteger slow=kind==2?d.SlowDurationTicks+gain:d.SlowDurationTicks;
            int range=kind==1||kind==5?d.Range:Geometry(d.Range,secondary);
            int interval=d.FireIntervalTicks;
            int splash=kind==1?Geometry(d.SplashRadius,secondary):d.SplashRadius;
            int penetration=kind==3?d.ArmorPiercing+(int)BigInteger.Min(1000,effective*2):d.ArmorPiercing;
            int targets=d.MaxTargets;
            int jump=kind==5?Geometry(d.JumpRange,secondary):d.JumpRange;
            return new TowerStats(damage,range,interval,0,0,slow,d.SlowPermille,splash,penetration,targets,jump,d.LineHalfWidth);
        }
        public static UpgradeStatPreview[] Preview(GameContent content,TowerState tower,TowerUpgradePath path)
        {
            var q=Quote(content,tower,path);if(!q.IsValid)return Array.Empty<UpgradeStatPreview>();var a=q.Current;var b=q.Next;
            var list=new List<UpgradeStatPreview>();
            void Add(string label,string unit,BigInteger current,BigInteger next,int divisor=1){if(current!=next)list.Add(new UpgradeStatPreview(label,unit,current,next,divisor));}
            Add("Schaden","",a.Damage,b.Damage);Add("Reichweite","",a.Range,b.Range);
            Add("Schusspause","s",a.FireIntervalTicks,b.FireIntervalTicks,content.TickRate);
            Add("Förderung","G/s",a.IncomeUnitsPerSecond,b.IncomeUnitsPerSecond,content.WaveRules.IncomeUnitsPerGold);
            Add("Frostdauer","s",a.SlowDurationTicks,b.SlowDurationTicks,content.TickRate);
            Add("Explosionsradius","",a.SplashRadius,b.SplashRadius);Add("Rüstungsdurchdringung","",a.ArmorPiercing,b.ArmorPiercing);
            Add("Trefferziele","",a.MaxTargets,b.MaxTargets);Add("Sprungreichweite","",a.JumpRange,b.JumpRange);
            return list.ToArray();
        }
    }
}
