return function(test,equal)
    local J=require('src.visual_juice')
    local PB=require('src.playback')
    local G=require('src.game')
    local P=require('src.persistence')
    local UI=require('src.pixel_ui')
    local Rewards=require('src.rewards')
    local function attack(id,dx,dy)
        return {id=1,kind='AttackPerformed',source_id=id,floor_id='floor',
            from_x=8,from_y=8,target_cells={{x=8+dx,y=8+dy}}}
    end
    test('recoil follows committed player and enemy attack direction within the existing beat',function()
        for _,id in ipairs({1,2})do for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1},{-1,-1}})do
            local e=attack(id,d[1],d[2]);local events=P.encode(e)
            local pb=PB.new();PB.add(pb,{e},1)
            local duration=pb.items[1].duration;local queue=P.encode(pb)
            local dx,dy=J.attack_offset(pb,id,'floor',true);assert(dx==0 and dy==0)
            PB.update(pb,math.min(J.attack_seconds,duration)/2)
            dx,dy=J.attack_offset(pb,id,'floor',true)
            assert(dx==d[1]*3 and dy==d[2]*3)
            assert(pb.items[1].duration==duration and #pb.items==1)
            local a=P.encode(pb);J.attack_offset(pb,id,'floor',true);assert(P.encode(pb)==a)
            PB.update(pb,duration);assert(not PB.busy(pb))
            dx,dy=J.attack_offset(pb,id,'floor',true);assert(dx==0 and dy==0)
            assert(P.encode(e)==events and queue~=P.encode(pb))
        end end
    end)
    test('recoil excludes windups, hidden-floor history, disabled effects and concurrent actor slides',function()
        local pb=PB.new();local e=attack(1,1,0);PB.add(pb,{e},1);PB.update(pb,.04)
        for _,a in ipairs({{1,'other',true},{2,'floor',true},{1,'floor',false}})do
            local x,y=J.attack_offset(pb,unpack(a));assert(x==0 and y==0)
        end
        pb.items[1].motions={[1]={}};local x,y=J.attack_offset(pb,1,'floor',true);assert(x==0 and y==0)
        pb.items[1].motions=nil;e.kind='WindupStarted'
        x,y=J.attack_offset(pb,1,'floor',true);assert(x==0 and y==0)
        e.kind='AttackPerformed';e.target_cells={{x=7,y=8},{x=9,y=8}}
        x,y=J.attack_offset(pb,1,'floor',true);assert(x==0 and y==0)
    end)
    test('all fifteen class attacks retain exact events, snapshots and playback duration under recoil sampling',function()
        for _,id in ipairs(require('src.classes').order)do
            local s=G.new(17,nil,{class_id=id});local initial=G.snapshot(s)
            local ok,why,events=G.submit(s,{kind='basic_attack',dx=1,dy=0});assert(ok,why)
            local committed=P.encode(G.snapshot(s));local encoded=P.encode(events)
            local pb=PB.new();PB.add(pb,events,s.player_id)
            local untouched=P.encode(pb)
            for _=1,8 do J.attack_offset(pb,s.player_id,s.active_floor_id,true)end
            assert(P.encode(pb)==untouched)
            for _=1,100 do J.attack_offset(pb,s.player_id,s.active_floor_id,true);PB.update(pb,.007)end
            assert(not PB.busy(pb) and P.encode(events)==encoded and P.encode(G.snapshot(s))==committed)
            local replay=G.restore(initial);local _,_,same=G.submit(replay,{kind='basic_attack',dx=1,dy=0})
            equal(events,same);equal(G.snapshot(s),G.snapshot(replay))
        end
    end)
    test('real currency and food collection produce cosmetic arcs after successful pickup only',function()
        local s=G.new(17);local p=G.player(s);p.hp=10
        Rewards.spawn_currency(s,3,s.active_floor_id,3,2,function()end)
        Rewards.spawn_supply(s,'food',s.active_floor_id,3,2,function()end)
        local model=J.new();local before=G.snapshot(s)
        J.add(model,{},s.player_id,s.active_floor_id,true);assert(#model.items==0)
        equal(before,G.snapshot(s))
        local ok,why,events=G.submit(s,{kind='move',dx=1,dy=0});assert(ok,why)
        local saved=G.snapshot(s);assert(s.currency==3 and p.hp==14)
        J.add(model,events,s.player_id,s.active_floor_id,true)
        assert(#model.items==2 and model.items[1].binding=='core:drop/currency' and model.items[2].binding=='core:drop/food')
        local e=model.items[1];local x,y,size,alpha=J.reward_pose(e,100,200,450,578)
        assert(x==100 and y==200 and size==18 and alpha==1)
        J.update(model,.24,s.active_floor_id,true)
        x,y,size,alpha=J.reward_pose(e,100,200,450,578)
        assert(x==275 and y==359 and size>8 and alpha>0)
        J.update(model,.3,s.active_floor_id,true);assert(#model.items==0)
        equal(saved,G.snapshot(s))
    end)
    test('unaffordable chest is inert; a real claim creates one boon flight and a short chest hop',function()
        local s=G.new(17);local c=G.spawn_chest(s,'core:chest/standard',3,2)
        local model=J.new();local ok,_,events=G.submit(s,{kind='claim_chest',chest_id=c.id,offer_index=1})
        assert(not ok);J.add(model,events,s.player_id,s.active_floor_id,true);assert(#model.items==0)
        s.currency=10
        ok,_,events=G.submit(s,{kind='claim_chest',chest_id=c.id,offer_index=1});assert(ok)
        local saved=G.snapshot(s);J.add(model,events,s.player_id,s.active_floor_id,true)
        assert(#model.items==1 and model.items[1].binding==c.offers[1].boon_id..'/icon')
        J.update(model,.11,s.active_floor_id,true);assert(J.chest_offset(model,c.id,true)==-4)
        assert(J.chest_offset(model,c.id,false)==0 and J.chest_offset(model,c.id+1,true)==0)
        J.update(model,.12,s.active_floor_id,true);assert(J.chest_offset(model,c.id,true)==0)
        equal(saved,G.snapshot(s))
    end)
    test('reward effects are capped, handle-free and discarded on floor change or disable',function()
        local model=J.new();local events={}
        for i=1,1000 do events[i]={kind='CurrencyCollected',source_id=1,amount=1,floor_id='a',x=i,y=2}end
        J.add(model,events,1,'a',true);assert(#model.items==J.reward_limit)
        for _,e in ipairs(model.items)do assert(not e.texture and not e.quad)end
        J.update(model,0,'b',true);assert(#model.items==0)
        J.add(model,events,1,'b',true);assert(#model.items==0,'retired floor leaked into view')
        J.add(model,events,1,'a',false);assert(#model.items==0)
        J.add(model,events,1,'a',true);J.update(model,0,'a',false);assert(#model.items==0)
        J.update(model,.01,'a',true);assert(#model.items==0,'reenabling replayed old pickups')
        J.add(model,events,2,'a',true);assert(#model.items==0,'another actor made a player pickup arc')
    end)
    test('real direct hits and blocks activate once at their displayed beat without changing events or recovery',function()
        for _,defense in ipairs({0,100})do
            local s=G.new(17);local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',3,2,10000)
            e.hp,e.max_hp,e.defense=100,100,defense
            local initial=G.snapshot(s)
            local ok,why,events=G.submit(s,{kind='basic_attack',dx=1,dy=0});assert(ok,why)
            local committed=P.encode(G.snapshot(s));local encoded=P.encode(events)
            local pb=PB.new();PB.add(pb,events,s.player_id);local queue=P.encode(pb)
            local m=J.new();J.add(m,events,s.player_id,s.active_floor_id,true)
            assert(#m.impacts==0,'impact appeared ahead of playback')
            J.update(m,0,s.active_floor_id,true,pb,s.player_id)
            assert(#m.impacts==1 and m.impacts[1].kind==(defense==0 and 'hit' or 'blocked'))
            local hit=m.impacts[1];assert(hit.dx==1 and hit.dy==0 and hit.friendly)
            J.add(m,{},s.player_id,s.active_floor_id,true)
            J.update(m,.05,s.active_floor_id,true,pb,s.player_id);assert(#m.impacts==1)
            local x,y,size,alpha=J.impact_pose(hit,3,40);assert(x>0 and y==0 and size>0 and alpha>0)
            assert(P.encode(pb)==queue and P.encode(events)==encoded and P.encode(G.snapshot(s))==committed)
            local replay=G.restore(initial);local _,_,same=G.submit(replay,{kind='basic_attack',dx=1,dy=0})
            equal(events,same);equal(G.snapshot(s),G.snapshot(replay))
        end
    end)
    test('sparks exclude zero damage and DOT; blocked rings and accents honor floor and off without replay',function()
        local pb=PB.new();local m=J.new()
        PB.add(pb,{{kind='DamageTaken',floor_id='a',x=4,y=3,actual_loss=2,direct_attack=false}},1)
        J.update(m,0,'a',true,pb,1);assert(#m.impacts==0)
        pb=PB.new();PB.add(pb,{{kind='DamageBlocked',floor_id='a',x=4,y=3}},1)
        J.update(m,0,'a',true,pb,1);assert(#m.impacts==1)
        local radius,alpha=J.ring_pose(m.impacts[1],40);assert(radius==5 and alpha==1)
        J.update(m,.08,'a',true,pb,1)
        local expanded,fade=J.ring_pose(m.impacts[1],40);assert(expanded>radius and fade<alpha)
        J.update(m,0,'a',false,pb,1);J.update(m,0,'a',true,pb,1)
        assert(#m.impacts==0,'effects-on replayed an old hit')
        J.update(m,0,'b',true,pb,1);assert(#m.impacts==0,'retired-floor impact leaked')
    end)
    test('a real lethal hit retains enemy artwork and one level badge while exact save continuation survives',function()
        local s=G.new(17);s.progression.xp_total=4
        local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',3,2,10000);e.hp=1
        local ok,why,events=G.submit(s,{kind='basic_attack',dx=1,dy=0});assert(ok,why)
        assert(not G.entity(s,e.id) and s.progression.level==2)
        local saved=G.snapshot(s);local pb=PB.new();PB.add(pb,events,s.player_id)
        local m=J.new();J.add(m,events,s.player_id,s.active_floor_id,true)
        assert(#m.celebrations==1 and m.celebrations[1].kind=='level' and m.celebrations[1].level==2)
        J.update(m,0,s.active_floor_id,true,pb,s.player_id)
        assert(#m.crumbles==1 and m.crumbles[1].binding==e.definition_id)
        assert(not m.crumbles[1].texture and not m.crumbles[1].quad)
        local a,b=G.restore(saved),G.restore(saved)
        for _=1,60 do J.update(m,.01,s.active_floor_id,true,pb,s.player_id)end
        assert(#m.crumbles==0 and #m.impacts==0,'expired beat respawned effects')
        equal(saved,G.snapshot(s));equal(saved,G.snapshot(a))
        local _,_,ea=G.submit(a,{kind='wait'});local _,_,eb=G.submit(b,{kind='wait'})
        equal(ea,eb);equal(G.snapshot(a),G.snapshot(b))
    end)
    test('actual generated secret discovery celebrates only when its existing playback beat displays',function()
        local s=G.new_generated(1)
        for _,e in ipairs(s.entities)do if e.controller=='ai'then require('src.scheduler').remove_for_actor(s,e.id)end end
        -- Assisted placement isolates discovery; this is not a claimed legal full run.
        local secret=s.secrets[1];local p=G.player(s)
        p.position.x,p.position.y=secret.x,secret.y;require('src.visibility').recompute(s)
        local ok,why,events=G.submit(s,{kind='basic_attack',dx=secret.wall_x-secret.x,dy=secret.wall_y-secret.y})
        assert(ok,why);assert(secret.revealed)
        local saved=G.snapshot(s);local pb=PB.new();PB.add(pb,events,s.player_id)
        local m=J.new();J.add(m,events,s.player_id,s.active_floor_id,true);assert(#m.celebrations==0)
        while PB.current(pb)and PB.current(pb).kind~='SecretDiscovered'do PB.update(pb,pb.remaining)end
        assert(PB.current(pb));J.update(m,0,s.active_floor_id,true,pb,s.player_id)
        assert(#m.celebrations==1 and m.celebrations[1].kind=='secret')
        local radius,alpha,rise=J.celebration_pose(m.celebrations[1],40)
        assert(radius==10 and alpha==1 and rise==0)
        J.update(m,.81,s.active_floor_id,true,pb,s.player_id);assert(#m.celebrations==0)
        equal(saved,G.snapshot(s));equal(saved,G.snapshot(G.restore(saved)))
    end)
    test('nine crumble clips cover the original sprite and drift apart with deterministic bounded lifetime',function()
        for _,size in ipairs({16,40})do
            local e={age=0};local area=0
            for i=1,9 do
                local x,y,w,h,dx,dy,alpha=J.crumble_piece(e,i,size)
                assert(x>=0 and y>=0 and x+w<=size and y+h<=size and dx==0 and dy==0 and alpha==1)
                area=area+w*h
            end
            assert(area==size*size)
            e.age=.12
            local _,_,_,_,dx,dy,alpha=J.crumble_piece(e,1,size);assert(dx<0 and dy<0 and alpha==.25)
            e.age=.24;local _,_,_,_,_,_,a=J.crumble_piece(e,9,size);assert(a==0)
        end
    end)
    test('burst crowds and many levels have bounded handle-free feedback; disable and floor changes clear everything',function()
        local events={};local model=J.new()
        for i=1,1000 do
            events[#events+1]={kind='DamageTaken',target_id=i,floor_id='a',x=i,y=2,actual_loss=1,direct_attack=true}
            events[#events+1]={kind='Died',target_id=i,target_faction='enemy',target_definition_id='core:enemy/mossbound_guard',floor_id='a',x=i,y=2}
            events[#events+1]={kind='LevelUp',source_id=1,new_level=i,source_position={floor_id='a',x=1,y=2}}
        end
        local item={event=events[1],events=events,duration=.3}
        local pb={items={item},facings={},remaining=.3}
        J.add(model,events,1,'a',true);J.update(model,0,'a',true,pb,1)
        assert(#model.impacts==J.impact_limit and #model.crumbles==J.crumble_limit)
        assert(#model.celebrations==1 and model.celebrations[1].level==1000)
        for _,list in ipairs({model.impacts,model.crumbles,model.celebrations})do
            for _,e in ipairs(list)do assert(not e.texture and not e.quad)end
        end
        J.update(model,0,'a',false,pb,1)
        assert(#model.impacts==0 and #model.crumbles==0 and #model.celebrations==0)
        J.update(model,0,'b',true,pb,1);assert(#model.impacts==0 and #model.crumbles==0)
    end)
    test('menu selection settles quickly, presses offset without moving hitboxes, and views reset feedback',function()
        UI.begin_frame('juice-test',true)
        local dy,spread,pulse=UI.option_feedback('NEW RUN',280,274,464,true,false)
        assert(dy==-1 and spread==4 and pulse==1)
        assert(UI.home_at(500,280)==2)
        UI.press();dy=UI.option_feedback('NEW RUN',280,274,464,true,false);assert(dy==1)
        UI.update(.2,true);dy,spread,pulse=UI.option_feedback('NEW RUN',280,274,464,true,false)
        assert(dy==0 and spread==0 and pulse==0)
        UI.begin_frame('different-view',true)
        dy,spread=UI.option_feedback('NEW RUN',280,274,464,true,true)
        assert(dy==0 and spread==0)
        UI.update(0,false);dy,spread,pulse=UI.option_feedback('NEW RUN',280,274,464,true,false)
        assert(dy==0 and spread==0 and pulse==0 and UI.home_at(500,280)==2)
    end)
    test('chest offer arrivals are staggered, settle exactly, and honor effects-off',function()
        UI.begin_frame('chest-reveal-test',true)
        local dy,scale=UI.offer_pose(1);assert(dy==6 and scale==1)
        UI.update(.08,true)
        local first=UI.offer_pose(1);local third=UI.offer_pose(3);assert(third>first)
        dy,scale=UI.offer_pose(1);assert(scale>1 and scale<=1.12)
        UI.update(.3,true)
        for i=1,3 do dy,scale=UI.offer_pose(i);assert(dy==0 and math.abs(scale-1)<1e-10)end
        UI.update(0,false);dy,scale=UI.offer_pose(1);assert(dy==0 and scale==1)
    end)
end
