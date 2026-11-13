# Defense Duell

Privates Tower-Defense-Spiel für Android mit deutscher Oberfläche, echten 3D-Karten und Solo-Training. Mit Freunden sind 1v1, 2v2, 3v3 und 2v2v2 möglich. Keine Werbung, In-App-Käufe oder bezahlten Spielassets.

Aktueller Integrations- und Prüfstatus: [PROJECT_STATE.md](PROJECT_STATE.md). Das Gesamtupdate0.16 ist integriert, geprüft und alsAPKgebaut: [FeatureAcceptance16.md](Docs/FeatureAcceptance16.md).

Version0.16.0 / Android-Code17. Installierbare APK lokal mit `bash Tools/unity.sh android` erstellen. Alle Multiplayerteilnehmer benötigen denselben Build. [Prüfbericht](Docs/Release16TestReport.md).

## Starten

1. Mit Unity **6000.3.28f1** öffnen und `Defense Duell > Prepare project` ausführen.
2. `Assets/Scenes/StudioIntro.unity` öffnen und Play starten; `Boot.unity` überspringt das Intro.
3. Im Menü Wellenzahl, Endlosmodus, Einkommensfaktor und Freundesformat wählen. Vor Solo/LAN/Online1–5Turmklassen auswählen und bestätigen.
4. Solo startet ein Duell gegen den Bot. WLAN verbindet Geräte im selben Netz; Online verwendet einen privaten Relay-Code. Der Host bleibt während des Spiels im Vordergrund.
5. In der Lobby Karte, Startgold und Regeln bestätigen. Jeder wählt sein Team und seine eigenen Turmklassen. Alle Plätze müssen besetzt und Teams gleich groß sein; es gibt kein Bot-Fill.

WLAN funktioniert auch über direkte IP. Die App sucht zusätzlich nach kompatiblen offenen LAN-Räumen und bietet bewusstes Beitreten an. Online verwendet technische anonyme Unity-Authentifizierung ohne eigenes Kontoformular. Während einer laufenden Partie führt Verbindungsverlust zum Abbruch; ein bereits bestätigtes Ergebnis bleibt erhalten.

## Regeln des integrierten0.16-Quellstands

- Persönliche Felder, Gebäude, Gold und Forschung; gemeinsame Team-LP: 30 pro Teammitglied. Nur gegnerische Felder können angegriffen werden. Ein bezahltes Paket behält sein gewähltes Ziel.
- Standard: 180 Startgold, 4 Gold pro aktiver Sekunde und zehn Wellen. Der Host kann 1–200 Wellen oder echtes Endlosspiel wählen. Ab Welle10 gibt es angekündigte wechselnde Gegenprofile, Bosse an jedem10er und Superbosse an jedem100er. Gruppen wachsen kapitelweise bis64Körper; ihre individuellen Lebenspunkte werden dadurch nicht kleiner. Endloswellen werden bei Bedarf erzeugt und besitzen keine künstliche letzte Welle.
- Einkommensfaktor 1×/2×/3×/5×/10× multipliziert laufendes Basis-, Minen- und Angriffseinkommen. Startgold, Preise, Verkauf und Abschussprämien bleiben davon getrennt. Während der Baupause ruhen Einkommen und Kampf.
- Regel-, Karten-, Team- und Turmauswahländerungen setzen Bereitschaft zurück. Startgold wird einmal beim gemeinsamen Start gutgeschrieben. Revanche behält die Raumregeln und verlangt erneut alle Bestätigungen.
- Frei auf legalem Boden bauen. Geld, Wege, Hindernisse und Abstände begrenzen den Platz; es gibt kein künstliches Stückzahllimit. Vor der Partie werden1–5 der acht Klassen gewählt; die Mine zählt mit. Niemand kann fremde Türme aufrüsten.
- Klassenspezifische Upgrades und Truppenforschung haben keine feste letzte Stufe. Jeder Ausbaupfad bietet1×/10×/Max mit exaktem Gesamtpreis und einer atomaren Hostbuchung. Exakte große Werte bleiben erhalten. Modelle unterscheiden gekaufte Pfade bis Level 30 bei jedem Ausbau und besitzen spätere Meilensteine bis 100; begrenzte Darstellungsstufen begrenzen keine Spielwerte.
- Goldminen kosten 160 G und liefern anfangs 1 G/s vor dem gewählten Faktor. Kampfgebäude erstatten 65 %, Minen 40 % der Investition. Bezahlte Truppen erhöhen Einkommen erst beim Ausmarsch, bis +3 G/s vor Faktor. Neutrale Gegner geben kein Abschussgold.
- Truppenkarten senden mit einem Tap. Links wischen oder den sichtbaren Upgrade-Hinweis antippen öffnet Forschung; erst ein separater Tap kauft sie. Bereits bezahlte Pakete behalten ihre Werte. Veraltete Angebote werden ohne Zahlung abgewiesen.
- 2×-Tempo und früherer Wellenstart benötigen alle lebenden Teilnehmer. Solo lässt sich direkt umschalten. Einkommensfaktor und Spieltempo sind getrennte Regeln.
- In endlichen Partien entscheidet nach der letzten geräumten Welle die verbleibende Team-Gesundheit; Gleichstand ist Remis. Endlos läuft bis zur Niederlage. Bei drei Teams bleiben besiegte Teams ausgeschieden; Angriffe werden nicht heimlich umgeleitet.

| Klasse | Preis | Aufgabe |
|---|---:|---|
| Schnellfeuer | 65 G | Schnelle Einzelzielangriffe |
| Kanone | 105 G | Flächenschaden mit sichtbarem Explosionsradius |
| Frost | 85 G | Verlangsamung, Dauer- und Reichweitenausbau |
| Panzerbrecher | 120 G | Schwere Einzelziele |
| Goldmine | 160 G | Laufendes Einkommen |
| Blitzspule | 125 G | Kettenangriff mit sichtbaren Blitzen |
| Balliste | 120 G | Durchschlag entlang einer Linie |
| Laser | 180 G | Präzise Strahlpulse und Leistungs-/Reichweitenausbau |

Bezahlte Truppen: Läufer18G, Sprinter24G, Tank65G, Schwarm38G, Sturmkoloss1200G und Belagerungskoloss1600G. Die beiden Eliteklassen haben unterschiedliche Tempo-/Panzerrollen und eigene Forschung. Die Grundtruppen rücken deutlich dichter aus; der Laser feuert alle8 statt5Ticks.

Sechs Karten: Grünes Tal, Sonnenpass, Frosthafen sowie die größeren Smaragdkessel, Bernsteinschlucht und Nordlichtbucht. Die bisherigen Karten haben mehr Baufläche; neue Karten messen 2400×1440 logische Einheiten. Karte und Overlay füllen den Bildschirm, Zoom und Verschieben bleiben verfügbar.

## Entwicklung und Nachweise

Vertrag: [CONTRACT.md](Docs/CONTRACT.md). Aktuelle Featureabnahme: [FeatureAcceptance16.md](Docs/FeatureAcceptance16.md). Historische Tests bleiben versionsgebunden und beweisen keine spätere Runtime. Native Suiten laufen seriell über `Tools/unity.sh`; echte Mehrprozess-Tests stehen in [README15.md](Tools/NetworkTests/README15.md). Nur der Hauptchat koordiniert Unity, Builds und Releases.

Eine APK-Prüfung ersetzt keine Handyabnahme. Android-Touch, Hardwareaudio/-video, Leistung und zwei menschliche Spieler werden separat ausgewiesen. iOS-Exportvorbereitung: [iOSPreparation.md](Docs/iOSPreparation.md); installierbare iPhone-Builds benötigen zusätzlich Xcode, Unity-iOS-Modul, Signierung und Geräteprüfung.
