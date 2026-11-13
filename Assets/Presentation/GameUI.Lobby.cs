using System;
using System.Collections.Generic;
using System.Globalization;
using BigInteger = System.Numerics.BigInteger;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        readonly LobbySettingsEditor lobbySettingsEditor = new LobbySettingsEditor();
        HostLobbyRulesPanel lobbyRulesPanel;
        MapThumbnailGraphic lobbyMapPreview;
        string lobbyPreviewMapId;
        string lobbyEpoch;
        LobbyMatchOptions lobbyOptions;
        BigInteger formattedLobbyGold;
        readonly Dictionary<ulong, LobbyUiRequest> lobbyRequests = new Dictionary<ulong, LobbyUiRequest>();
        sealed class LobbyUiRequest { internal string Epoch; internal Guid Id; }

        void BuildLobbyScreen()
        {
            float w=width-48,x=24;
            HudTheme.ButtonAt(screen,"Verlassen",24,18,132,48,ConfirmLeave,HudTheme.Panel,17).name="Lobby leave";
            UiKit.TextAt(screen,ModeName(),178,18,width-400,28,18,HudTheme.Gold).fontStyle=FontStyle.Bold;
            HudTheme.ButtonAt(screen,"Ton & Anzeige",width-204,18,180,48,OpenSettings,HudTheme.Card,16).name="Lobby settings";
            UiKit.TextAt(screen,session.Mode==SessionMode.Offline?"DEIN SOLO-DUELL":"EURE PARTIE",x,82,w,42,32,HudTheme.Text).fontStyle=FontStyle.Bold;
            string detail=session.Mode==SessionMode.Online?"Beitrittscode: "+session.JoinCode:session.Mode==SessionMode.Lan?(session.IsHost?"Deine IP: "+LocalAddresses()+" · Port "+formPort:"Mit dem WLAN-Raum verbunden"):"Trainiere gegen den Bot · deine Spielregeln";
            UiKit.TextAt(screen,detail,x,128,w,40,16,HudTheme.Muted).name="Lobby connection";
            statusText=UiKit.TextAt(screen,"",x,174,w,22,14,HudTheme.Muted);statusText.name="Lobby participants";
            RefreshLobbyModel();
            rosterLabels=new Text[session.State.Players.Length];float slotWidth=(w-6*(rosterLabels.Length-1))/rosterLabels.Length;
            for(int i=0;i<rosterLabels.Length;i++)
            {
                var slot=BattlePanel(screen,"Lobby player "+i,x+i*(slotWidth+6),202,slotWidth,54,HudPanel);
                rosterLabels[i]=UiKit.TextAt(slot.transform,"",8,4,slotWidth-16,46,13,HudText,TextAnchor.MiddleCenter);
            }
            float left=(w-20)*.56f,right=w-left-20,rx=x+left+20;
            lobbyRulesPanel=new HostLobbyRulesPanel(screen,x,272,left,lobbySettingsEditor,ApplyLobbySettings,SubmitLobbyReady,()=>LobbyInputMaySubmit&&lobbyChoiceSequence==0);
            // The map and loadout keep useful image sizes; shorter displays scroll this column.
            var viewport=UiKit.Rect("Lobby choices viewport",screen);UiKit.At(viewport,rx,272,right,Mathf.Max(160,height-284));
            var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=viewport;
            var choices=UiKit.Rect("Lobby choices",viewport);UiKit.At(choices,0,0,right,594);scroll.content=choices;
            lobbyMapButton=BattleButton(choices,"",0,0,right,150,()=>OpenMapPicker(true),HudCard,16);lobbyMapButton.name="Lobby map choice";
            var mapVisual=UiKit.ButtonContent(lobbyMapButton);var mapRect=UiKit.Rect("Lobby map preview",mapVisual);UiKit.At(mapRect,10,8,right-20,104);
            lobbyMapPreview=mapRect.gameObject.AddComponent<MapThumbnailGraphic>();lobbyMapPreview.raycastTarget=false;lobbyPreviewMapId=null;
            UiKit.At(lobbyMapButton.GetComponentInChildren<Text>().rectTransform,8,114,right-16,30);
            lobbyFormatButton=BattleButton(choices,"",0,158,right,44,()=>{if(!LobbyInputMaySubmit||!lobbySettingsEditor.CanEdit)return;int next=((int)lobbySettingsEditor.DraftFormat+1)%4;lobbySettingsEditor.EditRules(lobbySettingsEditor.DraftEndless,lobbySettingsEditor.DraftIncomeMultiplier,(MatchFormat)next,lobbySettingsEditor.DraftMapId);RefreshLobbyScreen();},HudCard,16);lobbyFormatButton.name="Lobby format choice";
            UiKit.TextAt(choices,"DEIN TEAM",0,212,right,22,12,HudGold);
            lobbyTeamButtons=new Button[session.State.Teams.Length];float tw=(right-6*(lobbyTeamButtons.Length-1))/lobbyTeamButtons.Length;
            for(int i=0;i<lobbyTeamButtons.Length;i++){int team=i;lobbyTeamButtons[i]=BattleButton(choices,"Team "+(i+1),i*(tw+6),238,tw,44,()=>ChooseLobbyTeam(team),HudPanel,14);lobbyTeamButtons[i].name="Lobby team "+i;}
            lobbyLoadoutCounter=UiKit.TextAt(choices,"",0,292,right,26,12,HudGold);
            lobbyLoadoutButtons=new Button[towers.Length];int columns=Mathf.Clamp(Mathf.FloorToInt((right+6)/90),2,4);float cw=(right-6*(columns-1))/columns;
            for(int i=0;i<towers.Length;i++)
            {
                int kind=i;var button=BattleButton(choices,"",(i%columns)*(cw+6),326+(i/columns)*112,cw,106,()=>ToggleLobbyClass(kind),HudPanel,12);button.name="Lobby loadout "+i;lobbyLoadoutButtons[i]=button;
                var visual=UiKit.ButtonContent(button);UiKit.SpriteAt(visual,GeneratedClassIcons.Get(i,true),(cw-72)*.5f,2,72,72,Color.white).name="Lobby tower portrait";
                UiKit.TextAt(visual,towers[i].Name,3,76,cw-6,28,11,HudText,TextAnchor.MiddleCenter);
            }
            float bottom=326+Mathf.CeilToInt((float)towers.Length/columns)*112;
            UiKit.TextAt(choices,"Eigene Türme, eigenes Gold. Team-LP werden geteilt.",0,bottom,right,34,12,HudMuted);
            choices.sizeDelta=new Vector2(right,bottom+42);scroll.verticalNormalizedPosition=1;
            RefreshLobbyScreen();
        }

        Text[] rosterLabels;
        Text lobbyLoadoutCounter;
        Button[] lobbyTeamButtons,lobbyLoadoutButtons;
        Button lobbyMapButton,lobbyFormatButton;
        ulong lobbyChoiceSequence,lobbyChoiceEpoch,lobbyChoicePublication;
        void ChooseLobbyTeam(int team)
        {if(!LobbyChoiceAllowed)return;TrackLobbyChoice(session.SetLobbyTeam(team,session.State.SessionId,session.ConfigRevision,session.RosterRevision));}
        void ToggleLobbyClass(int kind)
        {
            if(!LobbyChoiceAllowed)return;
            int mask=session.State.Players[(int)session.LocalSlot].AvailableTowerMask^(1<<kind);
            if(!TowerLoadoutRules.IsValid(mask,towers.Length)){notice=mask==0?"Mindestens eine Turmklasse bleibt ausgewählt.":"Maximal fünf Turmarten · zuerst eine andere abwählen.";dirty=true;return;}
            TrackLobbyChoice(session.SetLobbyLoadout(mask,session.State.SessionId,session.ConfigRevision,session.RosterRevision));
        }
        bool LobbyChoiceAllowed=>LobbyInputMaySubmit&&session.State!=null&&!lobbySettingsEditor.HasPending&&lobbyChoiceSequence==0;
        void TrackLobbyChoice(ulong sequence)
        {
            if(sequence==0){notice=session.Message;dirty=true;return;}
            lobbyChoiceSequence=sequence;lobbyChoiceEpoch=session.State.SessionId;lobbyChoicePublication=0;
            if(session.LastResult.Sequence==sequence&&session.LastResultEpoch==lobbyChoiceEpoch)ResolveLobbyChoice(session.LastResult);
            dirty=true;
        }
        void ResolveLobbyChoice(CommandResult result)
        {
            if(lobbyChoiceSequence!=result.Sequence||session.LastResultEpoch!=lobbyChoiceEpoch)return;
            if(!result.Accepted){notice=LobbyRuleError(result.Code);lobbyChoiceSequence=0;}
            else {lobbyChoicePublication=session.LastResultPublicationSequence;if(session.AuthoritativeStateSequence>=lobbyChoicePublication)lobbyChoiceSequence=0;}
            dirty=true;
        }

        LobbySettingsView CurrentLobbyView()
        {
            var state = session.State;
            if (state == null) return null;
            int maxWaves = MatchRulesPolicy.MaxWaveCount;
            var capabilities = new LobbyCapabilities(MatchRulesPolicy.MinWaveCount, maxWaves,
                checked(MatchRulesPolicy.MaxStartGoldMagnitudeBytes * 3), maximumGoldMagnitudeBytes: MatchRulesPolicy.MaxStartGoldMagnitudeBytes);
            // Full decimal formatting can be expensive near the transport budget. Readiness
            // publications reuse the exact amount rather than reformatting it ten times a second.
            // Options and revision must come from the same authoritative publication.
            // A synchronous host ACK may arrive before the renderer refreshes its content cache.
            var rules=state.MatchRules;
            var gold = rules.StartGold;
            if (lobbyOptions == null || lobbyOptions.WaveCount != rules.WaveCount || formattedLobbyGold != gold||lobbyOptions.Endless!=rules.Endless||lobbyOptions.IncomeMultiplier!=rules.IncomeMultiplier||lobbyOptions.Format!=rules.Format||lobbyOptions.MapId!=rules.MapId)
            { formattedLobbyGold = gold; lobbyOptions = new LobbyMatchOptions(rules.WaveCount,gold.ToString(CultureInfo.InvariantCulture),rules.Endless,rules.IncomeMultiplier,rules.Format,rules.MapId); }
            return new LobbySettingsView(state.SessionId.ToString(CultureInfo.InvariantCulture), lobbyOptions,
                state.ConfigRevision, state.RosterRevision, session.CanEditHostRules,
                session.CanReady, capabilities, session.AuthoritativeStateSequence, state.Phase, state.Players[(int)session.LocalSlot].Ready);
        }

        void RefreshLobbyModel()
        {
            var view = CurrentLobbyView(); if (view == null) return;
            // SessionCoordinator validates Welcome/rematch epochs before publishing its State.
            // Arbitrary incoming packets can never choose an editor epoch here.
            if (lobbyEpoch != view.RoomEpoch)
            { lobbyEpoch = view.RoomEpoch; lobbyRequests.Clear(); lobbySettingsEditor.BeginSession(lobbyEpoch); }
            if (lobbyRulesPanel != null) lobbyRulesPanel.Receive(view); else lobbySettingsEditor.Receive(view);
        }

        void RefreshLobbyScreen()
        {
            RefreshLobbyModel();
            var state = session.State;
            if (state == null) return;
            lobbyRulesPanel?.Refresh();
            int local=(int)session.LocalSlot;
            if(lobbyChoiceSequence!=0&&(lobbyChoiceEpoch!=state.SessionId||lobbyChoicePublication>0&&session.AuthoritativeStateSequence>=lobbyChoicePublication))lobbyChoiceSequence=0;
            if(statusText!=null)statusText.text=(session.Mode==SessionMode.Offline?"Du + Trainingsbot":session.ConnectedPlayers+" / "+state.Players.Length+" Spieler verbunden")+" · "+(lobbyChoiceSequence!=0?"Auswahl wird bestätigt …":notice);
            if(rosterLabels!=null)for(int i=0;i<rosterLabels.Length;i++)
            {
                var player=state.Players[i];bool connected=(state.ConnectedPlayerMask&(1<<i))!=0;
                rosterLabels[i].text=(i==local?"DU":"SPIELER "+(i+1))+" · T"+(player.TeamId+1)+"\n"+(connected?(player.Ready?"✓ BEREIT":"WARTET"):"PLATZ FREI");
                rosterLabels[i].color=connected?player.Ready?HudGold:HudText:HudMuted;
            }
            if(lobbyMapButton!=null){UiKit.ButtonText(lobbyMapButton,"KARTE · "+MapLabel(lobbySettingsEditor.DraftMapId));lobbyMapButton.interactable=lobbySettingsEditor.CanEdit&&lobbyChoiceSequence==0;}
            if(lobbyMapPreview!=null&&lobbyPreviewMapId!=lobbySettingsEditor.DraftMapId)
            {
                for(int i=0;i<MapCatalog.Count;i++){var map=MapCatalog.Create(i);if(map.Id!=lobbySettingsEditor.DraftMapId)continue;lobbyMapPreview.Initialize(map);lobbyPreviewMapId=map.Id;break;}
            }
            if(lobbyFormatButton!=null){UiKit.ButtonText(lobbyFormatButton,"FORMAT · "+FormatLabel(lobbySettingsEditor.DraftFormat)+" ⇄");lobbyFormatButton.interactable=lobbySettingsEditor.CanEdit&&session.Mode!=SessionMode.Offline&&lobbyChoiceSequence==0;}
            if(lobbyTeamButtons!=null)for(int i=0;i<lobbyTeamButtons.Length;i++){lobbyTeamButtons[i].interactable=LobbyChoiceAllowed&&session.Mode!=SessionMode.Offline;UiKit.ButtonColor(lobbyTeamButtons[i],state.Players[local].TeamId==i?HudSelected:HudPanel);}
            if(lobbyLoadoutCounter!=null)lobbyLoadoutCounter.text=SelectedTowerCount(state.Players[local].AvailableTowerMask)+" / "+TowerLoadoutRules.MaxSelectedClasses+" TURMARTEN · MINE ZÄHLT MIT";
            if(lobbyLoadoutButtons!=null)for(int i=0;i<lobbyLoadoutButtons.Length;i++){lobbyLoadoutButtons[i].interactable=LobbyChoiceAllowed;UiKit.ButtonColor(lobbyLoadoutButtons[i],(state.Players[local].AvailableTowerMask&(1<<i))!=0?HudSelected:HudPanel);}
        }

        void ResetLobbyPresentation()
        {
            lobbyRulesPanel = null;lobbyMapPreview=null;lobbyPreviewMapId=null;lobbyChoiceSequence=lobbyChoiceEpoch=lobbyChoicePublication=0; lobbySettingsEditor.Reset(); lobbyRequests.Clear(); lobbyEpoch = null; lobbyOptions = null; formattedLobbyGold = BigInteger.Zero;
        }
        bool LobbyInputMaySubmit => page == "lobby" && modal == null && !UiPointerGate.Active && isActiveAndEnabled;
        void ApplyLobbySettings(ApplyLobbyOptions request)
        {
            if (!LobbyInputMaySubmit) { lobbySettingsEditor.Reject(request.RoomEpoch, request.RequestId, "Aktion abgebrochen."); return; }
            var o=request.Options;
            ulong sequence = session.ApplyHostRules(new MatchRules(o.WaveCount,BigInteger.Parse(o.StartGoldDecimal,CultureInfo.InvariantCulture),o.Endless,o.IncomeMultiplier,o.Format,o.MapId),
                ulong.Parse(request.RoomEpoch, CultureInfo.InvariantCulture), request.ExpectedConfigRevision);
            RegisterLobbyRequest(sequence, request.RoomEpoch, request.RequestId);
        }
        void SubmitLobbyReady(ReadyForLobby request)
        {
            if (!LobbyInputMaySubmit) { lobbySettingsEditor.RejectReady(request.RoomEpoch, request.RequestId, "Aktion abgebrochen."); return; }
            ulong sequence = session.SetLobbyReady(true, ulong.Parse(request.RoomEpoch, CultureInfo.InvariantCulture),
                request.ExpectedConfigRevision, request.ExpectedRosterRevision);
            RegisterLobbyRequest(sequence, request.RoomEpoch, request.RequestId);
        }
        void RegisterLobbyRequest(ulong sequence, string epoch, Guid requestId)
        {
            if (sequence == 0)
            { lobbySettingsEditor.Resolve(epoch, requestId, false, 0, session.Message); lobbyRulesPanel?.Refresh(); return; }
            lobbyRequests[sequence] = new LobbyUiRequest { Epoch = epoch, Id = requestId };
            // Host acknowledgements can occur synchronously inside the submission method.
            if (session.LastResult.Sequence == sequence && session.LastResultEpoch.ToString(CultureInfo.InvariantCulture) == epoch)
                ResolveLobbyCommand(session.LastResult);
            dirty = true;
        }
        void ResolveLobbyCommand(CommandResult result)
        {
            ResolveLobbyChoice(result);
            if (!lobbyRequests.TryGetValue(result.Sequence, out var request) ||
                session.LastResultEpoch.ToString(CultureInfo.InvariantCulture) != request.Epoch) return;
            lobbyRequests.Remove(result.Sequence);
            lobbySettingsEditor.Resolve(request.Epoch, request.Id, result.Accepted,
                session.LastResultPublicationSequence, result.Accepted ? null : LobbyRuleError(result.Code));
            lobbyRulesPanel?.Refresh();
        }
        static string LobbyRuleError(ErrorCode code)
        {
            switch (code)
            {
                case ErrorCode.StaleLobbyRevision: return "Der Raum hat sich geändert. Bitte die neuen Regeln prüfen.";
                case ErrorCode.MissingPlayers: return "Warte auf alle Spieler und vollständig besetzte Teams.";
                case ErrorCode.RulesPayloadTooLarge: return "Diese Goldzahl ist für diese Partie zu groß.";
                case ErrorCode.UnsupportedWaveCount: return "Diese Wellenzahl ist für die Karte nicht verfügbar.";
                case ErrorCode.WrongPhase: return "Die Partie ist bereits gestartet.";
                case ErrorCode.NotOwner: return "Nur der Host kann die Regeln ändern.";
                default: return ErrorText(code);
            }
        }
        bool ConsumeLobbyInputBack()
        {
            if (modal != null || EventSystem.current == null) return false;
            var selected = EventSystem.current.currentSelectedGameObject;
            var input = selected != null ? selected.GetComponent<InputField>() : null;
            if (input == null || !input.isFocused) return false;
            input.DeactivateInputField(); EventSystem.current.SetSelectedGameObject(null); return true;
        }
    }
}
