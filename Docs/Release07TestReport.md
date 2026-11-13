# Testbericht Defense Duell 0.7

Stand: 11. November 2026. App 0.7.0 / Code 7, Core v1.9, Content-Schema 6, Wire 6. Runtime, Mac und Android gebaut; automatisierte Prüfungen und reale Desktop-Transportabnahme bestanden. Native Bedienung geprüft und die eigene Prüf-App anschließend geschlossen. Die finale APK ist zusätzlich unter Android16/API36 in einer getrennten ARM64-Emulation installiert und in zentralen Abläufen geprüft. Der Umfang und die Grenzen stehen unten. Die frühere Meldung einer offenen Freigabe war falsch: ein493,8s dauernder CUA-Aufruf verursachte den Wartezustand; ADB-/GameHost-Starts waren automatisch freigegeben. Historische Berichte bleiben unter Docs/Release04–06TestReport.md erhalten.

## Artefakte

- APK: `Builds/Android/DefenseDuell-0.7.apk`, **45.349.535 Bytes / 43,25 MiB**.
- SHA-256: `85186f2658658aa44e475ffb1776900cc7afeb03d17cd0c7dc639cd7ef0be931`.
- Paket `de.defenseduell.game`; Android ARM64/IL2CPP/GLES3, min API25, target API35. APK-v2 Signatur verifiziert; Development mit lokaler Android-Debug-Testsignatur, keine Store-Veröffentlichung.
- Neue native Vorschau: `Builds/macOS-WaveReview/DefenseDuell.app`, Bundle `de.defenseduell.preview.waves`, Fenster `Defense Duell 0.7.0`. Vorherige Nutzervorschauen unverändert.
- Metadaten: `TestResults/build-artifacts-v07.json`, `mac-wave-v07.json`. Frischimport: `Builds/FreshImport-20261111-104237`, Prepare und separate Prüfung aller43 Porträts bestanden; `fresh-import-v07.json`.
- Quellenarchiv: `Artifacts/DefenseDuell-0.7-Source.zip`, Basischeckpoint `0c97ad6` plus Abschlussdokumentation. Dateiliste/CRC und Pflichtdateien geprüft; Größe/SHA stehen in `TestResults/source-artifacts-v07.json`. Die begrenzte Android-Emulationsabnahme ist dokumentiert; sie ist kein physischer Handy-/Performance-PASS.

## Unity, Core und Oberfläche

**123/123 EditMode, 63/63 PlayMode** bestanden: `editmode-v07.xml`, `playmode-v07.xml`;117 während dieser Läufe unveränderte Quellen in `integrated-v07-source-manifest.json`. Danach erfolgten kleine Text-/Farbkorrekturen im HUD und eine diagnostische ACK-vor-Snapshot-Wartekorrektur. Abschließend **11/11 HUD-Fälle** (`playmode-v07-hud-final.xml`) und Mac-/Android-Compile bestanden. Finale113 Runtime-/Assetquellen und146 Mac-Binärdateien sind im separaten `network-v6-wave-v07-source-manifest.json` gebunden und nach allen Transportläufen unverändert. Diese geschichteten Belege werden nicht als identischer Vollsuite-Quellstand ausgegeben.

Geprüft wurden echte Input-System-Touch-Ereignisse für Drag/Release/Cancel/zweiten Kontakt, Info-/Modal-/Submit-Sperren, gehaltene Kontakte, dynamische Forschung und veraltete Quoten, responsive Header bei900/960/1280, sieben Baukarten,39 Modellzustände, Minenbruchwerte und Zielpriorität. Vier Profil-Lifecycle-Fälle prüfen reales Ergebnis/Revanche, ungebundene Endzustände sowie Fehler bei initialem und abschließendem Save. Ausstehende Belohnungen überleben Menü-/Revanchewechsel im laufenden Prozess; permanenter Schreibfehler plus Prozessverlust kann ungesicherte Daten weiterhin verlieren.

Erste fehlgeschlagene UI-Fixtures und Bilder bleiben erhalten. Die anschließenden Korrekturen betreffen neue sieben Klassen, pausiertes Einkommen, echtes Touchrouting und lesbare Farben. Endbilder: `Docs/Release07VisualEvidence/`, einschließlich tatsächlich geöffnetem Forschungsinfo vor Kauf, Upgradezuständen, Vorbereitung, Priorität, Mine und2×. Es sind Unity-Testbilder, keine Handyfotos.

Core: **79/79 Szenarien**,174 vollständige Partien im produktiven Core einschließlich90 Analysepartien und84 Produktionsbot-/Economypartien; `Tools/CoreHarness/Results/v1.9-source-manifest.json`, `Tools/WaveCoreReview/Results/v1.9-freeze/`.42 der90 Analysepartien erreichen Welle10. Exakte Geldbilanz, Seitenpaarung, Herkunftsgrenzen, Wellenbudget, unveränderliche Paketrezepte, Forschung0–8 und prioritätsabhängiger Kampf geprüft. Defensive Policy gewinnt16/18 gegen den festgelegten Mixed-Probegegner: menschliche Balance bleibt offen.

Drei Botstufen:36 doppelt simulierte Matchpaare,46.118.493 Assertions, identische Wiederholungen. Leicht2/12 Siege gegen Normal, Schwer8Siege/2Remis/2Niederlagen. Gleiche Ressourcen und Regeln; keine Aussage über menschliche Gewinnraten. `Tools/BotWaveReview/Results/final-v07/` plus unabhängiger Root-Quellreview in `TestResults/bot-v07-root-review.json`.

43 Porträts aus denselben tatsächlichen Spielmodellen geprüft.24 Meshassets für Blitzspule/Balliste aus12 originalen Quelldateien importiert. Landmarken ändern keine Core-Karte/Baufläche und bleiben Teil des Terrainmeshs. Geometrie-/Bildnachweise in `Docs/ResearchEvidence/map-art-v1/` und `tower-unlocks-v1/`.

## Netzwerk: echte Prozesse und unabhängiger Binärvergleich

Alle sechs Playerprozesse Exit0. Belege: `TestResults/network-v6-wave-v07-acceptance.json` sowie die Ordner `lan-wave-0.7-v6`, `relay-wave-0.7-v6`, `lan-impaired-wave-0.7-v6`.

- LAN auf Grünes Tal: zwei vollständige Partien mit bestätigter Revanche;530/527 gemeinsame Snapshots. Je1952 aktive Ticks, ca.110s Echtzeit.
- Echtes Relay/DTLS auf Sonnenpass: zwei vollständige Partien mit Revanche;489/489 gemeinsame Snapshots. Je1794 aktive Ticks, ca.102,7s Echtzeit. Tatsächliche Allocation/Join und getrennte anonyme Authentifizierungsprofile geprüft.
- Beide Vollpartienstrecken enden regulär in Welle2 mit beidseitigem Basisverlust und identischen Enddigests. Das gezielte Szenario verkauft seine Verteidigung für Kauf-/Minenprüfungen. **Kein Nachweis von zehn vollständig durchgespielten Netzwerkwellen.** Zehn-Wellen-Ende ist separat im Core geprüft.
- Gestörtes LAN auf Frosthafen: zwei vollständige Aktions-/Wellen-/Forschungsszenarien mit Abbruch/Neustart,339/337 gemeinsame Snapshots.75ms je Richtung,±15ms Jitter,2% nominaler Verlust; tatsächlich12/514 Client- und22/1197 Hostdatagramme verworfen. Keine vollständigen Ergebnispartien in diesem Modus.

Jede Strecke prüft Bauen, identische/veränderte Wiederholung, gemischte Ausbaupfade, Priorität, Verkauf, freie Mine,1×/2×/Rücknahme und alte Tempobestätigung; außerdem Baupause, einzelne/beidseitige Wellenbereitschaft, Forschung, veraltete Kaufangebote ohne Zahlung, alte/neue Rezepte, FIFO in Zukunftswellen, tatsächlichen Spawnbonus und Rapid-buy bis Goldmangel. Der unabhängige Vergleich dekodiert echte Binärframes und hostseitige Vorher-/Nachher-Belege, statt nur Treiber-PASS-Flags zu übernehmen.

Zusätzliche reine Prüfungen:38 Wire-NUnit-Methoden,141.184 Wirechecks mit24 vollständigen Botpartien/16.536 Frames,24 negative Python-Belegtests. Maximale gültige Schemafixture12.241Byte,14Chunks;61.205Payload-Byte/s bei5Hz ohne Transport-Overhead. Treibermodell auf drei Karten mit je zwei Partien ist ausdrücklich kein zusätzlicher Transportnachweis.

## Noch getrennt abzunehmen

Native Endsichtung: `Docs/FinalUXReview.md`. Menü, Bau-Bestätigung, beide Upgradepfade/MAX, Forschung, FIFO, PiP, Ergebnis/Revanche und Tempo sind direkt in der App bestätigt. CUA-Drag löste nur die alternative Bauvorschau aus; nativer Drag wird deshalb nicht als bestanden behauptet. Android-Installation/Emulation: Designchat, ausschließlich die oben gehashte APK. Mactician1.1.3 stellt das vorhandene Android-SDK/GameHost bereit; dessen TFT-Launcher wurde nicht umgebaut. Eigene AVD `DefenseDuellReview`, Android16/API36, ARM64,1920×1080, Serial `emulator-5582`. `adb install` erfolgreich; Paketversion0.7.0/Code7/ABI/Signatur per dumpsys bestätigt. Sichtbar geprüft: Hauptmenü, Solo-Lobby und Partie, Kanone per Antippen+Bestätigen, Schaden16→26, Reichweite155→186, gemeinsames MAX bei2 Käufen, Vorne→Stärkster, Tank fürWelle2/Stufe0, sichtbarer Spawnbonus+0,27G/s, Läuferforschung0→1→2 (HP34→38→42, Tempo104→106→108), vergrößerte Gegnerkarte ohne eigene Baukarten und Zoom100→125%. Belege: `Docs/ResearchEvidence/mactician-v07/`.

Die CUA-Drag-Geste erzeugte weder beim Turmbau noch beim Kartenpan eine verlässlich erkennbare Bewegung. Später verlor die Fenstersteuerung wiederholt den Zugriff, obwohl die App zuvor weiterlief. **Kein Drag-/Pan-PASS und keine eindeutige Zuordnung zu einem Runtimefehler.** Tempo, Mehrfinger, Minen und Revanche sind in dieser Androidstrecke nicht abschließend geprüft; entsprechende Core-/Unity-/native Desktopbelege bleiben getrennt. Keine weitere UI-Steuerungsumgehung und kein zusätzlicher Emulatorstart. Physisches Samsung, reale Touch-Haptik,15-Minuten-Thermik/FPS, zwei Menschen und getrennte Internetanschlüsse bleiben ungeprüft. Keine Leistungs- oder finale Balancezusage. Kein Billing, keine bezahlten Assets und keine Veröffentlichung.

## Ausgeführte Einstiegspunkte

```sh
bash Tools/unity.sh prepare
bash Tools/unity.sh portraits
bash Tools/unity.sh edit-tests
bash Tools/unity.sh play-tests
bash Tools/unity.sh wave-review
bash Tools/unity.sh android
python3 Tools/verify_android.py
python3 Tools/verify_fresh_import.py
python3 Tools/check_project_state.py --check-frozen
```

Die vollständigen Unity-Testläufe wurden danach als `editmode-v07` und `playmode-v07` archiviert. Der gezielte Abschlusslauf verwendete einen Filter auf `WaveHud07Tests`; der genaue Aufruf steht im finalen Unity-Log. Die drei Transportbefehle stehen unverändert in Tools/NetworkTests/README.md. Kein erneuter breiter Testlauf allein für Dokumentationsänderungen.

## Definition of Done

| Nr. | Kriterium | Status |
| --- | --- | --- |
| 1 | Frischimport und dokumentierter Build | **Bestanden:** Import ohne Library,43 Porträts, Mac und APK. |
| 2 | Vollständige Partie mit zwei Menschen | **Nicht geprüft.** Vier vollständige automatische Transportpartien separat bestanden. |
| 3 | WLAN ohne Cloud und Online mit Code | **Bestanden** über echte Desktopprozesse und Relay/DTLS; getrennte Internetanschlüsse nicht geprüft. |
| 4 | APK, Größe, SHA und Signatur | **Bestanden:**0.7.0/Code7, Testsignatur, obige Datei. |
| 5 | Android-Gerät und Leistungsziele | **Nicht geprüft auf physischer Hardware.** Installation/Start und zentrale Abläufe in Android16-ARM64-Emulation bestätigt; Drag/Pan und physische Leistungsziele offen. |
| 6 | Regeln, Käufe, Neustarts | **Bestanden** im dokumentierten automatisierten und nativen Umfang; keine bekannten blockierenden Runtimefehler. |
| 7 | Assetherkunft/Lizenzen | **Bestanden:** ASSET_LICENSES.csv/THIRD_PARTY_NOTICES.md, originale neue Modelle/Geometrie, keine ungeklärten Galeriedownloads. |
