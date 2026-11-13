# Testbericht Defense Duell 0.9

Stand: 11. November 2026. App **0.9.0 / Android-Code 9**, Core v1.9, Content-Schema 6, Wire 6. Darstellung, Einkommen-Rückmeldung und Audio sind integriert und in Unity gestartet. Mac und Android sind gebaut; der neue LAN-Lauf und ein Frischimport ohne Library sind bestanden. Historische Nachweise bleiben im [0.8-Bericht](Release08TestReport.md) und [0.7-Bericht](Release07TestReport.md) erhalten.

## Änderungen

Die Landschaft reicht hinter dem HUD bis an die Bildschirmränder. Die Kamera hält in der zurückgesetzten Übersicht die wesentlichen Spielinhalte und die Basis im freien Bereich. Das Haupt-Renderziel folgt tatsächlichen Bildschirmpixeln und Skalierungsänderungen bis 2560 Pixel Kantenlänge; zuvor war die Hauptansicht auf1152 begrenzt. Unterstützte4×-MSAA (2×in der Vorschau, Geräte-Fallback) glättet Kanten. Ruhigere Grünvariationen, breitere Wegschultern, kleine Bodenblumen und eine zum Biom passende Umgebung ergänzen die drei Karten. Materialien und alle43 Modellporträts wurden neu gerendert. Keine Spielgeometrie oder Kollisionsregel geändert.

Gold zeigt die aktuelle Rate pro **Spielsekunde**. Bei2× läuft diese Rate doppelt pro echter Sekunde; während der Baupause steht0G/s. Ein kurzer separater Plus-Hinweis zeigt nur tatsächlich zusätzliches Angriffseinkommen nach dem autoritativen Spawn. Kauf, neutrale Gegner, veraltete Zustände, Rebuild und Fokuswechsel erzeugen keine erfundenen Boni. Die bestehenden Goldregeln bleiben unverändert.

Bei gewählter Bauklasse markieren dezente Bodenpunkte gültige Flächen. Die Ziehvorschau zeigt das echte Modell, Preis, Gültigkeit und Ablehnungsgrund und bleibt innerhalb der SafeArea. Die Platzierungsvalidierung bleibt im Core. Gamifizierte Upgrade-/Verkaufsbuttons, kompakte FIFO und gegenseitiges2× bleiben erhalten.

Neue eigene PCM-Musik (20s/acht Takte/96BPM) und zehn semantische Aktionsklänge ersetzen die vorige Klangpalette. Begrenzter Stimmenpool, Lautstärkeregelung, Mute, Fokus-/Pause-/Gerätewechsel und saubere Clip-Lebensdauer sind geprüft. Es gibt keine externen Aufnahmen oder Laufzeit-Downloads. Hörprobe: `Docs/ResearchEvidence/audio-v09/Preview/showcase-default-mix.wav`. Technische Prüfung ist keine subjektive Hörabnahme auf dem Handy.

## Ausgeführte Prüfungen

- **96/96 PlayMode PASS**, `TestResults/playmode-v09.xml`,110s,16:19:31Z–16:21:21Z.124 Quellen im `immersive-v09-source-manifest.json` gebunden. Enthalten sind sechs neue Immersive-HUD-, drei Renderqualitäts- und16 Audiofälle sowie bisherige Käufe, Projektion, Drag, Modal-/Queue- und Lebensdauerregressionen.
- Erster Lauf95/96: Ein Legacytest wählte einen legalen Bodenpunkt innerhalb des großzügigen Touchziels eines bestehenden Turms. Der Test sucht nun tatsächlich freie Boden-Touchziele. Runtime blieb unverändert. Erstes XML/Manifest bleiben als`-first` erhalten. Ein zwischenzeitlicher zusätzlicher Capture-Aufruf scheiterte an getrennter Testassembly; vor dem erfolgreichen Vollrun entfernt, Compilerlog separat erhalten.
- Native1920×1080, Skalierungsänderung auf2560×1440 und begrenztes4K-Renderziel geprüft; stabile Texturwiederverwendung und Freigabe alter Texturen/Meshes bestätigt. Legale Hinweise reagieren auf belegte Flächen.
- **599/599 .NET-PCM-/Gatechecks PASS**, `Docs/ResearchEvidence/audio-v09/Preview/pcm-review.json`; tatsächliche Synthese, Signalpegel, Loopnaht, erste50ms der Effekte und Spambegrenzung.16 Unity-Audiofälle sind in96/96 enthalten.
- Root-Bildsichtung plus unabhängiges Review von zwölf tatsächlichen1350/1440/1920×1080-Captures: Vollbild, Lesbarkeit, freie Basis, fünf/sieben Klassen, Goldrate, Plus-Hinweis, Bauchip und Details ohne blockierenden Befund. `Docs/FinalUXEvidence/immersive-v09/review-01/` bewahrt den95/96-Zwischenstand und den96/96-Nachtrag separat. `Docs/Release09VisualEvidence/` enthält Vorherbild des Nutzers und tatsächliche neue Spielbilder. Testbilder nutzen kontrolliertes Gold/Freischaltungen, nicht geändertes Startgold. Sonnenpass/Frosthafen zusätzlich als640×480-Projektionsbilder gesichtet; keineHD-Abnahme dieser Bilder behauptet.
-27 Core/Networking/Content/Platform/Progressionquellen unverändert gegenüber0.8. Core-, Networking-, Content- und Progression-DLLs im neuen Mac-Build bytegleich mit0.8. Presentation und Platform neu kompiliert. Historische EditMode/Core/Bot/Wire/Relay-Belege gelten ausschließlich in ihrem dokumentierten Quellumfang.

## Artefakte und Netzwerk

- Android: `Builds/Android/DefenseDuell-0.9.apk`, **45.395.499 Bytes / 43,29 MiB**, App0.9.0 / Code9.
- SHA-256: `ba49555644417a93bf1eaffa0f66f03d3fdd07a90a1a0c8e896d7a89b5114f58`.
- Paket `de.defenseduell.game`, ARM64/IL2CPP/GLES3, minAPI25 / targetAPI35. APK-v2-Signatur verifiziert, Development mit Android-Debug-Testsignatur. Metadaten: `TestResults/build-artifacts-v09.json`.
- Mac: `Builds/macOS-ImmersiveReview/DefenseDuell.app`, Bundle `de.defenseduell.preview.immersive`, Fenstername `Defense Duell 0.9.0`. Als zwei eigene Headlessprozesse gestartet und beendet; kein interaktiver nativer Spieltest behauptet. Vorhandene Nutzervorschauen unverändert.
- **Neuer realer LAN-Lauf PASS:**361 gemeinsame Snapshots,50 Hostbefehle, beide ProzesseExit0. Ein Aktions-/Wellen-/Forschungszyklus einschließlich schneller Mehrfachkäufe und beidseitiger Tempowahl. Keine neue Vollpartie oder getrennten Internetanschlüsse behauptet. `TestResults/lan-immersive-v09/comparison.json` und `network-v6-immersive-v09-acceptance.json`.
-93 Runtimequellen und303 Dateien des Mac-Builds nach LAN unverändert gebunden: `network-v6-immersive-v09-source-manifest.json`. Echte Relay-Vollpartien/Revanche und beeinträchtigtes LAN wurden auf0.7 durchgeführt; unveränderte Kernassemblies, kein neuer0.9-Relay-Lauf.

- **Frischimport ohne Library PASS**, `Builds/FreshImport-20261111-162545`, separate Prüfung aller43 Porträts ebenfalls PASS. Beleg: `TestResults/fresh-import-v09.json`; Contenthash unverändert429ab672fc464511fbc3ff3f99d090bea03be6ab0db5e753b98be3cb36c53a6b.
- Quellenarchiv: `Artifacts/DefenseDuell-0.9-Source.zip`. Größe, SHA-256, CRC-/Pflichtdatei-/Quellhashprüfung stehen im separat erzeugten `TestResults/source-artifacts-v09.json`. Vorherige0.8-APK und Quellenarchiv bleiben erhalten. Keine Caches, privaten Schlüssel oder aktiven Folgelabs enthalten.

## Reproduktion

```sh
bash Tools/unity.sh play-tests
bash Tools/unity.sh immersive-review
bash Tools/unity.sh android
python3 Tools/verify_android.py Builds/Android/DefenseDuell-0.9.apk
python3 Tools/verify_fresh_import.py
python3 Tools/check_project_state.py --check-frozen --manifest TestResults/network-v6-immersive-v09-source-manifest.json
```

Den vollständigen Testlauf per Unity-Argumenten `-testResults`/`-logFile` versionsgebunden ablegen. Für LAN den neuen0.9-Player mit `Tools/NetworkTests/run_lan.py` mit `--cycles 1 --rapid-buy --speed-vote` starten (separates neues Ausgabeziel).

## Definition of Done

| Kriterium | Status | Nachweis / Grenze |
| --- | --- | --- |
| 1. Frischer Import und Build | Bestanden | Unity6000.3.28f1, Mac/APK, Frischimport und43 Porträts |
| 2. Vollständige Partie mit zwei Personen | Nicht geprüft | Automatische echte Prozesse; keine Abnahme mit zwei Menschen |
| 3. WLAN und echter Relay | Bestanden | NeuesLAN0.9; echteRelay-Vollpartien/Revanche0.7 mit unveränderten Kernassemblies. Kein neuerRelay-/Getrenntes-Internet-Test |
| 4. APK mit Größe, Hash und Signatur | Bestanden | Neue0.9-APK, Metadaten oben |
| 5. Androidgerät und Performance | Nicht geprüft |0.9 noch nicht aufAndroid gestartet; kein physischer15-Minuten-/Mehrfinger-/Lautsprecher-PASS |
| 6. Kern- und Integrationsprüfungen | Bestanden |96/96PlayMode,599PCM-Checks, neuerLAN-Lauf, unveränderte Kernnachweise |
| 7. Herkunft und Lizenzen | Bestanden | ASSET_LICENSES.csv / THIRD_PARTY_NOTICES.md, eigene Landschaft/PCM, keine neuen bezahlten Assets |

## Offene Grenzen

 Keine Android-0.9-Installation, physische Leistungsmessung, direkte Mehrfingerabnahme oder menschliche Zweispielerpartie behauptet. Der Nutzer hat die Installation/Funktion der zuvor gelieferten Version bestätigt; das ist eine Nutzerbeobachtung, kein eigener0.9-Gerätenachweis.

Die später angefragten unbegrenzten und klassenspezifischen Ausbauten bleiben separat offen. Insbesondere Frost-Dauer/Reichweite und unbegrenzte Gebäude-/Forschungs-/Truppenkäufe erfordern ein eigenes Core-/Wire-/Ressourcenpaket. Die isolierten Labs sind keine0.9-Runtime und werden nicht in das eingefrorene0.9-Quellenarchiv aufgenommen.
