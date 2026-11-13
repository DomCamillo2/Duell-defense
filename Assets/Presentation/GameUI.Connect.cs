using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        // Uses the same portraits, faceted frames and fixed touch targets as Home.
        // Connection actions still go through the existing coordinator and validation.
        void BuildConnectScreen()
        {
            bool lan=connectionPage=="lan";
            float w=Mathf.Min(1120,width-64),x=(width-w)*.5f,gap=24,col=(w-gap)*.5f;
            var back=HudTheme.ButtonAt(screen,"Zurück",24,22,144,48,ReturnMenu,HudTheme.Panel,17);
            back.name="Zurück";
            UiKit.TextAt(screen,lan?"WLAN-DUELL":"ONLINE-DUELL",190,22,width-420,26,17,HudTheme.Gold).fontStyle=FontStyle.Bold;
            UiKit.TextAt(screen,content.Map.Name,190,49,width-420,22,13,HudTheme.Muted);
            var settingsButton=HudTheme.ButtonAt(screen,"Ton & Anzeige",width-204,22,180,48,OpenSettings,HudTheme.Card,16);
            settingsButton.name="Connect settings";

            var title=UiKit.TextAt(screen,lan?"DEINE FREUNDE.\nEIN WLAN-DUELL.":"DEIN RAUM.\nEUER ONLINE-DUELL.",x,94,w-170,112,width<1000?36:42,HudTheme.Text);
            title.name="Connect title";title.fontStyle=FontStyle.Bold;
            UiKit.TextAt(screen,lan?"Gleiches WLAN. Einer erstellt den Raum, alle anderen treten bei.":"Spielt von überall. Teilt euren privaten Beitrittscode.",x,214,w,28,18,HudTheme.Muted).name="Connect subtitle";
            var portrait=UiKit.SpriteAt(screen,GeneratedClassIcons.GetTowerEvolution(lan?0:2,4,2),x+w-148,98,144,136,Color.white);
            portrait.name="Connect hero tower";

            const float top=252,cardHeight=306;
            var host=HudTheme.PanelAt(screen,"Connect host card",x,top,col,cardHeight,HudTheme.Card,HudFrameStyle.Card);
            host.SetStyle(HudFrameStyle.Card,HudTheme.Gold);
            var join=HudTheme.PanelAt(screen,"Connect join card",x+col+gap,top,col,cardHeight,HudTheme.Panel,HudFrameStyle.Card);
            ConnectCardHeading(host.transform,col,"Raum erstellen","DU LÄDST EIN",0);
            ConnectCardHeading(join.transform,col,"Raum beitreten","DU SPIELST MIT",1);
            UiKit.TextAt(host.transform,lan?"Öffne einen Raum. Teile danach deine IP mit deinem Gegenüber.":"Öffne einen privaten Raum. Teile danach den Code mit deinem Gegenüber.",24,112,col-48,66,19,HudTheme.Text);
            UiKit.TextAt(host.transform,lan?"Ohne Anmeldung · im selben WLAN":"Privater Code · über das Internet",24,188,col-48,24,14,HudTheme.Muted);
            var create=HudTheme.ButtonAt(host.transform,"RAUM ERSTELLEN",24,234,col-48,52,()=>
            {CaptureForm();notice="";Sound();if(lan){if(ParsePort(out var port)&&ConfigureMenuMatch(false))session.StartLanHost(port);}else _=StartOnline(true);},HudTheme.Action,20);
            create.name="Connect create";

            UiKit.TextAt(join.transform,lan?"IP DES ANDEREN HANDYS":"BEITRITTSCODE",24,112,col-48,22,13,HudTheme.Gold).fontStyle=FontStyle.Bold;
            var address=ConnectInput(join.transform,lan?"z. B. 192.168.1.10":"Code eingeben",lan?formAddress:formCode,24,142,col-48,60);
            address.name="Connect address";
            address.onValueChanged.AddListener(v=>{if(lan)formAddress=v;else formCode=v;});
            var enter=HudTheme.ButtonAt(join.transform,"BEITRETEN",24,234,col-48,52,()=>
            {CaptureForm();notice="";Sound();if(lan){if(ParsePort(out var port))session.JoinLan(formAddress.Trim(),port);}else _=StartOnline(false);},HudTheme.Selected,20);
            enter.name="Connect join";
            noticeText=UiKit.TextAt(screen,notice,x,570,w,56,18,UiKit.Hex(0xFFD29A));noticeText.name="Connect notice";
            var help=HudTheme.ButtonAt(screen,"Verbindung & Hilfe",x,height-70,244,48,OpenConnectHelp,HudTheme.Panel,16);
            help.name="Connect help";
            UiKit.TextAt(screen,lan?"2–6 SPIELER · GLEICHES WLAN":"2–6 SPIELER · PRIVATER RAUM",x+260,height-68,w-260,44,13,HudTheme.Muted,TextAnchor.MiddleRight);
        }

        void ConnectCardHeading(Transform parent,float w,string title,string label,int kind)
        {
            UiKit.SpriteAt(parent,GeneratedClassIcons.Get(kind,false),20,24,60,60,Color.white);
            UiKit.TextAt(parent,label,96,25,w-120,20,12,HudTheme.Gold).fontStyle=FontStyle.Bold;
            var text=UiKit.TextAt(parent,title,96,49,w-120,36,w<430?24:28,HudTheme.Text);text.fontStyle=FontStyle.Bold;
        }

        InputField ConnectInput(Transform parent,string placeholder,string value,float x,float y,float w,float h)
        {
            var field=UiKit.InputAt(parent,placeholder,value,x,y,w,h);
            var image=field.GetComponent<Image>();image.color=UiKit.Hex(0x0B202E);
            var outline=field.gameObject.AddComponent<Outline>();outline.effectColor=HudTheme.Edge;outline.effectDistance=new Vector2(1,-1);
            field.textComponent.color=HudTheme.Text;field.textComponent.fontSize=22;
            ((Text)field.placeholder).color=HudTheme.Muted;((Text)field.placeholder).fontSize=19;
            field.caretColor=HudTheme.Gold;field.customCaretColor=true;field.selectionColor=new Color(.30f,.73f,.75f,.45f);
            var colors=field.colors;colors.selectedColor=new Color(1.2f,1.2f,1.2f);field.colors=colors;
            return field;
        }

        void OpenConnectHelp()
        {
            Sound();bool lan=connectionPage=="lan";
            OpenModal(lan?"WLAN · VERBINDUNG & HILFE":"ONLINE · VERBINDUNG & HILFE",760,440);
            UiKit.TextAt(modal,lan?"1  Beide Handys mit demselben WLAN verbinden.\n2  Auf einem Handy einen Raum erstellen.\n3  Die dort angezeigte IP auf dem anderen eingeben.":"1  Auf einem Gerät einen Raum erstellen.\n2  Den angezeigten Beitrittscode teilen.\n3  Auf dem anderen Gerät den Code eingeben.",30,104,700,110,21,HudTheme.Text);
            if(lan)
            {
                UiKit.TextAt(modal,"PORT",30,232,130,50,15,HudTheme.Gold);
                var port=ConnectInput(modal,"7777",formPort,164,226,180,56);port.name="Connect port";
                port.contentType=InputField.ContentType.IntegerNumber;port.characterLimit=5;port.onValueChanged.AddListener(v=>formPort=v);
                UiKit.TextAt(modal,"Auf beiden Geräten gleich.\nNormalerweise bleibt es bei 7777.",366,226,364,58,17,HudTheme.Muted);
                UiKit.TextAt(modal,"Keine Verbindung? Gast-WLAN kann Geräte voneinander trennen. Verwendet euer normales WLAN.",30,310,700,64,18,HudTheme.Muted);
            }
            else UiKit.TextAt(modal,"Online benötigt Internet und eine eingerichtete Spielverbindung. Falls der Raum nicht erstellt werden kann, bleibt WLAN als Alternative verfügbar.",30,240,700,116,19,HudTheme.Muted);
        }
    }
}
