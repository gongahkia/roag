-- A paid parry window and one paid follow-up thrust. No automatic counter,
-- guaranteed reaction turn, render timer or random draw.
local Targeting=require('src.targeting')
local D={id='core:actor/duelist',bonus=2,tiny=99,onebit=124}
D.definition={id=D.id,name='Duelist',
    description='Two-cell thrust, timed frontal parry, one-cell sidestep',
    hp=20,damage=4,slash_damage=0,defense=0,speed=115,
    crit_chance_ppm=0,crit_damage_percent=150,
    costs={move=100,basic_attack=100,sweeping_slash=100,dash=85},
    dash_distance=1,basic_id='core:ability/duelist_thrust',special_id='core:ability/duelist_parry',
    basic_name='Thrust',special_name='Timed Parry',dash_name='Sidestep',
    ui_controls='F Thrust · R Parry · X Step'}
function D.bonus_for(owner,kind)
    return owner.definition_id==D.id and kind=='basic_attack' and owner.duelist_riposte and D.bonus or 0
end
function D.raise(state,owner,dx,dy,cost)
    local p=owner.position
    state.class_special.duelist_guard={floor_id=p.floor_id,x=p.x,y=p.y,dx=dx,dy=dy,
        expires=state.clock+cost}
end
function D.parry(state,target,source,direct,ranged,remaining_damage)
    local g=state.class_special and state.class_special.duelist_guard
    if not g or target.id~=state.player_id or target.definition_id~=D.id
        or not direct or ranged or remaining_damage<=0 or not source.position
        or source.faction==target.faction or source.faction~='enemy'
        or state.clock>g.expires or target.position.floor_id~=g.floor_id
        or target.position.x~=g.x or target.position.y~=g.y then return false end
    for _,c in ipairs(Targeting.cells('sweeping_slash',g.x,g.y,g.dx,g.dy))do
        if source.position.floor_id==g.floor_id and source.position.x==c.x and source.position.y==c.y then
            state.class_special.duelist_guard=nil;target.duelist_riposte=true
            return true
        end
    end
    return false
end
function D.validate(state)
    assert(not state.class_special or state.class_special.duelist_guard==nil,
        'a paid parry cannot outlive its committed action')
    for _,e in ipairs(state.entities)do if e.duelist_riposte~=nil then
        assert(e.id==state.player_id and e.definition_id==D.id and e.duelist_riposte==true,
            'invalid riposte charge')
    end end
end
return D
