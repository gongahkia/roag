import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
import {writeFileSync,mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
import {createWorld} from '../src/simulation/world.ts';
import {enqueue,step} from '../src/simulation/step.ts';
import {canonical} from '../src/simulation/canonical.ts';
import {materialAudit,workCommandReason} from '../src/simulation/jobs.ts';
import {makeSave,parseSave,encodeSave} from '../src/persistence/schema.ts';
import {total} from '../src/simulation/work-content.ts';
import {FixedClock} from '../src/platform/clock.ts';
const hash=w=>createHash('sha256').update(canonical(w)).digest('hex');
const evidence=resolve(import.meta.dirname,'..',process.env.ROAG_EVIDENCE_DIR??'../artifacts/colony-web/P05');mkdirSync(evidence,{recursive:true});
const logs=[];
function scenario(seed=42){const w=createWorld(seed,{scenario:'work-lab'}),commands=[],checkpoints=[];logs.push({seed,commands,checkpoints});
 const record=label=>{for(const a of Object.values(materialAudit(w)))assert.equal(a.produced,a.accounted);checkpoints.push({label,tick:w.tick,sha256:hash(w),materials:materialAudit(w),jobs:structuredClone(w.jobs),people:structuredClone(w.people)});};
 const tick=()=>{step(w);for(const a of Object.values(materialAudit(w)))assert.equal(a.produced,a.accounted);};
 const cmd=input=>{commands.push({due:w.tick+1,input});assert.ok(enqueue(w,input),JSON.stringify({input,reason:workCommandReason(w,input),people:w.people}));tick();};
 const until=(condition,label)=>{let n=0;while(!condition()&&n++<5000)tick();assert.ok(condition(),`${label} timed out`);if(label)record(label);};
 return {w,cmd,until,record,tick};}
const s=scenario(),{w,cmd,until,record,tick}=s;
cmd({kind:'stockpile',target:{x:63,y:66}});cmd({kind:'gather',source:11});cmd({kind:'gather',source:15});
until(()=>w.people.some(p=>total(p.cargo)>0),'extracted-carried');
until(()=>w.structures[0].inventory.stone>=6&&w.structures[0].inventory.wood>=10,'physically-stored');
const continuations=[];
cmd({kind:'build',building:'wall',target:{x:72,y:64}});const wallJob=w.jobs.at(-1);
for(const [label,condition] of [['pickup',()=>!!wallJob.reservation],['haul',()=>w.people.some(p=>p.job===wallJob.id&&total(p.cargo)>0&&p.next)],['construction',()=>wallJob.progress>0]]){
 until(condition,label);const save=encodeSave(makeSave(w,{manualPaused:true,speed:4}));writeFileSync(resolve(evidence,`save-${label}.json`),save+'\n');const a=structuredClone(w),b=parseSave(save).world;for(let i=0;i<300;i++){step(a);step(b);}assert.equal(hash(a),hash(b));assert.deepEqual(materialAudit(a),materialAudit(b));continuations.push({label,savedTick:w.tick,ticks:300,uninterruptedSha256:hash(a),restoredSha256:hash(b),materials:materialAudit(b)});
}
until(()=>wallJob.state==='done','wall-complete');const wall=w.structures.find(s=>s.kind==='wall');
for(const [building,x,y] of [['door',74,64],['storage',73,68],['workshop',74,70]]){cmd({kind:'build',building,target:{x,y}});const j=w.jobs.at(-1);until(()=>j.state==='done',`${building}-complete`);}
cmd({kind:'dev',action:'damage',id:wall.id});cmd({kind:'repair',structure:wall.id});const repair=w.jobs.at(-1);until(()=>repair.state==='done','repaired');
cmd({kind:'dismantle',structure:wall.id});const dis=w.jobs.at(-1);until(()=>dis.state==='done','dismantled-salvage');
cmd({kind:'build',building:'door',target:{x:74,y:72}});const cancelled=w.jobs.at(-1);until(()=>w.people.some(p=>p.job===cancelled.id&&total(p.cargo)>0),'before-cancel-cargo');cmd({kind:'work',job:cancelled.id,action:'cancel'});record('cancelled');assert.equal(enqueue(w,{kind:'work',job:cancelled.id,action:'cancel'}),false);
cmd({kind:'build',building:'wall',target:{x:72,y:72}});const interrupted=w.jobs.at(-1);until(()=>w.people.some(p=>p.job===interrupted.id&&total(p.cargo)>0),'before-suspend');cmd({kind:'work',job:interrupted.id,action:'suspend'});record('suspended');cmd({kind:'assign',job:interrupted.id,worker:7});cmd({kind:'work',job:interrupted.id,action:'resume'});until(()=>interrupted.worker===7&&total(w.people[5].cargo)>0,'replacement-cargo');cmd({kind:'dev',action:'kill',id:7});record('carrier-death');until(()=>interrupted.state==='done','replacement-complete');
for(let i=0;i<200;i++)tick();record('recovered-bundles');parseSave(encodeSave(makeSave(w,{manualPaused:true,speed:1})));
// Work commands are tick-indexed and identical across frame schedules, including simultaneous claims.
const inputs=[{due:1,input:{kind:'stockpile',target:{x:63,y:66}}},{due:1,input:{kind:'gather',source:11}},{due:1,input:{kind:'gather',source:15}},{due:2,input:{kind:'build',building:'wall',target:{x:72,y:64}}},{due:2,input:{kind:'build',building:'door',target:{x:74,y:64}}}];
function replay(cadence){const w=createWorld(42);for(const c of inputs)assert.ok(enqueue(w,c.input,c.due));const c=new FixedClock();c.setManual(false);const checkpoints=[];let frame=0;while(w.tick<1000)c.frame(cadence[frame++%cadence.length],()=>{if(w.tick<1000){step(w);if(w.tick%100===0){for(const a of Object.values(materialAudit(w)))assert.equal(a.produced,a.accounted);checkpoints.push({tick:w.tick,sha256:hash(w),rng:{...w.rng},materials:materialAudit(w)});}}});return{cadence,frames:frame,checkpoints};}
const cadences=[replay([10,17,33]),replay([250,120,7,850])];assert.deepEqual(cadences[0].checkpoints,cadences[1].checkpoints);
writeFileSync(resolve(evidence,'work-scenarios.json'),JSON.stringify({scenarios:logs,continuations,replay:{seed:42,commands:inputs,cadences}},null,2)+'\n');console.log(JSON.stringify({workCheckpoints:logs.reduce((n,s)=>n+s.checkpoints.length,0),continuations,replayCheckpoints:10}));
