using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    /// <summary>Independent title, value, progress and price labels on a sculpted action plate.</summary>
    public sealed class ActionButtonVisual : MonoBehaviour
    {
        Button button;
        ActionButtonGraphic artwork;
        Text title,value,caption,price;
        ActionSymbol symbol;
        int steps;
        bool progress;
        public PurchaseState State { get; private set; }
        public string ValueText=>value.text;
        public string PriceText=>price.text;
        public string CaptionText=>caption.text;
        public ActionButtonGraphic Artwork=>artwork;
        public void Initialize(Button owner,ActionButtonGraphic graphic,float w,float h)
        {
            button=owner;artwork=graphic;bool compact=h<75;
            title=UiKit.TextAt(graphic.transform,"",49,compact?7:9,w-59,22,compact?18:11,UiKit.Hex(0xF7EDD7));title.fontStyle=FontStyle.Bold;
            value=UiKit.TextAt(graphic.transform,"",compact?49:10,compact?25:42,w-(compact?59:20),compact?17:24,compact?11:12,UiKit.Hex(0xF7EDD7));value.fontStyle=compact?FontStyle.Normal:FontStyle.Bold;if(!compact)value.alignment=TextAnchor.MiddleCenter;
            caption=UiKit.TextAt(graphic.transform,"",49,31,w-59,14,10,UiKit.Hex(0xB6CED2),TextAnchor.MiddleCenter);
            price=UiKit.TextAt(graphic.transform,"",13,h-34,w-26,22,15,UiKit.Hex(0x142A3A),TextAnchor.MiddleCenter);price.fontStyle=FontStyle.Bold;
            caption.gameObject.SetActive(!compact);price.gameObject.SetActive(!compact);
        }
        public void SetDetails(ActionSymbol kind,string heading,string values,string cost,string detail,int completed=0,bool showProgress=false)
        {
            symbol=kind;steps=completed;progress=showProgress;title.text=heading;value.text=values;price.text=cost;caption.text=detail;
            if(artwork.rectTransform.rect.height>=75)
                UiKit.At(caption.rectTransform,49,31,artwork.rectTransform.rect.width-59,14);
            bool visible=kind!=ActionSymbol.Close;title.enabled=value.enabled=caption.enabled=price.enabled=visible;
            if(artwork.rectTransform.rect.height>=75){value.fontSize=values.Contains("\n")?10:12;value.resizeTextForBestFit=true;value.resizeTextMinSize=9;value.resizeTextMaxSize=value.fontSize;}
            artwork.SetStyle(symbol,State,steps,progress);
        }
        public void SetState(PurchaseState state)
        {
            State=state;button.interactable=state==PurchaseState.Available;artwork.SetStyle(symbol,state,steps,progress);
            title.color=value.color=state==PurchaseState.Available?UiKit.Hex(0xF7EDD7):UiKit.Hex(0xEBE9DF);
            caption.color=state==PurchaseState.Available?UiKit.Hex(0xB6CED2):UiKit.Hex(0xD4D7D8);
            price.color=state==PurchaseState.Available?UiKit.Hex(0x142A3A):UiKit.Hex(0x252D31);
        }
    }
}
