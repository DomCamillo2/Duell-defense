# Aktiver Backlog0.15

Der nächste APK-Build erfolgt erst nach vollständiger Integration und passender gemeinsamer Abnahme aller beauftragten Features. Verbindlicher Stand: [PROJECT_STATE](PROJECT_STATE.md), [Featurematrix](Docs/FeatureAcceptance15.md). Frühere Planprioritäten weiter unten sind historische Zwischenstände und werden durch den aktuellen Gesamtauftrag ersetzt.

- [x] Teams2v2/3v3/2v2v2, Wellenmenü1–200/Endlos, Einkommensfaktoren, Hostkartenwahl, individuelle8Klassen-Ausrüstung integriert.
- [x] Laser/Tesla-VFX, pfadspezifische Turmformen/2074Porträts, große Sendekarten/Swipe, Zwischenscreens und LAN-Hinweise integriert.
- [x] Sichtbar eigenständige neue Karten, mehr Höhen-/Landschaftsdetails; geometrisch legalen Bauplatz erhalten.
- [x] Finale native Tests, neue Bilder, finaler App/Discovery-/Störnetzabgleich.
- [x] APK nach vollständiger Integration gebaut/verifiziert; Frischimport und Quellpaket bestanden, lokalerCheckpoint gesichert.
- [ ] Extern: Androidhardware, zwei menschliche Spieler, getrennte Internetanschlüsse,15-Minuten-Leistung/Thermik.
- [ ] Extern: iOS-SDK/Unitymodul/Xcode/Signierung/iPhone; kein installierbarer iPhonebuild versprochen.
- [ ] Kasernen: beauftragter Plan, keine aktive Runtimeanforderung. Reconnect/Mortar/Hostmigration bleiben bloße Vorschläge.

Die historische Entwicklung bleibt erhalten:

# Historischer Backlog

Verbindlicher aktueller Überblick: [PROJECT_STATE.md](PROJECT_STATE.md). Verlauf: PROGRESS.md. Ausgeführte Nachweise: TEST_REPORT.md.

## Gesicherte Zwischenstände

- [x] Unity LTS mit Android-Werkzeugen, Core, Offline-Bot, LAN, echter Relay-Zugang ohne Billing.
- [x] Version0.2 inklusive APK, frischem Import, Quellarchiv und Commit2d41be6; Hardware ausdrücklich nicht geprüft.
- [x] Desktop-Version0.3 mit freier Kampfplatzierung, Drag/Tap, sofort bezahlter FIFO, Kill-Gold, drei großen3D-Karten und neuem HUD.
- [x] Version0.3:76 EditMode/26 PlayMode, zehn saubere und zehn gestörte LAN-Zyklen, je zwei vollständige LAN-/Relay-Partien mit Revanche und Rapid-buy.
- [x] Renderer v4 und tatsächlicher Vertex-Kamerafit integriert und getestet;0.3-Desktopcheckpoint9a509f2. Nutzer spielt separat die vorher gebaute0.3-Vorschau mit Renderer v3.

## Abgeschlossenes Paket 0.4

- [x] Neun generierte Klassenbilder, Vollbildkarte, Zoom/Pan/Pinch, große Gegneransicht und kompakter Kontext.
- [x] Freie Minen, zwei auswählbare Upgradepfade, Core v1.7 / Schema 5 / Wire 4.
- [x] 89 EditMode- und 35 PlayMode-Fälle, native Sichtprüfung sowie zehn saubere und zehn gestörte LAN-Zyklen; je zwei vollständige LAN-/Relay-Partien.
- [x] APK 0.4 mit Größe/Hash/Testsignatur, frischer Import, Quellarchiv und lokaler Commit 62df0bc. Nachweise: Docs/Release04TestReport.md.

## Abgeschlossenes Artpaket 0.5

- [x] Neun echte Spielmodelle nach den Konzeptporträts verbessern; 27 erkennbare Turm-Ausbaustufen mit begrenzter Geometrie.
- [x] 31 transparente Porträts aus den tatsächlichen Spielmodellen rendern, Quelle/Import/Hash prüfen und im HUD verwenden.
- [x] Spielerische Rahmen und Preisplaketten, graue unbezahlbare Käufe, weniger Angriffstext, unabhängiges animiertes Info-i.
- [x] Mehrfinger-/Info-Lifecycle und Kaufgrenzen im echten EventSystem prüfen; gehaltene Kontakte über vollständiges Schließen hinaus sperren.
- [x] Farblich abgestimmte drei Biome; exakte Terrain-Geometriegleichheit und neue Gesamtbilder prüfen.
- [x] Getrennte macOS-ArtReview-Vorschau, APK 0.5, passende LAN-/Relay-Binärprüfung, frischer Import, Archiv und Checkpoint.

## Abgeschlossenes Paket 0.6

- [x] Neu gestaltete Aktionskarten mit eigenen Symbolen, Preisplaketten und Ausbaustufen.
- [x] Autoritatives 2×-Tempo nur bei beidseitiger Zustimmung, sofortiger Rücknahme und Revanche-Reset; offline lokal steuerbar.
- [x] Core v1.8 / Wire 5, reine Regeln und echte Netzwerk-/HUD-Regressionen.
- [x] Separate SpeedReview-Vorschau, neue APK, Quellenarchiv und Checkpoint.

## Spätere Geräteabnahme

- [ ] Physisches Samsung per ADB freigeben, APK installieren und spielen. Der separate Mactician-Abschlusstest ersetzt keine physische Handyprüfung.
- [ ] Zweites Android-Gerät bzw. Android+Desktop und zwei getrennte Internetzugänge für tatsächliche Verbindungsprüfung.
- [ ]15 Minuten maximale Gerätelast: Frametimes/Ausreißer, Speicher/GC, Netzwerk und Thermik messen.60FPS und stabiler30FPS-Modus bleiben Ziele bis dahin.
- [ ] Menschliche Balanceprüfung: frühere Botvergleiche zeigen starke Verteidigung und Zeitlimit-Remis; neue Upgradepfade müssen separat gemessen werden.

Optionaler Nutzerwunsch, nicht Teil des Pflichtpakets: Geländehöhen beeinflussen Truppentempo. Feste Wege bleiben bestehen. Keine Maze-Blockierung, Helden oder zusätzlichen Spieler.

Tripo/Fab wurden recherchiert, aber keine ungeklärten Galerie-/Packmodelle importiert. Herkunft im Assetmanifest halten. Cloud-Anzeigename Handygame kann später in Defense Duell umbenannt werden; kostenlose Zuordnung funktioniert bereits.

Nicht beauftragt: Store-Veröffentlichung, kostenpflichtiges Billing, Ranglisten, Matchmaking, Chat, Monetarisierung, Host-Migration oder dedizierte Server.

## Abgeschlossene Entwürfe für 0.7

- [x] Economy-Analyse im Asset-/Audit-Chat: Truppensenden als Investition in eigenes Einkommen; Gegenprüfung von Kill-Gold, Minen, Queuegrenzen und Inflation. Nur Entwurf, keine Runtime-Änderung in0.6.
- [x] Interaktivitätsanalyse im Ansichts-Chat: aktive Rettungsfähigkeit, wählbare Turmpriorität und lesbarer Angriffsdruck priorisieren; mit Economy-Entwurf abstimmen. Nur Entwurf, keine Runtime-Änderung in0.6.
- [x] Truppenupgrade-Entwurf im Design-Chat: klassenbezogene Verbesserungen (z. B. Widerstandskraft/Tempo), mögliche unbegrenzte Ausbauanzahl prüfen; Kosten-/Statkurven, achtminütige Partie, Queue-/Feldgültigkeit und sichere Zahlenbereiche mit Economy-/Interaktivitätsentwurf abstimmen. Noch keine Upgrade-Regel oder Runtime-Änderung festgelegt.
- [x] Wellenvertrag im Kartenchat: automatische zunehmende Grundwellen zusätzlich zu PvP-Sendungen, endliche Level-Wellenzahl (100 als Beispiel), 5–10 Sekunden Vorbereitung, automatischer/früher Start, Bauen jederzeit; Laufzeit, Sieg/Gleichstand, FIFO-Fairness und Kill-Gold mit den anderen Entwürfen abstimmen. Kanonenpflicht separat optional. Acht-Minuten-Regel für neuen Wellenmodus neu bewerten.

## Gemeinsamer Umsetzungsvertrag0.7

- [x] Alle vier Entwürfe prüfen und gemeinsamen Clock-/Wellen-/Economy-/Forschungs-/HUD-Vertrag festlegen: Docs/Iteration07Contract.md.
- [x]0.7 Core/Content/Clock/Quote/Bot integrieren und kombinierte Partien prüfen.
- [x] Wire6/Coordinator und tatsächliche LAN-/Relay-Wellenpartien prüfen.
- [x] HUD/Forschung/Priorität/Bedrohungsanzeige integrieren, Pointer und Bilder abnehmen.
- [x] Neue WaveReview-Vorschau, APK, Frischimport, Archiv und separate Abnahmeberichte liefern. Android-Emulation separat offen.

- [x] Direkter Karten-Art-Folgeauftrag: Ansichts-Chat entwickelt isolierten MapLandmarkArt-Helper mit markanten Orten je Biom; Root integriert nach Bild-/Geometrie-/Verdeckungsabnahme. Dateigrenzen in PROJECT_STATE.md.


## Zusätzlicher finaler Nutzerumfang / aktive Umsetzung

- [x] Zwei freischaltbare Turm-Sidegrades mit eigenen echten3D-Modellen und sechs Ausbauvarianten integrieren; bestehende Klassen frei lassen.
- [x] Lokale Siegeswährung getrennt vom Matchgold, persistente Freischaltung im Hauptmenü und exakt einmalige Belohnung je abgeschlossenem Match.
- [x] Drei faire auswählbare Bot-Schwierigkeiten mit gleichen Spielregeln/Budgets, deterministisch prüfen.
- [x] Unabhängige native Endabnahme durch Bot-/Reviewchat, konkrete Mängel korrigieren und erneut nachweisen. Hardware-/Drag-Prüfgrenzen dokumentiert.
- [x] Finale APK mit Hash/Version/Paket-ID an Designchat für ausdrücklich gewünschten Mactician-Installations-/Spieltest übergeben.
- [x] Android-Emulation: APK installiert/gestartet; Bauen per Bestätigung, Upgrades/MAX/Priorität, Sendung/Forschung und PiP/Zoom dokumentiert.
- [ ] Direkter Android-Drag/Pan/Mehrfinger sowie Tempo/Minen/Revanche in stabiler Steuerungsumgebung bzw. auf Samsung prüfen. CUA-Fenstersteuerung verhinderte eine vollständige Abnahme.

0.7-Abschlussstand: Mac und APK gebaut, Frischimport bestanden, reale LAN-/Relay-/Störläufe bestanden. Native Endsichtung abgeschlossen; Implementierungscheckpoint0c97ad6 und Quellenarchiv erstellt. Begrenzte Android-Emulationsabnahme abgeschlossen; konkrete Gesten-/Hardwaregrenzen stehen im Testbericht. Keine Nutzerfreigabe offen. Historische Entwurfssätze oben beschreiben ihren damaligen Auftrag, aktuelle Regeln stehen in Docs/Iteration07Contract.md.

## Nutzerkorrektur0.8: HUD zu voll

- [x] Kompakten Status und Wellen-/Queue-Details auf Abruf umsetzen; tatsächliche Quote pro Angriffskarte erhalten.
- [x] Leere FIFO und gesperrte Bauklassen aus dem Spielbild entfernen, Infoziele in Karten integrieren.
- [x] Aktive/Vorbereitungs-/volle Queue-/Kontext-/Gegnerzustände bei900/960/1280 und Androidformat visuell prüfen; Input-/Quote-/Maskenregressionen erhalten.
- [x] Neue0.8-APK und separate HudReview-Vorschau bauen;0.7-Artefakte geschützt erhalten.

0.8: Vollsuite 71/71, gezielter finaler HUD-Nachlauf 19/19, unabhängige Bildsichtung und neue Mac-/APK-Builds bestanden. Reale LAN-Aktions-/Tempoabnahme mit 361 gemeinsamen Snapshots bestanden. Android-0.8-Installation und physische Gesten-/Leistungsabnahme bleiben offen; 0.7-Emulationsbelege sind historisch.

## Nutzerumfang 0.9: scharfes Vollbild, Einkommen und Klang

- [x] Physisch scharfes Rendering und Vollbildkarte hinter dem HUD, ohne hellen Rahmen.
- [x] Sichtbare Goldrate und korrektes Feedback für zusätzliches Angriffseinkommen.
- [x] Hochwertigeres Terrain und verständliche, angenehme Bauvorschau.
- [x] Neue eigene Musik und differenzierte, begrenzte Aktionssounds.
- [x] Integrierte Eingabe-/Render-/Audioabnahme, neue APK, Vorschau und Quellenarchiv.

## Integrierter Nutzerumfang 0.10/0.11

- [x] Unbegrenzte klassenspezifische Ausbau-/Forschungsstufen mit exakten Zahlen; Frost Dauer/Reichweite ohne Schadensupgrade.
- [x] Keine künstlichen Gebäude-/Truppen-/Wellenkaufmengenlimits; Geld, Platz, FIFO und Partiedauer bleiben.
- [x] Elf sichtbare Meilensteine mit Pfadmodulen und278 nativePorträts.
- [x] Größere Vollbildkarte und gamifiziertes HUD, gemessene Pixelauflösung auf3Maps/3Formaten.
- [x] LAN-Gastinterpolation, Wire7-Seiten/ACK-Fenster, Drag-/Abwahlkorrektur.
- [x] Material-/AO-/Kontaktschatten-/Bewegungsfeedback, neuesAppIcon, Einzelprüfung allerPorträts.
- [x] 0.10APK als geschützter Zwischenstand gebaut; Native140Edit/135Play+6Tooltip geprüft.
- [x] 0.11Minen-/Sendefluss integriert;102Corefälle und360Bestätigungsreplays bestanden.
- [x] 0.11 native Vollsuiten, echter LAN/Relay, finale Mac/APK und Frischimport abgeschlossen; Quellenpaket mit separatem Integritätsbeleg.

Nicht Teil dieses Pakets: Vier-Spieler-Modus (nur separate Machbarkeitsanalyse). Physische Geräte-, menschliche Balance- und Lautsprecherabnahme bleiben oben offen.

## Neu beauftragt für 0.12

- [x] Studiointro mit VideoPlayer/RawImage, Boot asynchron vorladen, Skip nach1,5s und weicher Übergang; robuste Fehler-/Headlesspfade.
- [x] Aufgeräumtes spielerisches Startmenü mit echtem Diorama, feststehenden Solo/WLAN/Online-Touchflächen und Arsenal; Animationen bei reduzierten Effekten/Energiesparen aus.

In0.12 integriert; native Vollsuite149/157 bestanden. Schärfenachlauf14/14, aktuelleLAN/Relaystarts, Mac/APK undFrischimport bestanden; Auslieferungsbelege siehe PROJECT_STATE. Keine0.11-Funktionen. Kasernen-Duell ist ausschließlich ein weiterer Konzeptauftrag.

## Direkte Folgeaufträge 0.13

- [x] Durchgehender Home-/Connecthintergrund außerhalbSafeArea; sichereTouchabstände erhalten.
- [x] NeueWLAN-/OnlineCreate/JoinKarten, kontrastreicheEingaben undHilfedialog.
- [x] Titelclipping undüberhoheLautstärkegriffe korrigiert.
- [x] Finale149/161Vollsuiten, aktuelleLAN/Relaystarts, neueMac/APK undFrischimport; QuellpaketmitseparatemHashnachweis.

## Nachfolgeupdate nach 0.14 – neue Nutzerpriorität

Zuerst den eingefrorenen0.14-Stand als APK fertigstellen. Neue Grafik-/UI-Wünsche nicht in diesen Build ziehen: deutlich verbesserte Grafik, größere Truppen-Sendebuttons, Truppenupgrade per Links-Swipe mit sichtbarem Gestenhinweis und Prüfung aller Zwischenbildschirm-GUIs. Isolierter Entwurf im Assetchat unter `Tools/NextVisualUpdateDraft/`; Runtimeumsetzung später durch Root/gezielteSubagents.

Zusätzlicher Folgeauftrag aus Economychat: unterschiedliche Teammitglieder mit unterschiedlichen Turmloadouts, erkennbare echte Blitzform für Blitzspulen-Angriffe. Bedeutung „jeder soll jeden Turm ausrüsten/aufrüsten“ wird dort noch geklärt; keine unbestätigten Gegner-Upgrade-Rechte einbauen. KeineRuntimeänderung im aktuellen200-Wellen-Addon.

Nutzerklarstellung: Gemeint ist individuelles AUSrüsten/Anwählen der eigenen Turmklassen im Warte-/Lobbybildschirm (z.B.Balliste undBlitzspule), keine gemeinsamenUpgrade-Rechte. Spieler dürfen verschiedeneAuswahlen haben; keine neueSlot-/Freischaltbeschränkung festgelegt. Blitzspule sollsichtbareBlitze stattKugeln erhalten. Umsetzung erstnach0.14.1.

Weiterer Folgeauftrag: Wellenzahl bereits im Menü perSchieberegler wählen undSolo/Host-Lobby konsistentvorbelegen; expliziter echterUnbegrenzt-Modus zusätzlich. Keinmaxint-Ersatz: dynamischerWellenplan/Ledger, Final-/Sendefenster-/Wire-/Readyregeln müssenfürEndlos angepasstwerden. Dieaktuelle0.14.1-APK bleibtbeimdirektbeauftragtengeprüften1–200-Addon.

Weiterer Folgeauftrag ausHUDchat: hostautoritativ synchronisierteKartenauswahl imHost-/Wartebildschirm inklusiveReadyreset; zusätzlichetatsächlichgrößere/detailliertereMaps imvorhandenenArtstil. KeinautomatischerTeammodus. VorhandeneisolierteMapkandidaten prüfen, keineDoppelentwürfe. Umsetzung zentralnachaktueller200-Wellen-APK.

Weiterer Folgeauftrag ausMapschat: Einkommensmultiplikator imMenü/Host-Lobby, fürbeideSpielergleich. Arbeitsannahme: laufendesBasis-/Minen-/Angriffseinkommen multiplizieren; Startgold/Preise/Verkaufsrefund/Killbounty bleibengetrennt. WerteauswahlnochkeineNutzervorgabe; exakte960-Reste, HUD, Hash/Wire/Readyreset/Revanche müssenmitgeführtwerden. UmsetzungnachaktuellerAPK; nichtmit2×Spieltempo verwechseln.

Konkreter Folgeauftrag: automatischeLAN-Lobbyerkennung bei geöffneterApp, Banner/Popup mitBeitreten/Später undexplizitemJoin. KompatibletatsächlichbeitretbareHosts, keineeigeneSession, Deduplizierung/Timeout, sichereAbsender-IP, keinModalimMatch, Pause/Netzwechselbereinigen; echte2Prozess-LAN-/Android-WLAN-Abnahme. KeinHintergrund-/OS-Push.

Explizit neu beauftragte Teamimplementierung: Assetchat besitzt ausschließlich `Tools/TeamMultiplayerCandidate/**` für ausführbare C#-Teamidentitäten/Targeting/Scheduler+Tests. Noch keineAssets-/MultiPeer-/HUD-Integration; Kandidat ausaktuellerAPK/Quellpaket/Checkpoint ausgeschlossen.

Zusätzlicher konkreter Kartenauftrag: mehr tatsächlichlegalenundnützlichenBauplatz inbestehendenundneuenMaps schaffen, nichtnurdieAußengrenzenvergrößern. Blocker/VisibleLandscape abgestimmtverkleinern/verschieben; Weg-/Turmabstände zunächstbeibehalten. Read-onlyFlächenauditimHUDchat; RuntimezuteilungnachaktuellerAPK.
