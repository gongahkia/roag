#!/usr/bin/env python3
"""Muted real-input sustained watch/rest and exact partial-order continuation."""
import argparse
import json
from pathlib import Path
from verify_native_journey import Client


def camp(c):
    if c.ui['panel']: c.control_click('dungeon:close')
    c.key('n'); c.control_click('dungeon:camp')


def verify(c):
    c.control_click('dungeon:class:0'); camp(c)
    c.capture('camp-normal-controls')
    before = c.ui['nativeDungeon']['clock']
    c.control_click('dungeon:activity:watch:10')
    for _ in range(120):
        if not any(a['activity'] for a in c.ui['nativeDungeon']['activities']): break
        c.request('wait', seconds=.2)
    else: raise AssertionError('Watch did not reach its finite ordered duration')
    state = c.ui['nativeDungeon']; assert state['clock'] >= before + 60000
    watch_fatigue = state['needs'][0]['fatigue']
    assert watch_fatigue > 0
    c.capture('paid-watch-completed-with-fatigue')
    camp(c); c.control_click('dungeon:activity:rest:60')
    # Opening inspection suspends only automatic issuance; prior recovery stays paid.
    c.key('n'); camp(c); c.capture('partial-rest-paused-for-inspection')
    state = c.ui['nativeDungeon']; clock = state['clock']; activity = state['activities']; needs = state['needs']
    assert activity[0]['activity']['kind'] == 0
    assert needs[0]['fatigue'] < watch_fatigue
    c.request('wait', seconds=.3); assert c.ui['nativeDungeon']['clock'] == clock
    c.resume()
    assert c.ui['nativeDungeon']['activities'] == activity
    assert c.ui['nativeDungeon']['needs'] == needs
    assert c.ui['panel'] == 'dungeon-camp'
    c.capture('partial-rest-exact-reload')
    c.control_click('dungeon:activity-cancel')
    assert c.ui['nativeDungeon']['clock'] == clock
    assert c.ui['nativeDungeon']['activities'][0]['activity'] is None
    c.control_click('dungeon:close'); c.request('wait', seconds=.3)
    assert c.ui['nativeDungeon']['clock'] == clock
    c.control_click('dungeon:menu'); c.control_click('dungeon:settings')
    if c.ui['nativeDungeon']['presentation']['font'] != 'Sans': c.control_click('dungeon:setting:font')
    for _ in range(3):
        if c.ui['nativeDungeon']['presentation']['textSize'] == 'Large': break
        c.control_click('dungeon:setting:text-size')
    camp(c); c.capture('camp-large-sans-controls')
    (c.output/'result.json').write_text(json.dumps(dict(campVerified=True, clock=clock, limits='Natural solo safe-room watch/rest/control/save journey. Hostile camp interruption and four-member rendered journey remain pending.'), indent=2)+'\n')


def main():
    p=argparse.ArgumentParser();p.add_argument('output',type=Path);a=p.parse_args()
    c=Client(a.output.resolve(),37,window_size='1024x720')
    try:verify(c)
    finally:c.close()

if __name__ == '__main__':main()
