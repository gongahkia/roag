#!/usr/bin/env python3
"""Collect runtime stacks, GC counters and graphics captures on a private display."""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools'))
from agent_support.workspace import Repository, atomic_json
from agent_support import runner
from playtest_support import driver

TOOL_VERSION = '10.0.750501'


def diagnostic_tool(repo, name, install=False):
    root = repo.root / 'artifacts/diagnostic-tools'
    def find():
        return sorted(root.glob(f'.store/{name}/{TOOL_VERSION}/{name}/{TOOL_VERSION}/tools/*/any/{name}.dll'))
    matches = find()
    if not matches and install:
        subprocess.run([str(repo.root / 'tools/dotnet.sh'), 'tool', 'install', name,
                        '--version', TOOL_VERSION, '--tool-path', str(root), '--allow-roll-forward'], check=True)
        matches = find()
    if len(matches) != 1:
        raise ValueError(f'{name} {TOOL_VERSION} is missing; use --install-tools.')
    return [str(repo.root / 'tools/dotnet.sh'), str(matches[0])]


def client_process(repo, process):
    if not runner.alive(process):
        raise ValueError('The isolated client process is no longer live.')
    wanted = str(repo.root / 'src/Vagabonds.Client/bin/Release/net10.0/Vagabonds.Client.dll').encode()
    pending = [process['pid']]
    while pending:
        pid = pending.pop(0)
        try:
            if Path(f'/proc/{pid}/exe').resolve().name == 'dotnet' and wanted in Path(f'/proc/{pid}/cmdline').read_bytes().split(b'\0'):
                token = runner.identity(pid)
                if token and runner.alive(token):
                    return token
            pending.extend(int(p) for p in Path(f'/proc/{pid}/task/{pid}/children').read_text().split())
        except (FileNotFoundError, ProcessLookupError):
            continue
    raise ValueError('Could not identify the .NET client inside the owned private-display process tree.')


def terminate(process):
    if process.poll() is None:
        process.terminate()
        try:
            process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            process.kill(); process.wait(timeout=5)


def graphics_evidence(info, profile):
    rows = [line.split() for line in profile.splitlines() if line.startswith('call ') and 'glDraw' in line]
    if info.get('FramesCount', 0) < 1 or not rows or any(len(row) != 13 for row in rows):
        raise ValueError('Replay did not produce frame and draw-call profiling evidence.')
    gpu = sum(int(row[3]) > 0 for row in rows); cpu = sum(int(row[5]) > 0 for row in rows)
    if not gpu or not cpu:
        raise ValueError('Replay did not produce positive CPU and GPU-query draw durations.')
    return {'frames': info['FramesCount'], 'drawSamples': len(rows), 'positiveGpuQuerySamples': gpu, 'positiveCpuSamples': cpu}


def runtime_profile(repo, args, output):
    trace = diagnostic_tool(repo, 'dotnet-trace', args.install_tools)
    counters = diagnostic_tool(repo, 'dotnet-counters', args.install_tools)
    current = driver.start(repo, seed=args.seed, scene=args.scene, no_build=True, root=output / 'runs')
    children = []
    try:
        folder = Path(current['folder'])
        token = client_process(repo, json.loads((folder / 'playtest.json').read_text())['process'])
        if args.live and not current['observation'].get('battle') and current['observation']['paused']:
            result = driver.act(repo, current['id'], {'kind': 'control', 'control': 'pause'}, output / 'runs')
            if not result['delivered']:
                raise ValueError('Could not start the live simulation workload.')
        duration = f'00:00:00:{args.seconds:02d}'
        commands = [trace + ['collect', '-p', str(token['pid']), '--profile', 'dotnet-common,dotnet-sampled-thread-time,gc-verbose',
                             '--format', 'Speedscope', '--duration', duration, '--buffersize', '64', '-o', str(output / 'runtime.nettrace')],
                    counters + ['collect', '-p', str(token['pid']), '--counters', 'System.Runtime', '--format', 'json',
                                '--duration', duration, '-o', str(output / 'counters.json')]]
        for index, command in enumerate(commands):
            with (output / f'collector-{index}.log').open('wb') as log:
                children.append(subprocess.Popen(command, cwd=repo.root, stdin=subprocess.DEVNULL, stdout=log, stderr=subprocess.STDOUT))
        remaining = args.seconds + 2
        while remaining > 0:
            seconds = min(30, remaining)
            result = driver.act(repo, current['id'], {'kind': 'wait', 'seconds': seconds}, output / 'runs', timeout=seconds + 20)
            if not result['delivered']:
                raise ValueError('The profiling workload wait failed.')
            remaining -= seconds
        for child in children:
            if child.wait(timeout=30) != 0:
                raise ValueError('A diagnostic collector failed; inspect collector logs.')
        with (output / 'top-methods.txt').open('wb') as log:
            subprocess.run(trace + ['report', str(output / 'runtime.nettrace'), 'topN', '-n', '25'], check=True, stdout=log, stderr=subprocess.STDOUT, timeout=30)
        speedscope = json.loads((output / 'runtime.speedscope.json').read_text())
        counter_data = json.loads((output / 'counters.json').read_text())
        if not speedscope.get('profiles') or not counter_data.get('Events'):
            raise ValueError('Collectors produced no sampled stacks or runtime counters.')
        return {'run': current['id'], 'process': token, 'seconds': args.seconds, 'live': args.live,
                'sampledProfiles': len(speedscope['profiles']), 'counterEvents': len(counter_data['Events']),
                'scope': 'Managed stack wall-time samples, allocation/GC events and process/runtime counters. Includes profiler overhead and software-renderer scheduling; these are not kernel CPU samples or hardware GPU timings.'}
    finally:
        for child in children:
            terminate(child)
        driver.stop(repo, current['id'], output / 'runs')


def graphics_profile(repo, args, output):
    apitrace = args.apitrace or shutil.which('apitrace')
    if not apitrace:
        portable = repo.root / 'artifacts/diagnostic-tools/apitrace-portable/usr/bin/apitrace'
        apitrace = str(portable) if portable.is_file() else None
    if not apitrace:
        raise ValueError('Graphics capture needs apitrace; use --apitrace PATH for a portable build.')
    trace = output / 'graphics.trace'
    current = driver.start(repo, seed=args.seed, scene=args.scene, no_build=True, root=output / 'runs',
                           launch_prefix=[apitrace, 'trace', '--api=gl', '--output', str(trace)])
    try:
        result = driver.act(repo, current['id'], {'kind': 'wait', 'seconds': .5}, output / 'runs')
        if not result['delivered']:
            raise ValueError('Graphics capture workload failed.')
    finally:
        closed = driver.stop(repo, current['id'], output / 'runs')
    if closed['forcedStop'] or not trace.is_file() or trace.stat().st_size < 1024:
        raise ValueError('Graphics trace did not close cleanly; inspect the isolated client log.')
    info = json.loads(subprocess.check_output([apitrace, 'info', str(trace)], timeout=15, text=True))
    atomic_json(output / 'graphics-info.json', info)
    with (output / 'graphics-calls.txt').open('w') as calls:
        subprocess.run([apitrace, 'dump', '--color=never', '--grep=glDraw.*|glXSwapBuffers', str(trace)],
                       check=True, stdout=calls, timeout=30)
    if 'glDraw' not in (output / 'graphics-calls.txt').read_text():
        raise ValueError('The graphics trace contains no draw commands.')
    command = ['xvfb-run', '-a', '-s', '-screen 0 1280x900x24', 'env', 'LIBGL_ALWAYS_SOFTWARE=1',
               'SDL_AUDIODRIVER=dummy', apitrace, 'replay', '--headless', '--pcpu', '--pgpu', str(trace)]
    with (output / 'graphics-profile.txt').open('w') as profile, (output / 'graphics-replay.log').open('w') as log:
        subprocess.run(command, check=True, stdout=profile, stderr=log, timeout=60)
    evidence = graphics_evidence(info, (output / 'graphics-profile.txt').read_text())
    return {'run': current['id'], 'trace': trace.name, 'bytes': trace.stat().st_size, **evidence,
            'scope': 'Actual OpenGL API trace, draw list and CPU/GPU-query replay profile on private Xvfb/software displays. Includes capture/replay overhead; software-driver query durations are not intended-hardware GPU timings.'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--scene', choices=driver.SCENES, default='world')
    parser.add_argument('--seed', type=int, default=42)
    parser.add_argument('--kind', choices=['runtime', 'graphics', 'all'], default='runtime')
    parser.add_argument('--seconds', type=int, choices=range(5, 46), default=15)
    parser.add_argument('--live', action='store_true', help='Advance the simulation during runtime collection.')
    parser.add_argument('--install-tools', action='store_true')
    parser.add_argument('--apitrace')
    parser.add_argument('--no-build', action='store_true')
    args = parser.parse_args()
    repo = Repository(Path(__file__).resolve().parents[1]); output = args.output.resolve()
    if output.exists():
        parser.error('Choose a new output directory to preserve earlier evidence.')
    if not args.no_build:
        driver.build(repo)
    output.mkdir(parents=True)
    report = {'schemaVersion': 1, 'source': repo.fingerprint(), 'head': repo.git('rev-parse', 'HEAD').decode().strip(),
              'seed': args.seed, 'scene': args.scene, 'display': 'private-xvfb-software', 'diagnosticToolVersion': TOOL_VERSION}
    atomic_json(output / 'report.json', {**report, 'status': 'running'})
    try:
        if args.kind in ('runtime', 'all'):
            report['runtime'] = runtime_profile(repo, args, output)
        if args.kind in ('graphics', 'all'):
            report['graphics'] = graphics_profile(repo, args, output)
        report['status'] = 'passed'
    except BaseException as error:
        atomic_json(output / 'report.json', {**report, 'status': 'failed', 'error': str(error)})
        raise
    atomic_json(output / 'report.json', report)
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
