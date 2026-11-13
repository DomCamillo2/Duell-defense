# Testbericht

Stand: 10. November 2026. **Geprüfter Desktop-Zwischenstand 0.3**, Core v1.6, Protokoll 3, Content-Schema 4. Die laufende Nutzer-Vorschau liegt unter `Builds/macOS-Next/DefenseDuell.app`. Die gemeinsame Quelle enthält danach zusätzlich Renderer v4 und den geprüften Vertex-Kamerafit. Die neu beauftragte Folgeversion 0.4 ist noch nicht Bestandteil dieser Nachweise.

## Ausgeführte Prüfungen

| Prüfung | Ergebnis | Beleg |
| --- | --- | --- |
| Unity Import/Content | Prepare erfolgreich; drei freie Karten, Schema 4 | TestResults/prepare.log; Assets/Content/Standard.asset |
| Unity EditMode | **76/76 bestanden**, keine übersprungenen Tests | TestResults/editmode-v03.xml |
| Unity PlayMode, abschließend mit Renderer v4/Vertexfit | **26/26 bestanden**, 51,208 s, Ende 19:46:21 UTC | TestResults/playmode-v03.xml |
| Freie Platzierung/UI | 318 Projektions-/Raycastpunkte, drei echte Tapkäufe/Entity-Auswahl; 3 Karten × 3 angewandte SafeFrames; Terrain-/Turmvolumen vollständig in OwnBoard und PiP | Docs/MapHudAuditEvidence/vertex-fit-v4 |
| Core/Geometrie | 49 Szenarien, 72 vollständige seitengetauschte Partien; unabhängige 194 Assertions + 17.978 exakte Geometrieproben | Docs/Gameplay.md; Docs/FreePlacementDesign.md |
| Wire/Belegvergleich | 56.733 Wirechecks, 24 vollständige Bot-Partien; 24 NUnit-Methoden und 39 Python-Prüfungen | Docs/Networking.md |
| Native Vorschau | Erfolgreicher MacNext-Build, neues Menü und laufende Solopartie im neuen HUD tatsächlich gesehen; Nutzer übernahm anschließend die Steuerung | TestResults/next-build.log |
| LAN sauber | **10/10** echte Host/Join/Abbruch-/Neustartzyklen bestanden | TestResults/lan-clean-v3/comparison.json |
| LAN gestört | **10/10** echte Zyklen bestanden, mindestens 17 gleiche Snapshots nach den Aktionen je Zyklus | TestResults/lan-impaired-v3/comparison.json und proxy.json |
| Vollständige LAN-Partien | **Zwei normale Partien plus beidseitige Revanche bestanden**, je 135,6 s, gleiche finale Zustände, HP 1:0 | TestResults/lan-complete-v3/comparison.json |
| Echte Relay-Partien | **Zwei normale DTLS-Partien plus Revanche bestanden**, je 131,65 s, gleiche finale Zustände, HP 1:0 | TestResults/relay-complete-v3/comparison.json |

Die vollständigen LAN-/Relay-Szenarien prüfen freie Koordinaten, identisches Request-Replay, Abweisung derselben ID mit veränderten Koordinaten, einmalige Goldabbuchung, Bauen/Upgrade/Verkauf, Mineneinkommen und sofortige Mehrfachkäufe. In beiden Runden und auf beiden Peers wurden jeweils drei Läufer im selben Frame bezahlt gekauft, weitere Käufe nur wegen fehlenden Goldes abgewiesen, bezahlte FIFO und Gold anschließend autoritativ bestätigt. Produktionstakt 20 Hz und Acht-Minuten-Regeln bleiben unverändert. Die Testspieler senden kontinuierlich und bauen nach dem Kaufszenario keine weitere Kampfverteidigung; diese zweiminütigen Szenarien sind kein allgemeiner Balancebeleg.

Der Störproxy verwendete 75 ±15 ms je Richtung (nominal 150 ms zusätzliche RTT), 2% konfigurierte Verlustrate und festen Seed. Tatsächlich verworfen: 7/556 Datagramme Client→Host, 24/1003 Host→Client. Reale Weiterleitung, keine bloß behauptete Störung.

Relay nutzt zwei getrennte anonyme Profile und echte Cloud-Allocation/Join mit DTLS. Beide Prozesse liefen auf demselben Mac und Internetanschluss. **Keine zwei Android-Geräte oder getrennten Internetzugänge geprüft.** Der eingefrorene getestete Core-/Content-/Netzquellstand steht in `TestResults/network-v3-source-manifest.json`.

## Bedienung und bekannte Grenzen

Die PlayMode-Prüfungen umfassen echten InputSystem-Mausdrag mit Pointer-Ereignissen, oberste HUD-Trefferfläche, PiP-/Zweitfingerabschirmung, sofortiges Pending-Cleanup, ACK/Snapshot-Reihenfolge und Verkauf/Neubau im selben Render-Slot ohne alte Animationen. Der Frosthafen-Regressionspunkt (153,80) wird korrekt frei gebaut. Die geprüften SafeFrames sind 1280×720, 1600×720 und 1440×680; frühere fälschlich als angewandt bezeichnete Formatläufe wurden im HUD-Bericht ausdrücklich korrigiert.

Die organischen Karten und Kamerafit v4 wurden in tatsächlichen Unity-RenderTextures geprüft. Diese Source-Korrektur ist nach der laufenden v3-Nutzer-Vorschau erfolgt und wird nicht rückwirkend als Bestandteil deren Binary bezeichnet.

Der 96-Partien-Botvergleich zeigte eine starke Economy-Strategie und 24 Zeitlimit-Remis gegen PureDefense. Die Seitenvertauschung war reproduzierbar. Menschliche Balancefreigabe bleibt offen. Reine Desktop-Simulation unter Maxlast ist kein Framerate-/Thermiknachweis auf Android.

## APK und Hardware

**Die vorhandene APK ist weiterhin die gesicherte Version 0.2.0**, nicht dieser 0.3-/0.4-Quellstand. Dateigröße 57.961.833 Byte, SHA256 `fedbd42369149b586ad09daa71daaae942fbaec410fc12c270156c610490a835`, ARM64/IL2CPP/GLES3, Min-API25/Ziel35, Development mit verifizierter Android-Debug-/APK-v2-Testsignatur. Baselinebeleg: TestResults/build-artifacts-v02.json; vollständiger damaliger Bericht Docs/Release02TestReport.md.

Der Nutzer möchte zunächst alles bauen und erst danach das Handy verwenden. Kein Installations-, Android-Start-, Geräteunterbrechungs- oder 15-Minuten-Lasttest wird behauptet.

## Definition of Done für den aktuellen Arbeitsstand

| Kriterium | Status | Praktische Grenze |
| --- | --- | --- |
| 1 Frischer Import und dokumentierter Build | Nicht geprüft für Folgeversion | Letzter frischer Import gehört 0.2; neuer Nachweis folgt für finalen Stand |
| 2 Bauen/Upgrade/Verkauf/Senden/volle Partie | Bestanden auf Desktop für 0.3 | Zwei echte Prozesse, keine Zwei-Personen-/Handyabnahme |
| 3 WLAN und Onlinecode | Bestanden auf Desktop für 0.3 | Echter Relay; getrennte Internetanschlüsse nicht geprüft |
| 4 Aktuelle APK mit Hash/Signatur | Nicht geprüft für Folgeversion | Vorhandene APK ist ausdrücklich 0.2 |
| 5 Android/Performance | Nicht geprüft | Auf Nutzerwunsch später |
| 6 Kernprüfungen/Neustarts | Bestanden für 0.3 | 76 EditMode, 26 PlayMode, 20 LAN-Zyklen, echte Revanchen; neue Features brauchen neue Tests |
| 7 Herkunft/Lizenzen | Bestanden für importierten Stand | Kenney CC0 + eigene Geometrie; neue generierte Icons noch im Kandidatenordner |
