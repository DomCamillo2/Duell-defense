# Netzwerk – Implementierung und Nachweise

Stand: 2026-11-10. `SessionCoordinator` ist der einzige Besitzer von `NetworkManager`, Transport, Simulation, Listenern und asynchronen Starts. Er wird von `GameBootstrap` einmal initialisiert; `GameUI` benutzt ausschließlich seine öffentliche API. Die Boot-Szene enthält keine Netzwerkobjekte. NGO Scene Management und automatische Spieler-Prefabs sind deaktiviert. Alle Geräte starten dieselbe lokale Szene.

## Modi und Cloud

- Offline erzeugt die autoritative Simulation und einen reproduzierbaren Bot. Es wird weder `UnityServices` initialisiert noch ein NetworkManager gestartet.
- WLAN verwendet NGO 2.13.3 und Unity Transport 2.7.5 direkt über die eingegebene IP/UDP-Port. Kein Cloud-Login. Host bindet an `0.0.0.0`, Client an einen freien lokalen Port. Bei Problemen: gleiche WLAN-Verbindung, korrekte private Host-IP, Port 7777, Gastnetz-/AP-Isolation und Firewall prüfen.
- Online verwendet `com.unity.services.multiplayer` 2.4.1, dessen aktuellen Relay-API-Pfad, `UnityServices.InitializeAsync`, anonyme technische Authentication und `AllocationUtils.ToRelayServerData(..., "dtls")`. Host reserviert genau eine Client-Verbindung und erhält einen Joincode; Client tritt über diesen Code bei. Kein eigenes Konto-UI, kein Lobby-Service, kein öffentliches Matchmaking. Quelle: [Unitys unterstützte MPS/NGO-Integration](https://docs.unity.com/en-us/mps-sdk/tutorials/relay-and-ngo).
- Fehlt `Application.cloudProjectId`, erscheint eine konkrete Einrichtungsnachricht. Es wird kein Cloud-Projekt angelegt, kein Billing aktiviert und kein fehlgeschlagener Relay-Test durch eine lokale Simulation ersetzt. Die Dienste-Authentifizierung wird nur bei Online aufgerufen. Login/Allocation haben begrenzte Wartezeit; ein Generation-Token verhindert, dass verspätete Tasks nach Abbruch einen Raum starten. Ein bereits laufender SDK-HTTP-Aufruf kann physisch weiterlaufen; ein dabei ungenutztes Relay-Allocation verfällt serverseitig.

## Protokoll 4, Karten und Authority

Handshake: 70 Bytes (Magic, Version 4, SHA-256 des geordneten Katalogs aller drei Content-Hashes). NGO Connection Approval prüft Version/Katalog und reserviert ausschließlich Slot 1. Host bleibt Slot 0; NGO-Client-IDs werden nie als Spielerslot interpretiert. Beide Seiten prüfen den Kataloghash vor Lobbyfreigabe. Protokoll ist zusätzlich in `NetworkConfig.ProtocolVersion` gesetzt.

`Initialize(GameContent)` erstellt über `MapCatalog.CreateContents` den Katalog; `Initialize(GameContent[])` prüft und kopiert die drei unveränderlichen Definitionen. `MapNames`, `SelectedMapIndex` und `SelectMap(index)` bilden die UI-Schnittstelle. Kartenwahl ist nur im Hauptmenü (`Idle`) erlaubt. Welcome enthält den Katalog-Handshake, einen Kartenindex, eine 32-Bit-Snapshotrevision und den Snapshot. Client prüft den Index und den Zustand gegen die passende Kartendefinition, übernimmt `Content` und benachrichtigt die UI. Seine vorherige Menüauswahl ist unerheblich. Revanche bleibt auf derselben Karte; Kartenwechsel in einer laufenden Verbindung wird abgewiesen. 3D-Darstellung bleibt vollständig von diesen autoritativen Regeln getrennt.

Kauf-/Verkaufs-/Bereit-Befehle sind 33 Byte (Sitzungs-Epoch, Sequenz, Typ, Kind, Ziel, PositionX, PositionY). Alle freien Bauten einschließlich Goldminen verwenden Target = −1 und tatsächliche Koordinaten. UpgradeTower.Kind wählt den Pfad Damage = 0, Range = 1 oder Income = 2; nur Minen verwenden Income. Beide Koordinaten und der gewählte Pfad gehören zur idempotenten Request-Identität. Protokoll 3 und ältere Versionen werden bereits beim Handshake abgewiesen. Es gibt keinen vom Client behaupteten Geldbetrag, Schaden oder Besitz. Der Host ordnet den Absender anhand der NGO-Verbindung zu und ruft für Host, Client und Bot dieselbe Core-Validierung auf. ACKs sind zuverlässig und enthalten Epoch, Request-ID, Ergebniscode und Tick, einschließlich `ClassLimit` sowie `OutsideMap`, `OnPath`, `BlockedArea` und `TooClose`; unbekannte Codes werden abgewiesen. Alle Core-Rejections einschließlich RateLimited bleiben idempotent. Das Core-Limit von 40 neuen Requests/Sekunde liefert explizite Rejection-ACKs; ein deutlich höherer Paketflood (80/Sekunde mit 120 Burst) beendet die Verbindung vor Body-Allokation. Pending-UI maximal 16 Requests; fehlendes ACK nach 12 Sekunden bricht die Partie ab.

Jeder Turmdatensatz enthält seine tatsächliche Position, SiteId, den separaten Renderindex und DamageLevel/RangeLevel (30 Byte). Der Decoder prüft die Pfadstufen mit `TowerUpgradeRules.ValidateState`, bevor Statistikwerte benutzt werden: Bei Kampftürmen entspricht Level der Summe beider Pfadstufen, insgesamt höchstens zwei Käufe; bei Minen sind beide Pfadstufen null und Level bildet die Einkommensstufe ab. Invested muss separat exakt dem Gebäudegrundpreis plus den Preisen der ersten Level Ausbaukäufe entsprechen; der gewählte Pfad verändert diese Summe nicht. Negative Werte, zusätzliche Beträge und fehlende Kaufkosten verwerfen den gesamten Snapshot. Alle Türme werden gemeinsam mit `PlacementValidator.ValidatePosition` geprüft; nur die jeweils geprüfte Entity wird ignoriert. Pro eigenem Feld gelten 30 Kampftürme plus zwei Minen, eindeutige Renderindices 0–31 und identische Rand-/Weg-/Gelände-/Gebäudeabstände für Kampfgebäude und Minen. Freie Karten haben keine reservierten Minensites; jede SiteId ist −1. Gegner dürfen dieselben Koordinaten auf ihrem eigenen Feld verwenden. Legacy-Karten verlangen Slot = SiteId.

Snapshots sind kompakte, ungekomprimierte Binärzustände mit strikter Größen-, Werte- und Anzahlvalidierung, einschließlich Klassenlimits, freier Minengeometrie und gültiger Upgradepfade. Bewegungszustände gehen 5-mal/Sekunde als höchstens 896 Byte große Teile mit `NetworkDelivery.Unreliable` heraus; nur vollständig zusammengesetzte Frames werden übernommen. Maximal 16 KiB und 19 Teile, ein Frame gleichzeitig, 1,5 Sekunden Assembly-TTL. Ein monotoner Snapshot-Zähler verhindert Zustandsrückschritte auch bei mehreren Änderungen im selben Simulationstick. Welcome, Countdown, Revanche und endgültiges Resultat enthalten zusätzlich zuverlässige Zustände. Keine Transform-RPCs pro Renderframe. Kauf-Spam löst keine zusätzlichen Vollsnapshots aus; der Bewegungsstrom bleibt bei 5 Hz, während ACKs und Phasenwechsel separat zuverlässig übertragen werden. Maximalzustand mit 64 Türmen, 160 Einheiten, 160 Aufträgen: 7.764 Byte in neun Chunks, ungefähr 38.820 Byte/s Snapshot-Nutzdaten bei 5 Hz plus Chunk-, NGO-, UDP- und Heartbeat-Overhead. Integrierte Bytezähler messen anwendungseigene Nutzdaten, nicht gesamte Funk-/IP-Kosten.

Ein kompromittierter Host kann die Simulation manipulieren. Das ist eine dokumentierte Vertrauensgrenze privater Freundespartien; kein vollständiger Cheat-Schutz.

## Lebenszyklus

Ready/Countdown/Ergebnis/Revanche kommen aus der Core-Simulation. Revanche braucht beide Zustimmungen, erzeugt eine neue Epoch und verwirft alte Pending-IDs. Der Bot bestätigt offline seine Seite. Online-/LAN-Verbindung ist keine Host-Migration oder Wiederaufnahme einer alten Partie: Transportausfall, App-Hintergrund, ausbleibende Heartbeats oder eingefrorene Host-Simulation führen zum Abbruch ohne erfundenen Sieger. Bereits bestätigte Endergebnisse werden bei späterem Verlassen nicht umgeschrieben. Die UI kann sofort zum Menü und einen neuen Raum öffnen.

Manager wird innerhalb einer Coordinator-Lebensdauer wiederverwendet, mit genau einmal registrierten Transportcallbacks. Message-Handler werden pro Start registriert und beim Shutdown entfernt. Neuer Host/Join wartet auf das Ende des NGO-Shutdowns. Callbacks eines alten Shutdowns sind über `networkStarted` abgeschirmt. Realtime-Heartbeats 1 Hz, Transporttimeout 10 s, Fortschrittstimeout 12 s, Connecting 25 s. Die Simulation akkumuliert volle monotone Echtzeit, arbeitet höchstens acht Ticks je Frame nach und bricht Netzwerkpartien bei mehr als 12 Sekunden Rückstand ab. Offline pausiert bei App-Hintergrund, Netzwerkpartien werden beendet.

## Automatisierte Prüfungen

Tatsächlich ausgeführt:

```sh
DOTNET_CLI_HOME=/tmp/defense-dotnet-network \
  /Applications/Unity/Hub/Editor/6000.6.6f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet \
  run --project Tools/NetworkTests/WireHarness.csproj --configuration Release
```

Ergebnis für Protokoll 4 und Core v1.7: **PASS 57.490 Wire-Prüfungen**, 24 komplette Bot-Partien über alle drei Karten mit zustandsgetreuem Binär-Roundtrip, Host-Kartenübernahme, Reordering/Atomic-Assembly-Prüfung und jeder abgeschnittenen Länge des Maximalzustands. Dies ist kein Netzwerk-, Relay- oder Android-Nachweis.

Zusätzlich wurden **28/28 Wire-NUnit-Methoden direkt im .NET-Harness** und **53/53 Python-Belegtests** ausgeführt. Der direkte NUnit-Aufruf prüft Codec/Geometrie, startet aber keinen Unity-Editor. Unity-Compile, EditMode und echte Playerläufe werden separat vom Lead protokolliert.

NUnit/EditMode: `WireProtocolTests` deckt Kataloghash/Reihenfolge, alle drei Welcome-Karten, ungültige Karten/Revisionen, freie Goldmine/Klassenlimit, alle Upgradepfadkombinationen, unmögliche Pfadstufen, exakte Investitionen nach regulären Käufen aller fünf Klassen (negative/zusätzliche/fehlende Kosten), geänderter Pfad unter derselben Request-ID, fehlerhafte Handshakes, Bodygrenzen, IDs/Besitz, ACKs, alle Snapshotfelder, Volllast, Chunk-Duplikate/Reordering/alte Epoch/TTL und Flood-Burst ab. PlayMode: `SessionLifecycleTests` prüft Kartenwahl ausschließlich im Menü, zehn Offline-Neustarts, zehn LAN-Host/Shutdown-Zyklen, abgebrochenes Join mit anschließendem Offline sowie LAN-Hintergrundabbruch. Editorergebnisse werden vom Lead im TEST_REPORT eingetragen.

Zwei echte Desktop-Prozesse, nach dem Standalone-Build:

```sh
python3 Tools/NetworkTests/run_lan.py 'Builds/macOS/DefenseDuell.app/Contents/MacOS/Defense Duell' --map 1 --cycles 10 --output TestResults/lan-v4-clean
python3 Tools/NetworkTests/run_lan.py 'Builds/macOS/DefenseDuell.app/Contents/MacOS/Defense Duell' --map 2 --cycles 10 --impaired --output TestResults/lan-v4-impaired
```

Treiber `--duel-host`/`--duel-client` aktiviert sich ausschließlich durch diese Argumente. Client startet absichtlich auf Karte 0 und übernimmt die Host-Auswahl. Beide bestätigen Ready, bauen einen Kampfturm, kaufen erst Reichweite und dann Schaden, verkaufen, senden und bauen eine freie Goldmine; deren zusätzliches Einkommen muss auf beiden Seiten sichtbar werden. Jeder Kauf und Verkauf braucht seinen ACK sowie den passenden autoritativen Zustand. Mindestens drei gemeinsame Snapshots zeigen beide Türme mit RangeLevel = 1 und DamageLevel = 1 vor dem Verkauf. Der Vergleich verlangt mindestens drei gemeinsam beobachtete autoritative Snapshot-SHA256 **nach sämtlichen bestätigten Aktionen**. Dann werden dieselben Prozesse sauber getrennt und neu verbunden. JSON-Berichte und Logs liegen im gewählten Ziel. Der Python-Proxy manipuliert echte Datagramme: nominal 75 ms je Richtung = 150 ms zusätzliche RTT, ±15 ms Jitter, 2 % Verlust je Richtung, Seed 20261110; der tatsächliche Drop-Anteil steht in `proxy.json`. `UnityTransport.DebugSimulator` wurde im lokalen Paket geprüft: deprecated und ohne Effekt; wird deshalb nicht als Nachweis verwendet.

Für einen ausdrücklich genehmigten echten Relay-Test nach Verknüpfung des kostenlosen Cloud-Projekts:

```sh
python3 Tools/NetworkTests/run_relay.py 'Builds/macOS/DefenseDuell.app/Contents/MacOS/Defense Duell' --map 1 --cycles 1 --output TestResults/relay-v4-local
```

Dieser Launcher verwendet `--duel-relay-host`/`--duel-relay-client`, echte SDK-Aufrufe und DTLS. Nur der öffentliche Beitrittscode wird in `relay-code.json` ausgegeben; Allocation-Schlüssel und Auth-Tokens werden weder geschrieben noch geloggt. Host- und Client-Prozess verwenden die getrennten Auth-Profile `duel_relay_host` und `duel_relay_client`. Die Berichte prüfen unterschiedliche SHA-256-Fingerprints der anonymen Spieler-IDs, erfolgreiche reale Allocation/Join, Kartenübernahme, Aktionen und Zustandsübereinstimmung. Ein einzelner Client auf einem anderen Rechner kann mit `--duel-relay-client --duel-code CODE --duel-report /pfad/client.json` gestartet werden. Ein fehlendes Cloud-Projekt oder fehlgeschlagener SDK-Aufruf erzeugt einen Fehlerbericht, niemals einen simulierten Erfolg. Zwei Prozesse auf diesem Mac beweisen keine zwei getrennten Internetzugänge.

## Vollständige Partien und Revanche

Der opt-in Schalter `--complete-matches` (im Player `--duel-complete-matches`) spielt **zwei vollständige Partien über dieselbe Verbindung**, mit einer beidseitig bestätigten Revanche dazwischen. Er ist für beide Launcher verfügbar:

```sh
python3 Tools/NetworkTests/run_lan.py 'Builds/macOS/DefenseDuell.app/Contents/MacOS/Defense Duell' --map 1 --complete-matches --cycles 1 --output TestResults/lan-v4-complete
python3 Tools/NetworkTests/run_relay.py 'Builds/macOS/DefenseDuell.app/Contents/MacOS/Defense Duell' --map 2 --complete-matches --cycles 1 --output TestResults/relay-v4-complete
```

Nach dem üblichen Bau-/Pfadupgrade-/Verkauf-/Send-/Minen-Szenario schicken beide Spieler regelmäßig bezahlbare Läufer. Käufe benutzen ausschließlich die normalen validierten Befehle und Goldkonten. Es gibt keinen Kauf-Cooldown: jeder akzeptierte Tap bezahlt einen eigenen FIFO-Auftrag. Klassenabstände begrenzen erst den tatsächlichen Ausmarsch; `NextSendTicks` ist dessen frühester nächster Start. Höchstens 80 bezahlte Aufträge pro Spieler warten auf freie Feldkapazität. Es werden keine weiteren Kampftürme gebaut, keine HP direkt verändert und keine Simulationsbeschleunigung oder abweichenden Regeln verwendet. Jede Runde endet durch normalen Basisschaden oder das unveränderte Acht-Minuten-Limit. Die tatsächlich beobachtete Wallclock-Dauer wird mit `PlayTicks / 20` verglichen.

Jeder Peer protokolliert für beide Runden die Epoch, frische Startressourcen, leere Einheiten-/Turm-/Auftragslisten, sämtliche Standardaktionen mit beiden Upgradepfaden, Goldabbuchungen und Verkaufsrefund, freie Mine und Mineneinkommen, akzeptierte Truppenkäufe, das endgültige Resultat und den SHA-256 des vollständigen Endzustands. Nur die beiden nachträglich veränderbaren Revanche-Stimmflags werden für den Endzustandsvergleich auf false normalisiert; Gold, HP, Entity-Zustände, Tick, Seed und Resultat bleiben enthalten. Beide Resultate müssen mit dem anderen Peer übereinstimmen. Die zweite Runde braucht eine neue Epoch und frische Ressourcen. Die beidseitige Zustimmung wird durch ACKs beziehungsweise durch den autoritativ erfolgten Epochwechsel nach den beiden übermittelten Zustimmungen belegt.

Der Host wartet nach dem zweiten Ergebnis zwei Sekunden vor dem Shutdown, damit der zuverlässige Endzustand ankommt. Der Client erkennt das Verlassen auch dann korrekt, wenn `SessionCoordinator` das bereits bestätigte `Finished`-Ergebnis beibehält. `--cycles` bedeutet in diesem Modus Verbindungsszenarien mit jeweils zwei Partien; der Standard ist ein Szenario. Die Wartefrist erlaubt beide normalen Acht-Minuten-Partien. Ohne `--complete-matches` bleibt der kurze Abbruch-/Neustarttest erhalten. `test_comparison.py` prüft separat, dass fehlende zweite Resultate, alte Epochs, übernommenes Gold/Queues, einseitige Revanche, abweichende Endzustände und beschleunigte Zeitbelege nicht als bestanden gelten. Synthetische Fixtures dieser Unit-Tests sind keine Netzwerkbelege.

## Freie Platzierung im echten Netzwerktest

Jeder CLI-Lauf sucht auf der Host-Karte einen legalen Punkt, der keinem früheren Bauplatz entspricht. Beide Spieler bauen dieselben logischen Koordinaten auf ihren unabhängigen Feldern. Nach dem akzeptierten Bau wird dieselbe Request-ID zuerst mit identischem Punkt wiederholt (derselbe ursprüngliche ACK), anschließend mit X + 1 (zwingend `Duplicate`). Erst ein späterer autoritativer Zustand mit genau einem eigenen Turm an der unveränderten Position, SiteId = −1 und einmaliger Goldabbuchung gibt weitere Szenarioaktionen frei. Die Berichte enthalten Punkt, Request-/ACK-Daten, Turm-ID, Gold und Zustandstick. Der Vergleich verlangt zusätzlich drei gemeinsame Folgesnapshots. Der Belegvergleich verlangt Protokoll 4; frühere Player-Berichte können ihn nicht bestehen. Die Standardeingaben der Launcher schreiben nach `TestResults/lan-v4` beziehungsweise `TestResults/relay-v4`, direkte Playerläufe nach `network-v4` im persistentDataPath.

## Upgradepfade und freie Minen im echten Netzwerktest

Der Standardablauf kauft zuerst Range und dann Damage über den normalen Befehlsweg. Jeder Kauf wird mit seinem ursprünglichen Preis, Tick, Invested, Pfadstufen und den gemeinsamen Quote-/Stats-Werten belegt. Range darf nur die Reichweite verändern, Damage nur den Schaden. Der Treiber wartet auf bezahlbares Gold und prüft den Kontostand einschließlich des tatsächlich verstrichenen passiven Einkommens. Beide Türme bleiben nach den zwei Käufen mindestens 1,5 Sekunden sichtbar; der Vergleich fordert drei identische Snapshot-Digests beider Peers innerhalb des Zeitfensters nach den Pfadkäufen und vor dem ersten Verkauf. Der anschließende Verkauf muss den Refund aus dem gesamten Invested gutschreiben und den Turm entfernen.

Erst wenn beide Kampftürme verkauft sind, werden Truppen gekauft. So können Kill-Bounties die Upgrade- und Goldnachweise nicht verdecken. Die anschließende Mine steht auf einem legalen Punkt außerhalb sämtlicher früherer Bauplätze, hat SiteId = −1, Level/DamageLevel/RangeLevel = 0 und wird genau einmal bezahlt. Für die beobachtete Goldsumme werden passives Einkommen und Mineinkommen ab dem tatsächlichen Bau-ACK-Tick getrennt berechnet. Jeder komplette Revanche-Durchlauf muss diese Belege erneut liefern. Die Python-Tests verwerfen fehlende oder falsche Pfade, vorgezogene Snapshots, falschen Refund, reservierte Mine-Sites und falsche Goldabbuchungen.

## Sofortkäufe, Idempotenz und bezahlte FIFO

```sh
python3 Tools/NetworkTests/run_lan.py 'Builds/macOS/DefenseDuell.app/Contents/MacOS/Defense Duell' --map 1 --rapid-buy --cycles 1 --output TestResults/lan-v4-rapid-buy
```

`--rapid-buy` ist auch mit `--complete-matches` und im Relay-Launcher kombinierbar. Der Player-Schalter ist `--duel-rapid-buy`. Nach freiem Bau, beiden Pfadkäufen, Verkauf und einer frei platzierten Mine wartet der Treiber auf mindestens drei bezahlbare Läufer. Beide Kampftürme müssen verkauft sein, damit während der Goldprüfung keine Abschussprämien entstehen. Dann werden im selben Frame mehrere gleichartige Käufe einschließlich zusätzlicher unbezahlbarer Anfragen gesendet. Jede Antwort muss akzeptiert oder `InsufficientGold` sein; `Cooldown`, ausbleibende Antworten oder andere Rejections lassen den Test fehlschlagen.

Danach wiederholt jeder Peer exakt eine akzeptierte Request-ID mit identischem Payload. Host benutzt denselben Validator, Client dieselbe reguläre NGO-Nachricht. Der originale akzeptierte ACK einschließlich ursprünglichem Tick muss erneut ankommen. Normale UI-ACKs bleiben dedupliziert; nur der ausdrücklich aktive CLI-Test beobachtet die Wiederholungsantwort. Mindestens einen regulären Einkommenszeitraum später müssen Gold und die Summe aus bezahlten Warteschlangeneinträgen plus neu entstandenen Läufern genau den einmal akzeptierten Käufen entsprechen. Das verhindert einen falschen Erfolg durch ACK-vor-Snapshot oder doppelte Abbuchung/Spawns. Mindestens drei beidseitig gleiche Snapshots nach dem Burst sind zusätzlich Pflicht. Die JSON-Berichte enthalten Baseline, sämtliche Antwortzahlen, Replay-ID/-Ticks, beobachteten Zustand und die Dispatch-Dauer. Diese neuen Netzwerkläufe gelten erst mit echten Player-Berichten als ausgeführt.

Die drei Karten sind 1600 × 960 logische Einheiten groß. Kampfgebäude und Minen werden frei platziert; es gibt keine reservierten Minensites. Pro Spieler werden höchstens 32 Gebäude verwaltet. Der Wire-Decoder validiert Gebäudetyp, Geometrie und Upgradepfade zusätzlich zu Besitzer, Anzahl und IDs. Pure Wire-Tests prüfen 80/81 Aufträge pro Spieler, getrennte FIFO-Pakete, Kauf trotz aktiver Ausmarschzeit und byteidentische Duplicate-ACKs ohne erneute Zahlung. Python-Prüfungen des Belegvergleichs benutzen ausdrücklich synthetische Fixtures und ersetzen keine Verbindungstests.

Noch gesondert nachzuweisen: zwei Android-Geräte, echter Relay-Test über zwei Internetzugänge, 15 Minuten Volllast auf Samsung-Gerät inklusive Thermik/Frametimes/GC/Speicher. Tatsächlich ausgeführte Relay-/LAN-Läufe und ihr Versionsstand stehen im zentralen TEST_REPORT. Desktop- und Codecprüfungen ersetzen Gerätetests nicht.

## Tatsächliche Playerabnahme für Wire 4 / Core v1.7

Am 2026-11-10, 20:40–20:45 UTC wurden die folgenden vier Läufe mit dem gebauten Candidate 0.4.0 und Unity 6000.3.28f1 ausgeführt. Verwendet wurde `Builds/macOS-Candidate/DefenseDuell.app/Contents/MacOS/Defense Duell 0.4.0`, ausschließlich in eigenen `-batchmode -nographics`-Prozessen. Der bereits geöffnete GUI-Player wurde weder gesteuert noch geschlossen oder überschrieben.

| Lauf | Umfang | Ergebnis | Beleg |
| --- | --- | --- | --- |
| LAN sauber, Karte 1, Port 7787 | 10 Verbindungen mit vollständigem Aktions-/Shutdown-/Neustartzyklus | PASS, Exit 0/0; mindestens 7 gemeinsame MixedPath-Snapshots und 36 nach sämtlichen Aktionen je Zyklus | [comparison.json](../TestResults/lan-clean-v4/comparison.json) |
| LAN mit Störungen, Karte 2, Ports 7797/7798 | 10 Verbindungen, 75 ± 15 ms je Richtung, nominal 2 % Paketverlust | PASS, Exit 0/0; mindestens 6 gemeinsame MixedPath-Snapshots und 35 nach sämtlichen Aktionen je Zyklus | [comparison.json](../TestResults/lan-impaired-v4/comparison.json) |
| LAN vollständig, Karte 0, Port 7807 | 2 normale Partien, beidseitige Revanche, Rapid-Buy | PASS, Exit 0/0; beide Partien 2812 PlayTicks = 140,6 s, Player0, HP 1:0 | [comparison.json](../TestResults/lan-complete-v4/comparison.json) |
| Echtes Relay DTLS, Karte 1 | 2 normale Partien, beidseitige Revanche, Rapid-Buy | PASS, Exit 0/0; beide Partien 2733 PlayTicks = 136,65 s, Player0, HP 1:0 | [comparison.json](../TestResults/relay-complete-v4/comparison.json) |

Alle Läufe belegen ACKs und tatsächliche Range-/Damage-Stats, gemeinsame Snapshots mit beiden gemischten Pfaden, Goldabbuchungen, Verkaufsrefund, freie Kampf- und Minenpositionen sowie zusätzliches Mineinkommen. In jeder vollständigen Runde kauften beide Peers drei gleichartige Truppen im Burst; idempotente Wiederholungs-ACKs, einmalige Zahlung und bezahlte FIFO wurden bestätigt. Die vollständigen Endzustands-Digests stimmen überein. Beide zweiten Runden haben neue Epochs, Anfangsgold/HP und leere Entity-/Auftragslisten. Der Relay-Lauf hat echte SDK-Allocation/Join und unterschiedliche anonymisierte Spieler-Fingerprints nachgewiesen.

Der [UDP-Proxybericht](../TestResults/lan-impaired-v4/proxy.json) zählt tatsächlich 20 von 834 verworfenen Datagrammen Client→Host und 31 von 1614 Host→Client. Anwendungsnutzdaten Host gesendet/Client empfangen: Clean-LAN 183.026/182.860 B, impaired-LAN 182.296/178.819 B, vollständiges LAN 1.044.491/1.044.496 B und Relay 955.971/947.818 B. Diese Zähler enthalten keine vollständigen Transport-/Funkkosten; separat zuverlässig gesendete Kontrollnachrichten können kleine Unterschiede verursachen.

[network-v4-acceptance.json](../TestResults/network-v4-acceptance.json) fasst die Nachweise zusammen und referenziert den [Quellen-/Assemblymanifest](../TestResults/network-v4-source-manifest.json). Alle zwölf Quellen und fünf verwalteten Candidate-Assemblies stimmten vor und nach den Läufen mit dem Manifest überein. Während der Abnahme wurden keine Runtimequellen geändert. Der erste Sandboxversuch konnte UDP beziehungsweise den Playerstart nicht ausführen und erzeugte keine Spielnachweise; die vier Fehlerarchive unter `*-v4-sandbox-failed` bleiben erhalten. Die Wiederholung mit freigegebenen Sockets bestand ohne Runtimeänderung.

Versionsgrenze: Die im Checkpoint `9a509f2` gesicherten älteren echten LAN-/Relay-Läufe gehören zu Protokoll 3/Core v1.6 und bleiben unverändert. Die hier verlinkten Berichte gehören ausschließlich zu Protokoll 4/Core v1.7. Beide Relay-Peers liefen auf demselben Mac; zwei unabhängige Internetzugänge, zwei Android-Geräte und Samsung-Dauerlast werden dadurch nicht belegt.


## ArtReview 0.5: begrenzte Binary-Regression

Der neue ArtReview-Player 0.5.0 wurde am 2026-11-10 mit unverändertem Core v1.7/Schema 5/Wire 4 tatsächlich geprüft. Pro Transport lief genau ein eigenes Headless-Host/Client-Paar mit `--cycles 1 --rapid-buy --complete-matches`: LAN auf Karte 0/Port 7827 und echtes Relay DTLS auf Karte 1. Beide Läufe bestanden, alle vier Player beendeten sich mit Exit 0; die vier Logs enthalten keine Fehler-/Exception-Meldungen.

Die [LAN-Berichte](../TestResults/lan-artreview-0.5-v4/comparison.json) und [Relay-Berichte](../TestResults/relay-artreview-0.5-v4/comparison.json) belegen jeweils zwei reguläre Partien samt beidseitiger Revanche. Endzustände, Resultate und neue Epochs mit frischen Ressourcen stimmen überein. LAN endete zweimal nach 2812 PlayTicks (140,6 s), Relay zweimal nach 2733 PlayTicks (136,65 s); jeweils Player0 bei HP 1:0. In jeder Runde wurden beide Upgradepfade in sieben gemeinsamen Snapshots bestätigt, dazu freie Bau-/Minenpositionen, Gold/Verkauf, drei sofort akzeptierte Truppenkäufe je Peer und idempotente Wiederholungs-ACKs. Reale Relay-Allocation/Join und unterschiedliche anonyme Auth-Fingerprints sind nachgewiesen.

Das [0.5-Aggregat](../TestResults/network-v4-artreview-v05-acceptance.json) enthält die zeitgestempelte Vor-/Nachprüfung gegen den [0.5-Buildmanifest](../TestResults/network-v4-artreview-v05-source-manifest.json): alle zwölf Core-/Content-/Networking-Quellen sind bytegleich zur geprüften 0.4, sämtliche fünf Managed-Assemblies und die ausführbare Datei entsprechen unverändert den freigegebenen 0.5-Hashes. Ausführbare SHA-256: `821f76d222297d93ef09241f2522710a5da205f466e0a62e6571f5fbde731f9b`.

Die vorhandenen 20 Lifecycle-/Impairmentzyklen wurden nicht wiederholt. GUI-Player wurden weder bedient noch geschlossen oder überschrieben. Diese Headless-Regression bindet den neuen Binärstand an den bestehenden Netzwerkvertrag; die neue Art-/HUD-Darstellung und ihre interaktive Bedienung werden separat geprüft. Android-Hardware, getrennte Internetzugänge und Thermik bleiben außerhalb dieses Nachweises.
