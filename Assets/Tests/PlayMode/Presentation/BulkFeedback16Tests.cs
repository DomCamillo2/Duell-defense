using System.Collections.Generic;
using System.Numerics;
using DefenseDuell.Core;
using NUnit.Framework;
namespace DefenseDuell.Presentation.Tests
{
 public sealed class BulkFeedback16Tests
 {
  [TestCase(false)][TestCase(true)]
  public void BatchWaitsForBothAckAndExactPathState(bool ackFirst)
  {
   var state=new MatchState{SessionId=16,Tick=10,Phase=MatchPhase.Playing};
   var tower=new TowerState{Id=8,Kind=0,Owner=PlayerSlot.Zero,Level=2,DamageLevel=2};state.Towers.Add(tower);
   var tracker=new PresentationCommandTracker();tracker.Reset(16);
   var context=PresentationCommandContext.Capture(state,PlayerSlot.Zero,CommandType.UpgradeTowerBatch,(int)TowerUpgradePath.Range,8,1,null,10);
   tracker.Register(1,context);Assert.That(context.ExpectedLevel,Is.EqualTo(new BigInteger(12)));Assert.That(context.ExpectedDamageLevel,Is.EqualTo(new BigInteger(2)));Assert.That(context.ExpectedRangeLevel,Is.EqualTo(new BigInteger(10)));
   var ready=new List<PresentationCommandContext>();var ack=new CommandResult{Sequence=1,Tick=10};
   if(ackFirst)tracker.Resolve(ack,out _);else{tower.Level=12;tower.RangeLevel=10;}
   tracker.Observe(state,1.1,ready);Assert.That(ready,Is.Empty);Assert.That(tracker.HasPendingUpgrade(8),Is.True);
   if(ackFirst){tower.Level=12;tower.RangeLevel=10;}else tracker.Resolve(ack,out _);
   tracker.Observe(state,1.2,ready);Assert.That(ready.Count,Is.EqualTo(1));Assert.That(tracker.HasPendingUpgrade(8),Is.False);
   tracker.Observe(state,1.3,ready);Assert.That(ready,Is.Empty);
  }
 }
}
