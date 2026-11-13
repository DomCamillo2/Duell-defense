# Testbericht – Defense Duell 0.6

Stand: 11. November 2026. Unity 6000.3.28f1; Core v1.8; Content-Schema 5; Wire 5. Basis: a80c882 (0.5). Alte Berichte und Vorschauen bleiben erhalten.

## Änderungen

Die Aktionskarten haben gezeichnete, abgeschrägte Rahmen, eigene Klassen-/Aktionssymbole, separate Vorher-/Nachher-Werte und goldene Preisfelder. Zwei gemeinsame Ausbau-Diamanten zeigen das Gebäudebudget. Goldmangel und Maximalstufe sind grau und ausdrücklich beschriftet. Verkauf und Schließen haben eigene Symbole. Die Karten verwenden die bestehenden Pointer-, Info- und Modal-Sperren.

Der Tempo-Knopf zeigt 1×/2× und beide bestätigten Zustimmungen. Multiplayer beschleunigt nur mit beiden Stimmen; jeder kann seine Zustimmung zurückziehen. Eine noch nicht bestätigte Anfrage gilt nicht als Zustimmung. Offline spiegelt der Coordinator die lokale Wahl für den Bot. Countdown, Heartbeats und Verbindungs-Timeouts bleiben unverändert. Bewegung, Angriffe, Einkommen, Spawnabstände und Spieluhr skalieren gemeinsam über zusätzliche unveränderte 20-Hz-Spielschritte; Time.timeScale bleibt 1. Ein Tempowechsel verwirft angesammelte Echtzeit, um Nachholsprünge zu verhindern. Revanche setzt beide Stimmen zurück.

## Builds und Artefakte

- Android: `Builds/Android/DefenseDuell-0.6.apk`, auch `DefenseDuell.apk`; **44.314.787 Bytes / 42,26 MiB**.
- SHA-256: `cea9e9b91bd931a68d0832dde65611259df165696cd2b25a2327004a4906c81b`.
- Version 0.6.0 / Code 6; `de.defenseduell.game`; ARM64 / IL2CPP / GLES3; min API25, target API35. APK-v2 verifiziert; Development-Build mit Android-Debug-Testsignatur, keine Store-Signierung.
- Separate Mac-Vorschau: `Builds/macOS-SpeedReview/DefenseDuell.app`, Bundle `de.defenseduell.preview.speed`, Fenster `Defense Duell 0.6.0`.
- Quellarchiv: `Artifacts/DefenseDuell-0.6-Source.zip`; genaue Größe/Prüfsumme in `TestResults/source-artifacts-v06.json` und der SHA-Datei neben dem Archiv.
- APK-Prüfung: `TestResults/build-artifacts-v06.json`. Handytests sind auf Wunsch des Nutzers auf später verschoben.

## Automatisierte Prüfungen und Darstellung

**96/96 EditMode und 46/46 PlayMode bestanden.** Belege: `TestResults/editmode-v06.xml`, `playmode-v06.xml`. Neue Tests prüfen Offline-Spiegelung, tatsächliche 1×/2×-Taktrate, Rücknahme, unveränderte Time.timeScale, neuere Zustandsrevisionen im selben Tick, Ablehnung älterer Revisionen, strikte Boolean-Dekodierung und gehaltene Pointer beim Info-Öffnen. Vorhandene Käufe, Placement, Modelle, drei Karten, HUD-/Input-Lifecycle und Neustarts bleiben geprüft.

63 reine Core-Szenarien bestanden, einschließlich 600 tickgleichen Gameplayvergleichen, 72 Kartenpartien und zwölf Economy-Partien. Unabhängiger Core-Review: 36.254 Assertions in fünf Gruppen einschließlich 1.024 Befehlsreihenfolgen. Nachweise: `Tools/CoreHarness/Results/v1.8-source-manifest.json` und `Docs/ResearchEvidence/speed-consent-v06-core-review.json`.

Wire-Harness: **57.542 Prüfungen**, 24 vollständige Botpartien auf drei Karten. Maximale gültige Snapshotgröße 7.766 Bytes, 38.830 Payload-Bytes/s bei 5 Hz ohne Transport-Overhead. **56 Python-Tests** prüfen den Evidenzvergleich einschließlich manipulierter Tempo-/Einkommensbelege. `TestResults/wire-v5-verification.json`.

Der erste PlayMode-Lauf hatte 45/46 Erfolge: Die alte Textprüfung suchte alle Werte in einem einzigen Textfeld. Sie wurde an die neue getrennte Werteinzeige angepasst. Die echte Bildprüfung fand außerdem einen fehlenden CanvasRenderer an den gezeichneten Aktionsflächen; dieser wurde ergänzt und die Wertezeile höher gesetzt. Erst danach wurde der vollständige PlayMode-Lauf wiederholt und bestanden. Erste Fehlerprotokolle bleiben separat erhalten.

Vier finale echte HUD-Captures (bezahlbar, Goldmangel, Vollausbau, Tempo 2×) wurden visuell geprüft: `Docs/Release06VisualEvidence/`. Es sind 640×480-Batchbilder kontrollierter PlayMode-Zustände, keine Handyfotos. `review.json` bindet Bilder und Runtime-Quellen. 31 Porträts neu mit den aktuellen Content-Hashes gebacken und geprüft; Modelle/Geometrie unverändert zur 0.5.

Frischer Import ohne vorhandene Library: `Builds/FreshImport-20261111-081431`. Prepare und separate Porträtprüfung Exit0, `TestResults/fresh-import-v06.json`.

## Reale Netzwerkprüfung

Die aktuellen Belege stehen in `TestResults/lan-speed-v06`, `relay-speed-v06` und `lan-speed-impaired-v06`. Zwei getrennte headless Player pro Lauf; die bestehenden GUI-Partien werden nicht übernommen. Der Quellen-/Binarybezug steht in `network-v5-speed-v06-source-manifest.json`; das abschließende Aggregat in `network-v5-speed-v06-acceptance.json`.

Jeder Tempodurchlauf misst mindestens zwei Sekunden für eine einzelne Zustimmung (1×), beide Zustimmungen (2×) und einseitige Rücknahme (1×). Die Goldbilanz wird anhand der tatsächlichen Spielticks geprüft. Eine alte akzeptierte Zustimmung wird nach der Rücknahme erneut gesendet: identische alte Quittung, keine erneute Beschleunigung. Veränderte Wiederholung derselben Sequenz wird abgewiesen. Alle Läufe enthalten außerdem freie Baupositionen, gemischte Ausbaupfade, Verkauf, freie Mine und schnelle bezahlte Truppenkäufe.

Zwei gestörte LAN-Zyklen auf Frosthafen bestanden: 75ms Verzögerung, 15ms Jitter und 2% Paketverlust konfiguriert; jeweils 165 gemeinsame Zustands-Snapshots, beide Prozesse Exit0. Die vollständigen LAN- und Relay-Läufe bestanden ebenfalls: je zwei Partien mit beidseitiger Revanche, zurückgesetzten Stimmen und identischen Endzustands-Digests. LAN: je 2.812 Spielticks, ca.137,15s Echtzeit; Relay: je 2.733 Spielticks, ca.133,22s. Insgesamt sechs Playerprozesse Exit0. Das Abschlussaggregat bestätigt unveränderte Quell-/Binary-Hashes. Zwei Prozesse auf einem Mac belegen keine zwei Menschen, Android-Geräte oder getrennten Internetanschlüsse.

Native Vorschau 0.6 geöffnet und Hauptmenü mit drei Karten sowie Offline/WLAN/Online bei 1440×866 gesichtet. Ein Solo-Klick konnte wegen gemeldeter Nutzeränderung und anschließendem CUA-Timeout nicht verifiziert werden; keine native Kauf-/Tempo-Bedienung behauptet. Das Fenster bleibt dem Nutzer überlassen. `Docs/Release06VisualEvidence/native-observation.json`.

## Definition of Done

| Nr. | Kriterium | Status |
| --- | --- | --- |
| 1 | Frischimport und dokumentierter Build | Bestanden: ohne Cache importiert, Porträts geprüft, Mac und APK gebaut. |
| 2 | Vollständige Partie mit zwei Menschen | Nicht geprüft; automatische Zwei-Prozess-Partien separat belegt. |
| 3 | WLAN ohne Cloud und Online mit Code | Bestanden über LAN und echtes Relay/DTLS auf Desktop; vollständige Partien mit Revanche. Getrennte Internetanschlüsse ungeprüft. |
| 4 | APK, Größe, SHA und Signatur | Bestanden; Testsignatur, Angaben oben. |
| 5 | Android-Hardware und Leistungsziele | Ausstehend auf Nutzerwunsch; keine Touch-/Thermik-/FPS-/15-Minuten-Zusage. |
| 6 | Kernlogik, Käufe und Neustarts | Bestanden im genannten automatisierten Umfang; reale Transportabnahme ebenfalls bestanden. |
| 7 | Assetherkunft und Lizenzhinweise | Vorhandene dokumentierte Quellen bleiben gültig; neue UI-Vektoren sind eigener Code. Keine ungeklärten Downloads hinzugefügt. |

Kein Billing, keine kostenpflichtigen Assets und keine Veröffentlichung aktiviert. Menschliche Balancebewertung und Geräteabnahme bleiben offen.
