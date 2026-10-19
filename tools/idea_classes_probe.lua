-- Headless legal first encounters, exact replay and bounded class-state stress.
-- Prepared stress states are labelled separately from legal generated play.
package.path='./?.lua;'..package.path
io.stdout:setvbuf('line')
assert(not _G.love,'this probe must not open LÖVE')
local G=require('src.game');local P=require('src.persistence');local Play=require('tests.legal_playthrough')
local classes={'geomancer','spiritbinder','tinkerer','stormcaller','wayfinder'}
for _,name in ipairs(classes)do
 for _,seed in ipairs({3,9,21})do
  local run=Play.new(seed,{class_id='core:actor/'..name,multifloor=true})
  local initial=G.snapshot(run.state)
  local enemy=assert(Play.nearest(run.state,function(e)return e.controller=='ai' and not e.invulnerable end))
  local ok,why=Play.fight(run,enemy.id,220);assert(ok,name..' seed '..seed..': '..tostring(why))
  local replay=G.restore(initial)
  for _,row in ipairs(run.trace)do
   local accepted,reason,events=G.submit(replay,row.intent)
   assert(accepted==row.ok and reason==row.reason and replay.clock==row.clock and #events==#row.event_kinds)
   for i,event in ipairs(events)do assert(event.kind==row.event_kinds[i])end
  end
  assert(P.encode(G.snapshot(replay))==P.encode(G.snapshot(run.state)))
  print(('LEGAL %s seed=%d actions=%d hp=%d clock=%d exact_replay=yes'):format(name,seed,#run.trace,G.player(run.state).hp,run.state.clock))
 end
end
for _,name in ipairs(classes)do
 local s=G.new(37,nil,{class_id='core:actor/'..name});local p=G.player(s);p.position.x,p.position.y=7,6;require('src.visibility').recompute(s)
 if name=='spiritbinder' or name=='stormcaller'then
  local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',10,6,1000000);e.hp=1000000;e.max_hp=1000000
 end
 local rng=s.rng.state;local start=os.clock();collectgarbage('collect');local heap=collectgarbage('count')
 for i=1,100 do
  local intents
  if name=='geomancer'then intents={{kind='sweeping_slash',dx=i%2==0 and -1 or 0,dy=i%2==0 and 0 or 1}}
  elseif name=='spiritbinder' or name=='tinkerer'then intents={{kind='sweeping_slash',dx=1,dy=0}}
  elseif name=='stormcaller'then intents={{kind='basic_attack',dx=1,dy=0},{kind='sweeping_slash',dx=1,dy=0}}
  else intents={{kind='sweeping_slash',dx=i%2==1 and 1 or -1,dy=0},{kind='dash',dx=1,dy=0}}end
  for _,intent in ipairs(intents)do local ok,why=G.submit(s,intent);assert(ok,name..': '..tostring(why))end
  assert(#s.entities<=(name=='spiritbinder' and 3 or 2) and #s.schedule<=3)
 end
 assert(G.restore(G.snapshot(s)) and s.rng.state==rng)
 collectgarbage('collect')
 print(('PREPARED-STRESS %s cycles=100 cpu=%.4fs heap_delta=%.1fKiB entities=%d schedule=%d save_bytes=%d rng_unchanged=yes'):format(
  name,os.clock()-start,collectgarbage('count')-heap,#s.entities,#s.schedule,#P.encode(G.snapshot(s))))
end
