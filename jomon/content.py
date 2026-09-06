"""Small authored tables used by deterministic world generation."""

from __future__ import annotations

COMMODITIES = {
    "charcoal": {"bulk": 2, "condition": "dry", "source": "wood burners", "use": "forge fuel"},
    "grain": {"bulk": 2, "condition": "dry", "source": "field farms", "use": "daily bread"},
    "ironwork": {"bulk": 3, "condition": "sound", "source": "riverside smiths", "use": "mill and vessel repair"},
    "lime": {"bulk": 3, "condition": "dry", "source": "lime burners", "use": "mortar and fields"},
    "paper": {"bulk": 1, "condition": "dry", "source": "rag mills", "use": "contracts and accounts"},
    "salt fish": {"bulk": 2, "condition": "sealed", "source": "coast smokehouses", "use": "stored food"},
    "timber": {"bulk": 4, "condition": "sound", "source": "managed woods", "use": "building and braces"},
    "wool": {"bulk": 2, "condition": "dry", "source": "upland folds", "use": "cloth and insulation"},
}

REGIONAL_CONTEXTS = (
    {
        "condition": "Long rain has swollen the river and drowned the mill path.",
        "work": "Hearthford keeps a grain mill beside a flood-damaged race.",
        "pressure": "The mill needs ironwork before its gate tears loose.",
        "commodity": "ironwork",
        "objective": "Recover two cases of iron fittings from the stranded works cart.",
        "opportunity": "charcoal",
        "hazard": "floodwater",
    },
    {
        "condition": "A salt surge has warped sheds along the lower river edge.",
        "work": "Hearthford cures fish and repairs coast-bound cargo boats.",
        "pressure": "The smokehouse needs timber braces before the next tide.",
        "commodity": "timber",
        "objective": "Recover two timber braces from the stranded works cart.",
        "opportunity": "salt fish",
        "hazard": "rising tide",
    },
    {
        "condition": "Cold fog has delayed fuel barges and left the forges idle.",
        "work": "Hearthford's smiths maintain mill gear and river tools.",
        "pressure": "The smithy needs charcoal while the safe towpath is closed.",
        "commodity": "charcoal",
        "objective": "Recover two dry charcoal sacks from the stranded works cart.",
        "opportunity": "paper",
        "hazard": "slick sluice water",
    },
)

FIRST_NAMES = (
    "Arel", "Bera", "Corin", "Dena", "Eris", "Fara", "Galen", "Hessa",
    "Iven", "Jora", "Kelan", "Mira", "Neris", "Oren", "Pava", "Risa",
    "Soren", "Tavi", "Vela", "Warin",
)
FAMILY_NAMES = (
    "Ash", "Barrow", "Cairn", "Dike", "Elm", "Ford", "Gull", "Hearth",
    "Ivy", "Keel", "Lark", "Moss", "Nettle", "Pike", "Quill", "Reed",
    "Silt", "Thorn", "Vale", "Wren",
)
ROLES = ("bargemaster", "pilot", "factor", "carpenter", "guard", "healer")
ROLE_EQUIPMENT = {
    "bargemaster": ("river pole", "waxed chart"),
    "pilot": ("lead line", "signal whistle"),
    "factor": ("ledger", "seal case"),
    "carpenter": ("adze", "oakum roll"),
    "guard": ("buckler", "hooked staff"),
    "healer": ("bandage roll", "herb case"),
}
ROLE_TECHNIQUE = {
    "bargemaster": "sure footing",
    "pilot": "quiet passage",
    "factor": "measured terms",
    "carpenter": "lever craft",
    "guard": "set stance",
    "healer": "field binding",
}

LOADOUTS = (
    ("arms", "Billhook & buckler", ("billhook", "buckler"), "hard strikes and a stronger guard"),
    ("smoke", "Smoke pot & rope", ("smoke pot", "rope"), "quiet evasion and safe water crossing"),
    ("tools", "Mallet & repair tools", ("mallet", "repair tools"), "alter the job at the flood control"),
)
SUPPORTS = (
    ("charts", "Pilot's route chart", "quieter travel and an alternate route"),
    ("treatment", "Healer's field dressing", "negates the first injury"),
    ("harness", "Porter's cargo harness", "four more bulk capacity and protected cargo"),
)

CONTACT_NAMES = ("Mara Venn", "Tomas Reed", "Iria Pike", "Sela Moss")
THREAT_NAMES = ("bank opportunist", "displaced levy guard")

JOMON_MAP = (
    "################################",
    "#.............#................#",
    "#.T...........#.....H..........#",
    "#..............................#",
    "#.C...........L.....P..........#",
    "#..............................+",
    "#.............#................#",
    "#.b...........#.....s..........#",
    "#..............................#",
    "################################",
)

# #: wall, ~: water, T: tree, M: contact, &: flood control, =: flooded crossing,
# R: objective resource, r: optional resource, +: Jomon gangplank.
REGION_MAP = (
    "################################################",
    "#..............#...TTTT....~~~~......#.........#",
    "#..h...........#....TT.....~~~~......#.........#",
    "#..............#...........~~~~......#....R....#",
    "#..###..###....#...TT......~~~~......#.........#",
    "#..............#...........~~~~......#.........#",
    "#....m.........#...TT......~~~~......#####.#####",
    "#..............#...........~~~~................#",
    "#..............#...............................#",
    "+.....M..................................R.....#",
    "#..............#...........~~~~................#",
    "#..###.........#...TT......~~~~......#####.#####",
    "#..............#...........~~~~......#.........#",
    "#..............#...........~~~~......#.........#",
    "#..............#........&..====......#.........#",
    "#..............#...........~~~~......#....R....#",
    "#..............#....r......~~~~......#.........#",
    "#..............#...........~~~~......#.........#",
    "################################################",
)

HELP_LINES = (
    "Move with arrows, HJKL, or YUBN diagonals.",
    "Enter/E interacts. A attacks. G guards. V negotiates. X uses gear.",
    "I shows carried equipment and goods. S saves when safe.",
    "On Jomon: C chooses courier, L loadout, P support, H inspects cargo/problem.",
    "Walk through + to depart or return. Movement and accepted actions take time.",
    "Inspecting, help, cancelled choices, and blocked movement take no time.",
    "Pressure is elapsed time + depth + noise + valuables; each remains visible.",
    "At &: tools can alter the commission; charts/smoke can open a quiet bypass.",
    "Q asks before quitting. Escape closes overlays.",
)
