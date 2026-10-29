import hashlib
import importlib.util
import io
import json
from pathlib import Path
import tarfile
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("linux_audit", ROOT / "scripts" / "audit-linux-package.py")
audit_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit_module)
spec = importlib.util.spec_from_file_location("linux_install", ROOT / "scripts" / "linux" / "install-linux.py")
install_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(install_module)


class LinuxPackageChecks(unittest.TestCase):
    def make_archive(self, folder, *, extra=None, missing=None, corrupt=False, executable=True, symlink=False, duplicate=False):
        files = {name: b"dependency" for name in audit_module.REQUIRED if not name.endswith("package-manifest.json") and name != missing}
        files["HuntersPC/HuntersPC"] = b"\x7fELF\x02\x01" + b"\x00" * 12 + b"\x3e\x00"
        if extra:
            files[extra] = b"private data"
        manifest = {"Version": "v0.2.0", "Platform": "linux-x64", "SourceCommit": "test", "GameDataIncluded": False, "Files": [
            {"Path": name.removeprefix("HuntersPC/"), "Bytes": len(data), "Sha256": hashlib.sha256(data).hexdigest()}
            for name, data in files.items()
        ]}
        if corrupt:
            manifest["Files"][0]["Sha256"] = "0" * 64
        files["HuntersPC/package-manifest.json"] = json.dumps(manifest).encode()
        archive = Path(folder) / "test.tar.gz"
        with tarfile.open(archive, "w:gz") as output:
            for name, data in files.items():
                entry = tarfile.TarInfo(name)
                entry.size = len(data)
                entry.mode = 0o755 if executable else 0o644
                output.addfile(entry, io.BytesIO(data))
            if symlink:
                entry = tarfile.TarInfo("HuntersPC/link")
                entry.type = tarfile.SYMTYPE
                entry.linkname = "/etc/passwd"
                output.addfile(entry)
            if duplicate:
                entry = tarfile.TarInfo("HuntersPC/LICENSE")
                output.addfile(entry)
        return archive

    def test_complete_package(self):
        with tempfile.TemporaryDirectory() as folder:
            self.assertTrue(audit_module.audit(self.make_archive(folder))["Valid"])

    def test_private_data_and_traversal_rejected(self):
        for name in ("HuntersPC/files/data.dat", "HuntersPC/Savedata/save001.json", "HuntersPC/paths.txt", "HuntersPC/game.nds",
                     "HuntersPC/pc-profile.json", "HuntersPC/evidence/test.png", "HuntersPC/../outside", "/absolute"):
            with self.subTest(name=name), tempfile.TemporaryDirectory() as folder:
                with self.assertRaises(ValueError):
                    audit_module.audit(self.make_archive(folder, extra=name))

    def test_runtime_required(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                audit_module.audit(self.make_archive(folder, missing="HuntersPC/libcoreclr.so"))

    def test_checksums_required(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                audit_module.audit(self.make_archive(folder, corrupt=True))

    def test_executable_modes_required(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                audit_module.audit(self.make_archive(folder, executable=False))

    def test_symlinks_and_duplicate_paths_rejected(self):
        for kwargs in ({"symlink": True}, {"duplicate": True}):
            with tempfile.TemporaryDirectory() as folder:
                with self.assertRaises(ValueError):
                    audit_module.audit(self.make_archive(folder, **kwargs))

    def test_install_and_update_preserve_user_state_and_quote_spaces(self):
        with tempfile.TemporaryDirectory(prefix="hunters install ") as folder:
            root = Path(folder)
            archive = self.make_archive(root)
            with tarfile.open(archive) as package:
                package.extractall(root, filter="data")
            data_home = root / "data"
            state = data_home / "hunterspc"
            state.mkdir(parents=True)
            save = state / "save.json"
            save.write_text("keep progress")
            source = root / "HuntersPC"
            (source / "pc-profile.json").write_text("must not be copied")
            for _ in range(2):
                install_module.install(source, root / "prefix with spaces", data_home)
                self.assertEqual(save.read_text(), "keep progress")
                self.assertFalse((root / "prefix with spaces/opt/HuntersPC/pc-profile.json").exists())
            desktop = (data_home / "applications/io.github.GenissonEmilio.HuntersPC.desktop").read_text()
            self.assertIn('Exec="', desktop)
            self.assertTrue((root / "prefix with spaces/bin/hunters-pc").stat().st_mode & 0o111)


if __name__ == "__main__":
    unittest.main()
