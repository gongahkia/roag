#!/usr/bin/env python3
"""Run a fixed-seed scene, verify budgets, and write a reproducible JSON report."""
import argparse
import csv
import json
import os
from pathlib import Path
import platform
import re
import statistics
import subprocess
import time

import sys
sys.dont_write_bytecode = True

import build

ROOT = build.ROOT
MIB = 1024 * 1024


def percentile(values, fraction):
    values = sorted(values)
    return values[int((len(values) - 1) * fraction)]


def summarize(path, warmup=3.0):
    with path.open() as file:
        rows = list(csv.DictReader(file))
    if not rows:
        raise AssertionError('Benchmark contains no frames')
    for row in rows:
        assert int(row['graphics_reserved_bytes']) <= int(row['gpu_budget_bytes']), 'Graphics budget exceeded'
        assert int(row['cpu_bytes']) <= int(row['cpu_budget_bytes']), 'CPU budget exceeded'
        assert int(row['upload_bytes']) <= int(row['upload_budget_bytes']), 'Upload budget exceeded'
    steady = [row for row in rows if float(row['elapsed_s']) >= warmup] or rows
    times = [float(row['frame_ms']) for row in steady]
    summary = {
        'frames': len(rows), 'elapsed_seconds': float(rows[-1]['elapsed_s']),
        'warmup_seconds': warmup, 'steady_frames': len(steady),
        'mean_frame_ms': statistics.mean(times),
        'measured_fps': 1000 / statistics.mean(times),
        'p95_frame_ms': percentile(times, 0.95), 'p99_frame_ms': percentile(times, 0.99),
        'mean_gpu_ms': statistics.mean(float(row['gpu_ms']) for row in steady),
        'peak_cpu_mib': max(int(row['cpu_bytes']) for row in rows) / MIB,
        'peak_mesh_mib': max(int(row['mesh_bytes']) for row in rows) / MIB,
        'peak_graphics_reserved_mib': max(int(row['graphics_reserved_bytes']) for row in rows) / MIB,
        'peak_upload_bytes': max(int(row['upload_bytes']) for row in rows),
        'final_radius': int(rows[-1]['radius']), 'pressure_events': int(rows[-1]['pressure']),
        'stale_results': int(rows[-1]['stale']),
        'steady_mean_triangles': statistics.mean(int(row['triangles']) for row in steady),
        'budgets_passed': True,
    }
    # OS working set is distinct from the engine's managed CPU allocation budget.
    if 'rss_bytes' in rows[0]:
        ram = [row for row in rows if int(row['ram_sample_valid'])]
        cpu = [row for row in steady if int(row['cpu_sample_valid'])]
        if ram:
            summary['peak_process_rss_mib'] = max(int(row['rss_bytes']) for row in ram) / MIB
            summary['peak_process_rss_hwm_mib'] = max(int(row['rss_peak_bytes']) for row in ram) / MIB
        if cpu:
            summary['mean_process_cpu_percent'] = statistics.mean(float(row['process_cpu_percent']) for row in cpu)
        summary['mean_rolling_1pct_low_fps'] = statistics.mean(float(row['low_1pct_fps']) for row in steady)
    if 'frame_cap' in rows[0]:
        summary['display_refresh_hz'] = int(rows[-1]['display_hz'])
        summary['frame_cap'] = int(rows[-1]['frame_cap'])  # Zero means no known software cap.
        summary['software_renderer'] = bool(int(rows[-1]['software_renderer']))
        summary['mean_swap_ms'] = statistics.mean(float(row['swap_ms']) for row in steady)
        summary['mean_pacing_wait_ms'] = statistics.mean(float(row['pacing_wait_ms']) for row in steady)
    assert any(int(row['draws']) > 0 for row in rows), 'Scene never rendered terrain'
    # The flight path repeats every 24 seconds after two seconds of warmup.
    if path.stem.startswith('flight') and summary['elapsed_seconds'] >= 50:
        first = [row for row in rows if 2 <= float(row['elapsed_s']) < 26]
        second = [row for row in rows if 26 <= float(row['elapsed_s']) < 50]
        first_peak = max(int(row['cpu_bytes']) for row in first)
        second_peak = max(int(row['cpu_bytes']) for row in second)
        assert second_peak <= first_peak + 4 * MIB, 'Repeated route shows CPU memory growth'
        assert max(int(row['chunks']) for row in second) <= max(int(row['chunks']) for row in first) + 64, 'Residency grew between repeated routes'
        summary['repeated_route_plateau_passed'] = True
        summary['first_cycle_peak_cpu_mib'] = first_peak / MIB
        summary['second_cycle_peak_cpu_mib'] = second_peak / MIB
    return summary


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--scene', choices=('normal', 'flight', 'edit', 'dense', 'fluids'), default='normal')
    parser.add_argument('--seconds', type=int, default=12)
    parser.add_argument('--profile', choices=('tiny', 'low', 'standard'), default='tiny')
    parser.add_argument('--radius', type=int)
    parser.add_argument('--gpu-mb', type=int)
    parser.add_argument('--cpu-mb', type=int)
    parser.add_argument('--upload-kb', type=int)
    parser.add_argument('--naive', action='store_true')
    parser.add_argument('--skip-build', action='store_true')
    parser.add_argument('--label')
    parser.add_argument('--screenshot')
    args = parser.parse_args()
    if args.seconds < 1:
        parser.error('--seconds must be positive')
    os.chdir(ROOT)
    if not args.skip_build:
        subprocess.run(['python3' if os.name != 'nt' else 'python', 'scripts/build.py', 'build'], check=True)
    binary = ROOT / 'build' / ('voxel.exe' if os.name == 'nt' else 'voxel')
    command = [str(binary), '--hidden', '--benchmark', args.scene, '--seconds', str(args.seconds), '--profile', args.profile]
    for flag, value in (('--radius', args.radius), ('--gpu-mb', args.gpu_mb), ('--cpu-mb', args.cpu_mb), ('--upload-kb', args.upload_kb), ('--screenshot', args.screenshot)):
        if value is not None:
            command += [flag, str(value)]
    if args.naive:
        command += ['--naive-mesh']
    print('Running:', ' '.join(command), flush=True)
    started = time.time()
    result = subprocess.run(command, env=build.environment(), text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=args.seconds + 60)
    print(result.stdout, end='')
    result.check_returncode()
    path = ROOT / 'benchmarks' / (args.scene + ('-naive' if args.naive else '') + '.csv')
    if args.label:
        if Path(args.label).name != args.label:
            raise SystemExit('Label must be a file name')
        target = path.with_name(args.label + '.csv')
        path.replace(target)
        path = target
    report = {
        'command': command[1:], 'platform': platform.platform(),
        'processor': platform.processor(), 'started_unix': started,
        'toolchain': json.loads((ROOT / 'toolchain.json').read_text())['release'],
        'runtime_output': result.stdout.splitlines(),
        'summary': summarize(path),
    }
    peak = re.search(r'CPU peak ([0-9.]+) MiB', result.stdout)
    if peak:
        report['summary']['allocator_peak_cpu_mib'] = float(peak.group(1))
    path.with_suffix('.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report['summary'], indent=2))


if __name__ == '__main__':
    main()
