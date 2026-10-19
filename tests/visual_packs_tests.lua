local P=require('src.visual_packs')
local Game=require('src.game')
return function(test,equal)
 test('game appearance is fixed and exposes no preference store or selectable list',function()
  assert(P.default=='onebit' and P.label=='Kenney 1-Bit')
  for _,name in ipairs({'order','labels','valid','index','save','inspect','files','format'})do assert(P[name]==nil,name)end
 end)
 test('fixed visual builder rejects alternate renderer injection',function()
  for _,style in ipairs({'classic','kenney','automatic','generated','published','unsafe'})do
   assert(not pcall(P.build,function()end,function()end,function()return {style=style},'loaded'end))
  end
 end)
 test('fixed visuals bypass published overrides and resolve only the native onebit source',function()
  local state=Game.new_stage(37);local before=Game.snapshot(state);local calls=0
  local native={style='onebit',resolve=function(_,id)if id=='core:actor/adventurer'then return {source='onebit'}end end}
  local function read()error('retired published artwork must not be read')end
  local v,status=P.build(read,function()error('unexpected loose image load')end,function(reader)
   assert(reader==read);calls=calls+1;return native,'loaded'
  end)
  assert(status=='loaded' and v.status=='generated' and calls==1 and v.has_terrain)
  assert(v:resolve('core:actor/adventurer').source=='onebit');equal(before,Game.snapshot(state))
 end)
 test('missing fixed artwork permits only automatic emergency drawing without writing preferences',function()
  local state=Game.new_stage(37);local before=Game.snapshot(state)
  local old={['visual-pack-a.roag']='old classic choice',['visual-pack-b.roag']='old mixed choice'}
  local function read(path)assert(not path:match('visual%-pack'));return old[path]end
  local v,status=P.build(read,function()error('unexpected image load')end,function()return nil,'absent'end)
  assert(status=='absent' and v.status=='absent' and not v:resolve('core:actor/adventurer'))
  assert(old['visual-pack-a.roag']=='old classic choice' and old['visual-pack-b.roag']=='old mixed choice')
  equal(before,Game.snapshot(state))
 end)
end
