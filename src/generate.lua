-- Preserve original scenarios byte-for-byte in generate_legacy.lua. New methods
-- generate only new expeditions, never replace live terrain or historical states.
local C=require('config')
local Legacy=require('src.generate_legacy')
local Frontier=require('src.generation.frontier')
local G={presets={'frontier','cistern','dunes','frost'}}
function G.make(seed,preset,mode,width,height,options)
 preset=preset or C.preset;mode=mode or C.mode;width=width or C.width;height=height or C.height
 if preset=='frontier' then
  options=options or {layout=C.layout,climate=C.climate,openness=C.openness,biomeScale=C.biomeScale,features=C.features,density=C.density,crew=C.crew}
  return Frontier.make(seed,mode,width,height,options)
 end
 return Legacy.make(seed,preset,mode,width,height)
end
return G
