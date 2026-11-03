#!/usr/bin/env python3
"""Muted natural NPC conversation/rumour journey, with ordinary input and saves."""
import argparse
from collections import deque
import json
from pathlib import Path
from verify_native_journey import Client, DIRECTIONS, position, distance


def finish_speech(client):
    for _ in range(100):
        if client.ui['nativeDungeon']['social']['finished']:
            return
        client.control_click('dungeon:social-next')
    raise AssertionError('Dialogue did not finish; not a pass')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    client = Client(args.output.resolve(), 42, window_size='1024x720', scene='world-campaign')
    known = {}
    try:
        client.control_click('dungeon:class:1')
        client.key('Tab')
        person = None
        for _ in range(20):
            ui = client.ui
            origin = position(ui['position'])
            for cell in ui['cells']:
                known[position(cell)] = cell['groundOpen']
            people = [a for a in ui['actors'] if a['kind'] == 'Person' and a['id'] < 80000]
            assert people, 'No actual resident is visible'
            person = min(people, key=lambda a: distance(origin, position(a)))
            if distance(origin, position(person)) <= 1:
                break
            blocked = {position(a) for a in ui['actors']}
            parents = {origin: None}
            queue = deque([origin])
            while queue:
                p = queue.popleft()
                for dx, dy, key in DIRECTIONS:
                    n = (p[0] + dx, p[1] + dy, p[2])
                    if n in parents or not known.get(n) or n in blocked:
                        continue
                    parents[n] = (p, key)
                    queue.append(n)
            goals = [p for p in parents if distance(p, position(person)) <= 1]
            assert goals, 'No observed path beside the resident'
            cursor = min(goals, key=lambda p: distance(origin, p))
            while parents[cursor][0] != origin:
                cursor = parents[cursor][0]
            client.key(parents[cursor][1])
        else:
            raise AssertionError('Resident approach exhausted its budget; not a pass')
        client.request('actor', actor=person['id'])
        assert client.ui['panel'] == 'dungeon-social'
        before = client.ui['nativeDungeon']['clock']
        client.capture('01-native-speakers')
        finish_speech(client)
        assert client.ui['nativeDungeon']['clock'] == before
        client.capture('02-free-dialogue-pages-and-choices')
        client.control_click('dungeon:social:interaction.stories')
        assert client.ui['nativeDungeon']['clock'] > before
        finish_speech(client)
        client.capture('03-paid-stories')
        for _ in range(4):
            before = client.ui['nativeDungeon']['clock']
            client.control_click('dungeon:social:interaction.rumours')
            assert client.ui['nativeDungeon']['clock'] > before
            finish_speech(client)
        discoveries = client.ui['discoveries']
        assert discoveries
        assert all(position(d) not in known for d in discoveries), 'A rumour revealed actual distant terrain'
        client.capture('04-paid-rumour-metadata')
        clock = client.ui['nativeDungeon']['clock']
        client.request('wait', seconds=1)
        assert client.ui['nativeDungeon']['clock'] == clock
        client.key('Escape')
        client.control_click('dungeon:menu')
        client.control_click('dungeon:settings')
        if client.ui['nativeDungeon']['presentation']['font'] != 'Sans':
            client.control_click('dungeon:setting:font')
        for _ in range(3):
            if client.ui['nativeDungeon']['presentation']['textSize'] == 'Large':
                break
            client.control_click('dungeon:setting:text-size')
        client.control_click('dungeon:close')
        client.request('actor', actor=person['id'])
        finish_speech(client)
        client.capture('05-large-sans-conversation')
        assert client.ui['nativeDungeon']['clock'] == clock
        client.key('Escape')
        client.resume()
        saved = json.loads(client.slot.read_text())
        client.control_click('dungeon:save')
        assert saved == json.loads(client.slot.read_text())
        assert discoveries == client.ui['discoveries']
        (client.output / 'world-knowledge.json').write_text(json.dumps(dict(
            townOrigin=position(client.ui['position']), known=[dict(position=p, open=v) for p, v in known.items()], visits=[]
        ), indent=2) + '\n')
        (client.output / 'result.json').write_text(json.dumps(dict(passed=True, person=person['id'], clock=clock,
            discoveries=[d['id'] for d in discoveries],
            checks=['ordinary physical approach', 'free voiced portrait pages', 'paid actual speaker stories and rumours',
                    'no distant terrain revelation', 'thinking consumes no time', 'minimum normal/large-sans layout',
                    'exact ordinary continuing save']), indent=2) + '\n')
    finally:
        client.close()


if __name__ == '__main__':
    main()
