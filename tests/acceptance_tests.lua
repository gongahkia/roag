local Game=require('src.game')
local Play=require('tests.legal_playthrough')
local Persistence=require('src.persistence')

return function(test,equal)
    test('legal generated run buys a boon, defeats Warden and fights in stage two',function()
        local run=Play.stage_one(3)
        local checkpoints={}
        for _,point in ipairs(run.checkpoints) do checkpoints[point.name]=point end
        assert(checkpoints.earned_currency.currency>=5)
        assert(checkpoints.paid_chest_claimed.stage==1)
        assert(checkpoints.elite_defeated.hp>0)
        assert(checkpoints.free_chest_claimed.stage==1)
        assert(checkpoints.stage_two_entered.stage==2 and
            checkpoints.stage_two_entered.clock==0)
        assert(checkpoints.stage_two_enemy_defeated.stage==2 and
            checkpoints.stage_two_enemy_defeated.clock>0)
        assert(Play.has_event(run,'EntityKilled'))
        assert(Play.has_event(run,'ExperienceGained'))
        assert(Play.has_event(run,'BoonGranted'))
        assert(Play.has_event(run,'BoonActivated'))
        assert(Play.has_event(run,'ElitePhaseChanged'))
        assert(Play.has_event(run,'StageEntered'))
        local replay=Play.replay(run)
        equal(Game.snapshot(run.state),Game.snapshot(replay.state))
        equal(run.events,replay.events)
    end)

    test('legal secret and door interactions on generated floors',function()
        local sigil=Play.new(9)
        local mark=sigil.state.secrets[2]
        local ok,reason=Play.navigate(sigil,function(p)
            return p.x==mark.x and p.y==mark.y
        end,220)
        assert(ok,reason)
        assert(mark.revealed and Play.has_event(sigil,'SecretDiscovered'))
        assert(sigil.state.currency>=4)

        local wall=Play.new(65)
        local clue=wall.state.secrets[1]
        ok,reason=Play.navigate(wall,function(p)
            return p.x==clue.x and p.y==clue.y
        end,180)
        assert(ok,reason)
        ok,reason=Play.action(wall,{kind='basic_attack',
            dx=clue.wall_x-clue.x,dy=clue.wall_y-clue.y})
        assert(ok,reason)
        assert(clue.revealed and Game.entity(wall.state,clue.reward_id))
        assert(Play.has_event(wall,'SecretDiscovered'))

        local door_run=Play.new(13)
        local door=assert(Play.nearest(door_run.state,function(entity)
            return entity.controller=='door' and not entity.open
        end))
        ok,reason=Play.navigate(door_run,function(p)
            return Play.cardinal(p,door.position)
        end,100)
        assert(ok,reason)
        local p=Game.player(door_run.state).position
        local dx,dy=door.position.x-p.x,door.position.y-p.y
        local before=door_run.state.clock
        ok,reason=Play.action(door_run,{kind='move',dx=dx,dy=dy})
        assert(ok,reason)
        assert(door.open and door_run.state.clock>before)
        assert(p.x~=door.position.x or p.y~=door.position.y)
        assert(Play.has_event(door_run,'DoorOpened'))
        ok,reason=Play.action(door_run,{kind='move',dx=dx,dy=dy})
        assert(ok,reason)
        assert(p.x==door.position.x and p.y==door.position.y)
    end)

    test('legal death rejects later actions and a fresh seed can restart',function()
        local run=Play.new(1)
        for _=1,120 do
            if run.state.game_over then break end
            assert(Play.action(run,{kind='wait'}))
        end
        assert(run.state.game_over and Game.player(run.state).hp==0)
        local before=Game.snapshot(run.state)
        local ok,reason,events=Play.action(run,{kind='wait'})
        assert(not ok and reason=='game over' and #events==0)
        equal(before,Game.snapshot(run.state))
        local restarted=Play.new(1)
        assert(not restarted.state.game_over and restarted.state.clock==0)
    end)

    test('legal action trace survives full inert serialization and continuation',function()
        local run=Play.stage_one(3)
        local bytes=Persistence.encode(Game.snapshot(run.state))
        local restored=Game.restore(Persistence.decode(bytes))
        equal(Game.snapshot(run.state),Game.snapshot(restored))
        for _,intent in ipairs({{kind='wait'},{kind='wait'},
            {kind='basic_attack',dx=1,dy=0}}) do
            local a_ok,a_reason,a_events=Game.submit(run.state,intent)
            local b_ok,b_reason,b_events=Game.submit(restored,intent)
            assert(a_ok==b_ok and a_reason==b_reason)
            equal(a_events,b_events)
        end
        equal(Game.snapshot(run.state),Game.snapshot(restored))
    end)
end
