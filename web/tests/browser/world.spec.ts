import { test, expect, chromium } from '@playwright/test';
import type { Page, TestInfo } from '@playwright/test';
import { readFile, mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import { createHash } from 'node:crypto';
import { canonical } from '../../src/simulation/canonical.ts';
import { step } from '../../src/simulation/step.ts';
import type { Save } from '../../src/persistence/schema.ts';
import { PNG } from 'pngjs';

const hash=(value:unknown)=>createHash('sha256').update(canonical(value)).digest('hex');
function errors(page:Page){const found:string[]=[];page.on('pageerror',e=>found.push(e.message));page.on('console',m=>{if(m.type()==='error')found.push(m.text());});page.on('requestfailed',r=>found.push(`${r.url()} ${r.failure()?.errorText}`));page.on('response',r=>{if(r.status()>=400)found.push(`${r.status()} ${r.url()}`);});return found;}
const ready=async(page:Page,url='./')=>{await page.goto(url);await expect(page.locator('#stage')).toHaveAttribute('data-ready','true');};
const tick=async(page:Page)=>Number(await page.locator('#tick').textContent());
async function exportSave(page:Page,info:TestInfo,name:string):Promise<Save>{const wait=page.waitForEvent('download');await page.getByRole('button',{name:'Export JSON',exact:true}).click();const download=await wait;const path=info.outputPath(`${name}.json`);await download.saveAs(path);return JSON.parse(await readFile(path,'utf8')) as Save;}
async function order(page:Page,x:number,y:number){await page.locator('#destination-x').fill(String(x));await page.locator('#destination-y').fill(String(y));await page.getByRole('button',{name:'Go to tile',exact:true}).click();}
async function paused(page:Page){const button=page.locator('#pause');if(await button.textContent()==='Pause')await button.click();await expect(button).toHaveText('Resume');}
async function stored(page:Page):Promise<string>{return await page.evaluate(()=>new Promise<string>((resolve,reject)=>{const request=indexedDB.open('roag-colony',1);request.onsuccess=()=>{const db=request.result;const get=db.transaction('saves','readonly').objectStore('saves').get('active');get.onsuccess=()=>{resolve(get.result as string);db.close();};get.onerror=()=>reject(get.error);};request.onerror=()=>reject(request.error);}));}
async function assertRockCrop(page:Page,info:TestInfo):Promise<void>{
 const manifest=JSON.parse(await readFile(new URL('../../public/game-assets/manifest.json',import.meta.url),'utf8')) as {sprites:Record<string,{frame:[number,number,number,number]}>};
 const source=PNG.sync.read(await readFile(new URL('../../public/game-assets/colony.png',import.meta.url)));
 const rendered=PNG.sync.read(await page.locator('canvas').screenshot());
 const view=JSON.parse((await page.locator('#stage').getAttribute('data-camera'))!) as {x:number;y:number;zoom:number};
 const [sx,sy]=manifest.sprites.rock!.frame;
 const left=Math.round(rendered.width/2+(60*12-view.x)*view.zoom),top=Math.round(rendered.height/2+(59*12-view.y)*view.zoom);
 let checked=0;
 for(let y=0;y<12;y++)for(let x=0;x<12;x++){
  const si=((sy+y)*source.width+sx+x)*4,alpha=source.data[si+3]!/255*.38;
  const di=((top+y*view.zoom+Math.floor(view.zoom/2))*rendered.width+left+x*view.zoom+Math.floor(view.zoom/2))*4;
  for(let channel=0;channel<3;channel++){
   const expected=Math.round(source.data[si+channel]!*alpha+[27,39,32][channel]!*(1-alpha));
   expect(Math.abs(rendered.data[di+channel]!-expected),`rock crop pixel ${x},${y} channel ${channel}`).toBeLessThanOrEqual(2);
  }
  checked++;
 }
 await info.attach('terrain-crop-evidence',{body:JSON.stringify({worldTile:{x:60,y:59},alias:'rock',sourceFrame:manifest.sprites.rock!.frame,checkedPixels:checked,zoom:view.zoom,alpha:.38}),contentType:'application/json'});
}

test('movement, speed, mid-move save/reload continuation and portable JSON with safe invalid imports',async({page},info)=>{
 const failures=errors(page);await ready(page);await page.getByRole('button',{name:'Find Ada Rowan, Builder'}).click();
 await page.getByRole('button',{name:'Zoom out',exact:true}).click();
 const camera=JSON.parse((await page.locator('#stage').getAttribute('data-camera'))!) as {x:number;y:number;zoom:number};
 const canvas=(await page.locator('canvas').boundingBox())!;
 await page.mouse.click(canvas.x+canvas.width/2+(80.5*12-camera.x)*camera.zoom,canvas.y+canvas.height/2+(64.5*12-camera.y)*camera.zoom,{button:'right'});
 await expect(page.locator('#storage-status')).toContainText('Destination 80, 64 queued');
 await page.getByRole('button',{name:'Resume',exact:true}).click();await expect.poll(()=>tick(page)).toBeGreaterThan(4);
 await expect(page.locator('#person-position')).not.toHaveText('61, 64');
 const speeds:unknown[]=[];
 for(const speed of [1,2,4]){await page.getByRole('button',{name:`${speed}×`,exact:true}).click();await expect(page.locator('#clock-status')).toHaveText(`Running at ${speed}×`);const start=await tick(page);await page.waitForTimeout(350);speeds.push({speed,start,end:await tick(page)});}
 await paused(page);const frozenTick=await tick(page);await page.waitForTimeout(250);expect(await tick(page)).toBe(frozenTick);
 const saved=await exportSave(page,info,'mid-movement');expect(saved.world.people[0]!.next).not.toBeNull();expect(saved.world.people[0]!.destination).not.toBeNull();
 await page.getByRole('button',{name:'Save',exact:true}).click();await expect(page.locator('#storage-status')).toHaveText(`Saved tick ${saved.world.tick}.`);
 await page.screenshot({path:info.outputPath('mid-movement-saved.png'),fullPage:true});
 await page.reload();await expect(page.locator('#stage')).toHaveAttribute('data-ready','true');await expect(page.locator('#storage-status')).toHaveText(`Restored tick ${saved.world.tick}.`);
 const reloaded=await exportSave(page,info,'reloaded');expect(reloaded).toEqual(saved);
 await page.getByRole('button',{name:'Find Ada Rowan, Builder'}).click();await page.getByRole('button',{name:'Resume',exact:true}).click();await expect.poll(()=>tick(page)).toBeGreaterThan(saved.world.tick+12);await paused(page);
 const continued=await exportSave(page,info,'continued');const expected=structuredClone(saved.world);for(let i=saved.world.tick;i<continued.world.tick;i++)step(expected);expect(hash(continued.world)).toBe(hash(expected));
 await page.locator('#seed').fill('1337');await page.getByRole('button',{name:'New run',exact:true}).click();await expect(page.locator('#run-seed')).toHaveText('1337');await expect(page.locator('#storage-status')).toContainText('Saved tick 0');
 await page.locator('#import-file').setInputFiles({name:'continue.json',mimeType:'application/json',buffer:Buffer.from(JSON.stringify(continued))});await expect(page.locator('#storage-status')).toHaveText(`Imported and saved tick ${continued.world.tick}.`);expect(await exportSave(page,info,'round-trip')).toEqual(continued);
 const slotBefore=await stored(page);
 const future=structuredClone(continued);(future as unknown as {version:number}).version=999;
 const badReference=structuredClone(continued);badReference.world.pending.push({sequence:badReference.world.nextSequence++,due:badReference.world.tick+1,input:{kind:'move',actor:999,target:{x:70,y:64}}});
 for(const [name,text]of [['corrupt','{broken'],['version',JSON.stringify(future)],['reference',JSON.stringify(badReference)]]){
  await page.locator('#import-file').setInputFiles({name:`${name}.json`,mimeType:'application/json',buffer:Buffer.from(text!)});await expect(page.locator('#storage-status')).toContainText('Import rejected');expect(await exportSave(page,info,`after-invalid-${name}`)).toEqual(continued);expect(await stored(page)).toBe(slotBefore);
 }
 await page.reload();await expect(page.locator('#stage')).toHaveAttribute('data-ready','true');expect(await exportSave(page,info,'slot-after-rejections')).toEqual(continued);expect(failures).toEqual([]);
 await info.attach('continuation-evidence',{body:JSON.stringify({seed:saved.world.seed,speeds,savedTick:saved.world.tick,savedPerson:saved.world.people[0],continuedTicks:continued.world.tick-saved.world.tick,expectedWorldSha256:hash(expected),actualWorldSha256:hash(continued.world),roundTripSha256:hash(continued),invalidImports:['corrupt','version','reference'],errors:failures},null,2),contentType:'application/json'});
});

test('routing lab blocks visibly, saves blocked intent, invalidates routes and recovers through a reopened gate',async({page},info)=>{
 const failures=errors(page);await ready(page,'./?scenario=routing-lab');await page.getByRole('button',{name:'Find Ada Rowan, Builder'}).click();await assertRockCrop(page,info);await order(page,70,60);await page.locator('#lab-controls').evaluate(node=>{(node.parentElement as HTMLDetailsElement).open=true;});await page.getByRole('button',{name:'Toggle lab gate'}).click();await page.getByRole('button',{name:'Resume',exact:true}).click();await expect(page.locator('#person-blocked')).toContainText('No route');await paused(page);await expect(page.locator('#person-position')).toHaveText('58, 60');
 const blocked=await exportSave(page,info,'blocked-world');await page.getByRole('button',{name:'Save',exact:true}).click();await expect(page.locator('#storage-status')).toContainText('Saved tick');await page.screenshot({path:info.outputPath('blocked.png'),fullPage:true});
 await page.reload();await expect(page.locator('#stage')).toHaveAttribute('data-ready','true');expect(await exportSave(page,info,'blocked-reloaded')).toEqual(blocked);await page.getByRole('button',{name:'Find Ada Rowan, Builder'}).click();await expect(page.locator('#person-blocked')).toContainText('No route');await page.locator('.development-info').evaluate((node)=>{(node as HTMLDetailsElement).open=true;});await page.getByRole('button',{name:'Toggle lab gate'}).click();await page.getByRole('button',{name:'4×',exact:true}).click();await page.getByRole('button',{name:'Resume',exact:true}).click();await expect(page.locator('#person-order')).toHaveText('Awaiting orders');await paused(page);await expect(page.locator('#person-position')).toHaveText('70, 60');await expect(page.locator('#person-blocked')).toHaveText('Clear');await page.screenshot({path:info.outputPath('recovered.png'),fullPage:true});expect(failures).toEqual([]);
 await info.attach('routing-evidence',{body:JSON.stringify({blockedTick:blocked.world.tick,blockedPerson:blocked.world.people[0],recoveredTick:await tick(page),errors:failures}),contentType:'application/json'});
});

test('quota-limited write preserves valid slot/current import state and export remains usable',async({page},info)=>{
 const failures=errors(page);await ready(page);await page.getByRole('button',{name:'Save',exact:true}).click();await expect(page.locator('#storage-status')).toContainText('Saved tick');const originalSlot=await stored(page);
 await page.getByRole('button',{name:'Find Ada Rowan, Builder'}).click();await order(page,75,64);const current=await exportSave(page,info,'quota-before');
 await page.evaluate(()=>{IDBObjectStore.prototype.put=function(){throw new DOMException('Injected quota limit','QuotaExceededError');};});
 await page.getByRole('button',{name:'Save',exact:true}).click();await expect(page.locator('#storage-status')).toContainText('Save failed');expect(await stored(page)).toBe(originalSlot);expect(await exportSave(page,info,'quota-export')).toEqual(current);
 const incoming=structuredClone(current);incoming.world.seed=1337;
 await page.locator('#import-file').setInputFiles({name:'valid.json',mimeType:'application/json',buffer:Buffer.from(JSON.stringify(incoming))});await expect(page.locator('#storage-status')).toContainText('Import rejected');expect(await exportSave(page,info,'quota-import-rejected')).toEqual(current);expect(await stored(page)).toBe(originalSlot);expect(failures).toEqual([]);
});

test('unavailable IndexedDB is visible while in-memory movement, new run and export still work',async({page},info)=>{
 await page.addInitScript(()=>Object.defineProperty(window,'indexedDB',{get(){throw new DOMException('Storage disabled','SecurityError');}}));const failures=errors(page);await ready(page);await expect(page.locator('#storage-status')).toContainText('Running in memory');await page.getByRole('button',{name:'Find Ada Rowan, Builder'}).click();await order(page,75,64);await page.getByRole('button',{name:'Resume',exact:true}).click();await expect.poll(()=>tick(page)).toBeGreaterThan(5);await paused(page);const snapshot=await exportSave(page,info,'memory-export');expect(snapshot.world.people[0]!.x).toBeGreaterThan(61);
 await page.getByRole('button',{name:'Save',exact:true}).click();await expect(page.locator('#storage-status')).toContainText('Save failed');await page.locator('#seed').fill('1337');await page.getByRole('button',{name:'New run',exact:true}).click();await expect(page.locator('#run-seed')).toHaveText('1337');expect((await exportSave(page,info,'new-memory-run')).world.seed).toBe(1337);expect(failures).toEqual([]);
});

async function naturalBrowser(){
 const profile=await mkdtemp(join(tmpdir(),'roag-p02-visibility-'));
 const child=spawn(chromium.executablePath(),['--headless=new','--no-sandbox','--remote-debugging-port=0',`--user-data-dir=${profile}`,'about:blank'],{stdio:['ignore','ignore','pipe']});
 try{
  const endpoint=await new Promise<string>((resolve,reject)=>{let stderr='';const timer=setTimeout(()=>reject(new Error('Chromium endpoint timeout')),10000);child.stderr!.on('data',chunk=>{stderr+=String(chunk);const match=stderr.match(/DevTools listening on (ws:\/\/\S+)/);if(match){clearTimeout(timer);resolve(match[1]!);}});child.on('error',reject);child.on('exit',()=>reject(new Error('Chromium exited before attaching')));});
  const browser=await chromium.connectOverCDP(endpoint,{noDefaults:true});
  return{browser,context:browser.contexts()[0]!,close:async()=>{await browser.newBrowserCDPSession().then(session=>session.send('Browser.close')).catch(()=>{});await browser.close();if(child.exitCode===null){child.kill();await once(child,'exit');}await rm(profile,{recursive:true,force:true,maxRetries:5,retryDelay:100});}};
 }catch(error){child.kill();await rm(profile,{recursive:true,force:true});throw error;}
}

test('native tab visibility pauses without catch-up and preserves manual pause intent',async({baseURL},info)=>{
 const native=await naturalBrowser();
 try{
  const page=native.context.pages()[0]!;const failures=errors(page);await page.setViewportSize({width:1440,height:1000});await ready(page,baseURL!);await page.bringToFront();await expect.poll(()=>page.evaluate(()=>document.visibilityState)).toBe('visible');await page.getByRole('button',{name:'Resume',exact:true}).click();await expect.poll(()=>tick(page)).toBeGreaterThan(4);
  const other=await native.context.newPage();await other.goto('about:blank');await other.bringToFront();await expect.poll(()=>page.evaluate(()=>document.visibilityState)).toBe('hidden');await expect(page.locator('#clock-status')).toHaveText('Hidden — paused');const hiddenTick=await tick(page);await page.waitForTimeout(900);expect(await tick(page)).toBe(hiddenTick);
  await page.bringToFront();await expect.poll(()=>page.evaluate(()=>document.visibilityState)).toBe('visible');const observed=JSON.parse((await page.locator('#visibility-evidence').textContent())!) as {state:string;tick:number;manualPaused:boolean}[];const hidden=observed.filter(e=>e.state==='hidden').at(-1)!,visible=observed.filter(e=>e.state==='visible').at(-1)!;expect(visible.tick).toBe(hidden.tick);expect(visible.manualPaused).toBe(false);await expect(page.locator('#clock-status')).toHaveText('Running at 1×');await expect.poll(()=>tick(page)).toBeGreaterThan(hiddenTick);
  await paused(page);const manualTick=await tick(page);await other.bringToFront();await expect.poll(()=>page.evaluate(()=>document.visibilityState)).toBe('hidden');await page.waitForTimeout(500);await page.bringToFront();await expect.poll(()=>page.evaluate(()=>document.visibilityState)).toBe('visible');await expect(page.locator('#clock-status')).toHaveText('Paused');expect(await tick(page)).toBe(manualTick);await page.screenshot({path:info.outputPath('visibility-resumed-manual-pause.png'),fullPage:true});expect(failures).toEqual([]);
  await info.attach('observed-native-visibility',{body:JSON.stringify({method:'Separate Chromium process; connectOverCDP noDefaults:true; native tab bringToFront. No visibility property override or synthetic event dispatch.',browserVersion:native.browser.version(),hiddenTick,manualTick,transitions:JSON.parse((await page.locator('#visibility-evidence').textContent())!),errors:failures},null,2),contentType:'application/json'});
 }finally{await native.close();}
});
