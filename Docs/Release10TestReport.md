# Defense Duell 0.10 – Integrationsprüfung

Stand: 13. November 2026. **Gebauter Zwischenstand; finale Transport-/Frischimportabnahme folgt auf 0.11.**

## Quellstand

App 0.10.0 / Android-Code 10, Core v2.0, Content-Schema 7, Wire 7. Unbegrenzte klassenspezifische Upgrades und Forschung, exakte Ganzzahlen, dynamische Darstellungsressourcen, Vollbildkarte/neues HUD, Dragkorrektur, gepufferte Gastbewegung und paginierte Zustandsübertragung sind integriert. Gemeinsamer Vertrag: Iteration10Contract.md.

## Tatsächlich ausgeführt

- Unity 6000.3.28f1 Prepare: erfolgreich. Content-Schema 7 migriert; Default-, Android-Legacy-, Round- und Adaptive-Icon-Slots erfolgreich zurückgelesen.
- Native Modellporträts: 278 erzeugt und verifiziert; manifestgebundene produktive Geometrie, Material und Kamera.
- Unity EditMode: 140/140 bestanden, `TestResults/editmode-v10.xml`.
- Aktueller Core: 94/94 reine Szenarien, einschließlich 72 Kartenpartien und zwölf Economy-Partien. Neue Bot-Spezialpfad- und letzte-Clearing-Forschungsregressionen enthalten.
- Wire/Bewegung: 41 isolierte NUnit-Prüfungen und acht unabhängige Python-Prüfungen bestanden. Mehr als 158.000 Harness-Assertions; kein Ersatz für echten Netzwerktransport.
- Quellbindung der ersten gemeinsamen Unity-Prüfung: `TestResults/fullscreen-v10-source-manifest-initial.json`.

## Reviewkorrekturen

Unabhängige Sichtung fand einen verlorenen ersten Seiten-ACK beim Wechsel eines unvollständigen kleinen UDP-Zustands auf eine große zuverlässige Revision. Explizite Seitenannahme ersetzt die fehlerhafte Gesamtbytezunahme. Bestätigte zuverlässige Seiten bleiben bis zum Verbindungs-/Session-Reset erhalten, auch bei Empfangspausen. Regression ausgeführt.

Der Bot berücksichtigt jetzt die tatsächliche zusätzliche Flächen-/Kettenwirkung. Forschung nach dem Ende aller weiteren Truppenkäufe wird auf Host und UI ohne Zahlung abgewiesen. Alte Testaufrufe wurden auf die tatsächlichen Unity- und BigInteger-APIs migriert.

## Abschluss des Zwischenstands

Vollständige PlayMode-Suite135/135 bestanden. Danach letzte Tooltip-Randänderung separat6/6 bestanden und beide Dragansichten visuell geprüft. Neun Kartenansichten, acht HUDansichten, sechs arrangierte Felder und alle278 Porträts geprüft. 177 AO-Gruppen/6.596.648 Strahlen ohne Abbruch; native Licht-/Schatten-/Ressourcentests enthalten.

APK `Builds/Android/DefenseDuell-0.10.apk`:70.780.698 Bytes, SHA256 `3de36851c7afc878fac43444ea684497f53f7dbb391ebb4d57153f011494c4e9`. ARM64/IL2CPP/GLES3, Development-Testsignatur/APK-v2 verifiziert. Quell-/Test-/APK-Bindung `TestResults/apk-v10-source-manifest.json`. Noch nicht auf Android gestartet.

Kein neuer Mac-, echter LAN-/Relay- oder Frischimportnachweis für0.10. Diese Abschlussprüfungen erfolgen auf0.11 mit der anschließend beauftragten Wirtschaftsüberarbeitung. Physische Handy-Frametime/Speicher/Thermik, zwei menschliche Spieler über getrennte Internetzugänge und subjektive Sound-/Balanceabnahme bleiben gesondert offen.
