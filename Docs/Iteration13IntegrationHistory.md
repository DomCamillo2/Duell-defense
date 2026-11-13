# Defense Duell – aktueller Projektstand

Stand:13. November2026. Einziger Integrations-/Release-Owner: Hauptchat **„Entwickle Defense Duell für Android“**, Thread `01a1c67c-8801-7cf2-9b86-2b5353ef4b76`, Host `local`. Alle Chats teilen den Ordner; Zusammenarbeit und Subagents sind vom Nutzer ausdrücklich beauftragt.

## Abgeschlossener Stand 0.12

App **0.12.0 / Android-Code12**, Core **2.1**, Content-Schema **8**, Wire **7**. **Integriert → getestet → gebaut.** Eigene Desktop-LAN-/Relayprozesse gestartet und sauber beendet. **Nicht auf Android installiert/gestartet, nicht durch zwei Menschen abgenommen.** Vertrag: [Docs/CONTRACT.md](Docs/CONTRACT.md); alle sieben DoD-Kriterien und Beweisgrenzen: [Docs/Release12TestReport.md](Docs/Release12TestReport.md).

Enthalten: größere scharfe Vollbildkarte, gamifiziertes HUD, unbegrenzte klassenspezifische Upgrades/Forschung und exakte große Zahlen, elf sichtbare Modellmeilensteine und278Porträts, Material/AO/Kontaktschatten, Effekte/App-Icon, Drag-/Abwahlfix und gepufferte Gastbewegung, eigene Sounds/Musik und Gold-/Einkommensfeedback. Mine160G/40%Refund, Kampftürme65%, Cooldowns12/20/30/24Ticks,30s Mindestsendefenster und Queue-Gold/Zeituntergrenzen aus0.11 bleiben.

Neu: bereitgestelltes Soballa-Studio-Intro, robustes asynchrones Boot/Skip nach1,5s, korrekte native Farben; spielerisches Home mit Diorama, festen Touchflächen, Solo/Schwierigkeit, WLAN/Online,3Karten und Arsenal. Home-Diorama bis1280nativePixel, Energiesparen/Minimap640; große Spielkarte unverändert bis2560. Gemessen1174×381statt640×208bei1920×1080. Keine höhere Auflösung der720×405-Introquelle behauptet.

## Nachweise

- **149/149 EditMode,157/157 PlayMode,14/14 finaler Schärfe-/Render-/Lebensdauernachlauf bestanden.** Vollsuitenbasis und fünf spätere Quellenänderungen getrennt in `studio-v12-source-manifest.json`/`studio-v12-final-deltas.json` gebunden.278Porträts neu gebunden, vollständig pixelidentisch zur0.11.
- Intro11nativeFälle inklVideo/Pause/Skip/Fehler/Deaktivierung sowie Menü5Fälle inVollsuite enthalten.3finaleMenübilder visuell bestanden; Textclipping/fehlendeRoutenbilder/Farbmatrix korrigiert, alteBelege erhalten.
- **Neue echte0.12LAN-/Relay-Start-/Aktions-/Shutdownzyklen bestanden:**465/442gemeinsameSnapshots,62/60Hostbefehle, Exit0/0, Mehrfachkäufe und beidseitigeTempowahl. Keine vollständigen0.12Partien behauptet.
- Historische0.11Abnahme bleibt separat:102reineCorefälle,360Bestätigungsreplays,10LANzyklen sowie vollständige gestörteLAN-/Relaypartien mitRevanche. Quellreuse-Audit explizit; CoreDLLbytegleich, kein pauschaleridentischerPlayer behauptet.
- Frischimport ohneLibrary `Builds/FreshImport-20261113-134342`:Prepare und278Porträts bestanden.
- FinaleBindung: `TestResults/network-v7-studio-v12-source-manifest.json`; Prüfung `python3 Tools/check_project_state.py --check-frozen --manifest TestResults/network-v7-studio-v12-source-manifest.json`.

## Artefakte

APK: `Builds/Android/DefenseDuell-0.12.apk`, **71.097.720Bytes/67,80MiB**, SHA256 `c416db08a002fdfd0c14a70c0ad09c840bb8bc9d32c04b441a7c5d4abb925c22`. ARM64/IL2CPP/GLES3, Development mit Android-Debug-Testsignatur/APK-v2 verifiziert.26Icons bytegleich zur0.11; finaleMP4exakt imAPKnachgewiesen.

Mac: `Builds/macOS-StudioReview/DefenseDuell.app`, Bundle `de.defenseduell.preview.studio`. Quellenpaket: `Artifacts/DefenseDuell-0.12-Source.zip`; separater Integritätsbeleg `TestResults/source-artifacts-v12.json` nachErstellung.0.11Checkpoint **e6fc24b**, APK/Quellenpaket und beideEconomy-Macfassungen bleiben erhalten. Ebenso0.10/0.9/ältere Vorschauen und Nutzer-AVD `DefenseDuellReview`: nicht überschreiben, schließen oder fernbedienen.

## Koordination und Grenzen

Kein offenes Runtime-Integrationsfenster. Intro-/Homeowner eingefroren; Root besitzt Unity, Builds, Versionierung, Checkpoints undPROJECT_STATE. Neue Änderungen erst mit konkreter Dateizuteilung, keine parallelen Unityprozesse.

ADB listet kein Gerät. Handygesten, Launcher, Lautsprecher, Hardwarevideo und15-Minuten-Frametimes/Speicher/Thermik bleiben nicht geprüft. Zwei menschliche Spieler/getrennteInternetanschlüsse und subjektiveBalance offen. Frühere Nutzerinstallation ist kein0.12Gerätenachweis. Keine bezahltenAssets/Billing/Storeveröffentlichung. Vier-Spieler-Modus und Kasernen-Duell sind ausschließlich separate Konzepte, keineAPKfunktionen. Keine automatische Dauerüberwachung.

Historie: [Docs/Iteration12IntegrationHistory.md](Docs/Iteration12IntegrationHistory.md), [Docs/Release11TestReport.md](Docs/Release11TestReport.md) und ältere versionsgebundene Berichte.

## Neues gezieltes Integrationsfenster 0.13

Direkter Nutzerbug im Economychat nach Auslieferung: heller linker Rand im Home auf2048×955Screenshot. Rootbackground bleibt hellgrün außerhalbSafeArea, Homebackdrop ist nurinnerhalbSafeArea navy. HUDowner erhält ausschließlich GameUI.cs/Home-Background und MenuPresentationTests.cs für asymmetrischeSafeInset-/Rotation-/Modalregression; Root besitzt Versionscode13, APK, Tests, Frischimport, Paketierung. Alle0.12Artefakte/Checkpoint12d535b geschützt. Keine Gameplay-/Netzwerkänderung, keine parallelenUnityprozesse.

Zusätzlicher direkter Nutzerauftrag ausAssetchat: WLAN-/OnlineConnectscreen imHome-Stil überarbeiten. GemeinsameVersion daher0.13/code13 statt0.12.1. HUDowner weiterhinGameUI.cs/Home/MenuTests nurSafeAreaBackground(Navyfürhome+connect). Assetowner NURneueGameUI.Connect.cs/ConnectPresentationTests+Metas, vorhandeneConnectmethoden/Callbacksweiterverwenden. Root integriert nachHUDFreezeConnectrouting/Header, Versionen undTests/Builds. CapturezieleMenu-v13/Connect-v13;0.12Belegegeschützt. KeinNet-/Regelwechsel.

Separater neuer Entwicklungsauftrag im Designchat: Freundes-/Teammodus für4/6Personen, ausdrücklich OHNEBots/BotFill. NurTools/TeamSiegeLab/** undDocs/ResearchEvidence/team-siege-v1/** zugeteilt; lauffähigerisolierterMechanik-/Geometrieprototyp, keineRuntime/Unity/0.13Änderung. AktiveEntwürfe aus0.13Paket/Checkpoint ausgeschlossen. BestehenderSolo-Bot unverändert. NächstesRuntimefenster wird erstnach0.13Abschluss konkretvereinbart.

Weiterer Folgeauftrag imHUDchat: Host/Admin legt vorStart Wellenzahl undStartgold fest. Fürnächstes0.14Fenster isoliertTools/HostSettingsDraft/** zugeteilt; hostautoritativeValidierung, Gastanzeige, Readyreset, Startlock undTeammoduskompatibilität erforderlich. NochkeineRuntimefreigabe, keineanderenÖkonomieregelnstillschweigendändern.0.13Abnahmebleibtfreeze.

Economyowner ergänzend ausschließlichTools/HostRulesLab/** fürreinenC#-Regel-/Validierungsprototypzugeteilt. KeinAssets-/Runtime-/Unityzugriff. API-AbgleichmitHUD/Teamdesign, ebenfallsaus0.13Paket/Checkpointausgeschlossen.
