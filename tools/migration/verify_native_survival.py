#!/usr/bin/env python3
"""Muted survival UI, paid food/medicine and exact needs continuation via real input."""
import argparse
import json
from pathlib import Path
from verify_native_journey import Client, DIRECTIONS, distance, position
from verify_native_party import route


def verify(c):
    c.control_click('dungeon:class:1')
    assert c.ui['nativeDungeon']['survivalEnabled']
    founder = c.ui['nativeDungeon']['activeActor']
    recruit = min(c.ui['nativeDungeon']['recruits'], key=lambda a: distance(position(c.ui['position']), position(a['position'])))
    for _ in range(50):
        at, target = position(c.ui['position']), position(recruit['position'])
        if distance(at, target) == 1:
            c.request('tile', x=target[0], y=target[1]); c.control_click('dungeon:recruit'); break
        goals = {(target[0]+dx, target[1]+dy, target[2]) for dx,dy,_ in DIRECTIONS}
        path = route(c.ui, goals); assert path
        c.key(next(k for dx,dy,k in DIRECTIONS if (at[0]+dx, at[1]+dy, at[2]) == path[0]))
    else: raise AssertionError('No paid route to natural recruit')
    victim = recruit['actor']
    for member in [founder, victim]:
        c.key('b'); c.control_click('dungeon:member:' + str(member))
        c.key('b'); c.control_click('dungeon:split'); c.control_click('dungeon:close')
    c.key('t')
    for _ in range(50):
        state = c.ui['nativeDungeon']; member = next(p for p in state['player'] if p['id'] == victim)
        if member['health'] < member['maxHealth']: break
        if state['activeActor'] != founder: c.key('space'); continue
        at, target = position(c.ui['position']), position(member['position'])
        if distance(at, target) == 1:
            c.key('f+' + next(k for dx,dy,k in DIRECTIONS if (at[0]+dx,at[1]+dy,at[2]) == target))
        else:
            path = route(c.ui, {(target[0]+dx,target[1]+dy,target[2]) for dx,dy,_ in DIRECTIONS}); assert path
            c.key(next(k for dx,dy,k in DIRECTIONS if (at[0]+dx,at[1]+dy,at[2]) == path[0]))
    else: raise AssertionError('Natural friendly-fire wound not reached')
    wounded_hp = member['health']
    c.capture('actual-friendly-fire-before-food')
    # Wait with normal paid actions until hunger accrues; no body or save injection.
    for _ in range(120):
        state = c.ui['nativeDungeon']
        need = next(n for n in state['needs'] if n['id'] == state['activeActor'])
        if state['clock'] >= 1300 and need['hunger'] > 0: break
        c.key('space')
    else: raise AssertionError('Paid world needs did not advance')
    assert next(p for p in c.ui['nativeDungeon']['player'] if p['id'] == victim)['health'] == wounded_hp + 1
    c.key('n'); before = c.ui['nativeDungeon']; food = c.ui['inventory']['food']
    c.capture('survival-normal-font')
    c.control_click('dungeon:auto-eat'); c.control_click('dungeon:auto-eat-combat')
    assert c.ui['nativeDungeon']['clock'] == before['clock']
    assert c.ui['nativeDungeon']['needs'] != before['needs']
    c.control_click('dungeon:eat')
    assert c.ui['inventory']['food'] == food - 1
    assert [p['health'] for p in c.ui['nativeDungeon']['player']] == [p['health'] for p in before['player']]
    eater = before['activeActor']; need = next(n for n in c.ui['nativeDungeon']['needs'] if n['id'] == eater)
    assert need['hunger'] == 0
    c.capture('paid-hunger-only-meal')
    if c.ui['panel']: c.control_click('dungeon:close')
    # Ordinary medicine on the wounded conscious member, administered by a ready member.
    medicine_verified = False
    for _ in range(20):
        state = c.ui['nativeDungeon']; patient = next(p for p in state['player'] if p['id'] == victim)
        if patient['health'] >= patient['maxHealth']: break
        c.key('n')
        control = next((v for v in c.ui['controls'] if v['id'] == 'dungeon:medicine:' + str(victim)), None)
        if control and control.get('enabled', True):
            medicine = c.ui['inventory']['medicine']; c.control_click(control['id'])
            assert c.ui['inventory']['medicine'] == medicine - 1
            assert next(p for p in c.ui['nativeDungeon']['player'] if p['id'] == victim)['health'] > patient['health']
            medicine_verified = True
            c.capture('paid-conscious-medicine')
            if c.ui['panel']: c.control_click('dungeon:close')
            break
        c.control_click('dungeon:close'); c.key('space')
    assert medicine_verified, 'No actual paid medicine administration was verified'
    c.key('n'); needs = c.ui['nativeDungeon']['needs']; clock = c.ui['nativeDungeon']['clock']
    c.resume(); assert c.ui['nativeDungeon']['needs'] == needs
    c.control_click('dungeon:menu'); c.control_click('dungeon:settings')
    if c.ui['nativeDungeon']['presentation']['font'] != 'Sans': c.control_click('dungeon:setting:font')
    for _ in range(3):
        if c.ui['nativeDungeon']['presentation']['textSize'] == 'Large': break
        c.control_click('dungeon:setting:text-size')
    c.control_click('dungeon:close'); c.key('n'); c.capture('survival-large-sans-exact-reload')
    assert c.ui['nativeDungeon']['clock'] == clock
    (c.output/'result.json').write_text(json.dumps(dict(survivalVerified=True, clock=clock, needs=needs,
        limits='Paid natural hunger, meal, policy and exact continuation; long-duration starvation and auto-eating use backend checks, camp journey pending.'), indent=2)+'\n')


def main():
    p=argparse.ArgumentParser(); p.add_argument('output',type=Path); p.add_argument('--seed',type=int,default=1); a=p.parse_args()
    c=Client(a.output.resolve(),a.seed,window_size='1024x720')
    try: verify(c)
    finally: c.close()

if __name__ == '__main__': main()
