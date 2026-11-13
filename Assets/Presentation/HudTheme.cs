using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    internal static class HudTheme
    {
        internal static readonly Color Panel=UiKit.Hex(0x142A3A),Card=UiKit.Hex(0x1C3D4A),Selected=UiKit.Hex(0x285C61),
            Action=UiKit.Hex(0x28606A),Text=UiKit.Hex(0xF7EDD7),Muted=UiKit.Hex(0xB6CED2),Gold=UiKit.Hex(0xF0C66A),Edge=UiKit.Hex(0x78938E);
        internal static HudFrameGraphic PanelAt(Transform parent,string name,float x,float y,float w,float h,Color color,HudFrameStyle style=HudFrameStyle.Panel)
        {
            var r=UiKit.Rect(name,parent);UiKit.At(r,x,y,w,h);
            var image=r.gameObject.AddComponent<HudFrameGraphic>();image.color=color;image.SetStyle(style,Edge);return image;
        }
        internal static Button ButtonAt(Transform parent,string value,float x,float y,float w,float h,Action click,Color background,int fontSize)
        {
            // Keep the hit rectangle, PointerSafeButton and press/reject behaviours exactly as UiKit.
            var hit=UiKit.PanelAt(parent,value,x,y,w,h,Color.clear);hit.raycastTarget=true;
            var visual=UiKit.Rect("Button visual",hit.transform);UiKit.Fill(visual);
            var image=visual.gameObject.AddComponent<HudFrameGraphic>();image.color=background;image.SetStyle(HudFrameStyle.Button,Edge);
            var button=hit.gameObject.AddComponent<PointerSafeButton>();button.targetGraphic=image;
            hit.gameObject.AddComponent<ButtonPressFeedback>().Initialize(visual,button);
            var colors=button.colors;colors.highlightedColor=new Color(1.12f,1.12f,1.12f);colors.pressedColor=new Color(.8f,.86f,.88f);colors.disabledColor=new Color(.48f,.52f,.56f);button.colors=colors;
            button.onClick.AddListener(()=>click?.Invoke());
            var label=UiKit.Label(visual,value,fontSize,Text,TextAnchor.MiddleCenter);UiKit.Fill(label.rectTransform,10,5,10,5);
            hit.gameObject.AddComponent<ButtonRejectFeedback>().Initialize(button);return button;
        }
        internal static HudIconGraphic Icon(Transform parent,HudSymbol symbol,float x,float y,float size,Color? color=null)
        {
            var r=UiKit.Rect(symbol+" icon",parent);UiKit.At(r,x,y,size,size);
            var icon=r.gameObject.AddComponent<HudIconGraphic>();icon.color=color??Text;icon.SetSymbol(symbol);return icon;
        }
        internal static void IconButton(Button button,HudSymbol symbol)
        {
            button.GetComponentInChildren<Text>(true).text="";
            var r=(RectTransform)UiKit.ButtonContent(button);
            Icon(r,symbol,(r.rect.width-24)*.5f,(r.rect.height-24)*.5f,24);
        }
    }
}
