-- Map-aware, ordinary-action acceptance; no HP, position, enemy or reward edits.
local G=require('src.game');local T=require('src.idea_terrain');local P=require('src.persistence')
local Play=require('tests.legal_playthrough')
local candidates={heat_vent={},push_floor={},fragile_crossing={}}
for seed=1,12 do
 local s=G.new_stage(seed,{multifloor=true,class_id='core:actor/sentinel'});local p=G.player(s).position
 for _,n in ipairs(s.idea_terrain.nodes)do if n.floor_id==s.active_floor_id then
  local list=candidates[n.kind];list[#list+1]={seed=seed,id=n.id,distance=math.abs(n.x-p.x)+math.abs(n.y-p.y)}
 end end
end
for _,kind in ipairs({'heat_vent','push_floor','fragile_crossing'})do
 local list=candidates[kind];table.sort(list,function(a,b)if a.distance~=b.distance then return a.distance<b.distance end;return a.seed<b.seed end)
 local passed=false
 for attempt=1,math.min(6,#list)do
  local c=list[attempt];local run=Play.new(c.seed,{multifloor=true,class_id='core:actor/sentinel'})
  local initial=G.snapshot(run.state);local n=T.find(run.state,c.id)
  local okay,why=pcall(function()
   local arrived,reason=Play.navigate(run,function(at)return math.abs(at.x-n.x)+math.abs(at.y-n.y)==1 end,260)
   assert(arrived,reason);assert(n.phase~='closed','shortcut already used by an enemy')
   local p=G.player(run.state);local dx,dy=n.x-p.position.x,n.y-p.position.y
   local committed,reason=Play.action(run,{kind='move',dx=dx,dy=dy});assert(committed,reason)
   if kind=='heat_vent'then
    -- Deliberately wait through the genuine warning, accepting ordinary risk.
    for _=1,6 do
     local burst=false;for _,e in ipairs(run.events)do if e.kind=='HeatVentBurst' and e.floor_id==n.floor_id and e.x==n.x and e.y==n.y then burst=true end end
     if burst then break end
     assert(Play.action(run,{kind='wait'}))
    end
    local burst=false;for _,e in ipairs(run.events)do if e.kind=='HeatVentBurst' and e.floor_id==n.floor_id and e.x==n.x and e.y==n.y then burst=true end end
    assert(burst,'no visible real vent impact')
   elseif kind=='push_floor'then
    local pushed=false;for _,e in ipairs(run.events)do if e.kind=='PushFloorTriggered' and e.target_id==run.state.player_id and e.x==n.x and e.y==n.y then pushed=true end end
    assert(pushed,'arrow landing was blocked')
   else
    assert(n.phase=='armed','shortcut not entered')
    local moved=false
    for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do
     local view=G.preview(run.state,'move',d[1],d[2])
     if view.valid and view.distance==1 then assert(Play.action(run,{kind='move',dx=d[1],dy=d[2]}));moved=true;break end
    end
    assert(moved and n.phase=='closed','shortcut occupied or no legal departure')
   end
   assert(not run.state.game_over,'dead')
   local replay=G.restore(initial);local offset=0
   for index,record in ipairs(run.trace)do
    if index==math.max(1,math.floor(#run.trace/2))then replay=G.restore(G.snapshot(replay))end
    local good,reason,events=G.submit(replay,record.intent);assert(good==record.ok and reason==record.reason)
    for _,e in ipairs(events)do offset=offset+1;assert(P.encode(e)==P.encode(run.events[offset]),'event replay differs')end
   end
   assert(offset==#run.events and P.encode(G.snapshot(replay))==P.encode(G.snapshot(run.state)),'snapshot replay differs')
  end)
  local p=G.player(run.state)
  if okay then
   print(('PASS %s seed=%d actions=%d time=%d HP=%d/%d gold=%d exact replay/Continue'):format(kind,c.seed,#run.trace,run.state.clock,p.hp,p.max_hp,run.state.currency));passed=true;break
  end
  assert(not tostring(why):find('invalid saved') and not tostring(why):find('replay differs')
   and not tostring(why):find('scheduler') and not tostring(why):find('orphan'),why)
  print(('ATTEMPT %s seed=%d actions=%d HP=%d reason=%s'):format(kind,c.seed,#run.trace,p.hp,tostring(why)))
 end
 assert(passed,'no successful natural legal scenario: '..kind)
end
print('3/3 natural terrain scenarios; public actions only; no LÖVE initialized')
