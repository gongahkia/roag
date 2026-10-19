return function(test,equal)
    local Game=require('src.game')
    local AI=require('src.ai')
    local Visibility=require('src.visibility')
    local I=require('src.idea_enemies')
    local Codec=require('src.persistence')
    local Playback=require('src.playback')
    local function trial()
        local s=Game.new(37);local p=Game.player(s)
        p.position.x,p.position.y=8,7
        local e=Game.spawn_enemy(s,'core:enemy/hollow_wraith',5,7,0)
        Visibility.recompute(s)
        return s,p,e
    end
    local function count(ev,kind)
        local n=0;for _,v in ipairs(ev)do if v.kind==kind then n=n+1 end end;return n
    end
    test('Wraith announces a pure visible wall-crossing destination and attacks separately',function()
        local s,p,e=trial();local before=Game.snapshot(s)
        local intent=AI.choose(s,e);assert(intent.support.kind=='phase')
        assert(intent.cells[1].x==7 and intent.cells[1].y==7)
        equal(before,Game.snapshot(s))
        local _,_,ev=Game.submit(s,{kind='wait'})
        assert(e.position.x==5 and e.pending_attack.due==111 and p.hp==24)
        assert(count(ev,'WindupStarted')==1 and count(ev,'DamageTaken')==0)
        local _,_,phase=Game.submit(s,{kind='wait'})
        assert(e.position.x==7 and p.hp==24 and count(phase,'Displaced')==1)
        assert(count(phase,'AttackPerformed')==0 and count(phase,'DamageTaken')==0)
        assert(Game.submit(s,{kind='wait'}));assert(p.hp==24 and e.pending_attack)
        local _,_,strike=Game.submit(s,{kind='wait'})
        assert(p.hp==22 and count(strike,'AttackPerformed')==1)
    end)
    test('occupying the committed phase landing cancels relocation without an ambush hit',function()
        local s,p,e=trial();assert(Game.submit(s,{kind='wait'}))
        local _,_,ev=Game.submit(s,{kind='move',dx=-1,dy=0})
        assert(p.position.x==7 and p.hp==24 and e.position.x==5)
        assert(count(ev,'PhaseBlocked')==1 and count(ev,'DamageTaken')==0)
    end)
    test('legal driver recognizes a phase marker as counterable support rather than damage',function()
        local s,p=trial();assert(Game.submit(s,{kind='wait'}))
        p.position.x=7 -- prepared decision on the marked destination
        local before=Game.snapshot(s)
        assert(not require('tests.legal_playthrough').escape({state=s,trace={},events={}}))
        equal(before,Game.snapshot(s))
    end)
    test('phase landing excludes stairs, exit cells and hidden destinations',function()
        local s,p,e=trial();local map=require('src.content').map_for(s)
        local cell={floor_id=s.active_floor_id,x=7,y=7}
        local free=function()return true end;local visible=function()return true end
        assert(I.phase_landing(s,e,cell,map,free,visible))
        assert(not I.phase_landing(s,e,cell,map,free,function()return false end))
        assert(not I.phase_landing(s,e,{floor_id=cell.floor_id,x=8,y=7},map,free,visible))
        local saved=map.placements
        -- Do not mutate the shared registered map, even in a fixture.
        local mapcopy={placements={stairs={{x=7,y=7}}}}
        assert(not I.phase_landing(s,e,cell,mapcopy,free,visible))
        s.entities[#s.entities+1]={controller='exit',position=cell}
        assert(not I.phase_landing(s,e,cell,map,free,visible))
        assert(map.placements==saved)
    end)
    test('current-position visibility query matches recompute without changing exploration or RNG',function()
        local s,p=trial();p.position.x,p.position.y=4,5
        local before=Game.snapshot(s);local visible={}
        local map=require('src.content').map_for(s)
        for y=1,map.height do for x=1,map.width do
            visible[y*map.width+x]=Visibility.can_see(s,s.active_floor_id,x,y)
        end end
        equal(before,Game.snapshot(s));Visibility.recompute(s)
        for y=1,map.height do for x=1,map.width do
            assert(visible[y*map.width+x]==Visibility.is_visible(s,s.active_floor_id,x,y))
        end end
    end)
    test('a marked phase arrival triggers the finite Trapper snare once',function()
        local s,p,e=trial()
        s.class_special={snare={floor_id=s.active_floor_id,x=7,y=7}}
        assert(Game.submit(s,{kind='wait'}))
        local _,_,ev=Game.submit(s,{kind='wait'})
        assert(e.position.x==7 and e.hp==4 and not s.class_special.snare
            and e.skip_next_turn and count(ev,'SnareTriggered')==1)
        assert(Game.submit(s,{kind='wait'}));assert(not e.skip_next_turn)
    end)
    test('committed phase save restores exact continuation and rejects forged geometry',function()
        local s=trial();assert(Game.submit(s,{kind='wait'}))
        local snapshot=Game.snapshot(s)
        local b=Game.restore(Codec.decode(Codec.encode(snapshot)))
        local _,_,aev=Game.submit(s,{kind='wait'});local _,_,bev=Game.submit(b,{kind='wait'})
        equal(aev,bev);equal(Game.snapshot(s),Game.snapshot(b))
        for _,e in ipairs(snapshot.entities)do if e.pending_attack then
            e.pending_attack.target_cells[1].x=8
        end end
        assert(not pcall(Game.restore,snapshot))
    end)
    test('stun and displacement cancel phase while playback cannot move simulation through walls',function()
        for _,kind in ipairs({'stun','displace'})do
            local s,p,e=trial();assert(Game.submit(s,{kind='wait'}))
            if kind=='stun' then assert(Game.stun(s,e.id,100))
            else assert(Game.displace(s,e.id,5,6))end
            assert(not e.pending_attack);assert(Game.submit(s,{kind='wait'}))
            assert(e.position.x==5 and p.hp==24)
        end
        local s,p,e=trial();assert(Game.submit(s,{kind='wait'}))
        local _,_,ev=Game.submit(s,{kind='wait'})
        local before=Game.snapshot(s);local pb=Playback.new();Playback.add(pb,ev,p.id)
        local found=false
        for _,item in ipairs(pb.items)do local motion=item.motions and item.motions[e.id]
            if motion then found=true;assert(motion.snap)end
        end
        assert(found);for _=1,100 do Playback.update(pb,.02)end
        equal(before,Game.snapshot(s))
    end)
    test('redirected Wraith art is exactly 416 in both packs and natural floors actually contain it',function()
        local audit=require('src.sprite_audit').collect()
        for _,name in ipairs({'kenney','onebit'})do
            local seen=0
            for _,h in ipairs(audit.packs[name].bindings)do
                if h.id:match('^core:enemy/hollow_wraith')then
                    assert(h.sheet=='onebit' and h.index==416);seen=seen+1
                end
            end
            assert(seen==3)
        end
        local found=false
        for seed=1,100 do
            local s=Game.new_stage(seed,{multifloor=true})
            for _,e in ipairs(s.entities)do if e.definition_id=='core:enemy/hollow_wraith' then found=true end end
            equal(Game.snapshot(s),Game.snapshot(Game.restore(Game.snapshot(s))))
        end
        assert(found,'Wraith not consumed by actual floor generation')
    end)
end
