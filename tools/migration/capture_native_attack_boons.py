#!/usr/bin/env python3
"""Original public preview/commit evidence for owned attack forms, patterns and payments."""
import json
from capture_native_boon_chains import lua, table, array
from inventory_boons import ROOT, REVISION, SOURCES

lua.execute("""
local G=require('src.game'); local B=require('src.boons'); local C=require('src.content')
local V=require('src.visibility'); local R=require('src.rng'); local Classes=require('src.classes')
function capture_attacks(input)
    local s=G.new(input.seed,'core:map/scrolling_room',{class_id='core:actor/'..input.class})
    local p=G.player(s);p.position.x=15;p.position.y=16;p.hp=p.max_hp-5
    local map=C.map_for(s);s.generated_map={};for k,v in pairs(map)do s.generated_map[k]=v end
    s.generated_map.rows={};for y,row in ipairs(map.rows)do s.generated_map.rows[y]=string.rep('.',#row)end
    s.entities={p};local actors={p};local ids={[p.id]=1}
    for _,foe in ipairs(input.enemies)do
        local e=G.spawn_enemy(s,foe.definition or 'core:enemy/mossbound_guard',15+foe.dx,16+foe.dy,100000)
        e.hp=1000;e.max_hp=1000;e.defense=foe.armor or 0
        actors[#actors+1]=e;ids[e.id]=#actors
    end
    for _,slug in ipairs(input.boons)do B.set_stacks(p,'core:boon/'..slug,'common',1)end
    s.rng=R.new(input.seed);V.recompute(s)
    local result={}
    for _,cmd in ipairs(input.commands)do
        local intent={kind=cmd.kind,dx=cmd.dx or 1,dy=cmd.dy or 0,vow=cmd.vow}
        local preview=G.preview(s,intent.kind,intent.dx,intent.dy,nil,{vow=cmd.vow})
        assert(preview.valid,preview.reason)
        local cells={};for _,c in ipairs(preview.cells)do cells[#cells+1]={x=c.x-15,y=c.y-16}end
        local patterns={};for _,c in ipairs(preview.boon_cells or {})do
            patterns[#patterns+1]=c.boon_id..':'..(c.x-15)..':'..(c.y-16)..':'..c.condition..':'..(c.delay or 0)
        end
        local damage
        if Classes.immediate_attack(p,preview.bump and 'basic_attack' or intent.kind,preview)then
            local _,d=G.effective_ability(s,preview.bump and 'basic_attack' or intent.kind,intent.dx,intent.dy,not preview.bump,true)
            damage=d+(preview.vow and preview.vow.bonus or 0)
        end
        local ok,why,events=G.submit(s,intent);assert(ok,why)
        local trace={}
        for _,e in ipairs(events)do
            local source=ids[e.source_id] or 0;local target=ids[e.target_id] or 0
            if e.kind=='BoonActivated'then trace[#trace+1]=e.kind..':'..source..':'..e.boon_id
            elseif e.kind=='DamageTaken' or e.kind=='DamageBlocked' or e.kind=='HitConfirmed'then
                trace[#trace+1]=e.kind..':'..source..':'..target..':'..e.actual_loss
            elseif e.kind=='Healed'then trace[#trace+1]=e.kind..':'..source..':'..target..':'..e.actual_gain
            elseif e.kind=='FateRolled'then trace[#trace+1]=e.kind..':'..source..':'..e.face
            elseif e.kind=='VowPaid'then trace[#trace+1]=e.kind..':'..source..':'..e.hp_cost
            end
        end
        local health={};for _,a in ipairs(actors)do health[#health+1]=a.hp end
        result[#result+1]={cells=cells,patterns=patterns,damage=damage,fate_power=preview.fate_power,
            vow_hp=preview.vow and preview.vow.hp_cost or nil,cost=preview.cost,trace=trace,
            health=health,clock=s.clock,random=s.rng.state}
    end
    return result
end
""")

cases = []
def add(name, kind, boons, commands, enemies=None, seed=37):
    spec={'name':name,'class':kind,'seed':seed,'boons':boons,'commands':commands,
          'enemies':enemies or [{'dx':1,'dy':0},{'dx':2,'dy':0},{'dx':1,'dy':1},{'dx':-1,'dy':0}]}
    expected=array(lua.globals().capture_attacks(table(spec)),lambda s:{
        'cells':array(s.cells,lambda p:{'x':p.x,'y':p.y}),'patterns':array(s.patterns),
        'damage':s.damage,'fate_power':s.fate_power,'vow_hp':s.vow_hp,'cost':s.cost,
        'trace':array(s.trace),'health':array(s.health),'clock':s.clock,'random':s.random})
    cases.append({'input':spec,'expected':expected})

for kind,boons,action in [
    ('adventurer',['longblade_geometry','pommel_return'],'basic_attack'),
    ('adventurer',['crescent_reach'],'sweeping_slash'),
    ('ranger',['splitflight','needle_flight'],'basic_attack'),
    ('ranger',['recurve'],'sweeping_slash'),
    ('arcanist',['expanding_sigil','hollow_sigil'],'sweeping_slash'),
    ('arcanist',['alchemical_flip','emberbrand'],'basic_attack'),
    ('berserker',['earthshaker'],'basic_attack'),
    ('berserker',['backswing'],'sweeping_slash'),
    ('sentinel',['distant_bastion'],'sweeping_slash'),
    ('alchemist',['volatile_vial'],'basic_attack'),
    ('duelist',['split_thrust'],'basic_attack'),
    ('adventurer',['linebreaker_pawn','rooks_wake','bishops_wake','knights_gambit','hollow_crown','fan_of_blades'],'sweeping_slash'),
]:
    add('/'.join(boons),kind,boons,[{'kind':action},{'kind':action},{'kind':'wait'}])
add('bump-form-exceptions','adventurer',['longblade_geometry','pommel_return'],[{'kind':'move'}])
add('bump-duelist','duelist',['split_thrust'],[{'kind':'move'}])
add('dash-and-wait-patterns','adventurer',['backtrail_mantle','crosswind'],[{'kind':'dash'},{'kind':'wait'}],
    enemies=[{'dx':1,'dy':1}])
add('dash-path-and-prime','berserker',['phantom_lunge','battle_march'],
    [{'kind':'dash'},{'kind':'basic_attack'},{'kind':'basic_attack'}],enemies=[{'dx':1,'dy':1},{'dx':3,'dy':0}])
for seed in range(1,9):
    add(f'dice/{seed}','adventurer',['fateful_dice','temporal_echo','pilgrims_vow'],
        # A real three-XP foe avoids an unrelated level-up RNG draw on the skull
        # face. Reward/progression integration remains a separate required port.
        [{'kind':'basic_attack','vow':True},{'kind':'wait'}],
        enemies=[{'dx':1,'dy':0,'definition':'core:enemy/ruin_skitter'}],seed=seed)

path=ROOT/'tests/Roag.Core.Tests/Tactics/Fixtures/original-attack-boons.json'
path.write_text(json.dumps({'revision':REVISION,'sources':dict(sorted(SOURCES.items())),'cases':cases},separators=(',',':'))+'\n')
print(f'Captured {len(cases)} public preview/attack scenarios and {sum(len(c["expected"]) for c in cases)} decisions.')
