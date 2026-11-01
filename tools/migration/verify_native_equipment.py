#!/usr/bin/env python3
"""Muted real-input equipment browsing checkpoint, without inventory injection.

This uses the imported starting tools, never injected gear or supplies.
World acquisition and combat-affix verification remain part of the expedition journey.
"""
import argparse
import json
from pathlib import Path

from verify_native_journey import Client


def verify(client):
    client.control_click('dungeon:class:1')
    before = client.ui['nativeDungeon']
    client.key('k')
    assert client.ui['panel'] == 'dungeon-gear'
    pack = client.ui['nativeDungeon']['packEquipment']
    assert any(i['id'] == 'item.pickaxe' and i['count'] == 1 for i in pack)
    assert len([c for c in client.ui['controls'] if c['id'].startswith('dungeon:gear-slot:')]) == 4
    client.capture('starting-equipment')
    for field in ['clock', 'player', 'activeActor', 'timeline', 'equipment']:
        assert before[field] == client.ui['nativeDungeon'][field], field
    client.control_click('dungeon:gear-item:item.pickaxe')
    client.capture('pickaxe-comparison')
    for field in ['clock', 'player', 'activeActor', 'timeline', 'equipment']:
        assert before[field] == client.ui['nativeDungeon'][field], field
    client.control_click('dungeon:gear-confirm')
    assert client.ui['nativeDungeon']['clock'] > before['clock']
    assert client.ui['nativeDungeon']['equipment'][0]['slots']['tool']['item'] == 'item.pickaxe'
    assert all(i['id'] != 'item.pickaxe' for i in client.ui['nativeDungeon']['packEquipment'])
    client.key('k'); client.capture('equipped-pickaxe')
    client.control_click('dungeon:gear-slot:tool')
    client.capture('remove-pickaxe-review')
    time = client.ui['nativeDungeon']['clock']
    client.control_click('dungeon:gear-confirm')
    assert client.ui['nativeDungeon']['clock'] > time
    assert not client.ui['nativeDungeon']['equipment'][0]['slots']
    assert any(i['id'] == 'item.pickaxe' and i['count'] == 1 for i in client.ui['nativeDungeon']['packEquipment'])
    client.resume()
    before = client.ui['nativeDungeon']
    client.control_click('dungeon:menu'); client.control_click('dungeon:settings')
    if client.ui['nativeDungeon']['presentation']['font'] != 'Sans':
        client.control_click('dungeon:setting:font')
    for _ in range(3):
        if client.ui['nativeDungeon']['presentation']['textSize'] == 'Large': break
        client.control_click('dungeon:setting:text-size')
    assert client.ui['nativeDungeon']['presentation']['textSize'] == 'Large'
    client.control_click('dungeon:close'); client.control_click('dungeon:menu')
    client.capture('large-sans-menu-equipment-link')
    client.control_click('dungeon:gear')
    client.capture('large-sans-equipment')
    for field in ['clock', 'player', 'activeActor', 'timeline', 'equipment']:
        assert before[field] == client.ui['nativeDungeon'][field], field
    client.control_click('dungeon:gear-item:item.pickaxe'); client.capture('large-sans-comparison')
    client.control_click('dungeon:gear-confirm')
    assert client.ui['nativeDungeon']['clock'] > before['clock']
    before = client.ui['nativeDungeon']
    client.resume()
    assert before['equipment'] == client.ui['nativeDungeon']['equipment']
    client.key('space'); assert client.ui['nativeDungeon']['clock'] > before['clock']
    client.capture('paid-continuation-after-free-browsing')
    (client.output / 'result.json').write_text(json.dumps(dict(freeGearBrowsingVerified=True, paidEquipRemoveVerified=True,
        minimumWindow='1024x720', largeSansVerified=True, exactReloadVerified=True,
        limits='Existing starting tools only; world acquisition and combat-affix effects are not rendered acceptance yet.'), indent=2) + '\n')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    client = Client(args.output.resolve(), 37, window_size='1024x720')
    try: verify(client)
    finally: client.close()


if __name__ == '__main__': main()
