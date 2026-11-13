using System;
using DefenseDuell.Core;
using UnityEngine;
using UnityEngine.UI;
namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        int homeDraftTowerMask;
        Text homeLoadoutCounter;
        Button[] homeLoadoutChoices;
        Button homeLoadoutContinue;
        void OpenHomeLoadout(Action proceed)
        {
            if(modal!=null||UiPointerGate.Active)return;
            homeDraftTowerMask=TowerLoadoutRules.Normalize(session.LocalTowerMask,towers.Length);
            float w=Mathf.Min(760,width-48),h=Mathf.Min(526,height-48);
            OpenModal("DEINE TURMAUSWAHL",w,h);
            homeLoadoutCounter=UiKit.TextAt(modal,"",24,76,w-48,32,17,HudGold);
            UiKit.TextAt(modal,"Wähle 1–5 Turmarten. Die Mine zählt mit. Gebäude kannst du mehrfach bauen.",24,112,w-48,40,14,HudMuted);
            var viewport=UiKit.Rect("Loadout choices viewport",modal);UiKit.At(viewport,24,158,w-48,h-240);
            var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.viewport=viewport;scroll.movementType=ScrollRect.MovementType.Clamped;
            var choices=UiKit.Rect("Loadout choices",viewport);int columns=Mathf.Clamp(Mathf.FloorToInt((w-42)/100),2,4);float cw=(w-48-8*(columns-1))/columns;
            UiKit.At(choices,0,0,w-48,Mathf.CeilToInt((float)towers.Length/columns)*114);scroll.content=choices;
            homeLoadoutChoices=new Button[towers.Length];
            for(int i=0;i<towers.Length;i++)
            {
                int kind=i;var b=BattleButton(choices,"",(i%columns)*(cw+8),(i/columns)*114,cw,106,()=>ToggleHomeLoadout(kind),HudPanel,12);b.name="Home loadout "+i;homeLoadoutChoices[i]=b;
                var visual=UiKit.ButtonContent(b);UiKit.SpriteAt(visual,GeneratedClassIcons.Get(i,true),(cw-72)/2,2,72,72,Color.white);
                UiKit.TextAt(visual,towers[i].Name,3,76,cw-6,28,12,HudText,TextAnchor.MiddleCenter);
            }
            homeLoadoutContinue=BattleButton(modal,"Mit Auswahl weiter",24,h-66,w-48,44,()=>
            {
                if(!TowerLoadoutRules.IsValid(homeDraftTowerMask,towers.Length)||!session.SetTowerAvailability(homeDraftTowerMask))return;
                CloseModal();proceed();
            },HudAction,18);homeLoadoutContinue.name="Confirm home loadout";
            RefreshHomeLoadout();
        }
        static int SelectedTowerCount(int mask){int count=0;for(uint bits=(uint)mask;bits!=0;bits>>=1)count+=(int)(bits&1);return count;}
        void ToggleHomeLoadout(int kind)
        {
            int next=homeDraftTowerMask^(1<<kind);
            if(!TowerLoadoutRules.IsValid(next,towers.Length))
            {homeLoadoutCounter.text=SelectedTowerCount(next)>TowerLoadoutRules.MaxSelectedClasses?"5 / 5 · Wähle erst eine andere Turmart ab.":"Mindestens eine Turmart auswählen.";return;}
            homeDraftTowerMask=next;RefreshHomeLoadout();
        }
        void RefreshHomeLoadout()
        {
            homeLoadoutCounter.text=SelectedTowerCount(homeDraftTowerMask)+" / "+TowerLoadoutRules.MaxSelectedClasses+" TURMARTEN";
            for(int i=0;i<homeLoadoutChoices.Length;i++)UiKit.ButtonColor(homeLoadoutChoices[i],(homeDraftTowerMask&(1<<i))!=0?HudSelected:HudPanel);
            homeLoadoutContinue.interactable=TowerLoadoutRules.IsValid(homeDraftTowerMask,towers.Length);
        }
    }
}
