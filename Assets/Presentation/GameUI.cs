using BigInteger = System.Numerics.BigInteger;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI : MonoBehaviour
    {
        [Header("Optional Kenney art; geometric class icons are always available")]
        public Sprite[] TowerSprites = new Sprite[4];
        public Sprite[] UnitSprites = new Sprite[4];
        public AudioClip ClickSound, ConfirmSound, ErrorSound, BackSound;
        public Font DisplayFont;
        SessionCoordinator session;
        GameContent content;
        TowerDefinition[] towers;
        UpgradeDefinition[][] upgrades;
        UnitDefinition[] units;
        MapPoint[] buildSites;
        Canvas canvas;
        Image canvasBackground;
        RectTransform safe, screen, modal;
        GameObject ownedEventSystem;
        LocalSettings settings;
        GameAudio gameAudio;
        string page="home", connectionPage="", notice="", formAddress="", formPort="7777", formCode="";
        double nextRefresh, feedbackUntil;
        float width, height;
        int selectedKind=0, selectedSlot=-1, selectedTower=-1, tutorialStep=-1;
        bool dirty=true, showEnemy, isInitialized, onlineBusy;
        Text statusText, goldText, incomeText, hpText, timeText, opponentText, queueText, selectionText, tutorialText, noticeText;
        Button confirmBuild, upgradeButton, sellButton, cancelSelection, readyButton, rematchButton;
        Button[] buildButtons, sendButtons;
        BoardView ownBoard, enemyBoard, menuBoard;
        readonly PresentationCommandTracker commands=new PresentationCommandTracker();
        readonly List<PresentationCommandContext> readyFeedback=new List<PresentationCommandContext>(12);
        readonly List<PresentationCommandContext> pendingFeedback=new List<PresentationCommandContext>(32);
        readonly List<RaycastResult> placementHits=new List<RaycastResult>(16);
        PointerEventData placementPointer;
        EventSystem placementEventSystem;
        BuildDragSource dragSource;
        RectTransform dragChip;
        Text dragLabel;
        Vector2 dragPosition;
        Camera dragCamera;
        int dragPointer,dragKind,dragSlot=-1;
        ulong dragSession;
        bool dragging;
        float dragBegan;
        MapPoint dragPoint,selectedPoint;
        bool dragHasGround,hasSelectedPoint;
        int selectedSite=-1;
        ulong displayedSession;
        Rect lastSafe;
        public bool TutorialActive => tutorialStep>=0;
        public string CurrentScreen => page;

        public void Initialize(SessionCoordinator value)
        {
            if(isInitialized)Detach();session=value;CacheContent();
            settings=new LocalSettings();ButtonPressFeedback.SetReducedEffects(settings.ReducedEffects);gameAudio=gameObject.AddComponent<GameAudio>();gameAudio.Initialize(settings);
            UiKit.Font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root=new GameObject("Defense Duell UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=1;
            var bg=UiKit.Rect("Background",root.transform);UiKit.Fill(bg);canvasBackground=bg.gameObject.AddComponent<Image>();canvasBackground.color=UiKit.Ink;canvasBackground.raycastTarget=false;
            safe=UiKit.Rect("Safe area",root.transform);UiKit.Fill(safe);
            if(EventSystem.current==null)
            {
                ownedEventSystem=new GameObject("Defense Duell EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                ownedEventSystem.transform.SetParent(transform,false);
                ownedEventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            session.Changed+=Changed;session.CommandResolved+=Resolved;
            isInitialized=true;ApplySafeArea();Canvas.ForceUpdateCanvases();Rebuild();
        }
        void Changed(){dirty=true;}
        void CacheContent()
        {
            ResetSpeedVote();ResetIncomeFeedback();CloseAttackInfo();CancelInteraction();commands.Reset(session.State?.SessionId??0);
            content=session.Content;towers=content.Towers;units=content.Units;buildSites=content.Map.BuildSlots;hasSelectedPoint=false;selectedSite=-1;
            upgrades=new UpgradeDefinition[towers.Length][];for(int i=0;i<towers.Length;i++)upgrades[i]=towers[i].Upgrades;
            selectedKind=Mathf.Clamp(selectedKind,0,towers.Length-1);selectedSlot=selectedTower=-1;
        }
        void Detach()
        {
            if(session!=null){session.Changed-=Changed;session.CommandResolved-=Resolved;}
            if(canvas!=null)Destroy(canvas.gameObject);if(ownedEventSystem!=null)Destroy(ownedEventSystem);if(gameAudio!=null)Destroy(gameAudio);
            ResetSpeedVote();CloseAttackInfo();CancelInteraction();commands.Reset(0);readyFeedback.Clear();isInitialized=false;
        }
        void OnDestroy(){Detach();}
        void OnEnable(){if(isInitialized)ResetIncomeFeedback();}
        void OnDisable(){if(session!=null)session.LanDiscoveryEnabled=false;ResetIncomeFeedback();CloseModal();CloseAttackInfo();CancelInteraction();commands.DiscardVisuals();CancelBoardFeedback();hasSelectedPoint=false;}
        void OnApplicationPause(bool pause){ResetIncomeFeedback();if(pause){CloseAttackInfo();CancelInteraction();commands.DiscardVisuals();CancelBoardFeedback();hasSelectedPoint=false;}}
        void OnApplicationFocus(bool focused){ResetIncomeFeedback();if(!focused){CloseAttackInfo();CancelInteraction();commands.DiscardVisuals();CancelBoardFeedback();hasSelectedPoint=false;}}
        void Update()
        {
            if(!isInitialized)return;
            if(dragSource!=null&&(session.State==null||session.State.SessionId!=dragSession||session.State.Phase!=MatchPhase.Playing||page!="game"||showEnemy||modal!=null))CancelInteraction();
            if(Screen.safeArea!=lastSafe||Mathf.Abs(safe.rect.width-width)>2||Mathf.Abs(safe.rect.height-height)>2)
            {ApplySafeArea();Canvas.ForceUpdateCanvases();Rebuild();}
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Back();
            if(modal!=null)UiPointerGate.RestrictNavigation(modal);
            if(Time.unscaledTimeAsDouble>=nextRefresh||dirty)
            {
                nextRefresh=Time.unscaledTimeAsDouble+.1;dirty=false;
                bool contentChanged=content.ContentHash!=session.Content.ContentHash;if(contentChanged)CacheContent();
                string wanted=DesiredPage();if(wanted!=page||contentChanged){page=wanted;selectedSlot=-1;selectedTower=-1;hasSelectedPoint=false;Rebuild();}
                RefreshLive();RefreshDiscovery();
            }
            if(dragging)RefreshDragPreview();
            RefreshIncomeFeedbackVisual();
        }
        void ApplySafeArea()
        {
            lastSafe=Screen.safeArea;
            if(Screen.width<=0||Screen.height<=0)return;
            safe.anchorMin=new Vector2(lastSafe.xMin/Screen.width,lastSafe.yMin/Screen.height);safe.anchorMax=new Vector2(lastSafe.xMax/Screen.width,lastSafe.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
        }
        string DesiredPage()
        {
            if(session.Status==ConnectionState.ConnectionLost)return "lost";
            if(session.Status==ConnectionState.Connecting)return "connecting";
            var state=session.State;
            if(state==null||session.Status==ConnectionState.Idle)return string.IsNullOrEmpty(connectionPage)?"home":"connect";
            if(state.Phase==MatchPhase.Lobby)return "lobby";
            if(state.Phase==MatchPhase.Finished||state.Phase==MatchPhase.Aborted)return state.Phase==MatchPhase.Aborted?"lost":"result";
            return "game";
        }
        void Rebuild()
        {
            if(safe==null)return;
            // Paint through device cutout insets; only controls belong inside the safe area.
            canvasBackground.color=UiKit.Hex(0x091B2B);
            ResetIncomeFeedback();DestroyFullScreenBoard();
            CloseAttackInfo();CancelInteraction();commands.DiscardVisuals();CancelBoardFeedback();hasSelectedPoint=false;
            width=safe.rect.width;height=safe.rect.height;
            if(width<100){width=1280;height=720;}
            if(screen!=null){screen.gameObject.SetActive(false);Destroy(screen.gameObject);}
            CloseModal();
            screen=UiKit.Rect("Screen "+page,safe);UiKit.Fill(screen);
            discoveryBanner=null;discoveryLobbyId=null;
            ownBoard=enemyBoard=menuBoard=null;statusText=goldText=incomeText=hpText=timeText=opponentText=queueText=selectionText=tutorialText=noticeText=null;
            confirmBuild=upgradeButton=sellButton=cancelSelection=readyButton=rematchButton=null;buildButtons=sendButtons=null;attackInfo=null;infoKind=-1;
            lobbyRulesPanel=null; if(page!="lobby")ResetLobbyPresentation();
            if(page!="game"&&page!="home"&&page!="connect"&&page!="lobby")Header();
            switch(page)
            {
                case "home":Home();break;
                case "connect":Connect();break;
                case "connecting":Connecting();break;
                case "lobby":Lobby();break;
                case "game":Game();break;
                case "result":Result();break;
                case "lost":Lost();break;
            }
            RefreshLive();
        }
        void Header()
        {
            UiKit.TextAt(screen,"D / D",24,12,130,44,28,UiKit.Mint).fontStyle=FontStyle.Bold;
            UiKit.TextAt(screen,page=="home"?"DEFENSE DUELL":ModeName()+"  ·  "+content.Map.Name,146,15,width-500,40,17,UiKit.Muted);
            UiKit.ButtonAt(screen,"Ton & Anzeige",width-268,12,244,52,OpenSettings,UiKit.PanelLight,18);
            UiKit.PanelAt(screen,"Header rule",24,71,width-48,1,UiKit.PanelLight);
        }
        void Home()=>PlayfulHome();
        void AddDemoTower(MatchState state,int kind)
        {
            int start=(kind*7)%buildSites.Length;
            for(int n=0;n<buildSites.Length;n++)
            {
                int index=(start+n)%buildSites.Length,site=content.Map.FreePlacement?-1:index;
                var point=buildSites[index];
                if(PlacementValidator.ValidatePosition(content,state,PlayerSlot.Zero,kind,site,point)!=ErrorCode.None)continue;
                state.Towers.Add(new TowerState{Id=kind+1,Owner=PlayerSlot.Zero,Kind=kind,Level=kind==0?1:0,DamageLevel=kind==0?1:0,Slot=state.Towers.Count,SiteId=site,Position=point});return;
            }
            if(towers[kind].IsEconomy&&!content.Map.FreePlacement)return;
            for(int y=90;y<content.Map.Height-90;y+=110)for(int x=90;x<content.Map.Width-90;x+=110)
            {
                var point=new MapPoint(x,y);if(PlacementValidator.ValidatePosition(content,state,PlayerSlot.Zero,kind,-1,point)!=ErrorCode.None)continue;
                state.Towers.Add(new TowerState{Id=kind+1,Owner=PlayerSlot.Zero,Kind=kind,Level=kind==0?1:0,DamageLevel=kind==0?1:0,Slot=state.Towers.Count,SiteId=-1,Position=point});return;
            }
        }
        void StartOffline(bool tutorial)
        {
            connectionPage="";tutorialStep=tutorial?0:-1;notice="";showEnemy=false;selectedSlot=-1;selectedKind=0;
            if(!ConfigureMenuMatch(true))return;session.StartOffline(1);page=DesiredPage();Rebuild();
        }
        void ShowConnect(string mode){connectionPage=mode;page="connect";notice="";Rebuild();}
        void Connect()=>BuildConnectScreen();
        void CaptureForm(){EventSystem.current?.SetSelectedGameObject(null);}
        bool ParsePort(out ushort port)
        {
            if(ushort.TryParse(formPort,out port)&&port>0)return true;
            notice="Bitte einen Port zwischen 1 und 65535 eingeben.";dirty=true;Sound(GameAudioCue.Error);return false;
        }
        async Task StartOnline(bool host)
        {
            if(onlineBusy)return;onlineBusy=true;notice="";
            try{if(host){if(!ConfigureMenuMatch(false))return;await session.HostOnlineAsync();}else await session.JoinOnlineAsync(formCode.Trim().ToUpperInvariant());}
            catch(Exception){notice="Online-Verbindung fehlgeschlagen. Bitte Einrichtung und Internet prüfen.";Sound(GameAudioCue.Error);}
            finally{onlineBusy=false;dirty=true;}
        }
        void Connecting()
        {
            var panel=StatusCard("VERBINDUNG", "Dein Raum wird vorbereitet", out float w);
            UiKit.TextAt(panel,"VERBINDUNG WIRD AUFGEBAUT",28,88,w-56,54,28,HudText).fontStyle=FontStyle.Bold;
            statusText=UiKit.TextAt(panel,session.Message,28,152,w-56,120,20,HudMuted);
            BattleButton(panel,"Abbrechen",28,296,w-56,64,ReturnMenu,HudCard,20).name="Cancel connection";
        }
        void Lobby()=>BuildLobbyScreen();
        RectTransform StatusCard(string tag,string subtitle,out float w)
        {
            w=Mathf.Min(820,width-48);float h=Mathf.Min(480,height-128);
            var panel=BattlePanel(screen,"Status card",(width-w)*.5f,100,w,h,HudPanel).rectTransform;
            UiKit.TextAt(panel,tag,28,20,w-56,28,15,HudGold).fontStyle=FontStyle.Bold;
            UiKit.TextAt(panel,subtitle,28,50,w-56,28,15,HudMuted);
            return panel;
        }
        void Result()
        {
            var state=session.State;string result=state==null?"PARTIE BEENDET":state.Outcome==MatchOutcome.Draw?"UNENTSCHIEDEN":
                (state.WinningTeamId>=0&&state.WinningTeamId==state.Players[(int)session.LocalSlot].TeamId)?"DEIN SIEG!":"GUT GEKÄMPFT.";
            var panel=StatusCard("DUELL BEENDET","Neue Runde, neuer Plan.",out float w);
            UiKit.TextAt(panel,result,28,88,w-56,70,40,HudText).fontStyle=FontStyle.Bold;
            string score=state==null?"":state.Players[(int)session.LocalSlot].Hp+" BASIS-LP";
            UiKit.TextAt(panel,score,28,158,w-56,44,26,HudGold);
            resultReward=UiKit.TextAt(panel,"",28,206,w-56,40,17,HudGold);
            UiKit.TextAt(panel,"Revanche: Alle Spieler bestätigen erneut.",28,256,w-56,38,18,HudMuted);
            float actionWidth=(w-68)*.5f;
            rematchButton=BattleButton(panel,"Revanche",28,310,actionWidth,64,()=>Send(CommandType.RequestRematch),HudAction,22);
            BattleButton(panel,"Hauptmenü",40+actionWidth,310,actionWidth,64,ReturnMenu,HudCard,22).name="Result main menu";
            statusText=UiKit.TextAt(panel,"",28,390,w-56,60,17,HudMuted);
        }
        void Lost()
        {
            var panel=StatusCard("VERBINDUNG BEENDET","Dein nächstes Duell wartet.",out float w);
            UiKit.TextAt(panel,"DUELL ABGEBROCHEN",28,88,w-56,64,34,HudText).fontStyle=FontStyle.Bold;
            UiKit.TextAt(panel,"Es wird kein Sieger gewertet.",28,160,w-56,48,23,HudGold);
            statusText=UiKit.TextAt(panel,session.Message,28,216,w-56,96,18,HudMuted);
            BattleButton(panel,"Zurück zum Hauptmenü",28,338,w-56,64,ReturnMenu,HudAction,22).name="Lost main menu";
        }
        void RefreshLive()
        {
            if(noticeText!=null)noticeText.text=notice;
            if(page=="connect"){if(noticeText!=null&&string.IsNullOrEmpty(notice)&&session.Message!="Bereit.")noticeText.text=session.Message;return;}
            var state=session.State;
            if(page=="connecting"||page=="lost"){if(statusText!=null)statusText.text=session.Message;return;}
            if(page=="home")RefreshHomeExtras();
            if(state==null)return;
            if(displayedSession!=state.SessionId){ResetIncomeFeedback();ResetSpeedVote();CloseAttackInfo();CancelInteraction();displayedSession=state.SessionId;commands.Reset(state.SessionId);readyFeedback.Clear();pendingFeedback.Clear();selectedSlot=-1;selectedTower=-1;hasSelectedPoint=false;}
            int local=(int)session.LocalSlot,other=BattleEnemySlot;
            if(page=="lobby")RefreshLobbyScreen();
            if(page=="result")
            {
                if(resultReward!=null){var progress=GetComponent<DefenseDuell.Progression.LocalProgress>();resultReward.text=progress!=null?(progress.LastReward>0?"+"+progress.LastReward+" Siegesmarken · insgesamt "+progress.Profile.Medals:progress.Message):"";}
                bool roomConnected=session.Mode==SessionMode.Offline||session.ConnectedPlayers==state.Players.Length;
                if(rematchButton!=null){rematchButton.interactable=roomConnected&&!state.Players[local].Rematch;UiKit.ButtonText(rematchButton,!roomConnected?"Raum beendet":state.Players[local].Rematch?"Revanche angefragt":"Revanche");}
                if(statusText!=null)statusText.text=roomConnected?RematchStatus():session.Message;
            }
            if(page!="game")return;
            var player=state.Players[local];
            hpText.text=player.Hp+"/"+(content.BaseHp*content.MatchRules.MembersPerTeam)+" LP";
            goldText.text=GameNumbers.Compact(player.Gold)+" G";
            RefreshIncomeHud();
            if(state.Phase==MatchPhase.Countdown)timeText.text="Start in "+Mathf.CeilToInt(state.CountdownRemaining/(float)content.TickRate);
            else timeText.text="W "+GameNumbers.Compact(state.WaveId)+"/"+WaveLimitLabel;
            RefreshTargetControl();
            opponentText.text=(showEnemy?"DU":"SPIELER "+(other+1))+" · "+state.Players[showEnemy?local:other].Hp+" LP";
            ownBoard.ReducedEffects=settings.ReducedEffects;enemyBoard.ReducedEffects=settings.ReducedEffects;ownBoard.PowerSaving=settings.PowerSaving;enemyBoard.PowerSaving=settings.PowerSaving;
            ownBoard.SetInteractive(!showEnemy&&state.Phase==MatchPhase.Playing&&player.Hp>0);
            commands.Observe(state,Time.unscaledTimeAsDouble,readyFeedback);
            commands.CopyPendingBuilds(pendingFeedback);ownBoard.SetPendingBuilds(showEnemy?null:pendingFeedback);
            ownBoard.Display(state,(PlayerSlot)(showEnemy?other:local),showEnemy||dragging?-1:selectedTower,selectedKind);
            enemyBoard.Display(state,(PlayerSlot)(showEnemy?local:other));
            if(!showEnemy&&modal==null)for(int i=0;i<readyFeedback.Count;i++){var c=readyFeedback[i];ownBoard.PlayConfirmedFeedback(c.Entity,c.Slot,c.Kind,c.ExpectedLevel,PresentationCommandContext.IsUpgrade(c.Type),c.ExpectedDamageLevel,c.ExpectedRangeLevel);}
            readyFeedback.Clear();if(dragging)RefreshDragPreview();else if(hasSelectedPoint&&!showEnemy&&modal==null&&!commands.IsBuildReserved(selectedPoint,selectedSite,PlacementValidator.BuildingSeparation))
            {bool valid=PlacementAllowed(selectedPoint,selectedKind,selectedSite,out _,out _);ownBoard.ShowDragTarget(selectedKind,selectedPoint,valid,selectedSite);}
            else if(!dragging)ownBoard.ClearDragTarget();
            queueText.text="ANGRIFF";
            for(int i=0;i<towers.Length;i++)
            {
                var availability=BuildPurchaseState(i);
                buildNames[i].text=availability==PurchaseState.Locked?"Gesperrt":availability==PurchaseState.Limit?"Limit erreicht":towers[i].Name;
                buildPrices[i].text=towers[i].Price+" G";
                SetPurchaseState(buildButtons[i],availability,buildGuideActive&&selectedKind==i?HudSelected:HudCard);
            }
            for(int i=0;i<units.Length;i++)
            {
                int packs=0;foreach(var order in state.SpawnOrders)if(order.Sender==session.LocalSlot&&order.Kind==i)packs++;
                sendPortraits[i].sprite=GeneratedClassIcons.GetUnitEvolution(i,player.ResearchLevels[i]);
                sendPrices[i].text=units[i].Price+" G";sendCounts[i].text=packs.ToString();sendCounts[i].transform.parent.gameObject.SetActive(packs>0);
                // Availability and target wave come from the same shared quote below.
            }
            RefreshSendQuotes();RefreshWaveHud();RefreshAttackInfo();RefreshTempoControl();
            RefreshSelection(state,player);
            RefreshBattleChrome();
            if(tutorialText!=null)tutorialText.text=Time.unscaledTimeAsDouble<feedbackUntil?notice:TutorialCopy();
        }
        void RefreshSelection(MatchState state,PlayerState player)
        {
            TowerState tower=null;
            for(int i=0;i<state.Towers.Count;i++)
            {
                var t=state.Towers[i];if(t.Owner!=session.LocalSlot)continue;
                if(t.Id==selectedTower||(hasSelectedPoint&&t.Kind==selectedKind&&t.SiteId==selectedSite&&t.Position.X==selectedPoint.X&&t.Position.Y==selectedPoint.Y&&!commands.IsBuildReserved(selectedPoint,selectedSite,PlacementValidator.BuildingSeparation))){tower=t;break;}
            }
            if(tower!=null){buildGuideActive=false;selectedTower=tower.Id;selectedSlot=tower.Slot;hasSelectedPoint=false;ownBoard.ClearDragTarget();}
            else if(selectedTower>=0){selectedTower=selectedSlot=-1;ownBoard.ClearDragTarget();}
            RefreshPriority(tower);
            selectionPortrait.sprite=tower!=null?GeneratedClassIcons.GetTowerEvolution(tower.Kind,towers[tower.Kind].IsEconomy?tower.Level:tower.DamageLevel,tower.RangeLevel):GeneratedClassIcons.Get(selectedKind,true);
            upgradeDetailsButton.gameObject.SetActive(tower!=null&&!showEnemy&&!dragging);
            if(upgradeBudget!=null){upgradeBudget.gameObject.SetActive(tower!=null);upgradeBudget.SetCompleted(tower!=null?tower.Level:0);}
            bool ground=hasSelectedPoint&&!showEnemy&&!dragging&&!commands.IsBuildReserved(selectedPoint,selectedSite,PlacementValidator.BuildingSeparation),playing=state.Phase==MatchPhase.Playing&&player.Hp>0;
            RefreshUpgradeModes(tower,playing);
            confirmBuild.gameObject.SetActive(ground&&tower==null);upgradeButton.gameObject.SetActive(tower!=null&&!showEnemy&&!dragging);rangeUpgradeButton.gameObject.SetActive(tower!=null&&!towers[tower.Kind].IsEconomy&&!showEnemy&&!dragging);sellButton.gameObject.SetActive(tower!=null&&!showEnemy&&!dragging);
            cancelSelection.gameObject.SetActive((ground||tower!=null)&&!showEnemy&&!dragging);
            if(tower!=null)
            {
                var def=towers[tower.Kind];var refund=EconomyRules.RefundGold(content,tower);var stats=TowerUpgradeRules.Stats(content,tower);bool pending=commands.HasPendingUpgrade(tower.Id);
                var firstPath=TowerUpgradeRules.PrimaryPath(content,tower.Kind);var secondPath=TowerUpgradeRules.SecondaryPath(content,tower.Kind);
                string path=def.IsEconomy?"Förderung +"+IncomeText(stats.IncomeUnitsPerSecond*content.MatchRules.IncomeMultiplier)+" G/s":
                    ShortUpgradeLabel(firstPath)+" "+GameNumbers.Compact(tower.DamageLevel)+" · "+ShortUpgradeLabel(secondPath)+" "+GameNumbers.Compact(tower.RangeLevel);
                selectionText.text=def.Name+" · Stufe "+GameNumbers.Compact(tower.Level)+"\n"+path+"\n"+(pending?"Upgrade wird bestätigt …":"Bild antippen: alle Werte");
                upgradeButton.name=def.IsEconomy?"Upgrade income":"Upgrade damage";
                RefreshUpgradeButton(upgradeButton,tower,firstPath,playing&&!pending,pending);
                if(!def.IsEconomy)RefreshUpgradeButton(rangeUpgradeButton,tower,secondPath,playing&&!pending,pending);
                sellButton.GetComponent<ActionButtonVisual>().SetDetails(ActionSymbol.Sell,"Verkaufen","+"+GameNumbers.Compact(refund)+" G",EconomyRules.RefundPercent(content,tower.Kind)+" % zurück","Rückerstattung");
                SetPurchaseState(sellButton,pending?PurchaseState.Pending:playing?PurchaseState.Available:PurchaseState.Inactive,HudCard);
            }
            else
            {
                var def=towers[selectedKind];bool valid=ground&&PlacementAllowed(selectedPoint,selectedKind,selectedSite,out _,out _);
                PlacementAllowed(selectedPoint,selectedKind,selectedSite,out _,out string reason);
                selectionText.text=def.Name+" · "+def.Price+" G\n"+(def.IsEconomy?"+"+IncomeText(def.IncomeUnitsPerSecond*content.MatchRules.IncomeMultiplier)+" G/s · "+(content.Map.FreePlacement?"freier Boden":"Minenplatz"):TowerRole(selectedKind))+"\n"+(valid?"Position gewählt · Kauf bestätigen.":reason);
                confirmBuild.GetComponent<ActionButtonVisual>().SetDetails(ActionSymbol.Build,"Bauen","Setzen",def.Price+" G",valid?"Position frei":"Position prüfen");
                SetPurchaseState(confirmBuild,!playing||!ground?PurchaseState.Inactive:BuildPurchaseState(selectedKind),HudAction);
            }
        }
        void RefreshUpgradeButton(Button button,TowerState tower,TowerUpgradePath path,bool allowed,bool pending)
        {
            var quote=TowerUpgradeBatchRules.Quote(content,tower,path,session.State.Players[(int)session.LocalSlot].Gold,upgradeBatchMode);var preview=BatchPreview(content,quote);
            bool secondaryPath=!towers[tower.Kind].IsEconomy&&path==TowerUpgradeRules.SecondaryPath(content,tower.Kind);
            if(secondaryPath)displayedSecondaryBatch=quote;else displayedPrimaryBatch=quote;
            var missing=BigInteger.Max(0,quote.Price-session.State.Players[(int)session.LocalSlot].Gold);
            string detail=pending?"Wird bestätigt …":quote.Count==0?"Kein Upgrade bezahlbar":missing>0?"Fehlen "+GameNumbers.Compact(missing)+" G":"Stufe "+GameNumbers.Compact(quote.NextLevel);
            var values=new System.Text.StringBuilder();
            foreach(var metric in preview)
            {
                if(values.Length>0)values.Append('\n');
                if(preview.Length>1)values.Append(ShortMetricLabel(metric.Label)).Append(": ");
                values.Append(GameNumbers.Ratio(metric.Current*(path==TowerUpgradePath.Income?content.MatchRules.IncomeMultiplier:1),metric.Divisor)).Append(" → ").Append(GameNumbers.Ratio(metric.Next*(path==TowerUpgradePath.Income?content.MatchRules.IncomeMultiplier:1),metric.Divisor));
                if(metric.Unit.Length>0)values.Append(' ').Append(metric.Unit);
            }
            bool secondary=path==TowerUpgradeRules.SecondaryPath(content,tower.Kind)&&!towers[tower.Kind].IsEconomy;
            var symbol=path==TowerUpgradePath.Income?ActionSymbol.Income:secondary?ActionSymbol.Range:path==TowerUpgradePath.FrostDuration?ActionSymbol.Frost:ActionSymbol.Damage;
            button.GetComponent<ActionButtonVisual>().SetDetails(symbol,GameNumbers.Compact(quote.Count)+"× "+(tower.Kind==7&&path==TowerUpgradePath.Damage?"Laserleistung":ShortUpgradeLabel(path)),values.ToString(),quote.Count>0?GameNumbers.Compact(quote.Price)+" G":"–",detail);
            SetPurchaseState(button,pending?PurchaseState.Pending:!allowed?PurchaseState.Inactive:quote.Code==ErrorCode.InsufficientGold?PurchaseState.Unaffordable:!quote.IsValid?PurchaseState.Inactive:PurchaseState.Available,HudAction);
        }
        void UpgradeSelectedPath(bool secondary)
        {
            if(session?.State==null)return;
            foreach(var tower in session.State.Towers)if(tower.Id==selectedTower&&tower.Owner==session.LocalSlot)
            {Upgrade(secondary?TowerUpgradeRules.SecondaryPath(content,tower.Kind):TowerUpgradeRules.PrimaryPath(content,tower.Kind));return;}
        }
        void SelectEntity(int entity)
        {
            if(showEnemy||dragging||InfoBlocking||modal!=null||UiPointerGate.Active)return;
            buildGuideActive=false;selectedTower=entity;selectedSlot=-1;hasSelectedPoint=false;ownBoard.ClearDragTarget();notice="Gebäude ausgewählt · verbessern oder verkaufen.";feedbackUntil=Time.unscaledTimeAsDouble+2;Sound();dirty=true;
        }
        void SelectGround(MapPoint point)
        {
            if(showEnemy||dragging||InfoBlocking||modal!=null||UiPointerGate.Active)return;
            // A ground tap outside an existing tower deselects it. Only an explicitly
            // armed build card may create a new placement preview after a completed action.
            if(!buildGuideActive){ClearBattleSelection();return;}
            selectedTower=selectedSlot=-1;hasSelectedPoint=true;selectedPoint=point;selectedSite=-1;
            if(!content.Map.FreePlacement)
            {
                selectedSite=ownBoard.FindSiteTarget(ownBoard.MapPointToScreen(point),null,towers[selectedKind].IsEconomy);
                if(selectedSite>=0)selectedPoint=buildSites[selectedSite];
            }
            bool valid=PlacementAllowed(selectedPoint,selectedKind,selectedSite,out _,out string reason);
            notice=valid?"Freier Boden · Bauen bestätigen oder andere Position antippen.":reason+" · Andere Position wählen.";feedbackUntil=Time.unscaledTimeAsDouble+2;Sound();dirty=true;
        }
        void ConfirmBuild()
        {
            if(!hasSelectedPoint||InfoBlocking||modal!=null)return;
            if(!PlacementAllowed(selectedPoint,selectedKind,selectedSite,out var placement,out string reason)){RejectPurchase(reason,confirmBuild);return;}
            ownBoard.ClearDragTarget();Send(CommandType.BuildTower,selectedKind,placement.SiteId,placement.Position,confirmBuild);
        }
        void Upgrade(TowerUpgradePath path)=>SubmitDisplayedUpgrade(path);
        void Sell(){if(selectedTower>=0)Send(CommandType.SellTower,0,selectedTower);}
        public bool BeginBuildPress(BuildDragSource source,int kind,PointerEventData e)
        {
            var state=session?.State;
            if(!isActiveAndEnabled||page!="game"||modal!=null||showEnemy||state==null||state.Phase!=MatchPhase.Playing||InfoBlocking||kind<0||kind>=towers.Length||BuildPurchaseState(kind)!=PurchaseState.Available||!UiPointerGate.Claim(source,e.pointerId))return false;
            buildGuideActive=true;dragSource=source;dragPointer=e.pointerId;dragKind=kind;dragSession=state.SessionId;dragSlot=-1;dragHasGround=false;dragging=false;dragCamera=e.pressEventCamera;dragPosition=e.position;return true;
        }
        public void MoveBuildDrag(BuildDragSource source,PointerEventData e)
        {
            if(source!=dragSource||e.pointerId!=dragPointer||!UiPointerGate.IsOwner(source,e.pointerId))return;
            if(!dragging)
            {
                dragging=true;dragBegan=Time.unscaledTime;selectedKind=dragKind;selectedSlot=selectedTower=-1;hasSelectedPoint=false;
                if(contextDock!=null)contextDock.gameObject.SetActive(false);
                dragChip=UiKit.Rect("Dragged tower preview",safe);dragChip.anchorMin=dragChip.anchorMax=dragChip.pivot=new Vector2(.5f,.5f);dragChip.sizeDelta=new Vector2(260,64);
                var image=dragChip.gameObject.AddComponent<HudFrameGraphic>();image.color=HudPanel;image.SetStyle(HudFrameStyle.Panel,HudEdge);
                var portrait=UiKit.SpriteAt(dragChip,GeneratedClassIcons.Get(dragKind,true),8,8,48,48,Color.white);portrait.name="Placement preview portrait";
                dragLabel=UiKit.TextAt(dragChip,"",64,3,188,58,14,HudText,TextAnchor.MiddleLeft);
                dirty=true;
            }
            dragPosition=e.position;dragCamera=e.pressEventCamera;RefreshDragPreview();
        }
        void RefreshDragPreview()
        {
            if(!dragging||ownBoard==null||dragChip==null)return;
            bool valid=TryPlacementAtPointer(dragPosition,dragCamera,dragKind,dragSlot,out dragPoint,out int site,out string reason);dragSlot=site;
            if(dragHasGround)ownBoard.ShowDragTarget(dragKind,dragPoint,valid,dragSlot);else ownBoard.ClearDragTarget();
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(safe,dragPosition,dragCamera,out var local))dragChip.anchoredPosition=PlacementChipPosition(local);
            dragChip.localScale=Vector3.one*(settings.ReducedEffects?1:Mathf.Lerp(.94f,1,Mathf.Clamp01((Time.unscaledTime-dragBegan)/.1f)));
            dragLabel.text=towers[dragKind].Name+" · "+towers[dragKind].Price+" G\n"+(valid?"✓ Frei · loslassen zum Bauen":"× "+reason);dragLabel.color=valid?UiKit.Hex(0xBFE8A8):UiKit.Hex(0xFFD09C);
            RefreshPlacementGuide();
            notice=valid?"Gültige Position · Loslassen zum Bauen.":reason;if(noticeText!=null)noticeText.text=notice;if(tutorialText!=null)tutorialText.text=notice;
        }
        bool TryPlacementAtPointer(Vector2 pointer,Camera camera,int kind,int previousSite,out MapPoint point,out int site,out string reason)
        {
            site=-1;point=default;dragHasGround=ownBoard!=null&&PointerHitsOwnBoard(pointer)&&ownBoard.TryScreenToMapPoint(pointer,camera,out point);
            if(!dragHasGround){reason="Auf die Insel ziehen.";return false;}
            if(!content.Map.FreePlacement)
            {
                site=ownBoard.FindSiteTarget(pointer,camera,towers[kind].IsEconomy,previousSite);
                if(site>=0)point=buildSites[site];else{reason=towers[kind].IsEconomy?"Auf ein goldenes Erzfeld ziehen":"Auf einen Bauplatz ziehen";return false;}
            }
            if(session.State==null||session.State.SessionId!=dragSession){reason="Die Partie hat gewechselt.";return false;}
            bool valid=PlacementAllowed(point,kind,site,out var placement,out reason);if(valid)point=placement.Position;return valid;
        }
        bool PointerHitsOwnBoard(Vector2 pointer)
        {
            var events=EventSystem.current;if(events==null||ownBoard==null)return false;
            if(placementPointer==null||placementEventSystem!=events){placementPointer=new PointerEventData(events);placementEventSystem=events;}
            placementPointer.position=pointer;placementPointer.pointerId=dragPointer;placementHits.Clear();events.RaycastAll(placementPointer,placementHits);
            return placementHits.Count>0&&placementHits[0].gameObject.transform.IsChildOf(ownBoard.transform);
        }
        bool PlacementAllowed(MapPoint point,int kind,int site,out TowerPlacement placement,out string reason)
        {
            placement=default;var state=session?.State;
            if(state==null||state.Phase!=MatchPhase.Playing||showEnemy||modal!=null){reason="Bau gerade nicht möglich.";return false;}
            if(commands.IsBuildReserved(point,site,PlacementValidator.BuildingSeparation)){reason="Hier wartet bereits ein Bau auf Bestätigung.";return false;}
            var error=PlacementValidator.ValidateBuild(content,state,session.LocalSlot,kind,site,point,out placement);
            reason=error==ErrorCode.None?"":PlacementErrorText(error);
            return error==ErrorCode.None;
        }
        public void EndBuildDrag(BuildDragSource source,PointerEventData e)
        {
            if(source!=dragSource||e.pointerId!=dragPointer)return;
            int kind=dragKind;bool valid=TryPlacementAtPointer(e.position,e.pressEventCamera,kind,dragSlot,out var point,out int site,out string reason);CancelInteraction();
            if(valid)Send(CommandType.BuildTower,kind,site,point);
            else RejectPurchase(reason+" · kein Gold ausgegeben.",buildButtons[kind]);
        }
        public void CancelBuildDrag(BuildDragSource source){if(source==dragSource)CancelInteraction();else UiPointerGate.Release(source);}
        void CancelInteraction()
        {
            if(troopSwipes!=null)foreach(var swipe in troopSwipes)if(swipe!=null)swipe.CancelGesture();
            buildGuideActive=false;ownBoard?.HidePlacementGuide();
            var source=dragSource;dragSource=null;dragging=false;dragHasGround=false;dragSlot=-1;dragSession=0;ButtonPressFeedback.ResetAll();
            if(source!=null){UiPointerGate.Release(source);source.CancelGesture();}
            if(dragChip!=null){dragChip.gameObject.SetActive(false);Destroy(dragChip.gameObject);}dragChip=null;dragLabel=null;
            if(ownBoard!=null)ownBoard.ClearDragTarget();
            if(ownBoard!=null)ownBoard.GetComponent<BoardPointerSurface>()?.CancelGesture();
        }
        void CancelBoardFeedback()
        {
            if(ownBoard!=null){ownBoard.CancelFeedback();ownBoard.SetPendingBuilds(null);}
            if(enemyBoard!=null)enemyBoard.CancelFeedback();if(menuBoard!=null)menuBoard.CancelFeedback();
        }
        void Send(CommandType type,int kind=0,int target=-1,MapPoint? position=null,Button feedbackButton=null,bool fromInfo=false)
        {
            if(session==null||session.State==null||modal!=null||UiPointerGate.Active||(InfoBlocking&&!(fromInfo&&type==CommandType.ResearchUnit&&infoKind==kind&&attackInfo.Open)))return;
            if(type==CommandType.SendUnit&&kind>=0&&kind<units.Length&&session.State.Players[(int)session.LocalSlot].Gold<units[kind].Price)
            {RejectPurchase("Für diese Truppen fehlen "+(units[kind].Price-session.State.Players[(int)session.LocalSlot].Gold)+" Gold.",sendButtons[kind]);return;}
            if(commands.SessionId!=session.State.SessionId)commands.Reset(session.State.SessionId);
            var context=PresentationCommandContext.Capture(session.State,session.LocalSlot,type,kind,target,Time.unscaledTimeAsDouble,position);
            context.FromConfirmation=feedbackButton!=null&&feedbackButton==confirmBuild;
            BigInteger expectedWave=0,expectedResearch=0;int enemyTarget=-1;
            if(type==CommandType.SetWaveReady)expectedWave=session.State.WaveId;
            if(type==CommandType.SendUnit)
            {
                if(displayedSendQuotes==null||kind<0||kind>=displayedSendQuotes.Length)return;
                var quote=displayedSendQuotes[kind];
                if(!quote.IsValid){RejectPurchase(ErrorText(quote.Code),sendButtons[kind]);return;}
                expectedWave=quote.WaveId;expectedResearch=quote.ResearchLevel;enemyTarget=quote.EnemyTarget;
                context.ExpectedWave=expectedWave;context.ExpectedResearch=expectedResearch;
            }
            if(type==CommandType.ResearchUnit)
            {
                if(kind!=infoKind||!displayedResearchQuote.IsValid)return;
                expectedResearch=displayedResearchQuote.Level;target=-1;context.Target=-1;context.ExpectedResearch=expectedResearch;
            }
            // Local host commands may resolve synchronously inside Submit; tutorial observes accepted state, not optimistic taps.
            ulong sequence=session.Submit(type,kind,target,position?.X??0,position?.Y??0,expectedWave,expectedResearch,enemyTarget);
            if(sequence==0){RejectPurchase(session.Message,FeedbackButton(context));return;}
            commands.Register(sequence,context);
            var last=session.LastResult;
            if(session.IsHost&&last.Sequence==sequence){if(commands.Resolve(last,out var captured))ApplyResolution(last,captured);}
            else notice="Aktion wird bestätigt …";
            dirty=true;
        }
        void Resolved(CommandResult result)
        {
            ResolveLobbyCommand(result);
            ResolveSpeedVote(result);
            if(commands.Resolve(result,out var context))ApplyResolution(result,context);
            dirty=true;
        }
        void ApplyResolution(CommandResult result,PresentationCommandContext context)
        {
            CommandType type=context.Type;
            feedbackUntil=Time.unscaledTimeAsDouble+2.5;
            if(result.Accepted)
            {
                if(type==CommandType.BuildTower&&context.FromConfirmation&&hasSelectedPoint&&selectedKind==context.Kind&&selectedSite==context.SiteId&&selectedPoint.X==context.Position.X&&selectedPoint.Y==context.Position.Y)ClearBattleSelection();
                notice=type==CommandType.BuildTower?"Bau bestätigt.":PresentationCommandContext.IsUpgrade(type)?"Upgrade bestätigt.":type==CommandType.SellTower?"Turm verkauft. Gold erstattet.":type==CommandType.SendUnit?"Bezahlt · Ausmarsch Welle "+context.ExpectedWave+" · Forschung "+context.ExpectedResearch:type==CommandType.ResearchUnit?"Forschung bestätigt · gilt für neue Käufe.":type==CommandType.SetWaveReady?"Wellenbereitschaft bestätigt.":type==CommandType.SetTowerPriority?"Zielpriorität bestätigt.":"Bestätigt.";
                Sound(type==CommandType.BuildTower?GameAudioCue.Build:PresentationCommandContext.IsUpgrade(type)?GameAudioCue.Upgrade:type==CommandType.SellTower?GameAudioCue.Sell:type==CommandType.SendUnit?GameAudioCue.Send:type==CommandType.ResearchUnit?GameAudioCue.Research:GameAudioCue.Ui);
                if(tutorialStep==0&&type==CommandType.BuildTower)tutorialStep=1;
                else if(tutorialStep==1&&type==CommandType.SendUnit)tutorialStep=2;
                else if(tutorialStep==2&&PresentationCommandContext.IsUpgrade(type))tutorialStep=3;
                else if(tutorialStep==3&&type==CommandType.SetTowerPriority){tutorialStep=4;notice="Tutorial geschafft! Spiele das Trainingsduell zu Ende.";}
            }
            else RejectPurchase(ErrorText(result.Code),FeedbackButton(context));
        }
        Button FeedbackButton(PresentationCommandContext context)
        {
            if(context.Type==CommandType.BuildTower)return context.FromConfirmation&&confirmBuild!=null&&confirmBuild.gameObject.activeInHierarchy?confirmBuild:buildButtons!=null&&context.Kind>=0&&context.Kind<buildButtons.Length?buildButtons[context.Kind]:null;
            if(context.Type==CommandType.SendUnit)return sendButtons!=null&&context.Kind>=0&&context.Kind<sendButtons.Length?sendButtons[context.Kind]:null;
            if(context.Type==CommandType.ResearchUnit)return researchButton;
            if(context.Type==CommandType.SetWaveReady)return waveStartButton;
            if(context.Type==CommandType.SetTowerPriority)return priorityButton;
            if(context.Entity!=selectedTower)return null;
            return context.Type==CommandType.SellTower?sellButton:(context.UpgradePath==TowerUpgradePath.Range||context.UpgradePath==TowerUpgradePath.BlastRadius||context.UpgradePath==TowerUpgradePath.JumpRange)?rangeUpgradeButton:upgradeButton;
        }
        void RejectPurchase(string reason,Button button)
        {
            notice=reason;feedbackUntil=Time.unscaledTimeAsDouble+3;if(button!=null)button.GetComponent<ButtonRejectFeedback>()?.Play();Sound(GameAudioCue.Error);dirty=true;
        }
        string TutorialCopy()
        {
            switch(tutorialStep)
            {
                case 0:return "1 / 4 · Ziehe Schnellfeuer auf eine freie Wiese. Alternativ antippen und Bau bestätigen.";
                case 1:return "2 / 4 · Kaufe einen Läufer. Seine Ausmarschwelle steht vor dem Kauf auf der Karte.";
                case 2:return "3 / 4 · Tippe deinen Turm an und kaufe ein Upgrade. Spare bei Bedarf Gold.";
                case 3:return "4 / 4 · Wähle beim Kampfturm das Ziel Vorne oder Stärkster. Dein Turm bleibt erhalten.";
                default:return content.MatchRules.Endless?"Geschafft! Überlebe weitere Wellen. Bei 0 Team-LP scheidet dein Team aus.":"Geschafft! Überstehe "+content.WaveCount+" Wellen. Danach zählen die übrigen Team-LP.";
            }
        }
        BigInteger TowerIncome(TowerState t)=>TowerUpgradeRules.Stats(content,t).IncomeUnitsPerSecond*content.MatchRules.IncomeMultiplier;
        static string TowerRole(int k)=>k==0?"Schnell gegen leichte Einzelziele.":k==1?"Flächenschaden gegen Gruppen.":k==2?"Bremst Gegner auf dem Weg.":k==3?"Durchdringt Rüstung. Langsame Schüsse.":k==5?"Blitze springen auf nahe Gegner über.":k==6?"Durchschlägt Gegner auf geraden Strecken.":k==7?"Präziser Energiestrahl gegen Einzelziele.":"Erzeugt zusätzliches Gold.";
        static string UnitRole(int k)=>k==0?"ausgeglichen":k==1?"schnell":k==2?"gepanzert":"4 Truppen";
        public static string ErrorText(ErrorCode error)
        {
            switch(error)
            {
                case ErrorCode.InsufficientGold:return "Nicht genug Gold. Einkommen gibt es während Welle und Aufräumen.";
                case ErrorCode.Cooldown:return "Diese Truppe wartet noch auf ihren Ausmarsch.";
                case ErrorCode.QueueFull:return "Der Auftrag konnte nicht angenommen werden. Kein Gold wurde abgezogen.";
                case ErrorCode.MaxLevel:return "Dieses Upgrade ist nicht verfügbar.";
                case ErrorCode.QuoteChanged:return "Angebot geändert. Kein Kauf. Prüfe die neue Welle/Stufe und tippe erneut.";
                case ErrorCode.WaveCapacity:return "Keine weitere Ausmarschwelle. Kein Gold abgezogen.";
                case ErrorCode.TowerLocked:return "Dieser Turm ist in deiner aktuellen Turmauswahl nicht ausgerüstet.";
                case ErrorCode.ClassLimit:return "Hier kann derzeit nicht gebaut werden.";
                case ErrorCode.OutsideMap:return "Der ganze Turm muss innerhalb des Kartenrands stehen.";
                case ErrorCode.OnPath:return "Zu nah am Weg. Halte mehr Abstand.";
                case ErrorCode.BlockedArea:return "Dieser Bereich ist durch Gelände oder Hindernisse gesperrt.";
                case ErrorCode.TooClose:return "Zu nah an einem anderen Gebäude.";
                case ErrorCode.Occupied:return "Dieser Bauplatz ist bereits belegt.";
                case ErrorCode.NotOwner:return "Du kannst nur deine eigenen Türme ändern.";
                case ErrorCode.WrongPhase:return "Diese Aktion ist in der aktuellen Spielphase gesperrt.";
                case ErrorCode.RateLimited:return "Zu viele Aktionen auf einmal. Bitte kurz warten.";
                case ErrorCode.Duplicate:return "Diese Aktion wurde bereits verarbeitet.";
                case ErrorCode.WrongSession:case ErrorCode.StaleSequence:return "Diese Aktion gehört zu einem älteren Spielstand.";
                default:return "Aktion abgelehnt: ungültiges Ziel oder ungültige Eingabe.";
            }
        }
        static string LocalAddresses()
        {
            try
            {
                var result=new List<string>(3);
                foreach(var adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if(adapter.OperationalStatus!=OperationalStatus.Up)continue;
                    foreach(var item in adapter.GetIPProperties().UnicastAddresses)
                        if(item.Address.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(item.Address)&&result.Count<3)result.Add(item.Address.ToString());
                }
                if(result.Count>0)return string.Join(" / ",result);
            }
            catch(Exception) { /* Some Android vendors hide interface information. Use the device settings. */ }
            return "unter Android-Einstellungen → WLAN → Netzwerkdetails";
        }
        string ModeName()=>page=="connect"?(connectionPage=="lan"?"WLAN · MULTIPLAYER":"ONLINE · MULTIPLAYER"):session.Mode==SessionMode.Offline?"SOLO · GEGEN BOT":session.Mode==SessionMode.Lan?"WLAN · MULTIPLAYER":"ONLINE · MULTIPLAYER";
        void Sound(GameAudioCue cue=GameAudioCue.Ui){if(gameAudio!=null)gameAudio.Play(cue);}
        void ReturnMenu(){bool hadMatch=session.State!=null;CancelInteraction();CloseModal();session.Shutdown();commands.Reset(0);readyFeedback.Clear();connectionPage="";tutorialStep=-1;notice="";selectedSlot=-1;hasSelectedPoint=false;page="home";Rebuild();if(hadMatch)StartCoroutine(ReleaseUnusedPortraits());}
        System.Collections.IEnumerator ReleaseUnusedPortraits(){yield return null;GeneratedClassIcons.TrimEvolutionCache();yield return Resources.UnloadUnusedAssets();}
        void Back(){if(page=="lobby"&&ConsumeLobbyInputBack())return;Sound(GameAudioCue.Back);if(InfoBlocking){attackInfo.Close();return;}if(dragSource!=null){CancelInteraction();notice="Bau abgebrochen.";dirty=true;return;}if(modal!=null){CloseModal();return;}if(page=="home"){ConfirmQuit();return;}if(page=="connect"||page=="connecting"||page=="lost"||page=="result")ReturnMenu();else if(selectedTower>=0||hasSelectedPoint||buildGuideActive)ClearBattleSelection();else ConfirmLeave();}
        void ConfirmQuit()
        {
            OpenModal("SPIEL SCHLIESSEN",620,300);
            UiKit.TextAt(modal,"Möchtest du Defense Duell jetzt beenden?",32,94,556,70,23,DialogMutedColor);
            DialogButton(modal,"Bleiben",32,190,260,78,CloseModal,UiKit.Mint,22);
            DialogButton(modal,"Schließen",310,190,278,78,()=>Application.Quit(),UiKit.PanelLight,22);
        }
        void ConfirmLeave()
        {
            OpenModal("DUELL VERLASSEN",620,340);
            string message=session.Mode==SessionMode.Offline?"Das laufende Solo-Duell wird beendet. Dein Spielstand wird nicht gespeichert.":"Das laufende Netzwerkduell wird für alle abgebrochen. Es wird kein Sieger gewertet.";
            UiKit.TextAt(modal,message,32,94,556,92,22,DialogMutedColor);
            DialogButton(modal,"Weiterspielen",32,222,260,80,CloseModal,UiKit.Mint,22);
            DialogButton(modal,"Verlassen",310,222,278,80,ReturnMenu,UiKit.PanelLight,22);
        }
        void OpenSettings()
        {
            OpenModal("TON & ANZEIGE",760,592);
            SliderRow("Musik",118,settings.Music,v=>settings.SetMusic(v));
            SliderRow("Effekte",209,settings.Effects,v=>{settings.SetEffects(v);Sound();});
            ToggleRow("Ton ausschalten",300,settings.Muted,v=>settings.SetMuted(v));
            ToggleRow("Reduzierte Effekte",373,settings.ReducedEffects,v=>{settings.SetReduced(v);ApplyVisualSettings();});
            ToggleRow("Energiesparen · 30 FPS",446,settings.PowerSaving,v=>{settings.SetPower(v);ApplyVisualSettings();});
            UiKit.TextAt(modal,page=="game"?"Die Partie läuft weiter. Einstellungen gelten nur für dieses Gerät.":"Einstellungen werden nur auf diesem Gerät gespeichert.",30,530,700,35,16,DialogMutedColor);
        }
        void ApplyVisualSettings()
        {
            ButtonPressFeedback.SetReducedEffects(settings.ReducedEffects);if(attackInfo!=null)attackInfo.SetReducedEffects(settings.ReducedEffects);if(settings.ReducedEffects&&dragChip!=null)dragChip.localScale=Vector3.one;
            if(menuBoard!=null){menuBoard.ReducedEffects=settings.ReducedEffects;menuBoard.PowerSaving=settings.PowerSaving;}
            if(homeMotion!=null)homeMotion.ApplyPolicy();
            if(ownBoard!=null){ownBoard.ReducedEffects=settings.ReducedEffects;ownBoard.PowerSaving=settings.PowerSaving;}
            if(enemyBoard!=null){enemyBoard.ReducedEffects=settings.ReducedEffects;enemyBoard.PowerSaving=settings.PowerSaving;}
        }
        void OpenModal(string title,float w,float h)
        {
            CloseAttackInfo();CancelInteraction();commands.DiscardVisuals();CancelBoardFeedback();hasSelectedPoint=false;
            CloseModal();
            modalPreviousFocus=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
            var blocker=UiKit.Rect("Modal blocker",safe);UiKit.Fill(blocker);var shade=blocker.gameObject.AddComponent<Image>();shade.color=new Color(.015f,.04f,.05f,.88f);shade.raycastTarget=true;
            modal=UiKit.Rect("Dialog",blocker);modal.anchorMin=modal.anchorMax=modal.pivot=new Vector2(.5f,.5f);modal.sizeDelta=new Vector2(w,h);modal.localScale=Vector3.one*Mathf.Min(1,(width-24)/w,(height-24)/h);
            Image panel;
            if(ThemedDialogs){var frame=modal.gameObject.AddComponent<HudFrameGraphic>();frame.SetStyle(HudFrameStyle.Panel,HudEdge);frame.color=HudPanel;panel=frame;}
            else{panel=modal.gameObject.AddComponent<Image>();panel.color=UiKit.Panel;}
            panel.raycastTarget=true;
            UiKit.TextAt(modal,title,30,22,w-128,60,28,DialogTextColor);
            var close=DialogButton(modal,"×",w-86,22,64,60,CloseModal,UiKit.PanelLight,32);
            if(ThemedDialogs)HudTheme.IconButton(close,HudSymbol.Close);
            UiPointerGate.SetScope(this,modal);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(close.gameObject);
        }
        void CloseModal(){homeArsenalOpen=false;virtualQueue=false;virtualQueueRows.Clear();wavePreviewText=paidQueueDetailText=null;paidQueueScroll=null;paidQueueContent=null;UiPointerGate.ClearScope(this);if(modal==null)return;var root=modal.parent.gameObject;root.SetActive(false);Destroy(root);modal=null;
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(modalPreviousFocus!=null&&modalPreviousFocus.activeInHierarchy?modalPreviousFocus:null);modalPreviousFocus=null;}
        void SliderRow(string title,float y,float value,Action<float> change)
        {
            UiKit.TextAt(modal,title,32,y,170,64,22,DialogTextColor);
            var r=UiKit.Rect(title+" slider",modal);UiKit.At(r,220,y,500,64);
            var hit=r.gameObject.AddComponent<Image>();hit.color=new Color(0,0,0,0);hit.raycastTarget=true;
            var slider=r.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;slider.direction=Slider.Direction.LeftToRight;
            var track=UiKit.Rect("Track",r);UiKit.Fill(track,8,28,8,28);var ti=track.gameObject.AddComponent<Image>();ti.color=ThemedDialogs?HudCard:UiKit.PanelLight;ti.raycastTarget=false;
            var fill=UiKit.Rect("Fill",track);UiKit.Fill(fill);var fi=fill.gameObject.AddComponent<Image>();fi.color=ThemedDialogs?HudGold:UiKit.Mint;fi.raycastTarget=false;slider.fillRect=fill;
            var handleArea=UiKit.Rect("Handle area",r);UiKit.Fill(handleArea,24,3,24,3);
            var handle=UiKit.Rect("Handle",handleArea);handle.sizeDelta=new Vector2(48,0);var hi=handle.gameObject.AddComponent<Image>();hi.color=DialogTextColor;
            slider.handleRect=handle;slider.targetGraphic=hi;slider.value=value;slider.onValueChanged.AddListener(v=>change(v));
        }
        void ToggleRow(string title,float y,bool value,Action<bool> change)
        {
            UiKit.TextAt(modal,title,32,y,500,62,22,DialogTextColor);
            Button button=null;bool current=value;
            button=DialogButton(modal,current?"AN":"AUS",560,y,160,62,()=>{current=!current;change(current);UiKit.ButtonText(button,current?"AN":"AUS");UiKit.ButtonColor(button,ThemedDialogs?(current?HudAction:HudCard):(current?UiKit.Hex(0xA7D585):UiKit.PanelLight));},current?UiKit.Hex(0xA7D585):UiKit.PanelLight,22);
        }
    }
}
