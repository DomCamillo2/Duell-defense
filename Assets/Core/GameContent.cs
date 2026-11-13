using System;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace DefenseDuell.Core
{
    public sealed class UpgradeDefinition
    {
        public int Price { get; }
        public int Damage { get; }
        public int Range { get; }
        public int FireIntervalTicks { get; }
        public int IncomePerSecond { get; }
        public int IncomeUnitsPerSecond { get; }
        public UpgradeDefinition(int price, int damage, int range, int fireIntervalTicks, int incomePerSecond = 0, int incomeUnitsPerSecond = -1)
        { Price = price; Damage = damage; Range = range; FireIntervalTicks = fireIntervalTicks; IncomeUnitsPerSecond = incomeUnitsPerSecond == -1 ? checked(incomePerSecond * 960) : incomeUnitsPerSecond; IncomePerSecond = IncomeUnitsPerSecond / 960; }
    }

    public sealed class TowerDefinition
    {
        public string Name { get; }
        public int Price { get; }
        public int Range { get; }
        public int Damage { get; }
        public int FireIntervalTicks { get; }
        public int SplashRadius { get; }
        /// <summary>Speed multiplier, not a stacking percentage. 1000 means normal speed.</summary>
        public int SlowPermille { get; }
        public int SlowDurationTicks { get; }
        public int ArmorPiercing { get; }
        public int IncomePerSecond { get; }
        public int IncomeUnitsPerSecond { get; }
        /// <summary>Legacy serialized content hint; never a runtime purchase limit.</summary>
        public int MaxPerPlayer { get; }
        public bool IsEconomy => IncomeUnitsPerSecond > 0;
        public WeaponMechanic Mechanic { get; }
        public int MaxTargets { get; }
        public int JumpRange { get; }
        public int LineHalfWidth { get; }
        private readonly int[] damagePermilleByHit;
        public int[] DamagePermilleByHit => (int[])damagePermilleByHit.Clone();
        internal int[] DamagePermilleData => damagePermilleByHit;
        private readonly UpgradeDefinition[] upgrades;
        /// <summary>Legacy serialized two-stage reference data. Runtime purchases/stats use TowerUpgradeRules.</summary>
        public UpgradeDefinition[] Upgrades => (UpgradeDefinition[])upgrades.Clone();
        internal UpgradeDefinition[] UpgradeData => upgrades;
        public TowerDefinition(string name, int price, int range, int damage, int fireIntervalTicks,
            int splashRadius, int slowPermille, int slowDurationTicks, int armorPiercing, UpgradeDefinition[] upgrades,
            int incomePerSecond = 0, int maxPerPlayer = 32, int incomeUnitsPerSecond = -1,
            WeaponMechanic mechanic = WeaponMechanic.Standard, int maxTargets = 1, int jumpRange = 0,
            int lineHalfWidth = 0, int[] damagePermilleByHit = null)
        {
            Name = name; Price = price; Range = range; Damage = damage;
            FireIntervalTicks = fireIntervalTicks; SplashRadius = splashRadius;
            SlowPermille = slowPermille; SlowDurationTicks = slowDurationTicks;
            ArmorPiercing = armorPiercing;
            IncomeUnitsPerSecond = incomeUnitsPerSecond == -1 ? checked(incomePerSecond * 960) : incomeUnitsPerSecond;
            IncomePerSecond = IncomeUnitsPerSecond / 960; MaxPerPlayer = maxPerPlayer;
            Mechanic=mechanic; MaxTargets=maxTargets; JumpRange=jumpRange; LineHalfWidth=lineHalfWidth;
            this.damagePermilleByHit=damagePermilleByHit==null ? new[] {1000} : (int[])damagePermilleByHit.Clone();
            this.upgrades = upgrades == null ? Array.Empty<UpgradeDefinition>() : (UpgradeDefinition[])upgrades.Clone();
        }
    }

    public sealed class UnitDefinition
    {
        public string Name { get; }
        public int Price { get; }
        public int Hp { get; }
        public int SpeedPerSecond { get; }
        public int Armor { get; }
        public int BaseDamage { get; }
        public int Count { get; }
        public int CooldownTicks { get; }
        public int KillBounty { get; }
        public UnitDefinition(string name, int price, int hp, int speedPerSecond, int armor,
            int baseDamage, int count, int cooldownTicks, int killBounty = 0)
        {
            Name = name; Price = price; Hp = hp; SpeedPerSecond = speedPerSecond;
            Armor = armor; BaseDamage = baseDamage; Count = count; CooldownTicks = cooldownTicks;
            KillBounty = killBounty;
        }
    }

    public sealed class MapDefinition
    {
        private readonly MapPoint[] waypoints, buildSlots;
        private readonly int[] mineSlots;
        private readonly MapRectangle[] blockedAreas;
        private readonly int[] segmentLengths;
        public MapPoint[] Waypoints => (MapPoint[])waypoints.Clone();
        public MapPoint[] BuildSlots => (MapPoint[])buildSlots.Clone();
        internal MapPoint[] SlotData => buildSlots;
        internal MapPoint[] WaypointData => waypoints;
        internal int[] MineSlotData => mineSlots;
        public int Length { get; }
        public string Id { get; }
        public string Name { get; }
        public int Theme { get; }
        public int Width { get; }
        public int Height { get; }
        public int[] MineSlots => (int[])mineSlots.Clone();
        public bool HasDedicatedMineSlots => mineSlots.Length > 0;
        public bool FreePlacement { get; }
        public MapRectangle[] BlockedAreas => (MapRectangle[])blockedAreas.Clone();
        internal MapRectangle[] BlockerData => blockedAreas;
        public MapDefinition(MapPoint[] waypoints, MapPoint[] buildSlots,
            string id = "meadow", string name = "Grünes Tal", int theme = 0, int width = 1000, int height = 600,
            int[] mineSlots = null, bool freePlacement = false, MapRectangle[] blockedAreas = null)
        {
            if (waypoints == null || waypoints.Length < 2) throw new ArgumentException("At least two waypoints are required.");
            if (buildSlots == null) throw new ArgumentNullException(nameof(buildSlots));
            if (string.IsNullOrEmpty(id) || id.Length > 32) throw new ArgumentException("Map ID must contain 1–32 lowercase ASCII identifier characters.", nameof(id));
            for (int i = 0; i < id.Length; i++)
                if (!((id[i] >= 'a' && id[i] <= 'z') || (id[i] >= '0' && id[i] <= '9') || id[i] == '-' || id[i] == '_'))
                    throw new ArgumentException("Map ID must contain lowercase ASCII letters, digits, hyphens or underscores.", nameof(id));
            if (string.IsNullOrWhiteSpace(name) || name.Length > 48 || name.IndexOf('\n') >= 0 || name.IndexOf('\r') >= 0)
                throw new ArgumentException("Map name must be a single line of 1–48 characters.", nameof(name));
            if (theme < 0 || theme > 2) throw new ArgumentOutOfRangeException(nameof(theme), "Map theme must be 0, 1 or 2.");
            if (width < 100 || width > 10000 || height < 100 || height > 10000)
                throw new ArgumentOutOfRangeException(nameof(width), "Map dimensions must be between 100 and 10000 logical units.");
            Id = id; Name = name; Theme = theme; Width = width; Height = height;
            FreePlacement = freePlacement;
            this.waypoints = (MapPoint[])waypoints.Clone();
            this.buildSlots = (MapPoint[])buildSlots.Clone();
            this.mineSlots = mineSlots == null ? Array.Empty<int>() : (int[])mineSlots.Clone();
            if (this.mineSlots.Length != 0 && this.mineSlots.Length != 2)
                throw new ArgumentException("A map has either mixed slots or exactly two dedicated mine slots.", nameof(mineSlots));
            Array.Sort(this.mineSlots);
            for (int i = 0; i < this.mineSlots.Length; i++)
                if (this.mineSlots[i] < 0 || this.mineSlots[i] >= buildSlots.Length
                    || (i > 0 && this.mineSlots[i] == this.mineSlots[i - 1]))
                    throw new ArgumentException("Mine slots must be distinct valid build-slot indices.", nameof(mineSlots));
            if (freePlacement && this.mineSlots.Length != 0)
                throw new ArgumentException("Free-placement maps cannot reserve fixed mine sites.", nameof(mineSlots));
            this.blockedAreas = blockedAreas == null ? Array.Empty<MapRectangle>() : (MapRectangle[])blockedAreas.Clone();
            if (this.blockedAreas.Length > 64) throw new ArgumentException("A map supports at most 64 blocked areas.", nameof(blockedAreas));
            foreach (MapRectangle area in this.blockedAreas)
                if (area.MinX < 0 || area.MinY < 0 || area.MaxX > width || area.MaxY > height
                    || area.MaxX <= area.MinX || area.MaxY <= area.MinY)
                    throw new ArgumentException("Blocked areas must be positive rectangles inside the map.", nameof(blockedAreas));
            segmentLengths = new int[waypoints.Length - 1];
            for (int i = 0; i < segmentLengths.Length; i++)
            {
                long dx = (long)waypoints[i + 1].X - waypoints[i].X;
                long dy = (long)waypoints[i + 1].Y - waypoints[i].Y;
                if (Math.Abs(dx) > 10000 || Math.Abs(dy) > 10000) throw new ArgumentException("Map coordinates are out of range.");
                int length = (int)Math.Round(Math.Sqrt(dx * dx + dy * dy), MidpointRounding.AwayFromZero);
                if (length < 1) throw new ArgumentException("A map cannot contain a zero-length segment.");
                segmentLengths[i] = length;
                Length += length;
            }
        }
        public bool IsMineSlot(int slot) => slot >= 0 && slot < buildSlots.Length && Array.IndexOf(mineSlots, slot) >= 0;
        public bool CanBuildAt(int slot, bool economy)
            => slot >= 0 && slot < buildSlots.Length && (!HasDedicatedMineSlots || IsMineSlot(slot) == economy);
        public MapPoint PositionAt(int distance)
        {
            if (distance <= 0) return waypoints[0];
            if (distance >= Length) return waypoints[waypoints.Length - 1];
            for (int i = 0; i < segmentLengths.Length; i++)
            {
                int length = segmentLengths[i];
                if (distance <= length)
                    return new MapPoint(waypoints[i].X + (waypoints[i + 1].X - waypoints[i].X) * distance / length,
                        waypoints[i].Y + (waypoints[i + 1].Y - waypoints[i].Y) * distance / length);
                distance -= length;
            }
            return waypoints[waypoints.Length - 1];
        }
    }

    /// <summary>Immutable balance/rule settings. All values participate in ContentHash.</summary>
    public sealed class GameRules
    {
        public int TickRate { get; }
        public int StartGold { get; }
        public int BaseHp { get; }
        public int PassiveGoldPerSecond { get; }
        /// <summary>Legacy serialized initial pool hint, not a live entity limit.</summary>
        public int MaxUnitsPerField { get; }
        /// <summary>Legacy serialized initial queue hint, not an order limit.</summary>
        public int MaxSpawnOrdersPerPlayer { get; }
        public int MatchDurationTicks { get; }
        public int CountdownTicks { get; }
        public int SellRefundPercent { get; }
        public int MineSellRefundPercent { get; }
        public int SpawnIntervalTicks { get; }
        public int MinimumSlowPermille { get; }
        public int MaxCommandsPerSecond { get; }
        /// <summary>Legacy base-content validation bound; wallets and earned money are unbounded BigInteger.</summary>
        public int MaxGold { get; }
        public GameRules(int tickRate = 20, int startGold = 180, int baseHp = 30,
            int passiveGoldPerSecond = 4, int maxUnitsPerField = 80, int maxSpawnOrdersPerPlayer = 80,
            int matchDurationTicks = 9600, int countdownTicks = 60, int sellRefundPercent = 65,
            int spawnIntervalTicks = 2, int minimumSlowPermille = 600, int maxCommandsPerSecond = 40,
            int maxGold = 10000, int mineSellRefundPercent = 40)
        {
            TickRate = tickRate; StartGold = startGold; BaseHp = baseHp;
            PassiveGoldPerSecond = passiveGoldPerSecond; MaxUnitsPerField = maxUnitsPerField;
            MaxSpawnOrdersPerPlayer = maxSpawnOrdersPerPlayer; MatchDurationTicks = matchDurationTicks;
            CountdownTicks = countdownTicks; SellRefundPercent = sellRefundPercent;
            SpawnIntervalTicks = spawnIntervalTicks; MinimumSlowPermille = minimumSlowPermille;
            MaxCommandsPerSecond = maxCommandsPerSecond; MaxGold = maxGold; MineSellRefundPercent = mineSellRefundPercent;
        }
    }

    public sealed class GameContent
    {
        // Change this whenever arithmetic, targeting, tick ordering or command semantics change.
        public const string SimulationRulesVersion = "defense-duell-core-v2.5";
        public const int UnitClassCount = 6;
        public const int NeutralClassCount = 4;
        private readonly object waveRecipeGate=new object();
        private readonly System.Collections.Generic.Dictionary<BigInteger,WaveRecipe> recipeCache=new System.Collections.Generic.Dictionary<BigInteger,WaveRecipe>();
        private readonly System.Collections.Generic.Queue<BigInteger> recipeOrder=new System.Collections.Generic.Queue<BigInteger>();
        public int CachedWaveRecipeCount { get { lock(waveRecipeGate)return recipeCache.Count; } }
        private readonly GameContent baseContent;
        private readonly TowerDefinition[] towers;
        private readonly UnitDefinition[] units;
        private readonly WaveDefinition[] waves;
        private readonly UnitResearchDefinition[] research;
        public WaveDefinition[] Waves => (WaveDefinition[])waves.Clone();
        public UnitResearchDefinition[] Research => (UnitResearchDefinition[])research.Clone();
        internal WaveDefinition[] WaveData => waves;
        internal UnitResearchDefinition[] ResearchData => research;
        public WaveRules WaveRules { get; }
        public int WaveCount => MatchRules.Endless ? 0 : waves.Length;
        public TowerDefinition[] Towers => (TowerDefinition[])towers.Clone();
        public UnitDefinition[] Units => (UnitDefinition[])units.Clone();
        internal TowerDefinition[] TowerData => towers;
        internal UnitDefinition[] UnitData => units;
        public MapDefinition Map { get; }
        public GameRules Rules { get; }
        public string ContentHash { get; }
        public GameContent BaseContent => baseContent ?? this;
        public string BaseContentHash => BaseContent.ContentHash;
        public MatchRules MatchRules { get; }
        public BigInteger MatchStartGold => MatchRules.StartGold;
        public int TickRate => Rules.TickRate;
        /// <summary>Serialized base catalog value only. Runtime payouts use MatchStartGold.</summary>
        public int StartGold => Rules.StartGold;
        public int BaseHp => Rules.BaseHp;
        public int PassiveGoldPerSecond => Rules.PassiveGoldPerSecond;
        public int MaxUnitsPerField => Rules.MaxUnitsPerField;
        public int MaxSpawnOrdersPerPlayer => Rules.MaxSpawnOrdersPerPlayer;
        public int MatchDurationTicks => Rules.MatchDurationTicks;
        public int CountdownTicks => Rules.CountdownTicks;
        public int SellRefundPercent => Rules.SellRefundPercent;

        public GameContent(GameRules rules, TowerDefinition[] towers, UnitDefinition[] units, MapDefinition map,
            WaveDefinition[] waves = null, UnitResearchDefinition[] research = null, WaveRules waveRules = null)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            this.towers = towers == null ? throw new ArgumentNullException(nameof(towers)) : (TowerDefinition[])towers.Clone();
            this.units = units == null ? throw new ArgumentNullException(nameof(units)) : (UnitDefinition[])units.Clone();
            Map = map ?? throw new ArgumentNullException(nameof(map));
            this.waves = waves == null ? WaveDefinition.CreateDefault() : (WaveDefinition[])waves.Clone();
            this.research = research == null ? UnitResearchDefinition.CreateDefault() : (UnitResearchDefinition[])research.Clone();
            WaveRules = waveRules ?? new WaveRules();
            MatchRules = new MatchRules(this.waves.Length, Rules.StartGold, mapId:Map.Id);
            Validate();
            ContentHash = ComputeHash();
        }

        private GameContent(GameContent source, MatchRules matchRules)
        {
            baseContent = source.BaseContent;
            Rules = baseContent.Rules; Map = baseContent.Map;
            towers = baseContent.towers; units = baseContent.units; research = baseContent.research;
            WaveRules = baseContent.WaveRules; MatchRules = matchRules;
            waves = new WaveDefinition[matchRules.Endless ? baseContent.waves.Length : matchRules.WaveCount];
            int authoredCount = Math.Min(baseContent.waves.Length, waves.Length);
            Array.Copy(baseContent.waves, waves, authoredCount);
            var lastAuthored = baseContent.waves[baseContent.waves.Length - 1];
            for (int w = authoredCount; w < waves.Length; w++)
                waves[w] = new WaveDefinition(lastAuthored.Counts,
                    checked(lastAuthored.HpPercent + WaveDefinition.ContinuationHpStep * (w - baseContent.waves.Length + 1)));
            Validate(); ContentHash = ComputeHash();
        }

        public GameContent WithMatchRules(MatchRules matchRules)
        {
            ErrorCode error = MatchRulesPolicy.Validate(BaseContent, matchRules);
            if (error != ErrorCode.None) throw new ArgumentException("Invalid match rules: " + error, nameof(matchRules));
            if (string.IsNullOrEmpty(matchRules.MapId)) matchRules=matchRules.WithMap(Map.Id);
            if (MatchRules.Equals(matchRules)) return this;
            GameContent basis=BaseContent;
            if(matchRules.MapId!=basis.Map.Id)
            {
                MapDefinition selected=null;
                for(int i=0;i<MapCatalog.Count;i++){var candidate=MapCatalog.Create(i);if(candidate.Id==matchRules.MapId){selected=candidate;break;}}
                if(selected==null)throw new ArgumentException("Unknown authored map",nameof(matchRules));
                basis=new GameContent(basis.Rules,basis.towers,basis.units,selected,basis.waves,basis.research,basis.WaveRules);
            }
            if (basis.MatchRules.Equals(matchRules)) return basis;
            return new GameContent(basis, matchRules);
        }

        public bool ContainsWave(BigInteger wave)=>wave>=1&&(MatchRules.Endless||wave<=WaveCount);
        public WaveRecipe GetWave(BigInteger wave)
        {
            if(!ContainsWave(wave))throw new ArgumentOutOfRangeException(nameof(wave));
            lock(waveRecipeGate)
            {
                if(recipeCache.TryGetValue(wave,out var cached))return cached;
                var authored=BaseContent.waves;
                WaveRecipe recipe;
                if(wave>=10)recipe=LateGameRules.Create(this,wave);
                else
                {
                    var value=wave<=authored.Length?authored[(int)wave-1]:authored[authored.Length-1];
                    BigInteger hp=wave<=authored.Length?value.HpPercent:value.HpPercent+WaveDefinition.ContinuationHpStep*(wave-authored.Length);
                    var effective=new BigInteger[NeutralClassCount];
                    for(int k=0;k<effective.Length;k++)effective[k]=((BigInteger)units[k].Hp*hp+99)/100;
                    recipe=new WaveRecipe(value,hp,effective);
                }
                if(recipeCache.Count>=3)recipeCache.Remove(recipeOrder.Dequeue());
                recipeCache.Add(wave,recipe);recipeOrder.Enqueue(wave);return recipe;
            }
        }

        public static GameContent CreateDefault(GameRules rules = null)
        {
            return new GameContent(rules ?? new GameRules(), new[]
            {
                new TowerDefinition("Schnellfeuer", 65, 150, 6, 8, 0, 1000, 0, 0,
                    new[] { new UpgradeDefinition(55, 9, 180, 8), new UpgradeDefinition(85, 14, 210, 8) }),
                new TowerDefinition("Kanone", 105, 155, 16, 32, 90, 1000, 0, 0,
                    new[] { new UpgradeDefinition(85, 26, 186, 32), new UpgradeDefinition(130, 40, 217, 32) }),
                new TowerDefinition("Frost", 85, 155, 4, 20, 0, 650, 30, 0,
                    new[] { new UpgradeDefinition(60, 6, 186, 20), new UpgradeDefinition(95, 8, 217, 20) }),
                new TowerDefinition("Panzerbrecher", 120, 190, 40, 42, 0, 1000, 0, 8,
                    new[] { new UpgradeDefinition(85, 64, 228, 42), new UpgradeDefinition(130, 96, 266, 42) }),
                new TowerDefinition("Goldmine", 160, 0, 0, 0, 0, 1000, 0, 0,
                    new[] { new UpgradeDefinition(100, 0, 0, 0, incomeUnitsPerSecond: 1440), new UpgradeDefinition(160, 0, 0, 0, incomeUnitsPerSecond: 1920) },
                    incomePerSecond: 1, maxPerPlayer: 2),
                new TowerDefinition("Blitzspule",125,155,12,24,0,1000,0,0,
                    new[] {new UpgradeDefinition(75,18,186,24),new UpgradeDefinition(115,26,217,24)},
                    mechanic: WeaponMechanic.Chain,maxTargets:3,jumpRange:75,damagePermilleByHit:new[] {1000,650,400}),
                new TowerDefinition("Balliste",120,185,26,36,0,1000,0,0,
                    new[] {new UpgradeDefinition(80,38,222,36),new UpgradeDefinition(125,54,259,36)},
                    mechanic: WeaponMechanic.PierceLine,maxTargets:3,lineHalfWidth:18,damagePermilleByHit:new[] {1000,700,450}),
                new TowerDefinition("Laser",180,200,6,8,0,1000,0,0,
                    new[] {new UpgradeDefinition(85,10,240,8),new UpgradeDefinition(125,16,280,8)},mechanic:WeaponMechanic.Beam)
            }, new[]
            {
                new UnitDefinition("Läufer", 18, 34, 104, 0, 1, 1, 6, 4),
                new UnitDefinition("Sprinter", 24, 22, 184, 0, 1, 1, 8, 5),
                new UnitDefinition("Tank", 65, 220, 59, 5, 3, 1, 12, 14),
                new UnitDefinition("Schwarm", 38, 26, 93, 0, 1, 4, 10, 2),
                new UnitDefinition("Sturmkoloss",1200,1000,215,2,6,1,60,150),
                new UnitDefinition("Belagerungskoloss",1600,3200,46,8,10,1,90,210)
            }, MapCatalog.Create(0));
        }

        public void Validate()
        {
            if (TickRate != 20 || StartGold < 0 || StartGold > Rules.MaxGold || BaseHp < 1 || BaseHp > 10000
                || PassiveGoldPerSecond < 0 || PassiveGoldPerSecond > 1000 || Rules.MaxGold < 1 || Rules.MaxGold > 1000000
                || MaxUnitsPerField < 1 || MaxUnitsPerField > 80 || MaxSpawnOrdersPerPlayer < 1 || MaxSpawnOrdersPerPlayer > 80
                || MatchDurationTicks < 1 || MatchDurationTicks > 72000 || CountdownTicks < 0 || CountdownTicks > 200
                || Rules.MineSellRefundPercent < 0 || Rules.MineSellRefundPercent > 100
                || SellRefundPercent < 0 || SellRefundPercent > 100 || Rules.SpawnIntervalTicks < 1 || Rules.SpawnIntervalTicks > 200
                || Rules.MinimumSlowPermille < 500 || Rules.MinimumSlowPermille > 1000
                || Rules.MaxCommandsPerSecond < 1 || Rules.MaxCommandsPerSecond > 100)
                throw new ArgumentException("Invalid economy, timing or capacity rule.");
            if (towers.Length < 5 || towers.Length > 8 || units.Length != UnitClassCount || Map.SlotData.Length < 1 || Map.SlotData.Length > 32 || Map.Waypoints.Length > 32)
                throw new ArgumentException("Exactly four combat towers, one gold mine, four unit classes and 1–32 build slots are required.");
            for (int i = 0; i < towers.Length; i++)
            {
                TowerDefinition t = towers[i];
                if (t == null || string.IsNullOrWhiteSpace(t.Name) || t.Name.Length > 48 || t.Price < 1 || t.Price > Rules.MaxGold
                    || t.MaxPerPlayer < 1 || t.MaxPerPlayer > 32 || t.IncomeUnitsPerSecond < 0 || t.IncomeUnitsPerSecond > 96000
                    || t.SplashRadius < 0 || t.SplashRadius > 1000
                    || t.SlowPermille < Rules.MinimumSlowPermille || t.SlowPermille > 1000
                    || t.SlowDurationTicks < 0 || t.SlowDurationTicks > 200 || t.ArmorPiercing < 0 || t.ArmorPiercing > 1000
                    || t.UpgradeData.Length != 2) throw new ArgumentException("Invalid tower definition at " + i);
                if (t.IsEconomy != (i == 4)) throw new ArgumentException("Only tower index 4 may be an economy building.");
                if ((byte)t.Mechanic > (byte)WeaponMechanic.Beam || t.MaxTargets < 1 || t.MaxTargets > 3
                    || t.DamagePermilleData.Length != t.MaxTargets || t.DamagePermilleData[0] != 1000)
                    throw new ArgumentException("Invalid weapon mechanic.");
                int previousPermille=1000;
                foreach(int factor in t.DamagePermilleData)
                { if(factor<1 || factor>previousPermille)throw new ArgumentException("Invalid hit damage factors."); previousPermille=factor; }
                if (t.Mechanic==WeaponMechanic.Standard || t.Mechanic==WeaponMechanic.Beam)
                { if(t.MaxTargets!=1 || t.JumpRange!=0 || t.LineHalfWidth!=0)throw new ArgumentException("Standard weapons cannot carry chained-hit settings."); }
                else if(t.IsEconomy || t.SplashRadius!=0 || t.SlowDurationTicks!=0 || t.MaxTargets<2
                    || (t.Mechanic==WeaponMechanic.Chain && (t.JumpRange<1 || t.JumpRange>1000 || t.LineHalfWidth!=0))
                    || (t.Mechanic==WeaponMechanic.PierceLine && (t.LineHalfWidth<1 || t.LineHalfWidth>1000 || t.JumpRange!=0)))
                    throw new ArgumentException("Invalid chain/line geometry.");
                if (t.IsEconomy)
                {
                    if (t.Damage != 0 || t.Range != 0 || t.FireIntervalTicks != 0 || t.SplashRadius != 0
                        || t.SlowPermille != 1000 || t.SlowDurationTicks != 0 || t.ArmorPiercing != 0)
                        throw new ArgumentException("Economy buildings cannot attack.");
                }
                else if (!ValidWeapon(t.Damage, t.Range, t.FireIntervalTicks)) throw new ArgumentException("Invalid combat tower weapon.");
                int previousIncome = t.IncomeUnitsPerSecond;
                int previousDamage = t.Damage, previousRange = t.Range;
                foreach (UpgradeDefinition u in t.UpgradeData)
                {
                    if (u == null || u.Price < 1 || u.Price > Rules.MaxGold)
                        throw new ArgumentException("Invalid tower upgrade at " + i);
                    if (t.IsEconomy)
                    {
                        if (u.Damage != 0 || u.Range != 0 || u.FireIntervalTicks != 0
                            || u.IncomeUnitsPerSecond <= previousIncome || u.IncomeUnitsPerSecond > 96000)
                            throw new ArgumentException("Invalid economy upgrade at " + i);
                        previousIncome = u.IncomeUnitsPerSecond;
                    }
                    else
                    {
                        if (!ValidWeapon(u.Damage, u.Range, u.FireIntervalTicks) || u.IncomeUnitsPerSecond != 0
                            || u.Damage <= previousDamage || u.Range <= previousRange || u.FireIntervalTicks != t.FireIntervalTicks)
                            throw new ArgumentException("Combat upgrade paths must increase damage/range and preserve the base interval.");
                        previousDamage = u.Damage; previousRange = u.Range;
                    }
                }
            }
            for (int i = 0; i < units.Length; i++)
            {
                UnitDefinition u = units[i];
                if (u == null || string.IsNullOrWhiteSpace(u.Name) || u.Name.Length > 48 || u.Price < 1 || u.Price > Rules.MaxGold
                    || u.Hp < 1 || u.Hp > 100000 || u.SpeedPerSecond < 1 || u.SpeedPerSecond > 1000
                    || u.Armor < 0 || u.Armor > 1000 || u.BaseDamage < 1 || u.BaseDamage > 100
                    || u.Count < 1 || u.Count > 8 || u.CooldownTicks < 1 || u.CooldownTicks > 2000
                    || u.KillBounty < 0 || u.KillBounty > Rules.MaxGold)
                    throw new ArgumentException("Invalid unit definition at " + i);
            }
            if (MatchRules == null || (!MatchRules.Endless && MatchRules.WaveCount != waves.Length) || MatchRulesPolicy.Validate(this, MatchRules) != ErrorCode.None)
                throw new ArgumentException("Invalid exact match options.");
            if (waves.Length < 1 || waves.Length > MatchRulesPolicy.MaxWaveCount || research.Length != units.Length) throw new ArgumentException("Invalid wave/research catalog size.");
            foreach (var wave in waves) if (wave == null) throw new ArgumentException("Null wave definition.");
            foreach (var definition in research) if (definition == null) throw new ArgumentException("Null research definition.");
            MapPoint[] waypoints = Map.Waypoints;
            foreach (MapPoint p in waypoints)
                if (p.X < 0 || p.X > Map.Width || p.Y < 0 || p.Y > Map.Height) throw new ArgumentException("Waypoint outside board.");
            MapPoint[] slots = Map.SlotData;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].X < 0 || slots[i].X > Map.Width || slots[i].Y < 0 || slots[i].Y > Map.Height)
                    throw new ArgumentException("Build slot outside board.");
                for (int j = i + 1; j < slots.Length; j++)
                    if (slots[i].X == slots[j].X && slots[i].Y == slots[j].Y) throw new ArgumentException("Duplicate build slot.");
                for (int segment = 0; segment + 1 < waypoints.Length; segment++)
                    if (!HasBuildClearance(slots[i], waypoints[segment], waypoints[segment + 1]))
                        throw new ArgumentException("Build slot must be at least 40 logical units from the route.");
            }
        }

        private static bool ValidWeapon(int damage, int range, int interval) =>
            damage > 0 && damage <= 10000 && range > 0 && range <= 1500 && interval > 0 && interval <= 2000;

        private static bool HasBuildClearance(MapPoint point, MapPoint start, MapPoint end)
        {
            long dx = end.X - start.X, dy = end.Y - start.Y;
            long px = point.X - start.X, py = point.Y - start.Y;
            long lengthSquared = dx * dx + dy * dy, projection = px * dx + py * dy;
            if (projection <= 0) return px * px + py * py >= 40 * 40;
            if (projection >= lengthSquared)
            { px = point.X - end.X; py = point.Y - end.Y; return px * px + py * py >= 40 * 40; }
            long cross = dx * py - dy * px;
            return cross * cross >= 40L * 40 * lengthSquared;
        }

        private string ComputeHash()
        {
            var b = new StringBuilder(2048);
            Append(b, SimulationRulesVersion);
            Append(b,LateGameRules.Version);Append(b,LateGameRules.Canonical);
            Append(b, MatchRulesPolicy.WavePlanVersion); Append(b, MatchRules.StartGoldDecimal);
            Append(b, MatchRules.Endless ? 1 : 0); Append(b, MatchRules.WaveCount); Append(b, MatchRules.IncomeMultiplier); Append(b, (int)MatchRules.Format); Append(b, MatchRules.MapId);
            Append(b, baseContent == null ? string.Empty : baseContent.ContentHash);
            int[] rules = { TickRate, StartGold, BaseHp, PassiveGoldPerSecond, MaxUnitsPerField, MaxSpawnOrdersPerPlayer,
                MatchDurationTicks, CountdownTicks, SellRefundPercent, Rules.SpawnIntervalTicks, Rules.MinimumSlowPermille,
                Rules.MaxCommandsPerSecond, Rules.MaxGold, Rules.MineSellRefundPercent };
            foreach (int value in rules) Append(b, value);
            foreach (TowerDefinition t in towers)
            {
                Append(b, t.Name); Append(b, t.Price); Append(b, t.Range); Append(b, t.Damage); Append(b, t.FireIntervalTicks);
                Append(b, t.SplashRadius); Append(b, t.SlowPermille); Append(b, t.SlowDurationTicks); Append(b, t.ArmorPiercing);
                Append(b, t.IncomeUnitsPerSecond); Append(b, t.MaxPerPlayer);
                Append(b,(int)t.Mechanic); Append(b,t.MaxTargets); Append(b,t.JumpRange); Append(b,t.LineHalfWidth);
                foreach(int factor in t.DamagePermilleData)Append(b,factor);
                foreach (UpgradeDefinition u in t.UpgradeData)
                { Append(b, u.Price); Append(b, u.Damage); Append(b, u.Range); Append(b, u.FireIntervalTicks); Append(b, u.IncomeUnitsPerSecond); }
            }
            foreach (UnitDefinition u in units)
            {
                Append(b, u.Name); Append(b, u.Price); Append(b, u.Hp); Append(b, u.SpeedPerSecond);
                Append(b, u.Armor); Append(b, u.BaseDamage); Append(b, u.Count); Append(b, u.CooldownTicks); Append(b, u.KillBounty);
            }
            Append(b, Map.Id); Append(b, Map.Name); Append(b, Map.Theme); Append(b, Map.Width); Append(b, Map.Height); Append(b, Map.Length);
            foreach (MapPoint p in Map.Waypoints) { Append(b, p.X); Append(b, p.Y); }
            foreach (MapPoint p in Map.BuildSlots) { Append(b, p.X); Append(b, p.Y); }
            Append(b, Map.MineSlots.Length);
            foreach (int slot in Map.MineSlots) Append(b, slot);
            Append(b, Map.FreePlacement ? 1 : 0); Append(b, Map.BlockerData.Length);
            foreach (MapRectangle area in Map.BlockerData)
            { Append(b, area.MinX); Append(b, area.MinY); Append(b, area.MaxX); Append(b, area.MaxY); }
            Append(b, PlacementValidator.BuildingRadius); Append(b, PlacementValidator.PathClearance);
            Append(b, PlacementValidator.BuildingSeparation); Append(b, PlacementValidator.CombatCapacity);
            Append(b, PlacementValidator.MineCapacity); Append(b, PlacementValidator.RenderCapacity);
            Append(b, TowerUpgradeRules.MaxPurchases);
            Append(b, WaveCount);
            foreach (var wave in waves) { Append(b, wave.HpPercent); foreach (int count in wave.Counts) Append(b, count); }
            foreach (var r in research) { Append(b,r.HpPerLevel); Append(b,r.SpeedPerLevel); Append(b,r.BasePrice); Append(b,r.PriceStep); Append(b,r.QuadraticPrice); Append(b,r.MaxLevel); Append(b,r.HpQuadraticNumerator); Append(b,r.HpQuadraticDenominator); }
            int[] waveRules = { WaveRules.PreparationTicks, WaveRules.WaveBudget, WaveRules.NeutralSpawnIntervalTicks,
                WaveRules.MinimumSendWindowTicks, WaveRules.SendWindowTailTicks, WaveRules.MaxUnitsPerOrigin,
                WaveRules.IncomeUnitsPerGold, WaveRules.AttackIncomeDivisor, WaveRules.MaxAttackIncomeUnits };
            foreach (int rule in waveRules) Append(b, rule);
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(b.ToString()));
                var hex = new StringBuilder(64);
                foreach (byte value in hash) hex.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return hex.ToString();
            }
        }
        private static void Append(StringBuilder builder, int value) => builder.Append(value.ToString(CultureInfo.InvariantCulture)).Append('|');
        private static void Append(StringBuilder builder, string value) => builder.Append(value.Length).Append(':').Append(value).Append('|');
    }
}
