// Actual release UI; only safe file imports and read-only checkpoints assist the journey.
const {chromium}=require('playwright'),fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const out=path.resolve(process.argv[2]||'/tmp/roag-tranche06-locate');fs.mkdirSync(out,{recursive:true});
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
 await selectPerson(meta.requestResident);await btn('Talk').click();await btn('Is there anything you need?').click();await btn("I'll see what I can do.").click();await btn('Close conversation').click();
 const promised=await read();await btn('Locate request').click();await snap('request-located');assert.deepEqual(await read(),promised);
 // the located home is at canvas centre; selecting its roof and moving it proves the hit.
 const cy=(top+bottom)/2;await page.mouse.click(width/2,cy-16);await btn('Move building · Free (M)').click();await page.mouse.click(width/2+4*48,cy);
 let s=await read();assert.deepEqual(buildingCell(s,meta.requestResident),[6,14]);assert.equal(s.colony.money,promised.colony.money);
 await btn('Return to lane (H)').click();zoom=4;ox=oy=0;await layout();await selectPerson(meta.requestResident);await btn('Talk').click();await btn('Is there anything you need?').click();
 const before=await read();await btn('Withdraw my promise').click();s=await read();assert.equal(s.colony.requests.records[0].state,'cancelled');assert.equal(s.colony.requests.records[0].rewardUntil,-1);assert.equal(s.colony.money,before.colony.money);assert.equal(s.colony.clock,before.colony.clock);await snap('promise-withdrawn');assert.deepEqual(errors,[]);
 await context.close();await browser.close();fs.writeFileSync(path.join(out,'locate-results.json'),JSON.stringify({checks:['Locate request centres the correct existing home','Locating changes no gameplay or RNG state','Selecting the centred roof relocates the same home ID for free','Withdraw dialogue closes the actual promise without cost, reward or time'],input:'Playwright mouse/keyboard and actual file import; no game callbacks',seed:202,errors},null,2));console.log('PASS request locating and withdrawal through actual UI');
})().catch(async e=>{console.error(e);if(page)await page.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});if(browser)await browser.close();process.exitCode=1;});
