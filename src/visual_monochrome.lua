-- Disposable source-preserving, two-tone derivatives for specifically approved
-- cross-pack art. Unlike RGB tinting, every visible pixel is actually neutral.
local M={threshold=.42,dark=.07,light=.90}
function M.pixel(r,g,b,a)
 local v=(r*.2126+g*.7152+b*.0722)>=M.threshold and M.light or M.dark
 return v,v,v,a
end
return M
