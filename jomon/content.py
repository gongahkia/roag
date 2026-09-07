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
    "longbow": ("Yew longbow", "long sightline aim; movement and wet strings break preparation"),
    "sling": ("Shepherd's sling", "quick ranged cast that exploits height and loose stone"),
    "heavy crossbow": ("Trestle arbalest", "long severe lane; setup and two committed reload actions"),
    "pike": ("Boarding pike", "reach-four brace; ineffective when an enemy closes adjacent"),
    "paired knives": ("Paired short knives", "adjacent double cut followed by a mobile guarded step"),
    "javelins": ("Bundle of javelins", "finite immediate throws or a close braced thrust"),
    "war hammer": ("Quarry war hammer", "loud armour break and forceful knockback"),
    "weighted net": ("Weighted net and knife", "range-four restraint followed by close control"),
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
    "pine resin dressing": ("consumable", "A single treatment that protects a wounded limb."),
    "brine wash": ("consumable", "Cleans one salt or cut status but chills exposed skin."),
    "splint roll": ("consumable", "Stabilises one arm or leg injury for an expedition."),
    "tide pin": ("tool", "Marks one tide change so a closing route remains readable."),
    "charcoal key": ("tool", "A soot-rubbed key from the forest storehouses."),
    "limestone wedge": ("tool", "Secures or deliberately releases one marked rock face."),
    "dry lamp wick": ("supply", "Restores two measures of finite sheltered light."),
    "fletched arrows": ("ammunition", "Four dry longbow arrows in a waxed wrap."),
    "sling shot pouch": ("ammunition", "Six selected stones that fit a sling cup."),
    "quarrel case": ("ammunition", "Two heavy arbalest bolts in a rigid case."),
    "casting net bundle": ("ammunition", "One repaired weighted net for another cast."),
    "salt-house chit": ("trade", "A witnessed claim on salt fish or merchant credit."),
}

RELICS = {
    "river-glass ward": "A finite cold shard that breaks instead of its bearer.",
    "tide-knot charm": "A specifically knotted river-glass cord that stills one pursuit.",
    "ebbglass spindle": "A finite glass spindle that holds one receding waterline, then clouds forever.",
    "coalheart seed": "A warm mineral seed that consumes smoke once and leaves the bearer painfully chilled.",
    "hollow-bell shard": "A cracked bronze sliver that moves one sound between levels and may call unintended listeners.",
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
    "tide ledger": (1, "observable tide warnings also improve salt-house terms"),
    "cork float": (2, "one valuable stack remains buoyant during a current loss"),
    "salt veil": (1, "head cover also filters grit and light smoke"),
    "wreck key": (1, "opens named coast lockers without breaking their condition"),
    "gull cord": (2, "a controlled coastal descent can carry one secured cargo stack"),
    "storm vane": (1, "a prepared ranged lane shows the next wind change"),
    "charcoal mask": (1, "smoke conceals while inhalation accumulates more slowly"),
    "resin grip": (1, "wet hands retain tool and bow preparation"),
    "bird whistle": (1, "a deliberate sound can pull trackers toward a chosen trail"),
    "coppice map": (2, "dense-growth shortcuts cost less time after discovery"),
    "thorn weave": (1, "guarding in thorns protects exposed arms and stores momentum"),
    "ember cloth": (2, "smoke and heat can be crossed once without losing guard"),
    "limestone cleat": (1, "light footwear gains scree grip without boot weight"),
    "echo slate": (1, "loud stone impacts expose alerts on two connected levels"),
    "quarry brace": (2, "a guarded heavy load stabilises one weak authored floor"),
    "sling cup": (1, "sling casts from height ignore partial cover and daze"),
    "chalk cipher": (1, "quarry marks reveal a nearby cache when examined"),
    "fall sail": (2, "a deliberate fall becomes lateral movement at the landing"),
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
    "war hammer": (3, "weapon"),
    "weighted net": (2, "weapon"),
    "quiet shoes": (1, "gear"),
    "trade seals": (1, "gear"),
    "longbow": (3, "weapon"),
    "heavy crossbow": (5, "weapon"),
    "pike": (3, "weapon"),
}

# Bounded authored roles. Regional placement and budgets live in encounters.py;
# actions remain ordinary Threat decisions rather than data-driven scripts.
ENEMY_ARCHETYPES = {
    "coast-tide-runner": {"region": "greywash", "name": "tide-cut cargo runner", "profile": "pursuer", "role": "thief", "goal": "steal cargo", "vision": 8, "hearing": 8, "range": 1, "capability": "crosses draining mud quickly", "morale": 2, "terrain": "tidal mud", "counterplay": "guard the load or cut off its marked shoreward escape", "budget": 2},
    "coast-dune-hunter": {"region": "greywash", "name": "dune longbow hunter", "profile": "ranged", "role": "shooter", "goal": "hold distance", "vision": 12, "hearing": 6, "range": 11, "capability": "seeks dune height before aiming", "morale": 2, "terrain": "dunes", "counterplay": "use wreck cover, smoke, or the landward gully", "budget": 3, "ranged_kind": "longbow"},
    "coast-wreck-shield": {"region": "greywash", "name": "wreck shield bearer", "profile": "reach", "role": "protector", "goal": "protect ally", "vision": 7, "hearing": 8, "range": 2, "capability": "interposes for a ranged ally", "morale": 4, "terrain": "wreck lanes", "counterplay": "pull the bearer away or change elevation", "budget": 3},
    "coast-salt-slinger": {"region": "greywash", "name": "salt-bank slinger", "profile": "ranged", "role": "skirmisher", "goal": "hold distance", "vision": 10, "hearing": 7, "range": 8, "capability": "casts while withdrawing across firm flats", "morale": 2, "terrain": "salt banks", "counterplay": "close through deep mud or deny loose stone", "budget": 2, "ranged_kind": "sling"},
    "coast-netter": {"region": "greywash", "name": "mudflat netter", "profile": "reach", "role": "controller", "goal": "deny route", "vision": 7, "hearing": 9, "range": 4, "capability": "marks a net lane that worsens current", "morale": 3, "terrain": "shallows", "counterplay": "move before the cast or cut the anchor rope", "budget": 2},
    "coast-lookout": {"region": "greywash", "name": "signal-mast lookout", "profile": "pursuer", "role": "lookout", "goal": "raise alarm", "vision": 13, "hearing": 6, "range": 1, "capability": "signals shore interceptors", "morale": 1, "terrain": "mast platforms", "counterplay": "remain unseen, distract, or interrupt the signal", "budget": 1},
    "coast-elite": {"region": "greywash", "name": "storm-chain captain", "profile": "reach", "role": "elite", "goal": "close tide chain", "vision": 10, "hearing": 10, "range": 3, "capability": "changes the escape route by hauling a tide chain", "morale": 5, "terrain": "chain house", "counterplay": "release the windlass, isolate support, or leave before the channel fills", "budget": 6, "elite": True},

    "forest-trail-watch": {"region": "greenwold", "name": "charcoal trail watcher", "profile": "pursuer", "role": "lookout", "goal": "raise alarm", "vision": 8, "hearing": 11, "range": 1, "capability": "recognises disturbed brush and signal birds", "morale": 2, "terrain": "forest trails", "counterplay": "move crosswind, use water, or silence the signal", "budget": 1},
    "forest-resin-hunter": {"region": "greenwold", "name": "resinwood bow hunter", "profile": "ranged", "role": "shooter", "goal": "obtain line of fire", "vision": 10, "hearing": 10, "range": 10, "capability": "tracks last-known positions through canopy gaps", "morale": 3, "terrain": "clearings", "counterplay": "change trail, enter dense growth, or use smoke downwind", "budget": 3, "ranged_kind": "longbow"},
    "forest-hook": {"region": "greenwold", "name": "coppice hook runner", "profile": "pursuer", "role": "flanker", "goal": "flank", "vision": 7, "hearing": 9, "range": 2, "capability": "uses side trails toward the courier's last-known flank", "morale": 3, "terrain": "coppice", "counterplay": "hold a narrow bridge or reverse through a loop", "budget": 2},
    "forest-smoke-tender": {"region": "greenwold", "name": "illicit burn smoke-tender", "profile": "ranged", "role": "suppressor", "goal": "deny area", "vision": 6, "hearing": 10, "range": 7, "capability": "feeds smoke into a watched clearing", "morale": 2, "terrain": "burn pits", "counterplay": "change wind at the shutter or quench the pit", "budget": 3, "ranged_kind": "sling"},
    "forest-tusker": {"region": "greenwold", "name": "charcoal-bristled tusker", "profile": "animal", "role": "territorial", "goal": "defend territory", "vision": 5, "hearing": 12, "range": 1, "capability": "charges toward loud movement but stops beyond its wallow", "morale": 4, "terrain": "wallow", "counterplay": "distract with sound, climb, or circle outside its territory", "budget": 3},
    "forest-pack-runner": {"region": "greenwold", "name": "hidden-store pack runner", "profile": "pursuer", "role": "thief", "goal": "steal treasure", "vision": 7, "hearing": 8, "range": 1, "capability": "takes opened-cache goods and exits by a side trail", "morale": 1, "terrain": "cache trails", "counterplay": "secure the pack or block the escape trail", "budget": 2},
    "forest-elite": {"region": "greenwold", "name": "ash-cloak fire warden", "profile": "ranged", "role": "elite", "goal": "drive the burn", "vision": 9, "hearing": 12, "range": 9, "capability": "redirects smoke and closes one clearing while support lives", "morale": 5, "terrain": "raised burn walk", "counterplay": "quench two feed points, change wind, or remove the signal watcher", "budget": 6, "elite": True, "ranged_kind": "longbow"},

    "upland-ridge-slinger": {"region": "whitecairn", "name": "ridge sling ward", "profile": "ranged", "role": "shooter", "goal": "seek elevation", "vision": 13, "hearing": 8, "range": 9, "capability": "casts over low terrace cover", "morale": 3, "terrain": "ridges", "counterplay": "take the switchback underhang or contest the ridge", "budget": 3, "ranged_kind": "sling"},
    "upland-pike": {"region": "whitecairn", "name": "quarry pike holder", "profile": "reach", "role": "protector", "goal": "hold route", "vision": 10, "hearing": 8, "range": 4, "capability": "braces a bridge or switchback", "morale": 4, "terrain": "bridges", "counterplay": "hook, net, climb around, or negotiate the toll", "budget": 3},
    "upland-scree-runner": {"region": "whitecairn", "name": "scree-side flanker", "profile": "pursuer", "role": "flanker", "goal": "flank", "vision": 10, "hearing": 9, "range": 1, "capability": "descends unstable scree without losing footing", "morale": 2, "terrain": "scree", "counterplay": "trigger controlled rockfall or hold firm ground", "budget": 2},
    "upland-lime-tender": {"region": "whitecairn", "name": "lime-kiln smoke tender", "profile": "ranged", "role": "suppressor", "goal": "deny area", "vision": 7, "hearing": 9, "range": 8, "capability": "vents caustic smoke across a lane", "morale": 2, "terrain": "kilns", "counterplay": "close the vent, approach from above, or wait crosswind", "budget": 3, "ranged_kind": "sling"},
    "upland-cave-hound": {"region": "whitecairn", "name": "pale quarry hound", "profile": "animal", "role": "tracker", "goal": "investigate sound", "vision": 4, "hearing": 13, "range": 1, "capability": "tracks sound through connected cave levels", "morale": 3, "terrain": "caves", "counterplay": "throw sound into a side shaft or reach daylight", "budget": 2},
    "upland-alarm-climber": {"region": "whitecairn", "name": "bell-rope climber", "profile": "pursuer", "role": "lookout", "goal": "raise alarm", "vision": 11, "hearing": 9, "range": 1, "capability": "climbs directly toward the warning bell", "morale": 1, "terrain": "tower", "counterplay": "cut the rope, block the ladder, or remain below the parapet", "budget": 1},
    "upland-elite": {"region": "whitecairn", "name": "false-bell quarry master", "profile": "reach", "role": "elite", "goal": "trigger rockfall", "vision": 12, "hearing": 11, "range": 3, "capability": "rings marked rockfall lanes and retreats between levels", "morale": 5, "terrain": "bell tower", "counterplay": "sever the bell line, shelter under arches, or expose the false toll", "budget": 6, "elite": True},
}

CONTACT_NAMES = ("Mara Venn", "Tomas Reed", "Iria Pike", "Sela Moss")

JOMON_MAP = (
    "################################################",
    "#..............................................#",
    "#.H..............==========....................#",
    "#................=  BAR   =....t.t.....t.t.....#",
    "#.L..............=   C    =....................#",
    "#................====+=====....t.t.....t.t.....#",
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
    "Move with arrows, HJKL, or YUBN diagonals. The camera follows across each seamless region.",
    "Enter/E interacts. A attacks. G guards or reloads. V negotiates. X uses gear.",
    "Map: @ courier, a ally, v visitor, c/M contacts, h/g/x/b hostiles, X elite, R cargo, C cache.",
    "Water ~, wall #, doors +, stairs <> and roof ^, controls &, hole O, weak floor d.",
    "Aboard, speak beside people to switch or recruit; tavern C selects support; chart P sets course.",
    "I opens the spatial pack/locker: move, rotate, transfer, equip, confirm, or cancel safely.",
    "Pressure is elapsed actions + geographic depth + noise + valuables; all remain visible.",
    "Normal colour is visible now; dim terrain is remembered; blank terrain remains unknown.",
    "Guard readable intent; smoke breaks aim; mud, water, controls, height, and walls matter.",
    "Q asks before quitting. Escape closes or backs out of a popup.",
)
