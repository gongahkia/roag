-- Contextual, presentation-only action choices. Commands remain validated when
-- queued and applied; this module never mutates a world or predicts a result.
local W=require('src.world')
local S=require('src.structures')
local Content=require('src.content')

local H={}

local function add(actions,id,label,hint)
 actions[#actions+1]={id=id,label=label,hint=hint}
end

local function addBlockActions(world,actions)
 add(actions,'build:dig','Delegate dig','Marks selected build blocks for ordinary excavation.')
 add(actions,'build:ladder','Delegate ladder','Build from physically delivered materials.')
 add(actions,'build:platform','Delegate floor','Build from physically delivered materials.')
 add(actions,'build:wall','Delegate wall','Build from physically delivered materials.')
 add(actions,'build:bed','Delegate bed','Build from physically delivered materials.')
 add(actions,'build:store','Delegate stockpile','Build from physically delivered materials.')
 add(actions,'build:farm','Delegate farm','Build from physically delivered materials.')
 add(actions,'build:pump','Delegate pump','Build from physically delivered materials.')
 if world.frontier and world.frontier.education==1 then add(actions,'build:field_school','Delegate field school','Requires four stone and two metal through ordinary construction.') end
end

local function selectedWorker(world,app)
 return app.selectedWorker and W.find(world.workers,app.selectedWorker) or nil
end

local function workerAt(world,cell)
 for _,worker in ipairs(world.workers) do
  if math.abs(cell.x-worker.x)<=2 and cell.y>=worker.y-3 and cell.y<=worker.y+1 then return worker end
 end
end

local function openJob(world,gx,gy)
 for _,job in ipairs(world.jobs) do if job.gx==gx and job.gy==gy and job.state=='open' then return job end end
end

function H.model(app,world)
 local hud=app.hud
 if hud and hud.selection then
  local selection=hud.selection;local count=selection.count or (selection.gx2-selection.gx1+1)*(selection.gy2-selection.gy1+1)
  local actions={};addBlockActions(world,actions)
  add(actions,'cancel','Cancel delegated orders','Cancels open orders in the selected blocks safely.')
  return {area=true,selection=selection,count=count,limit=selection.limit or 256,cell=hud.cell,gx=selection.gx1,gy=selection.gy1,title=count..' selected blocks',actions=actions}
 end
 local cell=hud and hud.cell or app.selectedCell
 if not cell then return nil end
 local gx,gy=W.tile(world,cell.x,cell.y)
 local worker=selectedWorker(world,app) or workerAt(world,cell)
 local structure=W.structureAt(world,cell.x,cell.y)
 local encounter,category=Content.at(world,cell.x,cell.y)
 local job=openJob(world,gx,gy)
 local actions={}

 if worker then
  add(actions,'delegate_worker',app.orderWorker==worker.id and 'Release worker delegation' or ('Delegate orders to '..worker.name),'New block actions use this worker when possible.')
  add(actions,'rally_worker','Rally '..worker.name..' here','Normal hunger, fatigue and hazards still interrupt a rally.')
  if worker.directive then add(actions,'release_rally','Release rally hold','Return this worker to ordinary scheduling.') end
 end

 if encounter then
  add(actions,'field:survey','Delegate survey','A reachable field worker identifies this encounter.')
  if category=='flora' and world.frontier and world.frontier.knowledge==1 then add(actions,'field:study','Delegate field study','Requires this worker\'s firsthand evidence.') end
  if category=='fauna' then add(actions,'field:cull','Delegate cull','A field worker pursues this creature.') else add(actions,'field:salvage','Delegate salvage','A field worker recovers this encounter.') end
 end

 if structure then
  local label=S.def[structure.kind].label
  if structure.kind=='field_school' and structure.education then add(actions,'school','Open '..label,'Configure records, teaching, or record study.')
  elseif structure.kind=='pump' then
   add(actions,'pump:intake','Delegate pump intake','Choose the next material cell as the intake.')
   add(actions,'pump:outlet','Delegate pump outlet','Choose the next material cell as the outlet.')
  elseif structure.kind=='charge' and not structure.fuseAt then add(actions,'arm','Delegate charge arming','Arming starts an irreversible fuse after real work.')
  elseif structure.kind=='ward' then add(actions,'toggle','Toggle ward','Changes only the selected installed ward.') end
  add(actions,'remove','Delegate dismantle '..label,'Uses the ordinary removal job and recovery rules.')
 else
  addBlockActions(world,actions)
  if world.mode=='practice' then add(actions,'paint','Practice brush','Places only the current practice material.') end
 end

 if job then
  add(actions,'cancel','Cancel delegated order','Cancels the selected block\'s open job safely.')
  add(actions,'priority:down','Lower order priority','Changes the selected open job to the next lower priority.')
  add(actions,'priority:up','Raise order priority','Changes the selected open job to the next higher priority.')
 end

 local title=worker and worker.name or structure and S.def[structure.kind].label or encounter and 'Encounter' or 'Selected block'
 return {cell=cell,gx=gx,gy=gy,worker=worker,structure=structure,encounter=encounter,category=category,job=job,title=title,actions=actions}
end

return H
