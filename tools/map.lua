-- Run from the repository root: lua tools/map.lua help
local Map=require('src.mapfile')
local G=require('src.generate')
local Rep=require('src.generation.report')
local J=require('src.json')
local U=require('src.util')
local function read(path)
 local f=assert(io.open(assert(path,'Missing input path'),'rb'));local size=f:seek('end');f:seek('set')
 if not size or size>Map.maxBytes then f:close();error('Map byte limit') end
 local s=f:read('*a');f:close();return Map.decode(s)
end
local function write(path,text)
 assert(path,'Missing output path')
 local prior=io.open(path,'rb');if prior then prior:close();error('Refusing to overwrite '..path..'; choose a new output name') end
 local f=assert(io.open(path,'wb'));assert(f:write(text));assert(f:close());print('Wrote '..path)
end
local function options(start)
 local out={};local allowed={seed=true,width=true,height=true,layout=true,climate=true,openness=true,['biome-scale']=true,preset=true,features=true,density=true,crew=true}
 local i=start
 while i<=#arg do
  local k=arg[i]:match('^%-%-(.+)$');assert(k and allowed[k],'Unknown option '..tostring(arg[i]))
  assert(arg[i+1] and not arg[i+1]:match('^%-%-'),'Missing value for '..k);assert(out[k]==nil,'Duplicate option '..k)
  out[k]=arg[i+1];i=i+2
 end
 for _,k in ipairs({'seed','width','height','openness','biome-scale','density','crew'}) do if out[k] then out[k]=assert(tonumber(out[k]),'Expected number for '..k) end end
 return out
end
local function run()
 local command=arg[1] or 'help'
 if command=='generate' then
  local o=options(3);local w=G.make(o.seed or 12345,o.preset or 'frontier','challenge',o.width or 256,o.height or 160,
   {layout=o.layout or 'hybrid',climate=o.climate or 'balanced',openness=o.openness or 0.48,biomeScale=o['biome-scale'] or 1,features=o.features or 'living',density=o.density or 1,crew=o.crew or 3})
  local d=Map.fromWorld(w);write(arg[2],Map.encode(d));print('Fingerprint '..Map.fingerprint(d));print(Rep.csv(Rep.measure(w)))
 elseif command=='inspect' then
  local d=read(arg[2]);print(d.title..' / '..Map.fingerprint(d));print(Rep.csv(Rep.measure(Map.toWorld(d))))
 elseif command=='roundtrip' then
  local d=read(arg[2]);write(arg[3],Map.encode(d));print('Fingerprint '..Map.fingerprint(d))
 elseif command=='regenerate' then
  local d=read(arg[2]);local o=options(4);local changes={}
  for k,v in pairs(o) do assert(k=='layout' or k=='climate' or k=='openness' or k=='biome-scale' or k=='features' or k=='density' or k=='crew','Regenerate cannot change '..k);changes[k=='biome-scale' and 'biomeScale' or k]=v end
  local new=Map.regenerate(d,changes);write(arg[3],Map.encode(new));print('Fingerprint '..Map.fingerprint(new))
 elseif command=='compare' then
  local a,b=read(arg[2]),read(arg[3]);print(J.encode(Rep.compare(Map.toWorld(a),Map.toWorld(b))))
 else
  assert(command=='help','Unknown command')
  print([[Cosmonauts map tools (run from the repository root; existing files never overwritten)
  lua tools/map.lua generate out.dwmap.json --seed 12345 --layout hybrid --climate balanced --width 256 --height 160
  lua tools/map.lua inspect in.dwmap.json
  lua tools/map.lua roundtrip in.dwmap.json out.dwmap.json
  lua tools/map.lua regenerate in.dwmap.json out.dwmap.json --layout worms
  lua tools/map.lua compare a.dwmap.json b.dwmap.json
Layouts: hybrid, noise, cellular, worms, faults, vaults, karst, labyrinth, roots, chasms, crystal.
Profiles: balanced, arid, frozen, volcanic, overgrown, ruined, abyssal.
Contents: --features living|ruins|none --density 0.5..1.5 --crew 3|6|9.
Optional: --openness 0.25..0.70 --biome-scale 0.65..1.5 --preset cistern|dunes|frost.
Frontier settings do not change legacy presets. 'compare' reports cell differences, not a quality ranking.]])
 end
end
local ok,err=pcall(run);if not ok then io.stderr:write(tostring(err)..'\n');os.exit(1) end
