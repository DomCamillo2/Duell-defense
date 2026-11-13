using System;
using System.Collections.Generic;
using BigInteger=System.Numerics.BigInteger;
using DefenseDuell.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    /// <summary>A real, separately lit 3D diorama projected into the existing touch UI. It never simulates gameplay.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        sealed class UnitVisual
        {
            public GameObject Root;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public Transform HealthRoot,HealthFill;
            public GameObject BossCrest,SuperBossCrest;
            public MaterialPropertyBlock Properties=new MaterialPropertyBlock();
            public Vector3 Target;
            public Quaternion Facing=Quaternion.identity;
            public int Id=-1,Distance,Kind;
            public BigInteger Hp;
            public float Ground,WalkPhase,MotionHeight,MotionMinY;
            public readonly RouteMotionSamples Motion=new RouteMotionSamples();
            public float FlashUntil;
            public bool Slow;
            public void Reset()
            {
                Id=-1;Hp=0;Distance=Kind=0;WalkPhase=MotionHeight=MotionMinY=0;Motion.Reset();FlashUntil=0;Slow=false;Target=Vector3.zero;Facing=Quaternion.identity;
                Root.transform.localPosition=Vector3.zero;Root.transform.localRotation=Quaternion.identity;Root.transform.localScale=Vector3.one;
                BossCrest.SetActive(false);SuperBossCrest.SetActive(false);Filter.sharedMesh=null;Properties.Clear();Renderer.SetPropertyBlock(null);HealthRoot.localRotation=Quaternion.identity;HealthRoot.localPosition=new Vector3(0,1,0);HealthRoot.gameObject.SetActive(false);HealthFill.localScale=Vector3.one;Root.SetActive(false);
            }
        }
        sealed class TowerVisual
        {
            public Transform Root,Visual,Head;
            public MeshFilter BaseFilter,HeadFilter,PathFilter;
            public MeshRenderer BaseRenderer,HeadRenderer,PathRenderer;
            public int Entity=-1,Kind=-1,Variant=-1,PreviousCooldown,LogicalSlot=-1;
            public BigInteger Level=-1,DamageLevel,RangeLevel;
            public float RecoilUntil;
            public float PulseBegan=-1;
            public bool UpgradePulse;
            public bool Preview;
        }
        sealed class ShotVisual
        {
            public Transform Root;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public MaterialPropertyBlock Properties=new MaterialPropertyBlock();
            public Vector3 From,To;
            public float Began,Duration,SplashRadius;
            public bool Burst,Active;
            public void Reset(){Active=false;Burst=false;Began=Duration=SplashRadius=0;From=To=Vector3.zero;Root.localPosition=Vector3.zero;Root.localScale=Vector3.one;Root.localRotation=Quaternion.identity;Properties.Clear();Renderer.SetPropertyBlock(null);Root.gameObject.SetActive(false);}
        }
        static readonly bool[] LayerLeases=new bool[7];
        static readonly int TintId=Shader.PropertyToID("_Tint"),FlashId=Shader.PropertyToID("_Flash"),ToyMotionId=Shader.PropertyToID("_ToyMotion");
        readonly Dictionary<int,UnitVisual> active=new Dictionary<int,UnitVisual>(80);
        readonly Stack<UnitVisual> pool=new Stack<UnitVisual>(80);
        readonly HashSet<int> seen=new HashSet<int>();
        readonly List<int> removed=new List<int>(80);
        GameContent content;
        MapDefinition map;
        TowerDefinition[] towerDefinitions;
        UpgradeDefinition[][] upgrades;
        UnitDefinition[] unitDefinitions;
        float[] unitGroundHeights;
        MapPoint[] slots;
        int[] mineSites;
        int towerCapacity;
        RectTransform frame,touchOverlay;
        RawImage image;
        GameObject world;
        Transform unitRoot,towerRoot,effectRoot,selection,range,splashPreview;
        Text splashPreviewLabel;
        MapPoint[] splashPath;
        Camera viewCamera;
        RenderTexture texture;
        DioramaResources resources;
        Mesh bossCrestMesh,superBossCrestMesh;
        ContactFxBatch contactFx;
        EnergyBeamBatch energyBeams;
        bool powerSaving,animateTroops;
        public bool PowerSaving
        {get=>powerSaving;set{if(powerSaving==value)return;powerSaving=value;contactFx?.SetOptions(ReducedEffects,powerSaving);renderDirty=true;}}
        public int ActiveEnergyBeamCount=>energyBeams==null?0:energyBeams.ActiveCount;
        public int ContactShadowCount=>contactFx==null?0:contactFx.ShadowCount;
        Button[] slotButtons;
        Text[] slotLabels;
        Vector2[] projectedCenters;
        float[] projectedSpacing;
        TowerVisual[] towerViews;
        TowerVisual[] pendingViews;
        Text[] pendingLabels;
        PresentationCommandContext[] pendingBuilds=new PresentationCommandContext[PlacementValidator.RenderCapacity];
        int pendingCount;
        readonly List<TowerState> frameTowers=new List<TowerState>(32);
        readonly Dictionary<int,TowerState> fixedTowers=new Dictionary<int,TowerState>();
        readonly SnapshotPlaybackClock playback=new SnapshotPlaybackClock();
        bool remotePlayback;
        const int AimCellSize=200;
        readonly Dictionary<long,List<UnitState>> aimCells=new Dictionary<long,List<UnitState>>();
        readonly Dictionary<int,MapPoint> aimPositions=new Dictionary<int,MapPoint>();
        public bool RemotePlayback
        {
            get=>remotePlayback;
            set{if(remotePlayback==value)return;remotePlayback=value;playback.Reset();foreach(var entry in active)entry.Value.Motion.Reset();}
        }
        Transform dragModel,dragHead,dragMarker,dragRange;
        MeshFilter dragBaseFilter,dragHeadFilter;
        MeshRenderer dragBaseRenderer,dragHeadRenderer,dragMarkerRenderer,dragRangeRenderer;
        MaterialPropertyBlock dragProperties;
        int dragKind=-1,dragTarget=-1;
        float dragSnapBegan;
        readonly ShotVisual[] shots=new ShotVisual[32];
        Action<int> onSlot;
        int layer=-1,shotCursor,selected=-1;
        bool initialized,interactive,largeView,renderDirty=true,firstDisplay=true;
        bool reducedEffects;
        double nextRender;
        ulong epoch;
        PlayerSlot defender;
        Vector2 previousSize;
        readonly Vector3[] pixelCorners=new Vector3[4];
        Rect overviewSafeRect=new Rect(0,0,1,1);
        Mesh backdropMesh,placementGuideMesh,placementMarkerMesh,splashPreviewMesh;
        GameObject placementGuide;
        int placementGuideSignature=int.MinValue;
        public Vector2Int RenderPixelSize=>texture==null?Vector2Int.zero:new Vector2Int(texture.width,texture.height);
        public int RenderSamples=>texture==null?0:texture.antiAliasing;
        // Home can opt into a sharper large preview without increasing opponent minimaps.
        public int PreviewPixelCap { get; set; } = 640;
        public Rect OverviewSafeRect=>overviewSafeRect;
        public int PlacementGuidePointCount { get; private set; }

        Vector3 cameraOffset;
        Vector3 overviewCamera;
        Vector2 panOffset;
        float overviewSize,zoomFactor=1;
        public const float MinimumZoom=1,MaximumZoom=3;
        const float GroundY=.18f;
        public event Action<MapPoint> GroundSelected;
        public event Action ViewChanged;
        public float ZoomFactor=>zoomFactor;
        public bool IsNavigable=>initialized&&largeView&&isActiveAndEnabled;
        public int PooledCount=>pool.Count;
        public int VisibleCount=>active.Count;
        public int SlotCount=>towerCapacity;
        public bool IsInteractive=>initialized&&interactive;
        public int FeedbackPlayedCount { get; private set; }
        public bool ReducedEffects
        {
            get=>reducedEffects;
            set { if(reducedEffects==value)return;reducedEffects=value;contactFx?.SetOptions(value,powerSaving);if(value)CancelFeedback();renderDirty=true; }
        }
        public void Initialize(GameContent data,Sprite[] towerSprites,Sprite[] unitSprites,bool allowInput,Action<int> select)
        {
            if(initialized)throw new InvalidOperationException("BoardView already initialized.");
            dragProperties=new MaterialPropertyBlock();
            content=data;map=data.Map;splashPath=map.Waypoints;towerDefinitions=data.Towers;unitDefinitions=data.Units;slots=map.BuildSlots;mineSites=map.MineSlots;towerCapacity=map.FreePlacement?PlacementValidator.RenderCapacity:slots.Length;onSlot=select;interactive=largeView=allowInput;
            upgrades=new UpgradeDefinition[towerDefinitions.Length][];for(int i=0;i<upgrades.Length;i++)upgrades[i]=towerDefinitions[i].Upgrades;
            frame=(RectTransform)transform;
            for(int i=0;i<LayerLeases.Length;i++)if(!LayerLeases[i]){LayerLeases[i]=true;layer=24+i;break;}
            if(layer<0)throw new InvalidOperationException("Too many concurrent board views.");
            resources=new DioramaResources(content);
            unitGroundHeights=new float[unitDefinitions.Length];
            for(int i=0;i<unitGroundHeights.Length;i++)unitGroundHeights[i]=GroundY+.034f-resources.Units[i].bounds.min.y;
            world=new GameObject("Defense Duell 3D island / "+map.Name){hideFlags=HideFlags.DontSave,layer=layer};world.transform.position=new Vector3((layer-24)*40,0,0);
            unitRoot=Root("Unit pool",world.transform);towerRoot=Root("Tower views",world.transform);effectRoot=Root("Bounded visual effects",world.transform);
            var backdropBuilder=new DioramaMeshBuilder();ScenicTerrain.BuildBackdrop(backdropBuilder,map);
            backdropMesh=backdropBuilder.Build("Continuous landscape behind HUD");resources.ApplyGraphicsBackdropSurface(backdropMesh);
            MeshObject("Surrounding landscape",world.transform,backdropMesh,Vector3.zero);
            MeshObject("Raised island",world.transform,resources.Terrain,Vector3.zero);
            placementGuide=MeshObject("Legal building ground",world.transform,null,Vector3.zero);placementGuide.SetActive(false);
            for(int i=0;i<slots.Length;i++)if(!map.FreePlacement||map.IsMineSlot(i))MeshObject("Build pad "+i,world.transform,map.IsMineSlot(i)?resources.MinePad:resources.Pad,DioramaResources.Point(slots[i]));
            var cameraObject=new GameObject("Diorama camera",typeof(Camera)){layer=layer};cameraObject.transform.SetParent(world.transform,false);
            viewCamera=cameraObject.GetComponent<Camera>();viewCamera.enabled=false;viewCamera.orthographic=true;viewCamera.clearFlags=CameraClearFlags.SolidColor;viewCamera.backgroundColor=resources.Sky;
            viewCamera.allowHDR=false;viewCamera.allowMSAA=true;viewCamera.nearClipPlane=.1f;viewCamera.farClipPlane=80;viewCamera.cullingMask=1<<layer;
            cameraOffset=new Vector3(1.8f,28,-28);
            viewCamera.transform.localPosition=resources.MapCenter+cameraOffset;viewCamera.transform.LookAt(world.transform.position+resources.MapCenter);
            var sunObject=new GameObject("Warm directional sunlight",typeof(Light)){layer=layer};sunObject.transform.SetParent(world.transform,false);sunObject.transform.localRotation=Quaternion.Euler(48,-34,0);
            var sun=sunObject.GetComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.1f;sun.color=new Color(1,.97f,.88f);sun.shadows=LightShadows.None;sun.cullingMask=1<<layer;sun.renderMode=LightRenderMode.ForcePixel;
            contactFx=new ContactFxBatch(world.transform,layer,Resources.Load<Shader>("GraphicsContactFx"),!largeView);contactFx.SetOptions(ReducedEffects,powerSaving);
            energyBeams=new EnergyBeamBatch(effectRoot,layer,!largeView);
            var imageRect=UiKit.Rect("Real 3D board render",transform);UiKit.Fill(imageRect);image=imageRect.gameObject.AddComponent<RawImage>();image.raycastTarget=true;image.color=Color.white;
            if(allowInput)gameObject.AddComponent<BoardPointerSurface>().Initialize(this);
            touchOverlay=UiKit.Rect("Projected touch targets",transform);UiKit.Fill(touchOverlay);
            int targets=towerCapacity+(map.FreePlacement?mineSites.Length:0);
            slotButtons=new Button[targets];slotLabels=new Text[targets];projectedCenters=new Vector2[targets];projectedSpacing=new float[targets];towerViews=new TowerVisual[towerCapacity];
            for(int i=0;i<towerCapacity;i++)
            {
                int index=i;CreateTouchButton(index,"Tower target "+i,()=>
                {
                    if(!interactive)return;var view=towerViews[index];
                    if(view.Entity>=0)onSlot?.Invoke(view.Entity);else if(!map.FreePlacement)GroundSelected?.Invoke(slots[index]);
                });
                towerViews[i]=CreateTowerView("Tower render slot "+i,towerRoot);
                slotButtons[i].gameObject.SetActive(!map.FreePlacement);
            }
            if(map.FreePlacement)for(int m=0;m<mineSites.Length;m++)
            {
                int site=mineSites[m],index=towerCapacity+m;
                CreateTouchButton(index,"Mine site "+site,()=>{if(interactive)GroundSelected?.Invoke(slots[site]);});slotLabels[index].text="G";
                ((PointerSafeButton)slotButtons[index]).PointerClickOverride=e=>SelectGroundAt(e.position,e.pressEventCamera);
            }
            pendingViews=new TowerVisual[pendingBuilds.Length];pendingLabels=new Text[pendingBuilds.Length];
            for(int i=0;i<pendingViews.Length;i++)
            {
                pendingViews[i]=CreateTowerView("Pending preview "+i,towerRoot);
                pendingLabels[i]=UiKit.TextAt(touchOverlay,"BAU WARTET",0,0,88,22,11,UiKit.Cream,TextAnchor.MiddleCenter);
                pendingLabels[i].rectTransform.pivot=new Vector2(.5f,.5f);pendingLabels[i].gameObject.SetActive(false);
            }
            selection=MeshObject("Selected pad rim",world.transform,resources.Selection,Vector3.zero).transform;selection.gameObject.SetActive(false);
            range=MeshObject("Authoritative tower range preview",world.transform,resources.Range,Vector3.zero).transform;range.gameObject.SetActive(false);
            var splashBuilder=new DioramaMeshBuilder();
            splashBuilder.Ring(Vector3.zero,1,.038f,DioramaResources.C(0x713A18),80);
            splashBuilder.Ring(Vector3.up*.001f,.992f,.023f,DioramaResources.C(0xFFBD57),80);
            splashPreviewMesh=splashBuilder.Build("Selected explosion radius geometry");
            splashPreview=MeshObject("Selected explosion radius preview",world.transform,splashPreviewMesh,Vector3.zero).transform;
            splashPreview.gameObject.SetActive(false);
            splashPreviewLabel=UiKit.TextAt(touchOverlay,"",0,0,184,26,12,UiKit.Hex(0xFFF0CC),TextAnchor.MiddleCenter);
            splashPreviewLabel.font=UiKit.Font??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            splashPreviewLabel.name="Explosion radius label";splashPreviewLabel.fontStyle=FontStyle.Bold;
            splashPreviewLabel.rectTransform.pivot=new Vector2(.5f,0);
            var labelShadow=splashPreviewLabel.gameObject.AddComponent<Shadow>();labelShadow.effectColor=new Color(.03f,.04f,.05f,.95f);labelShadow.effectDistance=new Vector2(1,-1);
            splashPreviewLabel.gameObject.SetActive(false);
            dragModel=Root("Drag model preview only",world.transform);var dragBase=MeshObject("Preview base",dragModel,null,Vector3.zero);var dragTop=MeshObject("Preview head",dragModel,null,new Vector3(0,.65f,0));
            dragBaseFilter=dragBase.GetComponent<MeshFilter>();dragHeadFilter=dragTop.GetComponent<MeshFilter>();dragBaseRenderer=dragBase.GetComponent<MeshRenderer>();dragHeadRenderer=dragTop.GetComponent<MeshRenderer>();dragHead=dragTop.transform;
            var footprint=new DioramaMeshBuilder();
            footprint.Ring(Vector3.zero,.75f,.028f,DioramaResources.C(0x203C35),64);
            footprint.Ring(Vector3.up*.001f,.69f,.065f,Color.white,64);
            placementMarkerMesh=footprint.Build("Clear construction footprint");
            dragMarker=MeshObject("Drag actual placement marker",world.transform,placementMarkerMesh,Vector3.zero).transform;dragMarkerRenderer=dragMarker.GetComponent<MeshRenderer>();
            dragRange=MeshObject("Drag range",world.transform,resources.Range,Vector3.zero).transform;dragRangeRenderer=dragRange.GetComponent<MeshRenderer>();ClearDragTarget();
            for(int i=0;i<content.MaxUnitsPerField;i++){var u=CreateUnit();u.Reset();pool.Push(u);}
            for(int i=0;i<shots.Length;i++)
            {
                var go=MeshObject("Pooled projectile / impact "+i,effectRoot,resources.Bullet,Vector3.zero);
                shots[i]=new ShotVisual{Root=go.transform,Filter=go.GetComponent<MeshFilter>(),Renderer=go.GetComponent<MeshRenderer>()};shots[i].Reset();
            }
            initialized=true;Resize(true);
        }
        Transform Root(string name,Transform parent){var go=new GameObject(name){layer=layer};go.transform.SetParent(parent,false);return go.transform;}
        GameObject MeshObject(string name,Transform parent,Mesh mesh,Vector3 position)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};go.transform.SetParent(parent,false);go.transform.localPosition=position;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=resources.Material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;return go;
        }
        void CreateTouchButton(int index,string name,Action action)
        {
            var button=UiKit.ButtonAt(touchOverlay,"",0,0,64,64,action,Color.clear,26);button.name=name;button.interactable=interactive;
            // One board pointer owner handles both terrain and entity taps, so starting a pan
            // on a building cannot become a separate click or steal the gesture.
            button.enabled=false;button.GetComponent<ButtonPressFeedback>().enabled=false;
            ((RectTransform)button.transform).pivot=new Vector2(.5f,.5f);slotButtons[index]=button;button.gameObject.AddComponent<SlotHitFilter>().Initialize(this,index);
            var label=button.GetComponentInChildren<Text>();label.color=DioramaResources.C(0x385D36);label.fontStyle=FontStyle.Bold;slotLabels[index]=label;
        }
        TowerVisual CreateTowerView(string name,Transform parent)
        {
            var root=Root(name,parent);var visual=Root("Confirmed building visual",root);
            var basis=MeshObject("Base",visual,null,Vector3.zero);var head=MeshObject("Distinct class head",visual,null,new Vector3(0,.65f,0));
            var path=MeshObject("Upgrade path module",head.transform,null,Vector3.zero);path.SetActive(false);root.gameObject.SetActive(false);
            return new TowerVisual{Root=root,Visual=visual,Head=head.transform,BaseFilter=basis.GetComponent<MeshFilter>(),HeadFilter=head.GetComponent<MeshFilter>(),BaseRenderer=basis.GetComponent<MeshRenderer>(),HeadRenderer=head.GetComponent<MeshRenderer>(),PathFilter=path.GetComponent<MeshFilter>(),PathRenderer=path.GetComponent<MeshRenderer>()};
        }
        void SetTowerMeshes(TowerVisual visual,int kind,BigInteger level,int variant,bool preview,BigInteger primary=default,BigInteger secondary=default)
        {
            visual.Level=level;
            var evolution=resources.GetTowerEvolution(kind,kind==4?level:primary,secondary);
            if(visual.Kind==kind&&visual.Variant==evolution.Key&&visual.Preview==preview)return;
            visual.BaseFilter.sharedMesh=evolution.Base;visual.HeadFilter.sharedMesh=evolution.Head;visual.PathFilter.sharedMesh=evolution.PathModule;
            visual.PathFilter.gameObject.SetActive(evolution.PathModule!=null);visual.Kind=kind;visual.Variant=evolution.Key;visual.Preview=preview;visual.Head.localRotation=Quaternion.identity;
            visual.Head.localPosition=DioramaResources.TowerHeadOffset(kind);
            var block=new MaterialPropertyBlock();block.SetColor(TintId,preview?new Color(.68f,.92f,1):Color.white);
            visual.BaseRenderer.SetPropertyBlock(block);visual.HeadRenderer.SetPropertyBlock(block);visual.PathRenderer.SetPropertyBlock(block);
        }
        float TowerGround(int kind)=>!map.FreePlacement&&towerDefinitions[kind].IsEconomy?.43f:GroundY;
        static float HealthFraction(BigInteger hp,BigInteger maximum)
        {return maximum<=0?0:(float)(int)BigInteger.Min(10000,BigInteger.Max(0,hp*10000/maximum))/10000f;}
        void EnsureTowerCapacity(int required)
        {
            if(required<=towerCapacity)return;
            int old=towerCapacity,next=Math.Max(required,checked(old*2)),mines=map.FreePlacement?mineSites.Length:0;
            Array.Resize(ref towerViews,next);Array.Resize(ref slotButtons,next+mines);Array.Resize(ref slotLabels,next+mines);
            Array.Resize(ref projectedCenters,next+mines);Array.Resize(ref projectedSpacing,next+mines);
            for(int m=mines-1;m>=0;m--)
            {
                slotButtons[next+m]=slotButtons[old+m];slotLabels[next+m]=slotLabels[old+m];
                projectedCenters[next+m]=projectedCenters[old+m];projectedSpacing[next+m]=projectedSpacing[old+m];
                slotButtons[next+m].GetComponent<SlotHitFilter>().Initialize(this,next+m);
            }
            towerCapacity=next;
            for(int i=old;i<next;i++)
            {
                int index=i;CreateTouchButton(index,"Tower target "+i,()=>{if(interactive&&towerViews[index].Entity>=0)onSlot?.Invoke(towerViews[index].Entity);});
                towerViews[i]=CreateTowerView("Tower render slot "+i,towerRoot);slotButtons[i].gameObject.SetActive(false);
            }
        }
        void EnsurePendingCapacity(int required)
        {
            if(required<=pendingBuilds.Length)return;
            int old=pendingBuilds.Length,next=Math.Max(required,checked(old*2));Array.Resize(ref pendingBuilds,next);
            Array.Resize(ref pendingViews,next);Array.Resize(ref pendingLabels,next);
            for(int i=old;i<next;i++)
            {
                pendingViews[i]=CreateTowerView("Pending preview "+i,towerRoot);
                pendingLabels[i]=UiKit.TextAt(touchOverlay,"BAU WARTET",0,0,88,22,11,UiKit.Cream,TextAnchor.MiddleCenter);
                pendingLabels[i].rectTransform.pivot=new Vector2(.5f,.5f);pendingLabels[i].gameObject.SetActive(false);
            }
        }
        Mesh Crest(bool superBoss)
        {
            var b=new DioramaMeshBuilder();var color=DioramaResources.C(superBoss?0xFA66D5u:0xF6C15Cu);
            b.Ring(Vector3.zero,.62f,superBoss?.10f:.065f,color,superBoss?12:8);
            int count=superBoss?6:3;
            for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count;b.Crystal(new Vector3(Mathf.Cos(a)*.5f,.14f,Mathf.Sin(a)*.5f),new Vector3(.13f,superBoss?.5f:.30f,.13f),color);}
            return b.Build(superBoss?"Superboss six-point crown":"Boss three-point crown");
        }
        UnitVisual CreateUnit()
        {
            var go=MeshObject("Pooled 3D troop",unitRoot,null,Vector3.zero);var health=Root("Camera-facing health",go.transform);health.localPosition=new Vector3(0,1,0);
            MeshObject("Health background",health,resources.HealthTrack,new Vector3(0,0,.025f));var fill=MeshObject("Health fill",health,resources.HealthBar,Vector3.zero);
            if(bossCrestMesh==null){bossCrestMesh=Crest(false);superBossCrestMesh=Crest(true);}
            var crown=MeshObject("Boss crown",go.transform,bossCrestMesh,Vector3.zero);crown.SetActive(false);
            var superCrown=MeshObject("Superboss crown",go.transform,superBossCrestMesh,Vector3.zero);superCrown.SetActive(false);
            return new UnitVisual{BossCrest=crown,SuperBossCrest=superCrown,Root=go,Filter=go.GetComponent<MeshFilter>(),Renderer=go.GetComponent<MeshRenderer>(),HealthRoot=health,HealthFill=fill.transform};
        }
        public void Display(MatchState state,PlayerSlot player,int selectedEntity=-1,int previewKind=-1)
        {
            if(!initialized||!isActiveAndEnabled||state==null)return;
            bool reset=epoch!=state.SessionId||defender!=player;
            if(reset){ResetVisualState();epoch=state.SessionId;defender=player;firstDisplay=true;}
            selected=selectedEntity;animateTroops=state.Phase==MatchPhase.Playing&&state.WavePhase!=WavePhase.Preparing;
            if(RemotePlayback && playback.Observe(state.Tick,content.TickRate,Time.unscaledTimeAsDouble))
                foreach(var entry in active)entry.Value.Motion.Reset();
            frameTowers.Clear();fixedTowers.Clear();
            foreach(var tower in state.Towers)if(tower.Owner==player){frameTowers.Add(tower);if(!map.FreePlacement)fixedTowers[tower.Slot]=tower;}
            if(map.FreePlacement)EnsureTowerCapacity(frameTowers.Count);
            IndexAimTargets(state,player);
            seen.Clear();for(int i=0;i<state.Units.Count;i++)if(state.Units[i].Defender==player&&state.Units[i].Hp>0)seen.Add(state.Units[i].Id);
            removed.Clear();foreach(var pair in active)if(!seen.Contains(pair.Key))removed.Add(pair.Key);
            for(int i=0;i<removed.Count;i++)
            {
                var old=active[removed[i]];if(!firstDisplay&&!ReducedEffects&&old.Distance<map.Length-40)BurstAt(old.Root.transform.localPosition+Vector3.up*.3f,DioramaResources.Stone);
                active.Remove(removed[i]);old.Reset();pool.Push(old);
            }
            for(int i=0;i<state.Units.Count;i++)
            {
                var unit=state.Units[i];if(unit.Defender!=player||unit.Hp<=0)continue;
                if(!active.TryGetValue(unit.Id,out var view))
                {
                    view=pool.Count>0?pool.Pop():CreateUnit();view.Id=unit.Id;view.Kind=unit.Kind;view.Hp=unit.Hp;
                    view.Filter.sharedMesh=resources.GetUnitEvolution(unit.Kind,unit.ResearchLevel);var motionBounds=view.Filter.sharedMesh.bounds;view.MotionHeight=motionBounds.size.y;view.MotionMinY=motionBounds.min.y;view.WalkPhase=unit.Id%31;view.Ground=GroundY+.034f+resources.UnitEvolutionGroundOffset(unit.Kind,unit.ResearchLevel);view.Root.transform.localPosition=DioramaResources.Point(map.PositionAt(unit.Distance),view.Ground);view.Root.SetActive(true);active.Add(unit.Id,view);
                }
                if(unit.Hp<view.Hp&&!firstDisplay&&!ReducedEffects)view.FlashUntil=Time.unscaledTime+.15f;
                view.Motion.Add(state.Tick/(double)content.TickRate,unit.Distance);view.Target=DioramaResources.Point(map.PositionAt(unit.Distance),view.Ground);view.Distance=unit.Distance;view.Hp=unit.Hp;view.Slow=unit.SlowPermille<1000;
                var forward=DioramaResources.Point(map.PositionAt(Math.Min(map.Length,unit.Distance+12)),view.Ground)-view.Target;if(forward.sqrMagnitude>.0001f)view.Facing=Quaternion.LookRotation(forward);
                bool boss=LateGameRules.IsBoss(content,unit);
                bool superBoss=boss&&content.GetWave(unit.WaveId).IsSuperBossWave;
                float bodyScale=superBoss?2.6f:boss?1.8f:1;
                view.BossCrest.SetActive(boss&&!superBoss);view.SuperBossCrest.SetActive(superBoss);
                var crestHeight=new Vector3(0,resources.UnitEvolutionHeight(unit.Kind,unit.ResearchLevel)+.04f,0);
                view.BossCrest.transform.localPosition=crestHeight;view.SuperBossCrest.transform.localPosition=crestHeight;
                view.Root.transform.localScale=Vector3.one*bodyScale;
                view.Ground=GroundY+.034f+resources.UnitEvolutionGroundOffset(unit.Kind,unit.ResearchLevel)*bodyScale;
                BigInteger maxHp=UnitResearchRules.Resolve(content,unit.Kind,unit.ResearchLevel,unit.Origin,unit.WaveId,unit.NeutralRecipeIndex).MaxHp;
                view.HealthRoot.gameObject.SetActive(boss||unit.Hp<maxHp);view.HealthFill.localScale=new Vector3(HealthFraction(unit.Hp,maxHp),1,1);
                view.HealthRoot.localPosition=new Vector3(0,resources.UnitEvolutionHeight(unit.Kind,unit.ResearchLevel)+.15f,0);
            }
            TowerState selectedTower=null;
            for(int slot=0;slot<towerCapacity;slot++)
            {
                TowerState tower=null;if(map.FreePlacement){if(slot<frameTowers.Count)tower=frameTowers[slot];}else fixedTowers.TryGetValue(slot,out tower);
                var visual=towerViews[slot];bool pending=tower!=null&&HasPendingAt(tower.Position,tower.Kind,tower.SiteId);
                visual.Root.gameObject.SetActive(tower!=null&&!pending);
                bool buttonActive=tower!=null&&!pending||!map.FreePlacement;slotButtons[slot].gameObject.SetActive(buttonActive);
                slotLabels[slot].text=tower==null&&!map.FreePlacement?(map.IsMineSlot(slot)?"G":"+"):"";
                if(tower==null){visual.Entity=-1;visual.Kind=-1;visual.PreviousCooldown=0;visual.RecoilUntil=0;visual.PulseBegan=-1;visual.Visual.localScale=Vector3.one;continue;}
                if(visual.Entity!=tower.Id)
                {
                    // A sale and rebuild can reuse this render slot between snapshots.
                    visual.PreviousCooldown=0;visual.RecoilUntil=0;visual.PulseBegan=-1;visual.UpgradePulse=false;
                    visual.Visual.localScale=Vector3.one;visual.Head.localScale=Vector3.one;visual.Head.localRotation=Quaternion.identity;
                }
                visual.Root.localPosition=DioramaResources.Point(tower.Position,TowerGround(tower.Kind));SetTowerMeshes(visual,tower.Kind,tower.Level,TowerUpgradeRules.VisualVariant(tower),false,tower.DamageLevel,tower.RangeLevel);visual.LogicalSlot=tower.Slot;visual.DamageLevel=tower.DamageLevel;visual.RangeLevel=tower.RangeLevel;
                ProjectTouch(slot,tower.Position,.5f);slotButtons[slot].name="Tower entity "+tower.Id;
                if(tower.Id==selectedEntity)selectedTower=tower;
                var target=TargetFor(state,tower);
                if(target!=null&&!pending)
                {
                    Vector3 point=DioramaResources.Point(map.PositionAt(target.Distance),.6f),aim=point-visual.Root.localPosition;aim.y=0;
                    if(aim.sqrMagnitude>.001f)visual.Head.localRotation=Quaternion.LookRotation(aim);
                    if(visual.Entity==tower.Id&&tower.CooldownTicks>visual.PreviousCooldown&&!firstDisplay)
                    {
                        var origin=visual.Root.localPosition+Vector3.up*(tower.Kind==7?1.04f:.95f)+aim.normalized*.55f;
                        var mechanic=towerDefinitions[tower.Kind].Mechanic;
                        if(mechanic==WeaponMechanic.Chain||mechanic==WeaponMechanic.Beam)
                            energyBeams.Trigger(origin,point,mechanic==WeaponMechanic.Chain,Time.unscaledTimeAsDouble,ReducedEffects);
                        else if(!ReducedEffects)
                        {visual.RecoilUntil=Time.unscaledTime+.12f;Shot(origin,point,ClassColor(tower.Kind),TowerUpgradeRules.Stats(content,tower).SplashRadius/50f);}
                    }
                }
                visual.Entity=tower.Id;visual.PreviousCooldown=tower.CooldownTicks;
            }
            if(map.FreePlacement)for(int m=0;m<mineSites.Length;m++)
            {
                int site=mineSites[m];bool occupied=false;for(int t=0;t<state.Towers.Count;t++)if(state.Towers[t].Owner==player&&state.Towers[t].SiteId==site){occupied=true;break;}
                slotButtons[towerCapacity+m].gameObject.SetActive(!occupied&&!HasPendingAt(slots[site],4,site));
            }
            for(int i=0;i<pendingViews.Length;i++)
            {
                bool visible=i<pendingCount&&player==pendingBuilds[i].Player;var v=pendingViews[i];v.Root.gameObject.SetActive(visible);pendingLabels[i].gameObject.SetActive(visible);
                if(!visible)continue;var c=pendingBuilds[i];v.Root.localPosition=DioramaResources.Point(c.Position,TowerGround(c.Kind));SetTowerMeshes(v,c.Kind,0,0,true);v.Visual.localScale=Vector3.one;
                ProjectRect(pendingLabels[i].rectTransform,c.Position,1.75f);
            }
            selection.gameObject.SetActive(selectedTower!=null);range.gameObject.SetActive(false);HideSplashPreview();
            if(selectedTower!=null)
            {
                selection.localPosition=DioramaResources.Point(selectedTower.Position,TowerGround(selectedTower.Kind)+.006f);selection.localScale=Vector3.one*(PlacementValidator.BuildingRadius/50f/.68f);
                if(!towerDefinitions[selectedTower.Kind].IsEconomy)
                {
                    int radius=TowerUpgradeRules.Stats(content,selectedTower).Range;
                    range.localPosition=DioramaResources.Point(selectedTower.Position,GroundY+.004f);range.localScale=Vector3.one*(radius/50f);range.gameObject.SetActive(true);
                }
            }
            if(largeView&&selectedTower!=null)ShowSplashPreview(state,selectedTower);
            firstDisplay=false;renderDirty=true;
        }
        static long AimKey(int x,int y)=>((long)x<<32)|(uint)y;
        void IndexAimTargets(MatchState state,PlayerSlot player)
        {
            foreach(var cell in aimCells.Values)cell.Clear();aimPositions.Clear();
            foreach(var unit in state.Units)
            {
                if(unit.Defender!=player||unit.Hp<=0)continue;
                var point=map.PositionAt(unit.Distance);aimPositions[unit.Id]=point;long key=AimKey(point.X/AimCellSize,point.Y/AimCellSize);
                if(!aimCells.TryGetValue(key,out var cell)){cell=new List<UnitState>();aimCells.Add(key,cell);}cell.Add(unit);
            }
        }
        UnitState TargetFor(MatchState state,TowerState tower)
        {
            if(towerDefinitions[tower.Kind].IsEconomy)return null;
            int radius=TowerUpgradeRules.Stats(content,tower).Range;long max=(long)radius*radius;UnitState best=null;
            int left=Math.Max(0,(tower.Position.X-radius)/AimCellSize),right=Math.Min(map.Width/AimCellSize,(tower.Position.X+radius)/AimCellSize);
            int bottom=Math.Max(0,(tower.Position.Y-radius)/AimCellSize),top=Math.Min(map.Height/AimCellSize,(tower.Position.Y+radius)/AimCellSize);
            for(int y=bottom;y<=top;y++)for(int x=left;x<=right;x++)
            {
                if(!aimCells.TryGetValue(AimKey(x,y),out var cell))continue;
                foreach(var u in cell)
                {
                    var p=aimPositions[u.Id];long dx=p.X-tower.Position.X,dy=p.Y-tower.Position.Y;if(dx*dx+dy*dy>max)continue;
                    bool stronger=best!=null&&tower.Priority==TargetPriority.Strongest&&u.Hp>best.Hp;
                    bool tied=best!=null&&(tower.Priority!=TargetPriority.Strongest||u.Hp==best.Hp);
                    if(best==null||stronger||(tied&&(u.Distance>best.Distance||(u.Distance==best.Distance&&u.Id<best.Id))))best=u;
                }
            }
            return best;
        }
        void Shot(Vector3 from,Vector3 to,Color color,float splashRadius=0)
        {
            var s=shots[shotCursor++%shots.Length];s.Reset();s.Filter.sharedMesh=resources.Bullet;s.From=from;s.To=to;s.SplashRadius=splashRadius;s.Began=Time.unscaledTime;s.Duration=.16f;s.Active=true;s.Properties.SetColor(TintId,color);s.Renderer.SetPropertyBlock(s.Properties);s.Root.gameObject.SetActive(true);
        }
        void HideSplashPreview()
        {
            if(splashPreview!=null)splashPreview.gameObject.SetActive(false);
            if(splashPreviewLabel!=null)splashPreviewLabel.gameObject.SetActive(false);
        }
        void ShowSplashPreview(MatchState state,TowerState tower)
        {
            var stats=TowerUpgradeRules.Stats(content,tower);if(stats.SplashRadius<=0)return;
            var target=TargetFor(state,tower);MapPoint point;
            if(target!=null)point=map.PositionAt(target.Distance);
            else
            {
                // A labelled sample on the nearest reachable path is useful before a wave.
                // It is not a claimed hit or an expansion of the tower's targeting range.
                float nearest=float.PositiveInfinity;point=default;
                var origin=new Vector2(tower.Position.X,tower.Position.Y);
                for(int i=1;i<splashPath.Length;i++)
                {
                    var a=new Vector2(splashPath[i-1].X,splashPath[i-1].Y);var b=new Vector2(splashPath[i].X,splashPath[i].Y);var segment=b-a;
                    float t=segment.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(origin-a,segment)/segment.sqrMagnitude):0;
                    var p=a+segment*t;float distance=(p-origin).sqrMagnitude;
                    if(distance<nearest){nearest=distance;point=new MapPoint(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y));}
                }
                if(nearest>(float)stats.Range*stats.Range)return;
            }
            splashPreview.localPosition=DioramaResources.Point(point,ContactFxBatch.RouteGroundPlane+.012f);
            splashPreview.localScale=Vector3.one*(stats.SplashRadius/50f);splashPreview.gameObject.SetActive(true);
            splashPreviewLabel.text=(target!=null?"Explosionsradius · ":"Radius-Vorschau · ")+stats.SplashRadius;
            ProjectRect(splashPreviewLabel.rectTransform,point,1.4f);splashPreviewLabel.gameObject.SetActive(true);
        }
        void BurstAt(Vector3 position,Color color)
        {
            if(!ReducedEffects)contactFx?.Trigger(ContactFxKind.Impact,position,color,Time.unscaledTimeAsDouble);
        }
        static Color ClassColor(int kind)=>kind==0?DioramaResources.Gold:kind==1?DioramaResources.Orange:kind==2?DioramaResources.Blue:kind==3?DioramaResources.Purple:kind==5?DioramaResources.Blue:kind==6?DioramaResources.Orange:kind==7?DioramaResources.C(0x5EF4DE):DioramaResources.Gold;
        public static string ClassGlyph(int kind)=>kind==0?"I":kind==1?"●":kind==2?"✦":kind==3?"◆":kind==5?"ϟ":kind==6?"➤":"G";
        public void SetInteractive(bool allow){interactive=allow;for(int i=0;i<slotButtons.Length;i++)slotButtons[i].interactable=allow;}
        public void SetPendingBuilds(IReadOnlyList<PresentationCommandContext> values)
        {
            EnsurePendingCapacity(values?.Count??0);pendingCount=values?.Count??0;
            for(int i=0;i<pendingBuilds.Length;i++)pendingBuilds[i]=i<pendingCount?values[i]:null;
            // A controller can be disabled without disabling this separate board. Clear immediately,
            // rather than waiting for the controller's next authoritative Display call.
            if(pendingViews!=null)for(int i=pendingCount;i<pendingViews.Length;i++)pendingViews[i].Root.gameObject.SetActive(false);
            if(pendingLabels!=null)for(int i=pendingCount;i<pendingLabels.Length;i++)pendingLabels[i].gameObject.SetActive(false);
            renderDirty=true;
        }
        bool HasPendingAt(MapPoint point,int kind,int site)
        {for(int i=0;i<pendingCount;i++){var p=pendingBuilds[i];if(p.Kind==kind&&p.SiteId==site&&p.Position.X==point.X&&p.Position.Y==point.Y)return true;}return false;}
        public bool TryScreenToMapPoint(Vector2 screenPoint,Camera eventCamera,out MapPoint point)
        {
            point=default;if(!initialized||image==null||!RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform,screenPoint,eventCamera,out var local))return false;
            Rect rect=image.rectTransform.rect;if(!rect.Contains(local))return false;Rect uv=image.uvRect;
            var viewport=new Vector3(uv.x+(local.x-rect.xMin)/rect.width*uv.width,uv.y+(local.y-rect.yMin)/rect.height*uv.height,0);
            Ray ray=viewCamera.ViewportPointToRay(viewport);var plane=new Plane(world.transform.up,world.transform.TransformPoint(new Vector3(0,GroundY,0)));
            if(!plane.Raycast(ray,out float distance)||distance<0)return false;
            Vector3 p=world.transform.InverseTransformPoint(ray.GetPoint(distance));point=new MapPoint(Mathf.RoundToInt(p.x*50+500),Mathf.RoundToInt(300-p.z*50));
            return point.X>=0&&point.Y>=0&&point.X<=map.Width&&point.Y<=map.Height;
        }
        public Vector2 MapPointToScreen(MapPoint point,Camera eventCamera=null)
        {
            if(!initialized)return Vector2.zero;Vector3 view=viewCamera.WorldToViewportPoint(world.transform.TransformPoint(DioramaResources.Point(point,GroundY)));
            Rect r=image.rectTransform.rect,uv=image.uvRect;var local=new Vector3(r.xMin+(view.x-uv.x)/uv.width*r.width,r.yMin+(view.y-uv.y)/uv.height*r.height,0);
            return RectTransformUtility.WorldToScreenPoint(eventCamera,image.rectTransform.TransformPoint(local));
        }
        public void SelectGroundAt(Vector2 screenPoint,Camera eventCamera)
        {
            if(!interactive)return;
            for(int i=0;i<towerViews.Length;i++)if(towerViews[i].Entity>=0&&slotButtons[i].gameObject.activeInHierarchy&&IsClosestTouchTarget(i,screenPoint,eventCamera)){onSlot?.Invoke(towerViews[i].Entity);return;}
            if(TryScreenToMapPoint(screenPoint,eventCamera,out var point))GroundSelected?.Invoke(point);
        }
        public int FindSiteTarget(Vector2 screenPoint,Camera eventCamera,bool economy,int previous=-1)
        {
            if(!initialized||!interactive||map.FreePlacement||!TryScreenToMapPoint(screenPoint,eventCamera,out _))return -1;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(touchOverlay,screenPoint,eventCamera,out var point))return -1;
            int best=-1;float distance=float.PositiveInfinity,old=float.PositiveInfinity;
            for(int i=0;i<slots.Length;i++)
            {
                if(!map.CanBuildAt(i,economy))continue;Vector3 p=viewCamera.WorldToViewportPoint(world.transform.TransformPoint(DioramaResources.Point(slots[i],GroundY)));
                Vector2 center=new Vector2((p.x-.5f)*touchOverlay.rect.width,(p.y-.5f)*touchOverlay.rect.height);float d=Vector2.Distance(point,center);
                if(i==previous)old=d;if(d<=38&&d<distance){best=i;distance=d;}
            }
            if(previous>=0&&old<=46&&(best<0||best==previous||distance+5>=old))return previous;
            return best;
        }
        public void ShowDragTarget(int kind,MapPoint point,bool valid,int siteId=-1)
        {
            if(!initialized||kind<0||kind>=towerDefinitions.Length){ClearDragTarget();return;}
            if(dragTarget!=siteId){dragSnapBegan=Time.unscaledTime;dragTarget=siteId;}
            if(dragKind!=kind){dragKind=kind;dragBaseFilter.sharedMesh=resources.TowerBases[kind,0];dragHeadFilter.sharedMesh=resources.TowerHeads[kind,0];dragHead.localPosition=DioramaResources.TowerHeadOffset(kind);}
            dragModel.localPosition=DioramaResources.Point(point,TowerGround(kind));dragModel.gameObject.SetActive(true);
            dragMarker.localPosition=DioramaResources.Point(point,TowerGround(kind)+.006f);dragMarker.localScale=Vector3.one*(PlacementValidator.BuildingRadius/50f/.68f);dragMarker.gameObject.SetActive(true);
            dragProperties.Clear();dragProperties.SetColor(TintId,valid?new Color(.68f,1,.68f):new Color(1,.45f,.4f));dragBaseRenderer.SetPropertyBlock(dragProperties);dragHeadRenderer.SetPropertyBlock(dragProperties);dragMarkerRenderer.SetPropertyBlock(dragProperties);dragRangeRenderer.SetPropertyBlock(dragProperties);
            dragRange.gameObject.SetActive(!towerDefinitions[kind].IsEconomy);dragRange.localPosition=DioramaResources.Point(point,GroundY+.004f);dragRange.localScale=Vector3.one*(towerDefinitions[kind].Range/50f);
            selection.gameObject.SetActive(false);range.gameObject.SetActive(false);renderDirty=true;
        }
        public void ShowPlacementGuide(MatchState state,PlayerSlot owner,int kind)
        {
            if(!initialized||!interactive||!map.FreePlacement||state==null||kind<0||kind>=towerDefinitions.Length)
            {HidePlacementGuide();return;}
            int signature=unchecked((int)state.SessionId*397+kind*37+(int)owner);
            foreach(var tower in state.Towers)if(tower.Owner==owner)
                signature=unchecked(signature*31+tower.Position.X*397+tower.Position.Y);
            if(signature!=placementGuideSignature||placementGuideMesh==null)
            {
                placementGuideSignature=signature;var b=new DioramaMeshBuilder();int count=0;
                // Discrete ground hints are advisory, never placement targets or snapping cells.
                for(int y=35;y<map.Height-25;y+=40)for(int x=35;x<map.Width-25;x+=40)
                {
                    var point=new MapPoint(x,y);
                    if(PlacementValidator.ValidatePosition(content,state,owner,kind,-1,point)!=ErrorCode.None)continue;
                    var p=DioramaResources.Point(point,GroundY+.047f);
                    b.Ring(p,.064f,.024f,DioramaResources.C(0xD7FFC0),8);count++;
                }
                if(placementGuideMesh!=null)Destroy(placementGuideMesh);
                placementGuideMesh=b.Build("Authoritative legal ground hints");
                placementGuide.GetComponent<MeshFilter>().sharedMesh=placementGuideMesh;PlacementGuidePointCount=count;
            }
            placementGuide.SetActive(true);renderDirty=true;
        }
        public void HidePlacementGuide(){if(placementGuide!=null)placementGuide.SetActive(false);renderDirty=true;}
        public void ClearDragTarget(){dragTarget=dragKind=-1;if(dragModel!=null){dragModel.gameObject.SetActive(false);dragModel.localScale=Vector3.one;}if(dragMarker!=null)dragMarker.gameObject.SetActive(false);if(dragRange!=null)dragRange.gameObject.SetActive(false);}
        public bool PlayConfirmedFeedback(int entity,int slot,int kind,BigInteger level,bool upgrade,BigInteger? damageLevel=null,BigInteger? rangeLevel=null)
        {
            if(!initialized||ReducedEffects||!isActiveAndEnabled||slot<0)return false;
            TowerVisual v=null;foreach(var candidate in towerViews)if(candidate.Entity==entity&&candidate.LogicalSlot==slot){v=candidate;break;}
            if(v==null||v.Preview||v.Kind!=kind||v.Level!=level||!v.Root.gameObject.activeSelf)return false;
            if(damageLevel.HasValue&&damageLevel.Value>=0&&(v.DamageLevel!=damageLevel.Value||v.RangeLevel!=(rangeLevel??-1)))return false;
            v.PulseBegan=Time.unscaledTime;v.UpgradePulse=upgrade;FeedbackPlayedCount++;
            var effectPosition=v.Root.localPosition;effectPosition.y=TowerGround(kind)+.007f;
            contactFx?.Trigger(upgrade?ContactFxKind.Upgrade:ContactFxKind.Build,effectPosition,ClassColor(kind),Time.unscaledTimeAsDouble);
            renderDirty=true;return true;
        }
        public void CancelFeedback()
        {
            contactFx?.CancelTransient();energyBeams?.Clear();
            for(int i=0;i<shots.Length;i++)shots[i]?.Reset();
            if(towerViews!=null)for(int i=0;i<towerViews.Length;i++){var t=towerViews[i];t.PulseBegan=-1;t.RecoilUntil=0;t.Visual.localScale=Vector3.one;t.Head.localScale=Vector3.one;if(t.Kind==4)t.Head.localRotation=Quaternion.identity;}
            if(selection!=null)selection.localScale=Vector3.one;if(dragModel!=null)dragModel.localScale=Vector3.one;renderDirty=true;
        }
        internal bool IsClosestTouchTarget(int index,Vector2 screenPoint,Camera eventCamera)
        {
            if(!initialized||!interactive||!RectTransformUtility.ScreenPointToLocalPointInRectangle(touchOverlay,screenPoint,eventCamera,out var point)||!touchOverlay.rect.Contains(point))return false;
            // Keep generous touch rectangles while partitioning their overlap by the actual projected pad centers.
            // Restrict candidates to their own rectangle so a diagonal corner cannot become a dead touch area.
            int closest=-1;float best=float.PositiveInfinity;
            for(int i=0;i<projectedCenters.Length;i++)
            {
                if(!slotButtons[i].gameObject.activeInHierarchy)continue;
                Vector2 delta=point-projectedCenters[i];float half=largeView?32:16;
                if(Mathf.Abs(delta.x)>half||Mathf.Abs(delta.y)>half)continue;
                float distance=delta.sqrMagnitude;if(distance<best){best=distance;closest=i;}
            }
            return closest==index;
        }
        void Update()
        {
            if(!initialized)return;Resize(false);float blend=1-Mathf.Exp(-Time.unscaledDeltaTime*20),now=Time.unscaledTime;
            double effectTime=Time.unscaledTimeAsDouble;
            bool collectShadows=contactFx!=null&&contactFx.BeginShadows(Time.unscaledTimeAsDouble,viewCamera);
            double presentationTime=RemotePlayback?playback.Advance(Time.unscaledTimeAsDouble):0;
            foreach(var pair in active)
            {
                var u=pair.Value;var previousPosition=u.Root.transform.localPosition;
                if(RemotePlayback&&u.Motion.Count>0)
                {
                    double distance=u.Motion.Sample(presentationTime);int whole=Math.Min(map.Length,(int)distance);
                    var from=DioramaResources.Point(map.PositionAt(whole),u.Ground);
                    var to=DioramaResources.Point(map.PositionAt(Math.Min(map.Length,whole+1)),u.Ground);
                    u.Root.transform.localPosition=Vector3.Lerp(from,to,(float)(distance-whole));
                    var ahead=DioramaResources.Point(map.PositionAt(Math.Min(map.Length,whole+12)),u.Ground)-from;
                    if(ahead.sqrMagnitude>.0001f)u.Facing=Quaternion.LookRotation(ahead);
                }
                else u.Root.transform.localPosition=Vector3.Lerp(u.Root.transform.localPosition,u.Target,blend);
                u.Root.transform.localRotation=Quaternion.Slerp(u.Root.transform.localRotation,u.Facing,blend);
                u.HealthRoot.rotation=viewCamera.transform.rotation;
                float moved=Vector3.Distance(previousPosition,u.Root.transform.localPosition);
                if(animateTroops&&moved<2)u.WalkPhase=(u.WalkPhase+moved*5.5f)%(Mathf.PI*2);
                u.Properties.SetVector(ToyMotionId,new Vector4(u.WalkPhase,!ReducedEffects&&u.Kind!=2?.015f:0,u.MotionHeight,u.MotionMinY));
                if(collectShadows)
                {
                    var ground=u.Root.transform.localPosition;ground.y=ContactFxBatch.RouteGroundPlane;
                    var radii=u.Kind==2?new Vector2(.52f,.54f):u.Kind==3?new Vector2(.27f,.20f):new Vector2(.36f,.27f);
                    contactFx.OfferShadow((long)u.Id*2,ground,radii,u.Kind==2?.17f:.14f);
                }
                u.Properties.SetColor(TintId,u.Slow?new Color(.72f,.91f,1):Color.white);u.Properties.SetFloat(FlashId,!ReducedEffects&&u.FlashUntil>now?.72f:0);u.Renderer.SetPropertyBlock(u.Properties);
            }
            for(int i=0;i<towerViews.Length;i++)
            {
                var t=towerViews[i];
                if(collectShadows&&t.Entity>=0&&!t.Preview&&t.Root.gameObject.activeSelf)
                {var ground=t.Root.localPosition;ground.y=TowerGround(t.Kind)+.004f;contactFx.OfferShadow((long)t.Entity*2+1,ground,new Vector2(.62f,.50f),.18f,t.Entity==selected?2:1);}
                t.Head.localScale=Vector3.one*(t.RecoilUntil>now&&!ReducedEffects?.9f:1);
                if(t.PulseBegan>=0)
                {
                    float age=now-t.PulseBegan,duration=t.UpgradePulse?.22f:.24f;
                    if(ReducedEffects||age>=duration){t.PulseBegan=-1;t.Visual.localScale=Vector3.one;}
                    else if(t.UpgradePulse)t.Visual.localScale=Vector3.one*(1+.05f*Mathf.Sin(Mathf.PI*age/duration));
                    else{float u=age<.09f?age/.09f:(age-.09f)/.15f;u=u*u*(3-2*u);t.Visual.localScale=Vector3.one*(age<.09f?Mathf.Lerp(.92f,1.04f,u):Mathf.Lerp(1.04f,1,u));}
                }
                if(t.Kind==4&&!ReducedEffects&&t.Root.gameObject.activeSelf)t.Head.localRotation=Quaternion.Euler(0,0,now*50);
            }
            if(collectShadows)renderDirty|=contactFx.EndShadows();
            for(int i=0;i<shots.Length;i++)
            {
                var s=shots[i];if(!s.Active)continue;float t=(now-s.Began)/s.Duration;
                if(t>=1)
                {
                    if(!s.Burst&&!ReducedEffects)
                    {
                        var impact=s.To;if(s.SplashRadius>0)impact.y=ContactFxBatch.RouteGroundPlane+.012f;
                        contactFx.Trigger(s.SplashRadius>0?ContactFxKind.Explosion:ContactFxKind.Impact,impact,s.Properties.GetColor(TintId),effectTime,s.SplashRadius);
                    }
                    s.Reset();continue;
                }
                s.Root.localPosition=Vector3.Lerp(s.From,s.To,t)+(s.Burst?Vector3.zero:Vector3.up*Mathf.Sin(t*Mathf.PI)*.25f);s.Root.localScale=Vector3.one*(s.Burst?.5f+t*1.2f:1);
            }
            // Trigger and advance share the same double-precision frame clock. A rounded
            // float clock can appear to go backwards and make the bounded pool discard a shot.
            renderDirty|=contactFx.UpdateEffects(effectTime,viewCamera);
            renderDirty|=energyBeams.Update(effectTime,viewCamera,ReducedEffects);
            if(selection.gameObject.activeSelf)selection.localScale=Vector3.one*(PlacementValidator.BuildingRadius/50f/.68f)*(ReducedEffects?1:1+.045f*Mathf.Sin(now*4));
            if(dragModel.gameObject.activeSelf)dragModel.localScale=Vector3.one*(ReducedEffects?1:1+.025f*Mathf.Sin(Mathf.Clamp01((now-dragSnapBegan)/.08f)*Mathf.PI));
            if(Time.unscaledTimeAsDouble>=nextRender)
            {
                if(largeView||renderDirty||active.Count>0){viewCamera.Render();renderDirty=false;}
                nextRender=Time.unscaledTimeAsDouble+(largeView?0:1.0/12);
            }
        }
        public void SetOverviewSafeRect(Rect normalized)
        {
            float left=Mathf.Clamp(normalized.xMin,0,.8f),bottom=Mathf.Clamp(normalized.yMin,0,.8f);
            float right=Mathf.Clamp(normalized.xMax,left+.1f,1),top=Mathf.Clamp(normalized.yMax,bottom+.1f,1);
            var next=Rect.MinMaxRect(left,bottom,right,top);
            if(overviewSafeRect==next)return;overviewSafeRect=next;if(initialized)Resize(true);
        }
        public void SetOverviewInsets(Vector4 logicalFrameInsets)
        {
            var r=((RectTransform)transform).rect;if(r.width<=0||r.height<=0)return;
            SetOverviewSafeRect(Rect.MinMaxRect(logicalFrameInsets.x/r.width,logicalFrameInsets.w/r.height,
                1-logicalFrameInsets.z/r.width,1-logicalFrameInsets.y/r.height));
        }
        public void RefreshRenderNow()
        {
            if(!initialized||!isActiveAndEnabled)return;
            Resize(false);viewCamera.Render();renderDirty=false;
        }
        Vector2Int DesiredRenderSize(Vector2 size)
        {
            var canvas=GetComponentInParent<Canvas>();var root=canvas==null?null:canvas.rootCanvas;
            Camera camera=root!=null&&root.renderMode!=RenderMode.ScreenSpaceOverlay?root.worldCamera:null;
            frame.GetWorldCorners(pixelCorners);
            Vector2 a=RectTransformUtility.WorldToScreenPoint(camera,pixelCorners[0]);
            Vector2 b=RectTransformUtility.WorldToScreenPoint(camera,pixelCorners[3]);
            Vector2 c=RectTransformUtility.WorldToScreenPoint(camera,pixelCorners[1]);
            float width=Vector2.Distance(a,b),height=Vector2.Distance(a,c);
            if(width<1||height<1){float scale=canvas==null?1:canvas.scaleFactor;width=size.x*scale;height=size.y*scale;}
            // Native pixels up to 1440p-class landscape screens; previews have their own budget.
            // Preserve aspect and respond to Canvas scale / render-target changes, not only layout.
            float cap=largeView?2560:Mathf.Clamp(powerSaving?Mathf.Min(640,PreviewPixelCap):PreviewPixelCap,32,2560);
            float ratio=Mathf.Min(1,cap/Mathf.Max(width,height));
            return new Vector2Int(Mathf.Max(32,Mathf.RoundToInt(width*ratio)),Mathf.Max(32,Mathf.RoundToInt(height*ratio)));
        }
        void Resize(bool force)
        {
            Vector2 size=frame.rect.size;if(size.x<20||size.y<20)return;
            Vector2Int pixels=DesiredRenderSize(size);
            bool layoutChanged=(size-previousSize).sqrMagnitude>=1;
            bool textureChanged=texture==null||texture.width!=pixels.x||texture.height!=pixels.y;
            if(!force&&!layoutChanged&&!textureChanged)return;previousSize=size;
            float aspect=size.x/size.y;
            if(textureChanged)
            {
                if(texture!=null){viewCamera.targetTexture=null;image.texture=null;texture.Release();Destroy(texture);}
                var descriptor=new RenderTextureDescriptor(pixels.x,pixels.y,RenderTextureFormat.ARGB32,24){msaaSamples=largeView?4:2};
                descriptor.msaaSamples=Mathf.Max(1,SystemInfo.GetRenderTextureSupportedMSAASampleCount(descriptor));
                texture=new RenderTexture(descriptor){name="Defense 3D "+map.Name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};
                texture.Create();viewCamera.targetTexture=texture;image.texture=texture;renderDirty=true;
            }
            viewCamera.aspect=aspect;
            // Fit actual scenery, not fictitious tall foreground corners of its world AABB.
            // This allocation/projection runs only on initialization or a viewport resize.
            var inverse=Quaternion.Inverse(viewCamera.transform.localRotation);
            float minX=float.PositiveInfinity,minY=float.PositiveInfinity,maxX=float.NegativeInfinity,maxY=float.NegativeInfinity;
            void Include(Vector3 point)
            {
                var local=inverse*(point-resources.MapCenter);
                minX=Mathf.Min(minX,local.x);maxX=Mathf.Max(maxX,local.x);
                minY=Mathf.Min(minY,local.y);maxY=Mathf.Max(maxY,local.y);
            }
            if(largeView)
            {
                // The playable ground drives the main view. Decorative border trees and
                // hypothetical tallest towers must not shrink the whole battlefield.
                // Those edges remain reachable by panning; the HUD deliberately overlays them.
                const float edge=.35f;
                var min=DioramaResources.Point(new MapPoint(0,map.Height),GroundY)-new Vector3(edge,0,edge);
                var max=DioramaResources.Point(new MapPoint(map.Width,0),GroundY)+new Vector3(edge,0,edge);
                for(int x=0;x<2;x++)for(int z=0;z<2;z++)
                    Include(new Vector3(x==0?min.x:max.x,GroundY,z==0?min.z:max.z));
            }
            else
            {
                // The small opponent overview still shows the complete diorama and tall models.
                foreach(var point in resources.Terrain.vertices)Include(point);
                float radius=Mathf.Max(DioramaResources.MaximumEvolutionTowerRadius,resources.MaximumTowerRadius),height=Mathf.Max(DioramaResources.MaximumEvolutionTowerHeight,GroundY+resources.MaximumTowerHeight);
                var min=DioramaResources.Point(new MapPoint(0,map.Height),GroundY)-new Vector3(radius,0,radius);
                var max=DioramaResources.Point(new MapPoint(map.Width,0),height)+new Vector3(radius,0,radius);
                for(int x=0;x<2;x++)for(int z=0;z<2;z++)for(int y=0;y<2;y++)
                    Include(new Vector3(x==0?min.x:max.x,y==0?min.y:max.y,z==0?min.z:max.z));
            }
            overviewCamera=resources.MapCenter+cameraOffset+viewCamera.transform.localRotation*new Vector3((minX+maxX)*.5f,(minY+maxY)*.5f,0);
            overviewSize=Mathf.Max((maxY-minY)*.5f/overviewSafeRect.height,
                (maxX-minX)*.5f/(aspect*overviewSafeRect.width))*1.025f;
            // Respect device safe areas. HUD rectangles no longer reserve entire strips.
            Vector2 centre=overviewSafeRect.center-Vector2.one*.5f;
            overviewCamera-=viewCamera.transform.localRotation*new Vector3(centre.x*2*overviewSize*aspect,centre.y*2*overviewSize,0);
            if(ApplyView())ViewChanged?.Invoke();
        }
        void ProjectTargets()
        {
            for(int i=0;i<towerCapacity;i++)
            {
                var v=towerViews[i];if(v.Entity>=0){Vector3 p=v.Root.localPosition;ProjectTouch(i,new MapPoint(Mathf.RoundToInt(p.x*50+500),Mathf.RoundToInt(300-p.z*50)),.5f);}
                else if(!map.FreePlacement)ProjectTouch(i,slots[i],.5f);
            }
            if(map.FreePlacement)for(int m=0;m<mineSites.Length;m++)ProjectTouch(towerCapacity+m,slots[mineSites[m]],.5f);
            for(int i=0;i<pendingCount;i++)ProjectRect(pendingLabels[i].rectTransform,pendingBuilds[i].Position,1.75f);
            renderDirty=true;
        }
        bool ApplyView()
        {
            Vector3 previous=viewCamera.transform.localPosition;float previousSize=viewCamera.orthographicSize;
            float visible=overviewSize/zoomFactor;
            // Extra reach allows map edges to be pulled clear of the HUD overlay at every zoom.
            float extraX=visible*viewCamera.aspect*.44f,extraY=visible*.38f;
            panOffset.x=Mathf.Clamp(panOffset.x,-(overviewSize-visible)*viewCamera.aspect-extraX,(overviewSize-visible)*viewCamera.aspect+extraX);
            panOffset.y=Mathf.Clamp(panOffset.y,-(overviewSize-visible)-extraY,(overviewSize-visible)+extraY);
            viewCamera.orthographicSize=visible;viewCamera.transform.localPosition=overviewCamera+viewCamera.transform.localRotation*new Vector3(panOffset.x,panOffset.y,0);
            ProjectTargets();return (viewCamera.transform.localPosition-previous).sqrMagnitude>1e-10f||Mathf.Abs(viewCamera.orthographicSize-previousSize)>1e-5f;
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        public void SetZoom(float value,Vector2? anchor=null,Camera eventCamera=null)
        {
            if(!IsNavigable||!Finite(value)||(anchor.HasValue&&(!Finite(anchor.Value.x)||!Finite(anchor.Value.y))))return;
            value=Mathf.Clamp(value,MinimumZoom,MaximumZoom);if(Mathf.Approximately(value,zoomFactor))return;
            Vector3 previous=viewCamera.transform.localPosition,before=default;float previousSize=viewCamera.orthographicSize;bool anchored=anchor.HasValue&&TryScreenToWorldGround(anchor.Value,eventCamera,out before);
            zoomFactor=value;ApplyView();
            if(anchored&&TryScreenToWorldGround(anchor.Value,eventCamera,out var after))
            {
                var shift=Quaternion.Inverse(viewCamera.transform.rotation)*(before-after);panOffset+=new Vector2(shift.x,shift.y);ApplyView();
            }
            if((viewCamera.transform.localPosition-previous).sqrMagnitude>1e-10f||Mathf.Abs(viewCamera.orthographicSize-previousSize)>1e-5f)ViewChanged?.Invoke();
        }
        public void PanByScreenDelta(Vector2 delta,Camera eventCamera=null)
        {
            if(!IsNavigable||image==null||!Finite(delta.x)||!Finite(delta.y)||delta.sqrMagnitude<1e-10f)return;
            var center=RectTransformUtility.WorldToScreenPoint(eventCamera,image.rectTransform.TransformPoint(image.rectTransform.rect.center));
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform,center,eventCamera,out var a)||!RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform,center+delta,eventCamera,out var b))return;
            Vector2 local=b-a;float scale=2*viewCamera.orthographicSize/image.rectTransform.rect.height;
            panOffset-=local*scale;if(ApplyView())ViewChanged?.Invoke();
        }
        bool TryScreenToWorldGround(Vector2 screenPoint,Camera eventCamera,out Vector3 point)
        {
            point=default;if(!initialized||image==null||!RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform,screenPoint,eventCamera,out var local))return false;
            Rect r=image.rectTransform.rect;if(!r.Contains(local))return false;Rect uv=image.uvRect;
            var ray=viewCamera.ViewportPointToRay(new Vector3(uv.x+(local.x-r.xMin)/r.width*uv.width,uv.y+(local.y-r.yMin)/r.height*uv.height,0));
            var plane=new Plane(world.transform.up,world.transform.TransformPoint(new Vector3(0,GroundY,0)));if(!plane.Raycast(ray,out float d))return false;point=ray.GetPoint(d);return true;
        }
        public void ResetView(){if(!initialized)return;zoomFactor=1;panOffset=Vector2.zero;if(ApplyView())ViewChanged?.Invoke();}
        void ProjectTouch(int index,MapPoint point,float y)
        {
            var r=(RectTransform)slotButtons[index].transform;ProjectRect(r,point,y);Vector3 p=viewCamera.WorldToViewportPoint(world.transform.TransformPoint(DioramaResources.Point(point,y)));
            projectedCenters[index]=new Vector2((p.x-.5f)*touchOverlay.rect.width,(p.y-.5f)*touchOverlay.rect.height);
            r.sizeDelta=Vector2.one*(largeView?64:32);slotLabels[index].fontSize=largeView?25:14;
        }
        void ProjectRect(RectTransform r,MapPoint point,float y)
        {
            Vector3 projected=viewCamera.WorldToViewportPoint(world.transform.TransformPoint(DioramaResources.Point(point,y)));
            r.anchorMin=r.anchorMax=new Vector2(projected.x,projected.y);r.anchoredPosition=Vector2.zero;
        }
        void ResetVisualState()
        {
            contactFx?.Clear();HideSplashPreview();animateTroops=false;
            playback.Reset();frameTowers.Clear();fixedTowers.Clear();aimPositions.Clear();foreach(var cell in aimCells.Values)cell.Clear();
            foreach(var pair in active){pair.Value.Reset();pool.Push(pair.Value);}active.Clear();seen.Clear();removed.Clear();
            for(int i=0;i<shots.Length;i++)shots[i]?.Reset();shotCursor=0;
            ClearDragTarget();HidePlacementGuide();CancelFeedback();
            for(int i=0;i<towerViews.Length;i++){towerViews[i].Entity=-1;towerViews[i].Kind=-1;towerViews[i].Level=-1;towerViews[i].PreviousCooldown=0;towerViews[i].RecoilUntil=0;towerViews[i].Root.gameObject.SetActive(false);}
        }
        void OnDisable()
        {
            contactFx?.Clear();HideSplashPreview();animateTroops=false;
            playback.Reset();foreach(var entry in active)entry.Value.Motion.Reset();
            CancelFeedback();ClearDragTarget();HidePlacementGuide();SetPendingBuilds(null);
            if(pendingLabels!=null)for(int i=0;i<pendingLabels.Length;i++)pendingLabels[i].gameObject.SetActive(false);
            if(pendingViews!=null)for(int i=0;i<pendingViews.Length;i++)pendingViews[i].Root.gameObject.SetActive(false);
            if(world!=null)world.SetActive(false);
        }
        void OnApplicationPause(bool paused){contactFx?.Clear();if(paused){HideSplashPreview();CancelFeedback();animateTroops=false;}playback.Reset();foreach(var entry in active)entry.Value.Motion.Reset();}
        void OnEnable(){if(world!=null){world.SetActive(true);renderDirty=true;}}
        void OnDestroy()
        {
            initialized=false;energyBeams?.Dispose();energyBeams=null;contactFx?.Dispose();contactFx=null;if(viewCamera!=null)viewCamera.targetTexture=null;if(image!=null)image.texture=null;
            if(texture!=null){texture.Release();Destroy(texture);texture=null;}
            if(world!=null){world.SetActive(false);Destroy(world);world=null;}
            if(splashPreviewMesh!=null)Destroy(splashPreviewMesh);if(backdropMesh!=null)Destroy(backdropMesh);if(placementGuideMesh!=null)Destroy(placementGuideMesh);if(placementMarkerMesh!=null)Destroy(placementMarkerMesh);
            if(bossCrestMesh!=null)Destroy(bossCrestMesh);if(superBossCrestMesh!=null)Destroy(superBossCrestMesh);
            resources?.Dispose();resources=null;
            active.Clear();pool.Clear();seen.Clear();removed.Clear();if(layer>=24&&layer<=30){LayerLeases[layer-24]=false;layer=-1;}
        }
    }
}
