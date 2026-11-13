using System;
using System.Numerics;

namespace DefenseDuell.Core
{
    /// <summary>Read-only optimistic delivery estimate, in active simulation ticks at 1x.
    /// Neutral traffic, travel and unknown wave/preparation waits are not included.
    /// RemainingPaidQuarterGold excludes the already spawned share of partial packets.</summary>
    public readonly struct SendQueueEstimate
    {
        // MinimumDelayTicks ends at the LAST body of the WHOLE queue; first-spawn fields are separate.
        public readonly BigInteger Packets, RemainingUnits, RemainingPaidQuarterGold, MinimumDelayTicks;
        public readonly BigInteger FirstSpawnDelayTicks, AdditionalFirstSpawnDelayTicks;
        public readonly BigInteger FirstWave, LastWave, AdditionalWave;
        public readonly bool HasUnknownWaveWait, WaitsForPreparation, IncludesAdditionalPurchase;
        public SendQueueEstimate(BigInteger packets,BigInteger units,BigInteger gold,BigInteger ticks,
            BigInteger firstWave,BigInteger lastWave,bool waveWait,bool preparation,bool additional,
            BigInteger firstSpawnDelay,BigInteger additionalFirstSpawnDelay,BigInteger additionalWave)
        { Packets=packets;RemainingUnits=units;RemainingPaidQuarterGold=gold;MinimumDelayTicks=ticks;
          FirstWave=firstWave;LastWave=lastWave;HasUnknownWaveWait=waveWait;
          WaitsForPreparation=preparation;IncludesAdditionalPurchase=additional;
          FirstSpawnDelayTicks=firstSpawnDelay;AdditionalFirstSpawnDelayTicks=additionalFirstSpawnDelay;AdditionalWave=additionalWave; }
    }

    public static class SendQueueForecast
    {
        /// <summary>Append at most one hypothetical packet for the before-buy preview.
        /// Insufficient money still permits a preview; invalid wave/kind does not append.
        /// This is a lower bound, never a promise of arrival or a mutating purchase.</summary>
        public static SendQueueEstimate Estimate(GameContent content,MatchState state,PlayerSlot sender,int additionalKind=-1)
        {
            if(content==null)throw new ArgumentNullException(nameof(content));
            if(state==null)throw new ArgumentNullException(nameof(state));
            if(!state.ValidPlayer(sender))throw new ArgumentOutOfRangeException(nameof(sender));
            if(additionalKind < -1 || additionalKind>=content.UnitData.Length)throw new ArgumentOutOfRangeException(nameof(additionalKind));
            if(state.Phase!=MatchPhase.Playing)return default;
            BigInteger packets=0,units=0,paid=0,last=state.PlayTicks;
            BigInteger next=BigInteger.Max((BigInteger)state.PlayTicks+1,state.NextPaidSpawnTicks[(int)sender]);
            var cooldown=new BigInteger[content.UnitData.Length];
            for(int i=0;i<cooldown.Length;i++)cooldown[i]=state.Players[(int)sender].NextSendTicks[i];
            BigInteger firstWave=0,lastWave=0,additionalWave=0;bool future=false,additional=false;
            BigInteger firstDelay=0,additionalDelay=0,lastPacketFirst=0;
            void Include(int kind,BigInteger wave,int initial,int remaining,BigInteger price,int nextTick)
            {
                if(packets.IsZero)firstWave=wave;
                packets++;units+=remaining;lastWave=wave;future|=wave>state.WaveId;
                paid+=4*price-4*price*(initial-remaining)/initial;
                BigInteger first=BigInteger.Max(next,nextTick);
                if(remaining==initial)
                {
                    first=BigInteger.Max(first,cooldown[kind]);
                    cooldown[kind]=first+content.UnitData[kind].CooldownTicks;
                }
                lastPacketFirst=first;
                if(packets==1)firstDelay=first-state.PlayTicks;
                // The remaining bodies of an already-started packet ignore the packet cooldown.
                last=first+(BigInteger)(remaining-1)*content.Rules.SpawnIntervalTicks;
                next=last+content.Rules.SpawnIntervalTicks;
            }
            foreach(var order in state.SpawnOrders)
                if(order.Sender==sender)Include(order.Kind,order.WaveId,order.InitialCount,order.Remaining,order.PaidPrice,order.NextTick);
            if(additionalKind>=0)
            {
                var quote=SendRules.Quote(content,state,sender,additionalKind);
                if(quote.Code==ErrorCode.None||quote.Code==ErrorCode.InsufficientGold)
                {
                    int count=content.UnitData[additionalKind].Count;
                    Include(additionalKind,quote.WaveId,count,count,quote.Price,state.PlayTicks);additional=true;
                    additionalDelay=lastPacketFirst-state.PlayTicks;additionalWave=quote.WaveId;
                }
            }
            return new SendQueueEstimate(packets,units,paid,BigInteger.Max(0,last-state.PlayTicks),firstWave,lastWave,
                future,packets>0&&state.WavePhase==WavePhase.Preparing,additional,firstDelay,additionalDelay,additionalWave);
        }
    }
}
