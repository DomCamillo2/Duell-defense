using BigInteger=System.Numerics.BigInteger;
using System.Collections.Generic;
using DefenseDuell.Core;
using NUnit.Framework;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class CommandFeedbackTests
    {
        static MatchState State()=>new MatchState{SessionId=7,Tick=40,Phase=MatchPhase.Playing};
        static TowerState Tower(int id=101,int level=0)=>new TowerState{Id=id,Owner=PlayerSlot.Zero,Slot=3,Kind=0,Level=level,DamageLevel=level,Position=new MapPoint(100,100),SiteId=-1};
        static CommandResult Ack(ulong sequence=1,ErrorCode error=ErrorCode.None)=>new CommandResult{Sequence=sequence,Tick=40,Code=error};
        static PresentationCommandTracker Track(MatchState state,CommandType type=CommandType.BuildTower)
        {
            var tracker=new PresentationCommandTracker();tracker.Reset(state.SessionId);
            tracker.Register(1,PresentationCommandContext.Capture(state,PlayerSlot.Zero,type,0,type==CommandType.BuildTower?-1:101,10,new MapPoint(100,100)));return tracker;
        }
        [Test]
        public void AckBeforeStateWaitsAndEmitsExactlyOnce()
        {
            var state=State();var tracker=Track(state);var ready=new List<PresentationCommandContext>();
            Assert.That(tracker.Resolve(Ack(),out var context),Is.True);Assert.That(context.Target,Is.EqualTo(-1));
            tracker.Observe(state,10.1,ready);Assert.That(ready,Is.Empty);var pending=new List<PresentationCommandContext>();tracker.CopyPendingBuilds(pending);Assert.That(pending.Count,Is.EqualTo(1));
            state.Towers.Add(Tower());tracker.Observe(state,10.2,ready);
            Assert.That(ready.Count,Is.EqualTo(1));Assert.That(ready[0].Entity,Is.EqualTo(101));Assert.That(tracker.IsBuildReserved(new MapPoint(100,100),-1,60),Is.False);
            tracker.Observe(state,10.3,ready);Assert.That(ready,Is.Empty);Assert.That(tracker.Resolve(Ack(),out _),Is.False);
        }
        [Test]
        public void StateBeforeAckAtTheSameTickIsNotAnEarlySuccess()
        {
            var state=State();var tracker=Track(state);var ready=new List<PresentationCommandContext>();
            state.Towers.Add(Tower());tracker.Observe(state,10.1,ready);Assert.That(ready,Is.Empty);Assert.That(tracker.IsBuildReserved(new MapPoint(100,100),-1,60),Is.True);
            Assert.That(tracker.Resolve(Ack(),out _),Is.True);tracker.Observe(state,10.2,ready);Assert.That(ready.Count,Is.EqualTo(1));
        }
        [Test]
        public void UpgradeRequiresTheCapturedEntityAndExactExpectedLevel()
        {
            var state=State();state.Towers.Add(Tower());var tracker=Track(state,CommandType.UpgradeTower);var ready=new List<PresentationCommandContext>();
            tracker.Resolve(Ack(),out var context);Assert.That(context.Entity,Is.EqualTo(101));Assert.That(context.PreviousLevel,Is.EqualTo(BigInteger.Zero));Assert.That(context.ExpectedLevel,Is.EqualTo((BigInteger)1));
            tracker.Observe(state,10.1,ready);Assert.That(ready,Is.Empty);
            state.Towers[0].Level=1;state.Towers[0].DamageLevel=1;tracker.Observe(state,10.2,ready);Assert.That(ready.Count,Is.EqualTo(1));
            tracker.Observe(state,10.3,ready);Assert.That(ready,Is.Empty);
        }
        [Test]
        public void ReplacedBuildingNeverReceivesAnOldBuildPulse()
        {
            var state=State();var tracker=Track(state);var ready=new List<PresentationCommandContext>();
            state.Towers.Add(Tower());tracker.Observe(state,10.1,ready);
            state.Towers[0]=Tower(202);tracker.Observe(state,10.2,ready);tracker.Resolve(Ack(),out _);tracker.Observe(state,10.3,ready);Assert.That(ready,Is.Empty);
        }
        [Test]
        public void RejectionTimeoutAndViewCancellationNeverCelebrate()
        {
            var state=State();var tracker=Track(state);var ready=new List<PresentationCommandContext>();state.Towers.Add(Tower());
            tracker.Resolve(Ack(error:ErrorCode.InsufficientGold),out _);tracker.Observe(state,10.1,ready);Assert.That(ready,Is.Empty);
            state.Towers.Clear();tracker=Track(state);tracker.Resolve(Ack(),out _);state.Towers.Add(Tower());tracker.Observe(state,15,ready);Assert.That(ready,Is.Empty);
            state.Towers.Clear();tracker=Track(state);tracker.DiscardVisuals();state.Towers.Add(Tower());tracker.Resolve(Ack(),out _);tracker.Observe(state,10.2,ready);Assert.That(ready,Is.Empty);
        }
        [Test]
        public void NewSessionCannotResolvePreviousPendingWork()
        {
            var state=State();var tracker=Track(state);tracker.Reset(99);Assert.That(tracker.Resolve(Ack(),out _),Is.False);Assert.That(tracker.Count,Is.Zero);
        }
        [Test]
        public void LaterSaleDiscardsUnseenBuildFeedback()
        {
            var state=State();var tracker=Track(state);var ready=new List<PresentationCommandContext>();state.Towers.Add(Tower());
            tracker.Register(2,PresentationCommandContext.Capture(state,PlayerSlot.Zero,CommandType.SellTower,0,101,10.1));
            tracker.Resolve(Ack(),out _);tracker.Observe(state,10.2,ready);Assert.That(ready,Is.Empty);
        }
        [Test]
        public void SameTotalLevelWithWrongUpgradePathNeverConfirms()
        {
            var state=State();state.Towers.Add(Tower());var tracker=new PresentationCommandTracker();tracker.Reset(state.SessionId);
            var context=PresentationCommandContext.Capture(state,PlayerSlot.Zero,CommandType.UpgradeTower,(int)TowerUpgradePath.Range,101,10);
            tracker.Register(1,context);Assert.That(tracker.HasPendingUpgrade(101),Is.True);tracker.Resolve(Ack(),out _);
            state.Towers[0].Level=1;state.Towers[0].DamageLevel=1;var ready=new List<PresentationCommandContext>();tracker.Observe(state,10.1,ready);
            Assert.That(ready,Is.Empty);Assert.That(tracker.HasPendingUpgrade(101),Is.False);
        }
        [Test]
        public void RangeSnapshotBeforeAckConfirmsOnlyCapturedFullPath()
        {
            var state=State();state.Towers.Add(Tower(level:1));var tracker=new PresentationCommandTracker();tracker.Reset(state.SessionId);
            var context=PresentationCommandContext.Capture(state,PlayerSlot.Zero,CommandType.UpgradeTower,(int)TowerUpgradePath.Range,101,10);
            tracker.Register(1,context);Assert.That(context.ExpectedDamageLevel,Is.EqualTo((BigInteger)1));Assert.That(context.ExpectedRangeLevel,Is.EqualTo((BigInteger)1));
            state.Towers[0].Level=2;state.Towers[0].RangeLevel=1;var ready=new List<PresentationCommandContext>();tracker.Observe(state,10.1,ready);Assert.That(ready,Is.Empty);
            tracker.Resolve(Ack(),out _);tracker.Observe(state,10.2,ready);Assert.That(ready.Count,Is.EqualTo(1));Assert.That(tracker.HasPendingUpgrade(101),Is.False);
        }
    }
}
