-- E04 presentation source. Plain, versioned data; no renderer or simulation state.
local Project=require('src.studio_project')
local Codec=require('src.persistence')
local T={schema='roag-studio-theme/1'}
T.slots={'hud_panel','hp_icon','xp_icon','currency_icon','title_background',
    'pause_background','bitmap_font','hit_effect','boon_effect'}
local accepted={
    hud_panel={ui=true,ui_panel=true},
    hp_icon={ui=true,ui_icon=true},xp_icon={ui=true,ui_icon=true},
    currency_icon={ui=true,ui_icon=true},
    title_background={background=true,ui_background=true},
    pause_background={background=true,ui_background=true},
    bitmap_font={glyph=true,bitmap_font=true},
    hit_effect={effect=true,effect_sprite=true},
    boon_effect={effect=true,effect_sprite=true},
}
T.accepted=accepted
local function check(ok,message) if not ok then error(message,2) end end
local function int(n,lo,hi)
    return type(n)=='number' and n%1==0 and n>=lo and n<=hi
end
function T.new(project_id)
    check(Project.valid_project_id(project_id),'invalid theme project ID')
    local slots={};for _,id in ipairs(T.slots) do slots[id]='' end
    return {schema=T.schema,project_id=project_id,name='Game theme',slots=slots,
        panel={left=4,right=4,top=4,bottom=4},
        font={characters='HP0123456789:/',glyph_width=4,glyph_height=8,
            advance=5,line_height=10,spacing=1},
        background={mode='cover'},
        effects={hit={event='DamageTaken',duration_ms=180,scale=1,alpha=1,
            attachment='world',cap=8},
            boon={event='BoonActivated',duration_ms=220,scale=1,alpha=1,
                attachment='world',cap=8}}}
end
function T.validate(theme,assets)
    check(type(theme)=='table' and theme.schema==T.schema,'incompatible theme schema')
    check(Project.valid_project_id(theme.project_id),'invalid theme project ID')
    check(type(theme.name)=='string' and #theme.name>0 and #theme.name<=80
        and not theme.name:find('[%c]'),'invalid theme name')
    check(type(theme.slots)=='table','invalid theme slots')
    local by_id={}
    for _,asset in ipairs(assets or {}) do by_id[asset.id]=asset end
    local count=0
    for slot,id in pairs(theme.slots) do
        check(accepted[slot],'unknown theme slot: '..tostring(slot));count=count+1
        check(id=='' or Project.valid_id(id),'invalid asset ID for '..slot)
        if assets and id~='' then
            local asset=by_id[id]
            check(asset and accepted[slot][asset.kind] and
                (asset.source~='' or asset.derived~=''),
                'missing, empty or wrong-kind asset for '..slot..': '..id)
            check(int(asset.width,1,1024) and int(asset.height,1,1024),
                'invalid dimensions for '..id)
        end
    end
    check(count==#T.slots,'missing theme slots')
    local panel=theme.panel
    check(type(panel)=='table','missing panel insets')
    for _,side in ipairs({'left','right','top','bottom'}) do
        check(int(panel[side],0,256),'invalid panel '..side..' inset')
    end
    if assets and theme.slots.hud_panel~='' then
        local asset=by_id[theme.slots.hud_panel]
        check(panel.left+panel.right<=asset.width and
            panel.top+panel.bottom<=asset.height,'nine-slice insets exceed panel image')
    end
    local font=theme.font
    check(type(font)=='table' and type(font.characters)=='string' and
        #font.characters>=1 and #font.characters<=96,'invalid glyph mapping')
    local seen={}
    for i=1,#font.characters do
        local byte=font.characters:byte(i)
        check(byte>=32 and byte<=126 and not seen[byte],'duplicate or unsupported glyph')
        seen[byte]=true
    end
    check(int(font.glyph_width,1,128) and int(font.glyph_height,1,128) and
        int(font.advance,1,256) and int(font.line_height,1,256) and
        int(font.spacing,0,64),'invalid bitmap font metrics')
    if assets and theme.slots.bitmap_font~='' then
        local asset=by_id[theme.slots.bitmap_font]
        local columns=math.floor(asset.width/font.glyph_width)
        local rows=math.floor(asset.height/font.glyph_height)
        check(columns>0 and rows>0 and columns*rows>=#font.characters,
            'glyph mapping exceeds sheet dimensions')
    end
    check(type(theme.background)=='table' and
        ({center=true,fit=true,cover=true})[theme.background.mode],
        'invalid background mode')
    check(type(theme.effects)=='table','missing effect presets')
    for key,event in pairs({hit='DamageTaken',boon='BoonActivated'}) do
        local effect=theme.effects[key]
        check(type(effect)=='table' and effect.event==event,
            'invalid effect event: '..key)
        check(int(effect.duration_ms,40,1000) and type(effect.scale)=='number' and
            effect.scale>=0.25 and effect.scale<=4 and type(effect.alpha)=='number' and
            effect.alpha>=0 and effect.alpha<=1 and int(effect.cap,1,32) and
            effect.attachment=='world','invalid effect preset: '..key)
    end
    return true
end
function T.encode(theme)
    T.validate(theme)
    local bytes=Codec.encode(theme)
    check(#bytes<=65536,'theme is too large')
    return bytes
end
function T.decode(bytes)
    check(type(bytes)=='string' and #bytes<=65536,'invalid theme size')
    local theme=Codec.decode(bytes)
    T.validate(theme)
    return theme
end
-- Returns source and destination rectangles in row-major order.
function T.nine_slice(w,h,insets,dw,dh)
    check(int(w,1,4096) and int(h,1,4096) and int(dw,1,4096) and
        int(dh,1,4096),'invalid panel dimensions')
    local l,r,t,b=insets.left,insets.right,insets.top,insets.bottom
    check(l+r<=w and t+b<=h,'panel insets exceed image')
    local scale=math.min(1,dw/math.max(1,l+r),dh/math.max(1,t+b))
    local dx={0,l*scale,dw-r*scale,dw}
    local dy={0,t*scale,dh-b*scale,dh}
    local sx={0,l,w-r,w};local sy={0,t,h-b,h}
    local out={}
    for row=1,3 do for col=1,3 do
        local sw,sh=sx[col+1]-sx[col],sy[row+1]-sy[row]
        local tw,th=dx[col+1]-dx[col],dy[row+1]-dy[row]
        if sw>0 and sh>0 and tw>0 and th>0 then
            out[#out+1]={sx=sx[col],sy=sy[row],sw=sw,sh=sh,
                x=dx[col],y=dy[row],w=tw,h=th}
        end
    end end
    return out
end
function T.background_rect(mode,w,h,dw,dh)
    check(({center=true,fit=true,cover=true})[mode] and w>0 and h>0 and
        dw>0 and dh>0,'invalid background geometry')
    local scale=mode=='center' and 1 or mode=='fit' and
        math.min(dw/w,dh/h) or math.max(dw/w,dh/h)
    return (dw-w*scale)/2,(dh-h*scale)/2,w*scale,h*scale
end
function T.glyphs(font,w,h)
    local columns=math.floor(w/font.glyph_width)
    local rows=math.floor(h/font.glyph_height)
    check(columns>0 and rows>0 and columns*rows>=#font.characters,
        'glyph mapping exceeds sheet')
    local out={}
    for i=1,#font.characters do
        local c=font.characters:sub(i,i)
        out[c]={x=((i-1)%columns)*font.glyph_width,
            y=math.floor((i-1)/columns)*font.glyph_height,
            w=font.glyph_width,h=font.glyph_height}
    end
    return out
end
function T.effect_for(theme,event,visible)
    if not visible or type(event)~='table' or not event.x or not event.y then
        return nil
    end
    local key=event.kind=='DamageTaken' and 'hit' or
        event.kind=='BoonActivated' and 'boon' or nil
    if not key then return nil end
    local slot=key..'_effect'
    if theme.slots[slot]=='' then return nil end
    return theme.effects[key],theme.slots[slot]
end
return T
