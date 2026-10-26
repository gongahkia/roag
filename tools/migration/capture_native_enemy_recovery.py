#!/usr/bin/env python3
"""Original public-command evidence for impact-time enemy recovery and interruptions."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game');local B=require('src.boons');local C=require('src.content')
local V=require('src.visibility');local R=require('src.rng')
function capture_recovery(input)
    local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/adventurer'})
    local p=G.player(s);p.position.x=15;p.position.y=16
    local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
    local rows={};for y,row in ipairs(map.rows)do rows[y]=string.rep('.',#row)end
    if input.block_push then rows[16]=rows[16]:sub(1,16)..'#'..rows[16]:sub(18)end
    s.generated_map.rows=rows;s.entities={p}
    local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',16,16,0)
    s.rng=R.new(37);V.recompute(s)
    if input.boon then B.set_stacks(p,'core:boon/'..input.boon,'common',input.stacks or 1)end
    if input.enemy_speed_boon then B.set_stacks(e,'core:boon/road_flask','common',1)
        e.boon_memory={['core:boon/road_flask']={speed=1000,expires_at=1000}}end
    local results={}
    for index,command in ipairs(input.commands)do
        if index==2 and input.speed then e.speed=input.speed end
        local ok,why,events=G.submit(s,{kind=command,dx=1,dy=0});assert(ok,why)
        local ready,impact
        for _,work in ipairs(s.schedule)do if work.actor_id==e.id then
            if work.kind=='actor_ready'then ready=work.due elseif work.kind=='impact'then impact=work.due end
        end end
        local trace={}
        for _,event in ipairs(events)do
            if event.kind=='WindupStarted'then trace[#trace+1]='windup:'..event.time
            elseif event.kind=='DamageTaken'then trace[#trace+1]='damage:'..event.time..':'..(event.source_id==p.id and 'player' or 'enemy')..':'..event.actual_loss
            elseif event.kind=='WindupCancelled'then trace[#trace+1]='cancel:'..event.time..':'..event.reason end
        end
        results[#results+1]={clock=s.clock,ready=ready,impact=impact,hero_hp=p.hp,enemy_hp=e.hp,
            enemy_dx=e.position.x-15,enemy_dy=e.position.y-16,random=s.rng.state,trace=trace}
    end
    return results
end
""")

cases=[]
specifications=[dict(name=f'speed-{speed}',speed=speed,commands=['wait','wait','wait']) for speed in [35,70,95,140,280]]
specifications += [dict(name='enemy-boon-speed',enemy_speed_boon=True,commands=['wait','wait','wait']),
    dict(name='stun-pending',boon='hourglass_clasp',stacks=3,commands=['wait','basic_attack','wait']),
    dict(name='stun-recovering',boon='hourglass_clasp',stacks=3,commands=['wait','wait','basic_attack']),
    dict(name='push-interrupts',boon='anchor_maul',commands=['wait','basic_attack','wait']),
    dict(name='blocked-push-keeps-windup',boon='anchor_maul',block_push=True,commands=['wait','basic_attack','wait'])]
for spec in specifications:
    result=lua.globals().capture_recovery(table(spec))
    cases.append(dict(input=spec,expected=array(result,lambda s:dict(clock=s.clock,ready=s.ready,impact=s.impact,
        hero_hp=s.hero_hp,enemy_hp=s.enemy_hp,enemy_dx=s.enemy_dx,enemy_dy=s.enemy_dy,random=s.random,trace=array(s.trace)))))
destination=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-enemy-recovery.json'
destination.write_text(json.dumps(dict(revision=REVISION,sources=dict(sorted(SOURCES.items())),cases=cases),separators=(',',':'))+'\n')
print(f'Captured {len(cases)} original recovery scenarios and {sum(len(c["expected"]) for c in cases)} decisions.')
