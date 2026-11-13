using System.Numerics;

namespace DefenseDuell.Core
{
    public sealed partial class MatchSimulation
    {
        private ErrorCode UpgradeBatch(PlayerSlot sender,CommandEnvelope command)
        {
            var tower=FindTower(command.Target);
            if(tower==null)return ErrorCode.InvalidTarget;
            if(tower.Owner!=sender)return ErrorCode.NotOwner;
            if(command.Kind<(int)TowerUpgradePath.Damage||command.Kind>(int)TowerUpgradePath.JumpRange)return ErrorCode.InvalidKind;
            var validity=TowerUpgradeRules.ValidateState(Content,tower);
            if(validity!=ErrorCode.None)return validity;
            var path=(TowerUpgradePath)command.Kind;
            if(!TowerUpgradeRules.IsAllowedPath(Content,tower.Kind,path))return ErrorCode.InvalidKind;
            if(command.UpgradeCount<=0||command.ExpectedTowerLevel<0||command.UpgradePriceLimit<=0)return ErrorCode.InvalidValue;
            if(tower.Level!=command.ExpectedTowerLevel)return ErrorCode.QuoteChanged;
            BigInteger cost=TowerUpgradeBatchRules.Cost(Content,tower.Kind,tower.Level,command.UpgradeCount);
            if(cost>command.UpgradePriceLimit)return ErrorCode.QuoteChanged;
            var player=State.Players[(int)sender];
            if(player.Gold<cost)return ErrorCode.InsufficientGold;
            bool economy=towers[tower.Kind].IsEconomy;
            bool secondary=!economy&&path==TowerUpgradeRules.SecondaryPath(Content,tower.Kind);
            // Validate everything before committing one transaction. Never reset firing cooldown.
            player.Gold-=cost;tower.Invested+=cost;tower.Level+=command.UpgradeCount;
            if(secondary)tower.RangeLevel+=command.UpgradeCount;
            else if(!economy)tower.DamageLevel+=command.UpgradeCount;
            return ErrorCode.None;
        }
    }
}
