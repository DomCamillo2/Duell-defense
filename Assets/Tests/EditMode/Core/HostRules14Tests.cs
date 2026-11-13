using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using DefenseDuell.Core;
#if UNITY_EDITOR
using NUnit.Framework;
#endif

namespace DefenseDuell.Tests
{
    public static class HostRules14Scenarios
    {
        static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        static CommandEnvelope Command(MatchSimulation sim, CommandType type, PlayerSlot player=PlayerSlot.Zero)
        {
            return new CommandEnvelope { SessionId=sim.State.SessionId, Sequence=sim.LastSequence(player)+1,
                Type=type,Kind=0,Target=-1,
                ExpectedConfigRevision=(type==CommandType.SetReady||type==CommandType.ApplyMatchRules)?sim.State.ConfigRevision:0,
                ExpectedRosterRevision=type==CommandType.SetReady?sim.State.RosterRevision:0 };
        }
        static CommandResult Apply(MatchSimulation sim,int waves,BigInteger gold,PlayerSlot player=PlayerSlot.Zero)
        {var c=Command(sim,CommandType.ApplyMatchRules,player);c.RuleWaveCount=waves;c.RuleStartGold=gold;return sim.Execute(player,c);}
        static CommandResult Ready(MatchSimulation sim,PlayerSlot player,bool ready=true)
        {var c=Command(sim,CommandType.SetReady,player);c.Kind=ready?0:1;return sim.Execute(player,c);}
        static void Start(MatchSimulation sim)
        {Check(Ready(sim,PlayerSlot.Zero).Accepted,"Host ready rejected");Check(Ready(sim,PlayerSlot.One).Accepted,"Guest ready rejected");}
        static string Stamp(MatchSimulation sim)
        {
            var s=sim.State;var b=new StringBuilder();b.Append(sim.Content.ContentHash).Append('|').Append(s.SessionId).Append('|')
                .Append(s.ConfigRevision).Append('|').Append(s.RosterRevision).Append('|').Append(s.ConnectedPlayerMask).Append('|')
                .Append(s.StartGoldGranted).Append('|').Append(s.Phase).Append('|').Append(s.MatchRules.StartGoldDecimal).Append('|').Append(s.MatchRules.WaveCount);
            foreach(var p in s.Players){b.Append('|').Append(p.Gold).Append('|').Append(p.Ready).Append('|').Append(p.Rematch).Append('|').Append(p.AvailableTowerMask);
                foreach(var n in p.ReservedGoldByWave)b.Append(',').Append(n);foreach(var n in p.SpawnedQuarterGoldByWave)b.Append(';').Append(n);}
            return b.ToString();
        }
        public static void DefaultPayoutOccursOnlyAtAtomicStart()
        {
            var s=new MatchSimulation(GameContent.CreateDefault());
            Check(s.Content.MatchStartGold==180 && s.Content.WaveCount==10,"Defaults changed");
            Check(s.State.Players[0].Gold==0 && !s.State.StartGoldGranted,"Premature lobby payout");
            Check(Ready(s,PlayerSlot.Zero).Accepted && s.State.Players[0].Gold==0,"First consent paid gold");
            Check(Ready(s,PlayerSlot.One).Accepted && s.State.StartGoldGranted,"Atomic start failed");
            Check(s.State.Players[0].Gold==180 && s.State.Players[1].Gold==180 && s.State.Phase==MatchPhase.Countdown,"Default start resources");
        }
        public static void ExactLargeGoldNeverNarrowsOrMutatesBase()
        {
            var basis=GameContent.CreateDefault();string hash=basis.ContentHash;var gold=BigInteger.Pow(10,100)+9007199254740993;
            var s=new MatchSimulation(basis);Check(Apply(s,3,gold).Accepted,"Huge exact apply");
            Check(s.Content.MatchStartGold==gold && s.Content.StartGold==180 && s.Content.Rules.StartGold==180,"Base gold mutated or narrowed");
            Check(basis.ContentHash==hash && s.Content.BaseContentHash==hash,"Base hash mutated");
            Check(s.State.Players[0].Gold==0 && s.State.Players[1].Gold==0,"Apply paid gold");Start(s);
            Check(s.State.Players[0].Gold==gold && s.State.Players[1].Gold==gold,"Start gold rounded");
        }
        public static void PrefixCanExtendAgainAndHashIsDeterministic()
        {
            var b=GameContent.CreateDefault();var shorter=b.WithMatchRules(new MatchRules(2,500));
            var longer=shorter.WithMatchRules(new MatchRules(9,500));var direct=b.WithMatchRules(new MatchRules(9,500));
            Check(longer.ContentHash==direct.ContentHash && longer.Waves[8].HpPercent==b.Waves[8].HpPercent,"Prefix path affected hash/recipe");
            Check(shorter.WithMatchRules(new MatchRules(10,180)).ContentHash==b.ContentHash,"Default roundtrip hash");
            Check(shorter.ContentHash!=b.WithMatchRules(new MatchRules(2,501)).ContentHash,"Gold missing from hash");
            Check(shorter.ContentHash!=b.WithMatchRules(new MatchRules(3,500)).ContentHash,"Waves missing from hash");
            var maps=MapCatalog.CreateContents(shorter);Check(maps.Length==MapCatalog.Count && maps[2].MatchStartGold==500 && maps[2].WaveCount==2,"Map copy lost rules");
            Check(maps[2].WithMatchRules(new MatchRules(10,500)).WaveCount==10,"Map copy lost authored basis");
        }
        public static void ShortAuthoredCatalogContinuesFromItsLastWave()
        {
            var b=GameContent.CreateDefault();var shortBase=new GameContent(b.Rules,b.Towers,b.Units,b.Map,new[]{b.Waves[0]},b.Research,b.WaveRules);
            var extended=shortBase.WithMatchRules(new MatchRules(200,180));
            Check(extended.WaveCount==200 && ReferenceEquals(extended.Waves[0],shortBase.Waves[0]),"Short authored prefix changed");
            for(int w=1;w<200;w++)
                Check(string.Join(",",extended.Waves[w].Counts)==string.Join(",",shortBase.Waves[0].Counts)
                    && extended.Waves[w].HpPercent==100+12*w,"Short-base continuation recipe wrong");
            Check(extended.WithMatchRules(new MatchRules(2,180)).WithMatchRules(new MatchRules(200,180)).ContentHash==extended.ContentHash,"Short-base continuation depends on previous option");
            Check(MatchRulesPolicy.Validate(shortBase,new MatchRules(201,180))==ErrorCode.UnsupportedWaveCount,"201 waves accepted");
            Check(shortBase.WithMatchRules(new MatchRules(1,0)).WaveCount==1,"One-wave authored plan rejected");
        }
        public static void CanonicalDecimalParsingAndMagnitudeBoundary()
        {
            foreach(var t in new[]{"", "01", "-1", "+1", "1e3", "1,000", " 1", "١"})
                Check(MatchRulesPolicy.TryParseStartGold(t,out _)==ErrorCode.InvalidValue,"Noncanonical text: "+t);
            Check(MatchRulesPolicy.TryParseStartGold("900719925474099312345",out var v)==ErrorCode.None && v==BigInteger.Parse("900719925474099312345",CultureInfo.InvariantCulture),"Exact decimal parse");
            var max=BigInteger.One << (8*(MatchRulesPolicy.MaxStartGoldMagnitudeBytes-1));var b=GameContent.CreateDefault();
            Check(MatchRulesPolicy.Validate(b,new MatchRules(10,max))==ErrorCode.None,"Maximum payload rejected");
            Check(MatchRulesPolicy.Validate(b,new MatchRules(10,max<<8))==ErrorCode.RulesPayloadTooLarge,"Oversize payload accepted");
            Check(MatchRulesPolicy.TryParseStartGold(new string('9',MatchRulesPolicy.MaxStartGoldMagnitudeBytes*3+1),out _)==ErrorCode.RulesPayloadTooLarge,"Oversize text parsed");
        }
        public static void InvalidGuestAndStaleApplyAreAtomic()
        {
            var s=new MatchSimulation(GameContent.CreateDefault());Ready(s,PlayerSlot.Zero);var before=Stamp(s);
            Check(Apply(s,5,500,PlayerSlot.One).Code==ErrorCode.NotOwner,"Guest changed rules");
            Check(Apply(s,201,500).Code==ErrorCode.UnsupportedWaveCount,"201-wave continuation accepted");
            Check(Apply(s,5,-1).Code==ErrorCode.InvalidValue,"Negative gold accepted");
            var c=Command(s,CommandType.ApplyMatchRules);c.ExpectedConfigRevision=9;c.RuleWaveCount=5;c.RuleStartGold=500;
            Check(s.Execute(PlayerSlot.Zero,c).Code==ErrorCode.StaleLobbyRevision,"Stale apply accepted");
            Check(Stamp(s)==before,"Rejected apply mutated state");
        }
        public static void ChangeResetsReadyNoopPreservesIt()
        {
            var s=new MatchSimulation(GameContent.CreateDefault());Ready(s,PlayerSlot.Zero);var before=Stamp(s);
            Check(Apply(s,10,180).Accepted && Stamp(s)==before,"No-op reset consent");
            Check(Apply(s,5,500).Accepted && s.State.ConfigRevision==1 && s.State.RosterRevision==0,"Config revision wrong");
            Check(!s.State.Players[0].Ready && !s.State.Players[1].Ready && !s.State.StartGoldGranted,"Consent/payout after change");
            Check(s.State.Players[0].ReservedGoldByWave.Length==5 && s.State.Players[1].SpawnedQuarterGoldByWave.Length==5,"Ledger length stale");
            Check(!ReferenceEquals(s.State.Players[0].ReservedGoldByWave,s.State.Players[1].ReservedGoldByWave),"Shared wallets/ledgers");
        }
        public static void ReadyBindsBothDisplayedRevisions()
        {
            var s=new MatchSimulation(GameContent.CreateDefault());var old=Command(s,CommandType.SetReady,PlayerSlot.One);
            Apply(s,5,500);Check(s.Execute(PlayerSlot.One,old).Code==ErrorCode.StaleLobbyRevision,"Old config Ready accepted");
            var current=Command(s,CommandType.SetReady,PlayerSlot.One);s.ConfigureGuestConnection(false);s.ConfigureGuestConnection(true);
            Check(s.Execute(PlayerSlot.One,current).Code==ErrorCode.StaleLobbyRevision,"Old roster Ready accepted");
            Check(!s.State.Players[1].Ready && s.State.Phase==MatchPhase.Lobby,"Stale consent started match");
        }
        public static void MissingGuestAndRosterCallbacksNeverPay()
        {
            var s=new MatchSimulation(GameContent.CreateDefault(),guestConnected:false);
            Check(Ready(s,PlayerSlot.Zero).Code==ErrorCode.MissingPlayers,"Host starts alone");
            Check(s.ConfigureGuestConnection(true) && s.State.RosterRevision==1,"Guest admission revision");
            Ready(s,PlayerSlot.Zero);var before=Stamp(s);Check(s.ConfigureGuestConnection(true) && Stamp(s)==before,"Duplicate admission reset");
            Check(s.ConfigureGuestConnection(false) && !s.State.Players[0].Ready && s.State.Players[0].Gold==0,"Disconnect failed reset");
            Check(s.ConfigureGuestConnection(true),"Reconnect denied");Start(s);
            Check(!s.ConfigureGuestConnection(false) && s.State.Players[0].Gold==180,"Connection callback changed locked wallet");
        }
        public static void MaskChangeBindsConsentAndNoopDoesNotReset()
        {
            var s=new MatchSimulation(GameContent.CreateDefault());Ready(s,PlayerSlot.Zero);var before=Stamp(s);
            Check(s.ConfigureTowerAvailability(PlayerSlot.One,31) && Stamp(s)==before,"Same mask reset");
            Check(s.ConfigureTowerAvailability(PlayerSlot.One,47),"Valid mask rejected");
            Check(s.State.RosterRevision==1 && !s.State.Players[0].Ready,"Changed availability did not invalidate consent");
            Start(s);Check(!s.ConfigureTowerAvailability(PlayerSlot.One,127),"Midmatch availability changed");
        }
        public static void ReplayedConsentCannotUndoWithdrawal()
        {
            var s=new MatchSimulation(GameContent.CreateDefault());var ready=Command(s,CommandType.SetReady);
            Check(s.Execute(PlayerSlot.Zero,ready).Accepted,"Ready rejected");Check(Ready(s,PlayerSlot.Zero,false).Accepted,"Withdrawal rejected");
            Check(s.Execute(PlayerSlot.Zero,ready).Accepted && !s.State.Players[0].Ready,"Receipt replay reapplied consent");
            var altered=ready;altered.ExpectedRosterRevision=1;
            Check(s.Execute(PlayerSlot.Zero,altered).Code==ErrorCode.Duplicate,"Changed revision reused receipt");
            altered=ready;altered.Kind=1;Check(s.Execute(PlayerSlot.Zero,altered).Code==ErrorCode.Duplicate,"Changed vote reused receipt");
        }
        public static void ReplayIdentityIncludesExactRulePayloadAndRevisions()
        {
            var s=new MatchSimulation(GameContent.CreateDefault());var c=Command(s,CommandType.ApplyMatchRules);c.RuleWaveCount=3;c.RuleStartGold=BigInteger.Pow(10,50);
            Check(s.Execute(PlayerSlot.Zero,c).Accepted,"Apply rejected");var before=Stamp(s);
            Check(s.Execute(PlayerSlot.Zero,c).Accepted && Stamp(s)==before,"Exact retry mutated state");
            var changed=c;changed.RuleStartGold++;Check(s.Execute(PlayerSlot.Zero,changed).Code==ErrorCode.Duplicate,"Gold payload not bound");
            changed=c;changed.RuleWaveCount=4;Check(s.Execute(PlayerSlot.Zero,changed).Code==ErrorCode.Duplicate,"Wave payload not bound");
            changed=c;changed.ExpectedConfigRevision=1;Check(s.Execute(PlayerSlot.Zero,changed).Code==ErrorCode.Duplicate,"Config not bound");
        }
        public static void UnusedCommandFieldsAreRejected()
        {
            var s=new MatchSimulation(GameContent.CreateDefault());var ready=Command(s,CommandType.SetReady);ready.RuleStartGold=1;
            Check(s.Execute(PlayerSlot.Zero,ready).Code==ErrorCode.InvalidValue,"Ready smuggled gold");
            var apply=Command(s,CommandType.ApplyMatchRules);apply.RuleWaveCount=3;apply.RuleStartGold=500;apply.ExpectedRosterRevision=1;
            Check(s.Execute(PlayerSlot.Zero,apply).Code==ErrorCode.InvalidValue,"Apply stray roster payload");Start(s);
            var speed=Command(s,CommandType.SetSpeedVote);speed.ExpectedConfigRevision=1;
            Check(s.Execute(PlayerSlot.Zero,speed).Code==ErrorCode.InvalidValue,"Gameplay smuggled lobby revision");
        }
        public static void StartLocksRulesAndPaysOnlyOnce()
        {
            var s=new MatchSimulation(GameContent.CreateDefault());Apply(s,1,999);Ready(s,PlayerSlot.Zero);
            var last=Command(s,CommandType.SetReady,PlayerSlot.One);Check(s.Execute(PlayerSlot.One,last).Accepted,"Start failed");
            s.State.Players[0].Gold-=17;Check(s.Execute(PlayerSlot.One,last).Accepted && s.State.Players[0].Gold==982,"Duplicate start repaid");
            Check(Apply(s,2,1000).Code==ErrorCode.WrongPhase && Ready(s,PlayerSlot.Zero,false).Code==ErrorCode.WrongPhase,"Countdown editable");
            while(s.State.Phase==MatchPhase.Countdown)s.Step();
            Check(Apply(s,2,1000).Code==ErrorCode.WrongPhase && s.State.Players[0].Gold==982,"Playing editable/repaid");
        }
        public static void ZeroCountdownAndZeroGoldStartCorrectly()
        {
            var b=GameContent.CreateDefault(new GameRules(countdownTicks:0));var s=new MatchSimulation(b.WithMatchRules(new MatchRules(1,0)));
            Start(s);Check(s.State.Phase==MatchPhase.Playing && s.State.StartGoldGranted && s.State.Players[0].Gold==0,"Zero start");
            Check(s.State.WavePhase==WavePhase.Preparing && s.State.PreparationRemaining>0,"First wave not prepared");
        }
        public static void RematchKeepsOptionsNewEpochAndWaitsForNewConsent()
        {
            var s=new MatchSimulation(GameContent.CreateDefault());Apply(s,3,BigInteger.Pow(10,40));Start(s);
            var oldReady=Command(s,CommandType.SetReady);ulong oldEpoch=s.State.SessionId;
            s.State.Phase=MatchPhase.Finished;s.State.Outcome=MatchOutcome.Draw;
            Check(s.Execute(PlayerSlot.Zero,Command(s,CommandType.RequestRematch)).Accepted,"Rematch host");
            Check(s.Execute(PlayerSlot.One,Command(s,CommandType.RequestRematch,PlayerSlot.One)).Accepted,"Rematch guest");
            Check(s.State.SessionId==oldEpoch+1 && s.State.ConfigRevision==0 && s.State.RosterRevision==0,"Rematch epoch/revision");
            Check(s.State.MatchRules.WaveCount==3 && s.State.Players[0].Gold==0 && !s.State.StartGoldGranted,"Rematch lost rules/premature payout");
            Check(s.Execute(PlayerSlot.Zero,oldReady).Code==ErrorCode.WrongSession,"Old session crossed rematch");Start(s);
            Check(s.State.Players[0].Gold==BigInteger.Pow(10,40) && s.State.Players[1].Gold==BigInteger.Pow(10,40),"Rematch exact start");
        }
        public static void EpochAndRevisionExhaustionNeverWrap()
        {
            var s=new MatchSimulation(GameContent.CreateDefault());s.State.ConfigRevision=ulong.MaxValue;var before=Stamp(s);
            Check(Apply(s,3,500).Code==ErrorCode.RevisionExhausted && Stamp(s)==before,"Config revision wrapped/mutated");
            Check(Apply(s,10,180).Accepted,"No-op denied at max revision");s.State.RosterRevision=ulong.MaxValue;
            before=Stamp(s);Check(!s.ConfigureGuestConnection(false) && !s.ConfigureTowerAvailability(PlayerSlot.One,63) && Stamp(s)==before,"Roster overflow mutated");
            s=new MatchSimulation(GameContent.CreateDefault(),sessionId:ulong.MaxValue);Start(s);s.State.Phase=MatchPhase.Finished;before=Stamp(s);
            Check(s.Execute(PlayerSlot.Zero,Command(s,CommandType.RequestRematch)).Code==ErrorCode.RevisionExhausted && Stamp(s)==before,"Epoch recycled or vote changed");
        }
        public static void EachSupportedWaveCountUsesIndependentExactLedgers()
        {
            for(int n=1;n<=200;n++)
            {
                var s=new MatchSimulation(GameContent.CreateDefault());Check(Apply(s,n,180+n).Accepted,"Supported wave count rejected");Start(s);
                for(int p=0;p<2;p++)Check(s.State.Players[p].ReservedGoldByWave.Length==n && s.State.Players[p].SpawnedQuarterGoldByWave.Length==n && s.State.Players[p].Gold==180+n,"Ledger/gold mismatch");
                var ledgers=new[]{s.State.Players[0].ReservedGoldByWave,s.State.Players[0].SpawnedQuarterGoldByWave,
                    s.State.Players[1].ReservedGoldByWave,s.State.Players[1].SpawnedQuarterGoldByWave};
                for(int i=0;i<ledgers.Length;i++)for(int j=i+1;j<ledgers.Length;j++)Check(!ReferenceEquals(ledgers[i],ledgers[j]),"Aliased wave ledgers");
                ledgers[0][n-1]=BigInteger.Pow(10,40)+n;
                Check(ledgers[1][n-1]==0 && ledgers[2][n-1]==0 && ledgers[3][n-1]==0,"Final ledger index leaks across owners/types");
            }
        }
        public static void FinalChosenWaveClosesQueueAndFinishes()
        {
            var s=new MatchSimulation(GameContent.CreateDefault(new GameRules(countdownTicks:0)).WithMatchRules(new MatchRules(1,500)));Start(s);
            s.State.WavePhase=WavePhase.Active;s.State.WaveStartPlayTick=s.State.PlayTicks;
            var quote=SendRules.Quote(s.Content,s.State,PlayerSlot.Zero,0);Check(quote.IsValid && quote.WaveId==1,"First selected wave quote");
            var send=Command(s,CommandType.SendUnit);send.ExpectedWave=quote.WaveId;send.EnemyTarget=quote.EnemyTarget;send.ExpectedResearch=quote.ResearchLevel;
            Check(s.Execute(PlayerSlot.Zero,send).Accepted && s.State.Players[0].ReservedGoldByWave[0]==18,"Reservation outside selected ledger");
            s.State.WavePhase=WavePhase.Clearing;Check(SendRules.Quote(s.Content,s.State,PlayerSlot.Zero,0).Code==ErrorCode.WaveCapacity,"Final send cutoff missed");
            Check(s.Execute(PlayerSlot.Zero,Command(s,CommandType.SendUnit)).Code!=ErrorCode.None,"Final send accepted");
            s.State.SpawnOrders.Clear();s.State.Units.Clear();s.State.NeutralCursor[0]=s.State.NeutralCursor[1]=s.Content.Waves[0].TotalCount;
            s.Step();Check(s.State.Phase==MatchPhase.Finished && s.State.WaveId==1,"Advanced past selected final wave");
        }
        public static void ConfiguredContentPreservesAllOtherEconomyRules()
        {
            var b=GameContent.CreateDefault();var c=b.WithMatchRules(new MatchRules(5,BigInteger.Pow(10,30)));
            Check(ReferenceEquals(b.Rules,c.Rules) && ReferenceEquals(b.WaveRules,c.WaveRules),"Economy settings copied/changed");
            Check(c.Towers[4].Price==160 && c.Rules.MineSellRefundPercent==40 && c.SellRefundPercent==65,"Mine/refund changed");
            Check(c.WaveRules.MinimumSendWindowTicks==600 && c.Units[0].CooldownTicks==6 && c.Units[2].CooldownTicks==12,"Wait settings changed");
            for(int w=0;w<5;w++)Check(ReferenceEquals(c.Waves[w],b.Waves[w]),"Authored wave recipe changed");
        }
        public static void NaturalShortMatchesReachTheirConfiguredFinalWave()
        {
            foreach(int count in new[]{1,3})
            {
                var basis=GameContent.CreateDefault(new GameRules(baseHp:10000,countdownTicks:0));
                var sim=new MatchSimulation(basis.WithMatchRules(new MatchRules(count,0)));Start(sim);
                int steps=0,maxWave=1;
                while(sim.State.Phase==MatchPhase.Playing && steps++<50000)
                {
                    maxWave=(int)BigInteger.Max(maxWave,sim.State.WaveId);
                    var forecast=SendQueueForecast.Estimate(sim.Content,sim.State,PlayerSlot.Zero,0);
                    if(forecast.IncludesAdditionalPurchase)Check(forecast.AdditionalWave<=count,"Forecast beyond configured end");
                    if(sim.State.WaveId==count && sim.State.WavePhase==WavePhase.Clearing)
                        Check(!forecast.IncludesAdditionalPurchase,"Final clearing offered new paid packet");
                    sim.Step();
                }
                Check(sim.State.Phase==MatchPhase.Finished && sim.State.WaveId==count && maxWave==count,"Natural match did not end on configured final wave");
                Check(sim.State.Players[0].Hp>0 && sim.State.Players[1].Hp>0,"Fixture ended by destruction rather than wave completion");
            }
        }
        public static void ReadyAndApplyRaceHasOnlySerializedValidOutcomes()
        {
            var s=new MatchSimulation(GameContent.CreateDefault());Ready(s,PlayerSlot.Zero);
            var guest=Command(s,CommandType.SetReady,PlayerSlot.One);var apply=Command(s,CommandType.ApplyMatchRules);
            apply.RuleWaveCount=1;apply.RuleStartGold=999;
            Check(s.Execute(PlayerSlot.Zero,apply).Accepted,"Change-first rejected");
            Check(s.Execute(PlayerSlot.One,guest).Code==ErrorCode.StaleLobbyRevision && s.State.Phase==MatchPhase.Lobby && s.State.Players[0].Gold==0,"Change-first started with stale consent");
            s=new MatchSimulation(GameContent.CreateDefault());Ready(s,PlayerSlot.Zero);guest=Command(s,CommandType.SetReady,PlayerSlot.One);
            apply=Command(s,CommandType.ApplyMatchRules);apply.RuleWaveCount=1;apply.RuleStartGold=999;
            Check(s.Execute(PlayerSlot.One,guest).Accepted && s.State.StartGoldGranted,"Ready-first did not start");
            Check(s.Execute(PlayerSlot.Zero,apply).Code==ErrorCode.WrongPhase && s.Content.MatchStartGold==180 && s.State.Players[0].Gold==180,"Late apply changed started rules");
        }
        public static void TwoHundredWavePlansPreserveEveryMapPrefixAndHash()
        {
            Check(MatchRulesPolicy.MaxWaveCount==200 && MatchRulesPolicy.WavePlanVersion=="authored-then-counter-profiles-boss10-v4"
                && GameContent.SimulationRulesVersion=="defense-duell-core-v2.5","Continuation version contract stale");
            foreach(var basis in MapCatalog.CreateContents(GameContent.CreateDefault()))
            {
                string baseHash=basis.ContentHash;var authored=basis.Waves;
                foreach(int count in new[]{1,10,11,199,200})
                {
                    var full=basis.WithMatchRules(new MatchRules(count,1234));
                    var roundtrip=full.WithMatchRules(new MatchRules(1,99)).WithMatchRules(new MatchRules(count,1234));
                    Check(full.WaveCount==count && full.ContentHash==roundtrip.ContentHash,"Extension history changed map hash");
                    Check(full.BaseContentHash==baseHash && basis.ContentHash==baseHash && basis.WaveCount==10,"Base catalog mutated");
                    for(int w=0;w<count;w++)
                    {
                        var wave=full.Waves[w];
                        if(w<authored.Length)Check(ReferenceEquals(wave,authored[w]),"Authored wave replaced");
                        else Check(wave.HpPercent==authored[authored.Length-1].HpPercent+12*(w-authored.Length+1)
                            && string.Join(",",wave.Counts)==string.Join(",",authored[authored.Length-1].Counts)
                            && string.Join(",",wave.Plan)==string.Join(",",authored[authored.Length-1].Plan),"Continuation plan not deterministic");
                    }
                    // Waves is legacy authored/continuation metadata; GetWave is the actual spawn recipe.
                    for(int actual=1;actual<=count;actual++)
                    {
                        var recipe=full.GetWave(actual);var again=roundtrip.GetWave(actual);
                        Check(string.Join(",",recipe.Counts)==string.Join(",",again.Counts)&&recipe.HpPercent==again.HpPercent,"Effective recipe changed with extension history");
                        if(actual<10)
                        {
                            Check(string.Join(",",recipe.Counts)==string.Join(",",authored[actual-1].Counts)&&recipe.HpPercent==authored[actual-1].HpPercent,"Authored W1..9 changed");
                            for(int k=0;k<4;k++)Check(recipe.HpForKind(k)==((BigInteger)basis.Units[k].Hp*authored[actual-1].HpPercent+99)/100,"Authored effective HP changed");
                        }
                        else
                        {
                            Check(recipe.IsBossWave==(actual%10==0)&&recipe.HasPerKindHp&&recipe.TotalCount>=1&&recipe.TotalCount<=64,"Lategame recipe invariant");
                            for(int k=0;k<4;k++)Check(recipe.HpForKind(k)==again.HpForKind(k),"Effective recipe HP changed with extension history");
                            if(actual%10==0)Check(recipe.Counts[2]==1&&recipe.Plan[recipe.BossUnitIndex]==2,"Unique boss identity");
                        }
                    }
                    if(count>10)Check(full.ContentHash!=basis.WithMatchRules(new MatchRules(count-1,1234)).ContentHash,"Additional wave missing from hash");
                }
                Check(basis.WithMatchRules(new MatchRules(200,180)).Waves[199].HpPercent==2488,"Default final HP percent");
            }
        }
        public static void ContinuationSpawnsUseTheirActualScaledHp()
        {
            // Deliberately position the fixture at the target wave; this is not a full 200-wave playthrough.
            foreach(int wave in new[]{11,199,200})
            {
                var basis=GameContent.CreateDefault(new GameRules(countdownTicks:0));
                var sim=new MatchSimulation(basis.WithMatchRules(new MatchRules(200,0)));Start(sim);
                sim.State.WaveId=wave;sim.State.WavePhase=WavePhase.Active;sim.State.PreparationRemaining=0;
                sim.State.WaveStartPlayTick=sim.State.PlayTicks;
                for(int ticks=0;ticks<40 && sim.State.Units.Count<8;ticks++)sim.Step();
                Check(sim.State.Units.Count==8,"Expected actual neutral spawns for four classes on both boards");
                int seenMask=0;foreach(var unit in sim.State.Units)
                {
                    var effective=sim.Content.GetWave(wave);
                    Check(effective.MatchesNeutral(unit.NeutralRecipeIndex,unit.Kind),"Neutral ordinal did not identify its recipe");
                    BigInteger expected=effective.HpForKind(unit.Kind);
                    Check(unit.Origin==UnitOrigin.NeutralWave && unit.WaveId==wave && unit.Hp==expected,"Spawned HP did not consume continuation definition");
                    seenMask|=1<<unit.Kind;
                }
                Check(seenMask==15,"Continuation did not spawn all four neutral classes");
            }
        }
        public static void FinalTwoHundredthWaveFixtureFinishesAndRematches()
        {
            // Bounded boundary fixture: skip preceding combat, then use actual transition/finish/rematch rules.
            var sim=new MatchSimulation(GameContent.CreateDefault(new GameRules(countdownTicks:0)).WithMatchRules(new MatchRules(200,500)));Start(sim);
            string hash=sim.Content.ContentHash;sim.State.WaveId=199;sim.State.WavePhase=WavePhase.Clearing;
            sim.State.NeutralCursor[0]=sim.State.NeutralCursor[1]=sim.Content.GetWave(199).TotalCount;
            sim.Step();Check(sim.State.Phase==MatchPhase.Playing && sim.State.WaveId==200 && sim.State.WavePhase==WavePhase.Preparing,"Wave199 ended the match early");
            var quote=SendRules.Quote(sim.Content,sim.State,PlayerSlot.Zero,0);Check(quote.IsValid && quote.WaveId==200,"Last wave purchase unavailable");
            var send=Command(sim,CommandType.SendUnit);send.ExpectedWave=quote.WaveId;send.EnemyTarget=quote.EnemyTarget;send.ExpectedResearch=quote.ResearchLevel;
            Check(sim.Execute(PlayerSlot.Zero,send).Accepted && sim.State.Players[0].ReservedGoldByWave[199]==18
                && sim.State.Players[0].ReservedGoldByWave[198]==0,"Wave200 reservation not exact");
            sim.State.WavePhase=WavePhase.Clearing;sim.State.PreparationRemaining=0;
            Check(SendRules.Quote(sim.Content,sim.State,PlayerSlot.Zero,0).Code==ErrorCode.WaveCapacity,"Wave201 offered");
            // Remove the fixture's pending combat; do not claim this as natural delivery/combat evidence.
            sim.State.SpawnOrders.Clear();sim.State.Units.Clear();
            sim.State.NeutralCursor[0]=sim.State.NeutralCursor[1]=sim.Content.GetWave(200).TotalCount;
            sim.Step();Check(sim.State.Phase==MatchPhase.Finished && sim.State.WaveId==200,"Final wave failed to finish");
            ulong epoch=sim.State.SessionId;
            Check(sim.Execute(PlayerSlot.Zero,Command(sim,CommandType.RequestRematch)).Accepted,"Host final-wave rematch");
            Check(sim.Execute(PlayerSlot.One,Command(sim,CommandType.RequestRematch,PlayerSlot.One)).Accepted,"Guest final-wave rematch");
            Check(sim.State.SessionId==epoch+1 && sim.State.Phase==MatchPhase.Lobby && sim.Content.ContentHash==hash,"Rematch lost final plan");
            for(int p=0;p<2;p++)Check(sim.State.Players[p].Gold==0 && sim.State.Players[p].ReservedGoldByWave.Length==200
                && sim.State.Players[p].ReservedGoldByWave[199]==0 && sim.State.Players[p].SpawnedQuarterGoldByWave[199]==0,"Final wave ledger or gold survived reset");
            Start(sim);Check(sim.State.WaveId==1 && sim.State.StartGoldGranted && sim.State.Players[0].Gold==500 && sim.State.Players[1].Gold==500,"Rematch did not start exactly once at wave1");
        }
        public static void ContinuationOverflowRejectsAtomicallyAndNeutralCountStaysBounded()
        {
            var b=GameContent.CreateDefault();var high=new GameContent(b.Rules,b.Towers,b.Units,b.Map,
                new[]{new WaveDefinition(new[]{1,0,0,0},int.MaxValue-12)},b.Research,b.WaveRules);
            Check(high.WithMatchRules(new MatchRules(2,180)).Waves[1].HpPercent==int.MaxValue,"Valid technical HP boundary rejected");
            var sim=new MatchSimulation(high);Ready(sim,PlayerSlot.Zero);string before=Stamp(sim);
            Check(Apply(sim,3,180).Code==ErrorCode.InvalidValue && Stamp(sim)==before,"HP continuation overflow mutated rules");
            bool rejected=false;try{high.WithMatchRules(new MatchRules(3,180));}catch(ArgumentException){rejected=true;}
            Check(rejected,"Overflowing direct configuration accepted");
            foreach(var counts in new[]{new[]{65,0,0,0},new[]{32,33,0,0},new[]{0,0,0,0}})
            {
                rejected=false;try{new WaveDefinition(counts,2488);}catch(ArgumentException){rejected=true;}
                Check(rejected,"Neutral count bound changed");
            }
            Check(new WaveDefinition(new[]{16,16,16,16},2488).TotalCount==64,"Valid neutral count rejected");
        }
        public static IEnumerable<KeyValuePair<string,Action>> All()
        {
            yield return new KeyValuePair<string,Action>(nameof(ContinuationOverflowRejectsAtomicallyAndNeutralCountStaysBounded),ContinuationOverflowRejectsAtomicallyAndNeutralCountStaysBounded);
            yield return new KeyValuePair<string,Action>(nameof(FinalTwoHundredthWaveFixtureFinishesAndRematches),FinalTwoHundredthWaveFixtureFinishesAndRematches);
            yield return new KeyValuePair<string,Action>(nameof(ContinuationSpawnsUseTheirActualScaledHp),ContinuationSpawnsUseTheirActualScaledHp);
            yield return new KeyValuePair<string,Action>(nameof(TwoHundredWavePlansPreserveEveryMapPrefixAndHash),TwoHundredWavePlansPreserveEveryMapPrefixAndHash);
            yield return new KeyValuePair<string,Action>(nameof(NaturalShortMatchesReachTheirConfiguredFinalWave),NaturalShortMatchesReachTheirConfiguredFinalWave);
            yield return new KeyValuePair<string,Action>(nameof(ReadyAndApplyRaceHasOnlySerializedValidOutcomes),ReadyAndApplyRaceHasOnlySerializedValidOutcomes);
            yield return new KeyValuePair<string,Action>(nameof(DefaultPayoutOccursOnlyAtAtomicStart),DefaultPayoutOccursOnlyAtAtomicStart);
            yield return new KeyValuePair<string,Action>(nameof(ExactLargeGoldNeverNarrowsOrMutatesBase),ExactLargeGoldNeverNarrowsOrMutatesBase);
            yield return new KeyValuePair<string,Action>(nameof(PrefixCanExtendAgainAndHashIsDeterministic),PrefixCanExtendAgainAndHashIsDeterministic);
            yield return new KeyValuePair<string,Action>(nameof(ShortAuthoredCatalogContinuesFromItsLastWave),ShortAuthoredCatalogContinuesFromItsLastWave);
            yield return new KeyValuePair<string,Action>(nameof(CanonicalDecimalParsingAndMagnitudeBoundary),CanonicalDecimalParsingAndMagnitudeBoundary);
            yield return new KeyValuePair<string,Action>(nameof(InvalidGuestAndStaleApplyAreAtomic),InvalidGuestAndStaleApplyAreAtomic);
            yield return new KeyValuePair<string,Action>(nameof(ChangeResetsReadyNoopPreservesIt),ChangeResetsReadyNoopPreservesIt);
            yield return new KeyValuePair<string,Action>(nameof(ReadyBindsBothDisplayedRevisions),ReadyBindsBothDisplayedRevisions);
            yield return new KeyValuePair<string,Action>(nameof(MissingGuestAndRosterCallbacksNeverPay),MissingGuestAndRosterCallbacksNeverPay);
            yield return new KeyValuePair<string,Action>(nameof(MaskChangeBindsConsentAndNoopDoesNotReset),MaskChangeBindsConsentAndNoopDoesNotReset);
            yield return new KeyValuePair<string,Action>(nameof(ReplayedConsentCannotUndoWithdrawal),ReplayedConsentCannotUndoWithdrawal);
            yield return new KeyValuePair<string,Action>(nameof(ReplayIdentityIncludesExactRulePayloadAndRevisions),ReplayIdentityIncludesExactRulePayloadAndRevisions);
            yield return new KeyValuePair<string,Action>(nameof(UnusedCommandFieldsAreRejected),UnusedCommandFieldsAreRejected);
            yield return new KeyValuePair<string,Action>(nameof(StartLocksRulesAndPaysOnlyOnce),StartLocksRulesAndPaysOnlyOnce);
            yield return new KeyValuePair<string,Action>(nameof(ZeroCountdownAndZeroGoldStartCorrectly),ZeroCountdownAndZeroGoldStartCorrectly);
            yield return new KeyValuePair<string,Action>(nameof(RematchKeepsOptionsNewEpochAndWaitsForNewConsent),RematchKeepsOptionsNewEpochAndWaitsForNewConsent);
            yield return new KeyValuePair<string,Action>(nameof(EpochAndRevisionExhaustionNeverWrap),EpochAndRevisionExhaustionNeverWrap);
            yield return new KeyValuePair<string,Action>(nameof(EachSupportedWaveCountUsesIndependentExactLedgers),EachSupportedWaveCountUsesIndependentExactLedgers);
            yield return new KeyValuePair<string,Action>(nameof(FinalChosenWaveClosesQueueAndFinishes),FinalChosenWaveClosesQueueAndFinishes);
            yield return new KeyValuePair<string,Action>(nameof(ConfiguredContentPreservesAllOtherEconomyRules),ConfiguredContentPreservesAllOtherEconomyRules);
        }
    }
#if UNITY_EDITOR
    public sealed class HostRules14Tests
    {
        [Test] public void ContinuationOverflowRejectsAtomicallyAndNeutralCountStaysBounded() => HostRules14Scenarios.ContinuationOverflowRejectsAtomicallyAndNeutralCountStaysBounded();
        [Test] public void FinalTwoHundredthWaveFixtureFinishesAndRematches() => HostRules14Scenarios.FinalTwoHundredthWaveFixtureFinishesAndRematches();
        [Test] public void ContinuationSpawnsUseTheirActualScaledHp() => HostRules14Scenarios.ContinuationSpawnsUseTheirActualScaledHp();
        [Test] public void TwoHundredWavePlansPreserveEveryMapPrefixAndHash() => HostRules14Scenarios.TwoHundredWavePlansPreserveEveryMapPrefixAndHash();
        [Test] public void NaturalShortMatchesReachTheirConfiguredFinalWave() => HostRules14Scenarios.NaturalShortMatchesReachTheirConfiguredFinalWave();
        [Test] public void ReadyAndApplyRaceHasOnlySerializedValidOutcomes() => HostRules14Scenarios.ReadyAndApplyRaceHasOnlySerializedValidOutcomes();
        [Test] public void DefaultPayoutOccursOnlyAtAtomicStart() => HostRules14Scenarios.DefaultPayoutOccursOnlyAtAtomicStart();
        [Test] public void ExactLargeGoldNeverNarrowsOrMutatesBase() => HostRules14Scenarios.ExactLargeGoldNeverNarrowsOrMutatesBase();
        [Test] public void PrefixCanExtendAgainAndHashIsDeterministic() => HostRules14Scenarios.PrefixCanExtendAgainAndHashIsDeterministic();
        [Test] public void ShortAuthoredCatalogContinuesFromItsLastWave() => HostRules14Scenarios.ShortAuthoredCatalogContinuesFromItsLastWave();
        [Test] public void CanonicalDecimalParsingAndMagnitudeBoundary() => HostRules14Scenarios.CanonicalDecimalParsingAndMagnitudeBoundary();
        [Test] public void InvalidGuestAndStaleApplyAreAtomic() => HostRules14Scenarios.InvalidGuestAndStaleApplyAreAtomic();
        [Test] public void ChangeResetsReadyNoopPreservesIt() => HostRules14Scenarios.ChangeResetsReadyNoopPreservesIt();
        [Test] public void ReadyBindsBothDisplayedRevisions() => HostRules14Scenarios.ReadyBindsBothDisplayedRevisions();
        [Test] public void MissingGuestAndRosterCallbacksNeverPay() => HostRules14Scenarios.MissingGuestAndRosterCallbacksNeverPay();
        [Test] public void MaskChangeBindsConsentAndNoopDoesNotReset() => HostRules14Scenarios.MaskChangeBindsConsentAndNoopDoesNotReset();
        [Test] public void ReplayedConsentCannotUndoWithdrawal() => HostRules14Scenarios.ReplayedConsentCannotUndoWithdrawal();
        [Test] public void ReplayIdentityIncludesExactRulePayloadAndRevisions() => HostRules14Scenarios.ReplayIdentityIncludesExactRulePayloadAndRevisions();
        [Test] public void UnusedCommandFieldsAreRejected() => HostRules14Scenarios.UnusedCommandFieldsAreRejected();
        [Test] public void StartLocksRulesAndPaysOnlyOnce() => HostRules14Scenarios.StartLocksRulesAndPaysOnlyOnce();
        [Test] public void ZeroCountdownAndZeroGoldStartCorrectly() => HostRules14Scenarios.ZeroCountdownAndZeroGoldStartCorrectly();
        [Test] public void RematchKeepsOptionsNewEpochAndWaitsForNewConsent() => HostRules14Scenarios.RematchKeepsOptionsNewEpochAndWaitsForNewConsent();
        [Test] public void EpochAndRevisionExhaustionNeverWrap() => HostRules14Scenarios.EpochAndRevisionExhaustionNeverWrap();
        [Test] public void EachSupportedWaveCountUsesIndependentExactLedgers() => HostRules14Scenarios.EachSupportedWaveCountUsesIndependentExactLedgers();
        [Test] public void FinalChosenWaveClosesQueueAndFinishes() => HostRules14Scenarios.FinalChosenWaveClosesQueueAndFinishes();
        [Test] public void ConfiguredContentPreservesAllOtherEconomyRules() => HostRules14Scenarios.ConfiguredContentPreservesAllOtherEconomyRules();
    }
#endif
}
