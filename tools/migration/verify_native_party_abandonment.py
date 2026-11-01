#!/usr/bin/env python3
"""Verify confirmed remote abandonment after natural downing; real input, muted.

Uses an ordinary four-member checkpoint and observed navigation/combat only.
Cancellation, safe default, large text, permanent death and exact continuation
are checked without injecting HP, positions, supplies or outcomes.
"""
import argparse
import copy
import json
from pathlib import Path
from verify_native_journey import Client, distance, position
from verify_native_party_journey import PartyJourney


class AbandonmentJourney(PartyJourney):
    abandoned = False

    def rescue(self):
        targets = [a for a in self.state['player'] if a['life'] == 'Downed']
        if not targets:
            return False
        target = min(targets, key=lambda a: a['bleedTurns'])
        # Spend the adjacent member's actual Wait so a distant member demonstrates
        # the approved remote action, without walking/teleporting the casualty.
        if distance(self.at, position(target['position'])) <= 1:
            self.client.key('space')
            return True
        self.client.capture('remote-member-before-abandonment')
        players = copy.deepcopy(self.state['player']); clock = self.state['clock']
        inventory = copy.deepcopy(self.ui['inventory'])
        self.client.key('b'); self.client.control_click('dungeon:abandon:' + str(target['id']))
        assert self.client.ui['panel'] == 'dungeon-abandon-confirm'
        assert self.client.ui['nativeDungeon']['player'] == players and self.client.ui['nativeDungeon']['clock'] == clock
        self.client.capture('abandonment-confirmation')
        self.client.key('Return')
        assert self.client.ui['panel'] == 'dungeon-party', 'Confirmation must default to keeping the companion.'
        assert self.client.ui['nativeDungeon']['player'] == players and self.client.ui['nativeDungeon']['clock'] == clock
        self.client.capture('safe-default-cancelled')
        self.client.control_click('dungeon:close')
        self.client.control_click('dungeon:menu'); self.client.control_click('dungeon:settings')
        for setting in ['font', 'text-size']:
            self.client.control_click('dungeon:setting:' + setting)
        self.client.control_click('dungeon:close'); self.client.key('b')
        self.client.control_click('dungeon:abandon:' + str(target['id']))
        self.client.capture('large-sans-abandonment-confirmation')
        assert self.client.ui['nativeDungeon']['player'] == players and self.client.ui['nativeDungeon']['clock'] == clock
        self.client.control_click('dungeon:abandon-confirm')
        current = self.client.ui['nativeDungeon']
        corpse = next(a for a in current['player'] if a['id'] == target['id'])
        assert corpse['life'] == 'Dead' and corpse['health'] == 0 and corpse['bleedTurns'] == 0
        assert corpse['boons'] == target['boons'] and corpse['position'] == target['position']
        assert self.client.ui['inventory'] == inventory and current['clock'] > clock
        assert any(a['life'] == 'Alive' for a in current['player'])
        self.client.capture('abandonment-permanent-death')
        self.checkpoint(); self.client.resume(); self.client.capture('abandonment-exact-reload')
        self.client.key('space'); self.checkpoint(); self.client.resume()
        self.client.capture('survivors-paid-continuation')
        self.abandoned = True
        (self.client.output / 'pause-request').write_text('Focused abandonment check completed.\n')
        return True


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--continue-from', type=Path, required=True)
    parser.add_argument('--runtime', type=Path)
    parser.add_argument('--budget', type=int, default=600)
    args = parser.parse_args()
    client = Client(args.output.resolve(), 37, args.runtime.resolve() if args.runtime else None,
        continuation=args.continue_from.resolve(), window_size='1024x720')
    try:
        journey = AbandonmentJourney(client, True)
        outcome = journey.run_party(args.budget)
        result = dict(abandonmentVerified=journey.abandoned and outcome == 'paused', outcome=outcome,
            clock=client.ui['nativeDungeon']['clock'], steps=journey.steps,
            limits='Focused remote abandonment, cancellation, presentation and exact continuing saves; not a full boss journey.')
        (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
        assert result['abandonmentVerified'], result
    finally:
        client.close()


if __name__ == '__main__': main()
