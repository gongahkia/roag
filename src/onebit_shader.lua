-- One cached presentation filter; unsupported shaders never block the game.
local M={}
local white={1,1,1}
local source=[[
extern vec3 ink;
vec4 effect(vec4 color, Image texture, vec2 texture_coords, vec2 screen_coords)
{
    vec4 pixel = Texel(texture, texture_coords) * color;
    float gray = dot(pixel.rgb, vec3(0.2126, 0.7152, 0.0722));
    return vec4(vec3(gray) * ink, pixel.a);
}
]]
function M.new()return setmetatable({attempted=false},{__index=M})end
function M:begin(g,warn)
    if not self.attempted then
        self.attempted=true
        local okay,result=pcall(function()
            local shader=g.newShader(source)
            shader:send('ink',white)
            return shader
        end)
        if okay then self.shader=result;self.ink=white
        elseif warn then warn(result)end
    end
    if not self.shader then return nil,false end
    local previous=g.getShader()
    g.setShader(self.shader)
    self:set_ink(g)
    return previous,true
end
function M:set_ink(g,ink)
    ink=ink or white
    local previous=self.ink or white
    local applied=self.shader and g.getShader()==self.shader or false
    if applied and (not self.ink or previous[1]~=ink[1] or
        previous[2]~=ink[2] or previous[3]~=ink[3]) then
        self.shader:send('ink',ink)
    end
    self.ink=ink
    return previous,applied
end
function M:finish(g,previous,applied)
    if applied then self:set_ink(g);g.setShader(previous)end
end
return M
