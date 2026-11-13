using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
    public sealed class FreePlacementProjectionTests
    {
        static T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);
        [UnityTest]
        public IEnumerator FullscreenOverlayNavigationAndFreeBuildingsAcrossMapsAndSafeFrames()
        {
            SceneManager.LoadScene("Boot");yield return new WaitForSecondsRealtime(.5f);
            var session=Object.FindFirstObjectByType<SessionCoordinator>();var ui=Object.FindFirstObjectByType<GameUI>();
            var safe=Field<RectTransform>(ui,"safe");var canvas=(RectTransform)safe.parent;
            try
            {
                for(int map=0;map<3;map++)
                {
                    Assert.That(session.SelectMap(map),Is.True);session.StartOffline(1);yield return new WaitForSecondsRealtime(.15f);
                    session.Submit(CommandType.SetReady);yield return new WaitForSecondsRealtime(3.3f);
                    Assert.That(session.Content.Map.FreePlacement,Is.True);Assert.That(session.Content.Map.MineSlots,Is.Empty);
                    foreach(var size in new[]{new Vector2(1280,720),new Vector2(1600,720),new Vector2(1440,680)})
                    {
                        safe.anchorMin=safe.anchorMax=safe.pivot=new Vector2(.5f,.5f);safe.anchoredPosition=Vector2.zero;safe.sizeDelta=size;
                        safe.localScale=Vector3.one*Mathf.Min(canvas.rect.width/size.x,canvas.rect.height/size.y)*.94f;
                        Canvas.ForceUpdateCanvases();typeof(GameUI).GetMethod("Rebuild",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,null);
                        yield return new WaitForSecondsRealtime(.25f);
                        Assert.That(safe.rect.width,Is.EqualTo(size.x).Within(.1f));Assert.That(safe.rect.height,Is.EqualTo(size.y).Within(.1f));
                        Assert.That(Field<float>(ui,"width"),Is.EqualTo(size.x).Within(.1f));
                        var board=Field<BoardView>(ui,"ownBoard");var enemy=Field<BoardView>(ui,"enemyBoard");
                        var rect=(RectTransform)board.transform;
                        Assert.That(rect.rect.width,Is.EqualTo(canvas.rect.width).Within(.1f),"Landscape covers the complete canvas width");
                        Assert.That(rect.rect.height,Is.EqualTo(canvas.rect.height).Within(.1f),"Landscape covers the complete canvas height");
                        board.ResetView();AssertFit(board);AssertFit(enemy);
                        int visible=0,covered=0;
                        for(int y=47;y<session.Content.Map.Height-30;y+=89)
                        for(int x=43;x<session.Content.Map.Width-30;x+=97)
                        {
                            var point=new MapPoint(x,y);
                            if(PlacementValidator.ValidatePosition(session.Content,session.State,session.LocalSlot,0,-1,point)!=ErrorCode.None)continue;
                            var screen=board.MapPointToScreen(point);AssertRoundTrip(board,point);
                            if(Top(screen).GetComponentInParent<BoardView>()==board)visible++;else covered++;
                        }
                        Assert.That(visible,Is.GreaterThan(10),"Overview must retain usable free ground");
                        Assert.That(board.TryScreenToMapPoint(new Vector2(-100,-100),null,out _),Is.False);
                        Debug.Log($"FREE_OVERLAY_PASS map={map} safe={size} visible={visible} hudCovered={covered}");
                        int groundEvents=0;System.Action<MapPoint> count=p=>groundEvents++;board.GroundSelected+=count;
                        var build=Find("Build class 0");var hud=Center((RectTransform)build.transform);
                        Assert.That(Top(hud).GetComponentInParent<BoardView>(),Is.Not.EqualTo(board));
                        Click(hud);Assert.That(groundEvents,Is.Zero,"HUD click reaches underlying ground");
                        AssertNavigation(board);AssertGestures(board,session);
                        Assert.That(enemy.GetComponent<BoardPointerSurface>(),Is.Null,"Compact PiP has no navigation handler");
                        var pip=Center((RectTransform)Find("Switch battlefield").transform);
                        Assert.That(Top(pip).GetComponentInParent<BoardView>(),Is.Not.EqualTo(board));
                        Click(pip);yield return new WaitForSecondsRealtime(.1f);
                        Assert.That(Field<bool>(ui,"showEnemy"),Is.True);int before=OwnedCount(session);
                        var free=FindFreePoint(session,board,0);Click(board.MapPointToScreen(free));
                        Assert.That(Field<bool>(ui,"hasSelectedPoint"),Is.False,"Enemy ground is read-only");
                        Assert.That(OwnedCount(session),Is.EqualTo(before));
                        board.SetZoom(2);Assert.That(board.ZoomFactor,Is.EqualTo(2));
                        Click(Center((RectTransform)Find("Return own field").transform));yield return new WaitForSecondsRealtime(.1f);
                        Assert.That(Field<bool>(ui,"showEnemy"),Is.False);Assert.That(board.ZoomFactor,Is.EqualTo(1));
                        Assert.That(groundEvents,Is.Zero,"Navigation and overlay controls must not select ground");
                        board.GroundSelected-=count;board.ResetView();AssertFit(board);
                        if(size.x==1600)CaptureBoard(board,map);
                    }
                    // Controlled funds isolate placement from passive-income timing. No production state is modified.
                    session.State.Players[(int)session.LocalSlot].Gold=10000;yield return null;
                    var own=Field<BoardView>(ui,"ownBoard");
                    var combat=map==2?new MapPoint(153,80):FindFreePoint(session,own,0);
                    Assert.That(PlacementValidator.ValidatePosition(session.Content,session.State,session.LocalSlot,0,-1,combat),Is.EqualTo(ErrorCode.None),"Keep original Frost regression coordinate");
                    own.SetZoom(2,own.MapPointToScreen(combat));own.PanByScreenDelta(Center((RectTransform)own.transform)-own.MapPointToScreen(combat));
                    yield return Purchase(session,ui,own,0,combat,false);
                    for(int n=0;n<2;n++)
                    {
                        own.ResetView();own.SetZoom(1.5f);var mine=FindFreePoint(session,own,4);
                        yield return Purchase(session,ui,own,4,mine,n==1);
                    }
                    int countBefore=OwnedCount(session);
                    Button mineCard=null;foreach(var candidate in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))if(candidate.name=="Build class 4")mineCard=candidate;
                    Assert.That(mineCard,Is.Not.Null);Assert.That(mineCard.interactable,Is.True,"Another mine remains purchasable when funded and ground is free");
                    Assert.That(mineCard.GetComponent<PurchaseAvailability>().State,Is.EqualTo(PurchaseState.Available));
                    own.ResetView();own.SetZoom(1.5f);var thirdMine=FindFreePoint(session,own,4);
                    yield return Purchase(session,ui,own,4,thirdMine,true);
                    Assert.That(OwnedCount(session),Is.EqualTo(countBefore+1),"Third mine must build exactly once");
                    foreach(var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))Assert.That(b.name.StartsWith("Mine site "),Is.False);
                    session.Shutdown();yield return new WaitForSecondsRealtime(.2f);
                }
            }
            finally
            {
                session.Shutdown();safe.localScale=Vector3.one;var a=Screen.safeArea;
                safe.anchorMin=new Vector2(a.xMin/Screen.width,a.yMin/Screen.height);safe.anchorMax=new Vector2(a.xMax/Screen.width,a.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
            }
        }
        static IEnumerator Purchase(SessionCoordinator session,GameUI ui,BoardView board,int kind,MapPoint point,bool drag)
        {
            int count=OwnedCount(session);var screen=board.MapPointToScreen(point);
            Assert.That(Top(screen).GetComponentInParent<BoardView>(),Is.EqualTo(board),"Purchase point must be visibly reachable");
            var button=Find("Build class "+kind);
            if(drag)
            {
                var source=button.GetComponent<BuildDragSource>();var start=Center((RectTransform)button.transform);
                var e=new PointerEventData(EventSystem.current){position=start,pressPosition=start,pointerId=701,button=PointerEventData.InputButton.Left,eligibleForClick=true};
                source.OnPointerDown(e);e.dragging=true;e.position=screen;source.OnBeginDrag(e);source.OnDrag(e);source.OnEndDrag(e);source.OnPointerUp(e);
            }
            else
            {
                Click(Center((RectTransform)button.transform));yield return null;Click(screen);yield return new WaitForSecondsRealtime(.1f);
                var selected=Field<MapPoint>(ui,"selectedPoint");Assert.That(selected.X,Is.EqualTo(point.X).Within(1));Assert.That(selected.Y,Is.EqualTo(point.Y).Within(1));
                Click(Center((RectTransform)Find("Confirm build").transform));
            }
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(OwnedCount(session),Is.EqualTo(count+1));TowerState built=null;
            foreach(var t in session.State.Towers)if(t.Owner==session.LocalSlot&&t.Kind==kind&&Mathf.Abs(t.Position.X-point.X)<=1&&Mathf.Abs(t.Position.Y-point.Y)<=1)built=t;
            Assert.That(built,Is.Not.Null);Assert.That(built.SiteId,Is.EqualTo(-1));
            var cancel=Field<Button>(ui,"cancelSelection");if(cancel.gameObject.activeInHierarchy)Click(Center((RectTransform)cancel.transform));yield return null;
            Click(board.MapPointToScreen(built.Position));yield return new WaitForSecondsRealtime(.1f);
            Assert.That(Field<int>(ui,"selectedTower"),Is.EqualTo(built.Id));
            Click(Center((RectTransform)Find("Cancel selection").transform));yield return null;
            Debug.Log($"FREE_BUILD_PASS kind={kind} point={point.X},{point.Y} drag={drag} entity={built.Id}");
        }
        static int OwnedCount(SessionCoordinator session){int count=0;foreach(var t in session.State.Towers)if(t.Owner==session.LocalSlot)count++;return count;}
        static MapPoint FindFreePoint(SessionCoordinator session,BoardView board,int kind)
        {
            for(int y=80;y<session.Content.Map.Height-80;y+=67)for(int x=80;x<session.Content.Map.Width-80;x+=73)
            {
                var p=new MapPoint(x,y);var screen=board.MapPointToScreen(p);
                if(PlacementValidator.ValidatePosition(session.Content,session.State,session.LocalSlot,kind,-1,p)==ErrorCode.None&&
                    // A legal location can still be inside an existing tower's generous touch target.
                    board.TryScreenToMapPoint(screen,null,out _)&&Top(screen).GetComponentInParent<BoardView>()==board&&
                    Top(screen).GetComponentInParent<Button>()==null)return p;
            }
            Assert.Fail("No visible legal point for kind "+kind);return default;
        }
        static Vector2 Center(RectTransform rect)=>RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        static void AssertRoundTrip(BoardView board,MapPoint point)
        {
            Assert.That(board.TryScreenToMapPoint(board.MapPointToScreen(point),null,out var result),Is.True);
            Assert.That(result.X,Is.EqualTo(point.X).Within(1));Assert.That(result.Y,Is.EqualTo(point.Y).Within(1));
        }
        static void AssertNavigation(BoardView board)
        {
            board.ResetView();var camera=Field<Camera>(board,"viewCamera");Vector3 original=camera.transform.localPosition;float size=camera.orthographicSize;
            int changed=0;System.Action count=()=>changed++;board.ViewChanged+=count;
            try
            {
                var map=Field<MapDefinition>(board,"map");var point=new MapPoint(map.Width/2,map.Height/2);var anchor=board.MapPointToScreen(point);
                board.SetZoom(2,anchor);Assert.That(changed,Is.GreaterThan(0));Assert.That(Vector2.Distance(anchor,board.MapPointToScreen(point)),Is.LessThan(1));AssertRoundTrip(board,point);
                int prior=changed;board.SetZoom(2);board.SetZoom(float.NaN);board.SetZoom(float.PositiveInfinity);board.PanByScreenDelta(new Vector2(float.NaN,1));Assert.That(changed,Is.EqualTo(prior));
                var before=board.MapPointToScreen(point);var delta=new Vector2(18,-12);board.PanByScreenDelta(delta);
                Assert.That(Vector2.Distance(board.MapPointToScreen(point)-before,delta),Is.LessThan(1));AssertRoundTrip(board,point);
                board.SetZoom(100);Assert.That(board.ZoomFactor,Is.EqualTo(3));board.PanByScreenDelta(Vector2.one*100000);
                var clamped=camera.transform.localPosition;board.PanByScreenDelta(Vector2.one*100000);Assert.That(Vector3.Distance(clamped,camera.transform.localPosition),Is.LessThan(.001f));
                board.SetZoom(-1);Assert.That(board.ZoomFactor,Is.EqualTo(1));board.ResetView();Assert.That(Vector3.Distance(original,camera.transform.localPosition),Is.LessThan(.001f));Assert.That(camera.orthographicSize,Is.EqualTo(size).Within(.001f));
                prior=changed;board.ResetView();Assert.That(changed,Is.EqualTo(prior));
            }
            finally{board.ViewChanged-=count;board.ResetView();}
        }
        static void AssertGestures(BoardView board,SessionCoordinator session)
        {
            var source=board.GetComponent<BoardPointerSurface>();Assert.That(source,Is.Not.Null);
            var p=board.MapPointToScreen(FindFreePoint(session,board,0));int before=OwnedCount(session);
            var a=new PointerEventData(EventSystem.current){pointerId=501,position=p,button=PointerEventData.InputButton.Left,eligibleForClick=true};
            source.OnPointerDown(a);a.dragging=true;a.position+=new Vector2(35,20);source.OnBeginDrag(a);source.OnPointerUp(a);source.OnEndDrag(a);source.OnPointerClick(a);
            board.ResetView();p=board.MapPointToScreen(FindFreePoint(session,board,0));
            a=new PointerEventData(EventSystem.current){pointerId=502,position=p-new Vector2(20,0),button=PointerEventData.InputButton.Left,eligibleForClick=true};
            var b=new PointerEventData(EventSystem.current){pointerId=503,position=p+new Vector2(20,0),button=PointerEventData.InputButton.Left,eligibleForClick=true};
            source.OnPointerDown(a);source.OnPointerDown(b);b.dragging=true;b.position+=new Vector2(30,0);source.OnDrag(b);Assert.That(board.ZoomFactor,Is.GreaterThan(1));
            source.OnPointerUp(a);source.OnPointerClick(a);source.OnPointerUp(b);source.OnEndDrag(b);source.OnPointerClick(b);
            board.ResetView();source.OnScroll(new PointerEventData(EventSystem.current){position=p,scrollDelta=new Vector2(0,1)});Assert.That(board.ZoomFactor,Is.GreaterThan(1));
            source.CancelGesture();board.ResetView();Assert.That(OwnedCount(session),Is.EqualTo(before));
        }
        static void AssertFit(BoardView board)
        {
            var camera=Field<Camera>(board,"viewCamera");var world=Field<GameObject>(board,"world");
            var resources=Field<object>(board,"resources");var mesh=(Mesh)resources.GetType().GetField("Terrain").GetValue(resources);
            Assert.That(board.ZoomFactor,Is.EqualTo(1),"Full framing only applies at overview");
            if(board.IsNavigable)
            {
                // 0.10 frames playable ground; outer scenery and tall edge towers can lie behind HUD.
                var logical=Field<MapDefinition>(board,"map");
                foreach(int x in new[]{0,logical.Width})foreach(int y in new[]{0,logical.Height})
                {
                    var screen=board.MapPointToScreen(new MapPoint(x,y));
                    var rect=(RectTransform)board.transform;
                    Assert.That(RectTransformUtility.RectangleContainsScreenPoint(rect,screen,null),Is.True,"Logical ground clipped");
                    AssertRoundTrip(board,new MapPoint(x,y));
                }
                return;
            }
            float minX=1,minY=1,maxX=0,maxY=0;
            foreach(var vertex in mesh.vertices)
            {
                var p=camera.WorldToViewportPoint(world.transform.TransformPoint(vertex));
                minX=Mathf.Min(minX,p.x);minY=Mathf.Min(minY,p.y);maxX=Mathf.Max(maxX,p.x);maxY=Mathf.Max(maxY,p.y);
            }
            Assert.That(minX,Is.GreaterThanOrEqualTo(0),"Terrain left clipped");Assert.That(minY,Is.GreaterThanOrEqualTo(0),"Terrain bottom clipped");
            Assert.That(maxX,Is.LessThanOrEqualTo(1),"Terrain right clipped");Assert.That(maxY,Is.LessThanOrEqualTo(1),"Terrain top clipped");
            var map=Field<MapDefinition>(board,"map");
            float radius=Mathf.Max(1.25f,(float)resources.GetType().GetField("MaximumTowerRadius").GetValue(resources));
            float top=Mathf.Max(3.05f,.18f+(float)resources.GetType().GetField("MaximumTowerHeight").GetValue(resources));
            foreach(float x in new[]{-radius*50,map.Width+radius*50})foreach(float y in new[]{-radius*50,map.Height+radius*50})foreach(float height in new[]{.18f,top})
            {
                var p=camera.WorldToViewportPoint(world.transform.TransformPoint(new Vector3((x-500)/50f,height,(300-y)/50f)));
                Assert.That(p.x,Is.InRange(0f,1f),"Tower region horizontal clip");Assert.That(p.y,Is.InRange(0f,1f),"Tower region vertical clip");
            }
        }
        static void CaptureBoard(BoardView board,int map)
        {
            var image=board.GetComponentInChildren<RawImage>();var source=image.texture as RenderTexture;
            Assert.That(source,Is.Not.Null);var prior=RenderTexture.active;
            var copy=new Texture2D(source.width,source.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active=source;copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0);copy.Apply();
                string folder=System.IO.Path.GetFullPath("TestResults/MapHudAudit-v15");System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,$"map-{map}-board.png"),copy.EncodeToPNG());
                var model=Field<MapDefinition>(board,"map");
                var corners=new[]{new MapPoint(0,0),new MapPoint(model.Width,0),new MapPoint(model.Width,model.Height),new MapPoint(0,model.Height)};
                var lines=new List<string>{"x,y,pixel_x,pixel_y"};
                foreach(var corner in corners)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform,board.MapPointToScreen(corner),null,out var local);
                    var r=image.rectTransform.rect;
                    float px=(local.x-r.xMin)/r.width*source.width,py=(local.y-r.yMin)/r.height*source.height;
                    lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2:F4},{3:F4}",corner.X,corner.Y,px,py));
                }
                System.IO.File.WriteAllLines(System.IO.Path.Combine(folder,$"map-{map}-projection.csv"),lines);

            }
            finally{RenderTexture.active=prior;Object.Destroy(copy);}
        }
        static GameObject Top(Vector2 p)
        {
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);
            Assert.That(hits.Count,Is.GreaterThan(0));return hits[0].gameObject;
        }
        static void Click(Vector2 p)
        {
            var e=new PointerEventData(EventSystem.current){position=p,button=PointerEventData.InputButton.Left,eligibleForClick=true};
            var hit=Top(p);e.pointerPress=hit;e.pressPosition=p;
            ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerClickHandler);
        }
        static Button Find(string name)
        {
            foreach(var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))if(b.name==name&&b.isActiveAndEnabled&&b.interactable)return b;
            Assert.Fail("Missing button "+name);return null;
        }
        static Button FindPrefix(string text)
        {
            foreach(var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))if(b.isActiveAndEnabled&&b.interactable)
                foreach(var t in b.GetComponentsInChildren<Text>())if(t.text.StartsWith(text))return b;
            Assert.Fail("Missing button "+text);return null;
        }
    }
}
