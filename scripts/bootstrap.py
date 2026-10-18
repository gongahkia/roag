#!/usr/bin/env python3
"""Install the pinned Odin release into .tools; no global installations."""
import argparse
import hashlib
import io
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import tarfile
import tempfile
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[1]
LOCK = json.loads((ROOT / 'toolchain.json').read_text())


def download(url, digest):
    print(f'Downloading {url}', flush=True)
    with urllib.request.urlopen(url, timeout=120) as response:
        data = response.read()
    if hashlib.sha256(data).hexdigest() != digest:
        raise SystemExit('SHA-256 mismatch; download was not installed')
    return data


def install_odin():
    system = {'Linux': 'linux', 'Darwin': 'macos', 'Windows': 'windows'}.get(platform.system())
    machine = {'x86_64': 'amd64', 'AMD64': 'amd64', 'aarch64': 'arm64', 'arm64': 'arm64'}.get(platform.machine())
    key = f'{system}-{machine}'
    if key not in LOCK['assets']:
        raise SystemExit(f'No pinned release binary for {key}. Build Odin commit {LOCK["commit"]} from source; see README.')
    target = ROOT / '.tools' / 'odin'
    binary = target / ('odin.exe' if system == 'windows' else 'odin')
    if binary.exists():
        subprocess.run([str(binary), 'version'], check=True)
        return
    asset = LOCK['assets'][key]
    data = download(f'https://github.com/odin-lang/Odin/releases/download/{LOCK["release"]}/{asset["file"]}', asset['sha256'])
    with tempfile.TemporaryDirectory(dir=ROOT / '.tools') as temporary:
        temporary = Path(temporary)
        if asset['file'].endswith('.zip'):
            with zipfile.ZipFile(io.BytesIO(data)) as archive:
                archive.extractall(temporary)
        else:
            with tarfile.open(fileobj=io.BytesIO(data), mode='r:gz') as archive:
                archive.extractall(temporary, filter='data')
        found = [p for p in temporary.rglob(binary.name) if p.is_file()]
        if len(found) != 1:
            raise SystemExit('Unexpected Odin archive structure')
        shutil.move(str(found[0].parent), target)
    if system != 'windows':
        binary.chmod(binary.stat().st_mode | 0o111)
    subprocess.run([str(binary), 'version'], check=True)


def install_local_glfw():
    if platform.system() != 'Linux' or platform.machine() not in ('x86_64', 'AMD64'):
        raise SystemExit('--local-glfw supports Linux x86_64 only; use your system GLFW package elsewhere')
    dep = LOCK['linux_local_glfw']
    data = download(dep['url'], dep['sha256'])
    # Read the ar container directly; this needs no package manager or root.
    if data[:8] != b'!<arch>\n':
        raise SystemExit('Invalid Debian archive')
    offset = 8
    while offset + 60 <= len(data):
        header = data[offset:offset + 60]
        name = header[:16].decode().strip().rstrip('/')
        size = int(header[48:58])
        body = data[offset + 60:offset + 60 + size]
        if name.startswith('data.tar'):
            with tarfile.open(fileobj=io.BytesIO(body), mode='r:*') as archive:
                member = next(m for m in archive.getmembers() if m.name.endswith('/libglfw.so.3.3'))
                content = archive.extractfile(member).read()
            target = ROOT / '.tools' / 'lib'
            target.mkdir(exist_ok=True)
            (target / 'libglfw.so.3.3').write_bytes(content)
            for name in ('libglfw.so', 'libglfw.so.3'):
                link = target / name
                if link.is_symlink() or link.exists():
                    link.unlink()
                link.symlink_to('libglfw.so.3.3')
            print(f'Installed local GLFW to {target}')
            return
        offset += 60 + size + (size % 2)
    raise SystemExit('No library payload in Debian archive')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--local-glfw', action='store_true', help='Also install a pinned local Linux x86_64 GLFW binary')
    args = parser.parse_args()
    (ROOT / '.tools').mkdir(exist_ok=True)
    install_odin()
    if args.local_glfw:
        install_local_glfw()
