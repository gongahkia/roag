local U=require('src.util')
local W=require('src.world')
local Sim=require('src.sim')
local Cmd=require('src.commands')
local Codec=require('src.codec')
local Metrics=require('src.metrics')
local C=require('config')
local H={};H.__index=H
function H.new(w)
 if not w.baseline then w.baseline=Metrics.measure(w) end
 return setmetatable({live=w,view=w,initial=U.deep(w),commands={},checkpoints={},
  frontier=w.tick,seekTarget=nil,checkpointEvery=C.historyEvery,checkpointLimit=C.historyLimit},H)
end
function H:atPresent() return self.view==self.live and not self.seekTarget end
function H:queue(c)
 if not self:atPresent() then
  if self.live.mode=='challenge' then return false,'Archive is read-only. End returns to the present.' end
  if self.seekTarget then return false,'Finish seeking before branching' end
  self.live=U.deep(self.view);self.view=self.live;self.frontier=self.live.tick
  for tick in pairs(self.commands) do if tick>self.frontier then self.commands[tick]=nil end end
  local keep={} for _,cp in ipairs(self.checkpoints) do if cp.tick<=self.frontier then keep[#keep+1]=cp end end self.checkpoints=keep
 end
 if self.live.extinct and self.live.mode=='challenge' then return false,'Settlement extinct. Start a new expedition.' end
 local ok,why=Cmd.valid(self.live,c);if not ok then return false,why end
 local n=0 for _,commands in pairs(self.commands) do n=n+#commands end
 if n>=C.maxCommands then return false,'Command limit reached; this run can still simulate' end
 local tick=self.live.tick+1
 self.commands[tick]=self.commands[tick] or {};table.insert(self.commands[tick],U.deep(c))
 return true
end
function H:advance(clock)
 if not self:atPresent() then return false end
 local times=Sim.step(self.live,self.commands[self.live.tick+1],clock)
 self.frontier=self.live.tick
 if self.live.tick%self.checkpointEvery==0 then
  self.checkpoints[#self.checkpoints+1]={tick=self.live.tick,state=U.deep(self.live)}
  while #self.checkpoints>self.checkpointLimit do table.remove(self.checkpoints,1) end
 end
 return true,times
end
function H:seek(target)
 U.integer(target,'seek',self.initial.tick,self.frontier)
 if target==self.frontier then self.view=self.live;self.seekTarget=nil;return end
 local chosen=self.initial
 if self.view~=self.live and self.view.tick<=target then chosen=self.view end
 for _,cp in ipairs(self.checkpoints) do if cp.tick<=target and cp.tick>chosen.tick then chosen=cp.state end end
 self.view=U.deep(chosen);self.seekTarget=target
 if self.view.tick==target then self.seekTarget=nil end
end
function H:updateSeek(budget)
 local count=0
 while self.seekTarget and self.view.tick<self.seekTarget and count<(budget or 40) do
  Sim.step(self.view,self.commands[self.view.tick+1]);count=count+1
 end
 if self.seekTarget and self.view.tick>=self.seekTarget then self.seekTarget=nil end
 return count
end
function H:bundle()
 return {schema=1,version=C.saveVersion,initial=U.deep(self.initial),live=U.deep(self.live),commands=U.deep(self.commands)}
end
function H.restore(b)
 assert(type(b)=='table' and b.schema==1 and (b.version==C.saveVersion or b.version==C.stateVersion),'Incompatible save')
 W.validate(b.initial);W.validate(b.live)
 assert(b.initial.seed==b.live.seed and b.initial.mode==b.live.mode,'Timeline mismatch')
 assert(b.initial.tick<=b.live.tick and type(b.commands)=='table','Malformed history')
 local n=0
 for tick,commands in pairs(b.commands) do
  U.integer(tick,'command tick',1,b.live.tick+1);assert(type(commands)=='table','Malformed command list')
  n=n+#commands
  for _,c in ipairs(commands) do assert(type(c)=='table' and type(c.type)=='string','Malformed command') end
 end
 assert(n<=C.maxCommands,'Command limit')
 local h=H.new(U.deep(b.live));h.initial=U.deep(b.initial);h.commands=U.deep(b.commands)
 return h
end
function H:saveText() return Codec.encode(self:bundle()) end
function H.fromText(s) return H.restore(Codec.decode(s)) end
return H
