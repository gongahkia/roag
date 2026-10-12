-- Writes only inside the disposable app copy supplied by the GUI smoke.
package.path='./?.lua;./?/init.lua;'..package.path
local Project=require('src.studio_project')
local PNG=require('src.studio_png')
local Theme=require('src.studio_theme')
local ThemeStore=require('src.studio_theme_store')
local root=assert(arg[1])
local function write(path,bytes)
    local f=assert(io.open(root..'/'..path,'wb'))
    assert(f:write(bytes));assert(f:close())
end
local project=Project.new('e06_visuals','E06 visuals')
local actor=Project.asset('core:actor/adventurer','Actor','actor')
actor.binding=actor.id;actor.source='sources/actor.png'
actor.width=16;actor.height=16;actor.format='png'
local icon=Project.asset('studio:ui/heart','Heart','ui_icon')
icon.source='sources/heart.png';icon.width=16;icon.height=16;icon.format='png'
project.assets={actor,icon}
local function color(r,g,b)
    return PNG.encode(16,16,string.rep(string.char(r,g,b,255),256))
end
write('assets/studio/'..actor.source,color(220,40,35))
write('assets/studio/'..icon.source,color(30,205,65))
write('alternate-actor.png',color(35,75,225))
write('alternate-icon.png',color(230,195,40))
write('assets/studio/manifest.txt',Project.encode(project))
local theme=Theme.new(project.id)
theme.slots.hp_icon=icon.id
local plan=ThemeStore.plan_published(theme,function()return nil end)
write(plan.path,plan.bytes)
print('e06 fixture '..root)
