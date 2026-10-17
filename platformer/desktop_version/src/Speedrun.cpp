#include "Speedrun.h"
#include "SpeedrunCore.h"
#include "Enums.h"
#include "Exit.h"
#include "FileSystemUtils.h"
#include "Font.h"
#include "Game.h"
#include "Graphics.h"
#include "KeyPoll.h"
#include "Music.h"
#include "Screen.h"
#include "tinyxml2.h"
#include <cmath>
#include <cstdio>
#include <vector>

namespace speedrun
{
static sprint::Race race;
static sprint::Input controls;
static int best = 0, best_splits[sprint::ROOM_COUNT] = {0};
static int previous_best = 0;
static std::vector<sprint::Sample> ghost;
static bool paused = false, show_ghost = true, new_best = false, save_failed = false;
static bool held_flip = false, held_restart = false, held_pause = false;
static bool held_practice = false, held_ghost = false, held_prev = false, held_next = false;

static bool pressed(bool down, bool& held)
{
    const bool edge = down && !held;
    held = down;
    return edge;
}

static void load_record()
{
    tinyxml2::XMLDocument doc;
    if (!FILESYSTEM_loadTiXml2Document("saves/sprint-v1.vvv", doc)) return;
    const tinyxml2::XMLElement* root = doc.FirstChildElement("sprint");
    int version = 0, ticks = 0;
    if (!root || root->QueryIntAttribute("course", &version) != tinyxml2::XML_SUCCESS
        || version != sprint::COURSE_VERSION
        || root->QueryIntAttribute("ticks", &ticks) != tinyxml2::XML_SUCCESS
        || ticks < sprint::ROOM_COUNT || ticks > 2500000) return;
    int splits[sprint::ROOM_COUNT];
    const tinyxml2::XMLElement* split = root->FirstChildElement("split");
    for (int i = 0; i < sprint::ROOM_COUNT; ++i)
    {
        if (!split || split->QueryIntAttribute("ticks", &splits[i]) != tinyxml2::XML_SUCCESS
            || splits[i] <= (i ? splits[i - 1] : 0) || splits[i] > ticks) return;
        split = split->NextSiblingElement("split");
    }
    if (splits[sprint::ROOM_COUNT - 1] != ticks) return;
    best = ticks;
    for (int i = 0; i < sprint::ROOM_COUNT; ++i) best_splits[i] = splits[i];
    std::vector<sprint::Sample> samples;
    for (const tinyxml2::XMLElement* p = root->FirstChildElement("frame"); p; p = p->NextSiblingElement("frame"))
    {
        sprint::Sample s;
        if (samples.size() >= static_cast<size_t>(ticks) || samples.size() >= sprint::MAX_GHOST_TICKS
            || p->QueryIntAttribute("room", &s.room) != tinyxml2::XML_SUCCESS
            || p->QueryFloatAttribute("x", &s.x) != tinyxml2::XML_SUCCESS
            || p->QueryFloatAttribute("y", &s.y) != tinyxml2::XML_SUCCESS
            || s.room < 0 || s.room >= sprint::ROOM_COUNT
            || !(s.x >= 0 && s.x <= 312) || !(s.y >= 64 && s.y <= 180))
        { samples.clear(); break; }
        s.inverted = p->BoolAttribute("inverted");
        s.left = p->BoolAttribute("left");
        samples.push_back(s);
    }
    if (samples.size() == static_cast<size_t>(ticks)) ghost.swap(samples);
}

static bool save_record()
{
    tinyxml2::XMLDocument doc;
    tinyxml2::XMLElement* root = doc.NewElement("sprint");
    doc.InsertEndChild(root);
    root->SetAttribute("course", sprint::COURSE_VERSION);
    root->SetAttribute("ticks", best);
    for (int i = 0; i < sprint::ROOM_COUNT; ++i)
    {
        tinyxml2::XMLElement* p = doc.NewElement("split");
        p->SetAttribute("ticks", best_splits[i]);
        root->InsertEndChild(p);
    }
    for (size_t i = 0; i < ghost.size(); ++i)
    {
        const sprint::Sample& s = ghost[i];
        tinyxml2::XMLElement* p = doc.NewElement("frame");
        p->SetAttribute("room", s.room); p->SetAttribute("x", s.x); p->SetAttribute("y", s.y);
        p->SetAttribute("inverted", s.inverted); p->SetAttribute("left", s.left);
        root->InsertEndChild(p);
    }
    if (!FILESYSTEM_saveTiXml2Document("saves/sprint-v1.tmp", doc)) return false;
    const std::string directory = FILESYSTEM_getUserSaveDirectory();
    return SDL_RenamePath((directory + "sprint-v1.tmp").c_str(), (directory + "sprint-v1.vvv").c_str());
}

static void restart(bool practice, int room = 0)
{
    race.reset(practice, room);
    paused = new_best = save_failed = false;
    previous_best = best;
    controls = sprint::Input();
}

void init()
{
    load_record();
    restart(false);
    game.gamestate = SPRINTMODE;
    graphics.fademode = FADE_NONE;
    graphics.flipmode = false;
    music.play(Music_POSITIVEFORCE);
    SDL_SetWindowTitle(gameScreen.m_window, "FLIP / SPRINT - a VVVVVV speedrun mod");
}

void focus_lost()
{
    if (race.started && !race.finished) race.valid = false;
}

void input()
{
    const bool flip = pressed(key.isDown(SDLK_SPACE) || key.isDown(SDLK_Z)
        || key.isDown(SDLK_UP) || key.isDown(game.controllerButton_flip), held_flip);
    const bool reset = pressed(key.isDown(SDLK_R) || key.isDown(game.controllerButton_restart)
        || (race.finished && key.isDown(SDLK_RETURN)), held_restart);
    const bool pause = pressed(key.isDown(SDLK_ESCAPE) || key.isDown(game.controllerButton_esc), held_pause);
    const bool practice = pressed(key.isDown(SDLK_P), held_practice);
    const bool toggle_ghost = pressed(key.isDown(SDLK_G), held_ghost);
    const bool prev = pressed(key.isDown(SDLK_LEFTBRACKET), held_prev);
    const bool next = pressed(key.isDown(SDLK_RIGHTBRACKET), held_next);
    if (reset) restart(race.practice, race.practice ? race.room : 0);
    if (practice) restart(!race.practice, race.practice ? 0 : race.room);
    if (race.practice && (prev || next))
        restart(true, (race.room + sprint::ROOM_COUNT + (next ? 1 : -1)) % sprint::ROOM_COUNT);
    if (toggle_ghost) show_ghost = !show_ghost;
    if (pause)
    {
        paused = !paused;
        if (race.started && !race.finished) race.valid = false;
    }
    if (paused && key.isDown(SDLK_Q)) VVV_exit(0);
    const bool left = key.isDown(SDLK_LEFT) || key.isDown(SDLK_A) || key.controllerWantsLeft(false);
    const bool right = key.isDown(SDLK_RIGHT) || key.isDown(SDLK_D) || key.controllerWantsRight(false);
    controls = sprint::Input(static_cast<int>(right) - static_cast<int>(left), flip);
}

void logic()
{
    if (paused || race.finished) return;
    const int old_flips = race.flips, old_deaths = race.deaths, old_room = race.room;
    race.step(controls);
    controls.flip = false;
    if (race.flips != old_flips) music.playef(race.inverted ? Sound_FLIP : Sound_UNFLIP);
    if (race.deaths != old_deaths) music.playef(Sound_CRY);
    if (race.room != old_room) music.playef(Sound_CHECKPOINT);
    if (race.finished) music.playef(Sound_NEWRECORD);
    if (race.eligible() && (!best || race.ticks < best))
    {
        new_best = true;
        best = race.ticks;
        for (int i = 0; i < sprint::ROOM_COUNT; ++i) best_splits[i] = race.splits[i];
        ghost = race.recording;
        if (ghost.size() != static_cast<size_t>(race.ticks)) ghost.clear();
        save_failed = !save_record();
    }
}

static void text(int x, int y, const std::string& value, int r = 216, int g = 225, int b = 239, uint32_t flags = 0)
{
    font::print(PR_FONT_8X8 | flags, x, y, value, r, g, b);
}
static void panel(int x, int y, int w, int h)
{
    graphics.fill_rect(x - 1, y - 1, w + 2, h + 2, 64, 91, 113);
    graphics.fill_rect(x, y, w, h, 12, 20, 33);
}
static void runner(const sprint::Sample& s, bool is_ghost)
{
    int frame = (s.left ? 3 : 0) + (s.inverted ? 6 : 0);
    if (is_ghost || std::fabs(race.vx) > 0.1f) frame += 1 + ((race.ticks / 4) % 2);
    SDL_Color color = {static_cast<Uint8>(is_ghost ? 174 : 115),
        static_cast<Uint8>(is_ghost ? 144 : 255), 255, static_cast<Uint8>(is_ghost ? 100 : 255)};
    graphics.draw_sprite(static_cast<int>(s.x) - 10, static_cast<int>(s.y) - 2, frame, color);
}

void render()
{
    graphics.set_render_target(graphics.gameTexture);
    graphics.fill_rect(9, 15, 26);
    const sprint::Room& room = sprint::course(race.room);
    // Quiet grid and original VVVVVV wall and spike tiles.
    for (int x = 0; x < 320; x += 16) graphics.fill_rect(x, 64, 1, 136, 15, 25, 39);
    for (int y = 64; y < 200; y += 16) graphics.fill_rect(0, y, 320, 1, 15, 25, 39);
    for (int x = 0; x < 320; x += 8)
    {
        graphics.draw_grid_tile(graphics.grphx.im_tiles_white, 1, x, 56, 8, 8, room.r, room.g, room.b);
        graphics.draw_grid_tile(graphics.grphx.im_tiles_white, 1, x, 200, 8, 8, room.r, room.g, room.b);
    }
    graphics.fill_rect(0, 63, 320, 1, room.r, room.g, room.b);
    graphics.fill_rect(0, 200, 320, 1, room.r, room.g, room.b);
    for (int i = 0; i < room.count; ++i)
    {
        const sprint::Hazard& h = room.hazards[i];
        for (int x = h.x; x < h.x + h.width; x += 8)
            graphics.draw_grid_tile(graphics.grphx.im_tiles_white, h.ceiling ? 9 : 8,
                x, h.ceiling ? 64 : 192, 8, 8, 255, 115, 141);
    }
    for (int y = 64; y < 200; y += 8)
        graphics.fill_rect(314, y, 6, 4, room.r / 2, room.g / 2, room.b / 2);
    if (show_ghost && !race.practice && race.started && race.ticks > 0
        && static_cast<size_t>(race.ticks) <= ghost.size())
    {
        const sprint::Sample& s = ghost[race.ticks - 1];
        if (s.room == race.room) runner(s, true);
    }
    if (!race.respawn_ticks || race.respawn_ticks % 2 == 0) runner(race.sample(), false);

    text(8, 6, "FLIP / SPRINT", 115, 255, 245);
    char line[80];
    std::sprintf(line, "%s  %02d/05", race.practice ? "PRACTICE" : "SPRINT", race.room + 1);
    text(8, 22, line, 143, 163, 185);
    text(312, 6, sprint::time_string(race.ticks), 238, 247, 255, PR_RIGHT | PR_2X);
    text(312, 28, best ? "PB " + sprint::time_string(best) : "PB --:--.---", 143, 163, 185, PR_RIGHT);
    for (int i = 0; i < sprint::ROOM_COUNT; ++i)
        graphics.fill_rect(8 + i * 62, 44, 56, 3, i <= race.room ? room.r : 30,
            i <= race.room ? room.g : 43, i <= race.room ? room.b : 61);
    text(8, 214, room.name, room.r, room.g, room.b);
    std::sprintf(line, "D:%d", race.deaths);
    text(312, 214, line, 143, 163, 185, PR_RIGHT);
    text(8, 230, "R RETRY  P PRACTICE  G GHOST  ESC", 108, 130, 154);

    // Cumulative comparison against the same room of the personal best.
    if (race.started && race.room > 0 && !race.practice && !race.finished)
    {
        const int split = race.room - 1;
        const int delta = race.splits[split] - best_splits[split];
        text(160, 80, best ? sprint::time_string(delta, true) : "SPLIT " + sprint::time_string(race.splits[split]),
            best && delta > 0 ? 255 : 120, best && delta > 0 ? 135 : 240, 180, PR_CEN | PR_BOR);
    }
    if (!race.started)
    {
        panel(16, 89, 288, 89);
        text(160, 98, race.practice ? "PRACTICE / NO RECORDS" : "5 ROOMS. ONE CLOCK.", 115, 255, 245, PR_CEN);
        text(160, 116, "A/D or arrows: move", 216, 225, 239, PR_CEN);
        text(160, 130, "SPACE: flip gravity on a surface", 216, 225, 239, PR_CEN);
        text(160, 150, race.practice ? "[ / ] select room" : "Move to start. Beat your ghost.", 143, 163, 185, PR_CEN);
        text(160, 164, "MOD OF VVVVVV BY TERRY CAVANAGH", 108, 130, 154, PR_CEN);
    }
    if (race.started && !race.valid && !race.practice && !race.finished)
        text(160, 102, "UNRANKED: PAUSED / R TO RESET", 255, 195, 95, PR_CEN | PR_BOR);
    if (race.finished)
    {
        panel(24, 67, 272, 136);
        text(160, 76, race.practice ? "PRACTICE COMPLETE" : new_best ? "NEW PERSONAL BEST" : "SPRINT COMPLETE",
            115, 255, 245, PR_CEN);
        text(160, 92, sprint::time_string(race.ticks), 238, 247, 255, PR_CEN | PR_2X);
        if (race.practice)
            text(160, 122, "[ / ] NEXT ROOM   P SPRINT", 164, 139, 250, PR_CEN);
        else
        {
            for (int i = 0; i < sprint::ROOM_COUNT; ++i)
            {
                std::sprintf(line, "%02d  %s", i + 1, sprint::time_string(race.splits[i]).c_str());
                text(72, 116 + i * 11, line, 143, 163, 185);
            }
        }
        const std::string status = race.practice ? "ROOM TIMER ONLY" : !race.valid ? "UNRANKED: RUN WAS PAUSED"
            : save_failed ? "SAVE FAILED: BEST IS IN MEMORY" : previous_best ? "VS PB " + sprint::time_string(race.ticks - previous_best, true)
            : "FIRST FINISH. NOW GO FASTER.";
        text(160, 176, status, 255, 195, 95, PR_CEN);
        text(160, 190, "R / ENTER: RUN IT BACK", 216, 225, 239, PR_CEN);
    }
    if (paused)
    {
        panel(24, 89, 272, 88);
        text(160, 100, "PAUSED", 115, 255, 245, PR_CEN | PR_2X);
        text(160, 128, "ESC RESUME   R RETRY   Q QUIT", 216, 225, 239, PR_CEN);
        text(160, 148, "Paused runs cannot set a PB.", 255, 195, 95, PR_CEN);
        text(160, 164, "BASED ON VVVVVV / TERRY CAVANAGH", 108, 130, 154, PR_CEN);
    }
    graphics.render();
}
}
