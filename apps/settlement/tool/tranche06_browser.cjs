// Actual release UI; only safe file imports and read-only checkpoints assist the journey.
const {chromium}=require('playwright'),fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const out=path.resolve(process.argv[2]||'/tmp/roag-tranche06-browser');fs.mkdirSync(out,{recursive:true});
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
 async function importFile(f){const chooser=page.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles(f);await btn('Replace settlement').click();await page.waitForTimeout(250);const imported=await read(),expected=JSON.parse(fs.readFileSync(f));assert.deepEqual(imported.lots,expected.lots,'file import succeeded');zoom=4;ox=oy=0;await layout();}
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
 await page.goto(url);await ready();await importFile(path.join(fixtures,'populated.json'));
 let s=await read();assert.equal(s.colony.residents.filter(r=>r.active).length,85);
 await page.keyboard.press('b');await snap('01-catalogue-buildings');
 for(const name of ['Cottage','Farm','Park','Workshop','Market','Community Hall','Path','Stone Road','Garden Path'])assert(await page.getByRole('menuitem',{name:new RegExp(name)}).count()>0);
 await page.mouse.move(480,420);await page.mouse.wheel(0,650);await page.waitForTimeout(250);await snap('02-catalogue-roads');await page.keyboard.press('Escape');
 pass('85 actual residents; sprite catalogue contains all nine buildable types with prices/access, selected tool and treasury coin');
 await btn('Zoom out (−)').click();zoom=3;await pan(-160,-70);
 const initial=await read();await page.keyboard.press('i');let p=pt(...meta.cottageSource);p[1]-=10;await page.mouse.click(...p);
 await btn('Move building · Free (M)').click();await page.mouse.move(...pt(...meta.cottageDestination));await snap('03-relocation-preview',true);assert.deepEqual(await read(),initial);
 await page.keyboard.press('Escape');assert.deepEqual(await read(),initial);
 await page.keyboard.press('i');p=pt(...meta.cottageSource);p[1]-=10;await page.mouse.click(...p);await btn('Move building · Free (M)').click();
 // Collision leaves the ghost active and makes no transaction.
 await page.mouse.click(...pt(2,14));assert.deepEqual(await read(),initial);
 await page.mouse.click(...pt(...meta.cottageDestination));s=await read();assert.deepEqual(buildingCell(s,meta.cottage),meta.cottageDestination);assert.equal(s.colony.money,initial.colony.money);
 assert.equal(s.colony.residents.filter(r=>r.active).length,85);assert.equal(s.nextCottageId,initial.nextCottageId);
 const owner=resident(initial,meta.cottage),moved=resident(s,meta.cottage);for(const k of ['name','bodyId','palette','trait','earned','workedSeconds','x','y'])assert.equal(moved[k],owner[k]);
 await snap('04-cottage-moved');await speed(1);
 await wait(s=>s.colony.residents.some(r=>r.id==='home-1'&&r.cell[0]===26&&r.cell[1]===19&&['home','resting'].includes(r.activity)));
 await speed(0);const arrived=await read();assert.equal(resident(arrived,meta.cottage).id,owner.id);await snap('05-same-resident-at-new-home');
 await page.keyboard.press('Control+z');s=await read();assert.deepEqual(s.lots,initial.lots);assert.equal(s.colony.money,arrived.colony.money);assert.equal(s.colony.clock,arrived.colony.clock);
 await page.keyboard.press('Control+Shift+z');s=await read();assert.deepEqual(buildingCell(s,meta.cottage),meta.cottageDestination);assert.equal(s.colony.money,arrived.colony.money);
 pass('Free cottage move: pure ghost/cancellation, invalid collision, durable person/variant/ID, actual return to new home, atomic Undo/Redo with forward money/time');
 await speed(1);await wait(s=>s.colony.residents.some(r=>r.activity==='working'&&r.workProgress<5));await speed(0);s=await read();
 const worker=s.colony.residents.find(r=>r.activity==='working'&&r.workProgress<8);assert(worker);const workId=worker.workId,from=buildingCell(s,workId),positions=s.colony.residents.filter(r=>r.workId===workId).map(r=>[r.id,r.x,r.y,r.earned,r.workedSeconds]);
 await page.mouse.click(...pt(...from),{button:'right'});await snap('06-building-context-menu');await page.getByRole('menuitem',{name:/^Move .* · Free$/}).click();
 await page.mouse.move(...pt(...meta.workDestination));await snap('07-workplace-ghost',true);await page.mouse.click(...pt(...meta.workDestination));
 let changed=await read();assert.deepEqual(buildingCell(changed,workId),meta.workDestination);assert.equal(changed.colony.money,s.colony.money);
 assert.deepEqual(changed.colony.residents.filter(r=>r.workId===workId).map(r=>[r.id,r.x,r.y,r.earned,r.workedSeconds]),positions);
 assert(changed.colony.navigationRevision>s.colony.navigationRevision);
 await speed(1);await wait(s=>s.colony.residents.some(r=>r.workId&&r.activity==='working'&&r.cell[0]===27&&r.cell[1]===19));await speed(0);await snap('08-work-at-new-location');
 pass('Right-click context-menu route and inspection Move both work; occupied workplace moves without teleport/pay, invalidates navigation, then real work resumes');
 // Save and import the populated relocation using real controls.
 const exportWait=page.waitForEvent('download');await menu('Export landscape…');const exported=path.join(out,'relocated-85.json');await(await exportWait).saveAs(exported);const saved=await read();
 await page.reload();await ready();assert.deepEqual(await read(),saved);await importFile(exported);changed=await read();assert.deepEqual(changed.lots,saved.lots);assert.deepEqual(changed.colony.residents,saved.colony.residents);assert(changed.colony.navigationRevision>saved.colony.navigationRevision);
 await page.setViewportSize({width:1440,height:900});await btn('Return to lane (H)').click();zoom=4;ox=oy=0;await layout();await snap('09-desktop-1440');await page.setViewportSize({width:1024,height:640});await btn('Return to lane (H)').click();await layout();
 pass('Populated relocation survives exact paused reload and validated file export/import; pan/zoom/resize remain usable');
 await importFile(path.join(fixtures,'need-work.json'));const beforeTalk=await read();
 await selectPerson(meta.requestResident);await btn('Talk').click();await snap('10-conversation');
 await btn('How are you doing?').click();await btn('Tell me about your day.').click();await snap('11-truthful-recent-branch');await btn('Anything I can help with?').click();await snap('12-genuine-work-request');
 await btn('Not right now.').click();await snap('13-declined');await btn('Maybe later').click();assert.deepEqual(await read(),beforeTalk);
 await btn('Talk').click();await btn('Is there anything you need?').click();await btn("I'll see what I can do.").click();s=await read();assert.equal(s.colony.requests.records.length,1);assert.equal(s.colony.requests.records[0].resident,meta.requestResident);assert.equal(s.colony.requests.records[0].state,'accepted');assert.equal(s.colony.money,beforeTalk.colony.money);await snap('14-promise');await btn('Back to building').click();
 await stroke(4,[[6,14],[7,14]]);s=await read();assert.equal(s.colony.requests.records[0].state,'accepted');assert.equal(s.colony.money,beforeTalk.colony.money-16000);await snap('15-build-real-jobs');
 await speed(1);await wait(s=>s.colony.requests?.records[0]?.state==='resolved');await speed(0);s=await read();assert(resident(s,meta.requestResident).workedSeconds>0);const reward=s.colony.requests.records[0];assert.equal(reward.rewardUntil,reward.closedAt+75);
 await selectPerson(meta.requestResident);await btn('Talk').click();assert((await labels()).includes('actually been back at work'));await snap('16-truthful-acknowledgment');await page.keyboard.press('Escape');
 const paid=await read();await page.keyboard.press('Control+z');s=await read();assert.equal(s.colony.requests.records[0].rewardUntil,reward.rewardUntil);assert.equal(s.colony.money,paid.colony.money+16000);assert.equal(resident(s,meta.requestResident).workIssue,'noWork');await page.keyboard.press('Control+Shift+z');
 await stroke(2,[[8,15],[12,15]]);await stroke(6,[[8,16]]);await snap('17-building-continues');
 const requestSaved=await read(),requestFile=path.join(out,'request-resolved.json');const requestDownload=page.waitForEvent('download');await menu('Export landscape…');await(await requestDownload).saveAs(requestFile);
 await page.reload();await ready();assert.deepEqual(await read(),requestSaved);await importFile(requestFile);const requestImported=await read();assert.deepEqual(requestImported.colony.requests,requestSaved.colony.requests);assert.deepEqual(requestImported.colony.residents,requestSaved.colony.residents);assert.equal(requestImported.colony.money,requestSaved.colony.money);
 pass('Two truthful branches and decline are read-only; deliberate promise, paid jobs, actual productive work, one gratitude award/acknowledgment, history and free continued building');
 for(const sponsor of [false,true]){
  await importFile(path.join(fixtures,'market-day.json'));s=await read();const money=s.colony.money,event=s.colony.special.id;
  await btn(/^Market day/).click();await btn('Discuss Market day').click();await snap(sponsor?'19-sponsored-decision':'18-ordinary-decision');
  await btn(sponsor?/^Sponsor ·/:'Let it happen normally · Free').click();changed=await read();assert.equal(changed.colony.special.civicChoice,sponsor?'sponsored':'ordinary');assert.equal(changed.colony.money,money-(sponsor?3000:0));
  await btn('Back to building').click();assert.equal(await btn('Talk').count(),0);await btn(/^Market day/).click();await btn('Discuss Market day').click();assert.equal(await btn(/^Sponsor ·/).count(),0);assert.equal((await read()).colony.money,changed.colony.money);await page.keyboard.press('Escape');
  await speed(1);await wait(s=>s.colony.tradeEarned>0);await speed(0);s=await read();assert.equal(s.colony.special.id,event);
  assert.equal(s.colony.special.civicChoice,sponsor?'sponsored':'ordinary');assert.equal(s.colony.civicSpent||0,sponsor?3000:0);
  if(sponsor){assert(s.colony.special.civicAwarded.length>0&&s.colony.special.civicAwarded.length<=6);for(const id of s.colony.special.civicAwarded)assert(resident(s,id).civicBenefitUntil> s.colony.clock);}
  else assert(s.colony.residents.every(r=>!r.civicBenefitUntil));
  await snap(sponsor?'21-real-sponsored-service':'20-real-ordinary-service');
  await stroke(0,[[3,15]]);await page.keyboard.press('Control+z');assert.equal((await read()).colony.special.civicChoice,sponsor?'sponsored':'ordinary');
  const checkpoint=await read();await page.reload();await ready();assert.deepEqual(await read(),checkpoint);
  const decisionFile=path.join(out,sponsor?'sponsored.json':'ordinary.json'),decisionSaved=await read();const decisionDownload=page.waitForEvent('download');await menu('Export landscape…');await(await decisionDownload).saveAs(decisionFile);await importFile(decisionFile);const decisionImported=await read();assert.deepEqual(decisionImported.colony.special,decisionSaved.colony.special);assert.deepEqual(decisionImported.colony.civicDecisions,decisionSaved.colony.civicDecisions);assert.equal(decisionImported.colony.civicDecisions[event],sponsor?'sponsored':'ordinary');assert.equal(decisionImported.colony.money,decisionSaved.colony.money);assert.equal(decisionImported.colony.civicSpent||0,sponsor?3000:0);
 }
 pass('Both eligible civic branches: exact one-time price, no replay by reopening, actual service and bounded hospitality, unchanged ordinary trade, Undo and reload');
 await importFile(path.join(fixtures,'market-no-customers.json'));await btn(/^Market day/).click();assert.equal(await btn('Discuss Market day').count(),0);await snap('22-no-customer-decision');
 await importFile(path.join(fixtures,'market-low-funds.json'));s=await read();assert(s.colony.money<3000);const lowMoney=s.colony.money;
 await btn(/^Market day/).click();await btn('Discuss Market day').click();assert(await btn(/^Sponsor ·/).isDisabled());assert((await labels()).includes('Not enough coins'));await snap('23-insufficient-funds');const stablePanel=await read();await btn('Zoom out (−)').click();await page.keyboard.press('ArrowRight');
 for(const viewport of [{width:1440,height:900},{width:931,height:582},{width:1024,height:640}]){await page.setViewportSize(viewport);await btn('Maybe later').scrollIntoViewIfNeeded();const bounds=await btn('Maybe later').boundingBox();assert(bounds.y+bounds.height<=viewport.height);await snap('24-civic-'+viewport.width);assert.deepEqual(await read(),stablePanel);}
 await btn('Let it happen normally · Free').click();assert.equal((await read()).colony.money,lowMoney);
 pass('Civic eligibility and affordability: no decision without customers; paid-construction depleted treasury disables sponsorship honestly and permits ordinary free choice; live dialogue survives pan/zoom and 1440/1024/931-CSS layouts without changing gameplay');

 assert.deepEqual(errors,[]);const video=await page.video().path();await context.close();fs.renameSync(video,path.join(out,'gameplay.webm'));await browser.close();
 fs.writeFileSync(path.join(out,'browser-results.json'),JSON.stringify({seed:202,checks,errors,timeline,population:85,native:false,input:'Playwright mouse/keyboard, actual menus/file chooser; read-only checkpoint assertions; no game callbacks',fixtureAssistance:'Safe 85-person copy, four-person genuine-need town, actual-service eligible Market day generated explicitly by developer tool'},null,2));
})().catch(async e=>{console.error(e);if(page){await page.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});try{fs.writeFileSync(path.join(out,'failure-state.json'),await page.evaluate(k=>localStorage.getItem(k),key));}catch{}}if(browser)await browser.close();process.exitCode=1;});
