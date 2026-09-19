-- Canonical, data-only serialization. Decoder NEVER executes Lua or bytecode.
-- String lengths and table sizes are explicit. No cyclic tables or metatables.
local U=require('src.util')
local Codec={}
function Codec.encode(value)
 local out,seen={},{}
 local function write(v,depth)
  assert(depth<=64,'Save nesting limit')
  local t=type(v)
  if t=='number' then assert(U.finite(v),'Nonfinite number');out[#out+1]='n'..string.format('%.17g',v)..';'
  elseif t=='string' then out[#out+1]='s'..#v..':'..v
  elseif t=='boolean' then out[#out+1]=v and 'b1' or 'b0'
  elseif t=='nil' then out[#out+1]='z'
  elseif t=='table' then
   assert(not seen[v],'Cyclic save data');seen[v]=true
   local keys={}
   for k in pairs(v) do
    assert(type(k)=='number' or type(k)=='string','Unsupported key type')
    if type(k)~='string' or k:sub(1,1)~='_' then keys[#keys+1]=k end
   end
   table.sort(keys,function(a,b) if type(a)==type(b) then return a<b end return type(a)<type(b) end)
   out[#out+1]='t'..#keys..':'
   for _,k in ipairs(keys) do write(k,depth+1);write(v[k],depth+1) end
   seen[v]=nil
  else error('Unsupported save type '..t) end
 end
 write(value,0);return table.concat(out)
end
function Codec.decode(s)
 assert(type(s)=='string' and #s<=32*1024*1024,'Save size limit')
 local pos,nodes=1,0
 local function count()
  local j=s:find(':',pos,true);assert(j and j-pos<10,'Malformed length')
  local raw=s:sub(pos,j-1);assert(raw:match('^%d+$'),'Malformed length')
  local n=tonumber(raw);assert(n<=2000000,'Container limit');pos=j+1;return n
 end
 local function read(depth)
  nodes=nodes+1;assert(depth<=64 and nodes<=2000000,'Save complexity limit')
  local c=s:sub(pos,pos);pos=pos+1
  if c=='n' then
   local j=s:find(';',pos,true);assert(j and j-pos<=32,'Malformed number')
   local v=tonumber(s:sub(pos,j-1));assert(U.finite(v),'Invalid number');pos=j+1;return v
  elseif c=='s' then local n=count();assert(pos+n-1<=#s,'Truncated string');local v=s:sub(pos,pos+n-1);pos=pos+n;return v
  elseif c=='b' then local v=s:sub(pos,pos);assert(v=='0' or v=='1','Invalid boolean');pos=pos+1;return v=='1'
  elseif c=='z' then return nil
  elseif c=='t' then
   local n=count();local t={}
   for _=1,n do
    local k=read(depth+1);assert(type(k)=='number' or type(k)=='string','Invalid key')
    assert(t[k]==nil,'Duplicate key');t[k]=read(depth+1)
   end
   return t
  end
  error('Malformed save token')
 end
 local v=read(0);assert(pos==#s+1,'Trailing save data');return v
end
-- Diagnostic rolling checksum, not cryptographic and not used as an integrity proof.
function Codec.hash(value)
 local s=Codec.encode(value);local h=1
 for i=1,#s do h=(h*131+s:byte(i))%2147483647 end
 return string.format('%08x',h)
end
return Codec
