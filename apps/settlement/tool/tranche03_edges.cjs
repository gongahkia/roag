const {chromium}=require('playwright');
const assert=require('node:assert/strict');
const fs=require('node:fs');const path=require('node:path');
const out=path.resolve(process.argv[2]||'/tmp/settlement-colony-edges');fs.mkdirSync(out,{recursive:true});
const key='roag.settlement.landscape.v1',url=process.env.SETTLEMENT_URL||'http://127.0.0.1:8767/settlement/';
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.CHROMIUM_BIN||'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
 const checks=[],errors=[];let page;
 try{
  async function open(context){page=await context.newPage();page.on('pageerror',e=>errors.push(e.message));await page.goto(url);await page.locator('#loading').waitFor({state:'detached'});await page.locator('flt-semantics-placeholder').press('Enter');await page.getByRole('button',{name:'Ⅱ',exact:true}).click();return page;}
  const button=name=>page.getByRole('button',{name,exact:typeof name==='string'});
  const read=()=>page.evaluate(k=>localStorage.getItem(k),key);
  const data=async()=>JSON.parse(await read());
  const shape=s=>JSON.stringify(s.lots);
  async function point(x,y){const a=await button(/^1 Cottage/).boundingBox(),m=await button('Landscape menu').boundingBox();return[24+(x+.5)*48,(m.y+m.height+12+a.y-14)/2+(y-15)*48];}
  async function stroke(tool,x,y,{hold=false}={}){await page.keyboard.press(String(tool));await page.mouse.move(...await point(x,y));await page.mouse.down();await page.waitForTimeout(80);if(!hold){await page.mouse.up();await page.waitForTimeout(100);}}
  async function menu(name){await button('Landscape menu').click();await page.getByRole('menuitem',{name,exact:true}).click();}
  const c=await browser.newContext({viewport:{width:1024,height:640},deviceScaleFactor:1.5});
  await c.addInitScript(()=>{window.soundCount=0;const create=AudioContext.prototype.createOscillator;AudioContext.prototype.createOscillator=function(...args){window.soundCount++;window.lastAudio=this;return create.apply(this,args);};});
  await open(c);
  await stroke(1,3,14);let stable=await data();
  await stroke(4,5,14,{hold:true});await page.keyboard.press('Escape');await page.mouse.up();assert.equal(shape(await data()),shape(stable));assert.equal((await data()).colony.money,stable.colony.money);
  await stroke(4,5,14,{hold:true});await page.keyboard.press('Control+z');await page.mouse.up();assert.equal(shape(await data()),shape(stable));
  await stroke(4,5,14,{hold:true});await page.keyboard.press('5');await page.mouse.up();assert.equal(shape(await data()),shape(stable));
  const p=await point(5,14),cdp=await c.newCDPSession(page);await page.keyboard.press('4');
  await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:p[0],y:p[1]}]});await cdp.send('Input.dispatchTouchEvent',{type:'touchCancel',touchPoints:[]});assert.equal(shape(await data()),shape(stable));
  checks.push('Escape, unfinished Undo, tool switch and browser touch cancellation cancel priced strokes');
  await stroke(1,4,14,{hold:true});const first=await button(/^1 Cottage/).boundingBox();await page.mouse.move(100,first.y+10);await page.mouse.up();let next=await data();assert.equal(next.lots[14*40+4][4]!==null,true);assert.equal(next.colony.money,stable.colony.money-5000);
  await page.keyboard.press('Control+z');assert.equal(shape(await data()),shape(stable));
  await page.keyboard.down('Space');await page.mouse.move(...await point(8,18));await page.mouse.down();await page.mouse.move(700,430,{steps:4});await page.keyboard.up('Space');await page.mouse.move(730,440);await page.mouse.up();assert.equal(shape(await data()),shape(stable));await button('Return to lane (H)').click();
  await stroke(1,4,14,{hold:true});await page.keyboard.down('Space');await page.mouse.move(...await point(5,14));await page.mouse.up();await page.keyboard.up('Space');assert.equal((await data()).colony.money,stable.colony.money-10000);await page.keyboard.press('Control+z');
  checks.push('Canvas exit commits the accepted prefix; pan/build mode survives modifier changes');
  stable=await data();await button('Inspect (I)').click();await page.mouse.click(...await point(3,14));await page.waitForTimeout(200);await page.screenshot({path:path.join(out,'readonly-inspection.png')});await page.keyboard.press('Escape');assert.deepEqual(await data(),stable);
  await menu('Unlimited building: Off');const money=(await data()).colony.money;await stroke(5,6,14);assert.equal((await data()).colony.money,money);await page.keyboard.press('Control+z');assert.equal((await data()).colony.money,money);await menu('Unlimited building: On');
  checks.push('Read-only inspection leaves the checkpoint unchanged; unlimited placement and its undo charge zero');
  const sounds=await page.evaluate(()=>soundCount);
  await button('Mute').click();await stroke(1,4,14);assert.equal(await page.evaluate(()=>soundCount),sounds);await page.keyboard.press('Control+z');
  await button('Unmute').click();await stroke(1,4,14);assert((await page.evaluate(()=>soundCount))>sounds);await page.keyboard.press('Control+z');assert.equal(await page.evaluate(()=>lastAudio.state),'running');
  checks.push('Existing user-gesture audio runs; mute suppresses creation of placement and undo tones');
  await button('1×').click();await page.waitForTimeout(4500);const behindBubbles=shape(await data());
  await stroke(4,4,14,{hold:true});await page.screenshot({path:path.join(out,'bubble-input-staged.png')});
  await page.keyboard.press('Escape');await page.mouse.up();await button('Ⅱ').click();assert.equal(shape(await data()),behindBubbles);
  checks.push('A priced brush stages through the speech layer and cancels without construction changes');
  const beforeFailure=await read();await page.evaluate(k=>{const write=Storage.prototype.setItem;Storage.prototype.setItem=function(key,value){if(key===k)throw new DOMException('Test quota','QuotaExceededError');return write.call(this,key,value);};},key);
  await stroke(1,4,14);assert.equal(await read(),beforeFailure);await page.screenshot({path:path.join(out,'save-failure.png')});await button('Landscape menu').click();await page.getByRole('menuitem',{name:'Not saved · export a copy',exact:true}).waitFor();await page.keyboard.press('Escape');
  await page.reload();await page.locator('#loading').waitFor({state:'detached'});assert.equal(await read(),beforeFailure);
  checks.push('DPR 1.5 picking; truthful storage-failure feedback preserves the previous saved checkpoint and refresh recovers it');
  await c.close();

  const legacy=fs.readFileSync(path.resolve('../../docs/world-builder/evidence/tranche-03/legacy-74.json'),'utf8');
  const migration=await browser.newContext({viewport:{width:1024,height:640}});
  await migration.addInitScript(({key,text})=>{if(!localStorage.getItem(key))localStorage.setItem(key,text);},{key,text:legacy});await open(migration);
  let migrated=await data();assert.equal(migrated.colony.residents.length,74);assert.equal(migrated.colony.money,200000+migrated.colony.grantEarned);
  assert.equal(await page.evaluate(k=>localStorage.getItem(k+'.before-colony-v3'),key),legacy);assert.equal(await button('Undo').isDisabled(),true);
  const snapshot=await read();await page.reload();await page.locator('#loading').waitFor({state:'detached'});assert.equal(await read(),snapshot);
  await page.screenshot({path:path.join(out,'migration-backed-up.png')});checks.push('Fixture-assisted schema-2 autoload: original 74-home data backed up exactly, neutral starter economy once, no retroactive history refund, reload has no second grant');
  await migration.close();
  const backupFailure=await browser.newContext({viewport:{width:1024,height:640}});
  await backupFailure.addInitScript(({key,text})=>{if(!localStorage.getItem(key))localStorage.setItem(key,text);const write=Storage.prototype.setItem;Storage.prototype.setItem=function(k,v){if(k.endsWith('.before-colony-v3'))throw Error('Test backup failure');return write.call(this,k,v);};},{key,text:legacy});await open(backupFailure);
  await page.waitForTimeout(3200);assert.equal(await read(),legacy);await page.screenshot({path:path.join(out,'backup-failure-protected.png')});
  checks.push('Fixture-assisted backup failure protects the old primary save instead of overwriting it');
  await backupFailure.close();
  const night=await browser.newContext({viewport:{width:1024,height:640}});await open(night);
  const file=page.waitForEvent('filechooser');await menu('Import landscape…');await(await file).setFiles(path.resolve('../../docs/world-builder/evidence/tranche-03/colony-night-cut.json'));
  await page.waitForTimeout(200);const cut=await data();assert(cut.colony.residents.some(r=>r.leisureIssue==='socialCut'));
  await page.waitForTimeout(200);await page.screenshot({path:path.join(out,'night-cut.png')});
  await stroke(2,9,15);const restored=await data();assert(restored.colony.residents.every(r=>r.leisureIssue!=='socialCut'));assert.equal(restored.colony.clock,cut.colony.clock);assert.equal(restored.colony.money,cut.colony.money);
  await page.waitForTimeout(200);await page.screenshot({path:path.join(out,'night-restored.png')});
  checks.push('Fixture-assisted night route repair immediately clears the resolved green complaint without advancing time or awarding money');await night.close();
  const reload=await browser.newContext({viewport:{width:1024,height:640}});page=await reload.newPage();page.on('pageerror',e=>errors.push(e.message));
  await page.goto(url);await page.locator('#loading').waitFor({state:'detached'});await page.waitForTimeout(1200);
  await page.reload();await page.locator('#loading').waitFor({state:'detached'});await page.locator('flt-semantics-placeholder').press('Enter');await button('Ⅱ').click();
  const resumed=await data();assert(resumed.colony.clock>=1);assert.equal(resumed.colony.money,200000+resumed.colony.grantEarned);
  checks.push('Ordinary active reload checkpoints foreground progress before leaving, without a second starter grant');await reload.close();
  assert.deepEqual(errors,[]);

  fs.writeFileSync(path.join(out,'edge-results.json'),JSON.stringify({browser:browser.version(),devicePixelRatio:1.5,checks,errors,fixtureAssistance:'Only the two migration profiles receive the actual old-format fixture via browser storage; main input tests use a normal new game. Fault injection touches Storage.setItem only.'},null,2));console.log(JSON.stringify({checks,errors},null,2));
 }catch(e){if(page)await page.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});throw e;}
 finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
