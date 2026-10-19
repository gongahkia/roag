"""Short, paginated field lessons. The tutorial holds simulation time."""

from dataclasses import dataclass


@dataclass(frozen=True)
class Lesson:
    title: str
    story: str
    instructions: str
    objective: str


LESSONS = (
    Lesson(
        "ONE LIGHT IN THE DARK",
        "Only Ada reached Tranquility. The generator hums, the air is good, and the other sleeping pods are empty.",
        "Space runs or pauses time. Arrows move the cursor. Scroll over the map to zoom at your pointer, or use +/- to zoom at the cursor; 0 resets the view. Ada walks, works, eats, and sleeps on her own. Menus hold time, including this tutorial.",
        "Take a breath. The colony starts with one person.",
    ),
    Lesson(
        "A VOICE IN THE WASTELAND",
        "Mika's damaged suit is losing oxygen outside the outpost. A distress signal appears as a red ? on the map, even beyond explored ground.",
        "Click a survivor's red ? directly on the map to send a ready colonist. Each rescue kit costs 2 food and 2 water. You can also move the cursor onto them and press Enter. Press r to jump between distress signals on the map.",
        "First job: click Mika's ? to send Ada. Space resumes time.",
    ),
    Lesson(
        "BRING THEM HOME",
        "Reaching someone is only half the rescue. Ada shares emergency oxygen, then escorts the injured survivor back at half walking speed.",
        "Survivors join your crew only after reaching pressurized shelter. They arrive hungry and exhausted, then eat, sleep, and work. More distress signals arrive over time. Leave someone too long and their oxygen, then health, runs out.",
        "Grow the colony through rescues. Press c to meet your crew.",
    ),
    Lesson(
        "LIVE OFF THE MOON",
        "Every new arrival needs air, water, food, and a place to recover. The supplies in this abandoned outpost will not last forever.",
        "Press d, move onto blue ice (~), amber ore (%), or purple crystal (*), then Enter to mark it for mining. Use v to mark a rectangle. Press p to survey visible open ground and uncover more terrain.",
        "Mine ice for water, ore for metal, and crystals for equipment.",
    ),
    Lesson(
        "BUILD A PLACE WORTH SAVING",
        "Air spreads through rooms one cell at a time. A single missing hull tile lets the room breathe into the wasteland.",
        "Press b to choose a blueprint, then Enter on the map to order it. Machines need deck and connected power. Enclose deck with hull and an airlock (+). Press o for oxygen, heat, and power overlays. Repair meteor breaches with pressure hull.",
        "Expand solar power and storage before adding hungry machines.",
    ),
    Lesson(
        "A FUTURE AT TRANQUILITY",
        "Earth is listening. Give the people you bring home a future here, then send a signal that Tranquility is alive.",
        "Rescue at least 3 survivors. Reach cycle 3 with 4 living crew and 20 food. Build an Earth uplink (60 metal + 20 crystal); keep it powered (5 kW) and pressurized. Founding the colony is a milestone; you can keep rescuing and building afterward.",
        "Click ? to rescue   r locate   c crew   t tutorial   q save & quit",
    ),
)
