-- Work policies are simulation state. Quotas apportion AUTO workers, not time slices.
-- Survival/escape overrides, explicit job ownership and manual movement are handled
-- by jobs.lua. Stable ID / role ordering makes the allocation replayable.
local U=require('src.util')
local W=require('src.world')
local L={roles={'general','dig','build','haul','farm','pump','field'},duties={'dig','build','haul','farm','pump','field'}}
L.labels={general='General',dig='Mining',build='Construction',haul='Hauling',farm='Food / crops',pump='Pumps',field='Fieldwork'}
local known={};for _,r in ipairs(L.roles) do known[r]=true end
local duty={};for _,r in ipairs(L.duties) do duty[r]=true end
function L.default(w)
 local p={quotas=false,weights={general=100,dig=0,build=0,haul=0,farm=0,pump=0,field=0},people={}}
 for _,a in ipairs(w.workers) do
  local prefs={};for _,r in ipairs(L.duties) do prefs[r]=2 end
  p.people[#p.people+1]={id=a.id,role='auto',prefs=prefs}
 end
 return p
end
function L.snapshot(w) return U.deep(w.labor or L.default(w)) end
function L.person(p,id) for _,a in ipairs(p.people) do if a.id==id then return a end end end
function L.validate(w,p)
 assert(type(p)=='table' and type(p.quotas)=='boolean' and type(p.weights)=='table' and type(p.people)=='table','Invalid work policy')
 local total=0
 for _,r in ipairs(L.roles) do U.integer(p.weights[r],'quota '..r,0,100);total=total+p.weights[r] end
 for k in pairs(p.weights) do assert(known[k],'Unknown quota role') end
 assert(total==100,'Quota percentages must total 100')
 assert(#p.people==#w.workers,'Work policy must cover every worker')
 local seen={}
 for _,a in ipairs(p.people) do
  assert(type(a)=='table' and W.find(w.workers,a.id) and not seen[a.id],'Unknown/duplicate worker')
  seen[a.id]=true;assert(a.role=='auto' or known[a.role],'Unknown worker role')
  assert(type(a.prefs)=='table','Missing duty priorities')
  for _,r in ipairs(L.duties) do U.integer(a.prefs[r],'duty priority',0,3) end
  for k in pairs(a.prefs) do assert(duty[k],'Unknown duty') end
 end
 return true
end
-- Largest remainder: integer counts sum to the number of workers, including zero.
function L.apportion(n,weights)
 local out,remainders={},{};local used=0
 for i,r in ipairs(L.roles) do
  local v=n*weights[r]/100;out[r]=math.floor(v);used=used+out[r]
  remainders[#remainders+1]={role=r,fraction=v-out[r],order=i}
 end
 table.sort(remainders,function(a,b) if a.fraction~=b.fraction then return a.fraction>b.fraction end return a.order<b.order end)
 for i=1,n-used do out[remainders[i].role]=out[remainders[i].role]+1 end
 return out
end
function L.adjust(weights,role,delta)
 local v=U.copy(weights)
 if delta<0 then
  local n=math.min(v[role],-delta);v[role]=v[role]-n
  local recipient=role=='general' and 'dig' or 'general';v[recipient]=v[recipient]+n
 else
  local remain=math.min(delta,100-v[role])
  for _,other in ipairs(L.roles) do if other~=role and remain>0 then
   local n=math.min(v[other],remain);v[other]=v[other]-n;v[role]=v[role]+n;remain=remain-n
  end end
 end
 return v
end
function L.allocate(w,p)
 p=p or w.labor
 if not p then return {},{} end
 local result,auto,counts={},{},{}
 for _,a in ipairs(w.workers) do if a.alive then
  local plan=L.person(p,a.id)
  if plan and plan.role~='auto' then result[a.id]=plan.role
  else auto[#auto+1]=a end
 end end
 table.sort(auto,function(a,b)return a.id<b.id end)
 if not p.quotas then for _,a in ipairs(auto) do result[a.id]='general' end return result,counts end
 counts=L.apportion(#auto,p.weights)
 -- Specialists are placed before generalists. Disabled duties leave vacancies.
 for _,r in ipairs(L.duties) do
  for _=1,counts[r] do
   local best,score
   for _,a in ipairs(auto) do if not result[a.id] then
    local person=L.person(p,a.id);local pref=person.prefs[r]
    local skill=r=='dig' and a.mine or r=='build' and a.build or 1
    local s=pref*10+skill
    if pref>0 and (not score or s>score) then best,score=a,s end
   end end
   if best then result[best.id]=r end
  end
 end
 -- Do not fill an unmet specialist slot with a forbidden duty or silently change
 -- the requested mix. Only the explicitly apportioned general slots are filled.
 local left=counts.general
 for _,a in ipairs(auto) do if not result[a.id] then
  if left>0 then result[a.id]='general';left=left-1 else result[a.id]='unfilled' end
 end end
 return result,counts
end
function L.roleForTask(w,t)
 if t.kind=='work' then
  local j=W.find(w.jobs,t.job)
  return j and (j.kind=='dig' and 'dig' or 'build') or 'build'
 end
 return ({haul='haul',irrigate='farm',harvest='farm',pump='pump',field='field'})[t.kind]
end
function L.score(w,a,t)
 local role=L.roleForTask(w,t);if not role then return 0 end
 local j=t.job and W.find(w.jobs,t.job)
 if j and j.owner and j.owner~=a.id then return nil end
 if not w.labor then return 0 end
 local person=L.person(w.labor,a.id);if not person or person.prefs[role]==0 then return nil end
 local assignments=w.laborAssignments or L.allocate(w)
 local assigned=assignments[a.id] or 'general'
 -- A named order overrides a quota/role, but NOT an explicitly disabled duty.
 if not (j and j.owner==a.id) and assigned~='general' and assigned~=role then return nil end
 return (person.prefs[role]-2)*140
end
function L.apply(w,p)
 L.validate(w,p)
 local J=require('src.jobs')
 for _,a in ipairs(w.workers) do if a.alive and a.task and L.roleForTask(w,a.task) then J.release(w,a,true);a.thinkAt=w.tick end end
 w.labor=U.deep(p);w.laborAssignments=L.allocate(w)
 W.event(w,'labor','Work policy changed. Personal needs and immediate hazards still override work.')
end
function L.refresh(w)
 if not w.labor then return end
 local new=L.allocate(w)
 local J=require('src.jobs')
 for _,a in ipairs(w.workers) do
  if a.alive and a.task and L.roleForTask(w,a.task) then
   local old=w.laborAssignments;w.laborAssignments=new
   if L.score(w,a,a.task)==nil then J.release(w,a,true);a.thinkAt=w.tick end
   w.laborAssignments=old
  end
 end
 w.laborAssignments=new
end
return L
