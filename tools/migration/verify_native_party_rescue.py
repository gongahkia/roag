#!/usr/bin/env python3
"""Verify a natural downing, paid rescue feedback and exact continuing save; muted.

Starts from an ordinary four-member checkpoint. Uses only observed map data,
actual X11 input and normal save/load; does not inject combat outcomes.
"""
import argparse
import json
from pathlib import Path
from verify_native_journey import Client
from verify_native_party_journey import PartyJourney


class RescueJourney(PartyJourney):
    def rescue(self):
        already = self.rescued
        moved = super().rescue()
        if self.rescued and not already:
            (self.client.output / 'pause-request').write_text('Focused rescue verified; stop further exploration.\n')
        return moved


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--continue-from', type=Path, required=True)
    parser.add_argument('--runtime', type=Path)
    parser.add_argument('--budget', type=int, default=600)
    args = parser.parse_args()
    client = Client(args.output.resolve(), 37, args.runtime.resolve() if args.runtime else None,
        continuation=args.continue_from.resolve(), window_size='1280x800')
    try:
        journey = RescueJourney(client, True)
        assert not journey.rescued, 'Use a checkpoint before its first rescue.'
        outcome = journey.run_party(args.budget)
        verified = journey.rescued and outcome == 'paused'
        result = dict(rescueFeedbackVerified=verified, outcome=outcome, clock=client.ui['nativeDungeon']['clock'],
            steps=journey.steps, limits='Focused rescue feedback/save check; not the complete boss/split/ownership journey.')
        (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
        assert verified, result
    finally:
        client.close()


if __name__ == '__main__': main()
