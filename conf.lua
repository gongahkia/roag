function love.conf(t)
    local c = require('config')
    t.identity = 'deepward_02'
    t.version = '11.5'
    t.console = true
    t.window.title = c.title .. ' / material colony prototype ' .. c.version
    t.window.width, t.window.height = c.windowWidth, c.windowHeight
    t.window.minwidth, t.window.minheight = 1040, 720
    t.window.resizable = true
    t.window.vsync = 1
    t.modules.audio, t.modules.physics, t.modules.joystick = false, false, false
end
