import test from 'node:test';
import assert from 'node:assert/strict';
import { createWorld } from '../src/simulation/world.ts';
import { enqueue, step } from '../src/simulation/step.ts';
import { advanceCombat, applyCombatCommand, prepareCombat, resolveCombatImpacts } from '../src/simulation/combat.ts';
import { COMBAT_CONTENT as C } from '../src/simulation/combat-content.ts';
import { shotCells, clearShot } from '../src/simulation/combat-geometry.ts';
import { distance, index, same, spatial, walkable } from '../src/simulation/model.ts';
import { destroyStructure } from '../src/simulation/jobs.ts';
import { emptyStock, WORK_CONTENT } from '../src/simulation/work-content.ts';
import { NEEDS } from '../src/simulation/survival-content.ts';

// Explicit isolated combat fixtures. These do not count as the ordinary playable/balance run.
function fixture() {
  const w=createWorld(42,{scenario:'work-lab'});
  for(let y=50;y<80;y++)for(let x=50;x<85;x++)w.terrain[index(x,y)]=1;
  w.people.forEach((p,i)=>{p.x=30+i*2;p.y=30;for(const duty of Object.keys(p.body.duties))p.body.duties[duty]=0;});
  return w;
}
function raider(w,x,y,kind='raider') {
  const r={id:w.nextId++,kind,health:C[kind].health,x,y,next:null,progress:0,route:[],routeVersion:w.terrainVersion,retryAt:w.tick,cooldown:0,target:null};
  w.combat.phase='combat';w.combat.wave=1;w.combat.raiders.push(r);w.combat.stats.spawned++;return r;
}
function building(w,x,y,kind='tower',ammo=0) {
  const s={id:w.nextId++,x,y,kind,health:100,open:false,inventory:emptyStock(),embodied:{...WORK_CONTENT.buildings[kind].cost}};
  s.inventory.ammo=ammo;w.structures.push(s);return s;
}
function attack(w,source,target,{kind='melee',faction='hostile',damage=12,due=w.tick+1,origin=source}={}) {
  const ref=source.kind==='tower'||source.kind==='trap'?{kind:'structure',id:source.id}:w.people.includes(source)?{kind:'person',id:source.id}:{kind:'raider',id:source.id};
  const a={id:w.nextId++,kind,source:ref,origin:{x:origin.x,y:origin.y},target:{x:target.x,y:target.y},cells:kind==='bolt'?shotCells(w,origin,target):[{x:target.x,y:target.y}],due,damage,faction};w.combat.attacks.push(a);return a;
}
function ticks(w,n){for(let i=0;i<n&&!w.terminal;i++)step(w);}

test('geometry includes solid corners; friendly bodies intercept without friendly damage',()=>{
  const w=fixture(),s=building(w,60,60),r=raider(w,64,60),p=w.people[0];
  p.x=62;p.y=60;assert.equal(clearShot(w,s,r,r.id),false);
  attack(w,s,r,{kind:'bolt',faction:'colony',damage:C.tower.damage});w.tick++;resolveCombatImpacts(w);
  assert.equal(p.body.health,100);assert.equal(r.health,C.raider.health);
  w.terrain[index(61,60)]=2;assert.deepEqual(shotCells(w,s,{x:63,y:63}),[{x:61,y:60}]);
});

test('locked melee and bolt targets do not follow a moving target; new blockers stop impact',()=>{
  const w=fixture(),r=raider(w,60,60),p=w.people[0];p.x=61;p.y=60;
  const a=attack(w,r,p);p.y=61;w.tick++;resolveCombatImpacts(w);assert.equal(p.body.health,100);assert.deepEqual(a.target,{x:61,y:60});
  const s=building(w,60,64);r.x=65;r.y=64;attack(w,s,r,{kind:'bolt',faction:'colony',damage:18});building(w,62,64,'wall');w.tick++;resolveCombatImpacts(w);assert.equal(r.health,C.raider.health);
});

test('destroyed tower cancels its pending shot without refunding spent ammunition',()=>{
  const w=fixture(),s=building(w,60,60,'tower',1),r=raider(w,65,60);advanceCombat(w);
  assert.equal(w.combat.stats.shots,1);assert.equal(w.consumed.ammo,1);assert.equal(w.combat.attacks.filter(a=>a.source.id===s.id).length,1);
  destroyStructure(w,s);ticks(w,10);assert.equal(r.health,C.raider.health);assert.equal(w.consumed.ammo,1);assert.equal(w.combat.stats.structuresLost,1);
});

test('tower magazines are finite and raider death pays exactly one physical loot unit',()=>{
  const w=fixture(),s=building(w,60,60,'tower',2),r=raider(w,65,60);r.retryAt=1000;
  for(let i=0;i<25;i++){w.tick++;resolveCombatImpacts(w);advanceCombat(w);}
  assert.equal(w.combat.stats.shots,2);assert.equal(s.inventory.ammo,0);assert.equal(w.consumed.ammo,2);assert.equal(w.combat.stats.kills,1);assert.equal(w.combat.lootWood,1);assert.equal(w.produced.wood,1);assert.equal(w.piles.reduce((n,p)=>n+p.inventory.wood,0),1);
  const second=raider(w,65,60);second.retryAt=1000;for(let i=0;i<40;i++){w.tick++;resolveCombatImpacts(w);advanceCombat(w);}
  assert.equal(w.combat.stats.shots,2);assert.equal(second.health,C.raider.health);assert.equal(w.combat.lootWood,1);
});

test('a physical trap charge fires once and only after an attacker reaches its cell',()=>{
  const w=fixture(),s=building(w,60,60,'trap',1),r=raider(w,59,60);r.next={x:60,y:60};r.progress=0;
  advanceCombat(w);assert.equal(w.combat.stats.traps,0);
  for(let i=0;i<12;i++){w.tick++;resolveCombatImpacts(w);advanceCombat(w);}
  assert.equal(w.combat.stats.traps,1);assert.equal(s.inventory.ammo,0);assert.equal(w.consumed.ammo,1);assert.equal(r.health,C.raider.health-C.trap.damage);
});

test('lethal impacts cancel enemy windup and do not repay death on repeated resolution',()=>{
  const w=fixture(),s=building(w,60,60),r=raider(w,62,60),p=w.people[0];p.x=63;p.y=60;
  attack(w,s,r,{kind:'bolt',faction:'colony',damage:99});attack(w,r,p);
  w.tick++;resolveCombatImpacts(w);resolveCombatImpacts(w);
  assert.equal(p.body.health,100);assert.equal(w.combat.stats.kills,1);assert.equal(w.combat.lootWood,1);assert.equal(w.combat.attacks.length,0);
});

test('an enclosed heart is reached by destroying a player obstruction, never by spawning inside',()=>{
  const w=fixture();for(let y=63;y<=65;y++)for(let x=63;x<=65;x++)if(x!==64||y!==64)building(w,x,y,'wall');
  const r=raider(w,68,64,'breacher');ticks(w,250);
  assert.ok(w.combat.stats.structuresLost>=1);assert.ok(w.heart.health<100);assert.ok(distance(r,w.heart)<=1);assert.equal(w.terminal,'heart-destroyed');
});

test('unfinished blueprint enclosures are breachable and cannot stop a raid forever',()=>{
  const w=fixture();for(let y=63;y<=65;y++)for(let x=63;x<=65;x++)if(x!==64||y!==64)assert.ok(enqueue(w,{kind:'build',building:'wall',target:{x,y}}));
  step(w);raider(w,68,64,'breacher');ticks(w,180);
  assert.ok(w.jobs.some(j=>j.kind==='build'&&j.state==='cancelled'));assert.ok(w.heart.health<100);
});

test('natural stone is never breached and blocked routes remain bounded',()=>{
  const w=fixture();for(let y=63;y<=65;y++)for(let x=63;x<=65;x++)if(x!==64||y!==64)w.terrain[index(x,y)]=2;
  raider(w,68,64,'breacher');ticks(w,300);assert.equal(w.heart.health,100);assert.equal(w.combat.stats.structuresLost,0);assert.equal(w.terrain[index(65,64)],2);
});

test('raid edge is selected once and blocked legal edge spawns remain pending',()=>{
  const w=fixture();w.scenario='colony';w.tick=C.warningAt;advanceCombat(w);assert.equal(w.combat.phase,'warning');assert.equal(w.combat.edge,'east');const rng=w.rng.raids;
  for(let y=1;y<=126;y++)w.terrain[index(126,y)]=2;
  w.tick=C.spawnAt;advanceCombat(w);assert.equal(w.combat.raiders.length,0);assert.equal(w.combat.pending.length,5);assert.equal(w.rng.raids,rng);
  w.terrain[index(126,64)]=1;w.tick++;advanceCombat(w);assert.equal(w.combat.raiders.length,1);assert.equal(w.combat.raiders[0].x,126);assert.equal(w.combat.pending.length,4);assert.equal(w.rng.raids,rng);
});

test('needs-induced downing cancels an unresolved guard windup in the same tick',()=>{
  const w=fixture(),p=w.people[0],r=raider(w,61,60);p.x=60;p.y=60;p.body.health=1;p.body.hunger=NEEDS.starveAt;w.tick=19;
  attack(w,p,r,{faction:'colony',due:22});step(w);
  assert.equal(p.life,'downed');assert.equal(w.combat.attacks.some(a=>a.source.id===p.id&&a.source.kind==='person'),false);
});

test('group shelter chooses stable distinct reachable tiles and releasing during care restores prior intent',()=>{
  const w=fixture();w.people.forEach((p,i)=>{p.x=59+i;p.y=60;});
  applyCombatCommand(w,{kind:'battle',actor:0,mode:'shelter',target:{x:64,y:65}});ticks(w,180);
  assert.ok(w.people.every(p=>distance(p,{x:64,y:65})<=3&&!p.destination&&!p.next),JSON.stringify(w.people.map(p=>({id:p.id,x:p.x,y:p.y,d:p.destination,b:p.blocked}))));
  assert.equal(new Set(w.people.map(p=>index(p.x,p.y))).size,6);
  const p=w.people[0];applyCombatCommand(w,{kind:'battle',actor:p.id,mode:'release',target:{x:64,y:65}});p.destination={x:80,y:80};
  applyCombatCommand(w,{kind:'battle',actor:p.id,mode:'shelter',target:{x:64,y:65}});
  p.body.routine={kind:'eat',facility:null,patient:null,reservation:null,cargo:emptyStock(),progress:0,status:'Eating',resume:{x:64,y:65}};
  applyCombatCommand(w,{kind:'battle',actor:p.id,mode:'release',target:{x:64,y:65}});assert.deepEqual(p.body.routine.resume,{x:80,y:80});
});

test('automatic shelter is issued once; a player release is not recreated every tick',()=>{
  const w=fixture();w.scenario='colony';w.combat.phase='warning';w.combat.wave=1;w.combat.pending=['raider'];w.tick=C.spawnAt;advanceCombat(w);assert.equal(w.combat.orders.length,6);
  applyCombatCommand(w,{kind:'battle',actor:0,mode:'release',target:{x:64,y:65}});prepareCombat(w);advanceCombat(w);assert.equal(w.combat.orders.length,0);
});

test('mid-wave cloned continuation preserves all movement, impacts, RNG and loot',()=>{
  const w=fixture();building(w,69,64,'tower',12);building(w,70,66,'tower',12);raider(w,76,64,'breacher');raider(w,74,66);ticks(w,7);
  assert.ok(w.combat.raiders.some(r=>r.next)||w.combat.attacks.length);const restored=structuredClone(w);
  ticks(w,180);ticks(restored,180);assert.deepEqual(restored,w);
  assert.equal(spatial(w).size,w.people.filter(p=>p.life!=='dead').reduce((n,p)=>n+1+(p.next?1:0),0)+w.combat.raiders.reduce((n,r)=>n+1+(r.next?1:0),0));
  assert.ok(w.combat.raiders.every(r=>walkable(w,r)&&(!r.next||walkable(w,r.next))));
});

test('a reserved movement cell never permits melee against a body two tiles away',()=>{
  const w=fixture(),r=raider(w,70,67),p=w.people[0];p.x=68;p.y=67;p.next={x:69,y:67};p.progress=0;p.destination={x:69,y:67};
  r.target={kind:'person',id:p.id};r.route=[{x:69,y:67}];r.routeVersion=w.terrainVersion;
  advanceCombat(w);assert.equal(w.combat.attacks.filter(a=>a.source.id===r.id).length,0);
});

test('breachers pressure nearby towers with stable ties while ordinary raiders pursue the heart',()=>{
  const w=fixture(),first=building(w,68,64,'tower',0);building(w,69,63,'tower',0);const r=raider(w,70,64,'breacher');
  w.structures.reverse();advanceCombat(w);assert.deepEqual(r.target,{kind:'structure',id:first.id});assert.equal(r.health,72,'No health inflation accompanies the targeting policy');
  const ordinary=fixture();building(ordinary,68,64,'tower',0);const other=raider(ordinary,70,64);advanceCombat(ordinary);assert.deepEqual(other.target,{kind:'heart',id:ordinary.heart.id});
});

test('automatic rally stores only coordinates when its destination is a guard post',()=>{
 const w=fixture(),p=w.people[0];p.body.duties.guard=3;const post=building(w,62,62,'guardpost');
 w.scenario='colony';w.tick=C.warningAt;advanceCombat(w);w.tick=C.spawnAt;advanceCombat(w);
 const order=w.combat.orders.find(o=>o.actor===p.id);assert.equal(order.mode,'rally');assert.deepEqual(order.target,{x:post.x,y:post.y});assert.deepEqual(Object.keys(order.target).sort(),['x','y']);
});
