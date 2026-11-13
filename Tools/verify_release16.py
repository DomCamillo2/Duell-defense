#!/usr/bin/env python3
"""Inspect the finished APK's actual payload; does not claim device execution."""
import hashlib
import json
from pathlib import Path
import struct
import zipfile

root = Path(__file__).resolve().parents[1]
apk = root / "Builds/Android/DefenseDuell.apk"
metadata = json.loads((root / "TestResults/build-artifacts.json").read_text())
digest = hashlib.sha256(apk.read_bytes()).hexdigest()
assert metadata["version"] == "0.16.0" and metadata["versionCode"] == 17
assert digest == metadata["sha256"]
media_path = root / "Assets/Art/StudioIntro/soballa_studio_intro.mp4"
media = media_path.read_bytes()
hits, gaps = [], []
with zipfile.ZipFile(apk) as archive, apk.open("rb") as raw:
    assert archive.testzip() is None, "APK ZIP CRC failure"
    previous_end = 0
    for entry in sorted(archive.infolist(), key=lambda item: item.header_offset):
        gap = entry.header_offset - previous_end
        if gap > 1024 * 1024:
            gaps.append({"before": entry.filename, "bytes": gap})
        raw.seek(entry.header_offset)
        header = raw.read(30)
        assert header[:4] == b"PK\x03\x04"
        name_length, extra_length = struct.unpack_from("<HH", header, 26)
        previous_end = entry.header_offset + 30 + name_length + extra_length + entry.compress_size
        if entry.filename.startswith("assets/bin/Data/") and entry.filename.endswith(".resource"):
            contents = archive.read(entry)
            offset = contents.find(media)
            if offset >= 0:
                hits.append({"entry": entry.filename, "offset": offset, "bytes": len(media),
                             "sha256": hashlib.sha256(media).hexdigest()})
assert not gaps, gaps
assert len(hits) == 1, "Expected exactly one byte-identical intro video payload"
for name, evidence in {
    "apk-v16-packaging.json": {"passed": True, "apkSha256": digest, "bytes": apk.stat().st_size,
                                "largeGaps": gaps, "zipCrc": "passed"},
    "apk-media-v16.json": {"passed": True, "apkSha256": digest, "media": hits,
                           "scope": "Exact source MP4 in actual signed APK; no hardware decoder claim"},
}.items():
    (root / "TestResults" / name).write_text(json.dumps(evidence, indent=2) + "\n")
print(json.dumps({"passed": True, "apkSha256": digest, "mediaMatches": len(hits), "largeGaps": gaps}))
