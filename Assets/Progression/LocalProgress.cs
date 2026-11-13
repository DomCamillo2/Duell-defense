using System;
using System.IO;
using DefenseDuell.Core;
using DefenseDuell.Networking;
using UnityEngine;

namespace DefenseDuell.Progression
{
    /// <summary>Binds a persisted ticket to a live non-terminal session before it can earn a reward.</summary>
    public sealed class LocalProgress : MonoBehaviour
    {
        public PlayerProfile Profile => profile.Copy();
        public string Message { get; private set; } = "";
        public int LastReward { get; private set; }
        public event Action Changed;
        PlayerProfile profile=new PlayerProfile();
        ProfileStore store;
        SessionCoordinator session;
        ulong boundSession;
        string ticket="";
        long counter;
        bool available, bound;
        PlayerProfile pendingSave;
        double retryAt;
        int pendingReward;
        PlayerProfile WorkingProfile => pendingSave ?? profile;
        // Lobby loadouts are a gameplay choice for every class. Legacy collection
        // records remain intact and no longer restrict the available room classes.
        int ValidSelectedTowerMask => session==null?TowerLoadoutRules.DefaultMask:TowerLoadoutRules.Normalize(session.LocalTowerMask,session.Content.Towers.Length);

        public void Initialize(SessionCoordinator coordinator, string storagePath=null)
        {
            if(pendingSave!=null && !RetrySave())return;
            if(session!=null)session.Changed-=Observe;
            session=coordinator;
            store=new ProfileStore(storagePath ?? Path.Combine(Application.persistentDataPath,"victory-profile-v1.save"),
                p=>JsonUtility.ToJson(p),s=>JsonUtility.FromJson<PlayerProfile>(s));
            try
            {
                profile=store.Load(out bool recovered); available=true;
                if(recovered)Message="Profil aus Sicherung wiederhergestellt.";
            }
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { available=false; Message="Profil konnte nicht geladen werden. Dein Spielstand bleibt erhalten."; Debug.LogWarning(e.Message); }
            session.SetTowerAvailability(ValidSelectedTowerMask);
            session.Changed+=Observe;
        }
        public bool Unlock(int kind)
        {
            if(!available || session.Status!=ConnectionState.Idle)return false;
            if(pendingSave!=null && !RetrySave())return false;
            var next=ProfileRules.Buy(profile,kind,out var result);
            if(result!=ProfileChange.Applied)
            { Message=result==ProfileChange.InsufficientMedals?"Noch nicht genug Siegesmarken.":result==ProfileChange.AlreadyOwned?"Bereits freigeschaltet.":"Turm nicht verfügbar.";Changed?.Invoke();return false; }
            if(!Persist(next))return false;
            session.SetTowerAvailability(ValidSelectedTowerMask);Message="Turm dauerhaft freigeschaltet.";Changed?.Invoke();return true;
        }
        void Observe()
        {
            if(!available)return;
            var state=session.State;
            if(session.Status==ConnectionState.Idle || state==null)
            {
                bound=false; boundSession=0;ticket="";counter=0;
                if(session!=null && session.Status==ConnectionState.Idle && session.LocalTowerMask!=ValidSelectedTowerMask)
                    session.SetTowerAvailability(ValidSelectedTowerMask);
                return;
            }
            if(!bound || boundSession!=state.SessionId)
            {
                // Never turn an unsolicited final snapshot into a fresh reward-bearing match.
                if(state.Phase==MatchPhase.Finished || state.Phase==MatchPhase.Aborted)return;
                // Keep the observed live-session binding in memory even if the first disk write fails.
                // A later terminal result may settle this ticket; an unobserved final snapshot never can.
                var next=ProfileRules.Begin(WorkingProfile,Guid.NewGuid());
                bound=true;boundSession=state.SessionId;ticket=next.ActiveTicket;counter=next.ActiveCounter;LastReward=0;pendingReward=0;
                Persist(next);
            }
            if(state.Phase!=MatchPhase.Finished && state.Phase!=MatchPhase.Aborted)return;
            int local=(int)session.LocalSlot;
            bool win=local>=0 && local<state.Players.Length && state.WinningTeamId>=0
                && state.Players[local].TeamId==state.WinningTeamId;
            var outcome=state.Phase==MatchPhase.Aborted?ProfileOutcome.Aborted:win?ProfileOutcome.Win:
                state.Outcome==MatchOutcome.Draw?ProfileOutcome.Draw:ProfileOutcome.Loss;
            var settled=ProfileRules.Settle(WorkingProfile,ticket,counter,outcome,true,out var change);
            if(change!=ProfileChange.Applied)return;
            pendingReward=win?ProfileRules.WinMedals:0;
            if(!Persist(settled))return;
            Changed?.Invoke();
        }
        bool Persist(PlayerProfile next)
        {
            // Consolidate immutable transitions, retaining earned currency even if the player
            // leaves or starts a rematch while storage is temporarily unavailable.
            pendingSave=next.Copy();
            return RetrySave();
        }
        bool RetrySave()
        {
            if(pendingSave==null)return true;
            retryAt=Time.realtimeSinceStartupAsDouble+1;
            try
            {
                store.Save(pendingSave);profile=pendingSave;pendingSave=null;
                LastReward=pendingReward;pendingReward=0;
                Message=LastReward>0?"+100 Siegesmarken für deinen Sieg!":"";
                if(session!=null && session.Status==ConnectionState.Idle && session.LocalTowerMask!=ValidSelectedTowerMask)
                    session.SetTowerAvailability(ValidSelectedTowerMask);
                return true;
            }
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException)
            {
                Message="Speichern ausstehend · wird erneut versucht. Bitte Speicherplatz prüfen.";
                Debug.LogWarning(e.Message);Changed?.Invoke();return false;
            }
        }
        void Update()
        {
            if(pendingSave!=null && Time.realtimeSinceStartupAsDouble>=retryAt && RetrySave())Changed?.Invoke();
        }
        void OnDestroy(){if(pendingSave!=null)RetrySave();if(session!=null)session.Changed-=Observe;}
    }
}
