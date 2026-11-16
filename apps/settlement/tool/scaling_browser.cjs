// Opt-in release measurements with isolated fixture imports and browser input.
const {chromium}=require('playwright'),fs=require('node:fs'),path=require('node:path');
const out=path.resolve(process.argv[2]||'/tmp/roag-scaling.json');
const sampleMs=Number(process.env.SETTLEMENT_SAMPLE_MS||16000);
if(!Number.isFinite(sampleMs)||sampleMs<1000||sampleMs>60000)throw Error('SETTLEMENT_SAMPLE_MS must be 1000–60000.');
const url=process.env.SETTLEMENT_URL||'http://127.0.0.1:8767/settlement/?profile=1';
let browser;
(async()=>{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',headless:true,args:['--no-sandbox']});const results=[];
 for(const population of (process.env.SETTLEMENT_POPULATIONS||'85,250,500').split(',').map(Number)){
  const context=await browser.newContext({viewport:{width:1024,height:640}}),page=await context.newPage();
  await page.goto(url);await page.locator('#loading').waitFor({state:'detached'});await page.locator('flt-semantics-placeholder').press('Enter');
  await page.getByRole('button',{name:'Landscape menu',exact:true}).click();const chooser=page.waitForEvent('filechooser');await page.getByRole('menuitem',{name:'Import landscape…',exact:true}).click();await(await chooser).setFiles(`${process.env.SETTLEMENT_FIXTURE_DIR||'/tmp/roag-tranche05-fixtures'}/scale-${population}.json`);
  await page.getByRole('button',{name:'Replace settlement',exact:true}).click();await page.waitForTimeout(300);await page.getByRole('button',{name:'1×',exact:true}).click();
  const collect=()=>page.evaluate(async(sampleMs)=>{const start=window.settlementMetrics;let backlogs=[],times=[],prev=performance.now();const end=prev+sampleMs;await new Promise(resolve=>{function tick(t){backlogs.push(window.settlementMetrics?.planningBacklog||0);times.push(t-prev);prev=t;t<end?requestAnimationFrame(tick):resolve();}requestAnimationFrame(tick)});const metrics={...window.settlementMetrics};const calls=metrics.modelCalls-start.modelCalls;metrics.modelMicros=metrics.modelMicros.slice(-calls);metrics.presentationMicros=metrics.presentationMicros.slice(-calls);return{frames:times,metrics,maxBacklog:Math.max(...backlogs),startClock:start.clock};},sampleMs);
  const ordinary=await collect();
  await page.getByRole('button',{name:'Zoom out (−)',exact:true}).click();
  // remove a thirteen-cell strip from a genuine connected grid via the brush.
  const tool=await page.getByRole('button',{name:/^1 Cottage/}).boundingBox(),menu=await page.getByRole('button',{name:'Landscape menu',exact:true}).boundingBox();const cy=(menu.y+menu.height+12+tool.y-14)/2;
  await page.keyboard.press('3');await page.mouse.move(512+(3.5-(512-24)/48)*36,cy);await page.mouse.down();await page.mouse.move(512+(15.5-(512-24)/48)*36,cy,{steps:15});await page.mouse.up();
  const recovery=await collect();
  const summary=run=>{const sorted=a=>[...a].sort((a,b)=>a-b),stats=a=>{const s=sorted(a);return{count:s.length,mean:s.reduce((a,b)=>a+b,0)/s.length,p50:s[Math.floor(s.length*.5)],p95:s[Math.floor(s.length*.95)],max:s.at(-1)}};return{maxBacklog:run.maxBacklog,advancedSeconds:run.metrics.clock-run.startClock,frameMs:stats(run.frames),modelUs:stats(run.metrics.modelMicros),presentationUs:stats(run.metrics.presentationMicros),...Object.fromEntries(Object.entries(run.metrics).filter(([k])=>!k.endsWith('Micros')))};};
  results.push({population,ordinary:summary(ordinary),recovery:summary(recovery)});console.log('MEASURED',population,JSON.stringify(results.at(-1)));await context.close();
 }
 await browser.close();fs.writeFileSync(out,JSON.stringify({browser:'Chromium 155, software rendering',url,viewport:'1024x640',sampling:`${sampleMs/1000} wall-seconds ordinary +${sampleMs/1000} recovery per real population; no timing assertion`,results},null,2));
})().catch(async e=>{console.error(e);if(browser)await browser.close();process.exitCode=1});
