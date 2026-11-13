using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DefenseDuell.Content;
using DefenseDuell.Core;
using DefenseDuell.Presentation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DefenseDuell.Editor
{
    /// <summary>Offline portrait production from the same meshes and material as BoardView.</summary>
    public static class ModelPortraitBaker
    {
        const string Folder = "Assets/Art/ModelPortraits/v1";
        const string ImageFolder = Folder + "/Resources/DefenseModelPortraits";
        const string ManifestPath = Folder + "/manifest.json";
        const int Size = 512;
        public static int ExpectedPortraitCount
        {
            get
            {
                int count=0;for(int kind=0;kind<UnitNames.Length;kind++)count+=1+EliteUnitGeometry.StageCount(kind);
                for(int kind=0;kind<TowerNames.Length;kind++)count+=(kind==4?3:6)+TowerStages(kind).Count;
                return count;
            }
        }
        static readonly string[] TowerNames = { "rapid", "cannon", "frost", "armorbreaker", "mine", "tesla", "ballista", "laser" };
        static readonly string[] UnitNames = { "runner", "sprinter", "tank", "swarm", "stormcolossus", "siegecolossus" };
        static readonly string[] SourceFiles = PortraitSources();
        static string[] PortraitSources()
        {
            var files = new List<string> {
                "Assets/Presentation/DioramaMeshes.cs", "Assets/Presentation/BoardView.cs", "Assets/Presentation/EliteUnitGeometry.cs",
                "Assets/Presentation/Resources/DefenseDiorama.shader", "Assets/Core/TowerUpgradeRules.cs",
                "Assets/Presentation/GeneratedClassIcons.cs", "Assets/Editor/ModelPortraitBaker.cs",
                "Assets/Editor/UnlockModelImporter.cs"
            };
            foreach (string kind in new[] { "tesla", "ballista" }) for (int variant = 0; variant < 6; variant++)
            {
                string name = kind + "_v" + variant;
                files.Add("Assets/Art/UnlockModels/Source/" + name + ".mesh.json");
                files.Add(UnlockModelImporter.Destination + "/" + name + "_Base.asset");
                files.Add(UnlockModelImporter.Destination + "/" + name + "_Head.asset");
            }
            var evolutionSources=Directory.GetFiles("Assets/Presentation", "VisualEvolution*.cs");
            Array.Sort(evolutionSources,StringComparer.Ordinal);
            foreach (string path in evolutionSources) files.Add(path.Replace('\\','/'));
            var graphicsSources=Directory.GetFiles("Assets/Presentation","Graphics*.cs");
            Array.Sort(graphicsSources,StringComparer.Ordinal);
            foreach(string path in graphicsSources)files.Add(path.Replace('\\','/'));
            files.Add("Assets/Presentation/Resources/DefenseSurfaceOcclusion.bytes");
            return files.ToArray();
        }
        static readonly Quaternion ViewRotation = Quaternion.LookRotation(-new Vector3(4, 3.6f, 6).normalized, Vector3.up);
        static readonly Vector3[] SwarmPositions = {
            new Vector3(-.35f, 0, -.31f), new Vector3(.35f, 0, -.31f),
            new Vector3(-.35f, 0, .31f), new Vector3(.35f, 0, .31f)
        };

        [Serializable] sealed class Source { public string path, sha256; }
        [Serializable] sealed class Portrait
        {
            public string file, category, title, sha256, meshSha256;
            public int kind, variant, instances, triangles, width, height, minX, minY, maxX, maxY, opaquePixels;
            public float cameraSize;
            public Vector3 cameraPosition, cameraEuler;
        }
        [Serializable] sealed class Manifest
        {
            public int version = 2, portraitCount;
            public string utc, unity, scope, lighting, contentHash;
            public Source[] sources;
            public Portrait[] portraits;
        }
        readonly struct Part
        {
            public readonly Mesh Mesh;
            public readonly Vector3 Offset;
            public Part(Mesh mesh, Vector3 offset) { Mesh = mesh; Offset = offset; }
        }

        [MenuItem("Defense Duell/Bake field model portraits")]
        public static void Bake()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new BuildFailedException("Portrait baking needs graphics: run bash Tools/unity.sh portraits without -nographics.");
            var asset = AssetDatabase.LoadAssetAtPath<GameContentAsset>("Assets/Content/Standard.asset");
            if (asset == null) throw new BuildFailedException("Prepare the content before baking portraits.");
            var content = asset.CreateContent();
            content.Validate();
            Directory.CreateDirectory(ImageFolder);
            var scene = EditorSceneManager.NewPreviewScene();
            var output = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                { antiAliasing = 4, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false)
                { hideFlags = HideFlags.HideAndDontSave };
            var previousActive = RenderTexture.active;
            var records = new List<Portrait>();
            DioramaResources resources = null;
            try
            {
                resources = new DioramaResources(content);
                var cameraObject = CreateObject("Portrait camera", scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.scene = scene;
                camera.orthographic = true;
                camera.aspect = 1;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 50;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                camera.renderingPath = RenderingPath.Forward;
                camera.transform.rotation = ViewRotation;
                output.Create();
                camera.targetTexture = output;
                var lightObject = CreateObject("Field sunlight", scene);
                lightObject.transform.rotation = Quaternion.Euler(48, -34, 0);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.color = new Color(1, .97f, .88f);
                light.shadows = LightShadows.None;
                light.renderMode = LightRenderMode.ForcePixel;

                for (int kind = 0; kind < TowerNames.Length; kind++)
                {
                    int count = kind == 4 ? 3 : 6;
                    var all = new List<Part>();
                    for (int variant = 0; variant < count; variant++) all.AddRange(TowerParts(resources, kind, variant));
                    Frame(camera, all);
                    for (int variant = 0; variant < count; variant++)
                        records.Add(Render(scene, camera, resources.Material, TowerParts(resources, kind, variant), pixels,
                            "tower_" + TowerNames[kind] + "_v" + variant, "tower", content.Towers[kind].Name, kind, variant, 1));
                }
                for (int kind = 0; kind < UnitNames.Length; kind++)
                {
                    var parts = new List<Part>();
                    if (kind == 3) foreach (var position in SwarmPositions) parts.Add(new Part(resources.Units[kind], position));
                    else parts.Add(new Part(resources.Units[kind], Vector3.zero));
                    Frame(camera, parts);
                    records.Add(Render(scene, camera, resources.Material, parts, pixels,
                        "unit_" + UnitNames[kind], "unit", content.Units[kind].Name, kind, 0, parts.Count));
                }
                // Bounded production milestones, with the exact primary/secondary path module.
                for(int kind=0;kind<TowerNames.Length;kind++)
                {
                    var stages=TowerStages(kind);
                    var all=new List<Part>();foreach(var stage in stages)all.AddRange(EvolutionTowerParts(resources,kind,stage.x,stage.y));
                    Frame(camera,all);
                    foreach(var stage in stages)
                    {
                        int key=VisualEvolution.TowerKey(kind,stage.x,stage.y);
                        records.Add(Render(scene,camera,resources.Material,EvolutionTowerParts(resources,kind,stage.x,stage.y),pixels,
                            "evolution_tower_"+TowerNames[kind]+"_k"+key,"evolutionTower",content.Towers[kind].Name,kind,key,1));
                    }
                }
                for(int kind=0;kind<UnitNames.Length;kind++)
                {
                    var all=new List<Part>();for(int tier=0;tier<EliteUnitGeometry.StageCount(kind);tier++)all.AddRange(EvolutionUnitParts(resources,kind,EliteUnitGeometry.LevelForVisualStage(kind,tier)));
                    Frame(camera,all);
                    for(int tier=0;tier<EliteUnitGeometry.StageCount(kind);tier++)
                    {
                        var parts=EvolutionUnitParts(resources,kind,EliteUnitGeometry.LevelForVisualStage(kind,tier));
                        records.Add(Render(scene,camera,resources.Material,parts,pixels,
                            "evolution_unit_"+UnitNames[kind]+"_s"+tier,"evolutionUnit",content.Units[kind].Name,kind,tier,parts.Count));
                    }
                }
                camera.targetTexture = null;
            }
            finally
            {
                RenderTexture.active = previousActive;
                EditorSceneManager.ClosePreviewScene(scene);
                resources?.Dispose();
                output.Release();
                Object.DestroyImmediate(output);
                Object.DestroyImmediate(pixels);
            }
            // Import only after every legacy and production milestone render succeeded. No runtime camera or texture readback is needed.
            AssetDatabase.Refresh();
            foreach (var record in records) ImportSprite(record.file);
            var sources = new List<Source>();
            foreach (string path in SourceFiles) sources.Add(new Source { path = path, sha256 = Hash(File.ReadAllBytes(path)) });
            var manifest = new Manifest {
                utc = DateTime.UtcNow.ToString("O"), unity = Application.unityVersion, portraitCount = records.Count,
                scope = "All reachable bounded production tower mesh/path keys for eight classes, including purchased20/30 and later silhouettes, plus unchanged unit milestones. Exact gameplay levels remain unlimited. Shared class framing; transparent RGBA512 source/editor, Android ETC2 RGBA8 for bounded GPU storage; no runtime HUD cameras.",
                lighting = "Same directional light and opaque shader as BoardView. Fixed +Z-front three-quarter view. Gamma color space, 4x MSAA, no post effects.",
                contentHash = content.ContentHash, sources = sources.ToArray(), portraits = records.ToArray()
            };
            File.WriteAllText(ManifestPath, JsonUtility.ToJson(manifest, true) + "\n");
            WriteGallery(records);
            AssetDatabase.Refresh();
            Verify();
            Debug.Log("DEFENSE_MODEL_PORTRAITS_BAKED count=" + records.Count);
        }

        static GameObject CreateObject(string name, Scene scene)
        {
            var result = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(result, scene);
            return result;
        }
        static readonly Dictionary<int,List<Vector2Int>> stageCache=new Dictionary<int,List<Vector2Int>>();
        static List<Vector2Int> TowerStages(int kind)
        {
            if(stageCache.TryGetValue(kind,out var cached))return cached;
            // All exact levels above100 share the last bounded visual band. Enumerating
            // representatives through200 covers every reachable tier/path-key combination.
            var unique=new SortedDictionary<int,Vector2Int>();
            for(int primary=0;primary<=200;primary++)for(int secondary=0;secondary<=(kind==4?0:200);secondary++)
            {
                int key=VisualEvolution.TowerKey(kind,primary,secondary);
                if(!unique.ContainsKey(key))unique.Add(key,new Vector2Int(primary,secondary));
            }
            cached=new List<Vector2Int>(unique.Values);stageCache.Add(kind,cached);return cached;
        }

        static List<Part> TowerParts(DioramaResources resources, int kind, int variant) => new List<Part> {
            new Part(resources.TowerBases[kind, variant], Vector3.zero),
            new Part(resources.TowerHeads[kind, variant], DioramaResources.TowerHeadOffset(kind))
        };
        static List<Part> EvolutionTowerParts(DioramaResources resources,int kind,int primary,int secondary)
        {
            var art=resources.GetTowerEvolution(kind,primary,secondary);
            var result=new List<Part>{new Part(art.Base,Vector3.zero),new Part(art.Head,DioramaResources.TowerHeadOffset(kind))};
            if(art.PathModule!=null)result.Add(new Part(art.PathModule,DioramaResources.TowerHeadOffset(kind)));
            return result;
        }
        static List<Part> EvolutionUnitParts(DioramaResources resources,int kind,int tier)
        {
            var mesh=resources.GetUnitEvolution(kind,tier);var result=new List<Part>();
            if(kind==3)foreach(var position in SwarmPositions)result.Add(new Part(mesh,position));
            else result.Add(new Part(mesh,Vector3.zero));return result;
        }
        static void Frame(Camera camera, List<Part> parts)
        {
            var inverse = Quaternion.Inverse(ViewRotation);
            bool first = true;
            var bounds = new Bounds();
            foreach (var part in parts) foreach (var vertex in part.Mesh.vertices)
            {
                var point = inverse * (vertex + part.Offset);
                if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                else bounds.Encapsulate(point);
            }
            camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.15f;
            camera.transform.position = ViewRotation * bounds.center - camera.transform.forward * 20;
        }
        static Portrait Render(Scene scene, Camera camera, Material material, List<Part> parts, Texture2D pixels,
            string name, string category, string title, int kind, int variant, int instances)
        {
            var subject = CreateObject(name, scene);
            int triangles = 0;
            try
            {
                foreach (var part in parts)
                {
                    var child = CreateObject(part.Mesh.name, scene);
                    child.transform.SetParent(subject.transform, false);
                    child.transform.localPosition = part.Offset;
                    child.AddComponent<MeshFilter>().sharedMesh = part.Mesh;
                    var renderer = child.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                    renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    triangles += part.Mesh.triangles.Length / 3;
                    foreach (var vertex in part.Mesh.vertices)
                    {
                        var projected = camera.WorldToViewportPoint(vertex + part.Offset);
                        if (projected.x < .025f || projected.x > .975f || projected.y < .025f || projected.y > .975f || projected.z <= 0)
                            throw new BuildFailedException("Clipped portrait geometry: " + name);
                    }
                }
                camera.Render();
                RenderTexture.active = camera.targetTexture;
                pixels.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                pixels.Apply();
                var record = new Portrait { file = ImageFolder + "/" + name + ".png", category = category, title = title,
                    kind = kind, variant = variant, instances = instances, triangles = triangles, width = Size, height = Size,
                    cameraSize = camera.orthographicSize, cameraPosition = camera.transform.position, cameraEuler = camera.transform.eulerAngles,
                    minX = Size, minY = Size, maxX = -1, maxY = -1, meshSha256 = MeshHash(parts) };
                var colors = pixels.GetPixels32();
                for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                {
                    if (colors[y * Size + x].a < 128) continue;
                    record.opaquePixels++;
                    record.minX = Math.Min(record.minX, x); record.maxX = Math.Max(record.maxX, x);
                    record.minY = Math.Min(record.minY, y); record.maxY = Math.Max(record.maxY, y);
                }
                if (record.opaquePixels < Size * Size / 12 || record.opaquePixels > Size * Size * .8f ||
                    record.minX < 8 || record.minY < 8 || record.maxX >= Size - 8 || record.maxY >= Size - 8)
                    throw new BuildFailedException("Blank, opaque-background or clipped portrait: " + name);
                byte[] png = pixels.EncodeToPNG();
                record.sha256 = Hash(png);
                if(!File.Exists(record.file)||Hash(File.ReadAllBytes(record.file))!=record.sha256)File.WriteAllBytes(record.file, png);
                return record;
            }
            finally { Object.DestroyImmediate(subject); }
        }
        static string MeshHash(List<Part> parts)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(parts.Count);
                foreach (var part in parts)
                {
                    writer.Write(part.Offset.x); writer.Write(part.Offset.y); writer.Write(part.Offset.z);
                    writer.Write(part.Mesh.vertexCount);
                    foreach (var v in part.Mesh.vertices) { writer.Write(v.x); writer.Write(v.y); writer.Write(v.z); }
                    foreach (var n in part.Mesh.normals) { writer.Write(n.x); writer.Write(n.y); writer.Write(n.z); }
                    foreach (var c in part.Mesh.colors32) { writer.Write(c.r); writer.Write(c.g); writer.Write(c.b); writer.Write(c.a); }
                    foreach (var uv in part.Mesh.uv) { writer.Write(uv.x); writer.Write(uv.y); }
                    var indices = part.Mesh.triangles;
                    writer.Write(indices.Length);
                    foreach (int index in indices) writer.Write(index);
                }
            }
            return Hash(stream.ToArray());
        }
        static void ImportSprite(string path)
        {
            if(AssetImporter.GetAtPath(path)==null)AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var android=importer.GetPlatformTextureSettings("Android");
            if(importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Single&&importer.spritePixelsPerUnit==100&&importer.alphaSource==TextureImporterAlphaSource.FromInput&&importer.alphaIsTransparency&&importer.sRGBTexture&&!importer.mipmapEnabled&&!importer.isReadable&&importer.npotScale==TextureImporterNPOTScale.None&&importer.maxTextureSize==Size&&importer.textureCompression==TextureImporterCompression.Uncompressed&&importer.filterMode==FilterMode.Bilinear&&importer.wrapMode==TextureWrapMode.Clamp&&android.overridden&&android.maxTextureSize==Size&&android.format==TextureImporterFormat.ETC2_RGBA8&&android.compressionQuality==100&&!importer.GetPlatformTextureSettings("Standalone").overridden)return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = Size;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            // ETC2 RGBA8 is part of GLES3: preserve512px alpha portraits while
            // using one quarter of uncompressed GPU storage on Android.
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name="Android",overridden=true,maxTextureSize=Size,format=TextureImporterFormat.ETC2_RGBA8,
                textureCompression=TextureImporterCompression.CompressedHQ,compressionQuality=100
            });
            importer.ClearPlatformTextureSettings("Standalone");
            importer.SaveAndReimport();
        }

        [MenuItem("Defense Duell/Verify field model portraits")]
        public static void Verify()
        {
            if (!File.Exists(ManifestPath)) throw new BuildFailedException("Missing model portraits. Run bash Tools/unity.sh portraits.");
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            if (manifest == null || manifest.version != 2 || manifest.portraitCount != ExpectedPortraitCount || manifest.portraits == null || manifest.portraits.Length != ExpectedPortraitCount)
                throw new BuildFailedException("Incomplete production tower/path and unit portrait series.");
            var content = AssetDatabase.LoadAssetAtPath<GameContentAsset>("Assets/Content/Standard.asset").CreateContent();
            if (manifest.contentHash != content.ContentHash) throw new BuildFailedException("Portrait content changed; rebake the portraits.");
            if (manifest.sources == null || manifest.sources.Length != SourceFiles.Length)
                throw new BuildFailedException("Incomplete portrait source manifest.");
            for (int i = 0; i < SourceFiles.Length; i++)
                if (manifest.sources[i].path != SourceFiles[i] || manifest.sources[i].sha256 != Hash(File.ReadAllBytes(SourceFiles[i])))
                    throw new BuildFailedException("Stale model portraits: " + SourceFiles[i] + ". Run bash Tools/unity.sh portraits.");
            var seen = new HashSet<string>();
            foreach (var portrait in manifest.portraits)
            {
                bool tower=portrait.category=="tower",unit=portrait.category=="unit",evolutionTower=portrait.category=="evolutionTower",evolutionUnit=portrait.category=="evolutionUnit";
                bool anyTower=tower||evolutionTower;
                if(!(tower||unit||evolutionTower||evolutionUnit)||portrait.kind<0||portrait.kind>=(anyTower?TowerNames.Length:UnitNames.Length)||portrait.variant<0)
                    throw new BuildFailedException("Invalid portrait identity in manifest.");
                bool valid=tower?portrait.variant<(portrait.kind==4?3:6):unit?portrait.variant==0:evolutionUnit?portrait.variant<EliteUnitGeometry.StageCount(portrait.kind):
                    TowerStages(portrait.kind).Exists(stage=>VisualEvolution.TowerKey(portrait.kind,stage.x,stage.y)==portrait.variant);
                if(!valid)throw new BuildFailedException("Invalid evolution portrait key.");
                string stem=tower?"tower_"+TowerNames[portrait.kind]+"_v"+portrait.variant:unit?"unit_"+UnitNames[portrait.kind]:
                    evolutionTower?"evolution_tower_"+TowerNames[portrait.kind]+"_k"+portrait.variant:"evolution_unit_"+UnitNames[portrait.kind]+"_s"+portrait.variant;
                string expected=ImageFolder+"/"+stem+".png";
                if (portrait.file != expected || !seen.Add(expected) || !File.Exists(expected) || Hash(File.ReadAllBytes(expected)) != portrait.sha256)
                    throw new BuildFailedException("Missing, duplicate or changed portrait: " + expected);
                var importer = AssetImporter.GetAtPath(expected) as TextureImporter;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(expected);
                if (importer == null || importer.textureType != TextureImporterType.Sprite ||
                    importer.spriteImportMode != SpriteImportMode.Single || importer.spritePixelsPerUnit != 100 ||
                    importer.alphaSource != TextureImporterAlphaSource.FromInput || !importer.alphaIsTransparency || !importer.sRGBTexture ||
                    importer.mipmapEnabled || importer.isReadable || importer.npotScale != TextureImporterNPOTScale.None ||
                    importer.maxTextureSize != Size || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                    importer.filterMode != FilterMode.Bilinear || importer.wrapMode != TextureWrapMode.Clamp ||
                    !importer.GetPlatformTextureSettings("Android").overridden ||
                    importer.GetPlatformTextureSettings("Android").format!=TextureImporterFormat.ETC2_RGBA8 ||
                    importer.GetPlatformTextureSettings("Android").maxTextureSize!=Size ||
                    importer.GetPlatformTextureSettings("Standalone").overridden ||
                    sprite == null || sprite.texture.width != Size || sprite.texture.height != Size ||
                    (sprite.texture.format != TextureFormat.RGBA32 && sprite.texture.format != TextureFormat.ETC2_RGBA8))
                    throw new BuildFailedException("Incorrect portrait Sprite import: " + expected);
            }
            Debug.Log("DEFENSE_MODEL_PORTRAITS_VERIFIED count="+ExpectedPortraitCount);
        }
        static string Hash(byte[] bytes)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        static void WriteGallery(List<Portrait> records)
        {
            string output = "TestResults/ModelPortraits-v16";
            Directory.CreateDirectory(output);
            var html = new StringBuilder("<!doctype html><meta charset='utf-8'><title>Defense Duell · Spielmodelle</title><style>body{margin:24px;background:#203c35;color:#fff3d8;font:15px system-ui}main{display:grid;grid-template-columns:repeat(6,minmax(0,1fr));gap:12px}figure{margin:0;background:#b5a08933;border:1px solid #e7bc5970;border-radius:18px;padding:8px}img{width:100%}figcaption{line-height:1.5}small{opacity:.75}</style><h1>Aus echten Spielmodellen gerendert</h1><p>Eine Kameragröße je Klasse und Serie,2088 Modellporträts einschließlich Eliteklassen und Klassenpfaden. Beim Schwarm sind vier Einheiten dargestellt.</p><main>");
            foreach (var record in records)
            {
                string filename = Path.GetFileName(record.file);
                File.Copy(record.file, Path.Combine(output, filename), true);
                html.Append($"<figure><img src='{filename}' alt='{record.title}'><figcaption>{record.title} · {record.variant}<br><small>{record.triangles} Dreiecke</small></figcaption></figure>");
            }
            html.Append("</main>");
            File.WriteAllText(Path.Combine(output, "index.html"), html.ToString());
            File.Copy(ManifestPath, Path.Combine(output, "manifest.json"), true);
        }
    }
}
