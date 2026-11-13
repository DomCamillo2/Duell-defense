# Grundwellen und zusätzliche Spielerangriffe

11. November 2026. **Status: abgestimmter Entwurf zur Zusammenführung durch den Hauptchat, nicht implementiert oder als Spielversion freigegeben.** Ausgangspunkt ist 0.6 / Core v1.8, Checkpoint `590a183`. Der Kartenchat besitzt diesen Entwurf und sein isoliertes Analysemodell. Core, Netzwerk, HUD, Terrain, Content und Builds wurden nicht verändert. Verbindlicher Projektstatus bleibt `PROJECT_STATE.md`.

## Nutzerauftrag und empfohlener erster Spieltest

Auf beiden Feldern kommen automatisch zunehmend schwierige Gegnerwellen. Spieler können jederzeit bauen und durch bezahlte Truppensendungen die Verteidigung des Gegners zusätzlich belasten. Zwischen abgeschlossenen Wellen gibt es eine kurze Vorbereitungszeit mit automatischem Start und einer Möglichkeit zum früheren Start. Die genannten 100 Wellen und fünf beziehungsweise zehn Sekunden sind Beispiele des Nutzers, keine bereits beschlossenen Zahlen. Kanonenpflicht für bestimmte Gegner ist eine gesonderte optionale Idee.

**Empfehlung: zunächst ein endliches Duell mit zehn Wellen und zehn echten Sekunden Vorbereitung vor jeder Welle.** Im Duell verkürzt nur beidseitige Bereitschaft die Pause; im Solo kann der lokale Spieler starten. Bauen, Ausbauen, Verkaufen und erlaubte Forschungen bleiben auch im Kampf möglich. Diese Defaults benötigen die zusammenhängende Vertragsentscheidung des Hauptchats und anschließend echte Spieltests.

Wellenzahl gehört in die Leveldefinition. Die drei Biome können zunächst dieselbe Zehnerfolge verwenden. Ein 100-Wellen-Level wäre ein eigener Langmodus mit eigener Economy-, Forschungs- und Dauerprüfung. Die bestehende Acht-Minuten-Siegregel darf nicht zusätzlich unbemerkt eine Partie mitten in der Wellenfolge beenden.

## Eindeutiger Ablauf und Wellenende

| Unterphase | Gegner und Zeit | Erlaubte Aktionen |
|---|---|---|
| Vorbereiten | Keine aktiven Gegner oder Spawns. Zehn Sekunden echte Planungszeit, unabhängig von 1×/2×. Einkommen, Angriffscooldowns, Forschungs-/Fähigkeitszeit und sonstige Spielzeit stehen. | Bauen, Ausbauen, Verkaufen, Forschung, bezahlte Angriffe vormerken, Bereitschaft setzen. |
| Welle | Grundwellen und zugeordnete Spieleraufträge marschieren aus; Einkommen und Kampf laufen in Spielzeit. Das Sendefenster dieser Welle ist offen. | Alle normalen Bau-/Kaufaktionen. Ein neuer Angriff wird nach verbindlicher Reservierung dieser oder einer späteren Welle zugewiesen. |
| Aufräumen | Das Sendefenster der laufenden Welle ist geschlossen. Bereits fest zugeordnete Aufträge und noch ausstehende Grundwellen-Spawns werden vollständig abgearbeitet; Gegner werden besiegt oder laufen durch. Einkommen und Spielzeit laufen weiter. | Bauen und Käufe bleiben erlaubt. Neue Angriffskäufe können ausschließlich verbleibenden späteren Wellen zugeordnet werden. |

Der Kandidat setzt das Sendefenster auf `max(12 s, letzter planmäßiger Grundwellen-Spawn + 4 s)` aktive Spielzeit seit Wellenbeginn. Grundwellen sollen alle 0,5 Spielsekunden eine einzelne Einheit anfordern. Das Fenster hängt nicht davon ab, wie schnell Gegner getötet werden; schnelles Abräumen beendet die Angriffsmöglichkeit des anderen Spielers nicht überraschend. Nach dem Fenster können noch längst nicht alle Gegner beseitigt sein.

**Eine Welle endet erst, wenn auf beiden Feldern sämtliche Gegner dieser Welle und alle ihr zugeordneten Grundwellen-/PvP-Spawns erledigt sind.** Aufträge für spätere Wellen verhindern das Ende nicht. Es gibt keinen festen Kampftimer, der lebende Einheiten löscht. Die gemeinsame Vorbereitung beginnt erst danach. Wer schneller verteidigt, wartet auf das andere Feld und darf weiter bauen. Das Interface benennt diesen Zustand.

Endlichkeit entsteht durch begrenzte Wellenpläne, begrenzte zugeordnete Spielerangriffe und den Kauf-Cutoff. Im vorhandenen System haben Einheiten positive Geschwindigkeit, Verlangsamung fällt nicht unter 60 %, und es gibt keine endlose Wegschleife. Neue Betäubungs-/Rückstoß-/Heilregeln müssen diese Annahme später ausdrücklich neu prüfen. Längeres Aufräumen kann Einkommen für beide Spieler verlängern; Slow-Stalling bleibt deshalb ein menschlich zu prüfender Balancepunkt. Keine zusätzliche Vergütung allein fürs Warten.

## Bezahlte FIFO: feste Zuteilung statt verlorener Käufe

Der zuerst untersuchte Kandidat von zwölf **insgesamt je Welle ausmarschierenden Körpern** wird nach dem Klassenvergleich nicht empfohlen. Er war kein Limit gleichzeitig lebender Gegner und hätte sich nach einem Abschuss nicht erneuert.

Der neue Startkandidat reserviert **höchstens 240 Gold tatsächlichen Truppenkaufwert je Spieler und Welle**. Das ist eine Ausmarschbegrenzung, keine zweite Währung, keine kostenlose Geldzuteilung und keine zweite Abbuchung. Preise bleiben zunächst 18/24/65/38. Nicht genutzte Reservierung einer beendeten Welle verfällt; es gibt dafür weder Gold noch zusätzliches Guthaben für später.

1. Vor der Abbuchung werden Phase, Preis, Gold, maximal 80 wartende Aufträge und ein passender endlicher Zielwellenplatz geprüft.
2. Eine komplette gekaufte Gruppe erhält die früheste noch zulässige Welle mit genügend Kaufwertkapazität, höchstens Welle zehn. Der Reservierungshorizont umfasst ausdrücklich auch mehrere spätere Wellen, nicht nur aktuelle/nächste Welle. Im offenen Fenster ist das die aktuelle Welle, sonst frühestens die nächste. Innerhalb einer FIFO darf ein späterer Kauf keinen früheren Auftrag überholen; seine Zielwelle ist mindestens die des vorherigen Auftrags.
3. Preis, Forschungsstand, Gruppengröße, Herkunft, Zielwelle und Statdefinition werden beim Kauf eingefroren. Bestehende Feldtruppen und ältere Aufträge werden nicht nachträglich verbessert. Forschung selbst ist kein Truppenkaufwert.
4. Eine Gruppe bleibt in ihrer Zielwelle, auch wenn sie in mehreren Spawns ausmarschiert. Die Klassenabstände und der Abstand zwischen Einzelspawns bleiben erhalten. Erledigter Ausmarsch gibt die Wellenreservierung **nicht erneut frei**.
5. Wenn eine Welle voll ist, zeigt die Kaufvorschau die tatsächliche spätere Zielwelle, etwa „Ausmarsch Welle 4“. Das kann mehr als „nächste Welle“ sein. Bezahlte Aufträge werden nie still verworfen oder neu bepreist. Die konkrete Zielwelle muss VOR dem Kauf sichtbar sein und als bestätigte Erwartung im Command stehen. Berechnet der Host inzwischen eine andere Zielwelle, lehnt er ohne Abbuchung/Reservierung ab; die UI aktualisiert die Vorschau für einen neuen Kauf. Keine stille Mehrwellen-Vorbestellung. Ein identischer Request-Replay liefert nur die vorhandene Quittung und reserviert niemals ein zweites Mal; geändertes Payload mit derselben Sequenz wird abgewiesen.
6. Wenn bis einschließlich letzter Welle kein Platz vorhanden ist, wird der Kauf ohne Abbuchung abgelehnt. In der letzten Aufräumphase sind neue Angriffskäufe entsprechend nicht mehr möglich. Beim normalen Überleben bis zum Levelende bleiben keine zugeordneten bezahlten Angriffe unausgeführt.
7. Eine vorzeitige Niederlage/Abbruch beendet wie bisher das Match; daraus entsteht kein zusätzlicher Ertrag für noch wartende Käufe. Ein Abbruch ist kein durchgelaufenes Levelende.

| Reine Klasse | Gesamtbudget zwölf Körper: Kaufwert | Kandidat 240 Kaufwert: Gruppen / Körper / Kaufwert |
|---|---:|---|
| Läufer | 216 | 13 / 13 / 234 |
| Sprinter | 288 | 10 / 10 / 240 |
| Tank | 780 | 3 / 3 / 195 |
| Schwarm | 114 | 6 / 24 / 228 |

Die Enumeration aller erreichbaren gemischten Kaufwert-/Körperzustände ergibt bei den aktuellen Preisen höchstens 24 PvP-Körper pro Welle. Die neutralen Wellen enthalten höchstens 48: somit können in dieser Konfiguration selbst bei keinerlei Abschüssen höchstens 72 Einheiten je Feld zu dieser Welle gehören. Das ist eine Kapazitätsaussage, keine Zusage, dass Spieler das maximale Budget finanzieren können. 240 Kaufwert bleibt ein Balanceparameter; bei späteren Preis-/Gruppengrößenänderungen müssen diese Rechnung und die Vergabe neuer Aufträge erneut geprüft werden. Bei vollständiger Vernichtung der reinen Belegungen entstehen 52/50/42/48 Gold PvP-Kopfgeld für den Verteidiger (Läufer/Sprinter/Tank/Schwarm). Ähnliche Kaufwerte oder Bounties beweisen keine gleiche Kampfkraft.

## Fairer deterministischer Ausmarsch

Grundwellen besitzen einen eigenen Plan und eine eigene Herkunft `NeutralWave`. Sie sind keine kostenlosen Käufe eines Spielers und benötigen keinen dritten Spieler. Spieleraufträge tragen `PaidPvP`, ihren echten Sender und die Zielwelle. Die neutralen Pläne sind auf beiden Feldern identisch; Unterschiede entstehen erst durch Verteidigung und gegnerische Angriffe. Private Forschung verändert ausschließlich neu gekaufte PvP-Truppen.

Pro Feld gilt weiterhin die gemeinsame Obergrenze von 80 lebenden Einheiten. Als allgemeine Reservierung für spätere, größere Pläne gelten höchstens 64 gleichzeitig lebende Einheiten derselben Herkunft. Damit kann eine Herkunft die andere nicht vollständig aus dem Feld verdrängen. Die Reservierung ist bewusst nicht ausleihbar. Der aktuelle Zehnerkandidat erreicht diese Grenzen ohnehin nicht.

An jedem erlaubten Ausmarschzeitpunkt – höchstens ein Einzelspawn je Feld alle vier Spielticks – wird die nächste berechtigte Quelle gewählt:

- Wenn beide Quellen bereit sind und Platz haben, wechseln sie sich ab. Nach jedem tatsächlichen Spawn hat die andere Quelle Vorrang beim nächsten gemeinsamen Konflikt.
- Wenn nur eine Quelle bereit ist, erhält sie den Slot. Ein wartender Klassenabstand der PvP-FIFO blockiert nicht die neutrale Quelle.
- Innerhalb einer Quelle wird niemals überholt. Neutrale Einheiten stehen in einer deterministischen, klassenweise verschachtelten Reihenfolge mit planmäßigen Zeitpunkten. PvP behält gekaufte FIFO, Klassenabstände und vollständige Gruppen.
- Ein volles Feld oder Herkunftslimit verschiebt einen Spawn. Es löscht weder einen Auftrag noch verbraucht es den ersten tatsächlichen Klassenausmarsch oder erzeugt einen Einkommensbonus.

Die Simulation, Herkunftszähler, Priorität, Planpositionen und Klassenabstände werden hostautoritativ geführt. Clientdarstellung zählt keine Spawns selbst hoch.

## Vorbereitungszeit, Bereitschaft und Tempo

Die vorgeschlagenen zehn Sekunden laufen als echte unskalierte Kontrollzeit; alle Gameplaytimer einschließlich Bruchteil-Einkommen bleiben in der Vorbereitung stehen. Die verbleibende Fraktion wird erhalten und nicht bei jeder Pause auf eine ganze Auszahlung aufgerundet. Dadurch bringt Abwarten kein zusätzliches Gold, keine kostenlose Cooldown-Erholung und keinen Tickvorsprung. Pause ist nicht gleich Netzwerkstillstand: Heartbeat, Verbindungsfristen und Rate-Limits bleiben aktiv. Bei angehaltenem Gameplay-Tick darf der Befehlslimiter nicht für die gesamte Pause festhängen.

Bereitschaft ist eine ausdrückliche Zustimmung für genau `(SessionId, WaveId)`, keine umschaltende globale Taste. Einseitige Zustimmung startet im Duell nicht früher; beide Zustimmungen erlauben den Übergang. Automatisch wird spätestens mit Ablauf der echten Pause gestartet. Alte oder doppelte Bereitschaft darf niemals eine spätere Welle überspringen. Beide Stimmen werden bei jedem Wellenwechsel zurückgesetzt. Lokales Pending ist keine bestätigte Bereitschaft.

Im Solo übernimmt der vorhandene Koordinator die Botzustimmung entsprechend dem bestätigten lokalen Frühstart. Bei 2× beschleunigen aktive Welle und Aufräumen gemeinsam; Vorbereitung bleibt 1×. Die bestehenden Tempovoten können bestehen bleiben, die effektive Beschleunigung hängt zusätzlich von der Wellenphase ab. Keine neuen Wandzeit-Catch-up-Ticks beim Wechsel. Revanche setzt sämtliche Wellen-, Plan-, Bereitschafts- und Economy-Zustände zurück.

## Startfolge und Schwierigkeitszunahme

Kandidat: dieselbe Folge auf beiden Seiten; Zahlen zählen **einzelne Körper**, nicht gekaufte Viererschwarm-Gruppen. Die Reihenfolge innerhalb einer Welle wird deterministisch aus den Klassenmengen verschachtelt. HP-Multiplikator gilt nur für neutrale Gegner, ganzzahlig aufgerundet; neutraler Basis-Schaden, Tempo und Rüstung bleiben zunächst klassenbezogen wie bisher. Die Folge ist für eine Erstprüfung vorbereitet, nicht als bereits spannend oder ausgeglichen nachgewiesen.

| Welle | Läufer | Sprinter | Tank | Schwarmkörper | Gesamt | Neutrale HP |
|---:|---:|---:|---:|---:|---:|---:|
| 1 | 10 | 0 | 0 | 0 | 10 | 100 % |
| 2 | 12 | 4 | 0 | 0 | 16 | 112 % |
| 3 | 12 | 4 | 0 | 4 | 20 | 124 % |
| 4 | 12 | 4 | 2 | 6 | 24 | 136 % |
| 5 | 10 | 8 | 2 | 8 | 28 | 148 % |
| 6 | 10 | 8 | 4 | 10 | 32 | 160 % |
| 7 | 10 | 10 | 4 | 12 | 36 | 172 % |
| 8 | 10 | 12 | 6 | 12 | 40 | 184 % |
| 9 | 10 | 14 | 8 | 12 | 44 | 196 % |
| 10 | 8 | 14 | 10 | 16 | 48 | 208 % |

Spezielle Bosse und absolute Immunitäten gehören nicht in diese erste Vertragsänderung. Spätere Kontergegner benötigen erkennbare Merkmale, angekündigte Zusammensetzung und erreichbare Gegenmittel. Die aktuelle Schadensformel garantiert mindestens einen Schaden; „nur Kanonen“ wäre eine eigenständige neue Kampfregel und ist nicht durch höhere Rüstung bereits umgesetzt.

## Economy und Forschung: abgestimmter Anschluss

Abgleich mit Economy-Chat und Truppenupgrade-Chat am 11. November:

- Neutrale Wellen kosten keinen Spieler Gold und erzeugen **kein Sendekauf-Einkommen**. Erster gemeinsamer Kandidat: **null Kopfgeld für neutrale Abschüsse**, um nicht parallel einen unbudgetierten neuen Geldstrom einzuführen. Eine spätere neutrale Prämie benötigt ein festes Wellenbudget und eine Gegenrechnung des regelmäßigen Einkommens.
- PvP-Kopfgeld bleibt zunächst 4/5/14/2 je tatsächlich besiegter Einheit; Leaks zahlen kein Kopfgeld. Neutrale und PvP-Kills brauchen eindeutige Herkunft auch im Zustandsdigest und Snapshot.
- Nur tatsächlicher Ausmarsch eines bezahlten PvP-Kaufs aktiviert anteilig Sendekauf-Einkommen. Der Economy-Kandidat ist eingesetzter Kaufwert / 240, insgesamt höchstens +3/s. Er ist kein abschließend akzeptierter Balancewert. Der Rendite-Nenner 240 und das Wellen-Ausmarschbudget 240 sind ausdrücklich zwei unabhängige Konfigurationen: Bei einem Renditevergleich mit Nenner 180/300 darf das Wellenbudget nicht automatisch mitgeändert werden. Bei 240 Deployment-Kaufwert kann eine volle Welle höchstens +1/s vor dem Gesamtdeckel neu freischalten. Viererschwarm zählt nach Kaufwertanteil, nicht vierfach.
- Sendebonus und Rest bleiben über Wellen erhalten; Reset nur mit neuem Match/Level. Forschungskosten, Bereitschaft, ACK, neutrales Erzeugen und wartende spätere Käufe aktivieren keinen Bonus.
- Forschung wird separat gekauft. Ein Auftrag friert die gültigen klassenbezogenen Stats und Kosten ein. Neutrale HP-Skalierung verwendet einen eigenen Definitionspfad; keine doppelte Multiplikation mit Spielerforschung oder 2×.
- 240 Sekunden Amortisation sowie vorgeschlagene Minenamortisationen von 200/320 Sekunden müssen gegen **verbleibende aktive Spielzeit nach tatsächlichem Ausmarsch** beurteilt werden. Wartende spätere Wellen und echte Pausen zählen nicht. Insbesondere ist nicht jeder späte Ausbau wirtschaftlich sinnvoll.

Referenzen: `Docs/ResearchEvidence/EconomySendIncomeProposal-2026-11-11.md`; separate Truppenupgrade-Analyse des Designchats. Änderungen von Basis-/Minenraten werden nicht durch diesen Wellenentwurf beschlossen.

## Dauerabschätzung statt Versprechen einer festen Partielänge

Alle Zeiten unten sind ohne ursprünglichen Lobby-Countdown, mit zehn Sekunden voller Vorbereitung vor jeder Welle. Früheres beidseitiges Starten verkürzt sie; 2× muss tatsächlich während der aktiven Phasen vereinbart sein.

| Wellen | Angenommene aktive Zeit je Welle | Gesamt bei 1× | Gesamt bei 2× |
|---:|---:|---:|---:|
| 8 | 40 s | 6:40 min | 4:00 min |
| 10 | 20 s | 5:00 min | 3:20 min |
| 10 | 40 s | 8:20 min | 5:00 min |
| 10 | 60 s | 11:40 min | 6:40 min |
| 12 | 40 s | 10:00 min | 6:00 min |
| 100 | 20 s | 50:00 min | 33:20 min |
| 100 | 40 s | 83:20 min | 50:00 min |
| 100 | 60 s | 116:40 min | 66:40 min |

Diese Tabelle ist eine Sensitivitätsrechnung, keine Vorhersage. Tatsächlich braucht ein ungebremster Tank auf den heutigen Wegen allein 68,44–76,81 Spielsekunden vom Eingang bis zur Basis. Bei dauerhaft maximaler aktueller Verlangsamung sind es 114,07–128,02 Sekunden. Langsame letzte Gegner oder spätes Senden können eine Welle erheblich verlängern; volle Wege sind nicht mit der typischen Zeit bis zum Abschuss gleichzusetzen. Basiszerstörung kann die Partie früher beenden.

Ein zusätzliches isoliertes Zeit-/FIFO-Modell entfernt jede Einheit nach bewusst vorgegebenen zehn oder 25 Sekunden, ohne echten Turmkampf. Bei der empfohlenen Zehnerfolge ergibt es:

- Ohne PvP 5,73 beziehungsweise 8,23 Minuten bei 1×, volle Pausen eingeschlossen.
- Bei jeder Welle maximal gefülltem Kaufwertbudget, Käufen unmittelbar vor Fensterende und reiner Klasse 7,63–11,21 Minuten bei 1× beziehungsweise 4,65–6,44 Minuten bei 2×. Finanzierung wird dabei absichtlich nicht vorausgesetzt oder bewiesen.
- Unter identischen 25-Sekunden-Annahmen streut das verworfene Zwölf-Körper-Budget von 9,73 Minuten für Schwärme bis 15,38 Minuten für Tanks. Der Kaufwertkandidat liegt näher zusammen: 10,13–11,21 Minuten.

Deshalb zehn Wellen als erster endlicher Test, Zielbereich grob acht bis zwölf Minuten bei 1× unter gemischten aktiven Angriffen. Dieser Zielbereich ist **kein hartes Zeitlimit und keine garantierte Dauer**. Erst ein integrierter Kampf-/Botvergleich plus menschliche Partien kann Anzahl, HP, Fenster und Budget festlegen. 100 Wellen werden nicht mit den selben ungetesteten Langzeitwerten veröffentlicht.

## Niederlage, letzte Welle und Gleichstand

Basiszerstörung beendet die Partie vorzeitig. Die Auflösung beider Felder eines autoritativen Ticks erfolgt vor der Siegentscheidung; bei beidseitigem Verlust im selben Tick gilt Unentschieden. Keine Bevorzugung durch die Reihenfolge der Felditeration.

Nach dem vollständigen gemeinsamen Abräumen der letzten Welle gewinnt als Vorschlag der Spieler mit mehr verbleibenden Basis-HP. Gleiche HP ergeben Unentschieden. Kein ungefragtes Sudden Death und kein Sieger allein aufgrund schnellerer Abschüsse. Außerhalb eines definierten Kampfticks werden keine weiteren Spawns, Goldzahlungen oder Forschungseffekte ausgelöst. Die reine HP-Regel kann bei starker beidseitiger Verteidigung zu vielen Remis führen; das ist ein ausdrücklicher Punkt der späteren Balanceprüfung.

## Benötigter gemeinsamer Vertrag und HUD

Core/Content benötigen versionierte Wellenpläne, aktuelle Unterphase, WaveId, echte Pausendeadline, Bereitschaft je Spieler, planmäßige und verbleibende Spawns, feste Zielwellen-/Kaufwertreservierungen, Quellenzuordnung und unveränderliche effektive Truppenstats. Kein zusätzlicher Spieler-Slot. Abgeleitete Zähler müssen im Validator konsistent sein. Konkrete DTOs, Commandnummern und Versionssprünge legt der Hauptchat fest; bestehende 0.6-Nachweise gelten nicht automatisch.

Snapshots müssen einen beitretenden/resynchronisierten Client eindeutig derselben Welle und Phase zuordnen; relevante Felder, Slots, Reihenfolge, Budget, Quellen und Statwerte gehören in Hash/Digest und strenge Grenzen. Der Host entscheidet Start und Ende; Countdownanzeigen sind keine lokale Simulation. Alte Sitzungen/Versionen und falsche WaveIds werden abgewiesen. Revanche ist ein kompletter Wellenreset.

Mit dem Ansichts-Chat abgestimmter Darstellungsbedarf:

- `Welle X / 10`, Vorbereiten / Welle / Aufräumen, echter Countdown und getrennte Bereitschaften.
- Vorschau der bekannten nächsten **Grundwelle**, einschließlich Typen und HP-Stärke. Gegnerische gekaufte FIFO bleibt verborgen.
- Eigene bezahlte FIFO mit Zielwelle, Menge, eingefrorener Forschungsstufe und Wartegrund; bei ferner Zuteilung konkrete Wellenzahl statt irreführendem „sofort“.
- Vor Kauf verständliche Restkapazität für die Welle; bei endgültig vollem Level keine Abbuchung und klarer Sperrgrund.
- Bauen in allen Unterphasen; Kampf- und Vorbereitungszustand nicht mit der bisherigen Lobbybereitschaft verwechseln. Zielprioritäten sind ein separater kleiner Interaktivitätskandidat; eine Frostfähigkeit gehört noch nicht zu diesem Paket.

## Nachweise, Abnahme und Übergabe

Ausgeführt: `python3 Tools/WaveDesignSandbox/analyze_waves.py`. Ergebnis: `Docs/ResearchEvidence/wave-design-v1.json`, mit Quellenhashes und Harnesshash. **15.752 Assertions im isolierten Python-Entwurfsmodell**, keine produktiven Core-/Unity-/Netzwerktests. Geprüft sind unter anderem unveränderte Ablehnungen, vollständige Gruppenzuteilung, FIFO ohne Vorziehen, Endwellenbudget, 80er-Auftragsgrenze, eingefrorene Forschungsreferenz, einfache WaveId-Bereitschaft, verbindliche erwartete Zielwelle ohne stille Umbuchung, identische/abweichende Request-Replays ohne Doppelreservierung, deterministische Quellenwechsel, synthetischer Kapazitätsstress und endliches Leeren. 100 fest gesetzte Zufallsfolgen prüfen Kaufreservierungen. Die synthetischen Kapazitätsfälle überschreiten bewusst den kleinen Zehnerkandidaten, um die allgemeinen Herkunftsgrenzen zu belasten.

Noch im tatsächlichen Core nachzuweisen: echte Phasen-/Echtzeitdeadline bei 1×/2×, keine Pause-Economy-/Cooldown-Exploits, volle Stat-/Preis-Snapshots, keine doppelte Bereitschaft oder Auszahlung, keine verloren bezahlten Aufträge, beide Seiten / Seitentausch, gemeinsame letzte Welle und gleichzeitiger Basisverlust, Revanche, LAN-/Relay-Snapshotgleichheit, HUD-/Pointerzustände und menschliche Kampfbalance. Das isolierte Modell enthält keine Schadensermittlung, Forschungswirkung, Basis-HP-Auflösung, echten Goldzufluss oder Finanzierung der Belastungsszenarien.

Geänderte Dateien dieses Auftrags: nur dieser Entwurf, `Tools/WaveDesignSandbox/analyze_waves.py` und dessen JSON-Ergebnis. Hauptchat ist alleiniger Integrations-/Release-Owner. Keine neue laufende Vorschau, kein Spielbuild und keine automatische Hintergrundüberwachung aus diesem Entwurf entstanden.
