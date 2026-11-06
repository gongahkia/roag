#!/usr/bin/env python3
"""Natural native secret unlock, final solo death and fresh-world retention; muted.

Every gameplay change uses ordinary rendered controls. Save JSON is inspected
only after input to verify receipts; no state, position, inventory or HP edits.
This is a scoped profile journey, not campaign completion or a release gate.
"""
import argparse
import json
from pathlib import Path

from verify_native_journey import Client, Journey

SECRET = 'core:achievement/uncovered_truth'


def profile(client):
    value = client.ui['nativeDungeon']['profile']
    assert value is not None, 'The isolated new native run was not enrolled'
    return value


def page(client, label, all_pages=False):
    before = client.ui['nativeDungeon']['clock']
    client.control_click('dungeon:menu')
    client.control_click('dungeon:profile')
    assert client.ui['panel'] == 'dungeon-profile'
    client.capture(label)
    if all_pages:
        for _ in range(7):
            client.control_click('dungeon:profile-next')
        client.capture(label + '-last-page')
    client.request('wait', seconds=.4)
    assert before == client.ui['nativeDungeon']['clock'], 'Achievement browsing consumed time'
    client.control_click('dungeon:close')


def exact_resume(client, label):
    client.control_click('dungeon:save')
    before = json.loads(client.slot.read_text())
    (client.output / (label + '-campaign.json')).write_text(json.dumps(before, indent=2) + '\n')
    client.resume()
    client.control_click('dungeon:save')
    assert before == json.loads(client.slot.read_text()), 'Ordinary continuing save changed on reload'
    return before['state']


def verify(client, budget, large):
    client.control_click('dungeon:class:0')
    original_seed = client.ui['seed']
    original_run = profile(client)['run']
    assert not profile(client)['earned']
    page(client, '01-profile-fresh-normal', all_pages=True)
    if large:
        client.control_click('dungeon:menu')
        client.control_click('dungeon:settings')
        while client.ui['nativeDungeon']['presentation']['font'] != 'Sans':
            client.control_click('dungeon:setting:font')
        while client.ui['nativeDungeon']['presentation']['textSize'] != 'Large':
            client.control_click('dungeon:setting:text-size')
        client.control_click('dungeon:close')
        page(client, '02-profile-fresh-large', all_pages=True)
    journey = Journey(client, 0)
    for step in range(budget):
        journey.observe()
        if SECRET in profile(client)['earned']:
            break
        assert journey.player['health'] > 0, 'The natural traveller died before discovering the secret'
        assert journey.fight() or journey.quiet_tasks() or journey.explore(), 'No observed ordinary path to continue'
        if step % 60 == 0:
            client.capture('exploring-' + str(step))
    else:
        raise AssertionError('Natural secret discovery action budget exhausted')
    assert 'secret' in journey.receipts, 'The secret was not revealed by the ordinary action driver'
    client.capture('03-real-secret-unlock')
    page(client, '04-profile-earned')
    earned = set(profile(client)['earned'])
    state = exact_resume(client, '05-unlock-exact')
    assert 'core:boon/prospectors_heart' not in state['tactics']['chests']['availableBoons'], 'The existing chest pool changed mid-run'
    for step in range(budget):
        journey.observe()
        if journey.player['health'] <= 0:
            break
        if journey.fighters:
            client.key('space')  # Deliberately face actual danger; enemies/bleeding decide the outcome.
        else:
            assert journey.explore(), 'No observed route to encounter danger'
        if step % 60 == 0:
            client.capture('danger-' + str(step))
    else:
        raise AssertionError('Natural final-death action budget exhausted')
    terminal = exact_resume(client, '06-terminal-exact')
    assert terminal['awaitingContinuation']
    assert all(a['life'] == 2 for a in terminal['actors'] if a['id'] in terminal['party'])
    assert earned <= set(profile(client)['earned'])
    client.capture('07-real-terminal-resumed')
    retained = set(profile(client)['earned'])
    client.control_click('dungeon:new')
    assert client.ui['seed'] != original_seed
    assert client.ui['panel'] == 'dungeon-class'
    assert client.ui['nativeDungeon']['clock'] == 0
    assert profile(client)['run'] > original_run
    assert retained <= set(profile(client)['earned'])
    client.control_click('dungeon:class:0')
    fresh = exact_resume(client, '08-fresh-exact')
    assert len(fresh['party']) == 1 and not fresh['awaitingContinuation']
    assert fresh['tactics']['profile']['earned'] == []
    assert 'core:boon/prospectors_heart' in fresh['tactics']['chests']['availableBoons']
    page(client, '09-profile-retained-new-world')
    client.capture('10-fresh-resumed')
    return dict(passed=True, scope='natural solo secret unlock / final death / fresh-world profile retention',
                oldSeed=original_seed, freshSeed=client.ui['seed'], oldRun=original_run,
                freshRun=profile(client)['run'], retained=sorted(retained), clock=client.ui['nativeDungeon']['clock'],
                completeCampaign=False, partyJourney=False, linuxPackage=False)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--runtime', type=Path)
    parser.add_argument('--seed', type=int, default=42)
    parser.add_argument('--budget', type=int, default=1200)
    parser.add_argument('--large-text', action='store_true')
    args = parser.parse_args()
    client = Client(args.output.resolve(), args.seed, args.runtime.resolve() if args.runtime else None,
                    window_size='1024x720')
    try:
        result = verify(client, args.budget, args.large_text)
        (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
        print(json.dumps(result), flush=True)
        client.request('stop'); client.process.wait(timeout=15)
    except Exception as error:
        (client.output / 'result.json').write_text(json.dumps(dict(passed=False, error=str(error)), indent=2) + '\n')
        raise
    finally:
        client.close()


if __name__ == '__main__':
    main()
