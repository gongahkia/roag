"""Bounded player knowledge. Never reads campaigns, world plans or recorder internals."""
from collections import deque
import json
from .recordings import read
from agent_support.workspace import atomic_json


def load(path, run_id, objective):
    state = read(path / 'player-memory.json')
    if state is None:
        state = {'schemaVersion': 1, 'id': run_id, 'objective': objective, 'plan': None, 'places': {},
                 'journal': [], 'notices': [], 'terrain': [], 'visits': {}, 'actions': [], 'milestones': {},
                 'lastObservation': None, 'lastPosition': None, 'observations': 0, 'stationaryActions': 0}
    if state.get('schemaVersion') != 1 or state.get('id') != run_id:
        raise ValueError('Player memory belongs to another session or schema.')
    state['objective'] = objective
    return state


def remember(state, observation):
    if state['lastObservation'] == observation['observation']:
        return
    state['observations'] += 1
    state['lastObservation'] = observation['observation']
    position = observation['position']
    key = cell_key(position['x'], position['y'], position['floor'])
    if state['lastPosition'] != position:
        state['visits'][key] = state['visits'].get(key, 0) + 1
        state['lastPosition'] = position.copy()
        state['stationaryActions'] = 0
    state['visits'] = dict(list(state['visits'].items())[-4096:])
    milestones = state['milestones']
    def mark(name):
        milestones.setdefault(name, {'observation': observation['observation'], 'position': position.copy()})
    mark('started')
    if len(observation.get('party', [])) >= 3:
        mark('recruited-two-companions')
    if position['floor'] > 0:
        mark('entered-dungeon')
    if observation.get('battle'):
        mark('encountered-combat')
    if observation.get('encounterReward'):
        mark('saw-rewards')
    for place in [*(observation.get('discoveries') or []), *(observation.get('places') or [])]:
        value = {k: place[k] for k in ('id', 'name', 'x', 'y', 'floor', 'visited', 'outcome') if k in place}
        value.setdefault('floor', 0)
        previous = state['places'].get(place['id'], {})
        state['places'][place['id']] = {**previous, **value, 'sourceObservation': previous.get('sourceObservation', observation['observation'])}
        if value.get('visited'):
            mark('visited-discovery')
    state['places'] = dict(list(state['places'].items())[-128:])
    for row in observation.get('journal') or []:
        text = row.get('text', '') if isinstance(row, dict) else str(row)
        if text.strip() and text not in state['journal']:
            state['journal'].append(text[:1000])
    state['journal'] = state['journal'][-80:]
    notice = observation.get('notice', '')[:2000]
    if notice and (not state['notices'] or state['notices'][-1]['text'] != notice):
        state['notices'].append({'text': notice, 'observation': observation['observation']})
    state['notices'] = state['notices'][-16:]
    terrain = {(x, y, f): open_ for x, y, f, open_ in state['terrain']}
    for x, y, open_, occupied, clickable in observation.get('localGround', []):
        terrain[(x, y, position['floor'])] = open_
    state['terrain'] = [[*key, value] for key, value in list(terrain.items())[-4096:]]


def save(path, state):
    if len(json.dumps(state).encode()) > 750000:
        raise ValueError('Player memory exceeds its storage bound.')
    atomic_json(path / 'player-memory.json', state)


def plan(state, value):
    if value is None:
        return
    if not isinstance(value, dict) or set(value) != {'goal', 'targetId', 'nextStep', 'remember'}:
        raise ValueError('Invalid player plan.')
    if any(not isinstance(value[k], str) or len(value[k]) > 1000 for k in ('goal', 'nextStep', 'remember')):
        raise ValueError('Plan text must be bounded.')
    target = value['targetId']
    if target is not None and (not isinstance(target, str) or target not in state['places']):
        raise ValueError('A navigation target must come from a player-observed known place.')
    state['plan'] = value.copy()


def action(state, result, reason):
    state['actions'].append({'step': result['step'], 'action': result['action'], 'delivered': result['delivered'],
                             'notice': result['notice'], 'position': result['position'], 'reason': reason[:500]})
    state['actions'] = state['actions'][-24:]
    if result['action']['kind'] not in ('capture', 'hover', 'mark'):
        state['stationaryActions'] += 1


def cell_key(x, y, floor):
    return f'{x},{y},{floor}'


def context(state, observation, journey=False):
    result = {k: state[k] for k in ('objective', 'plan', 'places', 'journal', 'notices', 'milestones', 'actions', 'stationaryActions')}
    result['planFeedback'] = state.get('planFeedback')
    result['completionFeedback'] = state.get('completionFeedback')
    result['decisionFeedback'] = state.get('decisionFeedback')
    result['currentPartySize'] = len(observation.get('party', []))
    result['milestoneMeaning'] = 'Observed checkpoints only; they do not certify understanding, enjoyment, victory or a completed objective.'
    result['commentDue'] = state['observations'] == 1 or state['observations'] % 8 == 0
    if journey:
        result['journey'] = guidance(state, observation)
    return result


def journey_complete(state):
    return all(k in state['milestones'] for k in ('recruited-two-companions', 'visited-discovery', 'entered-dungeon'))


def guidance(state, observation):
    """A labelled walking aid uses only remembered leads and currently observed ground."""
    p = observation['position']; milestones = state['milestones']
    checkpoint = next((s for s in ('recruited-two-companions', 'visited-discovery', 'entered-dungeon') if s not in milestones), 'review')
    result = {'nextUnmetCheckpoint': checkpoint, 'completedCheckpoints': list(milestones),
              'currentPartySize': len(observation.get('party', [])),
              'instruction': 'nextUnmetCheckpoint is NOT completed. Only completedCheckpoints are reached.',
              'assistance': 'Observed-ground route suggestion, not an unaided wayfinding test.', 'suggestedAction': None}
    if observation.get('panel') or observation.get('battle') or observation.get('context'):
        return result
    if observation.get('routeCount', 0):
        result['suggestedAction'] = {'kind': 'wait', 'seconds': min(8, observation['routeCount'] * .3 + .6)}
        return result
    if checkpoint == 'recruited-two-companions':
        return result
    known = list(state['places'].values())
    target = state['places'].get((state.get('plan') or {}).get('targetId'))
    sites = [v for v in known if v['id'].startswith('site-')]
    discoveries = [v for v in known if not v['id'].startswith(('town-', 'site-')) and not v.get('visited')]
    eligible = sites if checkpoint == 'entered-dungeon' else discoveries
    if target is None or target not in eligible:
        target = min(eligible, key=lambda v: abs(v['x'] - p['x']) + abs(v['y'] - p['y']), default=None)
    if not target:
        result['hint'] = 'Learn places through inhabitants or the map. At a visited discovery, Mark the linked dungeon provides its public location.'
        return result
    result['target'] = target
    if target.get('floor', 0) != p['floor']:
        return result
    result['distance'] = abs(target['x'] - p['x']) + abs(target['y'] - p['y'])
    features = [v for v in observation.get('localFeatures', []) if v['material'] == 'StairsDown']
    if checkpoint == 'entered-dungeon' and features:
        step = min(features, key=lambda v: abs(v['x'] - p['x']) + abs(v['y'] - p['y']))
        result['suggestedAction'] = {'kind': 'tile', 'x': step['x'], 'y': step['y']}
        return result
    visible = {(x, y): (open_, occupied, clickable) for x, y, open_, occupied, clickable in observation.get('localGround', [])}
    if (target['x'], target['y']) in visible:
        result['suggestedAction'] = {'kind': 'tile', 'x': target['x'], 'y': target['y']}
        return result
    start = (p['x'], p['y']); distances = {start: 0}; queue = deque([start])
    while queue:
        x, y = queue.popleft()
        for step in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            tile = visible.get(step)
            if step not in distances and tile and tile[0] and not tile[1]:
                distances[step] = distances[(x, y)] + 1; queue.append(step)
    choices = [v for v in distances if v != start and visible.get(v, (False, False, False))[2]]
    if choices:
        step = min(choices, key=lambda v: abs(v[0] - target['x']) + abs(v[1] - target['y']) + .15 * distances[v]
                   + 5 * state['visits'].get(cell_key(*v, p['floor']), 0))
        result['suggestedAction'] = {'kind': 'tile', 'x': step[0], 'y': step[1]}
    else:
        result['hint'] = 'No free observed walking step. Inspect the blocker and report it; no hidden route was queried.'
        party = observation.get('party', [])
        if len(party) > 1:
            last = party[-1]
            cell = (last.get('x'), last.get('y'))
            if cell in visible and visible[cell][2]:
                result['suggestedAction'] = {'kind': 'tile', 'x': cell[0], 'y': cell[1], 'button': 3}
                result['hint'] = 'The formation may block the exit. Inspect the last companion’s normal right-click options and read the available actions; no character was moved by the controller.'
    return result
