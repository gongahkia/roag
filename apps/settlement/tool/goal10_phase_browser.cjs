// Targeted final presentation check; saved encounter phases remain authoritative.
const {chromium}=require('playwright'),fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const out=path.resolve(process.argv[2]||'/var/tmp/roag-goal10/phase-ui'),url=process.env.SETTLEMENT_URL||'http://127.0.0.1:42863/roag/settlement/',dir=process.env.SETTLEMENT_FIXTURE_DIR||'/var/tmp/roag-goal10/fixtures',key='roag.settlement.landscape.v1';fs.mkdirSync(out,{recursive:true});let browser;
(async()=>{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',headless:true,args:['--no-sandbox']});const c=await browser.newContext({viewport:{width:1024,height:640}}),p=await c.newPage(),checks=[],errors=[],failedAssets=[];let mainJsHash;
 p.on('pageerror',e=>errors.push(e.message));p.on('response',async r=>{if(r.status()>=400)failedAssets.push([r.status(),r.url()]);if(r.url().endsWith('/main.dart.js'))mainJsHash=crypto.createHash('sha256').update(await r.body()).digest('hex')});
 const btn=n=>p.getByRole('button',{name:n,exact:typeof n==='string'}),state=()=>p.evaluate(k=>JSON.parse(localStorage.getItem(k)),key);
 async function ready(){await p.locator('#loading').waitFor({state:'detached'});const a=p.locator('flt-semantics-placeholder');if(await a.count())await a.press('Enter');await btn(/^1 Cottage/).waitFor()}
 await p.goto(url);await ready();await btn('Start building').click();await btn('Ⅱ').click();
 for(const [file,label] of [['raid-unprepared','Market disruption'],['caravan','shelter stop']]){
  await btn('Landscape menu').click();const chooser=p.waitForEvent('filechooser');await p.getByRole('menuitem',{name:'Import landscape…',exact:true}).click();await(await chooser).setFiles(path.join(dir,file+'.json'));await btn('Replace settlement').click();await btn('1×').click();
  await p.waitForFunction(k=>JSON.parse(localStorage.getItem(k)).colony.preparedness.active?.state==='grazing',key,{timeout:45000});await btn(new RegExp(label)).waitFor();await btn('Ⅱ').click();const before=(await state()).colony;assert.equal(before.preparedness.active.state,'grazing');await p.screenshot({path:path.join(out,file+'-phase.png')});await p.reload();await ready();assert.deepEqual((await state()).colony,before);await btn(new RegExp(label)).waitFor();checks.push(file+': readable '+label+'; original saved phase and complete paused checkpoint reload unchanged');
 }
 if(process.env.SETTLEMENT_EXPECT_MAIN_SHA)assert.equal(mainJsHash,process.env.SETTLEMENT_EXPECT_MAIN_SHA);assert.deepEqual(errors,[]);assert.deepEqual(failedAssets,[]);await c.close();await browser.close();fs.writeFileSync(path.join(out,'results.json'),JSON.stringify({url,mainJsHash,input:'Real release browser pointer/file menus; explicitly imported comparison fixtures. No gameplay callbacks.',checks,errors,failedAssets},null,2));console.log(checks.join('\n'));
})().catch(async e=>{console.error(e);if(browser)await browser.close();process.exitCode=1});
