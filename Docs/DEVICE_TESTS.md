# Geräte- und Netzwerkabnahme

Dieses Dokument ist eine Testanleitung, kein bestandener Test. Die ausgeführten Nachweise stehen ausschließlich in TEST_REPORT.md.

Der Nutzer möchte zuerst den fertigen Build. Ein Android-Handy ist verfügbar; für die weitere Entwicklung ist es derzeit nicht erforderlich. Die folgende Abnahme beginnt erst danach. Ein Handy plus Desktop kann einen ersten echten LAN-/Relay-Lauf ermöglichen; zwei Android-Geräte bleiben eine getrennte Prüfung.

## Zwei Android-Geräte

Pro Lauf notieren: Samsung-Modell bzw. Gerät, Android-/One-UI-Version, SoC/RAM, APK-SHA256, Build-Typ, Akku/Lademodus, Raumtemperatur, 30/60FPS-Modus, reduzierte Effekte, Netzwerktyp. USB-Debugging nur für die eigene lokale Testverbindung. Mit `adb devices -l` Verbindung bestätigen und `adb install -r Builds/Android/DefenseDuell.apk` installieren.

1. Auf beiden Geräten Offline ohne Internet starten; Tutorial mit Bauen/Senden/Upgrade/Verkauf beenden. Einstellungen speichern und App neu öffnen.
2. Beide ins gleiche normale WLAN. Host-Port7777, Client mit Host-IP. Kein Unity-Login darf erscheinen. Beide bereit → 3s Countdown. Beide kaufen, verkaufen, senden und beenden eine vollständige Partie. Zustände/Ergebnis vergleichen.
3. Rematch nur bei beidseitiger Bestätigung. Zehn Host/Join/Verlassen/Neustartzyklen ohne App-Neustart. Ein einziges Zurück/Bestätigen darf nur einmal Gold ändern.
4. Gastnetz/AP-Isolation gezielt aktivieren: verständlicher Timeout/Abbruch, kein falsches Verbunden. Danach mit normalem WLAN neu verbinden.
5. Host und Client jeweils: Home-Taste, Sperren, App-Wechsel, eingehender Unterbruch, WLAN aus, Routerwechsel. Netzwerkpartie muss definiert abbrechen; kein Sieger bei verlorenem Match. Rückkehr ins Menü und neue Verbindung funktionieren.
6. Kurzer Paketverlust/delay: tatsächliche Konfiguration aus Proxy/Transport dokumentieren, Ziel100–200ms RTT und1–3%Verlust. Keine erfundenen Werte aus bloßer Fenstersimulation.
7. Online separat mit **zwei unterschiedlichen Internetzugängen**, z.B. Heim-WLAN und Mobilfunk, überprüfen. Privater Code, anonyme Auth, DTLS. Zwei Fenster desselben Macs sind kein Android-/NAT-Nachweis.
8. Bildschirmformate16:9 und20:9, Aussparungen/Navigation/SafeArea, Zurücktaste, Eingabefokus und Mehrfachtaps prüfen. Keine Aktion darf durch einen offenen Dialog auf den Weg oder Kaufbutton fallen.
9. Neue Bedienung: Einfinger-Pan und Zweifinger-Zoom, Loslassen in unterschiedlicher Reihenfolge, dritter Finger, Abbruch durch App-Wechsel und wiederverwendete Touch-IDs. Keine Navigation darf einen Kauf oder eine Bodenwahl auslösen. Bau-Drag über einem HUD-Knopf muss ohne Kauf enden; nach Zoom/Pan muss die Vorschau den tatsächlichen Kaufpunkt zeigen.
10. Beide Minen frei platzieren, dritte Mine ablehnen. Schaden/Reichweite in beiden Reihenfolgen kaufen, Gesamtlimit zwei prüfen, Verkauf aus der vollständigen Investition berechnen. Unbezahlbaren Knopf drücken: rotes Feedback, verständlicher Fehlbetrag, keine Abbuchung. Gegner-PiP vergrößern, navigieren und ausdrücklich zurückkehren; dort kein Gebäudekauf.

## Belastung mindestens15Minuten

Maximal vorgesehene Last: 80 Einheiten je Feld, 30 Kampftürme plus zwei Minen je Spieler und 80 bezahlte Aufträge je Spieler. Simulationstest und echte Rendering-/Netzlast unterscheiden. Mit einem Debug-Aufbau oder kontrolliertem Testinhalt Last halten; solche Modifikationen samt Content-Hash protokollieren, nicht als normale Partie ausgeben.

Der lokale PerformanceRecorder wird über Startargument `--duel-performance` oder `PlayerPrefs.SetInt("duel.performance",1)` in einem Testbuild aktiviert. CSV unter `Application.persistentDataPath/performance-*.csv`, je10s ein Fenster: durchschnittliche/p95/p99/max Frametimes, maximale GC-Allokation pro Frame, verwalteter Speicher, Einheiten/Türme, Anwendungs-Netzbytes und Ziel-FPS. Das sind Anwendungsbytes, kein vollständiger IP-/Relay-Abrechnungszähler. Warm-up separat markieren.

Zusätzlich Android-/Unity-Profiler verwenden: Gesamtprozessspeicher, GC-Pausen, Draw Calls und GPU, thermischer Zustand/CPU-Throttling (z.B. `adb shell dumpsys thermalservice`), Akku und Erwärmung. CSV kann reale Temperatur nicht selbst belegen. Frametimes über die gesamte Laufzeit vergleichen, besonders nach5/10/15Minuten. Ausreißer, Speichertrend und Netzwerkverkehr erhalten, nicht nur Mittelwerte.

Abnahmeziele:60FPS angestrebt, stabiler30FPS-Energiesparmodus, keine fortlaufenden Objekt-/Listener-/Task-Zuwächse, keine verlorenen Käufe, höchstens80aktive Truppen je Feld. Werden Ziele verfehlt, erst Profilerbeleg und Ursache dokumentieren, dann Parameter/Darstellung ändern. Kein Editor-FPS-Wert gilt als Samsung-Messung.

## Nachweisvorlage

| Feld | Eintrag |
|---|---|
| Datum / Tester | offen |
| Gerät A / Android | offen |
| Gerät B / Android | offen |
| APK-SHA256 / Content-Hash | offen |
| LAN bzw. Relay / Zugänge | offen |
| tatsächliche RTT / Verlustkonfiguration | offen |
| Laufzeit / Einheiten / Türme | offen |
| p95 / p99 / Maximum / GC-Spitzen | offen |
| Speichertrend / Thermik | offen |
| zehn Neustarts / Rematch / Hintergrund | offen |
| Ergebnis / Belegdateien | nicht geprüft |
