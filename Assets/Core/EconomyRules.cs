using System;
using System.Numerics;

namespace DefenseDuell.Core
{
    public static class EconomyRules
    {
        public static int RefundPercent(GameContent content,int towerKind)
        {
            if(content==null)throw new ArgumentNullException(nameof(content));
            if(towerKind<0||towerKind>=content.TowerData.Length)throw new ArgumentOutOfRangeException(nameof(towerKind));
            return content.TowerData[towerKind].IsEconomy?content.Rules.MineSellRefundPercent:content.SellRefundPercent;
        }
        public static BigInteger RefundGold(GameContent content,TowerState tower)
        {
            if(tower==null)throw new ArgumentNullException(nameof(tower));
            return tower.Invested*RefundPercent(content,tower.Kind)/100;
        }
        public static BigInteger IncomeUnits(GameContent content, MatchState state, PlayerSlot player)
        {
            BigInteger income=(content.PassiveGoldPerSecond*content.WaveRules.IncomeUnitsPerGold+state.Players[(int)player].AttackIncomeUnits);
            foreach(var tower in state.Towers) if(tower.Owner==player && content.TowerData[tower.Kind].IsEconomy)
                income=(income+TowerUpgradeRules.Stats(content,tower).IncomeUnitsPerSecond);
            return income*content.MatchRules.IncomeMultiplier;
        }
        public static BigInteger EffectiveAttackIncomeUnits(GameContent content,PlayerState player)=> (BigInteger)player.AttackIncomeUnits*content.MatchRules.IncomeMultiplier;
        // The schema's 960/240 ratio makes these exact quarter-gold credits also income units.
        public static BigInteger SpawnCreditUnits(SpawnOrder order,int nextSpawnNumber)
        {
            if(order.PaidPrice<1 || order.InitialCount<1 || nextSpawnNumber<1 || nextSpawnNumber>order.InitialCount)
                throw new ArgumentException("Invalid paid packet credit.");
            return (4*order.PaidPrice*nextSpawnNumber/order.InitialCount)
                -(4*order.PaidPrice*(nextSpawnNumber-1)/order.InitialCount);
        }
        public static ErrorCode ValidateLedger(GameContent content,MatchState state,PlayerSlot sender)
        {
            if(!state.ValidPlayer(sender))return ErrorCode.InvalidPlayer;
            var player=state.Players[(int)sender];
            if(player==null||player.GoldRemainder<0||player.GoldRemainder>=content.WaveRules.IncomeUnitsPerGold||player.ArchivedSpawnedQuarterGold<0)return ErrorCode.InvalidValue;
            var ledgers=new[]{player.ReservedGoldByWave,player.SpawnedQuarterGoldByWave,player.CancelledQuarterGoldByWave};
            int length=content.MatchRules.Endless?2:content.WaveCount;
            BigInteger first=content.MatchRules.Endless?state.WaveId:BigInteger.One;
            foreach(var ledger in ledgers)if(ledger==null||ledger.Length!=length||ledger.FirstWave!=first||ledger.IsEndless!=content.MatchRules.Endless)return ErrorCode.InvalidValue;
            if(!content.MatchRules.Endless&&player.ArchivedSpawnedQuarterGold!=0)return ErrorCode.InvalidValue;
            BigInteger total=player.ArchivedSpawnedQuarterGold,prior=0;
            foreach(var order in state.SpawnOrders)if(order.Sender==sender)
            {
                if(order.Origin!=UnitOrigin.PaidPvP||order.Kind<0||order.Kind>=content.UnitData.Length
                    ||order.WaveId<state.WaveId||!content.ContainsWave(order.WaveId)||order.WaveId<prior
                    ||order.WaveId>=first+length||!state.ValidPlayer(order.EnemyTarget)
                    ||state.Players[(int)order.EnemyTarget].TeamId==player.TeamId||order.ResearchLevel<0
                    ||order.PaidPrice!=content.UnitData[order.Kind].Price||order.InitialCount!=content.UnitData[order.Kind].Count
                    ||order.Remaining<1||order.Remaining>order.InitialCount||order.NextTick<0
                    ||(order.WaveId>state.WaveId&&order.Remaining!=order.InitialCount))return ErrorCode.InvalidValue;
                prior=order.WaveId;
            }
            for(int i=0;i<length;i++)
            {
                BigInteger w=first+i,reserved=player.ReservedGoldByWave[w-1],spawned=player.SpawnedQuarterGoldByWave[w-1],cancelled=player.CancelledQuarterGoldByWave[w-1];
                if(reserved<0||spawned<0||cancelled<0||spawned+cancelled>4*reserved||(w>state.WaveId&&spawned!=0)
                    ||(w==state.WaveId&&state.WavePhase==WavePhase.Preparing&&spawned!=0))return ErrorCode.InvalidValue;
                BigInteger open=0;
                foreach(var order in state.SpawnOrders)if(order.Sender==sender&&order.WaveId==w)open+=4*order.PaidPrice-4*order.PaidPrice*(order.InitialCount-order.Remaining)/order.InitialCount;
                if(open+spawned+cancelled!=4*reserved)return ErrorCode.InvalidValue;
                total+=spawned;
            }
            return player.AttackIncomeUnits==BigInteger.Min(content.WaveRules.MaxAttackIncomeUnits,total)?ErrorCode.None:ErrorCode.InvalidValue;
        }
    }
}
