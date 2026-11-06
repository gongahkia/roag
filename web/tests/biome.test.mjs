import test from 'node:test';
import assert from 'node:assert/strict';
import {createWorld,generationIssues} from '../src/simulation/world.ts';
import {step,enqueue} from '../src/simulation/step.ts';
import {makeSave,parseSave,encodeSave} from '../src/persistence/schema.ts';
import {createRaider} from '../src/simulation/combat.ts';
import {biomeQuantity,groundMoveTicks,hotGround} from '../src/simulation/biome.ts';
import {canonical} from '../src/simulation/canonical.ts';
import {materialAudit} from '../src/simulation/jobs.ts';

const controls={manualPaused:true,speed:4};
test('103 seeds preserve connected colony starts and finite biome-specific stocks and patches',()=>{
 const counts={woodland:0,marsh:0,ash:0};
 for(const seed of [...Array.from({length:100},(_,i)=>i+1),912341,2147483646,1234567]){
  const w=createWorld(seed);assert.deepEqual(generationIssues(w),[]);counts[w.environment.biome]++;
  const e=w.environment;assert.equal(e.wet.length,e.biome==='marsh'?128:0);assert.equal(e.ash.length,e.biome==='ash'?128:0);
  for(const c of [...e.wet,...e.ash]){
   assert.ok(Math.max(Math.abs(c.x-64),Math.abs(c.y-64))>12);
   assert.ok(!w.resources.some(r=>r.x===c.x&&r.y===c.y));
  }
  assert.ok(w.resources.every(r=>r.initial>=10&&r.initial<=81&&r.initial===r.remaining));
  makeSave(w,controls);
 }
 assert.ok(Object.values(counts).every(n=>n>20));
 for(const seed of [42,43,44]){const w=createWorld(seed,{forceFallback:true});assert.deepEqual(generationIssues(w),[]);makeSave(w,controls);}
 assert.equal(biomeQuantity('marsh','food',40),60);assert.equal(biomeQuantity('marsh','wood',40),30);
 assert.equal(biomeQuantity('ash','stone',40),60);assert.equal(biomeQuantity('ash','food',40),30);
});

test('a reserved wet edge retains its duration after the water is removed and reload is exact',()=>{
 const w=createWorld(43,{scenario:'work-lab'}),p=w.people[0],wet=w.environment.wet[0];
 // Isolated movement fixture; preserve generated hazard coordinates and choose
 // one open adjacent cell, away from other actors and natural sources.
 const from=[{x:wet.x-1,y:wet.y},{x:wet.x+1,y:wet.y},{x:wet.x,y:wet.y-1},{x:wet.x,y:wet.y+1}].find(c=>w.terrain[c.y*128+c.x]!==2);
 assert.ok(from);p.x=from.x;p.y=from.y;
 assert.ok(enqueue(w,{kind:'move',actor:p.id,target:wet}));step(w);
 assert.equal(p.moveTicks,6);assert.ok(p.next);step(w);assert.equal(p.progress,1);
 w.environment.wet=[];assert.equal(groundMoveTicks(w,wet),4);
 const restored=parseSave(encodeSave(makeSave(w,controls))).world;
 for(let i=0;i<5;i++){step(w);step(restored);assert.equal(canonical(w),canonical(restored));makeSave(w,controls);}
 assert.equal(p.x,wet.x);assert.equal(p.y,wet.y);assert.equal(p.moveTicks,4);assert.equal(p.progress,0);
});

test('ash burns workers and attackers with the same cadence, actual health loss and no invented resources',()=>{
 const w=createWorld(44,{scenario:'work-lab'}),p=w.people[0],cells=w.environment.ash;
 p.x=cells[0].x;p.y=cells[0].y;
 const r=createRaider(w,{id:w.nextId++,kind:'raider',squadId:null,leaderId:null,edge:'east',entryOffset:0},cells[1]);
 w.combat.raiders.push(r);w.combat.stats.spawned=1;
 // Focused environmental fixture: prevent AI travel with its existing saved
 // cooldown/commitment-free routing retry, then exercise one actual heat tick.
 r.retryAt=100;r.cooldown=100;w.tick=49;
 assert.equal(hotGround(w,p),true);assert.equal(hotGround(w,r),true);
 step(w);assert.equal(p.body.health,98);assert.equal(r.health,34);assert.equal(w.combat.stats.colonyDamage,2);
 for(const a of Object.values(materialAudit(w)))assert.equal(a.produced,a.accounted);
});
