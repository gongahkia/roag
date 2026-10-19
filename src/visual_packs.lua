-- ROAG has one fixed appearance. Preferences and environment overrides from
-- retired selectable packs are intentionally neither read nor written.
local VisualAssets=require('src.visual_assets')
local P={default='onebit',label='Kenney 1-Bit'}
function P.build(read,make_image,load_native)
    local generated,status=load_native(read)
    assert(not generated or generated.style=='onebit','unsupported game artwork')
    -- Authored loose/atlas/theme content cannot select another visual style.
    return VisualAssets.new(function()return nil end,make_image,generated),status
end
return P
