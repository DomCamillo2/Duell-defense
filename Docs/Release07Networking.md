# Netzwerk 0.7 / Wire 6

Der SessionCoordinator besitzt Simulation, NGO-Manager, Transport, Listener und asynchrone Starts. Offline verwendet dieselbe Simulation mit Bot ohne Cloud. WLAN verwendet NGO und UDP; Online private Relay-Räume über DTLS mit technischer anonymer Anmeldung. Kartenwahl und lokale Freischaltungsmaske sind nur im Hauptmenü änderbar. Kein Billing wurde aktiviert.

## Aktueller Vertrag

- Wire 6, Core v1.9 und Content-Schema 6. Alte Peers werden abgewiesen. Handshake 74 Byte, Welcome-Kopf 79 Byte, Command 41 Byte einschließlich erwarteter Zielwelle und Forschungsstufe.
- Der Host validiert alle Käufe, Forschungs- und Prioritätsbefehle. Abweichende Kaufquote wird ohne Zahlung und ohne automatischen Kauf-Retry abgewiesen. Wiederholte identische Requests erhalten die ursprüngliche Quittung ohne zweite Mutation.
- 20 Kontrollschritte pro echter Sekunde. Aktive Phasen führen pro Kontrollschritt einen oder bei beiderseitigem 2× zwei Spielschritte aus. Vorbereitung führt keinen Spielschritt aus. Einkommen und Cooldowns pausieren; Netzwerkfristen laufen in Echtzeit.
- Beide müssen 2× zustimmen; jeder kann zurücknehmen. Wellenbereitschaft ist eine getrennte Abstimmung. Revanche erhält die individuellen Freischaltungen, setzt Ressourcen, Forschung, Bereitschaft und Tempo zurück und verwendet eine neue Sitzung.
- Snapshots validieren vollständige Frames einschließlich Wellen/Forschung, Herkunft, FIFO, Investition, Geometrie und Einkommen. 5 Hz, 880 Byte Payload je Chunk, unveränderte Obergrenze 16.384 Byte. Konservativer gültiger Schema-Maximalzustand: 12.241 Byte in 14 Chunks; 61.205 Payload-Byte/s ohne Transport-Overhead. Die Maximalfixture benutzt ausdrücklich einen größeren gültigen Testplan als das Standardspiel.
- Die lokale Freischaltungsmaske wird deklariert und hostseitig für Bauentscheidungen festgehalten. Lokale Profile und ein privater Host sind Vertrauensgrenzen, kein serverseitiger Cheat-Schutz.
- Abbruch, Vordergrundwechsel und Timeouts erzeugen keinen erfundenen Sieger. Handler und Pending-Befehle werden beim Shutdown entfernt. Keine Host-Migration.

Byteformel, Schnittstellen, negative Belegtests und die aktuellen Runner stehen vollständig in [Tools/NetworkTests/README.md](../Tools/NetworkTests/README.md). Versionsgebundene Transportergebnisse stehen in [TEST_REPORT.md](../TEST_REPORT.md); reine Codec-/Treibermodelle sind keine Transportprüfung.

## Diagnose

Die opt-in Acceptance-Player aktivieren sich nur mit `--duel-host`, `--duel-client` oder den Relay-Varianten. Sie benutzen normale Spielbefehle und unveränderte Ressourcen; das lokale Nutzerprofil wird in diesem Diagnosemodus weder gelesen noch belohnt. Hostprotokolle halten Befehlszustände vor/nach Annahme fest; der unabhängige Python-Vergleich dekodiert vollständige Binärframes. Vollpartien verlangen Resultat und beidseitige Revanche. Der Störproxy verzögert echte Datagramme und verwirft einen konfigurierten Anteil.

Die frühere Netzwerkbeschreibung ist als [Release06Networking.md](Release06Networking.md) historisch erhalten. Zwei Prozesse auf einem Mac belegen keine zwei Android-Geräte oder getrennten Internetanschlüsse.
