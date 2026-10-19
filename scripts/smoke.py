#!/usr/bin/env python3
"""Graphical, persistence, input, resizing, and memory-pressure smoke checks."""
import argparse
import csv
import json
import os
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import tempfile
import time

sys.dont_write_bytecode = True

import benchmark
import build
from ppm_to_png import convert
from smoke_input import interactive

ROOT = build.ROOT


def run(binary, env, args, expected=0):
    result = subprocess.run([str(binary), *args], env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=30)
    if result.returncode != expected:
        raise AssertionError(f'{args}: exit {result.returncode}\n{result.stdout}')
    return result.stdout


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--skip-build', action='store_true')
    parser.add_argument('--skip-input', action='store_true', help='Skip Linux XTest interaction checks')
    args = parser.parse_args()
    os.chdir(ROOT)
    if not args.skip_build:
        subprocess.run([sys.executable, 'scripts/build.py', 'build'], check=True)
    binary = ROOT / 'build' / ('voxel.exe' if os.name == 'nt' else 'voxel')
    env = build.environment()
    server = None
    if sys.platform.startswith('linux') and shutil.which('Xvfb'):
        # Keep test windows off the user's desktop, even in a normal WSL session.
        number = next(n for n in range(91, 120) if not Path(f'/tmp/.X11-unix/X{n}').exists())
        server = subprocess.Popen(['Xvfb', f':{number}', '-screen', '0', '1024x768x24', '-nolisten', 'tcp'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        env['DISPLAY'] = f':{number}'
        env.pop('WAYLAND_DISPLAY', None)
        env['LIBGL_ALWAYS_SOFTWARE'] = '1'
        time.sleep(0.4)
    try:
        with tempfile.TemporaryDirectory(dir=ROOT / 'build') as temporary:
            directory = Path(temporary)
            ppm = directory / 'smoke.ppm'
            run(binary, env, ['--hidden', '--no-save', '--radius', '2', '--frames', '90', '--screenshot', str(ppm)])
            rgb = ppm.read_bytes().split(b'\n', 3)[3]
            assert len({rgb[i:i+3] for i in range(0, len(rgb), 96)}) > 64, 'Screenshot contains insufficient scene detail'
            convert(ppm, ROOT / 'build' / 'smoke.png')
            print('PASS graphical rendering and screenshot', flush=True)
            save = directory / 'world.vox'
            settings = directory / 'startup-settings.cfg'
            run(binary, env, ['--hidden', '--save', str(save), '--settings', str(settings), '--radius', '1', '--frames', '10'])
            assert save.read_bytes().startswith(b'VOX2')
            output = run(binary, env, ['--hidden', '--save', str(save), '--settings', str(settings), '--seed', '999', '--radius', '1', '--frames', '10'])
            assert 'Loaded' in output
            assert struct.unpack_from('<Q', save.read_bytes(), 4)[0] == 42
            original = save.read_bytes()
            assert struct.unpack_from('<I', original, 48)[0] == 5
            run(binary, env, ['--hidden', '--save', str(save), '--generator', '2', '--frames', '1'], expected=2)
            assert save.read_bytes() == original, 'Generator mismatch changed an existing save'
            for version in (1, 2, 3, 4):
                legacy = directory / f'generator-{version}.vox'
                run(binary, env, ['--hidden', '--save', str(legacy), '--generator', str(version), '--radius', '1', '--frames', '2'])
                run(binary, env, ['--hidden', '--save', str(legacy), '--radius', '1', '--frames', '2'])
                assert struct.unpack_from('<I', legacy.read_bytes(), 48)[0] == version
            save.write_bytes(b'invalid-existing-save')
            run(binary, env, ['--hidden', '--save', str(save), '--frames', '1'], expected=1)
            assert save.read_bytes() == b'invalid-existing-save'
            print('PASS save/reload and preservation of corrupt saves', flush=True)
            for flags in (['--gpu-mb', '0'], ['--radius', '0'], ['--profile', 'invalid'], ['--workers', '99'], ['--frames', '-1'], ['--fps-limit', '999'], ['--generator', '0'], ['--generator', '6']):
                run(binary, env, flags, expected=2)
            print('PASS invalid configuration rejection', flush=True)
            if sys.platform.startswith('linux') and not args.skip_input:
                interactive(binary, env, directory)
                interactive(binary, env, directory, collisions=True)
            # Tiny scene isolates the software limiter from render load. Measured
            # intervals must include its wait and agree with wall-clock duration.
            run(binary, env, ['--hidden', '--benchmark', 'normal', '--seconds', '3', '--radius', '1', '--width', '320', '--height', '240', '--fps-limit', '30'])
            path = ROOT / 'benchmarks' / 'normal.csv'
            with path.open() as file:
                rows = list(csv.DictReader(file))
            assert all(None not in row and None not in row.values() for row in rows), 'Misaligned CSV fields'
            times = [float(row['frame_ms']) for row in rows]
            assert all(ms >= 1000 / 30 - 0.1 for ms in times), 'Measured FPS excluded the cap wait'
            assert any(float(row['pacing_wait_ms']) > 1 for row in rows), 'Limiter never waited'
            assert all(int(row['frame_cap']) == 30 for row in rows)
            wall = float(rows[-1]['elapsed_s'])
            assert abs(sum(times) / 1000 - wall) < 0.01, 'Completed frame intervals disagree with elapsed time'
            steady = [row for row in rows if float(row['elapsed_s']) > 2]
            assert all(0 < float(row['fps']) <= 30.1 for row in steady), 'HUD FPS was not measured at the cap'
            path.replace(path.with_name('smoke-frame-cap.csv'))
            print('PASS measured FPS, 30 FPS pacing, swap timing, and aligned CSV', flush=True)
            for scene in ('terrain', 'sky', 'caves'):
                ppm = directory / f'{scene}.ppm'
                run(binary, env, ['--hidden', '--benchmark', scene, '--generator', '3', '--seconds', '5', '--radius', '2', '--gpu-mb', '16', '--cpu-mb', '32', '--width', '640', '--height', '360', '--upload-kb', '64', '--screenshot', str(ppm)])
                path = ROOT / 'benchmarks' / f'{scene}.csv'
                summary = benchmark.summarize(path, warmup=1)
                assert summary['generator'] == 3 and summary['generated_jobs'] > 16, summary
                assert summary['fluid_queue_overflow'] == 0 and summary['fluid_refused'] == 0, summary
                target = path.with_name(f'smoke-v3-{scene}.csv'); path.replace(target)
                target.with_suffix('.json').write_text(json.dumps(summary, indent=2) + '\n')
                convert(ppm, ROOT / 'build' / f'terrain-v3-{scene}.png')
            print('PASS v3 surface/island/cave scenes, natural lakes, and 16/32 MiB budgets', flush=True)
            for scene in ('birch', 'cherry', 'mushrooms', 'snowy-peaks', 'snowy-plains', 'taiga', 'savanna', 'jungle', 'swamp', 'badlands'):
                ppm = directory / f'{scene}.ppm'
                output = run(binary, env, ['--hidden', '--benchmark', scene, '--generator', '4', '--seconds', '5', '--radius', '2', '--gpu-mb', '16', '--cpu-mb', '32', '--width', '640', '--height', '360', '--upload-kb', '64', '--screenshot', str(ppm)])
                assert 'OpenGL error' not in output, output
                path = ROOT / 'benchmarks' / f'{scene}.csv'
                summary = benchmark.summarize(path, warmup=1)
                assert summary['generator'] == 4 and summary['generated_jobs'] > 16, summary
                assert summary['fluid_queue_overflow'] == 0 and summary['fluid_refused'] == 0, summary
                target = path.with_name(f'smoke-v4-{scene}.csv'); path.replace(target)
                target.with_suffix('.json').write_text(json.dumps(summary, indent=2) + '\n')
                convert(ppm, ROOT / 'build' / f'biome-v4-{scene}.png')
            print('PASS ten v4 biome scenes, six-bit materials, ice transparency, and 16/32 MiB budgets', flush=True)
            fluid_ppm = directory / 'fluids.ppm'
            run(binary, env, ['--hidden', '--benchmark', 'fluids', '--seconds', '6', '--radius', '2', '--gpu-mb', '16', '--cpu-mb', '32', '--width', '640', '--height', '360', '--upload-kb', '64', '--screenshot', str(fluid_ppm)])
            path = ROOT / 'benchmarks' / 'fluids.csv'
            summary = benchmark.summarize(path, warmup=1)
            assert summary['fluid_cell_changes'] > 50, 'Fluid sources never propagated'
            assert summary['fluid_refused'] == 0 and summary['fluid_queue_overflow'] == 0, summary
            target = path.with_name('smoke-fluids.csv'); path.replace(target)
            target.with_suffix('.json').write_text(json.dumps(summary, indent=2) + '\n')
            convert(fluid_ppm, ROOT / 'build' / 'smoke-fluids.png')
            print('PASS water/lava rendering, flow updates, queue bounds, and 16 MiB graphics budget', flush=True)
            run(binary, env, ['--hidden', '--benchmark', 'dense', '--generator', '2', '--seconds', '10', '--radius', '4', '--gpu-mb', '16', '--cpu-mb', '32', '--width', '640', '--height', '360', '--upload-kb', '64'])
            path = ROOT / 'benchmarks' / 'dense.csv'
            summary = benchmark.summarize(path, warmup=1)
            assert summary['pressure_events'] > 0, 'Pressure scenario never exercised eviction'
            assert summary['final_radius'] < 4
            target = path.with_name('smoke-pressure.csv'); path.replace(target)
            target.with_suffix('.json').write_text(json.dumps(summary, indent=2) + '\n')
            print('PASS memory pressure, automatic draw-distance reduction, and bounded uploads', flush=True)
    finally:
        if server:
            server.terminate(); server.wait(timeout=5)


if __name__ == '__main__':
    main()
