-- Versioned TERRAIN TEMPLATES, deliberately separate from full colony saves.
-- Import loads exact cell arrays. A seed/recipe is provenance, not a reroll command.
local J=require('src.json')
local W=require('src.world')
local U=require('src.util')
local M=require('src.materials')
local B=require('src.biomes')
local E=require('src.expedition')
local Codec=require('src.codec')
local Map={format='deepward-map',version=2,maxBytes=J.limits.bytes}
Map.materialKeys={'air','bedrock','rock','soil','sand','water','ore','lava','steam','ice'}
local materialByKey={};for i,key in ipairs(Map.materialKeys) do materialByKey[key]=i-1 end
local function object(t,allowed,label)
 assert(type(t)=='table' and t~=J.null and not J.isArray(t),label..' must be an object')
 for k in pairs(t) do assert(type(k)=='string' and allowed[k],label..': unsupported field '..tostring(k)) end
end
local function array(t,label,max)
 assert(type(t)=='table' and t~=J.null,label..' must be an array')
 local count=0;for k in pairs(t) do U.integer(k,label..' index',1,max);count=count+1 end
 assert(count==#t and count<=max,label..' array is sparse or too large')
 return count
end
local function ascii(s,name,n)
 assert(type(s)=='string' and #s>=1 and #s<=n and not s:find('[^\032-\126]'),name..' must be printable ASCII, 1..'..n..' bytes')
end
local function unpackLayer(layer,n,lookup,label)
 object(layer,{palette=true,runs=true},label)
 local count=array(layer.palette,label..' palette',32);assert(count>0,'Empty palette')
 local ids,seen={},{}
 for i,key in ipairs(layer.palette) do
  assert(type(key)=='string' and lookup[key]~=nil,'Unknown '..label..' palette entry: '..tostring(key))
  assert(not seen[key],'Duplicate palette entry');seen[key]=true;ids[i]=lookup[key]
 end
 local length=array(layer.runs,label..' RLE',n*2);assert(length%2==0 and length>0,'RLE needs index/count pairs')
 local total=0
 -- Validate ALL counts and indices before allocating expanded cells.
 for k=1,length,2 do
  U.integer(layer.runs[k],label..' palette index',1,count)
  U.integer(layer.runs[k+1],label..' run count',1,n)
  total=total+layer.runs[k+1];assert(total<=n,'RLE exceeds map area')
 end
 assert(total==n,'RLE does not cover map area')
 local result={};local at=1
 for k=1,length,2 do local id=ids[layer.runs[k]]
  for _=1,layer.runs[k+1] do result[at]=id;at=at+1 end
 end
 return result
end
local function packLayer(cells,n,keys,idToPalette)
 local runs=J.array();local last,count=nil,0
 for i=1,n do
  local v=idToPalette(cells and cells[i] or 0)
  assert(v,'Unknown layer value')
  if v==last then count=count+1 else
   if last then runs[#runs+1]=last;runs[#runs+1]=count end
   last,count=v,1
  end
 end
 if last then runs[#runs+1]=last;runs[#runs+1]=count end
 return {palette=J.array(U.copy(keys)),runs=runs}
end
function Map.fromWorld(w,title)
 assert(w.home,'This world has no reusable arrival marker')
 local biomeKeys={};for id=0,B.count do biomeKeys[#biomeKeys+1]=B.def[id].key end
 local d={format=Map.format,version=Map.version,title=title or w.mapTitle or (w.preset..' / '..w.seed),
  width=w.width,height=w.height,seed=w.seed,preset=w.preset,
  arrival={left=w.home.left or 9,right=w.home.right,floor=w.home.floor},
  material=packLayer(w.mat,w.n,Map.materialKeys,function(id) return id+1 end),
  biome=packLayer(w.biomes,w.n,biomeKeys,function(id) return id+1 end)}
 if w.generation then d.recipe=U.deep(w.generation) end
 if w.content then
  d.features=require('src.content').template(w)
  for _,key in ipairs({'flora','fauna','sites','ruins'}) do d.features[key]=J.array(d.features[key]) end
 end
 Map.validate(d)
 return d
end
function Map.validate(d)
 object(d,{format=true,version=true,title=true,width=true,height=true,seed=true,preset=true,arrival=true,material=true,biome=true,recipe=true,features=true},'Map')
 assert(d.format==Map.format and (d.version==1 or d.version==2),'Unsupported map format/version')
 ascii(d.title,'Title',96);ascii(d.preset,'Preset label',32)
 assert(d.preset:match('^[%w_-]+$'),'Invalid preset label')
 U.integer(d.width,'map width',128,512);U.integer(d.height,'map height',80,256)
 assert(d.width%4==0 and d.height%4==0,'Map dimensions must be multiples of four')
 U.integer(d.seed,'map seed',0,2147483646)
 local n=d.width*d.height
 object(d.arrival,{left=true,right=true,floor=true},'Arrival')
 U.integer(d.arrival.left,'arrival left',5,d.width-64)
 if d.recipe~=nil then
  local fields={version=true,layout=true,climate=true,openness=true,biomeScale=true}
  if d.version==2 then fields.features=true;fields.density=true;fields.crew=true end
  object(d.recipe,fields,'Recipe')
  if d.recipe.features~=nil then assert(d.recipe.features=='none' or d.recipe.features=='ruins' or d.recipe.features=='living','Invalid feature profile') end
  if d.recipe.density~=nil then assert(U.finite(d.recipe.density) and d.recipe.density>=0.5 and d.recipe.density<=1.5,'Invalid feature density') end
  if d.recipe.crew~=nil then assert(d.recipe.crew==3 or d.recipe.crew==6 or d.recipe.crew==9,'Invalid crew size') end
  ascii(d.recipe.version,'Generator version',32);ascii(d.recipe.layout,'Layout identifier',32);ascii(d.recipe.climate,'Climate identifier',32)
  assert(U.finite(d.recipe.openness) and d.recipe.openness>=0.25 and d.recipe.openness<=0.70,'Invalid recipe openness')
  assert(U.finite(d.recipe.biomeScale) and d.recipe.biomeScale>=0.65 and d.recipe.biomeScale<=1.5,'Invalid recipe biome scale')
  -- Unknown generator identifiers may be kept as provenance: cell data is authoritative.
 end
 local mat=unpackLayer(d.material,n,materialByKey,'material')
 local biome=unpackLayer(d.biome,n,B.byKey,'biome')
 local w={width=d.width,height=d.height,n=n,mat=mat}
 for y=1,d.height do for x=1,d.width do
  if x<=2 or x>=d.width-1 or y<=2 or y>=d.height-1 then assert(mat[(y-1)*d.width+x]==M.BEDROCK,'Map must have a two-cell bedrock boundary') end
 end end
 if d.features~=nil then
  assert(d.version==2,'Feature templates require map schema 2')
  require('src.content').validateTemplate(d.features,d.width,d.height)
 end
 E.validateStart(w,d.arrival,d.features and d.features.crew)
 return mat,biome
end
function Map.encode(d) Map.validate(d);return J.encode(d) end
function Map.decode(text) local d=J.decode(text);Map.validate(d);return d end
function Map.toWorld(d,mode)
 local mat,biomes=Map.validate(d)
 local w=W.new(d.width,d.height,d.seed,d.preset,mode or 'challenge')
 w.mat=mat;w.biomes=biomes;w.mapTitle=d.title
 if d.recipe then w.generation=U.deep(d.recipe) end
 E.populate(w,d.arrival,d.features and d.features.crew)
 if d.features then require('src.content').install(w,d.features) end
 return w
end
function Map.fingerprint(d) return Codec.hash(d) end -- Diagnostic only, not authenticity.
function Map.regenerate(d,changes)
 Map.validate(d);assert(d.recipe and (d.recipe.version=='frontier-v1' or d.recipe.version=='frontier-v2'),'Recipe is not supported by this generator')
 local o={layout=d.recipe.layout,climate=d.recipe.climate,openness=d.recipe.openness,biomeScale=d.recipe.biomeScale}
 if d.recipe.version=='frontier-v2' then o.features=d.recipe.features;o.density=d.recipe.density;o.crew=d.recipe.crew end
 for k,v in pairs(changes or {}) do o[k]=v end
 local w
 if d.recipe.version=='frontier-v1' then w=require('src.generation.frontier_v1').make(d.seed,'challenge',d.width,d.height,o)
 else w=require('src.generate').make(d.seed,'frontier','challenge',d.width,d.height,o) end
 return Map.fromWorld(w)
end
return Map
