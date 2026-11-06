#!/usr/bin/env python3
"""Verify real town purchases and paid eating across the regional/pack boundary.

Only ordinary controls and privately visible needs determine inputs. Saved
manifests are read afterward for accounting; none are changed or injected.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

from verify_native_calendar import market
from verify_native_journey import Client


def saved(client, label):
    client.control_click('dungeon:save')
    path = client.output / (label + '-campaign.json')
    path.write_bytes(client.slot.read_bytes())
    return json.loads(path.read_text())['state']


def account(state):
    # These natural town journeys create no work. Do not quietly apply the
    # shorter stock formula to saves with carrying/output reservations.
    assert not state['work']
    stock = sum(sum(t['stocks'].values()) for t in state['towns'])
    stock += sum(sum(a['cargo'].values()) for a in state['actors'])
    pack = state['nativeCampaign'].get('packResourcesConsumed', 0)
    flow = state['resourcesProduced'] - (state['resourcesConsumed'] - pack) - state['resourcesLost']
    flow += state['resourcesImported'] - state['resourcesExported']
    return dict(stock=stock, flow=flow, consumed=state['resourcesConsumed'], packConsumed=pack,
                food=state['inventory']['food'], exported=state['resourcesExported'])


def verify(client, previous=None):
    if previous:
        before = json.loads(client.slot.read_text())
        client.control_click('dungeon:save')
        assert before == json.loads(client.slot.read_text()), 'Continuation changed the campaign before paid input'
        origin = previous / '01-before-purchases-campaign.json'
        shutil.copy2(origin, client.output / origin.name)  # Audit evidence only; never loaded as a rollback.
        initial = account(json.loads(origin.read_text())['state'])
        knowledge = previous / 'accounting-knowledge.json'
        if knowledge.exists():
            progress = json.loads(knowledge.read_text())
        else:
            # v1 stopped after an ordinary purchase. Recover only the driver's
            # recorded successful input receipts, never hidden actor/world data.
            receipts = [json.loads(line)['input'].get('control') for line in (previous / 'inputs.jsonl').read_text().splitlines()]
            progress = dict(purchases=receipts.count('dungeon:trade-do:Buy:food'), ate='dungeon:eat' in receipts)
    else:
        client.control_click('dungeon:class:0')
        initial = account(saved(client, '01-before-purchases'))
        progress = dict(purchases=0, ate=False)
    def remember():
        (client.output / 'accounting-knowledge.json').write_text(json.dumps(progress, indent=2) + '\n')
    remember()
    assert 0 <= progress['purchases'] <= 2
    for _ in range(progress['purchases'], 2):
        market(client)  # Every committed purchase closes the trade page.
        food = client.ui['inventory']['food']
        client.control_click('dungeon:trade-do:Buy:food')
        assert client.ui['inventory']['food'] == food + 1
        progress['purchases'] += 1
        remember()
    client.capture('02-actual-paid-purchases')
    if client.ui['panel']:
        client.control_click('dungeon:close')
    if not progress['ate']:
        for _ in range(60):
            active = client.ui['nativeDungeon']['activeActor']
            need = next(n for n in client.ui['nativeDungeon']['needs'] if n['id'] == active)
            if need['hunger'] > 0:
                break
            client.key('space')
        else:
            raise AssertionError('Ordinary paid waits did not create visible hunger')
        client.key('n')
        food = client.ui['inventory']['food']
        clock = client.ui['nativeDungeon']['clock']
        client.control_click('dungeon:eat')
        assert client.ui['inventory']['food'] == food - 1
        assert client.ui['nativeDungeon']['clock'] > clock
        progress['ate'] = True
        remember()
    client.capture('03-actual-paid-meal')
    final = account(saved(client, '04-after-meal'))
    assert final['food'] == initial['food'] + 1
    assert final['exported'] - initial['exported'] == 2
    assert final['packConsumed'] - initial['packConsumed'] == 1
    assert final['consumed'] - initial['consumed'] >= 1
    assert final['stock'] - initial['stock'] == final['flow'] - initial['flow'], (initial, final)
    before = json.loads(client.slot.read_text())
    client.resume()
    client.control_click('dungeon:save')
    assert before == json.loads(client.slot.read_text()), 'Ordinary reload changed the saved campaign'
    client.capture('05-exact-continuing-accounting')
    result = dict(passed=True, initial=initial, final=final, clock=client.ui['nativeDungeon']['clock'],
                  limits='Natural solo purchase/meal/accounting/exact-save check; not a new multi-day economy, ecology or full campaign pass',
                  runtimeHashes={p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in client.runtime.glob('Roag.*.dll')})
    (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--runtime', type=Path)
    parser.add_argument('--continue-from', type=Path)
    args = parser.parse_args()
    previous = args.continue_from.resolve() if args.continue_from else None
    client = Client(args.output.resolve(), 42, scene='world-campaign', window_size='1024x720', continuation=previous,
                    runtime_source=args.runtime.resolve() if args.runtime else None)
    checkpoint = dict(checkpointSaved=False, checkpointError=None)
    try:
        verify(client, previous)
    finally:
        try:
            pending = client.read('request.json')
            if pending:
                client.finish(pending)
            client.control_click('dungeon:save')
            checkpoint['checkpointSaved'] = True
            checkpoint.update(position=client.ui['position'], clock=client.ui['nativeDungeon']['clock'],
                              player=client.ui['nativeDungeon']['player'])
        except Exception as error:
            checkpoint['checkpointError'] = str(error)
        (client.output / 'handoff.json').write_text(json.dumps(checkpoint, indent=2) + '\n')
        client.close()


if __name__ == '__main__':
    main()
