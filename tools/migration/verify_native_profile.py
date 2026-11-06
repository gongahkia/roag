#!/usr/bin/env python3
"""Natural native reward unlock, final solo death and fresh-world retention; muted.

Every gameplay change uses ordinary rendered controls. Save JSON is inspected
only after input to verify receipts; no state, position, inventory or HP edits.
This is a scoped profile journey, not campaign completion or a release gate.
"""
import argparse
import json
from pathlib import Path

from verify_native_journey import Client, Journey, distance, position

UNLOCKS = {
    'secret': ('core:achievement/uncovered_truth', 'core:boon/prospectors_heart'),
    'guardian': ('core:achievement/first_guardian', 'core:boon/guardians_vow'),
}


class ProfileJourney(Journey):
    def quiet_tasks(self):
        for secret in self.state['secrets']:
            if secret['kind'] == 'MossSigil' and not secret['revealed']:
                if self.approach(position(secret['position'])):
                    return True
        return super().quiet_tasks()

    def fight(self):
        # The general journey favors ranged spacing. An Adventurer can also
        # strike diagonally; finish a visible adjacent foe instead of circling
        # forever around a skitter's repeatedly marked tiles.
        if self.player['classId'] == 'Adventurer' and self.player['health'] > self.player['maxHealth'] // 3:
            for enemy in sorted(self.fighters, key=lambda f: f['health']):
                target = position(enemy['position'])
                dx, dy = target[0] - self.at[0], target[1] - self.at[1]
                if target[2] != self.at[2] or max(abs(dx), abs(dy)) != 1 or target not in self.visible:
                    continue
                key = {(-1, -1): 'q', (1, -1): 'e', (-1, 1): 'z', (1, 1): 'c',
                       (-1, 0): 'a', (1, 0): 'd', (0, -1): 'w', (0, 1): 's'}[(dx, dy)]
                before = self.state['clock']
                self.client.key('f+' + key)
                if self.client.ui['nativeDungeon']['clock'] > before:
                    self.receipts.add('combat')
                    return True
        return super().fight()


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


def verify(client, budget, large, unlock):
    achievement, reward = UNLOCKS[unlock]
    if not client.continuation:
        client.control_click('dungeon:class:0')
    else:
        exact_resume(client, '00-continuation-exact')
    original_seed = client.ui['seed']
    original_run = profile(client)['run']
    if not client.continuation:
        assert not profile(client)['earned']
    page(client, '01-profile-continuing' if client.continuation else '01-profile-fresh-normal', all_pages=True)
    if large:
        client.control_click('dungeon:menu')
        client.control_click('dungeon:settings')
        while client.ui['nativeDungeon']['presentation']['font'] != 'Sans':
            client.control_click('dungeon:setting:font')
        while client.ui['nativeDungeon']['presentation']['textSize'] != 'Large':
            client.control_click('dungeon:setting:text-size')
        client.control_click('dungeon:close')
        page(client, '02-profile-continuing-large' if client.continuation else '02-profile-fresh-large', all_pages=True)
    journey = ProfileJourney(client, 0)
    client.profile_journey = journey
    for step in range(budget):
        journey.observe()
        if achievement in profile(client)['earned']:
            break
        assert journey.player['health'] > 0, 'The natural traveller died before earning the selected reward'
        assert journey.fight() or journey.quiet_tasks() or journey.explore(), 'No observed ordinary path to continue'
        if step % 60 == 0:
            client.capture('exploring-' + str(step))
    else:
        raise AssertionError('Natural reward discovery action budget exhausted')
    if unlock == 'secret':
        assert 'secret' in journey.receipts or any(s['revealed'] for s in client.ui['nativeDungeon']['secrets']), 'No observed secret discovery receipt'
    client.capture('03-natural-' + unlock + '-unlock')
    page(client, '04-profile-earned')
    earned = set(profile(client)['earned'])
    state = exact_resume(client, '05-unlock-exact')
    assert reward not in state['tactics']['chests']['availableBoons'], 'The existing chest pool changed mid-run'
    if unlock == 'secret':
        assert any(e['kind'] == 'SecretDiscovered' for e in state['tactics']['events']), 'No actual committed secret event'
    else:
        receipts = state['dungeon']['bossExitReceipts']
        bosses = {f['boss'] for f in state['dungeon']['floors'] if f['boss'] in receipts}
        assert any(a['id'] in bosses and a['life'] == 2 and a.get('tactics', {}).get('enemy') is not None for a in state['actors']), 'No actual defeated original biome guardian receipt'
    for step in range(budget):
        journey.observe()
        if journey.player['health'] <= 0:
            break
        if journey.fighters and min(distance(journey.at, position(f['position'])) for f in journey.fighters) <= 3:
            client.key('space')  # Deliberately face actual danger; enemies/bleeding decide the outcome.
        elif journey.fighters:
            foe = min(journey.fighters, key=lambda f: distance(journey.at, position(f['position'])))
            assert journey.go(journey.neighbors(position(foe['position']))), 'No observed approach to the visible danger'
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
    founder = next(a for a in fresh['actors'] if a['id'] == fresh['party'][0])
    assert founder['life'] == 0 and founder['tactics']['build']['level'] == 1 and founder['tactics']['build']['experience'] == 0
    assert founder['tactics']['boons']['inventory']['stacks'] == {}
    assert fresh['inventory']['gold'] == 0
    assert reward in fresh['tactics']['chests']['availableBoons']
    page(client, '09-profile-retained-new-world')
    client.capture('10-fresh-resumed')
    return dict(passed=True, scope='natural solo ' + unlock + ' unlock / final death / fresh-world profile retention',
                achievement=achievement, futureReward=reward, secretDiscovery=unlock == 'secret',
                oldSeed=original_seed, freshSeed=client.ui['seed'], oldRun=original_run,
                freshRun=profile(client)['run'], retained=sorted(retained), clock=client.ui['nativeDungeon']['clock'],
                completeCampaign=False, partyJourney=False, linuxPackage=False)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--runtime', type=Path)
    parser.add_argument('--continue-from', type=Path)
    parser.add_argument('--seed', type=int, default=42)
    parser.add_argument('--budget', type=int, default=1200)
    parser.add_argument('--unlock', choices=UNLOCKS, default='secret')
    parser.add_argument('--large-text', action='store_true')
    args = parser.parse_args()
    client = Client(args.output.resolve(), args.seed, args.runtime.resolve() if args.runtime else None,
                    args.continue_from.resolve() if args.continue_from else None, window_size='1024x720')
    try:
        result = verify(client, args.budget, args.large_text, args.unlock)
        (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
        print(json.dumps(result), flush=True)
        client.request('stop'); client.process.wait(timeout=15)
    except BaseException as error:
        result = dict(passed=False, error=str(error), errorType=type(error).__name__, rollback=False)
        try:
            journey = getattr(client, 'profile_journey', None)
            if journey is not None:
                journey.observe()
                journey.checkpoint()
            else:
                client.control_click('dungeon:save')
            client.capture('failure-current-checkpoint')
            result['ordinaryCheckpoint'] = True
            result['clock'] = client.ui['nativeDungeon']['clock']
        except Exception as checkpoint_error:
            result['checkpointError'] = str(checkpoint_error)
        (client.output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
        raise
    finally:
        client.close()


if __name__ == '__main__':
    main()
