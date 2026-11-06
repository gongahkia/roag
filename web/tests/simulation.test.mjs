import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {derive,draw,nextRaw,roll,streams,STREAMS} from '../src/simulation/random.ts';
import {createWorld,generationIssues} from '../src/simulation/world.ts';
import {enqueue,step,route,renderedPosition,terminalReason} from '../src/simulation/step.ts';
import {spatial,index,SEARCH_LIMIT} from '../src/simulation/model.ts';
import {canonical} from '../src/simulation/canonical.ts';
import {makeSave,encodeSave,parseSave} from '../src/persistence/schema.ts';
import {FixedClock} from '../src/platform/clock.ts';
const advance=(world,n)=>{for(let i=0;i<n;i++)step(world);};
const save=world=>makeSave(world,{manualPaused:true,speed:2});

test('reference-derived C#/Lua RNG vectors: 768 rejection-sampled draws and 9 UTF-8 labels',()=>{
 const reference=JSON.parse(readFileSync(new URL('../../tests/Roag.Core.Tests/Tactics/Fixtures/original-foundation.json',import.meta.url)));
 for(const row of reference.random){let state=row.seed;for(const expected of row.values){const actual=draw(state,row.maximum);assert.equal(actual.value,expected);state=actual.state;}assert.equal(state,row.state);assert.equal(nextRaw(state),nextRaw(row.state));}
 for(const row of reference.derived)assert.equal(derive(row.seed,row.label),row.value,row.label);
 assert.throws(()=>draw(42,0));assert.throws(()=>derive(42,''));assert.throws(()=>nextRaw(0));
 const rng=streams(42),before={...rng};roll(rng,'combat',6);for(const label of STREAMS)if(label!=='combat')assert.equal(rng[label],before[label]);
});
test('multi-seed normal generation validates connectivity, six spawns, finite resource access and fallback',()=>{
 for(const seed of [...Array.from({length:100},(_,i)=>i+1),1337,123456789,2147483646]){
  const world=createWorld(seed);assert.deepEqual(generationIssues(world),[],`seed ${seed}`);assert.equal(world.width,128);assert.equal(world.people.length,6);assert.ok(world.resources.every(r=>r.remaining>0));assert.deepEqual(world,createWorld(seed));
 }
 for(const seed of [1,42,2147483646]){const w=createWorld(seed,{forceFallback:true});assert.ok(w.generation.fallback);assert.deepEqual(generationIssues(w),[]);assert.equal(w.generation.attempts,0);}
});
test('render cadence cannot change ordered commands or exact state/RNG checkpoints',()=>{
 const run=cadence=>{
  const w=createWorld(42);enqueue(w,{kind:'move',actor:2,target:{x:72,y:64}},2);enqueue(w,{kind:'move',actor:3,target:{x:70,y:62}},2);enqueue(w,{kind:'move',actor:2,target:{x:58,y:66}},120);
  const clock=new FixedClock();clock.setManual(false);const hashes=[];let frame=0;
  while(w.tick<300){clock.frame(cadence[frame++%cadence.length],()=>{if(w.tick<300){step(w);if(w.tick%50===0)hashes.push(canonical(w));}});renderedPosition(w.people[0],clock.alpha);}
  return hashes;
 };
 assert.deepEqual(run([10,17,33]),run([250,120,7,850]));
});
test('mid-edge saves retain progress, reserved cells, pending tick commands, RNG and exact continuation',()=>{
 const w=createWorld(1337);enqueue(w,{kind:'move',actor:2,target:{x:75,y:64}});enqueue(w,{kind:'move',actor:3,target:{x:73,y:62}},40);advance(w,7);
 assert.ok(w.people[0].next);assert.ok(w.people[0].progress>0);const restored=parseSave(encodeSave(save(w))).world;assert.equal(canonical(restored),canonical(w));
 for(let i=0;i<180;i++){step(w);step(restored);assert.equal(canonical(restored),canonical(w),`continued tick ${w.tick}`);}
});
test('routing laboratory: obstruction, terrain version invalidation, mid-route editing and recovery',()=>{
 const w=createWorld(42,{scenario:'routing-lab'});enqueue(w,{kind:'move',actor:2,target:{x:70,y:60}});enqueue(w,{kind:'edit',target:{x:64,y:60},solid:true});advance(w,20);
 assert.equal(w.people[0].x,58);assert.match(w.people[0].blocked,/No route/);
 assert.equal(canonical(parseSave(encodeSave(save(w))).world),canonical(w));
 enqueue(w,{kind:'edit',target:{x:64,y:60},solid:false});advance(w,5);assert.ok(w.people[0].x>58);assert.equal(w.people[0].routeVersion,w.terrainVersion);
 enqueue(w,{kind:'edit',target:{x:68,y:60},solid:true});advance(w,30);assert.ok(w.people[0].x < 68); assert.match(w.people[0].blocked, /No route/); // finishes its reserved step, then invalidates the obstructed route
 enqueue(w,{kind:'edit',target:{x:68,y:60},solid:false});advance(w,60);assert.equal(w.people[0].x,70);assert.equal(w.people[0].destination,null);assert.equal(w.people[0].blocked,null);
});
test('competing agents reserve real tiles, never overlap/swap/teleport, recover when target vacates',()=>{
 const w=createWorld(42);const a=w.people[0],b=w.people[1];a.x=60;a.y=64;b.x=62;b.y=64;
 enqueue(w,{kind:'move',actor:2,target:{x:61,y:64}});enqueue(w,{kind:'move',actor:3,target:{x:61,y:64}});
 for(let i=0;i<60;i++){const before=w.people.map(p=>({x:p.x,y:p.y}));step(w);const occupied=spatial(w);assert.equal(occupied.size,w.people.filter(p=>p.life!=='dead').length+w.people.filter(p=>p.life!=='dead'&&p.next).length);for(let j=0;j<6;j++)assert.ok(Math.abs(w.people[j].x-before[j].x)+Math.abs(w.people[j].y-before[j].y)<=1);}
 assert.equal(a.x,61);assert.ok(b.destination);enqueue(w,{kind:'move',actor:2,target:{x:60,y:64}});advance(w,60);assert.equal(b.x,61);assert.equal(b.destination,null);
 const result=route(w,a,{x:120,y:120},spatial(w),a.id,5);assert.ok(result.expanded<=5);assert.ok(result.expanded<=SEARCH_LIMIT);assert.match(result.reason,/budget/);
});
test('invalid commands/saves do not alter source world; versions, references, RNG, routes and terminal state are checked',()=>{
 const w=createWorld(42),before=canonical(w);assert.equal(enqueue(w,{kind:'move',actor:999,target:{x:5,y:5}}),false);assert.equal(enqueue(w,{kind:'edit',target:{x:4,y:4},solid:true}),false);assert.equal(canonical(w),before);
 const good=encodeSave(save(w));
 const lab=createWorld(42,{scenario:'routing-lab'}),labBefore=canonical(lab);
 assert.equal(enqueue(lab,{kind:'unknown',target:{x:64,y:60},solid:true}),false);assert.equal(canonical(lab),labBefore);
 for(const mutate of [s=>s.version=999,s=>s.contentVersion='future',s=>s.world.rng.combat=0,s=>s.world.people[0].id=3,s=>s.world.people[0].x=-1,s=>s.world.people[0].progress=2,s=>s.world.terminal='all-dead',s=>Object.assign(s.world.people[1],{x:s.world.people[0].x,y:s.world.people[0].y}),s=>s.world.pending=[{sequence:1,due:1,input:{kind:'move',actor:999,target:{x:1,y:1}}}],s=>s.controls.speed=3,s=>s.world.people[0].life=['alive']]){
  const bad=JSON.parse(good);mutate(bad);assert.throws(()=>parseSave(JSON.stringify(bad)));assert.equal(canonical(w),before);
 }
 assert.throws(()=>parseSave('{corrupt'));assert.throws(()=>parseSave(' '.repeat(2000001)));assert.equal(encodeSave(parseSave(good)),good);
});
test('terminal scaffold respects actual death, heart loss and frozen terminal continuation',()=>{
 const w=createWorld(42);for(const p of w.people)p.life='downed';assert.equal(terminalReason(w),null);for(const p of w.people)p.life='dead';assert.equal(terminalReason(w),'all-dead');step(w);const frozen=canonical(w);step(w);assert.equal(canonical(w),frozen);
 const heart=createWorld(42);heart.heart.health=0;assert.equal(terminalReason(heart),'heart-destroyed');
});
test('foreground processing retains backlog; hidden/manual pause discard only explicit paused wall time',()=>{
 for(const speed of [1,2,4]){const measured=new FixedClock();measured.speed=speed;measured.setManual(false);let count=0;measured.frame(1000,()=>count++);while(measured.backlog)measured.frame(0,()=>count++);assert.equal(count,10*speed);}
 const clock=new FixedClock();let ticks=0;clock.setManual(false);assert.equal(clock.frame(5000,()=>ticks++),8);assert.equal(clock.backlog,42);while(clock.backlog)clock.frame(0,()=>ticks++);assert.equal(ticks,50);
 clock.setHidden(true);clock.frame(50000,()=>ticks++);clock.setHidden(false);assert.equal(clock.manualPaused,false);clock.frame(99,()=>ticks++);assert.equal(ticks,50);clock.frame(1,()=>ticks++);assert.equal(ticks,51);
 clock.setManual(true);clock.setHidden(true);clock.setHidden(false);clock.frame(5000,()=>ticks++);assert.equal(ticks,51);assert.equal(clock.manualPaused,true);
});
