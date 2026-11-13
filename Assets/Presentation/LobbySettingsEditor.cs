using System;
using System.Globalization;
using System.Numerics;
using DefenseDuell.Core;

namespace DefenseDuell.Presentation
{
    public sealed class LobbyMatchOptions
    {
        public int WaveCount { get; }
        public string StartGoldDecimal { get; }
        public bool Endless { get; }
        public int IncomeMultiplier { get; }
        public MatchFormat Format { get; }
        public string MapId { get; }
        public LobbyMatchOptions(int waves,string gold,bool endless=false,int incomeMultiplier=1,MatchFormat format=MatchFormat.Duel,string mapId=null)
        {WaveCount=waves;StartGoldDecimal=gold;Endless=endless;IncomeMultiplier=incomeMultiplier;Format=format;MapId=mapId;}
        public bool SameAs(LobbyMatchOptions other)=>other!=null&&WaveCount==other.WaveCount&&StartGoldDecimal==other.StartGoldDecimal
            &&Endless==other.Endless&&IncomeMultiplier==other.IncomeMultiplier&&Format==other.Format&&MapId==other.MapId;
    }

    /// <summary>Technical limits supplied by the validated core/transport adapter, never a display clamp.</summary>
    public sealed class LobbyCapabilities
    {
        public int MinimumWaves { get; }
        public int MaximumWaves { get; }
        public int MaximumGoldDigits { get; }
        public int MaximumGoldMagnitudeBytes { get; }
        public string MaximumGoldDecimal { get; }
        public LobbyCapabilities(int minimumWaves,int maximumWaves,int maximumGoldDigits,string maximumGoldDecimal=null,int maximumGoldMagnitudeBytes=0)
        {
            if(minimumWaves<1||maximumWaves<minimumWaves||maximumGoldDigits<1||maximumGoldMagnitudeBytes<0)
                throw new ArgumentException("Invalid negotiated UI capabilities.");
            MinimumWaves=minimumWaves; MaximumWaves=maximumWaves; MaximumGoldDigits=maximumGoldDigits;
            MaximumGoldMagnitudeBytes=maximumGoldMagnitudeBytes;
            if(maximumGoldDecimal!=null)
            {
                if(!TryGold(maximumGoldDecimal,out var canonical))throw new ArgumentException("Invalid gold bound.");
                MaximumGoldDecimal=canonical;
            }
        }
        static int CompareDecimal(string left,string right)=>left.Length!=right.Length?left.Length.CompareTo(right.Length):string.CompareOrdinal(left,right);
        public bool TryGold(string text,out string canonical)
        {
            canonical=null;
            if(string.IsNullOrEmpty(text)||text.Length>MaximumGoldDigits)return false;
            foreach(char c in text)if(c<'0'||c>'9')return false;
            int first=0;while(first<text.Length-1&&text[first]=='0')first++;
            string value=first==0?text:text.Substring(first);
            if(MaximumGoldMagnitudeBytes>0)
            {
                // Most inputs are far below the transport boundary. Avoid BigInteger parse/format
                // on every keystroke (and every HUD refresh) for thousands of decimal digits.
                double boundaryDigits=MaximumGoldMagnitudeBytes*8d*Math.Log10(2d);
                long certainDigits=(long)Math.Floor(boundaryDigits);
                if(value.Length>certainDigits+1)return false;
                if(value.Length>certainDigits)
                {
                    var amount=BigInteger.Parse(value,CultureInfo.InvariantCulture);
                    byte[] bytes=amount.ToByteArray();
                    int magnitude=bytes.Length>1&&bytes[bytes.Length-1]==0?bytes.Length-1:bytes.Length;
                    if(magnitude>MaximumGoldMagnitudeBytes)return false;
                }
            }
            canonical=value;return true;
        }
        public string Validate(int waves,string gold,out LobbyMatchOptions options)
        {
            options=null;
            if(waves<MinimumWaves||waves>MaximumWaves)return "Wähle zwischen "+MinimumWaves+" und "+MaximumWaves+" Wellen.";
            if(string.IsNullOrEmpty(gold))return "Bitte Startgold eingeben.";
            foreach(char c in gold)if(c<'0'||c>'9')return "Gold als ganze Zahl ohne Punkt oder Komma eingeben.";
            if(!TryGold(gold,out string canonical))return "Diese Goldzahl ist für diese Partie zu groß.";
            if(MaximumGoldDecimal!=null&&CompareDecimal(canonical,MaximumGoldDecimal)>0)
                return "Diese Goldzahl überschreitet die technische Grenze dieser Partie.";
            options=new LobbyMatchOptions(waves,canonical);return "";
        }
        public bool SameAs(LobbyCapabilities other)=>other!=null&&MinimumWaves==other.MinimumWaves&&MaximumWaves==other.MaximumWaves
            &&MaximumGoldDigits==other.MaximumGoldDigits&&MaximumGoldMagnitudeBytes==other.MaximumGoldMagnitudeBytes&&MaximumGoldDecimal==other.MaximumGoldDecimal;
    }

    public sealed class LobbySettingsView
    {
        public string RoomEpoch { get; }
        public LobbyMatchOptions Options { get; }
        public ulong ConfigRevision { get; }
        public ulong RosterRevision { get; }
        public bool CanEdit { get; }
        public bool CanReady { get; }
        public bool IsReady { get; }
        public ulong StateSequence { get; }
        public MatchPhase Phase { get; }
        public LobbyCapabilities Capabilities { get; }
        public LobbySettingsView(string roomEpoch,LobbyMatchOptions options,ulong configRevision,ulong rosterRevision,
            bool canEdit,bool canReady,LobbyCapabilities capabilities,ulong stateSequence,MatchPhase phase,bool isReady=false)
        {
            RoomEpoch=roomEpoch; Options=options; ConfigRevision=configRevision; RosterRevision=rosterRevision;
            CanEdit=canEdit; CanReady=canReady; Capabilities=capabilities; StateSequence=stateSequence; Phase=phase; IsReady=isReady;
        }
    }
    public sealed class ApplyLobbyOptions
    {
        public Guid RequestId { get; }=Guid.NewGuid();
        public string RoomEpoch { get; }
        public ulong ExpectedConfigRevision { get; }
        public LobbyMatchOptions Options { get; }
        public ApplyLobbyOptions(string epoch,ulong revision,LobbyMatchOptions options)
        {RoomEpoch=epoch; ExpectedConfigRevision=revision; Options=options;}
    }
    public sealed class ReadyForLobby
    {
        public Guid RequestId { get; }=Guid.NewGuid();
        public string RoomEpoch { get; }
        public ulong ExpectedConfigRevision { get; }
        public ulong ExpectedRosterRevision { get; }
        public ReadyForLobby(LobbySettingsView view)
        {RoomEpoch=view.RoomEpoch; ExpectedConfigRevision=view.ConfigRevision; ExpectedRosterRevision=view.RosterRevision;}
    }

    /// <summary>Presentation state only. ACKs resolve requests; only Receive changes confirmed options.</summary>
    public sealed class LobbySettingsEditor
    {
        string activeEpoch;
        string error="Raum wird geladen.";
        LobbyMatchOptions draftOptions;
        ulong? awaitingPublication;
        public LobbySettingsView Confirmed { get; private set; }
        public int DraftWaves { get; private set; }
        public string DraftGold { get; private set; }="";
        public bool DraftEndless { get; private set; }
        public int DraftIncomeMultiplier { get; private set; }=1;
        public MatchFormat DraftFormat { get; private set; }
        public string DraftMapId { get; private set; }
        public bool EditRules(bool endless,int incomeMultiplier,MatchFormat format,string mapId)
        {
            if(!CanEdit)return false;
            DraftEndless=endless;DraftIncomeMultiplier=incomeMultiplier;DraftFormat=format;DraftMapId=mapId;Notice="";ValidateDraft();return true;
        }
        public string Notice { get; private set; }="";
        public ApplyLobbyOptions Pending { get; private set; }
        public ReadyForLobby PendingReady { get; private set; }
        public bool HasPending=>Pending!=null||PendingReady!=null;
        public bool CanEdit=>Confirmed!=null&&Confirmed.Phase==MatchPhase.Lobby&&Confirmed.CanEdit&&!HasPending;
        public string Error=>error;
        public bool IsDirty=>Confirmed!=null&&(error.Length!=0||!draftOptions.SameAs(Confirmed.Options));
        public bool CanApply=>CanEdit&&IsDirty&&error.Length==0;
        public bool CanReady=>Confirmed!=null&&Confirmed.Phase==MatchPhase.Lobby&&Confirmed.CanReady&&!Confirmed.IsReady&&!IsDirty&&!HasPending;

        public void Reset()
        {
            activeEpoch=null; Confirmed=null; DraftWaves=0; DraftGold=""; Notice="";
            error="Raum wird geladen."; draftOptions=null; ClearPending();
        }
        /// <summary>Call only after the session adapter validates a new room/rematch epoch, never from arbitrary snapshots.</summary>
        public void BeginSession(string epoch)
        {
            if(string.IsNullOrEmpty(epoch))throw new ArgumentException("A validated session epoch is required.",nameof(epoch));
            if(activeEpoch==epoch)return; // A layout rebuild must not remove monotonic state protection.
            Reset();activeEpoch=epoch;
        }
        static bool CanAdvance(MatchPhase previous,MatchPhase next)
        {
            if(previous==next)return true;
            if(previous==MatchPhase.Finished||previous==MatchPhase.Aborted)return false;
            if(next==MatchPhase.Aborted)return true;
            return (int)next>(int)previous;
        }
        public bool Receive(LobbySettingsView view)
        {
            if(view==null)throw new ArgumentNullException(nameof(view));
            if(activeEpoch==null||view.RoomEpoch!=activeEpoch)return false;
            if(view.Options==null||view.Capabilities==null||view.Phase<MatchPhase.Lobby||view.Phase>MatchPhase.Aborted)
                throw new ArgumentException("Incomplete authoritative view.");
            var old=Confirmed;
            if(old!=null)
            {
                if(view.StateSequence<=old.StateSequence||view.ConfigRevision<old.ConfigRevision||view.RosterRevision<old.RosterRevision)return false;
                if(!CanAdvance(old.Phase,view.Phase))return false;
                if(view.ConfigRevision==old.ConfigRevision&&!view.Options.SameAs(old.Options))
                    throw new ArgumentException("Options changed without a config revision.");
                if(old.Phase!=MatchPhase.Lobby&&(view.ConfigRevision!=old.ConfigRevision||!view.Options.SameAs(old.Options)))return false;
            }
            bool sameOptions=old!=null&&view.Options.SameAs(old.Options)&&view.Capabilities.SameAs(old.Capabilities);
            if(!sameOptions&&(view.Capabilities.Validate(view.Options.WaveCount,view.Options.StartGoldDecimal,out var normalized).Length!=0||(normalized.WaveCount!=view.Options.WaveCount||normalized.StartGoldDecimal!=view.Options.StartGoldDecimal||!ExtraRulesValid(view.Options.IncomeMultiplier,view.Options.Format))))
                throw new ArgumentException("Invalid authoritative options.");
            bool changed=old!=null&&view.ConfigRevision!=old.ConfigRevision;
            bool rosterChanged=old!=null&&view.RosterRevision!=old.RosterRevision;
            bool wasDirty=IsDirty;
            Confirmed=view;
            if(old==null||changed||view.Phase!=MatchPhase.Lobby||!view.CanEdit)
            {
                ResetDraft();
                Notice=changed?(wasDirty?"Neue Raumeinstellungen übernommen. Bitte prüfen und erneut bereit melden.":"Einstellungen geändert. Bitte erneut bereit melden."):"";
            }
            else if(!view.Capabilities.SameAs(old.Capabilities))ValidateDraft();
            // A newer snapshot can supersede an operation even if its ACK arrives later.
            if(view.Phase!=MatchPhase.Lobby||(Pending!=null&&(changed||!view.CanEdit))
                ||(PendingReady!=null&&(changed||rosterChanged||view.IsReady||!view.CanReady)))ClearPending();
            if(awaitingPublication.HasValue&&view.StateSequence>=awaitingPublication.Value)ClearPending();
            return true;
        }
        public bool Edit(int waves,string gold)
        {
            if(!CanEdit)return false;
            DraftWaves=waves; DraftGold=gold??""; Notice=""; ValidateDraft();return true;
        }
        public bool StepWaves(int delta)
        {
            if(!CanEdit)return false;
            long next=(long)DraftWaves+delta;
            if(next<Confirmed.Capabilities.MinimumWaves||next>Confirmed.Capabilities.MaximumWaves)return false;
            return Edit((int)next,DraftGold);
        }
        public void Discard(){if(CanEdit){ResetDraft(); Notice="";}}
        static bool ExtraRulesValid(int income,MatchFormat format)=>(income==1||income==2||income==3||income==5||income==10)&&(int)format>=0&&(int)format<=3;
        void ValidateDraft()
        {
            error=Confirmed.Capabilities.Validate(DraftWaves,DraftGold,out var basic);
            if(error.Length==0&&!ExtraRulesValid(DraftIncomeMultiplier,DraftFormat))error="Ungültige Spielregeln.";
            draftOptions=error.Length==0?new LobbyMatchOptions(basic.WaveCount,basic.StartGoldDecimal,DraftEndless,DraftIncomeMultiplier,DraftFormat,DraftMapId):null;
        }
        void ResetDraft(){DraftWaves=Confirmed.Options.WaveCount; DraftGold=Confirmed.Options.StartGoldDecimal;DraftEndless=Confirmed.Options.Endless;DraftIncomeMultiplier=Confirmed.Options.IncomeMultiplier;DraftFormat=Confirmed.Options.Format;DraftMapId=Confirmed.Options.MapId; draftOptions=Confirmed.Options;error="";}
        void ClearPending(){Pending=null; PendingReady=null; awaitingPublication=null;}
        public ApplyLobbyOptions Apply()
        {
            if(!CanApply)return null;
            Pending=new ApplyLobbyOptions(activeEpoch,Confirmed.ConfigRevision,draftOptions); Notice="Einstellungen werden bestätigt …";return Pending;
        }
        public ReadyForLobby Ready()
        {
            if(!CanReady)return null;
            PendingReady=new ReadyForLobby(Confirmed);Notice="Bereitschaft wird bestätigt …";return PendingReady;
        }
        /// <summary>An ACK's state may be stale; resolve it separately without applying that state.
        /// Accepted ACK before its snapshot keeps the shared pending lock until that publication arrives.</summary>
        public bool Resolve(string epoch,Guid requestId,bool accepted,ulong publicationSequence,string message=null)
        {
            Guid? current=Pending!=null?(Guid?)Pending.RequestId:PendingReady?.RequestId;
            if(epoch!=activeEpoch||!current.HasValue||current.Value!=requestId)return false;
            if(accepted&&Confirmed!=null&&publicationSequence>Confirmed.StateSequence)
            {awaitingPublication=publicationSequence; Notice="Bestätigte Raumeinstellungen werden geladen …";return true;}
            ClearPending();Notice=message??(accepted?"Bestätigt.":"Aktion nicht übernommen. Bitte erneut versuchen.");return true;
        }
        public bool Reject(string epoch,Guid requestId,string message)=>Pending!=null&&Resolve(epoch,requestId,false,0,message);
        public bool RejectReady(string epoch,Guid requestId,string message)=>PendingReady!=null&&Resolve(epoch,requestId,false,0,message);
    }
}
