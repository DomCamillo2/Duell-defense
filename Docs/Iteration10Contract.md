# Integrationsvertrag 0.10 – Vollbild, unbegrenzter Ausbau und WLAN-Darstellung

Stand: 13. November 2026. In Integration; dieser Vertrag ist kein Test- oder Buildnachweis. App 0.10.0 / Android-Code 10, Core v2.0, Content-Schema 7, Wire 7. Finale Ausführungsergebnisse gehören in Release10TestReport und versionsgebundene Manifeste. Der Nutzer hat die unbegrenzte Variante ausdrücklich bestätigt.

## Erhaltene Grundlagen

Zwei Spieler, eigene Felder auf einer von drei Karten, Solo-Bot oder LAN/Relay. 180 Startgold, 30 Basis-LP, zehn Wellen mit Baupausen. Der Host entscheidet über Simulation und Käufe. 2× erfordert beidseitige Zustimmung, in Solo direkt. Laufende Pakete behalten Forschungsstand, Preis, Welle und FIFO-Position vom Kauf. Die separat geprüfte Vier-Spieler-Idee ist noch kein implementierter Modus.

## Mengen, Geld und Ausbauten

Keine künstlichen Grenzen für Kampftürme, Minen, angenommene Truppenaufträge oder aktive Truppen. Freie geometrisch gültige Plätze, Geld, Freischaltungen, Ausmarschrhythmus und die endliche Partie bleiben erforderlich. Kein Wellen-Kaufbudget von 240. Annahmeschluss der letzten Welle ist das Ende weiterer gültiger Sendungen, kein Mengenlimit. Bereits bezahlte Sendungen werden vollständig verarbeitet.

Gold, Restbeträge, Investitionen, steigende Preise, Forschung, Gebäude-Pfadlevel, HP, Schaden und Frostdauer werden exakt mit skalierbaren Ganzzahlen verarbeitet. Es gibt keinen Maximal-Kauf nach zwei, acht oder zehn Stufen und keine 10.000-Gold-Kappung. Technische Speicher-/Adressierungsgrenzen sind keine Zusage unendlich vieler gleichzeitig simulierbarer Objekte auf einem Handy. Belastungstests und praktische Gerätegrenzen sind gesondert auszuweisen.

Gebäudepreise richten sich nach der Gesamtzahl aller bisherigen Ausbauten desselben Gebäudes, nicht je Pfad getrennt. Verkauf verwendet die tatsächliche gesamte Investition. Forschung gilt nur für nach dem Upgrade gekaufte Pakete. Erwartete Forschungsstände werden exakt im Kommando übertragen; veraltete Angebote sind ohne Abbuchung abzulehnen.

## Klassenspezifische Pfade

| Klasse | Primär | Sekundär |
|---|---|---|
| Schnellfeuer | Feuerkraft | Reichweite |
| Kanone | Sprengkraft | Explosionsradius |
| Frost | Frostdauer | Reichweite |
| Panzerbrecher | Durchschlagskraft | Reichweite |
| Goldmine | Förderung | kein zweiter Kampfpfad |
| Blitzspule | Kettenenergie | Sprungreichweite |
| Balliste | Bolzenkraft | Reichweite |

Der effektive Statresolver versorgt Kaufvorschau und Kampf gleichermaßen. Frost besitzt ausdrücklich keinen Schadensausbau; sein geringer Basisschaden und seine bestehende Slowstärke bleiben. Ein manipuliertes Damage-Kommando für Frost wird zurückgewiesen. Dauer wirkt auf neue Treffer; bestehende Effekte werden nicht rückwirkend verlängert. Schuss- und Bewegungstempo bleiben in brauchbaren physikalischen Bereichen.

Räumliche Werte nähern sich einer sinnvollen Grenze. Nach zehn Sekundärausbauten steigt zusätzlich die Klassen-Primärwirkung; Beschriftung und Vorschau zeigen das ausdrücklich (bei Frost Dauer, niemals versteckter Schaden). Primärwerte, Kosten und Level haben weiterhin keine feste Endstufe. Sehr lange Frostdauer über die verbleibende Lebens-/Partiezeit hinaus bringt naturgemäß keinen zusätzlichen praktischen Nutzen. Finanzierbarkeit und mathematisches Wachstum sind keine menschliche Balanceabnahme.

Der Angriffseinkommens-Divisor 240 und das bestehende zusätzliche Angriffseinkommensmaximum von 3 G/s bleiben eigenständige Wirtschaftsregeln. Sie dürfen nicht versehentlich mit dem entfernten Kaufbudget gleichgesetzt werden. Mine und gesamtes Einkommen wachsen unabhängig davon. Der Plus-Hinweis erscheint erst beim tatsächlichen Spawn.

## Darstellung, Eingabe und Netzwerk

Die Hauptkamera passt den spielbaren Boden in die Geräte-SafeArea; das HUD darf die Map überlagern. Pan/Zoom macht Randbereiche erreichbar. Native Renderziele bis 2560 Pixel Kantenlänge werden tatsächlich gemessen; die Auflösung einer gespeicherten PNG allein ist kein Qualitätsnachweis.

Bauaktivität ist ein ausdrücklicher Zustand. Nach Bauabschluss oder Abbruch bleibt kein Geistergebäude am Pointer hängen. Ein Tipp neben einem ausgewählten Turm wählt ab; ein neuer Bau erfordert erneute Klassenwahl. PointerUp kann eine Ziehgeste abschließen, ein späteres EndDrag darf nicht doppelt kaufen. ACKs und Zustandswechsel sind nach Session und konkreter Entity zuzuordnen.

Gastbewegung wird aus autoritativen Zeit-/Wegpunkten gepuffert interpoliert. Keine neue clientseitige Kampfsimulation oder Geldvorhersage. Epochwechsel, neue Felder, längere Lücken und Fokuswechsel leeren alte Bewegungsdaten. Eine kleine Darstellungsverzögerung wird gegen gleichmäßige Bewegung getauscht.

Wire 7 überträgt exakte große Zahlen sowie variable Mengen über begrenzte einzelne Seiten. Ganze Zustände werden erst nach vollständiger Validierung atomar übernommen. Neue Revisionen dürfen einen laufenden großen Transfer nicht endlos verdrängen. Join/Welcome, zuverlässige Übergänge, Duplikate, vertauschte oder fehlende Seiten und Revanche brauchen eigene Belege. Transportüberlast darf akzeptierte Käufe nicht unbemerkt löschen. Wire-6-Clients sind inkompatibel und müssen die neue APK erhalten.

## Art und Ressourcen

Produktive Modelle haben elf visuelle Meilensteine (0–10), dazu kleine Primär-/Sekundär-/Mischpfadmodule. Diese endliche Geometrie begrenzt weder die tatsächliche Forschungsstufe noch die Wirkung. Exakte Level werden zusätzlich angezeigt. Dynamische wiederverwendbare Views dürfen keine simulierten Einheiten oder Gebäude wegen früherer Poolgrößen unsichtbar machen.

Eigene glattere Rundungen an Metall/Körpern, erhaltene Stein-/Kristallfacetten, detailliertere Landmarken und ein neu generiertes App-Icon. Keine kostenpflichtigen Assets, keine Laufzeit-KI. Porträts entstehen aus denselben produktiven Meshes und dem gleichen Material, mit gebundenem Quellenmanifest. Das alte Quellen-/APK-Paket bleibt erhalten.

## Erforderliche Abnahme

- Bauen über 30 Kampftürme/2 Minen/32 Gesamtobjekte; Kauf/FIFO über 80 Aufträge/240 Wellenwert; aktive Einheiten über 80/64 Herkunft, ohne verlorene Käufe.
- Forschung 8→9, 10→11, 100→101 und große exakte Zahlen; ungültige/alte Quotes und Duplikate atomar; Frost echte Dauerwirkung und unveränderter Schaden.
- Netzseiten über alte 16-KiB-, UInt16- und Byte-Seitenzahlgrenzen, volle atomare Übernahme unter Revisionenwechsel; eindeutige alte Versionsablehnung.
- Bewegungsprüfung bei 5-Hz-Zuständen und Jitter sowie tatsächliche neue LAN-/Relay-Prozesse; keine Geräte-Glätte behaupten ohne Gerätemessung.
- Drag-Bauen→Upgrade→Abwahl, fehlendes/verspätetes EndDrag, Fokus/Modal/Rebuild, späte ACK/Ablehnung.
- Drei Karten und verschiedene tatsächliche Pixelauflösungen, neue Modellstufen/Portraits/Icon; frischer Import, Mac und Android-APK, Signatur/Hash.
- Physische Handyperformance, zwei menschliche Spieler und subjektive Balance ausdrücklich getrennt bewerten.
