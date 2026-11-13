using DefenseDuell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        MenuMotion homeMotion;
        Text homeArsenalLabel;
        bool homeArsenalOpen;

        void PlayfulHome()
        {
            // Stable controls sit on top of the same faceted navy/teal/gold palette as battle.
            float margin=24,gap=24,right=Mathf.Clamp(width*.32f,300,420),left=width-margin*2-gap-right;
            float rx=margin+left+gap,mapY=height-174,heroBottom=mapY-24;
            homeMotion=screen.gameObject.AddComponent<MenuMotion>();
            var emblem=UiKit.SpriteAt(screen,GeneratedClassIcons.Get(0,true),20,10,64,64,Color.white);
            UiKit.TextAt(screen,"DEFENSE DUELL",94,18,width-330,34,27,HudTheme.Text).fontStyle=FontStyle.Bold;
            UiKit.TextAt(screen,"BAUEN. AUFRÜSTEN. ANGREIFEN.",96,52,width-350,20,12,HudTheme.Gold);
            var settingsButton=HudTheme.ButtonAt(screen,"Ton & Anzeige",width-204,24,180,48,OpenSettings,HudTheme.Card,16);
            settingsButton.name="Home settings";

            var title=UiKit.TextAt(screen,"DEINE FESTUNG.\nDEIN DUELL.",margin,100,left-164,110,left<620?36:44,HudTheme.Text);
            title.fontStyle=FontStyle.Bold;
            var heroTower=UiKit.SpriteAt(screen,GeneratedClassIcons.GetTowerEvolution(0,4,2),margin+left-156,94,156,156,Color.white);
            heroTower.name="Home hero tower";
            homeRulesButton=BattleButton(screen,MenuRuleLabel(),margin,204,left,44,OpenMenuRules,HudCard,14);homeRulesButton.name="Home match rules";
            var frame=HudTheme.PanelAt(screen,"Home diorama frame",margin,252,left,heroBottom-252,HudTheme.Card,HudFrameStyle.Card);
            var previewRoot=UiKit.Rect("Duel preview",frame.transform);UiKit.At(previewRoot,8,8,left-16,heroBottom-268);
            var preview=previewRoot.gameObject.AddComponent<BoardView>();menuBoard=preview;
            preview.PreviewPixelCap=1280;
            preview.Initialize(content,TowerSprites,UnitSprites,false,null);
            preview.ReducedEffects=settings.ReducedEffects||settings.PowerSaving;preview.PowerSaving=settings.PowerSaving;
            var state=new MatchSimulation(content,1,1).State;
            state.Phase=MatchPhase.Playing;state.WavePhase=WavePhase.Active;
            for(int kind=0;kind<Mathf.Min(5,towers.Length);kind++)AddDemoTower(state,kind);
            for(int i=0;i<5;i++)state.Units.Add(new UnitState{Id=100+i,Defender=PlayerSlot.Zero,Kind=i%4,
                Hp=units[i%4].Hp,Distance=content.Map.Length*(i+1)/7,SlowPermille=1000});
            preview.Display(state,PlayerSlot.Zero);

            UiKit.TextAt(screen,"DEIN SOLO-DUELL",rx,102,right,24,14,HudTheme.Gold).fontStyle=FontStyle.Bold;
            CreateHomeExtras(rx,136,right);
            HomeMode(rx,196,right,112,"Home solo","JETZT SPIELEN","OFFLINE","Dein Duell gegen den Bot",0,()=>StartOffline(false),true);
            UiKit.TextAt(screen,"FORDERE FREUNDE HERAUS",rx,324,right,24,14,HudTheme.Muted).fontStyle=FontStyle.Bold;
            HomeMode(rx,358,right,74,"Home lan","WLAN-DUELL","WLAN","2–6 Freunde · ein Netzwerk",1,()=>ShowConnect("lan"),false);
            HomeMode(rx,448,right,74,"Home online","ONLINE-DUELL","ONLINE","Privater Raum · Beitrittscode",2,()=>ShowConnect("online"),false);

            UiKit.TextAt(screen,"WÄHLE DEIN SCHLACHTFELD",margin,mapY-2,width-48,22,13,HudTheme.Muted).fontStyle=FontStyle.Bold;
            var names=session.MapNames;float mapW=width-margin*2-204;
            HomeMap(margin,mapY+28,mapW,session.SelectedMapIndex,names[session.SelectedMapIndex]);
            BattleButton(screen,"Alle Karten",width-margin-192,mapY+28,192,76,()=>OpenMapPicker(false),HudCard,19).name="Home map picker";
            var tutorial=HudTheme.ButtonAt(screen,"So geht's · interaktives Tutorial",margin,height-64,280,44,()=>OpenHomeLoadout(()=>StartOffline(true)),HudTheme.Panel,15);
            tutorial.name="Home tutorial";
            var arsenal=HudTheme.ButtonAt(screen,"",margin+292,height-64,260,44,OpenHomeArsenal,HudTheme.Card,16);
            arsenal.name="Home arsenal";homeArsenalLabel=arsenal.GetComponentInChildren<Text>(true);
            UiKit.TextAt(screen,"KEINE WERBUNG. NUR DUELL.",width-290,height-62,266,42,12,HudTheme.Muted,TextAnchor.MiddleRight);
            homeMotion.Initialize(settings,preview,content,state,()=>modal!=null||!isActiveAndEnabled);
            homeMotion.AddEntrance(title.rectTransform,.02f,true);
            homeMotion.AddEntrance(frame.rectTransform,.08f,true);
            homeMotion.AddFloat(emblem.rectTransform,3.5f,.8f);
            homeMotion.AddFloat(heroTower.rectTransform,4,.65f);
            foreach(var button in screen.GetComponentsInChildren<Button>())homeMotion.AddEntrance((RectTransform)button.transform,.12f,false);
            RefreshHomeExtras();
        }

        void HomeMode(float x,float y,float w,float h,string objectName,string title,string tag,string subtitle,int kind,System.Action action,bool primary)
        {
            var button=HudTheme.ButtonAt(screen,"",x,y,w,h,()=>{Sound();OpenHomeLoadout(action);},primary?HudTheme.Action:HudTheme.Card,20);
            button.name=objectName;var visual=UiKit.ButtonContent(button);
            float portrait=primary?78:54;
            UiKit.SpriteAt(visual,GeneratedClassIcons.Get(kind,false),12,(h-portrait)*.5f,portrait,portrait,Color.white);
            float tx=primary?102:78;
            if(primary)UiKit.TextAt(visual,tag,tx,10,w-tx-20,20,12,HudTheme.Gold).fontStyle=FontStyle.Bold;
            UiKit.TextAt(visual,title,tx,primary?32:10,w-tx-18,32,primary&&w>=340?24:20,HudTheme.Text).fontStyle=FontStyle.Bold;
            UiKit.TextAt(visual,subtitle,tx,primary?72:42,w-tx-18,24,primary?15:13,HudTheme.Muted);
            if(primary)HudTheme.Icon(visual,HudSymbol.Chevron,w-36,12,20,HudTheme.Gold);
        }

        void HomeMap(float x,float y,float w,int index,string name)
        {
            bool chosen=session.SelectedMapIndex==index;
            var button=HudTheme.ButtonAt(screen,"",x,y,w,76,()=>{if(session.SelectMap(index)){Sound();dirty=true;}},chosen?HudTheme.Selected:HudTheme.Panel,18);
            button.name="Map card "+index;var visual=UiKit.ButtonContent(button);
            var thumbnail=UiKit.Rect("Route preview",visual);UiKit.At(thumbnail,10,12,82,52);
            thumbnail.gameObject.AddComponent<MapThumbnailGraphic>().Initialize(index==session.SelectedMapIndex?content.Map:MapCatalog.Create(index));
            UiKit.TextAt(visual,name,104,10,w-114,30,19,HudTheme.Text).fontStyle=FontStyle.Bold;
            UiKit.TextAt(visual,chosen?"✓ AUSGEWÄHLT":"KARTE WÄHLEN",104,42,w-114,22,11,chosen?HudTheme.Gold:HudTheme.Muted);
        }
    }
}
