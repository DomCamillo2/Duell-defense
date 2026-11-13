# Integrationsvertrag 0.13 – Bildschirmränder und Verbindungseinstieg

App0.13.0/Android-Code13, Core2.1/Content8/Wire7 unverändert. Zwei direkte Nutzerwünsche nach0.12: heller linker Home-Rand bei Displayaussparung und unpassend blasse WLAN-/Online-Verbindungsseite.

Der Hintergrund füllt das gesamte Canvas in Navy für Home und Connect. Die Bedienflächen bleiben innerhalb Screen.safeArea. Rotation, links/rechts83PixelInset sowie obere/untereInsets dürfen keine hellen Randstreifen oder verschobenen Touchziele erzeugen. Wiederaufbau, modale Dialoge und Rückkehr ins Home müssen denselben Zustand herstellen.

Connect erhält eine passende Kopfzeile, Create/Join-Karten, klare Eingabelabels, kontrastreiche Felder und separate technische Hilfe. Bestehende Host/Join-/Port-/Online-Callbacks und Formwerte bleiben verbunden; kein Transport- oder Regelwechsel. Eine Beispiel-IP gehört nur in den Placeholder. Öffnen der Form startet keine Verbindung, Hilfe startet keine Services und Formwerte überstehen Rebuild/Navigation.

0.12 bleibt geschützt. Root integriert Routing und Version, führt native Layout-/Raycast-/Form-/Validierungsprüfungen, vollständige Regressionen und Build-/Transportstarts aus. Android-Hardware/SafeArea des tatsächlichen Geräts wird durch emulierte native Insets nicht als getestet behauptet. Tatsächliche Nachweise folgen in Release13TestReport.md.
