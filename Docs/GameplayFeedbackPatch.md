# Gameplay-Feedback vom 10. November 2026

Direkter Testauftrag: Platzierung zuverlässiger, klarere Turmbilder und Buttons, eigene Minenplätze, aktiverer Bot, Abschussgold statt hoher automatischer Einnahmen, sichtbare Leben und Truppengeschwindigkeit, Bauten unten, Angriffe rechts und eine Gegner-Vorschau mit laufenden Truppen.

## Abgestimmte Zuständigkeiten

Der Hauptchat „Entwickle Defense Duell für Android“ koordiniert Integration und Builds. Dieser Patch besitzt ausschließlich `Assets/Core/BotBrain.cs`, `Tools/BotFeedbackHarness/` und dieses Dokument. Andere Chats/Agents bearbeiten Core-Regeln, UI, Karten und Assets. Keine parallelen Unity-Prozesse oder Änderungen am laufenden Vorschau-Build durch diesen Patch.

| Wunsch | Vereinbarte Umsetzung | Verantwortung |
| --- | --- | --- |
| Zuverlässige Platzierung | Drag-Vorschau und eindeutige gültige Ziele; Mehrfinger-/Release-Fälle prüfen | Hauptchat/UI |
| Turmbilder und Knöpfe | Vier erkennbare Turmklassen, separate Mine, passende Klassenbilder und klare Preise | Hauptchat/UI, Asset-Recherche |
| Eigene Minenplätze | 32 Gesamtplätze: 30 Kampfplätze und 2 nur für Minen; autoritative Prüfung | Core, Karten, UI |
| Mehr Bot-Angriffe | Erster Turm, unmittelbar danach Angriff; wiederkehrende Angriffe auch beim Sparen | Dieser Bot-Patch |
| Geld für Abschüsse | Kandidaten je getöteter Einheit: Läufer 4, Sprinter 5, Tank 14, Schwarm 2; nie für Durchbrüche | Core |
| Weniger automatisches Geld | 4 gegen 6 Gold/s mit Abschussgold messen; Produktionswert zentral festlegen | Hauptchat/Core |
| Leben und Geschwindigkeit | Bestehende 30 Basisleben sichtbar; vorhandene Geschwindigkeiten 65/115/37/58 deutlich beschriften | UI |
| Anordnung | Bauleiste unten, Truppen rechts; bestätigte Warteschlange und Preise sichtbar | UI |
| Gegner-PiP | Live-Gegnerkarte mit eigenen vorrückenden Truppen; keine Bedienkollision mit eigenem Feld | UI |

Die Parameter für Abschussgold und Einkommen sind getrennte Balanceentscheidungen. Die Bot-Probe unten ist kein Nachweis einer fertigen Gesamteconomy oder einer Android-Geräteprüfung.

## Botänderung

- Die alte feste Liste von zwölf Slotnummern entfällt. Der Bot durchsucht alle tatsächlichen Plätze und fragt `Map.CanBuildAt(slot, economy)` ab.
- Kampfpositionen werden nach erreichbarer Weglänge bewertet. Ein geringer Bonus bevorzugt bei vergleichbarer Abdeckung frühere Wegabschnitte. Die Bewertung wird pro unveränderlichem Inhaltsobjekt einmal berechnet.
- Minen nutzen die ausdrücklich markierten Plätze. Auf unveränderten Legacy-Karten mit gemischten Plätzen nehmen sie die für Schnellfeuertürme schwächsten Positionen.
- Balanced und Economy kaufen nach dem ersten Turm einen Angriff. Nach zehn Sekunden ohne angenommenen Angriff wird ein bezahlbarer Angriff vor dem nächsten Sparziel versucht. Das ist eine Priorität bei der nächsten Botentscheidung, keine garantierte Truppenerzeugung bei leerem Konto oder voller Queue.
- Gebäudekäufe und Upgrades nach dem ersten Turm lassen bei gemischten Bots mindestens den Preis der günstigsten Truppe übrig. Dadurch verhindert ein gerade gekauftes Gebäude nicht den nächsten fälligen Angriff.
- Ein geplanter Turmkauf bleibt auch nach Zwischenangriffen gespeichert. Die reservierten Zehn-Sekunden-Angriffe nutzen die günstigste Klasse, damit sie das Sparziel nicht durch wiederholte teure Tankkäufe aufbrauchen. Gewöhnliche Angriffe bleiben gemischt.
- Ist eine zufällig bevorzugte Truppe zu teuer, probiert der gemischte Bot die anderen Klassen. RunnerRush und TankRush behalten ihre spezialisierte Auswahl; PureDefense/FrostDefense bleiben reine Verteidigungsstrategien.
- Alle Käufe laufen durch dieselben validierten Befehle, Preise, FIFO-Grenzen und Ausmarschtakte wie menschliche Eingaben. Es gibt keine zusätzlichen Ressourcen für den Bot.

## Prüfplan und Nachweise

`Tools/BotFeedbackHarness` prüft dieselben Seeds auf allen drei Karten und beiden Spielerseiten bei 4 sowie 6 Gold/s. Ein untätiger Gegenspieler isoliert Angriffspausen beim Sparen; das ist bewusst kein ausgeglichenes Bot-Selbstspiel. Jede Partie läuft parallel als deterministische Wiederholung. Erfasst werden angenommene Angriffspakete, enthaltene Einheiten, erster Angriff, längste Kaufpause, Bauten und Gold.

Zusätzliche reich finanzierte Testpartien prüfen, dass PureDefense über die alten zwölf Plätze hinaus baut und Economy beide dedizierten Minenplätze nutzt. Diese künstliche Finanzierung gilt ausschließlich für die Platzierungsprüfung.

Ausführung mit einem .NET-8-SDK:

```sh
dotnet run --project Tools/BotFeedbackHarness -c Release -- --verify
```

Optional erlaubt `-p:BaselineBot=/absoluter/pfad/BotBrain.cs` einen Vergleich mit einer gesicherten alten Botdatei unter denselben aktuellen Regeln. Ohne `--verify` werden nur Messdaten ausgegeben.

Prüfstatus: Gegen Core v1.5 kompiliert; alle 48 Messfälle mit deterministischer Wiederholung und alle drei kombinierten Platzierungsproben bestanden. Erster Angriff spätestens nach 0,6 Sekunden. Längste Pause zwischen angenommenen Angriffskäufen: 10,5 Sekunden bei beiden Einkommen. Die Prüfschwelle beträgt zwölf Sekunden, um den getakteten Botentscheidungen Spielraum zu lassen. Es handelt sich um Kaufabstände; die FIFO-Ausmarschregel bleibt separat aktiv.

| Einkommen | Alte/neue Pakete in 120 s | Alte/neue Einheiten in 120 s | Alte/neue maximale Kaufpause | Alte/neue Kampftürme |
| --- | --- | --- | --- | --- |
| 4 Gold/s | 14,75 / 15 | 16,25 / 17,25 | 31,9 / 10,5 s | 3 / 3 |
| 6 Gold/s | 21,5 / 24,75 | 23,75 / 27,75 | 21,7 / 10,5 s | 3 / 3 |

Paket-, Einheiten- und Turmzahlen sind Mittelwerte der jeweils 24 Proben. Bei 6 Gold/s baut der neue Bot außerdem in jeder Probe eine Mine. Die alte Botversion findet auf den dedizierten Minenfeldern keine gültige Mine; dieser Fehler ist Teil der Migration. Keine Schlussfolgerung über allgemeine Matchbalance aus dem untätigen Gegner ableiten.

Rohdaten: `Tools/BotFeedbackHarness/Results/baseline.csv`, `patched.csv`, `summary.json`. Die verglichenen Inhalts-Hashes sind je Fall identisch. Baseline-Bot: Commit `2d41be6`, Datei `Assets/Core/BotBrain.cs`, SHA-256 `c139cf4914e0b5a008e6b4de0bf7f5f43b45b8f4527966040a04b70483e9961f`.

Der Test fand und beseitigte zwei Zwischenregressionen: ausgehendes Angriffsgold direkt nach einem Gebäudekauf und durch Zwischenangriffe verlorene Turmsparziele. Ein zusätzlicher Verteidigungs-Check verhindert, dass ein Bot mit nur einem Turm die Druckprüfung besteht. Die endgültigen Proben enden sämtlich mit drei Kampftürmen.

## Folgeauftrag: Zusammenarbeit und freie Platzierung (v1.6)

Der Nutzer hat die aktive Übernahme offener Aufgaben aus den anderen Chats beauftragt. Hauptchat, Mapchat, Recherchechat und Ansichts-Chat wurden kontaktiert. Die abgestimmte Ergänzung dieses Arbeitsbereichs ist die Botmigration auf freie Koordinaten sowie die gezielte Gefahrenprüfung für Minen. Gelände, UI, Core-Validator und Netzwerk bleiben bei den jeweiligen Owners.

Kampfbauten verwenden nun `Target=-1` plus echte `PositionX/PositionY`; Minen verwenden ihre autoritative Site-ID. Der Bot durchsucht intern Punkte im Abstand von 40 logischen Einheiten und bewertet die tatsächliche Wegabdeckung. Diese Suchstrategie ist ausschließlich Botlogik: Menschliche Positionen werden durch den Core geometrisch geprüft und nicht auf dieses Raster beschränkt. Auch der Bot benutzt `PlacementValidator.ValidatePosition` und normale Kaufbefehle. Pfadproben und Bewertungen werden pro unveränderlichem Content gecacht; eine Position ohne Wegabdeckung ist für den Bot uninteressant.

Die Gefahrenprüfung berücksichtigt nur lebende Gegner auf dem eigenen Feld, die mit ihrer ungebremsten Klassengeschwindigkeit die Basis innerhalb von zehn Sekunden erreichen könnten. Ihre Basis-Schadenswerte werden summiert; mehr als drei verhindern die Investition. Mindestens 80 Prozent der Startleben und 15 Sekunden ohne beobachteten neuen Basisschaden bleiben Voraussetzung. Die ungebremste Geschwindigkeit überschätzt bei verlangsamten Gegnern bewusst die unmittelbare Gefahr.

### Isolierte Gefahrenmessung vor der Positionsmigration

`Results/FreePlacement/active-before.csv` und `active-threat-candidate.csv` im Bot-Harness vergleichen dieselben Inhalts-Hashes, 5 Gold/s und die neuen Geschwindigkeiten 104/184/59/93. Die Karten haben in diesem Vergleich noch feste Plätze. Je zwölf aktive Economy-gegen-Balanced-Partien laufen über drei Karten, zwei Seeds und vertauschte Seiten.

- Vorher: vier Partien ohne Economy-Mine, acht mit einer Mine. Der zuvor aus einem anderen Zwischenstand gemeldete Befund „keine Mine“ trifft auf diesen finalen v1.5-Bot nicht pauschal zu.
- Neues Gefahrenmodell: in allen zwölf Partien zwei Economy-Minen. Mittlere Economy-Angriffspakete steigen von 143,83 auf 165,5.
- Diese Änderung stärkt die Economy-Strategie. Sie ist keine Aussage über menschliche Spielbalance; der Hauptchat bekommt die Daten für seine gemeinsame Balanceentscheidung.

### Zusätzliche Regressionen

Der Harness prüft nun zusätzlich 5 Gold/s (insgesamt 72 reguläre Proben) und echte Positionsdaten im deterministischen Replay. Freie Platzierungsfixtures prüfen identische, unabhängige Botfelder und die Fälle entfernte Kolonne, nahe Tanks, niedrige HP, frischer Basisschaden sowie Erholung nach der Investitionspause. Diese Fälle sind bereits bestanden. Die vollständig integrierten MapCatalog-Sperrflächen werden nach Freigabe durch den Map-Owner gesondert geprüft; die bisherigen v1.5-Slotnachweise werden dadurch nicht rückwirkend zu Nachweisen freier Platzierung.

### Abnahme auf den integrierten freien Karten

Die anschließende Prüfung verwendet alle drei `MapCatalog`-Karten mit `FreePlacement=true` und ihren tatsächlich gespeicherten Sperrrechtecken. Bestanden: 72 reguläre Proben plus 72 deterministische Wiederholungen; pro Karte 30 legale freie Kampfbauten und zwei Minen; mindestens ein Kampfbau außerhalb der alten Slotkoordinaten; drei unabhängige Spiegelprüfungen; zwölf Gefahrenfixtures einschließlich anschließender Erholung nach Basisschaden. Die längste Angriffskaufpause in den regulären Proben beträgt 10,55 Sekunden.

Zwölf vollständige aktive Economy-gegen-Balanced-Partien mit 5 Gold/s und Produktionsgeschwindigkeiten endeten nach 455,4 oder 480 Sekunden. Alle Economy-Spieler bauten zwei Minen und gewannen ihre Partie. Alle sechs getauschten Seitenpaare lieferten identische Ressourcen-, Minen-, Angriffs- und Dauermesswerte. Durchschnittlich kaufte Economy 168,17 Angriffspakete, Balanced 146,5. Der Sieg in allen zwölf Partien wurde als Balancehinweis an den Hauptchat gemeldet. Diese kleine Strategiestichprobe bleibt eine Verhaltensprüfung, kein umfassender Balancebeleg.

Nachweise unter `Tools/BotFeedbackHarness/Results/FreePlacement/`: `free-placement-regression.csv`, `active-free-placement.csv`, `verification.json`. Das Manifest enthält Inhalts-Hashes je Karte, Bot-Quellhash und Harness-Binärhash. Die v1.5-Vorher-Messung bleibt separat erhalten. Keine Unity-Prozesse oder Builds im Hauptprojekt durch diesen Arbeitsbereich; Render-/Netzwerk-/Android-Integration bleiben beim Hauptchat.

## Begrenzter Balancevergleich: 96 Partien

Auf Folgeauftrag des Hauptchats wurde ausschließlich der Test-Harness erweitert. BotBrain, Preise, Einkommen, Tempo und Regeln blieben unverändert. Economy trat gegen Balanced, RunnerRush, TankRush und PureDefense an: drei finale Karten, vier Seeds und vertauschte Seiten ergeben 24 Partien je Gegner, insgesamt 96. Verwendet wurden die tatsächlichen Produktionsdefaults (5 Gold/s, Geschwindigkeiten 104/184/59/93, freie Platzierung und Sperrflächen).

| Gegner | Economy Siege / Niederlagen / Remis | Mittlere Dauer | Mittlere HP Economy / Gegner | Mittlere Minen Economy / Gegner | Mittlere Angriffspakete Economy / Gegner |
| --- | --- | --- | --- | --- | --- |
| Balanced | 24 / 0 / 0 | 477,95 s | 28,25 / 14,08 | 2 / 0,92 | 162,50 / 146,08 |
| RunnerRush | 24 / 0 / 0 | 210,49 s | 25,92 / 0 | 1,83 / 0 | 37,83 / 67,67 |
| TankRush | 20 / 4 / 0 | 160,06 s | 3,25 / 0,17 | 0 / 0 | 35,92 / 14,42 |
| PureDefense | 0 / 0 / 24 | 480 s | 30 / 30 | 2 / 0 | 134 / 0 |

**Einordnung:** In dieser begrenzten Botstichprobe gibt es keine verlässlich siegende offensive Gegenstrategie. TankRush erzeugt deutlich stärkeren frühen Druck: Economy baut gegen ihn keine einzige Mine und beendet die Partien mit durchschnittlich nur 3,25 Leben. Die vier Niederlagen betreffen je ein Seed-Seitenpaar auf Sonnenpass und Frosthafen; auf Grünes Tal gewinnt Economy alle acht Partien. Da in diesen Paarungen keine Mine gebaut wird, lassen sich die verbleibenden Economy-Siege nicht mit Minenerträgen erklären – verglichen werden vollständige Botstrategien.

Reine Verteidigung verhindert jeden Basisschaden und erzwingt 24 Zeitlimit-Remis bei 30:30 Leben. Das ist eine defensive Antwort, kein Sieg und kein Nachweis spannender Partien. Auch gegen Balanced enden 22 von 24 Partien am Zeitlimit. Der Hauptchat erhält deshalb sowohl den weiterhin starken Economy-Befund als auch die Neigung zu langen, festgefahrenen Spielen. Änderungen an Preisen oder Botregeln wurden ausdrücklich nicht vorgenommen.

Alle 48 Seitenpaare stimmen in sämtlichen normalisierten Messwerten überein. Die drei Content-Hashes entsprechen der vorherigen freien-Platzierungsprüfung; während des Laufs änderte sich keine erfasste Core- oder Harness-Quelldatei. Die getauschten Seiten sind gepaarte Beobachtungen, keine zusätzlichen unabhängigen Seeds. Vier Seeds je Karte und Gegner sowie regelbasierte Bots ersetzen keine menschlichen Tests.

Reproduktion: `dotnet run --project Tools/BotFeedbackHarness -c Release -- --bounded-balance`.

Nachweise: `Tools/BotFeedbackHarness/Results/BoundedBalance/matches.csv` (96 Einzelpartien inklusive beider Minenausbauten, Angriffspakete und Einheiten), `summary.csv`, `verification.json` sowie `source-hashes-before.json`.

## Folgeintegration Core v1.7: freie Minen und Upgradeauswahl

Nach Sicherung des Desktop-Checkpoints `9a509f2` hat der Hauptchat dieses Botpaket ausdrücklich freigegeben. Es ändert ausschließlich `Assets/Core/BotBrain.cs`, den separaten Bot-Harness und die zugeteilten Dokumente. Gemeinsame Upgrade-Regeln, Content, Wire, Darstellung und Builds bleiben bei den anderen Owners. Die folgenden Nachweise gelten für v1.7 und überschreiben keine v1.6-Ergebnisse.

### Implementierte Botänderungen

- Alle Gebäude auf FreePlacement-Karten, einschließlich Mine, verwenden `Target=-1`, `SiteId=-1` und tatsächliche Koordinaten. Die internen Suchpunkte werden durch denselben `PlacementValidator` geprüft. Es gibt keine alten Erzplatzlisten oder Minenreservierungen. Minen bevorzugen gültige Flächen mit geringer Wegabdeckung, damit sie gute Turmflächen möglichst freihalten.
- Beide Minen werden bei ausreichendem Budget über `TowerUpgradeRules.Quote(..., Income)` linear aufgewertet. Kampftürme holen ebenfalls die verbindlichen Damage-/Range-Quotes; ein lokaler Index auf alte Gesamt-Upgradewerte entscheidet nicht mehr über den Kauf.
- Bei einem lebenden eigenen Feldgegner im aktuellen Radius bevorzugt der Bot Schaden. Ohne solches Ziel wählt er Reichweite, wenn die gewichtete abgedeckte Weglänge um mindestens 25 Prozent wächst; bei bisher null Abdeckung genügt erstmalige positive Abdeckung. Andernfalls wählt er Schaden. Die Vorschauwerte stammen aus der gemeinsamen Quote. Gegner auf dem anderen Spielfeld beeinflussen diese Entscheidung nicht.
- Zehn-Sekunden-Angriffspriorität, günstige Zwischenangriffe, Mindestreserve für Angriffe, persistente Turmsparziele sowie das Gefahrenfenster für Minen wurden beibehalten. Keine Änderungen an HP, Preisen, Goldzufluss, Geschwindigkeiten oder Limits durch dieses Botpaket.

### Tatsächlich bestandene Prüfungen

`dotnet run --project Tools/BotFeedbackHarness -c Release -- --verify` kompiliert gegen die integrierten v1.7-Core-Dateien und besteht:

- 72 reguläre Fälle bei 4/5/6 Gold/s mit jeweils vollständiger deterministischer Wiederholung, nun einschließlich `DamageLevel` und `RangeLevel`.
- Auf allen drei Karten 30 gültige Kampfbauten sowie zwei frei gebaute, voll auf Income II ausgebaute Minen ohne Site-ID. Keine Mine verwendet eine alte feste Baukoordinate.
- Drei gespiegelte Kampf- und drei gespiegelte Minenprüfungen: dieselbe Position auf dem anderen Feld blockiert nicht, eigene Gebäude kollidieren weiterhin korrekt. Die beiden eigenen Minen haben unterschiedliche gültige Standorte.
- Zwölf gezielte Gefahrenfälle und drei Erholungsfälle nach frischem Basisschaden; die bisherigen Investitionssperren bleiben wirksam.
- Fünf gezielte Upgrade-Policy-Fixtures: Range II ohne unmittelbare Bedrohung bei klarer Zusatzabdeckung, Damage II gegen ein eigenes Ziel im Radius, Mischpfad, Ignorieren von Einheiten auf dem anderen Feld sowie korrektes Ansparen und Bezahlen des Quote-Preises. Die Verteidigung dieser Fixtures wurde über reguläre bezahlte Befehle aufgebaut; die veränderten Bedrohungs-/Goldlagen sind ausdrücklich kontrollierte Testbedingungen.

Die längste Angriffskaufpause bleibt 10,55 Sekunden. Die gemessenen Idle-Kennzahlen der 72 Fälle sind gegenüber dem archivierten v1.6-Lauf unverändert. Eine Quote allein erlaubt dem Bot keinen kostenlosen Kauf.

### Begrenzter Vergleich mit dem gesicherten 0.3-Bericht

Unveränderte Matrix: vier Gegner, drei finale Karten, vier Seeds, Seitentausch; insgesamt 96 Partien. Beide Ausrichtungen werden in normalen Partien genutzt. Die 48 normalisierten Seitenpaare stimmen in allen erfassten Messwerten überein.

| Gegner | v1.6 → v1.7 Economy Siege/Niederlagen/Remis | Mittlere Dauer v1.6 → v1.7 | Economy-Angriffspakete v1.6 → v1.7 |
| --- | --- | --- | --- |
| Balanced | 24/0/0 → 24/0/0 | 477,95 → 480 s | 162,50 → 160,92 |
| RunnerRush | 24/0/0 → 24/0/0 | 210,49 → 210,49 s | 37,83 → 37,92 |
| TankRush | 20/4/0 → 20/4/0 | 160,06 → 160,06 s | 35,92 → 35,92 |
| PureDefense | 0/0/24 → 0/0/24 | 480 → 480 s | 134 → 134 |

Gegen Balanced erreicht Economy nun im Mittel 3,42 Schadens- und 1,5 Reichweitenstufen über seine Kampftürme. Gegen PureDefense bleiben alle Partien bei 30:30 Leben im Zeitlimit; dieses bekannte Problem ist durch die neue Upgradewahl nicht gelöst. Gegen TankRush werden weder Minen noch Upgrades gekauft, sodass der dort unveränderte Ausgang keine Aussage über die Stärke des neuen Upgradesystems ist. Der Vergleich erfasst das kombinierte Folgepaket mit freien Minen und separaten Upgrades; er isoliert nicht kausal die Wirkung jeder Einzeländerung.

Nachweise: `Tools/BotFeedbackHarness/Results/v1.7/` mit `regression.csv`, `balance-matches.csv`, `balance-summary.csv`, `comparison-v16-v17.csv`, `source-hashes-before.json` und `verification.json`. Quellen blieben während des Balancelaufs unverändert; Manifest und CSVs enthalten die drei neuen Content-Hashes und den Bot-Quellhash. Keine Unity-Prozesse und keine Build-/Netzwerk-/Darstellungsfreigabe durch diese Prüfungen.
