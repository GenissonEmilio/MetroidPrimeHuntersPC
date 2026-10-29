"""Install the extracted package for the current user; never copy game data."""
import argparse
from pathlib import Path
import os
import shlex
import shutil
import subprocess


def desktop_quote(value):
    return '"' + str(value).replace('\\', '\\\\').replace('"', '\\"').replace('`', '\\`').replace('$', '\\$').replace('%', '%%') + '"'


def install(source, prefix, data_home):
    destination = prefix / "opt" / "HuntersPC"
    if source.resolve() == destination.resolve():
        raise ValueError("Execute o instalador a partir de uma pasta extraida separada.")
    if not (source / "HuntersPC").is_file() or not (source / "package-manifest.json").is_file():
        raise ValueError("Extraia o pacote Linux completo antes de instalar.")
    # Copy only files listed in the public manifest. State is stored outside app/.
    import json
    manifest = json.loads((source / "package-manifest.json").read_text())
    if manifest.get("Platform") != "linux-x64":
        raise ValueError("Este instalador requer o pacote linux-x64.")
    names = [entry["Path"] for entry in manifest["Files"]] + ["package-manifest.json"]
    for name in names:
        relative = Path(name)
        if relative.is_absolute() or ".." in relative.parts or (source / relative).is_symlink():
            raise ValueError("Caminho invalido no manifesto.")
        target = destination / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source / relative, target)
    bindir = prefix / "bin"
    bindir.mkdir(parents=True, exist_ok=True)
    launcher = bindir / "hunters-pc"
    launcher.write_text("#!/bin/sh\nexec " + shlex.quote(str(destination / "HuntersPC.sh")) + ' "$@"\n')
    launcher.chmod(0o755)
    applications = data_home / "applications"
    applications.mkdir(parents=True, exist_ok=True)
    desktop = applications / "io.github.GenissonEmilio.HuntersPC.desktop"
    desktop.write_text(
        "[Desktop Entry]\nType=Application\nName=Metroid Prime Hunters PC\n"
        "Comment=Metroid Prime Hunters para Linux\n"
        f"Exec={desktop_quote(destination / 'HuntersPC.sh')}\n"
        f"Icon={destination / 'hunterspc.svg'}\nTerminal=false\nCategories=Game;ActionGame;\n"
        "StartupNotify=false\nStartupWMClass=HuntersPC\n"
    )
    updater = shutil.which("update-desktop-database")
    if updater:
        subprocess.run([updater, str(applications)], check=False)
    print(f"Instalado: {destination}\nMenu: Metroid Prime Hunters PC\nComando: {launcher}")
    print(f"Dados e saves: {data_home / 'hunterspc'}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--prefix", type=Path, default=Path.home() / ".local")
    args = parser.parse_args()
    xdg = os.environ.get("XDG_DATA_HOME", "")
    data_home = Path(xdg) if Path(xdg).is_absolute() else Path.home() / ".local" / "share"
    install(Path(__file__).resolve().parent, args.prefix.expanduser().resolve(), data_home)
