# Integrationsvertrag0.15

Der Nutzer verlangt die nächste APK erst nach Integration aller beauftragten Funktionen aus den Projektchats; die frühere Reihenfolge „200Wellen zuerst“ ist aufgehoben. Nach vollständiger Integration hat die APK Vorrang. Root besitzt Integration, Unity und Releases. App0.15.0/Android16, Core2.4, Schema9, Wire9.

## Verbindliche Funktionen

- Solo und Duell erhalten; Freunde zusätzlich2v2,3v3 und2v2v2 mit4/6 echten Teilnehmern. Vollständig besetzte ausgeglichene Teams, eigene Felder/Wallets/Türme/Forschung/Loadouts, gemeinsame Team-LP mit30jeMitglied. Kein Bot-Fill. Gegnerisches Angriffsziel wird pro bezahltem Paket festgehalten; fremde Türme bleiben fremdes Eigentum.
- Wellenzahl im Menü und der Hostlobby mit Slider und exakter Zahl1–200; zusätzlich echtes Endlos. Solo/Host übernehmen die Auswahl, Gäste sehen den bestätigten Stand. BigInteger-Wellen und unveränderliche dynamische Rezepte; Endlos endet bei Teamniederlage und hält nur aktuelle/nächste Wellenkonten vor.
- Einkommensfaktor1/2/3/5/10 für alle Spieler. Er multipliziert die laufende Summe aus Basis-, Minen- und nach bestehender Obergrenze berechnetem Sendebonuseinkommen. Startgold, Preise, Refund und Kopfgeld bleiben unabhängig. Exakte1/960-Reste, HUD und Feedback; unabhängig vom2×-Tempo.
- Individuelle Ausrüstung aller acht Turmklassen in der Wartelobby, mindestens eine ausgewählt, ohne zusätzliche Vier-Slot-Grenze. Eigene Änderungen setzen Lobbybereitschaft zurück; Teammitglieder dürfen verschiedene Auswahlen haben.
- Hostkartenwahl mit Gastsync und Readyreset. Drei zusätzliche2400×1440-Dioramakarten mit eigenständigen Wegen, größeren erhöhten Landschaftsmassen und thematischen Details. Bestehende drei Wege bleiben erhalten und gewinnen nutzbaren Bauplatz durch abgestimmte Blockeranpassung. Sichtbare Landschaft und echte Bauprüfung bleiben konsistent.
- Größere Sendekarten; Links-Swipe öffnet Forschung, sichtbare Tapalternative. Swipe kauft/sendet niemals. Forschung nur über separate Aktion mit aktuellem Quote/ACK. Gemeinsame Pointer-/Modal-/Fokus-/Rebuildsicherheit und reduzierte Effekte. Alle Zwischenscreens und Dialoge dunkel, responsiv und lesbar; Ergebnisbuttons innerhalb des Panels.
- Tesla zeigt gezackte Blitze; achte Klasse ist der Laserturm mit eigener Beamdarstellung und echten Reichweiten-/Schadenspfaden. Konkrete Startwerte liegen im validierten Content. Keine unbeauftragte Focus-Ramp. Begrenzte Effektressourcen, reduzierte Effekte und sauberes Aufräumen.
- Echte pfadspezifische Turmformen für jeden gekauften Gesamtlevel bis30, spätere40/50/75/100-Meilensteine und beide Pfadmodule.2074Porträts aus denselben Modellen; kein unscharfes Ersatzbild für die Spielfeldgeometrie.
- Automatische LAN-Raumerkennung in der aktiven App mit explizit Beitreten/Später. Kompatible beitretbare Hosts, Absender-IP, Dedupe/TTL, Pause/Netzwechsel und kein störendes Popup im Match. Kein Autojoin oder OS-Push.
- Vorherige unbegrenzte Upgrades, genaue Goldwerte, Drag-/Abwahlkorrektur, Gastglättung, Tempo, Kanonenradius, Intro, Sounds und HUD bleiben integriert.

## Abnahme und Grenzen

[FeatureAcceptance15.md](FeatureAcceptance15.md) ordnet die Funktionen aktuellen Core-, Wire-, UI-, Bild- und realen Transportbeweisen zu. Vollständige native Suiten, echte4/6LAN-/Relayprozesse, Endlosfortschritt über200, exakte Wirtschaft und Gestenregressionen sind eigenständige Beweise. Ein Modell ist kein echter Transport; Desktop ist kein Androidgerät. Quellfreeze, Builddelta und tatsächliche Artefakthashes werden getrennt erfasst.

Androidhardware, menschliche Partien, getrennte Internetzugänge und15-Minuten-Leistung/Thermik bleiben externe Abnahme. iOS-Exportvorbereitung bleibt enthalten; installierbarer iPhonebuild/Crossplay benötigen fehlendes SDK/Unitymodul/Xcode/Signierung/Gerät. Kasernen wurden ausdrücklich als Plan beauftragt. Mortar, Reconnect und Hostmigration waren Vorschläge und erweitern die Runtime nicht stillschweigend.

Alle Übergaben sind inzwischen an Root zurückgegeben. Konkrete aktive Dateifenster und tatsächlicher Buildstatus stehen in [PROJECT_STATE.md](../PROJECT_STATE.md). Keine parallelen Unityprozesse in derselben Projektkopie, keine konkurrierenden Runtimeedits, keine Übernahme laufender Nutzervorschauen.
