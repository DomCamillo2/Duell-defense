# Netzwerk / Wire 7

SessionCoordinator bleibt alleiniger Besitzer der Simulation und Verbindung. Core v2.0 verwendet exakte BigInteger für progressive Gold-, Forschungs-, Turm- und HP-Werte; alte Wire6-Peers werden beim Handshake abgewiesen. Offline, WLAN und Relay verwenden dieselben autoritativen Spielregeln.

Snapshots haben Int32-Entitätsanzahlen und Slot-IDs ohne frühere Spielobergrenzen. Kanonische Ganzzahlen, komplette FIFO-Rezepte, Ledger, Investition, Geometrie und Zustand werden vor Übernahme geprüft. Seiten tragen24Byte Kopf plus maximal880Byte Daten. Große Zustände und Welcome werden zuverlässig mit maximal16 ungequitteten Seiten übertragen. Neue Revisionen warten auf den Abschluss und werden zusammengefasst; bezahlte Aufträge werden nicht zur Darstellung oder Paketgröße verworfen. Der Empfänger bestätigt explizit neu angenommene Seiten, auch wenn dabei eine unvollständige ältere UDP-Revision ersetzt wird. Bereits quittierte Seiten bleiben bis zum tatsächlichen Verbindungs-/Sitzungsreset erhalten. Reguläre kleine Snapshots bleiben bei5Hz.

Der Gast stellt Wegentfernungen zeitversetzt zwischen vollständigen Host-Zuständen dar. Diese Darstellung interpoliert und prognostiziert keine Spielaktionen. Üblich sind250ms Darstellungsverzögerung; Paketlücken halten die letzte bekannte Position. Pause, Revanche und Feldwechsel löschen den Verlauf. Turm-, Einheiten-, Touch- und Vorschau-Pools wachsen anhand tatsächlich sichtbarer Entitäten, nicht anhand einer möglicherweise großen Slotnummer.

Der vollständige Vertrag einschließlich Seitenquittungen, Grenzen für ungültige Eingaben, reproduzierbaren Runnern und Belegumfang steht in [Tools/NetworkTests/README.md](../Tools/NetworkTests/README.md). Stand der Übergabe: Runtime integriert; isolierte Codec-, Bewegungs- und Treibermodellprüfungen bestanden. Unity-, echte LAN-/Relay-, Android- und APK-Prüfungen werden getrennt vom Hauptchat dokumentiert. Zwei lokale Prozesse belegen keine zwei Android-Geräte oder getrennte Internetanschlüsse.

Die vorherige Wire6-Beschreibung bleibt in [Release07Networking.md](Release07Networking.md) erhalten; frühere Nachweise werden nicht auf Wire7 übertragen.
