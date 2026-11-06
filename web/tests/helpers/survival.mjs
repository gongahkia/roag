import assert from 'node:assert/strict';
import { createWorld } from '../../src/simulation/world.ts';
import { enqueue,step } from '../../src/simulation/step.ts';
import { materialAudit,workCommandReason } from '../../src/simulation/jobs.ts';
import { makeSave } from '../../src/persistence/schema.ts';
export const scenarioRuns=[];
export function harness(scenario='colony',initial=null) {
 const w=initial??createWorld(42,{scenario}),commands=[];scenarioRuns.push({seed:w.seed,scenario:w.scenario,initialTick:w.tick,commands});
 const audit=()=>{for(const [k,a]of Object.entries(materialAudit(w)))assert.equal(a.produced,a.accounted,k);};
 const tick=(n=1)=>{for(let i=0;i<n;i++){const before=w.people.map(p=>({x:p.x,y:p.y}));step(w);audit();w.people.forEach((p,j)=>assert.ok(Math.abs(p.x-before[j].x)+Math.abs(p.y-before[j].y)<=1,'physical movement'));}};
 const cmd=input=>{assert.ok(enqueue(w,input),JSON.stringify({input,reason:workCommandReason(w,input)}));commands.push({due:w.tick+1,input:structuredClone(input)});tick();};
 const until=(condition,label,limit=2500)=>{for(let i=0;i<limit&&!condition();i++)tick();assert.ok(condition(),`${label} timeout ${w.tick}: ${JSON.stringify({people:w.people.map(p=>({id:p.id,job:p.job,pos:[p.x,p.y],next:p.next,body:p.body})),jobs:w.jobs.filter(j=>j.state==='active')})}`);};
 const build=(building,x,y)=>{cmd({kind:'build',building,target:{x,y}});const j=w.jobs.at(-1);until(()=>j.state==='done',building);makeSave(w,{manualPaused:true,speed:4});return w.structures.find(s=>s.x===x&&s.y===y);};
 return {w,commands,audit,tick,cmd,until,build};
}
export function establish(scenario='colony') {
 const h=harness(scenario),{w,cmd,build,until}=h;
 for(const x of [63,62,61,60])cmd({kind:'stockpile',target:{x,y:66}});
 cmd({kind:'gather',source:11});cmd({kind:'gather',source:15});
 const farm=build('farm',68,64);cmd({kind:'produce',structure:farm.id,recipe:'farm'});
 const kitchen=build('kitchen',70,64);cmd({kind:'produce',structure:kitchen.id,recipe:'meal'});
 for(const x of [66,68,70])build('bed',x,68);
 const infirmary=build('infirmary',72,68),workshop=build('workshop',74,68);
 cmd({kind:'produce',structure:workshop.id,recipe:'ore'});
 cmd({kind:'produce',structure:infirmary.id,recipe:'medicine'});
 until(()=>w.recipeCounts.medicine>=2,'medicine production');
 // A finite recipe order is stopped through the same ordinary job control as UI.
 const medicine=w.jobs.find(j=>j.recipe==='medicine'&&j.state==='active');cmd({kind:'work',job:medicine.id,action:'cancel'});
 until(()=>w.structures.some(s=>s.inventory.ore>=1),'spare ore');const ore=w.jobs.find(j=>j.recipe==='ore'&&j.state==='active');cmd({kind:'work',job:ore.id,action:'cancel'});
 cmd({kind:'produce',structure:workshop.id,recipe:'tool'});until(()=>w.recipeCounts.tool>=1,'tool production');cmd({kind:'work',job:w.jobs.find(j=>j.recipe==='tool'&&j.state==='active').id,action:'cancel'});
 return {...h,farm,kitchen,infirmary,workshop};
}
