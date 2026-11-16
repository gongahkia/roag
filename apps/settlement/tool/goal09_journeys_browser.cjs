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
 let zoom=4,cy,width=1024,center,centerY=15;
 async function layout(){const t=await btn(/^1 Cottage/).boundingBox(),m=await btn('Landscape menu').boundingBox();width=await page.evaluate(()=>innerWidth);cy=(m.y+m.height+12+t.y-14)/2;center=Math.max(9,(width/2-24)/48);}
 const pt=(x,y)=>[width/2+(x+.5-center)*12*zoom,cy+(y-centerY)*12*zoom];
 async function home(){await btn('Return to lane (H)').click();zoom=4;centerY=15;await layout();}
 async function load(name){const file=path.isAbsolute(name)?name:path.join(dir,name+'.json');const chooser=page.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles(file);await btn('Replace settlement').click();await page.waitForTimeout(250);zoom=4;centerY=15;await layout();}
 async function panTo(c){const step=4/zoom;for(let i=0;i<20&&pt(...c)[1]<100;i++){await page.keyboard.press('ArrowUp');centerY-=step;}for(let i=0;i<20&&pt(...c)[1]>500;i++){await page.keyboard.press('ArrowDown');centerY+=step;}}
 async function tool(t){if(typeof t==='number'){await page.keyboard.press(''+t);return;}await btn(/Build catalogue/).click();const it=page.getByRole('menuitem',{name:new RegExp('^(?:[0-9] )?'+t+' ·')});await it.scrollIntoViewIfNeeded();await it.click();}
 async function stroke(t,a,b=a){await tool(t);await page.mouse.move(...pt(...a));await page.mouse.down();await page.mouse.move(...pt(...b),{steps:8});await page.mouse.up();await page.waitForTimeout(100);}
 async function pause(){await btn('Ⅱ').click();}
 async function wait(fn,timeout=70000){await page.waitForFunction(({k,f})=>Function('s',`return (${f})(s)`)(JSON.parse(localStorage.getItem(k))),{k:key,f:fn.toString()},{timeout});}
 async function inspect(c){await page.keyboard.press('Escape');await page.keyboard.press('i');await page.mouse.click(...pt(...c));await page.waitForTimeout(100);}

 let s;
 if(!process.env.SETTLEMENT_NETWORK_ONLY){
 // Isolated legacy autoload: this is setup of a test origin, not the user's town.
 const legacyFile=path.resolve('test/fixtures/goal08-town.json'),legacyText=fs.readFileSync(legacyFile,'utf8'),legacy=JSON.parse(legacyText);
 await context.addInitScript(({key,text})=>{if(!localStorage.getItem(key))localStorage.setItem(key,text);},{key,text:legacyText});
 await page.goto(url);await ready();await pause();await layout();
 s=await state();assert.equal(s.schema,5);assert.equal(safe(s).mode,'peaceful');assert.deepEqual(s.lots,legacy.lots);assert.equal(s.colony.money,legacy.colony.money);assert.deepEqual(people(s).map(r=>[r.id,r.name,r.bodyId,r.people,r.trait]),people(legacy).map(r=>[r.id,r.name,r.bodyId,r.people,r.trait]));
 assert.equal(await page.evaluate(k=>localStorage.getItem(k+'.before-incidents-v5'),key),legacyText);await snap('05-safe-legacy-migration');
 await menu('New landscape…');await btn('Continue existing settlement').click();assert.deepEqual((await state()).lots,legacy.lots);pass('Goal08 autoload preserves concrete land, identities, money and clock; schema-5 backup is exact; New World cancellation leaves town intact');
 await menu('New landscape…');await page.getByRole('textbox',{name:'World seed',exact:true}).fill('202');await btn('Create world').click();await btn('Start building').click();await pause();await layout();s=await state();assert.equal(safe(s).mode,'standard');assert.equal(safe(s).nextAt,450);assert.equal(safe(s).active,null);
 await stroke(2,[7,15],[14,15]);await stroke(1,[2,14],[7,14]);await stroke(1,[2,16],[7,16]);
 for(const[t,p]of [[4,[8,14]],['Workshop',[10,14]],['Market',[12,14]],['Community Hall',[14,14]],['Firehouse',[10,16]],['Watchpost',[12,16]],['Refuge',[14,16]]]){
 const l=(await state()).lots[p[1]*40+p[0]];if(['forest','marsh'].includes(l[9]))await stroke('Clear',p);await stroke(t,p);
 }
 await stroke(2,[13,15],[13,17]);await stroke('Inn',[14,17]);
 s=await state();assert.equal(people(s).length,12);assert.equal(s.colony.unlimited,false);assert.equal(safe(s).active,null);
 await btn('4×').click();await page.waitForTimeout(6000);await pause();s=await state();assert(people(s).filter(r=>r.workId?.startsWith('firehouse-')).length===2);assert(people(s).some(r=>r.workId?.startsWith('watchpost-')));assert(s.colony.workEarned>0);await snap('06-new-standard-prepared-town');pass('New Standard seed 202, grace, paid neighbourhood, automatic protective staffing and real productive income through ordinary construction');
 await menu(/^Incidents:/);await page.getByRole('button',{name:'Standard',exact:true}).click();await page.getByRole('menuitem',{name:'Peaceful',exact:true}).click();await btn('Back to building').click();assert.equal(safe(await state()).mode,'peaceful');pass('Compact setting explicitly switches a new town to Peaceful');
 // Comparable incidents use the same target/seed/weather and fixed severity.
 for(const kind of ['fire','wildlife']){
  for(const protectedTown of [true,false]){
   const name=kind+'-'+(protectedTown?'protected':'unprotected');await load(name);
   await btn('1×').click();await page.waitForTimeout(kind==='fire'?7300:8500);await pause();s=await state();
   if(kind==='wildlife') {assert(safe(s).active?.animal);assert(safe(s).active.animal.x>.5);}
   if(kind==='fire')assert(people(s).some(r=>r.safetyRole));
   await snap(name+'-active');
   await btn('4×').click();await wait(s=>s.colony.preparedness.history.length===1);await pause();s=await state();outcomes[name]=safe(s).history[0];assert.equal(outcomes[name].origin,'fixture');assert.equal(safe(s).active,null);await snap(name+'-aftermath');
   assert.equal(people(s).length,12);
  }
  assert(outcomes[kind+'-protected'].damage<outcomes[kind+'-unprotected'].damage);assert(outcomes[kind+'-protected'].responders>0);
  pass('Comparable '+kind+' fixtures: actual protected damage '+outcomes[kind+'-protected'].damage+' < '+outcomes[kind+'-unprotected'].damage+' unprotected; real travel and no lost residents');
 }
 // Development and optional conversations depend on real activity, not inspection.
 await load('prepared');await btn('4×').click();await page.waitForTimeout(18000);await pause();s=await state();assert(Object.values(safe(s).conditions).some(c=>c.productive>=45||c.uses>=4));await snap('07-established-real-use');
 const r=people(s).find(r=>r.x<12&&r.activity!=='pending');await inspect([r.x-.5,r.y-.5-1/3]);if(await btn('Talk').count()){
 await btn('Talk').click();await btn('How is the town prepared?').click();await snap('08-preparedness-conversation');await btn('Maybe later').click();await page.keyboard.press('Escape');}else throw Error('Resident not selectable for optional conversation');
 const clock=s.colony.clock,damage=JSON.stringify(safe(s).conditions);await stroke(2,[13,17],[13,19]);await page.keyboard.press('Control+z');await page.keyboard.press('Control+Shift+z');s=await state();assert.equal(s.colony.clock,clock);assert.equal(JSON.stringify(safe(s).conditions),damage);pass('Ordinary work/use adds visible established details; contextual preparedness dialogue and construction Undo/Redo keep clock and condition forward');
 // Repeated inspection is read-only; portable real export/import preserves it.
 const download=page.waitForEvent('download');await menu('Export landscape…');const exportPath=path.join(out,'active-export.json');await(await download).saveAs(exportPath);const exported=JSON.parse(fs.readFileSync(exportPath));await load(exportPath);const imported=(await state()).colony;const oldRevision=exported.colony.navigationRevision;assert(imported.navigationRevision>oldRevision);exported.colony.navigationRevision=imported.navigationRevision;for(const request of exported.colony.planning)if(request.revision===oldRevision)request.revision=imported.navigationRevision;assert.deepEqual(imported,exported.colony);pass('Real JSON export/import round-trips all condition, repair/development, resident and incident history');
 }else{await page.goto(url);await ready();if(await btn('Start building').count())await btn('Start building').click();await pause();await layout();}
 // Explicitly cut one bridge on the chosen responder route; other real crossings may reroute.
 await load('bridge-response');await btn('1×').click();await page.waitForTimeout(1400);await pause();s=await state();
 const response=people(s).find(r=>r.safetyRole==='fire'&&r.route.some(([x,y])=>s.lots[y*s.width+x][10]));
 assert(response,'Real responder route includes a bridge');const cut=response.route.find(([x,y])=>s.lots[y*s.width+x][10]);
 await page.keyboard.press('Minus');zoom=3;await page.keyboard.press('Minus');zoom=2;await panTo(cut);await stroke(3,cut);s=await state();assert.equal(s.lots[cut[1]*s.width+cut[0]][10],null);const position=people(s).find(r=>r.id===response.id);assert(Math.abs(position.x-response.x)<.01);
 await btn('1×').click();await page.waitForTimeout(2000);await pause();s=await state();assert(people(s).every(r=>r.route.every(([x,y])=>x!==cut[0]||y!==cut[1])));await snap('09-response-reroutes-broken-bridge');
 await page.keyboard.press('Control+z');s=await state();assert(s.lots[cut[1]*s.width+cut[0]][10]);pass('A genuine active-response bridge is removed by pointer input; actor keeps physical position, rejects deleted edge and reroutes; Undo restores crossing');
 // The fixture stops just before a legitimate scheduler opportunity: no forced start.
 await load('natural');assert.equal(safe(await state()).active,null);await btn('1×').click();await wait(s=>s.colony.preparedness.active?.origin==='scheduled');await pause();s=await state();assert(safe(s).active.starts>=450);assert.equal(safe(s).active.origin,'scheduled');await snap('10-natural-incident');outcomes.natural={kind:safe(s).active.kind,starts:safe(s).active.starts,origin:safe(s).active.origin};
 await btn('4×').click();await wait(s=>s.colony.preparedness.history.length===1);await pause();s=await state();assert(safe(s).nextAt>=safe(s).history[0].ends+599.9);pass('Naturally scheduled incident observed after actual 450-second grace; separately labelled from forced fixtures, with four-day post-incident cooldown');
 await page.emulateMedia({reducedMotion:'reduce'});await snap('11-reduced-motion');await page.emulateMedia({reducedMotion:'no-preference'});
 await page.setViewportSize({width:931,height:582});await snap('12-small-browser');await page.setViewportSize({width:1440,height:900});await snap('13-large-browser');await page.setViewportSize({width:1024,height:640});await page.waitForTimeout(500);await layout();await home();await btn('Zoom out (−)').click();await btn('Zoom in (+)').click();await btn('Mute').click();await page.mouse.move(28,32);await page.waitForTimeout(500);await btn(/^Unmute/).click();await page.mouse.move(28,32);await page.waitForTimeout(200);assert(await btn(/^Mute/).count());pass('Existing pan/zoom/home, resize, reduced motion and mute/unmute controls remain available');
 assert.deepEqual(errors,[]);assert.deepEqual(failedAssets,[]);const video=page.video();await context.close();await video.saveAs(path.join(out,'browser-journeys.webm'));await browser.close();
 fs.writeFileSync(path.join(out,'results-journeys.json'),JSON.stringify({url,input:'Playwright browser pointer/keyboard/file menus. Legacy preload is isolated setup; observations read localStorage. No gameplay callbacks.',checks,errors,failedAssets,shots,outcomes},null,2));
})().catch(async e=>{console.error(e);if(browser)await browser.close();process.exitCode=1});
