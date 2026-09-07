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

RECRUIT_TEMPLATES = (
    {
        "id": "recruit-maelin", "name": "Maelin Rook", "role": "tide runner",
        "technique": "ebb reader", "home_region": "greywash",
        "background": "A salt-runner who reads firm ground from draining ripples.",
        "build_tendency": "light armour, tide routes, and thrown weapons",
        "equipment": ["marsh waders", "javelins"],
        "terms": "Help the coast saltworks or offer two accountable credits.",
        "memory": "Jomon once carried medicine through a closing tidal channel.",
    },
    {
        "id": "recruit-jessa", "name": "Jessa Flint", "role": "netwright",
        "technique": "cast bind", "home_region": "greywash",
        "background": "A wreck-diver who knots weighted nets for surf and deck work.",
        "build_tendency": "nets, cargo recovery, and close control",
        "equipment": ["tarred gauntlets", "weighted net"],
        "terms": "Recover a named wreck locker without abandoning its owner.",
        "memory": "She will not call recovered property salvage while its keeper lives.",
    },
    {
        "id": "recruit-orra", "name": "Orra Fen", "role": "charcoal scout",
        "technique": "wind listener", "home_region": "greenwold",
        "background": "A burner-watch who follows smoke, disturbed birds, and crosswind sound.",
        "build_tendency": "stealth, smoke, bows, and open woodland routes",
        "equipment": ["felt hood", "longbow"],
        "terms": "Keep the forest burn from spreading into the medicine stands.",
        "memory": "Orra remembers which crews listened before entering the burnwood.",
    },
    {
        "id": "recruit-bran", "name": "Bran Wold", "role": "resin healer",
        "technique": "green poultice", "home_region": "greenwold",
        "background": "A travelling adult healer who trades resin dressings for safe passage.",
        "build_tendency": "injury control, thorns, and patient expeditions",
        "equipment": ["linen sleeves", "field satchel"],
        "terms": "Bring the wounded charcoal reeve home or settle their care.",
        "memory": "Bran judges households by how they return with wounded companions.",
    },
    {
        "id": "recruit-teren", "name": "Teren Chalk", "role": "quarry climber",
        "technique": "scree step", "home_region": "whitecairn",
        "background": "A face-worker who knows limestone fractures and suspended loads.",
        "build_tendency": "heavy tools, climbing, and breakable terrain",
        "equipment": ["hobnailed boots", "war hammer"],
        "terms": "Stabilise the quarry bridge or prove a safer route around it.",
        "memory": "Teren left a crew that treated preventable rockfall as fate.",
    },
    {
        "id": "recruit-sava", "name": "Sava Bell", "role": "ridge ward",
        "technique": "high arc", "home_region": "whitecairn",
        "background": "A shepherd-ward who uses sling arcs beyond cliff-edge cover.",
        "build_tendency": "sling fire, height, light loads, and lookout control",
        "equipment": ["leather leggings", "sling"],
        "terms": "Break the false toll alarm without harming the upland carriers.",
        "memory": "Sava remembers every warning bell rung for private profit.",
    },
)

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
    "sealed tally": ("trade", "A recoverable account worth paper or merchant credit."),
}

RELICS = {
    "river-glass ward": "A finite cold shard that breaks instead of its bearer.",
    "tide-knot charm": "A specifically knotted river-glass cord that stills one pursuit.",
}

PASSIVES = {
    "reed sole wraps": (1, "guarded movement through mud stores a quiet step"),
    "counterweight ring": (1, "billhook pulls carry the courier with the target"),
    "waxed bowstring": (1, "crossbows keep committed aim through hard rain"),
    "mill-tooth wedge": (2, "axes and cudgels break marked floors safely"),
    "cliff cord": (2, "authored falls become controlled descents"),
    "smoke lens": (1, "smoke conceals without blinding adjacent space"),
    "river hooks": (2, "rope can recover cargo or cross floodwater"),
    "witness token": (1, "valuables strengthen material negotiation"),
    "rain cape": (2, "storm movement avoids its extra time cost"),
    "echo bead": (1, "loud actions reveal alerts on adjacent levels"),
    "salted dressing": (1, "deepens prepared field-care protection"),
    "high tread": (2, "attacks from above push a target one pace"),
}

TREASURE_REWARDS = tuple(PASSIVES) + (
    "willow dressing", "dry smoke charge", "sealed tally",
    "river-glass ward", "tide-knot charm",
)

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
    "################################################",
    "#..............................................#",
    "#.H..............==========....................#",
    "#................=  BAR   =....t.t.....t.t.....#",
    "#.L..............=   C    =....................#",
    "#................==========....t.t.....t.t.....#",
    "#.P............................................#",
    "#..............................t.t.....t.t.....#",
    "#..............................................+",
    "#......................t.t.....................#",
    "#..............................................#",
    "#......................t.t.....t.t.............#",
    "#..............................................#",
    "#..............................................#",
    "#..............................................#",
    "################################################",
)

HELP_LINES = (
    "Move with arrows, HJKL, or YUBN diagonals. The camera follows across seamless Hearthford.",
    "Enter/E interacts. A attacks. G guards or reloads. V negotiates. X uses gear.",
    "Map: @ courier, a ally, v visitor, c/M contacts, h/g/x/b hostiles, X elite, R cargo, C cache.",
    "Water ~, wall #, doors +, stairs <> and roof ^, controls &, hole O, weak floor d.",
    "Aboard, speak beside visible people to switch or recruit; tavern C selects crew support.",
    "I opens the spatial pack/locker: move, rotate, transfer, equip, confirm, or cancel safely.",
    "Pressure is elapsed actions + geographic depth + noise + valuables; all remain visible.",
    "Normal colour is visible now; dim terrain is remembered; blank terrain remains unknown.",
    "Guard readable intent; smoke breaks aim; mud, water, controls, height, and walls matter.",
    "Q asks before quitting. Escape closes or backs out of a popup.",
)
