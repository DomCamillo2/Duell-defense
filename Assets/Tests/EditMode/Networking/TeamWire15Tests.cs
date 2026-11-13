using System;
using System.Numerics;
using DefenseDuell.Core;
using NUnit.Framework;
namespace DefenseDuell.Networking.Tests
{
    public sealed class TeamWire15Tests
    {
        static MatchSimulation Create(MatchFormat format,bool endless=false)=>new MatchSimulation(GameContent.CreateDefault().WithMatchRules(new MatchRules(1,1000,endless,3,format)),7,123);
        static MatchState Copy(MatchSimulation sim)
        {
            byte[] body=WireProtocol.EncodeSnapshot(sim.State);
            Assert.That(WireProtocol.TryDecodeSnapshot(body,sim.Content.BaseContent,out var copy),Is.True,"valid production snapshot");
            Assert.That(WireProtocol.EncodeSnapshot(copy),Is.EqualTo(body));return copy;
        }
        static void Start(MatchSimulation sim)
        {for(int p=0;p<sim.State.Players.Length;p++)Assert.That(WireFixtures.Execute(sim,(PlayerSlot)p,CommandType.SetReady).Accepted,Is.True);for(int t=0;t<sim.Content.CountdownTicks;t++)sim.Step();}
        [Test] public void AllFormatsRoundTripActualPlayerAndSharedTeamShapes()
        {
            foreach(MatchFormat format in Enum.GetValues(typeof(MatchFormat)))
            {
                var sim=Create(format);var lobby=Copy(sim);Assert.That(lobby.Players.Length,Is.EqualTo(sim.Content.MatchRules.PlayerCount));
                Assert.That(lobby.Teams.Length,Is.EqualTo(sim.Content.MatchRules.TeamCount));Start(sim);var state=Copy(sim);
                foreach(var player in state.Players)Assert.That(player.Gold,Is.EqualTo(new BigInteger(1000)));
                state.Teams[0].Hp-=3;foreach(var player in state.Players)if(player.TeamId==0)Assert.That(player.Hp,Is.EqualTo(state.Teams[0].Hp));
                state.Players[0].Hp--;Assert.That(state.Teams[0].Hp,Is.EqualTo(sim.Content.BaseHp*sim.Content.MatchRules.MembersPerTeam-4));
            }
        }
        [Test] public void WelcomeBindsEveryRecipientAndHostMapBeforeDecode()
        {
            var catalog=MapCatalog.CreateContents(GameContent.CreateDefault());var hash=WireProtocol.CatalogHash(catalog);
            for(int map=0;map<catalog.Length;map++)
            {
                var sim=new MatchSimulation(catalog[map].WithMatchRules(new MatchRules(200,BigInteger.Pow(10,35),true,10,MatchFormat.ThreeVsThree,catalog[map].Map.Id)));
                for(int p=1;p<6;p++)
                {
                    byte[] bytes=WireProtocol.EncodeWelcome(hash,(byte)map,(uint)p,sim.State,sim.Content,2,0,(PlayerSlot)p);
                    Assert.That(WireProtocol.TryDecodeWelcome(bytes,hash,catalog,out var selected,out _,out _,out _,out var recipient,out var effective,out var state,out _),Is.True);
                    Assert.That(selected,Is.EqualTo(map));Assert.That((int)recipient,Is.EqualTo(p));Assert.That(effective.MatchRules,Is.EqualTo(sim.Content.MatchRules));Assert.That(state.Players.Length,Is.EqualTo(6));
                }
            }
        }
        [Test] public void SendTargetSenderAndCancelledLedgerRemainExactAcrossCodec()
        {
            var sim=Create(MatchFormat.TwoVsTwoVsTwo);Start(sim);
            for(int p=0;p<6;p++)Assert.That(WireFixtures.Execute(sim,(PlayerSlot)p,CommandType.SendUnit,0).Accepted,Is.True);
            var state=Copy(sim);Assert.That(state.SpawnOrders.Count,Is.EqualTo(6));
            for(int p=0;p<6;p++){var order=state.SpawnOrders[p];Assert.That((int)order.Sender,Is.EqualTo(p));Assert.That(order.EnemyTarget,Is.EqualTo(sim.State.Players[p].AttackTarget));}
            for(int p=0;p<6;p++)WireFixtures.Execute(sim,(PlayerSlot)p,CommandType.SetWaveReady,1);
            for(int t=0;t<60;t++){sim.Step();if(t%3==0)Copy(sim);}
            bool paid=false;foreach(var unit in Copy(sim).Units)if(unit.Origin==UnitOrigin.PaidPvP){paid=true;Assert.That(sim.State.Players[(int)unit.Sender].TeamId,Is.Not.EqualTo(sim.State.Players[(int)unit.Defender].TeamId));}
            Assert.That(paid,Is.True);
            var broken=Copy(sim);foreach(var unit in broken.Units)if(unit.Origin==UnitOrigin.PaidPvP){unit.Sender=unit.Defender;break;}
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(broken),sim.Content,out _),Is.False);
        }
        [Test] public void EveryTeamFormatNaturalFinishAndUnanimousRematchRoundTrip()
        {
            foreach(var format in new[]{MatchFormat.TwoVsTwo,MatchFormat.ThreeVsThree,MatchFormat.TwoVsTwoVsTwo})
            {
                var sim=Create(format);Start(sim);int frames=0;
                while(sim.State.Phase!=MatchPhase.Finished&&sim.State.Tick<20000){sim.Step();if(sim.State.Tick%31==0){Copy(sim);frames++;}}
                Assert.That(sim.State.Phase,Is.EqualTo(MatchPhase.Finished));Assert.That(frames,Is.GreaterThan(1));var old=Copy(sim);int count=old.Players.Length;
                for(int p=0;p<count-1;p++){Assert.That(WireFixtures.Execute(sim,(PlayerSlot)p,CommandType.RequestRematch).Accepted,Is.True);Assert.That(sim.State.SessionId,Is.EqualTo(old.SessionId));Copy(sim);}
                Assert.That(WireFixtures.Execute(sim,(PlayerSlot)(count-1),CommandType.RequestRematch).Accepted,Is.True);var next=Copy(sim);
                Assert.That(next.SessionId,Is.EqualTo(old.SessionId+1));Assert.That(next.MatchRules,Is.EqualTo(old.MatchRules));Assert.That(next.StartGoldGranted,Is.False);
                foreach(var player in next.Players)Assert.That(player.Gold,Is.EqualTo(BigInteger.Zero));
            }
        }
        [Test] public void ArrangedBeyondInt64EndlessWaveUsesTwoExactLedgerEntries()
        {
            // Arranged serialization boundary, not a claim of playing this many waves.
            var sim=Create(MatchFormat.ThreeVsThree,true);Start(sim);BigInteger wave=BigInteger.Pow(10,40)+201;sim.State.WaveId=wave;
            foreach(var p in sim.State.Players){p.ReservedGoldByWave=new WaveLedger(wave,new BigInteger[2],true);p.SpawnedQuarterGoldByWave=new WaveLedger(wave,new BigInteger[2],true);p.CancelledQuarterGoldByWave=new WaveLedger(wave,new BigInteger[2],true);}
            var state=Copy(sim);Assert.That(state.WaveId,Is.EqualTo(wave));foreach(var p in state.Players)Assert.That(p.ReservedGoldByWave.Length,Is.EqualTo(2));
            var command=new CommandEnvelope{SessionId=sim.State.SessionId,Sequence=22,Type=CommandType.SendUnit,Kind=0,Target=-1,EnemyTarget=3,ExpectedWave=wave};
            Assert.That(WireProtocol.TryDecodeCommand(WireProtocol.EncodeCommand(command),out var decoded),Is.True);Assert.That(decoded.ExpectedWave,Is.EqualTo(wave));Assert.That(decoded.EnemyTarget,Is.EqualTo(3));
            state.Players[5].CancelledQuarterGoldByWave=new WaveLedger(wave+1,new BigInteger[2],true);
            Assert.That(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(state),sim.Content,out _),Is.False);
        }
        [Test] public void RosterAndRulesCannotChangeAtUnchangedPublicationTokens()
        {
            var sim=Create(MatchFormat.ThreeVsThree);var gate=new StatePublicationGate();Assert.That(gate.TryAccept(Copy(sim),1,0,0,5),Is.True);
            var invalid=Copy(sim);invalid.Players[5].AvailableTowerMask=128;Assert.That(gate.TryAccept(invalid,2,0,0,5),Is.False);
            invalid=Copy(sim);invalid.Players[5].TeamId=0;Assert.That(gate.TryAccept(invalid,2,0,0,5),Is.False);
            invalid=Copy(sim);invalid.MatchRules=new MatchRules(1,1000,false,5,MatchFormat.ThreeVsThree,sim.Content.Map.Id);Assert.That(gate.TryAccept(invalid,2,0,0,5),Is.False);
        }
    }
}
