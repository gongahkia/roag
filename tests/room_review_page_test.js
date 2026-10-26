// Minimal headless DOM smoke test for the self-contained review page.
const fs = require('fs');
const vm = require('vm');
const assert = require('assert');

const html = fs.readFileSync('docs/room-review/review.html', 'utf8');
const script = html.match(/<script>\s*([\s\S]*?)<\/script>\s*<\/body>/)[1];
new vm.Script(script);
const embedded = id => JSON.parse(html.match(new RegExp(
  `<script id="${id}" type="application/json">([^<]+)<\\/script>`))[1]);
const data = embedded('room-data');
const sprites = embedded('sprite-data');
assert.equal(data.rooms.length, 148);
assert.equal(data.proposals.families.flatMap(group => group.variants).length, 105);
assert.equal(data.proposals.corridors.length, 5);
for (const [name,path] of Object.entries({
  caves:'assets/kenney/caves-dungeons/terrain.png',
  micro:'assets/kenney/micro-roguelike/tiles.png',
  symbols:'assets/kenney/1-bit-pack/symbols.png',
  rpg:'assets/kenney/approved/roguelike-rpg.png'
})) assert(Buffer.from(sprites[name].split(',')[1], 'base64').equals(fs.readFileSync(path)));

const noop = () => {};
function context2d() {
  return {fillRect:noop,strokeRect:noop,drawImage:noop,beginPath:noop,arc:noop,
    fill:noop,stroke:noop,fillText:noop,save:noop,restore:noop,translate:noop,
    scale:noop,moveTo:noop,lineTo:noop,closePath:noop,setLineDash:noop};
}
function canvases(source,selector) {
  const result = [];
  for (const match of source.matchAll(/<canvas\s+([^>]+)>/g)) {
    const room = match[1].match(/data-room-id="([^"]+)"/);
    const kind = match[1].match(/data-kind="([^"]+)"/);
    if (selector === 'canvas[data-room-id]' && !room) continue;
    result.push({dataset:{roomId:room?.[1],kind:kind?.[1]},getContext:context2d});
  }
  return result;
}
class Element {
  constructor(id) {
    this.id=id;this.innerHTML='';this.value='';this.hidden=false;
    this.checked=true;this.listeners={};this.dataset={};this.attributes={};
  }
  addEventListener(type,callback) { this.listeners[type]=callback; }
  insertAdjacentHTML(_position,markup) {
    this.innerHTML+=markup;
    if (this.id==='proposal-biome' && !this.value)
      this.value=markup.match(/<option value="([^"]+)"/)?.[1] || '';
  }
  querySelectorAll(selector) { return canvases(this.innerHTML,selector); }
  setAttribute(name,value) { this.attributes[name]=value; }
}
const elements = new Map();
const getElementById = id => {
  if (id==='room-data' || id==='sprite-data') return {textContent:JSON.stringify(
    id==='room-data' ? data : sprites)};
  if (!elements.has(id)) elements.set(id,new Element(id));
  return elements.get(id);
};
const document = {
  getElementById,
  querySelectorAll(selector) {
    return [...elements.values()].flatMap(element => element.querySelectorAll(selector));
  },
  createElement:tag => tag==='canvas' ? {width:0,height:0,getContext:context2d}
    : tag==='a' ? {click:noop,remove:noop} : new Element(tag),
  body:{append:noop}
};
const storage = new Map();
const localStorage = {getItem:key => storage.get(key) || null,
  setItem:(key,value) => storage.set(key,value)};
class Image { set src(value) { this.url=value; } }
let exported;
class Blob { constructor(parts) { this.text=parts.join(''); } }
const URL={createObjectURL:blob => {exported=JSON.parse(blob.text);return 'blob:test';},
  revokeObjectURL:noop};
vm.runInNewContext(script,{document,localStorage,Image,console,setTimeout,URL,Blob});
assert.equal(getElementById('proposals-section').hidden,false);
assert.equal(canvases(getElementById('proposal-families').innerHTML,'canvas').length,15);
assert.equal(canvases(getElementById('proposal-corridors').innerHTML,'canvas').length,5);
const firstId=data.proposals.families[0].id;
const pill={dataset:{},textContent:''};
const buttons=['approve','reject','discuss'].map(choice => ({dataset:{choice},setAttribute:noop}));
const article={dataset:{proposalId:firstId},querySelector:() => pill,
  querySelectorAll:() => buttons};
buttons[1].closest=selector => selector==='[data-choice]' ? buttons[1] : article;
getElementById('proposal-families').listeners.click({target:buttons[1]});
assert.equal(JSON.parse(storage.get('roag-room-proposal-review/2'))[firstId].vote,'reject');
getElementById('proposal-families').listeners.input({target:{value:'Widen the east pocket',
  matches:() => true,closest:() => article}});
assert.equal(JSON.parse(storage.get('roag-room-proposal-review/2'))[firstId].comment,'Widen the east pocket');
getElementById('download-button').listeners.click();
assert.equal(exported.decisions.length,148);
assert.equal(exported.proposals.length,20);
assert.equal(exported.decisions.filter(item => item.vote==='approve').length,148);
assert.equal(exported.proposals.find(item => item.id===firstId).vote,'reject');
assert.equal(exported.proposals.find(item => item.id===firstId).comment,'Widen the east pocket');
getElementById('sprite-pack').value='onebit';
getElementById('sprite-pack').listeners.change();
assert.equal(getElementById('pack-label').textContent,'Kenney 1-Bit');
getElementById('proposal-biome').value='core:biome/fungal_hollows';
getElementById('proposal-biome').listeners.change();
assert(getElementById('proposal-families').innerHTML.includes('Fungal Hollows'));
getElementById('existing-tab').listeners.click();
assert.equal(getElementById('existing-section').hidden,false);
assert.equal(canvases(getElementById('rooms').innerHTML,'canvas').length,148);
getElementById('proposals-tab').listeners.click();
assert.equal(getElementById('proposals-section').hidden,false);
console.log('Room review page: data, initial proposals, biome switch, and tabs OK');
