return function(test,equal)
    local G=require('src.game')
    local Play=require('tests.legal_playthrough')
    test('combat driver can legally Dash out of a committed box with no safe one-tile escape',function()
        local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=7,6
        local e=G.spawn_enemy(s,'core:enemy/ember_crucible',10,6,0)
        e.ai_memory.attacks=1 -- prepared target-box fixture, not a full legal run
        assert(G.submit(s,{kind='wait'}));assert(e.pending_attack)
        local before=G.snapshot(s)
        local run={state=s,trace={},events={}}
        assert(not Play.escape(run,e.position));equal(before,G.snapshot(s))
        assert(Play.escape(run,e.position,nil,true))
        assert(run.trace[1].intent.kind=='dash' and p.position.x==9 and p.position.y==6)
        assert(p.hp==24 and s.clock==230,'normal recovery resolves the old box without a hit')
        assert(not e.pending_attack)
    end)
    test('Arcanist can hit an off-axis foe with the real Fire Sigil without chasing a bolt line',function()
        local s=G.new(37,nil,{class_id='core:actor/arcanist'})
        local p=G.player(s);p.position.x,p.position.y=7,6
        local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',9,7,100000)
        local run={state=s,trace={},events={}}
        assert(Play.attack_in_reach(run))
        assert(run.trace[1].intent.kind=='sweeping_slash' and e.hp==8)
        assert(p.position.x==7 and p.position.y==6 and s.clock==170)
    end)
    test('ranged combat can choose a safe lateral dodge instead of repeating the same axial corridor retreat',function()
        local function trial(prefer)
            local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=7,6
            local e=G.spawn_enemy(s,'core:enemy/thornspitter',7,9,50)
            assert(G.submit(s,{kind='wait'}));assert(e.pending_attack)
            local run={state=s,trace={},events={}}
            assert(Play.escape(run,e.position,nil,true,prefer))
            assert(p.hp==24 and s.clock==200)
            return p.position
        end
        local old=trial(false);assert(old.x==7 and old.y==5)
        local lateral=trial(true);assert(lateral.x==8 and lateral.y==6)
    end)

    test('an armored travel cycle may ignore harmless hits while a zero-damage Rammer shove still demands escape',function()
        local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=7,6;p.defense=2
        local e=G.spawn_enemy(s,'core:enemy/thornspitter',7,9,50)
        assert(G.submit(s,{kind='wait'}));local before=G.snapshot(s)
        local run={state=s,trace={},events={}}
        assert(not Play.escape(run,e.position,true));equal(before,G.snapshot(s))
        assert(Play.escape(run,e.position,false))
        s=G.new(37);p=G.player(s);p.position.x,p.position.y=7,6;p.defense=2
        e=G.spawn_enemy(s,'core:enemy/heavy_rammer',6,6,50)
        assert(G.submit(s,{kind='wait'}));assert(e.pending_attack.shove)
        run={state=s,trace={},events={}}
        assert(Play.escape(run,e.position,true) and #run.trace==1)
    end)

end
