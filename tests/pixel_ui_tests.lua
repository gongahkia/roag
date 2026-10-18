local UI=require('src.pixel_ui')
return function(test)
    test('focused menu list hitboxes select exact rows and reject gaps and edges',function()
        for i=1,5 do assert(UI.home_at(500,232+(i-1)*42+12)==i)end
        assert(not UI.home_at(500,270) and not UI.home_at(744,240))
        for i=1,9 do assert(UI.class_at(300,150+(i-1)*36+10)==i)end
        assert(not UI.class_at(500,200) and not UI.class_at(300,185))
    end)
    test('all sprite pack rows fit the shared title and pause panel layout',function()
        local count=#require('src.visual_packs').order
        assert(count==3)
        for i=1,count do
            local r=UI.settings_row(190,55,644,i)
            assert(r.y+r.h<55+422)
            assert(UI.settings_at(190,55,644,count,r.x+4,r.y+4)==i)
            assert(not UI.settings_at(190,55,644,count,r.x,r.y+r.h+1))
        end
    end)
    test('pause chest and route list targets match their visible choices',function()
        for i=1,3 do
            assert(UI.pause_at(500,252+(i-1)*44+8)==i)
            assert(UI.chest_at(300,178+(i-1)*76+8)==i)
            assert(UI.route_at(3,500,216+(i-1)*64+8)==i)
        end
        assert(not UI.route_at(1,500,300))
        assert(not UI.chest_at(600,190) and not UI.pause_at(200,270))
    end)
end
