// Ordinary five-wave acceptance: all supplies, structures and orders arise from
// normal commands. Every command is retained for exact fresh-seed replay.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { mkdirSync,writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fortify } from '../tests/helpers/defense.mjs';
import { placementReason,workCommandReason } from '../src/simulation/jobs.ts';
import { createWorld } from '../src/simulation/world.ts';
import { distance } from '../src/simulation/model.ts';
import { canonical } from '../src/simulation/canonical.ts';
import { enqueue,step } from '../src/simulation/step.ts';
import { makeSave,encodeSave,parseSave } from '../src/persistence/schema.ts';
const out=resolve(process.env.ROAG_EVIDENCE_DIR??'../artifacts/colony-web/P06/raids');mkdirSync(out,{recursive:true});
const h=fortify(),{w,cmd,until,build}=h;
for(const [x,y]of [[54,66],[55,66],[54,67],[55,67]]){until(()=>placementReason(w,{x,y})===null,'stock access');cmd({kind:'stockpile',target:{x,y}});}
for(const j of w.jobs.filter(j=>j.state==='active'))cmd({kind:'priority',job:j.id,value:j.kind==='gather'?1:3});
for(const source of [8,14,16])if(w.resources.find(r=>r.id===source)?.remaining)cmd({kind:'gather',source});
until(()=>placementReason(w,{x:72,y:70})===null,'second workshop access');const oreWorkshop=build('workshop',72,70);cmd({kind:'produce',structure:oreWorkshop.id,recipe:'ore'});cmd({kind:'produce',structure:h.infirmary.id,recipe:'medicine'});
for(const [x,y]of [[60,59],[69,58],[59,69]]){until(()=>placementReason(w,{x,y})===null,'extra defense access');build('tower',x,y);}
for(const actor of [6,7])cmd({kind:'duty',actor,duty:'guard',value:3});
function issue(input){if(input.kind!=='battle'&&workCommandReason(w,input)!==null)return false;if(!enqueue(w,input))return false;h.commands.push({due:w.tick+1,input:structuredClone(input)});return true;}
function maintain(){
 for(const j of w.jobs.filter(j=>j.state==='active'&&j.priority!==(j.kind==='gather'||j.kind==='haul'?1:3)))issue({kind:'priority',job:j.id,value:j.kind==='gather'||j.kind==='haul'?1:3});
 if(w.combat.phase==='combat'){
  for(const p of w.people.filter(p=>p.life==='alive'&&p.id<6&&w.combat.orders.some(o=>o.actor===p.id)))issue({kind:'battle',actor:p.id,mode:'release',target:{x:p.x,y:p.y}});
  if(w.tick%50===0){const enemy=w.combat.raiders.filter(r=>distance(r,w.heart)<=16).sort((a,b)=>(a.kind==='healer'?-1:0)-(b.kind==='healer'?-1:0)||a.id-b.id)[0];for(const actor of [6,7]){const p=w.people.find(p=>p.id===actor);if(p.life!=='alive')continue;const injured=p.body.health<50;issue({kind:'battle',actor,mode:injured?'shelter':'rally',target:injured?{x:64,y:67}:enemy?{x:enemy.x,y:enemy.y}:{x:62,y:62}});}}
 }
 for(const s of w.structures){
  if(s.health<100&&s.kind!=='stockpile')issue({kind:'repair',structure:s.id});
  if((s.kind==='tower'&&s.inventory.ammo<=8)||(s.kind==='trap'&&s.inventory.ammo===0))issue({kind:'reload',structure:s.id});
 }
}
const milestones=[],checkpoints=[];let phase='',specials=new Set(),saveChecks=0;
const save=(label)=>{const data=makeSave(w,{manualPaused:true,speed:4});saveChecks++;const encoded=encodeSave(data);writeFileSync(resolve(out,`${label}.json`),encoded);checkpoints.push({label,encoded,tick:w.tick,world:structuredClone(w)});};
try{
 while(w.tick<40000&&!w.terminal&&w.combat.completed<5){
  if(w.tick%10===0)maintain();h.tick();
  for(const a of w.combat.attacks)if(['heal','charge','rampart','arrow'].includes(a.kind)&&!specials.has(a.kind)){specials.add(a.kind);save(`first-${a.kind}`);}
  if(w.combat.barricades.length&&!specials.has('barricades')){specials.add('barricades');save('first-barricades');}
  if(w.combat.raiders.some(r=>r.charge)&&!specials.has('mid-charge')){specials.add('mid-charge');save('mid-charge');}
  const key=`${w.combat.wave}:${w.combat.phase}`;
  if(key!==phase){phase=key;const snapshot={tick:w.tick,wave:w.combat.wave,phase:w.combat.phase,heart:w.heart.health,stats:w.combat.stats,people:w.people.map(p=>[p.id,p.life,p.body.health]),defenses:w.structures.filter(s=>s.kind==='tower').map(s=>[s.x,s.y,s.health,s.inventory.ammo])};console.log(JSON.stringify(snapshot));milestones.push(structuredClone(snapshot));save(`wave-${key.replace(':','-')}`);}
  if(w.tick%50===0){makeSave(w,{manualPaused:true,speed:4});saveChecks++;}
 }
 console.log('RESULT',w.tick,w.terminal,w.combat.completed,w.combat.stats,w.statistics);
}catch(error){writeFileSync(resolve(out,'failure-world.json'),JSON.stringify(w));throw error;}
assert.equal(w.terminal,null,'ordinary colony survives');assert.equal(w.combat.completed,5);assert.equal(w.combat.stats.bosses,1);assert.ok(w.people.every(p=>p.life==='alive'));
save('after-five-waves');
for(const actor of [6,7])cmd({kind:'duty',actor,duty:'guard',value:0});
cmd({kind:'duty',actor:3,duty:'healer',value:3});
const recoveryStart=w.tick,workBefore=w.statistics.workTicks;
while(w.tick<recoveryStart+1000&&!w.terminal&&(!w.people.every(p=>p.life==='alive'&&p.body.health===100)||w.statistics.workTicks<=workBefore)){
 if(w.tick%10===0)maintain();h.tick();if(w.tick%50===0){makeSave(w,{manualPaused:true,speed:4});saveChecks++;}
}
assert.equal(w.terminal,null);assert.ok(w.people.every(p=>p.life==='alive'&&p.body.health===100),'ordinary post-wave treatment restores every founder');assert.ok(w.statistics.workTicks>workBefore,'colony work resumes');

for(const kind of ['arrow','charge','heal','rampart','barricades','mid-charge'])assert.ok(specials.has(kind),`ordinary encounter demonstrates ${kind}`);assert.equal(w.combat.lootWood,w.combat.stats.kills+4*w.combat.stats.bosses);
save('final');
const hash=world=>createHash('sha256').update(canonical(world)).digest('hex');
const events=new Map();for(const c of h.commands){const list=events.get(c.due)??[];list.push(c.input);events.set(c.due,list);}
const advanceTo=(world,end)=>{while(world.tick<end){for(const input of events.get(world.tick+1)??[])assert.ok(enqueue(world,input),'recorded ordinary command remains legal');step(world);assert.ok(!world.terminal,'continuation remains living');}};
const replay=createWorld(42);advanceTo(replay,w.tick);assert.equal(hash(replay),hash(w),'fresh-seed command replay');
const continuations=[];
for(const checkpoint of checkpoints){
 const first=parseSave(checkpoint.encoded).world,second=structuredClone(checkpoint.world),end=Math.min(w.tick,first.tick+300);
 advanceTo(first,end);advanceTo(second,end);assert.equal(hash(first),hash(second));makeSave(first,{manualPaused:true,speed:4});saveChecks++;
 continuations.push({label:checkpoint.label,start:checkpoint.tick,end,hash:hash(first),restored:hash(second)});
}
writeFileSync(resolve(out,'raid-scenarios.json'),JSON.stringify({origin:'Fresh seed42; ordinary commands only, actual needs/timing/resources, no injected stock or buildings',milestones,specials:[...specials],commands:h.commands,continuations,saveChecks,replayHash:hash(replay),final:{tick:w.tick,terminal:w.terminal,completed:w.combat.completed,combat:w.combat.stats,statistics:w.statistics,heart:w.heart.health,people:w.people.map(p=>({id:p.id,life:p.life,health:p.body.health})),hash:hash(w)}},null,2));
console.log('ACCEPTED',w.combat.completed,'waves',checkpoints.length,'continuations',saveChecks,'save checks',hash(w));
