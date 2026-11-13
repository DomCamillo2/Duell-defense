using System.Numerics;
namespace DefenseDuell.Core
{
    public readonly struct SendQuote
    {
        public ErrorCode Code { get; }
        public bool IsValid => Code == ErrorCode.None;
        public int Kind { get; }
        public BigInteger Price { get; }
        public BigInteger ResearchLevel { get; }
        public BigInteger WaveId { get; }
        public int EnemyTarget { get; }
        public SendQuote(ErrorCode code,int kind,BigInteger price,BigInteger researchLevel,BigInteger waveId,int enemyTarget=-1)
        { Code=code; Kind=kind; Price=price; ResearchLevel=researchLevel; WaveId=waveId; EnemyTarget=enemyTarget; }
    }
    public static class SendRules
    {
        public static SendQuote Quote(GameContent content, MatchState state, PlayerSlot sender, int kind,int enemyTarget=-1)
        {
            if (!state.ValidPlayer(sender)) return new SendQuote(ErrorCode.InvalidPlayer,kind,0,0,0);
            if (kind < 0 || kind >= content.UnitData.Length) return new SendQuote(ErrorCode.InvalidKind,kind,0,0,0);
            var player=state.Players[(int)sender]; BigInteger price=content.UnitData[kind].Price, level=player.ResearchLevels[kind];
            if(enemyTarget<0)enemyTarget=(int)player.AttackTarget;
            if(enemyTarget>=state.Players.Length||!state.CanTarget(sender,(PlayerSlot)enemyTarget))return new SendQuote(ErrorCode.InvalidTarget,kind,price,level,0,enemyTarget);
            if (state.Phase != MatchPhase.Playing) return new SendQuote(ErrorCode.WrongPhase,kind,price,level,0);
            BigInteger first=state.WaveId+(state.WavePhase == WavePhase.Clearing ? 1 : 0);
            foreach (var order in state.SpawnOrders) if (order.Sender == sender) { if(order.WaveId>first)first=order.WaveId; }
            BigInteger wave=first;
            if (!content.ContainsWave(wave)) return new SendQuote(ErrorCode.WaveCapacity,kind,price,level,0);
            ErrorCode code=player.Gold < price ? ErrorCode.InsufficientGold : ErrorCode.None;
            return new SendQuote(code,kind,price,level,wave,enemyTarget);
        }
    }
}
