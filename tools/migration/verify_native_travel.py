#!/usr/bin/env python3
"""Continue the actual reported entrance journey with observed paid movement.

Only UI-reported destination coordinates guide travel. Every crossed tile comes
from the traveller's observed map. No hidden saved geometry or state injection.
"""
import argparse
from collections import deque
import json
from pathlib import Path
import subprocess
import time

from verify_native_entrance import Walk, exact
from verify_native_journey import Journey, DIRECTIONS, distance, position
from verify_native_world import WorldClient, saved_complexes


class TravelFight(Journey):
    def observe(self):
        super().observe()
        self.player = next(p for p in self.state['player'] if p['id'] == self.state['controlledActor'])

    def aligned(self, a, b):
        limit = 4 if self.player['classId'] == 'Ranger' else 3
        if a[2] != b[2] or a[0] != b[0] and a[1] != b[1] or distance(a, b) > limit:
            return False
        dx, dy = (b[0] > a[0]) - (b[0] < a[0]), (b[1] > a[1]) - (b[1] < a[1])
        p = a[0] + dx, a[1] + dy, a[2]
        while p != b:
            if not self.known.get(p, {}).get('groundOpen') or p in self.blocked or p in self.occupied:
                return False
            p = p[0] + dx, p[1] + dy, p[2]
        return True


def navigation(walk, target):
    previous = {walk.at: None}
    queue = deque([walk.at])
    while queue:
        p = queue.popleft()
        for dx, dy, key in DIRECTIONS:
            q = p[0] + dx, p[1] + dy, p[2]
            if q not in previous and walk.known.get(q) and q not in walk.blocked and q != target:
                previous[q] = p, key
                queue.append(q)
    nearby = [p for p in walk.neighbors(target) if p in previous and p != walk.at]
    if nearby:
        goal = min(nearby, key=lambda p: distance(p, walk.at))
    else:
        frontiers = [p for p in previous if p != walk.at and any(n not in walk.known for n in walk.neighbors(p))]
        assert frontiers, 'No observed reachable frontier toward the reported entrance'
        goal = min(frontiers, key=lambda p: distance(p, target) + distance(p, walk.at) * .15 + walk.visits[p] * 12)
    route = []
    p = goal
    while previous[p] is not None:
        before, key = previous[p]
        route.append((p, key)); p = before
    return route[::-1]


def observe_report(client):
    before = client.ui['nativeDungeon']['clock']
    client.control_click('dungeon:menu'); client.control_click('dungeon:journal')
    reports = client.ui['expeditions']['entrances']
    assert reports, 'No legitimately recorded entrance report exists'
    origin = position(client.ui['position'])
    report = min(reports, key=lambda r: distance(origin, position(r['position'])))
    client.capture('02-reported-destination')
    client.control_click('dungeon:close')
    assert client.ui['nativeDungeon']['clock'] == before
    return report


def split_crossing(client, walk, target, party):
    walk.observe()
    assert distance(walk.at, target) == 1 and target in walk.visible
    assert walk.visible[target]['material'] == 'StairsDown', 'Reported surface entrance is not an actually observed stair'
    leader = party[0]
    other = party[1]
    assert client.ui['nativeDungeon']['activeActor'] == leader
    client.capture('04-observed-physical-entrance')
    clock = client.ui['nativeDungeon']['clock']
    client.key('2'); client.key('b'); client.control_click('dungeon:split'); client.key('Escape'); client.key('t')
    assert client.ui['nativeDungeon']['clock'] == clock
    parked = next(p for p in client.ui['nativeDungeon']['player'] if p['id'] == other)['position']
    dx, dy = target[0] - walk.at[0], target[1] - walk.at[1]
    key = next(k for x, y, k in DIRECTIONS if (x, y) == (dx, dy))
    client.key(key)
    n = client.ui['nativeDungeon']
    entered = next(p for p in n['player'] if p['id'] == leader)
    assert entered['position']['floor'] == 1 and n['clock'] >= clock
    assert client.ui['notice'] == 'Action committed.'
    assert next(p for p in n['player'] if p['id'] == other)['position'] == parked
    assert 'core:achievement/pathfinder' in n['profile']['earned']
    client.capture('05-individual-stair-arrival')
    for _ in range(4):
        client.key('t')
        before = client.ui['nativeDungeon']['clock']
        client.key('space')
        # Separate ready members may legally act at the same global timestamp.
        assert client.ui['nativeDungeon']['clock'] >= before
        assert client.ui['notice'] == 'Action committed.'
    client.key('2'); client.capture('06-private-outdoor-member')
    client.key('1'); client.capture('07-private-underground-member')
    final = exact(client, '08-split-floor-exact')
    members = [a for a in final['actors'] if a['id'] in party]
    assert {a['position']['floor'] for a in members} == {0, 1}
    proof = saved_complexes(client, '09-split-world-proof')
    walk.checkpoint('10-split-continuation-ready')
    return dict(passed=True, scope='reported physical surface entrance / individual paid stair / private split-floor paid exact continuation',
                clock=client.ui['nativeDungeon']['clock'], profile=proof['profile'], run=proof['run'], party=party,
                limits='Initial stage1 floor only; no deep complex, special deadline, new boss, victory, profile final-death or package acceptance.')


def verify(client, budget):
    start = exact(client, '00-exact-continuation')
    party = start['party']
    assert len(party) == 2 and all(a['life'] == 0 for a in start['actors'] if a['id'] in party)
    client.key('t')
    report = observe_report(client)
    target = position(report['position'])
    walk = Walk(client)
    client.travel_walk = walk
    fight = TravelFight(client)
    fight.last_floor = 0
    stalls = 0
    for step in range(budget):
        # The existing bridge deliberately caps each process at 1024 requests.
        # Count free controls too; preserve the complete actual state well before
        # that cap, then reset only the bridge via an ordinary exact reload.
        if client.sequence >= 680:
            walk.checkpoint('bridge-boundary-' + str(step))
            exact(client, 'bridge-boundary-' + str(step) + '-exact')
        if client.ui['nativeDungeon']['controlledActor'] != client.ui['nativeDungeon']['activeActor']:
            client.key('t')
        walk.observe()
        assert all(p['health'] > 0 for p in walk.native['player']), 'A traveller fell during the actual journey'
        fight.observe()
        if fight.fighters and fight.fight():
            continue
        if walk.native['activeActor'] != party[0]:
            client.key('space')
            continue
        if distance(walk.at, target) == 1:
            return split_crossing(client, walk, target, party)
        follower = next(p for p in walk.native['player'] if p['id'] == party[1])
        if distance(walk.at, position(follower['position'])) > 6:
            client.key('space')
            stalls += 1
            assert stalls < 100, 'Follower did not make physical progress during paid cohesion waits'
        else:
            stalls = 0
            route = navigation(walk, target)
            first_key = route[0][1]
            dash = len(route) >= 3 and all(key == first_key and p in walk.visible
                and not walk.visible[p]['material'].startswith('Stairs') for p, key in route[:3])
            before = walk.native['clock']
            client.key(('x+' if dash else '') + first_key)
            assert client.ui['nativeDungeon']['clock'] > before, 'Observed travel did not commit an actual action'
        if step % 25 == 0:
            walk.checkpoint('travel-' + str(step))
            print('remaining', distance(position(client.ui['position']), target), flush=True)
    walk.checkpoint('bounded-travel-checkpoint')
    exact(client, 'bounded-travel-exact')
    return dict(passed=False, outcome='bounded-travel-checkpoint', scope='actual continuing paid surface travel',
                clock=client.ui['nativeDungeon']['clock'], report=report,
                remaining=distance(position(client.ui['position']), target), party=party,
                limits='Entrance not yet reached; continue this latest ordinary checkpoint without reroll or rollback.')


def recover_accepted(client, source):
    """Reconstruct a lost unsaved tail using only its already accepted inputs.

    This is explicitly labelled reconstruction, never uninterrupted evidence.
    Every retained boundary must match; any divergence aborts dependent travel.
    """
    records = [json.loads(line) for line in (source / 'inputs.jsonl').read_text().splitlines()]
    last_save = max(i for i, row in enumerate(records) if row['input'].get('control') == 'dungeon:save')
    tail = records[last_save + 1:]
    assert len(tail) == 27, 'Recovery is restricted to the reviewed 27 accepted receipts'
    old_manifest = json.loads((source / 'control/campaign.json').read_text())
    assert json.loads(client.slot.read_text()) == old_manifest
    # The first retained capture followed the original checkpoint's Save, so
    # reproduce that free presentation context after loading the exact save.
    client.control_click('dungeon:save')
    assert json.loads(client.slot.read_text()) == old_manifest, 'Recovery prelude changed the continuing manifest'
    expected = json.loads((source / 'control/observation.json').read_text())['ui']
    walk = Walk(client)
    client.travel_walk = walk
    checks = []
    for row in tail:
        spec = row['input']
        if row['physicalKey']:
            client.key(row['physicalKey'], seconds=spec['seconds'])
        else:
            assert spec['kind'] == 'capture', 'Unreviewed recovery input'
            client.request('capture')
        actual = dict(position=client.ui['position'], clock=client.ui['nativeDungeon']['clock'],
                      health=client.ui['nativeDungeon']['player'][0]['health'],
                      notice=client.ui['notice'], panel=client.ui['panel'])
        recorded = {key: row[key] for key in actual}
        checks.append(dict(original=spec['id'], actual=actual, expected=recorded, matched=actual == recorded))
        (client.output / 'recovery-boundary-checks.json').write_text(json.dumps(checks, indent=2) + '\n')
        assert actual == recorded, 'Accepted receipt diverged: ' + spec['id']
        walk.observe()
    fields = ['seed', 'tick', 'position', 'inventory', 'panel', 'notice', 'weatherDescription',
              'cells', 'actors', 'expeditions']
    native = ['clock', 'activeActor', 'controlledActor', 'player', 'profile', 'timeline', 'following',
              'needs', 'equipment', 'packEquipment', 'activities', 'fighters', 'telegraphs', 'conditions']
    for key in fields:
        if client.ui[key] != expected[key]:
            (client.output / 'recovery-public-divergence.json').write_text(json.dumps(dict(
                field=key, expected=expected[key], actual=client.ui[key]), indent=2) + '\n')
        assert client.ui[key] == expected[key], 'Final retained public snapshot diverged: ' + key
    for key in native:
        if client.ui['nativeDungeon'][key] != expected['nativeDungeon'][key]:
            (client.output / 'recovery-public-divergence.json').write_text(json.dumps(dict(
                field='nativeDungeon.' + key, expected=expected['nativeDungeon'][key], actual=client.ui['nativeDungeon'][key]), indent=2) + '\n')
        assert client.ui['nativeDungeon'][key] == expected['nativeDungeon'][key], 'Final native snapshot diverged: ' + key
    # Save only after all retained public checks succeed, with no alternative input.
    walk.checkpoint('reconstructed-final-accepted-state')
    exact(client, 'reconstructed-final-exact')
    result = dict(passed=True, scope='fixed accepted-input reconstruction after automation request-cap failure',
                  source=str(source), receipts=checks, publicFields=fields, nativeFields=native,
                  clock=client.ui['nativeDungeon']['clock'], position=client.ui['position'],
                  limitations='Original process lost unsaved progress. Recorded-input reconstruction with retained public-state agreement; not uninterrupted or full hidden-state evidence.')
    (client.output / 'recovery-proof.json').write_text(json.dumps(result, indent=2) + '\n')
    return result


def retain_live_timeout(client, error):
    """Keep Xvfb/client alive for review instead of destroying unsaved progress."""
    record = dict(error=str(error), clientPid=client.process.pid, live=client.process.poll() is None,
                  replayed=False, retained=True)
    if record['live']:
        subprocess.run(['xdotool', 'keyup', 'w', 'a', 's', 'd', 'f', 'q', 'e', 'z', 'c', 'x', 't', 'space'], check=False)
        # F5 is the ordinary free keyboard save. During an active timed request
        # it can still be accepted even when a protocol response is delayed.
        before = client.slot.stat().st_mtime_ns
        subprocess.run(['xdotool', 'windowfocus', client.window, 'keydown', 'F5'], check=False)
        time.sleep(.5)
        subprocess.run(['xdotool', 'keyup', 'F5'], check=False)
        record['ordinaryF5Attempted'] = True
        for _ in range(20):
            if client.slot.stat().st_mtime_ns != before:
                record['saveChanged'] = True
                break
            time.sleep(.5)
    (client.output / 'live-timeout-handoff.json').write_text(json.dumps(record, indent=2) + '\n')
    print('LIVE CLIENT RETAINED', json.dumps(record), flush=True)
    # The wrapper owns Xvfb. Remain alive without further gameplay until the
    # coordinator inspects/resumes/stops this exact client.
    while client.process.poll() is None:
        time.sleep(1)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--runtime', type=Path, required=True)
    parser.add_argument('--continue-from', type=Path, required=True)
    parser.add_argument('--budget', type=int, default=1000)
    parser.add_argument('--recover-accepted', action='store_true')
    args = parser.parse_args()
    client = WorldClient.__new__(WorldClient)
    try:
        client.__init__(args.output.resolve(), 42, runtime_source=args.runtime.resolve(),
                        continuation=args.continue_from.resolve(), window_size='1024x720', scene='world-campaign')
        result = recover_accepted(client, args.continue_from.resolve()) if args.recover_accepted else verify(client, args.budget)
        (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
        print(json.dumps(result), flush=True)
        client.request('stop'); client.process.wait(timeout=15)
    except BaseException as error:
        result = dict(passed=False, error=str(error), errorType=type(error).__name__, rollback=False)
        if hasattr(client, 'journal'):
            if isinstance(error, TimeoutError) and client.process.poll() is None:
                retain_live_timeout(client, error)
            try:
                response = client.read('response.json')
                if hasattr(client, 'ui') and response and response['id'] == 'journey-' + str(client.sequence):
                    walk = getattr(client, 'travel_walk', None)
                    if walk is not None:
                        walk.checkpoint('failure-current-checkpoint')
                    else:
                        client.control_click('dungeon:save')
                    result['ordinaryCheckpoint'] = True
            except Exception as checkpoint_error:
                result['checkpointError'] = str(checkpoint_error)
            (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
        raise
    finally:
        if hasattr(client, 'journal') and hasattr(client, 'log'):
            client.close()


if __name__ == '__main__':
    main()
