using System;
using System.Collections;
using System.Reflection;
using BigInteger=System.Numerics.BigInteger;
using DefenseDuell.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace DefenseDuell.Presentation.Tests
{
    public sealed class BoardCapacityTests
    {
        [UnityTest] public IEnumerator SparseLogicalSlotsAndLargeArmiesGrowPoolsAndReleaseOnRematch()
        {
            var root=new GameObject("capacity canvas",typeof(Canvas));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var go=new GameObject("capacity board",typeof(RectTransform));go.transform.SetParent(root.transform,false);
            var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(1000,650);
            var board=go.AddComponent<BoardView>();var content=GameContent.CreateDefault();
            try
            {
                board.Initialize(content,Array.Empty<Sprite>(),Array.Empty<Sprite>(),true,_=>{});board.RemotePlayback=true;
                var state=new MatchSimulation(content,7,123).State;var huge=BigInteger.One<<2048;
                for(int i=0;i<40;i++)state.Towers.Add(new TowerState { Id=i+1,Owner=PlayerSlot.Zero,Slot=i==39?1000000:i,SiteId=-1,Kind=0,
                    Position=new MapPoint(60+(i%10)*85,60+(i/10)*100),Level=huge,DamageLevel=huge,Invested=TowerUpgradeRules.TotalInvestment(content,0,huge) });
                var recipe=UnitResearchRules.Resolve(content,0,huge,UnitOrigin.PaidPvP,1);
                for(int i=0;i<120;i++)state.Units.Add(new UnitState { Id=100+i,Defender=PlayerSlot.Zero,Kind=0,WaveId=1,Origin=UnitOrigin.PaidPvP,ResearchLevel=huge,Hp=recipe.MaxHp/2,Distance=i*4 });
                board.Display(state,PlayerSlot.Zero);yield return null;
                Assert.That(board.VisibleCount,Is.EqualTo(120));Assert.That(board.SlotCount,Is.GreaterThanOrEqualTo(40));Assert.That(board.SlotCount,Is.LessThan(1000));
                Assert.That(board.PlayConfirmedFeedback(40,1000000,0,huge,true,huge,0),Is.True,"Exact huge-level ACK resolves sparse slot");
                var world=(GameObject)typeof(BoardView).GetField("world",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(board);
                foreach(var t in world.GetComponentsInChildren<Transform>(true))
                {Assert.That(float.IsNaN(t.localScale.x)||float.IsInfinity(t.localScale.x),Is.False,t.name);}
                state.SessionId++;state.Units.Clear();state.Towers.Clear();board.Display(state,PlayerSlot.Zero);yield return null;
                Assert.That(board.VisibleCount,Is.Zero);Assert.That(board.PooledCount,Is.GreaterThanOrEqualTo(120));
                Assert.That(board.PlayConfirmedFeedback(40,1000000,0,huge,true,huge,0),Is.False);
            }
            finally {Object.Destroy(root);}
            yield return null;
        }
    }
}
