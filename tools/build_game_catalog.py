#!/usr/bin/env python3
"""Enrich every existing sprite with per-game usage and RPG affordances; preserve all source IDs/evidence.

Standard library only. catalog.json remains backwards compatible with its original
file inventory. sprites.json is retained as the frozen import audit. No images are changed.
"""
import argparse
import base64
import collections
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "assets"


def read(name):
    return json.loads((ASSETS / name).read_text())


def write(name, value, check=False, compact=False):
    target = ASSETS / name
    content = json.dumps(value, ensure_ascii=False, indent=None if compact else 2,
                         separators=(",", ":") if compact else None) + "\n"
    if check:
        if not target.exists() or target.read_text() != content:
            raise SystemExit(f"Stale generated file: {target.relative_to(ROOT)}; run python3 tools/build_game_catalog.py")
    else:
        target.write_text(content)


def assessment(r):
    """Evidence-scoped recommendations, not invented identification of unknown artwork."""
    category = r.get("category", "unclassified")
    name = r["name"].lower()
    tag = category.lower()
    if r["kind"] != "sprite":
        return {"family": "non-gameplay", "layer": "reference", "rpg_role": "Not a gameplay sprite", "ideas": [], "adaptation": "Keep blank cells, separator bands, and credits out of gameplay bindings."}
    if r["pack"] == "godot-node-icons":
        return {"family": "interface", "layer": "ui", "rpg_role": "HUD/action symbol: " + r["name"],
                "ideas": ["Inventory, ability, status, journal, or settings icon; inspect the named silhouette before binding.", *r.get("future_ideas", [])],
                "adaptation": "Editor-node origin is not a gameplay identity. Keep semantic family consistent across color variants; use a single readable palette."}
    if r["pack"] == "input-prompts":
        control = r.get("control", r["name"])
        return {"family": "input", "layer": "ui", "rpg_role": "Input tutorial for " + control,
                "ideas": ["Show the " + control + " prompt beside the action actually bound to that input."],
                "adaptation": "Keyboard/mouse are implemented. Controller art is exploratory; gamepad support is not implemented. Visual pressed/dim states do not prove publisher animation timings."}
    if "font" in tag or "glyph" in name:
        return {"family": "typography", "layer": "ui", "rpg_role": "Pixel text, numerals, or decorative lettering", "ideas": ["Use known default glyphs for dialogue and tabular HUD numbers; reserve gothic forms for short headings."], "adaptation": "Only identified glyphs can enter a font map. Fixed advance and integer-scale inspection are needed for readable text."}
    if r.get("character_matrix") or any(t in tag for t in ["character", "creature", "figure", "player", "enemy", "npc", "animal"]):
        obs = r.get("character_matrix", {}).get("held_prop_observation", r["name"])
        return {"family": "actors", "layer": "actor", "rpg_role": "Party/NPC/enemy appearance candidate: " + obs,
                "ideas": ["Cast this exact body/prop combination as an adventurer, settlement NPC, or encounter enemy after visual review.", "Use the stable ID as an appearance variant; map role and faction separately from appearance."],
                "adaptation": "A row or color is not a confirmed species. A prop is not proof of profession. Adjacent columns are independent silhouettes, not confirmed walking or attack frames."}
    if any(t in tag for t in ["weapon", "shield", "armor", "helmet", "ring", "necklace", "clothes", "clothing", "blades", "ranged"]):
        return {"family": "equipment", "layer": "ui-or-prop", "rpg_role": "Equipment/ability icon or treasure: " + r["name"],
                "ideas": ["Represent an equipable item, merchant stock, ability button, or reward preview.", "Recolor rarity or damage school in a GPU mask without changing the original sheet."],
                "adaptation": "A tile shows an item silhouette, not a complete equipping system. Held-weapon overlays require manual alignment; the prototype has role-defined equipment."}
    if any(t in tag for t in ["floor", "wall", "door", "stairs", "river", "water", "stockade", "bridge", "terrain", "overworld", "building", "street", "road", "rail", "multistory"]):
        return {"family": "environment", "layer": "ground-or-structure", "rpg_role": "Traversable ground, blocking structure, transition, or overworld landmark",
                "ideas": ["Compose a settlement, forest path, ruin, dungeon room, or travel map using this " + r["name"] + " crop.", "Keep tile collision and occlusion explicit in map data rather than inferring them from pixels."],
                "adaptation": "Verify orientation and multi-cell joins. Index values are not an established autotile bitmask. Building/overworld miniatures use a different representational scale from room walls."}
    if any(t in tag for t in ["flora", "plant", "flower", "farm", "mushroom"]):
        return {"family": "vegetation", "layer": "prop", "rpg_role": "Biome dressing, harvestable plant, or readable cover silhouette",
                "ideas": ["Use as woodland/overgrown-ruin dressing or an optional herb/food pickup.", "Distinguish decorative grass from collidable trees in map rules."],
                "adaptation": "Terrestrial vegetation suits the fantasy prototype. The colony game would need enclosed agriculture or a fictional biome; these do not imply lunar outdoor ecology."}
    if any(t in tag for t in ["food", "potion", "consumable", "syringe", "pill", "firstaid", "soda", "canned"]):
        return {"family": "consumables", "layer": "ui-or-prop", "rpg_role": "Healing, mana, camp provisions, or status-remedy candidate",
                "ideas": ["Use this " + r["name"] + " crop for a consumable inventory slot, chest reward, or merchant offer.", "A different silhouette can communicate the effect without relying only on color."],
                "adaptation": "Potion, torch, and ration resources are implemented. Additional item effects and inventory slots remain proposals; modern containers may need a fantasy reinterpretation."}
    if any(t in tag for t in ["magic", "effect", "projectile", "spark", "fire", "explosive"]):
        return {"family": "effects", "layer": "effect", "rpg_role": "Spell, damage, status, impact, or ambient effect candidate",
                "ideas": ["Overlay a brief attack impact, spell school, warning tile, or magical prop.", "Test the crop in the native viewer at several backgrounds and scales."],
                "adaptation": "Adjacent effect cells do not establish an animation sequence or timing. Several motifs span cells; assemble only after checking joins."}
    if any(t in tag for t in ["computer", "screen", "electronic", "industrial", "machine", "pipe", "deck", "accesscard", "chip", "science", "transport", "banknote", "celestial", "spacecraft"]):
        return {"family": "technology", "layer": "prop-or-assembly", "rpg_role": "Optional lost-technology ruin, apparatus, vehicle, or alternate science-fantasy theme",
                "ideas": ["Use as colony machinery first; try an ancient mechanism, alchemist's apparatus, airship, or science-fantasy dungeon prop.", "Group connected fragments into a reviewed assembly before using them as a complete machine."],
                "adaptation": "Outside the current RPG's medieval setting unless deliberately reinterpreted. No vehicle, currency-note, pipe-flow, or research simulation is implied by the art."}
    if any(t in tag for t in ["heraldry", "symbol", "HUD", "numbered", "patterned", "frame", "status", "bitmap"]):
        return {"family": "symbols-and-assemblies", "layer": "ui-or-assembly", "rpg_role": "Faction badge, status cue, modular frame, puzzle marker, or UI decoration",
                "ideas": ["Give a verified emblem a faction/quest meaning, or build a multi-cell ornament after inspecting its boundaries."],
                "adaptation": "No automatic bitmask/animation/complete-object interpretation. Sparse cells can be fragments rather than intentional standalone symbols."}
    if "sparse" in tag:
        return {"family": "needs-review", "layer": "unresolved", "rpg_role": "Unmapped: sparse or ambiguous artwork",
                "ideas": ["Inspect the exact crop and neighboring cells in the atlas viewer; retain it for ornament/effect/assembly experiments only after verification."],
                "adaptation": "Do not assign a concrete object identity from coordinates or color. Some very sparse crops are stray marks."}
    return {"family": "props", "layer": "prop", "rpg_role": "Scene dressing, interactable, treasure, or environmental storytelling candidate: " + r["name"],
            "ideas": ["Try this crop in a town interior, roadside campsite, crypt, chest, shrine, or collection display.", *r.get("future_ideas", [])],
            "adaptation": "Verify the silhouette, apparent material, scale, and neighboring fragments before binding. Proposed interactions need explicit game rules."}


def main():
    parser = argparse.ArgumentParser(); parser.add_argument("--check", action="store_true"); args = parser.parse_args()
    inventory = read("catalog.json")
    audit = read("sprites.json")
    records = audit["records"]
    sprites = {r["id"]: r for r in records if r["kind"] == "sprite"}
    colony = read("ithica-colony-sprites.json")
    rpg = read("ithica-expedition-bindings.json")
    bindings = rpg["bindings"]
    generation = read("ithica-generation.json")
    visual_review = read("reference/urizen-generation-review.json")
    reviewed = {r["id"]: r for r in visual_review["entries"]}
    if len(reviewed) != len(visual_review["entries"]) or any(sid not in sprites for sid in reviewed):
        raise ValueError("Invalid or duplicate Urizen generation review ID")
    rule_refs = collections.defaultdict(list)
    def visit_rules(value, path=""):
        if isinstance(value, dict):
            for key, child in value.items(): visit_rules(child, path + "." + key if path else key)
        elif isinstance(value, list):
            for i, child in enumerate(value): visit_rules(child, path + f"[{i}]")
        elif isinstance(value, str) and value in bindings:
            rule_refs[bindings[value]].append(path + " = " + value)
    visit_rules(generation)
    expected = [*generation["forest"]["props"], *generation["hearth"]["furniture"],
                generation["hazards"]["fireSprite"], generation["hazards"]["sporesSprite"], generation["hazards"]["corruptionSprite"]]
    expected += [s for m in generation["materials"].values() for s in m["sprites"]]
    for variant in generation["abbey"]["variants"]:
        expected += variant["floorSprites"] + variant["props"] + [variant["wallSprite"]]
        if variant["weight"] <= 0: raise ValueError("Room weight must be positive")
    if any(alias not in bindings for alias in expected): raise ValueError("Unbound sprite in generation rules")
    for key, sprite in bindings.items():
        if sprite not in sprites: raise ValueError(f"Missing RPG binding {key}: {sprite}")
    runtime = {"schemaVersion": 1, "sourceCatalog": "catalog.json", "notes": rpg["notes"], "bindings": bindings,
               "font": colony["font"], "sprites": {key: {"source": r["source"], "rect": r["rect"]} for key, r in sprites.items()}}
    write("ithica-expedition-sprites.json", runtime, args.check)
    usages = collections.defaultdict(lambda: {"colony": [], "rpg": []})
    for game, manifest in [("colony", colony), ("rpg", runtime)]:
        for alias, sid in manifest["bindings"].items(): usages[sid][game].append(alias)
        for glyph, sid in manifest["font"].items(): usages[sid][game].append("font:" + glyph)
    enriched = []
    for r in records:
        e = dict(r)
        e["import_audit"] = {"runtime_status": r["runtime_status"], "mapping_status": r["mapping_status"]}
        e["usage"] = usages[r["id"]]
        e["runtime_status"] = "used" if any(e["usage"].values()) else "unused" if r["kind"] == "sprite" else "not_applicable"
        e["assessment"] = assessment(r)
        family = e["assessment"]["family"]
        candidate = family in {"environment", "vegetation", "props", "effects", "technology", "symbols-and-assemblies"}
        status = "runtime_rule" if r["id"] in rule_refs else "candidate_requires_rules" if candidate and r["kind"] == "sprite" else "not_world_generation"
        if r["kind"] != "sprite": status = "not_applicable"
        e["generation"] = {"status": status, "rule_refs": rule_refs.get(r["id"], []),
            "role": {"environment": "Ground/structure/landmark; review joints and scale before choosing topology.",
                     "vegetation": "Biome or hazard-source dressing; assign collision, fuel and safe-route exclusions explicitly.",
                     "props": "Room/biome decoration; reserve quest footprints and decide blocking separately.",
                     "effects": "Hazard or effect overlay; visual crop does not define spreading or animation.",
                     "actors": "Encounter/NPC appearance chosen after the quest graph; never use body variants as room sockets.",
                     "technology": "Future alternate-theme assembly after joint/footprint review.",
                     "symbols-and-assemblies": "Future ornament/socket marker only after complete-assembly review."}.get(family, "UI/item/reference artwork; no automatic terrain placement."),
            "adjacency_basis": "Game-authored sockets and masks in ithica-generation.json; artwork indices are not inferred adjacency.",
            "visual_review": reviewed.get(r["id"]),
            "required_before_new_use": [] if status == "runtime_rule" else ["Review identity, layer, apparent scale, footprint and joins.", "Author biome, weight, collision and safe-route exclusions; validate reachability."] if candidate else []}
        if r["id"] in reviewed:
            note = reviewed[r["id"]]
            e["assessment"]["ideas"] = [note["future_use"], *e["assessment"]["ideas"]]
        e["tags"] = sorted({r["pack"], e["assessment"]["family"], e["assessment"]["layer"], r.get("category", "unclassified")})
        e["review"] = {"identity_basis": r.get("identity", {}).get("basis", "not_applicable"),
                       "confidence": r.get("identity", {}).get("confidence", "not_applicable"),
                       "individual_name_confirmed": r.get("identity", {}).get("basis", "").startswith(("author_", "publisher_filename", "visible_key")),
                       "game_role_is_interpretation": bool(e["usage"]["rpg"]),
                       "needs_visual_review": r["kind"] == "sprite" and r.get("identity", {}).get("confidence") != "high"}
        e["review"]["assessment_basis"] = "Evidence-scoped category/region rules plus preserved crop observations; curated runtime casting is separate from publisher identity. Not a new manual identification of every motif."
        e["review"]["generation_crop_visually_inspected"] = r["id"] in reviewed
        enriched.append(e)
    # Retain every original inventory field; replace only generated v2 enrichments.
    inventory.update(schema_version=3,
        catalog_scope="Original image/archive inventory plus every sprite/slot from all three packs, with live per-game usage and evidence-scoped RPG proposals.",
        source_audit="sprites.json", generator="tools/build_game_catalog.py",
        catalog_summary={"images": len(inventory["images"]), "records": len(enriched), "occupied_sprites": len(sprites),
                         "used_colony": sum(bool(e["usage"]["colony"]) for e in enriched), "used_rpg": sum(bool(e["usage"]["rpg"]) for e in enriched),
                         "used_either": sum(e["runtime_status"] == "used" for e in enriched),
                         "generation_rule_sprites": len(rule_refs), "generation_visually_reviewed": len(reviewed),
                         "unused_sprites": sum(e["runtime_status"] == "unused" for e in enriched),
                         "families": dict(sorted(collections.Counter(e["assessment"]["family"] for e in enriched if e["kind"] == "sprite").items()))},
        generation_rules={"source": "ithica-generation.json", "schema_version": generation["schemaVersion"], "generator_version": generation["generatorVersion"],
                          "visual_review": "reference/urizen-generation-review.json", "notes": generation["notes"]},
        game_bindings={"colony": colony["bindings"], "rpg": bindings}, sprites=enriched,
        catalog_limits=["Every crop has a usage/affordance assessment; that is not individual verified identification of all 7,115 motifs.",
                        "Historical exact-RGB matches and filename/control labels retain their original evidence. Uncertain figures and fragments remain uncertain.",
                        "No inferred animation timing, direction set, species, collision mask, or autotile bitmask is presented as publisher metadata.",
                        "Renderer registration in the viewer is separate from active gameplay use. Unbound sprites remain unused even though the viewer can display them."])
    write("catalog.json", inventory, args.check)
    index = [{"id": e["id"], "name": e["name"], "pack": e["pack"], "source": e["source"], "rect": e["rect"],
              "category": e.get("category", "unclassified"), "family": e["assessment"]["family"], "layer": e["assessment"]["layer"],
              "confidence": e["review"]["confidence"], "basis": e["review"]["identity_basis"], "role": e["assessment"]["rpg_role"],
              "ideas": e["assessment"]["ideas"], "adaptation": e["assessment"]["adaptation"], "usage": e["usage"],
              "generationNotes": e["generation"]["status"] + ". " + e["generation"]["role"] + " " + "; ".join(e["generation"]["rule_refs"])
                  + (" Visual review: " + reviewed[e["id"]]["visual_observation"] + " " + reviewed[e["id"]]["future_use"] if e["id"] in reviewed else "")}
             for e in enriched if e["kind"] == "sprite"]
    write("atlas-index.json", index, args.check, compact=True)
    casting={
        "wolf":"The author's dog becomes a Briar hound; enemy name/faction are game data.",
        "warden":"Raised blade-like prop; Rowan's role is casting, not a confirmed publisher profession.",
        "ranger":"Bow-like held prop; Vesper's role and abilities are game data.",
        "mage":"Staff-like held prop; no walking/casting animation sequence is assumed.",
        "healer":"Cross-shaped held prop; Sable's class is a game interpretation.",
        "skeleton":"Gray figure variant cast as an oathless guard; source species remains unconfirmed.",
        "abbot":"Author-matched monk/prayer figure cast as the Hollow Abbot boss.",
        "coffin":"Unconfirmed chest/workshop silhouette used as a coffin-shaped crypt prop; this is not an author label.",
        "staff":"Author's skullcane used for the wand/arcane ability icon.",
        "bow":"Author-confirmed bow used for arrow/volley ability icons.",
        "banner":"Heraldic/prop silhouette used for the party rally ability; no cloth simulation.",
        "water":"A source water motif repeated over blue backing; collision is explicit map data.",
        "wall":"Author's wall motif; repeated full cells rather than an inferred autotile bitmask.",
        "ruin-wall":"Author-confirmed stone wall used as masonry for generated room boundaries; sockets belong to the 7x7 room.",
        "cracked-wall":"Visually fractured structural pattern used for collapsed masonry; its exact publisher variant remains unconfirmed.",
        "grit":"Author-confirmed grit becomes eroded-room ground and scorched grass presentation. Collision remains explicit Ground data.",
        "boulder":"Author-confirmed boulder cluster used as nonblocking scenery, rather than adding an obstacle or mining resource.",
        "cave-mouth":"Author-confirmed cave silhouette used as scenery; it does not create a fourth playable area or transition.",
        "crystal":"Author-confirmed diagonal crystal becomes decorative ruin treasure; no mining/crafting/reward interaction is implemented.",
        "hazard-fire":"Author's fire crop marks spreading magical fire. Campfire props remain safe rest locations and do not ignite it.",
        "hazard-spores":"Author's button mushrooms represent a spore cloud/source overlay; poison is game casting, not a publisher property.",
        "hazard-corruption":"Author's leaf silhouette represents magical blight. Growth, source lifetime and stress costs are game rules.",
        "willow":"Author's willow is a collidable/occluding Tree appearance; changing the crop does not change tree rules.",
        "poplar":"Author's poplar is a collidable/occluding Tree appearance in the cellular forest.",
        "broken-column":"Author-confirmed broken antique column used as nonblocking collapsed-room scenery outside the safe cross.",
    }
    def cell(text):return str(text).replace("|","\\|").replace("\n"," ")
    lines=["# Ithica expedition: curated runtime bindings","",
           "Every RPG semantic alias below references an unchanged ID/crop in the source audit. Casting and palette are game decisions, not new author identities. The native viewer additionally registers all 7,115 occupied sprites; registration alone does not make them gameplay bindings.","",
           "Edit `assets/ithica-expedition-bindings.json`, then run `python3 tools/build_game_catalog.py`. Inspect crops in [the atlas](../atlas.html) or the native F2 viewer. Original publisher-name evidence remains in [catalog.json](../catalog.json).","",
           f"**{len(bindings)} semantic aliases, {len(colony['font'])} font glyphs, {len(set(bindings.values())|set(colony['font'].values()))} distinct runtime-bound IDs.**","",
           "| Alias | Stable ID | Source identity / working label | Evidence | Game interpretation / adaptation |","|---|---|---|---|---|"]
    for alias,sid in sorted(bindings.items()):
        r=sprites[sid];note=casting.get(alias,"Render as "+alias.replace('-',' ')+"; preserve the source identity and keep mechanics in game data.")
        lines.append("| "+" | ".join(cell(x) for x in [alias,sid,r['name'],r['identity']['basis']+" / "+r['identity']['confidence'],note])+" |")
    lines += ["","## Shared bitmap font","","The RPG reuses the colony's exact default-font map. Space is an advance without a crop. Gothic/unknown glyphs remain available for inspection but are not substituted automatically.","","| Character | Stable ID |","|---|---|"]
    for glyph,sid in sorted(colony['font'].items()):lines.append("| "+cell(repr(glyph))+" | "+sid+" |")
    md="\n".join(lines)+"\n";md_path=ASSETS/"reference"/"rpg-bindings.md"
    if args.check:
        if not md_path.exists() or md_path.read_text()!=md:raise SystemExit("Stale rpg-bindings.md")
    else:md_path.write_text(md)
    review_lines = ["# Urizen: generation crop review", "", visual_review["scope"], "",
        "This supplements the exhaustive per-sprite catalog. Observations below were checked against the exact source crops; uncertain publisher identities stay uncertain. Source pixels, IDs, rectangles, original audit labels and identity confidence are preserved.", "",
        "The game uses authored north/east/south/west sockets on **7 x 7 room chunks**, not on arbitrary sprite numbers. Only aliases referenced by [ithica-generation.json](../ithica-generation.json) are eligible for automatic generation. Collision belongs to Ground/object data; generated props, including boulders, grave markers and cave mouths, are nonblocking scenery. Cave artwork does not add another playable area. Crystal props add no mining or crafting mechanic.", "",
        "Fire, spores and corruption use explicit overlay rules. Button mushrooms represent spores and a leaf silhouette represents magical corruption; these are game interpretations, not publisher identities. Existing campfires remain rest locations, not fire sources.", "",
        f"**{len(reviewed)} individually inspected crops; {len(rule_refs)} distinct sprite IDs referenced by generation rules across all packs.**", "",
        "| Stable ID | Preserved identity / evidence | Visual observation | Generation status / rule references | Future use / limitations |", "|---|---|---|---|---|"]
    for note in visual_review["entries"]:
        r=sprites[note["id"]]
        refs="; ".join(rule_refs.get(note["id"], [])) or "Not used by generation; " + note["decision"]
        review_lines.append("| " + " | ".join(cell(v) for v in [r["id"], r["name"] + " / " + r["identity"]["confidence"], note["visual_observation"], refs, note["future_use"]]) + " |")
    review_path=ASSETS/"reference"/"urizen-generation.md"; review_md="\n".join(review_lines)+"\n"
    if args.check:
        if not review_path.exists() or review_path.read_text()!=review_md: raise SystemExit("Stale urizen-generation.md")
    else:review_path.write_text(review_md)
    template = (ASSETS / "reference" / "atlas-template.html").read_text()
    image_data={item["path"]:"data:image/png;base64,"+base64.b64encode((ASSETS/item["path"]).read_bytes()).decode("ascii") for item in inventory["images"]}
    html = template.replace("CATALOG_DATA", json.dumps(index, ensure_ascii=False, separators=(",", ":")).replace("<", "\\u003c")).replace("CATALOG_IMAGES",json.dumps(image_data,separators=(",", ":")))
    html_path = ASSETS / "atlas.html"
    if args.check:
        if not html_path.exists() or html_path.read_text() != html: raise SystemExit("Stale atlas.html")
    else: html_path.write_text(html)
    if args.check:
        for item in inventory["images"] + inventory["archives"]:
            if hashlib.sha256((ASSETS / item["path"]).read_bytes()).hexdigest() != item["sha256"]: raise SystemExit("Original file changed: " + item["path"])
    print(json.dumps(inventory["catalog_summary"], indent=2))


if __name__ == "__main__":
    main()
