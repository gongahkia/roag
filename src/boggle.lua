-- Authoritative board transactions and chronological routes. Combat is injected
-- from Game: forecasts and commits call the SAME command resolver.
local C=require('src.boggle_content')
local RNG=require('src.rng')
local Boons=require('src.boons')
local B={content=C}
local token={} -- Never in saved/cloned state or exposed through UI intents.
local function copy(v)
    if type(v)~='table'then return v end
    local out={};for k,x in pairs(v)do out[k]=copy(x)end;return out
end
B.copy=copy
local function shuffle(a,rng)
    for i=#a,2,-1 do local j=RNG.next_int(rng,i);a[i],a[j]=a[j],a[i]end
end
function B.definition(s,tile)
    local d=C.types[tile.type]
    if tile.type~='sweep'then return d end
    local out=copy(d);local at=C.directions[s.boggle.sweep_direction]
    out.dir=s.boggle.sweep_direction;out.dx,out.dy=at[1],at[2];out.name='Sweep '..at[3];return out
end
local function instance(b,key)
    local t={id=b.next_id,type=key};b.next_id=b.next_id+1;return t
end
local function sample(b)
    local n=RNG.next_int(b.rng,C.ordinary_weight)
    for _,k in ipairs(C.ordinary_order)do n=n-(b.pool[k]or 0);if n<=0 then return instance(b,k)end end
    error('ordinary pool weight changed')
end
function B.initialize(s)
    local b={board={},pool={},finite={},retired={},spent={},next_id=1,revision=1,
        rng=RNG.new(RNG.derive(s.run_seed,'boggle:board/v4')),shuffle_left=C.shuffle_uses,
        guard=0,focus=0,dull=0,fury={},fury_batches={},edit_applied=false}
    s.boggle=b
    local direction=RNG.next_int(b.rng,4);b.sweep_direction=RNG.next_int(b.rng,4)
    for _,k in ipairs(C.ordinary_order)do
        local d=C.types[k];local weight=(d.kind=='move' or d.kind=='strike')and 3 or d.kind=='dash' and (d.dir==direction and 1 or 0)or k=='guard' and 2 or 1
        b.pool[k]=weight;for _=1,weight do b.board[#b.board+1]=instance(b,k)end
    end
    for _,k in ipairs(C.finite)do local t=instance(b,k);b.board[#b.board+1]=t;b.finite[t.id]=k end
    assert(#b.board==C.size*C.size);shuffle(b.board,b.rng)
    local old=s.card_trial
    s.card_trial={version=4,seed=old.seed,preset='bare',mode='boggle',fight=old.fight,phase='combat'}
end
function B.stamp(s)
    -- Every authoritative simulation change advances action/event counters;
    -- board-only changes advance revision. No free-running world clock.
    local b=s.boggle
    local parts={b.revision,s.clock,s.next_action_id,s.next_event_id,s.next_entity_id,s.next_schedule_id,
        s.rng.state,b.rng.state,b.next_id,b.guard,b.focus,b.dull}
    -- Also witness public fixture/developer mutations (spawn, displacement,
    -- stat/boon grants) which may occur without a committed player command.
    -- Do not serialize the world or immutable content on pointer movement.
    for _,e in ipairs(s.entities)do
        for _,v in ipairs({e.id,e.hp or 0,e.position.x,e.position.y,e.speed or 0,e.defense or 0,
            e.damage or 0,e.slash_damage or 0,e.stunned_until or 0,b.fury[e.id]or 0,
            e.pending_attack and e.pending_attack.schedule_id or 0})do parts[#parts+1]=v end
        local boons={};for id in pairs(e.boons or {})do boons[#boons+1]=id end;table.sort(boons)
        for _,id in ipairs(boons)do parts[#parts+1]=id;for _,rarity in ipairs({'common','uncommon','rare','legendary'})do parts[#parts+1]=e.boons[id][rarity]or 0 end end
    end
    return table.concat(parts,':')
end
function B.cell(s,id)
    for i,t in ipairs(s.boggle.board)do if t and t.id==id then return i,t end end
end
function B.geometry(s,ids,complete)
    if type(ids)~='table'then return false,'Choose a route'end
    local n=0;for k in pairs(ids)do if type(k)~='number' or k%1~=0 or k<1 then return false,'Invalid route'end;n=n+1 end
    if n~=#ids or #ids>C.route_size or complete and #ids~=C.route_size or #ids==0 then return false,'Choose exactly '..C.route_size..' tiles'end
    local seen,previous={}
    for _,id in ipairs(ids)do
        local cell=B.cell(s,id)
        if not cell or seen[id]then return false,'Tiles must be available and distinct'end
        if previous and math.abs((cell-1)%C.size-(previous-1)%C.size)+math.abs(math.floor((cell-1)/C.size)-math.floor((previous-1)/C.size))~=1 then return false,'Connect orthogonal neighbours'end
        seen[id]=true;previous=cell
    end
    return true
end
function B.command_plan(s,intent,G)
    if intent.kind~='boggle_command' or intent.token~=token then return nil,'Use a four-tile route'end
    local b=s.boggle
    if s.card_trial.phase~='combat' then return nil,'Encounter ended'end
    if intent.utility then
        return {intent={kind='wait'},utility=true,cost=G.preview(s,'wait').cost,cells={},valid=true}
    end
    local at,t=B.cell(s,intent.id)
    if not at then return nil,'Stale tile identity'end
    local d=B.definition(s,t);local kind=({strike='basic_attack',sweep='sweeping_slash'})[d.kind]or d.kind
    local p
    if kind=='move' or kind=='dash' or kind=='basic_attack' or kind=='sweeping_slash'then
        p=G.preview(s,kind,d.dx,d.dy)
        if p.bump or p.door_id or p.vault_id or p.stair then return nil,'Move needs a free tile; it never becomes a Strike'end
        if not p.valid then return nil,p.reason or 'Blocked action'end
    else
        p={valid=true,cost=G.preview(s,'wait').cost,cells={}}
        if d.boon_id then
            local owner=G.player(s);local count=0;for _ in pairs(owner.boons)do count=count+1 end
            if owner.boons[d.boon_id]then return nil,'Boon already equipped'end
            if count>=C.boon_slots then return nil,'Both boon slots are full'end
        end
        kind='boggle_effect'
    end
    p.intent={kind=kind,dx=d.dx,dy=d.dy};p.tile=t;p.cell=at;p.definition=d;return p
end
function B.consume(s,plan,emit,G)
    if plan.utility then return end
    local b=s.boggle;local t=plan.tile;local d=plan.definition
    b.board[plan.cell]=false;b.spent[t.id]=true;b.revision=b.revision+1
    if d.finite then b.retired[t.id]=true end
    local owner=G.player(s);local pos=owner.position
    emit('BoardTileExecuted',{tile_id=t.id,tile_type=t.type,cell=plan.cell},owner.id)
    if d.kind=='guard'then
        b.guard=C.guard;emit('BoardGuarded',{guard=b.guard,floor_id=pos.floor_id,x=pos.x,y=pos.y},owner.id)
    elseif d.kind=='focus' or d.kind=='dull'then
        b[d.kind]=b[d.kind]+1;emit('BoardModifierApplied',{modifier=d.kind,power=b[d.kind],floor_id=pos.floor_id,x=pos.x,y=pos.y},owner.id)
    elseif d.kind=='fury'then
        for _,e in ipairs(s.entities)do if e.faction=='enemy' and e.hp>0 then b.fury[e.id]=(b.fury[e.id]or 0)+1 end end
        emit('BoardModifierApplied',{modifier='fury',floor_id=pos.floor_id,x=pos.x,y=pos.y},owner.id)
    elseif d.kind=='mend'then
        local before=owner.hp;owner.hp=math.min(owner.max_hp,before+C.heal)
        emit('Healed',{target_id=owner.id,requested=C.heal,actual_gain=owner.hp-before,hp_before=before,hp_after=owner.hp,
            healing_source='trial:boggle/mend',floor_id=pos.floor_id,x=pos.x,y=pos.y},owner.id)
    elseif d.boon_id then
        Boons.set_stacks(owner,d.boon_id,'common',1)
        emit('BoonGranted',{boon_id=d.boon_id,rarity='common',old_count=0,new_count=1,floor_id=pos.floor_id,x=pos.x,y=pos.y},owner.id)
    end
end
function B.absorb(s,target,damage)
    if not s.boggle or target.id~=s.player_id then return damage,0 end
    local b=s.boggle;local take=math.min(b.guard,damage);b.guard=b.guard-take;return damage-take,take
end
function B.player_attack(s)
    if not s.boggle then return 0 end
    local b=s.boggle;local bonus=b.focus-b.dull;b.focus,b.dull=0,0;return bonus
end
function B.enemy_attack(s,source,action_id)
    if not s.boggle or source.faction~='enemy'then return 0 end
    local b=s.boggle;local key=source.id..':'..action_id
    if b.fury_batches[key]==nil then b.fury_batches[key]=b.fury[source.id]or 0;b.fury[source.id]=nil end
    return b.fury_batches[key]
end
function B.after_action(s,G)
    local t=s.card_trial
    if G.player(s).hp<=0 then s.game_over=true;t.phase='death'
    else
        for _,e in ipairs(s.entities)do if e.faction=='enemy' and e.hp>0 then return end end
        if t.fight==2 then s.game_over=true;t.phase='victory'else t.phase='intermission'end
    end
end
function B.refill(s)
    local b=s.boggle
    for i=1,C.size*C.size do if not b.board[i]then b.board[i]=sample(b)end end
    b.revision=b.revision+1
end
local function execute(s,ids,G,records,offset)
    local all={};local executed=0
    for i,id in ipairs(ids)do
        if s.game_over or s.card_trial.phase~='combat'then break end
        local command={kind='boggle_command',id=id,token=token}
        local plan,why=B.command_plan(s,command,G)
        if not plan then return false,'Tile '..(i+(offset or 0))..': '..why,all,executed end
        local before=G.player(s);local start={x=before.position.x,y=before.position.y,hp=before.hp}
        local ok,reason,events=G.submit(s,command)
        if not ok then return false,'Tile '..(i+(offset or 0))..': '..tostring(reason),all,executed end
        executed=executed+1
        for _,e in ipairs(events)do all[#all+1]=e end
        if records then records[#records+1]={id=id,start=start,plan=plan,events=events,state=G.snapshot(s)}end
    end
    return true,nil,all,executed
end
-- Prefix cache contains only hypothetical mutable states; no assets or live events.
function B.forecast(s,ids,G,cache)
    local started=os.clock();local stamp=B.stamp(s)
    local valid,why=B.geometry(s,ids,false)
    if not valid then return {valid=false,reason=why,stamp=stamp,steps={},elapsed=os.clock()-started}end
    cache=cache or {};if cache.stamp~=stamp then cache.stamp=stamp;cache.entries={};cache.count=0 end
    local key=table.concat(ids,',');local found=cache.entries[key]
    if found then return found end
    local clone,steps=G.snapshot(s),{};local start=1
    for n=#ids-1,1,-1 do
        local prefix={};for j=1,n do prefix[j]=ids[j]end
        local hit=cache.entries[table.concat(prefix,',')]
        if hit and hit.valid then
            clone=G.snapshot(hit.prefix);steps=copy(hit.steps);start=n+1;break
        end
    end
    local suffix={};for j=start,#ids do suffix[#suffix+1]=ids[j]end
    local ok,reason=execute(clone,suffix,G,steps,start-1)
    local prefix=G.snapshot(clone)
    if ok and #ids==C.route_size and clone.card_trial.phase=='combat' and not clone.game_over then B.refill(clone)end
    local result={valid=ok,reason=reason,complete=#ids==C.route_size,stamp=stamp,steps=steps,
        state=clone,prefix=prefix,executed=#steps,elapsed=os.clock()-started}
    if cache.count>=64 then cache.entries={};cache.count=0 end
    cache.entries[key]=result;cache.count=cache.count+1;return result
end
function B.validate(s)
    local b=s.boggle;local seen={};local total=0
    for _,k in ipairs(C.ordinary_order)do assert((b.pool[k]or 0)>=0);total=total+(b.pool[k]or 0)end
    assert(total==C.ordinary_weight)
    for i=1,C.size*C.size do local t=b.board[i];if t then
        assert(C.types[t.type] and not seen[t.id] and not b.retired[t.id] and not b.spent[t.id]);seen[t.id]=true
        if C.types[t.type].finite then assert(b.finite[t.id]==t.type)end
    end end
    local n=0;for id in pairs(b.finite)do assert(seen[id]~=nil or b.retired[id]);n=n+1 end
    assert(n==#C.finite);return true
end
function B.submit(s,intent,G)
    local b=s.boggle;local t=s.card_trial
    if intent.revision~=B.stamp(s)then return false,'Plan changed; inspect a fresh forecast',{}end
    if intent.kind=='boggle_intermission'then
        if t.phase~='intermission' or b.intermission_initialized then return false,'Intermission already initialized',{}end
        B.refill(s);b.intermission_initialized=true;t.phase='edit';return true,nil,{}
    elseif intent.kind=='boggle_edit'then
        if t.phase~='edit' or b.edit_applied then return false,'Edit unavailable',{}end
        local a,ta=B.cell(s,intent.first);local c,tc=B.cell(s,intent.second)
        if intent.edit=='rotate'then
            local d=ta and C.types[ta.type]
            if not d or not d.dir or d.finite or (b.pool[ta.type]or 0)<1 then return false,'Choose an ordinary directional tile',{}end
            local key=d.kind..(d.dir%4+1);b.pool[ta.type]=b.pool[ta.type]-1;b.pool[key]=(b.pool[key]or 0)+1;ta.type=key
        elseif intent.edit=='swap'then
            if not a or not c or a==c then return false,'Choose two different tiles',{}end
            b.board[a],b.board[c]=b.board[c],b.board[a]
        elseif intent.edit~='skip'then return false,'Choose Rotate, Swap or Skip',{}end
        b.edit_applied=true;b.revision=b.revision+1;t.fight=2;t.phase='combat'
        require('src.card_trial').spawn_fight(s,G.spawn_enemy);return true,nil,{}
    end
    if t.phase~='combat' or s.game_over then return false,'Encounter ended',{}end
    if intent.kind=='boggle_route'then
        local ok,why=B.geometry(s,intent.ids,true);if not ok then return false,why,{}end
        local forecast=B.forecast(s,intent.ids,G)
        if not forecast.valid then return false,forecast.reason,{}end
        local accepted,reason,events=execute(s,intent.ids,G)
        if not accepted then b.defect='Unexpected execution mismatch: '..reason end
        if t.phase=='combat' and not s.game_over then B.refill(s)end
        return true,reason,events
    elseif intent.kind=='boggle_shuffle' or intent.kind=='boggle_reset'then
        local is_shuffle=intent.kind=='boggle_shuffle'
        if is_shuffle and b.shuffle_left<1 then return false,'Shuffle already used',{}end
        if is_shuffle then b.shuffle_left=b.shuffle_left-1 end
        local events={}
        for _=1,is_shuffle and 1 or 2 do
            local ok,why,e=G.submit(s,{kind='boggle_command',utility=true,token=token});assert(ok,why)
            for _,v in ipairs(e)do events[#events+1]=v end
            if s.game_over or t.phase~='combat'then return true,nil,events end
        end
        if is_shuffle then shuffle(b.board,b.rng)
        else
            local row
            for y=0,C.size-1 do local free=true;for x=1,C.size do if C.types[b.board[y*C.size+x].type].finite then free=false end end;if free then row=y;break end end
            assert(row,'finite supply exceeds recovery rows')
            for i,tile in ipairs(b.board)do if not C.types[tile.type].finite then
                b.spent[tile.id]=true
                local x,y=(i-1)%C.size,math.floor((i-1)/C.size)
                local key=y==row and x<C.route_size and 'guard' or ({'guard','strike1','move2','strike2','move4','strike4'})[(x+y)%6+1]
                b.board[i]=instance(b,key)
            end end
        end
        b.revision=b.revision+1;return true,nil,events
    end
    return false,'Use the action board',{}
end
return B
