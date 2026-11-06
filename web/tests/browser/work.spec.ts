import {test,expect} from '@playwright/test';
import type {Page,TestInfo} from '@playwright/test';
import {readFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import type {Save} from '../../src/persistence/schema.ts';
import {materialAudit} from '../../src/simulation/jobs.ts';
import {canonical} from '../../src/simulation/canonical.ts';
import {step} from '../../src/simulation/step.ts';
const hash=(v:unknown)=>createHash('sha256').update(canonical(v)).digest('hex');
async function exportSave(page:Page,info:TestInfo,name:string):Promise<Save>{const wait=page.waitForEvent('download');await page.locator('#export').click();const d=await wait;const path=info.outputPath(`${name}.json`);await d.saveAs(path);return JSON.parse(await readFile(path,'utf8')) as Save;}
async function pause(page:Page){if(await page.locator('#pause').textContent()==='Pause')await page.locator('#pause').click();await expect(page.locator('#clock-status')).toHaveText('Paused');}
async function resume(page:Page){if(await page.locator('#pause').textContent()==='Resume')await page.locator('#pause').click();}
async function place(page:Page,kind:string,x:number,y:number){await page.locator('#building').selectOption(kind);await page.locator('#build-x').fill(String(x));await page.locator('#build-y').fill(String(y));await page.locator('#build-form').getByRole('button',{name:'Place designation'}).click();await expect(page.locator('#storage-status')).toContainText('Work command queued');}
function assertAudit(s:Save){for(const a of Object.values(materialAudit(s.world)))expect(a.accounted).toBe(a.produced);}

test('ordinary UI extracts, carries, delivers and builds; real reload, cancel recovery, repair and dismantle',async({page},info)=>{
 test.setTimeout(150000);
 const errors:string[]=[];page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});page.on('requestfailed',r=>errors.push(`${r.url()} ${r.failure()?.errorText}`));page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`);});
 await page.goto('./?scenario=work-lab');await expect(page.locator('#stage')).toHaveAttribute('data-ready','true');
 // The lab only exposes damage/death commands. Terrain, sources, stocks, jobs and buildings use the ordinary generator and UI.
 await place(page,'stockpile',63,66);
 for(const id of ['11','15']){await page.locator('#work-source').selectOption(id);await page.getByRole('button',{name:'Designate gathering',exact:true}).click();}
 await place(page,'wall',72,64);await page.getByRole('button',{name:'Find Ada Rowan, Builder'}).click();await page.getByRole('button',{name:'4×',exact:true}).click();await resume(page);
 await expect(page.locator('#person-cargo')).toContainText('wood',{timeout:30000});await pause(page);
 const pickup=await exportSave(page,info,'extracted-cargo');expect(pickup.world.people[0]!.cargo.wood).toBeGreaterThan(0);expect(pickup.world.resources.find(r=>r.id===11)!.remaining).toBeLessThan(61);assertAudit(pickup);
 await page.getByRole('button',{name:'Find Ada Rowan, Builder'}).click();await page.screenshot({path:info.outputPath('extraction-and-cargo.png'),fullPage:true});
 await page.locator('#save').click();await expect(page.locator('#storage-status')).toContainText(`Saved tick ${pickup.world.tick}`);await page.reload();await expect(page.locator('#stage')).toHaveAttribute('data-ready','true');const reloaded=await exportSave(page,info,'work-reloaded');expect(reloaded).toEqual(pickup);
 await resume(page);const built=page.locator('#structures .work-card').filter({has:page.locator('h4', {hasText:'wall'})});await expect(built).toHaveCount(1,{timeout:40000});await pause(page);
 const completed=await exportSave(page,info,'wall-built-and-delivery');assertAudit(completed);expect(completed.world.structures.some(s=>s.kind==='wall'&&s.embodied.stone===3)).toBe(true);expect(completed.world.structures.some(s=>s.inventory.wood>0)).toBe(true);
 const expected=structuredClone(pickup.world);for(let i=pickup.world.tick;i<completed.world.tick;i++)step(expected);expect(hash(completed.world)).toBe(hash(expected));
 await built.getByRole('button',{name:'Find',exact:true}).click();await page.screenshot({path:info.outputPath('delivered-and-worker-built-wall.png'),fullPage:true});
 // Site cancellation leaves physical materials. Suspend makes the state stable for inspection.
 await place(page,'workshop',74,68);await resume(page);const project=page.locator('#jobs .work-card').filter({has:page.locator('h4',{hasText:'build workshop'})});await expect(project).toHaveCount(1);
 await expect(project.locator('.muted')).not.toContainText('Site: none',{timeout:35000});await pause(page);await project.getByRole('button',{name:'Suspend',exact:true}).click();await resume(page);await expect(project).toContainText('SUSPENDED');await pause(page);
 const suspended=await exportSave(page,info,'suspended-site');assertAudit(suspended);expect(suspended.world.jobs.find(j=>j.kind==='build'&&j.building==='workshop')!.site.wood).toBeGreaterThan(0);
 await project.getByRole('button',{name:'Cancel',exact:true}).click();await resume(page);await expect(project).toContainText('CANCELLED');await pause(page);const cancelled=await exportSave(page,info,'cancelled-recoverable');assertAudit(cancelled);expect(cancelled.world.jobs.find(j=>j.kind==='build'&&j.building==='workshop')!.completions).toBe(0);expect(cancelled.world.piles.some(p=>p.inventory.wood>0)||cancelled.world.people.some(p=>p.cargo.wood>0)).toBe(true);
 await page.screenshot({path:info.outputPath('cancelled-materials.png'),fullPage:true});
 await built.getByRole('button',{name:'Lab: damage to 50%',exact:true}).click();await resume(page);await expect(built).toContainText('Condition 50%');await pause(page);await built.getByRole('button',{name:'Repair',exact:true}).click();await resume(page);await expect(built).toContainText('Condition 100%',{timeout:25000});await pause(page);
 const repaired=await exportSave(page,info,'repaired');assertAudit(repaired);expect(repaired.world.spent.stone).toBeGreaterThan(0);await page.screenshot({path:info.outputPath('repaired-wall.png'),fullPage:true});
 await built.getByRole('button',{name:'Dismantle',exact:true}).click();await resume(page);await expect(built).toHaveCount(0,{timeout:15000});await pause(page);const dismantled=await exportSave(page,info,'dismantled');assertAudit(dismantled);expect(dismantled.world.jobs.some(j=>j.kind==='dismantle'&&j.state==='done')).toBe(true);await page.screenshot({path:info.outputPath('dismantled-salvage.png'),fullPage:true});
 // A passable stockpile must not deadlock when another founder is standing inside.
 await page.getByRole('button',{name:'Find Ivo Ash, Healer'}).click();await page.locator('#destination-x').fill('63');await page.locator('#destination-y').fill('66');await page.getByRole('button',{name:'Go to tile',exact:true}).click();await resume(page);await expect(page.locator('#person-position')).toHaveText('63, 66',{timeout:15000});await pause(page);
 const stockpile=page.locator('#structures .work-card').filter({has:page.locator('h4',{hasText:'stockpile'})});await stockpile.getByRole('button',{name:'Dismantle',exact:true}).click();await resume(page);await expect(stockpile).toHaveCount(0,{timeout:20000});await pause(page);
 const cleared=await exportSave(page,info,'occupied-stockpile-dismantled');assertAudit(cleared);expect(cleared.world.people[4]!.x===63&&cleared.world.people[4]!.y===66).toBe(false);expect(cleared.world.piles.some(p=>p.inventory.wood>0||p.inventory.stone>0)||cleared.world.people.some(p=>p.cargo.wood>0||p.cargo.stone>0)).toBe(true);await page.screenshot({path:info.outputPath('occupied-stockpile-cleared.png'),fullPage:true});
 // Portable JSON uses the transaction path, including strict P02/forged-claim rejection.
 await page.locator('#import-file').setInputFiles({name:'roundtrip.json',mimeType:'application/json',buffer:Buffer.from(JSON.stringify(cleared))});await expect(page.locator('#storage-status')).toContainText('Imported and saved');expect(await exportSave(page,info,'roundtrip-work')).toEqual(cleared);
 for(const [label,mutate] of [['p02-version',(s:Save)=>{(s as unknown as {version:number}).version=1;}],['forged-cargo',(s:Save)=>{s.world.people[0]!.cargo.stone+=1;}]] as const){const bad=structuredClone(cleared);mutate(bad);await page.locator('#import-file').setInputFiles({name:`${label}.json`,mimeType:'application/json',buffer:Buffer.from(JSON.stringify(bad))});await expect(page.locator('#storage-status')).toContainText('Import rejected');expect(await exportSave(page,info,`after-${label}`)).toEqual(cleared);}
 await page.reload();await expect(page.locator('#stage')).toHaveAttribute('data-ready','true');expect(await exportSave(page,info,'slot-preserved-after-invalid-work')).toEqual(cleared);
 expect(errors).toEqual([]);
 await info.attach('work-evidence',{body:JSON.stringify({seed:42,scenario:'work-lab (ordinary stocks and generation; damage-only development action)',commands:['stockpile 63,66','gather source 11','gather source 15','wall 72,64','save actual reload mid cargo','workshop 74,68','suspend/cancel workshop','damage wall to 50%','repair wall','dismantle wall','Ivo enters stockpile, dismantle yields occupant and retains contents','JSON round trip/rejections'],checkpoints:[pickup,completed,suspended,cancelled,repaired,dismantled,cleared].map(s=>({tick:s.world.tick,sha256:hash(s.world),materials:materialAudit(s.world)})),continuationExpected:hash(expected),continuationActual:hash(completed.world),errors},null,2),contentType:'application/json'});
});
