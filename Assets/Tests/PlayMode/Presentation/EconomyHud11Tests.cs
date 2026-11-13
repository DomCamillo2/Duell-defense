using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BigInteger=System.Numerics.BigInteger;
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
    public sealed class EconomyHud11Tests
    {
        SessionCoordinator session;
        GameUI ui;
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
        static void SetField(object target,string name,object value)=>target.GetType().GetField(name,Private).SetValue(target,value);
        static void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Private).Invoke(target,args);
        MatchSimulation Simulation=>Field<MatchSimulation>(session,"simulation");
        PlayerState Player=>session.State.Players[(int)session.LocalSlot];
        void Refresh(){Call(ui,"RefreshLive");Canvas.ForceUpdateCanvases();}
        void SampleForecast(){SetField(ui,"nextAdditionalForecastAt",0d);Refresh();}
        Button Find(string name){Ui16TestNavigation.Reveal(name);return ui.GetComponentsInChildren<Button>(true).First(b=>b.name==name&&b.gameObject.activeInHierarchy);}
        static string Copy(Component root)=>string.Join("\n",root.GetComponentsInChildren<Text>().Select(t=>t.text));
        string Delay(int kind)=>Field<Text[]>(ui,"sendDelayText")[kind].text;
        static Vector2 Center(Component c)
        {var r=(RectTransform)c.transform;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));}
        static void Submit(Button button)
        {
            Assert.That(button.interactable,Is.True,button.name);EventSystem.current.SetSelectedGameObject(button.gameObject);
            ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
        }
        static void Tap(Button button,int pointer)
        {
            var p=Center(button);var e=new PointerEventData(EventSystem.current){position=p,pressPosition=p,pointerId=pointer,
                button=PointerEventData.InputButton.Left,eligibleForClick=true};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);Assert.That(hits,Is.Not.Empty);
            var hit=hits[0].gameObject;Assert.That(hit.GetComponentInParent<Button>(),Is.SameAs(button));
            ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerDownHandler);ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit,e,ExecuteEvents.pointerClickHandler);
        }
        [UnitySetUp] public IEnumerator Start()
        {
            SceneManager.LoadScene("Boot");yield return new WaitForSecondsRealtime(.2f);
            session=Object.FindFirstObjectByType<SessionCoordinator>();ui=Object.FindFirstObjectByType<GameUI>();
            session.StartOffline(83);session.enabled=false;
            session.State.Phase=MatchPhase.Playing;session.State.WavePhase=WavePhase.Preparing;
            session.State.PreparationRemaining=session.Content.WaveRules.PreparationTicks;
            Player.Gold=BigInteger.Pow(10,9);Player.AvailableTowerMask=127;
            yield return null;yield return null;Refresh();
        }
        [UnityTearDown] public IEnumerator Stop()
        {
            if(ui!=null){Call(ui,"CancelInteraction");Call(ui,"CloseAttackInfo");Call(ui,"CloseModal");}
            if(session!=null){session.enabled=true;session.Shutdown();}
            yield return null;
        }
        TowerState Build(int kind)
        {
            for(int y=80;y<session.Content.Map.Height-80;y+=61)for(int x=80;x<session.Content.Map.Width-80;x+=67)
            {
                var p=new MapPoint(x,y);
                if(PlacementValidator.ValidatePosition(session.Content,session.State,session.LocalSlot,kind,-1,p)!=ErrorCode.None)continue;
                session.Submit(CommandType.BuildTower,kind,-1,x,y);Assert.That(session.LastResult.Accepted,Is.True);
                var tower=session.State.Towers.Last();Call(ui,"SelectEntity",tower.Id);Refresh();return tower;
            }
            Assert.Fail("No legal position for economy test");return null;
        }
        SpawnOrder Buy(int kind)
        {
            var quote=SendRules.Quote(session.Content,session.State,session.LocalSlot,kind);Assert.That(quote.IsValid,Is.True);
            session.Submit(CommandType.SendUnit,kind,-1,0,0,quote.WaveId,quote.ResearchLevel,quote.EnemyTarget);
            Assert.That(session.LastResult.Accepted,Is.True);return session.State.SpawnOrders.Last();
        }
        void BeginWave()
        {
            session.Submit(CommandType.SetWaveReady,1,expectedWave:session.State.WaveId);Assert.That(session.LastResult.Accepted,Is.True);
            Simulation.Step();Assert.That(session.State.WavePhase,Is.EqualTo(WavePhase.Active));
        }
        void Ledger()=>Assert.That(EconomyRules.ValidateLedger(session.Content,session.State,session.LocalSlot),Is.EqualTo(ErrorCode.None));
        IEnumerator SetWidth(int width)
        {
            var safe=Field<RectTransform>(ui,"safe");var canvas=(RectTransform)safe.parent;
            safe.anchorMin=safe.anchorMax=safe.pivot=new Vector2(.5f,.5f);safe.anchoredPosition=Vector2.zero;safe.sizeDelta=new Vector2(width,720);
            safe.localScale=Vector3.one*Mathf.Min(canvas.rect.width/width,canvas.rect.height/720)*.94f;
            Canvas.ForceUpdateCanvases();Call(ui,"Rebuild");yield return null;Refresh();
        }
        static void Capture(string suffix)
        {
            // Root owns the shared helper and has switched its destination to Hud-v11.
            CompactHud08Tests.CaptureHud("economy11-"+suffix);
        }

        [UnityTest] public IEnumerator MineAndCombatRefundLabelsAndExactDialogMatchRealSaleCredit()
        {
            foreach(int kind in new[]{4,0})
            {
                var tower=Build(kind);Submit(Find(kind==4?"Upgrade income":"Upgrade damage"));
                Assert.That(session.LastResult.Accepted,Is.True);Refresh();yield return null;Refresh();
                Assert.That(tower.Level,Is.EqualTo(BigInteger.One));
                int percent=EconomyRules.RefundPercent(session.Content,kind);Assert.That(percent,Is.EqualTo(kind==4?40:65));
                var refund=EconomyRules.RefundGold(session.Content,tower);Assert.That(refund,Is.EqualTo(tower.Invested*percent/100));
                var sell=Find("Sell tower");var visual=sell.GetComponent<ActionButtonVisual>();
                Assert.That(visual.ValueText,Is.EqualTo("+"+GameNumbers.Compact(refund)+" G"));
                Assert.That(visual.PriceText,Is.EqualTo(percent+" % zurück"));
                Tap(Find("Building upgrade details"),1100+kind);yield return null;
                Assert.That(Copy(Field<RectTransform>(ui,"modal")),Does.Contain("Verkaufen: "+GameNumbers.Exact(refund)+" Gold zurück ("+percent+" %)"));
                Capture("refund-"+kind);yield return null;
                Call(ui,"CloseModal");Refresh();yield return null;
                var gold=Player.Gold;Tap(sell,1110+kind);Assert.That(session.LastResult.Accepted,Is.True);Refresh();
                Assert.That(session.State.Towers.Any(t=>t.Id==tower.Id),Is.False);
                Assert.That(Player.Gold,Is.EqualTo(gold+refund));
            }
        }

        [UnityTest] public IEnumerator BeforeBuyEtaBelongsToNewPacketAndSwarmStartDiffersFromCompletion()
        {
            for(int i=0;i<10;i++)Buy(0);BeginWave();SampleForecast();
            var estimate=SendQueueForecast.Estimate(session.Content,session.State,session.LocalSlot,3);
            var head=SendQueueForecast.Estimate(session.Content,session.State,session.LocalSlot);
            Assert.That(estimate.AdditionalFirstSpawnDelayTicks,Is.GreaterThan(head.FirstSpawnDelayTicks+session.Content.TickRate));
            Assert.That(estimate.MinimumDelayTicks,Is.GreaterThan(estimate.AdditionalFirstSpawnDelayTicks));
            string shown=Delay(3);Assert.That(shown,Does.Contain("≥"+(estimate.AdditionalFirstSpawnDelayTicks/session.Content.TickRate)+" s"));
            Assert.That(shown,Does.Not.Contain("≥"+(head.FirstSpawnDelayTicks/session.Content.TickRate)+" s"));
            int count=session.State.SpawnOrders.Count;var gold=Player.Gold;Refresh();
            Assert.That(session.State.SpawnOrders.Count,Is.EqualTo(count));Assert.That(Player.Gold,Is.EqualTo(gold),"Forecasting must not purchase");
            Submit(Find("Send class 3"));Assert.That(session.LastResult.Accepted,Is.True);
            var swarm=session.State.SpawnOrders.Last();Assert.That(swarm.Kind,Is.EqualTo(3));Assert.That(swarm.InitialCount,Is.EqualTo(4));
            int began=session.State.PlayTicks,first=-1,last=-1;
            for(int i=0;i<500&&swarm.Remaining>0;i++)
            {
                int prior=swarm.Remaining;Simulation.Step();
                if(swarm.Remaining==prior)continue;
                if(first<0)first=session.State.PlayTicks;
                last=session.State.PlayTicks;
            }
            Assert.That(swarm.Remaining,Is.Zero);Assert.That(first,Is.GreaterThan(began));Assert.That(last,Is.GreaterThan(first));
            Assert.That((BigInteger)(first-began),Is.GreaterThanOrEqualTo(estimate.AdditionalFirstSpawnDelayTicks));
            Assert.That((BigInteger)(last-began),Is.GreaterThanOrEqualTo(estimate.MinimumDelayTicks));Ledger();
            yield return null;
        }

        [UnityTest] public IEnumerator PartiallySpawnedSwarmShowsOnlyRemainingPaidGoldAndSeparateStartEnd()
        {
            var swarm=Buy(3);BeginWave();
            for(int i=0;i<80&&swarm.Remaining==swarm.InitialCount;i++)Simulation.Step();
            Assert.That(swarm.Remaining,Is.EqualTo(3));Ledger();SampleForecast();
            var remainingQuarter=4*swarm.PaidPrice-Player.SpawnedQuarterGoldByWave[swarm.WaveId-1];
            Assert.That(remainingQuarter,Is.EqualTo(3*swarm.PaidPrice));
            string remaining=GameNumbers.Ratio(remainingQuarter,4);
            Assert.That(remaining,Is.Not.EqualTo(GameNumbers.Exact(swarm.PaidPrice)));
            Assert.That(Field<Text>(ui,"fifoText").text,Does.Contain(remaining+" G gebunden"));
            var estimate=Field<SendQueueEstimate>(ui,"queueForecast");
            Assert.That(estimate.RemainingPaidQuarterGold,Is.EqualTo(remainingQuarter));Assert.That(estimate.RemainingUnits,Is.EqualTo((BigInteger)3));
            Assert.That(estimate.MinimumDelayTicks,Is.GreaterThan(estimate.FirstSpawnDelayTicks));
            yield return null;Tap(Find("Own paid attack queue"),1120);yield return null;Refresh();
            string details=Field<Text>(ui,"paidQueueDetailText").text;
            Assert.That(details,Does.Contain("Noch gebunden: "+remaining+" G"));
            Assert.That(details,Does.Contain("Erster Ausmarsch:"));Assert.That(details,Does.Contain("Alles ausgesandt:"));
            Assert.That(details,Does.Contain("Untergrenze"));Capture("partial-swarm-bound-gold");
        }

        [UnityTest] public IEnumerator PreparationFutureWaveAndFinalClearingNeverShowInventedCountdowns()
        {
            Buy(0);SampleForecast();
            Assert.That(Delay(0),Does.Not.Contain("≥"));Assert.That(Delay(0),Does.Contain("Wellenstart"));
            Assert.That(Field<Text>(ui,"fifoText").text,Does.Contain("ab Wellenstart"));
            // An authority phase checkpoint closes this send window; existing orders are still valid.
            session.State.WavePhase=WavePhase.Clearing;Refresh();
            Assert.That(Delay(0),Does.Contain("offen"),"A cache from preparation cannot promise the next wave's start");
            var future=Buy(1);Assert.That(future.WaveId,Is.EqualTo((System.Numerics.BigInteger)2));Ledger();SampleForecast();
            Assert.That(Delay(1),Does.Contain("offen"));Assert.That(Delay(1),Does.Not.Contain("≥"));
            Submit(Find("Own paid attack queue"));yield return null;Refresh();
            Assert.That(Field<Text>(ui,"paidQueueDetailText").text,Does.Contain("Alles ausgesandt: Zeitpunkt offen"));
            Call(ui,"CloseModal");
            // Empty final-clearing checkpoint; retain a valid ledger rather than old future orders.
            session.State.SpawnOrders.Clear();for(int i=0;i<Player.ReservedGoldByWave.Length;i++)Player.ReservedGoldByWave[Player.ReservedGoldByWave.FirstWave-1+i]=0;
            for(int i=0;i<Player.SpawnedQuarterGoldByWave.Length;i++)Player.SpawnedQuarterGoldByWave[Player.SpawnedQuarterGoldByWave.FirstWave-1+i]=0;Player.AttackIncomeUnits=0;
            session.State.WaveId=session.Content.WaveCount;Ledger();SampleForecast();
            foreach(int kind in new[]{0,1,2,3})
            {
                Assert.That(Find("Send class "+kind).interactable,Is.False);
                Assert.That(Delay(kind),Does.Not.Contain("≥"));Assert.That(Delay(kind),Does.Contain("Kein Ausmarsch"));
                Assert.That(Field<SendQueueEstimate[]>(ui,"displayedSendForecasts")[kind].IncludesAdditionalPurchase,Is.False);
            }
        }

        [UnityTest] public IEnumerator ForecastThrottleDoesNotDelayLiveResearchGoldOrWavePurchaseQuotes()
        {
            for(int i=0;i<10;i++)Buy(0);BeginWave();SampleForecast();
            Assert.That(Field<double>(ui,"nextAdditionalForecastAt")-Time.unscaledTimeAsDouble,Is.InRange(0d,.5d));
            // Hold the sampled ETA across deliberate authority changes without relying on
            // a busy CI/ARM machine completing all assertions inside 250 ms.
            double deadline=Time.unscaledTimeAsDouble+60;SetField(ui,"nextAdditionalForecastAt",deadline);
            var sampled=Field<SendQueueEstimate[]>(ui,"displayedSendForecasts")[0];int sampledTick=Field<int>(ui,"additionalForecastPlayTick");
            session.Submit(CommandType.SetSpeedVote,1);Assert.That(session.LastResult.Accepted,Is.True);
            Assert.That(session.State.SimulationSpeed,Is.EqualTo(2));
            for(int i=0;i<10;i++)Simulation.Step();Refresh();
            Assert.That(Field<double>(ui,"nextAdditionalForecastAt"),Is.EqualTo(deadline));
            Assert.That(Field<int>(ui,"additionalForecastPlayTick"),Is.EqualTo(sampledTick));
            var remaining=BigInteger.Max(0,sampled.AdditionalFirstSpawnDelayTicks-(session.State.PlayTicks-sampledTick));
            Assert.That(Delay(0),Does.Contain("≥"+(remaining/session.Content.TickRate)+" s"),"2x advances gameplay ticks without reusing a wall-clock countdown");
            session.Submit(CommandType.ResearchUnit,0,-1,0,0,0,Player.ResearchLevels[0]);Assert.That(session.LastResult.Accepted,Is.True);
            Player.Gold=session.Content.Units[0].Price-1;Refresh();
            Assert.That(Find("Send class 0").interactable,Is.False);
            Assert.That(Field<SendQuote[]>(ui,"displayedSendQuotes")[0].Code,Is.EqualTo(ErrorCode.InsufficientGold));
            Player.Gold=session.Content.Units[0].Price+7;session.State.WavePhase=WavePhase.Clearing;Refresh();
            var quote=Field<SendQuote[]>(ui,"displayedSendQuotes")[0];
            Assert.That(quote.ResearchLevel,Is.EqualTo(Player.ResearchLevels[0]));Assert.That(quote.WaveId,Is.EqualTo((System.Numerics.BigInteger)2));
            Assert.That(Field<Text[]>(ui,"sendQuoteText")[0].text,Does.Contain("W2 · S"+GameNumbers.Exact(quote.ResearchLevel)));
            Assert.That(Field<Text[]>(ui,"sendPrices")[0].text,Is.EqualTo(quote.Price+" G"));
            Assert.That(Delay(0),Does.Contain("offen"));Assert.That(Field<double>(ui,"nextAdditionalForecastAt"),Is.EqualTo(deadline));
            Submit(Find("Send class 0"));Assert.That(session.LastResult.Accepted,Is.True);Refresh();
            var purchased=session.State.SpawnOrders.Last();Assert.That(purchased.ResearchLevel,Is.EqualTo(quote.ResearchLevel));
            Assert.That(purchased.WaveId,Is.EqualTo((System.Numerics.BigInteger)2));Assert.That(purchased.PaidPrice,Is.EqualTo(quote.Price));
            Assert.That(Player.Gold,Is.EqualTo((BigInteger)7));Ledger();yield return null;
        }

        [UnityTest] public IEnumerator LongFifoHasReadableHeaderAndReachableLastOrderAtNarrowAndWideWidths()
        {
            const int count=235;
            for(int i=0;i<count;i++){if(i>0&&i%20==0)session.State.Tick+=session.Content.TickRate;Buy(i%4);}Ledger();
            foreach(int width in new[]{900,1280})
            {
                yield return SetWidth(width);Tap(Find("Own paid attack queue"),1130+width);yield return null;Refresh();
                var scroll=Field<ScrollRect>(ui,"paidQueueScroll");var rows=Field<List<Text>>(ui,"virtualQueueRows");
                var header=Field<Text>(ui,"paidQueueDetailText");
                Assert.That(Field<bool>(ui,"virtualQueue"),Is.True);Assert.That(rows.Count,Is.LessThan(count));
                Assert.That(header.preferredHeight,Is.LessThanOrEqualTo(header.rectTransform.rect.height+.5f),"Queue summary is truncated at "+width);
                Assert.That(header.text,Does.Contain("235 Pakete"));Assert.That(header.text,Does.Contain("Noch gebunden:"));
                var first=rows.Single(t=>t.gameObject.activeInHierarchy&&t.text.StartsWith("1. W"));
                var headerCorners=new Vector3[4];var firstCorners=new Vector3[4];header.rectTransform.GetWorldCorners(headerCorners);first.rectTransform.GetWorldCorners(firstCorners);
                Assert.That(scroll.viewport.InverseTransformPoint(firstCorners[1]).y,Is.LessThanOrEqualTo(scroll.viewport.InverseTransformPoint(headerCorners[0]).y),"First order overlaps forecast summary");
                Capture("fifo-header-"+width);yield return null;Refresh();
                scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return null;Refresh();
                var last=rows.Single(t=>t.gameObject.activeInHierarchy&&t.text.StartsWith(count+". W"));
                Assert.That(last.text,Does.Contain(session.Content.Units[session.State.SpawnOrders.Last().Kind].Name));
                var corners=new Vector3[4];last.rectTransform.GetWorldCorners(corners);
                Assert.That(scroll.viewport.InverseTransformPoint(corners[0]).y,Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin-2));
                Assert.That(scroll.viewport.InverseTransformPoint(corners[1]).y,Is.LessThanOrEqualTo(scroll.viewport.rect.yMax+2));
                Assert.That(rows.Count,Is.LessThanOrEqualTo(Mathf.CeilToInt(scroll.viewport.rect.height/26f)+2));
                Capture("fifo-last-"+width);yield return null;Call(ui,"CloseModal");yield return null;
            }
        }
    }
}
