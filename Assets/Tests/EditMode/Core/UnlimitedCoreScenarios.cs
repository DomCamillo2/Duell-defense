using System;
using System.Collections.Generic;
using System.Numerics;
using DefenseDuell.Core;

namespace DefenseDuell.Tests
{
    public static class UnlimitedCoreScenarios
    {
        static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static void Equal(BigInteger actual,BigInteger expected,string message)=>Check(actual==expected,message+" actual="+actual+" expected="+expected);
        static KeyValuePair<string,Action> Case(string name,Action action)=>new KeyValuePair<string,Action>(name,action);
        public static IEnumerable<KeyValuePair<string,Action>> All()
        {
            yield return Case(nameof(UnlimitedBuildingsRetainGeometryAndAtomicPayment),UnlimitedBuildingsRetainGeometryAndAtomicPayment);
            yield return Case(nameof(UnlimitedPaidFifoExceedsOldBudgetAndQueue),UnlimitedPaidFifoExceedsOldBudgetAndQueue);
            yield return Case(nameof(ActiveArmiesExceedBothOldEntityCaps),ActiveArmiesExceedBothOldEntityCaps);
            yield return Case(nameof(ExactResearchContinuesBeyondIntegerPrices),ExactResearchContinuesBeyondIntegerPrices);
            yield return Case(nameof(ResearchQuoteRaceAndPaidRecipesAreImmutable),ResearchQuoteRaceAndPaidRecipesAreImmutable);
            yield return Case(nameof(UnlimitedTowerPathsAndInvestmentRemainExact),UnlimitedTowerPathsAndInvestmentRemainExact);
            yield return Case(nameof(FrostUpgradeChangesActualSlowWithoutDamage),FrostUpgradeChangesActualSlowWithoutDamage);
            yield return Case(nameof(CannonRadiusAndTeslaJumpAffectActualCombat),CannonRadiusAndTeslaJumpAffectActualCombat);
            yield return Case(nameof(ArmorAndPierceUseEffectiveStats),ArmorAndPierceUseEffectiveStats);
            yield return Case(nameof(SecondaryTailHasExplicitGrowingClassEffect),SecondaryTailHasExplicitGrowingClassEffect);
            yield return Case(nameof(UncappedMoneyIncomeAndRefundConserveValue),UncappedMoneyIncomeAndRefundConserveValue);
            yield return Case(nameof(ExactHistoricalLedgerExceedsInt64),ExactHistoricalLedgerExceedsInt64);
            yield return Case(nameof(BotsBuySpecialReachOnlyForAdditionalTargets),BotsBuySpecialReachOnlyForAdditionalTargets);
            yield return Case(nameof(FinalClearingRejectsResearchAtomically),FinalClearingRejectsResearchAtomically);
            yield return Case(nameof(LastWaveClearingRejectsNewOrdersWithoutLosingPaidFifo),LastWaveClearingRejectsNewOrdersWithoutLosingPaidFifo);
        }
        static MatchSimulation Sim(bool active=false)
        {
            var s=new MatchSimulation(GameContent.CreateDefault(),42,75);s.State.Phase=MatchPhase.Playing;
            s.State.WavePhase=active?WavePhase.Active:WavePhase.Preparing;s.State.PreparationRemaining=200;
            s.State.Players[0].Gold=s.State.Players[1].Gold=BigInteger.Pow(10,80);s.State.Players[0].AvailableTowerMask=s.State.Players[1].AvailableTowerMask=127;
            return s;
        }
        static CommandResult Command(MatchSimulation sim,CommandType type,int kind=0,int target=-1,MapPoint point=default,BigInteger? research=null)
        {
            var c=new CommandEnvelope{SessionId=sim.State.SessionId,Sequence=sim.LastSequence(PlayerSlot.Zero)+1,Type=type,Kind=kind,Target=target,PositionX=point.X,PositionY=point.Y};
            if(type==CommandType.SendUnit){var q=SendRules.Quote(sim.Content,sim.State,PlayerSlot.Zero,kind);c.ExpectedWave=q.WaveId;c.EnemyTarget=q.EnemyTarget;c.ExpectedResearch=research??q.ResearchLevel;}
            if(type==CommandType.ResearchUnit)c.ExpectedResearch=research??sim.State.Players[0].ResearchLevels[kind];
            return sim.Execute(PlayerSlot.Zero,c);
        }
        static TowerState Tower(MatchSimulation sim,int kind,BigInteger primary,BigInteger secondary,MapPoint? position=null)
        {
            BigInteger level=primary+secondary;var t=new TowerState{Id=500+sim.State.Towers.Count,Slot=sim.State.Towers.Count,Kind=kind,Owner=PlayerSlot.Zero,
                Level=level,DamageLevel=kind==4?0:primary,RangeLevel=kind==4?0:secondary,Position=position??new MapPoint(80,130),SiteId=-1,
                Invested=TowerUpgradeRules.TotalInvestment(sim.Content,kind,level)};sim.State.Towers.Add(t);return t;
        }
        static UnitState Unit(MatchSimulation sim,int distance,int kind=2)
        {
            var u=new UnitState{Id=1000+sim.State.Units.Count,Kind=kind,ResearchLevel=10,WaveId=1,Origin=UnitOrigin.PaidPvP,Defender=PlayerSlot.Zero,Distance=distance};
            u.Hp=sim.GetUnitStats(u).MaxHp;sim.State.Units.Add(u);return u;
        }
        static void Quiet(MatchSimulation sim)
        {sim.State.NeutralCursor[0]=sim.State.NeutralCursor[1]=sim.Content.Waves[0].TotalCount;}
        public static void UnlimitedBuildingsRetainGeometryAndAtomicPayment()
        {
            var s=Sim();int built=0,mines=0;var original=s.State.Players[0].Gold;BigInteger spent=0;
            for(int y=40;y<920&&built<45;y+=65)for(int x=40;x<1560&&built<45;x+=65)
            {
                int kind=built%3==0?4:0;var p=new MapPoint(x,y);
                if(PlacementValidator.ValidatePosition(s.Content,s.State,PlayerSlot.Zero,kind,-1,p)!=ErrorCode.None)continue;
                if(built%30==0)s.State.Tick+=20; // elapsed control time keeps ordinary input rate protection separate from quantity.
                var r=Command(s,CommandType.BuildTower,kind,-1,p);Check(r.Accepted,"legal purchase failed "+r.Code);
                built++;if(kind==4)mines++;spent+=s.Content.Towers[kind].Price;
            }
            Check(built==45&&mines>2,"insufficient legal building fixture");Equal(original-s.State.Players[0].Gold,spent,"building debit");
            var first=s.State.Towers[0];Check(Command(s,CommandType.BuildTower,0,-1,first.Position).Code==ErrorCode.TooClose,"geometry lost");
            s.State.Players[0].Gold=0;var quote=TowerUpgradeRules.Quote(s.Content,first,TowerUpgradeRules.PrimaryPath(s.Content,first.Kind));
            Check(quote.IsValid,"no max quote");Check(Command(s,CommandType.UpgradeTower,(int)quote.Path,first.Id).Code==ErrorCode.InsufficientGold,"unaffordable upgrade accepted");Equal(first.Level,0,"failed upgrade changed level");
        }
        public static void UnlimitedPaidFifoExceedsOldBudgetAndQueue()
        {
            var s=Sim();var before=s.State.Players[0].Gold;BigInteger spent=0;
            for(int i=0;i<125;i++){if(i%30==0)s.State.Tick+=20;int k=i%4;Check(Command(s,CommandType.SendUnit,k).Accepted,"order "+i);spent+=s.Content.Units[k].Price;}
            Check(s.State.SpawnOrders.Count==125,"queue truncated");Equal(before-s.State.Players[0].Gold,spent,"queue debit");Check(spent>240,"old budget not exceeded");
            foreach(var order in s.State.SpawnOrders)Check(order.WaveId==1,"old capacity pushed packet into future");
            Equal(s.State.Players[0].ReservedGoldByWave[0],spent,"reservation ledger");Check(EconomyRules.ValidateLedger(s.Content,s.State,PlayerSlot.Zero)==ErrorCode.None,"ledger invalid");
            for(int i=0;i<125;i++)Check(s.State.SpawnOrders[i].Kind==i%4,"FIFO changed");
        }
        public static void ActiveArmiesExceedBothOldEntityCaps()
        {
            var s=Sim(true);Quiet(s);for(int i=0;i<100;i++){if(i%30==0)s.State.Tick+=20;Check(Command(s,CommandType.SendUnit,3).Accepted,"army order");}
            for(int i=0;i<1500&&s.State.Phase==MatchPhase.Playing;i++)
            {
                s.Step();int paid=0;foreach(var u in s.State.Units)if(u.Defender==PlayerSlot.One&&u.Origin==UnitOrigin.PaidPvP)paid++;
                if(paid>80){Check(paid>64,"origin cap");Check(EconomyRules.ValidateLedger(s.Content,s.State,PlayerSlot.Zero)==ErrorCode.None,"army ledger");return;}
            }
            throw new InvalidOperationException("Paid army never exceeded eighty units");
        }
        public static void ExactResearchContinuesBeyondIntegerPrices()
        {
            foreach(BigInteger level in new[]{new BigInteger(8),10,100,BigInteger.Pow(10,15),BigInteger.Pow(10,30)})
            for(int k=0;k<4;k++)
            {
                var s=Sim();s.State.Players[0].ResearchLevels[k]=level;var q=UnitResearchRules.Quote(s.Content,s.State.Players[0],k);
                Check(q.IsValid&&q.Next.MaxHp>q.Current.MaxHp&&q.Price>0,"research stopped or lost usefulness");
                var gold=s.State.Players[0].Gold;Check(Command(s,CommandType.ResearchUnit,k,-1,research:level).Accepted,"exact research purchase");
                Equal(s.State.Players[0].ResearchLevels[k],level+1,"level wrap");Equal(gold-s.State.Players[0].Gold,q.Price,"research debit");
                var frozen=s.State.Players[0].ResearchLevels[k];Check(Command(s,CommandType.ResearchUnit,k,-1,research:level).Code==ErrorCode.QuoteChanged,"stale research quote accepted");Equal(s.State.Players[0].ResearchLevels[k],frozen,"stale mutated level");
            }
        }
        public static void ResearchQuoteRaceAndPaidRecipesAreImmutable()
        {
            var s=Sim();var level=BigInteger.Pow(10,20);s.State.Players[0].ResearchLevels[0]=level;
            Check(Command(s,CommandType.SendUnit,0).Accepted,"old recipe buy");Check(Command(s,CommandType.ResearchUnit,0,-1,research:level).Accepted,"recipe research");
            var gold=s.State.Players[0].Gold;Check(Command(s,CommandType.SendUnit,0,research:level).Code==ErrorCode.QuoteChanged,"stale packet accepted");Equal(s.State.Players[0].Gold,gold,"stale quote charged");
            Check(Command(s,CommandType.SendUnit,0).Accepted,"new recipe buy");Equal(s.State.SpawnOrders[0].ResearchLevel,level,"paid recipe upgraded retroactively");Equal(s.State.SpawnOrders[1].ResearchLevel,level+1,"new recipe not advanced");
            s.State.WavePhase=WavePhase.Active;Quiet(s);s.Step();Check(s.State.Units.Count==1,"first paid spawn");Equal(s.State.Units[0].ResearchLevel,level,"spawn altered paid recipe");
        }
        public static void UnlimitedTowerPathsAndInvestmentRemainExact()
        {
            for(int kind=0;kind<7;kind++)
            {
                var s=Sim();var t=Tower(s,kind,0,0);BigInteger expected=s.Content.Towers[kind].Price;
                for(int i=0;i<120;i++)
                {
                    if(i%30==0)s.State.Tick+=20;var path=kind==4||i%2==0?TowerUpgradeRules.PrimaryPath(s.Content,kind):TowerUpgradeRules.SecondaryPath(s.Content,kind);
                    var q=TowerUpgradeRules.Quote(s.Content,t,path);Check(q.IsValid,"tower max");expected+=q.Price;
                    Check(Command(s,CommandType.UpgradeTower,(int)path,t.Id).Accepted,"tower upgrade rejected");Equal(t.Invested,expected,"investment changed");Equal(TowerUpgradeRules.TotalInvestment(s.Content,kind,t.Level),expected,"closed form investment");
                }
                t.Level=BigInteger.Pow(10,30);t.DamageLevel=kind==4?0:t.Level;t.RangeLevel=0;t.Invested=TowerUpgradeRules.TotalInvestment(s.Content,kind,t.Level);
                var huge=TowerUpgradeRules.Quote(s.Content,t,TowerUpgradeRules.PrimaryPath(s.Content,kind));Check(huge.IsValid&&huge.Price>int.MaxValue,"huge tower quote");
                Equal(TowerUpgradeRules.TotalInvestment(s.Content,kind,t.Level+1)-t.Invested,huge.Price,"huge cumulative difference");
                Check(TowerUpgradeRules.VisualStage(t.Level)==10,"visual stage unsafe");Check(TowerUpgradeRules.VisualVariant(t)<6,"legacy variant overflow");
            }
        }
        public static void FrostUpgradeChangesActualSlowWithoutDamage()
        {
            var s=Sim(true);Quiet(s);var t=Tower(s,2,0,0);var u=Unit(s,0);var old=TowerUpgradeRules.Stats(s.Content,t);var gold=s.State.Players[0].Gold;
            Check(Command(s,CommandType.UpgradeTower,(int)TowerUpgradePath.Damage,t.Id).Code==ErrorCode.InvalidKind,"Frost accepted Damage");Equal(s.State.Players[0].Gold,gold,"invalid Frost charged");
            Check(Command(s,CommandType.UpgradeTower,(int)TowerUpgradePath.FrostDuration,t.Id).Accepted,"Frost duration purchase");
            var next=TowerUpgradeRules.Stats(s.Content,t);Equal(next.Damage,old.Damage,"Frost damage changed");Equal(next.SlowDurationTicks,34,"Frost duration curve");
            var hp=u.Hp;s.Step();Equal(hp-u.Hp,MatchSimulation.DamageAfterArmor(old.Damage,s.GetUnitStats(u).Armor,next.ArmorPiercing),"Frost hit damage");
            Equal(u.SlowUntilTick,s.State.PlayTicks+34,"Hit ignored upgraded duration");var activeUntil=u.SlowUntilTick;
            Check(Command(s,CommandType.UpgradeTower,(int)TowerUpgradePath.Range,t.Id).Accepted,"Frost range purchase");Equal(u.SlowUntilTick,activeUntil,"range retroactively extended slow");
            Equal(TowerUpgradeRules.Stats(s.Content,t).SlowDurationTicks,34,"early range changed duration");
        }
        public static void CannonRadiusAndTeslaJumpAffectActualCombat()
        {
            foreach(int kind in new[]{1,5})
            {
                var plain=Sim(true);var upgraded=Sim(true);Quiet(plain);Quiet(upgraded);
                Tower(plain,kind,0,0);Tower(upgraded,kind,0,10);
                var a=Unit(plain,0);var b=Unit(plain,kind==1?130:130);var c=Unit(upgraded,0);var d=Unit(upgraded,kind==1?130:130);
                var hpA=a.Hp;var hpB=b.Hp;plain.Step();upgraded.Step();
                Check(c.Hp<a.Hp||d.Hp<b.Hp,"special secondary did not reach additional target for class "+kind);
                Equal(TowerUpgradeRules.Stats(plain.Content,plain.State.Towers[0]).Damage,TowerUpgradeRules.Stats(upgraded.Content,upgraded.State.Towers[0]).Damage,"early secondary unexpectedly changed damage");
            }
        }
        public static void ArmorAndPierceUseEffectiveStats()
        {
            foreach(int kind in new[]{0,3,6}) foreach(var level in new[]{new BigInteger(10),BigInteger.Pow(10,30)})
            {
                var s=Sim(true);Quiet(s);var t=Tower(s,kind,level,0);var u=Unit(s,0);
                u.ResearchLevel=BigInteger.Pow(10,40);u.Hp=s.GetUnitStats(u).MaxHp;
                var hp=u.Hp;var stats=TowerUpgradeRules.Stats(s.Content,t);s.Step();
                Equal(hp-u.Hp,BigInteger.Min(hp,MatchSimulation.DamageAfterArmor(stats.Damage,s.GetUnitStats(u).Armor,stats.ArmorPiercing)),"effective damage ignored for class "+kind);
                Check(stats.Damage>s.Content.Towers[kind].Damage,"class power flat");
            }
            var huge=Sim(true);Quiet(huge);var frost=Tower(huge,2,BigInteger.Pow(10,30),0);var target=Unit(huge,0);
            huge.Step();Equal(target.SlowUntilTick,huge.State.PlayTicks+TowerUpgradeRules.Stats(huge.Content,frost).SlowDurationTicks,"huge actual Frost duration overflowed");
        }
        public static void SecondaryTailHasExplicitGrowingClassEffect()
        {
            for(int k=0;k<7;k++)if(k!=4)
            {
                var s=Sim();var t=Tower(s,k,0,BigInteger.Pow(10,20));var path=TowerUpgradeRules.SecondaryPath(s.Content,k);var q=TowerUpgradeRules.Quote(s.Content,t,path);
                Check(TowerUpgradeRules.PathLabel(s.Content,t,path).Contains(" + "),"composite tail not labelled");
                Check(k==2?q.Next.SlowDurationTicks>q.Current.SlowDurationTicks:q.Next.Damage>q.Current.Damage,"tail has no continuing effect");
                if(k==2)Equal(q.Next.Damage,s.Content.Towers[2].Damage,"Frost tail gained damage");
                Check(TowerUpgradeRules.Preview(s.Content,t,path).Length>0,"missing effective preview");
            }
        }
        public static void UncappedMoneyIncomeAndRefundConserveValue()
        {
            var s=Sim(true);Quiet(s);var t=Tower(s,4,BigInteger.Pow(10,15),0);var gold=s.State.Players[0].Gold;var income=EconomyRules.IncomeUnits(s.Content,s.State,PlayerSlot.Zero);
            for(int i=0;i<20;i++)s.Step();Equal(s.State.Players[0].Gold-gold,income/960,"income clamped or rounded incorrectly");Equal(s.State.Players[0].GoldRemainder,income%960,"income remainder");
            gold=s.State.Players[0].Gold;var refund=s.RefundFor(t);Check(Command(s,CommandType.SellTower,0,t.Id).Accepted,"sale failed");Equal(s.State.Players[0].Gold-gold,refund,"refund clamped");
            Check(s.State.Players[0].Gold>long.MaxValue,"large wallet fixture");
        }
        public static void ExactHistoricalLedgerExceedsInt64()
        {
            var s=Sim(true);var p=s.State.Players[0];var amount=BigInteger.Pow(10,40)+17;
            p.ReservedGoldByWave[0]=amount;p.SpawnedQuarterGoldByWave[0]=4*amount;p.AttackIncomeUnits=2880;
            Check(EconomyRules.ValidateLedger(s.Content,s.State,PlayerSlot.Zero)==ErrorCode.None,"valid huge historical ledger rejected");
            p.SpawnedQuarterGoldByWave[0]--;
            Check(EconomyRules.ValidateLedger(s.Content,s.State,PlayerSlot.Zero)==ErrorCode.InvalidValue,"huge ledger lost unit precision");
        }
        public static void BotsBuySpecialReachOnlyForAdditionalTargets()
        {
            foreach (int kind in new[] {1,5}) foreach (int fixture in new[] {0,1,2,3,4})
            {
                var d=GameContent.CreateDefault();
                var map=new MapDefinition(new[]{new MapPoint(0,100),new MapPoint(1000,100)},
                    new[]{new MapPoint(100,220),new MapPoint(700,220)});
                var sim=new MatchSimulation(new GameContent(d.Rules,d.Towers,d.Units,map));
                sim.State.Phase=MatchPhase.Playing;sim.State.WavePhase=WavePhase.Active;
                sim.State.Players[0].Gold=10000;sim.State.Players[0].AvailableTowerMask=127;
                var tower=Tower(sim,kind,0,0,new MapPoint(100,220));tower.SiteId=0;
                var other=Tower(sim,0,1,0,new MapPoint(700,220));other.SiteId=1;
                var primary=Unit(sim,100,0);
                int jump=kind==1?94:78; // Outside base90/75, inside next97/81.
                var secondary=Unit(sim,100-jump,0);
                if(fixture==1)secondary.Defender=PlayerSlot.One; // Other board cannot influence the purchase.
                if(fixture==2)secondary.Hp=0;
                if(fixture==3)secondary.Distance=90; // Already covered: no extra body from buying reach.
                if(fixture==4) { primary.Distance=700;secondary.Distance=700-jump; } // No primary in targeting range.
                var bot=new BotBrain(PlayerSlot.Zero,17,BotStrategy.PureDefense);
                var gold=sim.State.Players[0].Gold;bot.Tick(sim);
                Equal(tower.Level,1,"Bot did not execute ordinary upgrade command");
                Equal(tower.RangeLevel,fixture==0?1:0,"Bot chose special radius without an additional reachable target");
                Equal(tower.DamageLevel,fixture==0?0:1,"Bot selected wrong class path");
                Equal(gold-sim.State.Players[0].Gold,18,"Bot upgrade debit changed");
                if(fixture==0)
                {
                    other.CooldownTicks=100;var hp=secondary.Hp;Quiet(sim);sim.Step();
                    Check(secondary.Hp<hp,"Bot-bought special reach did not affect actual combat");
                }
            }
        }
        public static void FinalClearingRejectsResearchAtomically()
        {
            var sim=Sim();sim.State.WaveId=sim.Content.WaveCount;
            Check(Command(sim,CommandType.SendUnit,0).Accepted,"last wave paid fixture");
            sim.State.WavePhase=WavePhase.Clearing;
            var gold=sim.State.Players[0].Gold;var recipe=sim.State.SpawnOrders[0].ResearchLevel;
            Check(Command(sim,CommandType.ResearchUnit,0).Code==ErrorCode.WaveCapacity,"last clearing sold unusable research");
            Equal(sim.State.Players[0].Gold,gold,"rejected research spent gold");
            Equal(sim.State.Players[0].ResearchLevels[0],0,"rejected research advanced level");
            Equal(sim.State.SpawnOrders[0].ResearchLevel,recipe,"rejected research changed bound packet");
            sim=Sim();sim.State.WaveId=sim.Content.WaveCount-1;sim.State.WavePhase=WavePhase.Clearing;
            gold=sim.State.Players[0].Gold;
            Check(Command(sim,CommandType.ResearchUnit,0).Accepted,"earlier clearing research rejected");
            Equal(gold-sim.State.Players[0].Gold,15,"earlier clearing research debit");
            Check(Command(sim,CommandType.SendUnit,0).Accepted,"researched final-wave packet rejected");
            Equal(sim.State.SpawnOrders[0].ResearchLevel,1,"next-wave packet did not bind research");
            Equal(sim.State.SpawnOrders[0].WaveId,sim.Content.WaveCount,"clearing packet bound wrong wave");
        }
        public static void LastWaveClearingRejectsNewOrdersWithoutLosingPaidFifo()
        {
            var s=Sim();s.State.WaveId=10;Check(Command(s,CommandType.SendUnit,0).Accepted,"last-wave paid order");s.State.WavePhase=WavePhase.Clearing;var gold=s.State.Players[0].Gold;
            Check(SendRules.Quote(s.Content,s.State,PlayerSlot.Zero,0).Code==ErrorCode.WaveCapacity,"last-wave horizon ignored");
            Equal(s.State.Players[0].Gold,gold,"quote charged");Check(s.State.SpawnOrders.Count==1,"accepted packet lost");
        }
    }
}
