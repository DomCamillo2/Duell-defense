# Unabhängiger Netzwerk-Review

Datum: 10. November 2026. Reviewer: Gameplay-Agent, nicht Autor des Netzwerkcodes. Geprüft: `Assets/Networking/SessionCoordinator.cs`, `Assets/Networking/WireProtocol.cs`, die Netzwerk-Testfälle sowie die zwei Prozess-Testtreiber. Netzwerkdateien wurden vom Reviewer nicht verändert; Korrekturen liegen beim Networking-Autor.

## Ergebnis und Befunde

Keine P0-/P1-Sicherheits- oder Wirtschaftsfehler im geprüften Stand gefunden. Das ist ein gezielter Quellcode-Review mit unabhängigen Decoder-Proben, kein vollständiger Penetrationstest und kein Android-/Relay-Nachweis. Ein manipulierter Host bleibt die dokumentierte Vertrauensgrenze.

| Befund | Priorität | Trigger / Auswirkung | Status |
|---|---|---|---|
| Snapshot-Grenzen stimmten nicht mit Spielregeln überein | P2 | Decoder akzeptierte Gold oberhalb `MaxGold`, Frostfaktor unter `MinimumSlowPermille` sowie `Finished/None` oder `Playing/Draw`; ungültige Zustände konnten dargestellt werden | Behoben und unabhängig nachgeprüft |
| Erfolgreiche Authentifizierungs-Task dauerhaft gecacht | P2 | Nach späterem SDK-Sign-in-Verlust führte erneutes Online-Host/Join keine `IsSignedIn`-Prüfung mehr aus; potenzielle 401-Schleife bis App-Neustart | Behoben; Quellcode nachgeprüft, realer Auth-Ablauf mangels Cloud-Zugang nicht ausgeführt |
| Verwerfen von Simulationszeit bei Frames über 250 ms | P2 | `Min(deltaTime, .25)` lässt bei anhaltenden 2 FPS nur 10 Ticks/s zu; das Acht-Minuten-Spiel kann 16 reale Minuten dauern, obwohl Heartbeats und einzelne Ticks weiterlaufen | Behoben; vollständige monotone Echtzeit wird akkumuliert, höchstens acht Catch-up-Ticks pro Frame; >12 s Rückstand bricht Netzwerkpartie ab |
| LAN-Beleg verglich auch ausschließlich frühe Snapshots | P2 | Drei gemeinsame Lobby-/Countdown-Snapshots konnten genügen, ohne Zustand nach dem letzten akzeptierten Verkauf ausdrücklich zu vergleichen | Behoben; mindestens drei gleiche Snapshots strikt nach dem letzten akzeptierten Befehl beider Spieler werden verlangt; Comparator unabhängig geprüft |

Die Snapshot-Korrektur wurde mit einem separaten temporären .NET-8-Projekt geprüft, das ausschließlich den tatsächlichen Core und `WireProtocol.cs` kompiliert. Probe: Standardzustand muss lesbar sein; vier bewusst verfälschte Varianten müssen verworfen werden. Tatsächliches Ergebnis:

```text
PASS Gold above canonical cap
PASS Frost below canonical floor
PASS Finished without outcome
PASS Playing with finalized outcome
REVIEW_PROBES failures=0
```

Ausgeführter Befehl:

```sh
DOTNET_CLI_HOME=/tmp/defense-dotnet-home \
DOTNET_CLI_TELEMETRY_OPTOUT=1 \
"/Applications/Unity/Hub/Editor/6000.6.6f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet" \
  run --project /tmp/defense-network-review/Review.csproj --configuration Release
```

Die dauerhafte Regression liegt zusätzlich in `WireProtocolTests.SnapshotRejectsImpossibleEconomySlowAndOutcomeValues`. Der Auth-Fix verwendet jetzt nur eine noch laufende Auth-Task erneut; nach Abschluss wird `AuthenticateAsync` wieder aufgerufen und prüft den aktuellen SDK-Zustand. Cancellation- und Generation-Guards bleiben bestehen.

Der geänderte LAN-Comparator wurde unabhängig direkt importiert und mit zwei synthetischen Befunddatensätzen aufgerufen: Drei identische Snapshots ausschließlich vor `lastAcceptedActionTick=100` werden abgewiesen; drei zusätzliche identische Snapshots bei Tick 110/120/130 werden angenommen. Tatsächliche Ausgabe: `PASS early-only snapshot evidence rejected`, `PASS post-action matching snapshot evidence accepted`. Das prüft die Beweisregel, behauptet aber keinen echten Netzwerklauf. Der Zeitakkumulator-Fix wurde im Quellcode geprüft; Offline-Pause setzt zusätzlich seine Zeitbasis zurück, damit bei Rückkehr keine Hintergrundzeit nachsimuliert wird.

Alle vier gemeldeten Befunde sind im nachgeprüften Quellstand geschlossen. Auth-Wiederanmeldung und Android-Überlast-/Hintergrundverhalten brauchen weiterhin reale Integrations- beziehungsweise Gerätetests.

## Geprüfte Vertrauens- und Lebensdauergrenzen

- Nur der Host verarbeitet Commands. Der entfernte Sender muss exakt der genehmigten Verbindung entsprechen; Core erhält Slot One aus der Verbindung, niemals aus vom Client behaupteten Spieler-/Goldwerten. Lokale Hostaktionen gehen ebenfalls durch `Execute`.
- Commands enthalten nur Session-ID, Sequenz, Aktion, Klasse und Ziel. Die Bytezahl ist begrenzt. Eine vorgeschaltete hohe Draht-Ratenbegrenzung beendet echte Floods; normale Gameplay-Limits laufen durch den idempotenten Core-Antwortcache.
- ACKs tragen Epoch und Sequenz. Ein alter ACK kann keine ausstehende Aktion der neuen Runde bestätigen. Pending-Commands sind begrenzt; fehlende Bestätigung beendet die Partie nach einer Frist ohne erfundenen Sieger.
- Protocol-/Content-Handshake wird bei Connection Approval und beim Welcome geprüft. Ein Dritter bekommt keinen zusätzlichen Spielerslot. Szenen-/Prefab-Synchronisation ist deaktiviert.
- Bewegungssnapshots sind auf 16 KiB und kleine Einzelchunks begrenzt. Der Assembler hält höchstens einen Frame, verwirft falsche Epochs, Duplikate und alte Sequenzen und übernimmt nur vollständige Frames. Unzuverlässige Bewegung und zuverlässige Phasenwechsel verwenden gemeinsam monotone Revisionen, sodass ältere Daten nicht neuere Aktionen im selben Tick zurücksetzen.
- `Shutdown` hat einen Besitzer, entfernt benannte Message-Handler, beendet Coroutines, verwirft Pending-Kommandos und erhöht eine Generation. Asynchrone Relay-Fortsetzungen prüfen Cancellation/Generation vor Netzwerkstart. Start wartet auf den vorherigen NGO-Shutdown. Alte Callbacks sind durch `networkStarted`/`stopping` abgeschirmt.
- Heartbeats müssen eine neue Sequenz derselben Epoch tragen. Clients überwachen zusätzlich Tickfortschritt während Countdown/Playing. Android-Pause bricht Netzwerkpartien bewusst ab; Training wird pausiert. Kein Host-Migrations-/Reconnectversprechen.
- Ein bereits abgeschlossenes Ergebnis bleibt bei späterem Verbindungsverlust bestehen; eine laufende Partie wird abgebrochen. Die erneute Verbindung erfolgt über eine neue Sitzung.
- Online verwendet die Relay-API aus dem aktuellen Multiplayer Services SDK mit DTLS. Offline/WLAN initialisieren weder Services noch Authentication. Keine Zugangsdaten werden in UI-Fehlern ausgegeben.

## Noch getrennt zu belegen

Zwei tatsächliche Desktop-Prozesse, Verlust-/Latenztest, zehn Neustartzyklen, vollständige LAN-Partie und beidseitige Revanche sind Integrationsnachweise des Leads/Networking-Autors und müssen anhand ihrer Ergebnisdateien bewertet werden. Quellcode-Review ersetzt diese Läufe nicht. Echter Relay-Verkehr über zwei Internetzugänge, Android-Hintergrund/Sperren/WLAN-Ausfall und 15-Minuten-Geräteprofiling bleiben ohne entsprechende Hardware-/Cloud-Belege ungeprüft.
