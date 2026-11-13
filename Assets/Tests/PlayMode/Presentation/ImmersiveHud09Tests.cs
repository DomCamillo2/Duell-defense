using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    /// <summary>Rendering/input and authoritative income feedback integration; no new economy rules.</summary>
    public sealed class ImmersiveHud09Tests
    {
        SessionCoordinator session;
        GameUI ui;
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
        static void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,args);
        MatchSimulation Simulation=>Field<MatchSimulation>(session,"simulation");
        AttackIncomeFeedback Feedback=>Field<AttackIncomeFeedback>(ui,"attackIncomeFeedback");
        BoardView Board=>Field<BoardView>(ui,"ownBoard");
        [UnitySetUp] public IEnumerator Start()
        {
            SceneManager.LoadScene("Boot");yield return new WaitForSecondsRealtime(.2f);
            session=Object.FindFirstObjectByType<SessionCoordinator>();ui=Object.FindFirstObjectByType<GameUI>();
            session.StartOffline(43);session.enabled=false;
            session.State.Phase=MatchPhase.Playing;session.State.WavePhase=WavePhase.Preparing;
            session.State.PreparationRemaining=session.Content.WaveRules.PreparationTicks;
            session.State.Players[0].Gold=10000;
            yield return null;yield return null;Refresh();
        }
        [UnityTearDown] public IEnumerator Stop()
        {
            if(ui!=null){Call(ui,"CancelInteraction");Call(ui,"CloseModal");Call(ui,"CloseAttackInfo");}
            if(session!=null){session.enabled=true;session.Shutdown();}
            yield return null;
        }
        void Refresh(){Call(ui,"RefreshLive");Canvas.ForceUpdateCanvases();}
        Button Button(string name)=>ui.GetComponentsInChildren<Button>(true).First(b=>b.name==name&&b.gameObject.activeInHierarchy);
        static Vector2 ScreenPoint(RectTransform rect,Vector2 local)=>RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(local));
        static Vector2 Center(Component component){var rect=(RectTransform)component.transform;return ScreenPoint(rect,rect.rect.center);}
        static GameObject Hit(Vector2 screenPoint)
        {
            var e=new PointerEventData(EventSystem.current){position=screenPoint};var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(e,hits);return hits.Count>0?hits[0].gameObject:null;
        }
        static void Submit(Button button)
        {EventSystem.current.SetSelectedGameObject(button.gameObject);ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);}
        void Send(int kind)
        {
            var q=SendRules.Quote(session.Content,session.State,session.LocalSlot,kind);Assert.That(q.IsValid,Is.True);
            session.Submit(CommandType.SendUnit,kind,-1,0,0,q.WaveId,q.ResearchLevel,q.EnemyTarget);Assert.That(session.LastResult.Accepted,Is.True);
        }
        void BeginActiveWave()
        {
            session.Submit(CommandType.SetWaveReady,1,expectedWave:session.State.WaveId);
            Assert.That(session.LastResult.Accepted,Is.True);Simulation.Step();
            Assert.That(session.State.WavePhase,Is.EqualTo(WavePhase.Active));
        }

        [UnityTest] public IEnumerator FullscreenBoardStaysBehindSafeAreaHudAndKeepsItsBaseReachable()
        {
            var safe=Field<RectTransform>(ui,"safe");var canvas=(RectTransform)safe.parent;
            safe.anchorMin=new Vector2(.04f,.03f);safe.anchorMax=new Vector2(.97f,.96f);
            safe.offsetMin=safe.offsetMax=Vector2.zero;Canvas.ForceUpdateCanvases();Call(ui,"Rebuild");
            yield return null;Refresh();Call(ui,"RefreshBoardOverviewInsets");Board.RefreshRenderNow();yield return null;
            var full=Field<RectTransform>(ui,"fullScreenBoard");
            Assert.That(full.parent,Is.SameAs(canvas));Assert.That(safe.parent,Is.SameAs(canvas));
            Assert.That(full.GetSiblingIndex(),Is.LessThan(safe.GetSiblingIndex()));
            Assert.That(full.rect.width,Is.EqualTo(canvas.rect.width).Within(.1f));
            Assert.That(full.rect.height,Is.EqualTo(canvas.rect.height).Within(.1f));
            Assert.That(full.rect.width,Is.GreaterThan(safe.rect.width));
            Assert.That(full.rect.height,Is.GreaterThan(safe.rect.height));
            // The scenery also owns raycasts in the former pale frame, outside the HUD safe area.
            var edge=ScreenPoint(full,new Vector2(full.rect.xMin+2,full.rect.center.y));
            Assert.That(Hit(edge)?.GetComponentInParent<BoardView>(),Is.SameAs(Board));
            var waypoints=session.Content.Map.Waypoints;var exit=Board.MapPointToScreen(waypoints[waypoints.Length-1]);
            Board.SetZoom(1.5f);Board.PanByScreenDelta(Center(full)-Board.MapPointToScreen(waypoints[waypoints.Length-1]));
            yield return null;Refresh();
            Assert.That(Hit(Board.MapPointToScreen(waypoints[waypoints.Length-1]))?.GetComponentInParent<BoardView>(),Is.SameAs(Board),"Panning must expose the base beyond the HUD");
            Board.ResetView();Refresh();
            Assert.That(Hit(Center(Button("Send class 0")))?.GetComponentInParent<Button>(),Is.SameAs(Button("Send class 0")));
        }

        [UnityTest] public IEnumerator GoldRateShowsPausedZeroAndKeepsGameSecondRateAtDoubleSpeed()
        {
            Assert.That(Field<Text>(ui,"incomeText").text,Does.Contain("+0 G/s"));
            Assert.That(Field<Text>(ui,"incomeText").text,Does.Contain("Baupause"));
            bool built=false;
            for(int y=50;y<session.Content.Map.Height-50&&!built;y+=53)
                for(int x=50;x<session.Content.Map.Width-50;x+=59)
                {
                    var p=new MapPoint(x,y);
                    if(PlacementValidator.ValidatePosition(session.Content,session.State,session.LocalSlot,4,-1,p)!=ErrorCode.None)continue;
                    session.Submit(CommandType.BuildTower,4,-1,x,y);Assert.That(session.LastResult.Accepted,Is.True);
                    var mine=session.State.Towers.Last();session.Submit(CommandType.UpgradeTower,(int)TowerUpgradePath.Income,mine.Id);
                    Assert.That(session.LastResult.Accepted,Is.True);built=true;break;
                }
            Assert.That(built,Is.True);Refresh();Assert.That(Field<Text>(ui,"incomeText").text,Does.Contain("+0 G/s"));
            BeginActiveWave();Refresh();
            string normal=Field<Text>(ui,"incomeText").text;
            Assert.That(normal,Does.Contain("+5,1 G/s"));Assert.That(normal,Does.Contain("Spielzeit"));
            Submit(Button("Match tempo"));Refresh();Assert.That(session.State.SimulationSpeed,Is.EqualTo(2));
            Assert.That(Field<Text>(ui,"incomeText").text,Is.EqualTo(normal));
            CompactHud08Tests.CaptureHud("immersive-income-double-speed");yield return null;
            session.State.WavePhase=WavePhase.Preparing;Refresh();
            Assert.That(Field<Text>(ui,"incomeText").text,Does.Contain("+0 G/s"));
            Assert.That(Field<Text>(ui,"incomeText").text,Does.Contain("Baupause"));
            CompactHud08Tests.CaptureHud("immersive-income-paused");
        }

        [UnityTest] public IEnumerator PurchasedPacketsOnlyCelebrateTheirActualSpawnedIncomeAndAggregateTogether()
        {
            int baseline=Feedback.NoticeCount;
            Send(0);Send(0);Refresh();
            Assert.That(session.State.Players[0].AttackIncomeUnits,Is.Zero);
            Assert.That(Feedback.NoticeCount,Is.EqualTo(baseline));
            Assert.That(Field<RectTransform>(ui,"incomeFeedbackPanel").gameObject.activeSelf,Is.False);
            BeginActiveWave();
            // No HUD refresh between the two real paid spawns: one combined observed increase.
            int steps=0;
            while(session.State.Players[0].AttackIncomeUnits<144&&steps++<200)Simulation.Step();
            Assert.That(session.State.Players[0].AttackIncomeUnits,Is.EqualTo(144));
            Assert.That(session.State.Units.Any(u=>u.Origin==UnitOrigin.PaidPvP&&u.Defender==PlayerSlot.One),Is.True);
            Assert.That(session.State.Units.Any(u=>u.Origin==UnitOrigin.NeutralWave),Is.True);
            Refresh();
            Assert.That(Feedback.NoticeCount,Is.EqualTo(baseline+1));Assert.That(Feedback.VisibleIncreaseUnits,Is.EqualTo((System.Numerics.BigInteger)144));
            Assert.That(Field<Text>(ui,"incomeFeedbackText").text,Is.EqualTo("+0,15 G/s"));
            Assert.That(Field<RectTransform>(ui,"incomeFeedbackPanel").gameObject.activeSelf,Is.True);
            CompactHud08Tests.CaptureHud("immersive-actual-spawn-income");yield return null;
            int count=Feedback.NoticeCount;Refresh();Assert.That(Feedback.NoticeCount,Is.EqualTo(count));
            Assert.That(Feedback.VisibleIncreaseUnits,Is.EqualTo((System.Numerics.BigInteger)144));
        }

        [UnityTest] public IEnumerator NeutralSpawnsAndHudRebuildsDoNotInventIncomeNotices()
        {
            int count=Feedback.NoticeCount;BeginActiveWave();
            for(int i=0;i<40;i++)Simulation.Step();Refresh();
            Assert.That(session.State.Units.Any(u=>u.Origin==UnitOrigin.NeutralWave),Is.True);
            Assert.That(session.State.Players[0].AttackIncomeUnits,Is.Zero);Assert.That(Feedback.NoticeCount,Is.EqualTo(count));
            Send(0);for(int i=0;i<80&&session.State.Players[0].AttackIncomeUnits<72;i++)Simulation.Step();Refresh();
            Assert.That(session.State.Players[0].AttackIncomeUnits,Is.EqualTo(72));Assert.That(Feedback.NoticeCount,Is.EqualTo(count+1));
            Call(ui,"Rebuild");yield return null;Refresh();
            Assert.That(Feedback.NoticeCount,Is.EqualTo(count+1));Assert.That(Feedback.VisibleIncreaseUnits,Is.EqualTo(System.Numerics.BigInteger.Zero));
            Assert.That(Field<RectTransform>(ui,"incomeFeedbackPanel").gameObject.activeSelf,Is.False);
            Call(ui,"OnApplicationPause",true);Refresh();Assert.That(Feedback.NoticeCount,Is.EqualTo(count+1));
        }

        [Test] public void IncomeHighWaterRejectsDuplicateOlderAndRegressedSnapshotsAndResetsScopes()
        {
            var feedback=new AttackIncomeFeedback();feedback.Reset(11,0,72,100);
            Assert.That(feedback.Observe(11,0,72,100,true,1),Is.False);
            Assert.That(feedback.Observe(11,0,144,104,true,1.1),Is.True);
            Assert.That(feedback.VisibleIncreaseUnits,Is.EqualTo((System.Numerics.BigInteger)72));Assert.That(feedback.NoticeCount,Is.EqualTo(1));
            Assert.That(feedback.Observe(11,0,216,108,true,1.2),Is.False,"Additional actual gains join the visible aggregate");
            Assert.That(feedback.VisibleIncreaseUnits,Is.EqualTo((System.Numerics.BigInteger)144));
            feedback.Observe(11,0,288,99,true,1.3);feedback.Observe(11,0,72,109,true,1.4);feedback.Observe(11,0,216,110,true,1.5);
            Assert.That(feedback.VisibleIncreaseUnits,Is.EqualTo((System.Numerics.BigInteger)144));Assert.That(feedback.NoticeCount,Is.EqualTo(1));
            Assert.That(feedback.IsVisible(3.2),Is.False);
            Assert.That(feedback.Observe(11,0,288,112,true,4),Is.True);Assert.That(feedback.VisibleIncreaseUnits,Is.EqualTo((System.Numerics.BigInteger)72));
            int count=feedback.NoticeCount;
            Assert.That(feedback.Observe(12,0,960,0,true,5),Is.False);Assert.That(feedback.VisibleIncreaseUnits,Is.EqualTo(System.Numerics.BigInteger.Zero));
            Assert.That(feedback.Observe(12,1,1920,10,true,6),Is.False);Assert.That(feedback.VisibleIncreaseUnits,Is.EqualTo(System.Numerics.BigInteger.Zero));
            Assert.That(feedback.NoticeCount,Is.EqualTo(count));
            feedback.Observe(12,1,2000,11,false,7);Assert.That(feedback.IsVisible(7),Is.False);
            feedback.Clear();Assert.That(feedback.Observe(12,1,2000,12,true,8),Is.False);Assert.That(feedback.NoticeCount,Is.EqualTo(count));
        }

        [UnityTest] public IEnumerator DragChipShowsPortraitPriceAndValidityAndStaysInsideSafeArea()
        {
            var build=Button("Build class 0");var source=build.GetComponent<BuildDragSource>();
            Vector2 target=default;bool found=false;
            for(int y=80;y<session.Content.Map.Height-80&&!found;y+=47)for(int x=80;x<session.Content.Map.Width-80;x+=53)
            {
                var p=new MapPoint(x,y);
                if(PlacementValidator.ValidatePosition(session.Content,session.State,session.LocalSlot,0,-1,p)!=ErrorCode.None)continue;
                var point=Board.MapPointToScreen(p);
                if(Hit(point)?.GetComponentInParent<BoardView>()!=Board)continue;
                target=point;found=true;break;
            }
            Assert.That(found,Is.True);
            var e=new PointerEventData(EventSystem.current){pointerId=919,button=PointerEventData.InputButton.Left,
                position=Center(build),pressPosition=Center(build),eligibleForClick=true};
            ExecuteEvents.Execute(build.gameObject,e,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(build.gameObject,e,ExecuteEvents.initializePotentialDrag);
            e.position=target;e.dragging=true;ExecuteEvents.Execute(build.gameObject,e,ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(build.gameObject,e,ExecuteEvents.dragHandler);yield return null;Refresh();
            var chip=Field<RectTransform>(ui,"dragChip");Assert.That(chip,Is.Not.Null);
            var portrait=chip.Find("Placement preview portrait").GetComponent<Image>();Assert.That(portrait.sprite,Is.SameAs(GeneratedClassIcons.Get(0,true)));
            Assert.That(Field<Text>(ui,"dragLabel").text,Does.Contain(session.Content.Towers[0].Price+" G"));
            Assert.That(Field<Text>(ui,"dragLabel").text,Does.Contain("✓ Frei"));
            var pointerInSafe=Field<RectTransform>(ui,"safe");
            Assert.That(RectTransformUtility.ScreenPointToLocalPointInRectangle(pointerInSafe,target,null,out var pointerLocal),Is.True);
            var chipArea=new Rect(chip.anchoredPosition-chip.sizeDelta*.5f,chip.sizeDelta);
            var modelHeadArea=new Rect(pointerLocal.x-40,pointerLocal.y-8,80,120);
            Assert.That(chipArea.Overlaps(modelHeadArea),Is.False,"Placement label must leave the nearby upright model visible");
            CompactHud08Tests.CaptureHud("immersive-valid-drag-preview");yield return null;
            var gold=session.State.Players[0].Gold;var safe=Field<RectTransform>(ui,"safe");
            foreach(var point in new[]{safe.rect.min,safe.rect.max,new Vector2(safe.rect.xMin,safe.rect.yMax),new Vector2(safe.rect.xMax,safe.rect.yMin)})
            {
                e.position=ScreenPoint(safe,point);ExecuteEvents.Execute(build.gameObject,e,ExecuteEvents.dragHandler);
                var corners=new Vector3[4];chip.GetWorldCorners(corners);
                foreach(var corner in corners)
                {
                    var local=safe.InverseTransformPoint(corner);
                    Assert.That(local.x,Is.InRange(safe.rect.xMin,safe.rect.xMax-Mathf.Min(156,safe.rect.width*.22f)));Assert.That(local.y,Is.InRange(safe.rect.yMin+Mathf.Min(116,safe.rect.height*.22f),safe.rect.yMax-Mathf.Min(132,safe.rect.height*.22f)));
                }
                Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));Assert.That(session.State.Towers,Is.Empty);
            }
            Assert.That(Field<Text>(ui,"dragLabel").text,Does.Contain("×"));
            Assert.That(Field<Text>(ui,"dragLabel").text,Does.Contain(session.Content.Towers[0].Price+" G"));
            CompactHud08Tests.CaptureHud("immersive-invalid-clamped-drag-preview");
            ui.CancelBuildDrag(source);Assert.That(Field<RectTransform>(ui,"dragChip"),Is.Null);
            Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));Assert.That(session.State.Towers,Is.Empty);
        }
    }
}
