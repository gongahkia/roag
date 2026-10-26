// Offline review checks; optional headless Chromium. Never starts LÖVE.
'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),os=require('node:os');
const crypto=require('node:crypto'),{spawn,execFileSync}=require('node:child_process');
const {pathToFileURL}=require('node:url');
const API=require('../tools/item_room_review_client.js'),Feedback=require('../tools/item_room_feedback.js');
const folder='docs/sprite-audit/',data=JSON.parse(fs.readFileSync(folder+'items-rooms.json','utf8'));
const html=fs.readFileSync(folder+'items-rooms.html','utf8'),copy=v=>JSON.parse(JSON.stringify(v));
let passed=0;
function test(name,fn){fn();passed++;console.log('PASS '+name);}
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
test('14 previously approved concepts, exact 6/8 split, with fresh blank detail votes',()=>{
  const old=JSON.parse(fs.readFileSync(folder+'ideas.json','utf8'));
  const prior=JSON.parse(fs.readFileSync(folder+'ideas-feedback.json','utf8'));
  assert.equal(data.cards.length,14);assert.equal(new Set(data.cards.map(c=>c.id)).size,14);
  assert.equal(data.cards.filter(c=>c.kind==='item').length,6);assert.equal(data.cards.filter(c=>c.kind==='room').length,8);
  assert.equal(data.origin.proposal_fingerprint,old.fingerprint);
  assert.equal(data.origin.feedback_sha256,hash(fs.readFileSync(data.origin.feedback_path)));
  for(const c of data.cards){
    assert.equal(prior.cards.find(r=>r.id===c.id).vote,'approve');assert.equal(c.history.vote,'approve');
    assert.equal(c.plan.length,4);assert(c.example&&c.decisions);assert.deepEqual(c.dependencies,[]);
  }
  const state=API.blank(data);assert.deepEqual(API.checked(data,state),state);
  assert.equal(API.progress(data,state).reviewed,0);assert.equal(API.progress(data,state).artReviewed,0);
  assert.equal(API.progress(data,state).artTotal,28);assert.equal(state.schema,'roag-item-room-feedback/1');
});
test('prior exclusions apply to both looks, and every suggested sprite is a legal option',()=>{
  const excluded={
    'escape-scroll':['onebit:525'],'charm-well':['rpg:855'],'memorial-choice':['onebit:24'],
    'signposted-routes':[22,425,737,794,851,908].map(i=>'rpg:'+i),
    'camp-choice':[616,617,618,619,674,675].map(i=>'rpg:'+i)
  };
  for(const c of data.cards)for(const p of ['kenney','onebit']){
    const options=API.candidates(c,p);assert(options.length);if(c.suggested[p])assert(options.includes(c.suggested[p]));
    for(const k of excluded[c.id]||[])assert(!options.includes(k));
    if(p==='onebit')assert(options.every(k=>k.startsWith('onebit:')));
  }
  const camp=data.cards.find(c=>c.id==='camp-choice');
  assert.deepEqual(API.candidates(camp,'kenney'),['micro:117','onebit:508','onebit:509']);
  assert(!API.candidates(data.cards.find(c=>c.id==='cleanse-flask'),'kenney').includes('tiny:117'));
  for(const id of ['bell-sequence','falling-fixture']){
    const c=data.cards.find(c=>c.id===id);assert.equal(c.suggested.onebit,null);assert(c.art_notes.onebit);
  }
  assert(!data.cards.some(c=>['oracle-choice','cantor-class','secret-mural','heat-vents'].includes(c.id)));
});
test('reviewed binding usage is exact, including all three occupied candidate cells',()=>{
  const approvalPath=folder+'items-rooms-approval.json';
  const record=fs.existsSync(approvalPath)?JSON.parse(fs.readFileSync(approvalPath,'utf8')):null;
  const snapshot=record&&record.inventory_path;
  const live=snapshot?JSON.parse(fs.readFileSync(snapshot,'utf8')):JSON.parse(execFileSync('luajit',['tools/sprite_audit.lua']));
  if(snapshot)assert.equal(hash(fs.readFileSync(snapshot)),record.review_files[snapshot]);
  assert.equal(data.mapping_checksum,live.mapping_checksum);assert.equal(data.requirements,live.requirements.length);
  assert.equal(data.summary.reused,3);
  for(const [key,cell] of Object.entries(data.cells)){
    const [sheet,index]=key.split(':');assert(cell.nontransparent_pixels>0);
    for(const p of ['kenney','onebit'])assert.deepEqual(cell.usage[p],live.packs[p].bindings
      .filter(b=>b.sheet===sheet&&b.index===Number(index)&&!b.pose_alias).map(b=>b.id).sort());
  }
  assert.deepEqual(data.cells['onebit:574'].usage.kenney,['core:ui/idea_oath_effect']);
  assert.deepEqual(data.cells['onebit:539'].usage.onebit,['core:ui/idea_corpse_effect']);
  assert.deepEqual(data.cells['onebit:915'].usage.kenney,['core:ui/idea_alarm_effect']);
});
test('embedded PNGs preserve original bytes; fingerprint and HTML match the source data',()=>{
  const embedded=JSON.parse(html.match(/<script id="ideas-data" type="application\/json">([^<]+)<\/script>/)[1]);
  for(const [name,s] of Object.entries(data.sources)){
    const bytes=fs.readFileSync(s.path);assert.equal(hash(bytes),s.sha256);
    assert(Buffer.from(embedded.sources[name].image.split(',')[1],'base64').equals(bytes));
    delete embedded.sources[name].image;
  }
  assert.deepEqual(embedded,data);
  // Python's default JSON encoder escapes non-ASCII. Match its canonical input.
  function canonical(v){if(Array.isArray(v))return v.map(canonical);if(v&&typeof v==='object')return Object.fromEntries(Object.keys(v).sort().map(k=>[k,canonical(v[k])]));return v;}
  const unsigned=copy(data);delete unsigned.fingerprint;
  const encoded=JSON.stringify(canonical(unsigned)).replace(/[\u007f-\uffff]/g,c=>'\\u'+c.charCodeAt(0).toString(16).padStart(4,'0'));
  assert.equal(hash(encoded),data.fingerprint);
});
test('partial feedback round-trips literal comments, preferences and reuse without mutating history',()=>{
  const state=API.blank(data),r=state.cards.find(c=>c.id==='return-token');
  r.vote='discuss';r.priority='later';r.comment='<img src=x onerror=alert(1)> reconsider Wayfinder overlap';
  r.art.kenney.vote='approve';r.art.kenney.preferred=['onebit:574'];r.art.onebit.comment='different icon?';
  const before=copy(state);assert.deepEqual(API.checked(data,state),before);assert.deepEqual(state,before);
  const report=Feedback.report(data,state);assert.equal(report.progress.reviewed,1);
  assert.equal(report.reviewed[0].prior_decision.vote,'approve');
  assert.equal(report.reviewed[0].selected_existing_bindings.kenney[0].cell,'onebit:574');
  assert.deepEqual(report.reviewed[0].art_pending,['onebit']);
});
test('old feedback, bad fingerprints, forged/rejected sprite choices and malformed records reject',()=>{
  const good=API.blank(data),before=copy(good);
  for(const mutate of [s=>s.schema='roag-sprite-ideas-feedback/1',s=>s.proposal_fingerprint='stale',
    s=>s.audit_mapping_checksum='stale',s=>s.cards.pop(),s=>s.cards[1].id=s.cards[0].id,
    s=>s.cards[0].id='unknown',s=>s.cards[0].vote='yes',s=>s.cards[0].priority='urgent',
    s=>s.cards[0].comment='x'.repeat(4001),s=>s.cards[0].art=null,
    s=>s.cards.find(c=>c.id==='escape-scroll').art.kenney.preferred=['onebit:525'],
    s=>s.cards.find(c=>c.id==='camp-choice').art.kenney.preferred=['rpg:616'],
    s=>s.cards[0].art.onebit.preferred=['tiny:74'],
    s=>s.cards[0].art.onebit.preferred=['onebit:690','onebit:690']]){
    const bad=copy(good);mutate(bad);assert.throws(()=>API.checked(data,bad));assert.deepEqual(good,before);
  }
  assert.throws(()=>API.checked(data,JSON.parse(fs.readFileSync(folder+'ideas-feedback.json','utf8'))));
});
test('feedback CLI validates without changing the input or review files',()=>{
  const dir=fs.mkdtempSync(path.join(os.tmpdir(),'roag-item-feedback-'));
  try{
    const filename=path.join(dir,'feedback.json'),state=API.blank(data);
    state.cards[0].vote='approve';fs.writeFileSync(filename,JSON.stringify(state));
    const before=fs.readFileSync(filename),page=fs.readFileSync(folder+'items-rooms.html');
    const report=JSON.parse(execFileSync('node',['tools/item_room_feedback.js',filename]));
    assert.equal(report.schema,'roag-item-room-feedback-report/1');assert.equal(report.progress.approved,1);
    assert(fs.readFileSync(filename).equals(before));assert(fs.readFileSync(folder+'items-rooms.html').equals(page));
  }finally{fs.rmSync(dir,{recursive:true,force:true});}
});
console.log(`${passed} focused data/state checks passed; no LÖVE initialized`);

async function browser(binary,parent){
  fs.mkdirSync(parent,{recursive:true});
  const output=fs.mkdtempSync(path.join(path.resolve(parent),'run-'));
  const profile=fs.mkdtempSync(path.join(os.tmpdir(),'roag-item-browser-'));
  const chrome=spawn(binary,['--headless=new','--no-sandbox','--disable-gpu','--disable-dev-shm-usage',
    '--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],{stdio:['ignore','ignore','pipe']});
  let socket,serial=0,log='';const pending=new Map(),errors=[];
  chrome.stderr.on('data',b=>{log+=b;});
  const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));
  function call(method,params={}){return new Promise((resolve,reject)=>{
    const id=++serial;pending.set(id,{resolve,reject});socket.send(JSON.stringify({id,method,params}));
  });}
  async function evaluate(expression){
    const r=await call('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});
    assert(!r.exceptionDetails,JSON.stringify(r.exceptionDetails));return r.result.value;
  }
  async function until(fn){for(let i=0;i<100;i++){if(await fn())return;await delay(100);}throw Error('Browser check timed out');}
  async function screenshot(name){
    await evaluate('new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))');
    const r=await call('Page.captureScreenshot',{format:'png',captureBeyondViewport:false});
    fs.writeFileSync(path.join(output,name+'.png'),Buffer.from(r.data,'base64'));
  }
  try{
    const endpoint=await new Promise((resolve,reject)=>{
      const timer=setTimeout(()=>reject(Error('Chromium did not start: '+log)),15000);
      chrome.stderr.on('data',()=>{const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});
      chrome.on('error',reject);chrome.on('exit',code=>{if(code){clearTimeout(timer);reject(Error('Chromium exited '+code));}});
    });
    const address=new URL(endpoint),targets=await(await fetch('http://'+address.host+'/json/list')).json();
    socket=new WebSocket(targets.find(t=>t.type==='page').webSocketDebuggerUrl);
    await new Promise((resolve,reject)=>{socket.onopen=resolve;socket.onerror=reject;});
    socket.onmessage=e=>{const m=JSON.parse(e.data);if(m.id){const p=pending.get(m.id);pending.delete(m.id);
      if(m.error)p.reject(Error(JSON.stringify(m.error)));else p.resolve(m.result);}
      if(m.method==='Runtime.exceptionThrown')errors.push(m.params.exceptionDetails);};
    await call('Page.enable');await call('Runtime.enable');
    await call('Browser.setDownloadBehavior',{behavior:'allow',downloadPath:output});
    await call('Emulation.setDeviceMetricsOverride',{width:1440,height:1100,deviceScaleFactor:1,mobile:false});
    await call('Page.navigate',{url:pathToFileURL(path.resolve(folder+'items-rooms.html')).href});
    await until(()=>evaluate(`document.readyState==='complete'&&!!document.querySelector('article')`));
    await evaluate(`Promise.all(Object.values(JSON.parse(document.getElementById('ideas-data').textContent).sources).map(s=>new Promise((resolve,reject)=>{const i=new Image();i.onload=resolve;i.onerror=reject;i.src=s.image})))`);
    const click=s=>evaluate(`document.querySelector(${JSON.stringify(s)}).click()`);
    const change=(id,value)=>evaluate(`(()=>{const e=document.getElementById(${JSON.stringify(id)});e.value=${JSON.stringify(value)};e.dispatchEvent(new Event(e.tagName==='INPUT'?'input':'change',{bubbles:true}))})()`);
    const open=async id=>{await change('category','all');await change('status-filter','all');await change('search','');await click(`#queue [data-idea="${id}"]`);};
    await screenshot('initial-desktop');
    assert((await evaluate(`document.querySelector('#progress').textContent`)).startsWith('0/14'));
    for(const [kind,count] of Object.entries(data.summary.categories)){
      await change('category',kind);assert.equal(await evaluate(`document.querySelectorAll('#queue button').length`),count);
      for(const c of data.cards.filter(c=>c.kind===kind)){
        await click(`#queue [data-idea="${c.id}"]`);
        assert.equal(await evaluate(`document.querySelector('article').id`),'idea-'+c.id);
        assert.equal(await evaluate(`document.querySelectorAll('.plan dt').length`),4);
        assert.equal(await evaluate(`document.querySelectorAll('.art-panel').length`),2);
        for(const p of ['kenney','onebit'].filter(p=>c.suggested[p])){
          const s=`[data-key="${c.suggested[p]}"][data-pack="${p}"]`;
          assert.equal(await evaluate(`!!document.querySelector(${JSON.stringify(s)})`),true);
        }
        await evaluate(`document.querySelector('.art-grid').scrollIntoView({block:'start'})`);
        await screenshot('art-'+c.id);
      }
    }
    await open('return-token');
    assert((await evaluate(`document.querySelector('#conflicts').textContent`)).includes('core:ui/idea_oath_effect'));
    await click('[data-scope="idea"][data-value="discuss"]');await change('priority','later');
    const comment='Fixture only: <img src=x onerror=alert(1)> avoid duplicating Wayfinder.';
    await evaluate(`(()=>{const e=document.querySelector('textarea[data-scope="idea"]');e.value=${JSON.stringify(comment)};e.dispatchEvent(new Event('input',{bubbles:true}))})()`);
    await click('[data-action="prefer"][data-pack="kenney"][data-key="onebit:574"]');
    await click('[data-scope="art"][data-pack="kenney"][data-value="approve"]');
    await click('#download');const file=path.join(output,data.feedback_filename);await until(()=>fs.existsSync(file));
    const state=Feedback.read(file,data);assert.equal(Feedback.report(data,state).progress.reviewed,1);
    assert.equal(state.cards.find(r=>r.id==='return-token').comment,comment);
    await call('Page.reload');await until(()=>evaluate(`!!document.querySelector('article')`));await open('return-token');
    assert.equal(await evaluate(`document.querySelector('textarea[data-scope="idea"]').value`),comment);
    assert.equal(await evaluate(`document.querySelector('img[src=x]')===null`),true);
    assert.equal(await evaluate(`document.querySelector('[data-scope="art"][data-pack="kenney"][data-value="approve"]').getAttribute('aria-pressed')`),'true');
    await click('[data-action="prefer"][data-pack="kenney"][data-key="onebit:574"]');
    assert.equal(await evaluate(`document.querySelector('[data-scope="art"][data-pack="kenney"][data-value="approve"]').getAttribute('aria-pressed')`),'false');
    async function importFile(filename){
      const d=await call('DOM.getDocument'),n=await call('DOM.querySelector',{nodeId:d.root.nodeId,selector:'#import-file'});
      await call('DOM.setFileInputFiles',{nodeId:n.nodeId,files:[path.resolve(filename)]});
    }
    const bad=copy(state);bad.proposal_fingerprint='wrong';
    const badPath=path.join(output,'incompatible-fixture.json');fs.writeFileSync(badPath,JSON.stringify(bad));
    await importFile(badPath);await until(()=>evaluate(`document.querySelector('#message').textContent.startsWith('Import rejected:')`));
    assert.equal(await evaluate(`document.querySelector('textarea[data-scope="idea"]').value`),comment);
    await importFile(path.resolve(folder+'ideas-feedback.json'));
    await until(()=>evaluate(`document.querySelector('#message').textContent.startsWith('Import rejected:')&&document.querySelector('#message').textContent.includes('Not a feedback file')`));
    await importFile(file);await until(()=>evaluate(`document.querySelector('#message').textContent.startsWith('Feedback imported.')`));
    await change('status-filter','next');assert.equal(await evaluate(`document.querySelectorAll('#queue button').length`),0);
    await change('status-filter','discuss');assert.equal(await evaluate(`document.querySelectorAll('#queue button').length`),1);
    await open('camp-choice');await change('look','onebit');
    assert.equal(await evaluate(`document.querySelector('.art-panel[data-pack=kenney]').hidden`),true);
    await change('look','both');await change('search','onebit:574');
    assert.equal(await evaluate(`document.querySelectorAll('#queue button').length`),1);
    await change('search','no-real-card-9173');assert(await evaluate(`!!document.querySelector('.empty')`));
    await open('camp-choice');await evaluate('window.scrollTo(0,0)');
    await call('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:true});
    await screenshot('mobile-controls');
    await evaluate(`document.querySelector('article').scrollIntoView({block:'start'})`);await screenshot('mobile-proposal');
    await evaluate(`document.querySelector('.art-grid').scrollIntoView({block:'start'})`);await screenshot('mobile-art');
    assert(await evaluate('document.documentElement.scrollWidth<=window.innerWidth'));
    assert.deepEqual(errors,[]);
    console.log('PASS browser: all 14 cards/28 art panels; real download/import, rejected old/bad files, literal comments, reload persistence, filters, preference invalidation and mobile layout');
    console.log('Simulated fixture decisions are NOT user approval. HTML screenshots: '+output);
  }finally{
    fs.writeFileSync(path.join(output,'chromium.log'),log);
    fs.writeFileSync(path.join(output,'exceptions.json'),JSON.stringify(errors,null,2));
    if(socket)socket.close();chrome.kill('SIGTERM');
    await new Promise(resolve=>{if(chrome.exitCode!==null||chrome.signalCode!==null)resolve();else chrome.once('exit',resolve);});
    fs.rmSync(profile,{recursive:true,force:true});
  }
}
if(process.argv[2]==='--browser')browser(process.argv[3],process.argv[4]).catch(e=>{console.error(e);process.exitCode=1;});
