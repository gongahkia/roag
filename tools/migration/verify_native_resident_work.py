#!/usr/bin/env python3
"""Build and delegate a real town order using ordinary native inputs and saves."""
import argparse
import json
from pathlib import Path
from verify_native_journey import Client, DIRECTIONS, position, distance
from verify_native_work import open_work, recipe


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--large-text', action='store_true')
    args = parser.parse_args()
    client = Client(args.output.resolve(), 42, window_size='1024x720', scene='world-campaign')
    try:
        client.control_click('dungeon:class:0')
        client.key('Tab')
        if args.large_text:
            client.control_click('dungeon:menu')
            client.control_click('dungeon:settings')
            if client.ui['nativeDungeon']['presentation']['font'] != 'Sans':
                client.control_click('dungeon:setting:font')
            for _ in range(3):
                if client.ui['nativeDungeon']['presentation']['textSize'] == 'Large':
                    break
                client.control_click('dungeon:setting:text-size')
            client.control_click('dungeon:close')
        for _ in range(20):
            origin = position(client.ui['position'])
            people = [a for a in client.ui['actors'] if a['kind'] == 'Person' and a['id'] < 80000]
            choices = [(a, p) for a in people if distance(origin, position(a)) <= 2
                       for cell in client.ui['cells'] if (p := position(cell)) and distance(origin, p) == 1
                       and distance(position(a), p) == 1 and cell['groundOpen'] and not cell['occupied']
                       and not cell['prop'] and not cell['terrain'] and not cell['resource']]
            if choices:
                break
            assert people, 'No observed resident to approach'
            person = min(people, key=lambda a: distance(origin, position(a)))
            steps = [cell for cell in client.ui['cells'] if distance(origin, position(cell)) == 1
                     and cell['groundOpen'] and not cell['occupied']
                     and distance(position(cell), position(person)) < distance(origin, position(person))]
            assert steps, 'No observed step approaching the actual resident'
            step = min(steps, key=lambda c: distance(position(c), position(person)))
            client.key(next(key for dx, dy, key in DIRECTIONS
                            if (origin[0] + dx, origin[1] + dy, origin[2]) == position(step)))
        else:
            raise AssertionError('No actual resident and shared work site after paid approach')
        resident, target = choices[0]
        stone = client.ui['inventory']['stone']
        open_work(client, target)
        client.control_click('dungeon:work-view:build')
        client.control_click(recipe(client, 'wall'))
        open_work(client)
        client.control_click('dungeon:work-view:orders')
        order = next(o for o in client.ui['nativeDungeon']['workOrders'] if position(o['target']) == target)
        before = client.ui['nativeDungeon']['clock']
        client.capture('01-native-order-with-delegation')
        client.control_click('dungeon:work-delegate:' + str(order['id']))
        client.capture('02-free-illustrated-resident-choices')
        assert client.ui['nativeDungeon']['clock'] == before
        client.control_click('dungeon:work-resident:' + str(resident['id']))
        order = next(o for o in client.ui['nativeDungeon']['workOrders'] if o['id'] == order['id'])
        assert order['worker'] == resident['id'] and client.ui['nativeDungeon']['clock'] > before
        assert all(a['activity'] is None for a in client.ui['nativeDungeon']['activities'])
        assert client.ui['inventory']['stone'] == stone - 2
        open_work(client)
        client.control_click('dungeon:work-view:orders')
        client.capture('03-paid-delegated-order')
        clock = client.ui['nativeDungeon']['clock']
        client.resume()
        assert client.ui['nativeDungeon']['clock'] == clock
        assert next(o for o in client.ui['nativeDungeon']['workOrders'] if o['id'] == order['id'])['worker'] == resident['id']
        pending = json.loads(client.slot.read_text())
        client.control_click('dungeon:save')
        assert pending == json.loads(client.slot.read_text())
        if client.ui['panel']:
            client.control_click('dungeon:close')
        for step in range(300):
            if not any(o['id'] == order['id'] for o in client.ui['nativeDungeon']['workOrders']):
                break
            client.key('space')
        else:
            raise AssertionError('Natural resident work did not complete within its budget; not a pass')
        assert next(c for c in client.ui['cells'] if position(c) == target)['material'] == 'Wall'
        assert client.ui['inventory']['stone'] == stone - 2
        client.capture('04-physical-resident-construction-complete')
        client.resume()
        saved = json.loads(client.slot.read_text())
        client.control_click('dungeon:save')
        assert saved == json.loads(client.slot.read_text())
        (client.output / 'result.json').write_text(json.dumps(dict(passed=True, resident=resident['id'], target=target,
            largeText=args.large_text,
            paidAssignmentBefore=before, paidAssignmentAfter=clock, clock=client.ui['nativeDungeon']['clock'], waitInputs=step,
            checks=['ordinary town construction', 'free illustrated order/resident browsing', 'paid local NPC assignment',
                    'unchanged supply reservation', 'ordinary delegated save/reload', 'resident completes physical terrain',
                    'no automatic party work after delegation', 'exact completed continuing save']), indent=2) + '\n')
    finally:
        client.close()


if __name__ == '__main__':
    main()
