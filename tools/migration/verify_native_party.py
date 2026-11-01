#!/usr/bin/env python3
"""Real-input, muted party recruitment/control/save checkpoint; no game-state injection.

This focused check is not the boss/split-floor/rescue acceptance journey.
Run after building the client, under Xvfb/software GL, with a fresh evidence path.
"""
import argparse
from collections import deque
import json
from pathlib import Path

from verify_native_journey import Client, DIRECTIONS, distance, position


def route(ui, goals):
    at = position(ui['position'])
    cells = {position(c): c for c in ui['cells']}
    state = ui['nativeDungeon']
    occupied = {position(a['position']) for a in state['player'] if a['life'] != 'Dead' and a['id'] != state['activeActor']}
    occupied |= {position(a['position']) for a in state['recruits']}
    occupied |= {position(a['position']) for a in state['fighters'] if not a['friendly']}
    pending = deque([at]); previous = {at: None}
    while pending:
        p = pending.popleft()
        if p in goals:
            result = []
            while previous[p] is not None:
                result.append(p); p = previous[p]
            return result[::-1]
        for dx, dy, _ in DIRECTIONS:
            q = p[0] + dx, p[1] + dy, p[2]
            if q in previous or q in occupied or not cells.get(q, {}).get('groundOpen'): continue
            previous[q] = p; pending.append(q)
    return None


def verify(client, class_index=1):
    assert client.ui['panel'] == 'dungeon-class'
    client.control_click('dungeon:class:' + str(class_index))
    receipts = []
    visited = {}
    for iteration in range(180):
        ui = client.ui; state = ui['nativeDungeon']
        assert not state.get('defeat', False)
        if len(state['player']) == 4: break
        at = position(ui['position'])
        recruits = sorted(state['recruits'], key=lambda r: distance(at, position(r['position'])))
        key = state['activeActor'], at
        visited[key] = visited.get(key, 0) + 1
        if visited[key] >= 4 and len(state['player']) > 1:
            others = [a for a in state['player'] if a['life'] == 'Alive' and a['id'] != state['activeActor']]
            member = min(others, key=lambda a: min(distance(position(a['position']), position(r['position'])) for r in recruits))
            client.control_click('dungeon:member:' + str(member['id']))
            client.control_click('dungeon:party'); client.control_click('dungeon:lead-selected')
            client.control_click('dungeon:close'); client.key('t'); client.key('space')
            visited.clear()
            continue
        assert recruits, 'No visible adventurer available in the starting room'
        adjacent = next((r for r in recruits if distance(at, position(r['position'])) == 1), None)
        if adjacent:
            clock = state['clock']; location = adjacent['position']; expected = adjacent['classId']
            client.request('tile', x=location['x'], y=location['y'])
            assert client.ui['panel'] == 'dungeon-recruit', client.ui['notice']
            assert client.ui['nativeDungeon']['clock'] == clock
            client.capture('recruit-' + str(len(receipts) + 1) + '-review')
            client.control_click('dungeon:recruit')
            member = next(a for a in client.ui['nativeDungeon']['player'] if a['id'] == adjacent['actor'])
            assert member['classId'] == expected
            # A faster recruit may already spend their own scheduled follow step
            # before the recruiter next becomes ready; recruitment itself never relocates them.
            assert distance(position(member['position']), position(location)) <= 1
            receipts.append(adjacent['actor'])
            continue
        goals = set()
        for recruit in recruits:
            p = position(recruit['position'])
            goals |= {(p[0] + dx, p[1] + dy, p[2]) for dx, dy, _ in DIRECTIONS}
        path = route(ui, goals)
        if path:
            step = path[0]
            key = next(k for dx, dy, k in DIRECTIONS if (at[0] + dx, at[1] + dy, at[2]) == step)
            client.key(key)
        else:
            others = [a for a in state['player'] if a['life'] == 'Alive' and a['id'] != state['activeActor']]
            if others:
                member = min(others, key=lambda a: min(distance(position(a['position']), position(r['position'])) for r in recruits))
                client.control_click('dungeon:member:' + str(member['id']))
                client.control_click('dungeon:party'); client.control_click('dungeon:lead-selected')
                client.control_click('dungeon:close'); client.key('t')
            client.key('space')
    else:
        raise AssertionError('Recruitment input budget exhausted')
    state = client.ui['nativeDungeon']
    assert len(state['player']) == 4 and len(set(receipts)) == 3
    client.capture('four-member-gameplay')
    if class_index == 12:
        owner = client.ui['nativeDungeon']['player'][0]['id']
        client.control_click('dungeon:member:' + str(owner))
        client.control_click('dungeon:party'); client.control_click('dungeon:lead-selected')
        client.control_click('dungeon:close'); client.key('t')
        for _ in range(12):
            if client.ui['nativeDungeon']['activeActor'] == owner: break
            client.key('space')
        assert client.ui['nativeDungeon']['activeActor'] == owner
        for _, _, key in DIRECTIONS:
            client.key('r+' + key)
            member = next(a for a in client.ui['nativeDungeon']['player'] if a['id'] == owner)
            if member['resources']['sentry'] is not None: break
        else: raise AssertionError('No legal adjacent sentry deployment in the starting room')
        # Its first scheduled charge can already run during the owner's paid recovery.
        assert 1 <= member['resources']['sentry']['charges'] <= 3
        assert all(a['resources']['sentry'] is None for a in client.ui['nativeDungeon']['player'] if a['id'] != owner)
        client.capture('owned-sentry-and-party')
        client.resume()
        assert next(a for a in client.ui['nativeDungeon']['player'] if a['id'] == owner)['resources']['sentry'] is not None
        client.capture('owned-sentry-exact-reload')
    state = client.ui['nativeDungeon']
    assert not any(c['id'].startswith('roag:') for c in client.ui['controls'])
    client.control_click('dungeon:party'); client.capture('four-member-party-panel')
    client.control_click('dungeon:close')
    client.control_click('dungeon:menu'); client.control_click('dungeon:settings')
    client.control_click('dungeon:setting:text-size'); client.control_click('dungeon:setting:font')
    assert client.ui['nativeDungeon']['presentation']['textSize'] == 'Large'
    assert client.ui['nativeDungeon']['presentation']['font'] == 'Sans'
    client.control_click('dungeon:close'); client.capture('four-member-large-sans')
    client.control_click('dungeon:party'); client.capture('four-member-large-sans-panel')
    client.control_click('dungeon:close')
    clock = state['clock']; active = state['activeActor']; players = state['player']
    for index, member in enumerate(players):
        client.key(str(index + 1))
        observed = client.ui['nativeDungeon']
        assert observed['controlledActor'] == member['id']
        assert observed['activeActor'] == active and observed['clock'] == clock
        assert observed['player'] == players
        client.control_click('dungeon:boons'); client.capture('member-' + str(index + 1) + '-boons')
        client.control_click('dungeon:close')
        client.control_click('dungeon:utility'); client.capture('member-' + str(index + 1) + '-utility')
        client.control_click('dungeon:close')
    other = next(a for a in players if a['id'] != active)
    client.control_click('dungeon:member:' + str(other['id']))
    client.key('space')
    assert client.ui['nativeDungeon']['clock'] == clock
    assert client.ui['nativeDungeon']['player'] == players
    assert client.ui['nativeDungeon']['activeActor'] == active
    assert 'ready member' in client.ui['notice']
    client.capture('recovering-member-cannot-act')
    client.resume()
    assert client.ui['nativeDungeon']['controlledActor'] == other['id']
    assert client.ui['nativeDungeon']['activeActor'] == active
    client.capture('exact-four-member-reload')
    client.key('t'); assert client.ui['nativeDungeon']['controlledActor'] == active
    client.key('space'); client.resume()
    client.capture('continued-paid-party-action')
    # Group policy is free; actual following happens only on paid readiness.
    client.key('t')
    state = client.ui['nativeDungeon']; ready = state['activeActor']
    member = next(a for a in state['player'] if a['id'] != ready)
    client.control_click('dungeon:member:' + str(member['id']))
    client.control_click('dungeon:party')
    before = client.ui['nativeDungeon']
    client.control_click('dungeon:split')
    after = client.ui['nativeDungeon']
    for field in ['clock', 'activeActor', 'timeline', 'player']:
        assert after[field] == before[field], 'free split changed ' + field
    assert str(member['id']) not in after['following']
    client.capture('explicit-split-policy')
    client.control_click('dungeon:regroup')
    after = client.ui['nativeDungeon']
    assert len(after['following']) == 3, client.ui['notice']
    for field in ['clock', 'activeActor', 'timeline', 'player']:
        assert after[field] == before[field], 'free regroup changed ' + field
    client.capture('regroup-around-selected-member')
    client.control_click('dungeon:close'); client.key('t')
    before = client.ui['nativeDungeon']
    client.control_click('dungeon:party'); client.control_click('dungeon:lead-selected')
    after = client.ui['nativeDungeon']
    for field in ['clock', 'activeActor', 'timeline', 'player']:
        assert after[field] == before[field], 'free leadership changed ' + field
    assert set(after['following'].values()) == {after['activeActor']}
    client.capture('free-leadership-change')
    client.control_click('dungeon:close')
    acting = client.ui['nativeDungeon']['activeActor']
    initial = {a['id']: position(a['position']) for a in client.ui['nativeDungeon']['player']}
    at = initial[acting]
    options = [(at[0] + dx, at[1] + dy, at[2]) for dx, dy, _ in DIRECTIONS]
    path = route(client.ui, options)
    assert path, 'No free ordinary step beside the ready member'
    target = path[0]
    key = next(k for dx, dy, k in DIRECTIONS if (at[0] + dx, at[1] + dy, at[2]) == target)
    client.key(key)
    final = {a['id']: position(a['position']) for a in client.ui['nativeDungeon']['player']}
    assert final[acting] == target, client.ui['notice']
    assert len(set(final.values())) == 4, 'Group members overlapped'
    client.capture('paid-group-step')
    before_follow = client.ui['nativeDungeon']['following']
    client.resume()
    assert client.ui['nativeDungeon']['following'] == before_follow
    client.capture('exact-group-policy-reload')
    result = dict(recruitmentControlSaveVerified=True, groupPolicyVerified=True, iterations=iteration + 1,
        clock=client.ui['nativeDungeon']['clock'], player=client.ui['nativeDungeon']['player'],
        limits='Focused starting-room recruitment/control/build views/save check; boss, split-floor and rescue journey still pending.')
    (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    parser.add_argument('--runtime', type=Path)
    parser.add_argument('--seed', type=int, default=37)
    parser.add_argument('--class-index', type=int, default=1)
    parser.add_argument('--window-size', default='1024x720')
    args = parser.parse_args()
    client = Client(args.output.resolve(), args.seed, args.runtime, window_size=args.window_size)
    try: verify(client, args.class_index)
    finally: client.close()


if __name__ == '__main__': main()
