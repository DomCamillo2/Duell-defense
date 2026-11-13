# Integrationsvertrag 0.11 – Sendefluss und Minenwirtschaft

Stand:13. November2026. App0.11.0/Android-Code11, Core2.1, Content-Schema8, Wire7. Umsetzung und Prüfung laufen; kein Buildnachweis. Der Nutzer beauftragte die Wirtschaftsverbesserungen direkt im gemeinsam koordinierten Projektchat. Alle übrigen Regeln aus Iteration10Contract.md bleiben erhalten.

## Änderungen

Goldmine160G statt140G, Verkauf40% der tatsächlichen Investition; Kampftürme weiterhin65%. Kein dynamischer Aufschlag nach Anzahl der Minen. Alle ganzzahligen Beträge und großen Ausbauten bleiben exakt. Sendecooldowns für Läufer/Sprinter/Tank/Schwarm12/20/30/24 Simulationsticks. Sendefenster mindestens600Ticks (30aktiveSekunden bei1×); letzteWelle schließt anschließend weitere Käufe, bezahlteFIFO-Aufträge werden vollständig verarbeitet. 2× verändert die reale Dauer, nicht die Simulationsregeln.

Die Warteschlange zeigt noch gebundenes Gold ausschließlich für nicht gespawnte Einheiten. Prognosen unterscheiden erste Einheit, Abschluss der gesamtenFIFO und ersten Spawn eines zusätzlich angebotenen Pakets. Unbekannte zukünftige Wellenwartezeiten werden als offen markiert, nicht als genaue Restzeit dargestellt. Vier zusätzliche Kaufprognosen höchstens4Hz; tatsächliche Kaufquote und Hostprüfung bleiben aktuell. Alle Prognosen sind lesend.

## Evidenz und Kompatibilität

Baseline/Kandidat jeweils360 reale Simulationspartien mit Seitenwechseln und allen3Karten dienen dem Vergleich, nicht als menschliches Balanceurteil. Kürzere Sendewartezeit und weniger dominierende Minen sind beabsichtigt; strategische Gewinner/Verlierer bleiben ausgewiesen. Neue102 reineCore-Szenarien einschließlich FIFO, BigInteger-Refund, letzterWelle und1×/2× müssen auf integrierter Quelle wiederholt werden.

Wirelayout bleibt7. Neue Regeln und Contenthash verhindern inkompatible gemeinsame Partien mit0.10. Beide Spieler benötigen denselben neuen Build. NativeContentmigration, HUD-/Regressionstests, realeLAN-/Relayprozesse, Androidsignatur, frischerImport und versionsgebundene Quellen-/Binärnachweise werden getrennt protokolliert. VorherigeAPK0.10 bleibt als Zwischenstand erhalten.
