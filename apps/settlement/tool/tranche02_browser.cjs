// Browser input only. Save observation never calls the game or injects a world.
const {chromium}=require('playwright');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const crypto=require('node:crypto');
const out=path.resolve(process.argv[2]||'/tmp/settlement-tranche02');
fs.mkdirSync(out,{recursive:true});
let runningBrowser,runningPage;
function same(actual,expected){if(actual!==expected){const a=JSON.parse(actual),b=JSON.parse(expected);const changed=a.lots.map((l,i)=>JSON.stringify(l)!==JSON.stringify(b.lots[i])?[i%a.width,Math.floor(i/a.width)]:null).filter(Boolean);assert.fail(`Authored state changed: ${JSON.stringify(changed)}; IDs ${b.nextCottageId} -> ${a.nextCottageId}`);}}
(async()=>{
  const browser=await chromium.launch({executablePath:process.env.CHROMIUM_BIN||'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
  runningBrowser=browser;
  const context=await browser.newContext({viewport:{width:1024,height:640},recordVideo:{dir:out,size:{width:1024,height:640}}});
  await context.addInitScript(()=>{
    window.observedSaves=[];
    const write=Storage.prototype.setItem;
    Storage.prototype.setItem=function(k,v){if(k==='roag.settlement.landscape.v1')observedSaves.push(v);return write.call(this,k,v);};
  });
  const page=await context.newPage();runningPage=page;const errors=[],failed=[],steps=[];
  page.on('pageerror',e=>errors.push(e.message));
  page.on('response',r=>{if(r.status()>=400)failed.push([r.status(),r.url()]);});
  const start=performance.now();
  await page.goto(process.env.SETTLEMENT_URL||'http://127.0.0.1:8767/settlement/');
  await page.locator('flt-semantics-placeholder').press('Enter');
  await page.getByRole('button',{name:'2 Path',exact:true}).waitFor();
  await page.waitForLoadState('networkidle');
  const key='roag.settlement.landscape.v1';
  const raw=()=>page.evaluate(k=>localStorage.getItem(k),key);
  const world=async()=>JSON.parse(await raw());
  const writes=()=>page.evaluate(()=>observedSaves.length);
  const lot=(s,x,y)=>s.lots[y*s.width+x];
  const count=(s,type)=>s.lots.filter(l=>type==='path'?l[3]:l[4]!=null).length;
  const step=name=>{steps.push(name);console.log('PASS',name);};
  let zoom=4,ox=0,oy=0,center=0,rect;
  async function layout(){
    const first=await page.getByRole('button',{name:'1 Cottage',exact:true}).boundingBox();
    const menu=await page.getByRole('button',{name:'Landscape menu',exact:true}).boundingBox();
    const width=await page.evaluate(()=>innerWidth);
    rect={width,top:menu.y+menu.height+12,bottom:first.y-10};
  }
  await layout();center=Math.max(9,(rect.width/2-24)/48);
  const pt=(x,y)=>[rect.width/2+(x+.5-center)*12*zoom+ox,(rect.top+rect.bottom)/2+(y-15)*12*zoom+oy];
  async function select(n){await page.keyboard.press(String(n),{delay:40});}
  async function waitChanged(before){await page.waitForFunction(({key,before})=>localStorage.getItem(key)!==before,{key,before});}
  async function snap(name){await page.mouse.move(25,36);await page.waitForTimeout(100);await page.screenshot({path:path.join(out,name+'.png')});}
  async function drag(n,cells,{hold=false}={}){
    await select(n);const before=await raw(),saved=await writes();
    await page.mouse.move(...pt(...cells[0]));await page.mouse.down();
    for(const c of cells.slice(1))await page.mouse.move(...pt(...c),{steps:1});
    await page.waitForTimeout(70);
    assert.equal(await raw(),before,'a held stroke must not autosave');
    assert.equal(await writes(),saved);
    if(hold)return{before,saved};
    await page.mouse.up();await waitChanged(before);
    assert.equal(await writes(),saved+1,'one stroke requests exactly one save');
    return before;
  }
  async function undo(){await page.keyboard.press('Control+z',{delay:40});}
  async function redo(){await page.keyboard.press('Control+Shift+z',{delay:40});}
  async function home(){await page.getByRole('button',{name:'Return to lane (H)',exact:true}).click();zoom=4;ox=oy=0;await layout();center=Math.max(9,(rect.width/2-24)/48);}
  async function menu(label){await page.getByRole('button',{name:'Landscape menu',exact:true}).click();await page.getByRole('menuitem',{name:label,exact:true}).click();}

  await drag(2,[[7,15],[19,15]],{hold:true});await page.screenshot({path:path.join(out,'01-path-staged.png')});
  await page.mouse.up();await page.waitForFunction(k=>JSON.parse(localStorage.getItem(k))?.lots.filter(l=>l[3]).length===20,key);
  assert.equal(await writes(),1);step('Fast one-sample drag interpolates a continuous 12-cell lane extension, one save');
  await drag(1,[[2,14],[12,14]],{hold:true});await page.screenshot({path:path.join(out,'02-cottages-staged.png')});
  await page.mouse.up();await page.waitForFunction(k=>JSON.parse(localStorage.getItem(k))?.lots.filter(l=>l[4]!=null).length===11,key);
  assert.equal(await writes(),2);const village=await raw();step('Eleven cottages stage immediately and commit as one transaction');
  await page.mouse.move(25,36);await page.waitForTimeout(1100);await snap('03-arrivals');
  await drag(3,[[6,15]]);await page.waitForTimeout(900);await snap('04-cut-during-arrival');
  await undo();same(await raw(),village);await page.waitForTimeout(6200);await snap('05-inhabited');
  const authored=await raw(),saveCount=await writes();
  const imageA=await page.screenshot({clip:{x:24,y:rect.top+50,width:950,height:Math.min(300,rect.bottom-rect.top-50)}});
  await page.waitForTimeout(1000);
  const imageB=await page.screenshot({clip:{x:24,y:rect.top+50,width:950,height:Math.min(300,rect.bottom-rect.top-50)}});
  assert.notEqual(crypto.createHash('sha256').update(imageA).digest('hex'),crypto.createHash('sha256').update(imageB).digest('hex'));
  same(await raw(),authored);assert.equal(await writes(),saveCount);
  step('Visible arrivals, cut/reconnect while travelling, and ongoing path activity without save writes');
  const demoEnd=(performance.now()-start)/1000;

  const intact=await drag(3,[[3,14],[5,14],[3,14]]);const removed=await raw();
  assert.equal(count(await world(),'cottage'),8);assert.equal(lot(await world(),4,14)[2],'pad');
  await undo();same(await raw(),intact);await redo();same(await raw(),removed);await undo();
  step('Remove brush deduplicates footprints; one Undo/Redo restores the entire stroke');
  const stable=await raw();let saves=await writes();
  await drag(2,[[20,15]],{hold:true});await page.keyboard.press('Escape');await page.mouse.up();console.log("CHECK Escape");
  same(await raw(),stable);assert.equal(await writes(),saves);
  await drag(2,[[20,15]],{hold:true});await undo();await page.mouse.up();console.log("CHECK unfinished Undo");same(await raw(),stable);
  await drag(2,[[20,15]],{hold:true});await select(1);await page.mouse.up();console.log("CHECK tool switch");same(await raw(),stable);
  await drag(2,[[20,15]],{hold:true});const other=await context.newPage();await other.goto('about:blank');await other.bringToFront();
  await page.waitForTimeout(100);await other.close();await page.bringToFront();await page.mouse.up();console.log("CHECK lost focus");same(await raw(),stable);
  const cdp=await context.newCDPSession(page);
  await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:pt(13,16)[0],y:pt(13,16)[1]}]});
  await cdp.send('Input.dispatchTouchEvent',{type:'touchCancel',touchPoints:[]});console.log('CHECK pointer cancel');
  same(await raw(),stable);assert.equal(await writes(),saves);
  step('Escape, Undo during stroke, tool change, lost focus and actual pointer cancellation leave authored state unchanged');
  await select(1);await page.mouse.move(...pt(13,16));await page.mouse.down();
  await page.mouse.move(125,rect.bottom+30,{steps:1});await page.mouse.up();await waitChanged(stable);
  assert.equal(count(await world(),'cottage'),12);await undo();same(await raw(),stable);
  step('Leaving canvas commits only the last accepted cell, never a route behind toolbar controls');
  await page.keyboard.down('Space');await page.mouse.move(...pt(12,18));await page.mouse.down();
  await page.mouse.move(700,430,{steps:3});await page.keyboard.up('Space');await page.mouse.move(730,430,{steps:2});await page.mouse.up();
  same(await raw(),stable);await home();
  await drag(1,[[13,16]],{hold:true});await page.keyboard.down('Space');await page.mouse.move(...pt(14,16));await page.mouse.up();await page.keyboard.up('Space');
  await waitChanged(stable);assert.equal(count(await world(),'cottage'),13);await undo();same(await raw(),stable);
  step('Pan/build mode is chosen at pointer-down and survives modifier changes');
  await page.getByRole('button',{name:'Zoom out (−)',exact:true}).click();zoom=3;
  await drag(2,[[14,15],[14,11],[17,11],[17,9]]);
  assert(lot(await world(),17,11)[3]);assert.equal(lot(await world(),17,10)[1],'oak');assert(!lot(await world(),17,10)[3]);assert(!lot(await world(),17,9)[3]);
  await snap('06-turn-and-tree');step('Turn and obstacle: actual tree stops the described path without bridging its gap');
  await drag(1,[[6,16],[19,16]]);const pendingStart=performance.now(),withPending=await raw();
  await drag(3,[[19,16]]);assert(performance.now()-pendingStart<1800,'remove before the final 2-second arrival stagger');
  assert.equal(lot(await world(),19,16)[4],null);await undo();same(await raw(),withPending);await undo();await redo();
  same(await raw(),withPending);step('Remove a cottage while arrival is pending; reconcile undo/redo without changing its identity');
  await drag(2,[[5,15],[5,17],[19,17]]);await drag(1,[[5,18],[19,18]]);
  assert.equal(count(await world(),'cottage'),40);await page.waitForTimeout(1600);await snap('07-fuller-activity');
  await page.mouse.move(600,420);await page.mouse.down({button:'right'});await page.mouse.move(528,384,{steps:4});await page.mouse.up({button:'right'});ox=-72;oy=-36;
  await drag(2,[[17,11],[19,11]]);
  await page.setViewportSize({width:1440,height:900});await home();await page.getByRole('button',{name:'Zoom out (−)',exact:true}).click();zoom=3;
  await drag(1,[[18,10],[19,10]]);await snap('08-large-canvas');step('Pan, zoom, resize and repeat multi-piece placement through normal coordinates');
  const exported=path.join(out,'village.json'),download=page.waitForEvent('download');await menu('Export landscape…');await(await download).saveAs(exported);
  const snapshot=await raw();assert.deepEqual(JSON.parse(fs.readFileSync(exported,'utf8')),JSON.parse(snapshot));
  await drag(3,[[3,14]]);
  let chooser=page.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles(exported);
  await page.waitForFunction(({k,s})=>localStorage.getItem(k)===s,{k:key,s:snapshot});
  await page.reload();await page.locator('flt-semantics-placeholder').press('Enter');await page.waitForLoadState('networkidle');
  same(await raw(),snapshot);
  chooser=page.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles(path.resolve('../../docs/world-builder/evidence/tranche-01/fuller-landscape.json'));
  await page.waitForFunction(k=>JSON.parse(localStorage.getItem(k))?.schema===2 && JSON.parse(localStorage.getItem(k)).lots.filter(l=>l[4]!=null).length===22,key);
  chooser=page.waitForEvent('filechooser');await menu('Import landscape…');await(await chooser).setFiles(exported);
  await page.waitForFunction(({k,s})=>localStorage.getItem(k)===s,{k:key,s:snapshot});
  step('Refresh, complete-stroke export/import and a real schema-one save retain authored composition');
  await page.setViewportSize({width:1024,height:640});await home();await page.waitForTimeout(1300);await snap('09-final-1024');
  const intervals=await page.evaluate(()=>new Promise(resolve=>{const d=[];let last;function tick(t){if(last!==undefined)d.push(t-last);last=t;if(d.length<90)requestAnimationFrame(tick);else resolve(d);}requestAnimationFrame(tick);}));
  assert.deepEqual(errors,[]);assert.deepEqual(failed,[]);
  const final=await world();const result={seed:202,browser:await browser.version(),input:'Playwright mouse, keyboard and browser pointer/focus events; no game callbacks or injected world',steps,
    cottages:count(final,'cottage'),paths:count(final,'path'),uniqueCottageIds:new Set(final.lots.filter(l=>l[4]!=null).map(l=>l[5])).size,
    demoEndSeconds:demoEnd,frameIntervalsMs:{median:intervals.sort((a,b)=>a-b)[45],max:Math.max(...intervals)},errors,failed};
  fs.writeFileSync(path.join(out,'results.json'),JSON.stringify(result,null,2));
  await context.close();await browser.close();console.log(JSON.stringify(result,null,2));
})().catch(async e=>{console.error(e.message.split('\n')[0]);console.error(e.stack.split('\n').filter(l=>/^\s+at /.test(l)).join('\n'));if(runningPage)await runningPage.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});if(runningBrowser)await runningBrowser.close();process.exitCode=1;});
