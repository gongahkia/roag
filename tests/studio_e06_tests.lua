local Project=require('src.studio_project')
local Publish=require('src.studio_publish')
local Pipeline=require('src.studio_pipeline_status')
local PNG=require('src.studio_png')
local World=require('src.studio_world')
local Theme=require('src.studio_theme')
return function(test,equal)
    test('E06 readiness is read-only and detects changed draft versus published art',function()
        local project=Project.new('e06_fixture','E06')
        local actor=Project.asset('core:actor/adventurer','Hero','actor')
        actor.width=4;actor.height=4;actor.format='png'
        actor.source='sources/hero.png';actor.binding=actor.id
        project.assets={actor}
        local red=PNG.encode(4,4,string.rep(string.char(255,0,0,255),16))
        local blue=PNG.encode(4,4,string.rep(string.char(0,0,255,255),16))
        local sources={[actor.source]=red}
        local published={}
        local function source(path)return sources[path]end
        local function dest(path)return published[path]end
        local writes=0
        local world=World.new(project.id)
        local theme=Theme.new(project.id)
        local before=Pipeline.inspect(project,source,dest,world,theme)
        assert(before.loose:match('stale') and before.assets[1].source=='valid')
        assert(before.assets[1].resolver=='built-in fallback')
        for _,file in ipairs(Publish.plan(project,source,dest)) do
            published[file.path]=file.bytes;writes=writes+1
        end
        local current=Pipeline.inspect(project,source,dest,world,theme)
        assert(current.loose=='current' and current.assets[1].resolver=='published loose')
        local count=writes
        sources[actor.source]=blue
        local changed=Pipeline.inspect(project,source,dest,world,theme)
        assert(changed.loose:match('stale') and changed.assets[1].resolver=='published loose')
        assert(writes==count and published['assets/studio/manifest.txt'])
        sources[actor.source]=nil
        local broken=Pipeline.inspect(project,source,dest,world,theme)
        assert(broken.loose:match('invalid source') and
            broken.assets[1].source=='missing/draft')
        sources[actor.source]='not a PNG'
        local corrupt=Pipeline.inspect(project,source,dest,world,theme)
        assert(corrupt.loose:match('invalid source') and
            corrupt.assets[1].source=='missing/draft')
        assert(writes==count)
        published['assets/studio/manifest.txt']=Project.encode(
            Project.new('other_project','Other'))
        local other=Pipeline.inspect(project,source,dest,world,theme)
        assert(other.loose=='other project (other_project)')
    end)
    test('E06 readiness never changes game snapshot or RNG',function()
        local Game=require('src.game')
        local state=Game.new(93)
        local before=Game.snapshot(state)
        local project=Project.new('e06_empty','Empty')
        local report=Pipeline.inspect(project,function()return nil end,
            function()return nil end,World.new(project.id),Theme.new(project.id))
        assert(report.atlas=='unavailable without published visuals')
        equal(before,Game.snapshot(state))
    end)
end
