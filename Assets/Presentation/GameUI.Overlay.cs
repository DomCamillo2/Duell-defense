using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        bool ThemedDialogs=>true;
        Color DialogTextColor=>ThemedDialogs?HudText:UiKit.Cream;
        Color DialogMutedColor=>ThemedDialogs?HudMuted:UiKit.Muted;
        Button DialogButton(Transform parent,string value,float x,float y,float w,float h,Action click,Color? background=null,int size=22)
        {
            if(!ThemedDialogs)return UiKit.ButtonAt(parent,value,x,y,w,h,click,background,size);
            bool primary=background==UiKit.Mint||background==UiKit.Hex(0xA7D585);
            return BattleButton(parent,value,x,y,w,h,click,primary?HudAction:HudCard,size);
        }
    }
}
