#!/usr/bin/env python3
"""Muted four-member physical work takeover, simultaneous rest and continuing orders."""
import argparse
import json
from pathlib import Path
from verify_native_journey import Client, position, distance
from verify_native_party import verify as recruit
from verify_native_work import open_work, recipe


def verify(c):
    recruit(c, class_index=0)
    for member in c.ui['nativeDungeon']['player']:
        c.key('b'); c.control_click('dungeon:member:' + str(member['id']))
        c.key('b'); c.control_click('dungeon:split'); c.control_click('dungeon:close')
    c.key('t'); state=c.ui['nativeDungeon']; original=state['activeActor']; at=position(c.ui['position'])
    target=next(position(v) for v in c.ui['cells'] if distance(at,position(v))==1 and v['material']=='Floor'
                and v['groundOpen'] and not v['occupied'] and not v['prop'] and not v['terrain'])
    open_work(c,target); c.control_click('dungeon:work-view:build'); c.control_click(recipe(c,'camp'))
    order=c.ui['nativeDungeon']['workOrders'][0]; assert order['worker']==original
    replacement=c.ui['nativeDungeon']['activeActor']; assert replacement!=original
    stock=dict(c.ui['inventory']); open_work(c); c.control_click('dungeon:work-view:orders')
    c.capture('party-project-takeover-review'); c.control_click('dungeon:work-resume:'+str(order['id']))
    order=c.ui['nativeDungeon']['workOrders'][0]
    assert order['worker']==replacement and c.ui['inventory']==stock
    assert next(a for a in c.ui['nativeDungeon']['activities'] if a['id']==original)['activity'] is None
    c.capture('paid-takeover-keeps-one-reservation')
    for _ in range(8):
        state=c.ui['nativeDungeon']; actor=state['activeActor']
        if all(a['activity'] for a in state['activities']):break
        if next(a for a in state['activities'] if a['id']==actor)['activity'] is not None:
            c.request('wait',seconds=.15);continue
        c.key('n'); c.control_click('dungeon:camp'); c.control_click('dungeon:activity:rest:60')
    else: raise AssertionError('Four member activities did not start normally')
    c.key('j'); c.control_click('dungeon:work-view:orders')
    state=c.ui['nativeDungeon']; orders=state['workOrders']; activities=state['activities']; needs=state['needs']; clock=state['clock']
    assert orders and orders[0]['worker']==replacement
    c.capture('working-member-and-three-resting-companions'); c.resume()
    assert c.ui['nativeDungeon']['workOrders']==orders
    assert c.ui['nativeDungeon']['activities']==activities and c.ui['nativeDungeon']['needs']==needs
    assert c.ui['nativeDungeon']['clock']==clock
    c.capture('party-work-and-rest-exact-reload'); c.control_click('dungeon:close')
    for _ in range(160):
        if not c.ui['nativeDungeon']['workOrders']:break
        c.request('wait',seconds=.2)
    else:raise AssertionError('Party project did not complete; inspect physical blockers/danger')
    assert next(v for v in c.ui['cells'] if position(v)==target)['material']=='Camp'
    assert c.ui['nativeDungeon']['activeActor']==replacement
    assert len([a for a in c.ui['nativeDungeon']['activities'] if a['activity'] and a['activity']['kind']==0])==3
    c.capture('physical-party-camp-completed-others-still-resting')
    c.resume(); c.capture('party-camp-exact-completion-reload')
    (c.output/'work-result.json').write_text(json.dumps(dict(partyWorkVerified=True,original=original,replacement=replacement,
        clock=c.ui['nativeDungeon']['clock'],limits='Natural four-member safe-room work takeover, independent rest and exact saves; hostile camp/worker interruption and outdoor splits remain separate journeys.'),indent=2)+'\n')


def main():
    p=argparse.ArgumentParser();p.add_argument('output',type=Path);p.add_argument('--runtime',type=Path);a=p.parse_args()
    c=Client(a.output.resolve(),37,runtime_source=a.runtime,window_size='1024x720')
    try:verify(c)
    finally:c.close()

if __name__=='__main__':main()
