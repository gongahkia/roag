#!/usr/bin/env python3
"""Reuse a compatible Odin compiler or install the pinned release into .tools."""
import argparse
import hashlib
import io
import json
import os
from pathlib import Path
import platform
import re
import shutil
import subprocess
import sys
import tarfile
import tempfile
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[1]
LOCK = json.loads((ROOT / 'toolchain.json').read_text())
LLVM_VERSIONS = (22, 21, 20, 19, 18, 17)


def odin_version(binary):
    try:
        return subprocess.check_output(
            [str(binary), 'version'], text=True, stderr=subprocess.STDOUT,
        ).strip()
    except (OSError, subprocess.CalledProcessError):
        return None


def matches_pin(version):
    return version is not None and re.search(
        rf'(?<![0-9a-f]){LOCK["commit"][:7]}(?![0-9a-f])', version,
    ) is not None


def find_odin():
    override = os.environ.get('ODIN_BIN')
    if override:
        version = odin_version(override)
        if not matches_pin(version):
            raise SystemExit(
                f'ODIN_BIN must select Odin {LOCK["release"]} ({LOCK["commit"][:7]}); '
                f'got {version or "a missing or unusable compiler"}',
            )
        return override
    name = 'odin.exe' if platform.system() == 'Windows' else 'odin'
    candidates = [ROOT / '.tools' / 'odin' / name]
    candidates += sorted((ROOT / '.tools').glob(f'odin-*/{name}'))
    installed = shutil.which('odin')
    if installed:
        candidates.append(Path(installed))
    for path in candidates:
        if path.is_file() and matches_pin(odin_version(path)):
            return str(path)
    return None


def llvm_config():
    override = os.environ.get('LLVM_CONFIG')
    if override:
        return override
    candidates = []
    for name in ('llvm-config', *(f'llvm-config-{v}' for v in LLVM_VERSIONS)):
        found = shutil.which(name)
        if found:
            candidates.append(Path(found))
    brew = shutil.which('brew')
    if brew:
        prefix = Path(subprocess.check_output([brew, '--prefix'], text=True).strip())
        candidates += [prefix / 'opt' / f'llvm@{v}' / 'bin' / 'llvm-config' for v in LLVM_VERSIONS]
        candidates.append(prefix / 'opt' / 'llvm' / 'bin' / 'llvm-config')
    for path in candidates:
        if path.is_file():
            version = subprocess.check_output([str(path), '--version'], text=True).strip()
            if int(version.split('.')[0]) in LLVM_VERSIONS:
                return str(path)
    if not brew:
        raise SystemExit('Intel macOS needs LLVM 17–22. Install LLVM and set LLVM_CONFIG, or install Homebrew.')
    print('Installing LLVM 22 with Homebrew for the Intel macOS source build.', flush=True)
    subprocess.run([brew, 'install', 'llvm@22'], check=True)
    prefix = subprocess.check_output([brew, '--prefix', 'llvm@22'], text=True).strip()
    return str(Path(prefix) / 'bin' / 'llvm-config')


def build_macos_intel(target):
    try:
        subprocess.run(
            ['xcrun', '--sdk', 'macosx', '--show-sdk-path'],
            stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, check=True,
        )
    except (OSError, subprocess.CalledProcessError):
        raise SystemExit('Install the Xcode command-line tools first: xcode-select --install')
    env = os.environ.copy()
    env['LLVM_CONFIG'] = llvm_config()
    # This project uses GLFW, whose library is stored in regular Git. Skip
    # unrelated LFS assets, including when Git LFS is not installed.
    env['GIT_LFS_SKIP_SMUDGE'] = '1'
    print(f'Building Odin {LOCK["release"]} from source for Intel macOS. This takes a few minutes.', flush=True)
    subprocess.run([
        'git', '-c', 'filter.lfs.process=', '-c', 'filter.lfs.smudge=',
        '-c', 'filter.lfs.required=false', 'clone', '--depth', '1',
        '--branch', LOCK['release'], 'https://github.com/odin-lang/Odin.git', str(target),
    ], env=env, check=True)
    commit = subprocess.check_output(['git', '-C', str(target), 'rev-parse', 'HEAD'], text=True).strip()
    if commit != LOCK['commit']:
        raise SystemExit(f'Odin source commit mismatch: expected {LOCK["commit"]}, got {commit}')
    subprocess.run(['make', 'release-native'], cwd=target, env=env, check=True)


def download(url, digest):
    print(f'Downloading {url}', flush=True)
    with urllib.request.urlopen(url, timeout=120) as response:
        data = response.read()
    if hashlib.sha256(data).hexdigest() != digest:
        raise SystemExit('SHA-256 mismatch; download was not installed')
    return data


def install_odin():
    installed = find_odin()
    if installed:
        return installed
    system = {'Linux': 'linux', 'Darwin': 'macos', 'Windows': 'windows'}.get(platform.system())
    machine = {'x86_64': 'amd64', 'amd64': 'amd64', 'aarch64': 'arm64', 'arm64': 'arm64'}.get(platform.machine().lower())
    key = f'{system}-{machine}'
    source_build = key == 'macos-amd64'
    if key not in LOCK['assets'] and not source_build:
        raise SystemExit(f'No pinned Odin toolchain for {key}; see README.md for supported platforms.')
    tools = ROOT / '.tools'
    tools.mkdir(exist_ok=True)
    target = tools / f'odin-{LOCK["release"]}'
    name = 'odin.exe' if system == 'windows' else 'odin'
    print(f'Odin {LOCK["release"]} ({LOCK["commit"][:7]}) is missing; setting it up.', flush=True)
    with tempfile.TemporaryDirectory(prefix='.odin-install-', dir=tools) as temporary:
        temporary = Path(temporary)
        if source_build:
            staged = temporary / 'Odin'
            build_macos_intel(staged)
        else:
            asset = LOCK['assets'][key]
            data = download(f'https://github.com/odin-lang/Odin/releases/download/{LOCK["release"]}/{asset["file"]}', asset['sha256'])
            if asset['file'].endswith('.zip'):
                with zipfile.ZipFile(io.BytesIO(data)) as archive:
                    archive.extractall(temporary)
            else:
                with tarfile.open(fileobj=io.BytesIO(data), mode='r:gz') as archive:
                    archive.extractall(temporary, filter='data')
            found = [p for p in temporary.rglob(name) if p.is_file()]
            if len(found) != 1:
                raise SystemExit('Unexpected Odin archive structure')
            staged = found[0].parent
        binary = staged / name
        if system != 'windows':
            binary.chmod(binary.stat().st_mode | 0o111)
        version = odin_version(binary)
        if not matches_pin(version):
            raise SystemExit(f'Downloaded/built Odin does not match the pinned commit: {version}')
        # Replace only our versioned installation, after validating its replacement.
        previous = temporary / 'previous'
        if target.exists() or target.is_symlink():
            target.rename(previous)
        try:
            shutil.move(str(staged), target)
        except OSError:
            if previous.exists() or previous.is_symlink():
                previous.rename(target)
            raise
    print(f'Installed Odin in {target}', flush=True)
    return str(target / name)


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
    try:
        compiler = install_odin()
        print(f'Using {compiler}', flush=True)
        if args.local_glfw:
            (ROOT / '.tools').mkdir(exist_ok=True)
            install_local_glfw()
    except subprocess.CalledProcessError as error:
        sys.exit(error.returncode)
