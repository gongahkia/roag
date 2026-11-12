-- One catalogue-backed appearance. Simulation and player saves never select art.
local VisualAssets=require('src.visual_assets')
local P={default='catalog',label='Urizen catalog'}
function P.build(read,make_image,load_native)
    local generated,status=load_native(read)
    assert(not generated or generated.style=='catalog','unsupported game artwork')
    return VisualAssets.new(function()return nil end,make_image,generated),status
end
return P
