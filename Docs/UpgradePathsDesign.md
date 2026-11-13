# Wählbare Turm-Upgrades: Schaden und Reichweite

Stand: 10. November 2026. Direkter Nutzerauftrag im Gameplay-Feedback-Chat: verschiedene Upgradearten, etwa Reichweite und Schaden, entwickeln; ausgebauten Türmen deutlich kräftigere, attraktivere Formen geben und den Ausbau mit dem Hauptchat abstimmen.

**Nachtrag zum später freigegebenen v1.7-Fenster:** Die gemeinsamen `TowerUpgradeRules` wurden vom Gameplay-Owner angelegt; die Botanbindung verwendet jetzt ihre echten Quotes für Damage/Range/Income. Bot-Integration und Desktop-Regressionsprüfung sind abgeschlossen, einschließlich reiner und gemischter Pfade. Nachweise und begrenzter 0.3-Vergleich stehen in `Docs/GameplayFeedbackPatch.md` und `Tools/BotFeedbackHarness/Results/v1.7/`. Damit ist die folgende ursprüngliche Freeze-/Sandbox-Beschreibung historisch; sie bedeutet keine weiterhin geltende Sperre nach dem ausdrücklichen Lead-GO. Modell-, UI-, Wire- und Build-Abnahme erfolgen separat durch den Hauptchat und werden hier nicht aus Botprüfungen abgeleitet.

**Abgestimmte Empfehlung:** Zwei bezahlte Ausbauten insgesamt je Kampfturm. Bei jedem Kauf entscheidet der Spieler zwischen **Schaden** und **Reichweite**. Damit entstehen Schaden II, Reichweite II oder der Mischpfad Schaden I + Reichweite I. Der Hauptchat hat diesen Umfang ausdrücklich gegenüber zwei plus zwei Ausbauten bevorzugt. Bestehende Stufenpreise bleiben erhalten. Minen behalten ihren linearen Ertragspfad.

**Aktueller Status:** Konkreter Folgeentwurf und isolierter ausführbarer Regelprototyp unter `Tools/UpgradeDesignSandbox`. Während der Sicherung der getesteten Version 0.3 bleiben vorhandene Runtime-Dateien und Assets unverändert. Der Hauptchat gibt das Integrationsfenster nach APK/Commit frei. Die Werte unten sind prüfbare Kandidaten für diesen Folgeausbau; sie sind noch nicht Bestandteil der gespielten Vorschau oder eines geprüften Multiplayer-Builds.

## 1. Verhalten und begrenzte Balanceänderung

Eine Aufrüstung verbessert ausschließlich den gewählten Wert. Schaden verändert den Schaden pro Treffer; Reichweite verändert den Zielradius. In diesem Kandidaten bleibt der Schusstakt auf dem Basiswert des Turms. Splashradius, Rüstungsdurchdringung, Froststärke, Verlangsamungsdauer und Fußabdruck bleiben klassenabhängig unverändert. Ein Upgrade erzeugt keinen sofortigen Zusatzschuss und setzt eine laufende Waffenabklingzeit nicht zurück.

Das unterscheidet sich bewusst vom bisherigen Paket-Upgrade, das Schaden, Reichweite und Schusstakt gleichzeitig verbessert. Der reine Schadenspfad erreicht dieselben bisherigen Schadenswerte, bekommt aber keine versteckten Reichweiten-/Taktboni. Der Reichweitenkandidat beträgt +20 bzw. +40 Prozent des **Basisradius**, ganzzahlig aufgerundet. Die zweite Reichweitenstufe ist damit nicht nochmals +20 Prozent des bereits erhöhten Radius.

| Klasse | Schaden Basis / I / II | Reichweite Basis / I / II | Schusstakt in Ticks | Erster / zweiter Ausbau |
| --- | --- | --- | --- | --- |
| Schnellfeuer | 6 / 9 / 14 | 150 / 180 / 210 | 8 | 55 / 85 G |
| Kanone | 16 / 26 / 40 | 155 / 186 / 217 | 32 | 85 / 130 G |
| Frost | 4 / 6 / 8 | 155 / 186 / 217 | 20 | 60 / 95 G |
| Panzerbrecher | 40 / 64 / 96 | 190 / 228 / 266 | 42 | 85 / 130 G |
| Goldmine | kein Angriff | keine Waffenreichweite | kein Schuss | 100 / 160 G; Ertrag 1 → 2 → 3 G/s |

Reichweitenwerte sind logische Spielkoordinaten, keine erfundenen Meter. Im Produkt reichen ein Vorher/Nachher-Wert und der tatsächliche Reichweitenring; der Spieler braucht keine Erklärung der internen Koordinaten.

**Beispiel Schnellfeuer:** Nach dem Bau stehen „Schaden 6 → 9 · 55 G“ und „Reichweite 150 → 180 · 55 G“ zur Wahl. Nach Schaden I kosten beide verbleibenden Optionen 85 G: Schaden 9 → 14 oder Reichweite 150 → 180. Nach Reichweite I stehen Schaden 6 → 9 oder Reichweite 180 → 210 für 85 G zur Wahl. Nach dem zweiten Kauf sind beide Pfade ausgeschöpft.

Der Preis richtet sich nach der Zahl bereits gekaufter Ausbauten, nicht nach der gewählten Richtung. Schaden–Reichweite und Reichweite–Schaden ergeben dieselben Endwerte und dieselbe Gesamtinvestition. Verkauf bleibt 65 Prozent der gesamten Investition, abgerundet. Position und Grundfläche ändern sich durch ein Upgrade nicht.

Die bestehende defensive Stärke ist ein wesentlicher Prüfpunkt: Der vorherige 96-Partien-Botvergleich endete gegen PureDefense in 24 Zeitlimit-Remis. Deshalb gibt es hier keine zusätzlichen dritten/vierten Ausbauten, keine Erhöhung des Frost-Slows und keinen beiläufigen Preisnachlass. Ob +20/+40 Prozent Reichweite zu stark ist, wird nach Integration gezielt gemessen; die alten Balanceergebnisse werden nicht als Nachweis dieses neuen Systems bezeichnet.

## 2. Bedienung

Der Kontext eines ausgewählten Kampfturms zeigt zwei gleichwertige, ausreichend große Aktionen:

- **Schaden** mit eindeutiger Projektil-/Geschützmarke, aktuellem und folgendem Schaden pro Treffer sowie Preis.
- **Reichweite** mit Fadenkreuz-/Ringmarke, aktuellem und folgendem Radius sowie Preis.

Darüber steht etwa „Schnellfeuer · 1 von 2 Ausbauten · Schaden I“. Der Mischpfad heißt „Schaden I · Reichweite I“. Keine Anzeige „Stufe 2 von 5“ und keine zwei voneinander unabhängigen 0/2-Fortschrittsanzeigen, die vier Käufe suggerieren. Bei einer Mine erscheint nur „Ertrag 1 → 2 G/s · 100 G“.

Bei fehlendem Gold bleibt die Wirkung lesbar; die Aktion erklärt die fehlende Summe und bucht nichts. Bei voller Ausbaustufe steht „Voll ausgebaut“ mit den gekauften Richtungen. Ausstehende Upgrades eines Turms werden bis zur Befehlsauflösung gesperrt, damit ein schneller zweiter Tap nicht zu einem unklaren anderen Preis führt. Verkauf und Schließen bleiben klar getrennte Aktionen. Der Host bleibt maßgeblich.

Die bestehende Reichweitenanzeige verwendet die tatsächliche gewählte Reichweitenstufe. Eine Vorschau des nächsten Radius darf als unterscheidbarer zusätzlicher Ring erscheinen; bestätigte aktuelle Reichweite und Kandidat werden nicht verwechselt. Auf Touch wird kein Hover vorausgesetzt. Nach erfolgreichem Kauf genügt ein kurzer Ausbauimpuls mit sichtbarer neuer Silhouette; ReducedEffects zeigt sofort das neue Modell ohne dekorativen Impuls.

## 3. Sichtbare Modelländerungen

Die vorhandenen vier Klassen bleiben erkennbar. Schaden wirkt **breiter, kräftiger und stärker abgestützt**; Reichweite wirkt **länger, gezielter und mit klarer Optik ausgestattet**. Farbe unterstützt die Form, ersetzt sie aber nicht. Es werden eigene prozedurale Erweiterungen der vorhandenen Modelle verwendet; dafür ist kein weiterer Assetdownload notwendig.

| Klasse | Schaden I / II | Reichweite I / II |
| --- | --- | --- |
| Schnellfeuer | Verstärkter Laufmantel und seitliche Munitionszufuhr / kräftiger Block mit Kühlrippen und gepanzerten Schultern | Sichtbare obere Zieloptik und längere Läufe / ausgeprägte Optik mit Distanzmesser und schlankem Langlaufprofil |
| Kanone | Breitere Rohrmanschette und Rückstoßstreben / massiver Verschluss, stärkere Stützen und schwere Mündung | Längeres Rohr mit Seitenvisier / deutliches Langrohr mit Zielgerät; Mündung bleibt schlanker als beim Schadenspfad |
| Frost | Zusätzliche seitliche Kristallkerne / größerer Hauptkern und kräftige seitliche Kristalle | Erhöhter Fokus-Ring / zweiter höherer Ring und aufrechter Fokusprisma-Aufbau; keine Behauptung stärkeren Slows |
| Panzerbrecher | Breitere Schienen und zusätzliche Energieblöcke / verstärkte Doppel-Schienen mit dicken seitlichen Kondensatoren | Langgezogene Schienen und Zieloptik / große Zieloptik mit klar verlängertem Schienenprofil |

Der Mischpfad kombiniert jeweils die erste Erweiterung beider Richtungen. Schaden II darf nicht wie Reichweite II aussehen; ein zehnprozentiges gleichmäßiges Skalieren oder ein reiner Farbwechsel genügt ausdrücklich nicht. Die Goldmine kann ihre vorhandenen zusätzlichen Erzstücke und den ausgebauten Fördermechanismus behalten.

Basis und Kopf bleiben die vorhandenen zwei gepoolten Renderer pro Turm. Alle Varianten werden beim Erzeugen der `DioramaResources` einmal aufgebaut und danach als `sharedMesh` gewählt. Keine zusätzlichen Update-Schleifen, Materialinstanzen pro Turm oder Meshneuerzeugung bei jedem Snapshot. Türme behalten ihre Bodenkontaktfläche; größere Aufbauten dürfen keine weiteren Bauflächen beanspruchen. Kamera-/WorldBounds werden gegen den höchsten Aufbau geprüft.

Für Kampftürme sind sechs Varianten nötig:

| DamageLevel | RangeLevel | Level | VisualVariant |
| --- | --- | --- | --- |
| 0 | 0 | 0 | 0 |
| 1 | 0 | 1 | 1 |
| 0 | 1 | 1 | 2 |
| 2 | 0 | 2 | 3 |
| 1 | 1 | 2 | 4 |
| 0 | 2 | 2 | 5 |

Zuordnung: `Level * (Level + 1) / 2 + RangeLevel`. Bei der Mine bleibt der visuelle Index gleich ihrer Ertragsstufe 0–2. `BoardView` muss den visuellen Index zusätzlich zur Klasse cachen; allein `Level` reicht nicht, weil mehrere Ausrichtungen dieselbe Gesamtstufe haben. Das bisherige Mesharray kann auf `[towerCount, 6]` erweitert oder nach Klasse unterschiedlich dimensioniert werden; Minen benötigen nur die ersten drei Einträge.

## 4. Verbindlicher API-Vorschlag für das Integrationsfenster

Neue zentrale Regeln, beispielsweise `TowerUpgradeRules`, liefern Statistiken, Kaufangebot und visuellen Index. UI, Bot, Simulation und Snapshotprüfung lesen dieselben Funktionen. Keine parallelen Formeln für Reichweite oder Preis in der Oberfläche.

```csharp
public enum TowerUpgradePath : byte { Damage = 0, Range = 1, Income = 2 }

// Ergänzungen zu TowerState; bestehende Felder bleiben erhalten.
public int DamageLevel, RangeLevel;
// Kampf: Level == DamageLevel + RangeLevel, insgesamt 0..2.
// Mine: Level ist Ertragsstufe 0..2; DamageLevel == RangeLevel == 0.

// Vorschlag für die zentrale Abfrage, ohne Zustandsmutation:
UpgradeQuote Quote(GameContent content, TowerState tower, TowerUpgradePath path);
TowerStats Stats(GameContent content, TowerState tower);
int VisualVariant(TowerState tower);
```

`UpgradeQuote` enthält Fehlercode, Preis, aktuelle und folgende Werte sowie erwartete Gesamt-/Pfadstufen. Die reine Quote ist unabhängig vom verfügbaren Gold, damit UI und Bot auch einen noch anzusparenden Kauf korrekt darstellen. Die verbindliche Simulation prüft danach Eigentum, Phase, Session und Gold und verbucht Preis und Stufe atomar.

**Kaufbefehl:** vorhandener `CommandType.UpgradeTower`, `Target = TowerState.Id`, `Kind = (int)TowerUpgradePath`. Die ursprünglichen Build-/Send-Bedeutungen von `Kind` bleiben unverändert. `PositionX/PositionY` werden durch Upgrades nicht als neue Position übernommen. Kampfgebäude lehnen Income ab; Minen lehnen Damage/Range ab. Ungültige Pfadwerte werden abgelehnt. Eine wiederholte identische Sequenz gibt denselben Beleg zurück; dieselbe Sequenz mit anderem Pfad darf keinen zweiten Kauf ausführen (`SameRequest` berücksichtigt bereits `Kind`).

**Simulation:** `TowerDamage` verwendet `DamageLevel`, `TowerRange` verwendet `RangeLevel`, `TowerInterval` den Basis-Schusstakt. `TowerIncome` bleibt für Minen linear. Akzeptiertes Upgrade erhöht genau den gewählten Pfad und die Gesamtstufe um eins, erhöht `Invested` exakt um den Preis und erhält Position, Site-ID, Entity-ID und Restabklingzeit. Ein dritter Kauf liefert `MaxLevel`; ungültige oder unbezahlbare Käufe ändern keinerlei Matchzustand.

**Content:** Bestehende zwei `UpgradeDefinition`-Einträge und Preise können erhalten bleiben. Ihre `Damage`-Felder werden als Werte des jeweiligen Schadenspfadlevels interpretiert, ihre `Range`-Felder als Werte des jeweiligen Reichweitenpfadlevels. Die oben genannten Radiuskandidaten werden bei Migration explizit in diesen bereits gehashten Inhaltsfeldern gespeichert; kein versteckter ungehashter UI-Prozentbonus. `FireIntervalTicks` der Combat-Einträge wird für einen konsistenten Export auf den Basiswert gesetzt. Minendaten bleiben unverändert. Alle direkten Zugriffe auf `Upgrades[tower.Level]` müssen auf „Preis nach Gesamtstufe“ bzw. „Wert nach Pfadstufe“ geprüft werden.

## 5. Snapshot, Hash und Versionsgrenzen

- `TowerState.DamageLevel` und `.RangeLevel` werden als begrenzte Werte im Snapshot übertragen, zum Beispiel zwei zusätzliche Bytes je Turm. Beide sind 0–2. Bei Kampfgebäuden muss ihre Summe exakt `Level` entsprechen; bei Minen müssen beide null sein. Diese Invarianten werden vor Übernahme eines Snapshots geprüft.
- Die erwartete Investition bleibt wegen gleicher Stufenpreise `Grundpreis + erste Level Upgradepreise`. Refundprüfungen brauchen keine Kaufhistorie. Falsche Investitionen oder unmögliche Kombinationen werden weiterhin abgewiesen.
- Wire-Version nach derzeitiger Version 3 auf die nächste freie Version erhöhen, vorgeschlagen 4. Simulationsregelversion nach v1.6 auf die nächste freie Version erhöhen, vorgeschlagen v1.7. Alten und neuen Build nicht gemeinsam in eine Partie lassen; vorhandener Versions-/Content-Abgleich liefert die verständliche Ablehnung.
- Contentschema nach derzeitiger Version 4 erhöhen, vorgeschlagen 5, und die Testdatenmigration zentral ausführen. Neue explizite Radiuswerte, Intervallfelder und die Regelversion verändern den `ContentHash`. Änderungen an Meshformen allein brauchen keinen Simulationshash.
- Es gibt keine Live-Migration laufender v0.3-Partien. Das getestete Artefakt wird gesichert; der neue Build startet neue Sessions. Alte Testbelege bleiben mit Version und Hash erhalten.

## 6. Asynchrone Anzeige, Bot und bestehende Tests

`PresentationCommandContext.Kind` wird aktuell bei Upgrades zum Gebäudetyp umgeschrieben. Deshalb den gewünschten Upgradepfad separat speichern, etwa `UpgradePath`, plus Previous/ExpectedDamageLevel und Previous/ExpectedRangeLevel. `ExpectedLevel` allein verhindert keine falsche Darstellung zwischen Schaden II und Mischpfad. ACK und Snapshot dürfen weiterhin in beliebiger Reihenfolge eintreffen. Der bestätigte Effekt benötigt dieselbe Entity, Position, Site-ID, Klasse und vollständige erwartete Pfadkombination. Ein neuerer Zustand darf keinen alten Effekt nachholen.

Der Bot behält die budgetierten Angriffe, die Investitionsreserve und seine persistenten Bausparziele. Minen kaufen explizit Income. Kampftürme wählen deterministisch zwischen den legalen Angeboten: Reichweite nur, wenn sie tatsächlich zusätzliche Wegabdeckung erschließt; Schaden bevorzugen bei relevanten lebenden Zielen im aktuellen Radius. Beide Richtungen müssen in gezielten Botfixtures erreichbar sein. Die genaue Heuristik wird erst im Integrationsfenster implementiert und separat gemessen; das jetzige Botverhalten bleibt eingefroren.

Die alten linearen Upgrade-Tests werden als Baseline erhalten oder versionsbezogen ersetzt; eine Änderung erwarteter Zahlen ohne dokumentierte neue Semantik ist kein Testnachweis. Insbesondere müssen Wire-Fixtures, PlayMode-Tutorial, Upgrade-Feedbacktests, Contentmigration und Bot-Fingerprints die neuen Pfadfelder aufnehmen.

## 7. Begrenzte Abnahme

1. Alle vier Kaufreihenfolgen je Kampfturm (DD, DR, RD, RR), Minenertrag I/II, unveränderte Stufenpreise, gleiche Misch-Endwerte, dritte Käufe abgelehnt. Quote und tatsächliche Wirkung stimmen überein.
2. Schaden verändert keine Reichweite/Feuerrate, Reichweite keinen Schaden/Feuerrate; Frostslow, Splash und Panzerbrechung bleiben konstant. Bestehende Abklingzeit, Standort und Footprint bleiben erhalten. Verkauf rechnet sämtliche tatsächlich bezahlten Preise korrekt ab.
3. Falscher Besitzer, falsche Phase, fehlendes Gold, ungültiger Pfad, doppelte Sequenz und verändert wiederholte Sequenz haben keine unerlaubte Mutation. Beide Pfade benötigen genau eine Zahlung.
4. Snapshot-Roundtrip aller sechs Kampfkombinationen und drei Minenstufen; unmögliche Summen/Minenpfade abgewiesen. Alte Wire-Version und alter Hash werden abgefangen. Host-/Clientzustand stimmen überein.
5. UI mit tatsächlichen zwei Buttons, Vorher/Nachher-Statistik, Preisen, fehlendem Gold und Pending-Sperre; Tutorial auf beide Kampfrichtungen anwendbar. ACK-vor-Snapshot und Snapshot-vor-ACK, Disconnect und Revanche ohne falsche Effekte.
6. Kontaktbogen der vier Klassen mit sechs Varianten im tatsächlichen Renderer; zusätzlich Handy-Spielmaßstab. Formunterschiede ohne Text/Farbe erkennbar, keine optische Verwechslung mit Truppen, keine Abschneidung. Mesh-/Rendererbudget und ReducedEffects prüfen.
7. Begrenzter erneuter Botvergleich gegen v0.3 sowie gezielte Range-/Damage-Bots. Prüfen, ob längere Reichweite mehr Abdeckung oder vor allem zusätzliche Zeitlimit-Remis erzeugt. Kein automatischer Preis-/HP-Umbau im selben Schritt.

## 8. Aufgabenübergabe

Der Hauptchat entscheidet das Integrationsfenster und koordiniert Core/Content/Network/UI. Dieser Feedback-Chat liefert Entwurf, isolierten Regelprototyp und dessen Nachweise; nach Freigabe kann er gemeinsame Upgrade-Regeln, Botanbindung oder die eigene prozedurale Modellhilfe übernehmen. Die genaue Dateizuteilung erfolgt vor Runtime-Edits. Die derzeit gespielte v0.3 bleibt unangetastet.

Der isolierte Prototyp verwendet den eingefrorenen Content als Eingabe und berechnet die Kandidaten nur in eigenen Typen. `Results/states.csv` enthält die vorgesehenen Zustände und Werte. Seine Prüfungen sind ein Nachweis der Kandidatenarithmetik, Kosten- und Pfadlogik; sie ersetzen weder Unity-Sichtprüfung noch Integration, Netzwerk- oder Balanceprüfung.

Prüfergebnis: **234 Assertions bestanden**, 27 Zustandszeilen (vier Kampfklassen mit jeweils sechs Kombinationen, Mine mit drei Stufen), Produktions-Content unverändert. `Results/checks.log` und `Results/verification.json` dokumentieren den isolierten Lauf und seine Grenzen.

Reproduktion: `dotnet run --project Tools/UpgradeDesignSandbox -c Release`.
