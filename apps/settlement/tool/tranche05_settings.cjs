// Isolated release UI check; only media preference and tab focus are emulated.
const {chromium}=require('playwright');
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const out=path.resolve(process.argv[2]||'/tmp/roag-tranche05-settings');
const url=process.env.SETTLEMENT_URL||'http://127.0.0.1:8768/settlement/';
const key='roag.settlement.landscape.v1';fs.mkdirSync(out,{recursive:true});
(async()=>{
 const browser=await chromium.launch({executablePath:'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
 try{
  const context=await browser.newContext({viewport:{width:1024,height:640}}),page=await context.newPage(),errors=[];
  page.on('pageerror',e=>errors.push(e.message));await page.goto(url);
  await page.locator('#loading').waitFor({state:'detached'});await page.locator('flt-semantics-placeholder').press('Enter');
  const button=name=>page.getByRole('button',{name,exact:typeof name==='string'});
  await button('Landscape menu').click();const chooser=page.waitForEvent('filechooser');
  await page.getByRole('menuitem',{name:'Import landscape…',exact:true}).click();
  await(await chooser).setFiles('/tmp/roag-tranche05-browser-final/large-state.json');await button('Ⅱ').click();
  const read=()=>page.evaluate(k=>localStorage.getItem(k),key),saved=await read();
  await page.emulateMedia({reducedMotion:'reduce'});await page.waitForTimeout(500);
  await button(/^Rain shower/).click();await page.waitForTimeout(300);
  await page.screenshot({path:path.join(out,'rain-reduced-motion-inspection.png')});
  assert.equal(await read(),saved,'preference and event inspection leave gameplay unchanged');
  await page.keyboard.press('Escape');await page.emulateMedia({reducedMotion:'no-preference'});
  await button('1×').click();await page.waitForTimeout(1000);
  const other=await context.newPage();await other.goto('about:blank');await other.bringToFront();
  assert(await page.evaluate(()=>document.hidden));const hidden=JSON.parse(await read());
  await page.waitForTimeout(2200);assert.equal(JSON.parse(await read()).colony.clock,hidden.colony.clock);
  await page.bringToFront();await page.waitForTimeout(120);await button('Ⅱ').click();
  const resumed=JSON.parse(await read());assert(resumed.colony.clock-hidden.colony.clock<.6,'no hidden-tab catch-up');
  assert.deepEqual(errors,[]);
  fs.writeFileSync(path.join(out,'settings-results.json'),JSON.stringify({checks:['Reduced-motion preference during real active rain','Read-only persistent event inspection','Hidden tab pauses without catch-up'],input:'Playwright mouse/keyboard and browser media/tab controls; imported safe 85-person fixture; no game callbacks',hiddenSeconds:2.2,hiddenClock:hidden.colony.clock,resumedClock:resumed.colony.clock,errors},null,2));
  console.log('PASS reduced rain, event inspection and hidden-tab policy');await context.close();
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
