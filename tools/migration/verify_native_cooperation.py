#!/usr/bin/env python3
"""Real-input muted party cooperation/recovering-helper/save checkpoint.

Use an existing naturally recruited party continuation, or recruit through the UI.
Never inject members, positions, health, inventory, resources or game outcomes.
This safe-room check does not claim rendered combat/healing-effect acceptance.
"""
import argparse
import json
from pathlib import Path
import re

from verify_native_journey import Client
from verify_native_party import verify as recruit_party


def verify(client):
    if client.continuation is None: recruit_party(client)
    state = client.ui['nativeDungeon']
    assert len([p for p in state['player'] if p['life'] == 'Alive']) >= 2
    # Free explicit splits keep the probe focused on the two paid recoveries,
    # with no follower action automatically consuming its next readiness.
    for member in state['player']:
        if member['life'] != 'Alive': continue
        client.key('Escape') if client.ui['panel'] else None
        client.key('b')
        # Party roster selection closes the panel as ordinary gameplay does.
        client.control_click('dungeon:member:' + str(member['id']))
        client.key('b'); client.control_click('dungeon:split'); client.control_click('dungeon:close')
    client.key('t')
    for _ in range(30):
        client.key('o')
        options = [c for c in client.ui['controls'] if c['enabled'] and c['id'].startswith('dungeon:cooperate-choice:') and c['id'].endswith(':2')]
        state = client.ui['nativeDungeon']
        pending = {w['actorId']: w for w in state['timeline'] if w['kind'] in (0, 'ActorReady')}
        choice = next((c for c in options if pending.get(int(c['id'].split(':')[2]), {}).get('due', state['clock']) > state['clock']), None)
        if choice: break
        client.control_click('dungeon:close'); client.key('space')
    else: raise AssertionError('No nearby recovering helper reached through ordinary party turns')
    before = client.ui['nativeDungeon']; helper = int(choice['id'].split(':')[2]); leader = before['activeActor']
    helper_before = pending[helper]
    recovery, helper_recovery = [round(float(value) * 100) for value in re.findall(r'\+(\d+\.\d+)s', choice['detail'])]
    client.capture('recovering-helper-options')
    for field in ['clock', 'player', 'activeActor', 'timeline', 'cooperation']:
        assert client.ui['nativeDungeon'][field] == before[field], field
    client.control_click(choice['id'])
    after = client.ui['nativeDungeon']
    payments = {w['actorId']: w for w in after['timeline'] if w['kind'] in (0, 'ActorReady')}
    assert payments[leader]['due'] == before['clock'] + recovery
    assert payments[helper]['due'] == helper_before['due'] + helper_recovery
    assert payments[helper]['sequence'] == helper_before['sequence']
    assert next(p for p in after['cooperation'] if p['id'] == helper)['cooperationCommitted']
    client.capture('paid-aid-extends-existing-helper-recovery')
    client.resume()
    assert client.ui['nativeDungeon']['cooperation'] == after['cooperation']
    client.control_click('dungeon:menu'); client.control_click('dungeon:settings')
    if client.ui['nativeDungeon']['presentation']['font'] != 'Sans':
        client.control_click('dungeon:setting:font')
    for _ in range(3):
        if client.ui['nativeDungeon']['presentation']['textSize'] == 'Large': break
        client.control_click('dungeon:setting:text-size')
    assert client.ui['nativeDungeon']['presentation']['textSize'] == 'Large'
    client.control_click('dungeon:close')
    # Inspect the same helper from the original acting member, without readiness.
    client.control_click('dungeon:member:' + str(leader)); client.key('o')
    client.capture('large-sans-cooperation-after-payment')
    client.control_click('dungeon:close'); client.key('t'); client.key('space')
    client.capture('party-continues-after-cooperation')
    (client.output / 'cooperation-result.json').write_text(json.dumps(dict(recoveringHelperPaid=True,
        helper=helper, leader=leader, previousDue=helper_before['due'], paidDue=payments[helper]['due'],
        helperDelay=helper_recovery, exactSaveVerified=True,
        limits='Safe-room Shared aid and free cooperation menus; combat Twin strike and actual injured-party healing remain rendered journey work.'), indent=2) + '\n')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    parser.add_argument('--continue-from', type=Path)
    args = parser.parse_args()
    client = Client(args.output.resolve(), 37, continuation=args.continue_from, window_size='1024x720')
    try: verify(client)
    finally: client.close()


if __name__ == '__main__': main()
