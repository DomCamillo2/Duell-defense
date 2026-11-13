# Unabhängiger Review der 3D-Integration

Stand: 10. November 2026. Reviewer: Gameplay-Agent, nicht Autor von BoardView/GameUI. Geprüft wurden `BoardView.cs`, `DioramaMeshes.cs`, `DefenseDiorama.shader`, `SlotHitFilter.cs`, der überarbeitete `GameUI.cs` sowie `GameContentAsset.cs` und dessen Build-Migration. Es wurden keine fremden Implementierungsdateien verändert und keine Unity-Prozesse gestartet.

## Konkrete Befunde

| Befund | Auswirkung | Korrektur / Nachprüfung |
|---|---|---|
| Überlappende Touchflächen auf Sonnenpass | Bauplatzindex 6 bei (825,340) und Index 11 bei (825,440) lagen in der Game-Fläche 898×350 nur 3,3 logische UI-Pixel horizontal und 31,4 vertikal auseinander. Der spätere 64×64-Button überdeckte dadurch das Zentrum des früheren; Antippen von Bauplatz 7 konnte Bauplatz 12 auswählen. Dasselbe trat bei 20:9 auf, weil die Kamera vertikal eingepasst wird. | UI-Autor ergänzte `SlotHitFilter` und `IsClosestTouchTarget`: Unter allen Rechtecken, die den Tap enthalten, gewinnt ausschließlich der nächste projizierte Mittelpunkt. Der Reviewer prüfte die Koordinatenräume und berechnete anschließend alle 72 Slotzentren für drei Karten bei 1280×720 und 1600×720 erfolgreich. |
| Reduzierte Effekte aktualisierten die Menüvorschau nicht | Nach Einschalten der Option im Hauptmenü drehte die Minenwinde weiter, da die Home-Vorschau die Einstellung nur einmal bei Erstellung erhielt und `RefreshLive` ohne Match früh zurückkehrt. | UI-Autor hält nun `menuBoard` und ruft beim Umschalten `ApplyVisualSettings` für Menüvorschau, Hauptbrett und Gegnerbrett auf. Quellcode nachgeprüft. |

Beide Befunde sind im geprüften Quellstand geschlossen. Kein weiterer P0/P1-Fehler gefunden. Der geometrische Touchtest ist kein Ersatz für echte EventSystem-/Touchtests auf Android; der Lead muss den Laufzeitfall nach dem Build tatsächlich ausführen.

Die unabhängige Geometrieprüfung verwendete die tatsächlichen MapCatalog-Koordinaten, `Point`-Umrechnung, Kamera bei (1.8,22,-28), LookAt auf (0,.15,0), Orthographic-Fit der Plattform inklusive höchster Türme sowie die Größe der Spielansicht. Ergebnis nach Disambiguierung:

```text
PASS projected touch centers after nearest-rectangle disambiguation: 72
```

Der Touchfilter verwendet lokale Koordinaten des mit Pivot (.5,.5) angelegten Overlays. `projectedCenters` verwenden denselben Ursprung. Die Overlay-Buttons und das RawImage teilen exakt dieselbe Fläche; Safe-Area-/Canvas-Skalierung wird bei der Bildschirm-zu-Lokal-Umrechnung berücksichtigt. Nicht zugeordnete Board-Taps werden vom RawImage abgefangen. Es wurden keine kleineren Trefferflächen als Fehlerumgehung eingeführt.

## Ressourcen und Darstellung

- BoardView besitzt seine RenderTexture, Kamera, Lichtquelle und Diorama-Welt. `OnDestroy` trennt die Texturreferenzen, gibt die RenderTexture frei, deaktiviert/zerstört die Welt und entsorgt ausschließlich selbst erzeugte Materialien/Meshes. Geliehene Layer werden zurückgegeben.
- Einheitengrafiken sind auf 80 je Brett begrenzt. Beim Pool-Reset werden Entity-ID, Zielposition, Rotation, Skalierung, Mesh, Status-/Flashwerte und MaterialPropertyBlock zurückgesetzt. Epoch- oder Spielerwechsel leert alle aktiven Ansichten. Projektil-/Treffereffekte verwenden einen begrenzten Pool aus 32 Objekten.
- Minen besitzen eigene Geometrie und werden in `TargetFor` aus dem Waffenpfad ausgeschlossen. Die Reichweitenanzeige wird bei Economy-Gebäuden nicht dargestellt. Alle drei Turmstufen und fünf Gebäudeklassen werden dynamisch aus Definitionslängen angelegt; das frühere vierteilige Sprite-Array wird für 3D nicht indiziert.
- Die projektierten Positionen stammen aus der tatsächlich verwendeten Karte. Der Client bemerkt einen geänderten Content-Hash, aktualisiert seine Definitionscaches und baut seine Ansichten neu. Dadurch bleibt die Ansicht nach Übernahme der Hostkarte konsistent.
- RenderTexture und manuelles `Camera.Render` passen zur tatsächlich eingestellten Built-in-Renderpipeline (`m_CustomRenderPipeline = 0`). Der eigene Shader liegt im Resources-Verzeichnis. `BuildEntry.ConnectGame` setzt die Hintergrundkamera auf `cullingMask = 0`, damit die Dioramen nicht zusätzlich hinter dem Canvas gerendert werden.
- Große Ansicht und Vorschau haben getrennte Render-Auflösungsgrenzen; die Gegner-/Menüvorschau rendert höchstens zwölfmal pro Sekunde. Das sind Implementierungsgrenzen, keine gemessene Android-Framerate oder Speicherfreigabe.

## Bedienung und Inhaltsadapter

GameUI erzeugt fünf Gebäude- und vier Truppenaktionen aus den Definitionsarrays. Baukosten, Upgradepreise und Verkaufswerte kommen aus denselben Daten wie die Simulation. Die Anzeige trennt Basis- und Mineneinkommen; Minenerträge je Stufe und das Zweierlimit werden angezeigt. Auswahl/Vorschau/Bestätigung sowie Upgrade und Verkauf senden weiterhin nur validierte Commands. Die Gegneransicht deaktiviert eigene Bauslot-Aktionen; Einstellungen besitzen einen vollständigen Raycast-Blocker.

`GameContentAsset.CreateCatalog` kopiert die serialisierten Regeln, alle fünf Gebäudedefinitionen inklusive `IncomePerSecond`/`MaxPerPlayer`, Upgrade-Einkommen sowie Karten-ID/Name/Theme/Koordinaten in unveränderliche Core-Definitionen. Die interne Regelvalidierung und Hashberechnung laufen dabei erneut. Der Adapter speichert keinen Match-Laufzeitzustand.

Zum Prüfzeitpunkt lag die ältere `Standard.asset` noch mit vier Türmen auf Platte. Die Build-Vorbereitung ruft vor der Validierung `MigratePrototypeInEditor` auf und speichert danach die Assetdatei. Für `schemaVersion < 2` erzeugt diese Migration bewusst die aktuellen Standardwerte und alle drei Karten; für Version 2 bleibt sie unverändert. Der nächste Prepare-/Build-Lauf muss diese Migration ausführen. Direktes Play mit der alten, noch nicht migrierten Datei ist kein gültiger Teststand. Diese Migration ist ein Default-Neuaufbau des in dieser Aufgabe erzeugten Prototyps, keine allgemeine verlustfreie Migration beliebiger benutzerdefinierter Altinhalte.

## Getrennt erforderliche Laufzeitbelege

Quellcode und Geometrieprüfung belegen keine kompilierte Shader-Ausgabe, keine tatsächlichen Screenshots, keinen Android-Touchlauf und keine Geräteperformance. Diese Stufen werden vom Lead nach dem UI-ready-Stand durch Unity-Compile, gestartete 3D-Szene, sichtbare Prüfung, APK und gegebenenfalls echte Hardwaretests gesondert dokumentiert. Es wird hier kein unbekannter Geräte- oder Relaytest als bestanden markiert.
