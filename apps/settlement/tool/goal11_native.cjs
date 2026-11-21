// Native X11 interaction; raw CDP observes without Playwright's forced focus.
const {execFileSync,spawn}=require('node:child_process');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const os=require('node:os');
const path=require('node:path');
const output=path.resolve(process.argv[2]||'/var/tmp/roag-goal11/native');
fs.mkdirSync(output,{recursive:true});
(async()=>{
  const profile=fs.mkdtempSync(path.join(os.tmpdir(),'roag-native-dialogue-'));
  const native=spawn(process.env.CHROMIUM_BIN||'/usr/bin/chromium',[
    '--no-sandbox','--ozone-platform=x11','--force-device-scale-factor=1',
    '--remote-debugging-port=0',`--user-data-dir=${profile}`,'--window-size=1024,783',
    '--no-first-run','--no-default-browser-check','about:blank'],
    {env:{...process.env,DISPLAY:process.env.DISPLAY||':97',WAYLAND_DISPLAY:''},stdio:'ignore'});
  let socket, observe;
  try {
    const portFile=path.join(profile,'DevToolsActivePort');
    let port;
    for(let i=0;i<200;i++){
      const value=fs.existsSync(portFile)?fs.readFileSync(portFile,'utf8').split('\n')[0]:'';
      if(/^\d+$/.test(value) && Number(value)>0){port=value;break;}
      await new Promise(r=>setTimeout(r,25));
    }
    assert(port,'native browser published its debugging port');
    const pages=await(await fetch(`http://127.0.0.1:${port}/json/list`)).json();
    socket=new WebSocket(pages.find(p=>p.type==='page').webSocketDebuggerUrl);
    await new Promise((resolve,reject)=>{socket.addEventListener('open',resolve,{once:true});socket.addEventListener('error',reject,{once:true});});
    let id=0;const pending=new Map();
    socket.addEventListener('message',event=>{const m=JSON.parse(event.data),p=pending.get(m.id);if(p){pending.delete(m.id);m.error?p.reject(Error(m.error.message)):p.resolve(m.result);}});
    const send=(method,params={})=>new Promise((resolve,reject)=>{pending.set(++id,{resolve,reject});socket.send(JSON.stringify({id,method,params}));});
    observe=send;
    const read=async expression=>{const r=await send('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});if(r.exceptionDetails)throw Error(r.exceptionDetails.text);return r.result.value;};
    const wait=async expression=>{for(let i=0;i<1800;i++){if(await read(expression))return;await new Promise(r=>setTimeout(r,50));}throw Error(`Timed out: ${expression}`);};
    await send('Page.enable');await send('Runtime.enable');
    const fixture=fs.readFileSync(process.env.SETTLEMENT_NORMAL_EXPORT||'/var/tmp/roag-goal11/fixtures/three-biome-trade.json','utf8');
    await send('Page.addScriptToEvaluateOnNewDocument',{source:'if(!localStorage.getItem("roag.settlement.landscape.v1"))localStorage.setItem("roag.settlement.landscape.v1",'+JSON.stringify(fixture)+')'});
    await send('Page.navigate',{url:process.env.SETTLEMENT_URL||'http://127.0.0.1:8768/settlement/'});
    await wait("document.readyState==='complete' && !!window.settlementPlatform && !document.getElementById('loading')");
    const run=args=>execFileSync(process.env.XDOTOOL_BIN||'xdotool',args,{encoding:'utf8'});
    const window=run(['search','--onlyvisible','--name','ROAG']).trim().split('\n')[0];
    run(['windowfocus','--sync',window]);
    // accessibility setup only; all game commands below are native input.
    await read("document.querySelector('flt-semantics-placeholder')?.focus()");
    await send('Input.dispatchKeyEvent',{type:'keyDown',key:'Enter',code:'Enter',text:'\r',windowsVirtualKeyCode:13});
    await send('Input.dispatchKeyEvent',{type:'keyUp',key:'Enter',code:'Enter',windowsVirtualKeyCode:13});
    const buttonExpression=name=>`(()=>{const b=[...document.querySelectorAll('[role=button]')].find(b=>(b.getAttribute('aria-label')||b.textContent).split(' ').join('').startsWith(${JSON.stringify(name.split(' ').join(''))}));if(!b)return null;const r=b.getBoundingClientRect();return{x:r.x,y:r.y,w:r.width,h:r.height};})()`;
    await wait(buttonExpression('2 Path'));


    const original=await read('({ratio:devicePixelRatio,width:innerWidth,height:innerHeight})');
    run(['key','--clearmodifiers','ctrl+plus']);await wait(`devicePixelRatio>${original.ratio}`);
    const zoomed=await read('({ratio:devicePixelRatio,width:innerWidth,height:innerHeight})');
    assert(Math.abs(zoomed.ratio/original.ratio-1.1)<.02);
    const box=await read('({width:outerWidth,height:outerHeight})'),geometry=run(['getwindowgeometry','--shell',window]);
    const osScale=Number(geometry.match(/WIDTH=(\d+)/)[1])/box.width,border=(box.width-zoomed.width*zoomed.ratio)/2;
    function move(x,y){run(['mousemove','--window',window,String(Math.round((border+x*zoomed.ratio)*osScale)),String(Math.round((box.height-zoomed.height*zoomed.ratio-border+y*zoomed.ratio)*osScale))]);}
    async function button(name){await wait(buttonExpression(name));const b=await read(buttonExpression(name));move(b.x+b.w/2,b.y+b.h/2);run(['click','1']);await new Promise(r=>setTimeout(r,180));}
    async function shot(name){const r=await send('Page.captureScreenshot',{format:'png'});fs.writeFileSync(path.join(output,name+'.png'),Buffer.from(r.data,'base64'));}
    const state=async()=>JSON.parse(await read('window.settlementPlatform.load()'));
    const before=await state();await button('Ⅱ');run(['key','--clearmodifiers','w']);await wait(buttonExpression('Back to settlement'));await shot('native-regional-map-110');
    const site=await read("(()=>{const r=document.querySelector('[flt-semantics-identifier=\"regional-site-01\"]').getBoundingClientRect();return{x:r.x,y:r.y,w:r.width,h:r.height}})()");
    move(site.x+site.w/2,site.y+site.h/2);run(['click','1']);await button('Visit settlement');await wait("JSON.parse(window.settlementPlatform.load()).active==='site-01'");await button('Return to lane (H)');await shot('native-town-switch-110');
    run(['key','--clearmodifiers','Right']);await button('Zoom out');await button('Return to lane (H)');run(['key','--clearmodifiers','w']);await wait(buttonExpression('Back to settlement'));run(['key','--clearmodifiers','Escape']);
    const after=await state();assert.equal(after.clock,before.clock);assert.equal(after.towns.length,before.towns.length);
    for(const t of before.towns){const other=after.towns.find(x=>x.site===t.site);assert.deepEqual(other.colony,t.colony);}
    run(['key','--clearmodifiers','ctrl+0']);await wait(`devicePixelRatio===${original.ratio}`);
    fs.writeFileSync(path.join(output,'results.json'),JSON.stringify({input:'Native XTest pointer and keyboard in isolated headful Chromium/Xvfb. CDP navigation, initial public fixture and read-only observation/accessibility only.',original,zoomed,checks:['Real 110% browser zoom, not CSS resize','Native W opens regional map','Native mouse selects and visits another town','Native pan/zoom/home and Escape','Switch preserves every paused Colony state and clock','Ctrl+0 restores browser zoom']},null,2));
  } finally {socket?.close();native.kill('SIGTERM');}
})().catch(e=>{console.error(e);process.exitCode=1});
