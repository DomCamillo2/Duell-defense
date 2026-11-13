using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class UpgradeVisualProofTests
    {
        [Serializable] sealed class Picture
        {
            public string file, title, path, sha256;
            public int kind, damageLevel, rangeLevel, level, damage, range, income, incomeUnits, invested;
        }
        [Serializable] sealed class Source { public string file, sha256; }
        [Serializable] sealed class Evidence
        {
            public string utc, scope, rules;
            public Picture[] pictures;
            public Source[] sources;
        }
        static T Field<T>(object value, string name) =>
            (T)value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
        static string Hash(byte[] bytes)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        [UnityTest]
        public IEnumerator ActualUpgradeModelsRenderAllPathsWithoutClipping()
        {
            SceneManager.LoadScene("Boot");
            yield return new WaitForSecondsRealtime(.2f);
            var session = Object.FindFirstObjectByType<SessionCoordinator>();
            var ui = Object.FindFirstObjectByType<GameUI>();
            session.StartOffline(1);
            session.State.Phase = MatchPhase.Playing;
            session.enabled = false;
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            var board = GameObject.Find("Your defense board").GetComponent<BoardView>();
            var camera = Field<Camera>(board, "viewCamera");
            var world = Field<GameObject>(board, "world");
            var resources = Field<DioramaResources>(board, "resources");
            var terrain = world.transform.Find("Raised island").GetComponent<MeshRenderer>();
            var originalTarget = camera.targetTexture;
            var originalPosition = camera.transform.position;
            var originalBackground = camera.backgroundColor;
            bool originalReducedEffects = board.ReducedEffects;
            float originalAspect = camera.aspect, originalSize = camera.orthographicSize;
            var output = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            output.Create();
            var image = new Texture2D(512, 512, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            var records = new List<Picture>();
            string project = Path.GetDirectoryName(Application.dataPath);
            string folder = Path.Combine(project, "TestResults", "UpgradeVisuals-v15");
            Directory.CreateDirectory(folder);
            try
            {
                // Use the actual BoardView and its production meshes/materials. Only the test
                // camera framing and scenery visibility change to make each model inspectable.
                ui.enabled = false;
                board.ReducedEffects = true;
                terrain.enabled = false;
                camera.targetTexture = output;
                camera.aspect = 1;
                camera.backgroundColor = new Color(.91f, .95f, .89f);
                int[,] paths = { {0,0},{1,0},{0,1},{2,0},{1,1},{0,2},{10,0},{15,0},{20,0},{25,0},{30,0},{0,20},{0,30},{15,15},{20,10},{10,20} };
                int[] mineLevels={0,1,2,10,15,20,25,30,40,50,75,100};
                for (int kind = 0; kind < session.Content.Towers.Length; kind++)
                {
                    var hashes = new HashSet<string>();
                    int count = kind == 4 ? mineLevels.Length : paths.GetLength(0);
                    var fixtures = new List<MatchState>();
                    var frame = new Bounds();bool firstVertex = true;
                    // First measure actual actor transforms, including the head-local path module.
                    // Then render all purchases with one common frame so their growth stays comparable.
                    for (int variant = 0; variant < count; variant++)
                    {
                        var simulation = new MatchSimulation(session.Content, 1, (ulong)(100 + kind * 10 + variant));
                        simulation.State.Phase = MatchPhase.Playing;
                        simulation.State.Players[0].Gold = System.Numerics.BigInteger.Pow(10,30);
                        simulation.State.Players[0].AvailableTowerMask = (1<<session.Content.Towers.Length)-1;
                        MapPoint point = FindFree(simulation, kind);
                        ulong sequence = 1;
                        CommandResult Send(CommandType type, int value, int target = -1)
                        {
                            simulation.State.Tick+=session.Content.TickRate;
                            return simulation.Execute(PlayerSlot.Zero,
                                new CommandEnvelope { SessionId = simulation.State.SessionId, Sequence = sequence++, Type = type,
                                    Kind = value, Target = target, PositionX = type == CommandType.BuildTower ? point.X : 0, PositionY = type == CommandType.BuildTower ? point.Y : 0 });
                        }
                        Assert.That(Send(CommandType.BuildTower, kind).Accepted, Is.True);
                        var tower = simulation.State.Towers[0];
                        int damage = kind == 4 ? 0 : paths[variant, 0], range = kind == 4 ? 0 : paths[variant, 1];
                        for (int n = 0; n < damage; n++) Assert.That(Send(CommandType.UpgradeTower, (int)TowerUpgradeRules.PrimaryPath(session.Content,kind), tower.Id).Accepted, Is.True);
                        for (int n = 0; n < range; n++) Assert.That(Send(CommandType.UpgradeTower, (int)TowerUpgradeRules.SecondaryPath(session.Content,kind), tower.Id).Accepted, Is.True);
                        if (kind == 4) for (int n = 0; n < mineLevels[variant]; n++) Assert.That(Send(CommandType.UpgradeTower, (int)TowerUpgradePath.Income, tower.Id).Accepted, Is.True);
                        fixtures.Add(simulation.State);
                        board.Display(simulation.State, PlayerSlot.Zero);
                        var measuredActor = world.transform.Find("Tower views/Tower render slot 0");
                        Assert.That(measuredActor.gameObject.activeInHierarchy, Is.True);
                        foreach(var mesh in measuredActor.GetComponentsInChildren<MeshFilter>())
                        foreach(var vertex in mesh.sharedMesh.vertices)
                        {
                            var local=Quaternion.Inverse(camera.transform.rotation)*(mesh.transform.TransformPoint(vertex)-measuredActor.position);
                            if(firstVertex){frame=new Bounds(local,Vector3.zero);firstVertex=false;}else frame.Encapsulate(local);
                        }
                    }
                    camera.orthographicSize = Mathf.Max(frame.extents.x, frame.extents.y) * 1.15f;
                    for (int variant = 0; variant < count; variant++)
                    {
                        var state=fixtures[variant];var tower=state.Towers[0];
                        int damage=checked((int)tower.DamageLevel),range=checked((int)tower.RangeLevel);
                        board.Display(state, PlayerSlot.Zero);
                        var actor = world.transform.Find("Tower views/Tower render slot 0");
                        camera.transform.position = actor.position + camera.transform.rotation * frame.center - camera.transform.forward * 30;
                        yield return null;
                        foreach (var mesh in actor.GetComponentsInChildren<MeshFilter>())
                        foreach (var vertex in mesh.sharedMesh.vertices)
                        {
                            var projected = camera.WorldToViewportPoint(mesh.transform.TransformPoint(vertex));
                            Assert.That(projected.x, Is.InRange(0f, 1f), "Upgrade mesh is clipped horizontally");
                            Assert.That(projected.y, Is.InRange(0f, 1f), "Upgrade mesh is clipped vertically");
                            Assert.That(projected.z, Is.GreaterThan(0));
                        }
                        camera.Render();
                        RenderTexture.active = output;
                        image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                        image.Apply();
                        byte[] png = image.EncodeToPNG();
                        string file = $"class-{kind}-variant-{variant}.png", digest = Hash(png);
                        Assert.That(hashes.Add(digest), Is.True, "The actual rendered upgrade must differ visibly from the other states");
                        File.WriteAllBytes(Path.Combine(folder, file), png);
                        var stats = TowerUpgradeRules.Stats(session.Content, tower);
                        records.Add(new Picture { file = file, title = session.Content.Towers[kind].Name,
                            path = kind == 4 ? "Ertrag " + tower.Level : $"{TowerUpgradeRules.PathLabel(session.Content,tower,TowerUpgradeRules.PrimaryPath(session.Content,kind))} {damage} / {TowerUpgradeRules.PathLabel(session.Content,tower,TowerUpgradeRules.SecondaryPath(session.Content,kind))} {range}",
                            kind = kind, level = checked((int)tower.Level), damageLevel = damage, rangeLevel = range,
                            damage = checked((int)stats.Damage), range = stats.Range, income = checked((int)stats.IncomePerSecond), incomeUnits = checked((int)stats.IncomeUnitsPerSecond),
                            invested = checked((int)tower.Invested), sha256 = digest });
                    }
                }
                var sources = new List<Source>();
                foreach (string file in new[] { "Assets/Presentation/BoardView.cs", "Assets/Presentation/DioramaMeshes.cs", "Assets/Presentation/VisualEvolutionResources.cs", "Assets/Presentation/VisualEvolutionGeometry.cs", "Assets/Presentation/GraphicsSurfaceResources.cs",
                    "Assets/Presentation/Resources/DefenseDiorama.shader",
                    "Assets/Core/TowerUpgradeRules.cs", "Assets/Content/Standard.asset", "Assets/Tests/PlayMode/Presentation/UpgradeVisualProofTests.cs" })
                    sources.Add(new Source { file = file, sha256 = Hash(File.ReadAllBytes(Path.Combine(project, file))) });
                File.WriteAllText(Path.Combine(folder, "manifest.json"), JsonUtility.ToJson(new Evidence {
                    utc = DateTime.UtcNow.ToString("O"), rules = GameContent.SimulationRulesVersion,
                    scope = "Actual Unity BoardView meshes/materials rendered with one close-up frame per class, shared across low/high purchased branch states through level30 and later mine milestones; purchases use the real Core. This is a model proof, not an Android or HUD test.",
                    pictures = records.ToArray(), sources = sources.ToArray() }, true));
                var html = new StringBuilder("<!doctype html><meta charset='utf-8'><title>Defense Duell · Ausbauformen</title><style>body{font:15px system-ui;background:#12242d;color:#f4f1df;margin:24px}main{display:grid;grid-template-columns:repeat(6,1fr);gap:12px}figure{margin:0;background:#243d45;padding:8px;border-radius:12px}img{width:100%;border-radius:8px}figcaption{line-height:1.5}small{color:#c7d9cf}h1{font-size:24px}</style><h1>Defense Duell · tatsächliche 3D-Ausbauformen</h1><p>Frühe und hohe gekaufte Pfadkombinationen bis Level30; Minen-Meilensteine bis100. Keine Kaufgrenze. Gleiche Kamera und echte Spielmodelle.</p><main>");
                foreach (var item in records)
                    html.Append($"<figure><img src='{item.file}' alt='{item.title}: {item.path}'><figcaption><strong>{item.title}</strong><br>{item.path}<br><small>{item.damage} Schaden · {item.range} Reichweite · +{item.incomeUnits / (float)session.Content.WaveRules.IncomeUnitsPerGold:0.##} Gold/s<br>{item.invested} Gold investiert</small></figcaption></figure>");
                html.Append("</main>");
                File.WriteAllText(Path.Combine(folder, "index.html"), html.ToString());
                Assert.That(records.Count, Is.EqualTo((session.Content.Towers.Length-1)*paths.GetLength(0)+mineLevels.Length));
                Debug.Log("UPGRADE_VISUAL_PROOF_PASS actual_rendered_states="+records.Count+" scope=low-and-high-purchased-paths");
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = originalTarget;
                camera.transform.position = originalPosition;
                camera.backgroundColor = originalBackground;
                board.ReducedEffects = originalReducedEffects;
                camera.aspect = originalAspect;
                camera.orthographicSize = originalSize;
                terrain.enabled = true;
                ui.enabled = true;
                session.enabled = true;
                session.Shutdown();
                output.Release();
                Object.Destroy(output);
                Object.Destroy(image);
            }
            yield return null;
        }

        static MapPoint FindFree(MatchSimulation simulation, int kind)
        {
            for (int y = 80; y < simulation.Content.Map.Height - 80; y += 67)
            for (int x = 80; x < simulation.Content.Map.Width - 80; x += 73)
            {
                var point = new MapPoint(x, y);
                if (PlacementValidator.ValidateBuild(simulation.Content, simulation.State, PlayerSlot.Zero, kind, -1, point, out _) == ErrorCode.None)
                    return point;
            }
            throw new InvalidOperationException("No legal visual fixture position");
        }
    }
}
