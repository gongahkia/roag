#!/usr/bin/env python3
"""Muted native campaign town journey using observed terrain and ordinary input.

No actor, position, inventory, clock or outcome injection. This scoped review
does not establish the full world/dungeon/campaign acceptance gates.
"""
import argparse
from collections import Counter, deque
import json
import subprocess
from pathlib import Path
from verify_native_journey import Client, DIRECTIONS, position, distance


class WorldClient(Client):
    def until(self, check):
        # Record the normal 45-second deadline. Keep observing the same client
        # and pending input during a bounded grace period; never restart/replay.
        for attempt in range(4):
            try:
                return super().until(check)
            except TimeoutError as error:
                record = dict(error=str(error), deadlineSeconds=45, attempt=attempt + 1,
                              clientPid=self.process.pid, exitCode=self.process.poll(), replayed=False)
                with (self.output / 'observation-timeouts.jsonl').open('a') as stream:
                    stream.write(json.dumps(record) + '\n')
                print('observation-timeout', json.dumps(record), flush=True)
                subprocess.run(['xdotool', 'keyup', 'w', 'a', 's', 'd', 'f', 'q', 'e', 'z', 'c', 'space'], check=False)
                if attempt == 3:
                    raise


def profile_page(client, label):
    before = client.ui['nativeDungeon']['clock']
    progress = client.ui['nativeDungeon']['profile']
    assert progress is not None, 'Real isolated native campaign did not attach a profile'
    client.control_click('dungeon:menu')
    client.control_click('dungeon:profile')
    assert client.ui['panel'] == 'dungeon-profile'
    client.capture(label)
    client.control_click('dungeon:close')
    assert before == client.ui['nativeDungeon']['clock'], 'Profile inspection consumed time'
    assert progress == client.ui['nativeDungeon']['profile']


def saved_complexes(client, label):
    # Audit saved receipts only. Hidden saved geometry never guides navigation.
    manifest = json.loads(client.slot.read_text())
    state = manifest['state']
    complexes = state['nativeCampaign']['complexes']
    assert state['dungeon'] is None
    assert len(complexes) == 4 and all(c['populated'] and len(c['run']['floors']) == 19 for c in complexes)
    progress = state['tactics']['profile']
    assert progress is not None and progress['run'] == client.ui['nativeDungeon']['profile']['run']
    assert state['tactics']['controlledActor'] == client.ui['nativeDungeon']['controlledActor']
    assert position(state['position']) == position(client.ui['position'])
    proof = dict(profile=progress['profile'], run=progress['run'],
                 clock=state['tactics']['timeline']['clock'], controlledActor=state['tactics']['controlledActor'],
                 party=state['party'], complexes=[dict(id=c['parcel']['id'], populated=c['populated'],
                     floors=len(c['run']['floors']), bosses=sum(f['boss'] > 0 for f in c['run']['floors'])) for c in complexes])
    (client.output / (label + '.json')).write_text(json.dumps(proof, indent=2) + '\n')
    return proof


def explore_to_recruit(client, budget):
    known = {}
    visits = Counter()
    town_origin = position(client.ui['position'])
    destination = None
    if client.continuation and (client.continuation / 'world-knowledge.json').exists():
        memory = json.loads((client.continuation / 'world-knowledge.json').read_text())
        known = {tuple(cell['position']): cell['open'] for cell in memory['known']}
        visits = Counter({tuple(cell['position']): cell['count'] for cell in memory['visits']})
        town_origin = tuple(memory['townOrigin'])
    for step in range(budget):
        ui = client.ui
        assert not ui['nativeDungeon']['victory']
        assert ui['nativeDungeon']['player'][0]['health'] > 0, 'Journey ended in defeat'
        origin = position(ui['position'])
        visits[origin] += 1
        for cell in ui['cells']:
            known[position(cell)] = cell['groundOpen']
        if step % 25 == 0:
            client.control_click('dungeon:save')
            (client.output / 'world-knowledge.json').write_text(json.dumps(dict(
                townOrigin=town_origin,
                known=[dict(position=p, open=value) for p, value in known.items()],
                visits=[dict(position=p, count=value) for p, value in visits.items()]
            ), indent=2) + '\n')
        blocked = {position(a) for a in ui['actors']} | {
            position(a['position']) for a in ui['nativeDungeon']['player']
            if position(a['position']) != origin}
        recruits = ui['nativeDungeon']['recruits']
        adjacent = next((r for r in recruits if distance(origin, position(r['position'])) == 1), None)
        if adjacent:
            client.request('actor', actor=adjacent['actor'])
            assert client.ui['panel'] == 'dungeon-recruit'
            client.capture('04-fixed-adventurer')
            clock = client.ui['nativeDungeon']['clock']
            client.control_click('dungeon:recruit')
            assert len(client.ui['nativeDungeon']['player']) == 2
            assert client.ui['nativeDungeon']['clock'] >= clock
            assert next(p for p in client.ui['nativeDungeon']['player'] if p['id'] == adjacent['actor'])['classId'] == adjacent['classId']
            return step + 1
        previous = {origin: None}
        queue = deque([origin])
        while queue:
            p = queue.popleft()
            for dx, dy, key in DIRECTIONS:
                n = (p[0] + dx, p[1] + dy, p[2])
                if n in previous or not known.get(n) or n in blocked:
                    continue
                previous[n] = (p, key)
                queue.append(n)
        goals = [p for p in previous if p != origin and any(distance(p, position(r['position'])) == 1 for r in recruits)]
        if goals:
            goal = min(goals, key=lambda p: distance(origin, p))
        elif destination in previous and destination != origin:
            goal = destination
        else:
            frontiers = [p for p in previous if p != origin and not visits[p]
                and max(abs(town_origin[0] - p[0]), abs(town_origin[1] - p[1])) <= 28 and any(
                (p[0] + dx, p[1] + dy, p[2]) not in known for dx, dy, _ in DIRECTIONS)]
            assert frontiers, 'No observed reachable frontier; inspect the saved journey'
            goal = min(frontiers, key=lambda p: visits[p] * 5 + distance(town_origin, p) * .5 + distance(origin, p) * .1)
        destination = goal
        cursor = goal
        while previous[cursor][0] != origin:
            cursor = previous[cursor][0]
        client.key(previous[cursor][1])
    raise AssertionError('Recruitment journey exhausted its action budget; not a pass')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--seed', type=int, default=42)
    parser.add_argument('--runtime', type=Path)
    parser.add_argument('--budget', type=int, default=700)
    parser.add_argument('--continue-from', type=Path)
    parser.add_argument('--market-only', action='store_true')
    parser.add_argument('--large-text', action='store_true')
    args = parser.parse_args()
    client = WorldClient.__new__(WorldClient)
    try:
        client.__init__(args.output.resolve(), args.seed, runtime_source=args.runtime.resolve() if args.runtime else None,
            continuation=args.continue_from.resolve() if args.continue_from else None,
            window_size='1024x720', scene='world-campaign')
        assert client.ui['nativeDungeon']['campaign']
        assert client.ui['position']['floor'] == 0
        if not args.continue_from:
            client.capture('01-world-class-cards')
            client.control_click('dungeon:class:1')
        client.capture('02-native-outdoor-map')
        profile_page(client, '02b-free-permanent-profile')
        if client.ui['nativeDungeon']['mapVisible']:
            client.key('Tab')
        if args.large_text:
            client.control_click('dungeon:menu')
            client.control_click('dungeon:settings')
            if client.ui['nativeDungeon']['presentation']['font'] != 'Sans':
                client.control_click('dungeon:setting:font')
            for _ in range(3):
                if client.ui['nativeDungeon']['presentation']['textSize'] == 'Large':
                    break
                client.control_click('dungeon:setting:text-size')
            client.control_click('dungeon:close')
        client.control_click('dungeon:menu')
        client.control_click('dungeon:trade')
        buy = next(c for c in client.ui['controls'] if c['id'] == 'dungeon:trade-do:Buy:food')
        assert buy['enabled'], buy['reason']
        food = client.ui['inventory'].get('food', 0)
        gold = client.ui['inventory'].get('gold', 0)
        clock = client.ui['nativeDungeon']['clock']
        client.capture('03-actual-town-market')
        client.control_click(buy['id'])
        assert client.ui['inventory']['food'] == food + 1
        assert client.ui['inventory']['gold'] < gold
        assert client.ui['nativeDungeon']['clock'] > clock
        client.control_click('dungeon:save')
        initial_world = saved_complexes(client, '03b-populated-complex-save')
        if args.market_only:
            client.capture('04-paid-dawn-market')
            before = client.ui['nativeDungeon']['clock']
            client.request('wait', seconds=1)
            assert client.ui['nativeDungeon']['clock'] == before
            client.resume()
            saved = json.loads(client.slot.read_text())
            client.control_click('dungeon:save')
            assert saved == json.loads(client.slot.read_text())
            assert saved['state']['nativeCampaign']['startedAt'] == 6 * 300
            assert client.ui['nativeDungeon']['calendar'].startswith('Day 1 / 06:00')
            client.capture('05-exact-dawn-market-continuation')
            (client.output / 'result.json').write_text(json.dumps(dict(passed=True, clock=before,
                checks=['native dawn class choice and market', 'finite paid food purchase',
                        'thinking consumes no time', 'exact ordinary continuing save'],
                limits='One rendered morning market; night closing/calendar boundaries are backend evidence, not a full living-world journey.'), indent=2) + '\n')
            return
        steps = explore_to_recruit(client, args.budget)
        client.capture('05-native-town-party')
        client.key('b')
        client.capture('06-party-policy')
        client.key('Escape')
        inspect_clock = client.ui['nativeDungeon']['clock']
        other = client.ui['nativeDungeon']['player'][1]['id']
        client.key('2')
        assert client.ui['nativeDungeon']['controlledActor'] == other
        assert client.ui['nativeDungeon']['clock'] == inspect_clock
        client.capture('06b-private-second-member-view')
        client.key('b')
        client.capture('06c-second-member-party-policy')
        client.key('Escape')
        before = client.ui['nativeDungeon']['clock']
        client.request('wait', seconds=1)
        assert client.ui['nativeDungeon']['clock'] == before, 'Thinking advanced the campaign'
        client.resume()
        saved = json.loads(client.slot.read_text())
        client.control_click('dungeon:save')
        assert saved == json.loads(client.slot.read_text()), 'Loaded campaign changed its exact continuing save'
        continued = saved_complexes(client, '07b-populated-complex-continuation')
        assert continued['profile'] == initial_world['profile'] and continued['run'] == initial_world['run']
        assert continued['complexes'] == initial_world['complexes']
        assert client.ui['nativeDungeon']['controlledActor'] == other
        client.capture('07-exact-world-continuation')
        client.control_click('dungeon:menu')
        client.control_click('dungeon:settings')
        if client.ui['nativeDungeon']['presentation']['font'] != 'Sans':
            client.control_click('dungeon:setting:font')
        for _ in range(3):
            if client.ui['nativeDungeon']['presentation']['textSize'] == 'Large':
                break
            client.control_click('dungeon:setting:text-size')
        client.control_click('dungeon:close')
        client.capture('08-large-sans-world')
        profile_page(client, '08b-large-free-permanent-profile')
        (client.output / 'result.json').write_text(json.dumps(dict(
            passed=True, recruitmentActions=steps, clock=before,
            checks=['native campaign surface render', 'class atlas cards', 'private explored map',
                    'actual paid town food purchase', 'ordinary paid exploration', 'natural fixed-class recruitment',
                    'two-member party controls and private selected-member pages', 'free permanent-profile pages',
                    'four populated nineteen-floor complexes and profile identity preserved in continuing save',
                    'thinking consumes no time', 'ordinary-launch exact continuing save',
                    'minimum-window normal and large sans'],
            limits='Town journey only. Outdoor/dungeon split, ecology, large complexes, bosses, campaign and final delivery remain open.'
        ), indent=2) + '\n')
        client.request('stop'); client.process.wait(timeout=15)
    except BaseException as error:
        result = dict(passed=False, error=str(error), errorType=type(error).__name__, replayed=False)
        if hasattr(client, 'journal'):
            try:
                response = client.read('response.json')
                if hasattr(client, 'ui') and response and response['id'] == 'journey-' + str(client.sequence):
                    client.control_click('dungeon:save')
                    client.capture('failure-current-checkpoint')
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
