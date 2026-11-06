import type { ColonyController } from '../platform/controller.ts';
import type { BossCounterMode, BossFeature } from '../simulation/boss-model.ts';
import { BOSS_CONTENT } from '../simulation/boss-content.ts';
import { BOSS_NAMES } from '../simulation/raid-model.ts';
import { bossCounterReason } from '../simulation/bosses.ts';
import { element } from './panels.ts';
import type { UiCommand } from './panels.ts';

const actions=(feature:BossFeature):BossCounterMode[]=>feature.kind==='valve'?['drain']:feature.kind==='resonator'||feature.kind==='anchor'?['strike']:feature.kind==='web'?['cut','ignite']:['cut'];
const labels:Record<BossCounterMode,string>={strike:'Strike fixture',drain:'Drain channel',cut:'Cut thread',ignite:'Ignite web'};
export function setupBosses(controller:ColonyController,send:(command:UiCommand)=>void):()=>void {
 const rows=new Map<number,{node:HTMLLIElement;text:HTMLParagraphElement;buttons:{mode:BossCounterMode;button:HTMLButtonElement}[]}>();
 return()=>{
  const w=controller.world;
  element('biome-name').textContent=w.environment.biome.toUpperCase();
  element('biome-description').textContent=w.environment.biome==='woodland'?'Woodland: balanced finite sources and ordinary travel.'
   :w.environment.biome==='marsh'?`Marsh: richer forage, less wood. ${w.environment.wet.length} shallow tiles take 6 ticks per step for workers and enemies.`
   :`Ashlands: richer stone, less wood and forage. ${w.environment.ash.length} ember tiles deal 2 damage every 50 ticks to workers and enemies; a built structure covers its tile.`;
  element('boss-status').textContent=w.bosses.status;
  element('boss-encounters').textContent=w.bosses.encounters.map(e=>`${BOSS_NAMES[e.kind]} #${e.owner} · ${e.deadAt===null?'active':'defeated; committed fire remains'} · site ${e.layout.center.x},${e.layout.center.y}${e.exposedUntil>w.tick?` · exposed +50% damage for ${e.exposedUntil-w.tick} ticks`:''}${e.kind==='sluice'?` · ${e.drained.filter(Boolean).length}/${e.drained.length} channels drained`:''}${e.noise?` · last heard ${e.noise.cell.x},${e.noise.cell.y} at tick ${e.noise.at}`:''}`).join('\n')||'No environmental boss encounter on the map.';
  element('boss-effects').textContent=w.bosses.effects.map(e=>`${e.kind} #${e.id} · impact in ${e.due-w.tick} ticks · locked cells ${e.cells.map(c=>`${c.x},${c.y}`).join(' / ')}`).join('\n')||'No environmental effect pending.';
  element('boss-counters-empty').hidden=w.bosses.features.length>0;
  for(const [id,row]of rows)if(!w.bosses.features.some(f=>f.id===id)){row.node.remove();rows.delete(id);}
  for(const feature of w.bosses.features){
   let row=rows.get(feature.id);
   if(!row){
    const node=document.createElement('li'),text=document.createElement('p'),find=document.createElement('button'),buttons:{mode:BossCounterMode;button:HTMLButtonElement}[]=[];
    node.dataset.feature=String(feature.id);node.dataset.featureKind=feature.kind;find.type='button';find.textContent='Find counter';
    find.addEventListener('click',()=>{const f=controller.world.bosses.features.find(f=>f.id===feature.id);if(f)send({type:'focus',x:f.x,y:f.y});});node.append(text,find);
    for(const mode of actions(feature)){
     const button=document.createElement('button');button.type='button';button.textContent=labels[mode];button.dataset.counter=mode;
     button.addEventListener('click',()=>{const f=controller.world.bosses.features.find(f=>f.id===feature.id);if(f&&controller.selected!==null)controller.work({kind:'battle',actor:controller.selected,mode,target:{x:f.x,y:f.y}});});
     node.append(button);buttons.push({mode,button});
    }
    element('boss-counters').append(node);row={node,text,buttons};rows.set(feature.id,row);
   }
   const work=w.bosses.work.filter(work=>work.target===feature.id),encounter=w.bosses.encounters.find(e=>e.owner===feature.owner);
   const state=feature.kind==='valve'&&feature.channel!==null&&encounter?.drained[feature.channel]?' · drained':'';
   row.text.textContent=`${feature.kind} #${feature.id} · ${feature.x},${feature.y} · health ${feature.health}/${BOSS_CONTENT.featureHealth[feature.kind]}${state}${work.length?` · ${work.map(p=>`founder #${p.actor} ${p.mode} ${p.progress}/${BOSS_CONTENT.counters[p.mode]}`).join('; ')}`:''}`;
   for(const {mode,button}of row.buttons){const reason=bossCounterReason(w,mode,feature);button.disabled=Boolean(w.terminal)||controller.busy||!w.people.some(p=>p.id===controller.selected&&p.life==='alive')||reason!==null;button.title=reason??'Selected founder must approach and complete physical work; urgent care takes precedence';}
  }
 };
}
