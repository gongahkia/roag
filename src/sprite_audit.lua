-- Read-only inventory of the actual native resolvers, not every legacy authoring
-- placeholder. No graphics, player saves or simulation/RNG are initialized.
local K=require('src.kenney_visuals')
local O=require('src.onebit_visuals')
local S=require('src.kenney_symbols')
local T=require('src.kenney_terrain')
local Slots=require('src.visual_slots')
local A={}
local function array()return setmetatable({},{audit_array=true})end
local function keys(t)
    local out={};for k in pairs(t)do out[#out+1]=k end;table.sort(out);return out
end
local function read(path)
    local f=assert(io.open(path,'rb'));local bytes=f:read('*a');f:close();return bytes
end
local function image(bytes)
    assert(bytes:sub(1,8)=='\137PNG\r\n\26\n','invalid sprite sheet')
    local function dimension(at)
        local a,b,c,d=bytes:byte(at,at+3)
        return ((a*256+b)*256+c)*256+d
    end
    return {getWidth=function()return dimension(17)end,
        getHeight=function()return dimension(21)end}
end
local function quad(x,y,w,h)return {x=x,y=y,w=w,h=h}end
function A.collect()
    local report={version=1,sheets={},packs={},requirements=array()}
    for name,def in pairs({micro=K,caves=T,onebit=S})do
        report.sheets[name]={path=def.path,width=def.width,height=def.height,
            tile=def.tile,stride=def.stride or def.tile,
            columns=def.columns or def.width/def.tile,checksum=def.checksum}
    end
    for _,id in ipairs(keys(Slots.bindings))do
        if id:match('^core:actor/') or id:match('^core:enemy/') or
            id:match('^core:object/') or id:match('^core:chest/') or
            id:match('^core:boon/') or
            (id:match('^core:drop/') and not id:match('/detail$')) or
            id:match('^core:terrain/[^/]+$') or id:match('^core:terrain/.+/base$') or
            id:match('^core:ui/.+_icon$') or id:match('^core:ui/.+_effect$')then
            report.requirements[#report.requirements+1]=id
        end
    end
    for _,name in ipairs({'kenney','onebit'})do
        local definition=name=='kenney' and K or O
        local resolver=definition.new(read,image,quad)
        local p={bindings=array(),missing=array(),usage={},shared_actors=array(),actor_count=0,
            actor_silhouettes=0,static_pose_aliases=0,wall_masks=47,water_masks=47}
        report.packs[name]=p
        local function use(sheet,index,label)
            local usage=p.usage[sheet]or {};p.usage[sheet]=usage
            local cell=usage[tostring(index)]or {};usage[tostring(index)]=cell
            cell[#cell+1]=label
        end
        local aliases={}
        for _,id in ipairs(report.requirements)do
            local h=resolver:resolve(id)
            if not h then p.missing[#p.missing+1]=id
            else
                p.bindings[#p.bindings+1]={id=id,width=h.width,height=h.height,
                    index=h.symbol_index or h.tile_index or -1,
                    sheet=h.approved_sheet or h.terrain_pack or
                        (h.symbol_index and 'onebit') or h.tile_index and 'micro' or
                        (name=='kenney' and 'caves-composite-or-floor' or 'onebit-composite'),
                    motif=h.motif or '',pose_alias=id:match('/attack$')~=nil or id:match('/hurt$')~=nil}
            end
        end
        for _,id in ipairs(keys(definition.actors))do
            local index=definition.actors[id]
            use(name=='kenney' and 'micro' or 'onebit',index,id)
            aliases[index]=aliases[index]or {};aliases[index][#aliases[index]+1]=id
            p.actor_count=p.actor_count+1;p.static_pose_aliases=p.static_pose_aliases+2
        end
        if name=='kenney' then
            for _,id in ipairs({'core:actor/hookrunner','core:actor/trapper'}) do
                local h=assert(resolver:resolve(id))
                use(h.approved_sheet,h.tile_index,id)
                p.actor_count=p.actor_count+1
                p.actor_silhouettes=p.actor_silhouettes+1
                p.static_pose_aliases=p.static_pose_aliases+2
            end
        end
        for _,index in ipairs(keys(aliases))do
            p.actor_silhouettes=p.actor_silhouettes+1
            if #aliases[index]>1 then p.shared_actors[#p.shared_actors+1]={index=index,ids=aliases[index]}end
        end
        for _,id in ipairs(keys(definition.objects))do
            use(name=='kenney' and 'micro' or 'onebit',definition.objects[id],id)
        end
        for _,id in ipairs(keys(S.boons))do
            local h=resolver:resolve(id..'/icon')
            use(h.approved_sheet or h.terrain_pack or
                (h.symbol_index and 'onebit' or 'micro'),
                h.symbol_index or h.tile_index,id..'/icon + /detail')
        end
        for _,id in ipairs(keys(S.terrain))do use('onebit',S.terrain[id][1],id)end
        if name=='kenney'then
            for _,id in ipairs(keys(Slots.bindings))do
                if id:match('^core:object/review_') then
                    local h=resolver:resolve(id)
                    if h then use(h.approved_sheet or h.terrain_pack or 'micro',
                        h.tile_index or h.symbol_index,id) end
                end
            end
            for _,biome in ipairs(keys(T.styles))do
                for _,index in ipairs(T.styles[biome].floor)do use('caves',index,biome..' floor')end
            end
            for _,family in ipairs(keys(T.wall_materials))do
                for _,part in ipairs(keys(T.wall_materials[family]))do
                    use('caves',T.wall_materials[family][part],family..' wall material: '..part)
                end
            end
            for _,family in ipairs(keys(T.water_materials))do
                for part,index in ipairs(T.water_materials[family])do
                    use('caves',index,family..' water quadrant material '..part)
                end
            end
        else
            for _,part in ipairs({'floor','wall','water','frame'})do
                use('onebit',O[part..'_index'],'composite terrain material: '..part)
            end
        end
        for _,sheet in pairs(p.usage)do for _,labels in pairs(sheet)do table.sort(labels)end end
    end
    return report
end
-- Sorted object keys keep generated inventory byte-for-byte reproducible.
function A.json(value)
    local kind=type(value)
    if kind=='string'then return '"'..value:gsub('[%z\1-\31\\"]',function(c)
        return ('\\u%04x'):format(c:byte())end)..'"'end
    if kind=='number' or kind=='boolean'then return tostring(value)end
    assert(kind=='table','unsupported audit value')
    local parts={}
    if #value>0 or (getmetatable(value)or {}).audit_array then
        for _,item in ipairs(value)do parts[#parts+1]=A.json(item)end
        return '['..table.concat(parts,',')..']'
    end
    for _,key in ipairs(keys(value))do parts[#parts+1]=A.json(key)..':'..A.json(value[key])end
    return '{'..table.concat(parts,',')..'}'
end
return A
