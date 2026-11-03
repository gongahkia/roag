#!/usr/bin/env python3
"""Observe native residents after ordinary paid waits, without injecting game state."""
import argparse
import json
from pathlib import Path
from verify_native_journey import Client, position, distance


def explored(client, manifest):
    result = set()
    for entry in manifest['chunks']:
        chunk = json.loads((Path(str(client.slot) + '.chunks') / (entry['hash'] + '.json')).read_text())
        key = position(entry['key'])
        result.update((key, index) for index in chunk['discovered'])
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    client = Client(args.output.resolve(), 42, window_size='1024x720', scene='world-campaign')
    try:
        client.control_click('dungeon:class:0')
        client.key('Tab')
        client.control_click('dungeon:save')
        before = json.loads(client.slot.read_text())
        before_map = explored(client, before)
        actors = {a['id']: a for a in before['state']['actors']}
        guards = [a for a in actors.values() if a['job'] == 'guard' and a['homeTown'] != 0]
        assert guards and all(a['routine'] is None for a in guards)
        client.capture('01-before-native-resident-routines')
        for _ in range(60):
            client.key('space')
        assert client.ui['nativeDungeon']['clock'] == 6000
        client.control_click('dungeon:save')
        after = json.loads(client.slot.read_text())
        residents = [a for a in after['state']['actors'] if a['kind'] == 0 and not a['recruited'] and a['life'] == 0]
        assert residents and all(a['routine'] is not None for a in residents)
        after_actors = {a['id']: a for a in after['state']['actors']}
        assert actors.keys() == after_actors.keys(), 'Routine activation invented an actor'
        movements = [dict(actor=a['id'], steps=distance(position(a['position']), position(after_actors[a['id']]['position']))) for a in guards]
        assert all(m['steps'] <= 1 for m in movements), 'A distant guard relocated'
        assert any(m['steps'] == 1 for m in movements), 'No distant guard physically moved'
        assert before_map == explored(client, after), 'NPC routine travel revealed terrain'
        client.capture('02-after-paid-native-resident-travel')
        clock = client.ui['nativeDungeon']['clock']
        client.request('wait', seconds=1)
        assert client.ui['nativeDungeon']['clock'] == clock
        client.resume()
        client.control_click('dungeon:save')
        assert after == json.loads(client.slot.read_text())
        client.capture('03-exact-native-resident-continuation')
        (client.output / 'result.json').write_text(json.dumps(dict(
            passed=True, clock=clock, residents=len(residents), distantGuards=movements,
            checks=['ordinary paid waits only', 'native resident activation outside the camera',
                    'physical distant guard steps', 'no actor replacement', 'no unexplored terrain revelation',
                    'thinking consumes no time', 'exact ordinary save/reload/resave']), indent=2) + '\n')
    finally:
        client.close()


if __name__ == '__main__':
    main()
