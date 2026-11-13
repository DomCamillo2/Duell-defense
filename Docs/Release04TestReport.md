# Testbericht – geprüfter Zwischenstand 0.4.0

Stand: 10. November 2026. Unity6000.3.28f1, Corev1.7, Schema5, Wire4. Der neue Artauftrag0.5 ist noch nicht implementiert und nicht durch diese Ergebnisse abgenommen. Aktueller Projektstatus: PROJECT_STATE.md.

## Artefakte und Start

- Android: `Builds/Android/DefenseDuell-0.4.apk`, zusätzlich `DefenseDuell.apk` bis zum nächsten Build.
- Größe: **42,777,991 Bytes / 40.8 MiB**.
- SHA-256: `0e7b9fe4dbc7f3b8fca1f61607c36da0e9a2983bf65c8d870f81af94301a547f`.
- Version0.4.0 / Code4, Paket `de.defenseduell.game`, ARM64/IL2CPP/OpenGL ES3, minAPI25, targetAPI35.
- Development Build, Android-Debug-Testzertifikat, APK-v2 erfolgreich mit `apksigner` verifiziert. Keine Release-/Store-Signierung.
- Desktop: `Builds/macOS-Candidate/DefenseDuell.app`, Bundle `de.defenseduell.preview.candidate`, Fenster **Defense Duell 0.4.0**. Tatsächlich nativ gestartet, Solo-Kauf/Rangeupgrade/Zoom/PiP/Rückkehr geprüft, Hauptmenü dem Nutzer übergeben. Diese App und die ältere0.3-Vorschau bleiben geschützt.
- Quellarchiv: `Artifacts/DefenseDuell-0.4-Source.zip`; separate SHA-Datei. Vollständiger Unity-Quellstand mit Metas, gepinnten Paketen, Dokumentation und Prüfharnesses; ohne Library, private Schlüssel oder Buildprodukte.

## Tatsächlich ausgeführte zentrale Befehle

```sh
bash Tools/unity.sh prepare
bash Tools/unity.sh edit-tests
bash Tools/unity.sh play-tests
bash Tools/unity.sh candidate
bash Tools/unity.sh android
python3 Tools/verify_android.py
python3 Tools/verify_fresh_import.py
python3 Tools/package_project.py
```

Prepare beendet mit `DEFENSE_PREPARE_OK`. EditMode: **89/89 bestanden**, Ende20:32:28UTC. PlayMode: **35/35 bestanden**, Ende20:34:13UTC. Archive `TestResults/editmode-v04.xml` und `playmode-v04.xml`; ursprüngliche0.3-XMLs bleiben separat. Android-BuildSucceeded; die tatsächliche APK-Dateigröße stammt aus dem Dateisystem, nicht aus Unitys Summe aller Buildprodukte.

Frischer Import in `Builds/FreshImport-20261110-204121` ohne Library: Exit0, Prepare bestanden, korrekter v1.7-Contenthash. `TestResults/fresh-import.json` und Log. Die neue Kopie wurde aus Assets, Packages und ProjectSettings erzeugt, nicht aus dem bestehenden Library-Cache des Projekts.

## Regeln, Käufe und Darstellung

- Core:57/57 Szenarien,72 vollständige Kartenpartien/36 exakte Seitenpaare und12 Economy-Partien. Eigene Gold- und Request-Prüfungen, freie Minen, zwei Damage-/Range-Käufe, Verkauf und Klassenlimits.
- Bot:72 Szenarien plus Replays,96 Strategievergleiche. Beide Upgradepfade beobachtet. Defensive Zeitlimitpartien sind eine dokumentierte Balancegrenze; diese Botproben beweisen keinen Spielspaß.
- Wire:57.490 reine Prüfungen und24 Bot-Roundtrips,28 NUnit-Fälle direkt unter.NET,53 Python-Belegtests. Unabhängig1.454 Assertions plus101.405 Klassen-/Geometrievergleiche. Der unabhängig gefundene Invested+1-Fehler wurde korrigiert und unverändert nachgetestet.
- Unabhängiger Lifecycle:125 Szenarien/3763 Assertions, darunter echte Kosten, alle Upgradepfade, ACK vor/nach Snapshot, Ablehnung, Duplikate, Abbruch/Timeout und Pointer-ID-Reuse.
- PlayMode: tatsächliches InputSystem und EventSystem, fünf Klassen auf drei Karten, Bau-/Infoflächenabschirmung, bestätigte Käufe, begrenzte Pointer-Historie, rote Ablehnung, freie Minen und dritte-Mine-Rejection, zwei Upgradepfade, Kamera und PiP.
- Projektion separat ebenfalls bestanden: drei Karten × drei SafeFrames,336 legale Samplepunkte (335 direkt erreichbar,1 durch HUD bedeckt), neun Käufe einschließlich drei Dragminen. Frost-Regression153,80 erhalten. Im Überblick sind tatsächliche Terrainvertices und alle Ausbauhöhen im Bild.
-27 echte Modellrenderings, gleiche Kamera und ruhige Pose; alle Varianten ohne Randabschnitt. Unabhängige Bildprüfung bestanden. Frost Schaden1→2 ist visuell schwächer als andere Ausbauten und bleibt ein Feinschliffpunkt.
-27 tatsächliche Canvasbilder im isolierten640×480Batchcanvas mit künstlichen SafeFrames. Alle neun Porträts sichtbar und korrekt zugeordnet. Diese Bilder sind keine Handy-Lesbarkeitsmessung.
- Native1440×810-Prüfung separat dokumentiert. CUA-Drag erzeugte eine Platzierungsvorschau; dieser Kauf wurde explizit bestätigt. Automatischer Dragkauf ist durch den synthetischen InputSystem-Test belegt, nicht für diese konkrete native Geste behauptet.

Bild-/Quellenbelege: `Docs/Release04VisualEvidence/`, `Docs/MapHudAuditEvidence/overlay-v04/`, `Docs/ResearchEvidence/` und `TestResults/visual-acceptance.json`.

## Tatsächliche Multiplayer-Prüfung

Binary: `Builds/macOS-Candidate/DefenseDuell.app/Contents/MacOS/Defense Duell 0.4.0` (Dateiname enthält tatsächlich ein Leerzeichen vor0.4.0). Die exakten Runner-Befehle stehen in SETUP.md; vollständiger Sammelbeleg: `TestResults/network-v4-acceptance.json`.

| Lauf | Ergebnis |
| --- | --- |
| LAN sauber, Sonnenpass, Port7787 |10/10 Zyklen; mindestens36 identische Snapshots nach allen Aktionen und7 mit beiden gemischten Upgradepfaden je Zyklus. |
| LAN gestört, Frosthafen, Ports7797/7798 |10/10 Zyklen;75±15ms je Richtung, nominal150ms zusätzliche RTT,2% konfigurierte Verluste. Tatsächlich20/834 Datagramme Client→Host und31/1614 Host→Client verworfen. Mindestens35 gemeinsame Snapshots nach allen Aktionen. |
| LAN Vollpartien, Grünes Tal, Port7807 | Zwei reguläre20Hz-Partien à2812 PlayTicks/140,6s, beidseitige Revanche mit neuer Epoch und frischen Ressourcen, Rapid-Buy. Beide Endzustände identisch, Player0 gewinnt1:0HP. |
| Echtes Relay-DTLS, Sonnenpass | Zwei reguläre Partien à2733 PlayTicks/136,65s, tatsächliche Allocation/Join und unterschiedliche anonyme Auth-Fingerprints, Revanche und Rapid-Buy. Identische Endzustände, Player0 gewinnt1:0HP. |

Alle acht Testplayer endeten mit Exit0. Jeder Lauf bestätigt Bau, Range→Damage, tatsächliche Pfadstatistik, Verkauf, freie Mine und zusätzliches Einkommen auf beiden Peers. Rapid-Buy bestätigt pro Peer und Vollrunde drei sofort bezahlte Runnerkäufe plus Ablehnungen bei fehlendem Gold; Duplikate zahlen nicht erneut. Alle zwölf Core-/Content-/Netzquellen und fünf Candidate-Assemblies stimmen vor und nach den Läufen mit dem unveränderten Manifest überein.

Erste Sandboxstarts konnten keine UDP-Sockets öffnen (EPERM); sie sind separat als `*-v4-sandbox-failed` dokumentiert. Die autorisierten erneuten Läufe außerhalb dieser Einschränkung bestanden. Kein Billing wurde aktiviert, keine laufende GUI-Vorschau bedient.

Das sind echte Desktopprozesse auf einem Mac. **Nicht geprüft:** zwei Android-Geräte, zwei getrennte Internetzugänge, physisches WLAN/AP-Isolation, Unterbrechungen und Thermik auf Samsung.

## Sieben Abnahmekriterien

| Kriterium | Status für0.4 |
| --- | --- |
|1. Frischer Import und dokumentierter Build mit gepinnten Versionen | **Bestanden.** |
|2. Zwei Personen bauen, verbessern, verkaufen, senden und beenden eine Partie | **Nicht geprüft mit zwei Personen.** Vollständige Partien und alle Aktionen mit zwei echten Playerprozessen bestanden; ein nativer Solo-Test ist erfolgt. |
|3. WLAN ohne Cloud und Online mit Beitrittscode | **Bestanden auf Desktop.** Echter Relay-DTLS-Test erfolgt; getrennte Internetzugänge und Android bleiben offen. |
|4. APK mit Größe, SHA und Signaturangabe | **Bestanden.**42.777.991 Bytes, SHA oben, APK-v2/Debugsignatur verifiziert. |
|5. Android-Gerätetest und Performance | **Nicht geprüft.** Nutzer hat Handytests auf nach Fertigstellung verschoben.60/30FPS bleiben Ziele. |
|6. Kernprüfungen, Käufe und Neustarts | **Bestanden im geprüften Umfang.** Keine bekannten offenen Fehler, die diese geprüften Abläufe zerstören. Balance und menschliche Bedienungsbewertung bleiben eigene Schritte. |
|7. Asset-/Abhängigkeitsherkunft und Lizenzhinweise | **Bestanden.** KenneyCC0, eigene prozedurale Modelle, dokumentierte generierte Porträts, gepinnte Unity-Pakete mit Lizenzbelegen. |

Dieser Zwischenstand ist eine Testauslieferung. Der anschließend direkt beauftragte einheitliche Artstil, neue Modellporträts, Grauzustände und animierte Infoflächen gehören zu0.5 und sind ausdrücklich noch offen.
