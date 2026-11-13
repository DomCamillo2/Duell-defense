#!/usr/bin/env python3
"""Verify the actual APK manifest, native ABI, signing and digest with Unity's SDK."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import zipfile


def main():
    root = Path(__file__).resolve().parent.parent
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("apk", nargs="?", default=str(root / "Builds/Android/DefenseDuell.apk"))
    parser.add_argument("--android-tools", default="/Applications/Unity/Hub/Editor/6000.3.28f1/PlaybackEngines/AndroidPlayer")
    args = parser.parse_args()
    apk = Path(args.apk).resolve()
    android = Path(args.android_tools)
    build_tools = android / "SDK/build-tools/36.0.0"
    env = dict(os.environ, JAVA_HOME=str(android / "OpenJDK"))
    signing = subprocess.check_output([str(build_tools / "apksigner"), "verify", "--verbose", "--print-certs", str(apk)], env=env, text=True)
    manifest = subprocess.check_output([str(build_tools / "aapt"), "dump", "badging", str(apk)], env=env, text=True)

    def field(pattern):
        match = re.search(pattern, manifest)
        if not match:
            raise ValueError("APK manifest field missing: " + pattern)
        return match.group(1)

    with zipfile.ZipFile(apk) as archive:
        names = archive.namelist()
    abis = sorted({name.split("/")[1] for name in names if name.startswith("lib/") and name.endswith(".so")})
    assert abis == ["arm64-v8a"], abis
    assert "lib/arm64-v8a/libil2cpp.so" in names, "IL2CPP library missing"
    assert "Verified using v2 scheme (APK Signature Scheme v2): true" in signing, signing
    package = field(r"package: name='([^']+)'")
    assert package == "de.defenseduell.game", package
    version, code = field(r"versionName='([^']+)'"), int(field(r"versionCode='(\d+)'"))
    settings = (root / "ProjectSettings/ProjectSettings.asset").read_text()
    assert version == re.search(r"^  bundleVersion: (.+)$", settings, re.M).group(1)
    assert code == int(re.search(r"^  AndroidBundleVersionCode: (\d+)$", settings, re.M).group(1))
    assert "uses-gl-es: '0x30000'" in manifest, "Expected mandatory OpenGLES3"
    permissions = sorted(re.findall(r"uses-permission: name='([^']+)'", manifest))
    if version in ("0.15.0", "0.16.0"):
        required = {"android.permission.INTERNET", "android.permission.ACCESS_NETWORK_STATE",
                    "android.permission.ACCESS_WIFI_STATE", "android.permission.CHANGE_WIFI_MULTICAST_STATE"}
        assert required.issubset(permissions), sorted(required.difference(permissions))
    minimum, target = int(field(r"sdkVersion:'(\d+)'")), int(field(r"targetSdkVersion:'(\d+)'"))
    assert (minimum, target) == (25, 35), (minimum, target)
    digest = hashlib.sha256(apk.read_bytes()).hexdigest()
    certificate = re.search(r"Signer #1 certificate DN: (.+)", signing).group(1)
    assert "Android Debug" in certificate, "Expected the documented private-test certificate"
    assert "application-debuggable" in manifest, "Expected the documented Development test build"
    evidence = {
        "file": str(apk), "bytes": apk.stat().st_size, "MiB": round(apk.stat().st_size / 1048576, 2),
        "sha256": digest, "version": version, "versionCode": code,
        "unity": (root / "ProjectSettings/ProjectVersion.txt").read_text().splitlines()[0].split(": ", 1)[1],
        "target": "Android ARM64 IL2CPP GLES3", "abis": abis, "package": package,
        "configuration": "Development; Android debug test signature",
        "debuggable": "application-debuggable" in manifest,
        "deviceTest": "Not verified by this APK inspection; consult the version-bound device review.",
        "minSdk": minimum, "targetSdk": target,
        "launchActivity": field(r"launchable-activity: name='([^']+)'"),
        "verifiedSignatureScheme": "APK v2", "certificate": certificate,
        "apksignerVerified": True,
        "permissions": permissions,
        "requiredGles": "3.0",
    }
    results = root / "TestResults"
    results.mkdir(exist_ok=True)
    (results / "build-artifacts.json").write_text(json.dumps(evidence, indent=2) + "\n")
    (results / "apk-verification.log").write_text(signing + "\n" + manifest)
    apk.with_suffix(apk.suffix + ".sha256").write_text(digest + "  " + apk.name + "\n")
    print(json.dumps(evidence, indent=2))


if __name__ == "__main__":
    main()
