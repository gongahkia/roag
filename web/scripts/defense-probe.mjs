import {writeFileSync,mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
import {fortify} from '../tests/helpers/defense.mjs';
import {makeSave,encodeSave} from '../src/persistence/schema.ts';
import {materialAudit} from '../src/simulation/jobs.ts';
const dir=resolve(process.env.ROAG_EVIDENCE_DIR??'../artifacts/colony-web/P06');mkdirSync(dir,{recursive:true});
const h=fortify(),{w,tick,cmd}=h;
console.log('fortified',w.tick,w.structures.filter(s=>['tower','trap'].includes(s.kind)).map(s=>({kind:s.kind,x:s.x,y:s.y,ammo:s.inventory.ammo})));
writeFileSync(resolve(dir,'fortified.json'),encodeSave(makeSave(w,{manualPaused:true,speed:4})));
let phase='';
for(let i=0;i<16000&&!w.terminal&&w.combat.completed===0;i++) {
  tick();
  if(w.tick%50===0)makeSave(w,{manualPaused:true,speed:4});
  if(w.combat.phase!==phase){phase=w.combat.phase;console.log('phase',w.tick,phase,w.combat.edge);}
}
console.log(JSON.stringify({tick:w.tick,terminal:w.terminal,combat:w.combat,people:w.people.map(p=>({id:p.id,life:p.life,health:p.body.health,status:p.body.status})),structures:w.structures.map(s=>({kind:s.kind,x:s.x,y:s.y,health:s.health,ammo:s.inventory.ammo})),materials:materialAudit(w)},null,2));
writeFileSync(resolve(dir,'after-wave.json'),encodeSave(makeSave(w,{manualPaused:true,speed:4})));
