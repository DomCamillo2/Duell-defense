using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using DefenseDuell.Core;
using UnityEngine;
namespace DefenseDuell.Networking
{
    /// <summary>Explicit multi-process acceptance automation. Never creates fake connections, edits simulation state or fills slots with bots.</summary>
    public sealed class TeamNetworkTestDriver:MonoBehaviour
    {
        [Serializable] public sealed class Report
        {
            public int protocol=WireProtocol.Version,acceptanceSchema=4,slot=-1,format,capacity,mapIndex,incomeMultiplier;
            public string mode,role,rulesVersion=GameContent.SimulationRulesVersion,wavePlanVersion=MatchRulesPolicy.WavePlanVersion,catalogHash,baseContentHash,contentHash,startGold,authenticationProfile,anonymousPlayerFingerprint,startedUtc,endedUtc,failure="";
            public bool feature16; public int spawnIntervalTicks;public int[] unitPrices,unitCounts,researchPrices,unitCooldownTicks;public NetworkTestDriver.TowerProfile[] towerProfiles;
            public List<NetworkTestDriver.NeutralRecipeEvidence> neutralRecipes=new List<NetworkTestDriver.NeutralRecipeEvidence>();
            public bool endless,startOnly,passed,cleanShutdown,disconnectObserved,relayAllocationEstablished;
            public string test="Actual independent NGO peers; automation submits ordinary authority-validated commands. No bot fill, state mutation, device or separate-internet claim.";
            public List<Round> rounds=new List<Round>();public List<NetworkTestDriver.Checkpoint> checkpoints=new List<NetworkTestDriver.Checkpoint>();
        }
        [Serializable] public sealed class Round
        {
            public ulong epoch;public int number;public string resultDigest;public bool finished,rematchRequested;public double startedAt,smokeSeconds;
            public List<NetworkTestDriver.Frame> frames=new List<NetworkTestDriver.Frame>();
            public List<NetworkTestDriver.Ack> acks=new List<NetworkTestDriver.Ack>();
            public List<NetworkTestDriver.AuthorityProof> authority=new List<NetworkTestDriver.AuthorityProof>();
        }
        [Serializable] sealed class CodeFile{public string code,catalog;}
        sealed class Intent{public string label;public ulong epoch;public ErrorCode expected;}
        public static bool Requested=>Flag("--duel-team-test");
        public static string AuthenticationProfile=>Requested?"duel_team_"+Arg("--duel-peer-index","0"):null;
        SessionCoordinator session;Report report;Round round;MatchRules desired;
        bool host,relay,waitingCode,ending;string path,codePath;double began,nextPoll,advanceAt,finishAt=-1;
        int featureStage;int stage,hostStage,ownTower;ulong barrier;readonly Dictionary<ulong,Intent> pending=new Dictionary<ulong,Intent>();
        MatchState State=>session.State;int Slot=>(int)session.LocalSlot;double Now=>Time.realtimeSinceStartupAsDouble;
        public void Initialize(SessionCoordinator coordinator)
        {
            session=coordinator;host=Flag("--duel-host")||Flag("--duel-relay-host");relay=Flag("--duel-relay-host")||Flag("--duel-relay-client");
            path=Arg("--duel-report",Path.Combine(Application.persistentDataPath,"team-network.json"));codePath=Arg("--duel-code-file",path+".code");
            int format=int.Parse(Arg("--duel-format","1")),map=int.Parse(Arg("--duel-map","0")),income=int.Parse(Arg("--duel-income","3"));
            if(!MatchRulesPolicy.TryParseStartGold(Arg("--duel-start-gold","1000"),out var gold).Equals(ErrorCode.None))throw new ArgumentException("Exact gold invalid");
            desired=new MatchRules(1,gold,Flag("--duel-endless"),income,(MatchFormat)format,MapCatalog.CreateContents(session.Content.BaseContent)[map].Map.Id);
            if(desired.Format==MatchFormat.Duel||MatchRulesPolicy.Validate(session.Content.BaseContent,desired)!=ErrorCode.None)throw new ArgumentException("Invalid team test rules");
            report=new Report{mode=relay?"relay-dtls":"lan",role=host?"host":"client",format=format,capacity=desired.PlayerCount,mapIndex=map,incomeMultiplier=income,startGold=gold.ToString(),endless=desired.Endless,startOnly=Flag("--duel-team-start-only"),catalogHash=session.CatalogHash,startedUtc=DateTime.UtcNow.ToString("O")};
            if(report.endless&&!report.startOnly)throw new ArgumentException("Endless transport acceptance requires explicit start-only scope; finite mode proves two complete rounds.");
            report.feature16=Flag("--duel-features16");report.spawnIntervalTicks=session.Content.Rules.SpawnIntervalTicks;
            int n=session.Content.Units.Length;report.unitPrices=new int[n];report.unitCounts=new int[n];report.researchPrices=new int[n];report.unitCooldownTicks=new int[n];
            for(int k=0;k<n;k++){var u=session.Content.Units[k];report.unitPrices[k]=u.Price;report.unitCounts[k]=u.Count;report.researchPrices[k]=session.Content.Research[k].BasePrice;report.unitCooldownTicks[k]=u.CooldownTicks;}
            report.towerProfiles=new NetworkTestDriver.TowerProfile[session.Content.Towers.Length];
            for(int k=0;k<report.towerProfiles.Length;k++){var t=session.Content.Towers[k];report.towerProfiles[k]=new NetworkTestDriver.TowerProfile{price=t.Price,damage=t.Damage,range=t.Range,fireIntervalTicks=t.FireIntervalTicks};}
            session.CommandResolved+=Resolved;session.SnapshotObserved+=Snapshot;if(host)session.AuthoritativeCommandObserved+=Authority;
            if(host&&!session.ConfigureNextMatch(desired)){Fail("ConfigureNextMatch rejected");return;}
            began=Now;
            if(relay){if(host)_=ConnectOnline(null);else waitingCode=true;}
            else{ushort port=ushort.Parse(Arg("--duel-port","7777"));if(host)session.StartLanHost(port);else session.JoinLan(Arg("--duel-address","127.0.0.1"),port);}
            Persist();
        }
        async Task ConnectOnline(string code)
        {
            waitingCode=false;
            try{if(host)await session.HostOnlineAsync();else await session.JoinOnlineAsync(code);
                if(!session.RelayAllocationEstablished){Fail("Actual Relay allocation missing: "+session.Message);return;}
                if(host){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(codePath)));File.WriteAllText(codePath,JsonUtility.ToJson(new CodeFile{code=session.JoinCode,catalog=session.CatalogHash}));}}
            catch(Exception e){Fail("Relay: "+e.GetType().Name+": "+e.Message);}
        }
        void Update()
        {
            if(ending||report==null)return;
            if(Now-began>420){Fail("Team acceptance timeout stage="+stage+" hostStage="+hostStage);return;}
            if(waitingCode&&Now>=nextPoll){nextPoll=Now+.25;try{if(File.Exists(codePath)){var info=JsonUtility.FromJson<CodeFile>(File.ReadAllText(codePath));if(info.catalog==session.CatalogHash&&!string.IsNullOrEmpty(info.code))_=ConnectOnline(info.code);}}catch(IOException){}catch(ArgumentException){}}
            if(session.Status==ConnectionState.ConnectionLost||session.Status==ConnectionState.Finished&&session.ConnectedPlayers==0)
            {
                bool bothFinished=report.rounds.Count==2&&report.rounds[0].finished&&report.rounds[1].finished;
                bool scenarioComplete=round!=null&&(report.startOnly?Has("started-smoke"):bothFinished);
                if(NetworkTestDriver.ExpectedGuestShutdown(host,session.Status,session.ConnectedPlayers,report.startOnly,scenarioComplete,!report.startOnly,bothFinished)){report.disconnectObserved=true;Complete();}
                else Fail("Connection lost before expected shutdown: "+session.Message);return;
            }
            if(State==null||session.Status==ConnectionState.Connecting)return;
            NetworkTestDriver.CaptureNeutralRecipe(session.Content,State.WaveId,report.neutralRecipes);
            report.slot=Slot;report.contentHash=session.Content.ContentHash;report.baseContentHash=session.Content.BaseContentHash;
            report.authenticationProfile=session.AuthenticationProfile;report.anonymousPlayerFingerprint=session.AnonymousPlayerFingerprint;report.relayAllocationEstablished=session.RelayAllocationEstablished;
            if(round==null||round.epoch!=State.SessionId)
            {
                if(State.Phase!=MatchPhase.Lobby){Fail("First state of round was not lobby");return;}
                round=new Round{number=report.rounds.Count+1,epoch=State.SessionId};report.rounds.Add(round);stage=hostStage=featureStage=0;barrier=0;ownTower=0;pending.Clear();Capture("initial");
            }
            if(pending.Count>0||session.AuthoritativeStateSequence<barrier||Now<advanceAt)return;
            if(State.Phase==MatchPhase.Lobby){Lobby();return;}
            if(!Has("start-granted")){Capture("start-granted");round.startedAt=Now;foreach(var player in State.Players)if(player.Gold!=desired.StartGold){Fail("Start grant not exact");return;}stage=0;}
            if(State.Phase==MatchPhase.Finished){Finished();return;}
            if(State.Phase!=MatchPhase.Playing)return;
            Battle();
        }
        void Lobby()
        {
            if(session.ConnectedPlayers!=desired.PlayerCount)return;
            if(host&&hostStage<4)
            {
                if(hostStage==0){Capture("full-roster");var temp=new MatchRules(1,desired.StartGold,desired.Endless,desired.IncomeMultiplier==1?2:1,desired.Format,MapCatalog.CreateContents(session.Content.BaseContent)[(report.mapIndex+1)%MapCatalog.Count].Map.Id);Issue("temporary-rules",Rules(temp));}
                else if(hostStage==1)Issue("temporary-team",Command(CommandType.SetTeam,(State.Players[0].TeamId+1)%State.Teams.Length));
                else if(hostStage==2)Issue("restore-team",Command(CommandType.SetTeam,0));
                else Issue("final-rules",Rules(desired));
                hostStage++;advanceAt=Now+.4;return;
            }
            if(State.ConfigRevision<2||!State.MatchRules.Equals(desired))return;
            if(!Has("configured"))Capture("configured");
            // Serialize only lobby choices, using the visible preceding slot's final mask as a revision barrier.
            for(int p=0;p<Slot;p++)if(State.Players[p].AvailableTowerMask!=Loadout(p))return;
            if(State.Players[Slot].AvailableTowerMask!=Loadout(Slot)){Issue("loadout",Command(CommandType.SetLoadout,Loadout(Slot)));return;}
            for(int p=0;p<State.Players.Length;p++)if(State.Players[p].AvailableTowerMask!=Loadout(p))return;
            if(!Has("roster-ready"))Capture("roster-ready");
            if(!State.Players[Slot].Ready)Issue("ready",Command(CommandType.SetReady));
        }
        static int Loadout(int slot)=>129|(1<<(slot%7));
        CommandEnvelope Command(CommandType type,int kind=0,int target=-1)=>new CommandEnvelope{SessionId=State.SessionId,Type=type,Kind=kind,Target=target,
            ExpectedConfigRevision=type==CommandType.SetReady||type==CommandType.SetTeam||type==CommandType.SetLoadout||type==CommandType.ApplyMatchRules?State.ConfigRevision:0,
            ExpectedRosterRevision=type==CommandType.SetReady||type==CommandType.SetTeam||type==CommandType.SetLoadout?State.RosterRevision:0};
        CommandEnvelope Rules(MatchRules rules){var c=Command(CommandType.ApplyMatchRules);c.RuleWaveCount=rules.WaveCount;c.RuleStartGold=rules.StartGold;c.RuleEndless=rules.Endless;c.RuleIncomeMultiplier=rules.IncomeMultiplier;c.RuleFormat=rules.Format;c.RuleMapId=rules.MapId;return c;}
        void Battle()
        {
            if(report.startOnly)
            {
                if(Now-round.startedAt>=4&&!Has("started-smoke")){round.smokeSeconds=Now-round.startedAt;Capture("started-smoke");Persist();finishAt=Now+4;}
                if(host&&finishAt>0&&Now>=finishAt)Complete();return;
            }
            if(stage==0){int kind=Slot==0?7:0;var pos=Position(kind);var c=Command(CommandType.BuildTower,kind);c.PositionX=pos.X;c.PositionY=pos.Y;Issue("build-own",c);stage++;return;}
            if(stage==1)
            {
                TowerState mine=null,foreign=null;foreach(var tower in State.Towers){if(tower.Owner==session.LocalSlot)mine=tower;else foreign=tower;}
                if(mine==null||foreign==null)return;ownTower=mine.Id;Issue("foreign-upgrade",Command(CommandType.UpgradeTower,(int)TowerUpgradeRules.PrimaryPath(session.Content,foreign.Kind),foreign.Id),ErrorCode.NotOwner);stage++;return;
            }
            if(stage==2){TowerState tower=null;foreach(var t in State.Towers)if(t.Id==ownTower)tower=t;Issue("upgrade-own",Command(CommandType.UpgradeTower,(int)TowerUpgradeRules.PrimaryPath(session.Content,tower.Kind),ownTower));stage++;return;}
            if(stage==3&&report.feature16&&featureStage<6){FeatureActions();return;}
            if(stage==3){int ally=-1;for(int p=0;p<State.Players.Length;p++)if(p!=Slot&&State.Players[p].TeamId==State.Players[Slot].TeamId){ally=p;break;}var c=Command(CommandType.SendUnit);c.EnemyTarget=ally;c.ExpectedWave=State.WaveId;Issue("ally-send",c,ErrorCode.InvalidTarget);stage++;return;}
            if(stage==4){var quote=SendRules.Quote(session.Content,State,session.LocalSlot,0);if(!quote.IsValid){Fail("Required send quote: "+quote.Code);return;}var c=Command(CommandType.SendUnit);c.EnemyTarget=quote.EnemyTarget;c.ExpectedWave=quote.WaveId;c.ExpectedResearch=quote.ResearchLevel;Issue("paid-target",c);stage++;return;}
            if(stage==5){int alternative=-1;for(int p=0;p<State.Players.Length;p++)if(State.CanTarget(session.LocalSlot,(PlayerSlot)p)&&(PlayerSlot)p!=State.Players[Slot].AttackTarget){alternative=p;break;}if(alternative<0){Fail("No second enemy field");return;}Issue("change-target",Command(CommandType.SetAttackTarget,alternative));stage++;return;}
            if(stage==6&&report.feature16&&featureStage==6){FeatureActions();return;}
            if(stage==6){Capture("actions-complete");Issue("speed-vote",Command(CommandType.SetSpeedVote,1));stage++;return;}
            if(stage==7){if(State.WavePhase==WavePhase.Preparing){var c=Command(CommandType.SetWaveReady,1);c.ExpectedWave=State.WaveId;Issue("wave-ready",c);}stage++;return;}
        }
        void FeatureActions()
        {
            TowerState tower=null;foreach(var t in State.Towers)if(t.Id==ownTower)tower=t;
            if(tower==null){Fail("Feature tower disappeared");return;}
            var path=TowerUpgradeRules.PrimaryPath(session.Content,tower.Kind);
            if(featureStage==0||featureStage==6)
            {
                var mode=featureStage==0?UpgradeBatchMode.Ten:UpgradeBatchMode.Max;
                var q=TowerUpgradeBatchRules.Quote(session.Content,tower,path,State.Players[Slot].Gold,mode);
                if(!q.IsValid||q.Count<1){Fail("Feature batch quote: "+q.Code);return;}
                var c=Command(CommandType.UpgradeTowerBatch,(int)path,tower.Id);c.UpgradeCount=q.Count;c.ExpectedTowerLevel=tower.Level;c.UpgradePriceLimit=q.Price;
                Issue(featureStage==0?"batch-ten":"batch-max",c);
            }
            else if(featureStage==1)
            {
                var c=Command(CommandType.UpgradeTowerBatch,(int)path,tower.Id);c.UpgradeCount=1;c.ExpectedTowerLevel=tower.Level-1;c.UpgradePriceLimit=100000;
                Issue("batch-stale",c,ErrorCode.QuoteChanged);
            }
            else if(featureStage==4)
            {var c=Command(CommandType.ResearchUnit,4);c.ExpectedResearch=State.Players[Slot].ResearchLevels[4];Issue("elite-research",c);}
            else
            {
                int kind=featureStage==3?5:4;var q=SendRules.Quote(session.Content,State,session.LocalSlot,kind);
                if(!q.IsValid){Fail("Elite quote: "+q.Code);return;}
                var c=Command(CommandType.SendUnit,kind);c.EnemyTarget=q.EnemyTarget;c.ExpectedWave=q.WaveId;c.ExpectedResearch=q.ResearchLevel;
                Issue(featureStage==2?"elite-fast-old":featureStage==3?"elite-siege":"elite-fast-new",c);
            }
            featureStage++;
        }
        void Finished()
        {
            if(!round.finished){if(stage<7){Fail("Round ended before required actions");return;}round.finished=true;Capture("final");round.resultDigest=WireProtocol.Digest(WireProtocol.EncodeSnapshot(State));finishAt=Now;Persist();}
            if(round.number==1&&Now-finishAt>=2&&!round.rematchRequested){round.rematchRequested=true;Issue("rematch",Command(CommandType.RequestRematch));}
            else if(round.number==2&&host&&Now-finishAt>=5)Complete();
        }
        MapPoint Position(int kind)
        {
            for(int y=31;y<session.Content.Map.Height-30;y+=61)for(int x=31;x<session.Content.Map.Width-30;x+=61){var p=new MapPoint(x,y);if(PlacementValidator.ValidatePosition(session.Content,State,session.LocalSlot,kind,-1,p)==ErrorCode.None)return p;}
            throw new InvalidOperationException("No legal placement");
        }
        void Issue(string label,CommandEnvelope command,ErrorCode expected=ErrorCode.None)
        {
            var intent=new Intent{label=label,epoch=command.SessionId,expected=expected};ulong seq=session.SubmitForDiagnostics(command);
            if(seq==0){Fail("Submission blocked: "+label+": "+session.Message);return;}
            if(host)Record(intent,session.LastResult);else pending.Add(seq,intent);
        }
        void Resolved(CommandResult result){if(!host&&pending.TryGetValue(result.Sequence,out var intent)&&session.LastResultEpoch==intent.epoch){pending.Remove(result.Sequence);Record(intent,result);}}
        void Record(Intent intent,CommandResult result)
        {
            round.acks.Add(new NetworkTestDriver.Ack{label=intent.label,epoch=intent.epoch,sequence=result.Sequence,code=(int)result.Code,tick=result.Tick,publicationSequence=session.LastResultPublicationSequence.ToString()});
            barrier=Math.Max(barrier,session.LastResultPublicationSequence);
            if(result.Code!=intent.expected){Fail("Unexpected ACK "+intent.label+": "+result.Code);return;}Persist();
        }
        void Authority(PlayerSlot player,CommandEnvelope command,CommandResult result,byte[] before,byte[] after)
        {if(round!=null&&command.SessionId==round.epoch)round.authority.Add(new NetworkTestDriver.AuthorityProof{player=(int)player,command=new NetworkTestDriver.CommandProof(command),result=result,before=Convert.ToBase64String(before),after=Convert.ToBase64String(after)});}
        void Snapshot(ulong epoch,int tick,string hash)
        {if(report!=null&&report.checkpoints.Count<20000)report.checkpoints.Add(new NetworkTestDriver.Checkpoint{epoch=epoch,tick=tick,hash=hash,publicationSequence=session.AuthoritativeStateSequence.ToString()});}
        bool Has(string name){foreach(var frame in round.frames)if(frame.name==name)return true;return false;}
        void Capture(string name)
        {NetworkTestDriver.CaptureNeutralRecipe(session.Content,State.WaveId,report.neutralRecipes);var bytes=WireProtocol.EncodeSnapshot(State);round.frames.Add(new NetworkTestDriver.Frame{name=name,bytes=Convert.ToBase64String(bytes),hash=WireProtocol.Digest(bytes),wall=Now,publicationSequence=session.AuthoritativeStateSequence.ToString(),epoch=State.SessionId,tick=State.Tick,playTicks=State.PlayTicks,wave=State.WaveId.ToString(),phase=(int)State.WavePhase});}
        void Persist(){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));File.WriteAllText(path,JsonUtility.ToJson(report,true));}
        void Fail(string reason){if(ending)return;report.failure=reason;Debug.LogError("DUEL_TEAM_TEST: "+reason);Complete();}
        void Complete(){if(ending)return;ending=true;session.Shutdown();report.cleanShutdown=session.Status==ConnectionState.Idle;report.passed=string.IsNullOrEmpty(report.failure)&&report.cleanShutdown;report.endedUtc=DateTime.UtcNow.ToString("O");Persist();Application.Quit(report.passed?0:1);}
        void OnDestroy(){if(session==null)return;session.CommandResolved-=Resolved;session.SnapshotObserved-=Snapshot;session.AuthoritativeCommandObserved-=Authority;}
        static bool Flag(string name)=>Array.IndexOf(Environment.GetCommandLineArgs(),name)>=0;
        static string Arg(string name,string fallback){var args=Environment.GetCommandLineArgs();for(int i=0;i+1<args.Length;i++)if(args[i]==name)return args[i+1];return fallback;}
    }
}
