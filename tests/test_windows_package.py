import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import zipfile

SCRIPT = Path(__file__).resolve().parents[1] / "scripts" / "audit-windows-package.py"
spec = importlib.util.spec_from_file_location("package_audit", SCRIPT)
audit_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit_module)


class WindowsPackageChecks(unittest.TestCase):
    def make_archive(self, folder, *, extra=None, missing=None, corrupt=False):
        files = {name: b"test dependency" for name in audit_module.REQUIRED if not name.endswith("package-manifest.json") and name != missing}
        files["HuntersPC/HuntersPC.exe"] = b"MZtest launcher"
        if extra:
            files[extra] = b"must not be distributed"
        manifest = {"Version": "v0.1.0-alpha.1", "SourceCommit": "test", "Files": [
            {"Path": name.removeprefix("HuntersPC/"), "Bytes": len(data), "Sha256": hashlib.sha256(data).hexdigest()}
            for name, data in files.items()
        ]}
        if corrupt:
            manifest["Files"][0]["Sha256"] = "0" * 64
        archive = Path(folder) / "test.zip"
        with zipfile.ZipFile(archive, "w") as output:
            for name, data in files.items():
                output.writestr(name, data)
            output.writestr("HuntersPC/package-manifest.json", json.dumps(manifest))
        return archive

    def test_clean_package_and_manifest(self):
        with tempfile.TemporaryDirectory() as folder:
            self.assertTrue(audit_module.audit(self.make_archive(folder))["Valid"])

    def test_local_game_files_rejected(self):
        for name in ["HuntersPC/files/AMHE0/data.dat", "HuntersPC/Savedata/save001.json", "HuntersPC/paths.txt", "HuntersPC/pc-profile.json", "HuntersPC/game.nds", "HuntersPC/evidence/capture.png", "HuntersPC/../outside.dll"]:
            with self.subTest(name=name), tempfile.TemporaryDirectory() as folder:
                with self.assertRaises(ValueError):
                    audit_module.audit(self.make_archive(folder, extra=name))

    def test_missing_runtime_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                audit_module.audit(self.make_archive(folder, missing="HuntersPC/coreclr.dll"))

    def test_tampered_checksum_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                audit_module.audit(self.make_archive(folder, corrupt=True))


if __name__ == "__main__":
    unittest.main()
