using System;
using System.Collections.Generic;
using System.Numerics;
using DefenseDuell.Core;

namespace DefenseDuell.Networking
{
    /// <summary>Validates a complete decoded frame before any of it becomes visible.</summary>
    internal static class WireSnapshotValidator
    {
        internal static bool ValidTowerMask(int mask,int towerCount)
            =>TowerLoadoutRules.IsValid(mask,towerCount);
        static bool LedgerShape(WaveLedger ledger,GameContent content,MatchState state)
            =>ledger!=null&&ledger.IsEndless==content.MatchRules.Endless
                &&ledger.Length==(content.MatchRules.Endless?2:content.WaveCount)
                &&ledger.FirstWave==(content.MatchRules.Endless?state.WaveId:BigInteger.One);

        internal static bool IsValid(GameContent content,MatchState state)
        {
            if(state.MatchRules==null||!state.MatchRules.Equals(content.MatchRules)||MatchRulesPolicy.Validate(content.BaseContent,state.MatchRules)!=ErrorCode.None)return false;
            int count=state.MatchRules.PlayerCount,teamCount=state.MatchRules.TeamCount;
            if(state.Players==null||state.Players.Length!=count||state.Teams==null||state.Teams.Length!=teamCount
                ||state.ConnectedPlayerMask<1||(state.ConnectedPlayerMask&1)==0||(state.ConnectedPlayerMask&~((1<<count)-1))!=0
                ||state.SessionId==0||state.Tick<0||state.PlayTicks<0||(long)state.PlayTicks>2L*state.Tick
                ||state.CountdownRemaining<0||state.CountdownRemaining>content.CountdownTicks
                ||(uint)state.Phase>(uint)MatchPhase.Aborted||!Enum.IsDefined(typeof(MatchOutcome),state.Outcome)
                ||(byte)state.WavePhase>(byte)WavePhase.Clearing||!content.ContainsWave(state.WaveId)
                ||state.PreparationRemaining<0||state.PreparationRemaining>content.WaveRules.PreparationTicks
                ||state.WaveStartPlayTick<0||state.WaveStartPlayTick>state.PlayTicks
                ||state.Phase==MatchPhase.Lobby&&state.StartGoldGranted
                ||(state.Phase==MatchPhase.Countdown||state.Phase==MatchPhase.Playing||state.Phase==MatchPhase.Finished)&&!state.StartGoldGranted)return false;
            int expectedWinner=state.Outcome==MatchOutcome.Player0?0:state.Outcome==MatchOutcome.Player1?1:state.Outcome==MatchOutcome.Team2?2:-1;
            if(state.WinningTeamId!=expectedWinner||expectedWinner>=teamCount)return false;
            if(state.Phase==MatchPhase.Finished)
            {if(state.Outcome!=MatchOutcome.Player0&&state.Outcome!=MatchOutcome.Player1&&state.Outcome!=MatchOutcome.Team2&&state.Outcome!=MatchOutcome.Draw)return false;}
            else if(state.Phase==MatchPhase.Aborted){if(state.Outcome!=MatchOutcome.Aborted)return false;}
            else if(state.Outcome!=MatchOutcome.None)return false;
            if(state.WavePhase!=WavePhase.Preparing&&state.PreparationRemaining!=0)return false;
            if(state.NeutralCursor==null||state.NeutralCursor.Length!=count||state.NextSpawnTicks==null||state.NextSpawnTicks.Length!=count
                ||state.NextSpawnOrigins==null||state.NextSpawnOrigins.Length!=count||state.NextPaidSender==null||state.NextPaidSender.Length!=count
                ||state.NextPaidSpawnTicks==null||state.NextPaidSpawnTicks.Length!=count)return false;
            var towers=content.Towers;var units=content.Units;var plan=content.GetWave(state.WaveId).Plan;
            var neutralPrefix=new int[count,4];var teamMembers=new int[teamCount];bool anyReady=false;
            foreach(var team in state.Teams)if(team==null||team.Hp<0||team.Hp>content.BaseHp*state.MatchRules.MembersPerTeam)return false;
            for(int p=0;p<count;p++)
            {
                var player=state.Players[p];
                if(player==null||player.TeamId<0||player.TeamId>=teamCount||(int)player.AttackTarget>=count
                    ||player.Gold<0||player.Hp!=state.Teams[player.TeamId].Hp||!ValidTowerMask(player.AvailableTowerMask,towers.Length)
                    ||player.NextSendTicks==null||player.NextSendTicks.Length!=content.Units.Length||player.ResearchLevels==null||player.ResearchLevels.Length!=content.Units.Length
                    ||player.GoldRemainder<0||player.GoldRemainder>=content.WaveRules.IncomeUnitsPerGold||player.ArchivedSpawnedQuarterGold<0
                    ||player.AttackIncomeUnits<0||player.AttackIncomeUnits>content.WaveRules.MaxAttackIncomeUnits
                    ||!LedgerShape(player.ReservedGoldByWave,content,state)||!LedgerShape(player.SpawnedQuarterGoldByWave,content,state)||!LedgerShape(player.CancelledQuarterGoldByWave,content,state)
                    ||state.NeutralCursor[p]<0||state.NeutralCursor[p]>plan.Length||state.NextSpawnTicks[p]<0||(byte)state.NextSpawnOrigins[p]>(byte)UnitOrigin.PaidPvP
                    ||state.NextPaidSender[p]<0||state.NextPaidSender[p]>=count||state.NextPaidSpawnTicks[p]<0)return false;
                teamMembers[player.TeamId]++;anyReady|=player.Ready;
                if(!state.StartGoldGranted&&(player.Gold!=0||player.GoldRemainder!=0||player.AttackIncomeUnits!=0||player.ArchivedSpawnedQuarterGold!=0))return false;
                if(state.WavePhase!=WavePhase.Preparing&&player.WaveReady)return false;
                for(int k=0;k<content.Units.Length;k++)if(player.NextSendTicks[k]<0||player.ResearchLevels[k]<0||!state.StartGoldGranted&&(player.NextSendTicks[k]!=0||player.ResearchLevels[k]!=0))return false;
                if(!state.StartGoldGranted)
                {foreach(var value in player.ReservedGoldByWave)if(value!=0)return false;foreach(var value in player.SpawnedQuarterGoldByWave)if(value!=0)return false;foreach(var value in player.CancelledQuarterGoldByWave)if(value!=0)return false;}
                for(int n=0;n<state.NeutralCursor[p];n++)neutralPrefix[p,plan[n]]++;
                if(state.WavePhase==WavePhase.Preparing&&state.NeutralCursor[p]!=0)return false;
            }
            if(anyReady&&state.ConnectedPlayerMask!=((1<<count)-1))return false;
            if(state.StartGoldGranted||anyReady)foreach(int members in teamMembers)if(members!=state.MatchRules.MembersPerTeam)return false;
            if(state.Towers==null||state.Units==null||state.SpawnOrders==null)return false;
            if(!state.StartGoldGranted&&(state.Towers.Count!=0||state.Units.Count!=0||state.SpawnOrders.Count!=0))return false;
            var ids=new HashSet<int>();var slots=new HashSet<long>();
            foreach (var tower in state.Towers)
            {
                if (tower == null || tower.Id <= 0 || !ids.Add(tower.Id) || (int)tower.Owner >= count || tower.Slot < 0
                    || tower.Kind < 0 || tower.Kind >= towers.Length || !slots.Add(((long)(byte)tower.Owner<<32)|(uint)tower.Slot)
                    || (state.Players[(int)tower.Owner].AvailableTowerMask & (1 << tower.Kind)) == 0
                    || TowerUpgradeRules.ValidateState(content, tower) != ErrorCode.None
                    || tower.Invested != TowerUpgradeRules.TotalInvestment(content,tower.Kind,tower.Level)
                    || (byte)tower.Priority > (byte)TargetPriority.Strongest
                    || (towers[tower.Kind].IsEconomy && tower.Priority != TargetPriority.Front)
                    || (!content.Map.FreePlacement && tower.SiteId != tower.Slot)) return false;
                var stats = TowerUpgradeRules.Stats(content, tower);
                if (tower.CooldownTicks < 0 || tower.CooldownTicks > stats.FireIntervalTicks) return false;
            }
            foreach (var tower in state.Towers)
                if (PlacementValidator.ValidatePosition(content, state, tower.Owner, tower.Kind, tower.SiteId, tower.Position, tower.Id) != ErrorCode.None) return false;
            var neutralAlive = new int[count, 4]; var minimumPaidQuarter = new BigInteger[count];
            var neutralOrdinals=new HashSet<(PlayerSlot,int)>();
            foreach (var unit in state.Units)
            {
                if (unit == null || unit.Id <= 0 || !ids.Add(unit.Id) || (int)unit.Defender >= count || (int)unit.Sender >= count || unit.Kind < 0 || unit.Kind >= units.Length
                    || (byte)unit.Origin > (byte)UnitOrigin.PaidPvP || unit.WaveId != state.WaveId || unit.ResearchLevel < 0
                    || (unit.Origin == UnitOrigin.NeutralWave && (unit.Kind>=GameContent.NeutralClassCount || unit.ResearchLevel != 0 || unit.Sender!=unit.Defender
                        || unit.NeutralRecipeIndex<0||unit.NeutralRecipeIndex>=state.NeutralCursor[(int)unit.Defender]
                        || !content.GetWave(unit.WaveId).MatchesNeutral(unit.NeutralRecipeIndex,unit.Kind)||!neutralOrdinals.Add((unit.Defender,unit.NeutralRecipeIndex))))
                    || (unit.Origin == UnitOrigin.PaidPvP && (unit.NeutralRecipeIndex!=-1 || state.Players[(int)unit.Sender].TeamId==state.Players[(int)unit.Defender].TeamId || unit.ResearchLevel > state.Players[(int)unit.Sender].ResearchLevels[unit.Kind]))
                    || unit.Distance < 0 || unit.Distance > content.Map.Length || unit.SlowPermille < content.Rules.MinimumSlowPermille || unit.SlowPermille > 1000
                    || unit.SlowUntilTick < 0 || unit.MovementRemainder < 0 || unit.MovementRemainder >= content.TickRate * 1000) return false;
                var recipe = UnitResearchRules.Resolve(content, unit.Kind, unit.ResearchLevel, unit.Origin, unit.WaveId,unit.NeutralRecipeIndex);
                if (unit.Hp < 1 || unit.Hp > recipe.MaxHp) return false;
                if (unit.Origin == UnitOrigin.NeutralWave)
                {
                    if (++neutralAlive[(int)unit.Defender, unit.Kind] > neutralPrefix[(int)unit.Defender, unit.Kind]) return false;
                }
                else minimumPaidQuarter[(int)unit.Sender] += 4L * units[unit.Kind].Price / units[unit.Kind].Count;
            }
            var ordersPerPlayer = new int[count];
            foreach (var order in state.SpawnOrders)
            {
                if (order == null || (int)order.Sender >= count || (int)order.EnemyTarget >= count || order.Kind < 0 || order.Kind >= units.Length || order.Origin != UnitOrigin.PaidPvP
                    || order.WaveId < state.WaveId || !content.ContainsWave(order.WaveId) || order.WaveId>=state.Players[(int)order.Sender].ReservedGoldByWave.FirstWave+state.Players[(int)order.Sender].ReservedGoldByWave.Length
                    || state.Players[(int)order.Sender].TeamId==state.Players[(int)order.EnemyTarget].TeamId || order.ResearchLevel < 0
                    || order.ResearchLevel > state.Players[(int)order.Sender].ResearchLevels[order.Kind]
                    || order.PaidPrice != units[order.Kind].Price || order.InitialCount != units[order.Kind].Count
                    || order.Remaining < 1 || order.Remaining > order.InitialCount || order.NextTick < 0) return false;
                ordersPerPlayer[(int)order.Sender]++;
                if (order.Remaining < order.InitialCount && (ordersPerPlayer[(int)order.Sender] != 1 || order.WaveId != state.WaveId)) return false;
            }
            for (var p = 0; p < count; p++)
                if (EconomyRules.ValidateLedger(content, state, (PlayerSlot)p) != ErrorCode.None
                    || minimumPaidQuarter[p] > state.Players[p].SpawnedQuarterGoldByWave[state.WaveId - 1]) return false;
            if (state.WavePhase == WavePhase.Preparing && state.Units.Count != 0) return false;
            return true;
        }
    }
}
