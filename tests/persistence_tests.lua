return function(test,equal)
    local Game=require('src.game')
    local P=require('src.persistence')
    local Scheduler=require('src.scheduler')
    local Visibility=require('src.visibility')
    local Content=require('src.content')
    local function memory()
        local storage={fail=false,truncate=false}
        function storage:read(name) return self[name] end
        function storage:write(name,bytes)
            if self.fail then return false end
            self[name]=self.truncate and bytes:sub(1,math.floor(#bytes/2)) or bytes
            return true
        end
        return storage
    end
    local function quiet(state)
        for _,e in ipairs(state.entities) do
            if e.controller=='ai' then Scheduler.remove_for_actor(state,e.id) end
        end
    end
    local function near(state,entity)
        local p=Game.player(state)
        local dirs={{0,-1},{1,0},{0,1},{-1,0}}
        for _,d in ipairs(dirs) do
            local x,y=entity.position.x+d[1],entity.position.y+d[2]
            local clear=Content.walkable(state.generated_map,x,y)
            for _,e in ipairs(state.entities) do
                if e.id~=p.id and e.blocks_movement and e.position.x==x and e.position.y==y then
                    clear=false
                end
            end
            if clear then
                p.position.x,p.position.y=x,y
                Visibility.recompute(state)
                return entity.position.x-x,entity.position.y-y
            end
        end
        error('no approach')
    end
    test('save codec is inert and preserves nested numeric keyed plain data',function()
        local value={empty={},nested={1,false,{x='a\n\0b',n=-0.125}},
            [7]='seven',price=1.23e-10}
        equal(value,P.decode(P.encode(value)))
        local bad={'X','T1:Z','S3:hi','D3:nan','T1:S1:aYjunk'}
        for _,bytes in ipairs(bad) do assert(not pcall(P.decode,bytes)) end
        assert(not pcall(P.decode,string.rep('a',P.max_bytes+1)))
        assert(not pcall(P.encode,{f=function()end}))
    end)
    test('two generations select latest and recover a corrupt newest record',function()
        local store=memory(); local service=P.new(store)
        local state=Game.new_stage(2)
        assert(service:save(state))
        assert(Game.submit(state,{kind='wait'}))
        assert(service:save(state))
        local loaded,meta=service:load()
        assert(meta.status=='active' and meta.record.seq==2 and loaded.clock==state.clock)
        store[P.slots[2]]=store[P.slots[2]]:sub(1,50)
        loaded,meta=service:load()
        assert(meta.status=='recovered' and loaded.clock==0)
        store[P.slots[1]]='broken'
        loaded,meta=service:load()
        assert(loaded==nil and meta.status=='corrupt')
    end)
    test('failed or truncated writes retain prior verified generation',function()
        local store=memory();local service=P.new(store)
        local state=Game.new_stage(3)
        assert(service:save(state))
        assert(Game.submit(state,{kind='wait'}))
        store.fail=true
        assert(not service:save(state))
        store.fail=false
        local loaded=assert(service:load())
        assert(loaded.clock==0)
        store.truncate=true
        assert(not service:save(state))
        loaded=assert(service:load())
        assert(loaded.clock==0)
    end)
    test('New Run replacement writes inactive slot first and survives backup failure',function()
        local store=memory();local service=P.new(store)
        local old=Game.new_stage(12)
        assert(service:save(old))
        local original=store.write
        local writes=0
        function store:write(name,bytes)
            writes=writes+1
            if writes==1 then return false end
            return original(self,name,bytes)
        end
        local fresh=Game.new_stage(13)
        assert(not service:replace(fresh))
        local prior=assert(service:load())
        assert(prior.run_seed==12)
        writes=0
        function store:write(name,bytes)
            writes=writes+1
            if writes==2 then return false end
            return original(self,name,bytes)
        end
        local ok,warning=service:replace(fresh)
        assert(ok and warning and warning:find('backup write failed',1,true))
        local loaded=assert(service:load())
        assert(loaded.run_seed==13 and store[P.slots[1]] and store[P.slots[2]])
        assert(service:save(fresh))
        loaded=assert(service:load())
        assert(loaded.run_seed==13)
    end)
    test('newest integrity-valid incompatible record blocks stale rollback',function()
        local store=memory();local service=P.new(store)
        local state=Game.new_stage(4)
        assert(service:save(state));assert(service:save(state))
        store[P.slots[2]]=store[P.slots[2]]:gsub('roeg%-content/6','roeg-content/9',1)
        -- A header edit breaks its checksum only if payload changes; compatibility is header data.
        local loaded,meta=service:load()
        assert(not loaded and meta.status=='incompatible')
        assert(not service:save(state))
        assert(service:replace(state))
        assert(service:load())
    end)
    test('snapshot schema mismatch and unknown content are rejected before installation',function()
        local store=memory();local service=P.new(store)
        local state=Game.new_stage(4)
        assert(service:save(state));assert(service:save(state))
        store[P.slots[2]]=store[P.slots[2]]:gsub('roeg%-run/7','roeg-run/99',1)
        local loaded,meta=service:load()
        assert(not loaded and meta.status=='incompatible')
        local current=Game.snapshot(state)
        local enemy
        for _,e in ipairs(state.entities) do
            if e.controller=='ai' then enemy=e;break end
        end
        assert(enemy)
        enemy.definition_id='missing:enemy'
        assert(not service:save(state))
        enemy.definition_id=current.entities[enemy.id].definition_id
        equal(current,Game.snapshot(state))
    end)
    test('terminal generation prevents older active save resurrection',function()
        local store=memory();local service=P.new(store)
        local state=Game.new_stage(5)
        assert(service:save(state))
        assert(service:tombstone())
        local loaded,meta=service:load()
        assert(not loaded and meta.status=='terminal')
        store[meta.record.slot==1 and P.slots[1] or P.slots[2]]='broken'
        loaded,meta=service:load()
        assert(not loaded and meta.status=='terminal')
        assert(service:replace(state))
        assert(service:load())
        state.game_over=true
        assert(service:save(state))
        loaded,meta=service:load()
        assert(not loaded and meta.status=='terminal')
    end)
    test('save and load consume no world time or gameplay RNG',function()
        local store=memory();local service=P.new(store)
        local state=Game.new_stage(6)
        local before=Game.snapshot(state)
        assert(service:save(state))
        equal(before,Game.snapshot(state))
        local loaded=assert(P.new(store):load())
        equal(before,Game.snapshot(loaded))
    end)
    test('full bytes round trip preserves generated stage and deterministic continuation',function()
        local store=memory();local service=P.new(store)
        local state=Game.new_stage(8)
        local p=Game.player(state)
        Game.set_boon_stacks(state,p.id,'core:boon/crescent_reach','rare',2)
        state.currency=11
        state.progression.level=2
        state.progression.xp_total=1
        assert(service:save(state))
        local resumed=assert(P.new(store):load())
        for _,intent in ipairs({{kind='wait'},{kind='basic_attack',dx=1,dy=0},
            {kind='wait'}}) do
            local ok1,reason1,a=Game.submit(state,intent)
            local ok2,reason2,b=Game.submit(resumed,intent)
            assert(ok1==ok2 and reason1==reason2)
            equal(a,b)
        end
        equal(Game.snapshot(state),Game.snapshot(resumed))
    end)
    test('save retains earned reward, cached chest, opened door, secret and fog',function()
        local store=memory();local service=P.new(store)
        local state=Game.new_generated(303)
        quiet(state)
        local p=Game.player(state)
        local dirs={{0,-1},{1,0},{0,1},{-1,0}}
        local enemy
        for _,d in ipairs(dirs) do
            local x,y=p.position.x+d[1],p.position.y+d[2]
            if Content.walkable(state.generated_map,x,y) then
                local occupied=false
                for _,e in ipairs(state.entities) do
                    if e.blocks_movement and e.position.x==x and e.position.y==y then
                        occupied=true
                    end
                end
                if not occupied then
                    enemy=Game.spawn_enemy(state,'core:enemy/mossbound_guard',x,y)
                    Scheduler.remove_for_actor(state,enemy.id)
                    enemy.hp=1
                    local _,_,out=Game.submit(state,{kind='basic_attack',dx=d[1],dy=d[2]})
                    local kill=false
                    for _,event in ipairs(out) do if event.kind=='EntityKilled' then kill=true end end
                    assert(kill and state.progression.xp_total>0)
                    assert(Game.submit(state,{kind='move',dx=d[1],dy=d[2]}))
                    assert(state.currency>=4)
                    break
                end
            end
        end
        assert(enemy)
        local second
        for _,d in ipairs(dirs) do
            local x,y=p.position.x+d[1],p.position.y+d[2]
            if Content.walkable(state.generated_map,x,y) then
                local occupied=false
                for _,e in ipairs(state.entities) do
                    if e.blocks_movement and e.position.x==x and e.position.y==y then
                        occupied=true
                    end
                end
                if not occupied then
                    second=Game.spawn_enemy(state,'core:enemy/ruin_skitter',x,y)
                    Scheduler.remove_for_actor(state,second.id)
                    second.hp=1
                    assert(Game.submit(state,{kind='basic_attack',dx=d[1],dy=d[2]}))
                    assert(Game.submit(state,{kind='move',dx=d[1],dy=d[2]}))
                    break
                end
            end
        end
        assert(second and state.currency>=5)
        local door
        for _,e in ipairs(state.entities) do if e.controller=='door' then door=e;break end end
        assert(door)
        local dx,dy=near(state,door)
        assert(Game.submit(state,{kind='move',dx=dx,dy=dy}))
        assert(door.open)
        local secret
        for _,entry in ipairs(state.secrets) do if entry.wall_x then secret=entry;break end end
        assert(secret)
        p.position.x,p.position.y=secret.x,secret.y
        Visibility.recompute(state)
        assert(Game.submit(state,{kind='basic_attack',
            dx=secret.wall_x-secret.x,dy=secret.wall_y-secret.y}))
        assert(secret.revealed and secret.reward_id)
        local chest
        for _,e in ipairs(state.entities) do
            if e.controller=='chest' and e.price>0 then chest=e;break end
        end
        assert(chest)
        assert(state.currency>=chest.price)
        near(state,chest)
        local offers=Game.snapshot(chest.offers)
        assert(Game.submit(state,{kind='claim_chest',chest_id=chest.id,offer_index=1}))
        assert(chest.claimed)
        local continuation_aim
        for _,d in ipairs(dirs) do
            local x,y=p.position.x+d[1],p.position.y+d[2]
            if Content.walkable(state.generated_map,x,y) then
                local occupied=false
                for _,e in ipairs(state.entities) do
                    if e.blocks_movement and e.position.x==x and e.position.y==y then
                        occupied=true
                    end
                end
                if not occupied then
                    local target=Game.spawn_enemy(state,'core:enemy/ruin_skitter',x,y,
                        state.clock+1000)
                    Scheduler.remove_for_actor(state,target.id)
                    target.hp=1
                    continuation_aim=d
                    break
                end
            end
        end
        assert(continuation_aim)
        assert(service:save(state))
        local loaded=assert(P.new(store):load())
        equal(Game.snapshot(state),Game.snapshot(loaded))
        equal(offers,Game.entity(loaded,chest.id).offers)
        local restored_secret
        for _,entry in ipairs(loaded.secrets) do
            if entry.id==secret.id then restored_secret=entry;break end
        end
        assert(Game.entity(loaded,door.id).open and restored_secret
            and restored_secret.revealed and restored_secret.reward_id==secret.reward_id)
        local _,_,a=Game.submit(state,{kind='basic_attack',
            dx=continuation_aim[1],dy=continuation_aim[2]})
        local _,_,b=Game.submit(loaded,{kind='basic_attack',
            dx=continuation_aim[1],dy=continuation_aim[2]})
        equal(a,b);equal(Game.snapshot(state),Game.snapshot(loaded))
    end)
    test('delayed Temporal Echo keeps due time and ancestry through disk bytes',function()
        local store=memory();local service=P.new(store)
        local state=Game.new(71)
        local p=Game.player(state)
        Game.set_boon_stacks(state,p.id,'core:boon/temporal_echo','common',1)
        local target=Game.spawn_enemy(state,'core:enemy/thornspitter',3,2,1000)
        Scheduler.remove_for_actor(state,target.id)
        assert(Game.submit(state,{kind='basic_attack',dx=1,dy=0}))
        assert(#Game.pending_boon_effects(state)==1)
        assert(service:save(state))
        local restored=assert(P.new(store):load())
        equal(state.delayed_effects,restored.delayed_effects)
        local _,_,a=Game.submit(state,{kind='wait'})
        local _,_,b=Game.submit(restored,{kind='wait'})
        equal(a,b);equal(Game.snapshot(state),Game.snapshot(restored))
    end)
    test('stage two route and retired floor persist without regeneration',function()
        local store=memory();local service=P.new(store)
        local state=Game.new_stage(17)
        quiet(state)
        local elite=Game.entity(state,state.stage.elite_id)
        local p=Game.player(state)
        p.damage=elite.hp+elite.defense
        local dx,dy=near(state,elite)
        assert(Game.submit(state,{kind='basic_attack',dx=dx,dy=dy}))
        local exit=Game.entity(state,state.stage.exit_id)
        near(state,exit)
        local options=assert(Game.route_options(state,exit.id))
        local decision_time=state.clock
        assert(service:save(state))
        local reopened=assert(P.new(store):load())
        equal(options,Game.route_options(reopened,exit.id))
        assert(reopened.clock==decision_time)
        assert(Game.submit(state,{kind='choose_route',exit_id=exit.id,route_index=2}))
        assert(state.stage.index==2 and #state.retired_stages==1)
        assert(service:save(state))
        local loaded=assert(P.new(store):load())
        equal(Game.snapshot(state),Game.snapshot(loaded))
        assert(loaded.retired_stages[1].stage.phase=='transition')
        local _,_,a=Game.submit(state,{kind='wait'})
        local _,_,b=Game.submit(loaded,{kind='wait'})
        equal(a,b);equal(Game.snapshot(state),Game.snapshot(loaded))
    end)
    test('unclaimed chest menu reload retains three cached offers and one debit',function()
        local store=memory();local service=P.new(store)
        local state=Game.new_generated(21)
        quiet(state)
        local chest
        for _,e in ipairs(state.entities) do
            if e.controller=='chest' and e.price>0 then chest=e;break end
        end
        assert(chest)
        state.currency=chest.price+3
        near(state,chest)
        local before=Game.snapshot(state)
        local offers=assert(Game.chest_options(state,chest.id))
        assert(service:save(state))
        local loaded=assert(P.new(store):load())
        equal(before,Game.snapshot(loaded))
        equal(offers,Game.chest_options(loaded,chest.id))
        assert(Game.submit(loaded,{kind='claim_chest',chest_id=chest.id,offer_index=2}))
        assert(loaded.currency==3 and Game.entity(loaded,chest.id).claimed)
        assert(not Game.submit(loaded,{kind='claim_chest',chest_id=chest.id,offer_index=2}))
        assert(loaded.currency==3)
    end)
    test('pending Warden telegraph and extraction deadline resume once',function()
        local store=memory();local service=P.new(store)
        local state=Game.new_stage(11)
        quiet(state)
        local elite=Game.entity(state,state.stage.elite_id)
        local p=Game.player(state)
        p.hp,p.max_hp,p.defense,p.speed=1000,1000,100,200
        near(state,elite)
        assert(Game.submit(state,{kind='wait'}))
        assert(elite.pending_attack)
        assert(service:save(state))
        local restored=assert(P.new(store):load())
        equal(elite.pending_attack,Game.entity(restored,elite.id).pending_attack)
        local a,b=select(3,Game.submit(state,{kind='wait'})),
            select(3,Game.submit(restored,{kind='wait'}))
        equal(a,b);equal(Game.snapshot(state),Game.snapshot(restored))
        quiet(state)
        elite=Game.entity(state,state.stage.elite_id)
        local dx,dy=near(state,elite)
        p.damage=elite.hp+elite.defense
        assert(Game.submit(state,{kind='basic_attack',dx=dx,dy=dy}))
        assert(state.stage.phase=='extraction')
        assert(service:save(state))
        restored=assert(P.new(store):load())
        assert(restored.stage.deadline==state.stage.deadline)
        while state.clock<state.stage.deadline do
            local _,_,one=Game.submit(state,{kind='wait'})
            local _,_,two=Game.submit(restored,{kind='wait'})
            equal(one,two)
        end
        equal(Game.snapshot(state),Game.snapshot(restored))
        assert(state.stage.phase=='pursuit' and state.stage.pursuer_id)
    end)
    test('pressure timer and RNG continue through a save before reinforcement',function()
        local store=memory();local service=P.new(store)
        local state=Game.new_stage(18)
        local p=Game.player(state)
        p.hp,p.max_hp,p.defense=1000,1000,100
        while state.clock<1100 do assert(Game.submit(state,{kind='wait'})) end
        assert(state.stage.next_pressure>=state.clock)
        assert(service:save(state))
        local restored=assert(P.new(store):load())
        equal(state.stage.pressure_rng,restored.stage.pressure_rng)
        for _=1,4 do
            local _,_,a=Game.submit(state,{kind='wait'})
            local _,_,b=Game.submit(restored,{kind='wait'})
            equal(a,b)
        end
        equal(Game.snapshot(state),Game.snapshot(restored))
    end)
    test('real filesystem adapter survives fresh service and state object',function()
        local dir=os.tmpname()
        os.remove(dir)
        assert(os.execute('mkdir -p '..dir)==0)
        local store={}
        function store:read(name)
            local f=io.open(dir..'/'..name,'rb')
            if not f then return nil end
            local bytes=f:read('*a');f:close();return bytes
        end
        function store:write(name,bytes)
            local f=assert(io.open(dir..'/'..name,'wb'))
            f:write(bytes);f:close();return true
        end
        local state=Game.new_stage(9)
        assert(P.new(store):save(state))
        state=nil
        local restored=assert(P.new(store):load())
        assert(restored.run_seed==9 and restored.clock==0)
        os.remove(dir..'/'..P.slots[1]);os.remove(dir..'/'..P.slots[2])
        os.execute('rmdir '..dir)
    end)
end
