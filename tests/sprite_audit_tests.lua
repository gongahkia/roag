local Audit=require('src.sprite_audit')
return function(test,equal)
    test('native sprite inventory checks runtime coverage and distinguishes shared/static poses',function()
        local report=Audit.collect()
        assert(#report.requirements==491+2*#require('src.idea_boon_content').order)
        for _,pack in pairs(report.packs)do
            assert(#pack.missing==0 and #pack.bindings==#report.requirements)
            for kind in pairs(require('src.idea_interaction_content').art)do
                local found=false
                for _,b in ipairs(pack.bindings)do if b.id=='core:object/interaction_'..kind then found=true end end
                assert(found,'missing interaction art: '..kind)
            end
            assert(pack.actor_count==65 and pack.static_pose_aliases==130)
            assert(pack.wall_masks==47 and pack.water_masks==47)
        end
        assert(report.packs.onebit.actor_silhouettes==63)
        assert(report.packs.kenney==nil and report.sheets.micro==nil)
        assert(#report.packs.onebit.shared_actors==2)
        assert(not report.packs.onebit.usage.micro)
        assert(report.packs.onebit.usage.caves['146'] and report.packs.onebit.usage.rpg['60'])
        assert(report.packs.onebit.usage.caves['38'])
        assert(report.packs.onebit.usage.tiny['9'] and report.packs.onebit.usage.tiny['29'])
        assert(report.packs.onebit.usage.rpg['60'])
        assert(report.packs.onebit.usage.onebit['169'])
    end)
    test('sprite audit is deterministic and read only across repeated scans',function()
        local before=Audit.collect()
        equal(before,Audit.collect())
        assert(Audit.json(before)==Audit.json(Audit.collect()))
        assert(Audit.json({quote='"\n\\'})=='{"quote":"\\u0022\\u000a\\u005c"}')
    end)
    test('saved sprite inventory matches the current resolver mappings',function()
        local f=assert(io.open('docs/sprite-audit/inventory.json','rb'),
            'missing sprite catalogue; run make sprite-audit')
        local bytes=f:read('*a');f:close()
        local recorded=bytes:match('"mapping_checksum":%s*"([^"]+)"')
        assert(recorded==Audit.collect().mapping_checksum,
            'stale sprite catalogue; run make sprite-audit and include regenerated files')
    end)
end
