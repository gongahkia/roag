import {spawnSync} from 'node:child_process';
import {resolve} from 'node:path';
const root=resolve(process.env.ROAG_EVIDENCE_DIR??'../artifacts/colony-web/P07','boss-scenarios');
for(const seed of [43,44,45]){
 const result=spawnSync(process.execPath,['scripts/boss-scenarios.mjs'],{stdio:'inherit',env:{...process.env,ROAG_BOSS_SEED:String(seed),ROAG_EVIDENCE_DIR:resolve(root,`seed-${seed}`)}});
 if(result.status!==0)process.exit(result.status??1);
}
