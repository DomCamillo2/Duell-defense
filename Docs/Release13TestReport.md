# Defense Duell 0.13 – Test- und Auslieferungsbericht

Stand: 13. November 2026. App **0.13.0 / Android-Code 13**, Core **2.1**, Content-Schema **8**, Wire **7**. Integriert, nativ getestet und als Mac-/Android-Build erstellt. Eigene Desktopprozesse erfolgreich gestartet und beendet; auf keinem Android-Gerät installiert oder gestartet.

## Änderungen

Der Hintergrund füllt im Home und Verbindungsmenü das gesamte Canvas. Buttons bleiben innerhalb der SafeArea. Der vom Nutzer gemeldete helle linke Streifen ist im nativen Bildvergleich behoben; auch der spiegelbildliche rechte Rand wurde geprüft.

WLAN und Online erhalten eine eigene Kopfzeile, zwei gerahmte Karten für Erstellen/Beitreten, klare Feldlabels und kontrastreiche Eingaben. Port und technische Hinweise liegen in einem Hilfedialog. Formularwerte bleiben bei Dialogen und Neuaufbau erhalten. Die Beispiel-IP erscheint nur als Platzhalter. Host/Join und Validierung verwenden die vorhandenen Sessionwege; ein neuer Versuch entfernt eine veraltete lokale Fehlermeldung.

Die Lautstärkegriffe bleiben innerhalb ihrer Zeilen. Unity überschreibt die Slider-Anker selbst; deshalb begrenzt nun ein 58 Pixel hoher Container mit vertikalem sizeDelta 0 die tatsächliche Griffhöhe. Bewegung und Geometrie wurden bei Werten 0, 0,5 und 1 geprüft, ohne gespeicherte Lautstärken zu verändern.

Alle bisherigen Funktionen bleiben enthalten: unbegrenzte klassenspezifische Upgrades/Forschung, große scharfe Vollbildkarte, HUD, Sounds/Musik, Goldfeedback, Drag-/Abwahlfix, geglättete Gastbewegung und Studiointro. Kampfregeln, Wirtschaft und Transportprotokoll wurden nicht verändert.

## Native Prüfungen und Bildsichtung

**149/149 EditMode und 161/161 PlayMode auf dem finalen Quellstand bestanden.** Die EditMode-Suite wurde nach der letzten UI-Korrektur nochmals ausgeführt. Quellbasis: `TestResults/connect-v13-source-manifest.json`; finale Artefaktbindung: `network-v7-connect-v13-source-manifest.json`.

Der erste gezielte Lauf bestand 8/9 Fälle. Die neue Textprüfung fand einen zu niedrigen Titelbereich; seine Geometrie wurde korrigiert, ohne die Assertion abzuschwächen. Danach bestanden 3/3 Connect-Fälle. Die erste Vollsuite bestand 160/161: Unity überschrieb die erste Korrektur der Slider-Anker. Nach dem Containerfix bestand der komplette gezielte Insettest; anschließend bestanden die oben genannten vollständigen finalen Suiten. Ursprüngliche XML-, Quellen- und Bildbelege bleiben erhalten.

Geprüft wurden echte Raycasts, Modalabschirmung, Fokus-Rückgabe, persistente Formularwerte, ungültiger Port/IP/Code ohne gestartete Verbindung und Text-/Eingabekontrast. Die SafeArea-Prüfung verwendet ein natives 2048×955-Renderziel mit synthetischen 83-Pixel-Insets links bzw. rechts. Alle 79.265 Pixel des ausgeschlossenen Streifens wurden je Ansicht geprüft. Das Testfenster deaktiviert GameUI.Update, damit die SafeArea des Mac-Fensters den synthetischen Telefonfall nicht überschreibt; es ist ausdrücklich kein Android-Gerätetest. Obere/untere Geräte-Insetvarianten werden nicht als geprüft behauptet.

Sieben Connectbilder, acht Insetphasen sowie Home900/1280 und Arsenal wurden einzeln gesichtet. Beide Settingsbilder wurden nach dem finalen Sliderfix erneut geprüft. Belege: `Tools/ConnectUiLab/visual-review.json`, `Docs/FinalUXEvidence/menu-v13/`.

## Tatsächliche Netzwerkprozesse

Der neue 0.13-Mac-Player bestand einen LAN-Verbindungs-/Aktions-/Shutdownzyklus mit **453 gemeinsamen Snapshots und 62 Hostbefehlen**, beide Prozesse Exit 0. Ein neuer echter Unity-Relay/DTLS-Zyklus bestand mit **440 gemeinsamen Snapshots und 60 Hostbefehlen**, ebenfalls Exit 0/0. Beide prüfen Mehrfachkäufe und gemeinsame Tempowahl. Es sind keine vollständigen 0.13-Partien oder zwei menschlichen Spieler.

31 relevante Core-/Content-/Netzwerk-/Plattform-/Progressions-/Boardquellen sind gegenüber Checkpoint `12d535b` unverändert (`v13-reuse-audit.json`). Die Core-DLL ist bytegleich zur 0.12; die übrigen DLLs werden nicht als bytegleich behauptet. Historische vollständige Partien, zehn LAN-Zyklen und gestörtes LAN aus 0.11 bleiben im damaligen Bericht getrennt erhalten; sie werden nicht in neue 0.13-Tests umbenannt.

## Artefakte und reproduzierbare Befehle

- APK: `Builds/Android/DefenseDuell-0.13.apk`, **71.107.452 Bytes / 67,81 MiB**.
- SHA-256: `10175935f6b7d04def58c5cbcc1e39b369b0b87d62dd9411e0fee3a309f745e3`.
- Paket `de.defenseduell.game`, ARM64/IL2CPP/GLES3, minAPI25/targetAPI35. Developmentbuild mit Android-Debug-Testsignatur; APK-v2 verifiziert. Keine großen ungenutzten ZIP-Lücken. Alle 26 Icons bytegleich zur geprüften 0.12; die finale Intro-MP4 ist exakt im APK nachgewiesen.
- Mac: `Builds/macOS-ConnectionReview/DefenseDuell.app`, Bundle `de.defenseduell.preview.connection`, 631.761.216 Bytes und 310 gebundene Dateihashes.
- Frischimport ohne Library: `Builds/FreshImport-20261113-141706`; Prepare und 278 Porträts bestanden. Die Porträts und ihre Quellen sind unverändert, daher kein unnötiges Neubacken.
- Quellenpaket: `Artifacts/DefenseDuell-0.13-Source.zip`. Größe, SHA-256, CRC und Quellvergleich werden nach Erstellung separat in `TestResults/source-artifacts-v13.json` festgehalten.

Unity 6000.3.28f1 wurde seriell mit `-runTests -testPlatform EditMode` bzw. grafischem `PlayMode` gestartet. Ergebnisse liegen in `TestResults/editmode-v13.xml` und `playmode-v13.xml`. Builds: `bash Tools/unity.sh connection-review` und `bash Tools/unity.sh android`; jeweils dieselben BuildEntry-Methoden direkt mit versionsgebundenem Log aufgerufen. APK-Prüfung: `python3 Tools/verify_android.py Builds/Android/DefenseDuell-0.13.apk`. Frischimport: `python3 Tools/verify_fresh_import.py`.

Die Netzwerkwerkzeuge `Tools/NetworkTests/run_lan.py` und `run_relay.py` wurden mit dem neuen Player, `--cycles 1 --rapid-buy --speed-vote` und eigenen Ausgabeordnern `lan-connect-v13`/`relay-connect-v13` ausgeführt. Für LAN wurde Port7857 verwendet. Vorhandene Berichte werden nicht überschrieben.

## Definition of Done

| Kriterium | Status | Nachweis / Grenze |
|---|---|---|
|1. Frischer Import und Build|Bestanden|Gepinnte Unity-Version, neue Mac/APK, Import ohne Library, 278 Porträts|
|2. Vollständige Partie mit zwei Personen|Nicht geprüft|Automatisierte Prozesse ersetzen keine zwei Menschen|
|3. WLAN und echter Relay|Bestanden|Tatsächliche aktuelle LAN-/Relay-DTLS-Zyklen|
|4. APK mit Größe, Hash und Signatur|Bestanden|Obige verifizierte ARM64-APK|
|5. Android-Gerät und Performance|Nicht geprüft|ADB listet kein Gerät; keine Installation, Gesten, Hardwarevideo, Audio oder Frametimes|
|6. Kernprüfungen ohne bekannte partiekritische Fehler|Bestanden|149/161 finale Vollsuiten, aktuelle echte Netzwerkstarts|
|7. Herkunft und Lizenzhinweise|Bestanden|Unveränderte dokumentierte Assets; neue UI verwendet vorhandene eigene Komponenten|

Neue Host-Einstellungen und der separat beauftragte 4-/6-Spieler-Freundesmodus ohne Bots befinden sich in isolierten Folgeentwürfen. Sie sind keine Funktionen dieser APK und aus dem 0.13-Quellenpaket ausgeschlossen. Physische Handygesten, Displayaussparungen, Lautsprecher, Hardwarevideo und 15-Minuten-Frametimes/Speicher/Thermik bleiben offen. Keine bezahlten Assets, kein Billing und keine Store-Veröffentlichung.
