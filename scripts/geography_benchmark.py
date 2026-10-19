#!/usr/bin/env python3
"""Compare v4/v5 generation at identical coordinates; optionally validate graphics."""
import argparse
import csv
import hashlib
import json
import os
from pathlib import Path
import platform
import shutil
import statistics
import subprocess
import sys
import time

sys.dont_write_bytecode = True
import build
import benchmark
from ppm_to_png import convert

SCENES = ('ocean', 'coast', 'plains', 'forest', 'desert', 'terrain', 'sky',
          'snowy-plains', 'taiga', 'savanna', 'jungle', 'swamp', 'badlands',
          'birch', 'cherry', 'mushrooms', 'snowy-peaks', 'river', 'lake', 'caves')


def generation_summary(path, max_ratio):
    with path.open() as file:
        rows = list(csv.DictReader(file))
    assert rows and {int(r['generator']) for r in rows} == {4, 5}
    digests, coordinates, samples = {}, {}, set()
    for r in rows:
        fixture, version = int(r['fixture']), int(r['generator'])
        sample = (fixture, version, int(r['repeat']))
        assert sample not in samples, 'Duplicate benchmark sample'
        samples.add(sample)
        position = tuple(int(r[a]) for a in ('x', 'y', 'z'))
        assert coordinates.setdefault(fixture, position) == position, 'Unmatched coordinates'
        assert digests.setdefault((fixture, version), r['digest']) == r['digest'], 'Nondeterministic terrain'
        assert int(r['worker_heap_bytes']) <= 16 * benchmark.MIB, 'Worker heap exceeds reservation'
    versions = {}
    for version in (4, 5):
        selected = [r for r in rows if int(r['generator']) == version]
        repeats = sorted({int(r['repeat']) for r in selected})
        assert len(selected) == len(coordinates) * len(repeats), 'Incomplete benchmark samples'
        totals = [sum(float(r['generation_ms']) for r in selected if int(r['repeat']) == repeat) for repeat in repeats]
        versions[str(version)] = {
            'fixtures': len(coordinates), 'repeats': len(repeats),
            'median_generation_total_ms': statistics.median(totals),
            'mean_generation_ms': statistics.mean(float(r['generation_ms']) for r in selected),
            'mean_meshing_ms': statistics.mean(float(r['meshing_ms']) for r in selected),
            'peak_worker_heap_mib': max(int(r['worker_heap_bytes']) for r in selected) / benchmark.MIB,
            'mean_vertices': statistics.mean(int(r['vertices']) for r in selected),
        }
    ratio = versions['5']['median_generation_total_ms'] / versions['4']['median_generation_total_ms']
    assert ratio <= max_ratio, f'Generation regression: {ratio:.2f}x exceeds {max_ratio:.2f}x'
    return {'versions': versions, 'generation_ratio_v5_over_v4': ratio,
            'maximum_generation_ratio': max_ratio, 'budgets_passed': True}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--skip-build', action='store_true')
    parser.add_argument('--graphics', action='store_true')
    parser.add_argument('--scenes', nargs='+', choices=SCENES, default=SCENES)
    parser.add_argument('--wide', action='store_true', help='960x540 radius-four previews with 64 MiB allowances')
    parser.add_argument('--flight', action='store_true', help='Also validate the 52-second repeated route')
    parser.add_argument('--repeats', type=int, default=3)
    parser.add_argument('--seconds', type=int, default=6)
    parser.add_argument('--max-generation-ratio', type=float, default=1.5)
    args = parser.parse_args()
    if not 1 <= args.repeats <= 20 or args.seconds < 3 or args.max_generation_ratio <= 0:
        parser.error('Invalid benchmark duration, repeats, or regression threshold')
    os.chdir(build.ROOT)
    if not args.skip_build:
        subprocess.run([sys.executable, 'scripts/build.py', 'build'], check=True)
    binary = build.ROOT / 'build' / ('voxel.exe' if os.name == 'nt' else 'voxel')
    env = build.environment()
    command = [str(binary), '--benchmark', 'generation', '--frames', str(args.repeats)]
    output = subprocess.check_output(command, env=env, text=True, timeout=180)
    source_hash = hashlib.sha256()
    for path in sorted([p for p in Path('src').glob('*.odin') if not p.name.endswith('_test.odin')] + list(Path('shaders').glob('*'))):
        if path.is_file():
            source_hash.update(str(path).encode() + b'\0' + path.read_bytes())
    report = {'source_sha256': source_hash.hexdigest(), 'binary_sha256': hashlib.sha256(binary.read_bytes()).hexdigest(), 'platform': platform.platform(), 'toolchain': json.loads(Path('toolchain.json').read_text()),
              'generation_command': command[1:], 'generation_output': output.splitlines(),
              'generation': generation_summary(Path('benchmarks/generation.csv'), args.max_generation_ratio),
              'scenes': []}
    matrix = Path('benchmarks') / f'generation-v5-{"wide" if args.wide else "stress"}.csv'
    matrix.write_bytes(Path('benchmarks/generation.csv').read_bytes())
    report['generation_csv'] = str(matrix)
    print(json.dumps(report['generation'], indent=2), flush=True)
    server = None
    try:
        if args.graphics or args.flight:
            if sys.platform.startswith('linux') and shutil.which('Xvfb'):
                number = next(n for n in range(91, 131) if not Path(f'/tmp/.X11-unix/X{n}').exists())
                server = subprocess.Popen(['Xvfb', f':{number}', '-screen', '0', '1024x768x24', '-nolisten', 'tcp'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                env['DISPLAY'] = f':{number}'; env.pop('WAYLAND_DISPLAY', None)
                env['LIBGL_ALWAYS_SOFTWARE'] = '1'
                time.sleep(.4)
            scenes = list(args.scenes) if args.graphics else []
            if args.flight:
                scenes.append('flight')
            for scene in scenes:
                ppm = build.ROOT / 'build' / f'geography-v5-{"wide" if args.wide else "stress"}-{scene}.ppm'
                command = [str(binary), '--hidden', '--no-hud', '--benchmark', scene, '--generator', '5',
                           '--seconds', str(52 if scene == 'flight' else args.seconds),
                           '--radius', '4' if args.wide else '2', '--gpu-mb', '64' if args.wide else '16',
                           '--cpu-mb', '64' if args.wide else '32', '--upload-kb', '64',
                           '--width', '960' if args.wide else '640', '--height', '540' if args.wide else '360',
                           '--screenshot', str(ppm)]
                result = subprocess.run(command, env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=(52 if scene == 'flight' else args.seconds) + 30)
                assert result.returncode == 0 and 'OpenGL error' not in result.stdout, result.stdout
                path = Path('benchmarks') / f'{scene}.csv'
                summary = benchmark.summarize(path, warmup=2)
                assert summary['generator'] == 5 and summary['generated_jobs'] > 16, summary
                assert summary['fluid_queue_overflow'] == 0 and summary['fluid_refused'] == 0, summary
                assert summary['final_radius'] == (4 if args.wide else 2), summary
                target = path.with_name(f'geography-v5-{"wide" if args.wide else "stress"}-{scene}.csv')
                path.replace(target)
                convert(ppm, ppm.with_suffix('.png'))
                entry = {'scene': scene, 'command': command[1:], 'output': result.stdout.splitlines(), 'summary': summary}
                target.with_suffix('.json').write_text(json.dumps(entry, indent=2) + '\n')
                report['scenes'].append(entry)
                print(f'PASS {scene}: graphics {summary["peak_graphics_reserved_mib"]:.2f} MiB, heap {summary["peak_cpu_mib"]:.2f} MiB', flush=True)
    finally:
        if server:
            server.terminate(); server.wait(timeout=5)
    destination = Path('benchmarks') / f'geography-v5-{"wide" if args.wide else "stress"}.json'
    destination.write_text(json.dumps(report, indent=2) + '\n')
    print(f'Wrote {destination}', flush=True)


if __name__ == '__main__':
    main()
