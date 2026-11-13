#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DefenseDuell.Intro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DefenseDuell.Intro.Tests
{
    public sealed class StudioIntroNativeTests
    {
        [Test] public void SkipStartsAfterVisibleTimeAndPausesDoNotCount()
        {
            var gate = new StudioIntroGate();gate.Advance(3,false);Assert.That(gate.TrySkip(true),Is.False);
            gate.FrameDecoded();gate.Advance(1.49,false);Assert.That(gate.TrySkip(true),Is.False);
            gate.Advance(100,true);Assert.That(gate.TrySkip(true),Is.False);
            gate.FrameDecoded();gate.Advance(.01,false);Assert.That(gate.TrySkip(true),Is.True);
            Assert.That(gate.Exit(IntroExitReason.Completed),Is.False);
        }
        [Test] public void PreparationAndDecoderTimeoutsHaveFallbacks()
        {
            var gate = new StudioIntroGate();gate.Advance(8,false);Assert.That(gate.ExitReason,Is.EqualTo(IntroExitReason.PrepareTimeout));
            gate = new StudioIntroGate();gate.FrameDecoded();gate.Advance(5,false);Assert.That(gate.ExitReason,Is.EqualTo(IntroExitReason.PlaybackTimeout));
        }
        [TestCase("--duel-host")][TestCase("--duel-client")][TestCase("--duel-relay-host")][TestCase("--duel-relay-client")]
        public void AcceptanceProcessesBypassIntro(string argument)
        {Assert.That(StudioIntroGate.IsAutomationArgument(new[]{argument}),Is.True);}

        // Run with graphics, not -nographics. This exercises the supplied MP4 decoder,
        // including pause/resume, first-frame delivery and natural ending.
        [UnityTest] public IEnumerator SuppliedClipPreparesDecodesPausesAndEndsOnce()
        {
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Art/StudioIntro/soballa_studio_intro.mp4");
            Assert.That(clip,Is.Not.Null);Assert.That(clip.length,Is.EqualTo(4.2).Within(.08));
            var go = new GameObject("Intro decoder proof",typeof(VideoPlayer));var player = go.GetComponent<VideoPlayer>();
            var target = new RenderTexture(720,406,0);target.Create();int frames=0,ended=0;string error=null;bool varied=false;
            Texture2D pixels=null;var prior=RenderTexture.active;
            try
            {
                player.playOnAwake=false;player.isLooping=false;player.clip=clip;player.source=VideoSource.VideoClip;
                player.audioOutputMode=VideoAudioOutputMode.None;player.renderMode=VideoRenderMode.RenderTexture;player.targetTexture=target;player.sendFrameReadyEvents=true;
                player.frameReady+=(v,f)=>frames++;player.loopPointReached+=v=>ended++;player.errorReceived+=(v,e)=>error=e;
                player.Prepare();double limit=Time.realtimeSinceStartupAsDouble+12;
                while(!player.isPrepared&&error==null&&Time.realtimeSinceStartupAsDouble<limit)yield return null;
                Assert.That(error,Is.Null);Assert.That(player.isPrepared,Is.True);player.Play();
                while(frames<2&&error==null&&Time.realtimeSinceStartupAsDouble<limit)yield return null;
                Assert.That(frames,Is.GreaterThan(0));player.Pause();yield return null;yield return null;long pausedFrame=player.frame;
                yield return new WaitForSecondsRealtime(.15f);Assert.That(player.frame,Is.InRange(pausedFrame,pausedFrame+1));player.Play();
                pixels=new Texture2D(720,406,TextureFormat.RGB24,false);
                while(ended==0&&error==null&&Time.realtimeSinceStartupAsDouble<limit)
                {
                    if(!varied&&player.time>1.6&&player.time<3.5)
                    {
                        RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,406),0,0);pixels.Apply();
                        var center=pixels.GetPixel(360,200);var edge=pixels.GetPixel(10,10);
                        varied=Mathf.Abs(center.r-edge.r)+Mathf.Abs(center.g-edge.g)+Mathf.Abs(center.b-edge.b)>.08f;
                        RenderTexture.active=prior;
                    }
                    yield return null;
                }
                Assert.That(error,Is.Null);Assert.That(ended,Is.EqualTo(1));Assert.That(varied,Is.True,"Decoded image must contain the actual intro, not just a black target");
            }
            finally
            {
                RenderTexture.active=prior;player.Stop();player.targetTexture=null;target.Release();
                Object.Destroy(target);Object.Destroy(pixels);Object.Destroy(go);
            }
            yield return null;
        }

        StudioIntroController scenarioIntro;
        [Serializable] sealed class TransitionProof
        {
            public string scenario, exitReason, activeScene, captureScope="Native video RenderTexture frame; not a full UI screenshot";
            public bool bootLoaded, overlayDestroyed, videoTargetDestroyed, homeButtonRaycastReached;
            public float coverAlphaAtActivation;
            public int bootLoads;
        }
        static T Hidden<T>(object target,string name)=>(T)target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
        static IEnumerator Until(Func<bool> ready,string problem,double seconds=20)
        {
            double limit=Time.realtimeSinceStartupAsDouble+seconds;
            while(!ready()&&Time.realtimeSinceStartupAsDouble<limit)yield return null;
            Assert.That(ready(),Is.True,problem);
        }
        static void CaptureDecodedFrame(RenderTexture texture,string name)
        {
            Assert.That(texture,Is.Not.Null);var previous=RenderTexture.active;Texture2D image=null;
            try
            {
                RenderTexture.active=texture;image=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);image.Apply();
                Directory.CreateDirectory("TestResults/Intro-v141");File.WriteAllBytes("TestResults/Intro-v141/"+name+"-video-frame.png",image.EncodeToPNG());
            }
            finally{RenderTexture.active=previous;Object.Destroy(image);}
        }
        static bool AssertUsableHome()
        {
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("Boot"));
            Assert.That(EventSystem.current,Is.Not.Null,"Boot must own a working EventSystem after intro cleanup");
            var go=GameObject.Find("Home solo");Assert.That(go,Is.Not.Null,"Boot initialized the real home screen");
            var button=go.GetComponent<Button>();Assert.That(button,Is.Not.Null);Assert.That(button.IsInteractable(),Is.True);
            Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;var canvas=button.GetComponentInParent<Canvas>();
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            Assert.That(hits,Is.Not.Empty);Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(button.gameObject),"No leftover intro blocker may cover the real home button");
            return true;
        }
        IEnumerator RealSceneTransition(string scenario)
        {
            Assert.That(Application.CanStreamedLevelBeLoaded("StudioIntro"),Is.True,"Root must prepare [StudioIntro, Boot] first");
            var proof=new TransitionProof{scenario=scenario};RenderTexture videoTarget=null;
            UnityEngine.Events.UnityAction<Scene,LoadSceneMode> loaded=(scene,mode)=>
            {
                if(scene.name=="StudioIntro")
                {
                    foreach(var root in scene.GetRootGameObjects())
                    {
                        var candidate=root.GetComponentInChildren<StudioIntroController>();if(candidate==null)continue;
                        scenarioIntro=candidate;candidate.SkipInBatchMode=false;
                        if(scenario=="missing-clip")candidate.Clip=null;
                    }
                }
                if(scene.name=="Boot")
                {
                    proof.bootLoaded=true;proof.bootLoads++;
                    if(scenarioIntro!=null){proof.exitReason=scenario=="deactivate-resume"?"ExternallyDeactivated":scenarioIntro.ExitReason.ToString();proof.coverAlphaAtActivation=scenarioIntro.BlackFade.alpha;}
                }
            };
            SceneManager.sceneLoaded+=loaded;
            try
            {
                if(scenario=="missing-clip")LogAssert.Expect(LogType.Warning,"Studio intro unavailable; continuing to menu.");
                yield return SceneManager.LoadSceneAsync("StudioIntro",LoadSceneMode.Single);
                Assert.That(scenarioIntro,Is.Not.Null,"Callback must configure actual intro before Start");
                if(scenario!="missing-clip")
                {
                    yield return Until(()=>scenarioIntro!=null&&Hidden<StudioIntroGate>(scenarioIntro,"gate")?.HasFrame==true,"Intro did not decode first frame");
                    var gate=Hidden<StudioIntroGate>(scenarioIntro,"gate");var player=scenarioIntro.GetComponent<VideoPlayer>();videoTarget=player.targetTexture;
                    Assert.That(videoTarget,Is.Not.Null);scenarioIntro.RequestSkip();Assert.That(gate.WantsExit,Is.False,"Tap before1.5 visible seconds is ignored");
                    if(scenario=="skip-after-pause")
                    {
                        scenarioIntro.SendMessage("OnApplicationPause",true,SendMessageOptions.RequireReceiver);
                        double visible=gate.VisibleSeconds;yield return new WaitForSecondsRealtime(.25f);
                        Assert.That(gate.VisibleSeconds,Is.EqualTo(visible).Within(.005));scenarioIntro.RequestSkip();Assert.That(gate.WantsExit,Is.False);
                        scenarioIntro.SendMessage("OnApplicationPause",false,SendMessageOptions.RequireReceiver);
                    }
                    if(scenario=="deactivate-resume")
                    {
                        scenarioIntro.SendMessage("OnApplicationPause",true,SendMessageOptions.RequireReceiver);
                        var held=Hidden<AsyncOperation>(scenarioIntro,"preload");
                        yield return Until(()=>held.progress>=.9f,"Scene preload did not reach activation gate");
                        Assert.That(held.allowSceneActivation,Is.False);Assert.That(proof.bootLoaded,Is.False);
                        CaptureDecodedFrame(videoTarget,scenario);proof.exitReason="ExternallyDeactivated";
                        var introRoot=scenarioIntro.gameObject;introRoot.SetActive(false);
                        Assert.That(held.allowSceneActivation,Is.True,"Disabling must release Unity's global scene queue");
                        Assert.That(player.targetTexture,Is.Null,"Disabling must stop and detach decoder target immediately");
                        introRoot.SetActive(true); // Must not replay or start a second Boot load.
                    }
                    else
                    {
                        double captureAt=scenario=="natural-end"?2.0:1.6;
                        yield return Until(()=>scenarioIntro!=null&&gate.VisibleSeconds>=captureAt,"Intro ended before skip/capture gate");
                        var preload=Hidden<AsyncOperation>(scenarioIntro,"preload");Assert.That(preload.allowSceneActivation,Is.False);
                        Assert.That(proof.bootLoaded,Is.False,"Boot may preload but must not activate behind visible intro");
                        CaptureDecodedFrame(videoTarget,scenario);
                        if(scenario=="skip-after-pause")
                        {scenarioIntro.RequestSkip();scenarioIntro.RequestSkip();Assert.That(gate.ExitReason,Is.EqualTo(IntroExitReason.Skipped));}
                    }
                }
                yield return Until(()=>proof.bootLoaded&&scenarioIntro==null,"Intro did not enter Boot and destroy its overlay",30);
                yield return null;yield return null;
                Assert.That(proof.bootLoads,Is.EqualTo(1));if(scenario!="deactivate-resume")Assert.That(proof.coverAlphaAtActivation,Is.EqualTo(1).Within(.005));
                var expected=scenario=="natural-end"?IntroExitReason.Completed:scenario=="missing-clip"?IntroExitReason.MissingClip:IntroExitReason.Skipped;
                Assert.That(proof.exitReason,Is.EqualTo(scenario=="deactivate-resume"?"ExternallyDeactivated":expected.ToString()));Assert.That(videoTarget==null,Is.True,"Owned video RenderTexture must be destroyed");
                Assert.That(Object.FindFirstObjectByType<StudioIntroController>(),Is.Null);Assert.That(GameObject.Find("Soballa Studio Intro"),Is.Null);
                proof.overlayDestroyed=true;proof.videoTargetDestroyed=videoTarget==null;proof.activeScene=SceneManager.GetActiveScene().name;
                proof.homeButtonRaycastReached=AssertUsableHome();Directory.CreateDirectory("TestResults/Intro-v141");
                File.WriteAllText("TestResults/Intro-v141/"+scenario+"-transition.json",JsonUtility.ToJson(proof,true));
            }
            finally{SceneManager.sceneLoaded-=loaded;}
        }
        [UnityTest] public IEnumerator ActualIntroNaturallyEndsIntoUsableBootAndReleasesOwnedResources()
        {yield return RealSceneTransition("natural-end");}
        [UnityTest] public IEnumerator ActualIntroIgnoresEarlySkipPausesTimerThenSkipsIntoUsableBoot()
        {yield return RealSceneTransition("skip-after-pause");}
        [UnityTest] public IEnumerator ActualIntroWithMissingClipFallsBackToUsableBoot()
        {yield return RealSceneTransition("missing-clip");}
        [UnityTest] public IEnumerator ActualIntroDeactivationReactivationReleasesPreloadAndDoesNotReplay()
        {yield return RealSceneTransition("deactivate-resume");}
        [UnityTearDown] public IEnumerator CleanSceneAfterTransitionScenario()
        {
            if(scenarioIntro!=null)Object.Destroy(scenarioIntro.gameObject);
            scenarioIntro=null;yield return null;
            // A failed scene preload cannot be cancelled; OnDestroy releases its .9 lock.
            // A final ordered Boot load isolates the next fixture without touching user apps.
            if(Application.CanStreamedLevelBeLoaded("Boot"))yield return SceneManager.LoadSceneAsync("Boot",LoadSceneMode.Single);
            yield return null;
        }
    }
}

#endif
