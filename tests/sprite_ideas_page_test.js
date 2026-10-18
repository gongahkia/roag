// Read-only state/data checks, plus an optional isolated headless Chromium run.
// node tests/sprite_ideas_page_test.js --browser /path/to/chrome /var/tmp/roag-idea-review
const assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path'),os=require('node:os');
const crypto=require('node:crypto'),{spawn,execFileSync}=require('node:child_process');
const {pathToFileURL}=require('node:url');
const API=require('../tools/sprite_ideas_client.js');
const Feedback=require('../tools/sprite_ideas_feedback.js');
const folder='docs/sprite-audit/';
const data=JSON.parse(fs.readFileSync(folder+'ideas.json','utf8'));
const html=fs.readFileSync(folder+'ideas.html','utf8');
const embedded=JSON.parse(html.match(/<script id="ideas-data" type="application\/json">([^<]+)<\/script>/)[1]);
const copy=v=>JSON.parse(JSON.stringify(v));let passed=0;
function test(name,fn){fn();passed++;console.log('PASS '+name);}
test('all 3305 unused cells have one owner, with source/pack and live-usage checks',()=>{
  const live=JSON.parse(execFileSync('luajit',['tools/sprite_audit.lua']));
  assert.equal(data.mapping_checksum,live.mapping_checksum);
  assert.equal(data.summary.proposals,89);assert.equal(data.summary.held_groups,2);
  assert.equal(Object.keys(data.cells).length,3305);
  assert.equal(data.summary.proposed_cells+data.summary.held_cells,3305);
  const ids=new Set(data.cards.map(c=>c.id));assert.equal(ids.size,data.cards.length);
  let ownerCount=0;
  for(const c of data.cards){
    const unique=new Set(c.groups.flatMap(g=>g.cells));
    assert.equal(unique.size,c.candidate_count);
    assert.equal(c.owned_count,[...unique].filter(k=>data.cells[k].owner===c.id).length);
    ownerCount+=c.owned_count;
    for(const p of ['kenney','onebit'])for(const g of c.art[p]){
      if(p==='onebit')assert.equal(g.sheet,'onebit');
      for(const k of g.cells){assert(data.cells[k]);assert(data.cells[k].proposals.includes(c.id));}
    }
  }
  assert.equal(ownerCount,3305);
  for(const [key,c] of Object.entries(data.cells)){
    const [sheet,index]=key.split(':'),s=data.sources[sheet];
    assert(ids.has(c.owner));assert(c.proposals.includes(c.owner));assert(c.nontransparent_pixels>0);
    assert(Number(index)%s.columns*s.stride+s.tile<=s.width);
    assert(Math.floor(Number(index)/s.columns)*s.stride+s.tile<=s.height);
    for(const p of Object.values(live.packs))assert(!p.usage[sheet]?.[index]);
  }
});
test('all five embedded PNGs are original bytes, and HTML/data match exactly',()=>{
  for(const [name,s] of Object.entries(data.sources)){
    const bytes=fs.readFileSync(s.path);
    assert(Buffer.from(embedded.sources[name].image.split(',')[1],'base64').equals(bytes));
    assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),s.sha256);
    delete embedded.sources[name].image;
  }
  assert.deepEqual(embedded,data);
});
test('nothing is approved by default; held art is never proposed elsewhere',()=>{
  const state=API.blank(data);assert.deepEqual(API.checked(data,state),state);
  const p=API.progress(data,state);
  assert.equal(p.total,89);assert.equal(p.reviewed,0);assert.equal(p.artReviewed,0);
  const held=data.cards.filter(c=>c.excluded).flatMap(c=>c.groups.flatMap(g=>g.cells));
  assert.equal(new Set(held).size,23);
  for(const key of held)assert(data.cells[key].proposals.every(id=>data.cards.find(c=>c.id===id).excluded));
  assert(held.includes('micro:123')&&held.includes('onebit:740'));
});
test('literal comments, decisions, priorities and preferred cells round-trip without mutating input',()=>{
  const state=API.blank(data),r=state.cards.find(r=>r.id==='reanimator');
  r.vote='discuss';r.priority='next';r.comment='<img src=x onerror=alert(1)> once only, no XP farm';
  r.art.kenney.vote='approve';r.art.kenney.preferred=['tiny:85'];r.art.onebit.comment='Use the hooded figure.';
  const before=copy(state),out=API.checked(data,copy(state));
  assert.deepEqual(out,before);assert.deepEqual(state,before);
  assert.equal(API.progress(data,out).reviewed,1);assert.equal(API.progress(data,out).artReviewed,1);
  const report=Feedback.report(data,out);
  assert.equal(report.reviewed[0].comment,r.comment);assert.deepEqual(report.reviewed[0].art_pending,['onebit']);
});
test('incompatible/duplicate/missing records, forged selections and unavailable art reject safely',()=>{
  const good=API.blank(data),before=copy(good);
  for(const mutate of [s=>s.schema='roag-enemy-combo-feedback/1',s=>s.proposal_fingerprint='old',
    s=>s.audit_mapping_checksum='old',s=>s.cards.pop(),s=>s.cards[1].id=s.cards[0].id,
    s=>s.cards[0].id='unknown',s=>s.cards[2].vote='approved',s=>s.cards[2].comment='x'.repeat(4001),
    s=>s.cards[2].art.kenney.preferred=['tiny:84'],s=>s.cards[2].art.onebit.preferred=['tiny:85'],
    s=>s.cards[2].art.kenney.preferred=['tiny:85','tiny:85'],s=>s.cards[0].vote='approve',
    s=>s.cards[0].art.kenney.vote='approve',
    s=>s.cards.find(r=>r.id==='drowned-sanctum').art.onebit.vote='approve']){
    const bad=copy(good);mutate(bad);assert.throws(()=>API.checked(data,bad));assert.deepEqual(good,before);
  }
});
test('approved preferred actor reuse is flagged; non-actor art is not a false actor conflict',()=>{
  const state=API.blank(data);
  for(const id of ['reanimator','spiritbinder-class']){
    const r=state.cards.find(r=>r.id===id);r.vote='approve';r.art.kenney.vote='approve';r.art.kenney.preferred=['tiny:85'];
  }
  assert.deepEqual(API.conflicts(data,API.checked(data,state)),[
    {pack:'kenney',key:'tiny:85',ideas:['reanimator','spiritbinder-class']}]);
  state.cards.find(r=>r.id==='spiritbinder-class').vote='reject';
  assert.equal(API.conflicts(data,state).length,0);
});
console.log(`${passed} idea data/state checks passed; no LÖVE initialized`);

async function browser(binary,output){
  fs.mkdirSync(output,{recursive:true});
  output=fs.mkdtempSync(path.join(path.resolve(output),'run-'));
  const profile=fs.mkdtempSync(path.join(os.tmpdir(),'roag-ideas-browser-'));
  const chrome=spawn(binary,['--headless=new','--no-sandbox','--disable-gpu','--disable-dev-shm-usage',
    '--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  let socket,serial=0;const pending=new Map(),errors=[];
  const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));
  function call(method,params={}){
    return new Promise((resolve,reject)=>{const id=++serial;pending.set(id,{resolve,reject});socket.send(JSON.stringify({id,method,params}));});
  }
  async function evaluate(expression){
    const r=await call('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});
    assert(!r.exceptionDetails,JSON.stringify(r.exceptionDetails));return r.result.value;
  }
  async function until(fn){for(let i=0;i<100;i++){if(await fn())return;await delay(100);}throw Error('Browser check timed out');}
  async function screenshot(name){
    const r=await call('Page.captureScreenshot',{format:'png',captureBeyondViewport:false});
    fs.writeFileSync(path.join(output,name+'.png'),Buffer.from(r.data,'base64'));
  }
  try{
    const endpoint=await new Promise((resolve,reject)=>{
      let log='';const timer=setTimeout(()=>reject(Error('Chromium did not start: '+log)),15000);
      chrome.stderr.on('data',b=>{log+=b;const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});
      chrome.on('error',reject);chrome.on('exit',code=>{if(code)reject(Error('Chromium exited '+code+': '+log));});
    });
    const address=new URL(endpoint),targets=await(await fetch('http://'+address.host+'/json/list')).json();
    socket=new WebSocket(targets.find(t=>t.type==='page').webSocketDebuggerUrl);
    await new Promise((resolve,reject)=>{socket.onopen=resolve;socket.onerror=reject;});
    socket.onmessage=e=>{const m=JSON.parse(e.data);if(m.id){const p=pending.get(m.id);pending.delete(m.id);
      if(m.error)p.reject(Error(JSON.stringify(m.error)));else p.resolve(m.result);}
      if(m.method==='Runtime.exceptionThrown')errors.push(m.params.exceptionDetails);};
    await call('Page.enable');await call('Runtime.enable');
    await call('Browser.setDownloadBehavior',{behavior:'allow',downloadPath:path.resolve(output)});
    await call('Emulation.setDeviceMetricsOverride',{width:1440,height:1100,deviceScaleFactor:1,mobile:false});
    await call('Page.navigate',{url:pathToFileURL(path.resolve(folder+'ideas.html')).href});
    await until(()=>evaluate(`document.readyState==='complete' && !!document.querySelector('article')`));
    const click=s=>evaluate(`document.querySelector(${JSON.stringify(s)}).click()`);
    const change=(id,value)=>evaluate(`(()=>{const e=document.getElementById(${JSON.stringify(id)});e.value=${JSON.stringify(value)};e.dispatchEvent(new Event(e.tagName==='INPUT'?'input':'change',{bubbles:true}))})()`);
    const open=async id=>{
      await change('category','all');await change('status-filter','all');await change('search','');
      await click(`[data-idea="${id}"]`);
    };
    await screenshot('initial-review');
    assert((await evaluate(`document.querySelector('#progress').textContent`)).startsWith('0/89'));
    await click('[data-action="vote"][data-scope="idea"][data-value="discuss"]');
    await change('priority','next');
    const comment='Fixture only: <img src=x onerror=alert(1)> keep one revival, no XP farming.';
    await evaluate(`(()=>{const e=document.querySelector('textarea[data-scope="idea"]');e.value=${JSON.stringify(comment)};e.dispatchEvent(new Event('input',{bubbles:true}))})()`);
    await click('[data-action="prefer"][data-pack="kenney"][data-key="tiny:85"]');
    await click('[data-action="vote"][data-scope="art"][data-pack="kenney"][data-value="approve"]');
    await click('[data-action="prefer"][data-pack="kenney"][data-key="tiny:88"]');
    assert.equal(await evaluate(`document.querySelector('[data-action="vote"][data-scope="art"][data-pack="kenney"][data-value="approve"]').getAttribute('aria-pressed')`),'false');
    await click('[data-action="vote"][data-scope="art"][data-pack="kenney"][data-value="approve"]');
    await open('haunted-cache');await open('reanimator');
    assert.equal(await evaluate(`document.querySelector('textarea[data-scope="idea"]').value`),comment);
    await call('Page.reload');await until(()=>evaluate(`!!document.querySelector('article')`));
    assert.equal(await evaluate(`document.querySelector('textarea[data-scope="idea"]').value`),comment);
    assert.equal(await evaluate(`document.querySelector('img[src="x"]')===null`),true);
    await screenshot('saved-draft');
    await click('#download');
    const file=path.join(output,'roag-sprite-ideas-feedback.json');await until(()=>fs.existsSync(file));
    const state=Feedback.read(file,data);assert.equal(state.cards.find(r=>r.id==='reanimator').comment,comment);
    assert.equal(Feedback.report(data,state).progress.reviewed,1);
    async function importFile(filename){
      const d=await call('DOM.getDocument'),n=await call('DOM.querySelector',{nodeId:d.root.nodeId,selector:'#import-file'});
      await call('DOM.setFileInputFiles',{nodeId:n.nodeId,files:[path.resolve(filename)]});
    }
    const bad=copy(state);bad.proposal_fingerprint='wrong';
    const badPath=path.join(output,'incompatible-fixture.json');fs.writeFileSync(badPath,JSON.stringify(bad));
    await importFile(badPath);await until(()=>evaluate(`document.querySelector('#message').textContent.startsWith('Import rejected:')`));
    assert.equal(await evaluate(`document.querySelector('textarea[data-scope="idea"]').value`),comment);
    await importFile(file);await until(()=>evaluate(`document.querySelector('#message').textContent.startsWith('Feedback imported.')`));
    // Every card renders, including excluded groups. Filters expose exact category counts.
    for(const [kind,count] of Object.entries(data.summary.categories)){
      await change('category',kind);assert.equal(await evaluate(`document.querySelectorAll('#queue button').length`),count);
      for(const c of data.cards.filter(c=>c.kind===kind)){
        await click(`#queue [data-idea="${c.id}"]`);
        assert.equal(await evaluate(`document.querySelector('article').id`),'idea-'+c.id);
        assert.equal(await evaluate(`document.querySelectorAll('.art-panel').length`),2);
      }
      await evaluate(`document.querySelector('#content').scrollIntoView()`);await screenshot('category-'+kind);
    }
    await open('rpg-architecture-kit');
    const details='details[data-pack="kenney"][data-more="0"]';
    await click(details+' summary');
    await until(()=>evaluate(`document.querySelector('${details}').dataset.loaded==='true'`));
    const last=await evaluate(`document.querySelector('${details} .tile:last-child').dataset.key`);
    await click(`${details} [data-key="${last}"]`);
    await until(()=>evaluate(`document.querySelector('${details}').open && !!document.querySelector('${details} .tile:last-child')`));
    assert.equal(await evaluate(`document.querySelector('[data-action="prefer"][data-pack="kenney"][data-key="${last}"]').getAttribute('aria-pressed')`),'true');
    // Missing art cannot be approved; choosing one look only changes the preview.
    assert.equal(await evaluate(`document.querySelector('[data-action="vote"][data-scope="art"][data-pack="onebit"][data-value="approve"]').disabled`),true);
    await change('look','onebit');assert.equal(await evaluate(`document.querySelector('.art-panel[data-pack="kenney"]').hidden`),true);
    await change('look','both');
    await change('status-filter','needs-art');
    assert.equal(await evaluate(`document.querySelectorAll('#queue button').length`),data.summary.needs_counterpart);
    await change('status-filter','next');assert.equal(await evaluate(`document.querySelectorAll('#queue button').length`),1);
    await change('status-filter','all');await change('search','tiny:122');
    assert((await evaluate(`document.querySelectorAll('#queue button').length`))>0);
    await change('search','not-a-real-idea-2710');assert(await evaluate(`document.querySelector('.empty')!==null`));
    await change('search','');await open('haunted-cache');
    await evaluate(`document.querySelector('#art-haunted-cache-kenney').scrollIntoView()`);
    assert(await evaluate(`getComputedStyle(document.querySelector('.sprite.tiny')).backgroundImage.includes('data:image/png;base64,')`));
    await screenshot('sprite-voting');
    await call('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:true});
    await evaluate('window.scrollTo(0,0)');await screenshot('mobile-controls');
    await evaluate(`document.querySelector('#content').scrollIntoView()`);await screenshot('mobile-idea');
    assert(await evaluate('document.documentElement.scrollWidth<=window.innerWidth'));
    assert.deepEqual(errors,[]);
    console.log('PASS browser: 91 groups, 8 categories, decisions/preferences, local persistence, real download/import/rejection, large galleries, filters and mobile');
    console.log('Fixture feedback is simulated, not user approval. Artifacts: '+path.resolve(output));
  }finally{
    if(socket)socket.close();chrome.kill('SIGTERM');
    await new Promise(resolve=>{if(chrome.exitCode!==null||chrome.signalCode!==null)resolve();else chrome.once('exit',resolve);});
    fs.rmSync(profile,{recursive:true,force:true});
  }
}
if(process.argv[2]==='--browser')browser(process.argv[3],process.argv[4]).catch(e=>{console.error(e);process.exitCode=1;});
