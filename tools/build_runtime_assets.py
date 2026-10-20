#!/usr/bin/env python3
"""Select existing sprite IDs for the client without modifying the source catalog/artwork. Standard library only."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "assets"
catalog = json.loads((ASSETS / "sprites.json").read_text())
records = {r["id"]: r for r in catalog["records"] if r["kind"] == "sprite"}

bindings = {
    "vacuum": "urizen-onebit-r05-c002", "rock": "urizen-onebit-r02-c000",
    "ice": "urizen-onebit-r22-c003", "ore": "urizen-onebit-r22-c000", "crystal": "urizen-onebit-r22-c002",
    "floor": "urizen-onebit-r01-c052", "wall": "urizen-onebit-r00-c052", "airlock": "urizen-onebit-r00-c068",
    "airlock-open": "urizen-onebit-r00-c071", "solar": "urizen-onebit-r00-c064", "battery": "urizen-onebit-r09-c055",
    "oxygen": "urizen-onebit-r35-c058", "farm": "urizen-onebit-r24-c005", "bunk": "urizen-onebit-r36-c000",
    "medbay": "urizen-onebit-r09-c059", "beacon": "urizen-onebit-r06-c055", "rtg": "urizen-onebit-r06-c056",
    "crew": "urizen-onebit-r00-c104", "engineer": "urizen-onebit-r00-c105", "miner": "urizen-onebit-r00-c106",
    "survivor": "urizen-onebit-r01-c104", "fire": "urizen-onebit-r42-c006",
}
for name, icon in {"mine":"shovel", "build":"hammer", "survey":"search", "cancel":"clear", "inspect":"hand",
                   "water":"liquid", "metal":"gear", "food":"meat", "power":"thunder", "crystal":"gem", "crew":"character",
                   "rescue":"interrogation", "zoom":"magnifier", "health":"heart", "save":"save", "pause":"paused",
                   "play":"play", "time":"time", "signal":"signal", "log":"list", "settings":"gear_2", "center":"target",
                   "follow":"follow", "temperature":"freeze", "shield":"shield", "help":"scroll"}.items():
    bindings["icon-"+name] = "godot-white-"+icon

aliases = {"roundopen":"(","roundclose":")","squareopen":"[","squareclose":"]","curlyopen":"{","curlyclose":"}",
           "less":"<","more":">","plus":"+","minus":"-","question":"?","exclamation":"!","caret":"^",
           "colon":":","hash":"#","underscore":"_","atsign":"@","percent":"%","tilde":"~","dollar":"$",
           "quote":'"',"apostrophe":"'","ampersand":"&","asterisk":"*","equality":"=","graveaccent":"`",
           "bar":"|","slash":"/","backslash":"\\","period":".","comma":",","semicolon":";"}
font = {}
for r in records.values():
    if r.get("category") != "font":
        continue
    for match in r["identity"].get("upstream_matches", []):
        p=match["properties"]
        if p.get("group1") == "font" and p.get("group2") == "default":
            char=aliases.get(p["group3"],p["group3"])
            if len(char)==1:font[char]=r["id"]

for key in ["Space","Enter","Esc","Shift","Mouse","R","B","D","O","S","L","0"]:
    chosen=next(r for r in records.values() if r["source"] == "input-prompts/new/Keyboard White.png"
                and r.get("control")==key and r.get("visual_state") in ("raised","unspecified"))
    bindings["key-"+key.lower()]=chosen["id"]

selected=set(bindings.values())|set(font.values())
data={"schemaVersion":1,"sourceCatalog":"sprites.json","notes":"Runtime selection of unchanged catalog IDs and rectangles. Recoloring/background removal happens in GPU texture copies, never in source files.",
      "bindings":bindings,"font":font,
      "sprites":{key:{"source":records[key]["source"],"rect":records[key]["rect"]}for key in sorted(selected)}}
assert all(key in records for key in selected)
assert set("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789?!.,:+-=/()") <= set(font)
(ASSETS/"ithica-colony-sprites.json").write_text(json.dumps(data,indent=2)+"\n")
print(f"Selected {len(selected)} existing sprite IDs, {len(bindings)} bindings, {len(font)} glyphs. Original images and catalog unchanged.")
