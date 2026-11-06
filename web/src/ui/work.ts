import { DUTIES, NEEDS, RECIPES } from '../simulation/survival-content.ts';
import type { ColonyController } from '../platform/controller.ts';
import { active } from '../simulation/model.ts';
import type { CommandInput, Job } from '../simulation/model.ts';
import { capacity, jobCost, jobTicks, materialAudit } from '../simulation/jobs.ts';
import { MATERIALS, total, WORK_CONTENT } from '../simulation/work-content.ts';
import type { Building, Stock } from '../simulation/work-content.ts';
import { founders } from '../content.ts';
import type { UiCommand } from './panels.ts';
import { element } from './panels.ts';
const stockText=(s:Stock):string=>MATERIALS.filter(k=>s[k]).map(k=>`${s[k]} ${k}`).join(', ')||'none';
function text(parent:HTMLElement,tag:string,value:string):HTMLElement {const node=document.createElement(tag);node.textContent=value;parent.append(node);return node;}
function button(parent:HTMLElement,label:string,action:()=>void):HTMLButtonElement {const b=document.createElement('button');b.type='button';b.textContent=label;b.addEventListener('click',action);parent.append(b);return b;}
export function setupWork(controller:ColonyController,send:(c:UiCommand)=>void):()=>void {
  const source=element<HTMLSelectElement>('work-source'),building=element<HTMLSelectElement>('building');
  element('gather-form').addEventListener('submit',e=>{e.preventDefault();controller.work({kind:'gather',source:Number(source.value)});});
  element('source-find').addEventListener('click',()=>{const r=controller.world.resources.find(r=>r.id===Number(source.value));if(r)send({type:'focus',x:r.x,y:r.y});});
  element('build-form').addEventListener('submit',e=>{e.preventDefault();const target={x:Number(element<HTMLInputElement>('build-x').value),y:Number(element<HTMLInputElement>('build-y').value)};
    controller.work(building.value==='stockpile'?{kind:'stockpile',target}:{kind:'build',building:building.value as Building,target});});
  const cost=()=>{const entry=WORK_CONTENT.buildings[building.value as Building];element('build-cost').textContent=entry?`${stockText(entry.cost)} · ${entry.ticks} work ticks · one tile`:'Empty ground designation · no materials · capacity 30';};building.addEventListener('change',cost);cost();
  const rows=new Map<number,{node:HTMLElement;status:HTMLElement;detail:HTMLElement;priority:HTMLSelectElement;worker:HTMLSelectElement;suspend:HTMLButtonElement;cancel:HTMLButtonElement}>();
  const structures=new Map<number,{node:HTMLElement;status:HTMLElement;repair:HTMLButtonElement;door:HTMLButtonElement|null;reload:HTMLButtonElement|null}>();
  const jobAction=(j:Job,input:CommandInput)=>{if(active(j))controller.work(input);};
  const dutyRows=controller.world.people.map(p=>{
    const node=document.createElement('article');node.className='duty-card';text(node,'strong',founders[p.founder]!.name);const status=text(node,'p','');
    const selects=DUTIES.map(duty=>{const label=document.createElement('label');label.textContent=duty;const select=document.createElement('select');select.setAttribute('aria-label',`${founders[p.founder]!.name} ${duty} priority`);for(const n of [0,1,2,3])select.add(new Option(n===0?'Off':String(n),String(n)));select.addEventListener('change',()=>controller.work({kind:'duty',actor:p.id,duty,value:Number(select.value)}));label.append(select);node.append(label);return select;});
    if(controller.world.scenario==='work-lab')for(const action of ['injure','down','hungry','tired'] as const)button(node,`Lab: ${action} ${founders[p.founder]!.name}`,()=>controller.work({kind:'dev',action,id:p.id}));
    element('duties').append(node);return {status,selects};
  });
  let lastWorld=controller.world;
  return ()=>{
    const w=controller.world;
    element('survival-summary').textContent=`Day ${Math.floor(w.tick/NEEDS.dayTicks)+1} · ${w.people.filter(p=>p.life==='alive').length} capable / ${w.people.filter(p=>p.life==='downed').length} downed / ${w.people.filter(p=>p.life==='dead').length} dead · ${w.statistics.harvests} harvests · ${w.statistics.eatenMeals} meals + ${w.statistics.eatenFood} raw food eaten · ${w.people.reduce((n,p)=>n+p.body.rests,0)} rest cycles · ${w.statistics.rescues} rescues${w.terminal?' · DEFEAT: '+w.terminal:''}`;
    w.people.forEach((p,i)=>{const row=dutyRows[i]!;row.status.textContent=`${p.life} · Health ${p.body.health}% · Hunger ${Math.min(100,Math.floor(p.body.hunger/NEEDS.starveAt*100))}% · Fatigue ${Math.min(100,Math.floor(p.body.fatigue/NEEDS.exhaustedAt*100))}% · Mood ${p.body.mood}% · ${p.body.status}`;DUTIES.forEach((d,k)=>{row.selects[k]!.value=String(p.body.duties[d]);row.selects[k]!.disabled=p.life!=='alive';});});
    if(w!==lastWorld){element('jobs').replaceChildren();element('structures').replaceChildren();rows.clear();structures.clear();source.replaceChildren();lastWorld=w;}
    const chosen=source.value;
    for(const r of [...w.resources].sort((a,b)=>Math.abs(a.x-64)+Math.abs(a.y-64)-Math.abs(b.x-64)-Math.abs(b.y-64)||a.id-b.id)){
      let option=source.querySelector<HTMLOptionElement>(`option[value="${r.id}"]`);if(!option){option=document.createElement('option');option.value=String(r.id);source.append(option);}
      option.textContent=`#${r.id} ${r.kind} · ${r.remaining}/${r.initial} · ${r.x},${r.y}`;
    }
    if(chosen)source.value=chosen;
    element('work-count').textContent=`${w.jobs.filter(active).length} unfinished · ${w.piles.filter(p=>total(p.inventory)).length} bundles`;
    const ledger=materialAudit(w);element('material-ledger').textContent=MATERIALS.map(k=>`${k}: ${ledger[k].stored} stored / ${ledger[k].carried} carried / ${ledger[k].site} at sites / ${ledger[k].embodied} built / ${ledger[k].spent} spent · ${ledger[k].produced} sourced / ${ledger[k].consumed} consumed`).join(' • ');
    for(const [id,row] of rows)if(!w.jobs.some(j=>j.id===id)){row.node.remove();rows.delete(id);}
    for(const j of w.jobs){
      let row=rows.get(j.id);
      if(!row){
        const node=document.createElement('article');node.className='work-card';node.dataset.job=String(j.id);text(node,'h4',`#${j.id} ${j.kind}${j.building?' '+j.building:j.recipe?' '+j.recipe:''} · ${j.x},${j.y}`);
        const status=text(node,'p',''),detail=text(node,'p','');detail.className='muted';
        const controls=document.createElement('div');controls.className='job-actions';node.append(controls);
        const priority=document.createElement('select');priority.setAttribute('aria-label',`Priority for job ${j.id}`);for(const value of [3,2,1])priority.add(new Option(`${value===3?'High':value===2?'Normal':'Low'} priority`,String(value)));
        priority.addEventListener('change',()=>jobAction(j,{kind:'priority',job:j.id,value:Number(priority.value)}));controls.append(priority);
        const worker=document.createElement('select');worker.setAttribute('aria-label',`Worker for job ${j.id}`);worker.add(new Option('Any free worker',''));founders.forEach((f,i)=>worker.add(new Option(f.name,String(i+2))));worker.addEventListener('change',()=>jobAction(j,{kind:'assign',job:j.id,worker:worker.value?Number(worker.value):null}));controls.append(worker);
        const suspend=button(controls,'Suspend',()=>jobAction(j,{kind:'work',job:j.id,action:j.state==='suspended'?'resume':'suspend'}));
        const cancel=button(controls,'Cancel',()=>jobAction(j,{kind:'work',job:j.id,action:'cancel'}));button(controls,'Find',()=>send({type:'focus',x:j.x,y:j.y}));
        row={node,status,detail,priority,worker,suspend,cancel};rows.set(j.id,row);element('jobs').prepend(node);
      }
      row.status.textContent=`${j.state.toUpperCase()} · ${j.worker?founders[j.worker-2]!.name:'Unassigned'} · ${j.status}`;
      row.detail.textContent=`Cost: ${stockText(jobCost(j))} · Site: ${stockText(j.site)} · Work ${j.progress}/${jobTicks(j)}${j.kind==='farm'||j.kind==='produce'?` · Stage ${j.stage} · ${j.cycles} cycles`:''}${j.reservation?` · Reserved ${j.reservation.count} ${j.reservation.kind} at #${j.reservation.source}`:''}`;
      row.priority.value=String(j.priority);row.worker.value=j.requested===null?'':String(j.requested);row.suspend.textContent=j.state==='suspended'?'Resume':'Suspend';
      for(const control of [row.priority,row.worker,row.suspend,row.cancel])control.disabled=!active(j)||controller.busy;
    }
    for(const [id,row] of structures)if(!w.structures.some(s=>s.id===id)){row.node.remove();structures.delete(id);}
    for(const s of w.structures){
      let row=structures.get(s.id);
      if(!row){const node=document.createElement('article');node.className='work-card';node.dataset.structure=String(s.id);text(node,'h4',`#${s.id} ${s.kind==='door'?'gate':s.kind==='guardpost'?'guard post':s.kind} · ${s.x},${s.y}`);const status=text(node,'p','');
        if(s.kind==='farm')button(node,'Plant / tend / harvest',()=>controller.work({kind:'produce',structure:s.id,recipe:'farm'}));
        for(const [recipe,entry] of Object.entries(RECIPES))if(entry.facility===s.kind)button(node,`Produce ${recipe}`,()=>controller.work({kind:'produce',structure:s.id,recipe:recipe as keyof typeof RECIPES}));
        const repair=button(node,'Repair',()=>controller.work({kind:'repair',structure:s.id}));button(node,'Dismantle',()=>controller.work({kind:'dismantle',structure:s.id}));button(node,'Find',()=>send({type:'focus',x:s.x,y:s.y}));
        const door=s.kind==='door'?button(node,'Close gate',()=>controller.work({kind:'door',structure:s.id,open:!s.open})):null;
        const reload=s.kind==='tower'||s.kind==='trap'?button(node,`Reload ${s.kind==='tower'?4:1} ammo`,()=>controller.work({kind:'reload',structure:s.id})):null;
        if(w.scenario==='work-lab'&&s.kind!=='stockpile')button(node,'Lab: damage to 50%',()=>controller.work({kind:'dev',action:'damage',id:s.id}));
        row={node,status,repair,door,reload};structures.set(s.id,row);element('structures').prepend(node);
      }
      const defense=s.kind==='tower'||s.kind==='trap';
      const cooldown=w.combat.defenses.find(d=>d.structure===s.id)?.cooldown??0;
      row.status.textContent=`Condition ${s.health}%${defense?` · Ammo ${s.inventory.ammo}/${WORK_CONTENT.buildings[s.kind as 'tower'|'trap'].capacity}${s.inventory.ammo===0?' · Empty: produce ammo at a workshop, then reload':''}${cooldown>w.tick?` · Ready in ${cooldown-w.tick} ticks`:''}`:capacity(s)?` · ${total(s.inventory)}/${capacity(s)} capacity · ${stockText(s.inventory)}`:''}${s.kind==='door'?s.open?' · open to everyone':' · closed to everyone':''}`;row.repair.disabled=s.health===100||s.kind==='stockpile';if(row.door)row.door.textContent=s.open?'Close gate':'Open gate';
      if(row.reload){const batch=s.kind==='tower'?4:1,limit=WORK_CONTENT.buildings[s.kind as 'tower'|'trap'].capacity;const busy=w.jobs.some(j=>active(j)&&j.ref===s.id);row.reload.disabled=controller.busy||Boolean(w.terminal)||busy||s.inventory.ammo+batch>limit;row.reload.title=busy?'Finish or cancel this structure’s current job before reloading':s.inventory.ammo+batch>limit?`Reload needs room for ${batch} rounds`:'A worker collects and delivers ammunition from storage';}
    }
  };
}
