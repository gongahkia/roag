#ifndef SPEEDRUN_CORE_H
#define SPEEDRUN_CORE_H

#include <string>
#include <vector>

// Independent of SDL so the exact race simulation can be replayed and tested.
namespace sprint
{
enum { ROOM_COUNT = 5, TICK_MS = 34, COURSE_VERSION = 1, MAX_GHOST_TICKS = 18000 };
struct Hazard { int x, width; bool ceiling; };
struct Room
{
    const char* name;
    const char* hint;
    int r, g, b;
    Hazard hazards[3];
    int count;
};
const Room& course(int room);
std::string time_string(int ticks, bool sign = false);

struct Sample
{
    int room;
    float x, y;
    bool inverted, left;
};
struct Input
{
    int direction;
    bool flip;
    Input(int d = 0, bool f = false) : direction(d), flip(f) {}
};
struct Race
{
    float x, y, vx, vy;
    bool inverted, grounded, left;
    bool started, finished, practice, valid;
    int room, ticks, deaths, flips, respawn_ticks;
    int splits[ROOM_COUNT];
    std::vector<Sample> recording;

    Race();
    void reset(bool training = false, int first_room = 0);
    void respawn();
    void step(const Input& input);
    Sample sample() const;
    bool eligible() const;
};
}
#endif
