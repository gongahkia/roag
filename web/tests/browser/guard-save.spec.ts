import { test, expect } from '@playwright/test';
import type { Page, TestInfo } from '@playwright/test';
import { readFile, writeFile } from 'node:fs/promises';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import type { Save } from '../../src/persistence/schema.ts';
import { parseSave } from '../../src/persistence/schema.ts';
import { canonical } from '../../src/simulation/canonical.ts';

async function snapshot(page: Page, info: TestInfo, name: string): Promise<Save> {
  const pending=page.waitForEvent('download');await page.locator('#export').click();
  const download=await pending,path=info.outputPath(`${name}.json`);await download.saveAs(path);
  return parseSave(await readFile(path,'utf8'));
}

test('guard priority automatically rallies at a built post with coordinate-only save and real reload',async({page},info)=>{
  test.setTimeout(120000);
  const failures:string[]=[];
  page.on('pageerror',error=>failures.push(error.message));
  page.on('console',message=>{if(message.type()==='error')failures.push(message.text());});
  page.on('requestfailed',request=>failures.push(`${request.url()} ${request.failure()?.errorText}`));
  page.on('response',response=>{if(response.status()>=400)failures.push(`${response.status()} ${response.url()}`);});
  // Focused regression fixture: the same finite gathering/building/production
  // commands as the ordinary headless defense scenario, advanced to the warning.
  // The separate combat journey constructs a fresh colony through visible UI.
  const fixturePath=process.env.ROAG_GUARD_FIXTURE;
  const raw=fixturePath?await readFile(fixturePath,'utf8'):execFileSync(process.execPath,['--input-type=module','--eval',
    "import {fortify} from './tests/helpers/defense.mjs'; import {makeSave} from './src/persistence/schema.ts'; const h=fortify(); h.tick(11990-h.w.tick); process.stdout.write(JSON.stringify(makeSave(h.w,{manualPaused:true,speed:1})));"],
    {cwd:fileURLToPath(new URL('../..',import.meta.url)),maxBuffer:8*1024*1024}).toString();
  const before=parseSave(raw);expect(before.world.tick).toBe(11990);
  expect(before.controls).toEqual({manualPaused:true,speed:1});
  expect(before.world.structures.some(s=>s.kind==='guardpost'&&s.x===62&&s.y===62)).toBe(true);
  await writeFile(info.outputPath('ordinary-before-spawn.json'),raw);
  await page.goto('./');await expect(page.locator('#stage')).toHaveAttribute('data-ready','true');
  await page.locator('#import-file').setInputFiles({name:'ordinary-before-spawn.json',mimeType:'application/json',buffer:Buffer.from(raw)});
  await expect(page.locator('#storage-status')).toContainText('Imported and saved tick 11990');
  await expect(page.locator('#clock-status')).toHaveText('Paused');
  await page.getByRole('button',{name:'Find Ada Rowan, Builder',exact:true}).click();
  await page.getByLabel('Ada Rowan guard priority',{exact:true}).selectOption('3');
  await expect(page.locator('#storage-status')).toContainText('Work command queued');
  await page.locator('#pause').focus();await page.keyboard.press('Enter');
  await expect.poll(async()=>Number(await page.locator('#tick').textContent())).toBeGreaterThanOrEqual(12001);
  await page.keyboard.press('Enter');await expect(page.locator('#clock-status')).toHaveText('Paused');
  await expect(page.locator('#battle-current-order')).toHaveText('rally at 62, 62');
  const rallied=await snapshot(page,info,'automatic-rally-before-reload');
  expect(rallied.world.combat.phase).toBe('combat');
  expect(rallied.world.people[0]!.body.duties.guard).toBe(3);
  const order=rallied.world.combat.orders.find(order=>order.actor===2)!;
  expect(order.mode).toBe('rally');expect(order.target).toEqual({x:62,y:62});
  expect(Object.keys(order.target).sort()).toEqual(['x','y']);
  expect(order.resume).toEqual({x:61,y:67});
  for(const current of rallied.world.combat.orders){
    expect(Object.keys(current.target).sort()).toEqual(['x','y']);
    if(current.resume)expect(Object.keys(current.resume).sort()).toEqual(['x','y']);
  }
  await page.locator('#save').click();await expect(page.locator('#storage-status')).toHaveText(`Saved tick ${rallied.world.tick}.`);
  await page.reload();await expect(page.locator('#stage')).toHaveAttribute('data-ready','true');
  await expect(page.locator('#storage-status')).toHaveText(`Restored tick ${rallied.world.tick}.`);
  const reloaded=await snapshot(page,info,'automatic-rally-after-reload');expect(reloaded).toEqual(rallied);
  await page.getByRole('button',{name:'Find Ada Rowan, Builder',exact:true}).click();
  await expect(page.locator('#battle-current-order')).toHaveText('rally at 62, 62');
  await page.locator('.defense-panel').screenshot({path:info.outputPath('automatic-guard-rally.png')});
  expect(failures).toEqual([]);
  await info.attach('guard-save-evidence',{body:JSON.stringify({fixture:fixturePath??'Generated from ordinary fortify() commands',fixtureSha256:createHash('sha256').update(raw).digest('hex'),initialTick:before.world.tick,savedTick:rallied.world.tick,order,worldSha256:createHash('sha256').update(canonical(rallied.world)).digest('hex'),exactReload:true,errors:failures},null,2),contentType:'application/json'});
});
