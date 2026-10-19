#!/usr/bin/env python3
"""Measure voxel lighting cost and validate low-memory graphical scenes."""
import argparse
import csv
import hashlib
import json
import os
from pathlib import Path
import shutil
import statistics
import subprocess
import sys
import time

sys.dont_write_bytecode = True
import build
import benchmark
from ppm_to_png import convert


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--skip-build', action='store_true')
    parser.add_argument('--graphics', action='store_true')
    parser.add_argument('--flight', action='store_true')
    parser.add_argument('--repeats', type=int, default=3)
    parser.add_argument('--seconds', type=int, default=8)
    args = parser.parse_args()
    if not 1 <= args.repeats <= 20 or args.seconds < 3:
        parser.error('Expected 1–20 repeats and at least three seconds')
    os.chdir(build.ROOT)
    if not args.skip_build:
        subprocess.run([sys.executable, 'scripts/build.py', 'build'], check=True)
    binary = build.ROOT / 'build' / ('voxel.exe' if os.name == 'nt' else 'voxel')
    env = build.environment()
    output = subprocess.check_output([str(binary), '--benchmark', 'lighting', '--frames', str(args.repeats)], env=env, text=True, timeout=180)
    rows = list(csv.DictReader(Path('benchmarks/lighting.csv').open()))
    samples, coordinates, digests = set(), {}, {}
    for r in rows:
        key = (r['fixture'], r['lit'], r['repeat'])
        assert key not in samples
        samples.add(key)
        assert coordinates.setdefault(r['fixture'], tuple(r[a] for a in ('x', 'y', 'z'))) == tuple(r[a] for a in ('x', 'y', 'z'))
        assert digests.setdefault((r['fixture'], r['lit']), r['digest']) == r['digest'], 'Nondeterministic lit mesh'
        assert int(r['peak_worker_bytes']) <= 16 * benchmark.MIB, 'Worker reservation exceeded'
    report = {'platform': sys.platform, 'binary_sha256': hashlib.sha256(binary.read_bytes()).hexdigest(),
              'toolchain': json.loads(Path('toolchain.json').read_text()), 'output': output.splitlines(), 'modes': {}, 'scenes': []}
    source_hash = hashlib.sha256()
    for path in sorted([p for p in Path('src').glob('*.odin') if not p.name.endswith('_test.odin')] + list(Path('shaders').glob('*'))):
        source_hash.update(str(path).encode() + b'\0' + path.read_bytes())
    report['source_sha256'] = source_hash.hexdigest()
    for lit in ('0', '1'):
        selected = [r for r in rows if r['lit'] == lit]
        assert len(selected) == len(coordinates) * args.repeats
        mode = {f'mean_{k}': statistics.mean(float(r[k]) for r in selected) for k in ('generation_ms', 'lighting_ms', 'meshing_ms', 'total_ms', 'vertices')}
        mode.update(fixtures=len(coordinates), repeats=args.repeats, peak_worker_mib=max(int(r['peak_worker_bytes']) for r in selected) / benchmark.MIB)
        report['modes']['lit' if lit == '1' else 'unlit'] = mode
    print(json.dumps(report['modes'], indent=2), flush=True)
    server = None
    try:
        if args.graphics or args.flight:
            if sys.platform.startswith('linux') and shutil.which('Xvfb'):
                number = next(n for n in range(91, 131) if not Path(f'/tmp/.X11-unix/X{n}').exists())
                server = subprocess.Popen(['Xvfb', f':{number}', '-screen', '0', '1024x768x24', '-nolisten', 'tcp'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                env['DISPLAY'] = f':{number}'
                env.pop('WAYLAND_DISPLAY', None)
                env['LIBGL_ALWAYS_SOFTWARE'] = '1'
                time.sleep(.4)
            scenes = [(s, lit) for s in ('light-room', 'forest', 'caves', 'fluids', 'edit', 'sky') for lit in (True, False)] if args.graphics else []
            if args.flight:
                scenes.append(('flight', True))
            for scene, lit in scenes:
                name = f'lighting-{scene}-{"lit" if lit else "unlit"}'
                ppm = build.ROOT / 'build' / f'{name}.ppm'
                command = [str(binary), '--hidden', '--no-hud', '--benchmark', scene, '--generator', '5', '--seconds', str(52 if scene == 'flight' else args.seconds),
                           '--radius', '2', '--gpu-mb', '16', '--cpu-mb', '32', '--upload-kb', '64', '--width', '640', '--height', '360', '--screenshot', str(ppm)]
                if not lit:
                    command.append('--no-lighting')
                result = subprocess.run(command, env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=(52 if scene == 'flight' else args.seconds) + 30)
                assert result.returncode == 0 and 'OpenGL error' not in result.stdout, result.stdout
                source = Path('benchmarks') / f'{scene}.csv'
                summary = benchmark.summarize(source, warmup=2)
                assert summary['generated_jobs'] > 16, summary
                assert summary['final_radius'] == 2 and summary['fluid_refused'] == 0, summary
                with source.open() as f:
                    frames = list(csv.DictReader(f))
                summary['lighting_total_ms'] = float(frames[-1]['lighting_total_ms'])
                assert (summary['lighting_total_ms'] > 0) == lit
                destination = Path('benchmarks') / f'{name}.csv'
                source.replace(destination)
                convert(ppm, ppm.with_suffix('.png'))
                if scene == 'light-room' and lit:
                    rgb = ppm.read_bytes().split(b'\n', 3)[3]
                    def box_mean(x, y, width, height, channel):
                        return statistics.mean(rgb[(j * 640 + i) * 3 + channel]
                                               for j in range(y, y + height)
                                               for i in range(x, x + width))
                    assert box_mean(140, 110, 50, 50, 0) > 1.3 * box_mean(140, 110, 50, 50, 2), 'Lamp failed to produce warm illumination'
                    assert box_mean(20, 260, 40, 40, 0) < 30, 'Enclosed corner was not dark'
                entry = {'scene': scene, 'lit': lit, 'command': command[1:], 'output': result.stdout.splitlines(), 'summary': summary, 'csv': str(destination), 'image': str(ppm.with_suffix('.png'))}
                destination.with_suffix('.json').write_text(json.dumps(entry, indent=2) + '\n')
                report['scenes'].append(entry)
                print(f'PASS {name}: graphics {summary["peak_graphics_reserved_mib"]:.2f} MiB, heap {summary["peak_cpu_mib"]:.2f} MiB', flush=True)
    finally:
        if server:
            server.terminate()
            server.wait(timeout=5)
    Path('benchmarks/lighting-report.json').write_text(json.dumps(report, indent=2) + '\n')


if __name__ == '__main__':
    main()
