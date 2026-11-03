#!/usr/bin/env python3
"""Explore the normal rendered game using only visible observations and input.

Build Roag.Client, then run under xvfb-run with LIBGL_ALWAYS_SOFTWARE=1.
Test clients use OpenAL's null backend and SDL dummy audio so they stay muted.
Saves, screenshots and an input journal stay in the
fresh evidence directory. No gameplay state, positions or results are injected.
This is a gameplay probe: a defeat or action budget is reported, never a pass.
"""
import argparse
from collections import Counter, deque
import hashlib
import heapq
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
    def __init__(self, output, seed, runtime_source=None, continuation=None, window_size='1280x800', scene=None):
        self.output = output
        self.window_size = window_size
        output.mkdir(parents=True, exist_ok=False)
        self.control = output / 'control'
        self.control.mkdir()
        self.slot = self.control / 'campaign.json'
        self.runtime = output / 'runtime'
        shutil.copytree(runtime_source or ROOT / 'src/Roag.Client/bin/Debug/net10.0', self.runtime)
        self.continuation = continuation
        if continuation:
            shutil.copy2(continuation / 'control/campaign.json', self.slot)
            shutil.copytree(continuation / 'control/campaign.json.chunks', Path(str(self.slot) + '.chunks'))
        metadata = dict(commit=subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip(),
            status=subprocess.check_output(['git', 'status', '--short'], cwd=ROOT, text=True),
            assemblies={p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in self.runtime.glob('Roag*.dll')},
            seed=seed, scene=scene, display=os.environ.get('DISPLAY'), audioBackend='OpenAL null / SDL dummy',
            continuation=str(continuation) if continuation else None)
        (output / 'build.json').write_text(json.dumps(metadata, indent=2) + '\n')
        self.journal = (output / 'inputs.jsonl').open('w')
        self.sequence = 0
        self.launches = 0
        self.process = None
        flags = ['--load'] if continuation else ['--new', '--seed', str(seed)]
        self.start([*flags, '--scene', scene] if scene else flags)
        if continuation and (continuation / 'handoff.json').exists():
            handoff = json.loads((continuation / 'handoff.json').read_text())
            assert self.ui['position'] == handoff['position']
            assert self.ui['nativeDungeon']['clock'] == handoff['clock']
            assert self.ui['nativeDungeon']['player'] == handoff['player']
        metadata['seed'] = self.ui['seed']
        metadata['initialClock'] = self.ui['nativeDungeon']['clock']
        (output / 'build.json').write_text(json.dumps(metadata, indent=2) + '\n')

    def start(self, flags):
        self.launches += 1
        for name in ['observation.json', 'response.json', 'request.json', 'frame.png']:
            path = self.control / name
            if path.exists(): path.rename(self.control / (str(self.launches) + '-old-' + name))
        self.log = (self.output / ('client-' + str(self.launches) + '.log')).open('wb')
        command = [str(self.runtime / 'Roag.Client')] if (self.runtime / 'libhostfxr.so').exists() else [str(ROOT / 'tools/dotnet.sh'), str(self.runtime / 'Roag.Client.dll')]
        environment = {**os.environ, 'ALSOFT_DRIVERS': 'null', 'SDL_AUDIODRIVER': 'dummy'}
        self.process = subprocess.Popen([*command,
            *flags, '--save', str(self.slot), '--playtest-control', str(self.control),
            '--window-size', self.window_size, '--no-recording'], cwd=self.runtime, env=environment,
            stdout=self.log, stderr=subprocess.STDOUT)
        self.ui = self.until(lambda: self.read('observation.json'))['ui']
        self.window = subprocess.check_output(['xdotool', 'search', '--all', '--onlyvisible', '--pid', str(self.process.pid), '--name', '^Roag$'], text=True).splitlines()[-1]
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

    def finish(self, spec, physical_key=None):
        response = self.until(lambda: (r if (r := self.read('response.json')) and r['id'] == spec['id'] else None))
        if not response['success']: raise RuntimeError(str(response))
        self.ui = self.read('observation.json')['ui']
        self.journal.write(json.dumps(dict(input=spec, physicalKey=physical_key, position=self.ui['position'], clock=self.ui['nativeDungeon']['clock'],
            health=self.ui['nativeDungeon']['player'][0]['health'], notice=self.ui['notice'], panel=self.ui['panel'])) + '\n')
        self.journal.flush()
        return self.ui

    def request(self, kind, **args):
        return self.finish(self.send(kind, **args))

    def key(self, key, seconds=.32):
        spec = self.send('wait', seconds=seconds)
        # Wait for the game to accept the interval before pressing. Keep the
        # key held through the observation, then release it before the next
        # request. Both edges go through SDL's ordinary keyboard state.
        self.until(lambda: not (self.control / 'request.json').exists())
        subprocess.run(['xdotool', 'keydown', key], check=True)
        try: return self.finish(spec, key)
        finally: subprocess.run(['xdotool', 'keyup', key], check=True)

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
    def __init__(self, client, class_index=1, features=False):
        self.client = client
        self.class_index = class_index
        self.features = features
        self.known = {}
        self.visits = Counter()
        self.receipts = set()
        self.failed = Counter()
        self.steps = 0
        self.last_floor = 1
        self.exits = {}
        self.inspected_services = set()
        self.destination = None
        self.ready_routes = {}
        if client.continuation and (client.continuation / 'knowledge.json').exists():
            memory = json.loads((client.continuation / 'knowledge.json').read_text())
            self.known = {position(c): c for c in memory['cells']}
            self.exits = {int(k): tuple(v) for k, v in memory['exits'].items()}
            self.ready_routes = {int(k): tuple(v) for k, v in memory.get('readyRoutes', {}).items()}
            self.receipts.update(memory['receipts'])
            self.inspected_services.update(memory['inspectedServices'])
            if memory.get('destination'): self.destination = (tuple(memory['destination'][0]), memory['destination'][1])
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
        for gate in self.state.get('routeGates', []):
            self.exits[self.at[2]] = position(gate['position'])
            if gate['available']: self.ready_routes[self.at[2]] = position(gate['position'])
        for enemy in self.fighters:
            if self.enemies.get(enemy['class'], {}).get('ai') in ['warden', 'biome_elite', 'hollow_regent']:
                self.exits.setdefault(self.at[2], position(enemy['position']))
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
        queue = [(0, self.at)]; previous = {self.at: None}; costs = {self.at: 0}
        while queue:
            cost, p = heapq.heappop(queue)
            if cost != costs[p]: continue
            if p in goals:
                route = []
                while previous[p] is not None: route.append(p); p = previous[p]
                return route[::-1]
            for dx, dy, _ in DIRECTIONS:
                q = p[0] + dx, p[1] + dy, p[2]
                if not self.open(q, safe): continue
                terrain = self.known[q].get('terrain')
                # This is the same semantic overlay shown by sprites and hover.
                # Prefer a safe detour over contact damage; armor can negate it.
                price = 1 + self.failed[q] * 40 + (30 if terrain == 'b' and self.player['stats']['defense'] == 0 else 0) + (2 if terrain == 'g' else 0)
                candidate = cost + price
                if candidate < costs.get(q, float('inf')):
                    previous[q] = p; costs[q] = candidate; heapq.heappush(queue, (candidate, q))
        return None

    def select_movement(self):
        if self.client.ui['nativeDungeon']['aim'] != 'move': self.client.key('Escape')

    def move(self, p):
        dx, dy = p[0] - self.at[0], p[1] - self.at[1]
        key = next(k for x, y, k in DIRECTIONS if (x, y) == (dx, dy))
        self.select_movement()
        before = self.state['clock']
        self.client.key(key)
        self.visits[p] += 1
        after = self.client.ui
        if after['nativeDungeon']['clock'] == before or position(after['position']) == self.at:
            self.failed[p] += 1
            # A visible push floor can return a walker to the starting cell.
            # Try the class's ordinary movement skill through the known lane.
            if self.failed[p] in [2, 5, 8] and after['nativeDungeon']['clock'] > before:
                lane = [(self.at[0] + dx * n, self.at[1] + dy * n, self.at[2]) for n in range(1, 4 if self.player['classId'] == 'Ranger' else 3)]
                if all(q in self.known and self.known[q]['groundOpen'] for q in lane):
                    self.client.key('x+' + key)
                    if position(self.client.ui['position']) != self.at: self.receipts.add('movement-skill')
        return True

    def go(self, goals, safe=False):
        route = self.path(goals, safe)
        return self.move(route[0]) if route else False

    def neighbors(self, p):
        return [(p[0] + dx, p[1] + dy, p[2]) for dx, dy, _ in DIRECTIONS]

    def aligned(self, a, b):
        if a[2] != b[2] or a[0] != b[0] and a[1] != b[1] or distance(a, b) > {'Ranger': 4, 'Geomancer': 3, 'Adventurer': 1}[self.player['classId']]: return False
        dx, dy = (b[0] > a[0]) - (b[0] < a[0]), (b[1] > a[1]) - (b[1] < a[1])
        p = a[0] + dx, a[1] + dy, a[2]
        while p != b:
            c = self.known.get(p)
            if not c or not c['groundOpen'] or p in self.blocked or p in self.occupied: return False
            p = p[0] + dx, p[1] + dy, p[2]
        return True

    def fight(self):
        if self.player['supplies']['utility'] and self.player['supplies']['utility']['kind'] == 0 \
            and any(c['burning'] for c in self.state.get('conditions', [])):
            self.client.control_click('dungeon:utility')
            self.client.control_click('dungeon:utility-use')
            self.receipts.add('cleanse-utility'); return True
        if not self.fighters: return False
        if self.at in self.danger:
            safe = [p for p in self.neighbors(self.at) if self.open(p, True)]
            if safe:
                # Stay within fighting reach when possible. Always retreating
                # from a stationary boss can repeat the same warning forever.
                return self.move(min(safe, key=lambda p: (
                    not any(self.aligned(p, position(f['position'])) for f in self.fighters),
                    self.visits[p], min(distance(p, position(f['position'])) for f in self.fighters))))
            escapes = []
            for dx, dy, key in DIRECTIONS:
                lane = []
                for n in range(1, 4 if self.player['classId'] == 'Ranger' else 3):
                    cell = self.at[0] + dx * n, self.at[1] + dy * n, self.at[2]
                    if cell not in self.visible or not self.open(cell) or self.known[cell]['material'].startswith('Stairs'): break
                    lane.append(cell)
                if lane and lane[-1] not in self.danger:
                    escapes.append((min(distance(lane[-1], position(f['position'])) for f in self.fighters), key))
            for _, key in sorted(escapes):
                self.client.key('x+' + key)
                if position(self.client.ui['position']) != self.at:
                    self.receipts.add('movement-skill-evasion'); return True

        enemies = sorted(self.fighters, key=lambda f: (0 if any(k in f['class'] for k in ['weaver','cantor','conjurer']) else 1, f['health'], distance(self.at, position(f['position']))))
        for enemy in enemies:
            target = position(enemy['position'])
            if self.aligned(self.at, target) and target in self.visible and self.visible[target]['clickable']:
                clock = self.state['clock']
                dx, dy = (target[0] > self.at[0]) - (target[0] < self.at[0]), (target[1] > self.at[1]) - (target[1] < self.at[1])
                direction = next(k for x, y, k in DIRECTIONS if (x, y) == (dx, dy))
                self.client.key('f+' + direction)
                if self.client.ui['nativeDungeon']['clock'] == clock: self.failed[target] += 1
                else: self.receipts.add('combat')
                return True
        targets = [p for p in self.known if self.open(p, True) and p[2] == self.at[2]
            and any(self.aligned(p, position(f['position'])) for f in enemies)]
        route = self.path(targets, True)
        if route: return self.move(route[0])
        # A visible threat behind occluding terrain is approached through actual known tiles.
        return self.go([p for f in enemies for p in self.neighbors(position(f['position']))], True)

    def approach(self, target, radius=0):
        self.destination = (target, radius)
        return self.go([target, *self.neighbors(target)] if radius else [target])

    def quiet_tasks(self):
        # A seen object remains a navigation goal after it leaves live vision.
        # Its current receipt is rechecked only when it comes back into sight.
        if self.destination:
            target, radius = self.destination
            if distance(self.at, target) > radius:
                if self.go([target, *self.neighbors(target)] if radius else [target]): return True
            self.destination = None
        if self.player['classId'] == 'Adventurer':
            for secret in self.state['secrets']:
                if secret['kind'] != 'CrackedCache' or secret['revealed']: continue
                p = position(secret['position'])
                if distance(p, self.at) == 1:
                    dx, dy = p[0] - self.at[0], p[1] - self.at[1]
                    direction = next(k for x, y, k in DIRECTIONS if (x, y) == (dx, dy))
                    self.client.key('f+' + direction); self.receipts.add('secret')
                    self.client.capture('secret-' + str(secret['id']) + '-opened'); return True
                if self.approach(p, 1): return True
        for service in self.state['services']:
            if service['usedBy'] or service['id'] in self.inspected_services: continue
            if service['kind'] not in ['Camp', 'CleanseFlask', 'EscapeScroll', 'ReturnToken', 'CharmWell', 'OfferingFont', 'TravellingTrader']: continue
            if service['kind'] == 'Camp' and self.player['health'] == self.player['maxHealth']: continue
            p = position(service['position'])
            if distance(p, self.at) <= 1 and p in self.visible and self.visible[p]['clickable']:
                self.select_movement(); self.client.request('tile', x=p[0], y=p[1])
                assert self.client.ui['panel'] == 'dungeon-service', self.client.ui['notice']
                self.client.capture('service-' + str(service['id']) + '-offers')
                offers = self.client.ui['nativeDungeon']['serviceOffers'] or []
                eligible = [o for o in offers if o['enabled'] and not o['healthCost']]
                if eligible:
                    option = sorted(eligible, key=lambda o: (-o['healing'], o['replacement'], o['price'], o['choice']))[0]
                    self.client.control_click('dungeon:service-choice:' + str(option['choice']))
                    if self.client.ui['panel'] == 'dungeon-service-replace': self.client.control_click('dungeon:service-confirm')
                    self.receipts.add('service-' + service['kind'])
                else: self.client.control_click('dungeon:close')
                self.inspected_services.add(service['id']); return True
            if self.approach(p, 1): return True
        for drop in self.state['loot']:
            p = position(drop['position'])
            if drop['kind'] == 0 or self.player['health'] < self.player['maxHealth']:
                if self.approach(p): return True
        for key in self.state['vaultKeys']:
            if self.approach(position(key['position'])): return True
        for q in self.state['questNodes']:
            if not q['activated'] and (q['node'] in ['Clue','Key','Plate','Brazier'] or q['node'] == 'Reward' and q['ready']):
                if self.approach(position(q['position'])): return True
        for vault in self.state['vaults']:
            p = position(vault['position'])
            if vault['keyHeld'] and not vault['opened']:
                if distance(p, self.at) == 1:
                    self.select_movement(); self.client.request('tile', x=p[0], y=p[1]); self.receipts.add('vault'); return True
                if self.approach(p, 1): return True
        for chest in self.state['chests']:
            if chest['claimed'] or chest['definition'] != 'core:chest/free_elite' and self.ui['inventory'].get('gold', 0) < 5: continue
            p = position(chest['position'])
            if distance(p, self.at) == 1 and p in self.visible and self.visible[p]['clickable']:
                self.select_movement(); self.client.request('tile', x=p[0], y=p[1])
                assert self.client.ui['panel'] == 'dungeon-chest', self.client.ui['notice']
                self.client.capture('chest-' + str(chest['id']) + '-offers')
                offers = self.client.ui['nativeDungeon']['chestOffers']
                priorities = ['vampiric','lifesteal','mending','regeneration','funeral_coin','second_wind','heart','damage']
                choice = min(range(len(offers)), key=lambda i: next((n for n, s in enumerate(priorities) if s in offers[i]['boon']), len(priorities)))
                self.client.control_click('dungeon:claim:' + str(choice + 1)); self.receipts.add('boon-chest'); return True
            if self.approach(p, 1): return True
        return False

    def explore(self):
        ready_exit = self.ready_routes.get(self.at[2])
        if ready_exit:
            if distance(ready_exit, self.at) > 1:
                if self.go([ready_exit, *self.neighbors(ready_exit)]): return True
            else:
                self.select_movement()
                self.client.request('tile', x=ready_exit[0], y=ready_exit[1])
                if self.client.ui['panel'] == 'dungeon-routes':
                    self.client.capture('stage-' + str(self.state['stage']) + '-routes')
                    self.client.control_click('dungeon:route:1')
                    self.receipts.add('stage-route'); del self.ready_routes[self.at[2]]
                    self.destination = (ready_exit, 0); return True
        stairs = [p for p, c in self.known.items() if p[2] == self.at[2] and c['material'] == 'StairsDown']
        if self.go(stairs): return True
        # Only currently or previously visible terrain participates in pathfinding.
        frontier = [p for p in self.known if p[2] == self.at[2] and self.open(p)
            and any(n not in self.known for n in self.neighbors(p)) and self.failed[p] < 2]
        frontier.sort(key=lambda p: (distance(self.at, p) + self.visits[p] * 3, -p[1]))
        for p in frontier:
            if p == self.at: continue
            if self.go([p]): return True
        stairs = [p for p, c in self.known.items() if p[2] == self.at[2] and c['material'] == 'StairsDown']
        if self.go(stairs): return True
        # Remember only an elite position actually observed in the game.
        exit_position = self.exits.get(self.at[2])
        if exit_position and distance(exit_position, self.at) > 1:
            if self.go([exit_position, *self.neighbors(exit_position)]): return True
        self.client.key('Return')
        if self.client.ui['panel'] == 'dungeon-routes':
            self.client.capture('stage-' + str(self.state['stage']) + '-routes')
            self.client.control_click('dungeon:route:1'); self.receipts.add('stage-route'); return True
        if self.client.ui['panel']: self.client.control_click('dungeon:close')
        return False

    def exercise_gate(self):
        self.observe()
        gate = self.state['gates'][0]
        gate_position = position(gate['position'])
        plate = next(position(f['position']) for f in self.state['roomFeatures']
            if f['kind'] == 'PressurePlate' and distance(position(f['position']), gate_position) == 1)
        for _ in range(40):
            self.observe()
            if self.at == plate: break
            assert not self.fighters, 'Dedicated gate check needs an undisturbed approach.'
            assert self.go([plate]), 'Plate has no known ordinary route.'
        self.observe()
        assert self.at == plate
        opened = next(g for g in self.state['gates'] if g['id'] == gate['id'])
        assert opened['open'] and opened['closeAt'] > 0
        close_at = opened['closeAt']
        self.client.capture('gate-plate-opened')
        self.move(gate_position); self.observe(); assert self.at == gate_position
        while self.state['clock'] < close_at:
            self.client.key('space'); self.observe()
        assert next(g for g in self.state['gates'] if g['id'] == gate['id'])['open']
        self.client.capture('gate-occupied-at-closure')
        leaving = next(p for p in self.neighbors(self.at) if p != plate and self.open(p))
        self.move(leaving); self.observe()
        assert not next(g for g in self.state['gates'] if g['id'] == gate['id'])['open']
        self.client.capture('gate-closed-after-vacancy')
        self.receipts.add('occupied-gate-closure')

    def checkpoint(self):
        self.client.control_click('dungeon:save')
        (self.client.output / 'handoff.json').write_text(json.dumps(dict(position=self.client.ui['position'],
            clock=self.client.ui['nativeDungeon']['clock'], player=self.client.ui['nativeDungeon']['player'], rollback=False)) + '\n')
        (self.client.output / 'knowledge.json').write_text(json.dumps(dict(cells=list(self.known.values()),
            exits=self.exits, receipts=sorted(self.receipts), inspectedServices=sorted(self.inspected_services), destination=self.destination, readyRoutes=self.ready_routes)) + '\n')

    def run(self, budget, lifecycle):
        if not self.client.continuation:
            self.client.control_click('dungeon:class:' + str(self.class_index))
            self.client.control_click('dungeon:save')
            self.client.control_click('dungeon:menu')
            self.client.control_click('dungeon:new-question')
            before = self.client.ui['nativeDungeon']['clock']
            self.client.capture('00-fresh-run-confirmation')
            self.client.control_click('dungeon:new-cancel')
            assert self.client.ui['nativeDungeon']['clock'] == before
            self.client.capture('01-run-start')
            if self.features: self.exercise_gate()
        if self.client.continuation: self.client.capture('continuation-start')
        for self.steps in range(1, budget + 1):
            self.observe()
            if (self.client.output / 'pause-request').exists():
                self.checkpoint(); self.client.capture('continuation-boundary')
                (self.client.output / 'handoff.json').write_text(json.dumps(dict(position=self.client.ui['position'],
                    clock=self.client.ui['nativeDungeon']['clock'], player=self.client.ui['nativeDungeon']['player'], rollback=False)) + '\n')
                return 'paused'
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
            if self.features and 'secret' in self.receipts: return 'features-verified'
            if self.state['victory']:
                self.client.capture('victory'); self.client.resume(); self.observe()
                before = self.state['clock']
                neighbor = next(p for p in self.neighbors(self.at) if self.open(p, True))
                self.move(neighbor); self.observe()
                assert self.state['clock'] > before and self.state['victory']
                self.client.capture('victory-paid-movement'); self.client.resume()
                assert self.client.ui['nativeDungeon']['victory']
                self.client.capture('victory-resumed'); return 'victory'
            if self.client.sequence > 880: self.client.resume()
            if self.steps % 50 == 0:
                self.client.capture('progress-' + str(self.steps))
                self.checkpoint()
            if lifecycle and self.fighters:
                self.client.key('space'); continue
            if self.fight() or self.quiet_tasks() or self.explore(): continue
            self.client.capture('no-known-route'); return 'no-known-route'
        self.client.capture('action-budget'); return 'action-budget'

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--seed', type=int, default=42)
    parser.add_argument('--budget', type=int, default=6000)
    parser.add_argument('--runtime', type=Path, help='Extracted Linux package directory, or a built client directory.')
    parser.add_argument('--continue-from', dest='continuation', type=Path, help='Previous stopped probe evidence; continue its latest ordinary save and observed map.')
    parser.add_argument('--class-index', type=int, choices=[0, 1, 10], default=1, help='Adventurer, Ranger or Geomancer; probe supports their ordinary attack ranges.')
    parser.add_argument('--features', action='store_true', help='Exercise the natural starting gate, then seek an Adventurer cracked cache.')
    parser.add_argument('--lifecycle', action='store_true', help='Encounter danger normally, wait for defeat, verify terminal resume/fresh run.')
    args = parser.parse_args()
    client = Client(args.output.resolve(), args.seed, args.runtime.resolve() if args.runtime else None, args.continuation.resolve() if args.continuation else None)
    journey = Journey(client, args.class_index, args.features)
    try:
        outcome = journey.run(args.budget, args.lifecycle)
        if not args.lifecycle: journey.checkpoint()
        result = dict(outcome=outcome, actions=journey.steps, receipts=sorted(journey.receipts),
            lifecycleVerified=args.lifecycle and outcome == 'defeat', fullRunVerified=not args.lifecycle and outcome == 'victory')
        (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
        print(json.dumps(result), flush=True)
        client.request('stop'); client.process.wait(timeout=15)
    finally: client.close()
