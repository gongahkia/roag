import { NEEDS } from '../simulation/survival-content.ts';
import { setupWork } from './work.ts';
import { total, MATERIALS } from '../simulation/work-content.ts';
import { assetUrl } from '../assets.ts';
import type { Manifest } from '../assets.ts';
import { founders } from '../content.ts';
import type { ColonyController } from '../platform/controller.ts';
export type UiCommand={type:'select';id:number|null;focus?:boolean}|{type:'pan';dx:number;dy:number}|{type:'zoom';delta:number}|{type:'center'}|{type:'focus';x:number;y:number};
export function element<T extends HTMLElement>(id:string):T {const node=document.getElementById(id);if(!node)throw new Error(`Missing interface element: ${id}`);return node as T;}
export function art(manifest:Manifest,alias:string,scale:number):HTMLSpanElement {
  const sprite=manifest.sprites[alias];if(!sprite)throw new Error(`Missing sprite: ${alias}`);
  const [x,y,w,h]=sprite.frame as [number,number,number,number];const node=document.createElement('span');node.className='pixel-art';node.setAttribute('aria-hidden','true');
  Object.assign(node.style,{width:`${w*scale}px`,height:`${h*scale}px`,backgroundImage:`url("${assetUrl(manifest.image)}")`,backgroundPosition:`-${x*scale}px -${y*scale}px`,backgroundSize:`${manifest.imageSize[0]!*scale}px ${manifest.imageSize[1]!*scale}px`});return node;
}
export function setupPanels(manifest:Manifest,controller:ColonyController,send:(command:UiCommand)=>void):()=>void {
  document.querySelectorAll<HTMLElement>('[data-icon]').forEach(node=>node.append(art(manifest,node.dataset.icon!,1)));
  founders.forEach((founder,i)=>{
    const button=document.createElement('button');button.className='resident';button.dataset.resident=founder.key;button.setAttribute('aria-pressed','false');button.setAttribute('aria-label',`Find ${founder.name}, ${founder.role}`);button.append(art(manifest,founder.sprite,2));
    const text=document.createElement('span'),name=document.createElement('strong'),role=document.createElement('small');name.textContent=founder.name;role.textContent=founder.role;text.append(name,role);button.append(text);
    const number=document.createElement('span');number.className='resident-number';number.textContent=`0${i+1}`;button.append(number);button.addEventListener('click',()=>send({type:'select',id:i+2,focus:true}));element('roster').append(button);
  });
  element('zoom-in').addEventListener('click',()=>send({type:'zoom',delta:1}));element('zoom-out').addEventListener('click',()=>send({type:'zoom',delta:-1}));element('center').addEventListener('click',()=>send({type:'center'}));
  document.querySelectorAll<HTMLButtonElement>('[data-camera]').forEach(button=>button.addEventListener('click',()=>{const d=button.dataset.camera;send({type:'pan',dx:d==='left'?-48:d==='right'?48:0,dy:d==='up'?-48:d==='down'?48:0});}));
  element('credits-open').addEventListener('click',()=>element<HTMLDialogElement>('credits').showModal());element<HTMLAnchorElement>('credits-file').href=assetUrl('CREDITS.txt');
  element('pause').addEventListener('click',()=>controller.pause());
  document.querySelectorAll<HTMLButtonElement>('[data-speed]').forEach(button=>button.addEventListener('click',()=>controller.speed(Number(button.dataset.speed) as 1|2|4)));
  element('save').addEventListener('click',()=>void controller.save());
  element('new-run').addEventListener('click',()=>void controller.newRun(Number(element<HTMLInputElement>('seed').value)));
  element('export').addEventListener('click',()=>{
    const text=controller.exportText(),url=URL.createObjectURL(new Blob([text],{type:'application/json'})),a=document.createElement('a');a.href=url;a.download=`roag-${controller.world.seed}-tick-${controller.world.tick}.json`;a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);
  });
  element('import').addEventListener('click',()=>element<HTMLInputElement>('import-file').click());
  element<HTMLInputElement>('import-file').addEventListener('change',(event)=>{const input=event.target as HTMLInputElement;const file=input.files?.[0];if(file)void controller.importFile(file);input.value='';});
  element('destination-order').addEventListener('submit',(event)=>{event.preventDefault();controller.order({x:Number(element<HTMLInputElement>('destination-x').value),y:Number(element<HTMLInputElement>('destination-y').value)});});
  element('lab-gate').addEventListener('click',()=>controller.editGate());
  const updateWork=setupWork(controller,send);
  let lastInspection='',lastPerson:number|null=null;
  return ()=>{
    updateWork();
    const world=controller.world,p=world.people.find(p=>p.id===controller.selected),founder=p?founders[p.founder]:undefined;
    element('tick').textContent=String(world.tick);element('run-seed').textContent=String(world.seed);
    const status=controller.clock.hidden?'Hidden — paused':controller.clock.manualPaused?'Paused':`Running at ${controller.clock.speed}×`;
    element('clock-status').textContent=status;element<HTMLButtonElement>('pause').textContent=controller.clock.manualPaused?'Resume':'Pause';element('pause').setAttribute('aria-pressed',String(controller.clock.manualPaused));
    document.querySelectorAll<HTMLButtonElement>('[data-speed]').forEach(b=>b.setAttribute('aria-pressed',String(Number(b.dataset.speed)===controller.clock.speed)));
    element('backlog').textContent=controller.clock.backlog?`Processing backlog: ${controller.clock.backlog} ticks`:'';
    element('storage-status').textContent=controller.message;element('storage-status').classList.toggle('error',controller.storageError);
    for(const id of ['save','import','new-run'])element<HTMLButtonElement>(id).disabled=controller.busy;
    element('lab-controls').hidden=world.scenario!=='routing-lab';
    element('lab-gate-state').textContent=world.terrain[60*128+64]===2?'Gate closed':'Gate open';
    element('resource-summary').textContent=world.resources.map(r=>r.kind).filter((k,i,a)=>a.indexOf(k)===i).map(kind=>`${kind}: ${world.resources.filter(r=>r.kind===kind).reduce((s,r)=>s+r.remaining,0)}`).join(' · ');
    element('visibility-evidence').textContent=JSON.stringify(controller.visibilityTransitions);
    element('selection-state').textContent=p?'FOUNDER SELECTED':'NO SELECTION';
    document.querySelectorAll<HTMLButtonElement>('[data-resident]').forEach(button=>button.setAttribute('aria-pressed',String(button.dataset.resident===founder?.key)));
    element('destination-order').hidden=!p;
    if(p&&p.id!==lastPerson){element<HTMLInputElement>('destination-x').value=String(p.x+4);element<HTMLInputElement>('destination-y').value=String(p.y);}
    lastPerson=p?.id??null;
    const serialized=JSON.stringify(p??null);if(serialized===lastInspection)return;lastInspection=serialized;
    const inspection=element('inspection');const provenanceWasOpen=inspection.querySelector('details')?.open??false;inspection.replaceChildren();
    if(!p||!founder){const portrait=document.createElement('div');portrait.className='empty-portrait';portrait.append(art(manifest,'heart',3));const hint=document.createElement('p');hint.className='empty-title';hint.textContent='Meet your founders';const help=document.createElement('p');help.className='muted';help.textContent='Select a founder, then right-click a destination or enter its tile coordinates below.';inspection.append(portrait,hint,help);return;}
    const portrait=document.createElement('div');portrait.className='portrait';portrait.append(art(manifest,founder.sprite,4));const title=document.createElement('h3');title.textContent=founder.name;const role=document.createElement('p');role.className='role';role.textContent=founder.role;
    const note=document.createElement('p');note.className='resident-note';note.textContent=founder.note;const details=document.createElement('dl');
    const order=p.destination?`Move to ${p.destination.x}, ${p.destination.y}`:'Awaiting orders';
    for(const [label,value,id] of [['Health',`${p.body.health}% · ${p.body.status}`,'person-health'],['Needs',`Hunger ${Math.min(100,Math.floor(p.body.hunger/NEEDS.starveAt*100))}% · Fatigue ${Math.min(100,Math.floor(p.body.fatigue/NEEDS.exhaustedAt*100))}% · Mood ${p.body.mood}%`,'person-needs'],['Care',p.body.routine?`${p.body.routine.kind}: ${p.body.routine.status}`:'No active care','person-care'],['Life',p.life,'person-life'],['Location',`${p.x}, ${p.y}`,'person-position'],['Order',order,'person-order'],['Step',p.next?`${p.progress}/4 → ${p.next.x}, ${p.next.y}`:'At rest','person-progress'],['Access',p.blocked??'Clear','person-blocked'],['Job',p.job?`#${p.job} · ${world.jobs.find(j=>j.id===p.job)?.status}`:'No active job','person-job'],['Cargo',total(p.cargo)?MATERIALS.filter(k=>p.cargo[k]).map(k=>`${p.cargo[k]} ${k}`).join(', '):'Empty (capacity 5)','person-cargo']]){const dt=document.createElement('dt');dt.textContent=label!;const dd=document.createElement('dd');dd.id=id!;dd.textContent=value!;details.append(dt,dd);}
    const provenance=document.createElement('details');provenance.className='provenance';provenance.open=provenanceWasOpen;const summary=document.createElement('summary');summary.textContent='Artwork provenance';const source=document.createElement('p');const sprite=manifest.sprites[founder.sprite]!;source.textContent=`${sprite.id} · crop [${sprite.rect.join(', ')}] · ${sprite.source}. ${sprite.originalName}. Colony role assigned locally; no animation inferred.`;provenance.append(summary,source);
    const clear=document.createElement('button');clear.className='clear-selection';clear.textContent='Clear selection';clear.addEventListener('click',()=>send({type:'select',id:null}));inspection.append(portrait,title,role,note,details,provenance,clear);
  };
}
