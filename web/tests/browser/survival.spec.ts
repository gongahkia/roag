import {test,expect} from '@playwright/test';
import type {Page,TestInfo} from '@playwright/test';
import {readFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import type {Save} from '../../src/persistence/schema.ts';
import {materialAudit} from '../../src/simulation/jobs.ts';
import {canonical} from '../../src/simulation/canonical.ts';
import {step} from '../../src/simulation/step.ts';
const hash=(v:unknown)=>createHash('sha256').update(canonical(v)).digest('hex');
async function pause(p:Page){if(await p.locator('#pause').textContent()==='Pause')await p.locator('#pause').click();await expect(p.locator('#clock-status')).toHaveText('Paused');}
async function resume(p:Page){if(await p.locator('#pause').textContent()==='Resume')await p.locator('#pause').click();}
async function snapshot(p:Page,info:TestInfo,name:string):Promise<Save>{const wait=p.waitForEvent('download');await p.locator('#export').click();const d=await wait,path=info.outputPath(`${name}.json`);await d.saveAs(path);const s=JSON.parse(await readFile(path,'utf8')) as Save;for(const a of Object.values(materialAudit(s.world)))expect(a.produced).toBe(a.accounted);return s;}
const structure=(p:Page,kind:string)=>p.locator('#structures .work-card').filter({has:p.locator('h4',{hasText:new RegExp(` ${kind} ·`)})});
const job=(p:Page,kind:string)=>p.locator('#jobs .work-card').filter({has:p.locator('h4',{hasText:kind})});
async function place(p:Page,kind:string,x:number,y:number){await p.locator('#building').selectOption(kind);await p.locator('#build-x').fill(String(x));await p.locator('#build-y').fill(String(y));await p.locator('#build-form').getByRole('button',{name:'Place designation',exact:true}).click();await expect(p.locator('#storage-status')).toContainText('Work command queued');}
function errors(p:Page){const result:string[]=[];p.on('pageerror',e=>result.push(e.message));p.on('console',m=>{if(m.type()==='error')result.push(m.text());});p.on('requestfailed',r=>result.push(`${r.url()} ${r.failure()?.errorText}`));p.on('response',r=>{if(r.status()>=400)result.push(`${r.status()} ${r.url()}`);});return result;}

test('ordinary colony builds farm, kitchen, beds and clinic; duties, visible shortages, meals/rest and real reload',async({page},info)=>{
 test.setTimeout(240000);const failures=errors(page),commands:string[]=[];
 await page.goto('./');await expect(page.locator('#stage')).toHaveAttribute('data-ready','true');
 await page.getByRole('button',{name:'4×',exact:true}).click();
 // Establish all stock through gathering; no imported fixture, injected stock or direct simulation calls.
 for(const x of [63,62,61,60]){await place(page,'stockpile',x,66);commands.push(`stockpile ${x},66`);}
 for(const id of ['11','15']){await page.locator('#work-source').selectOption(id);await page.getByRole('button',{name:'Designate gathering',exact:true}).click();commands.push(`gather ${id}`);}
 await place(page,'farm',68,64);await resume(page);await expect(structure(page,'farm')).toHaveCount(1,{timeout:35000});await pause(page);
 await structure(page,'farm').getByRole('button',{name:'Plant / tend / harvest',exact:true}).click();commands.push('farm 68,64; plant/tend/harvest');
 await place(page,'kitchen',70,64);await resume(page);await expect(structure(page,'kitchen')).toHaveCount(1,{timeout:30000});await pause(page);await structure(page,'kitchen').getByRole('button',{name:'Produce meal',exact:true}).click();commands.push('kitchen 70,64; produce meals');
 for(const x of [66,68,70])await place(page,'bed',x,68);await place(page,'infirmary',72,68);commands.push('beds 66/68/70,68; infirmary 72,68');await resume(page);await expect(structure(page,'bed')).toHaveCount(3,{timeout:40000});await expect(structure(page,'infirmary')).toHaveCount(1,{timeout:40000});await pause(page);
 await structure(page,'infirmary').getByRole('button',{name:'Produce medicine',exact:true}).click();await resume(page);await expect(job(page,'produce medicine')).toContainText('Missing ingredients',{timeout:20000});await pause(page);
 await job(page,'produce medicine').scrollIntoViewIfNeeded();await page.screenshot({path:info.outputPath('medicine-needs-ore.png'),fullPage:true});commands.push('medicine intentionally waits for ore: visible missing ingredients');
 await page.getByLabel('Ada Rowan cook priority',{exact:true}).selectOption('3');await page.getByLabel('Ivo Ash healer priority',{exact:true}).selectOption('3');commands.push('Ada cook 3; Ivo healer 3');
 await place(page,'workshop',74,68);await resume(page);await expect(structure(page,'workshop')).toHaveCount(1,{timeout:40000});await pause(page);await structure(page,'workshop').getByRole('button',{name:'Produce ore',exact:true}).click();commands.push('workshop 74,68; produce ore');await resume(page);
 await expect(job(page,'produce medicine').locator('.muted')).toContainText(/· [1-9]\d* cycles/,{timeout:50000});
 await expect.poll(async()=>Number(await page.locator('#tick').textContent()),{timeout:90000}).toBeGreaterThan(3000);await pause(page);
 const colony=await snapshot(page,info,'ordinary-provisioned-colony');expect(colony.world.scenario).toBe('colony');expect(colony.world.statistics.harvests).toBeGreaterThan(0);expect(colony.world.recipeCounts.meal).toBeGreaterThan(0);expect(colony.world.recipeCounts.medicine).toBeGreaterThan(0);expect(colony.world.statistics.eatenMeals).toBeGreaterThanOrEqual(6);expect(colony.world.people.every(p=>p.life==='alive'&&p.body.health===100&&p.body.rests>=1)).toBe(true);expect(colony.world.people[0]!.body.duties.cook).toBe(3);expect(colony.world.people[4]!.body.duties.healer).toBe(3);
 await page.getByRole('button',{name:'Find Ada Rowan, Builder',exact:true}).click();await page.locator('#center').click();await page.screenshot({path:info.outputPath('food-rest-colony.png'),fullPage:true});
 await page.locator('#save').click();await expect(page.locator('#storage-status')).toContainText(`Saved tick ${colony.world.tick}`);await page.reload();await expect(page.locator('#stage')).toHaveAttribute('data-ready','true');expect(await snapshot(page,info,'actual-reload')).toEqual(colony);
 await resume(page);await expect.poll(async()=>Number(await page.locator('#tick').textContent())).toBeGreaterThan(colony.world.tick+80);await pause(page);const continued=await snapshot(page,info,'continued-colony');const expected=structuredClone(colony.world);while(expected.tick<continued.world.tick)step(expected);expect(hash(continued.world)).toBe(hash(expected));
 await page.locator('#import-file').setInputFiles({name:'colony.json',mimeType:'application/json',buffer:Buffer.from(JSON.stringify(continued))});await expect(page.locator('#storage-status')).toContainText('Imported and saved');expect(await snapshot(page,info,'round-trip')).toEqual(continued);
 for(const kind of ['old-version','bad-bed-claim','bad-needs']){const bad=structuredClone(continued);if(kind==='old-version')(bad as unknown as {version:number}).version=2;else if(kind==='bad-bed-claim')bad.world.people[0]!.body.routine={kind:'rest',facility:999999,patient:null,reservation:null,cargo:{wood:0,stone:0,food:0,meal:0,ore:0,medicine:0,tool:0},progress:0,status:'rest',resume:null};else bad.world.people[0]!.body.hunger=-1;await page.locator('#import-file').setInputFiles({name:`${kind}.json`,mimeType:'application/json',buffer:Buffer.from(JSON.stringify(bad))});await expect(page.locator('#storage-status')).toContainText('Import rejected');expect(await snapshot(page,info,`retained-${kind}`)).toEqual(continued);}
 await page.reload();await expect(page.locator('#stage')).toHaveAttribute('data-ready','true');expect(await snapshot(page,info,'valid-slot-retained')).toEqual(continued);
 await page.setViewportSize({width:390,height:844});await page.locator('#survival').scrollIntoViewIfNeeded();expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);await page.screenshot({path:info.outputPath('survival-390.png'),fullPage:true});
 expect(failures).toEqual([]);await info.attach('survival-evidence',{body:JSON.stringify({commands,seed:42,initialStock:0,colonyTick:colony.world.tick,statistics:colony.world.statistics,recipes:colony.world.recipeCounts,people:colony.world.people.map(p=>({id:p.id,...p.body})),materials:materialAudit(colony.world),savedHash:hash(colony),continuation:{ticks:continued.world.tick-colony.world.tick,expected:hash(expected),actual:hash(continued.world)},errors:failures},null,2),contentType:'application/json'});
});
