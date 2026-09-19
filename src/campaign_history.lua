-- One command log and archive cursor for the entire campaign, never per site.
local U=require('src.util')
local Campaign=require('src.campaign')
local Command=require('src.campaign_commands')
local Codec=require('src.campaign_codec')
local C=require('config')
local H={};H.__index=H

local function commandCount(commands)
 local total=0
 for _,tick in ipairs(U.keys(commands)) do
  local list=commands[tick]
  for _ in ipairs(list) do total=total+1 end
 end
 return total
end

local function commandList(commands,tick)
 local list=commands[tick]
 if not list then list={};commands[tick]=list end
 return list
end

function H.new(campaign)
 Campaign.validate(campaign)
 local live=Campaign.clone(campaign)
 return setmetatable({live=live,view=live,initial=Campaign.clone(live),commands={},checkpoints={},
  frontier=live.tick,seekTarget=nil,checkpointEvery=C.historyEvery,checkpointLimit=8},H)
end

function H:atPresent() return self.view==self.live and not self.seekTarget end

function H:queue(envelope)
 if not self:atPresent() then
  if self.live.mode=='challenge' then return false,'Archive is read-only. End returns to the present.' end
  if self.seekTarget then return false,'Finish seeking before branching' end
  self.live=Campaign.clone(self.view);self.view=self.live;self.frontier=self.live.tick
  for _,tick in ipairs(U.keys(self.commands)) do if tick>self.frontier then self.commands[tick]=nil end end
  local kept={}
  for _,checkpoint in ipairs(self.checkpoints) do
   if checkpoint.tick<=self.frontier then kept[#kept+1]=checkpoint end
  end
  self.checkpoints=kept
 end
 if Campaign.extinct(self.live) and self.live.mode=='challenge' then return false,'Campaign extinct. Start a new campaign.' end
 local ok,why=Command.valid(self.live,envelope);if not ok then return false,why end
 if commandCount(self.commands)>=C.maxCommands then return false,'Command limit reached; this campaign can still simulate' end
 local list=commandList(self.commands,self.live.tick+1)
 list[#list+1]=U.deep(envelope)
 return true
end

function H:advance(clock)
 if not self:atPresent() then return false end
 local timings=Campaign.step(self.live,self.commands[self.live.tick+1],clock)
 self.frontier=self.live.tick
 if self.live.tick%self.checkpointEvery==0 then
  self.checkpoints[#self.checkpoints+1]={tick=self.live.tick,state=Campaign.clone(self.live)}
  while #self.checkpoints>self.checkpointLimit do table.remove(self.checkpoints,1) end
 end
 return true,timings
end

function H:seek(target)
 U.integer(target,'campaign seek',self.initial.tick,self.frontier)
 if target==self.frontier then self.view=self.live;self.seekTarget=nil;return end
 local chosen=self.initial
 if self.view~=self.live and self.view.tick<=target then chosen=self.view end
 for _,checkpoint in ipairs(self.checkpoints) do
  if checkpoint.tick<=target and checkpoint.tick>chosen.tick then chosen=checkpoint.state end
 end
 self.view=Campaign.clone(chosen);self.seekTarget=target
 if self.view.tick==target then self.seekTarget=nil end
end

function H:updateSeek(budget)
 U.integer(budget or 40,'campaign seek budget',1,100000)
 local count=0
 while self.seekTarget and self.view.tick<self.seekTarget and count<(budget or 40) do
  Campaign.step(self.view,self.commands[self.view.tick+1]);count=count+1
 end
 if self.seekTarget and self.view.tick>=self.seekTarget then self.seekTarget=nil end
 return count
end

function H:bundle()
 return {format='cosmonauts-campaign',version=1,initial=Campaign.clone(self.initial),
  live=Campaign.clone(self.live),commands=U.deep(self.commands)}
end

local function validateCommands(bundle)
 assert(type(bundle.commands)=='table','Malformed campaign command history')
 local count=0
 for _,tick in ipairs(U.keys(bundle.commands)) do
  U.integer(tick,'campaign command tick',1,bundle.live.tick+1)
  local list=bundle.commands[tick]
  assert(type(list)=='table','Malformed campaign command list')
  local entries,max=0,0
  for key in pairs(list) do
   U.integer(key,'campaign command index',1,C.maxCommands);entries=entries+1
   if key>max then max=key end
  end
  assert(entries==max,'Campaign command list has a hole')
  for index=1,entries do
   local ok,why=Command.shape(list[index]);assert(ok,why)
   count=count+1;assert(count<=C.maxCommands,'Campaign command limit')
  end
 end
end

function H.restore(bundle)
 assert(type(bundle)=='table','Malformed campaign save')
 for key in pairs(bundle) do assert(key=='format' or key=='version' or key=='initial' or key=='live' or key=='commands','Unknown campaign save key') end
 assert(bundle.format=='cosmonauts-campaign' and bundle.version==1,'Incompatible campaign save')
 assert(bundle.initial~=nil and bundle.live~=nil and bundle.commands~=nil,'Incomplete campaign save')
 Campaign.validate(bundle.initial);Campaign.validate(bundle.live)
 assert(bundle.initial.ruleset==bundle.live.ruleset and bundle.initial.mode==bundle.live.mode
  and bundle.initial.seed==bundle.live.seed and Codec.encode(bundle.initial.features)==Codec.encode(bundle.live.features),
  'Campaign timeline mismatch')
 assert(bundle.initial.tick<=bundle.live.tick,'Malformed campaign timeline')
 validateCommands(bundle)
 local history=H.new(bundle.live)
 history.initial=Campaign.clone(bundle.initial);history.commands=U.deep(bundle.commands)
 return history
end

function H:saveText() return Codec.encode(self:bundle()) end
function H.fromText(text) return H.restore(Codec.decode(text)) end

function H:beginVerify()
 self._verify={campaign=Campaign.clone(self.initial),target=self.live.tick}
 return {tick=self._verify.campaign.tick,target=self._verify.target}
end

function H:updateVerify(budget)
 U.integer(budget or 40,'campaign verification budget',1,100000)
 if not self._verify then self:beginVerify() end
 local verify=self._verify
 local count=0
 while verify.campaign.tick<verify.target and count<(budget or 40) do
  Campaign.step(verify.campaign,self.commands[verify.campaign.tick+1]);count=count+1
 end
 local progress={ticks=count,tick=verify.campaign.tick,target=verify.target,done=verify.campaign.tick==verify.target}
 if not progress.done then return nil,'Campaign replay verification in progress',progress end
 local same=Codec.encode(verify.campaign)==Codec.encode(self.live)
 self._verify=nil
 if not same then return false,'Campaign replay differs from encoded live state',progress end
 return true,nil,progress
end

function H:verifyReplay(budget)
 if not self._verify then self:beginVerify() end
 return self:updateVerify(budget or 1000)
end

return H
