using BigInteger=System.Numerics.BigInteger;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
    public sealed class WaveHud07Tests
    {
        SessionCoordinator session;GameUI ui;Touchscreen touch;
        InputActionAsset isolatedActions;
        ReadOnlyArray<InputDevice>? previousDevices;
        InputSettings.BackgroundBehavior previousBackground;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorRouting;
        bool inputRoutingChanged;
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        static void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
        [UnitySetUp]public IEnumerator Start()
        {
            SceneManager.LoadScene("Boot");yield return new WaitForSecondsRealtime(.2f);
            session=Object.FindFirstObjectByType<SessionCoordinator>();ui=Object.FindFirstObjectByType<GameUI>();
            session.StartOffline(17);session.State.Phase=MatchPhase.Playing;session.State.WavePhase=WavePhase.Preparing;
            session.State.PreparationRemaining=200;session.State.Players[0].Gold=10000;session.State.Players[0].AvailableTowerMask=127;
            session.enabled=false;yield return null;yield return null;Refresh();
        }
        [UnityTearDown]public IEnumerator Stop()
        {
            if(isolatedActions!=null)isolatedActions.devices=previousDevices;
            if(touch!=null&&touch.added)InputSystem.RemoveDevice(touch);
            if(inputRoutingChanged){InputSystem.settings.backgroundBehavior=previousBackground;InputSystem.settings.editorInputBehaviorInPlayMode=previousEditorRouting;}
            session.enabled=true;session.Shutdown();yield return null;
        }
        void Refresh(){Call(ui,"RefreshLive");Canvas.ForceUpdateCanvases();}
        static Button Find(string name)
        {
            Ui16TestNavigation.Reveal(name);foreach(var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))if(b.name==name&&b.gameObject.activeInHierarchy)return b;Assert.Fail("Missing "+name);return null;}
        static Vector2 Center(Component component)
        {var r=(RectTransform)component.transform;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));}
        static Button HitButton(Vector2 position)
        {
            var pointer=new PointerEventData(EventSystem.current){position=position};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            return hits.Count==0?null:hits[0].gameObject.GetComponentInParent<Button>();
        }
        static void Submit(Button button)
        {EventSystem.current.SetSelectedGameObject(button.gameObject);ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);}
        static void Capture(string name)=>CompactHud08Tests.CaptureHud(name);

        [UnityTest]public IEnumerator NarrowHeadersAndAllSevenBuildCardsHaveDistinctRaycastTargets()
        {
            var safe=Field<RectTransform>(ui,"safe");var canvas=(RectTransform)safe.parent;
            foreach(int width in new[]{900,960,1280})
            {
                safe.anchorMin=safe.anchorMax=safe.pivot=new Vector2(.5f,.5f);safe.anchoredPosition=Vector2.zero;safe.sizeDelta=new Vector2(width,720);
                safe.localScale=Vector3.one*Mathf.Min(canvas.rect.width/width,canvas.rect.height/720)*.94f;
                Canvas.ForceUpdateCanvases();Call(ui,"Rebuild");yield return null;Refresh();
                Assert.That(Field<float>(ui,"width"),Is.EqualTo(width).Within(.1f));
                Assert.That(HitButton(Center(Find("Match tempo"))),Is.SameAs(Find("Match tempo")));
                for(int k=0;k<7;k++)
                {
                    var build=Find("Build class "+k);yield return null;Assert.That(build.interactable,Is.True);
                    Assert.That(((RectTransform)build.transform).rect.width,Is.GreaterThanOrEqualTo(44));
                    Assert.That(HitButton(Center(build)),Is.SameAs(build),"Build card overlap "+width+" / "+k);
                }
                for(int k=0;k<4;k++){var send=Find("Send class "+k);yield return null;Assert.That(HitButton(Center(send)),Is.SameAs(send));}
                var tempo=(RectTransform)Find("Match tempo").transform;
                var settings=Find("Battle menu").transform;
                Assert.That(settings,Is.Not.Null);
                var a=new Vector3[4];var b=new Vector3[4];tempo.GetWorldCorners(a);((RectTransform)settings).GetWorldCorners(b);
                Assert.That(a[2].x,Is.LessThan(b[0].x));
                Capture("waves-preparing-"+width);
            }
        }

        [UnityTest]public IEnumerator ModalRejectsKeyboardSubmitAndDirectPurchaseCallback()
        {
            var send=Find("Send class 0");var gold=session.State.Players[0].Gold;int count=session.State.SpawnOrders.Count;
            Call(ui,"OpenSettings");yield return null;
            Submit(send);send.onClick.Invoke();
            Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));Assert.That(session.State.SpawnOrders.Count,Is.EqualTo(count));
            Call(ui,"CloseModal");Refresh();Submit(send);
            Assert.That(session.State.SpawnOrders.Count,Is.EqualTo(count+1));
            yield return null;
        }

        [UnityTest]public IEnumerator DisableClosesModalAndReopenedDialogKeepsNavigationInside()
        {
            Call(ui,"OpenSettings");yield return null;
            ui.enabled=false;Assert.That(Field<RectTransform>(ui,"modal"),Is.Null);
            ui.enabled=true;yield return null;Call(ui,"OpenSettings");yield return null;
            var modal=Field<RectTransform>(ui,"modal");var board=Field<BoardView>(ui,"ownBoard");float zoom=board.ZoomFactor;
            Submit(Find("Zoom in"));Assert.That(board.ZoomFactor,Is.EqualTo(zoom));
            var first=modal.GetComponentInChildren<Button>();EventSystem.current.SetSelectedGameObject(first.gameObject);
            for(int i=0;i<10;i++)
            {
                var e=new AxisEventData(EventSystem.current){moveDir=MoveDirection.Down,moveVector=Vector2.down};
                ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject,e,ExecuteEvents.moveHandler);
                Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(modal),Is.True);
            }
        }

        [UnityTest]public IEnumerator ResearchBecomesReachableByMoveAfterGoldArrives()
        {
            session.State.Players[0].Gold=0;Call(ui,"ToggleAttackInfo",0);Refresh();yield return null;
            var close=Find("Close attack info");var research=Find("Research troop");
            Assert.That(research.interactable,Is.False);EventSystem.current.SetSelectedGameObject(close.gameObject);
            session.State.Players[0].Gold=10000;Refresh();
            var move=new AxisEventData(EventSystem.current){moveDir=MoveDirection.Down,moveVector=Vector2.down};
            var panel=Field<RectTransform>(ui,"attackInfoContent");var visited=new HashSet<GameObject>();
            int limit=panel.GetComponentsInChildren<Selectable>().Length;
            for(int step=0;step<limit&&EventSystem.current.currentSelectedGameObject!=research.gameObject;step++)
            {
                var current=EventSystem.current.currentSelectedGameObject;
                Assert.That(visited.Add(current),Is.True,"Navigation cycles before reaching the purchase action");
                ExecuteEvents.Execute(current,move,ExecuteEvents.moveHandler);
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.Not.Null);
                Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(panel),Is.True,"Focus escaped the information panel");
            }
            Assert.That(visited.Contains(Find("Research exact values").gameObject),Is.True,"Exact-price details must remain keyboard reachable");
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(research.gameObject));
            ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            Assert.That(session.State.Players[0].ResearchLevels[0],Is.EqualTo((BigInteger)1));
        }

        [UnityTest]public IEnumerator ResearchInsideInfoIsAllowedButUnderlyingSendAndStaleSendQuoteAreNot()
        {
            var send=Find("Send class 0");Call(ui,"ToggleAttackInfo",0);Refresh();yield return null;
            var gold=session.State.Players[0].Gold;var quote=UnitResearchRules.Quote(session.Content,session.State.Players[0],0);
            Submit(send);Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));
            yield return new WaitForSecondsRealtime(.25f);Refresh();
            Assert.That(Field<BattleInfoPanel>(ui,"attackInfo").Progress,Is.EqualTo(1).Within(.01f));
            Assert.That(Field<Text>(ui,"attackInfoBody").text,Does.Contain("Tempo"));
            Capture("waves-research-info-before-purchase");
            Submit(Find("Research troop"));Refresh();
            Assert.That(session.State.Players[0].ResearchLevels[0],Is.EqualTo((BigInteger)1));
            Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold-quote.Price));
            Assert.That(session.State.SpawnOrders,Is.Empty);
            Call(ui,"CloseAttackInfo");Refresh();
            var old=Field<SendQuote[]>(ui,"displayedSendQuotes")[0];
            session.Submit(CommandType.ResearchUnit,0,-1,0,0,0,1);var afterResearch=session.State.Players[0].Gold;
            Submit(send); // Deliberately do not refresh the old, displayed send quote first.
            Assert.That(session.LastResult.Code,Is.EqualTo(ErrorCode.QuoteChanged));
            Assert.That(session.State.Players[0].Gold,Is.EqualTo(afterResearch));Assert.That(session.State.SpawnOrders,Is.Empty);
            Assert.That(old.ResearchLevel,Is.EqualTo((BigInteger)1));Refresh();Submit(send);
            Assert.That(session.State.SpawnOrders[0].ResearchLevel,Is.EqualTo((BigInteger)2));
            yield return null;
        }

        [UnityTest]public IEnumerator TargetWaveRaceRequiresNewTapAndQueuesKeepBoughtLevel()
        {
            Refresh();var send=Find("Send class 0");
            for(int i=0;i<10;i++)
            {var q=SendRules.Quote(session.Content,session.State,session.LocalSlot,1);session.Submit(CommandType.SendUnit,1,-1,0,0,q.WaveId,q.ResearchLevel,q.EnemyTarget);Assert.That(session.LastResult.Accepted,Is.True);}
            session.State.WavePhase=WavePhase.Clearing; // A phase transition invalidates the displayed wave quote.
            var gold=session.State.Players[0].Gold;Submit(send);
            Assert.That(session.LastResult.Code,Is.EqualTo(ErrorCode.QuoteChanged));Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));
            Assert.That(session.State.SpawnOrders.Count,Is.EqualTo(10));
            Refresh();Submit(send);Assert.That(session.State.SpawnOrders[10].WaveId,Is.EqualTo((System.Numerics.BigInteger)2));
            session.State.Tick+=session.Content.TickRate;session.Submit(CommandType.ResearchUnit,0,-1,0,0,0,0);Assert.That(session.LastResult.Accepted,Is.True);Refresh();
            Assert.That(session.State.SpawnOrders[10].ResearchLevel,Is.EqualTo(BigInteger.Zero));
            Assert.That(Field<Text>(ui,"fifoText").text,Does.Contain("11 Pakete · 258 G gebunden"));
            Capture("waves-reserved-fifo");
            yield return null;
        }

        [UnityTest]public IEnumerator PreparationVotesMineDecimalsAndPriorityUseAuthoritativeState()
        {
            Submit(Find("Wave ready"));Refresh();
            Assert.That(session.State.Players[0].WaveReady,Is.True);Assert.That(session.State.Players[1].WaveReady,Is.True);
            Assert.That(session.State.WavePhase,Is.EqualTo(WavePhase.Preparing));
            int[] ids=new int[2];
            for(int n=0;n<2;n++)
            {
                int kind=n==0?4:0;bool built=false;
                for(int y=80;y<800&&!built;y+=67)for(int x=80;x<1500;x+=73)
                {
                    var p=new MapPoint(x,y);
                    if(PlacementValidator.ValidatePosition(session.Content,session.State,session.LocalSlot,kind,-1,p)!=ErrorCode.None)continue;
                    session.Submit(CommandType.BuildTower,kind,-1,x,y);Assert.That(session.LastResult.Accepted,Is.True);
                    ids[n]=session.State.Towers[session.State.Towers.Count-1].Id;built=true;break;
                }
                Assert.That(built,Is.True);
            }
            session.Submit(CommandType.UpgradeTower,(int)TowerUpgradePath.Income,ids[0]);Assert.That(session.LastResult.Accepted,Is.True);
            Call(ui,"SelectEntity",ids[0]);Refresh();Assert.That(Field<Text>(ui,"selectionText").text,Does.Contain("1,1"));Capture("waves-mine-income");
            var tower=session.State.Towers.Find(t=>t.Id==ids[1]);tower.CooldownTicks=7;
            Call(ui,"SelectEntity",tower.Id);Refresh();Submit(Find("Tower target priority"));Refresh();
            Assert.That(tower.Priority,Is.EqualTo(TargetPriority.Strongest));Assert.That(tower.CooldownTicks,Is.EqualTo(7));Capture("waves-target-priority");
            yield return null;
        }

        [UnityTest]public IEnumerator StaleResearchButtonCannotBuyAnotherLevel()
        {
            Call(ui,"ToggleAttackInfo",0);Refresh();yield return null;
            var button=Find("Research troop");
            session.Submit(CommandType.ResearchUnit,0,-1,0,0,0,0);Assert.That(session.LastResult.Accepted,Is.True);
            var gold=session.State.Players[0].Gold;var sequence=session.LastResult.Sequence;
            Submit(button);
            Assert.That(session.LastResult.Sequence,Is.EqualTo(sequence),"Stale displayed research must be discarded without any new command");
            Assert.That(session.State.Players[0].ResearchLevels[0],Is.EqualTo((BigInteger)1));
            Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));
            // A stale request that really reaches authority must still reject its exact old quote.
            session.Submit(CommandType.ResearchUnit,0,-1,expectedResearch:BigInteger.Zero);
            Assert.That(session.LastResult.Code,Is.EqualTo(ErrorCode.QuoteChanged));
            Assert.That(session.State.Players[0].ResearchLevels[0],Is.EqualTo((BigInteger)1));
            Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));
            yield return null;
        }
        [UnityTest]public IEnumerator PaidQueueSeparatesKnownMinimumDelayFromUnknownWaveAndPreparationWait()
        {
            session.Submit(CommandType.SendUnit,0,-1,0,0,1,0,(int)session.State.Players[(int)session.LocalSlot].AttackTarget);Assert.That(session.LastResult.Accepted,Is.True);
            var head=session.State.SpawnOrders[0];head.WaveId=2;Refresh();
            var copy=Field<Text>(ui,"fifoText").text;
            Assert.That(copy,Does.Contain("1 Paket · 18 G gebunden"));
            Assert.That(copy,Does.Contain("nach Wellenwechsel · Zeitpunkt offen"));
            Assert.That(copy,Does.Not.Contain("≥"),"Unknown next-wave timing must not become a numeric promise");
            head.WaveId=1;Refresh();Assert.That(Field<Text>(ui,"fifoText").text,Does.Contain("Start: ab Wellenstart"));
            session.State.WavePhase=WavePhase.Active;
            // Three and a half known seconds must round down to the advertised lower bound.
            session.State.NextPaidSpawnTicks[0]=session.State.PlayTicks+3*session.Content.TickRate+session.Content.TickRate/2;
            Refresh();Assert.That(Field<Text>(ui,"fifoText").text,Does.Contain("Start: ≥3 s Spielzeit"));
            session.State.NextPaidSpawnTicks[0]=0;session.State.NextSpawnOrigins[1]=UnitOrigin.NeutralWave;Refresh();
            Assert.That(Field<Text>(ui,"fifoText").text,Does.Contain("Start: ≥0 s Spielzeit"));
            Submit(Find("Own paid attack queue"));Refresh();
            var detail=Field<Text>(ui,"paidQueueDetailText").text;
            Assert.That(detail,Does.Contain("Untergrenze · Grundwelle kann verzögern"),"Neutral priority must qualify the optimistic forecast");
            Assert.That(detail,Does.Contain("Noch gebunden: 18 G"));Call(ui,"CloseModal");Refresh();
            for(int i=0;i<100;i++)session.State.Units.Add(new UnitState{Id=i+1,Hp=1,Defender=PlayerSlot.One,Origin=i<80?UnitOrigin.PaidPvP:UnitOrigin.NeutralWave});
            session.State.NextPaidSpawnTicks[0]=session.State.PlayTicks+3*session.Content.TickRate+session.Content.TickRate/2;Refresh();
            var wait=Field<Text>(ui,"fifoText").text;
            Assert.That(wait,Does.Contain("Start: ≥3 s Spielzeit"),"Field population must not invent a removed capacity gate");
            Assert.That(wait,Does.Not.Contain("Angriffslimit"));Assert.That(wait,Does.Not.Contain("Gegnerfeld voll"));
            yield return null;
        }
        [UnityTest]public IEnumerator HomeDifficultyAndUnlockRolesAreVisible()
        {
            session.Shutdown();yield return null;yield return null;Call(ui,"Rebuild");yield return null;
            Assert.That(ui.CurrentScreen,Is.EqualTo("home"));
            Submit(Find("Solo difficulty Schwer"));Assert.That(session.Difficulty,Is.EqualTo(BotDifficulty.Hard));
            yield return null;
            Submit(Find("Home arsenal"));yield return null;
            var labels=Field<Text[]>(ui,"unlockLabels");
            Assert.That(labels[0].text,Does.Contain("Kettenblitze"));Assert.That(labels[1].text,Does.Contain("Durchschuss"));
            Capture("home-difficulty-unlock-roles");
        }

        IEnumerator Touch(int id,UnityEngine.InputSystem.TouchPhase phase,Vector2 point)
        {InputSystem.QueueStateEvent(touch,new TouchState{touchId=id,phase=phase,position=point});yield return null;yield return null;}
        [UnityTest]public IEnumerator RawTouchCancellationAndSecondContactCannotBuyThroughDrag()
        {
            previousBackground=InputSystem.settings.backgroundBehavior;previousEditorRouting=InputSystem.settings.editorInputBehaviorInPlayMode;inputRoutingChanged=true;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            touch=InputSystem.AddDevice<Touchscreen>();
            var module=EventSystem.current.GetComponent<InputSystemUIInputModule>();
            isolatedActions=module.actionsAsset;previousDevices=isolatedActions.devices;isolatedActions.devices=new InputDevice[]{touch};
            yield return null;
            var board=Field<BoardView>(ui,"ownBoard");Vector2 ground=default;bool found=false;
            for(int y=300;y<session.Content.Map.Height-180&&!found;y+=61)for(int x=300;x<session.Content.Map.Width-240;x+=67)
            {
                var p=new MapPoint(x,y);if(PlacementValidator.ValidatePosition(session.Content,session.State,session.LocalSlot,0,-1,p)!=ErrorCode.None)continue;
                var screen=board.MapPointToScreen(p);var e=new PointerEventData(EventSystem.current){position=screen};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
                if(hits.Count>0&&hits[0].gameObject.GetComponentInParent<BoardView>()==board){ground=screen;found=true;break;}
            }
            Assert.That(found,Is.True);var build=Center(Find("Build class 0"));var send=Center(Find("Send class 0"));
            var probe=Find("Build class 0").gameObject.AddComponent<DragInputProbe>();
            var gold=session.State.Players[0].Gold;
            yield return Touch(1,UnityEngine.InputSystem.TouchPhase.Began,build);
            Assert.That(probe.Downs,Is.EqualTo(1));Assert.That(probe.DeviceId,Is.EqualTo(touch.deviceId));
            yield return Touch(1,UnityEngine.InputSystem.TouchPhase.Moved,ground);
            Assert.That(probe.Begins,Is.EqualTo(1));Assert.That(probe.Drags,Is.GreaterThan(0));
            Assert.That(Field<bool>(ui,"dragging"),Is.True,"Raw touch must enter actual drag handler");
            yield return Touch(2,UnityEngine.InputSystem.TouchPhase.Began,send);
            yield return Touch(2,UnityEngine.InputSystem.TouchPhase.Ended,send);
            Assert.That(session.State.SpawnOrders,Is.Empty);
            yield return Touch(1,UnityEngine.InputSystem.TouchPhase.Canceled,ground);
            Assert.That(session.State.Towers,Is.Empty);Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));
            yield return Touch(3,UnityEngine.InputSystem.TouchPhase.Began,build);
            yield return Touch(3,UnityEngine.InputSystem.TouchPhase.Moved,ground);
            yield return Touch(3,UnityEngine.InputSystem.TouchPhase.Ended,ground);
            Assert.That(session.State.Towers.Count,Is.EqualTo(1));Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold-session.Content.Towers[0].Price));
        }
    }
}
