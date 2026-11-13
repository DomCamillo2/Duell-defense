# Defense Duell – aktueller Projektstand

Stand: 13. November 2026. Einziger Integrations-/Release-Owner ist der Hauptchat **„Entwickle Defense Duell für Android“**, Thread `01a1c67c-8801-7cf2-9b86-2b5353ef4b76`, Host `local`. Zusammenarbeit und Subagents sind vom Nutzer ausdrücklich beauftragt. Alle Projektchats teilen den Arbeitsordner.

## Abgeschlossener Stand 0.13

App **0.13.0 / Android-Code 13**, Core **2.1**, Content-Schema **8**, Wire **7**. **Integriert → getestet → gebaut.** Eigene Desktop-LAN-/Relayprozesse gestartet und beendet. **Nicht auf Android installiert/gestartet und nicht mit zwei Menschen abgenommen.** Vertrag: [Docs/CONTRACT.md](Docs/CONTRACT.md); vollständige Nachweise und sieben DoD-Kriterien: [Docs/Release13TestReport.md](Docs/Release13TestReport.md).

Neu: durchgehender dunkler Home-/Connecthintergrund außerhalb der SafeArea, sichere Bedienelemente innerhalb; neue WLAN-/Onlinekarten, kontrastreiche Formulare und Hilfedialog mit Port; korrigierte Titelhöhe und Lautstärkegriffe. Vorhandene Host-/Join-/Validierungswege bleiben verbunden.

Weiter enthalten: unbegrenzte klassenspezifische Upgrades/Forschung mit exakten Zahlen, große native Vollbildkarte, elf Modellmeilensteine und278Porträts, Material/AO/Kontaktschatten, HUD/App-Icon, Sounds/Musik, Gold-/Einkommensfeedback, Drag-/Abwahlfix, gepufferte Gastbewegung, Studiointro und animiertes Home. Regeln/Wirtschaft aus0.11 unverändert.

## Abnahme

- **149/149 EditMode und161/161 PlayMode auf dem finalen Quellstand bestanden.** Vorherige8/9gezielte Prüfung und160/161ersteVollsuite bewahren die gefundenen Titel-/Sliderfehler. Anschließende3/3Connect- und1/1Insetprüfungen plus finaleVollsuiten bestätigen die Korrekturen.
- Sieben Connectbilder sowie Home-/Arsenal-/Insetbilder einzeln gesichtet. Links/rechts83Pixel bei2048×955 synthetisch nativ gerendert; gesamte ausgeschlossene Streifen Navy. Keine physische Geräte-SafeArea oder Top-/Bottom-Insetabnahme behauptet. Settings nach Containerfix erneut gesichtet.
- **Aktuelle echte LAN-/Relay-Start-/Aktions-/Shutdownzyklen bestanden:**453/440gemeinsameSnapshots,62/60Hostbefehle, Exit0/0. Mehrfachkäufe und gemeinsameTempowahl enthalten; keine vollständigen0.13Partien.
-31 relevante Core-/Content-/Netz-/Plattform-/Progressions-/Boardquellen gegenüber0.12Checkpoint unverändert. Core-DLL bytegleich. Frühere vollständige0.11Partien, gestörtesLAN und zehnLANzyklen bleiben separat historische Belege.
- Frischimport ohne Library `Builds/FreshImport-20261113-141706`: Prepare und278Porträts bestanden.
- Finale Bindung: `TestResults/network-v7-connect-v13-source-manifest.json`; Prüfung `python3 Tools/check_project_state.py --check-frozen --manifest TestResults/network-v7-connect-v13-source-manifest.json`.

## Artefakte

APK: `Builds/Android/DefenseDuell-0.13.apk`, **71.107.452Bytes /67,81MiB**, SHA256 `10175935f6b7d04def58c5cbcc1e39b369b0b87d62dd9411e0fee3a309f745e3`. ARM64/IL2CPP/GLES3, Development mit Android-Debug-Testsignatur/APK-v2 verifiziert.26Icons und finaleIntro-MP4 im APK geprüft.

Mac: `Builds/macOS-ConnectionReview/DefenseDuell.app`, Bundle `de.defenseduell.preview.connection`. Quellenpaket: `Artifacts/DefenseDuell-0.13-Source.zip`; separater Integritätsbeleg `TestResults/source-artifacts-v13.json` nach Erstellung.

Geschützt bleiben0.12Checkpoint **12d535b**, APK/Quellenpaket/StudioReview;0.11Checkpoint **e6fc24b** und frühere APKs/Macvorschauen sowie Nutzer-AVD `DefenseDuellReview`. Nicht überschreiben, schließen, fernbedienen oder ungefragt installieren.

## Offenes Integrationsfenster 0.14

**Runtimefreigabe ausschließlich nach folgender Dateizuteilung.** Basis `cdbe31d937c39682033d3506585dd64249073ad0`. Vertrag: `Docs/Iteration14Contract.md`. Ziel 0.14 / Core2.2 / Wire8. Root besitzt Unity, Builds, Versionierung, Checkpoints und PROJECT_STATE. Alle Runtimeübergaben sind angenommen und eingefroren. Finale EditMode-Suite181/181 bestanden; finalePlayMode-Suite läuft. Noch kein0.14-Build.

- Economychat: `Assets/Core/**` für Hostregeln/effective Content/Start/Ready/Revisionen, neue `Assets/Tests/EditMode/Core/HostRules14Tests.cs` (+meta), `Tools/HostRulesLab/**`. Keine anderen bestehenden Tests ohne Übergabe. API vor Netz-/HUD-Anbindung dokumentieren.
- HUDchat: `Assets/Presentation/GameUI.cs` nur Lobbyrouting/Refresh, neue `GameUI.Lobby.cs`, `LobbySettingsEditor.cs`, `HostLobbyRulesPanel.cs`, `ExactGoldInputField.cs` (+meta), neue HostLobby14-Tests unter PlayMode/Presentation; `Tools/HostSettingsDraft/**`.
- Assetchat: `Assets/Presentation/BoardView.cs`, `ContactFxBudget.cs`, `ContactFxBatch.cs`, neue `Assets/Tests/PlayMode/Presentation/BombRadiusPresentationTests.cs` (+meta); `Tools/BombRadiusLab/**`.
- Root/progression_core: bestehende `Assets/Tests/EditMode/Core/**` (außer neue HostRules14Tests) und `Tools/CoreHarness/**` für semantisch notwendige Fixturemigration.
- Root/drag_fix: neue Wire8-Pythonreader/-Comparator/-Tests und LAN-/Relayrunner unter `Tools/NetworkTests`; alte Wire7-Beweise bleiben unverändert.
- Root/network_smooth: `Assets/Networking/**`, neue Netzwerkregeltests; API-Abgleich mit Core/HUD erforderlich.
- Mapschat: neue `Assets/Editor/IosBuildPreparation.cs` (+meta), `Tools/iOS/**`, `Docs/iOSPreparation.md`. Keine BuildEntry-/ProjectSettings-/AppIconSetup-Edits.
- Designchat: Team-/größere Kartenentwürfe bleiben ausschließlich `Tools/TeamSiegeLab/**`, `Docs/ResearchEvidence/team-siege-v1/**`. Keine Team-Runtimefreigabe in diesem Fenster.

Übergaben abgeschlossen: Core, bestehende Fixtures, Netzwerk/Wire8, Pythonvergleich, Lobby, Radiusdarstellung und iOS-Editorhelper gehören jetzt Root. Neue direkte DLL-Nachweise22/22 und102/102 bestanden; ursprünglicher falsch gebundener102-Lauf wurde ausdrücklich invalidiert und erhalten. Native gezielte Lobby-/Radiussuite10/10, finaleEditMode181/181 bestanden. Alte Snapshot-Reflectionfixture auf tatsächlichen Empfangspfad migriert; PlayMode-Gesamtabnahme läuft. Keine konkurrierenden Runtimeänderungen.

Keine parallelen Unityprozesse; alle Eigentümer frieren vor Root-Import/Test/Build ein. Bestehende Zuteilungen laufen regulär bis Übergabe. Neue Implementierung erfolgt anschließend standardmäßig über gezielte Root-Subagents; offene Wünsche gehen an Root, bisherige Chats bleiben erhalten. Nur relevante Übergaben/Blocker/API-Änderungen melden; keine Broadcast-Schleifen. Siehe AGENTS.md.

Neue direkte Nutzeraufträge werden vorbereitet, sind aber keine0.13-Funktionen:

- Designchat `01a1c6b8-3c29-7cb3-9635-5eb91428c7a1`: ausschließlich `Tools/TeamSiegeLab/**` und `Docs/ResearchEvidence/team-siege-v1/**`. Freundesmodus4→2v2,6→3v3/2v2v2, ausdrücklich ohneBots/BotFill; Team-/Reconnect-/Laser-/Reichweitenmechanik.21/21isoliertePython-Modelltests gemeldet, keineC#/Unity-/Netzabnahme. BestehenderSolo-Bot bleibt unverändert.
- HUDchat `01a1c6c8-89f2-7ac3-9a13-fac8362270ca`: Entwurfsbasis `Tools/HostSettingsDraft/**`. Host/Admin konfiguriert vorStart Wellenzahl undStartgold proSpieler; readonlyGastwerte, Readyreset/Startlock undFormvertrag.
- Economychat `01a1c6bc-f70f-75c2-a78e-30f542f60d63`: Entwurfsbasis `Tools/HostRulesLab/**`, reinerC#-Regel-/Validierungsprototyp. API-Abgleich mitHUD/Teamdesign; exakteGoldwerte, Settings-/Rosterrevision, Content-/Wireänderungen undWellenfortsetzung müssen konkretgeprüft werden.

Diese aktiven Entwürfe sind aus0.13Quellenpaket/Checkpoint ausgeschlossen. Keine zusätzlichen Economyänderungen stillschweigend übernehmen. Noch keine sechs Spieler, frei konfigurierbaren Hostregeln oder Kasernen in der APK behaupten.

## Grenzen

ADB listet kein Gerät. Handygesten, Displayaussparungen, Launcher, Lautsprecher, Hardwarevideo und15-Minuten-Frametimes/Speicher/Thermik bleiben ungeprüft. Zwei menschliche Spieler, getrennteInternetanschlüsse und subjektiveBalance offen. Keine bezahltenAssets/Billing/Storeveröffentlichung und keine automatische Dauerüberwachung.

Historie: [Docs/Iteration13IntegrationHistory.md](Docs/Iteration13IntegrationHistory.md), [Docs/Release12TestReport.md](Docs/Release12TestReport.md) und ältere versionsgebundene Berichte.
