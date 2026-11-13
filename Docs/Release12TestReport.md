# Defense Duell 0.12 – Test- und Auslieferungsbericht

Stand: 13. November 2026. App **0.12.0 / Android-Code 12**, Core **2.1**, Content-Schema **8**, Wire **7**. Native Vollsuite149/149 EditMode und157/157 PlayMode bestanden. Zusätzlich14/14 abschließende Menü-/Render-/Grafikprüfungen bestanden. Mac und APK gebaut; aktuelle LAN-/Relay-Start-/Aktionszyklen und frischer Import bestanden. Auf keinem Android-Gerät installiert oder gestartet.

## Umfang

Alle Funktionen des [0.11-Berichts](Release11TestReport.md) bleiben enthalten: große native Vollbildkarte, gamifiziertes HUD, eigene Klänge/Musik, Goldrate und Ausmarschbonus, unbegrenzte klassenspezifische Ausbauten/Forschung mit exakten Zahlen, Minen-/Sendewirtschaft, Drag-/Abwahlfix und gepufferte Gastdarstellung.

Neu sind das bereitgestellte Soballa-Studio-Intro und ein spielerisches Startmenü mit echtem Diorama, drei Karten, Solo/Schwierigkeit, WLAN/Online und Arsenal. Dekorative Animationen verändern keine Touchflächen und pausieren bei reduzierten Effekten, Energiesparen, Modal, Fokusverlust oder App-Pause. Das Video wird aus der Originalanimation mit 720×406 Pixeln/25fps/4,2s und expliziter BT.709-Matrix bereitgestellt; keine vorgetäuschte höhere Quellauflösung und kein hinzugefügter Ton. Skip ist nach1,5 sichtbaren aktiven Sekunden möglich. Fehler, Stillstand und Deaktivierung dürfen Boot nicht blockieren.

## Prüfung und Korrekturen

Die erste gezielte Prüfung bestand neun Menü-/Bestandsfälle sowie elf Intro-Fälle. Die anschließende Bildsichtung fand trotzdem zwei Menüprobleme: abgeschnittener Solo-Text bei900 logischen Pixeln und fehlende CanvasRenderer an Kartenpiktogrammen. Beide wurden behoben und die Regressionen um tatsächliche Text-/Meshprüfung ergänzt. Außerdem zeigte der native Videoframe eine Farbverschiebung durch fehlende Matrixangaben. Die Konvertierung wurde direkt aus dem WebP mit passender Matrix wiederholt; keine bloße Umetikettierung der alten Videodatei. Originale Nachweise bleiben erhalten.

Nach der Vollsuite wurde das Home-Diorama gezielt von640 auf bis1280 native Pixel erhöht; Standardvorschau/Minimap und Energiesparen bleiben640, die große Spielkarte unverändert bis2560. Diese letzte Änderung bestand14/14 gezielte Prüfungen. Gemessen1174×381 statt640×208 im1920×1080Capture und768×381 bei1350×1080. Alle278 neu quellengebundenen Porträts sind vollständig pixelidentisch zur finalen0.11. Drei finale Menübilder visuell geprüft.

Der aktuelle Quellenumfang vor der Vollsuite ist in `TestResults/studio-v12-source-manifest.json` gebunden. Die frühere umfassende Regel-/Netzwerkabnahme wird nur mit ausdrücklichem Unverändertheitsnachweis weitergeführt; neue Startup-Prüfungen müssen das neue Playerpaket tatsächlich starten.

## Grenzen

Keine neue Android-Installation oder Hardwaredekodierung behauptet. Handygesten, Lautsprecher, Launcher,15-Minuten-Frametimes/Speicher/Thermik, zwei menschliche Spieler und getrennte Internetzugänge bleiben offen. Der Kasernenmodus und Vier-Spieler-Modus sind separate Konzepte, keine Funktionen dieser APK. Kein Billing, keine bezahlten Assets und keine Store-Veröffentlichung.

## Tatsächlich ausgeführte Befehle

Unity6000.3.28f1 wurde seriell mit `-batchmode -projectPath … -runTests -testPlatform EditMode` und danach grafischem `PlayMode` gestartet. Ergebnisse: `TestResults/editmode-v12.xml`, `playmode-v12.xml`. Der gezielteNachlauf filtert `MenuPresentationTests;RenderQuality09Tests;GraphicsIntegration10Tests;Fullscreen10Tests`; Ergebnis `playmode-v12-sharpness.xml`. Porträts: `DefenseDuell.Editor.ModelPortraitBaker.Bake`; vollständiger RGBA-Vergleich in `portrait-v12-pixel-comparison.json`.

Buildbefehle: `bash Tools/unity.sh studio-review` und `bash Tools/unity.sh android` (entsprechende BuildEntry-Methoden direkt mit versionsgebundenem Log aufgerufen). Frischimport: `python3 Tools/verify_fresh_import.py`. APK-Prüfung: `python3 Tools/verify_android.py Builds/Android/DefenseDuell-0.12.apk`. Diese Befehlsliste ersetzt keinen Ergebnisnachweis.

## Finale Artefakte und Transportnachweise

- APK `Builds/Android/DefenseDuell-0.12.apk`, **71.097.720 Bytes /67,80MiB**, SHA256 `c416db08a002fdfd0c14a70c0ad09c840bb8bc9d32c04b441a7c5d4abb925c22`.
- Paket `de.defenseduell.game`, Code12, ARM64/IL2CPP/GLES3, minAPI25/targetAPI35. Developmentbuild mit Android-Debug-Testsignatur, APK-v2 verifiziert. Keine großen inkrementellen ZIP-Lücken.26 Iconressourcen bytegleich zur geprüften0.11; finale296.149Byte-MP4 exakt in `sharedassets0.resource` nachgewiesen.
- Mac `Builds/macOS-StudioReview/DefenseDuell.app`, Bundle `de.defenseduell.preview.studio`,631.763.195Bytes;310Dateihashes in `mac-studio-v12.json`. Geschützte frühere Vorschauen bleiben erhalten.
- Ein neuer LAN-Verbindungs-/Aktions-/Shutdownzyklus:465 gemeinsame Snapshots,62Hostbefehle, beide Prozesse Exit0. Neuer echter Unity-Relay/DTLS-Zyklus:442 gemeinsame Snapshots,60Hostbefehle, beide Exit0. Beide enthalten schnelle Mehrfachkäufe und gemeinsame Tempowahl. Keine vollständigen Partien in diesen0.12-Startupzyklen.
- Die zehn LAN-Zyklen sowie vollständigen gestörten LAN-/Relay-Partien aus0.11 bleiben historische, separat gebundene Nachweise. Core2.1 ist auch im neuen Macbuild bytegleich; produktive Netzwerkquellen unverändert. Networking/Content/Platform/Progression unterscheiden sich als Datei; der separate Cecil-Vergleich bestätigt identische normalisierte IL-/Metadatensätze und Methoden sowie gleiche Assemblyreferenzen, bei unterschiedlicher MVID und Build-/Debugidentität. Insgesamt614Methoden/35.744IL-Instruktionen geprüft; Details in `v12-binary-semantic-review.json`. Kein identischer kompletter Player wird behauptet. Neuer Intro-Startpfad wurde durch tatsächlichen0.12Player nachgewiesen.
- Frischimport ohne Library: `Builds/FreshImport-20261113-134342`, Prepare und278Porträts bestanden. `TestResults/fresh-import-v12.json`.
- Finale Bindung `TestResults/network-v7-studio-v12-source-manifest.json`; Prüfung `python3 Tools/check_project_state.py --check-frozen --manifest TestResults/network-v7-studio-v12-source-manifest.json`.
- Quellenpaket `Artifacts/DefenseDuell-0.12-Source.zip`; Größe/Hash/CRC und Quellabgleich nach Erstellung separat in `TestResults/source-artifacts-v12.json`, um zirkuläre Archivhashes zu vermeiden.

## Definition of Done

| Kriterium | Status | Nachweis / Grenze |
|---|---|---|
|1. Frischer Import und Build|Bestanden|Unity6000.3.28f1, neuer Mac/APK, Import ohne Library und278Porträts|
|2. Vollständige Partie mit zwei Personen|Nicht geprüft|Automatisierte0.11Partien und neue0.12Aktionszyklen ersetzen keine zwei Menschen|
|3. WLAN und echter Relay|Bestanden|Tatsächliche0.12LAN-/Relay-DTLS-Playerprozesse, frühere vollständige Transportpartien separat|
|4. APK mit Größe/Hash/Signatur|Bestanden|Obige APK, ARM64/IL2CPP, APK-v2|
|5. Android-Gerät und Performance|Nicht geprüft|ADB listet kein Gerät; keine Installation, Handygesten, Videohardwaredecode oder Frametimes|
|6. Kernprüfungen ohne bekannte partiekritische Fehler|Bestanden|149/157Vollsuiten plus14finaleRegressionen, unveränderterCore und echte aktuelle Netzwerkstarts|
|7. Herkunft/Lizenzen|Bestanden|Assetverzeichnis, eigene Assets und dokumentierte vom Nutzer bereitgestellte Animation|
