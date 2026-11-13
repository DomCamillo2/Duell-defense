# Freie Turmplatzierung

Stand: 10. November 2026. Direkter neuer Nutzerauftrag im Recherche-Chat: Kampfgebäude frei platzieren statt vorgefertigter Plätze; mit den zuständigen Chats abstimmen und gemeinsam umsetzen. Dieser Auftrag ersetzt die bisherige Bindung der Kampfgebäude an zwölf bzw. 30 vorgegebene Bauplätze. [DefenseDesignResearch.md](DefenseDesignResearch.md) bleibt als historische Untersuchung erhalten.

**Neuer Folgeauftrag vom selben Tag:** Auch Goldminen sollen auf frei gewählten legalen Positionen gebaut werden können. Die nachfolgenden Aussagen über feste Minenplätze, Minenreservierungen und Protokoll 3 beschreiben den geprüften Stand **Core v1.6 / Version 0.3**. Sie sind keine Vorgabe gegen den neuen Nutzerwunsch. Der Hauptchat integriert freie Minen zusammen mit dem neuen HUD in das Folgepaket; der jeweils tatsächliche Integrationsstand steht ausschließlich in [PROJECT_STATE.md](../PROJECT_STATE.md) und [CONTRACT.md](CONTRACT.md).

Für diese Erweiterung müssen Mine und Kampfturm auf freien Karten dieselbe frei gewählte Position und dieselben Hindernis-/Weg-/Abstandsregeln benutzen. Leere frühere Minenstandorte dürfen keine reservierten Flächen mehr sperren; Erzdekoration darf keine vermeintlich verpflichtenden Bauplätze suggerieren. Das bisherige Limit von zwei gleichzeitig gebauten Minen bleibt die abgestimmte Ausgangsannahme, weil der neue Auftrag freie Orte verlangt und kein neues Mengenlimit nennt. Ein höheres Limit wäre eine getrennte Balanceänderung. Terrain, Bot, Wire-Übertragung, Snapshotprüfung und Minen-Kaufvorschau müssen die neue Semantik gemeinsam übernehmen.

Die unten aufgeführten 194 Assertions und 17.978 Geometrieproben sowie `Tools/FreePlacementAudit` sind **v1.6-Nachweise mit festen Minen**. Ihre Quellhashes bleiben erhalten. Sie belegen die v1.7-Erweiterung nicht; dafür werden nach dem API-Freeze neue, versionsgebundene Nachweise benötigt. Ebenso sind damalige Unity-Zahlen historische Läufe, keine pauschale Prüfung des laufend veränderten Quellstands.

## Spielentscheidung und Umfang

Freie Platzierung passt zum bisherigen Duell: Der Spieler entscheidet genauer, welche Wegstücke ein Turm erreicht und wie er Platz für weitere Türme lässt. Der feste Gegnerweg, identische Karten für beide Seiten, Angriff und Verteidigung aus demselben Goldbudget sowie die bestehenden Klassen bleiben erhalten. Gebäude versperren oder verändern den Weg nicht. Es entstehen weder Maze-Pathfinding noch Gelände-Kampfboni.

Die zwei zuvor ausdrücklich gewünschten Minenstandorte bleiben zunächst separat. Die bisherige maximale Ausbaumenge von 30 Kampftürmen und zwei Minen ist die Ausgangsgrenze für den Vergleich. Mehr frei wählbare Positionen sollen nicht unbeabsichtigt unbegrenzt viele Gebäude erlauben. Das ist eine Integrationsannahme zur Bewahrung des bisherigen Umfangs, keine neu gemessene Balanceempfehlung.

Frei bedeutet: Der Spieler wählt eine echte logische Position auf bebaubarem Boden. Eine feine Ganzzahlauflösung zur Netzwerkübertragung ist zulässig; heimliches Einrasten auf die alten 30 Positionen erfüllt den Auftrag nicht. Die alten Punkte dürfen noch als Botkandidaten oder Vergleichsbeispiele dienen.

## Gemeinsame Bauregel

Core definiert einen Gebäude-Fußabdruck und prüft denselben Zustand für Vorschau, Bot und verbindlichen Kauf:

- Der gesamte Fußabdruck liegt innerhalb der spielbaren Karte.
- Er hält den definierten Abstand zu jedem Wegsegment einschließlich Ecken und Endpunkten ein. Wegbreite und Gebäuderadius werden beide berücksichtigt.
- Er überlappt keine tatsächliche Sperrfläche und keinen reservierten Minenstandort.
- Er überlappt kein anderes Gebäude auf dem eigenen Spielfeld; das spiegelgleiche Gebäude des Gegners darf die eigene Position nicht sperren.
- Klasse, Mengenlimit, Gold, Matchphase und Session sind gültig. Eine Ablehnung bucht kein Gold ab.

Die konkreten Radien und Grenzkonventionen gehören in eine einzige versionierte Definition. Darstellung und Tests dürfen keine anderen Zahlen einsetzen. Abstände werden an logischen Koordinaten geprüft, nicht an zufälligen Mesh-Collidern. Vor Multiplikationen Koordinaten begrenzen und hinreichend breite Zahlentypen verwenden. Grenzfälle unmittelbar auf sowie eine Einheit neben Weg-, Karten- und Gebäudegrenzen sind Bestandteil der Prüfung.

**Abgestimmter Core-v1.6-Vertrag:** `PlacementValidator` prüft einen kreisförmigen Gebäudefuß mit Radius 30, mindestens 70 Einheiten Abstand zur Wegmitte, mindestens 60 zwischen Gebäudemittelpunkten und 30 zum Kartenrand. `MapDefinition` ergänzt `FreePlacement` und unveränderliche rechteckige `BlockedAreas` aus `MapRectangle(MinX, MinY, MaxX, MaxY)`. Diese sind eine einfache erste Darstellung von Sperrflächen, keine neue Landschaftsphysik. Der Map-Owner richtet die große sichtbare Dekoration daran aus.

Eine reine Flächenprüfung darf von der vollständigen Kaufprüfung getrennt sein: Terrain braucht beispielsweise keine Goldmenge. Die gemeinsame Geometrie bleibt dabei identisch. Platzierung weit vom Weg kann zulässig sein; null Reichweitenabdeckung soll ehrlich sichtbar werden, statt den Spieler auf versteckte Empfehlungspunkte zu zwingen.

## Bedienung

1. Turm aus der Bauleiste ziehen oder antippen und auf den Boden tippen.
2. Der echte Fußpunkt folgt der Eingabe; ein versetztes Geistermodell darf die Sicht verbessern. Reichweite und Gültigkeit beziehen sich auf den Fußpunkt.
3. Gültigkeit über Farbe und verständlichen Grund zeigen: etwa „Weg freihalten“, „Zu nah am Gebäude“, „Hier nicht bebaubar“ oder „Nicht genug Gold“.
4. Gültiger Drop bzw. die gewählte Tap-Bestätigung sendet genau einen Kaufauftrag. Außerhalb, über HUD oder auf der Gegneransicht abbrechen.
5. Ausstehende Position anzeigen, bis der Host entscheidet. Erst der bestätigte Zustand erzeugt das verbindliche Gebäude. Für Upgrade und Verkauf bestehende Gebäude per Entity-ID auswählen.

Die laufende Geste und der ausstehende Kauf haben getrennte Lebenszyklen. Zweiter Finger, Abbruch, Pause, Disconnect, Revanche, Boardwechsel und Safe-Area-/Layoutwechsel dürfen keine alten Drops wiederholen. Ein nachlaufendes Click-Ereignis nach Drag darf keinen zweiten Command senden. Ausstehende eigene Gebäudeflächen werden in der lokalen Vorschau berücksichtigt; der Host entscheidet bei zeitlich konkurrierenden Käufen abschließend.

Die aktuelle Darstellung verwendet eine geneigte Kamera und eine RenderTexture. Bildschirmkoordinaten müssen über das tatsächliche Boardrechteck und dessen UV-Abbildung in einen Strahl der Dioramakamera überführt und mit der logischen Bodenebene geschnitten werden. Einfache lineare Bildschirm-X/Y-Skalierung reicht für diese Kameradarstellung nicht. `ScreenPointToLocalPointInRectangle` bestätigt nur den Ebenentreffer, nicht das Liegen im Rechteck; dieses wird zusätzlich geprüft. [Unity 6.3: Rechteckumrechnung](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/recttransformutility/screenpointtolocalpointinrectangle), [ViewportPointToRay](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/camera/viewportpointtoray)

## Karten und Terrain

Die drei handgestalteten Wege benötigen für dieses Feature keine zufällige Neugenerierung. Die Landschaftserzeugung muss jedoch von „alte Sockel freihalten“ auf zusammenhängende nutzbare Flächen umgestellt werden. Kampfsockel werden nicht mehr als verpflichtende Ziele gezeichnet; unter einem tatsächlich gebauten Turm kann ein Fundament entstehen.

Sichtbare Felsen, Wasser oder unzugängliche Klippen dürfen nicht zugleich grün bebaubar sein. Dafür entweder gemeinsame logische Sperrflächen rendern oder große Dekoration konsequent außerhalb bebaubarer Flächen erzeugen. Niedrige Grastextur darf rein dekorativ bleiben. Baumkronen und schräge Projektion dürfen weder Weg noch freie Bauflächen verdecken. Beim Terrain-Generator prüfen, ob genügend zusammenhängende Flächen für die bisherige Ausbaumenge bleiben und die Minen erreichbar sind.

## Daten, Netzwerk und Bot

Der verbindliche Turmzustand braucht die echte Position. Bau-Command, Wiederholungsprüfung, Snapshot, Snapshotvalidierung, Content-Hash und Protokollversion müssen die neue Semantik gemeinsam abbilden. Ob der Core separate X/Y-Felder oder eine eindeutig begrenzte verlustfreie Kodierung verwendet, ist eine Implementierungsentscheidung des zuständigen Owners. Der Client darf die Position niemals aus einer alten Slotnummer erraten.

Abgestimmt sind `CommandEnvelope.PositionX/PositionY`, `TowerState.Position` und `SiteId`. Freie Kampftürme senden `Target=-1` plus echte Position; Minen senden ihre feste Standort-ID und erhalten die Position aus der Kartendefinition. `Slot` bleibt ausschließlich eine interne Render-ID von 0 bis 31. `PlacementValidator.ValidateBuild(..., out TowerPlacement)` liefert `Position/Slot/SiteId`; `ValidatePosition(..., ignoredTowerId=-1)` prüft Geometrie ohne Kaufzustand. Netzwerk migriert auf Protokoll 3. Eine Render-ID ist kein Beleg für fortbestehende vorgefertigte Bauplätze.

Snapshotprüfung darf keinen aufgrund von aktuellem Gold oder aktueller Matchphase ungültigen Kauf simulieren: Sie prüft die Plausibilität bereits vorhandener Gebäude. Sie muss trotzdem Kartenbegrenzung, tatsächliche Position, Menge, Eigentümer und unzulässige Überlappung erkennen. Ein anderer alter Build muss beim Versionsabgleich verständlich abgewiesen werden.

Der Bot bewertet deterministisch erzeugte Kandidaten anhand ihrer echten Wegabdeckung und benutzt denselben Core-Validator. Eine endliche Bot-Suchmenge beschränkt nicht die freien menschlichen Positionen. Vorhandene Angriffsrhythmik, Sparvorhaben und dedizierte Minen bleiben erhalten.

## Zuständigkeiten und Abnahme

Der Haupt-Chat integriert und koordiniert Core, Netzwerk, Contentmigration und Builds. Der Map-Chat besitzt Terrain und Kartengeometrie; der Feedback-Chat besitzt BotBrain. Die UI-Entwicklung besitzt Presentation, der Ansichts-Chat prüft Erreichbarkeit und Projektion; der Asset-/Drag-Chat prüft Gesten und Auftragslebenszyklus. Dieser Recherche-Chat besitzt dieses Dokument und unabhängige Geometrie-/Integrationsreview. Keine parallelen Änderungen an fremden Runtime-Dateien.

| Prüfung | Erwartung |
| --- | --- |
| Neue freie Punkte | Mehrere akzeptierte Positionen liegen nicht auf alten Slots; gespeicherte Position entspricht dem gewählten Punkt |
| Wege und Grenzen | Horizontale, vertikale, diagonale Segmente, Knicke, Endpunkte, Kartenrand, Hindernisse und Minenreserve korrekt |
| Gebäude | Gleiches Team kollidiert; gegenüberliegender Spieler darf dieselbe Position nutzen; Upgrade erhält Position; Verkauf gibt Fläche frei |
| Zahlung und Wiederholung | Ungültiger Drop kostenlos; gültiger Command genau einmal bezahlt; gleiche Request-ID mit veränderter Position erzeugt keinen zweiten Bau |
| Netzwerk | Position nach Encode/Decode exakt gleich; malformed/überlappende Zustände abgelehnt; Host-/Clientdarstellung stimmen überein |
| Mobile Projektion | Hin- und Rückprojektion auf allen Karten in 16:9 und 20:9 einschließlich Safe Area; keine Käufe über PiP oder UI |
| Bedienung | Drag und Tap, Abbruch, zweiter Finger, ausstehender Kauf, ACK vor/nach Snapshot, Verkauf und Sessionwechsel funktionieren |
| Bot und vollständige Partie | Legale freie Bauten und weiterhin regelmäßige Angriffe; Matchende und Revanche funktionieren |
| Darstellung | Keine alten verpflichtenden Kampfpads; sichtbare Hindernisse passen zu Bauregeln; keine Türme schweben oder stehen im Fels |

## Nachgewiesener Stand

Der neue Core-/Netzwerkstand und die drei Karten wurden in eine separate Quellkopie übernommen und ohne Unity kompiliert: null Fehler, null Warnungen. Der unabhängige [Abnahme-Harness](../Tools/FreePlacementAudit/Program.cs) besteht **194 Assertions**. Nachgewiesen sind echte Positionen außerhalb der alten Slotliste, voneinander unabhängige Spielerfelder, unveränderte Position nach Upgrade, freigegebene Fläche nach Verkauf, Minenstandorte, Baugrenzen, genau einmal bezahlte Wiederholungen, genaue Command-/Snapshotübertragung sowie Ablehnung fehlerhafter Zustände. Jede der drei Karten konnte in der Prüfung 30 Kampftürme und zwei Minen aufnehmen; ein zusätzlicher Kampfturm wurde ohne Zahlung abgewiesen.

Die separate [Python-Referenz](../Tools/FreePlacementAudit/verify_geometry.py) berechnet Segmentabstände mit exakten rationalen Zahlen. Sie stimmt an **17.978 Punkten** auf den drei echten Karten und einer künstlichen Grenzfallkarte vollständig mit dem Core überein. Diese Punkte umfassen Raster- und Zufallsproben sowie gezielte Rand-/Rechteckfälle; ihre Annahmequote ist keine Schätzung des bebaubaren Flächenanteils.

Eine [nicht vollständige Abdeckungsstichprobe](ResearchEvidence/free-placement-coverage-sample.json) zeigt auch einen konkreten taktischen Unterschied: Auf Sonnenpass erreicht ein einfacher Schnellfeuerturm am legalen freien Punkt `(597, 569)` etwa 538 Wegeinheiten gegenüber 450 am stärksten früheren festen Kampfplatz. Die Stichprobe bestimmt kein globales Optimum und simuliert keinen Kampf. Insbesondere die neuen Sperrflächen und Platzierungsregeln unterscheiden sich vom alten Vergleich. Daraus werden keine automatischen Werteänderungen abgeleitet; es ist ein Ansatzpunkt für den nächsten menschlichen Spieltest.

Die [Messdatei](ResearchEvidence/free-placement-audit.json) enthält die getesteten Definitionen, alle Proben und SHA-256 der tatsächlich kompilierten Core-/Netzwerkdateien. Bei Abschluss dieses Durchlaufs stimmten sämtliche aufgezeichneten Hashes noch mit dem gemeinsamen Projekt überein. Der getestete MapCatalog-Hash lautet `969493dfb8ebdc63413f7af7e7d570057e365bdd8122e08c2a03d325a1dbea2f`.

Reproduktion mit einem lokalen .NET-8-SDK: `dotnet run --project Tools/FreePlacementAudit/FreePlacementAudit.csproj -- /absoluter/pfad/Handygame /tmp/free-placement-audit.json`, danach `python3 Tools/FreePlacementAudit/verify_geometry.py /tmp/free-placement-audit.json`. Für konsistente Belege während paralleler Entwicklung vorher die benötigten Quellen einfrieren. Das optionale Argument `--fixture-only` prüft unabhängig von noch nicht migrierten Karten die synthetischen Core-/Netzwerkfälle.

Die getrennte [UI-/Projektionsabnahme](MapHudAudit.md) besteht ebenfalls: 318 freie Punkte auf allen drei Karten und drei nachgemessenen virtuellen SafeFrames, drei tatsächliche Tap-Käufe und anschließende Entity-Auswahl. Ein zunächst gefundener Fehler leitete den Frosthafen-Punkt `(153,80)` durch eine benachbarte Minen-Tippfläche zur Minenmitte um. Der UI-Owner hat dies behoben; der ursprüngliche Punkt bleibt im bestandenen Regressionstest. Das [UI-Prüfmanifest](MapHudAuditEvidence/free-projection-manifest.json) und [Unity-XML](MapHudAuditEvidence/free-projection-results.xml) dokumentieren Umfang und Quellstände.

Der zentrale Unity-Lauf besteht inzwischen **76 von 76 EditMode- und 25 von 25 PlayMode-Tests**. Das schließt den tatsächlichen InputSystem-Mausdrag, blockierende HUD-Grafiken sowie den freien Bau-/Auswahlablauf ein. Belege: [EditMode](../TestResults/editmode.xml) und [PlayMode](../TestResults/playmode.xml). Nach der weiteren Terrainüberarbeitung und Kameraänderung auf ungefähr 45° wurde der separate 318-Punkte-Test erneut bestanden; das UI-Manifest enthält die dazugehörigen Quellhashes.

Auch das produktive `Standard.asset` wurde direkt geprüft: Schema 4, drei aktivierte freie Karten, 5 Gold/s und Geschwindigkeiten 104/184/59/93. Diese Wirtschafts- und Tempowerte stammen aus den getrennten Aufträgen und Messungen des Haupt-Chats, nicht aus der freien-Placement-Geometrie. Die unabhängige Core-/Netzwerkprüfung wurde nach der letzten WireProtocol-Konstantenbereinigung erneut bestanden.

Noch in Arbeit: Start- und Sichtabnahme des endgültigen Players unter Verantwortung des Haupt-Chats. Die bisherigen Nachweise ersetzen diese Prüfung nicht. Geometrische Zulässigkeit ist außerdem kein Nachweis für Spielbalance oder Android-Leistung.

## Unabhängige Nachprüfung für Core v1.7

Am 10. November wurde die neue freie Minenplatzierung gegen eine separat eingefrorene Quellkopie geprüft. Der neue [Harness](../Tools/FreePlacementAuditV17/Program.cs) besteht **1.086 Assertions**. Alle fünf Gebäudeklassen erhalten exakt die gewählte Position mit `Target/SiteId=-1`; die sechs früheren Minenorte sind für jede Klasse gewöhnliche zulässige Fläche. Geprüft wurden außerdem Kollisionen, gleiche Koordinaten auf getrennten Spielerfeldern, Kauf ohne ausreichendes Gold, Wiederholung desselben Commands, Verkauf und erneuter Bau, höchstens zwei Minen sowie Platz für 30 Kampftürme plus zwei Minen auf jeder der drei Karten.

Die enthaltenen Upgradeprüfungen behandeln beide Kaufreihenfolgen und reine Zweige aller vier Kampfturmklassen. Schadensausbau ändert nur Schaden, Reichweitenausbau nur Reichweite; Preise richten sich nach der Gesamtzahl bisheriger Käufe. Quotes verändern den Zustand nicht und bleiben auch ohne ausreichendes Gold auswertbar. Ein dritter Ausbau wird abgewiesen. Minen besitzen ausschließlich Ertragsausbau, zahlen tatsächlich 1/2/3 zusätzliches Gold pro Sekunde und werden nicht zu Waffen. Diese Prüfung bestätigt Regeln und Zustandsübergänge, keine Balanceempfehlung.

Die [exakte unabhängige Geometriereferenz](../Tools/FreePlacementAuditV17/verify_geometry.py) stimmt an **20.281 Punkten / 101.405 Klassenvergleichen** vollständig mit dem Core überein. Enthalten sind drei echte Karten, eine synthetische Grenzfallkarte und dieselbe Karte mit eigenen und gegnerischen Gebäuden. Die Referenz verwendet rationale Segmentabstände und eigene Gebäude als Hindernisse; frühere Minenreservierungen sowie fremde Gebäude sperren keine eigene Fläche. Belege: [Core-JSON mit Quellhashes](ResearchEvidence/free-placement-v17-core.json), [Referenzbericht](ResearchEvidence/free-placement-v17-core-geometry.json). Punktzahlen sind Stichprobenumfänge, keine bebaubaren Flächenanteile.

Der erste Wire-v4-Nachlauf erkannte einen [reproduzierbaren Investitionsfehler](ResearchEvidence/free-placement-v17-wire-finding.json): Ein um eins erhöhter `Invested`-Wert wurde trotz unmöglicher Kaufhistorie akzeptiert. Der Hauptchat hat die eng begrenzte Korrektur dem Netzwerk-Owner zugeteilt. Dieser erste Nachlauf ist ausdrücklich **kein bestandener abschließender Wire-Nachweis**; der unveränderte Regressionstest bleibt im [Wire-Modul](../Tools/FreePlacementAuditV17/WireAudit.cs).

**Nachtest nach der Korrektur:** Der kombinierte Core-/Wire-Audit besteht **1.454 Assertions**, einschließlich desselben unveränderten `Invested+1`-Falls. Freie Koordinaten aller Klassen, Ausbaupfade, legale Gebäude bei null verbleibendem Gold, beide vollständig bebauten Spielerfelder sowie die Ablehnung von Überlappungen, alten Standort-IDs, doppelten Render-IDs, einer dritten Mine und ungültigen Ausbauständen wurden geprüft. Der korrigierte `WireProtocol.cs`-Hash ist `871199d90a686686fca42caea93fd3099e07133fd443c2de012e02e5bddbe90d`. Die Geometriereferenz besteht weiterhin an 20.281 Punkten / 101.405 Klassenvergleichen. [Finaler Quell-/Werkzeugnachweis](ResearchEvidence/free-placement-v17-manifest.json), [kombinierter Prüfbericht](ResearchEvidence/free-placement-v17-wire.json), [Geometriebericht](ResearchEvidence/free-placement-v17-wire-geometry.json) und [Konsolenprotokoll](ResearchEvidence/free-placement-v17-wire-run.txt) halten die Belege getrennt von den historischen v1.6- und ersten v1.7-Core-Läufen. Alle acht kompilierten Core-/Wire-Quelldateien stimmten beim Abschluss dieses Audits mit dem gemeinsamen Arbeitsordner überein. Das ist ein Codec-/Regelnachweis; tatsächlicher Netzwerktransport und Player-Abnahme bleiben separate Prüfungen.

Für eine Wiederholung die benötigten Core-Dateien und gegebenenfalls `WireProtocol.cs` zuerst koordiniert einfrieren. Mit einem .NET-8-SDK getrennt bauen und die erzeugte DLL starten; dadurch bleibt die tatsächlich kompilierte Quelle eindeutig:

```sh
dotnet build Tools/FreePlacementAuditV17/FreePlacementAuditV17.csproj -p:SourceRoot=/tmp/frozen-source -p:IncludeWire=true -p:BaseIntermediateOutputPath=/tmp/placement-audit/obj/ -o /tmp/placement-audit/bin/
dotnet /tmp/placement-audit/bin/FreePlacementAuditV17.dll /tmp/frozen-source /tmp/placement-v17.json
python3 Tools/FreePlacementAuditV17/verify_geometry.py /tmp/placement-v17.json --report /tmp/placement-v17-geometry.json
```

`IncludeWire=true` erst für den eingefrorenen gemeinsamen Wire-Stand verwenden; ohne diese Option läuft ausschließlich der Core-Audit. Die alten v1.6-Programme und Nachweise bleiben unverändert erhalten. Die integrierte Oberfläche, Transportläufe und gebaute Player werden zentral geprüft; ihr tatsächlicher Stand gehört weiterhin in `PROJECT_STATE.md`.
