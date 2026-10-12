local Project=require('src.studio_project')
assert(love.filesystem.load('roeg_main.lua'))()
local original_load,original_draw,original_update=love.load,love.draw,love.update
local output=assert(os.getenv('ROEG_GUI_PUBLISH_OUTPUT'))
local phase,advance,pending=1,nil,false
local before_manifest
local function state() return assert(love.roegStudioState()) end
local function published()
    local file=assert(io.open(love.filesystem.getSource()..
        '/assets/studio/manifest.txt','rb'))
    local bytes=file:read('*a');file:close();return bytes
end
local function clear_field()
    for _=1,50 do love.keypressed('backspace') end
end
function love.load(args)
    original_load(args)
    love.keypressed('n');love.textinput('GUI Publish');love.keypressed('return')
    love.keypressed('i');love.keypressed('b');love.keypressed('s')
    assert(not state().dirty)
    love.keypressed('p');love.keypressed('d')
    assert(state().mode=='pipeline' and state().loose_plan,
        'preview failed: '..tostring(state().mode)..' '..tostring(state().message))
    assert(state().loose_plan.counts.create==2 and
        state().loose_plan.counts.overwrite==0)
    assert(not love.filesystem.getInfo('assets/studio/manifest.txt'),
        'dry run created a published manifest')
end
function love.update(dt)
    original_update(dt)
    if not advance then return end
    local action=advance;advance=nil;action()
end
function love.draw()
    original_draw()
    if pending then return end
    pending=true
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(output..'/phase'..phase..'.tga','wb'))
        assert(file:write(image:encode('tga'):getString()))
        assert(file:close())
        local current=phase;phase=phase+1;pending=false
        if current==1 then
            advance=function()
                love.mousepressed(300,575,1) -- Apply reviewed plan
                assert(state().mode=='publish_confirm')
            end
        elseif current==2 then
            advance=function()
                love.keypressed('return')
                assert(state().mode=='pipeline' and not state().loose_plan)
                before_manifest=published()
                assert(Project.decode(before_manifest).id=='gui_publish')
                love.keypressed('escape')
                love.keypressed('1');clear_field()
                love.textinput('Changed player');love.keypressed('return')
                love.keypressed('s');love.keypressed('p');love.keypressed('d')
                assert(state().loose_plan.counts.overwrite==1)
            end
        elseif current==3 then
            advance=function()
                love.mousepressed(300,575,1)
                assert(state().mode=='publish_confirm')
                love.keypressed('return')
                assert(state().mode=='publish_confirm' and
                    published()==before_manifest,
                    'Enter bypassed the overwrite confirmation')
                love.textinput('OVERWRITE')
            end
        elseif current==4 then
            advance=function()
                love.mousepressed(420,399,1) -- Confirm button
                assert(state().mode=='pipeline' and not state().loose_plan)
                local after=published()
                assert(after~=before_manifest and
                    Project.decode(after).assets[1].name=='Changed player')
                local backup='studio/publish-backups/gui_publish/manifest-0001.txt'
                assert(love.filesystem.read(backup)==before_manifest,
                    'previous published manifest was not backed up')
            end
        else advance=function() love.event.quit() end end
    end)
end
