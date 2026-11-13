using System;
using System.IO;
using System.Numerics;
using DefenseDuell.Core;
using NUnit.Framework;
#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;
#endif

namespace DefenseDuell.Networking.Tests
{
    public sealed class HostRulesNetwork14Tests
    {
        static CommandEnvelope Command(MatchSimulation sim,PlayerSlot slot,CommandType type,int waves=0,BigInteger gold=default)
            => new CommandEnvelope{SessionId=sim.State.SessionId,Sequence=sim.LastSequence(slot)+1,Type=type,Target=-1,
                ExpectedConfigRevision=type==CommandType.SetReady||type==CommandType.ApplyMatchRules?sim.State.ConfigRevision:0,
                ExpectedRosterRevision=type==CommandType.SetReady?sim.State.RosterRevision:0,RuleWaveCount=waves,RuleStartGold=gold};
        static MatchState Copy(MatchSimulation sim)
        {Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State),sim.Content.BaseContent,out var copy),Is.True);return copy;}
        static void Ready(MatchSimulation sim,PlayerSlot player)=>Assert.That(sim.Execute(player,Command(sim,player,CommandType.SetReady)).Accepted,Is.True);
        static void Finish(MatchSimulation sim){sim.State.Phase=MatchPhase.Finished;sim.State.Outcome=MatchOutcome.Draw;}

        [Test] public void WelcomeDerivesEffectiveOneWaveContentBeforeLedgerDecodeAndBindsHash()
        {
            var catalog=MapCatalog.CreateContents(GameContent.CreateDefault());var hash=WireProtocol.CatalogHash(catalog);
            for(int map=0;map<catalog.Length;map++)
            {
                var sim=new MatchSimulation(catalog[map]);var gold=BigInteger.Pow(10,40)+123;
                Assert.That(sim.Execute(PlayerSlot.Zero,Command(sim,PlayerSlot.Zero,CommandType.ApplyMatchRules,1,gold)).Accepted,Is.True);
                var bytes=WireProtocol.EncodeWelcome(hash,(byte)map,1,sim.State,sim.Content,5);
                Assert.That(WireProtocol.TryDecodeWelcome(bytes,hash,catalog,out var selected,out _,out var publication,out _,out var effective,out var state,out _),Is.True);
                Assert.That(selected,Is.EqualTo(map));Assert.That(publication,Is.EqualTo(5));Assert.That(effective.WaveCount,Is.EqualTo(1));
                Assert.That(effective.MatchStartGold,Is.EqualTo(gold));Assert.That(state.Players[0].ReservedGoldByWave.Length,Is.EqualTo(1));
                Assert.That(effective.BaseContentHash,Is.EqualTo(catalog[map].BaseContentHash));Assert.That(effective.ContentHash,Is.Not.EqualTo(effective.BaseContentHash));
                // The effective content hash is the final64 bytes of the versioned Welcome header.
                bytes[WireProtocol.WelcomeHeaderBytes-64]=(byte)'z';
                Assert.That(WireProtocol.TryDecodeWelcome(bytes,hash,catalog,out _,out _,out _,out _),Is.False);
            }
        }
        [Test] public void ExactMaximumMagnitudeFitsCommandAndOneMoreByteRejectsWithoutMutation()
        {
            var sim=new MatchSimulation(GameContent.CreateDefault());
            var gold=BigInteger.One<<(MatchRulesPolicy.MaxStartGoldMagnitudeBytes*8-1);
            var command=Command(sim,PlayerSlot.Zero,CommandType.ApplyMatchRules,1,gold);
            var bytes=WireProtocol.EncodeCommand(command);Assert.That(bytes.Length,Is.GreaterThan(65536));
            Assert.That(WireProtocol.TryDecodeCommand(bytes,out var decoded),Is.True);Assert.That(decoded.RuleStartGold,Is.EqualTo(gold));
            Assert.That(sim.Execute(PlayerSlot.Zero,decoded).Accepted,Is.True);Assert.That(Copy(sim).MatchRules.StartGold,Is.EqualTo(gold));
            var before=WireProtocol.EncodeSnapshot(sim.State);command=Command(sim,PlayerSlot.Zero,CommandType.ApplyMatchRules,1,gold<<8);
            Assert.That(WireProtocol.TryDecodeCommand(WireProtocol.EncodeCommand(command),out decoded),Is.True);
            Assert.That(sim.Execute(PlayerSlot.Zero,decoded).Code,Is.EqualTo(ErrorCode.RulesPayloadTooLarge));
            Assert.That(WireProtocol.EncodeSnapshot(sim.State),Is.EqualTo(before));
        }
        [Test] public void ReadyAndApplyFieldsRoundTripAndStaleReplayCannotReconfirmNewRules()
        {
            var sim=new MatchSimulation(GameContent.CreateDefault());var ready=Command(sim,PlayerSlot.One,CommandType.SetReady);
            Assert.That(WireProtocol.TryDecodeCommand(WireProtocol.EncodeCommand(ready),out var copy),Is.True);Assert.That(sim.Execute(PlayerSlot.One,copy).Accepted,Is.True);
            Assert.That(sim.Execute(PlayerSlot.Zero,Command(sim,PlayerSlot.Zero,CommandType.ApplyMatchRules,1,0)).Accepted,Is.True);
            var state=WireProtocol.EncodeSnapshot(sim.State);Assert.That(sim.Execute(PlayerSlot.One,copy).Accepted,Is.True);
            Assert.That(WireProtocol.EncodeSnapshot(sim.State),Is.EqualTo(state));Assert.That(sim.State.Players[1].Ready,Is.False);
            copy.Sequence++;Assert.That(sim.Execute(PlayerSlot.One,copy).Code,Is.EqualTo(ErrorCode.StaleLobbyRevision));
            Assert.That(WireProtocol.EncodeSnapshot(sim.State),Is.EqualTo(state));
        }
        [Test] public void PublicationsRejectReorderedReadyAndSameSequenceConflicts()
        {
            var sim=new MatchSimulation(GameContent.CreateDefault());var gate=new StatePublicationGate();
            var initial=Copy(sim);Assert.That(gate.TryAccept(initial,1,0,0),Is.True);
            Ready(sim,PlayerSlot.Zero);var ready=Copy(sim);var withdraw=Command(sim,PlayerSlot.Zero,CommandType.SetReady);withdraw.Kind=1;
            Assert.That(sim.Execute(PlayerSlot.Zero,withdraw).Accepted,Is.True);var unready=Copy(sim);
            Assert.That(gate.TryAccept(unready,3,0,0),Is.True);Assert.That(gate.TryAccept(ready,2,0,0),Is.False);
            Assert.That(gate.TryAccept(ready,3,0,0),Is.False);Assert.That(gate.Sequence,Is.EqualTo(3));
        }
        [Test] public void CountdownAndPlayingCannotReopenLobbyEvenAtHigherPublication()
        {
            var sim=new MatchSimulation(GameContent.CreateDefault());var gate=new StatePublicationGate();var lobby=Copy(sim);
            Assert.That(gate.TryAccept(lobby,1,0,0),Is.True);Ready(sim,PlayerSlot.Zero);Ready(sim,PlayerSlot.One);
            Assert.That(gate.TryAccept(Copy(sim),2,0,0),Is.True);Assert.That(gate.TryAccept(lobby,3,0,0),Is.False);
            for(int i=0;i<sim.Content.CountdownTicks;i++)sim.Step();Assert.That(gate.TryAccept(Copy(sim),4,0,0),Is.True);
            Assert.That(gate.TryAccept(lobby,5,0,0),Is.False);
        }
        [Test] public void RematchRequiresRequestedExactSuccessorAndAllowsHostAlreadyReady()
        {
            var sim=new MatchSimulation(GameContent.CreateDefault(),1,123);Ready(sim,PlayerSlot.Zero);Ready(sim,PlayerSlot.One);Finish(sim);
            var old=Copy(sim);var gate=new StatePublicationGate();Assert.That(gate.TryAccept(old,10,0,0),Is.True);
            Assert.That(sim.Execute(PlayerSlot.Zero,Command(sim,PlayerSlot.Zero,CommandType.RequestRematch)).Accepted,Is.True);
            Assert.That(sim.Execute(PlayerSlot.One,Command(sim,PlayerSlot.One,CommandType.RequestRematch)).Accepted,Is.True);
            Ready(sim,PlayerSlot.Zero);var lobby=Copy(sim);
            Assert.That(gate.TryAccept(lobby,11,123,0),Is.False);Assert.That(gate.TryAccept(lobby,11,122,123),Is.False);
            Assert.That(gate.TryAccept(lobby,11,123,123),Is.True);Assert.That(gate.TryAccept(old,12,0,123),Is.False);
            Assert.That(gate.Epoch,Is.EqualTo(124));Assert.That(lobby.Players[0].Ready,Is.True);Assert.That(lobby.Players[1].Gold,Is.EqualTo(BigInteger.Zero));
        }
        [Test] public void HigherPublicationCannotRollBackRevisionsOrChangeRulesAtSameRevision()
        {
            var sim=new MatchSimulation(GameContent.CreateDefault());Assert.That(sim.Execute(PlayerSlot.Zero,Command(sim,PlayerSlot.Zero,CommandType.ApplyMatchRules,2,999)).Accepted,Is.True);
            var gate=new StatePublicationGate();Assert.That(gate.TryAccept(Copy(sim),1,0,0),Is.True);
            var invalid=Copy(sim);invalid.ConfigRevision=0;Assert.That(gate.TryAccept(invalid,2,0,0),Is.False);
            invalid=Copy(sim);invalid.MatchRules=new MatchRules(2,998);Assert.That(gate.TryAccept(invalid,2,0,0),Is.False);
            sim.ConfigureTowerAvailability(PlayerSlot.One,47);Assert.That(gate.TryAccept(Copy(sim),2,0,0),Is.True);
            invalid=Copy(sim);invalid.RosterRevision=0;Assert.That(gate.TryAccept(invalid,3,0,0),Is.False);
            Ready(sim,PlayerSlot.Zero);Ready(sim,PlayerSlot.One);Assert.That(gate.TryAccept(Copy(sim),3,0,0),Is.True);
            invalid=Copy(sim);invalid.ConfigRevision++;invalid.MatchRules=new MatchRules(2,1000);Assert.That(gate.TryAccept(invalid,4,0,0),Is.False);
        }
        [Test] public void AckCarriesExactEpochRequestAndPublicationWithoutApplyingState()
        {
            var sim=new MatchSimulation(GameContent.CreateDefault());var gate=new StatePublicationGate();Assert.That(gate.TryAccept(Copy(sim),10,0,0),Is.True);
            var bytes=WireProtocol.EncodeResult(99,new CommandResult{Sequence=123,Code=ErrorCode.StaleLobbyRevision,Tick=0},4);
            Assert.That(bytes.Length,Is.EqualTo(29));Assert.That(WireProtocol.TryDecodeResult(bytes,out var epoch,out var result,out var sequence),Is.True);
            Assert.That(epoch,Is.EqualTo(99));Assert.That(result.Sequence,Is.EqualTo(123));Assert.That(sequence,Is.EqualTo(4));
            Assert.That(gate.Sequence,Is.EqualTo(10));Assert.That(gate.Epoch,Is.EqualTo(sim.State.SessionId));
        }
        [Test] public void LobbyWalletGrantMarkerAndImpossibleOrderCountAreRejected()
        {
            var sim=new MatchSimulation(GameContent.CreateDefault());sim.State.Players[0].Gold=180;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State),sim.Content,out _),Is.False);
            sim.State.Players[0].Gold=0;sim.State.StartGoldGranted=true;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State),sim.Content,out _),Is.False);
            sim.State.StartGoldGranted=false;var bytes=WireProtocol.EncodeSnapshot(sim.State);bytes[bytes.Length-1]=127;
            Assert.That(WireProtocol.TryDecodeSnapshot(bytes,sim.Content,out _),Is.False);
        }
        [Test] public void TwoHundredWaveWelcomeDerivesExactContentAndFullLedgersOnEveryMap()
        {
            var catalog=MapCatalog.CreateContents(GameContent.CreateDefault());var hash=WireProtocol.CatalogHash(catalog);
            for(int map=0;map<catalog.Length;map++)
            {
                var sim=new MatchSimulation(catalog[map]);
                Assert.That(sim.Execute(PlayerSlot.Zero,Command(sim,PlayerSlot.Zero,CommandType.ApplyMatchRules,200,1000)).Accepted,Is.True);
                var envelope=WireProtocol.EncodeWelcome(hash,(byte)map,1,sim.State,sim.Content,2);
                var assembler=new SnapshotAssembler();assembler.Reset(sim.State.SessionId);byte[] reconstructed=null;
                int pages=(envelope.Length+WireProtocol.ChunkPayloadBytes-1)/WireProtocol.ChunkPayloadBytes;
                for(int page=pages-1;page>=0;page--)
                    Assert.That(assembler.TryAdd(WireProtocol.EncodeChunk(sim.State.SessionId,1,envelope,page),0,out reconstructed),Is.EqualTo(page==0));
                Assert.That(WireProtocol.TryDecodeWelcome(reconstructed,hash,catalog,out var selected,out _,out _,out _,out var effective,out var state,out _),Is.True);
                Assert.That(selected,Is.EqualTo(map));Assert.That(effective.WaveCount,Is.EqualTo(200));Assert.That(state.MatchRules.WaveCount,Is.EqualTo(200));
                Assert.That(effective.ContentHash,Is.EqualTo(sim.Content.ContentHash));Assert.That(effective.Waves[199].HpPercent,Is.EqualTo(2488));
                foreach(var player in state.Players)
                {Assert.That(player.ReservedGoldByWave.Length,Is.EqualTo(200));Assert.That(player.SpawnedQuarterGoldByWave.Length,Is.EqualTo(200));Assert.That(player.Gold,Is.EqualTo(BigInteger.Zero));}
                Ready(sim,PlayerSlot.Zero);var ready=Command(sim,PlayerSlot.One,CommandType.SetReady);
                Assert.That(sim.Execute(PlayerSlot.One,ready).Accepted,Is.True);var before=WireProtocol.EncodeSnapshot(sim.State);
                Assert.That(sim.Execute(PlayerSlot.One,ready).Accepted,Is.True);Assert.That(WireProtocol.EncodeSnapshot(sim.State),Is.EqualTo(before));
                var started=Copy(sim);Assert.That(started.StartGoldGranted,Is.True);
                foreach(var player in started.Players)Assert.That(player.Gold,Is.EqualTo(new BigInteger(1000)));
            }
        }
        [Test] public void ArrangedLateWaveSnapshotsRoundTrip199And200AndRejectPastConfiguredBoundary()
        {
            // Arranged codec fixtures, deliberately not evidence of playing 200 complete waves.
            var sim=new MatchSimulation(GameContent.CreateDefault().WithMatchRules(new MatchRules(200,1000)));
            Ready(sim,PlayerSlot.Zero);Ready(sim,PlayerSlot.One);
            for(int i=0;i<sim.Content.CountdownTicks;i++)sim.Step();
            sim.State.WaveId=199;Assert.That(Copy(sim).WaveId,Is.EqualTo(new BigInteger(199)));
            sim.State.WaveId=200;sim.State.WavePhase=WavePhase.Active;sim.State.PreparationRemaining=0;
            int kind=sim.Content.GetWave(200).Plan[0];var recipe=UnitResearchRules.Resolve(sim.Content,kind,0,UnitOrigin.NeutralWave,200);
            sim.State.NeutralCursor[0]=1;sim.State.Units.Add(new UnitState{Id=1,Kind=kind,Defender=PlayerSlot.Zero,
                Origin=UnitOrigin.NeutralWave,WaveId=200,NeutralRecipeIndex=0,Hp=recipe.MaxHp,SlowPermille=1000});
            var valid=Copy(sim);Assert.That(valid.WaveId,Is.EqualTo(new BigInteger(200)));Assert.That(valid.Units[0].Hp,Is.EqualTo(recipe.MaxHp));
            sim.State.Units[0].WaveId=201;Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State),sim.Content,out _),Is.False);
            sim.State.Units[0].WaveId=200;sim.State.WaveId=201;
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State),sim.Content,out _),Is.False);
        }
        [Test] public void TwoHundredWaveSnapshotRejectsMismatchedLedgerAndUnsupportedRules()
        {
            var sim=new MatchSimulation(GameContent.CreateDefault().WithMatchRules(new MatchRules(200,0)));
            sim.State.Players[0].ReservedGoldByWave=new BigInteger[199];sim.State.Players[0].SpawnedQuarterGoldByWave=new BigInteger[199];
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State),sim.Content,out _),Is.False);
            sim.State.Players[0].ReservedGoldByWave=new BigInteger[200];sim.State.Players[0].SpawnedQuarterGoldByWave=new BigInteger[200];
            sim.State.MatchRules=new MatchRules(201,0,mapId:sim.Content.Map.Id);
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(sim.State),sim.Content,out _),Is.False);
        }
#if UNITY_EDITOR
        [Test] public void ProductionWrongEpochRequestCannotOverwriteRematchTransitionProof()
        {
            var go=new GameObject("HostRules14SessionTest");var session=go.AddComponent<SessionCoordinator>();
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            try
            {
                session.Initialize(GameContent.CreateDefault());session.StartOffline();
                var sim=(MatchSimulation)typeof(SessionCoordinator).GetField("simulation",flags).GetValue(session);
                var execute=typeof(SessionCoordinator).GetMethod("ExecuteObserved",flags);
                ulong current=sim.State.SessionId;var wrong=new CommandEnvelope{SessionId=current==1?2:current-1,Sequence=100,Type=CommandType.SetReady,Target=-1};
                var result=(CommandResult)execute.Invoke(session,new object[]{PlayerSlot.Zero,wrong});Assert.That(result.Code,Is.EqualTo(ErrorCode.WrongSession));
                Assert.That((ulong)typeof(SessionCoordinator).GetField("previousEpoch",flags).GetValue(session),Is.Zero);
                Ready(sim,PlayerSlot.Zero);Finish(sim);
                execute.Invoke(session,new object[]{PlayerSlot.Zero,Command(sim,PlayerSlot.Zero,CommandType.RequestRematch)});
                execute.Invoke(session,new object[]{PlayerSlot.One,Command(sim,PlayerSlot.One,CommandType.RequestRematch)});
                Assert.That((ulong)typeof(SessionCoordinator).GetField("previousEpoch",flags).GetValue(session),Is.EqualTo(current));
                execute.Invoke(session,new object[]{PlayerSlot.One,wrong});
                Assert.That((ulong)typeof(SessionCoordinator).GetField("previousEpoch",flags).GetValue(session),Is.EqualTo(current));
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
#endif
    }
}
