using System;
using System.Collections;
using System.IO;
using System.Reflection;
using DefenseDuell.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class EnergyBeam15Tests
    {
        [UnityTest] public IEnumerator ActualPurchasedTeslaAndLaserUseEnergyRenderingIncludingReducedEffects()
        {
            foreach(int kind in new[]{5,7})foreach(bool reduced in new[]{false,true})
            {
                var basis=GameContent.CreateDefault(new GameRules(countdownTicks:0));
                var content=basis.WithMatchRules(new MatchRules(1,1000));
                var sim=new MatchSimulation(content,7,991);Assert.That(sim.ConfigureTowerAvailability(PlayerSlot.Zero,1<<kind),Is.True);
                void Send(PlayerSlot player,CommandType type,int value=0,MapPoint point=default)
                {
                    var command=new CommandEnvelope{SessionId=sim.State.SessionId,Sequence=sim.LastSequence(player)+1,
                        Type=type,Kind=value,Target=-1,PositionX=point.X,PositionY=point.Y,
                        ExpectedConfigRevision=type==CommandType.SetReady?sim.State.ConfigRevision:0,
                        ExpectedRosterRevision=type==CommandType.SetReady?sim.State.RosterRevision:0,
                        ExpectedWave=type==CommandType.SetWaveReady?sim.State.WaveId:0};
                    Assert.That(sim.Execute(player,command).Accepted,Is.True,type.ToString());
                }
                Send(PlayerSlot.Zero,CommandType.SetReady);Send(PlayerSlot.One,CommandType.SetReady);sim.Step();
                var start=content.Map.Waypoints[0];MapPoint position=default;bool found=false;
                for(int y=32;y<content.Map.Height-32&&!found;y+=16)for(int x=32;x<content.Map.Width-32;x+=16)
                {
                    long dx=x-start.X,dy=y-start.Y;
                    int comfortableRange=content.Towers[kind].Range-16;
                    if(dx*dx+dy*dy>(long)comfortableRange*comfortableRange)continue;
                    var point=new MapPoint(x,y);
                    if(PlacementValidator.ValidatePosition(content,sim.State,PlayerSlot.Zero,kind,-1,point)!=ErrorCode.None)continue;
                    position=point;found=true;break;
                }
                Assert.That(found,Is.True);Send(PlayerSlot.Zero,CommandType.BuildTower,kind,position);
                var root=new GameObject("Energy board proof",typeof(UnityEngine.Canvas));
                root.GetComponent<UnityEngine.Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var go=new GameObject("Energy board",typeof(RectTransform));go.transform.SetParent(root.transform,false);
                ((RectTransform)go.transform).sizeDelta=new Vector2(1280,720);
                var board=go.AddComponent<BoardView>();
                try
                {
                    board.Initialize(content,Array.Empty<Sprite>(),Array.Empty<Sprite>(),true,_=>{});board.ReducedEffects=reduced;
                    board.Display(sim.State,PlayerSlot.Zero,-1);
                    Send(PlayerSlot.Zero,CommandType.SetWaveReady,1);Send(PlayerSlot.One,CommandType.SetWaveReady,1);
                    for(int i=0;i<30&&board.ActiveEnergyBeamCount==0;i++)
                    {
                        sim.Step();board.Display(sim.State,PlayerSlot.Zero,-1);
                        typeof(BoardView).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(board,null);
                    }
                    Assert.That(board.ActiveEnergyBeamCount,Is.GreaterThan(0),"Actual tower cooldown must trigger its visible energy pulse");
                    var batch=(EnergyBeamBatch)typeof(BoardView).GetField("energyBeams",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(board);
                    Assert.That(batch.DisplayMesh.vertexCount,Is.EqualTo(kind==5&&!reduced?64:8));
                    board.RefreshRenderNow();
                    var texture=(RenderTexture)typeof(BoardView).GetField("texture",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(board);
                    var previous=RenderTexture.active;var capture=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);
                    try
                    {
                        RenderTexture.active=texture;capture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);capture.Apply();
                        Directory.CreateDirectory("TestResults/Energy-v15");
                        File.WriteAllBytes("TestResults/Energy-v15/"+(kind==5?"tesla":"laser")+(reduced?"-reduced":"-normal")+".png",capture.EncodeToPNG());
                    }
                    finally{RenderTexture.active=previous;Object.Destroy(capture);}
                    board.CancelFeedback();Assert.That(batch.DisplayMesh.vertexCount,Is.Zero);
                }
                finally{Object.Destroy(root);}
                yield return null;
            }
        }
        [Test] public void LightningHasVisibleJaggedGeometryAndReducedEffectsKeepsQuietBeam()
        {
            var root=new GameObject("Energy test");var camera=root.AddComponent<Camera>();
            var batch=new EnergyBeamBatch(root.transform,0,false);
            try
            {
                Assert.That(batch.Trigger(Vector3.zero,new Vector3(4,1,0),true,1,false),Is.True);
                Assert.That(batch.Update(1.01,camera,false),Is.True);
                Assert.That(batch.ActiveCount,Is.EqualTo(1));Assert.That(batch.DisplayMesh.vertexCount,Is.EqualTo(64));
                Assert.That(batch.DisplayMesh.bounds.size.x,Is.GreaterThan(3.8f));
                bool visibleAlpha=false;foreach(var color in batch.DisplayMesh.colors32)visibleAlpha|=color.a>100;
                Assert.That(visibleAlpha,Is.True);
                batch.Clear();batch.Trigger(Vector3.zero,new Vector3(4,1,0),true,2,true);batch.Update(2.01,camera,true);
                Assert.That(batch.DisplayMesh.vertexCount,Is.EqualTo(8));Assert.That(batch.ActiveCount,Is.EqualTo(1));
                batch.Update(2.2,camera,true);Assert.That(batch.ActiveCount,Is.Zero);Assert.That(batch.DisplayMesh.vertexCount,Is.Zero);
            }
            finally{batch.Dispose();Object.DestroyImmediate(root);}
        }
        [Test] public void PoolIsBoundedAndClearsAtClockResetOrLifecycleBoundary()
        {
            var root=new GameObject("Energy budget test");var batch=new EnergyBeamBatch(root.transform,0,true);
            try
            {
                for(int i=0;i<100;i++)batch.Trigger(Vector3.zero,new Vector3(3,i*.001f,0),true,10,false);
                batch.Update(10.01,null,false);Assert.That(batch.ActiveCount,Is.EqualTo(8));
                Assert.That(batch.DisplayMesh.vertexCount,Is.EqualTo(8*64));
                batch.Update(9,null,false);Assert.That(batch.ActiveCount,Is.Zero);
                Assert.That(batch.Trigger(new Vector3(float.NaN,0,0),Vector3.one,true,11,false),Is.False);
                batch.Trigger(Vector3.zero,Vector3.one,false,12,false);batch.Update(12.01,null,false);
                Assert.That(batch.DisplayMesh.vertexCount,Is.EqualTo(8));
                batch.Clear();Assert.That(batch.ActiveCount,Is.Zero);Assert.That(batch.DisplayMesh.vertexCount,Is.Zero);
            }
            finally{batch.Dispose();Object.DestroyImmediate(root);}
        }
    }
}
