# Architektur und Entscheidungen

Verbindliche Typen/APIs: `Docs/CONTRACT.md`. Die Assemblies trennen reine Regeln, Netzwerk, unveränderliche Inhalte, Darstellung, Plattform und Editor-Werkzeuge.

## Daten und Autorität

`GameContentAsset` enthält im Editor veränderbare Definitionen. `CreateCatalog()` erzeugt sechs validierte unveränderliche C#-Inhalte mit je einem SHA-256-Content-Hash. Laufzeitwerte gehören ausschließlich in eine `MatchState`-Instanz. Kein Match schreibt ins ScriptableObject. Der Hash deckt Regeln, Preise, Kartenkoordinaten, Kartentitel, Theme, Mineneinkommen/Verkaufsquoten und Definitionen sowie eine explizite Simulationsregelversion ab. Die idempotente Editor-Migration überführt ältere Assets in Schema 9 mit freien Gebäudepositionen, exakten Einkommenseinheiten, Wellen-/Forschungsdefinitionen und expliziten Waffenmechaniken; freie Karten reservieren keine Minenstandorte.

`MatchSimulation` ist reines C# ohne Unity-/Netcode-Abhängigkeit. 20 Kontrollticks/s; aktive Phasen führen je nach Zustimmung 1/2 innere Gameplayschritte aus, Vorbereitung keinen. Innerhalb eines Spielschritts: Income → Spawn → Combat → Movement/Basisschaden → Ergebnis/Wellenwechsel. Tote bezahlte Einheiten zahlen einmalig die klassenspezifische Prämie an ihren Defender und werden vor Basisschaden entfernt; beide Felder werden vor der Ergebnisentscheidung verarbeitet. Die Zielwahl verwendet Vorne oder Stärkster (aktuelle HP), mit stabilen Distanz-/ID-Tiebreakern. Es gibt keine Physik-Kollisionen oder dynamisches Pathfinding.

`PlayerSlot.Zero` bis `Five` bezeichnet die Partieposition. NGO-Verbindungs-IDs werden ausschließlich im SessionCoordinator diesen Slots zugeordnet. Clientnachrichten enthalten keine vertrauenswürdige Spieleridentität, Gold- oder Schadenswerte. Der Host berechnet Kosten, Treffer, Spawns und Ausgang. Auch Host-UI und Bot gehen durch `Execute`.

Session-ID und Request-Sequenz verhindern Wiederholungen über Revanchen hinweg. Bestätigungen werden begrenzt zwischengespeichert. Identische Wiederholungen erhalten das ursprüngliche Ergebnis; abgewiesene Befehle verändern keine ökonomischen Matchdaten. Metadaten wie Rate-Limit-/Sequenzzähler dürfen Ablehnungen erfassen.

## Netzwerk und Lebensdauer

Ein SessionCoordinator besitzt NetworkManager, UnityTransport, Ereignisabonnements, asynchrone Session-Generation und Shutdown. NGO synchronisiert keine Szenen/PlayerObjects: Beide Geräte haben dieselbe lokale Boot-Szene. Kommandos und Antworten laufen zuverlässig; kompakte binäre Zustandsbilder werden in begrenzten Teilen übertragen und nur vollständig angewendet. Clients simulieren keine Treffer, sondern interpolieren die Darstellung.

Verbindungsfreigabe prüft Protokoll v9 und den Hash des gesamten Kartenkatalogs. MatchRules konfiguriert1–200Wellen oder echtes Endlos sowie Teamformat und Einkommensfaktor. Dieersten vorhandenenWellen bleibenidentisch; zusätzlicheWellen wiederholendieletzteZusammenstellung mit12weiterenHP-Prozentpunkten proWelle. Endlos führt nur aktuelle und nächste Welle im Ledger; archivierte Summen bleiben exakt. Der Zustandsumschlag enthält Kartenindex, effektive Regeln und eine monotone Publikationssequenz; der Client leitet den effektiven Inhalt vor der Zustandsdekodierung ab. Config-/Rosterrevision und Match-Epoche binden Ready an den tatsächlich angezeigten Lobbystand. ACK-Auflösung ist getrennt von der Anwendung neuer Zustände; verspätete oder widersprüchliche Publikationen werden verworfen. Pfadstufen, Gesamtlevel und exakte Investitionssumme werden vor Anwendung eines Snapshots geprüft. Offline und LAN initialisieren keine Unity Services. Online verwendet anonyme Authentication plus `RelayService` aus MPS 2.4.1 und ausdrücklich `dtls`. Die geprüfte offizielle MPS-Dokumentation erlaubt diesen manuellen NGO-Pfad; Lobby/öffentliches Matchmaking sind für private Relay-Codes nicht erforderlich.

Generation-/Abbruchprüfungen verhindern, dass ein verspätetes Cloud-Ergebnis nach Verlassen des Menüs einen Raum startet. Transportverlust, fehlender Fortschritt oder Android-Hintergrund führen zu einem definierten Abbruch. Es gibt keine Host-Migration, erfundene Sieger oder Zusage nahtloser Wiederverbindung.

## Oberfläche

Die Boot-Szene referenziert das GameRoot-Prefab mit Bootstrap, SessionCoordinator und GameUI. UI wird zur Laufzeit als uGUI-Hierarchie erzeugt und ist vollständig mit dem Coordinator verbunden. Der Input-System-UI-Eingang verarbeitet Touch und Maus. Freie Positionen aller Gebäude werden über die tatsächliche Board-Kamera vom Bildschirm auf die logische Bodenebene projiziert. Drag und Tap mit Bestätigung nutzen denselben PlacementValidator wie der Host. Bestehende Gebäude werden per Entity-ID ausgewählt. Unit-Views werden über stabile IDs wiederverwendet; keine NGO-Objekt-Pools nötig. 2074 Porträts der tatsächlichen Spielmodelle werden bedarfsweise mit begrenztem Cache als Sprites geladen; Provenienz, Geometrie- und Importhashes liegen bei den Dateien.

`BoardView` zeichnet eigene 3D-Meshes mit geneigter orthografischer Kamera in eine RenderTexture innerhalb der UI. Die Hauptkarte füllt die Safe Area hinter dem HUD. Zoom und Pan verändern dieselbe Kamera, die für Hit-Tests und Vorschau verwendet wird. Der oberste EventSystem-Treffer schützt HUD-Flächen vor Bauklicks. Die interne Render-Slotnummer bestimmt niemals den Bauort. Alle 3D-Objekte, Materialien und RenderTextures gehören zur Board-Instanz und werden beim Verlassen freigegeben. Das Spielfeld bleibt eine feste mathematische Wegdistanz; die Darstellung verändert keine Treffer- oder Bewegungsregeln.

## Beschlüsse

- **D001:** Unity 6000.3.28f1 LTS installiert, statt stillschweigend auf vorhandenes 6.4/6.6 oder Godot zu wechseln. Android-Export früh durch echten Probe-Build geprüft.
- **D002:** Gepinnte stabile Pakete und echter Resolver-Lock. NGO-Dokumentationswiderspruch durch offizielle Release-/Registrybelege geklärt, siehe Research.
- **D003:** Host-Authority für Freunde. Kein vollständiger Schutz gegen einen manipulierten Host versprochen.
- **D004:** Einfache feste Wege und ganzzahlige Wegbewegung. Erst Profiling rechtfertigt komplexere räumliche Suche oder ECS.
- **D005:** Direkte Relay-API innerhalb MPS spart unnötige Lobby-/Session-Aufrufe. Keine Cloud-Verknüpfung mit einem beliebigen alten Nutzerprojekt.
- **D006:** Min Android API25 aus Unity6.3, Ziel-API35 für private Tests, ARM64/IL2CPP/OpenGLES3; keine Store-Zulassung behauptet.
- **D007:** Test-APK mit Unitys lokaler Debug-Signatur. Keine privaten Schlüssel im Repository und keine Veröffentlichung.
- **D008:** Expliziter Nutzerwunsch erweitert den ursprünglichen 2D-/Ein-Karten-Umfang auf echte 3D-Darstellung, drei Karten und Goldminen. BTD4 dient als Orientierung für erkennbare Klassen und klare Wege; keine fremden Spielassets werden kopiert.
- **D009 (historisch, ersetzt durch D020):** Minen sind eine begrenzte Wirtschaftsklasse im bestehenden Bau-/Upgrade-/Verkaufspfad: maximal zwei pro Spieler, Ertrag 1/2/3 Gold/s, keine Angriffe. Kein Einkommen durch Truppenkäufe.
- **D010:** Tripo-Kandidaten bleiben außerhalb des Builds, solange Export und konkrete Nutzungsrechte ungeklärt sind; siehe Docs/TripoAssets.md.

- **D011:** Nutzer beauftragt freie Kampfplatzierung auf größeren Karten. Integer-X/Y werden übertragen und gehasht; feste Wege bleiben erhalten. Geometrieprüfung ist von Gold-/Phasenprüfung getrennt, damit Snapshots bestehende Gebäude korrekt validieren. Hohe Dekoration liegt nur auf gemeinsamen logischen Sperrflächen.
- **D012:** Wiederholte Angriffskäufe sind sofort bezahlte FIFO-Aufträge. Klassenabstände gelten beim tatsächlichen Ausmarsch; ein volles Feld verwirft keine bezahlten Einheiten. Netzwerk-Snapshots bleiben auf 5 Hz begrenzt.
- **D013 (historisch, ersetzt durch D020):** Messgestützter Beta-Kandidat: 5 G/s, 160 Prozent des früheren Truppentempos und Kill-Gold. Historische Slotmessungen und neue freie Platzierungsregressionen werden getrennt dokumentiert; Botmatches beweisen keinen Spielspaß.
- **D014:** Präsentations-Tracker verbindet ACK und autoritativen Zustand in beliebiger Reihenfolge. Erst danach entsteht einmaliges Bau-/Upgradefeedback. Pointerbesitz, Pending-Flächen und Abbruchpfade sind eigene Präsentationszustände, keine optimistischen Spielkäufe.
- **D015:** Zwei bezahlte Ausbaukäufe insgesamt pro Kampfturm; Schaden und Reichweite bleiben getrennte Stufen. `TowerUpgradeRules` liefert gemeinsame Preis-/Statistikwerte für Core, Bot und UI. Sechs Modellvarianten entsprechen Basis, D, R, DD, DR/RD und RR. Minen behalten zwei Einkommensstufen.
- **D016:** Freie Minen benutzen denselben Geometriepfad wie Kampftürme. Das Klassenlimit bleibt zwei; es entstehen keine zusätzlichen reservierten Bereiche.
- **D017:** Gesten besitzen einen gemeinsamen Pointer-Gate. Aktive unterdrückte Kontakte bleiben bis Release blockiert, danach werden höchstens 64 abgeschlossene IDs aufbewahrt. Auswahl, Pan, Pinch und HUD-Käufe dürfen sich nicht gegenseitig auslösen.
- **D018:** Der neue Overlay-Entwurf übernimmt kompakte Bild-/Preiskarten und eine kontextabhängige Ausbauleiste. Der Browserentwurf ist eine Designreferenz; die tatsächlich verbundene Oberfläche bleibt Unity-uGUI und braucht eigene Render-/Eingabeprüfungen.

- **D019:** Wellenmodus 0.7 trennt echte Kontrollzeit und aktive Spielzeit. Zehn Grundwellen, vorbereitender Frühstart, Forschung 0–8 und vor Kauf bestätigte Zielwellen ersetzen die bisherige Zeitlimitpartie. Definitionen und Rezepte sind gehashter Content.
- **D020:** Wirtschaft v1.9: Basis 4 G/s, Minen 1/1,5/2 G/s, höchstens 3 G/s Angriffseinkommen aus tatsächlich ausmarschiertem Kaufwert. Exakte 1/960-Geldeinheiten und Wellenledger vermeiden Rundungs-/Reservierungsfehler. Neutrale Gegner zahlen kein Kopfgeld.
- **D021:** Lokale Siegesmarken sind vom Matchgold getrennt. Profilservice bindet einen Matchticket vor dem Ergebnis, schreibt Belohnung/Käufe atomar mit Backup und hält fehlgeschlagene Saves für Retry. Kein Cloudkonto- oder Cheat-Schutzversprechen.
- **D022:** Blitzspule und Balliste nutzen eigene originale Meshdaten mit sechs Ausbauvarianten. Der Editorimport erzeugt Basis/Kopf-Meshassets; Board und Porträtbäcker verwenden dieselben Daten. Zusätzliche Kartenorte sind statischer Teil des Terrainmeshs und verändern keine legale Baufläche.

- **D023:** Exakte BigInteger für Geld, Level, Preise und Kampfstärke ersetzen feste Ausbau- und Mengenlimits. Elf visuelleMeilensteine und kleine Pfadmodule halten Meshressourcen endlich; dynamische Pools zeigen alle tatsächlich simulierten Akteure. Core2.1/Schema8/Wire7 sind ein gemeinsamer neuer Kompatibilitätsstand.
- **D024:** Kleine5Hz-Snapshots bleiben unzuverlässig, große Zustände werden zuverlässig in Seiten mit ACK-Fenster übertragen. Laufende große Revisionen werden fertiggestellt, neue koalesziert; atomare Validierung vor Anwendung. Gastdarstellung interpoliert gepuffert Wegdistanz, ohne Treffer oder Gold vorherzusagen.
- **D025:** Eigener Shader, offline gebackene geometrischeAO und begrenzte Kontaktschatten-/Effektbatches verbessern Tiefe ohne Laufzeit-Raytracing. ReduzierteEffekte/Energiesparmodus begrenzen Aufwand; tatsächliche Geräteleistung separat messen.
- **D026:** Mine160G/40%Refund, Kampftürme65%, kürzereSendcooldowns und30sMindestfenster folgen vergleichbaren360Simulationspartien jeVariante. Queueprognosen sind unverbindliche Untergrenzen ohne neutrale Verzögerung; Preis-/Kaufautorität bleibt ausschließlich Core/Host.

## Einstieg und Menü 0.12

StudioIntro ist die erste Buildszene. Eine eigene Introassembly besitzt VideoPlayer, RenderTexture und Übergangsoverlay; Boot wird mit gehaltener Szenenaktivierung vorgeladen. Sichtbare aktive Wiedergabe steuert Skip und Timeouts, Fokus/Pause halten die Zeit an. Ende, Skip oder Decoderfehler geben Boot frei und entfernen alle Introressourcen. CLI-Netzwerktests umgehen das Intro. Der Scene-Setup-Code verweist auf die mitgelieferte Soballa-Studio-Animation; es gibt keinen zweiten EventSystembesitzer.

GameUI.Home baut das Startmenü aus feststehenden Touchflächen, sechs Kartenvorschauen, einem echten Diorama, Solo/Schwierigkeit und Netzwerkeinstiegen. Freischaltungen liegen im Arsenalmodal. MenuMotion bewegt nur dekorative Elemente; ReducedEffects, PowerSaving, Modal, Fokus/Pause und Deaktivierung stoppen sie. Der Spiel-Core und die Transportregeln bleiben unverändert.

## Bildschirmränder und Connect 0.13

Das Root-Canvas besitzt genau einen vollflächigen Background. Rebuild wählt für Home/Connect Navy; Screen und Bedienelemente bleiben unter dem SafeArea-Container. Der Hintergrund ist nicht raycastbar. Dadurch bleibt der Rand bei Displayaussparungen gefüllt, ohne Eingaben unter die Aussparung zu verschieben.

GameUI.Connect kapselt ausschließlich die Connect-Präsentation. BuildConnectScreen bindet die vorhandenen Formularfelder und Host/Join-Callbacks, Help öffnet den bestehenden modalen Eingabebereich. Die Portkonfiguration liegt in diesem Dialog, während ihre Validierung im bestehenden GameUI.ParsePort bleibt. Menüwechsel startet keine Verbindung; Fehler aus SessionCoordinator werden weiterhin in noticeText dargestellt.

## Team- und Endlosintegration0.15

Die Partie besitzt tatsächlich2,4oder6Spieler und2oder3Teams; gemeinsame Team-HP werden beim Dekodieren einmal gebunden. Ein bezahlter Auftrag behält sein konkretes gegnerisches Ziel. Hostrevisionen binden Kartenwahl, Regeln und ausgeglichene verbundene Teams an Ready; jeder Spieler rüstet seine eigenen Klassen aus. Slot und Verbindung bleiben getrennt. Pro Peer existieren eigene Sequenzbasis, ACK-/Transferfenster und Rate-Limits; eine nachträglich besetzte Lobbyposition übernimmt nur ihre autoritative Sequenzbasis, keine behauptete frühere Revanche.

Wellenidentitäten, Level und Gold bleiben BigInteger. Endlos verwendet unveränderliche Rezepte statt eines wachsenden Plans. Der Einkommensfaktor wirkt auf die exakte Summe aus Basis, Mine und vorab gedeckeltem Angriffseinkommen. Startgold, Preise, Rückerstattungen und Kopfgeld werden davon unabhängig berechnet. Details: Tools/Integration15/CoreApi.md.

LAN-Discovery läuft nur bei aktiver App. Browser hören aufUDP19175, Hosts senden über einen eigenen kurzlebigen Port. Das Verzeichnis prüft Protokoll, Katalog, Absender und TTL. Ein Banner bietet explizites Beitreten/Später; kein Autojoin. Androidrechte werden idempotent ins erzeugte Manifest eingefügt. Spielverbindung und Discovery besitzen getrennte Lebenszyklen.

Tesla und Laser verwenden begrenzte gemeinsame Beam-Meshes aus sichtbaren Snapshot-Kühlzeiten. DieseEffekte erfinden keinen zusätzlichen Schaden. Turmformen zeigen jeden bezahlten Gesamtlevel bis30 und begrenzte spätere Meilensteine, ergänzt um beidePfadmodule. Porträts werden offline aus denselben Modellen gebacken; Android nutzt512pxETC2RGBA8, keinRuntimeHUDkamerabetrieb.
