using System;
using System.Numerics;
using System.Collections.Generic;

namespace DefenseDuell.Core
{
    public enum PlayerSlot : byte { Zero, One, Two, Three, Four, Five }
    public enum MatchPhase { Lobby, Countdown, Playing, Finished, Aborted }
    public enum MatchOutcome { None, Player0, Player1, Draw, Aborted, Team2 }
    public enum CommandType { BuildTower, UpgradeTower, SellTower, SendUnit, SetReady, RequestRematch, SetSpeedVote, SetWaveReady, ResearchUnit, SetTowerPriority, ApplyMatchRules, SetTeam, SetLoadout, SetAttackTarget, UpgradeTowerBatch }
    public enum WavePhase : byte { Preparing, Active, Clearing }
    public enum UnitOrigin : byte { NeutralWave, PaidPvP }
    public enum TargetPriority : byte { Front, Strongest }
    public enum WeaponMechanic : byte { Standard, Chain, PierceLine, Beam }
    public enum ErrorCode
    {
        None, WrongSession, Duplicate, StaleSequence, InvalidPlayer, WrongPhase,
        InvalidKind, InvalidTarget, NotOwner, Occupied, InsufficientGold, MaxLevel,
        Cooldown, QueueFull, RateLimited, InvalidValue, ClassLimit,
        OutsideMap, OnPath, BlockedArea, TooClose, QuoteChanged, WaveCapacity, TowerLocked,
        StaleLobbyRevision, MissingPlayers, RulesPayloadTooLarge, UnsupportedWaveCount, RevisionExhausted
    }

    [Serializable]
    public struct CommandEnvelope
    {
        public ulong SessionId, Sequence;
        public CommandType Type;
        public int Kind, Target, PositionX, PositionY;
        public BigInteger ExpectedWave;
        public int EnemyTarget;
        public BigInteger ExpectedResearch;
        public ulong ExpectedConfigRevision, ExpectedRosterRevision;
        public int RuleWaveCount;
        public BigInteger RuleStartGold;
        public bool RuleEndless;
        public int RuleIncomeMultiplier;
        public MatchFormat RuleFormat;
        public string RuleMapId;
        public BigInteger UpgradeCount, ExpectedTowerLevel, UpgradePriceLimit;
    }

    [Serializable]
    public struct CommandResult
    {
        public ulong Sequence;
        public ErrorCode Code;
        public int Tick;
        public bool Accepted => Code == ErrorCode.None;
    }

    [Serializable]
    public sealed class MatchState
    {
        public ulong SessionId;
        public uint Seed;
        public MatchRules MatchRules;
        public ulong ConfigRevision, RosterRevision;
        public int ConnectedPlayerMask = 3;
        public bool StartGoldGranted;
        public int Tick, PlayTicks, CountdownRemaining;
        public MatchPhase Phase;
        public MatchOutcome Outcome;
        public int WinningTeamId = -1;
        public TeamState[] Teams;
        public PlayerState[] Players = new PlayerState[2];
        public List<TowerState> Towers = new List<TowerState>(64);
        public List<UnitState> Units = new List<UnitState>(160);
        public List<SpawnOrder> SpawnOrders = new List<SpawnOrder>(160);
        public WavePhase WavePhase;
        public BigInteger WaveId = 1;
        public int PreparationRemaining, WaveStartPlayTick;
        public int[] NeutralCursor = new int[2], NextSpawnTicks = new int[2];
        public UnitOrigin[] NextSpawnOrigins = new UnitOrigin[2];
        public int[] NextPaidSender = new int[2], NextPaidSpawnTicks = new int[2];
        // Step is always a control tick. Only active inner gameplay steps use this multiplier.
        public bool ValidPlayer(PlayerSlot player)=>(int)player>=0&&(int)player<Players.Length;
        public bool IsAlive(PlayerSlot player)=>ValidPlayer(player)&&Players[(int)player].Hp>0;
        public bool CanTarget(PlayerSlot sender,PlayerSlot target)=>IsAlive(sender)&&IsAlive(target)&&Players[(int)sender].TeamId!=Players[(int)target].TeamId;
        public bool AllLiving(Func<PlayerState,bool> predicate)
        {bool any=false;foreach(var p in Players)if(p!=null&&p.Hp>0){any=true;if(!predicate(p))return false;}return any;}
        public void BindTeamHealth()
        {if(Teams==null)return;foreach(var p in Players){if(p==null||p.TeamId<0||p.TeamId>=Teams.Length)throw new ArgumentException("Invalid team binding");p.BindTeam(Teams[p.TeamId]);}}
        public int SimulationSpeed => Phase==MatchPhase.Playing&&(WavePhase==WavePhase.Active||WavePhase==WavePhase.Clearing)&&AllLiving(p=>p.SpeedReady)?2:1;

    }

    [Serializable]
    public sealed class TeamState { public int Hp; }

    [Serializable]
    public sealed class PlayerState
    {
        public BigInteger Gold;
        private int hp;
        private TeamState team;
        public int TeamId;
        public int Hp { get=>team==null?hp:team.Hp; set {if(team==null)hp=value;else team.Hp=value;} }
        internal void BindTeam(TeamState value){team=value;}
        public PlayerSlot AttackTarget=PlayerSlot.One;
        public bool Ready, Rematch;
        public bool SpeedReady;
        public bool WaveReady;
        public int AvailableTowerMask = 255;
        public BigInteger[] ResearchLevels = new BigInteger[GameContent.UnitClassCount];
        public int AttackIncomeUnits;
        public BigInteger GoldRemainder;
        public WaveLedger ReservedGoldByWave = new BigInteger[10], SpawnedQuarterGoldByWave = new BigInteger[10], CancelledQuarterGoldByWave = new BigInteger[10];
        public BigInteger ArchivedSpawnedQuarterGold;
        // Earliest first-spawn tick for the next paid group of each class, not a purchase lock.
        public int[] NextSendTicks = new int[GameContent.UnitClassCount];
    }

    [Serializable]
    public sealed class TowerState
    {
        public int Id, Slot, Kind, CooldownTicks;
        public BigInteger Level, Invested, DamageLevel, RangeLevel;
        public int SiteId = -1;
        public MapPoint Position;
        public PlayerSlot Owner;
        public TargetPriority Priority;
    }

    [Serializable]
    public sealed class UnitState
    {
        public int Id, Kind, Distance;
        public int NeutralRecipeIndex = -1;
        public BigInteger Hp, SlowUntilTick;
        public int SlowPermille = 1000;
        public PlayerSlot Defender;
        public PlayerSlot Sender;
        public UnitOrigin Origin = UnitOrigin.PaidPvP;
        public BigInteger WaveId = 1;
        public BigInteger ResearchLevel;
        // Fractional movement is retained; low-speed units must not lose speed to rounding.
        public int MovementRemainder;
    }

    [Serializable]
    public sealed class SpawnOrder
    {
        public int Kind, Remaining, NextTick;
        public PlayerSlot Sender;
        public UnitOrigin Origin = UnitOrigin.PaidPvP;
        public BigInteger ResearchLevel, PaidPrice;
        public int InitialCount;
        public BigInteger WaveId = 1;
        public PlayerSlot EnemyTarget=PlayerSlot.One;
    }

    [Serializable]
    public struct MapPoint
    {
        public int X, Y;
        public MapPoint(int x, int y) { X = x; Y = y; }
    }
}
