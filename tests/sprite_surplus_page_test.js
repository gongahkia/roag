// Read-only catalogue regression checks. No LÖVE process or player data.
// Optional: node tests/sprite_surplus_page_test.js --browser /path/to/chrome /var/tmp/artifacts
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const os=require('node:os');
const crypto=require('node:crypto');
const {spawn}=require('node:child_process');
const {pathToFileURL}=require('node:url');
const folder='docs/sprite-audit/';
const html=fs.readFileSync(folder+'surplus.html','utf8');
const data=JSON.parse(fs.readFileSync(folder+'surplus.json','utf8'));
const inventory=JSON.parse(fs.readFileSync(folder+'inventory.json','utf8'));
const record=JSON.parse(fs.readFileSync(folder+'ideas-decisions.json','utf8'));
const reviewed=record.review_inventory?JSON.parse(fs.readFileSync(record.review_inventory.path,'utf8')):inventory;
assert.deepEqual(JSON.parse(html.match(/<script id="surplus-data" type="application\/json">([^<]+)<\/script>/)[1]),data);
assert.equal(data.mapping_checksum,reviewed.mapping_checksum);
assert.deepEqual(data.loaded_sheets.onebit,['onebit']);
for(const [name,s] of Object.entries(data.sources)) {
  const embedded=html.match(new RegExp('\\.sprite\\.'+name+'\\{background-image:url\\(data:image/png;base64,([^)]*)\\)'));
  const original=fs.readFileSync(s.path);
  assert(Buffer.from(embedded[1],'base64').equals(original));
  assert.equal(crypto.createHash('sha256').update(original).digest('hex'),s.sha256);
}
const catalogue=fs.readFileSync(folder+'index.html','utf8');
for(const card of data.cards)for(const pack of ['kenney','onebit'])for(const group of card[pack]) {
  assert(data.loaded_sheets[pack].includes(group.sheet));
  for(const index of group.indices) {
    assert(catalogue.includes(`id="cell-${group.sheet}-${index}"`));
    for(const p of Object.values(reviewed.packs))assert(!p.usage[group.sheet]?.[index]);
  }
}
console.log('PASS embedded sources, unchanged native PNG bytes, current mappings and every candidate link');

async function browser(binary,output) {
  fs.mkdirSync(output,{recursive:true});
  const profile=fs.mkdtempSync(path.join(os.tmpdir(),'roag-surplus-browser-'));
  const chrome=spawn(binary,['--headless=new','--no-sandbox','--disable-gpu',
    '--disable-dev-shm-usage','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],
    {stdio:['ignore','ignore','pipe']});
  let socket,serial=0;const pending=new Map(),errors=[];
  const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));
  function call(method,params={}) {
    return new Promise((resolve,reject)=>{
      const id=++serial;pending.set(id,{resolve,reject});socket.send(JSON.stringify({id,method,params}));
    });
  }
  async function evaluate(expression) {
    const r=await call('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});
    assert(!r.exceptionDetails,JSON.stringify(r.exceptionDetails));return r.result.value;
  }
  async function until(fn) {
    for(let i=0;i<100;i++){if(await fn())return;await delay(100);}
    throw Error('Browser check timed out');
  }
  async function screenshot(name) {
    const r=await call('Page.captureScreenshot',{format:'png',captureBeyondViewport:false});
    fs.writeFileSync(path.join(output,name+'.png'),Buffer.from(r.data,'base64'));
  }
  try {
    const endpoint=await new Promise((resolve,reject)=>{
      let log='';const timer=setTimeout(()=>reject(Error('Chromium did not start: '+log)),15000);
      chrome.stderr.on('data',bytes=>{log+=bytes;const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);
        if(m){clearTimeout(timer);resolve(m[1]);}});
      chrome.on('error',reject);
      chrome.on('exit',code=>{if(code)reject(Error('Chromium exited '+code+': '+log));});
    });
    const address=new URL(endpoint);
    const targets=await(await fetch('http://'+address.host+'/json/list')).json();
    socket=new WebSocket(targets.find(t=>t.type==='page').webSocketDebuggerUrl);
    await new Promise((resolve,reject)=>{socket.onopen=resolve;socket.onerror=reject;});
    socket.onmessage=event=>{
      const m=JSON.parse(event.data);
      if(m.id){const p=pending.get(m.id);pending.delete(m.id);
        if(m.error)p.reject(Error(JSON.stringify(m.error)));else p.resolve(m.result);}
      if(m.method==='Runtime.exceptionThrown')errors.push(m.params.exceptionDetails);
    };
    await call('Page.enable');await call('Runtime.enable');
    await call('Emulation.setDeviceMetricsOverride',{width:1400,height:1100,deviceScaleFactor:1,mobile:false});
    const navigate=async name=>{
      await call('Page.navigate',{url:pathToFileURL(path.resolve(folder+name)).href});
      await until(()=>evaluate(`document.readyState==='complete' && !!document.querySelector('#count')`));
    };
    await navigate('index.html');
    const packCounts={};
    for(const pack of ['union','kenney','onebit']){
      const sheets=inventory.pixel_inventory.filter(s=>pack!=='onebit'||s.sheet==='onebit');
      const all=sheets.reduce((n,s)=>n+s.grid,0),used=sheets.reduce((n,s)=>n+s[pack],0);
      const empty=sheets.reduce((n,s)=>n+s.empty,0);
      packCounts[pack]={all,used,empty,unused:all-empty-used};
    }
    for(const [pack,counts] of Object.entries(packCounts)) {
      for(const [show,count] of Object.entries(counts)) {
        const visible=await evaluate(`(()=>{document.querySelector('#pack').value='${pack}';
          const f=document.querySelector('#filter');f.value='${show}';f.dispatchEvent(new Event('change'));
          return [...document.querySelectorAll('article')].filter(a=>!a.hidden).length})()`);
        assert.equal(visible,count,`${pack} / ${show}`);
        if(pack==='onebit')assert.deepEqual(await evaluate(`Array.from(document.querySelectorAll('section[data-sheet]')).filter(s=>!s.hidden).map(s=>s.dataset.sheet)`),['onebit']);
      }
    }
    await evaluate(`document.querySelector('#filter').value='unused';filter()`);
    await screenshot('standalone-unused');
    await navigate('surplus.html');
    assert.equal(await evaluate(`document.querySelectorAll('article:not([hidden])').length`),10);
    await screenshot('surplus-overview');
    for(const [kind,count] of Object.entries({visual:5,mechanic:1,enemy:3,expansion:1})) {
      assert.equal(await evaluate(`(()=>{const k=document.querySelector('#kind');k.value='${kind}';
        k.dispatchEvent(new Event('input'));return document.querySelectorAll('article:not([hidden])').length})()`),count);
    }
    await evaluate(`document.querySelector('#kind').value='all';document.querySelector('#pack').value='onebit';
      document.querySelector('#search').value='finite reanimator';filter()`);
    assert.equal(await evaluate(`document.querySelectorAll('article:not([hidden])').length`),1);
    assert.equal(await evaluate(`document.querySelectorAll('.art:not([hidden])[data-pack="kenney"]').length`),0);
    await screenshot('reanimator-onebit');
    await evaluate(`document.querySelector('#search').value='';document.querySelector('#pack').value='both';filter();
      document.querySelector('#distinct-existing-bodies').scrollIntoView()`);
    await screenshot('spare-figures');
    for(const card of data.cards) {
      await evaluate(`document.getElementById(${JSON.stringify(card.id)}).scrollIntoView()`);
      await screenshot('idea-'+card.id);
    }
    await call('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:true});
    await evaluate('window.scrollTo(0,0)');await screenshot('surplus-mobile');
    assert(await evaluate('document.documentElement.scrollWidth<=window.innerWidth'));
    assert.deepEqual(errors,[]);
    console.log('PASS browser: 12 pack/filter cases, loaded-sheet restriction, shortlist/search, mobile, no JS errors');
    console.log('Screenshots: '+path.resolve(output));
  } finally {
    if(socket)socket.close();chrome.kill('SIGTERM');
    await new Promise(resolve=>{if(chrome.exitCode!==null||chrome.signalCode!==null)resolve();else chrome.once('exit',resolve);});
    fs.rmSync(profile,{recursive:true,force:true});
  }
}
if(process.argv[2]==='--browser')browser(process.argv[3],process.argv[4]).catch(error=>{console.error(error);process.exitCode=1;});
