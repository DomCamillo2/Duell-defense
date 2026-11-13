# Unabhängiger Review: Wirtschaft und Spielregeln

Datum: 10.11.2026. Reviewer: Subagent UI/Assets, unabhängig vom Autor der Core-Simulation.

**Ergebnis:** Keine offenen kritischen Fehler in den geprüften Kauf-, Verkaufs-, Queue-, Frost- und Ergebnisregeln gefunden. Sechs zusätzlich entwickelte Randfallprüfungen wurden mit Exit-Code 0 ausgeführt. Dies ist eine Quellcode-/Core-Prüfung; sie ersetzt keine Netzwerk-, Unity- oder Android-Abnahme.

## Geprüfter Stand

- `Assets/Core/MatchSimulation.cs`: SHA-256 `f932c8bb19346469228c0e98574b8915a2c48f24c92efa6aef6295360dfea523`
- `Assets/Core/GameContent.cs`: SHA-256 `202f71c741622801db3c83e12cc9ef6560e65a89fa38a60a05fe9b96f9735f68`
- Ergänzend gelesen: MatchState, BotBrain, CoreScenarios sowie vorhandene Balance-/Stressnachweise.
- Keine Core-Dateien durch den Reviewer geändert.

## Geprüfte Invarianten

| Bereich | Befund |
|---|---|
| Käufe | Kind, Ziel, Besitz, Phase, Cooldown, Queue und Gold werden vor der jeweiligen wirtschaftlichen Mutation geprüft. Nur Request-Metadaten werden bei Ablehnung verbraucht. |
| Idempotenz | Gleiche Request-ID und identischer Payload liefern den gespeicherten Beleg. Geänderter Payload wird abgelehnt. Nach Verdrängung aus dem 128-Einträge-Cache verhindert der Sequenz-Wasserstand eine erneute Ausführung. Alte Session-IDs werden vor der Ausführung abgewiesen. |
| Verkauf/Upgrades | Verkauf entfernt den Turm einmalig. Die feste Rückerstattung ist abgerundet und auf MaxGold begrenzt. Zwei Upgrades sind erlaubt; ein Upgrade setzt den Schusstimer nicht zurück. |
| Queue | Vor Goldabbuchung begrenzt; FIFO pro Sender. Bezahlte Gruppen werden nur beim tatsächlichen Spawn dekrementiert. Volles Feld wartet, statt Truppen oder Gold zu verlieren. |
| Frost/Rüstung | Rüstungsabzug mit Mindestschaden 1; Panzerbrecher reduziert effektive Rüstung. Frost verwendet den stärksten begrenzten Geschwindigkeitsfaktor und stapelt nicht multiplikativ. Bewegungsreste verhindern einen Rundungsstillstand langsamer Einheiten. |
| Basis/Ergebnis | Tote Einheiten werden vor Basisschaden entfernt. Entkommene Einheiten werden genau einmal entfernt/verrechnet. Beide Felder werden vollständig aufgelöst, bevor ein Ergebnis festgeschrieben wird. Finished verhindert spätere Ticks und Abort-Änderungen. |
| Definitionen/Hash | Öffentliche Array-Getter liefern Kopien; Definitionselemente sind immutable. Alle überprüften Balancewerte, Karte und Regelversionskennung fließen in SHA-256 ein. Bei Änderungen der Rechenregeln muss SimulationRulesVersion bewusst erhöht werden. |

## Zusätzliche ausgeführte Prüfungen

Ausgeführt mit dem vorhandenen .NET SDK 8.0.318 des Unity-Editors 6000.6.6f1; Core-Quellen selbst sind engineunabhängig. Kein NuGet-Download erforderlich. Dies war kein Test unter dem Unity-Player und kein Android-Performancebeleg.

```text
PASS RejectedReceiptCannotBecomePurchaseAfterIncome
PASS WorstLegalEconomyDoesNotOverflow
PASS ManyBuySellCyclesCannotManufactureGold
PASS SlowestLegalFrostVictimStillAdvances
PASS AllPaidUnitsEventuallyLeaveFullFieldQueue
PASS SameTickFinishCannotBeChangedByLaterIntent
INDEPENDENT_REVIEW total=6 failures=0
```

Die Fälle prüfen insbesondere eine nach späterem Einkommen erneut abgespielte Ablehnung, legal maximal hohe Preise/Investitionen, 80 Kauf-/Verkaufszyklen mit doppelten Verkaufsrequests, eine dauerhaft beschossene Einheit mit nur einer Weg-Einheit pro Sekunde, vollständige Abarbeitung einer bezahlten Queue nach vollem Feld sowie ein unveränderliches simultanes Unentschieden.

Tatsächlicher Befehl:

```sh
DOTNET_CLI_HOME=/private/tmp/defense-economy-dotnet \
  /Applications/Unity/Hub/Editor/6000.6.6f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet \
  run --project /private/tmp/defense-economy-review/Review.csproj --configuration Release \
  --configfile /private/tmp/defense-economy-review/NuGet.Config
```

## Offene Balance- und Vertrauensgrenzen

Die vorhandenen 24 Matches je Strategie mit getauschten Seiten zeigen: reine Verteidigung gegen den Balanced-Bot endet in 24/24 Fällen nach acht Minuten mit 30:30. Das ist regelkonform, aber ein Risiko für spannende menschliche Partien. Der untersuchte höhere-HP-Kandidat löst dies nicht und macht TankRush mit 24/24 Siegen dominant; ihn nicht zu übernehmen ist durch die Messung begründet. Ein menschlicher Spieltest und weitere kleine Balanceversuche bleiben offen. Die automatisierten Bot-Matches beweisen keinen Spielspaß.

MatchState ist absichtlich eine veränderliche autoritative Laufzeitstruktur. Die geprüfte Sicherheit setzt voraus, dass ausschließlich validierte Commands von Clients in diesen Zustand gelangen. Diese Grenze muss im Networking-Review separat geprüft werden. Ein manipulierter Host ist weiterhin eine dokumentierte Vertrauensgrenze.

## Reproduzierbarer Zusatz-Harness

Den folgenden C#-Block als `Program.cs` in einem temporären Verzeichnis speichern. Ein .NET-8-Konsolenprojekt muss ihn und `Assets/Core/*.cs` des zu prüfenden Projekts kompilieren. Keine zusätzlichen Pakete erforderlich. Beispielprojekt:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
  <ItemGroup><Compile Include="/ABSOLUTER/PROJEKTPFAD/Assets/Core/*.cs"/><Compile Include="Program.cs"/></ItemGroup>
</Project>
```

Mit `NuGet.Config` und Inhalt `<configuration><packageSources><clear /></packageSources></configuration>` sind externe Paketquellen ausgeschlossen.

```csharp
using System;
using System.Text.Json;
using DefenseDuell.Core;
class Review {
 static int failures;
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 static CommandEnvelope Cmd(MatchSimulation s,PlayerSlot p,CommandType type,int kind=0,int target=-1)=>new CommandEnvelope{SessionId=s.State.SessionId,Sequence=s.LastSequence(p)+1,Type=type,Kind=kind,Target=target};
 static MatchSimulation Start(GameContent c){var s=new MatchSimulation(c);s.Execute(PlayerSlot.Zero,Cmd(s,PlayerSlot.Zero,CommandType.SetReady));s.Execute(PlayerSlot.One,Cmd(s,PlayerSlot.One,CommandType.SetReady));while(s.State.Phase==MatchPhase.Countdown)s.Step();return s;}
 static string Snapshot(MatchState state)=>JsonSerializer.Serialize(state,new JsonSerializerOptions{IncludeFields=true});
 static void Run(string name,Action action){try{action();Console.WriteLine("PASS "+name);}catch(Exception e){failures++;Console.WriteLine("FAIL "+name+" "+e.Message);}}
 static int Main(){
 Run("RejectedReceiptCannotBecomePurchaseAfterIncome",()=>{
  var s=Start(GameContent.CreateDefault(new GameRules(startGold:0)));var cmd=Cmd(s,PlayerSlot.Zero,CommandType.BuildTower,0,0);
  Check(s.Execute(PlayerSlot.Zero,cmd).Code==ErrorCode.InsufficientGold,"initial rejection");for(int i=0;i<240;i++)s.Step();
  Check(s.State.Players[0].Gold>=65,"test gained enough gold");string before=Snapshot(s.State);
  Check(s.Execute(PlayerSlot.Zero,cmd).Code==ErrorCode.InsufficientGold,"cached rejection changed");Check(Snapshot(s.State)==before,"replay mutated current canonical state");
 });
 Run("WorstLegalEconomyDoesNotOverflow",()=>{
  var original=GameContent.CreateDefault();var ts=original.Towers;
  ts[0]=new TowerDefinition("Boundary",1000000,150,6,8,0,1000,0,0,new[]{new UpgradeDefinition(1000000,9,165,7),new UpgradeDefinition(1000000,14,180,6)});
  var c=new GameContent(new GameRules(startGold:1000000,maxGold:1000000,sellRefundPercent:100),ts,original.Units,original.Map);var s=Start(c);
  Check(s.Execute(PlayerSlot.Zero,Cmd(s,PlayerSlot.Zero,CommandType.BuildTower,0,0)).Accepted,"build");int id=s.State.Towers[0].Id;
  for(int i=0;i<2;i++){s.State.Players[0].Gold=1000000;Check(s.Execute(PlayerSlot.Zero,Cmd(s,PlayerSlot.Zero,CommandType.UpgradeTower,0,id)).Accepted,"upgrade");}
  Check(s.State.Towers[0].Invested==3000000,"invested overflow");Check(s.Execute(PlayerSlot.Zero,Cmd(s,PlayerSlot.Zero,CommandType.SellTower,0,id)).Accepted,"sale");Check(s.State.Players[0].Gold==1000000,"refund not capped");
 });
 Run("ManyBuySellCyclesCannotManufactureGold",()=>{
  var s=Start(GameContent.CreateDefault(new GameRules(startGold:10000,passiveGoldPerSecond:0)));int previous=s.State.Players[0].Gold;
  for(int i=0;i<80;i++){
   Check(s.Execute(PlayerSlot.Zero,Cmd(s,PlayerSlot.Zero,CommandType.BuildTower,i%4,i%12)).Accepted,"build failed");int id=s.State.Towers[0].Id;
   var sale=Cmd(s,PlayerSlot.Zero,CommandType.SellTower,0,id);Check(s.Execute(PlayerSlot.Zero,sale).Accepted,"sale failed");int balance=s.State.Players[0].Gold;
   s.Execute(PlayerSlot.Zero,sale);Check(s.State.Players[0].Gold==balance&&balance<previous,"loop minted gold");previous=balance;
   for(int tick=0;tick<20;tick++)s.Step();
  }
 });
 Run("SlowestLegalFrostVictimStillAdvances",()=>{
  var c=GameContent.CreateDefault(new GameRules(startGold:10000));var us=c.Units;us[2]=new UnitDefinition("Slow boundary",65,100000,1,5,3,1,70);
  c=new GameContent(c.Rules,c.Towers,us,c.Map);var s=Start(c);
  for(int i=0;i<12;i++)s.Execute(PlayerSlot.Zero,Cmd(s,PlayerSlot.Zero,CommandType.BuildTower,2,i));
  var u=new UnitState{Id=500,Defender=PlayerSlot.Zero,Kind=2,Hp=100000,Distance=0};s.State.Units.Add(u);
  for(int tick=0;tick<1200;tick++){s.Step();Check(u.SlowPermille>=600,"slow below floor");}
  Check(u.Distance>=36,"permanent frost stopped movement");
 });
 Run("AllPaidUnitsEventuallyLeaveFullFieldQueue",()=>{
  var c=GameContent.CreateDefault(new GameRules(startGold:10000,maxUnitsPerField:1,maxSpawnOrdersPerPlayer:4));var s=Start(c);
  s.State.Units.Add(new UnitState{Id=500,Defender=PlayerSlot.One,Kind=2,Hp=220});
  for(int k=0;k<4;k++)Check(s.Execute(PlayerSlot.Zero,Cmd(s,PlayerSlot.Zero,CommandType.SendUnit,k)).Accepted,"queue purchase");int expected=0;foreach(var u in c.Units)expected+=u.Count;
  for(int i=0;i<10;i++)s.Step();Check(s.State.SpawnOrders.Count==4,"full field silently lost order");s.State.Units.Clear();int spawned=0;
  for(int i=0;i<200&&s.State.SpawnOrders.Count>0;i++){s.Step();spawned+=s.State.Units.Count;s.State.Units.Clear();}
  Check(spawned==expected&&s.State.SpawnOrders.Count==0,"paid unit lost or duplicated");
 });
 Run("SameTickFinishCannotBeChangedByLaterIntent",()=>{
  var s=Start(GameContent.CreateDefault());s.State.Players[0].Hp=1;s.State.Players[1].Hp=1;
  for(int p=0;p<2;p++)s.State.Units.Add(new UnitState{Id=500+p,Defender=(PlayerSlot)p,Kind=2,Hp=220,Distance=s.Content.Map.Length});s.Step();
  Check(s.State.Outcome==MatchOutcome.Draw,"not a draw");string before=Snapshot(s.State);
  s.Execute(PlayerSlot.Zero,Cmd(s,PlayerSlot.Zero,CommandType.SendUnit,2));s.Abort();s.Step();Check(Snapshot(s.State)==before,"late action changed finished state");
 });
 Console.WriteLine("INDEPENDENT_REVIEW total=6 failures="+failures);return failures==0?0:1;
 }
}
```
