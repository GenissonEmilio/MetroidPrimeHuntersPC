"""Add Linux launchers, documentation and exact dependency license notices."""
import json
import os
from pathlib import Path
import shutil
import sys
import urllib.request

ROOT = Path(__file__).resolve().parents[1]


def prepare(output):
    output = output.resolve()
    for name in ("LICENSE", "README-PC.md", "README-LINUX.md", "README-UPSTREAM.md", "THIRD-PARTY-PC.md", "RELEASE_NOTES.md"):
        shutil.copy2(ROOT / name, output / name)
    for path in (ROOT / "scripts" / "linux").iterdir():
        if not path.is_file():
            continue
        shutil.copy2(path, output / path.name)
        if path.suffix == ".sh":
            (output / path.name).chmod(0o755)
    cache = Path(os.environ.get("NUGET_PACKAGES", Path.home() / ".nuget" / "packages"))
    libraries = json.loads((output / "HuntersPC.deps.json").read_text())["libraries"]
    for spec, info in libraries.items():
        if info["type"] not in ("package", "runtimepack"):
            continue
        package = cache / spec.lower().removeprefix("runtimepack.")
        if not package.is_dir():
            raise FileNotFoundError(f"Pacote NuGet ausente para copiar licencas: {package}")
        destination = output / "licenses" / "nuget" / package.parent.name
        destination.mkdir(parents=True, exist_ok=True)
        for notice in package.iterdir():
            if notice.is_file() and ("license" in notice.name.lower() or "notice" in notice.name.lower() or notice.suffix == ".nuspec"):
                shutil.copy2(notice, destination / notice.name)
    for name, url in {
        "opentk": "https://raw.githubusercontent.com/opentk/opentk/4.9.4/LICENSE.md",
        "glfw": "https://raw.githubusercontent.com/glfw/glfw/3.4/LICENSE.md",
    }.items():
        destination = output / "licenses" / name
        destination.mkdir(parents=True, exist_ok=True)
        with urllib.request.urlopen(url, timeout=30) as response:
            (destination / "LICENSE.md").write_bytes(response.read())


if __name__ == "__main__":
    prepare(Path(sys.argv[1]))
