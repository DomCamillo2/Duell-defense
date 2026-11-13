# Unabhängige Karten-/HUD-Abnahme

Stand: 10.11.2026. Mit „Maps plastischer gestalten“ und Hauptchat abgestimmt: Dieser Chat prüft Projektion, Bildaufteilung, Eingabe-Erreichbarkeit und HUD-Verdeckung. Keine Änderungen an den Runtime-Dateien der anderen Owner.

**Aktueller Stand, 20:46 Uhr:** Hauptchat hat v4-Kamerafit und erweiterten Regressionstest in den gemeinsamen Workspace übernommen; Quellen stimmen exakt mit dem geprüften Manifest überein. Die nachgebesserte Landschaft und der Fit bestehen unabhängig alle 318 Projektionen/Raycasts und drei Käufe mit Entity-Auswahl. Zusätzlich sind sämtliche Terrainvertices sowie ein konservatives Turm-/Effektvolumen auf Hauptboard und PiP in allen neun Karten-/Formatkombinationen im Bild. Der Fit vergrößert den logischen Spielbereich um 3,12–4,77 %. Der zentrale PlayMode-Nachlauf besteht mit 26/26 Tests (20:46 Uhr); XML wurde direkt gelesen und dauerhaft gesichert. Die laufende Nutzervorschau wurde nicht berührt; Android-Gerät nicht geprüft. Die folgenden Zwischenstände dokumentieren Fehler und deren Behebung.

## Geprüfter Zwischenstand mit 32 Plätzen

Nach Vorbereitung der separaten Projektkopie `/tmp/defense-map-hud-audit` wurde ein echter Unity-Startfehler gefunden: `MaterialPropertyBlock` wurde direkt in einem `BoardView`-Feldinitialisierer erzeugt. Das ist für ein MonoBehaviour unzulässig. Verschieben nach `Initialize` behebt den Fehler; der Hauptchat hat denselben Fix bestätigt übernommen.

Anschließend bestand der unabhängige Test auf allen drei Karten: jeweils 32 Plätze in den virtuellen sicheren Frames 1280×720, 1600×720 und 1440×680. Insgesamt 1440 echte EventSystem-Raycasts (Mitte und vier benachbarte Punkte) und 288 Drop-Zuordnungen. Kein geprüfter Punkt wurde vom HUD oder Nachbarplatz abgefangen, keine Platzmitte lag außerhalb von Board oder sicherem Bereich. Der Test lädt das echte Boot/GameUI und verwendet den vorhandenen GraphicRaycaster statt einer nachgebauten Kameraformel.

Belege: [XML](MapHudAuditEvidence/slot-snapshot-results.xml), [Quelldigests und Umfang](MapHudAuditEvidence/slot-snapshot-manifest.json), [archivierter Test](MapHudAuditEvidence/slot-snapshot-test.cs.txt). Die Testdatei wurde auf Wunsch des Hauptchats aus dem aktiven Unity-Testumfang genommen, weil die neue freie Platzierung die Slot-Schnittstelle ersetzt. Die neun Kombinationen sind virtuelle Layouttests, keine Android-Geräteprüfung.

## Noch offene Abnahme der freien Platzierung

Der neue direkte Nutzerauftrag aus dem Recherche-Chat wurde gelesen und mit dem Hauptchat abgeglichen. Endprüfung erst nach tatsächlicher Implementierung und neuem Build. Der erste macOS-Next-Build entstand vor dem Startfehlerfix und ist deshalb kein geeigneter Abnahmestand.

- Echte Bildschirm → Board-Viewport → Kamerastrahl → Bodenprojektion auf allen drei Karten, 16:9/20:9 und Safe Area.
- Freie Punkte abseits alter Slots treffen den tatsächlichen Ghost-Fußpunkt; keine verdeckte Slot-Suche.
- HUD, PiP, Gegnerfeld und Außenbereich können keinen Bau auslösen.
- Bestehende Gebäude lassen sich über ihre Entity-ID auswählen; Verkauf/Upgrade bleiben erreichbar.
- Mine und Kampfgebäude respektieren die gemeinsame Core-Geometrie.
- Screenshots des finalen Builds gehen an den Karten-Chat für dessen Terrain-/Dekorprüfung.

Freie Platzierung und finale Ansicht sind hier noch **nicht als bestanden** markiert. Integrations- und Buildhoheit bleibt beim Hauptchat.

## Freie Platzierung: erster Integrationslauf

Die echten Projektionsmethoden `MapPointToScreen` und `TryScreenToMapPoint` bestehen 318 legale freie Stichproben (37/31/38 Punkte je Karte in je drei sicheren Frames). Alle liegen abseits der alten Bauplätze. Hin-/Rückprojektion weicht höchstens eine logische Einheit ab; alle Treffer liegen auf dem eigenen Board. PiP, Bauleiste und Außenbereich werden nicht als eigenes Spielfeld akzeptiert.

Der **Gesamttest ist noch fehlgeschlagen**: Auf Frosthafen wird der erlaubte Punkt `(153,80)` vom Raycastziel `Mine site 0` abgefangen und auf den Minenmittelpunkt `(80,70)` umgeleitet. Dadurch ist Bauen nicht verfügbar. Das ist ein Fehler in der Weiterleitung des Boden-Taps, kein Fehler der Core-Geometrie oder Projektionsrechnung. Der ursprüngliche Testpunkt bleibt in der Regression erhalten. Beleg: [Fehler-XML](MapHudAuditEvidence/free-mine-touch-failure.xml). Lead und Karten-Chat sind informiert; produktiver Fix beim UI-Owner und erneute Abnahme folgen.

Aktiver Prüfer: `Assets/Tests/PlayMode/Platform/FreePlacementProjectionTests.cs`. Er prüft zusätzlich den tatsächlichen Tap-/Bestätigungsweg und die anschließende Entity-Auswahl. Ein isolierter Erfolg der Projektionsschleifen wird ausdrücklich nicht als bestandener Gesamtlauf gezählt.

## Korrektur des virtuellen Formatnachweises

Beim Auslesen der RenderTexture-Größen fiel auf, dass `GameUI.Update` die gesetzte virtuelle SafeRect durch `ApplySafeArea` zurücksetzte. Die bisherigen realen Raycasts, Käufe und der Minenfehler sind belegt, die drei verschiedenen simulierten Formate waren in diesen Läufen aber nicht wirksam. Die Formatangaben in den historischen Manifesten beschreiben daher nur die angeforderten Größen, nicht nachgewiesene angewandte Größen. Der aktive Free-Test ruft jetzt nach dem Setzen direkt Rebuild auf und prüft nach Update ausdrücklich SafeRect-Breite, -Höhe und UI-Breite. Erst dessen neuer Gesamtlauf zählt für die Formatabnahme.

## Erfolgreicher korrigierter Gesamtlauf

Der Nachtest vom 10.11.2026, 20:20 Uhr, besteht vollständig (XML `result=Passed`, Laufzeit16,03s). Die drei tatsächlich angewandten SafeFrame-Größen werden nach Update zurückgelesen und geprüft. 318 freie Punkte auf allen drei Karten bestehen die Hin-/Rückprojektion und den echten UI-Raycaster. Alle drei Tap-Käufe einschließlich Frosthafen `(153,80)` bauen genau einen Turm an der gewünschten Position und erlauben anschließend dessen Entity-Auswahl. PiP, Bauleiste und Außenbereich werden nicht als eigene Baufläche angenommen.

Der UI-Fix erhält bei der überlappenden Minen-Tippfläche den echten Pointerpunkt; feste Minenwahl bleibt Aufgabe der Wirtschaftsklasse. Der gefundene Fehler wurde nicht durch einen anderen Testpunkt umgangen. Belege: [grünes XML](MapHudAuditEvidence/free-projection-results.xml), [Manifest mit Quelldigests und PNG-Abmessungen](MapHudAuditEvidence/free-projection-manifest.json). Die Kartenbilder `map-0-board.png` bis `map-2-board.png` wurden aus diesem korrigierten Lauf ersetzt.

Die vollständige Player-Sichtprüfung und Android-Geräteprüfung bleiben davon getrennt.

## Renderer-Freeze v2 und 45°-Kamera

Nach Freigabe des Karten-Chats wurden dessen Terrain/Bounds und die Kameraänderung gemeinsam in die isolierte Testkopie übernommen. Der erneute Gesamtlauf vom 10.11.2026, 20:25 Uhr, besteht vollständig (`Passed`, 16,13s). Alle neun geprüften Quelldateien stimmen bytegenau mit dem gemeinsamen Workspace überein. Der unveränderte Frosthafen-Punkt `(153,80)` bleibt bestanden.

Die aktuellen Belegdateien `free-projection-results.xml`, `free-projection-manifest.json` und die drei `map-*-board.png` enthalten diesen Stand. Das Manifest hält Quellen- und Artefakt-SHA256 fest. Der vorherige korrigierte 57°-Lauf ist als `free-projection-57deg-results.xml` und `free-projection-57deg-manifest.json` archiviert. Sichtprüfung der Board-Bilder: Pfade und Minen sind vollständig im Bild; die Inselwand ist sichtbar, die harten rechteckigen Wald-/Schneeflecken sind aufgelöst. Native HUD-/Player-Prüfung ist ein separater letzter Schritt.

## Dichtere Landschaft v3: funktional grün, visuelle Nacharbeit

Der erneute Nutzerauftrag im Karten-Chat fordert größere Bäume/Felsen, stärkere Höhenunterschiede und mehr Details. v3 ergänzt hohe Randkulissen und innere Terrassen. Der unabhängige Unity-Lauf besteht vollständig (20:36 Uhr, 16,21s), einschließlich des unveränderten Minen-Regressionspunkts. BoardView enthält dabei auch den zwischenzeitlichen Entity-Reset beim Verkauf/Neubau.

Für den Größenvergleich wurde ausschließlich in der isolierten Testkopie die Screenshotfunktion um den Export der vier projizierten logischen Kartenecken erweitert. Ein erneuter v2-Baseline-Lauf und der v3-Lauf verwenden denselben Messhelfer. Bei 888×360 RenderTexture beträgt die logische Kartenbreite vorher 589,46 Pixel, danach 526,95 / 540,02 / 526,39 Pixel. Das entspricht einer linearen Verkleinerung von 10,60 / 8,39 / 10,70 Prozent.

Die drei Bilder zeigen stärkere Höhe, jedoch unnatürlich regelmäßige lange Randreihen und wieder harte rechteckige Terrassen. Keine beobachtete Weg-/Minenbeschneidung. Diese Kritik wurde konkret an beide Owner übergeben; die letzte Player-Abnahme bleibt offen. Dauerhafte Belege: `MapHudAuditEvidence/renderer-v2/` und `MapHudAuditEvidence/renderer-v3/` einschließlich XML, Bildern, Vier-Ecken-CSV, Größenvergleich und tatsächlich ausgeführtem Testquelltext.

## v4 und genauerer Kamerafit: getestete Übergabe

Der Karten-Chat korrigierte Randreihen zu Gruppen mit Lücken und ersetzte rechteckige Terrassen durch triangulierte Hügel. Den Vorschlag, echte Terrainvertices statt der acht Ecken ihrer Welt-AABB für den Kamerafit zu verwenden, hat dieser Ansichts-Chat isoliert implementiert. Die AABB enthält fiktive hohe Vordergrundecken, die den bisherigen Ausschnitt unnötig vergrößern.

Der Patch projiziert bei Initialisierung/Resize die echten statischen Terrainvertices. Zusätzlich berücksichtigt er die gesamte logische Karte mit horizontal 1,25 Welteinheiten Reserve und einer Höhe von 0,18 bis 3,05 für Turmköpfe, Drehungen, Bau-/Upgradeimpuls und begrenzte Kampf-/Treffereffekte. Die Arbeit erfolgt nicht pro Frame. Frei platzierte Türme werden unabhängig von alten Slotpositionen geschützt.

Der v4-Test besteht vollständig (20:41 Uhr, 16,78s). Der Prüfer kontrolliert alle echten Terrainvertices sowie die acht Ecken des konservativen Volumens im tatsächlichen Kamera-Viewport, sowohl auf dem Hauptboard als auch in der PiP. Alle bisherigen Projektions-, UI-Raycaster-, Tapkauf- und Entity-Auswahlprüfungen bleiben erhalten. Ein separater v4-Baseline-Lauf liefert den direkten Größenvergleich: Grünes Tal 528,99 → 554,02 Pixel, Sonnenpass 540,02 → 556,88 Pixel, Frosthafen 528,44 → 553,67 Pixel bei identischer RenderTexture.

Übergabe unter `MapHudAuditEvidence/vertex-fit-v4/`: `vertex-fit.patch`, `executed-test.cs.txt`, `results.xml`, `manifest.json`, `fit-comparison.json`, Kartenbilder und projizierte Eckkoordinaten. Der Hauptchat besitzt die gemeinsame BoardView und integriert selbst. Der Nutzer spielt bereits die vorige Vorschau; kein eigener nativer Eingriff und kein Überschreiben dieser App. Neuere Aufträge zu Vollbildlayout/Zoom müssen nach ihrer Umsetzung getrennt geprüft werden.

## Integration bestätigt

Der Hauptchat bestätigte die Übernahme ausdrücklich. Gemeinsam gespeicherte SHA256: BoardView `f0998ff8360a261ce98f5df8c1a11046f39b1b8c8ce24811d25652679860fb82`, FreePlacementProjectionTests `cc0dc444e483ba7f1dd2bca91fa7907a19bf74d5644eef30cd1405a6a840f575`. Diese Dateien und ScenicTerrain wurden nochmals direkt gegen das getestete Manifest verglichen. Damit ist die bisherige Fit-Aufgabe integriert. Für das Folgepaket bleibt dieser Chat Test-Owner; Anpassungen beginnen erst nach API-/UI-Freeze vom Hauptchat. UI-Runtime gehört weiterhin dessen bestehendem UI-Agenten.

Der zentrale Nachlauf vom 10.11.2026, 20:45:29–20:46:21 Uhr, besteht mit 26/26 Tests. Dauerhafte Kopie: `MapHudAuditEvidence/vertex-fit-v4/integrated-playmode-results.xml`. Dieser Stand deckt den integrierten Kamerafit ab, nicht das anschließend angeforderte Vollbild-/Zoom-Folgepaket.
