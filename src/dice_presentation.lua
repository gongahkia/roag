-- Cosmetic clocks and physical face identities; no Game, RNG or storage writes.
local P={roll_seconds=.20,unfold_seconds=.18,refold_seconds=.15}
P.normals={{0,0,1},{1,0,0},{0,1,0},{-1,0,0},{0,-1,0},{0,0,-1}}
-- Cross net: top 5, left 4, centre 1, right 2, bottom 3, tail 6.
P.net={{1,1},{2,1},{1,2},{0,1},{1,0},{1,3}}
local function copy(v)if type(v)~='table'then return v end;local t={};for k,x in pairs(v)do t[k]=copy(x)end;return t end
function P.new(dice,reduced)
 return {revision=dice.revision,results=copy(dice.results),dice=copy(dice.dice),age=reduced and P.roll_seconds or 0,
  rolling=not reduced,opening=not reduced,reduced=reduced or false,roll_count=1,net=0,net_target=false,net_die=1,
  focus={0,0,0},press={1,1,1},use_age=1,accents={},seen_events={},sounds={'roll'}}
end
function P.sound(p,name)p.sounds[#p.sounds+1]=name end
function P.finish(p)p.age=P.roll_seconds;p.rolling=false;p.opening=false end
function P.sync(p,dice)
 if dice.revision<=p.revision then return false end
 p.revision=dice.revision;p.results=copy(dice.results);p.dice=copy(dice.dice)
 p.age=0;p.rolling=true;p.opening=false;p.roll_count=p.roll_count+1;p.landed=false
 P.sound(p,'roll');return true
end
function P.inspect(p,open,die)
 if die then p.net_die=die end
 if p.reduced then p.net=open and 1 or 0 end
 if p.net_target~=open then p.net_target=open;P.sound(p,'fold')end
end
function P.select(p,n)p.press[n]=0 end
function P.use(p,n,face,dx,dy)
 p.use_age=0;p.use_die=n;p.use_face=face;p.dx=dx or 0;p.dy=dy or 0
end
function P.edit(p,die,index,old,new)
 p.edit={die=die,index=index,old=old,new=new,age=0};P.sound(p,'edit')
end
function P.update(p,dt,busy,focus)
 dt=math.max(0,dt or 0)
 p.age=p.age+dt;p.use_age=math.min(1,p.use_age+dt)
 for n=1,3 do
  local target=focus==n and 1 or 0;p.focus[n]=p.focus[n]+(target-p.focus[n])*math.min(1,dt/.06)
  p.press[n]=math.min(1,p.press[n]+dt/.08)
 end
 if p.rolling and (p.age>=P.roll_seconds or not busy and not p.opening or p.reduced)then
  P.finish(p);if not p.landed then P.sound(p,'land');p.landed=true end
 end
 local target=p.net_target and 1 or 0
 if p.reduced then p.net=target
 else local duration=p.net_target and P.unfold_seconds or P.refold_seconds
  if target>p.net then p.net=math.min(target,p.net+dt/duration)else p.net=math.max(target,p.net-dt/duration)end
 end
 if p.edit then
  p.edit.age=p.edit.age+dt
  if p.edit.age>=.24 or p.reduced then P.inspect(p,false)end
  if p.edit.age>=.24+P.refold_seconds or p.reduced then p.edit=nil end
 end
 for n=#p.accents,1,-1 do local a=p.accents[n];a.age=a.age+dt;if a.age>.55 then table.remove(p.accents,n)end end
end
function P.net_face(p,n,j)
 local edit=p.edit
 if edit and edit.die==n and edit.index==j and edit.age<.12 then return edit.old end
 return p.dice[n][j]
end
function P.events(p,events,player)
 for _,e in ipairs(events or {})do
  if e.id and not p.seen_events[e.id]then
   p.seen_events[e.id]=true
   local label,kind
   if e.kind=='BoonActivated'and e.boon_id=='core:boon/rhythm_of_steel'then label=p.labels and p.labels[e.id]or 'RHYTHM';kind='rhythm'
   elseif e.kind=='BoonActivated'and e.boon_id=='core:boon/detonation_bloom'then label='BLOOM';kind='bloom'
   elseif (e.kind=='DamageAttempted'or e.kind=='DamageTaken'or e.kind=='DamageBlocked')and e.target_id==player and (e.guard_absorbed or 0)>0 then label='GUARD -'..e.guard_absorbed;kind='guard'
   elseif e.kind=='BoardGuarded'and e.source_id==player then label='GUARD +2';kind='guard'
   elseif e.kind=='DamageTaken'and not e.direct_attack then
    for _,a in ipairs(e.ancestry or {})do
     if a:find('rhythm_of_steel',1,true)then label='RHYTHM +'..e.actual_loss;kind='rhythm'end
     if a:find('detonation_bloom',1,true)then label='BLOOM '..e.actual_loss;kind='bloom'end
    end
   end
   if label then
    p.accents[#p.accents+1]={label=label,kind=kind,x=e.x,y=e.y,source=e.source_id,age=0,face=p.use_face}
    if #p.accents>8 then table.remove(p.accents,1)end
   end
  end
 end
end
return P
