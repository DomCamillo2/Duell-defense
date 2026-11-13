# Defense Duell – verbindlicher Projektstand

Stand: 11. November 2026. Root/Hauptchat `01a1c67c-8801-7cf2-9b86-2b5353ef4b76`, Host local, ist alleiniger Integrations-/Release-Owner. AGENTS.md und Docs/CONTRACT.md beachten.

## Aktueller Stand 0.8 – getestet und gebaut

App 0.8.0 / Android-Code 8, Core v1.9, Content-Schema 6, Wire 6. Die Nutzerkorrektur „zu voll“ ist integriert: kompakter Status, Wellen- und Einkommensdetails auf Abruf, verborgene leere Queue, vollständig scrollbare FIFO, nur freigeschaltete Bauklassen und Infoziele in den Angriffskarten. Die Basis bleibt in der Übersicht außerhalb der Bedienelemente. Gamifizierte Upgrade-/Verkaufsbuttons und gegenseitiges 2× bleiben erhalten. Regeln, Balance und Modelle sind unverändert.

- Vollständiger PlayMode-Lauf: 71/71 PASS. Danach nur die Farbe und den Schatten der Angriffsüberschrift korrigiert; gezielter HUD-Nachlauf: 19/19 PASS. Beide Quellstände sind separat gebunden.
- Unabhängige Bildsichtung bei logischen 900/960/1280 mit tatsächlichen 1350/1440/1920×1080-Captures bestanden. Basis, Pointer, Kaufangebote, FIFO und Freischaltungsmasken geprüft. Bilder stammen aus Unity, nicht von Android.
- Mac: `Builds/macOS-HudReview/DefenseDuell.app`, Bundle `de.defenseduell.preview.hud`, Fenstername `Defense Duell 0.8.0`. Zwei eigene Headlessprozesse haben die reale LAN-Abnahme bestanden: 361 gemeinsame Snapshots, 50 Hostbefehle, beide Exit0. Kein interaktiver nativer 0.8-Spieltest behauptet.
- APK: `Builds/Android/DefenseDuell-0.8.apk`, 45.351.283 Bytes, SHA-256 `f81eaf57d634328dc3537297b7c3cf2a8f1051b04541ecc77b3a936ac4ec0ae6`. ARM64/IL2CPP/GLES3, APK-v2-Debug-Testsignatur, App 0.8.0 / Code 8. Noch nicht auf Android gestartet.
- Frischimport ohne Library und separate Prüfung aller 43 Porträts bestanden: `Builds/FreshImport-20261111-113144`. Quellenarchiv: `Artifacts/DefenseDuell-0.8-Source.zip`; Größe/Hash im versionsgebundenen Quellartefaktbeleg.
- 27 Core-/Net-/Content-/Platform-/Progressionquellen unverändert. Core-, Networking-, Content- und Progression-DLLs sind bytegleich mit 0.7. Platform wurde neu kompiliert. Frühere echte Relay-Vollpartien, Revanche und Störtests bleiben getrennt im 0.7-Bericht dokumentiert.

## Freeze und geschützte Apps

Alle Runtime- und Testdateien sind eingefroren. Root verantwortet Unity, Builds, Versionen und Git; unabhängige Reviewer haben ihre Bildbelege beendet. Keine parallelen Runtime-Edits. Neue Änderungen nur nach konkretem Nutzerwunsch oder Fehlerbefund mit Dateizuteilung.

SpeedReview 0.6, ArtReview 0.5, Candidate 0.4, Next 0.3 sowie WaveReview 0.7 und ihre Artefakte nicht überschreiben, schließen oder fernsteuern. Die 0.7-Mactician-AVD DefenseDuellReview wurde installiert/gestartet und bleibt dem Nutzer überlassen. Keine ungefragte 0.8-Neuinstallation darüber. Gebaut, gestartet, automatisch geprüft und menschlich gespielt getrennt benennen.

## Nachweise und offene Grenzen

Regelvertrag: `Docs/Iteration07Contract.md`; Darstellung 0.8 im README/TEST_REPORT. Finale Runtime-/Mac-Bindung: `TestResults/network-v6-compact-v08-source-manifest.json`. Prüfung: `python3 Tools/check_project_state.py --check-frozen`. Historie: `Docs/Iteration08IntegrationHistory.md`; 0.7-APK, Archiv und Bericht bleiben erhalten.

Offen sind Android-0.8-Installation/Gesten, ein physisches Samsung mit 15-Minuten-Frametime-/Speicher-/Thermikprüfung, zwei Menschen über getrennte Internetanschlüsse und menschliche Balance. 0.7 war begrenzt im Android-16/API-36-Emulator bedienbar; direkte Drag-/Pan-/Pinch-Abnahme fehlte wegen instabiler CUA-Steuerung. Keine Nutzerfreigabe offen, kein Billing und keine Store-Veröffentlichung. Nächster konkreter Schritt ist der separate Gerätetest der neuen APK. Kein weiterer Runtimeumbau ohne Befund.

## Aktives Integrationsfenster 0.9 – Grafik, Vollbild-HUD, Einkommen und Audio

Direkter Nutzerauftrag am 11. November: APK funktioniert und lässt sich installieren (Nutzerbeobachtung, kein eigener Gerätetest). Die Karte soll ohne hellen Rahmen hinter dem Overlay liegen, deutlich schärfer und optisch hochwertiger werden. Gewünscht sind eine sichtbare Goldrate pro Sekunde, ein Plus-Feedback für tatsächlich zusätzliches Angriffseinkommen, angenehmere Bauvorschau und bessere Sounds. Neue App 0.9.0 / Android-Code 9; Core v1.9 / Schema 6 / Wire 6 bleiben unverändert. 0.8-Artefakte bleiben erhalten.

Dateizuteilung: HUD-Reviewchat 01a1c6c8-89f2-7ac3-9a13-fac8362270ca besitzt GameUI*.cs, neue Income-/HUD-Helfer und eigene ImmersiveHud09Tests.cs sowie CompactHud08Tests/WaveHud07Tests. Root besitzt zusätzlich BoardView.cs, ScenicTerrain.cs, eigene Render-/Bauvisual-Helfer und RenderQuality09Tests.cs. Der Ansichts-Chat bearbeitet seinen neueren direkten Nutzerauftrag zu zehn Upgrade-Stufen ausschließlich im isolierten Tools/UpgradeEvolutionLab und eigenen Entwurfsbelegen. Audio-Chat 01a1c6a4-1df9-7c61-8217-1713a3dbd799 besitzt GameAudio.cs, eigene Synthese-/Audiohelfer und eigene Audio09Tests.cs sowie Editor-Audiovorschau. Root besitzt Integration, übrige Legacytests, Editor/Versionierung, Porträtbake, Dokumentation, Unity/Build/Git. Kein Owner startet Unity oder überschreibt bestehende Nutzervorschauen.

Schnittstellen früh abgleichen: Der Board-Frame darf Vollbild sein, die Kamera soll in der Übersicht wichtige Spielinhalte innerhalb des nicht verdeckten Bereichs halten. Rendering an tatsächlichen physischen Pixeln und Skalierungsänderungen ausrichten; keine starre 1152er-Hauptansicht. Einkommen aus dem verbindlichen Zustand, Bonus erst beim Spawn und nicht beim Kauf; keine neue Geldregel. Keine konkurrierenden Änderungen an Core/Networking/Progression.

Weiterer direkter Nutzerauftrag im Ansichts-Chat: alle sieben Türme und vier Truppen sollen bis etwa zehn Upgrades teurer, stärker und sichtbar mächtiger werden. Als eigener Folgedurchstich nach der Grafik-/Audiointegration eingeplant; Ansichts-Chat darf Tools/UpgradeEvolutionLab und eigene Entwurfsbelege vorbereiten. Keine stillen Core-/Wire-/Modellregeländerungen während 0.9. Noch nicht implementiert oder als geliefert behaupten.

Neuere Präzisierung im Maps-Chat: Gebäude, Forschung und ausdrücklich auch Truppenkauf/-sendung sollen ohne Mengenlimit möglich sein; Geldkosten, freier Platz und Wellen bleiben bestehen. Diese Präzisierung ersetzt die gedachte harte Zehn-Stufen-Grenze des Folgepakets. Maps-Chat analysiert isoliert Docs/UnlimitedProgressionDesign.md; zehn sichtbare Entwicklungsmeilensteine können als Artkonzept bleiben. Keine Änderung der laufenden 0.9-Core-/Wire-Regeln. Ressourcen-, Integer-, Wire- und Renderinggrenzen müssen für die spätere Umsetzung ausdrücklich gelöst werden.
