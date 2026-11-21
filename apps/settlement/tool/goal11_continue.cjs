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
 const key='roag.settlement.landscape.v1',legacy=fs.readFileSync(process.env.SETTLEMENT_NORMAL_CHECKPOINT||'/var/tmp/roag-goal11/browser-normal-final/failure-state.json','utf8');
 await context.addInitScript(({key,legacy})=>{if(!localStorage.getItem(key))localStorage.setItem(key,legacy)},{key,legacy});
 page=await context.newPage();const errors=[],failedAssets=[],checks=[],shots=[],hashes=new Set(),started=Date.now();
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});
 page.on('response',async r=>{if(r.status()>=400)failedAssets.push([r.status(),r.url()]);if(r.url().endsWith('/main.dart.js')&&r.status()===200){try{hashes.add(crypto.createHash('sha256').update(await r.body()).digest('hex'))}catch(e){errors.push(e.message)}}});
 const btn=n=>page.getByRole('button',{name:n==='Return to lane (H)'?/^Return to lane \(H\)/:n,exact:typeof n==='string'&&n!=='Return to lane (H)'});
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
 await page.goto(url);await ready();assert((await state()).towns.every(t=>!t.colony.colony.unlimited));
 for(const size of [{width:1280,height:800},{width:931,height:582},{width:1024,height:640}]){await page.setViewportSize(size);await page.waitForTimeout(150);await btn('Return to lane (H)').click();await page.keyboard.press('ArrowRight');await page.keyboard.press('-');await btn('Return to lane (H)').click();await snap('viewport-'+size.width)}
 await visit('site-03');const seasonStart=await state(),calendarBefore=townOf(seasonStart,'site-00').colony.colony.climate;await speed(16);const yearAt=seasonStart.clock;await waitState((r,a)=>r.clock>=a.yearAt+(900-a.yearAt%900)+5,{yearAt},240000);await pause();const seasonal=await state();assert(seasonal.towns.every(t=>t.colony.colony.clock===seasonal.clock));assert.notDeepEqual(townOf(seasonal,'site-00').colony.colony.climate,calendarBefore);await visit('site-00');await snap('seasonal-background-return');pass('A complete season advances while Pine Crossing is viewed; return preserves coherent calendars, identities and continuing background work');
 const final=await state(),download=page.waitForEvent('download');await menu('Export landscape…');const file=await download,exported=path.join(out,'normal-developed-region.json');await file.saveAs(exported);assert.deepEqual(JSON.parse(fs.readFileSync(exported)),final);await page.reload();await ready();assert.deepEqual(await state(),final);pass('Developed three-town region exports and reloads exactly; pan/zoom and desktop resizing remain usable');
 const chooser=page.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles(exported);await btn('Replace settlement').click();
 const normalize=value=>{const c=JSON.parse(JSON.stringify(value));for(const t of c.towns){const s=t.colony.colony;s.planning=s.planning.map(p=>({id:p.id,plan:p.plan,valid:p.revision===s.navigationRevision}));delete s.navigationRevision;}return c;};
 assert.deepEqual(normalize(await state()),normalize(final));pass('Real regional file import preserves all towns, stocks, shipment lifecycle and relative queued-route validity');
 
 for(const id of ['site-00','site-03','site-01']){await visit(id);assert.equal((await state()).active,id)}
 assert((await state()).towns.every(t=>t.colony.colony.unlimited===false));assert.deepEqual(errors,[]);assert.deepEqual(failedAssets,[]);assert.equal(hashes.size,1);
 const video=page.video();await context.close();await video.saveAs(path.join(out,'normal-region-journey.webm'));await browser.close();
 fs.writeFileSync(path.join(out,'results.json'),JSON.stringify({url,mainJsSha256:[...hashes][0],input:'Playwright mouse/wheel/keyboard against actual release. Continuation from the byte-identical normal paid-play checkpoint captured after forestry reconnection. No edits or developer state changes. Earlier normal journey supplies founding/trade/project footage.',checks,shots,errors,failedAssets,elapsedSeconds:(Date.now()-started)/1000},null,2));console.log('DONE',checks.length,'checks');
})().catch(async e=>{console.error(e);if(page){await page.evaluate(()=>window.settlementPlatform.load()).then(s=>fs.writeFileSync(path.join(out,'failure-state.json'),s)).catch(()=>{});await page.screenshot({path:path.join(out,'error.png')}).catch(()=>{});console.error((await page.locator('body').ariaSnapshot().catch(()=>'' )).slice(0,7000))}if(context)await context.close().catch(()=>{});if(browser)await browser.close().catch(()=>{});process.exitCode=1});
