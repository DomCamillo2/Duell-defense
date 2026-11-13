# Iteration 0.7 – gemeinsamer Integrationsvertrag

11. November 2026. Entscheidung des Hauptchats nach den vier übergebenen Entwürfen. **Status: 0.7 ist integriert; 123 EditMode-, 63 PlayMode- und 11 abschließende HUD-Tests bestanden. Mac-Vorschau gebaut. Aktuelle Build-/Transport-/Geräteabnahme siehe TEST_REPORT.md und PROJECT_STATE.md.** 0.6 / Commit590a183 samt APK, Quellenarchiv und Vorschauen bleibt unverändert verfügbar. Die unten gewählten Zahlen sind reproduzierbare Testparameter, keine behauptete finale Balance.

## Umfang und Entscheidung

Ein gemeinsames endliches Duell verbindet Grundwellen, bezahlte PvP-Verstärkung, Angriffseinkommen, Truppenforschung und kleine direkte Turmentscheidungen. Erste Konfiguration: zehn Wellen auf allen drei bestehenden Karten, zehn echte Sekunden Vorbereitung vor jeder Welle, gemeinsamer Frühstart und die nachfolgend festgelegten Werte. Solo verwendet dieselben Regeln mit Bot. Ein 100-Wellen-Modus, Kanonen-Immunitäten, aktive Frostfähigkeit und unbegrenzte Forschung sind nicht Bestandteil dieses ersten Integrationsumfangs. Nach echten Partien können Zahlen gezielt angepasst werden.

Implementierte Versionslinie: App0.7.0 / Android-Code7, Core-Regelnv1.9, Content-Schema6, Wire6. Diese Versionswerte wurden gemeinsam mit den implementierten Regeln aktualisiert. Kein anderer Chat hebt separat eine Version an. Neue Vorschau: `Builds/macOS-WaveReview/DefenseDuell.app`, Bundle `de.defenseduell.preview.waves`. Alle bisherigen Nutzervorschauen bleiben geschützt.

## Zeitmodell – zwei ausdrücklich getrennte Uhren

Die Integration ersetzt das bisherige Scheduling von20/40 Aufrufen pro Echtzeitsekunde durch **20 autoritative Kontrollschritte pro Echtzeitsekunde**. Ein öffentlicher `MatchSimulation.Step` bedeutet in0.7 einen solchen Kontrollschritt. Die Änderung ist bewusst versioniert; alte Tests dürfen ihre Bedeutung nicht stillschweigend behalten.

- `State.Tick` ist die monotone Kontrolltickzahl dieser Sitzung. Sie schreitet auch während Vorbereitung fort. ACK-Zeitpunkt, Befehlsrate, Lobby-Countdown, Bereitschaft und Vorbereitung verwenden diese Uhr. Der Coordinator akkumuliert unskalierte Echtzeit mit1/20s, unabhängig von Tempo-Zustimmungen. Netzwerkheartbeat und Verbindungsfristen bleiben echte monotone Echtzeit.
- `State.PlayTicks` ist ausschließlich aktive Gameplayzeit. Ein Kontrollschritt führt in aktiver Welle/Aufräumen einen oder bei beidseitigem2× zwei vollständige Gameplay-Schritte aus. Vorbereitung führt keinen aus. Einnahmen, Bewegung, Angriffe, Verlangsamungsende, Ausmarschabstände und Botentscheidungen hängen ausschließlich an Gameplayzeit.
- Alle bisherigen Gameplay-Zugriffe auf `State.Tick` werden gezielt migriert: `NextSendTicks`, `SpawnOrder.NextTick`, globale Ausmarschfristen und `SlowUntilTick` beziehen sich künftig auf `PlayTicks`; Turm-Restcooldowns werden nur im Gameplay-Schritt reduziert. Botplanung läuft pro aktivem Schritt und darf bei2× nicht auf halbe Denkfrequenz fallen. Exakte Einbindung koordiniert der Core-Owner; kein Unity-/Wandzeitbezug innerhalb des deterministischen Core.
- Lobby-Countdown bleibt separat. Danach startet Vorbereitung von Welle1 mit200 Kontrollticks. Beide für diese Welle bestätigten Bereitschaften oder der automatische Ablauf starten die Welle am nächsten Kontrollschritt. Keine rückwirkenden Gameplay-Schritte im bereits abgelaufenen Intervall. Übergang Vorbereitung→Welle verbraucht nur den Kontrollschritt; der erste Gameplay-Schritt folgt mit dem nächsten. Startet innerhalb eines Gameplay-Schritts Aufräumen→Vorbereitung oder Matchende, entfällt ein eventuell verbleibender zweiter2×-Schritt.
- Tempovoten bleiben über Wellen erhalten. Effektives Tempo ist in Vorbereitung1, in aktiver Welle/Aufräumen2 nur bei beiden Stimmen, sonst1. Eine Änderung wirkt für folgende Schritte; kein Multiplizieren bereits angesammelter Wandzeit. Revanche setzt beide Uhren und Voten zurück.
- Vorbereitung lässt Einkommen, Teilsekunde, Goldrest und Gameplay-Cooldowns unverändert. Bauen, Ausbauen, Verkaufen und Forschung dürfen vorhandenes Gold ausgeben. Der Befehlslimiter arbeitet weiterhin mit Kontrollticks und kann sich während der Pause regulär erneuern.

Keine abgeleitete Client-Uhr entscheidet über Kaufzulässigkeit oder Phasenwechsel. Der Client darf den angezeigten Countdown zwischen bestätigten Zuständen interpolieren, aber niemals dadurch eine Welle selbst starten.

## Wellen, Herkunft und Ende

`WavePhase = Preparing, Active, Clearing`; `WaveId` beginnt bei1, `WaveCount=10`. Der konkrete Neutralplan, deterministische Verschachtelung,0,5s planmäßiger Abstand und HP-Skalierung100…208% folgen der Zehner-Tabelle in `Docs/WaveSystemDesign.md`. Dieser Plan wird in gehashten Content übernommen, nicht aus dem HUD erzeugt. Neutraler Schaden/Tempo/Rüstung bleiben Basiswerte. Neutrale Gegner haben Herkunft `NeutralWave`, private Forschung0 und kein Kopfgeld. Spielertruppen tragen `PaidPvP`; ihr Sender ist der reale Gegner des verteidigten Feldes, kein künstlicher dritter Spieler.

Das Sendefenster endet nach `max(12 Sekunden, letzter planmäßiger Neutralspawn +4 Sekunden)` aktiver Zeit seit Wellenbeginn. Kaufzuordnung verwendet den Zustand bei Befehlsannahme; es gibt keine rückwirkende Umbuchung. Nach Fensterende beginnt Clearing. Bereits zugeordnete Spawns laufen weiter. Erst wenn auf beiden Feldern keine aktuellen Gegner und keine offenen aktuellen Neutral-/PvP-Spawns existieren, beginnt die nächste Vorbereitung. Zukünftige Aufträge blockieren den Übergang nicht. Wenn alles vor Fensterende leer ist, bleibt die Welle bis zum Cutoff offen.

Beide Felder werden vollständig innerhalb desselben Gameplay-Schritts aufgelöst, bevor Basiszerstörung entscheidet. Beidseitiger Basisverlust im selben Schritt bedeutet Remis. Nach der letzten vollständig geräumten Welle gewinnt die höhere verbleibende Basis-HP; gleiche HP bedeuten Remis. Die bisherige Acht-Minuten-Endregel gilt in diesem Wellenmodus nicht zusätzlich. In Finished/Aborted erfolgt keine weitere Gameplaybuchung. Langsames Aufräumen und hohe Remisrate bleiben Messpunkte, keine durch das Modell gelösten Balancefragen.

## Kaufquote, Reservierung und fairer Ausmarsch

Je Spieler/Welle können höchstens240 Gold **tatsächlich bezahlter Truppenpreis** reserviert werden. Das ist kein Wallet, kein Gratisgold und keine zusätzliche Abbuchung. Restkapazität wird weder ausgezahlt noch übertragen; ein Spawn gibt reservierten Wert nicht erneut frei. Reservierungshorizont: früheste zulässige Welle bis einschließlich10. Ganze Pakete werden zugeordnet, Zielwellen in der FIFO sind monoton; spätere Pakete dürfen nicht vorziehen. Maximal80 wartende Aufträge je Sender bleibt eine unabhängige Grenze.

Eine gemeinsame reine Quote liefert mindestens Fehlergrund, Klasse, Preis, aktuelle Forschungsstufe und konkrete Zielwelle. Der Kauf bestätigt erwartete Zielwelle und erwartete Forschungsstufe. Der Host berechnet dieselbe Quote erneut. Abweichung bedeutet Ablehnung ohne Abbuchung/Reservierung; UI aktualisiert den Vorschlag, **kein automatischer Kauf-Retry**. Wiederholte identische Requests liefern ihren ursprünglichen Beleg ohne Mutation; geänderte Payload unter derselben Sequenz wird abgewiesen. Alle zusätzlichen Erwartungsfelder gehören zur Requestidentität.

Ein akzeptierter Auftrag bindet Klasse, Forschungsstufe, bezahlten Preis, ursprüngliche Anzahl, verbleibende Anzahl und Zielwelle unveränderlich an die Sitzung/Contentversion. Nachträgliche Forschung verändert weder Queue noch schon vorhandene Einheiten. Auch ein teilweise ausmarschierter Schwarm behält seine gekaufte Stufe. Eine Klasse mit vier Mitgliedern ist ein Paket, keine vier separat nachforschbaren Käufe. Ein Kauf ohne verbleibende zulässige Welle wird vor jeder Mutation abgewiesen. Frühe Niederlage/Abbruch bleibt das Risiko bezahlter, noch wartender Aufträge.

Grundwellen haben ihren eigenen begrenzten Plan und verbrauchen keine bezahlten Auftragsplätze. Gemeinsame Grenze80 lebende Einheiten/Feld; maximal64 derselben Herkunft, nicht ausleihbar. Höchstens ein tatsächlicher Einzelspawn/Feld alle vier aktiven Ticks. Sind beide Quellen berechtigt, wechseln sie sich ab; nach jedem tatsächlichen Spawn erhält die andere beim nächsten Konflikt Vorrang. Ist nur eine bereit, nutzt sie den Slot. Klassenabstände der bezahlten FIFO bleiben20/30/70/40 aktive Ticks, keine Überholung innerhalb der Quelle. Ein blockierter Spawn löscht nichts, verbraucht keine Erstspawnfrist und erzeugt kein Einkommen.

Bei den festgelegten Preisen/Gruppen sind höchstens24 PvP-Körper je Welle reservierbar; zusammen mit höchstens48 neutralen Körpern ergeben sich72. Diese Grenze ist für genau diesen Content nachgerechnet und muss bei Preis-/Anzahländerung neu geprüft werden. Sie ersetzt nicht die allgemeinen80/64-Limits.

## Wirtschaft – erster Testkandidat

Startgold180, Geldgrenze10.000, Basis4Gold/aktiver Sekunde. Maximal zwei Minen, Ertrag je Mine1/1,5/2Gold pro aktiver Sekunde; Bau-/Ausbaupreise140/100/160 bleiben zunächst bestehen. Verkauf65% der tatsächlichen Investition, Truppenpreise18/24/65/38 und PvP-Kopfgeld4/5/14/2 je Körper unverändert. Leaks zahlen kein Kopfgeld. Neutrale Abschüsse zahlen0; eine spätere Neutralprämie benötigt eine eigene Gegenrechnung.

Nur tatsächliche PaidPvP-Spawns aktivieren dauerhaft `min(3, ausmarschierter Kaufwert/240)`Gold/s. Nenner240 und Wellenbudget240 sind zwei getrennte Contentfelder. Reservierung, Forschung, Kaufannahme, ACK und neutrale Spawns aktivieren nichts. Der Bonus bleibt über Wellen; Reset mit neuer Sitzung/Revanche.

Für den gewählten Nenner240: Einkommen in Einheiten von1/960Gold/s; Basis3840, Mine960/1440/1920, Angriff höchstens2880. Ein Paket mit PreisP und AnzahlC erzeugt beim n-ten tatsächlichen Spawn genau `floor(4*P*n/C)-floor(4*P*(n-1)/C)` Angriffseinheiten, danach Deckel. Zwischenprodukte werden in ausreichend breitem Integer gerechnet, ungültige Werte abgewiesen. Sekündlich alle Einkommen addieren, ganze Goldstücke buchen und Rest0…959 behalten. Reihenfolge pro Gameplay-Schritt: aktive Zeit fortschreiben, fälliges Einkommen, Spawns/Bonusaktivierung, Kampf, Bewegung, Ende/Phasenwechsel. Neu gespawnter Bonus verdient erst ab der nächsten Einkommensbuchung. Am MaxGold ganze überschüssige Auszahlung verwerfen; kein verborgenes späteres Guthaben.

Zusätzliche Bilanzfelder je Spieler/Welle: reservierter Kaufwert und kumulativ ausmarschierter Wert in Viertelgold-Einheiten. Zusammen mit den offenen Paketanteilen müssen sie exakt das reservierte Budget ergeben. Zukünftige Wellen haben keinen ausmarschierten Wert. Angriffseinheiten sind aus dem kumulierten tatsächlichen Ausmarsch ableitbar, vor dem Deckel. Solche Konsistenzprüfungen gehören in den Snapshotvalidator, nicht nur ins HUD.

Nenner180/300 sind spätere gezielte Sensitivitätsvarianten; dafür exakte gemeinsame Recheneinheiten wählen und Paketanteile neu prüfen. Die960-Konstante darf nicht unverändert eine andere Rendite vortäuschen. Keine automatische dynamische Anpassung an Restwellen.

## Forschung und Zielpriorität

Je Spieler vier Klassenstufen0…8. Pro Stufe Läufer+4HP/+2Tempo, Sprinter+1HP/+8Tempo, Tank+44HP, Schwarm+3HP je unverändert vier Körpern. Nächster Preis bei aktueller StufeL<8: `B + S*L +5*L*L`, B40/45/130/70, S20/25/40/25. Bei8 ausdrücklich MAX. Gesamtkosten bis8:1580/1760/2860/1960. Sendpreise, Rüstung, Basisschaden, Anzahl, Kopfgeld und Klassenabstände bleiben unverändert. Forschung kostet separat Gold, erzeugt weder Einkommen noch Wellenreservierung und bleibt über Wellen erhalten.

Ein gemeinsamer `UnitResearchRules`-/Rezeptresolver liefert Quote, aktuelle/maximale HP und Tempo für Core, Bot, UI und Wirevalidator. PaidPvP nutzt die beim Kauf gebundene Stufe; NeutralWave nutzt nur Basiswerte und die eigene Wave-HP-Skalierung. Keine Heilung/Tempozunahme alter Einheiten. Bei einem Forschungskauf bestätigt der Request die angezeigte aktuelle Stufe; eine andere autoritative Stufe führt zur Ablehnung ohne Abbuchung. Neue Sitzung setzt Forschung auf0.

Kampftürme erhalten `TargetPriority = Front, Strongest`, StandardFront. Front: weitester Wegfortschritt, dann kleinste stabile ID. Strongest: höchste **aktuelle** HP innerhalb des Radius, dann weitester Wegfortschritt, dann kleinste stabile ID. Zielwechsel erhält den Schusscooldown; kein Gratisangriff. Minen haben keine Zielprioritätsaktion. Die FIFO beschreibt Ausmarschreihenfolge, keine geschützte Formation: schnellere Truppen können Tanks überholen. Keine Taunt-/Kollisionsregel wird eingeführt.

## Geplante Command-/State-Schnittstelle

Bestehende Commandnummern0…6 bleiben. Neu: `SetWaveReady=7`, `ResearchUnit=8`, `SetTowerPriority=9`. `CommandEnvelope` erweitert um `ExpectedWave`, `ExpectedResearch`; vorhandene Felder bleiben. Alle Felder werden explizit validiert und in Identität/Replay einbezogen. Wire6-Kommandogröße damit41Bytes statt33. Nicht verwendete neue Felder müssen0 sein.

| Befehl | Payload |
| --- | --- |
| SendUnit | Kind=Klasse, Target=-1, XY=0, ExpectedWave=konkrete1…10, ExpectedResearch=0…8. |
| SetWaveReady | Kind=0 oder1 für ausdrückliche Rücknahme/Zustimmung, Target=aktuelle WaveId, XY=0, neue Erwartungsfelder0. Nur Preparing. Beide gültigen Stimmen starten früh am nächsten Kontrolltick. |
| ResearchUnit | Kind=Klasse, Target=erwartete aktuelle Stufe, XY=0, neue Felder0. Nur Playing, alle Wellenunterphasen. |
| SetTowerPriority | Kind=0Front/1Strongest, Target=eigene Kampfturm-ID, XY=0, neue Felder0. Nur Playing; gleiche Einstellung idempotent ohne Cooldownänderung. |

Alte Fehlernummern bleiben; ergänzen `QuoteChanged=21`, `WaveCapacity=22`. QuoteChanged bei geänderter bestätigter Forschungsstufe/Zielwelle; WaveCapacity bei keiner verbleibenden zulässigen Welle. Ungültige Wellen-/Levelwerte bleiben InvalidValue; falsche Phase WrongPhase, fremder Turm NotOwner, Mine als Ziel InvalidKind. Keine ungültige Eingabe durch Clamping akzeptieren.

State ergänzt mindestens: Wellenphase/-ID, Vorbereitung-Restkontrollticks, Wellenbeginn in aktiven Ticks, Wellenbereitschaft pro Spieler, neutraler Plancursor je Feld, globale Ausmarschfrist je Feld und nächster Quellvorrang; vier Forschungsstufen je Spieler, Angriffseinheiten/Goldrest, pro Welle reservierter/ausmarschierter Wert. TowerState erhält Priorität. SpawnOrder erhält gebundene ResearchLevel/PaidPrice/InitialCount/WaveId; Herkunft dort explizitPaidPvP. UnitState erhält Origin/WaveId/ResearchLevel; PaidPvP-Sender ist aus Defender ableitbar. Effektive Stats sind aus unveränderlichem Content plus Rezeptschlüssel ableitbar, keine vom Client frei gelieferten HP-/Speedboni.

## Netzwerk, Content und Validator

Content-Schema6 enthält Wellenpläne, Forschungskoeffizienten, Einheiten für Einkommenswerte, zwei getrennte240-Parameter und alle Grenzen. Contenthash muss alle neuen Regelwerte einschließen. Standard.asset und Import/Migration zusammen aktualisieren. Historische0.6-Dateien werden nicht passend umgedeutet; neue Builds müssen passende Content-/Porträthashes erzeugen.

Wire6 lehntWire5 und ältere Clients ab. Snapshot/Tower/Unit/Order-Datensätze und alle Clock-/Plan-/Budgetfelder vollständig versionieren. Höchstens10 Wellen in diesem ersten Schema, vier Forschungsstufen je Spieler,64 Türme,160 aktive Einheiten,160 bezahlte Aufträge, statisch begrenzte Neutralpläne. Strikte Boolean/Enum/Count/HP/Level/Preis-/Rezeptprüfungen; maximal gültige Payload anhand des konkreten Writers neu berechnen und unter dem bestehenden16.384-Byte-Limit nachweisen. Falls sie nicht hineinpasst, zuerst DTO prüfen; keine stillschweigende Vergrößerung. Exakte Chunk-/5Hz-Bandbreite aus realen Bytes berichten.

Snapshots müssen aktuelle Phase, Tempo, Wellen-/Forschungsbereitschaft, Reihenfolge und Budget konsistent wiedergeben. Alte Revisionen dürfen neuere Zustände nicht zurücksetzen; neuere Zustände im selben Kontrolltick sind zulässig. Reliabler kompletter Zustand nach akzeptierter Forschung, Wellenbereitschaft, Priorität, Tempo und Phasenwechsel; weiter gebündelte5Hz-Bewegung statt State-RPC je Spawn/Kauf. Kein vertrauter Client-Gold-/Wellen-/Statwert. Timeout und Heartbeat laufen auch in Preparing. Kein Anspruch auf Hostmigration oder Join mitten in einer laufenden Partie.

## Bedienung und Bot

HUD: WelleX/10, Unterphase, echte Vorbereitungsrestzeit, beide bestätigten Bereitschaften, getrenntes Tempo. Vorschau der nächsten bekannten Grundwelle inklusiveHP-Stärke; keine gegnerische private Kaufqueue. Eigene FIFO zeigt bestätigte Zielwelle, gekaufte Stufe, verbleibende Körper und Wartegrund. Vor dem Direktkauf konkrete Zielwelle und Restkapazität sichtbar, auch bei ferner Vorbestellung. Pending nicht als bezahlt oder bestätigt ausgeben. Kein zweitesGold-Wallet; Kapazität als Truppenwert der Welle beschriften. Letzte volle Welle sperrt Sendekauf mit Grund.

Klasseninfo bekommt separate Forschungsaktion, Stufe/HP-/Tempowirkung/Preis und Hinweis auf neue Käufe. Die bestehenden Turm-Ausbaudiamanten bleiben Gebäuden vorbehalten. Forschungsaktion innerhalb des Infopanels benötigt einen gezielten erlaubten Pfad; bestehende Info-/Modal-/Held-Pointer-Sperren für darunterliegende Käufe bleiben erhalten. Keine Click-through-Ausnahme für das gesamte HUD. Neues Info-/Rebuild-/Pendingverhalten mit realem EventSystem testen.

Eigene Bedrohungen kurz vor Ausgang dürfen markiert werden, ohne geheime Gegnervorschau. Prioritätswahl im Turmpanel; keine zusätzliche Frosttaste in diesem Paket. Der Bot nutzt dieselben Quotes/Budgets/Commands, reagiert auf echte Forschungsstats und entscheidet in aktiver Gameplayzeit. Vorbereitungsaktionen und Bereitschaft sind begrenzt und deterministisch; er darf die automatische Deadline nicht umgehen. Forschung nur mit Verteidigungsreserve und erwarteter Nutzung, kein unbegrenztes Restgold-Spamming.

## Dateizuständigkeiten und Ablauf

Hauptchat ist alleiniger Integrations-/Release-Owner. Diese Spezifikation erteilt keine stillschweigende Schreibfreigabe an frühere Entwurfs-Owner. **Root besitzt zunächst die gesamte Runtime-Integration**: Assets/Core inklusiveBotBrain, Assets/Content, Assets/Networking, Assets/Presentation sowie Tests/Editor/Versionen. Teilbereiche dürfen erst mit ausdrücklicher Dateiübergabe an genau einen Owner delegiert werden. Entwurfschats haben ihre Analyse übergeben und bleiben bei Rückfragen read-only. Keine parallelen Unityläufe oder konkurrierenden Dateiänderungen.

Umsetzungsreihenfolge: (1) Clock-/State-/Content-/Quotegrundlage plus reine Coretests, (2) Wellen/Reservierung/Herkunft/Income/Forschung/Bot plus deterministische integrierte Partien, (3) Wire/Coordinator und echte zwei Prozesse, (4) HUD/Priorität/Lesbarkeit mit Input- und Bildprüfung, (5) neue separate Vorschau, APK, Frischimport, Quellenarchiv und aktualisierte Einzelabnahme. Unabhängige Balanceparameter erst nach dem ersten konsistenten Kandidaten ändern. Ein fehlender Mensch-/Gerätetest darf nicht als PASS ausgegeben werden.

## Abnahme und Grenzen

Vor der neuen Spielversion nachweisen: Pausenzeit1×/2×, unveränderte Teilsekunde/Cooldowns, Rate-Limit in Pause, keine Rest-Zeitsprünge; gleiche aktive Geldbilanz bei gleicher Gameplayzeit; neutrale vs. bezahlte Herkunft; Quote-/Research-Races ohne Mutation; Teile eines Schwarmpakets vor/nach Forschung; alle Wellenreservierungen inklusive letzter Welle; volleFIFO/80/64-Grenzen, Quellenfairness und kein Überholen; Endlichkeit/Phase/gleichzeitiger Verlust/Rematch; beide Spieler unabhängig; alte/neue Snapshotrevisionen; vollständige LAN-/Relay-Partien; Native-/HUD-Sichtprüfung und alte Pointer-Sperren.

Gameplaymessung mit echten integrierten Core-Partien auf allen drei Karten und Seitentausch: aktive/echte Dauer, Wellenende, erste Durchbrüche, Gewinner/Remis, Goldquellen/-ausgaben/-verluste, Bank/Forschung, Klassenanteile, Peakfeld/Queuealter, Minenupgrades und Slow-Stalling. Vergleiche keine Forschung, gezielte frühe Klassenforschung, verteilte Forschung, defensiver Aufbau und gemischte Angriffe. Kontrollierte Modellproben sind keine menschlichen Fun-/Balancebeweise. Danach kurze menschliche Partien; Android-/Touch-/Thermik-/FPS-Abnahme wie bisher separat.

Prüfbasis dieser Entscheidung: Economy36 Sensitivitätszeilen nachgerechnet; Wellen15.752 isolierte Modellassertions reproduziert; Forschung408 Kandidatenchecks,1.296 Single-Packet- und108 Korridorproben in separatem Ausgabeordner reproduziert, fünf Ergebnisdateien bytegleich. Keine dieser Prüfungen führt bereits die kombinierte0.7-Simulation aus. Historische0.6-Builds/Nachweise bleiben unverändert.

## Ergänzung: direkter Karten-Art-Folgeauftrag

Karten sollen visuell markanter werden. Die enge Übergabe im aktuellen PROJECT_STATE.md erlaubt dem Ansichts-Chat ausschließlich den neuen MapLandmarkArt-Helper und eigene isolierte Nachweise. Bestehende Terrain-/Board-/Gameplaydateien bleiben beiRoot. Kein Terrainhöhen-Spielmechanismus oder neues Weglayout. Einbau erfolgt erst nach den drei Vorher/Nachher-Ansichten und Prüfungen der spielbaren Flächen; kein paralleler Hauptprojekt-Unitylauf.


## Implementierte Erweiterung im aktiven Integrationsfenster

Nach den weiteren direkten Nutzerwünschen umfasst der finale Testkandidat zusätzlich zwei ursprüngliche Kampfklassen, lokale Siegesmarken und drei Botstufen. Keine vierer Loadoutbegrenzung: alle freigeschalteten Klassen dürfen gebaut werden; Index4 bleibt Mine. Index5 Blitzspule (Kette bis3 verschiedene Ziele,75 Sprungradius,1000/650/400‰), Index6 Balliste (Primärziel plus höchstens2 Ziele dahinter auf demselben Strahl innerhalb Turmreichweite, Halbbreite18,1000/700/450‰). Primärziel nach Turmpriorität; Chainfolge nach Distanz/ID, Linienfolge Projektion/ID. Preise/Stats in ArtCandidates/TowerUnlocks/v1/catalog.json. Keine Balancefreigabe allein aus den Modellbildern.

PlayerState.AvailableTowerMask startet31 (Klassen0…4), darf nur gültige Katalogbits enthalten. ConfigureTowerAvailability ist nur in Lobby zulässig; Spielbefehle ändern den Mask nicht. Host validiert Build gegen diesen Mask, neuer FehlerTowerLocked23. Wire6 bindet die erklärte lokale Freischaltung an Approval/State; Solo-Bot nutzt denselben Mask. Dies ist lokale Spielprogression, kein manipulationssicherer Onlineaccount. Revanche erhält Freischaltungen, setzt Matchforschung/Geld/Tempovoten zurück.

Ein echter Sieg gibt100 Siegesmarken; Niederlage/Remis/Abbruch0. Blitzspule kostet200, Balliste300. Jede live beobachtete nichtterminale Sitzung bindet vor Belohnung eine neue persistierte GUID mit monotonem MatchCounter; Endmeldungswiederholungen buchen nicht erneut. Ein ungebundener bereits fertiger Snapshot erzeugt keine neue Prämie. Atomare lokale Speicherung und validierte Sicherung; beschädigte Dateien werden nicht stillschweigend gelöscht. Freischaltung nur im Hauptmenü, keine Veränderung des laufenden Matchgoldes. Lokale Daten werden nicht als Cloudsync beworben.

Modelle: zwölf tatsächliche Meshvarianten (Basis/D1/R1/D2/D1R1/R2) mit jeBase/Head, gemeinsames Feldmaterial; HeadPivot(0,.42,0). Originaldaten in Assets/Art/UnlockModels/Source, reproducible Editorimport,39 Turm- plus4 Truppenporträts aus tatsächlichen Feldmeshes. Kartenhelper ersetzt genau die ausdrücklich zugeteilten vorhandenen Sperrflächen, keine neue Bau-/Weggeometrie.

Bot Easy/Normal/Hard verwendet dieselben Regeln und Preise. Schwierigkeit beeinflusst Denkintervalle, Aktionsauswahl und Planung, keine HP-/Gold-/Schadensboni. Vollständige legale Platzsuche bleibt erhalten. Technische deterministische Partien und Schwierigkeitvergleich müssen noch gegen den finalen gemeinsamen Quellstand bestehen.
