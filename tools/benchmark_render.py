#!/usr/bin/env python3
"""Capture comparable render costs in isolated, stationary native scenes without model calls."""
import argparse
import json
from pathlib import Path
import statistics

from agent_support.workspace import Repository, atomic_json
from playtest_support import driver, recordings


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--seed', type=int, default=42)
    parser.add_argument('--scenes', nargs='+', choices=driver.SCENES, default=['world', 'world-resources', 'world-battle'])
    parser.add_argument('--no-build', action='store_true')
    args = parser.parse_args()
    repo = Repository(Path(__file__).resolve().parents[1]); output = args.output.resolve()
    if output.exists():
        parser.error('Choose a new output directory to preserve earlier evidence.')
    output.mkdir(parents=True)
    if not args.no_build:
        driver.build(repo)
    results = []
    for scene in args.scenes:
        current = driver.start(repo, scene=scene, seed=args.seed, no_build=True, root=output / 'runs')
        try:
            result = driver.act(repo, current['id'], {'kind': 'wait', 'seconds': 12}, output / 'runs')
            if not result['delivered']:
                raise RuntimeError(result['response']['reason'])
        finally:
            driver.stop(repo, current['id'], output / 'runs')
        folder = Path(current['folder'])
        session = recordings.sessions(folder / 'recordings')[0]
        samples = recordings.events(folder / 'recordings', session['id'], kind='performance', limit=200)['events']
        # Discard startup and the final partial interval; compare full warmed five-second windows.
        samples = [s['data'] for s in samples[1:] if s['data']['seconds'] >= 5]
        if not samples or 'renderP95Ms' not in samples[0]:
            raise RuntimeError('No warmed render measurements; rebuild the instrumented client.')
        results.append({'scene': scene, 'run': current['id'], 'samples': samples,
                        'median': {key: statistics.median(s[key] for s in samples) for key in
                                   ['frameP95Ms', 'updateP95Ms', 'renderP95Ms', 'terrainP95Ms', 'allocatedBytesPerFrame', 'drawCallsPerFrame']}})
    report = {'schemaVersion': 1, 'seed': args.seed, 'head': repo.git('rev-parse', 'HEAD').decode().strip(),
              'source': repo.fingerprint(), 'results': results,
              'scope': 'Stationary explicit preview scenes at 1024x720, Xvfb/software rendering and dummy audio. Warmed wall-clock timings, main-thread allocations and draw submissions; not GPU timings or a production FPS benchmark. No player saves or model calls.'}
    atomic_json(output / 'report.json', report)
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
