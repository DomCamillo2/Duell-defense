using System;
using System.Globalization;
using System.Numerics;
namespace DefenseDuell.Core
{
    public enum MatchFormat : byte { Duel, TwoVsTwo, ThreeVsThree, TwoVsTwoVsTwo }
    public sealed class MatchRules : IEquatable<MatchRules>
    {
        public int WaveCount { get; }
        public BigInteger StartGold { get; }
        public string StartGoldDecimal => StartGold.ToString(CultureInfo.InvariantCulture);
        public bool Endless { get; }
        public int IncomeMultiplier { get; }
        public MatchFormat Format { get; }
        public string MapId { get; }
        public int TeamCount => Format==MatchFormat.TwoVsTwoVsTwo?3:2;
        public int MembersPerTeam => Format==MatchFormat.Duel?1:Format==MatchFormat.ThreeVsThree?3:2;
        public int PlayerCount => TeamCount*MembersPerTeam;
        public MatchRules(int waveCount,BigInteger startGold,bool endless=false,int incomeMultiplier=1,MatchFormat format=MatchFormat.Duel,string mapId=null)
        {WaveCount=waveCount;StartGold=startGold;Endless=endless;IncomeMultiplier=incomeMultiplier;Format=format;MapId=mapId??"";}
        public MatchRules WithMap(string mapId)=>new MatchRules(WaveCount,StartGold,Endless,IncomeMultiplier,Format,mapId);
        public bool Equals(MatchRules other)=>other!=null&&WaveCount==other.WaveCount&&StartGold==other.StartGold&&Endless==other.Endless&&IncomeMultiplier==other.IncomeMultiplier&&Format==other.Format&&MapId==other.MapId;
        public override bool Equals(object other)=>Equals(other as MatchRules);
        public override int GetHashCode()=>unchecked(WaveCount*397^StartGold.GetHashCode()^(Endless?7919:0)^IncomeMultiplier*17^(int)Format*23^MapId.GetHashCode());
    }
    public static class MatchRulesPolicy
    {
        public const int MinWaveCount=1,MaxWaveCount=200;
        public const int MaxRulesPayloadBytes=65536,MaxStartGoldMagnitudeBytes=65523;
        public const string WavePlanVersion="authored-then-counter-profiles-boss10-v4";
        public static bool ValidIncomeMultiplier(int n)=>n==1||n==2||n==3||n==5||n==10;
        public static ErrorCode Validate(GameContent baseContent,MatchRules rules)
        {
            if(baseContent==null||rules==null)return ErrorCode.InvalidValue;
            if(rules.WaveCount<1||rules.WaveCount>200)return ErrorCode.UnsupportedWaveCount;
            if((byte)rules.Format>3||!ValidIncomeMultiplier(rules.IncomeMultiplier)||rules.StartGold.Sign<0)return ErrorCode.InvalidValue;
            if(rules.StartGold.GetByteCount(isUnsigned:true)>MaxStartGoldMagnitudeBytes)return ErrorCode.RulesPayloadTooLarge;
            if(rules.MapId.Length>64)return ErrorCode.InvalidValue;
            if(rules.MapId.Length!=0&&rules.MapId!=baseContent.BaseContent.Map.Id)
            {bool found=false;for(int i=0;i<MapCatalog.Count;i++)if(MapCatalog.Create(i).Id==rules.MapId){found=true;break;}if(!found)return ErrorCode.InvalidValue;}
            var authored=baseContent.BaseContent.WaveData;
            if(authored.Length<1||authored[authored.Length-1]==null)return ErrorCode.InvalidValue;
            if(!rules.Endless&&(long)authored[authored.Length-1].HpPercent+(long)WaveDefinition.ContinuationHpStep*Math.Max(0,rules.WaveCount-authored.Length)>int.MaxValue)return ErrorCode.InvalidValue;
            return ErrorCode.None;
        }
        public static ErrorCode TryParseStartGold(string text,out BigInteger value)
        {
            value=BigInteger.Zero;if(string.IsNullOrEmpty(text))return ErrorCode.InvalidValue;
            if(text.Length>MaxStartGoldMagnitudeBytes*3)return ErrorCode.RulesPayloadTooLarge;
            if(text.Length>1&&text[0]=='0')return ErrorCode.InvalidValue;
            foreach(char c in text)if(c<'0'||c>'9')return ErrorCode.InvalidValue;
            if(!BigInteger.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out value))return ErrorCode.InvalidValue;
            if(value.GetByteCount(isUnsigned:true)<=MaxStartGoldMagnitudeBytes)return ErrorCode.None;
            value=BigInteger.Zero;return ErrorCode.RulesPayloadTooLarge;
        }
    }
}
