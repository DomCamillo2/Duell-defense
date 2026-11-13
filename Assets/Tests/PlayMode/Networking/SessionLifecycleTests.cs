using System.Collections;
using BigInteger=System.Numerics.BigInteger;
using DefenseDuell.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DefenseDuell.Networking.Tests
{
    public sealed class SessionLifecycleTests
    {
        private GameObject root;
        private SessionCoordinator session;

        [UnitySetUp] public IEnumerator SetUp()
        {
            root = new GameObject("Session lifecycle test");
            session = root.AddComponent<SessionCoordinator>(); session.Initialize(GameContent.CreateDefault());
            yield return null;
        }

        [UnityTearDown] public IEnumerator TearDown()
        { if (session != null) session.Shutdown(); if (root != null) Object.Destroy(root); yield return null; yield return null; }

        [UnityTest] public IEnumerator SameTickNewerSnapshotAppliesConsentAndOldRevisionCannotUndoIt()
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var sim = new MatchSimulation(session.Content, 7, 999);
            void Execute(PlayerSlot player,CommandType type,int kind=0,int target=-1)
            {
                var command=new CommandEnvelope{SessionId=sim.State.SessionId,Sequence=sim.LastSequence(player)+1,
                    Type=type,Kind=kind,Target=target,ExpectedWave=type==CommandType.SetWaveReady?sim.State.WaveId:0,
                    ExpectedConfigRevision=type==CommandType.SetReady?sim.State.ConfigRevision:0,
                    ExpectedRosterRevision=type==CommandType.SetReady?sim.State.RosterRevision:0};
                Assert.That(sim.Execute(player,command).Accepted,Is.True);
            }
            Execute(PlayerSlot.Zero,CommandType.SetReady);Execute(PlayerSlot.One,CommandType.SetReady);
            for(int i=0;i<sim.Content.CountdownTicks;i++)sim.Step();
            Execute(PlayerSlot.Zero,CommandType.SetWaveReady,1);Execute(PlayerSlot.One,CommandType.SetWaveReady,1);sim.Step();
            Assert.That(sim.State.StartGoldGranted,Is.True);Assert.That(sim.State.WavePhase,Is.EqualTo(WavePhase.Active));
            var apply = typeof(SessionCoordinator).GetMethod("ApplyStateEnvelope",flags);
            Assert.That(apply,Is.Not.Null,"The test must exercise the production Wire9 envelope acceptance path.");
            byte[] Envelope(uint transfer,ulong publication)=>WireProtocol.EncodeWelcome(session.BaseCatalogHash,0,transfer,
                sim.State,sim.Content,publication);
            Assert.That((bool)apply.Invoke(session,new object[]{Envelope(1,1),(uint)1}),Is.True);
            var tick=session.State.Tick;
            Execute(PlayerSlot.Zero,CommandType.SetSpeedVote,1);Execute(PlayerSlot.One,CommandType.SetSpeedVote,1);
            var approved=Envelope(2,2);
            Assert.That((bool)apply.Invoke(session,new object[]{approved,(uint)2}),Is.True);
            Assert.That(session.State.Tick,Is.EqualTo(tick));Assert.That(session.State.SimulationSpeed,Is.EqualTo(2));
            Execute(PlayerSlot.Zero,CommandType.SetSpeedVote,0);
            Assert.That((bool)apply.Invoke(session,new object[]{Envelope(3,3),(uint)3}),Is.True);
            Assert.That(session.State.Tick,Is.EqualTo(tick));Assert.That(session.State.SimulationSpeed,Is.EqualTo(1));
            Assert.That((bool)apply.Invoke(session,new object[]{approved,(uint)2}),Is.False);
            Assert.That(session.State.SimulationSpeed,Is.EqualTo(1));
            // A newer transfer number is not permission to reapply an older publication.
            Execute(PlayerSlot.Zero,CommandType.SetSpeedVote,1);
            Assert.That((bool)apply.Invoke(session,new object[]{Envelope(4,2),(uint)4}),Is.False);
            Assert.That((bool)apply.Invoke(session,new object[]{Envelope(5,3),(uint)5}),Is.False);
            Assert.That(session.AuthoritativeStateSequence,Is.EqualTo(3));
            Assert.That(session.State.SessionId,Is.EqualTo(999));Assert.That(session.Content.ContentHash,Is.EqualTo(sim.Content.ContentHash));
            Assert.That(session.State.SimulationSpeed,Is.EqualTo(1));
            yield return null;
        }

        [UnityTest] public IEnumerator OfflineConsentMirrorsGameplayWhileControlClockStaysTwenty()
        {
            session.StartOffline(); session.State.Phase = MatchPhase.Playing; session.State.WavePhase = WavePhase.Active;
            int before = session.State.PlayTicks, controlBefore = session.State.Tick; double began = Time.realtimeSinceStartupAsDouble;
            yield return new WaitForSecondsRealtime(1.1f);
            double normal = (session.State.PlayTicks - before) / (Time.realtimeSinceStartupAsDouble - began);
            Assert.That(normal, Is.InRange(17, 23));
            Assert.That((session.State.Tick - controlBefore) / (Time.realtimeSinceStartupAsDouble - began), Is.InRange(17, 23));
            session.Submit(CommandType.SetSpeedVote, 1);
            Assert.That(session.State.Players[0].SpeedReady && session.State.Players[1].SpeedReady, Is.True);
            Assert.That(session.State.SimulationSpeed, Is.EqualTo(2));
            before = session.State.PlayTicks; controlBefore = session.State.Tick; began = Time.realtimeSinceStartupAsDouble;
            yield return new WaitForSecondsRealtime(1.1f);
            double fast = (session.State.PlayTicks - before) / (Time.realtimeSinceStartupAsDouble - began);
            Assert.That(fast, Is.InRange(36, 44)); Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That((session.State.Tick - controlBefore) / (Time.realtimeSinceStartupAsDouble - began), Is.InRange(17, 23));
            session.Submit(CommandType.SetSpeedVote, 0);
            Assert.That(session.State.SimulationSpeed, Is.EqualTo(1));
            Assert.That(session.State.Players[1].SpeedReady, Is.False);
            session.Shutdown(); session.StartOffline();
            Assert.That(session.State.Players[0].SpeedReady || session.State.Players[1].SpeedReady, Is.False);
        }

        [UnityTest] public IEnumerator PreparationUsesControlTimeAndOfflineEarlyReadinessMirrorsCurrentVote()
        {
            session.StartOffline(); session.State.Phase = MatchPhase.Playing;
            session.State.WavePhase = WavePhase.Preparing; session.State.PreparationRemaining = session.Content.WaveRules.PreparationTicks;
            session.Submit(CommandType.SetSpeedVote, 1);
            Assert.That(session.State.SimulationSpeed, Is.EqualTo(1));
            BigInteger gold = session.State.Players[0].Gold, remainder = session.State.Players[0].GoldRemainder; int tick = session.State.Tick;
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(session.State.Tick - tick, Is.InRange(9, 15));
            Assert.That(session.State.PlayTicks, Is.Zero); Assert.That(session.State.Players[0].Gold, Is.EqualTo(gold));
            Assert.That(session.State.Players[0].GoldRemainder, Is.EqualTo(remainder));
            session.Submit(CommandType.SetWaveReady, 1, expectedWave: session.State.WaveId);
            Assert.That(session.State.Players[0].WaveReady && session.State.Players[1].WaveReady, Is.True);
            session.Submit(CommandType.SetWaveReady, 0, expectedWave: session.State.WaveId);
            Assert.That(session.State.Players[0].WaveReady || session.State.Players[1].WaveReady, Is.False);
            session.Submit(CommandType.SetWaveReady, 1, expectedWave: session.State.WaveId);
            yield return new WaitForSecondsRealtime(.12f);
            Assert.That(session.State.WavePhase, Is.EqualTo(WavePhase.Active));
            Assert.That(session.State.SimulationSpeed, Is.EqualTo(2));
            Assert.That(session.State.Players[0].WaveReady || session.State.Players[1].WaveReady, Is.False);
        }

        [UnityTest] public IEnumerator DifficultyAndAsymmetricProfileSelectionAreIdleOnly()
        {
            Assert.That(session.SetDifficulty(BotDifficulty.Hard), Is.True);
            Assert.That(session.SetTowerAvailability(47), Is.True);
            Assert.That(session.SetTowerAvailability(256), Is.False);
            session.StartOffline();
            Assert.That(session.Difficulty, Is.EqualTo(BotDifficulty.Hard));
            Assert.That(session.State.Players[0].AvailableTowerMask, Is.EqualTo(47));
            Assert.That(session.State.Players[1].AvailableTowerMask, Is.EqualTo(47));
            Assert.That(session.SetDifficulty(BotDifficulty.Easy), Is.False);
            Assert.That(session.SetTowerAvailability(79), Is.False);
            session.Shutdown(); Assert.That(session.SetDifficulty(BotDifficulty.Easy), Is.True);
            yield return null;
        }

        [UnityTest] public IEnumerator TenOfflineRestartsResetStateAndRetainSingleEventDelivery()
        {
            var acknowledgements = 0;
            session.CommandResolved += _ => acknowledgements++;
            ulong previous = 0;
            for (var cycle = 0; cycle < 10; cycle++)
            {
                session.StartOffline((uint)cycle + 1);
                Assert.That(session.Status, Is.EqualTo(ConnectionState.Lobby));
                Assert.That(session.State.SessionId, Is.Not.EqualTo(previous)); previous = session.State.SessionId;
                Assert.That(session.State.Players[0].Gold, Is.EqualTo(System.Numerics.BigInteger.Zero));
                Assert.That(session.State.StartGoldGranted,Is.False);
                session.Submit(CommandType.SetReady);
                Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.Countdown));
                Assert.That(acknowledgements, Is.EqualTo(cycle + 1));
                session.Shutdown();
                Assert.That(session.State, Is.Null); Assert.That(session.PendingCommands, Is.Zero);
                Assert.That(session.RegisteredMessageHandlers, Is.Zero);
                yield return null;
            }
        }

        [UnityTest] public IEnumerator MapChoiceIsIdleOnlyAndOfflineUsesSelectedContent()
        {
            Assert.That(session.MapNames.Length, Is.EqualTo(MapCatalog.Count));
            var names = session.MapNames; names[0] = "mutated outside";
            Assert.That(session.MapNames[0], Is.Not.EqualTo(names[0]));
            Assert.That(session.SelectMap(2), Is.True);
            var selectedHash = session.Content.ContentHash;
            session.StartOffline();
            Assert.That(session.SelectedMapIndex, Is.EqualTo(2));
            Assert.That(session.SelectMap(1), Is.False);
            Assert.That(session.Content.ContentHash, Is.EqualTo(selectedHash));
            session.Shutdown();
            Assert.That(session.SelectMap(1), Is.True);
            Assert.That(session.Content.ContentHash, Is.Not.EqualTo(selectedHash));
            Assert.That(session.SelectMap(MapCatalog.Count), Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator LanHostShutdownCanStartAgainTenTimes()
        {
            for (var cycle = 0; cycle < 10; cycle++)
            {
                session.StartLanHost(19177);
                yield return new WaitForSecondsRealtime(.12f);
                Assert.That(session.Status, Is.EqualTo(ConnectionState.Lobby), session.Message);
                Assert.That(session.ConnectedPlayers, Is.EqualTo(1));
                Assert.That(session.RegisteredMessageHandlers, Is.EqualTo(8));
                session.Shutdown();
                Assert.That(session.RegisteredMessageHandlers, Is.Zero);
                yield return new WaitForSecondsRealtime(.12f);
            }
        }

        [UnityTest] public IEnumerator CancelledJoinCannotRestartAfterNewOfflineSession()
        {
            session.JoinLan("127.0.0.1", 19178);
            session.Shutdown(); session.StartOffline(42);
            var epoch = session.State.SessionId;
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(session.Mode, Is.EqualTo(SessionMode.Offline));
            Assert.That(session.State.SessionId, Is.EqualTo(epoch));
            Assert.That(session.Status, Is.EqualTo(ConnectionState.Lobby));
            Assert.That(session.RegisteredMessageHandlers, Is.Zero);
        }

        [UnityTest] public IEnumerator BackgroundedLanHostAbortsAndRequiresNewSession()
        {
            session.StartLanHost(19179);
            yield return new WaitForSecondsRealtime(.1f);
            session.SendMessage("OnApplicationPause", true);
            Assert.That(session.Status, Is.EqualTo(ConnectionState.ConnectionLost));
            Assert.That(session.State.Outcome, Is.EqualTo(MatchOutcome.Aborted));
            session.SendMessage("OnApplicationPause", false);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(session.Status, Is.EqualTo(ConnectionState.ConnectionLost));
            session.StartOffline();
            Assert.That(session.Status, Is.EqualTo(ConnectionState.Lobby));
        }
    }
}
