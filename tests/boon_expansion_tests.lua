local Catalog=require('src.boon_content')
local Game=require('src.game')
local Scheduler=require('src.scheduler')
local Rewards=require('src.rewards')
local Content=require('src.content')
local Boons=require('src.boons')

return function(test,equal)
    local function grant(s,name,rarity,count)
        Game.set_boon_stacks(s,s.player_id,'core:boon/'..name,rarity or 'rare',count or 1)
    end
    local function quiet(s,id,x,y)
        local e=Game.spawn_enemy(s,id or 'core:enemy/mossbound_guard',x,y)
        Scheduler.remove_for_actor(s,e.id)
        return e
    end
    local function events_of(events,kind)
        local result={}
        for _,event in ipairs(events) do if event.kind==kind then result[#result+1]=event end end
        return result
    end
    local function activations(events,name)
        local result={}
        for _,event in ipairs(events_of(events,'BoonActivated')) do
            if event.boon_id=='core:boon/'..name then result[#result+1]=event end
        end
        return result
    end

    test('original twenty boons remain validated and available before unlocks',function()
        assert(#Catalog.order==53 and Catalog.starter_count==20 and Catalog.validate())
        local seen={}
        for _,id in ipairs(Catalog.order) do
            assert(not seen[id]);seen[id]=true
            local def=assert(Catalog.get(id))
            local last=-1
            for _,rarity in ipairs(Catalog.rarities) do
                assert(def.parameters[rarity].power>last)
                last=def.parameters[rarity].power
                assert(#Catalog.offer_text(id,rarity)>#def.description)
            end
        end
        local found={}
        for seed=1,600 do
            local s=Game.new(seed)
            local offers=Rewards.make_offers(s,Content.get_chest('core:chest/standard'))
            for _,offer in ipairs(offers) do found[offer.boon_id]=true end
        end
        for i,id in ipairs(Catalog.order) do
            if i<=Catalog.starter_count then assert(found[id],id..' never offered')
            else assert(not found[id],id..' leaked before unlock') end
        end
    end)

    test('mixed rarity new stacks aggregate without cap and serialize',function()
        local s=Game.new(31)
        grant(s,'emberbrand','common',100)
        grant(s,'emberbrand','legendary',20)
        assert(Boons.aggregate('core:boon/emberbrand',Game.player(s).boons['core:boon/emberbrand'],'power')==320)
        equal(Game.snapshot(s),Game.snapshot(Game.restore(Game.snapshot(s))))
    end)

    test('Burning ticks on scheduler once per target and keeps player kill credit',function()
        local s=Game.new(32)
        grant(s,'emberbrand','rare')
        local e=quiet(s,'core:enemy/mossbound_guard',3,2)
        local _,_,first=Game.submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(#events_of(first,'StatusApplied')==1 and #events_of(first,'StatusTick')==1)
        assert(e.statuses.burning.remaining==2 and e.statuses.burning.next_due==200)
        local saved=Game.restore(Game.snapshot(s))
        local _,_,a=Game.submit(s,{kind='wait'})
        local _,_,b=Game.submit(saved,{kind='wait'})
        equal(a,b);equal(Game.snapshot(s),Game.snapshot(saved))
        assert(#events_of(a,'StatusTick')==1 and #events_of(a,'EntityKilled')==0)
        local _,_,c=Game.submit(s,{kind='wait'})
        assert(#events_of(c,'StatusTick')==1 and #events_of(c,'EntityKilled')==1)
        assert(events_of(c,'EntityKilled')[1].source_id==s.player_id)
        assert(s.progression.xp_total>0 and #s.drops==1)
        assert(Game.entity(s,e.id)==nil)
    end)

    test('Burning refresh keeps one scheduled tick and stronger mixed rarity power',function()
        local s=Game.new(320)
        grant(s,'emberbrand','common')
        local e=quiet(s,'core:enemy/mossbound_guard',3,2)
        e.hp=40;e.max_hp=40
        assert(Game.submit(s,{kind='basic_attack',dx=1,dy=0}))
        local status=e.statuses.burning
        assert(status.power==2 and status.remaining==2)
        local scheduled=status.schedule_id
        grant(s,'emberbrand','rare')
        assert(Game.submit(s,{kind='basic_attack',dx=1,dy=0}))
        status=e.statuses.burning
        assert(status.power==6 and status.remaining==2)
        assert(status.schedule_id~=scheduled and status.next_due==300)
        local ticks=0
        for _,item in ipairs(s.schedule) do
            if item.kind=='effect' and item.actor_id==e.id then ticks=ticks+1 end
        end
        assert(ticks==1)
    end)

    test('Emberbrand Burning enables Flashpoint and a credited Grave Dividend kill',function()
        local s=Game.new(33)
        local p=Game.player(s);p.hp=18
        grant(s,'longblade_geometry','common')
        grant(s,'emberbrand','rare')
        grant(s,'flashpoint','legendary')
        grant(s,'grave_dividend','rare')
        local a=quiet(s,'core:enemy/mossbound_guard',3,2)
        local b=quiet(s,'core:enemy/mossbound_guard',4,2)
        local _,_,events=Game.submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(#events_of(events,'StatusApplied')==2)
        assert(#activations(events,'flashpoint')>=1)
        assert(#events_of(events,'EntityKilled')>=1)
        assert(#activations(events,'grave_dividend')>=1)
        assert(p.hp>18)
        local flash_kill=false
        for _,event in ipairs(events_of(events,'EntityKilled')) do
            assert(event.source_id==p.id)
            for _,key in ipairs(event.ancestry) do
                if key==p.id..':core:boon/flashpoint' then flash_kill=true end
            end
        end
        assert(flash_kill,'Flashpoint did not cause a credited secondary kill')
    end)

    test('Opening Cut uses pre-batch full HP and Crowdbreaker counts other living hostiles',function()
        local s=Game.new(34)
        grant(s,'opening_cut','rare')
        grant(s,'crowdbreaker','rare')
        local a=quiet(s,'core:enemy/mossbound_guard',3,2)
        quiet(s,'core:enemy/mossbound_guard',4,2)
        local _,_,events=Game.submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(#activations(events,'opening_cut')==1)
        assert(#activations(events,'crowdbreaker')==1)
        assert(events_of(events,'DamageTaken')[1].hp_before==10)
        assert(events_of(events,'DamageTaken')[1].adjacent_hostile_count==1)
        assert(a.hp<7)
        local _,_,second=Game.submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(#activations(second,'opening_cut')==0)
        local sweep=Game.new(340)
        Game.player(sweep).position.x=3;Game.player(sweep).position.y=5
        grant(sweep,'opening_cut','rare')
        local left=quiet(sweep,'core:enemy/mossbound_guard',4,4)
        local right=quiet(sweep,'core:enemy/mossbound_guard',4,6)
        local _,_,batch=Game.submit(sweep,{kind='sweeping_slash',dx=1,dy=0})
        assert(#activations(batch,'opening_cut')==2)
        local primary=events_of(batch,'DamageBatchResolved')[1]
        assert(#primary.outcomes==2 and primary.outcomes[1].hp_before==10
            and primary.outcomes[2].hp_before==10)
        assert(left.hp<8 and right.hp<8)
    end)

    test('Final Spark threshold and Perfect Guard block are distinct from positive damage',function()
        local s=Game.new(35)
        grant(s,'final_spark','rare')
        local p=Game.player(s)
        local a=quiet(s,'core:enemy/mossbound_guard',3,2)
        local _,_,healthy=Game.submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(#activations(healthy,'final_spark')==0)
        p.hp=9;a.hp=10
        local _,_,low=Game.submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(#activations(low,'final_spark')==1)
        local blocked=Game.new(36)
        local q=Game.player(blocked);q.position.x=8;q.position.y=5;q.defense=99
        grant(blocked,'perfect_guard','rare')
        local spitter=Game.spawn_enemy(blocked,'core:enemy/thornspitter',8,2)
        local _,_,response=Game.submit(blocked,{kind='wait'})
        assert(#events_of(response,'DamageBlocked')>=1)
        assert(#activations(response,'perfect_guard')==1)
        assert(spitter.hp<spitter.max_hp and q.hp==q.max_hp)
    end)

    test('Harrier reacts to enemy windup once without interrupting ordinary damage',function()
        local s=Game.new(37)
        local p=Game.player(s);p.position.x=8;p.position.y=5
        grant(s,'harriers_response','legendary',2)
        local spitter=Game.spawn_enemy(s,'core:enemy/thornspitter',8,2)
        spitter.hp=100;spitter.max_hp=100
        local _,_,events=Game.submit(s,{kind='wait'})
        assert(#events_of(events,'WindupStarted')==1)
        assert(#activations(events,'harriers_response')>=1)
        assert(#events_of(events,'WindupCancelled')==0)
        assert(#events_of(events,'AttackPerformed')>=1)
    end)

    test('Pulse Step batches adjacent foes and ignores blocked movement',function()
        local s=Game.new(38)
        grant(s,'pulse_step','rare')
        local a=quiet(s,'core:enemy/ruin_skitter',4,2)
        local b=quiet(s,'core:enemy/ruin_skitter',2,3)
        local _,_,events=Game.submit(s,{kind='move',dx=1,dy=0})
        assert(#activations(events,'pulse_step')==1)
        assert(#events_of(events,'DamageBatchResolved')==1)
        assert(a.hp==1 and b.hp==1)
        local clock=s.clock
        local ok=Game.submit(s,{kind='move',dx=0,dy=-1})
        assert(not ok and s.clock==clock)
    end)

    test('Packed Powder uses disjoint triple deaths in the same chain',function()
        local s=Game.new(39)
        grant(s,'packed_powder','rare')
        Game.player(s).position.x=3;Game.player(s).position.y=5
        for _,pos in ipairs({{4,4},{4,5},{4,6}}) do
            local e=quiet(s,'core:enemy/ruin_skitter',pos[1],pos[2]);e.hp=1
        end
        local _,_,events=Game.submit(s,{kind='sweeping_slash',dx=1,dy=0})
        assert(#events_of(events,'EntityKilled')==3)
        assert(#activations(events,'packed_powder')==1)
        assert(#events_of(events,'EffectApplied')==1)
        local lone=Game.new(40);grant(lone,'packed_powder','rare')
        Game.player(lone).position.x=3;Game.player(lone).position.y=5
        for _,pos in ipairs({{4,4},{4,5}}) do
            local e=quiet(lone,'core:enemy/ruin_skitter',pos[1],pos[2]);e.hp=1
        end
        local _,_,two=Game.submit(lone,{kind='sweeping_slash',dx=1,dy=0})
        assert(#activations(two,'packed_powder')==0)
        local separate=Game.new(401)
        Game.player(separate).position.x=3;Game.player(separate).position.y=5
        grant(separate,'packed_powder','rare')
        for _,pos in ipairs({{4,4},{4,5}}) do
            local e=quiet(separate,'core:enemy/ruin_skitter',pos[1],pos[2]);e.hp=1
        end
        local _,_,first_chain=Game.submit(separate,{kind='sweeping_slash',dx=1,dy=0})
        assert(#activations(first_chain,'packed_powder')==0)
        local third=quiet(separate,'core:enemy/ruin_skitter',4,6);third.hp=1
        local _,_,next_chain=Game.submit(separate,{kind='basic_attack',dx=1,dy=1})
        assert(#activations(next_chain,'packed_powder')==0)
        for _,extras in ipairs({1,3}) do
            local chain=Game.new(400+extras)
            Game.player(chain).position.x=3;Game.player(chain).position.y=5
            grant(chain,'packed_powder','rare')
            grant(chain,'detonation_bloom','rare')
            for _,pos in ipairs({{4,4},{4,5},{4,6}}) do
                local e=quiet(chain,'core:enemy/ruin_skitter',pos[1],pos[2]);e.hp=1
            end
            for n=1,extras do
                local pos=({{5,4},{5,5},{3,6}})[n]
                local e=quiet(chain,'core:enemy/ruin_skitter',pos[1],pos[2]);e.hp=1
            end
            local _,_,chain_events=Game.submit(chain,{kind='sweeping_slash',dx=1,dy=0})
            assert(#events_of(chain_events,'EntityKilled')==3+extras)
            assert(#activations(chain_events,'packed_powder')==math.floor((3+extras)/3))
        end
    end)

    test('Longblade strikes second cell without changing bump or base ability',function()
        local s=Game.new(41)
        local before=Game.effective_ability(s,'basic_attack',1,0)
        assert(#before==1)
        grant(s,'longblade_geometry','rare')
        local cells,damage=Game.effective_ability(s,'basic_attack',1,0)
        assert(#cells==2 and cells[2].x==4 and damage>Game.player(s).damage)
        local far=quiet(s,'core:enemy/mossbound_guard',4,2)
        local _,_,hit=Game.submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(#events_of(hit,'HitConfirmed')==1 and far.hp<far.max_hp)
        local w=Game.new(42)
        grant(w,'longblade_geometry','rare')
        Game.player(w).position.y=3
        local blocked=Game.effective_ability(w,'basic_attack',1,0)
        assert(#blocked==1)
        local bump=Game.new(43);grant(bump,'longblade_geometry','rare')
        quiet(bump,'core:enemy/mossbound_guard',3,2)
        local _,_,b=Game.submit(bump,{kind='move',dx=1,dy=0})
        assert(events_of(b,'AttackPerformed')[1].bump==true)
        assert(#events_of(b,'HitConfirmed')==1)
        assert(#Game.effective_ability(Game.new(41),'basic_attack',1,0)==1)
        local diagonal=Game.new(410)
        Game.player(diagonal).position.x=2;Game.player(diagonal).position.y=4
        grant(diagonal,'longblade_geometry','common')
        grant(diagonal,'crescent_reach','common')
        local line=Game.effective_ability(diagonal,'basic_attack',1,-1)
        local slash=Game.effective_ability(diagonal,'sweeping_slash',1,0)
        assert(#line==2 and line[2].x==4 and line[2].y==2)
        assert(#slash==4)
    end)

    test('Phantom Lunge hits distinct neighbors of actual full and partial Dash path',function()
        local s=Game.new(44);grant(s,'phantom_lunge','rare')
        local a=quiet(s,'core:enemy/mossbound_guard',3,3)
        local b=quiet(s,'core:enemy/mossbound_guard',5,2)
        local _,_,events=Game.submit(s,{kind='dash',dx=1,dy=0})
        assert(Game.player(s).position.x==4 and #activations(events,'phantom_lunge')==1)
        assert(#events_of(events,'DamageTaken')==2 and a.hp<10 and b.hp<10)
        local p=Game.new(45);grant(p,'phantom_lunge','rare')
        quiet(p,'core:enemy/mossbound_guard',4,2)
        local near=quiet(p,'core:enemy/mossbound_guard',3,3)
        local far=quiet(p,'core:enemy/mossbound_guard',5,2)
        local _,_,partial=Game.submit(p,{kind='dash',dx=1,dy=0})
        assert(Game.player(p).position.x==3 and #events_of(partial,'DamageTaken')==2)
        assert(near.hp<10 and far.hp==10)
        local rejected=Game.new(46);grant(rejected,'phantom_lunge','rare')
        quiet(rejected,'core:enemy/mossbound_guard',3,2)
        local clock=rejected.clock
        local ok=Game.submit(rejected,{kind='dash',dx=1,dy=0})
        assert(not ok and rejected.clock==clock)
    end)
end
