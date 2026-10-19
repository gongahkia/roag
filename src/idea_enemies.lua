-- Approved idea encounters. Effects use the existing Game scheduler/HP batch.
local RNG=require('src.rng')
local Squads=require('src.enemy_squads')
local Webs=require('src.idea_webs')
local Oaths=require('src.idea_oaths')
local Sentinel=require('src.idea_sentinel')
local Mirrors=require('src.idea_mirrors')
local Silt=require('src.idea_silt')
local I={version='roag-idea-enemies/1'}
I.enemies={
    -- id, name, HP, damage, defense, speed, windup, recovery, range
    {'heavy_rammer','Heavy Rammer',12,2,1,70,110,140,1},
    {'hollow_wraith','Hollow Wraith',7,2,0,90,100,120,1},
    {'grave_cantor','Grave Cantor',7,1,0,80,150,160,4},
    {'gulp_toad','Gulp Toad',14,2,1,80,100,140,1},
    {'haunted_cache','Haunted Cache',8,2,0,75,140,140,1},
    {'bone_splitter','Bone Splitter',8,2,0,80,100,130,1},
    {'bone_fragment','Bone Fragment',2,1,0,95,100,110,1},
    {'alarm_keeper','Alarm Keeper',6,1,0,75,220,150,1},
    {'web_weaver','Web Weaver',7,1,0,90,140,140,4},
    {'oath_twin_a','Oath Twin — Pike',8,2,0,90,100,130,1},
    {'oath_twin_b','Oath Twin — Blade',8,1,0,100,110,130,1},
    {'rotating_sentinel','Rotating Sentinel',14,2,1,80,160,80,1},
    {'mirror_hexer','Mirror Hexer',7,2,0,85,150,140,4},
    {'silt_worm','Silt Worm',10,2,0,85,140,140,3},
}
I.champions={[Sentinel.id]=true}
I.secondary={bone_fragment=true,oath_twin_b=true}
I.roles={['core:enemy/heavy_rammer']='rammer',['core:enemy/hollow_wraith']='wraith',
    ['core:enemy/grave_cantor']='reviver',['core:enemy/gulp_toad']='toad',
    ['core:enemy/haunted_cache']='cache',['core:enemy/bone_splitter']='bone',
    ['core:enemy/bone_fragment']='fragment',['core:enemy/alarm_keeper']='alarm',
    ['core:enemy/web_weaver']='web',['core:enemy/oath_twin_a']='oath',
    ['core:enemy/oath_twin_b']='oath',[Sentinel.id]='sentinel',[Mirrors.id]='mirror',[Silt.id]='silt'}
I.tiny={['core:enemy/heavy_rammer']=89,['core:enemy/grave_cantor']=88,['core:enemy/gulp_toad']=123,
    ['core:enemy/haunted_cache']=92,['core:object/idea_cache_dormant']=90,
    ['core:enemy/web_weaver']=122,['core:enemy/oath_twin_a']=99,['core:enemy/oath_twin_b']=100}
-- The reviewed redirect permits only 416, in BOTH settings choices.
I.shared_onebit={['core:enemy/hollow_wraith']=416,['core:enemy/bone_splitter']=323,
    ['core:enemy/bone_fragment']=323,['core:enemy/alarm_keeper']=173,[Sentinel.id]=224,[Mirrors.id]=320,[Silt.id]=371}
I.onebit={['core:enemy/heavy_rammer']=129,['core:enemy/hollow_wraith']=416,
    ['core:enemy/grave_cantor']=318,['core:enemy/gulp_toad']=422,['core:enemy/haunted_cache']=417,
    ['core:enemy/bone_splitter']=323,['core:enemy/bone_fragment']=323,['core:enemy/alarm_keeper']=173,
    ['core:enemy/web_weaver']=366,['core:enemy/oath_twin_a']=124,['core:enemy/oath_twin_b']=127,
    [Sentinel.id]=224,[Mirrors.id]=320,[Silt.id]=371}
I.objects={['core:object/idea_cache_dormant']=303,['core:object/idea_silt_trail']=268}
I.caves_objects={['core:object/idea_silt_trail']=64}
I.symbols={['core:ui/idea_corpse_effect']=539,['core:ui/idea_alarm_effect']=915,
    ['core:ui/idea_oath_effect']=574,
    ['core:ui/idea_mirror_effect']=493,['core:ui/idea_reflect_effect']=494}
I.abilities={rammer_shove='rammer shove',wraith_phase='wraith phase',wraith_touch='wraith touch',
    cantor_revive='cantor revival',cantor_touch='cantor touch',
    toad_gulp='swallow loose drop',toad_bite='toad bite',cache_bite='haunted cache bite',
    bone_strike='bone strike',fragment_stab='fragment stab',
    keeper_alarm='reinforcement alarm',keeper_touch='keeper touch',
    weaver_web='anchored web',weaver_bite='weaver bite',
    oath_pike='oath pike',oath_blade='oath blade',oath_share='shared oath loss',
    sentinel_quarter='sentinel quarter turn',sentinel_touch='sentinel strike',
    hexer_mirror='directional reflector',hexer_bolt='hexer bolt',
    worm_charge='silt charge',worm_touch='worm bite'}
local directions={{0,-1},{1,0},{0,1},{-1,0}}
local corpse_ai={guard=true,spitter=true,skitter=true,spore_herald=true,
    canal_lancer=true,cinder_bellkeeper=true,expansion_regular=true}

function I.revivable(e)
    return corpse_ai[e.ai_id] or e.definition_id=='core:enemy/heavy_rammer'
        or e.definition_id=='core:enemy/hollow_wraith' or e.definition_id=='core:enemy/gulp_toad'
end
function I.drop(state,id)
    for _,drop in ipairs(state.drops)do if drop.id==id then return drop end end
end
function I.release_drop(state,target,emit)
    if not target.carried_drop_id then return end
    local drop=assert(I.drop(state,target.carried_drop_id),'missing carried drop')
    assert(drop.carried_by==target.id and not drop.collected,'invalid carried drop release')
    drop.carried_by=nil;target.carried_drop_id=nil
    drop.position={floor_id=target.position.floor_id,x=target.position.x,y=target.position.y}
    emit('DropRegurgitated',{drop_id=drop.id,drop_kind=drop.kind,amount=drop.amount,
        floor_id=drop.position.floor_id,x=drop.position.x,y=drop.position.y})
end
-- Called only while draining the canonical death event, after the entire
-- primary HP batch and removals. Fragments can never split or pay rewards.
function I.split(state,event,map,free,spawn,emit)
    if event.target_definition_id~='core:enemy/bone_splitter' or event.reward_exempt
        or state.game_over or event.floor_id~=state.active_floor_id then return end
    local points={{0,0},{0,-1},{1,0},{0,1},{-1,0}}
    local count=0
    for _,d in ipairs(points)do
        local x,y=event.x+d[1],event.y+d[2]
        local reserved=false
        for _,stair in ipairs(map.placements and map.placements.stairs or {})do
            if stair.x==x and stair.y==y then reserved=true end
        end
        if map.elite and map.elite.x==x and map.elite.y==y then reserved=true end
        for _,e in ipairs(state.entities)do
            if e.controller=='exit' and e.position.floor_id==event.floor_id
                and e.position.x==x and e.position.y==y then reserved=true end
        end
        if not reserved and free(x,y)then
            local fragment=spawn(state,'core:enemy/bone_fragment',x,y,state.clock+1)
            fragment.reward_exempt=true;fragment.fragment_of=event.target_id
            count=count+1
            emit('BoneFragmented',{target_id=fragment.id,original_id=event.target_id,
                floor_id=event.floor_id,x=x,y=y})
            if count==2 then break end
        end
    end
end
function I.prune_corpses(state)
    for i=#(state.idea_corpses or {}),1,-1 do
        local c=state.idea_corpses[i]
        local keep=false
        for _,e in ipairs(state.entities)do
            local pending=e.pending_attack
            if e.definition_id=='core:enemy/grave_cantor' and e.hp>0
                and e.position.floor_id==c.floor_id
                and (not e.ai_memory.revival_spent or pending and pending.support
                    and pending.support.kind=='revive' and pending.support.corpse_id==c.id) then
                keep=true;break
            end
        end
        if not keep then table.remove(state.idea_corpses,i)end
    end
end
function I.remember_corpse(state,target)
    if target.controller~='ai' or target.reward_exempt or not I.revivable(target) then return end
    local eligible=false
    for _,e in ipairs(state.entities)do
        if e.definition_id=='core:enemy/grave_cantor' and e.hp>0
            and not e.ai_memory.revival_spent and e.position.floor_id==target.position.floor_id then
            eligible=true;break
        end
    end
    if not eligible then return end
    state.idea_corpses=state.idea_corpses or {}
    if #state.idea_corpses>=32 then return end
    local count=0
    for _,c in ipairs(state.idea_corpses)do if c.floor_id==target.position.floor_id then count=count+1 end end
    if count>=16 then return end
    state.idea_corpses[#state.idea_corpses+1]={id=target.id,definition_id=target.definition_id,
        floor_id=target.position.floor_id,x=target.position.x,y=target.position.y,max_hp=target.max_hp}
end
function I.corpse(state,id)
    for _,c in ipairs(state.idea_corpses or {})do if c.id==id then return c end end
end
function I.commit(state,enemy,intent)
    Sentinel.commit(enemy,intent)
    if intent.ability_id=='core:ability/worm_charge'then
        assert((enemy.ai_memory.silt_charges or 0)<2,'spent silt charge budget')
        enemy.ai_memory.silt_charges=(enemy.ai_memory.silt_charges or 0)+1
    end
    if intent.support and intent.support.kind=='mirror'then
        assert((enemy.ai_memory.mirror_casts or 0)<2,'spent mirror budget')
        enemy.ai_memory.mirror_casts=(enemy.ai_memory.mirror_casts or 0)+1
    end
    if intent.support and intent.support.kind=='web'then
        assert((enemy.ai_memory.web_casts or 0)<2,'spent web budget')
        enemy.ai_memory.web_casts=(enemy.ai_memory.web_casts or 0)+1
    end
    if intent.support and intent.support.kind=='alarm' then
        assert(not enemy.ai_memory.alarm_spent,'spent alarm')
        enemy.ai_memory.alarm_spent=true
    end
    if intent.support and intent.support.kind=='revive' then
        local c=assert(I.corpse(state,intent.support.corpse_id),'missing committed corpse')
        assert(not c.claimed_by and not enemy.ai_memory.revival_spent,'spent revival')
        c.claimed_by=enemy.id;enemy.ai_memory.revival_spent=true
    end
end

function I.alarm_landing(state,enemy,cell,map,free,visible)
    local a=enemy.position
    if cell.floor_id~=a.floor_id or a.floor_id~=state.active_floor_id
        or math.abs(cell.x-a.x)+math.abs(cell.y-a.y)~=1
        or not map.rows[cell.y] or map.rows[cell.y]:sub(cell.x,cell.x)~='.'
        or not free(state,enemy.id,cell.x,cell.y) or not visible(cell.x,cell.y)then return false end
    for _,stair in ipairs(map.placements and map.placements.stairs or {})do
        if stair.x==cell.x and stair.y==cell.y then return false end
    end
    for _,e in ipairs(state.entities)do
        if e.controller=='exit' and e.position.floor_id==a.floor_id
            and e.position.x==cell.x and e.position.y==cell.y then return false end
    end
    return true
end

function I.phase_geometry(from,to)
    local dx,dy=to.x-from.x,to.y-from.y
    return (dx==0 or dy==0) and math.abs(dx)+math.abs(dy)==2
end

function I.phase_landing(state,enemy,cell,map,free,visible)
    if cell.floor_id~=enemy.position.floor_id or cell.floor_id~=state.active_floor_id
        or not I.phase_geometry(enemy.position,cell) then return false end
    if not free(state,enemy.id,cell.x,cell.y) or not visible(cell.x,cell.y) then return false end
    for _,stair in ipairs(map.placements and map.placements.stairs or {}) do
        if stair.x==cell.x and stair.y==cell.y then return false end
    end
    for _,other in ipairs(state.entities) do
        local p=other.position
        if other.controller=='exit' and p.floor_id==cell.floor_id
            and p.x==cell.x and p.y==cell.y then return false end
    end
    return true
end

function I.choose(state,enemy,player,h)
    local a,b=enemy.position,player.position
    local dx,dy=b.x-a.x,b.y-a.y
    local role=I.roles[enemy.definition_id]
    if role=='silt'then return Silt.choose(state,enemy,player,h)end
    if role=='mirror'then return Mirrors.choose(state,enemy,player,h)end
    if role=='sentinel'then return Sentinel.choose(state,enemy,player,h)end
    if role=='oath'then
        if math.abs(dx)+math.abs(dy)==1 then
            return {kind='windup',ability_id=enemy.definition_id==Oaths.a and
                'core:ability/oath_pike' or 'core:ability/oath_blade',cells={{x=b.x,y=b.y}}}
        end
        return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
    end
    if role=='web'then return Webs.choose(state,enemy,player,h)end
    if role=='alarm' then
        if not enemy.ai_memory.alarm_spent and h.visible(a.x,a.y)then
            local cells={}
            for _,d in ipairs(directions)do
                local c={floor_id=a.floor_id,x=a.x+d[1],y=a.y+d[2]}
                if I.alarm_landing(state,enemy,c,h.map,h.free,h.visible)then
                    cells[#cells+1]=c;if #cells==2 then break end
                end
            end
            if #cells>0 then return {kind='windup',ability_id='core:ability/keeper_alarm',cells=cells,
                support={kind='alarm'}}end
        end
        if math.abs(dx)+math.abs(dy)==1 then
            return {kind='windup',ability_id='core:ability/keeper_touch',cells={{x=b.x,y=b.y}}}
        end
        return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
    end
    if role=='bone' or role=='fragment' then
        if math.abs(dx)+math.abs(dy)==1 then
            return {kind='windup',ability_id='core:ability/'..(role=='bone' and 'bone_strike' or 'fragment_stab'),
                cells={{x=b.x,y=b.y}}}
        end
        return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
    end
    if I.roles[enemy.definition_id]=='cache' then
        if math.abs(dx)+math.abs(dy)==1 then
            return {kind='windup',ability_id='core:ability/cache_bite',
                cells={{x=b.x,y=b.y}},alert=true}
        end
        if not enemy.ai_memory.alerted and enemy.hp==enemy.max_hp then return {kind='wait'}end
        local intent=h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
        intent.alert=true;return intent
    end
    if I.roles[enemy.definition_id]=='toad' then
        local best,score
        if not enemy.carried_drop_id then
            for _,drop in ipairs(state.drops)do
                local p=drop.position
                local distance=math.abs(p.x-a.x)+math.abs(p.y-a.y)
                if not drop.collected and not drop.carried_by and p.floor_id==a.floor_id
                    and distance<=4 and h.free(state,enemy.id,p.x,p.y)
                    and h.visible(p.x,p.y) and h.clear_line(state,a,p)
                    and (not score or distance<score or distance==score and drop.id<best.id)then
                    best,score=drop,distance
                end
            end
        end
        if best then
            if score<=1 then return {kind='windup',ability_id='core:ability/toad_gulp',
                cells={{x=best.position.x,y=best.position.y}},support={kind='gulp',drop_id=best.id}}end
            return h.next_step(state,enemy,{[(best.position.y-1)*h.map.width+best.position.x]=true})
                or {kind='wait'}
        end
        if math.abs(dx)+math.abs(dy)==1 then
            return {kind='windup',ability_id='core:ability/toad_bite',cells={{x=b.x,y=b.y}}}
        end
        return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
    end
    if I.roles[enemy.definition_id]=='reviver' then
        local best,score
        if not enemy.ai_memory.revival_spent then
            for _,c in ipairs(state.idea_corpses or {})do
                local distance=math.abs(c.x-a.x)+math.abs(c.y-a.y)
                if c.floor_id==a.floor_id and not c.claimed_by and distance<=4
                    and h.free(state,enemy.id,c.x,c.y) and h.visible(c.x,c.y)
                    and h.clear_line(state,a,c) and (not score or distance<score) then
                    best,score=c,distance
                end
            end
        end
        if best then return {kind='windup',ability_id='core:ability/cantor_revive',
            cells={{x=best.x,y=best.y}},support={kind='revive',corpse_id=best.id}}end
        if math.abs(dx)+math.abs(dy)==1 then
            return {kind='windup',ability_id='core:ability/cantor_touch',cells={{x=b.x,y=b.y}}}
        end
        return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
    end
    if I.roles[enemy.definition_id]=='wraith' then
        if math.abs(dx)+math.abs(dy)==1 then
            return {kind='windup',ability_id='core:ability/wraith_touch',cells={{x=b.x,y=b.y}}}
        end
        local best,score
        for _,d in ipairs(directions) do
            local x,y=a.x+d[1]*2,a.y+d[2]*2
            local middle=h.map.rows[a.y+d[2]]
            local cell={floor_id=a.floor_id,x=x,y=y}
            local distance=math.abs(x-b.x)+math.abs(y-b.y)
            if middle and middle:sub(a.x+d[1],a.x+d[1])=='#'
                and distance<math.abs(dx)+math.abs(dy)
                and I.phase_landing(state,enemy,cell,h.map,h.free,h.visible)
                and (not score or distance<score) then best,score=cell,distance end
        end
        if best then
            return {kind='windup',ability_id='core:ability/wraith_phase',cells={best},
                support={kind='phase'}}
        end
        return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
    end
    if math.abs(dx)+math.abs(dy)==1 then
        return {kind='windup',ability_id='core:ability/rammer_shove',
            cells={{x=b.x,y=b.y}},
            shove={floor_id=a.floor_id,x=b.x,y=b.y,dx=dx,dy=dy}}
    end
    return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
end

-- Read-only generation plan: a revival encounter needs a reachable ordinary
-- ally. Reuse a nearby one, or place one original ordinary body beside the
-- caster. Never repaint geometry or occupy reserved markers to force a fit.
function I.revival_company(state,map,caster)
    local a=caster.position
    for _,e in ipairs(state.entities)do
        local b=e.position
        if e~=caster and e.controller=='ai' and e.hp>0 and I.revivable(e)
            and not e.reward_exempt and b.floor_id==a.floor_id
            and math.abs(a.x-b.x)+math.abs(a.y-b.y)<=4
            and Squads.clear(state,a,b)then return true end
    end
    local point=I.companion_cell(state,map,caster)
    return point~=nil,point
end
function I.companion_cell(state,map,caster)
    local a=caster.position
    local blocked={}
    local function reserve(p)if p and p.x and p.y then blocked[p.x..','..p.y]=true end end
    for _,e in ipairs(state.entities)do if e.position.floor_id==map.floor_id then reserve(e.position)end end
    for _,list in pairs(map.placements or {})do
        for _,p in ipairs(list)do reserve(p)end
    end
    for _,q in ipairs(map.corridor_quests or {})do for _,p in ipairs(q.cells)do reserve(p)end end
    for _,d in ipairs(directions)do
        local x,y=a.x+d[1],a.y+d[2]
        if map.rows[y] and map.rows[y]:sub(x,x)=='.' and not blocked[x..','..y]
            and math.abs(x-map.start_x)+math.abs(y-map.start_y)>=8
            and (not map.elite or math.abs(x-map.elite.x)+math.abs(y-map.elite.y)>2)then
            return {x=x,y=y}
        end
    end
    return nil
end

-- At most one primary idea replacement. A Cantor can have one ordinary ally;
-- Oath Twins require a paired body. Existing saves are not repopulated.
-- Selection is read-only and consumes no combat RNG.
function I.selection(root_seed,floor_id)
    local seed=RNG.derive(root_seed,'idea-enemies:'..floor_id)
    if seed%4~=0 then return nil,seed end
    local pool={}
    for _,row in ipairs(I.enemies)do if not I.secondary[row[1]]then pool[#pool+1]=row end end
    return pool[math.floor(seed/16)%#pool+1],seed
end
function I.populate(state,map,replace,spawn)
    local definition,seed=I.selection(state.root_seed or state.run_seed,map.floor_id)
    if not definition then return end
    local choices={}
    for _,e in ipairs(state.entities) do
        local p=e.position
        if e.controller=='ai' and p.floor_id==map.floor_id and not e.squad_id
            and (e.ai_id=='guard' or e.ai_id=='skitter' or e.ai_id=='expansion_regular')
            and math.abs(p.x-map.start_x)+math.abs(p.y-map.start_y)>=8 then
            choices[#choices+1]=e
        end
    end
    table.sort(choices,function(a,b)return a.id<b.id end)
    if #choices==0 then return end
    local offset=math.floor(seed/4)%#choices
    local target,companion
    for index=1,#choices do
        local candidate=choices[(offset+index-1)%#choices+1]
        local fits,point=true,nil
        if definition[1]=='rotating_sentinel'then fits=Sentinel.optional_room(map,candidate.position)~=nil end
        if definition[1]=='grave_cantor' then fits,point=I.revival_company(state,map,candidate)end
        if definition[1]=='oath_twin_a'then
            point=I.companion_cell(state,map,candidate);fits=point~=nil
            if fits then
                local other={floor_id=map.floor_id,x=point.x,y=point.y}
                fits=Squads.clear(state,candidate.position,other)
                    and Squads.clear(state,other,candidate.position)
            end
        end
        if fits and (not point or spawn)then target,companion=candidate,point;break end
    end
    if not target then return end
    local original=target.definition_id
    replace(target,'core:enemy/'..definition[1])
    for _,p in ipairs(map.placements.enemies) do
        if p.x==target.position.x and p.y==target.position.y then
            p.definition_id=target.definition_id
            break
        end
    end
    if companion then
        local paired=definition[1]=='oath_twin_a'
        local other=spawn(state,paired and Oaths.b or original,companion.x,companion.y)
        if paired then target.oath_partner_id=other.id;other.oath_partner_id=target.id end
    end
end

function I.validate_pending(enemy,pending)
    Webs.validate_pending(enemy,pending)
    local alarm=pending.support and pending.support.kind=='alarm'
    if alarm or pending.ability_id=='core:ability/keeper_alarm' then
        assert(alarm and enemy.definition_id=='core:enemy/alarm_keeper'
            and pending.ability_id=='core:ability/keeper_alarm' and enemy.ai_memory.alarm_spent==true
            and #pending.target_cells>=1 and #pending.target_cells<=2,'invalid committed alarm')
        local cells={}
        for _,c in ipairs(pending.target_cells)do
            local key=c.x..','..c.y
            assert(c.floor_id==pending.floor_id
                and math.abs(c.x-pending.source_x)+math.abs(c.y-pending.source_y)==1
                and not cells[key],'invalid alarm cell')
            cells[key]=true
        end
    end
    local gulp=pending.support and pending.support.kind=='gulp'
    if gulp or pending.ability_id=='core:ability/toad_gulp' then
        local id=gulp and pending.support.drop_id
        local cell=pending.target_cells[1]
        assert(gulp and enemy.definition_id=='core:enemy/gulp_toad'
            and pending.ability_id=='core:ability/toad_gulp'
            and type(id)=='number' and id%1==0 and id>=1
            and #pending.target_cells==1 and cell.floor_id==pending.floor_id
            and math.abs(cell.x-pending.source_x)+math.abs(cell.y-pending.source_y)<=1,
            'invalid committed gulp')
    end
    local revive=pending.support and pending.support.kind=='revive'
    if revive or pending.ability_id=='core:ability/cantor_revive' then
        local id=revive and pending.support.corpse_id
        assert(revive and enemy.definition_id=='core:enemy/grave_cantor'
            and pending.ability_id=='core:ability/cantor_revive'
            and enemy.ai_memory.revival_spent==true
            and type(id)=='number' and id%1==0 and id>=1
            and #pending.target_cells==1 and pending.target_cells[1].floor_id==pending.floor_id,
            'invalid committed revival')
    end
    local phase=pending.support and pending.support.kind=='phase'
    if phase or pending.ability_id=='core:ability/wraith_phase' then
        assert(phase and enemy.definition_id=='core:enemy/hollow_wraith'
            and pending.ability_id=='core:ability/wraith_phase'
            and #pending.target_cells==1
            and pending.target_cells[1].floor_id==pending.floor_id
            and I.phase_geometry({x=pending.source_x,y=pending.source_y},pending.target_cells[1]),
            'invalid committed phase')
    end
    local s=pending.shove
    if not s and pending.ability_id~='core:ability/rammer_shove' then return end
    assert(enemy.definition_id=='core:enemy/heavy_rammer'
        and pending.ability_id=='core:ability/rammer_shove'
        and type(s)=='table' and s.floor_id==pending.floor_id
        and type(s.x)=='number' and s.x%1==0
        and type(s.y)=='number' and s.y%1==0
        and type(s.dx)=='number' and s.dx%1==0
        and type(s.dy)=='number' and s.dy%1==0
        and math.abs(s.dx)+math.abs(s.dy)==1
        and s.x==pending.source_x+s.dx and s.y==pending.source_y+s.dy
        and #pending.target_cells==1
        and pending.target_cells[1].x==s.x and pending.target_cells[1].y==s.y,
        'invalid committed shove')
end

function I.validate(state,maps,content)
    Silt.validate(state,maps)
    Mirrors.validate(state,maps)
    Sentinel.validate(state,maps)
    Oaths.validate(state)
    Webs.validate(state,maps)
    local corpses=state.idea_corpses
    assert(corpses==nil or type(corpses)=='table' and #corpses<=32,'invalid corpse store')
    local seen,counts={},{}
    local function actor(id)
        for _,e in ipairs(state.entities)do if e.id==id then return e end end
    end
    local carriers={}
    for _,drop in ipairs(state.drops)do
        if drop.carried_by then
            local carrier=actor(drop.carried_by)
            assert(type(drop.carried_by)=='number' and carrier and carrier.controller=='ai'
                and carrier.definition_id=='core:enemy/gulp_toad' and carrier.hp>0
                and carrier.carried_drop_id==drop.id and not drop.collected
                and carrier.position.floor_id==drop.position.floor_id
                and not carriers[carrier.id],'invalid swallowed drop')
            carriers[carrier.id]=true
        end
    end
    for _,e in ipairs(state.entities)do if e.carried_drop_id then
        assert(e.definition_id=='core:enemy/gulp_toad' and carriers[e.id],'missing swallowed drop')
    end end
    local fragments,calls={},{}
    for _,e in ipairs(state.entities)do
        if e.definition_id=='core:enemy/alarm_keeper' then
            assert(e.ai_memory.alarm_spent==nil or e.ai_memory.alarm_spent==true,'invalid alarm budget')
        end
        if e.definition_id=='core:enemy/haunted_cache' then
            assert(e.ai_memory.alerted==nil or type(e.ai_memory.alerted)=='boolean','invalid cache wake state')
        end
        if e.definition_id=='core:enemy/grave_cantor' then
            assert(e.ai_memory.revival_spent==nil or e.ai_memory.revival_spent==true,'invalid revival budget')
        end
        if e.fragment_of or e.definition_id=='core:enemy/bone_fragment' then
            assert(e.controller=='ai' and e.definition_id=='core:enemy/bone_fragment'
                and e.reward_exempt==true and type(e.fragment_of)=='number'
                and e.fragment_of%1==0 and e.fragment_of>=1 and e.fragment_of<e.id
                and not e.revival_of and not e.revived_by,'invalid bone fragment')
            fragments[e.fragment_of]=(fragments[e.fragment_of] or 0)+1
            assert(fragments[e.fragment_of]<=2,'fragment cap exceeded')
        elseif e.called_by then
            assert(e.controller=='ai' and e.definition_id=='core:enemy/ruin_skitter'
                and e.reward_exempt==true and type(e.called_by)=='number'
                and e.called_by%1==0 and e.called_by>=1 and e.called_by<e.id
                and not e.revival_of and not e.revived_by,'invalid alarm reinforcement')
            calls[e.called_by]=(calls[e.called_by] or 0)+1
            assert(calls[e.called_by]<=2,'alarm reinforcement cap exceeded')
        elseif e.revival_of or e.revived_by or e.reward_exempt then
            assert(e.controller=='ai' and I.revivable(e) and e.reward_exempt==true
                and type(e.revival_of)=='number' and e.revival_of%1==0
                and e.revival_of>=1 and e.revival_of<e.id
                and type(e.revived_by)=='number' and e.revived_by%1==0
                and e.revived_by>=1 and e.revived_by<e.id,'invalid revived actor')
        end
    end
    for _,c in ipairs(corpses or {})do
        local map=maps[c.floor_id]
        local def=content.get_enemy(c.definition_id)
        assert(type(c.id)=='number' and c.id%1==0 and c.id>=1 and c.id<state.next_entity_id
            and not seen[c.id] and not actor(c.id) and map and def
            and I.revivable({ai_id=def.ai,definition_id=c.definition_id})
            and type(c.x)=='number' and c.x%1==0 and c.x>=1 and c.x<=map.width
            and type(c.y)=='number' and c.y%1==0 and c.y>=1 and c.y<=map.height
            and type(c.max_hp)=='number' and c.max_hp%1==0 and c.max_hp>=1 and c.max_hp<=1000000,
            'invalid corpse record')
        seen[c.id]=true;counts[c.floor_id]=(counts[c.floor_id] or 0)+1
        assert(counts[c.floor_id]<=16,'corpse floor cap exceeded')
        if c.claimed_by then
            local caster=actor(c.claimed_by)
            local p=caster and caster.pending_attack
            assert(caster and caster.definition_id=='core:enemy/grave_cantor'
                and caster.position.floor_id==c.floor_id and p and p.support
                and p.support.kind=='revive' and p.support.corpse_id==c.id
                and p.target_cells[1].x==c.x and p.target_cells[1].y==c.y,'invalid corpse claim')
        end
    end
    for _,e in ipairs(state.entities)do
        local p=e.pending_attack
        if p and p.support and p.support.kind=='revive' then
            local c=I.corpse(state,p.support.corpse_id)
            assert(c and c.claimed_by==e.id,'missing pending corpse claim')
        elseif p and p.support and p.support.kind=='gulp' then
            assert(I.drop(state,p.support.drop_id),'missing pending gulp drop')
        end
    end
end

return I
