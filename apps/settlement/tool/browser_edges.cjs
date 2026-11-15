const {chromium}=require('playwright');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const out=path.resolve(process.argv[2]||'/tmp/settlement-edge-evidence');
fs.mkdirSync(out,{recursive:true});
(async()=>{
  const browser=await chromium.launch({executablePath:process.env.CHROMIUM_BIN||'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
  const context=await browser.newContext({viewport:{width:1024,height:640},deviceScaleFactor:1.5});
  const page=await context.newPage();
  await page.goto(process.env.SETTLEMENT_URL||'http://127.0.0.1:8765/settlement/');
  await page.locator('flt-semantics-placeholder').press('Enter');
  await page.getByRole('button',{name:'2 Path',exact:true}).waitFor();
  await page.waitForLoadState('networkidle');
  await page.keyboard.press('2',{delay:40});await page.mouse.click(432,314);
  const key='roag.settlement.landscape.v1';
  await page.waitForFunction(key=>JSON.parse(localStorage.getItem(key))?.lots[15*40+8][3],key);
  await page.mouse.move(25,36);await page.screenshot({path:path.join(out,'hidpi-150.png')});
  const before=await page.evaluate(key=>localStorage.getItem(key),key);
  // Inject a browser capability failure, never a world or gameplay command.
  await page.evaluate(()=>{
    window.originalStorageWrite=Storage.prototype.setItem;
    Storage.prototype.setItem=function(){throw new DOMException('Test quota exceeded','QuotaExceededError');};
  });
  await page.keyboard.press('1',{delay:40});await page.mouse.click(288,266);
  await page.waitForTimeout(150);
  await page.screenshot({path:path.join(out,'save-failure-toast.png')});
  await page.getByRole('button',{name:'Landscape menu',exact:true}).click();
  await page.getByRole('menuitem',{name:'Not saved · export a copy',exact:true}).waitFor();
  assert.equal(await page.evaluate(key=>localStorage.getItem(key),key),before);
  await page.screenshot({path:path.join(out,'save-failure.png')});
  await page.keyboard.press('Escape');
  await page.evaluate(()=>{Storage.prototype.setItem=window.originalStorageWrite;});
  await page.reload();await page.locator('flt-semantics-placeholder').press('Enter');
  await page.getByRole('button',{name:'Landscape menu',exact:true}).waitFor();
  assert.equal(await page.evaluate(key=>localStorage.getItem(key),key),before);
  await page.getByRole('button',{name:'Landscape menu',exact:true}).click();
  await page.getByRole('menuitem',{name:'New landscape…',exact:true}).click();
  await page.getByRole('button',{name:'New landscape',exact:true}).click();
  await page.waitForFunction(key=>JSON.parse(localStorage.getItem(key)).lots.filter(l=>l[3]).length===8,key);
  const result={browser:await browser.version(),devicePixelRatio:await page.evaluate(()=>devicePixelRatio),
    checks:['DPR 1.5 picking and rendering','Storage failure reported truthfully without overwriting saved copy',
      'Refresh restores saved copy after failed write','Confirmed New Landscape restores initial lane'],
    faultInjection:'Storage.prototype.setItem only; no internal game state injection'};
  fs.writeFileSync(path.join(out,'edge-results.json'),JSON.stringify(result,null,2));
  await context.close();await browser.close();console.log(JSON.stringify(result,null,2));
})().catch(e=>{console.error(e);process.exit(1);});
