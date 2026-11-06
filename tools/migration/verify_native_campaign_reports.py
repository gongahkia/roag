#!/usr/bin/env python3
"""Read actual generated campaign reports through muted ordinary UI input.

Pays resident rumours, preserves private terrain, and checks an exact continuing
save. It never establishes physical entry, activation or boss combat acceptance.
"""
import argparse
from pathlib import Path
from collections import Counter
import hashlib,json,sys,time
from verify_native_world import WorldClient,saved_complexes
from verify_native_entrance import Walk,rumour,exact
from verify_native_journey import position
class FreshWalk(Walk):
    def __init__(self,client):
        self.client=client; self.known={}; self.visits=Counter(); self.home=position(client.ui['position']); self.observe()

def pages(client,label):
    if client.sequence>500:exact(client,'protocol-before-'+label)
    clock=client.ui['nativeDungeon']['clock']; selected=client.ui['nativeDungeon']['controlledActor']
    client.control_click('dungeon:menu');client.control_click('dungeon:journal')
    bosses=client.ui['expeditions']['majorBosses']; assert len(bosses)==4
    rows=[]
    for page in range(40):
        rows.extend(client.ui['expeditions']['rows']); client.capture(label+'-'+str(page))
        next_=next(c for c in client.ui['controls'] if c['id']=='dungeon:journal-next')
        if not next_['enabled']:break
        client.control_click(next_['id'])
    else:raise AssertionError('Journal pagination did not reach its last page')
    client.control_click('dungeon:close')
    assert client.ui['nativeDungeon']['clock']==clock and client.ui['nativeDungeon']['controlledActor']==selected
    return dict(bosses=bosses,rows=rows)

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output',type=Path)
    parser.add_argument('--runtime',type=Path,required=True)
    parser.add_argument('--provenance',type=Path,required=True,help='Frozen source.json/source.diff directory')
    parser.add_argument('--seed',type=int,default=42)
    args=parser.parse_args()
    RUNTIME=args.runtime.resolve();OUT=args.output.resolve()
    for filename in ('source.json','source.diff'):
        assert (args.provenance/filename).is_file(), 'Missing frozen provenance: '+filename
    client=WorldClient.__new__(WorldClient); held=False
    try:
        client.__init__(OUT,args.seed,RUNTIME,window_size='1024x720',scene='world-campaign')
        import shutil
        for filename in ('source.json','source.diff'):
            shutil.copy2(args.provenance/filename,OUT/('runtime-'+filename))
        shutil.copy2(Path(__file__),OUT/'executed-verifier.py')
        runtime_hashes={str(p.relative_to(RUNTIME)):hashlib.sha256(p.read_bytes()).hexdigest() for p in RUNTIME.rglob('*') if p.is_file()}
        (OUT/'runtime-source-proof.json').write_text(json.dumps(dict(runtime=str(RUNTIME),files=runtime_hashes,source=str(args.provenance.resolve()/'source.json')),indent=2)+'\n')
        client.control_click('dungeon:class:1');client.capture('00-native-campaign')
        assert client.ui['nativeDungeon']['specialDungeons']==[]
        normal=pages(client,'01-normal-journal')
        walk=FreshWalk(client)
        for i in range(4):
            if client.sequence>500:exact(client,'protocol-before-rumour-'+str(i))
            rumour(client,walk,100)
            walk.checkpoint('paid-rumour-'+str(i))
            client.control_click('dungeon:menu');client.control_click('dungeon:journal')
            entrances=client.ui['expeditions']['entrances']; specials=client.ui['nativeDungeon']['specialDungeons']
            (OUT/('reports-'+str(i)+'.json')).write_text(json.dumps(dict(entrances=entrances,specials=specials),indent=2)+'\n')
            client.control_click('dungeon:close')
        assert len(entrances)==4 and len(specials)==2
        clock=client.ui['nativeDungeon']['clock'];client.control_click('dungeon:menu');client.control_click('dungeon:specials')
        for i in range(2):
            client.capture('02-special-warning-'+str(i))
            controls=[c for c in client.ui['controls'] if c['id']=='dungeon:special-review']; assert len(controls)==1 and not controls[0]['enabled']
            assert all(s['activatedAt'] is None for s in client.ui['nativeDungeon']['specialDungeons'])
            if i==0:client.control_click('dungeon:special-next')
        client.control_click('dungeon:close');assert client.ui['nativeDungeon']['clock']==clock
        client.control_click('dungeon:menu');client.control_click('dungeon:settings')
        for _ in range(5):
            if client.ui['nativeDungeon']['presentation']['font']=='Sans':break
            client.control_click('dungeon:setting:font')
        for _ in range(4):
            if client.ui['nativeDungeon']['presentation']['textSize']=='Large':break
            client.control_click('dungeon:setting:text-size')
        client.control_click('dungeon:close');assert client.ui['nativeDungeon']['clock']==clock
        large=pages(client,'03-large-journal')
        state=exact(client,'04-exact-generated-world');walk.checkpoint('05-continuation-ready')
        proof=saved_complexes(client,'06-generated-campaign-proof')
        assert state['nativeCampaign']['finale'] is not None
        for relative,digest in runtime_hashes.items():
            assert hashlib.sha256((RUNTIME/relative).read_bytes()).hexdigest()==digest
            assert hashlib.sha256((client.runtime/relative).read_bytes()).hexdigest()==digest
        result=dict(passed=True,scope='native generated campaign / four paid public entrance rumours / free normal-large journal and inactive special warnings / exact continuing save',clock=clock,entrances=entrances,specials=specials,majorBosses=normal['bosses'],profile=proof['profile'],run=proof['run'],runtimeFiles=len(runtime_hashes),limits='No physical complex entry, special activation/consequence, boss combat, victory or Linux delivery acceptance.')
        (OUT/'result.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result),flush=True)
        client.request('stop');client.process.wait(timeout=15)
    except BaseException as error:
        result=dict(passed=False,error=str(error),type=type(error).__name__,replayed=False)
        if hasattr(client,'ui'):
            if isinstance(error,TimeoutError):
                held=True;result['processRetained']=client.process.poll() is None
            else:
                try:client.control_click('dungeon:save');client.capture('failure-current-checkpoint');result['ordinaryCheckpoint']=True
                except Exception as checkpoint:result['checkpointError']=str(checkpoint)
            (OUT/'result.json').write_text(json.dumps(result,indent=2)+'\n')
        print(json.dumps(result),flush=True)
        if held:
            while client.process.poll() is None:time.sleep(10)
        raise
    finally:
        if hasattr(client,'journal') and not held:client.close()

if __name__=='__main__':
    main()
