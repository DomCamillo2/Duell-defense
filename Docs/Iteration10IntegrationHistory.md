# Defense Duell – verbindlicher Projektstand

Stand: 11. November 2026. Root/Hauptchat `01a1c67c-8801-7cf2-9b86-2b5353ef4b76`, Host local, ist alleiniger Integrations-/Release-Owner. AGENTS.md und Docs/CONTRACT.md beachten.

## Aktueller Stand 0.9 – getestet und gebaut

App 0.9.0 / Android-Code9, Core v1.9 / Content-Schema6 / Wire6. Vollbildlandschaft hinter dem HUD, physisches Renderziel bis2560 Pixel, überarbeitetes Terrain/Material/43 Porträts, Goldrate mit tatsächlichem Spawnbonus, Bauhinweise und neue eigene Klangpalette sind integriert. Regeln/Preise/Kernprotokoll unverändert.

- Vollständige PlayMode-Suite96/96 PASS;124 gebundene Quellen.599/599 zusätzliche PCM-/Gatechecks. Neue Render-, Einkommens-, Bau- und Audiotests enthalten.
- Zwölf unabhängig geprüfte HD-Ansichten plus Root-Sichtung; keine blockierenden Layoutbefunde. Unity-Captures, keine Android- oder Audio-Hörabnahme.
- Mac: `Builds/macOS-ImmersiveReview/DefenseDuell.app`, eigenes Bundle/Fenster0.9.0. Zwei eigene Headlessprozesse: LAN PASS mit361 gemeinsamen Snapshots/50 Hostbefehlen/beideExit0. Keine interaktive native Abnahme.
- APK: `Builds/Android/DefenseDuell-0.9.apk`,45.395.499 Bytes, SHA-256 `ba49555644417a93bf1eaffa0f66f03d3fdd07a90a1a0c8e896d7a89b5114f58`. ARM64/IL2CPP/GLES3, Development/Debug-Testsignatur, APK-v2 verifiziert. Noch nicht auf Android gestartet.
-27 Core/Net/Content/Platform/Progressionquellen unverändert gegenüber0.8. Core/Net/Content/Progression-DLLs bytegleich. Reale Relay-Vollpartien/Revanche bleiben historische0.7-Belege, kein neuer0.9-Relay-Lauf.
- Frischimport ohne Library und43 Porträts PASS: `Builds/FreshImport-20261111-162545`. Quellenarchiv `Artifacts/DefenseDuell-0.9-Source.zip`; Größen/Hash/Integritätsnachweis im separaten `TestResults/source-artifacts-v09.json`.

## Freeze und Besitzregeln

Alle0.9-Runtime-/Testdateien eingefroren. Root allein verantwortet Unity, Builds, Versionen, Git und PROJECT_STATE. HUD-/Audio-Owner haben abgeschlossen; konkrete neue Änderungen nur nach Root-Dateiübergabe. Unabhängige Folgelabs bleiben isolierte Entwürfe, keine Runtimefreigabe. Historischer Integrationsverlauf: `Docs/Iteration09IntegrationHistory.md`.

Geschützt bleiben HudReview0.8, WaveReview0.7, SpeedReview0.6, ArtReview0.5, Candidate0.4 und Next0.3; nicht überschreiben, schließen oder fernsteuern. Auch die vom Nutzer übernommene0.7-AVD DefenseDuellReview nicht ungefragt neu installieren. Gebaut, automatisch gestartet, menschlich gespielt und auf Android gestartet getrennt benennen.

## Nachweise und Grenzen

Aktueller Bericht: `Docs/Release09TestReport.md`. Regelvertrag: `Docs/CONTRACT.md` und `Docs/Iteration07Contract.md`. Prüfung: `python3 Tools/check_project_state.py --check-frozen --manifest TestResults/network-v6-immersive-v09-source-manifest.json`. Finale PlayMode-Bindung: `TestResults/immersive-v09-source-manifest.json`.

Der Nutzer bestätigt Installation/Funktion der vorherigen Version; das ist eine Nutzerbeobachtung und kein unabhängiger0.9-Geräte-/Leistungsnachweis. Offen:0.9-Installation/Gesten/Handylautsprecher, physisches Samsung mit15-Minuten-Frametime/Speicher/Thermik, zwei Menschen über getrennte Internetanschlüsse und menschliche Balance. Kein Billing, keine bezahlten Assets, keine Store-Veröffentlichung, keine ausstehende Nutzerfreigabe. Keine automatische Dauerüberwachung.

## Separates Folgepaket – noch nicht in der Runtime

Direkte Nutzerwünsche aus den Projektchats: alle sieben Turm- und vier Truppenklassen sollen zunehmend teurer, stärker und sichtbar mächtiger werden. Neuere Präzisierung: keine künstlichen Mengenlimits für Gebäude, Forschung oder Truppenkauf/-sendung; Geld, Platz und Wellen bleiben. Zehn sichtbare Meilensteine sind kein Upgrade-Maximum. Pfade müssen zur Klasse passen; Frost ausdrücklich Frostdauer/Reichweite und kein Schadensupgrade, bestehender Basisschaden unverändert.

Eingefrorene isolierte Entwürfe: `Tools/UpgradeEvolutionLab/` mit Ansicht/Balance, `Tools/UnlimitedProgressionLab/`, `Docs/UnlimitedProgressionDesign.md` und zugehörige ResearchEvidence. Diese sind weder integrierte0.9-Regeln noch freigegebene Balance/Unitymodelle und aus dem0.9-Release-Checkpoint/Quellenarchiv ausgeschlossen. Zuständigkeiten für einen späteren Core/Net/Stats/Modell-Durchstich müssen vor Runtimearbeit neu vergeben werden. Offene Punkte: nützliche unbegrenzte Fortsetzung statt wirkungsloser Werte, Klassenrollen, Minenwirtschaft, echte Frost-Hit-Auflösung, sichere Zahlen/Wire, Renderingressourcen und Balance im Kampf.

## Aktives Integrationsfenster 0.10 – größere Spielkarte und neues HUD

Direkter Nutzerauftrag nach0.9: Die eigentliche Map soll viel größer bis Vollbild sein und darf unter dem Overlay liegen. Gesamtes Overlay schöner und spielerischer gestalten; tatsächliche Renderqualität prüfen. Root besitzt BoardView/Kamerafit, Root-Render-/Legacytests, Editor/Version und Integration. HUD-Chat01a1c6c8-89f2-7ac3-9a13-fac8362270ca erhält GameUI*.cs, eigene neue Theme/Icon-Helfer, CompactHud08Tests/ImmersiveHud09Tests und neue Overlay10Tests. Kein anderer Chat/Agent editiertRuntime oder startetUnity.0.9-Artefakte undNutzervorschauen bleiben erhalten. Core/Wire/Audio/Progression unverändert; Folgelabsbleiben eingefroren. NeueApp0.10.0/AndroidCode10 und separatesMaczielmacOS-FullscreenReview.

Kamera darf Randdeko und maximale hypothetische Turmhöhen zugunsten größerer Spielbodenfläche aus der Vollübersicht verlieren; Pan/Zoom muss alleBaupunkte erreichbar halten. GeplanteBildabnahme auf allen3Maps undgängigenSeitenverhältnissen mit gemessenerMainRenderTexture statt bloß PNG-Auflösung.

Erweiterte konkreteHUD-Zuteilung: ActionButtonGraphic.cs/ActionButtonVisual.cs dürfen durch denHUD-Owner für konsistentePaletten geändertwerden; Symbole/Zustände/Consent/TouchAPI bleiben erhalten.

## Aktives erweitertes Integrationsfenster 13. November 2026

Nutzer fordert neueAPK mit allenentwickeltenFunktionen, LAN-Gastruckelnkorrektur und eigenenSubagentfürhängendeBau/Dragauswahl. AufexpliziteRückfrage wurdeauchvollständigeIntegrationunbegrenzterklassenspezifischerUpgrades bestätigt. ÄltereFolgepaket-Freezegrenze ist fürdenfolgendenkonkretenScopeaufgehoben; keine0.10FreigabevorIntegration/Tests.

Dateibesitz: Root=Integration/Editor/Version/Docs/Testkoordination. Subagentdrag_fix=GameUI.cs/BattleHud/Immersive,BuildDragSource,BoardPointerSurface,UiPointerGate undneueRegressionstests; nachBugabschlussÜbergabeRootfürDTO/HUDMigration. Subagentprogression_core=Assets/Core/**,Assets/Content/** undCoretests. Subagentnetwork_smooth=Assets/Networking/**,BoardView.cs undneueInterpolation/Poolhelfer+Tests. AnsichtsChat01a1c6bc-f70f-75c2-a78e-30f542f60d63=DioramaMeshes.cs,neueVisualEvolution-Helfer/Tests,keineBoardViewänderungen. HUDchatistfrozen/read-only; Design/Mapschatsberatenread-only. AlleinRootstartetUnity/Build/Tests/Git.

VertraginAbstimmung: exakteBigIntegerfürGeld/Kosten/Level/HP/Schaden/Einkommen, semantischeKlassenpfade, FrostDauer/ReichweitekeineDamagekäufe; keineGameplaymengenlimits, Geld/Platz/Wellen/FIFObleiben. Wire/Schema/Coreversionwerdenangepasst. VisuelleMeilensteinestattunbegrenztemMeshcache. RenderingmitInterpolationohneÄnderungderHostautorität. Alte0.9Artefaktebehalten.

WeiterekonkreteÜbergaben: drag_fixhatRuntimefixfrozenanRootzurückgegeben; alleGameUI*.cs,ActionButtonGraphic/Visual,UpgradeBudgetGraphic,PresentationCommandTrackerundggfAttackIncomeFeedback liegenjetztbeimHUDchatfürBigInteger/semantischeUpgradeUI. drag_fixmigriertbestehendePlatform/PresentationPlayModetests (außerRootFullscreen10/RenderQuality09,Overlay10HUDchat,neueArt/Terrain/Networktests). HUDchaterstelltzusätzlichNutzerAppIconunterAssets/Art/AppIcon/v1undneuenEditorAppIconSetup. Audio/Assetchat01a1c6a4...hatScenicTerrain/MapLandmarkArtfürTerrainqualität;DioramaMeshesbleibtAnsichtsownerinklusiveBasismodellnormalen. RootalleinGeneratedClassIcons/ModelPortraitBaker;278gebundeneStufenporträtsgeplant. DerVier-Spieler-WunschimDesignchatistnureineMachbarkeitsprüfung,separaterEntwurf,nichtTeilaktuellerRuntimeintegration.

### Übergabe für gemeinsamen Import (13. November)

Core/Content, HUD/Icon, Modelle und Terrain sind als Quellstand übergeben; Core meldet 92/92 reine .NET-Szenarien. Netzwerk finalisiert das Wire-7-ACK-Fenster. Root übernimmt sämtliche Runtimequellen nach finaler Netzübergabe. drag_fix erstellt ausschließlich neue UnlimitedHud10Tests; progression_core migriert ausschließlich Tools/CoreHarness. Die bisherigen Test-/APK-Nachweise gelten weiter nur für 0.9. Neue Unity-Importe/Tests/Builds sind noch ausstehend. Zwei bestehende Projektchats prüfen die tatsächliche Core-/Netzimplementierung unabhängig und read-only.

### Native Integration und anschließender Grafikdurchstich

Prepare und 278 native Porträts erfolgreich; EditMode 140/140 PASS. Erster PlayMode-Lauf 118/124, Fehler bleiben als playmode-v10-initial.xml mit Quellmanifest und initialen Captureordnern erhalten. Korrekturen: fehlender CanvasRenderer auf HudIconGraphic (HUD-Owner, jetzt frozen), zwei NUnit-BigInteger-Vergleiche (Root), drei Testfixture-/Capture-/Navigationfehler (drag_fix, frozen). Root wartet vor erneutem Unitylauf auf die Modellfixes. Ansichtsowner hat VisualEvolutionGeometry und eigene Modelltests für Cannon-Pfadunterscheidbarkeit sowie Ballista/AP-Scopehalterung; keine anderen Modelle ändern.

Neue direkte Nutzerbeauftragung im Assetchat: Beleuchtung/Materialtiefe, AO/Kontaktschatten und dezente Effekte umsetzen sowie alle Assets prüfen. Assetchat bereitet dafür Tools/GraphicsQualityLab isoliert vor, keine Assets-Runtimeedits bis Root-Integration. Root besitzt weiterhin BoardView-Hooks, Bake, Unity und Release. Eine 0.10-APK ist weiterhin ausstehend. Native Baseline bestätigt alle drei Karten bei 1350/1920/2400×1080 mit 4×MSAA; nicht mit Android-Geräteleistung gleichsetzen.

Grafikpaket jetzt integriert: native Schattierungsberechnung 177 Gruppen / 6.596.648 Strahlen / 0 abgebrochene Strahlen, 1.323.726 Bytes; alle 278 Porträts mit neuem Licht/AO neu gebacken und unabhängig einzeln gesichtet. Zusätzliche native Grafik-/Ressourcen-/Lichttests laufen mit der vollständigen PlayMode-Suite. Keine APK-Freigabe daraus ableiten.

Neuer Nutzerauftrag im Ansichts-/Economychat: Wartezeiten/Minen für aktiveres Spiel optimieren und Balance testen. Vereinbartes Folgefenster 0.11: Ansichtsowner untersucht konkrete Varianten ausschließlich unter Tools/EconomyFlowReview in echter .NET-Kampfsimulation. Keine Core-/Content-/HUD-Runtimeänderung während 0.10-Tests/Build. Root vergibt diese Dateien nach 0.10-Buildabschluss; Regeln/Content ggf. 2.1/Schema8, Wireänderung nur bei tatsächlichem Datenlayoutwechsel. Diese neuen Wirtschaftsänderungen sind noch nicht Teil der 0.10-APK.

### Letzte 0.10-Abnahme / 0.11-Zuteilung

135/135 vollständige PlayMode-Prüfungen PASS (12:17–12:20 UTC). Alle 278 Porträts sowie neun Map- und sechs arrangierte Feldansichten unabhängig gesichtet. Eine letzte Tooltip-Randbegrenzung erhält einen gezielten ImmersiveHud-Nachlauf. Quellen: fullscreen-v10-source-manifest.json, Feldbilder zusätzlich graphics-field-v10-source-manifest.json. Der 0.10-Androidbuild wird als geschützter Zwischenstand erstellt; anschließend folgt unmittelbar das bereits autorisierte 0.11-Economy-Fenster mit endgültigem Mac/APK/Frischimport/LAN+Relay-Nachweis.

0.11-Kandidat gewählt nach echten Vergleichspartien: Mine160G,40%Minenrefund (Kampf65%), SendCooldowns12/20/30/24Ticks, MindestSendfenster600Ticks. Coreowner01a1c6bc... hält getesteten11-Dateien-Patch unter Tools/EconomyFlowReview/Integration bereit (102/102reineSzenarien), noch keine Assetsänderung. HUDowner01a1c6c8... bereitet Tools/EconomyHud11Draft isoliert vor, genaue Forecast-/Refund-API direkt abgestimmt. network_smooth bereitet Driver/Comparator auf geänderte Vorbereitungsbudgets vor, noch keine Runtimeänderung. Root vergibt tatsächliches Assetsfenster erst nach0.10-Androidbuild. Keine Dauerüberwachung behauptet.

### 0.10 gebaut; Runtimefenster 0.11 offen

0.10 APK geschützt als Builds/Android/DefenseDuell-0.10.apk,70.780.698 Bytes, SHA256 3de36851c7afc878fac43444ea684497f53f7dbb391ebb4d57153f011494c4e9. APK-v2/ARM64/IL2CPP/Version10 verifiziert; noch kein Androidstart. Letzte Tooltipprüfung6/6 und SichtungPASS; Fullsuite135/135 davor separat erhalten. Quellbindung apk-v10-source-manifest.json.

0.11-Dateien jetzt zugeteilt: Economychat11C# und3Meta gemäß Integrationmanifest; HUDchatfünfGameUI plus QueueForecast/neueEconomyHud11Tests; network_smooth NetworkTestDriver/ToolsNetworkTests; drag_fix ausschließlich passende bestehendePlayModefixtures. RootVersion/Editor/Vertrag/Tests/Build. Keine Unityprozesse währenddieserÄnderung. Grafik-/Audiogeometrie bleibt eingefroren. Finale realeLAN/Relay/Frischimport/Androidabnahme ausstehend.

0.11Core/Content jetztfrozen:102/102 aufAssets,360exakteBestätigungsreplays, unabhängigerReview ohneBefund. Legacytests/Netzdriverfrozen; HUDneueTestsletzteÜbergabe. Rootmigration/Unity/Build/Transport nochausstehend. StandardCoreHarness nun102Fälle.

### Native0.11-Prüfung läuft

AlleRuntimebesitzerhabenDateienanRootzurückgegeben; Freeze. Prepare/ContentmigrationSchema8 erfolgreich,149/149EditModePASS. Porträts278mitneuemContenthashverifiziert; alleAlphaexaktgleichzu0.10, maximaleRGB-Abweichung1/255. VollePlayMode-Suitegestartet, nochkeinErgebnis. EconomyNetzdriver3Maps×2RundenModellPASS/43ComparatorregressionenPASS; tatsächlicherTransportnochfolgend. Initialquellenbindung economy-v11-source-manifest-initial.json.
