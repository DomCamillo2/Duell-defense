using System.Collections.Generic;

namespace DefenseDuell.Presentation
{
    /// <summary>Live contacts stay blocked until release; only the last 64 completed contacts are retained.</summary>
    internal sealed class PointerSuppressionHistory
    {
        internal const int Capacity=64;
        readonly HashSet<int> active=new HashSet<int>();
        readonly HashSet<int> recent=new HashSet<int>();
        readonly List<int> ending=new List<int>(4);
        readonly int[] ids=new int[Capacity];
        readonly bool[] used=new bool[Capacity];
        int cursor;
        internal int RetainedCount=>recent.Count;
        internal bool Contains(int id)=>active.Contains(id)||recent.Contains(id);
        internal void Begin(int id){active.Remove(id);Forget(id);}
        internal void Block(int id)=>active.Add(id);
        internal void End(int id)
        {
            if(!active.Remove(id))return;Forget(id);
            if(used[cursor])recent.Remove(ids[cursor]);
            ids[cursor]=id;used[cursor]=true;recent.Add(id);cursor=(cursor+1)%Capacity;
        }
        internal bool Consume(int id){bool blocked=active.Remove(id)||recent.Contains(id);Forget(id);return blocked;}
        internal void EndAll(){ending.Clear();foreach(int id in active)ending.Add(id);for(int i=0;i<ending.Count;i++)End(ending[i]);ending.Clear();}
        void Forget(int id)
        {
            if(!recent.Remove(id))return;
            for(int i=0;i<Capacity;i++)if(used[i]&&ids[i]==id)used[i]=false;
        }
        internal void Clear(){active.Clear();recent.Clear();ending.Clear();System.Array.Clear(used,0,used.Length);cursor=0;}
    }
}
