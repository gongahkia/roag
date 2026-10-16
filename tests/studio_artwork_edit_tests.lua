local previous_utf8=package.preload.utf8
local previous_utf8_loaded=package.loaded.utf8
package.preload.utf8=function() return {} end
local App=require('src.studio_app')
package.preload.utf8=previous_utf8
package.loaded.utf8=previous_utf8_loaded
local Project=require('src.studio_project')
local Document=require('src.studio_document')
local PNG=require('src.studio_png')

return function(test)
    test('Artwork opens UI sprites and letters in the pixel workflow',function()
        local App=require('src.studio_app')
        local Inventory=require('src.studio_artwork_inventory')
        local Theme=require('src.studio_theme')
        local app=App.new({})
        app.project=Project.new('roag','ROAG')
        app.theme_pack=Theme.new('roag')
        app.slot_entries=Inventory.build(app.project,nil,app.theme_pack)
        app.slot_group='UI theme'
        local visible=app:visible_slots()
        for i,entry in ipairs(visible) do
            if entry.key=='theme:hp_icon' then app.slot_index=i;break end
        end
        app:open_selected_slot()
        assert(app.mode=='size' and app.pending_slot.binding=='core:ui/hp_icon')
        app.mode='slots';app.pending_slot=nil;app.slot_group='Letters & text'
        visible=app:visible_slots()
        for i,entry in ipairs(visible) do
            if entry.key=='core:glyph/ascii_097' then app.slot_index=i;break end
        end
        app:open_selected_slot()
        assert(app.mode=='size' and app.typed=='4x8' and
            app.pending_slot.kind=='glyph')
    end)
    test('Studio top navigation omits standalone Pixels and Publish tabs',function()
        local previous_love=_G.love
        local UI=require('src.studio_chrome')
        local original_button=UI.button
        local ok,reason=pcall(function()
            _G.love={graphics={getWidth=function() return 1024 end}}
            local labels={}
            UI.button=function(_,y,_,_,label)
                if y==5 then labels[#labels+1]=label end
            end
            local app=App.new({})
            app.project=Project.new('roag','ROAG');app.mode='project'
            app:draw_navigation()
            assert(table.concat(labels,',')=='Assets,Artwork,World,UI,Audio')
        end)
        UI.button=original_button
        _G.love=previous_love
        assert(ok,reason)
    end)
    test('Artwork stays the active tab during drawing and returns to its list',function()
        local app=App.new({})
        app.project=Project.new('roag','ROAG')
        app.mode='canvas';app.slot_return=true
        app.canvas={finish=function() end,dirty=function() return false end}
        app.canvas_asset={document='documents/saved.txt'}
        app.refresh_artwork=function(self) self.slot_entries={} end
        assert(app:view()=='artwork')
        app:navigate_history('back')
        assert(app.mode=='slots' and app:view()=='artwork')
        app.mode='canvas'
        assert(app:open_view('artwork') and app.mode=='slots')
    end)
    test('Assets Use in Game picks the selected art, not an old canvas',function()
        local app=App.new({})
        app.project=Project.new('roag','ROAG')
        local selected=Project.asset('studio:art/actor/ranger','Ranger','actor')
        selected.source='sources/ranger.png';selected.format='png'
        selected.width=16;selected.height=16
        app.project.assets[1]=selected;app.selected=1;app.mode='project'
        app.canvas_asset={id='studio:art/actor/old'}
        app.save=function() return true end
        app.refresh_artwork=function(self)
            self.slot_entries={{binding='core:actor/ranger',
                label='Ranger',key='core:actor/ranger'}}
        end
        app:asset_action('bind')
        assert(app.mode=='bind_asset' and app.binding_asset_id==selected.id)
    end)
    test('Pixels Save and Use in Game reaches a bound one-slot publish',function()
        local previous_love=_G.love
        local GuiPublish=require('src.studio_gui_publish')
        local original_new=GuiPublish.new
        local ok,reason=pcall(function()
            local saves,applies=0,0
            _G.love={graphics={getHeight=function() return 640 end},
                filesystem={getSource=function() return '/isolated-checkout' end}}
            local app=App.new({})
            app.project=Project.new('roag','ROAG')
            local asset=Project.asset('studio:art/actor/adventurer',
                'Adventurer','actor')
            asset.binding='core:actor/adventurer'
            app.project.assets[1]=asset;app.selected=1
            app.canvas_asset=asset;app.canvas={modal=nil}
            app.mode='canvas'
            app.save=function(self) saves=saves+1;return true end
            app.refresh_artwork=function(self)
                self.slot_entries={{binding='core:actor/adventurer',
                    label='Adventurer',key='core:actor/adventurer'}}
            end
            app.store={open=function() return app.project end}
            GuiPublish.new=function()
                return {plan_asset=function(_,project,id)
                    assert(project==app.project and id==asset.id)
                    return {binding=asset.binding,owner='player_sprite',
                        files={},counts={create=1,overwrite=1,unchanged=0}}
                end,apply_asset=function(_,project,id)
                    assert(project==app.project and id==asset.id)
                    applies=applies+1;return true,nil,'/isolated/backup'
                end}
            end
            app:mousepressed_inner(35,515,1) -- visible Save drawing button
            assert(saves==1 and app.mode=='canvas')
            app:mousepressed_inner(35,550,1) -- visible Use in Game button
            assert(saves==2 and app.mode=='bind_asset' and
                app:binding_choices()[1].binding==asset.binding)
            app:mousepressed_inner(35,555,1) -- choose selected binding
            assert(app.mode=='asset_publish_confirm' and applies==0)
            app:mousepressed_inner(35,510,1) -- explicit final confirmation
            assert(applies==1 and app.mode=='slots' and
                app.message:find('LIVE:',1,true))
        end)
        GuiPublish.new=original_new
        _G.love=previous_love
        assert(ok,reason)
    end)
    test('published artwork opens its pixels for editing; blank art stays explicit',function()
        local previous_love=_G.love
        local ok,reason=pcall(function()
            local rgba=string.char(255,0,0,255,0,0,255,255)
            local png=PNG.encode(2,1,rgba)
            local available=true
            _G.love={
                graphics={getWidth=function() return 1024 end,
                    getHeight=function() return 640 end},
                filesystem={
                    read=function(path)
                        assert(path=='assets/studio/sources/hero.png')
                        return available and png or nil
                    end,
                    newFileData=function(bytes) return bytes end,
                },
                image={newImageData=function(bytes)
                    assert(bytes==png)
                    return {getWidth=function() return 2 end,
                        getHeight=function() return 1 end,
                        getPixel=function(_,x)
                            if x==0 then return 1,0,0,1 end
                            return 0,0,1,1
                        end}
                end},
            }
            local published=Project.asset('studio:actor/hero','Hero','actor')
            published.binding='core:actor/adventurer'
            published.source='sources/hero.png'
            published.format='png';published.width=2;published.height=1
            local entry={key=published.binding,binding=published.binding,
                label='Actor/adventurer',kind='actor',group='Character',
                status='published elsewhere',published_asset=published}
            local function app()
                local result=App.new({})
                result.project=Project.new('roag','ROAG')
                result.slot_entries={entry};result.mode='slots'
                return result
            end
            local editor=app()
            editor:mousepressed(750,430,1)
            assert(editor.mode=='canvas' and #editor.project.assets==1)
            assert(editor.canvas_asset.binding==entry.binding and
                editor.canvas_asset.document=='')
            assert(editor.canvas.doc.width==2 and editor.canvas.doc.height==1)
            assert(Document.get(editor.canvas.doc,0,0)[1]==255 and
                Document.get(editor.canvas.doc,1,0)[3]==255,
                'published pixels must populate the editor')
            local blank=app()
            blank:mousepressed(750,480,1)
            assert(blank.mode=='size' and blank.typed=='2x1' and
                #blank.project.assets==0)
            available=false
            local missing=app()
            missing:mousepressed(750,430,1)
            assert(missing.mode=='slots' and #missing.project.assets==0,
                'missing published source must not create a blank asset')
        end)
        _G.love=previous_love
        assert(ok,reason)
    end)
end
