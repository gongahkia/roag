#!/usr/bin/env python3
"""Publish the native Linux x64 game, with a bundled .NET runtime and manifest.

Usage: python3 tools/package-linux.py [--output dist]
Builds from the current checkout; records its commit, dirty diff hash and SDK.
No Lua, generated reference runtime or sibling checkout enters the package.
"""
import argparse
import gzip
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tarfile
import tempfile

ROOT = Path(__file__).resolve().parents[1]

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=ROOT / 'dist')
    args = parser.parse_args()
    output = args.output.resolve(); output.mkdir(parents=True, exist_ok=True)
    commit = git('rev-parse', 'HEAD').decode().strip()
    epoch = int(git('show', '-s', '--format=%ct', 'HEAD'))
    archive = output / ('roag-linux-x64-' + commit[:8] + '.tar.gz')
    if archive.exists(): raise SystemExit('Preserving existing package: ' + str(archive))
    with tempfile.TemporaryDirectory(prefix='roag-publish-', dir=output) as temporary:
        package = Path(temporary) / 'roag-linux-x64'
        subprocess.run([str(ROOT / 'tools/dotnet.sh'), 'publish', str(ROOT / 'src/Roag.Client/Roag.Client.csproj'),
            '--configuration', 'Release', '--runtime', 'linux-x64', '--self-contained', 'true',
            '-p:PublishSingleFile=false', '-p:PublishTrimmed=false', '-p:DebugType=None', '-p:RestoreLockedMode=true',
            '--output', str(package)], cwd=ROOT, check=True)
        forbidden = [p for p in package.rglob('*') if p.suffix in ('.lua', '.pdb') or 'Roag.Reference' in p.name]
        if forbidden: raise RuntimeError('Unexpected reference/debug files: ' + str(forbidden))
        required = ['Roag.Client', 'libhostfxr.so', 'libcoreclr.so', 'libSDL2-2.0.so.0', 'libopenal.so',
            'Roag.Core.dll', 'assets/ithica-expedition-sprites.json', 'assets/fonts/ui-fonts.png']
        for name in required:
            if not (package / name).is_file(): raise RuntimeError('Missing package file: ' + name)
        launcher = package / 'roag'
        launcher.write_text('''#!/bin/sh
set -eu
roag_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
exec "$roag_dir/Roag.Client" "$@"
''')
        launcher.chmod(0o755)
        shutil.copy2(ROOT / 'docs/PLAY_LINUX.md', package / 'PLAY_LINUX.md')
        notices = package / 'licenses'; shutil.copytree(ROOT / 'assets/licenses', notices)
        # Read the runtime pack actually selected by restore; do not guess its
        # patch version or require a machine-specific SDK/cache location.
        assets = json.loads((ROOT / 'src/Roag.Client/obj/project.assets.json').read_text())
        runtime_pack = next((item for item in assets['project']['frameworks']['net10.0']['downloadDependencies']
            if item['name'].lower() == 'microsoft.netcore.app.runtime.linux-x64'), None)
        if not runtime_pack: raise RuntimeError('No resolved Linux runtime pack identity.')
        versions = [part.strip() for part in runtime_pack['version'].strip('[]').split(',')]
        if len(set(versions)) != 1: raise RuntimeError('The runtime pack needs an exact resolved version.')
        runtime_version = versions[0]
        pack_path = runtime_pack['name'].lower() + '/' + runtime_version
        pack_root = next((Path(folder) / pack_path for folder in assets['packageFolders'] if (Path(folder) / pack_path).exists()), None)
        if not pack_root: raise RuntimeError('Missing resolved .NET runtime pack.')
        for name in ['LICENSE.TXT', 'THIRD-PARTY-NOTICES.TXT']:
            shutil.copy2(pack_root / name, notices / ('dotnet-' + name))
        for source in ROOT.glob('LICENSE*'):
            if source.is_file(): shutil.copy2(source, package / source.name)
        source_paths = [ROOT / 'Directory.Build.props', ROOT / 'global.json']
        for project in ['Roag.Core', 'Roag.Graphics', 'Roag.Client']:
            source_paths.extend(p for p in (ROOT / 'src' / project).rglob('*') if p.is_file()
                and not {'bin', 'obj'}.intersection(p.relative_to(ROOT / 'src' / project).parts))
        metadata = dict(format=1, commit=commit, status=git('status', '--short').decode(),
            diffSha256=hashlib.sha256(git('diff', 'HEAD', '--binary')).hexdigest(),
            sdk=subprocess.check_output([str(ROOT / 'tools/dotnet.sh'), '--version'], text=True).strip(),
            runtime='linux-x64', runtimeVersion=runtime_version, selfContained=True, trimmed=False,
            sourceHashes={str(p.relative_to(ROOT)): digest(p) for p in sorted(source_paths)})
        (package / 'BUILD.json').write_text(json.dumps(metadata, indent=2) + '\n')
        hashes = [digest(p) + '  ' + str(p.relative_to(package)) for p in sorted(package.rglob('*')) if p.is_file()]
        (package / 'SHA256SUMS').write_text('\n'.join(hashes) + '\n')
        # Normalize archive ordering, ownership and mtimes. Identical published
        # bytes and build metadata produce identical archives, including gzip.
        with archive.open('wb') as raw, gzip.GzipFile(filename='', mode='wb', fileobj=raw, mtime=epoch) as zipped:
            with tarfile.open(fileobj=zipped, mode='w') as tar:
                for path in [package, *sorted(package.rglob('*'))]:
                    info = tar.gettarinfo(str(path), arcname=str(path.relative_to(package.parent)))
                    info.uid = info.gid = 0; info.uname = info.gname = ''; info.mtime = epoch
                    if path.is_file():
                        with path.open('rb') as stream: tar.addfile(info, stream)
                    else: tar.addfile(info)
    (archive.parent / (archive.name + '.sha256')).write_text(digest(archive) + '  ' + archive.name + '\n')
    print('Linux package:', archive)

if __name__ == '__main__': main()
