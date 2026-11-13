# iOS-Vorbereitung für Defense Duell

Stand: 13. November 2026. Zugeordneter Umfang aus `PROJECT_STATE.md` und `Iteration14Contract.md`: neuer Editorhelfer, `Tools/iOS/**` und dieses Dokument. Basis der Integration: `cdbe31d937c39682033d3506585dd64249073ad0`. Der Hauptchat besitzt weiterhin BuildEntry, AppIconSetup, ProjectSettings und alle Unity-/Buildläufe.

## Ergebnis

`Assets/Editor/IosBuildPreparation.cs` und Meta sind integriert. Die vorherige Kopie außerhalb Assets wurde entfernt; es gibt nur eine Implementierung. Der Export stellt IL2CPP, ARM64, Metal, iPhone DeviceSDK und das bestehende Minimum iOS 15 ein. Er verwendet vorbereitete Intro-/Boot-Szenen, prüft den Kartenkatalog und die gebundenen Modellporträts und verweigert bestehende Exportziele.

Die Appversion wird nicht separat festgesetzt. Buildnummer stammt aus der gemeinsamen Android-Release-Linie. Vor dem Export muss Root `BuildEntry.Prepare` auf dem eingefrorenen gemeinsamen Stand ausführen: Während der 0.14-Integration kann die BuildEntry-Version bereits 14 sein, obwohl noch nicht vorbereitete PlayerSettings weiterhin 13 enthalten.

Nach erfolgreichem Export ergänzt der Helper die Erklärung für lokale WLAN-Verbindungen direkt in der Xcode-Info.plist und schreibt `defense-crossplay.json`. Dieser enthält tatsächliche App-/Unity-/Simulationsregelversion, `WireProtocol.Version`, `baseCatalogHash` und Kartenhashes. Er enthält keine fest vorgegebene Wireversion. Das geplante Wire 8 entsteht erst durch den gemeinsam kompilierten 0.14-Stand. Hostoptionen, deren Revisionen und effektive Regel-/Plan-Hashes müssen zusätzlich im Netzwerkabgleich passen; das Basiskatalogmanifest ersetzt diesen Abgleich nicht.

Keine Apple-Team-ID oder Provisioningprofile werden hinzugefügt oder gelöscht; automatische Xcode-Signierung wird für die spätere lokale Teamauswahl aktiviert. Der Helper führt keinen Xcode-Build, keine Signierung, keine Installation und keinen Upload aus. Die Exportvorbereitung ändert keine Core-/Netzwerkregeln.

## Tatsächlich ausgeführte Prüfung

`Tools/iOS/compile_check.py` verwendet den zu Unity 6000.3.28f1 gehörenden Roslyn-Compiler über dessen mitgelieferte .NET-Laufzeit. Der originale neue Editorhelper kompiliert gegen installierte Unity-API-Assemblies und bestehende `Library/ScriptAssemblies/DefenseDuell.*.dll`. Anschließend wird ausschließlich seine reine XML-Plistfunktion mit Mono aufgerufen. **Kein Unity-Editorprozess** wird gestartet.

Beleg: `Tools/iOS/Evidence/api-compile-01.json` mit SHA-256 der Originalquellen und aller verwendeten Referenzassemblies. Ergebnisse: Helperkompilation Exit0; Prüfharnesskompilation Exit0; reine Plistprüfungen Exit0. Gültige exportartige XML-Plist, LAN-Erklärung, Normalisierung doppelter LAN-Schlüssel, unveränderte Bundle-/ATS-Werte und Idempotenz geprüft; drei ungültige Eingaben werden vor Dateimutation abgewiesen. Zusätzlich bestehen zwei portable Python-Plisttests und die Shell-Syntaxprüfung.

Die referenzierten Projekt-DLLs sind vorhandene Buildartefakte und können den parallelen 0.14-Änderungen vorausgehen. Diese API-Prüfung ist **keine Kompilierung des gesamten aktuellen Quellbaums**, kein Unity-Import, kein iOS-Export, kein IL2CPP-/Metal-/Gerätetest und keine Crossplayabnahme. Root muss den endgültigen gemeinsamen Stand importieren und bauen.

Reproduktion ohne Unity:

```sh
PYTHONDONTWRITEBYTECODE=1 python3 Tools/iOS/compile_check.py --report /private/tmp/defense-ios-api-new.json
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s Tools/iOS -p 'test_*.py' -v
bash -n Tools/iOS/export_xcode.sh
python3 Tools/iOS/preflight.py
```

## Voraussetzungen und verbleibende Grenzen

Der aktuelle Mac besitzt den passenden Unity-Editor und Androidmodul, aber kein iOS-Buildmodul für diese Version und kein ausgewähltes vollständiges Xcode/iPhoneOS-SDK. Ein Apple-Team wurde nicht eingerichtet. Deshalb sind Xcode-Projekt, signierte IPA und Installation noch offen. Keine Werkzeuge installiert und keine Signierschlüssel geprüft.

Root hat den iOS-Buildcallback von AppIconSetup bereits auf den validierten opaken Default-Icon-Pfad umgestellt. Die Android-Densityprüfung läuft dort nicht mehr; die allgemeine Android-/Desktop-Apply-Routine bleibt unverändert. Der Editorcode hat weiterhin eine Compile-Abhängigkeit von `UnityEditor.Android`, und die allgemeine Prepare-Routine ruft Android-Apply auf. Für diesen Android/iOS-Arbeitsplatz ist das Modul vorhanden. Für einen künftig reinen iOS-Rechner müsste Root Android-Iconcode in eine bedingt kompilierte Hilfsdatei/Assembly auslagern und auch Prepare nach Ziel trennen. Nicht behaupten, die Callbackkorrektur allein entferne diese Compile-Abhängigkeit.

Der erste iOS-Export muss App-Icon-Slots/1024er-Icon, Metalshader, Hardwarevideo, Audio, SafeArea, Touch und lokale Netzwerkfreigabe auf dem Gerät prüfen. Crossplay benötigt Android→iPhone und iPhone→Android als Host, jeweils LAN und Relay, mit tatsächlich kompatiblen Builds. Ältere APKs erhalten die neuen Hostoptionen nicht automatisch.

Die vollständigen Schritte für direkte Installation via Xcode/Personal Team sowie die spätere TestFlight-Option, Quellen und Geräteprüfmatrix stehen in [Tools/iOS/README.md](../Tools/iOS/README.md).

Status: **integrierter Editorquelltext und begrenzte Compile-/Plistnachweise → kein iOS-Build → keine Signierung/Installation → keine Geräte-Crossplayfreigabe.** Nach Übergabe friert der Mapschat die zugeteilten Dateien ein; weitere Integration/Prüfung bleibt beim Hauptchat.
