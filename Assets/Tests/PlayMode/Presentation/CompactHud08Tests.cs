using BigInteger=System.Numerics.BigInteger;
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
using Object = UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    /// <summary>0.8 clutter regression: visibility follows state while every purchase keeps its own quote.</summary>
    public sealed class CompactHud08Tests
    {
        SessionCoordinator session;
        GameUI ui;
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        static void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
        [UnitySetUp] public IEnumerator Start()
        {
            SceneManager.LoadScene("Boot");yield return new WaitForSecondsRealtime(.2f);
            session=Object.FindFirstObjectByType<SessionCoordinator>();ui=Object.FindFirstObjectByType<GameUI>();
            session.StartOffline(37);session.enabled=false;
            session.State.Phase=MatchPhase.Playing;session.State.WavePhase=WavePhase.Preparing;
            session.State.PreparationRemaining=session.Content.WaveRules.PreparationTicks;
            session.State.Players[0].Gold=10000;session.State.Players[0].AvailableTowerMask=31;
            yield return null;yield return null;Refresh();
        }
        [UnityTearDown] public IEnumerator Stop()
        {
            if(ui!=null){Call(ui,"CloseAttackInfo");Call(ui,"CloseModal");}
            if(session!=null){session.enabled=true;session.Shutdown();}
            yield return null;
        }
        void Refresh(){Call(ui,"RefreshLive");Canvas.ForceUpdateCanvases();}
        IEnumerator SetWidth(int width)
        {
            var safe=Field<RectTransform>(ui,"safe");var canvas=(RectTransform)safe.parent;
            safe.anchorMin=safe.anchorMax=safe.pivot=new Vector2(.5f,.5f);
            safe.anchoredPosition=Vector2.zero;safe.sizeDelta=new Vector2(width,720);
            safe.localScale=Vector3.one*Mathf.Min(canvas.rect.width/width,canvas.rect.height/720)*.94f;
            Canvas.ForceUpdateCanvases();Call(ui,"Rebuild");
            // Newly instantiated uGUI graphics need a rendered frame before they have a native
            // depth. GraphicRaycaster deliberately skips depth == -1 despite correct rectangles.
            yield return null;Refresh();
        }
        Button Find(string name)
        {
            Ui16TestNavigation.Reveal(name);
            foreach(var button in ui.GetComponentsInChildren<Button>(true))
                if(button.name==name&&button.gameObject.activeInHierarchy)return button;
            Assert.Fail("Active button missing: "+name);return null;
        }
        static Vector2 Center(Component component)
        {var rect=(RectTransform)component.transform;return RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));}
        static GameObject Hit(Vector2 position)
        {
            var pointer=new PointerEventData(EventSystem.current){position=position};var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer,hits);return hits.Count>0?hits[0].gameObject:null;
        }
        static Button HitButton(Vector2 position)=>Hit(position)?.GetComponentInParent<Button>();
        static void Submit(Button button)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
            ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
        }
        static void Tap(Button button,int pointerId)
        {
            var point=Center(button);var target=Hit(point);Assert.That(target,Is.Not.Null);
            Assert.That(target.GetComponentInParent<Button>(),Is.SameAs(button));
            var e=new PointerEventData(EventSystem.current){position=point,pressPosition=point,pointerId=pointerId,
                button=PointerEventData.InputButton.Left,eligibleForClick=true};
            ExecuteEvents.ExecuteHierarchy(target,e,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(target,e,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(target,e,ExecuteEvents.pointerClickHandler);
        }
        static string TextIn(Transform root)=>string.Join("\n",root.GetComponentsInChildren<Text>().Select(t=>t.text));
        static void AssertScrollbarHandleInsideTrack(ScrollRect scroll)
        {
            Assert.That(scroll.verticalScrollbar,Is.Not.Null);
            var track=(RectTransform)scroll.verticalScrollbar.transform;
            var handle=scroll.verticalScrollbar.handleRect;var corners=new Vector3[4];handle.GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                Vector3 local=track.InverseTransformPoint(corner);
                Assert.That(local.x,Is.InRange(track.rect.xMin-.1f,track.rect.xMax+.1f),"Scrollbar handle overflows track horizontally");
                Assert.That(local.y,Is.InRange(track.rect.yMin-.1f,track.rect.yMax+.1f),"Scrollbar handle overflows track vertically");
            }
        }
        void BuyPacket(int kind)
        {
            var quote=SendRules.Quote(session.Content,session.State,session.LocalSlot,kind);
            Assert.That(quote.IsValid,Is.True);
            session.Submit(CommandType.SendUnit,kind,-1,0,0,quote.WaveId,quote.ResearchLevel,quote.EnemyTarget);
            Assert.That(session.LastResult.Accepted,Is.True);
        }

        [UnityTest] public IEnumerator ActiveEmptyBoardHasNoWaveBannerOrEmptyQueueAndReadyIsPreparationOnly()
        {
            foreach(int width in new[]{900,960,1280})
            {
                yield return SetWidth(width);
                foreach(var phase in new[]{WavePhase.Preparing,WavePhase.Active,WavePhase.Clearing})
                {
                    session.State.WavePhase=phase;Refresh();yield return null;Refresh();
                    var menuText=Find("Battle menu").GetComponentInChildren<Text>();
                    Assert.That(menuText.preferredWidth,Is.LessThanOrEqualTo(menuText.rectTransform.rect.width+.1f),"Menu label wraps inside its compact button");
                    var preparation=Field<RectTransform>(ui,"wavePreparation");
                    Assert.That(preparation.gameObject.activeInHierarchy,Is.EqualTo(phase==WavePhase.Preparing));
                    Assert.That(Field<Button>(ui,"waveStartButton").gameObject.activeInHierarchy,Is.EqualTo(phase==WavePhase.Preparing));
                    Assert.That(Field<Button>(ui,"paidQueueButton").gameObject.activeInHierarchy,Is.False);
                    Assert.That(ui.GetComponentsInChildren<RectTransform>().Any(r=>r.name=="Wave status"),Is.False,"The former broad banner must not remain visible");
                    var screen=Field<RectTransform>(ui,"screen");var hit=Hit(Center(screen));
                    Assert.That(hit,Is.Not.Null);
                    Assert.That(hit.GetComponentInParent<BoardView>(),Is.SameAs(Field<BoardView>(ui,"ownBoard")),"Empty central playfield is blocked at "+width+" / "+phase);
                    var board=Field<BoardView>(ui,"ownBoard");var route=session.Content.Map.Waypoints;
                    Vector2 ownBase=board.MapPointToScreen(route[route.Length-1]);
                    // The full-size map may extend under HUD cards; every gameplay location remains reachable by pan.
                    board.SetZoom(1.5f);board.PanByScreenDelta(Center(screen)-board.MapPointToScreen(route[route.Length-1]));
                    yield return null;Refresh();
                    var baseHit=Hit(board.MapPointToScreen(route[route.Length-1]));
                    Assert.That(baseHit?.GetComponentInParent<BoardView>(),Is.SameAs(board),
                        "Panning cannot expose the own base at "+width+" / "+phase);
                    board.ResetView();Refresh();
                    Assert.That(Find("Wave details").interactable,Is.True);
                    CaptureHud("compact-empty-"+phase.ToString().ToLowerInvariant()+"-"+width);
                    // The capture temporarily changes the canvas render mode/camera. Let the
                    // normal canvas submit again before the next raycast assertion.
                    yield return null;Refresh();
                }
            }
            yield return null;
        }

        [UnityTest] public IEnumerator WaveDetailsAreAvailableOnDemandAndCannotTriggerBackgroundPurchase()
        {
            session.State.WavePhase=WavePhase.Active;Refresh();
            var send=Find("Send class 0");var gold=session.State.Players[0].Gold;
            Tap(Find("Wave details"),801);yield return null;
            var modal=Field<RectTransform>(ui,"modal");Assert.That(modal,Is.Not.Null);
            string details=TextIn(modal);
            Assert.That(details,Does.Contain("Grundwelle"));
            Assert.That(details,Does.Contain(session.Content.GetWave(2).HpForKind(0).ToString()));
            Submit(send);send.onClick.Invoke();
            Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));Assert.That(session.State.SpawnOrders,Is.Empty);
            CaptureHud("compact-wave-details");
            Call(ui,"CloseModal");yield return null;Refresh();
            Assert.That(Hit(Center(Field<RectTransform>(ui,"screen"))).GetComponentInParent<BoardView>(),Is.SameAs(Field<BoardView>(ui,"ownBoard")));
        }

        [UnityTest] public IEnumerator AttackCardsKeepTheirResearchQuotesWithoutArtificialWaveBudget()
        {
            // All classes keep the active target wave, including packets beyond the former 240 G budget.
            session.Submit(CommandType.ResearchUnit,0,-1,0,0,0,0);Assert.That(session.LastResult.Accepted,Is.True);
            BuyPacket(2);BuyPacket(2);BuyPacket(2);BuyPacket(1);BuyPacket(1);Refresh();
            Assert.That(session.State.Players[0].ReservedGoldByWave[0],Is.EqualTo((BigInteger)243));
            var labels=Field<Text[]>(ui,"sendQuoteText");var quotes=Field<SendQuote[]>(ui,"displayedSendQuotes");
            for(int kind=0;kind<4;kind++)
            {
                var actual=SendRules.Quote(session.Content,session.State,session.LocalSlot,kind);
                Assert.That(quotes[kind].WaveId,Is.EqualTo((System.Numerics.BigInteger)1));
                Assert.That(labels[kind].text,Does.Contain("W"+actual.WaveId));
                Assert.That(labels[kind].text,Does.Contain("S"+actual.ResearchLevel));
                Assert.That(labels[kind].text,Does.Not.Contain("Truppenwert"),"Repeated budget prose should be in details");
            }
            CaptureHud("compact-unlimited-send-quotes");
            Submit(Find("Send class 0"));
            var order=session.State.SpawnOrders[session.State.SpawnOrders.Count-1];
            Assert.That(order.Kind,Is.Zero);Assert.That(order.WaveId,Is.EqualTo((System.Numerics.BigInteger)1));Assert.That(order.ResearchLevel,Is.EqualTo((BigInteger)1));
            yield return null;
        }

        [UnityTest] public IEnumerator QueueAppearsOnlyForPaidOrdersAndDetailsContainTheEntireFifo()
        {
            Assert.That(Field<Button>(ui,"paidQueueButton").gameObject.activeInHierarchy,Is.False);
            session.Submit(CommandType.ResearchUnit,3,-1,0,0,0,0);Assert.That(session.LastResult.Accepted,Is.True);
            int[] kinds={0,3,2,1,0,3};foreach(int kind in kinds)BuyPacket(kind);Refresh();yield return null;Refresh();
            var queue=Find("Own paid attack queue");Assert.That(queue.interactable,Is.True);
            var gold=session.State.Players[0].Gold;int orders=session.State.SpawnOrders.Count;
            Tap(queue,802);yield return null;
            var modal=Field<RectTransform>(ui,"modal");Assert.That(modal,Is.Not.Null);
            string text=TextIn(modal);int cursor=0;
            foreach(int kind in kinds)
            {
                int found=text.IndexOf(session.Content.Units[kind].Name,cursor,StringComparison.Ordinal);
                Assert.That(found,Is.GreaterThanOrEqualTo(cursor),"Missing/reordered FIFO entry "+kind+" after offset "+cursor);
                cursor=found+session.Content.Units[kind].Name.Length;
            }
            Assert.That(text,Does.Contain("S1"));Assert.That(text,Does.Contain("W1"));Assert.That(text,Does.Contain("4"));
            Assert.That(text,Does.Contain("Erster Ausmarsch: ab Wellenstart"));
            Assert.That(text,Does.Contain("Noch gebunden: 201 G"));
            Assert.That(text,Does.Contain("Alles ausgesandt: Zeitpunkt offen"));
            Submit(Find("Send class 0"));Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));
            Assert.That(session.State.SpawnOrders.Count,Is.EqualTo(orders));
            CaptureHud("compact-full-fifo-details");
            Call(ui,"CloseModal");Refresh();CaptureHud("compact-paid-fifo-summary");
            // Visibility regression fixture: after the final real order leaves, the empty strip disappears.
            session.State.SpawnOrders.Clear();Refresh();
            Assert.That(Field<Button>(ui,"paidQueueButton").gameObject.activeInHierarchy,Is.False);
        }

        [UnityTest] public IEnumerator UnlockMasksPackCardsAndInCardInfoOwnsItsTouchAreaWithoutBuying()
        {
            foreach(int width in new[]{900,960,1280})
            {
                yield return SetWidth(width);
                foreach(int mask in new[]{31,143,151,31})
                {
                    session.State.Players[0].AvailableTowerMask=mask;Refresh();yield return null;Refresh();
                    var buttons=Field<Button[]>(ui,"buildButtons");float previousRight=float.NaN;
                    Assert.That(buttons.Length,Is.EqualTo(session.Content.Towers.Length));
                    for(int kind=0;kind<buttons.Length;kind++)
                    {
                        bool visible=(mask&(1<<kind))!=0;
                        if(visible)Ui16TestNavigation.Reveal("Build class "+kind);
                        Assert.That(buttons[kind].gameObject.activeSelf,Is.EqualTo(visible),"Locked card visibility "+kind);
                        if(!visible)continue;
                        var rect=(RectTransform)buttons[kind].transform;
                        Assert.That(rect.rect.width,Is.GreaterThanOrEqualTo(44));Assert.That(rect.rect.height,Is.GreaterThanOrEqualTo(44));
                        Assert.That(buttons[kind].name,Is.EqualTo("Build class "+kind));
                        Assert.That(HitButton(Center(buttons[kind])),Is.SameAs(buttons[kind]));
                        if(!float.IsNaN(previousRight))Assert.That(rect.anchoredPosition.x-previousRight,Is.InRange(0f,16f),"A locked card left a gap");
                        previousRight=rect.anchoredPosition.x+rect.rect.width;
                    }
                    CaptureHud("compact-packed-"+mask+"-"+width);
                    yield return null;Refresh();
                }
                for(int kind=0;kind<4;kind++)
                {
                    var info=Find("Attack info "+kind);var send=Find("Send class "+kind);yield return null;var rect=(RectTransform)info.transform;
                    Assert.That(rect.rect.width,Is.GreaterThanOrEqualTo(44));Assert.That(rect.rect.height,Is.GreaterThanOrEqualTo(44));
                    Assert.That(HitButton(Center(send)),Is.SameAs(send));
                    foreach(var normalized in new[]{new Vector2(.5f,.5f),new Vector2(.1f,.1f),new Vector2(.9f,.9f)})
                    {
                        Vector2 local=rect.rect.min+Vector2.Scale(rect.rect.size,normalized);
                        var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(local));
                        Assert.That(HitButton(point),Is.SameAs(info),"Info area routes to purchase "+width+" / "+kind);
                    }
                    var gold=session.State.Players[0].Gold;int orders=session.State.SpawnOrders.Count;
                    Tap(info,900+kind);Refresh();
                    Assert.That(Field<BattleInfoPanel>(ui,"attackInfo").Blocking,Is.True);
                    Assert.That(Field<int>(ui,"infoKind"),Is.EqualTo(kind));
                    Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));Assert.That(session.State.SpawnOrders.Count,Is.EqualTo(orders));
                    Call(ui,"CloseAttackInfo");Refresh();
                    Assert.That(HitButton(Center(send)),Is.SameAs(send),"Closed information shield blocks purchase");
                }
            }
            yield return null;
        }

        [UnityTest] public IEnumerator FullPaidQueueCanScrollToTheFinalOrder()
        {
            yield return SetWidth(1280);
            const int paidOrderCount=100;
            for(int i=0;i<paidOrderCount;i++)
            {
                if(i>0&&i%20==0)session.State.Tick+=session.Content.TickRate;
                BuyPacket(0);
            }
            Assert.That(session.State.SpawnOrders.Count,Is.EqualTo(paidOrderCount));
            Assert.That(session.State.SpawnOrders.All(o=>o.WaveId==1),Is.True);
            Refresh();Submit(Find("Own paid attack queue"));yield return null;Refresh();
            var text=Field<Text>(ui,"paidQueueDetailText");var scroll=Field<ScrollRect>(ui,"paidQueueScroll");
            Assert.That(text.text,Does.Contain(paidOrderCount+". W"));
            Assert.That(scroll.content.rect.height,Is.GreaterThan(scroll.viewport.rect.height));
            AssertScrollbarHandleInsideTrack(scroll);
            scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return null;
            Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(0).Within(.01f));
            var contentCorners=new Vector3[4];var viewportCorners=new Vector3[4];
            scroll.content.GetWorldCorners(contentCorners);scroll.viewport.GetWorldCorners(viewportCorners);
            Assert.That(contentCorners[0].y,Is.EqualTo(viewportCorners[0].y).Within(2),"The final rows cannot reach the visible viewport");
            AssertScrollbarHandleInsideTrack(scroll);
            CaptureHud("compact-full-fifo-bottom");
        }

        [UnityTest] public IEnumerator BattleMenuReplacesDialogsAndKeepsBackgroundPurchasesBlocked()
        {
            var gold=session.State.Players[0].Gold;var send=Find("Send class 0");
            foreach(string destination in new[]{"Ton & Anzeige","Duell verlassen"})
            {
                Tap(Find("Battle menu"),810);yield return null;
                var previous=Field<RectTransform>(ui,"modal");Assert.That(previous,Is.Not.Null);
                Tap(Find(destination),811);
                var current=Field<RectTransform>(ui,"modal");
                Assert.That(current,Is.Not.Null);Assert.That(current,Is.Not.SameAs(previous));
                Assert.That(previous.gameObject.activeInHierarchy,Is.False);
                Submit(send);send.onClick.Invoke();
                Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));Assert.That(session.State.SpawnOrders,Is.Empty);
                yield return null;Refresh();
                if(destination=="Duell verlassen")Tap(Find("Weiterspielen"),812);else Call(ui,"CloseModal");
                Assert.That(Field<RectTransform>(ui,"modal"),Is.Null);
                Assert.That(session.State.Phase,Is.EqualTo(MatchPhase.Playing));Refresh();
                Assert.That(HitButton(Center(send)),Is.SameAs(send));
            }
        }

        [UnityTest] public IEnumerator InCardInformationRemainsReachableWhenTheSendPurchaseIsUnaffordable()
        {
            yield return SetWidth(900);session.State.Players[0].Gold=0;Refresh();
            for(int kind=0;kind<4;kind++)
            {
                var send=Find("Send class "+kind);var info=Find("Attack info "+kind);yield return null;
                Assert.That(send.interactable,Is.False);Assert.That(info.interactable,Is.True);
                Assert.That(HitButton(Center(info)),Is.SameAs(info));Tap(info,820+kind);Refresh();
                Assert.That(Field<BattleInfoPanel>(ui,"attackInfo").Blocking,Is.True);
                Assert.That(Field<int>(ui,"infoKind"),Is.EqualTo(kind));
                Assert.That(session.State.Players[0].Gold,Is.EqualTo(BigInteger.Zero));Assert.That(session.State.SpawnOrders,Is.Empty);
                Call(ui,"CloseAttackInfo");Refresh();
            }
            yield return null;
        }

        [Serializable] sealed class CaptureDimensions
        {
            public int logicalWidth,logicalHeight,pixelWidth,pixelHeight;
            public float actualCanvasWidth,actualCanvasHeight;
            public bool ownBoardPresent,enemyBoardPresent;
            public int ownRenderPixelWidth,ownRenderPixelHeight,enemyRenderPixelWidth,enemyRenderPixelHeight;
        }

        // This helper is shared by legacy coverage during the current 0.10 integration.
        // Preserve historical Hud-v07/v08/v09 evidence rather than overwriting it on a new source state.
        internal static void CaptureHud(string name,Action captureReady=null)
        {
            var gameUI=Object.FindFirstObjectByType<GameUI>();
            var canvas=gameUI.GetComponentInChildren<Canvas>().rootCanvas;
            var safe=Field<RectTransform>(gameUI,"safe");
            int logicalWidth=Mathf.RoundToInt(safe.rect.width),logicalHeight=Mathf.RoundToInt(safe.rect.height);
            int pixelWidth=Mathf.RoundToInt(logicalWidth*1.5f),pixelHeight=Mathf.RoundToInt(logicalHeight*1.5f);
            var oldSafeScale=safe.localScale;float oldScaleFactor=canvas.scaleFactor;
            var scaler=canvas.GetComponent<CanvasScaler>();bool scalerEnabled=scaler!=null&&scaler.enabled;
            var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float distance=canvas.planeDistance;
            var go=new GameObject("Compact HUD proof camera");var camera=go.AddComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.15f,.13f);
            camera.orthographic=true;camera.transform.position=new Vector3(0,0,-100);
            var rt=new RenderTexture(pixelWidth,pixelHeight,24);rt.Create();camera.targetTexture=rt;
            var previous=RenderTexture.active;Texture2D image=null;
            try
            {
                if(scaler!=null)scaler.enabled=false;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
                // ScreenSpaceCamera uses the target texture's pixel rect. Render 1.5 physical pixels
                // per logical canvas unit and remove the test viewport's temporary fit-to-screen scale.
                canvas.scaleFactor=1.5f;safe.localScale=Vector3.one;Canvas.ForceUpdateCanvases();
                var canvasRect=(RectTransform)canvas.transform;
                Assert.That(canvasRect.rect.width,Is.EqualTo(logicalWidth).Within(1));
                Assert.That(canvasRect.rect.height,Is.EqualTo(logicalHeight).Within(1));
                Call(gameUI,"RefreshBoardOverviewInsets");
                foreach(var board in gameUI.GetComponentsInChildren<BoardView>())board.RefreshRenderNow();
                captureReady?.Invoke();
                camera.Render();RenderTexture.active=rt;
                image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
                var folder=System.IO.Path.GetFullPath("TestResults/Hud-v15");System.IO.Directory.CreateDirectory(folder);
                string stem=name+"-logical"+logicalWidth+"x"+logicalHeight+"-pixels"+pixelWidth+"x"+pixelHeight;
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,stem+".png"),image.EncodeToPNG());
                var own=Field<BoardView>(gameUI,"ownBoard");var enemy=Field<BoardView>(gameUI,"enemyBoard");
                System.IO.File.WriteAllText(System.IO.Path.Combine(folder,stem+".json"),JsonUtility.ToJson(new CaptureDimensions {
                    logicalWidth=logicalWidth,logicalHeight=logicalHeight,pixelWidth=pixelWidth,pixelHeight=pixelHeight,
                    actualCanvasWidth=canvasRect.rect.width,actualCanvasHeight=canvasRect.rect.height,
                    ownBoardPresent=own!=null,enemyBoardPresent=enemy!=null,
                    ownRenderPixelWidth=own!=null?own.RenderPixelSize.x:0,ownRenderPixelHeight=own!=null?own.RenderPixelSize.y:0,
                    enemyRenderPixelWidth=enemy!=null?enemy.RenderPixelSize.x:0,enemyRenderPixelHeight=enemy!=null?enemy.RenderPixelSize.y:0},true));
            }
            finally
            {
                RenderTexture.active=previous;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=distance;
                canvas.scaleFactor=oldScaleFactor;safe.localScale=oldSafeScale;
                if(scaler!=null)scaler.enabled=scalerEnabled;
                camera.targetTexture=null;if(image!=null)Object.Destroy(image);rt.Release();Object.Destroy(rt);Object.Destroy(go);Canvas.ForceUpdateCanvases();
                Call(gameUI,"RefreshBoardOverviewInsets");
                foreach(var board in gameUI.GetComponentsInChildren<BoardView>())board.RefreshRenderNow();
            }
        }
    }
}
