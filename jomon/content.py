"""Small authored tables for Hearthford's material choices and encounters."""

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
        "objective": "Recover two cases of iron fittings from the stranded wheelhouse cart.",
        "opportunity": "charcoal",
        "hazard": "floodwater",
    },
    {
        "condition": "A salt surge has warped sheds along the lower river edge.",
        "work": "Hearthford cures fish and repairs coast-bound cargo boats.",
        "pressure": "The smokehouse needs timber braces before the next tide.",
        "commodity": "timber",
        "objective": "Recover two timber braces from the stranded wheelhouse cart.",
        "opportunity": "salt fish",
        "hazard": "rising tide",
    },
    {
        "condition": "Cold fog has delayed fuel barges and left the forges idle.",
        "work": "Hearthford's smiths maintain mill gear and river tools.",
        "pressure": "The smithy needs charcoal while the safe towpath is closed.",
        "commodity": "charcoal",
        "objective": "Recover two dry charcoal sacks from the stranded wheelhouse cart.",
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

# Behavior stays direct in actions.py rather than becoming an ability schema.
WEAPONS = {
    "billhook": ("Billhook", "close strike; hooks machinery or a braced foe"),
    "spear": ("Ash spear", "reach-two attack and positional control"),
    "cudgel": ("Leadwood cudgel", "quiet impact that quickly breaks morale"),
    "staff": ("River staff", "modest attack and safer guarded movement"),
    "hand axe": ("Hand axe", "heavy close strike; breaks shutters and braces"),
    "crossbow": ("Windlass crossbow", "range-five shot followed by a reload"),
}

GEAR = {
    "buckler": ("Buckler", "turns a telegraphed close strike while guarding"),
    "rope": ("Tarred rope", "secures water, winches, and unstable crossings"),
    "quiet shoes": ("Reed-soled shoes", "reduce travel noise on surveyed routes"),
    "repair tools": ("Repair tools", "quietly alter machinery and the objective"),
    "smoke pot": ("Smoke pot", "finite cover that breaks aim and pursuit"),
    "cargo harness": ("Cargo harness", "more capacity and protects one lost stack"),
    "trade seals": ("Witnessed trade seals", "material leverage in human encounters"),
    "hooded lantern": ("Hooded lantern", "controls territorial beasts and reveals glass"),
}

SUPPORTS = {
    "route survey": ("Pilot's route survey", "reveals exits and makes detours quieter"),
    "field care": ("Healer's field care", "changes the first serious injury"),
    "porter watch": ("Porter's watch", "adds bulk capacity and guards cargo loss"),
    "carpenter rig": ("Carpenter's rig", "makes controls quiet and stabilises structures"),
    "factor surety": ("Factor's surety", "strengthens terms and merchant exchange"),
}

DISCOVERIES = {
    "willow dressing": ("consumable", "A bitter wrap that treats one expedition injury."),
    "dry smoke charge": ("consumable", "Refills a spent smoke pot once."),
    "pulley key": ("tool", "A mill key that makes the wheelhouse controls safer."),
    "reed-step notes": ("technique", "Practical marks for moving quietly through mud."),
    "sealed tally": ("trade", "A recoverable account worth paper or merchant credit."),
}

RELICS = {
    "river-glass ward": "A finite cold shard that breaks instead of its bearer.",
    "tide-knot charm": "A specifically knotted river-glass cord that stills one pursuit.",
}

MERCHANT_ITEMS = {
    "hand axe": (2, "weapon"),
    "crossbow": (3, "weapon"),
    "smoke pot": (1, "gear"),
    "cargo harness": (2, "gear"),
    "hooded lantern": (2, "gear"),
    "willow dressing": (1, "consumable"),
    "tide-knot charm": (3, "relic"),
}

CONTACT_NAMES = ("Mara Venn", "Tomas Reed", "Iria Pike", "Sela Moss")

JOMON_MAP = (
    "################################",
    "#.............#................#",
    "#.T...........#.....H..........#",
    "#..............................#",
    "#.C...........L.....P..........#",
    "#..............................+",
    "#.............#................#",
    "#.a...........#.....s..........#",
    "#..............................#",
    "################################",
)

HELP_LINES = (
    "Move with arrows, HJKL, or YUBN diagonals. Border arrows lead between rooms.",
    "Enter/E interacts. A attacks. G guards or reloads. V negotiates. X uses gear.",
    "Map: @ courier, a ally, c contact, h/s/x/b hostiles, X elite, R cargo, ? discovery.",
    "Water ~, wall #, exits <>^v/+, controls &, shutter D, cargo cover O, hazard m/%.",
    "At Jomon's tavern C: choose courier, weapon, gear, support, and carried relic.",
    "I inspects inventory. S saves aboard Jomon. Inspection and cancelled choices take no time.",
    "Pressure is elapsed time + room depth + noise + valuables; all remain visible.",
    "Guard readable intent; smoke breaks aim; mud, shutters, controls, and cover affect encounters.",
    "Q asks before quitting. Escape closes or backs out of a popup.",
)
