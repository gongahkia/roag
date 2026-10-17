#include "SpeedrunCore.h"
#include <algorithm>
#include <cstdio>
#include <cmath>

namespace sprint
{
static const Room rooms[ROOM_COUNT] = {
    {"LAUNCH WINDOW", "Flip before the spikes. Land to flip again.", 85, 232, 225,
        {{96, 32, false}, {208, 40, true}, {0, 0, false}}, 2},
    {"POLAR OPPOSITES", "Keep your momentum through the gate.", 164, 139, 250,
        {{72, 40, true}, {176, 48, false}, {0, 0, false}}, 2},
    {"THREAD THE NEEDLE", "Early flips make a clean line.", 255, 195, 95,
        {{80, 40, false}, {192, 48, true}, {280, 24, false}}, 3},
    {"NEGATIVE SPACE", "Use both surfaces. Watch your landing.", 255, 125, 157,
        {{64, 48, true}, {168, 40, false}, {272, 32, true}}, 3},
    {"THE LAST FLIP", "Finish anywhere along the right edge.", 130, 235, 154,
        {{80, 48, false}, {192, 48, true}, {288, 24, false}}, 3}
};
const Room& course(int room) { return rooms[std::max(0, std::min(ROOM_COUNT - 1, room))]; }

std::string time_string(int ticks, bool sign)
{
    const long ms = static_cast<long>(ticks) * TICK_MS;
    const long value = ms < 0 ? -ms : ms;
    char buffer[48];
    std::sprintf(buffer, "%s%02ld:%02ld.%03ld", sign ? (ms < 0 ? "-" : "+") : "",
        value / 60000, (value / 1000) % 60, value % 1000);
    return buffer;
}

Race::Race() { reset(); }
void Race::respawn()
{
    x = 16; y = 180; vx = vy = 0;
    inverted = false; grounded = true; left = false;
}
void Race::reset(bool training, int first_room)
{
    practice = training; valid = !training;
    room = std::max(0, std::min(ROOM_COUNT - 1, first_room));
    if (room != 0) valid = false;
    started = finished = false;
    ticks = deaths = flips = respawn_ticks = 0;
    std::fill(splits, splits + ROOM_COUNT, 0);
    recording.clear();
    respawn();
}
Sample Race::sample() const
{
    Sample s = {room, x, y, inverted, left};
    return s;
}
bool Race::eligible() const
{
    return finished && valid && !practice && ticks > 0;
}
void Race::step(const Input& input)
{
    if (finished) return;
    if (!started && (input.direction || input.flip)) started = true;
    if (!started) return;
    ++ticks;
    if (respawn_ticks > 0)
    {
        --respawn_ticks;
    }
    else
    {
        if (input.flip && grounded)
        {
            inverted = !inverted;
            grounded = false;
            vy = inverted ? -1.5f : 1.5f;
            ++flips;
        }
        const int direction = std::max(-1, std::min(1, input.direction));
        if (direction)
        {
            vx = std::max(-3.0f, std::min(3.0f, vx + direction * 0.8f));
            left = direction < 0;
        }
        else
        {
            vx *= 0.65f;
            if (std::fabs(vx) < 0.1f) vx = 0;
        }
        vy = std::max(-6.0f, std::min(6.0f, vy + (inverted ? -0.6f : 0.6f)));
        x = std::max(0.0f, x + vx);
        y += vy;
        grounded = false;
        if (y <= 64) { y = 64; vy = 0; grounded = inverted; }
        if (y >= 180) { y = 180; vy = 0; grounded = !inverted; }

        bool hit = false;
        const Room& c = course(room);
        for (int i = 0; i < c.count; ++i)
        {
            const Hazard& h = c.hazards[i];
            // Slightly inset from the visual tips; no hidden collision margin.
            if (x + 11 > h.x + 2 && x < h.x + h.width - 2
                && (h.ceiling ? y < 71 : y + 20 > 193)) hit = true;
        }
        if (hit)
        {
            ++deaths;
            respawn();
            respawn_ticks = 3;
        }
        else if (x >= 308)
        {
            splits[room] = ticks;
            if (practice || room == ROOM_COUNT - 1) finished = true;
            else { ++room; x = 0; }
        }
    }
    if (recording.size() < MAX_GHOST_TICKS) recording.push_back(sample());
}
}
