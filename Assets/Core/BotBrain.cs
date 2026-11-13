using System;
using System.Numerics;
using System.Collections.Generic;

namespace DefenseDuell.Core
{
    public enum BotStrategy { Balanced, RunnerRush, TankRush, PureDefense, Saving, FrostDefense, Economy }
    public enum BotDifficulty { Easy = 0, Normal = 1, Hard = 2 }

    /// <summary>Deterministic small rule-based opponent. Every action is an ordinary validated command.</summary>
    public sealed class BotBrain
    {
        private readonly PlayerSlot slot;
        private readonly uint initialSeed;
        private readonly BotStrategy strategy;
        private readonly BotDifficulty difficulty;
        private uint random;
        private ulong sessionId;
        private int nextThinkTick, purchases, lastAttackTick, lastObservedHp, lastBaseDamageTick;
        private bool savingForTower;
        private BigInteger preparationWave;
        private int preparationStartTick, preparationActions, nextPreparationTick;
        private int researchedPacketKind = -1;
        private bool preparationVoted;
        private GameContent scoredContent;
        private long[,] placementScores;
        private MapPoint[] buildCandidates;
        private MapPoint[] pathSamples;
        public BotBrain(PlayerSlot slot, uint seed = 1) : this(slot, seed, BotStrategy.Balanced, BotDifficulty.Normal) { }
        public BotBrain(PlayerSlot slot, uint seed, BotDifficulty difficulty) : this(slot, seed, BotStrategy.Balanced, difficulty) { }
        public BotBrain(PlayerSlot slot, uint seed, BotStrategy strategy) : this(slot, seed, strategy, BotDifficulty.Normal) { }
        public BotBrain(PlayerSlot slot, uint seed, BotStrategy strategy, BotDifficulty difficulty)
        {
            if ((byte)slot > 1) throw new ArgumentOutOfRangeException(nameof(slot));
            if (difficulty < BotDifficulty.Easy || difficulty > BotDifficulty.Hard) throw new ArgumentOutOfRangeException(nameof(difficulty));
            this.slot = slot; initialSeed = seed == 0 ? 1 : seed; random = initialSeed; this.strategy = strategy; this.difficulty = difficulty;
        }
        private int ThinkDelay => difficulty == BotDifficulty.Easy ? 40 : difficulty == BotDifficulty.Normal ? 20 : 10;
        private int PreparationLimit => difficulty == BotDifficulty.Easy ? 3 : difficulty == BotDifficulty.Normal ? 5 : 7;
        public void Tick(MatchSimulation simulation)
        {
            if(simulation.Content.MatchRules.Format!=MatchFormat.Duel)return;
            MatchState state = simulation.State;
            if (sessionId != state.SessionId)
            {
                sessionId = state.SessionId; random = initialSeed ^ state.Seed;
                if (random == 0) random = 1;
                nextThinkTick = 0; purchases = 0; lastAttackTick = -100000; savingForTower = false;
                lastObservedHp = state.Players[(int)slot].Hp; lastBaseDamageTick = -100000;
                preparationWave = 0; preparationActions = 0; preparationVoted = false;
                researchedPacketKind = -1;
            }
            if (state.Phase != MatchPhase.Playing) return;
            if (state.WavePhase == WavePhase.Preparing)
            {
                if (preparationWave != state.WaveId)
                {
                    preparationWave = state.WaveId; preparationStartTick = nextPreparationTick = state.Tick;
                    preparationActions = 0; preparationVoted = false;
                }
                // Preparation has no gameplay clock. At most a few decisions and one own vote;
                // never write the human's vote or repeatedly restore a withdrawn bot vote.
                if (!preparationVoted && state.Tick - preparationStartTick >= 6 * simulation.Content.TickRate)
                {
                    preparationVoted = true;
                    if (!state.Players[(int)slot].WaveReady) Send(simulation, CommandType.SetWaveReady, 1, state.WaveId);
                    return;
                }
                if (state.Tick < nextPreparationTick || preparationActions >= PreparationLimit) return;
                nextPreparationTick = state.Tick + ThinkDelay; preparationActions++;
            }
            else
            {
                if (state.PlayTicks < nextThinkTick) return;
                int jitter = difficulty == BotDifficulty.Easy ? 10 : difficulty == BotDifficulty.Normal ? 5 : 3;
                nextThinkTick = state.PlayTicks + ThinkDelay + (int)(Next() % (uint)(jitter + 1));
            }
            int hp = state.Players[(int)slot].Hp;
            if (hp < lastObservedHp) lastBaseDamageTick = state.PlayTicks;
            lastObservedHp = hp;
            if (strategy == BotStrategy.Saving) return;

            int ownTowers = 0, ownMines = 0, threats = 0;
            TowerState upgradeTarget = null;
            TowerState mineUpgradeTarget = null;
            TowerDefinition[] definitions = simulation.Content.TowerData;
            for (int i = 0; i < state.Towers.Count; i++)
            {
                TowerState tower = state.Towers[i];
                if (tower.Owner != slot) continue;
                if (definitions[tower.Kind].IsEconomy)
                {
                    ownMines++;
                    if ((mineUpgradeTarget == null || tower.Level < mineUpgradeTarget.Level)) mineUpgradeTarget = tower;
                    continue;
                }
                ownTowers++;
                if ((upgradeTarget == null || tower.Level < upgradeTarget.Level)) upgradeTarget = tower;
            }
            bool defenseOnly = strategy == BotStrategy.PureDefense || strategy == BotStrategy.FrostDefense;
            bool attackOnly = strategy == BotStrategy.RunnerRush || strategy == BotStrategy.TankRush;
            int desiredTowers = defenseOnly ? (simulation.Content.Map.FreePlacement ? 30 : simulation.Content.Map.SlotData.Length)
                : difficulty == BotDifficulty.Easy ? (int)BigInteger.Min(5, 2 + (state.WaveId - 1) / 3)
                : difficulty == BotDifficulty.Normal ? (int)BigInteger.Min(10, state.WaveId + 1)
                : (int)BigInteger.Min(12, state.WaveId == 1 ? 2 : state.WaveId + 3);
            if (!attackOnly && ownTowers < desiredTowers)
                savingForTower = true;
            // Far-away traffic on long maps is not an immediate threat to mine investment.
            // Ignore slows here: unimpeded travel gives a conservative earliest arrival.
            for (int i = 0; i < state.Units.Count; i++)
            {
                UnitState unit = state.Units[i];
                if (unit.Defender != slot || unit.Hp <= 0) continue;
                UnitStats definition = UnitResearchRules.Resolve(simulation.Content, unit.Kind, unit.ResearchLevel, unit.Origin, unit.WaveId);
                if ((long)simulation.Content.Map.Length - unit.Distance <= (long)definition.SpeedPerSecond * 10)
                    threats += definition.BaseDamage;
            }
            // Once the current wave's defensive target is funded, prefer periodic real,
            // paid pressure over discretionary mines/research. This is not an attack deadline
            // while gold, wave capacity or urgent defensive savings prevent a purchase.
            if (!defenseOnly && (ownTowers > 0 || attackOnly)
                && threats <= 3 && !savingForTower
                && state.PlayTicks - lastAttackTick >= (difficulty == BotDifficulty.Easy ? 14 : 10) * simulation.Content.TickRate
                && TryAttack(simulation, economical: true)) return;
            bool canInvest = !defenseOnly && !attackOnly && !savingForTower && ownTowers >= 2 && hp * 5 >= simulation.Content.BaseHp * 4
                && state.PlayTicks - lastBaseDamageTick >= 15 * simulation.Content.TickRate
                && threats <= 3 && state.WaveId >= 2 && state.WaveId <= 6;
            if (canInvest && ownMines < (strategy == BotStrategy.Economy ? 2 : 1))
            {
                if (FindBuildPosition(simulation, 4, out int mineSite, out MapPoint minePosition))
                {
                    if (!HasInvestmentBudget(simulation, definitions[4].Price, true)) return;
                    CommandResult mine = Build(simulation, 4, mineSite, minePosition);
                    if (mine.Accepted) purchases++;
                    if (mine.Accepted || mine.Code == ErrorCode.InsufficientGold) return;
                }
            }
            if (canInvest && strategy == BotStrategy.Economy && mineUpgradeTarget != null)
            {
                UpgradeQuote quote = TowerUpgradeRules.Quote(simulation.Content, mineUpgradeTarget, TowerUpgradePath.Income);
                if (!quote.IsValid || !HasInvestmentBudget(simulation, quote.Price, true)) return;
                CommandResult upgrade = Send(simulation, CommandType.UpgradeTower, (int)quote.Path, mineUpgradeTarget.Id);
                if (upgrade.Accepted) purchases++;
                if (upgrade.Accepted || upgrade.Code == ErrorCode.InsufficientGold) return;
            }
            bool shouldBuild = !attackOnly && savingForTower && ownTowers < desiredTowers;
            if (shouldBuild)
            {
                int towerKind = ChooseTowerKind(simulation, ownTowers);
                if (towerKind >= 0 && FindBuildPosition(simulation, towerKind, out int site, out MapPoint position))
                {
                    if (!HasInvestmentBudget(simulation, definitions[towerKind].Price, !defenseOnly && ownTowers > 0)) return;
                    CommandResult build = Build(simulation, towerKind, site, position);
                    if (build.Accepted) { purchases++; savingForTower = false; }
                    // Reserve gold for the planned defensive purchase instead of spending it forever on cheaper units.
                    if (build.Accepted || build.Code == ErrorCode.InsufficientGold) return;
                }
                else savingForTower = false;
            }
            if (difficulty == BotDifficulty.Hard && !attackOnly && !defenseOnly && ownTowers >= 3 && threats <= 3
                && ShouldSaveForResearch(simulation))
            {
                // A fast thinker otherwise spends each new coin on upgrades before a
                // research quote can ever become affordable. Save for a bounded real use.
                TryResearch(simulation);
                return;
            }
            if (difficulty != BotDifficulty.Easy)
            {
                foreach (var tower in state.Towers)
                {
                    if (tower.Owner != slot || definitions[tower.Kind].IsEconomy) continue;
                    TargetPriority priority = definitions[tower.Kind].ArmorPiercing > 0 ? TargetPriority.Strongest : TargetPriority.Front;
                    if (tower.Priority != priority && Send(simulation, CommandType.SetTowerPriority, (int)priority, tower.Id).Accepted) return;
                }
            }
            if (!attackOnly && upgradeTarget != null && (difficulty != BotDifficulty.Easy || upgradeTarget.Level == 0)
                && (defenseOnly || purchases % 7 == 5 || difficulty == BotDifficulty.Hard
                    || difficulty == BotDifficulty.Normal && upgradeTarget.Level == 0))
            {
                UpgradeQuote quote = ChooseCombatUpgrade(simulation, upgradeTarget);
                if (!quote.IsValid || !HasInvestmentBudget(simulation, quote.Price, !defenseOnly)) return;
                CommandResult upgrade = Send(simulation, CommandType.UpgradeTower, (int)quote.Path, upgradeTarget.Id);
                if (upgrade.Accepted) purchases++;
                if (upgrade.Accepted || upgrade.Code == ErrorCode.InsufficientGold) return;
            }
            if (defenseOnly) return;
            if (ownTowers >= 3 && threats <= 3 && !savingForTower && TryResearch(simulation)) return;
            TryAttack(simulation);
        }

        private int ChooseTowerKind(MatchSimulation simulation, int ownTowers)
        {
            var definitions = simulation.Content.TowerData;
            int mask = simulation.State.Players[(int)slot].AvailableTowerMask;
            int best = -1, bestCount = int.MaxValue;
            for (int kind = 0; kind < definitions.Length; kind++)
            {
                if (definitions[kind].IsEconomy || (mask & (1 << kind)) == 0) continue;
                int count = 0;
                foreach (var tower in simulation.State.Towers) if (tower.Owner == slot && tower.Kind == kind) count++;
                if (strategy == BotStrategy.FrostDefense && kind == 2) return kind;
                if (ownTowers == 0 && kind == 0) return kind;
                if (count < bestCount) { best = kind; bestCount = count; }
            }
            return best;
        }

        private bool TryResearch(MatchSimulation simulation)
        {
            if (difficulty == BotDifficulty.Easy || simulation.State.WaveId < 2) return false;
            int kind = (int)((simulation.State.WaveId - 2) % 4);
            var player = simulation.State.Players[(int)slot];
            ResearchQuote research = UnitResearchRules.Quote(simulation.Content, player, kind);
            int desired = (int)BigInteger.Min(8, simulation.State.WaveId / 2 + (difficulty == BotDifficulty.Hard ? 1 : 0));
            SendQuote send = SendRules.Quote(simulation.Content, simulation.State, slot, kind);
            // Buy an improvement only if there is a usable purchase horizon and enough gold
            // left for the next real packet plus a basic defensive tower.
            if (!research.IsValid || research.Level >= desired || !send.IsValid || send.WaveId > simulation.State.WaveId + 1
                || player.Gold < research.Price + send.Price + simulation.Content.TowerData[0].Price) return false;
            CommandResult result = simulation.Execute(slot,new CommandEnvelope { SessionId=simulation.State.SessionId, Sequence=simulation.LastSequence(slot)+1, Type=CommandType.ResearchUnit, Kind=kind, Target=-1, ExpectedResearch=research.Level });
            if (result.Accepted) { purchases++; researchedPacketKind = kind; }
            return result.Accepted;
        }

        private bool ShouldSaveForResearch(MatchSimulation simulation)
        {
            var state = simulation.State;
            if (state.WaveId < 2) return false;
            BigInteger total = 0;
            foreach (BigInteger level in state.Players[(int)slot].ResearchLevels) total += level;
            if (total >= state.WaveId - 1) return false;
            int kind = (int)((state.WaveId - 2) % 4);
            var quote = SendRules.Quote(simulation.Content, state, slot, kind);
            return quote.WaveId >= state.WaveId && quote.WaveId <= state.WaveId + 1
                && (quote.IsValid || quote.Code == ErrorCode.InsufficientGold);
        }

        private UpgradeQuote ChooseCombatUpgrade(MatchSimulation simulation, TowerState tower)
        {
            var content = simulation.Content;
            UpgradeQuote damage = TowerUpgradeRules.Quote(content, tower, TowerUpgradeRules.PrimaryPath(simulation.Content,tower.Kind));
            UpgradeQuote range = TowerUpgradeRules.Quote(content, tower, TowerUpgradeRules.SecondaryPath(simulation.Content,tower.Kind));
            if (!damage.IsValid) return range;
            if (!range.IsValid) return damage;
            long radiusSquared = (long)damage.Current.Range * damage.Current.Range;
            UnitState primary = null;
            foreach (UnitState unit in simulation.State.Units)
            {
                if (unit.Defender != slot || unit.Hp <= 0) continue;
                if (DistanceSquared(content.Map.PositionAt(unit.Distance), tower.Position) > radiusSquared) continue;
                // Match authoritative target priority before estimating the secondary effect.
                if (primary == null || (tower.Priority == TargetPriority.Strongest && unit.Hp > primary.Hp)
                    || ((tower.Priority == TargetPriority.Front || unit.Hp == primary.Hp)
                        && (unit.Distance > primary.Distance || (unit.Distance == primary.Distance && unit.Id < primary.Id))))
                    primary = unit;
            }
            if (primary != null)
            {
                if ((range.Next.SplashRadius > range.Current.SplashRadius || range.Next.JumpRange > range.Current.JumpRange)
                    && SecondaryHitCount(simulation, primary, range.Next) > SecondaryHitCount(simulation, primary, range.Current))
                    return range;
                return damage;
            }
            if (!ReferenceEquals(scoredContent, content)) ScorePlacements(content);
            long currentCoverage = CoverageScore(content, range.Current.Range, tower.Position);
            long nextCoverage = CoverageScore(content, range.Next.Range, tower.Position);
            // During quiet periods, buy reach only when it adds meaningful route exposure.
            // No enemies on the other field and no unaffordable quote can bias this choice.
            return nextCoverage > currentCoverage && (nextCoverage - currentCoverage) * 4 >= currentCoverage ? range : damage;
        }

        private int SecondaryHitCount(MatchSimulation simulation, UnitState primary, TowerStats stats)
        {
            var map = simulation.Content.Map;
            if (stats.SplashRadius > 0)
            {
                int count = 0; var impact = map.PositionAt(primary.Distance);
                foreach (var unit in simulation.State.Units)
                    if (unit.Defender == slot && unit.Hp > 0
                        && DistanceSquared(impact, map.PositionAt(unit.Distance)) <= (long)stats.SplashRadius * stats.SplashRadius) count++;
                return count;
            }
            // Follow actual nearest-neighbour chain hops, including its finite hit count and stable tie-break.
            var hit = new List<UnitState> { primary };
            var current = primary;
            while (hit.Count < stats.MaxTargets)
            {
                UnitState next = null; long nearest = long.MaxValue;
                var from = map.PositionAt(current.Distance);
                foreach (var unit in simulation.State.Units)
                {
                    if (unit.Defender != slot || unit.Hp <= 0 || hit.Contains(unit)) continue;
                    long distance = DistanceSquared(from, map.PositionAt(unit.Distance));
                    if (distance > (long)stats.JumpRange * stats.JumpRange) continue;
                    if (distance < nearest || (distance == nearest && (next == null || unit.Id < next.Id)))
                    { nearest = distance; next = unit; }
                }
                if (next == null) break;
                hit.Add(next); current = next;
            }
            return hit.Count;
        }

        private static long DistanceSquared(MapPoint a, MapPoint b)
        { long x = (long)a.X - b.X, y = (long)a.Y - b.Y; return x*x + y*y; }

        private bool HasInvestmentBudget(MatchSimulation simulation, BigInteger price, bool reserveAttack)
        {
            int reserve = 0;
            if (reserveAttack)
            {
                reserve = int.MaxValue;
                foreach (UnitDefinition unit in simulation.Content.UnitData) reserve = Math.Min(reserve, unit.Price);
            }
            return simulation.State.Players[(int)slot].Gold >= price + reserve;
        }

        private bool TryAttack(MatchSimulation simulation, bool economical = false)
        {
            int preferred = strategy == BotStrategy.RunnerRush ? 0 : strategy == BotStrategy.TankRush ? 2
                : researchedPacketKind >= 0 ? researchedPacketKind : (int)(Next() % 4);
            bool specialized = strategy == BotStrategy.RunnerRush || strategy == BotStrategy.TankRush;
            if (economical && !specialized && researchedPacketKind < 0)
                for (int i = 0; i < simulation.Content.UnitData.Length; i++)
                    if (simulation.Content.UnitData[i].Price < simulation.Content.UnitData[preferred].Price) preferred = i;
            for (int offset = 0; offset < (specialized ? 1 : 4); offset++)
            {
                int kind = (preferred + offset) % 4;
                SendQuote quote = SendRules.Quote(simulation.Content, simulation.State, slot, kind);
                int horizon = difficulty == BotDifficulty.Easy ? 0 : 1;
                if (!quote.IsValid || quote.WaveId > simulation.State.WaveId + horizon
                    || quote.Price > simulation.State.Players[(int)slot].Gold) continue;
                if (!economical && difficulty == BotDifficulty.Hard && !specialized)
                {
                    // Faster decisions must not turn hard mode into an indiscriminate
                    // queue spender. Keep a real defensive/research reserve between attacks.
                    BigInteger reserve = simulation.Content.TowerData[0].Price;
                    if (simulation.State.WaveId >= 2)
                    {
                        int researchKind = (int)((simulation.State.WaveId - 2) % 4);
                        ResearchQuote research = UnitResearchRules.Quote(simulation.Content, simulation.State.Players[(int)slot], researchKind);
                        if (research.IsValid && research.Level < BigInteger.Min(8, simulation.State.WaveId / 2 + 1)) reserve += research.Price;
                    }
                    if (simulation.State.Players[(int)slot].Gold < quote.Price + reserve) continue;
                }
                CommandResult result = simulation.Execute(slot, new CommandEnvelope { SessionId = simulation.State.SessionId,
                    Sequence = simulation.LastSequence(slot) + 1, Type = CommandType.SendUnit, Kind = kind, Target = -1,
                    ExpectedWave = quote.WaveId, ExpectedResearch = quote.ResearchLevel, EnemyTarget=quote.EnemyTarget });
                if (!result.Accepted) continue;
                purchases++;
                if (kind == researchedPacketKind) researchedPacketKind = -1;
                lastAttackTick = simulation.State.PlayTicks;
                return true;
            }
            return false;
        }

        private CommandResult Send(MatchSimulation simulation, CommandType type, int kind, BigInteger target)
            => simulation.Execute(slot, new CommandEnvelope { SessionId = simulation.State.SessionId,
                Sequence = simulation.LastSequence(slot) + 1, Type = type, Kind = kind, Target = type==CommandType.SetWaveReady?-1:checked((int)target), ExpectedWave=type==CommandType.SetWaveReady?target:BigInteger.Zero });
        private CommandResult Build(MatchSimulation simulation, int kind, int site, MapPoint position)
            => simulation.Execute(slot, new CommandEnvelope { SessionId = simulation.State.SessionId,
                Sequence = simulation.LastSequence(slot) + 1, Type = CommandType.BuildTower, Kind = kind,
                Target = site, PositionX = position.X, PositionY = position.Y });

        private bool FindBuildPosition(MatchSimulation simulation, int kind, out int site, out MapPoint position)
        {
            GameContent content = simulation.Content;
            MatchState state = simulation.State;
            if (!ReferenceEquals(scoredContent, content)) ScorePlacements(content);
            bool economy = content.TowerData[kind].IsEconomy;
            bool freePlacement = content.Map.FreePlacement;
            MapPoint[] candidates = freePlacement ? buildCandidates : content.Map.SlotData;
            int best = -1;
            long bestScore = long.MinValue;
            var alternatives = new List<int>();
            for (int candidate = 0; candidate < candidates.Length; candidate++)
            {
                if (!freePlacement && !content.Map.CanBuildAt(candidate, economy)) continue;
                // Prefer land of low combat value for mines; no ore-site list or reservation.
                long score = economy ? -placementScores[0, candidate] : placementScores[kind, candidate];
                if (!economy && score <= 0) continue;
                // Geometry only: unaffordable buildings must remain valid savings targets.
                if (PlacementValidator.ValidatePosition(content, state, slot, kind,
                    freePlacement ? -1 : candidate, candidates[candidate]) != ErrorCode.None) continue;
                alternatives.Add(candidate);
                if (score > bestScore) { bestScore = score; best = candidate; }
            }
            // Easier opponents still choose sensible legal ground. No stat/gold handicap,
            // no intentionally useless placement: choose among near-best alternatives.
            if (!economy && best >= 0 && difficulty != BotDifficulty.Hard)
            {
                alternatives.Sort((a,b) => placementScores[kind,b] != placementScores[kind,a]
                    ? placementScores[kind,b].CompareTo(placementScores[kind,a]) : a.CompareTo(b));
                int count = 0, limit = difficulty == BotDifficulty.Easy ? 3 : 2;
                while (count < alternatives.Count && count < limit && placementScores[kind,alternatives[count]] * 5 >= bestScore * 4) count++;
                best = alternatives[(int)(Next() % (uint)count)];
            }
            site = freePlacement ? -1 : best;
            position = best >= 0 ? candidates[best] : default;
            return best >= 0;
        }

        private void ScorePlacements(GameContent content)
        {
            scoredContent = content;
            pathSamples = new MapPoint[content.Map.Length / 20 + 1];
            for (int i = 0; i < pathSamples.Length; i++) pathSamples[i] = content.Map.PositionAt(i * 20);
            if (content.Map.FreePlacement)
            {
                // This is a private bot search grid, never the set of legal human positions.
                var candidates = new List<MapPoint>();
                for (int y = PlacementValidator.BuildingRadius; y <= content.Map.Height - PlacementValidator.BuildingRadius; y += 40)
                for (int x = PlacementValidator.BuildingRadius; x <= content.Map.Width - PlacementValidator.BuildingRadius; x += 40)
                    candidates.Add(new MapPoint(x, y));
                buildCandidates = candidates.ToArray();
            }
            else buildCandidates = content.Map.SlotData;
            placementScores = new long[content.TowerData.Length, buildCandidates.Length];
            for (int kind = 0; kind < content.TowerData.Length; kind++)
            for (int s = 0; s < buildCandidates.Length; s++)
                placementScores[kind, s] = CoverageScore(content, content.TowerData[kind].Range, buildCandidates[s]);
        }

        private long CoverageScore(GameContent content, int range, MapPoint position)
        {
            long score = 0;
            long rangeSquared = (long)range * range;
            for (int i = 0; i < pathSamples.Length; i++)
            {
                MapPoint path = pathSamples[i];
                long dx = (long)position.X - path.X, dy = (long)position.Y - path.Y;
                if (dx * dx + dy * dy <= rangeSquared) score += 100000L + content.Map.Length - i * 20;
            }
            return score;
        }
        private uint Next() { random ^= random << 13; random ^= random >> 17; random ^= random << 5; return random; }
    }
}
