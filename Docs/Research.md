# Verifizierte Quellen und Asset-Herkunft

Abrufstand: 10. November 2026. Nur Herstellerquellen wurden für technische Entscheidungen verwendet. Dieser Bericht belegt Paketmetadaten und Downloads, noch keinen Unity-Import oder Gerätetest.

## Unity und Pakete

Unity **6000.3.28f1 LTS** wurde am 3. November 2026 veröffentlicht. Unity 6.3 LTS wird laut Hersteller bis Dezember 2027 unterstützt. [Release](https://unity.com/releases/editor/whats-new/6000.3.28f1), [Support](https://unity.com/releases/unity-6/support).

| Paket | Exakte Auswahl | Kompatibilitätsbeleg |
|---|---|---|
| Netcode for GameObjects | 2.13.3 | Registry verlangt Unity 6000.0 oder neuer, Transport 2.6.0 oder neuer; offizieller GitHub-Release hat `prerelease: false`. |
| Unity Transport | 2.7.5 | Offiziell für 6000.3 freigegeben; Registry: Unity 2022.3 oder neuer. |
| Multiplayer Services SDK | 2.4.1 | Offiziell für 6000.3 freigegeben; Registry: Unity 6000.0 oder neuer; verlangt Transport 2.7.4 oder neuer. |
| Input System | 1.20.1 | Offiziell für 6000.3 freigegeben; Registry: Unity 6000.0 oder neuer. |
| Unity Test Framework | 1.6.0 | Aus dem eingebauten `package.json` des installierten 6000.3.28f1 ausgelesen; NUnit 2.0.3. |

Quellen: [NGO-Release v2.13.3](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/releases/tag/v2.13.3), [Unity 6000.3.26f1 Paketaktualisierung](https://unity.com/releases/editor/whats-new/6000.3.26f1), [Transport](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.transport.html), [MPS](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.services.multiplayer.html), [Input](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.inputsystem.html), [Test Framework](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.test-framework.html).

Die normalisierten Registry-Belege liegen unter `ThirdParty/Evidence/*-package.json`. MPS 2.4.1 verlangt unter anderem Authentication 3.8.0, Core 1.18.0, Collections 2.6.8, QoS 1.5.1 und Newtonsoft JSON 3.2.2. Der endgültige Unity-Resolver muss den vollständigen Graphen in `Packages/packages-lock.json` festhalten und der Import muss kompilieren.

**Dokumentationswiderspruch:** Die aktuelle 6000.3-Handbuchseite ordnet NGO 2.13.3 unter „Pre-release packages“ ein. Die Registry führt die Version ohne Preview-Suffix, die offizielle Release-API setzt ausdrücklich `prerelease: false`, und die 6000.3.26f1-Release-Notes aktualisierten dieses Paket auf 2.13.3. Die Auswahl stützt sich auf diese konkreten Paket-/Release-Metadaten, nicht auf die widersprüchliche Navigationskategorie. Beleg: `ThirdParty/Evidence/ngo-github-release.json`. NGO 3.x ist keine Alternative für dieses Projekt: laut Registry verlangt es Unity 6000.7 Beta und führt zusätzliche Netcode-for-Entities-Abhängigkeiten ein.

## Online-Kosten und Grenzen

| Dienst | Kostenloser Anteil laut Hersteller | Verwendung im MVP |
|---|---|---|
| Authentication | Kostenlos | Technische anonyme Anmeldung nur für Online. Kein Kontoformular im Spiel. |
| Relay | 50 durchschnittliche monatliche CCU; 3 GiB pro CCU, höchstens 150 GiB/Monat insgesamt | Zwei Spieler, Host-Simulation, verschlüsseltes DTLS. |
| Lobby | 10 GiB/Monat pro regionaler Gruppe US+EU bzw. Asien+Australien | Nur relevant, falls MPS-Sessions Lobby tatsächlich verwenden. Bei direkter Relay-API ist kein Lobbyaufruf erforderlich. |
| Sessions | SDK-Abstraktion, kein eigener separater Backenddienst | Kosten folgen den tatsächlich genutzten darunterliegenden Diensten. |

Quelle: [UGS-Preisliste](https://unity.com/products/gaming-services/pricing), [Sessions-Erklärung](https://docs.unity.com/en-us/mps-sdk/index). Ohne Zahlungsmethode können Relay und Lobby innerhalb der kostenlosen Kontingente verwendet werden. Die bestehende Organisation muss vor Aktivierung überprüft werden; eine bereits eingerichtete Zahlungsart oder ein vorhandener kostenpflichtiger Plan darf nicht als Null-Euro-Garantie behandelt werden. Budgetwarnungen sind Benachrichtigungen, keine Kostenobergrenze. Es wurden durch diese Recherche keine Konten, Dienste, Zahlungspläne oder automatische Aufladungen aktiviert. [Offizielle Billing-FAQ](https://support.unity.com/hc/en-us/articles/6821475035412-Billing-FAQ-Unity-Gaming-Services).

Relay erlaubt pro authentifiziertem Spieler jeweils 60 Aufrufe/Minute für Allocation, Join-Code, Join und Regionsliste. Session-Erstellung ist auf 20/Minute und Session-Beitritt auf 1/Sekunde begrenzt. Das Spiel soll Startaktionen gegen Mehrfachklicks sperren und Fehler mit Rückkehr ins Menü behandeln. [Rate Limits](https://docs.unity.com/en-us/mps-sdk/rate-limits).

Authentication wird als kostenlos geführt; in den geprüften öffentlichen Quellen wurde kein belastbares numerisches anonymes Auth-Kontingent gefunden. Deshalb wird kein unbelegtes Request-Limit behauptet. Das SDK kann Sign-in-Fehler und Rate-Limiting zurückgeben; einmalige Initialisierung und kontrollierte Wiederholung sind erforderlich. Gespeicherte anonyme Sitzungen werden vom SDK wiederverwendet. [Session-Verwaltung](https://docs.unity.com/en-us/authentication/session-management).

Für die manuelle Relay-Anbindung ist die Nutzung der Relay-API **innerhalb von `com.unity.services.multiplayer`** aktuell dokumentiert. Ein separates veraltetes `com.unity.services.relay`-Paket ist nicht erforderlich. `AllocationUtils.ToRelayServerData(allocation, "dtls")` konfiguriert verschlüsselten Transport. [Offizielle MPS-/NGO-Anleitung](https://docs.unity.com/en-us/mps-sdk/tutorials/relay-and-ngo).

## Kenney-Assets

Alle drei Einzelpakete wurden direkt von den vom Urheber verlinkten URLs kostenlos heruntergeladen. HTTP 403 beim ersten Abruf ließ sich mit normalem Browser-User-Agent und der offiziellen Assetseite als Referer beheben; es wurde kein Drittanbieter-Mirror benutzt.

| Paket | Version | Importauswahl | Lizenzbeleg |
|---|---|---|---|
| Tower Defense (Top-Down) | 1.0 | 16 unveränderte 64×64-PNG-Einzelbilder in `Assets/Art/Kenney/TowerDefense` | `ThirdParty/Kenney-tower-defense-top-down/License.txt` |
| Interface Sounds | 1.0 | Click, Bestätigung, Fehler, Zurück: vier OGG-Dateien | `ThirdParty/Kenney-interface-sounds/License.txt` |
| UI Pack | 2.0 | Fünf blaue UI-Bilder und zwei Kenney-Future-Schriften | `ThirdParty/Kenney-ui-pack/License.txt` |

Exakte Lizenz für alle Pakete: **CC0 1.0 Universal**. Namensnennung ist nicht vorgeschrieben; freiwilliger Hinweis „Kenney.nl“ wird empfohlen. Die vom Download mitgelieferte Lizenz, originale Vorschauen, Original-ZIPs, vollständiger CC0-Lizenztext und SHA-256-Werte sind unter `ThirdParty/` erhalten. `ASSET_LICENSES.csv` ordnet die importierten Dateien den Belegen zu. Keine Codeübernahme aus Boss Room oder anderen Referenz-Repositories fand statt.

Quellen: [Tower Defense](https://kenney.nl/assets/tower-defense-top-down), [Interface Sounds](https://kenney.nl/assets/interface-sounds), [UI Pack](https://kenney.nl/assets/ui-pack), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).

Integrationshinweise: Sprites als Sprite (2D/UI), 64 Pixel pro Unity-Einheit, Filter Bilinear oder Point konsistent wählen, ohne Mipmaps. Die Turm-/Einheitenformen müssen Klassen zusätzlich zur Farbe unterscheiden. Kleine Auswahl der tatsächlich genutzten Sprites in einen SpriteAtlas übernehmen; unbenutzte Dateien werden ohne Referenz nicht in den Build aufgenommen. Interface-Sounds sind kurze Effekte und enthalten keine Musik. Musik muss separat selbst erzeugt oder separat lizenziert werden. Die beiden Display-Schriften auf deutsche Umlaute prüfen; für längere Bedienhinweise eine vollständig lesbare UI-Schrift verwenden.
