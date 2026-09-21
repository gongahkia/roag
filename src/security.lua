-- COS-G06 security state.  This module owns bounded policy and person state;
-- combat/raids are layered on these stable feature-gated records.
local U=require('src.util')
local R=require('src.campaign_random')
local S={version=1}
function S.enabled(c) return c and c.features and c.features.security==1 end
function S.attach(w) w.security={version=1,posture='normal',posts={},policyRevision=1,events={}} end
function S.attachPerson(c,a)
 local r=R.new(R.derive(c.seed,'person/'..a.personId..'/security/combatXP/v1'))
 a.security={guardEnabled=false,combatXP=R.uniform(r,80)-1,grievance=0,causes={},ammo=0}
end
function S.initialise(c)
 for _,site in ipairs(c.sites) do for _,a in ipairs(site.world.workers) do if not a.security then S.attachPerson(c,a) end end end
end
function S.configure(w,p)
 assert(p.posture=='normal' or p.posture=='alert' or p.posture=='lockdown','Invalid security posture')
 w.security.posture=p.posture;w.security.policyRevision=w.security.policyRevision+1
end
function S.validate(c)
 for _,site in ipairs(c.sites) do local w=site.world;assert(type(w.security)=='table' and w.security.version==1,'Missing security state');assert(w.security.posture=='normal' or w.security.posture=='alert' or w.security.posture=='lockdown','Invalid security posture');assert(#w.security.posts<=8,'Too many security posts')
  for _,a in ipairs(w.workers) do local x=a.security;assert(type(x)=='table','Missing personal security');assert(type(x.guardEnabled)=='boolean');U.integer(x.combatXP,'combat XP',0,400);U.integer(x.grievance,'grievance',0,100);U.integer(x.ammo,'ammo',0,12);assert(#x.causes<=8,'Too many grievance causes') end
 end
 return true
end
return S
