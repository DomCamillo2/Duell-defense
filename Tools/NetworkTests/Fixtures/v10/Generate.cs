using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using DefenseDuell.Core;
using DefenseDuell.Networking;
class Generate
{
 static GameContent c;static List<object> frames=new List<object>();static List<object> proofs=new List<object>();static int checks;
 static string Bytes(MatchState s)=>Convert.ToBase64String(WireProtocol.EncodeSnapshot(s));
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 static void Frame(string name,MatchSimulation s){string b=Bytes(s.State);Check(WireProtocol.TryDecodeSnapshot(Convert.FromBase64String(b),s.Content,out _),"canonical snapshot "+name);frames.Add(new{name,bytes=b});}
 static CommandEnvelope Cmd(MatchSimulation s,PlayerSlot p,CommandType t,int k=0,int target=-1)
 {return new CommandEnvelope{SessionId=s.State.SessionId,Sequence=s.LastSequence(p)+1,Type=t,Kind=k,Target=target,ExpectedConfigRevision=t==CommandType.SetReady?s.State.ConfigRevision:0,ExpectedRosterRevision=t==CommandType.SetReady?s.State.RosterRevision:0};}
 static object Fields(CommandEnvelope x)=>new{SessionId=x.SessionId.ToString(),Sequence=x.Sequence.ToString(),Type=(int)x.Type,x.Kind,x.Target,x.PositionX,x.PositionY,ExpectedWave=x.ExpectedWave.ToString(),ExpectedResearch=x.ExpectedResearch.ToString(),ExpectedConfigRevision=x.ExpectedConfigRevision.ToString(),ExpectedRosterRevision=x.ExpectedRosterRevision.ToString(),x.RuleWaveCount,RuleStartGold=x.RuleStartGold.ToString(),x.RuleEndless,x.RuleIncomeMultiplier,RuleFormat=(int)x.RuleFormat,x.RuleMapId,x.EnemyTarget,UpgradeCount=x.UpgradeCount.ToString(),ExpectedTowerLevel=x.ExpectedTowerLevel.ToString(),UpgradePriceLimit=x.UpgradePriceLimit.ToString()};
 static CommandResult Execute(MatchSimulation s,PlayerSlot p,CommandEnvelope cmd,string label)
 {
   var before=Bytes(s.State);var body=WireProtocol.EncodeCommand(cmd);Check(WireProtocol.TryDecodeCommand(body,out var decoded),"command codec");
   var r=s.Execute(p,decoded);proofs.Add(new{label,player=(int)p,command=Fields(cmd),wire=Convert.ToBase64String(body),before,after=Bytes(s.State),code=(int)r.Code});return r;
 }
 static void Ready(MatchSimulation s){for(int i=0;i<s.State.Players.Length;i++){var p=(PlayerSlot)i;Check(s.Execute(p,Cmd(s,p,CommandType.SetReady)).Accepted,"ready");}while(s.State.Phase==MatchPhase.Countdown||s.State.WavePhase==WavePhase.Preparing)s.Step();}
 static MatchState Copy(MatchSimulation s){Check(WireProtocol.TryDecodeSnapshot(WireProtocol.EncodeSnapshot(s.State),s.Content,out var copy),"copy");return copy;}
 public static int Main(string[] args)
 {
   c=GameContent.CreateDefault();Check(c.Units.Length==6,"sixclasses");var s=new MatchSimulation(c.WithMatchRules(new MatchRules(100,BigInteger.Pow(10,30))),seed:42,sessionId:42);
   Frame("lobby",s);Ready(s);Frame("active",s);
   MapPoint point=default;bool found=false;
   for(int y=50;y<c.Map.Height-50&&!found;y+=25)for(int x=50;x<c.Map.Width-50&&!found;x+=25)
   if(PlacementValidator.ValidateBuild(s.Content,s.State,PlayerSlot.Zero,0,-1,new MapPoint(x,y),out _)==ErrorCode.None){point=new MapPoint(x,y);found=true;}
   var build=Cmd(s,PlayerSlot.Zero,CommandType.BuildTower);build.PositionX=point.X;build.PositionY=point.Y;Check(Execute(s,PlayerSlot.Zero,build,"build").Accepted,"build");
   var tower=s.State.Towers[0];var quote=TowerUpgradeBatchRules.Quote(c,tower,TowerUpgradeRules.PrimaryPath(c,0),s.State.Players[0].Gold,UpgradeBatchMode.Ten);
   var batch=Cmd(s,PlayerSlot.Zero,CommandType.UpgradeTowerBatch,(int)TowerUpgradeRules.PrimaryPath(c,0),tower.Id);batch.UpgradeCount=quote.Count;batch.ExpectedTowerLevel=tower.Level;batch.UpgradePriceLimit=quote.Price;
   Check(Execute(s,PlayerSlot.Zero,batch,"batch10").Accepted,"batch10");
   Check(Execute(s,PlayerSlot.Zero,batch,"batch-exact-replay").Accepted,"replay");
   var changed=batch;changed.UpgradeCount++;Check(Execute(s,PlayerSlot.Zero,changed,"batch-mutated-replay").Code==ErrorCode.Duplicate,"mutation");
   var stale=batch;stale.Sequence=s.LastSequence(PlayerSlot.Zero)+1;Check(Execute(s,PlayerSlot.Zero,stale,"batch-stale").Code==ErrorCode.QuoteChanged,"stale");
   var guest=batch;guest.Sequence=s.LastSequence(PlayerSlot.One)+1;Check(Execute(s,PlayerSlot.One,guest,"batch-not-owner").Code==ErrorCode.NotOwner,"guest");
   var price=TowerUpgradeBatchRules.Quote(c,tower,TowerUpgradeRules.PrimaryPath(c,0),s.State.Players[0].Gold,UpgradeBatchMode.Single);
   var low=Cmd(s,PlayerSlot.Zero,CommandType.UpgradeTowerBatch,(int)TowerUpgradeRules.PrimaryPath(c,0),tower.Id);low.UpgradeCount=1;low.ExpectedTowerLevel=tower.Level;low.UpgradePriceLimit=price.Price-1;
   Check(Execute(s,PlayerSlot.Zero,low,"batch-price-limit").Code==ErrorCode.QuoteChanged,"price binding");
   foreach(int kind in new[]{4,5})
   {
     var research=Cmd(s,PlayerSlot.Zero,CommandType.ResearchUnit,kind);research.ExpectedResearch=s.State.Players[0].ResearchLevels[kind];Check(Execute(s,PlayerSlot.Zero,research,"elite-research"+kind).Accepted,"research");
     var q=SendRules.Quote(s.Content,s.State,PlayerSlot.Zero,kind);var send=Cmd(s,PlayerSlot.Zero,CommandType.SendUnit,kind);send.EnemyTarget=(int)q.EnemyTarget;send.ExpectedWave=q.WaveId;send.ExpectedResearch=q.ResearchLevel;Check(Execute(s,PlayerSlot.Zero,send,"elite-purchase"+kind).Accepted,"send");
   }
   Frame("elite-queue",s);for(int i=0;i<30;i++)s.Step();Frame("elite-and-neutral",s);
   // Canonical neutral wave10 recipe, exact ordinal from actual Core spawn, no fabricated unit.
   var boss=new MatchSimulation(c.WithMatchRules(new MatchRules(100,BigInteger.Pow(10,30))),seed:43,sessionId:43);Ready(boss);
   boss.State.WaveId=10;boss.State.WavePhase=WavePhase.Active;boss.State.WaveStartPlayTick=boss.State.PlayTicks;
   for(int i=0;i<40;i++)boss.Step();Frame("boss-wave10",boss);
   var bad=Copy(s);bad.Players[0].AvailableTowerMask=63;var negatives=new List<object>();
   void Invalid(string name,MatchState state){byte[] b=WireProtocol.EncodeSnapshot(state);Check(!WireProtocol.TryDecodeSnapshot(b,s.Content,out _),"negative rejected "+name);negatives.Add(new{name,bytes=Convert.ToBase64String(b)});}
   Invalid("six-loadout",bad);
   bad=Copy(s);bad.Units.First(u=>u.Origin==UnitOrigin.PaidPvP).NeutralRecipeIndex=0;Invalid("paid-neutral-ordinal",bad);
   bad=Copy(s);bad.Units.First(u=>u.Origin==UnitOrigin.NeutralWave).NeutralRecipeIndex=999;Invalid("neutral-ordinal-outside-cursor",bad);
   bad=Copy(s);var neutral=bad.Units.First(u=>u.Origin==UnitOrigin.NeutralWave);neutral.Kind=5;Invalid("elite-in-neutral-plan",bad);
   bad=Copy(s);var ns=bad.Units.Where(u=>u.Origin==UnitOrigin.NeutralWave&&u.Defender==PlayerSlot.Zero).ToArray();Check(ns.Length>=2,"neutral fixture pair");ns[1].NeutralRecipeIndex=ns[0].NeutralRecipeIndex;ns[1].Kind=ns[0].Kind;Invalid("duplicate-neutral-ordinal",bad);
   foreach(var format in new[]{MatchFormat.TwoVsTwo,MatchFormat.ThreeVsThree,MatchFormat.TwoVsTwoVsTwo})
   {
     var team=new MatchSimulation(c.WithMatchRules(new MatchRules(100,BigInteger.Pow(10,30),format:format)),sessionId:100+(ulong)format);Ready(team);Frame("team-"+format,team);
   }
   var endless=new MatchSimulation(c.WithMatchRules(new MatchRules(1,BigInteger.Pow(10,30),endless:true)),sessionId:201);Ready(endless);
   endless.State.WaveId=201;
   foreach(var p in endless.State.Players){p.ReservedGoldByWave=new WaveLedger(201,new BigInteger[2],true);p.SpawnedQuarterGoldByWave=new WaveLedger(201,new BigInteger[2],true);p.CancelledQuarterGoldByWave=new WaveLedger(201,new BigInteger[2],true);}
   Frame("endless201-codec-fixture",endless);
   var plans=new List<object>();foreach(var wave in new[]{1,10,100,201})
   {
     var recipe=endless.Content.GetWave(wave);
     plans.Add(new{wave=wave.ToString(),profileId=recipe.ProfileId,pressureBudgetHp=recipe.PressureBudgetHp.ToString(),plan=recipe.Plan,hpPerKind=Enumerable.Range(0,4).Select(k=>UnitResearchRules.Resolve(endless.Content,k,0,UnitOrigin.NeutralWave,wave).MaxHp.ToString()).ToArray(),boss=recipe.IsBossWave,superBoss=wave>=100&&wave%100==0,bossIndex=recipe.BossUnitIndex});
   }
   var metadata=new{rulesVersion=GameContent.SimulationRulesVersion,wavePlanVersion=MatchRulesPolicy.WavePlanVersion,protocol=WireProtocol.Version,neutralRecipes=plans,unitPrices=c.Units.Select(u=>u.Price),unitCounts=c.Units.Select(u=>u.Count),researchPrices=c.Research.Select(u=>u.BasePrice),unitCooldownTicks=c.Units.Select(u=>u.CooldownTicks),spawnIntervalTicks=c.Rules.SpawnIntervalTicks,towerPrices=c.Towers.Select(t=>t.Price),refundPercents=Enumerable.Range(0,c.Towers.Length).Select(k=>EconomyRules.RefundPercent(c,k)),tickRate=c.TickRate,waveBudget=c.WaveRules.WaveBudget,passiveGoldPerSecond=c.PassiveGoldPerSecond,incomeUnitsPerGold=c.WaveRules.IncomeUnitsPerGold,towerProfiles=c.Towers.Select(t=>new{price=t.Price,fireIntervalTicks=t.FireIntervalTicks}),waveCount=100,expectedBaseHp=c.BaseHp,preparationTicks=c.WaveRules.PreparationTicks,mapId=c.Map.Id};
   var obj=new{scope="Pure actual C# production codec fixtures; not transport",checks,metadata,contentHash=c.ContentHash,handshake=Convert.ToBase64String(WireProtocol.Handshake(c.ContentHash,143)),frames,proofs,negatives};
   File.WriteAllText(args[0],JsonSerializer.Serialize(obj,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("WIRE10_GOLDENS_PASS checks="+checks);return 0;
 }
}
