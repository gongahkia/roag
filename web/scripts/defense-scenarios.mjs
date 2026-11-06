import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { mkdirSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fortify } from '../tests/helpers/defense.mjs';
import { establish, harness } from '../tests/helpers/survival.mjs';
import { canonical } from '../src/simulation/canonical.ts';
import { step } from '../src/simulation/step.ts';
import { makeSave, encodeSave, parseSave } from '../src/persistence/schema.ts';
import { materialAudit } from '../src/simulation/jobs.ts';

const dir=resolve(process.env.ROAG_EVIDENCE_DIR??'../artifacts/colony-web/P06');mkdirSync(dir,{recursive:true});
const controls={manualPaused:true,speed:4};
const hash=value=>createHash('sha256').update(canonical(value)).digest('hex');
const write=(name,value)=>writeFileSync(resolve(dir,name),JSON.stringify(value,null,2)+'\n');
let validations=0;
function validate(w){const save=makeSave(w,controls);validations++;return save;}
function tick(h){h.tick();if(h.w.tick%50===0)validate(h.w);}
function until(h,condition,label,limit=3000){for(let n=0;n<limit&&!condition()&&!h.w.terminal;n++)tick(h);assert.ok(condition(),`${label} at ${h.w.tick}: ${h.w.terminal??h.w.people.map(p=>`${p.id}:${p.life}:${p.body.health}:${p.body.status}`).join('; ')}`);}
function snapshot(w,name){const save=validate(w);writeFileSync(resolve(dir,name),encodeSave(save));return save;}
function continuation(save,n){const uninterrupted=structuredClone(save.world),restored=parseSave(encodeSave(save)).world;for(let i=0;i<n;i++){step(uninterrupted);step(restored);if(i%50===0){validate(uninterrupted);validate(restored);}}assert.equal(hash(restored),hash(uninterrupted));return {ticks:n,expected:hash(uninterrupted),restored:hash(restored),startTick:save.world.tick,endTick:restored.tick};}
function summary(w){return structuredClone({tick:w.tick,terminal:w.terminal,heart:w.heart.health,combat:w.combat.stats,completed:w.combat.completed,statistics:w.statistics,people:w.people.map(p=>({id:p.id,life:p.life,health:p.body.health})),materials:materialAudit(w),hash:hash(w)});}

// These cases start from createWorld through ordinary commands. No inventory,
// structures, health, enemy timing or needs are injected into either colony.
const prepared=fortify(),w=prepared.w,ready=summary(w),phases=[];
const firstRaidSchedule={warningAt:w.combat.warningAt,spawnAt:w.combat.spawnAt};
assert.equal(w.scenario,'colony');assert.ok(w.tick<w.combat.warningAt);assert.ok(w.structures.some(s=>s.kind==='tower'&&s.x===70&&s.y===62&&s.inventory.ammo===0),'Exposed front tower is unloaded; covering towers carry real magazines');
const cover=w.structures.filter(s=>s.kind==='tower'&&s.inventory.ammo===12);assert.equal(cover.length,3);
let phase='',mid=null,firstDamage=null;
for(let n=0;n<6000&&!w.terminal&&w.combat.completed===0;n++){
  tick(prepared);
  if(w.combat.phase!==phase){phase=w.combat.phase;phases.push({tick:w.tick,phase,edge:w.combat.edge});}
  if(!firstDamage&&w.structures.some(s=>s.health<100)){const s=w.structures.find(s=>s.health<100);firstDamage={tick:w.tick,structure:s.id,kind:s.kind,x:s.x,y:s.y,health:s.health};snapshot(w,'defense-natural-structure-damage.json');}
  if(!mid&&w.combat.phase==='combat'&&w.combat.attacks.length){const save=snapshot(w,'defense-prepared-midwave.json');mid={snapshotTick:w.tick,attacks:save.world.combat.attacks,continuation:continuation(save,250)};}
}
assert.equal(w.combat.completed,1);assert.equal(w.terminal,null);assert.equal(w.combat.stats.kills,5);assert.ok(firstDamage&&mid);
const afterRaid=summary(w),damaged=w.structures.find(s=>s.id===firstDamage.structure);
assert.ok(damaged&&damaged.health>0&&damaged.health<100,'Naturally damaged defense remains standing for physical repair');
snapshot(w,'defense-prepared-after-wave.json');
const beforeRepair={health:damaged.health,spentWood:w.spent.wood,tick:w.tick};
prepared.cmd({kind:'repair',structure:damaged.id});const repair=w.jobs.find(j=>j.kind==='repair'&&j.ref===damaged.id&&j.state==='active');assert.ok(repair);
until(prepared,()=>repair.state==='done','physical post-raid repair',3000);
assert.equal(damaged.health,100);assert.equal(w.spent.wood,beforeRepair.spentWood+1);assert.equal(repair.completions,1);
const repairEvidence={structure:damaged.id,before:beforeRepair,finishedAt:w.tick,health:damaged.health,woodSpent:w.spent.wood-beforeRepair.spentWood};
prepared.cmd({kind:'reload',structure:damaged.id});const reload=w.jobs.find(j=>j.kind==='reload'&&j.ref===damaged.id&&j.state==='active');assert.ok(reload);
until(prepared,()=>reload.state==='done','physical post-raid ammunition delivery');assert.equal(damaged.inventory.ammo,4);const reloadFinishedAt=w.tick;
// Recovery can need more than the starting four doses. Restart genuine finite-input
// production, replacing the workshop's ammunition job with ore after magazines are supplied.
const workshop=w.structures.find(s=>s.kind==='workshop'),clinic=w.structures.find(s=>s.kind==='infirmary');
const ammo=w.jobs.find(j=>j.ref===workshop.id&&j.recipe==='ammo'&&j.state==='active');if(ammo)prepared.cmd({kind:'work',job:ammo.id,action:'cancel'});
if(!w.jobs.some(j=>j.ref===workshop.id&&j.state==='active'))prepared.cmd({kind:'produce',structure:workshop.id,recipe:'ore'});
if(!w.jobs.some(j=>j.ref===clinic.id&&j.state==='active'))prepared.cmd({kind:'produce',structure:clinic.id,recipe:'medicine'});
until(prepared,()=>w.people.every(p=>p.life==='alive'&&p.body.health===100)&&w.statistics.rescues>0&&w.statistics.treatments>0&&w.statistics.recipes>afterRaid.statistics.recipes,'rescue, treatment and resumed production',3000);
assert.equal(w.statistics.deaths,0);assert.equal(w.terminal,null);
const recovered=snapshot(w,'defense-prepared-recovered.json');const recoveredContinuation=continuation(recovered,250);
const preparedResult={origin:'Fresh seed-42 colony; ordinary commands only; no injected stock/buildings/health/timing or disabled needs',ready,phases,firstDamage,mid,afterRaid,repair:repairEvidence,reload:{structure:damaged.id,completedAt:reloadFinishedAt,ammo:damaged.inventory.ammo},recovered:summary(w),continuation:recoveredContinuation,commands:prepared.commands};
console.log('Prepared colony:',JSON.stringify({ready:ready.tick,raid:afterRaid.tick,recovered:w.tick,damage:firstDamage,repair:repairEvidence,rescues:w.statistics.rescues,treatments:w.statistics.treatments,combat:w.combat.stats}));

const unprepared=establish();
assert.ok(unprepared.w.structures.every(s=>!['tower','trap','guardpost'].includes(s.kind)));
until(unprepared,()=>unprepared.w.terminal!==null,'unprepared normal-wave defeat',14000);
// The defended colony has already scheduled wave two; compare the original
// first-wave deadlines, not its moving endless-director schedule.
assert.equal(unprepared.w.combat.wave,1);assert.equal(unprepared.w.combat.edge,w.combat.edge);assert.equal(unprepared.w.combat.warningAt,firstRaidSchedule.warningAt);assert.equal(unprepared.w.combat.spawnAt,firstRaidSchedule.spawnAt);assert.equal(unprepared.w.combat.stats.spawned,5);assert.equal(unprepared.w.terminal,'heart-destroyed');
const terminal=snapshot(unprepared.w,'defense-unprepared-terminal.json'),terminalHash=hash(terminal.world),terminalRestored=parseSave(encodeSave(terminal));
for(let i=0;i<250;i++)step(terminalRestored.world);assert.equal(hash(terminalRestored.world),terminalHash);
const unpreparedResult={origin:'Fresh seed-42 colony with ordinary food/rest/care infrastructure but no defenses; no debug stock or altered raid timing',result:summary(unprepared.w),commands:unprepared.commands,terminalReload:{ticksAttempted:250,hash:terminalHash,restored:hash(terminalRestored.world)}};
console.log('Unprepared colony:',JSON.stringify({tick:unprepared.w.tick,terminal:unprepared.w.terminal,combat:unprepared.w.combat.stats}));

// Clearly isolated lifecycle fixture: visible work-lab down commands, no medication
// or rescue-capable residents. It does not stand in for ordinary balance evidence.
const lifecycle=harness('work-lab');for(const p of lifecycle.w.people)lifecycle.cmd({kind:'dev',action:'down',id:p.id});
assert.ok(lifecycle.w.people.every(p=>p.life==='downed'));assert.equal(lifecycle.w.terminal,null);
const downed=snapshot(lifecycle.w,'defense-focused-all-downed.json');until(lifecycle,()=>lifecycle.w.terminal==='all-dead','actual all-dead deadline',1000);
const dead=snapshot(lifecycle.w,'defense-focused-all-dead.json');const frozen=parseSave(encodeSave(dead));for(let i=0;i<100;i++)step(frozen.world);assert.equal(hash(frozen.world),hash(dead.world));
const lifecycleResult={origin:'Focused work-lab commands only; not ordinary balance',downedTick:downed.world.tick,downedTerminal:downed.world.terminal,deadTick:dead.world.tick,terminal:dead.world.terminal,restoredHash:hash(frozen.world),commands:lifecycle.commands};
write('defense-scenarios.json',{prepared:preparedResult,unprepared:unpreparedResult,focusedLifecycle:lifecycleResult,validation:{saveChecks:validations,cadence:'Every 50 combat/recovery ticks, at snapshots and continuation checkpoints; setup helpers additionally validate finished builds and conservation each tick'}});
console.log('Defense scenarios pass:',validations,'validated snapshots');
