local J=require('src.json')
local U=require('src.util')
local G=require('src.generate')
local W=require('src.world')
local M=require('src.materials')
local Map=require('src.mapfile')
local E=require('src.expedition')
local B=require('src.biomes')
local L=require('src.generation.layouts')
local Rep=require('src.generation.report')
local H=require('src.history')
local Codec=require('src.codec')
local Metrics=require('src.metrics')
local T={}
function T.run()
 local assertions,groups=0,0
 local function check(ok,why) assertions=assertions+1;assert(ok,why) end
 local function reject(fn) check(not pcall(fn),'Expected rejection') end
 local function test(name,fn) fn();groups=groups+1;print('PASS  '..name) end
 local world=G.make(12345,'frontier','challenge',128,80,{layout='hybrid'})
 local d=Map.fromWorld(world)
 test('JSON roundtrip, Unicode, arrays, null and canonical keys',function()
  local value=J.decode('{"a":[1,-2.5,1e-5,true,false,null],"z":"\\uD83D\\uDE00","b":[]}')
  check(value.a[6]==J.null and #value.a==6,'null must not truncate arrays')
  check(J.isArray(value.b) and #value.b==0)
  check(value.z==string.char(240,159,152,128))
  check(J.encode(J.decode(J.encode(value)))==J.encode(value))
  check(J.decode('"escaped\\nline"')=='escaped\nline')
 end)
 test('JSON rejects ambiguity, malformed numbers, UTF-8 and executable input',function()
  for _,s in ipairs({'{"a":null,"a":1}','[1,]','{"x":1,}','01','+1','1.','1e','1e999','-','NaN',
    'true false','{"a":1} trailing','"\\uD800"','"\\uDC00"','"\\uD800\\u0041"',
    '"'..string.char(192,128)..'"','"'..string.char(0)..'"','return os.execute("x")'}) do reject(function() J.decode(s) end) end
  reject(function() J.decode(string.rep('[',26)..'0'..string.rep(']',26)) end)
  reject(function() J.decode('"'..string.rep('x',4097)..'"') end)
  reject(function() J.encode({[1]='a',[3]='b'}) end)
  local cyc={};cyc.cyc=cyc;reject(function() J.encode(cyc) end)
 end)
 test('Map canonical encode/decode preserves exact terrain and biome arrays',function()
  local text=Map.encode(d);local restored=Map.decode(text)
  check(Map.encode(restored)==text)
  local w=Map.toWorld(restored,'practice')
  for i=1,w.n do check(w.mat[i]==world.mat[i] and w.biomes[i]==world.biomes[i],'Cell changed on import') end
  check(w.mode=='practice' and w.tick==0 and #w.jobs==0)
  check(W.totalResource(w,'food')==require('config').startFood and #w.workers==3)
  check(Map.fingerprint(d)==Map.fingerprint(restored))
 end)
 test('RLE validates positive counts, palettes, area and closed boundaries',function()
  local bad=U.deep(d);bad.material.runs[2]=0;reject(function() Map.validate(bad) end)
  bad=U.deep(d);bad.material.runs[2]=1e9;reject(function() Map.validate(bad) end)
  bad=U.deep(d);bad.material.runs[1]=99;reject(function() Map.validate(bad) end)
  bad=U.deep(d);table.remove(bad.biome.runs);reject(function() Map.validate(bad) end)
  bad=U.deep(d);bad.material.palette[1]='unregistered';reject(function() Map.validate(bad) end)
  bad=U.deep(d);bad.material.palette[1]=bad.material.palette[2];reject(function() Map.validate(bad) end)
  bad=U.deep(d);bad.material.runs[1]=1;reject(function() Map.validate(bad) end)
 end)
 test('Map loader rejects unsupported schemas, injected colony state and invalid spawns',function()
  for _,edit in ipairs({function(x) x.version=999 end,function(x) x.width=9999999 end,
   function(x) x.height=81 end,function(x) x.seed=-1 end,function(x) x.workers={} end,
   function(x) x.rules={hungerRate=0} end,function(x) x.arrival.left=2 end,
   function(x) x.arrival.floor=3 end,function(x) x.title='bad\nline' end,
   function(x) x.recipe.openness=0/0 end,function(x) x.recipe.script='os.execute()' end}) do
   local bad=U.deep(d);edit(bad);reject(function() Map.validate(bad) end)
  end
  local before=Codec.hash(world);local broken=Map.encode(d):sub(1,-5)
  reject(function() Map.decode(broken) end);check(Codec.hash(world)==before)
 end)
 test('Import uses baked cells, not executable recipes or generation calls',function()
  local old=G.make;G.make=function() error('Generator must not be called on import') end
  local ok,w=pcall(Map.toWorld,d,'challenge');G.make=old;check(ok and w.mat[100]==world.mat[100])
  local foreign=U.deep(d);foreign.recipe.version='future-generator';foreign.recipe.layout='external-caves'
  check(Map.toWorld(foreign).n==world.n)
  reject(function() Map.regenerate(foreign) end)
 end)
 test('All layout techniques differ but retain independent same-seed region geography',function()
  local hashes={};local first
  for _,layout in ipairs(L.names) do
   local a=G.make(1337,'frontier','challenge',128,80,{layout=layout})
   local b=G.make(1337,'frontier','challenge',128,80,{layout=layout})
   check(Codec.hash(a)==Codec.hash(b),'Generator not repeatable')
   local matHash=Codec.hash(a.mat);check(not hashes[matHash],'Different techniques produced identical terrain');hashes[matHash]=true
   if first then for i=1,a.n do check(a.biomes[i]==first.biomes[i],'Layout changed biome seed stream') end else first=a end
   E.validateStart(a,a.home);check(W.validate(a))
   local report=Rep.measure(a);check(report.biomeCount>=6 and report.horizontalTransitions>0 and report.verticalTransitions>0)
  end
 end)
 test('Depth and lateral variation, profiles and extreme seeds remain bounded',function()
  for _,seed in ipairs({0,1,2147483646}) do for _,climate in ipairs(B.climates) do
   local w=G.make(seed,'frontier','challenge',128,80,{layout='noise',climate=climate})
   local report=Rep.measure(w);check(report.biomeCount>=4 and report.horizontalTransitions>0 and report.verticalTransitions>0)
   check(W.validate(w));check(#w.biomes==w.n)
  end end
  local a=B.generate(12345,128,80,'balanced',0.75);local b=B.generate(12345,128,80,'balanced',1.4)
  check(Codec.hash(a)~=Codec.hash(b))
 end)
 test('Generation settings are validated; recipes explicitly regenerate or fork geometry',function()
  reject(function() G.make(1,'frontier','challenge',128,80,{layout='missing'}) end)
  reject(function() G.make(1,'frontier','challenge',128,80,{climate='missing'}) end)
  reject(function() G.make(1,'frontier','challenge',128,80,{openness=-1}) end)
  reject(function() G.make(1,'frontier','challenge',128,80,{biomeScale=2}) end)
  reject(function() G.make(1,'frontier','challenge',128,80,{extra=true}) end)
  check(Map.encode(Map.regenerate(d))==Map.encode(d))
  local other=Map.regenerate(d,{layout='worms'})
  local diff=Rep.compare(Map.toWorld(d),Map.toWorld(other))
  check(diff.changedMaterial>0 and diff.changedBiome==0)
 end)
 test('New terrain runs the existing colony rules with balanced ledgers',function()
  local h=H.new(G.make(12345,'frontier','challenge',128,80,{layout='hybrid'}))
  local left=h.live.home.left;local gx=math.floor((left+24)/4)+1;local gy=(h.live.home.floor-1)/4
  check(h:queue({type='order',kind='build',build='farm',gx=gx,gy=gy,priority=2}))
  for _=1,240 do h:advance() end
  local r=Metrics.measure(h.live)
  check(r.waterResidual==0 and r.mineralResidual==0 and r.foodResidual==0)
  check(h.live.stats.jobsDone>0,'Colonists did not construct in the new arrival chamber')
  check(W.validate(h.live))
 end)
 test('Biome arrays survive checkpoints, saves, archive replay and challenge locks',function()
  local h=H.new(G.make(42,'frontier','challenge',128,80,{layout='faults'}))
  h.checkpointEvery=15;h.checkpointLimit=2
  for _=1,120 do h:advance() end
  local expected=Codec.hash(h.live);h:seek(0);h:updateSeek(1000)
  check(not h:queue({type='paint',x=10,y=10,material=M.WATER}))
  local saved=H.fromText(h:saveText());check(Codec.hash(saved.live)==expected)
  h:seek(63);h:updateSeek(1000)
  local replay=H.new(U.deep(h.initial));for _=1,63 do replay:advance() end
  check(Codec.hash(replay.live)==Codec.hash(h.view))
  h:seek(h.frontier);check(Codec.hash(h.live)==expected)
 end)
 test('Authentic 0.2.0 save continues to the original expected state hash',function()
  local function read(path)
   if love and love.filesystem then local s=love.filesystem.read(path);if s then return s end end
   local f=assert(io.open(path,'rb'));local s=f:read('*a');f:close();return s
  end
  local h=H.fromText(read('tests/data/legacy-v020.dw'))
  check(h.live.version=='0.2.0' and h.live.biomes==nil and h.frontier==80)
  for _=1,80 do h:advance() end
  check(Codec.hash(h.live)==read('tests/data/legacy-v020-expected.txt'),'Legacy simulation changed')
 end)
 test('Legacy initial maps export independently of colony saves',function()
  for _,preset in ipairs({'cistern','dunes','frost'}) do
   local w=G.make(12345,preset,'challenge',128,80)
   local m=Map.fromWorld(w);local a=Map.toWorld(Map.decode(Map.encode(m)))
   check(Codec.hash(w.mat)==Codec.hash(a.mat));check(Rep.measure(a).biomeCount==0)
  end
 end)
 print(groups..' map/generation groups; '..assertions..' assertions passed.')
 return {groups=groups,assertions=assertions}
end
return T
