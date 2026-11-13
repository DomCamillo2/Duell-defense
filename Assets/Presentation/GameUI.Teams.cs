using DefenseDuell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        Button attackTargetButton;
        int BattleEnemySlot
        {
            get
            {
                var s=session.State;int local=(int)session.LocalSlot,target=(int)s.Players[local].AttackTarget;
                if(target>=0&&target<s.Players.Length&&s.Players[target].TeamId!=s.Players[local].TeamId)return target;
                for(int i=0;i<s.Players.Length;i++)if(s.Players[i].TeamId!=s.Players[local].TeamId)return i;
                return local;
            }
        }
        string WaveLimitLabel=>content.MatchRules.Endless?"∞":content.WaveCount.ToString();
        bool LivingVotes(bool speed)
        {for(int i=0;i<session.State.Players.Length;i++){if(i==(int)session.LocalSlot)continue;var player=session.State.Players[i];if(player.Hp>0&&!(speed?player.SpeedReady:player.WaveReady))return false;}return true;}
        int ReadyParticipantCount(bool speed)
        {int n=0;foreach(var p in session.State.Players)if(p.Hp>0&&(speed?p.SpeedReady:p.WaveReady))n++;return n;}
        int LivingParticipantCount(){int n=0;foreach(var p in session.State.Players)if(p.Hp>0)n++;return n;}
        void CreateAttackTargetControl()
        {attackTargetButton=BattleButton(screen,"",width-196,174,184,44,CycleAttackTarget,HudCard,13);attackTargetButton.name="Choose attack target";}
        void CycleAttackTarget()
        {
            if(page!="game"||modal!=null||InfoBlocking||UiPointerGate.Active||session.State==null||commands.HasPending(CommandType.SetAttackTarget))return;
            var state=session.State;int local=(int)session.LocalSlot,current=BattleEnemySlot;
            for(int step=1;step<state.Players.Length;step++)
            {
                int next=(current+step)%state.Players.Length;
                if(state.Players[next].TeamId==state.Players[local].TeamId||state.Players[next].Hp<=0)continue;
                CloseAttackInfo();CancelInteraction();commands.DiscardVisuals();CancelBoardFeedback();selectedSlot=selectedTower=-1;hasSelectedPoint=false;
                Send(CommandType.SetAttackTarget,next);nextAdditionalForecastAt=0;return;
            }
        }
        void RefreshTargetControl()
        {
            if(attackTargetButton==null)return;var s=session.State;int target=BattleEnemySlot;
            UiKit.ButtonText(attackTargetButton,commands.HasPending(CommandType.SetAttackTarget)?"Ziel wird bestätigt …":"ZIEL: SPIELER "+(target+1)+"  ⇄");
            attackTargetButton.interactable=s.Phase==MatchPhase.Playing&&s.Players[(int)session.LocalSlot].Hp>0&&!commands.HasPending(CommandType.SetAttackTarget);
        }
        string RematchStatus()
        {int ready=0;foreach(var p in session.State.Players)if(p.Rematch)ready++;return ready+" / "+session.State.Players.Length+" Spieler für die Revanche bereit.";}
    }
}
