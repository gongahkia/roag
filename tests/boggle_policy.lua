-- Test-only one-route lookahead through the real resolver. Never edits live state.
local G=require('src.game')
local B=require('src.boggle')
local P={}
function P.routes(s,visit)
 local path,seen={},{}
 local function walk(at)
  path[#path+1]=at;seen[at]=true
  if #path==4 then local ids={};for _,i in ipairs(path)do ids[#ids+1]=s.boggle.board[i].id end;visit(ids,path)
  else for _,n in ipairs({at-6,at+1,at+6,at-1})do
   if n>=1 and n<=36 and not seen[n] and math.abs((at-1)%6-(n-1)%6)+math.abs(math.floor((at-1)/6)-math.floor((n-1)/6))==1 then walk(n)end
  end end
  seen[at]=nil;path[#path]=nil
 end
 for i=1,36 do walk(i)end
end
function P.score(before,after)
 if after.card_trial.phase=='death'then return -100000 end
 if after.card_trial.phase=='victory' or after.card_trial.phase=='intermission'then return 10000+G.player(after).hp end
 local score=0
 for _,e in ipairs(before.entities)do if e.faction=='enemy'then
  local next=G.entity(after,e.id);score=score+(e.hp-(next and next.hp or 0))*8+(not next and 30 or 0)
 end end
 local p,q=G.player(before),G.player(after)
 score=score+(q.hp-p.hp)*6+(after.boggle.guard-before.boggle.guard)*1.4
 for id in pairs(q.boons)do if not p.boons[id]then score=score+15 end end
 score=score+(after.boggle.focus-before.boggle.focus)*1.2-(after.boggle.dull-before.boggle.dull)*1.2
 local distance=100
 for _,e in ipairs(after.entities)do if e.faction=='enemy'then distance=math.min(distance,math.abs(e.position.x-q.position.x)+math.abs(e.position.y-q.position.y))end end
 return score-distance*.6
end
function P.choose(s)
 local best,score,forecast;local cache={}
 P.routes(s,function(ids)
  local f=B.forecast(s,ids,G,cache)
  if f.valid then local v=P.score(s,f.state)
   if not score or v>score then best,score,forecast=ids,v,f end
  end
 end)
 return best,forecast,score
end
return P
