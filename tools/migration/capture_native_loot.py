#!/usr/bin/env python3
"""Capture physical pickups through pinned original public Game.submit commands."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local B=require('src.boons');local C=require('src.content')
local V=require('src.visibility');local R=require('src.rng');local Rewards=require('src.rewards')
function capture_loot(input)
    local s=G.new(input.seed,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
    local p=G.player(s);p.position.x=15;p.position.y=16;p.hp=p.max_hp-input.missing
    local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
    local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
    for _,cell in ipairs(input.walls)do
        local x,y=15+cell[1],16+cell[2];rows[y]=rows[y]:sub(1,x-1)..'#'..rows[y]:sub(x+1)
    end
    s.generated_map.rows=rows;s.entities={p};s.drops={};s.next_drop_id=1;s.currency=0
    local enemy
    if input.kill then
        enemy=G.spawn_enemy(s,'core:enemy/ruin_skitter',16,16,100000);enemy.hp=1;enemy.defense=0
    end
    for _,grant in ipairs(input.boons)do B.set_stacks(p,'core:boon/'..grant.id,'common',grant.count or 1)end
    for _,drop in ipairs(input.drops)do
        local d
        if drop.kind=='currency'then d=Rewards.spawn_currency(s,drop.amount,p.position.floor_id,15+drop.dx,16+drop.dy,function()end)
        else d=Rewards.spawn_supply(s,'health',p.position.floor_id,15+drop.dx,16+drop.dy,function()end)end
        if drop.carried then d.carried_by=p.id end
    end
    s.rng=R.new(input.seed);V.recompute(s)
    local steps={}
    for _,command in ipairs(input.commands)do
        local ok,why,events=G.submit(s,{kind=command.kind,dx=command.dx or 1,dy=command.dy or 0});assert(ok,why)
        local trace={}
        for _,e in ipairs(events)do
            if e.kind=='BoonActivated'then trace[#trace+1]=e.kind..':'..e.boon_id
            elseif e.kind=='CurrencyCollected' or e.kind=='CurrencyDropped' or e.kind=='CurrencyDropEnriched' then
                trace[#trace+1]=e.kind..':'..e.drop_id..':'..e.amount
            elseif e.kind=='SupplyCollected'then trace[#trace+1]=e.kind..':'..e.drop_id..':'..e.amount..':'..e.actual_gain
            elseif e.kind=='Healed'then trace[#trace+1]=e.kind..':'..e.actual_gain end
        end
        local drops={}
        for _,d in ipairs(s.drops)do if not d.collected then
            drops[#drops+1]={id=d.id,kind=d.kind,amount=d.amount,dx=d.position.x-15,dy=d.position.y-16,carried=d.carried_by~=nil}
        end end
        steps[#steps+1]={trace=trace,drops=drops,health=p.hp,gold=s.currency,clock=s.clock,random=s.rng.state,
            dx=p.position.x-15,dy=p.position.y-16}
    end
    return {steps=steps,currency=enemy and C.get_enemy(enemy.definition_id).currency_drop or 0,
        experience=enemy and C.get_enemy(enemy.definition_id).xp_reward or 0}
end
""")

cases = []
def add(name, drops, commands, boons=(), missing=5, walls=(), kill=False):
    spec = dict(name=name, seed=37, missing=missing, drops=drops, commands=[dict(kind=c) if isinstance(c,str) else c for c in commands],
                boons=[dict(id=b) if isinstance(b,str) else b for b in boons], walls=[list(w) for w in walls], kill=kill)
    result=lua.globals().capture_loot(table(spec))
    expected=array(result.steps,lambda s:dict(trace=array(s.trace),drops=array(s.drops,lambda d:dict(id=d.id,kind=d.kind,
        amount=d.amount,dx=d.dx,dy=d.dy,carried=d.carried)),health=s.health,gold=s.gold,clock=s.clock,random=s.random,dx=s.dx,dy=s.dy))
    cases.append(dict(input=spec,expected=expected,currency=result.currency,experience=result.experience))

def gold(dx,dy,amount=2,carried=False):return dict(kind='currency',dx=dx,dy=dy,amount=amount,carried=carried)
def health(dx,dy):return dict(kind='health',dx=dx,dy=dy)

add('walk-gold', [gold(1,0)], ['move','wait'], ['prospectors_heart'])
add('walk-health-overflow', [health(1,0)], ['move','wait'], ['overflow_chalice','field_dressing'],missing=2)
add('full-health-stays', [health(1,0)], ['move','wait'], ['field_dressing'],missing=0)
add('dash-path-order', [gold(1,0,2),gold(2,0,3),health(3,0)], ['dash','wait'],['prospectors_heart','overflow_chalice'])
add('one-tile-many-drops', [gold(0,-1,2),gold(0,-1,3),gold(1,0,5)], ['wait','wait'],['scavengers_hook','prospectors_heart'])
add('four-cardinals', [gold(0,-1,1),gold(1,0,2),gold(0,1,3),gold(-1,0,4)], ['wait','wait'],[dict(id='scavengers_hook',count=4)])
add('health-does-not-spend-tile', [health(0,-1),gold(1,0)], ['wait'],['scavengers_hook'],missing=0)
add('carried-stays', [gold(0,-1,3,True),gold(1,0)], ['wait','wait'],['scavengers_hook'])
add('wall-stays', [gold(0,-1),gold(1,0)], ['wait','wait'],['scavengers_hook'],walls=[(0,-1)])
add('half-health-ward', [gold(1,0)], ['move'],['beggars_cap'],missing=20)
add('funeral-before-collection', [], ['basic_attack','move','wait'],['funeral_coin','prospectors_heart'],kill=True)
add('funeral-and-gather', [], ['basic_attack','wait','wait'],['funeral_coin','scavengers_hook','prospectors_heart'],kill=True)

path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-physical-loot.json'
path.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original loot scenarios and {sum(len(c["expected"]) for c in cases)} decisions.')
