# Testbericht

Stand: 10. November 2026. Der Bericht unterscheidet Code, Unity-Tests, Desktop-Prozesse, APK-Export und tatsächliche Android-Hardware. Ein grüner Desktop-Test wird nicht als Gerätetest gewertet.

## Aktuell belegt: 3D / Core v1.2 / Netzwerkprotokoll 2

| Prüfung | Ausgeführt / Ergebnis | Beleg |
| --- | --- | --- |
| Unity-Import und Content-Migration | Erfolgreich, 6000.3.28f1. Drei Karten, fünf Gebäude, verbundenes Prefab/Boot. | TestResults/prepare.log; Assets/Content/Standard.asset Schema2 |
| Unity EditMode | **49/49 bestanden**, keine übersprungenen Tests, 0,435 s Testlauf | TestResults/editmode.xml / editmode.log |
| Unity PlayMode | **8/8 bestanden**, keine übersprungenen Tests, 28,178 s Testlauf | TestResults/playmode.xml / playmode.log |
| Core-Harness | 33 Szenarien, 72 seitengetauschte Kartenpartien, 12 Economy-Partien | Docs/Gameplay.md; Tools/CoreHarness/Results |
| Wire-Harness | 51.327 Prüfungen, 24 vollständige Bot-Partien auf drei Karten mit Codec-Roundtrip | Docs/Networking.md |
| Unabhängige Reviews | Wirtschaft, Netzwerk, 3D-/Touch-Lebensdauer; konkrete Befunde behoben | Docs/EconomyReview.md, NetworkReview.md, VisualReview.md |
| Cloud-Projektanlage | Erfolgreich nach CLI-Update beta.17; öffentliche Projekt-ID lokal verknüpft, kein Billing aktiviert | SETUP.md; ProjectSettings/ProjectSettings.asset |

EditMode prüft Geld-/Besitz-/Queue-/Upgrade-/Slow-/Siegregeln, Idempotenz, Karten-/Minenvalidierung, Hashes, Protokollgrenzen, Snapshot-/ACK-Verarbeitung und Paket-Reordering. PlayMode startet die verbundene Boot-Szene, bedient Tutorial/Bauen/Senden/Upgrade/Verkauf, prüft zehn Menü-/Sessionzyklen, Hintergrund-/Abbruchpfade und die drei echten 3D-Karten mit Mineneinkommen. Alle zwölf projizierten Bauplatzmittelpunkte je Karte werden durch den tatsächlichen EventSystem-Raycaster geprüft. Nach dem Verlassen bleiben nur die 3D-Ressourcen der Menüvorschau erhalten.

Ausgeführt:

```sh
bash Tools/unity.sh prepare
bash Tools/unity.sh edit-tests
bash Tools/unity.sh play-tests
```

## Frühere Nachweise, nicht als 3D-Regressionsnachweis verwenden

- 2D-Prototyp: 23/23 EditMode und 2/2 PlayMode bestanden, macOS-Spielbuild erfolgreich.
- Zehn echte Zwei-Prozess-LAN-Zyklen des alten Protokolls1 bestanden, TestResults/lan-clean/comparison.json. Die neue v2-Abnahme muss gesondert erfolgen.
- M0-Android-Exportprobe erfolgreich. `Builds/Smoke/M0-ExportProbe.apk` ist **kein spielbares Spiel**. 38 MB, SHA256 da82e78ba0a713ae8589f28fd8b06d010810fc593d9a59db87fe820b7f68515e.
- Reine Core-Lastsimulation: 160 Einheiten/24 Türme, 18.000 Ticks, ca.53ms auf Desktop, 144 Byte / 0 GC. Künstliche langlebige Testeinheiten, keine 15 realen Minuten und kein Rendering-/Android-Nachweis.

## Zusätzliche tatsächlich ausgeführte Abnahme der Version0.2

- **Frischer Import bestanden**: neue isolierte Kopie ohne Library, Unity-Prepare Exit0 und gleicher Contenthash. `TestResults/fresh-import.json`, `Tools/verify_fresh_import.py`.
- **Mac3D-Build bestanden**. Sichtprobe1280×720 und1600×720: drei Karten, Solo, Bauvorschau/Bestätigung, Mine+Upgrade und sichtbares Einkommen, Senden und Menürückkehr. Nutzer spielte anschließend selbst im20:9-Vorschaufenster. Keine Android-Aussage daraus.
- **LAN clean10/10 und gestört10/10 bestanden**: `TestResults/lan-clean-v2/comparison.json`, `lan-impaired-v2/comparison.json`. Störung nominal150mszusätzlicheRTT,±15msJitterjeRichtung,2%Loss; tatsächlicheDrops15/528Client→Host und16/1012Host→Client. Nach allenKäufen mindestens17gleicheSnapshotsimStörlauf jeZyklus.
- **Zwei vollständige LAN-Partien + beidseitige Revanche bestanden**: jeweils118,4s, beideBasen0imselbenTick→Draw. NeueEpoch/frischeGold/HP/Listen und identischefinaleStateDigests. `TestResults/lan-complete-v2/comparison.json`.
- **Echter Relay-DTLS-Test bestanden**, außerdem zwei vollständigePartien+Revanche, je122,35s, gleicheEndzustände1:0HP. Zwei getrennteanonymeAuthprofile, echteCloudAllocation/Join. `TestResults/relay-local/comparison.json`, `relay-complete-v2/comparison.json`. BeideProzesseaufdemselbenMac/Internetanschluss; ausdrücklichkeinzweiNetze-/Androidnachweis.
- **Spielbare APK gebaut und Signatur verifiziert**:57.961.833Byte/55,28MiB,0.2.0(Code2),ARM64/IL2CPP/GLES3,MinAPI25/Ziel35. Development-Build;AndroidDebug-Zertifikat,RSA2048,APK-v2-Signaturdurchapksignerverifiziert. Paketde.defenseduell.game,Activitycom.unity3d.player.UnityPlayerGameActivity. SHA256: `fedbd42369149b586ad09daa71daaae942fbaec410fc12c270156c610490a835`. Belege `TestResults/build-artifacts.json`, `Builds/Android/DefenseDuell.apk.sha256`.

Vollpartien nutzten unveränderte20Hz-/Produktionsregeln; dieTestspieler schickten nachdemKauf-/Minen-Szenario legalTruppen und bauten keine weitereVerteidigung. DiezweiminütigenSzenarienbelegenAbschluss/Revanche,keineallgemeineMatchdauerbalance.

## Nicht geprüft / weitere Nutzeriteration


- Neu beauftragtes Drag-and-drop und Angriff-Stacking/Economy-Iteration sind noch nicht Bestandteil dieser0.2-Nachweise.
- Samsung war im USB-Baum sichtbar, aber `adb devices -l` lieferte zuletzt eine leere Liste. Kein Gerät installiert/gestartet, Modell/Android-Version nicht ausgelesen.
- Zwei Android-Geräte, zwei Internetzugänge, echte Unterbrechungen am Handy und 15 Minuten maximale Grafik-/Netzlast inklusive Thermik: **nicht geprüft**.
- Botdaten zeigen starke Verteidigung. Menschliche Balancefreigabe: **nicht geprüft**.

## Definition of Done für die gesicherte Version0.2

| Kriterium | Status | Grenze |
| --- | --- | --- |
|1FrischerImport/dokumentierterBuild|Bestanden|IsolierteKopieundgepinntesSetup|
|2Bauen/Upgrade/Verkauf/Senden/vollePartie|Bestanden aufDesktop|Zwei echteSpielprozesse; keinezweiPersonenaufHandys|
|3WLANundOnlinecode|Bestanden aufDesktop|EchterRelay; zweiInternetanschlüsse nichtgeprüft|
|4APKmitGröße/Hash/Signatur|Bestanden|Development-Testsignatur,keineStorefreigabe|
|5Android/Performance|Nicht geprüft|AufNutzerwunschnachFertigstellungtesten|
|6Kernprüfungen/Neustarts|Bestanden|49EditMode/8PlayMode,20LANZyklen,volleRevanchen; menschlicheBalanceoffen|
|7Herkunft/Lizenzen|Bestanden fürimportiertenStand|KenneyCC0+eigeneMeshes;Tripo/Fabnichtimportiert|

Spätere Versionen müssen ihre geänderten Bereiche erneut abnehmen; dieseTabelleistkeinBlankonachweisfürneueFeatures.
