import assert from 'node:assert/strict';
import {establish,rankedSources} from './survival.mjs';
import {placementReason} from '../../src/simulation/jobs.ts';
import {makeSave} from '../../src/persistence/schema.ts';

// An ordinary zero-stock colony. Every material is gathered, grown or produced.
export function fortify(seed=42) {
  const h=establish('colony',seed),{w,cmd,build,until}=h;
  for(const x of [59,58,57,56]) {
    until(()=>placementReason(w,{x,y:66})===null,'stockpile access');
    cmd({kind:'stockpile',target:{x,y:66}});
  }
  const sources=seed===42?[12,18]:['wood','stone'].map(kind=>rankedSources(w,kind).find(r=>!w.jobs.some(j=>j.kind==='gather'&&j.ref===r.id&&j.state==='active'))?.id).filter(Boolean);
  for(const source of sources)cmd({kind:'gather',source});
  const defenses=[];
  for(const [building,x,y] of [['tower',71,60],['tower',70,62],['tower',64,69],['tower',60,63],['trap',64,58],['guardpost',62,62],['wall',65,60],['door',64,60]]) {
    until(()=>placementReason(w,{x,y})===null,`${building} footprint`);
    defenses.push(build(building,x,y));
  }
  cmd({kind:'produce',structure:h.workshop.id,recipe:'ammo'});
  for(const s of defenses.filter(s=>(s.kind==='tower'||s.kind==='trap')&&!(s.x===70&&s.y===62))) {
    const batches=s.kind==='tower'?3:1;
    for(let i=0;i<batches;i++) {
      cmd({kind:'reload',structure:s.id});const j=w.jobs.find(j=>j.kind==='reload'&&j.ref===s.id&&j.state==='active');assert.ok(j,'reload job identified by defense');
      until(()=>j.state==='done',`${s.kind} physical reload`,3000);
      assert.equal(s.inventory.ammo,(i+1)*(s.kind==='tower'?4:1));
    }
  }
  makeSave(w,{manualPaused:true,speed:4});
  return {...h,defenses};
}
