# Neue Matchansicht: Übergabe an den Hauptchat

Stand: 10. November 2026. Auftrag: Aus den drei vom Nutzer gelieferten Tower-Defense-Screenshots eine größere, auf dem Handy lesbare Spielansicht entwickeln, zuerst prüfen und anschließend an den Hauptchat übergeben.

Die Umsetzung ist als getrennte, spielbare Unity-Vorschau fertig. Die laufenden Runtime-Dateien im Hauptprojekt wurden von dieser Arbeit nicht verändert, weil dort gleichzeitig Drag-and-drop, Befehlsfeedback und neue Karten entstehen. **Die Quellstände müssen zusammengeführt werden; `GameUI.cs` und `BoardView.cs` nicht vollständig ersetzen.**

Nach der Übergabe sichtbar gewordener Abstimmungsstand des Hauptchats: Bauleiste unten, Angriffe rechts, laufende Gegnervorschau und eigene Minenplätze. Diese neueren Nutzerpräferenzen haben bei der Integration Vorrang. Reiter und manueller Gegnerwechsel dieser Vorschau sind ein geprüfter Layoutentwurf; große Landschaft, kompakte Statusleiste, Symbole, Kamera und Kontextaktionen können unabhängig davon übernommen werden. Der Hauptchat wurde auf diesen Unterschied ausdrücklich hingewiesen.

## Ergebnis

- Das Spielfeld belegt 75 Prozent der sicheren Bildschirmfläche: bei 1280×720 sind es 1280×540 statt bisher 898×350, rund 2,2-mal so viel Fläche. Eine Kamera rendert jeweils das eigene oder das gegnerische Feld.
- Die obere Leiste bündelt eigene Basis-HP samt Balken, Gold, Einkommen, Restzeit, Gegner-HP, Ansichtswechsel, Ton und Verlassen. Die angezeigten eigenen HP und Ressourcen bleiben beim Wechsel aufs Gegnerfeld die eigenen.
- Unten gibt es zwei Reiter, **Bauen** und **Angriff**. Alle fünf Gebäude beziehungsweise vier Truppenklassen haben eigene Symbole und sichtbare Preise.
- Der Kontextbereich erscheint erst für einen ausgewählten Platz oder Turm. Er enthält Reichweitenvorschau, Bau-/Upgradepreis, Verkaufserlös und Schließen. Die Auswahl lässt sich auch nach einem Bau direkt schließen.
- Das Match zeigt eine durchgehende Landschaft mit Randvegetation und einem seitlichen Flussbett. Die Kamera schaut steiler auf Wege und Gebäude. Die Menüvorschau behält ihr Diorama.
- Gegneransicht schaltet auf Angriff. **Bauen** kehrt zur eigenen Verteidigung zurück. Beim Gegner kann nicht gebaut werden. Das Tutorial führt nach seinem ersten Bau zum Angriffsreiter und fordert danach die Auswahl des gebauten Turms.

Die Referenzen wurden hinsichtlich Bildschirmaufteilung, Wegkontrast, Turmsilhouetten und sparsamem HUD ausgewertet. Zusätzliche Bildrecherche: [Kingdom Rush bei Armor Games](https://armorgames.com/news/kingdom-rush-free-for-ipadiphone), [Defense Zone, Herstellerseite](https://defensezone.net/). Die Gestaltung ist eine eigene Ableitung; fremde Spielgrafiken wurden nicht übernommen. Bestehende Kenney-Symbole und eigene prozedurale Geometrie werden verwendet.

## Dateien

Die spielbare macOS-Vorschau liegt unter `Builds/BattleViewHandoff/DefenseDuell-Ansicht.app`. Isoliertes Entwicklungsprojekt: `/tmp/defense-battleview-20261110`. Bundle-ID der Vorschau: `de.defenseduell.battleview`; die breite Prüfinstanz verwendet `de.defenseduell.battleviewwide`.

Die sechs zur Übergabe gehörenden Quelldateien liegen unter `Builds/BattleViewHandoff/source/Assets/`. [Patch](BattleViewEvidence/battle-view.patch) und [Prüfmanifest mit SHA-256](BattleViewEvidence/verification.json) liegen bei den Screenshots.

| Datei | Änderung |
| --- | --- |
| `Presentation/GameUI.BattleHud.cs` | Neues, abgegrenztes `partial GameUI`: Matchaufbau, Reiter, Kontextbereich, Statusdarstellung. |
| `Presentation/GameUI.cs` | `partial`, Matchheader überspringen, altes `Game()` in die neue Datei verlagern, Live-Texte aktualisieren, zweite Board-Aktualisierung entfernen, `RefreshBattleChrome()` aufrufen, Tutorial und Schließen anpassen. |
| `Presentation/BoardView.cs` | Optionales letztes Argument `battlefieldView=false`; Landschaftsmodus und steilere Kamera für die große Matchansicht. |
| `Presentation/DioramaMeshes.cs` | Optionaler Landschaftsmodus; erweiterter Boden, Randvegetation und seitlicher Fluss. Menügeometrie bleibt der bisherige Modus. |
| `Tests/PlayMode/Platform/ConnectedGameTests.cs` | Tutorial wählt nach dem Angriff wieder den Turm; im Match wird ein Board/eine RenderTexture erwartet. Alle Karten prüfen weiterhin echte Raycasts auf die Bauplatzmitten, Bau, Verkauf und Freigabe der Ressourcen. |
| `Tests/PlayMode/Platform/BattleHudTests.cs` | Neuer Ablauf: Gegner ansehen, Bauen gesperrt, echten Sendebefehl ausführen, über Bauen zurückkehren, Kontext öffnen und schließen. |

## Integration mit der parallel entstandenen Steuerung

Der geprüfte Entwicklungsstand stammt von 19:36 Uhr und hat noch die ursprünglichen zwölf Bauplätze. Während der Arbeit hat das Hauptprojekt bereits Drag-and-drop und Karten mit 32 Bauplätzen beziehungsweise 1600×960 Kartenkoordinaten erhalten. Diese Fortschritte erhalten.

1. Die neue `GameUI.BattleHud.cs` übernehmen und das bestehende `GameUI` zu `partial` machen. Das dortige alte `Game()` durch den neuen Aufbau ersetzen. Die neuen Befehls-Tracker, ACK-/Snapshot-Logik, Drag-Funktionen und Rückmeldungen des Hauptprojekts bleiben bestehen.
2. Die Baukarten im neuen `Game()` ebenfalls an `BuildDragSource.Initialize(this, kind)` anbinden. Dekorationen/Icons unter `UiKit.ButtonContent(button)` anlegen, damit sie mit dem dortigen `ButtonPressFeedback` animiert werden.
3. Bei Reiter- und Feldwechseln die neue zentrale `CancelInteraction()` aufrufen. `SelectSlot` um `attackTabOpen=false` ergänzen, seine bestehenden Eigentums-, Drag- und Reservierungsprüfungen beibehalten. Tutorialwechsel an die bestehende akzeptierte Befehlsauflösung anbinden.
4. Die angepassten Live-Texte und `RefreshBattleChrome()` in den aktuellen Refreshpfad übernehmen. Alle dortigen Zugriffe auf ein zweites `enemyBoard` entfernen oder nullsicher machen. **Bauen** auf der Gegneransicht muss zur eigenen Karte zurückkehren.
5. Den Landschaftsmodus in die aktuelle, inzwischen größenabhängige `DioramaResources`-/Kamerageometrie übertragen. **Nicht** die feste 1000×600 Umrechnung, die alte Kamera-Einpassung oder die alten Karten aus dem geprüften Snapshot zurückkopieren. Den seitlichen Fluss und Randbäume außerhalb der neuen spielbaren Kartenbounds platzieren. Projektion und Touch-Zonen müssen dieselbe aktuelle Kameratransformation benutzen.
6. Die Testanpassungen mit den neuen Drag-Tests verbinden. Erneut alle Bauplätze auf allen drei neuen Karten per Raycast prüfen; Kontext und Hinweisbanner dürfen sie im unselektierten Normalzustand nicht verdecken. Danach neuen Android-Build erstellen.

## Nachweise und Grenzen

- Unity 6000.3.28f1: macOS-Build erfolgreich, keine C#-Compilerfehler. Build-Log liegt unter `Builds/BattleViewHandoff/preview-build.log`.
- PlayMode-Gesamtlauf: 8 von 9 bestanden. Der einzige Fehler war die veraltete Mindestzahl an MeshRenderern für zwei gleichzeitig sichtbare Boards. Nach Anpassung lief genau dieser Drei-Karten-Test erneut und bestand. Damit sind alle neun geprüften Szenarien bestanden; es wird kein durchgehender 9/9-Gesamtlauf behauptet. XML-Dateien: `Builds/BattleViewHandoff/playmode.xml` und `battle-maps.xml`.
- Sichtprüfung im echten Unity-Spieler: 1440×810 (16:9) und 1600×720 (20:9). Im breiten Spieler Bau bestätigt, Auswahl geschlossen, Gegnerfeld geöffnet und Läufer gesendet; Geldabzug, Erfolgsmeldung und Einheit auf dem Gegnerweg waren sichtbar.
- Alle zwölf Bauplatzmitten auf jeder der drei Snapshot-Karten waren im automatisierten Raycast-Test erreichbar. Menüwechsel gab die Match-RenderTexture frei.
- Die Screenshots enthalten die macOS-Fenstertitelleiste; die genannten Seitenverhältnisse beziehen sich auf den Spielinhalt darunter.
- Noch kein Android-Gerätetest dieser neuen Ansicht und noch kein Integrationsnachweis für die parallel entwickelten 32-Platz-Karten/Drag-Bedienung. Keine Gameplay-, Preis-, Netzwerk- oder Cloud-Änderungen durch diese Übergabe.

![Match mit Kontextaktionen in 16:9](BattleViewEvidence/battle-view-16x9.png)

![Großes Spielfeld mit Bauleiste in 20:9](BattleViewEvidence/battle-view-20x9.png)

![Gegneransicht mit Angriffsleiste in 20:9](BattleViewEvidence/opponent-20x9.png)
