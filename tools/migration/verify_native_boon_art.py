#!/usr/bin/env python3
"""Reach a real boon chest, inspect illustrated choices, claim and reload.

Run under xvfb-run after building the client. Uses ordinary visible input;
the shared Client forces silent audio. No campaign state is injected.
"""
import argparse
import copy
import json
from pathlib import Path
from verify_native_journey import Client, Journey


class IllustratedClient(Client):
    art_seen = False
    offer_clock = None

    def capture(self, name):
        super().capture(name)
        if self.art_seen or self.ui['panel'] != 'dungeon-chest': return
        self.art_seen = True
        before = copy.deepcopy(self.ui['nativeDungeon'])
        self.offer_clock = before['clock']
        offers = before['chestOffers']
        assert len(offers) == 3
        assert [o['boon'] for o in offers] == [o['boon'] for o in before['boonSprites']]
        assert all(o['sprite'].startswith('roag-boon-') for o in before['boonSprites'])
        chest_id = int(name.split('-')[1])
        chest = next(c for c in before['chests'] if c['id'] == chest_id)
        self.control_click('dungeon:menu')
        self.control_click('dungeon:settings')
        self.control_click('dungeon:setting:font')
        self.control_click('dungeon:setting:text-size')
        self.control_click('dungeon:close')
        self.request('tile', x=chest['position']['x'], y=chest['position']['y'])
        assert self.ui['panel'] == 'dungeon-chest'
        now = self.ui['nativeDungeon']
        assert now['clock'] == before['clock'] and now['player'] == before['player']
        assert now['chestOffers'] == offers and now['boonSprites'] == before['boonSprites']
        assert now['presentation']['font'] == 'Sans' and now['presentation']['textSize'] == 'Large'
        super().capture('02-illustrated-choices-large')


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('output', type=Path)
parser.add_argument('--runtime', type=Path)
parser.add_argument('--owned-from', type=Path, help='Inspect every owned page in an existing isolated continuing probe slot.')
parser.add_argument('--seed', type=int, default=37)
parser.add_argument('--budget', type=int, default=600)
args = parser.parse_args()
client = IllustratedClient(args.output.resolve(), args.seed, args.runtime.resolve() if args.runtime else None,
    continuation=args.owned_from.resolve() if args.owned_from else None, window_size='1024x720')
journey = Journey(client)
try:
    if args.owned_from:
        before = copy.deepcopy(client.ui['nativeDungeon'])
        client.control_click('dungeon:menu'); client.control_click('dungeon:settings')
        client.control_click('dungeon:setting:font'); client.control_click('dungeon:setting:text-size')
        client.control_click('dungeon:close'); client.control_click('dungeon:boons')
        seen = []
        for page in range((len(before['player'][0]['boons']) + 2) // 3):
            icons = client.ui['nativeDungeon']['boonSprites']
            assert icons and all(i['sprite'].startswith('roag-boon-') for i in icons)
            seen.extend(i['boon'] for i in icons)
            client.capture('owned-page-' + str(page + 1))
            if (page + 1) * 3 < len(before['player'][0]['boons']): client.control_click('dungeon:boon-next')
        assert seen == sorted(before['player'][0]['boons'])
        assert client.ui['nativeDungeon']['clock'] == before['clock']
        assert client.ui['nativeDungeon']['player'] == before['player']
        client.resume(); client.control_click('dungeon:boons'); client.capture('owned-resumed')
        client.request('stop'); client.process.wait(timeout=15)
        (client.output / 'result.json').write_text(json.dumps(dict(passed=True, owned=len(seen),
            checks=['existing native save compatibility', 'every owned illustration/page',
                'minimum window with large sans text', 'free settings/paging', 'exact saved build continuation']), indent=2) + '\n')
        print('Existing native build and every illustrated boon page passed.', flush=True)
        raise SystemExit(0)
    client.control_click('dungeon:class:1')
    for journey.steps in range(1, args.budget + 1):
        journey.observe()
        assert journey.player['health'] > 0, 'Probe defeated before finding a chest.'
        if not (journey.fight() or journey.quiet_tasks() or journey.explore()):
            raise AssertionError('No known ordinary route to a chest.')
        if client.art_seen: break
    assert client.art_seen, 'Action budget reached without an ordinary chest claim.'
    assert client.ui['nativeDungeon']['clock'] > client.offer_clock
    client.control_click('dungeon:boons')
    illustrated = client.ui['nativeDungeon']['boonSprites']
    assert illustrated
    inventory = client.ui['nativeDungeon']['player'][0]['boons']
    assert all(b['boon'] in inventory for b in illustrated)
    clock = client.ui['nativeDungeon']['clock']
    client.capture('03-illustrated-owned-boons')
    client.control_click('dungeon:close')
    client.resume()
    client.control_click('dungeon:boons')
    assert client.ui['nativeDungeon']['boonSprites'] == illustrated
    assert client.ui['nativeDungeon']['clock'] == clock
    client.capture('04-illustrated-boons-resumed')
    client.request('stop'); client.process.wait(timeout=15)
    result = dict(passed=True, actions=journey.steps, seed=args.seed, window='1024x720',
        checks=['natural combat/exploration and paid chest claim', 'three mapped illustrated offers',
            'free font/settings/offer inspection', 'minimum window with large sans text',
            'matching owned-boon sprites', 'exact saved build and illustration continuation'])
    (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result), flush=True)
finally:
    client.close()
