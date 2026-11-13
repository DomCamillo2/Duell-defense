using System;
using System.Numerics;
using System.Collections.Generic;
using System.Text;
using DefenseDuell.Core;

namespace DefenseDuell.Tests
{
    /// <summary>Framework-independent regression scenarios shared with the offline command-line harness.</summary>
    public static class CoreScenarios
    {
        public static IEnumerable<KeyValuePair<string, Action>> All()
        {
            foreach (var scenario in UnlimitedCoreScenarios.All()) yield return scenario;
            foreach (var scenario in WaveCoreScenarios.All()) yield return scenario;
            foreach (var scenario in Legacy18Scenarios())
                if (scenario.Key != nameof(SpeedAgreementLeavesGameTimeRulesUnchanged)) yield return scenario;
            // The former one-game-tick-per-Step equivalence is superseded by the explicit
            // two-clock ActiveSpeedRunsAllGameplayRulesTogether scenario above.
        }
        public static IEnumerable<KeyValuePair<string, Action>> Legacy18Scenarios()
        {
            yield return Case(nameof(DefinitionsAreImmutableAndHashed), DefinitionsAreImmutableAndHashed);
            yield return Case(nameof(InvalidDefinitionsFailEarly), InvalidDefinitionsFailEarly);
            yield return Case(nameof(ReadyCountdownAndPhaseValidation), ReadyCountdownAndPhaseValidation);
            yield return Case(nameof(SpeedVotesRequireBothAndWithdrawImmediately), SpeedVotesRequireBothAndWithdrawImmediately);
            yield return Case(nameof(SpeedVotesRejectMalformedPayloadsAndWrongPhases), SpeedVotesRejectMalformedPayloadsAndWrongPhases);
            yield return Case(nameof(SpeedVoteReceiptsPreserveLatestConsent), SpeedVoteReceiptsPreserveLatestConsent);
            yield return Case(nameof(SpeedVoteRateLimitsAndStaleReceiptsAreStable), SpeedVoteRateLimitsAndStaleReceiptsAreStable);
            yield return Case(nameof(SpeedVotesResetAcrossResultsAndEpochs), SpeedVotesResetAcrossResultsAndEpochs);
            yield return Case(nameof(SpeedAgreementLeavesGameTimeRulesUnchanged), SpeedAgreementLeavesGameTimeRulesUnchanged);
            yield return Case(nameof(DuplicateAndStaleCommandsCannotPurchaseTwice), DuplicateAndStaleCommandsCannotPurchaseTwice);
            yield return Case(nameof(ForeignTowerAndInvalidPlacementLeaveStateUntouched), ForeignTowerAndInvalidPlacementLeaveStateUntouched);
            yield return Case(nameof(UpgradesAndRefundNeverCreateGold), UpgradesAndRefundNeverCreateGold);
            yield return Case(nameof(QueuePoolHintsDoNotCapPaidSpawns), QueuePoolHintsDoNotCapPaidSpawns);
            yield return Case(nameof(SwarmCountAndCooldownAreExact), SwarmCountAndCooldownAreExact);
            yield return Case(nameof(RapidTroopPurchasesSpendGoldExactlyOnce), RapidTroopPurchasesSpendGoldExactlyOnce);
            yield return Case(nameof(PaidGroupsRespectClassCooldownAndMixedFifo), PaidGroupsRespectClassCooldownAndMixedFifo);
            yield return Case(nameof(FieldPoolHintsDoNotBlockGroupStart), FieldPoolHintsDoNotBlockGroupStart);
            yield return Case(nameof(OrdersBeyondEightyArePaidAndRematchClearsThem), OrdersBeyondEightyArePaidAndRematchClearsThem);
            yield return Case(nameof(MapDimensionsAndThirtyTwoSlotsAreValidated), MapDimensionsAndThirtyTwoSlotsAreValidated);
            yield return Case(nameof(RapidSendRateLimitReceiptRemainsRejected), RapidSendRateLimitReceiptRemainsRejected);
            yield return Case(nameof(KillBountyPaysDefenderOnceIncludingSplash), KillBountyPaysDefenderOnceIncludingSplash);
            yield return Case(nameof(SimultaneousKillsAndLeaksKeepPayoutsDistinct), SimultaneousKillsAndLeaksKeepPayoutsDistinct);
            yield return Case(nameof(KillBountyIsUncappedAndSalesDoNotPayExtra), KillBountyIsUncappedAndSalesDoNotPayExtra);
            yield return Case(nameof(BountyDefinitionsAreValidatedAndHashed), BountyDefinitionsAreValidatedAndHashed);
            yield return Case(nameof(DedicatedMineSlotsAreImmutableAndEnforced), DedicatedMineSlotsAreImmutableAndEnforced);
            yield return Case(nameof(FreePlacementIsContinuousAtomicAndIdempotent), FreePlacementIsContinuousAtomicAndIdempotent);
            yield return Case(nameof(FreePlacementGeometryHasExactBoundaries), FreePlacementGeometryHasExactBoundaries);
            yield return Case(nameof(FreePlacementCapacityIsIndependentOfSiteCount), FreePlacementCapacityIsIndependentOfSiteCount);
            yield return Case(nameof(FreeTowerCombatUsesActualPosition), FreeTowerCombatUsesActualPosition);
            yield return Case(nameof(FreePlacementDefinitionDataIsImmutableAndHashed), FreePlacementDefinitionDataIsImmutableAndHashed);
            yield return Case(nameof(UpgradePathsHaveSixCanonicalVariants), UpgradePathsHaveSixCanonicalVariants);
            yield return Case(nameof(UpgradeOrdersAreAtomicAndPathSpecific), UpgradeOrdersAreAtomicAndPathSpecific);
            yield return Case(nameof(UpgradeQuotesIgnoreGoldAndRejectInvalidState), UpgradeQuotesIgnoreGoldAndRejectInvalidState);
            yield return Case(nameof(UpgradeReplayAndRematchPreservePathIntegrity), UpgradeReplayAndRematchPreservePathIntegrity);
            yield return Case(nameof(UpgradeCombatUsesChosenStatsAndBaseInterval), UpgradeCombatUsesChosenStatsAndBaseInterval);
            yield return Case(nameof(UpgradeDefinitionsRejectContradictoryPaths), UpgradeDefinitionsRejectContradictoryPaths);
            yield return Case(nameof(FreeMinesRespectGeometryWithoutClassCaps), FreeMinesRespectGeometryWithoutClassCaps);
            yield return Case(nameof(FormerMineSitesAreOrdinaryFreeGround), FormerMineSitesAreOrdinaryFreeGround);
            yield return Case(nameof(ArmorAndPiercingApplyDuringCombat), ArmorAndPiercingApplyDuringCombat);
            yield return Case(nameof(TargetPriorityUsesDistanceThenStableId), TargetPriorityUsesDistanceThenStableId);
            yield return Case(nameof(FrostIsBoundedAndExpires), FrostIsBoundedAndExpires);
            yield return Case(nameof(CannonHitsOnlyNearbyEnemies), CannonHitsOnlyNearbyEnemies);
            yield return Case(nameof(DeadAndEscapedUnitsAreResolvedOnce), DeadAndEscapedUnitsAreResolvedOnce);
            yield return Case(nameof(LethalShotPreventsSameTickBaseDamage), LethalShotPreventsSameTickBaseDamage);
            yield return Case(nameof(FixedTickIncomeAndFractionalMovementAreExact), FixedTickIncomeAndFractionalMovementAreExact);
            yield return Case(nameof(SimultaneousDestructionDraws), SimultaneousDestructionDraws);
            yield return Case(nameof(LastWaveAndFinishedOutcomeAreStable), LastWaveAndFinishedOutcomeAreStable);
            yield return Case(nameof(RematchRequiresBothAndResetsEpoch), RematchRequiresBothAndResetsEpoch);
            yield return Case(nameof(AbortHasNoWinner), AbortHasNoWinner);
            yield return Case(nameof(RateLimitRejectsWithoutMutationAndRecovers), RateLimitRejectsWithoutMutationAndRecovers);
            yield return Case(nameof(RandomCommandsPreserveEconomyAndGeometry), RandomCommandsPreserveEconomyAndGeometry);
            yield return Case(nameof(BotsAreReproducibleAndUseNormalRules), BotsAreReproducibleAndUseNormalRules);
            yield return Case(nameof(ArmiesGrowAboveLegacyFieldCapacity), ArmiesGrowAboveLegacyFieldCapacity);
            yield return Case(nameof(MapCatalogIsDistinctValidAndClear), MapCatalogIsDistinctValidAndClear);
            yield return Case(nameof(MapMetadataAndEconomyParticipateInHash), MapMetadataAndEconomyParticipateInHash);
            yield return Case(nameof(MapCatalogPreservesTemplateMapAndBalance), MapCatalogPreservesTemplateMapAndBalance);
            yield return Case(nameof(InvalidMapAndMineDefinitionsFailEarly), InvalidMapAndMineDefinitionsFailEarly);
            yield return Case(nameof(MinesRespectAffordabilityWithoutClassLimits), MinesRespectAffordabilityWithoutClassLimits);
            yield return Case(nameof(MineLevelsPayExactIncomeAndNeverShoot), MineLevelsPayExactIncomeAndNeverShoot);
            yield return Case(nameof(MineSaleStopsIncomeAndDuplicatesCannotProfit), MineSaleStopsIncomeAndDuplicatesCannotProfit);
            yield return Case(nameof(MineRematchRestoresBaseEconomy), MineRematchRestoresBaseEconomy);
            yield return Case(nameof(FullMatchesFinishOnEveryMapWithoutSideBias), FullMatchesFinishOnEveryMapWithoutSideBias);
            yield return Case(nameof(EconomyStrategyHasReproducibleMatchEvidence), EconomyStrategyHasReproducibleMatchEvidence);
        }
        private static KeyValuePair<string, Action> Case(string name, Action action) => new KeyValuePair<string, Action>(name, action);
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void Equal(BigInteger actual, BigInteger expected, string message) => Check(actual == expected, message + " actual=" + actual + " expected=" + expected);
        private static void Equal<T>(T actual, T expected, string message) => Check(EqualityComparer<T>.Default.Equals(actual, expected), message + " (actual=" + actual + ", expected=" + expected + ")");
        private static MatchSimulation Started(GameContent content = null)
        {
            var sim = new MatchSimulation(content ?? RuleContent(), 123, 9001);
            Command(sim, PlayerSlot.Zero, CommandType.SetReady);
            Command(sim, PlayerSlot.One, CommandType.SetReady);
            while (sim.State.Phase == MatchPhase.Countdown) sim.Step();
            ActivateArithmeticFixture(sim);
            return sim;
        }
        public static CommandResult Command(MatchSimulation sim, PlayerSlot p, CommandType type, int kind = 0, int target = -1,
            int positionX = 0, int positionY = 0)
            => WaveCoreScenarios.Command(sim,p,type,kind,target,positionX,positionY);
        private static GameContent RichContent(int maxUnits = 80, int maxQueue = 8)
            => RuleContent(new GameRules(startGold: 10000, maxUnitsPerField: maxUnits, maxSpawnOrdersPerPlayer: maxQueue));
        // Focused arithmetic/targeting tests use a frozen small map. Catalog geometry has its own tests.
        private static GameContent RuleContent(GameRules rules = null)
        {
            GameContent content = GameContent.CreateDefault(rules);
            var map = new MapDefinition(new[] {
                new MapPoint(30, 130), new MapPoint(780, 130), new MapPoint(780, 300),
                new MapPoint(220, 300), new MapPoint(220, 470), new MapPoint(970, 470)
            }, new[] {
                new MapPoint(120, 55), new MapPoint(340, 55), new MapPoint(560, 55), new MapPoint(900, 130),
                new MapPoint(675, 220), new MapPoint(450, 220), new MapPoint(240, 215), new MapPoint(100, 300),
                new MapPoint(330, 390), new MapPoint(550, 390), new MapPoint(770, 390), new MapPoint(850, 550)
            });
            return new GameContent(content.Rules, content.Towers, content.Units, map, waveRules:new WaveRules(waveBudget:10000,minimumSendWindowTicks:12000));
        }
        private static void ActivateArithmeticFixture(MatchSimulation sim)
        {
            // Isolate retained combat/economy arithmetic from automatic neutral arrivals.
            sim.State.WavePhase=WavePhase.Active;sim.State.PreparationRemaining=0;
            sim.State.NeutralCursor[0]=sim.State.NeutralCursor[1]=sim.Content.GetWave(sim.State.WaveId).TotalCount;
        }
        private static void FinishAtLastWave(MatchSimulation sim)
        {
            sim.State.WaveId=sim.Content.WaveCount;sim.State.WavePhase=WavePhase.Clearing;
            sim.State.Units.Clear();sim.State.SpawnOrders.Clear();
            sim.State.NeutralCursor[0]=sim.State.NeutralCursor[1]=sim.Content.GetWave(sim.State.WaveId).TotalCount;
            sim.Step();
        }
        private static void Steps(MatchSimulation sim, int count) { for (int i = 0; i < count; i++) sim.Step(); }
        private static UnitState AddUnit(MatchSimulation sim, int id, PlayerSlot defender, int kind, int distance, int hp = -1)
        {
            var unit = new UnitState { Id = id, Defender = defender, Kind = kind, Distance = distance,
                Hp = hp < 0 ? sim.Content.Units[kind].Hp : hp, SlowPermille = 1000 };
            sim.State.Units.Add(unit); return unit;
        }
        public static string Fingerprint(MatchState s, bool includeSpeedVotes = true)
        {
            var b = new StringBuilder();
            b.Append(s.SessionId).Append(',').Append(s.Seed).Append(',').Append(s.Tick).Append(',').Append(s.PlayTicks)
                .Append(',').Append(s.CountdownRemaining).Append(',').Append(s.Phase).Append(',').Append(s.Outcome);
            if (includeSpeedVotes) b.Append(",speed=").Append(s.SimulationSpeed);
            foreach (PlayerState p in s.Players)
            {
                b.Append('|').Append(p.Gold).Append(',').Append(p.Hp).Append(',').Append(p.Ready).Append(',').Append(p.Rematch);
                if (includeSpeedVotes) b.Append(',').Append(p.SpeedReady);
                foreach (int c in p.NextSendTicks) b.Append(',').Append(c);
            }
            foreach (TowerState t in s.Towers) b.Append("|T").Append(t.Id).Append(',').Append(t.Owner).Append(',').Append(t.Slot)
                .Append(',').Append(t.Kind).Append(',').Append(t.Level).Append(',').Append(t.CooldownTicks).Append(',').Append(t.Invested)
                .Append(',').Append(t.SiteId).Append(',').Append(t.Position.X).Append(',').Append(t.Position.Y)
                .Append(',').Append(t.DamageLevel).Append(',').Append(t.RangeLevel);
            foreach (UnitState u in s.Units) b.Append("|U").Append(u.Id).Append(',').Append(u.Defender).Append(',').Append(u.Kind)
                .Append(',').Append(u.Hp).Append(',').Append(u.Distance).Append(',').Append(u.SlowPermille)
                .Append(',').Append(u.SlowUntilTick).Append(',').Append(u.MovementRemainder);
            foreach (SpawnOrder q in s.SpawnOrders) b.Append("|Q").Append(q.Sender).Append(',').Append(q.Kind).Append(',').Append(q.Remaining).Append(',').Append(q.NextTick);
            b.Append("|V19").Append(WaveCoreScenarios.Fingerprint(s,includeSpeedVotes));
            return b.ToString();
        }

        public static void DefinitionsAreImmutableAndHashed()
        {
            GameContent a = GameContent.CreateDefault(), b = GameContent.CreateDefault();
            Equal(a.ContentHash, b.ContentHash, "Identical content must share a hash");
            Check(a.ContentHash.Length == 64, "SHA-256 hex expected");
            a.Towers[0] = a.Towers[1];
            a.Units[0] = a.Units[2];
            a.Map.BuildSlots[0] = new MapPoint(0, 0);
            a.Towers[0].Upgrades[0] = new UpgradeDefinition(1, 999, 999, 1);
            Equal(a.Towers[0].Name, b.Towers[0].Name, "Array mutation escaped immutable content");
            Equal(a.Units[0].Name, b.Units[0].Name, "Unit array mutation escaped immutable content");
            Equal(a.Map.BuildSlots[0].X, b.Map.BuildSlots[0].X, "Map array mutation escaped immutable content");
            Equal(a.Towers[0].Upgrades[0].Damage, b.Towers[0].Upgrades[0].Damage, "Upgrade array mutation escaped immutable content");
            Check(a.ContentHash != GameContent.CreateDefault(new GameRules(passiveGoldPerSecond: 7)).ContentHash, "Economy rule missing from hash");
            Check(a.ContentHash != GameContent.CreateDefault(new GameRules(minimumSlowPermille: 620)).ContentHash, "Frost rule missing from hash");
        }

        public static void InvalidDefinitionsFailEarly()
        {
            int failures = 0;
            try { GameContent.CreateDefault(new GameRules(maxUnitsPerField: 81)); } catch (ArgumentException) { failures++; }
            try { GameContent.CreateDefault(new GameRules(sellRefundPercent: 101)); } catch (ArgumentException) { failures++; }
            try { new MapDefinition(new[] { new MapPoint(1, 1), new MapPoint(1, 1) }, new MapPoint[12]); } catch (ArgumentException) { failures++; }
            try { GameContent.CreateDefault(new GameRules(minimumSlowPermille: 499)); } catch (ArgumentException) { failures++; }
            try { GameContent.CreateDefault(new GameRules(maxSpawnOrdersPerPlayer: 81)); } catch (ArgumentException) { failures++; }
            Equal(failures, 5, "Invalid data must fail before a session starts");
        }

        public static void ReadyCountdownAndPhaseValidation()
        {
            var sim = new MatchSimulation(GameContent.CreateDefault());
            CheckStartWallets(sim, false);
            string before = Fingerprint(sim.State);
            Equal(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0).Code, ErrorCode.WrongPhase, "Lobby allowed purchase");
            Equal(Fingerprint(sim.State), before, "Rejected lobby purchase changed state");
            Command(sim, PlayerSlot.Zero, CommandType.SetReady);
            Equal(sim.State.Phase, MatchPhase.Lobby, "One ready player started match");
            CheckStartWallets(sim, false);
            Command(sim, PlayerSlot.One, CommandType.SetReady);
            Equal(sim.State.Phase, MatchPhase.Countdown, "Both ready should start countdown");
            CheckStartWallets(sim, true);
            Equal(Command(sim, PlayerSlot.One, CommandType.SetReady).Code, ErrorCode.WrongPhase, "Countdown accepted another ready");
            CheckStartWallets(sim, true);
            Steps(sim, 59); Equal(sim.State.Phase, MatchPhase.Countdown, "Countdown too short");
            sim.Step(); Equal(sim.State.Phase, MatchPhase.Playing, "Countdown did not end synchronously");
            Equal(sim.State.PlayTicks, 0, "Countdown consumed match duration");
            CheckStartWallets(sim, true);
        }

        private static void CheckStartWallets(MatchSimulation sim, bool granted)
        {
            Equal(sim.State.StartGoldGranted, granted, "Start-gold grant marker disagrees with start state");
            for (int p = 0; p < 2; p++)
                Equal(sim.State.Players[p].Gold, granted ? sim.Content.MatchStartGold : BigInteger.Zero,
                    "Start gold must be paid exactly once after the final ready, to both players");
        }

        private static CommandEnvelope SpeedVote(MatchSimulation sim, PlayerSlot player, int kind = 1)
            => new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = sim.LastSequence(player) + 1,
                Type = CommandType.SetSpeedVote, Kind = kind, Target = -1 };

        public static void SpeedVotesRequireBothAndWithdrawImmediately()
        {
            Equal((int)CommandType.RequestRematch, 5, "Existing command numbering changed");
            Equal((int)CommandType.SetSpeedVote, 6, "Speed vote was not appended");
            Equal(new MatchState().SimulationSpeed, 1, "Empty state did not default to normal speed");
            for (int first = 0; first < 2; first++)
            {
                var sim = Started();
                sim.State.Players[0].Gold = sim.State.Players[1].Gold = 0;
                string gameplay = Fingerprint(sim.State, false);
                var a = (PlayerSlot)first; var b = (PlayerSlot)(1 - first);
                Check(!sim.State.Players[0].SpeedReady && !sim.State.Players[1].SpeedReady, "Fresh match inherited consent");
                Equal(sim.State.SimulationSpeed, 1, "Fresh match accelerated");
                Check(Command(sim, a, CommandType.SetSpeedVote, 0).Accepted, "Withdrawing an absent vote failed");
                Check(Command(sim, a, CommandType.SetSpeedVote, 1).Accepted, "First consent needed gold");
                Check(sim.State.Players[first].SpeedReady && !sim.State.Players[1 - first].SpeedReady, "A vote changed the other player");
                Equal(sim.State.SimulationSpeed, 1, "One player accelerated the match");
                Check(Command(sim, a, CommandType.SetSpeedVote, 1).Accepted, "Repeated explicit approval failed");
                Equal(sim.State.SimulationSpeed, 1, "Approval toggled or supplied another player's consent");
                Check(Command(sim, b, CommandType.SetSpeedVote, 1).Accepted, "Second consent failed");
                Equal(sim.State.SimulationSpeed, 2, "Two consents did not enable 2x");
                Check(Command(sim, a, CommandType.SetSpeedVote, 0).Accepted, "First player could not withdraw");
                Equal(sim.State.SimulationSpeed, 1, "Withdrawal required a later simulation tick");
                Check(!sim.State.Players[first].SpeedReady && sim.State.Players[1 - first].SpeedReady, "Withdrawal cleared someone else's vote");
                Check(Command(sim, a, CommandType.SetSpeedVote, 1).Accepted, "Renewed consent failed");
                Equal(sim.State.SimulationSpeed, 2, "Renewed joint agreement was ignored");
                Check(Command(sim, b, CommandType.SetSpeedVote, 0).Accepted, "Second player could not withdraw");
                Equal(sim.State.SimulationSpeed, 1, "Either player must be able to restore 1x immediately");
                Equal(Fingerprint(sim.State, false), gameplay, "Voting changed gold, ticks, timers or gameplay objects");
            }
        }

        public static void SpeedVotesRejectMalformedPayloadsAndWrongPhases()
        {
            foreach (var phase in new[] { MatchPhase.Lobby, MatchPhase.Countdown, MatchPhase.Finished, MatchPhase.Aborted })
            {
                var sim = Started(); sim.State.Phase = phase;
                // Even a snapshot retaining old votes must never accelerate countdown/results.
                sim.State.Players[0].SpeedReady = sim.State.Players[1].SpeedReady = true;
                Equal(sim.State.SimulationSpeed, 1, "Non-playing phase exposed 2x");
                string before = Fingerprint(sim.State);
                for (int p = 0; p < 2; p++) for (int kind = 0; kind < 2; kind++)
                    Equal(Command(sim, (PlayerSlot)p, CommandType.SetSpeedVote, kind).Code, ErrorCode.WrongPhase, "Wrong phase accepted a speed vote");
                Equal(Fingerprint(sim.State), before, "Wrong-phase vote changed canonical state");
            }
            var playing = Started();
            Command(playing, PlayerSlot.Zero, CommandType.SetSpeedVote, 1);
            Command(playing, PlayerSlot.One, CommandType.SetSpeedVote, 1);
            string baseline = Fingerprint(playing.State);
            void Reject(CommandEnvelope command, ErrorCode code, PlayerSlot sender = PlayerSlot.Zero)
            {
                Equal(playing.Execute(sender, command).Code, code, "Malformed speed intent was not rejected correctly");
                Equal(Fingerprint(playing.State), baseline, "Malformed speed intent changed votes or gameplay");
            }
            foreach (int kind in new[] { int.MinValue, -1, 2, 256, int.MaxValue })
                Reject(SpeedVote(playing, PlayerSlot.Zero, kind), ErrorCode.InvalidKind);
            foreach (int target in new[] { int.MinValue, -2, 0, 1, int.MaxValue })
            {
                var command = SpeedVote(playing, PlayerSlot.Zero, 0); command.Target = target;
                Reject(command, ErrorCode.InvalidTarget);
            }
            foreach (int position in new[] { int.MinValue, -1, 1, int.MaxValue })
            {
                var command = SpeedVote(playing, PlayerSlot.Zero, 0); command.PositionX = position;
                Reject(command, ErrorCode.InvalidValue);
                command = SpeedVote(playing, PlayerSlot.Zero, 0); command.PositionY = position;
                Reject(command, ErrorCode.InvalidValue);
            }
            foreach (int type in new[] { -1, (int)CommandType.SetAttackTarget + 1, int.MaxValue })
            {
                var command = SpeedVote(playing, PlayerSlot.Zero); command.Type = (CommandType)type;
                Reject(command, ErrorCode.InvalidValue);
            }
            ulong sequence = playing.LastSequence(PlayerSlot.Zero);
            var invalid = SpeedVote(playing, PlayerSlot.Zero, 0);
            Reject(invalid, ErrorCode.InvalidPlayer, (PlayerSlot)2);
            Reject(invalid, ErrorCode.InvalidPlayer, (PlayerSlot)255);
            invalid.SessionId--; Reject(invalid, ErrorCode.WrongSession);
            invalid = SpeedVote(playing, PlayerSlot.Zero, 0); invalid.Sequence = 0;
            Reject(invalid, ErrorCode.InvalidValue);
            Equal(playing.LastSequence(PlayerSlot.Zero), sequence, "Invalid transport metadata consumed the player's sequence");
        }

        public static void SpeedVoteReceiptsPreserveLatestConsent()
        {
            var sim = Started();
            var approval = SpeedVote(sim, PlayerSlot.Zero);
            CommandResult accepted = sim.Execute(PlayerSlot.Zero, approval);
            Check(accepted.Accepted, "Approval fixture failed");
            Command(sim, PlayerSlot.One, CommandType.SetSpeedVote, 1);
            var withdrawal = SpeedVote(sim, PlayerSlot.Zero, 0);
            Check(sim.Execute(PlayerSlot.Zero, withdrawal).Accepted, "Withdrawal fixture failed");
            sim.Step();
            string before = Fingerprint(sim.State);
            var replay = sim.Execute(PlayerSlot.Zero, approval);
            Check(replay.Accepted, "Accepted receipt was not replayed");
            Equal(replay.Tick, accepted.Tick, "Replay fabricated a new receipt tick");
            Equal(replay.Sequence, accepted.Sequence, "Replay changed request identity");
            Equal(Fingerprint(sim.State), before, "Old approval re-enabled withdrawn consent");
            for (int mutation = 0; mutation < 4; mutation++)
            {
                var changed = approval;
                if (mutation == 0) changed.Kind = 0;
                if (mutation == 1) changed.Target = 0;
                if (mutation == 2) changed.PositionX = 1;
                if (mutation == 3) changed.PositionY = 1;
                Equal(sim.Execute(PlayerSlot.Zero, changed).Code, ErrorCode.Duplicate, "Changed vote payload reused an accepted ID");
                Equal(Fingerprint(sim.State), before, "Changed duplicate altered latest consent");
            }
            Check(Command(sim, PlayerSlot.Zero, CommandType.SetSpeedVote, 1).Accepted, "Fresh approval failed");
            before = Fingerprint(sim.State);
            Check(sim.Execute(PlayerSlot.Zero, withdrawal).Accepted, "Withdrawal receipt was not replayed");
            Equal(Fingerprint(sim.State), before, "Old withdrawal revoked newer consent");
            var malformed = SpeedVote(sim, PlayerSlot.Zero, 2);
            Equal(sim.Execute(PlayerSlot.Zero, malformed).Code, ErrorCode.InvalidKind, "Malformed vote fixture failed");
            Equal(sim.Execute(PlayerSlot.Zero, malformed).Code, ErrorCode.InvalidKind, "Rejected receipt changed on replay");
            malformed.Kind = 0;
            Equal(sim.Execute(PlayerSlot.Zero, malformed).Code, ErrorCode.Duplicate, "Corrected payload reused a rejected request ID");
            Equal(Fingerprint(sim.State), before, "Rejected vote replay changed canonical state");
        }

        public static void SpeedVoteRateLimitsAndStaleReceiptsAreStable()
        {
            var sim = Started();
            Command(sim, PlayerSlot.One, CommandType.SetSpeedVote, 1);
            var approval = SpeedVote(sim, PlayerSlot.Zero);
            Check(sim.Execute(PlayerSlot.Zero, approval).Accepted, "Rate-limit approval fixture failed");
            for (int i = 0; i < 200; i++) Check(sim.Execute(PlayerSlot.Zero, approval).Accepted, "Duplicate vote receipt failed");
            for (int i = 1; i < sim.Content.Rules.MaxCommandsPerSecond; i++)
                Check(Command(sim, PlayerSlot.Zero, CommandType.SetSpeedVote, 1).Accepted, "Cached duplicates consumed command budget");
            var limited = SpeedVote(sim, PlayerSlot.Zero, 0);
            string before = Fingerprint(sim.State);
            var rejection = sim.Execute(PlayerSlot.Zero, limited);
            Equal(rejection.Code, ErrorCode.RateLimited, "Votes bypassed the regular command limit");
            Equal(Fingerprint(sim.State), before, "Rate-limited withdrawal mutated consent");
            Steps(sim, sim.Content.TickRate);
            before = Fingerprint(sim.State);
            var replay = sim.Execute(PlayerSlot.Zero, limited);
            Equal(replay.Code, ErrorCode.RateLimited, "A rejected vote became accepted after time passed");
            Equal(replay.Tick, rejection.Tick, "Rejected replay changed its receipt tick");
            Equal(Fingerprint(sim.State), before, "Rate-limit replay changed consent");
            Check(Command(sim, PlayerSlot.Zero, CommandType.SetSpeedVote, 0).Accepted, "Fresh withdrawal never recovered from rate limiting");
            Equal(sim.State.SimulationSpeed, 1, "Fresh withdrawal did not restore normal speed");
            for (int i = 0; i < 140; i++)
            {
                if (i % 20 == 0) Steps(sim, sim.Content.TickRate);
                Check(Command(sim, PlayerSlot.Zero, CommandType.SetSpeedVote, 0).Accepted, "Explicit normal-speed vote unexpectedly failed");
            }
            before = Fingerprint(sim.State);
            Equal(sim.Execute(PlayerSlot.Zero, approval).Code, ErrorCode.StaleSequence, "Evicted approval regained authority");
            Equal(Fingerprint(sim.State), before, "Expired approval changed speed");
        }

        public static void SpeedVotesResetAcrossResultsAndEpochs()
        {
            var sim = Started();
            var oldVote = SpeedVote(sim, PlayerSlot.Zero);
            sim.Execute(PlayerSlot.Zero, oldVote); Command(sim, PlayerSlot.One, CommandType.SetSpeedVote, 1);
            Equal(sim.State.SimulationSpeed, 2, "Joint speed fixture failed");
            FinishAtLastWave(sim);
            Equal(sim.State.Phase, MatchPhase.Finished, "Speed changed the game-time match limit");
            Equal(sim.State.SimulationSpeed, 1, "Result retained accelerated scheduling");
            string before = Fingerprint(sim.State);
            Equal(Command(sim, PlayerSlot.Zero, CommandType.SetSpeedVote, 0).Code, ErrorCode.WrongPhase, "Result allowed speed changes");
            Equal(Fingerprint(sim.State), before, "Result vote mutated final state");
            Command(sim, PlayerSlot.Zero, CommandType.RequestRematch);
            Command(sim, PlayerSlot.One, CommandType.RequestRematch);
            Check(sim.State.SessionId != oldVote.SessionId, "Rematch did not replace epoch");
            Check(!sim.State.Players[0].SpeedReady && !sim.State.Players[1].SpeedReady, "New epoch inherited speed consent");
            Equal(sim.State.SimulationSpeed, 1, "New lobby accelerated");
            before = Fingerprint(sim.State);
            Equal(sim.Execute(PlayerSlot.Zero, oldVote).Code, ErrorCode.WrongSession, "Old vote entered the new epoch");
            Equal(Fingerprint(sim.State), before, "Old epoch changed fresh speed state");
            Command(sim, PlayerSlot.Zero, CommandType.SetReady); Command(sim, PlayerSlot.One, CommandType.SetReady);
            Equal(sim.State.SimulationSpeed, 1, "Countdown accelerated");
            Equal(Command(sim, PlayerSlot.One, CommandType.SetSpeedVote, 1).Code, ErrorCode.WrongPhase, "Countdown accepted early approval");
            while (sim.State.Phase == MatchPhase.Countdown) sim.Step();
            ActivateArithmeticFixture(sim);
            Equal(sim.State.SimulationSpeed, 1, "New match inherited acceleration");
            Command(sim, PlayerSlot.Zero, CommandType.SetSpeedVote, 1); Command(sim, PlayerSlot.One, CommandType.SetSpeedVote, 1);
            sim.Abort();
            Equal(sim.State.SimulationSpeed, 1, "Aborted session retained accelerated scheduling");
            Equal(Command(sim, PlayerSlot.Zero, CommandType.SetSpeedVote, 0).Code, ErrorCode.WrongPhase, "Aborted session accepted a vote");
        }

        public static void SpeedAgreementLeavesGameTimeRulesUnchanged()
        {
            MatchSimulation normal = Started(RichContent()), fast = Started(RichContent());
            foreach (var sim in new[] { normal, fast })
                for (int p = 0; p < 2; p++)
                {
                    Check(Command(sim, (PlayerSlot)p, CommandType.BuildTower, 4, 11).Accepted, "Mine fixture failed");
                    Check(Command(sim, (PlayerSlot)p, CommandType.BuildTower, 2, 0).Accepted, "Frost fixture failed");
                    Check(Command(sim, (PlayerSlot)p, CommandType.BuildTower, 1, 1).Accepted, "Cannon fixture failed");
                    foreach (int kind in new[] { 2, 3, 0, 2, 3 })
                        Check(Command(sim, (PlayerSlot)p, CommandType.SendUnit, kind).Accepted, "Paid attack fixture failed");
                }
            Command(fast, PlayerSlot.Zero, CommandType.SetSpeedVote, 1); Command(fast, PlayerSlot.One, CommandType.SetSpeedVote, 1);
            bool sawSlow = false, sawDamage = false, sawQueuedSpawn = false;
            for (int tick = 0; tick < 600; tick++)
            {
                if (tick == 80 || tick == 160)
                    foreach (var sim in new[] { normal, fast })
                    {
                        TowerState mine = sim.State.Towers[0];
                        Check(Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, (int)TowerUpgradePath.Income, mine.Id).Accepted, "Income upgrade fixture failed");
                    }
                if (tick == 300) Command(fast, PlayerSlot.One, CommandType.SetSpeedVote, 0);
                if (tick == 400) Command(fast, PlayerSlot.One, CommandType.SetSpeedVote, 1);
                int priorTick = fast.State.Tick, priorPlay = fast.State.PlayTicks;
                normal.Step(); fast.Step();
                Equal(fast.State.Tick, priorTick + 1, "2x made one Step advance multiple ticks");
                Equal(fast.State.PlayTicks, priorPlay + 1, "2x independently scaled the match clock");
                Equal(Fingerprint(fast.State, false), Fingerprint(normal.State, false), "Speed vote changed economy, combat, movement or FIFO at tick " + tick);
                sawQueuedSpawn |= fast.State.SpawnOrders.Count < 10;
                foreach (var unit in fast.State.Units)
                {
                    sawSlow |= unit.SlowPermille < 1000;
                    sawDamage |= unit.Hp < fast.Content.Units[unit.Kind].Hp;
                }
            }
            Check(sawQueuedSpawn && sawSlow && sawDamage, "Equivalence fixture did not exercise queued spawning, frost and damage");
            Equal(fast.State.SimulationSpeed, 2, "Final joint agreement was lost");
            Equal(normal.State.SimulationSpeed, 1, "Reference match accelerated without consent");
        }

        public static void DuplicateAndStaleCommandsCannotPurchaseTwice()
        {
            MatchSimulation sim = Started(RichContent());
            var c = new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = 2, Type = CommandType.BuildTower, Kind = 0, Target = 0 };
            Check(sim.Execute(PlayerSlot.Zero, c).Accepted, "First purchase rejected");
            string before = Fingerprint(sim.State);
            Check(sim.Execute(PlayerSlot.Zero, c).Accepted, "Duplicate should replay accepted receipt");
            Equal(Fingerprint(sim.State), before, "Duplicate purchase changed match");
            c.Target = 1; Equal(sim.Execute(PlayerSlot.Zero, c).Code, ErrorCode.Duplicate, "Reused sequence with another payload accepted");
            Equal(Fingerprint(sim.State), before, "Reused sequence changed match");
            for (int i = 0; i < 140; i++) Command(sim, PlayerSlot.Zero, CommandType.BuildTower, -1, -1);
            c.Target = 0; Equal(sim.Execute(PlayerSlot.Zero, c).Code, ErrorCode.StaleSequence, "Evicted duplicate was accepted");
            Equal(sim.State.Towers.Count, 1, "Old command created extra towers");
        }

        public static void ForeignTowerAndInvalidPlacementLeaveStateUntouched()
        {
            MatchSimulation sim = Started(RichContent());
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0);
            int id = sim.State.Towers[0].Id;
            string before = Fingerprint(sim.State);
            Equal(Command(sim, PlayerSlot.One, CommandType.UpgradeTower, 0, id).Code, ErrorCode.NotOwner, "Foreign upgrade permitted");
            Equal(Command(sim, PlayerSlot.One, CommandType.SellTower, 0, id).Code, ErrorCode.NotOwner, "Foreign sale permitted");
            Equal(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0).Code, ErrorCode.Occupied, "Occupied slot accepted");
            Equal(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 8, 1).Code, ErrorCode.InvalidKind, "Unknown kind accepted");
            Equal(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 12).Code, ErrorCode.InvalidTarget, "Thirteenth slot accepted");
            Equal(Fingerprint(sim.State), before, "Denied commands mutated canonical state");
        }

        public static void UpgradesAndRefundNeverCreateGold()
        {
            MatchSimulation sim = Started(RichContent());
            var initialGold= sim.State.Players[0].Gold;
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0);
            TowerState tower = sim.State.Towers[0];
            Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, 0, tower.Id);
            Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, 0, tower.Id);
            var gold= sim.State.Players[0].Gold;
            Check(Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, 0, tower.Id).Accepted, "Third upgrade rejected");
            Equal(gold - sim.State.Players[0].Gold, 20, "Third upgrade debit incorrect");
            var invested = tower.Invested;
            var sell = new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = sim.LastSequence(PlayerSlot.Zero) + 1,
                Type = CommandType.SellTower, Target = tower.Id };
            Check(sim.Execute(PlayerSlot.Zero, sell).Accepted, "Valid sale rejected");
            Equal(sim.State.Players[0].Gold, initialGold - invested + invested * 65 / 100, "Incorrect refund");
            gold = sim.State.Players[0].Gold;
            sim.Execute(PlayerSlot.Zero, sell);
            Equal(sim.State.Players[0].Gold, gold, "Duplicate sale returned gold twice");
            Check(gold < initialGold, "Buy/sell created gold");
        }

        public static void QueuePoolHintsDoNotCapPaidSpawns()
        {
            MatchSimulation sim = Started(RichContent(1, 2));
            AddUnit(sim, 500, PlayerSlot.One, 2, 0, 100000);
            foreach (int kind in new[] {3,0,1}) Check(Command(sim, PlayerSlot.Zero, CommandType.SendUnit, kind).Accepted, "Legacy queue hint limited purchase");
            var gold = sim.State.Players[0].Gold;
            sim.Step();
            Equal(sim.State.Units.Count, 2, "Legacy field hint blocked paid spawn");
            Equal(sim.State.SpawnOrders[0].Remaining, 3, "Paid body was not consumed exactly once");
            Equal(sim.State.Players[0].Gold, gold, "Spawn charged twice");
            sim.State.Players[0].Gold = 0; string before = Fingerprint(sim.State);
            Equal(Command(sim,PlayerSlot.Zero,CommandType.SendUnit,1).Code,ErrorCode.InsufficientGold,"Unfunded order accepted");
            Equal(Fingerprint(sim.State),before,"Rejected order mutated state");
        }

        public static void SwarmCountAndCooldownAreExact()
        {
            MatchSimulation sim = Started();
            int start = sim.State.PlayTicks;
            Check(Command(sim, PlayerSlot.Zero, CommandType.SendUnit, 3).Accepted, "Swarm purchase rejected");
            Check(Command(sim, PlayerSlot.Zero, CommandType.SendUnit, 3).Accepted, "Second prepaid swarm purchase rejected");
            Equal(sim.State.Players[0].NextSendTicks[3], 0, "Buying started the class cooldown");
            sim.Step();
            Equal(sim.State.Units.Count, 1, "First swarm unit did not spawn");
            Equal(sim.State.Players[0].NextSendTicks[3], start + 1 + sim.Content.Units[3].CooldownTicks, "Swarm cooldown did not begin at its first spawn");
            Steps(sim, 3 * sim.Content.Rules.SpawnIntervalTicks);
            Equal(sim.State.Units.Count, 4, "Swarm group size incorrect");
            Equal(sim.State.SpawnOrders.Count, 1, "Second prepaid group disappeared");
            Steps(sim, sim.Content.Units[3].CooldownTicks - 3 * sim.Content.Rules.SpawnIntervalTicks - 1);
            Equal(sim.State.Units.Count, 4, "Second swarm started before its class cooldown");
            sim.Step();
            Equal(sim.State.Units.Count, 5, "Second swarm did not start when cooldown expired");
            Steps(sim, 3 * sim.Content.Rules.SpawnIntervalTicks);
            Equal(sim.State.Units.Count, 8, "Second swarm count or intra-group spacing incorrect");
            Equal(sim.State.SpawnOrders.Count, 0, "Completed order retained");
            foreach (UnitState unit in sim.State.Units) Equal(unit.Defender, PlayerSlot.One, "Troop sent to own board");
        }

        public static void RapidTroopPurchasesSpendGoldExactlyOnce()
        {
            MatchSimulation sim = Started();
            var first = new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = sim.LastSequence(PlayerSlot.Zero) + 1,
                Type = CommandType.SendUnit, Kind = 0, Target = -1, ExpectedWave=1, EnemyTarget=1, ExpectedResearch=0 };
            Check(sim.Execute(PlayerSlot.Zero, first).Accepted, "First rapid purchase failed");
            for (int i = 1; i < 10; i++) Check(Command(sim, PlayerSlot.Zero, CommandType.SendUnit, 0).Accepted,
                "Affordable rapid same-class purchase was rejected");
            Equal(sim.State.Players[0].Gold, 0, "Ten runner groups did not spend exactly the starting 180 gold");
            Equal(sim.PendingOrders(PlayerSlot.Zero), 10, "Rapid taps did not create ten paid groups");
            Equal(sim.State.Players[0].NextSendTicks[0], 0, "Purchase manufactured an early start timer");
            string before = Fingerprint(sim.State);
            Equal(Command(sim, PlayerSlot.Zero, CommandType.SendUnit, 0).Code, ErrorCode.InsufficientGold, "Unaffordable rapid order accepted");
            Equal(Fingerprint(sim.State), before, "Rejected rapid order changed canonical state");
            Check(sim.Execute(PlayerSlot.Zero, first).Accepted, "Accepted receipt was not replayed");
            Equal(Fingerprint(sim.State), before, "Duplicate prepaid order was charged or appended again");
            sim.Step();
            Equal(sim.State.Units.Count, 1, "First paid order did not depart");
            Equal(sim.PendingOrders(PlayerSlot.Zero), 9, "Departed runner order did not leave the queue");
            before = Fingerprint(sim.State);
            sim.Execute(PlayerSlot.Zero, first);
            Equal(Fingerprint(sim.State), before, "Duplicate after dispatch manufactured another troop");
        }

        public static void PaidGroupsRespectClassCooldownAndMixedFifo()
        {
            MatchSimulation sim = Started(RichContent());
            foreach (int kind in new[] { 2, 2, 0, 3 })
                Check(Command(sim, PlayerSlot.Zero, CommandType.SendUnit, kind).Accepted, "Mixed prepaid group rejected");
            int start = sim.State.PlayTicks;
            sim.Step(); Equal(sim.State.Units.Count, 1, "First tank did not spawn");
            Equal(sim.State.Units[0].Kind, 2, "FIFO head was bypassed");
            Equal(sim.State.Players[0].NextSendTicks[2], start + 1 + sim.Content.Units[2].CooldownTicks, "Tank group timer differs from class definition");
            Steps(sim, sim.Content.Units[2].CooldownTicks - 1); Equal(sim.State.Units.Count, 1, "Later class overtook a cooling-down FIFO head");
            sim.Step(); Equal(sim.State.Units.Count, 2, "Second tank missed its class departure");
            Equal(sim.State.Units[1].Kind, 2, "Second paid tank was reordered");
            Steps(sim, sim.Content.Rules.SpawnIntervalTicks - 1); Equal(sim.State.Units.Count, 2, "Global spawn spacing was skipped");
            sim.Step(); Equal(sim.State.Units[2].Kind, 0, "Runner did not follow the tanks");
            Steps(sim, sim.Content.Rules.SpawnIntervalTicks); Equal(sim.State.Units[3].Kind, 3, "Swarm did not follow the runner");
            Steps(sim, 3 * sim.Content.Rules.SpawnIntervalTicks); Equal(sim.State.Units.Count, 7, "Mixed FIFO lost part of the paid swarm");
            Equal(sim.PendingOrders(PlayerSlot.Zero), 0, "Mixed FIFO retained completed groups");
        }

        public static void FieldPoolHintsDoNotBlockGroupStart()
        {
            MatchSimulation sim = Started(RichContent(1, 2));
            AddUnit(sim, 500, PlayerSlot.One, 2, 0, 100000);
            Check(Command(sim, PlayerSlot.Zero, CommandType.SendUnit, 2).Accepted, "Tank purchase failed");
            var gold=sim.State.Players[0].Gold; sim.Step();
            Equal(sim.State.Players[0].NextSendTicks[2],sim.State.PlayTicks+sim.Content.Units[2].CooldownTicks,"Normal cooldown did not start above legacy field size");
            Equal(sim.State.Units.Count,2,"Legacy field hint still caps live troops");
            Equal(sim.State.SpawnOrders.Count,0,"Completed packet retained");
            Equal(sim.State.Players[0].Gold,gold,"Departure charged twice");
            PaidGroupsRespectClassCooldownAndMixedFifo();
        }

        public static void OrdersBeyondEightyArePaidAndRematchClearsThem()
        {
            MatchSimulation sim = Started(RuleContent(new GameRules(startGold: 10000, maxCommandsPerSecond: 100)));
            Equal(sim.Content.MaxSpawnOrdersPerPlayer, 80, "Default paid queue is not 80 orders");
            for (int i = 0; i < 80; i++) Check(Command(sim, PlayerSlot.Zero, CommandType.SendUnit, 0).Accepted,
                "Valid prepaid queue slot rejected");
            Equal(sim.State.Players[0].Gold, 10000 - 80 * 18, "Full paid queue cost is not exact");
            string before = Fingerprint(sim.State);
            Check(Command(sim, PlayerSlot.Zero, CommandType.SendUnit, 0).Accepted, "81st order was limited by legacy pool size");
            Equal(sim.State.Players[0].Gold, 10000-81*18, "81st packet not debited exactly");
            sim.State.Players[0].Hp = 0; sim.Step();
            Command(sim, PlayerSlot.Zero, CommandType.RequestRematch);
            Command(sim, PlayerSlot.One, CommandType.RequestRematch);
            Equal(sim.State.SpawnOrders.Count, 0, "Rematch retained prepaid orders");
            Equal(sim.State.Units.Count, 0, "Rematch retained already dispatched units");
            for (int p = 0; p < 2; p++)
            {
                Equal(sim.State.Players[p].Gold, 0, "Rematch paid gold before renewed readiness");
                foreach (int timer in sim.State.Players[p].NextSendTicks) Equal(timer, 0, "Rematch retained departure cooldown");
            }
            CheckStartWallets(sim, false);
            Check(Command(sim, PlayerSlot.Zero, CommandType.SetReady).Accepted, "Rematch ready zero");
            CheckStartWallets(sim, false);
            Check(Command(sim, PlayerSlot.One, CommandType.SetReady).Accepted, "Rematch ready one");
            CheckStartWallets(sim, true);
        }

        public static void MapDimensionsAndThirtyTwoSlotsAreValidated()
        {
            GameContent standard = GameContent.CreateDefault(new GameRules(startGold: 10000));
            var slots = new MapPoint[32];
            for (int i = 0; i < slots.Length; i++) slots[i] = new MapPoint(100 + (i % 8) * 200, 160 + (i / 8) * 200);
            var route = new[] { new MapPoint(30, 40), new MapPoint(1570, 40) };
            var map = new MapDefinition(route, slots, "capacity", "Kapazität", 0, 1600, 960);
            var content = new GameContent(standard.Rules, standard.Towers, standard.Units, map);
            MatchSimulation sim = Started(content);
            for (int p = 0; p < 2; p++)
                for (int slot = 0; slot < 32; slot++)
                    Check(Command(sim, (PlayerSlot)p, CommandType.BuildTower, 0, slot).Accepted, "A legal slot among 32 could not be built");
            Equal(sim.State.Towers.Count, 64, "Both players did not receive 32 independent slots");
            for (int kind = 0; kind < 4; kind++) Equal(standard.Towers[kind].MaxPerPlayer, 32, "Combat class cap is not 32");
            Equal(standard.Towers[4].MaxPerPlayer, 2, "Expanded map weakened mine class limit");
            int failures = 0;
            try { new MapDefinition(route, slots, width: 0); } catch (ArgumentException) { failures++; }
            try { new GameContent(standard.Rules, standard.Towers, standard.Units, new MapDefinition(route, slots)); }
            catch (ArgumentException) { failures++; }
            try { new GameContent(standard.Rules, standard.Towers, standard.Units,
                new MapDefinition(route, new MapPoint[33], width: 1600, height: 960)); }
            catch (ArgumentException) { failures++; }
            var outside = (MapPoint[])slots.Clone(); outside[0] = new MapPoint(1601, 500);
            try { new GameContent(standard.Rules, standard.Towers, standard.Units,
                new MapDefinition(route, outside, width: 1600, height: 960)); }
            catch (ArgumentException) { failures++; }
            Equal(failures, 4, "Map dimensions or capacity accepted invalid data");
            new GameContent(standard.Rules, standard.Towers, standard.Units,
                new MapDefinition(route, new[] { slots[0] }, width: 1600, height: 960)).Validate();
        }

        public static void RapidSendRateLimitReceiptRemainsRejected()
        {
            MatchSimulation sim = Started(RuleContent(new GameRules(startGold: 10000)));
            for (int i = 0; i < 40; i++) Check(Command(sim, PlayerSlot.Zero, CommandType.SendUnit, 0).Accepted,
                "A permitted rapid order was rate limited early");
            var rejected = new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = sim.LastSequence(PlayerSlot.Zero) + 1,
                Type = CommandType.SendUnit, Kind = 0, Target = -1, ExpectedWave=1, EnemyTarget=1, ExpectedResearch=0 };
            string before = Fingerprint(sim.State);
            CommandResult receipt = sim.Execute(PlayerSlot.Zero, rejected);
            Equal(receipt.Code, ErrorCode.RateLimited, "41st same-window purchase bypassed rate limit");
            Equal(Fingerprint(sim.State), before, "Rate rejection charged gold");
            Steps(sim, 20); before = Fingerprint(sim.State);
            CommandResult repeated = sim.Execute(PlayerSlot.Zero, rejected);
            Equal(repeated.Code, ErrorCode.RateLimited, "Cached rejected purchase became valid later");
            Equal(repeated.Tick, receipt.Tick, "Rejected purchase did not preserve its original receipt");
            Equal(Fingerprint(sim.State), before, "Repeated rejection changed the paid queue");
            Check(Command(sim, PlayerSlot.Zero, CommandType.SendUnit, 0).Accepted, "Fresh purchase did not recover after rate window");
        }

        public static void KillBountyPaysDefenderOnceIncludingSplash()
        {
            MatchSimulation sim = Started(RichContent());
            Check(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 1, 0).Accepted, "Cannon setup failed");
            for (int kind = 0; kind < 4; kind++) AddUnit(sim, 500 + kind, PlayerSlot.Zero, kind, 100 + kind * 4, 1);
            AddUnit(sim, 600, PlayerSlot.One, 0, 100, 1);
            BigInteger gold= sim.State.Players[0].Gold, other = sim.State.Players[1].Gold;
            int reward = 0; for(int kind=0;kind<4;kind++) reward += sim.Content.Units[kind].KillBounty; // Exactly the four paid fixture bodies above, not unspawned elite classes.
            sim.Step();
            Equal(sim.State.Players[0].Gold, gold + reward, "Splash kills did not pay each defender bounty exactly once");
            Equal(sim.State.Players[1].Gold, other, "Attacker received the defender's kill rewards");
            Equal(sim.State.Units.Count, 1, "Splash did not remove all four dead units");
            Steps(sim, 10);
            Equal(sim.State.Players[0].Gold, gold + reward, "Removed units paid bounty repeatedly");
        }

        public static void SimultaneousKillsAndLeaksKeepPayoutsDistinct()
        {
            MatchSimulation sim = Started(RichContent());
            for (int p = 0; p < 2; p++)
            {
                Command(sim, (PlayerSlot)p, CommandType.BuildTower, 0, 0);
                AddUnit(sim, 500 + p, (PlayerSlot)p, p, 100, 1);
                AddUnit(sim, 600 + p, (PlayerSlot)p, 0, sim.Content.Map.Length);
                sim.State.Players[p].Hp = 1;
            }
            BigInteger a= sim.State.Players[0].Gold, b = sim.State.Players[1].Gold;
            sim.Step();
            Equal(sim.State.Outcome, MatchOutcome.Draw, "Simultaneous lethal leaks did not draw");
            Equal(sim.State.Players[0].Gold, a + sim.Content.Units[0].KillBounty, "Leak incorrectly paid bounty or defender kill was lost");
            Equal(sim.State.Players[1].Gold, b + sim.Content.Units[1].KillBounty, "Same-tick opponent kill was not credited");
            string finished = Fingerprint(sim.State); Steps(sim, 25);
            Equal(Fingerprint(sim.State), finished, "Finished outcome paid additional gold");
        }

        public static void KillBountyIsUncappedAndSalesDoNotPayExtra()
        {
            MatchSimulation sim = Started(RichContent());
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0);
            TowerState tower = sim.State.Towers[0];
            sim.State.Players[0].Gold = sim.Content.Rules.MaxGold - 2;
            AddUnit(sim, 500, PlayerSlot.Zero, 0, 100, 1);
            sim.Step(); Equal(sim.State.Players[0].Gold, sim.Content.Rules.MaxGold+2, "Bounty was truncated by legacy base-content bound");
            sim.State.Players[0].Gold = 0;
            var refund = sim.RefundFor(tower);
            var sale = new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = sim.LastSequence(PlayerSlot.Zero) + 1,
                Type = CommandType.SellTower, Target = tower.Id };
            Check(sim.Execute(PlayerSlot.Zero, sale).Accepted, "Sale failed");
            Equal(sim.State.Players[0].Gold, refund, "Selling paid something beyond the explicit refund");
            sim.Execute(PlayerSlot.Zero, sale);
            Equal(sim.State.Players[0].Gold, refund, "Duplicate sale manufactured reward");
        }

        public static void BountyDefinitionsAreValidatedAndHashed()
        {
            GameContent content = RuleContent(); UnitDefinition original = content.Units[0];
            UnitDefinition[] units = content.Units;
            units[0] = new UnitDefinition(original.Name, original.Price, original.Hp, original.SpeedPerSecond,
                original.Armor, original.BaseDamage, original.Count, original.CooldownTicks, original.KillBounty + 1);
            Check(new GameContent(content.Rules, content.Towers, units, content.Map).ContentHash != content.ContentHash,
                "Kill reward is absent from content hash");
            int failures = 0;
            foreach (int bounty in new[] { -1, content.Rules.MaxGold + 1 })
            {
                units[0] = new UnitDefinition(original.Name, original.Price, original.Hp, original.SpeedPerSecond,
                    original.Armor, original.BaseDamage, original.Count, original.CooldownTicks, bounty);
                try { new GameContent(content.Rules, content.Towers, units, content.Map); }
                catch (ArgumentException) { failures++; }
            }
            Equal(failures, 2, "Invalid kill reward accepted");
            Equal(new UnitDefinition("Legacy", 10, 10, 10, 0, 1, 1, 20).KillBounty, 0, "Legacy constructor creates unexpected rewards");
        }

        public static void DedicatedMineSlotsAreImmutableAndEnforced()
        {
            GameContent original = RichContent(); MapDefinition old = original.Map;
            int[] indices = { 8, 2 };
            var map = new MapDefinition(old.Waypoints, old.BuildSlots, old.Id, old.Name, old.Theme, old.Width, old.Height, indices);
            var content = new GameContent(original.Rules, original.Towers, original.Units, map);
            indices[0] = 4; map.MineSlots[0] = 5;
            Check(map.HasDedicatedMineSlots && map.IsMineSlot(2) && map.IsMineSlot(8) && !map.IsMineSlot(4), "Mine slot definitions were mutable");
            Check(!map.CanBuildAt(-1, true) && !map.CanBuildAt(12, false), "Invalid slot index allowed");
            MatchSimulation sim = Started(content);
            string before = Fingerprint(sim.State);
            Equal(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 4, 0).Code, ErrorCode.InvalidTarget, "Mine built on combat-only slot");
            Equal(Fingerprint(sim.State), before, "Wrong mine slot spent gold");
            Equal(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 2).Code, ErrorCode.InvalidTarget, "Combat tower built on mine-only slot");
            Equal(Fingerprint(sim.State), before, "Wrong combat slot spent gold");
            Check(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 4, 2).Accepted, "Valid dedicated mine rejected");
            TowerState mine = sim.State.Towers[0];
            Check(Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, (int)TowerUpgradePath.Income, mine.Id).Accepted, "Dedicated mine first upgrade rejected");
            Check(Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, (int)TowerUpgradePath.Income, mine.Id).Accepted, "Dedicated mine second upgrade rejected");
            Equal(TowerUpgradeRules.Stats(sim.Content,mine).IncomeUnitsPerSecond, 1176, "Dedicated mine upgrades changed production");
            Check(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0).Accepted, "Combat-only slot rejected a tower");
            Check(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 4, 8).Accepted, "Second dedicated mine rejected");
            var same = new MapDefinition(old.Waypoints, old.BuildSlots, old.Id, old.Name, old.Theme, old.Width, old.Height, new[] { 2, 8 });
            Equal(new GameContent(original.Rules, original.Towers, original.Units, same).ContentHash, content.ContentHash,
                "Mine index order changed equivalent map hash");
            var changed = new MapDefinition(old.Waypoints, old.BuildSlots, old.Id, old.Name, old.Theme, old.Width, old.Height, new[] { 3, 8 });
            Check(new GameContent(original.Rules, original.Towers, original.Units, changed).ContentHash != content.ContentHash, "Mine slots absent from hash");
            Check(old.CanBuildAt(0, true) && old.CanBuildAt(0, false) && old.MineSlots.Length == 0, "Legacy mixed slots lost compatibility");
            int failures = 0;
            foreach (int[] invalid in new[] { new[] { 0 }, new[] { 0, 0 }, new[] { 0, 12 }, new[] { -1, 2 }, new[] { 0, 1, 2 } })
            {
                try { new MapDefinition(old.Waypoints, old.BuildSlots, mineSlots: invalid); }
                catch (ArgumentException) { failures++; }
            }
            Equal(failures, 5, "Invalid dedicated mine indices accepted");
        }

        private static GameContent FreeContent()
        {
            GameContent standard = GameContent.CreateDefault(new GameRules(startGold: 10000));
            var map = new MapDefinition(new[] { new MapPoint(100, 300), new MapPoint(900, 300) },
                new[] { new MapPoint(100, 100), new MapPoint(900, 100) }, "free-test", "Freie Fläche", 0,
                1000, 600, Array.Empty<int>(), true, new[] { new MapRectangle(400, 60, 500, 180) });
            return new GameContent(standard.Rules, standard.Towers, standard.Units, map);
        }

        public static void FreePlacementIsContinuousAtomicAndIdempotent()
        {
            MatchSimulation sim = Started(FreeContent());
            var command = new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = sim.LastSequence(PlayerSlot.Zero) + 1,
                Type = CommandType.BuildTower, Kind = 0, Target = -1, PositionX = 253, PositionY = 115 };
            string before = Fingerprint(sim.State);
            Equal(PlacementValidator.ValidateBuild(sim.Content, sim.State, PlayerSlot.Zero, 0, -1,
                new MapPoint(253, 115), out TowerPlacement preview), ErrorCode.None, "Legal continuous preview rejected");
            Equal(Fingerprint(sim.State), before, "Preview mutated authoritative match");
            Check(sim.Execute(PlayerSlot.Zero, command).Accepted, "Arbitrary legal coordinate was treated as a hidden fixed slot");
            TowerState tower = sim.State.Towers[0];
            Equal(tower.Position.X, 253, "Free build snapped its X coordinate");
            Equal(tower.Position.Y, 115, "Free build snapped its Y coordinate");
            Equal(tower.SiteId, -1, "Free tower received a fixed site ID");
            Equal(tower.Slot, preview.Slot, "Preview and command chose different render capacity");
            before = Fingerprint(sim.State); sim.Execute(PlayerSlot.Zero, command);
            Equal(Fingerprint(sim.State), before, "Repeated free-placement request charged or built twice");
            command.PositionX++;
            Equal(sim.Execute(PlayerSlot.Zero, command).Code, ErrorCode.Duplicate, "Reused request ID with changed coordinates was accepted");
            Equal(Fingerprint(sim.State), before, "Changed duplicate coordinates mutated match");
            Equal(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, -1, 253, 115).Code, ErrorCode.TooClose, "Overlapping free tower accepted");
            Equal(Fingerprint(sim.State), before, "Overlap rejection spent gold");
            Check(Command(sim, PlayerSlot.One, CommandType.BuildTower, 0, -1, 253, 115).Accepted, "Opponent building incorrectly blocked independent board");
            Check(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 4, -1, 101, 103).Accepted, "Free mine coordinate rejected");
            TowerState mine = sim.State.Towers[2];
            Equal(mine.SiteId, -1, "Free mine retained a fixed site identity");
            Equal(mine.Slot, 1, "Free mine overwrote occupied render capacity");
            Equal(mine.Position.X, 101, "Free mine snapped its requested X coordinate");
            Equal(mine.Position.Y, 103, "Free mine snapped its requested Y coordinate");
            Equal(PlacementValidator.ValidatePosition(sim.Content, sim.State, PlayerSlot.Zero, tower.Kind, tower.SiteId,
                tower.Position, tower.Id), ErrorCode.None, "Snapshot validation collided a tower with itself");
            before = Fingerprint(sim.State);
            Equal(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 1, 700, 500).Code, ErrorCode.InvalidTarget, "Free combat accepted a legacy site target");
            Equal(Fingerprint(sim.State), before, "Invalid free target spent gold");
        }

        public static void FreePlacementGeometryHasExactBoundaries()
        {
            MatchSimulation sim = Started(FreeContent());
            Func<int, int, ErrorCode> test = (x, y) => PlacementValidator.ValidatePosition(sim.Content, sim.State,
                PlayerSlot.Zero, 0, -1, new MapPoint(x, y));
            Equal(test(29, 100), ErrorCode.OutsideMap, "Footprint outside map accepted");
            Equal(test(30, 100), ErrorCode.None, "Exact legal boundary rejected");
            Equal(test(int.MaxValue, int.MinValue), ErrorCode.OutsideMap, "Extreme command coordinates overflowed validation");
            Equal(test(250, 231), ErrorCode.OnPath, "69-unit road clearance accepted");
            Equal(test(250, 230), ErrorCode.None, "Exact 70-unit road clearance rejected");
            Equal(test(450, 100), ErrorCode.BlockedArea, "Blocked terrain accepted");
            Equal(test(371, 100), ErrorCode.BlockedArea, "Footprint overlap with terrain blocker accepted");
            Equal(test(370, 100), ErrorCode.None, "Footprint tangent to blocker rejected");
            Equal(test(159, 100), ErrorCode.None, "Removed mine reservation still blocks free ground");
            Equal(test(160, 100), ErrorCode.None, "Exact mine-site separation rejected");
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, -1, 250, 100);
            Equal(test(309, 100), ErrorCode.TooClose, "59-unit building separation accepted");
            Equal(test(310, 100), ErrorCode.None, "Exact 60-unit building separation rejected");
            Equal(PlacementValidator.ValidatePosition(sim.Content, sim.State, PlayerSlot.Zero, 4, 0,
                new MapPoint(101, 100)), ErrorCode.InvalidTarget, "Free mine accepted an obsolete fixed site ID");
            sim.State.Phase = MatchPhase.Finished; sim.State.Players[0].Gold = 0;
            Equal(test(700, 500), ErrorCode.None, "Pure geometry depends on phase or gold");
            Equal(PlacementValidator.ValidateBuild(sim.Content, sim.State, PlayerSlot.Zero, 0, -1,
                new MapPoint(700, 500), out TowerPlacement ignored), ErrorCode.WrongPhase, "Build preview ignored phase");
        }

        public static void FreePlacementCapacityIsIndependentOfSiteCount()
        {
            MatchSimulation sim = Started(FreeContent());
            Equal(sim.Content.Map.BuildSlots.Length, 2, "Capacity fixture unexpectedly contains combat anchors");
            for (int p = 0; p < 2; p++)
            {
                for (int i = 0; i < 30; i++)
                    Check(Command(sim, (PlayerSlot)p, CommandType.BuildTower, 0, -1, 60 + (i % 10) * 90,
                        380 + (i / 10) * 80).Accepted, "Free map required a hidden combat anchor or exhausted capacity early");
                Check(Command(sim, (PlayerSlot)p, CommandType.BuildTower, 4, -1, 100, 100).Accepted, "First free mine capacity unavailable");
                Check(Command(sim, (PlayerSlot)p, CommandType.BuildTower, 4, -1, 900, 100).Accepted, "Second free mine capacity unavailable");
                string before = Fingerprint(sim.State);
                Check(Command(sim, (PlayerSlot)p, CommandType.BuildTower, 0, -1, 960, 540).Accepted, "31st free combat tower was capped");
            }
            Equal(sim.State.Towers.Count, 66, "Free board did not grow above old render pool size");
        }

        public static void FreeTowerCombatUsesActualPosition()
        {
            MatchSimulation sim = Started(FreeContent());
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, -1, 253, 230);
            UnitState target = AddUnit(sim, 500, PlayerSlot.Zero, 0, 153);
            sim.Step();
            Equal(target.Hp, sim.Content.Units[0].Hp - sim.Content.Towers[0].Damage,
                "Combat still used a fixed anchor instead of the tower's actual position");
        }

        public static void FreePlacementDefinitionDataIsImmutableAndHashed()
        {
            GameContent content = FreeContent(); MapDefinition map = content.Map;
            var areas = new[] { new MapRectangle(400, 60, 500, 180) };
            var copy = new MapDefinition(map.Waypoints, map.BuildSlots, map.Id, map.Name, map.Theme, map.Width, map.Height,
                map.MineSlots, true, areas);
            areas[0] = new MapRectangle(300, 60, 500, 180); copy.BlockedAreas[0] = areas[0];
            Equal(copy.BlockedAreas[0].MinX, 400, "Blocked terrain escaped defensive copying");
            var fixedMap = new MapDefinition(map.Waypoints, map.BuildSlots, map.Id, map.Name, map.Theme, map.Width, map.Height,
                map.MineSlots, false, map.BlockedAreas);
            var changed = new MapDefinition(map.Waypoints, map.BuildSlots, map.Id, map.Name, map.Theme, map.Width, map.Height,
                map.MineSlots, true, areas);
            Check(new GameContent(content.Rules, content.Towers, content.Units, fixedMap).ContentHash != content.ContentHash,
                "Free-placement flag absent from content hash");
            Check(new GameContent(content.Rules, content.Towers, content.Units, changed).ContentHash != content.ContentHash,
                "Blocked terrain absent from content hash");
            int failures = 0;
            try { new MapDefinition(map.Waypoints, map.BuildSlots, mineSlots: new[] { 0, 1 }, freePlacement: true); }
            catch (ArgumentException) { failures++; }
            try { new MapDefinition(map.Waypoints, map.BuildSlots, blockedAreas: new[] { new MapRectangle(-1, 0, 20, 20) }); }
            catch (ArgumentException) { failures++; }
            try { new MapDefinition(map.Waypoints, map.BuildSlots, blockedAreas: new[] { default(MapRectangle) }); }
            catch (ArgumentException) { failures++; }
            Equal(failures, 3, "Invalid free-placement definition accepted");
        }

        public static void UpgradePathsHaveSixCanonicalVariants()
        {
            GameContent content = GameContent.CreateDefault();
            int[][] damages = { new[] { 6, 7, 8 }, new[] { 16, 19, 23 }, new[] { 4, 4, 4 }, new[] { 40, 48, 58 } };
            int[][] ranges = { new[] { 150, 162, 174 }, new[] { 155, 155, 155 }, new[] { 155, 167, 179 }, new[] { 190, 205, 220 } };
            int[] intervals = { 8, 32, 20, 42 };
            int[,] states = { { 0, 0 }, { 1, 0 }, { 0, 1 }, { 2, 0 }, { 1, 1 }, { 0, 2 } };
            for (int kind = 0; kind < 4; kind++)
                for (int variant = 0; variant < 6; variant++)
                {
                    int damage = states[variant, 0], range = states[variant, 1];
                    var tower = new TowerState { Kind = kind, Level = damage + range, DamageLevel = damage, RangeLevel = range };
                    Equal(TowerUpgradeRules.ValidateState(content, tower), ErrorCode.None, "Valid upgrade combination rejected");
                    TowerStats stats = TowerUpgradeRules.Stats(content, tower);
                    Equal(stats.Damage, damages[kind][damage], "Damage path does not match approved independent values");
                    Equal(stats.Range, ranges[kind][range], "Range path does not match approved independent values");
                    Equal(stats.FireIntervalTicks, intervals[kind], "Upgrade secretly changed fire interval");
                    Equal(stats.IncomePerSecond, 0, "Combat path created income");
                    Equal(TowerUpgradeRules.VisualVariant(tower), variant, "Two distinct path combinations share or swap model variants");
                }
            for (int level = 0; level <= 2; level++)
            {
                var mine = new TowerState { Kind = 4, Level = level };
                TowerStats stats = TowerUpgradeRules.Stats(content, mine);
                Equal(stats.IncomeUnitsPerSecond, 960+96*level+12*level*(level-1), "Mine income path changed");
                Equal(stats.Damage + stats.Range + stats.FireIntervalTicks, 0, "Mine acquired weapon stats");
                Equal(TowerUpgradeRules.VisualVariant(mine), level, "Mine model variant changed");
            }
        }

        public static void UpgradeOrdersAreAtomicAndPathSpecific()
        {
            int[,] sequences = { { 0, 0 }, { 0, 1 }, { 1, 0 }, { 1, 1 } };
            for (int kind = 0; kind < 4; kind++)
                for (int order = 0; order < 4; order++)
                {
                    MatchSimulation sim = Started(RichContent());
                    Check(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, kind, 0).Accepted, "Upgrade fixture build failed");
                    TowerState tower = sim.State.Towers[0]; TowerDefinition definition = sim.Content.Towers[kind];
                    MapPoint position = tower.Position; int id = tower.Id, site = tower.SiteId, slot = tower.Slot;
                    string hash = sim.Content.ContentHash; var initialGold= sim.State.Players[0].Gold;
                    for (int stage = 0; stage < 2; stage++)
                    {
                        TowerUpgradePath path = sequences[order, stage] == 0 ? TowerUpgradeRules.PrimaryPath(sim.Content,kind) : TowerUpgradeRules.SecondaryPath(sim.Content,kind);
                        tower.CooldownTicks = 5;
                        TowerStats prior = TowerUpgradeRules.Stats(sim.Content, tower);
                        string before = Fingerprint(sim.State);
                        UpgradeQuote quote = TowerUpgradeRules.Quote(sim.Content, tower, path);
                        Check(quote.IsValid, "Legal upgrade sequence has no quote");
                        Equal(Fingerprint(sim.State), before, "Quote mutated state");
                        Equal(quote.Price, TowerUpgradeRules.Price(sim.Content,kind,stage+1), "Price depends on path instead of total purchases");
                        var gold= sim.State.Players[0].Gold;
                        Check(Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, (int)path, id).Accepted, "Legal path purchase rejected");
                        TowerStats actual = TowerUpgradeRules.Stats(sim.Content, tower);
                        Equal(actual.Damage, quote.Next.Damage, "Quote and actual damage disagree");
                        Equal(actual.Range, quote.Next.Range, "Quote and actual range disagree");
                        Equal(tower.Level, quote.NextLevel, "Quote and actual total level disagree");
                        Equal(tower.DamageLevel, quote.NextDamageLevel, "Quote and actual damage level disagree");
                        Equal(tower.RangeLevel, quote.NextRangeLevel, "Quote and actual range level disagree");
                        Equal(gold - sim.State.Players[0].Gold, quote.Price, "Upgrade did not charge exactly once");
                        Equal(tower.CooldownTicks, 5, "Upgrade reset a running weapon cooldown");
                        Equal(actual.FireIntervalTicks, definition.FireIntervalTicks, "Upgrade changed base interval");
                        if (path == TowerUpgradeRules.PrimaryPath(sim.Content,kind)) Equal(actual.Range, prior.Range, "Primary purchase also granted range");
                        else Equal(actual.Damage, prior.Damage, "Range purchase also granted damage");
                        Equal(tower.Position.X, position.X, "Upgrade moved X"); Equal(tower.Position.Y, position.Y, "Upgrade moved Y");
                        Equal(tower.Id, id, "Upgrade changed entity identity"); Equal(tower.SiteId, site, "Upgrade changed site identity");
                        Equal(tower.Slot, slot, "Upgrade changed render capacity"); Equal(tower.Owner, PlayerSlot.Zero, "Upgrade changed owner");
                    }
                    var totalPrice= TowerUpgradeRules.Price(sim.Content,kind,1) + TowerUpgradeRules.Price(sim.Content,kind,2);
                    Equal(initialGold - sim.State.Players[0].Gold, totalPrice, "Path order changed total cost");
                    Equal(tower.Invested, definition.Price + totalPrice, "Path investment omitted a purchase");
                    Equal(sim.RefundFor(tower), (definition.Price + totalPrice) * 65 / 100, "Path refund differs from paid investment");
                    Equal(sim.Content.ContentHash, hash, "Upgrade mutated content definitions");
                    if(order==1||order==2) { Equal(tower.DamageLevel,1,"Mixed primary missing"); Equal(tower.RangeLevel,1,"Mixed secondary missing"); }
                    var third=TowerUpgradeRules.Quote(sim.Content,tower,TowerUpgradeRules.PrimaryPath(sim.Content,kind));
                    Check(Command(sim,PlayerSlot.Zero,CommandType.UpgradeTower,(int)third.Path,id).Accepted,"Third class purchase capped");
                    Equal(tower.Level,3,"Third purchase level");
                    Equal(tower.Invested,definition.Price+totalPrice+third.Price,"Third purchase investment");
                }
        }

        public static void UpgradeQuotesIgnoreGoldAndRejectInvalidState()
        {
            MatchSimulation sim = Started(RichContent());
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0);
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 4, 1);
            TowerState tower = sim.State.Towers[0], mine = sim.State.Towers[1];
            sim.State.Players[0].Gold = 0;
            string before = Fingerprint(sim.State);
            UpgradeQuote quote = TowerUpgradeRules.Quote(sim.Content, tower, TowerUpgradePath.Range);
            Check(quote.IsValid && quote.Price == 10 && quote.Current.Range == 150 && quote.Next.Range == 162,
                "Unaffordable upgrade no longer exposes a correct UI quote");
            Equal(Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, 1, tower.Id).Code, ErrorCode.InsufficientGold, "Unaffordable path accepted");
            Equal(Fingerprint(sim.State), before, "Unaffordable path changed state");
            Equal(Command(sim, PlayerSlot.One, CommandType.UpgradeTower, 1, tower.Id).Code, ErrorCode.NotOwner, "Foreign path upgrade permitted");
            Equal(Fingerprint(sim.State), before, "Foreign path request changed state");
            foreach (int path in new[] { -1, 2, 4, 256, 258, int.MaxValue })
            {
                Equal(Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, path, tower.Id).Code, ErrorCode.InvalidKind,
                    "Invalid or Income combat path accepted, possibly after byte wrapping");
                Equal(Fingerprint(sim.State), before, "Invalid path changed state");
            }
            foreach (int path in new[] { 0, 1 })
                Equal(Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, path, mine.Id).Code, ErrorCode.InvalidKind, "Mine accepted a combat path");
            Equal(Fingerprint(sim.State), before, "Mine's rejected path changed state");
            Equal(TowerUpgradeRules.Quote(sim.Content, tower, (TowerUpgradePath)255).Code, ErrorCode.InvalidKind, "Quote accepted invalid enum");
            sim.State.Phase = MatchPhase.Finished;
            Check(TowerUpgradeRules.Quote(sim.Content, tower, TowerUpgradePath.Damage).IsValid, "Pure quote depends on match phase");
            Equal(Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, 0, tower.Id).Code, ErrorCode.WrongPhase, "Finished match accepted upgrade");
            foreach (TowerState invalid in new[] {
                new TowerState { Kind = 0, Level = 1 }, new TowerState { Kind = 0, Level = 2, DamageLevel = 2, RangeLevel = 1 },
                new TowerState { Kind = 0, DamageLevel = -1 }, new TowerState { Kind = 0, Level = 3, DamageLevel = 4 },
                new TowerState { Kind = 4, Level = 1, RangeLevel = 1 }, new TowerState { Kind = 4, Level = -1 } })
            {
                Equal(TowerUpgradeRules.ValidateState(sim.Content, invalid), ErrorCode.InvalidValue, "Invalid path invariant passed shared validation");
                Equal(TowerUpgradeRules.Quote(sim.Content, invalid, TowerUpgradePath.Damage).Code, ErrorCode.InvalidValue, "Quote ignored invalid state");
                int failures = 0;
                try { TowerUpgradeRules.Stats(sim.Content, invalid); } catch (ArgumentException) { failures++; }
                try { TowerUpgradeRules.VisualVariant(invalid); } catch (ArgumentException) { failures++; }
                Equal(failures, 2, "Stats/model silently clamped invalid upgrade state");
            }
        }

        public static void UpgradeReplayAndRematchPreservePathIntegrity()
        {
            MatchSimulation sim = Started(RichContent());
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0); TowerState tower = sim.State.Towers[0];
            var first = new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = sim.LastSequence(PlayerSlot.Zero) + 1,
                Type = CommandType.UpgradeTower, Kind = 0, Target = tower.Id };
            Check(sim.Execute(PlayerSlot.Zero, first).Accepted, "Damage purchase failed");
            string before = Fingerprint(sim.State); Check(sim.Execute(PlayerSlot.Zero, first).Accepted, "Upgrade receipt did not replay");
            Equal(Fingerprint(sim.State), before, "Repeated upgrade charged or advanced twice");
            first.Kind = 1;
            Equal(sim.Execute(PlayerSlot.Zero, first).Code, ErrorCode.Duplicate, "Changed path reused an accepted request ID");
            Equal(Fingerprint(sim.State), before, "Changed duplicate path advanced tower");
            Check(Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, 1, tower.Id).Accepted, "Fresh mixed-path purchase rejected");
            Equal(TowerUpgradeRules.VisualVariant(tower), 4, "Mixed path did not select mixed model");
            FinishAtLastWave(sim);
            Command(sim, PlayerSlot.Zero, CommandType.RequestRematch); Command(sim, PlayerSlot.One, CommandType.RequestRematch);
            Command(sim, PlayerSlot.Zero, CommandType.SetReady); Command(sim, PlayerSlot.One, CommandType.SetReady);
            while (sim.State.Phase == MatchPhase.Countdown) sim.Step();
            ActivateArithmeticFixture(sim);
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0); tower = sim.State.Towers[0];
            Equal(tower.Level + tower.DamageLevel + tower.RangeLevel, 0, "Rematch retained upgrade paths");
            Equal(TowerUpgradeRules.VisualVariant(tower), 0, "Rematch retained upgraded model");
            Equal(sim.Execute(PlayerSlot.Zero, first).Code, ErrorCode.WrongSession, "Old upgrade request crossed rematch epoch");
        }

        public static void UpgradeCombatUsesChosenStatsAndBaseInterval()
        {
            MatchSimulation sim = Started(RichContent());
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0); TowerState tower = sim.State.Towers[0];
            Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, 0, tower.Id);
            Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, 0, tower.Id);
            UnitState target = AddUnit(sim, 500, PlayerSlot.Zero, 0, 100, 1000);
            sim.Step(); Equal(target.Hp, 992, "Damage II did not affect actual combat");
            Steps(sim, 7); Equal(target.Hp, 992, "Upgrade secretly accelerated the base 8-tick interval");
            sim.Step(); Equal(target.Hp, 984, "Base interval failed to fire at its normal time");

            sim = Started(RichContent()); Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0); tower = sim.State.Towers[0];
            Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, 1, tower.Id);
            Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, 1, tower.Id);
            target = AddUnit(sim, 500, PlayerSlot.Zero, 0, 240, 1000);
            sim.Step(); Equal(target.Hp, 994, "Range II did not reach a target beyond base range or incorrectly added damage");

            sim = Started(RichContent()); Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0); tower = sim.State.Towers[0];
            tower.CooldownTicks = 5; target = AddUnit(sim, 500, PlayerSlot.Zero, 0, 100, 1000);
            Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, 0, tower.Id);
            Steps(sim, 4); Equal(target.Hp, 1000, "Upgrade manufactured an immediate shot during cooldown");
            sim.Step(); Equal(target.Hp, 993, "Preserved cooldown did not end normally");
        }

        public static void UpgradeDefinitionsRejectContradictoryPaths()
        {
            GameContent standard = GameContent.CreateDefault(); TowerDefinition original = standard.Towers[0];
            int failures = 0;
            foreach (UpgradeDefinition invalid in new[] {
                new UpgradeDefinition(55, 6, 180, 8), new UpgradeDefinition(55, 9, 150, 8),
                new UpgradeDefinition(55, 9, 180, 7), new UpgradeDefinition(55, 9, 180, 8, 1) })
            {
                TowerDefinition[] towers = standard.Towers;
                towers[0] = new TowerDefinition(original.Name, original.Price, original.Range, original.Damage,
                    original.FireIntervalTicks, original.SplashRadius, original.SlowPermille, original.SlowDurationTicks,
                    original.ArmorPiercing, new[] { invalid, original.Upgrades[1] });
                try { new GameContent(standard.Rules, towers, standard.Units, standard.Map); }
                catch (ArgumentException) { failures++; }
            }
            Equal(failures, 4, "Non-increasing paths or hidden interval/income changes accepted");
            TowerDefinition[] changed = standard.Towers;
            changed[0] = new TowerDefinition(original.Name, original.Price, original.Range, original.Damage,
                original.FireIntervalTicks, original.SplashRadius, original.SlowPermille, original.SlowDurationTicks,
                original.ArmorPiercing, new[] { new UpgradeDefinition(55, 9, 181, 8), original.Upgrades[1] });
            Check(new GameContent(standard.Rules, changed, standard.Units, standard.Map).ContentHash != standard.ContentHash,
                "Explicit range-path definition is absent from the content hash");
        }

        public static void FreeMinesRespectGeometryWithoutClassCaps()
        {
            MatchSimulation sim = Started(FreeContent());
            var invalidPositions = new[] { new MapPoint(29, 100), new MapPoint(250, 231), new MapPoint(450, 100) };
            var codes = new[] { ErrorCode.OutsideMap, ErrorCode.OnPath, ErrorCode.BlockedArea };
            for (int i = 0; i < invalidPositions.Length; i++)
            {
                string before = Fingerprint(sim.State);
                MapPoint point = invalidPositions[i];
                Equal(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 4, -1, point.X, point.Y).Code, codes[i], "Free mine bypassed shared terrain rules");
                Equal(Fingerprint(sim.State), before, "Invalid free mine charged gold");
            }
            var first = new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = sim.LastSequence(PlayerSlot.Zero) + 1,
                Type = CommandType.BuildTower, Kind = 4, Target = -1, PositionX = 173, PositionY = 157 };
            Check(sim.Execute(PlayerSlot.Zero, first).Accepted, "Mine required a hidden fixed site");
            TowerState mine = sim.State.Towers[0];
            Equal(mine.Position.X, 173, "Mine X snapped"); Equal(mine.Position.Y, 157, "Mine Y snapped"); Equal(mine.SiteId, -1, "Free mine retained site ID");
            string state = Fingerprint(sim.State); sim.Execute(PlayerSlot.Zero, first);
            Equal(Fingerprint(sim.State), state, "Duplicate free mine charged or built twice");
            Equal(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, -1, 232, 157).Code, ErrorCode.TooClose, "Combat overlapped a free mine");
            Equal(Fingerprint(sim.State), state, "Building overlap spent gold");
            Check(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 4, -1, 900, 100).Accepted, "Second free mine rejected");
            state = Fingerprint(sim.State);
            Check(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 4, -1, 300, 100).Accepted, "Third free mine capped");
            Check(Command(sim, PlayerSlot.One, CommandType.BuildTower, 4, -1, 173, 157).Accepted, "Other player's free mine blocked this field");
            for (int level = 1; level <= 2; level++)
            {
                Check(Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, 2, mine.Id).Accepted, "Free mine Income purchase rejected");
                Equal(mine.DamageLevel + mine.RangeLevel, 0, "Mine Income advanced combat paths");
                Equal(TowerUpgradeRules.Stats(sim.Content,mine).IncomeUnitsPerSecond, 960+96*level+12*level*(level-1), "Free mine Income quote and simulation disagree");
                Equal(TowerUpgradeRules.VisualVariant(mine), level, "Free mine model level incorrect");
            }
            Equal(PlacementValidator.ValidatePosition(sim.Content, sim.State, mine.Owner, mine.Kind, mine.SiteId, mine.Position, mine.Id),
                ErrorCode.None, "Upgraded free mine failed snapshot geometry");
        }

        public static void FormerMineSitesAreOrdinaryFreeGround()
        {
            int[,] priorSites = { { 0, 29 }, { 23, 27 }, { 0, 31 } };
            GameContent original = GameContent.CreateDefault(new GameRules(startGold: 10000));
            GameContent[] maps = MapCatalog.CreateContents(original);
            Check(maps.Length>=priorSites.GetLength(0), "Historical maps missing");
            for (int mapIndex = 0; mapIndex < priorSites.GetLength(0); mapIndex++)
            {
                GameContent content = maps[mapIndex]; MatchSimulation sim = Started(content);
                Equal(content.Map.MineSlots.Length, 0, "Catalog still has fixed mine indices");
                Check(!content.Map.HasDedicatedMineSlots, "Catalog claims dedicated mine sites");
                for (int site = 0; site < 2; site++)
                {
                    MapPoint point = content.Map.BuildSlots[priorSites[mapIndex, site]];
                    Check(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, -1, point.X, point.Y).Accepted,
                        "An obsolete mine reservation still blocks ordinary combat placement");
                    Equal(sim.State.Towers[site].SiteId, -1, "Former mine location became a hidden fixed target");
                }
            }
        }

        public static void ArmorAndPiercingApplyDuringCombat()
        {
            MatchSimulation sim = Started(RichContent());
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0);
            UnitState tank = AddUnit(sim, 500, PlayerSlot.Zero, 2, 90);
            var hp= tank.Hp;
            sim.Step(); Equal(hp - tank.Hp, 1, "Tank armor did not resist quick fire");
            Command(sim, PlayerSlot.Zero, CommandType.SellTower, 0, sim.State.Towers[0].Id);
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 3, 0);
            hp = tank.Hp; sim.Step(); Equal(hp - tank.Hp, 40, "Piercing did not bypass tank armor");
        }

        public static void TargetPriorityUsesDistanceThenStableId()
        {
            MatchSimulation sim = Started(RichContent());
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0);
            UnitState lower = AddUnit(sim, 900, PlayerSlot.Zero, 0, 100);
            UnitState tieHigh = AddUnit(sim, 800, PlayerSlot.Zero, 0, 140);
            UnitState tieLow = AddUnit(sim, 700, PlayerSlot.Zero, 0, 140);
            sim.Step();
            Equal(lower.Hp, 34, "Tower targeted a less advanced unit");
            Equal(tieHigh.Hp, 34, "Tower ignored stable ID tie breaker");
            Equal(tieLow.Hp, 28, "Tower did not hit selected target");
        }

        public static void FrostIsBoundedAndExpires()
        {
            MatchSimulation sim = Started(RichContent());
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 2, 1);
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 2, 6);
            UnitState unit = AddUnit(sim, 500, PlayerSlot.Zero, 2, 230, 100000);
            sim.Step();
            Equal(unit.SlowPermille, 650, "Frost stacked multiplicatively");
            int firstDistance = unit.Distance;
            for (int i = 0; i < 80; i++)
            {
                unit.Distance = 230 + (unit.Distance - firstDistance) % 30;
                sim.Step();
                Check(unit.SlowPermille >= sim.Content.Rules.MinimumSlowPermille, "Frost immobilized a unit");
            }
            sim.State.Towers.Clear();
            Steps(sim, 31);
            Equal(unit.SlowPermille, 1000, "Frost never expired");
            Equal(unit.SlowUntilTick, 0, "Expired status remained active");
        }

        public static void CannonHitsOnlyNearbyEnemies()
        {
            MatchSimulation sim = Started(RichContent());
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 1, 0);
            UnitState target = AddUnit(sim, 500, PlayerSlot.Zero, 0, 100);
            UnitState near = AddUnit(sim, 501, PlayerSlot.Zero, 0, 80);
            UnitState far = AddUnit(sim, 502, PlayerSlot.Zero, 0, 800);
            UnitState otherBoard = AddUnit(sim, 503, PlayerSlot.One, 0, 100);
            sim.Step();
            Equal(target.Hp, 18, "Cannon did not hit primary target");
            Equal(near.Hp, 18, "Cannon splash missing");
            Equal(far.Hp, 34, "Cannon splash exceeded radius");
            Equal(otherBoard.Hp, 34, "Cannon damaged other board");
        }

        public static void DeadAndEscapedUnitsAreResolvedOnce()
        {
            MatchSimulation sim = Started();
            AddUnit(sim, 500, PlayerSlot.Zero, 0, sim.Content.Map.Length, 0);
            AddUnit(sim, 501, PlayerSlot.Zero, 2, sim.Content.Map.Length, 100);
            sim.Step(); Equal(sim.State.Players[0].Hp, 27, "Dead troop damaged base or tank damage incorrect");
            Equal(sim.State.Units.Count, 0, "Resolved units retained");
            Steps(sim, 10); Equal(sim.State.Players[0].Hp, 27, "Escaped troop damaged base repeatedly");
        }

        public static void SimultaneousDestructionDraws()
        {
            MatchSimulation sim = Started();
            sim.State.Players[0].Hp = sim.State.Players[1].Hp = 1;
            AddUnit(sim, 500, PlayerSlot.Zero, 0, sim.Content.Map.Length);
            AddUnit(sim, 501, PlayerSlot.One, 0, sim.Content.Map.Length);
            sim.Step(); Equal(sim.State.Outcome, MatchOutcome.Draw, "Same-tick deaths created a biased winner");
            Equal(sim.State.Phase, MatchPhase.Finished, "Simultaneous destruction did not finish match");
        }

        public static void LethalShotPreventsSameTickBaseDamage()
        {
            MatchSimulation sim = Started();
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 11);
            AddUnit(sim, 500, PlayerSlot.Zero, 0, sim.Content.Map.Length - 1, 4);
            sim.Step();
            Equal(sim.State.Units.Count, 0, "Lethally hit unit remained active");
            Equal(sim.State.Players[0].Hp, 30, "Unit killed this tick also damaged the base");
        }

        public static void FixedTickIncomeAndFractionalMovementAreExact()
        {
            MatchSimulation sim = Started();
            UnitState unit = AddUnit(sim, 500, PlayerSlot.Zero, 0, 0);
            var initialGold= sim.State.Players[0].Gold;
            Steps(sim, 19);
            Equal(sim.State.Players[0].Gold, initialGold, "Passive income paid too early");
            sim.Step();
            Equal(sim.State.Players[0].Gold, initialGold + sim.Content.PassiveGoldPerSecond, "One-second income incorrect");
            Equal(unit.Distance, sim.Content.Units[0].SpeedPerSecond, "Integer movement rounding lost fractional distance");
            unit.SlowPermille = 650; unit.SlowUntilTick = sim.State.PlayTicks + 40;
            int before = unit.Distance;
            Steps(sim, 20);
            Equal(unit.Distance - before, sim.Content.Units[0].SpeedPerSecond * 650 / 1000, "Slow movement did not retain fractional units");
            Check(unit.MovementRemainder > 0, "Fractional slow distance was discarded");
        }

        public static void LastWaveAndFinishedOutcomeAreStable()
        {
            MatchSimulation sim = Started();
            sim.State.Players[0].Hp = 17; sim.State.Players[1].Hp = 16;
            FinishAtLastWave(sim); Equal(sim.State.Outcome, MatchOutcome.Player0, "HP comparison at time limit incorrect");
            string before = Fingerprint(sim.State);
            Steps(sim, 10); sim.Abort();
            Equal(Fingerprint(sim.State), before, "Final outcome was changed after completion");
            sim = Started(); FinishAtLastWave(sim); Equal(sim.State.Outcome, MatchOutcome.Draw, "Equal HP last wave was not a draw");
        }

        public static void RematchRequiresBothAndResetsEpoch()
        {
            MatchSimulation sim = Started();
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0);
            ulong oldEpoch = sim.State.SessionId;
            FinishAtLastWave(sim);
            Check(Command(sim, PlayerSlot.Zero, CommandType.RequestRematch).Accepted, "First rematch vote rejected");
            Equal(sim.State.Phase, MatchPhase.Finished, "Unilateral rematch started");
            Check(Command(sim, PlayerSlot.One, CommandType.RequestRematch).Accepted, "Second rematch vote rejected");
            Equal(sim.State.Phase, MatchPhase.Lobby, "Both rematch votes failed");
            Equal(sim.State.SessionId, oldEpoch + 1, "Rematch did not advance epoch");
            Equal(sim.State.Towers.Count, 0, "Rematch retained old towers");
            CheckStartWallets(sim, false);
            Equal(sim.LastSequence(PlayerSlot.Zero), 0UL, "Rematch did not reset sequence cache");
            Equal(sim.Execute(PlayerSlot.Zero, new CommandEnvelope { SessionId = oldEpoch, Sequence = 1,
                Type = CommandType.SetReady }).Code, ErrorCode.WrongSession, "Old-session command crossed epoch");
            Check(Command(sim, PlayerSlot.Zero, CommandType.SetReady).Accepted, "Fresh-session sequence was rejected");
            CheckStartWallets(sim, false);
            var finalReady = new CommandEnvelope { SessionId=sim.State.SessionId, Sequence=sim.LastSequence(PlayerSlot.One)+1,
                Type=CommandType.SetReady, Target=-1, ExpectedConfigRevision=sim.State.ConfigRevision, ExpectedRosterRevision=sim.State.RosterRevision };
            Check(sim.Execute(PlayerSlot.One, finalReady).Accepted, "Final rematch ready rejected");
            CheckStartWallets(sim, true);
            Check(sim.Execute(PlayerSlot.One, finalReady).Accepted, "Final-ready replay lost its accepted result");
            CheckStartWallets(sim, true);
            Equal(sim.Content.ContentHash, RuleContent().ContentHash, "Match mutated definitions");
        }

        public static void AbortHasNoWinner()
        {
            MatchSimulation sim = Started(); sim.State.Players[0].Hp = 1;
            sim.Abort(); Equal(sim.State.Outcome, MatchOutcome.Aborted, "Abort invented a winner");
            string before = Fingerprint(sim.State); Steps(sim, 20);
            Equal(Fingerprint(sim.State), before, "Aborted simulation kept running");
            Equal(Command(sim, PlayerSlot.Zero, CommandType.RequestRematch).Code, ErrorCode.WrongPhase, "Aborted connection silently resumed");
        }

        public static void RateLimitRejectsWithoutMutationAndRecovers()
        {
            MatchSimulation sim = Started(RichContent());
            for (int i = 0; i < sim.Content.Rules.MaxCommandsPerSecond; i++) Command(sim, PlayerSlot.Zero, CommandType.BuildTower, -1, -1);
            string before = Fingerprint(sim.State);
            Equal(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0).Code, ErrorCode.RateLimited, "Command rate was unbounded");
            Equal(Fingerprint(sim.State), before, "Rate limited command changed state");
            Steps(sim, 20);
            Check(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0).Accepted, "Rate limiter never recovered");
        }

        public static void RandomCommandsPreserveEconomyAndGeometry()
        {
            MatchSimulation sim = Started();
            var random = new Random(417);
            for (int i = 0; i < 16000 && sim.State.Phase == MatchPhase.Playing; i++)
            {
                PlayerSlot player = (PlayerSlot)random.Next(2);
                CommandType type = (CommandType)random.Next(-1, 8);
                int target = random.Next(-5, 40);
                string before = Fingerprint(sim.State);
                CommandResult result = Command(sim, player, type, random.Next(-2, 6), target);
                if (!result.Accepted) Equal(Fingerprint(sim.State), before, "Random rejected command changed state at " + i);
                for (int p = 0; p < 2; p++)
                {
                    Check(sim.State.Players[p].Gold >= 0, "Gold became negative");
                    Check(EconomyRules.ValidateLedger(sim.Content,sim.State,(PlayerSlot)p)==ErrorCode.None, "Paid queue ledger invalid");
                }
                if (i % 2 == 0) sim.Step();
            }
        }

        public static void BotsAreReproducibleAndUseNormalRules()
        {
            MatchSimulation a = new MatchSimulation(GameContent.CreateDefault()), b = new MatchSimulation(GameContent.CreateDefault());
            Command(a,PlayerSlot.Zero,CommandType.SetReady);Command(a,PlayerSlot.One,CommandType.SetReady);
            Command(b,PlayerSlot.Zero,CommandType.SetReady);Command(b,PlayerSlot.One,CommandType.SetReady);
            var a0 = new BotBrain(PlayerSlot.Zero, 45); var a1 = new BotBrain(PlayerSlot.One, 92);
            var b0 = new BotBrain(PlayerSlot.Zero, 45); var b1 = new BotBrain(PlayerSlot.One, 92);
            for (int i = 0; i < 100000 && a.State.Phase != MatchPhase.Finished; i++)
            {
                if(a.State.Phase==MatchPhase.Playing && a.State.WavePhase==WavePhase.Preparing){a0.Tick(a);a1.Tick(a);b0.Tick(b);b1.Tick(b);}
                a.Step(inner=>{a0.Tick(inner);a1.Tick(inner);});b.Step(inner=>{b0.Tick(inner);b1.Tick(inner);});
                if (i % 100 == 0) Equal(Fingerprint(a.State), Fingerprint(b.State), "Seeded bot diverged");
                Check(a.State.Players[0].Gold >= 0 && a.State.Players[1].Gold >= 0, "Bot overspent");
                Check(!a.State.Players[0].SpeedReady && !a.State.Players[1].SpeedReady, "Bot spontaneously voted for acceleration");
                Equal(a.State.SimulationSpeed, 1, "Bot match accelerated without human agreement");
            }
            Equal(a.State.Phase, MatchPhase.Finished, "Bot match never finished");
            Check(a.State.Towers.Count > 0, "Bot never built");
            bool upgraded = false;
            foreach (TowerState t in a.State.Towers) if (t.Level > 0) upgraded = true;
            Check(upgraded, "Bot never demonstrated upgrade");
        }

        public static void ArmiesGrowAboveLegacyFieldCapacity()
        {
            MatchSimulation sim = Started(RichContent());
            for (int p = 0; p < 2; p++)
                for (int i = 0; i < 80; i++) AddUnit(sim, 500 + p * 80 + i, (PlayerSlot)p, 2, i, 100000);
            Command(sim, PlayerSlot.Zero, CommandType.SendUnit, 3);
            Command(sim, PlayerSlot.One, CommandType.SendUnit, 3);
            Steps(sim, 100);
            Equal(sim.State.Units.Count, 168, "Paid troops blocked by legacy field size");
            Equal(sim.State.SpawnOrders.Count, 0, "Completed packets retained under load");
        }

        public static void MapCatalogIsDistinctValidAndClear()
        {
            GameContent[] contents = MapCatalog.CreateContents(GameContent.CreateDefault());
            Equal(contents.Length, 6, "Expected six selectable maps");
            var ids = new HashSet<string>(); var hashes = new HashSet<string>();
            foreach (GameContent content in contents)
            {
                content.Validate(); MapDefinition map = content.Map;
                Check(ids.Add(map.Id) && hashes.Add(content.ContentHash), "Maps or hashes are not distinct");
                Equal(map.BuildSlots.Length, map.Width==1600?32:1, "Legacy hints or large-map anchor changed");
                Check(map.Width==1600||map.Width==2400, "Unexpected map width");
                Equal(map.Height, map.Width==1600?960:1440, "Map aspect changed");
                Check(map.FreePlacement && !map.HasDedicatedMineSlots && map.MineSlots.Length == 0, "Catalog retained fixed mine reservations");
                MapPoint[] route = map.Waypoints;
                Check(map.Length > 1000 && route[0].X < map.Width / 10 && route[route.Length - 1].X > map.Width * 9 / 10, "Route endpoints/length invalid");
                double clearance = double.MaxValue;
                foreach (MapPoint slot in map.BuildSlots)
                {
                    Check(slot.X >= 0 && slot.X <= map.Width && slot.Y >= 0 && slot.Y <= map.Height, "Slot outside board");
                    for (int segment = 0; segment + 1 < route.Length; segment++)
                    {
                        double dx = route[segment + 1].X - route[segment].X, dy = route[segment + 1].Y - route[segment].Y;
                        double px = slot.X - route[segment].X, py = slot.Y - route[segment].Y;
                        double fraction = Math.Max(0, Math.Min(1, (px * dx + py * dy) / (dx * dx + dy * dy)));
                        double distance = Math.Sqrt(Math.Pow(px - fraction * dx, 2) + Math.Pow(py - fraction * dy, 2));
                        clearance = Math.Min(clearance, distance);
                        Check(distance >= 40, map.Id + " contains a build slot on/too near the path");
                    }
                }
                Console.WriteLine("MAP_GEOMETRY " + map.Id + " length=" + map.Length + " slots=" + map.BuildSlots.Length + " min_clearance="
                    + clearance.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " hash=" + content.ContentHash);
            }
        }

        public static void MapMetadataAndEconomyParticipateInHash()
        {
            GameContent template = GameContent.CreateDefault(); MapDefinition map = template.Map;
            string baseline = template.ContentHash;
            MapDefinition[] variants =
            {
                new MapDefinition(map.Waypoints, map.BuildSlots, "meadow2", map.Name, map.Theme, map.Width, map.Height, map.MineSlots, map.FreePlacement, map.BlockedAreas),
                new MapDefinition(map.Waypoints, map.BuildSlots, map.Id, "Anderes Tal", map.Theme, map.Width, map.Height, map.MineSlots, map.FreePlacement, map.BlockedAreas),
                new MapDefinition(map.Waypoints, map.BuildSlots, map.Id, map.Name, 1, map.Width, map.Height, map.MineSlots, map.FreePlacement, map.BlockedAreas),
                new MapDefinition(map.Waypoints, map.BuildSlots, map.Id, map.Name, map.Theme, map.Width + 1, map.Height, map.MineSlots, map.FreePlacement, map.BlockedAreas),
                new MapDefinition(map.Waypoints, map.BuildSlots, map.Id, map.Name, map.Theme, map.Width, map.Height + 1, map.MineSlots, map.FreePlacement, map.BlockedAreas)
            };
            foreach (MapDefinition variant in variants)
                Check(new GameContent(template.Rules, template.Towers, template.Units, variant).ContentHash != baseline,
                    "Map identity/name/theme/dimensions missing from content hash");
            TowerDefinition[] towers = template.Towers;
            towers[4] = MineDefinition(1, 1, 2, 3);
            Check(new GameContent(template.Rules, towers, template.Units, map).ContentHash != baseline, "Mine cap missing from hash");
            towers[4] = MineDefinition(2, 2, 3, 4);
            Check(new GameContent(template.Rules, towers, template.Units, map).ContentHash != baseline, "Mine income missing from hash");
            towers[4] = MineDefinition(1, 2, 3, 4);
            Check(new GameContent(template.Rules, towers, template.Units, map).ContentHash != baseline, "Mine upgrades missing from hash");
        }

        private static TowerDefinition MineDefinition(int income, int maxPerPlayer, int upgrade1, int upgrade2)
            => new TowerDefinition("Goldmine", 140, 0, 0, 0, 0, 1000, 0, 0,
                new[] { new UpgradeDefinition(100, 0, 0, 0, upgrade1), new UpgradeDefinition(160, 0, 0, 0, upgrade2) }, income, maxPerPlayer);

        public static void MapCatalogPreservesTemplateMapAndBalance()
        {
            GameContent standard = GameContent.CreateDefault();
            var customMap = new MapDefinition(standard.Map.Waypoints, standard.Map.BuildSlots, "custom-meadow", "Mein Tal", 0,
                standard.Map.Width, standard.Map.Height, standard.Map.MineSlots, standard.Map.FreePlacement, standard.Map.BlockedAreas);
            var template = new GameContent(new GameRules(passiveGoldPerSecond: 7), standard.Towers, standard.Units, customMap);
            GameContent[] catalog = MapCatalog.CreateContents(template);
            Check(ReferenceEquals(catalog[0].Map, customMap), "Catalog replaced the ScriptableObject template's custom map");
            foreach (GameContent content in catalog)
            {
                Check(ReferenceEquals(content.Rules, template.Rules), "Catalog changed balance settings");
                Equal(content.PassiveGoldPerSecond, 7, "Catalog lost custom economy");
                Check(ReferenceEquals(content.Towers[4], template.Towers[4]), "Catalog replaced immutable mine definition");
                Equal(content.Units[2].Price, template.Units[2].Price, "Catalog changed troop price");
            }
            catalog[1] = null;
            Check(MapCatalog.CreateContents(template)[1] != null, "Catalog exposed shared mutable array");
        }

        public static void InvalidMapAndMineDefinitionsFailEarly()
        {
            GameContent template = GameContent.CreateDefault(); MapDefinition map = template.Map; int failures = 0;
            try { new MapDefinition(map.Waypoints, map.BuildSlots, "../map"); } catch (ArgumentException) { failures++; }
            try { new MapDefinition(map.Waypoints, map.BuildSlots, name: " "); } catch (ArgumentException) { failures++; }
            try { new MapDefinition(map.Waypoints, map.BuildSlots, theme: 3); } catch (ArgumentException) { failures++; }
            try { MapCatalog.Create(-1); } catch (ArgumentException) { failures++; }
            try { MapCatalog.Create(MapCatalog.Count); } catch (ArgumentException) { failures++; }
            MapPoint[] slots = map.BuildSlots; slots[0] = map.Waypoints[0];
            try { new GameContent(template.Rules, template.Towers, template.Units,
                new MapDefinition(map.Waypoints, slots, width: map.Width, height: map.Height)); }
            catch (ArgumentException) { failures++; }
            TowerDefinition[] towers = template.Towers;
            towers[4] = MineDefinition(1, 3, 2, 3);
            Check(new GameContent(template.Rules, towers, template.Units, map) != null, "Legacy mine hint of three was rejected");
            towers[4] = MineDefinition(1, 2, 0, 3);
            try { new GameContent(template.Rules, towers, template.Units, map); } catch (ArgumentException) { failures++; }
            towers[4] = new TowerDefinition("Invalid armed mine", 140, 150, 6, 8, 0, 1000, 0, 0,
                new[] { new UpgradeDefinition(100, 0, 0, 0, 2), new UpgradeDefinition(160, 0, 0, 0, 3) }, 1, 2);
            try { new GameContent(template.Rules, towers, template.Units, map); } catch (ArgumentException) { failures++; }
            Equal(failures, 8, "Invalid map/economy content was accepted");
        }

        public static void MinesRespectAffordabilityWithoutClassLimits()
        {
            MatchSimulation poor = Started();
            Check(Command(poor, PlayerSlot.Zero, CommandType.BuildTower, 0, 0).Accepted, "Quick-fire setup failed");
            string before = Fingerprint(poor.State);
            Equal(Command(poor, PlayerSlot.Zero, CommandType.BuildTower, 4, 1).Code, ErrorCode.InsufficientGold, "Mine bypassed affordability");
            Equal(Fingerprint(poor.State), before, "Unfunded mine changed state");
            MatchSimulation sim = Started(RichContent());
            Check(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 4, 0).Accepted, "First mine rejected");
            Check(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 4, 1).Accepted, "Second mine rejected");
            before = Fingerprint(sim.State);
            var gold=sim.State.Players[0].Gold; Check(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 4, 2).Accepted, "Third mine capped");
            Equal(gold-sim.State.Players[0].Gold,sim.Content.Towers[4].Price,"Third mine debit");
            Check(Command(sim, PlayerSlot.One, CommandType.BuildTower, 4, 0).Accepted, "Mine cap incorrectly shared between players");
            Equal(Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 0, 0).Code, ErrorCode.Occupied, "Mine did not occupy its build slot");
        }

        public static void MineLevelsPayExactIncomeAndNeverShoot()
        {
            MatchSimulation sim = Started(RichContent());
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 4, 0);
            TowerState mine = sim.State.Towers[0];
            UnitState enemy = AddUnit(sim, 500, PlayerSlot.Zero, 2, 90);
            var hp= enemy.Hp;
            for (int level = 0; level < 3; level++)
            {
                if (level > 0) Check(Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, (int)TowerUpgradePath.Income, mine.Id).Accepted, "Mine upgrade rejected");
                var gold= sim.State.Players[0].Gold; var remainder=sim.State.Players[0].GoldRemainder;
                Steps(sim, 40);
                Equal((sim.State.Players[0].Gold-gold)*960+sim.State.Players[0].GoldRemainder-remainder,2*(sim.Content.PassiveGoldPerSecond*960+960+96*level+12*level*(level-1)),"Exact fractional mine income");
                Equal(enemy.Hp, hp, "Gold mine fired at an enemy");
                Equal(mine.CooldownTicks, 0, "Gold mine entered attack cooldown");
            }
            Check(Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, (int)TowerUpgradePath.Income, mine.Id).Accepted, "Third mine upgrade capped");
            Equal(mine.Invested, 209, "Mine upgrade costs not accounted centrally");
        }

        public static void MineSaleStopsIncomeAndDuplicatesCannotProfit()
        {
            MatchSimulation sim = Started(RichContent());
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 4, 0);
            TowerState mine = sim.State.Towers[0];
            Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, (int)TowerUpgradePath.Income, mine.Id);
            Command(sim, PlayerSlot.Zero, CommandType.UpgradeTower, (int)TowerUpgradePath.Income, mine.Id);
            Steps(sim, 19);
            var before= sim.State.Players[0].Gold;
            var sell = new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = sim.LastSequence(PlayerSlot.Zero) + 1,
                Type = CommandType.SellTower, Target = mine.Id };
            Check(sim.Execute(PlayerSlot.Zero, sell).Accepted, "Mine sale rejected");
            Equal(sim.State.Players[0].Gold - before, 75, "Mine refund did not use full investment");
            before = sim.State.Players[0].Gold;
            sim.Execute(PlayerSlot.Zero, sell);
            Equal(sim.State.Players[0].Gold, before, "Duplicate mine sale paid twice");
            sim.Step(); Equal(sim.State.Players[0].Gold - before, sim.Content.PassiveGoldPerSecond, "Sold mine paid queued income on the next tick");
            before = sim.State.Players[0].Gold; Steps(sim, 20);
            Equal(sim.State.Players[0].Gold - before, sim.Content.PassiveGoldPerSecond, "Sold mine retained a recurring income listener");
        }

        public static void MineRematchRestoresBaseEconomy()
        {
            MatchSimulation sim = Started(RichContent());
            Command(sim, PlayerSlot.Zero, CommandType.BuildTower, 4, 0);
            FinishAtLastWave(sim);
            Command(sim, PlayerSlot.Zero, CommandType.RequestRematch); Command(sim, PlayerSlot.One, CommandType.RequestRematch);
            Equal(sim.State.Towers.Count, 0, "Rematch retained mines");
            CheckStartWallets(sim, false);
            Command(sim, PlayerSlot.Zero, CommandType.SetReady);
            CheckStartWallets(sim, false);
            Command(sim, PlayerSlot.One, CommandType.SetReady);
            CheckStartWallets(sim, true);
            while (sim.State.Phase == MatchPhase.Countdown) sim.Step();
            ActivateArithmeticFixture(sim);
            sim.State.Players[0].Gold = 0; Steps(sim, 20);
            Equal(sim.State.Players[0].Gold, sim.Content.PassiveGoldPerSecond, "Mine income leaked into a new round");
        }

        public static void FullMatchesFinishOnEveryMapWithoutSideBias()
        {
            foreach (GameContent content in MapCatalog.CreateContents(GameContent.CreateDefault()))
            {
                int matches = 0, draws = 0, ticks = 0, lowHp = 0, highHp = 0;
                for (uint seed = 1; seed <= 12; seed++)
                {
                    int priorHpA = -1, priorHpB = -1, priorTicks = -1;
                    for (int swapped = 0; swapped < 2; swapped++)
                    {
                        MatchState state = RunBotMatch(content, seed, swapped, BotStrategy.Balanced, out int minesBuilt);
                        int hpA= state.Players[swapped].Hp, hpB = state.Players[1 - swapped].Hp;
                        if (swapped == 1)
                        {
                            Equal(hpA, priorHpA, "Mirrored slot assignment biased first bot on " + content.Map.Id);
                            Equal(hpB, priorHpB, "Mirrored slot assignment biased second bot on " + content.Map.Id);
                            Equal(state.PlayTicks, priorTicks, "Swapping sides changed match duration on " + content.Map.Id);
                        }
                        priorHpA = hpA; priorHpB = hpB; priorTicks = state.PlayTicks;
                        matches++; ticks += state.PlayTicks; lowHp += hpA; highHp += hpB;
                        if (state.Outcome == MatchOutcome.Draw) draws++;
                    }
                }
                Console.WriteLine("MAP_MATCHES " + content.Map.Id + " games=" + matches + " draws=" + draws + " mean_seconds="
                    + (ticks / 480.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                    + " mean_hp_a=" + (lowHp / 24.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                    + " mean_hp_b=" + (highHp / 24.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        public static void EconomyStrategyHasReproducibleMatchEvidence()
        {
            GameContent content = GameContent.CreateDefault(); int wins = 0, losses = 0, draws = 0, ticks = 0, mineEvidence = 0;
            for (uint seed = 1; seed <= 6; seed++)
                for (int swapped = 0; swapped < 2; swapped++)
                {
                    MatchState state = RunBotMatch(content, seed, swapped, BotStrategy.Economy, out int minesBuilt);
                    mineEvidence += minesBuilt; ticks += state.PlayTicks;
                    if (state.Outcome == MatchOutcome.Draw) draws++;
                    else if ((state.Outcome == MatchOutcome.Player0 ? 0 : 1) == swapped) wins++;
                    else losses++;
                }
            Check(mineEvidence > 0, "Economy bot never demonstrated a mine");
            Console.WriteLine("ECONOMY_MATCHES meadow games=12 wins=" + wins + " losses=" + losses + " draws=" + draws
                + " mean_seconds=" + (ticks / 240.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " mines_observed=" + mineEvidence);
        }

        private static MatchState RunBotMatch(GameContent content, uint seed, int swapped, BotStrategy strategy, out int minesBuilt)
        {
            var sim = new MatchSimulation(content, seed, seed);
            Command(sim, PlayerSlot.Zero, CommandType.SetReady); Command(sim, PlayerSlot.One, CommandType.SetReady);
            var a = new BotBrain((PlayerSlot)swapped, seed * 13, strategy);
            var b = new BotBrain((PlayerSlot)(1 - swapped), seed * 29, BotStrategy.Balanced);
            int maximumTicks = 100000;
            for (int i = 0; i < maximumTicks && sim.State.Phase != MatchPhase.Finished; i++)
            {
                if(sim.State.Phase==MatchPhase.Playing && sim.State.WavePhase==WavePhase.Preparing){a.Tick(sim);b.Tick(sim);}
                sim.Step(inner=>{a.Tick(inner);b.Tick(inner);});
                Check(sim.State.Players[0].Gold >= 0 && sim.State.Players[1].Gold >= 0, "Bot overspent on " + content.Map.Id);
                foreach(var tower in sim.State.Towers) Check(TowerUpgradeRules.ValidateState(content,tower)==ErrorCode.None,"Bot created invalid upgrade state");
            }
            Equal(sim.State.Phase, MatchPhase.Finished, "Map match did not finish: " + content.Map.Id);
            Check(sim.State.Outcome != MatchOutcome.None && sim.State.Outcome != MatchOutcome.Aborted, "Map match produced no valid result");
            minesBuilt = 0;
            foreach (TowerState t in sim.State.Towers) if (t.Kind == 4) minesBuilt++;
            return sim.State;
        }
    }
}
