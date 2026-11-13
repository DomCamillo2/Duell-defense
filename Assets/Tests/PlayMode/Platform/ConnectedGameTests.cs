using BigInteger=System.Numerics.BigInteger;
using System.Collections;
using System.Collections.Generic;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using DefenseDuell.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DefenseDuell.Tests
{
    public sealed class ConnectedGameTests
    {
        [UnityTest]
        public IEnumerator ActualBootSceneSupportsTutorialPurchasesAndCleanMenuReturn()
        {
            SceneManager.LoadScene("Boot");
            yield return new WaitForSecondsRealtime(.5f);
            var session = Object.FindFirstObjectByType<SessionCoordinator>();
            var ui = Object.FindFirstObjectByType<GameUI>();
            Assert.That(session, Is.Not.Null);
            Assert.That(ui.CurrentScreen, Is.EqualTo("home"));
            Press("So geht's · interaktives Tutorial");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(ui.TutorialActive, Is.True);
            Assert.That(session.Mode, Is.EqualTo(SessionMode.Offline));
            Press("Ich bin bereit");
            yield return new WaitForSecondsRealtime(3.3f);
            Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.Playing));
            PressNamed("Build class 0");
            ClickGround(OwnBoard(), FreePosition(session));
            yield return new WaitForSecondsRealtime(.2f);
            PressNamed("Confirm build");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(OwnTower(session), Is.Not.Null);
            PressNamed("Send class 0");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(session.LastResult.Accepted, Is.True);
            ClickGround(OwnBoard(), OwnTower(session).Position);
            yield return null;
            PressNamed("Upgrade damage");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(OwnTower(session).Level,Is.EqualTo((BigInteger)1));
            Assert.That(OwnTower(session).DamageLevel,Is.EqualTo((BigInteger)1));
            Assert.That(OwnTower(session).RangeLevel,Is.EqualTo(BigInteger.Zero));
            PressNamed("Sell tower");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(OwnTower(session), Is.Null);
            Assert.That(session.State.Players[0].Gold,Is.GreaterThanOrEqualTo((BigInteger)0));
            int tick = session.State.Tick;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(session.State.Tick, Is.GreaterThan(tick), "The game must actually advance.");
            PressNamed("Battle menu");
            yield return null;
            Press("Duell verlassen");
            yield return null;
            Press("Verlassen");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(ui.CurrentScreen, Is.EqualTo("home"));
            Assert.That(session.State, Is.Null);
            Assert.That(session.RegisteredMessageHandlers, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ThreeMapsBuildGoldminesAndReleaseTheir3DResources()
        {
            SceneManager.LoadScene("Boot");
            yield return new WaitForSecondsRealtime(.5f);
            var session = Object.FindFirstObjectByType<SessionCoordinator>();
            int menuBoards = Object.FindObjectsByType<BoardView>(FindObjectsSortMode.None).Length;
            int menuTextures = Owned3DTextures();
            for (int map = 0; map < 3; map++)
            {
                PressNamed("Home map picker"); yield return null;
                PressNamed("Map choice " + map);
                yield return null;
                Assert.That(session.SelectedMapIndex, Is.EqualTo(map));
                Press("OFFLINE");
                yield return new WaitForSecondsRealtime(.15f);
                Press("Ich bin bereit");
                yield return new WaitForSecondsRealtime(3.3f);
                Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.Playing));
                Assert.That(Object.FindObjectsByType<BoardView>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
                Assert.That(Owned3DTextures(), Is.EqualTo(2));
                // Free placement deliberately removes the old permanent combat pads.
                // Verify the actual rendered terrain meshes, not an obsolete object count.
                int terrains = 0;
                foreach (var mesh in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                {
                    if (mesh.name != "Raised island") continue;
                    Assert.That(mesh.sharedMesh, Is.Not.Null);
                    Assert.That(mesh.sharedMesh.vertexCount, Is.GreaterThan(1000));
                    Assert.That(mesh.sharedMesh.bounds.size.x, Is.GreaterThan(32));
                    terrains++;
                }
                Assert.That(terrains, Is.EqualTo(2));
                var board = OwnBoard();
                var point = FreePosition(session);
                Assert.That(board.TryScreenToMapPoint(board.MapPointToScreen(point), null, out var roundTrip), Is.True);
                Assert.That(roundTrip.X, Is.EqualTo(point.X));
                Assert.That(roundTrip.Y, Is.EqualTo(point.Y));
                // Mine is a free-coordinate purchase through the same actual UI as combat towers.
                Assert.That(session.Content.Map.MineSlots, Is.Empty);
                PressNamed("Build class 4");
                yield return null;
                var minePoint = FreePosition(session, 4);
                ClickGround(board, minePoint);
                yield return null;
                PressNamed("Confirm build");
                yield return new WaitForSecondsRealtime(.2f);
                var mine = OwnTower(session);
                Assert.That(mine, Is.Not.Null);
                Assert.That(mine.Kind, Is.EqualTo(4));
                Assert.That(mine.SiteId, Is.EqualTo(-1));
                Assert.That(mine.Position.X, Is.EqualTo(minePoint.X).Within(1));
                Assert.That(mine.Position.Y, Is.EqualTo(minePoint.Y).Within(1));
                var gold=session.State.Players[0].Gold;
                int pausedPlayTicks = session.State.PlayTicks;
                yield return new WaitForSecondsRealtime(1.1f);
                Assert.That(session.State.Players[0].Gold, Is.EqualTo(gold), "Mines do not pay during preparation");
                Assert.That(session.State.PlayTicks, Is.EqualTo(pausedPlayTicks));
                session.Submit(CommandType.SetWaveReady,1,expectedWave:session.State.WaveId);
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(session.State.WavePhase, Is.EqualTo(WavePhase.Active));
                gold = session.State.Players[0].Gold;
                yield return new WaitForSecondsRealtime(1.1f);
                Assert.That(session.State.Players[0].Gold - gold, Is.GreaterThanOrEqualTo((BigInteger)session.Content.PassiveGoldPerSecond + session.Content.Towers[4].IncomePerSecond));
                ClickGround(board, mine.Position);
                yield return null;
                PressNamed("Sell tower");
                yield return null;
                Assert.That(OwnTower(session), Is.Null);
                PressNamed("Battle menu"); yield return null; Press("Duell verlassen"); yield return null; Press("Verlassen");
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(Object.FindObjectsByType<BoardView>(FindObjectsSortMode.None).Length, Is.EqualTo(menuBoards));
                Assert.That(Owned3DTextures(), Is.EqualTo(menuTextures), "Leaving a match must release its own RenderTextures and retain only the menu preview.");
                Assert.That(session.State, Is.Null);
            }
        }

        static int Owned3DTextures()
        {
            int count = 0;
            foreach (var texture in Resources.FindObjectsOfTypeAll<RenderTexture>())
                if (texture.name.StartsWith("Defense 3D ", System.StringComparison.Ordinal)) count++;
            return count;
        }

        static void ConfirmHomeIfOpen()
        {
            Canvas.ForceUpdateCanvases();
            foreach(var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))if(b.name=="Confirm home loadout"&&b.isActiveAndEnabled)
            {Assert.That(b.interactable,Is.True);Assert.That(((RectTransform)b.transform).rect.height,Is.GreaterThanOrEqualTo(44));b.onClick.Invoke();return;}
        }
        static void PressNamed(string name)
        {
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
                if (button.name == name && button.isActiveAndEnabled && button.interactable)
                { button.onClick.Invoke(); ConfirmHomeIfOpen(); return; }
            Assert.Fail("Active named UI button not found: " + name);
        }

        [UnityTest]
        public IEnumerator TenOfflineOpenCloseCyclesLeaveNoExtraCanvasOrEventSystem()
        {
            SceneManager.LoadScene("Boot");
            yield return new WaitForSecondsRealtime(.5f);
            var session = Object.FindFirstObjectByType<SessionCoordinator>();
            for (int cycle = 0; cycle < 10; cycle++)
            {
                Press("OFFLINE");
                yield return new WaitForSecondsRealtime(.12f);
                Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.Lobby));
                PressNamed("Lobby leave"); yield return null; Press("Verlassen");
                yield return new WaitForSecondsRealtime(.12f);
                Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(session.State, Is.Null);
            }
        }

        static TowerState OwnTower(SessionCoordinator session)
        {
            foreach (var t in session.State.Towers) if (t.Owner == PlayerSlot.Zero) return t;
            return null;
        }
        static BoardView OwnBoard()
        {
            foreach (var board in Object.FindObjectsByType<BoardView>(FindObjectsSortMode.None))
                if (board.name == "Your defense board") return board;
            Assert.Fail("The actual player board is missing.");
            return null;
        }
        static MapPoint FreePosition(SessionCoordinator session, int kind = 0)
        {
            // The board fills the screen behind HUD. Choose a genuinely free point whose
            // actual top raycast is visible ground; occluded HUD taps are tested separately.
            var board = OwnBoard();
            for (int y = 97; y < session.Content.Map.Height - 90; y += 53)
            for (int x = 113; x < session.Content.Map.Width - 90; x += 53)
            {
                var point = new MapPoint(x, y);
                if (PlacementValidator.ValidateBuild(session.Content, session.State, session.LocalSlot, kind, -1, point, out _) != ErrorCode.None) continue;
                bool oldSlot = false;
                foreach (var slot in session.Content.Map.BuildSlots)
                    if (slot.X == point.X && slot.Y == point.Y) { oldSlot = true; break; }
                if (oldSlot) continue;
                var pointer = new PointerEventData(EventSystem.current) { position = board.MapPointToScreen(point) };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                if (hits.Count > 0 && hits[0].gameObject.GetComponentInParent<BoardView>() == board) return point;
            }
            Assert.Fail("No legal free building position found.");
            return default;
        }
        static void ClickGround(BoardView board, MapPoint point)
        {
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = board.MapPointToScreen(point), pointerId = -1,
                button = PointerEventData.InputButton.Left, eligibleForClick = true
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject.GetComponentInParent<BoardView>(), Is.EqualTo(board), "HUD must not cover the chosen ground point.");
            pointer.pointerCurrentRaycast = hits[0];
            var handler = ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(handler, Is.Not.Null, "A real board input handler must receive the tap.");
        }
        static void Press(string text) => PressMatching(text, false);
        static void PressPrefix(string text) => PressMatching(text, true);
        static void PressMatching(string text, bool prefix)
        {
            // A visible modal owns input; an identically labelled background button
            // must not be invoked by the fixture while it is covered.
            var dialog = GameObject.Find("Dialog")?.transform;
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!button.isActiveAndEnabled || !button.interactable
                    || dialog != null && !button.transform.IsChildOf(dialog)) continue;
                foreach (var label in button.GetComponentsInChildren<Text>())
                    if (prefix ? label.text.StartsWith(text, System.StringComparison.Ordinal) : label.text == text)
                    { button.onClick.Invoke(); ConfirmHomeIfOpen(); return; }
            }
            Assert.Fail("Active UI button not found: " + text);
        }
    }
}
