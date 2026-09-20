local C=require('config')
local G=require('src.generate')
local W=require('src.world')
local H=require('src.history')
local Campaign=require('src.campaign')
local CampaignHistory=require('src.campaign_history')
local R=require('src.render')
local Store=require('src.storage')
local Metrics=require('src.metrics')
local U=require('src.util')
local Bench=require('src.benchmark')
local M=require('src.materials')
local Map=require('src.mapfile')
local MapStore=require('src.mapstore')
local Report=require('src.generation.report')
local Layouts=require('src.generation.layouts')
local Biomes=require('src.biomes')
local Crew=require('src.ui.crew')
local Content=require('src.content')
local app={paused=true,speed=1,tool='inspect',priority=2,view=1,grid=false,accumulator=0}
local renderer
local function notify(s) app.toast=tostring(s);app.toastTime=8 end
local function currentWorld(history)
 history=history or app.history
 if app.campaign then
  local site=Campaign.site(history.view,app.siteId)
  if history.view.features.region==1 and (not site or site.ownerSocietyId~=history.view.society.id) then
   for _,candidate in ipairs(Campaign.sites(history.view)) do
    if candidate.ownerSocietyId==history.view.society.id then app.siteId=candidate.id;site=candidate;break end
   end
  end
  assert(site,'Selected campaign site is unavailable')
  return site.world
 end
 return history.view
end
local function liveWorld()
 if app.campaign then return assert(Campaign.site(app.history.live,app.siteId)).world end
 return app.history.live
end
local function initialWorld()
 if app.campaign then return assert(Campaign.site(app.history.initial,app.siteId)).world end
 return app.history.initial
end
local function localCommand(payload)
 if app.campaign then return {scope='site',siteId=app.siteId,payload=payload} end
 return payload
end
app.currentWorld=currentWorld
app.liveWorld=liveWorld
app.initialWorld=initialWorld
local function save()
 if app.saveBlocked then notify('Existing unreadable save preserved; start a new expedition explicitly.') return false end
 local ok,why=(app.campaign and Store.saveCampaign or Store.save)(app.history)
 if not ok then notify('SAVE FAILED: '..tostring(why)) else app.savedTick=app.history.live.tick end
 return ok
end
local function queue(c)
 local ok,why=app.history:queue(localCommand(c))
 if not ok then notify(why) else notify(app.paused and 'Order queued. Space runs; Right advances one tick.' or 'Order issued.') end
 return ok
end
local function archive()
 assert(love.filesystem.createDirectory('archives'),'Could not create archive directory')
 local base='archives/run-'..app.history.live.seed..'-'..os.time()
 local name=base..'.dw';local counter=1
 while love.filesystem.getInfo(name) do name=base..'-'..counter..'.dw';counter=counter+1 end
 -- Preserve an unreadable original verbatim rather than silently replacing it.
 local text
 if app.saveBlocked then text=assert(love.filesystem.read('run.dat')) else text=app.history:saveText() end
 assert(love.filesystem.write(name,text),'Could not archive current run')
end
local function previewNew()
 local n=app.newRun
 if n.imported then return true end
 local seed=tonumber(n.seed)
 if not seed or seed<0 or seed>2147483646 or seed%1~=0 then n.error='Seed must be an integer from 0 to 2147483646';return false end
 local start=love.timer.getTime()
 local ok,result=pcall(function()
  local w=G.make(seed,n.preset,n.mode,n.width,n.height,{layout=n.layout,climate=n.climate,openness=n.openness,biomeScale=n.biomeScale,features=n.features,density=n.density,crew=n.crew})
  local generationMs=(love.timer.getTime()-start)*1000
  return {world=w,map=Map.fromWorld(w),report=Report.measure(w),generationMs=generationMs}
 end)
 if not ok then n.error=tostring(result);return false end
 n.preview=result;n.error=nil;return true
end
local function startNew()
 local n=app.newRun
 -- First Enter previews; a second Enter explicitly commits that displayed map.
 if not n.preview then previewNew();return end
 n.preview.world.mode=n.mode
 local ok,result=pcall(function()
  local nextHistory=H.new(n.preview.world)
  if not app.campaign then archive() end
  local saved,why=Store.save(nextHistory);assert(saved,why)
  return nextHistory
 end)
 if not ok then n.error='Previous colony retained: '..tostring(result);return end
 app.history=result;app.campaign=false;app.siteId=nil;app.newRun=nil;app.mapBrowser=nil;app.paused=true;app.accumulator=0;app.saveBlocked=false
 app.selectedWorker=nil;app.selectedCell=nil;app.port=nil;app.drag=nil;app.benchmark=nil;app.stepBudget=0
 app.crew=nil;app.fieldnotes=nil;app.orderWorker=nil;app.rallyWorker=nil;app.panning=false
 renderer.zoom,renderer.panX,renderer.panY=1,0,0
 notify('New '..result.live.mode..' expedition. The previous run is archived. F7 shows biomes.')
end
local function campaignOptions(n)
 return {preset=n.preset,mode=n.mode,width=n.width,height=n.height,layout=n.layout,climate=n.climate,
  openness=n.openness,biomeScale=n.biomeScale,features=n.features,density=n.density,crew=n.crew}
end
local function startCampaign()
 local n=app.newRun;local seed=tonumber(n.seed)
 if not seed or seed<1 or seed>2147483646 or seed%1~=0 then n.error='Campaign seed must be an integer from 1 to 2147483646';return end
 if not n.campaignCandidate then
  local ok,result=pcall(function() return CampaignHistory.new(Campaign.newRegion(seed,campaignOptions(n))) end)
  if not ok then n.error='Previous session retained: '..tostring(result);return end
  n.campaignCandidate=result;n.error='Campaign generated and validated. Enter confirms the new frontier campaign.';return
 end
 local ok,why=Store.saveCampaign(n.campaignCandidate)
 if not ok then n.error='Previous session retained: '..tostring(why);return end
 app.history=n.campaignCandidate;app.campaign=true;app.siteId=1;app.newRun=nil;app.mapBrowser=nil;app.paused=true;app.accumulator=0;app.saveBlocked=false
 app.selectedWorker=nil;app.selectedCell=nil;app.port=nil;app.drag=nil;app.benchmark=nil;app.stepBudget=0;app.crew=nil;app.fieldnotes=nil;app.orderWorker=nil;app.rallyWorker=nil;app.panning=false
 renderer.zoom,renderer.panX,renderer.panY=1,0,0
 notify('New frontier campaign. Home Planet is active; moon transport is pending.')
end
local function continueCampaign()
 local n=app.newRun;local history,why=Store.loadCampaign()
 if not history then n.error=why or 'No campaign save.';return end
 app.history=history;app.campaign=true;app.siteId=1;app.newRun=nil;app.mapBrowser=nil;app.paused=true;app.accumulator=0;app.saveBlocked=false
 app.selectedWorker=nil;app.selectedCell=nil;app.port=nil;app.drag=nil;app.benchmark=nil;app.stepBudget=0;app.crew=nil;app.fieldnotes=nil;app.orderWorker=nil;app.rallyWorker=nil;app.panning=false
 renderer.zoom,renderer.panX,renderer.panY=1,0,0
 notify('Frontier campaign restored. Structural load did not replay its history.')
end
local function openImported(d,label)
 local world=Map.toWorld(d,'challenge')
 app.paused=true;app.accumulator=0;app.stepBudget=0;app.drag=nil;app.port=nil;app.benchmark=nil;app.help=false
 app.crew=nil;app.fieldnotes=nil
 app.newRun={seed=tostring(d.seed),preset=d.preset,mode='challenge',width=d.width,height=d.height,
  imported=true,source=label,preview={world=world,map=d,report=Report.measure(world)},view=1}
 app.mapBrowser=nil
end
local function openBrowser()
 app.paused=true;app.accumulator=0;app.stepBudget=0;app.drag=nil;app.benchmark=nil
 local ok,list=pcall(MapStore.list)
 app.mapBrowser={entries=ok and list or {},index=1,error=not ok and tostring(list) or nil}
end
local function exportMap()
 local d,scope
 if app.newRun and app.newRun.preview then d=app.newRun.preview.map;scope='preview'
 elseif love.keyboard.isDown('lshift','rshift') then
  d=Map.fromWorld(currentWorld());scope='viewed terrain ONLY; no people or structures'
 else d=Map.fromWorld(initialWorld());scope='initial terrain' end
 local path=MapStore.write(d)
 notify('Exported '..scope..': '..path)
 if app.newRun then app.newRun.error='Exported: '..path end
end
local function safeExportMap()
 local ok,err=pcall(exportMap);if not ok then
  notify('Map export failed: '..tostring(err));if app.newRun then app.newRun.error=tostring(err) end
 end
end
function love.filedropped(file)
 local ok,d=pcall(MapStore.dropped,file)
 if ok then
  local opened,why=pcall(openImported,d,'Dropped map')
  if not opened then notify('Map rejected; current colony unchanged: '..tostring(why)) end
 else notify('Map rejected; current colony unchanged: '..tostring(d)) end
end
function love.load()
 renderer=R.new()
 local h,err=Store.load()
 app.history=h or H.new(G.make(C.seed,C.preset,C.mode));app.campaign=false;app.siteId=nil
 if err then app.saveBlocked=true;notify(err)
 elseif not h then app.help=true;notify('New expedition. F1 closes the field manual.')
 else notify('Resumed the present. Archive viewing cannot resurrect a challenge settlement.') end
 love.keyboard.setKeyRepeat(true)
 renderer:layout(app);renderer:mapRect(currentWorld())
end
local function export()
 love.filesystem.createDirectory('exports')
 local w=currentWorld();local m=Metrics.measure(w);local stamp=os.time()
 local rows={U.csv({'metric','value'})}
 for _,k in ipairs(U.keys(m)) do if type(m[k])~='table' then rows[#rows+1]=U.csv({k,m[k]}) end end
 rows[#rows+1]=U.csv({'seed',w.seed});rows[#rows+1]=U.csv({'tick',w.tick});rows[#rows+1]=U.csv({'version',C.version})
 local path='exports/metrics-'..stamp..'.csv'
 assert(love.filesystem.write(path,table.concat(rows,'\n')))
 assert(love.filesystem.write('exports/geology-'..stamp..'.csv',Report.csv(Report.measure(w))))
 local log={U.csv({'id','tick','kind','text'})}
 for _,e in ipairs(w.events) do log[#log+1]=U.csv({e.id,e.tick,e.kind,e.text}) end
 assert(love.filesystem.write('exports/chronicle-'..stamp..'.csv',table.concat(log,'\n')))
 assert(love.filesystem.write('exports/replay-'..stamp..(app.campaign and '.campaign' or '.dw'),app.history:saveText()))
 local runtime=_VERSION..(jit and (' / '..jit.version) or '')
 local loveVersion='unavailable in test adapter'
 if love.getVersion then local major,minor,revision=love.getVersion();loveVersion=major..'.'..minor..'.'..revision end
 local info={C.title..' '..C.version,'Lua: '..runtime,'LOVE: '..loveVersion,
  'Seed: '..w.seed,'Preset: '..w.preset,'Mode: '..w.mode,'Selected tick: '..w.tick,
  'Live frontier: '..app.history.frontier,
  'Generation: '..(w.generation and w.generation.version..' / '..w.generation.layout..' / '..w.generation.climate or 'legacy'),
  'Replay bundle always retains the live frontier plus its full command history.',
  'Metrics describe the selected archive position; chronicle contains the latest 240 events at that position.'}
 assert(love.filesystem.write('exports/context-'..stamp..'.txt',table.concat(info,'\n')))
 notify('Exported to '..love.filesystem.getSaveDirectory()..'/exports')
end
function love.update(dt)
 if app.toastTime then app.toastTime=app.toastTime-dt;if app.toastTime<=0 then app.toast=nil;app.toastTime=nil end end
 if app.help or app.newRun or app.mapBrowser or app.crew or app.fieldnotes then return end
 local h=app.history
 if app.benchmark then
  local ok,message,manifest=coroutine.resume(app.benchmark)
  if not ok then notify('Benchmark failed: '..tostring(message));app.benchmark=nil
  elseif coroutine.status(app.benchmark)=='dead' then
   love.filesystem.createDirectory('exports');local stamp=os.time()
   local good,why=love.filesystem.write('exports/benchmark-'..stamp..'.csv',message)
   love.filesystem.write('exports/benchmark-'..stamp..'.txt',manifest..'Clock: love.timer.getTime (elapsed seconds)\n')
   app.benchmark=nil;notify(good and 'Benchmark exported. F6 exports the live colony metrics.' or why)
  else app.benchmarkStatus=message end
  return
 end
 if h.seekTarget then
  local start=love.timer.getTime()
  repeat h:updateSeek(1) until not h.seekTarget or love.timer.getTime()-start>0.010
  return
 end
 if not h:atPresent() then return end
 if not app.paused then app.accumulator=app.accumulator+math.min(dt,0.1)*C.ticksPerSecond*app.speed end
 local requests=math.floor(app.accumulator)+(app.stepBudget or 0)
 local steps=math.min(requests,C.maxStepsPerFrame)
 for _=1,steps do
  if app.stepBudget and app.stepBudget>0 then app.stepBudget=app.stepBudget-1 else app.accumulator=app.accumulator-1 end
  local alive=app.campaign and not Campaign.extinct(h.live) or W.alive(h.live)>0;local start=love.timer.getTime()
  h:advance(love.timer.getTime);app.tickMs=(love.timer.getTime()-start)*1000
  local stillAlive=app.campaign and not Campaign.extinct(h.live) or W.alive(h.live)>0
  if alive and not stillAlive then
   app.paused=true;app.accumulator=0;app.stepBudget=0;save();notify('A settler died. Loss committed; inspect the chronicle.');break
  end
  if h.live.tick%C.autosaveEvery==0 then save() end
 end
 -- Avoid an unbounded wall-clock backlog; never increase the model step.
 app.accumulator=math.min(app.accumulator,C.maxStepsPerFrame)
end
function love.draw() renderer:draw(app) end
function love.textinput(s)
 if app.newRun and not app.mapBrowser and not app.newRun.imported and s:match('^%d+$') and #app.newRun.seed+#s<=10 then app.newRun.seed=app.newRun.seed..s;app.newRun.preview=nil;app.newRun.campaignCandidate=nil end
end
local hotkeys={q='inspect',d='dig',l='ladder',f='platform',w='wall',b='bed',s='store',c='farm',p='pump',x='remove',e='cancel',a='charge',f9='ward',u='survey',z='salvage',k='cull',m='rally'}
local function hasRegion()
 return app.campaign and app.history and app.history.view.features.region==1
end
local function selectSite(siteId)
 if not hasRegion() then return false end
 local site=Campaign.site(app.history.view,siteId)
 if not site or site.ownerSocietyId~=app.history.view.society.id then return false end
 app.siteId=siteId;app.drag=nil;app.port=nil;app.selectedWorker=nil;app.selectedCell=nil;app.hover=nil;app.orderWorker=nil;app.rallyWorker=nil
 renderer.zoom,renderer.panX,renderer.panY=1,0,0
 return true
end
local function toggleRegion()
 if not hasRegion() then return false end
 app.region=not app.region;app.drag=nil;app.port=nil
 return true
end
function love.keypressed(key)
 if app.crew then Crew.key(app,key,queue);return end
 if app.fieldnotes then
  if key=='escape' or key=='f4' then app.fieldnotes=nil
  elseif key=='up' then app.fieldnotes.scroll=math.max(1,app.fieldnotes.scroll-1)
  elseif key=='down' then app.fieldnotes.scroll=app.fieldnotes.scroll+1 end
  return
 end
 if app.region then
  if key=='escape' or (key=='f7' and love.keyboard.isDown('lshift','rshift')) then app.region=nil
  elseif key=='space' then
   if app.history:atPresent() then app.paused=not app.paused;app.accumulator=0 else notify('Archive is inspection-only during playback. End returns to the live frontier.') end
  elseif key=='up' or key=='down' then
   local sites=Campaign.sites(app.history.view);local owned={};for _,site in ipairs(sites) do if site.ownerSocietyId==app.history.view.society.id then owned[#owned+1]=site.id end end
   if #owned>0 then local at=1;for i,id in ipairs(owned) do if id==app.siteId then at=i end end;selectSite(owned[(at-1+(key=='up' and -1 or 1))%#owned+1]) end
  end
  return
 end
 if app.help then if key=='f1' or key=='escape' then app.help=false end return end
 if app.mapBrowser then
  local b=app.mapBrowser
  if key=='escape' then app.mapBrowser=nil
  elseif key=='up' then b.index=math.max(1,b.index-1)
  elseif key=='down' then b.index=math.min(math.max(1,#b.entries),b.index+1)
  elseif (key=='return' or key=='kpenter') and b.entries[b.index] then
   local path=b.entries[b.index]
   local ok,err=pcall(function() openImported(MapStore.read(path),path) end)
   if not ok then b.error=tostring(err) end
  end
  return
 end
 if app.newRun then
  local n=app.newRun
  local function cycle(values,value,delta)
   local at=1;for i,v in ipairs(values) do if v==value then at=i end end
   return values[(at-1+delta)%#values+1]
  end
  if key=='escape' then app.newRun=nil
  elseif key=='return' or key=='kpenter' then
   if n.action=='campaign_new' then startCampaign() elseif n.action=='campaign_continue' then continueCampaign() else startNew() end
  elseif key=='f2' then if n.preview then safeExportMap() else n.error='Generate a preview before exporting.' end
  elseif key=='f3' then openBrowser()
  elseif key=='v' then n.view=n.view==4 and 1 or 4
  elseif key=='c' and not n.imported then
   n.action=({local_run='campaign_new',campaign_new='campaign_continue',campaign_continue='local_run'})[n.action or 'local_run'];n.campaignCandidate=nil;n.error=nil
  elseif key=='tab' then
   n.mode=n.mode=='challenge' and 'practice' or 'challenge'
   if n.preview then n.preview.world.mode=n.mode end;n.campaignCandidate=nil
  elseif not n.imported then
   if key=='space' then previewNew()
   elseif key=='backspace' then n.seed=n.seed:sub(1,-2);n.preview=nil;n.campaignCandidate=nil
   elseif key=='left' or key=='right' then n.preset=cycle(G.presets,n.preset,key=='left' and -1 or 1);n.preview=nil;n.campaignCandidate=nil
   elseif key=='up' or key=='down' then n.layout=cycle(Layouts.names,n.layout,key=='up' and -1 or 1);n.preview=nil;n.campaignCandidate=nil
   elseif key=='b' then n.climate=cycle(Biomes.climates,n.climate,1);n.preview=nil;n.campaignCandidate=nil
   elseif key=='s' then
    local sizes={{128,80},{192,112},{256,160},{384,224},{512,256}};local at=1
    for i,size in ipairs(sizes) do if size[1]==n.width and size[2]==n.height then at=i end end
    local size=sizes[at%#sizes+1];n.width,n.height=size[1],size[2];n.preview=nil;n.campaignCandidate=nil
   elseif key=='o' then n.openness=cycle({0.35,0.48,0.60},n.openness,1);n.preview=nil;n.campaignCandidate=nil
   elseif key=='g' then n.biomeScale=cycle({0.75,1,1.4},n.biomeScale,1);n.preview=nil;n.campaignCandidate=nil
   elseif key=='f' then n.features=cycle({'living','ruins','none'},n.features,1);n.preview=nil;n.campaignCandidate=nil
   elseif key=='x' then n.density=cycle({0.6,1,1.4},n.density,1);n.preview=nil;n.campaignCandidate=nil
   elseif key=='k' then n.crew=cycle({3,6,9},n.crew,1);n.preview=nil;n.campaignCandidate=nil end
  end
  return
 end
 local h=app.history
 if key=='escape' then app.benchmark=nil;app.port=nil;app.drag=nil;app.tool='inspect'
 elseif key=='f1' then app.help=true;app.paused=true
 elseif key=='h' then Crew.open(app)
 elseif key=='f4' then app.fieldnotes={scroll=1};app.paused=true;app.accumulator=0;app.stepBudget=0;app.drag=nil
 elseif key=='y' then
  if love.keyboard.isDown('lshift','rshift') then
   local cell=app.selectedCell or app.hover
   if cell then local gx,gy=W.tile(currentWorld(),cell.x,cell.y);queue({type='target_order',gx=gx,gy=gy,worker=app.orderWorker or app.selectedWorker or 0}) end
  else
   app.orderWorker=app.selectedWorker~=app.orderWorker and app.selectedWorker or nil
   local a=app.orderWorker and W.find(currentWorld().workers,app.orderWorker)
   notify(a and ('New orders reserved for '..a.name..'. Y clears; Shift+Y reassigns a block.') or 'New orders are available to any eligible worker.')
  end
 elseif key=='j' then queue({type='releaserally',worker=love.keyboard.isDown('lshift','rshift') and 0 or app.selectedWorker or 0})
 elseif key=='space' then
  if h:atPresent() then app.paused=not app.paused;app.accumulator=0
  else notify('Archive is inspection-only during playback. End returns to the live frontier.') end
 elseif key=='1' or key=='2' or key=='3' then app.speed=({['1']=1,['2']=2,['3']=4})[key]
 elseif key=='n' then
  app.paused=true;app.accumulator=0;app.stepBudget=0;app.drag=nil
  local g=liveWorld().generation or {}
  -- 'frontier' is selected so existing colonies can discover the new generation lab.
  app.newRun={seed=tostring((h.live.seed+1)%2147483647),preset='frontier',mode=h.live.mode,
   width=C.width,height=C.height,layout=g.layout or C.layout,climate=g.climate or C.climate,
   openness=g.openness or C.openness,biomeScale=g.biomeScale or C.biomeScale,features=g.features or C.features,density=g.density or C.density,crew=g.crew or C.crew,view=1,action='local_run'}
 elseif key=='home' or key=='end' then app.paused=true;app.accumulator=0;app.stepBudget=0;h:seek(key=='home' and h.initial.tick or h.frontier)
 elseif key=='left' or key=='right' then
  app.paused=true;app.accumulator=0;local n=(love.keyboard.isDown('lshift','rshift') and 20 or 1)
  if key=='right' and h:atPresent() then app.stepBudget=(app.stepBudget or 0)+n
  else app.stepBudget=0;h:seek(U.clamp(h.view.tick+(key=='left' and -n or n),h.initial.tick,h.frontier)) end
 elseif key=='tab' then app.view=app.view%4+1
 elseif key=='f7' and love.keyboard.isDown('lshift','rshift') then
  if app.benchmark then notify('Close the benchmark before opening Region.') elseif not toggleRegion() then notify('Region view is available in frontier campaigns.') end
 elseif key=='f7' then app.view=app.view==4 and 1 or 4
 elseif key=='f2' then safeExportMap()
 elseif key=='f3' then openBrowser()
 elseif key=='g' then app.grid=not app.grid
 elseif key=='r' then renderer.zoom,renderer.panX,renderer.panY=1,0,0
 elseif key=='=' or key=='+' or key=='-' then
  app.priority=U.clamp(app.priority+(key=='-' and -1 or 1),1,3)
  local cell=app.selectedCell or app.hover
  if cell then local gx,gy=W.tile(currentWorld(),cell.x,cell.y);queue({type='priority',gx=gx,gy=gy,value=app.priority}) end
  notify('Order priority '..app.priority)
 elseif key=='i' or key=='o' or key=='t' then
  local cell=app.selectedCell
  local w=currentWorld();local s=cell and W.structureAt(w,cell.x,cell.y)
  if s and s.kind=='charge' and key=='t' then queue({type='arm',slot=W.slot(w,s.gx,s.gy),worker=app.orderWorker or 0,priority=app.priority})
  elseif s and s.kind=='ward' and key=='t' then queue({type='toggle',gx=s.gx,gy=s.gy})
  elseif s and s.kind=='pump' then
   if key=='t' then queue({type='toggle',gx=s.gx,gy=s.gy})
   else app.port=key=='i' and 'intake' or 'outlet';app.portSlot=W.slot(w,s.gx,s.gy) end
  else notify('Select a completed pump; T also operates a ward or orders charge arming.') end
 elseif key=='v' then
  if liveWorld().mode~='practice' then notify('Material brushes are practice-only.')
  else app.brush=(app.brush or 3)+1;if app.brush>9 then app.brush=0 end;if app.brush==1 then app.brush=2 end
   app.tool='paint';notify('Practice brush: '..M.def[app.brush].name) end
 elseif key=='f5' then if save() then notify('Saved live frontier (not archive position).') end
 elseif key=='f6' then local ok,err=pcall(export);if not ok then notify(err) end
 elseif key=='f8' then
  app.paused=true
  app.benchmark=love.keyboard.isDown('lshift','rshift') and require('src.generation.benchmark').create(love.timer.getTime) or Bench.create(love.timer.getTime)
 elseif key=='f10' then
  app.paused=true;local ok,result=pcall(function() return require('tests.all').run() end)
  notify(ok and ('Core tests passed: '..result.groups..' groups.') or ('TEST FAILED: '..tostring(result)))
 elseif key=='f12' then
  love.filesystem.createDirectory('exports');love.graphics.captureScreenshot('exports/screenshot-'..os.time()..'.png');notify('Screenshot saved in exports.')
 elseif hotkeys[key] then
  app.tool=hotkeys[key];app.port=nil
  if app.tool=='rally' then app.rallyWorker=love.keyboard.isDown('lshift','rshift') and 0 or app.selectedWorker or 0;notify(app.rallyWorker==0 and 'Click a rally point for ALL workers. J releases.' or 'Click a rally point for the selected worker. J releases.') end
 end
end
local function contains(b,x,y) return x>=b.x and y>=b.y and x<=b.x+b.w and y<=b.y+b.h end
function love.mousepressed(mx,my,button)
 if app.crew then if button==1 then Crew.mouse(app,mx,my,queue) end return end
 if app.region then
  if button==1 then
   for _,b in ipairs(app.regionButtons or {}) do
    if contains(b,mx,my) then
     if b.action=='close' then app.region=nil elseif b.action=='site' then selectSite(b.siteId) elseif b.action=='unvisited' then app.regionSelected=b.siteId end
     return
    end
   end
  end
  return
 end
 if app.help or app.newRun or app.mapBrowser or app.benchmark or app.fieldnotes then return end
 if button==3 then app.panning=true;return end
 if button~=1 and button~=2 then return end
 if button==1 then
  if app.regionButton and contains(app.regionButton,mx,my) then toggleRegion();return end
  for _,b in ipairs(app.siteButtons or {}) do if contains(b,mx,my) then selectSite(b.siteId);return end end
  for _,b in ipairs(app.buttons) do if contains(b,mx,my) then app.tool=b.kind;app.port=nil;if b.kind=='rally' then app.rallyWorker=app.selectedWorker or 0 end;return end end
  for _,b in ipairs(app.crewButtons or {}) do if contains(b,mx,my) then app.selectedWorker=b.id;app.selectedCell=nil;return end end
  local t=renderer.timeline
  if contains(t,mx,my) then app.paused=true;app.accumulator=0;app.stepBudget=0;app.history:seek(math.floor(U.clamp((mx-t.x)/t.w)*app.history.frontier));return end
 end
 local x,y=renderer:cell(mx,my);if not x then return end
 local w=currentWorld()
 app.selectedCell={x=x,y=y};app.selectedWorker=nil
 if button==2 then app.tool='inspect';app.port=nil;return end
 if app.port then queue({type='port',slot=app.portSlot,port=app.port,x=x,y=y});app.port=nil;return end
 if app.tool=='inspect' then
  for _,a in ipairs(w.workers) do if math.abs(x-a.x)<=2 and y>=a.y-3 and y<=a.y+1 then app.selectedWorker=a.id;break end end
 elseif app.tool=='rally' then
  queue({type='rally',worker=app.rallyWorker or 0,x=x,y=y});app.tool='inspect'
 elseif app.tool=='survey' or app.tool=='salvage' or app.tool=='cull' then
  local p=Content.at(w,x,y)
  if p then queue({type='field',kind=app.tool,target=p.id,worker=app.orderWorker or 0,priority=app.priority})
  else notify('No living encounter at that cell. Inspect a growth, creature or ruin object.') end
 elseif app.tool=='paint' then queue({type='paint',x=x,y=y,material=app.brush or M.WATER})
 else local gx,gy=W.tile(w,x,y);app.drag={gx=gx,gy=gy,tool=app.tool} end
end
function love.mousemoved(mx,my,dx,dy)
 if app.crew or app.fieldnotes or app.newRun or app.mapBrowser or app.help or app.region then return end
 if app.panning then renderer.panX=renderer.panX+dx;renderer.panY=renderer.panY+dy end
 local x,y=renderer:cell(mx,my);app.hover=x and {x=x,y=y} or nil
end
function love.mousereleased(mx,my,button)
 if button==3 then app.panning=false end
 if button~=1 or not app.drag or app.crew or app.fieldnotes or app.newRun or app.mapBrowser then return end
 local d=app.drag;app.drag=nil
 local x,y=renderer:cell(mx,my);if not x then return end
 local ex,ey=W.tile(currentWorld(),x,y);local count=0
 for gy=math.min(d.gy,ey),math.max(d.gy,ey) do for gx=math.min(d.gx,ex),math.max(d.gx,ex) do
  if count<256 then
   if d.tool=='cancel' then queue({type='cancel',gx=gx,gy=gy})
   else queue({type='order',kind=(d.tool=='dig' or d.tool=='remove') and d.tool or 'build',
     build=(d.tool~='dig' and d.tool~='remove') and d.tool or nil,gx=gx,gy=gy,priority=app.priority,worker=app.orderWorker or 0}) end
   count=count+1
  end
 end end
 if count==256 then notify('Selection limited to 256 blocks.') end
end
function love.wheelmoved(_,dy) if not app.newRun and not app.mapBrowser and not app.help and not app.crew and not app.fieldnotes and not app.region then renderer.zoom=U.clamp(renderer.zoom+dy*0.25,0.5,6) end end
function love.resize() renderer:layout(app) end
function love.quit()
 if app.history and not app.saveBlocked and not save() then return true end
end
-- Test adapter access only; ordinary game modules never import main.lua.
return {app=app,getRenderer=function() return renderer end}
