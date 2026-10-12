-- Small overlays on the existing Studio editors. No simulation or disk writes.
local V={}
local function box(x,y,w,h)
    local g=love.graphics
    g.setColor(.025,.045,.05,.97);g.rectangle('fill',x,y,w,h)
    g.setColor(.65,.8,.76);g.rectangle('line',x,y,w,h)
end
function V.help(workspace)
    local g=love.graphics;local w,h=g.getDimensions()
    local x,y=math.floor((w-900)/2),math.floor((h-520)/2)
    box(x,y,900,520)
    g.setColor(.94,.96,.85)
    local lines={
        'ROEG STUDIO · F10 / Esc closes help',
        'Project: N new · O open · S save · E pixels · W world · U UI · P publish',
        'Pixels: P pencil · E eraser · I pick · F fill · M drag selection',
        'Pixels: C recolor · Delete clear · O outline · Tab draft preview',
        'Pixels: Cmd/Ctrl+Z undo · Cmd/Ctrl+Shift+Z redo',
        'Asset Binding (4): core:actor/adventurer/attack or /hurt',
        'Enemy Binding: core:enemy/<enemy_id>/attack or /hurt',
        'Boon Binding: core:boon/<boon_id>/icon or /detail',
        'World: core:terrain/<biome>/<floor|wall|water>/<shape>',
        'Shapes: center · edge_n/e/s/w · corner_nw/ne/se/sw · inner_corner_*',
        'Backdrop: core:background/<biome> · Drop: core:drop/currency',
        'World: drag paint · T rotations · M mirror · P/H transformed preview',
        'World: F generated biome preview · O overlays · V validate',
        'Theme: Tab cycles title, HUD sizes, menu and effect preview',
        'Publish: P readiness · D dry run · A apply after explicit confirmation.',
        'Game: F5 refreshes published visuals, preserving the current floor.',
        'Workspace: '..(workspace or ''),
    }
    for i,line in ipairs(lines) do g.print(line,x+20,y+18+(i-1)*28) end
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
local function button(x,y,label,enabled)
    local g=love.graphics
    g.setColor(enabled and .18 or .11,enabled and .31 or .17,
        enabled and .29 or .18)
    g.rectangle('fill',x,y,200,38)
    g.setColor(enabled and .72 or .4,enabled and .89 or .52,
        enabled and .81 or .48)
    g.rectangle('line',x,y,200,38)
    g.print(label,x+11,y+11)
end
function V.pipeline(report,workspace,dirty,selected,room,theme_name,
    plan,target,backup,message)
    local g=love.graphics
    g.setColor(.92,.96,.86)
    if plan then
        g.print('LOOSE IMAGE PUBLISH · DRY RUN · '..report.project,24,20)
        g.printf('Target checkout: '..target,24,51,976)
        g.print(('Create %d · Replace %d · Unchanged %d · Esc readiness · P rescan'):format(
            plan.counts.create,plan.counts.overwrite,plan.counts.unchanged),24,86)
        g.print('Only assets/studio/sources/*.png and assets/studio/manifest.txt are written.',24,114)
        local y=153
        for index,file in ipairs(plan.files) do
            if index>14 then
                g.print('… '..(#plan.files-14)..' more files',24,y);break
            end
            g.setColor(file.status=='overwrite' and 1 or .83,
                file.status=='overwrite' and .73 or .9,.77)
            g.printf(('%s  %s  (%d bytes)'):format(file.status:upper(),
                file.path,#file.bytes),24,y,970)
            y=y+24
        end
        if plan.blocked then
            g.setColor(1,.58,.45);g.printf('Cannot apply: '..plan.blocked,24,502,970)
        elseif plan.counts.overwrite>0 then
            g.setColor(1,.75,.48)
            g.print('Replacement needs typed OVERWRITE confirmation; old manifest is backed up.',24,502)
        end
    else
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
        if backup then
            g.setColor(.72,.87,.81)
            g.printf('Last manifest backup: '..backup,24,507,970)
        end
    end
    button(24,558,'D  Preview loose files',true)
    button(240,558,'A  Apply reviewed plan',plan and not plan.blocked)
    g.setColor(.72,.87,.81)
    g.printf(message or '',460,559,540)
    g.print('World/theme/atlas publishing remains separate · Game F5 refreshes art.',24,608)
end
function V.publish_confirm(plan,typed,message)
    local g=love.graphics
    box(260,206,504,228)
    g.setColor(.96,.92,.78)
    g.printf('PUBLISH LOOSE ART TO THIS CHECKOUT?',278,229,468,'center')
    if plan.counts.overwrite>0 then
        g.setColor(1,.73,.52)
        g.printf(('Replace %d file(s). A backup of the old manifest is saved.'):format(
            plan.counts.overwrite),278,265,468,'center')
        g.setColor(.94,.96,.86)
        g.printf('Type OVERWRITE exactly: '..typed,278,310,468,'center')
    else
        g.setColor(.84,.94,.86)
        g.printf(('Create %d file(s). No existing file will change.'):format(
            plan.counts.create),278,282,468,'center')
    end
    g.setColor(.18,.31,.29);g.rectangle('fill',360,380,140,40)
    g.rectangle('fill',520,380,140,40)
    g.setColor(.93,.96,.85)
    g.print('Enter: publish',371,393);g.print('Esc: cancel',535,393)
    if message then g.printf(message,278,345,468,'center') end
end
return V
