using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class DragSelectionRegressionTests
    {
        GameUI ui;SessionCoordinator session;
        static T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(obj);
        static void Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(obj,args);
        BoardView Board=>Field<BoardView>(ui,"ownBoard");
        void Refresh(){Call(ui,"RefreshLive");Canvas.ForceUpdateCanvases();}
        [UnitySetUp] public IEnumerator Start()
        {
            SceneManager.LoadScene("Boot");yield return new WaitForSecondsRealtime(.2f);
            session=Object.FindFirstObjectByType<SessionCoordinator>();ui=Object.FindFirstObjectByType<GameUI>();
            session.StartOffline(53);session.enabled=false;session.State.Phase=MatchPhase.Playing;
            session.State.Players[0].Gold=10000;session.State.Players[0].AvailableTowerMask=127;
            yield return null;yield return null;Refresh();
        }
        [UnityTearDown] public IEnumerator Stop()
        {
            if(ui!=null){Call(ui,"CloseModal");Call(ui,"CancelInteraction");}
            if(session!=null){session.enabled=true;session.Shutdown();}yield return null;
        }
        Button Button(string name)
        {
            foreach(var b in ui.GetComponentsInChildren<Button>())if(b.name==name&&b.isActiveAndEnabled)return b;
            Assert.Fail("Missing active button "+name);return null;
        }
        static Vector2 Center(Component c){var r=(RectTransform)c.transform;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));}
        static GameObject Hit(Vector2 p)
        {
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);
            return hits.Count==0?null:hits[0].gameObject;
        }
        static void Tap(Vector2 p)
        {
            var target=Hit(p);Assert.That(target,Is.Not.Null);var e=new PointerEventData(EventSystem.current){pointerId=1101,position=p,pressPosition=p,button=PointerEventData.InputButton.Left,eligibleForClick=true};
            ExecuteEvents.ExecuteHierarchy(target,e,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(target,e,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(target,e,ExecuteEvents.pointerClickHandler);
        }
        MapPoint FreePoint()
        {
            for(int y=90;y<session.Content.Map.Height-90;y+=73)for(int x=90;x<session.Content.Map.Width-90;x+=79)
            {
                var p=new MapPoint(x,y);if(PlacementValidator.ValidatePosition(session.Content,session.State,session.LocalSlot,0,-1,p)!=ErrorCode.None)continue;
                var hit=Hit(Board.MapPointToScreen(p));if(hit!=null&&hit.GetComponentInParent<BoardView>()==Board&&hit.GetComponentInParent<Button>()==null)return p;
            }
            Assert.Fail("No legal visible ground outside an existing tower touch target");return default;
        }
        void AssertNoGhost()
        {
            Assert.That(Field<bool>(ui,"dragging"),Is.False);Assert.That(Field<BuildDragSource>(ui,"dragSource"),Is.Null);
            Assert.That(Field<RectTransform>(ui,"dragChip"),Is.Null);Assert.That((bool)typeof(GameUI).Assembly.GetType("DefenseDuell.Presentation.UiPointerGate").GetProperty("Active",BindingFlags.Public|BindingFlags.Static).GetValue(null),Is.False);
            Assert.That(Field<Transform>(Board,"dragModel").gameObject.activeSelf,Is.False);
            Assert.That(Field<Transform>(Board,"dragMarker").gameObject.activeSelf,Is.False);
            Assert.That(Field<bool>(ui,"buildGuideActive"),Is.False);
        }
        [UnityTest] public IEnumerator BuildThenUpgradeThenGroundTapDeselectsWithoutRearmingConstruction()
        {
            Tap(Center(Button("Build class 0")));Refresh();var point=FreePoint();Tap(Board.MapPointToScreen(point));Refresh();
            Assert.That(Field<bool>(ui,"hasSelectedPoint"),Is.True);
            // Confirm is newly activated by ground selection. Native Canvas depth must exist
            // before another real raycast tap; ForceUpdateCanvases alone does not submit it.
            yield return null;Refresh();
            var confirm=Button("Confirm build");
            Assert.That(Hit(Center(confirm))?.GetComponentInParent<Button>(),Is.SameAs(confirm),"Confirm must receive the actual pointer tap");
            var before=session.State.Players[0].Gold;Tap(Center(confirm));
            Assert.That(session.LastResult.Accepted,Is.True,"The pointer must submit the build");
            yield return null;Refresh();
            Assert.That(session.State.Players[0].Gold,Is.EqualTo(before-session.Content.Towers[0].Price));
            Assert.That(session.State.Towers.Count,Is.EqualTo(1));AssertNoGhost();
            Tap(Board.MapPointToScreen(point));yield return null;Refresh();
            var selected=Field<int>(ui,"selectedTower");Assert.That(selected,Is.GreaterThanOrEqualTo(0));
            Tap(Center(Button("Upgrade damage")));yield return null;Refresh();
            Assert.That(session.LastResult.Accepted,Is.True);Assert.That(Field<int>(ui,"selectedTower"),Is.EqualTo(selected),"Upgrade actions remain accessible");
            var outside=FreePoint();Tap(Board.MapPointToScreen(outside));Refresh();
            Assert.That(Field<int>(ui,"selectedTower"),Is.EqualTo(-1));Assert.That(Field<bool>(ui,"hasSelectedPoint"),Is.False);AssertNoGhost();
            Tap(Board.MapPointToScreen(outside));Refresh();AssertNoGhost();Assert.That(Field<bool>(ui,"hasSelectedPoint"),Is.False);
            // Deselect swaps the context dock for newly activated build cards. A subsequent
            // touch happens after their Canvas submission, not as a third tap in the same frame.
            yield return null;Refresh();
            var rearm=Button("Build class 0");
            Assert.That(Hit(Center(rearm))?.GetComponentInParent<Button>(),Is.SameAs(rearm),"Reactivated build card must receive the pointer");
            Tap(Center(rearm));Refresh();
            Assert.That(Field<bool>(ui,"buildGuideActive"),Is.True,"The real class-card click must rearm construction");
            Assert.That(Field<BuildDragSource>(ui,"dragSource"),Is.Null,"A completed card tap must release its gesture owner");
            yield return null;Refresh();
            var groundHit=Hit(Board.MapPointToScreen(outside));
            Assert.That(groundHit?.GetComponentInParent<BoardView>(),Is.SameAs(Board));
            Assert.That(groundHit.GetComponentInParent<Button>(),Is.Null,"The deselected ground must still receive a ground tap");
            Tap(Board.MapPointToScreen(outside));Refresh();
            Assert.That(Field<bool>(ui,"hasSelectedPoint"),Is.True,"A deliberate new class selection still permits building");
        }
        [UnityTest] public IEnumerator PointerUpFinishesDragWhenEndDragIsMissingAndLateEndCannotDoubleBuy()
        {
            var button=Button("Build class 0");var source=button.GetComponent<BuildDragSource>();var start=Center(button);
            var e=new PointerEventData(EventSystem.current){pointerId=1102,position=start,pressPosition=start,button=PointerEventData.InputButton.Left,eligibleForClick=true};
            source.OnPointerDown(e);e.position=Board.MapPointToScreen(FreePoint());e.dragging=true;source.OnBeginDrag(e);source.OnDrag(e);
            Assert.That(Field<bool>(ui,"dragging"),Is.True);source.OnPointerUp(e);yield return null;Refresh();
            Assert.That(session.State.Towers.Count,Is.EqualTo(1));AssertNoGhost();var sequence=session.LastResult.Sequence;
            source.OnEndDrag(e);source.OnDrag(e);yield return null;Refresh();
            Assert.That(session.LastResult.Sequence,Is.EqualTo(sequence));Assert.That(session.State.Towers.Count,Is.EqualTo(1));AssertNoGhost();
        }
        [UnityTest] public IEnumerator FocusPauseRebuildAndModalCancelHeldDragWithoutBuying()
        {
            foreach(string action in new[]{"focus","pause","rebuild","modal"})
            {
                var button=Button("Build class 0");var source=button.GetComponent<BuildDragSource>();var start=Center(button);
                var e=new PointerEventData(EventSystem.current){pointerId=1103,position=start,pressPosition=start,button=PointerEventData.InputButton.Left};
                source.OnPointerDown(e);e.position=Board.MapPointToScreen(FreePoint());e.dragging=true;source.OnBeginDrag(e);
                if(action=="focus")Call(ui,"OnApplicationFocus",false);
                else if(action=="pause")Call(ui,"OnApplicationPause",true);
                else if(action=="rebuild")Call(ui,"Rebuild");
                else Call(ui,"OpenModal","Test",420f,280f);
                source.OnPointerUp(e);source.OnEndDrag(e);Call(ui,"CloseModal");yield return null;Refresh();
                Assert.That(session.State.Towers.Count,Is.Zero);AssertNoGhost();
            }
        }
        [UnityTest] public IEnumerator DelayedBuildAckAndRejectedCancelledRequestCannotRestorePreview()
        {
            var tracker=Field<PresentationCommandTracker>(ui,"commands");tracker.Reset(session.State.SessionId);
            Tap(Center(Button("Build class 0")));Refresh();var point=FreePoint();Tap(Board.MapPointToScreen(point));Refresh();
            var context=PresentationCommandContext.Capture(session.State,session.LocalSlot,CommandType.BuildTower,0,-1,Time.unscaledTimeAsDouble,point);context.FromConfirmation=true;
            tracker.Register(8701,context);Refresh();
            Assert.That(Field<Transform>(Board,"dragModel").gameObject.activeSelf,Is.False,"Only pending-command feedback may remain while waiting");
            Call(ui,"Resolved",new CommandResult{Sequence=8701,Tick=session.State.Tick,Code=ErrorCode.None});Refresh();AssertNoGhost();
            Assert.That(Field<bool>(ui,"hasSelectedPoint"),Is.False);
            tracker.Reset(session.State.SessionId);Tap(Center(Button("Build class 0")));Refresh();Tap(Board.MapPointToScreen(point));Refresh();
            var rejected=PresentationCommandContext.Capture(session.State,session.LocalSlot,CommandType.BuildTower,0,-1,Time.unscaledTimeAsDouble,point);rejected.FromConfirmation=true;
            tracker.Register(8702,rejected);Call(ui,"ClearBattleSelection");
            Call(ui,"Resolved",new CommandResult{Sequence=8702,Tick=session.State.Tick,Code=ErrorCode.InsufficientGold});Refresh();AssertNoGhost();
            Assert.That(Field<bool>(ui,"hasSelectedPoint"),Is.False);Assert.That(session.State.Towers.Count,Is.Zero);
            yield return null;Refresh();AssertNoGhost();
        }
    }
}
