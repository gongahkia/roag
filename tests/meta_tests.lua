local Game=require('src.game')
local Scheduler=require('src.scheduler')
local Rewards=require('src.rewards')
local Content=require('src.content')
local BoonContent=require('src.boon_content')
local Achievements=require('src.achievements')
local AchievementContent=require('src.achievement_content')
local Profile=require('src.profile')
local Persistence=require('src.persistence')
local Play=require('tests.legal_playthrough')

return function(test,equal)
    local function storage()
        local s={}
        function s:read(name) return self[name] end
        function s:write(name,bytes)
            if self.fail then return false end
            self[name]=self.truncate and bytes:sub(1,math.floor(#bytes/2)) or bytes
            return true
        end
        return s
    end
    local function count(events,kind,id)
        local n=0
        for _,event in ipairs(events) do
            if event.kind==kind and (not id or event.boon_id==id) then n=n+1 end
        end
        return n
    end
    local function grant(s,name)
        Game.set_boon_stacks(s,s.player_id,'core:boon/'..name,'rare',1)
    end
    local function quiet(s,id,x,y)
        local e=Game.spawn_enemy(s,id or 'core:enemy/mossbound_guard',x,y)
        Scheduler.remove_for_actor(s,e.id)
        return e
    end
    test('four validated achievements reward four functioning locked boon IDs',function()
        assert(AchievementContent.validate() and #AchievementContent.order==4)
        assert(#BoonContent.order==24 and #Achievements.pool({})==20)
        local earned={}
        for _,id in ipairs(AchievementContent.order) do earned[id]=true end
        local pool=Achievements.pool(earned)
        assert(#pool==24 and Achievements.validate_pool(pool))
        for _,id in ipairs(AchievementContent.order) do
            local reward=AchievementContent.get(id).reward
            assert(BoonContent.get(reward))
        end
    end)
    test('eligible run freezes chest pool and fixture grants remove eligibility',function()
        local meta=Achievements.new_run(true,'local-run/1')
        local s=Game.new_stage(3,{meta=meta})
        assert(#s.available_boons==20 and s.meta.eligible)
        local before=Persistence.encode(Game.snapshot(s))
        local options=Rewards.make_offers(s,Content.get_chest('core:chest/standard'))
        for _,offer in ipairs(options) do
            for i=21,24 do assert(offer.boon_id~=BoonContent.order[i]) end
        end
        assert(before~=Persistence.encode(Game.snapshot(s))) -- fixture roll consumes only its RNG
        Game.set_boon_stacks(s,s.player_id,'core:boon/emberbrand','common',1)
        assert(not s.meta.eligible and not Game.restore(Game.snapshot(s)).meta.eligible)
    end)
    test('profile slots recover corruption and reject newer incompatibility',function()
        local disk=storage();local service=Profile.new(disk)
        local first=Profile.fresh()
        first.earned[AchievementContent.order[1]]=true
        assert(service:save(first))
        local second=Persistence.decode(Persistence.encode(first))
        second.earned[AchievementContent.order[2]]=true
        assert(service:save(second))
        assert(service:inspect().profile.earned[AchievementContent.order[2]])
        disk[Profile.slots[2]]=disk[Profile.slots[2]]:sub(1,20)
        local recovered=service:inspect()
        assert(recovered.status=='recovered' and recovered.profile.earned[AchievementContent.order[1]]
            and not recovered.profile.earned[AchievementContent.order[2]])
        disk[Profile.slots[2]]=nil
        assert(service:save(second))
        disk[Profile.slots[2]]=disk[Profile.slots[2]]:gsub('roeg%-profile%-record/1',
            'roeg-profile-record/9',1)
        assert(service:inspect().status=='incompatible')
        assert(not service:save(second))
    end)
    test('failed profile writes preserve previous generation and do not touch run slots',function()
        local disk=storage();local service=Profile.new(disk)
        local p=Profile.fresh();assert(service:save(p))
        local old=disk[Profile.slots[1]]
        disk.fail=true;assert(not service:save(p));disk.fail=false
        assert(disk[Profile.slots[1]]==old and service:inspect().status=='active')
        disk.truncate=true;assert(not service:save(p));disk.truncate=false
        assert(service:inspect().status=='recovered')
        assert(disk[Persistence.slots[1]]==nil and disk[Persistence.slots[2]]==nil)
    end)
    test('all four unlocked boons are offered through frozen future-run chest rolls',function()
        local earned={};for _,id in ipairs(AchievementContent.order) do earned[id]=true end
        local pool=Achievements.pool(earned)
        local found={}
        for seed=1,400 do
            local s=Game.new(seed,nil,{available_boons=pool})
            for _,offer in ipairs(Rewards.make_offers(s,
                Content.get_chest('core:chest/standard'))) do found[offer.boon_id]=true end
        end
        for i=21,24 do assert(found[BoonContent.order[i]],BoonContent.order[i]) end
        local s=Game.new_stage(3,{available_boons=pool})
        local old=Persistence.encode(Game.snapshot(s))
        local restored=Game.restore(Game.snapshot(s))
        assert(Persistence.encode(Game.snapshot(restored))==old)
    end)
    test('Guardian Vow block bursts, Prospector pickup heals, Crownfire first strike burns',function()
        local blocked=Game.new(36)
        local p=Game.player(blocked);p.position.x=8;p.position.y=5;p.defense=99
        grant(blocked,'guardians_vow')
        Game.spawn_enemy(blocked,'core:enemy/thornspitter',8,2)
        local foe=quiet(blocked,'core:enemy/mossbound_guard',7,5)
        local _,_,events=Game.submit(blocked,{kind='wait'})
        assert(count(events,'DamageBlocked')>=1 and
            count(events,'BoonActivated','core:boon/guardians_vow')==1 and foe.hp<foe.max_hp)

        local pickup=Game.new(51)
        local q=Game.player(pickup);q.hp=q.hp-4
        grant(pickup,'prospectors_heart')
        Rewards.spawn_currency(pickup,2,q.position.floor_id,q.position.x+1,q.position.y,
            function()end)
        local _,_,collected=Game.submit(pickup,{kind='move',dx=1,dy=0})
        assert(count(collected,'CurrencyCollected')==1 and
            count(collected,'BoonActivated','core:boon/prospectors_heart')==1 and q.hp>q.max_hp-4)

        local burn=Game.new(52)
        grant(burn,'crownfire')
        local target=quiet(burn,nil,3,2)
        local _,_,hit=Game.submit(burn,{kind='basic_attack',dx=1,dy=0})
        assert(count(hit,'BoonActivated','core:boon/crownfire')==1
            and target.statuses and target.statuses.burning)
    end)
    test('Circuit Chorus follows another boon activation without self-loop',function()
        local s=Game.new(53)
        grant(s,'circuit_chorus')
        Game.set_boon_stacks(s,s.player_id,'core:boon/storm_conductor','legendary',2)
        quiet(s,nil,3,2);quiet(s,nil,4,2)
        local _,_,events=Game.submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(count(events,'BoonActivated','core:boon/storm_conductor')>=1)
        assert(count(events,'BoonActivated','core:boon/circuit_chorus')>=1)
        assert(#events<100)
    end)
    test('normal legal elite defeat awards once and future run can roll reward',function()
        local options={meta=Achievements.new_run(true,'local-run/100')}
        local run=Play.stage_one(3,1,options)
        local id=AchievementContent.order[1]
        assert(run.state.meta.earned[id] and run.state.meta.pending[id])
        local award_events=0
        for _,e in ipairs(run.events) do if e.kind=='AchievementEarned' then award_events=award_events+1 end end
        assert(award_events==1)
        local disk=storage();local profiles=Profile.new(disk)
        assert(profiles:commit_awards(run.state.meta.pending))
        local next_pool=Achievements.pool(profiles:inspect().profile.earned)
        assert(#next_pool==21 and next_pool[21]=='core:boon/guardians_vow')
        local found=false
        for seed=1,200 do
            local next_run=Game.new_stage(seed,{available_boons=next_pool})
            for _,chest in ipairs(next_run.entities) do
                if chest.controller=='chest' then
                    for _,offer in ipairs(chest.offers) do
                        if offer.boon_id==next_pool[21] then found=true end
                    end
                end
            end
            if found then break end
        end
        assert(found,'unlocked boon never appeared in generated chest')
    end)
    test('ordinary movement and empty-tile strike reveal secret achievement',function()
        local run=Play.new(1,{meta=Achievements.new_run(true,'local-run/secret')})
        local secret=run.state.secrets[1]
        assert(not run.state.meta.earned[AchievementContent.order[2]])
        local ok,reason=Play.navigate(run,function(p)
            return p.x==secret.x and p.y==secret.y end,350)
        assert(ok,reason)
        local _,_,events=Play.action(run,{kind='basic_attack',
            dx=secret.wall_x-secret.x,dy=secret.wall_y-secret.y})
        assert(count(events,'SecretDiscovered')==1
            and run.state.meta.earned[AchievementContent.order[2]])
        local awards=0
        for _,e in ipairs(events) do if e.kind=='AchievementEarned' then awards=awards+1 end end
        assert(awards==1)
        local _,_,again=Play.action(run,{kind='basic_attack',
            dx=secret.wall_x-secret.x,dy=secret.wall_y-secret.y})
        assert(count(again,'SecretDiscovered')==0 and count(again,'AchievementEarned')==0)
    end)
    test('chain rule requires three distinct same-chain boon activations',function()
        local s=Game.new(75,nil,{meta=Achievements.new_run(true,'local-run/chain')})
        local id=AchievementContent.order[3]
        local awards={}
        local emit=function(kind,fields)
            if kind=='AchievementEarned' then awards[#awards+1]=fields end
        end
        local function activation(boon,chain)
            Achievements.observe(s,{kind='BoonActivated',boon_id=boon,chain_id=chain},emit)
        end
        activation(BoonContent.order[1],7)
        activation(BoonContent.order[1],7)
        activation(BoonContent.order[2],7)
        assert(not s.meta.earned[id])
        activation(BoonContent.order[3],8)
        assert(not s.meta.earned[id])
        activation(BoonContent.order[3],7) -- chain 7 resumes after chain 8
        assert(s.meta.earned[id] and #awards==1)
        activation(BoonContent.order[4],8)
        assert(#awards==1)
    end)
    test('normal legal seed three Victory awards Regent Fall and excludes secret near miss',function()
        local run=Play.complete_run(3,{meta=Achievements.new_run(true,'local-run/victory')})
        assert(run.state.victory and #run.trace==991)
        assert(run.state.meta.earned[AchievementContent.order[1]])
        assert(run.state.meta.earned[AchievementContent.order[4]])
        assert(not run.state.meta.earned[AchievementContent.order[2]])
        local wins,awards=0,0
        for _,e in ipairs(run.events) do
            if e.kind=='Victory' then wins=wins+1 end
            if e.kind=='AchievementEarned' and
                e.achievement_id==AchievementContent.order[4] then awards=awards+1 end
        end
        assert(wins==1 and awards==1)
    end)
    test('profile run IDs advance even when a seed repeats',function()
        local disk=storage();local service=Profile.new(disk)
        local first=assert(service:allocate_run())
        local second=assert(service:allocate_run())
        assert(first~=second)
        local a=Game.new_stage(3,{meta=Achievements.new_run(true,first)})
        local b=Game.new_stage(3,{meta=Achievements.new_run(true,second)})
        assert(a.run_seed==b.run_seed and a.meta.run_id~=b.meta.run_id)
    end)
    test('pending terminal award outbox survives restart and idempotent commit',function()
        local disk=storage();local saves=Persistence.new(disk);local profiles=Profile.new(disk)
        local s=Game.new(66,nil,{meta=Achievements.new_run(true,'local-run/200')})
        local id=AchievementContent.order[1]
        s.meta.earned[id]=true;s.meta.pending[id]=true
        s.game_over=true;Game.player(s).hp=0
        assert(saves:pending_terminal(s))
        local found=Persistence.new(disk):inspect()
        assert(found.status=='pending_terminal' and not Persistence.new(disk):load())
        assert(profiles:commit_awards(found.state.meta.pending))
        assert(profiles:commit_awards(found.state.meta.pending))
        assert(saves:tombstone())
        assert(saves:inspect().status=='terminal')
        assert(profiles:inspect().profile.earned[id])
    end)
end
