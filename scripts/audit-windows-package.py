"""Check a Windows release ZIP without reading or publishing any local game data."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import zipfile

BLOCKED_DIRECTORIES = {"files", "savedata", "evidence", "roms", "_bin", "_archives", ".git", "local"}
BLOCKED_NAMES = {"paths.txt", "pc-profile.json", "rom-import.json", "hunters-error.log"}
BLOCKED_EXTENSIONS = {".nds", ".sav", ".dsv", ".sdat", ".ncsf", ".minincsf", ".sseq", ".swav", ".bin", ".png", ".jpg", ".jpeg", ".pdb", ".log"}
REQUIRED = {"HuntersPC/HuntersPC.exe", "HuntersPC/HuntersPC.dll", "HuntersPC/HuntersPC.runtimeconfig.json", "HuntersPC/HuntersPC.deps.json", "HuntersPC/NcsfPlay.dll", "HuntersPC/coreclr.dll", "HuntersPC/hostfxr.dll", "HuntersPC/hostpolicy.dll", "HuntersPC/glfw3.dll", "HuntersPC/OpenAL32.dll", "HuntersPC/README-PC.md", "HuntersPC/README-UPSTREAM.md", "HuntersPC/LICENSE", "HuntersPC/THIRD-PARTY-PC.md", "HuntersPC/package-manifest.json"}


def audit(archive: Path) -> dict:
    with zipfile.ZipFile(archive) as package:
        entries = [entry for entry in package.infolist() if not entry.is_dir()]
        names = {entry.filename for entry in entries}
        forbidden = []
        for entry in entries:
            path = PurePosixPath(entry.filename.replace("\\", "/"))
            parts = [part.lower() for part in path.parts]
            if path.is_absolute() or ".." in parts or parts[0] != "hunterspc" or any(p in BLOCKED_DIRECTORIES for p in parts) or path.name.lower() in BLOCKED_NAMES or path.suffix.lower() in BLOCKED_EXTENSIONS:
                forbidden.append(entry.filename)
        if forbidden:
            raise ValueError(f"Forbidden package contents: {forbidden}")
        missing = sorted(REQUIRED - names)
        if missing:
            raise ValueError(f"Missing release components: {missing}")
        manifest = json.loads(package.read("HuntersPC/package-manifest.json"))
        expected_names = {"HuntersPC/" + item["Path"] for item in manifest["Files"]}
        if names - {"HuntersPC/package-manifest.json"} != expected_names:
            raise ValueError("ZIP contents differ from the package manifest")
        for item in manifest["Files"]:
            data = package.read("HuntersPC/" + item["Path"])
            if len(data) != item["Bytes"] or hashlib.sha256(data).hexdigest() != item["Sha256"]:
                raise ValueError(f"Manifest checksum mismatch: {item['Path']}")
        if not package.read("HuntersPC/HuntersPC.exe").startswith(b"MZ"):
            raise ValueError("The launcher is not a Windows executable")
        return {"Valid": True, "Archive": archive.name, "Files": len(entries), "UncompressedBytes": sum(e.file_size for e in entries), "GameDataIncluded": False, "Sha256": hashlib.sha256(archive.read_bytes()).hexdigest(), "Version": manifest["Version"], "SourceCommit": manifest["SourceCommit"]}


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("archive", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    result = audit(args.archive)
    rendered = json.dumps(result, indent=2)
    if args.output:
        args.output.write_text(rendered + "\n", encoding="utf-8")
    print(rendered)
