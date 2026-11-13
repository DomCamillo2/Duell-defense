# Defense Duell 0.14 – Integrations- und Prüfbericht

Status: integriert,181/181 EditMode und171/171 PlayMode bestanden, Mac und Android gebaut. Echte Netzwerkabnahme bestanden. Durch neuen direkten200-Wellen-Auftrag folgt0.14.1; diese0.14-APK bleibt geschützter Zwischenstand. Geschützter Vorgänger: 0.13, Checkpoint cdbe31d937c39682033d3506585dd64249073ad0.

## Änderungen

Host konfiguriert Wellenpräfix und exaktes Startgold pro Spieler in der Zweispielerlobby. Bestätigte Änderungen setzen Bereitschaft zurück; neue Zustands-/Befehlsrevisionen verhindern Zustimmung zu veralteten Regeln. Lobby zeigt konfiguriertes Gold; die Wallet wird einmal beim gemeinsamen Start gefüllt. Effektive Regeln werden vor Snapshotvalidierung abgeleitet. App0.14/Core2.2/Wire8; Basiskatalogschema8 bleibt erhalten.

Kanonen zeigen tatsächlichen Explosionsradius getrennt von Angriffsreichweite; Schüsse behalten den Radius vom sichtbaren Schussbeginn. Statische Auswahl bleibt bei reduzierten Effekten verfügbar. Darstellung basiert weiterhin auf Snapshots, nicht auf neu eingeführten autoritativen Trefferereignissen.

Ein Editorhelper bereitet einen gemeinsamen iOS-Xcode-Export vor. Fehlendes Xcode/iOS-Modul und fehlende Geräteabnahme bedeuten: kein installierbares iPhone-Artefakt, kein nachgewiesenes Android/iPhone-Crossplay.

## Native und reine Prüfungen

181/181 finale EditMode-Fälle bestanden. Die gezielte native Lobby-/Radiussuite bestand10/10 nach Behebung eines echten VFX-Zeitfehlers: Trigger und Poolupdate verwenden nun dieselbe doppelt genaue Framezeit; neu ausgelöste Explosionen werden im selben Frame gezeichnet. Der erste9/10-Lauf bleibt erhalten. Die vollständige PlayMode-Wiederholung besteht171/171 nach Migration der alten Snapshot-Reflectionfixture auf den tatsächlichen Zustandsumschlag. Erster Vollsuitefehler170/171 und erster EditModefehler180/181 bleiben erhalten; der EditModefehler war eine veraltete Enumfixture, die den neu vergebenen Befehl10 noch als ungültig behandelte.

Reine Produktionsprüfungen:22 neue Core-Regelfälle,102 bestehende Corefälle und156.307 Wire-/Corechecks mit24 vollständigen Botpartien bestanden. Die102-Fälle wurden nach Entdeckung eines falsch gebundenen `dotnet run`-Nachweises neu gebaut und über die exakte DLL ausgeführt; die ursprüngliche Aussage ist ausdrücklich invalidiert, nicht als Beweis wiederverwendet.86 Pythonvergleichstests bestanden, einschließlich unabhängig gelesener echter C#-Wire8-Goldendaten.48 reine Wiretests und zehn modellierte Host/Gastpaare mit20 vollständigen Runden sind zusätzliche Modellbelege, keine echten Transporte.

Acht Lobby- und fünf Kanonenbilder wurden einzeln gesichtet und unter `Docs/FinalUXEvidence/hostrules-v14/review-01/` unverändert gesichert. Telefon-Insets sind synthetisch, Gastansichten isolierte Panelprüfungen. Die278 Porträts wurden aus den Spielmodellen neu gebacken und verifiziert; Geometrie bleibt gleich, maximaler RGB-Unterschied zum0.13-Porträt beträgt1/255, Alpha ist identisch.

## Gebaute Artefakte

APK: `Builds/Android/DefenseDuell-0.14.apk`, **71.324.648 Bytes /68,02 MiB**, SHA256 `fe8d0fab338ea1c76b2de1998e0e7aa587efea29e69cfc91a1dfbb16ee73f0ae`. App0.14.0/Code14, Paket `de.defenseduell.game`, ARM64/IL2CPP/GLES3, minAPI25/target35. Developmentbuild mit verifizierter Android-Debug-Testsignatur/APKv2. Keine großen ungenutzten ZIP-Lücken; finale Intro-MP4 exakt im APK nachgewiesen. Nicht auf Android installiert oder gestartet.

Mac: `Builds/macOS-HostRulesReview/DefenseDuell.app`, Bundle `de.defenseduell.preview.hostrules`,631.822.720Bytes/310 gebundene Dateien. Eigene automatisierte Testprozesse laufen, keine Nutzervorschau übernommen.

Frischimport ohne Library in `Builds/FreshImport-20261113-152052`: Prepare und278Porträts bestanden. Alle1128 gebundenen Quell-/Assetdateien stimmen nach beiden Builds weiterhin mit dem final nativ getesteten Stand überein. Manifest: `TestResults/network-v8-hostrules-v14-source-manifest.json`.

## Echte Netzwerkabnahme

Echte0.14-Spielerprozesse: je zwei konfigurierte3-Wellen-Partien bisWelle3 mitRevanche überLAN undRelay/DTLS bestanden. Je98Hostbefehle, alleProzesseExit0. Zusätzlich zwei1-Wellen-Partien mit0Startgold übergestörtesLAN bestanden (75msVerzögerung+15msJitter und2%Verlust proRichtung). Standard-LAN mitMehrfachkäufen/Tempowahl:454gemeinsameSnapshots/62Hostbefehle, Start-/Aktionszyklus bestanden. Diese0.14-Beweise gelten nicht automatischfürdas200-Wellen-Addon.

## Grenzen und Folgeaufträge

4/6 menschliche Spieler, größere Teamkarten und Kasernen sind weiterhin getrennte Entwürfe. Bestehende unbegrenzte Ausbauten, Vollbildkarte, Gastglättung, Dragkorrektur, Sounds und Goldfeedback bleiben Teil der Runtime. Handygesten, Tastatur, Hardwareleistung, zwei Menschen und getrennte Internetanschlüsse benötigen reale Abnahme.
