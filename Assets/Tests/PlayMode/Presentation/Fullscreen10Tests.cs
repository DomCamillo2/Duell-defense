using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class Fullscreen10Tests
    {
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        static void Call(object target,string name)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
        [Serializable] sealed class Measurement
        {
            public int map,logicalWidth,logicalHeight,renderWidth,renderHeight,msaaSamples;
            public float newCameraSize,previousCameraSize,linearEnlargement,groundWidthFraction,groundHeightFraction;
        }
        [Serializable] sealed class Evidence { public string scope="Actual Unity camera geometry; full-resolution render targets recorded separately in Hud-v12 capture sidecars"; public List<Measurement> samples=new List<Measurement>(); }
        // Reconstruct the released 0.9 fit from its original terrain and tower envelope.
        static float PreviousCameraSize(BoardView board,int width,int height)
        {
            var resources=Field<DioramaResources>(board,"resources");var camera=Field<Camera>(board,"viewCamera");var map=Field<MapDefinition>(board,"map");
            var inverse=Quaternion.Inverse(camera.transform.localRotation);float minX=float.PositiveInfinity,minY=minX,maxX=float.NegativeInfinity,maxY=maxX;
            void Include(Vector3 p){var v=inverse*(p-resources.MapCenter);minX=Mathf.Min(minX,v.x);maxX=Mathf.Max(maxX,v.x);minY=Mathf.Min(minY,v.y);maxY=Mathf.Max(maxY,v.y);}
            foreach(var point in resources.Terrain.vertices)Include(point);
            float radius=Mathf.Max(1.25f,resources.MaximumTowerRadius),top=Mathf.Max(3.05f,.18f+resources.MaximumTowerHeight);
            var min=DioramaResources.Point(new MapPoint(0,map.Height),.18f)-new Vector3(radius,0,radius);
            var max=DioramaResources.Point(new MapPoint(map.Width,0),top)+new Vector3(radius,0,radius);
            for(int x=0;x<2;x++)for(int y=0;y<2;y++)for(int z=0;z<2;z++)Include(new Vector3(x==0?min.x:max.x,y==0?min.y:max.y,z==0?min.z:max.z));
            return Mathf.Max((maxY-minY)*.5f/((height-204f)/height),(maxX-minX)*.5f/((width-156f)/height))*1.025f;
        }
        [UnityTest] public IEnumerator AllMapsFillTheDisplayAndProduceMeasuredHighResolutionHudCaptures()
        {
            SceneManager.LoadScene("Boot");yield return new WaitForSecondsRealtime(.3f);
            var session=Object.FindFirstObjectByType<SessionCoordinator>();var ui=Object.FindFirstObjectByType<GameUI>();
            var safe=Field<RectTransform>(ui,"safe");var canvas=(RectTransform)safe.parent;var report=new Evidence();
            try
            {
                for(int map=0;map<MapCatalog.Count;map++)
                {
                    Assert.That(session.SelectMap(map),Is.True);session.StartOffline((uint)(101+map));session.enabled=false;
                    session.State.Phase=MatchPhase.Playing;session.State.WavePhase=WavePhase.Active;
                    session.State.Players[0].Gold=1250;session.State.Players[0].AvailableTowerMask=(1<<session.Content.Towers.Length)-1;
                    foreach(int width in new[]{900,1280,1600})
                    {
                        safe.anchorMin=safe.anchorMax=safe.pivot=new Vector2(.5f,.5f);safe.anchoredPosition=Vector2.zero;
                        safe.sizeDelta=new Vector2(width,720);safe.localScale=Vector3.one*Mathf.Min(canvas.rect.width/width,canvas.rect.height/720);
                        Canvas.ForceUpdateCanvases();Call(ui,"Rebuild");yield return null;Call(ui,"RefreshLive");Canvas.ForceUpdateCanvases();
                        var board=Field<BoardView>(ui,"ownBoard");board.ResetView();board.RefreshRenderNow();
                        // Measure inside the physical capture, before it restores the small editor viewport.
                        CompactHud08Tests.CaptureHud("fullscreen-map-"+map+"-"+width,()=>
                        {
                            var camera=Field<Camera>(board,"viewCamera");
                            Assert.That(board.RenderPixelSize,Is.EqualTo(new Vector2Int(Mathf.RoundToInt(width*1.5f),1080)),"PNG size alone is not a sharpness proof");
                            Assert.That(board.RenderSamples,Is.GreaterThanOrEqualTo(1));
                            float current=camera.orthographicSize,previous=PreviousCameraSize(board,width,720);
                            Assert.That(previous/current,Is.GreaterThan(1.20f),"Actual map must grow, not merely gain a background");
                            var world=Field<GameObject>(board,"world");float minX=1,maxX=0,minY=1,maxY=0;
                            foreach(int x in new[]{0,session.Content.Map.Width})foreach(int y in new[]{0,session.Content.Map.Height})
                            {
                                var point=camera.WorldToViewportPoint(world.transform.TransformPoint(DioramaResources.Point(new MapPoint(x,y),.18f)));
                                Assert.That(point.x,Is.InRange(0f,1f));Assert.That(point.y,Is.InRange(0f,1f));
                                minX=Mathf.Min(minX,point.x);maxX=Mathf.Max(maxX,point.x);minY=Mathf.Min(minY,point.y);maxY=Mathf.Max(maxY,point.y);
                            }
                            Assert.That(Mathf.Max(maxX-minX,maxY-minY),Is.GreaterThan(.90f),"Playable ground must fill at least one display dimension");
                            report.samples.Add(new Measurement{map=map,logicalWidth=width,logicalHeight=720,newCameraSize=current,previousCameraSize=previous,
                                linearEnlargement=previous/current,groundWidthFraction=maxX-minX,groundHeightFraction=maxY-minY,renderWidth=board.RenderPixelSize.x,renderHeight=board.RenderPixelSize.y,msaaSamples=board.RenderSamples});
                        });
                        // Panning must bring a HUD-covered base back into the central play area at the default zoom.
                        var end=session.Content.Map.Waypoints[session.Content.Map.Waypoints.Length-1];var before=board.MapPointToScreen(end);
                        var r=(RectTransform)board.transform;var centre=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
                        board.PanByScreenDelta(centre-before);yield return null;
                        Assert.That(Vector2.Distance(board.MapPointToScreen(end),centre),Is.LessThan(Vector2.Distance(before,centre)),"Pan must reveal the base without a forced tiny overview");
                        board.ResetView();
                    }
                    session.enabled=true;session.Shutdown();yield return null;
                }
                Directory.CreateDirectory("TestResults/Fullscreen-v15");File.WriteAllText("TestResults/Fullscreen-v15/framing.json",JsonUtility.ToJson(report,true));
            }
            finally
            {
                session.enabled=true;session.Shutdown();safe.localScale=Vector3.one;var a=Screen.safeArea;
                safe.anchorMin=new Vector2(a.xMin/Screen.width,a.yMin/Screen.height);safe.anchorMax=new Vector2(a.xMax/Screen.width,a.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
            }
        }
    }
}
