import test from 'node:test';
import assert from 'node:assert/strict';
import {createWorld} from '../src/simulation/world.ts';
import {enqueue,step} from '../src/simulation/step.ts';
import {spatial,index,walkable,neighbors} from '../src/simulation/model.ts';
import {materialAudit,capacity,placementReason} from '../src/simulation/jobs.ts';
import {MATERIALS,total,emptyStock,validateWorkContent,WORK_CONTENT} from '../src/simulation/work-content.ts';
import {makeSave,encodeSave,parseSave} from '../src/persistence/schema.ts';
import {canonical} from '../src/simulation/canonical.ts';
export function audit(w) {
 for(const a of Object.values(materialAudit(w)))assert.equal(a.produced,a.accounted,JSON.stringify(a));
 const positions=w.people.filter(p=>p.life!=='dead').flatMap(p=>[p,...(p.next?[p.next]:[])]).map(p=>index(p.x,p.y));assert.equal(new Set(positions).size,positions.length);
 assert.equal(spatial(w).size,positions.length);
 for(const p of w.people){assert.ok(total(p.cargo)<=5);assert.ok(!p.job||w.jobs.some(j=>j.id===p.job&&j.worker===p.id));}
 for(const s of w.structures)assert.ok(total(s.inventory)<=capacity(s));
 for(const j of w.jobs)assert.ok(j.completions<=1);
}
export function advance(w,n=1){for(let i=0;i<n;i++){const before=w.people.map(p=>({x:p.x,y:p.y}));step(w);audit(w);w.people.forEach((p,j)=>assert.ok(Math.abs(p.x-before[j].x)+Math.abs(p.y-before[j].y)<=1));}}
export function until(w,condition,limit=4000){for(let i=0;i<limit&&!condition();i++)advance(w);assert.ok(condition(),`Timed out at ${w.tick}: ${JSON.stringify(w.jobs.map(j=>({id:j.id,state:j.state,status:j.status,worker:j.worker,progress:j.progress})))}`);}
const cmd=(w,input)=>{assert.ok(enqueue(w,input),JSON.stringify(input));advance(w);};
export function setup(){const w=createWorld(42,{scenario:'work-lab'});cmd(w,{kind:'stockpile',target:{x:63,y:66}});cmd(w,{kind:'gather',source:11});cmd(w,{kind:'gather',source:15});return w;}
const stored=(w,k)=>w.structures.reduce((n,s)=>n+s.inventory[k],0);
const snapshot=w=>encodeSave(makeSave(w,{manualPaused:true,speed:4}));
function build(w,building,x,y){cmd(w,{kind:'build',building,target:{x,y}});const j=w.jobs.at(-1);until(w,()=>j.state==='done');return w.structures.find(s=>s.x===x&&s.y===y);}

test('colony content validates integer costs, capacities, durations and salvage',()=>{
 validateWorkContent();for(const mutate of [c=>c.cargoCapacity=0,c=>c.salvagePercent=101,c=>c.buildings.wall.cost.stone=-1,c=>c.buildings.door.ticks=0]){const c=structuredClone(WORK_CONTENT);mutate(c);assert.throws(()=>validateWorkContent(c));}
});
test('physical extraction, competing claims, all four buildings, repair and dismantle conserve each unit',()=>{
 const w=setup();let extracted=false,carried=false,delivered=false,reserved=false;
 until(w,()=>stored(w,'wood')>=15&&stored(w,'stone')>=12);
 for(const p of w.people)if(total(p.cargo))carried=true;
 extracted=w.resources.some(r=>r.initial!==r.remaining);delivered=stored(w,'wood')>0;
 const built=[];for(const [kind,x,y] of [['wall',68,64],['door',69,64],['storage',72,68],['workshop',74,68]]){const s=build(w,kind,x,y);built.push(s);assert.ok(s);parseSave(snapshot(w));}
 assert.ok(extracted&&carried&&delivered);
 const wall=built[0];assert.equal(walkable(w,wall),false);const door=built[1];assert.equal(walkable(w,door),true);
 cmd(w,{kind:'door',structure:door.id,open:false});assert.equal(walkable(w,door),false);cmd(w,{kind:'door',structure:door.id,open:true});assert.equal(walkable(w,door),true);
 cmd(w,{kind:'dev',action:'damage',id:wall.id});assert.equal(wall.health,50);cmd(w,{kind:'repair',structure:wall.id});const repair=w.jobs.at(-1);
 until(w,()=>{reserved ||= !!repair.reservation;return repair.state==='done';});assert.ok(reserved);assert.equal(wall.health,100);assert.ok(w.spent.stone>0);
 cmd(w,{kind:'dismantle',structure:wall.id});const dismantle=w.jobs.at(-1);until(w,()=>dismantle.state==='done');assert.ok(!w.structures.some(s=>s.id===wall.id));assert.equal(walkable(w,wall),true);assert.ok(w.piles.some(p=>p.inventory.stone>0));
 const conserved=materialAudit(w);advance(w,300);for(const k of MATERIALS)assert.ok(materialAudit(w)[k].accounted>=conserved[k].accounted);parseSave(snapshot(w));
});
test('pickup, mid-haul and timed construction each reload to exact command/state/material continuation',()=>{
 const w=setup();until(w,()=>stored(w,'stone')>=3);cmd(w,{kind:'build',building:'wall',target:{x:72,y:64}});const j=w.jobs.at(-1);
 const checkpoints=[];for(const [label,condition] of [ ['pickup',()=>!!j.reservation],['haul',()=>!!w.people.find(p=>p.job===j.id&&total(p.cargo)&&p.next)],['building',()=>j.progress>0] ]){
  until(w,condition);const copy=parseSave(snapshot(w)).world;checkpoints.push(label);assert.equal(canonical(copy),canonical(w));
  const original=structuredClone(w);for(let i=0;i<250;i++){advance(original);advance(copy);assert.equal(canonical(copy),canonical(original));assert.deepEqual(materialAudit(copy),materialAudit(original));}
 }
 assert.deepEqual(checkpoints,['pickup','haul','building']);
});
test('suspend, handoff, death and repeated cancellation leave recoverable cargo and no orphan claims',()=>{
 const w=setup();until(w,()=>stored(w,'stone')>=6);cmd(w,{kind:'build',building:'wall',target:{x:72,y:66}});const j=w.jobs.at(-1);
 until(w,()=>w.people.some(p=>p.job===j.id&&total(p.cargo)>0));const first=j.worker;
 cmd(w,{kind:'work',job:j.id,action:'suspend'});assert.equal(j.worker,null);assert.equal(j.reservation,null);assert.ok(w.piles.some(p=>total(p.inventory)>0)||w.people.some(p=>p.job&&w.jobs.find(j=>j.id===p.job)?.kind==='haul'&&total(p.cargo)>0));parseSave(snapshot(w));
 const progress=j.progress;advance(w,30);assert.equal(j.progress,progress);cmd(w,{kind:'assign',job:j.id,worker:7});cmd(w,{kind:'work',job:j.id,action:'resume'});
 until(w,()=>j.worker===7);assert.notEqual(j.worker,first);until(w,()=>total(w.people[5].cargo)>0);
 cmd(w,{kind:'dev',action:'kill',id:7});assert.equal(w.people[5].life,'dead');assert.equal(total(w.people[5].cargo),0);assert.equal(j.worker===7,false);parseSave(snapshot(w));
 until(w,()=>j.state==='done');assert.equal(j.completions,1);
 cmd(w,{kind:'build',building:'door',target:{x:74,y:66}});const cancel=w.jobs.at(-1);until(w,()=>total(cancel.site)>0||w.people.some(p=>p.job===cancel.id&&total(p.cargo)>0));
 cmd(w,{kind:'work',job:cancel.id,action:'cancel'});const before=canonical(w);assert.equal(enqueue(w,{kind:'work',job:cancel.id,action:'cancel'}),false);assert.equal(canonical(w),before);assert.equal(cancel.completions,0);parseSave(snapshot(w));
});
test('full storage retains output; missing ingredients, changed source and storage removal recover',()=>{
 const w=setup();until(w,()=>w.structures[0].inventory.wood+w.structures[0].inventory.stone===30);advance(w,300);
 assert.ok(w.jobs.some(j=>j.status.includes('Storage full')));const source=w.resources.find(r=>r.id===11),remaining=source.remaining;
 cmd(w,{kind:'dev',action:'deplete',id:11});assert.equal(source.remaining,0);assert.equal(w.sourceRemoved.wood,remaining);parseSave(snapshot(w));
 cmd(w,{kind:'stockpile',target:{x:62,y:66}});until(w,()=>w.jobs.find(j=>j.kind==='gather'&&j.ref===11).state==='done');
 cmd(w,{kind:'build',building:'workshop',target:{x:72,y:68}});const job=w.jobs.at(-1);until(w,()=>job.state==='done');
 cmd(w,{kind:'dismantle',structure:w.structures[0].id});const dis=w.jobs.at(-1);until(w,()=>dis.state==='done');audit(w);parseSave(snapshot(w));
 const empty=createWorld(1);cmd(empty,{kind:'build',building:'wall',target:{x:70,y:64}});advance(empty,40);assert.match(empty.jobs[0].status,/Missing|worker/);assert.equal(empty.jobs[0].progress,0);
});
test('bounded traffic yields around head-on workers; blocked work retries after real navigation changes',()=>{
 const w=createWorld(42,{scenario:'routing-lab'});const a=w.people[0],b=w.people[1];a.x=60;a.y=64;b.x=63;b.y=64;
 cmd(w,{kind:'move',actor:a.id,target:{x:68,y:64}});cmd(w,{kind:'move',actor:b.id,target:{x:58,y:64}});until(w,()=>!a.destination&&!b.destination,300);assert.equal(a.x,68);assert.equal(b.x,58);
 // Use a focused edit scenario, never construction fixtures as browser economy evidence.
 a.x=58;a.y=60;cmd(w,{kind:'move',actor:2,target:{x:70,y:60}});cmd(w,{kind:'edit',target:{x:64,y:60},solid:true});advance(w,200);assert.match(a.blocked,/No route/);assert.ok(a.failures<15);cmd(w,{kind:'edit',target:{x:64,y:60},solid:false});until(w,()=>a.x===70);parseSave(snapshot(w));
 const buildingWorld=setup();until(buildingWorld,()=>stored(buildingWorld,'stone')>=3);cmd(buildingWorld,{kind:'build',building:'wall',target:{x:72,y:64}});const j=buildingWorld.jobs.at(-1);
 // Deliberately enclose a job with terrain, then open one approach; all movement remains physical.
 for(const p of neighbors(j))buildingWorld.terrain[index(p.x,p.y)]=2;buildingWorld.terrainVersion++;advance(buildingWorld,150);assert.equal(j.progress,0);assert.match(j.status,/interaction|No route/);
 buildingWorld.terrain[index(j.x-1,j.y)]=1;buildingWorld.terrainVersion++;until(buildingWorld,()=>j.state==='done');audit(buildingWorld);
});
test('strict expanded saves reject forged cargo/claims/sites/IDs/versions without mutating valid input',()=>{
 const w=setup();until(w,()=>stored(w,'stone')>=3);cmd(w,{kind:'build',building:'wall',target:{x:72,y:64}});until(w,()=>!!w.jobs.at(-1).reservation);const good=snapshot(w);
 for(const mutate of [s=>s.version=1,s=>s.version=999,s=>s.world.people[0].cargo.wood++,s=>s.world.jobs.at(-1).reservation.source=99999,s=>s.world.jobs.at(-1).reservation.count=99,s=>s.world.jobs.at(-1).worker=999,s=>s.world.jobs.at(-1).completions=2,s=>s.world.jobs.at(-1).site.stone=99,s=>s.world.structures[0].inventory.food=31,s=>s.world.resources[0].remaining++]){const bad=JSON.parse(good);mutate(bad);assert.throws(()=>parseSave(JSON.stringify(bad)));assert.equal(snapshot(w),good);}
 assert.ok(placementReason(w,w.heart));assert.ok(placementReason(w,w.people[0]));assert.equal(enqueue(w,{kind:'gather',source:11}),false);
});
test('a blocked nearest interaction tile cannot cause in-flight approach oscillation',()=>{
 const w=setup();until(w,()=>stored(w,'stone')>=3);const wall=build(w,'wall',72,64);
 cmd(w,{kind:'move',actor:4,target:{x:64,y:66}});cmd(w,{kind:'move',actor:5,target:{x:71,y:64}});until(w,()=>!w.people[2].destination&&!w.people[3].destination);
 cmd(w,{kind:'dismantle',structure:wall.id});const job=w.jobs.at(-1);cmd(w,{kind:'assign',job:job.id,worker:4});until(w,()=>job.state==='done',300);assert.equal(job.completions,1);parseSave(snapshot(w));
});
test('dismantling a claimed storage releases material/capacity references and permits a physical replan',()=>{
 const w=setup();until(w,()=>stored(w,'stone')>=6);cmd(w,{kind:'build',building:'wall',target:{x:72,y:70}});const job=w.jobs.at(-1);until(w,()=>!!job.reservation);
 cmd(w,{kind:'dismantle',structure:w.structures[0].id});const dis=w.jobs.at(-1);cmd(w,{kind:'priority',job:dis.id,value:3});until(w,()=>dis.state==='done');parseSave(snapshot(w));
 cmd(w,{kind:'stockpile',target:{x:62,y:66}});until(w,()=>job.state==='done');parseSave(snapshot(w));
});
test('pending door commands retire after dismantling; dead worker cells can be reused without invalid saves',()=>{
 const w=setup();until(w,()=>stored(w,'stone')>=3&&stored(w,'wood')>=3);const door=build(w,'door',72,64);
 assert.ok(enqueue(w,{kind:'door',structure:door.id,open:false},w.tick+500));cmd(w,{kind:'dismantle',structure:door.id});const j=w.jobs.at(-1);until(w,()=>j.state==='done');assert.ok(!w.pending.some(c=>c.input.kind==='door'));parseSave(snapshot(w));
 const p=w.people[4];cmd(w,{kind:'dev',action:'kill',id:p.id});cmd(w,{kind:'stockpile',target:{x:p.x,y:p.y}});parseSave(snapshot(w));
});
test('dismantling a passable stockpile physically clears its own worker or an idle occupant',()=>{
 for(const occupant of [2,3]){
  const w=createWorld(42);cmd(w,{kind:'stockpile',target:{x:63,y:66}});cmd(w,{kind:'move',actor:occupant,target:{x:63,y:66}});until(w,()=>!w.people[occupant-2].destination);
  cmd(w,{kind:'dismantle',structure:w.structures[0].id});const j=w.jobs.at(-1);until(w,()=>j.state==='done',200);assert.equal(w.structures.length,0);parseSave(snapshot(w));
 }
});
test('import cannot reserve a future colony larger than the live placement budget',()=>{
 const w=createWorld(42),cells=[];
 for(let y=2;y<126;y++)for(let x=2;x<126;x++)if(walkable(w,{x,y})&&!spatial(w).has(index(x,y))&&!w.resources.some(r=>r.x===x&&r.y===y))cells.push({x,y});
 const empty=emptyStock;
 for(let i=0;i<200;i++)w.structures.push({...cells.shift(),id:w.nextId++,kind:'stockpile',health:100,open:true,inventory:empty(),embodied:empty()});
 for(let i=0;i<57;i++)w.jobs.push({...cells.shift(),id:w.nextId++,kind:'build',ref:null,building:'wall',state:'active',priority:2,worker:null,requested:null,progress:0,site:empty(),reservation:null,delivery:null,status:'Waiting for worker',completions:0,retryAt:0,recipe:null,stage:0,readyAt:0,cycles:0});
 assert.throws(()=>snapshot(w),/future structure budget/);w.jobs.pop();assert.doesNotThrow(()=>snapshot(w));
});
test('exhausted source plots admit new blueprints without losing their depletion ledger',()=>{
 const w=createWorld(42,{scenario:'work-lab'}),r=w.resources.find(r=>r.id===11);assert.ok(placementReason(w,r));cmd(w,{kind:'dev',action:'deplete',id:r.id});assert.equal(placementReason(w,r),null);
 cmd(w,{kind:'build',building:'wall',target:{x:r.x,y:r.y}});assert.equal(w.resources.find(s=>s.id===r.id).initial,61);assert.equal(w.sourceRemoved.wood,61);parseSave(snapshot(w));audit(w);
});
