#!/usr/bin/env python3
"""Build the offline artwork reference. Development-only dependency: Pillow.

Run from any directory with a Python environment that has Pillow installed.
No network requests; original artwork and the file inventory are never rewritten.
"""
from __future__ import annotations

import hashlib
import json
import re
from collections import Counter
from pathlib import Path

from PIL import Image
from catalog_io import load_catalog

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "assets"
REFERENCE = ASSETS / "reference"
GUIDES = {25, 51, 77, 103}

# These are design proposals, not runtime bindings. An empty first element means
# there is no specific existing Regolith feature to assign the artwork to.
ICON_USES = {
    "animation": ([], "Future animated machine or crew preview; show idle/work frames in an inspector."),
    "area_damage": (["meteor impact / fire danger"], "Preview a meteor damage footprint or unsafe rescue area."),
    "area_meteo": (["solar storm warning"], "A forecast panel for radiation and meteor activity."),
    "audio": ([], "Future audio settings and alert-volume control."),
    "bag": (["metal / water / food / crystal stockpile"], "A future carried rescue-kit or cargo inventory."),
    "beetle": ([], "A maintenance microbot, or a contained biology experiment; no wild lunar insects exist in the simulation."),
    "bell": (["event log / critical alerts"], "Acknowledge low-oxygen and stranded-survivor notifications."),
    "bone": (["colonist death"], "A memorial marker or archaeological specimen in an optional scenario."),
    "boots": (["rescue travel / crew movement"], "Show travel time, walking speed, or suit mobility upgrades."),
    "brain": (["crew morale"], "Future skills, research, or stress-management UI."),
    "brush": ([], "Future room paint, colony colors, or cosmetic designation tool."),
    "brush_2": ([], "An alternate paint/designation tool or larger area brush."),
    "bucket": (["water stockpile"], "Future water hauling or coolant refill task."),
    "bug": ([], "Developer diagnostics, or an explicitly fictional bio-containment scenario."),
    "bullet": ([], "Future industrial fastener/ammunition inventory; combat is not implemented."),
    "call": (["Earth uplink / distress signal"], "An incoming distress-call or communications contact button."),
    "camera_grid": (["map view / cursor"], "Future camera bounds, screenshot framing, or survey targeting."),
    "canon": ([], "Future meteor-defense emplacement or industrial mass driver; filename spelling retained."),
    "canon_2": ([], "A second mass-driver/defense emplacement style; no weapon simulation currently exists."),
    "card": ([], "Future colonist identity card or access-control panel."),
    "case": ([], "A rescue-kit container or specialist's portable tool case."),
    "cell": (["cellular atmosphere / temperature overlays"], "A per-cell environmental inspection button."),
    "character": (["colonist / crew roster"], "A crew portrait or selected-colonist indicator."),
    "character_2": (["colonist / crew roster"], "An alternate portrait or survivor roster badge."),
    "chest": (["resource stockpile"], "Future item storage, with visible capacity or contents."),
    "circle": ([], "Future circular selection, radius preview, or legend marker."),
    "clear": (["cancel designation"], "Clear a selection or remove a queued work order."),
    "coin": ([], "Future trade credit or salvage value; the current game has no currency."),
    "color_correction": ([], "Future overlay palette or accessibility color settings."),
    "crate": (["resource stockpile / salvage"], "Future landed cargo or cached expedition supplies."),
    "crate_02": (["resource stockpile / salvage"], "An alternate cargo crate or reserved construction materials."),
    "damage_team": (["injured colonist / rescue"], "A group injury alert; team combat would require a new system."),
    "destroyable_2": (["salvage designation / damaged machine"], "Mark a structure for dismantling or repair."),
    "dialog": (["tutorial / survivor details"], "Future crew conversations or rescue debriefs."),
    "dice": (["world seed"], "A new-colony random-seed button or optional incident roll preview."),
    "disable": (["unpowered machine / unavailable action"], "Future machine enable/disable toggle."),
    "door": (["airlock"], "A seal/door-state indicator in a graphical inspector."),
    "drag": (["region designation / map navigation"], "Teach mouse dragging or show a future pan gesture."),
    "drop": ([], "Future cargo drop, hauling destination, or landing-point action."),
    "duplicate": ([], "Future repeated blueprint stamping or copy-room tool."),
    "emote": (["crew morale"], "Future readable colonist thoughts or reactions."),
    "event": (["event log"], "A timeline entry or critical incident marker."),
    "expand": (["zoom / map view"], "Future fit-colony or fullscreen map button."),
    "face": (["colonist details / morale"], "An expressive crew portrait or mood badge."),
    "file": (["save / load"], "Future save-slot details or exported colony report."),
    "flag": (["survey designation / founding charter"], "A future waypoint or landing-site flag."),
    "folder": (["load saved colony"], "A future save browser or colony archive."),
    "follow": (["follow selected colonist"], "A tracking-camera state indicator."),
    "follower": (["escort stranded survivor"], "Show the survivor following a rescue crew member."),
    "freeze": (["cold / temperature overlay"], "Future frost damage or cryogenic storage indicator."),
    "gear": (["machinery / build menu"], "Future colony settings or maintenance task."),
    "gear_2": (["machinery / build menu"], "An alternate machine-status or maintenance badge."),
    "gem": (["silicate crystal resource"], "Future refined electronics or crystal-grade display."),
    "glow": ([], "Future active-object highlight or lamp status; decorative glow is not an oxygen indicator."),
    "graph": (["power / oxygen / temperature statistics"], "Future history charts for life support and food production."),
    "grid": (["cell map / region designation"], "A future grid-visibility toggle."),
    "hammer": (["build designation"], "Blueprint toolbar and construction progress badge."),
    "hand": (["select / inspect / click survivor"], "A contextual interaction cursor."),
    "hat": (["crew profession"], "Future job clothing or specialist equipment indicator."),
    "heart": (["colonist health"], "A health warning or medical triage badge."),
    "heart_receptacle": (["colonist health"], "Future maximum-health or healing-capacity display."),
    "height": ([], "Future elevation, excavation depth, or terrain-height overlay."),
    "hitbox": ([], "Developer collision debug view or future construction footprint editor."),
    "human_controller": ([], "Future direct crew control or controller setup; crew are autonomous today."),
    "interrogation": (["stranded survivor / survey / help"], "A distress marker or unexplored-point-of-interest badge."),
    "key": ([], "Future airlock access permissions or locked derelict cache."),
    "lever": ([], "Future manual power isolator or emergency airlock override."),
    "life_bar": (["colonist health / suit oxygen"], "A compact HUD meter; label the measured quantity explicitly."),
    "light_bulb": (["power status"], "Future lighting network or research discovery notice."),
    "liquid": (["water stockpile / life support"], "Future coolant and water-network controls."),
    "list": (["crew roster / work orders"], "A future searchable task queue."),
    "loader": (["work progress / rescue progress"], "Future loading-screen or long-action indicator."),
    "lock": (["unavailable blueprint / charter requirement"], "Future room access or protected-object state."),
    "magnifier": (["zoom / inspect tile"], "Zoom toolbar and detailed cell inspection."),
    "map": (["colony map"], "Future sector overview or world-generation preview."),
    "map_2": (["colony map"], "An alternate overview-map button."),
    "meat": (["food stockpile / hunger"], "A ration icon; livestock and meat production are not current mechanics."),
    "meteo": (["solar storm / meteor warning"], "A future environmental forecast widget."),
    "minimap": (["map navigation"], "A future whole-colony minimap with distress pings."),
    "mirror": ([], "Future blueprint reflection or room-layout editing."),
    "money_bag": ([], "Future trade credits or rescue-contract rewards; no current economy."),
    "move": (["cursor / crew movement"], "A movement hint or future haul/reposition tool."),
    "next": (["tutorial pages / next distress signal"], "Advance a tutorial or cycle selected survivors."),
    "parchment": (["mission charter / tutorial"], "A colony charter document or future research report."),
    "particle": (["fire / meteor effects"], "Future dust, sparks, venting oxygen, or machine activity effects."),
    "path_follow": (["rescue route / autonomous travel"], "Preview a colonist's route and escort destination."),
    "paused": (["pause simulation"], "Pause toolbar and paused-state badge."),
    "play": (["run simulation"], "Resume toolbar and running-state badge."),
    "play_bar": (["simulation speed / single-step"], "Future replay transport or tick-step controls."),
    "pot": (["food / hydroponics"], "Future meal preparation or greenhouse planter."),
    "potion": ([], "Future medicine or oxygen-canister consumable; no magic system exists."),
    "projectile": (["meteor event"], "A falling meteor visual; weapons would be a new mechanic."),
    "propeller": ([], "Future pressure fan or ventilation machinery; no airborne lunar flight assumed."),
    "puzzle": ([], "Future research puzzle, modular-system connection, or damaged equipment diagnosis."),
    "receptacle": ([], "Future equipment socket, inventory slot, or machine input bay."),
    "reset": (["reset map zoom"], "Future reset-view or restart-dialog control."),
    "reset_2": (["reset map zoom"], "A compact reset-view icon; this source is 10x12 rather than 16x16."),
    "ring": ([], "Future personal keepsake, morale item, or equipment slot."),
    "robot": ([], "Future maintenance drone or salvageable robot; current colonists are people."),
    "save": (["save colony"], "Save button and successful-save notice."),
    "scene": ([], "Future scenario selection or colony screenshot gallery."),
    "screen_effect": ([], "Future radiation/damage vignette toggle or display settings."),
    "scroll": (["tutorial / event log"], "Future research notes or blueprint description."),
    "search": (["survey / inspect"], "Future crew, resource, or work-order search."),
    "shape": ([], "Future designation shape chooser or room footprint preview."),
    "shield": (["suit protection / storm shelter"], "Future shielding quality or radiation protection meter."),
    "ship": ([], "Future crashed lander, orbital supply vessel, or evacuation target."),
    "shovel": (["mine / salvage designation"], "Mining toolbar and geology work-order marker."),
    "signal": (["distress signal / Earth uplink"], "Show radio contact strength or rescue destination."),
    "skull": (["dead colonist / lethal hazard"], "Memorials and fatal atmosphere warnings."),
    "sound": ([], "Future sound-effect mute toggle or audible distress beacon."),
    "spiral": ([], "Future radiation anomaly, research subject, or decorative science badge."),
    "square": (["rectangular designation"], "A selection-area or construction-footprint indicator."),
    "star": (["founding charter completion"], "Future achievements or highlighted crew skills."),
    "stat": (["colony / colonist statistics"], "Future detailed production and survival reports."),
    "sword": ([], "Future combat or cutting-tool inventory; no combat system currently exists."),
    "target": (["cursor / designate work"], "A precise placement reticle or selected rescue target."),
    "target_2": (["cursor / designate work"], "An alternate selected-object or survey reticle."),
    "teleporter": ([], "Future fictional transport experiment; current rescues require walking and escorting."),
    "text_panel": (["tutorial / log / inspector"], "Future message panel or contextual explanation button."),
    "thunder": (["power / battery network"], "Electricity overlay and power failure alerts."),
    "time": (["cycle clock / rescue oxygen deadline"], "Countdowns, travel-time estimates, and work duration."),
    "timeline": (["cycle / event log"], "Future chronological colony history or incident replay."),
    "tower": (["Earth uplink"], "Future communications relay or remote rescue beacon."),
    "transition": ([], "Future tutorial transitions or scenario loading indicator."),
    "trap": ([], "Future environmental hazard designation or mining safety warning; traps are not implemented."),
    "tree": (["hydroponics / food"], "Future sealed arboretum or greenhouse variety; never exposed lunar vegetation."),
    "triangle": ([], "Future warning badge, directional marker, or selection shape."),
    "trophy": (["founding charter completion"], "Future survival milestones and achievements."),
    "trophy_2": (["founding charter completion"], "A second achievement/colony milestone style."),
    "unlock": (["met charter requirement / available action"], "Future access granted or discovered blueprint."),
    "visibility": (["discovered cells / overlays"], "Future fog-of-war or object-visibility toggle."),
    "visibility_off": (["undiscovered cells / hidden overlay"], "Future hidden-object filter or fog-of-war legend."),
    "wall": (["pressure hull"], "Build-menu hull icon or breached-hull alert."),
    "weapon": ([], "Future combat loadout or industrial cutting tool; no current weapon mechanic."),
    "wheel": ([], "Future rover transport, hauling cart, or machine transmission."),
    "wind": (["decompression / atmosphere flow"], "Show venting gas; this represents habitat gas flow, not lunar weather."),
}

# group1 from the historical author data -> current-feature proposal and an
# additional use. Unmapped groups still receive an explicit future idea below.
TILE_USES = {
    "wall": (["pressure hull / basalt"], "Use intact, broken, and patterned variants for hull damage or excavated rock; verify adjacency art visually."),
    "door": (["cycling airlock"], "Show sealed/open/cycling airlock art without changing the pressure rules."),
    "hatch": (["cycling airlock"], "Future vertical access hatch or maintenance-panel entrance."),
    "floor": (["habitat deck / lunar surface"], "Use deck texture for powered habitat cells and rough texture for regolith; decorative markings must not imply a separate network."),
    "mineral": (["silicate crystal / iron deposit / water ice"], "Choose a mineral silhouette and recolor distinct resource variants; the original mineral name need not be lunar geology."),
    "material": (["metal / crystal stockpile"], "Future physical resource stacks or construction material delivery."),
    "tool": (["mining / building / salvage"], "Future held tools, job badges, or equipment wear; the current simulation does not track individual tools."),
    "food": (["food stockpile / hunger"], "Future meal varieties or ration preferences; current food is a single aggregate resource."),
    "cannedfood": (["food stockpile / rescue kit"], "Future sealed emergency rations or salvage-cache contents."),
    "firstaid": (["med station / injured survivor"], "Future carried rescue medicine or triage supplies."),
    "patch": (["med station / health"], "Future wound dressing or suit leak repair consumable."),
    "pill": (["med station / health"], "Future medicine inventory or treatment progress indicator."),
    "pillsjar": (["med station / health"], "Future medical supply cabinet or medicine stockpile."),
    "syringe": (["med station / health"], "Future treatment supplies or a medic work badge."),
    "battery": (["battery bank / energy storage"], "Different silhouettes can represent charge level or damaged storage."),
    "accumulator": (["battery bank"], "Future battery capacity upgrade or portable emergency power."),
    "radiotower": (["Earth uplink / distress beacon"], "Future remote relay, direction finder, or rescue navigation tower."),
    "powerstation": (["radioisotope generator / solar network"], "A generator silhouette; no claim that the drawing depicts an RTG physically."),
    "transformer": (["power network"], "Future distribution switchgear or a power-network junction."),
    "electricpole": (["power overlay"], "Future external cable network; current power travels through connected deck and airlock cells."),
    "wire": (["power overlay"], "Future external cabling or decorative deck conduit."),
    "farm": (["hydroponics / food production"], "Future crop variants inside a pressurized greenhouse."),
    "flora": (["hydroponics"], "Future sealed plant nursery, research specimens, or crop-growth stages; not surface vegetation."),
    "bonfire": (["fire hazard"], "A visual flame overlay, kept separate from oxygen and heat cell values."),
    "fire": (["fire hazard"], "Future animated flame states or hot machinery warning."),
    "effect": (["fire / meteor / decompression effects"], "Future dust, sparks, venting atmosphere, or radiation event indicators."),
    "player": (["colonist / stranded survivor"], "Alternate crew appearance; add a suit/helmet cue for outdoor readability."),
    "npc": (["colonist / stranded survivor"], "Future rescued specialist portrait or suit-wearing variant."),
    "gravestone": (["dead colonist"], "Future memorial garden inside the habitat or a marked grave outside."),
    "bones": (["dead colonist / failed rescue site"], "Future archaeological sample or abandoned expedition remains."),
    "furniture": ([], "Future furnished habitat, sleeping quarters, laboratory, or recreation room; beds may also stand in for current sleeping pods."),
    "bed": (["sleeping pod"], "Future bunk quality or infirmary bed variant."),
    "computer": (["Earth uplink"], "Future research terminal or life-support control console."),
    "monitor": (["Earth uplink / life-support status"], "Future machine screen, diagnostics terminal, or alert panel."),
    "screen": (["Earth uplink / machine status"], "Future animated console content; use as an overlay inside a compatible machine frame."),
    "pda": (["survey / survivor details"], "Future handheld survey scanner or rescue checklist."),
    "science": (["survey / crystal analysis"], "Future laboratory research or atmospheric analysis station."),
    "testtube": (["life support / med station"], "Future water analysis, plant experiments, or medicine production."),
    "light": ([], "Future habitat lighting, with powered/unpowered states distinct from atmospheric safety."),
    "lightbulb": (["power status"], "Future electric light fixture or maintenance indicator."),
    "lamp": ([], "Future work light, emergency lighting, or room fixture."),
    "barrel": (["water stockpile"], "Future water/coolant tanks or sealed material storage."),
    "canister": (["water / suit oxygen"], "Future portable oxygen refill or coolant container."),
    "bucket": (["water stockpile"], "Future water hauling or greenhouse maintenance."),
    "bag": (["rescue supplies"], "Future crew inventory, ration bag, or salvaged cargo."),
    "water": (["water stockpile / ice resource"], "A UI/resource representation; open liquid water is not a current outdoor lunar terrain."),
    "cog": (["machinery / build work"], "Future spare parts, repairs, or machine condition indicator."),
    "toolbox": (["build / salvage work"], "Future repair kit or engineer equipment."),
    "calendar": (["cycle counter / founding charter"], "Future long-duration colony history or scheduled deliveries."),
    "clock": (["simulation clock / rescue deadline"], "Future task-duration or life-support countdown display."),
    "watch": (["simulation clock / rescue deadline"], "Future personal suit timer or EVA countdown."),
    "floppy": (["save colony"], "Future terminal backup or recovered mission data."),
    "keyboard": (["keyboard controls / uplink console"], "Future terminal furniture or control-help illustration."),
    "computermouse": (["mouse controls"], "Future mouse tutorial prompt or computer-terminal prop."),
    "radio": (["distress signal / Earth uplink"], "Future crew radio, communications room, or abandoned transmitter."),
    "phone": (["distress signal"], "Future communication handset or incoming rescue call."),
    "cellphone": (["distress signal"], "Future crew communicator or recovered mission log."),
    "smartphone": (["survey / distress signal"], "Future handheld map and radio receiver."),
    "headset": (["distress signal / Earth uplink"], "Future communications officer portrait or radio desk."),
    "headphones": ([], "Future recreation item or audio settings icon."),
    "sign": (["warning / survey marker"], "Future hazard labels and room names."),
    "roadsign": ([], "Future EVA route signs, traffic markers for rovers, or landing-zone navigation."),
    "font": (["graphical HUD / tutorial text"], "A future bitmap font; glyph identity comes from the matching author metadata, not OCR."),
    "enemy": ([], "Optional fictional threat, training-simulator target, or mascot; enemies do not exist in the lunar survival simulation."),
    "animal": ([], "Future sealed livestock/research specimen or crew mascot; no wildlife is assumed on the lunar surface."),
    "weapon": ([], "Future combat gear, museum display, or salvageable metal; current rescue and mining do not require weapons."),
    "bullet": ([], "Future ammunition or industrial fastener inventory; no current combat system."),
    "explosive": ([], "Future controlled excavation charge with explicit safety rules, or hazardous salvage."),
    "armor": ([], "Future EVA suit armor or museum costume; the sprite does not by itself depict a pressure-safe suit."),
    "bodyarmor": ([], "Future suit protection upgrade or salvage equipment."),
    "helmet": ([], "Future EVA helmet icon or crew outfit; a historical helmet is not a pressure helmet without a redraw."),
    "shield": ([], "Future radiation shielding plate or optional training gear."),
    "clothes": ([], "Future crew workwear, suit layers, or personal belongings."),
    "transport": ([], "Future lunar rover, grounded wreck, or supply vehicle; reskin terrestrial vehicles and keep traversal rules explicit."),
    "ship": ([], "Future grounded lander, crashed vessel, or orbital-supply icon."),
    "building": ([], "Future sector-map habitat, abandoned facility, or exterior landmark; not a drop-in one-cell machine sprite."),
    "multistory": ([], "Future derelict research tower or sector-map colony skyline; habitat interiors would need separate art."),
    "factory": ([], "Future refinery, fabrication plant, or abandoned industrial salvage site."),
    "district": ([], "Future sector-map zone, colony district, or landing-site landmark."),
    "overworld": ([], "Future expedition/sector-map landmark; adapt terrestrial landforms to lunar geography rather than assuming oceans or forests."),
    "road": ([], "Future rover road or marked EVA walkway, with connected edges chosen deliberately."),
    "railway": ([], "Future material-hauling rail or abandoned mining railway; no transport rail simulation exists today."),
    "street": ([], "Future landing-pad deck, service corridor, or colony exterior infrastructure."),
    "stockade": ([], "Future safety railing or fenced equipment enclosure inside a habitat."),
    "river": ([], "Future sealed coolant channel or greenhouse waterway; not exposed liquid on the Moon."),
    "bridge": ([], "Future gantry across a pit or elevated service crossing."),
    "footbridge": ([], "Future interior maintenance catwalk or excavation crossing."),
    "cave": (["basalt excavation / undiscovered geology"], "Future lava-tube entrance or underground expedition site."),
    "stairs": ([], "Future multilevel habitat, lava-tube descent, or height-change marker."),
    "column": ([], "Future habitat support column or derelict facility interior."),
    "awning": ([], "Future radiation shade or equipment cover, after a lunar materials redraw."),
    "streetlamp": ([], "Future exterior work light or rover-route beacon."),
    "gasstation": ([], "Future rover charging/refueling station or abandoned coolant depot."),
    "dining": ([], "Future mess hall, meal service, or crew recreation props."),
    "pot": ([], "Future galley vessel, greenhouse planter, or cooking equipment."),
    "potion": ([], "Future colored medicine or laboratory sample; no magic mechanics implied."),
    "scroll": ([], "Future recovered research note or mission document; replace parchment styling if desired."),
    "book": ([], "Future research manual, crew journal, or recreation item."),
    "magic": ([], "Future science-fiction anomaly effect or simulation minigame; magic is outside the current game."),
    "ring": ([], "Future keepsake or morale item from a rescued colonist."),
    "necklace": ([], "Future personal keepsake or recovered identity tag."),
    "banknote": ([], "Future trade vouchers, supply credits, or a museum prop; there is no current money resource."),
    "money": ([], "Future trade credit or salvage valuation."),
    "key": ([], "Future access token for abandoned airlocks or sealed rescue caches."),
    "accesscard": ([], "Future facility access level, crew ID, or machine authorization card."),
    "chip": ([], "Future electronics resource or upgrade component; current recipes aggregate metal and crystal."),
    "lever": ([], "Future emergency airlock override or power-isolation switch."),
    "trap": ([], "Future mining hazard or safety simulator obstacle; no traps exist in current play."),
    "web": ([], "Future cable tangle, damaged habitat insulation, or contained biology prop."),
    "rope": ([], "Future rescue tether, lifting line, or EVA safety gear."),
    "chain": ([], "Future cargo tie-down, hoist line, or industrial decoration."),
}


def dump(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n")


def pixels(im):
    return list(im.convert("RGBA").get_flattened_data())


def foreground(im):
    # The author acknowledges near-black background pixels. Alpha-only tests
    # incorrectly count opaque black padding as tiles. Keep this rule explicit.
    return [(r, g, b) for r, g, b, a in pixels(im) if a and max(r, g, b) > 8]


def visual_metrics(im):
    colors = Counter(foreground(im))
    return {
        "foreground_pixels": sum(colors.values()),
        "dominant_colors": ["#%02x%02x%02x" % rgb for rgb, _ in colors.most_common(3)],
        "rgba_sha256": hashlib.sha256(im.convert("RGBA").tobytes()).hexdigest(),
    }


def usage(uses, idea, name=""):
    return {
        "runtime_status": "unused",
        "mapping_status": "proposed" if uses else "unmapped",
        "current_game_uses": uses,
        "future_ideas": [idea],
    }


def historical_identity(matches):
    names = []
    for entry in matches:
        p = entry["properties"]
        parts = [str(p[k]) for k in ("group1", "group2", "group3") if p.get(k) not in (None, "")]
        label = " / ".join(parts) or "Unknown (historical metadata has no name)"
        for k in ("orientation", "index"):
            if k in p:
                label += f"; {k}={p[k]}"
        if label not in names:
            names.append(label)
    return " OR ".join(names)


def named_usage(props):
    group = str(props.get("group1", ""))
    detail = " ".join(str(props.get(k, "")) for k in ("group2", "group3")).lower()
    if group == "furniture" and any(s in detail for s in ("bed", "bunk")):
        return TILE_USES["bed"]
    if group == "furniture" and any(s in detail for s in ("plant", "flower")):
        return TILE_USES["flora"]
    if group in TILE_USES:
        return TILE_USES[group]
    extras = {
        "soda": "Future crew beverage, morale item, or empty container salvage.",
        "jam": "Future preserved food or galley decoration.",
        "musical": "Future recreation-room instrument or a rescued musician's personal item.",
        "gameconsole": "Future recreation-room electronics or crew morale activity.",
        "gamepad": "Future controller-settings icon or recreation-room prop.",
        "balloons": "Future celebration decoration inside a pressurized habitat.",
        "balloon": "Future atmospheric sensor experiment inside a habitat or celebration decoration.",
        "camera": "Future survey camera, research instrument, or security console.",
        "calculator": "Future engineering desk prop or production calculator icon.",
        "refrigerator": "Future food-preservation appliance in a powered galley.",
        "microwave": "Future ration-heating appliance in a pressurized mess hall.",
        "stove": "Future meal preparation in a pressurized galley.",
        "sink": "Future hygiene or galley water fixture.",
        "washer": "Future laundry/hygiene machinery and water demand.",
        "washbasin": "Future hygiene-room fixture or medical wash station.",
        "bathtub": "Future crew hygiene and morale fixture inside a habitat.",
        "toilet": "Future habitat sanitation and water-recycling fixture.",
        "sprinkler": "Future hydroponic irrigation or fire-suppression hardware.",
        "anvil": "Future fabrication workshop or historic cargo decoration.",
        "well": "Future ice borehole or water-extraction site after a suitable redraw.",
        "fountain": "Future recirculating water feature inside a sealed greenhouse.",
        "nest": "Future contained animal habitat or plant nursery prop.",
        "dumbbell": "Future crew exercise facility and morale/fitness activity.",
        "shoppingcart": "Future interior hauling cart or salvage trolley.",
        "microphone": "Future uplink broadcast desk or crew communications station.",
        "tv": "Future recreation monitor or Earth news display.",
        "systemunit": "Future uplink computer, research processor, or electronics salvage.",
        "printer": "Future fabrication-console prop or printed mission reports.",
        "notebook": "Future crew journal, research notes, or mission data.",
        "cd": "Future recovered data archive or electronics salvage.",
        "plasticcard": "Future supply credit card or crew ID.",
        "idcard": "Future rescued-colonist identity record or facility access badge.",
        "pen": "Future crew journal tool or mission-log icon.",
        "pencil": "Future blueprint drafting or survey-note icon.",
        "magnet": "Future magnetic salvage tool or laboratory experiment.",
        "bell": "Future pressure alarm or habitat notification device.",
        "urn": "Future memorial keepsake or habitat storage vessel.",
        "washtub": "Future hygiene-room fixture or greenhouse wash station.",
        "ladder": "Future multilevel maintenance access or rescue equipment.",
        "mast": "Future antenna support after replacing maritime styling.",
        "shipwheel": "Future manual valve handwheel after a visual review.",
        "chapel": "Future crew reflection/memorial room or a derelict landmark.",
        "gallows": "Future derelict-site storytelling prop; no execution mechanic is proposed.",
        "cigarette": "Future personal belongings or recovered cargo; no current smoking simulation.",
        "cigarettespack": "Future personal cargo or derelict storytelling prop.",
        "ammobox": "Future hazardous salvage container or optional combat supplies.",
        "sniperscope": "Future optical survey instrument after a redraw, or optional combat equipment.",
        "pellet": "Future fastener stock or optional ammunition.",
        "tinhat": "Future personal costume, keepsake, or fictional signal-shielding gag.",
        "sunglasses": "Future crew portrait accessory or visor-upgrade icon.",
        "skullcane": "Future museum cargo or fictional scenario prop.",
    }
    return [], extras.get(group, "Unmapped: visually review this named sprite for a sealed-habitat prop, recovered cargo, or optional scenario before assigning gameplay meaning.")


# Descriptions of regions are intentionally less specific than author names.
# They are observations, not a claim that every creature/profession is identified.
def region_for(col, row):
    if col >= 104:
        if row < 30:
            return "character-profession matrix"
        if row < 36:
            return "sparse creature-profession matrix"
        if row < 45:
            return "miscellaneous characters and creatures"
        return "character pose / alternate figure"
    if col <= 24:
        regions = [(4, "walls, doors, windows and stairs"), (7, "floor and terrain textures"),
                   (10, "plants, mushrooms and growth"), (13, "connected terrain / water textures"),
                   (16, "animals and invertebrates"), (20, "food and consumables"),
                   (23, "tableware, minerals and materials"), (29, "clothing and furniture"),
                   (32, "tools and industrial props"), (37, "overworld, buildings and modular terrain"),
                   (40, "bones, webs, architecture and ship parts"), (44, "devices, awards, keys and small items"),
                   (49, "decorations, containers, cables and effects")]
    elif col <= 50:
        regions = [(5, "fantasy buildings, potions and fences"), (8, "blades and hand tools"),
                   (12, "ranged weapons, armor and magical equipment"), (17, "scrolls, books, signs and shields"),
                   (21, "chests, bows, siege and workshop parts"), (25, "awards, accessories, holy symbols and furniture"),
                   (29, "flowers, plants and small props"), (41, "sparse fragments / empty expansion space"),
                   (45, "modular frames and magical effects"), (49, "heraldry, banners and large circular frame")]
    elif col <= 76:
        regions = [(1, "metal deck, grilles and sci-fi wall panels"), (5, "rails, roads and route signs"),
                   (8, "buildings, vehicles and modular facades"), (9, "medical and laboratory supplies"),
                   (12, "access cards, chips, data media and electronics"), (14, "appliances and containers"),
                   (18, "tools, devices, weapons and household props"), (20, "computers, instruments and small electronics"),
                   (23, "modular computers, screens and signal graphics"), (29, "modular industrial pipe / conduit tiles"),
                   (32, "banknote variants"), (34, "sparse expansion space"),
                   (40, "modular machine frames, supports and grilles"), (49, "status lights / sparse expansion space")]
    else:
        regions = [(3, "texture panels / sparse expansion space"), (7, "spacecraft, projectiles and sci-fi effects"),
                   (10, "sparse expansion space"), (15, "figures, suited figures and alternate poses"),
                   (17, "large celestial objects and cosmic effects"), (21, "planet, asteroid and spark effects"),
                   (26, "electronic fragments and waveform effects"), (31, "numbered / patterned tile set"),
                   (35, "HUD, resource, direction and status symbols"), (41, "sparse expansion space"),
                   (49, "bitmap alphabet, digits and punctuation")]
    return next(label for end, label in regions if row <= end)


# Held-object observations apply to the aligned profession columns, not to an
# inferred species name. Unknown silhouettes stay explicitly unknown.
CHARACTER_PROPS = {
    104: "empty hands / neutral stance", 105: "hammer-like hand tool",
    106: "pick-like hand tool", 107: "short blade-like tool", 108: "long pole-like tool",
    109: "rounded spade-like tool", 110: "broom-like tool", 111: "fork-like tool",
    112: "small vessel", 113: "bucket or rectangular shield-like prop",
    114: "short horizontal tool", 115: "small upright hand prop",
    116: "rounded shovel-like tool", 117: "axe-like tool", 118: "fishing-rod-like pole",
    119: "curved bow-like prop", 121: "hammer-like tool", 125: "raised blade-like tool",
    126: "long horizontal tool", 130: "upright rectangular prop",
    133: "small container-like prop", 134: "raised cross-shaped prop",
    143: "long pole with a raised tip", 144: "raised curved tool",
    150: "diagonal blade-like tool", 152: "raised broad tool",
    155: "large rounded shield-like prop", 157: "small cross-shaped hand prop",
    160: "large leaf or fan-like prop", 166: "rounded spade-like tool",
    170: "fork-like tool", 174: "large hammer-like tool", 175: "cross-shaped prop",
    176: "raised goblet-like prop", 178: "fork-like tool", 180: "broad axe-like tool",
    184: "cross-shaped hand prop", 187: "raised staff-like prop",
    188: "seated or low-wide stance", 190: "long horizontal pole",
    192: "diagonal blade-like tool", 194: "raised box-like prop",
    198: "bag-like hand prop", 200: "cross-shaped tool", 201: "prone / horizontal pose",
}


def visual_usage(col, row, region):
    if col >= 104:
        prop = CHARACTER_PROPS.get(col, "unidentified held prop / stance")
        label = f"Figure row {row:02d}, profession column {col - 104:02d}: {prop}"
        if row < 2 and col < 202:
            uses = ["colonist / stranded survivor"]
            idea = f"Candidate crew appearance with {prop}; distinguish working, stranded, and escorted states with overlays. Add an EVA helmet/suit cue outdoors."
        else:
            uses = []
            idea = f"Unmapped figure with {prop}: a future suit/body variant, specialist portrait, maintenance robot, or explicitly fictional encounter after checking the silhouette. Do not treat a fantasy creature as a confirmed human survivor."
        return label, uses, idea
    if col <= 24:
        if row <= 4:
            return f"Structural tile R{row:02d} C{col:03d}", ["pressure hull / airlock / deck"], "Compare corners, openings, and panels when assembling hulls; retain separate sealed/open airlock states. Adjacency is not encoded by the column number."
        if row <= 7 or 11 <= row <= 13:
            return f"Terrain / texture tile R{row:02d} C{col:03d}", ["lunar surface / basalt / resource overlay"], "A regolith, excavated rock, mineral, or frosted-ice variation after recoloring; blue water-like shapes would need a resource or indoor-water interpretation."
        if row <= 10:
            return f"Plant / growth tile R{row:02d} C{col:03d}", ["hydroponics"], "Future crop varieties or growth stages in a sealed greenhouse; exterior lunar ground stays barren."
        if row <= 16:
            return f"Animal / small creature tile R{row:02d} C{col:03d}", [], "A future contained research animal, habitat pet, or biology-lab specimen; no surface wildlife exists in current Regolith."
        if row <= 20:
            return f"Food / small consumable tile R{row:02d} C{col:03d}", ["food / rescue ration"], "Future ration varieties, harvested crops, or crew meal preferences; individual foods are not yet simulated."
        if row == 22:
            return f"Mineral / gem tile R{row:02d} C{col:03d}", ["silicate crystal / iron deposit / water ice"], "Select a distinct silhouette and palette for each mineable deposit or future refined material."
        if 24 <= row <= 29:
            return f"Clothing / furniture tile R{row:02d} C{col:03d}", [], "Future workwear, suit equipment, crew quarters, or morale furniture; verify the individual prop before assigning a functional machine."
        if 30 <= row <= 32:
            return f"Tool / industrial tile R{row:02d} C{col:03d}", ["mining / building / salvage"], "Future carried tool, engineering station, or repair component. Individual tool inventories are a new mechanic."
        return f"{region.capitalize()} R{row:02d} C{col:03d}", [], "Unmapped prop: consider a marked salvage cache, sealed-room decoration, or sector-map landmark after checking its exact silhouette and scale."
    if col <= 50:
        if 6 <= row <= 12:
            idea = "Future salvageable hand tool, museum cargo, or optional combat equipment; current rescue gameplay has no weapons or magic."
        elif 13 <= row <= 17:
            idea = "Future instruction placard, recovered research document, crew emblem, or facility warning; replace fantasy motifs if used in the lunar setting."
        elif 18 <= row <= 25:
            idea = "Future workshop storage, cargo chest, crew keepsake, or interior furniture; verify whether this is one part of a larger graphic."
        elif row >= 42:
            idea = "Future colony insignia, achievement banner, holographic laboratory effect, or modular UI frame; not an existing gameplay object."
        else:
            idea = "Future greenhouse ornament, salvage-site prop, or sector-map landmark after visual review; preserve any multi-cell assembly."
        return f"{region.capitalize()} R{row:02d} C{col:03d}", [], idea
    if col <= 76:
        if row <= 1:
            return f"Metal panel / grate R{row:02d} C{col:03d}", ["habitat deck / pressure hull"], "Use the grille/panel silhouette as a deck or hull variation; visual vents do not create a new gas connection."
        if row == 9:
            return f"Medical / laboratory prop R{row:02d} C{col:03d}", ["med station / life support"], "A medical supply, oxygen/laboratory consumable, or treatment badge after confirming the exact object."
        if 21 <= row <= 23:
            return f"Computer / display module R{row:02d} C{col:03d}", ["Earth uplink / machine status"], "Compose a console from matching frame and screen pieces; animate screen content only after checking frame compatibility."
        if 24 <= row <= 29:
            return f"Industrial conduit piece R{row:02d} C{col:03d}", [], "Future coolant/oxygen piping, reactor manifolds, or habitat wall conduits. Connectivity would need explicit rules; the current game has no pipe network."
        if 30 <= row <= 32:
            return f"Banknote-like token R{row:02d} C{col:03d}", [], "Future supply vouchers or trade credits; current resources are metal, water, food, and crystal, with no currency."
        if 35 <= row <= 40:
            return f"Modular machine part R{row:02d} C{col:03d}", ["radioisotope generator / life support / battery bank"], "Assemble a larger reactor or life-support cabinet using compatible corners, grilles, supports, and screens. Existing machines occupy one simulation cell; larger artwork must not silently expand their footprint."
        return f"{region.capitalize()} R{row:02d} C{col:03d}", [], "Future industrial salvage, research instrument, habitat furnishing, or device inventory after identifying the particular silhouette."
    if row >= 43:
        return f"Unidentified bitmap glyph R{row:02d} C{col:03d}", [], "Future pixel-font glyph or UI punctuation after reading this character manually; glyph identity is deliberately not guessed."
    if 11 <= row <= 15:
        return f"Alternate figure / pose R{row:02d} C{col:03d}", ["colonist / stranded survivor appearance"], "Candidate suited crew, portrait, injury pose, or escort-state marker after verifying the suit silhouette; no animation timing is supplied."
    if 16 <= row <= 21:
        return f"Celestial / spark piece R{row:02d} C{col:03d}", [], "Future sector-map moon/planet, orbital marker, meteor particle, or solar-storm illustration; larger celestial images span several cells."
    if 27 <= row <= 31:
        return f"Patterned / numbered tile R{row:02d} C{col:03d}", [], "Future diagnostic tile, puzzle/training panel, or numbered HUD badge after identifying the mark; not a new mineable resource."
    if 32 <= row <= 35:
        return f"HUD / status symbol R{row:02d} C{col:03d}", [], "Future health, resource, direction, confirmation, or environmental status icon after checking its exact symbol; pair color with text for readability."
    return f"{region.capitalize()} R{row:02d} C{col:03d}", [], "Future science-fiction HUD, survey effect, or decorative spacecraft component after manual identification; no current feature is assigned."


TWO_BIT_NAMES = {
    0: ["neutral figure", "hammer figure", "pick figure", "blade figure", "pole figure", "bow figure", "staff figure", "axe figure", "container figure", "raised tool figure", "red staff figure", "rod figure", "bow/tool figure", "fork figure", "small prop figure", "long tool figure", "yellow prop figure", "shovel figure", "cross/tool figure", "prone figure"],
    1: ["alternate neutral figure", "alternate hammer figure", "alternate pick figure", "alternate blade figure", "alternate pole figure", "alternate bow figure", "alternate staff figure", "alternate axe figure", "alternate container figure", "alternate raised tool figure", "alternate red staff figure", "alternate rod figure", "alternate bow/tool figure", "alternate fork figure", "alternate small prop figure", "alternate long tool figure", "alternate red prop figure", "alternate shovel figure", "alternate cross/tool figure", "alternate prone figure"],
    2: ["red-caped figure"] * 12 + ["skeleton-like figure", "alternate skeleton-like figure"],
    3: ["broad sword", "patterned sword", "club", "short blade", "slender blade", "bow", "staff", "axe", "blue-tipped staff", "red-tipped staff", "green-tipped staff", "crossbow-like tool", "blue-headed tool", "hammer", "pickaxe", "diagonal tool", "saw"],
    4: ["round cyan flask", "alternate cyan flask", "blue flask", "angular cyan flask", "red small item", "red staff-like item", "brown key/tool", "red berries", "purple cluster", "brown sack", "filled sack", "green round item", "pink round item", "cyan round item", "cyan access card", "red A access card", "blue B access card", "green C access card", "vine segment", "vine segment"],
    5: ["yellow building", "yellow hut", "computer terminal", "red-roof house", "red-roof building", "green figure", "green figure", "green figure", "green figure", "blue/green circular object", "curved orange tool", "yellow trophy-like object", "purple plant/tripod", "unknown", "unknown", "unknown", "unknown", "unknown", "vine segment", "vine segment"],
    6: ["metal fence", "column", "open frame", "metal frame", "wooden chair", "wooden steps/frame", "wooden chair", "wooden table", "wooden cabinet", "wooden grate/table", "red medical case", "orange case", "wooden chest", "wooden chest", "reinforced chest", "reinforced chest", "unknown", "unknown", "vine segment", "vine segment"],
}


def two_bit_usage(col, row, label):
    if row < 2:
        return ["colonist / stranded survivor"], "Alternate crew or injury/prone appearance; add EVA suit cues outdoors and use overlays to distinguish rescue state."
    if row == 2:
        return [], "Future specialist outfit, memorial/remains sprite, or optional fictional character; capes and skeletons are not current crew roles."
    if row == 3:
        return (["mining / building"] if col in (13, 14, 16) else []), "Future carried tool, workshop item, or salvageable equipment; combat and individual weapons are not current mechanics."
    if "vine" in label or "plant" in label:
        return ["hydroponics"], "Future greenhouse growth, crop variety, or a multi-cell plant inside a pressurized room."
    if "flask" in label:
        return ["med station / life support"], "Future medicine, water sample, or oxygen/coolant consumable; no magic potion mechanic."
    if "medical" in label:
        return ["med station / injured survivor"], "Future portable rescue first-aid kit or medical supply stockpile."
    if "terminal" in label:
        return ["Earth uplink"], "Future research or life-support terminal."
    return [], "Future habitat furniture, recovered cargo, crew keepsake, or sector-map prop after checking the depicted object; no current feature is assigned."


def build_urizen():
    matches = json.loads((REFERENCE / "urizen-names.json").read_text())["matches"]
    records = []
    for filename, cols, rows, short in [
        ("urizen_onebit_tileset__v2d0.png", 206, 50, "onebit"),
        ("urizen__2bit__free.png", 20, 7, "twobit"),
    ]:
        source = f"urizen/{filename}"
        im = Image.open(ASSETS / source).convert("RGBA")
        for row in range(rows):
            for col in range(cols):
                rect = [1 + col * 13, 1 + row * 13, 12, 12]
                tile = im.crop((rect[0], rect[1], rect[0] + 12, rect[1] + 12))
                metrics = visual_metrics(tile)
                kind = "guide" if short == "onebit" and col in GUIDES else "sprite" if metrics["foreground_pixels"] else "blank"
                record = {"id": f"urizen-{short}-r{row:02d}-c{col:03d}", "pack": "urizen", "source": source,
                          "rect": rect, "grid": [col, row], "kind": kind, **metrics}
                if kind != "sprite":
                    record.update(name="Magenta separator band" if kind == "guide" else "Empty / black-only atlas slot",
                                  runtime_status="not_applicable", mapping_status="not_applicable",
                                  current_game_uses=[], future_ideas=[],
                                  notes="Layout space, not a gameplay sprite. Blank detection ignores transparent and near-black (all RGB channels <=8) pixels.")
                    records.append(record)
                    continue
                if short == "onebit":
                    region = region_for(col, row)
                    matched = matches.get(f"{col},{row}", [])
                    valid = [m for m in matched if m["properties"].get("group1") not in (None, "", "unknown")]
                    if valid:
                        props = valid[0]["properties"]
                        name = historical_identity(valid)
                        uses, idea = named_usage(props)
                        identity = {"basis": "author_metadata_visible_rgb_match", "confidence": "high", "upstream_matches": valid}
                        category = props["group1"]
                    else:
                        name, uses, idea = visual_usage(col, row, region)
                        identity = {"basis": "visual_region_and_silhouette", "confidence": "low", "author_name_known": False}
                        category = region
                    record.update(name=name, category=category, atlas_region=region, identity=identity, **usage(uses, idea))
                    if col >= 104 and row < 36:
                        record["character_matrix"] = {"body_variant_row": row, "profession_variant_column": col - 104,
                                                      "held_prop_observation": CHARACTER_PROPS.get(col, "unidentified held prop / stance"),
                                                      "species_and_profession_confirmed": False}
                        record["notes"] = "Rows are visual body variants, not verified species or palette swaps. Columns align prop/stance variants but do not supply animation frames or job definitions."
                else:
                    names = TWO_BIT_NAMES.get(row, [])
                    name = names[col] if col < len(names) else "unidentified showcase sprite"
                    uses, idea = two_bit_usage(col, row, name)
                    record.update(name=f"2-bit showcase: {name} (R{row:02d} C{col:02d})", category="showcase / " + ("character" if row < 3 else "item or environment"),
                                  identity={"basis": "visual_inference", "confidence": "medium" if name != "unknown" else "low", "author_name_known": False}, **usage(uses, idea))
                    record["notes"] = "Visual working label, not an author-supplied name. This is a limited sample, not a full 2-bit replacement for the main atlas."
                if metrics["foreground_pixels"] <= 2:
                    record.update(mapping_status="unmapped", current_game_uses=[],
                                  future_ideas=["Unmapped sparse mark: inspect as a possible dust/spark pixel, punctuation fragment, or atlas residue before using it."],
                                  notes="Only one or two foreground pixels; retained for exhaustive coverage, not certified as an intentional standalone sprite.")
                records.append(record)
    return records


KEY_USES = {
    "Esc": "cancel selection / close current panel", "1": "normal simulation speed; choose blueprint/crew 1 in menus", "2": "faster simulation; choose blueprint/crew 2 in menus", "3": "fastest simulation; choose blueprint/crew 3 in menus",
    **{str(i): f"choose blueprint/crew {i} in the corresponding menu" for i in range(4, 10)},
    "0": "reset zoom to 100%; choose blueprint 10 in build menu", "Space": "pause / resume", "Enter": "confirm / designate / rescue selected survivor",
    "Up": "move cursor north", "Down": "move cursor south", "Left": "move cursor west", "Right": "move cursor east",
    "H": "h: move west; Shift+H: move cursor west by a larger step", "J": "j: move south; Shift+J: larger step",
    "K": "k: move north; Shift+K: larger step", "L": "l: move east; Shift+L: load colony",
    "B": "b: open build selector", "D": "d: mine / salvage tool", "V": "v: rectangular designation", "X": "x: cancel designation",
    "P": "p: survey tool; previous tutorial page", "R": "r: cycle on-map distress signals", "O": "o: cycle environmental overlays",
    "G": "g: center cursor/view on habitat", "C": "c: crew panel / close crew panel", "F": "f: follow selected colonist", "E": "e: event log",
    "N": "n: next tutorial page; restart colony on the game-over screen",
    "S": "s: save colony", "T": "t: start/restart tutorial", "Q": "q: quit",
    "Shift": "combine with arrows or H/J/K for larger cursor steps; Shift+L loads colony",
    "Keyboard": "keyboard-control tutorial illustration", "Mouse": "click a stranded survivor; wheel zooms around pointer",
}


def has_alpha(im, rect):
    x, y, w, h = rect
    return im.crop((x, y, x + w, y + h)).getchannel("A").getbbox() is not None


def prompt_regions(path):
    """Explicit rectangles; keyboard keys are NOT one sprite per 16px cell."""
    source = path.relative_to(ASSETS).as_posix()
    im = Image.open(path).convert("RGBA")
    regions = []

    def add(name, rect, family, visual_state="unspecified", kind="sprite"):
        if has_alpha(im, rect):
            regions.append((name, rect, family, visual_state, kind))

    if "Keyboard" in path.name:
        for dy, state in [(0, "raised"), (96, "flat")]:
            add("Esc", [27, dy, 21, 16], "keyboard", state)
            for i, key in enumerate("1234567890"):
                add(key, [48 + i * 16, 16 + dy, 16, 16], "keyboard", state)
            add("Tab", [16, 32 + dy, 32, 16], "keyboard", state)
            for i, key in enumerate("QWERTYUIOP"):
                add(key, [48 + i * 16, 32 + dy, 16, 16], "keyboard", state)
            for i, key in enumerate("ASDFGHJKL"):
                add(key, [48 + i * 16, 48 + dy, 16, 16], "keyboard", state)
            add("Enter", [192, 48 + dy, 42, 16], "keyboard", state)
            add("Shift", [6, 64 + dy, 42, 16], "keyboard", state)
            for i, key in enumerate("ZXCVBNM"):
                add(key, [48 + i * 16, 64 + dy, 16, 16], "keyboard", state)
            add("Up", [192, 64 + dy, 16, 16], "keyboard", state)
            corn = "Corn" in path.name
            add("Ctrl", [16, 80 + dy, 22 if corn else 21, 16], "keyboard", state)
            # The flat-state Alt and space really are shifted / shorter in the
            # source artwork. Do not infer their rectangles from the raised row.
            alt_x = 61 if corn else 64 if not dy else 80
            space_x = 80 if corn else alt_x + 16
            add("Alt", [alt_x, 80 + dy, space_x - alt_x, 16], "keyboard", state)
            add("Space", [space_x, 80 + dy, 160 - space_x, 16], "keyboard", state)
            for i, key in enumerate(["Left", "Down", "Right"]):
                add(key, [176 + 16 * i, 80 + dy, 16, 16], "keyboard", state)
        for x, y in [(224, 0), (32, 208)]:
            add("Keyboard", [x, y, 24, 16], "device legend")
            add("Mouse", [x + 24, y, 8, 16], "device legend")
        add("Cubs of Corn font credit", [0, 240, 176, 16], "annotation", kind="annotation")
        add("Hyohnoo author credit", [176, 240, 80, 16], "annotation", kind="annotation")
    else:
        legacy = path.parent.name == "legacy"
        ps4 = path.stem == "Ps4"
        xbox_one = path.stem == "Xbox one"
        families = [(0, 0, "large"), (160, 0, "compact"), (0, 128, "tiny")]
        if legacy:
            families = [(0, 0, "legacy bright"), (0, 128, "legacy dark")]
        for ox, oy, family in families:
            face_x = 32 if legacy else ox
            control_x = 96 if legacy else ox + 48
            faces = ["Cross", "Square", "Triangle", "Circle"] if ps4 else ["A", "X", "Y", "B"]
            for j, face in enumerate(faces):
                for i, state in enumerate(["bright", "dim"]):
                    add(face, [face_x + i * 16, 16 + j * 32 + oy, 16, 16], family, state)
            if ps4:
                for j, name in enumerate(["Options", "Share"]):
                    for i, state in enumerate(["bright", "dim"]):
                        add(name, [control_x + i * 16, 16 + j * 16 + oy, 16, 16], family, state)
            elif xbox_one or (legacy and oy):
                for j, state in enumerate(["bright", "dim"]):
                    for i, name in enumerate(["View", "Menu", "Blank button"]):
                        add(name, [control_x + i * 16, 16 + j * 16 + oy, 16, 16], family, state)
            else:
                for j, name in enumerate(["Start/right-triangle", "Back/left-triangle"]):
                    for i, state in enumerate(["bright", "dim"]):
                        add(name, [control_x + i * 16, 16 + j * 16 + oy, 16, 16], family, state)
            for i, direction in enumerate(["neutral", "left", "up", "right", "down"]):
                add("D-pad " + direction, [control_x + i * 16, 64 + oy, 16, 16], family, "direction highlighted")
            for j, pair in enumerate([["L2", "R2"], ["L1", "R1"]] if ps4 else [["LT", "RT"], ["LB", "RB"]]):
                for i, (key, state) in enumerate([(pair[0], "dim"), (pair[0], "bright"), (pair[1], "bright"), (pair[1], "dim")]):
                    trigger_x = control_x + (16 if family == "compact" and not ps4 else 0)
                    add(key, [trigger_x + i * 16, 96 + j * 16 + oy, 16, 16], family, state)
            stick_x = 176 if legacy else ox + 128
            for i, key in enumerate(["L stick", "R stick"]):
                add(key, [stick_x + i * 16, 96 + oy, 16, 16], family)
        if legacy:
            add("Controller silhouette", [224, 0, 32, 16], "device legend")
            add("Hyohnoo author credit", [176, 248, 80, 8], "annotation", kind="annotation")
    return source, im, regions


def controller_idea(name):
    if name in ("A", "Cross"):
        return "Future gamepad confirm, place blueprint, or interact/rescue action."
    if name in ("B", "Circle"):
        return "Future gamepad back/cancel action."
    if name in ("X", "Square"):
        return "Future gamepad mining or context-action prompt."
    if name in ("Y", "Triangle"):
        return "Future gamepad build menu or overlay-cycle prompt."
    if "D-pad" in name:
        return "Future discrete cursor movement, menu selection, or directional tutorial prompt."
    if name in ("LT", "RT", "L2", "R2"):
        return "Future gamepad zoom out/in or modifier prompt."
    if name in ("LB", "RB", "L1", "R1"):
        return "Future gamepad previous/next colonist, distress signal, or build category."
    if "stick" in name:
        return "Future analog map pan, cursor movement, or click-stick follow/center action."
    if name in ("Menu", "Options", "Start/right-triangle"):
        return "Future pause menu or tutorial controls on a gamepad."
    if name in ("Share", "View", "Back/left-triangle"):
        return "Future gamepad colony overview, crew roster, or help panel."
    return "Future remappable controller action or device-detection legend; choose a binding only after adding actual gamepad input."


def build_prompts():
    records, coverage = [], []
    for path in sorted((ASSETS / "input-prompts").glob("*/*.png")):
        source, im, regions = prompt_regions(path)
        covered = Image.new("L", im.size)
        from PIL import ImageDraw
        draw = ImageDraw.Draw(covered)
        for i, (key, rect, family, state, kind) in enumerate(regions):
            x, y, w, h = rect
            draw.rectangle((x, y, x + w - 1, y + h - 1), fill=255)
            tile = im.crop((x, y, x + w, y + h))
            uses = [KEY_USES[key]] if family in ("keyboard", "device legend") and key in KEY_USES else []
            idea = ("Future keyboard shortcut or keybinding screen; this key has no dedicated current action." if family == "keyboard" and not uses
                    else "Future graphical tutorial prompt with matching raised/flat feedback; keycaps show the key, not whether the game expects a shifted letter." if family == "keyboard"
                    else controller_idea(key))
            record = {"id": "prompt-" + re.sub(r"[^a-z0-9]+", "-", source[:-4].lower()).strip("-") + f"-{i:03d}",
                      "pack": "input-prompts", "source": source, "rect": rect, "kind": kind,
                      "name": f"{key} / {family} / {state}", "category": "keyboard" if family == "keyboard" else "device legend" if family == "device legend" else "controller",
                      "control": key, "style": path.stem, "visual_state": state, "size_family": family,
                      "identity": {"basis": "visible_key_label_or_symbol", "confidence": "high" if family == "keyboard" else "medium"},
                      **visual_metrics(tile), **usage(uses, idea)}
            if kind == "annotation":
                record.update(category="credit annotation", runtime_status="not_applicable", mapping_status="not_applicable", current_game_uses=[],
                              future_ideas=[], notes="Source-sheet credit lettering, retained in the source image; not a gameplay prompt.")
            elif family not in ("keyboard", "device legend"):
                record["notes"] = "No gamepad input currently exists. Bright/dim labels describe appearance, not a publisher-confirmed pressed/released state. L/R glyphs do not encode a stick direction."
            records.append(record)
        uncovered = [(x, y) for y in range(im.height) for x in range(im.width)
                     if im.getpixel((x, y))[3] and not covered.getpixel((x, y))]
        coverage.append({"source": source, "uncovered_nontransparent_pixels": len(uncovered), "regions": len(regions)})
        if uncovered:
            # Fail rather than silently claim that every prompt is documented.
            raise ValueError(f"Uncovered artwork in {source}: {len(uncovered)} pixels; first positions: {uncovered[:30]}")
    return records, coverage


def build_icons():
    records = []
    for path in sorted((ASSETS / "godot-node-icons").glob("*/*.png")):
        stem = path.stem.removeprefix("icon_")
        if stem not in ICON_USES:
            raise ValueError(f"Missing per-icon usage: {stem}")
        uses, idea = ICON_USES[stem]
        im = Image.open(path).convert("RGBA")
        style = path.parent.name
        records.append({"id": f"godot-{style}-{stem}", "pack": "godot-node-icons", "source": path.relative_to(ASSETS).as_posix(),
                        "rect": [0, 0, im.width, im.height], "kind": "sprite", "name": stem.replace("_", " "),
                        "category": "UI icon", "style": style, "semantic_family": stem,
                        "identity": {"basis": "publisher_filename", "confidence": "high"},
                        **visual_metrics(im), **usage(uses, idea),
                        "notes": f"{style} is a visual variant, not a separate Regolith object. Originally a Godot editor/node icon; the proposed use is for this game's HUD. "
                                 + ("White is the suggested primary HUD variant." if style == "white" else "An alternate to the white HUD variant; no variant is currently loaded.")})
    return records


def cell(text):
    return str(text).replace("|", "\\|").replace("\n", " ")


def write_pack_markdown(records, pack, title, output):
    lines = [f"# {title}", "", "Generated from the import-time audit in [sprites.json](../sprites.json). The `unused` / `proposed` statuses describe the archived Python prototype. See [catalog schema 5](CATALOG_V2.md) for neutral artwork metadata and separate current Ithica/preview selection and [ithica-colony-sprites.json](../ithica-colony-sprites.json) for the home bindings. `unmapped` means no specific feature was assigned in the audit; ideas can require new mechanics or art changes.", "",
             "All coordinates are zero-based source pixels; rectangles are `x, y, width, height`. Urizen IDs also encode zero-based row and column. Source paths are relative to `assets/`.", "",
             "Identity basis: **author match** = historical author name matched by visible RGB artwork; **filename** = publisher's filename; **visual** = working description, not a verified author name. Unnamed figures keep their species/profession unconfirmed.", ""]
    sources = sorted({r["source"] for r in records if r["pack"] == pack})
    for source in sources:
        subset = [r for r in records if r["source"] == source]
        art = [r for r in subset if r["kind"] == "sprite"]
        other = Counter(r["kind"] for r in subset if r["kind"] != "sprite")
        lines.extend([f"## {source}", "", f"{len(art):,} sprite records. Non-sprite regions: " + (", ".join(f"{n:,} {k}" for k, n in sorted(other.items())) or "none") + ". Non-sprite rectangles remain enumerated in the JSON.", "",
                      "| Sprite ID | Identity / basis | Crop (x,y,w,h) | Runtime / mapping | Existing-game proposal | Future idea / review note |",
                      "|---|---|---|---|---|---|"])
        for r in art:
            basis = r["identity"]["basis"]
            label = "author match" if basis.startswith("author_") else "filename" if basis == "publisher_filename" else "visual"
            lines.append("| " + " | ".join(cell(v) for v in [r["id"], r["name"] + f" ({label}; {r['identity']['confidence']})", ",".join(map(str, r["rect"])),
                         "unused / " + r["mapping_status"], "; ".join(r["current_game_uses"]) or "Unmapped", "; ".join(r["future_ideas"])]) + " |")
        lines.append("")
    output.write_text("\n".join(lines) + "\n")


def write_browser(data):
    # Inline data deliberately: file:// browsers cannot reliably fetch JSON.
    browser_records = []
    for record in data["records"]:
        entry = {k: v for k, v in record.items() if k not in ("rgba_sha256", "dominant_colors", "foreground_pixels", "character_matrix")}
        if "identity" in entry:
            entry["identity"] = {k: v for k, v in entry["identity"].items() if k != "upstream_matches"}
        browser_records.append(entry)
    compact = json.dumps(browser_records, ensure_ascii=False, separators=(",", ":")).replace("<", "\\u003c")
    template = (REFERENCE / "browser-template.html").read_text()
    (ASSETS / "sprites.html").write_text(template.replace("SPRITE_REFERENCE_DATA", compact))


def main():
    REFERENCE.mkdir(exist_ok=True)
    prompts, coverage = build_prompts()
    records = build_urizen() + build_icons() + prompts
    ids = [r["id"] for r in records]
    assert len(set(ids)) == len(ids), "Duplicate sprite IDs"
    files = load_catalog()["images"]
    sizes = {r["path"]: (r["width"], r["height"]) for r in files}
    assert {r["source"] for r in records} == set(sizes), "A source image is not covered"
    for filename, cols, rows in [("urizen_onebit_tileset__v2d0.png", 206, 50), ("urizen__2bit__free.png", 20, 7)]:
        slots = [r for r in records if r["source"] == "urizen/" + filename]
        assert len(slots) == cols * rows
        assert {tuple(r["grid"]) for r in slots} == {(col, row) for row in range(rows) for col in range(cols)}
    for entry in files:
        assert hashlib.sha256((ASSETS / entry["path"]).read_bytes()).hexdigest() == entry["sha256"], "Original artwork changed: " + entry["path"]
    for record in records:
        x, y, w, h = record["rect"]
        width, height = sizes[record["source"]]
        assert x >= 0 and y >= 0 and w > 0 and h > 0 and x + w <= width and y + h <= height, record["id"]
        if record["kind"] == "sprite":
            assert record["future_ideas"] and record["runtime_status"] == "unused"
            assert record["mapping_status"] in ("proposed", "unmapped")
    art = [r for r in records if r["kind"] == "sprite"]
    recommended = [
        ("Terrain.VACUUM", ["urizen-onebit-r05-c002"], "Sand/dirt-like texture recolored to gray regolith; artwork name is terrestrial, gameplay remains vacuum."),
        ("Terrain.ROCK", ["urizen-onebit-r02-c000"], "Stone-wall silhouette for solid basalt; distinguish it from constructed hull."),
        ("Terrain.ICE", ["urizen-onebit-r22-c003"], "Crystal silhouette recolored icy cyan as an ice-deposit overlay; no claim that the author called it water ice."),
        ("Terrain.ORE", ["urizen-onebit-r22-c000"], "Geode silhouette recolored iron/rust as an ore-deposit overlay."),
        ("Terrain.CRYSTAL", ["urizen-onebit-r22-c002"], "A separate mineral/crystal silhouette and violet palette distinguish silicates from ice."),
        ("Terrain.FLOOR / blueprint:floor", ["urizen-onebit-r01-c052"], "Metal floor for walkable habitat deck; deck connectivity continues to carry power."),
        ("Terrain.WALL / blueprint:wall", ["urizen-onebit-r00-c052"], "Metal wall for pressure hull; intact art is separate from breach damage."),
        ("Terrain.AIRLOCK / blueprint:airlock", ["urizen-onebit-r00-c068", "urizen-onebit-r00-c071"], "Metal valve door and open door are alternate visual states of the same airlock cell."),
        ("blueprint:solar", ["urizen-onebit-r00-c064"], "The author names this glass wall. Its diagonal cyan panel could be redrawn/recolored as a solar array; no exact named solar panel was recovered."),
        ("blueprint:battery", ["urizen-onebit-r09-c055"], "Battery icon as a one-cell storage-bank symbol; a larger bank can be drawn separately."),
        ("blueprint:oxygen", ["urizen-onebit-r35-c058"], "A modular machine vent/fan-like panel as a compact life-support symbol; exact machine identity is unconfirmed."),
        ("blueprint:farm", ["urizen-onebit-r24-c005"], "Vegetable crop inside a pressurized hydroponic module; no outdoor lunar vegetation."),
        ("blueprint:bunk", ["urizen-onebit-r36-c000"], "Single bed as the sleeping-pod symbol, optionally enclosed with sci-fi casing."),
        ("blueprint:medbay", ["urizen-onebit-r09-c059"], "First-aid case as the compact med-station symbol."),
        ("blueprint:beacon", ["urizen-onebit-r06-c055"], "Radio tower for the Earth uplink; future active/unpowered status can be overlaid."),
        ("machine:rtg", ["urizen-onebit-r06-c056"], "Power-station silhouette as a generator symbol; this is not an engineering depiction of an RTG."),
        ("colonist / stranded survivor", ["urizen-onebit-r00-c104", "urizen-onebit-r00-c105", "urizen-onebit-r00-c106"], "Neutral/tool-bearing figures for crew, engineer, and miner appearances. Profession is a Regolith proposal; outdoor figures need suit cues."),
        ("fire hazard", ["urizen-onebit-r42-c006"], "Flame overlay driven by cell.fire; art must not replace heat or oxygen simulation values."),
    ]
    by_id = {r["id"]: r for r in art}
    recommendations = []
    for feature, sprite_ids, rationale in recommended:
        recommendations.append({"feature": feature, "sprite_ids": sprite_ids, "rationale": rationale, "status": "proposed"})
        for sprite_id in sprite_ids:
            record = by_id[sprite_id]
            if feature not in record["current_game_uses"]:
                record["current_game_uses"].append(feature)
            record["mapping_status"] = "proposed"
    first_by_hash = {}
    for record in art:
        key = (tuple(record["rect"][2:]), record["rgba_sha256"])
        if key in first_by_hash:
            record["identical_crop_to"] = first_by_hash[key]
        else:
            first_by_hash[key] = record["id"]
    gutter_marks = []
    for source in sorted({r["source"] for r in art if r["pack"] == "urizen"}):
        im = Image.open(ASSETS / source).convert("RGBA")
        p = im.load()
        marks = [[x, y, *p[x, y]] for y in range(im.height) for x in range(im.width)
                 if (x % 13 == 0 or y % 13 == 0) and p[x, y][3] and max(p[x, y][:3]) > 8]
        gutter_marks.append({"source": source, "foreground_pixel_count": len(marks), "pixels_x_y_rgba": marks,
                             "interpretation": "Out-of-grid border/stray pixels, not additional standalone sprites; retained here so coverage does not silently omit them."})
    summary = {
        "source_images_covered": len(sizes), "records": len(records), "sprites": len(art),
        "by_kind": dict(Counter(r["kind"] for r in records)),
        "sprites_by_pack": dict(Counter(r["pack"] for r in art)),
        "mapping_status": dict(Counter(r["mapping_status"] for r in art)),
        "identity_basis": dict(Counter(r["identity"]["basis"] for r in art)),
        "runtime_used_sprites": 0,
        "identical_crop_repeats": sum("identical_crop_to" in r for r in art),
        "sprites_by_source": dict(Counter(r["source"] for r in art)),
        "prompt_coverage": coverage,
    }
    data = {
        "schema_version": 1,
        "description": "Exhaustive sprite/atlas-slot reference for Regolith's three downloaded art packs. Companion to the file-level catalog.json. Design proposals only; curses currently loads no bitmap sprites.",
        "coordinate_convention": {"origin": "top-left", "base": 0, "rect": ["x", "y", "width", "height"], "grid": ["column", "row"], "paths_relative_to": "assets/", "urizen_tile_size": [12, 12], "urizen_margin": [1, 1], "urizen_pitch": [13, 13]},
        "status_definitions": {
            "unused": "No current game code loads or renders this bitmap asset.",
            "proposed": "A candidate use for a currently implemented Regolith feature; not an active runtime mapping.",
            "unmapped": "No specific existing feature is assigned; future_ideas gives possible uses, often requiring new mechanics or redrawing.",
            "not_applicable": "Blank atlas slot, separator, or author/font credit annotation; not a gameplay sprite.",
        },
        "identification_limits": [
            "Author names are recovered only when the historical 12x12 visible RGB art matches. Near-black (all channels <=8) and alpha-zero pixels are normalized to black. This is not a fuzzy or shape-only match.",
            "Visual descriptions and regional groups are working labels. They do not certify species, profession, exact object identity, connectivity, or animation order.",
            "Every Urizen grid slot is included, including empty slots and separator bands; occupied fragment-like cells are retained and flagged.",
            "The onebit atlas includes modular multi-cell pictures. A crop is one 12x12 cell and may be only part of a machine, banner, celestial body, plant, or large building.",
            "Input prompts use explicit rectangles; styles, size families, and bright/dim or raised/flat art variants are enumerated separately. No gamepad input currently exists.",
        ],
        "evidence": {"game_renderer": "regolith/tui.py:TILES, ASCII_TILES, Terminal._tile_visual", "game_features": ["regolith/world.py:Terrain", "regolith/simulation.py:BLUEPRINTS", "regolith/simulation.py:SURVIVOR_PROFILES"],
                     "file_provenance_and_hashes": "catalog.json", "historical_urizen_name_matches": "reference/urizen-names.json", "license_and_publisher_links": "CREDITS.txt"},
        "summary": summary, "recommended_mappings": recommendations, "atlas_gutter_marks": gutter_marks, "records": records,
    }
    dump(ASSETS / "sprites.json", data)
    write_pack_markdown(records, "urizen", "Urizen: every occupied atlas cell", REFERENCE / "urizen.md")
    write_pack_markdown(records, "godot-node-icons", "Godot node icons: every variant", REFERENCE / "godot-node-icons.md")
    write_pack_markdown(records, "input-prompts", "Keyboard and controller prompts: every control graphic", REFERENCE / "input-prompts.md")
    write_browser(data)
    dump(REFERENCE / "coverage.json", summary)
    print(json.dumps({k: v for k, v in summary.items() if k != "sprites_by_source"}, indent=2))


if __name__ == "__main__":
    main()
