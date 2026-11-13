using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using BigInteger=System.Numerics.BigInteger;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class Screens15Tests
    {
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        GameUI ui;SessionCoordinator session;Component progress;string profileDirectory;
        static T Field<T>(object x,string name)=>(T)x.GetType().GetField(name,Private).GetValue(x);
        static object Call(object x,string name,params object[] args)=>x.GetType().GetMethod(name,Private).Invoke(x,args);
        Button Button(string name)=>ui.GetComponentsInChildren<Button>().Single(b=>b.name==name);
        [UnitySetUp] public IEnumerator Setup()
        {yield return SceneManager.LoadSceneAsync("Boot");yield return null;yield return null;ui=Object.FindFirstObjectByType<GameUI>();session=Object.FindFirstObjectByType<SessionCoordinator>();
            progress=Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(x=>x.GetType().FullName=="DefenseDuell.Progression.LocalProgress");
            profileDirectory=Path.Combine(Path.GetTempPath(),"DefenseDuell-screens15-"+Guid.NewGuid().ToString("N"));
            progress.GetType().GetMethod("Initialize").Invoke(progress,new object[]{session,Path.Combine(profileDirectory,"profile.save")});
        }
        [UnityTearDown] public IEnumerator Cleanup(){Call(ui,"CloseModal");session.enabled=true;session.Shutdown();Object.Destroy(ui.gameObject);Object.Destroy(progress);yield return null;if(Directory.Exists(profileDirectory))Directory.Delete(profileDirectory,true);}
        [UnityTest] public IEnumerator MenuRulesCarryExactWaveEndlessIncomeToRealSoloAndKeepSoloDuel()
        {
            Button("Home match rules").onClick.Invoke();yield return null;
            var waves=ui.GetComponentsInChildren<InputField>().Single(x=>x.name=="Menu wave count");waves.text="201";
            Button("Menu apply rules").onClick.Invoke();Assert.That(Field<RectTransform>(ui,"modal"),Is.Not.Null);
            waves.text="200";Button("Menu endless").onClick.Invoke();Button("Menu income 5").onClick.Invoke();Button("Menu format 3").onClick.Invoke();
            CompactHud08Tests.CaptureHud("screens15-menu-rules");
            Button("Menu apply rules").onClick.Invoke();Assert.That(Field<RectTransform>(ui,"modal"),Is.Null);
            Button("Home solo").onClick.Invoke();yield return null;Ui16TestNavigation.ConfirmHomeIfOpen();yield return null;
            Assert.That(session.Content.MatchRules.WaveCount,Is.EqualTo(200));Assert.That(session.Content.MatchRules.Endless,Is.True);
            Assert.That(session.Content.WaveCount,Is.Zero);Assert.That(session.Content.MatchRules.IncomeMultiplier,Is.EqualTo(5));
            Assert.That(session.Content.MatchRules.Format,Is.EqualTo(MatchFormat.Duel));Assert.That(session.State.Players.Length,Is.EqualTo(2));
            Assert.That(session.Content.ContainsWave(BigInteger.Parse("214748364800")),Is.True);
        }
        [Test] public void CompleteRulesParticipateInDraftRevisionAndReadonlyGuestBinding()
        {
            var cap=new LobbyCapabilities(1,200,200);
            var initial=new LobbyMatchOptions(200,"900719925474099312345",true,5,MatchFormat.TwoVsTwoVsTwo,"emerald-basin");
            var editor=new LobbySettingsEditor();editor.BeginSession("15");
            Assert.That(editor.Receive(new LobbySettingsView("15",initial,2,3,true,true,cap,1,MatchPhase.Lobby)),Is.True);
            Assert.That(editor.EditRules(false,10,MatchFormat.ThreeVsThree,"aurora-bay"),Is.True);var request=editor.Apply();
            Assert.That(request.Options.StartGoldDecimal,Is.EqualTo(initial.StartGoldDecimal));Assert.That(request.Options.Endless,Is.False);
            Assert.That(request.Options.IncomeMultiplier,Is.EqualTo(10));Assert.That(request.Options.Format,Is.EqualTo(MatchFormat.ThreeVsThree));
            Assert.That(request.Options.MapId,Is.EqualTo("aurora-bay"));Assert.That(request.ExpectedConfigRevision,Is.EqualTo(2UL));
            Assert.That(editor.Resolve("15",request.RequestId,true,3),Is.True);Assert.That(editor.HasPending,Is.True);
            Assert.That(editor.Receive(new LobbySettingsView("15",request.Options,3,4,false,true,cap,3,MatchPhase.Lobby)),Is.True);
            Assert.That(editor.CanEdit,Is.False);Assert.That(editor.EditRules(true,1,MatchFormat.Duel,"meadow"),Is.False);
            var ready=editor.Ready();Assert.That(ready.ExpectedConfigRevision,Is.EqualTo(3UL));Assert.That(ready.ExpectedRosterRevision,Is.EqualTo(4UL));
            Assert.That(editor.Receive(new LobbySettingsView("15",initial,2,3,false,true,cap,2,MatchPhase.Lobby)),Is.False);
        }
        [UnityTest] public IEnumerator EveryClassCanBeSelectedInSoloLobbyAndLastClassCannotBeRemoved()
        {
            session.StartOffline(15);yield return null;yield return null;
            Assert.That(session.State.Phase,Is.EqualTo(MatchPhase.Lobby));Assert.That(session.Content.Towers.Length,Is.EqualTo(8));
            for(int kind=0;kind<session.Content.Towers.Length;kind++)Assert.That(Button("Lobby loadout "+kind),Is.Not.Null);
            // Make room within the five-choice limit, then select laser and deselect the remaining defaults.
            Button("Lobby loadout 4").onClick.Invoke();Call(ui,"RefreshLive");Button("Lobby loadout 7").onClick.Invoke();Call(ui,"RefreshLive");
            for(int kind=0;kind<4;kind++){Button("Lobby loadout "+kind).onClick.Invoke();Call(ui,"RefreshLive");}
            Assert.That(session.State.Players[0].AvailableTowerMask,Is.EqualTo(128));
            var revision=session.State.RosterRevision;Button("Lobby loadout 7").onClick.Invoke();Call(ui,"RefreshLive");
            Assert.That(session.State.Players[0].AvailableTowerMask,Is.EqualTo(128));Assert.That(session.State.RosterRevision,Is.EqualTo(revision));
            Button("Lobby loadout 0").onClick.Invoke();Call(ui,"RefreshLive");Assert.That(session.State.Players[0].AvailableTowerMask,Is.EqualTo(129));
        }
        [UnityTest] public IEnumerator ResultAndDisconnectActionsFitNarrowSafeAreaAndRemainDark()
        {
            session.StartOffline(15);session.enabled=false;yield return null;
            ui.enabled=false;var safe=Field<RectTransform>(ui,"safe");safe.anchorMin=safe.anchorMax=new Vector2(.5f,.5f);safe.sizeDelta=new Vector2(900,720);Canvas.ForceUpdateCanvases();
            session.State.Phase=MatchPhase.Finished;session.State.Outcome=MatchOutcome.Player0;session.State.WinningTeamId=session.State.Players[0].TeamId;
            foreach(string page in new[]{"result","lost","connecting"})
            {
                typeof(GameUI).GetField("page",Private).SetValue(ui,page);Call(ui,"Rebuild");yield return null;Canvas.ForceUpdateCanvases();
                foreach(var control in ui.GetComponentsInChildren<Button>())
                {
                    var corners=new Vector3[4];((RectTransform)control.transform).GetWorldCorners(corners);
                    foreach(var point in corners){var p=safe.InverseTransformPoint(point);Assert.That(p.x,Is.InRange(safe.rect.xMin-.5f,safe.rect.xMax+.5f),control.name);Assert.That(p.y,Is.InRange(safe.rect.yMin-.5f,safe.rect.yMax+.5f),control.name);}
                }
                Assert.That(Field<Image>(ui,"canvasBackground").color.r,Is.LessThan(.1f));
                CompactHud08Tests.CaptureHud("screens15-"+page);
                Call(ui,"OpenSettings");yield return null;CompactHud08Tests.CaptureHud("screens15-"+page+"-settings");Assert.That(Field<RectTransform>(ui,"modal"),Is.Not.Null);Call(ui,"CloseModal");
            }
            session.enabled=true;ui.enabled=true;
        }
        [UnityTest] public IEnumerator SixPlayerLobbyAndTargetPickerUseActualCoreSlotsAndBindPaidTarget()
        {
            Assert.That(session.ConfigureNextMatch(new MatchRules(3,1000,false,2,MatchFormat.TwoVsTwoVsTwo,session.Content.Map.Id)),Is.True);
            ushort port;using(var sock=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp)){sock.Bind(new IPEndPoint(IPAddress.Loopback,0));port=(ushort)((IPEndPoint)sock.LocalEndPoint).Port;}
            session.StartLanHost(port);double deadline=Time.realtimeSinceStartupAsDouble+5;
            while(ui.CurrentScreen!="lobby"&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Assert.That(ui.CurrentScreen,Is.EqualTo("lobby"));Assert.That(session.State.Players.Length,Is.EqualTo(6));
            CompactHud08Tests.CaptureHud("screens15-six-slot-lobby");
            Assert.That(Field<Text[]>(ui,"rosterLabels").Length,Is.EqualTo(6));Assert.That(Button("Lobby rules primary").interactable,Is.False,"No fake peers or bot filling");
            Button("Lobby team 2").onClick.Invoke();Call(ui,"RefreshLive");Assert.That(session.State.Players[0].TeamId,Is.EqualTo(2));
            Button("Lobby team 0").onClick.Invoke();Call(ui,"RefreshLive");Assert.That(session.State.Players[0].TeamId,Is.Zero);
            session.enabled=false;var simulation=Field<MatchSimulation>(session,"simulation");
            // Presentation fixture: manufacture connected remote core participants explicitly.
            // This validates UI-to-authority binding, not six actual transport connections.
            for(int i=1;i<6;i++)Assert.That(simulation.ConfigureConnection((PlayerSlot)i,true),Is.True);
            typeof(SessionCoordinator).GetProperty("ConnectedPlayers").SetValue(session,6);
            for(int i=1;i<6;i++)Assert.That(simulation.Execute((PlayerSlot)i,new CommandEnvelope{SessionId=simulation.State.SessionId,Sequence=1,Type=CommandType.SetReady,Kind=0,Target=-1,ExpectedConfigRevision=simulation.State.ConfigRevision,ExpectedRosterRevision=simulation.State.RosterRevision}).Accepted,Is.True);
            session.SetLobbyReady(true,session.State.SessionId,session.ConfigRevision,session.RosterRevision);
            Assert.That(session.State.Phase,Is.EqualTo(MatchPhase.Countdown));
            for(int tick=0;tick<session.Content.TickRate*10&&session.State.Phase==MatchPhase.Countdown;tick++)simulation.Step();
            Assert.That(session.State.Phase,Is.EqualTo(MatchPhase.Playing));yield return null;yield return null;Call(ui,"RefreshLive");
            int original=(int)session.State.Players[0].AttackTarget;
            Button("Choose attack target").onClick.Invoke();Call(ui,"RefreshLive");int selected=(int)session.State.Players[0].AttackTarget;
            Assert.That(selected,Is.Not.EqualTo(original));Assert.That(session.State.Players[selected].TeamId,Is.Not.EqualTo(session.State.Players[0].TeamId));
            Button("Send class 0").onClick.Invoke();Call(ui,"RefreshLive");
            Assert.That(session.LastResult.Accepted,Is.True);Assert.That(session.State.SpawnOrders.Last().EnemyTarget,Is.EqualTo((PlayerSlot)selected));
            Button("Choose attack target").onClick.Invoke();Call(ui,"RefreshLive");
            Assert.That(session.State.SpawnOrders.Last().EnemyTarget,Is.EqualTo((PlayerSlot)selected),"Changing the next target must not retarget an already paid packet");
            Assert.That(ui.GetComponentsInChildren<BoardView>().Length,Is.EqualTo(2),"One main field and one PIP, not six render cameras");
            CompactHud08Tests.CaptureHud("screens15-six-slot-target");
        }

        [UnityTest] public IEnumerator RealTroopSwipeCannotSendOrResearchUntilASeparatePurchaseTap()
        {
            session.StartOffline(15);session.SetLobbyReady(true,session.State.SessionId,session.ConfigRevision,session.RosterRevision);session.enabled=false;
            var simulation=Field<MatchSimulation>(session,"simulation");for(int tick=0;tick<session.Content.TickRate*10&&session.State.Phase==MatchPhase.Countdown;tick++)simulation.Step();
            Assert.That(session.State.Phase,Is.EqualTo(MatchPhase.Playing));session.State.Players[0].Gold=1000;
            yield return null;yield return null;Call(ui,"RefreshLive");Canvas.ForceUpdateCanvases();
            var send=Button("Send class 0");var rect=(RectTransform)send.transform;
            var start=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var finish=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center+Vector2.left*80));
            var e=new PointerEventData(EventSystem.current){pointerId=1515,position=start,pressPosition=start,button=PointerEventData.InputButton.Left,eligibleForClick=true};
            var gold=session.State.Players[0].Gold;var level=session.State.Players[0].ResearchLevels[0];int orders=session.State.SpawnOrders.Count;
            ExecuteEvents.Execute(send.gameObject,e,ExecuteEvents.pointerDownHandler);e.position=finish;
            ExecuteEvents.Execute(send.gameObject,e,ExecuteEvents.dragHandler);ExecuteEvents.Execute(send.gameObject,e,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(send.gameObject,e,ExecuteEvents.pointerClickHandler);
            Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));Assert.That(session.State.SpawnOrders.Count,Is.EqualTo(orders));Assert.That(session.State.Players[0].ResearchLevels[0],Is.EqualTo(level));
            Assert.That(Field<BattleInfoPanel>(ui,"attackInfo").Open,Is.True);yield return new WaitForSecondsRealtime(.25f);Call(ui,"RefreshLive");
            CompactHud08Tests.CaptureHud("screens15-swipe-research");
            var research=Button("Research troop");var quote=UnitResearchRules.Quote(session.Content,session.State.Players[0],0);Assert.That(research.interactable,Is.True);
            ExecuteEvents.Execute(research.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);Call(ui,"RefreshLive");
            Assert.That(session.LastResult.Accepted,Is.True);Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold-quote.Price));Assert.That(session.State.Players[0].ResearchLevels[0],Is.EqualTo(level+1));Assert.That(session.State.SpawnOrders.Count,Is.EqualTo(orders));
        }

        [Test] public void TowerPortraitKeysKeepHighPathsDistinctAndCacheBoundedWithoutDestroyingLiveSprite()
        {
            var low=GeneratedClassIcons.GetTowerEvolution(0,1,0);var live=new GameObject("Live portrait",typeof(RectTransform),typeof(Image));live.GetComponent<Image>().sprite=low;
            try
            {
                foreach(int kind in new[]{0,1,2,3,4,5,6,7})foreach(int level in new[]{0,1,2,3,4,5,6,7,8,9,10,20,30,50,100,200})GeneratedClassIcons.GetTowerEvolution(kind,level,kind==4?0:level/2);
                Assert.That(GeneratedClassIcons.CachedEvolutionTowerCount,Is.LessThanOrEqualTo(64));Assert.That(live.GetComponent<Image>().sprite,Is.SameAs(low));
                Assert.That(GeneratedClassIcons.GetTowerEvolution(7,100,100),Is.Not.Null);
            }
            finally{Object.DestroyImmediate(live);GeneratedClassIcons.TrimEvolutionCache();}
        }
    }
}
