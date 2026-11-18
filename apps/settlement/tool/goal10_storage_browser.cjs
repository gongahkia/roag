// Isolated old-save copies and explicit quota faults; never a player profile.
const {chromium}=require('playwright'),fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const out=path.resolve(process.argv[2]||'/var/tmp/roag-goal10/storage'),url=process.env.SETTLEMENT_URL||'http://127.0.0.1:42863/roag/settlement/',key='roag.settlement.landscape.v1';
fs.mkdirSync(out,{recursive:true});let browser;
(async()=>{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',headless:true,args:['--no-sandbox']});const checks=[],errors=[];
 async function ready(p){p.on('pageerror',e=>errors.push(e.message));await p.goto(url);await p.locator('#loading').waitFor({state:'detached'});await p.locator('flt-semantics-placeholder').press('Enter')}
 for(const [file,suffix] of [['goal08-town.json','.before-incidents-v5'],['goal09-town.json','.before-stories-v6']]){
  const text=fs.readFileSync(path.join(__dirname,'../test/fixtures',file),'utf8'),c=await browser.newContext({viewport:{width:1024,height:640}});
  await c.addInitScript(({key,text,suffix})=>{localStorage.setItem(key,text);const set=Storage.prototype.setItem;Storage.prototype.setItem=function(k,v){if(k.endsWith(suffix))throw new DOMException('Explicit test quota fault','QuotaExceededError');return set.call(this,k,v)}},{key,text,suffix});
  const p=await c.newPage();await ready(p);await p.waitForTimeout(3300);assert.equal(await p.evaluate(k=>localStorage.getItem(k),key),text);await p.screenshot({path:path.join(out,file.replace('.json','')+'-protected.png')});checks.push(file+': failed '+suffix+' backup leaves original bytes protected and autosave paused');await c.close();
 }
 const c=await browser.newContext({viewport:{width:1024,height:640}}),p=await c.newPage();await ready(p);const btn=n=>p.getByRole('button',{name:n,exact:true});await btn('Start building').click();await btn('Ⅱ').click();
 const before=await p.evaluate(k=>localStorage.getItem(k),key);
 for(const field of ['incidentRng','scenario']){
  const d=JSON.parse(before);if(field==='incidentRng')d.colony.preparedness.rng=0;else d.colony.emergence.scenario='unknown-scenario';const file=path.join(out,'invalid-'+field+'.json');fs.writeFileSync(file,JSON.stringify(d));
  await btn('Landscape menu').click();const chooser=p.waitForEvent('filechooser');await p.getByRole('menuitem',{name:'Import landscape…',exact:true}).click();await(await chooser).setFiles(file);await p.waitForTimeout(250);assert.equal(await p.evaluate(k=>localStorage.getItem(k),key),before);assert.equal(await btn('Replace settlement').count(),0);checks.push('Malformed '+field+' rejected through real file picker before replacement');
 }
 await p.screenshot({path:path.join(out,'invalid-import-preserves-town.png')});await c.close();await browser.close();assert.deepEqual(errors,[]);fs.writeFileSync(path.join(out,'results.json'),JSON.stringify({url,input:'Playwright pointer/file menus; explicit isolated legacy preloads and quota fault injection, no private player storage',checks,errors},null,2));console.log(checks.join('\n'));
})().catch(async e=>{console.error(e);if(browser)await browser.close();process.exitCode=1});
