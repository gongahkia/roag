#!/usr/bin/env python3
"""Build, test, check, or run the sandbox using the pinned Odin toolchain."""
import argparse
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys

import bootstrap

ROOT = Path(__file__).resolve().parents[1]


def environment():
    env = os.environ.copy()
    local_lib = ROOT / '.tools' / 'lib'
    env['LD_LIBRARY_PATH'] = str(local_lib) + os.pathsep + env.get('LD_LIBRARY_PATH', '')
    # Odin only invokes clang to link its already compiled native object on Linux.
    # A cc forwarding wrapper is sufficient when clang is absent.
    if platform.system() == 'Linux' and not shutil.which('clang'):
        if not shutil.which('cc'):
            raise SystemExit('Install clang or a system C compiler (cc).')
        fallback = ROOT / '.tools' / 'linker'
        fallback.mkdir(parents=True, exist_ok=True)
        wrapper = fallback / 'clang'
        wrapper.write_text('#!/bin/sh\nexec cc "$@"\n')
        wrapper.chmod(0o755)
        env['PATH'] = str(fallback) + os.pathsep + env['PATH']
    return env


def odin():
    return bootstrap.install_odin()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('build', 'test', 'check', 'run'), nargs='?', default='build')
    parser.add_argument('--debug', action='store_true', help='Keep debug symbols and use modest optimization')
    args, rest = parser.parse_known_args()
    if rest[:1] == ['--']:
        rest = rest[1:]
    os.chdir(ROOT)
    (ROOT / 'build').mkdir(exist_ok=True)
    binary = ROOT / 'build' / ('voxel.exe' if os.name == 'nt' else 'voxel')
    compiler = odin()
    env = environment()
    command = 'build' if args.action == 'run' else args.action
    flags = ['-vet-unused-imports', '-vet-unused-variables']
    if args.action != 'check':
        output = ROOT / 'build' / ('tests.exe' if os.name == 'nt' else 'tests') if args.action == 'test' else binary
        flags += [f'-out:{output}', '-o:minimal' if args.debug else '-o:speed']
        if args.debug:
            flags += ['-debug']
        if platform.system() == 'Linux':
            flags += [f'-extra-linker-flags:-L{ROOT / ".tools" / "lib"}']
    subprocess.run([compiler, command, 'src', *flags], env=env, check=True)
    if args.action == 'run':
        subprocess.run([str(binary), *rest], env=env, check=True)
    elif rest:
        raise SystemExit(f'Unrecognized arguments: {rest}')


if __name__ == '__main__':
    try:
        main()
    except subprocess.CalledProcessError as error:
        sys.exit(error.returncode)
