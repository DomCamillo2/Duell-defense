# Defense Duell: freundliche 3D-Spielwelt

> Historische erste 3D-Ausbaustufe. Den aktuellen Artstand 0.5 und seine Nachweise führen `PROJECT_STATE.md`, `Docs/Presentation.md` und `TEST_REPORT.md`. Die unten datierten ersten Tests gelten nur für ihren damaligen Stand.

Stand: 10. November 2026. Die ausdrückliche Nutzeränderung „3D … so mäßig wie Bloons Tower Defense 4“ ersetzt die ursprüngliche 2D-Vorgabe. Die Inspiration ist die leichte Erfassbarkeit von Weg, Einheiten und Turmklassen in einer freundlichen Spielwelt; es werden keine Ninja-Kiwi-Grafiken, Modelle, Sounds oder Marken übernommen. Referenz: [Bloons Tower Defense 4 beim Publisher Ninja Kiwi auf Kongregate](https://www.kongregate.com/en/games/ninjakiwi/bloons-tower-defense-4/).

## Umsetzung

`BoardView` ist der einzige Spielbrett-Renderer. Er baut echte Unity-Meshes auf einer erhöhten Insel, beleuchtet sie mit einer gerichteten Lichtquelle und rendert mit geneigter orthografischer Kamera in eine uGUI-RawImage-Fläche. Die bestehende GameUI/SessionCoordinator-Schnittstelle bleibt erhalten. Kein zusätzlicher Simulations- oder Enginepfad. Die kleinen Wegpiktogramme in Kartenkacheln sind ausschließlich Menü-Icons.

Die gewählte Karte stammt aus dem autoritativen Content-Katalog. Grünes Tal hat sattes Gras und Bäume, Sonnenpass ockerfarbenen Boden, Kakteen und Felsen, Frosthafen Schnee, Eiswege und Kristalle. Pfade und zwölf Bauplätze benutzen exakt die jeweiligen Core-Koordinaten. Eine geometrische Distanzprüfung hält Dekoration von Wegen und Bauplätzen fern. `WorldToViewportPoint` projiziert die großen Touchflächen auf die wirklichen 3D-Bauplätze, auch nach Größenänderung. Eigene Karte und Gegnervorschau bleiben nebeneinander; der Ansichtswechsel ändert nur den betrachteten Spieler.

64-Pixel-Touchrechtecke können auf einer geneigten Karte überlappen. `SlotHitFilter` entscheidet im tatsächlichen Canvas-Koordinatenraum zugunsten des nächstgelegenen projizierten Bauplatzmittelpunkts unter allen getroffenen Rechtecken. Die Renderfläche fängt nicht zugeordnete Taps ab. Damit hängt die Auswahl nicht von der Reihenfolge der zwölf UI-Objekte ab; insbesondere bleiben die nahen Sonnenpass-Bauplätze 7 und 12 einzeln antippbar.

Alle fünf Gebäude besitzen eine eigene Silhouette: grüne Dreifachläufe, orange schwere Kanone, hoher blauer Frostkristall, violette Doppel-Schienenkanone und eine Goldmine mit Holzschacht, Goldkristallen und animiertem Förderrad. Drei Modellstufen zeigen Upgrades. Die vier Truppenformen unterscheiden sich zusätzlich zur Farbe durch Gestalt und Größe. Projektile, Mündungsrückstoß, Trefferaufhellung und kleine Zerfallssplitter sind reine Darstellung und verursachen niemals Schaden.

Das helle uGUI-Menü bietet drei Kartenkacheln mit Wegvorschau und deutlichem Auswahlzustand. Offline ist als Solo-Duell gegen den Bot beschriftet; WLAN und Online sind Multiplayer. Lobby und Match zeigen den Kartennamen. Ein vom Host übernommener neuer ContentHash erneuert gecachte Definitionen und beide Bretter. Die Bauleiste skaliert mit der Anzahl der Turmdefinitionen. Auswahl und Upgrade zeigen Minenertrag, Minenobergrenze und Verkaufserstattung; Minen haben keinen Reichweitenkreis. HUD-Einkommen zeigt Basis und Minen separat. Angriffe zeigen ihren unterschiedlichen Preis und ihre Rolle.

## Ressourcen und Lebensdauer

- Eigene zur Laufzeit erzeugte, flach schattierte Meshes und ein kleiner Vertexfarben-Shader; keine neuen Assetpakete und keine neuen Unity-Pakete nötig. Shader liegt in `Assets/Presentation/Resources/DefenseDiorama.shader` und wird dadurch auch im Player eingeschlossen.
- Ein gemeinsames Material pro Brett, gemeinsame Klassen-Meshes, MaterialPropertyBlock für Frost/Treffer/Projektile. Kein Physics- oder Rigidbody-Einsatz.
- Maximal 80 gepoolte Truppen pro Brett, 32 wiederverwendete Projektile/Treffereffekte. Rückgabe setzt ID, Mesh, Transform, Interpolationsziel, Farbe, Trefferzeit und Lebensbalken zurück. Spieler-/Sessionwechsel leert Pools, Zielzuordnung und Effekte vollständig.
- Kleine Vorschau maximal 12 Renderdurchläufe/s, Hauptbrett pro Frame. Haupt-RenderTexture bis 1152 Pixel Breite, Vorschau bis 512, je 2x MSAA, kein HDR, keine Schatten. Reduzierte Effekte schalten Schüsse, Trefferblitze, Rückstoß und Minenradanimation ab; 30-FPS-Einstellung bleibt verfügbar.
- Eigenständige Layer-Leases 24–30 isolieren Brettkamera und Licht. Unity-Boot-Kamera muss `cullingMask = 0` behalten, damit die ausgelagerten Inseln nicht als zusätzlicher Hintergrund gerendert werden. Der Lead hat diese Änderung in BuildEntry integriert.
- Bei Resize wird die alte RenderTexture freigegeben. Beim Entfernen des Bretts werden Kamera/Light/World, RenderTexture, alle Meshes und das Material zerstört; die Layer-Lease wird zurückgegeben. Keine persistierenden Weltobjekte außerhalb dieses Besitzmodells.

## Prüfstand

Quellcode und Schnittstellen sind implementiert. Der UI-Agent startet entsprechend der Arbeitsaufteilung keine Unity-Instanz. Der Lead hat die neue 3D-Version kompiliert und importiert (`TestResults/prepare.log`: `DEFENSE_PREPARE_OK`, einschließlich DefenseDiorama-Shaderimport). `TestResults/playmode.xml` vom 10. November 2026, 18:13 UTC, dokumentiert 8 bestandene Tests, keine Fehler. Darunter ist `ThreeMapsBuildGoldminesAndReleaseTheir3DResources`: echte EventSystem-Raycasts auf sämtliche Bauplatzmitten aller drei Karten, Minenkauf, Minenertrag, Verkauf und RenderTexture-Freigabe nach Match-Ende. Der neue Mac-Player-Build beendet sich ebenfalls erfolgreich (`TestResults/mac-3d-build.log`). Tatsächliche Screenshots, Spielgefühl und Android-Hardwareprüfung werden vom Lead zusätzlich belegt. Die früheren 2D-Ergebnisse gelten nicht als Nachweis für die neue 3D-Darstellung.

Die vorhandenen PlayMode-Tasttexte bleiben erhalten: `OFFLINE`, `So geht's · interaktives Tutorial`, `Build slot 1`, `Läufer`, `Bauen ·`, `Upgrade ·`, `Verkaufen ·`, `←`. Neue Kartenbuttons heißen `Map card 0`, `Map card 1`, `Map card 2`.
