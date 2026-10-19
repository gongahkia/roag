return function(test,equal)
 local G=require('src.game');local O=require('src.onebit_visuals');local P=require('src.visual_packs')
 local function read(path)local f=io.open(path,'rb');if f then local b=f:read('*a');f:close();return b end end
 local function image(b,path)
  local function n(i)local a,c,d,e=b:byte(i,i+3);return ((a*256+c)*256+d)*256+e end
  return {path=path,getWidth=function()return n(17)end,getHeight=function()return n(21)end}
 end
 local function quad(x,y,w,h)return{x=x,y=y,w=w,h=h}end
 local function fixture()
  assert(not _G.love)
  local fake={};local env=setmetatable({love=fake,os=setmetatable({getenv=function(name)
   if name=='ROAG_SPRITE_PACK'then return 'kenney'end
  end},{__index=os})},{__index=_G})
  local chunk=assert(loadfile('main.lua'));setfenv(chunk,env);chunk()
  local function visit(fn,callback,seen)
   seen=seen or {};if seen[fn]then return end;seen[fn]=true;local children={}
   for i=1,200 do local name,value=debug.getupvalue(fn,i);if not name then break end
    callback(fn,i,name,value);if type(value)=='function'then children[#children+1]=value end
   end
   for _,child in ipairs(children)do visit(child,callback,seen)end
  end
  local function walk(callback)for _,root in ipairs({fake.draw,fake.update,fake.keypressed})do visit(root,callback)end end
  local function set(key,value)
   local found=false;walk(function(fn,i,name)if name==key then debug.setupvalue(fn,i,value);found=true end end)
   assert(found,'fixture upvalue missing: '..key);if jit then jit.flush()end
  end
  local function get(key)local result;walk(function(_,_,name,value)if name==key then result=value end end);return assert(result,'missing '..key)end
  return fake,set,get
 end
 test('actual native loader ignores the retired environment override and cannot select the mixed renderer',function()
  local fake,_,get=fixture();local called=0;local original=O.new
  O.new=function(reader)called=called+1;assert(reader==read);return {style='onebit'},'loaded'end
  local okay,result,status=pcall(get('load_native_visuals'),read,'kenney');O.new=original
  assert(okay,result);assert(result.style=='onebit' and status=='loaded' and called==1)
  assert(fake.roagVisualPackMode()=='onebit')
 end)
 test('real settings callbacks offer only sound effects and accessibility and cannot save another art style',function()
  local fake,set,get=fixture();local state=G.new_stage(17);local before=G.snapshot(state)
  local settings=require('src.presentation_settings').defaults();local memory={}
  function memory:write(name,bytes)assert(name=='presentation-settings.roag');self[name]=bytes;return true end
  function memory:read(name)return self[name]end
  local font={getWidth=function(_,t)return #t*7 end,getHeight=function()return 14 end,
   setFilter=function()end,setFallbacks=function()end}
  local labels={};fake.graphics=setmetatable({newFont=function()return font end,getFont=function()return font end,
   getWidth=function()return 1024 end,getDimensions=function()return 1024,640 end,
   printf=function(t)labels[t]=true end},{__index=function()return function()end end})
  fake.audio={setVolume=function()end}
  set('state',state);set('title',true);set('title_view','settings');set('storage',memory)
  set('presentation_settings',settings);set('settings_page',1);set('settings_selection',1)
  set('ui_body_font',font);set('ui_menu_font',font);set('ui_display_font',font)
  _G.love=fake;local okay,why=pcall(function()
   fake.keypressed('left');assert(settings.master==90)
   fake.keypressed('tab');fake.keypressed('return');assert(settings.shake==false)
   fake.keypressed('tab');fake.keypressed('return');assert(settings.font=='sans')
   fake.keypressed('tab');fake.keypressed('right');assert(settings.master==100)
   for _,key in ipairs({'1','2','3','4','5','6'})do fake.keypressed(key)end
   get('draw_settings')()
  end);_G.love=nil;assert(okay,why)
  assert(labels.SOUND and labels.EFFECTS and labels.ACCESSIBILITY and not labels.SPRITES)
  assert(fake.roagVisualPackMode()=='onebit' and memory['visual-pack-a.roag']==nil)
  equal(G.snapshot(state),before)
 end)
 test('real refresh retains good resources after corrupt or missing artwork and never alters the run',function()
  local fake,set,get=fixture();local s=G.new_stage(17);local before=G.snapshot(s);local bad=false
  local function reader(path)
   if path:match('^assets/studio/')then return end
   if path==O.path and bad then if bad=='missing'then return nil end;return 'corrupt'end
   return read(path)
  end
  local function build(r)return P.build(r,image,function(rr)
   local okay,result,status=pcall(O.new,rr,image,quad)
   if not okay then return nil,'rejected: '..tostring(result)end
   return result,status
  end)end
  fake.filesystem={read=reader};fake.graphics={setBackgroundColor=function()end};local initial=build(reader)
  set('state',s);set('title',false);set('visual_assets',initial);set('build_selected_visuals',build)
  local refresh=get('refresh_visuals');assert(refresh());local good=get('visual_assets')
  assert(refresh() and fake.roagVisualRefreshStatus():match('checked'))
  good=get('visual_assets');bad='corrupt';assert(not refresh() and get('visual_assets')==good)
  bad='missing';assert(not refresh() and get('visual_assets')==good)
  bad=false;assert(refresh() and fake.roagAssetBacking(G.player(s).definition_id)=='onebit')
  equal(G.snapshot(s),before)
 end)
 test('fixed artwork preserves all 491 bindings and exactly thirteen approved cross-sheet counterparts',function()
  local seen={};local function reader(path)assert(not path:find('micro%-roguelike'));seen[path]=true;return read(path)end
  local native=assert(O.new(reader,image,quad));local count=0
  for _,id in ipairs(require('src.sprite_audit').collect().requirements)do
   local h=assert(native:resolve(id));assert(h.source=='onebit')
   if h.texture.path~=O.path then count=count+1;assert(h.monochrome)end
  end
  assert(count==13 and not next(native.environment.failures))
  assert(seen['assets/kenney/approved/tiny-dungeon.png'] and seen['assets/kenney/approved/roguelike-rpg.png']
   and seen['assets/kenney/caves-dungeons/terrain.png'])
 end)
 test('monochrome shader compiles once and restores the callers exact graphics state',function()
  local filter=require('src.onebit_shader').new();local prior={};local shader={};local active=prior;local calls=0
  local g={newShader=function(source)calls=calls+1;assert(source:find('pixel.a',1,true));return shader end,
   getShader=function()return active end,setShader=function(value)active=value end}
  for _=1,40 do local old,applied=filter:begin(g);assert(active==shader and old==prior);filter:finish(g,old,applied);assert(active==prior)end
  assert(calls==1)
 end)
 test('unsupported monochrome shader warns once and allows unshaded fallback rendering',function()
  local filter=require('src.onebit_shader').new();local warnings,calls=0,0
  local g={newShader=function()calls=calls+1;error('unsupported renderer')end,
   getShader=function()error('failed compile must not touch graphics state')end,setShader=function()error('failed compile must not bind')end}
  for _=1,40 do local old,applied=filter:begin(g,function()warnings=warnings+1 end);assert(not applied);filter:finish(g,old,applied)end
  assert(calls==1 and warnings==1)
 end)
end
