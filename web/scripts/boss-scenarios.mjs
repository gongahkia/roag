// Ordinary, zero-stock colonies on the three environmental-boss seeds.
// The saved command journal can be replayed from a fresh createWorld(seed).
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {mkdirSync,writeFileSync} from 'node:fs';
import {resolve} from 'node:path';
import {fortify} from '../tests/helpers/defense.mjs';
import {rankedSources} from '../tests/helpers/survival.mjs';
import {placementReason,workCommandReason,materialAudit} from '../src/simulation/jobs.ts';
import {createWorld} from '../src/simulation/world.ts';
import {distance} from '../src/simulation/model.ts';
import {canonical} from '../src/simulation/canonical.ts';
import {enqueue,step} from '../src/simulation/step.ts';
import {makeSave,encodeSave,parseSave} from '../src/persistence/schema.ts';
import {bossCounterReason} from '../src/simulation/bosses.ts';
import {BOSS_CONTENT} from '../src/simulation/boss-content.ts';
const seed=Number(process.env.ROAG_BOSS_SEED??43),out=resolve(process.env.ROAG_EVIDENCE_DIR??`../artifacts/colony-web/P07/seed-${seed}`);
mkdirSync(out,{recursive:true});const h=fortify(seed),{w,cmd,until,build}=h;
for(const [x,y]of [[54,66],[55,66],[54,67],[55,67]]){until(()=>placementReason(w,{x,y})===null,'stock access');cmd({kind:'stockpile',target:{x,y}});}
for(const j of w.jobs.filter(j=>j.state==='active'))cmd({kind:'priority',job:j.id,value:j.kind==='gather'?1:3});
for(const kind of ['wood','stone']){
 const gathering=w.jobs.filter(j=>j.kind==='gather'&&j.state==='active'&&w.resources.find(r=>r.id===j.ref)?.kind===kind);
 if(gathering.length<2){const source=rankedSources(w,kind).find(r=>!w.jobs.some(j=>j.kind==='gather'&&j.ref===r.id&&j.state==='active'));if(source)cmd({kind:'gather',source:source.id});}
}
until(()=>placementReason(w,{x:72,y:70})===null,'second workshop access');const oreWorkshop=build('workshop',72,70);cmd({kind:'produce',structure:oreWorkshop.id,recipe:'ore'});cmd({kind:'produce',structure:h.infirmary.id,recipe:'medicine'});
for(const [x,y]of [[60,59],[69,58],[59,69]]){until(()=>placementReason(w,{x,y})===null,'extra defense access');build('tower',x,y);}
for(const actor of [6,7])cmd({kind:'duty',actor,duty:'guard',value:3});
cmd({kind:'duty',actor:3,duty:'healer',value:3});
const milestones=[],checkpoints=[],specials=new Set(),counters=new Set();let phase='',saveChecks=0;
const hash=world=>createHash('sha256').update(canonical(world)).digest('hex');
const snapshot=label=>{const data=makeSave(w,{manualPaused:true,speed:4});saveChecks++;const encoded=encodeSave(data);writeFileSync(resolve(out,`${label}.json`),encoded);checkpoints.push({label,encoded,tick:w.tick,world:structuredClone(w)});};
function issue(input){if(input.kind!=='battle'&&workCommandReason(w,input)!==null)return false;if(!enqueue(w,input))return false;h.commands.push({due:w.tick+1,input:structuredClone(input)});return true;}
function maintain(){
 for(const j of w.jobs.filter(j=>j.state==='active'&&j.priority!==(j.kind==='gather'||j.kind==='haul'?1:3)))issue({kind:'priority',job:j.id,value:j.kind==='gather'||j.kind==='haul'?1:3});
 // Gathering is continuous work: reserve limits release workers for food,
 // medicine and reloads instead of filling every stockpile indefinitely.
 for(const kind of ['wood','stone']){
  const stored=[...w.structures,...w.piles].reduce((sum,s)=>sum+s.inventory[kind],0)+w.people.reduce((sum,p)=>sum+p.cargo[kind],0);
  const jobs=w.jobs.filter(j=>j.kind==='gather'&&['active','suspended'].includes(j.state)&&w.resources.find(r=>r.id===j.ref)?.kind===kind);
  const active=jobs.filter(j=>j.state==='active');
  if(stored>=35){for(const j of active)issue({kind:'work',job:j.id,action:'suspend'});}
  else if(stored<15&&active.length===0){
   const existing=jobs.find(j=>j.state==='suspended'&&w.resources.find(r=>r.id===j.ref)?.remaining>0);
   if(existing)issue({kind:'work',job:existing.id,action:'resume'});
   else {const source=rankedSources(w,kind).find(r=>!jobs.some(j=>j.ref===r.id));if(source)issue({kind:'gather',source:source.id});}
  }
  if(active.length>1)for(const j of active.slice(1))issue({kind:'work',job:j.id,action:'suspend'});
 }
 if(w.combat.phase==='combat'){
  for(const p of w.people.filter(p=>p.life==='alive'&&p.id<6&&w.combat.orders.some(o=>o.actor===p.id)))issue({kind:'battle',actor:p.id,mode:'release',target:{x:p.x,y:p.y}});
  if(w.tick%20===0){
   const encounter=w.bosses.encounters.find(e=>e.deadAt===null),boss=w.combat.raiders.find(r=>r.id===encounter?.owner);
   const enemy=w.combat.raiders.filter(r=>distance(r,w.heart)<=16).sort((a,b)=>(a.kind==='healer'?-1:0)-(b.kind==='healer'?-1:0)||a.id-b.id)[0];
   for(const actor of [6,7]){
    const p=w.people.find(p=>p.id===actor);if(p.life!=='alive')continue;const order=w.combat.orders.find(o=>o.actor===actor);
    if(p.body.health<50){if(order?.mode!=='shelter')issue({kind:'battle',actor,mode:'shelter',target:{x:61,y:65}});continue;}
    if(order&&['strike','drain','cut','ignite'].includes(order.mode)&&bossCounterReason(w,order.mode,order.target)===null)continue;
    // Keep the counter worker at the announced valve while the escort comes
    // into tower range; chasing an entry-edge boss abandons colony cover.
    if(boss&&encounter.kind==='sluice'&&!specials.has('feast')){
     const valve=w.bosses.features.find(f=>f.owner===boss.id&&f.kind==='valve');
     const target=actor===6&&valve?{x:valve.x,y:valve.y}:{x:62,y:62};
     if(!order||order.mode!==(actor===6?'shelter':'rally')||order.target.x!==target.x||order.target.y!==target.y)issue({kind:'battle',actor,mode:actor===6?'shelter':'rally',target});
     continue;
    }
    let feature=null,mode=null;
    if(boss&&actor===6){
     if(encounter.kind==='bellkeeper'&&specials.has('ring')){feature=w.bosses.features.filter(f=>f.owner===boss.id&&f.kind==='resonator').sort((a,b)=>distance(p,a)-distance(p,b)||a.id-b.id)[0];mode='strike';}
     else if(encounter.kind==='sluice'&&specials.has('feast')){feature=w.bosses.features.filter(f=>f.owner===boss.id&&f.kind==='valve'&&!encounter.drained[f.channel]).sort((a,b)=>distance(p,a)-distance(p,b)||a.id-b.id)[0];mode='drain';}
     else if(encounter.kind==='threadmother'&&specials.has('reel')){feature=w.bosses.features.filter(f=>f.owner===boss.id&&f.kind==='web'&&bossCounterReason(w,'ignite',f)===null).sort((a,b)=>distance(p,a)-distance(p,b)||a.id-b.id)[0];mode='ignite';}
    }
    if(feature&&mode)issue({kind:'battle',actor,mode,target:{x:feature.x,y:feature.y}});
    else issue({kind:'battle',actor,mode:'rally',target:boss?{x:boss.x,y:boss.y}:enemy?{x:enemy.x,y:enemy.y}:{x:62,y:62}});
   }
  }
 }
 for(const s of w.structures){if(s.health<100&&s.kind!=='stockpile')issue({kind:'repair',structure:s.id});if((s.kind==='tower'&&s.inventory.ammo<=8)||(s.kind==='trap'&&s.inventory.ammo===0))issue({kind:'reload',structure:s.id});}
}
try{
 while(w.tick<35000&&!w.terminal&&w.combat.completed<4){
  if(w.tick%10===0)maintain();
  const finishing=w.bosses.work.filter(work=>work.progress===BOSS_CONTENT.counters[work.mode]-1).map(work=>({work:structuredClone(work),feature:structuredClone(w.bosses.features.find(f=>f.id===work.target))}));
  h.tick();
  for(const {work,feature}of finishing){const after=w.bosses.features.find(f=>f.id===feature.id),e=w.bosses.encounters.find(e=>e.owner===feature.owner);if(work.mode==='drain'&&e?.drained[feature.channel]||work.mode==='ignite'&&w.bosses.effects.some(e=>e.kind==='fire'&&e.source===work.actor)||work.mode==='strike'&&e&&(!after||after.health<feature.health))counters.add(work.mode);}
  for(const effect of w.bosses.effects)if(!specials.has(effect.kind)){specials.add(effect.kind);snapshot(`first-${effect.kind}`);}
  for(const work of w.bosses.work)if(!specials.has(`work-${work.mode}`)){specials.add(`work-${work.mode}`);snapshot(`mid-${work.mode}`);}
  const key=`${w.combat.wave}:${w.combat.phase}`;if(key!==phase){phase=key;const value={tick:w.tick,wave:w.combat.wave,phase:w.combat.phase,heart:w.heart.health,people:w.people.map(p=>[p.id,p.life,p.body.health]),bosses:w.bosses.encounters.map(e=>({kind:e.kind,center:e.layout.center})),stats:structuredClone(w.combat.stats)};milestones.push(value);console.log(JSON.stringify(value));snapshot(`wave-${key.replace(':','-')}`);}
  if(w.tick%50===0){makeSave(w,{manualPaused:true,speed:4});saveChecks++;}
 }
 assert.equal(w.terminal,null);assert.equal(w.combat.completed,4);assert.equal(w.combat.stats.bosses,1);assert.ok(w.people.every(p=>p.life==='alive'),'all six founders survive');
 const expected=seed%4===3?'strike':seed%4===0?'drain':'ignite';assert.ok(counters.has(expected),`ordinary paid ${expected} counter`);
 for(const actor of [6,7])cmd({kind:'duty',actor,duty:'guard',value:0});
 const recoveryStart=w.tick,workBefore=w.statistics.workTicks;
 while(w.tick<recoveryStart+1100&&!w.terminal&&(!w.people.every(p=>p.life==='alive'&&p.body.health===100)||w.statistics.workTicks<=workBefore)){if(w.tick%10===0)maintain();h.tick();}
 assert.equal(w.terminal,null);assert.ok(w.people.every(p=>p.life==='alive'&&p.body.health===100),'post-boss actual treatment');assert.ok(w.statistics.workTicks>workBefore);snapshot('final');
 const events=new Map();for(const c of h.commands){const list=events.get(c.due)??[];list.push(c.input);events.set(c.due,list);}
 const advanceTo=(world,end)=>{while(world.tick<end){for(const input of events.get(world.tick+1)??[])assert.ok(enqueue(world,input));step(world);}};
 const replay=createWorld(seed);advanceTo(replay,w.tick);assert.equal(hash(replay),hash(w),'fresh seeded replay');
 const continuations=[];for(const checkpoint of checkpoints){const restored=parseSave(checkpoint.encoded).world,original=structuredClone(checkpoint.world),end=Math.min(w.tick,restored.tick+200);advanceTo(restored,end);advanceTo(original,end);assert.equal(hash(restored),hash(original));makeSave(restored,{manualPaused:true,speed:4});saveChecks++;continuations.push({label:checkpoint.label,start:checkpoint.tick,end,hash:hash(restored)});}
 writeFileSync(resolve(out,'boss-scenarios.json'),JSON.stringify({origin:`Fresh seed${seed}; ordinary commands only, no supplied inventory/structures/disabled needs`,seed,biome:w.environment.biome,milestones,specials:[...specials],counters:[...counters],commands:h.commands,continuations,saveChecks,replayHash:hash(replay),final:{tick:w.tick,heart:w.heart.health,people:w.people,combat:w.combat.stats,statistics:w.statistics,materials:materialAudit(w),hash:hash(w)}},null,2));console.log('ACCEPTED',seed,w.tick,[...specials],[...counters],hash(w));
}catch(error){writeFileSync(resolve(out,'failure-world.json'),JSON.stringify(w));writeFileSync(resolve(out,'failure-commands.json'),JSON.stringify(h.commands));console.log('FAILED DETAILS',seed,w.tick,[...specials],[...counters],w.people.map(p=>[p.id,p.life,p.body.health]),w.bosses);throw error;}
