#!/usr/bin/env python3
"""Continue a real four-member dungeon journey using visible observations and ordinary input.

No state injection or rollback. A budget, defeat or missing acceptance item is an
incomplete result, never a pass. Audio stays muted through the shared Client.
"""
import argparse
import json
from pathlib import Path
from verify_native_journey import Client, Journey, DIRECTIONS, distance, position


class PartyJourney(Journey):
    def __init__(self, client, accept_damage=False):
        super().__init__(client)
        self.accept_damage = accept_damage
        self.rescued = 'player-medicine-rescue' in self.receipts
        self.seen_split = 'party-floor-split' in self.receipts
        self.claimed = {a['id'] for a in client.ui['nativeDungeon']['player'] if a['boons']} if 'party-chest' in self.receipts else set()
        self.boss_defeated = False
        self.previous_actor = None
        self.floor_frames = set()
        self.guarding_for_rescue = False
        self.last_reverse = -100

    def observe(self):
        self.ui = self.client.ui
        self.state = self.ui['nativeDungeon']
        self.player = next((a for a in self.state['player'] if a['id'] == self.state['activeActor']), self.state['player'][0])
        self.at = position(self.player['position'])
        for cell in self.ui['cells']: self.known[position(cell)] = cell
        self.visible = {position(c): c for c in self.ui['cells']}
        self.fighters = [f for f in self.state['fighters'] if not f['friendly'] and f['health'] > 0]
        self.occupied = {position(f['position']) for f in self.fighters}
        self.occupied |= {position(a['position']) for a in self.state['player'] if a['life'] != 'Dead' and a['id'] != self.player['id']}
        self.occupied |= {position(r['position']) for r in self.state['recruits']}
        self.danger = {position(c) for t in self.state['telegraphs'] if t['due'] <= self.state['clock'] + 190 for c in t['cells']}
        self.doors = {position(d['position']) for d in self.state['doors']}
        self.blocked = {position(c['position']) for c in self.state['chests'] if not c['claimed']}
        self.blocked |= {position(c['position']) for c in self.state['vaults'] if not c['opened']}
        self.blocked |= {position(g['position']) for g in self.state['gates'] if not g['open']}
        self.blocked |= {position(q['position']) for q in self.state['questNodes'] if q['node'] == 'Gate' and not q['open']}
        self.blocked |= {p for p, c in self.known.items() if c['material'] == 'StairsUp'}
        if self.previous_actor != self.player['id']: self.destination = None
        self.previous_actor = self.player['id']
        for gate in self.state.get('routeGates', []):
            self.exits[self.at[2]] = position(gate['position'])
            if gate['available']:
                self.ready_routes[self.at[2]] = position(gate['position'])
                self.boss_defeated = True
        for enemy in self.fighters:
            if self.enemies.get(enemy['class'], {}).get('ai') in ['warden', 'biome_elite', 'hollow_regent']:
                self.exits.setdefault(self.at[2], position(enemy['position']))
        living = [a for a in self.state['player'] if a['life'] != 'Dead']
        split = len({a['position']['floor'] for a in living}) > 1
        if split and not self.seen_split:
            self.seen_split = True
            self.receipts.add("party-floor-split")
            self.client.capture('physical-party-floor-split')
            self.checkpoint(); self.client.resume()
            self.observe()
        if self.at[2] not in self.floor_frames:
            self.floor_frames.add(self.at[2]); self.client.capture('party-floor-' + str(self.at[2]))

    def aligned(self, a, b):
        ranges = {'Ranger': 4, 'Arcanist': 3, 'Berserker': 1, 'Adventurer': 1, 'Geomancer': 3}
        if a[2] != b[2] or a[0] != b[0] and a[1] != b[1] or distance(a, b) > ranges.get(self.player['classId'], 1): return False
        dx, dy = (b[0] > a[0]) - (b[0] < a[0]), (b[1] > a[1]) - (b[1] < a[1])
        p = a[0] + dx, a[1] + dy, a[2]
        while p != b:
            c = self.known.get(p)
            if not c or not c['groundOpen'] or p in self.blocked or p in self.occupied: return False
            p = p[0] + dx, p[1] + dy, p[2]
        return True

    def move(self, p):
        at = self.at; acting = self.player['id']
        dx, dy = p[0] - at[0], p[1] - at[1]
        key = next(k for x, y, k in DIRECTIONS if (x, y) == (dx, dy))
        self.select_movement(); self.client.key(key)
        actual = position(next(a for a in self.client.ui['nativeDungeon']['player'] if a['id'] == acting)['position'])
        self.visits[p] += 1
        if actual == at: self.failed[p] += 1
        return True

    def rescue(self):
        targets = [a for a in self.state['player'] if a['life'] == 'Downed' and a['position']['floor'] == self.at[2]]
        if not targets: return False
        target = min(targets, key=lambda a: (a['bleedTurns'], distance(self.at, position(a['position']))))
        p = position(target['position'])
        if distance(self.at, p) == 1 and self.ui['inventory'].get('medicine', 0) > 0:
            self.client.capture('downed-member-before-rescue')
            key = next(k for dx, dy, k in DIRECTIONS if (self.at[0] + dx, self.at[1] + dy, self.at[2]) == p)
            medicine = self.ui['inventory']['medicine']
            self.client.key('h+' + key)
            restored = next(a for a in self.client.ui['nativeDungeon']['player'] if a['id'] == target['id'])
            assert restored['life'] == 'Alive', self.client.ui['notice']
            assert self.client.ui['inventory']['medicine'] == medicine - 1
            self.rescued = True; self.receipts.add('player-medicine-rescue')
            self.client.capture('member-rescued-in-combat'); self.checkpoint(); self.client.resume()
            return True
        return self.go(self.neighbors(p), True)

    def accept_visible_hits(self):
        if not self.accept_damage or self.rescued or not self.fighters: return False
        if len({a['position']['floor'] for a in self.state['player'] if a['life'] != 'Dead'}) != 1: return False
        self.guarding_for_rescue = True
        target = min((a for a in self.state['player'] if a['life'] == 'Alive'), key=lambda a: a['health'] / a['maxHealth'])
        p = position(target['position'])
        if self.player['id'] != target['id']:
            if distance(self.at, p) > 1 and self.go(self.neighbors(p), True): return True
            enemy = min(self.fighters, key=lambda e: distance(self.at, position(e['position'])))
            q = position(enemy['position']); dx, dy = q[0] - self.at[0], q[1] - self.at[1]
            if abs(dx) >= abs(dy): key = 'd' if dx >= 0 else 'a'
            else: key = 's' if dy >= 0 else 'w'
            self.client.key('g+' + key)
        else: self.client.key('space')
        return True

    def quiet_tasks(self):
        if self.destination:
            target, radius = self.destination
            if distance(self.at, target) > radius and self.go([target, *self.neighbors(target)] if radius else [target]): return True
            self.destination = None
        for drop in self.state['loot']:
            if drop['kind'] == 0 or drop['kind'] == 1 and self.player['health'] < self.player['maxHealth']:
                if self.approach(position(drop['position'])): return True
        for chest in self.state['chests']:
            if chest['claimed'] or chest['definition'] != 'core:chest/free_elite' and self.ui['inventory'].get('gold', 0) < 5: continue
            p = position(chest['position'])
            if distance(p, self.at) == 1 and p in self.visible:
                self.select_movement(); self.client.request('tile', x=p[0], y=p[1])
                if self.client.ui['panel'] == 'dungeon-chest-recipient':
                    assert self.client.ui['nativeDungeon']['chestOffers'] is None
                    self.client.capture('party-chest-' + str(chest['id']) + '-recipient')
                    self.client.control_click('dungeon:chest-recipient:' + str(self.player['id']))
                assert self.client.ui['panel'] == 'dungeon-chest', self.client.ui['notice']
                self.client.capture('party-chest-' + str(chest['id']) + '-offers')
                offers = self.client.ui['nativeDungeon']['chestOffers']
                priorities = ['vampiric', 'lifesteal', 'mending', 'regeneration', 'second_wind', 'heart', 'damage']
                choice = min(range(3), key=lambda i: next((n for n, term in enumerate(priorities) if term in offers[i]['boon']), len(priorities)))
                before = {a['id']: a['boons'] for a in self.state['player']}
                self.client.control_click('dungeon:claim:' + str(choice + 1))
                after = self.client.ui['nativeDungeon']['player']
                assert all(a['boons'] == before[a['id']] for a in after if a['id'] != self.player['id'])
                self.claimed.add(self.player['id']); self.receipts.add('party-chest')
                self.client.capture('party-chest-' + str(chest['id']) + '-claimed')
                self.checkpoint(); self.client.resume(); return True
            if self.approach(p, 1): return True
        return False

    def reverse_group(self):
        if self.fighters or self.steps - self.last_reverse < 20: return False
        others = [a for a in self.state['player'] if a['life'] == 'Alive' and a['id'] != self.player['id']
            and a['position']['floor'] == self.at[2] and distance(self.at, position(a['position'])) <= 6]
        if not others: return False
        # The party blocks a return corridor. Choosing its other end as leader
        # reverses direction without moving, swapping or passing through anyone.
        rear = max(others, key=lambda a: distance(self.at, position(a['position'])))
        clock = self.state['clock']; active = self.state['activeActor']; players = self.state['player']
        self.client.control_click('dungeon:member:' + str(rear['id']))
        self.client.control_click('dungeon:party'); self.client.control_click('dungeon:regroup')
        self.client.control_click('dungeon:lead-selected')
        now = self.client.ui['nativeDungeon']
        assert now['clock'] == clock and now['activeActor'] == active and now['player'] == players
        self.client.capture('party-corridor-reversal-' + str(self.steps))
        self.client.control_click('dungeon:close'); self.client.key('t'); self.client.key('space')
        self.last_reverse = self.steps; self.receipts.add('paid-corridor-reversal')
        return True

    def run_party(self, budget):
        if self.client.ui['nativeDungeon']['mapVisible']: self.client.key('Tab')
        self.client.capture('party-journey-start')
        for self.steps in range(1, budget + 1):
            self.observe()
            if (self.client.output / 'pause-request').exists():
                self.checkpoint(); self.client.capture('party-continuation-boundary'); return 'paused'
            if not any(a['life'] == 'Alive' for a in self.state['player']):
                self.client.capture('party-terminal'); self.checkpoint(); return 'defeat'
            if self.boss_defeated and self.rescued and self.seen_split and self.claimed:
                self.client.capture('party-boss-rescue-split-chest'); self.checkpoint(); self.client.resume(); return 'journey-verified'
            if self.client.sequence > 820: self.checkpoint(); self.client.resume(); self.observe()
            if self.steps % 50 == 0:
                self.client.capture('party-progress-' + str(self.steps)); self.checkpoint()
            if self.rescue() or self.accept_visible_hits() or self.fight() or self.quiet_tasks() or self.explore(): continue
            if self.reverse_group(): continue
            # Wait for another member's original readiness when occupied bodies block a route.
            self.client.key('space')
        self.client.capture('party-action-budget'); self.checkpoint(); return 'action-budget'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--continue-from', type=Path, required=True)
    parser.add_argument('--runtime', type=Path)
    parser.add_argument('--budget', type=int, default=2500)
    parser.add_argument('--accept-damage-for-rescue', action='store_true')
    args = parser.parse_args()
    client = Client(args.output.resolve(), 37, args.runtime.resolve() if args.runtime else None,
        continuation=args.continue_from.resolve(), window_size='1280x800')
    journey = PartyJourney(client, args.accept_damage_for_rescue)
    try:
        outcome = journey.run_party(args.budget)
        result = dict(outcome=outcome, partyJourneyVerified=outcome == 'journey-verified', steps=journey.steps,
            rescued=journey.rescued, splitFloors=journey.seen_split, bossDefeated=journey.boss_defeated,
            chestOwners=sorted(journey.claimed), clock=client.ui['nativeDungeon']['clock'],
            limits='Distinct/duplicate nonempty boon ownership and final packaged verification still require separate auditing.')
        (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
        print(json.dumps(result), flush=True)
    finally: client.close()


if __name__ == '__main__': main()
