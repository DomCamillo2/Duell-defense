using System;
using System.Collections.Generic;
using System.Numerics;

namespace DefenseDuell.Core
{
    public sealed class WaveDefinition
    {
        public const int ContinuationHpStep = 12;
        // Technical representation bound: 1..Int32.MaxValue percent, with BigInteger effective HP.
        // Default 200-wave plan reaches 2488%; continuation overflow is rejected before applying options.
        readonly int[] counts, plan;
        public int[] Counts => (int[])counts.Clone();
        public int[] Plan => (int[])plan.Clone();
        internal int[] PlanData => plan;
        public int HpPercent { get; }
        public int TotalCount => plan.Length;
        public WaveDefinition(int[] counts, int hpPercent)
        {
            if (counts == null || counts.Length != 4) throw new ArgumentException("Four neutral class counts required.");
            this.counts = (int[])counts.Clone(); HpPercent = hpPercent;
            int total = 0;
            foreach (int count in counts) { if (count < 0 || count > 64) throw new ArgumentOutOfRangeException(nameof(counts)); total += count; }
            if (total < 1 || total > 64 || hpPercent < 1) throw new ArgumentException("Invalid bounded neutral plan.");
            var remaining = (int[])counts.Clone(); var sequence = new List<int>(total);
            while (sequence.Count < total) for (int k = 0; k < 4; k++) if (remaining[k] > 0) { sequence.Add(k); remaining[k]--; }
            plan = sequence.ToArray();
        }
        public static WaveDefinition[] CreateDefault()
        {
            int[,] counts = { {10,0,0,0}, {12,4,0,0}, {12,4,0,4}, {12,4,2,6}, {10,8,2,8},
                {10,8,4,10}, {10,10,4,12}, {10,12,6,12}, {10,14,8,12}, {8,14,10,16} };
            var waves = new WaveDefinition[10];
            for (int w = 0; w < waves.Length; w++) waves[w] = new WaveDefinition(new[] { counts[w,0], counts[w,1], counts[w,2], counts[w,3] }, 100 + 12*w);
            return waves;
        }
    }

    /// <summary>On-demand exact wave recipe; no cache/history grows with endless wave index.</summary>
    public sealed class WaveRecipe
    {
        private readonly WaveDefinition source;
        private readonly BigInteger[] hpPerKind;
        public int[] Counts=>source.Counts;
        public int[] Plan=>source.Plan;
        internal int[] PlanData=>source.PlanData;
        public int TotalCount=>source.TotalCount;
        public BigInteger HpPercent { get; }
        public string ProfileId { get; }
        public string Title { get; }
        public string CounterHint { get; }
        public bool IsBossWave { get; }
        public bool IsSuperBossWave { get; }
        public int BossUnitIndex { get; }
        public BigInteger PressureBudgetHp { get; }
        public bool HasPerKindHp=>hpPerKind!=null;
        public bool MatchesNeutral(int ordinal,int kind)=>ordinal>=0&&ordinal<source.TotalCount&&source.PlanData[ordinal]==kind;
        public BigInteger HpForKind(int kind)
        {
            if(kind<0||kind>=GameContent.NeutralClassCount)throw new ArgumentOutOfRangeException(nameof(kind));
            if(hpPerKind==null)throw new InvalidOperationException("Recipe has no effective per-kind HP.");
            return hpPerKind[kind];
        }
        internal WaveRecipe(WaveDefinition source,BigInteger hpPercent,BigInteger[] hp=null,string profileId="authored",
            string title="Grundwelle",string hint="Grundverteidigung und Wegabdeckung ausbauen",bool boss=false,BigInteger budget=default,bool superBoss=false)
        {
            this.source=source;HpPercent=hpPercent;hpPerKind=hp==null?null:(BigInteger[])hp.Clone();
            ProfileId=profileId;Title=title;CounterHint=hint;IsBossWave=boss;IsSuperBossWave=boss&&superBoss;PressureBudgetHp=budget;
            BossUnitIndex=boss?Array.IndexOf(source.PlanData,2):-1;
        }
    }

    public sealed class WaveRules
    {
        public int PreparationTicks { get; }
        /// <summary>Legacy serialized budget metadata; paid purchases have no per-wave gold cap.</summary>
        public int WaveBudget { get; }
        public int NeutralSpawnIntervalTicks { get; }
        public int MinimumSendWindowTicks { get; }
        public int SendWindowTailTicks { get; }
        /// <summary>Legacy pool hint only, never a live neutral or paid entity cap.</summary>
        public int MaxUnitsPerOrigin { get; }
        public int IncomeUnitsPerGold { get; }
        public int AttackIncomeDivisor { get; }
        public int MaxAttackIncomeUnits { get; }
        public WaveRules(int preparationTicks=200, int waveBudget=240, int neutralSpawnIntervalTicks=10,
            int minimumSendWindowTicks=600, int sendWindowTailTicks=80, int maxUnitsPerOrigin=64,
            int incomeUnitsPerGold=960, int attackIncomeDivisor=240, int maxAttackIncomeUnits=2880)
        {
            if (preparationTicks < 1 || preparationTicks > 1200 || waveBudget < 1 || waveBudget > 10000
                || neutralSpawnIntervalTicks < 1 || neutralSpawnIntervalTicks > 200 || minimumSendWindowTicks < 1 || minimumSendWindowTicks > 12000
                || sendWindowTailTicks < 1 || sendWindowTailTicks > 2000 || maxUnitsPerOrigin < 1 || maxUnitsPerOrigin > 64
                || incomeUnitsPerGold != 960 || attackIncomeDivisor != 240 || maxAttackIncomeUnits < 0 || maxAttackIncomeUnits > 96000)
                throw new ArgumentException("Invalid wave/economy rules; this schema uses exact 960 units and divisor 240.");
            PreparationTicks=preparationTicks; WaveBudget=waveBudget; NeutralSpawnIntervalTicks=neutralSpawnIntervalTicks;
            MinimumSendWindowTicks=minimumSendWindowTicks; SendWindowTailTicks=sendWindowTailTicks; MaxUnitsPerOrigin=maxUnitsPerOrigin;
            IncomeUnitsPerGold=incomeUnitsPerGold; AttackIncomeDivisor=attackIncomeDivisor; MaxAttackIncomeUnits=maxAttackIncomeUnits;
        }
        public int SendWindowTicks(WaveRecipe wave) => Math.Max(MinimumSendWindowTicks, (wave.TotalCount-1)*NeutralSpawnIntervalTicks+SendWindowTailTicks);
        public int SendWindowTicks(WaveDefinition wave) => Math.Max(MinimumSendWindowTicks, (wave.TotalCount-1)*NeutralSpawnIntervalTicks+SendWindowTailTicks);
    }

    public sealed class UnitResearchDefinition
    {
        public int HpPerLevel { get; }
        public int SpeedPerLevel { get; }
        public int BasePrice { get; }
        public int PriceStep { get; }
        public int QuadraticPrice { get; }
        /// <summary>Compatibility sentinel: zero means unlimited research levels.</summary>
        public int MaxLevel { get; }
        public int HpQuadraticNumerator { get; }
        public int HpQuadraticDenominator { get; }
        public UnitResearchDefinition(int hpPerLevel, int speedPerLevel, int basePrice, int priceStep, int quadraticPrice=5, int maxLevel=0, int hpQuadraticNumerator=0, int hpQuadraticDenominator=1)
        {
            if (hpPerLevel < 0 || hpPerLevel > 10000 || speedPerLevel < 0 || speedPerLevel > 100
                || basePrice < 1 || basePrice > 10000 || priceStep < 0 || priceStep > 10000 || quadraticPrice < 0 || quadraticPrice > 10000
                || maxLevel < 0 || hpQuadraticNumerator < 0 || hpQuadraticNumerator > 10000 || hpQuadraticDenominator < 1 || hpQuadraticDenominator > 10000) throw new ArgumentException("Invalid research coefficients.");
            HpPerLevel=hpPerLevel; SpeedPerLevel=speedPerLevel; BasePrice=basePrice; PriceStep=priceStep; QuadraticPrice=quadraticPrice; MaxLevel=0; HpQuadraticNumerator=hpQuadraticNumerator; HpQuadraticDenominator=hpQuadraticDenominator;
        }
        public static UnitResearchDefinition[] CreateDefault() => new[] {
            new UnitResearchDefinition(4,2,15,4,1,0,1,2), new UnitResearchDefinition(1,5,20,5,1,0,1,4),
            new UnitResearchDefinition(40,0,45,12,2,0,4,1), new UnitResearchDefinition(3,0,30,7,1,0,1,2),
            new UnitResearchDefinition(70,0,350,90,20,0,6,2),new UnitResearchDefinition(180,0,450,120,28,0,24,2) };
    }
}
