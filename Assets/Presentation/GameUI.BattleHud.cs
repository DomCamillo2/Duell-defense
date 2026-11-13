using System;
using DefenseDuell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        static readonly Color HudPanel=HudTheme.Panel,HudCard=HudTheme.Card,HudSelected=HudTheme.Selected,
            HudAction=HudTheme.Action,HudAttack=HudTheme.Card,HudText=HudTheme.Text,HudMuted=HudTheme.Muted,
            HudGold=HudTheme.Gold,HudEdge=HudTheme.Edge;
        const float AttackRailWidth=184, AttackRailReserve=252;
        TroopResearchSwipe[] troopSwipes;
        RectTransform contextDock,buildDock,feedbackPanel;
        Button buildPrevious,buildNext;
        Button[] attackHints,attackInfoLinks;
        Button attackPrevious,attackNext;
        int attackPage,attackRows;
        float attackTop,attackCardHeight;
        int buildPage;
        const float BuildCardWidth=96,BuildCardHeight=124;
        Image ownHealthFill,selectionPortrait;
        Image[] sendPortraits;
        Button upgradeDetailsButton;
        Button rangeUpgradeButton,returnOwnButton;
        Text zoomLabel;
        UpgradeBudgetGraphic upgradeBudget;
        void Game()
        {
            const float margin=12,gap=8,railWidth=AttackRailWidth,dockHeight=BuildCardHeight;
            float cardWidth=BuildCardWidth;
            var boardRect=UiKit.Rect("Your defense board",canvas.transform);UiKit.Fill(boardRect);
            boardRect.SetSiblingIndex(safe.GetSiblingIndex());fullScreenBoard=boardRect;
            ownBoard=boardRect.gameObject.AddComponent<BoardView>();ownBoard.Initialize(content,TowerSprites,UnitSprites,true,SelectEntity);
            ownBoard.RemotePlayback=!session.IsHost;
            ownBoard.GroundSelected+=SelectGround;ownBoard.ViewChanged+=BoardViewChanged;
            RefreshBoardOverviewInsets();

            var top=BattleButton(screen,"",margin,10,286,56,OpenWaveDetails,HudPanel,16);top.name="Battle resources";
            var topVisual=UiKit.ButtonContent(top);
            HudTheme.Icon(topVisual,HudSymbol.Shield,10,10,22,UiKit.Hex(0x9ED9BE));
            hpText=UiKit.TextAt(topVisual,"",36,3,78,34,16,HudText);
            UiKit.PanelAt(topVisual,"Base health track",12,42,94,4,HudCard);
            ownHealthFill=UiKit.PanelAt(topVisual,"Base health",12,42,94,4,UiKit.Hex(0x96CE9D));ownHealthFill.rectTransform.pivot=new Vector2(0,1);
            HudTheme.Icon(topVisual,HudSymbol.Coin,120,7,23,HudGold);
            goldText=UiKit.TextAt(topVisual,"",147,0,129,32,22,HudGold,TextAnchor.MiddleCenter);
            CreateIncomeHud(topVisual);
            CreateTempoControl(492);
            var menu=BattleButton(screen,"Menü",width-76,10,64,56,OpenBattleMenu,HudPanel,13);menu.name="Battle menu";
            HudTheme.Icon(UiKit.ButtonContent(menu),HudSymbol.Menu,23,6,18,HudGold);
            UiKit.At(menu.GetComponentInChildren<Text>().rectTransform,4,27,56,22);

            var pip=BattleButton(screen,"",width-152,74,140,94,ToggleBattleView,HudPanel,14);pip.name="Switch battlefield";
            var pipVisual=UiKit.ButtonContent(pip);
            opponentText=UiKit.TextAt(pipVisual,"",6,2,128,24,12,HudGold,TextAnchor.MiddleCenter);
            var enemyRect=UiKit.Rect("Opponent preview",pipVisual);UiKit.At(enemyRect,6,28,128,60);
            enemyBoard=enemyRect.gameObject.AddComponent<BoardView>();enemyBoard.Initialize(content,TowerSprites,UnitSprites,false,null);enemyBoard.RemotePlayback=!session.IsHost;
            attackRows=units.Length>4?3:units.Length;attackPage=0;
            float attackHeight=Mathf.Min(120,(height-252-(attackRows-1)*gap-margin)/attackRows),attackY=height-margin-attackRows*attackHeight-(attackRows-1)*gap;
            attackTop=attackY;attackCardHeight=attackHeight;attackHints=new Button[units.Length];
            attackPrevious=BattleButton(screen,"‹ Klassen",width-AttackRailReserve,attackY-78,118,44,()=>ChangeAttackPage(-1),HudPanel,12);attackPrevious.name="Previous attack classes";
            attackNext=BattleButton(screen,"Klassen ›",width-130,attackY-78,118,44,()=>ChangeAttackPage(1),HudPanel,12);attackNext.name="Next attack classes";
            CreateAttackTargetControl();
            var attackHeading=BattlePanel(screen,"Attack heading",width-margin-railWidth,attackY-30,railWidth,22,HudPanel);
            HudTheme.Icon(attackHeading.transform,HudSymbol.Attack,7,3,16,HudGold);
            queueText=UiKit.TextAt(attackHeading.transform,"ANGRIFF",25,0,railWidth-30,22,12,HudText,TextAnchor.MiddleCenter);
            sendPortraits=new Image[units.Length];sendButtons=new Button[units.Length];sendPrices=new Text[units.Length];sendCounts=new Text[units.Length];
            sendDelayText=new Text[units.Length];displayedSendForecasts=new SendQueueEstimate[units.Length];nextAdditionalForecastAt=0;sendQuoteText=new Text[units.Length];displayedSendQuotes=new SendQuote[units.Length];
            buildPrices=new Text[towers.Length];buildNames=new Text[towers.Length];
            troopSwipes=new TroopResearchSwipe[units.Length];
            for(int i=0;i<units.Length;i++)
            {
                int k=i;var b=BattleButton(screen,"",width-margin-railWidth,attackY+i*(attackHeight+gap),railWidth,attackHeight,()=>Send(CommandType.SendUnit,k),HudAttack,12);
                b.name="Send class "+i;sendButtons[i]=b;DecorateClassCard(b,i,false,railWidth,attackHeight);
                troopSwipes[i]=b.gameObject.AddComponent<TroopResearchSwipe>();
                troopSwipes[i].Initialize((PointerSafeButton)b,()=>Send(CommandType.SendUnit,k),()=>ToggleAttackInfo(k),
                    ()=>page=="game"&&modal==null&&!InfoBlocking&&dragSource==null&&session.State!=null,()=>settings.ReducedEffects);
                var hint=BattleButton(screen,"←\nUpgrade",width-AttackRailReserve+4,attackY+i*(attackHeight+gap)+52,52,52,()=>ToggleAttackInfo(k),HudCard,10);
                UiKit.Fill(hint.GetComponentInChildren<Text>().rectTransform,4,4,4,4);
                attackHints[i]=hint;hint.name="Research swipe alternative "+i;((PointerSafeButton)hint).PreparePress=PrepareAttackInfoPress;
            }

            float dockWidth=towers.Length*cardWidth+(towers.Length-1)*gap;
            buildDock=UiKit.Rect("Build cards",screen);UiKit.At(buildDock,margin,height-margin-dockHeight,dockWidth,dockHeight);
            buildPage=0;
            buildPrevious=BattleButton(buildDock,"‹",0,40,44,44,()=>{if(!UiPointerGate.Active){buildPage=Mathf.Max(0,buildPage-1);dirty=true;}},HudPanel,28);buildPrevious.name="Previous tower classes";
            buildNext=BattleButton(buildDock,"›",0,40,44,44,()=>{if(!UiPointerGate.Active){buildPage++;dirty=true;}},HudPanel,28);buildNext.name="Next tower classes";
            buildButtons=new Button[towers.Length];
            for(int i=0;i<towers.Length;i++)
            {
                int k=i;var b=BattleButton(buildDock,"",i*(cardWidth+gap),0,cardWidth,dockHeight,()=>
                {
                    if(dragging||InfoBlocking||modal!=null||BuildPurchaseState(k)!=PurchaseState.Available)return;selectedKind=k;buildGuideActive=true;selectedTower=selectedSlot=-1;hasSelectedPoint=false;ownBoard.ClearDragTarget();notice="Gebäude auf freien Boden ziehen. Alternativ Boden antippen und bestätigen.";
                    feedbackUntil=Time.unscaledTimeAsDouble+3;Sound();dirty=true;

                },HudCard,14);
                b.name="Build class "+i;buildButtons[i]=b;DecorateClassCard(b,i,true,cardWidth,dockHeight);b.gameObject.AddComponent<BuildDragSource>().Initialize(this,i);
            }
            float contextWidth=Mathf.Min(824,width-AttackRailReserve-12),close=56,info=Mathf.Clamp(contextWidth*.35f,230,288),actions=(contextWidth-info-close-32)/3;
            var context=BattlePanel(screen,"Selected building actions",margin,height-margin-124,contextWidth,124,HudPanel);context.raycastTarget=true;contextDock=context.rectTransform;
            selectionPortrait=UiKit.SpriteAt(contextDock,GeneratedClassIcons.Get(selectedKind,true),8,8,72,82,Color.white);selectionPortrait.name="Selected class portrait";
            selectionText=UiKit.TextAt(contextDock,"",86,8,info-98,60,13,HudText);
            upgradeDetailsButton=BattleButton(contextDock,"",8,8,72,82,OpenUpgradeDetails,Color.clear,12);
            upgradeDetailsButton.name="Building upgrade details";upgradeDetailsButton.targetGraphic.enabled=false;
            HudTheme.Icon(UiKit.ButtonContent(upgradeDetailsButton),HudSymbol.Info,49,55,20,HudGold);
            var budget=UiKit.Rect("Shared upgrade budget",contextDock);UiKit.At(budget,16,96,54,20);upgradeBudget=budget.gameObject.AddComponent<UpgradeBudgetGraphic>();upgradeBudget.raycastTarget=false;
            confirmBuild=UiKit.ActionButtonAt(contextDock,"Confirm build",info,12,actions,100,ConfirmBuild,ActionSymbol.Build);
            upgradeButton=UiKit.ActionButtonAt(contextDock,"Upgrade damage",info,12,actions,100,()=>UpgradeSelectedPath(false),ActionSymbol.Damage);
            rangeUpgradeButton=UiKit.ActionButtonAt(contextDock,"Upgrade range",info+actions+8,12,actions,100,()=>UpgradeSelectedPath(true),ActionSymbol.Range);
            sellButton=UiKit.ActionButtonAt(contextDock,"Sell tower",info+2*(actions+8),12,actions,100,Sell,ActionSymbol.Sell);
            cancelSelection=UiKit.ActionButtonAt(contextDock,"Cancel selection",contextWidth-close-8,12,close,100,ClearBattleSelection,ActionSymbol.Close);

            CreateUpgradeModes(contextWidth);

            var zoomOut=BattleButton(screen,"−",12,74,44,44,()=>ownBoard.SetZoom(ownBoard.ZoomFactor/1.25f),HudPanel,26);zoomOut.name="Zoom out";HudTheme.IconButton(zoomOut,HudSymbol.Minus);
            var zoomIn=BattleButton(screen,"+",62,74,44,44,()=>ownBoard.SetZoom(ownBoard.ZoomFactor*1.25f),HudPanel,24);zoomIn.name="Zoom in";HudTheme.IconButton(zoomIn,HudSymbol.Plus);
            var reset=BattleButton(screen,"1:1",112,74,44,44,()=>ownBoard.ResetView(),HudPanel,14);reset.name="Reset view";HudTheme.IconButton(reset,HudSymbol.Overview);zoomLabel=null;
            returnOwnButton=BattleButton(screen,"Eigenes Feld",12,126,200,44,()=>{if(showEnemy)ToggleBattleView();},HudAction,16);returnOwnButton.name="Return own field";
            HudTheme.Icon(UiKit.ButtonContent(returnOwnButton),HudSymbol.Back,8,10,24);
            UiKit.Fill(returnOwnButton.GetComponentInChildren<Text>().rectTransform,36,5,8,5);
            var feedback=BattlePanel(screen,"Battle notice",12,height-240,Mathf.Min(480,width-AttackRailReserve),44,HudPanel);feedback.raycastTarget=false;feedbackPanel=feedback.rectTransform;
            noticeText=UiKit.TextAt(feedbackPanel,notice,12,2,feedbackPanel.rect.width-24,40,14,HudText);
            if(tutorialStep>=0){noticeText.gameObject.SetActive(false);tutorialText=UiKit.TextAt(feedbackPanel,"",12,2,feedbackPanel.rect.width-24,40,14,HudText);}
            CreateWaveHud();
            CreateAttackInfo(attackY,attackHeight,gap,railWidth);
            RefreshBattleChrome();
        }
        static Image BattlePanel(Transform parent,string name,float x,float y,float w,float h,Color color)
            =>HudTheme.PanelAt(parent,name,x,y,w,h,color);
        static Button BattleButton(Transform parent,string value,float x,float y,float w,float h,Action click,Color background,int fontSize)
            =>HudTheme.ButtonAt(parent,value,x,y,w,h,click,background,fontSize);
        static readonly Color[] ClassAccents={UiKit.Hex(0x79B54B),UiKit.Hex(0xD98545),UiKit.Hex(0x56BDCE),UiKit.Hex(0x9574BC),UiKit.Hex(0xE7BC59)};
        Text[] buildPrices,buildNames,sendPrices,sendCounts;
        void DecorateClassCard(Button button,int kind,bool tower,float w,float h)
        {
            var visual=UiKit.ButtonContent(button);
            var label=button.GetComponentInChildren<Text>();label.text=tower?"":units[kind].Name;
            UiKit.At(label.rectTransform,4,tower?80:3,w-8,22);label.fontSize=tower?12:16;
            if(tower)buildNames[kind]=label;
            Color accent=ClassAccents[(tower?kind:kind==1?2:kind==2?1:kind)%ClassAccents.Length];
            ((HudFrameGraphic)button.targetGraphic).SetStyle(HudFrameStyle.Card,accent);
            float size=tower?76:56;
            float px=tower?(w-size)*.5f:8,py=tower?3:28;
            var well=HudTheme.PanelAt(visual,"Portrait recess",px,py,size,size,HudPanel,HudFrameStyle.Well);well.SetStyle(HudFrameStyle.Well,accent);
            var picture=UiKit.SpriteAt(visual,GeneratedClassIcons.Get(kind,tower),px,py,size,size,Color.white);picture.name="Generated class portrait";
            var plaque=HudTheme.PanelAt(visual,"Price plaque",tower?8:74,tower?h-24:32,tower?w-16:w-82,tower?20:26,HudGold,HudFrameStyle.Price);
            var price=UiKit.Label(plaque.transform,"",tower?15:19,HudPanel,TextAnchor.MiddleCenter);price.fontStyle=FontStyle.Bold;
            if(tower)buildPrices[kind]=price;
            else
            {
                sendPortraits[kind]=picture;sendPrices[kind]=price;
                sendQuoteText[kind]=UiKit.TextAt(visual,"",70,60,w-74,18,12,HudText,TextAnchor.MiddleCenter);
                sendDelayText[kind]=UiKit.TextAt(visual,"",70,78,w-74,16,11,HudMuted,TextAnchor.MiddleCenter);
                var count=HudTheme.PanelAt(visual,"Paid packet badge",44,64,28,22,HudPanel,HudFrameStyle.Well);
                sendCounts[kind]=UiKit.Label(count.transform,"",12,HudText,TextAnchor.MiddleCenter);
            }
            button.gameObject.AddComponent<PurchaseAvailability>().Initialize(button);
        }
        void ChangeAttackPage(int delta)
        {
            if(modal!=null||InfoBlocking||UiPointerGate.Active||dragSource!=null)return;
            CancelInteraction();attackPage=Mathf.Clamp(attackPage+delta,0,(units.Length-1)/attackRows);dirty=true;PackAttackCards();
        }
        void PackAttackCards()
        {
            if(sendButtons==null||attackRows<1)return;
            int pages=(units.Length+attackRows-1)/attackRows;attackPage=Mathf.Clamp(attackPage,0,pages-1);
            for(int i=0;i<sendButtons.Length;i++)
            {
                bool shown=i/attackRows==attackPage;float y=attackTop+(i%attackRows)*(attackCardHeight+8);
                sendButtons[i].gameObject.SetActive(shown);attackHints[i].gameObject.SetActive(shown);
                UiKit.At((RectTransform)sendButtons[i].transform,width-12-AttackRailWidth,y,AttackRailWidth,attackCardHeight);
                UiKit.At((RectTransform)attackHints[i].transform,width-AttackRailReserve+4,y+52,52,52);
                if(attackInfoLinks!=null){attackInfoLinks[i].gameObject.SetActive(shown);UiKit.At((RectTransform)attackInfoLinks[i].transform,width-AttackRailReserve+8,y,44,44);}
            }
            attackPrevious.gameObject.SetActive(pages>1);attackNext.gameObject.SetActive(pages>1);
            attackPrevious.interactable=attackPage>0;attackNext.interactable=attackPage+1<pages;
        }
        void PackBuildCards()
        {
            if(buildDock==null||buildButtons==null||session?.State==null)return;
            const float gap=8;
            int mask=session.State.Players[(int)session.LocalSlot].AvailableTowerMask,visible=0;
            for(int k=0;k<buildButtons.Length;k++)if((mask&(1<<k))!=0)visible++;
            float available=Mathf.Max(BuildCardWidth,width-AttackRailReserve-24);
            bool paged=visible*(BuildCardWidth+gap)-gap>available;
            int perPage=paged?Mathf.Max(1,Mathf.FloorToInt((available-104+gap)/(BuildCardWidth+gap))):Mathf.Max(1,visible);
            int pages=Mathf.Max(1,Mathf.CeilToInt((float)visible/perPage));buildPage=Mathf.Clamp(buildPage,0,pages-1);
            int ordinal=0,onPage=0;
            for(int k=0;k<buildButtons.Length;k++)
            {
                bool owned=(mask&(1<<k))!=0;
                bool shown=owned&&ordinal/perPage==buildPage;
                if(owned)ordinal++;
                buildButtons[k].gameObject.SetActive(shown);
                if(shown)UiKit.At((RectTransform)buildButtons[k].transform,(paged?52:0)+onPage++*(BuildCardWidth+gap),0,BuildCardWidth,BuildCardHeight);
            }
            float span=onPage*(BuildCardWidth+gap)-gap;
            buildPrevious.gameObject.SetActive(paged);buildNext.gameObject.SetActive(paged);
            buildPrevious.interactable=buildPage>0;buildNext.interactable=buildPage+1<pages;
            UiKit.At((RectTransform)buildNext.transform,52+span+8,40,44,44);
            buildDock.sizeDelta=new Vector2(Mathf.Max(0,span)+(paged?104:0),BuildCardHeight);
        }
        void OpenBattleMenu()
        {
            if(InfoBlocking||modal!=null||dragging||UiPointerGate.Active)return;
            OpenModal("DUELL",420,280);
            DialogButton(modal,"Ton & Anzeige",24,100,372,64,OpenSettings,UiKit.PanelLight,21);
            DialogButton(modal,"Duell verlassen",24,180,372,64,ConfirmLeave,UiKit.PanelLight,21);
        }
        void ClearBattleSelection()
        {
            CancelInteraction();selectedSlot=selectedTower=selectedSite=-1;hasSelectedPoint=false;notice="";dirty=true;RefreshBattleChrome();
        }
        void BoardViewChanged()
        {
            if(updatingOverviewInsets)return;
            hasSelectedPoint=false;selectedSlot=selectedTower=-1;ownBoard.ClearDragTarget();if(dragSource!=null)CancelInteraction();
            if(zoomLabel!=null)zoomLabel.text=Mathf.RoundToInt(ownBoard.ZoomFactor*100)+" %";dirty=true;
        }
        void ToggleBattleView()
        {
            CloseAttackInfo();CancelInteraction();commands.DiscardVisuals();CancelBoardFeedback();showEnemy=!showEnemy;selectedSlot=selectedTower=-1;hasSelectedPoint=false;ownBoard.ResetView();
            notice=showEnemy?"Gegnerisches Feld · zoomen und verschieben. Deine Angreifer sind hier unterwegs.":"Deine Verteidigung · alle Gebäude frei platzieren";feedbackUntil=Time.unscaledTimeAsDouble+2.5;dirty=true;
        }
        void RefreshBattleChrome()
        {
            if(page!="game"||contextDock==null)return;
            PackBuildCards();PackAttackCards();RefreshPlacementGuide();
            bool contextVisible=(selectedTower>=0||hasSelectedPoint)&&!showEnemy&&!dragging&&(!hasSelectedPoint||!commands.IsBuildReserved(selectedPoint,selectedSite,PlacementValidator.BuildingSeparation));
            contextDock.gameObject.SetActive(contextVisible);buildDock.gameObject.SetActive(!contextVisible&&!showEnemy);
            if(returnOwnButton!=null)returnOwnButton.gameObject.SetActive(showEnemy);
            if(ownHealthFill!=null&&session.State!=null)ownHealthFill.rectTransform.localScale=new Vector3(Mathf.Clamp01(session.State.Players[(int)session.LocalSlot].Hp/(float)(content.BaseHp*content.MatchRules.MembersPerTeam)),1,1);
            if(feedbackPanel!=null)feedbackPanel.gameObject.SetActive(tutorialStep>=0||(!dragging&&!string.IsNullOrEmpty(notice)&&Time.unscaledTimeAsDouble<feedbackUntil));
        }
    }
}
