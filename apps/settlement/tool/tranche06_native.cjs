// Native X11 interaction; raw CDP observes without Playwright's forced focus.
const {execFileSync,spawn}=require('node:child_process');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const os=require('node:os');
const path=require('node:path');
const output=path.resolve(process.argv[2]||'/tmp/roag-tranche06-native');
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
    const wait=async expression=>{for(let i=0;i<200;i++){if(await read(expression))return;await new Promise(r=>setTimeout(r,50));}throw Error(`Timed out: ${expression}`);};
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
    await button('Ⅱ');await button('Return to lane (H)');
    const tool=await read(buttonExpression('1 Cottage')),menu=await read(buttonExpression('Landscape menu'));
    const cy=(menu.y+menu.h+12+tool.y-14)/2;
    const saved="localStorage.getItem('roag.settlement.landscape.v1')";
    const state=async()=>JSON.parse(await read(saved));
    const text="[...document.querySelectorAll('flt-semantics')].map(e=>e.getAttribute('aria-label')||e.textContent).join('\\n')";
    const shot=async name=>{const png=await send('Page.captureScreenshot',{format:'png'});fs.writeFileSync(path.join(output,name+'.png'),Buffer.from(png.data,'base64'));};
    const itemExpression=name=>buttonExpression(name).replace("[role=button]","[role=menuitem]");
    async function item(name){const b=await read(itemExpression(name));assert(b,name);move(b.x+b.w/2,b.y+b.h/2);run(['click','1']);await new Promise(r=>setTimeout(r,150));}
    function point(x,y){return[24+(x+.5)*48,cy+(y-15)*48];}
    function click(x,y,button=1){move(x,y);run(['click',String(button)]);}
    await button('1 Cottage');move(...point(2,14));run(['mousedown','1']);move(...point(5,14));run(['mouseup','1']);
    await wait(`JSON.parse(${saved})?.colony.residents.filter(r=>r.active).length===4`);
    const beforeMove=await state(),owner=beforeMove.colony.residents[0],source=point(2,14);source[1]-=14;
    run(['key','--clearmodifiers','i']);click(...source);await wait(buttonExpression('Move building'));
    await button('Move building');move(...point(6,14));await new Promise(r=>setTimeout(r,300));await shot('native-move-ghost-110');
    assert.deepEqual(await state(),beforeMove);run(['key','--clearmodifiers','Escape']);assert.deepEqual(await state(),beforeMove);
    run(['key','--clearmodifiers','i']);click(...source);await wait(buttonExpression('Move building'));run(['key','--clearmodifiers','m']);click(...point(6,14));
    await wait(`JSON.parse(${saved})?.lots[14*40+6][5]===${JSON.stringify(owner.id)}`);
    let s=await state();assert.equal(s.colony.money,beforeMove.colony.money);assert.equal(s.colony.residents.length,4);assert.equal(s.nextCottageId,beforeMove.nextCottageId);
    run(['key','--clearmodifiers','ctrl+z']);await wait(`JSON.parse(${saved})?.lots[14*40+2][5]===${JSON.stringify(owner.id)}`);
    run(['key','--clearmodifiers','ctrl+shift+z']);await wait(`JSON.parse(${saved})?.lots[14*40+6][5]===${JSON.stringify(owner.id)}`);
    click(...point(6,14),3);await wait(itemExpression('Move Cottage'));await shot('native-building-context-110');await item('Move Cottage');await wait(buttonExpression('1×'));move(...point(7,14));await new Promise(r=>setTimeout(r,250));const ghost=point(7,14);move(ghost[0]+1,ghost[1]+1);await wait(`(${text}).includes('Move Cottage · Free')`);run(['key','--clearmodifiers','Escape']);
    await button('1×');await wait(`JSON.parse(${saved})?.colony.residents.some(r=>r.arrived&&r.workIssue==='noWork')`);await button('Ⅱ');
    s=await state();const actor=s.colony.residents.find(r=>r.arrived&&r.workIssue==='noWork');assert(actor);
    run(['key','--clearmodifiers','i']);let at=point(...actor.cell);at[1]-=16;click(...at);await wait(buttonExpression('Talk'));
    const unchanged=await state();assert((await read(text)).includes(actor.name+' ·'));
    await button('Talk');await button('How are you doing?');await button('Anything I can help with?');await shot('native-request-offer-110');
    await button('Not right now.');await button('Maybe later');assert.deepEqual(await state(),unchanged);
    await button('Talk');await button('Is there anything you need?');await button("I'll see what I can do.");await button('Back to building');
    assert.equal((await state()).colony.requests.records[0].state,'accepted');
    run(['key','--clearmodifiers','4']);move(...point(6,16));run(['mousedown','1']);move(...point(7,16));run(['mouseup','1']);
    await wait(`JSON.parse(${saved})?.lots.filter(l=>l[6]==='garden').length===2`);
    await button('1×');await wait(`JSON.parse(${saved})?.colony.requests.records[0].state==='resolved'`);await button('Ⅱ');
    const resolved=await state(),helped=resolved.colony.residents.find(r=>r.id===actor.id);assert(helped.workedSeconds>0);
    run(['key','--clearmodifiers','i']);at=point(...helped.cell);at[1]-=16;
    for(let i=0;i<8;i++){click(...at);await new Promise(r=>setTimeout(r,80));if((await read(text)).includes(actor.name+' ·'))break;}
    assert((await read(text)).includes(actor.name+' ·'));await button('Talk');assert((await read(text)).includes('actually been back at work'));await shot('native-thanks-110');
    run(['key','--clearmodifiers','Escape']);run(['key','--clearmodifiers','b']);await new Promise(r=>setTimeout(r,300));await shot('native-catalogue-110-top');
    move(500,400);run(['click','--repeat','4','--delay','80','5']);await new Promise(r=>setTimeout(r,250));await shot('native-catalogue-110-roads');run(['key','--clearmodifiers','Escape']);
    await wait(buttonExpression('Redo'));const redo=await read(buttonExpression('Redo'));assert(redo.x+redo.w<=zoomed.width+.5);await shot('native-settled-110');
    const checks=['real Ctrl-plus 110% browser zoom','native four-home drag','inspection Move and M shortcut','pure move ghost and Escape cancellation','same cottage/resident ID and free price','native relocation Undo/Redo','right-click building context Move','read-only native decline branch','native accepted work promise','native two-Farm drag, actual work and one gratitude acknowledgment','sprite catalogue and road previews through native scrolling','treasury coin and footer fit at 110%'];
    fs.writeFileSync(path.join(output,'native-results.json'),JSON.stringify({original,zoomed,osScale,checks,input:'XTest mouse/keyboard for all game commands; raw CDP observes state/screens and performs initial accessibility setup only',fixtureAssistance:false,request:resolved.colony.requests.records[0]},null,2));console.log(JSON.stringify({checks,original,zoomed},null,2));
    await send('Browser.close');
  } catch(e) {try{const png=await observe('Page.captureScreenshot',{format:'png'});fs.writeFileSync(path.join(output,'failure.png'),Buffer.from(png.data,'base64'));}catch{}throw e;} finally {socket?.close();native.kill('SIGTERM');}
})().catch(e=>{console.error(e);process.exitCode=1;});
