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

from verify_native_journey import Client, DIRECTIONS, distance, position
from verify_native_party import route


def step(client, path):
    at = position(client.ui['position']); target = path[0]
    key = next(k for dx, dy, k in DIRECTIONS if (at[0] + dx, at[1] + dy, at[2]) == target)
    client.key(key)


def verify(client):
    client.control_click('dungeon:class:1')
    recruits = client.ui['nativeDungeon']['recruits']
    pairs = [(medic, member) for medic in recruits if medic['medicine'] > 0
             for member in recruits if member['actor'] != medic['actor']
             and distance(position(medic['position']), position(member['position'])) <= 5]
    assert pairs, 'This seed has no nearby natural medicine carrier/recruit pair; no rescue was invented.'
    medic, candidate = min(pairs, key=lambda pair: (distance(position(pair[0]['position']), position(pair[1]['position'])), pair[1]['actor']))
    founder = client.ui['nativeDungeon']['activeActor']
    for _ in range(40):
        target = position(candidate['position']); at = position(client.ui['position'])
        if distance(at, target) == 1:
            client.request('tile', x=target[0], y=target[1]); client.capture('natural-recruit-before-rescue')
            client.control_click('dungeon:recruit'); break
        goals = {(target[0] + dx, target[1] + dy, target[2]) for dx, dy, _ in DIRECTIONS}
        path = route(client.ui, goals); assert path, 'No ordinary route to the recruit'
        step(client, path)
    else: raise AssertionError('Recruitment movement budget exhausted')
    victim = candidate['actor']
    assert len(client.ui['nativeDungeon']['player']) == 2
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
    client.capture('npc-notices-real-downed-companion'); client.resume()
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
    assert any(r['actor'] == helpers[0]['id'] for r in client.ui['nativeDungeon']['recruits'])
    client.capture('npc-paid-physical-revival'); client.resume(); client.capture('npc-rescue-exact-reload')
    clock = client.ui['nativeDungeon']['clock']
    client.control_click('dungeon:menu'); client.control_click('dungeon:settings')
    if client.ui['nativeDungeon']['presentation']['font'] != 'Sans': client.control_click('dungeon:setting:font')
    for _ in range(3):
        if client.ui['nativeDungeon']['presentation']['textSize'] == 'Large': break
        client.control_click('dungeon:setting:text-size')
    client.control_click('dungeon:close')
    helper = next(r for r in client.ui['nativeDungeon']['recruits'] if r['actor'] == helpers[0]['id'])
    client.request('tile', x=helper['position']['x'], y=helper['position']['y'])
    assert client.ui['panel'] == 'dungeon-recruit'
    assert client.ui['nativeDungeon']['clock'] == clock
    client.capture('large-sans-existing-rescuer-recruitment'); client.control_click('dungeon:close')
    (client.output / 'result.json').write_text(json.dumps(dict(npcRescueVerified=True, friendlyFireShots=shots,
        victim=victim, helper=helpers[0]['id'], clock=client.ui['nativeDungeon']['clock'],
        limits='Natural safe-room NPC medicine and two-member friendly-fire casualty; hostile expedition rescue remains pending.'), indent=2) + '\n')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    parser.add_argument('--seed', type=int, default=1)
    args = parser.parse_args()
    client = Client(args.output.resolve(), args.seed, window_size='1024x720')
    try: verify(client)
    finally: client.close()


if __name__ == '__main__': main()
