# Historischer Integrationsstand vor Abschluss0.8

# Defense Duell – verbindlicher Projektstand

Stand: 11. November 2026. Root / Hauptchat `01a1c67c-8801-7cf2-9b86-2b5353ef4b76`, Host local, ist alleiniger Integrations-/Release-Owner. AGENTS.md und Docs/CONTRACT.md beachten.

## Aktueller Kandidat 0.7 – gebaut und als Checkpoint gesichert

App 0.7.0 / Android-Code 7, Core v1.9, Content-Schema 6, Wire 6. Die Runtime ist integriert und für die Transport-/Geräteabnahme eingefroren. Neue Änderungen nur für konkrete reproduzierte Mängel mit passender Nachprüfung. Die früheren Entwurfs-/Kein-GO-Vermerke sind historisch; sie stehen jetzt in Docs/Iteration07IntegrationHistory.md.

- Zehn Grundwellen, Baupause mit beidseitigem Frühstart, beidseitiges 2× und Rücknahme; Vorbereitung pausiert Gameplay/Einkommen.
- Angriffseinkommen nach tatsächlichem Spawn, klare Kaufzielwelle/FIFO, klassenbezogene Forschung 0–8, Zielpriorität Vorne/Stärkster.
- Drei Karten mit markanten Orten, sieben Turmklassen und 43 Porträts aus tatsächlichen Modellen. Blitzspule/Balliste über lokale Siegesmarken freischaltbar; alle Starterklassen bleiben verfügbar.
- Solo Leicht/Normal/Schwer mit gleichen Regeln; lokales Profil mit dauerhaftem Kauf und einmaliger Siegprämie, auch bei temporärem Speicherfehler geprüft.
- 123/123 EditMode und 63/63 PlayMode bestanden. Danach kleine HUD-Text-/Farbänderungen und diagnostische ACK-Wartekorrektur: 11/11 gezielte HUD-Tests und finaler Mac-/Android-Compile bestanden. Keine pauschale Gleichheit des früheren 117-Dateien-Manifests mit allen finalen Dateien behaupten.
- Mac: Builds/macOS-WaveReview/DefenseDuell.app, Bundle de.defenseduell.preview.waves, Fenster Defense Duell 0.7.0. Native Bedienung geprüft; Reviewer hat nur diese neue App danach geschlossen. Headless Acceptance-Player sind regulär beendet.
- APK: Builds/Android/DefenseDuell-0.7.apk, 45.349.535 Bytes, SHA-256 85186f2658658aa44e475ffb1776900cc7afeb03d17cd0c7dc639cd7ef0be931. ARM64/IL2CPP/GLES3, APK-v2 Debug-Testsignatur. Android-Installation und Start in eigener Mactician-AVD DefenseDuellReview (Android16/API36/ARM64/1920×1080) bestätigt. Bauen per Bestätigung, Upgrade/MAX/Priorität, Sendung/FIFO, Forschung und PiP/Zoom geprüft. CUA-Fenstersteuerung instabil; Drag/Pan und weitere Androidabläufe nicht abschließend geprüft. Keine offene Nutzerfreigabe; frühere Wartezustandsmeldung war falsch.
- Reale Netzwerkprüfung bestanden: LAN/Relay mit je zwei vollständigen Partien/Revanche und zwei gestörte LAN-Szenarien mit Neustart. Alle sechs Prozesse Exit0; finale Quellen/Binaries unverändert. Vollpartien endeten legal durch beidseitigen Basisverlust in Welle2, zehn Netzwerkwellen sind nicht behauptet. Belege in TEST_REPORT.md.
- Frischimport ohne Library und separate43-Porträtprüfung bestanden in Builds/FreshImport-20261111-104237. Implementierungscheckpoint: **0c97ad6**. Quellenarchiv: Artifacts/DefenseDuell-0.7-Source.zip, Größe/SHA in TestResults/source-artifacts-v07.json. Begrenzte Android-Emulationsabnahme dokumentiert; physische Hardware und direkte Gesten bleiben offen.

## Zuständigkeiten und geschützte Apps

Alle Core-, Networking-, Bot-, HUD- und Modell-Owner haben ihre Implementierungen eingefroren übergeben. Root integriert, baut, prüft und versioniert. Bot-/Reviewchat aktualisiert Docs/FinalUXReview.md und eigene Belege. Designchat prüft ausschließlich die übergebene finale APK in der tatsächlich identifizierten Mactician-Android-Instanz. Keine konkurrierenden Runtime-Edits oder gemeinsamen Unityprozesse.

SpeedReview 0.6, ArtReview 0.5, Candidate 0.4 und Next 0.3 weder schließen noch überschreiben oder fernbedienen. Neuer Quellstand, gebautes Artefakt und tatsächlich vom Nutzer gespielte Version sind getrennte Zustände. Historischer 0.6-Checkpoint: 590a183, APK und Quellenarchiv bleiben erhalten.

## Nachweise und offene Grenzen

Aktueller Vertrag: Docs/Iteration07Contract.md. Laufende/finale Nachweise: TEST_REPORT.md, Tools/NetworkTests/Results/v6/handoff.json, TestResults/network-v6-wave-v07-source-manifest.json und die versionsgebundenen XML/JSON-Dateien. Read-only: `python3 Tools/check_project_state.py --check-frozen`.

Automatische Bot-/Zwei-Prozess-Partien sind keine menschliche Spaß-/Balanceabnahme. Mactician ist keine physische Samsung-, Thermik- oder 15-Minuten-Leistungsprüfung. Zwei getrennte Internetanschlüsse und zwei menschliche Mitspieler bleiben separat zu testen. Kein Billing, keine bezahlten Assets und keine Veröffentlichung.

Nächster konkreter Schritt: direkte Drag-/Pan-/Mehrfinger-Gesten sowie Tempo/Minen/Revanche auf physischem Samsung oder stabil bedienbarer Androidumgebung prüfen. Danach15-Minuten-Leistung und menschliche Balance separat messen. Kein weiterer Unity-/Runtimebau notwendig ohne konkreten Fehlerbefund.

## Neues Integrationsfenster 0.8 – HUD entzerren

Direkter Nutzerbefund im Reviewchat am11.November: „zu voll“, belegt mit dem laufenden Androidbild. Die funktionale0.7-Abnahme ist kein visueller Abschluss dieser neuen Rückmeldung.0.7-Artefakte bleiben unverändert erhalten; der kleinere HUD-Folgestand erhält App0.8.0/Android-Code8, neue Mac-Vorschau Builds/macOS-HudReview/DefenseDuell.app. Corev1.9/Schema6/Wire6 und Balance bleiben unverändert.

Exklusive Umsetzung: Reviewchat01a1c6c8-89f2-7ac3-9a13-fac8362270ca darf Assets/Presentation/GameUI*.cs, BattleInfoPanel.cs und Assets/Tests/PlayMode/Presentation/WaveHud07Tests.cs sowie eine eigene CompactHud08Tests.cs ändern. Ziel: kompakter Status, kein breiter aktiver Wellenbanner, leere FIFO verborgen, kompakte nichtleere FIFO mit Details, Info in Angriffskarten, gesperrte Türme nur im Menü. Echte Wellen-/Forschungsquote pro Kaufkarte sichtbar lassen, da Zielwellen je Preis differieren können. Keine Änderungen an Core, Networking, Progression, Board/Diorama/Assets, PointerGate/DragSource, Editor/Versionen oder fremden Tests ohne konkrete Übergabe.

Root ist alleiniger Integrations-/Build-/Versions-/Unity-Owner und unabhängiger Reviewer der UI-Änderung. Bisheriger Ansichts-Chat bleibt eingefroren. Gerätechat beendet lediglich seinen0.7-Bericht und verändert/installiert keinen Folgestand ohne Rootübergabe. Keine parallelen Runtimeedits. Der kompakte Entwurf und die Prüfmatrix stehen in Docs/FinalUXEvidence/compact-hud-request-v07.md.

0.8-Prüffenster: Prepare bestanden, unveränderterContenthash429ab672fc464511fbc3ff3f99d090bea03be6ab0db5e753b98be3cb36c53a6b. Erster vollständigerPlayMode-Lauf63/71PASS,8Fehler; Belegeplaymode-v08-first.xml/.log undHud-v08-first geschützt. Root migriert alteConnectedGame-Menü-/DragLayout-Fixtures; UI-Owner korrigiert neueRaycastfixture-Zeitpunkte sowie realenMenütextumbruch/Scrollbarüberstand. Nochkein0.8Build. Nur zugeteilteDateien offen; nächsterLauf nacherneutemFreeze.
