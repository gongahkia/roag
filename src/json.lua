-- Small bounded JSON codec written for map interchange. Never executes input.
-- RFC 8259 grammar; duplicate keys, invalid UTF-8, and unpaired surrogates rejected.
local U=require('src.util')
local J={null={},limits={bytes=6*1024*1024,nodes=600000,depth=24,string=4096}}
local arrayTag={}
function J.array(t) return setmetatable(t or {},arrayTag) end
function J.isArray(t) return type(t)=='table' and getmetatable(t)==arrayTag end
local function utf8(cp)
 if cp<128 then return string.char(cp) elseif cp<2048 then return string.char(192+math.floor(cp/64),128+cp%64)
 elseif cp<65536 then return string.char(224+math.floor(cp/4096),128+math.floor(cp/64)%64,128+cp%64) end
 return string.char(240+math.floor(cp/262144),128+math.floor(cp/4096)%64,128+math.floor(cp/64)%64,128+cp%64)
end
local function validUtf8(s)
 local i=1
 while i<=#s do
  local a=s:byte(i);local count,cp,min
  if a<128 then count,cp,min=1,a,0 elseif a>=194 and a<=223 then count,cp,min=2,a-192,128
  elseif a>=224 and a<=239 then count,cp,min=3,a-224,2048
  elseif a>=240 and a<=244 then count,cp,min=4,a-240,65536 else return false end
  for j=1,count-1 do local b=s:byte(i+j);if not b or b<128 or b>191 then return false end;cp=cp*64+b-128 end
  if cp<min or cp>1114111 or (cp>=55296 and cp<=57343) then return false end
  i=i+count
 end
 return true
end
function J.decode(s)
 assert(type(s)=='string' and #s<=J.limits.bytes,'JSON byte limit')
 local pos,nodes=1,0
 local function fail(message) error('JSON at byte '..pos..': '..message,0) end
 local function ws() while true do local c=s:sub(pos,pos);if c==' ' or c=='\n' or c=='\r' or c=='\t' then pos=pos+1 else return end end end
 local function str()
  if s:sub(pos,pos)~='"' then fail('expected string') end
  pos=pos+1;local out={};local bytes=0;local begin=pos
  local function add(v) bytes=bytes+#v;if bytes>J.limits.string then fail('string limit') end;out[#out+1]=v end
  while pos<=#s do
   local c=s:sub(pos,pos)
   if c=='"' then add(s:sub(begin,pos-1));pos=pos+1;local value=table.concat(out);if not validUtf8(value) then fail('invalid UTF-8') end;return value
   elseif c=='\\' then
    add(s:sub(begin,pos-1));pos=pos+1;c=s:sub(pos,pos);pos=pos+1
    local escape={['"']='"',['\\']='\\',['/']='/',b='\b',f='\f',n='\n',r='\r',t='\t'}
    if escape[c] then add(escape[c])
    elseif c=='u' then
     local raw=s:sub(pos,pos+3);if #raw~=4 or not raw:match('^%x%x%x%x$') then fail('bad Unicode escape') end
     local cp=tonumber(raw,16);pos=pos+4
     if cp>=55296 and cp<=56319 then
      if s:sub(pos,pos+1)~='\\u' then fail('unpaired surrogate') end;pos=pos+2
      raw=s:sub(pos,pos+3);if not raw:match('^%x%x%x%x$') then fail('bad low surrogate') end
      local lo=tonumber(raw,16);pos=pos+4;if lo<56320 or lo>57343 then fail('bad low surrogate') end
      cp=65536+(cp-55296)*1024+lo-56320
     elseif cp>=56320 and cp<=57343 then fail('unpaired surrogate') end
     add(utf8(cp))
    else fail('bad escape') end
    begin=pos
   elseif c:byte()<32 then fail('unescaped control character')
   else pos=pos+1;if pos-begin+bytes>J.limits.string then fail('string limit') end end
  end
  fail('unterminated string')
 end
 local read
 read=function(depth)
  nodes=nodes+1;if depth>J.limits.depth or nodes>J.limits.nodes then fail('complexity limit') end
  ws();local c=s:sub(pos,pos)
  if c=='"' then return str()
  elseif c=='{' then
   pos=pos+1;ws();local t,seen={},{};if s:sub(pos,pos)=='}' then pos=pos+1;return t end
   while true do
    ws();local key=str();if seen[key] then fail('duplicate key') end;seen[key]=true
    ws();if s:sub(pos,pos)~=':' then fail('expected colon') end;pos=pos+1;t[key]=read(depth+1);ws()
    c=s:sub(pos,pos);pos=pos+1;if c=='}' then return t elseif c~=',' then fail('expected comma or closing brace') end
   end
  elseif c=='[' then
   pos=pos+1;ws();local t=J.array();if s:sub(pos,pos)==']' then pos=pos+1;return t end
   while true do
    t[#t+1]=read(depth+1);ws();c=s:sub(pos,pos);pos=pos+1
    if c==']' then return t elseif c~=',' then fail('expected comma or closing bracket') end
   end
  elseif s:sub(pos,pos+3)=='true' then pos=pos+4;return true
  elseif s:sub(pos,pos+4)=='false' then pos=pos+5;return false
  elseif s:sub(pos,pos+3)=='null' then pos=pos+4;return J.null
  elseif c=='-' or c:match('%d') then
   local start=pos
   if c=='-' then pos=pos+1 end
   c=s:sub(pos,pos)
   if c=='0' then pos=pos+1;if s:sub(pos,pos):match('%d') then fail('leading zero') end
   elseif c:match('[1-9]') then repeat pos=pos+1 until not s:sub(pos,pos):match('%d')
   else fail('bad number') end
   if s:sub(pos,pos)=='.' then
    pos=pos+1;if not s:sub(pos,pos):match('%d') then fail('bad fraction') end
    repeat pos=pos+1 until not s:sub(pos,pos):match('%d')
   end
   c=s:sub(pos,pos)
   if c=='e' or c=='E' then
    pos=pos+1;c=s:sub(pos,pos);if c=='+' or c=='-' then pos=pos+1 end
    if not s:sub(pos,pos):match('%d') then fail('bad exponent') end
    repeat pos=pos+1 until not s:sub(pos,pos):match('%d')
   end
   if pos-start>32 then fail('number token limit') end
   local n=tonumber(s:sub(start,pos-1));if not U.finite(n) then fail('nonfinite number') end;return n
  end
  fail('unexpected token')
 end
 local value=read(0);ws();if pos~=#s+1 then fail('trailing data') end;return value
end
local function quote(s)
 assert(#s<=J.limits.string and validUtf8(s),'Invalid JSON string')
 return '"'..s:gsub('[%z\1-\31\\"]',function(c)
  if c=='"' then return '\\"' elseif c=='\\' then return '\\\\' end
  return string.format('\\u%04x',c:byte())
 end)..'"'
end
function J.encode(value)
 local seen={};local bytes,nodes=0,0;local out={}
 local function emit(s) bytes=bytes+#s;assert(bytes<=J.limits.bytes,'JSON byte limit');out[#out+1]=s end
 local write
 write=function(v,d)
  nodes=nodes+1;assert(d<=J.limits.depth and nodes<=J.limits.nodes,'JSON complexity limit')
  local kind=type(v)
  if v==J.null then emit('null')
  elseif kind=='string' then emit(quote(v))
  elseif kind=='number' then assert(U.finite(v),'Nonfinite JSON number');emit(string.format('%.17g',v))
  elseif kind=='boolean' then emit(v and 'true' or 'false')
  elseif kind=='table' then
   assert(not seen[v],'Cyclic JSON');seen[v]=true
   local count=0;local numeric=true
   for k in pairs(v) do count=count+1;if type(k)~='number' then numeric=false end end
   if J.isArray(v) or (numeric and count>0) then
    assert(count==#v,'Sparse JSON array');emit('[')
    for i=1,count do if i>1 then emit(',') end;assert(v[i]~=nil,'Sparse JSON array');write(v[i],d+1) end;emit(']')
   else
    local keys={};for k in pairs(v) do assert(type(k)=='string','JSON object key must be a string');keys[#keys+1]=k end
    table.sort(keys);emit('{')
    for i,k in ipairs(keys) do if i>1 then emit(',') end;emit(quote(k));emit(':');write(v[k],d+1) end;emit('}')
   end
   seen[v]=nil
  else error('Unsupported JSON value '..kind) end
 end
 write(value,0);return table.concat(out)..'\n'
end
return J
