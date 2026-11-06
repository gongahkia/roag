#!/usr/bin/env python3
"""Continue an actual native party through paid rumour and free expedition journal.

Navigate only observed terrain and actors. Read saves after input for verification;
never inject gameplay state. Physical entrance travel is a separate continuation.
"""
import argparse
from collections import Counter, deque
import json
from pathlib import Path

from verify_native_journey import DIRECTIONS, distance, position
from verify_native_world import WorldClient, saved_complexes


class Walk:
    def __init__(self, client):
        self.client = client
        self.known = {}
        self.visits = Counter()
        self.home = position(client.ui['position'])
        previous = client.continuation / 'world-knowledge.json'
        if previous.exists():
            data = json.loads(previous.read_text())
            self.home = tuple(data['townOrigin'])
            self.known = {tuple(c['position']): c['open'] for c in data['known']}
            self.visits = Counter({tuple(c['position']): c['count'] for c in data['visits']})
        self.observe()

    def observe(self):
        self.ui = self.client.ui
        self.native = self.ui['nativeDungeon']
        self.at = position(self.ui['position'])
        self.visible = {position(c): c for c in self.ui['cells']}
        for p, c in self.visible.items():
            self.known[p] = c['groundOpen']
        self.visits[self.at] += 1
        self.blocked = {position(a) for a in self.ui['actors']}
        self.blocked |= {position(a['position']) for a in self.native['player'] if a['id'] != self.native['controlledActor']}

    def route(self, goals):
        previous = {self.at: None}
        queue = deque([self.at])
        goals = set(goals)
        while queue:
            p = queue.popleft()
            if p in goals and p != self.at:
                while previous[p][0] != self.at:
                    p = previous[p][0]
                return previous[p][1]
            for dx, dy, key in DIRECTIONS:
                q = p[0] + dx, p[1] + dy, p[2]
                if q not in previous and self.known.get(q) and q not in self.blocked:
                    previous[q] = p, key
                    queue.append(q)
        return None

    def step(self, goals):
        key = self.route(goals)
        if key:
            before = self.native['clock']
            self.client.key(key)
            assert self.client.ui['nativeDungeon']['clock'] > before, 'Observed navigation did not commit an action'
            return True
        return False

    def neighbors(self, p):
        return [(p[0] + dx, p[1] + dy, p[2]) for dx, dy, _ in DIRECTIONS]

    def checkpoint(self, label):
        self.client.control_click('dungeon:save')
        self.observe()
        (self.client.output / 'world-knowledge.json').write_text(json.dumps(dict(
            townOrigin=self.home,
            known=[dict(position=p, open=value) for p, value in self.known.items()],
            visits=[dict(position=p, count=value) for p, value in self.visits.items()]), indent=2) + '\n')
        (self.client.output / 'handoff.json').write_text(json.dumps(dict(
            position=self.ui['position'], clock=self.native['clock'], player=self.native['player'], rollback=False)) + '\n')
        self.client.capture(label)


def exact(client, label):
    client.control_click('dungeon:save')
    before = json.loads(client.slot.read_text())
    (client.output / (label + '-campaign.json')).write_text(json.dumps(before, indent=2) + '\n')
    client.resume()
    client.control_click('dungeon:save')
    assert before == json.loads(client.slot.read_text()), 'The complete continuing manifest changed on ordinary reload'
    return before['state']


def finish_dialogue(client):
    clock = client.ui['nativeDungeon']['clock']
    for _ in range(40):
        if client.ui['nativeDungeon']['social']['finished']:
            assert client.ui['nativeDungeon']['clock'] == clock
            return
        client.control_click('dungeon:social-next')
    raise AssertionError('Conversation did not reach its ordinary choice boundary')


def rumour(client, walk, budget):
    for step in range(budget):
        walk.observe()
        assert all(p['health'] > 0 for p in walk.native['player']), 'A traveller fell before the rumour'
        recruits = {r['actor'] for r in walk.native['recruits']}
        people = [a for a in walk.ui['actors'] if a['kind'] == 'Person' and not a['hostile'] and a['id'] not in recruits]
        adjacent = next((a for a in people if distance(walk.at, position(a)) == 1), None)
        if adjacent:
            client.request('actor', actor=adjacent['id'])
            assert client.ui['panel'] == 'dungeon-social'
            finish_dialogue(client)
            choices = [c for c in client.ui['controls'] if c['id'].startswith('dungeon:social:')
                       and ('rumour' in c['id'].lower() or 'rumour' in c['label'].lower()) and c['enabled']]
            assert choices, 'Observed resident did not offer an enabled ordinary rumour'
            client.capture('03-resident-rumour-offer')
            before = client.ui['nativeDungeon']['clock']
            client.control_click(choices[0]['id'])
            assert client.ui['nativeDungeon']['clock'] > before, 'Rumour did not consume its actual action'
            finish_dialogue(client)
            client.capture('04-paid-resident-rumour')
            client.control_click('dungeon:close')
            return step + 1
        if people and walk.step([p for a in people for p in walk.neighbors(position(a))]):
            continue
        if distance(walk.at, walk.home) > 4 and walk.step([walk.home]):
            continue
        frontiers = [p for p, open_ in walk.known.items() if open_ and p != walk.at and p[2] == walk.at[2]
                     and max(abs(p[0] - walk.home[0]), abs(p[1] - walk.home[1])) <= 28
                     and any(n not in walk.known for n in walk.neighbors(p))]
        frontiers.sort(key=lambda p: distance(walk.at, p) + walk.visits[p] * 5)
        assert any(walk.step([p]) for p in frontiers), 'No observed route to a resident'
        if step % 25 == 0:
            walk.checkpoint('resident-search-' + str(step))
    raise AssertionError('Natural resident approach exhausted its action budget')


def journal(client, label):
    before = client.ui['nativeDungeon']['clock']
    controlled = client.ui['nativeDungeon']['controlledActor']
    client.control_click('dungeon:menu')
    client.control_click('dungeon:journal')
    assert client.ui['panel'] == 'dungeon-journal'
    assert client.ui['expeditions']['entrances'], 'The paid rumour did not report a labyrinth entrance'
    reports = client.ui['expeditions']['entrances']
    client.capture(label)
    next_ = next(c for c in client.ui['controls'] if c['id'] == 'dungeon:journal-next')
    if next_['enabled']:
        client.control_click(next_['id'])
        client.capture(label + '-next')
        client.control_click('dungeon:journal-prev')
    client.control_click('dungeon:close')
    assert before == client.ui['nativeDungeon']['clock']
    assert controlled == client.ui['nativeDungeon']['controlledActor']
    return reports


def verify(client, budget):
    starting = exact(client, '00-original-continuation')
    identity = starting['tactics']['profile']
    party = starting['party']
    assert len(party) == 2 and not starting['awaitingContinuation']
    client.capture('01-original-two-member-continuation')
    client.key('t')
    assert client.ui['nativeDungeon']['controlledActor'] == client.ui['nativeDungeon']['activeActor']
    walk = Walk(client)
    steps = rumour(client, walk, budget)
    reports = journal(client, '05-normal-expedition-journal')
    client.control_click('dungeon:menu')
    client.control_click('dungeon:settings')
    while client.ui['nativeDungeon']['presentation']['font'] != 'Sans':
        client.control_click('dungeon:setting:font')
    while client.ui['nativeDungeon']['presentation']['textSize'] != 'Large':
        client.control_click('dungeon:setting:text-size')
    client.control_click('dungeon:close')
    assert reports == journal(client, '06-large-expedition-journal')
    final = exact(client, '07-rumour-exact')
    assert final['party'] == party and final['tactics']['profile']['profile'] == identity['profile']
    assert final['tactics']['profile']['run'] == identity['run']
    proof = saved_complexes(client, '08-rumour-complex-profile-proof')
    assert any(c.get('knownEntrance') for c in final['nativeCampaign']['complexes'])
    walk.checkpoint('09-rumour-continuation-ready')
    (client.output / 'reported-entrances.json').write_text(json.dumps(reports, indent=2) + '\n')
    return dict(passed=True, scope='natural paid resident rumour / free normal-large journal / exact two-member continuation',
                approachActions=steps, clock=client.ui['nativeDungeon']['clock'], reports=reports,
                profile=proof['profile'], run=proof['run'], party=party,
                limits='Reported coordinates and saved populated complexes only; no physical entrance travel, split-floor journey, victory or package acceptance.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--runtime', type=Path, required=True)
    parser.add_argument('--continue-from', type=Path, required=True)
    parser.add_argument('--budget', type=int, default=400)
    args = parser.parse_args()
    client = WorldClient.__new__(WorldClient)
    walk = None
    try:
        client.__init__(args.output.resolve(), 42, runtime_source=args.runtime.resolve(),
                        continuation=args.continue_from.resolve(), window_size='1024x720', scene='world-campaign')
        result = verify(client, args.budget)
        (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
        print(json.dumps(result), flush=True)
        client.request('stop'); client.process.wait(timeout=15)
    except BaseException as error:
        result = dict(passed=False, error=str(error), errorType=type(error).__name__, rollback=False)
        if hasattr(client, 'journal'):
            try:
                response = client.read('response.json')
                if hasattr(client, 'ui') and response and response['id'] == 'journey-' + str(client.sequence):
                    client.control_click('dungeon:save'); client.capture('failure-current-checkpoint')
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
