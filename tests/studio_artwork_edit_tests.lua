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
