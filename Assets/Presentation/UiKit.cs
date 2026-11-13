using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    internal static class UiKit
    {
        internal static readonly Color Ink = Hex(0x091B2B), Panel = Hex(0x122F40), PanelLight = Hex(0x244657),
            Cream = Hex(0xF2F4E9), Muted = Hex(0xA9C0C6), Mint = Hex(0x64D3AE), Orange = Hex(0xEFAD77), Gold = Hex(0xEBC777);
        internal static Font Font;
        internal static Color Hex(uint value) => new Color(((value >> 16) & 255) / 255f, ((value >> 8) & 255) / 255f, (value & 255) / 255f, 1);
        internal static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }
        internal static void Fill(RectTransform r, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(.5f, .5f);
            r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top);
        }
        internal static void At(RectTransform r, float x, float y, float width, float height)
        {
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(width, height);
        }
        internal static Image PanelAt(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var r = Rect(name, parent); At(r, x, y, width, height);
            var image = r.gameObject.AddComponent<RoundedImage>(); image.color = color; image.raycastTarget = false;
            return image;
        }
        internal static Text Label(Transform parent, string value, int size = 22, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var r = Rect("Text", parent); Fill(r);
            var t = r.gameObject.AddComponent<Text>(); t.font = Font; t.text = value; t.fontSize = size;
            t.color = color ?? Cream; t.alignment = align; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.supportRichText = false;
            return t;
        }
        internal static Text TextAt(Transform parent, string value, float x, float y, float width, float height,
            int size = 22, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var text = Label(parent, value, size, color, align); At(text.rectTransform, x, y, width, height); return text;
        }
        internal static Button ButtonAt(Transform parent, string value, float x, float y, float width, float height,
            Action click, Color? background = null, int size = 22)
        {
            var hit = PanelAt(parent, value, x, y, width, height, Color.clear); hit.raycastTarget = true;
            var visual=Rect("Button visual",hit.transform);Fill(visual);
            var image=visual.gameObject.AddComponent<RoundedImage>();image.color=background??PanelLight;image.raycastTarget=false;
            var b = hit.gameObject.AddComponent<PointerSafeButton>(); b.targetGraphic = image;
            hit.gameObject.AddComponent<ButtonPressFeedback>().Initialize(visual,b);
            if (image.color.a > .1f) { var shadow=image.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(.12f,.24f,.09f,.14f);shadow.effectDistance=new Vector2(0,-3);shadow.useGraphicAlpha=true; }
            var colors = b.colors; colors.highlightedColor = new Color(1.15f,1.15f,1.15f); colors.pressedColor = new Color(.8f,.85f,.85f);
            colors.disabledColor = new Color(.48f,.52f,.56f); b.colors = colors;
            b.onClick.AddListener(() => click?.Invoke());
            var text = Label(image.transform, value, size, (background == Mint || background == Gold) ? Ink : Cream, TextAnchor.MiddleCenter);
            Fill(text.rectTransform, 10, 5, 10, 5);
            hit.gameObject.AddComponent<ButtonRejectFeedback>().Initialize(b);
            return b;
        }
        internal static void ButtonText(Button button, string text)
        { if (button != null) button.GetComponentInChildren<Text>(true).text = text; }
        internal static Button ActionButtonAt(Transform parent,string name,float x,float y,float width,float height,Action click,ActionSymbol symbol)
        {
            var hit=PanelAt(parent,name,x,y,width,height,Color.clear);hit.raycastTarget=true;
            var visual=Rect("Button visual",hit.transform);Fill(visual);
            var artwork=visual.gameObject.AddComponent<ActionButtonGraphic>();artwork.raycastTarget=false;
            var button=hit.gameObject.AddComponent<PointerSafeButton>();button.targetGraphic=artwork;
            var colors=button.colors;colors.highlightedColor=new Color(1.08f,1.08f,1.08f);colors.pressedColor=new Color(.84f,.88f,.85f);colors.disabledColor=Color.white;button.colors=colors;
            button.onClick.AddListener(()=>click?.Invoke());
            hit.gameObject.AddComponent<ButtonPressFeedback>().Initialize(visual,button);
            var presentation=hit.gameObject.AddComponent<ActionButtonVisual>();presentation.Initialize(button,artwork,width,height);
            presentation.SetDetails(symbol,"","","","");presentation.SetState(PurchaseState.Available);
            hit.gameObject.AddComponent<ButtonRejectFeedback>().Initialize(button);
            return button;
        }
        internal static Transform ButtonContent(Button button)=>button.targetGraphic.transform;
        internal static void ButtonColor(Button button,Color color){if(button!=null)button.targetGraphic.color=color;}
        internal static InputField InputAt(Transform parent, string placeholder, string initial, float x, float y, float width, float height)
        {
            var image = PanelAt(parent, "Input " + placeholder, x, y, width, height, PanelLight); image.raycastTarget = true;
            var input = image.gameObject.AddComponent<InputField>();
            var text = Label(image.transform, initial, 24); Fill(text.rectTransform, 20, 5, 20, 5);
            var hint = Label(image.transform, placeholder, 22, Muted); Fill(hint.rectTransform, 20, 5, 20, 5);
            input.textComponent = text; input.placeholder = hint; input.characterLimit = 64; input.text = initial;
            input.lineType = InputField.LineType.SingleLine;
            input.selectionColor = new Color(.65f,.93f,.69f,.4f);
            return input;
        }
        internal static Slider SliderAt(Transform parent,string name,float x,float y,float w,float h,int minimum,int maximum,Action<int> change)
        {
            var root=Rect(name,parent);At(root,x,y,w,h);
            var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var track=PanelAt(root,"Track",0,h*.5f-3,w,6,HudTheme.Card);
            var fillArea=Rect("Fill area",root);Fill(fillArea,12,h*.5f-3,12,h*.5f-3);
            var fill=PanelAt(fillArea,"Fill",0,0,w,6,HudTheme.Gold);Fill(fill.rectTransform);
            var handleArea=Rect("Handle area",root);Fill(handleArea,12,0,12,0);
            var handle=PanelAt(handleArea,"Handle",0,0,28,h,HudTheme.Gold);handle.raycastTarget=false;handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.anchoredPosition=Vector2.zero;handle.rectTransform.sizeDelta=new Vector2(28,0);
            var slider=root.gameObject.AddComponent<Slider>();slider.minValue=minimum;slider.maxValue=maximum;slider.wholeNumbers=true;
            slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
            slider.onValueChanged.AddListener(v=>change?.Invoke(Mathf.RoundToInt(v)));return slider;
        }
        internal static Image SpriteAt(Transform parent, Sprite sprite, float x, float y, float width, float height, Color tint)
        {
            var r = Rect("Icon", parent); At(r,x,y,width,height);
            var image=r.gameObject.AddComponent<Image>(); image.sprite=sprite; image.preserveAspect=true; image.color=tint; image.raycastTarget=false;
            return image;
        }
    }

}
