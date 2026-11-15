// Isolated release UI check; only the browser media preference is emulated.
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
  await button(/^Rain shower/).click();await page.mouse.move(28,32);await page.waitForTimeout(600);
  await page.screenshot({path:path.join(out,'rain-reduced-motion-inspection.png')});
  assert.equal(await read(),saved,'preference and event inspection leave gameplay unchanged');
  await page.keyboard.press('Escape');await page.emulateMedia({reducedMotion:'no-preference'});assert.equal(await read(),saved,'closing inspection and restoring motion leave gameplay unchanged');
  assert.deepEqual(errors,[]);
  fs.writeFileSync(path.join(out,'settings-results.json'),JSON.stringify({checks:['Reduced-motion preference during real active rain','Read-only persistent event inspection'],input:'Playwright mouse/keyboard and browser media controls; imported safe 85-person fixture; no game callbacks',errors},null,2));
  console.log('PASS reduced rain and event inspection');await context.close();
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
