-- Test-only one-action policy. Legal commands use the production resolver on copies.
local G=require('src.game')
local Cards=require('src.cards')
local Poker=require('src.poker')
local Content=require('src.content')
local P={}
local dirs={{0,-1},{1,-1},{1,0},{1,1},{0,1},{-1,1},{-1,0},{-1,-1}}
local function enemies(s)
 local hp,n=0,0
 for _,e in ipairs(s.entities)do if e.faction=='enemy' and e.hp and e.hp>0 then hp=hp+e.hp;n=n+1 end end
 return hp,n
end
local function distance(s)
 local p=G.player(s).position;local map=Content.map_for(s);local best=100
 for _,e in ipairs(s.entities)do if e.faction=='enemy' and e.hp and e.hp>0 then
  local q={{x=p.x,y=p.y,d=0}};local h=1;local seen={}
  while q[h]do local a=q[h];h=h+1
   if a.x==e.position.x and a.y==e.position.y then best=math.min(best,a.d);break end
   for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do
    local x,y=a.x+d[1],a.y+d[2];local k=x..','..y
    if not seen[k] and Content.walkable(map,x,y)then seen[k]=true;q[#q+1]={x=x,y=y,d=a.d+1}end
   end
  end
 end end
 return best==100 and 0 or best
end
function P.choose(s)
 if s.card_trial.resource_phase=='prepare'then return {kind='cash_hand'}end
 local candidates={}
 for _,d in ipairs(dirs)do
  candidates[#candidates+1]={kind='trial_action',action='move',dx=d[1],dy=d[2]}
  candidates[#candidates+1]={kind='trial_action',action='strike',dx=d[1],dy=d[2]}
 end
 for _,a in ipairs({'dash','sweep'})do for _,at in ipairs(G.card_targets(s,{kind='trial_action',action=a}))do
  candidates[#candidates+1]={kind='trial_action',action=a,x=at.x,y=at.y}
 end end
 candidates[#candidates+1]={kind='trial_action',action='mend'}
 candidates[#candidates+1]={kind='trial_action',action='recover'}
 local old_hp,old_n=enemies(s);local old_p=G.player(s).hp;local old_d=distance(s)
 local best,best_score,best_state,best_events
 for _,i in ipairs(candidates)do local preview=G.card_preview(s,i)
  if preview.valid and not (i.action=='move' and preview.bump)then
   local probe=G.snapshot(s);local ok,_,e=G.submit(probe,i);assert(ok)
   local hp,n=enemies(probe);local p=G.player(probe)
   local budget=probe.card_trial.stamina
   if probe.card_trial.resource_phase=='prepare'then budget=Poker.best(probe.card_trial.cards,probe.card_trial.cards.hand).stamina end
   local score=(old_hp-hp)*3+(old_n-n)*12-(old_p-p.hp)*2+(old_d-distance(probe))*3-(probe.clock-s.clock)*.003+budget*.6
   if i.action=='strike' and hp==old_hp then score=score-3 end
   if i.action=='mend'then score=score-7 end
   if i.action=='recover'then score=score-1 end
   if p.hp<=0 then score=score-10000 end
   if probe.card_trial.phase=='reward' or probe.card_trial.phase=='victory'then score=score+50 end
   if not best or score>best_score then best,best_score,best_state,best_events=i,score,probe,e end
  end
 end
 assert(best,'no legal action');return best,best_state,best_events
end
return P
