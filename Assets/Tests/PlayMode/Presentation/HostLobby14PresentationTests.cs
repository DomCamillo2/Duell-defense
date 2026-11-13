using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class HostLobby14PresentationTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        GameUI ui;
        SessionCoordinator session;
        Component progress;
        string profileDirectory;
        GameObject panelRoot;
        static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
        static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, AnyInstance).Invoke(target, args);
        static Button Button(Component root, string name) => root.GetComponentsInChildren<Button>(true)
            .Single(b => b.name == name && b.gameObject.activeInHierarchy);
        static InputField Gold(Component root) => root.GetComponentsInChildren<InputField>().Single(i => i.name == "Lobby start gold");
        static Vector2 Center(Component target, Camera camera = null)
        { var r = (RectTransform)target.transform; return RectTransformUtility.WorldToScreenPoint(camera, r.TransformPoint(r.rect.center)); }
        static GameObject Hit(Component target, Camera camera = null)
        {
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = Center(target, camera) }, hits);
            Assert.That(hits, Is.Not.Empty, "No native UI hit for " + target.name);
            return hits[0].gameObject;
        }
        static void Tap(Button button)
        {
            Assert.That(button.interactable, Is.True, button.name);
            var position = Center(button); var hit = Hit(button);
            Assert.That(hit.GetComponentInParent<Button>(), Is.SameAs(button), "Covered button " + button.name);
            var data = new PointerEventData(EventSystem.current) { position = position, pressPosition = position,
                pointerId = 1414, button = PointerEventData.InputButton.Left, eligibleForClick = true };
            ExecuteEvents.ExecuteHierarchy(hit, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit, data, ExecuteEvents.pointerClickHandler);
        }
        static void Submit(Button button) => ExecuteEvents.Execute(button.gameObject,
            new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);

        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Boot"); yield return null; yield return null;
            ui = Object.FindFirstObjectByType<GameUI>(); session = Object.FindFirstObjectByType<SessionCoordinator>();
            Assert.That(ui, Is.Not.Null); Assert.That(session.Status, Is.EqualTo(ConnectionState.Idle));
            progress = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Single(c => c.GetType().FullName == "DefenseDuell.Progression.LocalProgress");
            profileDirectory = Path.Combine(Path.GetTempPath(), "DefenseDuell-lobby14-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(profileDirectory, "profile.save");
            progress.GetType().GetMethod("Initialize").Invoke(progress, new object[] { session, path });
            Assert.That(Field<string>(Field<object>(progress, "store"), "path"), Is.EqualTo(path));
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (ui != null) Call(ui, "CloseModal");
            if (session != null) session.Shutdown();
            if (panelRoot != null) Object.Destroy(panelRoot);
            if (ui != null) Object.Destroy(ui.gameObject);
            if (progress != null) Object.Destroy(progress);
            yield return null;
            if (profileDirectory != null && Directory.Exists(profileDirectory)) Directory.Delete(profileDirectory, true);
        }
        static ushort FreeUdpPort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            { socket.Bind(new IPEndPoint(IPAddress.Loopback, 0)); return (ushort)((IPEndPoint)socket.LocalEndPoint).Port; }
        }
        IEnumerator HostLobby()
        {
            session.StartLanHost(FreeUdpPort());
            double until = Time.realtimeSinceStartupAsDouble + 5;
            while (ui.CurrentScreen != "lobby" && Time.realtimeSinceStartupAsDouble < until) yield return null;
            Assert.That(session.IsHost, Is.True); Assert.That(ui.CurrentScreen, Is.EqualTo("lobby"));
            Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.Lobby));
        }
        IEnumerator Layout(float fullWidth, float left = 0, float right = 0)
        {
            ui.enabled = false;
            var safe = Field<RectTransform>(ui, "safe"); var canvasRect = (RectTransform)safe.parent;
            safe.anchorMin = safe.anchorMax = safe.pivot = new Vector2(.5f, .5f);
            safe.sizeDelta = new Vector2(fullWidth - left - right, 720);
            float scale = Mathf.Min(canvasRect.rect.width / fullWidth, canvasRect.rect.height / 720) * .94f;
            safe.localScale = Vector3.one * scale; safe.anchoredPosition = new Vector2((left - right) * .5f * scale, 0);
            Canvas.ForceUpdateCanvases(); Call(ui, "Rebuild"); yield return null; yield return null; Canvas.ForceUpdateCanvases();
        }
        static LobbySettingsView View(bool host, ulong sequence, ulong revision = 0, int waves = 10,
            string gold = "180", MatchPhase phase = MatchPhase.Lobby, bool isReady = false, string epoch = "14") =>
            new LobbySettingsView(epoch, new LobbyMatchOptions(waves, gold), revision, 0,
                host && phase == MatchPhase.Lobby, phase == MatchPhase.Lobby,
                new LobbyCapabilities(1, MatchRulesPolicy.MaxWaveCount, 196569, maximumGoldMagnitudeBytes: 65523), sequence, phase, isReady);
        static LobbySettingsEditor Editor(LobbySettingsView view)
        { var editor = new LobbySettingsEditor(); editor.BeginSession(view.RoomEpoch); Assert.That(editor.Receive(view), Is.True); return editor; }
        object Panel(LobbySettingsEditor editor, Action<ApplyLobbyOptions> apply = null, Action<ReadyForLobby> ready = null,
            Func<bool> allowed = null)
        {
            ui.enabled = false; Field<RectTransform>(ui, "screen").gameObject.SetActive(false);
            if (panelRoot != null) { panelRoot.SetActive(false); Object.Destroy(panelRoot); }
            var safe = Field<RectTransform>(ui, "safe");
            panelRoot = new GameObject("Host lobby isolated native fixture", typeof(RectTransform));
            var parent = (RectTransform)panelRoot.transform; parent.SetParent(safe, false);
            parent.anchorMin = Vector2.zero; parent.anchorMax = Vector2.one; parent.offsetMin = parent.offsetMax = Vector2.zero;
            float width = Mathf.Min(1120, safe.rect.width - 64), x = (safe.rect.width - width) * .5f;
            var type = typeof(GameUI).Assembly.GetType("DefenseDuell.Presentation.HostLobbyRulesPanel", true);
            return Activator.CreateInstance(type, AnyInstance, null,
                new object[] { parent, x, 230f, width, editor, apply, ready, allowed ?? (() => true) }, CultureInfo.InvariantCulture);
        }
        static IEnumerator Focus(InputField input)
        {
            EventSystem.current.SetSelectedGameObject(input.gameObject); input.ActivateInputField();
            yield return null; yield return null;
            Assert.That(input.isFocused, Is.True, "Input did not acquire native focus");
        }
        void AssertTouchLayout(Component root, RectTransform safe, Camera camera = null)
        {
            var corners = new Vector3[4];
            foreach (var control in root.GetComponentsInChildren<Selectable>())
            {
                Ui16TestNavigation.ScrollIntoView(control);
                var rect = (RectTransform)control.transform;
                Assert.That(rect.rect.width, Is.GreaterThanOrEqualTo(44), control.name);
                Assert.That(rect.rect.height, Is.GreaterThanOrEqualTo(44), control.name);
                rect.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var p = safe.InverseTransformPoint(corner);
                    Assert.That(p.x, Is.InRange(safe.rect.xMin - .5f, safe.rect.xMax + .5f), control.name);
                    Assert.That(p.y, Is.InRange(safe.rect.yMin - .5f, safe.rect.yMax + .5f), control.name);
                }
                Assert.That(Hit(control, camera).GetComponentInParent<Selectable>(), Is.SameAs(control), control.name);
            }
            foreach (var label in root.GetComponentsInChildren<Text>())
            {
                if (string.IsNullOrEmpty(label.text) || label.GetComponentInParent<InputField>() != null) continue;
                Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1), "Clipped: " + label.text);
            }
        }

        [UnityTest] public IEnumerator RealLanHostRulesUseNativeSafeLayoutAtNarrowWideAndPhoneSizes()
        {
            yield return HostLobby();
            foreach (float full in new[] { 900f, 1280f, 2048f / 955 * 720 })
            {
                bool phone = full > 1500; float inset = phone ? 83f / 955 * 720 : 0;
                yield return Layout(full, inset, 0);
                AssertTouchLayout(ui, Field<RectTransform>(ui, "safe"));
                Assert.That(Gold(ui).readOnly, Is.False); Assert.That(Gold(ui).text, Is.EqualTo("180"));
                Assert.That(session.Content.WaveCount, Is.EqualTo(10));
                Assert.That(ui.GetComponentsInChildren<BoardView>().Length, Is.Zero, "Lobby keeps no menu render target alive");
                Capture("host-" + (phone ? "phone-left" : ((int)full).ToString()), full, inset, 0,
                    phone ? 2048 : Mathf.RoundToInt(full * 1.5f), phone ? 955 : 1080);
                if (phone)
                {
                    yield return Layout(full, 0, inset); AssertTouchLayout(ui, Field<RectTransform>(ui, "safe"));
                    Capture("host-phone-right", full, 0, inset, 2048, 955);
                }
            }
        }

        [UnityTest] public IEnumerator RealHostApplyChangesEffectiveRulesAndRebuildKeepsConfirmedValues()
        {
            yield return HostLobby(); yield return Layout(900);
            // The production input gate intentionally rejects actions on a disabled GameUI.
            // Safe-area geometry is already rebuilt and remains stable while it is enabled.
            ui.enabled = true;
            const string exact = "900719925474099312345678901234567890";
            Tap(Button(ui, "Lobby waves decrease")); Gold(ui).text = exact;
            Assert.That(session.Content.WaveCount, Is.EqualTo(10), "Draft changed authority before apply");
            Tap(Button(ui, "Lobby rules primary"));
            var published=(LobbySettingsView)Call(ui,"CurrentLobbyView");
            Assert.That(published.ConfigRevision,Is.EqualTo(session.State.ConfigRevision));
            Assert.That(published.Options.WaveCount,Is.EqualTo(9),"Synchronous ACK must not expose old render-cache options with new revision");
            Assert.That(published.Options.StartGoldDecimal,Is.EqualTo(exact));
            Call(ui, "RefreshLive"); yield return null;
            Assert.That(session.Content.WaveCount, Is.EqualTo(9));
            Assert.That(session.Content.MatchRules.StartGoldDecimal, Is.EqualTo(exact));
            Assert.That(session.State.Players.All(p => p.Gold.IsZero), Is.True, "Applying lobby rules paid start gold prematurely");
            Call(ui, "Rebuild"); yield return null; Call(ui, "RefreshLive");
            Assert.That(Gold(ui).text, Is.EqualTo(exact), "Rebuild lost authoritative amount");
            Assert.That(Button(ui, "Lobby rules primary").interactable, Is.False, "Ready requires the actual second participant");
            Capture("host-configured-900", 900, 0, 0, 1350, 1080);
        }

        [UnityTest] public IEnumerator HostCanTypeTwoHundredWavesAndRejectOutOfRangeWithoutChangingAuthority()
        {
            yield return HostLobby(); yield return Layout(900); ui.enabled = true;
            var input = ui.GetComponentsInChildren<InputField>().Single(i => i.name == "Lobby wave count");
            Assert.That(Hit(input).GetComponentInParent<InputField>(), Is.SameAs(input));
            input.text = "201"; Call(ui, "RefreshLive");
            Assert.That(Button(ui, "Lobby rules primary").interactable, Is.False);
            Assert.That(session.Content.WaveCount, Is.EqualTo(10));
            input.text = "200"; Tap(Button(ui, "Lobby rules primary"));
            var published=(LobbySettingsView)Call(ui,"CurrentLobbyView");
            Assert.That(published.ConfigRevision,Is.EqualTo(session.State.ConfigRevision));
            Assert.That(published.Options.WaveCount,Is.EqualTo(200));
            Call(ui, "RefreshLive"); yield return null;
            Assert.That(session.Content.WaveCount, Is.EqualTo(200));
            Assert.That(session.State.Players.All(p => p.Gold.IsZero && p.ReservedGoldByWave.Length == 200), Is.True);
            Assert.That(session.Content.Waves[199].HpPercent, Is.GreaterThan(session.Content.Waves[9].HpPercent));
            Call(ui, "Rebuild"); yield return null; Call(ui, "RefreshLive");
            input = ui.GetComponentsInChildren<InputField>().Single(i => i.name == "Lobby wave count");
            Assert.That(input.text, Is.EqualTo("200"));
            Assert.That(Button(ui, "Lobby waves increase").interactable, Is.False);
            Capture("host-200-waves-900", 900, 0, 0, 1350, 1080);
        }

        [UnityTest] public IEnumerator GuestRulesAreReadonlyAndKeepNativeReadingFocusAcrossReadyPublications()
        {
            const string exact = "900719925474099312345678901234567890";
            LobbySettingsEditor editor = null; object panel = null; InputField input = null;
            foreach (float full in new[] { 900f, 1280f, 2048f / 955 * 720 })
            {
                bool phone = full > 1500; float inset = phone ? 83f / 955 * 720 : 0;
                yield return Layout(full, 0, inset);
                editor = Editor(View(false, 1, gold: exact)); panel = Panel(editor);
                yield return null; Canvas.ForceUpdateCanvases(); input = Gold(panelRoot.transform);
                Assert.That(input.readOnly, Is.True); Assert.That(input.text, Is.EqualTo(exact));
                Assert.That(panelRoot.GetComponentsInChildren<Button>().Any(b => b.name == "Lobby waves decrease" || b.name == "Lobby waves increase"), Is.False);
                AssertTouchLayout(panelRoot.transform, Field<RectTransform>(ui, "safe"));
                Capture("guest-" + (phone ? "phone-right" : ((int)full).ToString()), full, 0, inset,
                    phone ? 2048 : Mathf.RoundToInt(full * 1.5f), phone ? 955 : 1080);
            }
            yield return Focus(input);
            Call(panel, "Receive", View(false, 2, gold: exact, isReady: true));
            Assert.That(input.isFocused, Is.True, "Read-only focus changed for an unrelated ready publication");
            Assert.That(input.text, Is.EqualTo(exact)); Assert.That(editor.CanEdit, Is.False);
        }

        [UnityTest] public IEnumerator NewConfigurationEndsOldEditSessionAndEscapeCannotRestoreItsAmount()
        {
            yield return Layout(900);
            var editor = Editor(View(true, 1)); var panel = Panel(editor); yield return null;
            var input = Gold(panelRoot.transform); yield return Focus(input);
            input.text = "123"; Assert.That(editor.DraftGold, Is.EqualTo("123"));
            Call(panel, "Receive", View(true, 2, revision: 1, gold: "600"));
            Assert.That(input.isFocused, Is.False); Assert.That(input.text, Is.EqualTo("600"));
            input.ProcessEvent(Event.KeyboardEvent("escape")); input.DeactivateInputField();
            Assert.That(input.text, Is.EqualTo("600")); Assert.That(editor.DraftGold, Is.EqualTo("600"));
            Assert.That(editor.IsDirty, Is.False, "Canceled old focus recreated obsolete draft");
        }

        [UnityTest] public IEnumerator NativePasteKeepsTwentyThousandDigitsExactAndRejectsWholeInvalidInsertions()
        {
            yield return Layout(900);
            var editor = Editor(View(true, 1)); Panel(editor); yield return null;
            var input = Gold(panelRoot.transform); yield return Focus(input);
            input.selectionAnchorPosition = 0; input.selectionFocusPosition = input.text.Length;
            string exact = "1" + new string('7', 19999);
            var append = input.GetType().GetMethod("Append", AnyInstance, null, new[] { typeof(string) }, null);
            Assert.That(append, Is.Not.Null);
            append.Invoke(input, new object[] { exact });
            Assert.That(editor.DraftGold, Is.EqualTo(exact), "Native paste silently truncated the submitted amount");
            Assert.That(input.GetType().GetProperty("ExactValue").GetValue(input), Is.EqualTo(exact));
            Assert.That(input.GetType().GetProperty("IsSummarized").GetValue(input), Is.EqualTo(true));
            Assert.That(input.text.Length, Is.LessThanOrEqualTo(1024), "Huge value was sent to native TextGenerator");
            Assert.That(editor.CanApply, Is.True);
            Assert.That(Call(input, "TryReplaceExactValue", "1\n5"), Is.EqualTo(false));
            Assert.That(editor.DraftGold, Is.EqualTo(exact), "Invalid paste became a different valid number");
            int maximum = (int)input.GetType().GetField("MaximumInputCharacters", AnyInstance).GetValue(input);
            Assert.That(Call(input, "TryReplaceExactValue", new string('9', maximum + 1)), Is.EqualTo(false));
            Assert.That(editor.DraftGold, Is.EqualTo(exact), "Over-limit paste partially replaced the amount");
            string originalClipboard = GUIUtility.systemCopyBuffer;
            try { Call(input, "CopyExactValue"); Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo(exact)); }
            finally { GUIUtility.systemCopyBuffer = originalClipboard; }
            var request = editor.Apply(); Assert.That(request.Options.StartGoldDecimal, Is.EqualTo(exact));
            Assert.That(editor.Reject("14", request.RequestId, "Test: continue editing"), Is.True);
            Call(input, "BeginReplacement");
            Assert.That(input.text, Is.Empty); Assert.That(editor.DraftGold, Is.Empty);
            Assert.That(input.GetType().GetProperty("IsSummarized").GetValue(input), Is.EqualTo(false));
            Assert.That(editor.CanApply, Is.False, "Explicit replacement submitted an empty amount");
            yield return null;
        }

        [UnityTest] public IEnumerator NativePrimaryHasOnePendingOperationAndOlderStateOrAckCannotUnlockStart()
        {
            yield return Layout(900);
            var editor = Editor(View(true, 1)); int applies = 0, readies = 0; ApplyLobbyOptions request = null;
            var panel = Panel(editor, value => { applies++; request = value; }, value => readies++); yield return null;
            Tap(Button(panelRoot.transform, "Lobby waves decrease")); Tap(Button(panelRoot.transform, "Lobby rules primary"));
            Assert.That(applies, Is.EqualTo(1)); Assert.That(editor.HasPending, Is.True);
            Submit(Button(panelRoot.transform, "Lobby rules primary"));
            Assert.That(applies, Is.EqualTo(1), "Disabled primary submitted again");
            Assert.That(editor.Resolve("14", request.RequestId, true, 3), Is.True);
            Assert.That(editor.HasPending, Is.True, "ACK released UI before required authoritative publication");
            Call(panel, "Receive", View(true, 3, revision: 1, waves: 9));
            Assert.That(editor.HasPending, Is.False); Assert.That(editor.IsDirty, Is.False);
            Tap(Button(panelRoot.transform, "Lobby rules primary"));
            Assert.That(readies, Is.EqualTo(1)); Assert.That(editor.CanEdit, Is.False);
            Submit(Button(panelRoot.transform, "Lobby rules primary")); Assert.That(readies, Is.EqualTo(1));
            Call(panel, "Receive", View(true, 4, revision: 1, waves: 9, phase: MatchPhase.Countdown));
            Call(panel, "Receive", View(true, 5, revision: 1, waves: 9));
            Assert.That(editor.Confirmed.Phase, Is.EqualTo(MatchPhase.Countdown));
            Assert.That(editor.CanEdit || editor.CanReady, Is.False);
            Assert.That(editor.Resolve("14", request.RequestId, true, 3), Is.False, "Obsolete request resolved a later operation");
        }

        [UnityTest] public IEnumerator ModalGateAndPanelRecreationCannotSubmitOrForgetPendingRequest()
        {
            yield return Layout(900);
            bool allowed = true; int applies = 0; var editor = Editor(View(true, 1));
            Panel(editor, value => applies++, allowed: () => allowed); yield return null;
            Tap(Button(panelRoot.transform, "Lobby waves decrease"));
            allowed = false;
            Submit(Button(panelRoot.transform, "Lobby rules primary"));
            Assert.That(applies, Is.Zero); Assert.That(editor.Pending, Is.Null, "Modal gate issued a background request");
            allowed = true; Submit(Button(panelRoot.transform, "Lobby rules primary"));
            Assert.That(applies, Is.EqualTo(1)); Guid pending = editor.Pending.RequestId;
            Panel(editor, value => applies++); yield return null;
            Assert.That(editor.Pending.RequestId, Is.EqualTo(pending));
            Assert.That(Button(panelRoot.transform, "Lobby rules primary").interactable, Is.False);
            Submit(Button(panelRoot.transform, "Lobby rules primary")); Assert.That(applies, Is.EqualTo(1));
        }

        void Capture(string name, float fullWidth, float left, float right, int pw, int ph)
        {
            var canvas = ui.GetComponentInChildren<Canvas>().rootCanvas; var safe = Field<RectTransform>(ui, "safe");
            var scale = safe.localScale; var position = safe.anchoredPosition; float factor = canvas.scaleFactor, distance = canvas.planeDistance;
            var scaler = canvas.GetComponent<CanvasScaler>(); bool enabled = scaler.enabled; var mode = canvas.renderMode;
            var previousCamera = canvas.worldCamera; var active = RenderTexture.active;
            var go = new GameObject("Lobby v14 proof camera"); var camera = go.AddComponent<Camera>();
            camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(0, 0, -100);
            var target = new RenderTexture(pw, ph, 24); target.Create(); camera.targetTexture = target; Texture2D image = null;
            try
            {
                scaler.enabled = false; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                canvas.planeDistance = 10; canvas.scaleFactor = ph / 720f;
                safe.localScale = Vector3.one; safe.anchoredPosition = new Vector2((left - right) * .5f, 0); Canvas.ForceUpdateCanvases();
                Assert.That(((RectTransform)canvas.transform).rect.width, Is.EqualTo(fullWidth).Within(1));
                AssertTouchLayout(ui, safe, camera); camera.Render(); RenderTexture.active = target;
                image = new Texture2D(pw, ph, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, pw, ph), 0, 0); image.Apply();
                string folder = Path.GetFullPath("TestResults/Lobby-v15"); Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
                File.WriteAllText(Path.Combine(folder, name + ".json"), "{\"width\":" + pw + ",\"height\":" + ph +
                    ",\"logicalWidth\":" + fullWidth.ToString(CultureInfo.InvariantCulture) +
                    ",\"leftLogicalInset\":" + left.ToString(CultureInfo.InvariantCulture) +
                    ",\"rightLogicalInset\":" + right.ToString(CultureInfo.InvariantCulture) +
                    ",\"scenario\":\"" + (name.StartsWith("guest", StringComparison.Ordinal) ? "isolated guest presentation fixture" : "actual local LAN host lobby") +
                    "\",\"scope\":\"native synthetic safe-area render; no physical device claim\"}");
                var navy = new Color32(9, 27, 43, 255);
                foreach (int x in new[] { 2, pw - 3 })
                {
                    var edge = (Color32)image.GetPixel(x, ph / 2);
                    Assert.That((int)edge.r, Is.EqualTo(navy.r).Within(2)); Assert.That((int)edge.g, Is.EqualTo(navy.g).Within(2));
                    Assert.That((int)edge.b, Is.EqualTo(navy.b).Within(2));
                }
            }
            finally
            {
                RenderTexture.active = active; canvas.renderMode = mode; canvas.worldCamera = previousCamera;
                canvas.planeDistance = distance; canvas.scaleFactor = factor; safe.localScale = scale; safe.anchoredPosition = position;
                scaler.enabled = enabled; camera.targetTexture = null;
                if (image != null) Object.Destroy(image); target.Release(); Object.Destroy(target); Object.Destroy(go); Canvas.ForceUpdateCanvases();
            }
        }
    }
}
