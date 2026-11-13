using System;
using System.Numerics;
using DefenseDuell.Core;
#if UNITY_EDITOR
using NUnit.Framework;
#endif
namespace DefenseDuell.Tests
{
 public static class BulkUpgrade16Scenarios
 {
  static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
  public static void QuotesMatchSinglePrices()
  {
   var c=GameContent.CreateDefault();
   for(int k=0;k<8;k++)for(int l=0;l<24;l++)
   {
    var t=new TowerState{Kind=k,Level=l,DamageLevel=k==4?0:l};BigInteger sum=0;
    for(int n=1;n<=20;n++)
    {
     sum+=TowerUpgradeRules.Price(c,k,l+n);Check(TowerUpgradeBatchRules.Cost(c,k,l,n)==sum,"aggregate price");
     var q=TowerUpgradeBatchRules.Quote(c,t,TowerUpgradeRules.PrimaryPath(c,k),sum,UpgradeBatchMode.Max);
     Check(q.Count==n&&q.Price==sum&&q.IsValid,"exact max boundary");
     q=TowerUpgradeBatchRules.Quote(c,t,TowerUpgradeRules.PrimaryPath(c,k),sum-1,UpgradeBatchMode.Max);Check(q.Count==n-1,"one gold short");
    }
    var ten=TowerUpgradeBatchRules.Quote(c,t,TowerUpgradeRules.PrimaryPath(c,k),0,UpgradeBatchMode.Ten);
    Check(ten.Count==10&&ten.Price>0&&ten.Code==ErrorCode.InsufficientGold,"unaffordable10 preview");
   }
  }
  static CommandResult Execute(MatchSimulation s,CommandEnvelope c,PlayerSlot p=PlayerSlot.Zero)
  {c.SessionId=s.State.SessionId;if(c.Sequence==0)c.Sequence=s.LastSequence(p)+1;return s.Execute(p,c);}
  static MatchSimulation Playing(int kind,out TowerState tower)
  {
   var s=new MatchSimulation(GameContent.CreateDefault().WithMatchRules(new MatchRules(10,1000000)));
   Check(s.ConfigureTowerAvailability(PlayerSlot.Zero,1<<kind),"test selects class within five-class limit");
   foreach(PlayerSlot p in new[]{PlayerSlot.Zero,PlayerSlot.One})Check(Execute(s,new CommandEnvelope{Type=CommandType.SetReady,Target=-1,ExpectedConfigRevision=s.State.ConfigRevision,ExpectedRosterRevision=s.State.RosterRevision},p).Accepted,"ready");
   while(s.State.Phase==MatchPhase.Countdown)s.Step();
   tower=null;
   for(int y=30;y<s.Content.Map.Height&&tower==null;y+=30)for(int x=30;x<s.Content.Map.Width&&tower==null;x+=30)
   {var point=new MapPoint(x,y);if(PlacementValidator.ValidateBuild(s.Content,s.State,PlayerSlot.Zero,kind,-1,point,out _)!=ErrorCode.None)continue;Check(Execute(s,new CommandEnvelope{Type=CommandType.BuildTower,Kind=kind,Target=-1,PositionX=x,PositionY=y}).Accepted,"build");tower=s.State.Towers[0];}
   Check(tower!=null,"placement");return s;
  }
  public static void AtomicCommandsAndReplays()
  {
   for(int k=0;k<8;k++)
   {
    var s=Playing(k,out var t);var q=TowerUpgradeBatchRules.Quote(s.Content,t,TowerUpgradeRules.PrimaryPath(s.Content,k),s.State.Players[0].Gold,UpgradeBatchMode.Ten);
    var cmd=new CommandEnvelope{Type=CommandType.UpgradeTowerBatch,Target=t.Id,Kind=(int)TowerUpgradeRules.PrimaryPath(s.Content,k),UpgradeCount=q.Count,ExpectedTowerLevel=t.Level,UpgradePriceLimit=q.Price,SessionId=s.State.SessionId,Sequence=s.LastSequence(PlayerSlot.Zero)+1};
    var gold=s.State.Players[0].Gold;var invested=t.Invested;t.CooldownTicks=13;
    Check(s.Execute(PlayerSlot.Zero,cmd).Accepted,"batch accepted");Check(t.Level==10&&t.DamageLevel==(k==4?0:10)&&s.State.Players[0].Gold==gold-q.Price&&t.Invested==invested+q.Price&&t.CooldownTicks==13,"atomic mutation");
    Check(s.Execute(PlayerSlot.Zero,cmd).Accepted&&t.Level==10,"replay idempotent");cmd.UpgradeCount++;Check(s.Execute(PlayerSlot.Zero,cmd).Code==ErrorCode.Duplicate,"changed replay");
    cmd.Sequence++;cmd.UpgradeCount=1;Check(s.Execute(PlayerSlot.Zero,cmd).Code==ErrorCode.QuoteChanged&&t.Level==10,"stale level");
    cmd.Sequence++;cmd.ExpectedTowerLevel=10;cmd.UpgradePriceLimit=1;Check(s.Execute(PlayerSlot.Zero,cmd).Code==ErrorCode.QuoteChanged&&t.Level==10,"price ceiling");
    cmd.Sequence++;cmd.UpgradePriceLimit=1000000;s.State.Players[0].Gold=0;Check(s.Execute(PlayerSlot.Zero,cmd).Code==ErrorCode.InsufficientGold&&t.Level==10,"gold race no partial");
    cmd.Sequence++;cmd.UpgradeCount=0;Check(s.Execute(PlayerSlot.Zero,cmd).Code==ErrorCode.InvalidValue,"zero invalid");
    cmd.UpgradeCount=1;cmd.Sequence=s.LastSequence(PlayerSlot.One)+1;Check(s.Execute(PlayerSlot.One,cmd).Code==ErrorCode.NotOwner,"foreign tower");
   }
  }
 }
#if UNITY_EDITOR
 public sealed class BulkUpgrade16Tests
 {
  [Test] public void QuotesMatchSinglePrices()=>BulkUpgrade16Scenarios.QuotesMatchSinglePrices();
  [Test] public void AtomicCommandsAndReplays()=>BulkUpgrade16Scenarios.AtomicCommandsAndReplays();
 }
#endif
}
