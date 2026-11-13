#!/usr/bin/env python3
"""Import a new isolated copy of Assets/Packages/ProjectSettings with no Library."""
import datetime
import json
import os
from pathlib import Path
import shutil
import subprocess

root = Path(__file__).resolve().parent.parent
stamp = datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%d-%H%M%S")
copy = root / "Builds" / ("FreshImport-" + stamp)
copy.mkdir(parents=True)
for folder in ("Assets", "Packages", "ProjectSettings"):
    shutil.copytree(root / folder, copy / folder)
assert not (copy / "Library").exists()
editor = os.environ.get("DEFENSE_UNITY_EDITOR", "/Applications/Unity/Hub/Editor/6000.3.28f1/Unity.app/Contents/MacOS/Unity")
log = root / "TestResults" / "fresh-import.log"
command = [editor, "-batchmode", "-nographics", "-quit", "-projectPath", str(copy),
           "-executeMethod", "DefenseDuell.Editor.BuildEntry.Prepare", "-logFile", str(log)]
result = subprocess.run(command)
text = log.read_text(errors="replace") if log.exists() else ""
portrait = {"required": (copy / "Assets/Art/ModelPortraits/v1/manifest.json").exists(), "passed": None}
if result.returncode == 0 and portrait["required"]:
    portrait_log = root / "TestResults" / "fresh-import-portraits.log"
    portrait_run = subprocess.run([editor, "-batchmode", "-nographics", "-quit", "-projectPath", str(copy),
                                   "-executeMethod", "DefenseDuell.Editor.ModelPortraitBaker.Verify", "-logFile", str(portrait_log)])
    portrait_text = portrait_log.read_text(errors="replace") if portrait_log.exists() else ""
    portrait.update({"exitCode": portrait_run.returncode,
                     "passed": portrait_run.returncode == 0 and "DEFENSE_MODEL_PORTRAITS_VERIFIED count=" + str(json.loads((copy / "Assets/Art/ModelPortraits/v1/manifest.json").read_text())["portraitCount"]) in portrait_text})
evidence = {"utc": stamp, "copy": str(copy), "startedWithoutLibrary": True,
            "exitCode": result.returncode, "passed": result.returncode == 0 and "DEFENSE_PREPARE_OK" in text and (not portrait["required"] or portrait["passed"]),
            "editor": editor, "scope": "Fresh isolated project import and connected content preparation; no APK or device claim",
            "modelPortraitVerification": portrait,
            "contentHashLines": [line for line in text.splitlines() if "DEFENSE_CONTENT_HASH " in line]}
(root / "TestResults" / "fresh-import.json").write_text(json.dumps(evidence, indent=2) + "\n")
print(json.dumps(evidence, indent=2))
raise SystemExit(0 if evidence["passed"] else 1)
