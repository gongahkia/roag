// Static evidence of the exact exported sprites, at gameplay size and enlarged.
const {chromium}=require('playwright');const fs=require('node:fs'),path=require('node:path');
(async()=>{
 const out=path.resolve(process.argv[2]||'/tmp/roag-tranche04-lineup');fs.mkdirSync(out,{recursive:true});
 const browser=await chromium.launch({executablePath:'/usr/bin/chromium',args:['--no-sandbox']});
 const page=await browser.newPage({viewport:{width:1024,height:680},deviceScaleFactor:1});
 const bodies=['plain-v1','longhair-v1','hood-v1','cap-v1'],palettes=['cream','teal','clay','lilac'];
 const font=fs.readFileSync('assets/SourceCodePro-Regular.otf').toString('base64');
 for(const [name,px] of [['gameplay',36],['enlarged',144]]){
  const cards=bodies.map((body,row)=>`<section><header>${body} · source r0${row}-c104</header><main>${palettes.map(p=>`<figure><img src="data:image/png;base64,${fs.readFileSync(`assets/images/person-${body}-${p}.png`).toString('base64')}"/><figcaption>${p}</figcaption></figure>`).join('')}</main></section>`).join('');
  await page.setContent(`<style>@font-face{font-family:Source;src:url(data:font/otf;base64,${font})}*{box-sizing:border-box}body{margin:0;padding:24px;background:#202a25;color:#e7d7a9;font:13px Source}h1{font-size:18px;margin:0 0 6px}p{margin:0 0 18px;color:#b4c0aa}section{border-top:1px solid #4c5e48;padding:10px 0}header{margin-bottom:4px;color:#bcc5b2}main{display:flex;justify-content:space-around}figure{margin:0;width:22%;text-align:center;background:#344a36;padding:6px}img{width:${px}px;height:${px}px;image-rendering:pixelated;display:block;margin:0 auto 5px}figcaption{font-size:11px}</style><h1>Four source bodies · four compatible palettes</h1><p>${px===36?'Actual gameplay size: 9 world pixels × 4 camera zoom = 36 screen pixels.':'Enlargement: each source pixel is a 12 × 12 block.'} Black/eye gaps and source silhouettes retained.</p>${cards}`);
  await page.screenshot({path:path.join(out,`resident-lineup-${name}.png`),fullPage:true});
 }
 await browser.close();console.log('Rendered exact source-derived cast at 36px and 144px.');
})().catch(e=>{console.error(e);process.exitCode=1;});
