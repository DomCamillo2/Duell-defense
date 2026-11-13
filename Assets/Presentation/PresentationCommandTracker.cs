using BigInteger = System.Numerics.BigInteger;
using System;
using System.Collections.Generic;
using DefenseDuell.Core;

namespace DefenseDuell.Presentation
{
    /// <summary>Captured before Submit, including the synchronous local-host path. Never changes simulation state.</summary>
    public sealed class PresentationCommandContext
    {
        public ulong SessionId,Sequence;
        public static bool IsUpgrade(CommandType type)=>type==CommandType.UpgradeTower||type==CommandType.UpgradeTowerBatch;
        public PlayerSlot Player;
        public CommandType Type;
        public BigInteger ExpectedWave;
        public BigInteger ExpectedResearch;
        public int Kind,Target,Slot=-1,Entity=-1,SubmittedTick;
        public BigInteger PreviousLevel=-1,ExpectedLevel;
        public TowerUpgradePath UpgradePath;
        public bool FromConfirmation;
        public BigInteger PreviousDamageLevel,PreviousRangeLevel,ExpectedDamageLevel,ExpectedRangeLevel;
        public MapPoint Position;
        public int SiteId=-1;
        public double SubmittedAt;
        internal int ObservedEntity=-1,AckTick;
        internal bool Resolved,Accepted,VisualAllowed=true;
        public static PresentationCommandContext Capture(MatchState state,PlayerSlot player,CommandType type,int kind,int target,double now,MapPoint? position=null,BigInteger upgradeCount=default)
        {
            var c=new PresentationCommandContext{SessionId=state?.SessionId??0,Player=player,Type=type,Kind=kind,Target=target,UpgradePath=(TowerUpgradePath)kind,SubmittedAt=now,SubmittedTick=state?.Tick??0};
            if(type==CommandType.BuildTower){c.Position=position??new MapPoint(0,0);c.SiteId=target;c.ExpectedLevel=0;}
            if(state!=null&&(IsUpgrade(type)||type==CommandType.SellTower))
                for(int i=0;i<state.Towers.Count;i++)
                {
                    var t=state.Towers[i];if(t.Owner!=player||t.Id!=target)continue;
                    c.Slot=t.Slot;c.Entity=t.Id;c.Kind=t.Kind;c.Position=t.Position;c.SiteId=t.SiteId;c.PreviousLevel=t.Level;var count=type==CommandType.UpgradeTowerBatch?upgradeCount:BigInteger.One;c.ExpectedLevel=t.Level+count;
                    c.PreviousDamageLevel=t.DamageLevel;c.PreviousRangeLevel=t.RangeLevel;
                    bool secondary=c.UpgradePath==TowerUpgradePath.Range||c.UpgradePath==TowerUpgradePath.BlastRadius||c.UpgradePath==TowerUpgradePath.JumpRange;
                    c.ExpectedDamageLevel=t.DamageLevel+(IsUpgrade(type)&&!secondary&&c.UpgradePath!=TowerUpgradePath.Income?count:0);
                    c.ExpectedRangeLevel=t.RangeLevel+(IsUpgrade(type)&&secondary?count:0);break;
                }
            return c;
        }
    }

    /// <summary>Joins ACK and authoritative state in either order, with bounded pending work and session-scoped deduplication.</summary>
    public sealed class PresentationCommandTracker
    {
        public const double LifetimeSeconds=4;
        const int MaxPending=128;
        readonly Dictionary<ulong,PresentationCommandContext> requests=new Dictionary<ulong,PresentationCommandContext>();
        readonly HashSet<ulong> resolved=new HashSet<ulong>();
        readonly List<ulong> removals=new List<ulong>(MaxPending);
        public ulong SessionId { get; private set; }
        public int Count=>requests.Count;
        public void Reset(ulong epoch){SessionId=epoch;requests.Clear();resolved.Clear();removals.Clear();}
        public void Register(ulong sequence,PresentationCommandContext context)
        {
            if(sequence==0||context==null||context.SessionId!=SessionId)return;
            // A later local operation on the same pad supersedes an older visual, but does not fake its ACK.
            foreach(var pair in requests)
            {
                var old=pair.Value;
                if((context.Entity>=0&&old.Entity==context.Entity)||(old.Type==CommandType.BuildTower&&SamePoint(old.Position,context.Position)&&context.Type!=CommandType.SendUnit&&context.Type!=CommandType.SetReady&&context.Type!=CommandType.RequestRematch))old.VisualAllowed=false;
            }
            if(requests.Count>=MaxPending){ulong oldest=ulong.MaxValue;foreach(var key in requests.Keys)if(key<oldest)oldest=key;requests.Remove(oldest);}
            context.Sequence=sequence;requests[sequence]=context;
        }
        public bool Resolve(CommandResult result,out PresentationCommandContext context)
        {
            context=null;if(!requests.TryGetValue(result.Sequence,out var found)||found.SessionId!=SessionId||!resolved.Add(result.Sequence))return false;
            found.Resolved=true;found.Accepted=result.Accepted;found.AckTick=result.Tick;context=found;
            if(!result.Accepted||!found.VisualAllowed||(found.Type!=CommandType.BuildTower&&!PresentationCommandContext.IsUpgrade(found.Type)))requests.Remove(result.Sequence);
            return true;
        }
        public bool IsBuildReserved(MapPoint point,int siteId,int minimumDistance)
        {
            foreach(var pair in requests)
            {
                var c=pair.Value;if(c.Type!=CommandType.BuildTower||(c.Resolved&&!c.Accepted))continue;
                long dx=(long)c.Position.X-point.X,dy=(long)c.Position.Y-point.Y;
                if((siteId>=0&&c.SiteId==siteId)||dx*dx+dy*dy<(long)minimumDistance*minimumDistance)return true;
            }
            return false;
        }
        public void CopyPendingBuilds(List<PresentationCommandContext> destination)
        {destination.Clear();foreach(var pair in requests){var c=pair.Value;if(c.Type==CommandType.BuildTower&&c.VisualAllowed&&(!c.Resolved||c.Accepted))destination.Add(c);}}
        public bool HasPendingUpgrade(int entity)
        {foreach(var pair in requests){var c=pair.Value;if(PresentationCommandContext.IsUpgrade(c.Type)&&c.Entity==entity&&(!c.Resolved||c.Accepted))return true;}return false;}
        public bool HasPending(CommandType type,int kind=-1)
        {foreach(var pair in requests){var c=pair.Value;if(c.Type==type&&(kind<0||c.Kind==kind)&&!c.Resolved)return true;}return false;}
        static bool SamePoint(MapPoint a,MapPoint b)=>a.X==b.X&&a.Y==b.Y;
        public void DiscardVisuals()
        {
            removals.Clear();foreach(var pair in requests){pair.Value.VisualAllowed=false;if(pair.Value.Resolved)removals.Add(pair.Key);}
            for(int i=0;i<removals.Count;i++)requests.Remove(removals[i]);removals.Clear();
        }
        public void Observe(MatchState state,double now,List<PresentationCommandContext> ready)
        {
            ready.Clear();if(state==null||state.SessionId!=SessionId){DiscardVisuals();return;}
            removals.Clear();
            foreach(var pair in requests)
            {
                var c=pair.Value;
                if(now-c.SubmittedAt>LifetimeSeconds||state.Phase!=MatchPhase.Playing){removals.Add(pair.Key);continue;}
                if(!c.VisualAllowed||(c.Type!=CommandType.BuildTower&&!PresentationCommandContext.IsUpgrade(c.Type))||state.Tick<c.SubmittedTick)continue;
                TowerState tower=null;
                for(int i=0;i<state.Towers.Count;i++){var t=state.Towers[i];if(t.Owner==c.Player&&(c.Type==CommandType.BuildTower?SamePoint(t.Position,c.Position):t.Id==c.Entity)){tower=t;break;}}
                bool matching=tower!=null&&tower.Kind==c.Kind&&tower.Level==c.ExpectedLevel&&tower.DamageLevel==c.ExpectedDamageLevel&&tower.RangeLevel==c.ExpectedRangeLevel&&SamePoint(tower.Position,c.Position)&&tower.SiteId==c.SiteId&&(c.Type==CommandType.BuildTower||tower.Id==c.Entity);
                if(matching)
                {
                    if(c.ObservedEntity>=0&&c.ObservedEntity!=tower.Id)c.VisualAllowed=false;
                    else c.ObservedEntity=tower.Id;
                }
                else if(c.ObservedEntity>=0)c.VisualAllowed=false;
                // A newer level/identity is already present: do not celebrate an earlier or replaced building.
                if(tower!=null&&(tower.Kind!=c.Kind||(PresentationCommandContext.IsUpgrade(c.Type)&&tower.Id!=c.Entity)||tower.Level>c.ExpectedLevel))c.VisualAllowed=false;
                if(tower!=null&&tower.Level==c.ExpectedLevel&&(tower.DamageLevel!=c.ExpectedDamageLevel||tower.RangeLevel!=c.ExpectedRangeLevel))c.VisualAllowed=false;
                if(c.Resolved&&c.Accepted&&state.Tick>=c.AckTick)
                {
                    if(matching&&c.VisualAllowed){c.Entity=tower.Id;c.Slot=tower.Slot;ready.Add(c);removals.Add(pair.Key);}
                    else if(!c.VisualAllowed)removals.Add(pair.Key);
                }
            }
            for(int i=0;i<removals.Count;i++)requests.Remove(removals[i]);removals.Clear();
        }
    }
}
