# Aufgaben und aktueller Stand

Die Codebasis auf `main` ist Version **0.16**. Die nächste Version0.17 mit gemeinsamen Teamkarten wird entwickelt. Eine neue APK entsteht erst nach vollständiger Integration und Prüfung.

## Aktives Update0.17

Verfolgbare Aufgabenliste: [Issue #1 – Gemeinsame Teamkarten und neue APK](https://github.com/DomCamillo2/Duell-defense/issues/1).

Ziel: Teammitglieder bauen und kämpfen auf derselben Karte. Die persönliche Turmauswahl bleibt erhalten. Anschließend eine neue geprüfte APK erstellen.

Basis: Version 0.16 ist gebaut und auf main verfügbar. Sie hat persönliche Spielfelder; gemeinsame Teamfelder sind noch nicht ausgeliefert. Die persönliche Auswahl von 1–5 aus acht Turmklassen ist bereits vorhanden.

- [x] Persönliche Turmauswahl in der Lobby als Ausgangsbasis vorhanden.
- [ ] Gemeinsames Teamfeld in Core und Platzierung integrieren: 2v2/3v3 zwei Felder, 2v2v2 drei Felder; konkurrierende Bauplätze nur einmal bezahlen.
- [ ] Gemeinsamen Kampf für alle Waffen, eindeutige Gegner-Teamziele und genau eine neutrale Wellenfolge pro Team umsetzen.
- [ ] Teamdruck, Boss-LP und Basis-Schaden für mehrere Bauende abstimmen; persönliche Wallets, Forschung, Loadouts, Kaufrechte und eindeutiges Kopfgeld erhalten.
- [ ] Wire11 und Snapshotprüfung auf Feld-/Owner-Semantik migrieren; Ready, Eliminierung und Revanche prüfen.
- [ ] HUD, Drag, Gegneransicht und Besitzerkennzeichnung anpassen; persönliche Lobbyauswahl deutlich „Deine Türme für dieses Match“ nennen.
- [ ] Neue Core-, native UI- und echte 4/6-Prozess-LAN-/Relay-Tests einschließlich Baukonflikt und gemeinsamer Feldansicht bestehen.
- [ ] Erst danach APK0.17 / Android-Code18 bauen, Signatur, Hash und Quellbindung prüfen und ausliefern.
- [ ] Spätere Geräteabnahme: Android-Touch/Audio/Video, zwei Menschen und 15-Minuten-Leistung/Thermik.

Aktueller Status: Core, Netzwerk und UI sind in Bearbeitung. Einzelne Codeänderungen sind keine gemeinsame Abnahme und keine fertige neue APK. Die vorhandene0.16-APK bleibt erhalten.

Kein neuer Pflichtumfang aus bloßen Vorschlägen: Kasernen bleiben ein Plan; Reconnect, Hostmigration und Mortar sind keine stillen Releaseanforderungen.

## Bereits in0.16 umgesetzt

- Solo, 1v1, 2v2, 3v3 und 2v2v2; sechs Karten, 1–200Wellen oder Endlos.
- Persönliche Auswahl aus acht Turmklassen, Mine eingeschlossen.
- Unbegrenzte klassenspezifische Upgrades und atomare1×/10×/Max-Käufe.
- Lasernerf, schnelleres Ausrücken gekaufter Truppen und zwei Eliteklassen ab1200/1600Gold.
- Boss-/Superbosswellen und wachsende Gegnergruppen im späteren Spiel.
- Vollbild-HUD, größere Modellbilder, Goldfeedback, Sounds, Dragkorrekturen und Gastglättung.
-0.16-Abnahme:237EditMode-,217PlayMode-,143reineCore- und181Pythonfälle bestanden. LAN/Relay mit getrennten4/6Prozessen, Revanche, Störnetz und frischer Projektimport geprüft.

## Weitere Grenzen

- Android-Gerätestart und Leistung, zwei menschliche Spieler und getrennte Internetzugänge werden gesondert geprüft.
- iOS-Vorbereitung ist vorhanden; SDK/Unitymodul/Xcode, Signierung und iPhone fehlen.
- Kein Storeupload, Billing, kostenpflichtiger Plan oder kostenpflichtiges Asset beauftragt.
