using DefenseDuell.Core;
using DefenseDuell.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        Text medalsLabel,resultReward;
        Button[] unlockButtons,difficultyButtons;
        Text[] unlockLabels;
        void CreateHomeExtras(float x,float y,float width)
        {
            medalsLabel=null;unlockButtons=null;unlockLabels=null;homeArsenalOpen=false;
            difficultyButtons=new Button[3];float w=(width-12)/3;
            string[] names={"Leicht","Normal","Schwer"};
            for(int i=0;i<3;i++)
            {
                int value=i;
                difficultyButtons[i]=HudTheme.ButtonAt(screen,names[i],x+i*(w+6),y,w,44,
                    ()=>{if(session.SetDifficulty((BotDifficulty)value)){Sound();dirty=true;}},HudTheme.Card,15);
                difficultyButtons[i].name="Solo difficulty "+names[i];
            }
        }
        void OpenHomeArsenal()
        {
            if(page!="home"||modal!=null||UiPointerGate.Active)return;
            float w=Mathf.Min(720,width-48);OpenModal("DEINE SAMMLUNG",w,454);homeArsenalOpen=true;
            medalsLabel=UiKit.TextAt(modal,"",28,96,w-56,58,16,HudTheme.Gold);
            unlockButtons=new Button[2];unlockLabels=new Text[2];float cardWidth=(w-72)/2;
            for(int i=0;i<2;i++)
            {
                int kind=i+5;if(kind>=towers.Length)continue;
                var button=HudTheme.ButtonAt(modal,"",24+i*(cardWidth+24),166,cardWidth,218,()=>UnlockTower(kind),HudTheme.Card,16);
                button.name="Unlock tower "+kind;unlockButtons[i]=button;var visual=UiKit.ButtonContent(button);
                UiKit.SpriteAt(visual,GeneratedClassIcons.Get(kind,true),cardWidth*.5f-44,10,88,88,Color.white);
                unlockLabels[i]=UiKit.TextAt(visual,"",12,108,cardWidth-24,98,16,HudTheme.Text,TextAnchor.MiddleCenter);
            }
            UiKit.TextAt(modal,"Alle Turmklassen sind im Duell verfügbar. Marken vervollständigen deine Sammlung.",28,396,w-56,48,15,HudTheme.Muted,TextAnchor.MiddleCenter);
            RefreshHomeExtras();
        }
        void UnlockTower(int kind)
        {
            // Only this specific modal may purchase; other dialogs keep the normal input barrier.
            if(page!="home"||!homeArsenalOpen||modal==null||UiPointerGate.Active)return;
            var progress=GetComponent<LocalProgress>();if(progress==null)return;
            bool accepted=progress.Unlock(kind);Sound(accepted?GameAudioCue.Unlock:GameAudioCue.Error);dirty=true;RefreshHomeExtras();
        }
        void RefreshHomeExtras()
        {
            var progress=GetComponent<LocalProgress>();var profile=progress!=null?progress.Profile:null;
            if(homeArsenalLabel!=null)homeArsenalLabel.text="SAMMLUNG"+(profile==null?"":" · "+profile.Medals+" MARKEN");
            if(medalsLabel!=null)
            {
                medalsLabel.text=profile==null?"Turmfreischaltungen nicht verfügbar":profile.Medals+" SIEGESMARKEN · +"+ProfileRules.WinMedals+" pro Sieg";
                if(progress!=null&&!string.IsNullOrEmpty(progress.Message))medalsLabel.text+="\n"+progress.Message;
            }
            for(int i=0;i<2;i++)
            {
                if(unlockButtons==null||unlockButtons[i]==null)continue;int kind=i+5;
                bool owned=profile!=null&&ProfileRules.Owns(profile,kind);int price=ProfileRules.Price(kind);
                unlockLabels[i].text=towers[kind].Name+(kind==5?"\nKettenblitze":"\nDurchschuss in Linie")+"\n"+
                    (owned?"✓ In deiner Sammlung":price+" Siegesmarken · "+(profile==null?"nicht verfügbar":profile.Medals>=price?"Sammeln":Mathf.Max(0,price-profile.Medals)+" fehlen"));
                unlockButtons[i].interactable=profile!=null&&!owned&&profile.Medals>=price;
                unlockLabels[i].color=owned?HudTheme.Gold:HudTheme.Text;
            }
            for(int i=0;i<3;i++)if(difficultyButtons!=null&&difficultyButtons[i]!=null)
            {
                bool selected=(int)session.Difficulty==i;
                UiKit.ButtonColor(difficultyButtons[i],selected?HudTheme.Selected:HudTheme.Card);
                var label=difficultyButtons[i].GetComponentInChildren<Text>(true);
                label.color=selected?HudTheme.Gold:HudTheme.Muted;
                label.fontStyle=selected?FontStyle.Bold:FontStyle.Normal;
                label.text=(selected?"✓ ":"")+(i==0?"Leicht":i==1?"Normal":"Schwer");
            }
        }
    }
}
