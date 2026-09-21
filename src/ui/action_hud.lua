-- Contextual, presentation-only action choices. Commands remain validated when
-- queued and applied; this module never mutates a world or predicts a result.
local W=require('src.world')
local S=require('src.structures')
local Content=require('src.content')
local Body=require('src.body')
local Visibility=require('src.visibility')

local H={}

local function add(actions,id,label,hint)
 actions[#actions+1]={id=id,label=label,hint=hint}
end

local function addBlockActions(world,actions)
 add(actions,'build:dig','Delegate dig','Marks selected build blocks for ordinary excavation.')
 if not (world.frontier and world.frontier.safe_excavation==1) then add(actions,'build:ladder','Delegate ladder','Build from physically delivered materials.') end
 add(actions,'build:platform','Delegate floor','Build from physically delivered materials.')
 add(actions,'build:wall','Delegate wall','Build from physically delivered materials.')
 add(actions,'build:bed','Delegate bed','Build from physically delivered materials.')
 add(actions,'build:store','Delegate stockpile','Build from physically delivered materials.')
 add(actions,'build:farm','Delegate farm','Build from physically delivered materials.')
 add(actions,'build:pump','Delegate pump','Build from physically delivered materials.')
 if world.frontier and world.frontier.education==1 then add(actions,'build:field_school','Delegate field school','Requires four stone and two metal through ordinary construction.') end
 if world.frontier and world.frontier.equipment==1 then add(actions,'build:tool_bench','Delegate tool bench','Requires four stone and two metal through ordinary construction.') end
 if world.frontier and world.frontier.visibility==1 then add(actions,'build:torch','Delegate torch','Requires one metal; mount it on a floor or a solid wall face.') end
 if world.frontier and world.frontier.industry==1 then
  add(actions,'build:solar_array','Build small solar array','Two supported blocks; 2 stone, 4 metal and one component.')
  add(actions,'build:power_pole','Build power pole','One metal; connects nearby industry.')
  add(actions,'build:battery','Build battery','Stores local solar power.')
  add(actions,'build:fabricator','Build fabricator','Powered manufacturing with physical buffers.')
  add(actions,'build:mining_rig','Build mining rig','Powered designated excavation.')
  add(actions,'build:industrial_bin','Build industrial bin','Physical belt-accessible storage.')
  add(actions,'build:conveyor','Build conveyor','Floor-level physical item transport.')
 add(actions,'build:electric_lamp','Build electric lamp','Powered light without fuel.')
 if world.frontier and world.frontier.factions==1 then
   add(actions,'build:signal_relay','Build signal relay','Powered contact scanning; does not reveal terrain.')
   add(actions,'build:trade_depot','Build trade depot','Physical barter buffer for off-map courier trade.')
  end
 end
 if world.frontier and world.frontier.security==1 then
  add(actions,'build:training_target','Build training target','Two stone and one metal; Guards train here without ammunition.')
  add(actions,'build:barricade','Build barricade','Two stone; walkable lower-cell projectile cover.')
 end
 if world.frontier and world.frontier.safe_excavation==1 then
  add(actions,'rope:down','Unfurl rope downward','Fetches one real rope coil and deploys a climb lane below the selected anchor.')
  add(actions,'rope:up','Unfurl rope upward','Fetches one real rope coil and deploys a climb lane upward to climb from below.')
 end
end

local function selectedWorker(world,app)
 return app.selectedWorker and W.find(world.workers,app.selectedWorker) or nil
end

local function workerAt(world,cell)
 for _,worker in ipairs(world.workers) do
  if Body.contains(world,worker.x,worker.y,cell.x,cell.y) then return worker end
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
 if Visibility.enabled(world) and not Visibility.currentlyVisible(world,cell.x,cell.y,app.campaign and {campaign=app.history.view,siteId=app.siteId} or nil) then
  return {cell=cell,gx=W.tile(world,cell.x,cell.y),title='Unexplored',actions={}}
 end
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
  elseif structure.kind=='tool_bench' then add(actions,'fabricate:pickaxe','Fabricate pickaxe','Consumes two metal and 120 work actions.');add(actions,'fabricate:rope_coil','Fabricate rope coil','Consumes one metal and 60 work actions.');if world.frontier and world.frontier.industry==1 then add(actions,'fabricate:component','Fabricate machine component','Consumes two metal and 120 work actions.') end
  elseif structure.kind=='fabricator' then
   add(actions,'industry:recipe:component','Set component recipe','Consumes two metal in 40 powered ticks.')
   add(actions,'industry:recipe:pickaxe','Set pickaxe recipe','Consumes two metal in 50 powered ticks.')
   add(actions,'industry:recipe:rope_coil','Set rope coil recipe','Consumes one metal in 30 powered ticks.')
   if world.frontier and world.frontier.security==1 then
    add(actions,'industry:recipe:frontier_carbine','Set carbine recipe','Consumes two metal and one machine component in 180 powered ticks.')
    add(actions,'industry:recipe:shock_baton','Set baton recipe','Consumes one metal and one machine component in 100 powered ticks.')
    add(actions,'industry:recipe:protective_vest','Set vest recipe','Consumes two metal and one machine component in 150 powered ticks.')
    add(actions,'industry:recipe:ammunition','Set ammunition recipe','Consumes one metal and produces six physical rounds in 60 powered ticks.')
   end
   add(actions,'industry:priority','Cycle power priority','Sets local consumer priority from 1 through 3.')
  elseif structure.kind=='mining_rig' or structure.kind=='electric_lamp' then add(actions,'industry:priority','Cycle power priority','Sets local consumer priority from 1 through 3.')
  elseif structure.kind=='conveyor' then add(actions,'industry:direction','Rotate conveyor','Cycles north, east, south and west.')
  elseif structure.kind=='industrial_bin' then add(actions,'industry:direction','Rotate bin output','Cycles north, east, south and west.');add(actions,'industry:mode','Toggle bin mode','Switches between receive and supply.')
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
