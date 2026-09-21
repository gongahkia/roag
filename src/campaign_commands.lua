-- Typed campaign envelopes bind a local command to its site at queue time.
local U=require('src.util')
local W=require('src.world')
local Cmd=require('src.commands')
local Campaign=require('src.campaign')
local Command={}

local function visibleTarget(campaign,site,payload)
 local world=site.world
 if not (world.frontier and world.frontier.visibility==1) then return true end
 local x,y
 if payload.type=='order' or payload.type=='cancel' or payload.type=='priority' or payload.type=='toggle' or payload.type=='target_order' or payload.type=='place_rope' then x,y=payload.gx*4-2,payload.gy*4-2
 elseif payload.type=='port' then x,y=payload.x,payload.y
 elseif payload.type=='rally' then x,y=payload.x,payload.y
 elseif payload.type=='arm' then local s=world.structures[payload.slot];if s then x,y=s.gx*4-2,s.gy*4-2 end
 elseif payload.type=='field' then local p=require('src.content').find(world,payload.target);if p then x,y=p.x,p.y end
 end
 if not x then return true end
 return require('src.visibility').currentlyVisible(world,x,y,{campaign=campaign,siteId=site.id}),'Target is not currently visible'
end

local fields={
 order={type=true,kind=true,gx=true,gy=true,build=true,priority=true,worker=true},
 cancel={type=true,gx=true,gy=true},priority={type=true,gx=true,gy=true,value=true},
 toggle={type=true,gx=true,gy=true},port={type=true,slot=true,port=true,x=true,y=true},
 paint={type=true,x=true,y=true,material=true},labor={type=true,plan=true},
 rally={type=true,worker=true,x=true,y=true},releaserally={type=true,worker=true},
 field={type=true,kind=true,target=true,worker=true,priority=true},arm={type=true,slot=true,worker=true,priority=true},
 target_order={type=true,gx=true,gy=true,worker=true},
 school_policy={type=true,slot=true,schoolId=true,expectedPolicyRevision=true,enabled=true,mode=true,topicId=true,topicVersion=true,priority=true},
 fabricate={type=true,slot=true,kind=true,priority=true},place_rope={type=true,gx=true,gy=true,priority=true,direction=true},remove_rope={type=true,ropeId=true,priority=true},drop_tool={type=true,equipmentId=true,worker=true},load_tool={type=true,equipmentId=true,craftId=true,priority=true},unload_tool={type=true,equipmentId=true,craftId=true,priority=true},
 industry_config={type=true,structureId=true,recipe=true,priority=true,direction=true,mode=true,filter=true,enabled=true},
 faction_contact={type=true,factionId=true,personId=true},
 faction_protocol={type=true,factionId=true,personId=true,action=true},
 faction_accept_offer={type=true,structureId=true,offerId=true,representativeId=true},
 faction_depot_load={type=true,structureId=true,itemId=true,amount=true},
 faction_dispatch={type=true,structureId=true,offerId=true},
 faction_rebind={type=true,shipmentId=true,structureId=true},
 security_posture={type=true,posture=true,expectedPolicyRevision=true},
 security_policy={type=true,posts=true,refuge=true,expectedPolicyRevision=true},
 security_guard={type=true,personId=true,enabled=true},
 security_equip={type=true,personId=true,equipmentId=true},
 security_reload={type=true,personId=true,amount=true},
}
local campaignFields={
 prepare_expedition={scope=true,type=true,sourceSiteId=true,craftId=true,destinationSiteId=true,passengers=true,cargo=true},
 assemble_expedition={scope=true,type=true,sourceSiteId=true,craftId=true,manifestId=true},
 cancel_expedition={scope=true,type=true,sourceSiteId=true,craftId=true,manifestId=true},
 unload_cargo={scope=true,type=true,sourceSiteId=true,craftId=true,resource=true,amount=true},
 cancel_cargo_unload={scope=true,type=true,sourceSiteId=true,craftId=true,operationId=true},
 launch_expedition={scope=true,type=true,sourceSiteId=true,craftId=true,manifestId=true,expectedManifestRevision=true},
 return_to_origin={scope=true,type=true,craftId=true,journeyId=true,expectedLeg=true},
}

local function exact(t,allowed,label)
 assert(type(t)=='table',label..' must be a table')
 for key in pairs(t) do assert(allowed[key],'Unknown '..label..' key '..tostring(key)) end
 for key in pairs(allowed) do assert(t[key]~=nil,'Missing '..label..' key '..key) end
end

local function laborShape(plan)
 exact(plan,{quotas=true,weights=true,people=true},'campaign labour plan')
 exact(plan.weights,{general=true,dig=true,build=true,haul=true,farm=true,pump=true,field=true},'campaign labour weights')
 assert(type(plan.people)=='table','campaign labour people must be an array')
 local count,max=0,0
 for index in pairs(plan.people) do
  U.integer(index,'campaign labour person index',1,32);count=count+1;if index>max then max=index end
 end
 assert(count==max,'campaign labour people has a hole')
 for index=1,max do
  local person=plan.people[index]
  exact(person,{id=true,role=true,prefs=true},'campaign labour person')
  exact(person.prefs,{dig=true,build=true,haul=true,farm=true,pump=true,field=true},'campaign labour preferences')
 end
end

local function shape(envelope)
 assert(type(envelope)=='table','Malformed campaign command')
 if envelope.scope=='site' then
  for key in pairs(envelope) do assert(key=='scope' or key=='siteId' or key=='payload','Unknown campaign command key') end
  U.integer(envelope.siteId,'campaign command site ID',1,100000000)
  assert(type(envelope.payload)=='table' and type(envelope.payload.type)=='string','Malformed site command payload')
  local allowed=fields[envelope.payload.type];assert(allowed,'Unknown site command')
  for key in pairs(envelope.payload) do assert(allowed[key],'Unknown '..envelope.payload.type..' command key '..tostring(key)) end
  if envelope.payload.type=='labor' then laborShape(envelope.payload.plan) end
 elseif envelope.scope=='campaign' then
  assert(type(envelope.type)=='string','Malformed campaign command type')
  local allowed=campaignFields[envelope.type];assert(allowed,'Unknown campaign command')
  for key in pairs(envelope) do assert(allowed[key],'Unknown '..envelope.type..' command key '..tostring(key)) end
  U.integer(envelope.craftId,'campaign craft ID',1,100000000)
  if envelope.type=='prepare_expedition' then
   U.integer(envelope.sourceSiteId,'campaign command source site ID',1,100000000)
   U.integer(envelope.destinationSiteId,'campaign destination site ID',1,100000000);assert(type(envelope.passengers)=='table' and type(envelope.cargo)=='table','Malformed expedition plan')
  elseif envelope.type=='assemble_expedition' or envelope.type=='cancel_expedition' then U.integer(envelope.manifestId,'campaign manifest ID',1,100000000)
   ;U.integer(envelope.sourceSiteId,'campaign command source site ID',1,100000000)
  elseif envelope.type=='unload_cargo' then U.integer(envelope.sourceSiteId,'campaign command source site ID',1,100000000);assert(type(envelope.resource)=='string','Malformed cargo resource');U.integer(envelope.amount,'campaign unload amount',1,1000)
  elseif envelope.type=='cancel_cargo_unload' then U.integer(envelope.sourceSiteId,'campaign command source site ID',1,100000000);U.integer(envelope.operationId,'campaign cargo operation ID',1,100000000)
  elseif envelope.type=='launch_expedition' then U.integer(envelope.sourceSiteId,'campaign command source site ID',1,100000000);U.integer(envelope.manifestId,'campaign manifest ID',1,100000000);U.integer(envelope.expectedManifestRevision,'campaign manifest revision',1,100000000)
  else U.integer(envelope.journeyId,'campaign journey ID',1,100000000);assert(envelope.expectedLeg=='outbound' or envelope.expectedLeg=='return','Malformed expected journey leg') end
 else error('Unsupported campaign command scope') end
 require('src.campaign_codec').encode(envelope)
 return true
end

function Command.shape(envelope)
 local ok,why=pcall(shape,envelope)
 return ok,ok and nil or tostring(why)
end

function Command.valid(campaign,envelope)
 local ok,why=pcall(function()
  shape(envelope)
  if envelope.scope=='campaign' then
   local handler=(envelope.type=='launch_expedition' or envelope.type=='return_to_origin') and require('src.travel') or require('src.logistics')
   local valid,reason=handler.valid(campaign,envelope);assert(valid,reason)
  else
   local site=Campaign.site(campaign,envelope.siteId)
   assert(site,'Unknown campaign site')
   assert(site.ownerSocietyId==campaign.society.id,'Site is not owned by this society')
   local valid,reason
   if envelope.payload.type=='school_policy' then valid,reason=require('src.education').policyValid(campaign,site,envelope.payload)
   elseif envelope.payload.type=='fabricate' or envelope.payload.type=='place_rope' or envelope.payload.type=='remove_rope' or envelope.payload.type=='drop_tool' or envelope.payload.type=='load_tool' or envelope.payload.type=='unload_tool' then valid,reason=visibleTarget(campaign,site,envelope.payload);if valid then valid,reason=require('src.equipment_commands').valid(campaign,site,envelope.payload) end
   elseif envelope.payload.type=='industry_config' then
    local s=require('src.industry').find(site.world,envelope.payload.structureId)
    valid=s and site.world.frontier and site.world.frontier.industry==1,'Industrial structure is unavailable'
    if valid then valid,reason=pcall(require('src.industry').validConfig,s,envelope.payload) end
   elseif envelope.payload.type:sub(1,9)=='security_' then valid,reason=campaign.features.security==1,'Security is unavailable'
   elseif envelope.payload.type:sub(1,8)=='faction_' then
    valid=campaign.features.factions==1;reason='Factions are unavailable'
   else valid,reason=visibleTarget(campaign,site,envelope.payload);if valid then valid,reason=Cmd.valid(site.world,envelope.payload) end end
   assert(valid,reason)
  end
 end)
 return ok,ok and nil or tostring(why)
end

function Command.apply(campaign,envelope)
 local structural,reason=Command.shape(envelope)
 if not structural then return false,reason end
 if envelope.scope=='campaign' then
  local handler=(envelope.type=='launch_expedition' or envelope.type=='return_to_origin') and require('src.travel') or require('src.logistics')
  return handler.apply(campaign,envelope)
 end
 local site=Campaign.site(campaign,envelope.siteId)
 if not site then return false,'Unknown campaign site' end
 if site.ownerSocietyId~=campaign.society.id then return false,'Site is not owned by this society' end
 local valid,why
 if envelope.payload.type=='school_policy' then valid,why=require('src.education').policyValid(campaign,site,envelope.payload)
 elseif envelope.payload.type=='fabricate' or envelope.payload.type=='place_rope' or envelope.payload.type=='remove_rope' or envelope.payload.type=='drop_tool' or envelope.payload.type=='load_tool' or envelope.payload.type=='unload_tool' then
  valid,why=require('src.equipment_commands').valid(campaign,site,envelope.payload)
 elseif envelope.payload.type=='industry_config' then
  local s=require('src.industry').find(site.world,envelope.payload.structureId);valid=s and site.world.frontier and site.world.frontier.industry==1,'Industrial structure is unavailable'
  if valid then valid,why=pcall(require('src.industry').validConfig,s,envelope.payload) end
 elseif envelope.payload.type:sub(1,9)=='security_' then valid,why=campaign.features.security==1,'Security is unavailable'
 elseif envelope.payload.type:sub(1,8)=='faction_' then valid,why=campaign.features.factions==1,'Factions are unavailable'
 else valid,why=visibleTarget(campaign,site,envelope.payload);if valid then valid,why=Cmd.valid(site.world,envelope.payload) end end
 if not valid then
  W.event(site.world,'rejected',why)
  return false,why
 end
 local applied
 if envelope.payload.type=='school_policy' then applied=require('src.education').applyPolicy(campaign,site,envelope.payload)
 elseif envelope.payload.type=='fabricate' or envelope.payload.type=='place_rope' or envelope.payload.type=='remove_rope' or envelope.payload.type=='drop_tool' or envelope.payload.type=='load_tool' or envelope.payload.type=='unload_tool' then applied=require('src.equipment_commands').apply(campaign,site,envelope.payload)
 elseif envelope.payload.type=='industry_config' then applied=require('src.industry').configure(site.world,require('src.industry').find(site.world,envelope.payload.structureId),envelope.payload)
 elseif envelope.payload.type=='security_posture' then
  local ok,result=pcall(require('src.security').configure,site.world,envelope.payload,envelope.payload.expectedPolicyRevision);if not ok then return false,tostring(result) end;applied=result
 elseif envelope.payload.type=='security_policy' then
  local ok,result=pcall(require('src.security').setPosts,site.world,envelope.payload.posts,envelope.payload.refuge,envelope.payload.expectedPolicyRevision,{campaign=campaign,siteId=site.id});if not ok then return false,tostring(result) end;applied=result
 elseif envelope.payload.type=='security_guard' or envelope.payload.type=='security_equip' or envelope.payload.type=='security_reload' then
  local Security=require('src.security');local fn=envelope.payload.type=='security_guard' and Security.toggleGuard or envelope.payload.type=='security_equip' and Security.equip or Security.reload
  local ok,result,why
  if envelope.payload.type=='security_guard' then ok,result,why=pcall(fn,campaign,envelope.siteId,envelope.payload.personId,envelope.payload.enabled)
  elseif envelope.payload.type=='security_equip' then ok,result,why=pcall(fn,campaign,envelope.siteId,envelope.payload.personId,envelope.payload.equipmentId)
  else ok,result,why=pcall(fn,campaign,envelope.siteId,envelope.payload.personId,envelope.payload.amount) end
  if not ok then return false,tostring(result) end;applied=result
 elseif envelope.payload.type=='faction_contact' then applied=require('src.factions').beginContact(campaign,envelope.siteId,envelope.payload.factionId,envelope.payload.personId)
 elseif envelope.payload.type=='faction_protocol' then applied=require('src.factions').resolveProtocol(campaign,envelope.siteId,envelope.payload.factionId,envelope.payload.personId,envelope.payload.action)
 elseif envelope.payload.type=='faction_accept_offer' then applied=require('src.factions').acceptOffer(campaign,envelope.siteId,envelope.payload.structureId,envelope.payload.offerId,envelope.payload.representativeId)
 elseif envelope.payload.type=='faction_depot_load' then applied=require('src.factions').deposit(campaign,envelope.siteId,envelope.payload.structureId,envelope.payload.itemId,envelope.payload.amount)
 elseif envelope.payload.type=='faction_dispatch' then applied=require('src.factions').dispatch(campaign,envelope.siteId,envelope.payload.structureId,envelope.payload.offerId)
 elseif envelope.payload.type=='faction_rebind' then applied=require('src.factions').rebindShipment(campaign,envelope.payload.shipmentId,envelope.siteId,envelope.payload.structureId)
 else applied=Cmd.apply(site.world,envelope.payload) end
 return applied,applied and nil or 'Site command was rejected'
end

return Command
