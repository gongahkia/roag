function love.conf(t)
    local c = require('config')
    -- Legacy save identity retained so existing colonies remain discoverable.
    t.identity = 'deepward_02'
    t.version = '11.5'
    t.console = true
    t.window.title = c.title .. ' / material colony prototype ' .. c.version
    t.window.width, t.window.height = c.windowWidth, c.windowHeight
    t.window.minwidth, t.window.minheight = 1040, 720
    t.window.resizable = true
    t.window.vsync = 1
    t.modules.audio, t.modules.physics, t.modules.joystick = false, false, false
    -- The launcher uses this no-gameplay probe to verify the actual save root
    -- before a windowed playtest. Normal launches keep their existing modules.
    if os.getenv('COSMONAUTS_PLAYTEST_PROBE') == '1' then
        t.modules.graphics, t.modules.window = false, false
    end
end
