using System;
using System.Numerics;
using System.Collections.Generic;
using System.Text;
using DefenseDuell.Core;

namespace DefenseDuell.Tests
{
    public static class WaveCoreScenarios
    {
        static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static void Equal(BigInteger actual, BigInteger expected, string message) => Check(actual == expected, message + " actual=" + actual + " expected=" + expected);
        static void Equal<T>(T a,T b,string message){Check(EqualityComparer<T>.Default.Equals(a,b),message+" actual="+a+" expected="+b);}
        static KeyValuePair<string,Action> Case(string name,Action action)=>new KeyValuePair<string,Action>(name,action);
        public static IEnumerable<KeyValuePair<string,Action>> All()
        {
            yield return Case(nameof(DefinitionsBindPlansRecipesAndFractionalIncome),DefinitionsBindPlansRecipesAndFractionalIncome);
            yield return Case(nameof(ControlClockFreezesGameplayDuringPreparation),ControlClockFreezesGameplayDuringPreparation);
            yield return Case(nameof(PreparationReadyAndSpeedAreEpochBound),PreparationReadyAndSpeedAreEpochBound);
            yield return Case(nameof(QuotesReserveUnlimitedCurrentWavePackets),QuotesReserveUnlimitedCurrentWavePackets);
            yield return Case(nameof(QuoteRacesAndReplayAreAtomic),QuoteRacesAndReplayAreAtomic);
            yield return Case(nameof(ResearchPricesAndBoundPacketsAreExact),ResearchPricesAndBoundPacketsAreExact);
            yield return Case(nameof(FractionalIncomeCreditsOnlyActualPaidSpawns),FractionalIncomeCreditsOnlyActualPaidSpawns);
            yield return Case(nameof(SourceFairnessContinuesAboveLegacyFieldAndOriginCaps),SourceFairnessContinuesAboveLegacyFieldAndOriginCaps);
            yield return Case(nameof(WaveCutoffClearingAndFinalOutcomeAreFinite),WaveCutoffClearingAndFinalOutcomeAreFinite);
            yield return Case(nameof(ResearchAndPriorityRequestsRejectInvalidPayloads),ResearchAndPriorityRequestsRejectInvalidPayloads);
            yield return Case(nameof(FrontAndStrongestUseCurrentHpAndPreserveCooldown),FrontAndStrongestUseCurrentHpAndPreserveCooldown);
            yield return Case(nameof(UnlockMasksAreLobbyOnlyAndPreservedOnRematch),UnlockMasksAreLobbyOnlyAndPreservedOnRematch);
            yield return Case(nameof(ChainAndLineHitCapsGeometryArmorAndTieBreaks),ChainAndLineHitCapsGeometryArmorAndTieBreaks);
            yield return Case(nameof(ActiveSpeedRunsAllGameplayRulesTogether),ActiveSpeedRunsAllGameplayRulesTogether);
            yield return Case(nameof(LedgerRejectsForgedFutureFractionsAndCapsIncome),LedgerRejectsForgedFutureFractionsAndCapsIncome);
            yield return Case(nameof(EveryResearchLevelAndMalformedVoteAreAtomic),EveryResearchLevelAndMalformedVoteAreAtomic);
            yield return Case(nameof(AdditionalWeaponsRespectRangeAndCooldown),AdditionalWeaponsRespectRangeAndCooldown);
        }
        static GameContent Content(int waves=10,int gold=10000,int hp=30,int window=240,int budget=240,int neutralInterval=10)
        {
            var d=GameContent.CreateDefault();var plans=new WaveDefinition[waves];
            for(int i=0;i<waves;i++)plans[i]=new WaveDefinition(new[]{1,0,0,0},100+i*12);
            var map=new MapDefinition(new[]{new MapPoint(0,100),new MapPoint(300,100),new MapPoint(300,200),new MapPoint(1000,200)},
                new[]{new MapPoint(100,300),new MapPoint(200,400),new MapPoint(400,400),new MapPoint(600,400)});
            return new GameContent(new GameRules(startGold:gold,baseHp:hp,maxCommandsPerSecond:100),d.Towers,d.Units,map,plans,d.Research,
                new WaveRules(waveBudget:budget,minimumSendWindowTicks:window,neutralSpawnIntervalTicks:neutralInterval));
        }
        public static CommandResult Command(MatchSimulation sim,PlayerSlot player,CommandType type,int kind=0,int target=-1,int x=0,int y=0)
        {
            var c=new CommandEnvelope{SessionId=sim.State.SessionId,Sequence=sim.LastSequence(player)+1,Type=type,Kind=kind,Target=target,PositionX=x,PositionY=y};
            if(type==CommandType.SetReady){c.ExpectedConfigRevision=sim.State.ConfigRevision;c.ExpectedRosterRevision=sim.State.RosterRevision;}
            if(type==CommandType.SendUnit){var q=SendRules.Quote(sim.Content,sim.State,player,kind);c.ExpectedWave=q.WaveId;c.EnemyTarget=q.EnemyTarget;c.ExpectedResearch=q.ResearchLevel;}
            if(type==CommandType.ResearchUnit){ c.ExpectedResearch=target; c.Target=-1; }
            return sim.Execute(player,c);
        }
        static MatchSimulation Preparing(GameContent content=null)
        {
            var sim=new MatchSimulation(content??Content());
            Check(Command(sim,PlayerSlot.Zero,CommandType.SetReady).Accepted,"ready0");
            Check(Command(sim,PlayerSlot.One,CommandType.SetReady).Accepted,"ready1");
            while(sim.State.Phase==MatchPhase.Countdown)sim.Step();return sim;
        }
        static MatchSimulation Active(GameContent content=null,bool clearNeutral=true)
        {
            var sim=Preparing(content);StartWave(sim);
            if(clearNeutral)sim.State.NeutralCursor[0]=sim.State.NeutralCursor[1]=sim.Content.Waves[0].TotalCount;
            return sim;
        }
        static void StartWave(MatchSimulation sim)
        {
            Check(Command(sim,PlayerSlot.Zero,CommandType.SetWaveReady,1,checked((int)sim.State.WaveId)).Accepted,"wave ready0");
            Check(Command(sim,PlayerSlot.One,CommandType.SetWaveReady,1,checked((int)sim.State.WaveId)).Accepted,"wave ready1");sim.Step();
        }
        static void Steps(MatchSimulation sim,int n){for(int i=0;i<n;i++)sim.Step();}
        static CommandEnvelope SendIntent(MatchSimulation sim,int kind=3)
        {
            var q=SendRules.Quote(sim.Content,sim.State,PlayerSlot.Zero,kind);
            return new CommandEnvelope{SessionId=sim.State.SessionId,Sequence=sim.LastSequence(PlayerSlot.Zero)+1,Type=CommandType.SendUnit,
                Kind=kind,Target=-1,ExpectedWave=q.WaveId,EnemyTarget=q.EnemyTarget,ExpectedResearch=q.ResearchLevel};
        }
        public static string Fingerprint(MatchState s,bool clocks=true)
        {
            var b=new StringBuilder();b.Append(s.SessionId).Append('/').Append(s.Seed).Append('/').Append(s.Phase).Append('/').Append(s.Outcome).Append('/').Append(s.CountdownRemaining);
            b.Append("|rules=").Append(s.MatchRules.WaveCount).Append(',').Append(s.MatchRules.StartGold)
                .Append(',').Append(s.ConfigRevision).Append(',').Append(s.RosterRevision).Append(',').Append(s.ConnectedPlayerMask).Append(',').Append(s.StartGoldGranted)
                .Append(',').Append(s.MatchRules.Endless).Append(',').Append(s.MatchRules.IncomeMultiplier).Append(',').Append(s.MatchRules.Format).Append(',').Append(s.MatchRules.MapId).Append(',').Append(s.WinningTeamId);
            if(s.Teams!=null)foreach(var team in s.Teams)b.Append("|teamhp=").Append(team.Hp);
            if(clocks)b.Append('/').Append(s.Tick);
            b.Append('/').Append(s.PlayTicks).Append('/').Append(s.WaveId).Append('/').Append(s.WavePhase).Append('/').Append(s.PreparationRemaining).Append('/').Append(s.WaveStartPlayTick);
            foreach(var p in s.Players)
            {
                b.Append('|').Append(p.Gold).Append(',').Append(p.Hp).Append(',').Append(p.AttackIncomeUnits).Append(',').Append(p.GoldRemainder).Append(',').Append(p.WaveReady).Append(',').Append(p.Ready).Append(',').Append(p.Rematch).Append(',').Append(p.AvailableTowerMask);
                if(clocks)b.Append(',').Append(p.SpeedReady);
                foreach(var v in p.ResearchLevels)b.Append(',').Append(v);
                foreach(int v in p.NextSendTicks)b.Append(',').Append(v);
                foreach(var v in p.ReservedGoldByWave)b.Append(',').Append(v);
                foreach(var v in p.SpawnedQuarterGoldByWave)b.Append(',').Append(v);
                foreach(var v in p.CancelledQuarterGoldByWave)b.Append(',').Append(v);
                b.Append("|team/target/archive=").Append(p.TeamId).Append(',').Append(p.AttackTarget).Append(',').Append(p.ArchivedSpawnedQuarterGold);
            }
            for(int p=0;p<s.Players.Length;p++)b.Append('|').Append(s.NeutralCursor[p]).Append(',').Append(s.NextSpawnTicks[p]).Append(',').Append(s.NextSpawnOrigins[p]).Append(',').Append(s.NextPaidSender[p]).Append(',').Append(s.NextPaidSpawnTicks[p]);
            foreach(var t in s.Towers)b.Append("|T").Append(t.Id).Append(',').Append(t.Owner).Append(',').Append(t.Kind).Append(',').Append(t.Level).Append(',').Append(t.DamageLevel).Append(',').Append(t.RangeLevel).Append(',').Append(t.CooldownTicks).Append(',').Append(t.Invested).Append(',').Append(t.Priority).Append(',').Append(t.Position.X).Append(',').Append(t.Position.Y).Append(',').Append(t.Slot).Append(',').Append(t.SiteId);
            foreach(var u in s.Units)b.Append("|U").Append(u.Id).Append(',').Append(u.Defender).Append(',').Append(u.Kind).Append(',').Append(u.Hp).Append(',').Append(u.Distance).Append(',').Append(u.SlowPermille).Append(',').Append(u.SlowUntilTick).Append(',').Append(u.MovementRemainder).Append(',').Append(u.Origin).Append(',').Append(u.WaveId).Append(',').Append(u.ResearchLevel);
            foreach(var q in s.SpawnOrders)b.Append("|Q").Append(q.Sender).Append(',').Append(q.Kind).Append(',').Append(q.Remaining).Append(',').Append(q.InitialCount).Append(',').Append(q.PaidPrice).Append(',').Append(q.WaveId).Append(',').Append(q.ResearchLevel).Append(',').Append(q.NextTick).Append(',').Append(q.Origin);
            return b.ToString();
        }
        public static void DefinitionsBindPlansRecipesAndFractionalIncome()
        {
            var d=GameContent.CreateDefault();Equal(d.WaveCount,10,"wave count");Equal(d.Towers.Length,8,"eight classes");
            int[] totals={10,16,20,24,28,32,36,40,44,48},costs={288,356,808,492};
            for(int w=0;w<10;w++){Equal(d.Waves[w].TotalCount,totals[w],"neutral body count");Equal(d.Waves[w].HpPercent,100+12*w,"neutral HP curve");}
            var copied=d.Waves[0].Counts;copied[0]=0;Equal(d.Waves[0].Counts[0],10,"mutable counts escaped");
            var hits=d.Towers[5].DamagePermilleByHit;hits[1]=1;Equal(d.Towers[5].DamagePermilleByHit[1],650,"mutable hit curve escaped");
            for(int k=0;k<4;k++)
            {
                var p=new PlayerState();BigInteger cost=0;
                for(int level=0;level<8;level++){p.ResearchLevels[k]=level;var q=UnitResearchRules.Quote(d,p,k);Check(q.IsValid,"research quote");cost+=q.Price;}
                Equal(cost,costs[k],"research total");p.ResearchLevels[k]=8;Check(UnitResearchRules.Quote(d,p,k).IsValid,"research stopped at eight");
            }
            var units=new[]{960,1056,1176};for(int l=0;l<3;l++)Equal(TowerUpgradeRules.Stats(d,new TowerState{Kind=4,Level=l}).IncomeUnitsPerSecond,units[l],"mine units");
            var waves=d.Waves;waves[0]=new WaveDefinition(new[]{10,0,0,0},101);
            Check(new GameContent(d.Rules,d.Towers,d.Units,d.Map,waves,d.Research,d.WaveRules).ContentHash!=d.ContentHash,"wave omitted from hash");
            var research=d.Research;research[0]=new UnitResearchDefinition(5,2,40,20);
            Check(new GameContent(d.Rules,d.Towers,d.Units,d.Map,d.Waves,research,d.WaveRules).ContentHash!=d.ContentHash,"research omitted from hash");
            research=d.Research;research[0]=new UnitResearchDefinition(4,2,15,4,1,0,3,2);
            Check(new GameContent(d.Rules,d.Towers,d.Units,d.Map,d.Waves,research,d.WaveRules).ContentHash!=d.ContentHash,"research HP quadratic coefficient omitted from hash");
            int[] dp=new int[241];for(int v=1;v<=240;v++){dp[v]=dp[v-1];foreach(var u in d.Units)if(v>=u.Price)dp[v]=Math.Max(dp[v],dp[v-u.Price]+u.Count);}Equal(dp[240],24,"PvP body proof changed");
        }
        public static void ControlClockFreezesGameplayDuringPreparation()
        {
            var sim=Preparing();var s=sim.State;Equal(s.PreparationRemaining,200,"initial prep");Equal(s.PlayTicks,0,"countdown game time");
            s.Players[0].GoldRemainder=479;s.Players[0].NextSendTicks[0]=70;
            Check(Command(sim,PlayerSlot.Zero,CommandType.BuildTower,0,0).Accepted,"build in prep");s.Towers[0].CooldownTicks=7;
            Command(sim,PlayerSlot.Zero,CommandType.SetSpeedVote,1);Command(sim,PlayerSlot.One,CommandType.SetSpeedVote,1);
            var gold=s.Players[0].Gold; int tick=s.Tick;Steps(sim,199);
            Equal(s.Tick,tick+199,"control clock stopped");Equal(s.PlayTicks,0,"prep gameplay advanced");Equal(s.SimulationSpeed,1,"prep accelerated");
            Equal(s.Players[0].Gold,gold,"prep paid income");Equal(s.Players[0].GoldRemainder,479,"prep lost fractional income");Equal(s.Towers[0].CooldownTicks,7,"prep reduced cooldown");
            sim.Step();Equal(s.WavePhase,WavePhase.Active,"deadline failed");Equal(s.PlayTicks,0,"transition backfilled gameplay");
            int calls=0;sim.Step(_=>calls++);Equal(calls,2,"2x bot frequency");Equal(s.PlayTicks,2,"2x active steps");Equal(s.Tick,tick+201,"2x control clock multiplied");
            s.Units.Clear();s.NeutralCursor[0]=s.NeutralCursor[1]=1;s.WavePhase=WavePhase.Clearing;int play=s.PlayTicks;
            sim.Step();Equal(s.PlayTicks,play+1,"second step ran after wave transition");Equal(s.WaveId,2,"wave did not advance");Equal(s.WavePhase,WavePhase.Preparing,"missing next preparation");
        }
        public static void PreparationReadyAndSpeedAreEpochBound()
        {
            var sim=Preparing();Command(sim,PlayerSlot.Zero,CommandType.SetWaveReady,1,1);sim.Step();Equal(sim.State.WavePhase,WavePhase.Preparing,"one ready started wave");
            Command(sim,PlayerSlot.Zero,CommandType.SetWaveReady,0,1);Command(sim,PlayerSlot.One,CommandType.SetWaveReady,1,1);sim.Step();Equal(sim.State.WavePhase,WavePhase.Preparing,"withdrawal ignored");
            Command(sim,PlayerSlot.Zero,CommandType.SetWaveReady,1,1);Equal(sim.State.WavePhase,WavePhase.Preparing,"command started wave outside control step");sim.Step();Equal(sim.State.WavePhase,WavePhase.Active,"joint ready failed");
            Equal(Command(sim,PlayerSlot.Zero,CommandType.SetWaveReady,1,1).Code,ErrorCode.WrongPhase,"active ready accepted");
            sim.State.WavePhase=WavePhase.Preparing;sim.State.WaveId=2;string before=Fingerprint(sim.State);
            Equal(Command(sim,PlayerSlot.Zero,CommandType.SetWaveReady,1,1).Code,ErrorCode.QuoteChanged,"stale wave readiness accepted");Equal(Fingerprint(sim.State),before,"wrong wave changed state");
            for(int i=0;i<100;i++)Command(sim,PlayerSlot.Zero,CommandType.SetSpeedVote,1);Steps(sim,20);
            Check(Command(sim,PlayerSlot.Zero,CommandType.SetSpeedVote,0).Accepted,"preparation limiter did not recover");
        }
        public static void QuotesReserveUnlimitedCurrentWavePackets()
        {
            var sim=Preparing();
            for(int n=0;n<90;n++)
            { var q=SendRules.Quote(sim.Content,sim.State,PlayerSlot.Zero,3);Equal(q.WaveId,1,"Affordable packet incorrectly deferred by old budget");Check(Command(sim,PlayerSlot.Zero,CommandType.SendUnit,3).Accepted,"Order above legacy budget/queue rejected"); }
            Equal(sim.State.SpawnOrders.Count,90,"packet count");Equal(sim.State.Players[0].Gold,10000-90*38,"exact reservation debit");
            Equal(sim.State.Players[0].ReservedGoldByWave[0],90*38,"unbounded reservation ledger");
            Equal(EconomyRules.ValidateLedger(sim.Content,sim.State,PlayerSlot.Zero),ErrorCode.None,"queued ledger");
            StartWave(sim);sim.State.NeutralCursor[0]=sim.State.NeutralCursor[1]=1;Steps(sim,220);
            Equal(sim.State.Players[0].ReservedGoldByWave[0],90*38,"spawn altered prepaid investment");
            Equal(EconomyRules.ValidateLedger(sim.Content,sim.State,PlayerSlot.Zero),ErrorCode.None,"partial ledger");
            Equal(sim.State.Players[0].SpawnedQuarterGoldByWave[1],0,"future deployment credit");
            sim.State.WavePhase=WavePhase.Clearing;
            Equal(SendRules.Quote(sim.Content,sim.State,PlayerSlot.Zero,0).WaveId,2,"clearing did not bind next wave");
            sim.State.WaveId=10;Equal(SendRules.Quote(sim.Content,sim.State,PlayerSlot.Zero,0).Code,ErrorCode.WaveCapacity,"accepted packet beyond match horizon");
        }
        public static void QuoteRacesAndReplayAreAtomic()
        {
            var sim=Preparing();for(int i=0;i<6;i++)Command(sim,PlayerSlot.Zero,CommandType.SendUnit,3);
            var wrong=SendIntent(sim);wrong.ExpectedWave=2;string before=Fingerprint(sim.State);
            Equal(sim.Execute(PlayerSlot.Zero,wrong).Code,ErrorCode.QuoteChanged,"wave race accepted");Equal(Fingerprint(sim.State),before,"race mutated reservation");
            wrong.ExpectedWave=1;Equal(sim.Execute(PlayerSlot.Zero,wrong).Code,ErrorCode.Duplicate,"changed expectation reused ID");
            var old=SendIntent(sim);Check(Command(sim,PlayerSlot.Zero,CommandType.ResearchUnit,3,0).Accepted,"research race setup");old.Sequence=sim.LastSequence(PlayerSlot.Zero)+1;before=Fingerprint(sim.State);
            Equal(sim.Execute(PlayerSlot.Zero,old).Code,ErrorCode.QuoteChanged,"research race accepted");Equal(Fingerprint(sim.State),before,"research race charged");
            var good=SendIntent(sim);Check(sim.Execute(PlayerSlot.Zero,good).Accepted,"quoted buy");before=Fingerprint(sim.State);Check(sim.Execute(PlayerSlot.Zero,good).Accepted,"receipt replay");Equal(Fingerprint(sim.State),before,"duplicate buy charged or reserved");
            good.ExpectedResearch=0;Equal(sim.Execute(PlayerSlot.Zero,good).Code,ErrorCode.Duplicate,"changed research replay");
        }
        public static void ResearchPricesAndBoundPacketsAreExact()
        {
            var sim=Active();Check(Command(sim,PlayerSlot.Zero,CommandType.SendUnit,3).Accepted,"swarm0");sim.Step();
            var old=sim.State.Units[0];Equal(old.ResearchLevel,0,"first bound level");Equal(sim.State.Players[0].AttackIncomeUnits,38,"first quarter credit");
            Check(Command(sim,PlayerSlot.Zero,CommandType.ResearchUnit,3,0).Accepted,"research1");Check(Command(sim,PlayerSlot.Zero,CommandType.SendUnit,3).Accepted,"swarm1");
            Steps(sim,60);int low=0,high=0;foreach(var unit in sim.State.Units)
            {if(unit.ResearchLevel==0){low++;Equal(unit.Hp,26,"old packet healed");}else {high++;Equal(unit.Hp,29,"new packet missed research");}}
            Equal(low,4,"old swarm packet split");Equal(high,4,"new swarm size");Equal(sim.State.Players[0].AttackIncomeUnits,304,"whole packet sum");Equal(EconomyRules.ValidateLedger(sim.Content,sim.State,PlayerSlot.Zero),ErrorCode.None,"deployed ledger");
            var neutral=UnitResearchRules.Resolve(sim.Content,3,0,UnitOrigin.NeutralWave,2);Equal(neutral.MaxHp,30,"neutral ceil HP");Equal(neutral.KillBounty,0,"neutral bounty");
            Equal(UnitResearchRules.Resolve(sim.Content,2,8,UnitOrigin.PaidPvP,1).MaxHp,764,"tank research recipe");
        }
        public static void FractionalIncomeCreditsOnlyActualPaidSpawns()
        {
            var sim=Active(Content(window:12000));Command(sim,PlayerSlot.Zero,CommandType.BuildTower,4,0);Command(sim,PlayerSlot.Zero,CommandType.UpgradeTower,2,sim.State.Towers[0].Id);
            var gold=sim.State.Players[0].Gold;Steps(sim,20);Equal(sim.State.Players[0].Gold,gold+5,"half income first second");Equal(sim.State.Players[0].GoldRemainder,96,"fractional remainder");
            Steps(sim,20);Equal(sim.State.Players[0].Gold,gold+10,"fractional income second second");Equal(sim.State.Players[0].GoldRemainder,192,"fractional remainder second");
            Command(sim,PlayerSlot.Zero,CommandType.SendUnit,3);Equal(sim.State.Players[0].AttackIncomeUnits,0,"purchase activated bonus");
            sim.State.NextSpawnTicks[1]=60;Steps(sim,20);Equal(sim.State.Players[0].GoldRemainder,288,"new spawn credited before same-tick payout");Equal(sim.State.Players[0].AttackIncomeUnits,38,"actual spawn missing bonus");
            var p=sim.State.Players[0];p.Gold=9999;p.GoldRemainder=959;sim.State.SpawnOrders.Clear();p.ReservedGoldByWave[0]=0;p.SpawnedQuarterGoldByWave[0]=0;p.AttackIncomeUnits=0;sim.State.Units.Clear();
            Steps(sim,20);Equal(p.Gold,10005,"legacy wallet cap truncated income");Equal(p.GoldRemainder,95,"fractional remainder conservation");
            p.Gold=0;Steps(sim,20);Equal(p.Gold,5,"discarded overflow paid later");
            sim.State.Units.Add(new UnitState{Id=999,Kind=2,Hp=0,Defender=PlayerSlot.Zero,Origin=UnitOrigin.NeutralWave,WaveId=1});gold=p.Gold;sim.Step();Equal(p.Gold,gold,"neutral kill paid bounty");
            sim.State.Units.Add(new UnitState{Id=1000,Kind=2,Hp=0,Defender=PlayerSlot.Zero,Origin=UnitOrigin.PaidPvP,WaveId=1});sim.Step();Equal(p.Gold,gold+14,"paid kill bounty missing");sim.Step();Equal(p.Gold,gold+14,"dead unit paid twice");
        }
        public static void SourceFairnessContinuesAboveLegacyFieldAndOriginCaps()
        {
            var d=Content(neutralInterval:1);var waves=d.Waves;waves[0]=new WaveDefinition(new[]{64,0,0,0},100);
            var sim=Active(new GameContent(d.Rules,d.Towers,d.Units,d.Map,waves,d.Research,d.WaveRules),false);
            Command(sim,PlayerSlot.Zero,CommandType.SendUnit,3);for(int n=0;n<4;n++){Steps(sim,n==0?1:sim.Content.Rules.SpawnIntervalTicks);Equal(sim.State.Units[sim.State.Units.Count-1].Origin,n%2==0?UnitOrigin.NeutralWave:UnitOrigin.PaidPvP,"source contention alternation");}
            sim=Active(d,false);Command(sim,PlayerSlot.Zero,CommandType.SendUnit,3);
            for(int n=0;n<80;n++)sim.State.Units.Add(new UnitState{Id=1000+n,Kind=2,Hp=1000,Defender=PlayerSlot.One,Origin=n<64?UnitOrigin.NeutralWave:UnitOrigin.PaidPvP,WaveId=1});
            sim.Step();Equal(sim.State.NeutralCursor[1],1,"neutral origin incorrectly capped at64");
            Steps(sim,sim.Content.Rules.SpawnIntervalTicks);Equal(sim.State.SpawnOrders[0].Remaining,3,"paid source blocked above80 live entities");
            Equal(sim.State.Players[0].NextSendTicks[3],sim.State.PlayTicks+sim.Content.Units[3].CooldownTicks,"first paid spawn did not start cooldown");
            Equal(sim.State.Players[0].AttackIncomeUnits,38,"actual paid spawn did not credit income");
        }
        public static void WaveCutoffClearingAndFinalOutcomeAreFinite()
        {
            var sim=Active();Steps(sim,239);Equal(sim.State.WavePhase,WavePhase.Active,"early cutoff");Equal(sim.State.WaveId,1,"empty wave ended before cutoff");
            sim.Step();Equal(sim.State.WaveId,2,"empty cutoff failed transition");Equal(sim.State.PlayTicks,240,"wrong active cutoff");
            sim=Active(Content(waves:1));sim.State.PlayTicks=239;sim.State.Players[0].Hp=20;sim.State.Players[1].Hp=19;sim.Step();Equal(sim.State.Outcome,MatchOutcome.Player0,"last wave HP result");
            string end=Fingerprint(sim.State);sim.Step();sim.Abort();Equal(Fingerprint(sim.State),end,"finished state changed");
            sim=Active();sim.State.Players[0].Hp=sim.State.Players[1].Hp=1;
            for(int p=0;p<2;p++)sim.State.Units.Add(new UnitState{Id=100+p,Kind=0,Hp=1,Distance=sim.Content.Map.Length-1,Defender=(PlayerSlot)p,Origin=UnitOrigin.NeutralWave,WaveId=1});
            sim.Step();Equal(sim.State.Outcome,MatchOutcome.Draw,"same-step bilateral loss biased by iteration");
            Command(sim,PlayerSlot.Zero,CommandType.RequestRematch);Command(sim,PlayerSlot.One,CommandType.RequestRematch);
            Equal(sim.State.Tick,0,"rematch control clock");Equal(sim.State.PlayTicks,0,"rematch game clock");Equal(sim.State.WaveId,1,"rematch wave");Equal(EconomyRules.ValidateLedger(sim.Content,sim.State,PlayerSlot.Zero),ErrorCode.None,"rematch ledger");
        }
        public static void ResearchAndPriorityRequestsRejectInvalidPayloads()
        {
            var sim=Preparing();string before=Fingerprint(sim.State);
            Equal(Command(sim,PlayerSlot.Zero,CommandType.ResearchUnit,0,1).Code,ErrorCode.QuoteChanged,"stale research expectation");
            Equal(Command(sim,PlayerSlot.Zero,CommandType.ResearchUnit,0,9).Code,ErrorCode.QuoteChanged,"stale high research expectation accepted");Equal(Fingerprint(sim.State),before,"research rejection changed state");
            Command(sim,PlayerSlot.Zero,CommandType.BuildTower,0,0);var tower=sim.State.Towers[0];tower.CooldownTicks=9;
            Check(Command(sim,PlayerSlot.Zero,CommandType.SetTowerPriority,1,tower.Id).Accepted,"priority accepted");Equal(tower.CooldownTicks,9,"priority reset cooldown");
            Equal(Command(sim,PlayerSlot.One,CommandType.SetTowerPriority,0,tower.Id).Code,ErrorCode.NotOwner,"foreign priority");
            Command(sim,PlayerSlot.Zero,CommandType.BuildTower,4,1);Equal(Command(sim,PlayerSlot.Zero,CommandType.SetTowerPriority,1,sim.State.Towers[1].Id).Code,ErrorCode.InvalidKind,"mine priority");
            var c=new CommandEnvelope{SessionId=sim.State.SessionId,Sequence=sim.LastSequence(PlayerSlot.Zero)+1,Type=CommandType.ResearchUnit,Kind=0,Target=0,ExpectedResearch=1};before=Fingerprint(sim.State);
            Equal(sim.Execute(PlayerSlot.Zero,c).Code,ErrorCode.InvalidValue,"unused expectation accepted");Equal(Fingerprint(sim.State),before,"unused expectation mutated state");
        }
        static UnitState Unit(MatchSimulation sim,int id,int hp,int distance,int kind=0,PlayerSlot defender=PlayerSlot.Zero)
        {var u=new UnitState{Id=id,Hp=hp,Distance=distance,Kind=kind,Defender=defender,Origin=UnitOrigin.PaidPvP,WaveId=1};sim.State.Units.Add(u);return u;}
        public static void FrontAndStrongestUseCurrentHpAndPreserveCooldown()
        {
            var sim=Active();var tower=new TowerState{Id=10,Kind=0,Owner=PlayerSlot.Zero,Position=new MapPoint(150,100),Priority=TargetPriority.Strongest};sim.State.Towers.Add(tower);
            var front=Unit(sim,12,20,250);var strong=Unit(sim,13,100,210);var tie=Unit(sim,11,100,210);sim.Step();Equal(tie.Hp,94,"strongest tie not stable ID");Equal(strong.Hp,100,"wrong strongest target");
            Command(sim,PlayerSlot.Zero,CommandType.SetTowerPriority,0,tower.Id);int cooldown=tower.CooldownTicks;sim.Step();Equal(front.Hp,20,"priority granted extra shot");Equal(tower.CooldownTicks,cooldown-1,"cooldown reset");
            Steps(sim,7);Equal(front.Hp,14,"front priority did not select furthest");
        }
        public static void UnlockMasksAreLobbyOnlyAndPreservedOnRematch()
        {
            var sim=new MatchSimulation(Content());Equal(sim.State.Players[0].AvailableTowerMask,31,"default five-class loadout");
            foreach(int mask in new[]{1,30,32,31,47,79,128,248})Check(sim.ConfigureTowerAvailability(PlayerSlot.Zero,mask),"valid unlock mask");
            foreach(int mask in new[]{-1,0,63,95,127,255,256,511})Check(!sim.ConfigureTowerAvailability(PlayerSlot.Zero,mask),"invalid unlock mask accepted");
            sim.ConfigureTowerAvailability(PlayerSlot.Zero,47);sim.ConfigureTowerAvailability(PlayerSlot.One,79);Command(sim,PlayerSlot.Zero,CommandType.SetReady);Command(sim,PlayerSlot.One,CommandType.SetReady);while(sim.State.Phase==MatchPhase.Countdown)sim.Step();
            Check(!sim.ConfigureTowerAvailability(PlayerSlot.Zero,128),"changed availability after lobby");
            Check(Command(sim,PlayerSlot.Zero,CommandType.BuildTower,5,0).Accepted,"unlocked tower rejected");string before=Fingerprint(sim.State);
            Equal(Command(sim,PlayerSlot.Zero,CommandType.BuildTower,6,1).Code,ErrorCode.TowerLocked,"locked tower accepted");Equal(Fingerprint(sim.State),before,"locked build charged");
            Equal(Command(sim,PlayerSlot.One,CommandType.BuildTower,5,0).Code,ErrorCode.TowerLocked,"peer inherited unlocks");
            sim.State.Phase=MatchPhase.Finished;sim.State.Outcome=MatchOutcome.Draw;Command(sim,PlayerSlot.Zero,CommandType.RequestRematch);Command(sim,PlayerSlot.One,CommandType.RequestRematch);Equal(sim.State.Players[0].AvailableTowerMask,47,"rematch lost local unlocks");Equal(sim.State.Players[1].AvailableTowerMask,79,"rematch lost peer unlocks");
            Check(sim.ConfigureTowerAvailability(PlayerSlot.Zero,143)&&sim.ConfigureTowerAvailability(PlayerSlot.One,47),"asymmetric five-class profile setup");
            sim.State.Phase=MatchPhase.Finished;sim.State.Outcome=MatchOutcome.Draw;Command(sim,PlayerSlot.Zero,CommandType.RequestRematch);Command(sim,PlayerSlot.One,CommandType.RequestRematch);
            Equal(sim.State.Players[0].AvailableTowerMask,143,"rematch lost selected five-class profile");Equal(sim.State.Players[1].AvailableTowerMask,47,"rematch merged peer profiles");
        }
        public static void ChainAndLineHitCapsGeometryArmorAndTieBreaks()
        {
            var sim=Active();var t=new TowerState{Id=1,Kind=5,Owner=PlayerSlot.Zero,Position=new MapPoint(100,100),Priority=TargetPriority.Strongest};sim.State.Towers.Add(t);
            var primary=Unit(sim,10,1000,200,2);var left=Unit(sim,11,100,175,2);var right=Unit(sim,12,100,225,2);var further=Unit(sim,13,100,150,2);var enemy=Unit(sim,14,100,201,2,PlayerSlot.One);
            sim.Step();Equal(primary.Hp,993,"chain primary armor");Equal(left.Hp,98,"nearest tie/second armor");Equal(further.Hp,99,"third floor/armor minimum");Equal(right.Hp,100,"chain exceeded three targets");Equal(enemy.Hp,100,"chain hit other board");Equal(t.CooldownTicks,24,"chain extra firing cycle");
            sim=Active();t=new TowerState{Id=1,Kind=6,Owner=PlayerSlot.Zero,Position=new MapPoint(200,100),Priority=TargetPriority.Strongest};sim.State.Towers.Add(t);
            primary=Unit(sim,20,1000,250);var before=Unit(sim,21,100,240);var edge=Unit(sim,22,100,318);var near=Unit(sim,23,100,317);var outside=Unit(sim,24,100,319);
            sim.Step();Equal(primary.Hp,974,"line primary");Equal(edge.Hp,82,"projection tie not ID ordered");Equal(near.Hp,89,"third line factor");Equal(before.Hp,100,"line hit before primary");Equal(outside.Hp,100,"line half-width exceeded");Equal(t.CooldownTicks,36,"line extra firing cycle");
        }
        public static void ActiveSpeedRunsAllGameplayRulesTogether()
        {
            MatchSimulation slow=Active(Content(window:12000)),fast=Active(Content(window:12000));
            foreach(var sim in new[]{slow,fast}){Command(sim,PlayerSlot.Zero,CommandType.SendUnit,3);Command(sim,PlayerSlot.One,CommandType.SendUnit,2);Command(sim,PlayerSlot.Zero,CommandType.BuildTower,4,0);}
            Command(fast,PlayerSlot.Zero,CommandType.SetSpeedVote,1);Command(fast,PlayerSlot.One,CommandType.SetSpeedVote,1);
            for(int i=0;i<200;i++){slow.Step();slow.Step();fast.Step();Equal(Fingerprint(fast.State,false),Fingerprint(slow.State,false),"same active-time state diverged");}
            Equal(fast.State.PlayTicks,400,"active2x clock");Equal(slow.State.Tick-fast.State.Tick,200,"control ticks not independent");
        }
        public static void LedgerRejectsForgedFutureFractionsAndCapsIncome()
        {
            var sim=Preparing();var p=sim.State.Players[0];
            sim.State.SpawnOrders.Add(new SpawnOrder{Sender=PlayerSlot.Zero,Kind=3,InitialCount=4,Remaining=2,PaidPrice=38,WaveId=2});p.ReservedGoldByWave[1]=19;
            Equal(EconomyRules.ValidateLedger(sim.Content,sim.State,PlayerSlot.Zero),ErrorCode.InvalidValue,"forged partial future packet passed exact-money equality");
            sim.State.SpawnOrders.Clear();p.ReservedGoldByWave[1]=0;p.ReservedGoldByWave[0]=38;p.SpawnedQuarterGoldByWave[0]=152;p.AttackIncomeUnits=152;
            Equal(EconomyRules.ValidateLedger(sim.Content,sim.State,PlayerSlot.Zero),ErrorCode.InvalidValue,"preparation carried current spawn credit");
            sim=Active(Content(hp:10000,window:12000,budget:10000));
            for(int i=0;i<19;i++)Check(Command(sim,PlayerSlot.Zero,CommandType.SendUnit,3).Accepted,"income cap fixture");
            Steps(sim,760);p=sim.State.Players[0];Equal(p.SpawnedQuarterGoldByWave[0],2888,"uncapped deployed value disappeared");Equal(p.AttackIncomeUnits,2880,"attack income cap");
            Equal(EconomyRules.ValidateLedger(sim.Content,sim.State,PlayerSlot.Zero),ErrorCode.None,"capped bonus contradicted ledger");p.AttackIncomeUnits--;
            Equal(EconomyRules.ValidateLedger(sim.Content,sim.State,PlayerSlot.Zero),ErrorCode.InvalidValue,"incorrect derived attack income accepted");
        }
        public static void EveryResearchLevelAndMalformedVoteAreAtomic()
        {
            var sim=Preparing();var initial=sim.State.Players[0].Gold;
            foreach(int kind in new[]{0,1,2,3})for(int level=0;level<8;level++)Check(Command(sim,PlayerSlot.Zero,CommandType.ResearchUnit,kind,level).Accepted,"research level purchase");
            Equal(sim.State.Players[0].Gold,initial-1944,"research total expenditure");Equal(sim.State.Players[0].AttackIncomeUnits,0,"research generated bonus");
            for(int k=0;k<4;k++)Check(Command(sim,PlayerSlot.Zero,CommandType.ResearchUnit,k,8).Accepted,"research stopped at eight");
            string before=Fingerprint(sim.State);
            foreach(var type in new[]{CommandType.SetWaveReady,CommandType.ResearchUnit,CommandType.SetTowerPriority,CommandType.SetSpeedVote})
            {
                var c=new CommandEnvelope{SessionId=sim.State.SessionId,Sequence=sim.LastSequence(PlayerSlot.Zero)+1,Type=type,Target=-1,ExpectedWave=1,ExpectedResearch=1};
                Equal(sim.Execute(PlayerSlot.Zero,c).Code,ErrorCode.InvalidValue,"unused expectation accepted");
                c.Sequence++;c.ExpectedWave=0;c.PositionX=1;Equal(sim.Execute(PlayerSlot.Zero,c).Code,ErrorCode.InvalidValue,"non-build coordinate accepted");
            }
            Equal(Fingerprint(sim.State),before,"malformed command mutated canonical state");
            var d=GameContent.CreateDefault();var five=new TowerDefinition[5];Array.Copy(d.Towers,five,5);
            var legacy=new MatchSimulation(new GameContent(d.Rules,five,d.Units,d.Map,d.Waves,d.Research,d.WaveRules));
            Check(legacy.ConfigureTowerAvailability(PlayerSlot.Zero,31),"five-class legacy profile");Check(!legacy.ConfigureTowerAvailability(PlayerSlot.Zero,47),"out-of-catalog unlock accepted");
        }
        public static void AdditionalWeaponsRespectRangeAndCooldown()
        {
            var sim=Active();var t=new TowerState{Id=1,Kind=5,Position=new MapPoint(150,100),Owner=PlayerSlot.Zero,Priority=TargetPriority.Strongest};sim.State.Towers.Add(t);
            var outside=Unit(sim,10,1000,400);sim.Step();Equal(outside.Hp,1000,"chain primary outside range hit");Equal(t.CooldownTicks,0,"empty shot consumed cooldown");
            sim.State.Units.Clear();var primary=Unit(sim,11,1000,100);var edge=Unit(sim,12,100,175);var tooFar=Unit(sim,13,100,251);
            sim.Step();Equal(edge.Hp,93,"75-unit jump boundary rejected");Equal(tooFar.Hp,100,"76-unit jump accepted");var hp=primary.Hp;Steps(sim,23);Equal(primary.Hp,hp,"chain fired before24 active ticks");
            sim.Step();Check(primary.Hp<hp,"chain never resumed after cooldown");
            sim=Active();t=new TowerState{Id=1,Kind=6,Position=new MapPoint(0,100),Owner=PlayerSlot.Zero,Priority=TargetPriority.Strongest};sim.State.Towers.Add(t);
            primary=Unit(sim,21,1000,100);outside=Unit(sim,22,100,186);sim.Step();Equal(outside.Hp,100,"line continued beyond turret range");Equal(primary.Hp,974,"line primary lost");
        }
    }
}
