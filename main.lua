local Camera = require("src.camera")
local BoonContent = require("src.boon_content")
local Content = require("src.content")
local Classes = require("src.classes")
local Biomes = require("src.biomes")
local Game = require("src.game")
local Generation = require("src.generation")
local Playback = require("src.playback")
local AttackOutline = require("src.attack_outline")
local HitFeedback = require("src.hit_feedback")
local CombatNumbers = require("src.combat_numbers")
local UIFont = require("src.ui_fonts")
local Rewards = require("src.rewards")
local RunSeed = require("src.run_seed")
local Persistence = require("src.persistence")
local Profile = require("src.profile")
local AchievementContent = require("src.achievement_content")
local Achievements = require("src.achievements")
local storage = require("src.love_storage")
local KenneyVisuals = require("src.kenney_visuals")
local KenneyTerrain = require("src.kenney_terrain")
local OneBitVisuals = require("src.onebit_visuals")
local Terrain = require("src.terrain")
local EnemySquads = require('src.enemy_squads')
local IdeaEnemies = require('src.idea_enemies')
local VisualPacks = require("src.visual_packs")
local PresentationSettings = require("src.presentation_settings")
local StudioProject = require("src.studio_project")
local VisualSlots = require("src.visual_slots")
local Spectator = require("src.spectator")
local TitleMenu = require("src.title_menu")
local PixelUI = require("src.pixel_ui")
local StudioWorldRuntime = require("src.studio_world_runtime")
local StudioThemeRuntime = require("src.studio_theme_runtime")
local AudioRuntime = require("src.studio_audio_runtime")
local AudioCues = require("src.audio_cues")

local TILE=40 -- Fixed 5× native Kenney pixels; presentation only.
local VIEWPORT = { x = 16, y = 16, width = 992, height = 520 }
local inventory_open,inventory_selection=false,1
local help_open=false
local pause_selection=1

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
local ui_body_font,ui_display_font,ui_menu_font
local presentation_settings=PresentationSettings.defaults()
local minimap_visible = true
local showcase_index = 0
local boon_page = 0
local boon_detail_id
local floor_seed = 1
local FRESH_RUN_SEED='fresh'
local selected_class_id=Classes.order[1]
local player_facing=1
local chest_menu -- presentation focus only; offers live in authoritative chest state.
local route_menu -- exit offers are plain data in the authoritative exit entity.
local reward_notices, reward_notice_time = {}, 0
local save_service=Persistence.new(storage)
local profile_service = Profile.new(storage)
local profile_status,profile,unlocks_view
local title, pause_menu, confirm_new, save_status = true, false, false, nil
local title_view='home' -- home, continue, class, settings
local title_selection=1
local confirm_reset_seed
local persistence_message = ""
local diagnostics = os.getenv("ROAG_DIAGNOSTICS")=="1" and
    require("src.diagnostics").new() or nil
local diagnostics_update_ms=0
local visual_assets
local visual_pack_mode=VisualPacks.default
local visual_pack_status='absent'
local visual_pack_generated_status='disabled'
local visual_pack_message=''
local settings_message=''
local pause_settings=false
local settings_page=1
local settings_selection=1
local achievement_page=0
local terrain_cache_assets,terrain_cache=nil,{}
local studio_world_status='absent'
local studio_theme,studio_theme_status
local refresh_pending=false
local pending_pack_mode
local refresh_result='not requested'
local visual_refresh_message=''
local next_world_pack
local visual_signature
local audio_runtime,audio_status
-- Applied to every 1-Bit draw call, including built-in fallback effects and HUD.
-- Sprite pack selection remains a presentation setting; gameplay colors are intact.
local onebit_grayscale_shader
local onebit_grayscale_source=[[
vec4 effect(vec4 color, Image texture, vec2 texture_coords, vec2 screen_coords)
{
    vec4 pixel = Texel(texture, texture_coords) * color;
    float gray = dot(pixel.rgb, vec3(0.2126, 0.7152, 0.0722));
    return vec4(gray, gray, gray, pixel.a);
}
]]
local function set_visual_style(style)
    PixelUI.set_style(style,presentation_settings.high_contrast)
    if style=='onebit' then
        love.graphics.setBackgroundColor(.055,.055,.055)
    else
        love.graphics.setBackgroundColor(.055,.075,.075)
    end
end
local function apply_presentation_settings()
    ui_body_font,ui_display_font,ui_menu_font=UIFont.load(
        presentation_settings.font,presentation_settings.text_size)
    love.graphics.setFont(ui_body_font)
    if love.audio.setVolume then
        love.audio.setVolume(presentation_settings.master/100)
    end
    if audio_runtime then
        audio_runtime:set_user_mix(presentation_settings.music/100,
            presentation_settings.effects/100)
    end
    set_visual_style(visual_assets and visual_assets.generated and
        visual_assets.generated.style)
end
local function save_presentation_settings()
    apply_presentation_settings()
    local ok,reason=PresentationSettings.save(storage,presentation_settings)
    if not ok then settings_message='Settings not saved: '..tostring(reason)
    else settings_message='Settings saved' end
end
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
    open_door = "Open Door", open_vault="Open Vault",
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
    inventory_open,help_open=false,false
    local map = Content.map_for(state)
    local position = Game.player(state).position
    if spectator_enabled then spectator_focus={x=position.x,y=position.y} end
    camera = Camera.new(map, VIEWPORT, TILE, position.x, position.y)
    hit_feedback = HitFeedback.new()
    camera_state = state
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

local function load_native_visuals(read,mode)
    local loader=mode=='onebit' and OneBitVisuals or KenneyVisuals
    local ok,visuals,status=pcall(loader.new,read,
        function(bytes,path,transparent)
            local data=love.filesystem.newFileData(bytes,path)
            local pixels=love.image.newImageData(data)
            if transparent=='onebit-surfaces' then
                local source=pixels
                pixels=OneBitVisuals.surface_sheet(source)
                source:release()
            elseif transparent=='caves-surfaces' then
                local source=pixels
                pixels=KenneyTerrain.surface_sheet(source)
                source:release()
            elseif transparent then
                pixels:mapPixel(function(x,y,r,g,b,a)
                    if math.floor(r*255+.5)==34 and math.floor(g*255+.5)==35
                        and math.floor(b*255+.5)==35 then return 0,0,0,0 end
                    return r,g,b,a
                end)
            end
            local image=love.graphics.newImage(pixels)
            image:setFilter('nearest','nearest')
            return image
        end)
    if not ok then return nil,'rejected: '..tostring(visuals) end
    return visuals,status
end

local function make_visual_image(bytes,asset)
    local data=love.filesystem.newFileData(bytes,asset.source)
    local pixels=love.image.newImageData(data)
    local image=love.graphics.newImage(pixels)
    image:setFilter(asset.filter,asset.filter)
    return image,pixels
end

local function build_selected_visuals(mode,read)
    return VisualPacks.build(mode,read,make_visual_image,
        load_native_visuals)
end

function love.load(args)
    presentation_settings=PresentationSettings.load(storage)
    ui_body_font,ui_display_font,ui_menu_font=UIFont.load(
        presentation_settings.font,presentation_settings.text_size)
    love.graphics.setFont(ui_body_font)
    local audio_ok,audio=pcall(AudioRuntime.new,
        function(path)return love.filesystem.read(path)end,
        function(bytes,sound,source_type)
            local data=love.filesystem.newFileData(bytes,sound.id..'.'..sound.format)
            return love.audio.newSource(data,source_type)
        end)
    if audio_ok then audio_runtime=audio;audio_status=audio.status
    else audio_status='silent: '..tostring(audio) end
    if love.audio.setVolume then
        love.audio.setVolume(presentation_settings.master/100)
    end
    if audio_runtime then
        audio_runtime:set_user_mix(presentation_settings.music/100,
            presentation_settings.effects/100)
    end
    local preference=VisualPacks.inspect(storage)
    visual_pack_mode,visual_pack_status=preference.mode,preference.status
    local preview_pack=os.getenv('ROAG_SPRITE_PACK')
    if preview_pack and VisualPacks.valid(preview_pack) then
        visual_pack_mode=preview_pack -- launch override, never writes settings
    end
    settings_selection=VisualPacks.index(visual_pack_mode)
    local read_visual=function(path)return love.filesystem.read(path)end
    visual_assets,visual_pack_generated_status=
        build_selected_visuals(visual_pack_mode,read_visual)
    local generated,generated_status=visual_assets.generated,
        visual_pack_generated_status
    set_visual_style(generated and generated.style)
    if generated_status:match('^rejected') then
        print('Kenney art fallback: '..generated_status)
    end
    if VisualPacks.uses_published_theme(visual_pack_mode) then
        studio_theme,studio_theme_status=StudioThemeRuntime.inspect(read_visual,
            visual_assets)
    else studio_theme,studio_theme_status=nil,'disabled by sprite pack' end
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
        notice=generated and ('SPECTATOR · '..(generated.style=='kenney' and 'Kenney' or 'generated')..' art loaded · F6 camera') or
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
        title_selection=TitleMenu.default_selection(save_status)
        if studio_world_status~='absent' and studio_world_status~='loaded' then
            persistence_message=(persistence_message~='' and persistence_message..' · ' or '')..
                'Studio world: '..studio_world_status
        end
        if studio_theme_status~='absent' and studio_theme_status~='loaded' and
            studio_theme_status~='disabled by sprite pack' then
            persistence_message=(persistence_message~='' and persistence_message..' · ' or '')..
                'Studio theme: '..studio_theme_status
        end
        if studio_theme and studio_theme.warning then
            persistence_message=(persistence_message~='' and persistence_message..' · ' or '')..
                'Studio theme fallback: '..studio_theme.warning
        end
    end
    sync_audio_scene()
end

-- Build all replacement GPU handles before publishing any of them.  This is
-- presentation state only; the active concrete floor is never regenerated.
local function refresh_visuals(requested_mode)
    local mode=requested_mode or visual_pack_mode
    local switching=requested_mode~=nil
    local read=function(path) return love.filesystem.read(path) end
    local ok,candidate,theme,theme_status,world,world_status,
        generated_status=pcall(function()
        local visuals,generated_status=build_selected_visuals(mode,read)
        assert(not generated_status:match('^rejected'),
            'Kenney art: '..generated_status)
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
        local new_theme,new_theme_status
        if VisualPacks.uses_published_theme(mode) then
            new_theme,new_theme_status=StudioThemeRuntime.inspect(read,visuals)
        else new_theme,new_theme_status=nil,'disabled by sprite pack' end
        if not (new_theme_status=='loaded' or new_theme_status=='absent' or
            new_theme_status=='disabled by sprite pack' or
            tostring(new_theme_status):match('^recovered')) then
            if switching then
                new_theme=nil
                new_theme_status='fallback: '..tostring(new_theme_status)
            else error('theme: '..tostring(new_theme_status)) end
        end
        local new_world,new_world_status=next_world_pack,studio_world_status
        if not switching then
            new_world,new_world_status=StudioWorldRuntime.inspect(read)
            assert(new_world_status=='loaded' or new_world_status=='absent' or
                tostring(new_world_status):match('^recovered'),
                'world: '..tostring(new_world_status))
            if new_world then
                local manifest=read('assets/studio/manifest.txt')
                local published_assets={}
                if manifest then
                    local decoded,project=pcall(StudioProject.decode,manifest)
                    assert(decoded,'world: published visual manifest invalid')
                    published_assets=project.assets
                end
                local valid,why=pcall(require('src.studio_world').validate,
                    new_world,published_assets)
                assert(valid,'world: '..tostring(why))
            end
        end
        return visuals,new_theme,new_theme_status,new_world,new_world_status,
            generated_status
    end)
    if not ok then
        refresh_result='rejected: '..tostring(candidate)
        local brief=tostring(candidate):find('theme:',1,true) and 'theme pack invalid' or
            tostring(candidate):find('world:',1,true) and 'world pack invalid' or
            tostring(candidate):find('visual manifest:',1,true) and 'visual manifest invalid' or
            'missing/corrupt visual'
        notice='F5 art rejected: '..brief
        if switching then
            visual_pack_message='Sprite pack unchanged: '..brief
        else visual_pack_message='F5 rejected; previous visuals retained: '..brief
        end
        visual_refresh_message=refresh_result
        print('Visual refresh '..refresh_result)
        return false
    end
    local signature=visual_fingerprint(candidate,theme,world)
    local same=signature==visual_signature
    visual_assets,studio_theme,studio_theme_status=candidate,theme,theme_status
    set_visual_style(candidate.generated and candidate.generated.style)
    visual_pack_generated_status=generated_status
    studio_world_status=world_status
    -- A run owns concrete floor geometry. World templates are installed only
    -- for a fresh run from the title, never into an existing run.
    next_world_pack=world
    visual_signature=signature
    if title and not state then Generation.install_studio_pack(world) end
    local backing=candidate.atlas_status
    if candidate.status=='absent' then backing='built-in fallback'
    elseif candidate.status=='generated' then
        backing=VisualPacks.labels[mode]
    elseif backing~='loaded' and backing~='recovered previous atlas generation' then
        backing='published loose ('..tostring(backing)..')'
    else backing='atlas' end
    refresh_result=(same and 'checked' or 'refreshed')..' · '..backing..
        ((state and world_status=='loaded') and ' · world geometry deferred' or '')..
        (theme and theme.warning and ' · theme fallback: '..theme.warning or '')
    local brief=candidate.status=='absent' and 'built-in' or
        (candidate.generated and backing or 'built-in')
    notice='F5 art '..(same and 'unchanged' or 'refreshed')..' · '..brief..
        (theme and theme.warning and ' · theme fallback' or '')
    if switching then
        visual_pack_mode=mode
        local saved,reason=VisualPacks.save(storage,mode)
        if saved then
            visual_pack_status='loaded'
            visual_pack_message='Selected '..VisualPacks.labels[mode]..' · '..backing
        else
            visual_pack_message='Selected for this session; settings not saved: '..
                tostring(reason)
        end
        local fallback
        if (mode=='kenney' or mode=='onebit') and generated_status~='loaded' then
            fallback=VisualPacks.labels[mode]..' sheet '..tostring(generated_status)..
                '; using built-in fallback'
        elseif tostring(theme_status):match('^fallback:') then
            fallback=theme_status
        end
        if fallback then visual_pack_message=visual_pack_message..' · '..fallback end
        notice=visual_pack_message
    else visual_pack_message=''
    end
    visual_refresh_message=refresh_result
    if not same then print('Visual refresh '..refresh_result) end
    collectgarbage('step',200)
    return true
end

function love.update(dt)
    if refresh_pending or pending_pack_mode then
        local requested=pending_pack_mode
        local reload_audio=refresh_pending
        refresh_pending=false
        pending_pack_mode=nil
        refresh_visuals(requested)
        if reload_audio and audio_runtime then
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
    if presentation_settings.shake then
        Camera.set_presentation_offset(camera, HitFeedback.offset(hit_feedback))
    else Camera.set_presentation_offset(camera,0,0) end
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
    seed=seed or RunSeed.for_serial(updated.next_run_serial-1,floor_seed)
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
    pause_settings=false
    title_view='home'
    save_status=nil
    persistence_message=reason or ""
    targeting,chest_menu,route_menu=nil,nil,nil
    playback=Playback.new()
    combat_numbers=CombatNumbers.new()
    notice=('New run · seed %d · %s'):format(seed,Classes.get(selected_class_id).name)
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
    pause_settings=false
    title_view='home'
    save_status=nil
    persistence_message=found.status=="recovered"
        and "Recovered previous valid save generation" or ""
    targeting,chest_menu,route_menu=nil,nil,nil
    playback=Playback.new() -- resolved presentation never replays authoritative effects.
    combat_numbers=CombatNumbers.new()
    notice="Continued saved run"
    reset_camera()
end

local function resumable(found)
    return TitleMenu.resumable(found)
end

local function request_new_run()
    save_status=save_service:inspect()
    if save_status.status=='pending_terminal' then
        persistence_message='Pending unlock recovery; press R to retry'
    elseif save_status.status=='active' or save_status.status=='recovered' or
        save_status.status=='incompatible' or save_status.status=='corrupt' then
        confirm_new=true
    else begin_new() end
end

local function settings_reset_focus()
    settings_page=1
    settings_selection=VisualPacks.index(visual_pack_mode)
end
local function activate_title_selection()
    if title_view=='home' then
        if title_selection==1 then
            title_view='continue'
        elseif title_selection==2 then
            title_view='class'
        elseif title_selection==3 then unlocks_view=true
        elseif title_selection==4 then
            title_view='settings'
            settings_reset_focus()
        else love.event.quit() end
    elseif title_view=='continue' then
        if resumable(save_status) then continue_run()
        else persistence_message='No compatible active run to continue' end
    elseif title_view=='class' then request_new_run() end
end

local settings_tabs={'SPRITES','SOUND','EFFECTS','ACCESSIBILITY'}
local settings_lengths={#VisualPacks.order,3,4,4}
local function settings_change(direction)
    if settings_page==1 then
        if direction~=0 then
            settings_selection=(settings_selection-1+direction+
                #VisualPacks.order)%#VisualPacks.order+1
        else
            pending_pack_mode=VisualPacks.order[settings_selection]
            visual_pack_message='Loading '..VisualPacks.labels[pending_pack_mode]..'…'
        end
        return
    end
    if settings_page==2 then
        local key=({'master','music','effects'})[settings_selection]
        presentation_settings[key]=math.max(0,math.min(100,
            presentation_settings[key]+(direction==0 and 1 or direction)*10))
    elseif settings_page==3 then
        local key=({'shake','flash','combat_effects','damage_numbers'})[settings_selection]
        presentation_settings[key]=not presentation_settings[key]
    else
        if settings_selection==1 then
            presentation_settings.font=presentation_settings.font=='pixel' and 'sans' or 'pixel'
        elseif settings_selection==2 then
            presentation_settings.text_size=presentation_settings.text_size=='normal' and 'large' or 'normal'
        elseif settings_selection==3 then
            presentation_settings.high_contrast=not presentation_settings.high_contrast
        else
            presentation_settings=PresentationSettings.defaults()
        end
    end
    save_presentation_settings()
end
local function settings_keypressed(key,on_title)
    if key=='up' or key=='w' then
        settings_selection=(settings_selection+settings_lengths[settings_page]-2)%
            settings_lengths[settings_page]+1
    elseif key=='down' or key=='s' then
        settings_selection=settings_selection%settings_lengths[settings_page]+1
    elseif key=='tab' or key==']' then
        settings_page=settings_page%#settings_tabs+1
        settings_selection=1
    elseif key=='[' then
        settings_page=(settings_page+#settings_tabs-2)%#settings_tabs+1
        settings_selection=1
    elseif key=='left' or key=='a' then settings_change(-1)
    elseif key=='right' or key=='d' then settings_change(1)
    elseif key=='return' or key=='kpenter' or key=='space' then
        settings_change(0)
    elseif key=='escape' or key=='b' then
        if on_title then title_view='home';title_selection=4
        else pause_settings=false end
    end
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
                local message="Achievement: "..
                    AchievementContent.get(event.achievement_id).name
                if event.reward_boon_id then
                    message=message.." → "..BoonContent.get(event.reward_boon_id).name
                end
                reward_notices[#reward_notices+1]=message
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
            local dash_name=Classes.for_player(Game.player(state)).dash_name
            notice = ("%s: %d tiles, +%d time"):format(dash_name,action.distance, action.cost)
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
            local class=Classes.for_player(Game.player(state))
            local name=action.kind=='basic_attack' and class.basic_name or
                action.kind=='sweeping_slash' and class.special_name or
                labels[action.kind] or action.kind
            notice = ("%s: +%d time"):format(name, action.cost)
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
            elseif event.kind=='DamageBlocked' and event.source_id==state.player_id and event.block_reason then
                notice=event.block_reason=='forcefield' and
                    'Forcefield: reach the Ward Weaver or lure the guard out of range' or
                    'Frontal shield: attack from the side/rear or interrupt the bearer'
            elseif event.kind=='EnemySnareTriggered' and event.target_id==state.player_id then
                notice='Snare: 1 damage and +40 recovery time'
            elseif event.kind=='ShoveBlocked' and event.target_id==state.player_id then
                notice='Shove blocked: '..event.reason
            elseif event.kind=='PhaseBlocked' then
                notice='Wraith phase blocked: destination occupied, out of sight or protected'
            elseif event.kind=='EnemyRevived' and event.floor_id==state.active_floor_id
                and visible_at(event.x,event.y) then
                notice='Grave Cantor: one weakened revival; no repeat rewards'
            elseif event.kind=='BoneFragmented' and event.floor_id==state.active_floor_id
                and visible_at(event.x,event.y) then
                notice='Bone Splitter: finish the two weak fragments; no repeat rewards'
            elseif event.kind=='AlarmReinforcement' and event.floor_id==state.active_floor_id
                and visible_at(event.x,event.y) then
                notice='Alarm Keeper: its only call has resolved; reinforcements give no extra rewards'
            elseif event.kind=='WebCreated' and event.floor_id==state.active_floor_id
                and visible_at(event.x,event.y) then
                notice='Web: crossing adds 50 recovery; strike either anchor to cut the line'
            elseif event.kind=='WebCrossed' and event.target_id==state.player_id then
                notice='Web crossed: +50 recovery; line consumed'
            elseif event.kind=='WebAnchorCut' and event.floor_id==state.active_floor_id
                and visible_at(event.x,event.y) then
                notice='Web anchor cut: line cleared'
            elseif event.kind=='RevivalBlocked' and event.floor_id==state.active_floor_id
                and visible_at(event.x,event.y) then
                notice='Revival blocked: the Cantor has spent its only attempt'
            elseif event.kind=='DropSwallowed' and Game.is_visible(state,event.floor_id,event.x,event.y) then
                notice='Gulp Toad swallowed '..event.amount..' '..event.drop_kind..'; defeat it to recover'
            elseif event.kind=='DropRegurgitated' and Game.is_visible(state,event.floor_id,event.x,event.y) then
                notice='Recovered drop: '..event.amount..' '..event.drop_kind
            elseif event.kind=='Displaced' and event.target_id==state.player_id and event.reason=='shove' then
                notice='Rammer shove: forced one tile'
            elseif event.kind=="VaultKeyCollected" then
                notice="Vault key found — seek the barred side room"
            elseif event.kind=="VaultOpened" then
                notice="Vault opened: "..tostring(event.outcome):gsub('_',' ')
            elseif event.kind=="VaultClueFound" then
                notice="Vault clue points "..tostring(event.direction)
            elseif event.kind=="WardFlaskCollected" then
                notice="Ward flask: one full block within 3 actions"
            elseif event.kind=="WardFlaskBlocked" then
                notice="Ward flask blocked the hit"
            elseif event.kind=="TimedGateOpened" then
                notice="Timed gate open: 300 time"
            elseif event.kind=="TimedGateClosed" then
                notice="Timed gate closed"
            elseif event.kind=="BrazierLit" then
                notice="Beacon lit: nearby mist cleared for 300 time"
            elseif event.kind=="OreBroken" then
                notice="Ore broken: currency released"
            elseif event.kind=='SidequestClueFound' then
                notice='Hidden passage clue found'
            elseif event.kind=='SidequestKeyFound' then
                notice='Side passage key found'
            elseif event.kind=='SidequestPlatePressed' then
                notice='Side passage opened'
            elseif event.kind=='SidequestBrazierLit' then
                notice='Side brazier lit'
            elseif event.kind=='SidequestRewardReady' then
                notice='Sidequest reward is ready'
            elseif event.kind=='SidequestCompleted' then
                notice='Sidequest complete: +'..tostring(event.amount)..' gold'
            elseif event.kind=="RingReversalPrimed" then
                notice="Reversal primed: strike the escaped attacker"
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

local function title_keypressed(key)
    if unlocks_view then
        local pages=math.max(1,math.ceil(#AchievementContent.order/5))
        if key=='escape' or key=='u' then unlocks_view=false
        elseif key=='right' or key=='down' or key=='d' or key=='s' then
            achievement_page=(achievement_page+1)%pages
        elseif key=='left' or key=='up' or key=='a' or key=='w' then
            achievement_page=(achievement_page+pages-1)%pages end
        return
    end
    if confirm_new then
        if key=='y' or key=='return' or key=='kpenter' then begin_new()
        elseif key=='n' or key=='escape' then confirm_new=false end
        return
    end
    if title_view=='settings' then settings_keypressed(key,true);return end
    if key=='r' and save_status and save_status.status=='pending_terminal' then
        local pending=save_status.state.meta and save_status.state.meta.pending or {}
        local ok,updated=profile_service:commit_awards(pending)
        if ok then
            profile=updated
            local done,why=save_service:tombstone()
            if done then
                save_status=save_service:inspect()
                profile_status=profile_service:inspect()
                persistence_message='Terminal unlock recovered and saved'
            else persistence_message='Terminal save needs retry: '..tostring(why) end
        else persistence_message='Terminal unlock recovery needs retry: '..tostring(updated) end
        return
    end
    local class_index=tonumber(key)
    if title_view=='home' then
        if class_index and Classes.order[class_index] then
            selected_class_id=Classes.order[class_index]
            title_view='class'
            return
        elseif key=='up' or key=='w' then
            title_selection=(title_selection+3)%5+1
        elseif key=='down' or key=='s' then
            title_selection=title_selection%5+1
        elseif key=='return' or key=='kpenter' then
            activate_title_selection()
        elseif key=='c' then
            if resumable(save_status) then continue_run()
            else persistence_message='No compatible active run to continue' end
        elseif key=='n' then request_new_run() -- legacy quick-start shortcut
        elseif key=='u' then unlocks_view=true
        elseif key=='escape' or key=='q' then love.event.quit() end
    elseif title_view=='continue' then
        if key=='return' or key=='kpenter' or key=='c' then
            activate_title_selection()
        elseif key=='escape' then
            title_view='home';title_selection=TitleMenu.default_selection(save_status)
        end
    elseif title_view=='class' then
        if class_index and Classes.order[class_index] then
            selected_class_id=Classes.order[class_index]
        elseif key=='left' or key=='up' or key=='a' or key=='w' then
            local index=1
            for i,id in ipairs(Classes.order) do
                if id==selected_class_id then index=i;break end
            end
            selected_class_id=Classes.order[(index+#Classes.order-2)%
                #Classes.order+1]
        elseif key=='right' or key=='down' or key=='d' or key=='s' then
            local index=1
            for i,id in ipairs(Classes.order) do
                if id==selected_class_id then index=i;break end
            end
            selected_class_id=Classes.order[index%#Classes.order+1]
        elseif key=='return' or key=='kpenter' or key=='n' then
            request_new_run()
        elseif key=='escape' then
            title_view='home';title_selection=2
        end
    end
end

function love.keypressed(key, _, isrepeat)
    if isrepeat and not (spectator_enabled and spectator_free and cardinal[key]) then
        return
    end
    if help_open then
        if key=='f1' or key=='escape' or key=='return'then help_open=false end
        return
    end
    if key=='f1' then help_open=true;return end
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
        if title and (title_view=='home' or title_view=='class') and
            tonumber(key) and Classes.order[tonumber(key)] then
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
    if title then title_keypressed(key);return end
    if inventory_open then
        local owned=owned_boon_ids()
        local pages=math.max(1,math.ceil(#owned/7))
        local count=math.min(7,math.max(0,#owned-(boon_page%pages)*7))
        if key=='i' or key=='escape'then inventory_open=false
        elseif key=='up' or key=='w'then inventory_selection=math.max(1,inventory_selection-1)
        elseif key=='down' or key=='s'then inventory_selection=math.min(math.max(1,count),inventory_selection+1)
        elseif key=='v' or key=='right'then boon_page=boon_page+1;inventory_selection=1
        elseif key=='left'then boon_page=(boon_page-1)%pages;inventory_selection=1
        elseif key=='return' or key=='kpenter'then
            boon_detail_id=owned[(boon_page%pages)*7+inventory_selection]
        end
        return
    end
    if pause_menu then
        if pause_settings then settings_keypressed(key,false);return end
        if key=='up' or key=='w'then pause_selection=(pause_selection+1)%3+1;return end
        if key=='down'then pause_selection=pause_selection%3+1;return end
        if key=='return' or key=='kpenter'then key=tostring(pause_selection)end
        if key=="escape" or key=="1" then
            pause_menu=false
        elseif key=="2" or key=="s" then
            if checkpoint(true) then love.event.quit() end
        elseif key=='3' then
            pause_settings=true
            settings_reset_focus()
        end
        return
    end
    if confirm_reset_seed then
        if key=="escape" or key=="n" then
            confirm_reset_seed=nil
        elseif key=="return" or key=="kpenter" or key=="y" then
            local requested=confirm_reset_seed
            if os.getenv("ROAG_GUI_AUTOSTART")~="1" then
                local old_state=state
                begin_new(requested==FRESH_RUN_SEED and nil or requested)
                if state~=old_state then confirm_reset_seed=nil end
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
            confirm_reset_seed=key=="m" and floor_seed%2147483646+1 or
                FRESH_RUN_SEED
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
        if key=='up' or key=='left'then
            route_menu.selected=(route_menu.selected+#route_menu.routes-2)%#route_menu.routes+1;return
        elseif key=='down' or key=='right'then route_menu.selected=route_menu.selected%#route_menu.routes+1;return end
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
        if key=='up' or key=='left'then chest_menu.selected=(chest_menu.selected+1)%3+1;return
        elseif key=='down' or key=='right'then chest_menu.selected=chest_menu.selected%3+1;return end
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
        pause_selection=1
        pause_settings=false
        persistence_message=""
        return
    end
    if key == "tab" then
        minimap_visible = not minimap_visible
        notice = minimap_visible and "Map shown — no time" or "Map hidden — no time"
        return
    end
    if (key=='i' or key=='v') and not targeting then
        if key=='v'then boon_page=boon_page+1 end
        inventory_open=true;inventory_selection=1
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
    if title and not unlocks_view and not confirm_new and
        (title_view=='home' or title_view=='class') then
        if button=='leftshoulder' or button=='rightshoulder' then
            if title_view=='home' then
                title_view='class'
            end
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
    elseif pause_menu or inventory_open or help_open then
        key=({a='return',b='escape',x='3',y='2',start='escape',
            dpup='up',dpdown='down',dpleft='left',dpright='right'})[button]
    else
        key=({a='return',b='escape',x='f',y='r',
            rightshoulder='x',leftshoulder='v',start='escape',back='tab',
            dpup='w',dpdown='s',dpleft='a',dpright='d'})[button]
    end
    if key then love.keypressed(key) end
end

local outline_cache={}
local function draw_cell(cell, red, green, blue, alpha, outline)
    if not visible_at(cell.x,cell.y) then return end
    love.graphics.setColor(red, green, blue, alpha)
    local screen_x, screen_y = Camera.tile_top_left(camera, cell.x, cell.y)
    if outline then
        outline_cache[TILE]=outline_cache[TILE] or AttackOutline.rectangles(TILE)
        for _,rect in ipairs(outline_cache[TILE]) do
            local x,y=screen_x+rect[1],screen_y+rect[2]
            love.graphics.setColor(0.08,0.02,0.02,0.85)
            love.graphics.rectangle('fill',x-1,y-1,rect[3]+2,rect[4]+2)
            love.graphics.setColor(red,green,blue,math.min(1,alpha+0.5))
            love.graphics.rectangle('fill',x,y,rect[3],rect[4])
        end
    else
        love.graphics.rectangle("fill", screen_x + 2, screen_y + 2, TILE - 4, TILE - 4)
        love.graphics.setColor(red, green, blue, math.min(1, alpha + 0.3))
        love.graphics.rectangle("line", screen_x + 2, screen_y + 2, TILE - 4, TILE - 4)
        love.graphics.setColor(red, green, blue, alpha)
    end
end

local function draw_cells(cells, red, green, blue, alpha, outline)
    for _, cell in ipairs(cells) do
        draw_cell(cell,red,green,blue,alpha,outline)
    end
end

local function draw_visual(handle,x,y,w,h,alpha,flip_x)
    if not handle then return false end
    local tint=handle.tint
    love.graphics.setColor(tint and tint[1] or 1,tint and tint[2] or 1,
        tint and tint[3] or 1,alpha or 1)
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
local function menu_frame(title,x,y,w,h)
    if studio_theme and studio_theme:draw_panel(x,y,w,h)then
        PixelUI.center(title,y-6,ui_display_font,PixelUI.colors.selected,x,w)
    else PixelUI.frame(title,x,y,w,h,ui_display_font)end
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
            local sprite_scale=VisualSlots.enemy_size(enemy.definition_id,TILE)/TILE
            local sprite_x=x+TILE*(1-sprite_scale)*.5
            local sprite_y=y+TILE*(1-sprite_scale)
            local enemy_art=actor_visual(enemy.definition_id,enemy.id) or
                (visual_assets and visual_assets:resolve_id(enemy.definition_id))
            local dormant_cache=enemy.definition_id=='core:enemy/haunted_cache'
                and not enemy.ai_memory.alerted and enemy.hp==enemy.max_hp
            if dormant_cache then enemy_art=bound_visual('core:object/idea_cache_dormant') or enemy_art end
            if enemy_art then
                if enemy_art.source=='generated' then
                    love.graphics.setColor(0,0,0,.45)
                    love.graphics.ellipse('fill',x+TILE*.5,y+TILE*.87,TILE*.30,TILE*.08)
                end
                local facing=Playback.facing(playback,enemy.id,enemy.pending_attack,
                    enemy.ai_memory and enemy.ai_memory.last_dx)
                draw_visual(enemy_art,sprite_x,sprite_y,TILE*sprite_scale,TILE*sprite_scale,nil,facing==-1)
                if dormant_cache then
                    love.graphics.setColor(1,.75,.4,1)
                    love.graphics.print('?',x+TILE*.68,y)
                end
            else
            -- Built-in tokens were drawn on a 32px tile. Scale them with the
            -- camera so fallback actors retain the same tile alignment.
            love.graphics.push()
            love.graphics.translate(sprite_x,sprite_y)
            love.graphics.scale(TILE/32*sprite_scale)
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
            elseif enemy.ai_id=='idea_regular' then
                love.graphics.setColor(.79,.70,.48)
                love.graphics.polygon('fill',x+16,y+2,x+29,y+17,x+16,y+28,x+3,y+17)
                love.graphics.setColor(.08,.12,.14)
                local label=({['core:enemy/heavy_rammer']='R',['core:enemy/hollow_wraith']='W',
                    ['core:enemy/grave_cantor']='C'})[enemy.definition_id]
                    or Content.get_enemy(enemy.definition_id).name:sub(1,1)
                love.graphics.print(dormant_cache and '?' or label,x+12,y+8)
            elseif enemy.ai_id=='squad' then
                love.graphics.setColor(.68,.72,.94)
                love.graphics.polygon('fill',x+16,y+2,x+29,y+17,x+16,y+28,x+3,y+17)
                love.graphics.setColor(.08,.12,.14)
                love.graphics.print(Content.get_enemy(enemy.definition_id).name:sub(1,1),x+12,y+8)
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
                local burning=bound_visual('core:ui/burning_effect')
                if burning then
                    draw_visual(burning,x+TILE*.66,y-TILE*.06,TILE*.32,TILE*.32)
                else
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
end

local function draw_squad_cues()
    for _,web in ipairs(state.idea_webs or {})do
        if web.floor_id==state.active_floor_id then
            love.graphics.setColor(.85,.83,.94,.85)
            for i=0,31 do
                local a,b=i/32,(i+1)/32
                local tx=web.ax+(web.bx-web.ax)*(a+b)/2
                local ty=web.ay+(web.by-web.ay)*(a+b)/2
                if visible_at(math.floor(tx+.5),math.floor(ty+.5))then
                    local x1,y1=Camera.tile_top_left(camera,web.ax+(web.bx-web.ax)*a,web.ay+(web.by-web.ay)*a)
                    local x2,y2=Camera.tile_top_left(camera,web.ax+(web.bx-web.ax)*b,web.ay+(web.by-web.ay)*b)
                    love.graphics.line(x1+TILE*.5,y1+TILE*.5,x2+TILE*.5,y2+TILE*.5)
                end
            end
            for _,p in ipairs({{web.ax,web.ay},{web.bx,web.by}})do
                if visible_at(p[1],p[2])then
                    local x,y=Camera.tile_top_left(camera,p[1],p[2])
                    love.graphics.circle('line',x+TILE*.5,y+TILE*.5,TILE*.12)
                end
            end
        end
    end
    for _,corpse in ipairs(state.idea_corpses or {})do
        if corpse.floor_id==state.active_floor_id and visible_at(corpse.x,corpse.y)then
            local x,y=Camera.tile_top_left(camera,corpse.x,corpse.y)
            if not draw_visual(bound_visual('core:ui/idea_corpse_effect'),
                x+TILE*.25,y+TILE*.25,TILE*.5,TILE*.5,.65)then
                love.graphics.setColor(.7,.7,.85,.7)
                love.graphics.print('+',x+TILE*.4,y+TILE*.3)
            end
        end
    end
    local function badge(name,x,y)
        if not draw_visual(bound_visual('core:ui/squad_'..name..'_effect'),
            x+TILE*.68,y-TILE*.08,TILE*.34,TILE*.34,.95) then
            love.graphics.setColor(.8,.92,1)
            love.graphics.print(({ward='W',heal='+',shield='S',rally='!'})[name] or '*',x+TILE*.72,y)
        end
    end
    for _,enemy in ipairs(state.entities) do
        local p=enemy.position
        if enemy.controller=='ai' and p.floor_id==state.active_floor_id and visible_at(p.x,p.y) then
            local x,y=Camera.tile_top_left(camera,p.x,p.y)
            local role=EnemySquads.roles[enemy.definition_id]
            local carried=enemy.carried_drop_id and IdeaEnemies.drop(state,enemy.carried_drop_id)
            if enemy.definition_id=='core:enemy/alarm_keeper' and
                (not enemy.ai_memory.alarm_spent or enemy.pending_attack
                    and enemy.pending_attack.support and enemy.pending_attack.support.kind=='alarm')then
                draw_visual(bound_visual('core:ui/idea_alarm_effect'),
                    x+TILE*.68,y-TILE*.08,TILE*.34,TILE*.34,.95)
            end
            if carried then
                draw_visual(bound_visual('core:drop/'..carried.kind),
                    x+TILE*.68,y-TILE*.08,TILE*.34,TILE*.34,.95)
            end
            local leader=EnemySquads.ward_source(state,enemy) or EnemySquads.rally_source(state,enemy)
            if leader and visible_at(leader.position.x,leader.position.y) then
                local lp=leader.position
                local steps=math.max(math.abs(lp.x-p.x),math.abs(lp.y-p.y))*4
                love.graphics.setColor(.56,.86,1,.6)
                for i=1,steps do
                    local t=i/steps
                    local tx,ty=p.x+(lp.x-p.x)*t,p.y+(lp.y-p.y)*t
                    if visible_at(math.floor(tx+.5),math.floor(ty+.5)) then
                        local sx,sy=Camera.tile_top_left(camera,tx,ty)
                        love.graphics.rectangle('fill',sx+TILE*.5,sy+TILE*.5,2,2)
                    end
                end
            end
            if role=='bodyguard_brute' and EnemySquads.ward_source(state,enemy) then
                love.graphics.setColor(.57,.83,1,.8)
                love.graphics.circle('line',x+TILE/2,y+TILE/2,TILE*.46)
                badge('ward',x,y)
            elseif role=='ward_weaver' then badge('ward',x,y)
            elseif role=='mend_acolyte' then badge('heal',x,y)
            elseif role=='banner_captain' or EnemySquads.rally_source(state,enemy) then badge('rally',x,y)
            elseif role=='shield_bearer' then
                local m=enemy.ai_memory
                local fx,fy=m.shield_dx or 0,m.shield_dy or 1
                if not m.shield_lowered then
                    love.graphics.setColor(.65,.85,1,.95)
                    love.graphics.line(x+TILE*(.5+fx*.43-fy*.28),y+TILE*(.5+fy*.43+fx*.28),
                        x+TILE*(.5+fx*.43+fy*.28),y+TILE*(.5+fy*.43-fx*.28))
                end
                badge('shield',x,y)
            elseif role=='pin_archer' then
                draw_visual(bound_visual('core:ui/weapon_bow_icon'),x+TILE*.7,y,TILE*.3,TILE*.3)
            end
        end
    end
    for _,trap in ipairs(state.enemy_snares or {}) do
        if trap.floor_id==state.active_floor_id and visible_at(trap.x,trap.y) then
            local x,y=Camera.tile_top_left(camera,trap.x,trap.y)
            if not draw_visual(bound_visual('core:ui/squad_snare_effect'),
                x+TILE*.2,y+TILE*.2,TILE*.6,TILE*.6,.9) then
                love.graphics.setColor(1,.7,.35)
                love.graphics.line(x+8,y+8,x+TILE-8,y+TILE-8,x+TILE-8,y+8,x+8,y+TILE-8)
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
    local function review_sprite(binding,x,y,alpha)
        return draw_visual(bound_visual('core:object/review_'..binding),x,y,TILE,TILE,alpha)
    end
    for _,feature in ipairs(state.review_features or {}) do
        if feature.floor_id==state.active_floor_id and
            visible_at(feature.x,feature.y) then
            local x,y=Camera.tile_top_left(camera,feature.x,feature.y)
            local kind=feature.kind
            local sprite
            if kind=='key' and not feature.collected then sprite='key'
            elseif kind=='ward' and not feature.collected then sprite='ward'
            elseif kind=='ore' then sprite=feature.broken and 'ore_depleted' or 'ore'
            elseif kind=='brazier' then
                sprite=(feature.lit_until or 0)>state.clock and 'brazier_lit' or 'brazier_unlit'
            elseif kind=='plate' then
                local gate=Game.entity(state,feature.gate_id)
                local tiny=gate and gate.art_variant=='tiny' and
                    visual_assets and visual_assets.generated and
                    visual_assets.generated.style=='kenney'
                sprite=(tiny and 'plate_tiny_' or 'plate_')..(gate and gate.open and 'down' or 'up')
            elseif kind=='mushroom' or kind=='overgrowth' then
                sprite=kind..'_'..(feature.variant or 1)
            elseif kind=='crystal' then
                sprite='ore'
            elseif kind=='quest_clue' and not feature.discovered then
                sprite='secret_clue'
            elseif kind=='quest_key' and not feature.collected then
                sprite='key'
            elseif kind=='quest_plate' then
                sprite=feature.activated and 'plate_down' or 'plate_up'
            elseif kind=='quest_brazier' then
                sprite=feature.lit and 'brazier_lit' or 'brazier_unlit'
            end
            if sprite and not review_sprite(sprite,x,y) then
                if kind=='key' or kind=='quest_key' then
                    love.graphics.setColor(.97,.84,.43)
                    love.graphics.circle('line',x+14,y+17,6)
                    love.graphics.line(x+20,y+17,x+31,y+17,x+31,y+23)
                elseif kind=='ore' then
                    love.graphics.setColor(feature.broken and .35 or .74,
                        feature.broken and .34 or .69,.42)
                    love.graphics.polygon('fill',x+8,y+30,x+13,y+12,
                        x+25,y+8,x+33,y+27)
                elseif kind=='brazier' or kind=='quest_brazier' then
                    love.graphics.setColor(.79,.57,.32)
                    love.graphics.rectangle('line',x+10,y+20,20,13)
                    if feature.lit or (feature.lit_until or 0)>state.clock then
                        love.graphics.setColor(1,.67,.28)
                        love.graphics.circle('fill',x+20,y+17,7)
                    end
                elseif kind=='ward' then
                    love.graphics.setColor(.55,.78,.99)
                    love.graphics.polygon('line',x+20,y+6,x+31,y+13,
                        x+27,y+29,x+20,y+34,x+13,y+29,x+9,y+13)
                elseif kind=='plate' or kind=='quest_plate' then
                    love.graphics.setColor(.63,.68,.64)
                    love.graphics.rectangle('line',x+7,y+13,26,15)
                else
                    love.graphics.setColor(kind=='overgrowth' and .33 or .78,
                        kind=='overgrowth' and .67 or .62,.37)
                    love.graphics.circle('fill',x+20,y+19,10)
                    love.graphics.rectangle('fill',x+18,y+19,4,13)
                end
            end
            if kind=='quest_reward' and not feature.collected then
                local chest=bound_visual('core:chest/standard')
                if not draw_visual(chest,x,y,TILE,TILE,
                    feature.unlocked and 1 or .57) then
                    love.graphics.setColor(feature.unlocked and .95 or .48,
                        feature.unlocked and .77 or .48,.35)
                    love.graphics.rectangle('line',x+7,y+10,TILE-14,TILE-18)
                end
            end
            if kind=='mine_rail' then
                if bound_visual('core:object/review_rail_top_left') then
                    local rows={'top','middle','bottom'}
                    for row_index,row in ipairs(rows) do
                        for col_index,col in ipairs({'left','right'}) do
                            draw_visual(bound_visual('core:object/review_rail_'..row..'_'..col),
                                x+(col_index-1)*TILE/2,y+(row_index-1)*TILE/3,
                                TILE/2,TILE/3)
                        end
                    end
                else
                    love.graphics.setColor(.55,.50,.39,.7)
                    love.graphics.line(x+6,y+6,x+6,y+34)
                    love.graphics.line(x+30,y+6,x+30,y+34)
                    love.graphics.line(x+6,y+13,x+30,y+13)
                    love.graphics.line(x+6,y+28,x+30,y+28)
                end
            elseif kind=='brazier' and (feature.lit_until or 0)>state.clock then
                love.graphics.setColor(.98,.78,.30,.45)
                love.graphics.circle('line',x+TILE/2,y+TILE/2,TILE*.44)
            end
        end
    end
    for _, drop in ipairs(state.drops) do
        if not drop.collected and not drop.carried_by and drop.position.floor_id==state.active_floor_id
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
            and visible_at(entity.position.x,entity.position.y)
            and (entity.controller=='review_vault' or
                entity.controller=='review_gate' or
                entity.controller=='review_pillar') then
            local x,y=Camera.tile_top_left(camera,entity.position.x,entity.position.y)
            local onebit=visual_assets and visual_assets.generated and
                visual_assets.generated.style=='onebit'
            local kind=entity.controller
            if kind=='review_pillar' then
                local drawn=review_sprite('pillar_bottom',x,y)
                review_sprite('pillar_middle',x,y-TILE)
                review_sprite('pillar_top',x,y-2*TILE)
                if not drawn then
                    love.graphics.setColor(.62,.59,.51)
                    love.graphics.rectangle('fill',x+10,y+3,TILE-20,TILE-6)
                    love.graphics.setColor(.84,.79,.62)
                    love.graphics.rectangle('line',x+6,y+2,TILE-12,TILE-4)
                end
            elseif kind=='review_vault' and not entity.opened then
                local drawn=review_sprite('vault_bottom',x,y)
                if not onebit then review_sprite('vault_top',x,y-TILE) end
                if not drawn then
                    love.graphics.setColor(.75,.68,.46)
                    love.graphics.rectangle('line',x+5,y+2,TILE-10,TILE-4)
                end
            elseif kind=='review_gate' then
                local tiny=entity.art_variant=='tiny' and not onebit
                local drawn
                if not entity.open then
                    if entity.art_variant=='hidden' then
                        drawn=review_sprite('secret_clue',x,y)
                    elseif tiny then drawn=review_sprite('gate_tiny',x,y)
                    else
                        drawn=review_sprite('gate_bottom',x,y)
                        if not onebit then review_sprite('gate_top',x,y-TILE) end
                    end
                end
                if not drawn and not entity.open then
                    love.graphics.setColor(.71,.73,.67)
                    love.graphics.rectangle('line',x+5,y+2,TILE-10,TILE-4)
                end
                if entity.open then
                    love.graphics.setColor(.35,.95,.71,.84)
                    love.graphics.rectangle('line',x+4,y+4,TILE-8,TILE-8)
                    if entity.closes_at then
                        local remaining=math.max(0,math.ceil((entity.closes_at-state.clock)/100))
                        love.graphics.print(tostring(remaining),x+TILE-13,y+2)
                    end
                end
            end
        end
        if entity.position.floor_id==state.active_floor_id
            and entity.controller == "chest" and
            visible_at(entity.position.x,entity.position.y) then
            local x, y = Camera.tile_top_left(camera, entity.position.x, entity.position.y)
            local chest_art=bound_visual(entity.claimed and 'core:object/chest_open' or
                entity.definition_id)
            if chest_art then
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
            if not draw_visual(bound_visual(entity.open and 'core:object/door_open' or 'core:object/door_closed'),x,y,TILE,TILE) then
            begin_tile_token(x,y)
            local x,y=0,0
            love.graphics.setColor(entity.open and 0.42 or 0.57,
                entity.open and 0.47 or 0.35,0.22)
            love.graphics.rectangle("fill",x+6,y+3,20,27)
            love.graphics.setColor(0.79,0.69,0.39)
            love.graphics.rectangle("line",x+6,y+3,20,27)
            love.graphics.pop()
            end
        end
        if entity.position.floor_id==state.active_floor_id
            and entity.controller=="exit" and
            visible_at(entity.position.x,entity.position.y) then
            local x,y=Camera.tile_top_left(camera,entity.position.x,entity.position.y)
            if not draw_visual(bound_visual('core:object/exit'),x,y,TILE,TILE) then
            begin_tile_token(x,y)
            local x,y=0,0
            love.graphics.setColor(0.34,0.88,0.94)
            love.graphics.circle("line",x+16,y+16,12)
            love.graphics.polygon("fill",x+16,y+6,x+23,y+17,x+16,y+25,x+9,y+17)
            love.graphics.pop()
            end
        end
    end
    for _,special in ipairs({'decoy','snare'}) do
        local marker=state.class_special and state.class_special[special]
        if marker and marker.floor_id==state.active_floor_id
            and visible_at(marker.x,marker.y) then
            local x,y=Camera.tile_top_left(camera,marker.x,marker.y)
            love.graphics.setColor(special=='decoy' and .57 or .98,
                special=='decoy' and .67 or .54,.95,.9)
            if special=='decoy' then
                love.graphics.circle('line',x+TILE/2,y+TILE/2,TILE*.33)
                love.graphics.circle('line',x+TILE/2,y+TILE/2,TILE*.17)
            else
                love.graphics.line(x+7,y+7,x+TILE-7,y+TILE-7)
                love.graphics.line(x+TILE-7,y+7,x+7,y+TILE-7)
            end
        end
    end
    for _,stair in ipairs(state.generated_map and
        state.generated_map.placements.stairs or {}) do
        if visible_at(stair.x,stair.y) then
            local x,y=Camera.tile_top_left(camera,stair.x,stair.y)
            if not draw_visual(bound_visual(stair.id=='stair:up' and 'core:object/stairs_up' or 'core:object/stairs_down'),x,y,TILE,TILE) then
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
end

local function draw_history_event(event)
    if not event then return end
    if event.kind=="Moved" or event.kind=="Displaced" then return end
    if event.kind == "WindupStarted" then
        if event.support_kind then draw_cells(event.target_cells,.45,.88,.9,.7,true)
        else draw_cells(event.target_cells, 1, 0.64, 0.18, 0.5, true) end
        if event.shove then
            local s=event.shove
            draw_cell({floor_id=s.floor_id,x=s.x+s.dx,y=s.y+s.dy},1,.8,.3,.5,true)
        end
    elseif event.kind == "AttackPerformed" then
        draw_cells(event.target_cells, 1, 0.22, 0.20, 0.7, true)
        local cells=event.target_cells or {}
        if #cells>0 then
            local cell=cells[math.ceil(#cells/2)]
            if visible_at(cell.x,cell.y) then
                local x,y=Camera.tile_top_left(camera,cell.x,cell.y)
                local attacker=Game.entity(state,event.source_id)
                local center=bound_visual('core:ui/attack_center_effect')
                if center then
                    draw_visual(center,x+TILE*.29,y+TILE*.29,TILE*.42,TILE*.42,.93)
                else
                    love.graphics.setColor(1,.89,.52,.9)
                    love.graphics.line(x+TILE*.38,y+TILE*.5,x+TILE*.62,y+TILE*.5)
                    love.graphics.line(x+TILE*.5,y+TILE*.38,x+TILE*.5,y+TILE*.62)
                end
                if attacker and attacker.controller=='player' then
                    local id=attacker.definition_id
                    local weapon=id=='core:actor/ranger' and 'bow' or
                        (id=='core:actor/arcanist' or id=='core:actor/alchemist') and 'spell' or
                        (id=='core:actor/hookrunner' and
                            event.ability_id=='core:ability/hookrunner_hook') and 'hook' or
                        (id=='core:actor/shade' or id=='core:actor/trapper' or
                            id=='core:actor/hookrunner') and 'dagger' or 'sword'
                    draw_visual(bound_visual('core:ui/weapon_'..weapon..'_icon'),
                        x+TILE*.64,y+TILE*.10,TILE*.27,TILE*.27,.95)
                end
            end
        end
    elseif event.kind == "WindupCancelled" then
        draw_cells(event.target_cells, 0.6, 0.6, 0.6, 0.4, true)
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
    if event.kind=='DamageTaken' and event.x and event.y
        and visible_at(event.x,event.y) then
        local x,y=Camera.tile_top_left(camera,event.x,event.y)
        draw_visual(bound_visual('core:ui/attack_impact_effect'),
            x+TILE*.19,y+TILE*.19,TILE*.62,TILE*.62,.85)
    end
    if event.kind~='WindupStarted' and event.kind~='AttackPerformed'
        and event.kind~='WindupCancelled' and event.from_x and event.from_y
        and visible_at(event.from_x,event.from_y) then
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
        local r,g,b,a
        if targeting.kind == "dash" then
            if cell.status == "open" then
                r,g,b,a=0.22,0.85,0.55,0.55
            elseif cell.status == "unreachable" then
                r,g,b,a=0.55,0.55,0.55,0.45
            else
                r,g,b,a=1.0,0.27,0.28,0.65
            end
        elseif targeting.kind == "sweeping_slash" then
            r,g,b,a=1.0,0.56,0.17,0.65
        else
            r,g,b,a=1.0,0.86,0.30,0.65
        end
        draw_cell(cell,r,g,b,a,true)
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
    local terrain_mode=visual_assets and visual_assets.generated and
        visual_assets.generated.terrain_mode
    local background=bound_visual('core:background/'..biome_id:match('/([^/]+)$'))
    if background and VisualSlots.background_alpha(background.source,terrain_mode)>0 then
        draw_visual(background,VIEWPORT.x,VIEWPORT.y,VIEWPORT.width,VIEWPORT.height,
            VisualSlots.background_alpha(background.source,terrain_mode))
    elseif terrain_mode=='tiled' then
        background=nil
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
            local kenney=visual_assets and visual_assets.generated and
                (visual_assets.generated.style=='kenney' or visual_assets.generated.style=='onebit')
            local kind=tile=='#' and 'wall' or tile=='~' and 'water' or
                tile=='.' and 'floor' or nil
            if terrain_mode=='tiled' then
                if tile=='B'then kind=kenney and 'floor' or 'wall'
                elseif tile=='b' or tile=='g' or tile=='i' or tile=='m' or tile=='c'then
                    kind='floor'
                end
            end
            love.graphics.setColor(r,g,b,background and not wall and 0.78 or 1)
            -- Complete ground underlay; native terrain fills every cell.
            local edge=kind and visual_assets and visual_assets.generated and 0 or 2
            love.graphics.rectangle("fill", screen_x, screen_y, TILE-edge,TILE-edge)
            if kind and visual_assets and visual_assets.has_terrain then
                local shape=VisualSlots.terrain_shape(map,x,y,kind)
                local cache_key=biome_id..':'..kind..':'..shape
                local handle=terrain_cache[cache_key]
                if kenney then
                    handle=visual_assets.generated:terrain(map,x,y,kind,function(xx,yy)
                        return explored_at(map.floor_id,xx,yy)
                    end)
                elseif handle==nil then
                    for _,binding in ipairs(VisualSlots.terrain_bindings(
                        biome_id,kind,shape)) do
                        handle=visual_assets:resolve(binding)
                        if handle then break end
                    end
                    terrain_cache[cache_key]=handle or false
                end
                if handle and VisualSlots.terrain_draws_at(kind,handle.source,x,y,terrain_mode) then
                    draw_visual(handle,screen_x,screen_y,TILE,TILE,
                        VisualSlots.terrain_alpha(kind,handle.source,remembered,terrain_mode))
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
            local mechanic=Terrain.get(tile)
            local mechanical_art=mechanic and tile~='.' and tile~='#' and tile~='~'
                and bound_visual(mechanic.id)
            if mechanical_art then
                if mechanical_art.ground then
                    local color=mechanical_art.ground
                    local dim=remembered and .38 or 1
                    -- Keep native ground texture visible beneath hazard markers.
                    local wash=visual_assets.generated and
                        visual_assets.generated.terrain_renderer and .42 or 1
                    love.graphics.setColor(color[1]*dim,color[2]*dim,color[3]*dim,wash)
                    love.graphics.rectangle('fill',screen_x,screen_y,TILE,TILE)
                end
                local inset=mechanical_art.tile_inset or 0
                draw_visual(mechanical_art,screen_x+inset,screen_y+inset,
                    TILE-2*inset,TILE-2*inset,remembered and .38 or 1)
            elseif tile=="b" or tile=="g" or tile=="i" or tile=="B"
                or tile=="m" or tile=="c" then
                love.graphics.setColor(0.88,0.91,0.78,remembered and 0.25 or 0.65)
                love.graphics.push()
                love.graphics.translate(screen_x,screen_y)
                love.graphics.scale(TILE/32)
                local screen_x,screen_y=0,0
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
                love.graphics.pop()
            end
            if not remembered and visual=="k" then
                draw_visual(bound_visual('core:object/review_secret_clue'),
                    screen_x+TILE*.13,screen_y+TILE*.13,TILE*.74,TILE*.74,.76)
                love.graphics.setColor(clue[1],clue[2],clue[3],0.82)
                love.graphics.setLineWidth(2)
                love.graphics.line(screen_x+8,screen_y+5,screen_x+16,screen_y+14,
                    screen_x+12,screen_y+21,screen_x+22,screen_y+31)
                love.graphics.line(screen_x+16,screen_y+14,screen_x+25,screen_y+10)
                love.graphics.setLineWidth(1)
            elseif not remembered and visual=="p" then
                love.graphics.setColor(clue[1],clue[2],clue[3],0.73)
                love.graphics.circle("line",screen_x+16,screen_y+16,6)
                love.graphics.circle("fill",screen_x+16,screen_y+16,2)
            elseif not remembered and terrain_mode~='tiled' and visual=="m" then
                love.graphics.setColor(0.28,0.47,0.23,0.65)
                love.graphics.circle("fill",screen_x+23,screen_y+9,3)
            elseif not remembered and terrain_mode~='tiled' and visual=="v" then
                love.graphics.setColor(0.33,0.52,0.25,0.75)
                love.graphics.line(screen_x+6,screen_y+3,screen_x+8,screen_y+12,
                    screen_x+14,screen_y+17)
            elseif not remembered and terrain_mode~='tiled' and visual=="r" then
                love.graphics.setColor(0.48,0.47,0.34,0.65)
                love.graphics.rectangle("fill",screen_x+6,screen_y+20,7,4)
                love.graphics.rectangle("fill",screen_x+17,screen_y+15,4,3)
            elseif not remembered and terrain_mode~='tiled' and visual=="w" and map.visual_rows then
                love.graphics.setColor(0.34,0.45,0.30,0.5)
                love.graphics.rectangle("fill",screen_x+3,screen_y+3,7,3)
            elseif not remembered and terrain_mode~='tiled' and visual=="f" then
                love.graphics.setColor(0.42,0.81,0.77,0.60)
                love.graphics.circle("fill",screen_x+8,screen_y+10,2)
                love.graphics.circle("fill",screen_x+23,screen_y+23,2)
            elseif not remembered and terrain_mode~='tiled' and visual=="b" then
                love.graphics.setColor(0.52,0.76,0.84,0.48)
                love.graphics.line(screen_x+4,screen_y+24,screen_x+26,screen_y+24)
            elseif not remembered and terrain_mode~='tiled' and visual=="q" then
                love.graphics.setColor(0.40,0.68,0.81,0.72)
                love.graphics.line(screen_x+3,screen_y+10,screen_x+12,screen_y+10)
                love.graphics.line(screen_x+17,screen_y+22,screen_x+28,screen_y+22)
            elseif not remembered and terrain_mode~='tiled' and visual=="t" then
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
            if strike.support then draw_cells(strike.target_cells,.45,.88,.9,.7,true)
            else draw_cells(strike.target_cells, 0.95, 0.26, 0.31, 0.38, true) end
            if strike.shove then
                local s=strike.shove
                draw_cell({floor_id=s.floor_id,x=s.x+s.dx,y=s.y+s.dy},1,.8,.3,.5,true)
            end
        end
    end
    draw_preview()
    draw_rewards()
    draw_enemies()
    draw_squad_cues()
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
        if avatar.source=='generated' then
            love.graphics.setColor(0,0,0,.6)
            love.graphics.ellipse('fill',screen_x+TILE*.5,screen_y+TILE*.88,
                TILE*.30,TILE*.09)
        end
        draw_visual(avatar,screen_x,screen_y,TILE,TILE,nil,player_facing==-1)
    else
        local colors={
            [Classes.order[1]]={0.95,0.78,0.30},
            [Classes.order[2]]={0.49,0.83,0.49},
            [Classes.order[3]]={0.67,0.62,0.96},
            [Classes.order[4]]={0.96,0.49,0.38},
            [Classes.order[5]]={0.55,0.76,0.93},
            [Classes.order[6]]={0.73,0.93,0.44},
            [Classes.order[7]]={0.61,0.56,0.89},
            [Classes.order[8]]={0.96,0.69,0.35},
            [Classes.order[9]]={0.68,0.86,0.41},
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
    if presentation_settings.combat_effects and visual_event and
        (not visual_event.floor_id or visual_event.floor_id==state.active_floor_id) then
        local seen,effects={},0
        for _,event in ipairs(current_group or {visual_event}) do
            if (event.kind=='DamageTaken' or event.kind=='BoonActivated' or event.kind=='DamageBlocked')
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
                        local id=event.kind=='DamageTaken' and 'core:ui/hit_effect' or
                            event.kind=='DamageBlocked' and 'core:ui/block_effect' or 'core:ui/boon_effect'
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
    local flash=presentation_settings.flash and HitFeedback.flash_alpha(hit_feedback) or 0
    if flash>0 then
        love.graphics.setColor(1,0.20,0.16,flash)
        love.graphics.rectangle("fill",VIEWPORT.x,VIEWPORT.y,
            VIEWPORT.width,VIEWPORT.height)
        love.graphics.setColor(1,0.35,0.28,math.min(0.4,flash*2))
        love.graphics.rectangle("line",VIEWPORT.x+1,VIEWPORT.y+1,
            VIEWPORT.width-2,VIEWPORT.height-2)
    end
    if presentation_settings.damage_numbers then draw_combat_numbers() end
    if spectator_enabled and spectator_free and spectator_focus then
        local cx,cy=Camera.tile_top_left(camera,spectator_focus.x,
            spectator_focus.y)
        love.graphics.setColor(0.38,0.94,1,0.9)
        love.graphics.rectangle('line',cx+1,cy+1,TILE-2,TILE-2)
    end
    draw_minimap()
    if chest_menu then
        PixelUI.dim(.55)
        menu_frame(chest_menu.name,80,120,472,360,ui_display_font)
        menu_frame('DETAILS',568,120,376,360,ui_display_font)
        for index,offer in ipairs(chest_menu.offers)do
            local definition=BoonContent.get(offer.boon_id)
            local y=178+(index-1)*76
            draw_visual(bound_visual(offer.boon_id..'/icon'),104,y,32,32)
            PixelUI.option(index..'. '..definition.name,146,y,376,
                index==chest_menu.selected,ui_menu_font)
            PixelUI.center(offer.rarity:upper(),y+24,ui_menu_font,PixelUI.colors.muted,146,376)
        end
        local offer=chest_menu.offers[chest_menu.selected]
        local art=bound_visual(offer.boon_id..'/detail') or bound_visual(offer.boon_id..'/icon')
        if art then draw_visual(art,724,152,64,64)end
        PixelUI.center(BoonContent.get(offer.boon_id).name,230,ui_menu_font,PixelUI.colors.selected,584,344)
        love.graphics.setFont(ui_menu_font);love.graphics.setColor(PixelUI.colors.text)
        love.graphics.printf(BoonContent.offer_text(offer.boon_id,offer.rarity),592,266,328)
        local power=BoonContent.get(offer.boon_id).parameters[offer.rarity]
        PixelUI.center(offer.rarity..' · power '..power.power,378,ui_menu_font,PixelUI.colors.muted,584,344)
        PixelUI.center(('Price %d · GOLD  %d'):format(chest_menu.price,state.currency),414,
            ui_menu_font,PixelUI.colors.text,102,420)
        PixelUI.option(chest_menu.shortfall>0 and ('Need '..chest_menu.shortfall..' more gold') or 'Enter claim',
            584,446,344,true,ui_menu_font,chest_menu.shortfall>0)
        PixelUI.center('1/2/3 or click choose · Enter claim · Esc cancel',516,ui_menu_font,PixelUI.colors.muted)
    end
    if route_menu then
        PixelUI.dim(.55)
        menu_frame(route_menu.stage_index>=3 and 'Enter the Final Sanctum' or 'Choose a route',
            160,138,704,350,ui_display_font)
        for i,route in ipairs(route_menu.routes)do
            local y=216+(i-1)*64
            PixelUI.option(i..'. '..route.name,180,y,664,i==route_menu.selected,ui_display_font)
            PixelUI.center(route.description,y+28,ui_menu_font,PixelUI.colors.muted,190,644)
        end
        PixelUI.option('CONFIRM ROUTE',320,450,384,true,ui_menu_font)
        PixelUI.center('1/2/3 or click choose · Enter confirm · Esc cancel',520,ui_menu_font,PixelUI.colors.muted)
    end
    if state.game_over and not Playback.busy(playback) then
        PixelUI.dim(.7)
        menu_frame(state.victory and 'VICTORY' or 'GAME OVER',160,108,704,366,ui_display_font)
        local player=Game.player(state)
        if state.victory then
            local result=state.victory
            local names={'Overgrown Ruins'}
            -- The final third route is the gateway, not a biome ID.
            for index=1,math.min(2,#result.route_history)do
                local route=result.route_history[index]
                names[#names+1]=Biomes.get(route:gsub('^core:route/','core:biome/')).name
            end
            PixelUI.center('Hollow Regent defeated',160,ui_menu_font,PixelUI.colors.selected,184,656)
            PixelUI.center(table.concat(names,' > '),208,ui_menu_font,PixelUI.colors.text,184,656)
            local class=Classes.get(result.class_id or Classes.order[1])
            PixelUI.center(('%s · Level %d · XP %d'):format(class.name,result.level,result.xp_total),
                274,ui_menu_font,PixelUI.colors.text,184,656)
            PixelUI.center(('HP %d/%d · GOLD %d · Boons %d'):format(result.hp,result.max_hp,result.currency,
                #owned_boon_ids()),310,ui_menu_font,PixelUI.colors.text,184,656)
        else
            PixelUI.center('Your run has ended.',188,ui_menu_font,PixelUI.colors.text,184,656)
            PixelUI.center(('%s · Level %d · GOLD %d'):format(Classes.for_player(player).name,
                state.progression.level,state.currency),244,ui_menu_font,PixelUI.colors.text,184,656)
        end
        local earned={}
        for _,id in ipairs(AchievementContent.order)do
            if state.meta and state.meta.earned[id]then earned[#earned+1]=AchievementContent.get(id).name end
        end
        PixelUI.center('Unlocked this run: '..(#earned>0 and table.concat(earned,', ') or 'none'),
            356,ui_menu_font,PixelUI.colors.muted,184,656)
        PixelUI.center('N New Run · I inspect boons',430,ui_menu_font,PixelUI.colors.selected,184,656)
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

local function draw_panel()
    local g=love.graphics
    local player=Game.player(state)
    local class=Classes.for_player(player)
    local map=Content.map_for(state)
    local biome=state.finale and 'FINAL SANCTUM' or
        Biomes.get(map.biome_id or Biomes.order[1]).name:upper()
    if not (studio_theme and studio_theme:draw_panel(16,544,992,80))then
        g.setColor(.025,.025,.04);g.rectangle('fill',16,544,992,80)
        g.setColor(PixelUI.colors.edge);g.rectangle('fill',16,544,992,2)
    end
    g.setFont(ui_body_font)
    g.setColor(PixelUI.colors.text);g.print('HEALTH',28,553)
    local hp=('%d/%d'):format(player.hp,player.max_hp)
    if not (studio_theme and studio_theme:draw_icon('hp_icon',28,570,16))then
        draw_visual(bound_visual('core:ui/hp_icon'),28,570,16,16)
    end
    if not (studio_theme and studio_theme:draw_text('HP '..hp,50,571,1))then
        g.setColor(player.hp*4<=player.max_hp and {1,.4,.4} or PixelUI.colors.text)
        g.print(hp,50,571)
    end
    local low=player.hp*4<=player.max_hp
    hud_meter(125,571,135,12,player.hp,player.max_hp,low and .94 or .65,low and .31 or .8,low and .31 or .52)
    local level=state.progression.level
    local previous=level>1 and Rewards.next_threshold(level-1) or 0
    local earned=state.progression.xp_total-previous
    local needed=Game.next_level_xp(state)-previous
    if not (studio_theme and studio_theme:draw_icon('xp_icon',282,570,16))then
        draw_visual(bound_visual('core:ui/xp_icon'),282,570,16,16)
    end
    g.setColor(PixelUI.colors.text)
    g.print(('LV %d   XP %d/%d'):format(level,earned,needed),282,553)
    hud_meter(306,573,108,8,earned,needed,.55,.65,.92)
    if not (studio_theme and studio_theme:draw_icon('currency_icon',442,570,16))then
        draw_visual(bound_visual('core:ui/currency_icon'),442,570,16,16)
    end
    g.setColor(PixelUI.colors.selected);g.print('GOLD  '..state.currency,442,553)
    g.setColor(PixelUI.colors.text);g.print(biome,558,553)
    g.setColor(PixelUI.colors.muted)
    g.print(state.finale and 'FINAL BOSS' or ('STAGE %d / FLOOR %d'):format(
        state.stage and state.stage.index or 1,state.active_floor_index or 1),792,553)
    local warning,detail,color='', '',PixelUI.colors.muted
    local history=Playback.current(playback)
    if history then
        warning=history.kind=='WindupStarted' and 'ATTACK WINDUP' or 'RESOLVING'
        detail=playback_cue_labels[history.kind] or 'Input resumes shortly'
        color=PixelUI.colors.selected
        if history.kind=='BoonActivated' and history.boon_id then
            warning='BOON ACTIVATED';detail=BoonContent.get(history.boon_id).name
        elseif history.kind=='DamageTaken' then
            warning=history.target_id==state.player_id and 'DAMAGE TAKEN' or 'HIT CONFIRMED'
            if history.target_id==state.player_id then color={1,.44,.4}end
        end
    else
        for _,strike in ipairs(Game.pending_strikes(state))do
            local shown=false
            for _,cell in ipairs(strike.target_cells)do if visible_at(cell.x,cell.y)then shown=true;break end end
            if shown then
                warning=strike.support and 'SUPPORT CHANNEL' or 'INCOMING ATTACK'
                detail=Content.ability_name(strike.ability_id)..' · '..math.max(0,strike.due-state.clock)..' time'
                color={1,.44,.4};break
            end
        end
        if warning=='' and targeting then
            warning='AIMING'
            local preview=targeting.dx and Game.preview(state,targeting.kind,targeting.dx,targeting.dy)
            detail=preview and (preview.valid and 'Enter to strike · Esc cancel' or preview.reason)
                or 'Choose a direction · Esc cancel'
            color=PixelUI.colors.selected
        elseif warning=='' and state.stage and state.stage.phase=='extraction'then
            warning='REACH THE EXIT';detail=math.max(0,state.stage.deadline-state.clock)..' time left'
            color=PixelUI.colors.selected
        elseif warning=='' and state.stage and state.stage.phase=='pursuit'then
            warning='PURSUER ACTIVE';detail='Reach the exit';color={1,.44,.4}
        end
    end
    local item=reward_notice_time>0 and reward_notices[1] or nil
    local status=type(item)=='table' and item.text or item or notice
    if not item and (status=='Ready' or status:match('^.-: %+%d+ time$') or
        status:match('^Dash: %d+ tiles, %+%d+ time$'))then status=''end
    if warning==''then
        detail='';color=PixelUI.colors.muted
        if state.finale then
            local boss=Game.entity(state,state.finale.boss_id)
            detail='HOLLOW REGENT · Phase '..(boss and boss.phase_two and '2' or '1')..
                (detail~='' and ' · '..detail or '')
        end
    end
    g.setColor(color);g.printf((warning~=''and warning..'  ' or '')..detail,558,575,430)
    -- One nonblocking feedback line. Important action results must not be
    -- hidden behind an earlier stat-gain notification or an attack warning.
    local result_notice=notice
    if result_notice=='Ready' or result_notice:match('^.-: %+%d+ time$') or
        result_notice:match('^Dash: %d+ tiles, %+%d+ time$')then result_notice=''end
    local feedback=result_notice~='' and result_notice or status or ''
    if feedback~=''then
        g.setColor(PixelUI.colors.selected);g.setScissor(28,588,960,14)
        g.print(feedback,28,588);g.setScissor()
    end
    g.setColor(PixelUI.colors.text)
    g.print('F '..class.basic_name,28,605)
    g.print('R '..class.special_name,250,605)
    g.print('X '..class.dash_name,486,605)
    g.setColor(PixelUI.colors.selected)
    g.print('I Boons / Stats',688,605)
    g.setColor(PixelUI.colors.muted);g.print('F1 Help',838,605);g.print('Esc Menu',930,605)
end

local function draw_inventory()
    local g=love.graphics
    PixelUI.dim(.55)
    menu_frame('BOONS',72,100,520,440,ui_display_font)
    menu_frame('CHARACTER',608,100,344,440,ui_display_font)
    local player=Game.player(state)
    local class=Classes.for_player(player)
    local owned=owned_boon_ids()
    local pages=math.max(1,math.ceil(#owned/7))
    local page=boon_page%pages
    g.setFont(ui_menu_font)
    if #owned==0 then
        g.setColor(PixelUI.colors.muted);g.print('No boons yet',106,170)
        g.printf('Open chests to choose boons. Your collected boons and stacks appear here.',106,204,440)
    end
    for line=1,7 do
        local id=owned[page*7+line]
        if id then
            local y=150+(line-1)*44
            local icon=bound_visual(id..'/icon')
            if icon then draw_visual(icon,104,y,32,32)end
            local total=0
            for _,rarity in ipairs(BoonContent.rarities)do total=total+((player.boons[id] or {})[rarity] or 0)end
            PixelUI.option(BoonContent.get(id).name..' x'..total,140,y+5,420,
                inventory_selection==line,ui_menu_font)
        end
    end
    PixelUI.center(('PAGE %d/%d · V next · Enter inspect'):format(page+1,pages),492,
        ui_menu_font,PixelUI.colors.muted,88,488)
    local art=actor_visual(player.definition_id,state.player_id)
    if art then draw_visual(art,724,136,96,96)end
    PixelUI.center(class.name:upper(),248,ui_menu_font,PixelUI.colors.selected,624,312)
    local lines={('Level %d · XP %d'):format(state.progression.level,state.progression.xp_total),
        ('HEALTH %d/%d'):format(player.hp,player.max_hp),
        ('Attack %d · Defense %d'):format(player.damage,player.defense),
        ('Speed %d · GOLD %d'):format(player.speed,state.currency),
        ('Boons %d'):format(#owned)}
    g.setFont(ui_menu_font);g.setColor(PixelUI.colors.text)
    for i,line in ipairs(lines)do g.print(line,636,286+(i-1)*26)end
    PixelUI.center('I / Esc close · no world time',518,ui_menu_font,PixelUI.colors.muted,88,488)
end

local function draw_help()
    PixelUI.dim(.7)
    menu_frame('HOW TO PLAY',160,90,704,460,ui_display_font)
    local g=love.graphics
    local lines={
        {'WASD / arrows','Move · bump to attack or interact'},
        {'F / R / X','Use your class attacks and movement skill'},
        {'Enter / Esc','Commit aim / cancel aim'},
        {'Space','Wait one action'},
        {'I / V','Boons and character / next page'},
        {'Tab','Show / hide map'},
        {'Esc','Pause · Save & Quit · Settings'},
        {'F5','Refresh selected sprite pack'},
    }
    g.setFont(ui_menu_font)
    for i,row in ipairs(lines)do
        local y=140+(i-1)*34
        g.setColor(PixelUI.colors.selected);g.print(row[1],194,y)
        g.setColor(PixelUI.colors.text);g.print(row[2],372,y)
    end
    g.setColor(PixelUI.colors.muted)
    g.printf('Outlined tiles are locked attacks. A fast committed strike can land before your next turn. Menus cost no world time.',194,438,630)
    PixelUI.center('F1 / Esc / click to return',516,ui_menu_font,PixelUI.colors.selected,160,704)
end

local function draw_boon_detail()
    local definition=boon_detail_id and BoonContent.get(boon_detail_id)
    if not definition then return end
    PixelUI.dim(.45)
    menu_frame('BOON DETAILS',200,152,624,330,ui_display_font)
    local art=bound_visual(boon_detail_id..'/detail') or bound_visual(boon_detail_id..'/icon')
    if art then draw_visual(art,228,252,96,96)end
    local stacks=Game.player(state).boons[boon_detail_id] or {}
    local rarity='common';local counts={}
    for _,candidate in ipairs(BoonContent.rarities)do
        if (stacks[candidate] or 0)>0 then
            rarity=candidate;counts[#counts+1]=candidate..' x'..stacks[candidate]
        end
    end
    PixelUI.center(definition.name,184,ui_menu_font,PixelUI.colors.selected,224,576)
    PixelUI.center(table.concat(counts,' · '),218,ui_menu_font,PixelUI.colors.muted,224,576)
    love.graphics.setFont(ui_menu_font);love.graphics.setColor(PixelUI.colors.text)
    love.graphics.printf(rarity:upper()..' EFFECT\n\n'..BoonContent.offer_text(boon_detail_id,rarity),
        art and 350 or 228,258,art and 446 or 568)
    PixelUI.center('Enter / Esc / click to return',446,ui_menu_font,PixelUI.colors.selected,224,576)
end

local function settings_tab_at(x,y)
    if y<91 or y>=139 then return nil end
    for i=1,#settings_tabs do
        if x>=120+(i-1)*190 and x<300+(i-1)*190 then return i end
    end
end
local function settings_row_at(x,y)
    return PixelUI.row_at(145,202,734,54,settings_lengths[settings_page],70,x,y)
end
local function settings_click(x,y)
    local tab=settings_tab_at(x,y)
    if tab then settings_page=tab;settings_selection=1;return true end
    local row=settings_row_at(x,y)
    if row then
        settings_selection=row
        if settings_page==2 then
            if x>=795 then settings_change(1)
            elseif x>=705 and x<765 then settings_change(-1) end
        else settings_change(0) end
        return true
    end
    return false
end
local function draw_settings()
    local g=love.graphics
    menu_frame('SETTINGS',100,40,824,550,ui_display_font)
    for i,label in ipairs(settings_tabs) do
        local x=120+(i-1)*190
        g.setColor(i==settings_page and PixelUI.colors.edge or PixelUI.colors.panel)
        g.rectangle('fill',x,91,180,48)
        g.setFont(ui_menu_font)
        g.setColor(i==settings_page and PixelUI.colors.selected or PixelUI.colors.text)
        g.printf(label,x,103,180,'center')
    end
    local descriptions={
        'Choose the art used for actors, terrain, and icons.',
        'Set overall volume and balance music against game sounds.',
        'Adjust motion and hit feedback without changing gameplay.',
        'Change menu and HUD text appearance and contrast.'}
    PixelUI.center(descriptions[settings_page],160,ui_menu_font,
        PixelUI.colors.muted,130,764)
    for index=1,settings_lengths[settings_page] do
        local y=202+(index-1)*70
        local selected=settings_selection==index
        g.setColor(selected and PixelUI.colors.edge or PixelUI.colors.muted)
        g.rectangle('line',145,y,734,54)
        if selected then
            g.setColor(PixelUI.colors.background)
            g.rectangle('fill',147,y+2,730,50)
        end
        local label,value
        if settings_page==1 then
            local mode=VisualPacks.order[index]
            label=VisualPacks.labels[mode]
            value=mode==visual_pack_mode and 'IN USE' or 'SELECT'
        elseif settings_page==2 then
            local key=({'master','music','effects'})[index]
            label=({master='Master volume',music='Music & ambience',
                effects='Effects & UI'})[key]
            value=presentation_settings[key]..'%'
        elseif settings_page==3 then
            local key=({'shake','flash','combat_effects','damage_numbers'})[index]
            label=({shake='Screen shake',flash='Hit flash',
                combat_effects='Combat effect sprites',
                damage_numbers='Floating damage numbers'})[key]
            value=presentation_settings[key] and 'ON' or 'OFF'
        else
            label=({'Font','Text size','High contrast','Restore defaults'})[index]
            value=({presentation_settings.font=='pixel' and 'PIXEL' or 'SANS',
                presentation_settings.text_size=='large' and 'LARGE' or 'NORMAL',
                presentation_settings.high_contrast and 'ON' or 'OFF','RESET'})[index]
        end
        g.setFont(ui_menu_font)
        g.setColor(selected and PixelUI.colors.selected or PixelUI.colors.text)
        g.print(label,164,y+14)
        g.printf(value,695,y+14,164,'right')
        if settings_page==2 then
            g.print('−',719,y+14)
            g.print('+',805,y+14)
        end
    end
    local info=settings_message
    if settings_page==1 then
        info=visual_pack_message~='' and visual_pack_message or
            VisualPacks.descriptions[VisualPacks.order[settings_selection]]
        if visual_pack_message=='' and visual_pack_mode~='classic' and
            visual_pack_generated_status~='loaded' then
            info='Artwork unavailable · using built-in fallback'
        end
    end
    PixelUI.center(info,501,ui_menu_font,PixelUI.colors.muted,130,764)
    PixelUI.center('Tab / click category · ↑↓ select · ←→ adjust · Enter apply · Esc back',
        545,ui_body_font,PixelUI.colors.selected,130,764)
end

local function draw_title_page()
    local g=love.graphics
    if studio_theme then studio_theme:draw_background('title_background',0,0,g.getDimensions())end
    if title_view=='home'then
        g.setFont(ui_display_font);g.setColor(PixelUI.colors.selected)
        g.print('ROAG',(g.getWidth()-ui_display_font:getWidth('ROAG')*3)/2,104,0,3,3)
        local options={'CONTINUE','NEW GAME','ACHIEVEMENTS','SETTINGS','QUIT'}
        for i,label in ipairs(options)do
            PixelUI.option(label,280,232+(i-1)*42,464,title_selection==i,ui_display_font,
                i==1 and not resumable(save_status))
        end
        local descriptions={resumable(save_status) and 'Saved run available' or 'No compatible saved run',
            'Choose a character and begin','View your permanent unlocks','Audio, visuals, and accessibility','Leave ROAG'}
        PixelUI.center(descriptions[title_selection],476,ui_menu_font,PixelUI.colors.muted)
        if save_status and save_status.status~='absent' and not resumable(save_status)then
            local headline=TitleMenu.save_summary(save_status)
            PixelUI.center('Continue unavailable · '..headline,510,ui_menu_font,PixelUI.colors.muted)
        end
        PixelUI.center('Arrows / Enter · N quick start · C quick continue',552,ui_menu_font,PixelUI.colors.muted)
        PixelUI.center('F1  HOW TO PLAY',600,ui_menu_font,PixelUI.colors.selected)
    elseif title_view=='continue'then
        menu_frame('CONTINUE SAVED RUN',192,130,640,360,ui_display_font)
        local headline,detail=TitleMenu.save_summary(save_status)
        PixelUI.center(headline,220,ui_menu_font,PixelUI.colors.selected,224,576)
        PixelUI.center(detail,274,ui_menu_font,PixelUI.colors.text,224,576)
        PixelUI.option('CONTINUE RUN',320,424,384,true,ui_display_font,not resumable(save_status))
        PixelUI.center('Enter continues · Esc / click BACK',554,ui_menu_font,PixelUI.colors.muted)
    elseif title_view=='class'then
        menu_frame('CHOOSE YOUR CHARACTER',160,92,704,470,ui_display_font)
        for i,id in ipairs(Classes.order)do
            PixelUI.option(i..' '..Classes.get(id).name:upper(),190,150+(i-1)*36,290,
                id==selected_class_id,ui_menu_font)
        end
        local chosen=Classes.get(selected_class_id)
        local art=bound_visual(selected_class_id)
        if art then draw_visual(art,614,162,96,96)end
        PixelUI.center(chosen.name:upper(),290,ui_menu_font,PixelUI.colors.selected,496,336)
        PixelUI.center(chosen.description,332,ui_menu_font,PixelUI.colors.text,496,336)
        PixelUI.center(('HP %d · ATK %d · DEF %d · SPD %d'):format(chosen.hp,chosen.damage,chosen.defense,chosen.speed),
            406,ui_menu_font,PixelUI.colors.muted,496,336)
        PixelUI.option('BEGIN NEW GAME',496,502,336,false,ui_menu_font)
        PixelUI.center('1–9 / arrows choose · Enter begin · Esc back',598,ui_menu_font,PixelUI.colors.muted)
    elseif title_view=='settings'then draw_settings()end
    if title_view~='home'then
        g.setFont(ui_menu_font);g.setColor(PixelUI.colors.selected);g.print('< BACK',192,608)
    end
    if unlocks_view then
        PixelUI.dim(.65);menu_frame('ACHIEVEMENTS',96,100,832,480,ui_display_font)
        local earned=profile and profile.earned or {}
        local count=0
        for _,id in ipairs(AchievementContent.order) do
            if earned[id] then count=count+1 end
        end
        g.setFont(ui_body_font);g.setColor(PixelUI.colors.muted)
        g.printf(count..' / '..#AchievementContent.order..' earned',128,130,768,'right')
        for slot=1,5 do
            local i=achievement_page*5+slot
            local id=AchievementContent.order[i]
            if id then
            local def=AchievementContent.get(id)
            local y=165+(slot-1)*68
            g.setFont(ui_menu_font)
            g.setColor(earned[id] and PixelUI.colors.selected or PixelUI.colors.muted)
            local reward=def.reward and ' · '..BoonContent.get(def.reward).name or ''
            g.printf((earned[id] and 'EARNED' or 'LOCKED')..' — '..def.name..reward,
                128,y,768)
            g.setFont(ui_body_font);g.setColor(PixelUI.colors.text)
            g.printf(def.hint,148,y+29,730)
            end
        end
        if profile_status and (profile_status.status=='corrupt' or profile_status.status=='incompatible')then
            PixelUI.center('Profile '..profile_status.status..' · progress preserved',492,
                ui_menu_font,PixelUI.colors.selected)
        end
        local pages=math.max(1,math.ceil(#AchievementContent.order/5))
        g.setFont(ui_menu_font);g.setColor(PixelUI.colors.selected)
        g.print('< PREV',146,525)
        g.printf('PAGE '..(achievement_page+1)..' / '..pages,378,525,268,'center')
        g.print('NEXT >',778,525)
        PixelUI.center('Left / right browse · Esc return',590,
            ui_body_font,PixelUI.colors.muted)
    end
    if confirm_new then
        PixelUI.dim(.65);menu_frame('REPLACE SAVED RUN?',250,285,524,142,ui_display_font)
        PixelUI.center('Your current run will be replaced.',325,ui_menu_font,PixelUI.colors.text,250,524)
        PixelUI.option('REPLACE · Y / Enter',275,377,224,true,ui_menu_font)
        PixelUI.option('CANCEL · Esc',525,377,224,false,ui_menu_font)
    end
    if persistence_message~=''then
        g.setFont(ui_menu_font);g.setColor(1,.5,.45);g.printf(persistence_message,235,520,554,'center')
    end
end

function love.draw()
    if title then
        draw_title_page()
        if help_open then draw_help()end
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
                            focus.x,focus.y,visual_assets.generated and
                                visual_assets.generated.terrain_mode) and '' or ' accent elsewhere')
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
    if persistence_message~="" and not pause_menu and not confirm_reset_seed
        and not inventory_open and not help_open and not chest_menu and not route_menu then
        love.graphics.setColor(1,0.48,0.43)
        love.graphics.printf(persistence_message,VIEWPORT.x+12,
            VIEWPORT.y+12,VIEWPORT.width-24,"center")
    end
    if confirm_reset_seed then
        PixelUI.dim(.65)
        menu_frame('REPLACE RUN?',224,210,576,220,ui_display_font)
        love.graphics.setColor(0,0,0,0)
        love.graphics.rectangle("fill",160,200,704,230)
        love.graphics.setColor(1,0.75,0.48)
        love.graphics.printf(confirm_reset_seed==FRESH_RUN_SEED and
            'Replace run with a new seed?' or
            ("Replace run with seed %d?"):format(confirm_reset_seed),
            160,255,704,"center")
        love.graphics.setFont(ui_menu_font)
        love.graphics.printf("Enter / Y confirm · Esc cancel",160,325,704,"center")
    end
    if inventory_open then draw_inventory()end
    if boon_detail_id then draw_boon_detail()end
    if pause_menu then
        PixelUI.dim(.6)
        if pause_settings then draw_settings()
        else
            if studio_theme then
                studio_theme:draw_background('pause_background',0,0,love.graphics.getDimensions())
                PixelUI.dim(.35)
            end
            menu_frame('PAUSED',288,184,448,264,ui_display_font)
            for i,label in ipairs({'RESUME','SAVE & QUIT','SETTINGS'})do
                PixelUI.option(i..' '..label,306,252+(i-1)*44,412,pause_selection==i,ui_display_font)
            end
            PixelUI.center('Up/down · Enter · Esc resume',400,ui_body_font,PixelUI.colors.muted,288,448)
            if persistence_message~=''then
                love.graphics.setColor(1,.5,.45);love.graphics.printf(persistence_message,304,470,416,'center')
            end
        end
    end
    if help_open then draw_help()end

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
    local onebit=visual_assets and visual_assets.generated and
        visual_assets.generated.style=='onebit'
    local previous_shader
    if onebit then
        onebit_grayscale_shader=onebit_grayscale_shader or
            love.graphics.newShader(onebit_grayscale_source)
        previous_shader=love.graphics.getShader()
        love.graphics.setShader(onebit_grayscale_shader)
    end
    if not diagnostics then
        ordinary_draw()
        if onebit then love.graphics.setShader(previous_shader) end
        return
    end
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
    if onebit then love.graphics.setShader(previous_shader) end
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
function love.roagUIState()
    return {page=help_open and 'help' or confirm_reset_seed and 'confirm' or boon_detail_id and 'boon-detail' or
        inventory_open and 'inventory' or title and (unlocks_view and 'achievements' or
        confirm_new and 'confirm' or title_view) or pause_menu and
        (pause_settings and 'settings' or 'pause') or chest_menu and 'chest' or
        route_menu and 'route' or 'game',class=selected_class_id,
        selected=title and title_selection or pause_menu and pause_selection or inventory_selection}
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
function love.roagVisualPackMode() return visual_pack_mode end
function love.roagThemeDrawCounts() return studio_theme and studio_theme.counts or {} end
function love.roagAssetResolved(binding)
    return visual_assets and visual_assets:resolve(binding)~=nil or false
end
function love.roagAssetIdResolved(id)
    return visual_assets and visual_assets:resolve_id(id)~=nil or false
end
function love.mousepressed(x,y,button,istouch,presses)
    if button~=1 then return end
    if help_open then help_open=false;return end
    if boon_detail_id then boon_detail_id=nil;return end
    if inventory_open then
        local owned=owned_boon_ids();local pages=math.max(1,math.ceil(#owned/7))
        local row=PixelUI.row_at(88,150,488,38,7,44,x,y)
        if row and owned[(boon_page%pages)*7+row]then
            inventory_selection=row
            if (presses or 1)>=2 then love.keypressed('return')end
        elseif y>=550 then inventory_open=false end
        return
    end
    if title then
        if unlocks_view then
            local pages=math.max(1,math.ceil(#AchievementContent.order/5))
            if y>=515 and y<=555 and x>=126 and x<=330 then
                achievement_page=(achievement_page+pages-1)%pages
            elseif y>=515 and y<=555 and x>=740 and x<=900 then
                achievement_page=(achievement_page+1)%pages
            elseif x<96 or x>928 or y<100 or y>580 then
                unlocks_view=false
            end
            return
        end
        if confirm_new then
            if y>=362 and y<=408 then
                if x>=275 and x<=500 then begin_new()
                elseif x>=525 and x<=750 then confirm_new=false end
            end
            return
        end
        if title_view~='home' and x>=180 and x<=330 and y>=596 and y<=632 then
            local was_settings=title_view=='settings'
            title_view='home';title_selection=was_settings and 4 or TitleMenu.default_selection(save_status)
            return
        end
        if title_view=='home'then
            if y>=590 and y<=630 then help_open=true;return end
            local index=PixelUI.home_at(x,y)
            if index then title_selection=index;activate_title_selection()end
        elseif title_view=='continue'then
            if x>=320 and x<=704 and y>=416 and y<=466 then activate_title_selection()end
        elseif title_view=='class'then
            local index=PixelUI.class_at(x,y)
            if index then selected_class_id=Classes.order[index]
            elseif x>=496 and x<=832 and y>=490 and y<=536 then request_new_run()end
        elseif title_view=='settings'then
            settings_click(x,y)
        end
        return
    end
    if pause_menu then
        if pause_settings then
            settings_click(x,y)
        else
            local index=PixelUI.pause_at(x,y)
            if index then pause_selection=index;love.keypressed(tostring(index))end
        end
        return
    end
    if chest_menu then
        local index=PixelUI.chest_at(x,y)
        if index then chest_menu.selected=index
        elseif x>=568 and x<=944 and y>=438 and y<=480 then love.keypressed('return')
        elseif y>=500 then love.keypressed('escape')end
        return
    end
    if route_menu then
        local index=PixelUI.route_at(#route_menu.routes,x,y)
        if index then route_menu.selected=index
        elseif x>=320 and x<=704 and y>=440 and y<=486 then love.keypressed('return')
        elseif y>=500 then love.keypressed('escape')end
        return
    end
    if not state or title or targeting then return end
    if y>=596 and y<=626 then
        if x>=680 and x<832 then love.keypressed('i')
        elseif x>=832 and x<926 then love.keypressed('f1')
        elseif x>=926 then love.keypressed('escape')end
    end
end
function love.wheelmoved(dx,dy)
    if help_open or boon_detail_id then return end
    if title and unlocks_view and dy~=0 then
        title_keypressed(dy>0 and 'left' or 'right')
        return
    end
    if inventory_open then
        if dy~=0 then love.keypressed(dy>0 and 'left' or 'right')end
        return
    end
    if dy~=0 and (title and title_view=='settings' or pause_settings) then
        settings_keypressed(dy>0 and 'up' or 'down',title)
    end
end
