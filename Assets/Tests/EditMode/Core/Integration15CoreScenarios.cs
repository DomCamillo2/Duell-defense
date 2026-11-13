using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DefenseDuell.Core;
#if UNITY_EDITOR
using NUnit.Framework;
#endif
namespace DefenseDuell.Tests
{
    public static class Integration15CoreScenarios
    {
        static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static CommandEnvelope Envelope(MatchSimulation s,PlayerSlot p,CommandType type,int kind=0,int target=-1)
        {
            bool lobby=type==CommandType.SetReady||type==CommandType.SetTeam||type==CommandType.SetLoadout;
            return new CommandEnvelope{SessionId=s.State.SessionId,Sequence=s.LastSequence(p)+1,Type=type,Kind=kind,Target=target,
                ExpectedConfigRevision=lobby||type==CommandType.ApplyMatchRules?s.State.ConfigRevision:0,ExpectedRosterRevision=lobby?s.State.RosterRevision:0};
        }
        static CommandResult Do(MatchSimulation s,int p,CommandType type,int kind=0,int target=-1)=>s.Execute((PlayerSlot)p,Envelope(s,(PlayerSlot)p,type,kind,target));
        static CommandResult Apply(MatchSimulation s,MatchRules rules)
        {
            var c=Envelope(s,PlayerSlot.Zero,CommandType.ApplyMatchRules);c.RuleWaveCount=rules.WaveCount;c.RuleStartGold=rules.StartGold;c.RuleEndless=rules.Endless;
            c.RuleIncomeMultiplier=rules.IncomeMultiplier;c.RuleFormat=rules.Format;c.RuleMapId=rules.MapId;return s.Execute(PlayerSlot.Zero,c);
        }
        static MatchSimulation Sim(MatchFormat format=MatchFormat.Duel,bool endless=false,int income=1,bool connected=true)
            =>new MatchSimulation(GameContent.CreateDefault(new GameRules(countdownTicks:0,maxCommandsPerSecond:100)).WithMatchRules(new MatchRules(3,10000,endless,income,format)),guestConnected:connected);
        static void Start(MatchSimulation s)
        {for(int p=0;p<s.State.Players.Length;p++)Check(Do(s,p,CommandType.SetReady).Accepted,"Ready rejected for "+p);Check(s.State.Phase==MatchPhase.Playing,"Match failed atomic start");}
        static void Active(MatchSimulation s,bool clearNeutral=true)
        {
            for(int p=0;p<s.State.Players.Length;p++){var c=Envelope(s,(PlayerSlot)p,CommandType.SetWaveReady,1);c.ExpectedWave=s.State.WaveId;Check(s.Execute((PlayerSlot)p,c).Accepted,"Wave vote rejected");}
            s.Step();Check(s.State.WavePhase==WavePhase.Active,"Wave votes did not activate");
            if(clearNeutral)for(int p=0;p<s.State.Players.Length;p++)s.State.NeutralCursor[p]=s.Content.GetWave(s.State.WaveId).TotalCount;
        }
        static CommandEnvelope Packet(MatchSimulation s,int sender,int target,int kind=0)
        {
            var q=SendRules.Quote(s.Content,s.State,(PlayerSlot)sender,kind,target);var c=Envelope(s,(PlayerSlot)sender,CommandType.SendUnit,kind);
            c.ExpectedWave=q.WaveId;c.ExpectedResearch=q.ResearchLevel;c.EnemyTarget=target;return c;
        }
        static void Send(MatchSimulation s,int sender,int target,int kind=0)=>Check(s.Execute((PlayerSlot)sender,Packet(s,sender,target,kind)).Accepted,"Paid send rejected");
        static MapPoint FindSite(MatchSimulation s,PlayerSlot p,int kind)
        {for(int y=30;y<s.Content.Map.Height-30;y+=60)for(int x=30;x<s.Content.Map.Width-30;x+=60){var point=new MapPoint(x,y);if(PlacementValidator.ValidateBuild(s.Content,s.State,p,kind,-1,point,out _) ==ErrorCode.None)return point;}throw new InvalidOperationException("No legal site");}
        static TowerState Build(MatchSimulation s,int p,int kind)
        {var point=FindSite(s,(PlayerSlot)p,kind);var c=Envelope(s,(PlayerSlot)p,CommandType.BuildTower,kind);c.PositionX=point.X;c.PositionY=point.Y;Check(s.Execute((PlayerSlot)p,c).Accepted,"Build rejected");return s.State.Towers.Last();}
        public static void FormatsRequireCompleteEqualHumanRosters()
        {
            foreach(MatchFormat format in Enum.GetValues(typeof(MatchFormat)))
            {
                var s=Sim(format,connected:false);int count=s.Content.MatchRules.PlayerCount;
                Check(s.State.Players.Length==count&&s.State.Teams.Length==s.Content.MatchRules.TeamCount&&s.State.ConnectedPlayerMask==1,"Fake or incorrect roster shape");
                Check(Do(s,0,CommandType.SetReady).Code==ErrorCode.MissingPlayers,"Host started without people");
                for(int p=1;p<count;p++)Check(s.ConfigureConnection((PlayerSlot)p,true),"Connection callback rejected");
                int original=s.State.Players[0].TeamId;
                Check(Do(s,0,CommandType.SetTeam,(original+1)%s.State.Teams.Length).Accepted,"Own team choice rejected");
                Check(Do(s,0,CommandType.SetReady).Code==ErrorCode.MissingPlayers,"Unbalanced teams ready");
                Check(Do(s,0,CommandType.SetTeam,original).Accepted,"Restored team rejected");Start(s);
                Check(s.State.Teams.All(t=>t.Hp==30*s.Content.MatchRules.MembersPerTeam),"Shared team initial HP wrong");
                if(format!=MatchFormat.Duel){var before=s.State.Towers.Count;new BotBrain(PlayerSlot.One).Tick(s);Check(s.State.Towers.Count==before,"Bot acted in human team mode");}
            }
        }
        public static void RulesMapFormatAndLoadoutInvalidateConsentExactly()
        {
            var s=Sim(connected:false);Check(s.ConfigureConnection(PlayerSlot.One,true),"Guest connect");Do(s,0,CommandType.SetReady);
            var stale=Envelope(s,PlayerSlot.One,CommandType.SetReady);ulong roster=s.State.RosterRevision;
            Check(Do(s,1,CommandType.SetLoadout,128).Accepted&&s.State.RosterRevision==roster+1&&!s.State.Players[0].Ready,"Eighth-class selection not bound");
            stale.Sequence=s.LastSequence(PlayerSlot.One)+1;
            Check(s.Execute(PlayerSlot.One,stale).Code==ErrorCode.StaleLobbyRevision,"Stale loadout readiness accepted");
            Check(Do(s,1,CommandType.SetLoadout,0).Code==ErrorCode.InvalidValue&&s.State.Players[1].AvailableTowerMask==128,"Empty loadout accepted");
            roster=s.State.RosterRevision;Check(Do(s,1,CommandType.SetLoadout,128).Accepted&&s.State.RosterRevision==roster,"No-op loadout invalidated");
            var map=MapCatalog.Create(MapCatalog.Count-1);var rules=new MatchRules(199,BigInteger.Pow(10,40),true,5,MatchFormat.ThreeVsThree,map.Id);
            Check(Apply(s,rules).Accepted&&s.Content.Map.Id==map.Id&&s.State.Players.Length==6&&s.State.ConnectedPlayerMask==3,"Atomic format/map/rules failed");
            Check(s.State.Players.All(p=>p.Gold==0&&!p.Ready)&&s.State.Players[1].AvailableTowerMask==128,"Apply lost selection or paid gold");
            for(int p=2;p<6;p++)s.ConfigureConnection((PlayerSlot)p,true);
            string hash=s.Content.ContentHash;var revision=s.State.ConfigRevision;
            Check(Apply(s,new MatchRules(3,10000)).Code==ErrorCode.MissingPlayers&&s.Content.ContentHash==hash&&s.State.ConfigRevision==revision,"Format shrink evicted connected people");
            Start(s);Check(s.State.Players.All(p=>p.Gold==rules.StartGold),"Exact per-person initial gold");
            Check(Do(s,0,CommandType.SetLoadout,1).Code==ErrorCode.WrongPhase,"Started loadout editable");
        }
        public static void TeamHpIsSharedButWalletResearchAndTowersRemainPersonal()
        {
            var s=Sim(MatchFormat.TwoVsTwo);Start(s);var tower=Build(s,0,0);var before=s.State.Players[1].Gold;
            Check(s.State.Players[0].Gold==10000-65&&before==10000,"Team wallets merged");
            Check(Do(s,1,CommandType.UpgradeTower,(int)TowerUpgradePath.Firepower,tower.Id).Code==ErrorCode.NotOwner,"Ally upgraded foreign tower");
            var research=Envelope(s,PlayerSlot.Zero,CommandType.ResearchUnit);research.ExpectedResearch=0;
            Check(s.Execute(PlayerSlot.Zero,research).Accepted&&s.State.Players[0].ResearchLevels[0]==1&&s.State.Players[1].ResearchLevels[0]==0,"Research merged");
            Active(s);for(int p=0;p<2;p++)s.State.Units.Add(new UnitState{Id=100+p,Kind=0,Defender=(PlayerSlot)p,Sender=(PlayerSlot)p,Origin=UnitOrigin.NeutralWave,Hp=1,Distance=s.Content.Map.Length-1});
            s.Step();Check(s.State.Teams[0].Hp==58&&s.State.Players[0].Hp==58&&s.State.Players[1].Hp==58&&s.State.Teams[1].Hp==60,"Same-tick personal-lane leaks did not aggregate to shared team HP");
        }
        public static void PaidTargetsAreImmutableAndSenderQueuesAreFair()
        {
            var s=Sim(MatchFormat.TwoVsTwo);Start(s);Active(s);var original=s.State.Players[0].Gold;
            foreach(int bad in new[]{0,1,4,-1})Check(s.Execute(PlayerSlot.Zero,Packet(s,0,bad)).Code==ErrorCode.InvalidTarget,"Own/ally/invalid target accepted");
            Check(s.State.Players[0].Gold==original,"Invalid targets charged gold");
            Send(s,0,2,3);Send(s,1,2,0);var packet=s.State.SpawnOrders[0];
            Check(Do(s,0,CommandType.SetAttackTarget,3).Accepted&&packet.EnemyTarget==PlayerSlot.Two,"Target selection rewrote purchased packet");
            Send(s,0,3,0);for(int i=0;i<25;i++)s.Step();
            var paid=s.State.Units.Where(u=>u.Origin==UnitOrigin.PaidPvP).OrderBy(u=>u.Id).ToArray();
            Check(paid.Length>=5&&paid[0].Sender==PlayerSlot.Zero&&paid[1].Sender==PlayerSlot.One,"One sender starved another on shared defender");
            Check(paid.Where(u=>u.Sender==PlayerSlot.Zero).Take(4).All(u=>u.Defender==PlayerSlot.Two),"Per-sender FIFO or immutable target lost");
            Check(paid.Any(u=>u.Sender==PlayerSlot.Zero&&u.Defender==PlayerSlot.Three),"Next target never dispatched");
            for(int p=0;p<4;p++)Check(EconomyRules.ValidateLedger(s.Content,s.State,(PlayerSlot)p)==ErrorCode.None,"Targeted paid ledger invalid");
        }
        public static void NeutralAndMultipleSendersAllReceiveSpawnTurns()
        {
            var s=Sim(MatchFormat.ThreeVsThree);Start(s);Active(s,false);
            for(int p=0;p<3;p++)Send(s,p,3,3);
            for(int i=0;i<60;i++)s.Step();
            var field=s.State.Units.Where(u=>u.Defender==PlayerSlot.Three).ToArray();
            Check(field.Any(u=>u.Origin==UnitOrigin.NeutralWave),"Paid traffic starved neutrals");
            for(int p=0;p<3;p++)Check(field.Any(u=>u.Origin==UnitOrigin.PaidPvP&&(int)u.Sender==p),"Paid sender starved: "+p);
            Check(s.State.NextPaidSender.Length==6&&s.State.NextPaidSpawnTicks.Length==6,"Scheduler lanes not actual player shape");
        }
        public static void ThreeTeamsEliminateAndRefundWithoutRetargeting()
        {
            var s=Sim(MatchFormat.TwoVsTwoVsTwo);Start(s);Active(s);Send(s,0,2,3);
            s.Step();var order=s.State.SpawnOrders.Single();Check(order.Remaining==3,"Partial packet fixture");
            BigInteger gold=s.State.Players[0].Gold,remainder=s.State.Players[0].GoldRemainder;
            s.State.Teams[1].Hp=1;s.State.Units.Add(new UnitState{Id=500,Kind=0,Defender=PlayerSlot.Three,Sender=PlayerSlot.Three,Origin=UnitOrigin.NeutralWave,Hp=1,Distance=s.Content.Map.Length-1});s.Step();
            Check(s.State.Phase==MatchPhase.Playing&&s.State.Teams[1].Hp==0&&s.State.Teams[2].Hp==60,"Three-team match ended with two teams alive");
            Check(s.State.SpawnOrders.Count==0&&!s.State.Units.Any(u=>s.State.Players[(int)u.Defender].TeamId==1),"Eliminated field traffic retained");
            Check((s.State.Players[0].Gold-gold)*960+s.State.Players[0].GoldRemainder-remainder==114*240,"Remaining packet refund lost fractional gold");
            Check(s.State.Players[0].CancelledQuarterGoldByWave[0]==114&&EconomyRules.ValidateLedger(s.Content,s.State,PlayerSlot.Zero)==ErrorCode.None,"Cancellation ledger not conserved");
            Check(Do(s,2,CommandType.SetSpeedVote,1).Code==ErrorCode.NotOwner,"Eliminated player acted");
            s.State.Teams[0].Hp=1;s.State.Units.Add(new UnitState{Id=501,Kind=0,Defender=PlayerSlot.One,Sender=PlayerSlot.One,Origin=UnitOrigin.NeutralWave,Hp=1,Distance=s.Content.Map.Length-1});s.Step();
            Check(s.State.Phase==MatchPhase.Finished&&s.State.WinningTeamId==2&&s.State.Outcome==MatchOutcome.Team2,"Third-team winner unrepresentable");
        }
        public static void EndlessRecipesAndLedgersDoNotAllocateHistory()
        {
            var s=Sim(endless:true);Start(s);Check(s.Content.WaveCount==0&&s.Content.Waves.Length==10,"Endless fakes a giant finite plan");
            BigInteger huge=BigInteger.Pow(10,30)+201;
            var recipe=s.Content.GetWave(huge);Check(recipe.HpPercent==100+12*(huge-1)+(2*(huge-10)*(huge-10)+4)/5&&recipe.TotalCount>=1&&recipe.TotalCount<=64&&recipe.HasPerKindHp,"Exact endless recipe narrowed");
            for(int wave=1;wave<=205;wave++)
            {
                Check(s.State.WaveId==wave,"Endless progression skipped/stopped");
                s.State.WavePhase=WavePhase.Clearing;for(int p=0;p<2;p++)s.State.NeutralCursor[p]=s.Content.GetWave(wave).TotalCount;s.Step();
                foreach(var p in s.State.Players)Check(p.ReservedGoldByWave.Length==2&&p.ReservedGoldByWave.FirstWave==s.State.WaveId,"Endless ledger grows with history");
            }
            s.State.WaveId=huge;foreach(var p in s.State.Players){p.ReservedGoldByWave=new WaveLedger(huge,new BigInteger[2],true);p.SpawnedQuarterGoldByWave=new WaveLedger(huge,new BigInteger[2],true);p.CancelledQuarterGoldByWave=new WaveLedger(huge,new BigInteger[2],true);}
            s.State.WavePhase=WavePhase.Active;s.State.WaveStartPlayTick=s.State.PlayTicks;s.State.NeutralCursor[0]=s.State.NeutralCursor[1]=0;s.Step();
            Check(s.State.Units.Count==2&&s.State.Units.All(u=>u.WaveId==huge&&u.Hp==recipe.HpForKind(u.Kind)&&recipe.MatchesNeutral(u.NeutralRecipeIndex,u.Kind)),"Huge exact recipe did not spawn actual HP");
            Check(SendRules.Quote(s.Content,s.State,PlayerSlot.Zero,0).WaveId==huge,"Endless send quote narrowed");
        }
        public static void EndlessSendCreditArchivesExactlyAndRejectsForgery()
        {
            var s=Sim(endless:true);Start(s);Active(s);Send(s,0,1);s.Step();s.State.Units.Clear();
            s.State.WavePhase=WavePhase.Clearing;s.Step();var p=s.State.Players[0];
            Check(s.State.WaveId==2&&p.ArchivedSpawnedQuarterGold==72&&p.AttackIncomeUnits==72,"Closed wave credit not archived exactly");
            Check(p.SpawnedQuarterGoldByWave.FirstWave==2&&p.SpawnedQuarterGoldByWave[1]==0&&EconomyRules.ValidateLedger(s.Content,s.State,PlayerSlot.Zero)==ErrorCode.None,"Bounded archive ledger invalid");
            p.ArchivedSpawnedQuarterGold++;Check(EconomyRules.ValidateLedger(s.Content,s.State,PlayerSlot.Zero)==ErrorCode.InvalidValue,"Forged archived income accepted");
        }
        public static void IncomeFactorsMultiplyOnlyRecurringIncome()
        {
            foreach(int factor in new[]{1,2,3,5,10})
            {
                var s=Sim(income:factor);Start(s);var mine=Build(s,0,4);Active(s);
                var p=s.State.Players[0];p.ReservedGoldByWave[0]=720;p.SpawnedQuarterGoldByWave[0]=2880;p.AttackIncomeUnits=2880;p.GoldRemainder=721;
                BigInteger before=p.Gold,units=(4*960+960+2880)*factor;
                Check(EconomyRules.IncomeUnits(s.Content,s.State,PlayerSlot.Zero)==units&&EconomyRules.EffectiveAttackIncomeUnits(s.Content,p)==2880*factor,"Factor applied before cap or missed mine/base");
                for(int i=0;i<20;i++)s.Step();Check(p.Gold==before+(units+721)/960&&p.GoldRemainder==(units+721)%960,"Exact recurring payout/remainder changed");
                Check(s.RefundFor(mine)==64&&SendRules.Quote(s.Content,s.State,PlayerSlot.Zero,0).Price==18,"Factor changed refund or price");
                BigInteger defender=s.State.Players[1].Gold;s.State.Units.Add(new UnitState{Id=999,Kind=0,Hp=0,Defender=PlayerSlot.One,Sender=PlayerSlot.Zero,Origin=UnitOrigin.PaidPvP});s.Step();
                Check(s.State.Players[1].Gold==defender+4,"Factor multiplied kill bounty");
            }
            foreach(int bad in new[]{0,4,6,-1,11})Check(MatchRulesPolicy.Validate(GameContent.CreateDefault(),new MatchRules(3,180,incomeMultiplier:bad))==ErrorCode.InvalidValue,"Unsupported factor accepted");
        }
        public static void AllLivingPlayersControlTempoAndRematchKeepsChoices()
        {
            var s=Sim(MatchFormat.ThreeVsThree,true,3);Do(s,5,CommandType.SetLoadout,128);Start(s);Active(s);
            for(int p=0;p<5;p++)Do(s,p,CommandType.SetSpeedVote,1);Check(s.State.SimulationSpeed==1,"Incomplete six-player consent accelerated");
            Do(s,5,CommandType.SetSpeedVote,1);Check(s.State.SimulationSpeed==2,"All six speed votes ignored");
            Do(s,3,CommandType.SetSpeedVote,0);Check(s.State.SimulationSpeed==1,"One withdrawal did not stop acceleration");
            string hash=s.Content.ContentHash;ulong epoch=s.State.SessionId;s.State.Teams[1].Hp=0;s.Step();Check(s.State.Phase==MatchPhase.Finished,"Elimination did not end endless");
            for(int p=0;p<5;p++)Do(s,p,CommandType.RequestRematch);Check(s.State.Phase==MatchPhase.Finished,"Partial rematch started");Do(s,5,CommandType.RequestRematch);
            Check(s.State.SessionId==epoch+1&&s.Content.ContentHash==hash&&s.State.Players.Length==6&&s.State.Players[5].AvailableTowerMask==128&&s.State.Players.All(p=>p.Gold==0&&!p.Ready&&!p.SpeedReady),"Rematch lost options/choices or paid early");Start(s);
            Check(s.State.Players.All(p=>p.Gold==10000)&&s.State.Teams.All(t=>t.Hp==90),"Rematch team HP/start wallets incorrect");
        }
        public static void LaserIsAnEighthRealWeaponWithEffectiveUpgrades()
        {
            var s=Sim();Check(s.ConfigureTowerAvailability(PlayerSlot.Zero,128),"Laser loadout rejected");Start(s);Check(s.Content.Towers.Length==8&&s.State.Players[0].AvailableTowerMask==128,"Laser unavailable in full loadout");
            var laser=Build(s,0,7);var definition=s.Content.Towers[7];Check(definition.Mechanic==WeaponMechanic.Beam&&definition.Price==180&&definition.FireIntervalTicks==8,"Laser definition wrong");
            var before=TowerUpgradeRules.Stats(s.Content,laser);Check(Do(s,0,CommandType.UpgradeTower,(int)TowerUpgradePath.Damage,laser.Id).Accepted,"Laser damage upgrade rejected");
            Check(Do(s,0,CommandType.UpgradeTower,(int)TowerUpgradePath.Range,laser.Id).Accepted,"Laser range upgrade rejected");var after=TowerUpgradeRules.Stats(s.Content,laser);
            Check(after.Damage>before.Damage&&after.Range>before.Range&&after.FireIntervalTicks==8,"Laser upgrades are labels only");
            Active(s);MapPoint point=default;int distance=-1;for(int d=0;d<s.Content.Map.Length;d+=10){point=s.Content.Map.PositionAt(d);long dx=point.X-laser.Position.X,dy=point.Y-laser.Position.Y;if(dx*dx+dy*dy<=(long)after.Range*after.Range){distance=d;break;}}
            Check(distance>=0,"Laser site cannot cover path");var target=new UnitState{Id=1000,Kind=0,Defender=PlayerSlot.Zero,Sender=PlayerSlot.One,Hp=1000,Distance=distance};s.State.Units.Add(target);s.Step();Check(target.Hp==1000-after.Damage,"Beam did not apply effective damage");
        }
        public static IEnumerable<KeyValuePair<string,Action>> All()
        {
            yield return new KeyValuePair<string,Action>(nameof(FormatsRequireCompleteEqualHumanRosters),FormatsRequireCompleteEqualHumanRosters);
            yield return new KeyValuePair<string,Action>(nameof(RulesMapFormatAndLoadoutInvalidateConsentExactly),RulesMapFormatAndLoadoutInvalidateConsentExactly);
            yield return new KeyValuePair<string,Action>(nameof(TeamHpIsSharedButWalletResearchAndTowersRemainPersonal),TeamHpIsSharedButWalletResearchAndTowersRemainPersonal);
            yield return new KeyValuePair<string,Action>(nameof(PaidTargetsAreImmutableAndSenderQueuesAreFair),PaidTargetsAreImmutableAndSenderQueuesAreFair);
            yield return new KeyValuePair<string,Action>(nameof(NeutralAndMultipleSendersAllReceiveSpawnTurns),NeutralAndMultipleSendersAllReceiveSpawnTurns);
            yield return new KeyValuePair<string,Action>(nameof(ThreeTeamsEliminateAndRefundWithoutRetargeting),ThreeTeamsEliminateAndRefundWithoutRetargeting);
            yield return new KeyValuePair<string,Action>(nameof(EndlessRecipesAndLedgersDoNotAllocateHistory),EndlessRecipesAndLedgersDoNotAllocateHistory);
            yield return new KeyValuePair<string,Action>(nameof(EndlessSendCreditArchivesExactlyAndRejectsForgery),EndlessSendCreditArchivesExactlyAndRejectsForgery);
            yield return new KeyValuePair<string,Action>(nameof(IncomeFactorsMultiplyOnlyRecurringIncome),IncomeFactorsMultiplyOnlyRecurringIncome);
            yield return new KeyValuePair<string,Action>(nameof(AllLivingPlayersControlTempoAndRematchKeepsChoices),AllLivingPlayersControlTempoAndRematchKeepsChoices);
            yield return new KeyValuePair<string,Action>(nameof(LaserIsAnEighthRealWeaponWithEffectiveUpgrades),LaserIsAnEighthRealWeaponWithEffectiveUpgrades);
        }
    }
#if UNITY_EDITOR
    public sealed class Integration15CoreTests
    {
        [TestCaseSource(typeof(Integration15CoreScenarios),nameof(Integration15CoreScenarios.All))]
        public void Scenario(KeyValuePair<string,Action> scenario)=>scenario.Value();
    }
#endif
}
