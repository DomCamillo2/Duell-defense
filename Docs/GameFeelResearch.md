# Game Feel für Defense Duell

Recherche und Umsetzungsspezifikation · 10. November 2026

## Aktueller Nutzerauftrag: ein gemeinsamer Art-Style

Direkter Auftrag in diesem Recherchechat am 10. November 2026, nach dem Lifecycle-Audit: Die bisherigen Klassenbilder gefallen, aber Türme und Truppen im Feld müssen dieselben Figuren zeigen. Karte, Modelle, Icons und Overlay sollen gemeinsam überarbeitet werden. Das HUD soll spielerischer und kunstvoller werden; unbezahlbare Käufe sollen grau erscheinen. Die rechte Angriffsleiste soll weniger Text enthalten und links neben jeder Karte einen Info-Knopf mit animiert aufklappbaren Details bekommen. **Der Hauptchat hat den Originalauftrag gelesen und vollständig als zusammenhängendes Folgepaket 0.5 übernommen.** Er sichert vorher den laufenden 0.4-Checkpoint; Runtime-GO für 0.5 folgt von ihm. Dies ist noch kein gebauter Art-Rework. Seine 0.4-Prüfung wird dadurch nicht nachträglich zur Abnahme dieses Reworks.

### Gemeinsame Formen und Materialien

Richtung: eine liebevoll modellierte kleine Spielzeugwelt, mit kräftigen Silhouetten, weichen abgeschrägten Kanten, warmem Stein, bemaltem Metall und klaren Klassenfarben. Das vorhandene generierte Schnellfeuer- und Runner-Porträt wurde visuell mit dem Renderer-v4-Kartenbild verglichen. Die Porträts sind die Qualitäts- und Formvorlage; ein bloßer Austausch gegen Renderings der unveränderten groben Modelle erfüllt den Auftrag nicht.

- Schnellfeuer: grünes rundes Gehäuse, drei dicke dunkle Läufe in Dreiecksanordnung, helle Fassungen, goldene Kappe und gestufter Steinsockel.
- Kanone: orange gepanzerter Körper, ein breites Rohr; Frost: eisblaue große Kristalle; Panzerbrecher: violetter Aufbau mit zwei langen Schienen; Mine: warmer Stein/Holz und große goldene Erzformen. Die konkreten bestehenden Porträts bleiben Formreferenz.
- Runner: kompakter grüner Steingolem mit großen hellen Augen, dunklen Stiefeln und goldenem Kragen. Sprinter: schmaler türkisblauer Kristallkörper mit Visier und orangefarbenen Seitenflossen. Tank: breites orangefarbenes Kettenfahrzeug mit heller Frontblende, ohne erfundene Kanone. Schwarm: kleine violette Golems mit goldenem Kristallkamm; das Kaufporträt zeigt vier Exemplare desselben Feldmodells, entsprechend dem tatsächlichen Truppenpaket.
- Landschaft: dieselben abgerundeten Steinformen und bewusst großen Farbflächen, warme Wegkanten, gestufte Felsinseln und kräftige Baumkronen. Wiese, Wüste und Frost behalten unterscheidbare Themen innerhalb dieser gemeinsamen Formensprache. Platzierungsgrenzen und Wege bleiben lesbar.
- HUD: dunkelgrüne/tealfarbene eingefasste Tafeln, warme elfenbeinfarbene Beschriftung, goldene Preisplaketten und leichte plastische Kanten. Kleine Blattformen oder Steine nur am Rahmen; keine Dekoration über Zahlen oder Spielfläche. Klassenfarben im Porträt und am Kartenakzent wiederholen.

Palette als Entwurfsbasis: tiefer Wald `#203C35`, Tafelfläche `#31584A`, Elfenbein `#FFF3D8`, Gold `#E7BC59`, warmer Stein `#B5A089`; Akzente Blattgrün `#79B54B`, Türkis `#56BDCE`, Orange `#D98545`, Violett `#9574BC`. Kontrast und Unterscheidbarkeit im tatsächlichen HUD prüfen, diese Werte sind keine gemessene Abnahme.

### Modelle sind die gemeinsame Quelle für Feld und Porträt

Der Modell-Owner entwickelt die tatsächlichen Feld-Meshes anhand der vorhandenen Porträts weiter. Erst daraus werden die neuen Kaufporträts mit einer festen leicht erhöhten Dreiviertelkamera, transparenter Fläche und einheitlichem Licht gerendert. Dieselben Mesh-/Materialdefinitionen und Farbwerte müssen in Feld, Kaufkarte und Auswahlansicht verwendet werden. Vergrößerte charakteristische Merkmale sind wichtiger als mikroskopische Texturen. Nicht neun separate 3D-Illustrationen erzeugen, die anschließend wieder vom Feld abweichen.

Bestätigte Umsetzung: Der Hauptchat erstellt einen Editor-Porträtrenderer und backt die Porträts aus den gemeinsamen Modellen nach `Assets/Art/ModelPortraits`. Insgesamt **31 Porträts**: vier Kampfklassen mit jeweils sechs Varianten, drei Minenstufen und vier Truppenporträts. Keine zusätzlichen dauernd rendernden Kameras pro HUD-Karte. Ein Rendermanifest verbindet Modell-/Materialstand, Klasse und Variante mit dem erzeugten Porträt. Die bestehende Bildserie bleibt als Referenz erhalten. `GeneratedClassIcons.Get(kind, bool)` bleibt für Basis-/Truppenkarten erhalten; neu liefert `GetTowerVariant(kind, visualVariant)` das zum gewählten Turm passende Porträt. Der HUD-Owner verwendet den gemeinsamen `TowerUpgradeRules.VisualVariant`-Index. Reichweitenring, Footprint und Trefferlogik werden durch das Aussehen nicht verändert.

### Ruhigere Angriffsleiste und animierte Information

Im geschlossenen Zustand enthält jede Angriffskarte nur Porträt, Goldpreis und einen kleinen Zähler für bereits bezahlte wartende Pakete. Die Gesamtwarteschlange bleibt als kompakte Zahl lesbar. Tempo, Basisschaden, Paketgröße und Erläuterungen stehen im Detailpanel. Name und Rolle erscheinen dort ebenfalls, statt die kleine Karte mit mehreren Textzeilen zu füllen.

Links neben jeder Karte steht ein optisch kleines `i` mit mindestens 44×44 UI-Einheiten eigener stabiler Touchfläche. Es ist ein Geschwisterelement der Kauftaste, keine verschachtelte zweite Kaufaktion. Ein Tap öffnet das zugehörige Panel nach links; ein weiterer Tap schließt es. Es ist höchstens ein Panel offen. Wechsel der Klasse aktualisiert denselben Panelbereich. Eigener Schließenknopf, Android-Zurück/Escape und ein außerhalb liegender Tap schließen, wobei der schließende Tap verbraucht wird und keinen Bau/Kauf auslöst.

Bewegungsvorgabe als zu prüfender Kandidat: 180 ms Öffnen, 130 ms Schließen, maximal 12 UI-Einheiten Verschiebung und weiches Ein-/Ausblenden; kein federnder Overshoot. Bei reduzierten Effekten sofortiger Zustandswechsel. Die Kaufkarte selbst verschiebt sich nicht. Detailpanel innerhalb der Safe Area, keine Überschneidung mit Rückkehr/PiP-Kontext. Seine Trefferfläche blockiert das Board ab Öffnungsbeginn bis zum vollständigen Schließen. Schnelles mehrfaches Tippen, Dragbesitz und zweite Finger dürfen keine widersprüchlichen Animationen oder Käufe auslösen.

### Bezahlbarkeit als klarer Zustand

Wenn das bestätigte lokale Guthaben unter dem Preis liegt, erscheinen Porträt, Rahmen und Preis deutlich entsättigt/grau; die Ziffer bleibt lesbar. Ein kleiner Fehlbetrag kann in der Infoansicht stehen. Sobald das tatsächliche Guthaben reicht, kehrt die reguläre Farbe ohne dauerhaftes Blinken zurück. Info bleibt unabhängig vom Guthaben bedienbar. Die Regel gilt konsistent auch für Truppen und Upgrade-Käufe; volle Ausbaustufe, Klassenlimit und laufende Bestätigung erhalten ihren eigenen Zustand.

Der Kauf und insbesondere der Beginn eines Turmdrags müssen gesperrt sein, solange der Preis nicht bezahlt werden kann. **Nur `Button.interactable=false` genügt nicht:** `BuildDragSource` verarbeitet eigene Pointer-Ereignisse; `BeginBuildPress` benötigt denselben Bezahlbarkeits-/Phasenguard. Bei ausreichend Gold zu Dragbeginn und später unzureichendem Gold wird beim Drop erneut autoritativ geprüft. Kein negatives Gold, keine Reservierung nur durch Öffnen der Info, kein Kauf durch das Schließen des Panels. Das vorhandene rote Feedback bleibt für tatsächlich abgelehnte Aktionen/Netzwerk-Rennen sinnvoll; normales fehlendes Gold ist bereits vor dem Versuch grau erkennbar.

### Gemeinsame Abnahme und Umsetzungsteilung

Vom Hauptchat präzisierte Aufteilung nach dem 0.4-Checkpoint und seinem 0.5-GO:

| Owner | Exklusiver Umfang für diesen Umbau |
| --- | --- |
| Ansichts-Chat `01a1c6bc-f70f-75c2-a78e-30f542f60d63` | `GameUI.cs`, `GameUI.BattleHud.cs`, `UiKit.cs`, neue Info-/Bezahlbarkeitskomponenten und zugehörige Tests; Anbindung der vom Hauptchat bereitgestellten Porträt-API. |
| Hauptchat-Agent `research_assets` | 3D-Modelle, Unitvisuals und DioramaShader; Modellanteile in `DioramaMeshes.cs`. Keine parallelen HUD-/Porträtloader-Edits. |
| Mapchat | Ausschließlich `ScenicTerrain.cs`; Landschaftsformen und Palette. Keine parallelen Änderungen an `DioramaMeshes.cs`. |
| Hauptchat | `GeneratedClassIcons`, neuer Editor-Porträtrenderer, `Assets/Art/ModelPortraits`, gemeinsames Rendermanifest, Integration, Prüfungen, Build und Freigabe. |
| Designchat | Unabhängige Look-/Silhouettenprüfung gegen die bestehenden neun Modellvorlagen. |
| Dieser Recherchechat | Diese Spezifikation und später unabhängiger read-only Lifecycle-Review der Infofläche; keine Runtime-Edits. |

`DioramaMeshes.cs` enthält Terrain- und Modellteile; die oben begrenzte Zuständigkeit verhindert konkurrierende Änderungen. Diese Aufteilung ersetzt die frühere Sammelzuständigkeit des UI-Agenten für HUD und Porträtintegration. Ein fertiger 0.4-Teststand ist kein Nachweis für die neue 0.5-Darstellung.

1. Alle neun Klassen jeweils als tatsächliches Feldmodell und Porträt nebeneinander prüfen; übereinstimmende Silhouette, Merkmale und Farben. Beim Schwarm ist die Gruppe ausdrücklich aus vier identischen Feldmodellen zusammengesetzt.
2. Alle sechs Varianten jeder Kampfklasse und drei Minenstufen kontrollieren; keine abgeschnittenen Modelle/Ringe nach dem Formumbau. Kleine HUD-Größen, eigene Karte und PiP berücksichtigen.
3. Bezahlbarkeit bei Preis−1, genau Preis und Preis+1; Einkommenswechsel bei geöffnetem Info-Panel und während des Ziehens. Unbezahlbarer Drag erzeugt weder Auftrag noch Vorschau eines bestätigten Kaufs.
4. Jeder Info-Tap verändert Gold und Queue um exakt null. Kauf kostet weiterhin exakt einmal. Zweiter Finger, rasches Öffnen/Schließen, Wechsel der Karte, Zurück, Fokusverlust und Rematch hinterlassen keine unsichtbaren Blocker oder offenen Pointer.
5. Screenshots der drei Kartenthemen mit identischem neuem HUD bei 16:9 und 20:9; keine doppelten Menüs, Textüberläufe oder verdeckten Preise. Danach zentraler Player-Build und wirklicher Spieltest. Ein schönes Einzelrendering ist noch keine integrierte Spielversion.

Die nachfolgenden Forschungsabschnitte dokumentieren die zuvor erarbeiteten Grundlagen; sie ersetzen den hier vorangestellten aktuellen Auftrag nicht.

**Empfehlung:** Defense Duell sollte auf kurze, gut zuordenbare Reaktionen setzen: ein fühlbarer Tastendruck, ein bestätigter Bauimpuls, unterscheidbare Turmbewegungen und klar sichtbare Folgen guter Entscheidungen. Die Intensität richtet sich nach der Bedeutung des Ereignisses. Die nächste Änderung sollte bei Kaufbestätigung und Bau/Upgrade beginnen.

**Abstimmung:** Der Entwicklungs-Chat „Entwickle Defense Duell für Android“ hat diese Recherche und eine priorisierte Spezifikation als nächsten Präsentationsschritt bestätigt. Seine erste APK-, Netzwerk- und Sichtabnahme wurde ohne parallele Runtime-Änderungen abgeschlossen. Der Nutzer hat anschließend Drag-and-drop sowie eine gemeinsame Implementierungs-/Prüfschleife beauftragt; diese ist nun im Hauptchat in Arbeit. Seine Vorgaben bleiben: Erfolg erst nach akzeptiertem Host-ACK, keine erfundenen Goldwerte, feste Touchflächen, keine Kameraerschütterung und abschaltbare Zusatzeffekte. Dieses Dokument beschreibt den Entwurf; die Wirkung ist noch nicht auf Android gemessen. Die neuen Anforderungen stehen in [DragPlacementSpec.md](/Users/domi/Documents/Handygame/Docs/DragPlacementSpec.md) und [EconomyAndAttackQueue.md](/Users/domi/Documents/Handygame/Docs/EconomyAndAttackQueue.md).

**Versionshinweis:** Die Codeanalyse unten beschreibt die Ausgangsfassung v1.2. In der Folgeiteration kommen freie Turmpositionen, eigene Minenstandorte, sofort bezahlte Angriffswarteschlangen und eine Kill-Gold-Regel hinzu. Aussagen über zwölf feste Bauplätze und fehlendes Kill-Gold sind historische Befunde. Die verlinkten Folge-Spezifikationen beschreiben den aktualisierten Auftrag; die Rückmelderegel „echtes bestätigtes Ereignis vor Erfolgsanimation“ bleibt unverändert.

## 1. Was „addictive“ und „satisfying“ hier bedeuten

„Addictive motion“ ist kein einheitlich definierter Animationstyp. Für dieses Projekt unterscheiden wir drei Ziele: **Satisfaction** bezeichnet die angenehme einzelne Interaktion, **Engagement** die Bereitschaft weiterzuspielen, **Retention** die Rückkehr an späteren Tagen. Ein schöner Treffer belegt keine langfristige Bindung. Die ausgewerteten Studien rechtfertigen auch keine pauschale Aussage, eine bestimmte Kurve löse einen „Dopamin-Hack“ aus.

**Game Feel** umfasst das Zusammenspiel von Eingabe, Spielreaktion und Wahrnehmung. **Juice** bezeichnet verstärkendes Feedback, etwa Bewegung, Klang und Partikel für dasselbe Ereignis. Eine Forschungsübersicht ordnet Game Feel in Physicality, Amplification und Support ein: fühlbare Eigenschaften, verstärkte Rückmeldung und Unterstützung der Interaktion. Diese Taxonomie hilft beim Entwerfen, ist selbst kein Wirkungsnachweis für eine bestimmte Animation. [Pichlmair & Johansen, *Designing Game Feel. A Survey*, 2020](https://arxiv.org/abs/2011.09201)

Für Defense Duell ist die gewünschte Erfahrung konkret: „Ich habe den richtigen Turm am richtigen Platz verbessert; ich erkenne, wie er die nächste Gruppe aufhält; ich möchte eine andere Kombination ausprobieren.“ Bewegung macht diese Ursache und Wirkung erfahrbar. Spielregeln, Gegenstrategien und Matchtempo müssen die interessanten Entscheidungen liefern.

## 2. Was die Forschung tatsächlich zeigt

Die Recherche kombiniert Primärstudien, eine Literaturübersicht, offizielle Plattformdokumentation und eine Prüfung des Projektcodes. Sie ist eine gezielte Literaturauswertung, keine vollständige systematische Review. Für die Übersicht sowie die Arbeiten von 2022 und 2024 wurden Volltexte herangezogen; die übrigen unten zusammengefassten Studien stützen sich auf ihre Publikationsabstracts. Die folgenden Befunde werden von unseren eigenen Designhypothesen getrennt.

| Quelle und Untersuchungsart | Belastbarer Befund | Grenze und Konsequenz für dieses Spiel |
| --- | --- | --- |
| Hicks et al., 2019; zwei Experimente mit 40 bzw. 32 Personen, drei Spiele | Visuelle Ausschmückung verbesserte die audiovisuelle Attraktivität; Kompetenzgewinne waren kontextabhängig. | Ein angenehmerer Eindruck ist nicht automatisch bessere Leistung. Lesbarkeit separat prüfen. [Primärpublikation](https://doi.org/10.1145/3311350.3347171) |
| Kao, 2020; vier Varianten eines Action-RPG, N = 3.018 | Keine und extreme Verstärkung schnitten bei Spielzeit, Spielerlebnis, intrinsischer Motivation und Leistung schlechter ab als mittlere und hohe Verstärkung. | Kein universeller optimaler Effektpegel; ein Spiel und eine Untersuchungssituation. Für unser dichtes Spielfeld ist dosiertes Feedback eine plausible Ausgangshypothese. [Publikationsabstract](https://experts.illinois.edu/en/publications/the-effects-of-juiciness-in-an-action-rpg/) |
| Kao et al., CHI 2024; präregistriertes Experiment, N = 1.699 | Erfolgsabhängiges Feedback stärkte unter anderem Kompetenzerleben. Neugier war im Modell mit freiwilliger Spielzeit verbunden; zufällige Feedbackvariation erhöhte sie nicht. Verstärkung allein wirkte teilweise negativ. | Verdeckende Effekte könnten die Zuordnung von Handlung und Ergebnis gestört haben. Die Autoren nennen dies eine Einschränkung. Keine Aussage über D7-Retention oder unser Tower-Defense-Spiel. Unsere Ableitung: Erfolg erkennbar abstufen und Ursachen sichtbar halten. [Volltext](https://people.csail.mit.edu/dkao/pdf/3613904.3642656.pdf) |
| Singhal & Schneider, CHI 2021; zwei Untersuchungen mit Smartphone-Interaktionen und einem Spiel | Passende zusätzliche Haptik konnte Freude, ästhetischen Eindruck, Immersion und Bedeutung verbessern. | Kein Beleg für dauerndes Vibrieren oder identische Wirkung auf jedem Gerät. Haptik als gezielte Ergänzung testen. [Autorenpublikation](https://uwspace.uwaterloo.ca/items/739bca60-4670-4ea7-bc4d-c1ddd2242b26) |
| Lin et al., 2022; Analyse von 5.000 Steam-Kommentaren und Spielmerkmalen | Untersucht Zusammenhänge zwischen wahrgenommener Trefferwirkung und Merkmalen wie Hitstop, Klang und Kamerafeedback. | Beobachtungsstudie, kein isolierter Kausaltest. Daraus folgt keine Pflicht zu Hitstop oder Kameraerschütterung auf dem Handy. [Volltext/Abstract](https://arxiv.org/abs/2208.06155) |
| Meiners et al., FDG 2025; N = 31, vier Varianten eines 2D-Platformers | Dekorative, nicht interaktive Elemente erhöhten vor allem audiovisuelle Attraktivität; andere Dimensionen, darunter Kompetenz und kognitive Belastung, unterschieden sich nicht nachweisbar. | Kleine Studie und anderes Genre. Fab-Bäume und Felsen können die Welt aufwerten; daraus folgt kein Nachweis für ein besseres Bediengefühl. [Publikation](https://publikationen.bibliothek.kit.edu/1000180225) |

Aktualitätsgrenze: Auch die CHI-EA-2026-Arbeit *Quantifying Hit Feedback Preferences in 2D Action Games* wurde gefunden. Ihr Volltext und ein belastbarer Ergebnisabstract waren nicht zugänglich; daraus werden hier keine Zahlen oder Empfehlungen übernommen. [Publikationskennung](https://doi.org/10.1145/3772363.3798911)

## 3. Bewegungsprinzipien als Designwerkzeug

Die folgenden Beispiele sind **eigene Vorschläge für Defense Duell**, keine in Studien bestätigten Idealwerte.

| Prinzip | Was man wahrnimmt | Konkreter Einsatz |
| --- | --- | --- |
| Schneller Beginn, sanftes Abbremsen | Die Bedienung reagiert; die Bewegung kommt kontrolliert an. | Button beim Drücken leicht einsenken; nach Loslassen zügig in die Ausgangslage. |
| Vorbereitung, Impuls, Auslaufen | Gewicht und Kraft werden unterscheidbar. | Kanonenlauf kurz zurücksetzen und langsamer zurückführen. Eine Vorbereitung darf keine tatsächliche Feuerverzögerung vortäuschen. |
| Kleines Überschwingen | Ein neu gesetztes Objekt wirkt körperlich und findet seinen Platz. | Bestätigter Turm wächst einmal von etwa 92 % über 104 % auf 100 %. Kein dauerndes Federn. |
| Squash and stretch | Elastizität oder Druck wird sichtbar. | Kleine Verformung des sichtbaren Aufbaus; Fundament, Reichweite und Touchfläche bleiben stabil. Nicht jeden steinernen Turm wie Gummi behandeln. |
| Gerichteter Impuls | Man erkennt Herkunft und Ziel eines Vorgangs. | Rückstoß entgegen der Schussrichtung, kurzer Strahl zum bestätigten Trefferpunkt. |
| Sekundärbewegung | Ein Mechanismus wirkt lebendig. | Minenrad läuft ruhig; ein kleiner Förderimpuls begleitet eine echte Auszahlung. |
| Gemeinsamer Zeitpunkt | Bild und Ton beschreiben dasselbe Ereignis. | Trefferakzent, Treffergeräusch und HP-Aktualisierung gehören zum selben dargestellten Treffer. |
| Hierarchie und Ruhe | Wichtige Vorgänge bleiben auffindbar. | Normaler Schuss sehr klein; Upgrade deutlich; Basisverlust deutlich lokal markiert; Ergebnisbildschirm als klarer Abschluss. |

Ein passender Motivationsrhythmus besteht aus drei Ebenen: unmittelbar eine Eingabe erkennen, nach einer Entscheidung ihre Kampfwirkung verstehen und nach einer Partie eine neue Strategie ausprobieren wollen. Weitere zufällige Partikelvarianten ersetzen diesen Zusammenhang nicht. Die im Projekt dokumentierten langen, oft am Zeitlimit endenden Bot-Partien bleiben eine gesonderte Balancefrage; eine Bauanimation löst sie nicht.

## 4. Prüfung des vorhandenen Spiels

Referenz ist der lokale Code vom 10. November 2026. Zeilennummern können sich bei der weiteren Entwicklung verschieben.

| Bestand | Beobachtung | Bedeutung für die Umsetzung |
| --- | --- | --- |
| `Assets/Presentation/BoardView.cs`, `Display`, `Update` | Gepoolte Projektile/Impact, Rückstoß, Trefferaufhellung, Frosttönung und Minenrad sind vorhanden. Rückstoß springt für 120 ms auf Skalierung 0,9 und zurück. | Die vorhandenen Mechanismen verfeinern; kein zweites konkurrierendes Effektsystem auf dieselben Transforms setzen. |
| `BoardView.Display` und `TargetFor` | Schüsse werden aus einem Anstieg des Cooldowns erschlossen; das Ziel wird aus dem aktuellen Zustand erneut gesucht. | Bei ausgelassenen Snapshots können Schüsse fehlen. Ein gerade getötetes Ziel ist schon entfernt; das ersatzweise gefundene Ziel kann falsch sein. Stärkere Effekte würden diesen Fehler sichtbarer machen. |
| `BoardView.Shot` / `Update` | Visuelle Flugzeit etwa 160 ms; der Regelschaden ist bereits berechnet. Anschließend erzeugt die Darstellung einen weiteren Impact. | Eine lange Flugbahn erzählt einen verspäteten Treffer. Präzise Schuss-/Trefferchoreografie braucht eindeutige Ereignisse oder eine gemeinsame Darstellungszeit. |
| `BoardView.Display`, Entfernen von Einheiten | Goldfarbene Zerfallspartikel; Tod wird über Verschwinden und Wegdistanz angenähert. | Kann wie Beute aussehen, obwohl Abschüsse kein Gold geben. Neutrale Splitter bevorzugen; Verschwinden allein beweist keinen Tod. |
| `SessionCoordinator.Update` | Simulation 20 Hz; reguläre Snapshots alle 0,2 s, also 5 Hz. | 60 Renderbilder pro Sekunde liefern keine 60 neuen Spielzustände. Vorhandenes exponentielles Annähern ist keine zeitgestützte Snapshot-Interpolation. |
| `GameUI.Send` / `ApplyResolution` | Erfolg wird nach Host-Ergebnis verarbeitet und anhand der Sequenz dedupliziert. Lokale Hostbefehle können synchron aufgelöst werden. Gespeichert wird bisher nur der Befehlstyp. | Für einen gezielten Bauimpuls zusätzlich Kind, Ziel/Slot, Session und erwartete Änderung merken. Synchronen ACK-Pfad erhalten. |
| `GameUI`, `LocalSettings`, projizierte Slots | Reduzierte Effekte, 30/60-FPS-Modus und Slotflächen von 64 Canvas-Einheiten existieren. | Neue Effekte integrieren sich in diese Einstellungen. Skalierung gehört auf sichtbare Kinder, nicht auf den Raycast-Bereich. Die Größe in Android-dp ist gesondert zu prüfen. |

## 5. Priorisierte Spezifikation

**P0 ist der vereinbarte nächste Umfang. P1 und P2 sind nachfolgende Vorschläge.** Alle Zeit- und Intensitätswerte sind Startwerte zur Erprobung, keine wissenschaftlich optimalen Konstanten. Die Spielregeln, Preise, Treffer, Laufwege und Netzwerkautorität bleiben unverändert.

| Priorität / Ereignis | Auslöser und Bewegung | Startwert / Grenze | Betroffene Stelle |
| --- | --- | --- | --- |
| P0 · Tastendruck | Lokal bei PointerDown den sichtbaren Buttoninhalt leicht drücken; bei Up, Exit, Cancel oder Disable zurücksetzen. | 1 → 0,97 in 80 ms; zurück in 120 ms. Eingabebestätigung startet im nächsten gerenderten Bild; dies ist ein Ziel, keine gemessene Latenz. | `UiKit.cs`, kleine Präsentationskomponente |
| P0 · Bauen | Akzeptiertes ACK bestätigt den Kauf. Erst wenn der passende autoritative Turm sichtbar ist, einmaliger Bauimpuls. | 0,92 → 1,04 → 1 in etwa 240 ms. Bei Ablehnung kein Bauimpuls. | `GameUI.ApplyResolution`, `BoardView` |
| P0 · Upgrade | Nach akzeptiertem ACK und passendem Stufenwechsel die neue Silhouette kurz hervorheben. | 1 → 1,05 → 1 in etwa 220 ms; höchstens ein lokaler Ring. | `GameUI`, `BoardView` |
| P0 · Weniger Bewegung | Laufende Dekoration sofort beenden, ursprüngliche Transforms wiederherstellen. | Status, aktuelles Modell, Gold und Fehlermeldungen bleiben sichtbar. | `LocalSettings`, `ApplyVisualSettings`, neue Komponente |
| P1 · Turmrückstoß | Vorhandenen Sprung durch zeitbasierte Kurve ersetzen; jeder Typ erhält eine passende Amplitude. | Schnellfeuer sehr kurz; Kanone etwa 40–60 ms Impuls und 120–160 ms Rückkehr. Nur sichtbare Teile bewegen. | `BoardView.Update` |
| P1 · Treffer/Tod | Kurze lokale Aufhellung und begrenzte Splitter; Tod und Entkommen nicht gleich behandeln. | Aufhellung etwa 60–100 ms, kein Vollbildblitz. Neutrale Splitter statt Goldbeute. Exakte Ursache erst mit P2-Ereignis sicher. | `BoardView`, Shaderparameter |
| P1 · Mineneinkommen | Reale Auszahlung einmal gebündelt am HUD bestätigen. Mine darf einen kurzen mechanischen Akzent erhalten. | Ein Akzent pro Auszahlung, nicht pro Goldstück. Zahl zeigt jederzeit den autoritativen Goldbestand. | `GameUI`, später Ereignisadapter |
| P2 · Präzise Kampfeffekte | Schuss, Treffer, Tod und Entkommen durch explizite Hostereignisse verbinden. | Begrenzter Puffer, Deduplizierung, keine Wiederholung alter Events beim Verbindungsaufbau. | Core-Ereignisadapter und versionierte Netzwerkübertragung |
| P2 · Gleichmäßige Truppenbewegung | Timestamp-/Tick-basierte Darstellung von Pfaddistanz prüfen. | Zusätzliche Darstellungslatenz messen; keine pauschale 200-ms-Verzögerung aller UI-Reaktionen. | `BoardView`, Snapshotdarstellung |
| P2 · Haptik und Kampfklang | Einzelne eigene Bestätigungen und wichtige Ereignisse betonen. | Abschaltbar; keine Vibration für jeden automatischen Turmschuss. Klangquellen begrenzen. | `GameAudio`, optional Android-Adapter |

Die vier Kampftürme erhalten eine erkennbare Bewegungssprache: **Schnellfeuer** mit kleinem schnellem Laufimpuls; **Kanone** mit größerem Rückstoß und langsamem Einpendeln; **Frost** mit einem kurzen Kristallimpuls und lesbarer Verlangsamung; **Panzerbrecher** mit gerichtetem hartem Impuls. Frost darf kein vollständiges Einfrieren zeigen: Die Regeln verlangsamen Einheiten, stoppen sie aber nicht. Ein langer Kanonenbogen ist erst sinnvoll, wenn Treffer und Flugbahn konsistent dargestellt werden können. Die **Goldmine** vermittelt Produktion über ihr Rad und tatsächliche Auszahlung.

## 6. Unity-Integration im Detail

### P0: Eingabe und bestätigtes Ergebnis verbinden

Der Ablauf ist `PointerDown → kleine lokale Druckreaktion → Submit → ACK → bestätigtes UI-Ergebnis → zum Ergebnis passender Board-Zustand → Bau-/Upgradeimpuls`. Das UI kann den angenommenen Auftrag sofort bestätigen, sobald das ACK da ist. Die Weltanimation wartet gegebenenfalls auf den passenden Snapshot. Die Reihenfolge von ACK und Snapshot darf dabei beliebig sein.

In `Send` den vollständigen Präsentationskontext erfassen: aktuelle Session, Befehlstyp, Kind, Ziel, bei Upgrades bisherige Entity und Stufe. Nach `Submit` mit der erhaltenen Sequenz speichern. Den vorhandenen `LastResult`-Pfad für synchronen Host-ACK ebenfalls mit diesem Kontext aufrufen. Die Deduplizierung wird auf den Session-Lebenszyklus begrenzt. Ablehnung, Abbruch, Revanche oder Sessionwechsel löschen offene Impulse.

Ein angenommener Befehl darf genau eine Bestätigung auslösen. Der Boardimpuls wird einmal verbraucht, wenn Zustand und erwartete Änderung zusammenpassen. Bei inzwischen verkauftem/ersetztem Gebäude oder nicht eindeutig zuordenbarem Zustand entfällt der Impuls; es wird kein Modell rekonstruiert. Kein künstliches Wiederholen beim Öffnen der Gegneransicht oder beim initialen Laden eines bestehenden Matchzustands. Ein begrenztes Ablaufdatum verhindert verspätete Feiern nach einem Verbindungsproblem.

Für Buttons bleibt der äußere RectTransform samt GraphicRaycaster-Fläche stabil. Bild und Beschriftung liegen auf einem gemeinsam animierten Kind; vorhandener ColorTint und Druckanimation dürfen nicht gegeneinander arbeiten. Beim Turm bleibt der Root am Bauplatz. Ein gemeinsamer visueller Parent kann Bau-/Upgrade-Skalierung tragen, darunter liegen Fundament und Kopf; Rückstoß und Zielen werden an getrennten Kindern oder in einer gemeinsamen Berechnung zusammengesetzt. Das Minenrad behält seinen Pivot. Kein Update darf unbemerkt die Rotation eines anderen Effekts überschreiben.

### Zeitbasierte Kurven statt Framezählen

Für kurze kosmetische Animationen eignet sich die vorhandene unskalierte Zeit. Unity dokumentiert `Time.unscaledTime` als unabhängig von `timeScale`. Die folgenden Kurven verändern nur Darstellung; Treffer- und Einkommenszeit bleiben im Core. [Unity-6.3-API](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/time/unscaledtime)

```csharp
// Entwurf einer reinen Kurvenfunktion, kein eingebauter Runtime-Patch.
static float Smooth01(float value)
{
    float u = Mathf.Clamp01(value);
    return u * u * (3f - 2f * u);
}

static float BuildScale(float ageSeconds)
{
    // 0,92 -> 1,04 -> exakt 1; insgesamt 240 ms.
    if (ageSeconds < 0.09f)
        return Mathf.Lerp(0.92f, 1.04f, Smooth01(ageSeconds / 0.09f));
    return Mathf.Lerp(1.04f, 1f, Smooth01((ageSeconds - 0.09f) / 0.15f));
}

// Beispiel im bereits vorhandenen Präsentations-Update:
// scale = reducedEffects ? 1f : BuildScale(now - began);
// visualChild.localScale = savedBaseScale * scale;
// Nach Ende, Poolrückgabe und Disable: exakt savedBaseScale restaurieren.
```

Amplitude und Dauer werden zentral als Profile gepflegt. Basiswerte einmal speichern, nicht fortlaufend die bereits veränderte Skalierung multiplizieren. Wiederholte Impulse ersetzen oder kombinieren sich begrenzt; sie stapeln sich nicht unendlich. Bei App-Pause/Resume ausstehende Einmaleffekte verwerfen oder sauber neu baselinen. Keine Wirkung auf `Time.timeScale`, Simulationsschritte oder Eingabefreigabe. Ein globaler Hitstop würde hier zwei gleichzeitig laufende Spielfelder und deren Bedienung betreffen und ist für diesen Entwurf ausgeschlossen.

### P2: Eindeutige Ereignisse und ehrliche Goldanzeige

Ein möglicher rein darstellender Datensatz lautet `FeedbackEvent(SessionId, EventSequence, Tick, Kind, SourceId, TargetId, Position, Amount)`. Der Host erzeugt ihn bei der tatsächlichen Aktion. `Amount` dient nur der Darstellung eines bereits gebuchten Werts. Der Client berechnet daraus niemals Schaden, Gold oder Treffer. Eventsequenzen und Puffermengen werden begrenzt; Wiederholungen anhand von Session und Eventsequenz verworfen. Initialzustände enthalten keine nachträglich abzuspielende Historie. Übertragungsform, Verlustbehandlung und Protokollversion sind eine eigene Netzwerkänderung mit eigener Abnahme.

Netto-Golddifferenzen zwischen zwei Snapshots sind **kein zuverlässiger Einkommensnachweis**: Ein Kauf und eine Auszahlung können sich im selben Intervall überlagern. Ohne eindeutiges Auszahlungssignal zeigt das HUD lediglich den tatsächlichen Bestand und die bereits vorhandene Rate. Mit Signal kann ein kleiner `+N`-Akzent erscheinen; ein dekoratives Goldstück darf 400–600 ms zum HUD fliegen, während die verfügbare Zahl trotzdem sofort korrekt bleibt. Die Ausgangsfassung vergütet Abschüsse nicht. Für die später ergänzte Kill-Gold-Regel ist eine passende Goldanimation erst an die tatsächliche autoritative Buchung zu binden. Ausgelassene Pakete oder verschwundene Einheiten dürfen keine erfundenen Auszahlungen erzeugen.

Für Kampfeffekte entweder einen kurzen, zum bestätigten Ereignis gehörenden Tracer verwenden oder Zustände und Ereignisse auf eine gemeinsame Darstellungszeit abbilden. Eine längere visuelle Flugzeit darf nicht allein die tatsächliche Trefferberechnung verzögern. Interpolation sollte der Pfaddistanz folgen, damit Einheiten an Ecken nicht durch die Landschaft schneiden. Bei Paketlücken sind begrenztes Extrapolieren, Stoppen und Korrektur bewusst zu vergleichen; sichtbare Glätte wird gegen Darstellungslatenz abgewogen.

### Ressourcen, Klang und Haptik

Vorhandene Pools weiterverwenden. Aktuell gibt es 32 Schuss-/Burstobjekte pro Board; die Kapazität wird nicht pauschal erhöht. Vorschau reduziert darstellen, unwichtige alte Effekte zuerst ersetzen und keinen Instantiate/Destroy-Zyklus pro Treffer einführen. Gemeinsame Materialien und vorhandene MaterialPropertyBlocks nutzen. Dekorative Animation braucht keine zusätzlichen Collider, dynamischen Lichter oder transparenten Vollbildflächen.

Unity empfiehlt für zeitweise animierte UI Code oder Tweening und weist darauf hin, dass UI-Animatoren auch ohne Wertänderung Elemente jedes Bild als verändert markieren können. Deshalb kleine ereignisgesteuerte Komponenten verwenden und Canvas-Rebuilds messen; kein zusätzliches Tween-Paket ist für diesen Umfang nötig. [Unity UI optimization](https://unity.com/how-to/unity-ui-optimization-tips)

Audioereignisse zeitlich mit dem sichtbaren Ereignis verbinden, bei hohem Aufkommen begrenzen und eigene Käufe gegenüber automatischem Kampf priorisieren. Kleine Klangvarianten können Wiederholung weniger auffällig machen; daraus wird kein Bindungsversprechen abgeleitet. Ein Startbudget von beispielsweise acht gleichzeitigen Kampfklängen ist zu profilieren, keine aktuelle Projektgarantie.

Android empfiehlt konsistente, zur Aktion passende Haptik und warnt bei älteren One-shot-Vibrationen vor nachlaufendem, summendem Gefühl. Für eine spätere Unity-Brücke können passende `HapticFeedbackConstants` über `View.performHapticFeedback` dienen; API-Verfügbarkeit berücksichtigen, Systemeinstellungen respektieren und den Aufruf auf dem Android-UI-Thread kapseln. Fehlende Unterstützung bleibt ohne Haptik funktionsfähig. [Designprinzipien](https://developer.android.com/develop/ui/views/haptics/haptics-principles), [API-Referenz](https://developer.android.com/develop/ui/views/haptics/haptics-apis)

„Reduzierte Effekte“ deaktiviert zusätzliche Skalierung, Impulse, Partikel und gegebenenfalls neue haptische Akzente. Eindeutige Auswahlmarkierungen, HP, Froststatus, Fehlermeldungen und bestätigte Zahlen bleiben erhalten. Eine separate Haptikoption ist für P2 sinnvoll. Das Weglassen von Kameraerschütterung unterstützt die stabile Touchplatzierung und entspricht auch der Richtung der offiziellen Zugänglichkeitsleitlinie zu vermeidbaren Kamerabewegungen. [Microsoft XAG 117](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/117)

## 7. Abnahme und Prüfung der Wirkung

**Funktionale Prüfungen für P0:** Angenommene, abgelehnte und doppelte Ergebnisse; synchroner Host-ACK; ACK vor/nach Snapshot; rascher Ansichtswechsel; neues Match mit wiederverwendeter Sequenz; Verkauf vor verspätetem Impuls. Jede erfolgreiche Aktion bestätigt genau einmal. Ablehnung verbraucht kein Gold und feiert keinen Bau. Baupositionen, Minenstandorte und vorhandene Gebäude bleiben auf allen verfügbaren Karten erreichbar; die früheren zwölf Slots sind keine feste Testgrenze. Ein Wechsel zu ReducedEffects mitten in der Animation stellt die Basiswerte wieder her. Poolrückgabe und Szenenwechsel hinterlassen keine aktiven Effekte oder Listener.

**Geräteprüfung:** Auf dem Android-Zielgerät beide Boards und insgesamt 160 Einheiten mit maximaler Effektbelegung prüfen. CPU/GPU-Frametimes, UI-Rebuilds, GC-Spitzen, Überzeichnung, Speicher und Erwärmung beobachten. 16,7 ms bei 60 FPS beziehungsweise 33,3 ms bei 30 FPS sind Framebudgets, keine bereits erreichten Messwerte. Erst einen Ausgangslauf, dann denselben Ablauf mit P0 messen. Ein gewonnener Bauimpuls rechtfertigt keine schlechtere Eingabelatenz.

**Formative Spieltests:** Zunächst 8–12 Personen als praktikabler kleiner Test, nicht als statistischer Wirksamkeitsbeweis. Gegenbalancierte Reihenfolge von aktuellem Stand, abgestimmter Variante und reduzierten Effekten; gleiche Karten, Regeln und vergleichbare Szenarien. Nach Kauf, Upgrade und Kampfsituation erfassen: Wurde das Ergebnis richtig erkannt? Wie unmittelbar und angenehm fühlte es sich an? Was war ablenkend? Fehlbedienungen und Übersehen von Statuswechseln beobachten. Eine freiwillige Revanche ist ein ergänzendes Signal; lange Partien können ebenso Unklarheit oder defensive Balance widerspiegeln.

Eine Variante wird übernommen, wenn Ergebnisse mindestens ebenso gut verstanden werden, die Bedienung stabil bleibt und die Rückmeldungen zum Spielgefühl dafür sprechen. Erst danach P1 testen. P2 wird wegen der Netzwerkschnittstellen als eigener Entwicklungsschritt behandelt.

## 8. Bezug zur Asset-Auswahl und Übergabe

Die Pakete aus [FabAssetSelection.md](/Users/domi/Documents/Handygame/Docs/FabAssetSelection.md) können Wald, Steine, Kristalle und Mine ausgestalten. Für die geplanten Bewegungen sind saubere Pivots, getrennte bewegliche Teile und konsistente Größen wichtiger als ein großer Effektkatalog. An einer Mine muss das Rad separat drehbar sein; Turmkopf und Lauf sollten getrennt animierbar bleiben. Der aktuelle Vertex-Color-Shader und das mobile Materialbudget sind bei einem Import zu beachten. Für P0 ist kein zusätzliches Fab-Paket erforderlich.

An den Entwicklungs-Chat übergeben wurden: diese Evidenzbewertung, die P0-Abgrenzung, die Kurve, der ACK-/Snapshot-Ablauf und die Abnahmekriterien. Die Folgeiteration ist inzwischen mit Drag-and-drop und Angriffsstapeln beauftragt. Die interaktive Bewegungsillustration im Recherche-Chat zeigt nur die vorgeschlagenen Unterschiede; sie ist kein Screenshot und kein Nachweis eines implementierten Unity-Builds. Ihre JavaScript-Syntax und ihr Fragmentformat wurden geprüft. Die lokale visuelle Browserprüfung war durch die URL-Sicherheitsrichtlinie blockiert und ist nicht als bestanden zu werten.
