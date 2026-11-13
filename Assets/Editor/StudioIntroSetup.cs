using System.IO;
using DefenseDuell.Intro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace DefenseDuell.Editor
{
    // Called by Root's Prepare AFTER Boot exists. Does not modify Boot or build order itself.
    public static class StudioIntroSetup
    {
        public const string ScenePath = "Assets/Scenes/StudioIntro.unity";
        public const string ClipPath = "Assets/Art/StudioIntro/soballa_studio_intro.mp4";
        [MenuItem("Defense Duell/Intro/Prepare studio intro scene")]
        public static void EnsureScene()
        {
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(ClipPath);
            if (clip == null) throw new BuildFailedException("Studio intro VideoClip missing: " + ClipPath);
            if (File.Exists(ScenePath)) return;
            Directory.CreateDirectory("Assets/Scenes");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var root = new GameObject("Soballa Studio Intro", typeof(VideoPlayer), typeof(StudioIntroController));
                var cameraObject = new GameObject("Intro background camera", typeof(Camera)); cameraObject.transform.SetParent(root.transform,false);
                var camera = cameraObject.GetComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.cullingMask = 0; camera.depth = -100;
                var intro = root.GetComponent<StudioIntroController>();intro.Clip = clip;intro.NextScene = "Boot";
                var player = root.GetComponent<VideoPlayer>();player.playOnAwake = false;player.isLooping = false;player.audioOutputMode = VideoAudioOutputMode.None;
                var canvasObject = new GameObject("IntroCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(root.transform,false);
                var canvas = canvasObject.GetComponent<Canvas>();canvas.renderMode = RenderMode.ScreenSpaceOverlay;canvas.sortingOrder = 32760;
                var scaler = canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280,720);scaler.matchWidthOrHeight = .5f;
                var backdrop = Image("Background",canvas.transform,new Color(11/255f,14/255f,22/255f));intro.Backdrop = backdrop;
                var surface = new GameObject("Video",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage),typeof(AspectRatioFitter));surface.transform.SetParent(canvas.transform,false);
                Full(surface.GetComponent<RectTransform>());intro.VideoSurface = surface.GetComponent<RawImage>();intro.VideoSurface.raycastTarget = false;
                var aspect = surface.GetComponent<AspectRatioFitter>();aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio = clip.width/(float)clip.height;
                var hint = new GameObject("Skip hint",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));hint.transform.SetParent(canvas.transform,false);
                var hintRect = hint.GetComponent<RectTransform>();hintRect.anchorMin = new Vector2(.15f,.035f);hintRect.anchorMax = new Vector2(.85f,.10f);hintRect.offsetMin = hintRect.offsetMax = Vector2.zero;
                intro.SkipHint = hint.GetComponent<Text>();intro.SkipHint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");intro.SkipHint.text = "Zum Überspringen tippen";
                intro.SkipHint.fontSize = 20;intro.SkipHint.alignment = TextAnchor.MiddleCenter;intro.SkipHint.color = new Color(.8f,.86f,.89f);intro.SkipHint.raycastTarget = false;intro.SkipHint.enabled = false;
                var black = Image("Black transition",canvas.transform,Color.black);intro.BlackFade = black.gameObject.AddComponent<CanvasGroup>();intro.BlackFade.alpha = 1;intro.BlackFade.blocksRaycasts = true;
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene,true);
            }
        }
        static Image Image(string name,Transform parent,Color color)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);
            Full(go.GetComponent<RectTransform>());var image = go.GetComponent<Image>();image.color = color;image.raycastTarget = true;return image;
        }
        static void Full(RectTransform rect)
        {rect.anchorMin = Vector2.zero;rect.anchorMax = Vector2.one;rect.offsetMin = rect.offsetMax = Vector2.zero;}
    }
}
