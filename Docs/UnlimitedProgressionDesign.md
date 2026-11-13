# Mengenlimits und Forschung ohne Endstufe

Stand: 11. November 2026. **Entwurf, keine integrierte Regeländerung.**

## Direkter Auftrag und Zuständigkeit

Der Nutzer verlangt kein Baulimit insgesamt oder pro Item und unbegrenzte Forschung. Die Rückfrage zu Truppen wurde ausdrücklich mit „Ja, auch Truppen ohne Mengenlimit“ beantwortet. Hauptchat `01a1c67c-8801-7cf2-9b86-2b5353ef4b76` ist informiert und besitzt die Integration. Der neuere Wunsch ersetzt ein festes Forschungsende bei 8 oder 10. Sichtbare Modell-Meilensteine bis 10 sind weiterhin sinnvoll, aber kein Forschungsmaximum.

Dieser Entwurf ändert weder das laufende Grafik-/Audiofenster 0.9 noch fremde Runtime-Dateien. Arbeitsumfang dieses Chats: dieses Dokument, `Tools/UnlimitedProgressionLab/` und eigene Belege unter `Docs/ResearchEvidence/unlimited-progression-v1/`. Konkrete Runtime-Dateiübergabe wurde beim Hauptchat angefragt.

Gewünschtes Verhalten:

- Gebäude und Minen können gebaut werden, solange Gold und eine geometrisch gültige freie Position vorhanden sind. Keine Stückzahl pro Klasse oder insgesamt.
- Bezahlbare Truppenpakete können ohne Gesamt-, Wellen- oder Warteschlangen-Mengenlimit gekauft werden. Bereits angenommene Pakete bleiben vollständig erhalten und marschieren in FIFO-Reihenfolge aus.
- Forschung bietet auch nach Stufe 8, 10 und darüber hinaus einen wirksamen nächsten Kauf an. Steigende Kosten bleiben; kein „Maximal“-Button nach einer festen Endstufe.
- Wellen, Baupausen, räumliche Abstände, Freischaltungen, Goldkosten und nachvollziehbarer Ausmarschrhythmus bleiben bestehen. „Ohne Mengenlimit“ bedeutet nicht kostenlose Käufe oder alle Einheiten im selben Tick.
- Gebäude-Ausbau besitzt zusätzlich eine eigene Zweikaufgrenze. Sie darf im gemeinsamen Ausbauentwurf nicht übersehen werden; Pfadwirkung und sichtbare Meilensteine müssen mit dem Ansichts-Chat vereinheitlicht werden.

## Ergänzung: Upgradepfade nach Turmtyp

Neuer direkter Nutzerauftrag: Die Upgrades müssen spezifisch zur Funktion des Turms passen. Explizit soll Frost **kein Schadensupgrade** anbieten; Frostdauer wurde als passendes Beispiel genannt und Reichweite ausdrücklich bestätigt. Für den Entwurf wird Frost deshalb mit **Frostdauer / Reichweite** geführt. Der geringe bestehende Basisschaden wird dadurch nicht automatisch entfernt. Diese Vorgabe gilt zusätzlich zur unbegrenzten Forschung.

Folgende Matrix ist ein konkreter Vorschlag für die übrigen Klassen, keine bereits integrierte Balance:

| Klasse | Rollenbezogene Upgradepfade | Sichtbare Wirkung / verständliche Anzeige |
|---|---|---|
| Schnellfeuer | Feuerrate, Reichweite | Kürzere Schusspause; Schüsse/s und Reichweite vorher → nachher |
| Kanone | Explosionsschaden, Explosionsradius | Stärkerer Flächentreffer oder größerer Einschlagsbereich |
| Frost | Frostdauer, Reichweite | Verlangsamung hält länger oder erfasst Ziele aus größerer Entfernung; kein Schadenspfad |
| Panzerbrecher | Panzerbrechende Munition, Reichweite | Wirkung gegen Rüstung als eigener Munitionspfad; Schaden und Rüstungsdurchdringung transparent getrennt ausweisen |
| Goldmine | Förderung | Tatsächlicher Einkommenszuwachs in Gold/s; weder Kampf- noch Reichweitenpfad |
| Blitzspule | Kettenwirkung, Sprungreichweite | Mehr beziehungsweise wirksamere Folgetreffer; Abstand zwischen Kettenzielen klar von Zielerfassungsreichweite unterscheiden |
| Balliste | Durchschlagskraft, Reichweite | Wirkung auf hintereinander stehende Ziele; Anzahl und Schaden der Folgetreffer verständlich ausweisen |

Frostdauer und Froststärke sind verschiedene Größen: z. B. länger dieselbe Verlangsamung ist kein stärkerer Slow. Der Nutzer hat keine zusätzliche Vollvereisung/Stunmechanik verlangt. Weitere Slowstärke- oder Stunpfade werden hier nicht stillschweigend eingeführt.

Technische Umsetzung im späteren Integrationsfenster:

- Pfade im Content pro Klasse explizit definieren und über stabile semantische IDs ansprechen. Der Host verwirft ein Schadensupgrade für Frost auch bei einem direkt konstruierten Netzwerkkommando, ohne Geld oder Level zu ändern. Die Oberfläche allein auszublenden reicht nicht.
- `TowerUpgradePath` besitzt aktuell nur Damage/Range/Income; `TowerState` und Wire speichern DamageLevel/RangeLevel. Der neue Vertrag muss klassenbezogene Pfadlevel und erlaubte Kombinationen durchgängig abbilden. Alte Damagewerte nicht still zu SlowDuration umdeuten; Schema-/Wirewechsel eindeutig behandeln.
- `TowerStats` enthält aktuell keine Slowdauer/-stärke, Explosionseigenschaften oder Kettenparameter. Ein gemeinsamer effektiver Statresolver muss Quote, Vorschau, Kampf, Bot und Snapshotvalidierung versorgen. `MatchSimulation.Hit` liest Slowwerte derzeit direkt aus `TowerDefinition`; eine reine Buttonumbenennung hätte keine Frostwirkung. Auch Splash und MultiHit lesen heute Basiswerte.
- Frostdauer in aktiven Gameplay-Ticks berechnen, im UI in Sekunden zeigen; Baupause und 2× müssen zur bestehenden Spieluhr passen. Erneute Treffer und überlagerte Slows deterministisch prüfen. Ein Frost-Upgrade wirkt auf künftige Treffer; bereits laufende Effekte werden nicht rückwirkend verlängert.
- Feuerrate darf keine Null-Tick-Schusspause erzeugen; zusätzliche Ketten-/Durchschlagziele dürfen keine festen Hitpuffer überlaufen. Pfadfortsetzung nach sichtbarem Meilenstein 10 gehört in denselben Kosten-/Wirkungsresolver wie die unbegrenzte Forschung. Bezahlte Stufen ohne tatsächlichen Nutzen vermeiden; räumliche und zeitliche Größen nicht bloß immer weiter addieren.
- Artmodule werden nach tatsächlichem Pfad benannt: Frost-Kühlreservoir/Dauerkristall für Slowdauer, Zieloptik für Reichweite. Ein generisches Damage-Modul wäre fachlich falsch. Das Ansichts-Lab ist informiert; seine Dateien bleiben bei dessen Owner.

Zusätzliche integrierte Abnahme: Frost-Dauerupgrade verändert SlowUntilTick nach einem neuen Treffer, aber nicht Treffer- oder Flächenschaden; Frost-Reichweitenupgrade ändert nur den vorgesehenen Reichweitenpfad. Unzulässiger Frost-Damage-Befehl ist atomar abgelehnt. UI zeigt echte Vorher-/Nachherwerte und Kosten. Für jede andere Klasse mindestens einen Kampffall prüfen, der genau ihre Spezialwirkung belegt. Laufende Effekte, Snapshot/Join und Bot müssen dieselben effektiven Werte verwenden. Dies sind ausstehende Tests, keine ausgeführten Nachweise.

## Tatsächliche Grenzen im jetzigen Code

| Bereich | Grenze und Quelle | Notwendige zusammenhängende Änderung |
|---|---|---|
| Gebäude | `PlacementValidator`: 30 Kampftürme, 2 Minen, 32 Renderplätze; zusätzlich `TowerDefinition.MaxPerPlayer` | Mengenchecks entfernen, freie IDs/Slots dynamisch vergeben; Geometrieprüfung bewahren |
| Gebäudeausbau | `TowerUpgradeRules.MaxPurchases = 2`, endliche Upgrade-Arrays, dreieckiger VisualVariant-Index | Formel-/Meilensteinresolver statt Arrayzugriff mit beliebiger Stufe; Pfade und Investition erhalten |
| Forschung | `UnitResearchDefinition.MaxLevel = 8`, Konstruktor, Quote und Resolve | Endstufenvalidierung und `ResearchQuote.Maximum` ersetzen; Current/Next anzeigen |
| Kaufplanung | `SendRules`: 240 bezahlter Goldwert je Welle, maximal 80 Aufträge | Keine budgetbedingte Weiterverteilung oder Ablehnung; akzeptierte FIFO ohne Mengenende |
| Aktive Truppen | `MatchSimulation.Spawn`: 80 je Feld und 64 je Herkunft | Keine Besetztmeldung oder Ausmarschblockade allein aufgrund Anzahl; Simulation und Darstellung skalieren |
| Gold | `GameRules.MaxGold = 10000`; Einkommen, Verkauf und Bounty kappen | Progressionsgeld und Kosten gemeinsam skalieren; sonst entsteht ein verstecktes Forschungsende |
| Buchhaltung | `ReservedGoldByWave`, `SpawnedQuarterGoldByWave`, `ValidateLedger` setzen Budget und Queuecap voraus | Zahlung/ausmarschierten Wert weiter exakt prüfen, ohne die alten Mengenobergrenzen |
| Darstellung | `BoardView`: Slot-/View-/Touch-/Pending-Arrays mit 32 Plätzen; Research- und Kaufinfos zeigen Limits | Dynamische wiederverwendbare Views und Touchziele; UI virtualisieren; keine abgeschnittenen Käufe |
| Netzwerk | Wire 6: Snapshot maximal 16.384 Bytes, Entitycounts UInt16, Towerslot und Upgradepfade Byte | Neues versionsgebundenes Streaming-/Pagingformat, größere IDs/Zähler und neue Validatoren |
| Content/Bot/Tests | `GameContent.Validate` und Hash, `Standard.asset`, Bot-Zählziele, zahlreiche Tests verlangen Caps | Schema-/Hashmigration, Botstrategie ohne Spielercap, alte Sollregeln durch neue Akzeptanzfälle ersetzen |

Die beiden Zahlen 240 sind getrennt: Das **Wellenbudget** beschränkt Truppenkäufe und muss entfallen. Der **Divisor für zusätzliches Angriffseinkommen** bestimmt dessen Rate. Er wird nicht versehentlich mitgelöscht. Auch das aktuelle +3-Gold/s-Angriffseinkommensmaximum ist eine Wirtschaftsregel, kein Kaufmengenlimit; eine Änderung daran gesondert im gemeinsamen Balancing festhalten.

## Umsetzungsvorschlag für den Hauptchat

### Kauf und Wellen

`SendQuote` bestimmt die Zielwelle anhand Phase und FIFO, ohne Kapazitätssuche: in Preparing/Active die aktuelle Welle, in Clearing die nächste. Keine angenommene Sendung wird gelöscht oder wegen eines neuen Limits abgewiesen. Nach dem Annahmeschluss läuft die aktuelle Welle leer; neue Käufe gehen in die nächste Welle. In der letzten Clearing-Phase ist keine weitere Zielwelle vorhanden: das ist das Matchende und muss so angezeigt werden, nicht als „Kontingent voll“. Die Anzahl der Wellen wird durch diesen Auftrag nicht verändert.

Pakete behalten ihr beim Kauf gebundenes Rezept, ihren tatsächlich gezahlten Preis und ihre FIFO-Position. Benachbarte identische Pakete dürfen nur mit derselben Welle, Forschung und Preis zusammengefasst werden. Dabei pro Einzelpaket weiterhin Kaufcooldown, Paketgrenzen und exakte anteilige Einkommensgutschrift beachten; bloßes Multiplizieren von InitialCount/Remaining würde die bestehende Semantik ändern. Sequenzquittungen verhindern Doppelabbuchungen. Schutz vor ungültigen Netzwerkkommandos bleibt bestehen; normale Bedienung darf dadurch keinen verborgenen Tages-/Wellen-/Gesamtmengendeckel erhalten.

### Endlose Progression und Zahlen

Nur `MaxLevel` zu entfernen ist falsch: Die bestehenden quadratischen Preise werden bald größer als das Wallet; später laufen Int32-Kosten und Stats über. Vorschlag: exakte skalierbare Integer für Forschungslevel, Geld, Kosten und wachsendes HP-/Schadens-/Einkommensbudget. `System.Numerics.BigInteger` ist ein zu prüfender Kandidat; IL2CPP, Wirekodierung und Vergleichskosten sind vor Integration zu verifizieren. Ein größeres festes MaxLevel oder ein stilles Sättigen aller Werte wäre kein erfüllter Auftrag.

Räumliche Werte getrennt behandeln: Reichweite und Bewegung müssen zur endlichen Karte passen. Beliebig weiter erforschbare Stufen müssen weiterhin einen tatsächlichen Nutzen haben, auch wenn Reichweite oder Geschwindigkeit sich einer sinnvollen Grenze nähern. Der konkrete Kosten-/Wirkungsresolver ist mit dem Ausbauentwurf abzugleichen; bis dahin ist hier keine neue Balanceformel verbindlich. Bestehende PaidPvP-Rezepte bleiben nach späterer Forschung unverändert.

Modelle brauchen nicht für jede Zahl eine neue Meshvariante. Eine endliche Folge deutlich sichtbarer Ausbaumeilensteine plus exakte Stufenanzeige kann beliebig viele Regelstufen darstellen. Die Darstellung darf den Regelwert nicht zurücksetzen oder begrenzen.

### Skalierbare Darstellung und Übertragung

Views nach Entity-ID verwalten, Pools bei Bedarf vergrößern und nicht sichtbare Effekte reduzieren. Das darf keine simulierten Einheiten oder Kaufaufträge vernichten. Größere Armeen benötigen räumliche Zielsuche statt dauerhaft jeden Turm mit jeder Einheit zu vergleichen. Die Zahl gleichzeitig gezeichneter Detailmodelle darf nicht die Kauf- oder Simulationskapazität definieren.

Wire 6 kann größere Zustände nicht übertragen. Einzelne Transportpakete bleiben begrenzt, aber der gesamte gültige Zustand muss in geordnete Seiten/Abschnitte zerlegt werden können. Revision, Epoch, Vollständigkeit und atomare Übernahme müssen Join, Reconnect und Revanche abdecken. Nicht einfach MaxSnapshotBytes erhöhen: auch Byte-Slots, UInt16-Gesamtlänge, Byte-Chunkzahl, Reassembly, Welcome und Vergleichstools passen bisher nur zum alten Format. Verbrauchslimits für unvollständige oder ungültige Netzwerkdaten sind Schutzmaßnahmen; sie dürfen nicht als neues verborgenes legitimes Truppenlimit eingebaut werden. Überlast darf keine bereits bezahlten Käufe lautlos verlieren.

## Abnahme für die spätere integrierte Version

1. Auf legalen Positionen 31. Kampfturm, 3. Mine und mehr als 32 Gesamtgebäude bauen, auswählen, ausbauen und verkaufen; identische Klassen erlaubt. Geometrie- und Goldfehler bleiben korrekt, keine Doppelabbuchung bei wiederholtem Kommando.
2. Mit ausreichendem Geld 81 und mehr unterschiedliche FIFO-Aufträge, mehr als 240 Goldwert in einer Welle und mehr als 80 aktive Einheiten pro Feld testen; Herkunftsgrenze 64 ebenfalls überschreiten. Keine verlorenen Pakete, konsistente Cooldowns und Kaufrezepte.
3. Forschung 8→9, 10→11, 100→101 und oberhalb alter Integer-Preisschwellen: positive exakte Kosten, tatsächlicher Nutzen, kein Überlauf und keine künstliche Max-Stufe. Ein Kauf bei ungenügendem Geld verändert nichts.
4. Forschungsaufträge mit veraltetem Quote, Forschungsfortschritt während wartender Truppen, Einkommensreste und Revanche prüfen. Kein nachträgliches Aufwerten alter Pakete, keine Gratis- oder Doppelzahlung.
5. Snapshot größer als 16.384 Bytes und Slots oberhalb 255 vollständig übertragen; Join/Reconnect sowie doppelte, umsortierte und fehlende Seiten prüfen. Keine teilweise übernommene Welt. Alte Wireversion eindeutig ablehnen.
6. UI zeigt Current/Next, lesbare große Geldwerte und scroll-/virtualisierbare FIFO. Keine Beschriftung „max. 2“, „30/30“, „8/8“ oder „Gegnerfeld voll“ als Mengenregel.
7. Hauptchat führt integrierte Core-/Net-/PlayMode-Tests, LAN/Relay und Builds aus. Auf Android große Armeen und viele Bauobjekte mit Frametime-/Speichermessung prüfen; keine praktische Unbegrenztheit aus einem kleinen Desktoptest ableiten.

## Bereits erreichter Status und reproduzierbarer Beleg

Quellprüfung und arithmetische Gegenbeispiele sind vorhanden. Ausführen ohne Unity:

```sh
python3 Tools/UnlimitedProgressionLab/audit_limits.py
```

Der Beleg unter `Docs/ResearchEvidence/unlimited-progression-v1/limit-audit.json` bindet die gelesenen Core-/Net-Dateien per SHA-256. Er zeigt, wann das alte Wallet weitere Forschung verhindern würde und wann die bestehende Snapshotgrenze bei mehr Aufträgen überschritten wird. Dies sind **keine ausgeführten Runtime-, Netzwerk- oder Androidtests** und kein Test neuer Spielregeln.

Status: **Entwurf erstellt → Integration offen → Runtimeprüfung offen → neuer Build offen → nicht gestartet.** Vorhandene 0.8- und laufende 0.9-Artefakte liefern diesen Wunsch noch nicht. Der Hauptchat vergibt das eigene Regel-/Content-/Wirefenster; keine automatische Hintergrundüberwachung zugesagt.
