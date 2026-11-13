# Größere 3D-Karten mit freier Turmplatzierung

Stand: 10. November 2026. Der Nutzerauftrag begann mit plastischeren Karten nach einer Pinterest-Vorlage und deutlich mehr Platz für Türme. Der anschließend direkt verifizierte Nutzerwunsch nach freier Kampfplatzierung ersetzt vorgefertigte Kampfsockel. Zwei eigene Minenstandorte pro Karte bleiben erhalten.

## Ergebnis

Die drei Karten sind **1600 × 960** statt 1000 × 600 groß: **2,56-mal so viel logische Fläche**. Kampftürme können auf geeigneten Flächen frei platziert werden. Die Core-Grenze beträgt **30 Kampftürme plus zwei Minen pro Spielerfeld**. Beide Spieler erhalten dieselbe Karte; Gebäude auf dem anderen Feld blockieren die eigene Platzierung nicht.

| Karte | Weglänge | Ungehinderter Läufer bei 65 Einheiten/s | Sichtbare Sperrflächen | Zulässige Punkte im 20er-Prüfraster |
|---|---:|---:|---:|---:|
| Grünes Tal | 4532 | 69,72 s | 9 | 993 |
| Sonnenpass | 4122 | 63,42 s | 10 | 939 |
| Frosthafen | 4038 | 62,12 s | 9 | 1047 |

Das Prüfraster dient nur dem Nachweis und ist **kein Platzierungsraster im Spiel**. Die Baupositionen sind echte logische Koordinaten. Der feste Gegnerweg bleibt erhalten; weder zufällige Wege noch eine Maze-Mechanik wurden eingeführt. Größere Karten verlängern die Laufwege. Die Gesamtbalance wird im Hauptchat gemeinsam mit Bot, Truppen und Wirtschaft geprüft.

## Visuelle Richtung und Assets

Der [Pinterest-Pin](https://de.pinterest.com/pin/203928689355588925/) wurde einschließlich des vollständigen Bildes im Browser angesehen. Er zeigt **Tower Defense Levels, Yegor Blynov**, laut sichtbarer Beschreibung aus **Ember Guards**. Übernommen werden Gestaltungsprinzipien: mehrstufige Nadelbäume, große facettierte Felsen, dunkle Bodenkontakte, zusammenhängende Grün-/Braunflächen, heller Weg und erkennbare besondere Bauorte. Die Meshes sind eigene Geometrie.

Der vom Nutzer eingefügte Reddit-Text unterscheidet feste von adaptiven Wegen und betont Engstellen, Sichtbarkeit und die Beurteilbarkeit der Verteidigung. Die neuen Karten bieten freie Außenflächen, Kurven für längere Beschusszeit und eine letzte Verteidigungslinie. Deko verändert den Gegnerweg nicht.

`ScenicTerrain.cs` erzeugt die Umgebung als ein gemeinsames statisches Mesh: abgerundeter Inselrand mit Gras-, Erd- und Gesteinsschichten, subtile Bodenflächen, vierstufige Nadelbäume, Schneekronen, Kakteen und facettierte Felsgruppen. Vertexfarben simulieren weiche Bodenschatten ohne zusätzlichen Shadow-Map-Renderdurchlauf. Wegschultern, kleine Fahrspuren, Kiesel, Eingang und befestigte Basis vervollständigen die Szene. Goldadern und goldene Erzfundamente kennzeichnen die zwei Minen.

Hohe Kulissen stehen auf freien Maps in den autoritativen `MapRectangle`-Sperrflächen und außerhalb der logischen Kartengrenzen. `ReservedScenery` beschränkt die jeweilige Größe sowie physische und projizierte Kronen-/Felsausdehnung auf diese Rechtecke. Die Freihaltung berücksichtigt die aktuelle leicht gedrehte Kamera und mindestens 43 Grad Elevation. Markierter Untergrund und Unterholz machen die gesamte reservierte Fläche sichtbar. Kleine Bodenpflanzen und Kiesel bleiben überbaubare Details. Innerhalb des frei bebaubaren Kartenbereichs gibt es keine zusätzlich frei verstreuten hohen Bäume, Felsen oder Wegzäune.

Die folgenden Originalquellen wurden am 10. November 2026 geprüft und sind passende Alternativen für weitere Varianten:

| Paket | Angaben auf der Originalseite | Eignung |
|---|---|---|
| [Kenney Nature Kit](https://kenney.nl/assets/nature-kit) | 3D, 330 Dateien, Creative Commons CC0 | Bäume, Felsen und Pflanzen |
| [Kenney Tower Defense Kit](https://kenney.nl/assets/tower-defense-kit) | 3D, 160 Dateien, Creative Commons CC0 | zusätzliche Turm-, Weg- und Umgebungsmodelle |

Aus diesen beiden Paketen wurden in diesem Upgrade keine Modelle importiert. Die eigene gebatchte Geometrie ist mit den Spielflächen verbunden und benötigt keine neuen Unity-Pakete, Physikobjekte oder einzelnen GameObjects pro Baum.

## Zuständigkeit und Integration

Dieser Karten-Chat bearbeitet `Assets/Core/MapCatalog.cs`, Terrain/Pad/Bounds in `Assets/Presentation/DioramaMeshes.cs`, den neuen `ScenicTerrain.cs` samt Meta und die eigenen Nachweise. Hauptchat und seine Mitarbeiter übernehmen Core/Netzwerk, Content-Migration, BoardView/GameUI, Bot und Integration.

`MapCatalog` setzt `freePlacement: true` und kuratierte `blockedAreas`. Die bisherige Koordinatenliste bleibt zur Wahrung der stabilen Minen-IDs im Content; ihre übrigen Punkte sind keine sichtbaren oder verpflichtenden Kampfsockel. Nullbasierte MineSiteIds: Grünes Tal `{0,29}`, Sonnenpass `{23,27}`, Frosthafen `{0,31}`.

Der Core-Vertrag verwendet `PlacementValidator.BuildingRadius = 30`, `PathClearance = 70`, `BuildingSeparation = 60`. Das Terrain setzt exakt dieselben `Map.BlockedAreas` in sichtbare Kulissen um. Die Wegschulter hat 39 logische Einheiten Radius. Kampfplatzierung, Vorschau und Host-Verarbeitung verwenden den gemeinsamen Validator, keine eigene Terrain-Kopie dieser Regeln.

Renderer-Vertrag:

- `DioramaResources.MapCenter`: lokale Weltkoordinaten, für diese Karten `(6, 0.15, -3.6)`.
- `DioramaResources.WorldBounds`: Geländeausdehnung samt Turmvolumen für den Kamerafit, ohne Reichweitenringe.
- `DioramaResources.MinePad`: goldenes Erzfundament auf derselben Aufbauhöhe wie das normale Fundament.
- `DioramaResources.Point` behält den bisherigen Ursprung `(X - 500) / 50`, `(300 - Y) / 50`.
- `BoardView` erzeugt auf freien Karten nur zwei MinePads. Kampftürme stehen an `TowerState.Position`; alte Slot-Indizes sind keine Positionsquelle für freie Türme.

Neue Meshes werden über die vorhandene `owned`-Liste freigegeben. Der Shader und das vorhandene gemeinsame Material bleiben verwendbar. Die laufende Vorschau unter `Builds/macOS-Preview` wird nicht überschrieben; Integration erfolgt separat.

## Verifizierte Prüfungen

`Docs/ResearchEvidence/FreeMapAudit.cs` wurde gegen die tatsächlichen Core-Dateien kompiliert und ausgeführt. Er verwendet den echten `PlacementValidator` und `MatchSimulation.Execute`:

- 3588 Rasterpunkte je Karte geprüft; 993 / 939 / 1047 zulässig.
- 784 / 777 / 828 zulässige Punkte liegen innerhalb der Reichweite **jeder** Basis-Kampfwaffe.
- Auf beiden Spielerfeldern jeder Karte jeweils 30 Kampftürme und zwei Minen über echte Baubefehle erstellt: **192 akzeptierte Befehle**.
- Sperrflächen, Minenreservierung, Klassen-/Gesamtkapazität und Isolation der gespiegelten Felder bestätigt.

Ergebnis: `Docs/ResearchEvidence/free-map-audit.json`, einschließlich ContentHashes und geprüfter Koordinaten. Der unabhängige Recherchechat bestätigt zusätzlich 17.978 Geometrieproben gegen eine exakte rationale Referenz und 194 Core-/Netzwerk-Assertions; siehe `Docs/ResearchEvidence/free-placement-audit.json`.

Core, `DioramaMeshes.cs` und `ScenicTerrain.cs` wurden mit Unitys Roslyn/Mono-Werkzeugen und der echten UnityEngine-Referenz fehlerfrei kompiliert, ohne einen Editor zu starten. Quellen-SHA256 und Prüfkontext stehen in `Docs/ResearchEvidence/free-map-compile.json`.

## Frühere Sichtprüfung des Renderers v2

Die ersten echten Unity-Bilder wurden überprüft und anschließend überarbeitet: harte Rechteckflecken wurden durch weiche organische Übergänge ersetzt, die Inselwand stärker sichtbar gemacht und Koniferen/Felsen in ihren Proportionen verbessert. Der UI-Agent stellte Hauptansicht und Gegnervorschau auf ungefähr 45 Grad Kameraelevation um. Weg, Sperrflächen, Minen und sämtliche Corewerte blieben bei dieser Bilditeration unverändert.

Alle drei abschließenden Unity-RenderTextures wurden vom Karten-Chat angesehen und für die Geländedarstellung abgenommen: sichtbare Tiefe, lesbare Vegetation und Felsen, vollständige Wege und Minen, keine beobachtete Deko-Verdeckung. Die Bilder sind 888 × 360 Pixel große echte Board-Renderings ohne HUD aus dem korrekt angewandten 1600 × 720-Testframe:

- `Docs/MapHudAuditEvidence/map-0-board.png` — Grünes Tal
- `Docs/MapHudAuditEvidence/map-1-board.png` — Sonnenpass
- `Docs/MapHudAuditEvidence/map-2-board.png` — Frosthafen

Der erneute Unity-PlayMode-Lauf bestand mit **318 Projektions-/Raycast-Prüfungen** über drei Karten und drei SafeFrames sowie **drei echten UI-Tapkäufen und anschließender Gebäudeauswahl**. Ein zuvor gefundener Mine-Pointer-Fehler bei Frosthafen `(153,80)` ist im Test enthalten und behoben. `Docs/MapHudAuditEvidence/free-projection-manifest.json` dokumentiert die übereinstimmenden Quellen-Hashes, Bildgrößen, Artefakthashes und den erfolgreichen Test; XML liegt daneben.

Die vollständige native Playeransicht mit HUD und die Android-/Leistungsabnahme bleiben beim Hauptchat. Die geringe Größe einzelner Gebäude in der Gesamtansicht wurde als konkreter Prüfpunkt für eine mögliche Zoomfunktion weitergegeben. Eine solche Funktion wird hier nicht als bereits umgesetzt behauptet. Die laufende alte Vorschau bleibt erhalten; der neue Stand geht in den separaten Integrationsbuild.

## Überarbeitung v3/v4 nach erneutem Nutzerfeedback

Der Nutzer empfand v2 weiterhin als zu leer und flach. V3 verstärkte die Randkulisse und erhöhte die inneren Sperrflächen. Die tatsächlichen Unity-Bilder zeigten jedoch eine Zaunwirkung durch gleichmäßige Randreihen und zu rechteckige Terrassen. Der Kamera-AABB-Fit verkleinerte die Spielfläche dabei um 8,39–10,70 Prozent gegenüber v2; die 318 Projektions-/Raycastchecks und drei UI-Käufe bestanden dennoch. Dieser Zwischenstand wurde nicht als finales visuelles Ergebnis freigegeben.

V4 verwendet 32 unregelmäßig gestaffelte große Wald-/Felsgruppen mit Lücken am äußeren Rand und elf niedrige Felsgruppen im Vordergrund. Die bisherigen Rechteckplateaus sind durch organisch konturierte, triangulierte Hügel bis 0,7 Welteinheiten Höhe ersetzt. Stämme und Felsen verwenden dieselbe interpolierte Dreieckshöhe wie die Geländeoberfläche. Die Vegetation bleibt in bestehenden Sperrflächen; Randobjekte stehen außerhalb der logischen Fläche.

Die 1600 × 960 große logische Karte, Wege, Sperrflächen und Baukapazität bleiben unverändert. Der äußere Inselrand wächst von 0,72 auf 1,9 Welteinheiten. Vordergrundobjekte bleiben bewusst niedrig, um freie Baupositionen nicht zu verdecken.

V4 ist fehlerfrei kompiliert. Alle drei Baseline-Bilder sowie alle drei abschließenden Bilder mit verbessertem Kamerafit wurden im Karten-Chat tatsächlich angesehen. Die aktuelle Geometrie und der Fit sind visuell freigegeben. Der isolierte Unity-PlayMode-Lauf bestand in 16,78 Sekunden: 318 Projektions-/Raycastpunkte, drei tatsächliche UI-Käufe mit Gebäudeauswahl sowie Sichtbarkeitsprüfungen für sämtliche Terrainvertices und ein konservatives Turm-/Effektvolumen auf eigener Karte und Gegner-PiP in drei Bildschirmformaten.

Der neue Fit verwendet tatsächliche Terrainvertices plus das separate Turm-/Effektvolumen. Das bringt gegenüber v4 mit altem Fit 4,73 % / 3,12 % / 4,77 % mehr lineare Spielfeldgröße. Es ist kein Zoomfeature; die Integration des geprüften BoardView-Patches liegt beim Hauptchat.

Nachweise und echte Screenshots: `Docs/MapHudAuditEvidence/vertex-fit-v4/manifest.json`, `fit-comparison.json`, `results.xml`, `map-0-board.png`, `map-1-board.png`, `map-2-board.png`. Diese versionierten Bilder zeigen den geprüften Zwischenvertrag mit zwei festen Minen. Der danach im Recherche-Chat erteilte Nutzerauftrag für freie Minen und Vollbild/Zoom wird vom Hauptchat als eigenes Integrationspaket koordiniert. Leere MineSlots entfernen im Terrain automatisch alle festen Erzstellen; eine freie Mine benötigt weiterhin ihre tatsächliche Gebäudeposition im BoardView.
