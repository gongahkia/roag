#!/usr/bin/env python3
"""Muted natural work controls, physical construction/crafting/digging and exact job saves."""
import argparse
import json
from pathlib import Path
from verify_native_journey import Client, distance, position


def open_work(c, target=None):
    if c.ui['panel']: c.control_click('dungeon:close')
    if target: c.request('tile', x=target[0], y=target[1], button=3)
    else: c.key('j')
    assert c.ui['panel'] == 'dungeon-work'


def recipe(c, word):
    for _ in range(10):
        matches = [b for b in c.ui['controls'] if b['id'].startswith('dungeon:work-recipe:') and word in b['label'].lower()]
        if matches:
            assert matches[0]['enabled'], matches[0]
            return matches[0]['id']
        nxt = next(b for b in c.ui['controls'] if b['id'] == 'dungeon:work-page:1')
        assert nxt['enabled'], 'No ordinary recipe for ' + word
        c.control_click(nxt['id'])
    raise AssertionError('Recipe paging exhausted')


def settle(c):
    for _ in range(150):
        if not c.ui['nativeDungeon']['workOrders']: return
        c.request('wait', seconds=.2)
    raise AssertionError('Natural work did not finish; inspect danger/blocked access, not a passing result')


def verify(c):
    c.control_click('dungeon:class:0')
    at = position(c.ui['position'])
    target = next(position(v) for v in c.ui['cells'] if distance(at, position(v)) == 1 and v['groundOpen'] and not v['occupied'] and not v['prop'] and not v['terrain'] and v['material'] == 'Floor')
    before_clock = c.ui['nativeDungeon']['clock']; initial = dict(c.ui['inventory'])
    open_work(c, target); assert position(c.ui['nativeDungeon']['workTarget']) == target
    assert c.ui['nativeDungeon']['clock'] == before_clock
    c.capture('free-right-click-work-tools')
    c.control_click('dungeon:work-view:build'); camp_recipe = recipe(c, 'camp')
    c.capture('native-construction-recipes'); c.control_click(camp_recipe)
    c.key('j'); c.control_click('dungeon:work-view:orders')
    assert c.ui['nativeDungeon']['workOrders'], 'The initial work finished before partial-save verification'
    orders = c.ui['nativeDungeon']['workOrders']; activity = c.ui['nativeDungeon']['activities']; clock = c.ui['nativeDungeon']['clock']
    assert any(o['progress'] > 0 for o in orders)
    c.capture('partial-construction-order'); c.resume()
    assert c.ui['nativeDungeon']['workOrders'] == orders
    assert c.ui['nativeDungeon']['activities'] == activity
    assert c.ui['nativeDungeon']['clock'] == clock
    c.capture('partial-work-exact-reload')
    c.control_click('dungeon:work-cancel:' + str(orders[0]['id']))
    assert c.ui['nativeDungeon']['clock'] == clock
    assert c.ui['inventory'] == initial
    assert not c.ui['nativeDungeon']['workOrders']
    c.capture('free-cancellation-full-input-refund')
    open_work(c, target); c.control_click('dungeon:work-view:build'); c.control_click(recipe(c, 'camp')); settle(c)
    cell = next(v for v in c.ui['cells'] if position(v) == target)
    assert cell['material'] == 'Camp'
    c.capture('constructed-physical-camp')
    pack = {v['id']: v['count'] for v in c.ui['nativeDungeon']['packEquipment']}
    open_work(c); c.control_click('dungeon:work-view:craft')
    choices = [v for v in c.ui['controls'] if v['id'].startswith('dungeon:work-recipe:') and v['enabled'] and 'staff' in v['label'].lower()]
    assert choices, 'No real equipment recipe can be afforded at the constructed camp'
    crafted = choices[0]['label']; c.capture('camp-equipment-recipes'); c.control_click(choices[0]['id']); settle(c)
    assert any(v['count'] > pack.get(v['id'], 0) for v in c.ui['nativeDungeon']['packEquipment'])
    c.capture('crafted-shared-equipment')
    at = position(c.ui['position'])
    walls = [position(v) for v in c.ui['cells'] if v['material'] == 'Wall' and not v['prop'] and distance(at, position(v)) <= 3]
    assert walls, 'No naturally visible ordinary wall for excavation'
    wall = min(walls, key=lambda p: distance(at,p)); stock = c.ui['inventory']['stone']
    open_work(c, wall); c.control_click('dungeon:work-action:Dig'); settle(c)
    assert next(v for v in c.ui['cells'] if position(v) == wall)['material'] == 'Floor'
    assert c.ui['inventory']['stone'] > stock
    c.capture('ordinary-wall-excavated-into-real-floor')
    c.resume(); c.control_click('dungeon:menu'); c.control_click('dungeon:settings')
    if c.ui['nativeDungeon']['presentation']['font'] != 'Sans': c.control_click('dungeon:setting:font')
    for _ in range(3):
        if c.ui['nativeDungeon']['presentation']['textSize'] == 'Large': break
        c.control_click('dungeon:setting:text-size')
    open_work(c); c.capture('large-sans-work-tools')
    (c.output/'result.json').write_text(json.dumps(dict(workVerified=True, crafted=crafted, clock=c.ui['nativeDungeon']['clock'], camp=target, excavated=wall,
        limits='Natural solo construction/crafting/excavation and partial work continuation; party takeover, hostile interruptions and outdoor resource journeys remain pending.'),indent=2)+'\n')


def main():
    p=argparse.ArgumentParser();p.add_argument('output',type=Path);p.add_argument('--runtime',type=Path);a=p.parse_args()
    c=Client(a.output.resolve(),37,runtime_source=a.runtime,window_size='1024x720')
    try: verify(c)
    finally: c.close()

if __name__ == '__main__': main()
