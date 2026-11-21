// Storage-only regression: no browser profile, live settlement or application callbacks.
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),vm=require('node:vm');
const key='roag.settlement.landscape.v1',values=new Map();let fail=false;
const scope={window:{},document:{addEventListener(){}},localStorage:{getItem:k=>values.get(k)??null,setItem(k,v){if(fail)throw Error('Quota denied');values.set(k,v);}}};
vm.createContext(scope);vm.runInContext(fs.readFileSync(path.resolve(__dirname,'../web/platform.js'),'utf8'),scope);
const p=scope.window.settlementPlatform;
const legacy=fs.readFileSync(path.resolve(__dirname,'../test/fixtures/goal09-town.json'),'utf8');
values.set(key,legacy);assert.equal(p.load(),legacy);
p.backupRegion(legacy);p.backupRegion('another town');assert.equal(values.get(key+'.before-region-v7'),legacy);
const original=JSON.parse(legacy),payload={schema:7,towns:[original,original,original],unicode:'森林 · 🏘️',empty:{},array:[null,{},[],true,1.25],names:JSON.parse('{"__proto__":null,"$":"literal"}')};
const text=JSON.stringify(payload);p.save(text);assert.deepEqual(JSON.parse(p.load()),payload);
assert(values.get(key).length<text.length);const saved=values.get(key);fail=true;
assert.throws(()=>p.save(JSON.stringify({...payload,unicode:'new'})),/Quota denied/);assert.equal(values.get(key),saved);fail=false;
const packed=JSON.parse(saved);packed.value.$[0]=999999;values.set(key,JSON.stringify(packed));assert.throws(()=>p.load(),/Invalid packed region field/);
values.set(key,saved);assert.deepEqual(JSON.parse(p.load()),payload);
console.log(JSON.stringify({checks:6,originalCharacters:text.length,packedCharacters:saved.length,ratio:saved.length/text.length,legacyBytesPreserved:true,quotaFailurePreservedPriorSave:true}));
