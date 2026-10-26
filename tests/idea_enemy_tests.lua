return function(test,equal)
    local Game=require('src.game')
    local Content=require('src.content')
    local AI=require('src.ai')
    local Codec=require('src.persistence')
    local Playback=require('src.playback')
    local Biomes=require('src.biomes')
    local function copy(v)
        if type(v)~='table' then return v end
        local out={};for k,x in pairs(v) do out[k]=copy(x) end;return out
    end
    local function trial()
        local s=Game.new(37)
        local p=Game.player(s);p.position.x,p.position.y=7,6
        local e=Game.spawn_enemy(s,'core:enemy/heavy_rammer',6,6,0)
        return s,p,e
    end
    local function events(list,kind)
        local out={};for _,e in ipairs(list) do if e.kind==kind then out[#out+1]=e end end
        return out
    end
    test('Rammer AI is read-only and commits a cardinal locked shove with strict due time',function()
        local s,p,e=trial();local before=Game.snapshot(s)
        local a=AI.choose(s,e);equal(a,AI.choose(s,e));equal(before,Game.snapshot(s))
        assert(a.shove.dx==1 and a.shove.dy==0 and a.cells[1].x==7)
        local _,_,ev=Game.submit(s,{kind='wait'})
        assert(s.clock==100 and e.pending_attack.due==157 and p.hp==24)
        assert(events(ev,'WindupStarted')[1].shove.x==7)
        assert(Game.submit(s,{kind='wait'}))
        assert(s.clock==200 and p.hp==22 and p.position.x==8 and p.position.y==6)
    end)
    test('Rammer impact never tracks a target that left its locked cell',function()
        local s,p=trial();assert(Game.submit(s,{kind='wait'}))
        local _,_,ev=Game.submit(s,{kind='move',dx=0,dy=-1})
        assert(p.hp==24 and p.position.x==7 and p.position.y==5)
        assert(#events(ev,'Displaced')==0 and #events(ev,'DamageTaken')==0)
    end)
    test('shove rejects walls, occupants, stairs and exits without moving readiness or floor',function()
        local s,p,e=trial();s.meta.eligible=true
        local blocker=Game.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,100000)
        local before=Game.snapshot(s)
        local landing,reason=Game.shove_landing(s,p.id,1,0)
        assert(not landing and reason=='occupied');equal(before,Game.snapshot(s))
        assert(Game.submit(s,{kind='wait'}))
        local _,_,ev=Game.submit(s,{kind='wait'})
        assert(p.position.x==7 and p.hp==22 and s.clock==200 and s.meta.eligible)
        assert(events(ev,'ShoveBlocked')[1].reason=='occupied')
        blocker.position.x=9
        s.generated_map=copy(Content.map_for(s))
        s.generated_map.placements={stairs={{x=8,y=6,to_floor_id='other',to_x=2,to_y=2}}}
        assert(select(2,Game.shove_landing(s,p.id,1,0))=='stairs')
        s.generated_map.placements.stairs={}
        blocker.controller='exit';blocker.blocks_movement=false;blocker.position.x=8
        assert(select(2,Game.shove_landing(s,p.id,1,0))=='exit')
        assert(select(2,Game.shove_landing(s,p.id,2,0))=='invalid direction')
        p.position.x,p.position.y=2,2
        assert(select(2,Game.shove_landing(s,p.id,-1,0))=='wall')
    end)
    test('forced entry triggers brambles once, collects supplies and keeps the action gate',function()
        local s,p=trial();s.generated_map=copy(Content.map_for(s))
        local row=s.generated_map.rows[6]
        s.generated_map.rows[6]=row:sub(1,7)..'b'..row:sub(9)
        local Rewards=require('src.rewards')
        p.hp=12
        Rewards.spawn_supply(s,'food',s.active_floor_id,8,6,function()end)
        assert(Game.submit(s,{kind='wait'}))
        local _,_,ev=Game.submit(s,{kind='wait'})
        assert(#events(ev,'TerrainTriggered')==1 and #events(ev,'SupplyCollected')==1)
        assert(p.hp==13 and s.clock==200 and s.drops[1].collected)
    end)
    test('a forced snare entry delays the queued player recovery exactly once',function()
        local s,p=trial()
        local setter=Game.spawn_enemy(s,'core:enemy/snare_scribe',10,6,100000)
        s.enemy_snares={{source_id=setter.id,floor_id=s.active_floor_id,x=8,y=6}}
        assert(Game.submit(s,{kind='wait'}))
        local _,_,ev=Game.submit(s,{kind='wait'})
        assert(p.position.x==8 and p.hp==21 and s.clock==240)
        assert(#events(ev,'EnemySnareTriggered')==1 and not p.snare_delay and #s.enemy_snares==0)
        assert(Game.submit(s,{kind='move',dx=0,dy=-1}))
        assert(s.clock==340,'snare delay applied twice')
    end)
    test('Rammer friendly fire shoves an ally and interrupts that ally’s windup',function()
        local s,p=trial();assert(Game.submit(s,{kind='wait'}))
        local guard=Game.spawn_enemy(s,'core:enemy/mossbound_guard',7,5,100)
        -- Move away legally; the guard then occupies the old locked impact cell.
        guard.position.x,guard.position.y=7,6
        local _,_,ev=Game.submit(s,{kind='move',dx=0,dy=-1})
        assert(p.position.x==7 and p.position.y==5 and p.hp==24)
        assert(guard.position.x==8 and guard.position.y==6 and not guard.pending_attack)
        assert(#events(ev,'WindupCancelled')==1)
        assert(events(ev,'DamageTaken')[1].target_id==guard.id)
    end)
    test('pending shove round-trips through disk codec and corrupted direction rejects',function()
        local s=trial();assert(Game.submit(s,{kind='wait'}))
        local snapshot=Game.snapshot(s)
        local restored=Game.restore(Codec.decode(Codec.encode(snapshot)))
        local _,_,a=Game.submit(s,{kind='wait'});local _,_,b=Game.submit(restored,{kind='wait'})
        equal(a,b);equal(Game.snapshot(s),Game.snapshot(restored))
        for _,e in ipairs(snapshot.entities) do if e.pending_attack then e.pending_attack.shove.dx=2 end end
        assert(not pcall(Game.restore,snapshot))
    end)
    test('shove playback slides the victim without changing the authoritative snapshot',function()
        local s,p=trial();assert(Game.submit(s,{kind='wait'}))
        local _,_,ev=Game.submit(s,{kind='wait'})
        local before=Game.snapshot(s);local pb=Playback.new()
        Playback.add(pb,ev,p.id)
        local found=false
        for _,item in ipairs(pb.items) do
            local move=item.motions and item.motions[p.id]
            if move then found=true;assert(not move.snap and #move.path==2) end
        end
        assert(found)
        for _=1,50 do Playback.update(pb,.02) end
        equal(before,Game.snapshot(s))
    end)
    test('natural stage replacements are bounded, deterministic and preserved on restore',function()
        local seen=0
        for _,id in ipairs(Biomes.order) do for seed=1,5 do
            local first=id==Biomes.order[1]
            local s=Game.new_stage(seed,{multifloor=true,stage_options={biome_id=id,
                index=first and 1 or 2,
                route_history=first and {} or {(id:gsub('^core:biome/','core:route/'))}}})
            assert(s.generated_map.biome_id==id)
            local count={}
            for _,e in ipairs(s.entities) do if e.definition_id=='core:enemy/heavy_rammer' then
                count[e.position.floor_id]=(count[e.position.floor_id] or 0)+1
                seen=seen+1
            end end
            for _,n in pairs(count) do assert(n<=1) end
            local restored=Game.restore(Codec.decode(Codec.encode(Game.snapshot(s))))
            equal(Game.snapshot(s),Game.snapshot(restored))
        end end
        assert(seen>0,'Rammer was never consumed by actual stage population')
        equal(Game.snapshot(Game.new_stage(18,{multifloor=true})),
            Game.snapshot(Game.new_stage(18,{multifloor=true})))
    end)
    test('legal travel driver breaks opposed-lane dodge loops without assistance',function()
        local Play=require('tests.legal_playthrough')
        local s=Game.new(37,nil,{class_id='core:actor/arcanist'})
        s.generated_map=copy(Content.map_for(s))
        local map=s.generated_map
        for y=1,map.height do map.rows[y]=string.rep('#',map.width)end
        map.rows[6]='#.............#';map.rows[5]='######.########'
        local p=Game.player(s);p.position.x,p.position.y=7,5
        -- Faster opposed ranged fixtures reproduce a permanent greedy-dodge
        -- loop (the previous driver reaches its 60-action limit at full HP).
        Game.spawn_enemy(s,'core:enemy/thornspitter',3,6,20).speed=110
        Game.spawn_enemy(s,'core:enemy/thornspitter',12,6,20).speed=110
        local eligibility=s.meta.eligible
        local run={state=s,trace={},events={}}
        assert(Play.navigate(run,function(at)return at.x==11 and at.y==6 end,60))
        local dashed=false
        for _,v in ipairs(run.trace)do if v.intent.kind=='dash'then dashed=true end end
        assert(dashed and p.hp<p.max_hp and p.hp>0 and s.meta.eligible==eligibility,
            'driver must accept ordinary risk, without HP/position assistance')
    end)
    test('legal driver holds against fully armoured strikes but still dodges shove threats',function()
        local Play=require('tests.legal_playthrough')
        local s=Game.new(37);local p=Game.player(s);p.position.x,p.position.y=7,6;p.defense=10
        Game.spawn_enemy(s,'core:enemy/mossbound_guard',6,6,50)
        assert(Game.submit(s,{kind='wait'}));assert(Game.pending_strikes(s)[1])
        local before=Game.snapshot(s)
        assert(not Play.escape({state=s,trace={},events={}},nil,true));equal(before,Game.snapshot(s))
        local rammer,player=trial();player.defense=10
        assert(Game.submit(rammer,{kind='wait'}))
        assert(Play.escape({state=rammer,trace={},events={}},nil,true))
        assert(player.position.x~=7 or player.position.y~=6)
    end)
    test('legal driver respects projectile corners and uses an area attack against a protected formation',function()
        local Play=require('tests.legal_playthrough')
        local s=Game.new(37,nil,{class_id='core:actor/alchemist'})
        local p=Game.player(s);p.position.x,p.position.y=6,6
        local e=Game.spawn_enemy(s,'core:enemy/mossbound_guard',7,7,100000)
        local run={state=s,trace={},events={}};local before=Game.snapshot(s)
        assert(not Play.attack_adjacent(run));equal(before,Game.snapshot(s))
        assert(Play.action(run,{kind='move',dx=1,dy=0}))
        assert(Play.attack_adjacent(run) and e.hp<e.max_hp)

        s=Game.new(37,nil,{class_id='core:actor/alchemist'});p=Game.player(s)
        p.position.x,p.position.y=7,6
        local caster=Game.spawn_enemy(s,'core:enemy/ward_weaver',10,6,100000)
        local guard=Game.spawn_enemy(s,'core:enemy/bodyguard_brute',8,6,100000)
        caster.squad_id=caster.id;caster.squad_kind='warded_bodyguards'
        caster.ai_memory.ward_ready=true
        guard.squad_id=caster.id;guard.squad_leader_id=caster.id;guard.squad_kind=caster.squad_kind
        assert(require('src.enemy_squads').ward_source(s,guard)==caster)
        assert(Play.attack_in_reach({state=s,trace={},events={}}))
        assert(caster.hp==2 and guard.hp==guard.max_hp,
            'area attack must reach caster while retaining the primary batch field rule')
    end)
end
