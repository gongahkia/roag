-- Assisted transition fixtures, not legal-playthrough claims. All tested
-- handoffs, pickups and status ticks still use real Game.submit.
return function(test,equal)
    local G=require('src.game')
    local B=require('src.boons')
    local P=require('src.persistence')
    local F=require('tests.idea_boon_handoff_fixture')
    local submit,ready,route=F.submit,F.ready,F.route
    local function grant(s,p,slug)
        G.set_boon_stacks(s,p.id,'core:boon/'..slug,'rare',1)
    end
    test('stage handoff preserves remaining boon duration and never revives expired speed or prime charges',function()
        local s,p,exit=ready();local old_clock=s.clock
        for _,slug in ipairs({'road_flask','bloodrush','ration_reserve','betrayal_ward','fateful_dice'})do grant(s,p,slug)end
        p.boon_memory={
            ['core:boon/road_flask']={speed=20,expires_at=old_clock+250},
            ['core:boon/bloodrush']={speed=20,expires_at=old_clock-1},
            ['core:boon/ration_reserve']={charges=1,expires_at=old_clock-1},
            ['core:boon/betrayal_ward']={ward=3,expires_at=old_clock+150},
            ['core:boon/fateful_dice']={face=4,roll_action_id=1,ward=3,expires_at=old_clock+100},
        }
        local before=G.snapshot(s);local rng=s.rng.state
        assert(not G.submit(s,{kind='choose_route',exit_id=exit,route_index=99}))
        equal(before,G.snapshot(s))
        route(s,exit);p=G.player(s)
        assert(s.clock==0 and s.rng.state==rng)
        assert(p.boon_memory['core:boon/road_flask'].expires_at==250)
        assert(p.boon_memory['core:boon/betrayal_ward'].expires_at==150)
        assert(p.boon_memory['core:boon/fateful_dice'].expires_at==100)
        assert(p.boon_memory['core:boon/fateful_dice'].face==4)
        assert(p.boon_memory['core:boon/bloodrush'].speed==0)
        assert(p.boon_memory['core:boon/ration_reserve'].charges==0 and B.speed_bonus(s,p)==20)
        equal(before.entities[1].boon_memory,s.retired_stages[1].entities[1].boon_memory)
        equal(G.snapshot(s),G.snapshot(G.restore(G.snapshot(s))))
    end)
    test('an expiry exactly at the old clock survives the zero-time handoff and expires after the next action',function()
        local s,p,exit=ready();grant(s,p,'road_flask')
        p.boon_memory={['core:boon/road_flask']={speed=20,expires_at=s.clock}}
        route(s,exit);p=G.player(s)
        assert(B.speed_bonus(s,p)==20 and p.boon_memory['core:boon/road_flask'].expires_at==0)
        submit(s,{kind='wait'});assert(B.speed_bonus(s,p)==0)
    end)
    test('actual player Burning gets a new matching status schedule across stages and resumes exactly',function()
        local s,p,exit=F.burning()
        local old_id=p.id;local burn=P.decode(P.encode(p.statuses.burning))
        local remaining=burn.next_due-s.clock;local hp=p.hp;local rng=s.rng.state
        assert(remaining>0 and remaining<=100)
        route(s,exit);p=G.player(s)
        assert(p.id~=old_id and p.hp==hp and s.rng.state==rng)
        assert(p.statuses.burning.next_due==remaining)
        local found=false
        for _,work in ipairs(s.schedule)do
            if work.id==p.statuses.burning.schedule_id then
                assert(work.kind=='effect' and work.actor_id==p.id and work.due==remaining)
                assert(work.floor_id==p.position.floor_id);found=true
            end
        end
        assert(found,'traveling Burning lost its work')
        local restored=G.restore(P.decode(P.encode(G.snapshot(s))))
        local a=submit(s,{kind='wait'});local b=submit(restored,{kind='wait'})
        equal(a,b);equal(G.snapshot(s),G.snapshot(restored))
        local ticks=0
        for _,e in ipairs(a)do if e.kind=='StatusTick' and e.target_id==p.id then ticks=ticks+1 end end
        assert(ticks==1 and p.hp==hp-math.max(0,burn.power-p.defense))
    end)
    test('refreshing expired speed after a rarity trade uses current power rather than the expired larger value',function()
        local s=G.new(37);local p=G.player(s);p.hp=10
        G.set_boon_stacks(s,p.id,'core:boon/road_flask','legendary',1)
        local R=require('src.rewards')
        R.spawn_supply(s,'food',s.active_floor_id,3,2,function()end,p.id)
        submit(s,{kind='move',dx=1,dy=0})
        assert(p.boon_memory['core:boon/road_flask'].speed==25)
        while s.clock<=300 do submit(s,{kind='wait'})end
        G.set_boon_stacks(s,p.id,'core:boon/road_flask','legendary',0)
        G.set_boon_stacks(s,p.id,'core:boon/road_flask','common',1)
        p.hp=10;R.spawn_supply(s,'food',s.active_floor_id,2,2,function()end,p.id)
        submit(s,{kind='move',dx=-1,dy=0})
        assert(p.boon_memory['core:boon/road_flask'].speed==10)
    end)
end
