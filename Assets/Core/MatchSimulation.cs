using System;
using System.Numerics;
using System.Collections.Generic;

namespace DefenseDuell.Core
{
    /// <summary>Single-owner authoritative rules. Presentation never calls Step on a client replica.</summary>
    public sealed partial class MatchSimulation
    {
        private const int ReceiptCapacity = 128;
        private Dictionary<ulong,Receipt>[] receipts;
        private Queue<ulong>[] receiptOrder;
        private ulong[] lastSequence;
        private int[] rateWindow,rateCount;
        private readonly List<UnitState> hitTargets = new List<UnitState>(12);
        private readonly TowerDefinition[] towers;
        private readonly UnitDefinition[] units;
        private int nextEntityId = 1;
        public GameContent Content { get; private set; }
        public MatchState State { get; private set; }

        private struct Receipt { public CommandEnvelope Command; public CommandResult Result; }

        public MatchSimulation(GameContent content, uint seed = 1, ulong sessionId = 1, bool guestConnected = true)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            content.Validate();
            towers = content.Towers;
            units = content.Units;
            if (sessionId == 0) throw new ArgumentOutOfRangeException(nameof(sessionId), "Session zero is reserved.");
            Reset(seed, sessionId, guestConnected);
        }

        public bool ConfigureTowerAvailability(PlayerSlot player,int mask)
        {
            if(State.Phase!=MatchPhase.Lobby||!State.ValidPlayer(player)||!TowerLoadoutRules.IsValid(mask,towers.Length))return false;
            if(State.Players[(int)player].AvailableTowerMask==mask)return true;
            if(State.RosterRevision==ulong.MaxValue)return false;
            State.Players[(int)player].AvailableTowerMask=mask;State.RosterRevision++;ClearLobbyReady();return true;
        }
        public bool ConfigureConnection(PlayerSlot player,bool connected)
        {
            if(State.Phase!=MatchPhase.Lobby||!State.ValidPlayer(player))return false;
            int bit=1<<(int)player,mask=connected?State.ConnectedPlayerMask|bit:State.ConnectedPlayerMask&~bit;
            if(mask==State.ConnectedPlayerMask)return true;
            if(State.RosterRevision==ulong.MaxValue)return false;
            State.ConnectedPlayerMask=mask;State.RosterRevision++;ClearLobbyReady();return true;
        }
        public bool ConfigureGuestConnection(bool connected)=>ConfigureConnection(PlayerSlot.One,connected);
        private void ClearLobbyReady(){foreach(var p in State.Players)p.Ready=false;}
        private bool BalancedRoster()
        {
            if(State.ConnectedPlayerMask!=(1<<State.Players.Length)-1)return false;
            var counts=new int[State.Teams.Length];foreach(var p in State.Players){if(p.TeamId<0||p.TeamId>=counts.Length)return false;counts[p.TeamId]++;}
            foreach(int count in counts)if(count!=Content.MatchRules.MembersPerTeam)return false;return true;
        }
        private bool AllPlayers(Func<PlayerState,bool> predicate){foreach(var p in State.Players)if(!predicate(p))return false;return true;}
        private void InitializeTargets()
        {for(int p=0;p<State.Players.Length;p++)for(int t=0;t<State.Players.Length;t++)if(State.CanTarget((PlayerSlot)p,(PlayerSlot)t)){State.Players[p].AttackTarget=(PlayerSlot)t;break;}}
        public UnitStats GetUnitStats(UnitState unit) => UnitResearchRules.Resolve(Content,unit.Kind,unit.ResearchLevel,unit.Origin,unit.WaveId,unit.NeutralRecipeIndex);
        public ulong LastSequence(PlayerSlot player) => State.ValidPlayer(player) ? lastSequence[(int)player] : 0;

        public CommandResult Execute(PlayerSlot sender, CommandEnvelope command)
        {
            if (!State.ValidPlayer(sender)) return Result(command, ErrorCode.InvalidPlayer);
            int p = (int)sender;
            if (command.SessionId != State.SessionId) return Result(command, ErrorCode.WrongSession);
            if (command.Sequence == 0) return Result(command, ErrorCode.InvalidValue);
            if (receipts[p].TryGetValue(command.Sequence, out Receipt receipt))
                return SameRequest(receipt.Command, command) ? receipt.Result : Result(command, ErrorCode.Duplicate);
            if (command.Sequence <= lastSequence[p]) return Result(command, ErrorCode.StaleSequence);

            // Only transport metadata is consumed for a rejected intent; no canonical match data changes.
            lastSequence[p] = command.Sequence;
            ErrorCode error;
            if (State.Phase == MatchPhase.Playing && !AllowRate(p)) error = ErrorCode.RateLimited;
            else error = ValidateAndApply(sender, command);
            CommandResult result = Result(command, error);
            if (State.SessionId == command.SessionId)
            {
                if (receiptOrder[p].Count >= ReceiptCapacity) receipts[p].Remove(receiptOrder[p].Dequeue());
                receipts[p].Add(command.Sequence, new Receipt { Command = command, Result = result });
                receiptOrder[p].Enqueue(command.Sequence);
            }
            return result;
        }

        private bool AllowRate(int player)
        {
            int window = State.Tick / Content.TickRate;
            if (window != rateWindow[player]) { rateWindow[player] = window; rateCount[player] = 0; }
            return ++rateCount[player] <= Content.Rules.MaxCommandsPerSecond;
        }

        private ErrorCode ValidateAndApply(PlayerSlot sender,CommandEnvelope c)
        {
            if((int)c.Type<0||(int)c.Type>(int)CommandType.UpgradeTowerBatch)return ErrorCode.InvalidValue;
            if(c.Type!=CommandType.UpgradeTowerBatch&&(c.UpgradeCount!=0||c.ExpectedTowerLevel!=0||c.UpgradePriceLimit!=0))return ErrorCode.InvalidValue;
            if(c.Type==CommandType.UpgradeTowerBatch&&(c.UpgradeCount<=0||c.ExpectedTowerLevel<0||c.UpgradePriceLimit<=0))return ErrorCode.InvalidValue;
            bool choice=c.Type==CommandType.SetTeam||c.Type==CommandType.SetLoadout;
            if(c.Type!=CommandType.SendUnit&&c.Type!=CommandType.ResearchUnit&&c.Type!=CommandType.SetWaveReady&&(c.ExpectedWave!=0||c.ExpectedResearch!=0))return ErrorCode.InvalidValue;
            if(c.Type!=CommandType.BuildTower&&(c.PositionX!=0||c.PositionY!=0))return ErrorCode.InvalidValue;
            if(c.Type!=CommandType.SendUnit&&c.EnemyTarget!=0)return ErrorCode.InvalidValue;
            if(c.Type!=CommandType.ApplyMatchRules&&(c.RuleWaveCount!=0||c.RuleStartGold!=0||c.RuleEndless||c.RuleIncomeMultiplier!=0||c.RuleFormat!=0||!string.IsNullOrEmpty(c.RuleMapId)))return ErrorCode.InvalidValue;
            if(c.Type!=CommandType.SetReady&&c.Type!=CommandType.ApplyMatchRules&&!choice&&(c.ExpectedConfigRevision!=0||c.ExpectedRosterRevision!=0))return ErrorCode.InvalidValue;
            var player=State.Players[(int)sender];
            if(c.Type==CommandType.ApplyMatchRules)return ApplyMatchRules(sender,c);
            if(choice)
            {
                if(State.Phase!=MatchPhase.Lobby||State.StartGoldGranted)return ErrorCode.WrongPhase;
                if(c.Target!=-1)return ErrorCode.InvalidTarget;
                if(c.ExpectedConfigRevision!=State.ConfigRevision||c.ExpectedRosterRevision!=State.RosterRevision)return ErrorCode.StaleLobbyRevision;
                if((State.ConnectedPlayerMask&(1<<(int)sender))==0)return ErrorCode.MissingPlayers;
                if(c.Type==CommandType.SetLoadout)
                {
                    if(!TowerLoadoutRules.IsValid(c.Kind,towers.Length))return ErrorCode.InvalidValue;
                    if(c.Kind==player.AvailableTowerMask)return ErrorCode.None;
                    if(State.RosterRevision==ulong.MaxValue)return ErrorCode.RevisionExhausted;
                    ConfigureTowerAvailability(sender,c.Kind);return ErrorCode.None;
                }
                if(c.Kind<0||c.Kind>=State.Teams.Length)return ErrorCode.InvalidValue;
                if(c.Kind==player.TeamId)return ErrorCode.None;
                if(State.RosterRevision==ulong.MaxValue)return ErrorCode.RevisionExhausted;
                player.TeamId=c.Kind;State.BindTeamHealth();State.RosterRevision++;ClearLobbyReady();InitializeTargets();return ErrorCode.None;
            }
            if(c.Type==CommandType.SetReady)
            {
                if(State.Phase!=MatchPhase.Lobby||State.StartGoldGranted)return ErrorCode.WrongPhase;
                if((c.Kind!=0&&c.Kind!=1)||c.Target!=-1)return ErrorCode.InvalidValue;
                if(c.ExpectedConfigRevision!=State.ConfigRevision||c.ExpectedRosterRevision!=State.RosterRevision)return ErrorCode.StaleLobbyRevision;
                if((State.ConnectedPlayerMask&(1<<(int)sender))==0||(c.Kind==0&&!BalancedRoster()))return ErrorCode.MissingPlayers;
                player.Ready=c.Kind==0;
                if(AllPlayers(p=>p.Ready))
                {
                    foreach(var p in State.Players)p.Gold=Content.MatchStartGold;
                    State.StartGoldGranted=true;InitializeTargets();State.CountdownRemaining=Content.CountdownTicks;
                    State.Phase=Content.CountdownTicks==0?MatchPhase.Playing:MatchPhase.Countdown;
                    if(State.Phase==MatchPhase.Playing)PrepareWave();
                }
                return ErrorCode.None;
            }
            if(c.Type==CommandType.RequestRematch)
            {
                if(State.Phase!=MatchPhase.Finished)return ErrorCode.WrongPhase;
                if(c.Kind!=0||c.Target!=-1)return ErrorCode.InvalidValue;
                if(State.SessionId==ulong.MaxValue)return ErrorCode.RevisionExhausted;
                player.Rematch=true;
                if(AllPlayers(p=>p.Rematch))
                {
                    var prior=State.Players;int connected=State.ConnectedPlayerMask;
                    Reset(unchecked(State.Seed+1),State.SessionId+1,false);State.ConnectedPlayerMask=connected;
                    for(int i=0;i<prior.Length;i++){State.Players[i].AvailableTowerMask=prior[i].AvailableTowerMask;State.Players[i].TeamId=prior[i].TeamId;}
                    State.BindTeamHealth();InitializeTargets();
                }
                return ErrorCode.None;
            }
            if(State.Phase!=MatchPhase.Playing)return ErrorCode.WrongPhase;
            if(!State.IsAlive(sender))return ErrorCode.NotOwner;
            switch(c.Type)
            {
                case CommandType.BuildTower:return Build(sender,c.Kind,c.Target,new MapPoint(c.PositionX,c.PositionY));
                case CommandType.UpgradeTower:return Upgrade(sender,c.Target,c.Kind);
                case CommandType.UpgradeTowerBatch:return UpgradeBatch(sender,c);
                case CommandType.SellTower:return c.Kind==0?Sell(sender,c.Target):ErrorCode.InvalidKind;
                case CommandType.SendUnit:return Send(sender,c);
                case CommandType.SetAttackTarget:
                    if(c.Target!=-1||c.Kind<0||c.Kind>=State.Players.Length||!State.CanTarget(sender,(PlayerSlot)c.Kind))return ErrorCode.InvalidTarget;
                    player.AttackTarget=(PlayerSlot)c.Kind;return ErrorCode.None;
                case CommandType.SetWaveReady:
                    if(State.WavePhase!=WavePhase.Preparing)return ErrorCode.WrongPhase;
                    if(c.Kind!=0&&c.Kind!=1)return ErrorCode.InvalidKind;
                    if(c.ExpectedResearch!=0)return ErrorCode.InvalidValue;
                    BigInteger readyWave=c.ExpectedWave!=0?c.ExpectedWave:c.Target;
                    if(c.ExpectedWave!=0&&c.Target!=-1)return ErrorCode.InvalidTarget;
                    if(!Content.ContainsWave(readyWave))return ErrorCode.InvalidValue;
                    if(readyWave!=State.WaveId)return ErrorCode.QuoteChanged;
                    player.WaveReady=c.Kind==1;return ErrorCode.None;
                case CommandType.ResearchUnit:
                    if(c.Kind<0||c.Kind>=units.Length)return ErrorCode.InvalidKind;
                    if(c.Target!=-1||c.ExpectedWave!=0||c.ExpectedResearch<0)return ErrorCode.InvalidValue;
                    if(c.ExpectedResearch!=player.ResearchLevels[c.Kind])return ErrorCode.QuoteChanged;
                    if(!Content.MatchRules.Endless&&State.WavePhase==WavePhase.Clearing&&State.WaveId==Content.WaveCount)return ErrorCode.WaveCapacity;
                    var research=UnitResearchRules.Quote(Content,player,c.Kind);
                    if(!research.IsValid)return research.Code;
                    if(player.Gold<research.Price)return ErrorCode.InsufficientGold;
                    player.Gold-=research.Price;player.ResearchLevels[c.Kind]=research.NextLevel;return ErrorCode.None;
                case CommandType.SetTowerPriority:
                    var target=FindTower(c.Target);if(target==null)return ErrorCode.InvalidTarget;
                    if(target.Owner!=sender)return ErrorCode.NotOwner;
                    if(towers[target.Kind].IsEconomy||(c.Kind!=0&&c.Kind!=1))return ErrorCode.InvalidKind;
                    target.Priority=(TargetPriority)c.Kind;return ErrorCode.None;
                case CommandType.SetSpeedVote:
                    if(c.Kind!=0&&c.Kind!=1)return ErrorCode.InvalidKind;
                    if(c.Target!=-1)return ErrorCode.InvalidTarget;
                    player.SpeedReady=c.Kind==1;return ErrorCode.None;
                default:return ErrorCode.InvalidValue;
            }
        }
        private ErrorCode ApplyMatchRules(PlayerSlot sender,CommandEnvelope command)
        {
            if(sender!=PlayerSlot.Zero)return ErrorCode.NotOwner;
            if(State.Phase!=MatchPhase.Lobby||State.StartGoldGranted)return ErrorCode.WrongPhase;
            if(command.Kind!=0||command.Target!=-1||command.ExpectedRosterRevision!=0)return ErrorCode.InvalidValue;
            if(command.ExpectedConfigRevision!=State.ConfigRevision)return ErrorCode.StaleLobbyRevision;
            var requested=new MatchRules(command.RuleWaveCount,command.RuleStartGold,command.RuleEndless,
                command.RuleIncomeMultiplier==0?1:command.RuleIncomeMultiplier,command.RuleFormat,
                string.IsNullOrEmpty(command.RuleMapId)?Content.Map.Id:command.RuleMapId);
            var error=MatchRulesPolicy.Validate(Content.BaseContent,requested);if(error!=ErrorCode.None)return error;
            if(Content.MatchRules.Equals(requested))return ErrorCode.None;
            bool formatChanged=requested.Format!=Content.MatchRules.Format;
            if((State.ConnectedPlayerMask&~((1<<requested.PlayerCount)-1))!=0)return ErrorCode.MissingPlayers;
            if(State.ConfigRevision==ulong.MaxValue||(formatChanged&&State.RosterRevision==ulong.MaxValue))return ErrorCode.RevisionExhausted;
            var effective=Content.WithMatchRules(requested);var oldPlayers=State.Players;
            var players=new PlayerState[requested.PlayerCount];var teams=CreateTeams(effective);
            for(int i=0;i<players.Length;i++)
            {
                players[i]=NewPlayer(effective,i);
                if(i<oldPlayers.Length){players[i].AvailableTowerMask=oldPlayers[i].AvailableTowerMask;if(!formatChanged)players[i].TeamId=oldPlayers[i].TeamId;}
            }
            Content=effective;State.MatchRules=effective.MatchRules;State.Players=players;State.Teams=teams;State.BindTeamHealth();State.ConfigRevision++;
            if(formatChanged){State.RosterRevision++;ResizeTransport(players.Length,false);ResizeLanes(players.Length);}
            ClearLobbyReady();InitializeTargets();return ErrorCode.None;
        }

        private ErrorCode Build(PlayerSlot sender, int kind, int target, MapPoint requested)
        {
            ErrorCode result = PlacementValidator.ValidateBuild(Content, State, sender, kind, target, requested, out TowerPlacement placement);
            if (result != ErrorCode.None) return result;
            PlayerState player = State.Players[(int)sender];
            int price = towers[kind].Price;
            int id = NextEntityId(); // Fail technical ID exhaustion before spending. No gameplay count limit.
            player.Gold -= price;
            State.Towers.Add(new TowerState { Id = id, Owner = sender, Slot = placement.Slot,
                SiteId = placement.SiteId, Position = placement.Position, Kind = kind, Invested = price });
            return ErrorCode.None;
        }

        private ErrorCode Upgrade(PlayerSlot sender, int id, int kind)
        {
            TowerState tower = FindTower(id);
            if (tower == null) return ErrorCode.InvalidTarget;
            if (tower.Owner != sender) return ErrorCode.NotOwner;
            if (kind < (int)TowerUpgradePath.Damage || kind > (int)TowerUpgradePath.JumpRange) return ErrorCode.InvalidKind;
            UpgradeQuote quote = TowerUpgradeRules.Quote(Content, tower, (TowerUpgradePath)kind);
            if (!quote.IsValid) return quote.Code;
            PlayerState player = State.Players[(int)sender];
            if (player.Gold < quote.Price) return ErrorCode.InsufficientGold;
            player.Gold -= quote.Price;
            tower.Invested += quote.Price;
            tower.Level = quote.NextLevel;
            tower.DamageLevel = quote.NextDamageLevel;
            tower.RangeLevel = quote.NextRangeLevel;
            // Preserve time until next shot: repeatedly upgrading cannot manufacture immediate shots.
            return ErrorCode.None;
        }

        private ErrorCode Sell(PlayerSlot sender, int id)
        {
            TowerState tower = FindTower(id);
            if (tower == null) return ErrorCode.InvalidTarget;
            if (tower.Owner != sender) return ErrorCode.NotOwner;
            PlayerState player = State.Players[(int)sender];
            player.Gold += RefundFor(tower);
            State.Towers.Remove(tower);
            return ErrorCode.None;
        }

        private ErrorCode Send(PlayerSlot sender, CommandEnvelope command)
        {
            if (command.Kind < 0 || command.Kind >= units.Length) return ErrorCode.InvalidKind;
            if (command.Target != -1 || command.EnemyTarget<0 || command.EnemyTarget>=State.Players.Length || !State.CanTarget(sender,(PlayerSlot)command.EnemyTarget)) return ErrorCode.InvalidTarget;
            if (!Content.ContainsWave(command.ExpectedWave) || command.ExpectedResearch < 0) return ErrorCode.InvalidValue;
            SendQuote quote = SendRules.Quote(Content,State,sender,command.Kind,command.EnemyTarget);
            if (quote.Code == ErrorCode.WaveCapacity) return quote.Code;
            if (quote.WaveId != command.ExpectedWave || quote.ResearchLevel != command.ExpectedResearch) return ErrorCode.QuoteChanged;
            if (!quote.IsValid) return quote.Code;
            PlayerState player = State.Players[(int)sender]; UnitDefinition unit = units[command.Kind];
            player.Gold -= quote.Price; player.ReservedGoldByWave[quote.WaveId-1] += quote.Price;
            State.SpawnOrders.Add(new SpawnOrder { Sender=sender, Kind=command.Kind, Remaining=unit.Count,
                InitialCount=unit.Count, PaidPrice=quote.Price, ResearchLevel=quote.ResearchLevel,
                WaveId=quote.WaveId, EnemyTarget=(PlayerSlot)quote.EnemyTarget, NextTick=State.PlayTicks, Origin=UnitOrigin.PaidPvP });
            return ErrorCode.None;
        }

        public TowerState FindTower(int id)
        {
            for (int i = 0; i < State.Towers.Count; i++) if (State.Towers[i].Id == id) return State.Towers[i];
            return null;
        }
        public int PendingOrders(PlayerSlot sender)
        {
            int count = 0;
            for (int i = 0; i < State.SpawnOrders.Count; i++) if (State.SpawnOrders[i].Sender == sender) count++;
            return count;
        }
        public BigInteger RefundFor(TowerState tower) => EconomyRules.RefundGold(Content,tower);
        public BigInteger TowerDamage(TowerState t) => TowerUpgradeRules.Stats(Content, t).Damage;
        public int TowerRange(TowerState t) => TowerUpgradeRules.Stats(Content, t).Range;
        public int TowerInterval(TowerState t) => TowerUpgradeRules.Stats(Content, t).FireIntervalTicks;
        public BigInteger TowerIncome(TowerState t) => TowerUpgradeRules.Stats(Content, t).IncomePerSecond;
        public static BigInteger DamageAfterArmor(BigInteger damage, int armor, int armorPiercing) => BigInteger.Max(1, damage - Math.Max(0, armor - armorPiercing));

        public void Step() => Step(null);
        public void Step(Action<MatchSimulation> beforeGameplayStep)
        {
            if (State.Phase == MatchPhase.Finished || State.Phase == MatchPhase.Aborted) return;
            State.Tick=checked(State.Tick+1);
            if (State.Phase == MatchPhase.Countdown)
            {
                if (--State.CountdownRemaining <= 0)
                { State.CountdownRemaining=0; State.Phase=MatchPhase.Playing; PrepareWave(); }
                return;
            }
            if (State.Phase != MatchPhase.Playing) return;
            if (State.WavePhase == WavePhase.Preparing)
            {
                if (--State.PreparationRemaining <= 0 || State.AllLiving(p=>p.WaveReady))
                {
                    State.PreparationRemaining=0; State.WavePhase=WavePhase.Active; State.WaveStartPlayTick=State.PlayTicks;
                    foreach(var p in State.Players)p.WaveReady=false;
                }
                return;
            }
            int steps=State.SimulationSpeed;
            for (int i=0;i<steps;i++)
            {
                beforeGameplayStep?.Invoke(this);
                if (State.Phase != MatchPhase.Playing || State.WavePhase == WavePhase.Preparing) break;
                State.PlayTicks=checked(State.PlayTicks+1);
                if (State.PlayTicks % Content.TickRate == 0)
                    for(int p=0;p<State.Players.Length;p++)
                    {
                        PlayerState player=State.Players[p];
                        if(player.Hp<=0)continue;
                        BigInteger total=EconomyRules.IncomeUnits(Content,State,(PlayerSlot)p)+player.GoldRemainder;
                        player.Gold+=total/Content.WaveRules.IncomeUnitsPerGold;
                        player.GoldRemainder=total%Content.WaveRules.IncomeUnitsPerGold;
                    }
                Spawn(); Combat(); MoveAndResolve(); ResolveOutcome();
                if (State.Phase != MatchPhase.Playing || State.WavePhase == WavePhase.Preparing) break;
            }
        }
        private void PrepareWave()
        {
            State.WavePhase=WavePhase.Preparing; State.PreparationRemaining=Content.WaveRules.PreparationTicks;
            State.WaveStartPlayTick=State.PlayTicks;
            for(int p=0;p<State.Players.Length;p++){State.Players[p].WaveReady=false;State.NeutralCursor[p]=0;}
        }
        private SpawnOrder Head(PlayerSlot sender)
        {foreach(var order in State.SpawnOrders)if(order.Sender==sender)return order;return null;}
        private void Spawn()
        {
            var wave=Content.GetWave(State.WaveId);
            for(int defender=0;defender<State.Players.Length;defender++)
            {
                if(!State.IsAlive((PlayerSlot)defender)||State.PlayTicks<State.NextSpawnTicks[defender])continue;
                int cursor=State.NeutralCursor[defender];
                bool neutral=cursor<wave.TotalCount&&State.PlayTicks>=checked(State.WaveStartPlayTick+cursor*Content.WaveRules.NeutralSpawnIntervalTicks);
                SpawnOrder head=null;int sender=-1;
                for(int offset=0;offset<State.Players.Length;offset++)
                {
                    int candidate=(State.NextPaidSender[defender]+offset)%State.Players.Length;
                    var order=Head((PlayerSlot)candidate);
                    if(order==null||(int)order.EnemyTarget!=defender||order.WaveId!=State.WaveId||order.NextTick>State.PlayTicks||State.NextPaidSpawnTicks[candidate]>State.PlayTicks)continue;
                    if(order.Remaining==order.InitialCount&&State.Players[candidate].NextSendTicks[order.Kind]>State.PlayTicks)continue;
                    head=order;sender=candidate;break;
                }
                bool paid=head!=null;if(!neutral&&!paid)continue;
                var origin=neutral&&(!paid||State.NextSpawnOrigins[defender]==UnitOrigin.NeutralWave)?UnitOrigin.NeutralWave:UnitOrigin.PaidPvP;
                int id=NextEntityId(),kind;BigInteger level;
                if(origin==UnitOrigin.NeutralWave){kind=wave.PlanData[cursor];level=0;State.NeutralCursor[defender]++;sender=defender;}
                else
                {
                    kind=head.Kind;level=head.ResearchLevel;
                    if(head.Remaining==head.InitialCount)State.Players[sender].NextSendTicks[kind]=checked(State.PlayTicks+units[kind].CooldownTicks);
                    var credit=EconomyRules.SpawnCreditUnits(head,head.InitialCount-head.Remaining+1);
                    State.Players[sender].SpawnedQuarterGoldByWave[State.WaveId-1]+=credit;
                    State.Players[sender].AttackIncomeUnits=(int)BigInteger.Min(Content.WaveRules.MaxAttackIncomeUnits,State.Players[sender].AttackIncomeUnits+credit);
                    head.Remaining--;head.NextTick=checked(State.PlayTicks+Content.Rules.SpawnIntervalTicks);
                    State.NextPaidSpawnTicks[sender]=head.NextTick;State.NextPaidSender[defender]=(sender+1)%State.Players.Length;
                    if(head.Remaining==0)State.SpawnOrders.Remove(head);
                }
                int recipeIndex=origin==UnitOrigin.NeutralWave?cursor:-1;
                var stats=UnitResearchRules.Resolve(Content,kind,level,origin,State.WaveId,recipeIndex);
                State.Units.Add(new UnitState{Id=id,Kind=kind,Hp=stats.MaxHp,Defender=(PlayerSlot)defender,Sender=(PlayerSlot)sender,Origin=origin,WaveId=State.WaveId,ResearchLevel=level,NeutralRecipeIndex=recipeIndex});
                State.NextSpawnTicks[defender]=checked(State.PlayTicks+Content.Rules.SpawnIntervalTicks);
                State.NextSpawnOrigins[defender]=origin==UnitOrigin.NeutralWave?UnitOrigin.PaidPvP:UnitOrigin.NeutralWave;
            }
        }

        private void Combat()
        {
            for (int t = 0; t < State.Towers.Count; t++)
            {
                TowerState tower = State.Towers[t];
                if (towers[tower.Kind].IsEconomy || !State.IsAlive(tower.Owner)) continue;
                if (tower.CooldownTicks > 0) tower.CooldownTicks--;
                if (tower.CooldownTicks > 0) continue;
                UnitState target = null;
                var stats = TowerUpgradeRules.Stats(Content,tower);
                int range = stats.Range;
                long rangeSquared = (long)range * range;
                MapPoint origin = tower.Position;
                for (int u = 0; u < State.Units.Count; u++)
                {
                    UnitState unit = State.Units[u];
                    if (unit.Defender != tower.Owner || unit.Hp <= 0) continue;
                    if (DistanceSquared(origin, Content.Map.PositionAt(unit.Distance)) > rangeSquared) continue;
                    if (target == null || (tower.Priority == TargetPriority.Strongest && unit.Hp > target.Hp)
                        || ((tower.Priority == TargetPriority.Front || unit.Hp == target.Hp)
                            && (unit.Distance > target.Distance || (unit.Distance == target.Distance && unit.Id < target.Id))))
                        target = unit;
                }
                if (target == null) continue;
                TowerDefinition definition = towers[tower.Kind];
                BigInteger damage = stats.Damage;
                tower.CooldownTicks = stats.FireIntervalTicks;
                if (definition.Mechanic == WeaponMechanic.Chain || definition.Mechanic == WeaponMechanic.PierceLine)
                { MultiHit(tower,target,definition,stats); }
                else if (stats.SplashRadius > 0)
                {
                    MapPoint impact = Content.Map.PositionAt(target.Distance);
                    long radiusSquared = (long)stats.SplashRadius * stats.SplashRadius;
                    for (int u = 0; u < State.Units.Count; u++)
                    {
                        UnitState unit = State.Units[u];
                        if (unit.Defender == tower.Owner && unit.Hp > 0
                            && DistanceSquared(impact, Content.Map.PositionAt(unit.Distance)) <= radiusSquared)
                            Hit(unit, stats, damage);
                    }
                }
                else Hit(target, stats, damage);
            }
        }

        private void MultiHit(TowerState tower, UnitState primary, TowerDefinition definition, TowerStats stats)
        {
            hitTargets.Clear();hitTargets.Add(primary);
            MapPoint start=tower.Position, primaryPoint=Content.Map.PositionAt(primary.Distance);
            long dx=(long)primaryPoint.X-start.X,dy=(long)primaryPoint.Y-start.Y,length=dx*dx+dy*dy;
            long previousProjection=length, rangeSquared=(long)stats.Range*stats.Range;
            UnitState current=primary;
            for(int hit=0;hit<stats.MaxTargets;hit++)
            {
                Hit(current,stats,BigInteger.Max(1,stats.Damage*definition.DamagePermilleData[Math.Min(hit,definition.DamagePermilleData.Length-1)]/1000));
                if(hit+1==stats.MaxTargets)break;
                UnitState next=null;long best=long.MaxValue;
                MapPoint from=Content.Map.PositionAt(current.Distance);
                foreach(var candidate in State.Units)
                {
                    if(candidate.Defender!=tower.Owner || candidate.Hp<=0)continue;
                    bool seen=false;for(int h=0;h<=hit;h++)if(hitTargets[h]==candidate){seen=true;break;}if(seen)continue;
                    MapPoint point=Content.Map.PositionAt(candidate.Distance);long metric;
                    if(definition.Mechanic==WeaponMechanic.Chain)
                    {
                        metric=DistanceSquared(from,point);
                        if(metric>(long)stats.JumpRange*stats.JumpRange)continue;
                    }
                    else
                    {
                        if(length==0 || DistanceSquared(start,point)>rangeSquared)continue;
                        long x=(long)point.X-start.X,y=(long)point.Y-start.Y;
                        metric=x*dx+y*dy;long cross=x*dy-y*dx;
                        if(metric<=length || metric<previousProjection || cross*cross>(long)stats.LineHalfWidth*stats.LineHalfWidth*length)continue;
                    }
                    if(metric<best || (metric==best && (next==null || candidate.Id<next.Id))){next=candidate;best=metric;}
                }
                if(next==null)break;
                current=next;hitTargets.Add(next);if(definition.Mechanic==WeaponMechanic.PierceLine)previousProjection=best;
            }
            hitTargets.Clear();
        }

        private void Hit(UnitState target, TowerStats tower, BigInteger damage)
        {
            target.Hp = BigInteger.Max(0, target.Hp - DamageAfterArmor(damage, units[target.Kind].Armor, tower.ArmorPiercing));
            if (target.Hp <= 0 || tower.SlowPermille >= 1000 || tower.SlowDurationTicks <= 0) return;
            int current = target.SlowUntilTick > State.PlayTicks ? target.SlowPermille : 1000;
            target.SlowPermille = Math.Max(Content.Rules.MinimumSlowPermille, Math.Min(current, tower.SlowPermille));
            target.SlowUntilTick = BigInteger.Max(target.SlowUntilTick, State.PlayTicks + tower.SlowDurationTicks);
        }

        private void MoveAndResolve()
        {
            for (int i = State.Units.Count - 1; i >= 0; i--)
            {
                UnitState unit = State.Units[i];
                if (unit.Hp <= 0)
                {
                    PlayerState defender = State.Players[(int)unit.Defender];
                    defender.Gold += GetUnitStats(unit).KillBounty;
                    State.Units.RemoveAt(i);
                    continue;
                }
                if (unit.SlowUntilTick <= State.PlayTicks)
                { unit.SlowPermille = 1000; unit.SlowUntilTick = 0; }
                int denominator = Content.TickRate * 1000;
                int movement = GetUnitStats(unit).SpeedPerSecond * unit.SlowPermille + unit.MovementRemainder;
                unit.Distance += movement / denominator;
                unit.MovementRemainder = movement % denominator;
                if (unit.Distance < Content.Map.Length) continue;
                PlayerState player = State.Players[(int)unit.Defender];
                player.Hp = Math.Max(0, player.Hp - units[unit.Kind].BaseDamage);
                State.Units.RemoveAt(i);
            }
        }

        private void RetireDefeatedTargets()
        {
            for(int i=State.SpawnOrders.Count-1;i>=0;i--)
            {
                var order=State.SpawnOrders[i];if(State.IsAlive(order.EnemyTarget))continue;
                var player=State.Players[(int)order.Sender];
                BigInteger quarters=4*order.PaidPrice-4*order.PaidPrice*(order.InitialCount-order.Remaining)/order.InitialCount;
                player.CancelledQuarterGoldByWave[order.WaveId-1]+=quarters;
                BigInteger refundUnits=quarters*240+player.GoldRemainder;
                player.Gold+=refundUnits/960;player.GoldRemainder=refundUnits%960;
                State.SpawnOrders.RemoveAt(i);
            }
            for(int i=State.Units.Count-1;i>=0;i--)if(!State.IsAlive(State.Units[i].Defender))State.Units.RemoveAt(i);
            for(int p=0;p<State.Players.Length;p++)if(!State.IsAlive((PlayerSlot)p))State.NeutralCursor[p]=Content.GetWave(State.WaveId).TotalCount;
        }
        private void ResolveOutcome()
        {
            RetireDefeatedTargets();int alive=0;foreach(var team in State.Teams)if(team.Hp>0)alive++;
            if(alive<=1){Finish();return;}
            var wave=Content.GetWave(State.WaveId);
            if(State.WavePhase==WavePhase.Active&&State.PlayTicks-State.WaveStartPlayTick>=Content.WaveRules.SendWindowTicks(wave))State.WavePhase=WavePhase.Clearing;
            if(State.WavePhase!=WavePhase.Clearing||State.Units.Count!=0)return;
            for(int p=0;p<State.Players.Length;p++)if(State.IsAlive((PlayerSlot)p)&&State.NeutralCursor[p]<wave.TotalCount)return;
            foreach(var order in State.SpawnOrders)if(order.WaveId==State.WaveId)return;
            if(!Content.MatchRules.Endless&&State.WaveId==Content.WaveCount){Finish();return;}
            if(Content.MatchRules.Endless)foreach(var player in State.Players)
            {
                player.ReservedGoldByWave.Advance();player.CancelledQuarterGoldByWave.Advance();
                player.ArchivedSpawnedQuarterGold+=player.SpawnedQuarterGoldByWave.Advance();
            }
            State.WaveId++;PrepareWave();
        }
        private void Finish()
        {
            int highest=-1,winner=-1;bool tie=false;
            for(int t=0;t<State.Teams.Length;t++){int hp=State.Teams[t].Hp;if(hp>highest){highest=hp;winner=t;tie=false;}else if(hp==highest)tie=true;}
            State.WinningTeamId=tie?-1:winner;
            State.Outcome=tie?MatchOutcome.Draw:winner==0?MatchOutcome.Player0:winner==1?MatchOutcome.Player1:MatchOutcome.Team2;
            State.Phase=MatchPhase.Finished;
        }

        public void Abort()
        {
            if (State.Phase == MatchPhase.Finished || State.Phase == MatchPhase.Aborted) return;
            State.Outcome = MatchOutcome.Aborted;
            State.Phase = MatchPhase.Aborted;
        }

        private static TeamState[] CreateTeams(GameContent content)
        {var result=new TeamState[content.MatchRules.TeamCount];for(int i=0;i<result.Length;i++)result[i]=new TeamState{Hp=checked(content.BaseHp*content.MatchRules.MembersPerTeam)};return result;}
        private void ResizeLanes(int count)
        {State.NeutralCursor=new int[count];State.NextSpawnTicks=new int[count];State.NextSpawnOrigins=new UnitOrigin[count];State.NextPaidSender=new int[count];State.NextPaidSpawnTicks=new int[count];}
        private void ResizeTransport(int count,bool clear)
        {
            var priorReceipts=receipts;var priorOrder=receiptOrder;var priorSequence=lastSequence;var priorWindow=rateWindow;var priorCount=rateCount;
            receipts=new Dictionary<ulong,Receipt>[count];receiptOrder=new Queue<ulong>[count];lastSequence=new ulong[count];rateWindow=new int[count];rateCount=new int[count];
            for(int p=0;p<count;p++)
            {
                bool keep=!clear&&priorReceipts!=null&&p<priorReceipts.Length;
                receipts[p]=keep?priorReceipts[p]:new Dictionary<ulong,Receipt>(ReceiptCapacity);receiptOrder[p]=keep?priorOrder[p]:new Queue<ulong>(ReceiptCapacity);
                lastSequence[p]=keep?priorSequence[p]:0;rateWindow[p]=keep?priorWindow[p]:-1;rateCount[p]=keep?priorCount[p]:0;
            }
        }
        private void Reset(uint seed,ulong sessionId,bool guestConnected)
        {
            int count=Content.MatchRules.PlayerCount;
            State=new MatchState{SessionId=sessionId,Seed=seed,Phase=MatchPhase.Lobby,MatchRules=Content.MatchRules,
                ConnectedPlayerMask=guestConnected?(1<<count)-1:1,Players=new PlayerState[count],Teams=CreateTeams(Content)};
            for(int p=0;p<count;p++)State.Players[p]=NewPlayer(Content,p);
            State.BindTeamHealth();ResizeLanes(count);InitializeTargets();ResizeTransport(count,true);nextEntityId=1;
        }
        private int NextEntityId(){int id=nextEntityId;nextEntityId=checked(nextEntityId+1);return id;}
        private static PlayerState NewPlayer(GameContent content,int slot)=>new PlayerState{Gold=0,Hp=content.BaseHp*content.MatchRules.MembersPerTeam,
            TeamId=slot/content.MatchRules.MembersPerTeam,AvailableTowerMask=TowerLoadoutRules.Normalize(TowerLoadoutRules.DefaultMask,content.TowerData.Length),NextSendTicks=new int[content.UnitData.Length],
            ReservedGoldByWave=WaveLedger.Create(content),SpawnedQuarterGoldByWave=WaveLedger.Create(content),CancelledQuarterGoldByWave=WaveLedger.Create(content)};
        private CommandResult Result(CommandEnvelope c,ErrorCode error)=>new CommandResult{Sequence=c.Sequence,Code=error,Tick=State.Tick};
        private static bool SameRequest(CommandEnvelope a,CommandEnvelope b)=>a.SessionId==b.SessionId&&a.Sequence==b.Sequence&&a.Type==b.Type&&a.Kind==b.Kind&&a.Target==b.Target
            &&a.PositionX==b.PositionX&&a.PositionY==b.PositionY&&a.ExpectedWave==b.ExpectedWave&&a.ExpectedResearch==b.ExpectedResearch&&a.EnemyTarget==b.EnemyTarget
            &&a.ExpectedConfigRevision==b.ExpectedConfigRevision&&a.ExpectedRosterRevision==b.ExpectedRosterRevision&&a.RuleWaveCount==b.RuleWaveCount&&a.RuleStartGold==b.RuleStartGold
            &&a.RuleEndless==b.RuleEndless&&a.RuleIncomeMultiplier==b.RuleIncomeMultiplier&&a.RuleFormat==b.RuleFormat&&a.RuleMapId==b.RuleMapId
            &&a.UpgradeCount==b.UpgradeCount&&a.ExpectedTowerLevel==b.ExpectedTowerLevel&&a.UpgradePriceLimit==b.UpgradePriceLimit;
        private static long DistanceSquared(MapPoint a, MapPoint b)
        { long dx = a.X - b.X, dy = a.Y - b.Y; return dx * dx + dy * dy; }
    }
}
