#!/usr/bin/env python3
"""Follow actual ordered town rests through day/night and ordinary saves.

Only visible client observations and real input drive the journey. No clock,
position, inventory, actor or outcome is injected. Long runs can buy provisions
by actually selling the starting wood. Automation stays muted.
"""
import argparse
import json
from pathlib import Path
import re

from verify_native_camping import camp
from verify_native_journey import Client


def calendar(client):
    text = client.ui['nativeDungeon']['calendar']
    match = re.match(r'Day (\d+) / (\d+):(\d+)', text)
    assert match, text
    day, hour, minute = map(int, match.groups())
    return (day - 1) * 24 * 60 + hour * 60 + minute


def market(client, closed=False):
    if client.ui['panel']:
        client.control_click('dungeon:close')
    client.control_click('dungeon:menu')
    client.control_click('dungeon:trade')
    offers = [c for c in client.ui['controls'] if c['id'].startswith('dungeon:trade-do:')]
    assert len(offers) == 8 and all(c['sprite'] for c in offers)
    if closed:
        assert all(not c['enabled'] and 'closed' in c['reason'] for c in offers), offers
    return offers


def exact_checkpoint(client, label=None):
    # A real menu pauses further automatic issuance; committed recovery stays paid.
    camp(client)
    clock = client.ui['nativeDungeon']['clock']
    needs = client.ui['nativeDungeon']['needs']
    activities = client.ui['nativeDungeon']['activities']
    client.resume()
    assert client.ui['nativeDungeon']['clock'] == clock
    assert client.ui['nativeDungeon']['needs'] == needs
    assert client.ui['nativeDungeon']['activities'] == activities
    saved = json.loads(client.slot.read_text())
    client.control_click('dungeon:save')
    assert saved == json.loads(client.slot.read_text())
    if label:
        client.capture(label)
    if client.ui['panel']:
        client.control_click('dungeon:close')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--hours', type=int, default=24)
    parser.add_argument('--seed', type=int, default=42)
    args = parser.parse_args()
    assert 24 <= args.hours <= 72
    client = Client(args.output.resolve(), args.seed, scene='world-campaign', window_size='1024x720')
    completed = 0
    try:
        client.control_click('dungeon:class:0')
        assert '06:00' in client.ui['nativeDungeon']['calendar']
        client.capture('01-dawn-town')
        if args.hours > 24:
            for _ in range(2):
                market(client)
                client.control_click('dungeon:trade-amount:10')
                client.control_click('dungeon:trade-do:Sell:wood')
            for _ in range(2):
                market(client)
                client.control_click('dungeon:trade-amount:10')
                client.control_click('dungeon:trade-do:Buy:food')
            assert client.ui['inventory']['food'] >= 28
            client.capture('02-actual-expedition-provisions')
            if client.ui['panel']:
                client.control_click('dungeon:close')
        start = calendar(client)
        start_clock = client.ui['nativeDungeon']['clock']
        for hour in range(args.hours):
            camp(client)
            control = next(c for c in client.ui['controls'] if c['id'] == 'dungeon:activity:rest:60')
            assert control['enabled'], control['reason']
            client.control_click(control['id'])
            for poll in range(5000):
                member = client.ui['nativeDungeon']['player'][0]
                assert member['life'] == 'Alive', 'The ordinary calendar journey ended in a casualty; no pass claimed'
                active = any(a['activity'] for a in client.ui['nativeDungeon']['activities'])
                if not active:
                    break
                if client.sequence >= 800:
                    exact_checkpoint(client)
                else:
                    client.request('wait', seconds=.2)
            else:
                raise AssertionError('Ordered rest exceeded the observation budget; preserve this ordinary save')
            completed = hour + 1
            assert calendar(client) >= start + completed * 60, 'The rest was interrupted early; no full-hour pass claimed'
            print('completed-hour', completed, client.ui['nativeDungeon']['calendar'],
                  'clock', client.ui['nativeDungeon']['clock'], 'food', client.ui['inventory']['food'], flush=True)
            # The market review is physical and free, at the actual calendar boundary.
            now = calendar(client) % (24 * 60)
            if completed <= 24 and now // 60 in (0, 6, 20):
                closed = now // 60 in (0, 20)
                market(client, closed)
                client.capture(f'boundary-{completed:02}-market')
                before = client.ui['nativeDungeon']['clock']
                client.request('wait', seconds=.4)
                assert client.ui['nativeDungeon']['clock'] == before
                client.control_click('dungeon:close')
            exact_checkpoint(client, f'checkpoint-{completed:02}' if completed % 12 == 0 else None)
        camp(client)
        final_clock = client.ui['nativeDungeon']['clock']
        assert final_clock >= start_clock + args.hours * 3600 * 100
        exact_checkpoint(client, 'final-exact-calendar-continuation')
        (client.output / 'result.json').write_text(json.dumps(dict(passed=True, hours=completed,
            clock=final_clock, launches=client.launches,
            checks=['actual ordered paid rests', 'shared day/night calendar', 'free physical overnight market review',
                    'finite member-owned automatic meals', 'ordinary exact partial and completed continuing saves'],
            limits='One safe-town solo calendar journey; not a natural wildlife birth/migration, season or full campaign acceptance.'), indent=2) + '\n')
    finally:
        try:
            if client.process.poll() is None:
                client.control_click('dungeon:save')
            (client.output / 'handoff.json').write_text(json.dumps(dict(hoursCompleted=completed,
                clock=client.ui['nativeDungeon']['clock'], position=client.ui['position'],
                player=client.ui['nativeDungeon']['player']), indent=2) + '\n')
        finally:
            client.close()


if __name__ == '__main__':
    main()
