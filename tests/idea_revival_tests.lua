return function(test,equal)
    local G=require('src.game')
    local AI=require('src.ai')
    local C=require('src.persistence')
    local I=require('src.idea_enemies')
    local function trial()
        local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=7,6
        local caster=G.spawn_enemy(s,'core:enemy/grave_cantor',10,6,200)
        local victim=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,100000);victim.hp=3;victim.max_hp=6
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        return s,p,caster,victim.id
    end
    local function commit(s)
        assert(G.submit(s,{kind='wait'}));assert(G.submit(s,{kind='wait'}))
        assert(s.clock==300)
    end
    local function revived(s)
        for _,e in ipairs(s.entities)do if e.revival_of then return e end end
    end
    test('Cantor records an eligible corpse and commits one pure locked revival',function()
        local s,p,caster,id=trial()
        assert(#s.idea_corpses==1 and s.idea_corpses[1].id==id and s.progression.xp_total==5)
        local before=G.snapshot(s);local intent=AI.choose(s,caster)
        equal(before,G.snapshot(s));assert(intent.support.corpse_id==id)
        commit(s);assert(caster.ai_memory.revival_spent and caster.pending_attack.due==388)
        assert(s.idea_corpses[1].claimed_by==caster.id)
        assert(G.submit(s,{kind='wait'}));local actor=assert(revived(s))
        assert(actor.hp==3 and actor.max_hp==3 and actor.revival_of==id and actor.reward_exempt)
        assert(#s.idea_corpses==0 and not caster.pending_attack)
    end)
    test('revived units never duplicate XP, currency or supplies and cannot revive again',function()
        local s,p,caster=trial();commit(s);assert(G.submit(s,{kind='wait'}))
        local actor=assert(revived(s));local xp=s.progression.xp_total;local drops=#s.drops
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        assert(not G.entity(s,actor.id) and s.progression.xp_total==xp and #s.drops==drops)
        assert(#s.idea_corpses==0 and s.rewarded_kills[actor.id])
        assert(not (AI.choose(s,caster).support))
    end)
    test('occupying a corpse cancels the only attempt without spawning into the player',function()
        local s,p,caster=trial();commit(s)
        local _,_,events=G.submit(s,{kind='move',dx=1,dy=0})
        local blocked=false;for _,e in ipairs(events)do if e.kind=='RevivalBlocked'then blocked=true end end
        assert(blocked and not revived(s) and p.position.x==8 and #s.idea_corpses==0
            and caster.ai_memory.revival_spent)
    end)
    test('stun cancels a committed revival permanently and clears its corpse marker',function()
        local s,p,caster=trial();commit(s);assert(G.stun(s,caster.id,100))
        assert(not caster.pending_attack and caster.ai_memory.revival_spent and #s.idea_corpses==0)
        assert(G.submit(s,{kind='wait'}));assert(not revived(s))
    end)
    test('a primary batch killing caster and ally cannot create a resurrection afterlife',function()
        local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=7,6
        local caster=G.spawn_enemy(s,'core:enemy/grave_cantor',8,5,200);caster.hp=3
        local ally=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,100000);ally.hp=2
        assert(G.submit(s,{kind='sweeping_slash',dx=1,dy=0}))
        assert(not G.entity(s,caster.id) and not G.entity(s,ally.id) and not revived(s))
        assert(not s.idea_corpses or #s.idea_corpses==0)
    end)
    test('corpse claims and revival budget survive save with exact subsequent rewards and RNG',function()
        local s=trial();commit(s)
        local snapshot=G.snapshot(s);local b=G.restore(C.decode(C.encode(snapshot)))
        local _,_,aev=G.submit(s,{kind='wait'});local _,_,bev=G.submit(b,{kind='wait'})
        equal(aev,bev);equal(G.snapshot(s),G.snapshot(b))
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}));assert(G.submit(b,{kind='basic_attack',dx=1,dy=0}))
        equal(G.snapshot(s),G.snapshot(b))
        snapshot.idea_corpses[1].claimed_by=999999
        assert(not pcall(G.restore,snapshot))
    end)
    test('corpse storage is bounded per floor and ignores elites, casters and revived bodies',function()
        local s=G.new(37)
        G.spawn_enemy(s,'core:enemy/grave_cantor',10,6,100000)
        local floor=s.active_floor_id
        for id=100,199 do
            I.remember_corpse(s,{id=id,controller='ai',ai_id='guard',definition_id='core:enemy/mossbound_guard',
                max_hp=10,position={floor_id=floor,x=8,y=6}})
        end
        assert(#s.idea_corpses==16)
        local before=G.snapshot(s)
        for _,case in ipairs({{'biome_elite','core:enemy/thorn_warden'},
            {'idea_regular','core:enemy/grave_cantor'}})do
            I.remember_corpse(s,{id=200,controller='ai',ai_id=case[1],definition_id=case[2],
                max_hp=10,position={floor_id=floor,x=8,y=6}})
        end
        I.remember_corpse(s,{id=201,controller='ai',ai_id='guard',reward_exempt=true,
            definition_id='core:enemy/mossbound_guard',max_hp=10,position={floor_id=floor,x=8,y=6}})
        equal(before,G.snapshot(s))
        -- Corpse markers on frozen floors must not grow beyond the restore cap.
        for index=2,3 do
            local caster=G.spawn_enemy(s,'core:enemy/grave_cantor',11,6,100000)
            caster.position.floor_id='test:frozen/'..index
            for id=1,20 do
                I.remember_corpse(s,{id=300+index*100+id,controller='ai',ai_id='guard',
                    definition_id='core:enemy/mossbound_guard',max_hp=10,
                    position={floor_id=caster.position.floor_id,x=8,y=6}})
            end
        end
        assert(#s.idea_corpses==32)
    end)
    test('natural generation includes Cantors without repopulating loaded floors',function()
        local found=0
        local B=require('src.biomes')
        for _,id in ipairs(B.order)do for seed=1,30 do
            local first=id==B.order[1]
            local s=G.new_stage(seed,{multifloor=true,stage_options={biome_id=id,
                index=first and 1 or 2,route_history=first and {} or
                    {(id:gsub('^core:biome/','core:route/'))}}})
            for _,e in ipairs(s.entities)do if e.definition_id=='core:enemy/grave_cantor'then
                found=found+1
                local map=e.position.floor_id==s.active_floor_id and s.generated_map
                    or s.inactive_floor_maps[e.position.floor_id]
                local fits,extra=I.revival_company(s,map,e)
                assert(fits and not extra,'natural Cantor lacks an eligible ally in clear revival range')
                for _,other in ipairs(s.entities)do if other.controller=='ai'
                    and I.revivable(other) and other.position.floor_id==e.position.floor_id then
                    local p=other.position
                    for _,stair in ipairs(map.placements.stairs or {})do
                        assert(p.x~=stair.x or p.y~=stair.y,'companion occupies stairs')
                    end
                end end
            end end
            equal(G.snapshot(s),G.snapshot(G.restore(C.decode(C.encode(G.snapshot(s))))))
        end end
        assert(found>=20,'insufficient natural revival encounter coverage')
        local audit=require('src.sprite_audit').collect()
        for _,pack in ipairs({'kenney','onebit'})do
            assert(#audit.packs[pack].missing==0)
            local caster,grave=false,false
            for _,h in ipairs(audit.packs[pack].bindings)do
                if h.id=='core:enemy/grave_cantor'then
                    assert(h.index==(pack=='kenney' and 88 or 318));caster=true
                elseif h.id=='core:ui/idea_corpse_effect'then assert(h.index==539);grave=true end
            end
            assert(caster and grave)
        end
    end)
    test('Cantor companion planning is pure, reuses allies and cannot occupy reserved geometry',function()
        local s=G.new(37)
        local caster=G.spawn_enemy(s,'core:enemy/grave_cantor',10,6,100000)
        local map=require('src.content').map_for(s)
        local before=G.snapshot(s)
        local fits,point=I.revival_company(s,map,caster)
        assert(fits and point);equal(before,G.snapshot(s))
        G.spawn_enemy(s,'core:enemy/mossbound_guard',9,6,100000)
        fits,point=I.revival_company(s,map,caster)
        assert(fits and not point,'existing ally should avoid an extra spawn')
        table.remove(s.entities)
        map=C.decode(C.encode(map));map.placements={stairs={}}
        for y=1,map.height do map.rows[y]=string.rep('#',map.width)end
        local points={{10,5},{11,6},{10,7},{9,6}}
        for _,p in ipairs(points)do
            local row=map.rows[p[2]];map.rows[p[2]]=row:sub(1,p[1]-1)..'.'..row:sub(p[1]+1)
            map.placements.stairs[#map.placements.stairs+1]={x=p[1],y=p[2]}
        end
        assert(not I.revival_company(s,map,caster),'stairs must remain free')
        map.placements.stairs={}
        assert(I.revival_company(s,map,caster),'ordinary adjacent floor should fit')
        for y=1,map.height do map.rows[y]=string.rep('#',map.width)end
        assert(not I.revival_company(s,map,caster),'never carve a companion into walls')
    end)
end
