using BigInteger = System.Numerics.BigInteger;
using DefenseDuell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        BattleInfoPanel attackInfo;
        RectTransform attackInfoContent;
        Text attackInfoTitle,attackInfoBody,attackInfoPrice,researchDetails;
        Image attackInfoPortrait;
        int infoKind=-1;
        float infoRowY,infoRowHeight,infoRowGap;
        bool InfoBlocking=>attackInfo!=null&&attackInfo.Blocking;

        void CreateAttackInfo(float y,float rowHeight,float gap,float railWidth)
        {
            infoKind=-1;infoRowY=y;infoRowHeight=rowHeight;infoRowGap=gap;
            var shield=UiKit.Rect("Attack info shield",screen);UiKit.Fill(shield);
            var surface=shield.gameObject.AddComponent<Image>();surface.color=new Color(0,0,0,.08f);surface.raycastTarget=true;
            attackInfo=shield.gameObject.AddComponent<BattleInfoPanel>();
            float panelWidth=Mathf.Min(344,width-220);
            var panel=BattlePanel(shield,"Attack details",0,0,panelWidth,350,HudPanel);panel.raycastTarget=true;
            attackInfoContent=panel.rectTransform;panel.gameObject.AddComponent<BattleInfoSurface>();
            attackInfoPortrait=UiKit.SpriteAt(panel.transform,null,14,14,72,72,Color.white);
            attackInfoTitle=UiKit.TextAt(panel.transform,"",100,14,panelWidth-158,32,21,HudText);attackInfoTitle.fontStyle=FontStyle.Bold;
            attackInfoPrice=UiKit.TextAt(panel.transform,"",100,50,panelWidth-114,36,16,HudGold);
            attackInfoBody=UiKit.TextAt(panel.transform,"",20,94,panelWidth-40,130,13,HudText);
            var close=BattleButton(panel.transform,"×",panelWidth-52,8,44,44,()=>attackInfo.Close(),HudCard,25);close.name="Close attack info";HudTheme.IconButton(close,HudSymbol.Close);
            researchDetails=UiKit.TextAt(panel.transform,"",16,224,panelWidth-76,50,13,HudGold);
            var exact=BattleButton(panel.transform,"",panelWidth-56,225,44,44,OpenResearchDetails,HudPanel,12);exact.name="Research exact values";HudTheme.IconButton(exact,HudSymbol.Info);
            researchButton=BattleButton(panel.transform,"",16,280,panelWidth-32,54,ResearchFromInfo,HudAction,14);researchButton.name="Research troop";
            attackInfo.Initialize(attackInfoContent);attackInfo.SetReducedEffects(settings.ReducedEffects);
            // Above the dismiss shield so another information button can change/toggle the same panel.
            var controls=UiKit.Rect("Attack info controls",screen);UiKit.Fill(controls);attackInfo.SetNavigationRoot(controls);
            attackInfoLinks=new Button[units.Length];
            for(int i=0;i<units.Length;i++)
            {
                int kind=i;var button=BattleButton(controls,"i",width-AttackRailReserve+8,y+i*(rowHeight+gap),44,44,()=>ToggleAttackInfo(kind),HudPanel,22);
                attackInfoLinks[i]=button;button.name="Attack info "+i;((PointerSafeButton)button).PreparePress=PrepareAttackInfoPress;HudTheme.IconButton(button,HudSymbol.Info);
            }
        }
        void PrepareAttackInfoPress()
        {
            // Cancel only troop contacts. A construction drag continues to own the gate.
            if(page!="game"||modal!=null||dragSource!=null)return;
            if(troopSwipes!=null)foreach(var swipe in troopSwipes)if(swipe!=null)swipe.CancelGesture();
        }
        void ToggleAttackInfo(int kind)
        {
            if(page!="game"||modal!=null||session.State==null||dragSource!=null||UiPointerGate.Active)return;
            if(attackInfo.Open&&infoKind==kind){attackInfo.Close();return;}
            InvalidateBattlePresses();CancelInteraction();infoKind=kind;
            float x=width-AttackRailReserve-attackInfoContent.rect.width;
            float y=Mathf.Clamp(infoRowY+(kind%attackRows)*(infoRowHeight+infoRowGap),94,Mathf.Max(94,height-362));
            attackInfo.SetReducedEffects(settings.ReducedEffects);attackInfo.Show(new Vector2(Mathf.Max(12,x),-y));
            RefreshAttackInfo();Canvas.ForceUpdateCanvases();
        }
        void RefreshAttackInfo()
        {
            if(!InfoBlocking||infoKind<0||session.State==null)return;
            var unit=units[infoKind];var missing=BigInteger.Max(0,unit.Price-session.State.Players[(int)session.LocalSlot].Gold);
            attackInfoTitle.text=unit.Name;attackInfoPortrait.sprite=GeneratedClassIcons.GetUnitEvolution(infoKind,session.State.Players[(int)session.LocalSlot].ResearchLevels[infoKind]);
            attackInfoPrice.text=unit.Price+" Gold"+(missing>0?" · fehlen "+GameNumbers.Compact(missing):"");
            string role=infoKind==0?"Günstiger Druck auf die Verteidigung.":infoKind==1?"Schnell und leicht – erreicht rasch die Basis.":infoKind==2?"Langsam und schwer gepanzert.":infoKind==3?"Vier kleine Angreifer in einem Paket.":infoKind==4?"Schneller Sturmkoloss · setzt Lücken unter Druck.":"Schwerer Belagerungskoloss · langsam, gepanzert und hoher Basisschaden.";
            var state=session.State;var player=state.Players[(int)session.LocalSlot];
            displayedResearchQuote=UnitResearchRules.Quote(content,player,infoKind);
            var stats=displayedResearchQuote.Current;var send=SendRules.Quote(content,state,session.LocalSlot,infoKind);
            string capacity=send.WaveId>0?"Ausmarsch W"+send.WaveId+": "+AdditionalForecastStartText(infoKind,false):"Keine weitere Welle";
            attackInfoBody.text=role+"\n"+GameNumbers.Compact(stats.MaxHp)+" LP · Tempo "+stats.SpeedPerSecond+" · Rüstung "+stats.Armor+
                "\n"+unit.Count+" pro Paket · Basis −"+unit.BaseDamage+" LP\n"+capacity+"\n"+"FIFO: "+GameNumbers.Compact(queueForecast.Packets)+" Pakete · "+QueueBoundGold(queueForecast)+" G gebunden";
            bool pending=commands.HasPending(CommandType.ResearchUnit,infoKind);
            var q=displayedResearchQuote;
            bool researchClosed=!content.MatchRules.Endless&&state.WaveId==content.WaveCount&&state.WavePhase==WavePhase.Clearing;
            researchDetails.text="FORSCHUNG S"+GameNumbers.Compact(q.Level)+" · neue Käufe\n"+
                (q.IsValid?"LP "+GameNumbers.Compact(q.Current.MaxHp)+" → "+GameNumbers.Compact(q.Next.MaxHp)+" · Tempo "+q.Current.SpeedPerSecond+" → "+q.Next.SpeedPerSecond:"Forschung nicht verfügbar");
            UiKit.ButtonText(researchButton,researchClosed?"Keine weiteren Truppenkäufe":pending?"Forschung wird bestätigt …":q.IsValid?"Erforschen · "+GameNumbers.Compact(q.Price)+" G":"Forschung nicht verfügbar");
            researchButton.interactable=attackInfo.Open&&state.Phase==MatchPhase.Playing&&player.Hp>0&&q.IsValid&&!researchClosed&&!pending&&player.Gold>=q.Price;
            UiPointerGate.RestrictNavigation(attackInfoContent);
        }
        void InvalidateBattlePresses()
        {if(screen!=null)foreach(var button in screen.GetComponentsInChildren<PointerSafeButton>(true))button.InvalidateHeldPointers();}
        void CloseAttackInfo(){InvalidateBattlePresses();if(attackInfo!=null)attackInfo.CloseImmediately();infoKind=-1;}

        PurchaseState BuildPurchaseState(int kind)
        {
            var state=session?.State;
            if(state==null||state.Phase!=MatchPhase.Playing||state.Players[(int)session.LocalSlot].Hp<=0||showEnemy||kind<0||kind>=towers.Length)return PurchaseState.Inactive;
            if((state.Players[(int)session.LocalSlot].AvailableTowerMask&(1<<kind))==0)return PurchaseState.Locked;
            return state.Players[(int)session.LocalSlot].Gold<towers[kind].Price?PurchaseState.Unaffordable:PurchaseState.Available;
        }
        static void SetPurchaseState(Button button,PurchaseState state,Color background)
        {
            var action=button.GetComponent<ActionButtonVisual>();if(action!=null){action.SetState(state);return;}
            var appearance=button.GetComponent<PurchaseAvailability>();
            if(appearance==null){appearance=button.gameObject.AddComponent<PurchaseAvailability>();appearance.Initialize(button);}
            appearance.Apply(state,background);
        }
    }
}
