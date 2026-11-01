#!/usr/bin/env python3
"""Explore the normal rendered game using only visible observations and input.

Build Roag.Client, then run under xvfb-run with SDL_AUDIODRIVER=dummy and
LIBGL_ALWAYS_SOFTWARE=1. Saves, screenshots and an input journal stay in the
fresh evidence directory. No gameplay state, positions or results are injected.
This is a gameplay probe: a defeat or action budget is reported, never a pass.
"""
import argparse
from collections import Counter, deque
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import time

ROOT = Path(__file__).resolve().parents[2]
DIRECTIONS = ((0, -1, 'w'), (1, 0, 'd'), (0, 1, 's'), (-1, 0, 'a'))

def position(value):
    return value['x'], value['y'], value['floor']

def distance(a, b):
    return abs(a[0] - b[0]) + abs(a[1] - b[1]) if a[2] == b[2] else 100000

class Client:
    def __init__(self, output, seed):
        self.output = output
        output.mkdir(parents=True, exist_ok=False)
        self.control = output / 'control'
        self.control.mkdir()
        self.slot = self.control / 'campaign.json'
        self.runtime = output / 'runtime'
        shutil.copytree(ROOT / 'src/Roag.Client/bin/Debug/net10.0', self.runtime)
        metadata = dict(commit=subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip(),
            status=subprocess.check_output(['git', 'status', '--short'], cwd=ROOT, text=True),
            assemblies={p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in self.runtime.glob('Roag*.dll')},
            seed=seed, display=os.environ.get('DISPLAY'))
        (output / 'build.json').write_text(json.dumps(metadata, indent=2) + '\n')
        self.journal = (output / 'inputs.jsonl').open('w')
        self.sequence = 0
        self.launches = 0
        self.process = None
        self.start(['--new', '--seed', str(seed)])

    def start(self, flags):
        self.launches += 1
        for name in ['observation.json', 'response.json', 'request.json', 'frame.png']:
            path = self.control / name
            if path.exists(): path.rename(self.control / (str(self.launches) + '-old-' + name))
        self.log = (self.output / ('client-' + str(self.launches) + '.log')).open('wb')
        self.process = subprocess.Popen([str(ROOT / 'tools/dotnet.sh'), str(self.runtime / 'Roag.Client.dll'),
            *flags, '--save', str(self.slot), '--playtest-control', str(self.control),
            '--window-size', '1280x800', '--no-recording'], cwd=self.runtime, stdout=self.log, stderr=subprocess.STDOUT)
        self.ui = self.until(lambda: self.read('observation.json'))['ui']
        self.window = subprocess.check_output(['xdotool', 'search', '--onlyvisible', '--name', '^Roag$'], text=True).splitlines()[-1]
        subprocess.run(['xdotool', 'windowfocus', self.window], check=True)
        self.sequence = 0

    def until(self, check):
        deadline = time.monotonic() + 45
        while time.monotonic() < deadline:
            if self.process.poll() is not None: raise RuntimeError('Client exited; inspect its log.')
            value = check()
            if value: return value
            time.sleep(.015)
        raise TimeoutError('Client did not reach an observation boundary.')

    def read(self, name):
        path = self.control / name
        return json.loads(path.read_text()) if path.exists() else None

    def send(self, kind, **args):
        self.sequence += 1
        observation = self.read('observation.json')
        spec = dict(schemaVersion=1, id='journey-' + str(self.sequence), observation=observation['observation'], kind=kind, **args)
        temporary = self.control / 'request.tmp'
        temporary.write_text(json.dumps(spec)); temporary.replace(self.control / 'request.json')
        return spec

    def finish(self, spec):
        response = self.until(lambda: (r if (r := self.read('response.json')) and r['id'] == spec['id'] else None))
        if not response['success']: raise RuntimeError(str(response))
        self.ui = self.read('observation.json')['ui']
        self.journal.write(json.dumps(dict(input=spec, position=self.ui['position'], clock=self.ui['nativeDungeon']['clock'],
            health=self.ui['nativeDungeon']['player'][0]['health'], notice=self.ui['notice'], panel=self.ui['panel'])) + '\n')
        self.journal.flush()
        return self.ui

    def request(self, kind, **args):
        return self.finish(self.send(kind, **args))

    def key(self, key):
        spec = self.send('wait', seconds=.32)
        time.sleep(.07)
        subprocess.run(['xdotool', 'keydown', key], check=True)
        time.sleep(.15)
        subprocess.run(['xdotool', 'keyup', key], check=True)
        return self.finish(spec)

    def control_click(self, name):
        return self.request('control', control=name)

    def capture(self, name):
        self.request('capture')
        shutil.copy2(self.control / 'frame.png', self.output / (name + '.png'))
        shutil.copy2(self.control / 'observation.json', self.output / (name + '.json'))
        print(name, 'floor', self.ui['position']['floor'], 'clock', self.ui['nativeDungeon']['clock'],
            'HP', self.ui['nativeDungeon']['player'][0]['health'], flush=True)

    def resume(self):
        before = self.control_click('dungeon:save')
        self.request('stop'); self.process.wait(timeout=15); self.log.close()
        self.start(['--load'])
        for field in ['clock', 'player', 'stage', 'victory']:
            assert before['nativeDungeon'][field] == self.ui['nativeDungeon'][field], field
        assert before['position'] == self.ui['position']

    def close(self):
        if self.process is not None and self.process.poll() is None:
            self.process.terminate()
            try: self.process.wait(timeout=15)
            except subprocess.TimeoutExpired: self.process.kill(); self.process.wait()
        self.log.close(); self.journal.close()

class Journey:
    def __init__(self, client):
        self.client = client
        self.known = {}
        self.visits = Counter()
        self.receipts = set()
        self.failed = Counter()
        self.steps = 0
        self.last_floor = 1
        self.enemies = {d['id']: d for d in json.loads((ROOT / 'assets/tactics/enemies.json').read_text())['definitions']}

    def observe(self):
        self.ui = self.client.ui
        self.state = self.ui['nativeDungeon']
        self.player = self.state['player'][0]
        self.at = position(self.ui['position'])
        for cell in self.ui['cells']: self.known[position(cell)] = cell
        self.visible = {position(c): c for c in self.ui['cells']}
        self.fighters = [f for f in self.state['fighters'] if not f['friendly'] and f['health'] > 0]
        self.occupied = {position(f['position']) for f in self.fighters}
        self.danger = {position(c) for t in self.state['telegraphs'] if t['due'] <= self.state['clock'] + 190 for c in t['cells']}
        self.doors = {position(d['position']) for d in self.state['doors']}
        self.blocked = {position(c['position']) for c in self.state['chests'] if not c['claimed']}
        self.blocked |= {position(c['position']) for c in self.state['vaults'] if not c['opened']}
        self.blocked |= {position(g['position']) for g in self.state['gates'] if not g['open']}
        self.blocked |= {position(q['position']) for q in self.state['questNodes'] if q['node'] == 'Gate' and not q['open']}
        self.blocked |= {p for p, c in self.known.items() if c['material'] == 'StairsUp'}
        if self.at[2] != self.last_floor:
            self.last_floor = self.at[2]
            self.client.capture('floor-' + str(self.at[2]) + '-arrival')
            self.client.resume()
            self.observe()

    def open(self, p, safe=False):
        c = self.known.get(p)
        return c and (c['groundOpen'] or p in self.doors) and p not in self.occupied and p not in self.blocked and (not safe or p not in self.danger)

    def path(self, goals, safe=False):
        goals = set(goals)
        queue = deque([self.at]); previous = {self.at: None}
        while queue:
            p = queue.popleft()
            if p in goals:
                route = []
                while previous[p] is not None: route.append(p); p = previous[p]
                return route[::-1]
            for dx, dy, _ in DIRECTIONS:
                q = p[0] + dx, p[1] + dy, p[2]
                if q not in previous and self.open(q, safe): previous[q] = p; queue.append(q)
        return None

    def move(self, p):
        dx, dy = p[0] - self.at[0], p[1] - self.at[1]
        key = next(k for x, y, k in DIRECTIONS if (x, y) == (dx, dy))
        self.client.control_click('roag:move')
        before = self.state['clock']
        self.client.key(key)
        self.visits[p] += 1
        if self.client.ui['nativeDungeon']['clock'] == before: self.failed[p] += 1
        return True

    def go(self, goals, safe=False):
        route = self.path(goals, safe)
        return self.move(route[0]) if route else False

    def neighbors(self, p):
        return [(p[0] + dx, p[1] + dy, p[2]) for dx, dy, _ in DIRECTIONS]

    def aligned(self, a, b):
        if a[2] != b[2] or a[0] != b[0] and a[1] != b[1] or distance(a, b) > 4: return False
        dx, dy = (b[0] > a[0]) - (b[0] < a[0]), (b[1] > a[1]) - (b[1] < a[1])
        p = a[0] + dx, a[1] + dy, a[2]
        while p != b:
            c = self.known.get(p)
            if not c or not c['groundOpen'] or p in self.blocked or p in self.occupied: return False
            p = p[0] + dx, p[1] + dy, p[2]
        return True

    def fight(self):
        if not self.fighters: return False
        if self.at in self.danger:
            safe = [p for p in self.neighbors(self.at) if self.open(p, True)]
            if safe:
                return self.move(max(safe, key=lambda p: min(distance(p, position(f['position'])) for f in self.fighters)))
        enemies = sorted(self.fighters, key=lambda f: (0 if any(k in f['class'] for k in ['weaver','cantor','conjurer']) else 1, f['health'], distance(self.at, position(f['position']))))
        for enemy in enemies:
            target = position(enemy['position'])
            if self.aligned(self.at, target) and target in self.visible and self.visible[target]['clickable']:
                self.client.control_click('roag:basic_attack')
                clock = self.state['clock']
                self.client.request('tile', x=target[0], y=target[1])
                if self.client.ui['nativeDungeon']['clock'] == clock: self.failed[target] += 1
                else: self.receipts.add('combat')
                return True
        targets = [p for p in self.known if self.open(p, True) and p[2] == self.at[2]
            and any(self.aligned(p, position(f['position'])) for f in enemies)]
        route = self.path(targets, True)
        if route: return self.move(route[0])
        # A visible threat behind occluding terrain is approached through actual known tiles.
        return self.go([p for f in enemies for p in self.neighbors(position(f['position']))], True)

    def quiet_tasks(self):
        for drop in self.state['loot']:
            p = position(drop['position'])
            if drop['kind'] == 0 or self.player['health'] < self.player['maxHealth']:
                if self.go([p]): return True
        for key in self.state['vaultKeys']:
            if self.go([position(key['position'])]): return True
        for q in self.state['questNodes']:
            if not q['activated'] and (q['node'] in ['Clue','Key','Plate','Brazier'] or q['node'] == 'Reward' and q['ready']):
                if self.go([position(q['position'])]): return True
        for vault in self.state['vaults']:
            p = position(vault['position'])
            if vault['keyHeld'] and not vault['opened']:
                if distance(p, self.at) == 1:
                    self.client.control_click('roag:move'); self.client.request('tile', x=p[0], y=p[1]); self.receipts.add('vault'); return True
                if self.go(self.neighbors(p)): return True
        for chest in self.state['chests']:
            if chest['claimed'] or chest['definition'] != 'core:chest/free_elite' and self.ui['inventory'].get('gold', 0) < 5: continue
            p = position(chest['position'])
            if distance(p, self.at) == 1 and p in self.visible and self.visible[p]['clickable']:
                self.client.control_click('roag:move'); self.client.request('tile', x=p[0], y=p[1])
                assert self.client.ui['panel'] == 'dungeon-chest', self.client.ui['notice']
                self.client.capture('chest-' + str(chest['id']) + '-offers')
                offers = self.client.ui['nativeDungeon']['chestOffers']
                priorities = ['vampiric','lifesteal','mending','regeneration','funeral_coin','second_wind','heart','damage']
                choice = min(range(len(offers)), key=lambda i: next((n for n, s in enumerate(priorities) if s in offers[i]['boon']), len(priorities)))
                self.client.control_click('dungeon:claim:' + str(choice + 1)); self.receipts.add('boon-chest'); return True
            if self.go(self.neighbors(p)): return True
        return False

    def explore(self):
        # Only currently or previously visible terrain participates in pathfinding.
        frontier = [p for p in self.known if p[2] == self.at[2] and self.open(p)
            and any(n not in self.known for n in self.neighbors(p)) and self.failed[p] < 2]
        frontier.sort(key=lambda p: (distance(self.at, p) + self.visits[p] * 3, -p[1]))
        for p in frontier:
            if p == self.at: continue
            if self.go([p]): return True
        stairs = [p for p, c in self.known.items() if p[2] == self.at[2] and c['material'] == 'StairsDown']
        if self.go(stairs): return True
        # A defeated elite's exit becomes the next-stage route selection anchor.
        for fighter in self.state['fighters']:
            if 'elite' in self.enemies.get(fighter['class'], {}).get('ai', ''): continue
        self.client.key('Return')
        if self.client.ui['panel'] == 'dungeon-routes':
            self.client.capture('stage-' + str(self.state['stage']) + '-routes')
            self.client.control_click('dungeon:route:1'); self.receipts.add('stage-route'); return True
        if self.client.ui['panel']: self.client.control_click('dungeon:close')
        return False

    def run(self, budget, lifecycle):
        self.client.control_click('dungeon:class:1')
        self.client.control_click('dungeon:save')
        self.client.capture('01-ranger-start')
        for self.steps in range(1, budget + 1):
            self.observe()
            if self.player['health'] <= 0:
                self.client.capture('terminal-run')
                self.client.resume()
                assert self.client.ui['nativeDungeon']['player'][0]['health'] == 0
                self.client.capture('terminal-resumed')
                if lifecycle:
                    old_seed = self.client.ui['seed']
                    self.client.control_click('dungeon:new')
                    assert self.client.ui['seed'] != old_seed
                    assert self.client.ui['panel'] == 'dungeon-class'
                    assert self.client.ui['nativeDungeon']['clock'] == 0
                    assert self.client.ui['nativeDungeon']['player'][0]['health'] > 0
                    self.client.capture('fresh-run')
                    self.client.resume()
                    assert self.client.ui['nativeDungeon']['clock'] == 0
                return 'defeat'
            if self.state['victory']:
                self.client.capture('victory'); self.client.resume(); self.client.capture('victory-resumed'); return 'victory'
            if self.client.sequence > 880: self.client.resume()
            if self.steps % 50 == 0: self.client.capture('progress-' + str(self.steps))
            if lifecycle and self.fighters:
                self.client.control_click('roag:wait'); continue
            if self.fight() or self.quiet_tasks() or self.explore(): continue
            self.client.capture('no-known-route'); return 'no-known-route'
        self.client.capture('action-budget'); return 'action-budget'

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--seed', type=int, default=42)
    parser.add_argument('--budget', type=int, default=1600)
    parser.add_argument('--lifecycle', action='store_true', help='Encounter danger normally, wait for defeat, verify terminal resume/fresh run.')
    args = parser.parse_args()
    client = Client(args.output.resolve(), args.seed)
    journey = Journey(client)
    try:
        outcome = journey.run(args.budget, args.lifecycle)
        result = dict(outcome=outcome, actions=journey.steps, receipts=sorted(journey.receipts),
            lifecycleVerified=args.lifecycle and outcome == 'defeat', fullRunVerified=not args.lifecycle and outcome == 'victory')
        (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
        print(json.dumps(result), flush=True)
    finally: client.close()
