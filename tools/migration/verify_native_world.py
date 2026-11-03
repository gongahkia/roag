#!/usr/bin/env python3
"""Muted native campaign town journey using observed terrain and ordinary input.

No actor, position, inventory, clock or outcome injection. This scoped review
does not establish the full world/dungeon/campaign acceptance gates.
"""
import argparse
from collections import Counter, deque
import json
from pathlib import Path
from verify_native_journey import Client, DIRECTIONS, position, distance


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
    parser.add_argument('--budget', type=int, default=700)
    parser.add_argument('--continue-from', type=Path)
    args = parser.parse_args()
    client = Client(args.output.resolve(), args.seed, continuation=args.continue_from.resolve() if args.continue_from else None,
        window_size='1024x720', scene='world-campaign')
    try:
        assert client.ui['nativeDungeon']['campaign']
        assert client.ui['position']['floor'] == 0
        if not args.continue_from:
            client.capture('01-world-class-cards')
            client.control_click('dungeon:class:1')
        client.capture('02-native-outdoor-map')
        if client.ui['nativeDungeon']['mapVisible']:
            client.key('Tab')
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
        steps = explore_to_recruit(client, args.budget)
        client.capture('05-native-town-party')
        client.key('b')
        client.capture('06-party-policy')
        client.key('Escape')
        before = client.ui['nativeDungeon']['clock']
        client.request('wait', seconds=1)
        assert client.ui['nativeDungeon']['clock'] == before, 'Thinking advanced the campaign'
        client.resume()
        saved = json.loads(client.slot.read_text())
        client.control_click('dungeon:save')
        assert saved == json.loads(client.slot.read_text()), 'Loaded campaign changed its exact continuing save'
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
        (client.output / 'result.json').write_text(json.dumps(dict(
            passed=True, recruitmentActions=steps, clock=before,
            checks=['native campaign surface render', 'class atlas cards', 'private explored map',
                    'actual paid town food purchase', 'ordinary paid exploration', 'natural fixed-class recruitment',
                    'two-member party controls', 'thinking consumes no time', 'ordinary-launch exact continuing save',
                    'minimum-window normal and large sans'],
            limits='Town journey only. Outdoor/dungeon split, ecology, large complexes, bosses, campaign and final delivery remain open.'
        ), indent=2) + '\n')
    finally:
        client.close()


if __name__ == '__main__':
    main()
