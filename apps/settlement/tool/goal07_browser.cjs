// Real release-browser input; checkpoint observations are read only.
const{chromium}=require('playwright'),fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const out=path.resolve(process.argv[2]||'/tmp/roag-goal07-browser');fs.mkdirSync(out,{recursive:true});
const url=process.env.SETTLEMENT_URL||'http://127.0.0.1:8768/settlement/',key='roag.settlement.landscape.v1';
let browser,page;const checks=[],errors=[],timeline=[];let started;
(async()=>{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
 const context=await browser.newContext({viewport:{width:1024,height:640},recordVideo:{dir:out,size:{width:1024,height:640}}});
 page=await context.newPage();started=Date.now();page.on('pageerror',e=>errors.push(e.message));
 const btn=n=>page.getByRole('button',{name:n,exact:typeof n==='string'}),read=()=>page.evaluate(k=>JSON.parse(localStorage.getItem(k)),key);
 async function ready(){await page.locator('#loading').waitFor({state:'detached'});const p=page.locator('flt-semantics-placeholder');if(await p.count())await p.press('Enter');await btn(/^1 Cottage/).waitFor();}
 async function dismiss(){if(await btn('Start building').count())await btn('Start building').click();}
 async function menu(n){await btn('Landscape menu').click();await page.getByRole('menuitem',{name:n,exact:true}).click();}
 async function load(f,confirm=true){const choose=page.waitForEvent('filechooser');await menu('Import landscape…');await(await choose).setFiles(f);await btn('Replace settlement').waitFor();await btn(confirm?'Replace settlement':'Keep current settlement').click();await page.waitForTimeout(250);zoom=4;ox=oy=0;await layout();}
 async function newWorld(preset,seed){timeline.push({name:'create-'+preset,seconds:(Date.now()-started)/1000});await menu('New landscape…');await btn(/Environment/).click();await page.getByRole('menuitem',{name:preset,exact:true}).click();await page.getByRole('textbox',{name:'World seed',exact:true}).fill(seed);await btn('Create world').click();await btn('Start building').click();await btn('Ⅱ').click();zoom=4;ox=oy=0;await layout();}
 async function wait(fn,timeout=90000){await page.waitForFunction(({key,fn})=>Function('s',`return (${fn})(s)`)(JSON.parse(localStorage.getItem(key))),{key,fn:fn.toString()},{timeout});}
 let width=1024,top,bottom,center,zoom=4,ox=0,oy=0;
 async function layout(){const t=await btn(/^1 Cottage/).boundingBox(),m=await btn('Landscape menu').boundingBox();width=await page.evaluate(()=>innerWidth);top=m.y+m.height+12;bottom=t.y-14;center=Math.max(9,(width/2-24)/48);}
 const pt=(x,y)=>[width/2+(x+.5-center)*12*zoom+ox,(top+bottom)/2+(y-15)*12*zoom+oy];
 async function pan(dx,dy){await page.mouse.move(650,430);await page.mouse.down({button:'right'});await page.mouse.move(650+dx,430+dy,{steps:10});await page.mouse.up({button:'right'});ox+=dx;oy+=dy;}
 async function tool(t){if(typeof t==='number'){await page.keyboard.press(String(t));return;}await btn(/Build catalogue/).click();const item=page.getByRole('menuitem',{name:new RegExp('^'+t+' ·')});await item.scrollIntoViewIfNeeded();await item.click();}
 async function stroke(t,cells){await tool(t);await page.mouse.move(...pt(...cells[0]));await page.mouse.down();for(const cell of cells.slice(1))await page.mouse.move(...pt(...cell),{steps:10});await page.mouse.up();await page.waitForTimeout(150);}
 async function snap(n,keepPointer=false){timeline.push({name:n,seconds:(Date.now()-started)/1000});if(!keepPointer)await page.mouse.move(28,32);await page.waitForTimeout(200);await page.screenshot({path:path.join(out,n+'.png')});}
 const pass=s=>{checks.push(s);console.log('PASS',s);};
 await page.goto(url);await ready();await dismiss();await btn('Ⅱ').click();await layout();
 const old=path.resolve('../../docs/world-builder/evidence/tranche-05/large-85.json');await load(old);
 let s=await read(),initial=JSON.parse(fs.readFileSync(old));assert.deepEqual(s.lots,initial.lots);assert.equal(s.colony.residents.filter(r=>r.active).length,85);await snap('01-continued-legacy');
 await menu('New landscape…');await page.getByRole('textbox',{name:'World seed',exact:true}).fill('uncommitted preview');await btn('Continue existing settlement').click();assert.deepEqual((await read()).lots,initial.lots);
 await load(old,false);assert.deepEqual((await read()).lots,initial.lots);await page.reload();await ready();assert.deepEqual((await read()).lots,initial.lots);
 pass('Legacy 85-person town continues/reloads unchanged; cancelling New World and Import keeps it');
 await newWorld('Gentle Plains','meadow 07');const plains=await read();assert.equal(plains.generation.seedText,'meadow 07');assert.equal(plains.generation.preset,'plains');await snap('02-plains-created');
 await stroke(2,[[7,15],[10,15]]);await stroke(1,[[2,14],[5,14]]);await stroke(4,[[6,14],[7,14]]);await stroke(5,[[8,14]]);
 await btn('4×').click();await wait(s=>s.colony.workEarned>0 && s.colony.residents.some(r=>r.lastSocial>=0));await btn('Ⅱ').click();await snap('03-plains-alive');
 assert.equal((await read()).colony.residents.filter(r=>r.active).length,4);
 pass('Text-seeded Plains supports paid cottages, Farms, a Park, free paths and actual work/social life');
 await menu('World information & seed');await btn('Copy seed').waitFor();await context.grantPermissions(['clipboard-read','clipboard-write']);await btn('Copy seed').click();assert.equal(await page.evaluate(()=>navigator.clipboard.readText()),'meadow 07');await btn('Back to settlement').click();
 await newWorld('Gentle Plains','meadow 07');assert.deepEqual((await read()).lots,plains.lots);pass('Same seed/environment reproduces terrain; seed information and copy control are available');
 await newWorld('Woodlands','202');const woodlandMoney=(await read()).colony.money;await snap('04-woodlands-created');
 await stroke(2,[[7,15],[10,15]]);s=await read();assert.equal(s.lots[15*40+11][9],'forest');const beforeClear=s.lots;
 await tool('Clear');await page.mouse.move(...pt(11,15));await page.mouse.down();await page.mouse.move(...pt(11,17),{steps:16});await snap('05-woodland-staged',true);await page.mouse.up();await page.waitForTimeout(200);
 s=await read();assert.equal(s.colony.money,woodlandMoney-3000);assert.equal(s.lots[15*40+11].length,8);assert.equal(s.lots[15*40+11][2],'scar');
 await page.keyboard.press('Control+z');assert.deepEqual((await read()).lots,beforeClear);await page.keyboard.press('Control+Shift+z');assert.equal((await read()).colony.money,woodlandMoney-3000);
 await stroke(2,[[10,15],[11,15],[11,17]]);await stroke(1,[[12,15],[12,17]]);await snap('06-woodland-neighbourhood');
 await stroke('Plant',[[10,13]]);s=await read();assert.equal(s.lots[13*40+10][9],'forest');await page.keyboard.press('Control+z');await page.keyboard.press('Control+Shift+z');assert.deepEqual((await read()).lots,s.lots);
 pass('Woodlands expands through paid Clear strokes, exact Undo/Redo, connected construction and Plant');
 await newWorld('River Country','202');s=await read();const water=[];for(let x=0;x<40;x++)if(s.lots[15*40+x][9]==='water')water.push(x);const left=water[0]-1,right=water.at(-1)+1;
 await btn('Zoom out (−)').click();zoom=3;await snap('07-river-created');await stroke(2,[[7,15],[left,15]]);const beforeBridge=await read();
 await stroke('Bridge',[[left,15],[right-1,15]]);assert.deepEqual((await read()).lots,beforeBridge.lots);assert.equal((await read()).colony.money,beforeBridge.colony.money);
 await tool('Bridge');await page.mouse.move(...pt(left,15));await page.mouse.down();await page.mouse.move(...pt(right,15),{steps:16});await snap('08-bridge-preview',true);await page.keyboard.press('Escape');await page.mouse.up();assert.deepEqual((await read()).lots,beforeBridge.lots);
 await stroke('Bridge',[[left,15],[right,15]]);s=await read();assert.equal(s.colony.money,beforeBridge.colony.money-water.length*3000);assert(water.every(x=>s.lots[15*40+x][10]==='horizontal'));await snap('09-complete-bridge');
 await page.keyboard.press('Control+z');assert.deepEqual((await read()).lots,beforeBridge.lots);await page.keyboard.press('Control+Shift+z');assert.deepEqual((await read()).lots,s.lots);
 await stroke(2,[[right,15],[right+2,15]]);await stroke(1,[[2,14],[5,14]]);await stroke(4,[[right+1,14]]);await stroke(5,[[7,14]]);
 await btn('1×').click();await wait(s=>s.colony.residents.some(r=>r.x>17&&r.x<20));await snap('10-actual-crossing');await wait(s=>s.colony.workEarned>0);await btn('Ⅱ').click();s=await read();await snap('11-work-across-river');
 pass('Atomic paid bridge preview/cancel/Undo/Redo; actual residents cross and earn from far-bank work');
 const beforeCut=s.colony.workEarned;await stroke(3,[[left+2,15]]);await snap('12-crossing-cut');await btn('1×').click();await page.waitForTimeout(10000);await btn('Ⅱ').click();s=await read();assert.equal(s.colony.workEarned,beforeCut);assert(s.colony.residents.every(r=>!r.route.some(c=>c[0]===left+2&&c[1]===15)));await snap('13-residents-wait');
 await stroke('Bridge',[[left,15],[right,15]]);await btn('4×').click();await page.waitForFunction(({key,earned})=>JSON.parse(localStorage.getItem(key)).colony.workEarned>earned,{key,earned:beforeCut},{timeout:90000});await btn('Ⅱ').click();s=await read();assert(s.colony.workEarned>beforeCut);await snap('14-crossing-repaired');
 pass('Bridge removal stops disconnected production and changes real routes; paid one-cell repair resumes work');
 const wet=s.lots.findIndex((l,i)=>l[9]==='marsh'&&Math.abs(i%40-left)<3&&Math.abs(Math.floor(i/40)-15)<=3);assert(wet>=0);const wx=wet%40,wy=Math.floor(wet/40);
 await stroke(2,[[left,15],[left,wy],[wx,wy]]);s=await read();assert(s.lots[wet][3]);await stroke(9,[[wx,wy]]);assert.equal((await read()).lots[wet][8],'stone');assert.equal((await read()).lots[wet][9],'marsh');await snap('15-terrain-aware-stone');await page.keyboard.press('i');await page.mouse.click(...pt(wx,wy));await page.waitForTimeout(300);assert((await page.locator('flt-semantics').evaluateAll(es=>es.map(e=>e.getAttribute('aria-label')||e.textContent).join(' '))).includes('unaffected by wet ground or rain'));await snap('15b-wet-road-inspection');await page.keyboard.press('Escape');
 const file=path.join(out,'river-town.json'),dl=page.waitForEvent('download');await menu('Export landscape…');await(await dl).saveAs(file);const saved=await read();
 await page.reload();await ready();zoom=4;ox=oy=0;await layout();assert.deepEqual(await read(),saved);await load(file);assert.deepEqual((await read()).lots,saved.lots);assert.deepEqual((await read()).colony.residents,saved.colony.residents);
 pass('Concrete terrain, crossings, residents/events survive paused reload and real file export/import');
 await pan(-100,55);await btn('Zoom in (+)').click();zoom=5;await btn('Zoom out (−)').click();zoom=4;
 await page.setViewportSize({width:1440,height:900});await btn('Return to lane (H)').click();ox=oy=0;await layout();await snap('16-river-desktop-1440');
 await page.setViewportSize({width:931,height:582});await btn('Return to lane (H)').click();await layout();await snap('17-river-931');await page.setViewportSize({width:1024,height:640});await btn('Return to lane (H)').click();await layout();
 await page.keyboard.press('i');await page.mouse.click(...pt(2,14));await btn('Move building · Free (M)').click();await page.mouse.click(...pt(8,14));s=await read();assert(s.lots[14*40+8][5]);await page.keyboard.press('Control+z');await snap('18-final-river');
 pass('Pan, zoom, resize and contextual relocation remain usable on seeded terrain');
 assert.deepEqual(errors,[]);await context.close();await browser.close();fs.writeFileSync(path.join(out,'results.json'),JSON.stringify({url,input:'Playwright browser pointer/keyboard, real menus/downloads/file input; checkpoints read only',checks,errors,timeline},null,2));
})().catch(async e=>{console.error(e);if(page)await page.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});if(browser)await browser.close();process.exitCode=1});
