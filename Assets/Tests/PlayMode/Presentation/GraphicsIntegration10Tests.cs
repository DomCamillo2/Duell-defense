using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using BigInteger=System.Numerics.BigInteger;
using DefenseDuell.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class GraphicsIntegration10Tests
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly int MotionId=Shader.PropertyToID("_ToyMotion");
        static T Field<T>(object source,string name)=>(T)source.GetType().GetField(name,Hidden|BindingFlags.Public).GetValue(source);
        static void Call(object source,string name,params object[] args)=>source.GetType().GetMethod(name,Hidden).Invoke(source,args);
        static BoardView Board(GameContent content,out GameObject canvas)
        {
            canvas=new GameObject("graphics integration canvas",typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var go=new GameObject("graphics integration board",typeof(RectTransform));go.transform.SetParent(canvas.transform,false);
            ((RectTransform)go.transform).sizeDelta=new Vector2(1000,650);
            var board=go.AddComponent<BoardView>();board.Initialize(content,Array.Empty<Sprite>(),Array.Empty<Sprite>(),true,_=>{});return board;
        }
        static void Group(GraphicsSurfaceArchive archive,HashSet<string> keys,string key,params Mesh[] parts)
        {
            Assert.That(archive.TryGet(key,out var record),Is.True,key);keys.Add(key);
            Assert.That(record.Parts.Length,Is.EqualTo(parts.Length),key);
            for(int part=0;part<parts.Length;part++)
            {
                var mesh=parts[part];var baked=record.Parts[part];var uv=mesh.uv2;
                Assert.That(GraphicsSurfaceCache.GeometrySha256(mesh),Is.EqualTo(baked.GeometrySha256),key);
                Assert.That(uv.Length,Is.EqualTo(mesh.vertexCount),key);
                Assert.That(uv.Length,Is.EqualTo(baked.Darkening.Length),key);
                for(int v=0;v<uv.Length;v++)
                    if(Mathf.Abs(uv[v].x-baked.Darkening[v]/255f)>1e-6f||uv[v].y!=0)
                        Assert.Fail(key+" vertex "+v+" did not receive its exact baked indirect darkening");
            }
        }
        [UnityTest] public IEnumerator ActualBoardBackdropAndSharedModelsApplyExactDataWithoutRepeatingShading()
        {
            var archive=GraphicsSurfaceAssets.Load();Assert.That(archive,Is.Not.Null);
            foreach(var content in MapCatalog.CreateContents(GameContent.CreateDefault()))
            {
                var board=Board(content,out var canvas);
                try
                {
                    var r=Field<DioramaResources>(board,"resources");var keys=new HashSet<string>();
                    Assert.That(GraphicsSurfaceAssets.Load(),Is.SameAs(archive));
                    var backdrop=Field<Mesh>(board,"backdropMesh");Group(archive,keys,"backdrop/"+content.Map.Id,backdrop);
                    int expectedBaseGroups=content.Units.Length+2;for(int kind=0;kind<content.Towers.Length;kind++)expectedBaseGroups+=kind==4?3:6;
                    Assert.That(r.GraphicsSurfaceAppliedGroups,Is.EqualTo(expectedBaseGroups),"Actual BoardView initializes its own backdrop after catalog surfaces");
                    var primary=r.GetTowerEvolution(0,8,0);var uv=primary.Head.uv2;var paint=primary.Head.colors32;var finish=primary.Head.uv;
                    var secondary=r.GetTowerEvolution(0,0,8);var mixed=r.GetTowerEvolution(0,4,4);
                    Assert.That(secondary.Base,Is.SameAs(primary.Base));Assert.That(mixed.Head,Is.SameAs(primary.Head));
                    Group(archive,keys,"tower/0/t8",primary.Base,primary.Head);
                    Assert.That(primary.Head.uv2,Is.EqualTo(uv));Assert.That(primary.Head.colors32,Is.EqualTo(paint));Assert.That(primary.Head.uv,Is.EqualTo(finish));
                    var troop=r.GetUnitEvolution(0,10);Assert.That(r.GetUnitEvolution(0,1000000),Is.SameAs(troop));Group(archive,keys,"unit/0/t10",troop);
                    int applied=r.GraphicsSurfaceAppliedGroups;r.GetTowerEvolution(0,8,0);r.GetTowerEvolution(0,0,8);r.GetTowerEvolution(0,4,4);r.GetUnitEvolution(0,10);
                    Assert.That(r.GraphicsSurfaceAppliedGroups,Is.EqualTo(applied));
                    var old=backdrop.uv2;r.ApplyGraphicsBackdropSurface(backdrop);Assert.That(backdrop.uv2,Is.EqualTo(old),"Repeated application must not multiply darkening");
                    Assert.That(r.GraphicsSurfaceMissingGroups,Is.Zero);Assert.That(r.GraphicsSurfaceRejectedGroups,Is.Zero);
                }
                finally {Object.Destroy(canvas);}
                yield return null;yield return null;
            }
        }

        [UnityTest] public IEnumerator BoardContactsConfirmedEffectsOptionsAndOwnedResourcesHaveBoundedLifetimes()
        {
            var content=GameContent.CreateDefault();var board=Board(content,out var canvas);GameObject world=null;Mesh shadowMesh=null;Material fxMaterial=null;
            try
            {
                world=Field<GameObject>(board,"world");var fx=Field<ContactFxBatch>(board,"contactFx");
                var state=new MatchSimulation(content,7,851).State;state.Phase=MatchPhase.Playing;state.WavePhase=WavePhase.Active;
                for(int i=0;i<50;i++)state.Towers.Add(new TowerState { Id=i+1,Owner=PlayerSlot.Zero,Slot=i,SiteId=-1,Kind=0,Position=new MapPoint(80+i%10*85,80+i/10*90),Invested=content.Towers[0].Price });
                for(int i=0;i<260;i++)state.Units.Add(new UnitState { Id=1000+i,Defender=PlayerSlot.Zero,Kind=i%4,Hp=1,WaveId=1,Origin=UnitOrigin.PaidPvP,Distance=i*(content.Map.Length-20)/260 });
                board.Display(state,PlayerSlot.Zero,1);yield return new WaitForSecondsRealtime(.12f);
                Assert.That(board.VisibleCount,Is.EqualTo(260),"Visual shadow budgets must not remove simulation actors");
                Assert.That(board.ContactShadowCount,Is.InRange(65,ContactShadowSelection.Capacity));
                var shadow=world.transform.Find("Combined contact shadows").GetComponent<MeshRenderer>();
                var effects=world.transform.Find("Combined bounded dust and sparks").GetComponent<MeshRenderer>();
                Assert.That(shadow.enabled,Is.True);Assert.That(effects.sharedMaterial,Is.SameAs(shadow.sharedMaterial));
                shadowMesh=shadow.GetComponent<MeshFilter>().sharedMesh;fxMaterial=shadow.sharedMaterial;
                Assert.That(shadowMesh.vertexCount,Is.EqualTo(ContactShadowSelection.Capacity*4));
                Assert.That(board.PlayConfirmedFeedback(1,0,0,0,false,0,0),Is.True);Assert.That(fx.ActiveEffectCount,Is.EqualTo(1));
                Assert.That(board.PlayConfirmedFeedback(999,0,0,0,false,0,0),Is.False);Assert.That(fx.ActiveEffectCount,Is.EqualTo(1),"Rejected/unmatched feedback cannot spawn particles");
                Assert.That(board.PlayConfirmedFeedback(1,0,0,0,true,0,0),Is.True);Assert.That(fx.ActiveEffectCount,Is.EqualTo(2));
                board.PowerSaving=true;yield return new WaitForSecondsRealtime(.09f);
                Assert.That(board.ContactShadowCount,Is.InRange(1,ContactShadowSelection.PowerCapacity));Assert.That(board.VisibleCount,Is.EqualTo(260));
                Assert.That(fx.ActiveEffectCount,Is.LessThanOrEqualTo(ContactFxEvents.PowerCapacity));
                board.ReducedEffects=true;
                Assert.That(fx.ActiveEffectCount,Is.Zero);Assert.That(board.PlayConfirmedFeedback(1,0,0,0,true,0,0),Is.False);
                yield return new WaitForSecondsRealtime(.09f);Assert.That(board.ContactShadowCount,Is.GreaterThan(0),"Reduced motion retains static contact cues");
                board.ReducedEffects=false;board.PowerSaving=false;board.PlayConfirmedFeedback(1,0,0,0,true,0,0);
                Call(board,"OnApplicationPause",true);Assert.That(fx.ActiveEffectCount,Is.Zero);Assert.That(board.ContactShadowCount,Is.Zero);
                board.enabled=false;Assert.That(world.activeSelf,Is.False);Assert.That(fx.ActiveEffectCount,Is.Zero);
                board.enabled=true;Assert.That(world.activeSelf,Is.True);
                state.SessionId++;state.Towers.Clear();state.Units.Clear();board.Display(state,PlayerSlot.Zero);yield return new WaitForSecondsRealtime(.08f);
                Assert.That(board.VisibleCount,Is.Zero);Assert.That(board.ContactShadowCount,Is.Zero);Assert.That(fx.ActiveEffectCount,Is.Zero);
            }
            finally {Object.Destroy(canvas);}
            yield return null;yield return null;
            Assert.That(world==null,Is.True);Assert.That(shadowMesh==null,Is.True);Assert.That(fxMaterial==null,Is.True);
        }
        static Vector4 Motion(object unit)
        {
            var renderer=Field<MeshRenderer>(unit,"Renderer");var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);return block.GetVector(MotionId);
        }
        [UnityTest] public IEnumerator TroopShaderMotionFollowsDistanceAndStopsForPreparationPauseAndReducedEffects()
        {
            var content=GameContent.CreateDefault();var board=Board(content,out var canvas);
            try
            {
                var state=new MatchSimulation(content,7,852).State;state.Phase=MatchPhase.Playing;state.WavePhase=WavePhase.Active;
                state.Towers.Add(new TowerState { Id=1,Owner=PlayerSlot.Zero,Slot=0,Kind=0,SiteId=-1,Position=new MapPoint(500,300),Invested=content.Towers[0].Price });
                state.Units.Add(new UnitState { Id=100,Defender=PlayerSlot.Zero,Kind=0,Hp=1,WaveId=1,Distance=100 });
                state.Units.Add(new UnitState { Id=102,Defender=PlayerSlot.Zero,Kind=2,Hp=1,WaveId=1,Distance=160 });
                board.Display(state,PlayerSlot.Zero);yield return null;yield return null;
                var active=Field<IDictionary>(board,"active");var runner=active[100];var tank=active[102];
                var still=Motion(runner);Assert.That(still.y,Is.GreaterThan(0));Assert.That(Motion(tank).y,Is.Zero);
                yield return new WaitForSecondsRealtime(.06f);Assert.That(Motion(runner).x,Is.EqualTo(still.x).Within(1e-5f),"Wall time alone cannot animate a stationary actor");
                state.Units[0].Distance+=20;state.Tick+=4;board.Display(state,PlayerSlot.Zero);yield return new WaitForSecondsRealtime(.08f);
                Assert.That(Mathf.Abs(Motion(runner).x-still.x),Is.GreaterThan(.01f),"Real displayed movement drives troop phase");
                var towerRoot=Field<Transform>(board,"towerRoot");var block=new MaterialPropertyBlock();
                foreach(var renderer in towerRoot.GetComponentsInChildren<MeshRenderer>(true))
                {renderer.GetPropertyBlock(block);Assert.That(block.GetVector(MotionId),Is.EqualTo(Vector4.zero),renderer.name+" opted a building into troop sway");}
                state.WavePhase=WavePhase.Preparing;board.Display(state,PlayerSlot.Zero);var frozen=Motion(runner).x;
                yield return new WaitForSecondsRealtime(.06f);Assert.That(Motion(runner).x,Is.EqualTo(frozen).Within(1e-5f));
                state.WavePhase=WavePhase.Active;state.Units[0].Distance+=20;board.Display(state,PlayerSlot.Zero);Call(board,"OnApplicationPause",true);frozen=Motion(runner).x;
                yield return new WaitForSecondsRealtime(.06f);Assert.That(Motion(runner).x,Is.EqualTo(frozen).Within(1e-5f));
                board.ReducedEffects=true;yield return null;Assert.That(Motion(runner).y,Is.Zero);
                state.SessionId++;state.Units.Clear();board.Display(state,PlayerSlot.Zero);
                Assert.That(Field<float>(runner,"WalkPhase"),Is.Zero);Assert.That(Motion(runner),Is.EqualTo(Vector4.zero),"Recycled actor must not retain shader motion");
            }
            finally {Object.Destroy(canvas);}
            yield return null;
        }

        [Serializable] sealed class FieldActor
        {
            public string role,level,primary,secondary,hp,maxHp;
            public int id,kind,mapX,mapY,distance;
            public float renderedX,renderedY,renderedZ,motionPhase;
        }
        [Serializable] sealed class FieldShadow
        {public long stableId;public float x,y,z,radiusX,radiusZ,opacity;}
        [Serializable] sealed class FieldCapture
        {
            public string fixtureScope="Arranged native graphics fixture; valid building geometry and resolved unit/tower recipes, NOT a played or authority-validated match.";
            public string drawcountsScope="Board camera renderer/frustum/submesh counts; drawSubmissionUpperBound is not a GPU profiler draw-call measurement.";
            public string mapId,view,image;
            public int actualRenderWidth,actualRenderHeight,msaaSamples,shadowCount,liveEffects,activeMeshRenderers,frustumVisibleMeshRenderers,drawSubmissionUpperBound;
            public long visibleTriangleUpperBound;
            public float zoom,cameraSize;
            public int movedFrames,frameCount,fixtureControlTick,aoAppliedGroups,aoMissingGroups,aoRejectedGroups;
            public List<FieldActor> actors=new List<FieldActor>();
            public List<FieldShadow> shadows=new List<FieldShadow>();
        }
        static MapPoint FieldBuildPoint(GameContent content,MatchState state,int kind,MapPoint desired)
        {
            MapPoint best=default;long score=long.MaxValue;
            for(int y=35;y<content.Map.Height-35;y+=12)for(int x=35;x<content.Map.Width-35;x+=12)
            {
                long dx=x-desired.X,dy=y-desired.Y,d=dx*dx+dy*dy;if(d>=score)continue;
                var point=new MapPoint(x,y);
                if(PlacementValidator.ValidatePosition(content,state,PlayerSlot.Zero,kind,-1,point)!=ErrorCode.None)continue;
                best=point;score=d;
            }
            Assert.That(score,Is.Not.EqualTo(long.MaxValue),"No legal arranged build position for class "+kind);return best;
        }
        static MatchState FieldState(GameContent content)
        {
            var state=new MatchSimulation(content,733,853).State;state.Phase=MatchPhase.Playing;state.WavePhase=WavePhase.Active;
            state.PreparationRemaining=0;state.Tick=200;state.PlayTicks=100;state.Players[0].AvailableTowerMask=(1<<content.Towers.Length)-1;
            for(int row=0;row<2;row++)for(int kind=0;kind<content.Towers.Length;kind++)
            {
                int level=row==0?0:new[]{3,6,10}[kind%3];
                var desired=new MapPoint(content.Map.Width*(kind+1)/(content.Towers.Length+1),content.Map.Height*(row==0?24:72)/100);
                var point=FieldBuildPoint(content,state,kind,desired);
                var tower=new TowerState { Id=state.Towers.Count+1,Owner=PlayerSlot.Zero,Slot=state.Towers.Count,Kind=kind,SiteId=-1,Position=point,
                    Level=level,DamageLevel=kind==4||level==0?0:level-1,RangeLevel=kind==4||level==0?0:1,Invested=TowerUpgradeRules.TotalInvestment(content,kind,level) };
                Assert.That(TowerUpgradeRules.ValidateState(content,tower),Is.EqualTo(ErrorCode.None));state.Towers.Add(tower);
            }
            for(int kind=0;kind<4;kind++)state.Players[1].ResearchLevels[kind]=10;
            for(int group=0;group<4;group++)for(int kind=0;kind<4;kind++)
            {
                int level=new[]{0,3,6,10}[group];var recipe=UnitResearchRules.Resolve(content,kind,level,UnitOrigin.PaidPvP,1);
                state.Units.Add(new UnitState { Id=100+group*4+kind,Defender=PlayerSlot.Zero,Kind=kind,WaveId=1,Origin=UnitOrigin.PaidPvP,ResearchLevel=level,
                    Hp=group%2==0?recipe.MaxHp:BigInteger.Max(1,recipe.MaxHp*3/4),Distance=content.Map.Length*(group+1)/5+kind*22 });
            }
            foreach(var tower in state.Towers)
                Assert.That(PlacementValidator.ValidatePosition(content,state,tower.Owner,tower.Kind,tower.SiteId,tower.Position,tower.Id),Is.EqualTo(ErrorCode.None));
            return state;
        }
        static void CaptureField(BoardView board,GameContent content,MatchState state,string view,int movedFrames)
        {
            board.RefreshRenderNow();var rt=Field<RenderTexture>(board,"texture");
            Assert.That(new Vector2Int(rt.width,rt.height),Is.EqualTo(new Vector2Int(1920,1080)),"Capture uses actual native BoardView target, never enlarged PNG output");
            var camera=Field<Camera>(board,"viewCamera");var world=Field<GameObject>(board,"world");var fx=Field<ContactFxBatch>(board,"contactFx");
            var name="map-"+content.Map.Id+"-"+view;var directory="TestResults/GraphicsField-v15";Directory.CreateDirectory(directory);
            var report=new FieldCapture { mapId=content.Map.Id,view=view,image=name+".png",actualRenderWidth=rt.width,actualRenderHeight=rt.height,msaaSamples=rt.antiAliasing,
                shadowCount=board.ContactShadowCount,liveEffects=fx.ActiveEffectCount,zoom=board.ZoomFactor,cameraSize=camera.orthographicSize,movedFrames=movedFrames,frameCount=Time.frameCount,fixtureControlTick=state.Tick };
            var surfaces=Field<DioramaResources>(board,"resources");report.aoAppliedGroups=surfaces.GraphicsSurfaceAppliedGroups;report.aoMissingGroups=surfaces.GraphicsSurfaceMissingGroups;report.aoRejectedGroups=surfaces.GraphicsSurfaceRejectedGroups;
            Assert.That(report.aoMissingGroups,Is.Zero);Assert.That(report.aoRejectedGroups,Is.Zero);
            Assert.That(report.shadowCount,Is.GreaterThan(0));
            var planes=GeometryUtility.CalculateFrustumPlanes(camera);
            foreach(var renderer in world.GetComponentsInChildren<MeshRenderer>())
            {
                if(!renderer.enabled||!renderer.gameObject.activeInHierarchy)continue;report.activeMeshRenderers++;
                if(!GeometryUtility.TestPlanesAABB(planes,renderer.bounds))continue;report.frustumVisibleMeshRenderers++;
                var mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;
                report.drawSubmissionUpperBound+=mesh.subMeshCount;
                for(int sub=0;sub<mesh.subMeshCount;sub++)report.visibleTriangleUpperBound+=(long)mesh.GetIndexCount(sub)/3;
            }
            foreach(var tower in state.Towers)report.actors.Add(new FieldActor { role="tower",id=tower.Id,kind=tower.Kind,mapX=tower.Position.X,mapY=tower.Position.Y,
                level=tower.Level.ToString(),primary=tower.DamageLevel.ToString(),secondary=tower.RangeLevel.ToString() });
            var active=Field<IDictionary>(board,"active");
            foreach(var unit in state.Units)
            {
                var visual=active[unit.Id];var position=Field<GameObject>(visual,"Root").transform.localPosition;var point=content.Map.PositionAt(unit.Distance);
                report.actors.Add(new FieldActor { role="troop",id=unit.Id,kind=unit.Kind,level=unit.ResearchLevel.ToString(),distance=unit.Distance,mapX=point.X,mapY=point.Y,
                    renderedX=position.x,renderedY=position.y,renderedZ=position.z,motionPhase=Field<float>(visual,"WalkPhase"),hp=unit.Hp.ToString(),
                    maxHp=UnitResearchRules.Resolve(content,unit.Kind,unit.ResearchLevel,unit.Origin,unit.WaveId).MaxHp.ToString() });
            }
            var selection=Field<ContactShadowSelection>(fx,"shadowSelection");
            for(int i=0;i<selection.Count;i++){var a=selection[i];report.shadows.Add(new FieldShadow { stableId=a.StableId,x=a.X,y=a.Y,z=a.Z,radiusX=a.RadiusX,radiusZ=a.RadiusZ,opacity=a.Opacity });}
            var prior=RenderTexture.active;var resolved=RenderTexture.GetTemporary(rt.width,rt.height,0,RenderTextureFormat.ARGB32);var pixels=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            try
            {
                Graphics.Blit(rt,resolved);RenderTexture.active=resolved;pixels.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(directory,name+".png"),pixels.EncodeToPNG());File.WriteAllText(Path.Combine(directory,name+".json"),JsonUtility.ToJson(report,true));
            }
            finally {RenderTexture.active=prior;RenderTexture.ReleaseTemporary(resolved);Object.Destroy(pixels);}
            Debug.Log("GRAPHICS_FIELD_NATIVE "+name+" "+rt.width+"x"+rt.height+" towers="+state.Towers.Count+" troops="+state.Units.Count+" shadows="+report.shadowCount+" visibleRenderers="+report.frustumVisibleMeshRenderers);
        }
        [UnityTest] public IEnumerator ArrangeAllClassesOnThreeNativeFieldsAndCaptureOverviewAndZoom()
        {
            foreach(var content in MapCatalog.CreateContents(GameContent.CreateDefault()))
            {
                var cameraObject=new GameObject("field capture physical canvas camera",typeof(Camera));var physical=cameraObject.GetComponent<Camera>();physical.enabled=false;physical.orthographic=true;physical.transform.position=new Vector3(0,0,-100);
                var physicalTarget=new RenderTexture(1920,1080,24);physicalTarget.Create();physical.targetTexture=physicalTarget;
                var canvasObject=new GameObject("field capture canvas",typeof(RectTransform),typeof(Canvas));var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=physical;canvas.planeDistance=10;canvas.scaleFactor=1.5f;
                try
                {
                    var go=new GameObject("arranged native field",typeof(RectTransform));go.transform.SetParent(canvas.transform,false);var rect=(RectTransform)go.transform;
                    rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;Canvas.ForceUpdateCanvases();
                    var board=go.AddComponent<BoardView>();board.Initialize(content,null,null,true,null);var state=FieldState(content);board.Display(state,PlayerSlot.Zero);
                    for(int frame=0;frame<24;frame++)
                    {state.Tick++;state.PlayTicks++;foreach(var unit in state.Units)unit.Distance=Math.Min(content.Map.Length-30,unit.Distance+2);board.Display(state,PlayerSlot.Zero);yield return null;}
                    board.ResetView();yield return new WaitForSecondsRealtime(.09f);CaptureField(board,content,state,"overview",24);
                    var focus=new MapPoint(content.Map.Width/2,content.Map.Height*60/100);board.SetZoom(1.8f);
                    var centre=RectTransformUtility.WorldToScreenPoint(physical,rect.TransformPoint(rect.rect.center));board.PanByScreenDelta(centre-board.MapPointToScreen(focus,physical),physical);
                    for(int frame=0;frame<12;frame++)
                    {state.Tick++;state.PlayTicks++;foreach(var unit in state.Units)unit.Distance=Math.Min(content.Map.Length-30,unit.Distance+2);board.Display(state,PlayerSlot.Zero);yield return null;}
                    yield return new WaitForSecondsRealtime(.09f);CaptureField(board,content,state,"zoom",36);
                }
                finally {physical.targetTexture=null;Object.Destroy(canvasObject);Object.Destroy(cameraObject);physicalTarget.Release();Object.Destroy(physicalTarget);}
                yield return null;yield return null;
            }
        }

        [UnityTest] public IEnumerator NativeShaderUsesActualDirectionalLightIntensity()
        {
            const int layer=31;var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);var enabled=new bool[lights.Length];
            var priorSun=RenderSettings.sun;var priorActive=RenderTexture.active;var root=new GameObject("isolated native shader intensity");
            var mesh=new Mesh{name="Lighting response quad"};Material material=null;RenderTexture target=null;Texture2D pixels=null;
            try
            {
                for(int i=0;i<lights.Length;i++){enabled[i]=lights[i].enabled;lights[i].enabled=false;}
                var shader=Resources.Load<Shader>("DefenseDiorama");Assert.That(shader,Is.Not.Null);Assert.That(shader.isSupported,Is.True);
                material=new Material(shader);
                mesh.vertices=new[]{new Vector3(-1,-1,0),new Vector3(-1,1,0),new Vector3(1,1,0),new Vector3(1,-1,0)};
                mesh.normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.back};mesh.triangles=new[]{0,1,2,0,2,3};
                var paint=new Color(.32f,.40f,.24f,1);mesh.colors=new[]{paint,paint,paint,paint};mesh.uv=new[]{new Vector2(.25f,0),new Vector2(.25f,0),new Vector2(.25f,0),new Vector2(.25f,0)};
                var surface=new GameObject("lit surface",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};surface.transform.SetParent(root.transform);surface.GetComponent<MeshFilter>().sharedMesh=mesh;surface.GetComponent<MeshRenderer>().sharedMaterial=material;
                var cameraObject=new GameObject("isolated light response camera",typeof(Camera));cameraObject.transform.SetParent(root.transform);cameraObject.transform.position=new Vector3(0,0,-3);
                var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<layer;camera.orthographic=true;camera.orthographicSize=1.2f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.allowHDR=false;camera.renderingPath=RenderingPath.Forward;
                var lightObject=new GameObject("measured key light",typeof(Light));lightObject.transform.SetParent(root.transform);var light=lightObject.GetComponent<Light>();light.type=LightType.Directional;light.color=Color.white;light.cullingMask=1<<layer;light.renderMode=LightRenderMode.ForcePixel;RenderSettings.sun=light;
                target=new RenderTexture(64,64,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;pixels=new Texture2D(64,64,TextureFormat.RGBA32,false);
                var samples=new float[3];
                for(int i=0;i<3;i++)
                {
                    light.intensity=i*.5f;yield return null;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,64,64),0,0);pixels.Apply();
                    double sum=0;for(int y=20;y<44;y++)for(int x=20;x<44;x++){var c=pixels.GetPixel(x,y);sum+=.2126*c.r+.7152*c.g+.0722*c.b;}samples[i]=(float)(sum/(24*24));
                }
                Debug.Log("GRAPHICS_LIGHT_RESPONSE_NATIVE intensity0="+samples[0]+" intensity0.5="+samples[1]+" intensity1="+samples[2]);
                Assert.That(samples[0],Is.GreaterThan(.02f),"Indirect fill remains when key light is off");
                Assert.That(samples[1]-samples[0],Is.GreaterThan(.015f));Assert.That(samples[2]-samples[1],Is.GreaterThan(.015f));
                Assert.That(samples[2],Is.LessThan(.95f),"Palette should not clip to white");
            }
            finally
            {
                RenderTexture.active=priorActive;RenderSettings.sun=priorSun;
                for(int i=0;i<lights.Length;i++)if(lights[i]!=null)lights[i].enabled=enabled[i];
                if(target!=null){target.Release();Object.Destroy(target);}Object.Destroy(pixels);Object.Destroy(material);Object.Destroy(mesh);Object.Destroy(root);
            }
            yield return null;
        }
    }
}
