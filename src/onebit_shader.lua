-- One cached presentation filter; unsupported shaders never block the game.
local M={}
local source=[[
vec4 effect(vec4 color, Image texture, vec2 texture_coords, vec2 screen_coords)
{
    vec4 pixel = Texel(texture, texture_coords) * color;
    float gray = dot(pixel.rgb, vec3(0.2126, 0.7152, 0.0722));
    return vec4(gray, gray, gray, pixel.a);
}
]]
function M.new()return setmetatable({attempted=false},{__index=M})end
function M:begin(g,warn)
    if not self.attempted then
        self.attempted=true
        local okay,result=pcall(g.newShader,source)
        if okay then self.shader=result
        elseif warn then warn(result)end
    end
    if not self.shader then return nil,false end
    local previous=g.getShader()
    g.setShader(self.shader)
    return previous,true
end
function M:finish(g,previous,applied)
    if applied then g.setShader(previous)end
end
return M
