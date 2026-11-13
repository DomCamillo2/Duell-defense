# Defense Duell0.16 – Prüfbericht

App0.16.0/Android17, Core2.5, Content11, Wire10. Beauftragter Gesamtumfang: [Iteration16Contract.md](Iteration16Contract.md), Umsetzung: [FeatureAcceptance16.md](FeatureAcceptance16.md). Vorherige0.15-Artefakte bleiben erhalten.

Quellfreeze: TestResults/integration-v16-source-manifest.json mit4836Dateien.214AOgruppen und2088gerenderte/verifizierte Modellporträts. Laser8Ticks, Ausrückabstand2Ticks, vier Grundklassen6/8/12/10Ticks; zwei zusätzliche Eliteklassen1200/1600G. 1×/10×/Max, fünf aus acht Turmklassen, sichtbare Boss-/Superbossgruppen und dynamische Endlosrezepte integriert.

NativeEditMode237/237 und aktuelle reineCore143/143 bestanden. Python181/181 auch nach abschließender diagnostischer Masken-/Receiptpräzisierung erneut bestanden. RealeWire10-Codecgoldens65Checks auf aktuellenCorequellen. Kein Addieren überlappender Laborassertionen als unabhängige Vollsuiten.

NativePlayMode: Erstlauf188/217;29überwiegend überholte Navigations-/Preis-/Auswahl-/Darstellungsannahmen. Die vorgeschaltete Turmauswahl wird nun ausdrücklich bestätigt, Seitenbuttons werden geklickt, Scrolloptionen sichtbar bewegt und frisch aktivierte Grafiken vor Raycasts einmal tatsächlich gerendert. Assertions für Geld, Besitz, Modalabschirmung, Mindestgrößen, Klickziele und Freigabe bleiben erhalten. Erhaltene Zwischenläufe und die anschließende vollständige Suite liegen getrennt unter TestResults/playmode-v16*. **Finale vollständige NativePlayMode-Suite217/217bestanden**,319,18Sekunden, keine übersprungenen Tests.

EchteMehrprozessabnahme: 2v2,3v3,2v2v2 jeweils LAN und Relay/DTLS bestanden, je zwei vollständige1-Wellen-Partien samtRevanche. Vier beziehungsweise sechs eigene Spielprozesse, keine Bots oder gefälschten Verbindungen. In2v2wurden zusätzlich10×/Max, abgelehnte veralteteBatchquotes sowie beideEliteklassen und eingefroreneForschung pro Teilnehmer geprüft (features16-lan/relay). Alle Peers stimmen im endgültigen Zustand überein. GemeinsamerCore/Wire/Session/Teamdriver unverändert; einzige spätere Runtimeänderung betrifft die1v1-Diagnosemaske143→151, damit fünf ausgewählte Klassen einschließlich Mine dieselben Diagnoseaktionen erlauben.

Finaler1v1-Störnetzlauf bestanden:441gemeinsameSnapshots,62Hostbefehle; tatsächlicherUDPproxy mit75msjeRichtung,±15msJitter und2%nominellem Verlust. Weitergeleitet329/340Gast→Host und1315/1338Host→Gast. Mehrfachkäufe und Tempowahl geprüft; keine Android-Frametimebehauptung. Relay200-Wellen-Auswahl wurde als Starttest bestätigt, nicht als200gespielteNetzwellen.

EarlyBoss16:120arrangierteW10-Kämpfe/18.984Checks,76Bosskills. Aufallen6Maps bei6000G vollständige Gegenstrategien ohneLecks, präziseKosten/Positionen/Restgold festgehalten. Kein natürlich erspieltesBudget- oder allgemeinesSpaß-/Monotonieargument. EncounterGrowthisoliert30.276Checks, aktuelleCore prüft Bodybudget bis64, erhalteneLP, eindeutigenBossindex undBigIntegerwellen. FrüheNaturalSlices mit50kSetup bleiben gesondert markiert.

FinalerFrischimport ohneLibrary bestanden:4829kopierteAssets/Packages/Settings-Dateien exakt verglichen und2088Porträts geprüft, TestResults/fresh-import-v16.json. APKnachfinalerFeature-/Testabnahmegebaut: [DefenseDuell-0.16.apk](../Builds/Android/DefenseDuell-0.16.apk),132.243.214Bytes/126,12MiB. SHA256`d005c5f4c06408782a1323956b1291821fc320a88a34cc951aebc80bdd50e868`. ARM64/IL2CPP/GLES3,minAPI25/target35,Code17,APK-v2Debug-Testsignatur. GleicherSignaturfingerabdruckwie0.15,tatsächlicheLAN-/Multicastrechte,ZIP-CRC,keinegroßenPaketlückenundeinbytegenauesIntrovideoPASS.4836Freeze-DateiennachBuildexakt;TestResults/release-v16-source-artifact-binding.json. QuellenarchivnachdiesemBerichtgebautundvollständigbyteverglichen:7972Dateien,399.166.773Bytes,SHA256`f1fabb7a1cfe96b55a942ce04a51bf7904f5163256b0b4f8af80ef26f2361bc6`. TestResults/source-artifacts-v16.jsonbindetdenSnapshotvorAbschlussmetadaten. Kein Gerätestart, Zwei-Menschen-Test oder15-Minuten-Hardwareleistungsnachweis. Keine Änderung anBilling/Plänen, keine bezahltenAssets, keinStoreupload.


| Definition of Done | Stand |
|---|---|
|1. Frischer Import und dokumentierter Build | Bestanden: exakter Frischimport und Android-/Mac-Build |
|2. Vollständige Mehrspielerpartien | Automatisiert bestanden: sechs Team-LAN/Relaykombinationen; zwei menschliche Spieler nicht geprüft |
|3. WLAN und echtes Online-Relay | Bestanden: echte getrennte Spielprozesse, keine Relay-Simulation |
|4. APK, Größe, SHA256, Signatur | Bestanden:132.243.214Bytes, obigerHash,APK-v2-Testsignatur |
|5. Android-Geräte/Leistung | Nicht geprüft: kein freigegebenesADBgerät, Hardwareaudio/video/touch und15-Minuten-Thermik fehlen |
|6. Kernregeln und Neustarts | Bestanden:237Edit/217Play/143Core, atomareWirtschaft, Snapshot-/Revanche-/Störnetzpfade |
|7. Herkunft und Lizenzen | Bestanden: ASSET_LICENSES.csv/THIRD_PARTY_NOTICES; eigeneElite-/Bossgeometrie, keinebezahltenAssets |
