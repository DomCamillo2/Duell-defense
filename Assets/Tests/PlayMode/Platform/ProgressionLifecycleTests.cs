using System;
using System.Collections;
using System.IO;
using System.Reflection;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using DefenseDuell.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace DefenseDuell.Tests
{
    public sealed class ProgressionLifecycleTests
    {
        static MatchSimulation Simulation(SessionCoordinator session)=>(MatchSimulation)typeof(SessionCoordinator).GetField("simulation",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(session);
        static void Observe(LocalProgress progress)=>typeof(LocalProgress).GetMethod("Observe",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(progress,null);
        [UnityTest]
        public IEnumerator TerminalReplayAndFreshRematchAwardExactlyOnceAndPersist()
        {
            string directory=Path.Combine(Application.temporaryCachePath,"progression-test-"+Guid.NewGuid().ToString("N"));
            string path=Path.Combine(directory,"profile.save");
            var go=new GameObject("Progression lifecycle review");
            var session=go.AddComponent<SessionCoordinator>();session.Initialize(GameContent.CreateDefault());
            var progress=go.AddComponent<LocalProgress>();progress.Initialize(session,path);
            try
            {
                session.StartOffline(8421);Observe(progress);
                Assert.That(progress.Profile.ActiveCounter,Is.EqualTo(1));
                var simulation=Simulation(session);
                // A staged terminal result traverses the real Core end-of-step resolver.
                simulation.State.Phase=MatchPhase.Playing;simulation.State.WavePhase=WavePhase.Active;
                simulation.State.Players[1].Hp=0;simulation.Step();
                Assert.That(simulation.State.Outcome,Is.EqualTo(MatchOutcome.Player0));
                for(int i=0;i<20;i++)Observe(progress);
                Assert.That(progress.Profile.Medals,Is.EqualTo(100));
                Assert.That(progress.LastReward,Is.EqualTo(100));
                session.Submit(CommandType.RequestRematch);Observe(progress);
                Assert.That(session.State.Phase,Is.EqualTo(MatchPhase.Lobby));
                Assert.That(progress.Profile.ActiveCounter,Is.EqualTo(2));
                simulation=Simulation(session);simulation.State.Phase=MatchPhase.Playing;simulation.State.WavePhase=WavePhase.Active;
                simulation.State.Players[1].Hp=0;simulation.Step();Observe(progress);
                Assert.That(progress.Profile.Medals,Is.EqualTo(200));
                session.Shutdown();
                Assert.That(progress.Unlock(5),Is.True);
                Assert.That(progress.Profile.Medals,Is.Zero);
                Assert.That(session.LocalTowerMask,Is.EqualTo(31));
                progress.Initialize(session,path);
                Assert.That(progress.Profile.TowerMask,Is.EqualTo(63));
                Assert.That(progress.Profile.Medals,Is.Zero);
                session.StartOffline(8421);Observe(progress);
                Assert.That(progress.Profile.ActiveCounter,Is.EqualTo(3),"Repeated seed/epoch never reuses a persistent reward ticket");
                simulation=Simulation(session);simulation.Abort();Observe(progress);
                Assert.That(progress.Profile.Medals,Is.Zero);
            }
            finally {session.Shutdown();Object.Destroy(go);if(Directory.Exists(directory))Directory.Delete(directory,true);}
            yield return null;
        }
        sealed class FaultStore : ProfileStore
        {
            public bool FailWrites;
            public FaultStore(string path):base(path,p=>JsonUtility.ToJson(p),s=>JsonUtility.FromJson<PlayerProfile>(s)){}
            public override void Save(PlayerProfile profile)
            {if(FailWrites)throw new IOException("Injected transient profile write failure");base.Save(profile);}
        }
        [UnityTest]
        public IEnumerator FailedSettlementSurvivesMenuAndNewSessionThenRetriesExactlyOnce()
        {
            string directory=Path.Combine(Application.temporaryCachePath,"progression-fault-"+Guid.NewGuid().ToString("N"));
            string path=Path.Combine(directory,"profile.save");
            var go=new GameObject("Transient profile failure review");
            var session=go.AddComponent<SessionCoordinator>();session.Initialize(GameContent.CreateDefault());
            var progress=go.AddComponent<LocalProgress>();progress.Initialize(session,path);
            var store=new FaultStore(path);
            typeof(LocalProgress).GetField("store",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(progress,store);
            try
            {
                session.StartOffline(7);Observe(progress);
                store.FailWrites=true;
                var sim=Simulation(session);sim.State.Phase=MatchPhase.Playing;sim.State.WavePhase=WavePhase.Active;sim.State.Players[1].Hp=0;sim.Step();
                Observe(progress);Observe(progress);
                Assert.That(progress.Profile.Medals,Is.Zero,"Uncommitted currency is not displayed as durable");
                session.Shutdown();session.StartOffline(7);Observe(progress);
                store.FailWrites=false;
                yield return new WaitForSecondsRealtime(1.15f);
                Assert.That(progress.Profile.Medals,Is.EqualTo(100),"Old victory survives a new live session");
                Assert.That(progress.Profile.ActiveCounter,Is.EqualTo(2));
                sim=Simulation(session);sim.State.Phase=MatchPhase.Playing;sim.State.WavePhase=WavePhase.Active;sim.State.Players[1].Hp=0;sim.Step();
                Observe(progress);Observe(progress);
                Assert.That(progress.Profile.Medals,Is.EqualTo(200));
                Assert.That(store.Load(out _).Medals,Is.EqualTo(200));
                session.Submit(CommandType.RequestRematch);Observe(progress);
                store.FailWrites=true;
                sim=Simulation(session);sim.State.Phase=MatchPhase.Playing;sim.State.WavePhase=WavePhase.Active;sim.State.Players[1].Hp=0;sim.Step();
                Observe(progress);
                session.Submit(CommandType.RequestRematch);Observe(progress);
                store.FailWrites=false;
                yield return new WaitForSecondsRealtime(1.15f);
                Assert.That(progress.Profile.Medals,Is.EqualTo(300),"Direct rematch preserves the failed victory write");
                Assert.That(progress.Profile.ActiveCounter,Is.EqualTo(4));
                Assert.That(store.Load(out _).Medals,Is.EqualTo(300));
            }
            finally{store.FailWrites=false;session.Shutdown();Object.Destroy(go);}
            yield return null;
            if(Directory.Exists(directory))Directory.Delete(directory,true);
        }
        [UnityTest]
        public IEnumerator FailedBeginRetainsObservedTicketButNeverInventsAnUnobservedMatch()
        {
            string directory=Path.Combine(Application.temporaryCachePath,"progression-begin-fault-"+Guid.NewGuid().ToString("N"));
            string path=Path.Combine(directory,"profile.save");var go=new GameObject("Initial write failure review");
            var session=go.AddComponent<SessionCoordinator>();session.Initialize(GameContent.CreateDefault());
            var progress=go.AddComponent<LocalProgress>();progress.Initialize(session,path);
            var store=new FaultStore(path){FailWrites=true};
            typeof(LocalProgress).GetField("store",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(progress,store);
            try
            {
                session.StartOffline(8);Observe(progress);Assert.That(progress.Profile.ActiveCounter,Is.Zero);
                var sim=Simulation(session);sim.State.Phase=MatchPhase.Playing;sim.State.WavePhase=WavePhase.Active;sim.State.Players[1].Hp=0;sim.Step();
                store.FailWrites=false;Observe(progress);Observe(progress);
                Assert.That(progress.Profile.Medals,Is.EqualTo(100));Assert.That(store.Load(out _).SettledCounter,Is.EqualTo(1));
            }
            finally{store.FailWrites=false;session.Shutdown();Object.Destroy(go);}
            yield return null;
            if(Directory.Exists(directory))Directory.Delete(directory,true);
        }

        [UnityTest]
        public IEnumerator UnboundFinishedSnapshotCannotCreateRewardTicket()
        {
            string directory=Path.Combine(Application.temporaryCachePath,"progression-test-"+Guid.NewGuid().ToString("N"));
            var go=new GameObject("Unbound result review");var session=go.AddComponent<SessionCoordinator>();session.Initialize(GameContent.CreateDefault());
            session.StartOffline(3);var simulation=Simulation(session);
            simulation.State.Phase=MatchPhase.Playing;simulation.State.WavePhase=WavePhase.Active;simulation.State.Players[1].Hp=0;simulation.Step();
            var progress=go.AddComponent<LocalProgress>();progress.Initialize(session,Path.Combine(directory,"profile.save"));
            try {Observe(progress);Assert.That(progress.Profile.Medals,Is.Zero);Assert.That(progress.Profile.MatchCounter,Is.Zero);}
            finally{session.Shutdown();Object.Destroy(go);if(Directory.Exists(directory))Directory.Delete(directory,true);}
            yield return null;
        }
    }
}
