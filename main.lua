local Camera = require("src.camera")
local BoonContent = require("src.boon_content")
local Content = require("src.content")
local Classes = require("src.classes")
local Biomes = require("src.biomes")
local Game = require("src.game")
local Generation = require("src.generation")
local Playback = require("src.playback")
local HitFeedback = require("src.hit_feedback")
local CombatNumbers = require("src.combat_numbers")
local UIFont = require("src.ui_fonts")
local Rewards = require("src.rewards")
local Persistence = require("src.persistence")
local Profile = require("src.profile")
local AchievementContent = require("src.achievement_content")
local Achievements = require("src.achievements")
local storage = require("src.love_storage")
local VisualAssets = require("src.visual_assets")
local GeneratedVisuals = require("src.generated_visuals")
local VisualSlots = require("src.visual_slots")
local Spectator = require("src.spectator")
local StudioWorldRuntime = require("src.studio_world_runtime")
local StudioThemeRuntime = require("src.studio_theme_runtime")
local AudioRuntime = require("src.studio_audio_runtime")
local AudioCues = require("src.audio_cues")

local ZOOM_TILES={32,40,48,56,64}
local zoom_index=3 -- 150% by default; gameplay geometry remains 32px logical art.
local TILE=ZOOM_TILES[zoom_index]
local VIEWPORT = { x = 16, y = 16, width = 704, height = 608 }
local PANEL_X = 742
local BOON_ROW_Y,BOON_ROW_H=425,15

local state
local spectator_enabled=os.getenv('ROAG_SPECTATOR')=='1'
local spectator_free=false
local spectator_focus
local spectator_biome_index=1
local camera
local camera_state
local targeting -- nil, or a presentation-only { kind, dx, dy }.
local notice = "Ready"
local playback = Playback.new()
local hit_feedback = HitFeedback.new()
local combat_numbers = CombatNumbers.new()
local ui_body_font,ui_display_font
local minimap_visible = true
local showcase_index = 0
local boon_page = 0
local boon_detail_id
local floor_seed = 1
local selected_class_id=Classes.order[1]
local player_facing=1
local chest_menu -- presentation focus only; offers live in authoritative chest state.
local route_menu -- exit offers are plain data in the authoritative exit entity.
local reward_notices, reward_notice_time = {}, 0
local save_service = Persistence.new(storage)
local profile_service = Profile.new(storage)
local profile_status,profile,unlocks_view
local title, pause_menu, confirm_new, save_status = true, false, false, nil
local confirm_reset_seed
local persistence_message = ""
local diagnostics = os.getenv("ROAG_DIAGNOSTICS")=="1" and
    require("src.diagnostics").new() or nil
local diagnostics_update_ms=0
local visual_assets
local terrain_cache_assets,terrain_cache=nil,{}
local studio_world_status='absent'
local studio_theme,studio_theme_status
local refresh_pending=false
local refresh_result='not requested'
local visual_refresh_message=''
local next_world_pack
local visual_signature
local audio_runtime,audio_status
local function local_room_audio(kind)
    if not state or not state.generated_map or not audio_runtime then return nil end
    local player=Game.player(state).position
    for _,room in ipairs(state.generated_map.rooms or {}) do
        if room.template_id and room.template_id:match('^studio:room/') and
            player.x>=room.x and player.y>=room.y and
            player.x<room.x+room.width and player.y<room.y+room.height then
            local cue=AudioCues.room_cue(room.template_id,kind)
            if audio_runtime.bindings and audio_runtime.bindings[cue] then
                return cue
            end
        end
    end
end
local function audio_scene()
    if title or not state then return 'core:cue/music/title' end
    if state.game_over then
        return state.victory and 'core:cue/music/victory' or 'core:cue/music/defeat'
    end
    if state.finale then return 'core:cue/music/final_boss' end
    local biome=state.stage and state.stage.biome_id
    if not biome then return 'core:cue/music/title' end
    local local_music=local_room_audio('music')
    if local_music then return local_music end
    local tail=biome:match('([^/]+)$')
    local elite=state.stage.phase=='elite_active' or
        state.stage.phase=='extraction' or state.stage.phase=='pursuit'
    return 'core:cue/music/'..(elite and 'elite' or 'explore')..'/'..tail
end
local function audio_ambience()
    if title or not state or state.game_over or state.finale or not state.stage then
        return nil
    end
    return local_room_audio('ambient') or
        'core:cue/ambient/'..state.stage.biome_id:match('([^/]+)$')
end
local function sync_audio_scene()
    if audio_runtime then
        audio_runtime:set_scene(audio_scene())
        audio_runtime:set_ambience(audio_ambience())
    end
end
local showcase = {
    {name="Lightning", boons={{"storm_conductor","rare",2},
        {"static_footsteps","rare",1}}},
    {name="Cascade", boons={{"storm_conductor","legendary",2},
        {"detonation_bloom","rare",2}, {"resonant_wounds","rare",1},
        {"temporal_echo","uncommon",1}}},
    {name="Transformation", boons={{"crescent_reach","rare",1},
        {"thorn_mirror","rare",1}, {"turncoat_spark","rare",1}}},
    {name="Firestorm",boons={{"emberbrand","rare",1},{"flashpoint","rare",1},
        {"grave_dividend","uncommon",1}}},
    {name="Blade and Lunge",boons={{"longblade_geometry","rare",1},
        {"phantom_lunge","rare",1},{"pulse_step","uncommon",1}}},
}

local function apply_showcase()
    if state.meta then state.meta.eligible=false end
    local player = Game.player(state)
    for _, id in ipairs(BoonContent.order) do
        for _, rarity in ipairs(BoonContent.rarities) do
            Game.set_boon_stacks(state, player.id, id, rarity, 0)
        end
    end
    local selected = showcase[showcase_index]
    if selected then
        for _, entry in ipairs(selected.boons) do
            Game.set_boon_stacks(state, player.id, "core:boon/" .. entry[1], entry[2], entry[3])
        end
    end
end

local cardinal = {
    w = { 0, -1 }, up = { 0, -1 }, kp8 = { 0, -1 },
    s = { 0, 1 }, down = { 0, 1 }, kp2 = { 0, 1 },
    a = { -1, 0 }, left = { -1, 0 }, kp4 = { -1, 0 },
    d = { 1, 0 }, right = { 1, 0 }, kp6 = { 1, 0 },
}
local diagonal = {
    q = { -1, -1 }, kp7 = { -1, -1 },
    e = { 1, -1 }, kp9 = { 1, -1 },
    z = { -1, 1 }, kp1 = { -1, 1 },
    c = { 1, 1 }, kp3 = { 1, 1 },
}
local labels = {
    move = "Move", wait = "Wait", basic_attack = "Sword Strike",
    sweeping_slash = "Sweeping Slash", dash = "Dash", claim_chest = "Claim Chest",
    open_door = "Open Door",
    choose_route = "Choose Route",
}
local playback_cue_labels={
    DamageTaken="Damage", DamageBlocked="Blocked", EffectApplied="Effects",
    StatusApplied="Status", StatusTick="Status ticks",
    DelayedEffectScheduled="Echo queued", DelayedEffectResolved="Echo",
    TerrainTriggered="Terrain", TerrainBroken="Barricade broken",
}

local function visible_at(x,y)
    if spectator_enabled then return true end
    return not (state.generated_map or state.finale) or
        Game.is_visible(state,state.active_floor_id,x,y)
end

local function explored_at(floor_id,x,y)
    return spectator_enabled or Game.was_explored(state,floor_id,x,y)
end

local function visible_event(event)
    if not (state.generated_map or state.finale) then return true end
    if event.floor_id and event.floor_id~=state.active_floor_id then return false end
    local spatial=false
    if event.x and event.y and visible_at(event.x,event.y) then return true end
    if event.x and event.y then spatial=true end
    for _,cell in ipairs(event.target_cells or {}) do
        spatial=true
        if (not cell.floor_id or cell.floor_id==state.active_floor_id)
            and visible_at(cell.x,cell.y) then return true end
    end
    if not spatial and event.source_position and
        event.source_position.floor_id==state.active_floor_id and
        visible_at(event.source_position.x,event.source_position.y) then return true end
    return event.kind=="BoonGranted" or event.kind=="StatIncreased"
end

local function visible_events(events)
    if not (state.generated_map or state.finale) then return events end
    local shown={}
    for _,event in ipairs(events) do
        if visible_event(event) then shown[#shown+1]=event end
    end
    return shown
end
local stat_labels = {max_health="Max HP", damage="Damage", defense="Defense",
    speed="Speed", crit_chance="Crit chance", crit_damage="Crit damage"}

local function upgrade_notice(event)
    local old, new = event.old_value, event.new_value
    if event.stat == "crit_chance" then
        old, new = ("%.1f%%"):format(old / 10000), ("%.1f%%"):format(new / 10000)
    elseif event.stat == "crit_damage" then
        old, new = old .. "%", new .. "%"
    end
    return ("Level %d: %s %s → %s"):format(event.level,
        stat_labels[event.stat] or event.stat, old, new)
end

local function owned_boon_ids()
    if not state then return {} end
    local player=Game.player(state)
    local result={}
    for _,id in ipairs(BoonContent.order) do
        local stacks=player.boons and player.boons[id]
        if stacks then
            for _,rarity in ipairs(BoonContent.rarities) do
                if (stacks[rarity] or 0)>0 then
                    result[#result+1]=id;break
                end
            end
        end
    end
    return result
end

local function reset_camera()
    local map = Content.map_for(state)
    local position = Game.player(state).position
    if spectator_enabled then spectator_focus={x=position.x,y=position.y} end
    camera = Camera.new(map, VIEWPORT, TILE, position.x, position.y)
    hit_feedback = HitFeedback.new()
    camera_state = state
end

local function zoom_by(steps)
    local next_index=math.max(1,math.min(#ZOOM_TILES,zoom_index+steps))
    if next_index==zoom_index then return end
    zoom_index=next_index
    TILE=ZOOM_TILES[zoom_index]
    if state and camera then
        local position=Game.player(state).position
        local x,y=Playback.position(playback,state.player_id,
            position.floor_id,position.x,position.y)
        if spectator_enabled and spectator_free and spectator_focus then
            x,y=spectator_focus.x,spectator_focus.y
        end
        Camera.set_tile(camera,TILE,x,y)
    end
    notice=('Zoom %d%% — no world time'):format(TILE*100/32)
end

local function visual_fingerprint(visuals,theme,world)
    local sum=require('src.studio_atlas').checksum
    local parts={visuals.status,visuals.atlas_status or '',
        visuals.generated and visuals.generated.signature or '',
        theme and require('src.studio_theme').encode(theme.theme) or '',
        world and require('src.studio_world').encode(world) or ''}
    if visuals.read then
        parts[#parts+1]=sum(visuals.read('assets/studio/manifest.txt') or '')
    end
    local ids={}
    for id in pairs(visuals.by_id or {}) do ids[#ids+1]=id end
    table.sort(ids)
    for _,id in ipairs(ids) do
        local asset=visuals.by_id[id]
        parts[#parts+1]=id
        parts[#parts+1]=sum(visuals.read('assets/studio/'..asset.source) or '')
    end
    return sum(table.concat(parts,'|'))
end

local function load_generated_visuals(read)
    if os.getenv('ROAG_GENERATED_ART')~='1' then return nil,'disabled' end
    local ok,visuals,status=pcall(GeneratedVisuals.new,read,
        function(bytes,path)
            local data=love.filesystem.newFileData(bytes,path)
            local pixels=love.image.newImageData(data)
            local image=love.graphics.newImage(pixels)
            image:setFilter('nearest','nearest')
            return image
        end)
    if not ok then return nil,'rejected: '..tostring(visuals) end
    return visuals,status
end

function love.load(args)
    ui_body_font,ui_display_font=UIFont.load()
    love.graphics.setFont(ui_body_font)
    local audio_ok,audio=pcall(AudioRuntime.new,
        function(path)return love.filesystem.read(path)end,
        function(bytes,sound,source_type)
            local data=love.filesystem.newFileData(bytes,sound.id..'.'..sound.format)
            return love.audio.newSource(data,source_type)
        end)
    if audio_ok then audio_runtime=audio;audio_status=audio.status
    else audio_status='silent: '..tostring(audio) end
    local generated,generated_status=load_generated_visuals(function(path)
        return love.filesystem.read(path)
    end)
    if generated_status:match('^rejected') then
        print('Generated art fallback: '..generated_status)
    end
    visual_assets=VisualAssets.new(function(path)
        return love.filesystem.read(path)
    end,function(bytes,asset)
        local data=love.filesystem.newFileData(bytes,asset.source)
        local pixels=love.image.newImageData(data)
        local image=love.graphics.newImage(pixels)
        image:setFilter(asset.filter,asset.filter)
        return image,pixels
    end,generated)
    studio_theme,studio_theme_status=StudioThemeRuntime.inspect(function(path)
        return love.filesystem.read(path)
    end,visual_assets)
    local world_pack
    world_pack,studio_world_status=StudioWorldRuntime.inspect(function(path)
        return love.filesystem.read(path)
    end)
    next_world_pack=world_pack
    visual_assets:check_atlas()
    visual_signature=visual_fingerprint(visual_assets,studio_theme,world_pack)
    Generation.install_studio_pack(world_pack)
    if spectator_enabled then
        title=false
        state=Spectator.new(floor_seed,Biomes.order[spectator_biome_index],
            selected_class_id)
        notice=generated and 'SPECTATOR · generated art loaded · F6 camera' or
            ('SPECTATOR · art fallback: '..tostring(generated_status))
        reset_camera()
    elseif os.getenv("ROAG_GUI_AUTOSTART") == "1" then
        title=false
        state=Game.new_stage(floor_seed)
        apply_showcase()
        reset_camera()
    else
        save_status=save_service:inspect()
        persistence_message=save_status.reason or ""
        profile_status=profile_service:inspect()
        profile=profile_status.profile
        if profile_status.status=="corrupt" or profile_status.status=="incompatible" then
            persistence_message="Profile "..profile_status.status.."; progress preserved. "..
                (profile_status.reason or "")
        elseif profile_status.status=="recovered" then
            persistence_message="Recovered previous valid profile generation"
        end
        if save_status.status=="pending_terminal" then
            local pending=save_status.state.meta and save_status.state.meta.pending or {}
            local ok,updated=profile_service:commit_awards(pending)
            if ok then
                profile=updated
                local done,why=save_service:tombstone()
                if done then save_status=save_service:inspect()
                else persistence_message="Terminal save needs retry: "..tostring(why) end
            else
                persistence_message="Terminal unlock recovery needs retry: "..tostring(updated)
            end
        end
        if studio_world_status~='absent' and studio_world_status~='loaded' then
            persistence_message=(persistence_message~='' and persistence_message..' · ' or '')..
                'Studio world: '..studio_world_status
        end
        if studio_theme_status~='absent' and studio_theme_status~='loaded' then
            persistence_message=(persistence_message~='' and persistence_message..' · ' or '')..
                'Studio theme: '..studio_theme_status
        end
        if studio_theme and studio_theme.warning then
            persistence_message=(persistence_message~='' and persistence_message..' · ' or '')..
                'Studio theme fallback: '..studio_theme.warning
        end
    end
    love.graphics.setBackgroundColor(0.055, 0.075, 0.075)
    sync_audio_scene()
end

-- Build all replacement GPU handles before publishing any of them.  This is
-- presentation state only; the active concrete floor is never regenerated.
local function refresh_visuals()
    local read=function(path) return love.filesystem.read(path) end
    local make_image=function(bytes,asset)
        local data=love.filesystem.newFileData(bytes,asset.source)
        local pixels=love.image.newImageData(data)
        local image=love.graphics.newImage(pixels)
        image:setFilter(asset.filter,asset.filter)
        return image,pixels
    end
    local ok,candidate,theme,theme_status,world,world_status=pcall(function()
        local generated,generated_status=load_generated_visuals(read)
        assert(not generated_status:match('^rejected'),
            'generated art: '..generated_status)
        local visuals=VisualAssets.new(read,make_image,generated)
        assert(visuals.status=='loaded' or visuals.status=='absent' or
            visuals.status=='generated',
            'visual manifest: '..visuals.status)
        if visuals.status=='loaded' then
            local ids={}
            for id in pairs(visuals.by_id) do ids[#ids+1]=id end
            table.sort(ids)
            for _,id in ipairs(ids) do
                assert(visuals:resolve_id(id),'missing or invalid visual '..id)
            end
        end
        local new_theme,new_theme_status=StudioThemeRuntime.inspect(read,visuals)
        assert(new_theme_status=='loaded' or new_theme_status=='absent' or
            tostring(new_theme_status):match('^recovered'),
            'theme: '..tostring(new_theme_status))
        local new_world,new_world_status=StudioWorldRuntime.inspect(read)
        assert(new_world_status=='loaded' or new_world_status=='absent' or
            tostring(new_world_status):match('^recovered'),
            'world: '..tostring(new_world_status))
        if new_world then
            local published_assets=visuals.atlas_project and
                visuals.atlas_project.assets or {}
            local valid,why=pcall(require('src.studio_world').validate,
                new_world,published_assets)
            assert(valid,'world: '..tostring(why))
        end
        return visuals,new_theme,new_theme_status,new_world,new_world_status
    end)
    if not ok then
        refresh_result='rejected: '..tostring(candidate)
        local brief=tostring(candidate):find('theme:',1,true) and 'theme pack invalid' or
            tostring(candidate):find('world:',1,true) and 'world pack invalid' or
            tostring(candidate):find('visual manifest:',1,true) and 'visual manifest invalid' or
            'missing/corrupt visual'
        notice='F5 art rejected: '..brief
        visual_refresh_message=refresh_result
        print('Visual refresh '..refresh_result)
        return false
    end
    local signature=visual_fingerprint(candidate,theme,world)
    local same=signature==visual_signature
    visual_assets,studio_theme,studio_theme_status=candidate,theme,theme_status
    studio_world_status=world_status
    -- A run owns concrete floor geometry. World templates are installed only
    -- for a fresh run from the title, never into an existing run.
    next_world_pack=world
    visual_signature=signature
    if title and not state then Generation.install_studio_pack(world) end
    local backing=candidate.atlas_status
    if candidate.status=='absent' then backing='built-in fallback'
    elseif candidate.status=='generated' then backing='generated atlas'
    elseif backing~='loaded' and backing~='recovered previous atlas generation' then
        backing='published loose ('..tostring(backing)..')'
    else backing='atlas' end
    refresh_result=(same and 'checked' or 'refreshed')..' · '..backing..
        ((state and world_status=='loaded') and ' · world geometry deferred' or '')..
        (theme and theme.warning and ' · theme fallback: '..theme.warning or '')
    local brief=candidate.status=='absent' and 'built-in' or
        (backing=='atlas' and 'atlas' or 'loose (atlas unavailable)')
    notice='F5 art '..(same and 'unchanged' or 'refreshed')..' · '..brief..
        (theme and theme.warning and ' · theme fallback' or '')
    visual_refresh_message=refresh_result
    if not same then print('Visual refresh '..refresh_result) end
    collectgarbage('step',200)
    return true
end

function love.update(dt)
    if refresh_pending then
        refresh_pending=false
        refresh_visuals()
        if audio_runtime then
            local _,reason=audio_runtime:refresh();audio_status=reason
            if tostring(reason):match('^rejected') then
                notice='F5 audio: '..tostring(reason)
                print(notice)
            end
        end
    end
    if audio_runtime then
        sync_audio_scene()
        audio_runtime:set_paused(pause_menu)
        audio_runtime:update(dt)
    end
    if title or not state then return end
    if camera_state ~= state then reset_camera() end
    Playback.update(playback, dt)
    CombatNumbers.update(combat_numbers,playback,dt,state.player_id,
        state.active_floor_id)
    local position = Game.player(state).position
    local visual_x,visual_y=Playback.position(playback,state.player_id,
        position.floor_id,position.x,position.y)
    if spectator_enabled and spectator_free and spectator_focus then
        Camera.update(camera,dt,spectator_focus.x,spectator_focus.y)
    else
        Camera.update(camera, dt, visual_x, visual_y)
    end
    HitFeedback.update(hit_feedback, playback, dt, state.player_id,
        state.active_floor_id)
    Camera.set_presentation_offset(camera, HitFeedback.offset(hit_feedback))
    if #reward_notices > 0 then
        reward_notice_time = reward_notice_time - math.max(0, dt)
        while reward_notice_time <= 0 and #reward_notices > 0 do
            table.remove(reward_notices, 1)
            if #reward_notices > 0 then
                reward_notice_time = reward_notice_time + 3
            else
                reward_notice_time = 0
            end
        end
    end
end

local function begin_new(seed)
    seed=seed or floor_seed
    if save_service:inspect().status=="pending_terminal" then
        persistence_message="Terminal unlock is pending; retry recovery before New Run"
        return
    end
    local current=profile_service:inspect()
    if current.status=="corrupt" or current.status=="incompatible" then
        persistence_message="Cannot replace incompatible/corrupt profile; progress preserved"
        return
    end
    local run_id,updated=profile_service:allocate_run()
    if not run_id then
        persistence_message="New Run profile write failed: "..tostring(updated)
        return
    end
    Generation.install_studio_pack(next_world_pack)
    local candidate=Game.new_stage(seed,{meta=Achievements.new_run(true,run_id),
        available_boons=Achievements.pool(updated.earned),multifloor=true,
        class_id=selected_class_id})
    local ok,reason=save_service:replace(candidate)
    if not ok then
        persistence_message="New Run save failed: "..tostring(reason)
        return
    end
    state=candidate
    player_facing=1
    profile=updated
    profile_status=profile_service:inspect()
    floor_seed=seed
    title,confirm_new,pause_menu=false,false,false
    persistence_message=reason or ""
    targeting,chest_menu,route_menu=nil,nil,nil
    playback=Playback.new()
    combat_numbers=CombatNumbers.new()
    notice=("Seed %d — new run"):format(floor_seed)
    reset_camera()
end

local function continue_run()
    local loaded,found=save_service:load()
    save_status=found
    if not loaded then
        persistence_message=found.reason or found.status
        return
    end
    state=loaded
    player_facing=1
    selected_class_id=Game.player(state).definition_id
    floor_seed=state.root_seed or state.run_seed
    title,confirm_new,pause_menu=false,false,false
    persistence_message=found.status=="recovered"
        and "Recovered previous valid save generation" or ""
    targeting,chest_menu,route_menu=nil,nil,nil
    playback=Playback.new() -- resolved presentation never replays authoritative effects.
    combat_numbers=CombatNumbers.new()
    notice="Continued saved run"
    reset_camera()
end

local function checkpoint(force)
    if spectator_enabled then return true end -- disposable visual inspection only
    if not force and os.getenv("ROAG_GUI_AUTOSTART") == "1" then return true end
    local pending=state.meta and state.meta.pending or {}
    if state.game_over and next(pending) then
        local saved,reason=save_service:pending_terminal(state)
        if not saved then
            persistence_message="Terminal save failed: "..tostring(reason)
            return false
        end
        local committed,updated=profile_service:commit_awards(pending)
        if not committed then
            persistence_message="Unlock pending; retry required: "..tostring(updated)
            return false
        end
        profile=updated
        local done,why=save_service:tombstone()
        if not done then
            persistence_message="Terminal save needs retry: "..tostring(why)
            return false
        end
        state.meta.pending={}
        save_status=save_service:inspect()
        return true
    end
    local ok,reason=save_service:save(state)
    if not ok then
        persistence_message="Autosave failed: "..tostring(reason)
        return false
    end
    if next(pending) then
        local committed,updated=profile_service:commit_awards(pending)
        if not committed then
            persistence_message="Unlock pending; retry required: "..tostring(updated)
            return false
        end
        profile=updated
        state.meta.pending={}
        local acknowledged,why=save_service:save(state)
        if not acknowledged then
            persistence_message="Unlock saved; run checkpoint retry: "..tostring(why)
        end
    end
    profile_status=profile_service:inspect()
    return true
end

local function submit(intent)
    local old_floor=state.active_floor_id
    local committed, reason, events, preview = Game.submit(state, intent)
    if committed then
        if old_floor~=state.active_floor_id then
            playback=Playback.new() -- historical attacks belong to the retired floor.
            combat_numbers=CombatNumbers.new()
            reset_camera()
        end
        local shown=visible_events(events)
        Playback.add(playback, shown, state.player_id)
        if audio_runtime then
            for _,event in ipairs(shown) do audio_runtime:event(event,state) end
            sync_audio_scene()
        end
        for _, event in ipairs(events) do
            if event.kind == "StatIncreased" then
                reward_notices[#reward_notices + 1] = upgrade_notice(event)
                if reward_notice_time == 0 then reward_notice_time = 3 end
            elseif event.kind == "CurrencyCollected" then
                notice = ("Collected %d currency"):format(event.amount)
                if visual_assets and
                    visual_assets.bindings['core:drop/currency/detail'] then
                    reward_notices[#reward_notices+1]={text=notice,
                        binding='core:drop/currency/detail'}
                    if reward_notice_time==0 then reward_notice_time=3 end
                end
            elseif event.kind == "BoonGranted" then
                if visual_assets and (visual_assets.bindings[event.boon_id..'/detail']
                    or visual_assets.bindings[event.boon_id..'/icon']) then
                    reward_notices[#reward_notices+1]={
                        text='Gained '..BoonContent.get(event.boon_id).name,
                        binding=event.boon_id..'/detail',
                        fallback=event.boon_id..'/icon'}
                    if reward_notice_time==0 then reward_notice_time=3 end
                end
            elseif event.kind=="AchievementEarned" then
                reward_notices[#reward_notices+1]="Unlocked: "..
                    AchievementContent.get(event.achievement_id).name.." → "..
                    BoonContent.get(event.reward_boon_id).name
                if reward_notice_time==0 then reward_notice_time=3 end
            end
        end
        local milestone=state.game_over or
            old_floor~=state.active_floor_id or
            (state.last_action and state.last_action.kind=="claim_chest")
        for _,event in ipairs(events) do
            if event.kind=="EliteDefeated" or event.kind=="PursuerArrived"
                or event.kind=="AchievementEarned" then
                milestone=true
            end
        end
        if milestone then checkpoint() end
        local action = state.last_action
        if action.kind == "dash" then
            notice = ("Dash: %d tiles, +%d time"):format(action.distance, action.cost)
        elseif action.kind == "claim_chest" then
            notice = ("Claimed %s (%s), +%d time"):format(
                BoonContent.get(action.boon_id).name, action.rarity, action.cost)
        elseif action.kind == "open_door" then
            notice = ("Opened door: +%d time"):format(action.cost)
        elseif action.kind == "choose_route" then
            notice = state.finale and "Final Sanctum — Hollow Regent"
                or ("Stage %d: %s"):format(state.stage.index,
                    Biomes.get(state.stage.biome_id).name)
        else
            notice = ("%s: +%d time"):format(labels[action.kind], action.cost)
        end
        for _, event in ipairs(events) do
            if event.kind == "CurrencyCollected" then
                notice = notice .. ("; +%d currency"):format(event.amount)
            elseif event.kind == "SupplyCollected" then
                notice = notice .. ("; +%d HP (%s)"):format(
                    event.actual_gain,event.supply_kind)
            elseif event.kind == "SecretDiscovered" then
                notice = notice .. "; hidden cache found"
            elseif event.kind == "FloorChanged" then
                notice = ("Stairs to floor %d/2: +%d time"):format(
                    state.active_floor_index,state.last_action.cost)
            elseif event.kind == "EliteDefeated" then
                notice = "Thorn Warden defeated — free chest and exit unlocked"
            elseif event.kind == "PursuerArrived" then
                notice = "The Extraction Pursuer has arrived! Reach the exit."
            elseif event.kind == "PursuerSpawnFailed" then
                notice = "Pursuer route unavailable — exit remains open"
            elseif event.kind == "ElitePhaseChanged" then
                notice = "Thorn Warden enters phase two"
            elseif event.kind == "RegentPhaseChanged" then
                notice = "Hollow Regent enters phase two"
            elseif event.kind == "RegentDefeated" then
                notice = "The Hollow Regent falls"
            elseif event.kind == "Victory" then
                notice = "VICTORY — the Hollow Regent is defeated"
            end
        end
    else
        if reason == "chest" and preview and preview.chest_id then
            chest_menu = Game.chest_options(state, preview.chest_id)
            if chest_menu then
                chest_menu.selected = 1
                notice = "Chest offers ready — no time"
            end
        elseif reason=="exit" and preview and preview.exit_id then
            route_menu=Game.route_options(state,preview.exit_id)
            if route_menu then notice="Routes ready — no world time" end
        else
            notice = (reason or "Invalid action") .. " — no time"
        end
    end
    return committed
end

local function spectator_restart()
    state=Spectator.new(floor_seed,Biomes.order[spectator_biome_index],
        selected_class_id)
    playback=Playback.new()
    combat_numbers=CombatNumbers.new()
    targeting,chest_menu,route_menu,boon_detail_id=nil,nil,nil,nil
    pause_menu=false
    reward_notices,reward_notice_time={},0
    reset_camera()
    notice=('SPECTATOR · %s · seed %d · no saves'):format(
        Biomes.get(Biomes.order[spectator_biome_index]).name,floor_seed)
end

function love.keypressed(key, _, isrepeat)
    if isrepeat and not (spectator_enabled and spectator_free and cardinal[key]) then
        return
    end
    if key=='f5' then refresh_pending=true;return end
    if key=="f3" then
        if diagnostics then diagnostics=nil
        else diagnostics=require("src.diagnostics").new() end
        return
    end
    if spectator_enabled and state then
        if key=='f6' then
            spectator_free=not spectator_free
            if spectator_free then
                targeting,chest_menu,route_menu,boon_detail_id=nil,nil,nil,nil
            end
            local p=Game.player(state).position
            spectator_focus={x=p.x,y=p.y}
            Camera.set_tile(camera,TILE,p.x,p.y)
            notice=spectator_free and 'FREE CAMERA · WASD pan · Shift fast · F6 return' or
                'GOD PLAY · invulnerable · F6 free camera'
            return
        elseif key=='f7' then
            spectator_biome_index=Spectator.next_index(Biomes.order,
                spectator_biome_index)
            spectator_restart()
            return
        elseif key=='f8' then
            local ok,reason=Spectator.switch_floor(state)
            if ok then
                playback=Playback.new()
                combat_numbers=CombatNumbers.new()
                targeting,chest_menu,route_menu,boon_detail_id=nil,nil,nil,nil
                reset_camera()
                notice=('SPECTATOR · floor %d/2 · no world time'):format(
                    state.active_floor_index or 1)
            else notice='Spectator floor: '..tostring(reason) end
            return
        elseif key=='f9' then
            local index=1
            for i,id in ipairs(Classes.order) do
                if id==selected_class_id then index=i;break end
            end
            selected_class_id=Classes.order[Spectator.next_index(Classes.order,index)]
            spectator_restart()
            return
        elseif key=='n' or key=='m' then
            if key=='m' then floor_seed=floor_seed%2147483646+1 end
            spectator_restart()
            return
        elseif key=='escape' then
            if targeting or chest_menu or route_menu or boon_detail_id then
                targeting,chest_menu,route_menu,boon_detail_id=nil,nil,nil,nil
                notice='Inspection menu closed · no world time'
            else love.event.quit() end
            return
        elseif spectator_free then
            local direction=cardinal[key]
            if direction then
                spectator_focus=Spectator.camera_step(state,spectator_focus,
                    direction[1],direction[2],
                    love.keyboard.isDown('lshift','rshift'))
                notice=('FREE CAMERA · %d,%d · no world time'):format(
                    spectator_focus.x,spectator_focus.y)
            elseif key=='=' or key=='+' or key=='kp+' then zoom_by(1)
            elseif key=='-' or key=='kp-' then zoom_by(-1)
            elseif key=='tab' then minimap_visible=not minimap_visible
            end
            return -- free camera never submits a gameplay action
        end
    end
    if audio_runtime and (title or pause_menu or chest_menu or route_menu) then
        local cue=(key=='escape' or key=='n' and confirm_new) and
            'core:cue/ui/cancel' or
            (key=='return' or key=='kpenter') and 'core:cue/ui/confirm' or
            (key=='up' or key=='down' or key=='left' or key=='right' or
                key=='1' or key=='2' or key=='3' or key=='4') and
                'core:cue/ui/hover' or nil
        if cue then audio_runtime:emit(cue) end
        if title and tonumber(key) and Classes.order[tonumber(key)] then
            local class=Classes.order[tonumber(key)]
            audio_runtime:emit('core:cue/class/'..class:match('([^/]+)$')..'/select')
        end
    end
    if boon_detail_id then
        if key=='escape' or key=='return' or key=='kpenter' then
            boon_detail_id=nil
        end
        return
    end
    if title then
        if unlocks_view then
            if key=="escape" or key=="u" then unlocks_view=false end
            return
        end
        if confirm_new then
            if key=="y" or key=="return" or key=="kpenter" then begin_new()
            elseif key=="n" or key=="escape" then confirm_new=false end
            return
        end
        local class_index=tonumber(key)
        if class_index and Classes.order[class_index] then
            selected_class_id=Classes.order[class_index]
            return
        end
        if key=="c" or key=="return" or key=="kpenter" then
            if save_status and (save_status.status=="active" or
                save_status.status=="recovered") then continue_run()
            else persistence_message="No compatible active run to continue" end
        elseif key=="n" then
            if save_status and (save_status.status=="active" or
                save_status.status=="recovered" or
                save_status.status=="incompatible") then
                confirm_new=true
            else begin_new() end
        elseif key=="u" then unlocks_view=true
        elseif key=="r" and save_status and save_status.status=="pending_terminal" then
            local pending=save_status.state.meta and save_status.state.meta.pending or {}
            local ok,updated=profile_service:commit_awards(pending)
            if ok then
                profile=updated
                local done,why=save_service:tombstone()
                if done then
                    save_status=save_service:inspect()
                    profile_status=profile_service:inspect()
                    persistence_message="Terminal unlock recovered and saved"
                else persistence_message="Terminal save needs retry: "..tostring(why) end
            else persistence_message="Terminal unlock recovery needs retry: "..tostring(updated) end
        elseif key=="escape" or key=="q" then love.event.quit() end
        return
    end
    if pause_menu then
        if key=="escape" or key=="1" or key=="return" then
            pause_menu=false
        elseif key=="2" or key=="s" then
            if checkpoint(true) then love.event.quit() end
        end
        return
    end
    if confirm_reset_seed then
        if key=="escape" or key=="n" then
            confirm_reset_seed=nil
        elseif key=="return" or key=="kpenter" or key=="y" then
            local requested=confirm_reset_seed
            if os.getenv("ROAG_GUI_AUTOSTART")~="1" then
                begin_new(requested)
                if state and state.run_seed==requested then confirm_reset_seed=nil end
            else
                floor_seed=requested
                state=Game.new_stage(floor_seed)
                apply_showcase()
                playback,targeting,chest_menu,route_menu=Playback.new(),nil,nil,nil
                combat_numbers=CombatNumbers.new()
                reward_notices,reward_notice_time={},0
                notice=("Seed %d — new floor"):format(floor_seed)
                confirm_reset_seed=nil
                reset_camera()
            end
        end
        return
    end
    if key == "n" or key == "m" then
        if os.getenv("ROAG_GUI_AUTOSTART")~="1" then
            confirm_reset_seed=key=="m" and floor_seed%2147483646+1 or floor_seed
            persistence_message="Replace current run? Enter/Y confirm · Esc cancel"
            return
        end
        if key == "m" then floor_seed = floor_seed % 2147483646 + 1 end
        state = Game.new_stage(floor_seed)
        apply_showcase()
        playback, targeting, chest_menu, route_menu, notice = Playback.new(), nil, nil, nil,
            ("Seed %d — new floor"):format(floor_seed)
        combat_numbers=CombatNumbers.new()
        reward_notices, reward_notice_time = {}, 0
        reset_camera()
        return
    end
    if route_menu then
        if key=="escape" then
            route_menu=nil
            notice="Route choice cancelled — no time"
        elseif (key=="1" or key=="2" or key=="3")
            and route_menu.routes[tonumber(key)] then
            route_menu.selected=tonumber(key)
        elseif key=="return" or key=="kpenter" then
            if submit({kind="choose_route",exit_id=route_menu.exit_id,
                route_index=route_menu.selected}) then route_menu=nil end
        end
        return
    end
    if chest_menu then
        if key == "escape" then
            chest_menu = nil
            notice = "Chest cancelled — no time; offers retained"
        elseif key == "1" or key == "2" or key == "3" then
            chest_menu.selected = tonumber(key)
        elseif key == "return" or key == "kpenter" then
            local chosen = chest_menu.selected
            if submit({kind="claim_chest", chest_id=chest_menu.chest_id,
                offer_index=chosen}) then chest_menu = nil end
        end
        return
    end
    if key=="escape" and not targeting then
        pause_menu=true
        persistence_message=""
        return
    end
    if key == "tab" then
        minimap_visible = not minimap_visible
        notice = minimap_visible and "Map shown — no time" or "Map hidden — no time"
        return
    end
    if key=='=' or key=='+' or key=='kp+' then zoom_by(1);return end
    if key=='-' or key=='kp-' then zoom_by(-1);return end
    if key == "v" and not targeting then
        boon_page=boon_page+1
        notice="Boon list page — no time"
        return
    end
    if Playback.busy(playback) then
        if cardinal[key] or diagonal[key] or key=="space" or key=="."
            or key=="f" or key=="r" or key=="x"
            or key=="return" or key=="kpenter" then
            notice="Input ignored during playback — press again when ready"
        end
        return
    end
    if state.game_over then return end
    if key == "b" and not targeting then
        showcase_index = (showcase_index + 1) % (#showcase + 1)
        apply_showcase()
        notice = "DEV loadout: " .. (showcase[showcase_index] and
            showcase[showcase_index].name or "None") .. " — no time"
        return
    end
    if targeting then
        if key == "escape" then
            targeting = nil
            notice = "Targeting cancelled — no time"
            return
        end
        local direction = cardinal[key] or diagonal[key]
        if direction then
            targeting.dx, targeting.dy = direction[1], direction[2]
            player_facing=VisualSlots.facing(player_facing,direction[1])
            local preview = Game.preview(state, targeting.kind, targeting.dx, targeting.dy)
            notice = preview.valid and "Preview ready; Enter to commit" or
                ((preview.reason or "Invalid target") .. " — no time")
            return
        end
        if key == "return" or key == "kpenter" then
            if targeting.dx == nil then
                notice = "Choose a direction first — no time"
                return
            end
            local committed = submit({
                kind = targeting.kind, dx = targeting.dx, dy = targeting.dy,
            })
            if committed then targeting = nil end
        end
        return
    end

    if key == "f" then
        targeting = { kind = "basic_attack" }
        notice = Classes.for_player(Game.player(state)).basic_name..
            ": choose direction, then Enter"
    elseif key == "r" then
        targeting = { kind = "sweeping_slash" }
        notice = Classes.for_player(Game.player(state)).special_name..
            ": choose direction, then Enter"
    elseif key == "x" then
        targeting = { kind = "dash" }
        notice = Classes.for_player(Game.player(state)).dash_name..
            ": choose cardinal direction, then Enter"
    elseif key == "space" or key == "." or key == "kp5" then
        submit({ kind = "wait" })
    elseif cardinal[key] then
        local direction = cardinal[key]
        player_facing=VisualSlots.facing(player_facing,direction[1])
        submit({ kind = "move", dx = direction[1], dy = direction[2] })
    end
end

-- LÖVE's standard gamepad buttons reuse the same validated key path and
-- presentation gate. D-pad menu selection never submits a world action.
function love.gamepadpressed(_,button)
    if title and not unlocks_view and not confirm_new then
        if button=='leftshoulder' or button=='rightshoulder' then
            local index=1
            for i,id in ipairs(Classes.order) do
                if id==selected_class_id then index=i;break end
            end
            index=(index-1+(button=='rightshoulder' and 1 or -1)+
                #Classes.order)%#Classes.order+1
            selected_class_id=Classes.order[index]
            if audio_runtime then
                audio_runtime:emit('core:cue/class/'..
                    selected_class_id:match('([^/]+)$')..'/select')
            end
            return
        end
    end
    if route_menu or chest_menu then
        if button=='dpup' or button=='dpleft' or
            button=='dpdown' or button=='dpright' then
            local menu=route_menu or chest_menu
            local count=route_menu and #menu.routes or #menu.offers
            local delta=(button=='dpup' or button=='dpleft') and -1 or 1
            local selected=(menu.selected-1+delta+count)%count+1
            love.keypressed(tostring(selected))
            return
        end
    end
    local key
    if title then
        key=({a='return',b='escape',x='u',y='n',start='n',
            dpup='up',dpdown='down',dpleft='left',dpright='right'})[button]
    elseif pause_menu then
        key=({a='return',b='escape',y='2',start='escape'})[button]
    else
        key=({a='return',b='escape',x='f',y='r',
            rightshoulder='x',leftshoulder='v',start='escape',back='tab',
            dpup='w',dpdown='s',dpleft='a',dpright='d'})[button]
    end
    if key then love.keypressed(key) end
end

local function draw_cells(cells, red, green, blue, alpha)
    love.graphics.setColor(red, green, blue, alpha)
    for _, cell in ipairs(cells) do
        if visible_at(cell.x,cell.y) then
        local screen_x, screen_y = Camera.tile_top_left(camera, cell.x, cell.y)
        love.graphics.rectangle("fill", screen_x + 2, screen_y + 2, TILE - 4, TILE - 4)
        love.graphics.setColor(red, green, blue, math.min(1, alpha + 0.3))
        love.graphics.rectangle("line", screen_x + 2, screen_y + 2, TILE - 4, TILE - 4)
        love.graphics.setColor(red, green, blue, alpha)
        end
    end
end

local function draw_visual(handle,x,y,w,h,alpha,flip_x)
    if not handle then return false end
    love.graphics.setColor(1,1,1,alpha or 1)
    local draw_x,scale_x=VisualSlots.horizontal_draw(x,w,handle.width,
        flip_x and -1 or 1)
    if handle.quad then
        love.graphics.draw(handle.texture,handle.quad,draw_x,y,0,
            scale_x,h/handle.height)
    else
        love.graphics.draw(handle.texture,draw_x,y,0,
            scale_x,h/handle.height)
    end
    return true
end
local function bound_visual(binding)
    return visual_assets and visual_assets:resolve(binding) or nil
end
local function actor_visual(binding,id)
    local pose=VisualSlots.pose(id,Playback.current(playback),
        Playback.current_pose_events(playback))
    return pose and bound_visual(binding..'/'..pose) or bound_visual(binding)
end
local expansion_enemy_colors={
    ["core:enemy/mossbreaker"]={0.42,0.69,0.34},
    ["core:enemy/gloom_strider"]={0.49,0.66,0.85},
    ["core:enemy/mire_binder"]={0.45,0.71,0.58},
    ["core:enemy/pack_chorister"]={0.78,0.62,0.83},
    ["core:enemy/vault_lurker"]={0.56,0.53,0.75},
    ["core:enemy/crystal_bombardier"]={0.52,0.88,0.96},
    ["core:enemy/cinder_mender"]={0.94,0.64,0.38},
    ["core:enemy/glass_charger"]={0.73,0.87,0.97},
    ["core:enemy/forge_conjurer"]={0.98,0.49,0.25},
    ["core:enemy/bastion_bulwark"]={0.72,0.61,0.45},
}
local biome_elite_colors={
    ["core:enemy/mycelial_matriarch"]={0.73,0.52,0.90},
    ["core:enemy/canal_lockmaster"]={0.34,0.72,0.89},
    ["core:enemy/ash_marshal"]={0.88,0.51,0.37},
    ["core:enemy/prism_curator"]={0.72,0.88,0.96},
    ["core:enemy/index_keeper"]={0.87,0.75,0.44},
    ["core:enemy/ember_crucible"]={0.97,0.55,0.27},
}

local function draw_enemies()
    for _, enemy in ipairs(state.entities) do
        local position=enemy.position
        if enemy.controller == "ai" and position.floor_id==state.active_floor_id then
            -- Both the event-time pose and final authoritative tile must be
            -- visible; hidden movement cannot expose unseen routes.
            local visual_x,visual_y,shown=Playback.visible_position(playback,enemy.id,
                position.floor_id,position.x,position.y,visible_at)
            if shown then
            local x, y = Camera.tile_top_left(camera, visual_x, visual_y)
            local enemy_art=actor_visual(enemy.definition_id,enemy.id) or
                (visual_assets and visual_assets:resolve_id(enemy.definition_id))
            if enemy_art then
                draw_visual(enemy_art,x,y,TILE,TILE)
            else
            -- Built-in tokens were drawn on a 32px tile. Scale them with the
            -- camera so zoom works even without published enemy artwork.
            love.graphics.push()
            love.graphics.translate(x,y)
            love.graphics.scale(TILE/32)
            local x,y=0,0
            if enemy.ai_id == "guard" then
                love.graphics.setColor(0.42, 0.73, 0.43)
                love.graphics.rectangle("fill", x + 5, y + 5, 20, 19)
            elseif enemy.ai_id == "spitter" then
                love.graphics.setColor(0.70, 0.40, 0.80)
                love.graphics.polygon("fill", x + 15, y + 3, x + 28, y + 15,
                    x + 15, y + 27, x + 2, y + 15)
            elseif enemy.ai_id=="spore_herald" then
                love.graphics.setColor(0.50,0.87,0.88)
                love.graphics.circle("fill",x+16,y+17,10)
                love.graphics.setColor(0.82,0.45,0.90)
                love.graphics.circle("fill",x+11,y+9,5)
                love.graphics.circle("fill",x+22,y+10,5)
            elseif enemy.ai_id=="canal_lancer" then
                love.graphics.setColor(0.28,0.76,0.90)
                love.graphics.rectangle("fill",x+6,y+8,18,19)
                love.graphics.setColor(0.86,0.92,0.77)
                love.graphics.line(x+24,y+4,x+24,y+28)
            elseif enemy.ai_id=="cinder_bellkeeper" then
                love.graphics.setColor(0.82,0.47,0.28)
                love.graphics.rectangle("fill",x+5,y+8,22,18)
                love.graphics.setColor(1,0.78,0.35)
                love.graphics.circle("line",x+16,y+10,8)
            elseif enemy.ai_id=="warden" then
                love.graphics.setColor(0.42,0.75,0.22)
                love.graphics.rectangle("fill",x+3,y+3,26,24)
                love.graphics.setColor(0.78,0.32,0.27)
                love.graphics.polygon("fill",x+4,y+4,x+16,y-5,x+28,y+4)
                love.graphics.print(enemy.phase_two and "II" or "I",x+9,y+7)
            elseif enemy.ai_id=="biome_elite" then
                love.graphics.setColor(unpack(biome_elite_colors[enemy.definition_id]))
                love.graphics.polygon("fill",x+16,y+1,x+30,y+11,
                    x+25,y+29,x+7,y+29,x+2,y+11)
                love.graphics.setColor(0.16,0.18,0.23)
                love.graphics.print(enemy.phase_two and "II" or "I",x+10,y+9)
            elseif enemy.ai_id=="expansion_regular" then
                love.graphics.setColor(unpack(expansion_enemy_colors[enemy.definition_id]))
                love.graphics.polygon("fill",x+16,y+3,x+28,y+11,
                    x+24,y+27,x+8,y+27,x+4,y+11)
                love.graphics.setColor(0.12,0.17,0.20)
                love.graphics.print(Content.get_enemy(enemy.definition_id).name:sub(1,1),
                    x+12,y+8)
            elseif enemy.ai_id=="hollow_regent" then
                love.graphics.setColor(0.59,0.31,0.76)
                love.graphics.polygon("fill",x+16,y+1,x+29,y+12,
                    x+25,y+27,x+7,y+27,x+3,y+12)
                love.graphics.setColor(0.96,0.77,0.43)
                love.graphics.polygon("line",x+3,y+10,x+8,y+2,
                    x+16,y+8,x+24,y+2,x+29,y+10)
                love.graphics.print(enemy.phase_two and "II" or "I",x+10,y+10)
            elseif enemy.ai_id=="pursuer" then
                love.graphics.setColor(0.55,0.86,1,0.85)
                love.graphics.circle("fill",x+16,y+14,12)
                love.graphics.setColor(0.10,0.24,0.30)
                love.graphics.circle("fill",x+12,y+12,2)
                love.graphics.circle("fill",x+20,y+12,2)
            else
                love.graphics.setColor(0.95, 0.49, 0.27)
                love.graphics.polygon("fill", x + 15, y + 3, x + 28, y + 26,
                    x + 2, y + 26)
            end
            love.graphics.pop()
            end
            local bar_x=x+TILE*3/32
            local bar_y=y+TILE*28/32
            local bar_width=TILE*26/32
            local bar_height=math.max(3,TILE*3/32)
            love.graphics.setColor(0.16, 0.19, 0.16)
            love.graphics.rectangle("fill",bar_x,bar_y,bar_width,bar_height)
            love.graphics.setColor(0.94, 0.28, 0.29)
            love.graphics.rectangle("fill",bar_x,bar_y,
                bar_width*enemy.hp/enemy.max_hp,bar_height)
            if enemy.statuses and enemy.statuses.burning then
                local scale=TILE/32
                love.graphics.setColor(1,0.47,0.13,0.95)
                love.graphics.circle("fill",x+26*scale,y+5*scale,3*scale)
                love.graphics.polygon("fill",x+23*scale,y+5*scale,
                    x+26*scale,y-2*scale,x+29*scale,y+5*scale)
            end
            end
        end
    end
end

local function begin_tile_token(x,y)
    love.graphics.push()
    love.graphics.translate(x,y)
    love.graphics.scale(TILE/32)
end

local function draw_rewards()
    for _, drop in ipairs(state.drops) do
        if not drop.collected and drop.position.floor_id==state.active_floor_id
            and visible_at(drop.position.x,drop.position.y) then
            local x, y = Camera.tile_top_left(camera, drop.position.x, drop.position.y)
            if not draw_visual(bound_visual('core:drop/'..drop.kind),x,y,TILE,TILE) then
                begin_tile_token(x,y)
                local x,y=0,0
                if drop.kind=="food" then
                    love.graphics.setColor(.48,.82,.28)
                    love.graphics.circle("fill",x+16,y+17,7)
                    love.graphics.setColor(.19,.45,.16)
                    love.graphics.rectangle("fill",x+15,y+7,3,5)
                elseif drop.kind=="health" then
                    love.graphics.setColor(.94,.3,.38)
                    love.graphics.rectangle("fill",x+13,y+8,6,17)
                    love.graphics.rectangle("fill",x+8,y+13,16,6)
                else
                    love.graphics.setColor(1, 0.86, 0.26)
                    love.graphics.circle("fill", x + 16, y + 16, 6)
                    love.graphics.setColor(0.26, 0.19, 0.05)
                    love.graphics.circle("line", x + 16, y + 16, 6)
                end
                love.graphics.pop()
            end
        end
    end
    for _, entity in ipairs(state.entities) do
        if entity.position.floor_id==state.active_floor_id
            and entity.controller == "chest" and
            visible_at(entity.position.x,entity.position.y) then
            local x, y = Camera.tile_top_left(camera, entity.position.x, entity.position.y)
            local chest_art=bound_visual(entity.definition_id)
            if chest_art and not entity.claimed then
                draw_visual(chest_art,x,y,TILE,TILE)
            else
            begin_tile_token(x,y)
            local x,y=0,0
            if entity.claimed then
                love.graphics.setColor(0.34, 0.26, 0.17)
            else
                love.graphics.setColor(0.78, 0.51, 0.19)
            end
            love.graphics.rectangle("fill", x + 4, y + 10, 24, 18)
            love.graphics.setColor(1, 0.86, 0.41)
            love.graphics.rectangle("line", x + 4, y + 10, 24, 18)
            love.graphics.pop()
            end
        end
        if entity.position.floor_id==state.active_floor_id
            and entity.controller == "door" and
            visible_at(entity.position.x,entity.position.y) then
            local x,y=Camera.tile_top_left(camera,entity.position.x,entity.position.y)
            begin_tile_token(x,y)
            local x,y=0,0
            love.graphics.setColor(entity.open and 0.42 or 0.57,
                entity.open and 0.47 or 0.35,0.22)
            love.graphics.rectangle("fill",x+6,y+3,20,27)
            love.graphics.setColor(0.79,0.69,0.39)
            love.graphics.rectangle("line",x+6,y+3,20,27)
            love.graphics.pop()
        end
        if entity.position.floor_id==state.active_floor_id
            and entity.controller=="exit" and
            visible_at(entity.position.x,entity.position.y) then
            local x,y=Camera.tile_top_left(camera,entity.position.x,entity.position.y)
            begin_tile_token(x,y)
            local x,y=0,0
            love.graphics.setColor(0.34,0.88,0.94)
            love.graphics.circle("line",x+16,y+16,12)
            love.graphics.polygon("fill",x+16,y+6,x+23,y+17,x+16,y+25,x+9,y+17)
            love.graphics.pop()
        end
    end
    for _,stair in ipairs(state.generated_map and
        state.generated_map.placements.stairs or {}) do
        if visible_at(stair.x,stair.y) then
            local x,y=Camera.tile_top_left(camera,stair.x,stair.y)
            begin_tile_token(x,y)
            local x,y=0,0
            love.graphics.setColor(0.43,0.88,0.96)
            love.graphics.rectangle("line",x+4,y+4,24,24)
            love.graphics.line(x+9,y+13,x+16,y+19,x+23,y+13)
            love.graphics.line(x+9,y+20,x+16,y+26,x+23,y+20)
            love.graphics.pop()
        end
    end
end

local function draw_history_event(event)
    if not event then return end
    if event.kind=="Moved" or event.kind=="Displaced" then return end
    if event.kind == "WindupStarted" then
        draw_cells(event.target_cells, 1, 0.64, 0.18, 0.5)
    elseif event.kind == "AttackPerformed" then
        draw_cells(event.target_cells, 1, 0.22, 0.20, 0.7)
    elseif event.kind == "WindupCancelled" then
        draw_cells(event.target_cells, 0.6, 0.6, 0.6, 0.4)
    elseif event.kind == "EffectApplied" or event.kind == "DelayedEffectResolved" then
        if event.damage_type=="fire" then
            draw_cells(event.target_cells or {},1,0.42,0.13,0.51)
        elseif event.damage_type=="pulse" then
            draw_cells(event.target_cells or {},0.55,0.94,0.67,0.51)
        else
            draw_cells(event.target_cells or {}, 0.36, 0.78, 1.0, 0.57)
        end
    elseif event.kind=="StatusApplied" or event.kind=="StatusTick" then
        draw_cells({{x=event.x,y=event.y}},1,0.43,0.13,0.45)
    elseif event.kind == "SecretDiscovered" then
        draw_cells({{x=event.wall_x or event.x,y=event.wall_y or event.y}},
            0.97,0.78,0.31,0.60)
    elseif event.kind == "DoorOpened" then
        draw_cells({{x=event.x,y=event.y}},0.69,0.85,0.40,0.48)
    elseif event.kind == "TerrainTriggered" then
        draw_cells({{x=event.x,y=event.y}},0.76,0.50,0.22,0.42)
    elseif event.kind == "TerrainBroken" then
        draw_cells({{x=event.x,y=event.y}},0.90,0.72,0.34,0.52)
    elseif event.x and event.y then
        draw_cells({{x=event.x,y=event.y}}, 1, 0.20, 0.22, 0.5)
    end
    if event.from_x and event.from_y and visible_at(event.from_x,event.from_y) then
        local x, y = Camera.tile_top_left(camera, event.from_x, event.from_y)
        love.graphics.setColor(1, 0.92, 0.52, 0.85)
        love.graphics.circle("line", x + TILE / 2, y + TILE / 2, 13)
    end
end

local function draw_combat_numbers()
    local g=love.graphics
    g.setFont(ui_display_font)
    for _,hit in ipairs(combat_numbers.items) do
        if visible_at(hit.x,hit.y) then
            local sx,sy=Camera.tile_top_left(camera,hit.x,hit.y)
            local t=hit.age/CombatNumbers.lifetime
            local rise=math.floor(27*(1-(1-t)*(1-t))+0.5)
            local drift=hit.lane*12+(hit.from_player and 3 or -3)*(1-t)
            local label=hit.amount>0 and
                (tostring(hit.amount)..(hit.critical and '!' or '')) or '0'
            local side=sx+TILE*1.5<VIEWPORT.x+VIEWPORT.width and 1 or -1
            local x=math.floor(sx+TILE/2+side*16+drift-
                g.getFont():getWidth(label)/2+0.5)
            local y=math.floor(sy-3-rise)
            local alpha=t<0.58 and 1 or math.max(0,(1-t)/0.42)
            g.setColor(0.025,0.025,0.025,alpha*0.9)
            g.print(label,x-1,y);g.print(label,x+1,y)
            g.print(label,x,y-1);g.print(label,x,y+1)
            if hit.amount==0 then g.setColor(.73,.86,1,alpha)
            elseif hit.critical then g.setColor(1,.48,.22,alpha)
            elseif hit.from_player then g.setColor(1,.88,.45,alpha)
            else g.setColor(1,.49,.43,alpha) end
            g.print(label,x,y)
        end
    end
    g.setFont(ui_body_font)
end

local function draw_preview()
    if not targeting or targeting.dx == nil then return end
    local preview = Game.preview(state, targeting.kind, targeting.dx, targeting.dy)
    for _, cell in ipairs(preview.cells) do
        if visible_at(cell.x,cell.y) then
        if targeting.kind == "dash" then
            if cell.status == "open" then
                love.graphics.setColor(0.22, 0.85, 0.55, 0.55)
            elseif cell.status == "unreachable" then
                love.graphics.setColor(0.55, 0.55, 0.55, 0.45)
            else
                love.graphics.setColor(1.0, 0.27, 0.28, 0.65)
            end
        elseif targeting.kind == "sweeping_slash" then
            love.graphics.setColor(1.0, 0.56, 0.17, 0.65)
        else
            love.graphics.setColor(1.0, 0.86, 0.30, 0.65)
        end
        local screen_x, screen_y = Camera.tile_top_left(camera, cell.x, cell.y)
        love.graphics.rectangle("fill", screen_x + 2, screen_y + 2, TILE - 4, TILE - 4)
        love.graphics.setColor(1, 1, 1, 0.9)
        love.graphics.rectangle("line", screen_x + 2, screen_y + 2, TILE - 4, TILE - 4)
        end
    end
end

local function draw_minimap()
    if not (state.generated_map or state.finale) or not minimap_visible then return end
    local map=Content.map_for(state)
    local known=state.explored[map.floor_id]
    local scale=1.3
    local ox,oy=VIEWPORT.x+9,VIEWPORT.y+18
    love.graphics.setColor(0.015,0.027,0.023,0.91)
    love.graphics.rectangle("fill",ox-5,oy-14,map.width*scale+10,
        map.height*scale+20)
    love.graphics.setColor(0.7,0.85,0.7)
    love.graphics.print("MAP · Tab",ox,oy-14)
    local function draw_minimap_cell(x,y)
        local current=visible_at(x,y)
        local tile=map.rows[y]:sub(x,x)
        if tile=="~" then
            love.graphics.setColor(current and 0.23 or 0.10,
                current and 0.51 or 0.22,current and 0.70 or 0.30)
        elseif tile=="#" then
            love.graphics.setColor(current and 0.54 or 0.22,
                current and 0.64 or 0.28,current and 0.54 or 0.24)
        else
            love.graphics.setColor(current and 0.40 or 0.16,
                current and 0.62 or 0.26,current and 0.39 or 0.17)
        end
        love.graphics.rectangle("fill",ox+(x-1)*scale,oy+(y-1)*scale,scale,scale)
    end
    if spectator_enabled then
        for y=1,map.height do
            for x=1,map.width do draw_minimap_cell(x,y) end
        end
    else
        for _,at in ipairs(known and known.order or {}) do
            draw_minimap_cell((at-1)%map.width+1,
                math.floor((at-1)/map.width)+1)
        end
    end
    for _,entity in ipairs(state.entities) do
        if entity.position.floor_id==map.floor_id and entity.controller=="door" and
            explored_at(map.floor_id,entity.position.x,entity.position.y) then
            love.graphics.setColor(0.9,0.67,0.30)
            love.graphics.rectangle("fill",ox+(entity.position.x-1)*scale,
                oy+(entity.position.y-1)*scale,2.4,2.4)
        end
    end
    for _,stair in ipairs(map.placements and map.placements.stairs or {}) do
        if explored_at(map.floor_id,stair.x,stair.y) then
            love.graphics.setColor(0.43,0.88,0.96)
            love.graphics.rectangle("line",ox+(stair.x-1)*scale-1,
                oy+(stair.y-1)*scale-1,4,4)
        end
    end
    if map.elite and (not state.stage or
        state.stage.phase=="exploring" or state.stage.phase=="elite_active")
        and (not state.floor_order or state.active_floor_index==2)
        and explored_at(map.floor_id,map.elite.x,map.elite.y) then
        love.graphics.setColor(0.94,0.67,0.35)
        love.graphics.rectangle("line",ox+(map.elite.x-1)*scale-1,
            oy+(map.elite.y-1)*scale-1,4,4)
    end
    if state.stage and state.stage.exit_id then
        local exit=Game.entity(state,state.stage.exit_id)
        if exit and exit.position.floor_id==map.floor_id
            and explored_at(map.floor_id,
            exit.position.x,exit.position.y) then
            love.graphics.setColor(0.35,0.88,0.98)
            love.graphics.rectangle("line",ox+(exit.position.x-1)*scale-1,
                oy+(exit.position.y-1)*scale-1,4,4)
        end
    end
    local p=Game.player(state).position
    love.graphics.setColor(1,0.86,0.23)
    love.graphics.rectangle("fill",ox+(p.x-1)*scale-1,
        oy+(p.y-1)*scale-1,4,4)
    if spectator_enabled and spectator_free and spectator_focus then
        love.graphics.setColor(0.4,0.92,1)
        love.graphics.rectangle('line',ox+(spectator_focus.x-1)*scale-2,
            oy+(spectator_focus.y-1)*scale-2,6,6)
    end
end

local function draw_world()
    local map = Content.map_for(state)
    local clue=Biomes.get(map.biome_id or Biomes.order[1]).clue_color
    love.graphics.setScissor(VIEWPORT.x, VIEWPORT.y, VIEWPORT.width, VIEWPORT.height)
    love.graphics.setColor(0.09, 0.13, 0.11)
    love.graphics.rectangle("fill", VIEWPORT.x, VIEWPORT.y, VIEWPORT.width, VIEWPORT.height)
    local biome_id=map.biome_id or Biomes.order[1]
    local background=bound_visual('core:background/'..biome_id:match('/([^/]+)$'))
    if background then
        draw_visual(background,VIEWPORT.x,VIEWPORT.y,VIEWPORT.width,VIEWPORT.height,
            VisualSlots.background_alpha(background.source))
    end
    if terrain_cache_assets~=visual_assets then
        terrain_cache_assets=visual_assets;terrain_cache={}
    end
    local min_x=math.max(1,math.floor((camera.x-VIEWPORT.width/2)/TILE)+1)
    local max_x=math.min(map.width,math.ceil((camera.x+VIEWPORT.width/2)/TILE)+1)
    local min_y=math.max(1,math.floor((camera.y-VIEWPORT.height/2)/TILE)+1)
    local max_y=math.min(map.height,math.ceil((camera.y+VIEWPORT.height/2)/TILE)+1)
    for y = min_y, max_y do
        for x = min_x, max_x do
            if not (state.generated_map or state.finale) or
                explored_at(map.floor_id,x,y) then
            local screen_x, screen_y = Camera.tile_top_left(camera, x, y)
            local tile=map.rows[y]:sub(x,x)
            local wall = tile == "#" or tile=="B"
            local visual=map.visual_rows and map.visual_rows[y]:sub(x,x) or nil
            local remembered=(state.generated_map or state.finale) and not visible_at(x,y)
            local r,g,b
            if tile=="b" then
                r,g,b=0.30,0.31,0.12
            elseif tile=="g" then
                r,g,b=0.19,0.27,0.16
            elseif tile=="i" then
                r,g,b=0.35,0.54,0.62
            elseif tile=="B" then
                r,g,b=0.46,0.32,0.20
            elseif tile=="m" then
                r,g,b=0.26,0.35,0.36
            elseif tile=="c" then
                r,g,b=0.32,0.26,0.46
            elseif tile=="~" then
                r,g,b=0.10,0.29,0.42
            elseif wall then
                r,g,b=0.26,0.33,0.29
                if visual=="F" then r,g,b=0.23,0.31,0.36
                elseif visual=="W" then r,g,b=0.20,0.32,0.42
                elseif visual=="T" then r,g,b=0.39,0.29,0.28
                elseif visual=="I" then r,g,b=0.42,0.57,0.65
                elseif visual=="L" then r,g,b=0.41,0.34,0.27
                elseif visual=="O" then r,g,b=0.45,0.28,0.24 end
            elseif visual=="a" then
                r,g,b=0.29,0.25,0.35
            elseif visual=="f" then
                r,g,b=0.17,0.31,0.31
            elseif visual=="b" then
                r,g,b=0.20,0.36,0.43
            elseif visual=="i" then
                r,g,b=0.25,0.40,0.47
            elseif visual=="l" then
                r,g,b=0.35,0.29,0.23
            elseif visual=="o" then
                r,g,b=0.36,0.25,0.20
            elseif visual=="t" then
                r,g,b=0.36,0.27,0.24
            elseif visual=="c" then
                r,g,b=0.24,0.25,0.20
            elseif visual=="m" then
                r,g,b=0.16,0.28,0.15
            elseif visual=="v" then
                r,g,b=0.15,0.25,0.16
            elseif visual=="r" then
                r,g,b=0.20,0.21,0.17
            else
                r,g,b=0.15,0.20,0.17
            end
            if remembered then r,g,b=r*0.38,g*0.38,b*0.38 end
            if state.finale then
                if wall then r,g,b=0.25,0.19,0.35
                else r,g,b=0.24,0.19,0.29 end
                if remembered then r,g,b=r*0.38,g*0.38,b*0.38 end
            end
            local kind=tile=='#' and 'wall' or tile=='~' and 'water' or
                tile=='.' and 'floor' or nil
            love.graphics.setColor(r,g,b,background and not wall and 0.78 or 1)
            -- Generated motifs have transparent margins. Fill their terrain
            -- continuously so those margins do not become a repeated grid.
            local edge=kind and visual_assets and visual_assets.generated and 0 or 2
            love.graphics.rectangle("fill", screen_x, screen_y, TILE-edge,TILE-edge)
            if kind and visual_assets and visual_assets.has_terrain then
                local shape=VisualSlots.terrain_shape(map,x,y,kind)
                local cache_key=biome_id..':'..kind..':'..shape
                local handle=terrain_cache[cache_key]
                if handle==nil then
                    for _,binding in ipairs(VisualSlots.terrain_bindings(
                        biome_id,kind,shape)) do
                        handle=visual_assets:resolve(binding)
                        if handle then break end
                    end
                    terrain_cache[cache_key]=handle or false
                end
                if handle and VisualSlots.terrain_draws_at(kind,handle.source,x,y) then
                    draw_visual(handle,screen_x,screen_y,TILE,TILE,
                        VisualSlots.terrain_alpha(kind,handle.source,remembered))
                end
            end
            local authored_id=map.studio_visuals and map.studio_visuals[x..','..y]
            if authored_id and visual_assets then
                local handle=visual_assets:resolve_id(authored_id)
                if handle then
                    love.graphics.setColor(1,1,1,remembered and 0.38 or 1)
                    if handle.quad then
                        love.graphics.draw(handle.texture,handle.quad,screen_x,screen_y,
                            0,TILE/handle.width,TILE/handle.height)
                    else
                        love.graphics.draw(handle.texture,screen_x,screen_y,0,
                            TILE/handle.width,TILE/handle.height)
                    end
                end
            end
            if tile=="b" or tile=="g" or tile=="i" or tile=="B"
                or tile=="m" or tile=="c" then
                love.graphics.setColor(0.88,0.91,0.78,remembered and 0.25 or 0.65)
                if tile=="b" then
                    love.graphics.line(screen_x+7,screen_y+24,screen_x+14,screen_y+8,
                        screen_x+20,screen_y+24)
                elseif tile=="g" then
                    love.graphics.circle("line",screen_x+16,screen_y+17,7)
                elseif tile=="i" then
                    love.graphics.line(screen_x+5,screen_y+21,screen_x+25,screen_y+10)
                elseif tile=="B" then
                    love.graphics.line(screen_x+5,screen_y+9,screen_x+25,screen_y+9)
                    love.graphics.line(screen_x+5,screen_y+20,screen_x+25,screen_y+20)
                elseif tile=="m" then
                    love.graphics.circle("line",screen_x+16,screen_y+16,8)
                elseif tile=="c" then
                    love.graphics.polygon("line",screen_x+16,screen_y+5,
                        screen_x+24,screen_y+16,screen_x+16,screen_y+27,
                        screen_x+8,screen_y+16)
                end
            end
            if not remembered and visual=="k" then
                love.graphics.setColor(clue[1],clue[2],clue[3],0.78)
                love.graphics.line(screen_x+9,screen_y+6,screen_x+17,screen_y+16,
                    screen_x+12,screen_y+25)
            elseif not remembered and visual=="p" then
                love.graphics.setColor(clue[1],clue[2],clue[3],0.73)
                love.graphics.circle("line",screen_x+16,screen_y+16,6)
                love.graphics.circle("fill",screen_x+16,screen_y+16,2)
            elseif not remembered and visual=="m" then
                love.graphics.setColor(0.28,0.47,0.23,0.65)
                love.graphics.circle("fill",screen_x+23,screen_y+9,3)
            elseif not remembered and visual=="v" then
                love.graphics.setColor(0.33,0.52,0.25,0.75)
                love.graphics.line(screen_x+6,screen_y+3,screen_x+8,screen_y+12,
                    screen_x+14,screen_y+17)
            elseif not remembered and visual=="r" then
                love.graphics.setColor(0.48,0.47,0.34,0.65)
                love.graphics.rectangle("fill",screen_x+6,screen_y+20,7,4)
                love.graphics.rectangle("fill",screen_x+17,screen_y+15,4,3)
            elseif not remembered and visual=="w" and map.visual_rows then
                love.graphics.setColor(0.34,0.45,0.30,0.5)
                love.graphics.rectangle("fill",screen_x+3,screen_y+3,7,3)
            elseif not remembered and visual=="f" then
                love.graphics.setColor(0.42,0.81,0.77,0.60)
                love.graphics.circle("fill",screen_x+8,screen_y+10,2)
                love.graphics.circle("fill",screen_x+23,screen_y+23,2)
            elseif not remembered and visual=="b" then
                love.graphics.setColor(0.52,0.76,0.84,0.48)
                love.graphics.line(screen_x+4,screen_y+24,screen_x+26,screen_y+24)
            elseif not remembered and visual=="q" then
                love.graphics.setColor(0.40,0.68,0.81,0.72)
                love.graphics.line(screen_x+3,screen_y+10,screen_x+12,screen_y+10)
                love.graphics.line(screen_x+17,screen_y+22,screen_x+28,screen_y+22)
            elseif not remembered and visual=="t" then
                love.graphics.setColor(0.72,0.48,0.34,0.52)
                love.graphics.line(screen_x+4,screen_y+14,screen_x+27,screen_y+14)
            end
            end
        end
    end
    if map.elite and (not state.stage or
        state.stage.phase=="exploring" or state.stage.phase=="elite_active")
        and visible_at(map.elite.x,map.elite.y) then
        local x,y=Camera.tile_top_left(camera,map.elite.x,map.elite.y)
        love.graphics.setColor(0.96,0.72,0.32)
        love.graphics.circle("line",x+TILE/2,y+TILE/2,12)
        love.graphics.circle("line",x+TILE/2,y+TILE/2,8)
    end
    if not Playback.busy(playback) then
        for _, strike in ipairs(Game.pending_strikes(state)) do
            draw_cells(strike.target_cells, 0.95, 0.26, 0.31, 0.38)
        end
    end
    draw_preview()
    draw_rewards()
    draw_enemies()
    if state.stage and state.stage.phase=="elite_active" then
        local elite=Game.entity(state,state.stage.elite_id)
        if elite and visible_at(elite.position.x,elite.position.y) then
            love.graphics.setColor(0.04,0.06,0.04,0.82)
            love.graphics.rectangle("fill",230,27,330,37)
            love.graphics.setColor(0.82,0.95,0.60)
            local elite_name=Content.get_enemy(elite.definition_id).name
            if elite.ai_id=="warden" then elite_name=elite_name:upper() end
            love.graphics.print(("%s  phase %d  %d/%d HP"):format(
                elite_name,elite.phase_two and 2 or 1,elite.hp,elite.max_hp),239,34)
            love.graphics.setColor(0.37,0.72,0.25)
            love.graphics.rectangle("fill",239,52,310*elite.hp/elite.max_hp,5)
        end
    end
    if state.finale and state.finale.phase=="active" then
        local boss=Game.entity(state,state.finale.boss_id)
        if boss and visible_at(boss.position.x,boss.position.y) then
            love.graphics.setColor(0.05,0.04,0.09,0.88)
            love.graphics.rectangle("fill",220,27,350,39)
            love.graphics.setColor(0.94,0.78,0.95)
            love.graphics.print(("HOLLOW REGENT  phase %d  %d/%d HP"):format(
                boss.phase_two and 2 or 1,boss.hp,boss.max_hp),229,34)
            love.graphics.setColor(0.68,0.37,0.82)
            love.graphics.rectangle("fill",229,53,328*boss.hp/boss.max_hp,5)
        end
    end
    local position = Game.player(state).position
    local visual_x,visual_y=Playback.position(playback,state.player_id,
        position.floor_id,position.x,position.y)
    local screen_x, screen_y = Camera.tile_top_left(camera, visual_x, visual_y)
    local avatar=actor_visual(Game.player(state).definition_id,state.player_id)
    if avatar then
        draw_visual(avatar,screen_x,screen_y,TILE,TILE,nil,player_facing==-1)
    else
        local colors={
            [Classes.order[1]]={0.95,0.78,0.30},
            [Classes.order[2]]={0.49,0.83,0.49},
            [Classes.order[3]]={0.67,0.62,0.96},
            [Classes.order[4]]={0.96,0.49,0.38},
            [Classes.order[5]]={0.55,0.76,0.93},
            [Classes.order[6]]={0.73,0.93,0.44},
        }
        love.graphics.setColor(unpack(colors[Game.player(state).definition_id]))
        love.graphics.circle("fill", screen_x + TILE / 2 - TILE/32,
            screen_y + TILE / 2 - TILE/32, 11*TILE/32)
    end
    local current_group=Playback.current_group(playback)
    if current_group then
        for _,event in ipairs(current_group) do draw_history_event(event) end
    else
        draw_history_event(Playback.current(playback))
    end
    local visual_event=Playback.current(playback)
    if visual_event and
        (not visual_event.floor_id or visual_event.floor_id==state.active_floor_id) then
        local seen,effects={},0
        for _,event in ipairs(current_group or {visual_event}) do
            if (event.kind=='DamageTaken' or event.kind=='BoonActivated')
                and event.x and event.y and
                (not event.floor_id or event.floor_id==state.active_floor_id) then
                local key=event.kind..':'..event.x..':'..event.y
                if not seen[key] then
                    seen[key]=true
                    if studio_theme then
                        studio_theme:draw_effect(event,playback,
                            function(x,y) return Camera.tile_top_left(camera,x,y) end,
                            visible_at)
                    elseif visible_at(event.x,event.y) then
                        local id=event.kind=='DamageTaken' and
                            'core:ui/hit_effect' or 'core:ui/boon_effect'
                        local art=bound_visual(id)
                        if art then
                            local x,y=Camera.tile_top_left(camera,event.x,event.y)
                            draw_visual(art,x+TILE*0.125,y+TILE*0.125,
                                TILE*0.75,TILE*0.75,0.82)
                        end
                    end
                    effects=effects+1
                    if effects>=8 then break end
                end
            end
        end
    end
    local flash=HitFeedback.flash_alpha(hit_feedback)
    if flash>0 then
        love.graphics.setColor(1,0.20,0.16,flash)
        love.graphics.rectangle("fill",VIEWPORT.x,VIEWPORT.y,
            VIEWPORT.width,VIEWPORT.height)
        love.graphics.setColor(1,0.35,0.28,math.min(0.4,flash*2))
        love.graphics.rectangle("line",VIEWPORT.x+1,VIEWPORT.y+1,
            VIEWPORT.width-2,VIEWPORT.height-2)
    end
    draw_combat_numbers()
    if spectator_enabled and spectator_free and spectator_focus then
        local cx,cy=Camera.tile_top_left(camera,spectator_focus.x,
            spectator_focus.y)
        love.graphics.setColor(0.38,0.94,1,0.9)
        love.graphics.rectangle('line',cx+1,cy+1,TILE-2,TILE-2)
    end
    draw_minimap()
    if chest_menu then
        if studio_theme and studio_theme:draw_panel(70,100,640,390) then
            love.graphics.setColor(0.02,0.04,0.04,0.72)
            love.graphics.rectangle('fill',74,104,632,382)
        else
            love.graphics.setColor(0.02, 0.04, 0.04, 0.93)
            love.graphics.rectangle("fill", 70, 100, 640, 390)
        end
        love.graphics.setColor(1, 0.88, 0.53)
        love.graphics.print(("%s — price %d (you have %d)"):format(
            chest_menu.name, chest_menu.price, state.currency), 90, 120)
        for index, offer in ipairs(chest_menu.offers) do
            local definition = BoonContent.get(offer.boon_id)
            local power = definition.parameters[offer.rarity]
            local chance = power.chance and power.chance > 0
                and (" · %.1f%% chance"):format(power.chance / 10000) or ""
            love.graphics.setColor(index == chest_menu.selected and 1 or 0.85,
                index == chest_menu.selected and 0.89 or 0.88, 0.67)
            love.graphics.print(("%d. %s [%s] · power %d%s"):format(
                index, definition.name, offer.rarity, power.power, chance), 90, 158 + (index-1)*88)
            love.graphics.printf(BoonContent.offer_text(offer.boon_id,offer.rarity),
                110, 180 + (index-1)*88,470)
            local art=bound_visual(offer.boon_id..'/detail') or
                bound_visual(offer.boon_id..'/icon')
            if art then draw_visual(art,610,151+(index-1)*88,64,64) end
        end
        love.graphics.setColor(0.9, 0.95, 0.9)
        local status = chest_menu.shortfall > 0 and
            ("Need %d more currency · Esc cancels free"):format(chest_menu.shortfall)
            or "1/2/3 select · Enter claim · Esc cancel (no time)"
        love.graphics.print(status, 90, 451)
    end
    if route_menu then
        love.graphics.setColor(0.02,0.04,0.05,0.95)
        love.graphics.rectangle("fill",85,135,600,350)
        love.graphics.setColor(0.66,0.91,1)
        love.graphics.print(route_menu.stage_index>=3
            and "Enter the Final Sanctum"
            or "Choose a route — next biome",105,155)
        for index,route in ipairs(route_menu.routes) do
            love.graphics.setColor(index==route_menu.selected and 1 or 0.76,
                index==route_menu.selected and 0.87 or 0.85,0.67)
            love.graphics.print(index..". "..route.name,110,190+(index-1)*75)
            love.graphics.print(route.description,130,215+(index-1)*75)
        end
        love.graphics.setColor(0.9,0.95,0.9)
        love.graphics.print(route_menu.stage_index>=3
            and "Enter confirm · Esc cancel (no time)"
            or "1/2/3 select · Enter confirm · Esc cancel (no time)",105,435)
    end
    if state.game_over and not Playback.busy(playback) then
        love.graphics.setColor(0, 0, 0, 0.84)
        love.graphics.rectangle("fill", VIEWPORT.x, VIEWPORT.y, VIEWPORT.width, VIEWPORT.height)
        if state.victory then
            local result=state.victory
            local names={"Overgrown Ruins"}
            for index=1,2 do
                local biome_id=result.route_history[index]:gsub("^core:route/","core:biome/")
                names[#names+1]=Biomes.get(biome_id).name
            end
            local owned={}
            for _,id in ipairs(BoonContent.order) do
                local stacks=result.boons[id]
                if stacks then
                    local total=0
                    for _,rarity in ipairs(BoonContent.rarities) do
                        total=total+(stacks[rarity] or 0)
                    end
                    if total>0 then owned[#owned+1]=BoonContent.get(id).name.." x"..total end
                end
            end
            love.graphics.setColor(1,0.82,0.43)
            love.graphics.printf("VICTORY",VIEWPORT.x,130,VIEWPORT.width,"center")
            love.graphics.setColor(0.92,0.89,0.96)
            love.graphics.printf("Hollow Regent defeated",
                VIEWPORT.x,168,VIEWPORT.width,"center")
            love.graphics.printf(table.concat(names," > "),VIEWPORT.x+30,199,
                VIEWPORT.width-60,"center")
            local class=Classes.get(result.class_id or Classes.order[1])
            love.graphics.printf(("%s · Level %d · XP %d · HP %d/%d · Currency %d"):format(
                class.name,result.level,result.xp_total,result.hp,result.max_hp,result.currency),
                VIEWPORT.x+30,235,VIEWPORT.width-60,"center")
            love.graphics.printf("Boons: "..(#owned>0 and table.concat(owned,", ") or "none"),
                VIEWPORT.x+45,278,VIEWPORT.width-90,"center")
            local unlocked={}
            for _,id in ipairs(AchievementContent.order) do
                if state.meta and state.meta.earned[id] then
                    unlocked[#unlocked+1]=BoonContent.get(AchievementContent.get(id).reward).name
                end
            end
            love.graphics.printf("Unlocked this run: "..(#unlocked>0 and
                table.concat(unlocked,", ") or "none"),VIEWPORT.x+45,330,
                VIEWPORT.width-90,"center")
            love.graphics.printf("N — New Run",VIEWPORT.x,408,VIEWPORT.width,"center")
        else
            love.graphics.setColor(1, 0.36, 0.33)
            love.graphics.printf("GAME OVER — press N to restart", VIEWPORT.x,
                VIEWPORT.y + VIEWPORT.height / 2 - 10, VIEWPORT.width, "center")
            if state.meta then
                local earned={}
                for _,id in ipairs(AchievementContent.order) do
                    if state.meta.earned[id] then
                        earned[#earned+1]=AchievementContent.get(id).name
                    end
                end
                love.graphics.printf("Achievements this run: "..(#earned>0 and
                    table.concat(earned,", ") or "none"),VIEWPORT.x+35,380,
                    VIEWPORT.width-70,"center")
            end
        end
    end
    love.graphics.setScissor()
end

local function hud_meter(x,y,w,h,value,maximum,r,g,b)
    local fraction=math.max(0,math.min(1,value/math.max(1,maximum)))
    love.graphics.setColor(.09,.14,.15)
    love.graphics.rectangle('fill',x,y,w,h)
    love.graphics.setColor(r,g,b)
    love.graphics.rectangle('fill',x+2,y+2,math.floor((w-4)*fraction+0.5),h-4)
    love.graphics.setColor(.025,.045,.05,.85)
    for i=1,11 do
        love.graphics.rectangle('fill',x+math.floor(i*w/12),y+1,2,h-2)
    end
end

local function hud_rule(y)
    love.graphics.setColor(.23,.32,.33)
    love.graphics.rectangle('fill',PANEL_X,y,260,1)
end

local function hud_action(key,label,y)
    local g=love.graphics
    g.setColor(.13,.21,.22)
    g.rectangle('fill',PANEL_X,y,20,18)
    g.setColor(.47,.75,.77)
    g.rectangle('line',PANEL_X+.5,y+.5,19,17)
    g.print(key,PANEL_X+6,y+3)
    g.setColor(.9,.93,.85)
    g.print(label,PANEL_X+30,y+3)
end

local function draw_panel()
    local g=love.graphics
    local px=PANEL_X
    local player=Game.player(state)
    local class=Classes.for_player(player)
    local map=Content.map_for(state)
    local biome_id=state.stage and state.stage.biome_id or map.biome_id
    local biome=state.finale and 'FINAL SANCTUM' or
        biome_id and Biomes.get(biome_id).name:upper() or 'THE RUINS'
    if not (studio_theme and studio_theme:draw_panel(px-12,16,280,608)) then
        g.setColor(.055,.083,.087)
        g.rectangle('fill',px-12,16,280,608)
        g.setColor(.25,.36,.36)
        g.rectangle('line',px-11.5,16.5,279,607)
    end
    g.setColor(.025,.046,.05,.65)
    g.rectangle('fill',px-9,19,274,602)
    g.setFont(ui_body_font)
    g.setColor(.65,.84,.79)
    g.print(biome,px,27)
    g.setColor(.55,.66,.65)
    local location=state.finale and 'FINAL BOSS' or
        state.floor_order and ('STAGE '..state.stage.index..'  /  FLOOR '..state.active_floor_index)
        or state.stage and ('STAGE '..state.stage.index) or 'ADVENTURE'
    g.print(location,px,46)
    hud_rule(68)

    g.setColor(.93,.94,.82)
    g.print(class.name:upper()..'  ·  LV '..state.progression.level,px,80)
    if not (studio_theme and studio_theme:draw_icon('hp_icon',px,103,16)) then
        draw_visual(bound_visual('core:ui/hp_icon'),px,103,16,16)
    end
    g.setColor(.77,.83,.78)
    if not (studio_theme and studio_theme:draw_text('HEALTH',px+22,105,2)) then
        g.print('HEALTH',px+22,105)
    end
    local hp_text=('%d/%d'):format(player.hp,player.max_hp)
    if not (studio_theme and studio_theme:draw_text('HP '..hp_text,px+176,98,2)) then
        g.setFont(ui_display_font)
        g.setColor(player.hp*4<=player.max_hp and {1,.48,.42} or {.98,.88,.66})
        g.print(hp_text,px+260-g.getFont():getWidth(hp_text),94)
        g.setFont(ui_body_font)
    end
    local low=player.hp*4<=player.max_hp
    hud_meter(px,130,260,14,player.hp,player.max_hp,
        low and .94 or .57,low and .31 or .75,low and .31 or .45)

    local level=state.progression.level
    local previous=level>1 and Rewards.next_threshold(level-1) or 0
    local next_xp=Game.next_level_xp(state)
    local earned=state.progression.xp_total-previous
    local needed=next_xp-previous
    if not (studio_theme and studio_theme:draw_icon('xp_icon',px,155,16)) then
        draw_visual(bound_visual('core:ui/xp_icon'),px,155,16,16)
    end
    g.setColor(.77,.83,.78)
    local xp_label=('XP  %d/%d'):format(earned,needed)
    if not (studio_theme and studio_theme:draw_text(xp_label,px+22,156,2)) then
        g.print(xp_label,px+22,156)
    end
    hud_meter(px,174,260,8,earned,needed,.43,.72,.94)
    if not (studio_theme and studio_theme:draw_icon('currency_icon',px,192,16)) then
        draw_visual(bound_visual('core:ui/currency_icon'),px,192,16,16)
    end
    g.setColor(.98,.81,.44)
    local gold_label='GOLD  '..state.currency
    if not (studio_theme and studio_theme:draw_text(gold_label,px+22,193,2)) then
        g.print(gold_label,px+22,193)
    end
    hud_rule(214)

    g.setColor(.075,.13,.14,.95)
    g.rectangle('fill',px,224,260,59)
    local history=Playback.current(playback)
    local warning,detail,r,gc,b
    if history then
        local group=Playback.current_group(playback)
        if history.kind=='WindupStarted' then
            warning=group and (#group..' locked attacks') or 'ATTACK WINDUP'
            detail='Resolving · input pauses'
            r,gc,b=1,.62,.34
        elseif group and group[#group].kind=='Died' then
            warning='ENEMY DOWN';detail='Hit confirmed';r,gc,b=1,.84,.48
        elseif history.kind=='DamageTaken' then
            warning='HIT CONFIRMED';detail='Resolving · input pauses';r,gc,b=1,.84,.48
        elseif history.kind=='BoonActivated' and history.boon_id then
            warning='BOON ACTIVATED'
            detail=BoonContent.get(history.boon_id).name
            r,gc,b=.58,.83,1
        else
            warning='RESOLVING'
            detail=playback_cue_labels[history.kind] or 'Input resumes shortly'
            r,gc,b=.68,.82,.84
        end
    else
        local strikes={}
        for _,strike in ipairs(Game.pending_strikes(state)) do
            for _,cell in ipairs(strike.target_cells) do
                if visible_at(cell.x,cell.y) then strikes[#strikes+1]=strike;break end
            end
        end
        if #strikes>0 then
            warning='INCOMING ATTACK  '..#strikes
            detail=Content.ability_name(strikes[1].ability_id)..
                ' · '..math.max(0,strikes[1].due-state.clock)..' time'
            r,gc,b=1,.48,.42
        elseif state.stage and state.stage.phase=='extraction' then
            warning='REACH THE EXIT'
            detail=math.max(0,state.stage.deadline-state.clock)..' time left'
            r,gc,b=1,.56,.37
        elseif state.stage and state.stage.phase=='pursuit' then
            warning='PURSUER ACTIVE'
            detail='Reach the exit'
            r,gc,b=1,.46,.42
        elseif state.finale then
            local boss=Game.entity(state,state.finale.boss_id)
            warning='HOLLOW REGENT'
            detail=boss and ('Phase '..(boss.phase_two and 2 or 1)) or 'Defeated'
            r,gc,b=.85,.64,.96
        elseif state.stage then
            warning='DANGER  '..state.stage.pressure_count
            detail='Next threat in '..math.max(0,
                (state.stage.next_pressure or state.clock)-state.clock)
            r,gc,b=.85,.68,.43
        else
            warning='EXPLORE';detail='Find the way forward';r,gc,b=.57,.78,.72
        end
    end
    g.setColor(r,gc,b)
    g.rectangle('fill',px,224,3,59)
    g.printf(warning,px+12,234,244)
    g.setColor(.76,.83,.79)
    g.printf(detail,px+12,257,244)

    hud_rule(296)
    g.setColor(.6,.73,.72)
    g.print(targeting and 'AIMING' or 'ACTIONS',px,304)
    if targeting then
        local name=targeting.kind=='basic_attack' and class.basic_name or
            targeting.kind=='sweeping_slash' and class.special_name or class.dash_name
        g.setColor(.98,.83,.49)
        g.printf(name,px,323,260)
        g.setColor(.85,.9,.84)
        if targeting.dx then
            local preview=Game.preview(state,targeting.kind,targeting.dx,targeting.dy)
            g.print(preview.valid and 'Enter to strike' or
                (preview.reason or 'Blocked'),px,344)
        else g.print('Choose a direction',px,344) end
        g.setColor(.56,.7,.7)
        g.print('Esc cancel',px,365)
    else
        hud_action('F',class.basic_name,320)
        hud_action('R',class.special_name,342)
        hud_action('X',class.dash_name,364)
    end

    hud_rule(395)
    g.setColor(.6,.75,.83)
    local owned={}
    for _,id in ipairs(BoonContent.order) do
        local stacks=player.boons and player.boons[id]
        if stacks then
            local counts={}
            local short={common='C',uncommon='U',rare='R',legendary='L'}
            for _,rarity in ipairs(BoonContent.rarities) do
                if (stacks[rarity] or 0)>0 then
                    counts[#counts+1]=short[rarity]..stacks[rarity]
                end
            end
            if #counts>0 then
                owned[#owned+1]={id=id,label=BoonContent.get(id).name..
                    '  '..table.concat(counts,' ')}
            end
        end
    end
    local pages=math.max(1,math.ceil(#owned/7))
    local page=boon_page%pages
    g.print(('BOONS  %d/%d'):format(page+1,pages),px,404)
    g.setColor(.46,.65,.72)
    g.print('V page · click',px+132,404)
    for line=1,7 do
        local item=owned[page*7+line]
        if item then
            local y=BOON_ROW_Y+(line-1)*BOON_ROW_H
            local icon=bound_visual(item.id..'/icon')
            if icon then draw_visual(icon,px,y,12,12) end
            g.setColor(.72,.85,.92)
            local tx=px+(icon and 17 or 0)
            g.setScissor(tx,y,px+260-tx,14)
            g.print(item.label,tx,y)
            g.setScissor()
        end
    end
    if #owned==0 then
        g.setColor(.55,.67,.67)
        if not (studio_theme and studio_theme:draw_text('No boons yet',
            px,BOON_ROW_Y,2)) then
            g.print('No boons yet',px,BOON_ROW_Y)
        end
    end
    hud_rule(533)
    local item=reward_notice_time>0 and reward_notices[1] or nil
    local status=type(item)=='table' and item.text or item or notice
    if not item and (status=='Ready' or status:match('^.-: %+%d+ time$') or
        status:match('^Dash: %d+ tiles, %+%d+ time$')) then status='' end
    if status~='' then
        g.setColor(item and {.98,.82,.45} or {.78,.88,.81})
        g.setScissor(px,540,260,50)
        g.printf(status,px,542,260)
        g.setScissor()
    else
        g.setColor(.58,.72,.69)
        g.print('WASD move  ·  Space wait',px,543)
    end
    g.setColor(.46,.61,.62)
    g.print('Tab map  ·  +/- zoom  ·  Esc pause',px,600)
end

local function draw_reward_detail()
    local item=reward_notice_time>0 and reward_notices[1] or nil
    if type(item)~='table' or chest_menu or pause_menu then return end
    local art=bound_visual(item.binding) or
        (item.fallback and bound_visual(item.fallback))
    if not art then return end
    local x,y=VIEWPORT.x+VIEWPORT.width-174,VIEWPORT.y+VIEWPORT.height-150
    love.graphics.setColor(0.025,0.045,0.05,0.93)
    love.graphics.rectangle('fill',x,y,160,134)
    draw_visual(art,x+32,y+8,96,96)
    love.graphics.setColor(0.93,0.95,0.85)
    love.graphics.printf(item.text,x+6,y+107,148,'center')
end

local function draw_boon_detail()
    if not boon_detail_id then return end
    local definition=BoonContent.get(boon_detail_id)
    if not definition then return end
    local x,y=150,158
    love.graphics.setColor(0.02,0.04,0.05,0.97)
    love.graphics.rectangle('fill',x,y,550,305)
    love.graphics.setColor(0.68,0.83,0.78)
    love.graphics.rectangle('line',x,y,550,305)
    local art=bound_visual(boon_detail_id..'/detail') or
        bound_visual(boon_detail_id..'/icon')
    if art then draw_visual(art,x+18,y+50,192,192) end
    love.graphics.setColor(0.97,0.91,0.69)
    love.graphics.printf(definition.name,x+225,y+24,300)
    local stacks=Game.player(state).boons[boon_detail_id] or {}
    local rarity='common'
    for _,candidate in ipairs(BoonContent.rarities) do
        if (stacks[candidate] or 0)>0 then rarity=candidate end
    end
    love.graphics.setColor(0.88,0.91,0.88)
    love.graphics.printf(BoonContent.offer_text(boon_detail_id,rarity),
        x+225,y+65,300)
    love.graphics.print('Esc or click to close · no world time',x+18,y+275)
end

function love.draw()
    if title then
        if studio_theme and studio_theme:draw_background('title_background',0,0,
            love.graphics.getWidth(),love.graphics.getHeight()) then
            love.graphics.setColor(0,0,0,.45)
            love.graphics.rectangle('fill',0,0,love.graphics.getWidth(),
                love.graphics.getHeight())
        end
        love.graphics.setColor(.035,.07,.075,.95)
        love.graphics.rectangle('fill',250,78,524,460)
        love.graphics.setColor(.29,.43,.42)
        love.graphics.rectangle('line',250.5,78.5,523,459)
        love.graphics.setColor(.95,.86,.6)
        love.graphics.setFont(ui_display_font)
        love.graphics.printf('ROAG',250,105,524,'center')
        love.graphics.setFont(ui_body_font)
        love.graphics.setColor(.57,.76,.73)
        love.graphics.printf('SEVEN BIOMES',250,145,524,'center')
        local chosen=Classes.get(selected_class_id)
        love.graphics.setColor(.37,.57,.56)
        love.graphics.rectangle('fill',294,187,436,1)
        love.graphics.setColor(.64,.77,.75)
        love.graphics.printf('1–6  CHOOSE CLASS FOR NEW RUN',250,201,524,'center')
        love.graphics.setColor(.96,.94,.8)
        love.graphics.printf(chosen.name:upper(),250,225,524,'center')
        love.graphics.setColor(.77,.85,.79)
        love.graphics.printf(chosen.description,278,250,468,'center')
        love.graphics.setColor(.59,.73,.72)
        love.graphics.printf(('HP %d  ·  ATTACK %d  ·  DEFENSE %d  ·  SPEED %d')
            :format(chosen.hp,chosen.damage,chosen.defense,chosen.speed),
            250,285,524,'center')
        local can_continue=save_status and
            (save_status.status=="active" or save_status.status=="recovered")
        local saved_player=can_continue and save_status.state and
            Game.player(save_status.state)
        local saved_class=saved_player and Classes.get(saved_player.definition_id)
        love.graphics.setColor(.57,.72,.69)
        love.graphics.printf('Class choice applies to New Run only',250,313,524,'center')
        love.graphics.setColor(can_continue and {.95,.87,.61} or {.46,.57,.56})
        love.graphics.printf(can_continue and
            ('C / Enter: Continue saved '..(saved_class and saved_class.name or 'run')) or
            'Continue unavailable',250,340,524,'center')
        love.graphics.setColor(.94,.94,.82)
        love.graphics.printf('N: New Run as '..chosen.name,250,369,524,'center')
        love.graphics.setColor(.66,.8,.77)
        love.graphics.printf('U: Achievements',250,398,524,'center')
        love.graphics.setColor(.44,.61,.6)
        love.graphics.printf('Q / Esc: Quit  ·  Gamepad supported',250,478,524,'center')
        local status=save_status and save_status.status or "absent"
        if status=="pending_terminal" then
            love.graphics.setColor(1,.68,.48)
            love.graphics.printf('R: Retry reward recovery',250,432,524,'center')
        elseif status=='terminal' then
            love.graphics.setColor(.72,.83,.72)
            love.graphics.printf('RUN COMPLETE',250,432,524,'center')
        end
        if unlocks_view then
            love.graphics.setColor(0.02,0.05,0.05,0.96)
            love.graphics.rectangle("fill",75,75,874,490)
            love.graphics.setColor(0.91,0.96,0.81)
            love.graphics.printf("ACHIEVEMENTS · Esc to return",90,100,844,"center")
            local earned=profile and profile.earned or {}
            for index,id in ipairs(AchievementContent.order) do
                local def=AchievementContent.get(id)
                local label=earned[id] and "UNLOCKED" or "LOCKED"
                love.graphics.printf(("%s — %s · %s"):format(label,def.name,
                    BoonContent.get(def.reward).name),105,155+(index-1)*88,814,"left")
                love.graphics.printf(def.hint,125,181+(index-1)*88,780,"left")
            end
            if profile_status and (profile_status.status=="corrupt" or
                profile_status.status=="incompatible") then
                love.graphics.printf("Profile "..profile_status.status.." — progress preserved",
                    90,518,844,"center")
            end
            return
        end
        if confirm_new then
            love.graphics.setColor(1,0.72,0.38)
            love.graphics.printf("Replace saved run? Y / Enter confirm · Esc cancel",
                250,432,524,"center")
        end
        if persistence_message~="" then
            love.graphics.setColor(1,0.46,0.43)
            love.graphics.printf(persistence_message,275,550,474,"center")
        end
        return
    end
    if camera_state ~= state then reset_camera() end
    draw_world()
    draw_panel()
    if spectator_enabled then
        local actor=bound_visual(Game.player(state).definition_id)
        local source=actor and actor.source or 'built-in'
        local map=Content.map_for(state)
        local biome=Biomes.get(map.biome_id)
        local focus=spectator_free and spectator_focus or Game.player(state).position
        local symbol=map.rows[focus.y]:sub(focus.x,focus.x)
        local kind=symbol=='#' and 'wall' or symbol=='~' and 'water' or
            symbol=='.' and 'floor' or nil
        local tile_info=('tile %s at %d,%d · built-in'):format(symbol,
            focus.x,focus.y)
        if kind and visual_assets then
            local shape=VisualSlots.terrain_shape(map,focus.x,focus.y,kind)
            for _,binding in ipairs(VisualSlots.terrain_bindings(
                map.biome_id,kind,shape)) do
                local handle=visual_assets:resolve(binding)
                if handle then
                    tile_info=('%s · %s [%s%s]'):format(
                        ('tile %s at %d,%d'):format(symbol,focus.x,focus.y),
                        binding,handle.source,
                        VisualSlots.terrain_draws_at(kind,handle.source,
                            focus.x,focus.y) and '' or ' accent elsewhere')
                    break
                end
            end
        end
        love.graphics.setColor(0.01,0.025,0.025,0.91)
        love.graphics.rectangle('fill',VIEWPORT.x+7,
            VIEWPORT.y+VIEWPORT.height-77,VIEWPORT.width-14,72)
        love.graphics.setColor(0.94,0.92,0.65)
        love.graphics.setFont(ui_body_font)
        love.graphics.print(('SPECTATOR  %s  floor %d/2  %s  art: %s'):format(
            spectator_free and 'FREE CAMERA' or 'GOD PLAY',
            state.active_floor_index or 1,biome and biome.name or 'Unknown',source),
            VIEWPORT.x+15,VIEWPORT.y+VIEWPORT.height-72)
        love.graphics.setColor(0.72,0.86,0.82)
        love.graphics.print(tile_info,VIEWPORT.x+15,VIEWPORT.y+VIEWPORT.height-50)
        love.graphics.print('F6 camera/play  F7 biome  F8 floor  F9 class  M seed  Esc quit',
            VIEWPORT.x+15,VIEWPORT.y+VIEWPORT.height-28)
    end
    draw_reward_detail()
    draw_boon_detail()
    if persistence_message~="" and not pause_menu then
        love.graphics.setColor(1,0.48,0.43)
        love.graphics.printf(persistence_message,VIEWPORT.x+12,
            VIEWPORT.y+12,VIEWPORT.width-24,"center")
    end
    if confirm_reset_seed then
        love.graphics.setColor(0,0,0,0.84)
        love.graphics.rectangle("fill",160,200,704,230)
        love.graphics.setColor(1,0.75,0.48)
        love.graphics.printf(("Replace run with seed %d?"):format(confirm_reset_seed),
            160,255,704,"center")
        love.graphics.printf("Enter / Y confirm · Esc cancel",160,295,704,"center")
    end
    if pause_menu then
        love.graphics.setColor(0,0,0,0.83)
        love.graphics.rectangle("fill",160,150,704,330)
        if studio_theme then studio_theme:draw_background('pause_background',160,150,704,330) end
        love.graphics.setColor(0.94,0.95,0.85)
        love.graphics.printf("PAUSED",160,190,704,"center")
        love.graphics.printf("1 / Enter / Esc: Resume",160,255,704,"center")
        love.graphics.printf("2 / S: Save & Quit",160,290,704,"center")
        if persistence_message~="" then
            love.graphics.setColor(1,0.46,0.43)
            love.graphics.printf(persistence_message,180,355,664,"center")
        end
    end
end

-- Keep measured callback CPU durations separate from observed frame intervals.
-- This wrapper and the F3 overlay are presentation-only and disabled by default.
local ordinary_update,ordinary_draw=love.update,love.draw
function love.update(dt)
    if not diagnostics then return ordinary_update(dt) end
    local start=love.timer.getTime()
    ordinary_update(dt)
    diagnostics_update_ms=(love.timer.getTime()-start)*1000
end
function love.draw()
    if not diagnostics then return ordinary_draw() end
    local frame=love.timer.getTime()
    ordinary_draw()
    local draw_ms=(love.timer.getTime()-frame)*1000
    local visible=0
    if state and state.entities then
        for _,entity in ipairs(state.entities) do
            if entity.position and
                (entity.id==state.player_id or entity.controller=="ai") and
                visible_at(entity.position.x,entity.position.y) then
                visible=visible+1
            end
        end
    end
    local stats=love.graphics.getStats and love.graphics.getStats() or nil
    require("src.diagnostics").sample(diagnostics,frame,
        diagnostics_update_ms,draw_ms,visible,stats)
    local summary=require("src.diagnostics").summary(diagnostics)
    love.graphics.setColor(0,0,0,0.78)
    love.graphics.rectangle("fill",12,12,340,67)
    love.graphics.setColor(0.9,1,0.9)
    love.graphics.print(("F3 diag  FPS %d  actors %d  draws %s"):format(
        love.timer.getFPS(),visible,tostring(summary.draw_calls)),20,18)
    love.graphics.print(("frame p50/p95/p99 %.1f / %.1f / %.1f ms"):format(
        summary.frame_p50_ms or 0,summary.frame_p95_ms or 0,
        summary.frame_p99_ms or 0),20,37)
    love.graphics.print(("update/draw CPU p95 %.1f / %.1f ms"):format(
        summary.update_p95_ms or 0,summary.draw_p95_ms or 0),20,56)
end
function love.roagDiagnostics()
    return diagnostics and require("src.diagnostics").summary(diagnostics) or nil
end
-- Read-only seam for callback smoke tests: a blocked key during presentation
-- playback has not been submitted to the simulation.
function love.roagPlaybackPending()
    return Playback.busy(playback)
end
function love.roagMenuState()
    local menu=route_menu or chest_menu
    if not menu then return nil end
    return {kind=route_menu and 'route' or 'chest',selected=menu.selected,
        count=route_menu and #menu.routes or #menu.offers}
end
function love.roagAssetStatus()
    return visual_assets and visual_assets.status or 'absent'
end
function love.roagVisualRefreshStatus() return refresh_result end
function love.roagAudioStatus() return audio_runtime and audio_runtime.status or audio_status end
function love.roagAudioScene() return audio_runtime and audio_runtime.scene_id or 'silent' end
function love.roagAudioDiagnostics()
    if not audio_runtime then return {status=audio_status,played=0,voices=0} end
    return {status=audio_runtime.status,played=audio_runtime.played_count,
        voices=#audio_runtime.voices,scene=audio_runtime.scene_id,
        music_playing=audio_runtime.music~=nil and
            audio_runtime.music.source:isPlaying(),
        music_transitioning=audio_runtime.retiring_music~=nil,
        sound_count=audio_runtime.pack and #audio_runtime.pack.sounds or 0,
        binding_count=audio_runtime.pack and #audio_runtime.pack.bindings or 0,
        fallback_binding_count=audio_runtime.fallback_binding_count or 0}
end
function love.roagWorldPackStatus() return studio_world_status end
function love.roagThemeStatus() return studio_theme_status or 'absent' end
function love.roagAtlasStatus()
    return visual_assets and visual_assets.atlas_status or 'absent'
end
function love.roagAssetBacking(id)
    local handle=visual_assets and visual_assets:resolve_id(id)
    return handle and handle.source or 'fallback'
end
function love.roagThemeDrawCounts() return studio_theme and studio_theme.counts or {} end
function love.roagAssetResolved(binding)
    return visual_assets and visual_assets:resolve(binding)~=nil or false
end
function love.roagAssetIdResolved(id)
    return visual_assets and visual_assets:resolve_id(id)~=nil or false
end
function love.mousepressed(x,y,button,istouch,presses)
    if button~=1 then return end
    if boon_detail_id then boon_detail_id=nil;return end
    if not state or title or pause_menu or chest_menu or route_menu or
        Playback.busy(playback) or x<PANEL_X or x>1008 or
        y<BOON_ROW_Y or y>=BOON_ROW_Y+7*BOON_ROW_H then
        return
    end
    local owned=owned_boon_ids()
    local pages=math.max(1,math.ceil(#owned/7))
    local index=(boon_page%pages)*7+math.floor((y-BOON_ROW_Y)/BOON_ROW_H)+1
    boon_detail_id=owned[index]
end
function love.wheelmoved(dx,dy)
    if state and not title and dy~=0 then
        zoom_by(dy>0 and 1 or -1)
    end
end
