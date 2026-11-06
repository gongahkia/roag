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
    parser.add_argument('--continue-from', type=Path)
    parser.add_argument('--runtime', type=Path)
    args = parser.parse_args()
    assert 24 <= args.hours <= 72
    previous = args.continue_from.resolve() if args.continue_from else None
    if previous:
        assert (previous / 'control/campaign.json').is_file(), 'No durable continuing save exists in that probe'
    client = Client(args.output.resolve(), args.seed, scene='world-campaign', window_size='1024x720', continuation=previous,
        runtime_source=args.runtime.resolve() if args.runtime else None)
    completed = 0
    start = start_clock = None
    try:
        if previous:
            saved = json.loads((previous / 'handoff.json').read_text())
            before = json.loads(client.slot.read_text())
            client.control_click('dungeon:save')
            assert before == json.loads(client.slot.read_text()), 'Continuation changed the world before a paid action'
            completed = saved['hoursCompleted']
            knowledge = previous / 'calendar-knowledge.json'
            if knowledge.exists():
                origin = json.loads(knowledge.read_text())
            else:
                # Earlier v1 recorded its actual dawn observation and handoff,
                # but did not yet have a separate journey-origin record.
                dawn = json.loads((previous / '01-dawn-town.json').read_text())['ui']['nativeDungeon']
                origin = dict(startClock=dawn['clock'], startCalendar=6 * 60)
            start, start_clock = origin['startCalendar'], origin['startClock']
        else:
            client.control_click('dungeon:class:0')
            assert '06:00' in client.ui['nativeDungeon']['calendar']
            client.capture('01-dawn-town')
        if args.hours > 24 and not previous:
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
        if start is None:
            start, start_clock = calendar(client), client.ui['nativeDungeon']['clock']
        (client.output / 'calendar-knowledge.json').write_text(json.dumps(dict(startClock=start_clock,
            startCalendar=start, continuation=str(previous) if previous else None), indent=2) + '\n')
        for hour in range(completed, args.hours):
            if not any(a['activity'] for a in client.ui['nativeDungeon']['activities']):
                camp(client)
                control = next(c for c in client.ui['controls'] if c['id'] == 'dungeon:activity:rest:60')
                assert control['enabled'], control['reason']
                client.control_click(control['id'])
            elif client.ui['panel']:
                client.control_click('dungeon:close')
            for poll in range(5000):
                if (client.output / 'stop-requested').exists():
                    raise SystemExit('Probe stopped at an observation boundary; preserving its ordinary continuing save')
                member = client.ui['nativeDungeon']['player'][0]
                assert member['life'] == 'Alive', 'The ordinary calendar journey ended in a casualty; no pass claimed'
                active = any(a['activity'] for a in client.ui['nativeDungeon']['activities'])
                if not active:
                    break
                if client.sequence >= 800:
                    exact_checkpoint(client)
                else:
                    client.request('wait', seconds=2)
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
        checkpoint_saved = False
        checkpoint_error = None
        try:
            if client.process.poll() is None:
                # An interrupt can arrive during a real wait. Finish that
                # request before issuing another action against its observation.
                if client.sequence:
                    pending = 'journey-' + str(client.sequence)
                    client.until(lambda: (r if (r := client.read('response.json')) and r['id'] == pending else None))
                    client.ui = client.read('observation.json')['ui']
                camp(client)
                client.control_click('dungeon:save')
                checkpoint_saved = True
        except Exception as error:
            checkpoint_error = str(error)
            print('Final checkpoint failed:', checkpoint_error, flush=True)
        finally:
            (client.output / 'handoff.json').write_text(json.dumps(dict(hoursCompleted=completed,
                clock=client.ui['nativeDungeon']['clock'], position=client.ui['position'],
                player=client.ui['nativeDungeon']['player'], checkpointSaved=checkpoint_saved,
                checkpointError=checkpoint_error), indent=2) + '\n')
            client.close()


if __name__ == '__main__':
    main()
