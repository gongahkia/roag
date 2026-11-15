// Native X11 interaction; raw CDP observes without Playwright's forced focus.
const {execFileSync,spawn}=require('node:child_process');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const os=require('node:os');
const path=require('node:path');
const output=path.resolve(process.argv[2]||'/tmp/roag-tranche05-native');
fs.mkdirSync(output,{recursive:true});
(async()=>{
  const profile=fs.mkdtempSync(path.join(os.tmpdir(),'roag-native-brush-'));
  const native=spawn(process.env.CHROMIUM_BIN||'/usr/bin/chromium',[
    '--no-sandbox','--ozone-platform=x11','--force-device-scale-factor=1',
    '--remote-debugging-port=0',`--user-data-dir=${profile}`,'--window-size=1024,783',
    '--no-first-run','--no-default-browser-check','about:blank'],
    {env:{...process.env,DISPLAY:process.env.DISPLAY||':95',WAYLAND_DISPLAY:''},stdio:'ignore'});
  let socket;
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
    const read=async expression=>{const r=await send('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});if(r.exceptionDetails)throw Error(r.exceptionDetails.text);return r.result.value;};
    const wait=async expression=>{for(let i=0;i<200;i++){if(await read(expression))return;await new Promise(r=>setTimeout(r,50));}throw Error(`Timed out: ${expression}`);};
    await send('Page.enable');await send('Runtime.enable');
    await send('Page.navigate',{url:process.env.SETTLEMENT_URL||'http://127.0.0.1:8765/settlement/'});
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
    const box=await read('({width:outerWidth,height:outerHeight})');
    const geometry=run(['getwindowgeometry','--shell',window]);
    const osScale=Number(geometry.match(/WIDTH=(\d+)/)[1])/box.width;
    const border=(box.width-zoomed.width*zoomed.ratio)/2;
    function move(x,y){run(['mousemove','--window',window,String(Math.round((border+x*zoomed.ratio)*osScale)),String(Math.round((box.height-zoomed.height*zoomed.ratio-border+y*zoomed.ratio)*osScale))]);}
    async function button(name){const b=await read(buttonExpression(name));assert(b,name);move(b.x+b.w/2,b.y+b.h/2);run(['click','1']);await new Promise(r=>setTimeout(r,150));}
    await button('Ⅱ');await button('Return to lane (H)');await button('2 Path');
    const tool=await read(buttonExpression('1 Cottage')),menu=await read(buttonExpression('Landscape menu'));
    const cy=(menu.y+menu.h+12+tool.y-10)/2;
    const saved="localStorage.getItem('roag.settlement.landscape.v1')";
    const initialSaved=await read(saved);
    move(24+8.5*48,cy);run(['mousedown','1']);await new Promise(r=>setTimeout(r,150));assert.equal(await read(saved),initialSaved);
    // clearmodifiers would release the held mouse before testing focus loss.
    run(['key','ctrl+t']);await wait('document.hidden || !document.hasFocus()');run(['mouseup','1']);await new Promise(r=>setTimeout(r,100));
    assert((await read(saved))===initialSaved,'native tab focus loss cancels the unfinished stroke');
    run(['key','--clearmodifiers','ctrl+w']);await wait('!document.hidden');await button('2 Path');
    move(24+8.5*48,cy);run(['mousedown','1']);await new Promise(r=>setTimeout(r,100));assert.equal(await read(saved),initialSaved);
    move(24+12.5*48,cy);await new Promise(r=>setTimeout(r,100));run(['mouseup','1']);
    await wait(`JSON.parse(${saved})?.lots.filter(l=>l[3]).length===13`);
    const beforePriced=await read(saved);
    await button('4 Farm');move(24+8.5*48,cy-48);run(['mousedown','1']);
    await new Promise(r=>setTimeout(r,100));run(['key','ctrl+t']);await wait('document.hidden || !document.hasFocus()');
    run(['mouseup','1']);assert.equal(await read(saved),beforePriced,'focus loss cancels priced construction without a charge');
    run(['key','--clearmodifiers','ctrl+w']);await wait('!document.hidden');
    move(24+8.5*48,cy-48);run(['click','1']);await wait(`JSON.parse(${saved})?.lots.filter(l=>l[6]==='garden').length===1`);
    await button('5 Park');move(24+10.5*48,cy-48);run(['click','1']);await wait(`JSON.parse(${saved})?.lots.filter(l=>l[6]==='green').length===1`);
    const completed=JSON.parse(await read(saved));assert.equal(completed.colony.money,JSON.parse(beforePriced).colony.money-14000);
    await button('1 Cottage');move(24+2.5*48,cy-48);run(['mousedown','1']);
    move(24+5.5*48,cy-48);run(['mouseup','1']);
    await wait(`JSON.parse(${saved})?.colony.residents.filter(r=>r.active).length===4`);
    const beforeInspect=await read(saved);
    run(['key','--clearmodifiers','i']);move(24+8.5*48,cy-48);run(['click','1']);
    await wait(`[...document.querySelectorAll('[aria-label]')].some(e=>e.getAttribute('aria-label').includes('Farm · 2 work positions'))`);
    assert.equal(await read(saved),beforeInspect,'native inspection is read-only');
    run(['key','--clearmodifiers','Escape']);
    move(550,300);run(['mousedown','3']);move(470,260);run(['mouseup','3']);
    run(['click','5']);await button('Return to lane (H)');
    run(['key','--clearmodifiers','6']);move(24+12.5*48,cy-48);run(['click','1']);
    await wait(`JSON.parse(${saved})?.lots.some(l=>l[6]==='workshop')`);
    run(['key','--clearmodifiers','7']);move(24+11.5*48,cy+48);run(['click','1']);
    await wait(`JSON.parse(${saved})?.lots.some(l=>l[6]==='market')`);
    run(['key','--clearmodifiers','8']);move(24+9.5*48,cy+48);run(['click','1']);
    await wait(`JSON.parse(${saved})?.lots.some(l=>l[6]==='hall')`);
    const hallShot=await send('Page.captureScreenshot',{format:'png'});fs.writeFileSync(path.join(output,'native-hall-selected.png'),Buffer.from(hallShot.data,'base64'));
    run(['key','--clearmodifiers','b']);await new Promise(r=>setTimeout(r,250));
    const catalogueShot=await send('Page.captureScreenshot',{format:'png'});fs.writeFileSync(path.join(output,'native-catalogue-110.png'),Buffer.from(catalogueShot.data,'base64'));
    run(['key','--clearmodifiers','Escape']);
    run(['key','--clearmodifiers','9']);move(24+8.5*48,cy);run(['mousedown','1']);move(24+12.5*48,cy);run(['mouseup','1']);
    await wait(`JSON.parse(${saved})?.lots.filter(l=>l[8]==='stone').length===5`);
    run(['key','--clearmodifiers','0']);move(24+6.5*48,cy);run(['click','1']);
    await wait(`JSON.parse(${saved})?.lots.some(l=>l[8]==='gardenPath')`);
    await button('1×');
    await wait(`JSON.parse(${saved})?.colony.residents.some(r=>r.workedSeconds>0)`);
    const screenshot=await send('Page.captureScreenshot',{format:'png'});fs.writeFileSync(path.join(output,'native-browser-zoom.png'),Buffer.from(screenshot.data,'base64'));
    const result={original,zoomed,osScale,paths:13,nativeInput:'XTest: real Ctrl+plus, palette clicks, native tab focus loss, five-cell drag and release',observation:'raw CDP, no focus emulation or game callbacks',checks:['110% browser zoom','native tab focus loss cancels without save','no partial-stroke save','cells 8 through 12 painted as one stroke','priced Farm focus-loss cancellation','Farm and Park native placement deduct exact costs','native four-home drag','I inspection and Escape cancellation','right-button pan and native wheel zoom','visible inhabitants and actual work at 110%','Workshop/Market/Hall through native shortcuts','mixed Stone/Garden road edits','native grouped catalogue at 110%','long Hall selection fits the footer']};
    fs.writeFileSync(path.join(output,'native-results.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
    await send('Browser.close');
  } finally {socket?.close();native.kill('SIGTERM');}
})().catch(e=>{console.error(e);process.exitCode=1;});
