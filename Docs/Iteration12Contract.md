# Integrationsvertrag 0.12 – Studiointro und Startmenü

Stand:13. November2026. App0.12.0/Android-Code12; Core2.1, Content-Schema8 und Wire7 unverändert. Zwei zusätzliche direkte Nutzeraufträge aus den koordinierten Projektchats.0.11 bleibt als vollständig geprüfter APK-/Quellcheckpoint e6fc24b erhalten. Dieser Vertrag ist kein Test-/Buildnachweis; tatsächliche Ergebnisse stehen in Release12TestReport.md.

## Studiointro

Die bereitgestellte Soballa-Studio-Animation läuft vor dem vorhandenen Boot-Menü.105Frames,25fps,4,2Sekunden,stumm. H264Main720×406 aus720×405 mit einer Paddingzeile; keine inhaltliche Neugenerierung oder vorgetäuschte Auflösungssteigerung. VideoPlayer und RawImage verwenden eine eigene freizugebende RenderTexture. Boot wird asynchron vorgeladen, Aktivierung bis zum Ende/zulässigenSkip gehalten. Der Übergang verwendet eine kurzlebige schwarze CanvasGroup über dem Szenenwechsel, bis das Menü initialisiert ist.

Skip frühestens nach1,5 sichtbaren aktiven Sekunden ab erstem dekodierten Frame. Fokus/Pause stoppen Video und Timer. Fehlendes/fehlerhaftes Video, Vorbereitungstimeout oder Stillstand führen weiter zu Boot. Deaktivierung/Zerstörung darf Unitys globale Asyncqueue nicht bei0,9 blockieren. Es entsteht kein zweites Menü und kein zweiter EventSystembesitzer. Automatisierte Headless-Netzwerkspieler umgehen das Intro zuverlässig; native Editor-Videotests können es ausdrücklich einschalten.

## Startmenü

Gemeinsame Navy/Teal/Gold-Gestaltung mit dem Spiel-HUD. Echtes Karten-Diorama, deutlicher Solo-Einstieg mit Schwierigkeit, WLAN/Online sowie Kartenwahl. Freischaltungen und Marken liegen hinter Arsenal. Dekorative Einblendung und ruhige Bewegung dürfen Touchflächen nicht verschieben; bei reduzierten Effekten, Energiesparen, Fokusverlust/Pause oder Modal sind Bewegungen aus. Rebuild/Verlassen gibt Diorama-, Textur- und Ereignisressourcen frei. Solo/Netz/Tutorial/Freischaltungen behalten ihre Regeln.

## Abnahme

Echte Videodekodierung und erster Frame, Pause/Resume/Ende; Controller→Boot bei natürlichemEnde, frühem/spätemSkip, fehlendemClip und Deaktivierung. Kein übrig gebliebenes Overlay/RenderTexture, Menübedienung nach Übergang. NeuesMenü bei900/1280logischenBreiten, echtesArsenal/Profilpersistenz, festeHitflächen undBewegungsoptionen. NativeRegressionen, neueMac/APK, verifizierteSzenenreihenfolge, Frischimport und aktuelleStartup-Netzwerkprüfungen. Unveränderte0.11Core-/Transportbelege nur mit expliziter Quell-/Binärbindung weiterverwenden. Android-Hardwaredecode/Performance bleiben bisGerätetest ungeprüft.
