using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using DefenseDuell.Core;
using UnityEngine;

namespace DefenseDuell.Networking
{
    /// <summary>Explicit opt-in acceptance driver. All actions use the production command path and rules.</summary>
    public sealed class NetworkTestDriver : MonoBehaviour
    {
        [Serializable] public sealed class Checkpoint { public ulong epoch; public int tick; public string hash, publicationSequence; }
        [Serializable] public sealed class Frame
        {
            public string name, bytes, hash, publicationSequence;
            public double wall;
            public int tick, playTicks, phase; public string wave;
            public List<TowerSample> towers = new List<TowerSample>();
            public ulong epoch;
        }
        [Serializable] public sealed class NeutralRecipeEvidence
        {
            public string wave,profileId,pressureBudgetHp; public int[] plan; public string[] hpPerKind; public bool boss,superBoss;public int bossIndex;
        }
        public static void CaptureNeutralRecipe(GameContent content,BigInteger wave,List<NeutralRecipeEvidence> rows)
        {
            string id=wave.ToString();foreach(var row in rows)if(row.wave==id)return;
            var recipe=content.GetWave(wave);var hp=new string[4];for(int k=0;k<4;k++)hp[k]=recipe.HpForKind(k).ToString();
            rows.Add(new NeutralRecipeEvidence{wave=id,profileId=recipe.ProfileId,pressureBudgetHp=recipe.PressureBudgetHp.ToString(),plan=recipe.Plan,hpPerKind=hp,boss=recipe.IsBossWave,superBoss=recipe.IsSuperBossWave,bossIndex=recipe.BossUnitIndex});
        }
        [Serializable] public sealed class TowerSample { public int id, range; public string damage, incomeUnits; }
        [Serializable] public sealed class TowerProfile { public int price, damage, range, fireIntervalTicks; public int[] upgradePrices, upgradeDamage, upgradeRange; }
        [Serializable] public sealed class Ack
        {
            public string label, publicationSequence;
            public ulong epoch, sequence;
            public int code, tick;
            public bool diagnostic;
        }
        [Serializable] public sealed class CommandProof {
            public ulong SessionId,Sequence; public int Type,Kind,Target,PositionX,PositionY,EnemyTarget,RuleIncomeMultiplier,RuleFormat; public bool RuleEndless;public string RuleMapId,ExpectedWave,ExpectedResearch,ExpectedConfigRevision,ExpectedRosterRevision,RuleStartGold; public int RuleWaveCount; public string UpgradeCount,ExpectedTowerLevel,UpgradePriceLimit;
            public CommandProof(CommandEnvelope c) { SessionId=c.SessionId;Sequence=c.Sequence;Type=(int)c.Type;Kind=c.Kind;Target=c.Target;PositionX=c.PositionX;PositionY=c.PositionY;ExpectedWave=c.ExpectedWave.ToString();EnemyTarget=c.EnemyTarget;RuleEndless=c.RuleEndless;RuleIncomeMultiplier=c.RuleIncomeMultiplier;RuleFormat=(int)c.RuleFormat;RuleMapId=c.RuleMapId;ExpectedResearch=c.ExpectedResearch.ToString();ExpectedConfigRevision=c.ExpectedConfigRevision.ToString();ExpectedRosterRevision=c.ExpectedRosterRevision.ToString();RuleWaveCount=c.RuleWaveCount;RuleStartGold=c.RuleStartGold.ToString();UpgradeCount=c.UpgradeCount.ToString();ExpectedTowerLevel=c.ExpectedTowerLevel.ToString();UpgradePriceLimit=c.UpgradePriceLimit.ToString(); }
        }
        [Serializable] public sealed class AuthorityProof
        {
            public int player;
            public CommandProof command;
            public CommandResult result;
            public string before, after;
        }
        [Serializable] public sealed class ClockWindow
        {
            public string name;
            public bool complete;
            public string wave; public int speed, controlStart, controlEnd, playStart, playEnd;
            public double began, seconds, controlsPerSecond, gameplayPerSecond;
        }
        [Serializable] public sealed class RoundEvidence
        {
            public int number, stage, initialMask0, initialMask1, resultTick, playTicks, resultHp0, resultHp1; public string resultWave;
            public ulong epoch;
            public bool freshResources, scenarioPassed, resultObserved, rematchRequested, rematchAccepted, startOnlyObserved;
            public double startedSmokeSeconds;
            public bool automaticPreparation, frozenPreparation, earlyStart, origins, futurePacket, rapidPassed, mineRebuilt;
            public int mineSoldId,mineRebuiltId; public string mineRefundGold;
            public int rapidRequested, rapidAccepted, rapidGoldRejected, rapidBeforeGold, rapidBeforeOrders;
            public string resultDigest, outcome;
            public double began, resultAt;
            public List<Frame> frames = new List<Frame>();
            public List<Ack> acks = new List<Ack>();
            public List<AuthorityProof> authority = new List<AuthorityProof>();
            public List<ClockWindow> clocks = new List<ClockWindow>();
        }
        [Serializable] public sealed class CycleEvidence
        {
            public int cycle, initialMapIndex, mapIndex;
            public bool connected, cleanShutdown, disconnectObserved, relayAllocationEstablished;
            public string finalContentHash, catalogHash, authenticationProfile, anonymousPlayerFingerprint, failure = "";
            public long sentBytes, receivedBytes;
            public List<Checkpoint> checkpoints = new List<Checkpoint>();
            public List<RoundEvidence> rounds = new List<RoundEvidence>();
        }
        [Serializable] public sealed class Evidence
        {
            public string role, unityVersion, platform, contentHash, finalContentHash, catalogHash, startedUtc, endedUtc, mode;
            public string expectedStartGold, configuredStartGold, baseContentHash, wavePlanVersion=MatchRulesPolicy.WavePlanVersion;
            public bool hostRules, hostRulesStartOnly, ruleEndless; public int configuredWaves,incomeMultiplier,matchFormat; public string mapId;
            public int protocol = WireProtocol.Version, requestedCycles, requestedMapIndex, mapIndex, expectedBaseHp, tickRate, waveCount, preparationTicks, waveBudget;
            public int[] unitPrices, unitCounts, researchPrices, towerPrices, refundPercents, unitCooldownTicks, sendWindowTicksByWave;
            public int acceptanceSchema=3,passiveGoldPerSecond,incomeUnitsPerGold,spawnIntervalTicks;
            public string rulesVersion=GameContent.SimulationRulesVersion;
            public TowerProfile[] towerProfiles;
            public List<NeutralRecipeEvidence> neutralRecipes=new List<NeutralRecipeEvidence>();
            public bool passed, completeMatches, rapidBuy, speedVote = true;
            public string test = "Actual NGO transport; host command before/after states and peer ACK/state evidence. No device or separate-internet claim.";
            public List<CycleEvidence> cycles = new List<CycleEvidence>();
        }
        [Serializable] private sealed class RelayCode { public int cycle, mapIndex; public string joinCode, catalogHash, createdUtc; }
        private sealed class Intent { public string label; public CommandEnvelope command; public ErrorCode expected; }
        public static bool Requested => HasArg("--duel-host") || HasArg("--duel-client") || HasArg("--duel-relay-host") || HasArg("--duel-relay-client");
        public static string AuthenticationProfile => HasArg("--duel-relay-host") ? "duel_relay_host" : HasArg("--duel-relay-client") ? "duel_relay_client" : null;
        SessionCoordinator session;
        Evidence evidence;
        CycleEvidence cycle;
        RoundEvidence round;
        string reportPath, address, codePath, suppliedCode;
        ushort port;
        bool host, relay, completeMatches, rapidBuy, waitingForCode, submittedReady, finishing;
        int requestedMap, stage, combatId, mineId;
        double began, restartAt = -1, nextCodePoll, stageAt, mixedAt = -1, autoAt;
        bool hostRules, hostRulesStartOnly;
        double startGrantedAt=-1;
        int configuredWaves, rulesStage;
        BigInteger configuredGold;
        ulong initialConfigRevision, initialRosterRevision, defensePublicationBarrier;
        bool exactReplay, mutatedReplay, speedApproved, speedWithdrawn, oldVoteReplay, waveReadySent;
        CommandEnvelope buildCommand, approveCommand, mineSaleCommand;
        MapPoint mineRebuildPosition;
        ClockWindow clockWindow;
        readonly Dictionary<ulong, Intent> pending = new Dictionary<ulong, Intent>();
        readonly Dictionary<string, Ack> latest = new Dictionary<string, Ack>();
        MatchState State => session.State;
        PlayerState Own => State.Players[(int)session.LocalSlot];
        double Now => Time.realtimeSinceStartupAsDouble;

        public void Initialize(SessionCoordinator coordinator)
        {
            session = coordinator; relay = HasArg("--duel-relay-host") || HasArg("--duel-relay-client");
            host = HasArg("--duel-host") || HasArg("--duel-relay-host"); completeMatches = HasArg("--duel-complete-matches"); rapidBuy = HasArg("--duel-rapid-buy");
            hostRulesStartOnly=HasArg("--duel-host-rules-start-only");
            hostRules=hostRulesStartOnly || HasArg("--duel-host-rules");
            configuredWaves=int.TryParse(Arg("--duel-waves","1"),out var waves)?waves:0;
            if(MatchRulesPolicy.TryParseStartGold(Arg("--duel-start-gold","180"),out configuredGold)!=ErrorCode.None
                || MatchRulesPolicy.Validate(session.Content,new MatchRules(configuredWaves,configuredGold))!=ErrorCode.None)
                throw new ArgumentException("Invalid exact host-rules driver options.");
            if(hostRules)completeMatches=!hostRulesStartOnly;
            if(hostRulesStartOnly)rapidBuy=false;
            port = ushort.TryParse(Arg("--duel-port", "7777"), out var parsed) ? parsed : (ushort)7777;
            address = Arg("--duel-address", "127.0.0.1");
            reportPath = Arg("--duel-report", Path.Combine(Application.persistentDataPath, "network-v7", (host ? "host" : "client") + ".json"));
            codePath = Arg("--duel-code-file", Path.Combine(Path.GetDirectoryName(Path.GetFullPath(reportPath)), "relay-code.json"));
            suppliedCode = Arg("--duel-code", "").Trim().ToUpperInvariant();
            requestedMap = host && int.TryParse(Arg("--duel-map", "0"), out var map) ? map : 0;
            var cycles = int.TryParse(Arg("--duel-cycles", "1"), out var count) ? Math.Max(1, Math.Min(20, count)) : 1;
            evidence = new Evidence { role = host ? "host" : "client", unityVersion = Application.unityVersion, platform = Application.platform.ToString(),
                contentHash = session.Content.ContentHash, catalogHash = session.CatalogHash, startedUtc = DateTime.UtcNow.ToString("O"), requestedCycles = cycles,
                requestedMapIndex = requestedMap, mode = relay ? "relay-dtls" : "lan", completeMatches = completeMatches, rapidBuy = rapidBuy,
                expectedStartGold = session.Content.MatchStartGold.ToString(), hostRules=hostRules,hostRulesStartOnly=hostRulesStartOnly,configuredWaves=configuredWaves,configuredStartGold=configuredGold.ToString(),baseContentHash=session.Content.BaseContentHash, expectedBaseHp = session.Content.BaseHp, tickRate = session.Content.TickRate,
                waveCount = session.Content.WaveCount, preparationTicks = session.Content.WaveRules.PreparationTicks, waveBudget = session.Content.WaveRules.WaveBudget,
                unitPrices = new int[session.Content.Units.Length], unitCounts = new int[session.Content.Units.Length], researchPrices = new int[session.Content.Units.Length], towerPrices = new int[session.Content.Towers.Length], towerProfiles = new TowerProfile[session.Content.Towers.Length],
                refundPercents=new int[session.Content.Towers.Length],unitCooldownTicks=new int[session.Content.Units.Length],sendWindowTicksByWave=new int[session.Content.WaveCount],
                passiveGoldPerSecond=session.Content.PassiveGoldPerSecond,incomeUnitsPerGold=session.Content.WaveRules.IncomeUnitsPerGold,spawnIntervalTicks=session.Content.Rules.SpawnIntervalTicks };
            for(int w=0;w<evidence.sendWindowTicksByWave.Length;w++)evidence.sendWindowTicksByWave[w]=session.Content.WaveRules.SendWindowTicks(session.Content.GetWave(w+1));
            for (var k = 0; k < session.Content.Units.Length; k++) { evidence.unitCooldownTicks[k]=session.Content.Units[k].CooldownTicks; evidence.unitPrices[k] = session.Content.Units[k].Price; evidence.unitCounts[k] = session.Content.Units[k].Count; evidence.researchPrices[k] = session.Content.Research[k].BasePrice; }
            for (var k = 0; k < evidence.towerPrices.Length; k++)
            {
                var tower = session.Content.Towers[k]; var ups = tower.Upgrades;
                evidence.towerPrices[k] = tower.Price; evidence.refundPercents[k]=EconomyRules.RefundPercent(session.Content,k);
                evidence.towerProfiles[k] = new TowerProfile { price = tower.Price, damage = tower.Damage, range = tower.Range, fireIntervalTicks=tower.FireIntervalTicks,
                    upgradePrices = new[] { (int)TowerUpgradeRules.Price(session.Content,k,1), (int)TowerUpgradeRules.Price(session.Content,k,2) },
                    upgradeDamage = new[] { (int)TowerUpgradeRules.Stats(session.Content,new TowerState { Kind=k,Level=1,DamageLevel=k==4?0:1 }).Damage, (int)TowerUpgradeRules.Stats(session.Content,new TowerState { Kind=k,Level=2,DamageLevel=k==4?0:2 }).Damage },
                    upgradeRange = new[] { TowerUpgradeRules.Stats(session.Content,new TowerState { Kind=k,Level=1,RangeLevel=k==4?0:1 }).Range, TowerUpgradeRules.Stats(session.Content,new TowerState { Kind=k,Level=2,RangeLevel=k==4?0:2 }).Range } };
            }
            session.CommandResolved += OnResolved; session.DiagnosticAcknowledgement += OnDiagnostic; session.SnapshotObserved += OnSnapshot;
            if (host) session.AuthoritativeCommandObserved += OnAuthority;
            StartCycle();
        }
        void StartCycle()
        {
            cycle = new CycleEvidence { cycle = evidence.cycles.Count + 1 }; evidence.cycles.Add(cycle); round = null;
            began = Now; restartAt = -1; waitingForCode = false; submittedReady = false; finishing = false;
            if (!session.SelectMap(requestedMap) || !session.SetTowerAvailability(host ? 31 : 151)) { Fail("Map or asymmetric unlock mask could not be configured in Idle."); return; }
            cycle.initialMapIndex = session.SelectedMapIndex;
            if (relay) { if (host || suppliedCode.Length > 0) _ = StartRelayCycleAsync(suppliedCode); else { waitingForCode = true; nextCodePoll = 0; } }
            else if (host) session.StartLanHost(port); else session.JoinLan(address, port);
            Persist();
        }
        async Task StartRelayCycleAsync(string code)
        {
            var active = cycle;
            try
            {
                waitingForCode = false;
                if (host) await session.HostOnlineAsync(); else await session.JoinOnlineAsync(code);
                if (cycle != active || cycle == null) return;
                if (!session.RelayAllocationEstablished) { Fail("No actual Relay allocation/join: " + session.Message); return; }
                CaptureConnection();
                if (host)
                {
                    var info = new RelayCode { cycle = cycle.cycle, mapIndex = session.SelectedMapIndex, joinCode = session.JoinCode,
                        catalogHash = session.CatalogHash, createdUtc = DateTime.UtcNow.ToString("O") };
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(codePath))); File.WriteAllText(codePath, JsonUtility.ToJson(info, true));
                }
                Persist();
            }
            catch (Exception error) { if (cycle == active && cycle != null) Fail("Relay driver: " + error.GetType().Name); }
        }
        void PollRelayCode()
        {
            if (!waitingForCode || Now < nextCodePoll) return; nextCodePoll = Now + .25;
            try
            {
                if (!File.Exists(codePath) || new FileInfo(codePath).Length > 2048) return;
                var info = JsonUtility.FromJson<RelayCode>(File.ReadAllText(codePath));
                if (info != null && info.cycle == cycle.cycle && info.catalogHash == session.CatalogHash && !string.IsNullOrEmpty(info.joinCode)) _ = StartRelayCycleAsync(info.joinCode);
            }
            catch (IOException) { } catch (ArgumentException) { }
        }
        void CaptureConnection()
        {
            evidence.ruleEndless=session.Content.MatchRules.Endless;evidence.incomeMultiplier=session.Content.MatchRules.IncomeMultiplier;evidence.matchFormat=(int)session.Content.MatchRules.Format;evidence.mapId=session.Content.Map.Id;
            cycle.mapIndex = session.SelectedMapIndex; cycle.finalContentHash = session.Content.ContentHash; cycle.catalogHash = session.CatalogHash;
            cycle.relayAllocationEstablished = session.RelayAllocationEstablished; cycle.authenticationProfile = session.AuthenticationProfile;
            cycle.anonymousPlayerFingerprint = session.AnonymousPlayerFingerprint;
            evidence.baseContentHash=session.Content.BaseContentHash;
            evidence.waveCount=session.Content.WaveCount;evidence.expectedStartGold=session.Content.MatchStartGold.ToString();
            if(evidence.sendWindowTicksByWave.Length!=session.Content.WaveCount)evidence.sendWindowTicksByWave=new int[session.Content.WaveCount];
            for(int w=0;w<evidence.sendWindowTicksByWave.Length;w++)evidence.sendWindowTicksByWave[w]=session.Content.WaveRules.SendWindowTicks(session.Content.GetWave(w+1));
            evidence.mapIndex = cycle.mapIndex; evidence.contentHash = evidence.finalContentHash = cycle.finalContentHash;
        }
        void ObserveRound()
        {
            if (State == null || session.ConnectedPlayers != 2 || round != null && round.epoch == State.SessionId) return;
            if (round != null)
            {
                if (!completeMatches || !round.resultObserved || !round.rematchRequested || cycle.rounds.Count != 1) { Fail("Unexpected epoch change."); return; }
                round.rematchAccepted = true; // new authoritative epoch proves both validated rematch requests, even if the old ACK arrived late.
            }
            round = new RoundEvidence { number = cycle.rounds.Count + 1, epoch = State.SessionId, began = Now,
                initialMask0 = State.Players[0].AvailableTowerMask, initialMask1 = State.Players[1].AvailableTowerMask };
            round.freshResources = State.WaveId == 1 && State.PlayTicks == 0 && State.Towers.Count == 0 && State.Units.Count == 0 && State.SpawnOrders.Count == 0
                && round.initialMask0 == 31 && round.initialMask1 == 151;
            foreach (var player in State.Players)
            {
                round.freshResources &= player.Gold == 0 && !State.StartGoldGranted && player.Hp == session.Content.BaseHp && !player.SpeedReady && !player.WaveReady
                    && player.AttackIncomeUnits == 0 && player.GoldRemainder == 0;
                foreach (var level in player.ResearchLevels) round.freshResources &= level == 0;
                foreach (var value in player.ReservedGoldByWave) round.freshResources &= value == 0;
                foreach (var value in player.SpawnedQuarterGoldByWave) round.freshResources &= value == 0;
            }
            cycle.rounds.Add(round); Capture("initial");
            if (!round.freshResources) { Fail("Rematch/initial state did not reset resources/research/votes/ledger while retaining 31/151 masks."); return; }
            rulesStage=0;startGrantedAt=-1;defensePublicationBarrier=0;initialConfigRevision=State.ConfigRevision;initialRosterRevision=State.RosterRevision;
            stage = combatId = mineId = 0; stageAt = Now; mixedAt = -1; clockWindow = null; pending.Clear(); latest.Clear();
            exactReplay = mutatedReplay = speedApproved = speedWithdrawn = oldVoteReplay = waveReadySent = submittedReady = false;
            buildCommand = approveCommand = mineSaleCommand = default; mineRebuildPosition=default; Persist();
        }
        // A confirmed result remains Finished when the remote host shuts down. This is a
        // transport terminal state only with zero connected peers, never a new aborted match.
        public static bool ExpectedGuestShutdown(bool host,ConnectionState status,int connectedPlayers,
            bool startOnly,bool scenarioPassed,bool completeMatches,bool bothRoundsFinished)
        {
            if(host||!scenarioPassed)return false;
            if(status==ConnectionState.ConnectionLost)return !completeMatches||bothRoundsFinished;
            return status==ConnectionState.Finished&&connectedPlayers==0&&!startOnly&&completeMatches&&bothRoundsFinished;
        }
        bool BothRoundsFinished()=>cycle!=null&&cycle.rounds.Count==2&&cycle.rounds[0].resultObserved&&cycle.rounds[1].resultObserved;

        void Update()
        {
            if (session == null || evidence == null || !enabled) return;
            if (restartAt >= 0) { if (Now >= restartAt) StartCycle(); return; }
            if (cycle == null || finishing) return;
            PollRelayCode(); if (cycle == null) return;
            if (session.ConnectedPlayers == 2 && State != null) { cycle.connected = true; CaptureConnection(); }
            ObserveRound(); if (cycle == null) return;
            if (session.Status == ConnectionState.ConnectionLost || session.Status==ConnectionState.Finished && session.ConnectedPlayers==0)
            {
                if (ExpectedGuestShutdown(host,session.Status,session.ConnectedPlayers,hostRulesStartOnly,round!=null&&round.scenarioPassed&&(!hostRulesStartOnly||round.startOnlyObserved),completeMatches,BothRoundsFinished())) { cycle.disconnectObserved = true; FinishCycle(); }
                else Fail("Unexpected connection loss: " + session.Message);
                return;
            }
            if (round == null) { if (Now - began > 60) Fail("Connection did not complete: " + session.Message); return; }
            if(State.StartGoldGranted && !HasFrame("start-granted"))
            {
                if(State.Players[0].Gold!=State.MatchRules.StartGold || State.Players[1].Gold!=State.MatchRules.StartGold)
                {Fail("Start grant was not observed exactly before spending.");return;}
                Capture("start-granted");startGrantedAt=Now;
            }
            if(hostRules)
            {
                if(session.Status==ConnectionState.Lobby)HostRulesScenario();
                if(State.StartGoldGranted)round.scenarioPassed=HasFrame("configured") && HasFrame("start-granted");
                if(hostRulesStartOnly)HostRulesStartSmoke();
                else if(State.Phase==MatchPhase.Playing && configuredWaves>1)HostRulesDefense();
            }
            else
            {
                if (!submittedReady && session.Status == ConnectionState.Lobby) { submittedReady = true; Send("lobby-ready", CommandType.SetReady); }
                if (State.Phase == MatchPhase.Playing) Scenario();
            }
            if (cycle == null) return;
            if (!hostRulesStartOnly && State != null && State.Phase == MatchPhase.Finished) FinishedRound();
            if (cycle != null && Now - began > (completeMatches ? 1800 : 360)) Fail("Wave acceptance watchdog expired at stage " + stage + ": " + session.Message);
        }
        void HostRulesStartSmoke()
        {
            if(!State.StartGoldGranted || startGrantedAt<0)return;
            if(State.Phase!=MatchPhase.Countdown && State.Phase!=MatchPhase.Playing)
            {Fail("Start-only smoke did not remain in a started match.");return;}
            if(!round.startOnlyObserved && Now-startGrantedAt>=3)
            {
                if(State.MatchRules.WaveCount!=configuredWaves || State.MatchRules.StartGold!=configuredGold
                    || State.Players[0].ReservedGoldByWave.Length!=configuredWaves || State.Players[1].ReservedGoldByWave.Length!=configuredWaves)
                {Fail("Start-only smoke rules/ledger mismatch.");return;}
                round.startedSmokeSeconds=Now-startGrantedAt;Capture("started-smoke");round.startOnlyObserved=true;Persist();
            }
            // Allow the peer to observe its own full three-second smoke before the orderly leave.
            if(host && round.startOnlyObserved && Now-startGrantedAt>=8)FinishCycle();
        }
        void HostRulesScenario()
        {
            if(pending.Count>0)return;
            int temporaryWaves=configuredWaves==1?2:1;
            if(host)
            {
                switch(rulesStage)
                {
                    case 0:SendRulesProbe("rules-invalid",0,configuredGold,State.ConfigRevision,ErrorCode.UnsupportedWaveCount);rulesStage++;break;
                    case 1:
                        if(!State.Players[1].Ready)return;
                        SendRulesProbe("rules-noop",State.MatchRules.WaveCount,State.MatchRules.StartGold,State.ConfigRevision);rulesStage++;break;
                    case 2:
                        if(!Accepted("rules-noop"))return;
                        if(!State.Players[1].Ready){Fail("No-op rules invalidated readiness.");return;}
                        Capture("before-apply");SendRulesProbe("rules-temporary",temporaryWaves,configuredGold,State.ConfigRevision);rulesStage++;break;
                    case 3:
                        if(!Accepted("rules-temporary") || !State.Players[1].Ready)return;
                        SendRulesProbe("rules-apply",configuredWaves,configuredGold,State.ConfigRevision);rulesStage++;break;
                    case 4:
                        if(!Accepted("rules-apply") || !State.Players[1].Ready)return;
                        Capture("configured");Send("lobby-ready",CommandType.SetReady);rulesStage++;break;
                }
            }
            else
            {
                switch(rulesStage)
                {
                    case 0:SendRulesProbe("rules-unauthorized",configuredWaves,configuredGold,State.ConfigRevision,ErrorCode.NotOwner);rulesStage++;break;
                    case 1:Send("rules-ready-initial",CommandType.SetReady);rulesStage++;break;
                    case 2:
                        if(State.ConfigRevision<=initialConfigRevision)return;
                        if(State.Players[1].Ready){Fail("Changed rules retained stale readiness.");return;}
                        Send("rules-ready-stale",CommandType.SetReady,expected:ErrorCode.StaleLobbyRevision,
                            configRevision:initialConfigRevision,rosterRevision:initialRosterRevision);rulesStage++;break;
                    case 3:Send("rules-ready-temporary",CommandType.SetReady);rulesStage++;break;
                    case 4:
                        if(State.ConfigRevision<initialConfigRevision+2)return;
                        if(State.Players[1].Ready || State.MatchRules.WaveCount!=configuredWaves || State.MatchRules.StartGold!=configuredGold)
                        {Fail("Final options/readiness mismatch.");return;}
                        Capture("configured");Send("rules-ready-final",CommandType.SetReady);rulesStage++;break;
                }
            }
        }
        void HostRulesDefense()
        {
            if(pending.Count>0 || session.PendingCommands>0 || session.AuthoritativeStateSequence<defensePublicationBarrier || Now<autoAt)return;
            autoAt=Now+.7;
            int count=0;TowerState weakest=null;
            foreach(var tower in State.Towers)if(tower.Owner==session.LocalSlot)
            {count++;if(weakest==null || tower.Level<weakest.Level)weakest=tower;}
            int kind=count%4;
            if(count<4 && Own.Gold>=session.Content.Towers[kind].Price)
            {
                int range=session.Content.Towers[kind].Range;long best=-1;MapPoint chosen=default;
                var map=session.Content.Map;
                for(int y=32;y<map.Height-31;y+=32)for(int x=32;x<map.Width-31;x+=32)
                {
                    var point=new MapPoint(x,y);
                    if(PlacementValidator.ValidatePosition(session.Content,State,session.LocalSlot,kind,-1,point)!=ErrorCode.None)continue;
                    long score=0;
                    for(int distance=0;distance<map.Length;distance+=24)
                    {var path=map.PositionAt(distance);long dx=path.X-x,dy=path.Y-y;if(dx*dx+dy*dy<=(long)range*range)score++;}
                    if(score>best){best=score;chosen=point;}
                }
                if(best>0){Send("automatic-defense-build-"+State.Tick,CommandType.BuildTower,kind,-1,chosen.X,chosen.Y,allowRace:true);return;}
            }
            if(weakest!=null)
            {
                var path=TowerUpgradeRules.PrimaryPath(session.Content,weakest.Kind);
                var quote=TowerUpgradeRules.Quote(session.Content,weakest,path);
                if(quote.IsValid && Own.Gold>=quote.Price)Send("automatic-defense-upgrade-"+State.Tick,CommandType.UpgradeTower,(int)path,weakest.Id,allowRace:true);
            }
        }
        void SendRulesProbe(string label,int waves,BigInteger gold,ulong revision,ErrorCode expected=ErrorCode.None)
            => Send(label,CommandType.ApplyMatchRules,expected:expected,configRevision:revision,ruleWaves:waves,ruleGold:gold);

        void Scenario()
        {
            var state = State;
            if (state.WaveId == 1 && state.WavePhase == WavePhase.Preparing)
            {
                if (!HasFrame("preparation-start")) Capture("preparation-start");
                if (stage >= 5 && Accepted("sell") && FindTower(combatId) == null && !HasFrame("preparation-stable")) Capture("preparation-stable");
                // Retain the latest state actually observed before the transition. A guest's
                // 5-Hz stream may legitimately skip the final four preparation ticks.
                var stable=FindFrame("preparation-stable");
                if(stable!=null&&state.Tick>stable.tick)Capture("preparation-end",replaceLatest:true);
            }
            if (state.WaveId == 1 && state.WavePhase != WavePhase.Preparing && !round.automaticPreparation)
            { round.automaticPreparation = true; Capture("automatic-start"); }
            if (pending.Count > 0) return;
            TowerState combat = FindTower(combatId), mine = FindTower(mineId);
            switch (stage)
            {
                case 0:
                    if (!FindFreePoint(0, out var point)) { Fail("No legal free combat position."); return; }
                    buildCommand = Send("build", CommandType.BuildTower, 0, -1, point.X, point.Y); Advance(); break;
                case 1:
                    combat = FindOwn(0); if (combat == null || !Accepted("build")) return; combatId = combat.Id;
                    if (!exactReplay) { exactReplay = true; session.RepeatCommandForDiagnostics(buildCommand); return; }
                    if (!HasAck("replay", buildCommand.Sequence, ErrorCode.None)) return;
                    if (!mutatedReplay) { mutatedReplay = true; var changed = buildCommand; changed.PositionX++; session.RepeatCommandForDiagnostics(changed); return; }
                    if (!HasAck("replay", buildCommand.Sequence, ErrorCode.Duplicate)) return;
                    Capture("build-replays"); Send("range", CommandType.UpgradeTower, (int)TowerUpgradePath.Range, combat.Id); Advance(); break;
                case 2:
                    if (combat == null || combat.RangeLevel != 1 || !Accepted("range")) return;
                    Send("priority", CommandType.SetTowerPriority, (int)TargetPriority.Strongest, combat.Id); Advance(); break;
                case 3:
                    if (combat == null || combat.Priority != TargetPriority.Strongest || !Accepted("priority")) return;
                    if (Own.Gold < TowerUpgradeRules.Quote(session.Content, combat, TowerUpgradePath.Damage).Price) return;
                    Send("damage", CommandType.UpgradeTower, (int)TowerUpgradePath.Damage, combat.Id); Advance(); break;
                case 4:
                    if (combat == null || combat.DamageLevel != 1 || combat.RangeLevel != 1 || !Accepted("damage")) return;
                    if (mixedAt < 0) { if (!BothMixed()) return; mixedAt = Now; Capture("mixed-paths"); }
                    if (Now - mixedAt < 2.2) return;
                    Send("sell", CommandType.SellTower, 0, combat.Id); Advance(); break;
                case 5:
                    if (FindTower(combatId) != null || !Accepted("sell")) return;
                    if (Own.Gold < session.Content.Towers[4].Price || !FindFreePoint(4, out var minePoint)) return;
                    Capture("sale"); Send("mine", CommandType.BuildTower, 4, -1, minePoint.X, minePoint.Y); Advance(); break;
                case 6:
                    mine = FindOwn(4); if (mine == null || !Accepted("mine")) return;
                    mineId = mine.Id; Capture("free-mine"); Advance(); break;
                case 7:
                    if (state.WavePhase == WavePhase.Preparing) return;
                    if (!SpeedScenario()) return;
                    Advance(); break;
                case 8:
                    if (state.WaveId < 2 || state.WavePhase != WavePhase.Preparing) return;
                    Capture("wave-two-preparation"); Advance(); break;
                case 9:
                    if (state.WavePhase != WavePhase.Preparing) { Fail("Research/rapid scenario missed the genuine preparation window."); return; }
                    if (Now - stageAt < .8) return;
                    Capture("wave-two-frozen"); round.frozenPreparation = true;
                    SendUnit("old-recipe", 0); Advance(); break;
                case 10:
                    if (!Accepted("old-recipe") || !OwnOrder(0, 0)) return;
                    Capture("old-recipe"); Send("research", CommandType.ResearchUnit, 0, expectedResearch: Own.ResearchLevels[0]); Advance(); break;
                case 11:
                    if (!Accepted("research") || Own.ResearchLevels[0] != 1) return;
                    Capture("researched-old-packet");
                    Send("stale-send", CommandType.SendUnit, 0, -1, 0, 0, state.WaveId, 0, ErrorCode.QuoteChanged); Advance(); break;
                case 12:
                    if (!AcceptedCode("stale-send", ErrorCode.QuoteChanged)) return;
                    Send("stale-research", CommandType.ResearchUnit, 0, -1, expected: ErrorCode.QuoteChanged); Advance(); break;
                case 13:
                    if (!AcceptedCode("stale-research", ErrorCode.QuoteChanged)) return;
                    SendUnit("new-recipe", 0); Advance(); break;
                case 14:
                    if (!Accepted("new-recipe") || !OwnOrder(0, 1)) return;
                    Capture("new-recipe");
                    if (rapidBuy)
                    {
                        var quote = SendRules.Quote(session.Content, state, session.LocalSlot, 0);
                        int accepted = checked((int)(Own.Gold / quote.Price)), requests = accepted + 1;
                        if (accepted < 3 || requests > 16)
                        { Fail("Production gold/budget cannot support the required same-frame rapid proof."); return; }
                        round.rapidRequested = requests; round.rapidBeforeGold = checked((int)Own.Gold); round.rapidBeforeOrders = 0;
                        foreach (var queued in state.SpawnOrders) if (queued.Sender == session.LocalSlot) round.rapidBeforeOrders++;
                        Capture("rapid-before");
                        for (var i = 0; i < requests; i++) Send("rapid-" + i, CommandType.SendUnit, 0, -1, 0, 0, quote.WaveId, quote.ResearchLevel,
                            i < accepted ? ErrorCode.None : ErrorCode.InsufficientGold);
                    }
                    Advance(); break;
                case 15:
                    if (rapidBuy)
                    {
                        int accepted = 0, rejected = 0;
                        for (var i = 0; i < round.rapidRequested; i++)
                        { if (!latest.TryGetValue("rapid-" + i, out var ack)) return; if (ack.code == 0) accepted++; else if (ack.code == (int)ErrorCode.InsufficientGold) rejected++; }
                        round.rapidAccepted = accepted; round.rapidGoldRejected = rejected; round.rapidPassed = accepted >= 3 && rejected == 1;
                        if (!round.rapidPassed) { Fail("Rapid requests were not confirmed as expected."); return; }
                        // A reliable ACK can precede its authoritative state. Never treat receipt as a replicated purchase.
                        int observedOrders = 0;
                        foreach (var queued in state.SpawnOrders) if (queued.Sender == session.LocalSlot) observedOrders++;
                        if (state.WavePhase != WavePhase.Preparing) { Fail("Rapid authoritative state did not arrive during preparation."); return; }
                        if (Own.Gold != round.rapidBeforeGold - accepted * session.Content.Units[0].Price
                            || observedOrders != round.rapidBeforeOrders + accepted) return;
                    }
                    Capture("rapid-after"); Advance(); break;
                case 16:
                    if (waveReadySent && Accepted("wave-ready")) { Advance(); break; }
                    if (state.WavePhase != WavePhase.Preparing) { Fail("Early readiness proof missed preparation."); return; }
                    if (host && !waveReadySent) { waveReadySent = true; Send("wave-ready", CommandType.SetWaveReady, 1, -1, expectedWave:state.WaveId); }
                    if (state.Players[0].WaveReady && !state.Players[1].WaveReady)
                    {
                        if (!HasFrame("single-wave-ready")) { Capture("single-wave-ready"); stageAt = Now; }
                        if (!host && Now - stageAt >= 1.1 && !waveReadySent) { waveReadySent = true; Send("wave-ready", CommandType.SetWaveReady, 1, -1, expectedWave:state.WaveId); }
                    }
                    break;
                case 17:
                    if (state.WavePhase == WavePhase.Preparing) return;
                    round.earlyStart = true; Capture("early-start"); Advance(); break;
                case 18:
                    bool neutral = false, oldPaid = false, newPaid = false;
                    foreach (var unit in state.Units)
                    {
                        if (unit.Origin == UnitOrigin.NeutralWave) neutral = true;
                        else if (unit.Defender != session.LocalSlot && unit.Kind == 0) { if (unit.ResearchLevel == 0) oldPaid = true; if (unit.ResearchLevel == 1) newPaid = true; }
                    }
                    if (!neutral || !oldPaid || !newPaid || Own.AttackIncomeUnits == 0) return;
                    round.origins = true; Capture("mixed-origins-and-credit"); Advance(); break;
                case 19:
                    if (state.WavePhase != WavePhase.Clearing) return;
                    var future = SendRules.Quote(session.Content, state, session.LocalSlot, 0);
                    if (!future.IsValid || future.WaveId <= state.WaveId) return;
                    Capture("future-before"); SendUnit("future", 0); Advance(); break;
                case 20:
                    if (!Accepted("future")) return;
                    bool futureQueued = false; foreach (var order in state.SpawnOrders) if (order.Sender == session.LocalSlot && order.WaveId > state.WaveId) futureQueued = true;
                    if (!futureQueued) return;
                    round.futurePacket = true; Capture("future-after"); Advance(); break;
                case 21:
                    if(mine==null){Fail("Mine sale proof lost its owned mine.");return;}
                    var refund=EconomyRules.RefundGold(session.Content,mine);
                    if(Own.Gold+refund<session.Content.Towers[4].Price)return;
                    mineRebuildPosition=mine.Position;round.mineSoldId=mine.Id;round.mineRefundGold=refund.ToString();
                    Capture("mine-sale-before");mineSaleCommand=Send("mine-sale",CommandType.SellTower,0,mine.Id);Advance();break;
                case 22:
                    if(!Accepted("mine-sale")||FindTower(round.mineSoldId)!=null)return;
                    Capture("mine-sale-after");Send("mine-rebuild",CommandType.BuildTower,4,-1,mineRebuildPosition.X,mineRebuildPosition.Y);Advance();break;
                case 23:
                    if(!Accepted("mine-rebuild"))return;
                    var rebuilt=FindOwn(4);if(rebuilt==null||rebuilt.Id==round.mineSoldId)return;
                    mineId=rebuilt.Id;round.mineRebuiltId=rebuilt.Id;Capture("mine-rebuild-after");
                    session.RepeatCommandForDiagnostics(mineSaleCommand);Advance();break;
                case 24:
                    if(!HasAck("replay",mineSaleCommand.Sequence,ErrorCode.None))return;
                    if(FindTower(round.mineRebuiltId)==null){Fail("Replayed old mine sale removed the rebuilt mine.");return;}
                    Capture("mine-sale-replay");round.mineRebuilt=true;
                    round.scenarioPassed = HasFrame("preparation-stable") && HasFrame("preparation-end") && round.mineRebuilt && round.automaticPreparation && round.frozenPreparation && round.earlyStart && round.origins && (!rapidBuy || round.rapidPassed);
                    if (!round.scenarioPassed) { Fail("Scenario evidence incomplete."); return; }
                    Advance(); Persist(); break;
                default:
                    if (!completeMatches)
                    { if (host && Now - stageAt > 3) FinishCycle(); }
                    else if (Now >= autoAt && session.PendingCommands == 0)
                    {
                        autoAt = Now + .7;
                        var quote = SendRules.Quote(session.Content, state, session.LocalSlot, 0);
                        if (quote.IsValid) Send("automatic-" + state.Tick, CommandType.SendUnit, 0, -1, 0, 0, quote.WaveId, quote.ResearchLevel, allowRace: true);
                    }
                    break;
            }
        }
        bool SpeedScenario()
        {
            if (host && !speedApproved) { speedApproved = true; approveCommand = Send("speed-approve", CommandType.SetSpeedVote, 1); }
            if (State.Players[0].SpeedReady && !State.Players[1].SpeedReady && !HasFrame("speed-both"))
            {
                if (SampleClock("speed-one", 1) && !host && !speedApproved)
                { speedApproved = true; approveCommand = Send("speed-approve", CommandType.SetSpeedVote, 1); clockWindow = null; }
            }
            if (cycle == null) return false;
            if (State.SimulationSpeed == 2)
            {
                if (!HasFrame("speed-both")) { Capture("speed-both"); clockWindow = null; }
                if (SampleClock("speed-both", 2) && host && !speedWithdrawn && Now - clockWindow.began > 3)
                { speedWithdrawn = true; Send("speed-withdraw", CommandType.SetSpeedVote, 0); clockWindow = null; }
            }
            if (cycle == null) return false;
            if (!State.Players[0].SpeedReady && State.Players[1].SpeedReady)
            {
                if (!HasFrame("speed-withdrawn")) { Capture("speed-withdrawn"); clockWindow = null; }
                if (host && !oldVoteReplay) { oldVoteReplay = true; session.RepeatCommandForDiagnostics(approveCommand); }
                if (SampleClock("speed-withdrawn", 1))
                {
                    if (host && !HasAck("replay", approveCommand.Sequence, ErrorCode.None)) return false;
                    if (State.Players[0].SpeedReady) { Fail("Old approval replay reactivated a withdrawn vote."); return false; }
                    Capture("speed-complete"); return true;
                }
            }
            return false;
        }
        bool SampleClock(string name, int speed)
        {
            if (clockWindow == null || clockWindow.name != name)
            {
                clockWindow = new ClockWindow { name = name, speed = speed, wave = State.WaveId.ToString(), began = Now, controlStart = State.Tick, playStart = State.PlayTicks };
                round.clocks.Add(clockWindow); Capture(name + "-start");
            }
            if (clockWindow.complete) return true;
            if (Now - clockWindow.began < 2.2) return false;
            clockWindow.controlEnd = State.Tick; clockWindow.playEnd = State.PlayTicks; clockWindow.seconds = Now - clockWindow.began;
            clockWindow.controlsPerSecond = (clockWindow.controlEnd - clockWindow.controlStart) / clockWindow.seconds;
            clockWindow.gameplayPerSecond = (clockWindow.playEnd - clockWindow.playStart) / clockWindow.seconds;
            if (State.WaveId.ToString() != clockWindow.wave || State.WavePhase == WavePhase.Preparing
                || Math.Abs(clockWindow.controlsPerSecond - 20) > 4 || Math.Abs(clockWindow.gameplayPerSecond - 20 * speed) > 5)
            { Fail("Control/gameplay clock window failed: " + name); return false; }
            clockWindow.complete = true; Capture(name + "-end"); return true;
        }
        void FinishedRound()
        {
            if (!round.resultObserved)
            {
                if (!round.scenarioPassed) { Fail("Production match ended before required wave/research/upgrade evidence at stage " + stage); return; }
                if (State.Outcome == MatchOutcome.None || State.Outcome == MatchOutcome.Aborted) { Fail("No normal result."); return; }
                WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(State), session.Content, out var canonical);
                if (canonical == null) { Fail("Final snapshot invalid."); return; }
                canonical.Players[0].Rematch = canonical.Players[1].Rematch = false;
                round.resultDigest = WireProtocol.Digest(WireProtocol.EncodeSnapshot(canonical)); round.resultObserved = true;
                round.outcome = State.Outcome.ToString(); round.resultTick = State.Tick; round.playTicks = State.PlayTicks; round.resultWave = State.WaveId.ToString();
                round.resultHp0 = State.Players[0].Hp; round.resultHp1 = State.Players[1].Hp; round.resultAt = Now; Capture("final"); Persist();
            }
            if (!completeMatches) { if (host && Now - round.resultAt > 2) FinishCycle(); return; }
            if (round.number == 1)
            {
                if (!round.rematchRequested && Now - round.resultAt >= 2)
                { round.rematchRequested = true; Send("rematch", CommandType.RequestRematch); }
            }
            else if (host && Now - round.resultAt > 2.5) FinishCycle();
        }
        CommandEnvelope Send(string label, CommandType type, int kind = 0, int target = -1, int x = 0, int y = 0,
            BigInteger expectedWave = default, BigInteger expectedResearch = default, ErrorCode expected = ErrorCode.None, bool allowRace = false, ulong? configRevision=null,ulong? rosterRevision=null,int ruleWaves=0,BigInteger ruleGold=default)
        {
            if (cycle == null || State == null) return default;
            var epoch = State.SessionId;
            ulong config=configRevision ?? (type==CommandType.SetReady?State.ConfigRevision:0);
            ulong roster=rosterRevision ?? (type==CommandType.SetReady?State.RosterRevision:0);
            var command = new CommandEnvelope { SessionId = epoch, Type = type, Kind = kind, Target = target,
                PositionX = x, PositionY = y, ExpectedWave = expectedWave, ExpectedResearch = expectedResearch,
                ExpectedConfigRevision=config,ExpectedRosterRevision=roster,RuleWaveCount=ruleWaves,RuleStartGold=ruleGold,
                EnemyTarget=type==CommandType.SendUnit?(int)State.Players[(int)session.LocalSlot].AttackTarget:0 };
            ulong sequence=type==CommandType.SetReady ? session.SetLobbyReady(kind==0,epoch,config,roster)
                : type==CommandType.ApplyMatchRules ? session.SubmitForDiagnostics(command)
                : session.Submit(type,kind,target,x,y,expectedWave,expectedResearch,command.EnemyTarget);
            command.Sequence=sequence;
            if (sequence == 0) { Fail("Command not submitted: " + label); return command; }
            var intent = new Intent { label = label, command = command, expected = allowRace ? (ErrorCode)255 : expected };
            if (host) Record(intent, session.LastResult); else pending[sequence] = intent;
            return command;
        }
        void SendUnit(string label, int kind)
        {
            var quote = SendRules.Quote(session.Content, State, session.LocalSlot, kind);
            if (!quote.IsValid) { Fail("Cannot afford production scenario action " + label + ": " + quote.Code); return; }
            Send(label, CommandType.SendUnit, kind, -1, 0, 0, quote.WaveId, quote.ResearchLevel);
        }
        void OnResolved(CommandResult result)
        {
            if (host || round == null || session.LastResultEpoch!=round.epoch || !pending.TryGetValue(result.Sequence, out var intent)) return;
            pending.Remove(result.Sequence); Record(intent, result);
        }
        void Record(Intent intent, CommandResult result)
        {
            if (cycle == null || round == null) return;
            var ack = new Ack { label = intent.label, epoch = intent.command.SessionId, sequence = result.Sequence, code = (int)result.Code, tick = result.Tick,publicationSequence=session.LastResultPublicationSequence.ToString() };
            round.acks.Add(ack); latest[intent.label] = ack;
            if(result.Accepted && intent.label.StartsWith("automatic-defense-",StringComparison.Ordinal))defensePublicationBarrier=session.LastResultPublicationSequence;
            if (result.Code != intent.expected && !(intent.expected == (ErrorCode)255 && (result.Accepted || result.Code == ErrorCode.QuoteChanged || result.Code == ErrorCode.WrongPhase || result.Code == ErrorCode.WaveCapacity || result.Code == ErrorCode.InsufficientGold)))
                Fail("Unexpected ACK for " + intent.label + ": " + result.Code);
            if (intent.label == "rematch" && result.Accepted) round.rematchAccepted = true;
        }
        void OnDiagnostic(ulong epoch, CommandResult result)
        {
            if (round == null || epoch != round.epoch) return;
            round.acks.Add(new Ack { label = "replay", epoch = epoch, sequence = result.Sequence, code = (int)result.Code, tick = result.Tick, diagnostic = true,publicationSequence=session.DiagnosticPublicationSequence.ToString() });
        }
        void OnAuthority(PlayerSlot player, CommandEnvelope command, CommandResult result, byte[] before, byte[] after)
        {
            if (round == null || round.epoch != command.SessionId || round.authority.Count >= 2000) return;
            CaptureNeutralRecipe(session.Content,State.WaveId,evidence.neutralRecipes);
            round.authority.Add(new AuthorityProof { player = (int)player, command = new CommandProof(command), result = result,
                before = Convert.ToBase64String(before), after = Convert.ToBase64String(after) });
        }
        void OnSnapshot(ulong epoch, int tick, string hash)
        {
            if (cycle != null && cycle.checkpoints.Count < 15000) cycle.checkpoints.Add(new Checkpoint { epoch = epoch, tick = tick, hash = hash,publicationSequence=session.AuthoritativeStateSequence.ToString() });
        }
        void Capture(string name,bool replaceLatest=false)
        {
            if (round == null || State == null || round.frames.Count > 100) return;
            int replaceIndex=-1;
            if(replaceLatest)
                for(int i=0;i<round.frames.Count;i++)if(round.frames[i].name==name)
                {
                    replaceIndex=i;
                    if(round.frames[i].tick==State.Tick&&round.frames[i].publicationSequence==session.AuthoritativeStateSequence.ToString())return;
                    break;
                }
            CaptureNeutralRecipe(session.Content,State.WaveId,evidence.neutralRecipes);
            var bytes = WireProtocol.EncodeSnapshot(State);
            var frame = new Frame { name = name, bytes = Convert.ToBase64String(bytes), hash = WireProtocol.Digest(bytes), wall = Now,
                publicationSequence=session.AuthoritativeStateSequence.ToString(),tick = State.Tick, playTicks = State.PlayTicks, wave = State.WaveId.ToString(), phase = (int)State.WavePhase, epoch = State.SessionId };
            foreach (var tower in State.Towers)
            {
                var stats = TowerUpgradeRules.Stats(session.Content, tower);
                frame.towers.Add(new TowerSample { id = tower.Id, damage = stats.Damage.ToString(), range = stats.Range, incomeUnits = stats.IncomeUnitsPerSecond.ToString() });
            }
            if(replaceIndex>=0)round.frames[replaceIndex]=frame;else round.frames.Add(frame);
        }
        Frame FindFrame(string name){foreach(var frame in round.frames)if(frame.name==name)return frame;return null;}
        bool HasFrame(string name) { foreach (var frame in round.frames) if (frame.name == name) return true; return false; }
        bool Accepted(string label) => AcceptedCode(label, ErrorCode.None);
        bool AcceptedCode(string label, ErrorCode code) => latest.TryGetValue(label, out var ack) && ack.code == (int)code;
        bool HasAck(string label, ulong sequence, ErrorCode code) { foreach (var ack in round.acks) if (ack.label == label && ack.sequence == sequence && ack.code == (int)code) return true; return false; }
        TowerState FindTower(int id) { foreach (var tower in State.Towers) if (tower.Id == id) return tower; return null; }
        TowerState FindOwn(int kind) { foreach (var tower in State.Towers) if (tower.Owner == session.LocalSlot && tower.Kind == kind) return tower; return null; }
        bool OwnOrder(int kind, int research) { foreach (var order in State.SpawnOrders) if (order.Sender == session.LocalSlot && order.Kind == kind && order.ResearchLevel == research) return true; return false; }
        bool BothMixed()
        {
            int mask = 0;
            foreach (var tower in State.Towers) if (tower.Kind == 0 && tower.RangeLevel == 1 && tower.DamageLevel == 1 && tower.Priority == TargetPriority.Strongest) mask |= 1 << (int)tower.Owner;
            return mask == 3;
        }
        bool FindFreePoint(int kind, out MapPoint point)
        {
            for (var y = 37; y < session.Content.Map.Height - 30; y += 61)
            for (var x = 43; x < session.Content.Map.Width - 30; x += 61)
            {
                point = new MapPoint(x, y); bool formerSlot = false;
                foreach (var old in session.Content.Map.BuildSlots) if (old.X == x && old.Y == y) formerSlot = true;
                if (!formerSlot && PlacementValidator.ValidatePosition(session.Content, State, session.LocalSlot, kind, -1, point) == ErrorCode.None) return true;
            }
            point = default; return false;
        }
        void Advance() { stage++; round.stage = stage; stageAt = Now; }
        void FinishCycle()
        {
            if (cycle == null || finishing) return; finishing = true; CaptureConnection();
            cycle.sentBytes = session.SentBytes; cycle.receivedBytes = session.ReceivedBytes;
            if (cycle.failure.Length == 0)
            {
                if (!cycle.connected || cycle.rounds.Count != (completeMatches ? 2 : 1)) cycle.failure = "Missing connection/rounds.";
                foreach (var item in cycle.rounds)
                    if (!item.freshResources || !item.scenarioPassed || completeMatches && !item.resultObserved
                        || hostRulesStartOnly && !item.startOnlyObserved) cycle.failure = "Missing wave scenario or full result.";
                if (completeMatches && (!cycle.rounds[0].rematchRequested || !cycle.rounds[0].rematchAccepted)) cycle.failure = "No mutually confirmed rematch.";
                if (relay && (!cycle.relayAllocationEstablished || string.IsNullOrEmpty(cycle.anonymousPlayerFingerprint))) cycle.failure = "Actual Relay authentication/allocation missing.";
            }
            session.Shutdown(); pending.Clear();
            cycle.cleanShutdown = session.State == null && session.PendingCommands == 0 && session.RegisteredMessageHandlers == 0;
            if (!cycle.cleanShutdown) cycle.failure = "Shutdown leaked handlers/state/pending commands.";
            bool failed = cycle.failure.Length > 0; Persist(); cycle = null;
            if (failed || evidence.cycles.Count == evidence.requestedCycles) Complete(!failed);
            else { finishing = false; restartAt = Now + (host ? 2.5 : 2.8); }
        }
        void Fail(string reason) { if (cycle == null || finishing) return; cycle.failure = reason; Debug.LogError("DUEL_NETWORK_TEST: " + reason); FinishCycle(); }
        void Complete(bool passed)
        {
            evidence.passed = passed; evidence.endedUtc = DateTime.UtcNow.ToString("O"); Persist(); enabled = false;
            Debug.Log("DUEL_NETWORK_TEST " + (passed ? "PASS " : "FAIL ") + reportPath);
            Unsubscribe(); if (!Application.isEditor) Application.Quit(passed ? 0 : 2);
        }
        void Persist()
        {
            try { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))); File.WriteAllText(reportPath, JsonUtility.ToJson(evidence, true)); }
            catch (Exception error) { Debug.LogError("Cannot write network evidence: " + error.GetType().Name); }
        }
        void OnDestroy() => Unsubscribe();
        void Unsubscribe() { if (session == null) return; session.CommandResolved -= OnResolved; session.DiagnosticAcknowledgement -= OnDiagnostic; session.SnapshotObserved -= OnSnapshot; session.AuthoritativeCommandObserved -= OnAuthority; }
        static bool HasArg(string name) { foreach (var arg in Environment.GetCommandLineArgs()) if (arg == name) return true; return false; }
        static string Arg(string name, string fallback) { var args = Environment.GetCommandLineArgs(); for (var i = 0; i + 1 < args.Length; i++) if (args[i] == name) return args[i + 1]; return fallback; }
    }
}
