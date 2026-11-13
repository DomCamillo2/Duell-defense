# Defense Duell – verbindlicher Projektstand

Stand: 11. November 2026. Hauptchat `01a1c67c-8801-7cf2-9b86-2b5353ef4b76`, Host local, ist alleiniger Integrations-/Release-Owner. AGENTS.md und Docs/CONTRACT.md beachten.

## Letzter gebauter Stand: 0.6 abgeschlossen

- Version 0.6.0 / Code6, Core v1.8, Content-Schema5, Wire5; Basis a80c882 (geprüfte0.5).
- Neue gezeichnete Aktionskarten: Rahmen, Symbole, Vorher/Nachher, Preisplakette, gemeinsame Ausbau-Diamanten, Grau-/Max-Zustände.
- Gemeinsames2×: nur beide Stimmen; jeder kann zurücknehmen. Offline direkt steuerbar. Spieluhr, Einkommen, Bewegung, Angriffe und Spawn skalieren gemeinsam. Countdown/Netztimeouts bleiben1×, Revanche setzt beide Stimmen zurück.
- Prepare/Porträtprüfung bestanden,96/96 EditMode und46/46 PlayMode.31 aktuelle Porträts, vier final gesichtete HUD-Captures.
- LAN und tatsächliches Relay/DTLS: je zwei vollständige Partien samt Revanche, frischen Stimmen und identischen Enddigests. Zusätzlich zwei gestörte LAN-Zyklen mit Neustart. Sechs Player Exit0; Quellen-/Binaryhashes unverändert.
- APK: Builds/Android/DefenseDuell-0.6.apk,44.314.787Bytes, SHA cea9e9b91bd931a68d0832dde65611259df165696cd2b25a2327004a4906c81b. ARM64/IL2CPP/GLES3, APK-v2 Android-Debug-Testsignatur.
- Frischimport ohne Library plus separate Porträtprüfung bestanden: Builds/FreshImport-20261111-081431.
- Mac: Builds/macOS-SpeedReview/DefenseDuell.app, Bundle de.defenseduell.preview.speed, Fenster Defense Duell0.6.0. Hauptmenü nativ gesichtet. Ein Solo-Klick wurde nicht verifiziert (Nutzeränderung/Tooltimeout). Keine weitere Fernbedienung; Vorschau ist dem Nutzer überlassen.
- Quellarchiv: Artifacts/DefenseDuell-0.6-Source.zip, Hash/Größe im source-artifacts-v06.json.

## Geschützte Vorschauen

SpeedReview0.6, ArtReview0.5, Candidate0.4 und Next0.3 nicht schließen, überschreiben oder fernbedienen. Neue Weiterentwicklung erhält einen neuen abgestimmten Pfad. Aktueller Programmquellstand und tatsächlich vom Nutzer gespielte App sind getrennt zu betrachten.

## Umfang und Grenzen

Drei echte3D-Karten, vier Kampfklassen plus freie Goldminen, vier Truppenklassen, freie Platzierung, zwei gemeinsame Ausbauten, Solo-Bot, WLAN und private Relay-Räume. Keine neuen Preise/Balancerelationen in0.6. Modelle, Terrain und Bot unverändert zur0.5.

Handytests auf Nutzerwunsch später: keine ADB-Installation, Touch-/Thermik-/15-Minuten-/FPS-Abnahme behaupten. Zwei Desktopprozesse sind keine zwei Menschen oder getrennten Internetanschlüsse. Menschliche Balancebewertung bleibt offen. Kein Billing, keine bezahlten Assets, keine Veröffentlichung.

## Koordination / Belege

Alle Runtime-Dateien sind eingefroren. Root hat nach unterbrochenen Agentenläufen Networking/HUD/Tests übernommen und integriert. Keine parallelen Edits oder Unityprozesse im Hauptprojekt. Core/Bot/Maps/Modelle bleiben eingefroren; neue Dateizuständigkeiten vor weiterer Arbeit vereinbaren.

Vollständiger aktueller Bericht TEST_REPORT.md / Docs/Release06TestReport.md. Vorherige Berichte Release04/05TestReport.md bleiben erhalten. Aktuelle Belege: TestResults/network-v5-speed-v06-acceptance.json, network-v5-speed-v06-source-manifest.json, build-artifacts-v06.json, fresh-import-v06.json, editmode-v06.xml und playmode-v06.xml. Read-only-Prüfung: python3 Tools/check_project_state.py --check-frozen.

0.6-Checkpoint: **590a183**. Das verifizierte Quellenarchiv gehört zu diesem abgeschlossenen Stand. Danach eingegangene direkte Nutzerwünsche in anderen Projektchats werden zunächst separat analysiert: Economy durch Truppensenden (Asset-/Audit-Chat) und mehr aktive taktische Entscheidungen (Ansichts-Chat). Kein Runtime-GO für ein Folgepaket; Zuständigkeiten/Entwürfe siehe BACKLOG.md.

Zusätzlicher Entwurfsauftrag: Design-Chat `01a1c6b8-3c29-7cb3-9635-5eb91428c7a1` untersucht eigene Truppenupgrades einschließlich möglicher unbegrenzter Ausbauanzahl. Er ist alleiniger Owner dieses Entwurfs und stimmt Kosten-/Statkurven mit Economy und Interaktivität ab. Noch keine konkrete Regel beschlossen; 0.6 bleibt unverändert.

Wellen-Folgeauftrag: Kartenchat `01a1c6c0-a95b-7c10-b9a7-4514b4316e84` ist Owner des Grundwellenvertrags (nur eigene Entwürfe/isolierte Analyse). Root bündelt Core/Content-/Net-Vertrag; Ansichts-Chat verantwortet HUD-/Interaktivitätsentwurf. Economy- und Upgrade-Owner müssen Grundwellen, Einkommen und geänderte Matchdauer einbeziehen. Bisherige acht Minuten gelten nicht automatisch für den neuen Modus. 100 Wellen, zehn echte Sekunden Pause, beidseitiger Frühstart und Kanonenpflicht sind noch keine festgelegten Regeln. Keine Runtime-Dateiübergabe oder Unityfreigabe vor zusammenhängender Integration.

Entwurfsübergaben Interaktivität und Wellen eingegangen; Root-Zusammenführung in `Docs/ResearchEvidence/followup-integration-review-2026-11-11.json`. Priorität: gemeinsamer Wellen-/Economy-/Upgradevertrag, verständliche aktuelle/nächste FIFO und Phasenanzeige, danach Turmzielpriorität. Aktive Frostfähigkeit bleibt späterer isolierter Prototyp. Keine Umsetzung oder neuen Testergebnisse für diese Entwürfe behaupten.

Economy-Entwurf vollständig übergeben und rechnerisch nachgeprüft: 36 Sensitivitätszeilen, fünf Quellhashes und vier Paket-Kreditsummen stimmen. Kandidat Basis4 + höchstens3 Sendbonus + höchstens4 Mine ist noch keine finale Balance. Keine Kampf-/Bot-/Unityprüfung; endgültige Zahlen hängen von Wellenlaufzeit und Forschungskosten ab. Details in der gemeinsamen Entwurfs-JSON.

Finaler Wellenentwurf `Docs/WaveSystemDesign.md` eingegangen; Root hat15.752 Assertions im isolierten Modell reproduziert und die drei Lieferhashes plus vier aktuelle Corehashes geprüft. Das ist kein Core-/Kampf-/Netztest. Neuer Kandidat:10 Wellen,10 echte Sekunden Vorbereitung,240 bezahlter Kaufwert pro Spieler/Welle; Körper12 ist verworfen. Reservierung ausdrücklich bis zur letzten Welle möglich, konkrete erwartete Zielwelle vor Kauf anzeigen und vom Host bestätigen lassen; Abweichung ohne Zahlung/Reservierung/automatischen Retry. Gemeinsame Synthese aktualisiert; finale Forschungsübergabe und konkreter Integrationsvertrag noch offen.

## Folgepaket0.7 – Vertrag entschieden, Umsetzung ausstehend

Alle vier Entwürfe sind übergeben und geprüft. Verbindlicher nächster Integrationsumfang: `Docs/Iteration07Contract.md`. Erste Testkonfiguration:10 Grundwellen,10 echte Sekunden Vorbereitung,240 bezahlter PvP-Kaufwert je Spieler/Welle, Angriffseinkommen nur nach echtem Spawn, separate Forschung0…8, Vorne/Stärkster und transparentes Wellen-/FIFO-HUD. Kein Endlosmodus, keine absolute Immunität, keine Frostfähigkeit in diesem Paket. Zahlen sind Testkandidat, keine Balancefreigabe.

Wichtige gemeinsame Clock-Entscheidung: in0.7 konstant20 Kontrollschritte/Echtzeitsekunde; Tick ist Kontrollzeit, PlayTicks aktive Gameplayzeit; aktive Phasen führen je nach Zustimmung1/2 Gameplayschritte aus, Vorbereitung0. Alle Gameplayfristen explizit migrieren. Geplant Corev1.9/Schema6/Wire6/App0.7.0; derzeitige Runtime bleibt0.6.

Forschung nachgeprüft:15 Eingabe-/7 Ausgabehashes;408 Checks,1296 Einzelpaket- und108 Korridorproben in separatem Verzeichnis wiederholt; fünf JSON-Ausgaben bytegleich. Keine kombinierte0.7-Simulation, kein Unity-/Netztest für neue Regeln.

Root besitzt zunächst die gesamte Runtime-Integration inklusiveCore/Bot/Content/Net/HUD/Tests/Editor. Frühere Entwurfs-Owner haben keine automatische Runtime-Schreibfreigabe. Konkrete Teilübergaben erst vor paralleler Arbeit; bisherige Freeze-/Kein-GO-Abschnitte gelten für andere Chats weiter, nicht als Nutzerfreigabe-Hürde für Root. Neue separate Vorschau erst unter macOS-WaveReview; bestehende Apps geschützt.

## Karten-Art: ausdrückliche begrenzte Dateiübergabe

Direkter neuer Nutzerwunsch im Ansichts-Chat: Karten wirken langweilig; markante Orte/Geländeformen entwickeln und abstimmen. Owner `01a1c6bc-f70f-75c2-a78e-30f542f60d63` erhält ausschließlich den neuen Helper `Assets/Presentation/MapLandmarkArt.cs` samt eigener .meta sowie eigene Dateien unter `Tools/MapArtDesign/` und `Docs/ResearchEvidence/map-art-v1/`. Schnittstelle: `internal static MapLandmarkArt.Append(DioramaMeshBuilder builder, MapDefinition map)` im vorhandenen Presentation-Namespace. Zusätzliche deterministische, statische Geometrie für den bestehenden zusammengeführten Terrainmesh; kein neuer Renderer erforderlich. Keine Änderungen an ScenicTerrain, DioramaMeshes, BoardView, Shader, GameUI, Core, MapCatalog, Content, Paketen oder Versionen. Diese enge Übergabe ist die einzige Ausnahme zur Root-Runtimezuständigkeit.

Ein eigener temporärer Integrationseinbau/Capture ist ausschließlich in einer isolierten Kopie unter `Builds/MapArtSandbox-*` erlaubt, nie im gemeinsamen Hauptprojekt. Nur batchbasierte Entwurfsbilder/Prüfungen, keine bestehenden Apps bedienen oder überschreiben. Root fügt den Helper später in ScenicTerrain ein und führt die gemeinsame Abnahme aus. Kartenchat bleibt read-only beratend und besitzt keine konkurrierenden Präsentationsdateien.

Ziele: je Biom ein klar erkennbarer Hauptort und wenige dazu passende Nebenformen; Lesbarkeit vor zusätzlicher Dekodichte. Höhen/Objekte nur innerhalb vorhandener Sperrflächen oder äußerem Rand, keine neue Einschränkung legaler Bauflächen, keine Wege-/Projektions-/Gameplayänderung. Zusätzliche Geometrie höchstens8.000 Dreiecke je Karte als erste Obergrenze; Quell-/Bildhashes, Dreiecke/Bounds, Vorher/Nachher aller drei Karten sowie Prüfung von Weg-/Truppen-/Bauflächenverdeckung bei Zoom1/3 liefern. Keine FPS-Zusage ohne Messung. Status zunächst Prototyp, nicht integriert.


## Aktives Integrationsfenster 0.7 (11. November, nach „weiter“)

Die Runtime-Umsetzung ist jetzt gestartet; kein neuer Build/Testsieg behauptet. Das folgende ersetzt ältere pauschale Freeze-/Root-only-Angaben für genau diese Dateien. Root bleibt alleiniger Integrations-, Unity-, Build-, Versions- und Release-Owner.

- Gameplay-Agent `/root/gameplay`: Assets/Core außer BotBrain.cs; Assets/Tests/EditMode/Core; eigene CoreHarness-/WaveCoreReview-Dateien. Implementiert den vollständigen Iteration07Contract und meldet konkrete DTOs früh.
- Networking-Agent `/root/networking`: Assets/Networking, Networking-Tests und eigene Netzwerk-Harnessdateien. Wire6, feste Kontrolluhr und echte Folgebelege; kein Unitylauf ohne Root.
- Bot-/Abnahmechat `01a1c6c8-89f2-7ac3-9a13-fac8362270ca`: BotBrain.cs plus eigene Tools/BotWaveReview zur Umsetzung fairer Wellen-/Forschungslogik und Schwierigkeit; daneben unabhängige Endabnahme. Root prüft dessen Botänderungen separat.
- Ansichts-Chat: bestehende enge MapLandmarkArt-Zuteilung zuerst abschließen. Danach exklusiv GameUI*.cs, BattleInfoPanel.cs, PurchaseAvailability.cs, PresentationCommandTracker.cs und eigene UI-Tests für Wellen/Forschung/Priorität. Root reserviert spätere Hauptmenü-/Unlockintegration erst nach seiner Übergabe.
- Assetchat: isoliert ArtCandidates/TowerUnlocks/v1, Tools/TowerUnlockLab, eigene ResearchEvidence für zwei freischaltbare Turm-Sidegrades samt sechs Geometrievarianten, Siegeswährung und Botprofilvorschlag; zunächst keine bestehenden Runtime-Dateien.
- Root: Content-Asset/Editor/Platform/Versionen, Terrain-Einbau und verbleibende gemeinsame Integration. Kartenchat bleibt beratend ohne konkurrierende Runtimearbeit.

Ziel der vom Nutzer gewünschten finalen Test-APK umfasst neben dem0.7-Wellenvertrag die übernommenen Kartenmotive, zwei freischaltbare Türme, lokale Siegeswährung und wählbare faire Botstufen. Neue Modelle/Profile noch in Entwicklung; keine Funktions-/Balancefreigabe vor Integration. Bestehende Türme bleiben verfügbar, Siegeswährung ist kein Matchgold, keine bezahlten Dienste.

Neuer direkter Nutzerauftrag im Designchat hebt die bisherige Verschiebung für den abschließenden Gerätetest auf: Chat `01a1c6b8-3c29-7cb3-9635-5eb91428c7a1` prüft die gewünschte Mactician-Anbindung und installiert/testet erst die fertig übergebene APK. Root liefert dazu Pfad, Version/Code, Paket-ID, SHA-256 und bekannte Grenzen. Bis dahin keine alte APK installieren; noch kein Geräte-PASS. Alle bestehenden Vorschauen bleiben geschützt.


Zwischenstand aktive Integration: Corev1.9/API und sieben Turmklassen sind geschrieben; Wire6/HUD/Bot in Umsetzung, noch kein gemeinsamer Compiler-/Build-PASS. Root hat Schema6-Serialisierung, lokale Profil-/Speicherlogik, Originalmesh-Import und43-Porträtpipeline eingebunden.752 reine Progressions-/Dateisystemprüfungen bestanden; Unity-Lifecycle noch offen. MapArt-Handoff nach gesichteten Tal-/Wüsten-/Frostbildern mit Ersatzhook integriert, gemeinsame Rendererprüfung folgt. Erste Bot-Partien deckten invertierte Schwierigkeiten und verlorene Freischaltungen bei Revanche auf; Korrekturschleife läuft. Aktuelle Artefakte bleiben0.6, neueVersionswerte bisher nur im Build-Rezept0.7.


Erstes gemeinsames Unityfenster bestanden: Prepare Exit0 (TestResults/prepare-v07-first.log), Standard.asset aufSchema6 und Apprezept0.7/Code7, Contenthash429ab672fc464511fbc3ff3f99d090bea03be6ab0db5e753b98be3cb36c53a6b.24 neue originaleMeshassets importiert,43 tatsächlichePorträts gebacken und geprüft (portraits-v07-first.log). Noch kein0.7APK. Ein unabhängiger Profilreview fand verlorene Prämien bei vorübergehendem Speicherfehler; konsolidierte ausstehende Profile und begrenzteRetries sind implementiert, gezielte Unity-Fehlerinjektion steht an. Bot-Iteration04 bestand36 doppelt simuliertePartien; Root-Quellreview ohneneuenBefund, zwei anschließend verschärfteCorevalidatoren erfordern vor finalemFreeze neueHashbindung. Aktueller Backend-/HUD-/Teststand weiterhin imKorrekturfenster.
