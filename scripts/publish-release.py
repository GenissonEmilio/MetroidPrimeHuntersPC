"""Publish audited CI packages together, without rebuilding or accessing game data."""
import importlib.util
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import tarfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def gh(*args):
    return subprocess.check_output(['gh', *args], text=True)


def unique(folder, pattern):
    matches = list(folder.rglob(pattern))
    if len(matches) != 1:
        raise ValueError(f'Expected exactly one {pattern}, found {len(matches)}')
    return matches[0]


def find_release(repo, version):
    # The tag endpoint only returns published releases. Drafts must be found
    # through the authenticated release list, then accessed by numeric ID.
    releases = json.loads(gh('api', f'repos/{repo}/releases?per_page=100'))
    return next((release for release in releases if release['tag_name'] == version), None)


def publish():
    version, commit, repo = (os.environ[key] for key in ('RELEASE_VERSION', 'RELEASE_COMMIT', 'GH_REPO'))
    if not re.fullmatch(r'v\d+\.\d+\.\d+', version) or not re.fullmatch(r'[0-9a-f]{40}', commit):
        raise ValueError('Invalid version or commit')
    if subprocess.check_output(['git', 'rev-parse', version + '^{}'], text=True).strip() != commit:
        raise ValueError('Release tag does not point to the package source commit')
    notes = subprocess.check_output(['git', 'show', commit + ':RELEASE_NOTES.md']).decode('utf-8-sig')
    stage = ROOT / 'local' / ('release-' + version)
    stage.mkdir(parents=True, exist_ok=True)
    assets = []
    audits = []
    for platform, env_key, workflow, suffix in [
        ('windows', 'WINDOWS_RUN', 'windows.yml', 'windows-x64.zip'),
        ('linux', 'LINUX_RUN', 'linux.yml', 'linux-x64.tar.gz')]:
        run = os.environ[env_key]
        if not run.isdigit():
            raise ValueError('Invalid build run ID')
        info = json.loads(gh('api', f'repos/{repo}/actions/runs/{run}'))
        if (info['conclusion'] != 'success' or info['head_sha'] != commit
                or info['path'] != '.github/workflows/' + workflow
                or info['repository']['full_name'] != repo):
            raise ValueError('Build identity or conclusion mismatch')
        folder = stage / platform
        gh('run', 'download', run, '--repo', repo, '--name', f'HuntersPC-{platform}-x64', '--dir', str(folder))
        archive = unique(folder, f'MetroidPrimeHuntersPC-{version}-{suffix}')
        spec = importlib.util.spec_from_file_location('audit_' + platform, ROOT / 'scripts' / f'audit-{platform}-package.py')
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        audit = module.audit(archive)
        if (not audit['Valid'] or audit['GameDataIncluded'] or audit['SourceCommit'] != commit or audit['Version'] != version):
            raise ValueError('Package source/version mismatch')
        sums = unique(folder, 'SHA256SUMS.txt' if platform == 'windows' else 'SHA256SUMS-linux.txt')
        if sums.read_text(encoding='utf-8-sig').split() != [audit['Sha256'], archive.name]:
            raise ValueError('CI checksum mismatch')
        if platform == 'windows':
            with zipfile.ZipFile(archive) as package:
                packaged_notes = package.read('HuntersPC/RELEASE_NOTES.md').decode('utf-8-sig')
        else:
            with tarfile.open(archive, 'r:gz') as package:
                manifest = json.load(package.extractfile('HuntersPC/package-manifest.json'))
                if manifest['SourceDirty']:
                    raise ValueError('Linux package was built from a dirty checkout')
                packaged_notes = package.extractfile('HuntersPC/RELEASE_NOTES.md').read().decode('utf-8-sig')
            smoke = json.loads(unique(folder, 'smoke.json').read_text())
            if not smoke['Success'] or 'Scene presentation/AA shader' not in smoke['Checks']:
                raise ValueError('Packaged Linux smoke/shader check missing')
        if packaged_notes.replace('\r\n', '\n') != notes.replace('\r\n', '\n'):
            raise ValueError('Packaged release notes differ from tagged source')
        audit_file = stage / f'package-audit-{platform}.json'
        audit_file.write_text(json.dumps(audit, indent=2) + '\n')
        assets.extend([archive, audit_file])
        audits.append(audit)
    sums = stage / 'SHA256SUMS.txt'
    sums.write_text(''.join(f"{a['Sha256']}  {a['Archive']}\n" for a in audits))
    assets.append(sums)
    notes_file = stage / 'release-notes.md'
    notes_file.write_text(notes)
    title = f'Metroid Prime Hunters PC {version} - abertura completa e graficos melhorados'
    release = find_release(repo, version)
    if release is None:
        gh('release', 'create', version, '--repo', repo, '--verify-tag', '--draft', '--title', title,
           '--notes-file', str(notes_file), *map(str, assets))
        release = find_release(repo, version)
    if release is None or not release['draft']:
        raise ValueError('Expected an unpublished draft; existing releases are preserved')
    actual = {a['name']: a for a in release['assets']}
    expected = {p.name for p in assets}
    if set(actual) - expected:
        raise ValueError('Draft release asset list mismatch')
    missing = [asset for asset in assets if asset.name not in actual]
    if missing:
        gh('release', 'upload', version, '--repo', repo, *map(str, missing))
        release = json.loads(gh('api', f"repos/{repo}/releases/{release['id']}"))
        actual = {a['name']: a for a in release['assets']}
    if set(actual) != expected:
        raise ValueError('Draft release is incomplete')
    for asset in assets:
        digest = hashlib.sha256(asset.read_bytes()).hexdigest()
        if actual[asset.name]['size'] != asset.stat().st_size or actual[asset.name].get('digest') != 'sha256:' + digest:
            raise ValueError('Uploaded asset digest mismatch: ' + asset.name)
    gh('api', '--method', 'PATCH', f"repos/{repo}/releases/{release['id']}", '-F', 'draft=false', '-f', 'make_latest=true')
    print(gh('release', 'view', version, '--repo', repo, '--json', 'url,isDraft,isPrerelease,assets'))


if __name__ == '__main__':
    publish()
