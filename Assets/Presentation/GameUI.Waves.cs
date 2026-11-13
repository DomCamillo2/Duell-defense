using BigInteger = System.Numerics.BigInteger;
using System.Text;
using System.Collections.Generic;
using DefenseDuell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        Text wavePhaseText, wavePreviewText, waveReadyText, fifoText,paidQueueDetailText;
        RectTransform wavePreparation,paidQueueContent;
        ScrollRect paidQueueScroll;
        Button paidQueueButton;
        string wavePreviewCopy="",paidQueueCopy="";
        Text[] sendQuoteText,sendDelayText;
        SendQuote[] displayedSendQuotes;
        Button waveStartButton, priorityButton, researchButton;
        ResearchQuote displayedResearchQuote;
        GameObject modalPreviousFocus;
        readonly List<SpawnOrder> visiblePaidOrders=new List<SpawnOrder>();
        readonly List<Text> virtualQueueRows=new List<Text>();
        bool virtualQueue;
        const float QueueRowHeight=26;

        string IncomeText(BigInteger value)=>GameNumbers.Ratio(value,content.WaveRules.IncomeUnitsPerGold);
        void CreateWaveHud()
        {
            var wave=BattleButton(screen,"",306,10,178,56,OpenWaveDetails,HudPanel,16);wave.name="Wave details";
            var waveVisual=UiKit.ButtonContent(wave);
            HudTheme.Icon(waveVisual,HudSymbol.Wave,10,8,21,HudGold);
            timeText=UiKit.TextAt(waveVisual,"",37,0,111,28,19,HudText);
            wavePhaseText=UiKit.TextAt(waveVisual,"",12,29,138,22,13,HudMuted);
            HudTheme.Icon(waveVisual,HudSymbol.Chevron,151,20,16,HudMuted);
            wavePreparation=UiKit.Rect("Wave preparation",screen);UiKit.At(wavePreparation,306,74,360,44);
            waveReadyText=UiKit.TextAt(wavePreparation,"",0,0,144,44,13,HudText,TextAnchor.MiddleCenter);
            waveReadyText.gameObject.AddComponent<Shadow>().effectColor=new Color(0,0,0,.8f);
            waveStartButton=BattleButton(wavePreparation,"Früher starten",152,0,208,44,ToggleWaveReady,HudAction,14);
            waveStartButton.name="Wave ready";
            paidQueueButton=BattleButton(screen,"",12,height-188,Mathf.Min(420,width-184),44,OpenPaidQueueDetails,HudPanel,12);
            paidQueueButton.name="Own paid attack queue";
            fifoText=UiKit.TextAt(UiKit.ButtonContent(paidQueueButton),"",12,2,((RectTransform)paidQueueButton.transform).rect.width-24,40,13,HudText);
            priorityButton=BattleButton(contextDock,"",86,72,Mathf.Clamp(contextDock.rect.width*.35f,230,288)-98,44,TogglePriority,HudCard,12);
            priorityButton.name="Tower target priority";
        }
        void ToggleWaveReady()
        {
            var state=session?.State;
            if(state==null||state.Phase!=MatchPhase.Playing||state.WavePhase!=WavePhase.Preparing)return;
            if(commands.HasPending(CommandType.SetWaveReady))return;
            Send(CommandType.SetWaveReady,state.Players[(int)session.LocalSlot].WaveReady?0:1);
        }
        void TogglePriority()
        {
            if(session?.State==null)return;
            foreach(var tower in session.State.Towers)
                if(tower.Id==selectedTower&&tower.Owner==session.LocalSlot&&!towers[tower.Kind].IsEconomy)
                {Send(CommandType.SetTowerPriority,tower.Priority==TargetPriority.Front?1:0,tower.Id);return;}
        }
        void RefreshWaveHud()
        {
            var state=session.State;var player=state.Players[(int)session.LocalSlot];
            bool playing=state.Phase==MatchPhase.Playing,preparing=playing&&state.WavePhase==WavePhase.Preparing;
            wavePreparation.gameObject.SetActive(preparing);
            if(!playing)
            {
                queueForecast=default;visiblePaidOrders.Clear();
                wavePhaseText.text="Duell startet";wavePreviewCopy=content.MatchRules.Endless?"Endlos: Verteidige dein Team so lange wie möglich.":"Verteidige deine Basis über "+content.WaveCount+" Wellen.";
                waveStartButton.interactable=false;waveReadyText.text="";fifoText.text="";paidQueueCopy="Noch keine bezahlten Angriffe";
                paidQueueButton.gameObject.SetActive(false);RefreshBattleDetails();return;
            }
            BigInteger wave=state.WaveId;
            wavePhaseText.text=preparing?"Baupause · "+Mathf.CeilToInt(state.PreparationRemaining/(float)content.TickRate)+" s":
                state.WavePhase==WavePhase.Active?"Welle läuft":"Aufräumen";
            var currentProfile=content.GetWave(wave);
            wavePhaseText.text=(currentProfile.IsSuperBossWave?"SUPERBOSS · ":currentProfile.IsBossWave?"BOSS · ":"")+(preparing?"Baupause · "+Mathf.CeilToInt(state.PreparationRemaining/(float)content.TickRate)+" s":currentProfile.Title);
            wavePreviewCopy=WavePreview16.Describe(content,wave);
            bool pending=commands.HasPending(CommandType.SetWaveReady);
            waveStartButton.interactable=preparing&&player.Hp>0&&!pending;
            UiKit.ButtonText(waveStartButton,pending?"Wird angefragt …":player.WaveReady?"Bereit zurücknehmen":"Früher starten");
            waveReadyText.text=ReadyParticipantCount(false)+" / "+LivingParticipantCount()+" bereit";
            var fifo=new StringBuilder();int orders=0;visiblePaidOrders.Clear();
            foreach(var order in state.SpawnOrders)
            {
                if(order.Sender!=session.LocalSlot)continue;orders++;visiblePaidOrders.Add(order);
                if(orders<=200){if(fifo.Length>0)fifo.Append('\n');fifo.Append(FormatPaidOrder(order,orders));}
            }
            queueForecast=SendQueueForecast.Estimate(content,state,session.LocalSlot);
            string wait=ForecastStartText(queueForecast,false,false);
            fifoText.text=orders==0?"":orders+(orders==1?" Paket · ":" Pakete · ")+QueueBoundGold(queueForecast)+" G gebunden  ›\nStart: "+wait;
            paidQueueCopy=orders==0?"Keine wartenden Aufträge":QueueForecastDetails(queueForecast)+(orders<=200?"\n\n"+fifo:"");
            paidQueueButton.gameObject.SetActive(orders>0);
            RefreshBattleDetails();
        }
        void OpenWaveDetails()
        {
            if(InfoBlocking||modal!=null||dragging||UiPointerGate.Active)return;
            OpenModal("WELLEN & GOLD",620,440);
            wavePreviewText=UiKit.TextAt(modal,"",28,96,564,316,18,DialogTextColor);
            RefreshBattleDetails();
        }
        void OpenPaidQueueDetails()
        {
            if(InfoBlocking||modal!=null||dragging||UiPointerGate.Active)return;
            float w=Mathf.Min(620,width-48),h=Mathf.Min(540,height-48);
            OpenModal("BEZAHLTE ANGRIFFE",w,h);
            var viewport=UiKit.Rect("Paid queue viewport",modal);UiKit.At(viewport,24,94,w-96,h-120);
            var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
            paidQueueScroll=viewport.gameObject.AddComponent<ScrollRect>();paidQueueScroll.horizontal=false;paidQueueScroll.vertical=true;
            paidQueueScroll.movementType=ScrollRect.MovementType.Clamped;paidQueueScroll.scrollSensitivity=28;paidQueueScroll.viewport=viewport;
            paidQueueContent=UiKit.Rect("All paid orders",viewport);UiKit.At(paidQueueContent,0,0,w-96,h-120);
            paidQueueDetailText=UiKit.TextAt(paidQueueContent,"",0,0,w-96,h-120,17,DialogTextColor,TextAnchor.UpperLeft);paidQueueDetailText.raycastTarget=true;
            paidQueueScroll.content=paidQueueContent;virtualQueueRows.Clear();
            paidQueueScroll.onValueChanged.AddListener(_=>RefreshVirtualQueueRows());
            var track=UiKit.PanelAt(modal,"Queue scrollbar",w-64,94,44,h-120,HudCard);track.raycastTarget=true;
            var bar=track.gameObject.AddComponent<Scrollbar>();bar.direction=Scrollbar.Direction.BottomToTop;
            var handle=UiKit.PanelAt(track.transform,"Scroll handle",0,0,44,44,HudGold);
            handle.rectTransform.sizeDelta=Vector2.zero;
            bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;bar.numberOfSteps=0;paidQueueScroll.verticalScrollbar=bar;
            RefreshBattleDetails();paidQueueScroll.verticalNormalizedPosition=1;
        }
        void RefreshBattleDetails()
        {
            if(wavePreviewText!=null&&session?.State!=null)
            {
                var state=session.State;var player=state.Players[(int)session.LocalSlot];
                wavePreviewText.text=content.Map.Name+" · Welle "+state.WaveId+" / "+WaveLimitLabel+"\n"+wavePhaseText.text+"\n\n"+wavePreviewCopy+
                    "\n\nEinkommen ("+content.MatchRules.IncomeMultiplier+"×): +"+IncomeText(EconomyRules.IncomeUnits(content,state,session.LocalSlot))+" G/s pro Spielsekunde"+
                    "\nDavon Angriffe: +"+IncomeText(EconomyRules.EffectiveAttackIncomeUnits(content,player))+" G/s"+
                    "\n2× Tempo: doppelte Rate pro Echtsekunde."+
                    "\nWährend der Baupause: 0 G/s, kein Ausmarsch.";
            }
            if(paidQueueDetailText!=null)
            {
                virtualQueue=visiblePaidOrders.Count>200;paidQueueDetailText.text=paidQueueCopy;
                float h=virtualQueue?QueueHeaderHeight+visiblePaidOrders.Count*QueueRowHeight:Mathf.Max(paidQueueScroll.viewport.rect.height,paidQueueDetailText.preferredHeight+12);
                paidQueueContent.sizeDelta=new Vector2(paidQueueContent.sizeDelta.x,h);
                paidQueueDetailText.rectTransform.sizeDelta=new Vector2(paidQueueDetailText.rectTransform.sizeDelta.x,virtualQueue?QueueHeaderHeight-4:h);
                RefreshVirtualQueueRows();
            }
        }
        string FormatPaidOrder(SpawnOrder order,int index)=>index+". W"+order.WaveId+" "+units[order.Kind].Name+" S"+GameNumbers.Compact(order.ResearchLevel)+" ×"+order.Remaining+" → Spieler "+((int)order.EnemyTarget+1);
        void RefreshVirtualQueueRows()
        {
            if(paidQueueScroll==null||paidQueueContent==null)return;
            if(!virtualQueue){foreach(var row in virtualQueueRows)if(row!=null)row.gameObject.SetActive(false);return;}
            int needed=Mathf.CeilToInt(paidQueueScroll.viewport.rect.height/QueueRowHeight)+2;
            while(virtualQueueRows.Count<needed)
            {
                var row=UiKit.TextAt(paidQueueContent,"",0,0,paidQueueContent.rect.width,QueueRowHeight,17,DialogTextColor,TextAnchor.MiddleLeft);
                row.raycastTarget=true;virtualQueueRows.Add(row);
            }
            int first=Mathf.Clamp(Mathf.FloorToInt((paidQueueContent.anchoredPosition.y-QueueHeaderHeight)/QueueRowHeight),0,Mathf.Max(0,visiblePaidOrders.Count-1));
            for(int i=0;i<virtualQueueRows.Count;i++)
            {
                int index=first+i;var row=virtualQueueRows[i];bool show=i<needed&&index<visiblePaidOrders.Count;
                row.gameObject.SetActive(show);if(!show)continue;
                UiKit.At(row.rectTransform,0,QueueHeaderHeight+index*QueueRowHeight,paidQueueContent.rect.width,QueueRowHeight);
                row.text=FormatPaidOrder(visiblePaidOrders[index],index+1);
            }
        }
        string PaidQueueWait(SpawnOrder head)
        {
            if(head==null)return "";
            var state=session.State;
            if(head.WaveId>state.WaveId)return "Wartet auf Welle "+head.WaveId;
            if(state.WavePhase==WavePhase.Preparing)return "Vorbereitung pausiert Ausmarsch";
            int defender=(int)head.EnemyTarget;
            int deadline=Mathf.Max(state.NextSpawnTicks[defender],head.NextTick);
            if(head.Remaining==head.InitialCount)deadline=Mathf.Max(deadline,state.Players[(int)session.LocalSlot].NextSendTicks[head.Kind]);
            if(deadline>state.PlayTicks)return "Ausmarschabstand / Klassenpause";
            int cursor=state.NeutralCursor[defender];
            bool neutralReady=cursor<content.GetWave(state.WaveId).TotalCount
                &&state.PlayTicks>=state.WaveStartPlayTick+cursor*content.WaveRules.NeutralSpawnIntervalTicks;
            if(neutralReady&&state.NextSpawnOrigins[defender]==UnitOrigin.NeutralWave)return "Grundwelle zuerst · danach dein Angriff";
            return "Nächster Angreifer bereit";
        }
        void RefreshSendQuotes()
        {
            var state=session.State;RefreshAdditionalForecasts();
            for(int k=0;k<units.Length;k++)
            {
                var quote=SendRules.Quote(content,state,session.LocalSlot,k);displayedSendQuotes[k]=quote;
                var missing=BigInteger.Max(0,units[k].Price-state.Players[(int)session.LocalSlot].Gold);
                sendDelayText[k].text=missing>0?"Fehlen "+GameNumbers.Compact(missing)+" G":AdditionalForecastStartText(k,true);
                sendQuoteText[k].text=quote.WaveId>0?"W"+quote.WaveId+" · S"+GameNumbers.Compact(quote.ResearchLevel):
                    quote.Code==ErrorCode.WaveCapacity?"Keine weitere Welle":"Ab Spielbeginn";
                var availability=quote.IsValid?PurchaseState.Available:quote.Code==ErrorCode.InsufficientGold?PurchaseState.Unaffordable:
                    quote.Code==ErrorCode.QueueFull||quote.Code==ErrorCode.WaveCapacity?PurchaseState.Limit:PurchaseState.Inactive;
                SetPurchaseState(sendButtons[k],availability,HudAttack);
            }
        }
        void RefreshPriority(TowerState tower)
        {
            if(priorityButton==null)return;
            bool visible=tower!=null&&!towers[tower.Kind].IsEconomy&&!showEnemy&&!dragging;
            priorityButton.gameObject.SetActive(visible);
            if(!visible)return;
            UiKit.ButtonText(priorityButton,"Ziel: "+(tower.Priority==TargetPriority.Front?"Vorne":"Stärkster")+"  ⇄");
            priorityButton.interactable=session.State.Phase==MatchPhase.Playing&&!commands.HasPending(CommandType.SetTowerPriority);
        }
        void ResearchFromInfo()
        {
            if(!InfoBlocking||attackInfo==null||!attackInfo.Open||infoKind<0||modal!=null||dragging||UiPointerGate.Active)return;
            if(commands.HasPending(CommandType.ResearchUnit,infoKind))return;
            if(!displayedResearchQuote.IsValid)return;
            var current=UnitResearchRules.Quote(content,session.State.Players[(int)session.LocalSlot],infoKind);
            if(!current.IsValid||current.Level!=displayedResearchQuote.Level||current.Price!=displayedResearchQuote.Price){RefreshAttackInfo();RejectPurchase("Forschung hat sich geändert. Bitte erneut prüfen.",researchButton);return;}
            if(!content.MatchRules.Endless&&session.State.WaveId==content.WaveCount&&session.State.WavePhase==WavePhase.Clearing)return;
            Send(CommandType.ResearchUnit,infoKind,-1,null,researchButton,true);
        }
    }
}
