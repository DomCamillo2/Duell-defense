using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Net;
using System.Reflection;
using System.Numerics;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
using Vector2=UnityEngine.Vector2;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class IncomeInitialization15Tests
    {
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,Private).GetValue(value);
        [Test] public void NewUiAttachesToAnExistingSessionBeforeItsContentCacheExists()
        {
            var root=new GameObject("Existing session before UI");var session=root.AddComponent<SessionCoordinator>();
            try
            {
                session.Initialize(GameContent.CreateDefault());session.enabled=false;
                Assert.That(session.ConfigureNextMatch(new MatchRules(10,180,incomeMultiplier:5,mapId:session.Content.Map.Id)),Is.True);
                session.StartOffline();Assert.That(session.State,Is.Not.Null);
                var ui=root.AddComponent<GameUI>();Assert.That(Field<GameContent>(ui,"content"),Is.Null);
                Assert.DoesNotThrow(()=>ui.Initialize(session));
                Assert.That(Field<GameContent>(ui,"content"),Is.SameAs(session.Content));
                Assert.That(session.State.Players.All(p=>p.Gold.IsZero),Is.True,"UI initialization never grants starting gold");
                var feedback=Field<AttackIncomeFeedback>(ui,"attackIncomeFeedback");
                Assert.That(feedback.VisibleIncreaseUnits,Is.EqualTo(BigInteger.Zero));
            }
            finally{session.Shutdown();Object.DestroyImmediate(root);}
        }
        [Test] public void IncomeResetUsesSessionFactorInsteadOfAStaleRenderCacheAndClearsWithoutSession()
        {
            var root=new GameObject("Income baseline source");var session=root.AddComponent<SessionCoordinator>();var ui=root.AddComponent<GameUI>();
            try
            {
                session.Initialize(GameContent.CreateDefault());session.enabled=false;
                session.ConfigureNextMatch(new MatchRules(10,180,incomeMultiplier:5,mapId:session.Content.Map.Id));session.StartOffline();
                // Presentation-only arranged credit: this does not claim a legal replicated ledger or a played match.
                session.State.Players[0].AttackIncomeUnits=192;
                typeof(GameUI).GetField("session",Private).SetValue(ui,session);
                typeof(GameUI).GetField("content",Private).SetValue(ui,GameContent.CreateDefault());
                var reset=typeof(GameUI).GetMethod("ResetIncomeFeedback",Private);reset.Invoke(ui,null);
                var feedback=Field<AttackIncomeFeedback>(ui,"attackIncomeFeedback");
                Assert.That(feedback.Observe(session.State.SessionId,0,960,1,true,0),Is.False,"Unchanged effective credit must not replay an income reward");
                Assert.That(feedback.Observe(session.State.SessionId,0,1000,2,true,1),Is.True);
                Assert.That(feedback.VisibleIncreaseUnits,Is.EqualTo(new BigInteger(40)));
                typeof(GameUI).GetField("session",Private).SetValue(ui,null);Assert.DoesNotThrow(()=>reset.Invoke(ui,null));
                Assert.That(feedback.VisibleIncreaseUnits,Is.EqualTo(BigInteger.Zero));
            }
            finally{session.Shutdown();Object.DestroyImmediate(root);}
        }
    }

    public sealed class DiscoveryPresentation15Tests
    {
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        SessionCoordinator session;GameUI ui;LanLobbyDirectory directory;Component progress;string profileDirectory;
        static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,Private).GetValue(value);
        void Refresh()=>typeof(GameUI).GetMethod("RefreshDiscovery",Private).Invoke(ui,null);
        Button Button(string name)=>ui.GetComponentsInChildren<Button>(true).Single(b=>b.name==name&&b.gameObject.activeInHierarchy);
        RectTransform Banner=>Field<RectTransform>(ui,"discoveryBanner");
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Boot");yield return null;yield return null;
            session=Object.FindFirstObjectByType<SessionCoordinator>();ui=Object.FindFirstObjectByType<GameUI>();
            Assert.That(session.Status,Is.EqualTo(ConnectionState.Idle));
            progress=Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(c=>c.GetType().FullName=="DefenseDuell.Progression.LocalProgress");
            profileDirectory=Path.Combine(Path.GetTempPath(),"DefenseDuell-discovery-ui-"+Guid.NewGuid().ToString("N"));
            progress.GetType().GetMethod("Initialize").Invoke(progress,new object[]{session,Path.Combine(profileDirectory,"profile.save")});
            // Deterministic received advertisements; no sockets, external room or automatic join.
            session.enabled=false;session.LanDiscoveryEnabled=false;
            var discovery=Field<LanLobbyDiscovery>(session,"discovery");directory=Field<LanLobbyDirectory>(discovery,"directory");
            Refresh();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {if(session!=null){session.Shutdown();session.enabled=true;}if(progress!=null)Object.Destroy(progress);yield return null;if(profileDirectory!=null&&Directory.Exists(profileDirectory))Directory.Delete(profileDirectory,true);}
        void Advertise(ulong epoch=1)
        {
            var a=new LanLobbyAdvertisement{ProcessId=new Guid("12792284-d4e1-4ee9-a80f-572f1ef4be77"),Epoch=epoch,Sequence=1,
                CatalogHash=session.BaseCatalogHash,Port=19179,MapName="Testkarte",FormatName="1v1",PlayerCount=1,Capacity=2,Joinable=true};
            Assert.That(directory.Receive(LanDiscoveryProtocol.Encode(a),IPAddress.Loopback,Time.realtimeSinceStartupAsDouble),Is.True);Refresh();
        }
        static void Tap(Button button)
        {
            var rect=(RectTransform)button.transform;var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var e=new PointerEventData(EventSystem.current){position=point,pressPosition=point,pointerId=1515,button=PointerEventData.InputButton.Left,eligibleForClick=true};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
            Assert.That(hits,Is.Not.Empty);Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(button));
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,e,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,e,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,e,ExecuteEvents.pointerClickHandler);
        }
        [UnityTest] public IEnumerator ReceivedRoomShowsNativeJoinAndLaterControlsWithoutAutojoin()
        {
            Advertise();yield return null;yield return null;Canvas.ForceUpdateCanvases();
            Assert.That(Banner.gameObject.activeInHierarchy,Is.True);Assert.That(Field<RectTransform>(ui,"modal"),Is.Null);
            Assert.That(Button("LAN discovery join").interactable,Is.True);Assert.That(session.State,Is.Null);
            Assert.That(session.Status,Is.EqualTo(ConnectionState.Idle));
            Tap(Button("LAN discovery dismiss"));Assert.That(Banner.gameObject.activeInHierarchy,Is.False);
            Refresh();Assert.That(Banner.gameObject.activeInHierarchy,Is.False);Assert.That(session.State,Is.Null);
            Advertise(2);yield return null;Assert.That(Banner.gameObject.activeInHierarchy,Is.True,"A distinct room epoch remains discoverable");
        }
        [UnityTest] public IEnumerator BannerNeverOpensAModalAndIsHiddenDuringSettingsAndMatch()
        {
            Advertise();yield return null;
            typeof(GameUI).GetMethod("OpenSettings",Private).Invoke(ui,null);Refresh();
            Assert.That(Field<RectTransform>(ui,"modal"),Is.Not.Null);Assert.That(Banner.gameObject.activeInHierarchy,Is.False);
            typeof(GameUI).GetMethod("CloseModal",Private).Invoke(ui,null);Refresh();Assert.That(Banner.gameObject.activeInHierarchy,Is.True);
            session.StartOffline();yield return null;yield return null;Refresh();
            Assert.That(session.LanDiscoveryEnabled,Is.False);Assert.That(Banner==null||!Banner.gameObject.activeInHierarchy,Is.True);
            Assert.That(Field<RectTransform>(ui,"modal"),Is.Null);
        }
        [UnityTest] public IEnumerator StaleRoomCannotJoinEvenWhenItsOldBannerWasVisible()
        {
            Advertise();yield return null;yield return null;Canvas.ForceUpdateCanvases();
            Assert.That(directory.Expire(Time.realtimeSinceStartupAsDouble+LanLobbyDirectory.TtlSeconds+1),Is.True);
            Tap(Button("LAN discovery join"));
            Assert.That(session.Status,Is.EqualTo(ConnectionState.Idle));Assert.That(session.State,Is.Null);
            Assert.That(Banner.gameObject.activeInHierarchy,Is.False);
        }
    }
}
