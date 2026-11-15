// Real browser input and read-only checkpoints; no calls into Dart/game internals.
const {chromium}=require('playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const out=path.resolve(process.argv[2]||'/tmp/roag-tranche04-browser');fs.mkdirSync(out,{recursive:true});
const url=process.env.SETTLEMENT_URL||'http://127.0.0.1:8767/settlement/';
const key='roag.settlement.landscape.v1',steps=[],errors=[];
let browser,page;
(async()=>{
 browser=await chromium.launch({executablePath:process.env.CHROMIUM_BIN||'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
 async function open(record=false,reduced=false){
  const context=await browser.newContext({viewport:{width:1024,height:640},reducedMotion:reduced?'reduce':'no-preference',...(record?{recordVideo:{dir:out,size:{width:1024,height:640}}}:{})});
  page=await context.newPage();page.on('pageerror',e=>errors.push(e.message));
  await page.goto(url);await page.locator('#loading').waitFor({state:'detached'});await page.locator('flt-semantics-placeholder').press('Enter');await page.getByRole('button',{name:/^1 Cottage/}).waitFor();return context;
 }
 const btn=name=>page.getByRole('button',{name,exact:typeof name==='string'});
 const read=()=>page.evaluate(k=>JSON.parse(localStorage.getItem(k)),key);
 const speed=n=>btn(n===0?'Ⅱ':`${n}×`).click();
 async function menu(name){await btn('Landscape menu').click();await page.getByRole('menuitem',{name,exact:true}).click();}
 async function importFile(file){const chooser=page.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles(file);await page.waitForTimeout(200);}
 async function snap(name){await page.mouse.move(26,32);await page.waitForTimeout(80);await page.screenshot({path:path.join(out,name+'.png')});}
 async function waitFor(fn,timeout=35000){await page.waitForFunction(({key,source})=>Function('s',`return (${source})(s)`)(JSON.parse(localStorage.getItem(key))),{key,source:fn.toString()},{timeout});}
 let width=1024,top,bottom,center,zoom=4,ox=0,oy=0;
 async function layout(){const a=await btn(/^1 Cottage/).boundingBox(),m=await btn('Landscape menu').boundingBox();width=await page.evaluate(()=>innerWidth);top=m.y+m.height+12;bottom=a.y-14;center=Math.max(9,(width/2-24)/48);}
 const pt=(x,y)=>[width/2+(x+.5-center)*12*zoom+ox,(top+bottom)/2+(y-15)*12*zoom+oy];
 async function home(){await btn('Return to lane (H)').click();zoom=4;ox=oy=0;await layout();}
 async function stroke(t,cells,hold=false){await page.keyboard.press(String(t));await page.mouse.move(...pt(...cells[0]));await page.mouse.down();for(const cell of cells.slice(1))await page.mouse.move(...pt(...cell),{steps:6});await page.waitForTimeout(60);if(!hold){await page.mouse.up();await page.waitForTimeout(70);}}
 const pass=s=>{steps.push(s);console.log('PASS',s);};
 let context=await open(true);await speed(0);await layout();
 await stroke(2,[[7,15],[12,15]]);await stroke(1,[[2,14],[5,14]]);
 const before=await read();assert.equal(before.colony.residents.length,4);assert.equal(new Set(before.colony.residents.map(r=>r.bodyId)).size,4);
 await page.keyboard.press('g');await page.mouse.move(25,35);await page.waitForTimeout(100);await snap('01-farm-tool');
 assert(await page.getByLabel('2 work positions · earn coins while working.',{exact:true}).isVisible());
 await page.mouse.click(...pt(8,14));await page.keyboard.press('v');await snap('02-park-tool');
 assert(await page.getByLabel('6 visitor spaces · social visits improve mood; evening gatherings.',{exact:true}).isVisible());
 await page.mouse.click(...pt(10,14));let s=await read();assert.equal(s.colony.money,before.colony.money-14000);assert(s.lots.some(l=>l[6]==='garden'));assert(s.lots.some(l=>l[6]==='green'));
 pass('Fresh paid Farm/Park, aliases G/V, 4 durable distinct bodies, unchanged costs');
 await speed(1);const samples=[];
 for(let i=0;i<34;i++){await page.waitForTimeout(1000);s=await read();samples.push({clock:s.colony.clock,earned:s.colony.workEarned,people:s.colony.residents.map(r=>({id:r.id,body:r.bodyId,palette:r.palette,activity:r.activity,x:r.x,y:r.y,worked:r.workedSeconds,social:r.lastSocial,workIssue:r.workIssue}))});if([5,11,20,30].includes(i))await snap('03-small-'+i);}
 assert(samples.some(s=>s.people.some(r=>r.activity==='working')));assert(samples.some(s=>s.people.some(r=>r.activity==='socialising')));assert(samples.at(-1).earned>0);
 pass('34 seconds of continuous 1× play: actual walking, working, visits and need/event speech');
 await speed(0);s=await read();const appearance=Object.fromEntries(s.colony.residents.map(r=>[r.id,[r.bodyId,r.palette]]));
 await btn('Inspect (I)').click();await page.mouse.click(...pt(8,14));await page.waitForTimeout(140);await snap('04-farm-inspection');
 assert(await page.getByLabel(/Farm · 2 work positions/).isVisible());const inspectBefore=await read();await page.mouse.click(...pt(10,14));await snap('05-park-inspection');assert(await page.getByLabel(/Park · 6 visitor spaces/).isVisible());assert.deepEqual(await read(),inspectBefore);await page.keyboard.press('Escape');
 await stroke(1,[[6,14]]);await page.keyboard.press('Control+z');s=await read();for(const r of s.colony.residents.filter(r=>r.active))assert.deepEqual([r.bodyId,r.palette],appearance[r.id]);
 const old=JSON.stringify(s.lots);await stroke(4,[[11,14]],true);await snap('06-held-preview');await page.keyboard.press('Escape');await page.mouse.up();assert.equal(JSON.stringify((await read()).lots),old);
 await page.mouse.move(650,420);await page.mouse.down({button:'right'});await page.mouse.move(578,384,{steps:6});await page.mouse.up({button:'right'});ox=-72;oy=-36;await btn('Zoom out (−)').click();zoom=3;await snap('07-pan-zoom');await home();
 const download=page.waitForEvent('download');await menu('Export landscape…');const file=path.join(out,'small-export.json');await(await download).saveAs(file);await page.reload();await page.locator('#loading').waitFor({state:'detached'});await page.locator('flt-semantics-placeholder').press('Enter');await btn(/^1 Cottage/).waitFor();await importFile(file);s=await read();for(const r of s.colony.residents.filter(r=>r.active))assert.deepEqual([r.bodyId,r.palette],appearance[r.id]);
 pass('Read-only Farm/Park inspection, paid Undo, held preview cancellation, pan/zoom, reload and export/import preserve appearance');
 fs.writeFileSync(path.join(out,'small-samples.json'),JSON.stringify(samples,null,2));const smallVideo=await page.video().path();await context.close();fs.renameSync(smallVideo,path.join(out,'small-normal-speed.webm'));

 context=await open(true);await importFile('/tmp/roag-tranche04-fixtures/residents-85-active.json');await layout();await btn('Zoom out (−)').click();zoom=3;
 // Pan to the actual Farms/Parks on the southern road through normal input.
 await page.mouse.move(620,400);await page.mouse.down({button:'right'});await page.mouse.move(620,280,{steps:8});await page.mouse.up({button:'right'});oy=-120;
 const large=[];for(let i=0;i<26;i++){await page.waitForTimeout(1000);s=await read();large.push({clock:s.colony.clock,work:s.colony.workEarned,activities:s.colony.residents.reduce((a,r)=>(a[r.activity]=(a[r.activity]||0)+1,a),{})});if([1,8,16,24].includes(i))await snap('08-crowd-'+i);}
 s=await read();assert.equal(s.colony.residents.filter(r=>r.active).length,85);assert(large.at(-1).work>large[0].work);assert(s.colony.residents.some(r=>r.workIssue==='workFull'));assert(s.colony.residents.some(r=>r.lastSocial>=0));
 await speed(0);await page.setViewportSize({width:1440,height:900});await home();await snap('09-large-desktop');await page.setViewportSize({width:1024,height:640});await home();
 await btn('Inspect (I)').click();await page.mouse.click(...pt(2,20));await snap('10-crowded-inspection');await page.keyboard.press('Escape');
 pass('85-resident normal-speed town: genuine full-staff need alongside actual work/social activity, crowded offsets, 1024×640 and 1440×900');
 fs.writeFileSync(path.join(out,'crowd-samples.json'),JSON.stringify(large,null,2));const crowdVideo=await page.video().path();await context.close();fs.renameSync(crowdVideo,path.join(out,'crowd-normal-speed.webm'));

 context=await open(false,true);await importFile(path.resolve('../../docs/world-builder/evidence/tranche-03/colony-gathering.json'));await page.waitForTimeout(26000);await snap('11-rest-reduced-motion');s=await read();assert(s.colony.residents.some(r=>r.activity==='resting'));
 await speed(0);await menu('Controls & about');
 await snap('12-help');await page.getByRole('button',{name:'Back to the landscape',exact:true}).click();
 pass('Reduced-motion browser preference, night rest and Farm/Park help');await context.close();
 assert.deepEqual(errors,[]);fs.writeFileSync(path.join(out,'browser-results.json'),JSON.stringify({seed:202,input:'Playwright mouse/keyboard and actual file chooser; read-only localStorage samples; no game callbacks',steps,errors},null,2));
 await browser.close();
})().catch(async e=>{console.error(e);if(page)await page.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});if(browser)await browser.close();process.exitCode=1;});
