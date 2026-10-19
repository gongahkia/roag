return function(test,equal)
    local G=require('src.game')
    local C=require('src.boon_content')
    local B=require('src.boons')
    local Forms=require('src.boon_patterns').weapon_forms
    local Content=require('src.content')
    local Codec=require('src.persistence')
    local V=require('src.visibility')
    local RNG=require('src.rng')
    local Q=require('src.scheduler')
    local order={'pommel_return','needle_flight','hollow_sigil','backswing',
        'distant_bastion','volatile_vial','split_thrust'}
    local function grant(s,slug,rarity,n)
        G.set_boon_stacks(s,s.player_id,'core:boon/'..slug,rarity or 'rare',n or 1)
    end
    local function fixture(slug)
        local s=G.new(37,'core:map/scrolling_room',{class_id=Forms[slug].class_id})
        local p=G.player(s);p.position.x,p.position.y=15,16
        grant(s,slug);V.recompute(s);return s,p
    end
    local function foe(s,dx,dy,definition,hp)
        local p=G.player(s).position
        local e=G.spawn_enemy(s,definition or 'core:enemy/mossbound_guard',p.x+dx,p.y+dy,100000)
        e.hp=hp or 100;e.max_hp=hp or 100;return e
    end
    local function kind(slug)
        return ({hollow_sigil=true,backswing=true,distant_bastion=true})[slug]
            and 'sweeping_slash' or 'basic_attack'
    end
    local function action(s,k,dx,dy,options)
        local intent={kind=k or 'basic_attack',dx=dx or 1,dy=dy or 0}
        if options then for key,value in pairs(options)do intent[key]=value end end
        local ok,why,events=G.submit(s,intent);assert(ok,why);return events
    end
    local function has(cells,x,y)
        for _,c in ipairs(cells)do if c.x==x and c.y==y then return true end end
        return false
    end
    local function count(events,k)
        local n=0;for _,e in ipairs(events)do if e.kind==k then n=n+1 end end;return n
    end
    local function primary(events)
        for _,e in ipairs(events)do if e.kind=='DamageBatchResolved' then return e.outcomes end end
    end
    local function prepared_mirror(slug)
        local s,p=fixture(slug)
        local e=foe(s,3,0,'core:enemy/mirror_hexer');e.hp=7;e.max_hp=7
        e.speed=75
        Q.remove_for_actor(s,e.id)
        Q.enqueue(s,{kind='actor_ready',due=50,actor_id=e.id,is_player=false})
        for _=1,4 do
            if #(s.idea_mirrors or {})>0 then break end
            action(s,'wait')
        end
        assert(#s.idea_mirrors==1,'real mirror not created')
        return s,p,e,s.idea_mirrors[1]
    end
    test('seven class weapon forms declare exact eligible abilities and native artwork',function()
        local seen={}
        for _,slug in ipairs(order)do
            local d=C.get('core:boon/'..slug);local f=Forms[slug]
            assert(d.eligible_classes[1]==f.class_id and #d.eligible_classes==1)
            assert(d.modifiers[1].kind=='weapon_form' and d.modifiers[1].form==slug
                and d.modifiers[1].ability_id==f.ability_id)
            assert(not seen[d.art.index]);seen[d.art.index]=true
            assert(require('src.kenney_symbols').boons[d.id][1]==d.art.index)
        end
        assert(C.validate())
        local d=Codec.decode(Codec.encode(C.get('core:boon/needle_flight')))
        d.modifiers[1].form='walk_through_walls'
        assert(not pcall(C.validate,{[d.id]=d},{d.id}))
        d.modifiers[1].form='needle_flight';d.eligible_classes={'core:actor/adventurer'}
        assert(not pcall(C.validate,{[d.id]=d},{d.id}))
    end)
    test('normal offers include each owned-class form and exclude every other class form',function()
        local pool=C.starter_order()
        for _,slug in ipairs(order)do pool[#pool+1]='core:boon/'..slug end
        for _,slug in ipairs(order)do
            local found=false
            for seed=1,160 do
                local s=G.new(seed,nil,{class_id=Forms[slug].class_id,available_boons=pool})
                for _,o in ipairs(require('src.rewards').make_offers(s,Content.get_chest('core:chest/standard')))do
                    if o.boon_id=='core:boon/'..slug then found=true end
                    local d=C.get(o.boon_id)
                    if d.eligible_classes then assert(d.eligible_classes[1]==Forms[slug].class_id)end
                end
            end
            assert(found,slug)
        end
    end)
    test('Pommel Return hits the front and clear rear in one primary HP batch with unchanged recovery',function()
        local s,p=fixture('pommel_return');local front=foe(s,1,0);local rear=foe(s,-1,0)
        local before=G.snapshot(s);local preview=G.preview(s,'basic_attack',1,0)
        assert(#preview.cells==2 and preview.weapon_hint:find('rear',1,true))
        equal(before,G.snapshot(s))
        local events=action(s)
        assert(front.hp==94 and rear.hp==94 and #primary(events)==2 and s.clock==100)
        assert(p.hp==p.max_hp and count(events,'AttackPerformed')==1)
    end)
    test('Pommel composes with Longblade while bump retains its original narrow footprint',function()
        for _,bump in ipairs({false,true})do
            local s,p=fixture('pommel_return');grant(s,'longblade_geometry','common',2)
            local front=foe(s,1,0);local far=foe(s,2,0);local rear=foe(s,-1,0)
            local events=action(s,bump and 'move' or 'basic_attack')
            assert(front.hp==92 and p.position.x==15)
            assert(far.hp==(bump and 100 or 92)and rear.hp==(bump and 100 or 92))
            assert(#primary(events)==(bump and 1 or 3))
        end
    end)
    test('a closed corner blocks the new rear cell without changing the normal front strike',function()
        local s,p=fixture('pommel_return')
        G.spawn_door(s,p.position.x-1,p.position.y)
        local front=foe(s,1,1);local rear=foe(s,-1,-1)
        local preview=G.preview(s,'basic_attack',1,1)
        assert(not has(preview.cells,rear.position.x,rear.position.y))
        action(s,'basic_attack',1,1);assert(front.hp==94 and rear.hp==100)
    end)
    test('Needle Flight pierces the full clear bow line but bump stops at first contact',function()
        for _,bump in ipairs({false,true})do
            local s=fixture('needle_flight');local first=foe(s,1,0);local far=foe(s,3,0)
            local events=action(s,bump and 'move' or 'basic_attack')
            assert(first.hp==95 and far.hp==(bump and 100 or 95))
            assert(#primary(events)==(bump and 1 or 2) and s.clock==87)
        end
    end)
    test('Needle and Splitflight preserve the ray plus valid branches without duplicate primary victims',function()
        local s=fixture('needle_flight');grant(s,'splitflight','common')
        local a=foe(s,1,0);local b=foe(s,2,0);local c=foe(s,1,1)
        local events=action(s);local outcomes=primary(events)
        assert(#outcomes==3 and a.hp<100 and b.hp<100 and c.hp<100)
        local seen={};for _,o in ipairs(outcomes)do assert(not seen[o.target_id]);seen[o.target_id]=true end
    end)
    test('Needle stops at a closed door and preserves ordinary contact with a breakable barricade',function()
        local s,p=fixture('needle_flight');local front=foe(s,1,0);local far=foe(s,3,0)
        G.spawn_door(s,p.position.x+2,p.position.y)
        action(s);assert(front.hp==95 and far.hp==100)
        s,p=fixture('needle_flight');front=foe(s,1,0);far=foe(s,3,0)
        s.generated_map=Codec.decode(Codec.encode(Content.map_for(s)))
        local map=s.generated_map;local x,y=p.position.x+2,p.position.y
        map.rows[y]=map.rows[y]:sub(1,x-1)..'B'..map.rows[y]:sub(x+1)
        local preview=G.preview(s,'basic_attack',1,0)
        assert(has(preview.cells,x,y))
        local events=action(s)
        assert(front.hp==95 and far.hp==100 and count(events,'TerrainBroken')==1)
        assert(map.rows[y]:sub(x,x)=='.')
    end)
    test('a piercing Needle reflects even after striking an intervening creature',function()
        local s,p,m,r=prepared_mirror('needle_flight')
        local before=foe(s,1,0);local turn=foe(s,2,1);local far=foe(s,2,2)
        V.recompute(s);local snapshot=G.snapshot(s)
        local preview=G.preview(s,'basic_attack',1,0)
        assert(preview.reflection and preview.reflection.id==r.id)
        equal(snapshot,G.snapshot(s))
        local events=action(s)
        assert(before.hp==95 and turn.hp==95 and far.hp==95 and m.hp==7)
        assert(count(events,'MirrorReflected')==1 and #s.idea_mirrors==0)
    end)
    test('Hollow Sigil spares its center while a surrounding victim takes primary fire damage',function()
        local s,p=fixture('hollow_sigil');local center=foe(s,2,0);local edge=foe(s,2,1)
        local preview=G.preview(s,'sweeping_slash',1,0)
        assert(#preview.cells==8 and not has(preview.cells,center.position.x,center.position.y))
        local events=action(s,'sweeping_slash')
        assert(center.hp==100 and edge.hp==94 and #primary(events)==1 and s.clock==170)
        assert(primary(events)[1].damage_type=='fire')
    end)
    test('Hollow and Expanding Sigil retain the outer ring while sparing the center and caster',function()
        local s,p=fixture('hollow_sigil');grant(s,'expanding_sigil','common')
        local center=foe(s,2,0);local inner=foe(s,2,1);local outer=foe(s,4,0)
        local preview=G.preview(s,'sweeping_slash',1,0)
        assert(#preview.cells==23 and not has(preview.cells,center.position.x,center.position.y)
            and not has(preview.cells,p.position.x,p.position.y))
        local events=action(s,'sweeping_slash')
        assert(center.hp==100 and inner.hp<100 and outer.hp<100 and #primary(events)==2)
    end)
    test('an unplaced blocked Hollow Sigil keeps the original short travel cells',function()
        local s,p=fixture('hollow_sigil');local door=G.spawn_door(s,p.position.x+1,p.position.y)
        local preview=G.preview(s,'sweeping_slash',1,0)
        assert(preview.valid and #preview.cells==1 and preview.cells[1].x==door.position.x)
        action(s,'sweeping_slash');assert(not door.open and s.clock==170)
    end)
    test('Backswing completes eight open neighboring cells in one deliberate primary batch',function()
        local s,p=fixture('backswing');local foes={}
        for y=-1,1 do for x=-1,1 do if x~=0 or y~=0 then foes[#foes+1]=foe(s,x,y)end end end
        local preview=G.preview(s,'sweeping_slash',1,0);assert(#preview.cells==8)
        local events=action(s,'sweeping_slash')
        assert(#primary(events)==8 and s.clock==240 and p.hp==p.max_hp)
        for _,e in ipairs(foes)do assert(e.hp==93)end
    end)
    test('Distant Bastion hits the exact two-step diamond while sparing cardinal-adjacent creatures',function()
        local s=fixture('distant_bastion');local near=foe(s,1,0)
        local far=foe(s,2,0);local diagonal=foe(s,1,1)
        local preview=G.preview(s,'sweeping_slash',1,0)
        assert(#preview.cells==8)
        for _,c in ipairs(preview.cells)do assert(math.abs(c.x-15)+math.abs(c.y-16)==2)end
        local events=action(s,'sweeping_slash')
        assert(near.hp==100 and far.hp==95 and diagonal.hp==95 and #primary(events)==2)
    end)
    test('closed doors and stone corners clip the Distant Bastion diamond rather than being bypassed',function()
        local s,p=fixture('distant_bastion');G.spawn_door(s,p.position.x+1,p.position.y)
        local far=foe(s,2,0);local diagonal=foe(s,1,1)
        local preview=G.preview(s,'sweeping_slash',1,0)
        assert(not has(preview.cells,far.position.x,far.position.y)
            and not has(preview.cells,diagonal.position.x,diagonal.position.y))
        action(s,'sweeping_slash');assert(far.hp==100 and diagonal.hp==100)
    end)
    test('a Volatile Vial keeps its clear travel cells and resolves the first-contact cross in one primary batch',function()
        local s=fixture('volatile_vial')
        local targets={foe(s,2,0),foe(s,2,-1),foe(s,2,1),foe(s,3,0)}
        local beyond=foe(s,4,0)
        local preview=G.preview(s,'basic_attack',1,0)
        assert(#preview.cells==5 and has(preview.cells,16,16)and has(preview.cells,18,16))
        local events=action(s)
        assert(#primary(events)==4 and beyond.hp==100 and s.clock==100)
        for _,e in ipairs(targets)do assert(e.hp==95)end
    end)
    test('empty and concealed-body Vial previews are identical and cannot reveal a hidden splash center',function()
        local s=fixture('volatile_vial');local e=foe(s,2,0);grant(s,'linebreaker_pawn')
        local map=Content.map_for(s)
        s.visible.cells[(e.position.y-1)*map.width+e.position.x]=nil
        local empty=Codec.decode(Codec.encode(G.snapshot(s)))
        Q.remove_for_actor(empty,e.id)
        for n,a in ipairs(empty.entities)do if a.id==e.id then table.remove(empty.entities,n);break end end
        local before=G.snapshot(s);local empty_before=G.snapshot(empty)
        equal(G.preview(s,'basic_attack',1,0),G.preview(empty,'basic_attack',1,0))
        equal(before,G.snapshot(s));equal(empty_before,G.snapshot(empty))
        action(s);assert(e.hp==95,'concealment must not remove physical projectile contact')
    end)
    test('Volatile Vial reflects before finding contact and splashing instead of disabling the mirror',function()
        local s,p,m=prepared_mirror('volatile_vial')
        local center=foe(s,2,1);local splash=foe(s,2,2);V.recompute(s)
        local before=G.snapshot(s);local preview=G.preview(s,'basic_attack',1,0)
        assert(preview.reflection and has(preview.cells,splash.position.x,splash.position.y))
        equal(before,G.snapshot(s))
        local events=action(s)
        assert(center.hp==95 and splash.hp==95 and m.hp==7)
        assert(count(events,'MirrorReflected')==1 and #s.idea_mirrors==0)
    end)
    test('a concealed body cannot leak through the Vial mirror preview but still blocks actual reflection',function()
        local s,p,m=prepared_mirror('volatile_vial');local hidden=foe(s,1,0)
        local map=Content.map_for(s)
        s.visible.cells[(hidden.position.y-1)*map.width+hidden.position.x]=nil
        local empty=Codec.decode(Codec.encode(G.snapshot(s)))
        Q.remove_for_actor(empty,hidden.id)
        for n,a in ipairs(empty.entities)do if a.id==hidden.id then table.remove(empty.entities,n);break end end
        local before=G.snapshot(s)
        local preview=G.preview(s,'basic_attack',1,0)
        assert(preview.reflection)
        equal(preview,G.preview(empty,'basic_attack',1,0));equal(before,G.snapshot(s))
        local events=action(s)
        assert(hidden.hp==95 and m.hp==7 and #s.idea_mirrors==1 and count(events,'MirrorReflected')==0)
    end)
    test('a skull Vial evaluates a living wizard forcefield before committing any primary deaths',function()
        local s,p=fixture('volatile_vial');grant(s,'fateful_dice')
        local brute=foe(s,2,0,'core:enemy/bodyguard_brute')
        local mage=foe(s,2,-1,'core:enemy/ward_weaver');mage.ai_memory.ward_ready=true
        for _,e in ipairs({mage,brute})do
            e.squad_id='vial-batch';e.squad_kind='warded_bodyguards';e.squad_leader_id=mage.id
            e.squad_home={floor_id=s.active_floor_id,x=mage.position.x,y=mage.position.y}
        end
        for seed=1,80 do if RNG.next_int({state=seed},8)==7 then s.rng.state=seed;break end end
        local events=action(s)
        assert(not G.entity(s,mage.id)and brute.hp==100 and count(events,'EntityKilled')==1)
        assert(#primary(events)==2)
    end)
    test('Split Thrust keeps the stem, forks the second step and spares its former straight endpoint',function()
        local s=fixture('split_thrust');local stem=foe(s,1,0);local straight=foe(s,2,0)
        local a=foe(s,2,-1);local b=foe(s,2,1)
        local preview=G.preview(s,'basic_attack',1,0)
        assert(#preview.cells==3 and not has(preview.cells,straight.position.x,straight.position.y))
        local events=action(s)
        assert(stem.hp==94 and a.hp==94 and b.hp==94 and straight.hp==100)
        assert(#primary(events)==3 and s.clock==87)
    end)
    test('forked thrust clips each branch independently and leaves bump unchanged',function()
        local s,p=fixture('split_thrust');local first=foe(s,1,0)
        local door=G.spawn_door(s,p.position.x+1,p.position.y-1)
        local a=foe(s,2,-1);local b=foe(s,2,1)
        action(s);assert(first.hp==94 and a.hp==100 and b.hp==94 and not door.open)
        s=fixture('split_thrust');first=foe(s,1,0);a=foe(s,2,-1);b=foe(s,2,1)
        action(s,'move');assert(first.hp==94 and a.hp==100 and b.hp==100)
    end)
    test('all seven forms keep one Vow cost and one primary critical roll for every batch',function()
        for _,slug in ipairs(order)do
            local s,p=fixture(slug);grant(s,'pilgrims_vow');p.hp=10;p.crit_chance_ppm=1000000
            foe(s,1,0);foe(s,2,1)
            local damage=select(2,G.effective_ability(s,kind(slug),1,0,true))
            local events=action(s,kind(slug),1,0,{vow=true})
            assert(p.hp==8 and count(events,'VowPaid')==1 and count(events,'AttackPerformed')==1,slug)
            for _,event in ipairs(events)do if event.kind=='AttackPerformed'then
                assert(event.raw_damage==math.floor((damage+6)*p.crit_damage_percent/100+.5),slug)
            end end
        end
    end)
    test('a dice cross stays a paid genuine miss for every form with no new hidden Echo or Burning',function()
        for _,slug in ipairs(order)do
            local s=fixture(slug);local e=foe(s,1,0);foe(s,2,1)
            grant(s,'fateful_dice');grant(s,'temporal_echo');grant(s,'emberbrand')
            for seed=1,80 do if RNG.next_int({state=seed},8)==8 then s.rng.state=seed;break end end
            local events=action(s,kind(slug))
            assert(e.hp==100 and count(events,'DamageTaken')==0
                and count(events,'BoonActivated')==0 and not next(s.delayed_effects),slug)
        end
    end)
    test('mixed rarity uncapped copies change power once without multiplying weapon cells or consuming preview RNG',function()
        for _,slug in ipairs(order)do
            local s,p=fixture(slug);foe(s,1,0);foe(s,2,1)
            local cells=G.effective_ability(s,kind(slug),1,0,true)
            grant(s,slug,'common',10);grant(s,slug,'legendary',20)
            local before=G.snapshot(s)
            for _=1,20 do G.preview(s,kind(slug),1,0)end
            equal(before,G.snapshot(s))
            local after,damage=G.effective_ability(s,kind(slug),1,0,true)
            equal(cells,after)
            local base=kind(slug)=='basic_attack' and p.damage or p.slash_damage
            assert(damage==base+B.aggregate('core:boon/'..slug,p.boons['core:boon/'..slug],'power'))
        end
    end)
    test('each saved form continues exactly with pending Echo/Burning and later transformed attacks',function()
        for _,slug in ipairs(order)do
            local s=fixture(slug);foe(s,1,0,nil,1000)
            grant(s,'temporal_echo','common');grant(s,'emberbrand','common')
            action(s);assert(next(s.delayed_effects),slug)
            local restored=G.restore(Codec.decode(Codec.encode(G.snapshot(s))))
            for _,k in ipairs({'wait',kind(slug),'wait','basic_attack'})do
                equal(action(s,k),action(restored,k));equal(G.snapshot(s),G.snapshot(restored))
            end
        end
    end)
end
