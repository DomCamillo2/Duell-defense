# Testbericht – Defense Duell 0.5

Stand: 10. November 2026. Unity 6000.3.28f1, Core v1.7, Content-Schema 5, Wire 4. Diese Version vereinheitlicht Modelle, Porträts, Kartenfarben und HUD. Die Spielregeln und alle zwölf gebundenen Core-/Content-/Netzquellen sind bytegleich zur geprüften 0.4 (`62df0bc`). Der frühere Bericht bleibt in `Docs/Release04TestReport.md` erhalten.

## Artefakte und tatsächlicher Status

- **APK gebaut und verifiziert:** `Builds/Android/DefenseDuell-0.5.apk`; identischer aktueller Pfad `Builds/Android/DefenseDuell.apk`.
- **44.297.631 Bytes / 42,25 MiB**, SHA-256 `d8cb68acb775ab374a2ce5e2134e5727a601ee4991d4640701c066077d5aee5a`.
- Version **0.5.0 / Code 5**, Paket `de.defenseduell.game`, ARM64, IL2CPP, OpenGL ES 3, min API 25, target API 35.
- Development Build; **Android-Debug-Testsignatur**, APK-v2 mit `apksigner` verifiziert. Zertifikat `C=US, O=Android, CN=Android Debug`. Keine Store-/Release-Signierung. Start-Activity `com.unity3d.player.UnityPlayerGameActivity`.
- **macOS gebaut und geöffnet:** `Builds/macOS-ArtReview/DefenseDuell.app`, Bundle `de.defenseduell.preview.art`, Fenster `Defense Duell 0.5.0`. Native Sichtprüfung siehe unten. Die laufende Partie bleibt für den Nutzer unangetastet; auch ältere Candidate-/Next-Vorschauen sind geschützt.
- Quellarchiv: `Artifacts/DefenseDuell-0.5-Source.zip`, zusätzlicher aktueller Pfad `Artifacts/DefenseDuell-Source.zip`. Größe und Hash werden in der benachbarten SHA-Datei und `TestResults/source-artifacts-v05.json` festgehalten. Cache, APKs und private Signierschlüssel gehören nicht ins Quellarchiv.
- **Kein Android-Gerätetest ausgeführt.** Der Nutzer möchte zuerst das Spiel fertig bauen. Ein vorhandenes Samsung und ein USB-Kabel gelten nicht als Start-, Touch- oder Leistungsnachweis.

APK-Beleg: `TestResults/build-artifacts-v05.json`. Unity meldete Android BuildSucceeded in 57,87 s; seine Summe sämtlicher Buildprodukte ist nicht die oben angegebene tatsächliche APK-Dateigröße.

## Import, Compile und automatisierte Prüfungen

Die dokumentierten Einstiegspunkte wurden tatsächlich verwendet:

```sh
bash Tools/unity.sh prepare
bash Tools/unity.sh portraits
bash Tools/unity.sh edit-tests
bash Tools/unity.sh play-tests
bash Tools/unity.sh art-review
bash Tools/unity.sh android
python3 Tools/verify_android.py
python3 Tools/verify_fresh_import.py
```

Prepare und Shader-/C#-Compile bestanden. **89/89 EditMode** bestanden, Ende 21:08:26 UTC. Diese unveränderten Regel-/Datenprüfungen wurden nach reinen Modell-/HUD-Korrekturen nicht unnötig wiederholt. Der finale Compile und **43/43 PlayMode** bestanden, Ende **21:23:37 UTC**. Archive: `TestResults/editmode-v05.xml` und `playmode-v05.xml`.

Der erste PlayMode-Lauf hatte 36/42 bestandene Fälle. Die gefundenen Fehler wurden behoben und im finalen Lauf weiter geprüft:

- Bei schmalem Canvas überlappten Info-i und Kontextleiste. Die Kontextbreite reserviert jetzt den Platz einschließlich 12 Einheiten Abstand; die Rechtecke werden ausdrücklich auf Nichtüberlappung geprüft.
- Eine neu aktivierte Infofläche besaß im Öffnungsframe noch keine native Raycast-Depth. Shield und Panel bleiben jetzt im Canvas, schalten Alpha/Raycast/CanvasGroup und erhalten ihre Position unmittelbar beim Öffnen. Der Schild bleibt bis zum Ende des Schließens aktiv.
- Bereits vor Info-Öffnung gehaltene Kaufkontakte bleiben bis zum Release/Klick unterdrückt, auch nach vollständigem Schließen, reduzierten Effekten und Fokusverlust. Ein frischer Kontakt derselben ID kauft wieder genau einmal.
- Seitengeröll der Mine lag 0,02 Welteinheiten unter der Bodenebene. Die Geometrie wurde angehoben; die strenge Bodenprüfung bleibt erhalten.
- Rohr und Mündungsring hatten koplanare Innenflächen. Drei angrenzende Profile mit dunkler vertiefter Kavität beseitigen die sichtbaren Zacken; die Rapid-D2-Strebe liegt unter den drei Öffnungen. Eine neue Sichtstrahlprüfung umfasst alle 24 Rapid-/Kanonenmündungen.

Die neuen Prüfungen umfassen zusätzlich alle Preisgrenzen (Preis−1/exakt/+1), unabhängige Dragguards, Klassenlimit, echte späte Host-/Queue-Ablehnungen ohne Goldverlust, Info-/Mehrfinger-/Focus-/Rebuild-Lifecycle, Variantenporträtbindung, Poolreset und Modellbudgets. Geprüftes Maximum: **2.112 Dreiecke je Turm, 992 je Truppe**; zwei Renderer je Turm und ein Körperrenderer je Truppe. Das ist ein Geometriebudget, kein Android-FPS-Nachweis.

Frischer Import: `Builds/FreshImport-20261110-213005`, ausdrücklich ohne vorhandene Library gestartet; Prepare Exit 0 und anschließende separate Porträtprüfung Exit 0. `TestResults/fresh-import-v05.json`. Der Content-Hash stimmt. Die APK wurde im Hauptprojekt gebaut; ein zweiter APK-Build in der Importkopie wird nicht behauptet.

## Sicht- und Terrainprüfung

**31 Porträts** wurden aus exakt den produktiven Meshes, Kopfpositionen und Materialien gerendert: 27 Gebäudezustände und vier Truppen, beim Schwarm vier Instanzen. Pro Klasse bleibt der Kameraausschnitt über alle Ausbaustufen gleich. Source-/PNG-Hashes, transparente Ränder und Sprite-Importwerte einschließlich Plattform-Overrides werden vor dem Build geprüft. Ein unabhängiger PNG-Decode bestätigte RGBA 512×512, Hashes und freie Ränder. Im Spiel sind keine zusätzlichen Porträtkameras nötig; Ausbauporträts werden bei Bedarf geladen.

Finales Rendermanifest: `Assets/Art/ModelPortraits/v1/manifest.json`, SHA `81e3434b8b40bbdee9c6c87bb25dcf19098b8c2d62e1e284518bbff56a707ea6`. Unabhängiger Designerreview aller 31 Bilder einschließlich korrigierter Mündungen und Sprinterform: `Docs/ResearchEvidence/model-portraits-v05-final-review.json`. Ergebnis ist ein gemeinsamer Low-Poly-Stil; eine identische Qualität zur weich gerenderten Konzeptserie wird nicht behauptet.

Der zentrale PlayMode-Lauf renderte außerdem alle **27 tatsächlichen BoardView-Ausbauzustände**, prüfte vollständige Kamerabounds und unterschiedliche Bilder. Nachweis: `Docs/Release05VisualEvidence/upgrade-models/`.

Isolierte finale Projection-/Canvas-Abnahme: **2/2 bestanden**, Ende 21:23:51 UTC, **42 Full-Canvasbilder plus drei Boardbilder**. Drei Biome, SafeFrames 1280×720/1600×720, Übersicht, Info, Goldmangel, Goldmangel mit Info, Gegneransicht, Ausbau/Zoom und vier Truppenklassen. Die Kampfbilder sind kontrolliert eingesetzte autoritative Testzustände. Tatsächlicher Capturecanvas **640×480 mit simulierten SafeFrames**, keine nativen 1600-Pixel- oder Handybilder. 59 Quellen/Porträts passen zum finalen Stand. `Docs/MapHudAuditEvidence/overlay-v05/final/`.

Der exakte Terrainvergleich lief zweimal in isoliertem Unity, zuletzt mit finalen Quellen: **365.202 Vertices / 121.734 Dreiecke** über drei Karten. Positionen, Normalen, Indizes, Indexformat und Terrainbounds sind bytegenau zur 0.4 identisch; ausschließlich die Farben änderten sich. Finale Belege: `Docs/ResearchEvidence/terrain-v05/unity-mesh-final-result.json` und `unity-mesh-final-manifest.json`. Der nun zweifarbige Reichweitenkreis bleibt außerhalb des Terrainmeshes.

**Native Sichtprüfung:** Das echte macOS-Fenster 0.5 zeigte eine laufende Tal-Partie mit Rapid, Mine, Läufer-/Sprinter-/Schwarmformen, farbigen und grauen Kaufkarten, Info-i, Zoomsteuerung und Gegner-PiP. Root öffnete die neue App und beobachtete sie anschließend nur lesend; keine native Spielaktion wurde durch Root ausgeführt. Die bereits aktive Partie wurde nicht übernommen oder beendet. Interaktionsbelege stammen aus den PlayMode-Tests. `TestResults/visual-acceptance-v05.json` und `Docs/Release05VisualEvidence/native-observation.json`.

## Netzwerk mit dem tatsächlich gebauten 0.5-Player

Je Transport wurden zwei separate headless macOS-Player gestartet. Neue Ergebnisse überschreiben keine 0.4-Belege. Kein laufendes GUI-Spiel wurde bedient oder beendet.

| Transport | Tatsächliches Ergebnis |
| --- | --- |
| LAN, Grünes Tal, Port 7827 | Zwei vollständige Partien mit beidseitiger Revanche; je 2.812 Spielticks / 140,6 s; Player0, HP 1:0. Identische Enddigests, frische Epochen und Ressourcen. |
| Echter Relay, Sonnenpass, DTLS | Reale Authentifizierung sowie Allocation/Join mit getrennten anonymen Identitäten. Zwei vollständige Partien mit Revanche; je 2.733 Ticks / 136,65 s; Player0, HP 1:0. Identische Enddigests und frische Ressourcen. |

Jede Runde enthält sieben gemeinsame Zustände mit Schaden+Reichweite, freie Mine samt Einkommen, Verkauf, Rapid-Buy und korrekte Gold-/Queue-Ablehnungen. Vier Player Exit 0, keine gemeldeten Logfehler. Vor und nach den Läufen wurden zwölf Quellen, fünf Managed-Assemblies und die ausführbare Datei unverändert geprüft. Die Quellen entsprechen der geprüften 0.4. Aggregat: `TestResults/network-v4-artreview-v05-acceptance.json`; Quellen-/Binarybindung: `network-v4-artreview-v05-source-manifest.json`.

Die bereits bestandenen zehn sauberen und zehn gestörten LAN-Zyklen für die unveränderte Logik bleiben in `Docs/Release04TestReport.md` und den ursprünglichen `*-v4`-Verzeichnissen erhalten. Sie wurden für den reinen Artumbau nicht erneut breit wiederholt. Zwei Prozesse auf diesem Mac belegen **keine zwei Android-Geräte oder getrennten Internetzugänge**.

## Definition of Done – einzeln ausgewiesen

| Nr. | Kriterium | Status und Grenze |
| --- | --- | --- |
| 1 | Frischer Import und dokumentierter Build | **Bestanden.** Frischimport ohne Library, Porträtprüfung und reale Desktop-/APK-Builds. |
| 2 | Zwei Personen spielen eine vollständige Partie | **Nicht geprüft mit zwei Menschen.** Der vollständige Befehls-/Partienablauf ist mit zwei echten Playerprozessen geprüft. |
| 3 | WLAN ohne Cloud und Online mit Code | **Bestanden auf Desktop**, einschließlich echtem Relay/DTLS. Getrennte Internetzugänge bleiben ungeprüft. |
| 4 | APK mit Größe, SHA und Signatur | **Bestanden.** Angaben und Beleg oben; Development-Testsignatur. |
| 5 | Android-Gerät und Leistungsziele | **Nicht geprüft.** Installation, Touch, Hintergrund/Lock, 15-Minuten-Last, Thermik, Speicher und FPS folgen erst später. |
| 6 | Kernprüfungen, Käufe, Partien und Neustarts | **Bestanden im dokumentierten Testumfang.** 89/89 EditMode, 43/43 PlayMode, reale LAN-/Relay-Revanchen; gefundene Fehler behoben. |
| 7 | Assetherkunft und Lizenzhinweise | **Bestanden.** Kenney-CC0-Belege, Unity-Paketlizenzen, eigene Modelle/Renders und dokumentierte Konzeptbilder. Keine ungeklärten Tripo-/Fab-Modelle übernommen. |

Menschliche Balancebewertung bleibt ausstehend; frühere Botvergleiche zeigen bei defensiven Strategien Zeitlimit-Remis. 60 FPS und stabiler 30-FPS-Modus sind Ziele bis zur echten Gerätemessung. Kein Billing, keine Store-Veröffentlichung und keine kostenpflichtigen Assets wurden aktiviert.
