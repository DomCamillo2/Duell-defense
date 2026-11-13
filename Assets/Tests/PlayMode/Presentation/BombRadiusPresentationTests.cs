using System;
using System.Collections;
using System.IO;
using System.Reflection;
using DefenseDuell.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class BombRadiusPresentationTests
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Hidden).GetValue(o);
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,Hidden).SetValue(o,value);
        static void Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Hidden).Invoke(o,args);
        static BoardView MakeBoard(GameContent content,out GameObject root)
        {
            root=new GameObject("Bomb radius fixture",typeof(Canvas));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var go=new GameObject("Bomb radius board",typeof(RectTransform));go.transform.SetParent(root.transform,false);
            ((RectTransform)go.transform).sizeDelta=new Vector2(1280,720);
            var board=go.AddComponent<BoardView>();board.Initialize(content,Array.Empty<Sprite>(),Array.Empty<Sprite>(),true,_=>{});return board;
        }
        static MatchState State(GameContent c,int secondary)
        {
            var s=new MatchSimulation(c,7,932).State;s.Phase=MatchPhase.Playing;s.WavePhase=WavePhase.Active;
            int distance=c.Map.Length/2;var p=c.Map.PositionAt(distance);
            s.Towers.Add(new TowerState{Id=1,Kind=1,Owner=PlayerSlot.Zero,Slot=0,SiteId=-1,Position=new MapPoint(p.X+75,p.Y+50),Level=secondary,RangeLevel=secondary});
            for(int i=0;i<5;i++)s.Units.Add(new UnitState{Id=100+i,Kind=i%4,Defender=PlayerSlot.Zero,Distance=distance-i*35,Hp=10000,WaveId=1,Origin=UnitOrigin.PaidPvP});
            return s;
        }
        static void Level(TowerState t,int secondary){t.Level=secondary;t.DamageLevel=0;t.RangeLevel=secondary;}
        static ContactFxEvent Explosion(BoardView board)
        {
            var events=Field<ContactFxEvents>(Field<ContactFxBatch>(board,"contactFx"),"events");
            for(int i=0;i<ContactFxEvents.Capacity;i++)if(events[i].Active&&events[i].Kind==ContactFxKind.Explosion)return events[i];
            Assert.Fail("Actual BoardView shot did not produce explosion event");return default;
        }
        static void FinishShot(BoardView board)
        {
            var shots=Field<Array>(board,"shots");bool found=false;
            foreach(var shot in shots)if(Field<bool>(shot,"Active")){Set(shot,"Began",Time.unscaledTime-.18f);found=true;}
            Assert.That(found,Is.True,"Cooldown change must produce the real pooled shot");Call(board,"Update");
        }
        static void Capture(BoardView board,string name,int radius)
        {
            board.RefreshRenderNow();var rt=Field<RenderTexture>(board,"texture");var prior=RenderTexture.active;Texture2D image=null;
            try
            {
                RenderTexture.active=rt;image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();Directory.CreateDirectory("TestResults/BombRadius-v15");
                File.WriteAllBytes("TestResults/BombRadius-v15/"+name+".png",image.EncodeToPNG());
                File.WriteAllText("TestResults/BombRadius-v15/"+name+".json","{\"scope\":\"actual BoardView RenderTexture; overlay label asserted separately\",\"splashRadius\":"+radius+",\"width\":"+rt.width+",\"height\":"+rt.height+"}");
            }
            finally{RenderTexture.active=prior;if(image!=null)Object.Destroy(image);}
        }
        [UnityTest] public IEnumerator SelectedCannonShowsTrueRadiusAtTargetAndLabelledSampleWithoutChangingTargetingRange()
        {
            var c=GameContent.CreateDefault();var board=MakeBoard(c,out var root);Mesh owned=null;
            try
            {
                var state=State(c,0);var t=state.Towers[0];board.Display(state,PlayerSlot.Zero,1);yield return null;
                var preview=Field<Transform>(board,"splashPreview");owned=Field<Mesh>(board,"splashPreviewMesh");
                var label=Field<Text>(board,"splashPreviewLabel");var range=Field<Transform>(board,"range");
                foreach(int level in new[]{0,1,5,10,20})
                {
                    Level(t,level);board.Display(state,PlayerSlot.Zero,1);var stats=TowerUpgradeRules.Stats(c,t);
                    Assert.That(preview.gameObject.activeSelf,Is.True);Assert.That(label.gameObject.activeSelf,Is.True);Assert.That(label.font,Is.Not.Null);
                    Assert.That(preview.localScale.x*owned.bounds.size.x,Is.EqualTo(stats.SplashRadius*2/50f).Within(.002f));
                    var target=c.Map.PositionAt(state.Units[0].Distance);
                    Assert.That(preview.localPosition.x,Is.EqualTo(DioramaResources.Point(target).x).Within(.001f));
                    Assert.That(preview.localPosition.z,Is.EqualTo(DioramaResources.Point(target).z).Within(.001f));
                    Assert.That(label.text,Does.Contain(stats.SplashRadius.ToString()));Assert.That(range.localScale.x,Is.EqualTo(155/50f));
                    if(level==0||level==10)Capture(board,"selected-radius-"+level,stats.SplashRadius);
                }
                board.ReducedEffects=true;board.Display(state,PlayerSlot.Zero,1);Assert.That(preview.gameObject.activeSelf,Is.True,"Reduced effects retains static game information");
                Capture(board,"selected-reduced-effects",TowerUpgradeRules.Stats(c,t).SplashRadius);
                state.Units.Clear();board.Display(state,PlayerSlot.Zero,1);Assert.That(label.text,Does.StartWith("Radius-Vorschau"));Assert.That(preview.gameObject.activeSelf,Is.True);
                board.Display(state,PlayerSlot.Zero);Assert.That(preview.gameObject.activeSelf,Is.False);Assert.That(label.gameObject.activeSelf,Is.False);
                board.Display(state,PlayerSlot.Zero,1);state.SessionId++;state.Towers.Clear();board.Display(state,PlayerSlot.Zero);
                Assert.That(preview.gameObject.activeSelf,Is.False);Assert.That(label.gameObject.activeSelf,Is.False);
            }
            finally{Object.Destroy(root);}
            yield return null;yield return null;Assert.That(owned==null,Is.True,"Owned preview geometry must be disposed");
        }
        [UnityTest] public IEnumerator ActualCooldownShotsKeepLaunchRadiusAndRenderLargerUpgradedBlastWithinSameBudget()
        {
            var c=GameContent.CreateDefault();var board=MakeBoard(c,out var root);
            try
            {
                var s=State(c,0);var t=s.Towers[0];board.Display(s,PlayerSlot.Zero);yield return null;
                var fx=Field<ContactFxBatch>(board,"contactFx");var camera=Field<Camera>(board,"viewCamera");var world=Field<GameObject>(board,"world");
                var mesh=world.transform.Find("Combined bounded dust and sparks").GetComponent<MeshFilter>().sharedMesh;
                int vertexCapacity=mesh.vertexCount;
                t.CooldownTicks=32;s.Tick++;board.Display(s,PlayerSlot.Zero);
                Level(t,10);s.Tick++;board.Display(s,PlayerSlot.Zero);FinishShot(board);
                var old=Explosion(board);Assert.That(old.Radius,Is.EqualTo(90/50f),"Mid-flight upgrade must not resize the old shot");
                Assert.That(old.Y,Is.EqualTo(ContactFxBatch.RouteGroundPlane+.012f).Within(.001f));
                Assert.That(mesh.bounds.size.x,Is.EqualTo(2*old.Radius+.01f).Within(.015f),"Explosion mesh must be visible in its triggering frame");
                fx.UpdateEffects(old.Began+.10,camera);Assert.That(mesh.bounds.size.x,Is.EqualTo(2*old.Radius+.01f).Within(.015f));
                Capture(board,"impact-radius-0",90);fx.Clear();
                t.CooldownTicks=0;s.Tick++;board.Display(s,PlayerSlot.Zero);t.CooldownTicks=32;s.Tick++;board.Display(s,PlayerSlot.Zero);FinishShot(board);
                var upgraded=Explosion(board);Assert.That(upgraded.Radius,Is.EqualTo(181/50f));
                fx.UpdateEffects(upgraded.Began+.10,camera);Assert.That(mesh.bounds.size.x,Is.EqualTo(2*upgraded.Radius+.01f).Within(.015f));
                Assert.That(mesh.vertexCount,Is.EqualTo(vertexCapacity));Capture(board,"impact-radius-10",181);
                board.PowerSaving=true;Assert.That(fx.ActiveEffectCount,Is.LessThanOrEqualTo(ContactFxEvents.PowerCapacity));
                board.ReducedEffects=true;Assert.That(fx.ActiveEffectCount,Is.Zero);
                foreach(var shot in Field<Array>(board,"shots")){Assert.That(Field<bool>(shot,"Active"),Is.False);Assert.That(Field<float>(shot,"SplashRadius"),Is.Zero);}
                board.ReducedEffects=false;board.CancelFeedback();Assert.That(fx.ActiveEffectCount,Is.Zero);
                board.enabled=false;Assert.That(world.activeSelf,Is.False);board.enabled=true;
                s.SessionId++;s.Towers.Clear();s.Units.Clear();board.Display(s,PlayerSlot.Zero);Assert.That(fx.ActiveEffectCount,Is.Zero);
            }
            finally{Object.Destroy(root);}
            yield return null;yield return null;
        }
        [Test] public void ExplosionEventsRejectInvalidRadiiAndRespectReducedPowerExpiryAndCapacity()
        {
            var events=new ContactFxEvents();
            foreach(float radius in new[]{0f,-1,float.NaN,float.PositiveInfinity})
                Assert.That(events.Trigger(ContactFxKind.Explosion,0,.226f,0,1,.5f,0,0,false,false,radius),Is.False);
            Assert.That(events.Trigger(ContactFxKind.Explosion,0,.226f,0,1,.5f,0,0,true,false,1.8f),Is.False);
            for(int i=0;i<1000;i++)events.Trigger(ContactFxKind.Explosion,0,.226f,0,1,.5f,0,1,false,true,3.62f);
            Assert.That(events.ActiveCount,Is.EqualTo(ContactFxEvents.PowerCapacity));
            Assert.That(events.Trigger(ContactFxKind.Build,0,0,0,1,1,1,1.2,false,true),Is.True,"Confirmed build must replace incidental explosions");
            Assert.That(events.Trigger(ContactFxKind.Upgrade,0,0,0,1,1,1,1.21,false,true),Is.True,"Confirmed upgrade must replace incidental explosions");
            events.Tick(2,false,true);Assert.That(events.ActiveCount,Is.Zero);
        }
    }
}
