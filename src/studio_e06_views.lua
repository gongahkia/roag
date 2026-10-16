-- Small overlays on the existing Studio editors. No simulation or disk writes.
local V={}
local UI=require('src.studio_chrome')
local function box(x,y,w,h)
    UI.modal(x,y,w,h)
end
function V.help(workspace)
    local g=love.graphics;local w,h=g.getDimensions()
    local x,y=math.floor((w-900)/2),math.floor((h-550)/2)
    box(x,y,900,550)
    g.setColor(UI.color.text)
    local lines={
        'ROAG STUDIO · F10 / Esc closes help',
        'Top tabs: Assets / Artwork / World / UI / Audio · drawing opens from Artwork',
        'Artwork: Enter edits art · N new blank · click Search or press / · Tab group',
        'Pixels: P pencil · E eraser · I pick · F fill · M drag selection',
        'Pixels: C recolor · Delete clear · O outline · G reference · Tab preview',
        'Asset field 6: reference asset ID (same source dimensions); copy sets it',
        'Pixels: Cmd/Ctrl+Z undo · Cmd/Ctrl+Shift+Z redo',
        'Drawing canvas: Save drawing, then Use in Game to bind and publish one image',
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
    g.setColor(UI.color.text)
    g.print('DRAFT · current pixels, including unsaved strokes · Tab/click closes',x+18,y+14)
    local function checker(px,py,bw,bh)
        for cy=0,bh-1,12 do for cx=0,bw-1,12 do
            g.setColor((math.floor(cx/12)+math.floor(cy/12))%2==0 and
                UI.color.checker_dark or UI.color.checker_light)
            g.rectangle('fill',px+cx,py+cy,
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
    g.setColor(UI.color.muted)
    g.print('Draft 1× (large art clipped)',x+20,y+285)
    g.print('Draft '..scale..'× · '..dw..'×'..dh,x+235,y+285)
    g.print('Published '..(handle and handle.source or 'fallback'),x+450,y+285)
    g.setColor(UI.color.warning)
    g.print('Preview is read-only. Save + publish before game F5.',x+20,y+322)
end
local function button(x,y,label,enabled)
    UI.button(x,y,200,38,label,false,enabled)
end
function V.pipeline(report,workspace,dirty,selected,room,theme_name,
    plan,target,backup,message)
    local g=love.graphics
    local w,h=g.getDimensions()
    UI.begin('ROAG  /  PUBLISH',plan and
        ('LOOSE ART  ·  DRY RUN  ·  '..report.project) or
        ('READINESS  ·  '..report.project..'  ·  P rescan  ·  Esc assets'))
    UI.panel(18,72,math.min(988,w-36),math.max(460,h-211))
    g.setColor(UI.color.text)
    if plan then
        g.setColor(UI.color.muted);g.printf('TARGET CHECKOUT  '..target,30,80,960)
        g.setColor(UI.color.accent)
        g.print(('CREATE %d   REPLACE %d   UNCHANGED %d'):format(
            plan.counts.create,plan.counts.overwrite,plan.counts.unchanged),30,111)
        g.setColor(UI.color.muted)
        g.print('Esc readiness  ·  P rescan  ·  Only assets/studio/sources/*.png and manifest.txt are written.',30,138)
        UI.rule(30,167,math.min(964,w-60))
        local y=182
        for index,file in ipairs(plan.files) do
            if index>14 then
                g.print('… '..(#plan.files-14)..' more files',30,y);break
            end
            g.setColor(file.status=='overwrite' and UI.color.warning or UI.color.text)
            g.printf(('%s  %s  (%d bytes)'):format(file.status:upper(),
                file.path,#file.bytes),30,y,958)
            y=y+24
        end
        if plan.blocked then
            g.setColor(UI.color.warning);g.printf('Cannot apply: '..plan.blocked,30,502,958)
        elseif plan.counts.overwrite>0 then
            g.setColor(UI.color.warning)
            g.print('Replacement needs typed OVERWRITE confirmation; old manifest is backed up.',30,502)
        end
    else
        g.setColor(UI.color.muted);g.printf('WORKSPACE  '..workspace,30,80,958)
        g.setColor(dirty and UI.color.warning or UI.color.accent)
        g.print('PROJECT  '..(dirty and 'UNSAVED · save first' or 'SAVED')..
            '   /   SELECTED  '..(selected or 'none'),30,112)
        g.setColor(UI.color.muted)
        g.print('ROOM  '..(room or 'none')..'   /   THEME  '..(theme_name or 'none'),30,138)
        UI.rule(30,168,math.min(964,w-60))
        g.setColor(UI.color.text)
        g.print('LOOSE  '..report.loose..'     WORLD  '..report.world,30,181)
        g.print('THEME  '..report.theme..'     ATLAS  '..report.atlas,30,205)
        UI.rule(30,237,math.min(964,w-60))
        local y=250
        for i,item in ipairs(report.assets) do
            if i>9 then g.print('… '..(#report.assets-9)..' more',30,y);break end
            g.print(item.id..' · source '..item.source..' · '..item.resolver,30,y)
            y=y+21
        end
        y=math.max(y+12,454)
        for i,msg in ipairs(report.warnings) do
            if i>2 then break end
            g.setColor(UI.color.warning);g.print(msg:sub(1,145),30,y);y=y+21
        end
        if backup then
            g.setColor(UI.color.muted)
            g.printf('Last manifest backup: '..backup,30,507,958)
        end
    end
    UI.statusbar(548,math.max(92,h-548))
    button(24,558,'D  Preview loose files',true)
    button(240,558,'A  Apply reviewed plan',plan and not plan.blocked)
    g.setColor(UI.color.muted)
    g.printf(message or '',460,559,540)
    g.print('World/theme/atlas publishing remains separate · Game F5 refreshes art.',24,608)
end
function V.publish_confirm(plan,typed,message)
    local g=love.graphics
    box(260,206,504,228)
    g.setColor(UI.color.text)
    g.printf('PUBLISH LOOSE ART TO THIS CHECKOUT?',278,229,468,'center')
    if plan.counts.overwrite>0 then
        g.setColor(UI.color.warning)
        g.printf(('Replace %d file(s). A backup of the old manifest is saved.'):format(
            plan.counts.overwrite),278,265,468,'center')
        g.setColor(UI.color.text)
        g.printf('Type OVERWRITE exactly: '..typed,278,310,468,'center')
    else
        g.setColor(UI.color.muted)
        g.printf(('Create %d file(s). No existing file will change.'):format(
            plan.counts.create),278,282,468,'center')
    end
    UI.button(360,380,140,40,'Enter: publish',false,true)
    UI.button(520,380,140,40,'Esc: cancel',false,true)
    if message then g.printf(message,278,345,468,'center') end
end
return V
