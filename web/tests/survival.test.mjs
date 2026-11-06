import test,{after} from 'node:test';
import {createHash} from 'node:crypto';
import {writeFileSync,mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
import assert from 'node:assert/strict';
import {establish,harness,scenarioRuns} from './helpers/survival.mjs';
import {NEEDS,RECIPES,validateSurvivalContent} from '../src/simulation/survival-content.ts';
import {makeSave,encodeSave,parseSave} from '../src/persistence/schema.ts';
import {canonical} from '../src/simulation/canonical.ts';
import {step,enqueue} from '../src/simulation/step.ts';
import {materialAudit} from '../src/simulation/jobs.ts';
import {index,neighbors} from '../src/simulation/model.ts';
import {total} from '../src/simulation/work-content.ts';
const continuations=[];const hash=w=>createHash('sha256').update(canonical(w)).digest('hex');
const prepared=establish('work-lab').w;
function setup(){const h=harness('work-lab',structuredClone(prepared));for(const j of h.w.jobs.filter(j=>j.state==='active'))h.cmd({kind:'work',job:j.id,action:'cancel'});return h;}
const save=w=>encodeSave(makeSave(w,{manualPaused:true,speed:4}));
function continuation(w,n=500){const copy=parseSave(save(w)).world,original=structuredClone(w);for(let i=0;i<n;i++){step(copy);step(original);}assert.equal(canonical(copy),canonical(original));for(const a of Object.values(materialAudit(copy)))assert.equal(a.produced,a.accounted);continuations.push({savedTick:w.tick,ticks:n,savedHash:hash(w),uninterruptedHash:hash(original),restoredHash:hash(copy),routines:structuredClone(w.people.map(p=>({id:p.id,job:p.job,cargo:p.cargo,body:p.body}))),materials:materialAudit(copy)});return copy;}
test('survival tuning rejects invalid thresholds, times, capacities and recipe output',()=>{
 validateSurvivalContent();for(const f of [n=>n.dayTicks=0,n=>n.mealAt=n.starveAt,n=>n.restTarget=n.restAt]){const n=structuredClone(NEEDS);f(n);assert.throws(()=>validateSurvivalContent(n));}
 for(const f of [r=>r.meal.output.meal=6,r=>r.medicine.cost.ore=-1,r=>r.tool.reserve=0,r=>delete r.medicine.cost.wood,r=>r.ore.facility='unknown']){const r=structuredClone(RECIPES);f(r);assert.throws(()=>validateSurvivalContent(NEEDS,r));}
});
test('ordinary production and three days sustain six founders with physical food, repeated rest and exact save',()=>{
 const h=establish(),{w}=h;while(w.tick<NEEDS.dayTicks*3)h.tick();assert.ok(w.people.every(p=>p.life==='alive'&&p.body.health===100&&p.body.meals>=5&&p.body.rests>=3));assert.ok(w.statistics.harvests>=3&&w.recipeCounts.meal>=9&&w.recipeCounts.medicine>=2&&w.recipeCounts.ore>=3&&w.recipeCounts.tool>=1);continuation(w);
});
for(const phase of ['haul','construction'])for(const [action,routine]of [['hungry','eat'],['tired','rest'],['injure','treatment']])test(`${routine} interrupts ${phase}, retains progress and cargo through exact reload and resumes`,()=>{
 const h=setup(),{w,cmd,until}=h;
 cmd({kind:'build',building:'wall',target:{x:72,y:64}});const j=w.jobs.at(-1);
 until(()=>phase==='haul'?w.people.some(p=>p.job===j.id&&total(p.cargo)&&p.next):j.progress>0,phase);
 const p=w.people.find(p=>p.id===j.worker),progress=j.progress,cargo={...p.cargo},site={...j.site};
 cmd({kind:'dev',action,id:p.id});assert.equal(p.body.routine?.kind,routine);assert.equal(j.worker,p.id);assert.equal(p.job,j.id);assert.equal(j.progress,progress);assert.deepEqual(j.site,site);assert.deepEqual(p.cargo,cargo);
 const copy=continuation(w,650);assert.equal(copy.jobs.find(a=>a.id===j.id).state,'done');until(()=>j.state==='done','resumed work',1000);assert.equal(j.completions,1);save(w);
});
test('cancel and reassign during needs release original claims/cargo exactly once while care retains its own claim',()=>{
 for(const action of ['cancel','suspend','assign']){
  const h=setup(),{w,cmd,until}=h;cmd({kind:'build',building:'door',target:{x:72,y:64}});const j=w.jobs.at(-1);until(()=>w.people.some(p=>p.job===j.id&&total(p.cargo)>0),'carrying');const p=w.people.find(p=>p.id===j.worker);cmd({kind:'dev',action:'tired',id:p.id});
  if(action==='assign')cmd({kind:'assign',job:j.id,worker:w.people.find(a=>a.id!==p.id).id});else cmd({kind:'work',job:j.id,action});assert.equal(p.job,null);assert.equal(total(p.cargo),0);assert.equal(p.body.routine.kind,'rest');assert.equal(j.reservation,null);continuation(w);if(action==='cancel'){assert.equal(enqueue(w,{kind:'work',job:j.id,action:'cancel'}),false);assert.equal(j.completions,0);}
 }
});
test('bed and medicine/patient claims remain exclusive; rescue spends one real dose after physical timed aid',()=>{
 const h=setup(),{w,cmd,until,tick}=h;
 for(const p of w.people)cmd({kind:'dev',action:'tired',id:p.id});tick(20);
 const claims=w.people.map(p=>p.body.routine?.facility).filter(x=>x!==null&&x!==undefined);assert.equal(new Set(claims).size,claims.length);assert.equal(claims.length,3);save(w);
 until(()=>w.people.every(p=>p.body.routine?.kind!=='rest'),'rest rotation',1000);
 const p=w.people[5],start=w.statistics.rescues,consumed=w.consumed.medicine;cmd({kind:'dev',action:'down',id:p.id});const pos={x:p.x,y:p.y},at=w.tick;
 until(()=>w.people.some(a=>a.body.routine?.kind==='rescue'&&a.body.routine.cargo.medicine===1),'actual medicine pickup');assert.equal(w.consumed.medicine,consumed);assert.equal(w.people.filter(a=>a.body.routine?.patient===p.id).length,1);continuation(w,400);
 until(()=>p.life==='alive','rescue');assert.ok(w.tick-at>=NEEDS.rescueTicks);assert.deepEqual({x:p.x,y:p.y},pos);assert.equal(w.statistics.rescues,start+1);assert.equal(w.consumed.medicine,consumed+1);
 until(()=>p.body.health===100&&p.body.recoverAt===0,'infirmary treatment',800);assert.equal(w.consumed.medicine,consumed+2);assert.ok(w.statistics.treatments>=1);save(w);
});
test('a blocked clinic cannot treat remotely; opening access resumes physical treatment with retained medicine',()=>{
 const h=setup(),{w,cmd,until,tick}=h,p=w.people[0],clinic=w.structures.find(s=>s.kind==='infirmary');
 for(const [i,a]of w.people.entries())cmd({kind:'move',actor:a.id,target:{x:58+i,y:70}});until(()=>w.people.every(a=>!a.destination&&!a.next),'clear clinic');
 const cells=neighbors(clinic);assert.ok(cells.every(c=>!w.people.some(p=>p.x===c.x&&p.y===c.y||p.next?.x===c.x&&p.next?.y===c.y)));const old=cells.map(c=>w.terrain[index(c.x,c.y)]);cells.forEach(c=>w.terrain[index(c.x,c.y)]=2);w.terrainVersion++;
 cmd({kind:'dev',action:'injure',id:p.id});tick(150);assert.equal(p.body.health,30);assert.match(p.body.status,/interaction|Reaching/);assert.equal(w.consumed.medicine,prepared.consumed.medicine);continuation(w,100);
 cells.forEach((c,i)=>w.terrain[index(c.x,c.y)]=old[i]);w.terrainVersion++;until(()=>p.body.health===100,'access recovery',800);save(w);
});
test('scarce food is reserved once and eating cannot consume a remote shared total',()=>{
 const h=setup(),{w,cmd,tick,until}=h;
 const p=w.people.find(p=>p.x>=68)||w.people[0];cmd({kind:'dev',action:'hungry',id:p.id});const eaten=w.statistics.eatenMeals+w.statistics.eatenFood;assert.equal(w.statistics.eatenMeals+w.statistics.eatenFood,eaten);assert.equal(p.body.routine.kind,'eat');assert.ok(p.body.routine.reservation||total(p.body.routine.cargo)===1);
 for(const a of w.people.filter(a=>a!==p))cmd({kind:'dev',action:'hungry',id:a.id});tick(1);save(w);until(()=>w.people.every(a=>a.body.hunger<NEEDS.mealAt),'all eat',600);assert.equal(w.statistics.eatenMeals+w.statistics.eatenFood,eaten+6);
});
test('ordinary unprovisioned hunger and fatigue cause visible health loss; food/rest/treatment access restores health',()=>{
 const h=harness(),{w,tick}=h;tick(3650);assert.ok(w.people.every(p=>p.body.health<100));assert.ok(w.people.some(p=>p.body.status.includes('Starving')));assert.ok(w.people.every(p=>p.body.mood<30));tick(1200);assert.ok(w.people.some(p=>p.body.fatigue>=NEEDS.exhaustedAt));
 // Separate established scenario: focused unmet-need thresholds, actual facilities/food/medicine provide recovery.
 const r=setup(),p=r.w.people[0];p.body.hunger=NEEDS.starveAt;p.body.fatigue=NEEDS.exhaustedAt;r.tick(25);assert.ok(p.body.health<100);r.until(()=>p.body.health===100&&p.body.hunger<NEEDS.mealAt&&p.body.fatigue<NEEDS.restAt,'recover from deprivation',1200);save(r.w);
});
test('no rescuer is invented: all downed remain nonterminal, deadlines cause exactly-once death and terminal reload',()=>{
 const h=harness('work-lab'),{w,cmd,tick}=h;for(const p of w.people)cmd({kind:'dev',action:'down',id:p.id});assert.equal(w.terminal,null);assert.ok(w.people.every(p=>p.life==='downed'));assert.match(w.people[0].body.status,/Nobody capable/);save(w);
 tick(NEEDS.rescueWindow);assert.equal(w.terminal,'all-dead');assert.equal(w.statistics.deaths,6);const stable=save(w);tick(20);assert.equal(save(w),stable);const loaded=parseSave(stable).world;step(loaded);assert.equal(canonical(loaded),canonical(w));
});
test('death during medicine pickup/transport settles care and original job, never repeats a reward or consumption',()=>{
 const h=setup(),{w,cmd,until,tick}=h;cmd({kind:'dev',action:'down',id:7});until(()=>w.people.some(p=>p.body.routine?.cargo.medicine===1),'rescuer carrying medicine');const medic=w.people.find(p=>p.body.routine?.cargo.medicine===1),used=w.consumed.medicine;cmd({kind:'dev',action:'kill',id:medic.id});assert.equal(medic.body.routine,null);assert.equal(medic.job,null);assert.equal(w.consumed.medicine,used);assert.equal(enqueue(w,{kind:'dev',action:'kill',id:medic.id}),false);tick(20);save(w);continuation(w);
});
test('expanded saves reject forged claims, lifecycle deadlines, crop stages and accounting; prior versions are safe rejections',()=>{
 const h=setup(),{w,cmd}=h;cmd({kind:'dev',action:'hungry',id:2});const good=save(w);
 for(const mutate of [s=>s.version=2,s=>s.world.people[0].body.routine.reservation={source:999999,kind:'meal',count:1},s=>s.world.people[0].body.duties.cook=4,s=>s.world.people[0].body.deathAt=s.world.tick+1,s=>s.world.produced.meal++,s=>s.world.people[0].body.routine.progress=999,s=>s.world.jobs.find(j=>j.kind==='produce').stage=2,s=>s.world.statistics.deaths++]){const bad=JSON.parse(good);mutate(bad);assert.throws(()=>parseSave(JSON.stringify(bad)));assert.equal(save(w),good);}
});
test('duty priorities dispatch real workers and guard watch is a physical noncombat assignment',()=>{
 const h=setup(),{w,cmd,until}=h;for(const p of w.people)cmd({kind:'duty',actor:p.id,duty:'builder',value:p.id===7?3:0});cmd({kind:'build',building:'wall',target:{x:72,y:64}});const j=w.jobs.at(-1);until(()=>j.state==='done','chosen builder');assert.ok(w.structures.some(s=>s.x===72&&s.y===64));
 cmd({kind:'duty',actor:7,duty:'guard',value:3});cmd({kind:'duty',actor:7,duty:'builder',value:1});until(()=>w.people[5].body.routine?.status.includes('Watching'),'watch post');cmd({kind:'duty',actor:7,duty:'guard',value:0});assert.equal(w.people[5].body.routine,null);save(w);
});

after(()=>{const dir=resolve(import.meta.dirname,'..',process.env.ROAG_EVIDENCE_DIR??'../artifacts/colony-web/P04');mkdirSync(dir,{recursive:true});writeFileSync(resolve(dir,`needs-checkpoints-${process.pid}.json`),JSON.stringify({continuations,scenarioRuns},null,2)+'\n');});
