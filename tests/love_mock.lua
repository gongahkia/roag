-- A contract/wiring and draw-command adapter, NOT a real LÖVE runtime.
-- It cannot establish GPU behaviour, SDL input delivery or LuaJIT performance.
local Mock={}
function Mock.install(directory)
 directory=directory or '/tmp/cosmonauts-mock'
 assert(directory:match('^[%w%_/%-%.]+$'),'Unsafe mock directory')
 os.execute('mkdir -p '..directory)
 local state={width=1340,height=840,records={},record=false,color={1,1,1,1},font={size=14},held={},mouseX=0,mouseY=0}
 local function record(kind,t)
  if state.record then
   t=t or {};t.kind=kind;t.color={state.color[1],state.color[2],state.color[3],state.color[4] or 1}
   t.font=state.font.size;t.clip=state.clip and {state.clip[1],state.clip[2],state.clip[3],state.clip[4]} or nil
   state.records[#state.records+1]=t
  end
 end
 local function write(name,text)
  local f,err=io.open(directory..'/'..name,'wb');if not f then return false,err end
  f:write(text);f:close();return true
 end
 local graphics={}
 function graphics.newFont(path,size)
  if size==nil then size=path end
  assert(type(size)=='number' and size>0,'Mock font size must be numeric')
  return {size=size,getHeight=function() return size end,
   getWidth=function(_,s) return #tostring(s)*size*0.54 end}
 end
 function graphics.setFont(f) state.font=f end
 function graphics.getFont() return state.font end
 function graphics.getDimensions() return state.width,state.height end
 function graphics.setColor(r,g,b,a)
  if type(r)=='table' then state.color={r[1],r[2],r[3],r[4] or 1} else state.color={r,g,b,a or 1} end
 end
 function graphics.clear(r,g,b,a)
  graphics.setColor(r,g,b,a);record('clear')
 end
 function graphics.rectangle(mode,x,y,w,h) record('rect',{mode=mode,x=x,y=y,w=w,h=h}) end
 function graphics.line(...) record('line',{points={...}}) end
 function graphics.push() end
 function graphics.pop() end
 function graphics.translate() end
 function graphics.scale() end
 function graphics.print(s,x,y) record('text',{text=tostring(s),x=x,y=y}) end
 function graphics.printf(s,x,y,width,align) record('text',{text=tostring(s),x=x,y=y,width=width,align=align}) end
 function graphics.setLineWidth(v) state.lineWidth=v end
 function graphics.setScissor(x,y,w,h) state.clip=x and {x,y,w,h} or nil end
 function graphics.newImage(data)
  return {data=data,setFilter=function() end,release=function(self) self.released=true end,
   replacePixels=function(self,d) assert(not self.released);assert(d.w==self.data.w and d.h==self.data.h);self.data=d end}
 end
 function graphics.draw(image,x,y,rotation,sx,sy)
  assert(not image.released);assert(rotation==0,'Mock only supports unrotated terrain image')
  if state.record then
   local data=image.data;local hex={}
   for i=1,data.w*data.h do local p=data.pixels[i] or {0,0,0,1}
    hex[#hex+1]=string.format('%02x%02x%02x',math.floor(p[1]*255+0.5),math.floor(p[2]*255+0.5),math.floor(p[3]*255+0.5))
   end
   record('image',{x=x,y=y,sx=sx,sy=sy,w=data.w,h=data.h,pixels=table.concat(hex)})
  end
 end
 function graphics.captureScreenshot(path) state.screenshotPath=path end
 local image={}
 function image.newImageData(w,h,format)
  assert(format=='rgba8')
  return {w=w,h=h,pixels={},setPixel=function(self,x,y,r,g,b,a)
   assert(x>=0 and x<w and y>=0 and y<h,'Pixel outside image')
   assert(r>=0 and r<=1 and g>=0 and g<=1 and b>=0 and b<=1,'Invalid colour')
   self.pixels[y*w+x+1]={r,g,b,a}
  end,release=function(self) self.released=true end}
 end
 _G.love={graphics=graphics,image=image,keyboard={
  setKeyRepeat=function() end,isDown=function(...) for _,key in ipairs({...}) do if state.held[key] then return true end end return false end},
  timer={getTime=os.clock,getFPS=function() return 60 end},
  mouse={getPosition=function() return state.mouseX,state.mouseY end},
  filesystem={getSaveDirectory=function() return directory end,write=write,
   createDirectory=function(name) assert(name:match('^[%w%_/%-%.]+$'));return os.execute('mkdir -p '..directory..'/'..name) end,
   getInfo=function(name)
    assert(name:match('^[%w%_/%-%.]+$'),'Unsafe mock path')
    for _,path in ipairs({directory..'/'..name,name}) do
     local status=os.execute('test -d '..path)
     if status==true or status==0 then return {type='directory',size=0} end
     local f=io.open(path,'rb');if f then local size=f:seek('end');f:close();if size then return {type='file',size=size} end end
    end
   end,
   getDirectoryItems=function(name)
    assert(name:match('^[%w%_/%-%.]+$'),'Unsafe mock directory')
    local entries,seen={},{}
    for _,path in ipairs({directory..'/'..name,name}) do
     local p=io.popen('find '..path.." -mindepth 1 -maxdepth 1 -printf '%f\\n' 2>/dev/null")
     if p then for file in p:lines() do if not seen[file] then entries[#entries+1]=file;seen[file]=true end end;p:close() end
    end
    return entries
   end,
   read=function(name)
    local f,err=io.open(directory..'/'..name,'rb');if not f then f,err=io.open(name,'rb') end
    if not f then return nil,err end;local data=f:read('*a');f:close();return data
   end},
 }
 return state
end
return Mock
