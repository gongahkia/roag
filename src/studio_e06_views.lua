-- Small overlays on the existing Studio editors. No simulation or disk writes.
local V={}
local function box(x,y,w,h)
    local g=love.graphics
    g.setColor(.025,.045,.05,.97);g.rectangle('fill',x,y,w,h)
    g.setColor(.65,.8,.76);g.rectangle('line',x,y,w,h)
end
function V.help(workspace)
    local g=love.graphics;local w,h=g.getDimensions()
    local x,y=math.floor((w-740)/2),math.floor((h-420)/2)
    box(x,y,740,420)
    g.setColor(.94,.96,.85)
    local lines={
        'ROEG STUDIO · F10 / Esc closes help',
        'Project: N new · O open · S save · E pixels · W world · U UI · P publish',
        'Pixels: P pencil · E eraser · I pick · F fill · M drag selection',
        'Pixels: C recolor · Delete clear · O outline · Tab draft preview',
        'Pixels: Cmd/Ctrl+Z undo · Cmd/Ctrl+Shift+Z redo',
        'World: drag paint · T rotations · M mirror · P/H transformed preview',
        'World: F generated biome preview · O overlays · V validate',
        'Theme: Tab cycles title, HUD sizes, menu and effect preview',
        'Publish readiness is read-only. Save first; use the documented CLI.',
        'Game: F5 refreshes published visuals, preserving the current floor.',
        'Workspace: '..(workspace or ''),
    }
    for i,line in ipairs(lines) do g.print(line,x+20,y+18+(i-1)*38) end
end
function V.draft(canvas,handle)
    local g=love.graphics;local w,h=g.getDimensions()
    local x,y=math.floor((w-690)/2),math.floor((h-360)/2)
    box(x,y,690,360)
    canvas:refresh_texture()
    local texture=canvas.texture
    local dw,dh=texture:getWidth(),texture:getHeight()
    g.setColor(.96,.95,.85)
    g.print('DRAFT · current pixels, including unsaved strokes · Tab/click closes',x+18,y+14)
    local function checker(px,py,bw,bh)
        for cy=0,bh-1,12 do for cx=0,bw-1,12 do
            local v=(math.floor(cx/12)+math.floor(cy/12))%2==0 and .33 or .5
            g.setColor(v,v,v);g.rectangle('fill',px+cx,py+cy,
                math.min(12,bw-cx),math.min(12,bh-cy))
        end end
    end
    checker(x+20,y+65,190,205);checker(x+235,y+65,190,205)
    checker(x+450,y+65,210,205)
    g.setScissor(x+20,y+65,190,205)
    g.setColor(1,1,1);g.draw(texture,x+20,y+65)
    g.setScissor(x+235,y+65,190,205)
    local scale=math.max(1,math.min(8,math.floor(190/math.max(dw,dh))))
    g.draw(texture,x+235,y+65,0,scale,scale)
    g.setScissor(x+450,y+65,210,205)
    if handle then
        local size=math.min(4,200/math.max(handle.width,handle.height))
        if handle.quad then
            g.draw(handle.texture,handle.quad,x+450,y+65,0,size,size)
        else g.draw(handle.texture,x+450,y+65,0,size,size) end
    end
    g.setScissor()
    g.setColor(.85,.91,.86)
    g.print('Draft 1× (large art clipped)',x+20,y+285)
    g.print('Draft '..scale..'× · '..dw..'×'..dh,x+235,y+285)
    g.print('Published '..(handle and handle.source or 'fallback'),x+450,y+285)
    g.setColor(1,.75,.46)
    g.print('Preview is read-only. Save + publish before game F5.',x+20,y+322)
end
function V.pipeline(report,workspace,dirty,selected,room,theme_name)
    local g=love.graphics
    g.setColor(.92,.96,.86)
    g.print('PUBLISH READINESS · '..report.project..' · P rescan · Esc back · F10 help',24,20)
    g.print('Workspace: '..workspace,24,50)
    g.print('Project '..(dirty and 'UNSAVED · save first' or 'saved')..
        ' · selected '..(selected or 'none'),24,80)
    g.print('Room: '..(room or 'none')..' · Theme: '..(theme_name or 'none'),24,102)
    g.print('Loose: '..report.loose..'    World: '..report.world,24,127)
    g.print('Theme: '..report.theme..'    Atlas: '..report.atlas,24,151)
    local y=185
    for i,item in ipairs(report.assets) do
        if i>11 then g.print('… '..(#report.assets-11)..' more',24,y);break end
        g.print(item.id..' · source '..item.source..' · '..item.resolver,24,y)
        y=y+21
    end
    y=math.max(y+12,440)
    for i,msg in ipairs(report.warnings) do
        if i>2 then break end
        g.setColor(1,.72,.5);g.print(msg:sub(1,145),24,y);y=y+21
    end
    g.setColor(.72,.87,.81)
    g.print('Save -> loose -> world/theme -> atlas build -> atlas publish',24,540)
    g.print('Exact dry-run and --apply commands: docs/ROEG_STUDIO_E06.md',24,567)
    g.print('Separate packs are not one atomic transaction. Game F5 refreshes art.',24,592)
end
return V
