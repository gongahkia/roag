import test from 'node:test';
import assert from 'node:assert/strict';
import {establish,harness} from './helpers/survival.mjs';
import {NEEDS} from '../src/simulation/survival-content.ts';
import {index,neighbors} from '../src/simulation/model.ts';
import {total} from '../src/simulation/work-content.ts';
import {makeSave,encodeSave,parseSave} from '../src/persistence/schema.ts';
import {canonical} from '../src/simulation/canonical.ts';
import {enqueue,step} from '../src/simulation/step.ts';
const baseline=establish('work-lab').w;
function setup(){const h=harness('work-lab',structuredClone(baseline));for(const j of h.w.jobs.filter(j=>j.state==='active'))h.cmd({kind:'work',job:j.id,action:'cancel'});return h;}
const save=w=>encodeSave(makeSave(w,{manualPaused:true,speed:4}));
test('changing to a higher guard duty releases active lower-priority work and physically takes watch',()=>{
 const h=setup(),{w,cmd,until}=h;cmd({kind:'build',building:'wall',target:{x:72,y:64}});const j=w.jobs.at(-1);until(()=>w.people.some(p=>p.job===j.id&&total(p.cargo)>0),'worker in flight');const p=w.people.find(p=>p.id===j.worker);cmd({kind:'duty',actor:p.id,duty:'guard',value:3});assert.equal(p.job,null);assert.equal(total(p.cargo),0);until(()=>p.body.routine?.status.includes('Watching'),'watch');save(w);
});
test('a destination issued during eating is retained until care completes',()=>{
 const h=setup(),{w,cmd,until}=h,p=w.people[0];cmd({kind:'dev',action:'hungry',id:p.id});assert.equal(p.body.routine.kind,'eat');cmd({kind:'move',actor:p.id,target:{x:58,y:70}});assert.deepEqual(p.body.routine.resume,{x:58,y:70});until(()=>p.x===58&&p.y===70&&!p.next&&!p.destination,'deferred destination',500);save(w);
});
test('production output survives missing storage, cancellation and physical recovery without repeated output',()=>{
 for(const cancel of [false,true]){
  const h=setup(),{w,cmd,until,tick}=h;
  until(()=>w.statistics.eatenMeals>=12,'consume reserve',1200);
  for(const s of [...w.structures].filter(s=>s.kind==='stockpile'))cmd({kind:'dismantle',structure:s.id});until(()=>!w.structures.some(s=>s.kind==='stockpile'),'remove storage',600);
  const kitchen=w.structures.find(s=>s.kind==='kitchen');cmd({kind:'produce',structure:kitchen.id,recipe:'meal'});const j=w.jobs.at(-1);
  until(()=>j.stage===3&&j.status.includes('Storage full'),'actual output awaiting storage',1500);const cycles=j.cycles,produced=w.produced.meal;tick(50);assert.equal(j.cycles,cycles);assert.equal(w.produced.meal,produced);save(w);
  if(cancel){cmd({kind:'work',job:j.id,action:'cancel'});assert.equal(enqueue(w,{kind:'work',job:j.id,action:'cancel'}),false);assert.equal(j.completions,0);}
  cmd({kind:'stockpile',target:{x:58,y:66}});until(()=>w.structures.find(s=>s.x===58&&s.y===66).inventory.meal>0,'physical output recovery',1000);save(w);
 }
});
test('a sealed patient cannot be rescued remotely: the paid dose survives failed access and death',()=>{
 const h=setup(),{w,cmd,until,tick}=h,p=w.people[5];cmd({kind:'move',actor:p.id,target:{x:55,y:70}});until(()=>p.x===55&&p.y===70&&!p.next&&!p.destination,'isolation');cmd({kind:'dev',action:'down',id:p.id});
 for(const c of neighbors(p))w.terrain[index(c.x,c.y)]=2;w.terrainVersion++;
 const consumed=w.consumed.medicine;until(()=>w.people.some(a=>a.body.routine?.patient===p.id&&a.body.routine.cargo.medicine===1),'medicine carried');tick(100);assert.equal(p.life,'downed');assert.equal(w.consumed.medicine,consumed);assert.ok(w.people.some(a=>a.body.routine?.status.includes('interaction')));const copy=parseSave(save(w)).world;const n=p.body.deathAt-w.tick;tick(n);for(let i=0;i<n;i++)step(copy);assert.equal(canonical(copy),canonical(w));assert.equal(p.life,'dead');assert.equal(w.statistics.deaths,1);assert.equal(w.consumed.medicine,consumed);assert.ok(!w.people.some(a=>a.body.routine?.patient===p.id));save(w);
});
test('raw finite food is physically eaten and accounted without a kitchen',()=>{
 const h=harness(),{w,cmd,tick}=h;cmd({kind:'stockpile',target:{x:63,y:66}});cmd({kind:'gather',source:21});tick(2000);assert.ok(w.statistics.eatenFood>=6);assert.equal(w.statistics.eatenMeals,0);assert.equal(w.consumed.food,w.statistics.eatenFood);save(w);
});
test('overwork alone visibly damages a fed colony; a rebuilt bed and medical care allow recovery',()=>{
 const h=setup(),{w,cmd,until}=h;
 for(const s of [...w.structures].filter(s=>s.kind==='bed'))cmd({kind:'dismantle',structure:s.id});until(()=>!w.structures.some(s=>s.kind==='bed'),'remove beds');
 const p=w.people[0];until(()=>p.body.fatigue>=NEEDS.exhaustedAt&&p.body.health<100,'overwork injury',6000);assert.ok(p.body.hunger<NEEDS.starveAt);assert.ok(p.body.fatigue>=NEEDS.exhaustedAt);assert.match(p.body.status,/Overworked/);
 h.build('bed',66,68);until(()=>p.body.fatigue<NEEDS.restAt&&p.body.health===100,'bed and medicine recovery',1000);save(w);
});
