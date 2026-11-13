#!/usr/bin/env python3
"""Read-only project inventory. A test XML or an existing app is not a release approval."""
import argparse
import datetime
import hashlib
import json
from pathlib import Path
import plistlib
import re
import subprocess
import xml.etree.ElementTree as ET


def digest(path):
    if not path.is_file():
        return None
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(chunk)
    return value.hexdigest()


def read_json(path):
    try:
        value = json.loads(path.read_text())
        if not isinstance(value, dict):
            raise ValueError("Expected a JSON object")
        return value
    except (OSError, ValueError) as error:
        return {"read_error": str(error)}


def inside(root, name):
    path = (root / name).resolve()
    if not path.is_relative_to(root.resolve()):
        raise ValueError("Manifest path is outside this project: " + str(name))
    return path


def frozen_check(root, manifest):
    data = read_json(manifest)
    result = {"manifest": str(manifest), "status": "unavailable", "files_checked": 0, "differences": []}
    sources = data.get("sources", data.get("files"))
    if not isinstance(sources, dict) or not sources:
        result["reason"] = data.get("read_error", "Manifest has no source hashes")
        return result
    try:
        for name, expected in sources.items():
            if not isinstance(expected, str) or not re.fullmatch(r"[0-9a-fA-F]{64}", expected):
                raise ValueError("Invalid SHA-256 for " + str(name))
            actual = digest(inside(root, name))
            result["files_checked"] += 1
            if actual != expected.lower():
                result["differences"].append({"path": name, "expected": expected, "actual": actual})
    except (OSError, TypeError, ValueError) as error:
        result["reason"] = str(error)
        return result
    result["status"] = "match" if not result["differences"] else "mismatch"
    result["scope"] = data.get("scope", "Only files explicitly listed in the manifest")
    return result


def tests(path):
    result = {"path": str(path), "source_binding": "Result file only; does not prove current source was tested"}
    try:
        node = ET.parse(path).getroot()
        result.update({key: node.get(key) for key in ("result", "total", "passed", "failed", "start-time", "end-time")})
        result["sha256"] = digest(path)
    except (OSError, ET.ParseError) as error:
        result["read_error"] = str(error)
    return result


def app(path):
    result = {"path": str(path), "present": path.is_dir(), "running": "not inferred"}
    if not result["present"]:
        return result
    try:
        data = plistlib.loads((path / "Contents/Info.plist").read_bytes())
        result.update(version=data.get("CFBundleShortVersionString"), bundle_id=data.get("CFBundleIdentifier"))
        result["assemblies"] = {
            name: digest(path / "Contents/Resources/Data/Managed" / name)
            for name in ("DefenseDuell.Core.dll", "DefenseDuell.Presentation.dll", "DefenseDuell.Networking.dll")
        }
    except (OSError, ValueError, plistlib.InvalidFileException) as error:
        result["read_error"] = str(error)
    return result


def git(root, *args):
    result = subprocess.run(["git", "-C", str(root), *args], text=True, capture_output=True)
    return result.stdout.strip() if result.returncode == 0 else None


def value(root, filename, pattern):
    try:
        match = re.search(pattern, (root / filename).read_text())
        return match.group(1) if match else None
    except OSError:
        return None


def inventory(root, manifest):
    version = value(root, "ProjectSettings/ProjectSettings.asset", r"(?m)^  bundleVersion: (.+)$")
    protocol = value(root, "Assets/Networking/WireProtocol.cs", r"public const ushort Version\s*=\s*(\d+)")
    dirty = git(root, "status", "--porcelain", "--untracked-files=normal")
    result = {
        "schema_version": 1,
        "observed_at_utc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "project_root": str(root),
        "git": {"head": git(root, "rev-parse", "HEAD"), "branch": git(root, "branch", "--show-current"),
                "dirty_entries": len(dirty.splitlines()) if dirty is not None else None},
        "source": {
            "app_version": version,
            "rules_version": value(root, "Assets/Core/GameContent.cs", r'SimulationRulesVersion\s*=\s*"([^"]+)"'),
            "protocol_version": protocol,
            "content_schema": value(root, "Assets/Content/Standard.asset", r"(?m)^  schemaVersion: (\d+)$"),
            "presentation_hashes": {name: digest(root / "Assets/Presentation" / name)
                                    for name in ("ScenicTerrain.cs", "DioramaMeshes.cs", "BoardView.cs", "GameUI.cs")},
        },
        "frozen_network_sources": frozen_check(root, manifest),
        "unity_results": [tests(root / "TestResults" / (name + ".xml")) for name in (("editmode-v16", "playmode-v16") if version == "0.16.0" else ("editmode-v15", "playmode-v15") if version == "0.15.0" else ("editmode-v141", "playmode-v141") if version == "0.14.1" else ("editmode-v14", "playmode-v14") if version == "0.14.0" else ("editmode-v13", "playmode-v13") if version == "0.13.0" else ("editmode-v12", "playmode-v12") if version == "0.12.0" else ("editmode-v11", "playmode-v11") if version == "0.11.0" else ("editmode-v10", "playmode-v10") if version == "0.10.0" else ("playmode-v09",) if version == "0.9.0" else ("playmode-v08", "playmode-v08-hud-final") if version == "0.8.0" else ("editmode-v07", "playmode-v07", "playmode-v07-hud-final") if version == "0.7.0" else ("editmode", "playmode"))],
        "desktop_artifacts": [app(root / "Builds" / folder / "DefenseDuell.app")
                              for folder in ("macOS-Preview", "macOS-Next", "macOS-Candidate", "macOS-ArtReview", "macOS-SpeedReview", "macOS-WaveReview", "macOS-HudReview", "macOS-ImmersiveReview", "macOS-FullscreenReview", "macOS-EconomyReview", "macOS-EconomyFinalReview", "macOS-StudioReview", "macOS-ConnectionReview", "macOS-HostRulesReview", "macOS-Waves200Review", "macOS-AllFeaturesReview", "macOS-Update16Review")],
        "network_results": {},
    }
    folders = ("features16-lan", "team16-lan-3v3", "team16-lan-2v2v2", "features16-relay", "team16-relay-3v3", "team16-relay-2v2v2", "duel16-impaired", "duel16-relay200-start") if version == "0.16.0" else ("team15-final-lan-2v2", "team15-final-lan-3v3", "team15-final-lan-2v2v2", "team15-final-relay-2v2", "team15-final-relay-3v3", "team15-final-relay-2v2v2", "team15-final-endless-start", "duel15-final-impaired", "duel15-final-relay200-start") if version == "0.15.0" else ("lan-waves200-v141", "relay-waves200-v141") if version == "0.14.1" else ("lan-hostrules-v14", "relay-hostrules-v14") if version == "0.14.0" else ("lan-connect-v13", "relay-connect-v13") if version == "0.13.0" else ("lan-studio-v12", "relay-studio-v12") if version == "0.12.0" else ("lan-economy-v11", "lan-impaired-economy-v11-final", "relay-economy-v11-final") if version == "0.11.0" else ("lan-fullscreen-v10", "lan-impaired-fullscreen-v10", "relay-fullscreen-v10") if version == "0.10.0" else ("lan-immersive-v09",) if version == "0.9.0" else ("lan-compact-v08",) if version == "0.8.0" else ("lan-wave-0.7-v6", "lan-impaired-wave-0.7-v6", "relay-wave-0.7-v6") if version == "0.7.0" else ("lan-speed-v06", "lan-speed-impaired-v06", "relay-speed-v06") if version == "0.6.0" else tuple(f"{stem}-v{protocol}" for stem in ("lan-clean", "lan-impaired", "lan-complete", "relay-complete"))
    for folder in folders:
        path = root / "TestResults" / folder / "comparison.json"
        review = path.with_name("comparison-final-review.json")
        if review.is_file(): path = review
        data = read_json(path)
        result["network_results"][folder] = {
            "path": str(path), "passed": data.get("passed"), "read_error": data.get("read_error"),
            "cycles": len(data.get("cycles", [])) or len({r.get("cycle") for r in data.get("rounds", [])}),
            "completed_rounds": sum(len(c.get("completed_rounds", [])) for c in data.get("cycles", [])) + sum(bool(r.get("result")) for r in data.get("rounds", [])),
        }
    metadata = read_json(root / "TestResults/build-artifacts.json")
    apk = {"recorded_version": metadata.get("version"), "matches_project_version": metadata.get("version") == version,
           "recorded_path": metadata.get("file"), "checksum_matches": None}
    if metadata.get("file"):
        try:
            actual = digest(inside(root, metadata["file"]))
            apk.update(actual_sha256=actual, checksum_matches=actual is not None and actual == metadata.get("sha256"))
        except ValueError as error:
            apk["read_error"] = str(error)
    else:
        apk["read_error"] = metadata.get("read_error", "No APK manifest")
    result["apk"] = apk
    result["meaning"] = "Read-only evidence inventory, not a release decision or a running-app detector. PROJECT_STATE.md records the lead's handoff."
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", help="Defaults to the source manifest for the current WireProtocol version")
    parser.add_argument("--check-frozen", action="store_true", help="Return nonzero if the declared frozen source set changed or cannot be checked")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    protocol = value(root, "Assets/Networking/WireProtocol.cs", r"public const ushort Version\s*=\s*(\d+)")
    version = value(root, "ProjectSettings/ProjectSettings.asset", r"(?m)^  bundleVersion: (.+)$")
    manifest = args.manifest or ("TestResults/integration-v16-source-manifest.json" if version == "0.16.0" else "TestResults/integration-v15-source-manifest.json" if version == "0.15.0" else "TestResults/network-v8-waves200-v141-source-manifest.json" if version == "0.14.1" else "TestResults/network-v8-hostrules-v14-source-manifest.json" if version == "0.14.0" else "TestResults/network-v7-connect-v13-source-manifest.json" if version == "0.13.0" else "TestResults/network-v7-studio-v12-source-manifest.json" if version == "0.12.0" else "TestResults/network-v7-economy-v11-source-manifest.json" if version == "0.11.0" else "TestResults/apk-v10-source-manifest.json" if version == "0.10.0" else "TestResults/network-v6-immersive-v09-source-manifest.json" if version == "0.9.0" else "TestResults/network-v6-compact-v08-source-manifest.json" if version == "0.8.0" else "TestResults/network-v6-wave-v07-source-manifest.json" if protocol == "6" else "TestResults/network-v5-speed-v06-source-manifest.json" if protocol == "5" else f"TestResults/network-v{protocol}-source-manifest.json")
    report = inventory(root, inside(root, manifest))
    print(json.dumps(report, indent=2, ensure_ascii=False))
    if args.check_frozen:
        return {"match": 0, "mismatch": 1, "unavailable": 2}[report["frozen_network_sources"]["status"]]
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
