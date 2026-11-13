# Gameplay, Regeln und Messungen

Stand: 10. November 2026, Regelversion **v1.8**. Implementierung: `Assets/Core/`. Die Simulation ist reines C# ohne Unity-Abhängigkeit. Nach dem gesicherten 0.5-Checkpoint `a80c882` ergänzt v1.8 die gemeinsame Tempoabstimmung. Freie Minen, zwei insgesamt wählbare Schaden-/Reichweitenkäufe und sämtliche Balancewerte bleiben erhalten. Die Produktionswerte bleiben ein Beta-Kandidat; eine abschließende Spielspaß-/Strategiebalance ist nicht belegt. Frühere Economy- und Slotkartenproben in `Docs/EconomyIteration.md` bleiben getrennt von diesen Regeln.

## Verbindliche Regeln

- 20 Schritte pro Spielsekunde; Countdown 60 Schritte = 3 Echtzeitsekunden; danach maximal 9600 Schritte = 8 Spielminuten. Countdown zählt nicht zur Matchdauer.
- Beide Spieler stimmen während Playing ausdrücklich über 2× ab. Erst zwei gesetzte Stimmen ergeben SimulationSpeed=2; eine akzeptierte Rücknahme ergibt sofort wieder1. Countdown und Ergebnisse bleiben1×. Jede Core-Step-Ausführung bleibt ein Spieltick; der Coordinator führt bei2× doppelt so viele Spielticks je Echtzeitsekunde aus. Einkommen, Bewegung, Kampf, Ausmarsch und Matchuhr folgen gemeinsam der Spielzeit. Neue Runden beginnen ohne Zustimmung; der Bot stimmt niemals selbständig ab.
- Beide Spieler beginnen mit 180 Gold und 30 Basis-HP; alle 20 Spielschritte erhalten beide 5 Basis-Gold plus den Ertrag ihrer aktuell bestehenden Goldminen. Getötete Läufer/Sprinter/Tanks/Schwarmeinheiten zahlen einmalig 4/5/14/2 Gold an den Verteidiger. Entkommene Einheiten zahlen nichts. Obergrenze 10.000 Gold; Truppenkäufe erhöhen das passive Einkommen nicht.
- Bis zu 30 Kampftürme und zwei Minen können frei in gültigen Mapkoordinaten gebaut werden. Es gibt keine reservierten Minenflächen mehr. Drei symmetrische Karten sind jeweils 1600×960 logische Einheiten groß. Gebäuderadius 30, Wegmittenabstand mindestens 70, Gebäudemittenabstand mindestens 60 und Randabstand 30 gelten für beide Gebäudekategorien. Terrain-Sperrflächen berücksichtigen den Gebäuderadius; Gegnergebäude blockieren das eigene Feld nicht.
- Zielpriorität: höchste Wegdistanz in Reichweite; bei Gleichstand kleinere stabile Entity-ID. Kollisionsfreie Bewegung, keine Unity-Physik und kein Pathfinding.
- Schrittfolge: Einkommen → bezahlte Spawnwarteschlangen → Turmtreffer/Status → tote Einheiten einmalig vergüten/entfernen und Bewegung/Basisschaden → einmalige Ergebnisentscheidung. Dadurch kann ein im selben Schritt erschossener Gegner keine Basis mehr beschädigen. Beide Basen im selben Schritt auf null ergeben Unentschieden.
- Bei Ablauf gewinnt die höhere verbleibende Basis-HP, bei Gleichstand Unentschieden. Ein einmal gesetztes Ergebnis wird nicht überschrieben, auch nicht durch einen späteren Abbruch.
- Rüstung reduziert Schaden um `max(0, Rüstung − Panzerbrechung)`; jeder Treffer verursacht mindestens 1 Schaden. Frost ist ein Geschwindigkeitsfaktor: 1000 normal, 650 beim Standard-Frost, niemals unter 600. Wiederholte Treffer verlängern die Wirkung, multiplizieren sie aber nicht. Einheiten werden niemals eingefroren.
- Verkauf gibt abgerundet 65 Prozent der gesamten bisherigen Investition zurück. Eine Rückabwicklung von Truppenkäufen ist nicht vorgesehen.
- Höchstens 80 aktive Einheiten auf jedem Feld. Jeder Tap bezahlt sofort eine weitere Gruppe; bis zu 80 bezahlte Aufträge je Absender warten FIFO. Klassenabstände beginnen erst beim ersten tatsächlichen Spawn einer Gruppe. Der FIFO-Kopf darf auf seinen Klassen-Timer warten; spätere Aufträge überholen ihn nicht. Global und innerhalb eines Schwarms erscheint höchstens eine Einheit je 4 Ticks. Volle Felder behalten alle bezahlten Einheiten und starten noch nicht begonnene Gruppen-Timer nicht. Ein voller Puffer weist Käufe vor jeder Abbuchung zurück.

## Türme und wählbare Ausbauten

Jeder Kampfturm erlaubt insgesamt **zwei bezahlte Käufe**; pro Kauf wird Schaden oder Reichweite gewählt. Der Preis folgt dem ersten/zweiten Gesamtkauf, der verbesserte Wert der eigenen Pfadstufe. Schaden–Reichweite und Reichweite–Schaden ergeben denselben Mischzustand. Der Schusstakt bleibt beim Basisintervall. Laufende Abklingzeit, Position und Fußabdruck bleiben erhalten. Verkauf berücksichtigt beide tatsächlich gezahlten Preise.

| Klasse | Grundpreis | Schaden Basis/I/II | Reichweite Basis/I/II | Intervall | Kauf 1/2 | unveränderte Besonderheit |
|---|---:|---|---|---:|---|---|
| Schnellfeuer | 65 | 6/9/14 | 150/180/210 | 8 Ticks | 55/85 | Einzelziel |
| Kanone | 105 | 16/26/40 | 155/186/217 | 32 Ticks | 85/130 | Splashradius 90 |
| Frost | 85 | 4/6/8 | 155/186/217 | 20 Ticks | 60/95 | 65 % Bewegung für 30 Ticks |
| Panzerbrecher | 120 | 40/64/96 | 190/228/266 | 42 Ticks | 85/130 | ignoriert 8 Rüstung |
| Goldmine | 140 | 0 | 0 | keine Angriffe | 100/160 | Ertrag 1/2/3 Gold/s, höchstens zwei |

Die Reichweiten I/II entsprechen aufgerundet 120/140 Prozent des Basisradius und sind explizit in den gehashten Definitionen gespeichert. Der zweite Reichweitenkauf multipliziert nicht erneut den bereits vergrößerten Wert. Schaden verbessert keine Reichweite und keinen Schusstakt; Reichweite verbessert keinen Schaden. Splash, Frost und Rüstungsdurchdringung bleiben unverändert.

Minen werden wie Kampftürme auf jeder geeigneten freien Fläche gebaut. Sie besitzen ausschließlich zwei Ertragskäufe und niemals Angriffswerte. Zu jeder vollen Spielsekunde werden Erträge aus vorhandenen Minen ermittelt; Verkauf stoppt sie unmittelbar. Eine maximale Mine kostet 400 Gold, erzeugt 3 Gold/s und verkauft sich für 260 Gold. Zwei maximale Minen erhöhen passives Einkommen von 5 auf 11 Gold/s. Eine dritte wird vor Abbuchung mit ClassLimit abgewiesen. Legacy-Slotkarten bleiben separat kompatibel.

`TowerUpgradeRules` liefert gemeinsame Stats, goldunabhängige Quotes und visuelle Varianten. Kampfstufen `(Schaden,Reichweite)` verwenden `(0,0)→0`, `(1,0)→1`, `(0,1)→2`, `(2,0)→3`, `(1,1)→4`, `(0,2)→5`; Minen behalten Variante 0/1/2. Der Core-Nachweis prüft diese Zuordnung. Erkennbare Modellformen im echten Renderer, Bounds und ReducedEffects werden separat von der Presentation-Integration geprüft.

## Kartenkatalog

| Index / ID | Name | Weglänge | Wegpunkte | Terrain-Sperrflächen | Gebäudepositionen |
|---|---|---:|---:|---:|---|
| 0 / `meadow` | Grünes Tal | 4532 | 23 | 9 | frei, auch Minen |
| 1 / `desert` | Sonnenpass | 4122 | 20 | 10 | frei, auch Minen |
| 2 / `frost` | Frosthafen | 4038 | 21 | 9 | frei, auch Minen |

`MapCatalog.Create(index)` erzeugt unveränderliche Kartendefinitionen. `CreateContents(template)` behält die möglicherweise angepasste `template.Map` an Index 0. Die 32 überlieferten Koordinaten sind nur noch mögliche Bot-Hinweise; kein freies Gebäude wird darauf beschränkt. Alle drei freien Karten haben leere MineSlots. Frühere Minenstandorte sind gewöhnlicher geeigneter Boden. Weg- und Sperrflächengeometrie wurde in dieser Iteration nicht verändert. Sämtliche Metadaten, Koordinaten, MineSlots/FreePlacement/BlockedAreas und Platzierungsgrenzen werden gehasht.

## Truppen

| Klasse | Preis pro Bestellung | HP je Einheit | Tempo/s | Rüstung | Basisschaden | Anzahl | Gruppenstart-Abstand | Kill-Gold/Einheit |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Läufer | 18 | 34 | 104 | 0 | 1 | 1 | 20 Ticks | 4 |
| Sprinter | 24 | 22 | 184 | 0 | 1 | 1 | 30 Ticks | 5 |
| Tank | 65 | 220 | 59 | 5 | 3 | 1 | 70 Ticks | 14 |
| Schwarm | 38 | 26 | 93 | 0 | 1 | 4 | 40 Ticks | 2 |

## Befehle und Lebensdauer

`Execute(PlayerSlot, CommandEnvelope)` ist der einzige Mutationspfad für Spieler- und Bot-Aktionen. Jeder freie Bau nutzt Target=-1 plus PositionX/PositionY, auch Minen. Upgrade/Sell.Target ist die Tower-ID. Upgrade.Kind bedeutet Damage=0, Range=1 oder Income=2. Quote zeigt Wirkung/Preis auch ohne genügend Gold; Execute prüft zusätzlich Besitzer, Phase, Session und Gold. Tower.Position bleibt echte Kampforigin, SiteId=-1 gilt für alle freien Gebäude und Slot0..31 ist nur Renderkapazität. Vertrag: `Docs/CONTRACT.md`.

Ein Match besitzt eine nichtnullige `SessionId`. Jeder Slot führt einen monotonen Sequenzzähler und einen begrenzten Cache der letzten 128 Antworten. Identische ID und Payload wiederholen die ursprüngliche Antwort ohne Gameplay-Mutation. Dieselbe ID mit anderer Payload wird abgewiesen. Aus dem Cache gefallene alte IDs werden als veraltet abgewiesen. Auch abgelehnte Käufe behalten ihre ursprüngliche Ablehnung. Falsche Session-IDs werden nie in die neue Runde übernommen.

`SetSpeedVote` ist der angehängte Befehl6: Kind0 zieht die eigene Zustimmung zurück, Kind1 stimmt2× zu. Target muss−1 und beide Koordinaten müssen0 sein. Falsche Phase ergibt WrongPhase, ungültiges Kind InvalidKind, falsches Target InvalidTarget, nichtnullige Koordinaten InvalidValue. Der Befehl kostet kein Gold und durchläuft dieselbe Session-/Sequenz-/Receipt-/Rateprüfung. Alte Zustimmungs- oder Rücknahmeantworten werden nur wiederholt und überschreiben niemals neuere Stimmen. Eine wegen Rate-Limit abgelehnte Rücknahme verändert die Stimmen nicht; nach Erholung ist ein neuer Request erforderlich.

Während Playing erlaubt Core maximal 40 neue Befehle je Slot und 20-Tick-Zeitfenster; Rate-Limit-Antworten werden ebenfalls gecacht. In Lobby und Finished ist dieser tickbasierte Limiter deaktiviert, damit stillstehende Simulationsticks keine dauerhafte Rematchsperre erzeugen. Dort sind nur Ready bzw. Rematch gültig; der Transport kann zusätzlich nach Echtzeit begrenzen.

Revanche benötigt beide Stimmen. Dann wird eine neue `MatchState` mit neuer Session-ID erzeugt; Gold, HP, Entitäten einschließlich Minen, Warteschlangen, Status, Ready-Flags, Sequenzen und Antwortcaches werden zurückgesetzt. Definitionen sind unveränderlich; Arrays werden im Konstruktor und bei öffentlichen Gettern kopiert. Für oft gelesene UI-Definitionen sollten Aufrufer die Arrays einmal cachen. `ContentHash` ist SHA-256 über alle Spielwerte einschließlich Einkommens-/Klassenlimits, Kartenmetadaten, Weg-/Baukoordinaten und die explizite Simulationsregelversion. Bei semantischen Regeländerungen muss diese Version erhöht werden.

Der Bot ist ein kleiner reproduzierbarer Regelalgorithmus mit Seed, ohne KI-API. Er verwendet ausschließlich `Execute` und `LastSequence(slot) + 1`. Er bestätigt Ready/Revanche nicht selbst; das ist die Aufgabe des Offline-Coordinators. Geplante Bau- und Upgradeaktionen reservieren Gold, damit der Bot nicht durch wiederholte Billigkäufe dauerhaft am Upgraden gehindert wird. Diese Fehlerursache wurde durch einen fehlgeschlagenen Regressionstest gefunden und behoben.

Der normale Balanced-Bot plant höchstens eine Goldmine; Economy plant zwei und deren Upgrades. Der überarbeitete Bot hält Budget für Angriffe frei, bewertet legale freie Kampfpositionen über denselben Validator und unterscheidet unmittelbare Basisbedrohung von weit entfernten Einheiten auf langen Routen. Die alte Bedingung „höchstens drei Angreifer irgendwo“ verhinderte Mineninvestitionen; ein fehlgeschlagener Regressionstest fand das, der Bot-Owner reparierte es. Der finale Nachweis beobachtet wieder Minen, zeigt aber zugleich einen offenen Economy-Vorteil gegen Balanced.

## Automatisierte Nachweise

Der ausführbare Harness verwendet dieselben Szenarien wie `CoreEditModeTests`, ohne NuGet-Pakete:

```sh
DOTNET_CLI_HOME=/tmp/defense-dotnet-home \
DOTNET_CLI_TELEMETRY_OPTOUT=1 \
"/Applications/Unity/Hub/Editor/6000.6.6f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet" \
  run --project Tools/CoreHarness/CoreHarness.csproj --configuration Release -- --balance --stress
```

Alternativ genügt ein installiertes .NET-8-SDK mit `dotnet run ...`. `Tools/CoreHarness/NuGet.Config` deaktiviert Paketfeeds; der Harness benötigt nur mitgelieferte .NET-Referenzpakete.

Historische Nachweisdatei der Version v1.0: `Tools/CoreHarness/Results/verified-2026-11-10.txt`. Abgedeckt sind Datenvalidierung und Hash, Countdown, idempotente/stale Befehle, fremdes Eigentum, Upgrade-/Refundgrenzen, volle Felder und Warteschlangen, exakte Schwarmzahl/Cooldown, Rüstung/Panzerbrechung, Priorität, Frost, Flächenschaden, Tod/Entkommen, gleichzeitige Basiszerstörung, Zeitlimit, Revanche, Abbruch, Rate-Limit-Erholung, 16.000 zufällige Befehle, reproduzierbare Bots sowie 160 Einheiten. Zusätzlich wird der kritische tödliche Treffer vor Basisschaden und exaktes Einkommen/Bewegungs-Rundung geprüft.

### v1.8: geprüfter Core für Folgeversion 0.6

Finaler .NET-Release-Build: **0 Fehler, 0 Warnungen; 63/63 Szenarien bestanden**. Laufprotokoll: `Tools/CoreHarness/Results/verified-v1.8-2026-11-10.txt`. Das neue `Tools/CoreHarness/Results/v1.8-source-manifest.json` ordnet den geprüften Quellumfang separat zu; v1.7-Dateien bleiben unverändert erhalten.

Sechs neue Szenarien prüfen Zustimmung in beiden Spielerreihenfolgen, fehlendes Gold, wiederholtes explizites Setzen, unmittelbare Rücknahme, strikte Payload-/Phasenvalidierung, fremde/alte Session-Metadaten, Sequenz0, geänderte Duplikate und verspätete Antworten nach neuerer Zustimmung oder Rücknahme. Weitere Fälle prüfen unveränderte Ablehnung nach Rate-Limit-Erholung, aus dem Cache gefallene Votes, Ergebnis/Abbruch und Zurücksetzen bei Revanche. Ein gepaarter Lauf vergleicht **600 aufeinanderfolgende Spielticks** mit und ohne Tempoabstimmung einschließlich bezahlter FIFO, Frost, Schaden und Minenupgrades; alle kanonischen Gameplaywerte bleiben exakt gleich. Ein Step erhöht auch bei2× Tick und PlayTicks nur um1.

Der unveränderte v1.7-Bot (SHA-256 `361f1c16d97b54df50f7390e2371d3b70814a92b77ce013fd129202046cb9a33`) wurde erneut mitgeprüft und stimmt nicht selbständig ab. Die72 vollständigen Kartenpartien einschließlich36 Seitentauschpaaren und zwölf Economy-gegen-Balanced-Partien liefern dieselben Ergebnisse wie der unten erhaltene v1.7-Lauf. Es gab keine neue Balanceänderung. Der lange defensive Verlauf und Economy-Vorteil in dieser kleinen Stichprobe bleiben offene Spieltestpunkte.

```text
meadow 3ff29591e7acea602eace59e262d4ec06ceb54082a9287e6b8b254ffa529bf76
desert 0a94f852160d2e61fb96626f28ef04b415c181929e954e34e4bd7daa979df707
frost  51e293c96b73b2f26125d2a31889407a9c728c0ad29ab34a96a13d5174a8e386
CORE_TESTS total=63 failures=0
STRESS ticks=18000 towers=64 units=160 wall_ms=231 allocated_bytes=144 gen0_collections=0
```

Die Lastmessung betrifft allein den .NET-Regelcode auf dem Entwicklungs-Mac. Tatsächliches Echtzeittempo, Zurücksetzen des Coordinator-Akkumulators, Offline-Spiegelung, Wire5-Replikation und HUD-Verhalten sind eigenständige Integrationsprüfungen. Kein Unity-Prozess und kein Build der Folgeversion wurde durch diesen Corelauf gestartet oder freigegeben.

### Historisch: v1.7-Core für Folgeversion 0.4

Finaler .NET-Build: **0 Fehler, 0 Warnungen; 57/57 Szenarien bestanden**. Protokoll: `Tools/CoreHarness/Results/verified-v1.7-2026-11-10.txt`. Quellenzuordnung: `Tools/CoreHarness/Results/v1.7-source-manifest.json`, 14 Quellen-/Konfigurationsdateien beim Abschluss unverändert geprüft. Der freigegebene Bot wurde vor dem Lauf über SHA-256 `361f1c16d97b54df50f7390e2371d3b70814a92b77ce013fd129202046cb9a33` verifiziert. Es wurde keine eigene Unity-Instanz gestartet; integrierte Unity-/Netz-/Renderer-/Buildprüfungen liegen beim Lead.

Neue Prüfungen decken alle sechs Kampfkombinationen und alle Reihenfolgen DD/DR/RD/RR ab, zusätzlich Mine Income I/II. Quote und tatsächlich verbuchte Werte stimmen überein; gemischte Reihenfolgen kosten dasselbe. Getestet sind unveränderte Restabklingzeit/Position/Identität, wirklicher Schaden im Kampf, tatsächliche erhöhte Reichweite, unveränderter Basisschusstakt, dritte Käufe, falsche Pfade einschließlich Integer-zu-Byte-Grenzen, fehlendes Gold, fremder Besitzer, falsche Phase, Replay, geänderter Replay-Pfad, Revanche und ungültige Snapshot-Pfadkombinationen.

Freie Minen wurden auf beliebigen gültigen Koordinaten sowie gegen Weg-/Rand-/Terrain-/Gebäudeabstandsfehler, doppelte Befehle und die Zweiergrenze geprüft. Frühere Minenstandorte sind auf allen drei Karten nachweislich für normale Kampftürme frei. Alte gemischte und dedizierte Slotkarten bleiben in eigenen Fixtures gültig. Strikt steigende Damage-/Range-Daten, Basisintervalle und Hash-Änderungen werden vor Matchbeginn validiert.

Zusätzlich liefen **72 vollständige Kartenpartien** mit zwölf Seeds und Seitentausch: 36 Seitenpaare behielten zugeordnete HP und Dauer exakt bei. Eine separate kleine Meadow-Reihe umfasst zwölf Economy-gegen-Balanced-Partien.

| Karte | Balanced-Partien | Remis | mittlere Dauer | mittlere Rest-HP A/B |
|---|---:|---:|---:|---:|
| Grünes Tal | 24 | 8 | 480 s | 24,08 / 23,75 |
| Sonnenpass | 24 | 4 | 480 s | 24,83 / 22,67 |
| Frosthafen | 24 | 0 | 480 s | 26,25 / 22,00 |

Economy gewann die zwölf Meadow-Partien gegen Balanced, ebenfalls nach 480 Sekunden; 36 bestehende Minen wurden beobachtet. Das ist ein konkreter offener Strategiehinweis, kein Beweis globaler Dominanz. Die weiterhin langen defensiven Matches bleiben ein menschlicher Balance-/Spielgefühlpunkt. Es wurde daraufhin kein weiterer ungeplanter Preis-, HP- oder Einkommensumbau vorgenommen. Die separate Bot-Owner-Reihe dokumentiert weitere Gegenstrategien unter `Tools/BotFeedbackHarness/Results/v1.7`.

```text
meadow 15a4eb886b5242e51e06a95041728402a5888cbc6c375df9037bf86d647cc0ed
desert e8e71f8a89a2390bf3ea3714c74f53ff0e26566ff5371ec70ca0560da6cfaabf
frost  1bdefc52b0224cee5e6d75471cdc20adcfb36979fc3ade2cf354c86d32a0b0e4
CORE_TESTS total=57 failures=0
STRESS ticks=18000 towers=64 units=160 wall_ms=255 allocated_bytes=144 gen0_collections=0
```

Die Lastprobe verwendet 60 Kampftürme und vier frei platzierte Minen sowie 160 Einheiten über 900 simulierte Sekunden. 255 ms Walltime und kein Gen0-GC betreffen ausschließlich den .NET-Regelcode auf dem Entwicklungs-Mac; sie belegen keine Android-Framerate. Die neuen Coredateien wurden danach für die Integration eingefroren.

### Historisch: v1.6-Core-Integrationsstand

Der finale .NET-Build am 10. November meldete **0 Fehler, 0 Warnungen; 49/49 Szenarien bestanden**. Vollständiger Lauf: `Tools/CoreHarness/Results/verified-v1.6-2026-11-10.txt`. Neue Fälle belegen sofortige zehn Läuferkäufe bis Gold0, gecachte Rate-Limit-Ablehnung, FIFO/Tank-/Schwarmabstände, volle Felder, 80/81 Aufträge, Revanche-Reset, exakte einmalige Kill-Bounties einschließlich Splash und gleichzeitiger Leaks, Golddeckel, dedizierte Minenslots sowie freie Positionen ohne versteckte Kampfliste.

Freie Geometrie wurde an exakten Grenzen geprüft: Weg 70, Radius/Rand 30, Gebäudeabstand 60, Sperrflächen mit Radius, reservierte noch unbebaute Minen, unabhängiges Gegnerfeld, Selbst-Ausnahme für Snapshotvalidierung und extreme Integer-Koordinaten. Eine Testkarte mit ausschließlich zwei definierten Minensites erlaubt nachweislich trotzdem 30 frei gewählte Kampftürme pro Spieler. Kampfreichweite benutzt die tatsächliche Position. Eine veränderte Koordinate unter wiederverwendeter Request-ID wird als Duplicate abgewiesen.

Zusätzlich liefen **72 vollständige Partien** auf den finalen freien Karten mit zwölf Seeds und Seitentausch. Jede Spiegelprüfung behielt zugeordnete HP und Dauer exakt bei. Alle endeten regulär nach 480 Sekunden; die weiterhin langen defensiven Partien sind ausdrücklich ein offener Spieltestpunkt.

| Karte | Partien | Remis | mittlere Rest-HP A/B |
|---|---:|---:|---:|
| Grünes Tal | 24 | 0 | 23,75 / 23,33 |
| Sonnenpass | 24 | 2 | 23,25 / 22,25 |
| Frosthafen | 24 | 4 | 24,50 / 22,50 |

Die kleine Economy-gegen-Balanced-Reihe auf Grünes Tal enthält zwölf Economy-Siege, keine Niederlage/kein Remis und 36 bestehende Minen. Das belegt die reparierte Investitionsausführung und zugleich einen möglichen Strategie-Vorteil; es ist kein allgemeiner Balancenachweis. Weitere Gegenstrategien und menschliche Partien sind nötig. Die umfangreichen v1.5-Slotkartenmessungen dürfen nicht als v1.6-Ergebnisse ausgegeben werden.

```text
meadow defcb4df18ce4a014cfc3383c1f82ec924502180fc3be6265b153827bac98a94
desert 656a79ddf1ff7f7bf0a4ff997838e14bd6dbe39c2b3c136b0cb39b969d66589e
frost  fc73bc9ae278285f814b020213f653dbf592ade3e8a8cbf7d01dd556af2793ca
CORE_TESTS total=49 failures=0
STRESS ticks=18000 towers=64 units=160 wall_ms=235 allocated_bytes=144 gen0_collections=0
```

Die Lastprobe umfasst 60 Kampftürme und 4 Minen sowie 160 Einheiten über 900 simulierte Sekunden. Ihr .NET-Regelcode benötigt im gemessenen Entwicklungs-Mac-Lauf 235 ms; das belegt keine Android-Frametimes. Unity-Import, APK, reale Netzpartien und Touch/Rendering werden separat vom Lead geprüft. Coredateien wurden danach für die Integration eingefroren.

### Historisch: v1.2-Karten- und Minenregression

Der erneute .NET-8-Harness-Lauf am 10. November bestand **33/33 Szenarien**, einschließlich sämtlicher bisherigen Regeln. Neue Prüfungen: drei unterschiedliche gültige Karten mit je zwölf freien Bauplätzen, Karten-/Minenparameter im Hash, Erhalt angepasster Template-Daten, ungültige Metadaten/Wirtschaftsdefinitionen, Mine ohne ausreichendes Gold, dritte Mine ohne Goldverlust, belegte Slots, exakter Ertrag aller drei Stufen, niemals schießende Mine, Verkauf unmittelbar vor dem Einkommens-Tick, doppelte Verkäufe und Reset des Einkommens nach Revanche.

Zusätzlich wurden pro Karte zwölf Seeds mit getauschten Seiten gespielt: insgesamt **72 vollständige Partien**. Bei jedem Seed blieb nach Seitentausch sowohl die zugeordnete Rest-HP als auch die Matchdauer exakt gleich. Alle Partien endeten regulär innerhalb des Limits; der Test enthält eine feste Tickobergrenze und prüft Gold-/Entitätenlimits.

| Karte | Partien | Unentschieden | mittlere Dauer | mittlere Rest-HP Bot A / Bot B |
|---|---:|---:|---:|---:|
| Grünes Tal | 24 | 6 | 480,00 s | 27,42 / 27,92 |
| Sonnenpass | 24 | 6 | 480,00 s | 27,58 / 27,83 |
| Frosthafen | 24 | 4 | 480,00 s | 26,08 / 26,75 |

Economy gegen Balanced auf Grünes Tal: sechs Seeds mit getauschten Seiten = **12 Partien**, 4 Siege / 6 Niederlagen / 2 Unentschieden für Economy, mittlere Dauer 480 s, insgesamt 36 bestehende Minen beobachtet (Economy zwei, Balanced eine pro Match). Diese kleine Stichprobe zeigt keine dominante Minenstrategie, beweist aber noch keine allgemeine Balance. Die bekannten defensiven Partien bleiben als offener menschlicher Spieltest bestehen.

Historische v1.2-Hashes:

```text
meadow 1af48454a094f857c519a428364afad0be37c2c3c36c314d5c865dfd8146471b
desert d60c8cee3da5aedff2415919f03a0fcd2d1820830b97e1d7b9e7875b089b121d
frost  bf62ba03cbebbf1a038ebc092b3676957897bc33b46672b9c55ae404ce440c5d
CORE_TESTS total=33 failures=0
```

Ausführung wie oben, ohne zusätzliche `--balance --stress`-Optionen; die neuen Karten-/Minenfälle laufen automatisch über `CoreScenarios.All`. Vollständiges unmittelbares Laufprotokoll: `/tmp/defense-core-v12-results.txt`. Der Unity-Testadapter verwendet dieselben Szenarien. Unity-Import, 3D-Rendering, APK, Multiplayer-Kartenauswahl und Geräteverhalten müssen separat integriert/geprüft werden; der Core-Harness allein belegt sie nicht.

### Historische Balance-Schleife vor Einführung der Goldminen

Hypothese: Grundabwehr hält zu viele Angriffe auf. Ausgangsmessung: je 12 Seeds, jeweils beide Seiten, 24 Partien pro Strategie gegen Balanced, insgesamt 144 vollständige Partien. „RunnerRush“ und „TankRush“ kaufen ausschließlich ihre Truppen; „Saving“ tut nichts; „PureDefense“ baut/verbessert nur Verteidigung; „FrostDefense“ baut ausschließlich Frost. Das sind feste Probe-Strategien, keine optimale Spielsuche.

| Strategie gegen Balanced | Siege | Niederlagen | Unentschieden | mittlere Matchdauer | eigene / gegnerische Rest-HP |
|---|---:|---:|---:|---:|---:|
| Balanced | 6 | 12 | 6 | 480,00 s | 27,42 / 27,92 |
| Läufer-Spam | 0 | 24 | 0 | 175,88 s | 0 / 30 |
| Tank-Spam | 0 | 24 | 0 | 175,88 s | 0 / 4,25 |
| Reine Verteidigung | 0 | 0 | 24 | 480,00 s | 30 / 30 |
| Sparen ohne Aktionen | 0 | 24 | 0 | 175,88 s | 0 / 30 |
| Nur Frost | 0 | 22 | 2 | 480,00 s | 27 / 30 |

Kandidat: Einheiten-HP auf 50/36/290/40 erhöhen; alle anderen Regeln gleich. Derselbe Versuch mit 144 weiteren Partien zeigte Tank-Spam als dominierend: 24/24 Siege nach durchschnittlich 148,21 s. Gleichzeitig dauerten Balanced-Partien weiterhin 480 s. Deshalb wurde der Kandidat **verworfen**, und die Ausgangswerte bleiben erhalten. Reproduzieren mit `--balance --candidate`; Beleg `Tools/CoreHarness/Results/candidate-hp-2026-11-10.txt`.

Die damalige Simulation erfüllt das definierte Acht-Minuten-Limit; gemischte Bot-Partien sind aber deutlich defensiv. Reine Verteidigung erzwingt gegen diesen Bot häufig Unentschieden. Die v1.2-Kartenmessungen zeigen weiterhin starke Verteidigung. Das ist ein offener Balance-/Spielspaßpunkt für menschliche Spieltests, kein Nachweis einer fertig abgestimmten Veröffentlichung. Diese Bots beweisen keinen Spielspaß und keine globale Strategie-Balance. Die historischen `--balance`-Zahlen sind mit dem aktuellen Minen-Bot nicht unverändert reproduzierbar; ihre Ausgangsprotokolle bleiben als Entscheidungsnachweis erhalten.

### Last

Der aktuelle Harness hält 160 aktive Einheiten und 64 Bauobjekte für 18.000 Ticks = 15 Minuten **simulierte** Zeit. Die Stress-Definition erhöht nur für diesen Test HP auf 100.000 und reduziert Tempo auf 1, damit maximale Belegung erhalten bleibt. Einkommen, Targeting, Treffer und Frost laufen weiter. Historische v1.0-Messungen hatten 24 Türme; diese Zahlen bleiben getrennt. Die Messung betrifft .NET-Regelcode auf dem Entwicklungs-Mac und belegt weder Unity-Rendering noch Android-Frametimes, Netzwerkvolumen, Speichertrend oder Erwärmung.
