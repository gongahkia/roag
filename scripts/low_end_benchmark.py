#!/usr/bin/env python3
"""Repeat warmed low-end scenes, gate readiness/budgets, and report frame/edit latency."""
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

import benchmark
import build

SCENES = ('forest', 'jungle', 'caves', 'fluids', 'flight', 'edit')


def analyze(path, scene, warmup=2, expected_radius=None):
    path = Path(path)
    with path.open() as file:
        rows = list(csv.DictReader(file))
    assert rows and all(None not in r and None not in r.values() for r in rows), 'Empty/misaligned CSV'
    assert all(int(r['requested_chunks']) > 0 for r in rows), 'No terrain requested'
    if expected_radius is not None:
        assert all(int(r['radius']) == expected_radius for r in rows), 'Draw distance reduced under pressure'
    complete = [r for r in rows if int(r['ready_chunks']) == int(r['requested_chunks'])]
    assert complete, 'Terrain never became fully ready; refusing to score incomplete rendering'
    first_ready = float(complete[0]['elapsed_s'])
    steady = [r for r in rows if float(r['elapsed_s']) >= first_ready + warmup]
    assert len(steady) >= 10, 'Insufficient fully warmed frames'
    result = benchmark.summarize(path, warmup=first_ready + warmup)
    drawn = [r for r in rows if int(r['draws']) > 0]
    assert drawn, 'No terrain was drawn'
    result.update(
        first_terrain_ms=float(drawn[0]['elapsed_s']) * 1000,
        full_terrain_ms=first_ready * 1000,
        min_ready_fraction=min(int(r['ready_chunks']) / int(r['requested_chunks']) for r in steady),
        mean_ready_fraction=statistics.mean(int(r['ready_chunks']) / int(r['requested_chunks']) for r in steady),
    )
    assert result['fluid_queue_overflow'] == 0 and result['fluid_refused'] == 0, 'Fluid simulation exceeded its limits'
    latencies = [float(r[f'edit_latency_{i}']) for r in steady for i in range(int(r['edit_latency_count']))]
    if scene == 'edit':
        assert latencies, 'No edited chunk finished uploading'
    result['edit_latency'] = {
        'samples': len(latencies),
        'p50_ms': benchmark.percentile(latencies, .5) if latencies else None,
        'p95_ms': benchmark.percentile(latencies, .95) if latencies else None,
        'max_ms': max(latencies) if latencies else None,
    }
    if scene == 'flight' and float(rows[-1]['elapsed_s']) - first_ready >= 50:
        cycles = [[r for r in rows if start <= float(r['elapsed_s']) - first_ready < start + 24] for start in (0, 24)]
        peaks = [max(int(r['cpu_bytes']) for r in cycle) for cycle in cycles]
        assert peaks[1] <= peaks[0] + 4 * benchmark.MIB, 'Repeated-route memory growth'
        result['ready_route_plateau_passed'] = True
    return result


def fingerprint():
    digest = hashlib.sha256()
    for path in sorted(list((build.ROOT / 'src').glob('*.odin')) + list((build.ROOT / 'shaders').glob('*'))):
        digest.update(str(path.relative_to(build.ROOT)).encode() + b'\0' + path.read_bytes())
    return digest.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--skip-build', action='store_true')
    parser.add_argument('--scenes', nargs='+', choices=SCENES, default=list(SCENES))
    parser.add_argument('--repeats', type=int, default=3)
    parser.add_argument('--seconds', type=int, default=12)
    parser.add_argument('--flight-seconds', type=int, default=52)
    parser.add_argument('--profile', choices=('tiny', 'low', 'standard'), default='tiny')
    parser.add_argument('--lighting', choices=('full', 'fast', 'off'), default='full')
    parser.add_argument('--stress', action='store_true', help='640x360, radius 2, 16/32 MiB, 64 KiB uploads')
    parser.add_argument('--max-p95-ms', type=float)
    parser.add_argument('--max-edit-ms', type=float)
    parser.add_argument('--min-ready-fraction', type=float, default=0)
    parser.add_argument('--baseline', type=Path, help='Compare matching scene medians against a previous report')
    parser.add_argument('--max-regression', type=float, default=1.2)
    parser.add_argument('--output', type=Path, default=Path('benchmarks/low-end-report.json'))
    args = parser.parse_args()
    if args.repeats < 1 or args.seconds < 4 or args.flight_seconds < 4 or not 0 <= args.min_ready_fraction <= 1 or args.max_regression <= 0:
        parser.error('Expected repeats >=1, durations >=4, readiness in [0,1], positive regression ratio')
    os.chdir(build.ROOT)
    if not args.skip_build:
        subprocess.run([sys.executable, 'scripts/build.py', 'build'], check=True)
    binary = build.ROOT / 'build' / ('voxel.exe' if os.name == 'nt' else 'voxel')
    env, server = build.environment(), None
    report = dict(platform=platform.platform(), processor=platform.processor(),
                  source_sha256=fingerprint(), binary_sha256=hashlib.sha256(binary.read_bytes()).hexdigest(),
                  configuration=vars(args).copy(), runs=[], scenes={}, failures=[])
    report['configuration'] = {k: str(v) if isinstance(v, Path) else v for k, v in report['configuration'].items()}
    run_directory = args.output.parent / f'{args.output.stem}-runs-{time.strftime("%Y%m%dT%H%M%S")}-{os.getpid()}'
    run_directory.mkdir(parents=True, exist_ok=False)
    try:
        if sys.platform.startswith('linux') and shutil.which('Xvfb'):
            number = next(n for n in range(91, 150) if not Path(f'/tmp/.X11-unix/X{n}').exists())
            server = subprocess.Popen(['Xvfb', f':{number}', '-screen', '0', '1920x1080x24', '-nolisten', 'tcp'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            env['DISPLAY'] = f':{number}'; env.pop('WAYLAND_DISPLAY', None); env.pop('XDG_RUNTIME_DIR', None); env['XDG_SESSION_TYPE'] = 'x11'
            # Keep a usable physical GL implementation when available. Pass
            # LIBGL_ALWAYS_SOFTWARE=1 explicitly to request software validation.
            time.sleep(.4)
        for repeat in range(args.repeats):
            for scene in (args.scenes if repeat % 2 == 0 else list(reversed(args.scenes))):
                seconds = args.flight_seconds if scene == 'flight' else args.seconds
                command = [str(binary), '--hidden', '--benchmark', scene, '--benchmark-ready', '--seconds', str(seconds),
                           '--profile', args.profile, '--lighting', args.lighting, '--seed', '42', '--generator', '5']
                if args.stress:
                    command += ['--radius', '2', '--width', '640', '--height', '360', '--gpu-mb', '16', '--cpu-mb', '32', '--upload-kb', '64']
                print(f'Running {scene}, repeat {repeat + 1}/{args.repeats}', flush=True)
                result = subprocess.run(command, env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=seconds + 90)
                record = dict(scene=scene, repeat=repeat + 1, command=command[1:], output=result.stdout.splitlines())
                try:
                    assert result.returncode == 0, result.stdout
                    source = build.ROOT / 'benchmarks' / f'{scene}.csv'
                    target = run_directory / f'{scene}-{repeat + 1}.csv'
                    shutil.move(source, target); record['csv'] = os.path.relpath(target.resolve(), build.ROOT)
                    radius = 2 if args.stress else dict(tiny=4, low=8, standard=12)[args.profile]
                    record['summary'] = analyze(target, scene, expected_radius=radius)
                    summary = record['summary']
                    assert summary['min_ready_fraction'] >= args.min_ready_fraction, 'Readiness threshold failed'
                    if args.max_p95_ms is not None:
                        assert summary['p95_frame_ms'] <= args.max_p95_ms, 'Frame-time threshold failed'
                    if args.max_edit_ms is not None and summary['edit_latency']['samples']:
                        assert summary['edit_latency']['p95_ms'] <= args.max_edit_ms, 'Edit-latency threshold failed'
                except (AssertionError, KeyError, ValueError) as error:
                    record['failure'] = str(error); report['failures'].append(f'{scene}/{repeat + 1}: {error}')
                report['runs'].append(record)
        for scene in args.scenes:
            summaries = [r['summary'] for r in report['runs'] if r['scene'] == scene and 'summary' in r]
            if summaries:
                report['scenes'][scene] = {key: statistics.median(s[key] for s in summaries) for key in ('p95_frame_ms', 'p99_frame_ms', 'full_terrain_ms', 'peak_cpu_mib', 'peak_graphics_reserved_mib')}
        if args.baseline:
            baseline = json.loads(args.baseline.read_text())
            # Comparing different quality/resolution/budgets is not a regression test.
            for key in ('profile', 'lighting', 'stress', 'seconds', 'flight_seconds'):
                assert baseline['configuration'][key] == report['configuration'][key], f'Baseline configuration mismatch: {key}'
            for scene in args.scenes:
                if scene not in report['scenes'] or scene not in baseline['scenes']:
                    report['failures'].append(f'Missing baseline scene: {scene}'); continue
                ratio = report['scenes'][scene]['p95_frame_ms'] / baseline['scenes'][scene]['p95_frame_ms']
                report['scenes'][scene]['p95_regression_ratio'] = ratio
                if ratio > args.max_regression:
                    report['failures'].append(f'{scene}: p95 regression {ratio:.3f}x')
        report['passed'] = not report['failures']
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(report, indent=2) + '\n')
        print(json.dumps(dict(passed=report['passed'], scenes=report['scenes'], failures=report['failures']), indent=2))
        if not report['passed']:
            raise SystemExit(1)
    finally:
        if server:
            server.terminate(); server.wait(timeout=5)


if __name__ == '__main__':
    main()
