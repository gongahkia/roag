local U = {}
function U.clamp(v, a, b) return math.max(a or 0, math.min(b or 1, v)) end
function U.finite(v) return type(v)=='number' and v==v and math.abs(v)<math.huge end
function U.integer(v, name, a, b)
    assert(U.finite(v) and v==math.floor(v) and v>=a and v<=b, (name or 'value')..' out of range')
    return v
end
function U.array(n, v) local a={} for i=1,n do a[i]=v or 0 end return a end
function U.copy(t) local r={} for k,v in pairs(t) do r[k]=v end return r end
-- No functions, resources or cyclic references in simulation state.
-- Underscore-prefixed entries are disposable caches, never authoritative state.
function U.deep(t)
    if type(t)~='table' then return t end
    local r={} for k,v in pairs(t) do
        if type(k)~='string' or k:sub(1,1)~='_' then r[k]=U.deep(v) end
    end return r
end
function U.keys(t)
    local a={} for k in pairs(t) do a[#a+1]=k end
    table.sort(a,function(x,y) if type(x)==type(y) then return x<y end return type(x)<type(y) end)
    return a
end
function U.distance(x,y,a,b) return math.abs(x-a)+math.abs(y-b) end
function U.rectDistance(x,y,x1,y1,x2,y2)
    return math.max(x1-x,0,x-x2)+math.max(y1-y,0,y-y2)
end
function U.csv(a)
    local s={} for i,v in ipairs(a) do s[i]='"'..tostring(v):gsub('"','""')..'"' end
    return table.concat(s,',')
end
function U.mean(a) local s=0 for _,v in ipairs(a) do s=s+v end return #a>0 and s/#a or 0 end
function U.percentile(a,p) local b=U.copy(a) table.sort(b) return b[math.max(1,math.ceil(#b*p))] or 0 end
return U
