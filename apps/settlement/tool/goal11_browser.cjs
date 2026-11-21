// Actual release input. The initial compatibility segment uses a labelled old-town
// fixture; the new-region journey uses only ordinary UI, paid building and time.
const {chromium}=require('playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const url=process.env.SETTLEMENT_URL||'http://127.0.0.1:8765/roag/settlement/';
const out=path.resolve(process.argv[2]||'/var/tmp/roag-goal11/browser');fs.mkdirSync(out,{recursive:true});
let browser,page,context;
(async()=>{
 browser=await chromium.launch({executablePath:process.env.CHROMIUM_BIN||'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
 context=await browser.newContext({viewport:{width:1024,height:640},acceptDownloads:true,recordVideo:{dir:out,size:{width:1024,height:640}}});
 const key='roag.settlement.landscape.v1',legacy=fs.readFileSync(path.resolve(__dirname,'../test/fixtures/goal09-town.json'),'utf8');
 await context.addInitScript(({key,legacy})=>{if(!localStorage.getItem(key))localStorage.setItem(key,legacy)},{key,legacy});
 page=await context.newPage();const errors=[],failedAssets=[],checks=[],shots=[],hashes=new Set(),started=Date.now();
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});
 page.on('response',async r=>{if(r.status()>=400)failedAssets.push([r.status(),r.url()]);if(r.url().endsWith('/main.dart.js')&&r.status()===200){try{hashes.add(crypto.createHash('sha256').update(await r.body()).digest('hex'))}catch(e){errors.push(e.message)}}});
 const btn=n=>page.getByRole('button',{name:n,exact:typeof n==='string'});
 const state=()=>page.evaluate(()=>JSON.parse(window.settlementPlatform.load()));
 const worldOf=r=>r.towns.find(t=>t.site===r.active).colony;
 const townOf=(r,id)=>r.towns.find(t=>t.site===id);
 const lot=(w,x,y)=>w.lots[y*w.width+x];
 const pass=t=>{checks.push(t);console.log('PASS',t)};
 async function ready(){await page.locator('#loading').waitFor({state:'detached'});const a=page.locator('flt-semantics-placeholder');if(await a.count())await a.press('Enter');await btn(/^Region ·/).waitFor()}
 async function pause(){await btn('Ⅱ').click()}
 async function menu(name){await btn('Landscape menu').click();await page.getByRole('menuitem',{name,exact:true}).click()}
 async function map(){await btn(/^Region ·/).click();await page.getByRole('dialog').waitFor()}
 async function closeMap(){await btn('Back to settlement').click()}
 async function node(id){
   const identifier=page.locator('[flt-semantics-identifier="regional-'+id+'"]');
   if(await identifier.count()){await identifier.click();return;}
   const r=await state(),t=townOf(r,id),known=r.discovered.includes(id),site=r.geography.sites.find(s=>s.id===id);
   const names={plains:'Gentle Plains',woodlands:'Woodlands',river:'River Country',wetlands:'Wetlands',highlands:'Highlands',frontier:'Harsh Frontier',pineUplands:'Pine Uplands',saltCoast:'Salt Coast'};
   const name=t?t.name:known?names[site.environment]:'Uncharted '+id;
   await btn(new RegExp(name.replace(/[.*+?^${}()|[\]\\]/g,'\\$&')+'$')).click();
 }
 async function visit(id){await map();await node(id);await btn(id===(await state()).active?'Return to this settlement':'Visit settlement').click();await page.waitForTimeout(100)}
 async function snap(name){const d=page.getByRole('dialog');if(await d.count()){const b=await d.first().boundingBox();await page.mouse.move(b.x+8,b.y+8)}else{const h=await page.getByRole('group',{name:'Settlement controls',exact:true}).boundingBox();await page.mouse.move(h.x+5,8)}await page.waitForTimeout(120);await page.screenshot({path:path.join(out,name+'.png')});shots.push({name,seconds:(Date.now()-started)/1000})}
 async function waitState(fn,args={},timeout=120000){await page.waitForFunction(({f,args})=>{try{return Function('r','args','return ('+f+')(r,args)')(JSON.parse(window.settlementPlatform.load()),args)}catch(_){return false}},{f:fn.toString(),args},{timeout,polling:100})}
 async function point(x,y){const w=await page.getByRole('group',{name:'Settlement world',exact:true}).boundingBox();return [w.x+w.width/2+(x+.5-Math.max(9,(w.width/2-24)/48))*48,w.y+w.height/2+(y-15)*48]}
 async function tool(t){if(typeof t==='number'){await page.keyboard.press(String(t));return;}const escaped=t.replace(/[.*+?^${}()|[\]\\]/g,'\\$&'),it=btn(new RegExp('^(?:[0-9] )?'+escaped+' ·'));
   const h=await page.getByRole('group',{name:'Settlement controls',exact:true}).boundingBox(),undo=await btn('Undo').boundingBox();
   for(let i=0;i<28;i++){const b=await it.boundingBox();if(b&&b.y>=undo.y+undo.height+4&&b.y+b.height<h.height-6){await it.click();return;}await page.mouse.move(h.x+h.width/2,h.height-60);await page.mouse.wheel(0,b&&b.y<undo.y+undo.height+4?-350:350);await page.waitForTimeout(90)}throw Error('Could not reach tool '+t);
 }
 async function stroke(t,a,b=a){await tool(t);await page.mouse.move(...await point(...a));await page.mouse.down();await page.mouse.move(...await point(...b),{steps:Math.max(4,(Math.abs(b[0]-a[0])+Math.abs(b[1]-a[1]))*4)});await page.mouse.up();await page.waitForTimeout(100)}
 async function speed(n){
   if(n===16&&await btn('16×').count()===0){if(townOf(await state(),'site-00').colony.colony.speed!==16)await menu('Watch a year · 16×');return;}
   await btn(n+'×').click();
 }
 async function mapScroll(item){
   const d=await page.getByRole('dialog').first().boundingBox();
   for(let i=0;i<30;i++){const b=await item.boundingBox();if(b&&b.y>=d.y+80&&b.y+b.height<d.y+d.height-55)return;
     await page.mouse.move(d.x+d.width-140,d.y+d.height-100);await page.mouse.wheel(0,b&&b.y<d.y+80?-360:360);await page.waitForTimeout(90);}
   throw Error('Map inspector item could not be reached');
 }
 async function connect(name){const action=btn(new RegExp('^Connect '+name));await mapScroll(action);await action.click();}

 async function found(id,name){await map();await node(id);if(!(await state()).discovered.includes(id))await btn('Discover location · 2 coins').click();await snap('founding-'+id);const before=await state();await btn('Found settlement · 60 coins').click();await page.getByRole('textbox',{name:'Settlement name',exact:true}).fill(name);await btn('Confirm founding').click();const after=await state();assert.equal(townOf(after,'site-00').colony.colony.money,townOf(before,'site-00').colony.colony.money-60000);assert.equal(townOf(after,id).colony.colony.money,50000);await btn('Visit settlement').click()}
 function subset(a,e){if(e&&typeof e==='object'){if(Array.isArray(e)){assert(Array.isArray(a));assert.equal(a.length,e.length);e.forEach((v,i)=>subset(a[i],v));}else{for(const [k,v]of Object.entries(e))if(k!=='schema')subset(a[k],v)}}else assert.deepEqual(a,e)}
 await page.goto(url);await ready();let r=await state();assert.equal(r.schema,7);subset(townOf(r,'site-00').colony,JSON.parse(legacy));assert.equal(await page.evaluate(k=>localStorage.getItem(k+'.before-region-v7'),key),legacy);await snap('legacy-town-protected');await map();await snap('legacy-region');await closeMap();pass('Existing single-town fixture retains terrain, identities, finances, calendar, incidents and history; exact original text backed up before regional write');
 const protectedState=await state();await menu('New landscape…');await btn('Continue existing settlement').click();assert.deepEqual(await state(),protectedState);pass('Cancelling New World leaves the complete region unchanged');
 await menu('New landscape…');await page.getByRole('textbox',{name:'World seed',exact:true}).fill('202');await btn(/^Incidents /).click();await page.getByRole('menuitem',{name:'Peaceful · no destructive incidents',exact:true}).click();await btn('Create world').click();await btn('Start building').click();await pause();
 r=await state();assert.equal(r.geography.seedText,'202');assert.equal(townOf(r,'site-00').colony.colony.unlimited,false);const startMoney=townOf(r,'site-00').colony.colony.money;
 await stroke(1,[2,14],[5,14]);await stroke(4,[2,16]);await stroke(6,[4,16]);await stroke(7,[6,16]);await stroke(5,[6,14]);r=await state();assert.equal(townOf(r,'site-00').colony.colony.money,startMoney-64000);await snap('normal-founding-neighbourhood');await speed(16);await waitState(r=>r.towns.find(t=>t.site==='site-00').stage!=='hamlet');await pause();await snap('normal-village');pass('Normal entered-seed region reaches Village through paid cottages, a real Farm/Workshop/Market, actual work and social space; no Free Build or state injection');
 await found('site-01','Alder Reach');r=await state();assert.equal(r.active,'site-01');assert.equal(worldOf(r).generation.preset,'woodlands');const parentPeople=JSON.stringify(townOf(r,'site-00').colony.colony.residents.map(p=>[p.id,p.name,p.bodyId,p.people,p.palette,p.trait]));
 await btn('Return to lane (H)').click();await stroke(1,[4,14],[6,14]);r=await state();if(lot(worldOf(r),2,12)[1]!=='none')await stroke(3,[2,12]);await stroke('Plant',[2,12]);await stroke('Lumber Camp',[2,14]);await stroke(7,[6,16]);await stroke(5,[4,16]);r=await state();assert.equal(worldOf(r).colony.money,0);await snap('woodland-forestry-town');pass('Paid founding creates a different Woodland town; its 50-coin allocation builds real housing, managed timber, a Lumber Camp, Market and Park');
 await map();await node('site-00');await connect('Alder Reach');await snap('trade-established');await btn('Visit settlement').click();await speed(4);await waitState(r=>r.trade.shipments.some(s=>s.state==='transit'));await pause();let transit=await state();const shipment=transit.trade.shipments.find(s=>s.state==='transit');await snap('origin-caravan-dispatch');await page.reload();await ready();assert.deepEqual(await state(),transit);pass('A real origin caravan collects cargo and exits before regional travel; paused reload preserves the in-transit record exactly');
 await map();await node('site-01');await snap('caravan-on-regional-road');await btn('Visit settlement').click();await speed(4);await waitState(r=>r.trade.routes[0].deliveries>0);await pause();r=await state();assert(r.trade.shipments.some(s=>s.id===shipment.id&&s.state==='delivered'));assert(townOf(r,'site-00').exportsEarned>0);await snap('first-delivery-benefit');pass('Caravan completes regional transit, legally enters the destination, serves the Market and pays the exporter once');
 await visit('site-00');assert.equal(JSON.stringify(townOf(await state(),'site-00').colony.colony.residents.map(p=>[p.id,p.name,p.bodyId,p.people,p.palette,p.trait])),parentPeople);
 await found('site-03','Pine Crossing');r=await state();assert.equal(worldOf(r).generation.preset,'pineUplands');await btn('Return to lane (H)').click();await stroke(1,[2,14],[5,14]);await stroke(4,[2,16]);await stroke(7,[4,16]);await stroke(5,[6,14]);await snap('third-pine-settlement');await map();await node('site-00');await connect('Pine Crossing');await snap('three-town-network');await btn('Visit settlement').click();await speed(16);
 await waitState(r=>r.trade.routes.every(route=>route.deliveries>0)&&r.trade.routes.reduce((n,t)=>n+t.deliveries,0)>=4&&r.towns.find(t=>t.site==='site-00').stock.materials+r.towns.find(t=>t.site==='site-00').supplied.materials>=4);await pause();r=await state();assert.equal(r.towns.length,3);assert(r.towns.every(t=>t.colony.colony.clock===r.clock));await snap('background-progress');pass('Third settlement discovers real Pine Uplands terrain; three towns work and trade under a coherent shared calendar while another is viewed');
 await map();await node('site-00');const complete=btn('Complete Trade Network');await mapScroll(complete);await complete.click();await btn('Complete project').click();await snap('trade-network-project');await closeMap();r=await state();assert(r.projects.includes('network'));
 const witness=townOf(r,'site-00').colony.colony.residents.find(p=>p.active&&p.arrived);
 await page.keyboard.press('i');await page.mouse.click(...await point(witness.x-.5,witness.y-.5));await page.waitForTimeout(150);
 assert((await page.locator('body').ariaSnapshot()).includes('Regional supply +2'));await snap('actual-regional-mood-benefit');await page.keyboard.press('Escape');
 await stroke(1,[7,14]);pass('Actual multi-town deliveries and produced/imported goods complete the optional Trade Network, then ordinary paid construction continues');
 await speed(16);await waitState(r=>r.history.some(f=>f.kind==='merchant-opportunity'));await pause();await map();await snap('regional-merchant-opportunity');await closeMap();pass('Consumed delivered Wares produce a recorded regional merchant opportunity through the existing visitor system');
 await visit('site-01');const beforeCut=await state(),production=townOf(beforeCut,'site-01').produced.materials,camp=lot(townOf(beforeCut,'site-01').colony,2,14)[7];await stroke(3,[2,15]);await visit('site-00');await speed(16);const stoppedAt=(await state()).clock;await waitState((r,a)=>r.clock>=a.stoppedAt+35,{stoppedAt});await pause();r=await state();assert.equal(townOf(r,'site-01').produced.materials,production);await visit('site-01');await page.keyboard.press('Control+z');await page.waitForTimeout(150);const workersBefore=townOf(await state(),'site-01').colony.colony.residents.filter(p=>p.workId===camp).reduce((n,p)=>n+p.workedSeconds,0);await speed(16);await waitState((r,a)=>{const t=r.towns.find(t=>t.site==='site-01');return t.produced.materials>a.production || t.stock.materials===24&&t.colony.colony.residents.filter(p=>p.workId===a.camp).reduce((n,p)=>n+p.workedSeconds,0)>a.workersBefore+1},{production,camp,workersBefore});await pause();await snap('forestry-reconnected');pass('Breaking a real road stops off-screen forestry and trade access; construction Undo reconnects it without rewinding regional time or other towns');
 for(const size of [{width:1280,height:800},{width:931,height:582},{width:1024,height:640}]){await page.setViewportSize(size);await page.waitForTimeout(150);await btn('Return to lane (H)').click();await page.keyboard.press('ArrowRight');await page.keyboard.press('-');await btn('Return to lane (H)').click();await snap('viewport-'+size.width)}
 await visit('site-03');const seasonStart=await state(),calendarBefore=townOf(seasonStart,'site-00').colony.colony.climate;await speed(16);const yearAt=seasonStart.clock;await waitState((r,a)=>r.clock>=a.yearAt+245,{yearAt});await pause();const seasonal=await state();assert(seasonal.towns.every(t=>t.colony.colony.clock===seasonal.clock));assert.notDeepEqual(townOf(seasonal,'site-00').colony.colony.climate,calendarBefore);await visit('site-00');await snap('seasonal-background-return');pass('A complete season advances while Pine Crossing is viewed; return preserves coherent calendars, identities and continuing background work');
 const final=await state(),download=page.waitForEvent('download');await menu('Export landscape…');const file=await download,exported=path.join(out,'normal-developed-region.json');await file.saveAs(exported);assert.deepEqual(JSON.parse(fs.readFileSync(exported)),final);await page.reload();await ready();assert.deepEqual(await state(),final);pass('Developed three-town region exports and reloads exactly; pan/zoom and desktop resizing remain usable');
 const chooser=page.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles(exported);await btn('Replace settlement').click();
 const normalize=value=>{const c=JSON.parse(JSON.stringify(value));for(const t of c.towns){const s=t.colony.colony;s.planning=s.planning.map(p=>({id:p.id,plan:p.plan,valid:p.revision===s.navigationRevision}));delete s.navigationRevision;}return c;};
 assert.deepEqual(normalize(await state()),normalize(final));pass('Real regional file import preserves all towns, stocks, shipment lifecycle and relative queued-route validity');
 
 for(const id of ['site-00','site-03','site-01']){await visit(id);assert.equal((await state()).active,id)}
 assert((await state()).towns.every(t=>t.colony.colony.unlimited===false));assert.deepEqual(errors,[]);assert.deepEqual(failedAssets,[]);assert.equal(hashes.size,1);
 const video=page.video();await context.close();await video.saveAs(path.join(out,'normal-region-journey.webm'));await browser.close();
 fs.writeFileSync(path.join(out,'results.json'),JSON.stringify({url,mainJsSha256:[...hashes][0],input:'Playwright mouse/wheel/keyboard against actual release. Only the initial legacy compatibility segment injects the checked-in town fixture. The entered-seed progression, three founding towns, real production, shipments and project use normal paid rules, no developer injection or Free Build.',checks,shots,errors,failedAssets,elapsedSeconds:(Date.now()-started)/1000},null,2));console.log('DONE',checks.length,'checks');
})().catch(async e=>{console.error(e);if(page){await page.evaluate(()=>window.settlementPlatform.load()).then(s=>fs.writeFileSync(path.join(out,'failure-state.json'),s)).catch(()=>{});await page.screenshot({path:path.join(out,'error.png')}).catch(()=>{});console.error((await page.locator('body').ariaSnapshot().catch(()=>'' )).slice(0,7000))}if(context)await context.close().catch(()=>{});if(browser)await browser.close().catch(()=>{});process.exitCode=1});
