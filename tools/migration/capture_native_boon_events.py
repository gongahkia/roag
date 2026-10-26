#!/usr/bin/env python3
"""Execute pinned predicates/dispatch with explicit world queries; never translate their code."""
import copy
import json
import random

from lupa import LuaRuntime
from inventory_boons import ROOT, REVISION, SOURCES, original_source, data

lua = LuaRuntime(unpack_returned_tuples=True)
lua.globals().original_source = original_source
lua.execute("""
package.path=''; package.cpath=''
package.searchers={function(name) return assert(load(original_source(name),'@original/'..name)) end}
local B=require('src.boons')
local C=require('src.content')
local T=require('src.terrain')
local F=require('src.floors')
local V=require('src.visibility')
local I=require('src.idea_terrain')
local R=require('src.rng')
local predicate
for i=1,100 do
    local name,value=debug.getupvalue(B.on_event,i)
    if name=='predicate' then predicate=value;break end
end
assert(predicate)
local function position(p) return p and {x=p.x,y=p.y,floor_id=p.floor} end
local function positions(ps)
    local result={};for _,p in ipairs(ps or {}) do result[#result+1]=position(p) end;return result
end
local function event(input)
    local e={};for k,v in pairs(input) do e[k]=v end
    local p=input.position
    if p then e.floor_id=p.floor;e.x=p.x;e.y=p.y end
    if input.from then
        e.floor_id=e.floor_id or input.from.floor;e.from_x=input.from.x;e.from_y=input.from.y
    end
    e.source_position=position(input.source_position);e.pattern_origin=position(input.pattern_origin)
    e.path=positions(input.path);e.target_cells=positions(input.target_cells)
    e.ancestry={}
    for _,a in ipairs(input.ancestry or {}) do e.ancestry[#e.ancestry+1]=a.owner..':'..a.boon end
    return e
end
local function setup(input)
    local state={clock=input.clock,entities={},chain_history={},rng=R.new(input.seed or 42),next_activation_id=1,
        last_action={kind=input.dash and 'dash' or 'move'}}
    for _,a in ipairs(input.actors) do
        local actor={id=a.id,hp=a.hp,max_hp=a.max_hp,faction=a.faction,position=position(a.position),
            definition_id=a.class or (a.regular and 'fixture:regular' or 'fixture:boss'),
            controller='ai',boons={},boon_memory=a.memory or {}}
        for _,b in ipairs(a.boons or {}) do actor.boons[b]={common=1,rare=1} end
        if a.locked then actor.pending_attack={target_cells=positions(a.locked)} end
        state.entities[#state.entities+1]=actor
    end
    F.map=function(_,floor) return {floor_id=floor} end
    T.symbol=function(map,x,y)
        for _,p in ipairs(input.terrain or {}) do
            if p.floor==map.floor_id and p.x==x and p.y==y then return p.symbol end
        end
        return '.'
    end
    I.at=function(_,floor,x,y)
        for _,p in ipairs(input.special or {}) do
            if p.floor==floor and p.x==x and p.y==y then return {kind=p.kind} end
        end
    end
    V.is_visible=function(_,floor,x,y)
        for _,p in ipairs(input.hidden or {}) do if p.floor==floor and p.x==x and p.y==y then return false end end
        return floor==0
    end
    C.get_enemy=function(id)
        if id=='fixture:regular' then return {currency_drop=3,ai='guard'} end
        if id=='fixture:boss' then return {currency_drop=3,ai='guard',special_encounter=true} end
        if id=='fixture:empty' then return {currency_drop=0,ai='guard'} end
    end
    return state
end
function check_predicate(node,input,boon)
    local state=setup(input)
    local history={damages={},pairs={},groups={},windups={},actions={}}
    for _,e in ipairs(input.prior or {}) do
        local p=e.position
        history.damages[#history.damages+1]={target_id=e.target_id,target_faction=e.target_faction,
            floor_id=p.floor,x=p.x,y=p.y}
    end
    local result={}
    for _,e in ipairs(input.events) do result[#result+1]=not not predicate(node,state,state.entities[1],boon,event(e),history) end
    return result
end
function check_dispatch(input)
    local state=setup(input);local output={}
    for _,e in ipairs(input.events) do
        B.on_event(state,event(e),function(a,effect)
            output[#output+1]={type='effect',activation=a.id,kind=effect.kind}
        end,function(a)
            output[#output+1]={type='activation',id=a.id,boon=a.boon_id,owner=a.owner_id,power=a.power,
                chain=a.chain_id,root=a.root_action_id,parent=a.parent_event_id,pair=a.pair_target_id,ancestry=a.ancestry}
        end)
    end
    return {output=output,random=state.rng.state,next_id=state.next_activation_id}
end
""")


def table(value):
    if isinstance(value, dict):
        return lua.table_from({k: table(v) for k, v in value.items()})
    if isinstance(value, list):
        return lua.table_from([table(v) for v in value])
    return value


def p(x, y, floor=0):
    return {"x": x, "y": y, "floor": floor}


inventory = json.loads((ROOT / 'docs/migration/original-boon-inventory.json').read_text())
nodes = {}


def collect(node):
    if not isinstance(node, dict):
        return
    if any(k in node for k in ['op', 'all', 'any', 'not_']):
        nodes[json.dumps(node, sort_keys=True)] = node
    for key, value in node.items():
        if isinstance(value, list):
            for child in value:
                collect(child)
        elif isinstance(value, dict):
            collect(value)


for definition in inventory['definitions']:
    collect(definition.get('predicate'))
    for effect in definition['effects']:
        collect(effect.get('when'))
for op, value in [('source_id', 1), ('target_id', 2), ('source_tag', 'living'),
                  ('target_tag', 'undead'), ('source_faction', 'player'), ('target_faction', 'enemy')]:
    collect({'op': op, 'value': value})
boon = 'core:boon/temporal_echo'
base_event = {'id': 1, 'chain_id': 1, 'kind': 'HitConfirmed', 'root_action_id': 1, 'source_id': 1, 'target_id': 2,
              'source_faction': 'player', 'target_faction': 'enemy', 'source_tags': ['living'], 'target_tags': ['undead'],
              'source_position': p(10, 10), 'position': p(11, 10), 'from': p(13, 10),
              'target_cells': [p(10, 10), p(11, 10)], 'path': [p(12, 10), p(11, 10)],
              'ability_id': 'core:ability/basic_attack', 'damage_type': 'physical', 'direct_attack': True,
              'hp_before': 10, 'target_max_hp': 20, 'actual_loss': 4, 'amount': 8, 'actual_gain': 3,
              'price': 20, 'supply_kind': 'food', 'windup_id': 1, 'boon_id': 'core:boon/ember_seed',
              'fate_face': 3, 'status': 'burning', 'target_burning': True, 'adjacent_burning_count': 1,
              'wall_backed': True, 'target_definition_id': 'fixture:regular', 'terrain_id': 'core:terrain/ice'}
base = {'clock': 100, 'dash': True, 'seed': 42,
        'actors': [{'id': 1, 'hp': 10, 'max_hp': 40, 'class': 'core:actor/adventurer', 'faction': 'player', 'position': p(10, 10),
                    'memory': {boon: {'expires_at': 100, 'charges': 1, 'ward': 5}}},
                   {'id': 2, 'hp': 10, 'max_hp': 20, 'faction': 'enemy', 'position': p(11, 10), 'regular': True},
                   {'id': 3, 'hp': 10, 'max_hp': 20, 'faction': 'enemy', 'position': p(12, 10), 'locked': [p(11, 10)]},
                   {'id': 4, 'hp': 10, 'max_hp': 20, 'faction': 'enemy', 'position': p(11, 11)}],
        'terrain': [dict(p(12, 10), symbol='~'), dict(p(10, 10), symbol='i'), dict(p(13, 10), symbol='b')],
        'special': [dict(p(11, 10), kind='heat_vent')], 'hidden': [],
        'prior': [{'target_id': 3, 'target_faction': 'enemy', 'position': p(12, 10)},
                  {'target_id': 4, 'target_faction': 'enemy', 'position': p(12, 11)}]}
scenarios = []
for variant in range(28):
    scenario = copy.deepcopy(base)
    e = copy.deepcopy(base_event)
    if variant == 1:
        e = {'id': 1, 'chain_id': 1, 'kind': 'Waited'}
    elif variant == 2:
        e.update(source_id=2, target_id=1, source_faction='enemy', target_faction='player')
    elif variant == 3:
        e['source_faction'] = e['target_faction'] = 'enemy'
    elif variant == 4:
        e.update(kind='Moved', ability_id='core:ability/dash')
    elif variant == 5:
        e.update(kind='Moved', ability_id='core:ability/dash'); scenario['dash'] = False
    elif variant == 6:
        e.update(ability_id='core:ability/sweeping_slash', hp_before=20, target_max_hp=20)
    elif variant == 7:
        e.update(position=p(11, 11), pattern_origin=p(9, 11), target_burning=False)
    elif variant == 8:
        e.update(position=p(11, 10, 1), source_position=p(10, 10, 1)); e['from'] = p(13, 10, 1)
    elif variant == 9:
        scenario['hidden'] = [p(10, 10), p(11, 10)]
    elif variant == 10:
        scenario['clock'] = 101
    elif variant == 11:
        scenario['clock'] = 99; scenario['actors'][0]['hp'] = 40
    elif variant == 12:
        scenario['actors'][0]['hp'] = 0; scenario['actors'][1]['hp'] = 0
    elif variant == 13:
        e.update(source_hp_at_attack=0, source_max_hp_at_attack=40, adjacent_hostile_count=0, actual_loss=0)
    elif variant == 14:
        e.update(source_hp_at_attack=40, source_max_hp_at_attack=40, adjacent_hostile_count=5, actual_gain=8,
                 target_definition_id='fixture:empty', adjacent_burning_count=0)
    elif variant == 15:
        e.update(reward_exempt=True, wall_backed=False, direct_attack=False, boon_id=boon, fate_face=8)
    elif variant == 16:
        e.pop('position'); e.pop('from'); e['source_position'] = p(10, 10)
    elif variant == 17:
        scenario['actors'][1]['regular'] = False
        scenario['actors'][2]['locked'] = [p(11, 10, 1)]
        e.update(source_id=3, target_id=2, amount=0, actual_gain=0)
    elif 18 <= variant < 22:
        e['position'] = p(9 + variant % 3, 9 + variant % 4)
        e['fate_face'] = variant - 17
        e['damage_type'] = ['fire', 'ice', 'lightning', 'acid'][variant - 18]
        e['supply_kind'] = 'health'
        e['terrain_id'] = 'core:terrain/bog'
        scenario['actors'][0]['memory'] = {}
    elif variant == 22:
        e['kind'] = 'TileEntered'; scenario['terrain'][2]['symbol'] = 'g'
    elif variant == 23:
        e['kind'] = 'DamageTaken'
        scenario['terrain'][2]['symbol'] = 'i'; scenario['terrain'][0]['symbol'] = 'i'
        scenario['terrain'][1]['symbol'] = 'm'; scenario['terrain'].append(dict(p(11, 11), symbol='c'))
    elif variant == 24:
        e.update(kind='DamageBlocked', terrain_id='core:terrain/bramble_bed', position=p(14, 10))
    elif variant == 25:
        e.update(kind='SupplyCollected', ability_id='core:ability/berserker_crush')
        scenario['actors'][3]['position'] = p(13, 10)
    elif variant == 26:
        e['fate_face'] = 5
    elif variant == 27:
        e['fate_face'] = 6
    scenario['events'] = [e, copy.deepcopy(e), dict(e, id=3, target_id=3, boon_id='core:boon/a'),
                          dict(e, id=4, target_id=4, boon_id='core:boon/b', root_action_id=2, windup_id=2),
                          dict(e, id=5, target_id=5, boon_id='core:boon/c')]
    scenarios.append(scenario)
predicates = []
for node in nodes.values():
    values = []
    for scenario in scenarios:
        result = lua.globals().check_predicate(table(node), table(scenario), boon)
        values.append([bool(result[i]) for i in range(1, len(result) + 1)])
    predicates.append({'node': node, 'expected': values})

# Real catalog loadouts across all trigger kinds; callbacks record work without resolving effects.
generator = random.Random(982)
dispatch = []
for index in range(24):
    scenario = copy.deepcopy(base)
    scenario.pop('prior')
    scenario['seed'] = [1, 42, 2147483646][index % 3]
    owners = [scenario['actors'][0]]
    if index % 2:
        second = copy.deepcopy(owners[0]); second['id'] = 5; second['position'] = p(12, 10); owners.append(second)
    for owner in owners:
        owner['boons'] = [d['id'] for d in inventory['definitions'] if not d.get('eligible_classes') and generator.random() < .7]
        # Conflicts are inventory acquisition rules; choose a legal loadout.
        chosen = []
        definitions = {d['id']: d for d in inventory['definitions']}
        for identifier in owner['boons']:
            if not any(other in definitions[identifier].get('incompatible', []) or identifier in definitions[other].get('incompatible', []) for other in chosen):
                chosen.append(identifier)
        owner['boons'] = chosen
        owner['memory'] = {identifier: {'expires_at': 100, 'charges': 1, 'ward': 5} for identifier in chosen}
    scenario['actors'] = owners + scenario['actors'][1:]
    events = []
    triggers = list(dict.fromkeys(t for d in inventory['definitions'] for t in d['triggers']))
    for n in range(80):
        e = copy.deepcopy(base_event)
        e.update(id=n+1, chain_id=1+n//40, root_action_id=1+n//4, windup_id=1+n//3,
                 kind=triggers[n % len(triggers)], proc_coefficient=[0, .25, 1, 2][index % 4],
                 source_id=[1, 2, 3][n % 3], target_id=2+n%3, position=p(11+n%2, 10+n%3),
                 source_faction=['player', 'enemy', 'enemy'][n % 3], target_faction='enemy')
        if n % 7 == 0: e['oath_shared_only'] = True
        if n % 11 == 0: e['fate_miss'] = True
        if n % 5 == 0: e.update(ability_id='core:ability/dash', fate_face=1+n%8)
        if n % 3 == 0: e['ancestry'] = [{'owner': 1, 'boon': owner['boons'][n % len(owner['boons'])]}]
        events.append(e)
    scenario['events'] = events
    expected = data(lua.globals().check_dispatch(table(scenario)))
    dispatch.append({'input': scenario, 'expected': expected})

result = {'revision': REVISION, 'sources': dict(sorted(SOURCES.items())), 'boon': boon,
          'scenarios': scenarios, 'predicates': predicates, 'dispatch': dispatch}
destination = ROOT / 'tests/Roag.Core.Tests/Tactics/Fixtures/original-boon-events.json'
destination.write_text(json.dumps(result, separators=(',', ':')) + '\n')
operations = {n['op'] for n in nodes.values() if 'op' in n}
print(f'Captured {len(predicates)} predicate trees covering {len(operations)} operations across {len(scenarios)} scenarios, and {len(dispatch)} dispatch sequences.')
