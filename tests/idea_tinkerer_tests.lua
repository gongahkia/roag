return function(test,equal)
 local G=require('src.game');local C=require('src.idea_companions');local V=require('src.visibility');local P=require('src.persistence')
 local function trial(x,y)
  local s=G.new(37,nil,{class_id=C.tinker_id});local p=G.player(s);p.position.x,p.position.y=7,6
  local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',x or 10,y or 6,100000);V.recompute(s);return s,p,e
 end
 local function deploy(s)assert(G.submit(s,{kind='sweeping_slash',dx=1,dy=0}));return assert(C.owned(s,'sentry'))end
 test('Tinkerer deploys one three-attempt sentry and shows its locked real scheduled line',function()
  local s,p,e=trial();local before=G.snapshot(s);assert(G.preview(s,'sweeping_slash',1,0).valid);equal(before,G.snapshot(s))
  local sentry=deploy(s);assert(s.clock==168 and sentry.sentry_shots==2 and sentry.sentry_phase=='impact')
  local strikes=G.pending_strikes(s);assert(#strikes==1 and strikes[1].due==180 and strikes[1].target_cells[2].x==10)
  assert(e.hp==10 and G.restore(G.snapshot(s)))
 end)
 test('three finite sentry bolts use normal armor and cannot farm more attacks by waiting',function()
  local s,p,e=trial();local sentry=deploy(s);local hits=0
  for _=1,12 do local _,_,events=G.submit(s,{kind='wait'})
   for _,v in ipairs(events)do if v.kind=='AttackPerformed'then assert(v.ability_id=='core:ability/tinkerer_sentry' and v.proc_coefficient==0);hits=hits+1 end end
  end
  assert(hits==3 and e.hp==4 and sentry.sentry_shots==0 and not sentry.sentry_schedule_id)
  assert(G.restore(G.snapshot(s)))
 end)
 test('committed sentry bolts retain friendly fire and stop on the player in their line',function()
  local s,p,e=trial(5,6);deploy(s);assert(G.submit(s,{kind='wait'}))
  assert(p.hp==20 and e.hp==10)
 end)
 test('a moved sentry target is not tracked and replacement cancels the old committed shot',function()
  local s,p,e=trial();local first=deploy(s);e.position.y=7
  assert(G.submit(s,{kind='wait'}));assert(e.hp==10)
  s,p,e=trial();first=deploy(s);local old=first.sentry_schedule_id
  assert(G.submit(s,{kind='sweeping_slash',dx=0,dy=1}));local sentry=C.owned(s,'sentry')
  assert(sentry.id~=first.id and e.hp==10)
  for _,w in ipairs(s.schedule)do assert(w.id~=old)end
 end)
 test('sentry placement rejects occupied diagonals walls and stairs without spending time',function()
  local s,p,e=trial(8,6);local snap=G.snapshot(s)
  assert(not G.submit(s,{kind='sweeping_slash',dx=1,dy=0}));equal(snap,G.snapshot(s))
  assert(not G.submit(s,{kind='sweeping_slash',dx=1,dy=1}));equal(snap,G.snapshot(s))
  p.position.x,p.position.y=8,5;snap=G.snapshot(s)
  assert(not G.submit(s,{kind='sweeping_slash',dx=1,dy=0}));equal(snap,G.snapshot(s))
 end)
 test('sentry kills credit the owner once without autonomous boon or Echo proc chains',function()
  local s,p,e=trial();e.hp=2;G.set_boon_stacks(s,p.id,'core:boon/temporal_echo','common',1)
  deploy(s);local _,_,events=G.submit(s,{kind='wait'})
  assert(not G.entity(s,e.id) and s.progression.xp_total>0 and not next(s.delayed_effects))
  local kills=0;for _,v in ipairs(events)do if v.kind=='EntityKilled'then assert(v.source_id==p.id);kills=kills+1 end end;assert(kills==1)
 end)
 test('pending finite sentry shots resume exactly and reject forged charge or missing timer state',function()
  local s=trial();deploy(s);local snap=G.snapshot(s);local b=G.restore(P.decode(P.encode(snap)))
  for _=1,5 do local _,_,x=G.submit(s,{kind='wait'});local _,_,y=G.submit(b,{kind='wait'});equal(x,y);equal(G.snapshot(s),G.snapshot(b))end
  for _,change in ipairs({function(v)for _,e in ipairs(v.entities)do if e.companion_kind=='sentry'then e.sentry_shots=4 end end end,
   function(v)for i=#v.schedule,1,-1 do if v.schedule[i].idea_sentry then table.remove(v.schedule,i)end end end})do
   local v=P.decode(P.encode(snap));change(v);assert(not pcall(G.restore,v))
  end
 end)
end
