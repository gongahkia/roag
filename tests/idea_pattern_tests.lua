return function(test,equal)
    local G=require('src.game')
    local C=require('src.boon_content')
    local P=require('src.boon_patterns')
    local Content=require('src.content')
    local Codec=require('src.persistence')
    local V=require('src.visibility')
    local function grant(s,slug,tier,n)
        G.set_boon_stacks(s,s.player_id,'core:boon/'..slug,tier or 'rare',n or 1)
    end
    local function fixture(slug,class,original)
        local s=G.new(37,nil,{class_id=class});local p=G.player(s)
        local map=Content.map_for(s)
        if not original then
            s.generated_map=Codec.decode(Codec.encode(map));map=s.generated_map
            for y=1,map.height do map.rows[y]=(y==1 or y==map.height)
                and string.rep('#',map.width) or '#'..string.rep('.',map.width-2)..'#'end
        end
        p.position.x,p.position.y=7,6
        if slug then grant(s,slug)end
        V.recompute(s);return s,p,map
    end
    local function wall(map,x,y)
        map.rows[y]=map.rows[y]:sub(1,x-1)..'#'..map.rows[y]:sub(x+1)
    end
    local function foe(s,x,y,hp)
        local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',x,y,100000)
        e.hp=hp or 100;e.max_hp=100;return e
    end
    local function action(s,kind,dx,dy)
        local ok,why,events=G.submit(s,{kind=kind or 'basic_attack',dx=dx or 1,dy=dy or 0})
        assert(ok,why);return events
    end
    local function event(x,y,ox,oy)
        return {floor_id='core:floor/test',x=x,y=y,from_x=x,from_y=y,
            source_position={floor_id='core:floor/test',x=ox or 7,y=oy or 6}}
    end
    local function has(cells,x,y)
        for _,c in ipairs(cells or {})do if c.x==x and c.y==y then return true end end
        return false
    end
    local function effects(events,slug)
        local list={}
        for _,e in ipairs(events)do if e.kind=='EffectApplied' and
            e.boon_id=='core:boon/'..slug then list[#list+1]=e end end
        return list
    end
    test('eight positional boons declare distinct native art, conditional previews and validated shapes',function()
        local n,seen=0,{}
        for _,id in ipairs(C.order)do local d=C.get(id)
            if d.pattern_preview then
                n=n+1;assert(not seen[d.art.index]);seen[d.art.index]=true
                assert(P.names[d.effects[1].pattern])
                assert(require('src.kenney_symbols').boons[id][1]==d.art.index)
            end
        end
        assert(n==8 and C.validate())
        local d=Codec.decode(Codec.encode(C.get('core:boon/linebreaker_pawn')))
        d.effects[1].pattern='whole_floor'
        assert(not pcall(C.validate,{[d.id]=d},{d.id}))
        d.effects[1].pattern='lance';d.predicate={op='pattern_axis',value='hexagonal'}
        assert(not pcall(C.validate,{[d.id]=d},{d.id}))
    end)
    test('lance, rook, bishop, knight and hollow ring have exact distinct bounded geometry',function()
        local s=fixture();local e=event(8,6)
        local lance=P.cells(s,'lance',e);assert(#lance==2 and has(lance,9,6)and has(lance,10,6))
        local rook=P.cells(s,'rook',e);assert(#rook==4 and has(rook,8,4)and has(rook,8,8))
        assert(#P.cells(s,'rook',event(8,7))==0)
        local bishop=P.cells(s,'bishop',event(8,7));assert(#bishop==8 and has(bishop,10,9))
        assert(#P.cells(s,'bishop',e)==0)
        local knight=P.cells(s,'knight',e);assert(#knight==8 and has(knight,10,7))
        assert(not has(knight,9,6)and not has(knight,8,6))
        local ring=P.cells(s,'crown',e);assert(#ring==16 and has(ring,10,6))
        assert(not has(ring,9,6)and not has(ring,8,6))
        assert(#P.cells(s,'fan',e)==3 and #P.cells(s,'crosswind',e)==8)
        assert(#P.cells(s,'backtrail',e)==5)
    end)
    test('walls, closed doors and diagonal stone corners clip the shared pattern geometry',function()
        local s,_,map=fixture();local e=event(8,6)
        wall(map,9,6);assert(#P.cells(s,'lance',e)==0)
        local ring=P.cells(s,'crown',e);assert(not has(ring,10,6)and has(ring,6,6))
        assert(not has(P.cells(s,'knight',e),10,6))
        local door=G.spawn_door(s,8,7)
        local rook=P.cells(s,'rook',e);assert(not has(rook,8,8)and has(rook,8,4))
        door.open=true;door.blocks_projectiles=false
        assert(has(P.cells(s,'rook',e),8,8))
        wall(map,9,7)
        assert(not has(P.cells(s,'bishop',event(8,7)),10,9))
        assert(not has(P.cells(s,'fan',e),9,7))
    end)
    test('knight endpoints require at least one clear orthogonal bending route',function()
        local s,_,map=fixture();local e=event(8,6)
        wall(map,9,6)
        assert(has(P.cells(s,'knight',e),10,7),'second leg order must remain usable')
        wall(map,8,7)
        assert(not has(P.cells(s,'knight',e),10,7),'both blocked routes cannot jump walls')
    end)
    test('pattern clipping is rotationally symmetric, deterministic and independent of gameplay RNG',function()
        for _,shape in ipairs({'lance','rook','bishop','knight','crown','crosswind','fan','backtrail'})do
            local s,_,map=fixture();wall(map,9,7);local before=G.snapshot(s)
            local e=event(8,7)
            local first=P.cells(s,shape,e)
            for _=1,20 do equal(first,P.cells(s,shape,e))end
            equal(before,G.snapshot(s))
            local r,_,rm=fixture();wall(rm,7,5)
            local rotated=P.cells(r,shape,event(8,6,9,7))
            -- Rotate around (8,6): (x,y) -> (16-x,12-y).
            if shape~='backtrail' then
                local a=P.cells(s,shape,event(8,6,7,5))
                assert(#a==#rotated,shape)
                for _,c in ipairs(a)do assert(has(rotated,16-c.x,12-c.y),shape)end
            end
        end
    end)
    test('a real lance primary hit damages only the two cells behind the victim after the primary batch',function()
        local s=fixture('linebreaker_pawn');local a=foe(s,8,6)
        local b=foe(s,9,6);local c=foe(s,10,6);local spare=foe(s,8,7)
        local preview=G.preview(s,'basic_attack',1,0)
        assert(#preview.boon_cells==2 and preview.boon_hint=='on hit')
        local events=action(s);local list=effects(events,'linebreaker_pawn')
        assert(#list==1);equal(P.cells(s,'lance',event(8,6)),list[1].target_cells)
        assert(a.hp==97 and b.hp==98 and c.hp==98 and spare.hp==100)
        local primary,activation
        for n,e in ipairs(events)do
            if e.kind=='DamageBatchResolved' and not primary then primary=n end
            if e.kind=='BoonActivated' then activation=n end
        end
        assert(primary<activation)
    end)
    test('rook and bishop require their actual primary strike axes and do not recurse on their own damage',function()
        for _,case in ipairs({{'rooks_wake',8,6,0,8,5},
            {'bishops_wake',8,7,1,9,8}})do
            local s=fixture(case[1]);foe(s,case[2],case[3]);local extra=foe(s,case[5],case[6])
            local events=action(s,'basic_attack',1,case[4])
            assert(#effects(events,case[1])==1 and extra.hp==98)
            s=fixture(case[1]);foe(s,8,case[4]==0 and 7 or 6)
            events=action(s,'basic_attack',1,case[4]==0 and 1 or 0)
            assert(#effects(events,case[1])==0)
        end
    end)
    test('special-only knight and fan leave basics unchanged and use the same cells as visible previews',function()
        for _,slug in ipairs({'knights_gambit','fan_of_blades'})do
            local s=fixture(slug);foe(s,8,6);local e=foe(s,10,7)
            assert(not G.preview(s,'basic_attack',1,0).boon_cells)
            assert(#effects(action(s),slug)==0 and e.hp==100)
            local preview=G.preview(s,'sweeping_slash',1,0)
            assert(preview.boon_hint=='on hit' and #preview.boon_cells>0)
            local events=action(s,'sweeping_slash',1,0);local list=effects(events,slug)
            assert(#list==1)
            for _,c in ipairs(preview.boon_cells)do assert(has(list[1].target_cells,c.x,c.y))end
        end
    end)
    test('a hollow crown waits for a credited kill and spares the victim inner ring',function()
        local s=fixture('hollow_crown');local a=foe(s,8,6,5)
        local inner=foe(s,9,6);local outer=foe(s,10,6)
        local preview=G.preview(s,'basic_attack',1,0)
        assert(preview.boon_hint=='on kill' and #preview.boon_cells>0)
        assert(#effects(action(s),'hollow_crown')==0 and a.hp==2)
        local events=action(s);assert(not G.entity(s,a.id))
        assert(#effects(events,'hollow_crown')==1 and outer.hp==98 and inner.hp==100)
    end)
    test('Crosswind uses paid Wait time and cardinal lanes without self damage, recursion or diagonals',function()
        local s,p=fixture('crosswind');local near=foe(s,8,6);local far=foe(s,9,6)
        local diagonal=foe(s,8,7)
        local before=G.snapshot(s);local preview=G.preview(s,'wait')
        assert(preview.boon_hint=='Wait rays' and #preview.boon_cells>0)
        equal(before,G.snapshot(s))
        local events=action(s,'wait');assert(s.clock==100 and p.hp==p.max_hp)
        assert(near.hp==98 and far.hp==98 and diagonal.hp==100)
        assert(#effects(events,'crosswind')==1)
    end)
    test('Backtrail locks the original Dash cross until 150 time and hits moved-in enemies without tracking',function()
        local s,p=fixture('backtrail_mantle');local e=foe(s,7,5)
        local preview=G.preview(s,'dash',1,0)
        assert(preview.boon_hint=='Dash wake +150 time' and #preview.boon_cells==5)
        action(s,'dash',1,0)
        assert(s.clock<150 and p.position.x==9 and e.hp==100)
        local pending=G.pending_boon_effects(s);assert(#pending==1 and pending[1].due==150)
        local before=G.snapshot(s)
        for _=1,30 do equal(pending,G.pending_boon_effects(s))end
        equal(before,G.snapshot(s))
        local arriving=foe(s,7,6)
        local events=action(s,'wait');assert(e.hp==98 and arriving.hp==98)
        assert(#G.pending_boon_effects(s)==0)
        local resolved
        for _,event in ipairs(events)do if event.kind=='DelayedEffectResolved' then resolved=event end end
        assert(resolved and resolved.time==150 and has(resolved.target_cells,7,6)
            and not has(resolved.target_cells,9,6))
    end)
    test('partial Dash still leaves one wake, but ordinary movement and escape-scroll movement cannot',function()
        local s,p=fixture('backtrail_mantle');foe(s,9,6)
        action(s,'dash',1,0);assert(p.position.x==8 and #G.pending_boon_effects(s)==1)
        s,p=fixture('backtrail_mantle');action(s,'move',1,0)
        assert(#G.pending_boon_effects(s)==0)
        p.utility={kind='escape_scroll'}
        local ok,why=G.submit(s,{kind='use_utility',dx=1,dy=0,steps=2})
        assert(ok,why);assert(#G.pending_boon_effects(s)==0)
    end)
    test('a Wayfinder beacon return cannot masquerade as a Dash wake',function()
        local s,p=fixture('backtrail_mantle','core:actor/wayfinder')
        action(s,'sweeping_slash',1,0)
        action(s,'move',-1,0)
        local preview=G.preview(s,'dash',1,0)
        assert(preview.valid and preview.beacon_return and not preview.boon_cells)
        action(s,'dash',1,0);assert(#G.pending_boon_effects(s)==0)
    end)
    test('pending wake and Temporal Echo keep separate locked cells and exact saved continuation',function()
        local s=fixture('backtrail_mantle',nil,true);grant(s,'temporal_echo');foe(s,7,5)
        action(s,'dash',1,0);foe(s,10,6)
        action(s)
        local restored=G.restore(Codec.decode(Codec.encode(G.snapshot(s))))
        assert(#G.pending_boon_effects(s)==1)
        for _,kind in ipairs({'wait','basic_attack','wait'})do
            equal(action(s,kind),action(restored,kind));equal(G.snapshot(s),G.snapshot(restored))
        end
    end)
    test('blocked hits, empty attacks and a rolled dice miss cannot activate primary-hit patterns',function()
        for _,slug in ipairs({'linebreaker_pawn','rooks_wake','bishops_wake','knights_gambit','fan_of_blades'})do
            local s=fixture(slug)
            assert(#effects(action(s),slug)==0)
            local a=foe(s,8,6);a.defense=100
            assert(#effects(action(s,'sweeping_slash'),slug)==0)
        end
        local s,p=fixture('linebreaker_pawn');local a=foe(s,8,6);local b=foe(s,9,6)
        grant(s,'fateful_dice')
        for seed=1,80 do
            if require('src.rng').next_int({state=seed},8)==8 then s.rng.state=seed;break end
        end
        local events=action(s);assert(a.hp==100 and b.hp==100 and #effects(events,'linebreaker_pawn')==0)
    end)
    test('all fifteen classes share pattern basics, including each real Duelist piercing victim',function()
        for _,class in ipairs(require('src.classes').order)do
            local s=fixture('linebreaker_pawn',class,true);foe(s,8,6);local e=foe(s,9,6)
            local preview=G.preview(s,'basic_attack',1,0)
            assert(preview.boon_cells and has(preview.boon_cells,9,6),class)
            local events=action(s);local hits,primary_loss=0,0
            for _,event in ipairs(events)do
                if event.kind=='DamageTaken' and event.direct_attack then
                    hits=hits+1
                    if event.target_id==e.id then primary_loss=event.actual_loss end
                end
            end
            assert(#effects(events,'linebreaker_pawn')==hits and e.hp==98-primary_loss,class)
            equal(G.snapshot(s),G.snapshot(G.restore(G.snapshot(s))))
        end
    end)
    test('utility placement and parry cannot trigger special-only patterns or show on-hit cells',function()
        for _,class in ipairs({'core:actor/duelist','core:actor/geomancer',
            'core:actor/tinkerer','core:actor/wayfinder','core:actor/spiritbinder'})do
            local s=fixture('knights_gambit',class);grant(s,'fan_of_blades')
            local preview=G.preview(s,'sweeping_slash',1,0)
            assert(preview.valid and not preview.boon_cells,class)
            local events=action(s,'sweeping_slash')
            assert(#effects(events,'knights_gambit')==0 and #effects(events,'fan_of_blades')==0,class)
        end
    end)
    test('simultaneous primary sweep HP changes precede every overlapping rook pattern batch',function()
        local s=fixture('rooks_wake')
        local a=foe(s,8,5);local b=foe(s,8,6);local c=foe(s,8,7)
        local events=action(s,'sweeping_slash')
        local batch,activation
        for n,e in ipairs(events)do
            if e.kind=='DamageBatchResolved' and not batch then batch=n;assert(#e.outcomes==3)end
            if e.kind=='BoonActivated' and not activation then activation=n end
        end
        assert(batch<activation and #effects(events,'rooks_wake')==1)
        assert(a.hp==96 and b.hp==98 and c.hp==96)
    end)
    test('commanded Spirit patterns use the actual spirit origin in preview and damage',function()
        local s,p=fixture('rooks_wake','core:actor/spiritbinder')
        require('src.idea_companions').place(s,p,{x=9,y=5},function()end)
        foe(s,9,6);local east=foe(s,10,6);local south=foe(s,9,7)
        local preview=G.preview(s,'sweeping_slash',1,0)
        assert(preview.valid and preview.companion_mode=='strike')
        assert(has(preview.boon_cells,10,6)and not has(preview.boon_cells,9,7))
        local events=action(s,'sweeping_slash',1,0)
        assert(#effects(events,'rooks_wake')==1 and east.hp==98 and south.hp==100)
    end)
    test('mixed rarity uncapped copies increase damage without changing geometry or multiplying activations',function()
        local s,p=fixture('linebreaker_pawn');grant(s,'linebreaker_pawn','common',10)
        grant(s,'linebreaker_pawn','legendary',20)
        foe(s,8,6);local e=foe(s,9,6,1000)
        local before=G.snapshot(s);local preview=G.preview(s,'basic_attack',1,0)
        for _=1,30 do equal(preview,G.preview(s,'basic_attack',1,0))end
        equal(before,G.snapshot(s))
        local events=action(s);assert(#effects(events,'linebreaker_pawn')==1 and e.hp==908)
    end)
    test('fog hides conditional pattern cells and unseen primary targets without changing gameplay state',function()
        local s,p=fixture('linebreaker_pawn');foe(s,8,6)
        local visible=s.visible.cells
        assert(visible)
        -- Visibility uses numeric cell keys; remove all known visible cells.
        s.visible.cells={}
        local before=G.snapshot(s)
        local preview=G.preview(s,'basic_attack',1,0)
        assert(not preview.boon_cells)
        equal(before,G.snapshot(s))
    end)
end
