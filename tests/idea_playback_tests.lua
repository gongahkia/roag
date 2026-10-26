return function(test,equal)
    local PB=require('src.playback');local G=require('src.game');local B=require('src.boons')
    local P=require('src.persistence');local V=require('src.visibility')
    local function event(kind,id,time,chain,floor)
        return {kind=kind,id=id,time=time or 0,chain_id=chain or 1,
            floor_id=floor or 'floor',source_id=1,target_id=2,x=3,y=2,
            actual_loss=1,boon_id='core:boon/emberbrand',target_cells={{x=3,y=2}}}
    end
    test('mixed causal damage, boon and status cues share one bounded ordered beat',function()
        local events={};local kinds={'DamageTaken','BoonActivated','EffectApplied','StatusApplied','StatusTick'}
        for i=1,100 do events[i]=event(kinds[(i-1)%#kinds+1],i)end
        -- Real EffectApplied events carry floor through source_position.
        events[3].floor_id=nil;events[3].source_position={floor_id='floor',x=2,y=2}
        local before=P.encode(events);local pb=PB.new();PB.add(pb,events,1)
        assert(#pb.items==1 and pb.items[1].duration==PB.max_cascade_seconds)
        for i,e in ipairs(pb.items[1].events)do assert(e==events[i])end
        assert(#pb.items[1].events==100 and P.encode(events)==before)
    end)
    test('causal grouping never crosses a simulation timestamp, chain, floor or separate submission',function()
        local events={event('DamageTaken',1,0,1),event('BoonActivated',2,100,1),
            event('StatusTick',3,100,2),event('DamageTaken',4,100,2,'other')}
        local pb=PB.new();PB.add(pb,events,1);assert(#pb.items==4)
        PB.add(pb,{event('DamageTaken',5,100,2,'other')},1);assert(#pb.items==5)
    end)
    test('mixed feedback cannot absorb a locked windup, enemy impact or actor motion',function()
        local events={event('BoonActivated',1),event('DamageTaken',2),event('WindupStarted',3),
            event('AttackPerformed',4),event('DamageTaken',5),event('BoonActivated',6)}
        events[4].source_id=2
        events[7]={kind='Moved',source_id=2,floor_id='floor',from_x=3,from_y=2,x=4,y=2}
        local pb=PB.new();PB.add(pb,events,1)
        assert(#pb.items==5 and pb.items[2].event==events[3]and pb.items[3].event==events[4])
        assert(pb.items[5].motions[2]and pb.items[5].duration==PB.tile_seconds)
    end)
    test('same-chain simultaneous deaths remain ordered and summed numbers retain every victim',function()
        local events={event('DamageTaken',1),event('Died',2),event('DamageTaken',3),event('Died',4)}
        events[3].target_id=3;events[4].target_id=3;events[3].x=4;events[4].x=4
        events[1].actual_loss=3;events[3].actual_loss=5;events[3].critical=true
        local pb=PB.new();PB.add(pb,events,1);assert(#pb.items==1 and #pb.items[1].events==4)
        local N=require('src.combat_numbers');local n=N.new();N.update(n,pb,0,1,'floor')
        assert(#n.items==2 and n.items[1].amount==3 and n.items[2].amount==5 and n.items[2].critical)
    end)
    test('a real dense new-boon chain keeps all visible events with under two seconds of playback',function()
        local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/berserker'})
        local p=G.player(s);p.position.x,p.position.y=15,16;p.hp=10
        for _,slug in ipairs({'backswing','threefold_remedy','fourfold_ledger','long_wick',
            'passing_ember','rooks_wake','fan_of_blades','emberbrand','temporal_echo',
            'cluster_rupture','grave_dividend','storm_conductor'})do
            B.set_stacks(p,'core:boon/'..slug,'rare',2)
        end
        for y=-2,2 do for x=-2,2 do if x~=0 or y~=0 then
            local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',15+x,16+y,100000)
            e.hp=1000;e.max_hp=1000
        end end end
        V.recompute(s)
        local ok,why,events=G.submit(s,{kind='sweeping_slash',dx=1,dy=0});assert(ok,why)
        assert(#events>500);local before=G.snapshot(s)
        local pb=PB.new();PB.add(pb,events,s.player_id)
        local total,shown,prev=0,{},0
        for _,item in ipairs(pb.items)do
            total=total+item.duration
            for _,e in ipairs(item.events or {item.event})do
                assert(e.id>prev,'displayed events reordered');prev=e.id;shown[e.id]=true
            end
        end
        assert(total<2 and #pb.items<=5,'mixed causal chain created a presentation backlog')
        for _,e in ipairs(events)do
            if ({DamageTaken=true,DamageBlocked=true,BoonActivated=true,EffectApplied=true,
                StatusApplied=true,StatusTick=true,DelayedEffectScheduled=true,
                DelayedEffectResolved=true,Died=true})[e.kind]then assert(shown[e.id],e.kind..' lost')end
        end
        PB.update(pb,total+.01);assert(not PB.busy(pb));equal(before,G.snapshot(s))
        local b=G.restore(before)
        local _,_,a=G.submit(s,{kind='wait'});local _,_,c=G.submit(b,{kind='wait'})
        equal(a,c);equal(G.snapshot(s),G.snapshot(b))
    end)
end
