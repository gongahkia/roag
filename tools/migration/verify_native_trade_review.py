#!/usr/bin/env python3
"""Muted ordinary menu review; never claims an underground menu is a real market journey."""
import argparse
import json
from pathlib import Path
from verify_native_journey import Client


def trade_page(client):
    if client.ui['panel']:
        client.control_click('dungeon:close')
    client.control_click('dungeon:menu')
    client.control_click('dungeon:trade')
    offers = [c for c in client.ui['controls'] if c['id'].startswith('dungeon:trade-do:')]
    assert len(offers) == 8
    assert all(not c['enabled'] and c['sprite'] for c in offers)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    parser.add_argument('--runtime', type=Path)
    args = parser.parse_args()
    client = Client(args.output.resolve(), 37, runtime_source=args.runtime, window_size='1024x720')
    try:
        client.control_click('dungeon:class:0')
        before = client.ui['nativeDungeon']['player']
        clock = client.ui['nativeDungeon']['clock']
        trade_page(client)
        client.capture('illustrated-trade-unavailable-underground')
        client.control_click('dungeon:trade-amount:10')
        client.capture('free-quantity-review')
        assert client.ui['nativeDungeon']['clock'] == clock
        assert client.ui['nativeDungeon']['player'] == before
        client.control_click('dungeon:close')
        client.control_click('dungeon:menu')
        client.control_click('dungeon:settings')
        if client.ui['nativeDungeon']['presentation']['font'] != 'Sans':
            client.control_click('dungeon:setting:font')
        for _ in range(3):
            if client.ui['nativeDungeon']['presentation']['textSize'] == 'Large':
                break
            client.control_click('dungeon:setting:text-size')
        trade_page(client)
        client.capture('large-sans-trade-review')
        client.control_click('dungeon:close')
        client.resume()
        assert client.ui['nativeDungeon']['clock'] == clock
        assert client.ui['nativeDungeon']['player'] == before
        (client.output / 'result.json').write_text(json.dumps(dict(
            tradeReviewVerified=True, clock=clock,
            limits='Ordinary fresh underground run, atlas cards, free quantity review, disabled out-of-town offers, normal/large-sans layouts and exact save. Actual paid outdoor market trading remains a required world journey.'
        ), indent=2) + '\n')
    finally:
        client.close()


if __name__ == '__main__':
    main()
