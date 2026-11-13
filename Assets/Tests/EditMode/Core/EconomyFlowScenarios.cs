using System;
using System.Collections.Generic;
using System.Numerics;
using DefenseDuell.Core;

namespace DefenseDuell.Tests
{
    public static class EconomyFlowScenarios
    {
        static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
        public static IEnumerable<KeyValuePair<string,Action>> All()
        {
            yield return new KeyValuePair<string,Action>(nameof(ActiveEconomyDefaultsAndHashes),ActiveEconomyDefaultsAndHashes);
            yield return new KeyValuePair<string,Action>(nameof(MineRefundUsesPaidInvestmentWithoutArbitrage),MineRefundUsesPaidInvestmentWithoutArbitrage);
            yield return new KeyValuePair<string,Action>(nameof(ForecastDoesNotPurchaseOrMutate),ForecastDoesNotPurchaseOrMutate);
            yield return new KeyValuePair<string,Action>(nameof(ForecastBoundsActualFifoDeliveryAtBothSpeeds),ForecastBoundsActualFifoDeliveryAtBothSpeeds);
            yield return new KeyValuePair<string,Action>(nameof(PartialSwarmRefundAndCooldownForecast),PartialSwarmRefundAndCooldownForecast);
            yield return new KeyValuePair<string,Action>(nameof(FutureAndFinalWaveForecastRemainHonest),FutureAndFinalWaveForecastRemainHonest);
            yield return new KeyValuePair<string,Action>(nameof(LongerSendWindowKeepsFiniteCutoff),LongerSendWindowKeepsFiniteCutoff);
            yield return new KeyValuePair<string,Action>(nameof(ActiveEconomyFullMatchAndRematchReset),ActiveEconomyFullMatchAndRematchReset);
        }
        static CommandResult Command(MatchSimulation sim,PlayerSlot slot,CommandType type,int kind=0,int target=-1,MapPoint point=default)
        {
            var c=new CommandEnvelope{SessionId=sim.State.SessionId,Sequence=sim.LastSequence(slot)+1,Type=type,Kind=kind,Target=target,PositionX=point.X,PositionY=point.Y};
            if(type==CommandType.SetReady){c.ExpectedConfigRevision=sim.State.ConfigRevision;c.ExpectedRosterRevision=sim.State.RosterRevision;}
            if(type==CommandType.SendUnit){var q=SendRules.Quote(sim.Content,sim.State,slot,kind);c.ExpectedWave=q.WaveId;c.EnemyTarget=q.EnemyTarget;c.ExpectedResearch=q.ResearchLevel;}
            if(type==CommandType.ResearchUnit)c.ExpectedResearch=sim.State.Players[(int)slot].ResearchLevels[kind];
            return sim.Execute(slot,c);
        }
        static MatchSimulation Start(bool active=true,int hp=10000)
        {
            var sim=new MatchSimulation(GameContent.CreateDefault(new GameRules(startGold:10000,baseHp:hp)),17,12);
            Check(Command(sim,PlayerSlot.Zero,CommandType.SetReady).Accepted,"ready0");
            Check(Command(sim,PlayerSlot.One,CommandType.SetReady).Accepted,"ready1");
            while(sim.State.Phase!=MatchPhase.Playing)sim.Step();
            if(active)
            {
                Command(sim,PlayerSlot.Zero,CommandType.SetWaveReady,1,1);Command(sim,PlayerSlot.One,CommandType.SetWaveReady,1,1);sim.Step();
            }
            return sim;
        }
        public static void ActiveEconomyDefaultsAndHashes()
        {
            var c=GameContent.CreateDefault();Check(c.Towers[4].Price==160&&c.Rules.MineSellRefundPercent==40,"mine defaults");
            Check(c.SellRefundPercent==65&&c.WaveRules.MinimumSendWindowTicks==600,"refund/window defaults");
            int[] wanted={6,8,12,10};for(int k=0;k<4;k++)Check(c.Units[k].CooldownTicks==wanted[k],"cadence "+k);
            Check(c.ContentHash!=GameContent.CreateDefault(new GameRules(mineSellRefundPercent:41)).ContentHash,"mine refund missing from content hash");
            foreach(int invalid in new[]{-1,101})
            {bool threw=false;try{GameContent.CreateDefault(new GameRules(mineSellRefundPercent:invalid));}catch(ArgumentException){threw=true;}Check(threw,"invalid mine refund accepted");}
        }
        public static void MineRefundUsesPaidInvestmentWithoutArbitrage()
        {
            var sim=Start(false);MapPoint point=default;bool found=false;
            for(int y=30;y<930&&!found;y+=60)for(int x=30;x<1570&&!found;x+=60)
            {var p=new MapPoint(x,y);if(PlacementValidator.ValidateBuild(sim.Content,sim.State,PlayerSlot.Zero,4,-1,p,out _)==ErrorCode.None){point=p;found=true;}}
            Check(found&&Command(sim,PlayerSlot.Zero,CommandType.BuildTower,4,-1,point).Accepted,"mine build");
            var t=sim.State.Towers[0];Check(sim.RefundFor(t)==64,"base mine refund");
            Check(Command(sim,PlayerSlot.Zero,CommandType.UpgradeTower,(int)TowerUpgradePath.Income,t.Id).Accepted,"mine upgrade");
            Check(t.Invested==172&&EconomyRules.RefundGold(sim.Content,t)==68,"upgraded mine refund");
            var gold=sim.State.Players[0].Gold;Check(Command(sim,PlayerSlot.Zero,CommandType.SellTower,0,t.Id).Accepted,"mine sale");
            Check(sim.State.Players[0].Gold==gold+68&&sim.State.Players[0].Gold<10000,"sale created money");
            Check(!Command(sim,PlayerSlot.Zero,CommandType.SellTower,0,t.Id).Accepted,"sold twice");
            t.Invested=BigInteger.Pow(10,100)+17;Check(EconomyRules.RefundGold(sim.Content,t)==t.Invested*40/100,"large refund lost precision");
            t.Kind=0;Check(EconomyRules.RefundGold(sim.Content,t)==t.Invested*65/100,"combat refund changed");
        }
        public static void ForecastDoesNotPurchaseOrMutate()
        {
            var sim=Start(false);var gold=sim.State.Players[0].Gold;var seq=sim.LastSequence(PlayerSlot.Zero);
            var q=SendQueueForecast.Estimate(sim.Content,sim.State,PlayerSlot.Zero,2);
            Check(q.IncludesAdditionalPurchase&&q.Packets==1&&q.RemainingUnits==1&&q.RemainingPaidQuarterGold==260,"purchase preview");
            Check(q.WaitsForPreparation&&!q.HasUnknownWaveWait,"preparation flags");
            Check(sim.State.SpawnOrders.Count==0&&sim.State.Players[0].Gold==gold&&sim.LastSequence(PlayerSlot.Zero)==seq,"forecast mutated purchase state");
            for(int i=0;i<5;i++)Check(Command(sim,PlayerSlot.Zero,CommandType.SendUnit,2).Accepted,"preview backlog");
            q=SendQueueForecast.Estimate(sim.Content,sim.State,PlayerSlot.Zero,3);
            Check(q.FirstSpawnDelayTicks==1&&q.AdditionalFirstSpawnDelayTicks==1+4*sim.Content.Units[2].CooldownTicks+sim.Content.Rules.SpawnIntervalTicks&&q.MinimumDelayTicks==1+4*sim.Content.Units[2].CooldownTicks+4*sim.Content.Rules.SpawnIntervalTicks,"new packet first spawn confused with queue head or last body");
            Check(q.AdditionalWave==1&&q.Packets==6&&sim.State.SpawnOrders.Count==5,"hypothetical packet appended into real state");
            sim.State.Players[0].Gold=0;q=SendQueueForecast.Estimate(sim.Content,sim.State,PlayerSlot.Zero,3);
            Check(q.IncludesAdditionalPurchase&&q.RemainingUnits==9&&sim.State.Players[0].Gold==0,"unaffordable preview");
        }
        public static void ForecastBoundsActualFifoDeliveryAtBothSpeeds()
        {
            foreach(bool neutral in new[]{false,true})foreach(bool speed2 in new[]{false,true})
            {
                var sim=Start();if(!neutral)sim.State.NeutralCursor[0]=sim.State.NeutralCursor[1]=sim.Content.Waves[0].TotalCount;
                for(int n=0;n<10;n++)Check(Command(sim,PlayerSlot.Zero,CommandType.SendUnit,2).Accepted,"ten tank queue");
                if(speed2){Command(sim,PlayerSlot.Zero,CommandType.SetSpeedVote,1);Command(sim,PlayerSlot.One,CommandType.SetSpeedVote,1);}
                var q=SendQueueForecast.Estimate(sim.Content,sim.State,PlayerSlot.Zero);
                Check(q.MinimumDelayTicks==1+9*sim.Content.Units[2].CooldownTicks&&q.RemainingPaidQuarterGold==2600,"ten tank cadence lower bound");
                int start=sim.State.PlayTicks,steps=0;while(sim.State.SpawnOrders.Count>0&&steps++<1000)sim.Step();
                Check(sim.State.SpawnOrders.Count==0&&sim.State.PlayTicks-start>=q.MinimumDelayTicks,"forecast promised later than actual delivery");
                if(!neutral)Check(sim.State.PlayTicks-start<=q.MinimumDelayTicks+1,"uncontended cadence changed");
            }
        }
        public static void PartialSwarmRefundAndCooldownForecast()
        {
            var sim=Start();sim.State.NeutralCursor[0]=sim.State.NeutralCursor[1]=sim.Content.Waves[0].TotalCount;
            Check(Command(sim,PlayerSlot.Zero,CommandType.SendUnit,3).Accepted,"swarm buy");sim.Step();
            Check(sim.State.SpawnOrders[0].Remaining==3,"first swarm body");
            var q=SendQueueForecast.Estimate(sim.Content,sim.State,PlayerSlot.Zero);
            Check(q.RemainingUnits==3&&q.RemainingPaidQuarterGold==114,"partial packet paid remainder");
            Check(q.MinimumDelayTicks==3*sim.Content.Rules.SpawnIntervalTicks,"partial packet incorrectly waits whole-packet cooldown");
            int before=sim.State.PlayTicks;while(sim.State.SpawnOrders.Count>0)sim.Step();Check(sim.State.PlayTicks-before==3*sim.Content.Rules.SpawnIntervalTicks,"partial actual drain");
        }
        public static void FutureAndFinalWaveForecastRemainHonest()
        {
            var sim=Start();sim.State.WavePhase=WavePhase.Clearing;
            var q=SendQueueForecast.Estimate(sim.Content,sim.State,PlayerSlot.Zero,0);
            Check(q.FirstWave==2&&q.LastWave==2&&q.HasUnknownWaveWait,"next wave wait hidden");
            sim.State.WaveId=sim.Content.WaveCount;q=SendQueueForecast.Estimate(sim.Content,sim.State,PlayerSlot.Zero,0);
            Check(!q.IncludesAdditionalPurchase&&q.Packets==0,"final clearing advertises another purchase");
            var gold=sim.State.Players[0].Gold;Check(Command(sim,PlayerSlot.Zero,CommandType.ResearchUnit,0).Code==ErrorCode.WaveCapacity,"useless final research accepted");
            Check(sim.State.Players[0].Gold==gold,"rejected final research charged");
        }
        public static void LongerSendWindowKeepsFiniteCutoff()
        {
            var sim=Start();while(sim.State.PlayTicks<599)sim.Step();
            Check(sim.State.WavePhase==WavePhase.Active&&SendRules.Quote(sim.Content,sim.State,PlayerSlot.Zero,2).WaveId==1,"window closes before thirty seconds");
            Check(Command(sim,PlayerSlot.Zero,CommandType.SendUnit,2).Accepted,"late valid current-wave purchase");sim.Step();
            Check(sim.State.WavePhase==WavePhase.Clearing&&SendRules.Quote(sim.Content,sim.State,PlayerSlot.Zero,2).WaveId==2,"window does not close finitely");
        }
        public static void ActiveEconomyFullMatchAndRematchReset()
        {
            var sim=Start(true,30);var contentHash=sim.Content.ContentHash;
            for(int i=0;i<20;i++)Check(Command(sim,PlayerSlot.Zero,CommandType.SendUnit,2).Accepted,"endgame packet");
            int steps=0;while(sim.State.Phase==MatchPhase.Playing&&steps++<10000)sim.Step();
            Check(sim.State.Phase==MatchPhase.Finished,"endgame stalled");var epoch=sim.State.SessionId;
            Check(Command(sim,PlayerSlot.Zero,CommandType.RequestRematch).Accepted&&Command(sim,PlayerSlot.One,CommandType.RequestRematch).Accepted,"rematch requests");
            Check(sim.State.SessionId!=epoch&&sim.State.Phase==MatchPhase.Lobby,"rematch session");
            Check(sim.State.SpawnOrders.Count==0&&sim.State.Towers.Count==0&&sim.State.Players[0].Gold==0&&sim.State.Players[1].Gold==0&&!sim.State.StartGoldGranted,"rematch retains spending/queue or pays before ready");
            Check(sim.Content.ContentHash==contentHash&&sim.Content.Rules.MineSellRefundPercent==40,"rematch rules changed");
            Check(SendQueueForecast.Estimate(sim.Content,sim.State,PlayerSlot.Zero).Packets==0,"stale lobby forecast");
            Check(Command(sim,PlayerSlot.Zero,CommandType.SetReady).Accepted,"first rematch ready");
            Check(sim.State.Players[0].Gold==0&&sim.State.Players[1].Gold==0&&!sim.State.StartGoldGranted,"one ready paid start gold");
            Check(Command(sim,PlayerSlot.One,CommandType.SetReady).Accepted,"final rematch ready");
            Check(sim.State.StartGoldGranted&&sim.State.Players[0].Gold==sim.Content.MatchStartGold&&sim.State.Players[1].Gold==sim.Content.MatchStartGold,"rematch start credit not exact");
            Check(Command(sim,PlayerSlot.One,CommandType.SetReady).Code==ErrorCode.WrongPhase,"started match accepted another ready");
            Check(sim.State.Players[0].Gold==sim.Content.MatchStartGold&&sim.State.Players[1].Gold==sim.Content.MatchStartGold,"rejected ready credited twice");
        }
    }
}
