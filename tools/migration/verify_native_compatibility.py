#!/usr/bin/env python3
"""Verify an actual continuing native save against a separately pinned runtime.

Only free Save/capture/stop/load controls are used. Any complete manifest
divergence halts dependent gameplay without repair or replay.
"""
import argparse
import json
from pathlib import Path
import shutil

from verify_native_entrance import exact
from verify_native_travel import retain_live_timeout
from verify_native_world import WorldClient


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--runtime', type=Path, required=True)
    parser.add_argument('--continue-from', type=Path, required=True)
    args = parser.parse_args()
    previous = args.continue_from.resolve()
    expected = json.loads((previous / 'control/campaign.json').read_text())
    client = WorldClient.__new__(WorldClient)
    try:
        client.__init__(args.output.resolve(), 42, runtime_source=args.runtime.resolve(),
                        continuation=previous, window_size='1024x720', scene='world-campaign')
        client.control_click('dungeon:save')
        assert json.loads(client.slot.read_text()) == expected, 'Initial ordinary load/save changed the complete manifest'
        exact(client, '00-runtime-compatibility-exact')
        assert json.loads(client.slot.read_text()) == expected
        state = expected['state']
        campaign = state['nativeCampaign']
        assert 'finale' not in campaign and all('special' not in c for c in campaign['complexes'])
        client.capture('01-retained-partial-rest')
        n = client.ui['nativeDungeon']
        (client.output / 'handoff.json').write_text(json.dumps(dict(
            position=client.ui['position'], clock=n['clock'], player=n['player'], rollback=False)) + '\n')
        if (previous / 'world-knowledge.json').exists():
            shutil.copy2(previous / 'world-knowledge.json', client.output / 'world-knowledge.json')
        result = dict(passed=True, scope='complete exact-load compatibility before further paid input',
                      clock=n['clock'], needs=n['needs'], activities=n['activities'], inventory=client.ui['inventory'],
                      absentFinaleAndSpecialPreserved=True, paidInputs=0,
                      limits='Old world retains absent fresh-generation metadata. Earlier labelled accepted-input reconstruction remains; no current fresh-world generation, completed rest, excavation or entrance proof.')
        (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
        print(json.dumps(result), flush=True)
        client.request('stop'); client.process.wait(timeout=15)
    except BaseException as error:
        if hasattr(client, 'journal'):
            (client.output / 'result.json').write_text(json.dumps(dict(
                passed=False, error=str(error), errorType=type(error).__name__, repair=False, replay=False), indent=2) + '\n')
            if isinstance(error, TimeoutError) and client.process.poll() is None:
                retain_live_timeout(client, error)
        raise
    finally:
        if hasattr(client, 'journal') and hasattr(client, 'log'):
            client.close()


if __name__ == '__main__':
    main()
