"""Build a fresh Linux x64 release with no local ROM, profile or save files."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import tarfile
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def package(version):
    if not re.fullmatch(r"v\d+\.\d+\.\d+(?:-[A-Za-z0-9.]+)?", version):
        raise ValueError("Versao de pacote invalida.")
    release = ROOT / "artifacts" / version
    release.mkdir(parents=True, exist_ok=True)
    archive = release / f"MetroidPrimeHuntersPC-{version}-linux-x64.tar.gz"
    if archive.exists():
        raise FileExistsError(f"Preserve o pacote existente ou escolha outra versao: {archive}")
    with tempfile.TemporaryDirectory(prefix="staging-linux-", dir=ROOT / "artifacts") as stage:
        output = Path(stage) / "HuntersPC"
        subprocess.run(["bash", str(ROOT / "scripts" / "build-linux.sh"), str(output)], check=True)
        commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
        dirty = bool(subprocess.check_output(["git", "status", "--porcelain", "--untracked-files=normal"], cwd=ROOT))
        manifest = {"Version": version, "Platform": "linux-x64", "SourceCommit": commit,
                    "SourceDirty": dirty, "GameDataIncluded": False, "Files": []}
        for path in sorted(output.rglob("*")):
            if path.is_file():
                data = path.read_bytes()
                manifest["Files"].append({"Path": path.relative_to(output).as_posix(), "Bytes": len(data),
                                          "Sha256": hashlib.sha256(data).hexdigest()})
        (output / "package-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
        with tarfile.open(archive, "w:gz") as tar:
            tar.add(output, arcname="HuntersPC")
    subprocess.run(["python3", str(ROOT / "scripts" / "audit-linux-package.py"), str(archive),
                    "--output", str(release / "package-audit-linux.json")], check=True)
    checksum = hashlib.sha256(archive.read_bytes()).hexdigest()
    (release / "SHA256SUMS-linux.txt").write_text(f"{checksum}  {archive.name}\n")
    shutil.copy2(ROOT / "RELEASE_NOTES.md", release / "release-notes.md")
    print(f"Pacote auditado: {archive}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", default="v0.3.0")
    package(parser.parse_args().version)
