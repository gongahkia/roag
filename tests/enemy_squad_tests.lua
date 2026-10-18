return function(test,equal)
    local Game=require('src.game')
    local S=require('src.enemy_squads')
    local AI=require('src.ai')
    local Content=require('src.content')
    local Codec=require('src.persistence')
    local RNG=require('src.rng')
    local Biomes=require('src.biomes')
    local function trial(kind,px,py)
        local state=Game.new(37)
        local p=Game.player(state);p.position.x,p.position.y=px or 9,py or 6
        p.hp,p.max_hp=100,100
        return state,p
    end
    local function member(state,role,x,y,leader,due)
        local e=Game.spawn_enemy(state,'core:enemy/'..role,x,y,due or 100000)
        e.squad_id='test:squad';e.squad_kind=({ward_weaver='warded_bodyguards',
            bodyguard_brute='warded_bodyguards',mend_acolyte='healer_rushwings',
            rushwing='healer_rushwings',snare_scribe='snares_and_siege',
            siege_wisp='snares_and_siege',shield_bearer='shield_and_archer',
            pin_archer='shield_and_archer',banner_captain='captain_and_followers',
            banner_follower='captain_and_followers'})[role]
        e.squad_leader_id=leader and leader.id or e.id
        e.squad_home={floor_id=e.position.floor_id,x=leader and leader.position.x or x,
            y=leader and leader.position.y or y}
        return e
    end
    local function events(events,kind)
        local out={};for _,e in ipairs(events) do if e.kind==kind then out[#out+1]=e end end
        return out
    end
    test('approved squads have ten registered roles and exact reviewed sprite assignments',function()
        local K=require('src.kenney_visuals');local O=require('src.onebit_visuals')
        assert(#S.enemies==10 and #S.order==5)
        for _,row in ipairs(S.enemies) do
            local id='core:enemy/'..row[1]
            assert(Content.get_enemy(id).ai=='squad')
            assert(K.squad_actors[id]==S.tiny[row[1]] and O.actors[id]==S.onebit[row[1]])
        end
        assert(S.tiny.ward_weaver==84 and S.onebit.ward_weaver==319 and S.tiny.snare_scribe==124)
    end)
    test('completed field protects only linked nearby brutes; LOS, stun and caster death break it',function()
        local state,p=trial(nil,9,7)
        local mage=member(state,'ward_weaver',8,8)
        local brute=member(state,'bodyguard_brute',9,8,mage)
        assert(not S.ward_source(state,brute))
        mage.ai_memory.ward_ready=true
        local hp=brute.hp
        local _,_,ev=Game.submit(state,{kind='basic_attack',dx=0,dy=1})
        assert(brute.hp==hp and events(ev,'DamageBlocked')[1].block_reason=='forcefield')
        assert(not S.ward_source(state,mage))
        brute.position.x=13;assert(not S.ward_source(state,brute))
        brute.position.x=9;mage.stunned_until=state.clock+100
        assert(not S.ward_source(state,brute));mage.stunned_until=0
        local wall=Game.spawn_door(state,8,7)
        brute.position.x,brute.position.y=8,6
        assert(not S.ward_source(state,brute));wall.open=true;wall.blocks_vision=false;wall.blocks_projectiles=false
        assert(S.ward_source(state,brute))
        mage.hp=0;assert(not S.ward_source(state,brute))
    end)
    test('guard lure leaves the four-tile field but respects six-tile leash on normal AI turns',function()
        local state,p=trial(nil,13,8)
        local mage=member(state,'ward_weaver',7,8)
        mage.ai_memory.ward_ready=true
        local brute=member(state,'bodyguard_brute',11,8,mage)
        local before=Game.snapshot(state)
        local intent=AI.choose(state,brute)
        equal(before,Game.snapshot(state))
        assert(intent.kind=='move' and intent.dx==1)
        brute.position.x=12;assert(not S.ward_source(state,brute))
        brute.position.x=13;p.position.x=14
        assert(AI.choose(state,brute).kind=='windup')
        brute.position.x=14;p.position.x=13
        intent=AI.choose(state,brute)
        assert(intent.kind=='move','outside leash must return rather than attack')
    end)
    test('ward activation is a cancellable locked channel and resumes byte-identically',function()
        local state=trial()
        local mage=member(state,'ward_weaver',9,8,nil,0)
        assert(Game.submit(state,{kind='wait'}))
        assert(mage.pending_attack.support.kind=='ward' and not mage.ai_memory.ward_ready)
        local saved=Game.snapshot(state)
        local restored=Game.restore(Codec.decode(Codec.encode(saved)))
        local _,_,a=Game.submit(state,{kind='wait'})
        local _,_,b=Game.submit(restored,{kind='wait'})
        equal(a,b);equal(Game.snapshot(state),Game.snapshot(restored))
        assert(mage.ai_memory.ward_ready and #events(a,'SquadWardRaised')==1)
        local cancelled=Game.restore(saved)
        assert(Game.stun(cancelled,mage.id,300))
        assert(Game.submit(cancelled,{kind='wait'}))
        assert(not Game.entity(cancelled,mage.id).ai_memory.ward_ready)
    end)
    test('primary multi-target damage evaluates the ward before the full HP batch commits',function()
        local state,p=trial(nil,8,7);p.damage,p.slash_damage=30,30
        local mage=member(state,'ward_weaver',8,8)
        local brute=member(state,'bodyguard_brute',9,8,mage)
        mage.ai_memory.ward_ready=true
        local _,_,ev=Game.submit(state,{kind='sweeping_slash',dx=0,dy=1})
        assert(not Game.entity(state,mage.id) and brute.hp==brute.max_hp)
        assert(#events(ev,'DamageBatchResolved')>=1 and not S.ward_source(state,brute))
        assert(Game.submit(state,{kind='basic_attack',dx=1,dy=1}))
        assert(not Game.entity(state,brute.id))
    end)
    test('directional shield uses authoritative facing and leaves rear, DOT and secondary damage open',function()
        local state,p=trial(nil,9,7)
        local shield=member(state,'shield_bearer',9,8)
        shield.ai_memory.shield_dx,shield.ai_memory.shield_dy=0,-1
        assert(S.block(state,shield,p,true)=='directional shield')
        assert(not S.block(state,shield,p,false))
        p.position.x,p.position.y=10,8;assert(not S.block(state,shield,p,true))
        p.position.x,p.position.y=9,9;assert(not S.block(state,shield,p,true))
        p.position.y=7
        local hp=shield.hp
        assert(Game.submit(state,{kind='basic_attack',dx=0,dy=1}));assert(shield.hp==hp)
        shield.stunned_until=state.clock+200
        assert(Game.submit(state,{kind='basic_attack',dx=0,dy=1}));assert(shield.hp<hp)
    end)
    test('captain rally is frozen into future follower commitments and removed on leader death',function()
        local state,p=trial(nil,9,7)
        local captain=member(state,'banner_captain',8,8)
        local follower=member(state,'banner_follower',9,8,captain,0)
        follower.speed=50
        assert(Game.submit(state,{kind='wait'}))
        assert(follower.pending_attack.damage==2)
        p.damage=30
        local _,_,ev=Game.submit(state,{kind='basic_attack',dx=-1,dy=1})
        assert(not Game.entity(state,captain.id) and not S.rally_source(state,follower))
        local hit=events(ev,'DamageTaken')
        assert(hit[#hit].target_id==p.id and hit[#hit].attempted_damage==2)
        for _=1,3 do
            assert(Game.submit(state,{kind='wait'}))
            if follower.pending_attack then break end
        end
        assert(follower.pending_attack and follower.pending_attack.damage==1)
    end)
    test('healer targets a locked wounded ally and separate attackers choose different approaches',function()
        local state,p=trial(nil,9,5)
        local healer=member(state,'mend_acolyte',9,8)
        local a=member(state,'rushwing',8,7,healer)
        local b=member(state,'rushwing',10,7,healer)
        a.hp=1
        local intent=AI.choose(state,healer)
        assert(intent.support.kind=='heal' and intent.support.target_id==a.id and intent.support.power==2)
        a.hp=a.max_hp
        local first,second=AI.choose(state,a),AI.choose(state,b)
        assert(first.kind=='move' and second.kind=='move')
        assert(a.position.x+first.dx~=b.position.x+second.dx or a.position.y+first.dy~=b.position.y+second.dy)
    end)
    test('snare placement is locked, capped, visible data and leaves an untrapped escape',function()
        local state,p=trial()
        local scribe=member(state,'snare_scribe',9,8,nil,0)
        assert(Game.submit(state,{kind='wait'}))
        assert(scribe.pending_attack.support.kind=='snare')
        local saved=Game.restore(Game.snapshot(state))
        local _,_,a=Game.submit(state,{kind='wait'})
        local _,_,b=Game.submit(saved,{kind='wait'})
        equal(a,b);equal(Game.snapshot(state),Game.snapshot(saved))
        local trap=assert(state.enemy_snares[1]);assert(#events(a,'EnemySnarePlaced')==1)
        local old_time,old_hp=state.clock,p.hp
        local dx,dy=trap.x-p.position.x,trap.y-p.position.y
        assert(math.abs(dx)+math.abs(dy)==1)
        local _,_,ev=Game.submit(state,{kind='move',dx=dx,dy=dy})
        assert(p.hp==old_hp-1 and state.clock==old_time+140 and #state.enemy_snares==0)
        assert(#events(ev,'EnemySnareTriggered')==1)
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
        state.enemy_snares={{source_id=scribe.id,floor_id=p.position.floor_id,x=8,y=6},
            {source_id=scribe.id,floor_id=p.position.floor_id,x=10,y=6}}
        local intent=AI.choose(state,scribe)
        assert(not intent.support or intent.support.kind~='snare')
        local bad=Game.snapshot(state);bad.enemy_snares[3]=bad.enemy_snares[1]
        assert(not pcall(Game.restore,bad))
    end)
    test('artillery locks a compact square and archer never selects a friendly-blocked shot',function()
        local state,p=trial(nil,9,6)
        local wisp=member(state,'siege_wisp',9,9)
        local intent=AI.choose(state,wisp)
        assert(intent.ability_id=='core:ability/siege_square' and #intent.cells==4 and intent.ranged=='area')
        local archer=member(state,'pin_archer',10,9)
        p.position.x,p.position.y=10,6
        local shield=member(state,'shield_bearer',10,8,nil)
        intent=AI.choose(state,archer)
        assert(intent.ability_id~='core:ability/pin_shot')
        shield.position.x=11
        assert(AI.choose(state,archer).ability_id=='core:ability/pin_shot')
    end)
    test('generated squads fit open counterplay patches, seed replay and old snapshots never repopulate',function()
        local seen,followers={},{}
        local total=0
        for _,biome in ipairs(Biomes.order) do for seed=1,30 do
            local first=biome==Biomes.order[1]
            local options={stage_options={index=first and 1 or 2,biome_id=biome,
                route_history=first and {} or {(biome:gsub('^core:biome/','core:route/'))}}}
            local state=Game.new_stage(seed,options)
            assert(state.rng.state==RNG.new(seed).state)
            local leader,count
            for _,e in ipairs(state.entities) do if e.squad_id then
                count=(count or 0)+1
                seen[e.squad_kind]=true
                if e.id==e.squad_leader_id then leader=e;total=total+1 end
            end end
            if leader then
                if leader.squad_kind=='captain_and_followers' then
                    assert(count>=3 and count<=6);followers[count-1]=true
                elseif leader.squad_kind=='warded_bodyguards' then
                    local points={{x=leader.position.x,y=leader.position.y}}
                    for _,e in ipairs(state.entities) do if e.squad_leader_id==leader.id and e.id~=leader.id then
                        points[#points+1]=e.position
                    end end
                    assert(S.flank_reachable(state.generated_map,points))
                end
                equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
            end
        end end
        for _,kind in ipairs(S.order) do assert(seen[kind],'unplaced squad '..kind) end
        for n=2,5 do assert(followers[n],'missing seeded follower count '..n) end
        print(('SQUAD CORPUS 210 floors: %d squads; five combinations, all 2–5 follower sizes'):format(total))
        local state=Game.new_generated(401)
        local before=Game.snapshot(state)
        equal(before,Game.snapshot(Game.restore(before)))
    end)
    test('invulnerable guards cannot seal the only generated approach to their wizard',function()
        local map={start_x=2,start_y=3,rows={'#######','#######','#.....#','#######','#######'}}
        local points={{x=5,y=3},{x=3,y=3}}
        assert(not S.flank_reachable(map,points))
        map.rows[2]='#.....#'
        assert(S.flank_reachable(map,points))
    end)
    test('completed healer channel restores one wounded ally but does not follow a displaced target',function()
        local state=trial()
        local healer=member(state,'mend_acolyte',9,8,nil,0)
        local wing=member(state,'rushwing',8,8,healer);wing.hp=1
        assert(Game.submit(state,{kind='wait'}))
        assert(healer.pending_attack.support.target_id==wing.id)
        local moved=Game.restore(Game.snapshot(state))
        assert(Game.displace(moved,wing.id,7,8))
        local _,_,ev=Game.submit(state,{kind='wait'})
        assert(wing.hp==3 and #events(ev,'Healed')==1)
        assert(Game.submit(moved,{kind='wait'}))
        assert(Game.entity(moved,wing.id).hp==1,'locked heal followed its former target')
    end)
    test('squad artillery retains locked cells and canonical enemy-on-enemy damage batching',function()
        local state,p=trial()
        Game.spawn_enemy(state,'core:enemy/siege_wisp',9,9,0)
        local ally=Game.spawn_enemy(state,'core:enemy/mossbound_guard',10,6,100000)
        assert(Game.submit(state,{kind='wait'}))
        local saved=Game.restore(Game.snapshot(state))
        local output={}
        for _=1,2 do
            local _,_,a=Game.submit(state,{kind='wait'})
            local _,_,b=Game.submit(saved,{kind='wait'})
            equal(a,b)
            for _,e in ipairs(a) do output[#output+1]=e end
        end
        local batch=events(output,'DamageBatchResolved')[1]
        assert(batch and #batch.outcomes==2 and p.hp==98 and ally.hp==9)
        equal(Game.snapshot(state),Game.snapshot(saved))
    end)
    test('caster death removes its bounded snares and existing saves gain no new encounters',function()
        local state,p=trial(nil,9,7);p.damage=30
        local scribe=member(state,'snare_scribe',9,8)
        state.enemy_snares={{source_id=scribe.id,floor_id=state.active_floor_id,x=8,y=6}}
        assert(Game.restore(Game.snapshot(state)))
        assert(Game.submit(state,{kind='basic_attack',dx=0,dy=1}))
        assert(#state.enemy_snares==0 and not Game.entity(state,scribe.id))
        assert(Game.restore(Game.snapshot(state)))
        local old=Game.new_generated(41)
        local before=Game.snapshot(old)
        local resumed=Game.restore(Codec.decode(Codec.encode(before)))
        equal(before,Game.snapshot(resumed))
        for _,e in ipairs(resumed.entities) do assert(not e.squad_id) end
        local corrupt=Game.snapshot(state)
        corrupt.enemy_snares={{source_id=scribe.id,floor_id=state.active_floor_id,x=8,y=6}}
        assert(not pcall(Game.restore,corrupt))
    end)
    test('shield bash lowers protection through its windup and recovery for corridor counterplay',function()
        local state,p=trial(nil,9,7)
        local shield=member(state,'shield_bearer',9,8,nil,0)
        shield.ai_memory.shield_dx,shield.ai_memory.shield_dy=0,-1
        assert(S.block(state,shield,p,true))
        assert(Game.submit(state,{kind='wait'}))
        assert(shield.pending_attack and shield.ai_memory.shield_lowered)
        assert(not S.block(state,shield,p,true))
        local saved=Game.restore(Codec.decode(Codec.encode(Game.snapshot(state))))
        for _=1,3 do
            local intent={kind='basic_attack',dx=0,dy=1}
            local _,_,a=Game.submit(state,intent)
            local _,_,b=Game.submit(saved,intent)
            equal(a,b)
        end
        assert(not Game.entity(state,shield.id),'frontal corridor enemy had no punish window')
        equal(Game.snapshot(state),Game.snapshot(saved))
    end)
end
