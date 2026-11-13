# Defense Duell – aktueller Projektstand

Stand:13. November2026. Einziger Integrations-/Release-Owner: Hauptchat **„Entwickle Defense Duell für Android“**, Thread `01a1c67c-8801-7cf2-9b86-2b5353ef4b76`, Host `local`. Alle Chats teilen den Ordner; der Nutzer hat Zusammenarbeit und Subagents ausdrücklich beauftragt.

## Abgeschlossener Stand 0.11

App **0.11.0 / Android-Code11**, Core **2.1**, Content-Schema **8**, Wire **7**. Integriert → getestet → gebaut. Eigene Desktop-Netzwerkprozesse gestartet und sauber beendet; noch nicht auf Android installiert/gestartet, nicht durch zwei Menschen abgenommen. Vertrag: [Docs/CONTRACT.md](Docs/CONTRACT.md). Vollständiger Bericht mit sieben DoD-Kriterien: [Docs/Release11TestReport.md](Docs/Release11TestReport.md).

Enthalten: größere scharfe Vollbildkarte, gamifiziertes HUD, unbegrenzte klassenspezifische Upgrades/Forschung und exakte große Zahlen, elf sichtbare Modellmeilensteine und278 Porträts, Material/AO/Kontaktschatten, neue Effekte und App-Icon, Drag-/Abwahlfix und gepufferte Gastbewegung. Eigene Sounds und Gold-/Einkommensfeedback bleiben enthalten. Mine160G/40%Refund, Kampftürme65%, Cooldowns12/20/30/24Ticks und30s Mindestsendefenster; Queue-Gold und ehrliche Zeituntergrenzen sichtbar.

- **149/149 EditMode,141/141 PlayMode,6/6 finaler HUD-Nachlauf bestanden.**102/102 reine Corefälle;360 Kandidat-Bestätigungsreplays und vergleichbare Baseline.50 Comparator- und9 ACK-Race-Prüfungen bestanden.
- **10 echte LAN-Verbindungs-/Aktions-/Shutdownzyklen bestanden**,4558 gemeinsame Snapshots/620 Hostbefehle. Zusätzlich zwei vollständige gestörte LAN-Partien mit Revanche (150ms nominale Zusatz-RTT,±15ms Jitter,2% Verlust) sowie zwei echte Relay/DTLS-Partien mit Revanche bestanden.
- Testinfrastrukturkorrekturen offen dokumentiert: legitime automatische Kaufablehnung bei veraltetem Gold und Auswertung fertiger Tempomessfenster neben unfertigen Folgemessungen. Pflichtaktionen und Messgrenzen bleiben strikt. Ursprüngliche Fehlerbelege erhalten.
-278 Porträts verifiziert; neue Economy-HUD-Sichtung7/7 plus Einzahl-Nachtrag. Frischimport ohne Library bestanden: `Builds/FreshImport-20261113-125029`.
- Finalbindung: `TestResults/network-v7-economy-v11-source-manifest.json`; `python3 Tools/check_project_state.py --check-frozen --manifest TestResults/network-v7-economy-v11-source-manifest.json`. Vollsuitenbasis und drei spätere Änderungen separat gebunden; nicht als unveränderte finale Vollsuite ausgeben.

## Artefakte

APK: `Builds/Android/DefenseDuell-0.11.apk`,**70.616.730Bytes/67,35MiB**, SHA256 `257e72a4238bc36411327b8c4f3d704e407f45b7c180406ca8b42db1b7206966`. ARM64/IL2CPP/GLES3, Development mit Android-Debug-Testsignatur/APK-v2 verifiziert.26 verpackte Iconressourcen geprüft.18.562.493Bytes ungenutzte inkrementelle ZIP-Lücken entfernt, keine Assets reduziert.

Mac: `Builds/macOS-EconomyFinalReview/DefenseDuell.app`, Bundle `de.defenseduell.preview.economy.final`. Erste0.11-Macfassung unter `macOS-EconomyReview` bleibt für ihre zehn LAN-Zyklen erhalten. Quellenpaket: `Artifacts/DefenseDuell-0.11-Source.zip`; Größe/Hash/CRC-Nachweis separat in `TestResults/source-artifacts-v11.json` nach Erzeugung.

Geschützt bleiben0.10APK und sämtliche0.9/0.8/0.7/älteren Vorschauen, insbesondere Nutzer-AVD `DefenseDuellReview`. Nicht überschreiben, schließen, fernbedienen oder ungefragt neu installieren.

## Aktives Integrationsfenster 0.12

Direkte neue Nutzerwünsche in Projektchats: Studiointro aus `soballa_studio_intro.webp` (105Frames/720×405/25fps/4,2s/stumm; MP4-Konvertierung) und spielerisches, aufgeräumtes animiertes Startmenü. Noch kein0.12-APK und noch kein Intro/Neumenü in0.11 behaupten.

0.11-Checkpoint **e6fc24b** ist gesichert. Die Entwürfe lagen getrennt unter: Assetchat `Tools/StudioIntroLab/**`, HUDchat `Tools/MenuPlayfulDraft/**`. Diese aktiven Entwürfe sind aus0.11-Quellenarchiv/Checkpoint ausgeschlossen. Geplantes gemeinsames0.12-Fenster: neue Introassembly/Controller/EditorSetup/Tests beim Assetowner; GameUI.cs nurHome/Chrome/Rebuild/Dialogtheme, GameUI.Progress.cs HomeExtras, GameUI.Overlay.cs HomeTheme, neueHome/MenuMotion/MenuPresentationTests sowie genauHomeDifficultyAndUnlockRolesAreVisible beim HUDowner. Root besitzt BuildEntry, Versionen, Szenenintegration, GameBootstrap, Unity, Tests, Builds, Git und PROJECT_STATE. Runtimefreigabe jetzt erteilt für den oben genannten Umfang. Assetowner nur neue Intro-Runtime/Assembly/EditorSetup/Tests/Media, keine bestehenden Dateien; Root integriert Editorreferenz/Szenenliste und generiert die Szene. HUDowner nur genannte Home-Dateien/neue Menütests/einen Home-Legacytest. Keine konkurrierenden Runtimeedits, nur ein Unity-Editorprozess.

## Grenzen

ADB listet derzeit kein Gerät. Handygesten, Launcher, Lautsprecher und15-Minuten-Frametimes/Speicher/Thermik bleiben nicht geprüft. Zwei menschliche Spieler/getrennte Internetzugänge und subjektive Balance bleiben offen. Nutzerbestätigung einer früheren Installation ist kein0.11-Gerätenachweis. Keine bezahlten Assets/Billing/Storeveröffentlichung. Vier-Spieler-Modus nur separate Machbarkeitsanalyse. Keine automatische Dauerüberwachung.

Historie: [Docs/Iteration11IntegrationHistory.md](Docs/Iteration11IntegrationHistory.md), [Docs/Iteration10IntegrationHistory.md](Docs/Iteration10IntegrationHistory.md) und ältere versionsgebundene Berichte.

0.12-Zwischenprüfung: Menü9/9, Intro11/11 native gezielte Tests bestanden. Visuelle Nachprüfung fand abgeschnittene Solo-CTA bei900, fehlende CanvasRenderer für Kartenpiktogramme sowie falsche Video-Farbmatrix. Gezielte Korrekturfenster: HUDowner GameUI.Home.cs/MapThumbnailGraphic.cs/MenuPresentationTests.cs; Assetowner ausschließlich explizite BT.709-Medienkonvertierung und Nachweis. Keine konkurrierenden Unity-Läufe. Endabnahme und0.12-Build stehen noch aus. Kasernenmodus ist zusätzlicher Konzeptauftrag im Designchat, keine Implementierung in0.12.
