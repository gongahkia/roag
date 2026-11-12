-- Active classic runner. Original cases stay in tests/run.lua and its modules.
-- Exactly 30 obsolete Kenney image/layout/catalogue cases are archived below.
-- Three mixed modules are loaded with explicit catalogue adapters; no gameplay
-- assertions are removed. The original runner retains failures and exit status.
package.path="./?.lua;./?/init.lua;"..package.path
local retired={
 ['tests.fixed_visuals_tests']={
  ["fixed artwork preserves all existing and additive boon bindings and exactly thirteen approved cross-sheet counterparts"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
 },
 ['tests.idea_environment_tests']={
  ["approved room materials and foreground art have distinct stable IDs and bounded native regions"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["all 256 neighbor cases resolve canonical connected environment walls and hide unknown connections"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["derived material layouts are deterministic, bounded and nonoverlapping across cached pages"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["fully connected wall pixels sample the correct native center at untrimmed local coordinates"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["water exterior and concave corners use the inspected RPG bank family rather than stray floor sprites"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["monochrome environment surfaces contain neutral opaque pixels and keep native originals immutable"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["environment handles cache every texture and Quad instead of decoding them per tile or frame"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["missing optional colored counterparts preserve native actors/material fallback and a distinct refresh signature"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["saved catalogue covers all environment and boon IDs without changing class bodies"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
 },
 ['tests.idea_terrain_tests']={
  ["the fixed renderer resolves all new terrain and the permitted vent derivative preserves neutral pixels and alpha"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
 },
 ['tests.kenney_terrain_tests']={
  ["Caves native source validates 16px regions with 1px gutters and rejects damaged inputs"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["all biomes cover every wall/water neighborhood without disclosing unknown geometry"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["Caves surface assembly preserves native quarter pixels and leaves source untouched"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
 },
 ['tests.kenney_visuals_tests']={
  ["Kenney native packed regions cover classes, enemies, props and all biomes"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["Kenney reads exact checked source and fails closed on missing or corrupt sheet"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["Kenney connected walls and water select native edges without revealing unknown cells"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["Caves floors use only native ground, never door frames or wall props"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["all boons have deliberate native icons and matching details"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["all six mechanical terrains have legible native art with exact integer inset scaling"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["Kenney supplement rejects missing, corrupt, wrong-sized and out-of-range inputs"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["historical mixed fixture cannot be selected by the fixed game resolver"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
 },
 ['tests.onebit_visuals_tests']={
  ["1-Bit sheet ink and composed surface background are neutral"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["standalone 1-Bit covers every actor, prop, boon and terrain sprite using only its own PNG"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["standalone 1-Bit neighbor lookup, opacity and source composition remain deterministic"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["standalone 1-Bit fails closed on absent, corrupt or wrongly sized art"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["fixed 1-Bit construction has no saved-choice dependency and leaves the run intact"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
 },
 ['tests.sprite_audit_tests']={
  ["native sprite inventory checks runtime coverage and distinguishes shared/static poses"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["sprite audit is deterministic and read only across repeated scans"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
  ["saved sprite inventory matches the current resolver mappings"]="Retired Kenney source pixels/layout; replaced by classic catalogue contract and real LÖVE GUI checks",
 },
}

local base_require=require
local encountered,adapted={},{}
local function source(path)
 local f=assert(io.open(path,'rb'));local text=f:read('*a');f:close();return text
end
local function replace(text,old,new,expected)
 local count=0;local start=1;local parts={}
 while true do
  local first,last=text:find(old,start,true)
  if not first then parts[#parts+1]=text:sub(start);break end
  parts[#parts+1]=text:sub(start,first-1);parts[#parts+1]=new;start=last+1;count=count+1
 end
 assert(count==(expected or 1),'adapter source changed: '..old..' occurrences='..count)
 return table.concat(parts)
end
local function active_module(name)
 if name=='tests.visual_packs_tests' then
  local text=source('tests/visual_packs_tests.lua')
  text=replace(text,"'onebit'","'catalog'",4)
  text=replace(text,"'Kenney 1-Bit'","'Urizen catalog'")
  adapted[#adapted+1]=name..': fixed style/label/backing expectation changed to catalogue; all four assertions/cases retained'
  return assert(loadstring(text,'@tests/visual_packs_tests.lua'))()
 elseif name=='tests.fixed_visuals_tests' or name=='tests.idea_environment_tests' then
  local path=name:gsub('%.','/')..'.lua';local text=source(path)
  local _,count=text:gsub("src%.onebit_visuals",'')
  text=replace(text,"require('src.onebit_visuals')","require('src.catalog_visuals')",count)
  if name=='tests.fixed_visuals_tests' then
   text=replace(text,"'onebit'","'catalog'",6)
   text=replace(text,'path==O.path','path==next(require(\'src.classic_sprite_data\').sources)')
   text=replace(text,'local function image(b,path)',[[local function image(b,path,meta)
  assert(b:sub(1,8)=='\137PNG\r\n\26\n','invalid PNG')
  if meta and meta.catalog_surface then local s=meta.catalog_surface
   return {path=path,getWidth=function()return s.width end,getHeight=function()return s.height end}
  end]])
   adapted[#adapted+1]=name..': actual loader, settings and refresh use catalogue; shader/fallback assertions unchanged'
  else
   text=replace(text,'local function image(raw)',[[local function image(raw,path,meta)
  if meta and meta.catalog_surface then local s=meta.catalog_surface
   return {getWidth=function()return s.width end,getHeight=function()return s.height end}
  end]])
   text=replace(text,'local function resolver(style,reader,factory)return E.new(style,reader or read,factory or image,quad)end',[[local function resolver(style,reader,factory)
  local catalog=assert(require('src.catalog_visuals').new(reader or read,factory or image,quad))
  return {set_world=function(self,state)catalog:set_world(state);self.plan=catalog.environment.plan end}
 end]])
   adapted[#adapted+1]=name..': same planning/fog/world/HUD/actual-hit assertions use catalogue resources and derived-surface sizes'
  end
  return assert(loadstring(text,'@'..path))()
 end
 return base_require(name)
end
local function active_require(name)
 local module=active_module(name)
 if not retired[name] then return module end
 return function(test,equal)
  return module(function(label,fn)
   local reason=retired[name][label]
   if reason then
    local key=name..' :: '..label;assert(not encountered[key]);encountered[key]=reason
    print('ARCHIVED ART ONLY '..key)
   else test(label,fn)end
  end,equal)
 end
end
local function audit()
 local rows={};local expected=0
 for module,names in pairs(retired)do for name,reason in pairs(names)do
  local key=module..' :: '..name;expected=expected+1
  assert(encountered[key], 'retirement entry no longer matched: '..key)
  rows[#rows+1]=key..'\n  '..reason
 end end
 assert(expected==30,'explicit retirement count changed')
 table.sort(rows);table.sort(adapted)
 local report='CLASSIC ACTIVE TEST AUDIT\nArchived image-only cases: '..expected..
  '\nAll original gameplay cases retained. No assertions deleted from active mixed cases.\n\n'..
  table.concat(rows,'\n')..'\n\nADAPTED MIXED MODULES\n'..table.concat(adapted,'\n')..'\n'
 local path=os.getenv('ROAG_CLASSIC_TEST_AUDIT')
 if path then local f=assert(io.open(path,'wb'));f:write(report);f:close()end
 print('Classic audit: '..expected..' obsolete image-only cases archived; gameplay assertions retained.')
end
local original=source('tests/run.lua')
original=replace(original,'print(("%d passed, %d failed"):format(passed, failed))',[[
require('tests.classic_catalog_visuals_tests')(test,equal)
classic_audit()
print(("%d passed, %d failed"):format(passed, failed))]])
local chunk=assert(loadstring(original,'@tests/run.lua'))
setfenv(chunk,setmetatable({require=active_require,classic_audit=audit},{__index=_G}))
chunk()
