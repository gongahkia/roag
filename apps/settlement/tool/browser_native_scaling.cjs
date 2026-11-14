// Optional Linux/X11 check: xdotool sends the browser's real zoom accelerator.
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
    env:{...process.env,DISPLAY:process.env.DISPLAY||':96',WAYLAND_DISPLAY:''}});
  const page=await browser.newPage({viewport:{width:1024,height:640}});
  await page.goto('http://127.0.0.1:8765/settlement/');
  await page.locator('flt-semantics-placeholder').press('Enter');
  await page.getByRole('button',{name:'2 Path',exact:true}).waitFor();
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(500);
  const original=await page.evaluate(()=>({ratio:devicePixelRatio,width:innerWidth,height:innerHeight}));
  const xdotool=process.env.XDOTOOL_BIN||'xdotool';
  const windows=execFileSync(xdotool,['search','--onlyvisible','--name','ROAG'],{encoding:'utf8'}).trim().split('\n');
  execFileSync(xdotool,['windowfocus','--sync',windows[0]]);
  execFileSync(xdotool,['key','--clearmodifiers','ctrl+plus']);
  await page.waitForFunction(ratio=>devicePixelRatio>ratio,original.ratio);
  const zoomed=await page.evaluate(()=>({ratio:devicePixelRatio,width:innerWidth,height:innerHeight}));
  // The world's camera stays at the same world point when the viewport resizes.
  await page.getByRole('button',{name:'2 Path',exact:true}).click();
  const x=zoomed.width/2-24,y=72+(zoomed.height-150)/2;
  const windowBox=await page.evaluate(()=>({width:outerWidth,height:outerHeight}));
  const geometry=execFileSync(xdotool,['getwindowgeometry','--shell',windows[0]],{encoding:'utf8'});
  const osScale=Number(geometry.match(/WIDTH=(\d+)/)[1])/windowBox.width;
  const border=(windowBox.width-zoomed.width*zoomed.ratio)/2;
  const nativeX=Math.round((border+x*zoomed.ratio)*osScale);
  const nativeY=Math.round((windowBox.height-zoomed.height*zoomed.ratio-border+y*zoomed.ratio)*osScale);
  console.log(JSON.stringify({original,zoomed,point:[x,y],windowBox,osScale,geometry,native:[nativeX,nativeY]}));
  execFileSync(xdotool,['mousemove','--window',windows[0],String(nativeX),String(nativeY)]);
  execFileSync(xdotool,['click','1']);
  execFileSync('ffmpeg',['-y','-f','x11grab','-video_size','1600x1000','-i',process.env.DISPLAY||':96','-frames:v','1',path.join(output,'native-desktop.png')],{stdio:'ignore'});
  await page.screenshot({path:path.join(output,'native-before-assert.png')});
  await page.waitForFunction(()=>JSON.parse(localStorage.getItem('roag.settlement.landscape.v1'))?.lots[15*40+8][3]);
  await page.screenshot({path:path.join(output,'native-browser-zoom.png')});
  assert(zoomed.ratio>original.ratio);
  const result={browser:await browser.version(),original,zoomed,osScale,
    nativeInput:'XTest via xdotool: focus and Ctrl +; actual Chromium desktop zoom',
    otherInput:'Playwright palette selection; XTest mouse click extends correct path cell after zoom'};
  fs.writeFileSync(path.join(output,'native-results.json'),JSON.stringify(result,null,2));
  await browser.close();console.log(JSON.stringify(result,null,2));
})().catch(e=>{console.error(e);process.exit(1);});
