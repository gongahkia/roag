#!/usr/bin/env python3
"""Capture complete paid-command proc chains from pinned original Game.submit.

Only actors, owned stacks and empty arena terrain are prepared; commands, event
draining, damage, targeting, statuses and delayed effects execute original code.
"""
import json
from lupa.luajit21 import LuaRuntime
from inventory_boons import ROOT, REVISION, SOURCES, original_source

lua = LuaRuntime(unpack_returned_tuples=True)
lua.globals().original_source = original_source
lua.execute("""
require('bit')
package.path=''; package.cpath=''
package.loaders={function(name) return assert(loadstring(original_source(name),'@original/'..name)) end}
local G=require('src.game'); local B=require('src.boons'); local C=require('src.content')
local V=require('src.visibility'); local R=require('src.rng')
function capture(input)
    local s=G.new(input.seed,'core:map/scrolling_room',{class_id='core:actor/'..input.class})
    local p=G.player(s);p.position.x=15;p.position.y=16;p.hp=p.max_hp-5
    -- Preserve the map identity but eliminate unrelated authored terrain/objects.
    local map=C.map_for(s);local rows={}
    for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
    s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end;s.generated_map.rows=rows
    s.entities={p};local actors={p};local indices={[p.id]=1}
    for _,foe in ipairs(input.enemies)do
        local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',15+foe.dx,16+foe.dy,100000)
        e.hp=1000;e.max_hp=1000;e.defense=foe.armor or 0
        actors[#actors+1]=e;indices[e.id]=#actors
    end
    for _,grant in ipairs(input.grants)do B.set_stacks(actors[grant.owner],'core:boon/'..grant.boon,'common',grant.count or 1)end
    s.rng=R.new(input.seed); V.recompute(s)
    local steps={}
    for _,command in ipairs(input.commands)do
        local ok,why,events=G.submit(s,{kind=command,dx=1,dy=0});assert(ok,why)
        local trace={}
        for _,e in ipairs(events)do
            local source=indices[e.source_id] or 0;local target=indices[e.target_id] or 0
            if e.kind=='BoonActivated'then trace[#trace+1]=e.kind..':'..source..':'..e.boon_id
            elseif e.kind=='DamageTaken' or e.kind=='DamageBlocked' or e.kind=='HitConfirmed'then
                trace[#trace+1]=e.kind..':'..source..':'..target..':'..e.actual_loss
            elseif e.kind=='Healed'then trace[#trace+1]=e.kind..':'..source..':'..target..':'..e.actual_gain
            end
        end
        local health={};for _,a in ipairs(actors)do health[#health+1]=a.hp end
        steps[#steps+1]={trace=trace,health=health,clock=s.clock,random=s.rng.state}
    end
    return steps
end
""")


def table(value):
    if isinstance(value, dict): return lua.table_from({k: table(v) for k, v in value.items()})
    if isinstance(value, list): return lua.table_from([table(v) for v in value])
    return value


def array(value, convert=lambda x: x):
    return [convert(value[i]) for i in range(1, len(value)+1)]


def main():
    cases = []
    for seed in [1, 37, 919]:
        for scenario in ['opening', 'reflections', 'burn_echo', 'batch', 'movement', 'dash', 'blocked']:
            spec = {'name': f'{scenario}/{seed}', 'seed': seed, 'class': 'adventurer',
                    'enemies': [{'dx': 1, 'dy': 0}], 'grants': [],
                    'commands': ['basic_attack', 'wait', 'wait', 'basic_attack']}
            grants = spec['grants']
            def grant(boon, owner=1, count=1): grants.append({'boon': boon, 'owner': owner, 'count': count})
            if scenario == 'opening': grant('opening_cut'); grant('storm_conductor', count=23)
            if scenario == 'reflections':
                grant('thorn_mirror'); grant('thorn_mirror', owner=2); grant('opening_cut')
            if scenario == 'burn_echo':
                for boon in ['emberbrand','temporal_echo','storm_conductor','fourfold_ledger']: grant(boon)
            if scenario == 'batch':
                spec['enemies']=[{'dx':1,'dy':-1},{'dx':1,'dy':0},{'dx':1,'dy':1}]
                spec['commands']=['sweeping_slash','wait','wait']
                for boon in ['threefold_remedy','resonant_wounds','temporal_echo','opening_cut']: grant(boon)
            if scenario in ['movement','dash']:
                spec['enemies']=[{'dx':1,'dy':1},{'dx':2,'dy':1}]
                spec['commands']=['move' if scenario=='movement' else 'dash','wait','wait']
                for boon in ['static_footsteps','backtrail_mantle','storm_conductor','crosswind']: grant(boon)
            if scenario == 'blocked':
                spec['enemies'][0]['armor']=20
                grant('storm_conductor',count=50); grant('emberbrand')
            steps=lua.globals().capture(table(spec))
            expected=array(steps,lambda s:{'trace':array(s.trace), 'health':array(s.health), 'clock':s.clock,'random':s.random})
            cases.append({'input':spec,'expected':expected})
    destination=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-boon-chains.json'
    destination.write_text(json.dumps({'revision':REVISION,'sources':dict(sorted(SOURCES.items())),'cases':cases},separators=(',',':'))+'\n')
    print(f'Captured {len(cases)} original public-command chains ({sum(len(c["expected"]) for c in cases)} decisions).')


if __name__ == '__main__':
    main()
