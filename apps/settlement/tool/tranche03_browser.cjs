// Normal browser input; storage reads observe checkpoints, never call game code.
const {chromium}=require('playwright');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const out=path.resolve(process.argv[2]||'/tmp/settlement-colony-evidence');
fs.mkdirSync(out,{recursive:true});
let browser,page;
(async()=>{
 browser=await chromium.launch({executablePath:process.env.CHROMIUM_BIN||'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
 const context=await browser.newContext({viewport:{width:1024,height:640},recordVideo:{dir:out,size:{width:1024,height:640}}});
 page=await context.newPage();const errors=[],failed=[],steps=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('response',r=>{if(r.status()>=400)failed.push([r.status(),r.url()]);});
 const began=performance.now(),key='roag.settlement.landscape.v1';
 const read=()=>page.evaluate(k=>JSON.parse(localStorage.getItem(k)),key);
 const physical=s=>JSON.stringify(s.lots);
 const button=name=>page.getByRole('button',{name,exact:typeof name==='string'});
 const speed=n=>button(n===0?'Ⅱ':`${n}×`).click();
 async function ready(){await page.locator('#loading').waitFor({state:'detached'});await page.locator('flt-semantics-placeholder').press('Enter');await button(/^1 Cottage/).waitFor();}
 await page.goto(process.env.SETTLEMENT_URL||'http://127.0.0.1:8767/settlement/');await ready();await speed(0);
 let zoom=4,ox=0,oy=0,width=1024,top,bottom,center;
 async function layout(){const a=await button(/^1 Cottage/).boundingBox(),m=await button('Landscape menu').boundingBox();width=await page.evaluate(()=>innerWidth);top=m.y+m.height+12;bottom=a.y-14;center=Math.max(9,(width/2-24)/48);}
 await layout();
 const pt=(x,y)=>[width/2+(x+.5-center)*12*zoom+ox,(top+bottom)/2+(y-15)*12*zoom+oy];
 async function home(){await button('Return to lane (H)').click();zoom=4;ox=oy=0;await layout();}
 async function stroke(tool,cells,{hold=false}={}){await page.keyboard.press(String(tool));await page.mouse.move(...pt(...cells[0]));await page.mouse.down();for(const c of cells.slice(1))await page.mouse.move(...pt(...c),{steps:8});await page.waitForTimeout(80);if(!hold){await page.mouse.up();await page.waitForTimeout(80);}}
 async function snap(name){await page.mouse.move(25,35);await page.waitForTimeout(120);await page.screenshot({path:path.join(out,name+'.png')});}
 async function waitFor(predicate,timeout=30000){await page.waitForFunction(({key,source})=>Function('s',`return (${source})(s)`)(JSON.parse(localStorage.getItem(key))),{key,source:predicate.toString()},{timeout});}
 async function menu(label){await button('Landscape menu').click();await page.getByRole('menuitem',{name:label,exact:true}).click();}
 const step=name=>{steps.push(name);console.log('PASS',name);};

 const opening=await read();await stroke(2,[[7,15],[12,15]]);await stroke(1,[[2,14],[5,14]]);
 let s=await read();assert.equal(s.colony.money,opening.colony.money-20000);assert.equal(s.colony.residents.length,4);
 await snap('01-paid-homes');await speed(1);await page.waitForTimeout(5200);
 await snap('02-work-needed');await speed(0);s=await read();assert(s.colony.residents.some(r=>r.workIssue==='noWork'));
 step('Paid home stroke, visible arrivals, and a truthful work need at 1×');
 await stroke(4,[[8,14]]);await snap('03-garden-built');await speed(1);
 await waitFor(s=>s.colony.workEarned>0);await snap('04-working-income');
 const income=await read();assert(income.colony.residents.some(r=>r.workedSeconds>0));assert(income.colony.workEarned>0);
 await waitFor(s=>s.colony.residents.some(r=>r.workId && r.lastWork>=0 && r.leisureIssue==='noLeisure'));
 await speed(0);await page.waitForTimeout(4500);await snap('05-leisure-needed');await stroke(5,[[10,14]]);await snap('06-green-built');await speed(4);
 await waitFor(s=>s.colony.residents.some(r=>r.lastSocial>=0));await snap('07-social-visit');
 step('A garden earns through real work; a green addresses leisure and produces real social experience');
 await waitFor(s=>s.colony.clock>=86);await speed(0);s=await read();
 // Pause through visible controls to inspect a short event without a game callback.
 for(let i=0;i<10 && (!s.colony.event || s.colony.event.state!=='active');i++){
  await speed(4);await page.waitForTimeout(700);await speed(0);s=await read();
 }
 assert.equal(s.colony.event?.state,'active');assert(s.colony.event.awarded.length>=3);
 for(const id of s.colony.event.awarded){const r=s.colony.residents.find(r=>r.id===id);assert.equal(r.activity,'gathering');assert.equal(r.awardedEvent,s.colony.event.id);}
 await snap('08-evening-gathering');await page.waitForTimeout(2200);const demoEndSeconds=(performance.now()-began)/1000;
 step('Ordinary seed-202 conditions reach an evening gathering with actual awarded attendees');
 const eventId=s.colony.event.id,awarded=[...s.colony.event.awarded];
 await stroke(3,[[9,15]]);s=await read();assert.equal(s.colony.event.state,'ended');
 await page.keyboard.press('Control+z');s=await read();assert.equal(s.colony.event.id,eventId);assert.deepEqual(s.colony.event.awarded,awarded);
 await speed(4);await page.waitForTimeout(1800);await speed(0);await snap('09-cut-and-restored');
 step('Cut/restored access during the event ends it gracefully without replaying rewards');

 // Use normal speed controls to reach the next useful work period.
 await speed(4);await waitFor(s=>s.colony.clock>150 && s.colony.residents.some(r=>r.activity==='working'),45000);await speed(0);
 await stroke(1,[[6,14],[7,14]]);const paid=await read();await speed(4);await page.waitForTimeout(1500);await speed(0);
 const earnedBeforeUndo=await read();assert(earnedBeforeUndo.colony.workEarned>paid.colony.workEarned);
 await page.keyboard.press('Control+z');s=await read();assert.equal(s.colony.money,earnedBeforeUndo.colony.money+10000);assert.equal(s.colony.workEarned,earnedBeforeUndo.colony.workEarned);assert.equal(s.colony.clock,earnedBeforeUndo.colony.clock);
 const unchanged=physical(s),beforeHold=s.colony.money;
 await stroke(4,[[11,14]],{hold:true});await page.keyboard.press('t');await page.keyboard.press('t');await page.keyboard.press('t'); // pause → 1 → 2 → 4, same held stroke
 await page.waitForTimeout(3300);const whileHeld=await read();assert.equal(physical(whileHeld),unchanged);assert(whileHeld.colony.money>beforeHold);
 await page.keyboard.press('Escape');await page.mouse.up();await speed(0);s=await read();assert.equal(physical(s),unchanged);
 step('Undo reverses the exact paid charge after income; cancelling a held priced stroke preserves ongoing earnings and committed checkpoints');

 await stroke(2,[[1,15],[1,17],[19,17]]);
 await stroke(2,[[12,15],[19,15]]);
 await stroke(1,[[2,18],[19,18]]);await stroke(1,[[2,16],[19,16]]);
 await stroke(1,[[6,14],[19,14]]);s=await read();assert(s.colony.money<5000);
 const broke=physical(s),money=s.colony.money;await stroke(4,[[19,14]]);s=await read();assert.equal(physical(s),broke);assert.equal(s.colony.money,money);
 await snap('10-unaffordable');await speed(4);await waitFor(s=>s.colony.money>=8000,45000);await speed(0);await stroke(4,[[19,14]]);s=await read();assert.equal(s.lots[14*s.width+19][6],'garden');
 step('A partial-affordability stroke stops within its budget; ordinary work/grant income recovers enough for a real garden');

 await button('Inspect (I)').click();await page.mouse.click(...pt(8,14));await page.waitForTimeout(200);await snap('11-inspection');await page.keyboard.press('Escape');
 await page.mouse.move(650,420);await page.mouse.down({button:'right'});await page.mouse.move(578,384,{steps:4});await page.mouse.up({button:'right'});ox=-72;oy=-36;
 await button('Zoom out (−)').click();zoom=3;
 await stroke(2,[[17,17],[17,19],[19,19]]);
 await page.setViewportSize({width:1440,height:900});await home();await button('Zoom out (−)').click();zoom=3;
 await stroke(2,[[19,17],[20,17]]);await snap('12-large-desktop');
 step('Read-only inspection, pan, zoom, resize and further brush picking remain usable');
 const download=page.waitForEvent('download');await menu('Export landscape…');const exported=path.join(out,'colony.json');await(await download).saveAs(exported);
 const saved=await read();assert.deepEqual(JSON.parse(fs.readFileSync(exported)),saved);
 await page.reload();await ready();assert.deepEqual(await read(),saved);
 let chooser=page.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles(exported);await page.waitForTimeout(200);assert.deepEqual(await read(),saved);
 const invalid=JSON.parse(JSON.stringify(saved));invalid.colony.money=-1;const bad=path.join(out,'invalid.json');fs.writeFileSync(bad,JSON.stringify(invalid));
 chooser=page.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles(bad);await page.waitForTimeout(200);assert.deepEqual(await read(),saved);
 step('Reload and export/import preserve authoritative money, progress, identities and event rewards; invalid imports leave the current checkpoint intact');
 const old=path.resolve('../../docs/world-builder/evidence/tranche-03/legacy-74.json');
 chooser=page.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles(old);await page.waitForTimeout(150);await speed(0);
 s=await read();assert.equal(s.colony.residents.filter(r=>r.active).length,74);assert.equal(new Set(s.colony.residents.map(r=>r.id)).size,74);assert.equal(s.colony.money,200000+s.colony.grantEarned);
 assert.deepEqual(s.lots.map(l=>l.slice(0,6)),JSON.parse(fs.readFileSync(old)).lots);
 await home();await button('Zoom out (−)').click();zoom=3;await snap('13-migrated-74');
 const largeBuild=[];
 for(let y=19;y<=21;y++)for(let x=1;x<20;x++){
  const lot=s.lots[y*s.width+x];if(lot[3] || lot[4] || lot[6] || lot[1]==='oak')continue;
  if([[x,y+1],[x+1,y],[x-1,y],[x,y-1]].some(([nx,ny])=>nx>=0 && ny>=0 && nx<s.width && ny<s.height && s.lots[ny*s.width+nx][3]))largeBuild.push([x,y]);
 }
 assert(largeBuild.length>=8,'legacy fixture has visible free frontage for productive places');
 for(const c of largeBuild.slice(0,6))await stroke(4,[c]);for(const c of largeBuild.slice(6,8))await stroke(5,[c]);
 await speed(4);await waitFor(s=>s.colony.workEarned>0 && s.colony.residents.some(r=>r.lastSocial>=0),45000);
 s=await read();assert(s.colony.workEarned>s.colony.grantEarned);await snap('13b-large-working');
 const intervals=await page.evaluate(()=>new Promise(resolve=>{let prev,values=[];function tick(t){if(prev)values.push(t-prev);prev=t;if(values.length>=90)resolve(values);else requestAnimationFrame(tick);}requestAnimationFrame(tick);}));
 const beforeOffscreen=(await read()).colony.workEarned;
 await page.mouse.move(600,400);await page.mouse.down({button:'right'});await page.mouse.move(600,800,{steps:5});await page.mouse.up({button:'right'});
 await page.waitForFunction(({key,before})=>JSON.parse(localStorage.getItem(key)).colony.workEarned>before,{key,before:beforeOffscreen});
 await speed(0);await page.setViewportSize({width:1024,height:640});await home();await snap('14-final-1024');
 step('Comparable 74-cottage legacy village migrates intact, adopts garden/social routines and earns through off-screen work at accelerated speed');
 assert.deepEqual(errors,[]);assert.deepEqual(failed,[]);
 const result={seed:202,browser:browser.version(),input:'Playwright browser mouse and keyboard, actual menus/file chooser; no game callbacks or injected main-journey state',steps,demoEndSeconds,finalPopulation:74,frameIntervalsMs:{median:[...intervals].sort((a,b)=>a-b)[45],max:Math.max(...intervals)},errors,failed};
 fs.writeFileSync(path.join(out,'browser-results.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
 await context.close();await browser.close();
})().catch(async e=>{console.error(e);if(page)await page.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});if(browser)await browser.close();process.exitCode=1;});
