using System;
using System.Globalization;
using BigInteger=System.Numerics.BigInteger;
using DefenseDuell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        MatchRules menuRules=new MatchRules(10,180);
        Button homeRulesButton;
        static string FormatLabel(MatchFormat format)=>format==MatchFormat.TwoVsTwo?"2 gegen 2":format==MatchFormat.ThreeVsThree?"3 gegen 3":format==MatchFormat.TwoVsTwoVsTwo?"2 gegen 2 gegen 2":"1 gegen 1";
        string MapLabel(string id)
        {for(int i=0;i<MapCatalog.Count;i++){var map=MapCatalog.Create(i);if(map.Id==id)return map.Name;}return content.Map.Name;}
        string MenuRuleLabel()=> (menuRules.Endless?"∞ Endlos":menuRules.WaveCount+" Wellen")+" · "+menuRules.IncomeMultiplier+"× Einkommen · Regeln ändern";
        bool ConfigureMenuMatch(bool solo)
        {
            var rules=new MatchRules(menuRules.WaveCount,menuRules.StartGold,menuRules.Endless,menuRules.IncomeMultiplier,solo?MatchFormat.Duel:menuRules.Format,content.Map.Id);
            if(session.ConfigureNextMatch(rules))return true;
            notice=session.Message;dirty=true;return false;
        }
        void OpenMenuRules()
        {
            if(page!="home"||modal!=null||UiPointerGate.Active)return;
            float w=Mathf.Min(760,width-48);OpenModal("DEINE SPIELREGELN",w,560);
            int waves=menuRules.WaveCount,factor=menuRules.IncomeMultiplier;bool endlessMode=menuRules.Endless;MatchFormat format=menuRules.Format;
            UiKit.TextAt(modal,"Wellen · 1 bis 200 oder unbegrenzt",28,96,w-56,30,20,HudText);
            InputField exact=ConnectInput(modal,"Wellen",waves.ToString(),28,136,120,52);exact.name="Menu wave count";exact.keyboardType=TouchScreenKeyboardType.NumberPad;
            Slider slider=null;Button endlessButton=null;Text error=null;
            Action refresh=()=>
            {
                slider?.SetValueWithoutNotify(waves);if(!exact.isFocused)exact.SetTextWithoutNotify(waves.ToString(CultureInfo.InvariantCulture));
                exact.readOnly=endlessMode;if(slider!=null)slider.interactable=!endlessMode;
                if(endlessButton!=null){UiKit.ButtonText(endlessButton,endlessMode?"∞ ENDLOS ✓":"∞ Endlos");UiKit.ButtonColor(endlessButton,endlessMode?HudSelected:HudCard);}
            };
            slider=UiKit.SliderAt(modal,"Menu waves slider",166,140,w-370,44,1,200,value=>{waves=value;if(error!=null)error.text="";refresh();});
            endlessButton=BattleButton(modal,"",w-184,136,156,52,()=>{endlessMode=!endlessMode;refresh();},HudCard,17);endlessButton.name="Menu endless";
            exact.onValueChanged.AddListener(value=>{if(int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out int parsed)&&parsed>=1&&parsed<=200){waves=parsed;if(error!=null)error.text="";slider.SetValueWithoutNotify(waves);}else if(error!=null)error.text="Bitte 1 bis 200 Wellen eingeben.";});
            UiKit.TextAt(modal,"LAUFENDES EINKOMMEN · BASIS + MINEN + ANGRIFFE",28,208,w-56,28,13,HudGold);
            int[] factors={1,2,3,5,10};var income=new Button[5];float fw=(w-80)/5;
            for(int i=0;i<5;i++){int index=i;income[i]=BattleButton(modal,factors[i]+"×",28+i*(fw+6),244,fw,48,()=>{factor=factors[index];for(int j=0;j<5;j++)UiKit.ButtonColor(income[j],factor==factors[j]?HudSelected:HudCard);},factor==factors[i]?HudSelected:HudCard,21);income[i].name="Menu income "+factors[i];}
            UiKit.TextAt(modal,"FREUNDEFORMAT · SOLO BLEIBT GEGEN DEN BOT",28,312,w-56,28,13,HudGold);
            var formats=new Button[4];float fmt=(w-74)/4;
            for(int i=0;i<4;i++){int index=i;formats[i]=BattleButton(modal,FormatLabel((MatchFormat)i),28+i*(fmt+6),348,fmt,54,()=>{format=(MatchFormat)index;for(int j=0;j<4;j++)UiKit.ButtonColor(formats[j],(int)format==j?HudSelected:HudCard);},(int)format==i?HudSelected:HudCard,16);formats[i].name="Menu format "+i;}
            error=UiKit.TextAt(modal,"",28,416,w-56,42,16,HudGold);
            BattleButton(modal,"REGELN MERKEN",28,474,w-56,58,()=>
            {
                if(!int.TryParse(exact.text,NumberStyles.None,CultureInfo.InvariantCulture,out int parsed)||parsed<1||parsed>200){error.text="Bitte 1 bis 200 Wellen eingeben.";return;}
                menuRules=new MatchRules(parsed,menuRules.StartGold,endlessMode,factor,format,content.Map.Id);CloseModal();if(homeRulesButton!=null)UiKit.ButtonText(homeRulesButton,MenuRuleLabel());
            },HudAction,20).name="Menu apply rules";
            refresh();
        }
        void OpenMapPicker(bool lobby)
        {
            if(modal!=null||UiPointerGate.Active||lobby&&!lobbySettingsEditor.CanEdit)return;
            float w=Mathf.Min(900,width-40),cw=(w-64)/3;OpenModal("WÄHLE DEIN SCHLACHTFELD",w,498);
            for(int i=0;i<MapCatalog.Count;i++)
            {
                int index=i;var map=MapCatalog.Create(i);bool selected=lobby?lobbySettingsEditor.DraftMapId==map.Id:session.SelectedMapIndex==i;
                var b=BattleButton(modal,"",20+(i%3)*(cw+12),98+(i/3)*174,cw,160,()=>
                {
                    if(lobby)lobbySettingsEditor.EditRules(lobbySettingsEditor.DraftEndless,lobbySettingsEditor.DraftIncomeMultiplier,lobbySettingsEditor.DraftFormat,map.Id);
                    else session.SelectMap(index);
                    CloseModal();dirty=true;if(lobby)RefreshLobbyScreen();
                },selected?HudSelected:HudCard,16);b.name="Map choice "+i;
                var visual=UiKit.ButtonContent(b);var thumb=UiKit.Rect("Map route",visual);UiKit.At(thumb,10,10,cw-20,100);thumb.gameObject.AddComponent<MapThumbnailGraphic>().Initialize(map);
                UiKit.TextAt(visual,(selected?"✓ ":"")+map.Name,10,114,cw-20,36,17,HudText,TextAnchor.MiddleCenter);
            }
        }
    }
}
