using BigInteger=System.Numerics.BigInteger;
using System.Collections;
using System.Collections.Generic;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class DragPlacementTests
    {
        SessionCoordinator session;
        GameUI ui;
        Mouse testMouse;
        InputActionAsset isolatedActions;
        ReadOnlyArray<InputDevice>? previousDevices;
        InputSettings.BackgroundBehavior previousBackground;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorRouting;
        bool inputRoutingChanged;
        int previousReduced;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousReduced=PlayerPrefs.GetInt("duell.reduced",0);PlayerPrefs.SetInt("duell.reduced",0);
            SceneManager.LoadScene("Boot");yield return new WaitForSecondsRealtime(.2f);
            session=Object.FindFirstObjectByType<SessionCoordinator>();ui=Object.FindFirstObjectByType<GameUI>();
            Assert.That(session,Is.Not.Null);Assert.That(ui,Is.Not.Null);
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if(isolatedActions!=null)isolatedActions.devices=previousDevices;isolatedActions=null;
            if(testMouse!=null&&testMouse.added)InputSystem.RemoveDevice(testMouse);testMouse=null;
            if(inputRoutingChanged){InputSystem.settings.backgroundBehavior=previousBackground;InputSystem.settings.editorInputBehaviorInPlayMode=previousEditorRouting;inputRoutingChanged=false;}
            if(session!=null){session.enabled=true;session.Shutdown();}
            PlayerPrefs.SetInt("duell.reduced",previousReduced);yield return null;
        }
        IEnumerator Playing(int map)
        {
            session.enabled=true;session.Shutdown();Assert.That(session.SelectMap(map),Is.True);session.StartOffline(4);
            // Fixed authoritative fixture: isolate purchase accounting from passive income and the opponent bot.
            session.State.Phase=MatchPhase.Playing;session.State.Players[0].Gold=10000;session.State.Players[0].AvailableTowerMask=(1<<session.Content.Towers.Length)-1;session.enabled=false;
            yield return null;yield return null;Canvas.ForceUpdateCanvases();Assert.That(ui.CurrentScreen,Is.EqualTo("game"));
        }
        static Button Named(string name,bool actionable=true)
        {
            Ui16TestNavigation.Reveal(name);
            foreach(var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))if(b.name==name&&b.isActiveAndEnabled&&(!actionable||b.interactable))return b;
            Assert.Fail("Active button missing: "+name);return null;
        }
        static Vector2 Center(Button b)
        {var r=(RectTransform)b.transform;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));}
        static PointerEventData Pointer(Button b,int id)
        {
            var p=new PointerEventData(EventSystem.current){pointerId=id,button=PointerEventData.InputButton.Left,position=Center(b),pressPosition=Center(b),eligibleForClick=true,pointerPress=b.gameObject,pointerDrag=b.gameObject};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(p,hits);Assert.That(hits.Count,Is.GreaterThan(0));
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.EqualTo(b),b.name+" must receive the actual raycast");p.pointerCurrentRaycast=p.pointerPressRaycast=hits[0];return p;
        }
        static void Down(Button b,PointerEventData p){ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.initializePotentialDrag);}
        static void Move(Button b,PointerEventData p,Vector2 target)
        {p.delta=target-p.position;p.position=target;ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.beginDragHandler);p.dragging=true;ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.dragHandler);}
        static void End(Button b,PointerEventData p)
        {ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.pointerUpHandler);if(p.eligibleForClick)ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.pointerClickHandler);ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.endDragHandler);p.dragging=false;}
        IEnumerator Drag(int kind,MapPoint point,int id=11)
        {
            var b=Named("Build class "+kind);yield return null;var p=Pointer(b,id);Down(b,p);Move(b,p,Board().MapPointToScreen(point));yield return null;End(b,p);yield return null;yield return null;
        }
        BoardView Board()=>GameObject.Find("Your defense board").GetComponent<BoardView>();
        MapPoint FreePoint(int kind=0)
        {
            var map=session.Content.Map;var board=Board();
            for(int y=80;y<map.Height-80;y+=73)for(int x=80;x<map.Width-80;x+=79)
            {
                var point=new MapPoint(x,y);
                if(PlacementValidator.ValidatePosition(session.Content,session.State,PlayerSlot.Zero,kind,-1,point)!=ErrorCode.None)continue;
                var hit=TopAt(board.MapPointToScreen(point),out _);
                if(hit.transform.IsChildOf(board.transform)&&hit.GetComponentInParent<Button>()==null)return point;
            }
            Assert.Fail("No visible legal free fixture position");return default;
        }
        static GameObject TopAt(Vector2 position,out PointerEventData pointer,int id=90)
        {
            pointer=new PointerEventData(EventSystem.current){pointerId=id,button=PointerEventData.InputButton.Left,position=position,pressPosition=position,eligibleForClick=true};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.That(hits.Count,Is.GreaterThan(0));
            pointer.pointerCurrentRaycast=pointer.pointerPressRaycast=hits[0];return hits[0].gameObject;
        }
        static void TapAt(Vector2 position,int id=90)
        {
            var top=TopAt(position,out var pointer,id);
            ExecuteEvents.ExecuteHierarchy(top,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(top,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(top,pointer,ExecuteEvents.pointerClickHandler);
        }
        int OwnBuildings(){int n=0;foreach(var t in session.State.Towers)if(t.Owner==PlayerSlot.Zero)n++;return n;}
        [UnityTest]
        public IEnumerator AllEightClassesDragOnAllThreeMapsExactlyOnce()
        {
            for(int map=0;map<MapCatalog.Count;map++)
            {
                yield return Playing(map);var defs=session.Content.Towers;
                for(int kind=0;kind<defs.Length;kind++)
                {
                    MapPoint point=FreePoint(kind);var gold=session.State.Players[0].Gold;int count=OwnBuildings();ulong sequence=session.LastResult.Sequence;
                    yield return Drag(kind,point,20+kind);
                    Assert.That(OwnBuildings(),Is.EqualTo(count+1));Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold-defs[kind].Price));
                    if(kind>0)Assert.That(session.LastResult.Sequence,Is.EqualTo(sequence+1));
                    Assert.That(session.LastResult.Accepted,Is.True);
                    // Selection replaces the build cards. Return through the real cancel action
                    // before beginning the next class, rather than invoking a hidden button.
                    var build=Named("Build class "+kind);TapAt(Board().MapPointToScreen(point));yield return null;
                    Assert.That(build.gameObject.activeInHierarchy,Is.False);TapAt(Center(Named("Cancel selection")));yield return null;
                    Assert.That(build.gameObject.activeInHierarchy,Is.True);
                }
            }
        }
        [UnityTest]
        public IEnumerator InvalidTargetsInsufficientGoldAndOutsideDropsNeverSubmit()
        {
            yield return Playing(0);MapPoint combat=FreePoint(0);yield return Drag(0,combat);
            ulong last=session.LastResult.Sequence;var gold=session.State.Players[0].Gold;int count=OwnBuildings();
            yield return Drag(1,combat);yield return Drag(4,combat);yield return Drag(0,session.Content.Map.Waypoints[1]);
            Assert.That(session.LastResult.Sequence,Is.EqualTo(last));Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));Assert.That(OwnBuildings(),Is.EqualTo(count));
            session.State.Players[0].Gold=0;yield return Drag(1,FreePoint(0));Assert.That(session.LastResult.Sequence,Is.EqualTo(last));
            session.State.Players[0].Gold=gold;var b=Named("Build class 0");var p=Pointer(b,77);Down(b,p);Move(b,p,new Vector2(Screen.width-4,2));yield return null;End(b,p);yield return null;
            Assert.That(session.LastResult.Sequence,Is.EqualTo(last));Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));
        }
        [UnityTest]
        public IEnumerator SecondFingerReleasedAfterDragCannotBuyOrChangeItsClass()
        {
            yield return Playing(0);var build=Named("Build class 0");var first=Pointer(build,111);Down(build,first);Move(build,first,Board().MapPointToScreen(FreePoint(0)));yield return null;
            var send=Named("Send class 0");var second=Pointer(send,222);Down(send,second);Assert.That(second.eligibleForClick,Is.False);
            var gold=session.State.Players[0].Gold;End(build,first);yield return null;
            ExecuteEvents.Execute(send.gameObject,second,ExecuteEvents.pointerUpHandler);
            // Even a late click forwarded after the first owner released must remain blocked.
            ExecuteEvents.Execute(send.gameObject,second,ExecuteEvents.pointerClickHandler);yield return null;
            Assert.That(OwnBuildings(),Is.EqualTo(1));Assert.That(session.State.SpawnOrders,Is.Empty);Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold-session.Content.Towers[0].Price));
        }
        [UnityTest]
        public IEnumerator DisableCancelsGestureAndRestoresFixedButtonVisual()
        {
            yield return Playing(1);var b=Named("Build class 0");var outer=(RectTransform)b.transform;var size=outer.rect.size;var p=Pointer(b,81);Down(b,p);Move(b,p,Board().MapPointToScreen(FreePoint(0)));yield return null;
            ui.enabled=false;yield return null;End(b,p);yield return null;Assert.That(OwnBuildings(),Is.Zero);Assert.That(outer.rect.size,Is.EqualTo(size));
            Assert.That(b.GetComponent<ButtonPressFeedback>().Visual.localScale,Is.EqualTo(Vector3.one));ui.enabled=true;
        }
        [UnityTest]
        public IEnumerator MinesUseFreeCoordinatesAndCanBeBuiltBeyondTwo()
        {
            yield return Playing(0);Assert.That(session.Content.Map.MineSlots,Is.Empty);
            var first=FreePoint(4);yield return Drag(4,first);
            Assert.That(OwnBuildings(),Is.EqualTo(1));var mine=session.State.Towers[0];Assert.That(mine.SiteId,Is.EqualTo(-1));Assert.That(mine.Position.X,Is.EqualTo(first.X));Assert.That(mine.Position.Y,Is.EqualTo(first.Y));
            Assert.That(GameObject.Find("Tower render slot 0").transform.localPosition.y,Is.EqualTo(.18f).Within(.001f));
            yield return Drag(4,FreePoint(4));var gold=session.State.Players[0].Gold;ulong sequence=session.LastResult.Sequence;
            typeof(GameUI).GetMethod("RefreshLive",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(ui,null);
            var card=Named("Build class 4",false);Assert.That(card.interactable,Is.True);Assert.That(card.GetComponent<PurchaseAvailability>().State,Is.EqualTo(PurchaseState.Available));
            yield return Drag(4,FreePoint(4));
            Assert.That(OwnBuildings(),Is.EqualTo(3));Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold-session.Content.Towers[4].Price));Assert.That(session.LastResult.Sequence,Is.GreaterThan(sequence));
        }
        [UnityTest]
        public IEnumerator PreviewImageBubblesToItsBattlefieldButton()
        {
            yield return Playing(0);var board=Board();var preview=GameObject.Find("Opponent preview").GetComponentInChildren<RawImage>();
            Vector2 center=RectTransformUtility.WorldToScreenPoint(null,preview.rectTransform.TransformPoint(preview.rectTransform.rect.center));
            var top=TopAt(center,out _);Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(top).name,Is.EqualTo("Switch battlefield"));
            TapAt(center);yield return null;Assert.That(board.IsInteractive,Is.False);TapAt(center);yield return null;Assert.That(board.IsInteractive,Is.True);
        }
        [UnityTest]
        public IEnumerator ATopmostHudGraphicBlocksPlacementAtValidGround()
        {
            yield return Playing(0);var canvas=ui.GetComponentInChildren<Canvas>();var b=Named("Build class 0");var p=Pointer(b,91);Down(b,p);
            Vector2 target=Board().MapPointToScreen(FreePoint(0));Move(b,p,target);
            var overlay=new GameObject("Test HUD occluder",typeof(RectTransform),typeof(Image));overlay.transform.SetParent(canvas.transform,false);
            var r=(RectTransform)overlay.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(120,120);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,target,null,out var local);r.anchoredPosition=local;
            overlay.GetComponent<Image>().raycastTarget=true;Canvas.ForceUpdateCanvases();var gold=session.State.Players[0].Gold;
            // GraphicRaycaster deliberately ignores fresh graphics with native canvas depth == -1.
            // Let the test fixture render once before asking it to occlude real gameplay.
            yield return null;Canvas.ForceUpdateCanvases();
            Assert.That(RectTransformUtility.RectangleContainsScreenPoint(r,target,null),Is.True);
            Assert.That(overlay.GetComponent<Image>().depth,Is.GreaterThanOrEqualTo(0));
            Assert.That(TopAt(target,out _),Is.EqualTo(overlay));End(b,p);yield return null;
            Assert.That(OwnBuildings(),Is.Zero);Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));Object.Destroy(overlay);
        }
        [UnityTest]
        public IEnumerator QueuedInputSystemMouseDragExercisesTheActualUiModule()
        {
            yield return Playing(2);
            previousBackground=InputSystem.settings.backgroundBehavior;previousEditorRouting=InputSystem.settings.editorInputBehaviorInPlayMode;inputRoutingChanged=true;
            // A batch editor has no focused Game View. Route this test's synthetic events to the player,
            // and isolate its action bindings from the native mouse without changing production settings.
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            testMouse=InputSystem.AddDevice<Mouse>("Defense drag regression mouse");
            var module=EventSystem.current.GetComponent<InputSystemUIInputModule>();Assert.That(module,Is.Not.Null);
            isolatedActions=module.actionsAsset;previousDevices=isolatedActions.devices;isolatedActions.devices=new InputDevice[]{testMouse};
            var source=Named("Build class 0");var probe=source.gameObject.AddComponent<DragInputProbe>();
            var start=Center(source);var target=Board().MapPointToScreen(FreePoint(0));var gold=session.State.Players[0].Gold;
            InputSystem.QueueStateEvent(testMouse,new MouseState{position=start});yield return null;yield return null;
            Assert.That(testMouse.position.ReadValue(),Is.EqualTo(start));Assert.That(module.GetLastRaycastResult(testMouse.deviceId).gameObject,Is.EqualTo(source.gameObject),"The production UI module must raycast the injected mouse.");
            InputSystem.QueueStateEvent(testMouse,new MouseState{position=start}.WithButton(MouseButton.Left));yield return null;yield return null;
            Assert.That(probe.Downs,Is.EqualTo(1),"Synthetic mouse must produce an actual UI PointerDown.");Assert.That(probe.DeviceId,Is.EqualTo(testMouse.deviceId));
            InputSystem.QueueStateEvent(testMouse,new MouseState{position=Vector2.Lerp(start,target,.5f)}.WithButton(MouseButton.Left));yield return null;
            InputSystem.QueueStateEvent(testMouse,new MouseState{position=target}.WithButton(MouseButton.Left));yield return null;yield return null;
            Assert.That(probe.Begins,Is.EqualTo(1));Assert.That(probe.Drags,Is.GreaterThan(0));
            InputSystem.QueueStateEvent(testMouse,new MouseState{position=target});yield return null;yield return null;yield return new WaitForSecondsRealtime(.1f);
            Assert.That(probe.Ends,Is.EqualTo(1));Assert.That(probe.LastPosition,Is.EqualTo(target));
            Assert.That(OwnBuildings(),Is.EqualTo(1));Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold-session.Content.Towers[0].Price));
            var board=Board();Assert.That(board.FeedbackPlayedCount,Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(.35f);Assert.That(board.FeedbackPlayedCount,Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator DisablingControllerImmediatelyHidesPendingModelAndLabel()
        {
            yield return Playing(0);var board=Board();var context=PresentationCommandContext.Capture(session.State,PlayerSlot.Zero,CommandType.BuildTower,0,-1,Time.unscaledTimeAsDouble,FreePoint(0));
            board.SetPendingBuilds(new[]{context});board.Display(session.State,PlayerSlot.Zero);
            var model=GameObject.Find("Pending preview 0");Assert.That(model,Is.Not.Null);
            Text label=null;foreach(var t in board.GetComponentsInChildren<Text>())if(t.text=="BAU WARTET"){label=t;break;}
            Assert.That(label,Is.Not.Null);Assert.That(model.activeSelf,Is.True);
            ui.enabled=false;Assert.That(model.activeSelf,Is.False);Assert.That(label.gameObject.activeSelf,Is.False);
            yield return null;Assert.That(board.FeedbackPlayedCount,Is.Zero);ui.enabled=true;
        }
        [UnityTest]
        public IEnumerator FullBoardUsesAllNinePortraitsAndSeparateUpgradeQuotes()
        {
            yield return Playing(0);var board=Board();Assert.That(((RectTransform)board.transform).rect.size,Is.EqualTo(((RectTransform)board.transform.parent).rect.size),"Landscape covers its full canvas behind the HUD");
            for(int k=0;k<5;k++){var sprite=GeneratedClassIcons.Get(k,true);Assert.That(sprite,Is.Not.Null);Assert.That(sprite.texture.width,Is.LessThanOrEqualTo(512));Assert.That(Named("Build class "+k).transform.Find("Button visual/Generated class portrait"),Is.Not.Null);}
            for(int k=0;k<4;k++){var sprite=GeneratedClassIcons.Get(k,false);Assert.That(sprite,Is.Not.Null);Assert.That(sprite.texture.width,Is.LessThanOrEqualTo(512));Assert.That(Named("Send class "+k).transform.Find("Button visual/Generated class portrait"),Is.Not.Null);}
            var point=FreePoint(0);yield return Drag(0,point);TapAt(board.MapPointToScreen(point));yield return null;
            var damage=Named("Upgrade damage");var range=Named("Upgrade range");var quote=TowerUpgradeRules.Quote(session.Content,session.State.Towers[0],TowerUpgradePath.Damage);
            Assert.That(damage.GetComponent<ActionButtonVisual>().ValueText,Does.Contain(quote.Current.Damage+" → "+quote.Next.Damage));
            TapAt(Center(damage));yield return null;yield return null;Assert.That(session.State.Towers[0].DamageLevel,Is.EqualTo((BigInteger)1));Assert.That(session.State.Towers[0].RangeLevel,Is.EqualTo(BigInteger.Zero));
            TapAt(Center(Named("Upgrade range")));yield return null;yield return null;Assert.That(session.State.Towers[0].DamageLevel,Is.EqualTo((BigInteger)1));Assert.That(session.State.Towers[0].RangeLevel,Is.EqualTo((BigInteger)1));Assert.That(session.State.Towers[0].Level,Is.EqualTo((BigInteger)2));
        }
        [UnityTest]
        public IEnumerator KnownMoneyLimitsDisableCardsButLateRejectionsStillFlash()
        {
            yield return Playing(0);var point=FreePoint(0);yield return Drag(0,point);TapAt(Board().MapPointToScreen(point));yield return null;
            var refresh=typeof(GameUI).GetMethod("RefreshLive",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            session.State.Players[0].Gold=0;refresh.Invoke(ui,null);
            var upgrade=Named("Upgrade damage",false);var send=Named("Send class 0",false);
            Assert.That(upgrade.interactable,Is.False);Assert.That(send.interactable,Is.False);
            TapAt(Center(upgrade));TapAt(Center(send));Assert.That(session.State.Towers[0].Level,Is.EqualTo(BigInteger.Zero));Assert.That(session.State.SpawnOrders,Is.Empty);
            Assert.That(session.State.Players[0].Gold,Is.EqualTo(BigInteger.Zero));Assert.That(send.GetComponent<ButtonRejectFeedback>().RejectionCount,Is.Zero);
            TapAt(Center(Named("Cancel selection")));yield return null;
            var build=Named("Build class 0",false);Assert.That(build.interactable,Is.False);
            var pointer=Pointer(build,944);Down(build,pointer);Move(build,pointer,Board().MapPointToScreen(FreePoint(0)));End(build,pointer);yield return null;
            Assert.That(OwnBuildings(),Is.EqualTo(1));Assert.That(session.State.Players[0].Gold,Is.EqualTo(BigInteger.Zero));
            // Card was affordable at pointer-down, but authoritative money changes before release.
            session.State.Players[0].Gold=1000;refresh.Invoke(ui,null);var held=Pointer(send,945);Down(send,held);
            session.State.Players[0].Gold=0;ExecuteEvents.Execute(send.gameObject,held,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(send.gameObject,held,ExecuteEvents.pointerClickHandler);
            Assert.That(send.GetComponent<ButtonRejectFeedback>().RejectionCount,Is.EqualTo(1));Assert.That(session.State.Players[0].Gold,Is.EqualTo(BigInteger.Zero));
            // A research change after the displayed send quote is still rejected without a charge.
            session.State.Players[0].Gold=1000;refresh.Invoke(ui,null);
            var research=session.State.Players[0].ResearchLevels[0];
            session.Submit(CommandType.ResearchUnit,0,-1,0,0,0,research);Assert.That(session.LastResult.Accepted,Is.True);
            var afterResearch=session.State.Players[0].Gold;TapAt(Center(send));
            Assert.That(session.LastResult.Code,Is.EqualTo(ErrorCode.QuoteChanged));Assert.That(session.State.Players[0].Gold,Is.EqualTo(afterResearch));
            Assert.That(send.GetComponent<ButtonRejectFeedback>().RejectionCount,Is.EqualTo(2));

        }
        [UnityTest]
        public IEnumerator CompactHudReusesOneBottomRowAndConfirmationRestoresCards()
        {
            yield return Playing(0);var build=Named("Build class 0");var dock=(RectTransform)build.transform.parent;RectTransform context=null;
            foreach(var rect in ui.GetComponentsInChildren<RectTransform>(true))if(rect.name=="Selected building actions"){context=rect;break;}
            Assert.That(context,Is.Not.Null);Assert.That(dock.rect.height,Is.EqualTo(124));
            var corners=new Vector3[4];dock.GetWorldCorners(corners);float right=corners[2].x;
            for(int kind=0;kind<session.Content.Towers.Length;kind++){var card=(RectTransform)Named("Build class "+kind).transform;card.GetWorldCorners(corners);Assert.That(corners[2].x,Is.LessThanOrEqualTo(right+.1f));Assert.That(card.rect.width,Is.GreaterThanOrEqualTo(80));}
            Assert.That(((RectTransform)build.transform).rect.height,Is.EqualTo(dock.rect.height));
            Assert.That(((RectTransform)Named("Send class 0").transform).rect.width,Is.EqualTo(184));
            Assert.That(((RectTransform)Named("Switch battlefield").transform).rect.size,Is.EqualTo(new Vector2(140,94)));
            Assert.That(context.anchoredPosition.y-context.rect.height,Is.EqualTo(dock.anchoredPosition.y-dock.rect.height),"Context and build cards share the same bottom edge");
            Assert.That(context.rect.height,Is.EqualTo(124));
            Assert.That(context.gameObject.activeInHierarchy,Is.False);
            var point=FreePoint(0);TapAt(Center(Named("Build class 0")));yield return null;TapAt(Board().MapPointToScreen(point));yield return null;
            Assert.That(context.gameObject.activeInHierarchy,Is.True);Assert.That(dock.gameObject.activeInHierarchy,Is.False);
            var infoRect=(RectTransform)Named("Attack info 3").transform;Assert.That(context.anchoredPosition.x+context.rect.width,Is.LessThan(infoRect.anchoredPosition.x),"Context and info controls must not overlap on narrow canvases");
            TapAt(Center(Named("Cancel selection")));yield return null;
            Assert.That(context.gameObject.activeInHierarchy,Is.False);Assert.That(dock.gameObject.activeInHierarchy,Is.True);
            TapAt(Center(Named("Build class 0")));yield return null;TapAt(Board().MapPointToScreen(point));yield return null;TapAt(Center(Named("Confirm build")));yield return null;yield return null;
            Assert.That(OwnBuildings(),Is.EqualTo(1));Assert.That(context.gameObject.activeInHierarchy,Is.False);Assert.That(dock.gameObject.activeInHierarchy,Is.True);
            TapAt(Board().MapPointToScreen(point));yield return null;
            Assert.That(context.gameObject.activeInHierarchy,Is.True);Assert.That(dock.gameObject.activeInHierarchy,Is.False);
            TapAt(Center(Named("Sell tower")));yield return null;yield return null;
            Assert.That(OwnBuildings(),Is.Zero);Assert.That(context.gameObject.activeInHierarchy,Is.False);Assert.That(dock.gameObject.activeInHierarchy,Is.True);
        }
        [UnityTest]
        public IEnumerator NavigationZoomPanAndPinchDoNotSelectOrPurchase()
        {
            yield return Playing(0);var board=Board();var point=FreePoint(0);var start=board.MapPointToScreen(point);int selections=0,changes=0;board.GroundSelected+=_=>selections++;board.ViewChanged+=()=>changes++;
            board.SetZoom(float.NaN);board.PanByScreenDelta(new Vector2(float.PositiveInfinity,0));board.SetZoom(1);board.ResetView();Assert.That(changes,Is.Zero);
            board.SetZoom(1.5f,start);Assert.That(board.ZoomFactor,Is.EqualTo(1.5f));Assert.That(changes,Is.EqualTo(1));Assert.That(board.TryScreenToMapPoint(start,null,out var anchored),Is.True);Assert.That(anchored.X,Is.EqualTo(point.X).Within(1));Assert.That(anchored.Y,Is.EqualTo(point.Y).Within(1));
            board.ResetView();start=board.MapPointToScreen(point);var top=TopAt(start,out var p,401);var handler=ExecuteEvents.GetEventHandler<IDragHandler>(top);Assert.That(handler,Is.EqualTo(board.gameObject));
            ExecuteEvents.ExecuteHierarchy(top,p,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(handler,p,ExecuteEvents.initializePotentialDrag);
            p.position=start+new Vector2(40,0);p.delta=new Vector2(40,0);p.dragging=true;ExecuteEvents.Execute(handler,p,ExecuteEvents.beginDragHandler);ExecuteEvents.Execute(handler,p,ExecuteEvents.dragHandler);
            ExecuteEvents.ExecuteHierarchy(top,p,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(handler,p,ExecuteEvents.endDragHandler);ExecuteEvents.ExecuteHierarchy(top,p,ExecuteEvents.pointerClickHandler);
            Assert.That(board.MapPointToScreen(point).x,Is.GreaterThan(start.x+10));Assert.That(selections,Is.Zero);Assert.That(OwnBuildings(),Is.Zero);
            board.ResetView();start=board.MapPointToScreen(FreePoint(0));top=TopAt(start,out var first,501);var second=new PointerEventData(EventSystem.current){pointerId=502,button=PointerEventData.InputButton.Left,position=start+new Vector2(100,0),eligibleForClick=true};
            ExecuteEvents.Execute(board.gameObject,first,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(board.gameObject,second,ExecuteEvents.pointerDownHandler);
            second.position+=new Vector2(80,0);second.dragging=true;ExecuteEvents.Execute(board.gameObject,second,ExecuteEvents.dragHandler);
            Assert.That(board.ZoomFactor,Is.GreaterThan(1));ExecuteEvents.Execute(board.gameObject,first,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(board.gameObject,second,ExecuteEvents.endDragHandler);ExecuteEvents.Execute(board.gameObject,first,ExecuteEvents.pointerClickHandler);ExecuteEvents.Execute(board.gameObject,second,ExecuteEvents.pointerClickHandler);
            Assert.That(selections,Is.Zero);Assert.That(OwnBuildings(),Is.Zero);TapAt(Center(Named("Reset view")));yield return null;Assert.That(board.ZoomFactor,Is.EqualTo(1));
            TapAt(Center(Named("Switch battlefield")));yield return null;Assert.That(board.IsInteractive,Is.False);Assert.That(board.IsNavigable,Is.True);
            TapAt(Center(Named("Zoom in")));yield return null;Assert.That(board.ZoomFactor,Is.GreaterThan(1));TapAt(Center(Named("Return own field")));yield return null;Assert.That(board.IsInteractive,Is.True);Assert.That(OwnBuildings(),Is.Zero);
        }
        [UnityTest]
        public IEnumerator ManyUniqueTouchIdsKeepBoundedHistoryAndBlockLateClicks()
        {
            yield return Playing(0);var board=Board();var surface=board.GetComponent<BoardPointerSurface>();var start=board.MapPointToScreen(FreePoint(0));int selections=0;board.GroundSelected+=_=>selections++;
            var held=new PointerEventData(EventSystem.current){pointerId=5000,button=PointerEventData.InputButton.Left,position=start,eligibleForClick=true};ExecuteEvents.Execute(board.gameObject,held,ExecuteEvents.pointerDownHandler);
            PointerEventData last=null;
            for(int i=0;i<160;i++)
            {
                last=new PointerEventData(EventSystem.current){pointerId=6000+i,button=PointerEventData.InputButton.Left,position=start+new Vector2(90,0),eligibleForClick=true};
                ExecuteEvents.Execute(board.gameObject,last,ExecuteEvents.pointerDownHandler);last.position+=new Vector2(20,0);last.dragging=true;ExecuteEvents.Execute(board.gameObject,last,ExecuteEvents.dragHandler);
                ExecuteEvents.Execute(board.gameObject,last,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(board.gameObject,last,ExecuteEvents.endDragHandler);
            }
            ExecuteEvents.Execute(board.gameObject,held,ExecuteEvents.pointerUpHandler);Assert.That(surface.RetainedPointerCount,Is.LessThanOrEqualTo(64));
            ExecuteEvents.Execute(board.gameObject,held,ExecuteEvents.pointerClickHandler);ExecuteEvents.Execute(board.gameObject,last,ExecuteEvents.pointerClickHandler);Assert.That(selections,Is.Zero);board.ResetView();
            var build=Named("Build class 0");var first=Pointer(build,8000);Down(build,first);Move(build,first,board.MapPointToScreen(FreePoint(0)));var send=Named("Send class 0");
            for(int i=0;i<160;i++){last=Pointer(send,9000+i);Down(send,last);ExecuteEvents.Execute(send.gameObject,last,ExecuteEvents.pointerUpHandler);}
            Assert.That(send.GetComponent<PointerSafeButton>().RetainedPointerCount,Is.LessThanOrEqualTo(64));End(build,first);ExecuteEvents.Execute(send.gameObject,last,ExecuteEvents.pointerClickHandler);yield return null;
            Assert.That(OwnBuildings(),Is.EqualTo(1));Assert.That(session.State.SpawnOrders,Is.Empty);
        }
        [UnityTest]
        public IEnumerator CombatEvolutionCombinesDistinctStagesAndPathGeometry()
        {
            yield return Playing(0);ui.enabled=false;var board=Board();var point=FreePoint(0);var state=session.State;int[,] branches={{0,0},{1,0},{0,1},{2,0},{1,1},{0,2}};
            for(int kind=0;kind<session.Content.Towers.Length;kind++)
            {
                if(kind==4)continue;
                var seen=new HashSet<string>();int baseCount=0;
                for(int variant=0;variant<6;variant++)
                {
                    state.Towers.Clear();state.Towers.Add(new TowerState{Id=100+kind,Owner=PlayerSlot.Zero,Kind=kind,Slot=0,SiteId=-1,Position=point,DamageLevel=branches[variant,0],RangeLevel=branches[variant,1],Level=branches[variant,0]+branches[variant,1]});board.Display(state,PlayerSlot.Zero);
                    var root=GameObject.Find("Tower render slot 0");var meshes=root.GetComponentsInChildren<MeshFilter>();
                    Assert.That(meshes.Length,Is.EqualTo(variant==0?2:3));
                    string geometry="";int vertexCount=0;
                    foreach(var filter in meshes){geometry+=filter.sharedMesh.GetInstanceID()+":";vertexCount+=filter.sharedMesh.vertexCount;}
                    Assert.That(seen.Add(geometry),Is.True,"Each purchase combination must have distinct stage/path geometry");
                    if(variant==0)baseCount=vertexCount;else Assert.That(vertexCount,Is.GreaterThan(baseCount),"Upgrades need added geometry, not a uniform scale.");
                }
                Assert.That(seen.Count,Is.EqualTo(6));
            }
            ui.enabled=true;yield return null;
        }
        [UnityTest]
        public IEnumerator ReusedRenderSlotDoesNotInheritPreviousEntityAnimation()
        {
            yield return Playing(0);ui.enabled=false;var board=Board();var point=FreePoint(0);var state=session.State;
            state.Towers.Add(new TowerState{Id=700,Owner=PlayerSlot.Zero,Kind=0,Level=0,Slot=0,SiteId=-1,Position=point});board.Display(state,PlayerSlot.Zero);
            Assert.That(board.PlayConfirmedFeedback(700,0,0,0,false),Is.True);
            var root=GameObject.Find("Tower render slot 0");Assert.That(root,Is.Not.Null);
            var visual=root.transform.Find("Confirmed building visual");var head=visual.Find("Distinct class head");
            // Represent the in-flight scale/recoil/aim pose, without depending on the batch renderer's frame duration.
            visual.localScale=Vector3.one*.96f;head.localScale=Vector3.one*.9f;head.localRotation=Quaternion.Euler(0,70,0);
            state.Towers[0]=new TowerState{Id=701,Owner=PlayerSlot.Zero,Kind=0,Level=0,Slot=0,SiteId=-1,Position=point};board.Display(state,PlayerSlot.Zero);
            Assert.That(visual.localScale,Is.EqualTo(Vector3.one));Assert.That(head.localScale,Is.EqualTo(Vector3.one));Assert.That(head.localRotation,Is.EqualTo(Quaternion.identity));
            yield return null;Assert.That(visual.localScale,Is.EqualTo(Vector3.one),"An earlier entity's active pulse must not restart on the replacement.");
            Assert.That(board.PlayConfirmedFeedback(700,0,0,0,false),Is.False);Assert.That(board.FeedbackPlayedCount,Is.EqualTo(1));ui.enabled=true;
        }
    }

    public sealed class DragInputProbe : MonoBehaviour,IPointerDownHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public int Downs,Begins,Drags,Ends,DeviceId;
        public Vector2 LastPosition;
        void Record(PointerEventData e){LastPosition=e.position;DeviceId=(e as ExtendedPointerEventData)?.device?.deviceId??-1;}
        public void OnPointerDown(PointerEventData e){Downs++;Record(e);}
        public void OnBeginDrag(PointerEventData e){Begins++;Record(e);}
        public void OnDrag(PointerEventData e){Drags++;Record(e);}
        public void OnEndDrag(PointerEventData e){Ends++;Record(e);}
    }
}
