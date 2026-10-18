// Headless review checks. Optional real-browser checks use an isolated profile.
// node tests/enemy_review_page_test.js --browser /path/to/chrome /var/tmp/artifacts
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const crypto=require('node:crypto');
const os=require('node:os');
const {spawn}=require('node:child_process');
const {pathToFileURL}=require('node:url');
const API=require('../tools/enemy_review_client.js');
const html=fs.readFileSync('docs/sprite-audit/enemy-review.html','utf8');
const data=JSON.parse(html.match(/<script id="enemy-data" type="application\/json">([^<]+)<\/script>/)[1]);
const copy=value=>JSON.parse(JSON.stringify(value));
let passed=0;
function test(name,fn){fn();passed++;console.log('PASS '+name);}
test('all five combinations and ten roles have bounds-checked, unbound native candidates',()=>{
  assert.equal(data.combos.length,5);assert.equal(data.roles.length,10);
  assert.equal(data.audit.requirements,289);
  assert.equal(Object.keys(data.sources).length,5);
  for(const role of data.roles)for(const pack of ['kenney','onebit']) {
    for(const candidate of role.candidates[pack]) {
      const s=data.sources[candidate.sheet];
      assert(candidate.nontransparent_pixels>5);
      assert(candidate.index%s.columns*s.stride+s.tile<=s.width);
      assert(Math.floor(candidate.index/s.columns)*s.stride+s.tile<=s.height);
      assert(Object.values(candidate.runtime_uses).every(ids=>Object.keys(ids).length===0));
      if(pack==='onebit')assert.equal(candidate.sheet,'onebit');
    }
  }
});
test('the five embedded PNGs exactly match checked source files',()=>{
  for(const s of Object.values(data.sources)) {
    const bytes=fs.readFileSync(s.path);
    assert(Buffer.from(s.image.split(',')[1],'base64').equals(bytes));
    assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),s.sha256);
  }
});
test('defaults select proposed art but authorize none of thirty decisions',()=>{
  const state=API.blank(data);
  assert.deepEqual(API.progress(state),{total:30,reviewed:0,approved:0,rejected:0,discussed:0});
  assert.deepEqual(API.checked(data,state),state);
  for(const p of ['kenney','onebit'])assert.equal(new Set(state.roles.map(r=>r.art[p].choice)).size,10);
});
test('both packs, cue choices and literal comments survive exact feedback roundtrip',()=>{
  const state=API.blank(data);
  state.combos[0].vote='discuss';state.combos[0].cue.vote='reject';
  state.roles[0].art.kenney.choice='tiny:84';state.roles[0].art.kenney.vote='approve';
  state.roles[0].art.kenney.comment='<img src=x onerror=alert(1)> keep as literal text';
  state.roles[0].art.onebit.vote='reject';
  assert.deepEqual(API.checked(data,JSON.parse(JSON.stringify(state))),state);
  assert.equal(API.progress(state).reviewed,4);
  assert.equal(state.roles[0].art.onebit.choice,'onebit:318');
});
test('unknown IDs, duplicate records, wrong candidates and incompatible feedback reject cleanly',()=>{
  const state=API.blank(data),unchanged=copy(state);
  for(const mutate of [s=>s.schema='roag-room-review/1',s=>s.proposal_fingerprint='old',
    s=>s.audit_mapping_checksum='old',s=>s.combos[1].id=s.combos[0].id,
    s=>s.roles[0].id='unexpected',s=>s.roles.pop(),
    s=>s.roles[0].art.kenney.choice='tiny:111',s=>s.combos[0].vote='approved',
    s=>s.combos[0].comment='x'.repeat(4001)]) {
    const bad=copy(state);mutate(bad);assert.throws(()=>API.checked(data,bad));
    assert.deepEqual(state,unchanged);
  }
});
console.log(`${passed} review data/state checks passed; no LÖVE initialized`);

async function browser(binary,output) {
  fs.mkdirSync(output,{recursive:true});
  const profile=fs.mkdtempSync(path.join(os.tmpdir(),'roag-enemy-browser-'));
  const chrome=spawn(binary,['--headless=new','--no-sandbox','--disable-gpu',
    '--disable-dev-shm-usage','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],
    {stdio:['ignore','ignore','pipe']});
  let socket,serial=0;const pending=new Map(),errors=[];
  function call(method,params={}) {
    return new Promise((resolve,reject)=>{
      const id=++serial;pending.set(id,{resolve,reject});socket.send(JSON.stringify({id,method,params}));
    });
  }
  const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));
  const evaluate=async expression=>{
    const r=await call('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});
    if(r.exceptionDetails)throw Error(r.exceptionDetails.text+' '+JSON.stringify(r.exceptionDetails.exception));
    return r.result.value;
  };
  async function until(fn) {
    for(let i=0;i<100;i++){if(await fn())return;await delay(100);}
    throw Error('Browser check timed out');
  }
  try {
    const endpoint=await new Promise((resolve,reject)=>{
      let log='';const timer=setTimeout(()=>reject(Error('Chromium did not start: '+log)),15000);
      chrome.stderr.on('data',bytes=>{log+=bytes;
        const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);
        if(m){clearTimeout(timer);resolve(m[1]);}
      });
      chrome.on('error',reject);chrome.on('exit',code=>{if(code)reject(Error('Chromium exited '+code+': '+log));});
    });
    const address=new URL(endpoint);
    const targets=await(await fetch('http://'+address.host+'/json/list')).json();
    socket=new WebSocket(targets.find(t=>t.type==='page').webSocketDebuggerUrl);
    await new Promise((resolve,reject)=>{socket.onopen=resolve;socket.onerror=reject;});
    socket.onmessage=event=>{
      const message=JSON.parse(event.data);
      if(message.id){const p=pending.get(message.id);pending.delete(message.id);
        if(message.error)p.reject(Error(JSON.stringify(message.error)));else p.resolve(message.result);}
      if(message.method==='Runtime.exceptionThrown')errors.push(message.params.exceptionDetails);
    };
    await call('Page.enable');await call('Runtime.enable');
    await call('Browser.setDownloadBehavior',{behavior:'allow',downloadPath:path.resolve(output)});
    await call('Emulation.setDeviceMetricsOverride',{width:1400,height:1100,deviceScaleFactor:1,mobile:false});
    await call('Page.navigate',{url:pathToFileURL(path.resolve('docs/sprite-audit/enemy-review.html')).href});
    await until(()=>evaluate(`document.readyState==='complete' && !!document.querySelector('#progress') && document.querySelector('#progress').textContent.includes('0/30')`));
    await until(()=>evaluate(`(()=>{const c=document.querySelector('canvas.sprite');return c&&c.getContext('2d').getImageData(0,0,80,80).data.some((v,i)=>i%4===3&&v>0)})()`));
    async function screenshot(name) {
      const r=await call('Page.captureScreenshot',{format:'png',captureBeyondViewport:false});
      fs.writeFileSync(path.join(output,name+'.png'),Buffer.from(r.data,'base64'));
    }
    const click=selector=>evaluate(`document.querySelector(${JSON.stringify(selector)}).click()`);
    await screenshot('overview');
    await click('[data-action="vote"][data-scope="combo"][data-value="approve"]');
    await click('[data-action="choose"][data-id="ward_weaver"][data-pack="kenney"][data-choice="tiny:84"]');
    await click('[data-action="vote"][data-id="ward_weaver"][data-pack="kenney"][data-value="approve"]');
    await click('[data-action="vote"][data-id="ward_weaver"][data-pack="onebit"][data-value="discuss"]');
    const comment='Prefer the purple wizard; <script>must stay literal</script>';
    await evaluate(`(()=>{const t=document.querySelector('textarea[data-id="ward_weaver"][data-pack="kenney"]');t.value=${JSON.stringify(comment)};t.dispatchEvent(new Event('input',{bubbles:true}))})()`);
    await click('[data-combo="healer_rushwings"]');await click('[data-combo="warded_bodyguards"]');
    assert.equal(await evaluate(`document.querySelector('textarea[data-id="ward_weaver"][data-pack="kenney"]').value`),comment);
    await evaluate(`document.getElementById('role-ward_weaver').scrollIntoView()`);await screenshot('wizard-art');
    await click('[data-action="choose"][data-id="ward_weaver"][data-pack="kenney"][data-choice="tiny:100"]');
    assert.equal(await evaluate(`document.querySelector('[data-action="vote"][data-id="ward_weaver"][data-pack="kenney"][data-value="approve"]').getAttribute('aria-pressed')`),'false');
    await click('[data-action="vote"][data-id="ward_weaver"][data-pack="kenney"][data-value="approve"]');
    await call('Page.reload');
    await until(()=>evaluate(`!!document.querySelector('#progress')&&document.querySelector('#progress').textContent.includes('3/30')`));
    assert.equal(await evaluate(`document.querySelector('textarea[data-id="ward_weaver"][data-pack="kenney"]').value`),comment);
    await click('#export');
    const feedbackFile=path.join(output,'roag-enemy-combo-feedback.json');
    await until(()=>fs.existsSync(feedbackFile));
    const feedback=JSON.parse(fs.readFileSync(feedbackFile));
    assert.equal(API.progress(API.checked(data,feedback)).reviewed,3);
    const invalid=copy(feedback);invalid.proposal_fingerprint='old';
    const badFile=path.join(output,'incompatible.json');fs.writeFileSync(badFile,JSON.stringify(invalid));
    async function importFile(file) {
      const d=await call('DOM.getDocument');
      const n=await call('DOM.querySelector',{nodeId:d.root.nodeId,selector:'#import-file'});
      await call('DOM.setFileInputFiles',{nodeId:n.nodeId,files:[path.resolve(file)]});
    }
    await importFile(badFile);
    await until(()=>evaluate(`document.querySelector('#message').textContent.startsWith('Import rejected:')`));
    assert((await evaluate(`document.querySelector('#progress').textContent`)).includes('3/30'));
    await importFile(feedbackFile);
    await until(()=>evaluate(`document.querySelector('#message').textContent.startsWith('Feedback imported.')`));
    for(const combo of data.combos) {
      await click(`[data-combo="${combo.id}"]`);
      assert.equal(await evaluate(`document.querySelectorAll('.role').length`),2);
      assert.equal(await evaluate(`document.querySelectorAll('canvas.formation').length`),2);
      await evaluate(`document.querySelector('#content').scrollIntoView()`);
      await screenshot(combo.id);
    }
    await call('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:true});
    await evaluate('window.scrollTo(0,0)');
    assert(await evaluate('document.documentElement.scrollWidth<=window.innerWidth+1'),'mobile horizontal overflow');
    await screenshot('mobile');
    await evaluate(`(()=>{Object.defineProperty(window,'localStorage',{configurable:true,get(){throw Error('blocked storage')}});return true})()`);
    await click('[data-action="vote"][data-scope="combo"][data-value="discuss"]');
    assert((await evaluate(`document.querySelector('#storage').textContent`)).includes('storage unavailable'));
    assert.equal(errors.length,0,JSON.stringify(errors));
    console.log('PASS Chromium: offline sprites, five groups, independent votes, comments, choice invalidation, reopen, export/import, bad import, storage fallback and mobile');
    console.log('Browser artifacts:',output);
  } finally {
    if(socket)socket.close();chrome.kill('SIGTERM');
    await delay(200);if(chrome.exitCode===null)chrome.kill('SIGKILL');
    fs.rmSync(profile,{recursive:true,force:true});
  }
}
if(process.argv.includes('--browser')) {
  const at=process.argv.indexOf('--browser');
  const binary=process.argv[at+1];
  assert(binary,'Supply an explicit Chromium executable');
  const output=process.argv[at+2]||fs.mkdtempSync('/var/tmp/roag-enemy-review-');
  browser(binary,output).catch(error=>{console.error(error);process.exitCode=1;});
}
