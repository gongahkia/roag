// Actual hosted release, fresh context; input and file chooser only.
const{chromium}=require('playwright'),fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const out=path.resolve(process.argv[2]||'/tmp/roag-goal07-public');fs.mkdirSync(out,{recursive:true});
(async()=>{const b=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--no-sandbox']});const p=await b.newPage({viewport:{width:1024,height:640}}),errors=[],bad=[];
try{
 p.on('pageerror',e=>errors.push(e.message));p.on('response',r=>{if(r.status()>=400)bad.push([r.status(),r.url()])});
 const btn=n=>p.getByRole('button',{name:n,exact:typeof n==='string'}),key='roag.settlement.landscape.v1',read=()=>p.evaluate(k=>JSON.parse(localStorage.getItem(k)),key);
 async function ready(){await p.locator('#loading').waitFor({state:'detached',timeout:60000});const x=p.locator('flt-semantics-placeholder');if(await x.count())await x.press('Enter');await btn(/^1 Cottage/).waitFor();}
 async function menu(n){await btn('Landscape menu').click();await p.getByRole('menuitem',{name:n,exact:true}).click();}
 await p.goto('https://gongahkia.github.io/roag/settlement/');await ready();await btn('Start building').click();await btn('Ⅱ').click();
 let s=await read();assert.equal(s.generation.version,2);assert.equal(s.generation.preset,'plains');
 await p.keyboard.press('1');await p.mouse.move(144,262);await p.mouse.down();await p.mouse.move(288,262,{steps:12});await p.mouse.up();
 await btn('1×').click();await p.waitForTimeout(1800);await btn('Ⅱ').click();s=await read();assert.equal(s.colony.residents.filter(r=>r.active).length,4);await p.screenshot({path:path.join(out,'public-browser.png')});
 const initial=s;await menu('New landscape…');await p.getByRole('textbox',{name:'World seed',exact:true}).fill('uncommitted public draft');await btn('Continue existing settlement').click();assert.deepEqual((await read()).lots,initial.lots);
 const invalid=path.join(out,'invalid.json');fs.writeFileSync(invalid,'{"schema":999}');let chooser=p.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles(invalid);await p.waitForTimeout(350);assert.deepEqual((await read()).lots,initial.lots);
 chooser=p.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles('/tmp/roag-goal07-fixtures/scale-500.json');await btn('Replace settlement').click();await btn('4×').click();
 await p.waitForFunction(k=>JSON.parse(localStorage.getItem(k))?.colony.workEarned>0,key,{timeout:60000});await btn('Ⅱ').click();s=await read();assert.equal(s.colony.residents.filter(r=>r.active).length,500);assert.equal(s.generation.preset,'river');
 const earned=s.colony.workEarned;await p.mouse.move(650,430);await p.mouse.down({button:'right'});await p.mouse.move(950,130,{steps:12});await p.mouse.up({button:'right'});await btn('4×').click();
 await p.waitForFunction(({key,earned})=>JSON.parse(localStorage.getItem(key)).colony.workEarned>earned,{key,earned},{timeout:30000});await btn('Ⅱ').click();s=await read();await p.screenshot({path:path.join(out,'public-500-river.png')});
 await p.reload();await ready();assert.deepEqual((await read()).lots,s.lots);assert.deepEqual((await read()).colony.residents,s.colony.residents);
 assert.deepEqual(errors,[]);assert.deepEqual(bad,[]);
 fs.writeFileSync(path.join(out,'results.json'),JSON.stringify({url:p.url(),generationVersion:2,secure:await p.evaluate(()=>isSecureContext),input:'Playwright browser mouse/keyboard and real file chooser; isolated fresh context; no game callbacks',checks:['Actual canonical public URL and local assets','Version 2 Gentle Plains; four cottages and resident arrivals','Cancelled world draft and invalid import preserve active settlement','Real import of validated 500-person generated River fixture','Actual off-screen work advances income after pan','Paused public reload retains terrain and residents'],population:500,workBeforePan:earned,workAfterPan:s.colony.workEarned,errors,bad},null,2));
}catch(e){await p.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});throw e;}finally{await b.close();}})().catch(e=>{console.error(e);process.exitCode=1});
