# Integrationsvertrag 0.14

Stand 13. November 2026. Basis: Checkpoint cdbe31d937c39682033d3506585dd64249073ad0, ausgelieferte 0.13 bleibt geschützt. Ziel App 0.14.0 / Android-Code 14, Core 2.2, Wire 8. Content-Schema bleibt 8, solange das gespeicherte Basiskatalogformat unverändert ist. Status: integriert; tatsächliche Test-/Buildnachweise stehen im separaten Release14TestReport.md.

## Umfang

Hostregeln im bestehenden Zweispieler-LAN/Relay: vor Start 1–10 tatsächlich vorhandene Wellen (Präfix, keine erfundene Fortsetzung), nichtnegatives exaktes Startgold pro Spieler; Default 10/180. Keine Preis-/Einkommensänderung. Keine Goldgutschrift durch Apply oder Wiedereintritt; Initialisierung einmal beim verbindlichen Start. Solo bleibt mit bisherigen Defaults kompatibel, Revanche behält bestätigte Optionen und verlangt neue Zustimmung.

Host-only Apply in Lobby bindet Configrevision. Wirksame Änderung setzt alle Readys zurück, wirkungslose/ungültige Änderungen nicht. Ready bindet angezeigte Config-/Rosterrevision, Sessionepoch und monotone Befehlsnummer. Nach letzter gültiger Bereitschaft atomarer Start. Alle Zustandswege erhalten hostseitige monotone Publikationssequenz; verspätete ACKs können Requests auflösen, aber keinen neueren Zustand überschreiben. Countdown/Playing darf in gleicher Epoche nicht zurück in Lobby. Neue Revanche-Epoche ausdrücklich validieren; keine stillen Überläufe.

Basiskataloghash und effektiver Regel-/Plan-Hash getrennt. Welcome validiert Optionen und effektiven Content vor wellenabhängigem Snapshotdecode. Technische Transport-/Goldgrenzen werden explizit validiert und als Fehler angezeigt, nie gerundet/geklammert. Core/API-Details werden im Eigentümerabgleich vor Netzwerk-/HUD-Anbindung festgelegt.

Kanonen: tatsächlichen SplashRadius als orange Boden-Vorschau und Explosion darstellen, Angriffsreichweite separat. VFX speichern Radius bei sichtbarem Schussbeginn; Upgrades ändern alte Fluggeschosse nicht. Bestehende snapshotabgeleitete VFX sind keine neuen autoritativen Trefferereignisse. Vorhandene 24/8-Budgets, statische Vorschau auch bei reduzierten Effekten.

## Getrennte Folgeaufträge

4/6 menschliche Spieler, größere Teamkarten und Kasernen sind weiterhin isolierte Modelle; keine Behauptung einer fertigen Team-Runtime in 0.14. iOS-Exportvorbereitung darf als Editorwerkzeug integriert werden; fehlendes Xcode/iOS-Modul und Signierung verhindern aktuell einen installierbaren iPhone-Build. Crossplay benötigt reale Geräteabnahme.

## Abnahme

Aktuelle Core-/Wire-/HUD-Regressionen und neue Regeln/Ready-/Epoch-/Payloadtests; native Radiusdarstellung mit Bildern; echte LAN-/Relayläufe einschließlich konfigurierter Regeln und letzter gewählter Welle; neuer separater Desktopbuild und APK, Quellbindungen und Quellenpaket. Geräte-/Zwei-Menschen-Tests bleiben getrennte Aussagen. Root allein führt Unity, Builds, Versionsstand und Checkpoints.
