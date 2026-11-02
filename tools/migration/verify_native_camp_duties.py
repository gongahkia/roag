#!/usr/bin/env python3
"""Muted real-input camp stories/watch from an ordinary constructed-camp save.

Ingredient acquisition and paid prepared-meal eating need a separate outdoor
journey; this probe never injects ingredients, meals, injuries or camp outcomes.
"""
import argparse
import json
from pathlib import Path
from verify_native_journey import Client, distance, position
from verify_native_work import open_work, settle


def duties(c, target):
    open_work(c, target)
    c.control_click('dungeon:work-view:camp')


def open_orders(c):
    # Mouse menus remain available during continuing work; a physical key held
    # across a single slow simulation frame can miss its SDL sampling boundary.
    c.control_click('dungeon:menu'); c.control_click('dungeon:work')
    c.control_click('dungeon:work-view:orders')


def choice(c, word):
    return next(b['id'] for b in c.ui['controls'] if b['id'].startswith('dungeon:work-duty:') and word in b['label'].lower())


def verify(c):
    at = position(c.ui['position'])
    camp = next(position(v) for v in c.ui['cells'] if v['material'] == 'Camp' and distance(at, position(v)) <= 4)
    stock = dict(c.ui['inventory']); clock = c.ui['nativeDungeon']['clock']
    duties(c, camp)
    assert len([b for b in c.ui['controls'] if b['id'].startswith('dungeon:work-duty:')]) == 7
    assert c.ui['inventory'] == stock and c.ui['nativeDungeon']['clock'] == clock
    c.capture('seven-illustrated-camp-duties')
    c.control_click(choice(c, 'stor'))
    open_orders(c)
    state = c.ui['nativeDungeon']; assert state['workOrders'] and state['workOrders'][0]['kind'] == 6
    orders = state['workOrders']; activity = state['activities']; clock = state['clock']; needs = state['needs']
    c.capture('paid-partial-story-duty'); c.resume()
    assert c.ui['nativeDungeon']['workOrders'] == orders
    assert c.ui['nativeDungeon']['activities'] == activity and c.ui['nativeDungeon']['needs'] == needs
    assert c.ui['nativeDungeon']['clock'] == clock
    c.capture('partial-camp-duty-exact-reload'); c.control_click('dungeon:close'); settle(c)
    c.control_click('dungeon:save')
    story = json.loads(c.slot.read_text())['state']['lastCampStory']
    assert story and story['speaker'] == c.ui['nativeDungeon']['activeActor']
    duties(c, camp)
    repeated = next(b for b in c.ui['controls'] if b['id'] == choice(c, 'stor'))
    assert not repeated['enabled'] and 'already' in repeated.get('reason', '').lower()
    c.capture('completed-story-and-own-cooldown')
    c.control_click(choice(c, 'watch')); settle(c)
    actor = c.ui['nativeDungeon']['activeActor']
    preparation = next(p for p in c.ui['nativeDungeon']['preparations'] if p['id'] == actor)['effects']
    assert any(p['effect'] == 'watch' and p['power'] == 2 for p in preparation)
    c.request('hover', x=c.ui['position']['x'], y=c.ui['position']['y'])
    assert 'Prepared:' in c.ui['hover']['detail']
    c.capture('completed-watch-visible-preparation')
    c.resume()
    assert next(p for p in c.ui['nativeDungeon']['preparations'] if p['id'] == actor)['effects'] == preparation
    c.key('n'); c.control_click('dungeon:work-meals')
    assert len([b for b in c.ui['controls'] if b['id'].startswith('dungeon:work-eat:')]) == 4
    assert not any(b['enabled'] for b in c.ui['controls'] if b['id'].startswith('dungeon:work-eat:'))
    c.capture('four-illustrated-shared-meals-without-injected-stock')
    c.control_click('dungeon:close'); c.control_click('dungeon:menu'); c.control_click('dungeon:settings')
    if c.ui['nativeDungeon']['presentation']['font'] != 'Sans': c.control_click('dungeon:setting:font')
    for _ in range(3):
        if c.ui['nativeDungeon']['presentation']['textSize'] == 'Large': break
        c.control_click('dungeon:setting:text-size')
    duties(c, camp); c.capture('large-sans-illustrated-camp-duties')
    c.control_click('dungeon:work-view:meals'); c.capture('large-sans-prepared-meals')
    (c.output / 'result.json').write_text(json.dumps(dict(campDutiesVerified=True, story=story,
        clock=c.ui['nativeDungeon']['clock'], camp=camp,
        limits='Ordinary constructed-camp continuation: paid stories/watch, private illustrations, own cooldown, exact partial/completed saves and normal/large-sans controls. No ingredient acquisition, meal consumption, treatment or hostile work interruption is claimed.'), indent=2) + '\n')


def main():
    p = argparse.ArgumentParser(); p.add_argument('output', type=Path); p.add_argument('--continue-from', required=True, type=Path); p.add_argument('--runtime', type=Path)
    a = p.parse_args(); c = Client(a.output.resolve(), 37, runtime_source=a.runtime, continuation=a.continue_from.resolve(), window_size='1024x720')
    try: verify(c)
    finally: c.close()


if __name__ == '__main__': main()
