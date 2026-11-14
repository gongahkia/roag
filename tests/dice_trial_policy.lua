-- Test-only policy: consider only this roll's legal commands, never future dice.
local G=require('src.game')
local D=require('src.dice_trial')
local Content=require('src.content')
local P={}
local dirs={{0,-1},{1,-1},{1,0},{1,1},{0,1},{-1,1},{-1,0},{-1,-1}}
local function stats(s)
 local hp,n,d=0,0,100;local p=G.player(s).position
 -- Arena path distance avoids trying to attack through the central pillar.
 local queue={{p.x,p.y,0}};local seen={[p.x..','..p.y]=0};local head=1
 while queue[head]do local at=queue[head];head=head+1
  for _,v in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do local x,y=at[1]+v[1],at[2]+v[2];local k=x..','..y
   if not seen[k]and Content.walkable(Content.map_for(s),x,y)then seen[k]=at[3]+1;queue[#queue+1]={x,y,at[3]+1}end
  end
 end
 for _,e in ipairs(s.entities)do if e.faction=='enemy'and e.hp>0 then hp=hp+e.hp;n=n+1;d=math.min(d,seen[e.position.x..','..e.position.y]or 100)end end
 return hp,n,n==0 and 0 or d
end
function P.choose(s)
 local oh,on,od=stats(s);local op=G.player(s).hp;local best,score
 for n=1,3 do for _,v in ipairs(dirs)do
  local i={kind='dice_action',die=n,revision=s.dice.revision,dx=v[1],dy=v[2]}
  local f=D.forecast(s,i,G)
  if f.valid then
   local h,num,dist=stats(f.state);local p=G.player(f.state)
   local value=(oh-h)*4+(on-num)*15-(op-p.hp)*5+(od-dist)*3-f.plan.cost*.003
   if p.hp<=0 then value=value-10000 end
   if f.state.card_trial.phase=='victory'or f.state.card_trial.phase=='face_edit'then value=value+100 end
   if not score or value>score then best,score=i,value end
  end
 end end
 assert(best,'no legal action');return best
end
return P
