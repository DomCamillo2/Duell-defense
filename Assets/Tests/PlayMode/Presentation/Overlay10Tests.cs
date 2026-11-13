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
    /// <summary>0.10 ornamentation must preserve the actual controls and authoritative purchases.</summary>
    public sealed class Overlay10Tests
    {
        GameUI ui;
        SessionCoordinator session;
        bool hadReduced;
        int oldReduced;
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        static void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);

        [UnitySetUp] public IEnumerator Start()
        {
            hadReduced=PlayerPrefs.HasKey("duell.reduced");oldReduced=PlayerPrefs.GetInt("duell.reduced",0);
            SceneManager.LoadScene("Boot");yield return new WaitForSecondsRealtime(.2f);
            ui=Object.FindFirstObjectByType<GameUI>();session=Object.FindFirstObjectByType<SessionCoordinator>();
            session.StartOffline(53);session.enabled=false;
            session.State.Phase=MatchPhase.Playing;session.State.WavePhase=WavePhase.Preparing;
            session.State.PreparationRemaining=session.Content.WaveRules.PreparationTicks;
            session.State.Players[0].Gold=10000;session.State.Players[0].AvailableTowerMask=127;
            yield return null;yield return null;Refresh();
        }
        [UnityTearDown] public IEnumerator Stop()
        {
            if(ui!=null){Call(ui,"CancelInteraction");Call(ui,"CloseModal");Call(ui,"CloseAttackInfo");}
            if(session!=null){session.enabled=true;session.Shutdown();}
            if(hadReduced)PlayerPrefs.SetInt("duell.reduced",oldReduced);else PlayerPrefs.DeleteKey("duell.reduced");
            ButtonPressFeedback.SetReducedEffects(oldReduced==1);PlayerPrefs.Save();yield return null;
        }
        void Refresh(){Call(ui,"RefreshLive");Canvas.ForceUpdateCanvases();}
        Button Find(string name){Ui16TestNavigation.Reveal(name);return ui.GetComponentsInChildren<Button>().First(b=>b.name==name);}
        static Vector2 Center(Component component)
        {var r=(RectTransform)component.transform;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));}
        static GameObject Hit(Vector2 point)
        {
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            Assert.That(hits,Is.Not.Empty,"No graphic hit at "+point);return hits[0].gameObject;
        }
        static void Tap(Button button,int id,bool atIcon=false)
        {
            var icon=atIcon?button.GetComponentInChildren<HudIconGraphic>():null;
            if(atIcon)Assert.That(icon,Is.Not.Null,"Control lacks its expected decorative icon: "+button.name);
            if(atIcon)AssertVisibleIconMesh(icon);
            var point=Center(icon!=null?(Component)icon:button);var target=Hit(point);
            Assert.That(target.GetComponentInParent<Button>(),Is.SameAs(button),"Decoration stole the hit for "+button.name);
            var e=new PointerEventData(EventSystem.current){pointerId=id,position=point,pressPosition=point,
                button=PointerEventData.InputButton.Left,eligibleForClick=true,pointerPress=target};
            ExecuteEvents.ExecuteHierarchy(target,e,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(target,e,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(target,e,ExecuteEvents.pointerClickHandler);
        }
        IEnumerator SetWidth(int width)
        {
            var safe=Field<RectTransform>(ui,"safe");var canvas=(RectTransform)safe.parent;
            safe.anchorMin=safe.anchorMax=safe.pivot=new Vector2(.5f,.5f);safe.anchoredPosition=Vector2.zero;
            safe.sizeDelta=new Vector2(width,720);safe.localScale=Vector3.one*Mathf.Min(canvas.rect.width/width,canvas.rect.height/720)*.94f;
            Canvas.ForceUpdateCanvases();Call(ui,"Rebuild");yield return null;Refresh();
        }
        static bool VisibleDecoration(Graphic graphic)
        {
            if(!graphic.isActiveAndEnabled||graphic.color.a<=.001f)return false;
            float alpha=1;
            for(var ancestor=graphic.transform;ancestor!=null;ancestor=ancestor.parent)
            {
                bool ignoreParents=false;
                foreach(var group in ancestor.GetComponents<CanvasGroup>())
                {
                    if(!group.isActiveAndEnabled)continue;
                    alpha*=group.alpha;ignoreParents|=group.ignoreParentGroups;
                }
                if(alpha<=.001f)return false;
                if(ignoreParents)break;
            }
            return true;
        }
        static Color32[] RenderedFrameColors(HudFrameGraphic frame)
        {
            var mesh=Object.Instantiate(frame.canvasRenderer.GetMesh());
            try
            {
                Assert.That(mesh.vertexCount,Is.GreaterThan(0),frame.name+" has no rendered mesh");
                var colors=mesh.colors32;Assert.That(colors.Length,Is.EqualTo(mesh.vertexCount));return colors;
            }
            finally{Object.Destroy(mesh);}
        }
        static void AssertVisibleIconMesh(HudIconGraphic icon)
        {
            // A valid rect and working button raycast do not prove that its symbol was drawn.
            // Unity 6 Graphic/MaskableGraphic does not automatically add CanvasRenderer.
            var renderer=icon.GetComponent<CanvasRenderer>();
            Assert.That(renderer,Is.Not.Null,icon.name+" has no CanvasRenderer");
            Assert.That(renderer.cull,Is.False,icon.name+" is culled despite visible decoration");
            var mesh=renderer.GetMesh();Assert.That(mesh,Is.Not.Null,icon.name+" has no submitted mesh");
            Assert.That(mesh.vertexCount,Is.GreaterThan(0),icon.name+" submitted an empty mesh");
            Assert.That(mesh.colors32.Any(c=>c.a>0),Is.True,icon.name+" submitted only transparent vertices");
            var vertices=mesh.vertices;var triangles=mesh.triangles;float area=0;
            for(int i=0;i+2<triangles.Length;i+=3)
                area+=Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).magnitude*.5f;
            Assert.That(area,Is.GreaterThan(1),icon.name+" has no visible triangle area");
            var r=icon.rectTransform.rect;
            foreach(var vertex in vertices)
            {
                Assert.That(vertex.x,Is.InRange(r.xMin-1,r.xMax+1),icon.name+" mesh escaped its rect horizontally");
                Assert.That(vertex.y,Is.InRange(r.yMin-1,r.yMax+1),icon.name+" mesh escaped its rect vertically");
            }
        }

        [UnityTest] public IEnumerator DecorativeIconsLeaveFortyFourPointControlsAndCardsReachable()
        {
            foreach(int width in new[]{900,1280})
            {
                yield return SetWidth(width);
                var icons=ui.GetComponentsInChildren<HudIconGraphic>();Assert.That(icons,Is.Not.Empty);
                Assert.That(ui.GetComponentsInChildren<HudFrameGraphic>(),Is.Not.Empty);
                foreach(var icon in icons)
                {
                    Assert.That(icon.raycastTarget,Is.False,icon.name+" is decoration, not a touch target");
                    if(VisibleDecoration(icon))AssertVisibleIconMesh(icon);
                }
                var names=new[]{"Battle menu","Zoom in","Zoom out","Reset view","Attack info 0","Attack info 1","Attack info 2","Attack info 3"};
                foreach(string name in names)
                {
                    var b=Find(name);yield return null;var r=(RectTransform)b.transform;
                    Assert.That(r.rect.width,Is.GreaterThanOrEqualTo(44));Assert.That(r.rect.height,Is.GreaterThanOrEqualTo(44));
                    Assert.That(Hit(Center(b)).GetComponentInParent<Button>(),Is.SameAs(b),width+" / "+name);
                    foreach(var icon in b.GetComponentsInChildren<HudIconGraphic>())
                        Assert.That(Hit(Center(icon)).GetComponentInParent<Button>(),Is.SameAs(b),"Icon covers "+name);
                }
                for(int k=0;k<7;k++){var card=Find("Build class "+k);yield return null;Assert.That(Hit(Center(card)).GetComponentInParent<Button>(),Is.SameAs(card));}
                for(int k=0;k<4;k++){var card=Find("Send class "+k);yield return null;Assert.That(Hit(Center(card)).GetComponentInParent<Button>(),Is.SameAs(card));}
                CompactHud08Tests.CaptureHud("overlay-controls-"+width);yield return null;Refresh();
            }
        }

        [UnityTest] public IEnumerator InformationIconNeverPurchasesAndClosingItPreservesTheDisplayedQuote()
        {
            session.Submit(CommandType.ResearchUnit,0,-1,0,0,0,0);Assert.That(session.LastResult.Accepted,Is.True);Refresh();
            var send=Find("Send class 0");var quote=Field<SendQuote[]>(ui,"displayedSendQuotes")[0];var gold=session.State.Players[0].Gold;
            Tap(Find("Attack info 0"),1001,true);yield return new WaitForSecondsRealtime(.3f);Refresh();
            Assert.That(Field<BattleInfoPanel>(ui,"attackInfo").Open,Is.True);
            Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));Assert.That(session.State.SpawnOrders,Is.Empty);
            EventSystem.current.SetSelectedGameObject(send.gameObject);
            ExecuteEvents.Execute(send.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));Assert.That(session.State.SpawnOrders,Is.Empty);
            CompactHud08Tests.CaptureHud("overlay-troop-information");yield return null;
            Tap(Find("Close attack info"),1002,true);yield return new WaitForSecondsRealtime(.3f);Refresh();
            Tap(send,1003);Refresh();
            Assert.That(session.LastResult.Accepted,Is.True);Assert.That(session.State.SpawnOrders.Count,Is.EqualTo(1));
            var order=session.State.SpawnOrders[0];
            Assert.That(order.WaveId,Is.EqualTo(quote.WaveId));Assert.That(order.ResearchLevel,Is.EqualTo(quote.ResearchLevel));
            Assert.That(order.PaidPrice,Is.EqualTo(quote.Price));Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold-quote.Price));
        }

        [UnityTest] public IEnumerator ZoomAndBattleMenuWorkWhenTappedThroughTheirIcons()
        {
            var board=Field<BoardView>(ui,"ownBoard");var gold=session.State.Players[0].Gold;
            float start=board.ZoomFactor;Tap(Find("Zoom in"),1011,true);Assert.That(board.ZoomFactor,Is.GreaterThan(start));
            float zoomed=board.ZoomFactor;Tap(Find("Zoom out"),1012,true);Assert.That(board.ZoomFactor,Is.LessThan(zoomed));
            Tap(Find("Zoom in"),1013,true);Tap(Find("Reset view"),1014,true);Assert.That(board.ZoomFactor,Is.EqualTo(1).Within(.001f));
            Tap(Find("Battle menu"),1015,true);yield return null;
            var modal=Field<RectTransform>(ui,"modal");Assert.That(modal,Is.Not.Null);
            Assert.That(modal.GetComponentsInChildren<Text>().Any(t=>t.text=="DUELL"),Is.True);
            var background=Find("Send class 0");
            Assert.That(Hit(Center(background)).GetComponentInParent<Button>(),Is.Not.SameAs(background));
            Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold));Assert.That(session.State.SpawnOrders,Is.Empty);
            CompactHud08Tests.CaptureHud("overlay-battle-menu");
        }

        [UnityTest] public IEnumerator UnaffordableCardsDesaturateRenderedFramesAndPortraitsWhileInfoStillWorks()
        {
            var cards=new[]{Find("Build class 0"),Find("Send class 0")};
            foreach(var card in cards)
            {
                Assert.That(card.interactable,Is.True);
                var frames=card.GetComponentsInChildren<HudFrameGraphic>().Where(VisibleDecoration).ToArray();
                Assert.That(frames.Any(f=>f.Style==HudFrameStyle.Card),Is.True);
                Assert.That(frames.Any(f=>f.Style==HudFrameStyle.Price),Is.True);
                Assert.That(frames.Any(f=>f.Style==HudFrameStyle.Well),Is.True);
                foreach(var frame in frames)
                    Assert.That(RenderedFrameColors(frame).Any(c=>c.a>0&&(Mathf.Abs(c.r-c.g)>1||Mathf.Abs(c.g-c.b)>1)),Is.True,
                        card.name+" / "+frame.name+" must begin with actual colored decoration");
            }
            session.State.Players[0].Gold=0;Refresh();yield return null;Refresh();yield return null;
            foreach(var card in cards)
            {
                Assert.That(card.interactable,Is.False);
                Assert.That(card.GetComponent<PurchaseAvailability>().State,Is.EqualTo(PurchaseState.Unaffordable));
                var frames=card.GetComponentsInChildren<HudFrameGraphic>().Where(VisibleDecoration).ToArray();
                Assert.That(frames,Is.Not.Empty);
                foreach(var frame in frames)foreach(var color in RenderedFrameColors(frame))
                {
                    Assert.That(Mathf.Abs(color.r-color.g),Is.LessThanOrEqualTo(1),card.name+" / "+frame.name+" retains red/green trim");
                    Assert.That(Mathf.Abs(color.g-color.b),Is.LessThanOrEqualTo(1),card.name+" / "+frame.name+" retains green/blue trim");
                    Assert.That(Mathf.Abs(color.r-color.b),Is.LessThanOrEqualTo(1),card.name+" / "+frame.name+" retains red/blue trim");
                }
                var portrait=card.GetComponentsInChildren<Image>().Single(i=>i.name=="Generated class portrait");
                Assert.That(portrait.sprite,Is.Not.Null);Assert.That(portrait.material.shader.name,Is.EqualTo("DefenseDuell/UIGrayscale"));
            }
            CompactHud08Tests.CaptureHud("overlay-unaffordable-desaturated-cards");yield return null;Refresh();
            var info=Find("Attack info 0");Assert.That(info.interactable,Is.True);Tap(info,1021,true);
            yield return new WaitForSecondsRealtime(.3f);Refresh();
            Assert.That(Field<BattleInfoPanel>(ui,"attackInfo").Open,Is.True);
            Assert.That(session.State.Players[0].Gold,Is.EqualTo(System.Numerics.BigInteger.Zero));Assert.That(session.State.SpawnOrders,Is.Empty);
        }

        [UnityTest] public IEnumerator ReducedEffectsHasStableDecorationWithoutContinuousAnimationDrivers()
        {
            Field<LocalSettings>(ui,"settings").SetReduced(true);Call(ui,"ApplyVisualSettings");
            yield return null;Refresh();yield return null;
            // Closed attack details remain active for native raycast depth but have CanvasGroup
            // alpha zero. Exclude them by visibility, never by whether their mesh is empty.
            var graphics=ui.GetComponentsInChildren<Graphic>().Where(g=>(g is HudIconGraphic||g is HudFrameGraphic)&&VisibleDecoration(g)).ToArray();
            Assert.That(graphics,Is.Not.Empty);
            var hiddenInfo=Field<RectTransform>(ui,"attackInfoContent");
            Assert.That(Field<BattleInfoPanel>(ui,"attackInfo").Open,Is.False);
            Assert.That(graphics.Any(g=>g.transform.IsChildOf(hiddenInfo)),Is.False,"Closed alpha-zero information panel entered the visible mesh sample");
            var positions=graphics.Select(g=>g.transform.localPosition).ToArray();var scales=graphics.Select(g=>g.transform.localScale).ToArray();
            var colors=graphics.Select(g=>g.color).ToArray();var meshes=new Mesh[graphics.Length];
            try
            {
                for(int i=0;i<graphics.Length;i++)
                {
                    var g=graphics[i];Assert.That(g.GetComponentsInChildren<Component>(true).Any(c=>c!=null&&(c.GetType().FullName=="UnityEngine.Animator"||c.GetType().FullName=="UnityEngine.Animation")),Is.False);
                    foreach(string callback in new[]{"Update","LateUpdate","FixedUpdate"})
                        Assert.That(g.GetType().GetMethod(callback,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly),Is.Null,g.GetType().Name+" runs continuous decoration");
                    meshes[i]=Object.Instantiate(g.canvasRenderer.GetMesh());
                    Assert.That(meshes[i].vertexCount,Is.GreaterThan(0),g.name+" has no rendered decoration to compare");
                }
                yield return new WaitForSecondsRealtime(.35f);Refresh();yield return null;
                for(int i=0;i<graphics.Length;i++)
                {
                    var g=graphics[i];Assert.That(g.transform.localPosition,Is.EqualTo(positions[i]));Assert.That(g.transform.localScale,Is.EqualTo(scales[i]));Assert.That(g.color,Is.EqualTo(colors[i]));
                    var after=Object.Instantiate(g.canvasRenderer.GetMesh());try
                    {
                        Assert.That(after.vertices,Is.EqualTo(meshes[i].vertices));Assert.That(after.colors32,Is.EqualTo(meshes[i].colors32));
                    }
                    finally{Object.Destroy(after);}
                }
                CompactHud08Tests.CaptureHud("overlay-reduced-effects");
            }
            finally{foreach(var mesh in meshes)if(mesh!=null)Object.Destroy(mesh);}
        }
    }
}
