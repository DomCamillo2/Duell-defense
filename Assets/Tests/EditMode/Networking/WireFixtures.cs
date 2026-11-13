using System;
using System.Numerics;
using DefenseDuell.Core;

namespace DefenseDuell.Networking.Tests
{
    public static class WireFixtures
    {
        public static MatchSimulation Playing(GameContent content, bool active = true, int zeroMask = 31, int oneMask = 31)
        {
            var sim = new MatchSimulation(content, 7, 1234);
            var allowed = (1 << content.Towers.Length) - 1;
            sim.ConfigureTowerAvailability(PlayerSlot.Zero, zeroMask & allowed);
            sim.ConfigureTowerAvailability(PlayerSlot.One, oneMask & allowed);
            Execute(sim, PlayerSlot.Zero, CommandType.SetReady);
            Execute(sim, PlayerSlot.One, CommandType.SetReady);
            for (var i = 0; i < content.CountdownTicks; i++) sim.Step();
            if (active)
            {
                Execute(sim, PlayerSlot.Zero, CommandType.SetWaveReady, 1);
                Execute(sim, PlayerSlot.One, CommandType.SetWaveReady, 1);
                sim.Step();
            }
            return sim;
        }

        public static CommandResult Execute(MatchSimulation sim, PlayerSlot slot, CommandType type, int kind = 0, int target = -1)
        {
            var command = new CommandEnvelope { SessionId = sim.State.SessionId, Sequence = sim.LastSequence(slot) + 1,
                Type = type, Kind = kind, Target = target };
            if(type==CommandType.SetWaveReady){command.ExpectedWave=sim.State.WaveId;command.Target=-1;}
            if(type==CommandType.SetReady){command.ExpectedConfigRevision=sim.State.ConfigRevision;command.ExpectedRosterRevision=sim.State.RosterRevision;}
            if (type == CommandType.BuildTower && sim.Content.Map.FreePlacement)
            {
                var point = FreePosition(sim.Content, sim.State, slot, kind);
                command.Target = -1; command.PositionX = point.X; command.PositionY = point.Y;
            }
            if (type == CommandType.SendUnit)
            {
                var quote = SendRules.Quote(sim.Content, sim.State, slot, kind);
                command.EnemyTarget=quote.EnemyTarget; command.ExpectedWave = quote.WaveId; command.ExpectedResearch = quote.ResearchLevel;
            }
            if (type == CommandType.ResearchUnit) { command.Target = -1; command.ExpectedResearch = sim.State.Players[(int)slot].ResearchLevels[kind]; }
            return sim.Execute(slot, command);
        }

        // A legal schema-bound fixture uses a larger validated test budget and neutral plan.
        // Production prices/budgets are never changed by the runtime test driver.
        public static MatchState Maximum(out GameContent content)
        {
            var defaults = GameContent.CreateDefault();
            var waves = new WaveDefinition[10];
            for (var w = 0; w < waves.Length; w++) waves[w] = new WaveDefinition(new[] { 64, 0, 0, 0 }, 100);
            content = new GameContent(defaults.Rules, defaults.Towers, defaults.Units, defaults.Map,
                waves, defaults.Research, new WaveRules(waveBudget: 10000));
            var state = Playing(content).State; state.Tick = 2000; state.PlayTicks = 1000;
            var id = 1; FillTowers(content, state, ref id);
            for (var p = 0; p < 2; p++)
            {
                state.NeutralCursor[p] = 64;
                for (var i = 0; i < 80; i++) state.Units.Add(new UnitState { Id = id++, Defender = (PlayerSlot)p,
                    Sender=(PlayerSlot)(i<64?p:1-p), Kind = 0, Hp = 1, Origin = i < 64 ? UnitOrigin.NeutralWave : UnitOrigin.PaidPvP,
                    WaveId = 1, NeutralRecipeIndex=i<64?i:-1, Distance = i * 3, SlowPermille = 650, MovementRemainder = 123 });
                // Sixteen fully spawned paid packets followed by eighty intact pending packets.
                state.Players[p].ReservedGoldByWave[0] = 16 * content.Units[0].Price;
                state.Players[p].SpawnedQuarterGoldByWave[0] = 4 * state.Players[p].ReservedGoldByWave[0];
                state.Players[p].AttackIncomeUnits = (int)BigInteger.Min(2880, state.Players[p].SpawnedQuarterGoldByWave[0]);
                for (var q = 0; q < 80; q++) Queue(content, state, (PlayerSlot)p, 0, 1);
            }
            return state;
        }

        public static SpawnOrder Queue(GameContent content, MatchState state, PlayerSlot sender, int kind, int wave)
        {
            var order = new SpawnOrder { Sender = sender, EnemyTarget=state.Players[(int)sender].AttackTarget, Kind = kind, WaveId = wave, Origin = UnitOrigin.PaidPvP,
                ResearchLevel = state.Players[(int)sender].ResearchLevels[kind], PaidPrice = content.Units[kind].Price,
                InitialCount = content.Units[kind].Count, Remaining = content.Units[kind].Count, NextTick = state.PlayTicks };
            state.SpawnOrders.Add(order); state.Players[(int)sender].ReservedGoldByWave[wave - 1] += order.PaidPrice;
            return order;
        }

        public static BigInteger Investment(GameContent content, int kind, BigInteger level)
            => TowerUpgradeRules.TotalInvestment(content, kind, level);

        public static MapPoint FreeCombatPosition(GameContent content, MatchState state, PlayerSlot owner)
            => FreePosition(content, state, owner, 0);

        public static MapPoint FreePosition(GameContent content, MatchState state, PlayerSlot owner, int kind)
        {
            var map = content.Map;
            for (var y = 31; y < map.Height - 30; y += 61)
            for (var x = 31; x < map.Width - 30; x += 61)
            {
                var position = new MapPoint(x, y);
                if (PlacementValidator.ValidatePosition(content, state, owner, kind, -1, position) == ErrorCode.None) return position;
            }
            throw new InvalidOperationException("No legal free combat position remains in the test fixture.");
        }

        public static void FillTowers(GameContent content, MatchState state, ref int id)
        {
            for (var p = 0; p < 2; p++)
            {
                var owner = (PlayerSlot)p; var render = 0;
                if (content.Map.FreePlacement)
                {
                    for (var mine = 0; mine < PlacementValidator.MineCapacity; mine++)
                        state.Towers.Add(new TowerState { Id = id++, Owner = owner, Slot = render++, Kind = 4, SiteId = -1,
                            Position = FreePosition(content, state, owner, 4), Level = 2, Invested = Investment(content, 4, 2) });
                    for (var i = 0; i < PlacementValidator.CombatCapacity; i++)
                        state.Towers.Add(new TowerState { Id = id++, Owner = owner, Slot = render++, Kind = 0, SiteId = -1,
                            Position = FreeCombatPosition(content, state, owner), Level = 2, DamageLevel = 2 - i % 3, RangeLevel = i % 3, Invested = Investment(content, 0, 2) });
                }
                else
                    for (var site = 0; site < content.Map.BuildSlots.Length; site++)
                        state.Towers.Add(new TowerState { Id = id++, Owner = owner, Slot = site, Kind = content.Map.IsMineSlot(site) ? 4 : site % 4,
                            SiteId = site, Position = content.Map.BuildSlots[site], Level = 2,
                            DamageLevel = content.Map.IsMineSlot(site) ? 0 : 2, Invested = Investment(content, content.Map.IsMineSlot(site) ? 4 : site % 4, 2) });
            }
        }
    }
}
