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

COMMODITY_LOGISTICS = {
    "charcoal": {"handling": "keep covered and separate from sparks", "failure": "rain spoils heat; fire consumes the lot", "buyers": ("smiths", "kiln crews"), "environment": "burns hot and produces smoke", "quest_use": "restores fuel work after a route shortage", "equipment_use": "feeds controlled fires and powder preparation"},
    "grain": {"handling": "keep dry and raised above bilge water", "failure": "water swells sacks; fire destroys food", "buyers": ("mill fellowships", "travelling households"), "environment": "wet sacks gain condition loss and obstruct cargo space", "quest_use": "settles food shortages and seed claims", "equipment_use": "provisions expedition rest and cooking"},
    "ironwork": {"handling": "lash rigid cases against shifting cargo", "failure": "salt and lime abrade fittings; impact bends them", "buyers": ("millwrights", "bridge companies"), "environment": "adds structural mass and corrodes in slurry", "quest_use": "repairs gates, bridges and Jomon", "equipment_use": "supplies reinforcement and workshop fittings"},
    "lime": {"handling": "keep sealed, dry and away from skin", "failure": "water makes a caustic slurry; spill contaminates cloth", "buyers": ("mortar crews", "field courts"), "environment": "wet lime raises abrasive smoke and coating", "quest_use": "repairs masonry and disputed field soil", "equipment_use": "prepares finite lime pots and protective wash"},
    "paper": {"handling": "carry flat in a waxed case", "failure": "water erases accounts; fire removes evidence", "buyers": ("factors", "river authorities"), "environment": "burns quickly and absorbs water", "quest_use": "provides witnessed terms and replacement records", "equipment_use": "supports negotiation when seals are absent"},
    "salt fish": {"handling": "keep sealed and shaded from bilge heat", "failure": "broken seals spoil provisions; water weakens wrapping", "buyers": ("inland households", "coppice camps"), "environment": "salt residue contaminates wet cargo", "quest_use": "feeds isolated workers during route closure", "equipment_use": "extends voyage provisions without cooking"},
    "timber": {"handling": "brace long pieces and keep bindings sound", "failure": "fire spreads; saturation adds load; impact splits support", "buyers": ("shipwrights", "raised-bank companies"), "environment": "floats, burns and serves as load-bearing support", "quest_use": "repairs crossings, sheds and flood banks", "equipment_use": "provides braces, hafts and hull repair"},
    "wool": {"handling": "bundle dry and ventilate after spray", "failure": "water adds weight; sparks smoulder in packed cloth", "buyers": ("winter pilots", "armour workers"), "environment": "insulates when dry and absorbs floodwater", "quest_use": "supplies cold shelters and seasonal obligations", "equipment_use": "lines armour and protects winter rigging"},
}
for commodity, logistics in COMMODITY_LOGISTICS.items():
    COMMODITIES[commodity].update(logistics)


def validate_commodity_content() -> None:
    required = {
        "bulk", "condition", "source", "use", "handling", "failure",
        "buyers", "environment", "quest_use", "equipment_use",
    }
    for name, definition in COMMODITIES.items():
        if set(definition) != required or not all(definition[key] for key in required):
            raise ValueError(f"commodity {name!r} has an incomplete physical lifecycle")
        if not isinstance(definition["bulk"], int) or definition["bulk"] < 1:
            raise ValueError(f"commodity {name!r} has invalid bulk")
        if not isinstance(definition["buyers"], tuple) or len(definition["buyers"]) < 2:
            raise ValueError(f"commodity {name!r} needs at least two material buyers")

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
    "staff sling": ("Oak staff sling", "arcs beyond low cover but needs room to cast"),
    "hooked javelin": ("Hooked river javelin", "pulls position and leaves a recoverable shaft"),
    "boar spear": ("Crossbar boar spear", "reach brace pins charging animals outside knife range"),
    "handgonne": ("Powder handgonne", "loud prepared shot creates smoke and commits a slow reload"),
}

from .work_weapons import WORK_WEAPONS

WEAPONS.update({name: (spec.name, spec.description) for name, spec in WORK_WEAPONS.items()})
from .expanded_weapons import ARSENAL, BOMB_AMMUNITION

WEAPONS.update({name: (name.title(), spec.description) for name, spec in ARSENAL.items()})

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
    "handgonne charges": ("ammunition", "Three wrapped powder charges and lead balls."),
}

DISCOVERIES.update({
    "sealed pitch pot": ("ammunition", "One bulky fire pot for the pot sling; water stops ignition, not fuel loss."),
    "sealed lime pot": ("ammunition", "One abrasive cloud for the pot sling; wet lime remains caustic."),
    "sealed brine pot": ("ammunition", "One finite pot of salt water to quench, thaw or flood a visible cell."),
})

from .preparations import PREPARATIONS

DISCOVERIES.update({
    name: ("preparation", preparation.description)
    for name, preparation in PREPARATIONS.items()
})

RELICS = {
    "river-glass ward": "A finite cold shard that breaks instead of its bearer.",
    "tide-knot charm": "A specifically knotted river-glass cord that stills one pursuit.",
    "ebbglass spindle": "A finite glass spindle that holds one receding waterline, then clouds forever.",
    "coalheart seed": "A warm mineral seed that consumes smoke once and leaves the bearer painfully chilled.",
    "hollow-bell shard": "A cracked bronze sliver that moves one sound between levels and may call unintended listeners.",
    "stillwater filament": "A finite pale strand that arrests one local current, but calls listeners to its last motion.",
    "flood-mark clasp": "A repaired river clasp lowers nearby water and steadies soaked timber once; the resisting load leaves its bearer fatigued.",
    "ashglass lens": "A fire-clouded lens clears local smoke and exposes one hidden store, while the flare gives nearby groups the courier's position.",
    "quarry echo pin": "A stoneworker's resonant pin seats nearby warned supports once; its report alerts every listener in range.",
    "winter sounding bead": "A cold sounding bead freezes a bounded set of fresh shallows in winter, then leaves the bearer chilled.",
    "red-clay seal": "A fired account seal compels one witnessed local compact; the institution records two obligations for the concession.",
    "wreck-light prism": "A salvaged signal prism spends lamp oil to mark one unopened store and turn animals, but human lookouts see the flash.",
}

from .arc_relics import ARC_RELIC_DESCRIPTIONS

RELICS.update(ARC_RELIC_DESCRIPTIONS)

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
    "sighting knot": (1, "a staff sling reads one more pace and names its arcing lane"),
    "gullbone reel": (2, "a hooked javelin can be recovered directly when rope is readied"),
    "sluice token": (1, "witnessed control work is quiet even without a carpenter"),
    "cache bell": (1, "one sounding per region marks an unopened cache but alerts listeners"),
    "scar salve recipe": (1, "finite field dressing restores more health after an injury"),
    "load ledger": (2, "accounted cargo gains modest bulk and weight capacity but remains valuable"),
    "roof nail": (1, "a ranged line survives one careful move on an upper structure"),
    "fen sledge": (2, "a broad runner keeps mud from bogging a carried load or broadcasting the first step"),
    "ice awl": (1, "tests frozen shallows for firm footing and can break thin ice with a contextual material action"),
    "fire rake tooth": (1, "breaks a burning reed or timber cell into smoking ash without requiring a heavy tool"),
    "limewash seal": (1, "keeps wet lime from abrading the courier and their physically carried equipment"),
    "smoke braid": (1, "a close strike made from smoke gains one harm and one morale pressure"),
    "salvage tally": (1, "the first difficult cache opened in a region establishes one witnessed trade credit"),
    "counterbrace pin": (1, "guarding on a damaged support restores one support and cancels its warned collapse"),
    "pitch cup": (1, "the first ignition each expedition carries measured pitch instead of consuming lamp oil and burns longer"),
    "shingle skids": (2, "a carried load crosses released water without the extra exposed action"),
    "signal mirror": (1, "from elevation, a flash interrupts one marked ranged lane but reveals the courier to its group"),
    "market weights": (1, "a delivered dependency lot adds one verified stock and extra confidence to its named work account"),
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
    "staff sling": (3, "weapon"),
    "hooked javelin": (3, "weapon"),
    "boar spear": (3, "weapon"),
    "handgonne": (5, "weapon"),
    "reed brim": (2, "armour"), "kiln face wrap": (2, "armour"), "ridge visor": (4, "armour"),
    "cork-backed coat": (4, "armour"), "kiln apron": (3, "armour"), "winter felt coat": (4, "armour"),
    "reed splints": (2, "armour"), "quarry sleeves": (3, "armour"), "watch vambraces": (3, "armour"),
    "potter mitts": (2, "armour"), "archer tabs": (1, "armour"), "split-hide palms": (2, "armour"),
    "reed gaiters": (2, "armour"), "quarry chaps": (3, "armour"), "frost leggings": (3, "armour"),
    "peat pattens": (2, "armour"), "felt overboots": (2, "armour"), "ice cleats": (3, "armour"),
}
MERCHANT_ITEMS.update({key: (4, "weapon") for key in WORK_WEAPONS})
MERCHANT_ITEMS.update({key: (7 if spec.family == "gun" else 5, "weapon") for key, spec in ARSENAL.items()})
MERCHANT_ITEMS.update({kind.split(":", 1)[1]: (2, "consumable") for kind in BOMB_AMMUNITION.values()})
MERCHANT_ITEMS.update({f"sealed {material} pot": (2, "consumable") for material in ("pitch", "lime", "brine")})

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
    "coast-elite-wreck": {"region": "greywash", "name": "wreck-chain reeve", "profile": "ranged", "role": "elite", "goal": "strip marked salvage", "vision": 11, "hearing": 9, "range": 8, "capability": "hauls wreck cover out of a telegraphed firing lane", "morale": 4, "terrain": "wreck road", "counterplay": "move behind fixed dunes, dog the chain, or present the witnessed wreck account", "budget": 6, "elite": True, "ranged_kind": "sling"},

    "forest-trail-watch": {"region": "greenwold", "name": "charcoal trail watcher", "profile": "pursuer", "role": "lookout", "goal": "raise alarm", "vision": 8, "hearing": 11, "range": 1, "capability": "recognises disturbed brush and signal birds", "morale": 2, "terrain": "forest trails", "counterplay": "move crosswind, use water, or silence the signal", "budget": 1},
    "forest-resin-hunter": {"region": "greenwold", "name": "resinwood bow hunter", "profile": "ranged", "role": "shooter", "goal": "obtain line of fire", "vision": 10, "hearing": 10, "range": 10, "capability": "tracks last-known positions through canopy gaps", "morale": 3, "terrain": "clearings", "counterplay": "change trail, enter dense growth, or use smoke downwind", "budget": 3, "ranged_kind": "longbow"},
    "forest-hook": {"region": "greenwold", "name": "coppice hook runner", "profile": "pursuer", "role": "flanker", "goal": "flank", "vision": 7, "hearing": 9, "range": 2, "capability": "uses side trails toward the courier's last-known flank", "morale": 3, "terrain": "coppice", "counterplay": "hold a narrow bridge or reverse through a loop", "budget": 2},
    "forest-smoke-tender": {"region": "greenwold", "name": "illicit burn smoke-tender", "profile": "ranged", "role": "suppressor", "goal": "deny area", "vision": 6, "hearing": 10, "range": 7, "capability": "feeds smoke into a watched clearing", "morale": 2, "terrain": "burn pits", "counterplay": "change wind at the shutter or quench the pit", "budget": 3, "ranged_kind": "sling"},
    "forest-tusker": {"region": "greenwold", "name": "charcoal-bristled tusker", "profile": "animal", "role": "territorial", "goal": "defend territory", "vision": 5, "hearing": 12, "range": 1, "capability": "charges toward loud movement but stops beyond its wallow", "morale": 4, "terrain": "wallow", "counterplay": "distract with sound, climb, or circle outside its territory", "budget": 3},
    "forest-pack-runner": {"region": "greenwold", "name": "hidden-store pack runner", "profile": "pursuer", "role": "thief", "goal": "steal treasure", "vision": 7, "hearing": 8, "range": 1, "capability": "takes opened-cache goods and exits by a side trail", "morale": 1, "terrain": "cache trails", "counterplay": "secure the pack or block the escape trail", "budget": 2},
    "forest-elite": {"region": "greenwold", "name": "ash-cloak fire warden", "profile": "ranged", "role": "elite", "goal": "drive the burn", "vision": 9, "hearing": 12, "range": 9, "capability": "redirects smoke and closes one clearing while support lives", "morale": 5, "terrain": "raised burn walk", "counterplay": "quench two feed points, change wind, or remove the signal watcher", "budget": 6, "elite": True, "ranged_kind": "longbow"},
    "forest-elite-resin": {"region": "greenwold", "name": "resin-fire tracker", "profile": "pursuer", "role": "elite", "goal": "burn out concealed cargo", "vision": 7, "hearing": 13, "range": 2, "capability": "marks a resin patch, then ignites it into rising smoke", "morale": 4, "terrain": "resin grove", "counterplay": "leave the marked patch, cross water, or preserve the medicine coppice", "budget": 6, "elite": True},

    "upland-ridge-slinger": {"region": "whitecairn", "name": "ridge sling ward", "profile": "ranged", "role": "shooter", "goal": "seek elevation", "vision": 13, "hearing": 8, "range": 9, "capability": "casts over low terrace cover", "morale": 3, "terrain": "ridges", "counterplay": "take the switchback underhang or contest the ridge", "budget": 3, "ranged_kind": "sling"},
    "upland-pike": {"region": "whitecairn", "name": "quarry pike holder", "profile": "reach", "role": "protector", "goal": "hold route", "vision": 10, "hearing": 8, "range": 4, "capability": "braces a bridge or switchback", "morale": 4, "terrain": "bridges", "counterplay": "hook, net, climb around, or negotiate the toll", "budget": 3},
    "upland-scree-runner": {"region": "whitecairn", "name": "scree-side flanker", "profile": "pursuer", "role": "flanker", "goal": "flank", "vision": 10, "hearing": 9, "range": 1, "capability": "descends unstable scree without losing footing", "morale": 2, "terrain": "scree", "counterplay": "trigger controlled rockfall or hold firm ground", "budget": 2},
    "upland-lime-tender": {"region": "whitecairn", "name": "lime-kiln smoke tender", "profile": "ranged", "role": "suppressor", "goal": "deny area", "vision": 7, "hearing": 9, "range": 8, "capability": "vents caustic smoke across a lane", "morale": 2, "terrain": "kilns", "counterplay": "close the vent, approach from above, or wait crosswind", "budget": 3, "ranged_kind": "sling"},
    "upland-cave-hound": {"region": "whitecairn", "name": "pale quarry hound", "profile": "animal", "role": "tracker", "goal": "investigate sound", "vision": 4, "hearing": 13, "range": 1, "capability": "tracks sound through connected cave levels", "morale": 3, "terrain": "caves", "counterplay": "throw sound into a side shaft or reach daylight", "budget": 2},
    "upland-alarm-climber": {"region": "whitecairn", "name": "bell-rope climber", "profile": "pursuer", "role": "lookout", "goal": "raise alarm", "vision": 11, "hearing": 9, "range": 1, "capability": "climbs directly toward the warning bell", "morale": 1, "terrain": "tower", "counterplay": "cut the rope, block the ladder, or remain below the parapet", "budget": 1},
    "upland-elite": {"region": "whitecairn", "name": "false-bell quarry master", "profile": "reach", "role": "elite", "goal": "trigger rockfall", "vision": 12, "hearing": 11, "range": 3, "capability": "rings marked rockfall lanes and retreats between levels", "morale": 5, "terrain": "bell tower", "counterplay": "sever the bell line, shelter under arches, or expose the false toll", "budget": 6, "elite": True},
    "upland-elite-bridge": {"region": "whitecairn", "name": "bridge-breaker bellward", "profile": "ranged", "role": "elite", "goal": "drop the ridge bridge", "vision": 13, "hearing": 10, "range": 10, "capability": "marks a floor brace before breaking its crossing and changing elevation", "morale": 4, "terrain": "ridge bridge", "counterplay": "leave the marked brace, use the lower switchback, or ring the honest warning", "budget": 6, "elite": True, "ranged_kind": "heavy crossbow"},

    "hearth-bank-lookout": {"region": "hearthford", "name": "bank-road toll watch", "profile": "pursuer", "role": "lookout", "goal": "raise alarm", "vision": 9, "hearing": 8, "range": 1, "capability": "patrols a finite road circuit and signals only its own watch", "reaction": "returns to the road after a broken sightline", "morale": 2, "terrain": "bank road", "counterplay": "leave its sightline or interrupt the signal; a side meadow bypasses the patrol", "budget": 1, "glyph": "A"},
    "hearth-reed-boar": {"region": "hearthford", "name": "bristleback reed boar", "profile": "animal", "role": "territorial", "goal": "defend territory", "vision": 6, "hearing": 10, "range": 1, "capability": "telegraphs a charge and bogs in deep mud", "reaction": "circles after a guarded or redirected charge", "morale": 3, "terrain": "reed wallow", "counterplay": "lead it into mud or use controlled light; distance breaks the charge", "budget": 3, "ecology": "territorial", "glyph": "B"},
    "hearth-roof-keeper": {"region": "hearthford", "name": "watch-roof crossbow keeper", "profile": "ranged", "role": "shooter", "goal": "seek elevation", "vision": 13, "hearing": 7, "range": 14, "capability": "uses aligned ladders and a heavy prepared roof lane", "reaction": "reloads twice after a visible telegraphed shot", "morale": 3, "terrain": "watch roof", "counterplay": "break line below the roof or climb through the aligned opening; smoke interrupts aim", "budget": 4, "ranged_kind": "heavy crossbow", "glyph": "C"},
    "hearth-mill-protector": {"region": "hearthford", "name": "displaced mill levy", "profile": "reach", "role": "protector", "goal": "protect ally", "vision": 8, "hearing": 9, "range": 2, "capability": "interposes for the gantry carrier and covers a wounded withdrawal", "reaction": "abandons pursuit when its paired carrier needs cover", "morale": 4, "terrain": "mill threshold", "counterplay": "separate it with a hook or take the culvert loop; witnessed mill terms can end the levy", "budget": 3, "glyph": "G"},
    "hearth-gantry-suppressor": {"region": "hearthford", "name": "gantry sling carrier", "profile": "ranged", "role": "suppressor", "goal": "deny area", "vision": 10, "hearing": 10, "range": 8, "capability": "marks a missile lane and feeds smoke through a bounded gantry vent", "reaction": "holds cover while its paired levy withdraws", "morale": 2, "terrain": "upper gantry", "counterplay": "move before release or climb under the lane; wind and water clear its smoke", "budget": 3, "ranged_kind": "sling", "glyph": "M"},
    "hearth-cargo-reaver": {"region": "hearthford", "name": "valuable-seeking river reaver", "profile": "reach", "role": "thief", "goal": "steal cargo", "vision": 7, "hearing": 12, "range": 2, "capability": "takes one physical valuable and escapes toward a named home route", "reaction": "drops the exact stolen item if intercepted", "morale": 4, "terrain": "flood meadow", "counterplay": "carry fewer exposed valuables or block its homeward line; morale loss forces retreat", "budget": 4, "ecology": "raider", "glyph": "T"},
    "hearth-elite-claimant": {"region": "hearthford", "name": "floodgate claimant", "profile": "reach", "role": "elite", "goal": "open a disputed sluice", "vision": 9, "hearing": 10, "range": 3, "capability": "telegraphs and floods a three-cell mill crossing", "morale": 4, "terrain": "mill race", "counterplay": "dog the sluice, use the upper gantry, or establish the public compact", "budget": 6, "elite": True},
}

# The older authored regional actors predate per-archetype glyph data. They
# retain their established behavior while gaining unique ASCII fallbacks; the
# renderer still assigns semantic hostile colour and bold independent of glyph.
LEGACY_STANDARD_GLYPHS = {
    "coast-tide-runner": "a", "coast-dune-hunter": "c",
    "coast-wreck-shield": "d", "coast-salt-slinger": "e",
    "coast-netter": "f", "coast-lookout": "i",
    "forest-trail-watch": "j", "forest-resin-hunter": "k",
    "forest-hook": "l", "forest-smoke-tender": "n",
    "forest-tusker": "o", "forest-pack-runner": "p",
    "upland-ridge-slinger": "r", "upland-pike": "u",
    "upland-scree-runner": "v", "upland-lime-tender": "w",
    "upland-cave-hound": "y", "upland-alarm-climber": "9",
}
for identity, glyph in LEGACY_STANDARD_GLYPHS.items():
    ENEMY_ARCHETYPES[identity]["glyph"] = glyph

# Named work and wildlife combinations. Behaviour lives in the bounded ecology
# and combat reducers; these rows contain no executable scripts.
FRONTIER_ACTORS = (
    ("fen-pail", "dunmire", "peat-bank pail keeper", "pursuer", "protector", "quench", "worker", 8, 8, 1, 2, "P", "puts out visible fire with three pails", "draw away from water work or use smoke and a side causeway"),
    ("fen-cinder", "dunmire", "cut-bank cinder thrower", "ranged", "suppressor", "kindle", "raider", 9, 8, 8, 3, "F", "ignites a warned resin feed, then uses a sling lane", "wet the feed, interrupt preparation, or close its sightline"),
    ("fen-lynx", "dunmire", "reedbank lynx", "animal", "tracker", "hunt", "predator", 6, 13, 1, 2, "Y", "hunts exposed fen hares and tracks noise into reeds", "lead it toward prey or break sight beyond its reed shelter"),
    ("fen-hare", "dunmire", "long-eared fen hare", "animal", "territorial", "", "prey", 9, 7, 1, 1, "H", "flees visible hunters, moving the lynx's attention", "keep distance to watch it or use its flight as a distraction"),
    ("fen-bracer", "dunmire", "raised-bank brace ward", "reach", "protector", "brace", "warden", 8, 7, 2, 3, "D", "repairs warned supports before guarding the causeway", "draw it to a damaged support or approach from the drain"),
    ("fen-recoverer", "dunmire", "reed-pack recoverer", "pursuer", "thief", "scavenge", "raider", 7, 11, 1, 2, "K", "retrieves visible dropped equipment and leaves by its home route", "secure dropped goods or intercept its loaded retreat"),
    ("gorge-cutter", "rillscar", "scaffold cord cutter", "pursuer", "flanker", "cut support", "raider", 9, 8, 1, 2, "J", "cuts warned floor support, then takes a side approach", "brace the marked support or disrupt the cutting stance"),
    ("gorge-escort", "rillscar", "two-bridge escort", "reach", "protector", "escort", "warden", 10, 8, 2, 3, "E", "stays with visible injured carriers and opposes cargo raiders", "separate the escort from its ally or use the other bridge"),
    ("gorge-surgeon", "rillscar", "cutworks field surgeon", "pursuer", "protector", "heal", "worker", 8, 7, 1, 2, "I", "uses two dressings on observed wounded allies", "interrupt the approach or force its supplies to be spent"),
    ("gorge-marmot", "rillscar", "shelf marmot", "animal", "territorial", "", "prey", 7, 12, 1, 1, "U", "warns of hunters by abandoning an exposed rock shelf", "observe at a distance or follow the direction of its flight"),
    ("gorge-cat", "rillscar", "scree hunting cat", "animal", "tracker", "hunt", "predator", 13, 4, 1, 2, "N", "hunts across scree without losing footing; weak hearing", "use a solid sight break or put prey between it and the route"),
    ("gorge-caller", "rillscar", "cliff signal caller", "pursuer", "lookout", "rally", "warden", 12, 7, 1, 1, "L", "spends two signals on wavering allies and raises a group alarm", "break the signal lane or force a withdrawal before reinforcement"),
    ("terrace-drainer", "marlbank", "field ditch keeper", "pursuer", "protector", "drain", "worker", 8, 8, 1, 2, "Q", "drains observed deep patches using three stop boards", "draw toward a flooded side field or travel along the dry bank"),
    ("terrace-kiln", "marlbank", "kiln mouth sling ward", "ranged", "suppressor", "kindle", "warden", 9, 6, 8, 3, "S", "feeds a warned kiln mouth and denies the firing lane", "wet the marked fuel or pass crosswind behind the kiln"),
    ("terrace-hare", "marlbank", "seed-field hare", "animal", "territorial", "", "prey", 10, 8, 1, 1, "V", "crosses cultivated rows while fleeing visible orchard hunters", "leave it quiet or exploit its flight to draw the hunter"),
    ("terrace-thief", "marlbank", "seed-sack taker", "pursuer", "thief", "scavenge", "raider", 8, 7, 1, 2, "W", "collects abandoned seed sacks and escapes through irrigation paths", "block its escape or make the sack too risky to retrieve"),
    ("terrace-netter", "marlbank", "harvest net escort", "reach", "controller", "escort", "warden", 7, 10, 2, 3, "Z", "escorts distressed carriers and telegraphs a restraining lane", "step out of the marked net or separate escort and carrier"),
    ("terrace-fox", "marlbank", "orchard fox", "animal", "tracker", "hunt", "predator", 7, 12, 1, 2, "2", "hunts seed-field hares around rows of dense growth", "draw it onto prey or use the irrigation sight breaks"),
    ("estuary-drainer", "frostmere", "thaw-board runner", "pursuer", "flanker", "drain", "worker", 9, 8, 1, 2, "3", "drains standing thaw water before taking a side route", "use frozen ground or divert it toward deeper water"),
    ("estuary-caller", "frostmere", "shelter line caller", "pursuer", "lookout", "rally", "warden", 13, 7, 1, 1, "4", "signals distant allies; weak hearing in shelter", "break the visible lane or approach through crosswind cover"),
    ("estuary-hunter", "frostmere", "grey channel hunter", "animal", "tracker", "hunt", "predator", 12, 5, 1, 2, "5", "hunts snow hares over dry gravel and seasonal ice", "thaw a crossing or distract it with fleeing prey"),
    ("estuary-hare", "frostmere", "gravel snow hare", "animal", "territorial", "", "prey", 11, 6, 1, 1, "6", "flees toward its gravel shelter, exposing the hunter's route", "watch from beyond its sight or draw the hunter across the braid"),
    ("estuary-escort", "frostmere", "net-loft withdrawing bow", "ranged", "skirmisher", "escort", "raider", 12, 7, 12, 3, "7", "covers a loaded ally and shoots while withdrawing", "cut its sightline or take cover while closing the useful range"),
    ("estuary-mender", "frostmere", "cold-net field mender", "pursuer", "protector", "heal", "worker", 7, 10, 1, 2, "8", "uses two dressings before returning to shelter", "force separate injuries or deny the visible treatment route"),
)
for identity, region, name, profile, role, duty, ecology, vision, hearing, reach, budget, glyph, capability, counterplay in FRONTIER_ACTORS:
    ENEMY_ARCHETYPES[identity] = {
        "region": region, "name": name, "profile": profile, "role": role,
        "goal": duty or "feed within shelter", "vision": vision, "hearing": hearing,
        "range": reach, "capability": capability, "morale": 3 if ecology == "warden" else 2,
        "terrain": region, "counterplay": counterplay, "budget": budget,
        "ecology": ecology, "duty": duty, "glyph": glyph,
        "supplies": 2 if duty in {"heal", "rally"} else 3 if duty in {"brace", "drain", "quench", "kindle", "cut support"} else 0,
        "ranged_kind": {
            "dunmire": "sling", "rillscar": "crossbow",
            "marlbank": "sling", "frostmere": "longbow",
        }.get(region, "sling"),
    }

# Three further authored roles per region. They use the same bounded ecology,
# equipment, perception and material-duty reducers as the retained roster.
EXPANDED_STANDARD_ACTORS = (
    ("hearth-sluice-runner", "hearthford", "sluice pail runner", "pursuer", "protector", "quench", "worker", 8, 9, 1, 2, "b", "spends three pails on fire threatening a public bank", "draw it away from the bank or exhaust its visible pails"),
    ("hearth-rope-cutter", "hearthford", "ferry-rope cutter", "pursuer", "flanker", "cut support", "raider", 9, 8, 1, 2, "g", "warns before cutting a ferry brace and then takes the meadow flank", "brace the marked rope or interrupt the cutter from the bank loop"),
    ("hearth-meadow-kite", "hearthford", "flood-meadow kite", "animal", "tracker", "scavenge", "scavenger", 12, 5, 1, 1, "h", "descends on a visible dropped food or light tool", "secure dropped goods or use the descent to reveal the meadow patrol"),
    ("coast-wreck-gull", "greywash", "storm-wreck gull", "animal", "tracker", "scavenge", "scavenger", 11, 6, 1, 1, "m", "takes exposed light salvage from the ebb road", "pack the salvage or let its flight expose the landward gully"),
    ("coast-chain-mender", "greywash", "ebb-chain mender", "pursuer", "protector", "brace", "worker", 8, 10, 1, 2, "q", "repairs a warned wreck-chain support before covering withdrawal", "damage a different support or separate the mender from its ward"),
    ("coast-brine-pourer", "greywash", "brine-sluice pourer", "reach", "controller", "drain", "warden", 9, 8, 2, 3, "s", "drains one deep flat before holding its narrow outlet", "freeze the shallow cut or approach over fixed dune cover"),
    ("forest-cinder-crow", "greenwold", "cinder-feeding crow", "animal", "tracker", "scavenge", "scavenger", 10, 9, 1, 1, "t", "carries small abandoned objects from cooling burn ground", "secure the object or follow its flight toward the ash cache"),
    ("forest-well-runner", "greenwold", "coppice well runner", "pursuer", "protector", "quench", "worker", 7, 12, 1, 2, "x", "quenches a watched medicine boundary with finite buckets", "draw the runner downwind or spend its buckets on a decoy fire"),
    ("forest-coppice-escort", "greenwold", "charcoal sledge escort", "reach", "protector", "escort", "warden", 9, 9, 2, 3, "z", "keeps beside an injured or loaded charcoal carrier", "pull the escort from the sledge or cross through wet coppice"),
    ("upland-lantern-taker", "whitecairn", "quarry-lamp taker", "pursuer", "thief", "scavenge", "raider", 8, 11, 1, 2, "O", "retrieves dropped lamps and tools before climbing for shelter", "keep light in the pack or block the nearest aligned stair"),
    ("upland-face-bracer", "whitecairn", "moving-face brace hand", "reach", "protector", "brace", "worker", 10, 8, 2, 3, "R", "seats a warned quarry support before holding two-pace measure", "force work at a second crack or enter inside the brace"),
    ("upland-crag-goat", "whitecairn", "bell-shelf crag goat", "animal", "territorial", "feed", "prey", 12, 7, 1, 1, "X", "flees a disturbed shelf and exposes the hunter's upper path", "watch from below or use its flight to draw the cave hound"),
    ("fen-peat-raker", "dunmire", "dry-peat spark raker", "pursuer", "flanker", "kindle", "raider", 8, 10, 1, 2, "!", "warns before feeding dry peat then circles by a wet cut", "wet the marked peat or interrupt from the raised walk"),
    ("fen-board-mender", "dunmire", "bog-island board mender", "reach", "protector", "brace", "worker", 9, 8, 2, 3, "$", "repairs a failing inhabited walk before defending it", "damage an empty branch or approach through the drain"),
    ("fen-marsh-harrier", "dunmire", "low-reed marsh harrier", "animal", "tracker", "hunt", "predator", 11, 5, 1, 2, "&", "hunts disturbed fen hares across open water margins", "lead it toward sheltered prey or disappear into dense reeds"),
    ("gorge-hoist-thief", "rillscar", "dropped-hoist tackle thief", "pursuer", "thief", "scavenge", "raider", 9, 9, 1, 2, "'", "retrieves a dropped tool then escapes by the lower bridge", "secure the tackle or close the lower switchback"),
    ("gorge-lime-bracer", "rillscar", "lime-cut scaffold bracer", "reach", "protector", "brace", "worker", 8, 10, 2, 3, "(", "repairs a warned scaffold while lime smoke obscures the lane", "clear the smoke or threaten a second unsupported bay"),
    ("gorge-raven", "rillscar", "cutwall raven", "animal", "tracker", "scavenge", "scavenger", 13, 4, 1, 1, ")", "takes a light dropped object to an elevated ledge", "secure the object or use the ledge flight as a cache clue"),
    ("terrace-rill-dogger", "marlbank", "seed-rill stop-board hand", "pursuer", "protector", "drain", "worker", 9, 7, 1, 2, "*", "drains one flooded seed row before guarding its board", "redirect water to another row or approach through the kiln lee"),
    ("terrace-ash-thief", "marlbank", "fired-clay tally thief", "pursuer", "thief", "scavenge", "raider", 7, 12, 1, 2, "-", "takes a dropped account or tool through irrigation cover", "keep the account packed or close the dry terrace exit"),
    ("terrace-rook", "marlbank", "kiln-field rook", "animal", "tracker", "scavenge", "scavenger", 10, 8, 1, 1, "/", "collects light objects from cooling kiln margins", "secure the object or follow it toward a hidden firing shelf"),
    ("estuary-ice-bracer", "frostmere", "thaw-channel ice bracer", "reach", "protector", "brace", "worker", 9, 9, 2, 3, "0", "seats a marked channel stake before holding the crossing", "thaw the alternate braid or enter inside its long brace"),
    ("estuary-wreck-taker", "frostmere", "drift-store wreck taker", "pursuer", "thief", "scavenge", "raider", 8, 10, 1, 2, ":", "retrieves exposed winter stores and flees over firm gravel", "secure the store or block the gravel shelter route"),
    ("estuary-ice-otter", "frostmere", "ice-channel otter", "animal", "territorial", "hunt", "predator", 9, 11, 1, 2, "?", "hunts fleeing gravel hares but leaves deep salted current", "draw it after prey or break contact across a brined opening"),
)
for identity, region, name, profile, role, duty, ecology, vision, hearing, reach, budget, glyph, capability, counterplay in EXPANDED_STANDARD_ACTORS:
    ENEMY_ARCHETYPES[identity] = {
        "region": region, "name": name, "profile": profile, "role": role,
        "goal": f"{duty or 'hold'} {identity}", "vision": vision,
        "hearing": hearing, "range": reach, "capability": capability,
        "morale": 3 if role == "protector" else 2, "terrain": region,
        "counterplay": counterplay, "budget": budget, "ecology": ecology,
        "duty": "" if duty == "feed" else duty, "glyph": glyph,
        "supplies": 3 if duty in {"quench", "brace", "drain", "kindle", "cut support"} else 0,
        "ranged_kind": "sling",
    }

from .frontier_elites import ELITE_DEFINITIONS

ENEMY_ARCHETYPES.update(ELITE_DEFINITIONS)

STANDARD_REACTIONS = {
    "lookout": "returns to its patrol after an interrupted alarm",
    "flanker": "changes to a side route when the direct lane is held",
    "protector": "covers a wounded ally before resuming pursuit",
    "shooter": "seeks elevation or reloads after a telegraphed shot",
    "skirmisher": "withdraws when the courier enters its preferred range",
    "suppressor": "marks a lane and feeds smoke before direct pressure",
    "territorial": "returns home when prey or courier leaves its boundary",
    "thief": "escapes toward home after taking one physical item",
    "tracker": "investigates sound or prey without learning an unseen courier position",
    "controller": "recovers a missed marked restraint before casting again",
}
for data in ENEMY_ARCHETYPES.values():
    if not data.get("elite"):
        data.setdefault("reaction", STANDARD_REACTIONS[str(data["role"])])

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
    ";: move a zero-time look cursor; mouse click also inspects a map cell without acting.",
    "O: observe visible people, wildlife, duties, supplies and tactical counters (free).",
    "F: inspect and handle nearby material; choices commit one action, Escape costs none.",
    "Z: regional work account, linked history, testimony and action-clock forecast (free).",
    "Move with arrows, HJKL, or YUBN diagonals. The camera follows across each seamless region.",
    "T follows a seen local destination one ordinary step at a time; any key stops it.",
    "Enter/E interacts. A previews and targets any attack. G guards, reloads, or holds to listen.",
    "Map: @ courier, a ally, v visitor, c/M contacts, h/g/x/b hostiles, X elite, R cargo, C cache.",
    "Reverse ! cells mark an observed prepared attack; @ is reversed when standing in its mark.",
    "Water ~, wall #, doors +, stairs <> and roof ^, controls &, hole O, weak floor d.",
    "Aboard, speak beside people to switch or recruit; tavern C selects support; chart P sets course.",
    "I opens the spatial pack/locker: move, rotate, transfer, equip, confirm, or cancel safely.",
    "Pressure is elapsed actions + geographic depth + noise + valuables; all remain visible.",
    "Normal colour is visible now; dim terrain is remembered; blank terrain remains unknown.",
    "Guard readable intent; smoke breaks aim; mud, water, controls, height, and walls matter.",
    "Q asks before quitting. Escape closes or backs out of a popup.",
)
