using DefenseDuell.Core;
using UnityEngine;

namespace DefenseDuell.Presentation
{
    // Read-only display estimates. Command quotes and purchasing remain authoritative and unthrottled.
    public sealed partial class GameUI
    {
        SendQueueEstimate queueForecast;
        SendQueueEstimate[] displayedSendForecasts;
        const float QueueHeaderHeight=120;
        double nextAdditionalForecastAt;
        ulong additionalForecastSession;
        System.Numerics.BigInteger additionalForecastWave;
        int additionalForecastPlayTick,additionalForecastTarget=-1;
        WavePhase additionalForecastPhase;

        void RefreshAdditionalForecasts()
        {
            // Pure ETA work is bounded to 4 Hz. Authoritative command quotes are refreshed separately.
            if(Time.unscaledTimeAsDouble<nextAdditionalForecastAt)return;
            var state=session.State;nextAdditionalForecastAt=Time.unscaledTimeAsDouble+.25;
            additionalForecastSession=state.SessionId;additionalForecastWave=state.WaveId;
            additionalForecastTarget=BattleEnemySlot;additionalForecastPhase=state.WavePhase;additionalForecastPlayTick=state.PlayTicks;
            for(int k=0;k<units.Length;k++)displayedSendForecasts[k]=SendQueueForecast.Estimate(content,state,session.LocalSlot,k);
        }
        string AdditionalForecastStartText(int kind,bool compact)
        {
            var state=session.State;var quote=displayedSendQuotes[kind];var forecast=displayedSendForecasts[kind];
            if(quote.WaveId<=0)return compact?"Kein Ausmarsch":"Kein weiterer Ausmarsch möglich";
            if(additionalForecastTarget!=BattleEnemySlot||additionalForecastSession!=state.SessionId||additionalForecastWave!=state.WaveId||additionalForecastPhase!=state.WavePhase
                ||!forecast.IncludesAdditionalPurchase||forecast.AdditionalWave!=quote.WaveId)return "Zeitpunkt offen";
            return ForecastStartText(forecast,true,compact);
        }

        string ForecastDelayText(System.Numerics.BigInteger ticks,bool compact)
        {
            // Round DOWN: an optimistic lower bound must not become a later guaranteed minimum.
            var seconds=System.Numerics.BigInteger.Max(0,ticks)/content.TickRate;
            return "≥"+GameNumbers.Compact(seconds)+(compact?" s Spielz.":" s Spielzeit");
        }
        string ForecastStartText(SendQueueEstimate forecast,bool additional,bool compact)
        {
            if(additional&&!forecast.IncludesAdditionalPurchase)return compact?"Kein Ausmarsch":"Kein weiterer Ausmarsch möglich";
            if(!additional&&forecast.Packets.IsZero)return "Keine Aufträge";
            if(forecast.HasUnknownWaveWait)return compact?"Zeitpunkt offen":"nach Wellenwechsel · Zeitpunkt offen";
            if(forecast.WaitsForPreparation)return "ab Wellenstart";
            var ticks=additional?forecast.AdditionalFirstSpawnDelayTicks:forecast.FirstSpawnDelayTicks;
            // A cached lower bound must advance with authoritative play ticks, not wall-clock speed.
            if(additional)ticks=System.Numerics.BigInteger.Max(0,ticks-Mathf.Max(0,session.State.PlayTicks-additionalForecastPlayTick));
            return ForecastDelayText(ticks,compact);
        }
        string ForecastCompletionText(SendQueueEstimate forecast)
        {
            if(forecast.HasUnknownWaveWait||forecast.WaitsForPreparation)return "Zeitpunkt offen";
            return ForecastDelayText(forecast.MinimumDelayTicks,false);
        }
        string QueueBoundGold(SendQueueEstimate forecast)=>GameNumbers.Ratio(forecast.RemainingPaidQuarterGold,4);
        string QueueForecastDetails(SendQueueEstimate forecast)
        {
            string waves=forecast.FirstWave==forecast.LastWave?"W"+forecast.FirstWave:"W"+forecast.FirstWave+"–W"+forecast.LastWave;
            return GameNumbers.Compact(forecast.Packets)+(forecast.Packets.IsOne?" Paket · ":" Pakete · ")+GameNumbers.Compact(forecast.RemainingUnits)+(forecast.RemainingUnits.IsOne?" Truppe · ":" Truppen · ")+waves+
                "\nNoch gebunden: "+QueueBoundGold(forecast)+" G"+
                "\nErster Ausmarsch: "+ForecastStartText(forecast,false,false)+
                "\nAlles ausgesandt: "+ForecastCompletionText(forecast)+
                "\nUntergrenze · Grundwelle kann verzögern";
        }
    }
}
