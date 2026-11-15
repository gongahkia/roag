// Normal browser pointer/keyboard input against a served release. Reads saves only
// for assertions; never injects a landscape or calls a gameplay callback.
const {chromium} = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const output = path.resolve(process.argv[2] || '/tmp/settlement-browser-evidence');
fs.mkdirSync(output, {recursive:true});

(async () => {
  const browser = await chromium.launch({executablePath:process.env.CHROMIUM_BIN || '/usr/bin/chromium',
    headless:true, args:['--no-sandbox']});
  const context = await browser.newContext({viewport:{width:1024,height:640},
    recordVideo:{dir:output,size:{width:1024,height:640}}});
  await context.addInitScript(() => {
    const Base = window.AudioContext;
    window.audioObservation = {starts:0, contexts:[]};
    window.AudioContext = class extends Base {
      constructor(...args) {super(...args);window.audioObservation.contexts.push(this);}
      createOscillator() {
        const oscillator=super.createOscillator(), start=oscillator.start.bind(oscillator);
        oscillator.start=(...args)=>{window.audioObservation.starts++;return start(...args);};
        return oscillator;
      }
    };
  });
  const page = await context.newPage();
  const errors = [], failedRequests = [], steps = [], latencies = [];
  page.on('pageerror', e => errors.push(e.message));
  page.on('response', r => {if(r.status() >= 400) failedRequests.push([r.status(),r.url()]);});
  const url = process.env.SETTLEMENT_URL || 'http://127.0.0.1:8765/settlement/';
  await page.goto(url);
  await page.locator('flt-semantics-placeholder').press('Enter');
  await page.getByRole('button',{name:'2 Path',exact:true}).waitFor();
  await page.waitForLoadState('networkidle');
  const key = 'roag.settlement.landscape.v1';
  const raw = () => page.evaluate(key => localStorage.getItem(key), key);
  const saved = async () => JSON.parse(await raw());
  const lot = (s,x,y) => s.lots[y*s.width+x];
  let width=1024,height=640,scale=48,offsetX=0,offsetY=0;
  const point=(x,y) => [width/2+(x+.5-Math.max(9,(width/2-24)/48))*scale+offsetX,
    72+(height-156)/2+(y-15)*scale+offsetY];
  async function cell(x,y) {
    const p=point(x,y);assert(p[0]>0 && p[0]<width && p[1]>72 && p[1]<height-78);
    await page.mouse.move(...p);await page.mouse.click(...p);
  }
  const shot=async name => {await page.mouse.move(25,36);await page.waitForTimeout(220);
    await page.screenshot({path:path.join(output,name+'.png')});};
  const step=name=>{steps.push(name);console.log('PASS',name);};
  async function edit(tool,x,y,check) {
    await page.keyboard.press(String(tool),{delay:40});
    const start=performance.now(); await cell(x,y);
    await page.waitForFunction(({key,x,y,check}) => {
      const s=JSON.parse(localStorage.getItem(key));if(!s)return false;
      const l=s.lots[y*s.width+x];return check==='path'?l[3]:check==='cottage'?l[4]!==null:
        check==='pad'?l[4]===null&&l[2]==='pad':!l[3]&&l[2]==='scar';
    },{key,x,y,check});
    latencies.push(performance.now()-start);
    await page.waitForTimeout(70);
  }
  async function home() {
    await page.getByRole('button',{name:'Return to lane (H)',exact:true}).click();
    scale=48;offsetX=offsetY=0;
  }
  async function menu(name) {
    await page.getByRole('button',{name:'Landscape menu',exact:true}).click();
    await page.getByRole('menuitem',{name,exact:true}).click();
  }
  await shot('01-opening');
  for(let x=8;x<=13;x++)await edit(2,x,15,'path');
  step('Initial keyboard shortcut and lane extension');
  for(const [x,y] of [[3,14],[5,14],[7,16],[9,14],[11,16]])await edit(1,x,y,'cottage');
  for(const [x,y] of [[10,14],[10,13],[10,16],[10,17]])await edit(2,x,y,'path');
  for(const [x,y] of [[9,13],[11,13],[9,17],[11,17]])await edit(1,x,y,'cottage');
  step('Nine cottages, branches, and access junctions');await shot('02-junctions');
  const beforeHover=await raw();
  for(const p of [[12,14],[13,14],[9,16],[2,14]])await page.mouse.move(...point(...p));
  assert.equal(await raw(),beforeHover);step('Hover does not edit or save');
  await edit(3,5,14,'pad');await shot('03-retained-pad');
  await edit(3,6,15,'scar');await shot('04-disconnected');
  const disconnected=await saved();assert(lot(disconnected,7,15)[3]);assert(lot(disconnected,9,14)[4]);
  step('Demolition retains pad/scar, cottages and disconnected fragments');
  await edit(2,6,15,'path');await edit(1,5,14,'cottage');
  const restored=await raw();
  for(let i=0;i<3;i++)await page.keyboard.press('Control+z');
  assert.notEqual(await raw(),restored);
  for(let i=0;i<3;i++)await page.keyboard.press('Control+Shift+z');
  assert.equal(await raw(),restored);step('Reconnect, build over pad, exact undo/redo');
  await page.keyboard.press('1');const beforeInvalid=await raw();await cell(15,11);
  assert.equal(await raw(),beforeInvalid);step('Invalid disconnected placement is a no-op');
  // A left drag builds one stroke; one Undo restores it. Right drag pans only.
  const dragStart=point(4,14);await page.mouse.move(...dragStart);await page.mouse.down();
  await page.mouse.move(dragStart[0]+100,dragStart[1]+40,{steps:8});await page.mouse.up();
  assert.notEqual(await raw(),beforeInvalid);await page.keyboard.press('Control+z',{delay:40});assert.equal(await raw(),beforeInvalid);
  await page.mouse.move(600,400);await page.mouse.down({button:'right'});
  await page.mouse.move(696,448,{steps:8});await page.mouse.up({button:'right'});
  offsetX=96;offsetY=48;
  assert.equal(await raw(),beforeInvalid);await edit(3,3,14,'pad');await page.keyboard.press('Control+z');
  assert.equal(await raw(),beforeInvalid);step('Camera drag isolation and picking after pan');
  await home();await page.mouse.move(width/2,314);await page.mouse.wheel(0,-100);scale=60;
  await edit(3,5,14,'pad');await page.keyboard.press('Control+z');
  assert.equal(await raw(),beforeInvalid);step('Pointer-anchored zoom and picking');
  await page.setViewportSize({width:1440,height:900});width=1440;height=900;await home();
  await edit(1,2,16,'cottage');await shot('05-large-desktop');
  step('1440×900 resize and correctly picked placement');
  const beforeRefresh=await raw();await page.reload();
  await page.locator('flt-semantics-placeholder').press('Enter');
  await page.getByRole('button',{name:'Landscape menu',exact:true}).waitFor();
  await page.waitForLoadState('networkidle');
  assert.equal(await raw(),beforeRefresh);step('Refresh restores exact scene');
  // Real download and native file-input picker; import invokes public menu only.
  const downloadPromise=page.waitForEvent('download');await menu('Export landscape…');
  const download=await downloadPromise;const exported=path.join(output,'landscape.json');await download.saveAs(exported);
  assert.deepEqual(JSON.parse(fs.readFileSync(exported,'utf8')),await saved());
  await home();await edit(3,5,14,'pad');
  const chooserPromise=page.waitForEvent('filechooser');await menu('Import landscape…');
  await (await chooserPromise).setFiles(exported);
  await page.waitForFunction(({key,text})=>localStorage.getItem(key)===text,{key,text:beforeRefresh});
  step('Export, scene change and import reproduce exact composition');
  const invalid=path.join(output,'unsupported.json');fs.writeFileSync(invalid,'{"schema":99}');
  const badChooser=page.waitForEvent('filechooser');await menu('Import landscape…');
  await (await badChooser).setFiles(invalid);await page.waitForTimeout(350);
  assert.equal(await raw(),beforeRefresh);step('Unsupported import preserves scene');
  await menu('New landscape…');await page.getByRole('button',{name:'Keep building',exact:true}).click();
  assert.equal(await raw(),beforeRefresh);step('New Landscape cancellation and menu isolation');
  await page.getByRole('button',{name:'Mute',exact:true}).click();
  await page.getByRole('button',{name:'Unmute',exact:true}).waitFor();
  const audioBefore=await page.evaluate(()=>audioObservation.starts);
  await page.keyboard.press('3');await cell(1,13);
  assert.equal(await page.evaluate(()=>audioObservation.starts),audioBefore);
  await page.getByRole('button',{name:'Unmute',exact:true}).click();
  await cell(1,13);
  assert.equal(await page.evaluate(()=>audioObservation.starts),audioBefore+1);
  const audioStates=await page.evaluate(()=>audioObservation.contexts.map(c=>c.state));
  assert(audioStates.length>0 && audioStates.every(s=>s==='running'));
  step('User-gesture audio context running; mute suppresses oscillator creation');
  for(let x=14;x<=19;x++)await edit(2,x,15,'path');
  for(const x of [13,15,17,19]) {
    for(const y of [14,13,12]) await edit(2,x,y,'path');
  }
  for(const [x,y] of [[12,14],[14,14],[16,14],[18,14],
    [12,12],[14,12],[16,12],[18,12],[13,16],[15,16],[17,16],[19,16]])await edit(1,x,y,'cottage');
  const fuller=await saved();assert(fuller.lots.filter(l=>l[4]!==null).length>=22);
  step('Fuller settlement: 22 cottages and an extended branched road network');
  await shot('06-fuller-settlement');
  const deltas=await page.evaluate(()=>new Promise(resolve=>{
    const times=[];let last;
    function tick(t){if(last!==undefined)times.push(t-last);last=t;
      if(times.length<90)requestAnimationFrame(tick);else resolve(times);}
    requestAnimationFrame(tick);
  }));
  await page.setViewportSize({width:1024,height:640});width=1024;height=640;await home();
  await shot('07-final-1024');
  const fullerDownload=page.waitForEvent('download');await menu('Export landscape…');
  await (await fullerDownload).saveAs(path.join(output,'fuller-landscape.json'));
  assert.deepEqual(JSON.parse(fs.readFileSync(path.join(output,'fuller-landscape.json'),'utf8')),fuller);
  assert.deepEqual(errors,[]);assert.deepEqual(failedRequests,[]);
  const summary={browser:await browser.version(),viewportChecks:['1024x640','1440x900'],
    input:'Playwright browser mouse/keyboard; no game callbacks or world injection',seed:202,steps,
    cottageCount:fuller.lots.filter(l=>l[4]!==null).length,
    pathCount:fuller.lots.filter(l=>l[3]).length,
    inputToObservedAutosaveMs:{min:Math.min(...latencies),max:Math.max(...latencies),
      median:latencies.sort((a,b)=>a-b)[Math.floor(latencies.length/2)]},
    frameIntervalsMs:{median:deltas.sort((a,b)=>a-b)[45],max:Math.max(...deltas)},
    errors,failedRequests};
  fs.writeFileSync(path.join(output,'browser-results.json'),JSON.stringify(summary,null,2));
  await context.close();await browser.close();console.log(JSON.stringify(summary,null,2));
})().catch(e=>{console.error(e);process.exit(1);});
