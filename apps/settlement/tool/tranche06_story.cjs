// Focused real-UI recording with reading time; no game callbacks.
const {chromium}=require('playwright'),fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const out=path.resolve(process.argv[2]||'/tmp/roag-tranche06-story');fs.mkdirSync(out,{recursive:true});
const url=process.env.SETTLEMENT_URL||'http://127.0.0.1:8768/settlement/',key='roag.settlement.landscape.v1';
const fixtures='/tmp/roag-tranche06-fixtures',meta=JSON.parse(fs.readFileSync(path.join(fixtures,'metadata.json')));
let browser,page;const checks=[],errors=[],timeline=[];let videoStarted;
(async()=>{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
 const context=await browser.newContext({viewport:{width:1024,height:640},recordVideo:{dir:out,size:{width:1024,height:640}}});
 page=await context.newPage();videoStarted=Date.now();page.on('pageerror',e=>errors.push(e.message));
 const btn=n=>page.getByRole('button',{name:n,exact:typeof n==='string'}),read=()=>page.evaluate(k=>JSON.parse(localStorage.getItem(k)),key);
 async function ready(){await page.locator('#loading').waitFor({state:'detached'});const p=page.locator('flt-semantics-placeholder');if(await p.count())await p.press('Enter');await btn(/^1 Cottage/).waitFor();}
 async function menu(n){await btn('Landscape menu').click();await page.getByRole('menuitem',{name:n,exact:true}).click();}
 async function importFile(f){const chooser=page.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles(f);await page.waitForTimeout(250);const imported=await read(),expected=JSON.parse(fs.readFileSync(f));assert.deepEqual(imported.lots,expected.lots,'file import succeeded');zoom=4;ox=oy=0;await layout();}
 const speed=n=>btn(n===0?'Ⅱ':`${n}×`).click();
 async function wait(fn,timeout=90000){await page.waitForFunction(({key,fn})=>Function('s',`return (${fn})(s)`)(JSON.parse(localStorage.getItem(key))),{key,fn:fn.toString()},{timeout});}
 const labels=()=>page.locator('flt-semantics').evaluateAll(es=>es.map(e=>e.getAttribute('aria-label')||e.textContent).join('\n'));
 let width=1024,top,bottom,center,zoom=4,ox=0,oy=0;
 async function layout(){const t=await btn(/^1 Cottage/).boundingBox(),m=await btn('Landscape menu').boundingBox();width=await page.evaluate(()=>innerWidth);top=m.y+m.height+12;bottom=t.y-14;center=Math.max(9,(width/2-24)/48);}
 const pt=(x,y)=>[width/2+(x+.5-center)*12*zoom+ox,(top+bottom)/2+(y-15)*12*zoom+oy];
 async function pan(dx,dy){await page.mouse.move(650,430);await page.mouse.down({button:'right'});await page.mouse.move(650+dx,430+dy,{steps:10});await page.mouse.up({button:'right'});ox+=dx;oy+=dy;}
 async function stroke(t,cells){await page.keyboard.press(String(t));await page.mouse.move(...pt(...cells[0]));await page.mouse.down();for(const cell of cells.slice(1))await page.mouse.move(...pt(...cell),{steps:8});await page.mouse.up();await page.waitForTimeout(100);}
 async function snap(n,keepPointer=false){timeline.push({name:n,seconds:(Date.now()-videoStarted)/1000});if(!keepPointer)await page.mouse.move(28,32);await page.waitForTimeout(200);await page.screenshot({path:path.join(out,n+'.png')});}
 async function selectPerson(id){const s=await read(),r=s.colony.residents.find(r=>r.id===id);assert(r);await btn('Inspect (I)').click();for(let i=0;i<8;i++){const p=pt(...r.cell);p[1]-=4*zoom;await page.mouse.click(...p);await page.waitForTimeout(100);if((await labels()).includes(r.name+' ·'))return;}throw Error('Could not select '+r.name);}
 const buildingCell=(s,id)=>{const i=s.lots.findIndex(l=>l[5]===id||l[7]===id);assert(i>=0);return[i%s.width,Math.floor(i/s.width)];};
 const resident=(s,id)=>s.colony.residents.find(r=>r.id===id);
 const pass=s=>{checks.push(s);console.log('PASS',s);};
 await page.goto(url);await ready();await importFile(path.join(fixtures,'need-work.json'));
 await selectPerson(meta.requestResident);await btn('Talk').click();await btn('Is there anything you need?').click();await snap('need-expressed');await page.waitForTimeout(3000);
 await btn("I'll see what I can do.").click();await snap('promise-accepted');await page.waitForTimeout(1800);await btn('Back to building').click();
 await stroke(4,[[6,14],[7,14]]);let s=await read();assert.equal(s.colony.requests.records[0].state,'accepted');await snap('jobs-built');await page.waitForTimeout(900);
 await speed(1);await wait(s=>s.colony.requests?.records[0]?.state==='resolved');await page.waitForTimeout(1800);await snap('actual-work');await speed(0);
 await selectPerson(meta.requestResident);await btn('Talk').click();assert((await labels()).includes('actually been back at work'));await snap('resident-responds');await page.waitForTimeout(3500);
 s=await read();assert(resident(s,meta.requestResident).workedSeconds>0);assert.equal(s.colony.requests.records[0].state,'resolved');assert.deepEqual(errors,[]);
 const video=await page.video().path();await context.close();fs.renameSync(video,path.join(out,'gameplay.webm'));await browser.close();
 fs.writeFileSync(path.join(out,'story-results.json'),JSON.stringify({seed:202,timeline,errors,request:s.colony.requests.records[0],resident:resident(s,meta.requestResident),input:'Playwright mouse/keyboard, actual fixture import; no game callbacks or forced job/reward',fixture:'Four-person genuine-need town; dialogue/construction paused explicitly, actual work at 1×; reading holds belong only to this recording script'},null,2));console.log('PASS real request, explicit choice, paid jobs, actual work and truthful acknowledgment');
})().catch(async e=>{console.error(e);if(browser)await browser.close();process.exitCode=1;});
