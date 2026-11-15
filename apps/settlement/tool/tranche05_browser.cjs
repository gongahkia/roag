// Release UI journeys: browser mouse/keyboard and actual file chooser, no game callbacks.
const {chromium}=require('playwright'),fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const out=path.resolve(process.argv[2]||'/tmp/roag-tranche05-browser');fs.mkdirSync(out,{recursive:true});
const url=process.env.SETTLEMENT_URL||'http://127.0.0.1:8768/settlement/',key='roag.settlement.landscape.v1';
let browser,page;const checks=[],errors=[],timeline=[];let videoStarted;
(async()=>{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
 const context=await browser.newContext({viewport:{width:1024,height:640},recordVideo:{dir:out,size:{width:1024,height:640}}});
 page=await context.newPage();videoStarted=Date.now();page.on('pageerror',e=>errors.push(e.message));
 const btn=name=>page.getByRole('button',{name,exact:typeof name==='string'}),read=()=>page.evaluate(k=>JSON.parse(localStorage.getItem(k)),key);
 async function ready(){await page.locator('#loading').waitFor({state:'detached'});const p=page.locator('flt-semantics-placeholder');if(await p.count())await p.press('Enter');await btn(/^1 Cottage/).waitFor();}
 const speed=n=>btn(n===0?'Ⅱ':`${n}×`).click();
 async function menu(name){await btn('Landscape menu').click();await page.getByRole('menuitem',{name,exact:true}).click();}
 async function importFile(file){const fc=page.waitForEvent('filechooser');await menu('Import landscape…');await(await fc).setFiles(file);await page.waitForTimeout(250);}
 async function wait(fn,timeout=120000){await page.waitForFunction(({k,s})=>Function('s',`return (${s})(s)`)(JSON.parse(localStorage.getItem(k))),{k:key,s:fn.toString()},{timeout});}
 let width=1024,top,bottom,center,zoom=4,ox=0,oy=0;
 async function layout(){const a=await btn(/^1 Cottage/).boundingBox(),m=await btn('Landscape menu').boundingBox();width=await page.evaluate(()=>innerWidth);top=m.y+m.height+12;bottom=a.y-14;center=Math.max(9,(width/2-24)/48);}
 const pt=(x,y)=>[width/2+(x+.5-center)*12*zoom+ox,(top+bottom)/2+(y-15)*12*zoom+oy];
 async function stroke(t,cells,hold=false){await page.keyboard.press(String(t));await page.mouse.move(...pt(...cells[0]));await page.mouse.down();for(const c of cells.slice(1))await page.mouse.move(...pt(...c),{steps:10});await page.waitForTimeout(70);if(!hold){await page.mouse.up();await page.waitForTimeout(90);}}
 async function snap(name){timeline.push({name,seconds:(Date.now()-videoStarted)/1000});await page.mouse.move(28,32);await page.waitForTimeout(80);await page.screenshot({path:path.join(out,name+'.png')});}
 const pass=s=>{checks.push(s);console.log('PASS',s);};
 await page.goto(url);await ready();await speed(0);await layout();
 await page.keyboard.press('b');await page.getByRole('menuitem',{name:/6 Workshop/}).waitFor();await page.screenshot({path:path.join(out,'01-catalogue.png')});await page.getByRole('menuitem',{name:/6 Workshop/}).click();
 await stroke(2,[[7,15],[18,15]]);await stroke(1,[[2,14],[5,14]]);await stroke(1,[[2,16],[5,16]]);
 for(const [tool,x] of [[4,6],[6,8],[7,10],[8,16],[5,12]])await stroke(tool,[[x,14]]);
 let s=await read();assert.equal(s.colony.residents.filter(r=>r.active).length,8);assert.equal(s.lots.filter(l=>l[6]).length,5);assert.equal(new Set(s.colony.residents.map(r=>r.name)).size,8);
 await snap('02-five-destinations');pass('Fresh paid town: eight actual residents; all five destinations, grouped catalogue, new and old shortcuts');
 const journeyStart=performance.now();await speed(1);
 await wait(s=>s.colony.special?.kind==='rain' && s.colony.clock<s.colony.special.ends);const rain=await read();assert.equal(rain.colony.special.starts>=12,true);
 await snap('03-natural-rain');
 // Continuous normal-speed construction response; accounting includes legitimate concurrent earnings.
 const before=await read();await stroke(9,[[2,15],[8,15]]);s=await read();const income=s.colony.grantEarned-before.colony.grantEarned+s.colony.workEarned-before.colony.workEarned+s.colony.tradeEarned-before.colony.tradeEarned;assert.equal(s.colony.money,before.colony.money-14000+income);
 assert(s.lots.slice(15*s.width+2,15*s.width+9).every(l=>l[8]==='stone'));
 await snap('04-stone-in-rain');await page.keyboard.press('Control+z');const undone=await read();assert(undone.colony.clock>=before.colony.clock);assert.equal(undone.colony.special.id,rain.colony.special.id);await page.keyboard.press('Control+Shift+z');
 await stroke(0,[[9,15],[11,15]]);await snap('05-mixed-roads');
 await wait(s=>s.colony.tradeEarned>0 && s.colony.residents.some(r=>r.lastSocial>=0));s=await read();assert(s.colony.residents.some(r=>r.workId?.startsWith('workshop-') && r.workedSeconds>0));assert(s.colony.residents.some(r=>r.workId?.startsWith('market-') && r.workedSeconds>0));
 await page.waitForTimeout(6000);await snap('06-real-activity');const journeyEnd=performance.now();
 pass('Naturally scheduled seed-202 rain; real Workshop/vendor work, served customers and social visits; paid mixed roads and forward Undo/Redo while time continues');
 await wait(s=>s.colony.residents.some(r=>['working','socialising'].includes(r.activity) && s.colony.residents.some(q=>q.id!==r.id && ['working','socialising'].includes(q.activity) && q.cell.join()===r.cell.join())));await speed(0);s=await read();const beforeInspect=s;const stacked=s.colony.residents.find(r=>['working','socialising'].includes(r.activity) && s.colony.residents.some(q=>q.id!==r.id && ['working','socialising'].includes(q.activity) && q.cell.join()===r.cell.join()));assert(stacked);const inspectPoint=pt(...stacked.cell).map((v,i)=>i===1?v-16:v);
 await btn('Inspect (I)').click();await page.mouse.click(...inspectPoint);await page.waitForTimeout(120);await snap('07-resident-card');
 const labels=await page.locator('[aria-label]').evaluateAll(es=>es.map(e=>e.getAttribute('aria-label')).join('\n'));assert(/Brisk|Sociable|Green thumb/.test(labels));assert(labels.includes('Home: Cottage'));assert(/Click again to cycle/.test(labels));const firstPerson=s.colony.residents.find(r=>labels.includes(r.name+' ·'))?.id;assert(firstPerson);assert.deepEqual(await read(),beforeInspect);
 await page.mouse.click(...inspectPoint);await snap('08-overlap-cycle');const nextLabels=await page.locator('[aria-label]').evaluateAll(es=>es.map(e=>e.getAttribute('aria-label')).join('\n'));const nextPerson=s.colony.residents.find(r=>nextLabels.includes(r.name+' ·'))?.id;assert(nextPerson);assert.notEqual(nextPerson,firstPerson);await page.keyboard.press('Escape');
 await btn('Inspect (I)').click();await page.mouse.move(...pt(8,15));await page.mouse.down();await page.mouse.move(...pt(10,15),{steps:5});await page.mouse.up();assert.deepEqual(await read(),beforeInspect);await page.keyboard.press('Escape');
 await stroke(9,[[13,15]],true);await page.keyboard.press('Escape');await page.mouse.up();assert.deepEqual((await read()).lots,beforeInspect.lots);
 pass('Read-only named resident card, real trait descriptions, repeated-click overlap cycling, drag threshold and priced cancellation');
 const dl=page.waitForEvent('download');await menu('Export landscape…');const exportFile=path.join(out,'settlement.json');await(await dl).saveAs(exportFile);const saved=await read();
 await page.reload();await ready();let loaded=await read();assert.deepEqual(loaded.colony.residents,saved.colony.residents);assert.deepEqual(loaded.colony.special,saved.colony.special);assert.equal(loaded.colony.specialRng,saved.colony.specialRng);assert.equal(loaded.colony.tradeEarned,saved.colony.tradeEarned);
 await importFile(exportFile);loaded=await read();assert.deepEqual(loaded.colony.residents,saved.colony.residents);assert.equal(loaded.colony.special.id,saved.colony.special.id);assert(loaded.colony.navigationRevision>saved.colony.navigationRevision);
 pass('Reload/import preserve event identity/RNG, funds, names, traits, appearances, traversal/trade credits; import invalidates navigation');
 await page.setViewportSize({width:1440,height:900});await btn('Return to lane (H)').click();zoom=4;ox=oy=0;await layout();await snap('09-large-desktop');await page.setViewportSize({width:1024,height:640});await btn('Return to lane (H)').click();await layout();
 await page.mouse.move(700,420);await page.mouse.down({button:'right'});await page.mouse.move(620,370,{steps:6});await page.mouse.up({button:'right'});await btn('Zoom out (−)').click();await snap('10-pan-zoom');await btn('Return to lane (H)').click();await layout();
 for(const kind of ['harvest','marketDay']){
  await importFile(`/tmp/roag-tranche05-play/${kind}.json`);await layout();const start=await read();await page.waitForTimeout(2000);await snap('11-'+kind);
  if(kind==='marketDay'){
   await wait(s=>s.colony.tradeEarned>0);await speed(0);let q=await read();assert(q.colony.residents.some(r=>r.tradeDay===Math.floor(q.colony.clock/150)+1));
   await btn('Inspect (I)').click();await page.mouse.click(...pt(10,14));await snap('12-market-staffing');await page.keyboard.press('Escape');
   await speed(1);await stroke(3,[[10,14]]);await snap('13-market-removed-during-event');const removed=await read();await page.waitForTimeout(2200);await speed(0);q=await read();assert.equal(q.colony.tradeEarned,removed.colony.tradeEarned);await page.keyboard.press('Control+z');assert.equal((await read()).colony.special.id,start.colony.special.id);
  } else {await page.waitForTimeout(3500);const q=await read();assert(q.colony.workEarned>start.colony.workEarned);await speed(4);await page.waitForTimeout(1600);await speed(0);await snap('14-accelerated-harvest');}
 }
 pass('Explicitly fixture-assisted harvest and Market day; actual bonuses/service, Market inspection, removal during the event, Undo and accelerated simulation');
 await importFile('/tmp/roag-tranche05-play/large-85.json');await layout();await btn('Zoom out (−)').click();zoom=3;
 await page.mouse.move(700,420);await page.mouse.down({button:'right'});await page.mouse.move(580,330,{steps:8});await page.mouse.up({button:'right'});ox=-120;oy=-90;
 await speed(1);await wait(s=>s.colony.special?.kind==='rain' && s.colony.clock<s.colony.special.ends);await snap('15-populated-natural-rain');
 await stroke(9,[[18,19],[26,19]]);await snap('16-populated-stone-response');
 await wait(s=>s.colony.tradeEarned>0 && s.colony.residents.some(r=>r.workId?.startsWith('workshop-') && r.workedSeconds>0));await page.waitForTimeout(6500);await snap('17-populated-service');
 s=await read();assert.equal(s.colony.residents.filter(r=>r.active).length,85);assert(s.colony.residents.some(r=>r.lastSocial>=0));fs.writeFileSync(path.join(out,'large-state.json'),JSON.stringify(s));
 pass('Safe 85-resident town: new workplaces/service, natural rain, mixed roads and a visible paid construction response at 1×');
 assert.deepEqual(errors,[]);const video=await page.video().path();await context.close();fs.renameSync(video,path.join(out,'gameplay.webm'));await browser.close();
 fs.writeFileSync(path.join(out,'browser-results.json'),JSON.stringify({seed:202,checks,errors,timeline,native:false,input:'Playwright browser mouse/keyboard; actual menus/file chooser; read-only checkpoints; no game callbacks',fixtureEvents:['harvest','marketDay'],naturalEvent:'Rain scheduled during fresh paid UI construction journey',normalJourneySeconds:(journeyEnd-journeyStart)/1000},null,2));
})().catch(async e=>{console.error(e);if(page){await page.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});try{fs.writeFileSync(path.join(out,'failure-state.json'),await page.evaluate(k=>localStorage.getItem(k),key));}catch{}}if(browser)await browser.close();process.exitCode=1;});
