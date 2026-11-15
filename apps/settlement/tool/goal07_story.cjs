// Real release-browser input; checkpoint observations are read only.
const{chromium}=require('playwright'),fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const out=path.resolve(process.argv[2]||'/tmp/roag-goal07-story');fs.mkdirSync(out,{recursive:true});
const url=process.env.SETTLEMENT_URL||'http://127.0.0.1:8768/settlement/',key='roag.settlement.landscape.v1';
let browser,page;const checks=[],errors=[],timeline=[];let started;
(async()=>{
 browser=await chromium.launch({executablePath:'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
 const context=await browser.newContext({viewport:{width:1024,height:640},recordVideo:{dir:out,size:{width:1024,height:640}}});
 page=await context.newPage();started=Date.now();page.on('pageerror',e=>errors.push(e.message));
 const btn=n=>page.getByRole('button',{name:n,exact:typeof n==='string'}),read=()=>page.evaluate(k=>JSON.parse(localStorage.getItem(k)),key);
 async function ready(){await page.locator('#loading').waitFor({state:'detached'});const p=page.locator('flt-semantics-placeholder');if(await p.count())await p.press('Enter');await btn(/^1 Cottage/).waitFor();}
 async function dismiss(){if(await btn('Start building').count())await btn('Start building').click();}
 async function menu(n){await btn('Landscape menu').click();await page.getByRole('menuitem',{name:n,exact:true}).click();}
 async function load(f,confirm=true){const choose=page.waitForEvent('filechooser');await menu('Import landscape…');await(await choose).setFiles(f);await btn('Replace settlement').waitFor();await btn(confirm?'Replace settlement':'Keep current settlement').click();await page.waitForTimeout(250);zoom=4;ox=oy=0;await layout();}
 async function newWorld(preset,seed){timeline.push({name:'create-'+preset,seconds:(Date.now()-started)/1000});await menu('New landscape…');await btn(/Environment/).click();await page.getByRole('menuitem',{name:preset,exact:true}).click();await page.getByRole('textbox',{name:'World seed',exact:true}).fill(seed);await page.waitForTimeout(900);await btn('Create world').click();await btn('Start building').click();await btn('Ⅱ').click();zoom=4;ox=oy=0;await layout();}
 async function wait(fn,timeout=90000){await page.waitForFunction(({key,fn})=>Function('s',`return (${fn})(s)`)(JSON.parse(localStorage.getItem(key))),{key,fn:fn.toString()},{timeout});}
 let width=1024,top,bottom,center,zoom=4,ox=0,oy=0;
 async function layout(){const t=await btn(/^1 Cottage/).boundingBox(),m=await btn('Landscape menu').boundingBox();width=await page.evaluate(()=>innerWidth);top=m.y+m.height+12;bottom=t.y-14;center=Math.max(9,(width/2-24)/48);}
 const pt=(x,y)=>[width/2+(x+.5-center)*12*zoom+ox,(top+bottom)/2+(y-15)*12*zoom+oy];
 async function pan(dx,dy){await page.mouse.move(650,430);await page.mouse.down({button:'right'});await page.mouse.move(650+dx,430+dy,{steps:10});await page.mouse.up({button:'right'});ox+=dx;oy+=dy;}
 async function tool(t){if(typeof t==='number'){await page.keyboard.press(String(t));return;}await btn(/Build catalogue/).click();const item=page.getByRole('menuitem',{name:new RegExp('^'+t+' ·')});await item.scrollIntoViewIfNeeded();await item.click();}
 async function stroke(t,cells){await tool(t);await page.mouse.move(...pt(...cells[0]));await page.mouse.down();for(const cell of cells.slice(1))await page.mouse.move(...pt(...cell),{steps:10});await page.mouse.up();await page.waitForTimeout(150);}
 async function snap(n,keepPointer=false){timeline.push({name:n,seconds:(Date.now()-started)/1000});if(!keepPointer)await page.mouse.move(28,32);await page.waitForTimeout(200);await page.screenshot({path:path.join(out,n+'.png')});}
 const pass=s=>{checks.push(s);console.log('PASS',s);};
 await page.goto(url);await ready();await dismiss();await btn('Ⅱ').click();await layout();
 await newWorld('Woodlands','202');await snap('story-woodland');
 await stroke(2,[[7,15],[10,15]]);await tool('Clear');await page.mouse.move(...pt(11,15));await page.mouse.down();await page.mouse.move(...pt(11,17),{steps:16});await page.waitForTimeout(700);await snap('story-clear',true);await page.mouse.up();
 await stroke(2,[[10,15],[11,15],[11,17]]);await stroke(1,[[12,15],[12,17]]);await btn('1×').click();await page.waitForTimeout(2000);await btn('Ⅱ').click();await snap('story-woodland-built');
 await newWorld('River Country','202');await btn('Zoom out (−)').click();zoom=3;
 const initial=await read(),water=[];for(let x=0;x<40;x++)if(initial.lots[15*40+x][9]==='water')water.push(x);
 const left=water[0]-1,right=water.at(-1)+1;
 await stroke(2,[[7,15],[left,15]]);await tool('Bridge');await page.mouse.move(...pt(left,15));await page.mouse.down();await page.mouse.move(...pt(right,15),{steps:16});await page.waitForTimeout(700);await snap('story-bridge-preview',true);await page.mouse.up();
 await stroke(2,[[right,15],[right+2,15]]);await stroke(1,[[2,14],[5,14]]);await stroke(4,[[right+1,14]]);await stroke(5,[[7,14]]);
 await btn('4×').click();await wait(s=>s.colony.clock>=12);await btn('1×').click();await snap('story-neighbours');
 let crossed=false;for(let i=0;i<400;i++){await page.waitForTimeout(100);await btn('1×').click();if((await read()).colony.residents.some(r=>r.x>17&&r.x<20)){crossed=true;break;}}assert(crossed);await snap('story-crossing');await wait(s=>s.colony.workEarned>0);await snap('story-work');await page.waitForTimeout(1800);await btn('Ⅱ').click();
 const state=await read();assert(state.colony.workEarned>0);assert.deepEqual(errors,[]);
 await context.close();await browser.close();fs.writeFileSync(path.join(out,'results.json'),JSON.stringify({url,input:'Playwright browser input; no callbacks or injected state',seed:202,environmentSequence:['Woodlands','River Country'],readingHolds:'900ms world review and 700ms staged landscape/bridge previews; harness only',timeline,errors,workEarned:state.colony.workEarned},null,2));
})().catch(async e=>{console.error(e);if(page)await page.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});if(browser)await browser.close();process.exitCode=1});
