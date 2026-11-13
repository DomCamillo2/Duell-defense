# Third-party notices

Stand: 2026-11-13. Es werden keine unbekannten Reuploads, bezahlten Assets, Werbe- oder In-App-Kauf-SDKs verwendet.

## Grafik und Audio

Kenney: Tower Defense Top-Down 1.0, Interface Sounds 1.0, UI Pack 2.0. **CC0 1.0 Universal**, keine Namensnennung vorgeschrieben. Freiwillig: Kenney.nl. Original-URLs, Abrufdatum, Dateien, Änderungen und Lizenzbelege sind in `ASSET_LICENSES.csv` dokumentiert. Originale Lizenzdateien, Archive und CC0-Volltext liegen in `ThirdParty/`.

Version 0.9 ersetzt den früheren einfachen Ambient-Loop durch einen eigenen zwanzigsekündigen Stereo-Musikloop und zehn eigenständig synthetisierte Aktionsklänge. Komposition, PCM-Synthese, Landschaft und Bauvisuals sind projektspezifische Implementierungen. Es wurden keine Audioaufnahmen, fremden Musikwerke oder zusätzlichen fremden Modelle/Texturen übernommen. Die älteren Kenney-Dateien bleiben mit ihren Lizenznachweisen im Projekt erhalten.

Die neun Klassenporträts unter `Assets/Art/GeneratedClassIcons/v1/` wurden für dieses Projekt mit OpenAI imagegen erzeugt: vier Truppen und fünf Gebäude, jeweils als transparentes PNG. Sie stammen aus keinem fremden Assetpaket. `provenance.json` dokumentiert Generator, vollständige Prompts, Bildreferenzen, Abmessungen und SHA-256. Die Referenzen sind selbst innerhalb desselben Projektauftrags erzeugte Bilder. Die PNGs wurden unverändert übernommen; Unity importiert sie als Sprites mit maximal512 Pixeln ohne Mipmaps. Die Bilder bleiben als Konzeptreferenz und Fallback erhalten.

Seit Version 0.7 zeigt das HUD 43 aus den eigenen Spielmodellen gerenderte Porträts: 39 Gebäudezustände und vier Truppenklassen. In 0.9 werden sie mit dem aktualisierten Material neu gerendert. Die PNGs unter `Assets/Art/ModelPortraits/v1/` verwenden exakt die produktiven prozeduralen Meshes und deren Material. Das Rendermanifest bindet Modell-/Shader-/Bakerquellen, Variante, PNG-Hash und Bildrahmen. Es wurden dafür keine fremden Modelle oder Texturen übernommen.

## Unity

Unity Editor und Runtime unterliegen den jeweils anwendbaren Unity-Softwarebedingungen. Das Projekt wurde mit einer bereits aktiven Unity-Personal-Lizenz erstellt; es wurde kein kostenpflichtiges Abo aktiviert. Die eigene Berechtigung für Unity Personal muss der jeweilige Nutzer erfüllen.

Alle exakten direkten und transitiven Paketversionen stehen in `Packages/packages-lock.json`. Die vom Package Manager geladenen Lizenztexte und Third-Party Notices wurden unverändert nach `ThirdParty/Packages/` kopiert; Index: `ThirdParty/PACKAGE_LICENSES.md`. Paketquellen sind die offizielle Unity Registry bzw. die eingebauten Pakete der gepinnten Editor-Version. Keine Preview- oder Git-master-Abhängigkeiten.

Die manuelle Relay-Anbindung verwendet das aktuelle Multiplayer Services SDK. Unity Authentication/Relay unterliegen den Unity Gaming Services Bedingungen und kostenlosen Kontingenten, wie in `Docs/Research.md` belegt. Keine Zusage unbegrenzten kostenlosen Hostings. Keine Zahlungsdaten, Verwaltungs- oder Signierschlüssel gehören in den Client.

Es wurde kein Code aus Boss Room, Godot, Nakama oder GUT übernommen. Die technische Dokumentation diente als API-Referenz; die Spiel- und Netzwerklogik wurde projektbezogen geschrieben.

Die HUD-Rahmen und Symbole in 0.10 werden aus eigenen Vektorgeometrien gezeichnet (`HudTheme`, `HudFrameGraphic`, `HudIconGraphic`). Es werden keine externen Iconfonts oder UI-Texturpakete verwendet. Die bestehenden Modellporträts bleiben an produktive Meshes gebunden.

Version 0.10 erweitert eigene Geometrien um elf sichtbare Ausbaustufen, Pfadmodule, geglättete Materialnormalen und detailliertere Landmarken. Die eigentlichen Kaufstufen sind unabhängig davon unbegrenzt. Die neuen Porträts werden erneut aus diesen produktiven Modellen gerendert. Das neue Launcher-Icon samt adaptiver Vordergrundebene ist projektspezifisch mit OpenAI imagegen erzeugt; vollständige Prompts und Herkunft stehen unter `Assets/Art/AppIcon/v1/provenance.json`. Der einfarbige Hintergrund wird lokal erzeugt. Keine zusätzlichen fremden Assetpakete wurden übernommen.

Der zusätzliche Grafikdurchstich verwendet eigene Materialbeleuchtung, offline berechnete geometrische Umgebungsverdeckung und begrenzte Kontakt-/Interaktionseffekte. Es werden keine externen Texturen, Shaderpakete oder Laufzeit-KI verwendet. Die gebundenen Schattierungsdaten werden vor dem Build aus den eigenen Modellen berechnet; die Runtime wendet sie auf geteilte Meshes an.

## Soballa Studio Intro (0.12)

Die Animation wurde vom Nutzer aus seinem lokalen Soballa-Studio-Ordner bereitgestellt. Das Spiel verwendet eine H264-Konvertierung derselben105Frames (25fps,4,2s,stumm); eine zusätzliche untere Paddingzeile macht720×405 für das Codecformat zu720×406. Keine fremde Stockbibliothek, keine zusätzliche Musik und kein neues Logo. Quell-/Ausgabehash und Konvertierung: Tools/StudioIntroLab/Media/color-conversion.json.

Die beiden Elite-Truppenfamilien und Bosskronen von0.16 sind eigene prozedurale Projektgeometrie. Keine zusätzlichen fremden3D-Modelle oder kostenpflichtigenAssets wurden übernommen. Modellporträts werden aus den Spielmeshes erzeugt; die Herkunft der bestehendenKI-Bilder bleibt separat dokumentiert.
