using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DefenseDuell.Core;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace DefenseDuell.Networking
{
    public enum SessionMode { Offline, Lan, Online }
    public enum ConnectionState { Idle, Connecting, Lobby, Loading, Playing, ConnectionLost, Finished }

    /// <summary>Single owner for simulation, NGO, connection lifetime, async work and message subscriptions.</summary>
    public sealed class SessionCoordinator : MonoBehaviour
    {
        private const string CommandMessage = "dd.command.2", ResultMessage = "dd.ack.2", SnapshotMessage = "dd.snapshot.2";
        private const string WelcomeMessage = "dd.welcome.2", StateMessage = "dd.state.2", HeartbeatMessage = "dd.heartbeat.2";
        private const string LeaveMessage = "dd.leave.8", PageAckMessage="dd.page-ack.8";
        private const double ProgressTimeout = 12, ConnectingTimeout = 25;
        private static readonly string[] MessageNames = { CommandMessage, ResultMessage, SnapshotMessage, WelcomeMessage, StateMessage, HeartbeatMessage, LeaveMessage, PageAckMessage };

        public MatchState State => simulation != null ? simulation.State : receivedState;
        public GameContent Content { get; private set; }
        public int SelectedMapIndex { get; private set; }
        public BotDifficulty Difficulty { get; private set; } = BotDifficulty.Normal;
        public int LocalTowerMask { get; private set; } = TowerLoadoutRules.DefaultMask;
        public string[] MapNames => mapNames == null ? Array.Empty<string>() : (string[])mapNames.Clone();
        public string CatalogHash { get; private set; }
        public bool RelayAllocationEstablished { get; private set; }
        public string AuthenticationProfile { get; private set; } = string.Empty;
        public string AnonymousPlayerFingerprint { get; private set; } = string.Empty;
        public PlayerSlot LocalSlot { get; private set; }
        public bool IsHost { get; private set; }
        public SessionMode Mode { get; private set; }
        public ConnectionState Status { get; private set; }
        public string Message { get; private set; } = "Bereit.";
        public string JoinCode { get; private set; } = string.Empty;
        public int ConnectedPlayers { get; private set; }
        public long SentBytes { get; private set; }
        public long ReceivedBytes { get; private set; }
        public CommandResult LastResult { get; private set; }
        public ulong LastResultEpoch { get; private set; }
        public ulong LastResultPublicationSequence { get; private set; }
        public ulong AuthoritativeStateSequence => IsHost ? publicationSequence : publicationGate.Sequence;
        public ulong ConfigRevision => State?.ConfigRevision ?? 0;
        public ulong RosterRevision => State?.RosterRevision ?? 0;
        public int ConfiguredWaveCount => State?.MatchRules?.WaveCount ?? Content.MatchRules.WaveCount;
        public BigInteger ConfiguredStartGold => State?.MatchRules?.StartGold ?? Content.MatchStartGold;
        public bool CanEditHostRules => IsHost && State!=null && State.Phase==MatchPhase.Lobby && Status==ConnectionState.Lobby;
        public bool CanReady
        {
            get
            {
                if(State==null||State.Phase!=MatchPhase.Lobby||Status!=ConnectionState.Lobby||State.ConnectedPlayerMask!=((1<<State.Players.Length)-1))return false;
                for(int team=0;team<State.MatchRules.TeamCount;team++){int members=0;foreach(var player in State.Players)if(player.TeamId==team)members++;if(members!=State.MatchRules.MembersPerTeam)return false;}
                return true;
            }
        }
        public bool LanDiscoveryEnabled
        {
            get=>discoveryEnabled;
            set
            {
                if(discoveryEnabled==value)return;
                discoveryEnabled=value;
                // This flag controls browsing only. A hosting lobby advertises independently.
                if(!value)UpdateDiscovery(Now);
            }
        }
        public IReadOnlyList<LanLobbyInfo> DiscoveredLanLobbies=>discovery?.Lobbies??Array.Empty<LanLobbyInfo>();
        public event Action LanLobbiesChanged;
        private bool discoveryEnabled,applicationPaused;
        private LanLobbyDiscovery discovery;
        private readonly AndroidLanMulticastLease multicastLease=new AndroidLanMulticastLease();
        private ushort lanPort=7777;
        private MatchRules nextMatchRules;
        public string BaseCatalogHash => CatalogHash;
        public string EffectiveContentHash => Content?.ContentHash;
        public int PendingCommands => pending.Count;
        public int LastSnapshotTick { get; private set; }
        public string LastSnapshotHash { get; private set; }
        public int RegisteredMessageHandlers => handlersRegistered ? MessageNames.Length : 0;
        public event Action Changed;
        public event Action<CommandResult> CommandResolved;
        public event Action<ulong, int, string> SnapshotObserved;
        // Optional acceptance instrumentation: no allocations when unsubscribed, no gameplay decisions.
        internal event Action<PlayerSlot, CommandEnvelope, CommandResult, byte[], byte[]> AuthoritativeCommandObserved;
        internal event Action<ulong, CommandResult> DiagnosticAcknowledgement;
        internal ulong DiagnosticPublicationSequence { get; private set; }
        internal ulong SubmitForDiagnostics(CommandEnvelope command) => NetworkTestDriver.Requested ? SubmitEnvelope(command) : 0;

        private MatchSimulation simulation;
        private GameContent[] catalog;
        private string[] mapNames;
        private MatchState receivedState;
        private BotBrain bot;
        private NetworkManager manager;
        private UnityTransport transport;
        private GameObject networkObject;
        private bool handlersRegistered, stopping, paused, destroying, networkStarted;
        private int generation;
        private CancellationTokenSource operation;
        private Task authenticationTask;
        private ulong localSequence, localEpoch;
        private sealed class Peer
        {
            public ulong Id,JoinEpoch;public PlayerSlot Slot;public int Mask;public bool Connected;public double JoinedAt,LastProgress;
            public uint Heartbeat;public MessageRateLimiter Limiter=new MessageRateLimiter(80,120);
            public StateTransfer Outgoing;public int Queued;
        }
        private readonly Dictionary<ulong,Peer> peers=new Dictionary<ulong,Peer>();
        private readonly List<ulong> stalePeers=new List<ulong>();
        private readonly List<Peer> sendPeers=new List<Peer>();
        private int transferCursor;
        private uint snapshotSequence, heartbeatSequence, lastPeerHeartbeat, appliedSnapshotSequence;
        private readonly Dictionary<(ulong epoch,ulong sequence), double> pending = new Dictionary<(ulong,ulong), double>();
        private ulong publicationSequence, previousEpoch, requestedRematchEpoch;
        private readonly StatePublicationGate publicationGate = new StatePublicationGate();
        private readonly SnapshotAssembler assembler = new SnapshotAssembler(double.PositiveInfinity);
        private readonly SnapshotAssembler welcomeAssembler = new SnapshotAssembler(double.PositiveInfinity), reliableAssembler = new SnapshotAssembler(double.PositiveInfinity);
        private sealed class StateTransfer
        {
            public string Name; public ulong Epoch; public uint Revision; public byte[] Body;
            public int Part,Count; public NetworkDelivery Delivery; public StateTransferWindow Window;
        }

        private double accumulator, nextSnapshotAt, nextHeartbeatAt, connectBegan, lastPeerProgress, lastSimulationProgress, lastUpdateAt;
        private int lastTick = -1;
        private BigInteger lastWaveId;
        private MatchPhase lastPhase;
        private WavePhase lastWavePhase;

        public void Initialize(GameContent content)
        {
            if (Content != null) return;
            Initialize(MapCatalog.CreateContents(content ?? throw new ArgumentNullException(nameof(content))));
        }

        public void Initialize(GameContent[] contents)
        {
            if (Content != null) return;
            if (contents == null || contents.Length != MapCatalog.Count) throw new ArgumentException("The complete validated map catalog is required.", nameof(contents));
            catalog = (GameContent[])contents.Clone(); mapNames = new string[catalog.Length];
            for (var i = 0; i < catalog.Length; i++)
            {
                if (catalog[i] == null) throw new ArgumentException("Map catalog entries cannot be null.", nameof(contents));
                catalog[i].Validate(); mapNames[i] = catalog[i].Map.Name;
            }
            CatalogHash = WireProtocol.CatalogHash(catalog);
            SelectedMapIndex = 0; Content = catalog[0];nextMatchRules=Content.MatchRules;LocalTowerMask=TowerLoadoutRules.Normalize(TowerLoadoutRules.DefaultMask,Content.Towers.Length);
            discovery=new LanLobbyDiscovery(CatalogHash);discovery.Changed+=()=>LanLobbiesChanged?.Invoke();
            Application.runInBackground = true;
            Status = ConnectionState.Idle;
            if(TeamNetworkTestDriver.Requested && GetComponent<TeamNetworkTestDriver>()==null)
                gameObject.AddComponent<TeamNetworkTestDriver>().Initialize(this);
            else if (NetworkTestDriver.Requested && GetComponent<NetworkTestDriver>() == null)
                gameObject.AddComponent<NetworkTestDriver>().Initialize(this);
        }

        public bool SelectMap(int index)
        {
            EnsureInitialized();
            if (Status != ConnectionState.Idle)
            { Message = "Die Karte kann nur im Hauptmenü gewählt werden."; RaiseChanged(); return false; }
            if (index < 0 || index >= catalog.Length)
            { Message = "Diese Karte ist nicht verfügbar."; RaiseChanged(); return false; }
            SelectedMapIndex = index; nextMatchRules=(nextMatchRules??catalog[index].MatchRules).WithMap(catalog[index].Map.Id);Content = catalog[index].WithMatchRules(nextMatchRules);
            Message = "Karte gewählt: " + mapNames[index] + "."; RaiseChanged(); return true;
        }

        public bool ConfigureNextMatch(MatchRules rules)
        {
            EnsureInitialized();if(Status!=ConnectionState.Idle||rules==null)return false;
            var error=MatchRulesPolicy.Validate(catalog[SelectedMapIndex],rules);
            if(error!=ErrorCode.None){Message=ErrorText(error);RaiseChanged();return false;}
            try{var effective=catalog[SelectedMapIndex].WithMatchRules(rules);int index=MapIndex(effective.Map.Id);if(index<0)return false;
                nextMatchRules=effective.MatchRules;SelectedMapIndex=index;Content=effective;RaiseChanged();return true;}
            catch(ArgumentException){Message="Diese Regeln oder Karte sind nicht verfügbar.";RaiseChanged();return false;}
        }
        int MapIndex(string id){for(int i=0;i<catalog.Length;i++)if(catalog[i].Map.Id==id)return i;return -1;}
        public bool JoinDiscoveredLanLobby(string lobbyId)
        {
            if(Status!=ConnectionState.Idle||discovery==null||!discovery.TryGet(lobbyId,Now,out var lobby))return false;
            JoinLan(lobby.HostAddress,lobby.Port);return Status==ConnectionState.Connecting;
        }
        void UpdateDiscovery(double now)
        {
            if(discovery==null)return;
            if(applicationPaused||destroying){discovery.Suspend();multicastLease.SetActive(false);return;}
            bool browse=discoveryEnabled&&Status==ConnectionState.Idle;
            LanLobbyAdvertisement announcement=null;
            if(Mode==SessionMode.Lan&&IsHost&&!stopping&&manager!=null&&manager.IsListening&&State?.Phase==MatchPhase.Lobby)
                announcement=new LanLobbyAdvertisement{Epoch=State.SessionId,CatalogHash=CatalogHash,Port=lanPort,MapName=Content.Map.Name,
                    FormatName=State.MatchRules.Format==MatchFormat.Duel?"1v1":State.MatchRules.Format==MatchFormat.TwoVsTwo?"2v2":State.MatchRules.Format==MatchFormat.ThreeVsThree?"3v3":"2v2v2",
                    PlayerCount=ConnectedPlayers,Capacity=State.Players.Length,Joinable=ConnectedPlayers<State.Players.Length};
            multicastLease.SetActive(browse||announcement!=null);discovery.Tick(now,browse,announcement);
        }

        public bool SetDifficulty(BotDifficulty difficulty)
        {
            if (Status != ConnectionState.Idle || (int)difficulty < (int)BotDifficulty.Easy || (int)difficulty > (int)BotDifficulty.Hard) return false;
            Difficulty = difficulty; RaiseChanged(); return true;
        }

        public bool SetTowerAvailability(int mask)
        {
            EnsureInitialized();
            if (Status != ConnectionState.Idle || !WireSnapshotValidator.ValidTowerMask(mask, Content.Towers.Length)) return false;
            LocalTowerMask = mask; RaiseChanged(); return true;
        }

        public void StartOffline(uint seed = 1)
        {
            EnsureInitialized();
            if(Content.MatchRules.Format!=MatchFormat.Duel){Message="Das Training ist ein 1v1 gegen den Bot. Bitte 1v1 wählen.";RaiseChanged();return;}
            Begin(SessionMode.Offline, true);
            simulation = new MatchSimulation(Content, seed, NewEpoch());
            if (!simulation.ConfigureTowerAvailability(PlayerSlot.Zero, LocalTowerMask)
                || !simulation.ConfigureTowerAvailability(PlayerSlot.One, LocalTowerMask)) throw new InvalidOperationException("Ungültige lokale Turmfreigaben.");
            bot = new BotBrain(PlayerSlot.One, seed ^ 0x5A731u, Difficulty);
            ConnectedPlayers = 2;
            BumpPublication(); RefreshEpoch();
            ExecuteObserved(PlayerSlot.One, MakeCommand(PlayerSlot.One, CommandType.SetReady));
            SetStatus(ConnectionState.Lobby, "Training gegen den Bot. Bereit für das Duell?");
        }

        public void StartLanHost(ushort port = 7777)
        {
            EnsureInitialized();
            if (port == 0) { Message = "Bitte einen Port von 1 bis 65535 eingeben."; RaiseChanged(); return; }
            lanPort=port;var id = Begin(SessionMode.Lan, true);
            StartCoroutine(StartLanAfterShutdown(id, true, "127.0.0.1", port));
        }

        public void JoinLan(string address, ushort port = 7777)
        {
            EnsureInitialized();
            if (port == 0 || !IPAddress.TryParse(address?.Trim(), out var ip) || IPAddress.Any.Equals(ip) || IPAddress.IPv6Any.Equals(ip))
            { Message = "Bitte eine gültige Host-IP und einen Port von 1 bis 65535 eingeben."; RaiseChanged(); return; }
            var id = Begin(SessionMode.Lan, false);
            StartCoroutine(StartLanAfterShutdown(id, false, ip.ToString(), port));
        }

        private IEnumerator StartLanAfterShutdown(int id, bool host, string address, ushort port)
        {
            while (manager != null && manager.ShutdownInProgress)
            { if (id != generation) yield break; yield return null; }
            if (id != generation) yield break;
            try
            {
                PrepareNetwork();
                transport.SetConnectionData(address, port, host ? "0.0.0.0" : null);
                StartNetwork(host);
                if (host) Message = "WLAN-Raum geöffnet. Host-IP und Port " + port + " weitergeben.";
                else Message = "WLAN-Verbindung wird hergestellt …";
                RaiseChanged();
            }
            catch (Exception error) { FailStart("WLAN konnte nicht gestartet werden: " + SafeError(error)); }
        }

        public Task HostOnlineAsync() => StartOnlineAsync(true, null);
        public Task JoinOnlineAsync(string code) => StartOnlineAsync(false, code);

        private async Task StartOnlineAsync(bool host, string code)
        {
            EnsureInitialized();
            code = code?.Trim().ToUpperInvariant();
            if (!host && (string.IsNullOrEmpty(code) || code.Length < 4 || code.Length > 12))
            { Message = "Bitte den Beitrittscode eingeben."; RaiseChanged(); return; }
            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                Message = "Online ist noch nicht eingerichtet: In Unity unter Project Settings > Services ein kostenlos nutzbares Unity-Cloud-Projekt verknüpfen. WLAN und Training sind sofort nutzbar.";
                RaiseChanged(); return;
            }
            var id = Begin(SessionMode.Online, host);
            var cancellation = operation.Token;
            try
            {
                Message = "Online: anonyme Anmeldung …"; RaiseChanged();
                while (manager != null && manager.ShutdownInProgress)
                { cancellation.ThrowIfCancellationRequested(); await Task.Yield(); }
                if (authenticationTask == null || authenticationTask.IsCompleted)
                    authenticationTask = AuthenticateAsync();
                await AwaitBounded(authenticationTask, cancellation);
                if (id != generation) return;
                AuthenticationProfile = AuthenticationService.Instance.Profile;
                AnonymousPlayerFingerprint = WireProtocol.Digest(Encoding.UTF8.GetBytes(AuthenticationService.Instance.PlayerId));
                PrepareNetwork();
                if (host)
                {
                    var allocation = await AwaitBounded(RelayService.Instance.CreateAllocationAsync(5), cancellation);
                    if (id != generation) return;
                    var joinCode = await AwaitBounded(RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId), cancellation);
                    if (id != generation) return;
                    transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
                    JoinCode = joinCode;
                }
                else
                {
                    var allocation = await AwaitBounded(RelayService.Instance.JoinAllocationAsync(code), cancellation);
                    if (id != generation) return;
                    transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
                    JoinCode = code;
                }
                RelayAllocationEstablished = true;
                cancellation.ThrowIfCancellationRequested();
                StartNetwork(host);
                Message = host ? "Online-Raum geöffnet. Beitrittscode: " + JoinCode : "Online-Verbindung wird hergestellt …";
                RaiseChanged();
            }
            catch (OperationCanceledException) { /* Shutdown owns the resulting view. */ }
            catch (Exception error)
            { if (id == generation) FailStart("Online-Verbindung fehlgeschlagen. Projekt, Internet und Relay-Freikontingent prüfen. " + SafeError(error)); }
        }

        private static async Task AuthenticateAsync()
        {
            var profile = TeamNetworkTestDriver.AuthenticationProfile ?? NetworkTestDriver.AuthenticationProfile;
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var options = new InitializationOptions();
                if (!string.IsNullOrEmpty(profile)) options.SetProfile(profile);
                await UnityServices.InitializeAsync(options);
            }
            if (!string.IsNullOrEmpty(profile) && AuthenticationService.Instance.Profile != profile)
            {
                if (AuthenticationService.Instance.IsSignedIn) AuthenticationService.Instance.SignOut();
                AuthenticationService.Instance.SwitchProfile(profile);
            }
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        private static async Task AwaitBounded(Task task, CancellationToken cancellation)
        {
            var delay = Task.Delay(20000, cancellation);
            if (await Task.WhenAny(task, delay) != task)
            {
                // SDK HTTP calls are not cancelable; observe late faults without letting them change session state.
                _ = task.ContinueWith(completed => { _ = completed.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                cancellation.ThrowIfCancellationRequested(); throw new TimeoutException("Zeitüberschreitung beim Onlinedienst.");
            }
            await task; cancellation.ThrowIfCancellationRequested();
        }

        private static async Task<T> AwaitBounded<T>(Task<T> task, CancellationToken cancellation)
        { await AwaitBounded((Task)task, cancellation); return await task; }

        private int Begin(SessionMode mode, bool host)
        {
            Shutdown();
            operation = new CancellationTokenSource();
            Mode = mode; IsHost = host; LocalSlot = host ? PlayerSlot.Zero : PlayerSlot.One;
            Status = ConnectionState.Connecting; Message = "Verbindung wird vorbereitet …";
            connectBegan = Now; lastPeerProgress = Now; lastSimulationProgress = Now;
            lastUpdateAt = Now;
            nextHeartbeatAt = Now; nextSnapshotAt = Now; localSequence = 0; localEpoch = 0; snapshotSequence = 0;
            publicationSequence=0;previousEpoch=requestedRematchEpoch=0;publicationGate.Reset();LastResultEpoch=LastResultPublicationSequence=0;
            Content=catalog[SelectedMapIndex].WithMatchRules(nextMatchRules??catalog[SelectedMapIndex].MatchRules);
            heartbeatSequence = 0; lastPeerHeartbeat = 0; accumulator = 0; lastTick = -1;
            SentBytes = 0; ReceivedBytes = 0; LastSnapshotHash = null; LastSnapshotTick = 0;
            peers.Clear();stopping = false; paused = false; appliedSnapshotSequence = 0;
            RaiseChanged(); return generation;
        }

        private void EnsureInitialized() { if (Content == null) Initialize(GameContent.CreateDefault()); }

        private void PrepareNetwork()
        {
            if (manager == null)
            {
                networkObject = new GameObject("Defense Duell Network");
                DontDestroyOnLoad(networkObject);
                transport = networkObject.AddComponent<UnityTransport>();
                manager = networkObject.AddComponent<NetworkManager>();
                manager.OnClientConnectedCallback += OnConnected;
                manager.OnClientDisconnectCallback += OnDisconnected;
                manager.OnTransportFailure += OnTransportFailure;
                manager.ConnectionApprovalCallback = Approve;
            }
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport, EnableSceneManagement = false, ConnectionApproval = true, ProtocolVersion = WireProtocol.Version,
                ConnectionData = WireProtocol.Handshake(CatalogHash, LocalTowerMask), TickRate = (uint)Content.TickRate,
                ClientConnectionBufferTimeout = 10, ForceSamePrefabs = false
            };
            transport.HeartbeatTimeoutMS = 500;
            transport.ConnectTimeoutMS = 1000;
            transport.MaxConnectAttempts = 12;
            transport.DisconnectTimeoutMS = 10000;
            peers.Clear();stopping = false;
            welcomeAssembler.Reset(0);reliableAssembler.Reset(0);assembler.Reset(0);
        }

        private void StartNetwork(bool host)
        {
            networkStarted = true;
            if (host)
            {
                simulation = new MatchSimulation(Content, 1, NewEpoch(), guestConnected:false);
                if (!simulation.ConfigureTowerAvailability(PlayerSlot.Zero, LocalTowerMask)) throw new InvalidOperationException("Ungültige lokale Turmfreigaben.");
                BumpPublication(); RefreshEpoch();
                if (!manager.StartHost()) throw new InvalidOperationException("Host konnte nicht gestartet werden.");
                ConnectedPlayers = 1;
                Status = ConnectionState.Lobby;
            }
            else
            {
                if (!manager.StartClient()) throw new InvalidOperationException("Client konnte nicht gestartet werden.");
                Status = ConnectionState.Connecting;
            }
            RegisterMessages();
        }

        private void RegisterMessages()
        {
            if (handlersRegistered) return;
            var custom = manager.CustomMessagingManager;
            custom.RegisterNamedMessageHandler(CommandMessage, OnCommand);
            custom.RegisterNamedMessageHandler(ResultMessage, OnResult);
            custom.RegisterNamedMessageHandler(SnapshotMessage, OnSnapshotChunk);
            custom.RegisterNamedMessageHandler(WelcomeMessage, OnWelcome);
            custom.RegisterNamedMessageHandler(StateMessage, OnReliableState);
            custom.RegisterNamedMessageHandler(HeartbeatMessage, OnHeartbeat);
            custom.RegisterNamedMessageHandler(LeaveMessage, OnLeave);
            custom.RegisterNamedMessageHandler(PageAckMessage, OnPageAcknowledged);
            handlersRegistered = true;
        }

        private void UnregisterMessages()
        {
            if (!handlersRegistered) return;
            if (manager != null && manager.CustomMessagingManager != null)
                foreach (var name in MessageNames) manager.CustomMessagingManager.UnregisterNamedMessageHandler(name);
            handlersRegistered = false;
        }

        private void Approve(NetworkManager.ConnectionApprovalRequest request,NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject=false;response.Pending=false;
            if(request.ClientNetworkId==NetworkManager.ServerClientId){response.Approved=true;return;}
            if(stopping||simulation==null||State.Phase!=MatchPhase.Lobby||peers.Count>=State.Players.Length-1)
            {response.Approved=false;response.Reason="Der Raum ist voll oder das Match hat bereits begonnen.";return;}
            response.Approved=WireProtocol.ValidateHandshake(request.Payload,CatalogHash,Content.Towers.Length,out var mask,out var reason);response.Reason=reason;
            if(!response.Approved)return;
            for(int slot=1;slot<State.Players.Length;slot++)
            {
                bool reserved=false;foreach(var peer in peers.Values)if((int)peer.Slot==slot){reserved=true;break;}
                if(reserved)continue;
                peers[request.ClientNetworkId]=new Peer{Id=request.ClientNetworkId,JoinEpoch=State.SessionId,Slot=(PlayerSlot)slot,Mask=mask,JoinedAt=Now,LastProgress=Now};return;
            }
            response.Approved=false;response.Reason="Der Raum ist voll.";
        }
        private void OnConnected(ulong peerId)
        {
            if(stopping||!networkStarted||Mode==SessionMode.Offline)return;
            lastPeerProgress=lastSimulationProgress=Now;
            if(IsHost&&peerId!=NetworkManager.ServerClientId)
            {
                if(!peers.TryGetValue(peerId,out var peer)){manager.DisconnectClient(peerId);return;}
                if(!simulation.ConfigureTowerAvailability(peer.Slot,peer.Mask)||!simulation.ConfigureConnection(peer.Slot,true))
                {peers.Remove(peerId);manager.DisconnectClient(peerId);return;}
                peer.Connected=true;peer.LastProgress=Now;ConnectedPlayers=ConnectedCount(State.ConnectedPlayerMask);BumpPublication();
                SendWelcome();Message="Spieler verbunden. Teams, Klassen und Regeln gemeinsam bestätigen.";
            }
            else if(!IsHost)Message="Verbindung steht. Spieldaten werden abgeglichen …";
            RaiseChanged();
        }
        static int ConnectedCount(int mask){int count=0;while(mask!=0){count+=mask&1;mask>>=1;}return count;}
        private void OnDisconnected(ulong peerId)
        {
            if(stopping||!networkStarted||Mode==SessionMode.Offline||Status==ConnectionState.Idle||Status==ConnectionState.ConnectionLost)return;
            if(IsHost)
            {
                if(!peers.TryGetValue(peerId,out var peer))return;
                peers.Remove(peerId);
                if(State?.Phase==MatchPhase.Lobby)
                {simulation.ConfigureConnection(peer.Slot,false);ConnectedPlayers=ConnectedCount(State.ConnectedPlayerMask);BumpPublication();SendWelcome();Message="Ein Spieler hat die Lobby verlassen.";RaiseChanged();return;}
            }
            var reason=!IsHost&&manager!=null?manager.DisconnectReason:string.Empty;
            LoseConnection(string.IsNullOrEmpty(reason)?"Verbindung beendet. Das Match wurde abgebrochen; es gibt keinen Sieger.":reason);
        }

        private void OnTransportFailure() { if (!stopping && networkStarted) LoseConnection("Netzwerkfehler. Bitte zurück ins Menü und einen neuen Raum öffnen."); }

        public ulong ApplyHostRules(int waveCount,BigInteger startGold,ulong displayedEpoch,ulong displayedConfigRevision)
            =>ApplyHostRules(new MatchRules(waveCount,startGold,State?.MatchRules.Endless??false,State?.MatchRules.IncomeMultiplier??1,State?.MatchRules.Format??MatchFormat.Duel,State?.MatchRules.MapId),displayedEpoch,displayedConfigRevision);
        public ulong ApplyHostRules(MatchRules rules,ulong displayedEpoch,ulong displayedConfigRevision)
        {
            if(!CanEditHostRules){Message="Nur der Host kann Regeln vor dem Start ändern.";RaiseChanged();return 0;}
            var error=MatchRulesPolicy.Validate(catalog[SelectedMapIndex],rules);
            if(error!=ErrorCode.None){Message=ErrorText(error);RaiseChanged();return 0;}
            return SubmitEnvelope(new CommandEnvelope{SessionId=displayedEpoch,Type=CommandType.ApplyMatchRules,Target=-1,
                ExpectedConfigRevision=displayedConfigRevision,RuleWaveCount=rules.WaveCount,RuleStartGold=rules.StartGold,
                RuleEndless=rules.Endless,RuleIncomeMultiplier=rules.IncomeMultiplier,RuleFormat=rules.Format,RuleMapId=rules.MapId});
        }
        public ulong SetLobbyTeam(int team,ulong epoch,ulong config,ulong roster)
            =>SubmitEnvelope(new CommandEnvelope{SessionId=epoch,Type=CommandType.SetTeam,Kind=team,Target=-1,ExpectedConfigRevision=config,ExpectedRosterRevision=roster});
        public ulong SetLobbyLoadout(int mask,ulong epoch,ulong config,ulong roster)
            =>SubmitEnvelope(new CommandEnvelope{SessionId=epoch,Type=CommandType.SetLoadout,Kind=mask,Target=-1,ExpectedConfigRevision=config,ExpectedRosterRevision=roster});
        public ulong SetLobbyReady(bool ready,ulong displayedEpoch,ulong displayedConfigRevision,ulong displayedRosterRevision)
            => SubmitEnvelope(new CommandEnvelope{SessionId=displayedEpoch,Type=CommandType.SetReady,Kind=ready?0:1,Target=-1,
                ExpectedConfigRevision=displayedConfigRevision,ExpectedRosterRevision=displayedRosterRevision});

        public ulong SubmitTowerUpgradeBatch(int towerId,TowerUpgradePath path,BigInteger count,BigInteger expectedLevel,BigInteger priceLimit)
            =>SubmitEnvelope(new CommandEnvelope{SessionId=State?.SessionId??0,Type=CommandType.UpgradeTowerBatch,Kind=(int)path,Target=towerId,
                UpgradeCount=count,ExpectedTowerLevel=expectedLevel,UpgradePriceLimit=priceLimit});

        public ulong Submit(CommandType type,int kind=0,int target=-1,int positionX=0,int positionY=0,
            BigInteger expectedWave=default,BigInteger expectedResearch=default,int enemyTarget=-1)
        {
            // Network readiness must originate from the actually displayed view, never hidden current tokens.
            if(type==CommandType.SetReady)
            {
                if(Mode==SessionMode.Offline)return SetLobbyReady(kind==0,State?.SessionId??0,ConfigRevision,RosterRevision);
                Message="Bitte die angezeigten Lobbyregeln bestätigen.";RaiseChanged();return 0;
            }
            if(type==CommandType.SendUnit&&enemyTarget<0){Message="Bitte ein gegnerisches Spielfeld als Ziel bestätigen.";RaiseChanged();return 0;}
            return SubmitEnvelope(new CommandEnvelope{SessionId=State?.SessionId??0,Type=type,Kind=kind,Target=target,
                PositionX=positionX,PositionY=positionY,ExpectedWave=expectedWave,ExpectedResearch=expectedResearch,EnemyTarget=type==CommandType.SendUnit?enemyTarget:0});
        }
        private ulong SubmitEnvelope(CommandEnvelope command)
        {
            if(State==null || Status==ConnectionState.Idle || Status==ConnectionState.Connecting || Status==ConnectionState.ConnectionLost)
            {Message="Zuerst eine Verbindung oder ein Training starten.";RaiseChanged();return 0;}
            if(Mode!=SessionMode.Offline && ConnectedPlayers!=State.Players.Length && (command.Type==CommandType.SetReady || command.Type==CommandType.RequestRematch))
            {Message="Warte auf alle Spieler und vollständige Teams.";RaiseChanged();return 0;}
            if(command.SessionId!=State.SessionId){Message=ErrorText(ErrorCode.WrongSession);RaiseChanged();return 0;}
            RefreshEpoch();
            if(pending.Count>=16){Message="Bitte auf die ausstehenden Bestätigungen warten.";RaiseChanged();return 0;}
            if(localSequence==ulong.MaxValue){Message=ErrorText(ErrorCode.RevisionExhausted);RaiseChanged();return 0;}
            command.Sequence=++localSequence;
            if(command.Type==CommandType.RequestRematch && State.Phase==MatchPhase.Finished)requestedRematchEpoch=command.SessionId;
            if(IsHost)
            {
                var result=ExecuteObserved(LocalSlot,command);
                Resolve(result,command.SessionId,publicationSequence);AfterAuthorityCommand(command.SessionId,result,command.Type);
            }
            else
            {
                pending.Add((command.SessionId,command.Sequence),Now);
                Send(CommandMessage,NetworkManager.ServerClientId,WireProtocol.EncodeCommand(command),NetworkDelivery.ReliableFragmentedSequenced);
                Message="Aktion wird bestätigt …";RaiseChanged();
            }
            return command.Sequence;
        }
        private CommandEnvelope MakeCommand(PlayerSlot slot,CommandType type)
        {
            if(simulation.LastSequence(slot)==ulong.MaxValue)throw new InvalidOperationException("Command sequence exhausted.");
            return new CommandEnvelope{SessionId=State.SessionId,Sequence=simulation.LastSequence(slot)+1,Type=type,Target=-1,
                ExpectedConfigRevision=type==CommandType.SetReady?ConfigRevision:0,ExpectedRosterRevision=type==CommandType.SetReady?RosterRevision:0};
        }

        // Only the explicitly launched acceptance driver can replay a request. It uses the production validator/wire path.
        internal bool RepeatCommandForDiagnostics(CommandEnvelope command)
        {
            if (!NetworkTestDriver.Requested || State == null || command.SessionId != State.SessionId
                || command.Sequence == 0 || ConnectedPlayers != State.Players.Length || stopping) return false;
            if (IsHost)
            {
                var result = ExecuteObserved(LocalSlot, command);
                DiagnosticPublicationSequence=publicationSequence;DiagnosticAcknowledgement?.Invoke(command.SessionId, result);
                AfterAuthorityCommand(command.SessionId, result, command.Type);
            }
            else Send(CommandMessage, NetworkManager.ServerClientId, WireProtocol.EncodeCommand(command), NetworkDelivery.ReliableFragmentedSequenced);
            return true;
        }

        private void OnCommand(ulong sender, FastBufferReader reader)
        {
            if (!IsHost || stopping || !peers.TryGetValue(sender,out var peer) || !peer.Connected || simulation == null) return;
            // Gameplay's idempotent 40-command/second limiter returns an ordinary rejected ACK.
            // A much higher wire-level flood is a protocol violation: end the session before allocation.
            if (!peer.Limiter.Allow(Now)) { LoseConnection("Zu viele Netzwerkanfragen. Das Duell wurde beendet."); return; }
            if (!ReadBody(reader, WireProtocol.MaxCommandBytes, out var body) || !WireProtocol.TryDecodeCommand(body, out var command)) return;
            var result = ExecuteObserved(peer.Slot, command);
            Send(ResultMessage, sender, WireProtocol.EncodeResult(command.SessionId, result,publicationSequence), NetworkDelivery.ReliableSequenced);
            AfterAuthorityCommand(command.SessionId, result, command.Type);
        }

        private CommandResult ExecuteObserved(PlayerSlot player, CommandEnvelope command)
        {
            var observer = AuthoritativeCommandObserved;
            var before = observer == null ? null : WireProtocol.EncodeSnapshot(State);
            var oldStateEpoch=State.SessionId;var oldConfig=State.ConfigRevision;var oldPhase=State.Phase;
            bool oldReady=(int)player<State.Players.Length && State.Players[(int)player].Ready;
            bool fresh=(int)player<State.Players.Length && command.SessionId==oldStateEpoch && command.Sequence>simulation.LastSequence(player);
            var result = publicationSequence==ulong.MaxValue
                ? new CommandResult{Sequence=command.Sequence,Code=ErrorCode.RevisionExhausted,Tick=State.Tick}
                : simulation.Execute(player, command);
            Content=simulation.Content;SelectedMapIndex=MapIndex(Content.Map.Id);nextMatchRules=Content.MatchRules;
            if(State.ValidPlayer(LocalSlot))LocalTowerMask=State.Players[(int)LocalSlot].AvailableTowerMask;
            bool changed=result.Accepted && fresh;
            if(command.Type==CommandType.ApplyMatchRules && State.ConfigRevision==oldConfig)changed=false;
            if(result.Accepted && command.Type==CommandType.SetReady && State.Phase==oldPhase && State.Players[(int)player].Ready==oldReady)changed=false;
            if(changed)BumpPublication();
            if(result.Accepted && State.SessionId!=oldStateEpoch)previousEpoch=oldStateEpoch;
            if (observer != null) observer(player, command, result, before, WireProtocol.EncodeSnapshot(State));
            return result;
        }

        private void AfterAuthorityCommand(ulong oldEpoch, CommandResult result, CommandType type)
        {
            if (result.Accepted && Mode == SessionMode.Offline && type == CommandType.SetSpeedVote && State.Phase == MatchPhase.Playing
                && State.Players[1].SpeedReady != State.Players[(int)LocalSlot].SpeedReady)
            {
                // Mirror the authoritative current vote, never an old accepted receipt's payload.
                var mirror = MakeCommand(PlayerSlot.One, CommandType.SetSpeedVote);
                mirror.Kind = State.Players[(int)LocalSlot].SpeedReady ? 1 : 0;
                ExecuteObserved(PlayerSlot.One, mirror);
            }
            if (result.Accepted && Mode == SessionMode.Offline && type == CommandType.SetWaveReady
                && State.Phase == MatchPhase.Playing && State.WavePhase == WavePhase.Preparing
                && State.Players[1].WaveReady != State.Players[(int)LocalSlot].WaveReady)
            {
                var mirror = MakeCommand(PlayerSlot.One, CommandType.SetWaveReady);
                mirror.Kind = State.Players[(int)LocalSlot].WaveReady ? 1 : 0; mirror.Target = -1;mirror.ExpectedWave=State.WaveId;
                ExecuteObserved(PlayerSlot.One, mirror);
            }
            if (result.Accepted && Mode == SessionMode.Offline && type == CommandType.RequestRematch && State.Phase == MatchPhase.Finished)
                ExecuteObserved(PlayerSlot.One, MakeCommand(PlayerSlot.One, CommandType.RequestRematch));
            if (result.Accepted && State.SessionId != oldEpoch)
            {
                RefreshEpoch();
                if (Mode == SessionMode.Offline) ExecuteObserved(PlayerSlot.One, MakeCommand(PlayerSlot.One, CommandType.SetReady));
                else SendWelcome();
            }
            UpdatePhase();
            // Purchases get a reliable result; the next 5-Hz snapshot reconciles visible state.
            // Lobby/countdown/rematch decisions additionally receive a reliable complete state.
            if (result.Accepted && (type == CommandType.ApplyMatchRules || type == CommandType.SetTeam || type == CommandType.SetLoadout || type == CommandType.SetReady || type == CommandType.RequestRematch || type == CommandType.SetSpeedVote
                || type == CommandType.SetWaveReady || type == CommandType.ResearchUnit || type == CommandType.SetTowerPriority)) SendReliableState();
            // A burst of paid purchases must not turn the 5-Hz stream into one full snapshot per command/frame.
            RaiseChanged();
        }

        private void OnResult(ulong sender, FastBufferReader reader)
        {
            if (!FromHost(sender) || !ReadBody(reader, WireProtocol.ResultBytes, out var body)
                || !WireProtocol.TryDecodeResult(body, out var epoch, out var result,out var publication)) return;
            DiagnosticPublicationSequence=publication;DiagnosticAcknowledgement?.Invoke(epoch, result);
            if (!pending.Remove((epoch,result.Sequence))) return;
            Resolve(result,epoch,publication);
        }

        private void Resolve(CommandResult result,ulong epoch,ulong publication)
        {
            LastResultEpoch=epoch;LastResultPublicationSequence=publication;
            LastResult = result;
            Message = ErrorText(result.Code);
            CommandResolved?.Invoke(result); RaiseChanged();
        }

        public static string ErrorText(ErrorCode code)
        {
            switch (code)
            {
                case ErrorCode.StaleLobbyRevision:return "Die Lobbyregeln haben sich geändert. Bitte erneut prüfen.";
                case ErrorCode.MissingPlayers:return "Warte auf alle Spieler und gleich große Teams.";
                case ErrorCode.RulesPayloadTooLarge:return "Der exakte Goldwert überschreitet das technische Übertragungslimit.";
                case ErrorCode.UnsupportedWaveCount:return "Es sind Wellen " + MatchRulesPolicy.MinWaveCount + " bis " + MatchRulesPolicy.MaxWaveCount + " verfügbar.";
                case ErrorCode.RevisionExhausted:return "Die technische Revisionsgrenze ist erreicht. Bitte einen neuen Raum öffnen.";
                case ErrorCode.None: return "Aktion angenommen.";
                case ErrorCode.InsufficientGold: return "Dafür fehlt noch Gold.";
                case ErrorCode.Cooldown: return "Diese Truppe ist noch in Abklingzeit.";
                case ErrorCode.QueueFull: return "Die Spawn-Warteschlange ist voll. Es wurde kein Gold abgezogen.";
                case ErrorCode.Occupied: return "Dieser Bauplatz ist bereits belegt.";
                case ErrorCode.NotOwner: return "Du kannst nur deine eigenen Türme verändern.";
                case ErrorCode.MaxLevel: return "Der Turm ist bereits vollständig ausgebaut.";
                case ErrorCode.ClassLimit: return "Du hast die maximale Anzahl dieser Turmklasse erreicht.";
                case ErrorCode.OutsideMap: return "Der Turm muss vollständig auf der Karte stehen.";
                case ErrorCode.OnPath: return "Halte genügend Abstand zum Truppenweg.";
                case ErrorCode.BlockedArea: return "In diesem Gelände kann kein Turm stehen.";
                case ErrorCode.TooClose: return "Dieser Platz liegt zu nah an einem anderen Gebäude oder einer Mine.";
                case ErrorCode.WrongPhase: return "Diese Aktion ist jetzt noch nicht möglich.";
                case ErrorCode.QuoteChanged: return "Das Angebot hat sich geändert. Zielwelle und Forschungsstufe erneut prüfen.";
                case ErrorCode.WaveCapacity: return "In den verbleibenden Wellen ist keine Kapazität für dieses Paket frei.";
                case ErrorCode.TowerLocked: return "Diese Turmklasse ist in dieser Partie nicht freigeschaltet.";
                case ErrorCode.WrongSession: return "Diese Aktion gehört zu einer älteren Partie.";
                case ErrorCode.Duplicate: case ErrorCode.StaleSequence: return "Diese Aktion wurde bereits verarbeitet.";
                case ErrorCode.RateLimited: return "Zu viele Aktionen. Bitte einen Moment warten.";
                default: return "Die Aktion ist ungültig. Es wurde kein Gold abgezogen.";
            }
        }

        private void SendWelcome()=>QueueStateTransfer(3);

        private void QueueStateTransfer(int kind)
        {
            if(!CanSendToRemote)return;
            foreach(var peer in peers.Values)
            {
                if(!peer.Connected)continue;
                if(peer.Outgoing!=null&&peer.Outgoing.Epoch!=State.SessionId)peer.Outgoing=null;
                peer.Queued=Math.Max(peer.Queued,kind);if(peer.Outgoing==null)BeginStateTransfer(peer);if(!CanSendToRemote)return;
            }
        }
        private void BeginStateTransfer(Peer peer)
        {
            if(!CanSendToRemote||!peer.Connected||peer.Queued==0)return;
            if(snapshotSequence==uint.MaxValue){LoseConnection("Übertragungsrevision erschöpft. Bitte einen neuen Raum öffnen.");return;}
            int kind=peer.Queued;peer.Queued=0;uint revision=++snapshotSequence;
            var snapshot=WireProtocol.EncodeSnapshot(State);string name=kind==3?WelcomeMessage:kind==2?StateMessage:SnapshotMessage;
            var body=EncodePeerEnvelope(peer,revision);
            int count=(int)(((long)body.Length+WireProtocol.ChunkPayloadBytes-1)/WireProtocol.ChunkPayloadBytes);
            var delivery=kind!=1||count>20?NetworkDelivery.ReliableSequenced:NetworkDelivery.Unreliable;
            peer.Outgoing=new StateTransfer{Name=name,Epoch=State.SessionId,Revision=revision,Body=body,Count=count,Delivery=delivery,Window=delivery==NetworkDelivery.Unreliable?null:new StateTransferWindow(count)};
            ObserveSnapshot(snapshot,State);
        }
        private byte[] EncodePeerEnvelope(Peer peer,uint revision)
            =>WireProtocol.EncodeWelcome(CatalogHash,(byte)SelectedMapIndex,revision,State,Content,publicationSequence,peer.JoinEpoch==State.SessionId?0:previousEpoch,peer.Slot,simulation.LastSequence(peer.Slot));

        private void PumpStateTransfer()
        {
            if(!CanSendToRemote)return;
            sendPeers.Clear();foreach(var peer in peers.Values)if(peer.Connected)sendPeers.Add(peer);
            if(sendPeers.Count==0)return;int budget=32;
            for(int visited=0;visited<sendPeers.Count&&budget>0;visited++)
            {
                var peer=sendPeers[(transferCursor+visited)%sendPeers.Count];if(peer.Outgoing==null)BeginStateTransfer(peer);if(peer.Outgoing==null)continue;
                var transfer=peer.Outgoing;int allowance=Math.Min(budget,sendPeers.Count==1?20:8);
                while(allowance-->0&&transfer.Part<transfer.Count)
                {
                    int part=transfer.Part;if(transfer.Window!=null&&!transfer.Window.TryTake(out part))break;
                    Send(transfer.Name,peer.Id,WireProtocol.EncodeChunk(transfer.Epoch,transfer.Revision,transfer.Body,part),transfer.Delivery);transfer.Part++;budget--;
                }
                if(transfer.Window==null&&transfer.Part==transfer.Count||transfer.Window!=null&&transfer.Window.Complete)peer.Outgoing=null;
            }
            transferCursor=(transferCursor+1)%sendPeers.Count;
        }
        void OnPageAcknowledged(ulong sender,FastBufferReader reader)
        {
            if(!CanSendToRemote||!peers.TryGetValue(sender,out var peer)||peer.Outgoing?.Window==null||!ReadBody(reader,16,out var bytes)||bytes.Length!=16)return;
            using var stream=new MemoryStream(bytes,false);using var input=new BinaryReader(stream);
            if(input.ReadUInt64()!=peer.Outgoing.Epoch||input.ReadUInt32()!=peer.Outgoing.Revision)return;
            if(peer.Outgoing.Window.Acknowledge(input.ReadInt32()))peer.LastProgress=Now;
        }
        void AcknowledgePage(byte[] page)
        {
            // Called only after a validated page made progress. Reliable ordered messages
            // preserve this acknowledgement order, including the final applied page.
            var receipt=new byte[16];Buffer.BlockCopy(page,0,receipt,0,12);Buffer.BlockCopy(page,16,receipt,12,4);
            Send(PageAckMessage,NetworkManager.ServerClientId,receipt,NetworkDelivery.ReliableSequenced);
        }

        private void OnWelcome(ulong sender, FastBufferReader reader)
        {
            if(!FromHost(sender) || !ReadBody(reader,WireProtocol.ChunkPayloadBytes+WireProtocol.ChunkHeaderBytes,out var page))return;
            var complete=welcomeAssembler.TryAdd(page,Now,out var body,out var accepted);
            if(accepted){AcknowledgePage(page);connectBegan=lastPeerProgress=Now;}
            if(!complete)return;
            ApplyStateEnvelope(body,welcomeAssembler.CompletedSequence);
        }

        private bool ApplyStateEnvelope(byte[] body,uint transferRevision)
        {
            if(!WireProtocol.TryDecodeWelcome(body,CatalogHash,catalog,out var mapIndex,out var revision,out var publication,
                out var priorEpoch,out var assignedSlot,out var recipientCommandSequence,out var effective,out var next,out var reason))
            {LoseConnection(reason);return false;}
            if(revision!=transferRevision)return false;
            bool initialAssignment=receivedState==null;
            if(initialAssignment)
            {if(next.Players[(int)assignedSlot].AvailableTowerMask!=LocalTowerMask)return false;}
            else if(assignedSlot!=LocalSlot)return false;
            if(!publicationGate.TryAccept(next,publication,priorEpoch,requestedRematchEpoch,(int)assignedSlot))return false;
            LocalSlot=assignedSlot;SelectedMapIndex=mapIndex;Content=effective;receivedState=next;LocalTowerMask=next.Players[(int)LocalSlot].AvailableTowerMask;RefreshEpoch();
            if(initialAssignment)localSequence=recipientCommandSequence;
            appliedSnapshotSequence=revision;ConnectedPlayers=ConnectedCount(next.ConnectedPlayerMask);lastPeerProgress=lastSimulationProgress=Now;
            ObserveSnapshot(WireProtocol.EncodeSnapshot(next),next);UpdatePhase();RaiseChanged();return true;
        }

        private void SendReliableState()=>QueueStateTransfer(2);

        private void OnReliableState(ulong sender, FastBufferReader reader)
        {
            if(!FromHost(sender) || receivedState==null || !ReadBody(reader,WireProtocol.ChunkPayloadBytes+WireProtocol.ChunkHeaderBytes,out var page))return;
            bool complete=reliableAssembler.TryAdd(page,Now,out var snapshot,out var accepted);
            if(accepted){AcknowledgePage(page);lastSimulationProgress=lastPeerProgress=Now;}
            if(complete)ApplyStateEnvelope(snapshot,reliableAssembler.CompletedSequence);
        }

        private void BroadcastSnapshot()=>QueueStateTransfer(1);

        private void OnSnapshotChunk(ulong sender, FastBufferReader reader)
        {
            if(!FromHost(sender) || receivedState==null || !ReadBody(reader,WireProtocol.ChunkPayloadBytes+WireProtocol.ChunkHeaderBytes,out var page))return;
            bool complete=assembler.TryAdd(page,Now,out var snapshot,out var accepted);
            if(accepted){if(BitConverter.ToInt32(page,20)>20)AcknowledgePage(page);lastSimulationProgress=lastPeerProgress=Now;}
            if(complete)ApplyStateEnvelope(snapshot,assembler.CompletedSequence);
        }

        private void ObserveSnapshot(byte[] body, MatchState state)
        {
            LastSnapshotTick = state.Tick;
            // Hashes are computed only while the integration driver has an observer.
            if (SnapshotObserved != null)
            { LastSnapshotHash = WireProtocol.Digest(body); SnapshotObserved.Invoke(state.SessionId, state.Tick, LastSnapshotHash); }
        }

        private void Update()
        {
            UpdateDiscovery(Now);
            if (Content == null || stopping || paused || Status == ConnectionState.Idle || Status == ConnectionState.ConnectionLost) return;
            var now = Now;
            var elapsed = Math.Max(0, now - lastUpdateAt); lastUpdateAt = now;
            if (Status == ConnectionState.Connecting && now - connectBegan > ConnectingTimeout)
            { LoseConnection("Zeitüberschreitung. Host-IP/Code prüfen. Im WLAN müssen beide Geräte im selben Netz ohne Gastnetz-/AP-Isolation sein."); return; }
            if (IsHost && simulation != null)
            {
                accumulator += elapsed;
                if (Mode != SessionMode.Offline && accumulator > ProgressTimeout)
                { LoseConnection("Die Host-Simulation ist zu weit im Rückstand. Das Duell wurde abgebrochen."); return; }
                var steps = 0;
                while (steps++ < 8)
                {
                    var tickTime = 1.0 / Content.TickRate;
                    if (accumulator < tickTime) break;
                    if(publicationSequence==ulong.MaxValue){LoseConnection("Publikationsrevision erschöpft. Bitte einen neuen Raum öffnen.");return;}
                    if (Mode == SessionMode.Offline && State.Phase == MatchPhase.Playing && State.WavePhase == WavePhase.Preparing)
                        bot.Tick(simulation);
                    if (Mode == SessionMode.Offline) simulation.Step(BeforeOfflineGameplayStep);
                    else simulation.Step();
                    BumpPublication(); accumulator -= tickTime;
                    // Core decides 0/1/2 inner gameplay steps; wall-time/control frequency never changes.
                    UpdatePhase();
                }
                if (State.Tick != lastTick) { lastTick = State.Tick; lastSimulationProgress = now; }
                UpdatePhase();
            }
            if (Mode == SessionMode.Offline) return;
            if(IsHost)
            {
                stalePeers.Clear();foreach(var peer in peers.Values)
                    if(!peer.Connected&&now-peer.JoinedAt>ConnectingTimeout||peer.Connected&&now-peer.LastProgress>ProgressTimeout)stalePeers.Add(peer.Id);
                foreach(var id in stalePeers){manager.DisconnectClient(id);OnDisconnected(id);if(Status==ConnectionState.ConnectionLost)return;}
            }
            else if(ConnectedPlayers>0&&now-lastPeerProgress>ProgressTimeout)
            {LoseConnection("Der Host antwortet nicht mehr. Das Match wurde abgebrochen.");return;}
            if(!IsHost&&(State?.Phase==MatchPhase.Playing||State?.Phase==MatchPhase.Countdown)&&now-lastSimulationProgress>ProgressTimeout)
            {LoseConnection("Die Host-Simulation läuft nicht weiter. Das Match wurde abgebrochen.");return;}
            if(State!=null&&now>=nextHeartbeatAt)
            {
                nextHeartbeatAt=now+1;if(heartbeatSequence==uint.MaxValue){LoseConnection("Heartbeatrevision erschöpft.");return;}
                using var stream=new MemoryStream(12);using var writer=new BinaryWriter(stream);writer.Write(State.SessionId);writer.Write(++heartbeatSequence);
                var body=stream.ToArray();if(IsHost){foreach(var peer in peers.Values)if(peer.Connected)Send(HeartbeatMessage,peer.Id,body,NetworkDelivery.Unreliable);}
                else Send(HeartbeatMessage,NetworkManager.ServerClientId,body,NetworkDelivery.Unreliable);
            }
            if (IsHost && now >= nextSnapshotAt)
            { nextSnapshotAt = now + .2; BroadcastSnapshot(); RaiseChanged(); }
            if(IsHost)PumpStateTransfer();
            foreach (var item in pending)
            {
                if (now - item.Value > ProgressTimeout)
                { LoseConnection("Eine Aktion wurde nicht bestätigt. Das Duell wurde sicher abgebrochen."); break; }
            }
        }

        private void OnHeartbeat(ulong sender,FastBufferReader reader)
        {
            if(stopping||!ReadBody(reader,12,out var body)||body.Length!=12)return;
            using var stream=new MemoryStream(body,false);using var binary=new BinaryReader(stream);var epoch=binary.ReadUInt64();var sequence=binary.ReadUInt32();
            if(State==null||epoch!=State.SessionId)return;
            if(IsHost)
            {if(!peers.TryGetValue(sender,out var peer)||!peer.Connected||sequence<=peer.Heartbeat)return;peer.Heartbeat=sequence;peer.LastProgress=Now;}
            else{if(sender!=NetworkManager.ServerClientId||sequence<=lastPeerHeartbeat)return;lastPeerHeartbeat=sequence;lastPeerProgress=Now;}
        }
        private void OnLeave(ulong sender,FastBufferReader reader)
        {
            if(stopping||IsHost&&!peers.ContainsKey(sender)||!IsHost&&sender!=NetworkManager.ServerClientId)return;
            if(!ReadBody(reader,1,out var body)||body.Length!=1)return;
            if(IsHost&&State?.Phase==MatchPhase.Lobby){manager.DisconnectClient(sender);OnDisconnected(sender);}
            else LoseConnection("Ein Spieler hat das Match verlassen. Es gibt keinen Sieger.");
        }

        private void UpdatePhase()
        {
            if (State == null || Status == ConnectionState.ConnectionLost || stopping) return;
            var phase = State.Phase;
            var changed = phase != lastPhase || State.WaveId != lastWaveId || State.WavePhase != lastWavePhase;
            lastPhase = phase;
            lastWaveId = State.WaveId; lastWavePhase = State.WavePhase;
            switch (phase)
            {
                case MatchPhase.Lobby: Status = ConnectionState.Lobby; break;
                case MatchPhase.Countdown: Status = ConnectionState.Loading; break;
                case MatchPhase.Playing: Status = ConnectionState.Playing; break;
                case MatchPhase.Finished: Status = ConnectionState.Finished; break;
                case MatchPhase.Aborted: Status = ConnectionState.ConnectionLost; break;
            }
            if (changed)
            {
                lastSimulationProgress = Now;
                if (IsHost) SendReliableState();
                RaiseChanged();
            }
        }

        private void RefreshEpoch()
        {
            if (State == null || State.SessionId == localEpoch) return;
            localEpoch = State.SessionId; localSequence = 0; lastTick = -1;
            accumulator = 0; lastUpdateAt = Now;
            assembler.Reset(localEpoch); reliableAssembler.Reset(localEpoch); lastSimulationProgress = Now;
        }

        private void BumpPublication()
        {
            if(publicationSequence==ulong.MaxValue)throw new InvalidOperationException("Authoritative publication sequence exhausted.");
            publicationSequence++;
        }

        private void BeforeOfflineGameplayStep(MatchSimulation activeSimulation) => bot.Tick(activeSimulation);

        private bool FromHost(ulong sender) => !IsHost && !stopping && networkStarted && sender == NetworkManager.ServerClientId;
        private bool CanSendToRemote => IsHost && Mode != SessionMode.Offline && !stopping && manager != null
            && manager.IsListening && peers.Count>0 && ConnectedPlayers>1 && State != null;

        private void Send(string name, ulong peer, byte[] body, NetworkDelivery delivery)
        {
            if (manager == null || !manager.IsListening || manager.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(body.Length + sizeof(int), Allocator.Temp);
            writer.WriteValueSafe(body.Length); writer.WriteBytesSafe(body);
            manager.CustomMessagingManager.SendNamedMessage(name, peer, writer, delivery);
            SentBytes += body.Length + sizeof(int);
        }

        private bool ReadBody(FastBufferReader reader, int maximum, out byte[] body)
        {
            body = null;
            if (reader.Length - reader.Position < sizeof(int)) return false;
            reader.ReadValueSafe(out int length);
            if (length <= 0 || length > maximum || length != reader.Length - reader.Position) return false;
            body = new byte[length]; reader.ReadBytesSafe(ref body, length); ReceivedBytes += length + sizeof(int); return true;
        }

        private void FailStart(string reason) => LoseConnection(reason);

        private void LoseConnection(string reason)
        {
            if (stopping || Status == ConnectionState.ConnectionLost) return;
            generation++; operation?.Cancel(); operation?.Dispose(); operation = null; StopAllCoroutines();
            var hadResult = State != null && State.Phase == MatchPhase.Finished;
            if (simulation != null && !hadResult) simulation.Abort();
            if (receivedState != null && !hadResult) { receivedState.Phase = MatchPhase.Aborted; receivedState.Outcome = MatchOutcome.Aborted; }
            StopNetwork(); pending.Clear(); ConnectedPlayers = IsHost ? 1 : 0;
            SetStatus(hadResult ? ConnectionState.Finished : ConnectionState.ConnectionLost,
                hadResult ? "Verbindung beendet. Das bestätigte Ergebnis bleibt bestehen; für eine neue Partie zurück ins Menü." : reason);
        }

        public void Shutdown()
        {
            generation++;
            operation?.Cancel(); operation?.Dispose(); operation = null;
            StopAllCoroutines();
            if(!stopping&&manager!=null&&manager.IsListening)
            {if(IsHost){foreach(var peer in peers.Values)if(peer.Connected)Send(LeaveMessage,peer.Id,new byte[]{1},NetworkDelivery.ReliableSequenced);}
                else if(State!=null)Send(LeaveMessage,NetworkManager.ServerClientId,new byte[]{1},NetworkDelivery.ReliableSequenced);}
            StopNetwork();
            simulation = null; receivedState = null; bot = null; pending.Clear(); ConnectedPlayers = 0;
            JoinCode = string.Empty; localEpoch = 0; localSequence = 0; accumulator = 0;
            RelayAllocationEstablished = false; AuthenticationProfile = string.Empty; AnonymousPlayerFingerprint = string.Empty;
            if(catalog!=null)Content=catalog[SelectedMapIndex];
            publicationGate.Reset();publicationSequence=previousEpoch=requestedRematchEpoch=0;
            Status = ConnectionState.Idle; Message = "Bereit."; RaiseChanged();
        }

        private void StopNetwork()
        {
            stopping = true; networkStarted = false; UnregisterMessages();
            peers.Clear();assembler.Reset(0);reliableAssembler.Reset(0);welcomeAssembler.Reset(0);
            if (manager != null && manager.IsListening && !manager.ShutdownInProgress) manager.Shutdown();

        }

        private void OnApplicationPause(bool isPaused)
        {
            applicationPaused=isPaused;if(isPaused){discovery?.Suspend();multicastLease.SetActive(false);}
            if (Status == ConnectionState.Idle) return;
            if (Mode == SessionMode.Offline) { paused = isPaused; accumulator = 0; lastUpdateAt = Now; return; }
            if (isPaused) LoseConnection("Die App war im Hintergrund. Für eine verlässliche Partie wurde die Verbindung beendet.");
        }

        private void OnApplicationQuit() => Shutdown();
        private void OnDestroy()
        {
            destroying = true; Shutdown();discovery?.Dispose();multicastLease.Dispose();
            if (manager != null)
            {
                manager.OnClientConnectedCallback -= OnConnected;
                manager.OnClientDisconnectCallback -= OnDisconnected;
                manager.OnTransportFailure -= OnTransportFailure;
                manager.ConnectionApprovalCallback = null;
            }
            if (networkObject != null) Destroy(networkObject);
        }

        private void SetStatus(ConnectionState state, string message) { Status = state; Message = message; RaiseChanged(); }
        private void RaiseChanged() { if (!destroying) Changed?.Invoke(); }
        private static double Now => Time.realtimeSinceStartupAsDouble;
        private static ulong NewEpoch() { var value = BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 0); return value == 0 ? 1 : value; }
        private static string SafeError(Exception error)
        {
            // Do not expose SDK response bodies, access tokens or allocation credentials in the UI/logs.
            if (error is TimeoutException) return "Zeitüberschreitung.";
            if (error is ArgumentException) return "Verbindungsdaten sind ungültig.";
            if (error is RequestFailedException serviceError) return "Dienstfehler " + serviceError.ErrorCode + " (" + error.GetType().Name + ").";
            return "Fehlerart: " + error.GetType().Name + ".";
        }
    }
}
