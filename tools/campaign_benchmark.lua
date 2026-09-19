-- Isolated CPU diagnostic for COS-P01. It writes no save or benchmark artifact.
local G=require('src.generate')
local Sim=require('src.sim')
local Metrics=require('src.metrics')
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local U=require('src.util')
local ticks=tonumber(arg and arg[1]) or 2000
U.integer(ticks,'campaign benchmark ticks',100,100000)

local function source()
 local world=G.make(12345,'cistern','challenge',128,80)
 world.baseline=Metrics.measure(world)
 return world
end

local function median(values)
 local copy=U.copy(values);table.sort(copy)
 return copy[math.floor((#copy+1)/2)]
end

local function localTrace()
 local world=source();collectgarbage('collect')
 local start=os.clock();for _=1,ticks do Sim.step(world) end;return os.clock()-start
end

local function campaignTrace()
 local campaign=Campaign.new(source());collectgarbage('collect')
 local start=os.clock();for _=1,ticks do Campaign.step(campaign) end;return os.clock()-start
end

local warmLocal,warmCampaign=localTrace(),campaignTrace()
local localTimes,campaignTimes={},{}
for sample=1,7 do
 if sample%2==1 then localTimes[sample]=localTrace();campaignTimes[sample]=campaignTrace()
 else campaignTimes[sample]=campaignTrace();localTimes[sample]=localTrace() end
end
local localMedian,campaignMedian=median(localTimes),median(campaignTimes)
local delta=(campaignMedian/localMedian-1)*100
print(string.format('Campaign simulation CPU: %d ticks, local median %.6f s, campaign median %.6f s, delta %.1f%%.',
 ticks,localMedian,campaignMedian,delta))
print('Local samples: '..table.concat(localTimes,', '))
print('Campaign samples: '..table.concat(campaignTimes,', '))
if delta>15 then print('INVESTIGATE: campaign wrapper exceeded the 15% median diagnostic guard.') end

local history=History.new(Campaign.new(source()));history.checkpointEvery=100
local start=os.clock();for _=1,600 do history:advance() end;local checkpointCpu=os.clock()-start
start=os.clock();local text=history:saveText();local saveCpu=os.clock()-start
print(string.format('Campaign persistence CPU: 600 history ticks with six checkpoints %.6f s; canonical encode %d bytes in %.6f s.',
 checkpointCpu,#text,saveCpu))
print('CPU-clock diagnostic only: excludes world construction, renderer/GPU, filesystem replacement and RSS.')
