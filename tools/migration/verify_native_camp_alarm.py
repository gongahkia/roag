#!/usr/bin/env python3
"""Natural hostile camp alarm, using an unmodified four-member construction save."""
import argparse
import json
import hashlib
import shutil
from pathlib import Path
from verify_native_journey import Client, Journey, position, distance


def verify(c):
    if c.ui['panel']:c.control_click('dungeon:close')
    state=c.ui['nativeDungeon']; scout=next(p['id'] for p in state['player'] if p['classId']=='Adventurer')
    watcher=next(p['id'] for p in state['player'] if p['classId']=='Arcanist')
    camp=position(next(p for p in state['player'] if p['id']==watcher)['position'])
    for member in [scout,watcher]:
        c.key('b');c.control_click('dungeon:member:'+str(member));c.key('n');c.control_click('dungeon:camp')
        c.control_click('dungeon:activity-cancel');c.control_click('dungeon:close')
    c.key('t');c.key('n');c.control_click('dungeon:camp');c.control_click('dungeon:activity:rest:60')
    for _ in range(20):
        actor=c.ui['nativeDungeon']['activeActor']
        if actor==watcher:break
        if actor==scout:c.key('space')
        else:c.request('wait',seconds=.15)
    else:raise AssertionError('The woken lookout did not receive their own input turn')
    c.key('n');c.control_click('dungeon:camp');c.control_click('dungeon:activity:watch:60')
    for _ in range(20):
        if c.ui['nativeDungeon']['activeActor']==scout:break
        c.request('wait',seconds=.15)
    else:raise AssertionError('The awake scout did not receive their own input turn')
    state=c.ui['nativeDungeon']; sleeping={a['id'] for a in state['activities'] if a['activity'] and a['activity']['kind']==0}
    assert len(sleeping)==2 and next(a for a in state['activities'] if a['id']==watcher)['activity']['kind']==1
    c.capture('two-sleepers-lookout-and-awake-scout')
    journey=Journey(c);alarm=False;luring=False;retreat_steps=0;lost_waits=0
    scout_name=next(p['name'] for p in c.ui['nativeDungeon']['player'] if p['id']==scout)
    for _ in range(240):
        state=c.ui['nativeDungeon']
        if any(e['kind']=='CampAlarm' for e in state['feedback']['effects']):
            alarm=True;break
        assert all(p['life']=='Alive' for p in state['player']), 'Natural danger downed a member; this is not a passing alarm journey'
        if state['activeActor']!=scout:
            c.request('wait',seconds=.15);continue
        journey.observe();journey.player=next(p for p in state['player'] if p['id']==scout)
        journey.occupied |= {position(p['position']) for p in state['player'] if p['id']!=scout and p['life']!='Dead'}
        if journey.fighters and not luring:
            enemy=min(journey.fighters,key=lambda f:distance(journey.at,position(f['position'])))
            p=position(enemy['position']);c.request('hover',x=p[0],y=p[1])
            detail=(c.ui.get('hover') or {}).get('detail','')
            if scout_name in detail and ('Preferred target' in detail or 'Attack locked' in detail):
                luring=True;c.capture('real-hostile-detects-scout')
            else:
                goals=[q for q in journey.known if 2<=distance(q,p)<=3 and journey.open(q,True)]
                if not journey.go(goals,safe=True):c.key('space')
                continue
        if luring:
            if not journey.fighters and lost_waits<6:
                lost_waits+=1;c.key('space');continue
            if journey.fighters:lost_waits=0
            goals=[p for p in journey.known if 2<=distance(p,camp)<=4 and journey.open(p,True)]
            if distance(journey.at,camp)<=4 or retreat_steps%3==2 or not journey.go(goals,safe=True):c.key('space')
            retreat_steps+=1
            continue
        # Ordinary frontier exploration may open a door and expose the camp to
        # a real hostile. No enemy, direction, attack or notice is injected.
        if not journey.explore():c.key('space')
    assert alarm, 'No natural lookout alarm occurred within the paid exploration budget'
    state=c.ui['nativeDungeon'];assert next(a for a in state['activities'] if a['id']==watcher)['activity'] is None
    assert all(next(a for a in state['activities'] if a['id']==member)['activity'] is None for member in sleeping)
    c.capture('natural-hostile-lookout-alarm-wakes-camp')
    c.key('j');before=c.ui['nativeDungeon'];c.resume()
    for field in ['activities','needs','clock','player']:
        assert c.ui['nativeDungeon'][field]==before[field],field
    c.capture('hostile-alarm-exact-camp-continuation')
    (c.output/'alarm-result.json').write_text(json.dumps(dict(naturalAlarmVerified=True,scout=scout,watcher=watcher,
        sleepers=sorted(sleeping),clock=c.ui['nativeDungeon']['clock'],limits='Natural same-room four-member hostile alarm; wall/distance-separated non-hearers are covered by backend checks, broader outdoor camp remains pending.'),indent=2)+'\n')


def main():
    p=argparse.ArgumentParser();p.add_argument('output',type=Path);p.add_argument('--continue-from',type=Path,required=True);p.add_argument('--runtime',type=Path);a=p.parse_args()
    output=a.output.resolve();checkpoint=output.with_name(output.name+'-checkpoint');(checkpoint/'control').mkdir(parents=True,exist_ok=False)
    source=a.continue_from.resolve()/'control/campaign.json';shutil.copy2(source,checkpoint/'control/campaign.json')
    shutil.copytree(Path(str(source)+'.chunks'),checkpoint/'control/campaign.json.chunks')
    (checkpoint/'source.json').write_text(json.dumps(dict(source=str(source),sha256=hashlib.sha256(source.read_bytes()).hexdigest(),note='Byte-for-byte ordinary continuing save; no stale earlier recruitment handoff metadata imported.'),indent=2)+'\n')
    c=Client(output,37,runtime_source=a.runtime,continuation=checkpoint,window_size='1024x720')
    shutil.copy2(__file__,c.output/'driver.py')
    try:verify(c)
    finally:c.close()

if __name__=='__main__':main()
