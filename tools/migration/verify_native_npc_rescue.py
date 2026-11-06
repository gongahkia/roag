#!/usr/bin/env python3
"""Muted natural recruitment, ordinary friendly fire and physical NPC rescue.

No actor, HP, item, position, outcome or save injection. The two controlled
members deliberately practice friendly fire in a safe room beside an existing
medicine carrier. This isolates rescue; hostile expedition danger remains a
separate rendered acceptance journey.
"""
import argparse
import json
from pathlib import Path

from verify_native_journey import Client, Journey, DIRECTIONS, distance, position
from verify_native_party import route


def step(client, path):
    at = position(client.ui['position']); target = path[0]
    key = next(k for dx, dy, k in DIRECTIONS if (at[0] + dx, at[1] + dy, at[2]) == target)
    client.key(key)


class TownSurvey(Journey):
    """Keep only observed town ground and people while actually walking."""
    def __init__(self, client):
        super().__init__(client)
        self.start = position(client.ui['position'])
        self.recruits = {}

    def observe(self):
        if (self.client.output / 'stop-requested').exists():
            raise SystemExit('Town survey stopped at an observation boundary')
        if self.client.sequence >= 800:
            exact_resume(self.client)
        if self.client.ui['nativeDungeon']['activeActor'] != self.client.ui['nativeDungeon']['controlledActor']:
            self.client.control_click('dungeon:ready')
        self.ui = self.client.ui
        self.state = self.ui['nativeDungeon']
        self.player = next(p for p in self.state['player'] if p['id'] == self.state['activeActor'])
        self.at = position(self.player['position'])
        assert self.player['life'] == 'Alive'
        self.visible = {position(c): c for c in self.ui['cells']}
        self.known.update(self.visible)
        self.recruits.update({r['actor']: r for r in self.state['recruits']})
        self.occupied = {position(a) for a in self.ui['actors']}
        self.occupied |= {position(p['position']) for p in self.state['player'] if p['id'] != self.player['id'] and p['life'] != 'Dead'}
        self.blocked = {p for p, c in self.known.items() if c['material'].startswith('Stairs')}
        self.doors = set()
        self.danger = {position(p) for t in self.state['telegraphs'] for p in t['cells']}

    def search(self):
        frontier = [p for p in self.known if self.open(p, True) and p != self.at and distance(p, self.start) <= 80
            and any(q not in self.known for q in self.neighbors(p))]
        frontier.sort(key=lambda p: (distance(p, self.at) + self.visits[p] * 8
            - (8 if self.known[p]['material'] == 'Floor' else 0), p))
        return any(self.go([p], True) for p in frontier)

    def find_pair(self):
        for _ in range(500):
            self.observe()
            medics = [r for r in self.recruits.values() if r['medicine'] > 0]
            pairs = [(m, r) for m in medics for r in self.recruits.values() if m['actor'] != r['actor']]
            if pairs:
                return min(pairs, key=lambda pair: distance(self.at, position(pair[1]['position'])))
            assert self.search(), 'No reachable observed town frontier for finding existing adventurers'
        raise AssertionError('No observed town medic/recruit pair within the walking budget')

    def rally(self, medic):
        for _ in range(400):
            self.observe()
            medic = self.recruits[medic['actor']]
            target = position(medic['position'])
            if any(r['actor'] == medic['actor'] and r['medicine'] > 0 for r in self.state['recruits']) \
                    and all(distance(position(p['position']), target) <= 3 for p in self.state['player']):
                return
            if distance(self.at, target) <= 2:
                self.client.key('space')
            else:
                assert self.go(self.neighbors(target), True), 'No remembered physical route toward the observed town medic'
        raise AssertionError('Party did not physically gather near the observed medicine carrier')


def verify(client, world_campaign=False):
    client.control_click('dungeon:class:1')
    survey = TownSurvey(client) if world_campaign else None
    if survey:
        medic, candidate = survey.find_pair()
        client.capture('observed-town-adventurers')
    else:
        recruits = client.ui['nativeDungeon']['recruits']
        pairs = [(medic, member) for medic in recruits if medic['medicine'] > 0
                 for member in recruits if member['actor'] != medic['actor']
                 and distance(position(medic['position']), position(member['position'])) <= 5]
        assert pairs, 'This seed has no nearby natural medicine carrier/recruit pair; no rescue was invented.'
        medic, candidate = min(pairs, key=lambda pair: (distance(position(pair[0]['position']), position(pair[1]['position'])), pair[1]['actor']))
    founder = client.ui['nativeDungeon']['activeActor']
    for _ in range(400 if survey else 40):
        if survey:
            survey.observe()
            candidate = survey.recruits[candidate['actor']]
        target = position(candidate['position']); at = position(client.ui['position'])
        if distance(at, target) == 1:
            client.request('tile', x=target[0], y=target[1]); client.capture('natural-recruit-before-rescue')
            client.control_click('dungeon:recruit'); break
        goals = {(target[0] + dx, target[1] + dy, target[2]) for dx, dy, _ in DIRECTIONS}
        if survey:
            assert survey.go(goals, True), 'No remembered physical route to the observed town recruit'
        else:
            path = route(client.ui, goals); assert path, 'No ordinary route to the recruit'
            step(client, path)
    else: raise AssertionError('Recruitment movement budget exhausted')
    victim = candidate['actor']
    assert len(client.ui['nativeDungeon']['player']) == 2
    if survey:
        survey.rally(medic)
        client.capture('party-walked-to-town-medic')
    for member in [founder, victim]:
        client.key('b'); client.control_click('dungeon:member:' + str(member))
        client.key('b'); client.control_click('dungeon:split'); client.control_click('dungeon:close')
    client.key('t')
    medicine = client.ui['inventory']['medicine']; shots = 0
    for _ in range(100):
        state = client.ui['nativeDungeon']; player = next(a for a in state['player'] if a['id'] == victim)
        if player['life'] == 'Downed': break
        assert player['life'] == 'Alive', 'The casualty died before rescue; checkpoint incomplete'
        if state['activeActor'] != founder: client.key('space'); continue
        at = position(client.ui['position']); target = position(player['position'])
        if distance(at, target) == 1:
            key = next(k for dx, dy, k in DIRECTIONS if (at[0] + dx, at[1] + dy, at[2]) == target)
            client.key('f+' + key); shots += 1
        else:
            goals = {(target[0] + dx, target[1] + dy, target[2]) for dx, dy, _ in DIRECTIONS}
            path = route(client.ui, goals); assert path, 'No clear ordinary position for deliberate friendly fire'
            step(client, path)
    else: raise AssertionError('No natural downing within the paid-input budget')
    state = client.ui['nativeDungeon']; player = next(a for a in state['player'] if a['id'] == victim)
    assert player['health'] == 0 and player['bleedTurns'] > 0
    noticed = [r for r in state['rescuers'] if r['medicine'] > 0 and r['activity']['target'] == victim]
    assert noticed, 'No existing NPC detected and accepted the casualty'
    client.capture('npc-notices-real-downed-companion'); exact_resume(client)
    for _ in range(20):
        player = next(a for a in client.ui['nativeDungeon']['player'] if a['id'] == victim)
        if player['life'] == 'Alive': break
        assert player['life'] == 'Downed', 'Rescue failed; death is not a passing result'
        client.key('space')
    else: raise AssertionError('NPC rescue did not complete')
    assert player['health'] == max(1, player['maxHealth'] // 4)
    assert client.ui['inventory']['medicine'] == medicine
    helpers = [r for r in client.ui['nativeDungeon']['rescuers'] if r['id'] in {r['id'] for r in noticed} and r['medicine'] == 0]
    assert helpers, 'The actual NPC did not spend their carried medicine'
    if not world_campaign:
        assert any(r['actor'] == helpers[0]['id'] for r in client.ui['nativeDungeon']['recruits'])
    client.capture('npc-paid-physical-revival'); exact_resume(client); client.capture('npc-rescue-exact-reload')
    clock = client.ui['nativeDungeon']['clock']
    client.control_click('dungeon:menu'); client.control_click('dungeon:settings')
    if client.ui['nativeDungeon']['presentation']['font'] != 'Sans': client.control_click('dungeon:setting:font')
    for _ in range(3):
        if client.ui['nativeDungeon']['presentation']['textSize'] == 'Large': break
        client.control_click('dungeon:setting:text-size')
    client.control_click('dungeon:close')
    helper = next(r for r in client.ui['nativeDungeon']['rescuers'] if r['id'] == helpers[0]['id'])
    client.request('tile', x=helper['position']['x'], y=helper['position']['y'])
    expected_panels = ('dungeon-recruit', 'dungeon-social') if world_campaign else ('dungeon-recruit',)
    assert client.ui['panel'] in expected_panels
    assert client.ui['nativeDungeon']['clock'] == clock
    client.capture('large-sans-existing-rescuer-inspection' if world_campaign else 'large-sans-existing-rescuer-recruitment')
    client.control_click('dungeon:close')
    (client.output / 'result.json').write_text(json.dumps(dict(npcRescueVerified=True, friendlyFireShots=shots,
        victim=victim, helper=helpers[0]['id'], clock=client.ui['nativeDungeon']['clock'],
        worldCampaign=world_campaign, limits='Natural town NPC medicine and two-member friendly-fire casualty; hostile expedition rescue remains pending.'
            if world_campaign else 'Natural safe-room NPC medicine and two-member friendly-fire casualty; hostile expedition rescue remains pending.'), indent=2) + '\n')


def exact_resume(client):
    client.resume()
    before = json.loads(client.slot.read_text())
    client.control_click('dungeon:save')
    assert before == json.loads(client.slot.read_text()), 'The ordinary rescue save changed without a paid action'


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    parser.add_argument('--seed', type=int, default=1)
    parser.add_argument('--world-campaign', action='store_true')
    parser.add_argument('--runtime', type=Path)
    args = parser.parse_args()
    client = Client(args.output.resolve(), args.seed, window_size='1024x720',
        scene='world-campaign' if args.world_campaign else None,
        runtime_source=args.runtime.resolve() if args.runtime else None)
    try: verify(client, args.world_campaign)
    finally: client.close()


if __name__ == '__main__': main()
