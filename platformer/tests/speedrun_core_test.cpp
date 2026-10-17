#include "SpeedrunCore.h"
#include <cstdlib>
#include <iostream>
#include <vector>

static void check(bool result, const char* message)
{
    if (!result) { std::cerr << "FAIL: " << message << '\n'; std::exit(1); }
}

// A player strategy, not a hardcoded finish: approach hazards at full speed,
// flip early when a hazard is on the surface we are standing on.
static sprint::Input route(const sprint::Race& race)
{
    const sprint::Room& room = sprint::course(race.room);
    bool flip = false;
    if (race.grounded)
        for (int i = 0; i < room.count; ++i)
        {
            const sprint::Hazard& hazard = room.hazards[i];
            if (hazard.x + hazard.width > race.x && hazard.ceiling == race.inverted
                && race.x >= hazard.x - 64) flip = true;
        }
    return sprint::Input(1, flip);
}

int main()
{
    sprint::Race race;
    for (int i = 0; i < 100; ++i) race.step(sprint::Input());
    check(!race.started && race.ticks == 0, "clock waits for first movement");
    std::vector<sprint::Input> replay;
    while (!race.finished && race.ticks < 1000)
    {
        const sprint::Input input = route(race);
        replay.push_back(input);
        race.step(input);
        check(race.deaths == 0, "all five rooms have a playable full-speed route");
    }
    check(race.finished && race.eligible(), "full course finishes and qualifies");
    check(race.room == sprint::ROOM_COUNT - 1, "finish requires the fifth room");
    check(race.flips == 11, "route uses both surfaces throughout the course");
    check(race.recording.size() == replay.size(), "ghost has a sample for every tick");
    for (int i = 1; i < sprint::ROOM_COUNT; ++i)
        check(race.splits[i] > race.splits[i - 1], "splits are cumulative and ordered");
    check(race.splits[4] == race.ticks, "final split matches the finish time");
    const int finish = race.ticks;
    race.step(sprint::Input(1, true));
    check(race.ticks == finish, "finish freezes the timer");

    sprint::Race copy;
    for (size_t i = 0; i < replay.size(); ++i)
    {
        copy.step(replay[i]);
        const sprint::Sample a = copy.sample(), b = race.recording[i];
        check(a.room == b.room && a.x == b.x && a.y == b.y && a.inverted == b.inverted,
            "replaying input reproduces every ghost position exactly");
    }
    check(copy.ticks == finish && copy.eligible(), "replay finishes at the same time");
    copy.valid = false;
    check(!copy.eligible(), "invalidated runs cannot set records");

    for (int room = 0; room < sprint::ROOM_COUNT; ++room)
    {
        sprint::Race practice;
        practice.reset(true, room);
        for (int i = 0; !practice.finished && i < 200; ++i) practice.step(route(practice));
        check(practice.finished && practice.room == room && practice.deaths == 0,
            "every practice room is completable from its checkpoint");
        check(!practice.eligible(), "practice never sets a race record");
    }

    sprint::Race crash;
    for (int i = 0; i < 35; ++i) crash.step(sprint::Input(1));
    check(crash.deaths > 0 && crash.room == 0 && crash.ticks == 35,
        "spikes respawn the player and keep the clock running");
    crash.reset();
    check(crash.ticks == 0 && crash.deaths == 0 && crash.recording.empty() && crash.room == 0,
        "retry clears the entire attempt");
    crash.step(sprint::Input(0, true));
    crash.step(sprint::Input(0, true));
    check(crash.inverted && crash.flips == 1, "gravity cannot flip again in midair");
    check(sprint::time_string(1) == "00:00.034" && sprint::time_string(-1, true) == "-00:00.034",
        "timer and signed deltas use the simulation timestep");
    std::cout << "All sprint checks passed. Full course: " << sprint::time_string(finish) << '\n';
}
