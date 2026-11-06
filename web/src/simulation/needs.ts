import { distance } from './model.ts';
import type { Person, Routine, World } from './model.ts';
import { NEEDS as N } from './survival-content.ts';
import { emptyStock } from './work-content.ts';
import { approach, available, clearTravel, drop, inventory, release } from './jobs.ts';

// A Captain can down a guard after this tick's prepareCombat pass. Retire
// life-bound intent immediately so every public tick remains saveable.
function retireBattleIntent(w:World,p:Person):void {
  w.combat.orders=w.combat.orders.filter(o=>o.actor!==p.id);
  w.combat.cooldowns=w.combat.cooldowns.filter(c=>c.actor!==p.id);
  w.combat.attacks=w.combat.attacks.filter(a=>a.source.kind!=='person'||a.source.id!==p.id);
}

export function endRoutine(w:World,p:Person):void {
  const r=p.body.routine;if(!r)return;drop(w,p,r.cargo);const resume=r.resume;p.body.routine=null;clearTravel(p);
  if(!p.job&&resume&&p.life==='alive')p.destination=resume;
}
function begin(w:World,p:Person,kind:Routine['kind']):Routine {
  const resume=p.body.routine?.resume??(!p.job&&p.destination?{...p.destination}:null);
  endRoutine(w,p);clearTravel(p);
  const r:Routine={kind,facility:null,patient:null,reservation:null,cargo:emptyStock(),progress:0,status:'Seeking access',resume};p.body.routine=r;return r;
}
export function die(w:World,p:Person):void {
  if(p.life==='dead')return;endRoutine(w,p);
  retireBattleIntent(w,p);
  if(p.job){const j=w.jobs.find(j=>j.id===p.job)!;release(w,j);j.requested=null;}else drop(w,p,p.cargo);
  p.life='dead';p.body.health=0;p.body.deathAt=0;p.body.recoverAt=0;p.next=null;p.progress=0;clearTravel(p);p.body.status='Dead';w.statistics.deaths++;
  for(const helper of w.people)if(helper.body.routine?.patient===p.id)endRoutine(w,helper);
}
export function fall(w:World,p:Person):void {
  if(p.life!=='alive')return;endRoutine(w,p);
  retireBattleIntent(w,p);
  if(p.job){const j=w.jobs.find(j=>j.id===p.job)!;release(w,j);j.requested=null;}else drop(w,p,p.cargo);
  p.life='downed';p.body.health=0;p.body.deathAt=w.tick+N.rescueWindow;p.body.recoverAt=0;p.next=null;p.progress=0;clearTravel(p);
  p.body.status=`Downed; medicine and rescue before tick ${p.body.deathAt}`;
}
function chooseFacility(w:World,p:Person,r:Routine,kind:'bed'|'infirmary'):boolean {
  const unclaimed=w.structures.filter(s=>s.kind===kind&&!w.people.some(other=>other!==p&&other.body.routine?.facility===s.id));
  let s=unclaimed.find(s=>s.id===r.facility);
  if(!s){s=unclaimed.sort((a,b)=>distance(p,a)-distance(p,b)||a.id-b.id)[0];r.facility=s?.id??null;r.progress=0;}
  if(!s){r.status=`No free ${kind}; build one or wait for its claim`;return false;}
  return approach(w,r,p,s,`Reaching claimed ${kind}`);
}
function obtain(w:World,p:Person,r:Routine,kind:'meal'|'food'|'medicine'):boolean {
  if(r.cargo[kind]>0)return true;
  if(r.reservation&&(!inventory(w,r.reservation.source)||available(w,r.reservation.source,r.reservation.kind)<0)){r.reservation=null;r.progress=0;}
  if(!r.reservation){
    const source=[...w.structures,...w.piles].filter(s=>available(w,s.id,kind)>0).sort((a,b)=>distance(p,a)-distance(p,b)||a.id-b.id)[0];
    if(!source){r.status=`No unreserved ${kind}; gather or produce and deliver supplies`;return false;}
    r.reservation={source:source.id,kind,count:1};
  }
  const claim=r.reservation,source=w.structures.find(s=>s.id===claim.source)??w.piles.find(s=>s.id===claim.source);
  if(!source){r.reservation=null;return false;}
  if(!approach(w,r,p,source,`Collecting claimed ${claim.kind}`))return false;
  inventory(w,source.id)![claim.kind]--;r.cargo[claim.kind]++;r.reservation=null;return true;
}
function hasFood(w:World):boolean {return [...w.structures,...w.piles].some(s=>available(w,s.id,'meal')>0||available(w,s.id,'food')>0);}
function consume(w:World,r:Routine,kind:'meal'|'food'|'medicine'):void {r.cargo[kind]--;w.consumed[kind]++;}
function care(w:World,p:Person,r:Routine):void {
  if(r.kind==='eat'){
    const kind=r.cargo.meal||r.reservation?.kind==='meal'?'meal':r.cargo.food||r.reservation?.kind==='food'?'food':[...w.structures,...w.piles].some(s=>available(w,s.id,'meal')>0)?'meal':'food';
    if(!obtain(w,p,r,kind))return;
    if(p.next)return;r.status=`Eating ${kind}`;if(++r.progress<N.eatTicks)return;
    consume(w,r,kind);if(kind==='meal')w.statistics.eatenMeals++;else w.statistics.eatenFood++;p.body.hunger=Math.max(0,p.body.hunger-(kind==='meal'?N.mealReduction:N.rawReduction));p.body.meals++;endRoutine(w,p);return;
  }
  if(r.kind==='rest'){
    if(!chooseFacility(w,p,r,'bed'))return;r.status='Resting in claimed bed';p.body.fatigue=Math.max(0,p.body.fatigue-N.restReduction);r.progress++;
    if(p.body.fatigue<=N.restTarget){p.body.rests++;endRoutine(w,p);}return;
  }
  if(r.kind==='treatment'){
    if(!chooseFacility(w,p,r,'infirmary'))return;
    r.status=p.body.recoverAt>w.tick?`Recovering until tick ${p.body.recoverAt}`:'At infirmary; waiting for healer and medicine';
    if(p.body.health===100&&p.body.recoverAt<=w.tick){p.body.recoverAt=0;endRoutine(w,p);}return;
  }
  if(r.kind==='rescue'){
    const patient=w.people.find(a=>a.id===r.patient);
    if(!patient||patient.life==='dead'||patient.life==='alive'&&patient.body.health===100){endRoutine(w,p);return;}
    if(!obtain(w,p,r,'medicine'))return;
    if(patient.life==='alive'){
      if(patient.body.routine?.kind!=='treatment'){endRoutine(w,p);return;}
      const facility=w.structures.find(s=>s.id===patient.body.routine?.facility&&s.kind==='infirmary');
      if(!facility||patient.next||distance(patient,facility)>1){r.status='Waiting for patient to reach infirmary';return;}
    }
    if(!approach(w,r,p,patient,patient.life==='downed'?'Reaching downed patient':'Reaching infirmary patient'))return;
    r.status=patient.life==='downed'?'Stabilizing with medicine':'Treating with medicine';
    if(++r.progress<(patient.life==='downed'?N.rescueTicks:N.treatmentTicks))return;
    consume(w,r,'medicine');
    if(patient.life==='downed'){
      patient.life='alive';patient.body.health=40;patient.body.deathAt=0;
      // Stabilization permits the patient to walk to treatment; never move their coordinates remotely.
      w.statistics.rescues++;patient.body.status='Stabilized; seeking infirmary';
    }else{patient.body.health=100;patient.body.recoverAt=w.tick+N.recoveryTicks;w.statistics.treatments++;}
    endRoutine(w,p);return;
  }
  const target={x:60+p.founder,y:67};approach(w,r,p,target,'Walking to watch post');if(!p.destination&&!p.next)r.status='Watching the heart (noncombat)';
}
export function advanceNeeds(w:World):void {
  // Stable founder order arbitrates beds, scarce supplies and patients.
  for(const p of w.people){
    const b=p.body;if(p.life==='dead')continue;
    if(p.life==='downed'){if(w.tick>=b.deathAt)die(w,p);else b.status=`Downed; rescue deadline ${b.deathAt}. ${w.people.some(h=>h.life==='alive'&&h.body.duties.healer>0)?'Needs physical healer and medicine':'Nobody capable of rescue remains'}`;continue;}
    if(b.recoverAt<=w.tick)b.recoverAt=0;
    b.hunger=Math.min(10000,b.hunger+1);b.fatigue=Math.min(10000,b.fatigue+1);
    b.mood=Math.max(0,100-Math.floor(b.hunger/100)-Math.floor(b.fatigue/100)-Math.floor((100-b.health)/2));
    if(w.tick%N.damageEvery===0&&(b.hunger>=N.starveAt||b.fatigue>=N.exhaustedAt))b.health=Math.max(0,b.health-1);
    if(!b.health){fall(w,p);continue;}
    if(b.routine?.kind==='watch'&&(b.hunger>=N.mealAt||b.fatigue>=N.restAt||b.health<70||b.duties.guard===0))endRoutine(w,p);
    if((b.routine?.kind==='rest'||b.routine?.kind==='treatment')&&b.hunger>=N.starveAt&&hasFood(w))begin(w,p,'eat');
    if((b.routine?.kind==='treatment'||b.routine?.kind==='rescue')&&b.fatigue>=N.restAt&&w.structures.some(s=>s.kind==='bed'&&!w.people.some(a=>a.body.routine?.facility===s.id)))begin(w,p,'rest');
    if(!b.routine){
      if(b.hunger>=N.mealAt&&hasFood(w))begin(w,p,'eat');
      else if(b.fatigue>=N.restAt&&w.structures.some(s=>s.kind==='bed'&&!w.people.some(a=>a.body.routine?.facility===s.id)))begin(w,p,'rest');
      else if(b.health<100&&b.hunger<N.starveAt&&b.fatigue<N.exhaustedAt&&w.structures.some(s=>s.kind==='infirmary'&&!w.people.some(a=>a.body.routine?.facility===s.id)))begin(w,p,'treatment');
      else b.status=b.hunger>=N.starveAt?'Starving: health falling; produce accessible food':b.fatigue>=N.exhaustedAt?'Overworked: health falling; build accessible beds':b.hunger>=N.mealAt?'Hungry: no unreserved food; work at 50%':b.fatigue>=N.restAt?'Tired: beds busy or absent; work at 50%':b.health<100?'Injured: needs free infirmary, healer and medicine':'Ready';
    }
    if(b.routine){care(w,p,b.routine);if(b.routine)b.status=(b.hunger>=N.starveAt?'Starving; ':b.fatigue>=N.exhaustedAt?'Overworked; ':'')+b.routine.status;}
  }
  const patients=w.people.filter(p=>p.life==='downed'||p.life==='alive'&&p.body.health<100&&p.body.routine?.kind==='treatment').sort((a,b)=>(a.life==='downed'?0:1)-(b.life==='downed'?0:1)||a.body.deathAt-b.body.deathAt||a.id-b.id);
  for(const patient of patients){
    if(w.people.some(p=>p.body.routine?.patient===patient.id))continue;
    const healer=[...w.people].sort((a,b)=>b.body.duties.healer-a.body.duties.healer||a.id-b.id).find(p=>p!==patient&&p.life==='alive'&&p.body.duties.healer>0&&!p.body.routine&&p.body.health>=70&&p.body.hunger<N.starveAt&&p.body.fatigue<N.exhaustedAt);
    const source=healer?[...w.structures,...w.piles].filter(s=>available(w,s.id,'medicine')>0).sort((a,b)=>distance(healer,a)-distance(healer,b)||a.id-b.id)[0]:undefined;
    if(healer&&source){const r=begin(w,healer,'rescue');r.patient=patient.id;r.reservation={source:source.id,kind:'medicine',count:1};r.status='Rescue: collecting real medicine';}
  }
  for(const p of w.people)if(p.life==='alive'&&!p.job&&!p.body.routine&&!p.destination&&!p.next&&p.body.duties.guard>0&&p.body.duties.guard>Math.max(p.body.duties.builder,p.body.duties.cook,p.body.duties.gatherer,p.body.duties.healer))begin(w,p,'watch');
}
