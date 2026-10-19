-- Presentation-only restoration against a real two-generation run store.
local G=require('src.game');local P=require('src.persistence');local E=require('src.idea_environment')
local mode,dir,style=arg[1],arg[2],arg[3]
assert((mode=='write'or mode=='continue')and dir:match('^/var/tmp/roag%-environment%-disk%-[%w%-]+$'))
assert(style=='kenney'or style=='onebit')
local allowed={['active-a.roag']=true,['active-b.roag']=true,['appearance.data']=true,['snapshot.data']=true,['events.data']=true,['next.data']=true}
local disk={}
function disk:read(name)assert(allowed[name]);local f=io.open(dir..'/'..name,'rb');if f then local b=f:read('*a');f:close();return b end end
function disk:write(name,b)assert(allowed[name]);local f=assert(io.open(dir..'/'..name,'wb'));assert(f:write(b));assert(f:close());return true end
local store=P.new(disk)
local function source(path)local f=io.open(path,'rb');if f then local b=f:read('*a');f:close();return b end end
local function image(b)
 local function u32(i)local a,c,d,e=b:byte(i,i+3);return ((a*256+c)*256+d)*256+e end
 return {getWidth=function()return u32(17)end,getHeight=function()return u32(21)end}
end
local function quad(x,y,w,h)return{x=x,y=y,w=w,h=h}end
local required=require('src.sprite_audit').collect().requirements
local function appearance(s)
 local native=assert(require(style=='onebit'and'src.onebit_visuals'or'src.kenney_visuals').new(source,image,quad))
 native:set_world(s);assert(not next(native.environment.failures))
 local out={signature=native.signature,plan=native.environment.plan,handles={}}
 for _,id in ipairs(required)do
  local h=assert(native:resolve(id));out.handles[id]={quad=h.quad,source=h.source,width=h.width,height=h.height,material=h.material,mask=h.mask}
 end
 return P.encode(out)
end
if mode=='write'then
 local s=G.new_stage(19,{multifloor=true});assert(store:save(s))
 disk:write('snapshot.data',P.encode(G.snapshot(s)));disk:write('appearance.data',appearance(s))
 local clone=G.restore(G.snapshot(s));local okay,why,events=G.submit(clone,{kind='wait'});assert(okay,why)
 disk:write('events.data',P.encode(events));disk:write('next.data',P.encode(G.snapshot(clone)))
 print('PASS '..style..': generated floor, source handles and checked run generation 1')
else
 local s=assert(store:load());local before=P.encode(G.snapshot(s))
 assert(before==disk:read('snapshot.data'));assert(appearance(s)==disk:read('appearance.data'),'reopened appearance differs')
 for _=1,25 do assert(appearance(s)==disk:read('appearance.data'))end
 assert(P.encode(G.snapshot(s))==before,'resource refresh changed the run/RNG')
 local okay,why,events=G.submit(s,{kind='wait'});assert(okay,why)
 assert(P.encode(events)==disk:read('events.data'));assert(P.encode(G.snapshot(s))==disk:read('next.data'))
 assert(store:save(s));assert(store:inspect().record.seq==2)
 print('PASS '..style..': fresh-process identical art, 25 resolver replacements, exact action/RNG and generation 2')
end
