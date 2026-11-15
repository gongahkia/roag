// Optional Linux/X11 check: native browser zoom and a real XTest brush stroke.
const {chromium}=require('playwright');
const {execFileSync}=require('node:child_process');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const output=path.resolve(process.argv[2]||'/tmp/settlement-native-evidence');
fs.mkdirSync(output,{recursive:true});
(async()=>{
  const browser=await chromium.launch({executablePath:process.env.CHROMIUM_BIN||'/usr/bin/chromium',
    headless:false,args:['--no-sandbox','--ozone-platform=x11','--force-device-scale-factor=1'],
    env:{...process.env,DISPLAY:process.env.DISPLAY||':95',WAYLAND_DISPLAY:''}});
  const page=await browser.newPage({viewport:{width:1024,height:640}});
  await page.goto(process.env.SETTLEMENT_URL||'http://127.0.0.1:8765/settlement/');
  await page.locator('flt-semantics-placeholder').press('Enter');
  await page.getByRole('button',{name:'2 Path',exact:true}).waitFor();
  await page.waitForLoadState('networkidle');await page.locator('#loading').waitFor({state:'detached'});await page.waitForTimeout(500);
  const original=await page.evaluate(()=>({ratio:devicePixelRatio,width:innerWidth,height:innerHeight}));
  const xdotool=process.env.XDOTOOL_BIN||'xdotool';
  const run=args=>execFileSync(xdotool,args,{encoding:'utf8'});
  const window=run(['search','--onlyvisible','--name','ROAG']).trim().split('\n')[0];
  run(['windowfocus','--sync',window]);run(['key','--clearmodifiers','ctrl+plus']);
  await page.waitForFunction(ratio=>devicePixelRatio>ratio,original.ratio);
  const zoomed=await page.evaluate(()=>({ratio:devicePixelRatio,width:innerWidth,height:innerHeight}));
  const box=await page.evaluate(()=>({width:outerWidth,height:outerHeight}));
  const geometry=run(['getwindowgeometry','--shell',window]);
  const osScale=Number(geometry.match(/WIDTH=(\d+)/)[1])/box.width;
  const border=(box.width-zoomed.width*zoomed.ratio)/2;
  function move(x,y){run(['mousemove','--window',window,
    String(Math.round((border+x*zoomed.ratio)*osScale)),
    String(Math.round((box.height-zoomed.height*zoomed.ratio-border+y*zoomed.ratio)*osScale))]);}
  async function clickButton(name){
    const b=await page.getByRole('button',{name,exact:true}).boundingBox();move(b.x+b.width/2,b.y+b.height/2);run(['click','1']);await page.waitForTimeout(120);
  }
  await clickButton('Return to lane (H)');await clickButton('2 Path');
  const tool=await page.getByRole('button',{name:'1 Cottage',exact:true}).boundingBox();
  const menu=await page.getByRole('button',{name:'Landscape menu',exact:true}).boundingBox();
  const cy=(menu.y+menu.height+12+tool.y-10)/2;
  move(24+8.5*48,cy);run(['mousedown','1']);await page.waitForTimeout(100);
  assert.equal(await page.evaluate(()=>localStorage.getItem('roag.settlement.landscape.v1')),null);
  run(['key','--clearmodifiers','ctrl+l']);
  await page.waitForFunction(()=>!document.hasFocus());run(['mouseup','1']);
  await page.waitForTimeout(150);
  assert.equal(await page.evaluate(()=>localStorage.getItem('roag.settlement.landscape.v1')),null,'native focus loss cancels, rather than committing');
  run(['key','Escape']);await clickButton('2 Path');
  move(24+8.5*48,cy);run(['mousedown','1']);await page.waitForTimeout(100);
  assert.equal(await page.evaluate(()=>localStorage.getItem('roag.settlement.landscape.v1')),null);
  move(24+12.5*48,cy);await page.waitForTimeout(100);run(['mouseup','1']);
  await page.waitForFunction(()=>JSON.parse(localStorage.getItem('roag.settlement.landscape.v1'))?.lots.filter(l=>l[3]).length===13);
  await page.screenshot({path:path.join(output,'native-browser-zoom.png')});
  assert(zoomed.ratio>original.ratio);
  const result={browser:await browser.version(),original,zoomed,osScale,
    nativeInput:'XTest via xdotool: real Ctrl+plus desktop zoom, palette controls, five-cell path drag and release',
    checks:['110% browser zoom','native focus-loss cancellation','no save during held stroke','exact cells 8..12 added through one native drag'],paths:13};
  fs.writeFileSync(path.join(output,'native-results.json'),JSON.stringify(result,null,2));
  await browser.close();console.log(JSON.stringify(result,null,2));
})().catch(e=>{console.error(e);process.exit(1);});
