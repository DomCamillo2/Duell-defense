using System;
using System.Numerics;
using DefenseDuell.Core;
#if UNITY_EDITOR
using NUnit.Framework;
#endif
namespace DefenseDuell.Tests
{
 public static class Encounter16Scenarios
 {
  static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
  public static void GrowthKeepsBodiesAndExactBossIdentity()
  {
   var c=GameContent.CreateDefault().WithMatchRules(new MatchRules(10,180,true));
   foreach(var wave in new BigInteger[]{10,20,100,200,BigInteger.Pow(10,80)})
   {
    var r=c.GetWave(wave);Check(r.IsBossWave&&r.IsSuperBossWave==(wave%100==0),"boss rank");
    Check(r.Counts[2]==1&&r.MatchesNeutral(r.BossUnitIndex,2),"unique boss ordinal");
    int expected=(int)BigInteger.Min(64,17+2*(BigInteger.Max(0,wave-10)/10));Check(r.TotalCount==expected,"chapter body budget");
    BigInteger total=0;for(int k=0;k<4;k++)total+=r.Counts[k]*r.HpForKind(k);
    Check(total==r.PressureBudgetHp,"aggregate pressure must include added bodies");
    var boss=new UnitState{Kind=2,Origin=UnitOrigin.NeutralWave,WaveId=wave,NeutralRecipeIndex=r.BossUnitIndex};
    Check(LateGameRules.IsBoss(c,boss),"neutral boss identity");boss.Origin=UnitOrigin.PaidPvP;boss.NeutralRecipeIndex=-1;
    Check(!LateGameRules.IsBoss(c,boss),"paid tank became boss");
   }
   var h=new BigInteger[]{100,50,400,80};var grown=EncounterGrowth.Apply(100,new[]{4,4,1,8},h);
   Check(string.Join(",",grown.HpPerKind)==string.Join(",",h),"growth diluted body HP");
   Check(grown.TotalCount==35&&grown.Counts[2]==1,"century escort growth");
   for(int w=1;w<=1000;w++)c.GetWave(w);Check(c.CachedWaveRecipeCount<=3,"recipe history leaked");
  }
  public static void EliteResearchAndLoadoutsStayBounded()
  {
   var c=GameContent.CreateDefault();Check(c.Units.Length==6&&c.Research.Length==6,"six paid classes");
   Check(c.Units[4].Price==1200&&c.Units[5].Price==1600,"elite prices");
   for(int mask=0;mask<256;mask++)Check(TowerLoadoutRules.IsValid(mask,8)==(mask>0&&TowerLoadoutRules.Count(mask)<=5),"five-class availability");
   foreach(int k in new[]{4,5})
   {
    var a=UnitResearchRules.Resolve(c,k,0,UnitOrigin.PaidPvP,1);
    var b=UnitResearchRules.Resolve(c,k,BigInteger.Pow(10,80),UnitOrigin.PaidPvP,1);
    Check(b.MaxHp>a.MaxHp,"elite research stopped");
    if(k==4)Check(b.SpeedPerSecond<=305&&b.SpeedPerSecond>=a.SpeedPerSecond,"unbounded elite speed");
   }
   Check(c.Towers[7].FireIntervalTicks==8&&c.Rules.SpawnIntervalTicks==2,"requested cadence");
  }
 }
#if UNITY_EDITOR
 public sealed class Encounter16Tests
 {
  [Test] public void GrowthKeepsBodiesAndExactBossIdentity()=>Encounter16Scenarios.GrowthKeepsBodiesAndExactBossIdentity();
  [Test] public void EliteResearchAndLoadoutsStayBounded()=>Encounter16Scenarios.EliteResearchAndLoadoutsStayBounded();
 }
#endif
}
