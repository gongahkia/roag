-- Test-only one-action lookahead. Uses the real resolver on private snapshots;
-- never edits the live actor, hand, RNG or encounters to win a playthrough.
local G=require('src.game')
local Cards=require('src.cards')
local Content=require('src.content')
local P={}
local directions={{0,-1},{1,-1},{1,0},{1,1},{0,1},{-1,1},{-1,0},{-1,-1}}
local submit=G.submit
local function enemies(s)
    local hp,n=0,0
    for _,e in ipairs(s.entities)do if e.faction=='enemy' and e.hp and e.hp>0 then hp=hp+e.hp;n=n+1 end end
    return hp,n
end
local function distance(s)
    local p=G.player(s).position;local targets={};local map=Content.map_for(s)
    for _,e in ipairs(s.entities)do if e.faction=='enemy' and e.hp and e.hp>0 then targets[e.position.x..','..e.position.y]=true end end
    local q={{x=p.x,y=p.y,d=0}};local seen={[p.x..','..p.y]=true};local h=1
    while q[h]do local a=q[h];h=h+1
        for _,v in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do
            local x,y=a.x+v[1],a.y+v[2];local k=x..','..y
            if targets[k]then return a.d+1 end
            if not seen[k] and Content.walkable(map,x,y)then seen[k]=true;q[#q+1]={x=x,y=y,d=a.d+1}end
        end
    end
    return 0
end
local function pairs_in(hand)
    local ranks={}
    for _,id in ipairs(hand)do local rank=Cards.card(id).rank;ranks[rank]=(ranks[rank]or 0)+1 end
    local n=0;for _,v in pairs(ranks)do n=n+math.floor(v/2)end;return n
end
function P.choose(s,coverage)
    local hand=s.card_trial.cards.hand;local selections={}
    for _,id in ipairs(hand)do selections[#selections+1]={id}end
    for i=1,4 do for j=i+1,5 do
        if Cards.card(hand[i]).rank==Cards.card(hand[j]).rank then selections[#selections+1]={hand[i],hand[j]}end
    end end
    local old_hp,old_n=enemies(s);local old_p=G.player(s).hp;local old_d=distance(s)
    local best,best_score,best_state,best_events
    for _,ids in ipairs(selections)do
        for _,a in ipairs({'move','attack','heal','redraw'})do
            local count=(a=='move' or a=='attack')and 8 or 1
            for i=1,count do
                local d=directions[i]
                local intent={kind='card_action',action=a,cards=ids,dx=count>1 and d[1]or nil,dy=count>1 and d[2]or nil}
                if G.card_preview(s,intent).valid then
                    local next=G.snapshot(s);local ok,_,ev=submit(next,intent);assert(ok)
                    local hp,n=enemies(next);local p=G.player(next)
                    local loss=old_p-p.hp
                    local score=(old_hp-hp)*2+(old_n-n)*9-loss*5+(old_d-distance(next))*1.1
                        -#ids*.4-(next.clock-s.clock)*.003+pairs_in(next.card_trial.cards.hand)*.3
                    if a=='heal'then score=score-13 end
                    if a=='attack' and hp==old_hp then score=score-3 end
                    if a=='redraw'then score=score-1 end
                    if p.hp<=0 then score=score-10000 end
                    if next.card_trial.phase=='reward' or next.card_trial.phase=='victory'then score=score+50 end
                    if coverage and loss<4 and p.hp>0 then
                        local name=a..(#ids==2 and '_pair' or '_single')
                        if (name=='move_pair' or name=='attack_pair') and not coverage[name]then score=score+4 end
                        if a=='heal' and not coverage.heal_single and #ids==1 and old_p<=20 then score=score+35
                        elseif a=='heal' and coverage.heal_single and not coverage.heal_pair and #ids==2 and old_p<=16 then score=score+35 end
                        if a=='redraw' and not coverage.redraw_single and old_p>=18 then score=score+4 end
                    end
                    if not best or score>best_score then best,best_score,best_state,best_events=intent,score,next,ev end
                end
            end
        end
    end
    assert(best,'no legal card action');return best,best_state,best_events
end
return P
