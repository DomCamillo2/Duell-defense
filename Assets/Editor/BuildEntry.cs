using System;
using System.IO;
using DefenseDuell.Content;
using DefenseDuell.Networking;
using DefenseDuell.Platform;
using DefenseDuell.Presentation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEditor.U2D;

namespace DefenseDuell.Editor
{
    /// <summary>One reproducible local entry point. Never links a cloud project or changes billing.</summary>
    public static class BuildEntry
    {
        public const string BootScene = "Assets/Scenes/Boot.unity";

        [MenuItem("Defense Duell/Refresh integrated content and art")]
        public static void RefreshIntegratedArt()
        {
            Prepare();
            GraphicsSurfaceBake.Run();
            ModelPortraitBaker.Bake();
        }

        [MenuItem("Defense Duell/Prepare project")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Content");
            PlayerSettings.companyName = "Defense Duell";
            PlayerSettings.productName = "Defense Duell";
            PlayerSettings.bundleVersion = "0.16.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "de.defenseduell.game");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.game");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.defaultScreenWidth = 1440;
            PlayerSettings.defaultScreenHeight = 810;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)35;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = 17;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            // Input System only; this serialized editor property has no public setter in Unity 6.
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler");
            if (input != null) { input.intValue = 1; settings.ApplyModifiedPropertiesWithoutUndo(); }
            if (!File.Exists(BootScene))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cameraObject = new GameObject("Camera", typeof(Camera), typeof(AudioListener));
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.04f, 0.07f, 0.12f);
                camera.orthographic = true;
                cameraObject.transform.position = new Vector3(0, 0, -10);
                EditorSceneManager.SaveScene(scene, BootScene);
            }
            ConnectGame();
            StudioIntroSetup.EnsureScene();
            AppIconSetup.Apply();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(StudioIntroSetup.ScenePath, true), new EditorBuildSettingsScene(BootScene, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("DEFENSE_PREPARE_OK Unity=" + Application.unityVersion);
        }

        static void ConnectGame()
        {
            const string contentPath = "Assets/Content/Standard.asset";
            var content = AssetDatabase.LoadAssetAtPath<GameContentAsset>(contentPath);
            if (content == null)
            {
                content = ScriptableObject.CreateInstance<GameContentAsset>();
                content.SetEditorDefaults();
                AssetDatabase.CreateAsset(content, contentPath);
            }
            if (content.MigratePrototypeInEditor()) EditorUtility.SetDirty(content);
            if (content.RefreshEditorMapCatalog()) EditorUtility.SetDirty(content);
            foreach (var mapContent in content.CreateCatalog()) mapContent.Validate();
            UnlockModelImporter.Import();
            ConfigureArt();
            const string prefabPath = "Assets/Prefabs/GameRoot.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                var root = new GameObject("Defense Duell");
                var session = root.AddComponent<SessionCoordinator>();
                var ui = root.AddComponent<GameUI>();
                var boot = root.AddComponent<GameBootstrap>();
                boot.Content = content; boot.Session = session; boot.Interface = ui;
                ui.TowerSprites = new[] { Tile(249), Tile(204), Tile(226), Tile(206) };
                ui.UnitSprites = new[] { Tile(245), Tile(246), Tile(268), Tile(247) };
                ui.ClickSound = Sound("click_003"); ui.ConfirmSound = Sound("confirmation_001");
                ui.ErrorSound = Sound("error_003"); ui.BackSound = Sound("back_001");
                prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                UnityEngine.Object.DestroyImmediate(root);
            }
            var scene = EditorSceneManager.OpenScene(BootScene, OpenSceneMode.Single);
            if (UnityEngine.Object.FindFirstObjectByType<GameBootstrap>() == null) PrefabUtility.InstantiatePrefab(prefab, scene);
            var backgroundCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (backgroundCamera != null) backgroundCamera.cullingMask = 0;
            EditorSceneManager.SaveScene(scene);
            Debug.Log("DEFENSE_CONTENT_HASH " + content.CreateContent().ContentHash);
        }

        static Sprite Tile(int id) => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Kenney/TowerDefense/towerDefense_tile{id:000}.png");
        static AudioClip Sound(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Art/Kenney/InterfaceSounds/{name}.ogg");

        static void ConfigureArt()
        {
            foreach (string path in Directory.GetFiles("Assets/Art", "*.png", SearchOption.AllDirectories))
            {
                if (path.Replace('\\','/').StartsWith("Assets/Art/AppIcon/", StringComparison.Ordinal)) continue;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null || importer.textureType == TextureImporterType.Sprite) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            const string atlasPath = "Assets/Art/GameAtlas.spriteatlas";
            if (File.Exists(atlasPath)) return;
            var atlas = new SpriteAtlas();
            atlas.SetPackingSettings(new SpriteAtlasPackingSettings { padding = 2, enableRotation = false, enableTightPacking = false });
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings { readable = false, generateMipMaps = false, sRGB = true, filterMode = FilterMode.Bilinear });
            atlas.SetIncludeInBuild(true);
            atlas.Add(new[] { AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Art/Kenney/TowerDefense"), AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Art/Kenney/UI") });
            AssetDatabase.CreateAsset(atlas, atlasPath);
        }

        [MenuItem("Defense Duell/Build Android APK")]
        public static void Android()
        {
            Prepare();
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, "Builds/Android/DefenseDuell.apk");
        }

        [MenuItem("Defense Duell/Build macOS LAN test player")]
        public static void MacOS()
        {
            Prepare();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build macOS 3D test player")]
        public static void MacOS3D()
        {
            Prepare();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-3D/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build isolated visual preview")]
        public static void MacOSPreview()
        {
            Prepare();
            // A separate bundle id keeps native UI automation and the user's existing game unambiguous.
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-Preview/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build alternate macOS preview")]
        public static void MacOSNext()
        {
            Prepare();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.next");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-Next/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build final local test preview")]
        public static void MacOSCandidate()
        {
            Prepare();
            // Keep a currently played preview intact while preparing the final tested artifact.
            PlayerSettings.productName = "Defense Duell " + PlayerSettings.bundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.candidate");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-Candidate/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build art review preview")]
        public static void MacOSArtReview()
        {
            Prepare();
            PlayerSettings.productName = "Defense Duell " + PlayerSettings.bundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.art");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-ArtReview/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build speed review preview")]
        public static void MacOSSpeedReview()
        {
            Prepare();
            PlayerSettings.productName = "Defense Duell " + PlayerSettings.bundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.speed");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-SpeedReview/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build wave review preview")]
        public static void MacOSWaveReview()
        {
            Prepare();
            PlayerSettings.productName = "Defense Duell " + PlayerSettings.bundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.waves");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-WaveReview/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build compact HUD review preview")]
        public static void MacOSHudReview()
        {
            Prepare();
            PlayerSettings.productName = "Defense Duell " + PlayerSettings.bundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.hud");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-HudReview/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build immersive review preview")]
        public static void MacOSImmersiveReview()
        {
            Prepare();
            PlayerSettings.productName = "Defense Duell " + PlayerSettings.bundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.immersive");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-ImmersiveReview/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build fullscreen review preview")]
        public static void MacOSFullscreenReview()
        {
            Prepare();
            PlayerSettings.productName = "Defense Duell " + PlayerSettings.bundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.fullscreen");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-FullscreenReview/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build economy review preview")]
        public static void MacOSEconomyReview()
        {
            Prepare();
            PlayerSettings.productName = "Defense Duell " + PlayerSettings.bundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.economy");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-EconomyReview/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build final economy review preview")]
        public static void MacOSEconomyFinalReview()
        {
            Prepare();
            PlayerSettings.productName = "Defense Duell " + PlayerSettings.bundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.economy.final");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-EconomyFinalReview/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build studio menu review preview")]
        public static void MacOSStudioReview()
        {
            Prepare();
            PlayerSettings.productName = "Defense Duell " + PlayerSettings.bundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.studio");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-StudioReview/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build connection UI review preview")]
        public static void MacOSConnectionReview()
        {
            Prepare();
            PlayerSettings.productName = "Defense Duell " + PlayerSettings.bundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.connection");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-ConnectionReview/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build host rules review preview")]
        public static void MacOSHostRulesReview()
        {
            Prepare();
            PlayerSettings.productName = "Defense Duell " + PlayerSettings.bundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.hostrules");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-HostRulesReview/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build 200 waves review preview")]
        public static void MacOSWaves200Review()
        {
            Prepare();
            PlayerSettings.productName = "Defense Duell " + PlayerSettings.bundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.waves200");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-Waves200Review/DefenseDuell.app");
        }

        [MenuItem("Defense Duell/Build integrated features review preview")]
        public static void MacOSAllFeaturesReview()
        {
            Prepare();
            PlayerSettings.productName = "Defense Duell " + PlayerSettings.bundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.allfeatures");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-AllFeaturesReview/DefenseDuell.app");
        }

        public static void MacOSBalanceReview()
        {
            Prepare();
            PlayerSettings.productName = "Defense Duell " + PlayerSettings.bundleVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "de.defenseduell.preview.update16");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS-Update16Review/DefenseDuell.app");
        }

        static void Build(BuildTarget target, string path)
        {
            ModelPortraitBaker.Verify();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { StudioIntroSetup.ScenePath, BootScene }, locationPathName = path, target = target,
                options = BuildOptions.Development
            });
            var s = report.summary;
            Debug.Log($"DEFENSE_BUILD result={s.result} target={target} bytes={s.totalSize} duration={s.totalTime}");
            if (s.result != BuildResult.Succeeded) throw new BuildFailedException($"Build failed: {s.result}, errors={s.totalErrors}");
        }
    }
}
