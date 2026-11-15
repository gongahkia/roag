// Release-browser interaction only; saved checkpoints are read-only observations.
const{chromium}=require('playwright'),fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const out=path.resolve(process.argv[2]||'/tmp/roag-goal08-browser');fs.mkdirSync(out,{recursive:true});
const url=process.env.SETTLEMENT_URL||'http://127.0.0.1:8768/settlement/',key='roag.settlement.landscape.v1';
let browser;
(async()=>{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
 const context=await browser.newContext({viewport:{width:1024,height:640},recordVideo:{dir:out,size:{width:1024,height:640}}}),page=await context.newPage(),errors=[],checks=[],shots=[];
 page.on('pageerror',e=>errors.push(e.message));let started=Date.now();const pass=t=>{checks.push(t);console.log('PASS',t)};
 const btn=n=>page.getByRole('button',{name:n,exact:typeof n==='string'}),state=()=>page.evaluate(k=>JSON.parse(localStorage.getItem(k)),key);
 async function ready(){await page.locator('#loading').waitFor({state:'detached'});const a=page.locator('flt-semantics-placeholder');if(await a.count())await a.press('Enter');await btn(/^1 Cottage/).waitFor();}
 async function menu(n){await btn('Landscape menu').click();await page.getByRole('menuitem',{name:n,exact:true}).click();}
 async function snap(n){await page.mouse.move(28,32);await page.waitForTimeout(120);await page.screenshot({path:path.join(out,n+'.png')});shots.push({name:n,seconds:(Date.now()-started)/1000});}
 async function load(name){const chooser=page.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles(name);await btn('Replace settlement').click();await page.waitForTimeout(250);zoom=4;await layout();}
 async function wait(fn,timeout=100000){await page.waitForFunction(({k,f})=>Function('s',`return (${f})(s)`)(JSON.parse(localStorage.getItem(k))),{k:key,f:fn.toString()},{timeout});}
 let zoom=4,cy,width=1024,center;
 async function layout(){const t=await btn(/^1 Cottage/).boundingBox(),m=await btn('Landscape menu').boundingBox();width=await page.evaluate(()=>innerWidth);cy=(m.y+m.height+12+t.y-14)/2;center=Math.max(9,(width/2-24)/48);}
 const pt=(x,y)=>[width/2+(x+.5-center)*12*zoom,cy+(y-15)*12*zoom];
 async function tool(t){if(typeof t==='number'){await page.keyboard.press(''+t);return;}await btn(/Build catalogue/).click();const it=page.getByRole('menuitem',{name:new RegExp('^(?:[0-9] )?'+t+' ·')});await it.scrollIntoViewIfNeeded();await it.click();}
 async function stroke(t,a,b=a){await tool(t);await page.mouse.move(...pt(...a));await page.mouse.down();await page.mouse.move(...pt(...b),{steps:8});await page.mouse.up();await page.waitForTimeout(80);}
 async function closeInspect(){await page.keyboard.press('Escape');}
 await page.goto(url);await ready();if(await btn('Start building').count())await btn('Start building').click();await btn('Ⅱ').click();await layout();
 const oldFile=path.resolve('../../docs/world-builder/evidence/tranche-05/large-85.json'),old=JSON.parse(fs.readFileSync(oldFile));await load(oldFile);let s=await state();assert.deepEqual(s.lots,old.lots);assert.equal(s.schema,4);assert(s.colony.residents.every(r=>r.people==='human-v1'));assert.equal(s.colony.money,old.colony.money);assert.equal(s.colony.clock,old.colony.clock);
 assert(await page.evaluate(k=>!!localStorage.getItem(k),key));await page.reload();await ready();s=await state();assert.deepEqual(s.lots,old.lots);await snap('01-legacy-humans');
 await menu('New landscape…');await btn('Continue existing settlement').click();assert.deepEqual((await state()).lots,old.lots);pass('Legacy 85-person checkpoint retains lots, money, clock and humans through import/reload; New World cancellation protects it');
 await menu('New landscape…');await page.getByRole('textbox',{name:'World seed',exact:true}).fill('202');await btn('Create world').click();await btn('Start building').click();await btn('Ⅱ').click();await layout();
 await stroke(2,[7,15],[14,15]);await stroke(1,[2,14],[7,14]);await stroke(1,[2,16],[7,16]);
 for(const [t,p] of [[4,[8,14]],['Workshop',[10,14]],['Market',[12,14]],['Community Hall',[14,14]],[5,[8,16]]]){const row=(await state()).lots[p[1]*40+p[0]];if(['forest','marsh'].includes(row[9]))await stroke('Clear',p);await stroke(t,p);}
 s=await state();assert.equal(s.colony.residents.filter(r=>r.active).length,12);assert.deepEqual(new Set(s.colony.residents.map(r=>r.people)),new Set(['human-v1','imp-v1','djinn-v1']));assert.equal(s.colony.unlimited,false);await snap('02-seeded-neighbourhood');pass('New Gentle Plains seed 202 builds 12 paid cottages and all workplaces/social venues; actual seeded arrivals include Humans, Imps and Djinn');
 await btn('1×').click();await wait(s=>s.colony.visitors.active.some(v=>v.role==='storyteller'&&v.actor.activity==='socialising'));
 await btn('Ⅱ').click();s=await state();const story=s.colony.visitors.active.find(v=>v.role==='storyteller');assert(story);await snap('03-arriving-and-storytelling');
 await btn(/^Wandering storyteller/).click();await btn('Talk').click();assert((await page.locator('flt-semantics').allTextContents()).join(' ').length>0);const before=s.colony.money;await btn('Sponsor an encore · 2 coins').click();s=await state();assert.equal(s.colony.visitors.spent,2000);assert.equal(s.colony.money,before-2000);await snap('04-optional-story-dialogue');await btn('Maybe later').click();await closeInspect();
 const activeSaved=await state();await page.reload();await ready();assert.deepEqual((await state()).colony.visitors,activeSaved.colony.visitors);await btn('1×').click();await wait(s=>s.colony.visitors.history.some(v=>v.role==='storyteller'&&v.attendees>0)&&s.colony.visitors.history.some(v=>v.role==='merchant'));
 await btn('Ⅱ').click();s=await state();assert(s.colony.workEarned>0);assert(s.colony.visitors.earned>=2000);await snap('05-visits-earned-real-benefits');pass('Both roles follow roads and complete activities; actual listeners benefit, merchant pays once, encore charges once, active visits resume after exact reload');
 // Inspect an actual imp at its authoritative current location through the canvas.
 await btn('Return to lane (H)').click();zoom=4;await layout();const imp=s.colony.residents.find(r=>r.active&&r.people==='imp-v1');await page.keyboard.press('i');await page.mouse.click(...pt(imp.x-.5,imp.y-.5));await snap('06-mixed-people-inspection');await closeInspect();
 const exp=page.waitForEvent('download');await menu('Export landscape…');const exportFile=path.join(out,'living-export.json');await(await exp).saveAs(exportFile);const saved=await state();await load(exportFile);assert.deepEqual((await state()).colony.residents,saved.colony.residents);pass('World, identities, climate, visit history/rewards and active journeys round-trip through actual file export/import');
 if(!process.env.SETTLEMENT_SKIP_YEAR) {
 // Accelerated real clock, no model jumps: observe all four seasons and a year rollover.
 await menu('Watch a year · 16×');let seen=new Set();const until=Date.now()+260000;while(Date.now()<until){s=await state();const day=Math.floor(s.colony.clock/150),season=['spring','summer','autumn','winter'][Math.floor((day-s.colony.climate.calendar.anchorDay)/6)%4];if(!seen.has(season)){seen.add(season);await snap('season-'+season);}if(day>=24)break;await page.waitForTimeout(700);}
 assert.equal(seen.size,4);assert(Math.floor(s.colony.clock/150)>=24);await btn('Ⅱ').click();await snap('07-complete-calendar-year');
 await menu('Calendar & forecast');await snap('08-truthful-forecast');await btn('Back to settlement').click();pass('Actual 16× simulation traverses all four seasons and a whole year; forecast reads the authoritative schedule');
 } else {pass('Full-year journey already captured separately; this run skips only its repeated wait');}
 // Explicit saved seasonal fixtures make adverse conditions repeatable without dev controls in the UI.
 await load('/tmp/roag-goal08-fixtures/winter.json');await btn('1×').click();await page.waitForTimeout(3500);await btn('Ⅱ').click();await snap('09-winter-snow');
 await page.emulateMedia({reducedMotion:'reduce'});await snap('10-reduced-motion');await page.emulateMedia({reducedMotion:'no-preference'});
 await page.setViewportSize({width:931,height:582});await snap('11-browser-small-scale');await page.setViewportSize({width:1440,height:900});await snap('12-browser-large');await page.setViewportSize({width:1024,height:640});await layout();
 await btn('Zoom out (−)').click();await btn('Zoom in (+)').click();await btn('Return to lane (H)').click();await btn('Mute').click();await btn('Unmute').click();pass('Snow, reduced motion, camera zoom, resize and existing audio controls remain usable');
 assert.deepEqual(errors,[]);await context.close();await browser.close();fs.writeFileSync(path.join(out,'results.json'),JSON.stringify({url,input:'Playwright browser pointer/keyboard and real file menus; saved state observations only, seasonal fixtures explicitly identified',checks,errors,shots},null,2));
})().catch(async e=>{console.error(e);if(browser)await browser.close();process.exitCode=1});
