import { createRaider } from '../../src/simulation/combat.ts';
import { RAID_COST, waveBudget } from '../../src/simulation/raid-director.ts';

/** Explicit isolated fixture; never used by the ordinary balance/browser journey. */
export function addRaider(w,x,y,kind='raider') {
  const c=w.combat;
  if(c.phase!=='combat') {
    c.phase='combat';c.wave=c.completed+1;c.warningAt=0;c.spawnAt=0;c.recoveryUntil=0;c.budget=waveBudget(c.wave);c.spent=0;
    c.waveStart={tick:w.tick,spawned:c.stats.spawned,kills:c.stats.kills,deaths:w.statistics.deaths,ammo:w.consumed.ammo,structuresLost:c.stats.structuresLost,repairs:c.stats.repairs};
  }
  const ticket={id:w.nextId++,kind,squadId:null,leaderId:null,edge:'east',entryOffset:0};
  const r=createRaider(w,ticket,{x,y});c.raiders.push(r);c.stats.spawned++;c.spent+=RAID_COST[kind];return r;
}
