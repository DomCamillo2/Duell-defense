using DefenseDuell.Core;
using System.Numerics;

namespace DefenseDuell.Networking
{
    /// <summary>Pure acceptance guard. ACK completion never calls this or changes the active epoch.</summary>
    public sealed class StatePublicationGate
    {
        public ulong Epoch { get; private set; }
        public ulong Sequence { get; private set; }
        public MatchPhase Phase { get; private set; }
        public int Tick { get; private set; }
        public int PlayTicks { get; private set; }
        public BigInteger Wave { get; private set; }
        private ulong configRevision,rosterRevision;
        private MatchRules rules;
        private int connectedMask;
        private int[] teams,loadouts;
        public void Reset(){Epoch=Sequence=0;Tick=PlayTicks=0;Wave=0;Phase=MatchPhase.Lobby;configRevision=rosterRevision=0;rules=null;connectedMask=0;teams=loadouts=null;}
        public bool TryAccept(MatchState next,ulong sequence,ulong previousEpoch,ulong requestedRematchEpoch,int localSlot=1)
        {
            if(next==null || next.MatchRules==null || next.Players==null || next.Players.Length!=next.MatchRules.PlayerCount || localSlot<0 || localSlot>=next.Players.Length || next.SessionId==0 || sequence==0)return false;
            if(Epoch!=0)
            {
                // The publication counter spans the whole connection, including rematches.
                if(sequence<=Sequence)return false;
                if(next.SessionId!=Epoch)
                {
                    if(Epoch==ulong.MaxValue || previousEpoch!=Epoch || next.SessionId!=Epoch+1
                        || requestedRematchEpoch!=Epoch || Phase!=MatchPhase.Finished || next.Phase!=MatchPhase.Lobby
                        || next.StartGoldGranted || next.Players[localSlot].Ready || !next.MatchRules.Equals(rules))return false;
                }
                else if(next.Tick<Tick || next.PlayTicks<PlayTicks || next.WaveId<Wave
                    || (int)next.Phase<(int)Phase || next.ConfigRevision<configRevision || next.RosterRevision<rosterRevision
                    || next.ConfigRevision==configRevision && !next.MatchRules.Equals(rules)
                    || next.RosterRevision==rosterRevision && next.ConnectedPlayerMask!=connectedMask
                    || Phase!=MatchPhase.Lobby && (next.ConfigRevision!=configRevision || next.RosterRevision!=rosterRevision || !next.MatchRules.Equals(rules)))return false;
            }
            else if(previousEpoch!=0)return false;
            if(Epoch==next.SessionId && next.RosterRevision==rosterRevision && teams!=null)
            {
                if(teams.Length!=next.Players.Length)return false;
                for(int p=0;p<teams.Length;p++)if(teams[p]!=next.Players[p].TeamId||loadouts[p]!=next.Players[p].AvailableTowerMask)return false;
            }
            if(teams==null||teams.Length!=next.Players.Length){teams=new int[next.Players.Length];loadouts=new int[next.Players.Length];}
            for(int p=0;p<teams.Length;p++){teams[p]=next.Players[p].TeamId;loadouts[p]=next.Players[p].AvailableTowerMask;}
            Epoch=next.SessionId;Sequence=sequence;Phase=next.Phase;Tick=next.Tick;PlayTicks=next.PlayTicks;Wave=next.WaveId;configRevision=next.ConfigRevision;rosterRevision=next.RosterRevision;rules=next.MatchRules;connectedMask=next.ConnectedPlayerMask;return true;
        }
    }
}
