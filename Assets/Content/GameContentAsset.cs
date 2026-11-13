using System;
using DefenseDuell.Core;
using UnityEngine;

namespace DefenseDuell.Content
{
    [CreateAssetMenu(menuName = "Defense Duell/Validated content")]
    public sealed class GameContentAsset : ScriptableObject
    {
        [Serializable] sealed class UpgradeData
        {
            public int Price, Damage, Range, FireIntervalTicks, IncomePerSecond, IncomeUnitsPerSecond;
            public UpgradeDefinition ToDefinition() => new UpgradeDefinition(Price, Damage, Range, FireIntervalTicks, IncomePerSecond, IncomeUnitsPerSecond);
            public UpgradeData(UpgradeDefinition d) { Price = d.Price; Damage = d.Damage; Range = d.Range; FireIntervalTicks = d.FireIntervalTicks; IncomePerSecond = d.IncomePerSecond; IncomeUnitsPerSecond = d.IncomeUnitsPerSecond; }
        }
        [Serializable] sealed class TowerData
        {
            public string Name;
            public int Price, Range, Damage, FireIntervalTicks, SplashRadius, SlowPermille, SlowDurationTicks, ArmorPiercing, IncomePerSecond, MaxPerPlayer, IncomeUnitsPerSecond;
            public UpgradeData[] Upgrades;
            public WeaponMechanic Mechanic;
            public int MaxTargets, JumpRange, LineHalfWidth;
            public int[] DamagePermilleByHit;
            public TowerDefinition ToDefinition()
            {
                var upgrades = new UpgradeDefinition[Upgrades.Length];
                for (int i = 0; i < upgrades.Length; i++) upgrades[i] = Upgrades[i].ToDefinition();
                return new TowerDefinition(Name, Price, Range, Damage, FireIntervalTicks, SplashRadius, SlowPermille, SlowDurationTicks, ArmorPiercing, upgrades, IncomePerSecond, MaxPerPlayer, IncomeUnitsPerSecond, Mechanic, MaxTargets, JumpRange, LineHalfWidth, DamagePermilleByHit);
            }
            public TowerData(TowerDefinition d)
            {
                Name = d.Name; Price = d.Price; Range = d.Range; Damage = d.Damage; FireIntervalTicks = d.FireIntervalTicks;
                SplashRadius = d.SplashRadius; SlowPermille = d.SlowPermille; SlowDurationTicks = d.SlowDurationTicks; ArmorPiercing = d.ArmorPiercing;
                IncomePerSecond = d.IncomePerSecond; MaxPerPlayer = d.MaxPerPlayer; IncomeUnitsPerSecond = d.IncomeUnitsPerSecond;
                Mechanic = d.Mechanic; MaxTargets = d.MaxTargets; JumpRange = d.JumpRange; LineHalfWidth = d.LineHalfWidth; DamagePermilleByHit = d.DamagePermilleByHit;
                var source = d.Upgrades; Upgrades = new UpgradeData[source.Length];
                for (int i = 0; i < source.Length; i++) Upgrades[i] = new UpgradeData(source[i]);
            }
        }
        [Serializable] sealed class UnitData
        {
            public string Name;
            public int Price, Hp, SpeedPerSecond, Armor, BaseDamage, Count, CooldownTicks, KillBounty;
            public UnitDefinition ToDefinition() => new UnitDefinition(Name, Price, Hp, SpeedPerSecond, Armor, BaseDamage, Count, CooldownTicks, KillBounty);
            public UnitData(UnitDefinition d) { Name = d.Name; Price = d.Price; Hp = d.Hp; SpeedPerSecond = d.SpeedPerSecond; Armor = d.Armor; BaseDamage = d.BaseDamage; Count = d.Count; CooldownTicks = d.CooldownTicks; KillBounty = d.KillBounty; }
        }
        [Serializable] sealed class RulesData
        {
            public int TickRate, StartGold, BaseHp, PassiveGoldPerSecond, MaxUnitsPerField, MaxSpawnOrdersPerPlayer;
            public int MatchDurationTicks, CountdownTicks, SellRefundPercent, SpawnIntervalTicks, MinimumSlowPermille, MaxCommandsPerSecond, MaxGold, MineSellRefundPercent;
            public GameRules ToRules() => new GameRules(TickRate, StartGold, BaseHp, PassiveGoldPerSecond, MaxUnitsPerField, MaxSpawnOrdersPerPlayer,
                MatchDurationTicks, CountdownTicks, SellRefundPercent, SpawnIntervalTicks, MinimumSlowPermille, MaxCommandsPerSecond, MaxGold, MineSellRefundPercent);
            public RulesData(GameRules d)
            {
                TickRate = d.TickRate; StartGold = d.StartGold; BaseHp = d.BaseHp; PassiveGoldPerSecond = d.PassiveGoldPerSecond;
                MaxUnitsPerField = d.MaxUnitsPerField; MaxSpawnOrdersPerPlayer = d.MaxSpawnOrdersPerPlayer; MatchDurationTicks = d.MatchDurationTicks;
                CountdownTicks = d.CountdownTicks; SellRefundPercent = d.SellRefundPercent; SpawnIntervalTicks = d.SpawnIntervalTicks;
                MinimumSlowPermille = d.MinimumSlowPermille; MaxCommandsPerSecond = d.MaxCommandsPerSecond; MaxGold = d.MaxGold; MineSellRefundPercent = d.MineSellRefundPercent;
            }
        }

        [Serializable] sealed class WaveData
        {
            public int[] Counts;
            public int HpPercent;
            public WaveData(WaveDefinition d) { Counts = d.Counts; HpPercent = d.HpPercent; }
            public WaveDefinition ToDefinition() => new WaveDefinition(Counts, HpPercent);
        }
        [Serializable] sealed class ResearchData
        {
            public int HpPerLevel, SpeedPerLevel, BasePrice, PriceStep, QuadraticPrice, MaxLevel, HpQuadraticNumerator, HpQuadraticDenominator;
            public ResearchData(UnitResearchDefinition d)
            { HpPerLevel=d.HpPerLevel; SpeedPerLevel=d.SpeedPerLevel; BasePrice=d.BasePrice; PriceStep=d.PriceStep; QuadraticPrice=d.QuadraticPrice; MaxLevel=d.MaxLevel; HpQuadraticNumerator=d.HpQuadraticNumerator; HpQuadraticDenominator=d.HpQuadraticDenominator; }
            public UnitResearchDefinition ToDefinition() => new UnitResearchDefinition(HpPerLevel,SpeedPerLevel,BasePrice,PriceStep,QuadraticPrice,MaxLevel,HpQuadraticNumerator,HpQuadraticDenominator);
        }
        [Serializable] sealed class WaveRulesData
        {
            public int PreparationTicks, WaveBudget, NeutralSpawnIntervalTicks, MinimumSendWindowTicks, SendWindowTailTicks;
            public int MaxUnitsPerOrigin, IncomeUnitsPerGold, AttackIncomeDivisor, MaxAttackIncomeUnits;
            public WaveRulesData(WaveRules d)
            {
                PreparationTicks=d.PreparationTicks; WaveBudget=d.WaveBudget; NeutralSpawnIntervalTicks=d.NeutralSpawnIntervalTicks;
                MinimumSendWindowTicks=d.MinimumSendWindowTicks; SendWindowTailTicks=d.SendWindowTailTicks;
                MaxUnitsPerOrigin=d.MaxUnitsPerOrigin; IncomeUnitsPerGold=d.IncomeUnitsPerGold;
                AttackIncomeDivisor=d.AttackIncomeDivisor; MaxAttackIncomeUnits=d.MaxAttackIncomeUnits;
            }
            public WaveRules ToDefinition() => new WaveRules(PreparationTicks,WaveBudget,NeutralSpawnIntervalTicks,
                MinimumSendWindowTicks,SendWindowTailTicks,MaxUnitsPerOrigin,IncomeUnitsPerGold,AttackIncomeDivisor,MaxAttackIncomeUnits);
        }

        [Serializable] sealed class BlockedAreaData
        {
            public int MinX, MinY, MaxX, MaxY;
            public BlockedAreaData(MapRectangle area) { MinX = area.MinX; MinY = area.MinY; MaxX = area.MaxX; MaxY = area.MaxY; }
            public MapRectangle ToDefinition() => new MapRectangle(MinX, MinY, MaxX, MaxY);
        }

        [Serializable] sealed class MapData
        {
            public string Id, Name;
            public int Theme, Width, Height;
            public bool FreePlacement;
            public int[] MineSlots;
            public BlockedAreaData[] BlockedAreas;
            public MapPoint[] Waypoints, BuildSlots;
            public MapData(MapDefinition d)
            {
                Id = d.Id; Name = d.Name; Theme = d.Theme; Width = d.Width; Height = d.Height;
                FreePlacement = d.FreePlacement; MineSlots = d.MineSlots; Waypoints = d.Waypoints; BuildSlots = d.BuildSlots;
                var areas = d.BlockedAreas; BlockedAreas = new BlockedAreaData[areas.Length];
                for (int i = 0; i < areas.Length; i++) BlockedAreas[i] = new BlockedAreaData(areas[i]);
            }
            public MapDefinition ToDefinition()
            {
                var areas = new MapRectangle[BlockedAreas?.Length ?? 0];
                for (int i = 0; i < areas.Length; i++) areas[i] = BlockedAreas[i].ToDefinition();
                return new MapDefinition(Waypoints, BuildSlots, Id, Name, Theme, Width, Height, MineSlots, FreePlacement, areas);
            }
        }

        [SerializeField] int schemaVersion;
        [SerializeField] RulesData rules;
        [SerializeField] TowerData[] towers;
        [SerializeField] UnitData[] units;
        [SerializeField] MapData[] maps;
        [SerializeField] WaveData[] waves;
        [SerializeField] ResearchData[] research;
        [SerializeField] WaveRulesData waveRules;

        /// <summary>Produces fresh, immutable definitions. The asset never stores any live match state.</summary>
        public GameContent CreateContent(int mapIndex = 0)
        {
            if (rules == null || towers == null || units == null || maps == null || waves == null || research == null || waveRules == null) throw new InvalidOperationException("Inhaltsdaten fehlen. Defense Duell > Prepare project ausführen.");
            if (mapIndex < 0 || mapIndex >= maps.Length) throw new ArgumentOutOfRangeException(nameof(mapIndex));
            var towerDefinitions = new TowerDefinition[towers.Length];
            var unitDefinitions = new UnitDefinition[units.Length];
            for (int i = 0; i < towers.Length; i++) towerDefinitions[i] = towers[i].ToDefinition();
            for (int i = 0; i < units.Length; i++) unitDefinitions[i] = units[i].ToDefinition();
            var waveDefinitions = new WaveDefinition[waves.Length];
            var researchDefinitions = new UnitResearchDefinition[research.Length];
            for (int i = 0; i < waves.Length; i++) waveDefinitions[i] = waves[i].ToDefinition();
            for (int i = 0; i < research.Length; i++) researchDefinitions[i] = research[i].ToDefinition();
            return new GameContent(rules.ToRules(), towerDefinitions, unitDefinitions, maps[mapIndex].ToDefinition(), waveDefinitions, researchDefinitions, waveRules.ToDefinition());
        }

        public GameContent[] CreateCatalog()
        {
            if (maps == null) throw new InvalidOperationException("Kartenkatalog fehlt.");
            var result = new GameContent[maps.Length];
            for (int i = 0; i < result.Length; i++) result[i] = CreateContent(i);
            return result;
        }

#if UNITY_EDITOR
        // BuildEntry calls this only for the project's built-in Standard.asset. Keep
        // authored balance values while refreshing code-authored map geometry.
        public bool RefreshEditorMapCatalog()
        {
            var next = new MapData[MapCatalog.Count];
            bool changed = maps == null || maps.Length != next.Length;
            for (int i = 0; i < next.Length; i++)
            {
                next[i] = new MapData(MapCatalog.Create(i));
                if (!changed && JsonUtility.ToJson(maps[i]) != JsonUtility.ToJson(next[i])) changed = true;
            }
            if (changed) maps = next;
            return changed;
        }

        public void SetEditorDefaults()
        {
            var source = GameContent.CreateDefault();
            rules = new RulesData(source.Rules);
            var ts = source.Towers; towers = new TowerData[ts.Length];
            var us = source.Units; units = new UnitData[us.Length];
            for (int i = 0; i < ts.Length; i++) towers[i] = new TowerData(ts[i]);
            for (int i = 0; i < us.Length; i++) units[i] = new UnitData(us[i]);
            maps = new MapData[MapCatalog.Count];
            for (int i = 0; i < maps.Length; i++) maps[i] = new MapData(MapCatalog.Create(i));
            var ws = source.Waves; waves = new WaveData[ws.Length];
            var rs = source.Research; research = new ResearchData[rs.Length];
            for (int i = 0; i < ws.Length; i++) waves[i] = new WaveData(ws[i]);
            for (int i = 0; i < rs.Length; i++) research[i] = new ResearchData(rs[i]);
            waveRules = new WaveRulesData(source.WaveRules);
            schemaVersion = 11;
        }

        // Schema11 adds the two paid elite classes and preserves existing authored
        // economy, waves and maps while refreshing the targeted departure values.
        public bool MigratePrototypeInEditor()
        {
            if (schemaVersion >= 11) return false;
            if (schemaVersion < 9) { SetEditorDefaults(); return true; }
            var current = GameContent.CreateDefault();
            rules.SpawnIntervalTicks = current.Rules.SpawnIntervalTicks;
            int oldUnits = units.Length, oldResearch = research.Length;
            Array.Resize(ref units, current.Units.Length);
            Array.Resize(ref research, current.Research.Length);
            for (int i = oldUnits; i < units.Length; i++) units[i] = new UnitData(current.Units[i]);
            for (int i = oldResearch; i < research.Length; i++) research[i] = new ResearchData(current.Research[i]);
            for (int i = 0; i < units.Length; i++) units[i].CooldownTicks = current.Units[i].CooldownTicks;
            towers[7].FireIntervalTicks = current.Towers[7].FireIntervalTicks;
            foreach (var upgrade in towers[7].Upgrades) upgrade.FireIntervalTicks = current.Towers[7].FireIntervalTicks;
            schemaVersion = 11;
            return true;
        }
#endif
    }
}
