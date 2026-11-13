# Defense Duell0.15 – Integrations- und Prüfbericht

App **0.15.0 / Android-Code16**, Core **2.4**, Content-Schema **9**, Wire **9**. Sämtliche beauftragten Androidfunktionen sind in der gemeinsamen Runtime integriert. **232/232EditMode und209/209PlayMode bestanden.** Android wurde nach dieser Abnahme mit ausdrücklicher Nutzerpriorität gebaut und die APK tatsächlich geprüft. Kein neuer Android-/Nutzerstart behauptet.

## Implementierte Funktionen

Solo/Duell und echte Freundesräume2v2,3v3,2v2v2 besitzen2/4/6reale Slots und2/3Teams, eigene Felder/Wallets/Türme/Forschung sowie gemeinsame Teamleben. Kein Bot-Fill. Bezahlte Angriffe behalten ihr ausgewähltes gegnerisches Ziel. Eigene Lobbyausrüstung umfasst alle acht Klassen; fremde Gebäude erhalten keine zusätzlichen Besitzrechte.

Wellenmenü mit Slider/exakter Zahl1–200 und echtem Endlos, Einkommensfaktoren1/2/3/5/10 sowie hostautoritativ synchronisierte Karten-/Regelwahl binden Ready an den bestätigten Stand. Endlos verwendet BigInteger-Wellen und ein begrenztes Ledgerfenster. Der Faktor wirkt auf laufendes Basis-/Minen-/gedeckeltes Angriffseinkommen, unabhängig von Startgold/Preisen/Refund/Kopfgeld und2×-Tempo.

Sechs Karten: drei bestehende mit mehr tatsächlich legalem Bauplatz und drei neue2400×1440-Felder mit eigenständigen Wegen, größeren erhöhten Fels-/Baum-/Wasserlandschaften. Neue Wege behalten exakt4532/4122/4038logische Längeneinheiten;60erRaster liefert509/505/518legale Kandidaten, davon201/196/192innerhalb150zurRoute. Dies sind leere Feldkandidaten, keine gleichzeitige Gebäudekapazität. Karten werden als echte3D-Meshes in physischer Renderauflösung gezeichnet, kein Hintergrundbild als Ersatz.

Größere Sendekarten, Links-Swipe zur Forschung und separate Tapalternative; kein Kauf durch Swipe. Überarbeitete responsive Zwischenbilder, Ergebnisbuttons innerhalb des Panels. Laser als achte Klasse, gezackte Tesla-Blitze, begrenzte Effektbatches und reduzierte Effekte. Turmformen zeigen jeden bezahlten Gesamtlevel bis30 und spätere40/50/75/100-Meilensteine mit echten Pfadmodulen.2074Porträts aus denselben Spielmodellen,512px-Quellen, AndroidETC2RGBA8. Alle bisherigen unbegrenzten Upgrades, Goldfeedback, Vollbild-HUD, Sounds, Gastglättung, Dragkorrektur, Intro und Kanonenradius bleiben integriert.

LAN-Raumerkennung zeigt explizit Beitreten/Später und behandelt Protokoll/Katalog, Absender, TTL/Dedupe, Pause und Netzwechsel. Kein Autojoin/OS-Push. Browser hören aufUDP19175; Hosts senden über eigenen Port. Androidrechte werden idempotent ins tatsächliche Gradlemanifest eingefügt.

## Aktuelle native und reine Beweise

Finale Vollsuiten: TestResults/editmode-v15.xml232/232 und playmode-v15.xml209/209. Sourcefreeze TestResults/integration-v15-source-manifest.json bindet4790Dateien nach tatsächlichem Standard.asset-Abgleich,202AOgruppen und neuem2074-Porträtbake. Der unabhängige Audit prüfte alle2074Porträtdateien und50Quellbindungen. Macbuildbedingte ProjectSettings-Deltas sind gesondert dokumentiert; Runtimequellen bleiben identisch.

139reine aktuelle Produktions-Corefälle bestanden. Vier aktuelle natürliche Szenarien: alle drei Teamformate auf den neuen Karten mit je drei vollständigen Wellen und573legalen Befehlen pro Slot; Endlos erreicht206 und endet nach regulärem Verteidigungsverkauf, Ledger maximal2, archivierte14760Quarter-Credits jeSpieler. Frischer Build und direkter Aufruf der exakten DLLs samt Hosttrace/Hashes: TestResults/core-v15-final. Der erste Root-Harnessaufruf hatte den erforderlichen Berichtspfad weggelassen; sein fehlgeschlagener Hilfsaufruf bleibt erhalten, die korrekte Wiederholung besteht139/139. Kein Runtimefehler daraus abgeleitet.

151bestehende Pythonfälle und8gezielte neue Comparatorfälle bestanden. Weitere Core-/Wiremodelle sind getrennte, teilweise überlappende Beweise und werden nicht zu einer künstlichen Gesamttestzahl addiert. Ein reines Modell wird vom echten Transportvergleich ohne ausdrückliche Modellfreigabe abgelehnt.

Neue Kartenübersichten, Zoom-/Kamerafälle, rechtmäßige Bauprojektionen und Terrainbudgets bestanden. Root sah die drei neuen Übersichten, Amberzoom und aktuellen Research-HUD direkt; der unabhängige Audit sah sechs aktuelle Übersichten und dreiZooms sowie den Hinweistext. Seine anfänglichen Qualitätsbefunde wurden durch neue Wege/Relief und breiteren Hinweistext korrigiert und geschlossen. Quelle, Bildhashes und ehrliche Sichtgrenzen: Tools/FeatureIntegrationAudit/Iteration15Final-PostPolishVisualEvidence.json. Telefonfälle nutzen synthetische Renderziele/Insets; keine tatsächliche Handy-Qualitäts-/Leistungsmessung.

## Tatsächliche finale Netzwerkprozesse

Alle drei Teamformate jeweils über LAN und echtes UnityRelay/DTLS mit4/6getrennten finalen Playerprozessen bestanden, jedes mit zwei vollständigen1-Wellen-Partien, gemeinsamer Bereitschaft, unterschiedlichen eigenen Loadouts, Hostkarten-/Faktorwechsel, Teamreparatur, abgewiesenen fremden Upgrades/Verbündetensends, festem Angriffsziel, Tempowahl, Ergebnis, Revanche und geordnetem Shutdown. TestResults/team15-final-*/comparison.json. Scope: tatsächliche automatische Desktopprozesse, keine menschlichen Teilnehmer oder getrennten Internetzugänge.

Finales gestörtes Duell-LAN mit75msVerzögerung,15msJitter und2%Verlust jeRichtung besteht mit408gemeinsamenSnapshots,60Hostbefehlen undExit0/0: TestResults/duel15-final-impaired. Dieser Aktionszyklus ist keine vollständige Partie. Sechs-Peer-Endlosstart und200-Wellen-Konfigurationsstart über echtesRelay bestehen ausdrücklich als Startupsmokes, nicht als vollständig gespielte200Netzwellen. Frühere vollständige3-Wellen-Duelle des0.15-Vorpolishstands bleiben separat erhalten.

Echter finaler LAN-Apphost wurde von exakt aktuellem Produktions-C#-Discoverycode erkannt: TestResults/discovery-v15-final/binding.json, korrekter Katalog/Port/1von4/Map und Aufräumen. Kein Fakebeacon oder Autojoin. Zusammenfassung mit Quell-, Macartefakt- und Berichtshashes: TestResults/network-v9-allfeatures-v15-acceptance.json.

## Gefundene Fehler und erhaltene Erstbelege

Frühe native Suite191/202 deckte echte atomare Lobbycache-/Info-Pointerfehler und veraltete Fixtureannahmen auf. Weitere202/203 hatte den Home-Raycast unter gleichzeitig eingehenden echten LAN-Bannern. Base-Menüfixture isoliert jetzt nur externes Polling; neue separate native Bannerfälle prüfen Beitreten/Später, keinAutojoin, Modal-/Matchunterdrückung und abgelaufene Räume. Alle ursprünglichen Trefferassertions bleiben erhalten. Ein echter standalone Bootstrap-NRE wurde durch Zugriff auf Session.Content/State statt noch leerem UIcache korrigiert.

Frühe4/6-Peerläufe spielten zwei vollständige Runden, aber ihr Testdriver erkannte den absichtlich erhaltenen Finished-Zustand nach Hostende nicht. Der Driver wird nur nach beiden belegten Runden und nullPeers beendet; Sessionergebnissemantik bleibt erhalten. Die alten vom Root beendeten Prüfprozesse sind als fehlgeschlagene Erstläufe bewahrt, nicht rückwirkend bestanden.

Leere Discoveryobserverlisten waren durch fehlendes SO_REUSEPORT im Pythonobserver verursacht. Acht echte unveränderte Hostbeacons bewiesen dies. Observer und Runtime-Rollensockets korrigiert;12gezielte echte UDPfälle bestanden. Der erste Störnetzvergleich verlangte einen Zustand in einem200msFenster, das ein5Hz-Gast unter Verlust überspringen kann. Der Testdriver hält jetzt genau den letzten tatsächlich empfangenen Preparing-Zustand fest; der Comparator prüft Phase, positiveRestzeit, strikte Zeitordnung und identische Deadline. Kein Sendraten-/Simulationsfix und keine pauschal erhöhte Toleranz.

Weiterhin erhalten: veralteter Cachefixturefehler, ungültige Test-Metaguid, ursprüngliche XMLs/Quellstände und Vorpolishbilder. Buildasset-Parität wird für jeden Slot unabhängig gegenMapCatalog.Create(i) geprüft; eine veraltete map0-Geometrie wird ausdrücklich vor der idempotenten Reparatur nachgewiesen.

## Artefakte und Definition of Done

Mac: Builds/macOS-AllFeaturesReview/DefenseDuell.app, Bundlede.defenseduell.preview.allfeatures,2.520.627.122Bytes. Datei-/Assemblyhashes in TestResults/mac-allfeatures-v15.json. Nur eigene Testprozesse gestartet, keine Nutzervorschau übernommen.

APK: **Builds/Android/DefenseDuell-0.15.apk**,131.647.834Bytes/125,55MiB,SHA256 `a7e724287eea52fe9a9ab9e254ab9db1f0ba125679b1f03cc84dca411088bb2f`. Paketde.defenseduell.game, Version0.15.0/Code16, ARM64/IL2CPP/GLES3, minAPI25/target35. Developmentbuild mit verifizierterAndroid-Debug-Testsignatur undAPK-v2. Tatsächliche LAN-/Multicastrechte vorhanden, ZIP-CRC bestanden, keine großen ungenutzten ZIP-Lücken, finaleIntro-MP4 bytegleich imAPK nachgewiesen. Versionierte und generischeAPK sind bytegleich. Auf keinemAndroidgerät durch diese Aufgabe installiert oder gestartet.

Alle4790getesteten Dateien einschließlichProjectSettings stimmen nach beidenBuilds exakt mit dem getestetenFreeze überein. Bindung: TestResults/release-v15-source-artifact-binding.json. Das zwischenzeitlicheMac-Vorschau-Metadatendelta wurde durchAndroidPrepare zurückgesetzt. KeineRuntimeänderung nach den finalenVollsuiten.

Frischimport ohneLibrary in Builds/FreshImport-20261113-180208: Prepare und2074-Porträtprüfung bestanden. Alle kopiertenAssets/Packages/ProjectSettings stimmen nachImport exakt mit dem getestetenFreeze überein: TestResults/fresh-import-v15-source-binding.json. Quellenarchiv: Artifacts/DefenseDuell-0.15-Source.zip; Größe/SHA256/CRC und vollständiger Vergleich sind im nach Erstellung erzeugten TestResults/source-artifacts-v15.json gebunden. ZIP-CRC und vollständiger Vergleich aller archivierten Dateien bestanden. Der integrierte0.15-Quellstand erhält einen eigenen lokalenCheckpoint.

| Kriterium | Status | Grenze |
|---|---|---|
|1.Frischer Import und Build|Bestanden|FinaleMac/APK und neuerImport ohneLibrary samt2074-Porträtprüfung; exakteQuellbindung|
|2.Vollständige Partie mit zwei Menschen|Nicht geprüft|Automatisierte4/6Prozesse ersetzen keine Menschen|
|3.LAN und echterRelay|Bestanden|Finale Prozesse; keine zwei getrennten Internetzugänge|
|4.APK mit Größe/Hash/Signatur|Bestanden|125,55MiB, SHA256, ARM64/IL2CPP/GLES3, Version16, APK-v2 und Testzertifikat geprüft|
|5.Androidgerät/Performance|Nicht geprüft|Kein freigegebenesADBgerät; keine Hardwaregesten/Audio/Video/Frametimes/Thermik|
|6.Kernprüfungen ohne bekannten partiekritischen Fehler|Bestanden im beschriebenen Scope|232/209Vollsuiten, aktuelle Core- und echte Netztests|
|7.Herkunft/Lizenzhinweise|Bestanden|Eigene neue Geometrie/UI/VFX; vorhandene dokumentierte Assets; keinBilling/Storeupload|

iOS-Exporthelper bleibt integriert. Unity-iOS-Modul, vollständigesXcode/SDK, Signierung undiPhone fehlen; kein installierbaresIPA oder echtes Crossplay behauptet. Kasernen sind ausdrücklich ein Plan; Mortar/Reconnect/Hostmigration waren Vorschläge. Die externe Geräte-/Menschenabnahme bleibt trotz auslieferbarer privater Test-APK offen.
