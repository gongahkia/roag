#!/usr/bin/env python3
"""Observe a natural native hunt, finite remains recovery and exact continuation.

Only private client observations guide movement/aim. The final ordinary save is
read to audit the already observed animal's death/reward identity, never to find
animals or routes. No game state is injected.
"""
import argparse
import heapq
import json
from pathlib import Path
from verify_native_journey import Client, Journey, DIRECTIONS, position, distance


class Hunt(Journey):
    def __init__(self, client):
        super().__init__(client)
        self.target = None
        self.last_target = None
        self.start = position(client.ui['position'])
        self.initial_xp = client.ui['nativeDungeon']['player'][0]['experience']
        memory = client.continuation / 'hunt-knowledge.json' if client.continuation else None
        if memory and memory.exists():
            saved = json.loads(memory.read_text())
            self.target = saved['target']
            self.last_target = tuple(saved['lastTarget']) if saved['lastTarget'] else None
            self.start = tuple(saved['start'])
            self.initial_xp = saved.get('initialExperience', self.initial_xp)
            self.known = {position(c): c for c in saved['cells']}
            self.visits.update({tuple(v['position']): v['count'] for v in saved['visits']})

    def checkpoint(self):
        self.client.control_click('dungeon:save')
        (self.client.output / 'hunt-knowledge.json').write_text(json.dumps(dict(
            target=self.target, lastTarget=self.last_target, start=self.start, initialExperience=self.initial_xp,
            cells=list(self.known.values()),
            visits=[dict(position=p, count=n) for p, n in self.visits.items()]), indent=2) + '\n')

    def observe(self):
        if self.client.ui['nativeDungeon']['activeActor'] != self.client.ui['nativeDungeon']['controlledActor']:
            self.client.control_click('dungeon:ready')
        self.ui = self.client.ui
        self.state = self.ui['nativeDungeon']
        self.player = next(p for p in self.state['player'] if p['id'] == self.state['activeActor'])
        self.at = position(self.player['position'])
        assert self.player['life'] == 'Alive', 'The hunt ended in a casualty; no pass claimed'
        assert self.player['health'] >= self.player['maxHealth'] / 4, 'Health is low; preserve this ordinary journey for review'
        self.visible = {position(c): c for c in self.ui['cells']}
        self.known.update(self.visible)
        self.doors = set()
        self.blocked = set()
        self.fighters = [f for f in self.state['fighters'] if not f['friendly'] and f['health'] > 0]
        animals = [a for a in self.ui['actors'] if a.get('habitat')]
        if self.target is None and animals:
            chosen = min(animals, key=lambda a: ('Timid' in a['behaviour'], distance(self.at, position(a))))
            self.target = chosen['id']
            self.client.capture('01-observed-native-wildlife')
        chosen = next((a for a in animals if a['id'] == self.target), None)
        if chosen:
            self.last_target = position(chosen)
            fighter = next(f for f in self.state['fighters'] if f['id'] == self.target)
            if all(f['id'] != self.target for f in self.fighters):
                self.fighters.append(fighter)
        self.occupied = {position(a) for a in self.ui['actors']}
        self.occupied |= {position(p['position']) for p in self.state['player'] if p['id'] != self.player['id'] and p['life'] != 'Dead'}
        self.danger = {position(p) for t in self.state['telegraphs'] if t['due'] <= self.state['clock'] + 190 for p in t['cells']}

    def explore(self):
        if self.last_target and self.approach(self.last_target, 1):
            return True
        frontiers = {p for p in self.known if self.open(p, True) and p != self.at
            and any((p[0] + dx, p[1] + dy, p[2]) not in self.known for dx, dy, _ in DIRECTIONS)}
        # Find actual routes once. Manhattan estimates selected remote frontiers
        # behind barriers repeatedly in the earlier exploration attempts.
        costs, previous = {self.at: 0}, {self.at: None}
        queue = [(0, self.at)]
        while queue:
            cost, cell = heapq.heappop(queue)
            if costs[cell] != cost:
                continue
            for neighbor in self.neighbors(cell):
                if not self.open(neighbor, True):
                    continue
                candidate = cost + 1 + self.failed[neighbor] * 40
                if candidate < costs.get(neighbor, float('inf')):
                    costs[neighbor], previous[neighbor] = candidate, cell
                    heapq.heappush(queue, (candidate, neighbor))
        reachable = frontiers.intersection(costs)
        if not reachable:
            return False
        # East is an expedition direction, not hidden terrain/animal knowledge.
        # Keep travel costs in the score so an east-facing barrier does not send
        # the player back and forth between distant failed approaches.
        aim = 4095, self.start[1], self.start[2]
        target = min(reachable, key=lambda p: (distance(p, aim) + costs[p] * .4 + self.visits[p] * 3, p))
        while previous[target] != self.at:
            target = previous[target]
        return self.move(target)

    def fight(self):
        # A faster timid animal cannot be caught by repeatedly walking after
        # it. Use the Ranger's actual gap closer and five-tile volley, with
        # the same observed clear lanes and paid recovery as ordinary input.
        animal = next((a for a in self.ui['actors'] if a['id'] == self.target and not a['hostile']), None)
        if animal and self.at not in self.danger and self.player['classId'] == 'Ranger':
            target = position(animal)
            if self.at[0] == target[0] or self.at[1] == target[1]:
                dx = (target[0] > self.at[0]) - (target[0] < self.at[0])
                dy = (target[1] > self.at[1]) - (target[1] < self.at[1])
                length = distance(self.at, target)
                lane = [(self.at[0] + dx * n, self.at[1] + dy * n, self.at[2]) for n in range(1, length)]
                if all(self.open(p, True) for p in lane):
                    key = next(k for x, y, k in DIRECTIONS if (x, y) == (dx, dy))
                    action = 'r' if length == 5 else 'x' if 6 <= length <= 8 else None
                    if action:
                        before = self.state['clock']
                        self.client.key(action + '+' + key)
                        if self.client.ui['nativeDungeon']['clock'] > before:
                            return True
        return super().fight()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--seed', type=int, default=42)
    parser.add_argument('--budget', type=int, default=1200)
    parser.add_argument('--continue-from', type=Path)
    parser.add_argument('--runtime', type=Path)
    args = parser.parse_args()
    client = Client(args.output.resolve(), args.seed, scene='world-campaign', window_size='1024x720',
        continuation=args.continue_from.resolve() if args.continue_from else None,
        runtime_source=args.runtime.resolve() if args.runtime else None)
    hunt = None
    try:
        if not args.continue_from:
            client.control_click('dungeon:class:1')
        if client.ui['nativeDungeon']['mapVisible']:
            client.key('Tab')
        hunt = Hunt(client)
        for step in range(args.budget):
            if (client.output / 'stop-requested').exists():
                raise SystemExit('Probe stopped at an observation boundary; preserving its ordinary continuing save')
            if client.sequence >= 850:
                hunt.checkpoint()
                client.resume()
            hunt.observe()
            if step % 25 == 0:
                hunt.checkpoint()
            cache = next((c for c in hunt.state['worldCaches'] if c['id'] == 'wildlife-' + str(hunt.target)), None)
            if cache and hunt.player['experience'] > hunt.initial_xp:
                target = position(cache['position'])
                if distance(hunt.at, target) > 1:
                    assert hunt.approach(target, 1), 'No private route to the real remains'
                    continue
                client.request('tile', x=target[0], y=target[1])
                assert client.ui['panel'] == 'dungeon-world-loot'
                client.capture('02-actual-hunt-remains')
                offers = [c for c in client.ui['controls'] if c['id'].startswith('dungeon:world-loot:')]
                available = [c for c in offers if c['enabled']]
                assert available and all(c['sprite'] for c in offers)
                before = client.ui['nativeDungeon']['clock']
                inventory = client.ui['inventory'].copy()
                client.control_click(available[0]['id'])
                assert client.ui['nativeDungeon']['clock'] > before
                assert client.ui['inventory'] != inventory
                client.capture('03-paid-hunt-recovery')
                client.resume()
                saved = json.loads(client.slot.read_text())
                client.control_click('dungeon:save')
                assert saved == json.loads(client.slot.read_text())
                animal = next(a for a in saved['state']['actors'] if a['id'] == hunt.target)
                assert animal['life'] == 2 and hunt.target in saved['state']['rewardedEncounterIds']
                assert any(e['kind'] == 'ExperienceGained' and e['target'] == hunt.target and e['amount'] > 0
                           for e in saved['state']['tactics']['events']), 'No actual owned XP receipt for the observed hunt'
                assert sum(c['sourceActor'] == hunt.target and c['taken'] for c in saved['state']['loot']) == 1
                client.capture('04-exact-hunt-continuation')
                (client.output / 'result.json').write_text(json.dumps(dict(passed=True, target=hunt.target,
                    species=animal['archetype'], steps=step, clock=client.ui['nativeDungeon']['clock'],
                    checks=['natural private exploration', 'native owned hunt and authored contribution XP',
                            'one persistent dead animal', 'finite paid illustrated remains recovery', 'exact ordinary continuation'],
                    limits='One natural solo hunt, not long-duration ecology, boss/group spoils, or full campaign acceptance.'), indent=2) + '\n')
                return
            if hunt.fight():
                continue
            assert hunt.explore(), 'No reachable privately observed exploration frontier'
        raise AssertionError('Natural hunt action budget exhausted; no pass claimed')
    finally:
        try:
            if hunt and client.process.poll() is None:
                if client.sequence:
                    pending = 'journey-' + str(client.sequence)
                    client.until(lambda: (r if (r := client.read('response.json')) and r['id'] == pending else None))
                    client.ui = client.read('observation.json')['ui']
                hunt.checkpoint()
        finally:
            client.close()


if __name__ == '__main__':
    main()
