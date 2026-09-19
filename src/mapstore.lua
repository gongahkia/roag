-- LÖVE file adapter. Parse and validate before offering a replacement expedition.
local Map=require('src.mapfile')
local Store={}
local function validPath(path)
 return type(path)=='string' and path:match('^maps/[%w%._/%-]+%.dwmap%.json$') and not path:find('..',1,true)
end
function Store.list()
 local entries={}
 for _,dir in ipairs({'maps','maps/examples'}) do
  if love.filesystem.getInfo(dir) then
   for _,name in ipairs(love.filesystem.getDirectoryItems(dir)) do
    local path=dir..'/'..name
    if validPath(path) then local info=love.filesystem.getInfo(path)
     if info and info.type=='file' then entries[#entries+1]=path end
    end
   end
  end
 end
 table.sort(entries);return entries
end
function Store.read(path)
 assert(validPath(path),'Invalid map-library path')
 local info=assert(love.filesystem.getInfo(path),'Map file not found')
 assert(info.type=='file' and info.size and info.size<=Map.maxBytes,'Map is too large or unreadable')
 local text,err=love.filesystem.read(path);assert(text,err)
 return Map.decode(text)
end
function Store.dropped(file)
 local size,why=file:getSize();assert(size and size<=Map.maxBytes,'Dropped map is too large or unreadable: '..tostring(why))
 local opened,err=file:open('r');assert(opened,err)
 local ok,result=pcall(function()
  local text,readErr=file:read(Map.maxBytes+1);assert(text,readErr);return Map.decode(text)
 end)
 file:close();if not ok then error(result,0) end;return result
end
function Store.write(d)
 local text=Map.encode(d)
 assert(love.filesystem.createDirectory('maps'),'Could not create maps directory')
 local base='maps/map-'..d.seed..'-'..Map.fingerprint(d)..'-'..os.time()
 local path=base..'.dwmap.json';local n=1
 while love.filesystem.getInfo(path) do path=base..'-'..n..'.dwmap.json';n=n+1 end
 local good,err=love.filesystem.write(path,text);assert(good,err)
 return path
end
return Store
