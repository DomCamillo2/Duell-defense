using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BigInteger = System.Numerics.BigInteger;
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
    public sealed class UnlimitedHud10Tests
    {
        SessionCoordinator session;
        GameUI ui;
        Action<PlayerSlot,CommandEnvelope,CommandResult,byte[],byte[]> observer;
        readonly List<CommandEnvelope> commands=new List<CommandEnvelope>();
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
        static void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Private).Invoke(target,args);
        void Refresh(){Call(ui,"RefreshLive");Canvas.ForceUpdateCanvases();}
        Button Find(string name)=>ui.GetComponentsInChildren<Button>(true).First(b=>b.name==name&&b.gameObject.activeInHierarchy);
        static string Copy(Component root)=>string.Join("\n",root.GetComponentsInChildren<Text>().Select(t=>t.text));
        static void Submit(Button button)
        {
            Assert.That(button.interactable,Is.True,button.name);
            EventSystem.current.SetSelectedGameObject(button.gameObject);
            ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
        }
        void ObserveCommands()
        {
            observer=(player,command,result,before,after)=>commands.Add(command);
            typeof(SessionCoordinator).GetEvent("AuthoritativeCommandObserved",Private).GetAddMethod(true).Invoke(session,new object[]{observer});
        }
        [UnitySetUp] public IEnumerator Start()
        {
            SceneManager.LoadScene("Boot");yield return new WaitForSecondsRealtime(.2f);
            session=Object.FindFirstObjectByType<SessionCoordinator>();ui=Object.FindFirstObjectByType<GameUI>();
            session.StartOffline(71);session.enabled=false;
            session.State.Phase=MatchPhase.Playing;session.State.WavePhase=WavePhase.Preparing;
            session.State.PreparationRemaining=session.Content.WaveRules.PreparationTicks;
            session.State.Players[0].AvailableTowerMask=127;session.State.Players[0].Gold=BigInteger.Pow(10,80);
            commands.Clear();yield return null;yield return null;Refresh();
        }
        [UnityTearDown] public IEnumerator Stop()
        {
            if(session!=null&&observer!=null)
                typeof(SessionCoordinator).GetEvent("AuthoritativeCommandObserved",Private).GetRemoveMethod(true).Invoke(session,new object[]{observer});
            observer=null;
            if(ui!=null){Call(ui,"CloseAttackInfo");Call(ui,"CloseModal");}
            if(session!=null){session.enabled=true;session.Shutdown();}
            yield return null;
        }
        TowerState Build(int kind)
        {
            for(int y=80;y<session.Content.Map.Height-80;y+=61)
            for(int x=80;x<session.Content.Map.Width-80;x+=67)
            {
                var point=new MapPoint(x,y);
                if(PlacementValidator.ValidatePosition(session.Content,session.State,session.LocalSlot,kind,-1,point)!=ErrorCode.None)continue;
                session.Submit(CommandType.BuildTower,kind,-1,x,y);Assert.That(session.LastResult.Accepted,Is.True);
                var tower=session.State.Towers.Last();Call(ui,"SelectEntity",tower.Id);Refresh();return tower;
            }
            Assert.Fail("No legal test building position");return null;
        }
        void SetLevels(TowerState tower,BigInteger primary,BigInteger secondary)
        {
            // Deliberate authoritative checkpoint fixture: avoid billions of setup purchases.
            tower.DamageLevel=primary;tower.RangeLevel=secondary;tower.Level=primary+secondary;
            tower.Invested=TowerUpgradeRules.TotalInvestment(session.Content,tower.Kind,tower.Level);
            Assert.That(TowerUpgradeRules.ValidateState(session.Content,tower),Is.EqualTo(ErrorCode.None));Refresh();
        }
        [UnityTest] public IEnumerator FrostKeepsBuyingPastTenAndNeverRoutesThroughDamage()
        {
            var tower=Build(2);ObserveCommands();var original=TowerUpgradeRules.Stats(session.Content,tower);
            for(int level=1;level<=12;level++)
            {
                var quote=TowerUpgradeRules.Quote(session.Content,tower,TowerUpgradePath.FrostDuration);
                var gold=session.State.Players[0].Gold;var button=Find("Upgrade damage");
                Assert.That(Copy(button),Does.Contain("Frostdauer"));Assert.That(Copy(button),Does.Not.Contain("Schaden"));
                Submit(button);Assert.That(session.LastResult.Accepted,Is.True);Refresh();
                Assert.That(tower.DamageLevel,Is.EqualTo((BigInteger)level));Assert.That(tower.RangeLevel,Is.EqualTo(BigInteger.Zero));
                Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold-quote.Price));
                Assert.That(TowerUpgradeRules.Stats(session.Content,tower).Damage,Is.EqualTo(original.Damage));
                Assert.That(commands.Last().Kind,Is.EqualTo((int)TowerUpgradePath.FrostDuration));
                yield return null;
            }
            Assert.That(Find("Upgrade damage").interactable,Is.True);
            Assert.That(TowerUpgradeRules.Stats(session.Content,tower).SlowDurationTicks,Is.GreaterThan(original.SlowDurationTicks));
            Assert.That(TowerUpgradeRules.Quote(session.Content,tower,TowerUpgradePath.Damage).Code,Is.EqualTo(ErrorCode.InvalidKind));
        }
        [UnityTest] public IEnumerator SecondaryTenToElevenDisplaysEveryChangedStatAndBuysTheCorrectPath()
        {
            ObserveCommands();
            foreach(int kind in new[]{0,1,2,3,5,6})
            {
                var tower=Build(kind);SetLevels(tower,0,10);
                var path=TowerUpgradeRules.SecondaryPath(session.Content,kind);
                var quote=TowerUpgradeRules.Quote(session.Content,tower,path);
                var preview=TowerUpgradeRules.Preview(session.Content,tower,path);
                Assert.That(preview.Length,Is.GreaterThanOrEqualTo(2),"Secondary continuation must disclose its added class power");
                var button=Find("Upgrade range");string shown=button.GetComponent<ActionButtonVisual>().ValueText;
                foreach(var metric in preview)
                    Assert.That(shown,Does.Contain(GameNumbers.Ratio(metric.Current,metric.Divisor)+" → "+GameNumbers.Ratio(metric.Next,metric.Divisor)),metric.Label);
                Assert.That(shown.Split('\n').Length,Is.EqualTo(preview.Length));
                var gold=session.State.Players[0].Gold;Submit(button);Assert.That(session.LastResult.Accepted,Is.True);Refresh();
                var actual=TowerUpgradeRules.Stats(session.Content,tower);
                Assert.That(tower.RangeLevel,Is.EqualTo((BigInteger)11));Assert.That(tower.DamageLevel,Is.EqualTo(BigInteger.Zero));
                Assert.That(actual.Damage,Is.EqualTo(quote.Next.Damage));Assert.That(actual.SlowDurationTicks,Is.EqualTo(quote.Next.SlowDurationTicks));
                Assert.That(actual.Range,Is.EqualTo(quote.Next.Range));Assert.That(actual.SplashRadius,Is.EqualTo(quote.Next.SplashRadius));
                Assert.That(actual.JumpRange,Is.EqualTo(quote.Next.JumpRange));Assert.That(actual.ArmorPiercing,Is.EqualTo(quote.Next.ArmorPiercing));
                Assert.That(commands.Last().Kind,Is.EqualTo((int)path));Assert.That(session.State.Players[0].Gold,Is.EqualTo(gold-quote.Price));
                yield return null;
            }
        }
        [UnityTest] public IEnumerator BeyondInt64DetailsStayExactAndPurchaseLeavesTheExactRemainder()
        {
            var tower=Build(0);var huge=(BigInteger)long.MaxValue+19;SetLevels(tower,huge,0);
            var quote=TowerUpgradeRules.Quote(session.Content,tower,TowerUpgradePath.Firepower);
            Assert.That(quote.Price,Is.GreaterThan((BigInteger)long.MaxValue));
            session.State.Players[0].Gold=quote.Price+7;Refresh();
            Assert.That(Field<Text>(ui,"goldText").text,Does.Contain("≈"));
            Assert.That(Find("Upgrade damage").GetComponent<ActionButtonVisual>().PriceText,Does.Contain("≈"));
            Submit(Field<Button>(ui,"upgradeDetailsButton"));yield return null;
            string exact=Copy(Field<RectTransform>(ui,"modal"));
            Assert.That(exact,Does.Contain("Stufe "+GameNumbers.Exact(huge)));
            Assert.That(exact,Does.Contain("Gold: "+GameNumbers.Exact(quote.Price+7)));
            Assert.That(exact,Does.Contain(GameNumbers.Exact(quote.Price)+" Gold"));
            Assert.That(exact,Does.Contain(GameNumbers.Exact(quote.Current.Damage)+" → "+GameNumbers.Exact(quote.Next.Damage)));
            Call(ui,"CloseModal");Refresh();Submit(Find("Upgrade damage"));Assert.That(session.LastResult.Accepted,Is.True);Refresh();
            Assert.That(tower.Level,Is.EqualTo(huge+1));Assert.That(session.State.Players[0].Gold,Is.EqualTo((BigInteger)7));
            Assert.That(tower.Invested,Is.EqualTo(TowerUpgradeRules.TotalInvestment(session.Content,0,huge+1)));
            Assert.That(Find("Upgrade damage").interactable,Is.False);
        }
        [UnityTest] public IEnumerator ResearchSendsExactBigIntegerQuoteAndRejectsAStaleLevelWithoutCharge()
        {
            ObserveCommands();var player=session.State.Players[0];var huge=(BigInteger)long.MaxValue+31;player.ResearchLevels[0]=huge;
            Submit(Find("Attack info 0"));yield return null;Refresh();
            var quote=UnitResearchRules.Quote(session.Content,player,0);var gold=player.Gold;
            Submit(Find("Research troop"));Assert.That(session.LastResult.Accepted,Is.True);
            Assert.That(commands.Last().Target,Is.EqualTo(-1));Assert.That(commands.Last().ExpectedResearch,Is.EqualTo(huge));
            Assert.That(player.ResearchLevels[0],Is.EqualTo(huge+1));Assert.That(player.Gold,Is.EqualTo(gold-quote.Price));Refresh();
            // A newer authoritative state arrives while the displayed quote still names the previous level.
            var displayed=Field<ResearchQuote>(ui,"displayedResearchQuote");player.ResearchLevels[0]++;gold=player.Gold;
            int commandCount=commands.Count;ulong previousSequence=session.LastResult.Sequence;
            Submit(Find("Research troop"));
            Assert.That(commands.Count,Is.EqualTo(commandCount),"Stale UI quote must not submit or silently buy a fresh level");
            Assert.That(session.LastResult.Sequence,Is.EqualTo(previousSequence));
            Assert.That(player.Gold,Is.EqualTo(gold));Assert.That(player.ResearchLevels[0],Is.EqualTo(huge+2));
            session.Submit(CommandType.ResearchUnit,0,-1,expectedResearch:displayed.Level);
            Assert.That(session.LastResult.Code,Is.EqualTo(ErrorCode.QuoteChanged));
            Assert.That(commands.Count,Is.EqualTo(commandCount+1));
            Assert.That(commands.Last().ExpectedResearch,Is.EqualTo(displayed.Level));Assert.That(player.Gold,Is.EqualTo(gold));
            Assert.That(player.ResearchLevels[0],Is.EqualTo(huge+2));
            Refresh();quote=UnitResearchRules.Quote(session.Content,player,0);Submit(Find("Research troop"));
            Assert.That(session.LastResult.Accepted,Is.True);Assert.That(commands.Last().ExpectedResearch,Is.EqualTo(huge+2));
            Assert.That(commands.Last().Target,Is.EqualTo(-1));Assert.That(player.Gold,Is.EqualTo(gold-quote.Price));
            Assert.That(player.ResearchLevels[0],Is.EqualTo(huge+3));
        }
        [UnityTest] public IEnumerator FinalClearingDisablesResearchWithoutSendingButEarlierClearingAllowsIt()
        {
            ObserveCommands();var player=session.State.Players[0];
            Assert.That(session.Content.WaveCount,Is.GreaterThan(1));
            session.State.WaveId=session.Content.WaveCount-1;session.State.WavePhase=WavePhase.Clearing;Refresh();
            Submit(Find("Attack info 0"));yield return null;Refresh();
            var button=Find("Research troop");var quote=UnitResearchRules.Quote(session.Content,player,0);var gold=player.Gold;
            Assert.That(button.interactable,Is.True,"Research can still affect purchases for the next wave");
            Submit(button);Assert.That(session.LastResult.Accepted,Is.True);Refresh();
            Assert.That(player.ResearchLevels[0],Is.EqualTo(quote.NextLevel));Assert.That(player.Gold,Is.EqualTo(gold-quote.Price));
            Assert.That(commands.Last().Type,Is.EqualTo(CommandType.ResearchUnit));
            int sent=commands.Count;var level=player.ResearchLevels[0];gold=player.Gold;var sequence=session.LastResult.Sequence;
            // Transition after display: the callback must guard the new state before the next HUD refresh.
            session.State.WaveId=session.Content.WaveCount;button.onClick.Invoke();
            Assert.That(commands.Count,Is.EqualTo(sent));Assert.That(player.Gold,Is.EqualTo(gold));
            Assert.That(player.ResearchLevels[0],Is.EqualTo(level));Refresh();
            Assert.That(button.interactable,Is.False);Assert.That(Copy(button),Does.Contain("Keine weiteren Truppenkäufe"));
            EventSystem.current.SetSelectedGameObject(button.gameObject);
            ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            button.onClick.Invoke(); // Even a direct late callback cannot bypass the disabled button.
            yield return null;Refresh();
            Assert.That(commands.Count,Is.EqualTo(sent));Assert.That(session.LastResult.Sequence,Is.EqualTo(sequence));
            Assert.That(player.Gold,Is.EqualTo(gold));Assert.That(player.ResearchLevels[0],Is.EqualTo(level));
        }
        [UnityTest] public IEnumerator MoreThanTwoHundredPaidOrdersReachTheLastRowWithABoundedVisiblePool()
        {
            const int count=235;
            for(int i=0;i<count;i++)
            {
                if(i>0&&i%20==0)session.State.Tick+=session.Content.TickRate;
                var q=SendRules.Quote(session.Content,session.State,session.LocalSlot,i%4);
                session.Submit(CommandType.SendUnit,i%4,-1,0,0,q.WaveId,q.ResearchLevel,q.EnemyTarget);Assert.That(session.LastResult.Accepted,Is.True);
            }
            Refresh();Submit(Find("Own paid attack queue"));yield return null;Refresh();
            var scroll=Field<ScrollRect>(ui,"paidQueueScroll");var rows=Field<List<Text>>(ui,"virtualQueueRows");
            Assert.That(session.State.SpawnOrders.Count,Is.EqualTo(count));Assert.That(Field<bool>(ui,"virtualQueue"),Is.True);
            int bound=Mathf.CeilToInt(scroll.viewport.rect.height/26f)+2;
            Assert.That(rows.Count,Is.LessThanOrEqualTo(bound));Assert.That(rows.Count,Is.LessThan(count));
            Assert.That(rows.Any(r=>r.gameObject.activeInHierarchy&&r.text.StartsWith("1. W")),Is.True);
            Assert.That(Field<Text>(ui,"paidQueueDetailText").text,Does.Not.Contain(count+". W"),"Large queues must not create one enormous Text mesh");
            scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return null;Refresh();
            Assert.That(rows.Count,Is.LessThanOrEqualTo(bound));
            var final=rows.Single(r=>r.gameObject.activeInHierarchy&&r.text.StartsWith(count+". W"));
            var last=session.State.SpawnOrders[count-1];Assert.That(final.text,Does.Contain(session.Content.Units[last.Kind].Name));
            var corners=new Vector3[4];final.rectTransform.GetWorldCorners(corners);
            float bottom=scroll.viewport.InverseTransformPoint(corners[0]).y,top=scroll.viewport.InverseTransformPoint(corners[1]).y;
            Assert.That(bottom,Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin-2));
            Assert.That(top,Is.LessThanOrEqualTo(scroll.viewport.rect.yMax+2));
            Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(0).Within(.01f));
        }
    }
}
