using DefenseDuell.Core;
using DefenseDuell.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        Button tempoButton;
        bool speedVotePending,speedVoteDesired,speedVoteWasReady,speedVoteAccepted;
        ulong speedVoteSequence,speedVoteEpoch;
        double speedVoteAt;
        void CreateTempoControl(float x)
        {
            tempoButton=UiKit.ActionButtonAt(screen,"Match tempo",x,10,188,62,ToggleSpeedVote,ActionSymbol.Speed);
            var art=tempoButton.GetComponent<ActionButtonVisual>().Artwork;
            UiKit.TextAt(art.transform,"Du",62,44,30,13,10,HudText);
            UiKit.TextAt(art.transform,"Alle",107,44,65,13,10,HudText);
        }
        void ResetSpeedVote()
        {speedVotePending=speedVoteDesired=speedVoteWasReady=speedVoteAccepted=false;speedVoteSequence=speedVoteEpoch=0;speedVoteAt=0;}
        void ToggleSpeedVote()
        {
            var state=session?.State;
            if(!isActiveAndEnabled||state==null||state.Phase!=MatchPhase.Playing||page!="game"||InfoBlocking||modal!=null||UiPointerGate.Active||dragSource!=null)return;
            if(speedVotePending&&!speedVoteDesired)return;
            // An outstanding approval can always be withdrawn with an explicit Kind=0 command.
            bool desired=speedVotePending?false:!state.Players[(int)session.LocalSlot].SpeedReady;
            speedVotePending=true;speedVoteDesired=desired;speedVoteWasReady=state.Players[(int)session.LocalSlot].SpeedReady;
            speedVoteAccepted=false;speedVoteEpoch=state.SessionId;speedVoteSequence=0;speedVoteAt=Time.unscaledTimeAsDouble;
            ulong sequence=session.Submit(CommandType.SetSpeedVote,desired?1:0,-1,0,0);
            speedVoteSequence=sequence;
            if(sequence==0){ResetSpeedVote();RejectPurchase(session.Message,tempoButton);}
            else if(session.IsHost&&session.LastResult.Sequence==sequence)ResolveSpeedVote(session.LastResult);
            dirty=true;RefreshTempoControl();
        }
        void ResolveSpeedVote(CommandResult result)
        {
            if(!speedVotePending||result.Sequence!=speedVoteSequence||session?.State==null||session.State.SessionId!=speedVoteEpoch)return;
            if(result.Accepted)speedVoteAccepted=true;
            else{ResetSpeedVote();RejectPurchase(ErrorText(result.Code),tempoButton);}
            dirty=true;
        }
        void RefreshTempoControl()
        {
            if(tempoButton==null||session.State==null)return;
            var state=session.State;int local=(int)session.LocalSlot;bool own=state.Players[local].SpeedReady,other=LivingVotes(true);
            if(speedVotePending)
            {
                if(state.SessionId!=speedVoteEpoch||state.Phase!=MatchPhase.Playing)ResetSpeedVote();
                else if(own==speedVoteDesired&&(speedVoteAccepted||own!=speedVoteWasReady))ResetSpeedVote();
                else if(Time.unscaledTimeAsDouble-speedVoteAt>4){ResetSpeedVote();RejectPurchase("Tempo nicht bestätigt. Bitte erneut versuchen.",tempoButton);}
            }
            string detail;
            if(state.Phase!=MatchPhase.Playing)detail="Ab Spielbeginn";
            else if(speedVotePending)detail=speedVoteDesired?"Anfrage · Abbrechen":"1× wird angefragt …";
            else if(state.WavePhase==WavePhase.Preparing&&own&&other)detail="2× bereit · Zurücknehmen";
            else if(state.SimulationSpeed==2)detail="Zurück zu 1×";
            else if(own)detail="Warten · Zurücknehmen";
            else if(other)detail="2×? Jetzt zustimmen";
            else detail=session.Mode==SessionMode.Offline?"Auf 2× beschleunigen":"2× gemeinsam anfragen";
            var visual=tempoButton.GetComponent<ActionButtonVisual>();
            string title=state.Phase==MatchPhase.Playing&&state.WavePhase==WavePhase.Preparing?"1× BAUPAUSE":state.SimulationSpeed+"× TEMPO";
            visual.SetDetails(ActionSymbol.Speed,title,detail,"","");
            visual.Artwork.SetConsents(own,other);
            visual.SetState(state.Phase!=MatchPhase.Playing||state.Players[local].Hp<=0?PurchaseState.Inactive:speedVotePending&&!speedVoteDesired?PurchaseState.Pending:PurchaseState.Available);
        }
    }
}
