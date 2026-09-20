-- Typed campaign envelopes bind a local command to its site at queue time.
local U=require('src.util')
local W=require('src.world')
local Cmd=require('src.commands')
local Campaign=require('src.campaign')
local Command={}

local fields={
 order={type=true,kind=true,gx=true,gy=true,build=true,priority=true,worker=true},
 cancel={type=true,gx=true,gy=true},priority={type=true,gx=true,gy=true,value=true},
 toggle={type=true,gx=true,gy=true},port={type=true,slot=true,port=true,x=true,y=true},
 paint={type=true,x=true,y=true,material=true},labor={type=true,plan=true},
 rally={type=true,worker=true,x=true,y=true},releaserally={type=true,worker=true},
 field={type=true,kind=true,target=true,worker=true,priority=true},arm={type=true,slot=true,worker=true,priority=true},
 target_order={type=true,gx=true,gy=true,worker=true},
}
local campaignFields={
 prepare_expedition={scope=true,type=true,sourceSiteId=true,craftId=true,destinationSiteId=true,passengers=true,cargo=true},
 assemble_expedition={scope=true,type=true,sourceSiteId=true,craftId=true,manifestId=true},
 cancel_expedition={scope=true,type=true,sourceSiteId=true,craftId=true,manifestId=true},
 unload_cargo={scope=true,type=true,sourceSiteId=true,craftId=true,resource=true,amount=true},
 cancel_cargo_unload={scope=true,type=true,sourceSiteId=true,craftId=true,operationId=true},
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
  U.integer(envelope.sourceSiteId,'campaign command source site ID',1,100000000);U.integer(envelope.craftId,'campaign craft ID',1,100000000)
  if envelope.type=='prepare_expedition' then
   U.integer(envelope.destinationSiteId,'campaign destination site ID',1,100000000);assert(type(envelope.passengers)=='table' and type(envelope.cargo)=='table','Malformed expedition plan')
  elseif envelope.type=='assemble_expedition' or envelope.type=='cancel_expedition' then U.integer(envelope.manifestId,'campaign manifest ID',1,100000000)
  elseif envelope.type=='unload_cargo' then assert(type(envelope.resource)=='string','Malformed cargo resource');U.integer(envelope.amount,'campaign unload amount',1,1000)
  else U.integer(envelope.operationId,'campaign cargo operation ID',1,100000000) end
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
   local valid,reason=require('src.logistics').valid(campaign,envelope);assert(valid,reason)
  else
   local site=Campaign.site(campaign,envelope.siteId)
   assert(site,'Unknown campaign site')
   assert(site.ownerSocietyId==campaign.society.id,'Site is not owned by this society')
   local valid,reason=Cmd.valid(site.world,envelope.payload)
   assert(valid,reason)
  end
 end)
 return ok,ok and nil or tostring(why)
end

function Command.apply(campaign,envelope)
 local structural,reason=Command.shape(envelope)
 if not structural then return false,reason end
 if envelope.scope=='campaign' then return require('src.logistics').apply(campaign,envelope) end
 local site=Campaign.site(campaign,envelope.siteId)
 if not site then return false,'Unknown campaign site' end
 if site.ownerSocietyId~=campaign.society.id then return false,'Site is not owned by this society' end
 local valid,why=Cmd.valid(site.world,envelope.payload)
 if not valid then
  W.event(site.world,'rejected',why)
  return false,why
 end
 local applied=Cmd.apply(site.world,envelope.payload)
 return applied,applied and nil or 'Site command was rejected'
end

return Command
