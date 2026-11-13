using System;

namespace DefenseDuell.Networking
{
    /// <summary>Backpressure for one reliable revision. Later revisions wait until every
    /// page is acknowledged; at most sixteen pages can occupy the transport send queue.</summary>
    public sealed class StateTransferWindow
    {
        public int Count { get; }
        public int Sent { get; private set; }
        public int Acknowledged { get; private set; }
        public bool Complete=>Acknowledged==Count;
        public StateTransferWindow(int count){if(count<1)throw new ArgumentOutOfRangeException(nameof(count));Count=count;}
        public bool TryTake(out int part)
        {part=-1;if(Sent==Count || Sent-Acknowledged>=16)return false;part=Sent++;return true;}
        public bool Acknowledge(int part)
        {if(part!=Acknowledged || part>=Sent)return false;Acknowledged++;return true;}
    }
}
