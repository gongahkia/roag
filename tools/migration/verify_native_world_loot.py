#!/usr/bin/env python3
"""Continue an ordinary native campaign toward an observed cache, without state injection."""
import argparse
from collections import deque
import json
from pathlib import Path
from verify_native_journey import Client, DIRECTIONS, position, distance


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--continue-from', type=Path, required=True)
    parser.add_argument('--budget', type=int, default=180)
    args = parser.parse_args()
    previous_run = args.continue_from.resolve()
    client = Client(args.output.resolve(), 42, continuation=previous_run, window_size='1024x720')
    known = {}
    memory = previous_run / 'world-knowledge.json'
    if memory.exists():
        known = {tuple(c['position']): c['open'] for c in json.loads(memory.read_text())['known']}
    tried = set()
    visits = {}
    destination = None
    try:
        if client.ui['nativeDungeon']['mapVisible']:
            client.key('Tab')
        for step in range(args.budget):
            ui = client.ui
            assert all(p['health'] > 0 for p in ui['nativeDungeon']['player']), 'A party member fell; inspect the journey'
            if ui['nativeDungeon']['activeActor'] != ui['nativeDungeon']['controlledActor']:
                client.control_click('dungeon:ready')
                ui = client.ui
            origin = position(ui['position'])
            visits[origin] = visits.get(origin, 0) + 1
            for cell in ui['cells']:
                known[position(cell)] = cell['groundOpen']
            caches = [c for c in ui['nativeDungeon']['worldCaches'] if c['id'] not in tried]
            near = next((c for c in caches if distance(origin, position(c['position'])) <= 1), None)
            if near:
                p = near['position']
                client.request('tile', x=p['x'], y=p['y'])
                assert client.ui['panel'] == 'dungeon-world-loot'
                offers = [c for c in client.ui['controls'] if c['id'].startswith('dungeon:world-loot:')]
                available = [c for c in offers if c['enabled']]
                if not available:
                    client.capture('unavailable-' + str(len(tried)))
                    tried.add(near['id'])
                    client.control_click('dungeon:close')
                    continue
                assert all(c['sprite'] for c in offers)
                client.capture('01-actual-world-cache')
                before = client.ui['nativeDungeon']['clock']
                inventory = client.ui['inventory'].copy()
                client.control_click(available[0]['id'])
                assert client.ui['nativeDungeon']['clock'] > before
                assert client.ui['inventory'] != inventory
                client.capture('02-paid-world-recovery')
                client.resume()
                saved = json.loads(client.slot.read_text())
                client.control_click('dungeon:save')
                assert saved == json.loads(client.slot.read_text())
                (client.output / 'result.json').write_text(json.dumps(dict(passed=True,
                    cache=near['id'], steps=step, clock=client.ui['nativeDungeon']['clock'],
                    limits='One natural world cache and exact continuation; not the full ecology/dungeon/campaign journey.'), indent=2) + '\n')
                return
            blocked = {position(a) for a in ui['actors']} | {position(a['position']) for a in ui['nativeDungeon']['player']
                if position(a['position']) != origin}
            targets = [(c['id'], position(c['position'])) for c in caches]
            targets += [(d['id'], position(d)) for d in ui['discoveries'] if d['id'] not in tried and d['floor'] == origin[2]]
            targets.sort(key=lambda pair: distance(origin, pair[1]))
            parents = {origin: None}
            queue = deque([origin])
            while queue:
                p = queue.popleft()
                for dx, dy, key in DIRECTIONS:
                    n = (p[0] + dx, p[1] + dy, p[2])
                    if n in parents or not known.get(n) or n in blocked:
                        continue
                    parents[n] = (p, key)
                    queue.append(n)
            goal = None
            for identity, p in targets:
                if distance(origin, p) <= 1:
                    tried.add(identity)
                    continue
                goals = [cell for cell in parents if distance(cell, p) <= 1]
                if goals:
                    goal = min(goals, key=lambda cell: distance(origin, cell))
                    break
            # A spoken landmark is legitimate navigation knowledge, not a map
            # of unseen ground. Walk observed frontiers toward it, keeping a
            # destination until arrival rather than oscillating each step.
            if goal is None and destination in parents and destination != origin:
                goal = destination
            if goal is None and targets:
                frontiers = [cell for cell in parents if cell != origin and not visits.get(cell)
                    and any((cell[0] + dx, cell[1] + dy, cell[2]) not in known for dx, dy, _ in DIRECTIONS)]
                if frontiers:
                    goal = min(frontiers, key=lambda cell: min(distance(cell, p) for _, p in targets)
                        + distance(origin, cell) * .2)
            assert goal is not None, 'No observed reachable cache/discovery route; no pass claimed'
            destination = goal
            cursor = goal
            while parents[cursor][0] != origin:
                cursor = parents[cursor][0]
            client.key(parents[cursor][1])
            if step % 20 == 0:
                client.control_click('dungeon:save')
        raise AssertionError('World cache journey exhausted its action budget; not a pass')
    finally:
        client.close()


if __name__ == '__main__':
    main()
