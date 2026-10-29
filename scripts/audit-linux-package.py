"""Audit the public tarball, executable modes, paths and per-file checksums."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path, PurePosixPath
import tarfile

spec = importlib.util.spec_from_file_location("windows_audit", Path(__file__).with_name("audit-windows-package.py"))
windows = importlib.util.module_from_spec(spec)
spec.loader.exec_module(windows)
REQUIRED = {
    "HuntersPC/HuntersPC", "HuntersPC/HuntersPC.sh", "HuntersPC/HuntersPC.dll",
    "HuntersPC/HuntersPC.runtimeconfig.json", "HuntersPC/HuntersPC.deps.json", "HuntersPC/NcsfPlay.dll",
    "HuntersPC/libcoreclr.so", "HuntersPC/libhostfxr.so", "HuntersPC/libhostpolicy.so",
    "HuntersPC/libglfw.so.3", "HuntersPC/libminiaudio.so", "HuntersPC/install.sh", "HuntersPC/install-linux.py",
    "HuntersPC/hunterspc.svg", "HuntersPC/LICENSE", "HuntersPC/README-LINUX.md", "HuntersPC/README-PC.md",
    "HuntersPC/README-UPSTREAM.md", "HuntersPC/THIRD-PARTY-PC.md", "HuntersPC/RELEASE_NOTES.md",
    "HuntersPC/licenses/glfw/LICENSE.md", "HuntersPC/licenses/opentk/LICENSE.md",
    "HuntersPC/package-manifest.json",
}


def audit(archive):
    with tarfile.open(archive, "r:gz") as package:
        entries = package.getmembers()
        names = set()
        files = {}
        for entry in entries:
            path = PurePosixPath(entry.name)
            parts = [part.lower() for part in path.parts]
            if (not parts or parts[0] != "hunterspc" or path.is_absolute() or ".." in parts or "\\" in entry.name
                or any(part in windows.BLOCKED_DIRECTORIES for part in parts)
                or path.name.lower() in windows.BLOCKED_NAMES or path.suffix.lower() in windows.BLOCKED_EXTENSIONS
                or not (entry.isfile() or entry.isdir()) or entry.name in names):
                raise ValueError(f"Forbidden package entry: {entry.name}")
            names.add(entry.name)
            if entry.isfile():
                files[entry.name] = entry
        missing = REQUIRED - files.keys()
        if missing:
            raise ValueError(f"Missing release components: {sorted(missing)}")
        manifest = json.load(package.extractfile("HuntersPC/package-manifest.json"))
        if manifest.get("Platform") != "linux-x64" or manifest.get("GameDataIncluded") is not False:
            raise ValueError("Incorrect package platform or game-data declaration")
        expected = {"HuntersPC/" + item["Path"] for item in manifest["Files"]}
        if len(expected) != len(manifest["Files"]) or files.keys() - {"HuntersPC/package-manifest.json"} != expected:
            raise ValueError("Archive differs from manifest")
        for item in manifest["Files"]:
            data = package.extractfile("HuntersPC/" + item["Path"]).read()
            if len(data) != item["Bytes"] or hashlib.sha256(data).hexdigest() != item["Sha256"]:
                raise ValueError(f"Manifest checksum mismatch: {item['Path']}")
        for name in ("HuntersPC", "HuntersPC.sh", "install.sh"):
            if not files["HuntersPC/" + name].mode & 0o111:
                raise ValueError(f"Missing executable mode: {name}")
        elf = package.extractfile("HuntersPC/HuntersPC").read(20)
        if elf[:6] != b"\x7fELF\x02\x01" or elf[18:20] != b"\x3e\x00":
            raise ValueError("The launcher is not a Linux x86-64 ELF executable")
        return {"Valid": True, "Archive": archive.name, "Files": len(files), "Platform": "linux-x64",
                "UncompressedBytes": sum(entry.size for entry in files.values()), "GameDataIncluded": False,
                "Sha256": hashlib.sha256(archive.read_bytes()).hexdigest(),
                "Version": manifest["Version"], "SourceCommit": manifest["SourceCommit"]}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    rendered = json.dumps(audit(args.archive), indent=2) + "\n"
    if args.output:
        args.output.write_text(rendered)
    print(rendered, end="")
