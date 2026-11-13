# Angriffsstapel und Economy-Review

Stand: 10. November 2026. Der Nutzer möchte wiederholt auf Angriffe tippen, die bezahlten Truppen stapeln und automatisch nacheinander mit begrenztem Durchsatz ausmarschieren lassen. Eine separat per Button freizugebende Welle ist ausdrücklich nicht gemeint. Zusätzlich empfindet er derzeit zu viel verfügbares Gold. Diese Anforderungen ergänzen den laufenden Drag-and-drop-/Game-Feel-Zyklus.

Hauptchat und Gameplay-Agent verantworten Core, Netzwerk, Inhaltsdaten und Messläufe. Der Recherche-Chat liefert die Diagnose und prüft die Resultate. Die folgenden Zahlen beschreiben die geprüfte Ausgangsfassung `defense-duell-core-v1.2` mit `Standard.asset`; sie sind noch keine beschlossenen Änderungen.

**Aktueller Integrationsentscheid:** Nach insgesamt fünf getrennten Messreihen hat der Hauptchat für die v1.6-Beta **5 Basisgold/s**, Kill-Bounties **4/5/14/2** und Geschwindigkeiten **104/184/59/93** für Läufer/Sprinter/Tank/Schwarm festgelegt. Das entspricht 160 % der vorherigen Bewegungsgeschwindigkeiten. Startgold, Preise und Minenkosten bleiben gleich. Die Gründe und vollständigen zusätzlichen Messreihen stehen in [EconomyIteration.md](/Users/domi/Documents/Handygame/Docs/EconomyIteration.md). Der Kandidat wird zusammen mit freier Platzierung integriert; die bisherigen Slotkarten-Messungen sind kein Nachweis für die finale freie Karte oder Android-Spielgefühl.

Für das tatsächliche Geldgefühl bleibt eine Rückkopplung relevant: Vollständig abgewehrte Angriffe übertragen dem Verteidiger etwa 21–22 % ihres Kaufpreises als Kill-Gold (Läufer 4/18, Sprinter 5/24, Tank 14/65, Schwarm 8/38). Ein vereinfachtes symmetrisches Modell mit ausschließlich Angriffsausgaben, vollständiger Abwehr auf beiden Seiten, ohne Minen und ohne Aufbaukosten ergibt `Ausgabenrate = 5 / (1 − Rückflussanteil)`, also ungefähr 6,32–6,43 Gold/s. Das ist eine illustrative stationäre Rechnung, kein gemessener Matchverlauf. Sie erklärt, warum die nominelle Senkung auf 5 G/s zusammen mit Kill-Gold weiterhin ein ähnliches Gesamtbudget wie früher erlauben kann. Bei der nächsten menschlichen Runde sowohl ungenutztes Gold als auch wahrgenommene Kaufentscheidungen prüfen.

## 1. Ausgangslage

Startgold: 180. Basiseinkommen: 6 Gold/s. Minen: höchstens zwei, Ertrag pro Mine 1/2/3 Gold/s, Kosten 140 + 100 + 160. Damit reicht das laufende Einkommen von 6 bis 12 Gold/s. Abschüsse geben kein Gold. Für alle Gebäude werden beim Verkauf 65 % der investierten Kosten erstattet.

| Zeitpunkt seit Spielbeginn | Ohne Ausgaben/Minen bei 6 G/s | Vergleich bei 5 G/s | Vergleich bei 4 G/s |
| --- | ---: | ---: | ---: |
| 30 Sekunden | 360 | 330 | 300 |
| 1 Minute | 540 | 480 | 420 |
| 2 Minuten | 900 | 780 | 660 |
| 3 Minuten | 1.260 | 1.080 | 900 |
| 4 Minuten | 1.620 | 1.380 | 1.140 |
| 8 Minuten | 3.060 | 2.580 | 2.100 |

Rechnung: `180 + Sekunden × Basiseinkommen`, ohne Ausgaben, Minen, Verkauf oder vorzeitiges Matchende. Das sind Budgetbeispiele, keine beobachteten Spielerbestände.

Bei leerem Konto kostet das Ansparen eines Läufers für 18 Gold rechnerisch 3 / 3,6 / 4,5 Sekunden bei 6 / 5 / 4 Gold/s. Ein Schnellfeuerturm für 65 Gold benötigt 10,83 / 13 / 16,25 Sekunden. Die tatsächlichen Buchungen erfolgen sekündlich. Ein Wechsel auf 4 G/s senkt die laufende Basiszufuhr um ein Drittel; bei unverändertem Startgold sinkt das verfügbare Gesamtbudget weniger stark.

Eine Basismine braucht ohne Verkauf 140 Sekunden, um ihre 140 Gold selbst zu erwirtschaften. Mit anschließender Rückerstattung von 91 Gold liegt der rechnerische Ausgleich der Nettokosten bei 49 Sekunden. Die inkrementellen Upgrades entsprechen ohne Verkauf 100 bzw. 160 Sekunden für jeweils +1 G/s; einschließlich zusätzlichem Verkaufserlös 35 bzw. 56 Sekunden. Diese vereinfachte Betrachtung ignoriert entgangene Angriffe, belegte Slots und drohenden Basisschaden. Sie erklärt jedoch, weshalb spätes Verkaufen die Bank zusätzlich auffüllen kann. Minenrefund und Basisincome zunächst getrennt untersuchen.

## 2. Diagnose: Geldzufluss, Bedienung und Angriffswirkung trennen

In der Ausgangsfassung v1.2 blockiert `MatchSimulation.Send` denselben Truppentyp während `NextSendTicks`. Dadurch kann der Nutzer vorhandenes Gold beim schnellen Tippen nicht unmittelbar in diese Angriffe umwandeln. Die Meldung „Abklingzeit“ ersetzt den gewünschten Stapel. Deren FIFO-Warteschlange enthält höchstens acht Aufträge; sie begrenzt außerdem aktive Einheiten auf 80 pro gegnerischem Feld und gibt höchstens eine Einheit pro vier Ticks = 0,2 Sekunden frei.

Die frühere Kaufabklingzeit beträgt Läufer 1 s, Sprinter 1,5 s, Tank 3,5 s und Schwarm 2 s. Ein Schwarmkauf enthält vier Einheiten. Nur die Cooldownprüfung zu entfernen würde daher die mögliche Tankdichte stark erhöhen. Das kann gewünscht sein, muss aber als eigene Änderung des Kampftempos bewertet werden.

Die bisherigen Bot-Messungen zeigen zudem viele Partien bis zum Acht-Minuten-Limit und starke Verteidigung. Wenn zusätzliche Angriffe kaum etwas bewirken, fehlt auch bei genügend kaufbaren Truppen ein überzeugender Ausgabengrund. Weniger Einkommen allein beweist deshalb noch keine bessere Economy.

## 3. Verbindlicher Ablauf der Angriffswarteschlange

**Ein Tap → ein Kauf → autoritativer Auftrag → begrenzter FIFO-Ausmarsch.** Ein Auftrag darf nur bei ausreichend Gold und freier Kapazität angenommen werden. Geld wird genau einmal beim Kauf gebucht. Abgelehnte Käufe verändern weder Gold noch Stapel. Ein bestätigter Auftrag bleibt beim Erreichen des aktiven Einheitenlimits bestehen und läuft weiter, wenn wieder Platz frei ist.

Kaufannahme und Freigabetakt sind getrennte Regeln. Die bisherigen Cooldowns dürfen zusätzliche bezahlte Käufe nicht verhindern. Für den ersten Vergleich kann ihre Bedeutung in eine Ausmarsch-/Bereitschaftsregel verschoben werden; alternativ wird ein gemeinsamer engerer Abstand getestet. Beides verändert das Verhalten gegenüber v1.2 und benötigt eine aktualisierte Regelversion sowie zugehörige Tests. Das endgültige Ausmarschtempo wird nach Messung festgelegt, nicht als Nebeneffekt des UI-Codes.

Eine sichtbare, begrenzte Kapazität bleibt nötig. Der Hauptchat hat sie in v1.4 auf 80 bezahlte Aufträge je Spieler angehoben und die Kaufabklingzeit in den tatsächlichen Ausmarsch verschoben. Der globale Mindestabstand bleibt zusätzlich wirksam. Die früher erwogenen 32 Aufträge sind damit kein aktueller Implementierungswert. Ein zusammengefasster Stapel würde weiterhin eine bewusste Anpassung der Snapshotvalidierung erfordern: `SpawnOrder.Remaining` beschreibt höchstens die Einheitenzahl eines einzelnen Pakets. Mehrfache Schwarmkäufe bleiben mehrere bezahlte Pakete.

FIFO-Reihenfolge darf bei visueller Zusammenfassung nicht verändert werden. Aufträge einer Klasse können im Badge summiert werden; eine kleine Vorschau der nächsten Aufträge sollte die tatsächliche Reihenfolge erhalten. Ansonsten würde beispielsweise „Läufer → Tank → Läufer“ fälschlich zu „zwei Läufer → Tank“ umgeordnet.

Für den ersten Umfang genügt schnelles wiederholtes Tippen. Gedrückthalten ist eine optionale spätere Komfortfunktion, darf nach Loslassen jedoch niemals weitere Käufe auslösen oder neu eintreffendes Einkommen automatisch ausgeben. Keine unbezahlten lokalen Aufträge vormerken.

## 4. Darstellung und Gefühl

- Auf der Angriffskarte Preis und Paketgröße klar zeigen, etwa „Schwarm ×4 · 38 G“. Ein zusätzlicher bestätigter Schwarm erhöht die wartenden Einheiten um vier, nicht um eins. Falls Aufträge statt Einheiten gezählt werden, ausdrücklich „Pakete“ anzeigen.
- Direkte kleine Druckreaktion pro Tap. Positive Stapelbestätigung erst bei angenommenem Auftrag; ausstehende Eingaben sind als wartend unterscheidbar. Die autoritative Goldanzeige bleibt wahr.
- Bei Annahme eine kleine Zählbewegung oder ein kurzer Impuls am Stapel. Beim Ausmarsch verschwindet jeweils die tatsächlich gestartete Einheit aus dem Wartezähler. Kein Großeffekt für jede automatische Freigabe.
- „Warten“ und „Aktiv“ getrennt darstellen. Am Limit „Warteschlange voll“ beziehungsweise „Wartet auf freien Platz“ anzeigen. Kein stiller Verlust bezahlter Truppen.
- Kein zusätzlicher „Welle starten“-Knopf. Die Warteschlange läuft automatisch weiter, bis sie leer ist.
- ReducedEffects behält Zahlen, Reihenfolge und Zustände, entfernt dekorative Bewegung. Mehrere schnelle Taps dürfen nicht mehrere überlappende Sounds pro Bild erzeugen.

## 5. Economy-Vergleich und Entscheidung

Zuerst 6, 5 und 4 Gold/s bei ansonsten identischen Regeln vergleichen. Startgold und Minen zunächst unverändert lassen, um die Wirkung des Basiseinkommens zu isolieren. Danach die bestätigte Queue-Regel separat und zusammen mit dem bevorzugten Einkommen prüfen. So wird ein stärkerer Angriffsdruck nicht fälschlich der Goldänderung zugeschrieben.

Mindestens dieselben Seeds mit getauschten Seiten auf allen drei Karten verwenden. Balanced, Economy, reine Verteidigung und unterschiedliche Angriffsstrategien einbeziehen. Bot-Ergebnisse sind eine technische Balanceprobe, kein Ersatz für menschliche Spieltests.

Erfassen:

- Goldbestand nach 30/60/120/240 Sekunden, Median und hohe Bestände; nur noch laufende Partien vergleichbar auswerten, frühere Enden getrennt melden.
- Insgesamt erzeugtes und ausgegebenes Gold, getrennt nach Bauten, Upgrades, Minen, Angriffen und Verkaufserlösen; keine Netto-Differenzen als vollständige Ausgabenstatistik ausgeben.
- Käufe und Ablehnungsgründe, tatsächliche Freigaben, Warteschlangenlänge und Wartezeit sowie Zeiten am Einheitenlimit.
- Erste sinnvolle Verteidigung, erster Angriff, erster Basisschaden, Matchdauer, verbleibende HP, Zeitlimit-/Unentschiedenanteil und dominante Strategien.

Ein sinnvoller Kandidat ermöglicht am Anfang Verteidigung plus eine offensive Wahl, macht den dritten oder vierten Kauf zu einer Entscheidung und verhindert langes Ansammeln ohne überzeugende Ausgaben. Es gibt keinen künstlichen Bonus nur fürs Ausgeben. Ein reduziertes Grundeinkommen erhöht relativ den Wert von Minen; deshalb Economy-Strategien ausdrücklich erneut prüfen. Eine zusätzliche Kürzung des Startgolds, andere Minenpreise oder ein veränderter Refund folgen erst bei entsprechendem Befund.

### Gewünschte Entwicklung einer Partie

Das sind Ziele für den nächsten Spieltest, keine zusätzlichen zeitgesteuerten Spielregeln:

- **Eröffnung:** Die 180 Startgold erlauben beispielsweise zwei Schnellfeuertürme und zwei Läufer für insgesamt 166 Gold. Alternativ binden zehn Läufer das gesamte Startgold sofort in der Queue. Danach soll der Unterschied zwischen frühem Druck und früher Absicherung spürbar sein.
- **Aufbau:** Eine Mine für 140 Gold konkurriert mit zwei Schnellfeuertürmen für 130 oder einem Tank plus drei Sprintern für 137. Ihre spätere Rendite soll diesen unmittelbaren Verzicht rechtfertigen; der Bot muss diese Entscheidung tatsächlich treffen können.
- **Mittlere Partie:** Upgrades und gemischte Angriffe sollen auf erkennbare Lücken reagieren. Ein größerer Kontostand ist sinnvoll, wenn der Spieler bewusst auf eine teure Option spart. Gleichförmiges Ansammeln ohne wirksame Ausgabe ist das eigentliche Problem.
- **Späte Partie:** Zwei voll ausgebaute Minen erhöhen das laufende Einkommen bei Basis5 auf 11 Gold/s, zuzüglich tatsächlichem Kill-Gold. Das darf mehr gleichzeitige Entscheidungen und Angriffsdruck ermöglichen. Es sollte nicht bloß die unveränderte Verteidigung weitere Minuten finanzieren; HP-Verlauf, Durchbrüche und Timeout-Anteil bleiben deshalb Teil der Bewertung.

Die Warteschlange ist der Verbindungspunkt zwischen Bedienung und Economy: Sie bindet vorhandenes Gold sofort, ohne den begrenzten Ausmarsch aufzuheben. Wer alles in Angriffe steckt, kann denselben Betrag nicht gleichzeitig zum Bauen verwenden. Bei leerer Bank braucht ein neuer Läufer bei 5 Basisgold/s im kontinuierlichen Rechenmodell 3,6 Sekunden; die tatsächliche nächste Kaufgelegenheit hängt von den sekündlichen Buchungen und weiterem Ertrag ab.

## 6. Korrektheits- und Integrationsprüfungen

Schnelle wiederholte Käufe derselben Klasse müssen bis zur tatsächlichen Gold-/Kapazitätsgrenze angenommen werden. Fehlendes Gold, voller Stapel, falsche Phase, Disconnect und Rate-Limit dürfen keinen Teilkauf erzeugen. Doppelte Sequenzen ziehen nicht erneut Gold ab. Pending-Eingaben dürfen keine falsche verfügbare Kaufkraft anzeigen.

Ausmarsch bleibt FIFO, begrenzt pro Zeitschritt und unter dem 80-Einheiten-Limit. Schwarmgröße bleibt exakt. Bei vollem Feld wartet der bezahlte Rest; er wird weder verworfen noch doppelt erzeugt. Snapshot-Roundtrip und Validierung müssen die maximal erlaubten Stapel erhalten. Revanche löscht alte Aufträge, Reservierungen und UI-Impulse. Host und Client müssen auch mit Verzögerung/Paketverlust denselben Endzustand erreichen.

Produktionswerte werden in Core-Defaults und `Assets/Content/Standard.asset` konsistent aktualisiert. `ContentHash`, gegebenenfalls `SimulationRulesVersion`, Netzwerkannahmen, Bots, Tests und Dokumentation müssen zum neuen Verhalten passen. Erst ein neu gebautes und geprüftes Paket enthält die neue Economy; die gerade gespielte Baseline wird nicht als verändert bezeichnet.

## Review-Protokoll

- Ausgangsanalyse: abgeschlossen; Zahlen und Codepfade geprüft.
- Nutzerklärung: automatische FIFO-Warteschlange durch wiederholte bezahlte Taps bestätigt.
- Messungen: v1.2, v1.4 mit neuer Queue und v1.5 mit Kill-Gold auf bisherigen Karten unabhängig ausgewertet, siehe unten. Zusätzliche große Slotkarten-/Tempo-Reihen sind im Bericht des Gameplay-Owners dokumentiert; daraus hat der Hauptchat den obigen Beta-Kandidaten gewählt. Freie Platzierung und der finale Botstand benötigen weiterhin ihre eigene Abnahme.
- Drag-and-drop und Kauf-Lifecycle: unabhängiger lesender Review abgeschlossen; eigener Core-/Tracker-Probe 8/8 und zentrale Unity-Läufe 76/76 EditMode sowie 25/25 PlayMode bestanden. Nachweise und konkrete Korrekturschleife stehen in [DragPlacementSpec.md](/Users/domi/Documents/Handygame/Docs/DragPlacementSpec.md). Dies ist keine abschließende menschliche Balance- oder Android-Leistungsbewertung.

### Erste Rückmeldung aus der Messschleife

Die vom Gameplay-Agent erzeugte v1.2-Reihe umfasst 2.160 Matches: drei Einkommen, drei Karten, 15 Strategiepaarungen, acht Seeds und jeweils Seitentausch. Die 4.320 Spielerzeilen sind keine 4.320 unabhängigen Matches. Für die folgende Tabelle sind beide Spieler aller Balanced-gegen-Balanced-Partien berücksichtigt: 48 Matches beziehungsweise 96 Spielerbeobachtungen pro Einkommen. Diese Bot-Selbstspiele sind eine kontrollierte Diagnose; sie messen nicht die Bedienung durch den Nutzer.

| Basiseinkommen | Zeitlich gemitteltes ungenutztes Gold | Mittlere Angriffsausgaben pro Spieler | Mittlere Matchdauer | Median Gold nach 60 s / 120 s |
| --- | ---: | ---: | ---: | ---: |
| 6 G/s | 25,69 | 2.539,56 | 480 s | 18 / 18 |
| 5 G/s | 26,36 | 2.055,69 | 480 s | 22 / 34 |
| 4 G/s | 28,95 | 1.576,69 | 480 s | 53 / 67 |

Die Auswertung stützt keine generelle Goldhortung durch aktive Balanced-Bots. Das langsamere Einkommen verringert ihre Angriffsausgaben, während alle diese Partien am Zeitlimit bleiben. Der vorübergehend höhere Kontostand bei 4 G/s passt zur vorhandenen Botregel, auf bestimmte Bauten zu sparen; er ist keine zusätzliche Geldquelle. Die vom Nutzer wahrgenommene Geldfülle kann trotzdem real sein: Sein Ausgabeverhalten und die derzeitige Kauf-Abklingzeit unterscheiden sich von dieser automatisierten Probe.

**Review-Empfehlung:** Stapelbare Käufe und die Ausmarschregel zuerst gegenprüfen. 5 G/s ist ein möglicher moderater Folgeversuch; eine sofortige Kürzung auf 4 G/s ist durch diese Daten nicht als Verbesserung belegt. Zusätzlich die geringe Wirksamkeit von Angriffen gegen aufgebaute Verteidigung untersuchen. Diese Bewertung wurde an den Hauptchat zurückgegeben.

Nachweise: [Konfigurationen und Hashes](/Users/domi/Documents/Handygame/Tools/CoreHarness/Results/EconomyIteration/v1.2/content.csv), [Spielerwerte](/Users/domi/Documents/Handygame/Tools/CoreHarness/Results/EconomyIteration/v1.2/matches.csv), [Zeitpunkte](/Users/domi/Documents/Handygame/Tools/CoreHarness/Results/EconomyIteration/v1.2/milestones.csv), [Messcode](/Users/domi/Documents/Handygame/Tools/CoreHarness/EconomyAnalysis.cs). Die Tabelle wurde aus den CSVs nachgerechnet; eine nachfolgende Queue-Regel benötigt ihre eigene Ergebnisreihe.

### Zweite Messreihe: Queue-Regel auf den bisherigen Karten

Die gesonderte Reihe `v1.4-legacy-maps` enthält weitere 2.160 Matches mit denselben drei Einkommen und bisherigen Karten. In Balanced-Selbstspielen liegt das mittlere ungenutzte Gold bei 25,63 / 26,64 / 28,57; die mittleren Angriffsausgaben bei 2.536,94 / 2.049,71 / 1.566,71 Gold für 6 / 5 / 4 G/s. Die mittlere Dauer bleibt jeweils 480 Sekunden. Der höchste beobachtete Stapel dieser Balanced-Bots wächst von einem auf zwei Aufträge. Der gewählte Bot ersetzt deshalb keinen menschlichen Spam-/Kapazitätstest.

Diese Reihe isoliert die Queue besser als ein gleichzeitiger Kartenwechsel. Sie belegt aber keine Balance der neuen 32-Platz-Karten und keiner späteren Kill-Gold-Regel. Das sind weitere, getrennt zu kennzeichnende Änderungen. Die entsprechenden Daten stehen in [v1.4-Spielerwerten](/Users/domi/Documents/Handygame/Tools/CoreHarness/Results/EconomyIteration/v1.4-legacy-maps/matches.csv).

### Nachträgliche Nutzersteuerung aus dem Gameplay-Feedback-Chat

Der Hauptchat prüft inzwischen eigene Minenplätze, überarbeitete Bots und eine Kombination aus weniger Basiseinkommen und tatsächlichem Kill-Gold. Die Aussagen über fehlendes Kill-Gold und zwölf Plätze oben beschreiben die Ausgangsfassung. Für einen neuen Kill-Ertrag muss die Buchung an genau einen tatsächlichen Tod gebunden sein; Entkommen, entfernte Darstellungen, doppelte Treffer oder ein neu geladener Snapshot sind keine zusätzlichen Kills. Eine Belohnung guter Verteidigung ist außerdem gegen die bereits defensiven Matchverläufe zu prüfen.

### Dritte Messreihe: Kill-Gold auf bisherigen Karten

Die Reihe `v1.5-legacy-bounty` umfasst ebenfalls 2.160 Matches. Sie verwendet laut Metadaten das eingefrorene Botprofil `frozen-v1.4` und die bisherigen Karten. Die folgenden Werte stammen wieder aus jeweils 96 Spielerbeobachtungen in Balanced-Selbstspielen. Alle diese Matches dauern 480 Sekunden.

| Basiseinkommen | Mittleres Kill-Gold pro Spieler | Basis + durchschnittliches Kill-Gold pro Sekunde, ohne Minen | Mittlere Angriffsausgaben | Mittleres ungenutztes Gold |
| --- | ---: | ---: | ---: | ---: |
| 6 G/s | 674,56 | 7,41 G/s | 3.208,60 | 25,73 |
| 5 G/s | 544,65 | 6,13 G/s | 2.592,10 | 26,56 |
| 4 G/s | 414,29 | 4,86 G/s | 1.978,00 | 28,03 |

Damit liegt der Angriffsetat bei **5 G/s plus Kill-Gold** sogar leicht über den vorherigen **6 G/s ohne Kill-Gold** mit 2.536,94 Gold. Eine nominelle Senkung des Basiseinkommens ist hier also keine entsprechende Senkung des tatsächlich verfügbaren Gesamtbudgets. Kill-Gold hängt von Gegner und Verteidigung ab; der durchschnittliche Zusatz ist kein garantierter passiver Ertrag.

Reine Verteidigung gegen Balanced endet bei 6 G/s in 44 von 48 Spielerfällen unentschieden, zuvor waren es 40 von 48. Bei 5 und 4 G/s bleiben es 42 beziehungsweise 36. Diese Probe zeigt keine Auflösung der defensiven Pattsituation. Sie beweist auch keine Balance der neuen Karte oder der freien Platzierung.

Die Goldbilanz aller 4.320 Spielerzeilen wurde unabhängig nachgerechnet: `180 + Basisertrag + Minenertrag + Kill-Gold − Ausgaben − abgeschnittener Ertrag = Endgold`. Die CSV-Spalte `mine_refund` enthält den am Matchende möglichen, noch nicht realisierten Verkaufswert vorhandener Minen; sie ist hier kein zusätzlicher Geldzufluss. Alle Zeilen stimmen mit dieser Definition überein.

Nachweise: [v1.5-Metadaten](/Users/domi/Documents/Handygame/Tools/CoreHarness/Results/EconomyIteration/v1.5-legacy-bounty/content.csv), [v1.5-Spielerwerte](/Users/domi/Documents/Handygame/Tools/CoreHarness/Results/EconomyIteration/v1.5-legacy-bounty/matches.csv). Produktionswerte erst anhand der zusammengeführten Regeln und menschlicher Spielrückmeldung festlegen; keine pauschale Empfehlung „5 ist weniger Gold“ aus dem Basiswert allein ableiten.
