using BigInteger = System.Numerics.BigInteger;
using DefenseDuell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        readonly AttackIncomeFeedback attackIncomeFeedback=new AttackIncomeFeedback();
        RectTransform incomeFeedbackPanel;
        Text incomeFeedbackText;
        CanvasGroup incomeFeedbackVisual;

        void CreateIncomeHud(Transform resources)
        {
            incomeText=UiKit.TextAt(resources,"",118,32,158,20,13,HudMuted,TextAnchor.MiddleCenter);
            var panel=BattlePanel(screen,"Attack income increase",164,74,132,44,HudPanel);
            panel.raycastTarget=false;incomeFeedbackPanel=panel.rectTransform;
            incomeFeedbackVisual=panel.gameObject.AddComponent<CanvasGroup>();incomeFeedbackVisual.blocksRaycasts=false;incomeFeedbackVisual.interactable=false;
            incomeFeedbackText=UiKit.TextAt(panel.transform,"",5,1,122,23,16,HudGold,TextAnchor.MiddleCenter);
            UiKit.TextAt(panel.transform,"Angriff · Spielzeit",5,25,122,15,11,HudMuted,TextAnchor.MiddleCenter);
            incomeFeedbackPanel.gameObject.SetActive(false);
        }
        void ResetIncomeFeedback()
        {
            // Bootstrap may attach the UI after a session has already published a state.
            // The render cache is assigned later by CacheContent; use its authoritative pair.
            var source=session;var state=source?.State;var currentContent=source?.Content;
            int slot=source!=null?(int)source.LocalSlot:0;
            if(currentContent!=null&&state?.Players!=null&&slot>=0&&slot<state.Players.Length&&state.Players[slot]!=null)
                attackIncomeFeedback.Reset(state.SessionId,slot,EconomyRules.EffectiveAttackIncomeUnits(currentContent,state.Players[slot]),state.PlayTicks);
            else attackIncomeFeedback.Clear();
            if(incomeFeedbackPanel!=null)incomeFeedbackPanel.gameObject.SetActive(false);
        }
        void RefreshIncomeHud()
        {
            var state=session.State;int slot=(int)session.LocalSlot;
            bool running=state.Phase==MatchPhase.Playing&&state.WavePhase!=WavePhase.Preparing;
            BigInteger rate=running?EconomyRules.IncomeUnits(content,state,session.LocalSlot):0;
            incomeText.text="+"+IncomeText(rate)+" G/s · "+(running?"Spielzeit":"Baupause");
            bool newAggregate=attackIncomeFeedback.Observe(state.SessionId,slot,EconomyRules.EffectiveAttackIncomeUnits(content,state.Players[slot]),state.PlayTicks,state.Phase==MatchPhase.Playing,Time.unscaledTimeAsDouble);
            if(newAggregate&&gameAudio!=null)gameAudio.PlayIncomeIncrease();
            RefreshIncomeFeedbackVisual();
        }
        void RefreshIncomeFeedbackVisual()
        {
            if(incomeFeedbackPanel==null)return;
            bool visible=page=="game"&&attackIncomeFeedback.IsVisible(Time.unscaledTimeAsDouble);
            incomeFeedbackPanel.gameObject.SetActive(visible);
            if(!visible)return;
            incomeFeedbackText.text="+"+IncomeText(attackIncomeFeedback.VisibleIncreaseUnits)+" G/s";
            // A static, bounded notice also respects reduced-effects mode; no screen-wide particles.
            incomeFeedbackVisual.alpha=1;
        }
    }
}
