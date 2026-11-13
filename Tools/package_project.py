#!/usr/bin/env python3
"""Create a local source archive without caches, build products or private keys."""
import hashlib
import re
from pathlib import Path
import zipfile

root = Path(__file__).resolve().parent.parent
version=re.search(r"(?m)^  bundleVersion: (.+)$",(root/"ProjectSettings/ProjectSettings.asset").read_text()).group(1)
series="v"+version.split(".")[1]
destination = root / "Artifacts" / ("DefenseDuell-"+".".join(version.split(".")[:2])+"-Source.zip")
destination.parent.mkdir(exist_ok=True)
folders = ("Assets", "Packages", "ProjectSettings", "Tools", "Docs", "ThirdParty", "ArtCandidates")
top_files = (".gitignore", ".gitattributes", "AGENTS.md", "PROJECT_STATE.md", "README.md", "SETUP.md", "ARCHITECTURE.md", "BACKLOG.md",
             "PROGRESS.md", "TEST_REPORT.md", "ASSET_LICENSES.csv", "THIRD_PARTY_NOTICES.md")
excluded_parts = {"bin", "obj", "__pycache__", ".DS_Store", "node_modules"}
forbidden_suffixes = {".keystore", ".jks", ".p12", ".mobileprovision"}
excluded_drafts = ("Tools/TeamSiegeLab/", "Docs/ResearchEvidence/team-siege-v1/", "Tools/NextVisualUpdateDraft/", "Tools/TeamMultiplayerCandidate/") if "bundleVersion: 0.14." in (root / "ProjectSettings/ProjectSettings.asset").read_text() else ("Tools/TeamSiegeLab/", "Docs/ResearchEvidence/team-siege-v1/", "Tools/HostSettingsDraft/", "Tools/HostRulesLab/") if "bundleVersion: 0.13.0" in (root / "ProjectSettings/ProjectSettings.asset").read_text() else ("Tools/StudioIntroLab/", "Tools/MenuPlayfulDraft/") if "bundleVersion: 0.11.0" in (root / "ProjectSettings/ProjectSettings.asset").read_text() else ()
files = [root / name for name in top_files if (root / name).is_file()]
for folder in folders:
    for path in sorted((root / folder).rglob("*")):
        if not path.is_file() or path.is_symlink():
            continue
        relative = path.relative_to(root)
        if relative.as_posix().startswith(excluded_drafts):
            continue
        if relative.as_posix() == "Docs/ResearchEvidence/mactician-v07/emulator-start.log":
            continue  # Live process output; the immutable captured evidence is packaged instead.
        if any(part in excluded_parts or part.startswith(("bin-", "obj-")) or part.endswith(("-bin", "-obj")) for part in relative.parts) or path.suffix in forbidden_suffixes:
            continue
        if path.name.startswith(".env"):
            continue
        files.append(path)
# Include the final connection-screen visual proofs referenced by the0.13review.
files.extend(sorted((root / "TestResults/Connect-v13").glob("*.png")))
files.extend(sorted((root / "TestResults/BombRadius-v14").glob("*.png")))
files.extend(sorted((root / "TestResults/Lobby-v14").glob("*.png")))
files.extend(sorted((root / "TestResults/Lobby-v141").glob("*.png")))
for model_folder in ("hostrules-v14-network-model", "hostrules200-v141-network-model"):
    for path in (root / "TestResults" / model_folder).rglob("*"):
        if path.is_file() and path.suffix != ".log": files.append(path)
# Include current0.15 native proofs and source/artifact bindings. Raw process logs and Relay codes stay local.
for folder in ("Maps-v15", "Maps-v15-polish", "HighEvolution-v15", "UpgradeVisuals-v15", "Energy-v15", "Hud-v15", "Screens-v15", "Lobby-v15"):
    files.extend(path for path in (root / "TestResults" / folder).rglob("*") if path.is_file() and path.suffix in (".png", ".json"))
for folder in ("core-v15-final",):
    files.extend((root / "TestResults" / folder).glob("*.json"))
for pattern in ("*v15*.json", "*v15*.xml", "*qa15*.json", "*qa15*.xml"):
    files.extend((root / "TestResults").glob(pattern))
for folder in ("team15-lan-2v2-retry", "team15-lan-3v3", "team15-lan-2v2v2", "team15-relay-2v2", "team15-relay-3v3-retry", "team15-relay-2v2v2", "team15-endless-start", "duel15-rules-lan", "duel15-impaired"):
    files.extend(path for path in (root / "TestResults" / folder).glob("*.json") if path.name != "relay-code.json")

for folder in ("team15-final-lan-2v2", "team15-final-lan-3v3", "team15-final-lan-2v2v2", "team15-final-relay-2v2", "team15-final-relay-3v3", "team15-final-relay-2v2v2", "team15-final-endless-start", "duel15-final-impaired", "duel15-final-relay200-start", "discovery-v15-final"):
    files.extend(path for path in (root / "TestResults" / folder).glob("*.json") if path.name != "relay-code.json")

# Current release proofs, including exact core/source bindings. No raw process logs or relay codes.
for pattern in ("*v16*.json", "*v16*.xml", "core-v16-final/*.json", "features16-*/*.json", "team16-*/*.json", "duel16-*/*.json"):
    files.extend(path for path in (root/"TestResults").glob(pattern) if path.name not in ("relay-code.json","source-artifacts-v16.json"))
# Include compact verification summaries; raw logs and ephemeral Relay codes stay local.
files.extend(sorted((root / "TestResults").glob("**/comparison*.json")))
files.extend(sorted((root / "TestResults").glob("**/proxy.json")))
files.extend(sorted((root / "TestResults").glob("network-v*-source-manifest.json")))
files.extend(sorted((root / "TestResults").glob("network-v*-acceptance.json")))
files.extend(sorted((root / "TestResults").glob("build-artifacts-v*.json")))
files.extend(sorted((root / "TestResults").glob("fresh-import-v*.json")))
for name in ("editmode-v141.xml", "playmode-v141.xml", "waves200-v141-source-manifest.json", "mac-waves200-v141.json", "device-v141.json", "project-inventory-v141.json", "apk-v141-packaging.json", "apk-media-v141.json", "playmode-v14-first.xml", "hostrules-v14-play-first-source-manifest.json", "hostrules-v14-before-capture-routing-source-manifest.json", "capture-routing-v14.json", "wire8-core-harness-evidence.json", "apk-v14-packaging.json", "apk-media-v14.json", "editmode-v14-first.xml", "playmode-v14-targeted.xml", "playmode-v14-targeted-clockfix.xml", "hostrules-v14-initial-source-manifest.json", "hostrules-v14-clockfix-source-manifest.json", "hostrules-v14-first-full-source-manifest.json", "portrait-v14-pixel-comparison.json", "editmode-v14.xml", "playmode-v14.xml", "hostrules-v14-source-manifest.json", "mac-hostrules-v14.json", "device-v14.json", "project-inventory-v14.json", "fresh-import.json", "build-artifacts.json",
             "editmode.xml", "playmode.xml", "editmode-v07.xml", "playmode-v07.xml", "playmode-v07-hud-final.xml",
             "integrated-v07-source-manifest.json", "mac-wave-v07.json", "bot-v07-root-review.json",
             "playmode-v13-first-full.xml", "playmode-v13-connect-initial.xml", "playmode-v13-connect.xml", "playmode-v13-slider.xml", "connect-v13-initial-source-manifest.json", "connect-v13-first-full-source-manifest.json",
             "editmode-v13.xml", "playmode-v13.xml", "connect-v13-source-manifest.json", "mac-connect-v13.json", "apk-icon-v13.json", "apk-media-v13.json", "device-v13.json", "v13-reuse-audit.json", "v13-binary-reuse.json", "apk-v13-packaging.json", "project-inventory-v13.json",
             "editmode-v12.xml", "playmode-v12.xml", "studio-v12-source-manifest.json", "studio-v12-final-deltas.json", "mac-studio-v12.json", "apk-icon-v12.json", "intro-v12-source-manifest.json", "v12-rule-reuse-audit.json", "v12-binary-reuse.json", "v12-binary-semantic-review.json", "apk-v12-packaging.json", "playmode-v12-sharpness.xml", "portrait-v12-pixel-comparison.json", "playmode-v12-intro.xml", "playmode-v12-startup.xml", "apk-media-v12.json", "device-v12.json",
             "editmode-v11.xml", "playmode-v11.xml", "economy-v11-source-manifest.json", "mac-economy-v11.json", "mac-economy-v11-final.json", "playmode-v11-hud-final.xml",
             "economy-v11-final-deltas.json", "portrait-v11-pixel-comparison.json", "apk-icon-v11.json", "apk-v11-final-packaging.json", "device-v11.json",
             "apk-v10-source-manifest.json", "playmode-v10-tooltip.xml", "graphics-field-v10-source-manifest.json",
             "editmode-v10.xml", "playmode-v10.xml", "fullscreen-v10-source-manifest.json", "fullscreen-v10-baseline.json", "mac-fullscreen-v10.json",
             "playmode-v09.xml", "immersive-v09-source-manifest.json", "immersive-v09-baseline.json", "mac-immersive-v09.json",
             "playmode-v09-first.xml", "immersive-v09-source-manifest-first.json",
             "playmode-v08.xml", "playmode-v08-hud-final.xml", "compact-hud-v08-source-manifest.json",
             "compact-hud-v08-final-source-manifest.json", "compact-hud-v08-baseline.json", "mac-hud-v08.json",
             "unlock-models-v07.json", "progression-v07-pure.json", "visual-acceptance.json"):
    path = root / "TestResults" / name
    if path.is_file():
        files.append(path)
files = [path for path in files if path.name not in ("source-artifacts-v15.json","source-artifacts-v16.json")]
with zipfile.ZipFile(destination, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    for path in sorted(set(files)):
        archive.write(path, "DefenseDuell/" + str(path.relative_to(root)))
digest = hashlib.sha256(destination.read_bytes()).hexdigest()
(destination.with_suffix(".zip.sha256")).write_text(digest + "  " + destination.name + "\n")
print(f"{destination}: {destination.stat().st_size} bytes, {len(set(files))} files, SHA256 {digest}")
