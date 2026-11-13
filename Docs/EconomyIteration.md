# Economy- und Queue-Iteration, 10. November 2026

## Festgelegter Beta-Kandidat

Der Lead hat nach den folgenden Messungen **5 Basisgold/s, Einheiten-Tempo 104/184/59/93 und Kill-Bounties 4/5/14/2** für Läufer/Sprinter/Tank/Schwarm festgelegt. Die Tempi sind die bisherigen 65/115/37/58 × 1,6, kaufmännisch vom Nullpunkt weg gerundet. Preise, HP, Schaden, Klassenabstände und Minenkosten bleiben unverändert. Die Werte sind zusammen mit der freien Platzierung in v1.6 integriert. Es handelt sich um einen spielbaren Beta-Kandidaten, nicht um nachgewiesen abschließend ausbalancierte Werte.

4 Basisgold allein senkte den Angriffsdruck, ohne defensive Timeouts zu lösen. 5 Gold plus schnellerer Bewegung folgt dem Wunsch nach weniger passivem Einkommen und gleicht einen Teil der längeren neuen Routen aus. Kein Truppenkauf erzeugt passives Einkommen; Kill-Gold geht ausschließlich an den Verteidiger der getöteten Einheit. Es wurde keine manuell ausgelöste Wellenmechanik eingeführt.

## Methode und getrennte Datenstände

Jeder der fünf nachfolgenden Datensätze enthält 2.160 vollständige Partien: 3 Einkommensraten (6/5/4), 3 Karten, 15 ungeordnete Paarungen einschließlich Selbstspiel zwischen RunnerRush, TankRush, PureDefense, Balanced und Economy, 8 Seeds, jeweils beide Seiten. Insgesamt sind 10.800 Partien gespeichert. Der Seitenwechsel erhält die einem Bot zugeordneten Seed-Streams; Rest-HP, Dauer, Geld-/Ausgabenverlauf und Queue-Metriken müssen danach exakt übereinstimmen. Das ist eine Symmetrieprüfung, keine unabhängige Verdopplung der Seed-Stichprobe.

| Verzeichnis unter `Tools/CoreHarness/Results/EconomyIteration/` | Regeln / Geometrie / Bot |
|---|---|
| `v1.2` | Kauf-Cooldown, Queue 8, alte 1000×600-Karten mit 12 Slots, Policy aus Commit `2d41be6`, keine Bounties |
| `v1.4-legacy-maps` | Sofortkauf, Queue 80, Klassenabstand beim Ausmarsch; exakt dieselben alten Karten und dieselbe Policy |
| `v1.5-legacy-bounty` | Wie vorige Zeile, zusätzlich Bounties 4/5/14/2; ausschließlich isolierte Bounty-Probe |
| `v1.5-large-bounty` | Neue 1600×960-Karten, 30 Kampf-/2 Minenslots, Bounties, überarbeiteter Slotkarten-Bot vor Mine-Gate-Reparatur, altes Tempo |
| `v1.5-large-bounty-speed160` | Exakt voriger Stand, ausschließlich Tempo ×1,6 |

Die letzten beiden Tabellen gelten ausdrücklich für **v1.5 mit festen Slots**, nicht für die danach angeforderte freie Platzierung. Die Bot-Policy der letzten beiden Läufe ist als `slot-map-bot-before-mine-gate-fix.cs.txt` gesichert (SHA-256 `4550832fb1ace756d0d8808f1b444f70cf16f98b89b549fb1e98497ae1381ca2`). Die Legacy-Policy steht separat in `EconomyProbeBot.cs`; Legacy-Geometrie steht in `LegacyMapFixture.cs`. Produktions-Bot und neue Karten werden daher nicht rückwirkend in alte Vergleichsläufe gemischt.

`content.csv` enthält Content-Hashes, RulesVersion, Seeds und bei späteren Läufen Policy-/Tempo-Metadaten. `matches.csv` enthält beide Spieler jedes Matches; `summary.csv` aggregiert Strategie/Gegner/Karte; `milestones.csv` enthält Gold, erzeugtes Gold, Ausgaben und Angriffsaufträge bei 30/60/120/240 Sekunden. Endet ein Match vorher, wird kein späterer Messpunkt erfunden.

Geldmittel ist das arithmetische Mittel aller 20-Hz-Stichproben vom ersten Spieltick bis zum Ende. Peak enthält das Startgold. Ausgaben werden aus tatsächlich akzeptierten Aktionen zerlegt in Angriffe, Verteidigung und Minen. Die Tick-Bilanz prüft unabhängig `Start + Basis + Minen + Tötungsbelohnungen − Deckelverluste − Ausgaben = Endgold`. Der Harness beobachtet Todesfälle über erhaltene Unit-Referenzen; sofort beim Spawn getötete Einheiten erkennt er am dekrementierten bezahlten Auftrag. Bounty-/Deckel-/Leak-Regressionsfälle prüfen diese Regeln zusätzlich direkt.

Das sind deterministische Desktop-Regeltests. Sie messen weder menschliche Tippfrequenz oder Spielspaß noch Unity-Frametimes oder Android-Verhalten. Der 480-Sekunden-Abschluss ist ein regulärer Timeout nach Rest-HP, nicht automatisch ein Remis.

## Sofortkauf ohne Änderung des Angriffsabstands

Ein Tap bezahlt sofort eine vollständige Gruppe; derselbe Typ darf im selben Tick erneut gekauft werden. Beim ersten tatsächlichen Spawn beginnt erst der Klassenabstand. Der FIFO-Kopf wartet nötigenfalls darauf; eine später gekaufte andere Klasse überholt ihn nicht. Innerhalb eines Schwarms und global pro Absender bleibt der Abstand bei 4 Ticks. Ein volles Feld verbraucht keine bezahlten Einheiten und startet keinen noch nicht begonnenen Klassen-Timer. Maximal 80 bezahlte Aufträge je Spieler; darüber wird vor Goldabbuchung abgewiesen. Der bestehende Limiter von 40 neuen Befehlen pro Sekunde und der idempotente Antwortcache bleiben aktiv.

Ein Hold-Frontend hätte lediglich wiederholte Käufe nach Ablauf des Kauf-Cooldowns ausgelöst. Es hätte das bestätigte Anliegen, den vorhandenen Goldbetrag durch rasche einzelne Käufe zu binden, nicht vollständig erfüllt. Die jetzt gewählte bezahlte FIFO erfüllt dieses Anliegen ohne zusätzliche Freigabe-/Wellensteuerung.

| Klasse | Preis | Gruppenabstand | Dauerhafter Ausmarschwert |
|---|---:|---:|---:|
| Läufer | 18 | 1,0 s | 18,00 G/s |
| Sprinter | 24 | 1,5 s | 16,00 G/s |
| Tank | 65 | 3,5 s | 18,57 G/s |
| Schwarm ×4 | 38 | 2,0 s | 19,00 G/s |

Diese Einzelklassenraten liegen oberhalb von Basis- plus maximalem Mineneinkommen. Bei einer gemischten FIFO sind sie wegen gegenseitigen Wartens keine addierbare Garantie. Kauf und Ausmarsch sind getrennt: Gold kann sofort gebunden werden, ohne sofort alle bezahlten Einheiten gleichzeitig erscheinen zu lassen.

## Legacy-Vergleich: Geldangebot und Ausgaben

Alle Zahlen gegen Balanced, über drei alte Karten aggregiert, je 48 Partien. S/N/R steht für Sieg/Niederlage/Remis aus Sicht der genannten Strategie. Selbstspiel zeigt nur den A-Seed-Stream und ist daher keine 50:50-Erwartungsprüfung.

| FIFO-Einkommen | Strategie | S/N/R | Dauer s | Goldmittel | Peak | Endgold | Ausgaben G/s | Angriffsbudget G |
|---:|---|---|---:|---:|---:|---:|---:|---:|
| 6 | RunnerRush | 0/48/0 | 184,70 | 10,04 | 180,00 | 8,75 | 6,92 | 1276,50 |
| 6 | TankRush | 0/48/0 | 178,55 | 34,06 | 180,00 | 32,21 | 6,81 | 1216,04 |
| 6 | PureDefense | 0/8/40 | 480,00 | 49,32 | 180,00 | 115,00 | 6,13 | 0,00 |
| 6 | Balanced | 20/12/16 | 480,00 | 25,73 | 180,00 | 18,88 | 7,17 | 2537,38 |
| 6 | Economy | 12/20/16 | 480,00 | 34,62 | 180,00 | 21,58 | 10,39 | 3488,38 |
| 5 | RunnerRush | 0/48/0 | 209,44 | 11,82 | 180,00 | 11,29 | 5,80 | 1213,50 |
| 5 | TankRush | 4/44/0 | 200,60 | 31,63 | 180,00 | 21,67 | 5,78 | 1159,17 |
| 5 | PureDefense | 0/6/42 | 480,00 | 45,08 | 180,00 | 75,00 | 5,22 | 0,00 |
| 5 | Balanced | 24/10/14 | 480,00 | 26,91 | 180,00 | 14,25 | 6,17 | 2054,29 |
| 5 | Economy | 12/20/16 | 480,00 | 35,83 | 180,00 | 23,42 | 8,98 | 2872,08 |
| 4 | RunnerRush | 0/48/0 | 263,79 | 10,60 | 180,00 | 7,83 | 4,65 | 1225,50 |
| 4 | TankRush | 4/44/0 | 250,03 | 33,33 | 180,00 | 35,08 | 4,57 | 1142,92 |
| 4 | PureDefense | 0/12/36 | 480,00 | 44,38 | 180,00 | 35,00 | 4,30 | 0,00 |
| 4 | Balanced | 26/8/14 | 480,00 | 28,55 | 180,00 | 15,33 | 5,13 | 1564,04 |
| 4 | Economy | 8/26/14 | 480,00 | 37,24 | 180,00 | 17,75 | 8,39 | 2516,12 |

Bei 6 Gold/s senkt der Sofortkauf das Geldmittel von RunnerRush von 15,62 auf 10,04 Gold. Balanced blieb praktisch gleich (25,64 → 25,73). In diesen aktiven Bots stieg der Peak gegen Balanced nie über die 180 Startgold. Die zuvor vermutete große Bank ist hier nicht nachweisbar; Bedienung und menschliche Kaufpausen bleiben eigenständige Faktoren.

## Größere Slotkarten, Kill-Gold und Tempo

Je Zeile 48 Partien gegen Balanced. „Getroffen“ zählt Partien mit mindestens einem eigenen HP-Verlust; „Timeout“ zählt reguläre Acht-Minuten-Enden. Erster Schaden wird nur unter tatsächlich getroffenen Basen gemittelt; dieser bedingte Mittelwert darf bei geänderter Trefferquote nicht allein verglichen werden.

| Tempo | Basis G/s | Strategie | S/N/R | Angriffsbefehle | Kill-Gold | Rest-HP | Getroffen | Timeout | Dauer s |
|---:|---:|---|---|---:|---:|---:|---:|---:|---:|
| 100% | 6 | Balanced | 16/6/26 | 162,33 | 695,54 | 28,38 | 22/48 | 48/48 | 480,00 |
| 100% | 6 | TankRush | 0/48/0 | 17,38 | 0,00 | 0,00 | 48/48 | 0/48 | 164,12 |
| 100% | 6 | PureDefense | 0/0/48 | 0,00 | 642,04 | 30,00 | 0/48 | 48/48 | 480,00 |
| 100% | 5 | Balanced | 24/12/12 | 141,54 | 586,71 | 28,75 | 18/48 | 48/48 | 480,00 |
| 100% | 5 | TankRush | 0/48/0 | 16,50 | 0,00 | 0,00 | 48/48 | 0/48 | 185,13 |
| 100% | 5 | PureDefense | 0/0/48 | 0,00 | 552,29 | 30,00 | 0/48 | 48/48 | 480,00 |
| 100% | 4 | Balanced | 22/10/16 | 124,08 | 503,83 | 28,62 | 22/48 | 48/48 | 480,00 |
| 100% | 4 | TankRush | 0/48/0 | 15,50 | 0,00 | 0,00 | 48/48 | 0/48 | 215,93 |
| 100% | 4 | PureDefense | 0/0/48 | 0,00 | 441,42 | 30,00 | 0/48 | 48/48 | 480,00 |
| 160% | 6 | Balanced | 20/14/14 | 162,58 | 681,58 | 21,96 | 44/48 | 48/48 | 480,00 |
| 160% | 6 | TankRush | 0/48/0 | 15,08 | 0,00 | 0,00 | 48/48 | 0/48 | 139,53 |
| 160% | 6 | PureDefense | 0/8/40 | 0,00 | 642,79 | 29,50 | 8/48 | 48/48 | 480,00 |
| 160% | 5 | Balanced | 30/10/8 | 144,71 | 609,38 | 26,42 | 34/48 | 48/48 | 480,00 |
| 160% | 5 | TankRush | 6/42/0 | 14,58 | 0,00 | 0,12 | 48/48 | 0/48 | 161,11 |
| 160% | 5 | PureDefense | 0/6/42 | 0,00 | 551,88 | 29,62 | 6/48 | 48/48 | 480,00 |
| 160% | 4 | Balanced | 20/16/12 | 125,08 | 507,29 | 26,54 | 38/48 | 48/48 | 480,00 |
| 160% | 4 | TankRush | 22/26/0 | 14,00 | 0,00 | 0,71 | 48/48 | 0/48 | 189,40 |
| 160% | 4 | PureDefense | 0/10/38 | 0,00 | 439,33 | 29,38 | 10/48 | 48/48 | 480,00 |

Der Tempo-Kandidat erhöht bei 6 Gold/s die Zahl getroffener Balanced-Basen von 22 auf 44 unter 48 Partien und reduziert die mittleren Rest-HP von 28,38 auf 21,96. PureDefense erzwingt dennoch 40/48 Remis. 4 Gold/s ohne Tempoänderung reduziert Angriffsaufträge um rund 24 Prozent, während sämtliche Balanced- und PureDefense-Partien weiterhin bis 480 Sekunden laufen. Der festgelegte Kompromiss 5 Gold/s + Tempo160 hat 6/48 TankRush-Siege gegen Balanced; die reine Tankstrategie wird in dieser Stichprobe nicht dominant. Die weiterhin zahlreichen defensiven Timeouts bleiben offen.

## Gold bei 30/60/120/240 Sekunden im gewählten Slotkarten-Kandidaten

Der spätere v1.6-Aufbau wird separat geprüft; diese Werte stammen aus v1.5 mit 5 Gold/s, Tempo160 und dem oben gesicherten Botstand.

| Strategie gegen Balanced | Sekunde | Gold | erzeugt seit Start | ausgegeben seit Start | Angriffsaufträge seit Start |
|---|---:|---:|---:|---:|---:|
| Balanced | 30 | 96,21 | 158,21 | 242,00 | 6,12 |
| Balanced | 60 | 23,58 | 334,33 | 490,75 | 11,12 |
| Balanced | 120 | 24,75 | 694,71 | 849,96 | 24,83 |
| Balanced | 240 | 32,25 | 1531,58 | 1679,33 | 63,96 |
| Economy | 30 | 103,58 | 157,08 | 233,50 | 6,25 |
| Economy | 60 | 39,04 | 329,62 | 470,58 | 10,96 |
| Economy | 120 | 26,83 | 689,00 | 842,17 | 23,83 |
| Economy | 240 | 33,67 | 1527,46 | 1673,79 | 63,38 |

## Minen-Amortisation

| Mine | Gesamtinvestition | Ertrag/s | Brutto-Amortisation bei sofort vorhandenem Level | Verkaufserlös | Nettoeinsatz nach Verkauf / Ertrag |
|---|---:|---:|---:|---:|---:|
| Stufe 0 | 140 | 1 | 140,00 s | 91 | 49,00 s |
| Stufe 1 | 240 | 2 | 120,00 s | 156 | 42,00 s |
| Stufe 2 | 400 | 3 | 133,33 s | 260 | 46,67 s |

Das erste Upgrade zahlt seinen zusätzlichen Preis von 100 Gold durch das zusätzliche 1 Gold/s nach 100 Sekunden zurück; das zweite benötigt 160 Sekunden. Die Tabelle unterstellt sofortige Levelverfügbarkeit und darf nicht mit einer praktisch ab Start finanzierbaren Investitionsfolge verwechselt werden. Mit zwei maximalen Minen kommen 6 Gold/s hinzu: bei Basis6 verdoppelt sich das laufende passive Einkommen, bei Basis5 steigt es auf11, bei Basis4 auf10. Eine Reduktion der Basis erhöht also den relativen Einfluss von Minen.

Im überarbeiteten Slotkarten-Bot wurde ein echter Integrationsmangel festgestellt: die Bedingung „höchstens drei Angreifer irgendwo auf dem Feld“ unterbindet auf den langen Routen fast sämtliche Mineninvestitionen. Der Core-Test `EconomyStrategyHasReproducibleMatchEvidence` fand in zwölf Meadow-Partien keine Mine. Das wurde dem Bot-Owner gemeldet. Die Slotkarten-Balancezeilen sind daher kein belastbarer Nachweis der finalen Economy-Strategie. Die Legacy-Probe enthält tatsächlich Minen und bleibt separat auswertbar.

## Reproduktion und Regression

```sh
DOTNET_CLI_HOME=/tmp/defense-dotnet-home DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  /Applications/Unity/Hub/Editor/6000.6.6f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet \
  run --project Tools/CoreHarness/CoreHarness.csproj --configuration Release -- \
  --economy --catalog --current-bots --seeds 8 --speed-percent 160 --output /tmp/economy-candidate
```

Ohne `--catalog` benutzt der Harness die eingefrorenen alten Karten; ohne `--current-bots` die eingefrorene Probe-Policy. `--speed-percent` multipliziert die **jeweils aktuellen** Definitionstempi. Die gespeicherten v1.5-Tempo160-Daten wurden vor Übernahme der schnelleren Produktionswerte erzeugt; auf v1.6 darf nicht nochmals160% zum Nachstellen angewendet werden. Zur exakten Wiederholung älterer Läufe ist der jeweilige Core-Definitions-/Regelstand notwendig. `content.csv` macht Abweichungen sichtbar.

Die v1.4-Regressionsrunde bestand 38/38 Szenarien, darunter 10 sofortige Käufe bis Gold0, doppelte Requests, Rate-/Queuegrenzen, gemischte FIFO, exakter Tank-/Schwarmabstand, Unterbrechung durch volle Felder, Rematch sowie 64 Bauobjekte/160 Einheiten. Die v1.5-Runde bestand alle neuen Tötungs-/Deckel-/Leak-/Slotklassentests; einzig der oben benannte Bot-Minen-Nachweis scheiterte. Der endgültige v1.6-Lauf samt freier Platzierung ist in `Docs/Gameplay.md` ergänzt: 49/49 Core-Szenarien und 72 seitengetauschte freie-Map-Partien bestanden. Der finale Bot investiert wieder in Minen; zwölf Economy-Siege gegen Balanced bleiben als offener Strategiehinweis dokumentiert. Unity-Import/APK/Netzprotokoll und Gerätetest verantwortet die Integration; diese Economydatei behauptet dafür keinen Nachweis.
