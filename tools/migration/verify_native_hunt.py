#!/usr/bin/env python3
"""Observe a natural native hunt, finite remains recovery and exact continuation.

Only private client observations guide movement/aim. The final ordinary save is
read to audit the already observed animal's death/reward identity, never to find
animals or routes. No game state is injected.
"""
import argparse
import json
from pathlib import Path
from verify_native_journey import Client, Journey, DIRECTIONS, position, distance


class Hunt(Journey):
    def __init__(self, client):
        super().__init__(client)
        self.target = None
        self.last_target = None
        self.start = position(client.ui['position'])

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
        if self.destination:
            target, radius = self.destination
            if self.at != target and self.go([target]):
                return True
            self.destination = None
        frontiers = [p for p in self.known if self.open(p, True) and p != self.at and not self.visits[p]
            and any((p[0] + dx, p[1] + dy, p[2]) not in self.known for dx, dy, _ in DIRECTIONS)]
        # Explore east from the actual start. This is a direction of travel,
        # not knowledge of a hidden animal, landmark or terrain route.
        aim = self.start[0] + 100, self.start[1], self.start[2]
        for target in sorted(frontiers, key=lambda p: distance(p, aim) + distance(self.at, p) * .2):
            if self.approach(target):
                return True
        return False


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--seed', type=int, default=42)
    parser.add_argument('--budget', type=int, default=1200)
    args = parser.parse_args()
    client = Client(args.output.resolve(), args.seed, scene='world-campaign', window_size='1024x720')
    try:
        client.control_click('dungeon:class:1')
        if client.ui['nativeDungeon']['mapVisible']:
            client.key('Tab')
        hunt = Hunt(client)
        initial_xp = client.ui['nativeDungeon']['player'][0]['experience']
        for step in range(args.budget):
            hunt.observe()
            cache = next((c for c in hunt.state['worldCaches'] if c['id'] == 'wildlife-' + str(hunt.target)), None)
            if cache and hunt.player['experience'] > initial_xp:
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
            if step % 25 == 0:
                client.control_click('dungeon:save')
        raise AssertionError('Natural hunt action budget exhausted; no pass claimed')
    finally:
        client.close()


if __name__ == '__main__':
    main()
