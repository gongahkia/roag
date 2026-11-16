// Real release UI: pointer/keyboard/file menus. No simulation callbacks or writes
// to gameplay state; isolated legacy preloads and fault injection are labelled.
const {chromium}=require('playwright'),fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const out=path.resolve(process.argv[2]||'/var/tmp/roag-goal09-browser');fs.mkdirSync(out,{recursive:true});
const url=process.env.SETTLEMENT_URL||'http://127.0.0.1:8768/roag/settlement/',dir=process.env.SETTLEMENT_FIXTURE_DIR||'/var/tmp/roag-goal09-fixtures',key='roag.settlement.landscape.v1';
let browser;
(async()=>{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
 const context=await browser.newContext({viewport:{width:1024,height:640},recordVideo:{dir:out,size:{width:1024,height:640}}}),page=await context.newPage(),errors=[],failedAssets=[],checks=[],shots=[],outcomes={};
 page.on('pageerror',e=>errors.push(e.message));page.on('response',r=>{if(r.status()>=400)failedAssets.push([r.status(),r.url()]);});
 const started=Date.now(),pass=t=>{checks.push(t);console.log('PASS',t);};
 const btn=n=>page.getByRole('button',{name:n,exact:typeof n==='string'}),state=()=>page.evaluate(k=>JSON.parse(localStorage.getItem(k)),key);
 const safe=s=>s.colony.preparedness,people=s=>s.colony.residents.filter(r=>r.active);
 async function ready(){await page.locator('#loading').waitFor({state:'detached'});const a=page.locator('flt-semantics-placeholder');if(await a.count())await a.press('Enter');await btn(/^1 Cottage/).waitFor();}
 async function menu(n){await btn('Landscape menu').click();await page.getByRole('menuitem',{name:n,exact:true}).click();}
 async function snap(n){await page.mouse.move(28,32);await page.waitForTimeout(100);await page.screenshot({path:path.join(out,n+'.png')});shots.push({name:n,seconds:(Date.now()-started)/1000});}
 let zoom=4,cy,width=1024,center;
 async function layout(){const t=await btn(/^1 Cottage/).boundingBox(),m=await btn('Landscape menu').boundingBox();width=await page.evaluate(()=>innerWidth);cy=(m.y+m.height+12+t.y-14)/2;center=Math.max(9,(width/2-24)/48);}
 const pt=(x,y)=>[width/2+(x+.5-center)*12*zoom,cy+(y-15)*12*zoom];
 async function home(){await btn('Return to lane (H)').click();zoom=4;await layout();}
 async function load(name){const file=path.isAbsolute(name)?name:path.join(dir,name+'.json');const chooser=page.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles(file);await btn('Replace settlement').click();await page.waitForTimeout(250);zoom=4;await layout();}
 async function tool(t){if(typeof t==='number'){await page.keyboard.press(''+t);return;}await btn(/Build catalogue/).click();const it=page.getByRole('menuitem',{name:new RegExp('^(?:[0-9] )?'+t+' ·')});await it.scrollIntoViewIfNeeded();await it.click();}
 async function stroke(t,a,b=a){await tool(t);await page.mouse.move(...pt(...a));await page.mouse.down();await page.mouse.move(...pt(...b),{steps:8});await page.mouse.up();await page.waitForTimeout(100);}
 async function pause(){await btn('Ⅱ').click();}
 async function wait(fn,timeout=70000){await page.waitForFunction(({k,f})=>Function('s',`return (${f})(s)`)(JSON.parse(localStorage.getItem(k))),{k:key,f:fn.toString()},{timeout});}
 async function inspect(c){await page.keyboard.press('Escape');await page.keyboard.press('i');await page.mouse.click(...pt(...c));await page.waitForTimeout(100);}
 await page.goto(url);await ready();if(await btn('Start building').count())await btn('Start building').click();await pause();await layout();
 // This uninterrupted segment is preparation -> ignition -> real response -> repair.
 await load('preparation');
 await stroke('Watchpost',[12,16]);await stroke('Refuge',[14,16]);await stroke('Inn',[14,17]);
 let s=await state();assert.equal(people(s).filter(r=>r.workId?.startsWith('firehouse-')).length,2);assert.equal(s.colony.unlimited,false);
 await inspect([10,16]);await snap('01-prepared-firehouse');await page.keyboard.press('Escape');pass('Paid Watchpost, Refuge and Inn constructed through catalogue; two automatically trained Firehouse staff already present');
 await btn('1×').click();await page.waitForTimeout(7200);await pause();s=await state();assert.equal(safe(s).active.kind,'fire');assert.equal(safe(s).active.origin,'fixture');assert(people(s).some(r=>r.safetyRole==='fire'&&['responseTravel','responding'].includes(r.activity)));await snap('02-actual-fire-response');
 const active=s.colony;await page.reload();await ready();assert.deepEqual((await state()).colony,active);await layout();pass('Active fire, real responders, damage and seeded state reload exactly while paused');
 await btn('1×').click();await wait(s=>s.colony.preparedness.history.length===1);await pause();s=await state();const h=safe(s).history[0];assert(h.responders>0);assert(h.damage>0);assert.equal(safe(s).active,null);outcomes.continuous=h;
 await snap('03-contained-aftermath');
 const damaged=Object.entries(safe(s).conditions).filter(([id,c])=>id.startsWith('home-')&&c.health<100);assert(damaged.length>0);
 const beforeMoney=s.colony.money,beforeHealth=Object.fromEntries(damaged.map(([id,c])=>[id,c.health])),ids=people(s).map(r=>[r.id,r.name,r.bodyId]);
 await stroke('Repair',[2,14],[7,14]);s=await state();assert(damaged.every(([id])=>safe(s).conditions[id].health===100));assert(s.colony.money<beforeMoney);assert.deepEqual(people(s).map(r=>[r.id,r.name,r.bodyId]),ids);
 const repairedMoney=s.colony.money;await page.keyboard.press('Control+z');await page.waitForTimeout(120);s=await state();assert.equal(s.colony.money,beforeMoney);for(const [id,health]of Object.entries(beforeHealth))assert.equal(safe(s).conditions[id].health,health);
 await page.keyboard.press('Control+Shift+z');await page.waitForTimeout(120);s=await state();assert.equal(s.colony.money,repairedMoney);assert(damaged.every(([id])=>safe(s).conditions[id].health===100));await snap('04-repaired-same-neighbourhood');
 pass('Continuous preparation, ignition, autonomous arrival/containment and paid drag repair; fresh repair Undo/Redo retains original people and exact charge');
 // Save the continuous segment independently before later scenario imports.
 const video=page.video();await context.close();await video.saveAs(path.join(out,'preparation-response-recovery.webm'));
 await browser.close();
 fs.writeFileSync(path.join(out,'results-continuous.json'),JSON.stringify({url,input:'Playwright pointer/keyboard/file menus; actual release. Imported deterministic fixture is labelled, not a natural incident.',checks,errors,failedAssets,shots,outcomes},null,2));
 assert.deepEqual(errors,[]);assert.deepEqual(failedAssets,[]);
})().catch(async e=>{console.error(e);if(browser)await browser.close();process.exitCode=1});
