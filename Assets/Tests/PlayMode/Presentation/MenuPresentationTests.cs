using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class MenuPresentationTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        SessionCoordinator session;
        GameUI ui;
        Component progress;
        string profileDirectory,profilePath;
        int oldFrameRate,oldVsync;
        readonly Dictionary<string,int?> preferences=new Dictionary<string,int?>();
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
        static void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Private).Invoke(target,args);
        Button Find(string name)=>ui.GetComponentsInChildren<Button>().First(b=>b.name==name);
        MenuMotion Motion=>Field<MenuMotion>(ui,"homeMotion");
        BoardView Preview=>Field<BoardView>(ui,"menuBoard");
        object Profile=>progress.GetType().GetProperty("Profile").GetValue(progress);
        static int ProfileInt(object profile,string name)=>(int)profile.GetType().GetField(name).GetValue(profile);
        static Vector2 Center(Component c)
        {var r=(RectTransform)c.transform;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));}
        static GameObject Hit(Vector2 point)
        {
            var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            Assert.That(hits,Is.Not.Empty,"No native UI raycast at "+point);return hits[0].gameObject;
        }
        static void Tap(Button button,int pointer=1200)
        {
            Assert.That(button.interactable,Is.True,button.name);var position=Center(button);var hit=Hit(position);
            Assert.That(hit.GetComponentInParent<Button>(),Is.SameAs(button),"Covered button: "+button.name);
            var e=new PointerEventData(EventSystem.current){position=position,pressPosition=position,pointerId=pointer,
                button=PointerEventData.InputButton.Left,eligibleForClick=true};
            ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerClickHandler);
        }
        static void Submit(Button button)=>ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
        void InitializeTemporaryProfile()=>progress.GetType().GetMethod("Initialize").Invoke(progress,new object[]{session,profilePath});
        [UnitySetUp] public IEnumerator Start()
        {
            oldFrameRate=Application.targetFrameRate;oldVsync=QualitySettings.vSyncCount;
            preferences.Clear();
            foreach(string key in new[]{"duell.reduced","duell.power"})
            {preferences[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetInt(key):(int?)null;PlayerPrefs.SetInt(key,0);}
            SceneManager.LoadScene("Boot");yield return new WaitForSecondsRealtime(.2f);
            session=Object.FindFirstObjectByType<SessionCoordinator>();ui=Object.FindFirstObjectByType<GameUI>();
            Assert.That(ui,Is.Not.Null);Assert.That(session.Status,Is.EqualTo(ConnectionState.Idle));
            // Reflection avoids adding Progression to the Presentation.Tests asmdef. Rebind before
            // any match/unlock can save; only this unique temporary profile is ever modified.
            progress=Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Single(c=>c.GetType().FullName=="DefenseDuell.Progression.LocalProgress");
            profileDirectory=Path.Combine(Path.GetTempPath(),"DefenseDuell-menu-tests-"+Guid.NewGuid().ToString("N"));
            profilePath=Path.Combine(profileDirectory,"profile.save");InitializeTemporaryProfile();
            Assert.That(Field<string>(Field<object>(progress,"store"),"path"),Is.EqualTo(profilePath));
            yield return SetWidth(1280);
        }
        [UnityTearDown] public IEnumerator Stop()
        {
            if(ui!=null)Call(ui,"CloseModal");
            if(session!=null)session.Shutdown();
            // Remove the scene before restoring preferences: no remaining menu Update may change policy.
            if(ui!=null)Object.Destroy(ui.gameObject);
            if(progress!=null)Object.Destroy(progress);
            yield return null;
            foreach(var entry in preferences)
            {if(entry.Value.HasValue)PlayerPrefs.SetInt(entry.Key,entry.Value.Value);else PlayerPrefs.DeleteKey(entry.Key);}
            PlayerPrefs.Save();Application.targetFrameRate=oldFrameRate;QualitySettings.vSyncCount=oldVsync;
            if(profileDirectory!=null&&Directory.Exists(profileDirectory))Directory.Delete(profileDirectory,true);
        }
        IEnumerator SetWidth(int width)
        {
            var safe=Field<RectTransform>(ui,"safe");var canvas=(RectTransform)safe.parent;
            safe.anchorMin=safe.anchorMax=safe.pivot=new Vector2(.5f,.5f);safe.anchoredPosition=Vector2.zero;
            safe.sizeDelta=new Vector2(width,720);safe.localScale=Vector3.one*Mathf.Min(canvas.rect.width/width,canvas.rect.height/720)*.94f;
            Canvas.ForceUpdateCanvases();Call(ui,"Rebuild");yield return null;yield return null;Canvas.ForceUpdateCanvases();
        }
        static readonly string[] HomeButtons={"Home solo","Home lan","Home online","Home tutorial","Home settings","Home arsenal",
            "Map card 0","Home map picker","Home match rules","Solo difficulty Leicht","Solo difficulty Normal","Solo difficulty Schwer"};

        [UnityTest] public IEnumerator AnimatedHomeKeepsAllTouchTargetsFixedAndRendersRealDioramaAtBothWidths()
        {
            // This measures the base menu, not live broadcast arrivals from another test/player.
            // Discovery has separate native banner/no-autojoin coverage.
            bool sessionEnabled=session.enabled;session.enabled=false;session.LanDiscoveryEnabled=false;
            try
            {
            foreach(int width in new[]{900,1280})
            {
                yield return SetWidth(width);Assert.That(Motion.MotionAllowed,Is.True);
                Assert.That(Preview,Is.Not.Null);Preview.RefreshRenderNow();
                Assert.That(Preview.RenderPixelSize.x,Is.GreaterThan(0));Assert.That(Preview.RenderPixelSize.y,Is.GreaterThan(0));
                var positions=HomeButtons.ToDictionary(n=>n,n=>((RectTransform)Find(n).transform).anchoredPosition);
                float clock=Motion.AnimationTime;
                for(int frame=0;frame<5;frame++)
                {
                    yield return null;Canvas.ForceUpdateCanvases();
                    foreach(string name in HomeButtons)
                    {
                        var button=Find(name);var rect=(RectTransform)button.transform;
                        Assert.That(rect.rect.width,Is.GreaterThanOrEqualTo(44));Assert.That(rect.rect.height,Is.GreaterThanOrEqualTo(44));
                        Assert.That(rect.anchoredPosition,Is.EqualTo(positions[name]),"Animation moved hit rectangle: "+name);
                        foreach(var uv in new[]{new Vector2(.5f,.5f),new Vector2(.1f,.1f),new Vector2(.9f,.9f)})
                        {
                            var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.min+Vector2.Scale(rect.rect.size,uv)));
                            var hit=Hit(point);Assert.That(hit.GetComponentInParent<Button>(),Is.SameAs(button),width+" / "+name+" / hit "+hit.name+" / parent "+hit.transform.parent?.name);
                        }
                    }
                }
                Assert.That(Motion.AnimationTime,Is.GreaterThan(clock));Assert.That(session.State,Is.Null,"Diorama started a real match");
                // Keep hit tests inside the entrance, but capture its fully revealed endpoint.
                Motion.GetType().GetField("elapsed",Private).SetValue(Motion,1f);yield return null;
                Canvas.ForceUpdateCanvases();
                var title=Find("Home solo").GetComponentsInChildren<Text>().Single(t=>t.text=="JETZT SPIELEN");
                Assert.That(title.preferredWidth,Is.LessThanOrEqualTo(title.rectTransform.rect.width+.5f),"Solo CTA wraps at "+width);
                Assert.That(title.preferredHeight,Is.LessThanOrEqualTo(title.rectTransform.rect.height+.5f),"Solo CTA clips at "+width);
                Assert.That(title.cachedTextGenerator.lineCount,Is.EqualTo(1),"Solo CTA must be fully visible on one line");
                for(int map=0;map<1;map++)
                {
                    var graphic=Find("Map card "+map).GetComponentInChildren<MapThumbnailGraphic>();
                    Assert.That(graphic,Is.Not.Null);var renderer=graphic.GetComponent<CanvasRenderer>();
                    Assert.That(renderer,Is.Not.Null,"Missing route renderer for map "+map);
                    Assert.That(renderer.cull,Is.False);Assert.That(renderer.GetAlpha(),Is.GreaterThan(.9f));
                    var mesh=renderer.GetMesh();Assert.That(mesh,Is.Not.Null);Assert.That(mesh.vertexCount,Is.GreaterThan(4));
                    Assert.That(mesh.colors32.Any(c=>c.a>0),Is.True,"Transparent route mesh");
                    float area=0;var vertices=mesh.vertices;var triangles=mesh.triangles;
                    for(int t=0;t<triangles.Length;t+=3)
                        area+=Vector3.Cross(vertices[triangles[t+1]]-vertices[triangles[t]],vertices[triangles[t+2]]-vertices[triangles[t]]).magnitude*.5f;
                    Assert.That(area,Is.GreaterThan(100),"Empty or degenerate route mesh");
                    Assert.That(mesh.bounds.size.x,Is.InRange(81f,83f));Assert.That(mesh.bounds.size.y,Is.InRange(51f,53f));
                }
                Capture("home-"+width);yield return null;
            }
            }
            finally{session.enabled=sessionEnabled;}
        }

        IEnumerator ChooseMap(int index)
        {
            Tap(Find("Home map picker"));yield return null;Ui16TestNavigation.ConfirmHomeIfOpen();yield return null;yield return null;Canvas.ForceUpdateCanvases();
            Tap(Find("Map choice "+index));yield return null;yield return null;
        }
        [UnityTest] public IEnumerator AllMapsRemainReachableInNativeMapPickerAtNarrowWidth()
        {
            yield return SetWidth(900);
            for(int index=0;index<MapCatalog.Count;index++)
            {
                Tap(Find("Home map picker"));yield return null;Ui16TestNavigation.ConfirmHomeIfOpen();yield return null;yield return null;Canvas.ForceUpdateCanvases();
                for(int k=0;k<MapCatalog.Count;k++)
                {
                    var button=Find("Map choice "+k);Assert.That(Hit(Center(button)).GetComponentInParent<Button>(),Is.SameAs(button));
                    var graphic=button.GetComponentInChildren<MapThumbnailGraphic>();Assert.That(graphic,Is.Not.Null);
                    Assert.That(graphic.GetComponent<CanvasRenderer>().GetMesh().vertexCount,Is.GreaterThan(4));
                }
                if(index==0)CompactHud08Tests.CaptureHud("screens15-all-map-choices");
                Tap(Find("Map choice "+index));yield return null;yield return null;
                Assert.That(session.SelectedMapIndex,Is.EqualTo(index));Assert.That(session.Content.Map.Id,Is.EqualTo(MapCatalog.Create(index).Id));
            }
        }

        [UnityTest] public IEnumerator MapAndDifficultySelectionReachSoloWhileNetworkButtonsOnlyOpenConnectionForms()
        {
            foreach(string mode in new[]{"lan","online"})
            {
                Tap(Find("Home "+mode));yield return null;Ui16TestNavigation.ConfirmHomeIfOpen();yield return null;
                Assert.That(ui.CurrentScreen,Is.EqualTo("connect"));Assert.That(Field<string>(ui,"connectionPage"),Is.EqualTo(mode));
                Assert.That(session.State,Is.Null);Assert.That(session.Status,Is.EqualTo(ConnectionState.Idle));
                Tap(Find("Zurück"));yield return null;yield return null;
                Assert.That(ui.CurrentScreen,Is.EqualTo("home"));Assert.That(Motion,Is.Not.Null);
            }
            yield return ChooseMap(2);
            Assert.That(session.SelectedMapIndex,Is.EqualTo(2));var map=session.Content.Map;
            Tap(Find("Solo difficulty Schwer"));yield return null;
            Assert.That(session.Difficulty,Is.EqualTo(BotDifficulty.Hard));
            Tap(Find("Home solo"));yield return null;Ui16TestNavigation.ConfirmHomeIfOpen();yield return null;
            Assert.That(session.Mode,Is.EqualTo(SessionMode.Offline));Assert.That(session.State,Is.Not.Null);
            Assert.That(session.SelectedMapIndex,Is.EqualTo(2));Assert.That(session.Content.Map.Id,Is.EqualTo(map.Id));
            Assert.That(session.Difficulty,Is.EqualTo(BotDifficulty.Hard));Assert.That(ui.TutorialActive,Is.False);
            Assert.That(File.Exists(profilePath),Is.True,"Match ticket must use the temporary profile");
        }

        [UnityTest] public IEnumerator ArsenalBlocksBackgroundAndPersistsOneUnlockToAnIsolatedProfile()
        {
            var mutable=Field<object>(progress,"profile");mutable.GetType().GetField("Medals").SetValue(mutable,1000);
            Call(ui,"RefreshHomeExtras");var solo=Find("Home solo");var map=Find("Home map picker");
            Tap(Find("Home arsenal"));yield return null;Ui16TestNavigation.ConfirmHomeIfOpen();yield return null;
            var modal=Field<RectTransform>(ui,"modal");Assert.That(modal,Is.Not.Null);Assert.That(Field<bool>(ui,"homeArsenalOpen"),Is.True);
            Assert.That(Hit(Center(solo)).GetComponentInParent<Button>(),Is.Not.SameAs(solo));
            Assert.That(Hit(Center(map)).GetComponentInParent<Button>(),Is.Not.SameAs(map));
            Submit(solo);Submit(map);Assert.That(session.State,Is.Null);Assert.That(session.SelectedMapIndex,Is.EqualTo(0));
            int price=(int)progress.GetType().Assembly.GetType("DefenseDuell.Progression.ProfileRules").GetMethod("Price").Invoke(null,new object[]{5});
            var unlock=Find("Unlock tower 5");Tap(unlock);yield return null;
            Assert.That(ProfileInt(Profile,"Medals"),Is.EqualTo(1000-price));Assert.That(ProfileInt(Profile,"TowerMask")&(1<<5),Is.Not.Zero);
            Assert.That(session.LocalTowerMask,Is.EqualTo(31));Assert.That(unlock.interactable,Is.False);
            Submit(unlock);Assert.That(ProfileInt(Profile,"Medals"),Is.EqualTo(1000-price),"Owned unlock charged twice");
            Assert.That(File.Exists(profilePath),Is.True);InitializeTemporaryProfile();
            Assert.That(ProfileInt(Profile,"Medals"),Is.EqualTo(1000-price));Assert.That(ProfileInt(Profile,"TowerMask")&(1<<5),Is.Not.Zero);
            Capture("arsenal-unlocked");yield return null;Tap(Find("×"));yield return null;
            Assert.That(Field<RectTransform>(ui,"modal"),Is.Null);Assert.That(Field<bool>(ui,"homeArsenalOpen"),Is.False);
            yield return ChooseMap(1);Assert.That(session.SelectedMapIndex,Is.EqualTo(1));
        }

        Button SettingToggle(float y)=>Field<RectTransform>(ui,"modal").GetComponentsInChildren<Button>()
            .Single(b=>Mathf.Abs(((RectTransform)b.transform).anchoredPosition.y+y)<1&&((RectTransform)b.transform).rect.width==160);
        [UnityTest] public IEnumerator ReducedEffectsAndPowerSavingStopMenuMotionAcrossRebuildAndResumeWhenDisabled()
        {
            foreach(float row in new[]{373f,446f})
            {
                Tap(Find("Home settings"));yield return null;Ui16TestNavigation.ConfirmHomeIfOpen();yield return null;Tap(SettingToggle(row));Tap(Find("×"));yield return null;
                Assert.That(Motion.MotionAllowed,Is.False);Assert.That(Preview.ReducedEffects,Is.True);
                Assert.That(Preview.PowerSaving,Is.EqualTo(row==446));float stopped=Motion.AnimationTime;
                AssertPreviewPixels(row==446?640:1280);
                yield return null;yield return null;Assert.That(Motion.AnimationTime,Is.EqualTo(stopped));
                Call(ui,"Rebuild");yield return null;yield return null;
                Assert.That(Motion.MotionAllowed,Is.False);Assert.That(Preview.ReducedEffects,Is.True);
                AssertPreviewPixels(row==446?640:1280);
                stopped=Motion.AnimationTime;yield return null;Assert.That(Motion.AnimationTime,Is.EqualTo(stopped));
                Tap(Find("Home settings"));yield return null;Ui16TestNavigation.ConfirmHomeIfOpen();yield return null;Tap(SettingToggle(row));Tap(Find("×"));yield return null;
                Assert.That(Motion.MotionAllowed,Is.True);float moving=Motion.AnimationTime;
                yield return null;yield return null;Assert.That(Motion.AnimationTime,Is.GreaterThan(moving));
                AssertPreviewPixels(1280);
            }
            foreach(string callback in new[]{"OnApplicationFocus","OnApplicationPause"})
            {
                bool stoppedValue=callback=="OnApplicationPause";
                Call(Motion,callback,stoppedValue);Assert.That(Motion.MotionAllowed,Is.False);
                Assert.That(Preview.ReducedEffects,Is.True);float stopped=Motion.AnimationTime;
                yield return null;yield return null;Assert.That(Motion.AnimationTime,Is.EqualTo(stopped));
                Call(Motion,callback,!stoppedValue);Assert.That(Motion.MotionAllowed,Is.True);
                float running=Motion.AnimationTime;yield return null;yield return null;
                Assert.That(Motion.AnimationTime,Is.GreaterThan(running));
            }
            ui.enabled=false;yield return null;float disabledClock=Motion.AnimationTime;
            Assert.That(Motion.MotionAllowed,Is.False);Assert.That(Preview.ReducedEffects,Is.True);
            yield return null;yield return null;Assert.That(Motion.AnimationTime,Is.EqualTo(disabledClock));
            ui.enabled=true;yield return null;float resumedClock=Motion.AnimationTime;
            Assert.That(Motion.MotionAllowed,Is.True);yield return null;yield return null;
            Assert.That(Motion.AnimationTime,Is.GreaterThan(resumedClock));
        }

        static int OwnedTextures()=>Resources.FindObjectsOfTypeAll<RenderTexture>().Count(t=>t.name.StartsWith("Defense 3D ",StringComparison.Ordinal));
        void AssertPreviewPixels(int cap)
        {
            Assert.That(Preview.PreviewPixelCap,Is.EqualTo(1280),"Only the home hero opts into the higher preview budget");
            Preview.RefreshRenderNow();
            var canvas=Preview.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var corners=new Vector3[4];((RectTransform)Preview.transform).GetWorldCorners(corners);
            var bottomLeft=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);
            float w=Vector2.Distance(bottomLeft,RectTransformUtility.WorldToScreenPoint(camera,corners[3]));
            float h=Vector2.Distance(bottomLeft,RectTransformUtility.WorldToScreenPoint(camera,corners[1]));
            var pixels=Preview.RenderPixelSize;
            Assert.That(Mathf.Max(pixels.x,pixels.y),Is.LessThanOrEqualTo(cap));
            if(Mathf.Max(w,h)<=cap)
            {
                Assert.That(pixels.x,Is.EqualTo(Mathf.RoundToInt(w)),"Home preview undersamples visible screen pixels");
                Assert.That(pixels.y,Is.EqualTo(Mathf.RoundToInt(h)),"Home preview undersamples visible screen pixels");
            }
            else Assert.That(Mathf.Max(pixels.x,pixels.y),Is.EqualTo(cap),"Preview did not use its permitted pixel budget");
            Assert.That(pixels.x/(float)pixels.y,Is.EqualTo(w/h).Within(.025f),"Budget change distorted the preview");
        }
        [UnityTest] public IEnumerator RepeatedNavigationDisposesPreviewAndModalScopeWithoutLeakingTextures()
        {
            Preview.RefreshRenderNow();int baseline=OwnedTextures();Assert.That(baseline,Is.EqualTo(1));
            for(int cycle=0;cycle<3;cycle++)
            {
                foreach(string dialog in new[]{"Home settings","Home arsenal"})
                {
                    var oldMotion=Motion;Tap(Find(dialog));yield return null;
                    Assert.That(oldMotion.MotionAllowed,Is.False);float paused=oldMotion.AnimationTime;
                    yield return null;Assert.That(oldMotion.AnimationTime,Is.EqualTo(paused));
                    // Rebuild while a modal owns input: destroying its hierarchy must release scope.
                    Call(ui,"Rebuild");yield return null;yield return null;
                    Assert.That(oldMotion==null,Is.True);Assert.That(Field<RectTransform>(ui,"modal"),Is.Null);
                    Assert.That(ui.GetComponentsInChildren<MenuMotion>().Length,Is.EqualTo(1));
                    Assert.That(ui.GetComponentsInChildren<BoardView>().Length,Is.EqualTo(1));
                    Assert.That(Hit(Center(Find("Home solo"))).GetComponentInParent<Button>(),Is.SameAs(Find("Home solo")));
                    // A pointer purchase/navigation proves there is no stale semantic modal scope.
                    Tap(Find("Home lan"));yield return null;Ui16TestNavigation.ConfirmHomeIfOpen();yield return null;yield return null;
                    Assert.That(ui.CurrentScreen,Is.EqualTo("connect"));Assert.That(OwnedTextures(),Is.Zero);
                    Tap(Find("Zurück"));yield return null;yield return null;Preview.RefreshRenderNow();
                    Assert.That(OwnedTextures(),Is.EqualTo(baseline));Assert.That(session.Status,Is.EqualTo(ConnectionState.Idle));
                }
            }
        }

        [UnityTest] public IEnumerator AsymmetricSafeInsetsKeepNativeCanvasEdgesNavyThroughRotationModalAndHomeReturn()
        {
            const int pixelWidth=2048,pixelHeight=955,inset=83;
            var canvas=ui.GetComponentInChildren<Canvas>().rootCanvas;var safe=Field<RectTransform>(ui,"safe");
            var scaler=canvas.GetComponent<CanvasScaler>();bool scalerEnabled=scaler!=null&&scaler.enabled,uiEnabled=ui.enabled;
            var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldDistance=canvas.planeDistance,oldFactor=canvas.scaleFactor;
            var oldMin=safe.anchorMin;var oldMax=safe.anchorMax;var oldPivot=safe.pivot;
            var oldPosition=safe.anchoredPosition;var oldSize=safe.sizeDelta;var oldScale=safe.localScale;
            var oldActive=RenderTexture.active;
            var cameraObject=new GameObject("Menu asymmetric inset proof camera");var camera=cameraObject.AddComponent<Camera>();
            camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.magenta;
            camera.transform.position=new Vector3(0,0,-100);
            var target=new RenderTexture(pixelWidth,pixelHeight,24);target.Create();camera.targetTexture=target;
            try
            {
                // Synthetic device insets must survive frames without GameUI.ApplySafeArea replacing
                // them with the test runner's host window. Child graphics and EventSystem stay active.
                ui.enabled=false;if(scaler!=null)scaler.enabled=false;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
                canvas.scaleFactor=pixelHeight/720f;safe.localScale=Vector3.one;
                foreach(bool left in new[]{true,false})
                {
                    safe.anchorMin=new Vector2(left?inset/(float)pixelWidth:0,0);
                    safe.anchorMax=new Vector2(left?1:1-inset/(float)pixelWidth,1);
                    safe.offsetMin=safe.offsetMax=Vector2.zero;
                    Canvas.ForceUpdateCanvases();Call(ui,"Rebuild");yield return null;yield return null;Canvas.ForceUpdateCanvases();
                    var canvasRect=(RectTransform)canvas.transform;
                    Assert.That(canvasRect.rect.width,Is.EqualTo(pixelWidth*720f/pixelHeight).Within(.1f));
                    Assert.That(canvasRect.rect.height,Is.EqualTo(720).Within(.1f));
                    Assert.That(safe.rect.width,Is.EqualTo((pixelWidth-inset)*720f/pixelHeight).Within(.1f));
                    Assert.That(camera.pixelWidth,Is.EqualTo(pixelWidth));Assert.That(camera.pixelHeight,Is.EqualTo(pixelHeight));
                    AssertInsetTargets(camera,left,inset,pixelWidth,pixelHeight);
                    CaptureInset(camera,target,left,inset,"home");

                    TapWithCamera(Find("Home settings"),camera);yield return null;Ui16TestNavigation.ConfirmHomeIfOpen();yield return null;
                    Assert.That(Field<RectTransform>(ui,"modal"),Is.Not.Null);
                    Canvas.ForceUpdateCanvases();
                    foreach(var slider in Field<RectTransform>(ui,"modal").GetComponentsInChildren<Slider>())
                    {
                        float saved=slider.value,previousX=float.NegativeInfinity;
                        foreach(float value in new[]{0f,.5f,1f})
                        {
                            slider.SetValueWithoutNotify(value);Canvas.ForceUpdateCanvases();
                            Assert.That(slider.handleRect.rect.height,Is.InRange(44f,64f),"Volume handle must fit its64px row");
                            Assert.That(slider.handleRect.rect.width,Is.InRange(44f,64f));
                            float x=ScreenPoint(slider.handleRect,camera).x;
                            Assert.That(x,Is.GreaterThan(previousX),"Volume handle must move across its track");previousX=x;
                        }
                        slider.SetValueWithoutNotify(saved);
                    }
                    var solo=Find("Home solo");var soloPoint=ScreenPoint(solo,camera);
                    Assert.That(Hit(soloPoint).GetComponentInParent<Button>(),Is.Not.SameAs(solo));
                    Submit(solo);Assert.That(session.State,Is.Null,"Modal leaked a background Solo submit");
                    CaptureInset(camera,target,left,inset,"settings");

                    Call(ui,"Rebuild");yield return null;yield return null;
                    Assert.That(Field<RectTransform>(ui,"modal"),Is.Null);
                    AssertInsetTargets(camera,left,inset,pixelWidth,pixelHeight);
                    CaptureInset(camera,target,left,inset,"rebuilt");
                    TapWithCamera(Find("Home lan"),camera);yield return null;Ui16TestNavigation.ConfirmHomeIfOpen();yield return null;
                    Assert.That(ui.CurrentScreen,Is.EqualTo("connect"));Assert.That(session.Status,Is.EqualTo(ConnectionState.Idle));
                    TapWithCamera(Find("Zurück"),camera);yield return null;yield return null;
                    Assert.That(ui.CurrentScreen,Is.EqualTo("home"));Assert.That(session.State,Is.Null);
                    AssertInsetTargets(camera,left,inset,pixelWidth,pixelHeight);
                    CaptureInset(camera,target,left,inset,"returned");
                }
            }
            finally
            {
                Call(ui,"CloseModal");RenderTexture.active=oldActive;
                canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;canvas.scaleFactor=oldFactor;
                safe.anchorMin=oldMin;safe.anchorMax=oldMax;safe.pivot=oldPivot;
                safe.sizeDelta=oldSize;safe.anchoredPosition=oldPosition;safe.localScale=oldScale;
                if(scaler!=null)scaler.enabled=scalerEnabled;camera.targetTexture=null;
                target.Release();Object.Destroy(target);Object.Destroy(cameraObject);
                Canvas.ForceUpdateCanvases();Call(ui,"Rebuild");ui.enabled=uiEnabled;
            }
        }
        static Vector2 ScreenPoint(Component component,Camera camera)
        {var rect=(RectTransform)component.transform;return RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));}
        static void TapWithCamera(Button button,Camera camera)
        {
            Assert.That(button,Is.InstanceOf<PointerSafeButton>());Assert.That(button.interactable,Is.True);
            var position=ScreenPoint(button,camera);var hit=Hit(position);
            Assert.That(hit.GetComponentInParent<Button>(),Is.SameAs(button),"Covered native-camera button: "+button.name);
            var e=new PointerEventData(EventSystem.current){position=position,pressPosition=position,pointerId=1213,
                button=PointerEventData.InputButton.Left,eligibleForClick=true};
            ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerClickHandler);
        }
        void AssertInsetTargets(Camera camera,bool left,int inset,int pixelWidth,int pixelHeight)
        {
            Canvas.ForceUpdateCanvases();var corners=new Vector3[4];
            foreach(string name in HomeButtons)
            {
                var button=Find(name);var rect=(RectTransform)button.transform;rect.GetWorldCorners(corners);
                Assert.That(button,Is.InstanceOf<PointerSafeButton>());
                Assert.That(rect.rect.width,Is.GreaterThanOrEqualTo(44));Assert.That(rect.rect.height,Is.GreaterThanOrEqualTo(44));
                foreach(var corner in corners)
                {
                    var point=RectTransformUtility.WorldToScreenPoint(camera,corner);
                    Assert.That(point.x,Is.InRange(left?inset-.5f:-.5f,left?pixelWidth+.5f:pixelWidth-inset+.5f),name+" outside safe width");
                    Assert.That(point.y,Is.InRange(-.5f,pixelHeight+.5f),name+" outside safe height");
                }
                foreach(var uv in new[]{new Vector2(.5f,.5f),new Vector2(.1f,.1f),new Vector2(.9f,.9f)})
                {
                    var point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.min+Vector2.Scale(rect.rect.size,uv)));
                    Assert.That(Hit(point).GetComponentInParent<Button>(),Is.SameAs(button),"Native inset raycast: "+name);
                }
            }
        }
        [Serializable] sealed class InsetCaptureInfo
        {
            public int pixelWidth,pixelHeight,insetLeft,insetRight,checkedBorderPixels,menuRenderPixelWidth,menuRenderPixelHeight;
            public float canvasLogicalWidth,canvasLogicalHeight,safeLogicalWidth,safeLogicalHeight,canvasScaleFactor;
            public string stage,expectedBorder;
            public bool checkedEntirePerimeter;
        }
        void CaptureInset(Camera camera,RenderTexture target,bool left,int inset,string stage)
        {
            Canvas.ForceUpdateCanvases();foreach(var board in ui.GetComponentsInChildren<BoardView>())board.RefreshRenderNow();
            var previous=RenderTexture.active;Texture2D image=null;
            try
            {
                camera.Render();RenderTexture.active=target;image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                var pixels=image.GetPixels32();string mismatch=null;int checkedPixels=0;
                bool checkEntirePerimeter=Field<RectTransform>(ui,"modal")==null;
                for(int y=0;y<target.height;y++)for(int x=0;x<target.width;x++)
                {
                    // The excluded strip must remain navy even while a modal dims the safe area.
                    // Only without that intentional dimmer must all four outer edges also be navy.
                    bool border=left?x<inset:x>=target.width-inset;
                    bool perimeter=x==0||x==target.width-1||y==0||y==target.height-1;
                    if(!border&&!(checkEntirePerimeter&&perimeter))continue;
                    checkedPixels++;var c=pixels[y*target.width+x];
                    if(mismatch==null&&(Math.Abs(c.r-9)>2||Math.Abs(c.g-27)>2||Math.Abs(c.b-43)>2))
                        mismatch="Native pixel ("+x+","+y+") = RGB("+c.r+","+c.g+","+c.b+")";
                }
                var folder=Path.GetFullPath("TestResults/Menu-v15");Directory.CreateDirectory(folder);
                string stem=Path.Combine(folder,"inset-"+(left?"left":"right")+"-"+inset+"-"+stage+"-pixels"+target.width+"x"+target.height);
                File.WriteAllBytes(stem+".png",image.EncodeToPNG());
                var canvas=ui.GetComponentInChildren<Canvas>().rootCanvas;var root=(RectTransform)canvas.transform;var safe=Field<RectTransform>(ui,"safe");
                File.WriteAllText(stem+".json",JsonUtility.ToJson(new InsetCaptureInfo{pixelWidth=target.width,pixelHeight=target.height,
                    insetLeft=left?inset:0,insetRight=left?0:inset,checkedBorderPixels=checkedPixels,checkedEntirePerimeter=checkEntirePerimeter,stage=stage,expectedBorder="#091B2B ±2 RGB bytes",
                    canvasLogicalWidth=root.rect.width,canvasLogicalHeight=root.rect.height,safeLogicalWidth=safe.rect.width,safeLogicalHeight=safe.rect.height,
                    canvasScaleFactor=canvas.scaleFactor,menuRenderPixelWidth=Preview.RenderPixelSize.x,menuRenderPixelHeight=Preview.RenderPixelSize.y},true));
                Assert.That(checkedPixels,Is.GreaterThanOrEqualTo(inset*target.height));
                Assert.That(mismatch,Is.Null,(left?"Left":"Right")+" inset / "+stage+": full canvas background must be navy; "+mismatch);
            }
            finally{RenderTexture.active=previous;if(image!=null)Object.Destroy(image);}
        }

        [Serializable] sealed class CaptureInfo
        {
            public int logicalWidth,logicalHeight,pixelWidth,pixelHeight,menuRenderPixelWidth,menuRenderPixelHeight;
            public float actualCanvasWidth,actualCanvasHeight;
        }
        void Capture(string name)
        {
            var canvas=ui.GetComponentInChildren<Canvas>().rootCanvas;var safe=Field<RectTransform>(ui,"safe");
            int w=Mathf.RoundToInt(safe.rect.width),h=Mathf.RoundToInt(safe.rect.height),pw=Mathf.RoundToInt(w*1.5f),ph=Mathf.RoundToInt(h*1.5f);
            var scale=safe.localScale;float factor=canvas.scaleFactor,distance=canvas.planeDistance;
            var scaler=canvas.GetComponent<CanvasScaler>();bool enabled=scaler!=null&&scaler.enabled;
            var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;var active=RenderTexture.active;
            var go=new GameObject("Menu v12 proof camera");var camera=go.AddComponent<Camera>();camera.orthographic=true;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.transform.position=new Vector3(0,0,-100);
            var target=new RenderTexture(pw,ph,24);target.Create();camera.targetTexture=target;Texture2D image=null;
            try
            {
                if(scaler!=null)scaler.enabled=false;canvas.renderMode=RenderMode.ScreenSpaceCamera;
                canvas.worldCamera=camera;canvas.planeDistance=10;canvas.scaleFactor=1.5f;safe.localScale=Vector3.one;Canvas.ForceUpdateCanvases();
                var rect=(RectTransform)canvas.transform;Assert.That(rect.rect.width,Is.EqualTo(w).Within(1));Assert.That(rect.rect.height,Is.EqualTo(h).Within(1));
                foreach(var board in ui.GetComponentsInChildren<BoardView>())board.RefreshRenderNow();
                AssertPreviewPixels(1280);Assert.That(Preview.RenderPixelSize.x,Is.GreaterThan(640),"PNG resolution alone is not proof of a sharper diorama");
                camera.Render();RenderTexture.active=target;
                image=new Texture2D(pw,ph,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,pw,ph),0,0);image.Apply();
                var folder=Path.GetFullPath("TestResults/Menu-v15");Directory.CreateDirectory(folder);
                string stem=Path.Combine(folder,name+"-logical"+w+"x"+h+"-pixels"+pw+"x"+ph);
                File.WriteAllBytes(stem+".png",image.EncodeToPNG());
                File.WriteAllText(stem+".json",JsonUtility.ToJson(new CaptureInfo{logicalWidth=w,logicalHeight=h,pixelWidth=pw,pixelHeight=ph,
                    actualCanvasWidth=rect.rect.width,actualCanvasHeight=rect.rect.height,menuRenderPixelWidth=Preview.RenderPixelSize.x,menuRenderPixelHeight=Preview.RenderPixelSize.y},true));
            }
            finally
            {
                RenderTexture.active=active;canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=distance;
                canvas.scaleFactor=factor;safe.localScale=scale;if(scaler!=null)scaler.enabled=enabled;
                camera.targetTexture=null;if(image!=null)Object.Destroy(image);target.Release();Object.Destroy(target);Object.Destroy(go);
                Canvas.ForceUpdateCanvases();foreach(var board in ui.GetComponentsInChildren<BoardView>())board.RefreshRenderNow();
            }
        }
    }
}
