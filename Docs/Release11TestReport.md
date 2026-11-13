# Defense Duell 0.11 – Test- und Auslieferungsbericht

Stand: 13. November 2026. App **0.11.0 / Android-Code 11**, Core **2.1**, Content-Schema **8**, Wire **7**. Implementiert, nativ kompiliert und getestet; Mac und Android gebaut. Alle zehn LAN-Zyklen bestanden; Quellenpaket wird mit separatem Hashnachweis ausgeliefert. Kein Android-Geräte- oder menschlicher Zweispielertest behauptet.

## Änderungen

Die eigentliche Bodenfläche ist deutlich größer und liegt hinter dem gamifizierten HUD. Drei Karten nutzen gemessene native Renderziele bis 2560 Pixel Kantenlänge mit unterstützter Kantenglättung. Bei1920×1080 wurde gegenüber der rekonstruierten früheren Kamerapassung etwa53% größere lineare Kartendarstellung gemessen. Pan/Zoom erreicht Randbereiche unter Bedienelementen. Materialien, gebackene geometrische AO, Kontaktschatten, begrenzte Effekte, Modellbewegung und ein neues App-Icon ergänzen die Grafik. Die eigene Musik, zehn Aktionsklänge sowie Goldrate und tatsächlicher Ausmarschbonus bleiben enthalten.

Alle sieben Turmklassen besitzen passende unbegrenzte Ausbauten, darunter Frost ausschließlich Frostdauer/Reichweite. Forschung, Geld, Kosten und relevante Kampfwerte verwenden exakte große Ganzzahlen. Es gibt keine früheren künstlichen Gebäude-/Auftrags-/Wellenkaufgrenzen; Geld, freier Boden, FIFO und die endliche Partie bleiben wirksam. Elf sichtbare Meilensteine plus Pfadmodule begrenzen die Modellressourcen, nicht die tatsächlichen Level.278 Porträts stammen aus den produktiven Modellen.

Nach Bauen, Upgrade oder Abwahl bleibt kein alter Baugeist am Pointer hängen. Gastbewegung wird zwischen autoritativen Wegpunkten gepuffert geglättet. Große Wire7-Zustände werden zuverlässig in bestätigten Seiten übertragen und erst vollständig validiert angewendet. Beide Spieler benötigen denselben neuen Build.

Die zusätzlich beauftragte Wirtschaftsüberarbeitung verwendet Mine160G/40% Verkaufsrefund, Kampftürme weiterhin65%, Sendecooldowns12/20/30/24Ticks und mindestens30 aktive Sekunden Sendefenster. Das HUD zeigt ungespawnt gebundenes Gold, ersten Ausmarsch, Ende der FIFO und Beginn eines neuen Pakets getrennt. Die Zeiten sind Untergrenzen; unbekannte Wellenwechsel bleiben ausdrücklich offen.

## Ausgeführte Prüfungen

- **149/149 EditMode und141/141 vollständige PlayMode-Prüfungen bestanden.** Danach nur die Einzahlform der Queue-Beschriftung korrigiert: sechs gezielte EconomyHUD-Prüfungen erneut bestanden. Enthalten sind echte UI-Käufe/Verkäufe, Drag-/Abwahlregressionen,235-Paket-Scrollliste, Render-/Grafik-/Audio-/Lebensdauerprüfungen.
- **102/102 reine Core-Szenarien bestanden**, sowohl über tatsächliche Assets im Economy-Runner als auch im Standard-CoreHarness. Baseline/Kandidat je360 Simulationspartien und360 zusätzliche exakte Kandidat-Bestätigungsreplays. Seitenwechsel und alle drei Karten enthalten. Das ist keine menschliche Balanceabnahme.
- Native Prepare/Schema8-Migration bestanden.278 Porträts gebacken und verifiziert. Gegenüber den einzeln gesichteten0.10-Porträts: Alpha vollständig identisch, RGB-Abweichung höchstens1/255. Alle sieben neuen Economy-HUD-Ansichten gesichtet; die Einzahlkorrektur separat nachgesichtet. Die bisherigen neun Karten-, acht HUD- und sechs arrangierten Feldansichten bleiben versionsgebundene Grafikbelege.
- Netzwerk-Testdriver mit tatsächlichem Core2.1 auf drei Karten × zwei Runden geprüft.43 ursprüngliche Comparator-Prüfungen plus sieben Messfensterregressionen: **50/50 bestanden**. Zusätzlich **9/9 Prüfungen** der automatischen ACK-Ablehnung bestanden.
- Frischer Import ohne Library bestanden: `Builds/FreshImport-20261113-125029`;278 Porträts auch dort verifiziert. `TestResults/fresh-import-v11.json`.

Die Vollsuiten sind in `economy-v11-source-manifest.json` gebunden. `economy-v11-final-deltas.json` weist die drei späteren Änderungen getrennt aus: Queue-Einzahlform mit6/6 Nachlauf, isolierter Mac-Buildpfad und CLI-Testdriver-ACK-Korrektur mit9/9 Regressionen sowie realen Wiederholungsläufen. Es wird keine unveränderte finale Vollsuite behauptet.

## Echte Netzwerkprozesse

**Zehn saubere LAN-Verbindungs-/Aktions-/Shutdown-/Neustartzyklen bestanden**, beide Prozesse Exit0:4558 gemeinsame Snapshots und620 Host-Kommandobelege auf der ersten0.11-Macfassung. Die finale Fassung ergänzt ausschließlich die unten erklärte Behandlung normaler Testbot-Ablehnungen; Core und produktive Session-/Wirequellen sind unverändert.

**Gestörtes LAN:** zwei vollständige Partien mit beidseitiger Revanche auf Frosthafen, beide Prozesse Exit0.466/472 gemeinsame Snapshots und76/74 Host-Kommandobelege. Echter UDP-Proxy:75ms Verzögerung je Richtung, zusätzliche nominale150ms RTT, ±15ms Jitter je Richtung und2% angeforderter Paketverlust. Tatsächlich13/735 Clientpakete und57/2594 Hostpakete verworfen. Das ist kein physisches WLAN-/Android-Frametime-Profil.

**Echter Unity Relay/DTLS:** finale Fassung, zwei vollständige Partien mit Revanche auf Sonnenpass, beide Prozesse Exit0.503/496 gemeinsame Snapshots und78/76 Host-Kommandobelege. Getrennte anonyme Testprofile und tatsächliche Relay-Zuordnung. Zwei Prozesse auf demselben Mac; keine zwei Menschen oder getrennten Internetanschlüsse behauptet. Ein früherer0.11-Relay-Lauf bestand ebenfalls zwei Runden und bleibt separat erhalten.

Zwei Fehler in der Testinfrastruktur wurden transparent korrigiert. Erstens verwendete der automatische Gast-Testbot einen veralteten Goldstand: der Host lehnte bei10G einen18G-Kauf korrekt ab, ohne Zustandsänderung. Nur automatische Testkäufe dürfen diese normale Ablehnung nun akzeptieren; Pflichtaktionen bleiben strikt. Zweitens verdrängte in der Auswertung ein später begonnenes, unfertiges Tempomessfenster eine gültige vollständige Messung. Jetzt werden sämtliche vollständigen Messungen mit unveränderten Rate-/Dauer-/Tickgrenzen geprüft; unvollständige Fenster ersetzen keinen Beleg. Ursprüngliche Fehlerauswertungen und unveränderte Host-/Clientdaten bleiben erhalten.

## Artefakte

- APK: `Builds/Android/DefenseDuell-0.11.apk`, **70.616.730 Bytes /67,35MiB**.
- SHA-256: `257e72a4238bc36411327b8c4f3d704e407f45b7c180406ca8b42db1b7206966`.
- Paket `de.defenseduell.game`; ARM64/IL2CPP/GLES3, minAPI25/targetAPI35, Development mit Android-Debug-Testsignatur. APK-v2 verifiziert. Alle26 Legacy-/Round-/Adaptive-Iconressourcen entsprechen der gesichteten Version. Kein Launcher-Gerätetest.
- Finale Macfassung: `Builds/macOS-EconomyFinalReview/DefenseDuell.app`, Bundle `de.defenseduell.preview.economy.final`. Eigene Headlessprozesse für Tests gestartet; keine interaktive menschliche Abnahme. Erste0.11-Fassung und frühere Nutzervorschauen bleiben erhalten.
- APK-Verpackung einmal ohne inkrementelle Gradle-Restlücken neu erstellt:18.562.493Bytes eingespart, ohne reduzierte Assets. Metadaten/GUIDs des Builds ändern sich regulär; Klassenbibliothek und Bildressourcen wurden nicht reduziert.
- Quellenpaket-Ziel: `Artifacts/DefenseDuell-0.11-Source.zip`. Größe, SHA-256 und Integritätsprüfung werden nach Erzeugung separat in `TestResults/source-artifacts-v11.json` festgehalten, um einen zirkulären Archivhash zu vermeiden.

## Definition of Done

| Kriterium | Status | Nachweis / Grenze |
|---|---|---|
|1. Frischer Import und Build|Bestanden|Gepinnte Unity-Version, neue Mac/APK, Import ohne Library und278 Porträts|
|2. Vollständige Partie mit zwei Personen|Nicht geprüft|Automatische echte Zwei-Prozess-Partien, keine zwei Menschen|
|3. WLAN und echter Relay|Bestanden|Aktuelle tatsächliche Transportpartien; zehn zusätzliche LAN-Zyklen bestanden|
|4. APK mit Größe/Hash/Signatur|Bestanden|Obige gebaute und verifizierte Datei|
|5. Android-Gerät und Performance|Nicht geprüft|ADB listet kein Gerät; keine Installation/Frametimes/Thermik|
|6. Kernprüfungen ohne bekannte partiekritische Fehler|Bestanden|Native/reine Regeln, UI, echte Partien, Testinfrastrukturkorrekturen mit Belegen|
|7. Herkunft/Lizenzen|Bestanden|ASSET_LICENSES.csv, THIRD_PARTY_NOTICES.md und Original-Assetprovenienz|

Offen bleiben tatsächliche Handygesten, Launcherkomposition, Lautsprecherklang,15-Minuten-Frametimes/Speicher/Thermik, zwei Menschen über getrennte Internetzugänge und subjektive Balance. Keine kostenpflichtigen Assets, kein Billing, keine Store-Veröffentlichung. Vier-Spieler-Modus ist nur eine getrennte Machbarkeitsanalyse.
