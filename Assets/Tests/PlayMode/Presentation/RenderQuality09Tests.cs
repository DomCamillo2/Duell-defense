using System.Collections;
using System.Reflection;
using DefenseDuell.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class RenderQuality09Tests
    {
        GameObject canvasObject,cameraObject;
        Canvas canvas;
        Camera camera;
        RenderTexture output;
        BoardView board;
        GameContent content;
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
        void Target(int width,int height,float scale)
        {
            camera.targetTexture=null;if(output!=null){output.Release();Object.Destroy(output);}
            output=new RenderTexture(width,height,24);output.Create();camera.targetTexture=output;
            canvas.scaleFactor=scale;Canvas.ForceUpdateCanvases();
        }
        [UnitySetUp] public IEnumerator Start()
        {
            content=GameContent.CreateDefault();
            cameraObject=new GameObject("Physical pixel fixture",typeof(Camera));camera=cameraObject.GetComponent<Camera>();
            camera.enabled=false;camera.orthographic=true;camera.transform.position=new Vector3(0,0,-100);
            canvasObject=new GameObject("Physical canvas",typeof(RectTransform),typeof(Canvas));canvas=canvasObject.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
            Target(1920,1080,1.5f);
            var go=new GameObject("Physical board",typeof(RectTransform));go.transform.SetParent(canvas.transform,false);
            var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            Canvas.ForceUpdateCanvases();board=go.AddComponent<BoardView>();board.Initialize(content,null,null,true,null);
            board.RefreshRenderNow();yield return null;
        }
        [UnityTearDown] public IEnumerator Stop()
        {
            if(camera!=null)camera.targetTexture=null;
            if(canvasObject!=null)Object.Destroy(canvasObject);if(cameraObject!=null)Object.Destroy(cameraObject);
            if(output!=null){output.Release();Object.Destroy(output);}yield return null;
        }
        [UnityTest] public IEnumerator NativePixelsFollowPhysicalScaleWithoutLayoutChangeAndRespectBudget()
        {
            Assert.That(board.RenderPixelSize,Is.EqualTo(new Vector2Int(1920,1080)));
            var logical=((RectTransform)board.transform).rect.size;
            var initial=Field<RenderTexture>(board,"texture");board.RefreshRenderNow();
            Assert.That(Field<RenderTexture>(board,"texture"),Is.SameAs(initial),"Stable frames must reuse the native texture");
            Target(2560,1440,2);board.RefreshRenderNow();
            Assert.That(((RectTransform)board.transform).rect.size.x,Is.EqualTo(logical.x).Within(1));
            Assert.That(board.RenderPixelSize,Is.EqualTo(new Vector2Int(2560,1440)),"DPI/target change must resize even at identical logical dimensions");
            var resized=Field<RenderTexture>(board,"texture");Assert.That(resized,Is.Not.SameAs(initial));
            Target(3840,2160,3);board.RefreshRenderNow();
            Assert.That(board.RenderPixelSize,Is.EqualTo(new Vector2Int(2560,1440)),"4K output uses the documented render budget");
            Assert.That(Field<RenderTexture>(board,"texture"),Is.SameAs(resized));
            yield return null;Assert.That(initial==null,Is.True,"Old render target is released after resizing");
        }
        [UnityTest] public IEnumerator OverviewKeepsBothPathEndsInClearAreaWhileRenderCoversWholeFrame()
        {
            var safe=new Rect(.025f,.19f,.83f,.71f);board.SetOverviewSafeRect(safe);board.ResetView();board.RefreshRenderNow();
            var view=Field<Camera>(board,"viewCamera");var world=Field<GameObject>(board,"world");
            foreach(var point in new[]{content.Map.Waypoints[0],content.Map.Waypoints[content.Map.Waypoints.Length-1]})
            {
                var v=view.WorldToViewportPoint(world.transform.TransformPoint(DioramaResources.Point(point,.18f)));
                Assert.That(safe.Contains(new Vector2(v.x,v.y)),Is.True,"Entry/base is behind the reserved HUD area: "+v);
            }
            Assert.That(board.RenderPixelSize,Is.EqualTo(new Vector2Int(1920,1080)),"Insets affect camera fit, never crop or shrink the screen render");
            var backdrop=world.transform.Find("Surrounding landscape");Assert.That(backdrop,Is.Not.Null);
            Assert.That(backdrop.GetComponent<MeshFilter>().sharedMesh.vertexCount,Is.GreaterThan(500));
            Assert.That(backdrop.GetComponent<MeshFilter>().sharedMesh.bounds.size.x,Is.GreaterThan(100));
            yield return null;
        }
        [UnityTest] public IEnumerator BuildHintsRespectOccupiedGroundAndDisposeWithBoard()
        {
            var state=new MatchSimulation(content).State;state.Phase=MatchPhase.Playing;
            board.ShowPlacementGuide(state,PlayerSlot.Zero,0);var world=Field<GameObject>(board,"world");
            var guide=world.transform.Find("Legal building ground");Assert.That(guide.gameObject.activeSelf,Is.True);
            int before=board.PlacementGuidePointCount;Assert.That(before,Is.GreaterThan(20));
            var mesh=guide.GetComponent<MeshFilter>().sharedMesh;
            MapPoint chosen=default;bool found=false;
            for(int y=35;y<content.Map.Height-25&&!found;y+=40)for(int x=35;x<content.Map.Width-25;x+=40)
            {
                var p=new MapPoint(x,y);if(PlacementValidator.ValidatePosition(content,state,PlayerSlot.Zero,0,-1,p)!=ErrorCode.None)continue;
                chosen=p;found=true;break;
            }
            Assert.That(found,Is.True);
            state.Towers.Add(new TowerState{Id=17,Owner=PlayerSlot.Zero,Kind=0,Position=chosen,SiteId=-1,Slot=0});
            board.ShowPlacementGuide(state,PlayerSlot.Zero,0);
            Assert.That(board.PlacementGuidePointCount,Is.LessThan(before),"Occupied building clearance must remove formerly legal hints");
            var next=guide.GetComponent<MeshFilter>().sharedMesh;Assert.That(next,Is.Not.SameAs(mesh));
            board.ShowPlacementGuide(state,PlayerSlot.Zero,0);Assert.That(guide.GetComponent<MeshFilter>().sharedMesh,Is.SameAs(next));
            board.HidePlacementGuide();Assert.That(guide.gameObject.activeSelf,Is.False);
            var backdrop=world.transform.Find("Surrounding landscape").GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(board.gameObject);yield return null;yield return null;
            Assert.That(next==null&&mesh==null&&backdrop==null,Is.True,"Added scenery/placement meshes must not leak across matches");
        }
    }
}
