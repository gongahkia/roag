#!/usr/bin/env python3
"""Muted natural Ranger boon acquisition, skill review/cast and exact continuation."""
import argparse
import hashlib
import json
import shutil
from pathlib import Path
from verify_native_journey import Client, Journey


class UnlockReached(Exception):
    pass


class SkillClient(Client):
    wanted = 'core:boon/expedition_volley'
    unlocked = False

    def control_click(self, name):
        if not self.unlocked and name.startswith('dungeon:claim:') and self.ui['panel'] == 'dungeon-chest':
            offers = self.ui['nativeDungeon']['chestOffers']
            chosen = next((i for i, offer in enumerate(offers) if offer['boon'] == self.wanted), None)
            if chosen is not None:
                self.capture('natural-class-eligible-skill-boon-offer')
                result = super().control_click('dungeon:claim:' + str(chosen + 1))
                assert self.wanted in self.ui['nativeDungeon']['player'][0]['boons']
                self.unlocked = True
                raise UnlockReached()
        return super().control_click(name)


def skill_page(c):
    if c.ui['panel']: c.control_click('dungeon:close')
    c.control_click('dungeon:menu'); c.control_click('dungeon:skills')
    chosen = next(b for b in c.ui['controls'] if b['id'].startswith('dungeon:skill-open:') and b['label'] == 'Volley')
    c.control_click(chosen['id'])
    return next(b for b in c.ui['controls'] if b['id'] == 'dungeon:skill-cast:0')


def verify(c, budget):
    journey = Journey(c, class_index=1)
    if c.continuation:
        assert c.wanted in c.ui['nativeDungeon']['player'][0]['boons']
        c.unlocked = True
    else:
        try: outcome = journey.run(budget, lifecycle=False)
        except UnlockReached: outcome = 'unlocked'
        assert outcome == 'unlocked' and c.unlocked, 'No natural skill unlock within the journey; inspect ordinary exploration/combat evidence'
    c.capture('naturally-owned-volley-boon'); c.resume()
    skills = c.ui['nativeDungeon']['retainedSkills']; clock = c.ui['nativeDungeon']['clock']
    assert any(s['boon'] == c.wanted for member in skills for s in member['skills'])
    skill_page(c); c.capture('free-illustrated-owned-skill-review')
    assert c.ui['nativeDungeon']['clock'] == clock
    c.control_click('dungeon:close')
    for _ in range(240):
        journey.observe()
        assert journey.player['health'] > 0, 'The ordinary campaign ended before casting'
        if journey.fighters:
            cast = skill_page(c)
            if cast['enabled']:
                before = c.ui['nativeDungeon']; before_clock = before['clock']
                c.capture('natural-visible-volley-targets-and-paid-recovery')
                c.control_click(cast['id'])
                after = c.ui['nativeDungeon']
                assert after['clock'] > before_clock
                assert any(e['kind'] == 'RetainedSkillUsed' and e['label'] == 'SKILL' for e in after['feedback']['effects'])
                c.capture('paid-native-volley-with-combat-feedback')
                break
            c.control_click('dungeon:close')
        if journey.fight() or journey.quiet_tasks() or journey.explore(): continue
        raise AssertionError('No ordinary exploration route to another visible hostile')
    else: raise AssertionError('No ordinary visible target for the unlocked skill')
    c.resume()
    assert c.ui['nativeDungeon']['retainedSkills'] == skills
    skill_page(c); c.capture('exact-owned-skill-reload-after-combat')
    c.control_click('dungeon:close'); c.control_click('dungeon:menu'); c.control_click('dungeon:settings')
    if c.ui['nativeDungeon']['presentation']['font'] != 'Sans': c.control_click('dungeon:setting:font')
    for _ in range(3):
        if c.ui['nativeDungeon']['presentation']['textSize'] == 'Large': break
        c.control_click('dungeon:setting:text-size')
    skill_page(c); c.capture('large-sans-unlocked-skill-review')
    (c.output / 'result.json').write_text(json.dumps(dict(retainedSkillVerified=True, seed=c.ui['seed'],
        boon=c.wanted, clock=c.ui['nativeDungeon']['clock'],
        limits='Natural Ranger chest/owned unlock, free atlas skill review, paid visible-hostile Volley, feedback and exact continuing save. Other seven active skills, hostile imported ability controllers and outdoor acquisition require their own journeys.'), indent=2) + '\n')


def main():
    p = argparse.ArgumentParser(); p.add_argument('output', type=Path); p.add_argument('--runtime', type=Path); p.add_argument('--budget', type=int, default=900)
    p.add_argument('--continue-from', type=Path)
    a = p.parse_args(); continuation = a.continue_from
    if continuation:
        # The journey's progress handoff may predate its latest ordinary save.
        # Copy the real manifest/chunks verbatim and omit stale probe metadata.
        checkpoint = a.output.resolve().with_name(a.output.name + '-source')
        (checkpoint / 'control').mkdir(parents=True, exist_ok=False)
        source = continuation / 'control/campaign.json'
        shutil.copy2(source, checkpoint / 'control/campaign.json')
        shutil.copytree(Path(str(source) + '.chunks'), checkpoint / 'control/campaign.json.chunks')
        (checkpoint / 'source.json').write_text(json.dumps(dict(path=str(source.resolve()), sha256=hashlib.sha256(source.read_bytes()).hexdigest()), indent=2) + '\n')
        continuation = checkpoint
    c = SkillClient(a.output.resolve(), 4, runtime_source=a.runtime, continuation=continuation, window_size='1024x720')
    try: verify(c, a.budget)
    finally: c.close()


if __name__ == '__main__': main()
