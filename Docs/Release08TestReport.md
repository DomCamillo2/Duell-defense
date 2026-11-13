# Testbericht Defense Duell 0.8

Stand: 11. November 2026. App **0.8.0 / Android-Code 8**, Core v1.9, Content-Schema 6, Wire 6. Die kompaktere Oberfläche ist integriert, in Unity gestartet, geprüft und für Mac/Android gebaut. Regeln, Preise, Modelle und Netzwerkprotokoll bleiben unverändert. Frühere Ergebnisse stehen im [0.7-Bericht](Release07TestReport.md).

## Änderungen und visuelle Abnahme

Status, Wellenphase und Tempo stehen oben; Wellen-/Einkommensdetails öffnen sich auf Abruf. Die leere Angriffsqueue bleibt verborgen, bezahlte Pakete haben eine kompakte Zusammenfassung und eine vollständig scrollbare Liste. Nur freigeschaltete Bauklassen erscheinen im Match. Jede Angriffskarte behält ihren Preis und ihre tatsächliche Zielwelle/Forschungsstufe; das eigene Infoziel bleibt auch bei fehlendem Gold bedienbar. Die Übersicht reserviert Platz für Seitenleiste und Gebäudeaktionen, damit die Basis sichtbar bleibt. Menü, Scrollgriff und der Kontrast der Angriffsüberschrift sind korrigiert.

Gamifizierte Upgrade-/Verkaufsbuttons und der Tempo-Button bleiben erhalten. 2× verlangt im Multiplayer beide Stimmen; jeder kann zurücknehmen. Die Baupause bleibt unabhängig davon in Echtzeit.

Finale Bilder: `Docs/Release08VisualEvidence/`. Unabhängige Bildreviews: `Docs/FinalUXEvidence/compact-v08/view-chat-independent-review-final.json` und `final/visual-review.json`. Das sind Unity-Testbilder mit kontrollierten Geld-/Freischaltungswerten, keine Androidaufnahmen und kein geändertes Startgold.

## Ausgeführte Prüfungen

- **71/71 PlayMode bestanden**, `TestResults/playmode-v08.xml`, Quellenbindung `compact-hud-v08-source-manifest.json`. Frühere fehlgeschlagene Läufe bleiben als `-first`/`-second` erhalten. Angepasste alte Layout-/Menü-Fixtures behalten Kauf-, Drag-, Projektions- und Modalassertionen.
- Danach ausschließlich dunklere Farbe/entfernter Schatten für „ANGRIFF“: **19/19 gezielte HUD-Tests bestanden**, `playmode-v08-hud-final.xml`, finale 114 Quellen in `compact-hud-v08-final-source-manifest.json`. Nicht als identischer Vollsuite-Quellstand ausgegeben.
- Basis-Raycasts in900/960/1280 bei Vorbereitung/aktiver Welle/Aufräumen; korrekte getrennte Kaufangebote, fünf/sieben Klassen, 44er-Infoziele, Modalsperren, 80 echte FIFO-Aufträge und Scrollgrenzen geprüft. Touch-/Drag-Regressionen sind Unity-Input-System-Tests, kein physischer Android-Gestentest.
- **Reales LAN mit zwei neuen 0.8-Playerprozessen bestanden:** 361 gemeinsame Snapshots, 50 Hostbefehle, beide Exit0. Ein Aktions-/Wellen-/Forschungszyklus mit schnellen Mehrfachkäufen und beidseitiger Tempoabstimmung; keine neue Vollpartie behauptet. Belege: `TestResults/lan-compact-v08/`, `network-v6-compact-v08-acceptance.json`.
- 27 relevante Core/Networking/Content/Platform/Progressionquellen unverändert gegenüber 0.7. Core-, Networking-, Content- und Progression-DLLs bytegleich; Presentation und Platform neu kompiliert. Historische 123 EditMode-, Core-/Bot-/Wire-, echte Relay-Vollpartie-/Revanche- und Störtests siehe 0.7-Bericht; für 0.8 nicht pauschal neu ausgeführt.
- Finale 113 Runtimequellen und 303 Dateien des Mac-Builds nach LAN unverändert: `network-v6-compact-v08-source-manifest.json`.
- **Frischimport ohne Library bestanden**, `Builds/FreshImport-20261111-113144`, separate Prüfung aller 43 Modellporträts ebenfalls PASS. Beleg `TestResults/fresh-import-v08.json`. Contenthash unverändert 429ab672fc464511fbc3ff3f99d090bea03be6ab0db5e753b98be3cb36c53a6b.

## Artefakte

- Android: `Builds/Android/DefenseDuell-0.8.apk`, **45,351,283 Bytes / 43.25 MiB**.
- SHA-256: `f81eaf57d634328dc3537297b7c3cf2a8f1051b04541ecc77b3a936ac4ec0ae6`.
- Paket `de.defenseduell.game`, ARM64/IL2CPP/GLES3, min API25 / target API35, APK-v2-Signatur geprüft. **Development mit Android-Debug-Testsignatur**, keine Store-Veröffentlichung. Metadaten `TestResults/build-artifacts-v08.json`.
- Separate Mac-Vorschau: `Builds/macOS-HudReview/DefenseDuell.app`, Bundle `de.defenseduell.preview.hud`, Fenstername `Defense Duell 0.8.0`. FürLAN als eigeneHeadlessprozesse gestartet/beendet; kein interaktiver nativer 0.8-Spieltest behauptet. Ältere Nutzervorschauen und 0.7-APK/Archiv erhalten.
- Quellenarchiv: `Artifacts/DefenseDuell-0.8-Source.zip`; Dateiliste/CRC und gebundene Quellen beim Packen geprüft. Größe/Hash stehen in `TestResults/source-artifacts-v08.json` und der ZIP-SHA256-Datei.

## Reproduktion

```sh
# Vollständige Suite; Ausgabe mit -testResults/-logFile versionsgebunden ablegen.
bash Tools/unity.sh play-tests
# Finaler visueller Nachlauf wurde mit -testFilter 'CompactHud08Tests;WaveHud07Tests' ausgeführt.
bash Tools/unity.sh hud-review
bash Tools/unity.sh android
python3 Tools/verify_android.py Builds/Android/DefenseDuell-0.8.apk
python3 Tools/verify_fresh_import.py
python3 Tools/check_project_state.py --check-frozen
```

## Definition of Done

| Kriterium | Status | Nachweis / Grenze |
| --- | --- | --- |
| 1. Frischer Import und Build | Bestanden | Unity 6000.3.28f1, Mac/APK, Frischimport und 43 Porträts |
| 2. Vollständige Partie mit zwei Personen | Nicht geprüft | Zwei echte Prozesse geprüft; zwei menschliche Spieler nicht abgenommen |
| 3. WLAN und echter Relay | Bestanden | LAN 0.8; echte Relay-Vollpartien/Revanche 0.7 mit unverändertem Core/Net. Keine getrennten Internetanschlüsse behauptet |
| 4. APK mit Größe, Hash und Signatur | Bestanden | Neue 0.8-APK, Metadaten oben |
| 5. Androidgerät und Performance | Nicht geprüft | 0.8 nicht auf Android gestartet; 0.7 nur begrenzt im Emulator. Kein physisches Samsung-, 15-Minuten- oder Mehrfinger-PASS |
| 6. Kern- und Integrationsprüfungen | Bestanden | 71+19, neuer LAN-Lauf, unveränderte Kernnachweise; keine bekannten blockierenden Kauf-/Partiefehler |
| 7. Herkunft und Lizenzen | Bestanden | ASSET_LICENSES.csv / THIRD_PARTY_NOTICES.md; keine neuen Assets oder Abhängigkeiten |

Offen bleiben die Android-0.8-Installation, direkte Drag-/Pan-/Pinch-Gesten, physische Leistung/Thermik, zwei Menschen über getrennte Internetanschlüsse und menschliche Balance. Automatische Botpartien beweisen keinen Spielspaß. Kein Billing aktiviert, keine bezahlten Assets und keine Veröffentlichung.
