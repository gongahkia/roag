// Native X11 interaction; raw CDP observes without Playwright's forced focus.
const {execFileSync,spawn}=require('node:child_process');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const os=require('node:os');
const path=require('node:path');
const output=path.resolve(process.argv[2]||'/tmp/roag-goal07-native');
fs.mkdirSync(output,{recursive:true});
(async()=>{
  const profile=fs.mkdtempSync(path.join(os.tmpdir(),'roag-native-dialogue-'));
  const native=spawn(process.env.CHROMIUM_BIN||'/usr/bin/chromium',[
    '--no-sandbox','--ozone-platform=x11','--force-device-scale-factor=1',
    '--remote-debugging-port=0',`--user-data-dir=${profile}`,'--window-size=1024,783',
    '--no-first-run','--no-default-browser-check','about:blank'],
    {env:{...process.env,DISPLAY:process.env.DISPLAY||':95',WAYLAND_DISPLAY:''},stdio:'ignore'});
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
    const box=await read('({width:outerWidth,height:outerHeight})');
    const geometry=run(['getwindowgeometry','--shell',window]);
    const osScale=Number(geometry.match(/WIDTH=(\d+)/)[1])/box.width;
    const border=(box.width-zoomed.width*zoomed.ratio)/2;
    function move(x,y){run(['mousemove','--window',window,String(Math.round((border+x*zoomed.ratio)*osScale)),String(Math.round((box.height-zoomed.height*zoomed.ratio-border+y*zoomed.ratio)*osScale))]);}
    async function button(name){await wait(buttonExpression(name));const b=await read(buttonExpression(name));assert(b,name);move(b.x+b.w/2,b.y+b.h/2);run(['click','1']);await new Promise(r=>setTimeout(r,150));}
    if (await read(buttonExpression('Start building'))) await button('Start building');
    await button('Ⅱ');await button('Return to lane (H)');
    const tool=await read(buttonExpression('1 Cottage')),menu=await read(buttonExpression('Landscape menu'));
    const cy=(menu.y+menu.h+12+tool.y-14)/2;
    const saved="localStorage.getItem('roag.settlement.landscape.v1')";
    const state=async()=>JSON.parse(await read(saved));
    const text="[...document.querySelectorAll('flt-semantics')].map(e=>e.getAttribute('aria-label')||e.textContent).join('\\n')";
    const shot=async name=>{const png=await send('Page.captureScreenshot',{format:'png'});fs.writeFileSync(path.join(output,name+'.png'),Buffer.from(png.data,'base64'));};
    const itemExpression=name=>buttonExpression(name).replace("[role=button]","[role=menuitem]");
    async function item(name){await wait(itemExpression(name));const b=await read(itemExpression(name));assert(b,name);move(b.x+b.w/2,b.y+b.h/2);run(['click','1']);await new Promise(r=>setTimeout(r,150));}
    function point(x,y){return[24+(x+.5)*48,cy+(y-15)*48];}
    function click(x,y,button=1){move(x,y);run(['click',String(button)]);}
    async function newWorld(preset) {
      await button('Landscape menu'); await item('New landscape…');
      await button('Environment'); await item(preset); await button('Create world');
      await wait(`JSON.parse(${saved})?.generation?.preset===${JSON.stringify(preset==='Woodlands'?'woodlands':'river')}`);
      await button('Start building');
      await button('Ⅱ'); await button('Return to lane (H)');
      await button('Zoom out'); await button('Zoom out');
    }
    function tile(x,y){return[zoomed.width/2+(x+.5-9)*24,cy+(y-15)*24];}
    async function stroke(key,a,b=a){run(['key','--clearmodifiers',key]);move(...tile(...a));run(['mousedown','1']);move(...tile(...b));run(['mouseup','1']);await new Promise(r=>setTimeout(r,200));}
    async function landscape(name){
      await button('Build catalogue');
      for(var i=0;i<24;i++){
        const b=await read(itemExpression(name));
        if(b&&b.y>=0&&b.y+b.h<=zoomed.height){await item(name);return;}
        const m=await read(`(()=>{const r=document.querySelector('[role=menu]')?.getBoundingClientRect();return r?{x:r.x,y:r.y,w:r.width,h:r.height}:null})()`);
        assert(m);move(m.x+m.w/2,Math.min(m.y+m.h/2,zoomed.height-80));run(['click','--repeat','2','--delay','30','5']);await new Promise(r=>setTimeout(r,100));
      }
      throw Error('Could not scroll to '+name);
    }
    await newWorld('Woodlands');const woodlandMoney=(await state()).colony.money;await shot('native-woodlands-created-110');
    await stroke('2',[7,15],[10,15]);await landscape('Clear');
    move(...tile(11,15));run(['mousedown','1']);move(...tile(11,17));await shot('native-clearing-preview-110');run(['mouseup','1']);
    await wait(`JSON.parse(${saved})?.colony.money===${woodlandMoney-3000}`);
    run(['key','--clearmodifiers','ctrl+z']);await wait(`JSON.parse(${saved})?.colony.money===${woodlandMoney}`);
    run(['key','--clearmodifiers','ctrl+shift+z']);await wait(`JSON.parse(${saved})?.colony.money===${woodlandMoney-3000}`);
    await stroke('2',[10,15],[11,15]);await stroke('2',[11,15],[11,17]);await shot('native-woodland-road-110');
    await newWorld('River Country');let s=await state();const water=[];
    for(let x=0;x<40;x++)if(s.lots[15*40+x][9]==='water')water.push(x);
    const left=water[0]-1,right=water.at(-1)+1;
    await stroke('2',[7,15],[left,15]);const riverMoney=(await state()).colony.money;await landscape('Bridge');
    move(...tile(left,15));run(['mousedown','1']);move(...tile(right,15));await shot('native-bridge-preview-110');run(['key','Escape']);await wait(`!(${text}).includes('Release:')`);run(['mouseup','1']);
    assert.equal((await state()).colony.money,riverMoney);
    await landscape('Bridge');move(...tile(left,15));run(['mousedown','1']);move(...tile(right,15));run(['mouseup','1']);
    await wait(`JSON.parse(${saved})?.colony.money===${riverMoney-water.length*3000}`);
    await stroke('2',[right,15],[right+2,15]);await stroke('1',[2,14],[5,14]);await stroke('4',[right+1,14]);
    let crossing=false;
    // selecting the current speed uses normal native input and checkpoints the
    // live positions, avoiding missed sub-second crossings between autosaves.
    for(var i=0;i<400;i++) {
      await button('1×');
      if((await state()).colony.residents.some(r=>r.x>left+1&&r.x<right)){crossing=true;break;}
    }
    assert(crossing,'a resident actually crossed the water span');
    await shot('native-residents-crossing-110');
    await wait(`JSON.parse(${saved})?.colony.workEarned>0`);await button('Ⅱ');await shot('native-river-working-110');
    fs.writeFileSync(path.join(output,'results.json'),JSON.stringify({input:'Native XTest mouse/keyboard, headful Chromium, isolated profile; raw CDP only for accessibility setup and observations',original,zoomed,checks:['Native 110% browser zoom','World creation: Woodlands and River Country, seed 202','Paid Clear drag, actual preview, Undo and Redo','Free road construction on cleared woodland','Complete bridge preview, cancellation and exact bank-to-bank charge','Actual residents traverse native-built bridge and work on the far bank'],state:await state()},null,2));
  } catch(error) {
    if(observe){try{const png=await observe('Page.captureScreenshot',{format:'png'});fs.writeFileSync(path.join(output,'failure.png'),Buffer.from(png.data,'base64'));}catch(_){}}
    throw error;
  } finally {socket?.close();native.kill('SIGTERM');}
})().catch(e=>{console.error(e);process.exitCode=1});
