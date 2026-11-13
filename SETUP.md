> Gesamtupdate0.16.0 / Code17 / Core2.5 / Schema11 / Wire10. Aktueller Prüf- und Buildstand: [PROJECT_STATE.md](PROJECT_STATE.md). Alle Teilnehmer benötigen denselben Build. Separate Macprüfung: `bash Tools/unity.sh update16-review`; iOS-Voraussetzungen: [Docs/iOSPreparation.md](Docs/iOSPreparation.md).

# Reproduzierbares Setup

Stand: 10. November 2026. Das Projekt wurde lokal auf macOS 26.6.2 / Apple M4 Pro eingerichtet. Keine Cloud-CI erforderlich. Unity Personal war bereits aktiviert.

## Versionen

| Werkzeug | Exakte Version |
| --- | --- |
| Unity Editor LTS | 6000.3.28f1, Revision 8f082d2fe476 |
| Netcode for GameObjects | 2.13.3 |
| Multiplayer Services SDK | 2.4.1 |
| Unity Transport | 2.7.5 |
| Input System | 1.20.1 |
| Unity Test Framework | 1.6.0 |
| uGUI | 2.0.0 |
| OpenJDK (Unity Android-Modul) | 17.0.18 |
| Android NDK | r27c / 27.2.12479018 |
| Android SDK Build Tools | 36.0.0 |
| Android Platform Tools | 37.0.1 |
| Gradle (Unity) | 9.3.1 |

Direkte Pakete stehen in `Packages/manifest.json`, der gesamte vom Unity Package Manager aufgelöste Graph in `Packages/packages-lock.json`. Unter anderem Authentication 3.8.1, Services Core 1.18.0, Collections 2.7.1 und Burst 1.8.31. Keine Preview-Pakete oder Git-master-Abhängigkeiten. Quellen und Kompatibilitätsprüfung: `Docs/Research.md`.

## Installation und Import

Über Unity Hub **6000.3.28f1** mit **Android Build Support**, **Android SDK & NDK Tools** und **OpenJDK** installieren. Die kostenlose Personal-Lizenz muss in Hub aktiviert sein. Eine bestehende Lizenz wurde genutzt; es wurde kein kostenpflichtiger Plan aktiviert.

Alternativ wurde auf diesem Mac die offizielle Unity CLI verwendet:

```sh
unity install 6000.3.28f1 --architecture arm64 --module android --child-modules --yes --accept-eula --non-interactive
```

Das Repository/Projektarchiv in einen neuen Ordner entpacken, in Hub hinzufügen und mit genau diesem Editor öffnen. `Library` wird neu erzeugt. Der vollständige Einstieg ist `Assets/Scenes/StudioIntro.unity`; `Boot.unity` überspringt das Intro.

```sh
bash Tools/unity.sh prepare
# Nach Änderungen an Modellen/Karten/Shadern: bash Tools/unity.sh refresh-art
bash Tools/unity.sh verify-portraits
bash Tools/unity.sh edit-tests
bash Tools/unity.sh play-tests
bash Tools/unity.sh android
python3 Tools/verify_android.py
```

`prepare` verbindet validierte Inhaltsdaten, Prefab, StudioIntro-/Boot-Szenen und Build-Einstellungen. Es verknüpft kein Cloud-Projekt. Derselbe Einstieg ist im Editor unter **Defense Duell** verfügbar. Bei anderem Installationsort `DEFENSE_UNITY_EDITOR` auf die ausführbare Editor-Datei setzen. Nicht zwei Editor-Instanzen gleichzeitig auf denselben Projektordner starten.

Build: ARM64, IL2CPP, OpenGL ES 3, Min-API25 (Android 7.1), Ziel-API35, Querformat, Development Build mit lokaler Debug-Testsignatur. Ausgabe: `Builds/Android/DefenseDuell.apk`. Für dauerhafte Veröffentlichung wäre eine eigene sicher verwaltete Release-Signatur notwendig; kein Signierschlüssel gehört ins Repository.

`verify_android.py` prüft die gebaute Datei mit `aapt` und `apksigner` aus Unitys SDK, kontrolliert Versionsnummer, Manifest und ARM64/IL2CPP-Inhalt und schreibt SHA-256 sowie `TestResults/build-artifacts.json`. Für einen anderen SDK-Pfad `--android-tools` verwenden. Frischer Import: `python3 Tools/verify_fresh_import.py`. Lokales Quellarchiv: `python3 Tools/package_project.py`.

Aktuelle Desktop-Prüfung 0.13 (Teststatus separat in TEST_REPORT):

```sh
bash Tools/unity.sh connection-review
python3 Tools/NetworkTests/run_lan.py 'Builds/macOS-ConnectionReview/DefenseDuell.app/Contents/MacOS/Defense Duell 0.13.0' --map 0 --cycles 1 --port 7827 --rapid-buy --complete-matches --output TestResults/lan-connect-v13-usercheck
```

`connection-review` verwendet ein eigenes App-Ziel und Bundle `de.defenseduell.preview.connection`. Die vorherigen Vorschauen `macOS-Candidate` (0.4) und `macOS-Next` (0.3) bleiben erhalten. Laufende Vorschauen vor einem erneuten Build desselben Ziels nicht überschreiben. Aktuelle0.13-Nachweise stehen im Testbericht. Jeder neue Lauf braucht einen neuen Ausgabeordner; vorhandene Berichte werden nicht überschrieben.

## Porträts aus den Spielmodellen

Das Quellpaket enthält die 278 fertigen RGBA-Porträts samt Importern und Quellenmanifest unter `Assets/Art/ModelPortraits/v1/`. Der Build prüft automatisch, dass Modell-, Shader-, Baker- und PNG-Hashes noch zusammenpassen. Bei einer Modelländerung zuerst neu rendern:

```sh
bash Tools/unity.sh portraits
bash Tools/unity.sh verify-portraits
```

Der Bake läuft mit Grafikzugriff, daher ausdrücklich ohne `-nographics`. Er rendert die produktiven `DioramaResources`-Meshes und ihr Material in einer getrennten Preview-Szene, mit derselben Sonnenbeleuchtung wie im Spiel. Eine feste Dreiviertelansicht und ein gemeinsamer Ausschnitt pro Klasse erhalten die Größenunterschiede der Ausbaustufen. Danach werden Kamera, RenderTexture, Meshes und Material wieder freigegeben. Im Spiel werden nur Sprites geladen; es gibt keine zusätzlichen Porträtkameras. Unity-API: [Camera.scene](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/camera/scene), [NewPreviewScene](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/scenemanagement/editorscenemanager/newpreviewscene).

Die neun bisherigen generierten Bilder bleiben als dokumentierte Konzeptreferenz und Fallback erhalten. Normale Bau-/Angriffskarten laden nur neun Basistexturen; ein Ausbauporträt wird erst bei Auswahl dieses Zustands geladen und anschließend wiederverwendet.

## Unity Cloud und Relay

Offline und WLAN funktionieren ohne Cloud-Verknüpfung. Online braucht eine öffentliche Unity-Cloud-Projekt-ID und aktiv verfügbare Authentication-/Relay-Dienste. Keine Verwaltungs- oder Service-Account-Schlüssel im Client verwenden. Nutzerfreigabe liegt für ein neues kostenloses Projekt **Defense Duell** in **DomCamillo** vor; Billing oder kostenpflichtige Pläne sind nicht freigegeben.

Das Cloud-Projekt wurde am 10. November 2026 erfolgreich erstellt und verknüpft. Die CLI verwendete zunächst den Ordnernamen **Handygame** als Cloud-Anzeigenamen. Öffentliche Projekt-ID: `3c5e5abe-479d-4e35-b63d-674f739182d6`; Organisation DomCamillo. Die beiden alten Projekte wurden nicht verändert. Kein Billing wurde aktiviert.

Die anfängliche Verknüpfung erhielt HTTP400 wegen eines bestätigten Fehlers in Unity CLI beta.13 bei `--coppa unspecified`. Das offizielle Update auf **1.0.0-beta.17** behob den Fehler. Der gleiche Befehl war danach erfolgreich; es wurde keine falsche COPPA-Einstufung zum Umgehen des Fehlers gesetzt. [Release Notes](https://docs.unity.com/en-us/unity-cli/release-notes). Der tatsächliche Relay-Teststatus steht in `TEST_REPORT.md`.

Das Spiel nutzt anonyme technische Anmeldung, eine Relay-Allocation für einen Mitspieler und DTLS. Es benötigt keinen Lobby-Service, keine Rangliste und kein eigenes Login-UI. Den Code gibt der Host dem Mitspieler direkt. Bei Fehlern kann man zum Menü zurückkehren und erneut hosten/joinen.

Kostenlose Kontingente laut geprüfter Herstellerquelle: Authentication kostenlos; Relay 50 durchschnittliche monatliche CCU und 3 GiB pro CCU bis maximal 150 GiB/Monat. Die tatsächliche Organisation darf ohne ausdrückliche Zustimmung keine kostenpflichtige Nutzung aktivieren. Ein Budgetalarm ist keine garantierte Kostenobergrenze. Quellen: `Docs/Research.md`.

Nach bestätigter Cloud-Einrichtung und neuem Build:

```sh
python3 Tools/NetworkTests/run_relay.py 'Builds/macOS-ConnectionReview/DefenseDuell.app/Contents/MacOS/Defense Duell 0.13.0' --map 1 --cycles 1 --rapid-buy --complete-matches --output TestResults/relay-connect-v13-usercheck
```

Das ist ein echter Relay-/Auth-Test mit zwei Desktop-Prozessen, aber kein Nachweis für zwei Android-Geräte oder zwei Internetzugänge. Dafür siehe `Docs/DEVICE_TESTS.md`.

## Android-Testgerät

Der Nutzer hat diese Schritte auf **nach Fertigstellung des Builds** verschoben. Für Implementierung, Desktop- und APK-Prüfungen muss kein Handy angeschlossen sein.

Am Gerät Entwickleroptionen und USB-Debugging aktivieren, über ein Datenkabel verbinden und die RSA-Abfrage für diesen Mac bestätigen. `adb devices -l` muss den Status `device` zeigen. Ein nur im USB-Baum sichtbares Samsung-Gerät genügt nicht.

```sh
adb devices -l
adb install -r Builds/Android/DefenseDuell.apk
adb shell am start -n de.defenseduell.game/com.unity3d.player.UnityPlayerGameActivity
```

Die genaue Activity lässt sich zusätzlich aus dem gebauten Manifest prüfen. Echte Gerätedaten, Start, Steuerung und 15-Minuten-Messung erst nach Ausführung im Testbericht als bestanden markieren. Für zwei Spieler genügt zunächst ein Android-Gerät plus Desktop im gleichen WLAN; zwei Mobilgeräte und getrennte Internetzugänge bleiben eigene Prüfungen.
