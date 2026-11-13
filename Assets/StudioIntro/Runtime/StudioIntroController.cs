using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace DefenseDuell.Intro
{
    [DisallowMultipleComponent, RequireComponent(typeof(VideoPlayer))]
    public sealed class StudioIntroController : MonoBehaviour
    {
        [Header("Scene and supplied video")]
        public string NextScene = "Boot";
        public VideoClip Clip;
        public RawImage VideoSurface;
        public CanvasGroup BlackFade;
        public Graphic Backdrop;
        public Text SkipHint;
        [Header("Presentation")]
        public bool AllowTapToSkip = true;
        [Min(0)] public float SkipAfterSeconds = 1.5f;
        [Min(0)] public float FadeSeconds = .25f;
        [Min(1)] public float PrepareTimeoutSeconds = 8;
        [Min(1)] public float StallTimeoutSeconds = 5;
        [Min(1)] public float MaximumPlaybackSeconds = 15;
        public bool SkipInBatchMode = true;

        public IntroExitReason ExitReason => gate == null ? IntroExitReason.None : gate.ExitReason;
        public bool TransitionStarted { get; private set; }
        public bool CanSkip => gate != null && !Paused && gate.CanSkip(AllowTapToSkip);
        VideoPlayer player;
        RenderTexture ownedTexture;
        AsyncOperation preload;
        StudioIntroGate gate;
        bool applicationPaused, applicationFocused = true, playbackRequested, quitting, failed, bypass, resourcesReleased;
        bool Paused => applicationPaused || (!Application.isBatchMode && !applicationFocused);
        double lastTick;

        IEnumerator Start()
        {
            // The same opaque overlay remains alive through scene activation and menu Start().
            string introSceneName = gameObject.scene.name;
            DontDestroyOnLoad(gameObject);
            gate = new StudioIntroGate(SkipAfterSeconds, PrepareTimeoutSeconds, StallTimeoutSeconds, MaximumPlaybackSeconds);
            player = GetComponent<VideoPlayer>(); player.playOnAwake = false; player.isLooping = false;
            player.audioOutputMode = VideoAudioOutputMode.None; // Supplied animation has no audio track.
            lastTick = Time.realtimeSinceStartupAsDouble;
            if (VideoSurface != null) VideoSurface.enabled = false;
            if (SkipHint != null) SkipHint.enabled = false;
            if (BlackFade != null) { BlackFade.alpha = 1; BlackFade.blocksRaycasts = true; }
            bypass = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                StudioIntroGate.IsAutomationArgument(Environment.GetCommandLineArgs()) || (SkipInBatchMode && Application.isBatchMode);
            if (string.IsNullOrWhiteSpace(NextScene) || NextScene == introSceneName || !Application.CanStreamedLevelBeLoaded(NextScene))
            {
                FailConfiguration("Intro target scene is not in Build Settings: " + NextScene); yield break;
            }
            try { preload = SceneManager.LoadSceneAsync(NextScene, LoadSceneMode.Single); }
            catch (Exception error) { FailConfiguration("Intro scene preload failed: " + error.Message); }
            if (preload == null) { if (!failed) FailConfiguration("Intro scene preload returned no operation."); yield break; }
            preload.allowSceneActivation = false;
            if (bypass) gate.Exit(IntroExitReason.Automation);
            else if (Clip == null || VideoSurface == null || BlackFade == null)
            {
                Debug.LogWarning("Studio intro unavailable; continuing to menu."); gate.Exit(IntroExitReason.MissingClip);
            }
            else
            {
                try { ConfigureVideo(); }
                catch (Exception error) { VideoFailed(player, error.Message); }
            }
            while (!gate.HasFrame && !gate.WantsExit) yield return null;
            if (gate.HasFrame && !gate.WantsExit) yield return FadeTo(0, Mathf.Min(.15f, FadeSeconds));
            while (!gate.WantsExit) yield return null;
            TransitionStarted = true;
            if (SkipHint != null) SkipHint.enabled = false;
            yield return FadeTo(1, bypass ? 0 : FadeSeconds);
            playbackRequested = false; if (player != null) player.Stop();
            // Never wait for isDone with activation locked: Unity intentionally stops at .9.
            while (preload.progress < .9f) yield return null;
            preload.allowSceneActivation = true;
            while (!preload.isDone) yield return null;
            // Allow Boot's existing GameBootstrap and GameUI to initialize under black.
            yield return null; yield return null;
            if (VideoSurface != null) VideoSurface.enabled = false;
            if (Backdrop != null) Backdrop.enabled = false;
            yield return FadeTo(0, bypass ? 0 : FadeSeconds);
            if (BlackFade != null) BlackFade.blocksRaycasts = false;
            Destroy(gameObject);
        }
        void ConfigureVideo()
        {
            int width = Mathf.Max(2, (int)Clip.width), height = Mathf.Max(2, (int)Clip.height);
            float scale = Mathf.Min(1, Mathf.Min(1920f / width, 1080f / height));
            ownedTexture = new RenderTexture(Mathf.Max(2, Mathf.RoundToInt(width * scale)), Mathf.Max(2, Mathf.RoundToInt(height * scale)), 0, RenderTextureFormat.ARGB32)
            { name = "Studio intro video target", useMipMap = false, autoGenerateMips = false, antiAliasing = 1 };
            ownedTexture.Create(); VideoSurface.texture = ownedTexture;
            var aspect = VideoSurface.GetComponent<AspectRatioFitter>();
            if (aspect != null) { aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio = width / (float)height; }
            player.source = VideoSource.VideoClip; player.clip = Clip; player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = ownedTexture; player.aspectRatio = VideoAspectRatio.FitInside; player.waitForFirstFrame = true;
            player.sendFrameReadyEvents = true;
            player.prepareCompleted += Prepared; player.frameReady += FrameReady; player.loopPointReached += Ended; player.errorReceived += VideoFailed;
            player.Prepare();
        }
        void Update()
        {
            if (gate == null || failed) return;
            double now = Time.realtimeSinceStartupAsDouble; double delta = Math.Max(0, now - lastTick); lastTick = now;
            gate.Advance(delta, Paused);
            if (SkipHint != null) SkipHint.enabled = CanSkip && !TransitionStarted;
            if (CanSkip && (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true ||
                Mouse.current?.leftButton.wasPressedThisFrame == true || Keyboard.current?.spaceKey.wasPressedThisFrame == true ||
                Keyboard.current?.escapeKey.wasPressedThisFrame == true)) RequestSkip();
        }
        public void RequestSkip() { if (!Paused) gate?.TrySkip(AllowTapToSkip); }
        void Prepared(VideoPlayer source)
        {
            if (gate.WantsExit) return; playbackRequested = true;
            if (!Paused) source.Play();
        }
        void FrameReady(VideoPlayer source, long frame)
        {
            if (gate.WantsExit) return;
            if (!gate.HasFrame) lastTick = Time.realtimeSinceStartupAsDouble;
            gate.FrameDecoded(); VideoSurface.enabled = true;
        }
        void Ended(VideoPlayer source) { gate?.Exit(IntroExitReason.Completed); }
        void VideoFailed(VideoPlayer source, string error)
        { Debug.LogWarning("Studio intro playback failed; continuing to menu: " + error); gate?.Exit(IntroExitReason.VideoError); }
        IEnumerator FadeTo(float target, float seconds)
        {
            if (BlackFade == null) yield break;
            float from = BlackFade.alpha;
            if (seconds <= 0) { BlackFade.alpha = target; yield break; }
            float elapsed = 0;
            while (elapsed < seconds)
            {
                if (!Paused) { elapsed += Time.unscaledDeltaTime; BlackFade.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(elapsed / seconds)); }
                yield return null;
            }
            BlackFade.alpha = target;
        }
        void OnApplicationPause(bool paused) { applicationPaused = paused; UpdatePause(); }
        void OnApplicationFocus(bool focused) { applicationFocused = focused; UpdatePause(); }
        void UpdatePause()
        {
            lastTick = Time.realtimeSinceStartupAsDouble;
            if (player == null || gate == null || gate.WantsExit || !playbackRequested) return;
            if (Paused) player.Pause(); else player.Play();
        }
        void FailConfiguration(string message)
        {
            failed = true; Debug.LogError(message);
            if (BlackFade != null) BlackFade.alpha = 0;
            if (SkipHint != null) { SkipHint.text = "Das Menü konnte nicht geladen werden."; SkipHint.enabled = true; }
        }
        void OnApplicationQuit() { quitting = true; }
        void OnDisable()
        {
            // Start is not restarted after re-enabling an already-started component.
            // Release the scene queue even if disabling stopped our startup coroutine.
            // A never-started component (including Editor scene setup) owns nothing.
            if (gate == null || resourcesReleased) return;
            StopAllCoroutines();
            ReleaseResources();
            if (!quitting) Destroy(gameObject);
        }
        void OnDestroy() { ReleaseResources(); }
        void ReleaseResources()
        {
            if (resourcesReleased) return;
            resourcesReleased = true; playbackRequested = false;
            if (player != null)
            {
                player.prepareCompleted -= Prepared; player.frameReady -= FrameReady;
                player.loopPointReached -= Ended; player.errorReceived -= VideoFailed;
                player.Stop(); player.targetTexture = null;
            }
            if (VideoSurface != null) VideoSurface.texture = null;
            if (ownedTexture != null) { ownedTexture.Release(); Destroy(ownedTexture); ownedTexture = null; }
            // Unity cannot cancel a scene preload; do not leave its global async queue held at .9.
            if (!quitting && preload != null && !preload.isDone) preload.allowSceneActivation = true;
        }
    }
}
