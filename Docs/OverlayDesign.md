# Overlay-Entwurf nach Tower-Defense-Referenzen

Direkter Nutzerauftrag vom 10.11.2026: Oberflächen anderer Tower-Defense-Spiele, ausdrücklich Bloons, ansehen; einen passenden Entwurf entwickeln und mit dem Hauptchat abstimmen. Die folgenden Screenshots wurden tatsächlich im Browser betrachtet, nicht nur anhand von Suchbeschreibungen ausgewertet.

## Beobachtungen und Übertragung

- [Bloons TD 6 auf MiniReview](https://minireview.io/tower-defense/bloons-td-6): seitlicher Shop mit großen Klassenbildern, gut sichtbaren Preisen und abgeblendeten nicht bezahlbaren Einträgen; Leben und Geld kompakt am oberen Rand. [Betrachtetes Bild](https://minireview.io/common/uploads/review/c845e7d56a48e7a3d48187d44a69eded.jpg).
- [Bloons TD 6, TouchArcade-Review](https://toucharcade.com/2018/06/15/bloons-td-6-review-the-game-where-everything-happens-so-much/): weiterer Screenshot mit horizontalem Shop am unteren Rand. Bild, kurzer Name und Preis reichen für die wiederholte Auswahl. Die unterschiedlichen Bildformate sind keine Behauptung über eine aktuelle automatische Layoutumschaltung. [Betrachtetes Bild](https://cdn.toucharcade.com/wp-content/uploads/2018/06/Bloons-TD-6-3.jpg).
- [Kingdom Rush 5: Alliance, Hersteller](https://www.ironhidegames.com/Games/kingdom-rush-alliance): im [offiziellen Gameplaybild](https://www.kingdomrushalliance.com/en/img/kr5/screenshots/KRA_screenshot_01.jpg) kleine Statusgruppe oben links, einzelne Einstellungen oben rechts, bildbetonte Aktionen an den unteren Ecken und eine freie Mitte.

Für Defense Duell sind Bauen und wiederholtes Angreifen gleichzeitig wichtig. Deshalb bleiben beide erreichbar: fünf Gebäude unten links und vier Angriffstypen rechts. Keine abwechselnden Bauen/Angreifen-Tabs. Die große Gegnerkarte besitzt einen ausdrücklichen Rückweg. Ein Mehrspieler-Menü behauptet keine Pause. Es werden keine Grafiken, Markenfiguren oder Spielregeln der Referenzen übernommen.

## Entwickelter interaktiver Entwurf

`Tools/OverlayLab/index.html`, `overlay.css`, `overlay.js`; vom Projektroot mit `python3 -m http.server 8767 --bind 127.0.0.1` öffnen: `http://127.0.0.1:8767/Tools/OverlayLab/index.html`.

Der Entwurf nutzt die neun bereits erzeugten eigenen Klassenbilder und eine historische Unity-Kartenaufnahme. Er ist ausdrücklich keine Spielsimulation, besitzt keinen Host, keinen Live-Gegner und keine autoritative Geländeprüfung. Preise, beispielhafte Ausbauwerte und 65-Prozent-Verkauf stammen aus dem gelesenen aktuellen Core; produktiv bleiben Content und `TowerUpgradeRules.Quote` alleinige Datenquelle. Die historische Aufnahme enthält noch Erzfundamente; sie ist keine Behauptung über den neuen freien Minenstand.

Konkrete Unterschiede zum zuerst geschriebenen 0.4-HUD, in einem logischen 1280er-Entwurfsrahmen:

| Bereich | Entwurf | Zweck |
| --- | --- | --- |
| Ressourcen | etwa 336 × 62, Leben, Gold/Einkommen, Uhr | Kein fast bildschirmbreites Statuspanel |
| Gebäude | fünf 104 × 105 Karten, 8 Abstand | Große Bilder und Preis, Rollenbeschreibungen erst bei Auswahl |
| Angriffe | 94 breite Schiene, vier Bildkarten | Gleichzeitiger Zugriff ohne 232 breite Textspalte |
| Gegner-PiP | 190 × 120, mit Name, HP und Vergrößerungshinweis | Klare Aktion und kleiner dauerhafter Platzbedarf |
| Turmkontext | 680 × 116, ersetzt die Baukarten | Keine zweite dauerhafte Leiste; Schaden/Reichweite mit Vorher → Nachher |
| Fehler | betroffene Karte kurz rot, Text mit fehlendem Gold | Farbe plus konkrete Erklärung; kein Erfolg und keine Abbuchung |
| Oberflächen | dunkles Blaugrün, helle Schrift, Goldpreise | Lokaler Kontrast zur hellen Landschaft; nicht globales App-Restyling |

Die Maße sind eine Startvorlage, keine unveränderlichen Runtime-Koordinaten. Unity muss tatsächliche SafeArea, Canvas-Skalierung und mindestens angemessene physische Touchflächen berücksichtigen. Zoom-/Schließen-Controls des Browserentwurfs sind noch keine nachgewiesenen Handy-Touchgrößen. UI-Tests verwenden deshalb die tatsächlichen RectTransforms und Raycasts statt diese Entwurfskoordinaten.

## Verifikation und Übergabe

Browserprüfung: Klassenwahl → Boden → Bestätigung; Schnellfeuerkauf 180 → 115 Gold; Schadenpfad 6 → 9 für 55 Gold, Rest 60; anschließende Mine mit Hinweis „Es fehlen 80 Gold“ ohne Abbuchung. Gegner-PiP öffnen, auf 125 Prozent zoomen und ausdrücklich zurückkehren. 20:9- und 16:9-Anordnung angesehen; 16:9 als vollständige Seitenaufnahme gesichert. JavaScript-Syntax mit `node --check Tools/OverlayLab/overlay.js` geprüft.

Screenshots: `Tools/OverlayLab/evidence/overview-20x9.png`, `overview-16x9.png`, `selected-tower.png`, `rejected-purchase.png`, `enemy-expanded.png`.

Der Hauptchat hat die ausgewählte-Turm-Ansicht gesichtet und die kompakten Deltas für das aktuelle 0.4-Paket ausdrücklich an seinen UI-Owner freigegeben. Dieser Chat hat keine konkurrierenden UI-Runtime-Dateien bearbeitet. Umsetzung und tatsächlicher Unity-Nachweis bleiben getrennt vom Browserentwurf. Der zentrale Projektstatus bleibt ausschließlich `PROJECT_STATE.md`; dieser Text dokumentiert nur Entwurfsbegründung und Übergabe.
