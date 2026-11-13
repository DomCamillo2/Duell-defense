using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DefenseDuell.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    /// <summary>Real BoardView renders and pointer geometry, using arranged legal actors.
    /// No Boot scene, user profile, network session or played-match/balance claim.</summary>
    public sealed class MapPresentation15Tests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        static T Field<T>(object source, string name) => (T)source.GetType().GetField(name, Hidden).GetValue(source);
        static readonly string[] AddedIds = { "emerald-basin", "amber-canyon", "aurora-bay" };
        GameObject ownedEventSystem;
        Font oldFont;
        FieldInfo fontField;
        readonly List<Fixture> fixtures = new List<Fixture>();

        sealed class Fixture
        {
            internal GameObject Root;
            internal Canvas Canvas;
            internal Camera Physical;
            internal RenderTexture PhysicalTarget;
            internal GraphicRaycaster Raycaster;
            internal BoardView Board;
            internal int Width, Height;
            internal Camera View => Field<Camera>(Board, "viewCamera");
            internal GameObject World => Field<GameObject>(Board, "world");
            internal RenderTexture Render => Field<RenderTexture>(Board, "texture");
        }
        [Serializable] sealed class Proof
        {
            public string scope = "Actual native BoardView with geometrically legal arranged actors. Not a played match, physical device or GPU performance proof.";
            public string mapId, view;
            public int mapWidth, mapHeight, theme, physicalWidth, physicalHeight, renderWidth, renderHeight;
            public int terrainVertices, backdropVertices, activeUnits, ownTowerCount, sampledPixelColors, legalGroundHints;
            public float zoom, orthographicSize, safeLeft, safeRight, safeTop, safeBottom, projectedGroundWidth, projectedGroundHeight;
            public bool interactive, powerSaving, reducedEffects;
        }
        [SetUp] public void Setup()
        {
            fontField = typeof(GameUI).Assembly.GetType("DefenseDuell.Presentation.UiKit", true)
                .GetField("Font", BindingFlags.Static | BindingFlags.NonPublic);
            oldFont = (Font)fontField.GetValue(null); fontField.SetValue(null, Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            if (EventSystem.current == null) ownedEventSystem = new GameObject("Map15 owned EventSystem", typeof(EventSystem));
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var fixture in fixtures) Release(fixture);
            fixtures.Clear();
            if (ownedEventSystem != null) Object.Destroy(ownedEventSystem);
            if (fontField != null) fontField.SetValue(null, oldFont);
            yield return null; yield return null;
        }
        Fixture Make(GameContent content, int width, int height, bool interactive, int previewCap = 640)
        {
            var f = new Fixture { Width = width, Height = height, Root = new GameObject("Map15 owned fixture " + content.Map.Id) };
            fixtures.Add(f);
            var cameraObject = new GameObject("Map15 physical canvas camera", typeof(Camera)); cameraObject.transform.SetParent(f.Root.transform);
            f.Physical = cameraObject.GetComponent<Camera>(); f.Physical.enabled = false; f.Physical.orthographic = true;
            f.Physical.transform.position = new Vector3(0, 0, -100);
            f.PhysicalTarget = new RenderTexture(width, height, 24); f.PhysicalTarget.Create(); f.Physical.targetTexture = f.PhysicalTarget;
            var canvasObject = new GameObject("Map15 native canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(f.Root.transform); f.Canvas = canvasObject.GetComponent<Canvas>();
            f.Canvas.renderMode = RenderMode.ScreenSpaceCamera; f.Canvas.worldCamera = f.Physical; f.Canvas.planeDistance = 10;
            f.Canvas.scaleFactor = height / 720f; f.Canvas.sortingOrder = 200;
            f.Raycaster = canvasObject.GetComponent<GraphicRaycaster>();
            var boardObject = new GameObject("Map15 actual BoardView", typeof(RectTransform)); boardObject.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)boardObject.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; Canvas.ForceUpdateCanvases();
            f.Board = boardObject.AddComponent<BoardView>(); f.Board.PreviewPixelCap = previewCap;
            f.Board.Initialize(content, Array.Empty<Sprite>(), Array.Empty<Sprite>(), interactive, null); return f;
        }
        [UnityTest] public IEnumerator SuperbossIsDistinctAndPooledTroopsLoseItsCrown()
        {
            var content=GameContent.CreateDefault().WithMatchRules(new MatchRules(10,180,true));
            var state=new MatchSimulation(content).State;state.Phase=MatchPhase.Playing;state.WavePhase=WavePhase.Active;state.WaveId=100;
            var recipe=content.GetWave(100);state.NeutralCursor[0]=recipe.BossUnitIndex+1;
            state.Units.Add(new UnitState{Id=9001,Kind=2,Defender=PlayerSlot.Zero,Sender=PlayerSlot.Zero,Origin=UnitOrigin.NeutralWave,WaveId=100,NeutralRecipeIndex=recipe.BossUnitIndex,Hp=recipe.HpForKind(2),Distance=content.Map.Length/2});
            var f=Make(content,1920,1080,true);f.Board.Display(state,PlayerSlot.Zero);yield return null;
            var crests=f.World.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.name=="Superboss crown").ToArray();
            Assert.That(crests.Any(m=>m.gameObject.activeInHierarchy),Is.True,"Superboss must carry the distinct six-point crown");
            var body=f.World.GetComponentsInChildren<Transform>().First(t=>t.name=="Pooled 3D troop");Assert.That(body.localScale.x,Is.EqualTo(2.6f));
            Capture(f,content,state,"superboss-v16",Vector2.zero);
            state.Units.Clear();state.Units.Add(new UnitState{Id=9002,Kind=2,Defender=PlayerSlot.Zero,Sender=PlayerSlot.One,Origin=UnitOrigin.PaidPvP,WaveId=100,Hp=content.Units[2].Hp,Distance=content.Map.Length/2});
            f.Board.Display(state,PlayerSlot.Zero);yield return null;
            Assert.That(crests.Any(m=>m.gameObject.activeInHierarchy),Is.False,"Pooled paid tank retained the neutral crown");
            Assert.That(body.localScale,Is.EqualTo(Vector3.one));Assert.That(f.Board.VisibleCount,Is.EqualTo(1));
        }
        static void Release(Fixture f)
        {
            if (f == null || f.Root == null) return;
            f.Root.SetActive(false);
            if (f.Physical != null) f.Physical.targetTexture = null;
            if (f.PhysicalTarget != null) { f.PhysicalTarget.Release(); Object.Destroy(f.PhysicalTarget); }
            Object.Destroy(f.Root);
        }
        static GameContent[] Contents()
        {
            Assert.That(MapCatalog.Count, Is.EqualTo(6), "0.15 must contain all six runtime maps");
            var contents = MapCatalog.CreateContents(GameContent.CreateDefault()); Assert.That(contents.Length, Is.EqualTo(6));
            for (int i = 0; i < contents.Length; i++)
            {
                var map = contents[i].Map;
                Assert.That(map.Width, Is.EqualTo(i < 3 ? 1600 : 2400)); Assert.That(map.Height, Is.EqualTo(i < 3 ? 960 : 1440));
                Assert.That(map.Theme, Is.EqualTo(i % 3));
                if (i >= 3) Assert.That(map.Id, Is.EqualTo(AddedIds[i - 3]));
            }
            return contents;
        }
        static MapPoint LegalPoint(GameContent content, MatchState state, PlayerSlot owner, int kind, MapPoint desired)
        {
            long best = long.MaxValue; MapPoint chosen = default;
            for (int y = 45; y < content.Map.Height - 45; y += 24)
                for (int x = 45; x < content.Map.Width - 45; x += 24)
                {
                    long dx = x - desired.X, dy = y - desired.Y, distance = dx * dx + dy * dy;
                    if (distance >= best || PlacementValidator.ValidatePosition(content, state, owner, kind, -1, new MapPoint(x, y)) != ErrorCode.None) continue;
                    best = distance; chosen = new MapPoint(x, y);
                }
            Assert.That(best, Is.Not.EqualTo(long.MaxValue), "No legal placement on " + content.Map.Id); return chosen;
        }
        static MatchState Arrange(GameContent content)
        {
            var state = new MatchSimulation(content, 151, 15001).State; state.Phase = MatchPhase.Playing; state.WavePhase = WavePhase.Active;
            for (int owner = 0; owner < 2; owner++)
            {
                state.Players[owner].AvailableTowerMask = 255;
                for (int kind = 0; kind < content.Towers.Length; kind++)
                {
                    var desired = new MapPoint(content.Map.Width * (kind + 1) / 8, content.Map.Height * (owner == 0 ? 45 : 65) / 100);
                    var point = LegalPoint(content, state, (PlayerSlot)owner, kind, desired);
                    state.Towers.Add(new TowerState { Id = 1 + owner * 100 + kind, Owner = (PlayerSlot)owner, Kind = kind,
                        Slot = kind, SiteId = -1, Position = point, Invested = content.Towers[kind].Price });
                }
                for (int i = 0; i < 12; i++)
                {
                    int kind = i % 4;
                    var stats = UnitResearchRules.Resolve(content, kind, 0, UnitOrigin.PaidPvP, 1);
                    state.Units.Add(new UnitState { Id = 1000 + owner * 100 + i, Defender = (PlayerSlot)owner, Kind = kind,
                        Hp = stats.MaxHp, Origin = UnitOrigin.PaidPvP, WaveId = 1, Distance = (i + 1) * (content.Map.Length - 80) / 13 });
                }
            }
            return state;
        }
        static Vector2 GroundExtent(Fixture f, GameContent content, bool safe)
        {
            float minX = 1, maxX = 0, minY = 1, maxY = 0; Rect bounds = safe ? f.Board.OverviewSafeRect : new Rect(0, 0, 1, 1);
            foreach (int x in new[] { 0, content.Map.Width }) foreach (int y in new[] { 0, content.Map.Height })
            {
                var p = f.View.WorldToViewportPoint(f.World.transform.TransformPoint(DioramaResources.Point(new MapPoint(x, y), .18f)));
                Assert.That(p.z, Is.InRange(f.View.nearClipPlane, f.View.farClipPlane), "Ground is depth-clipped on " + content.Map.Id);
                Assert.That(p.x, Is.InRange(bounds.xMin - .003f, bounds.xMax + .003f), content.Map.Id + " x");
                Assert.That(p.y, Is.InRange(bounds.yMin - .003f, bounds.yMax + .003f), content.Map.Id + " y");
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            return new Vector2(maxX - minX, maxY - minY);
        }
        static void RoundTrip(Fixture f, MapPoint source)
        {
            Vector2 screen = f.Board.MapPointToScreen(source, f.Physical);
            Assert.That(f.Board.TryScreenToMapPoint(screen, f.Physical, out var result), Is.True, "Point outside visible board");
            Assert.That(result.X, Is.EqualTo(source.X).Within(1)); Assert.That(result.Y, Is.EqualTo(source.Y).Within(1));
        }
        static void NativeGroundTap(Fixture f, GameContent content, MatchState state)
        {
            var hits = new List<RaycastResult>(); MapPoint point = default; PointerEventData data = null; RaycastResult hit = default;
            bool found = false;
            for (int y = content.Map.Height / 4; y < content.Map.Height * 3 / 4 && !found; y += 35)
                for (int x = content.Map.Width / 4; x < content.Map.Width * 3 / 4 && !found; x += 35)
                {
                    var candidate = new MapPoint(x, y);
                    if (PlacementValidator.ValidatePosition(content, state, PlayerSlot.Zero, 0, -1, candidate) != ErrorCode.None) continue;
                    Vector2 position = f.Board.MapPointToScreen(candidate, f.Physical);
                    data = new PointerEventData(EventSystem.current) { position = position, pressPosition = position, pointerId = 15015,
                        button = PointerEventData.InputButton.Left, eligibleForClick = true };
                    hits.Clear(); f.Raycaster.Raycast(data, hits);
                    if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Selectable>() != null) continue;
                    if (hits[0].gameObject.GetComponentInParent<BoardView>() != f.Board) continue;
                    point = candidate; hit = hits[0]; found = true;
                }
            Assert.That(found, Is.True, "No visible legal ground target on " + content.Map.Id);
            data.pointerCurrentRaycast = data.pointerPressRaycast = hit;
            int taps = 0; MapPoint selected = default;
            Action<MapPoint> callback = value => { taps++; selected = value; }; f.Board.GroundSelected += callback;
            try
            {
                ExecuteEvents.ExecuteHierarchy(hit.gameObject, data, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.ExecuteHierarchy(hit.gameObject, data, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.ExecuteHierarchy(hit.gameObject, data, ExecuteEvents.pointerClickHandler);
                Assert.That(taps, Is.EqualTo(1)); Assert.That(selected.X, Is.EqualTo(point.X).Within(1)); Assert.That(selected.Y, Is.EqualTo(point.Y).Within(1));
                Assert.That(PlacementValidator.ValidatePosition(content, state, PlayerSlot.Zero, 0, -1, selected), Is.EqualTo(ErrorCode.None));
            }
            finally { f.Board.GroundSelected -= callback; }
        }

        [UnityTest] public IEnumerator SixActualFieldsFitNativeDesktopAndInsetPhoneAndRemainSelectableAtZoom()
        {
            foreach (var content in Contents()) foreach (bool phone in new[] { false, true })
            {
                var f = Make(content, phone ? 2048 : 1920, phone ? 955 : 1080, true); var state = Arrange(content);
                float inset = phone ? 83f * 720 / 955 : 0;
                f.Board.SetOverviewInsets(new Vector4(inset, 0, 0, 0)); f.Board.Display(state, PlayerSlot.Zero);
                yield return null; yield return null; f.Board.ResetView(); f.Board.RefreshRenderNow();
                Assert.That(f.Board.RenderPixelSize, Is.EqualTo(new Vector2Int(f.Width, f.Height)), "Native pixels, not just exported PNG dimensions");
                var extent = GroundExtent(f, content, true);
                Assert.That(Mathf.Max(extent.x, extent.y), Is.GreaterThan(.88f), "Board shrank to a decorative thumbnail");
                foreach (var point in content.Map.Waypoints) RoundTrip(f, point);
                foreach (var tower in state.Towers.Where(t => t.Owner == PlayerSlot.Zero)) RoundTrip(f, tower.Position);
                NativeGroundTap(f, content, state);
                f.Board.ShowPlacementGuide(state, PlayerSlot.Zero, 0);
                Assert.That(f.Board.PlacementGuidePointCount, Is.GreaterThan(50), "The larger field has no usable placement guidance");
                Assert.That(Field<GameObject>(f.Board, "placementGuide").activeSelf, Is.True);
                f.Board.HidePlacementGuide();
                Capture(f, content, state, phone ? "phone-overview" : "desktop-overview", extent);
                if (phone)
                {
                    var focus = state.Towers.First(t => t.Owner == PlayerSlot.Zero && t.Kind == 1).Position;
                    f.Board.SetZoom(1.8f, f.Board.MapPointToScreen(focus, f.Physical), f.Physical);
                    var rect = (RectTransform)f.Board.transform;
                    var centre = RectTransformUtility.WorldToScreenPoint(f.Physical, rect.TransformPoint(rect.rect.center));
                    f.Board.PanByScreenDelta(centre - f.Board.MapPointToScreen(focus, f.Physical), f.Physical);
                    yield return null; f.Board.RefreshRenderNow(); RoundTrip(f, focus);
                    Assert.That(f.Board.ZoomFactor, Is.EqualTo(1.8f).Within(.001f));
                    Capture(f, content, state, "phone-zoom", Vector2.zero);
                    f.Board.ResetView(); GroundExtent(f, content, true); RoundTrip(f, focus);
                }
                Release(f); fixtures.Remove(f); yield return null; yield return null;
            }
        }

        [UnityTest] public IEnumerator SixOpponentPreviewsUseActualEnemyActorsAndRespectPreviewPowerBudget()
        {
            foreach (var content in Contents())
            {
                var state = Arrange(content); var own = Make(content, 1280, 720, true);
                var enemy = Make(content, 1280, 720, false, 1280);
                own.Board.Display(state, PlayerSlot.Zero); enemy.Board.Display(state, PlayerSlot.One);
                yield return null; yield return null; own.Board.RefreshRenderNow(); enemy.Board.RefreshRenderNow();
                Assert.That(own.Board.IsInteractive, Is.True); Assert.That(enemy.Board.IsInteractive, Is.False);
                Assert.That(enemy.Board.RenderPixelSize.x, Is.EqualTo(1280)); GroundExtent(enemy, content, false);
                Assert.That(Field<IDictionary>(own.Board, "active").Keys.Cast<int>(), Is.EquivalentTo(state.Units.Where(u => u.Defender == PlayerSlot.Zero).Select(u => u.Id)));
                Assert.That(Field<IDictionary>(enemy.Board, "active").Keys.Cast<int>(), Is.EquivalentTo(state.Units.Where(u => u.Defender == PlayerSlot.One).Select(u => u.Id)));
                enemy.Board.PowerSaving = true; enemy.Board.ReducedEffects = true; yield return null; enemy.Board.RefreshRenderNow();
                Assert.That(enemy.Board.RenderPixelSize, Is.EqualTo(new Vector2Int(640, 360)));
                Assert.That(enemy.Board.VisibleCount, Is.EqualTo(12), "Budget hides real enemy troops");
                int selected = 0; enemy.Board.GroundSelected += _ => selected++;
                enemy.Board.SelectGroundAt(enemy.Board.MapPointToScreen(state.Towers.First(t => t.Owner == PlayerSlot.One).Position, enemy.Physical), enemy.Physical);
                Assert.That(selected, Is.Zero, "Preview can issue placement input");
                Capture(enemy, content, state, "enemy-power-preview", GroundExtent(enemy, content, false));
                own.Board.PowerSaving = true; own.Board.ReducedEffects = true; yield return null; own.Board.RefreshRenderNow();
                Assert.That(own.Board.RenderPixelSize, Is.EqualTo(new Vector2Int(1280, 720)), "Power mode blurred the gameplay map");
                Release(enemy); Release(own); fixtures.Remove(enemy); fixtures.Remove(own); yield return null; yield return null;
            }
        }

        [UnityTest] public IEnumerator RepeatedSixMapBoardsReleaseOwnedWorldMeshRenderTextureAndLayerLease()
        {
            var leases = (bool[])typeof(BoardView).GetField("LayerLeases", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            int before = leases.Count(x => x);
            foreach (var content in Contents())
            {
                var f = Make(content, 900, 600, true); f.Board.Display(Arrange(content), PlayerSlot.Zero); yield return null;
                var world = f.World; var texture = f.Render; var terrain = Field<DioramaResources>(f.Board, "resources").Terrain;
                var material = world.transform.Find("Raised island").GetComponent<MeshRenderer>().sharedMaterial;
                Assert.That(leases.Count(x => x), Is.EqualTo(before + 1));
                f.Board.enabled = false; Assert.That(world.activeSelf, Is.False);
                f.Board.enabled = true; Assert.That(world.activeSelf, Is.True);
                Release(f); fixtures.Remove(f); yield return null; yield return null;
                Assert.That(world == null, Is.True); Assert.That(texture == null, Is.True);
                Assert.That(terrain == null, Is.True); Assert.That(material == null, Is.True);
                Assert.That(leases.Count(x => x), Is.EqualTo(before), "Map switch leaked a board layer lease");
            }
        }

        static void Capture(Fixture f, GameContent content, MatchState state, string view, Vector2 extent)
        {
            f.Board.RefreshRenderNow(); var terrain = Field<DioramaResources>(f.Board, "resources").Terrain;
            var backdrop = Field<Mesh>(f.Board, "backdropMesh");
            Assert.That(f.World.transform.Find("Raised island").GetComponent<MeshFilter>().sharedMesh, Is.SameAs(terrain));
            Assert.That(terrain.vertexCount, Is.GreaterThan(100)); Assert.That(backdrop.vertexCount, Is.GreaterThan(100));
            Assert.That(GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(f.View),
                f.World.transform.Find("Raised island").GetComponent<MeshRenderer>().bounds), Is.True);
            var rt = f.Render; var previous = RenderTexture.active; var resolved = RenderTexture.GetTemporary(rt.width, rt.height, 0, RenderTextureFormat.ARGB32);
            var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            try
            {
                Graphics.Blit(rt, resolved); RenderTexture.active = resolved;
                image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); image.Apply();
                var palette = new HashSet<int>(); var pixels = image.GetPixels32();
                for (int y = rt.height / 8; y < rt.height * 7 / 8; y += 7) for (int x = rt.width / 8; x < rt.width * 7 / 8; x += 7)
                { var c = pixels[y * rt.width + x]; palette.Add((c.r >> 3) << 10 | (c.g >> 3) << 5 | (c.b >> 3)); }
                Assert.That(palette.Count, Is.GreaterThan(30), "Rendered field is blank/flat; scenery must be real native geometry");
                var safe = f.Board.OverviewSafeRect;
                var proof = new Proof { mapId = content.Map.Id, view = view, mapWidth = content.Map.Width, mapHeight = content.Map.Height, theme = content.Map.Theme,
                    physicalWidth = f.Width, physicalHeight = f.Height, renderWidth = rt.width, renderHeight = rt.height, terrainVertices = terrain.vertexCount,
                    backdropVertices = backdrop.vertexCount, activeUnits = f.Board.VisibleCount, ownTowerCount = content.Towers.Length, sampledPixelColors = palette.Count,
                    legalGroundHints = f.Board.PlacementGuidePointCount,
                    zoom = f.Board.ZoomFactor, orthographicSize = f.View.orthographicSize, safeLeft = safe.xMin, safeRight = 1 - safe.xMax,
                    safeTop = 1 - safe.yMax, safeBottom = safe.yMin, projectedGroundWidth = extent.x, projectedGroundHeight = extent.y,
                    interactive = f.Board.IsInteractive, powerSaving = f.Board.PowerSaving, reducedEffects = f.Board.ReducedEffects };
                string folder = Path.GetFullPath(view.Contains("v16")?"TestResults/Boss-v16":"TestResults/Maps-v15-polish"); Directory.CreateDirectory(folder);
                string stem = content.Map.Id + "-" + view;
                File.WriteAllBytes(Path.Combine(folder, stem + ".png"), image.EncodeToPNG());
                File.WriteAllText(Path.Combine(folder, stem + ".json"), JsonUtility.ToJson(proof, true));
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(resolved); Object.Destroy(image); }
        }
    }
}
