# Defense Duell – aktueller Projektstand

Stand: 13. November 2026. Integrations-/Release-Owner ist der Hauptchat „Entwickle Defense Duell für Android“, Thread `01a1c67c-8801-7cf2-9b86-2b5353ef4b76`, Host `local`. Alle Projektchats teilen diesen Arbeitsordner. Der Nutzer hat ihre Zusammenarbeit und die Subagent-Aufgaben ausdrücklich beauftragt.

## Aktive Version 0.11

App **0.11.0 / Android-Code 11**, Core **2.1**, Content-Schema **8**, Wire **7**. Runtime ist integriert und eingefroren; native Vollsuiten bestanden; reale Netzwerkprozesse und finale Builds folgen. Vertrag: [Docs/CONTRACT.md](Docs/CONTRACT.md). Aktueller Bericht: [Docs/Release11TestReport.md](Docs/Release11TestReport.md).

Enthalten: deutlich größere Vollbildkarte, gamifiziertes HUD, unbegrenzte klassenspezifische Upgrades/Forschung und exakte große Zahlen, elf sichtbare Modellmeilensteine,278 native Porträts, Material/AO/Kontaktschatten und dezente Effekte, neues App-Icon, Drag-/Abwahlkorrektur und gepufferte Gastbewegung. Eigene Sounds und Gold-/Einkommensfeedback bleiben enthalten.

Zusätzlicher direkter Auftrag aus dem Economychat: aktiverer Sendefluss und Minenbalance. Integriert sind Mine160G/40%Verkaufsrefund, Kampf65%, Cooldowns12/20/30/24Ticks und30s Mindestsendefenster. HUD zeigt noch gebundenes Gold, ersten/neuen/letzten Ausmarsch getrennt und unbekannte Wellenwartezeiten offen.

## Tatsächliche Nachweise

- 102/102 reine Core-Szenarien auf tatsächlichen Assets bestanden; Standard-CoreHarness ebenfalls102/102. Baseline und Kandidat je360 Vergleichspartien; zusätzliche360 Kandidat-Replays exakt bestätigt. Unabhängige Coreprüfung ohne reproduzierbaren Befund.
- Netzwerk-Testdriver gegen echte Core2.1-Modelle:3Karten×2Runden bestanden.43 Comparator-Prüfungen bestanden. Echter Relay hat zwei vollständige Partien/Revanche bestanden; zehn LAN-Zyklen laufen. Gestörter Lauf fand einen False-Fail im automatischen Testspieler nach korrekter InsufficientGold-Ablehnung; Pflichtszenarien waren bestanden.
- Unity6000.3.28f1 Prepare und Contentmigration erfolgreich.149/149 EditMode bestanden. 141/141 vollständige PlayMode-Prüfungen bestanden, einschließlich aller sechs neuen EconomyHud11-Tests.
- 278 Porträts neu gebacken/verifiziert. Gegenüber einzeln gesichteter0.10Galerie: identische Alpha, maximal1/255 RGB-Abweichung. Keine neue Geometrieänderung in0.11.
- Quellbindung vor PlayMode: `TestResults/economy-v11-source-manifest-initial.json`.

## Gebauter Zwischenstand 0.10

`Builds/Android/DefenseDuell-0.10.apk`,70.780.698Bytes, SHA256 `3de36851c7afc878fac43444ea684497f53f7dbb391ebb4d57153f011494c4e9`. ARM64/IL2CPP/GLES3, Development/Testsignatur, APK-v2 verifiziert. Noch nicht auf Android gestartet.140Edit/135Play plus6 abschließende Tooltipprüfungen bestanden.278Porträts,9Map-,8HUD- und6arrangierteFeldansichten gesichtet. Voller Bericht: [Docs/Release10TestReport.md](Docs/Release10TestReport.md).

## Zuständigkeit und Freeze

Alle Runtime-/Testbesitzer haben ihren Stand an Root zurückgegeben. Keine weiteren konkurrierenden Runtimeedits. Root allein führt Unity, Tests, Versionen, Builds, Git und PROJECT_STATE. Bei konkretem Fehler erfolgt eine neue ausdrückliche Dateizuteilung. Read-only Reviews sind erlaubt. Keine parallelen Unity-Editoren.

Neue Buildziele: `Builds/macOS-EconomyReview/DefenseDuell.app`, `Builds/Android/DefenseDuell-0.11.apk`. Noch nicht als vorhanden oder gestartet ausgeben. Protected:0.10APK sowie alle0.9/0.8/0.7 und älteren Vorschauen, insbesondere vom Nutzer übernommeneAVD `DefenseDuellReview`. Nicht schließen, überschreiben oder fernbedienen.

## Offene Abschlussarbeit und Grenzen

HUD7/7 plus Einzahl-Nachtrag bestanden. Mac/APK0.11 und Frischimport bereits erfolgreich; nach der unten beschriebenen Testdriverkorrektur werden finale Artefakte und gestörterLAN/Relay erneut erzeugt. Zehn LAN-Zyklen und Quellenarchiv abschließen. Physischer Android-15Minuten-Frametime/Speicher/Thermiktest, zwei menschliche Spieler über getrennte Internetzugänge und subjektive Lautsprecher-/Balanceabnahme bleiben ausdrücklich nicht geprüft. Nutzer bestätigt Installation/Funktion einer früheren Version; das ist kein neuer0.11Gerätenachweis.

Keine bezahlten Assets, kein Billing, keine Store-Veröffentlichung. Vier-Spieler-Modus nur getrennte Machbarkeitsanalyse. Keine automatische Dauerüberwachung. Historische Integrationsschritte: [Docs/Iteration10IntegrationHistory.md](Docs/Iteration10IntegrationHistory.md);0.9-Abschlussbericht bleibt separat erhalten.

### Gezieltes Testdriverfenster

network_smooth darf ausschließlich die allowRace-Whitelist automatischer Botkäufe in NetworkTestDriver.Record um InsufficientGold ergänzen und isolierte Regressionen ausführen. Autoritätsbeleg: vorherigerKauf22→4G, veralteteReplicaTick1995, FolgekommandoHosttick2010bei10G korrektabgelehnt und before==after. Pflichtaktionen bleiben strikt; keine Core-/Sessionänderung. Root baut in separates macOS-EconomyFinalReview, damit laufende10LAN-Zyklen auf vorherigemMac nicht überschrieben werden. AlteFehler-/Erfolgsbelege bleiben erhalten.

Finaler Testdriverfixfrozen und9isolierteRegressionenPASS. Mac-EconomyFinalReview, finaleAPK undFrischimport gebaut/geprüft. NeuerRelay erneut2Vollpartien/RevanchePASS. GestörterLAN ebenfalls2Vollpartien/Exit0; Auswertung verwechselt fertigesTempomessfenster mit späterem unvollständigemDuplikat. network_smooth darf ausschließlich Comparator/Negativtests korrigieren, ohneRuntime- oder Toleranzänderung. Originalauswertungalscomparison-initial.jsonerhalten; Daten/Logsbleibenunverändert. ZehnsaubereLAN-Zyklenlaufenweiter.
