#!/usr/bin/env python3
"""Export the small, verified settlement set without changing source artwork."""
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools'))
from asset_catalog import Png, encode_rgba_png

APP = ROOT / 'apps/settlement'
SOURCE = 'assets/urizen/urizen_onebit_tileset__v2d0.png'
FRAMES = {
    'grass0': (5, 15), 'grass1': (5, 16), 'dirt': (5, 1),
    'oak': (8, 1), 'vegetables': (24, 5),
    'cottage0': (33, 0), 'cottage1': (33, 1),
    'resident': (0, 104),
    'imp': (20, 104), 'djinn': (18, 104),
    'merchant': (0, 198), 'storyteller': (2, 187),
    'season-flowers': (10, 1), 'autumn-leaf': (9, 1),
    'oak-winter': (10, 5), 'visitor-bag': (47, 6), 'visitor-torch': (42, 3),
    'water': (12, 1), 'boulder': (13, 4),
    'bridge-h': (40, 11), 'bridge-v': (40, 10),
    'workshop': (33, 5), 'market': (33, 4), 'hall': (33, 2),
    **{f'road{i}': (11, i) for i in range(13)},
}

ICONS = ('hammer', 'grid', 'clear', 'coin')

# four inspected source silhouettes; these are static bodies, not walk frames.
BODIES = {'plain-v1': 0, 'longhair-v1': 1, 'hood-v1': 2, 'cap-v1': 3}
PALETTES = {
    'cream': ('ead9a2', 'a6714e', 'b4ad82', '68523c'),
    'teal': ('87c8bd', 'd6a478', '667e83', '493d3d'),
    'clay': ('dc967a', 'e7bd91', '89765e', '5a4133'),
    'lilac': ('b6bce0', 'ba895f', '777d98', '473e39'),
}


def resident_crop(png, body, palette):
    row = BODIES[body]
    rect = [1353, 1 + row * 13, 12, 12]
    rgba = bytearray(png.crop(rect))
    peak = max(rgba[i] for i in range(0, len(rgba), 4))
    coat, skin, trousers, hair = [bytes.fromhex(c) for c in PALETTES[palette]]
    for pixel in range(144):
        i, x, y = pixel * 4, pixel % 12, pixel // 12
        value = max(rgba[i:i+3])
        if value <= 8:
            rgba[i+3] = 0
            continue
        head_end = 4 if row < 2 else 5
        if y <= head_end:
            # retain the keyed eye/neck gaps; colour only original opaque pixels.
            is_hair = (y <= (1 if row < 2 else 3) or
                       row == 1 and x <= 3 or row == 3 and x >= 8)
            color = hair if is_hair else skin
        elif y >= 10:
            color = trousers
        elif x in (3, 9):
            color = skin
        else:
            color = coat
        rgba[i:i+3] = bytes(round(c * value / peak) for c in color)
    return rect, rgba


LIVING_BODIES = {
    'imp-body-v1': (20, 104), 'djinn-body-v1': (18, 104),
    'merchant-body-v1': (0, 198), 'storyteller-body-v1': (2, 187),
}
LIVING_COLORS = {
    'imp-body-v1': ('da9278','c67c70','d2a073','c48ca3'),
    'djinn-body-v1': ('92c6df','89c9c1','b4a5da','9eb9d5'),
    'merchant-body-v1': ('d5bd83','c9ac77','d7c395','c0ad90'),
    'storyteller-body-v1': ('c3a9d5','adbbd6','c7b4d2','ada5cf'),
}

def living_crop(png, body, palette):
    row, col = LIVING_BODIES[body]
    rect = [1+col*13, 1+row*13, 12, 12]
    rgba = bytearray(png.crop(rect))
    peak = max(max(rgba[i:i+3]) for i in range(0,len(rgba),4))
    color = bytes.fromhex(LIVING_COLORS[body][list(PALETTES).index(palette)])
    for i in range(0,len(rgba),4):
        value=max(rgba[i:i+3])
        if value<=8: rgba[i+3]=0
        else: rgba[i:i+3]=bytes(round(c*value/peak) for c in color)
    return rect, rgba


def resident_atlas(png, living=False):
    # bake the existing four-direction outline at 4x; original colour pixels stay intact.
    cell, width, height = 52, 416, 468 if living else 260
    atlas = bytearray(width * height * 4)
    frames = []
    for body in [*BODIES, *(LIVING_BODIES if living else {})]:
        for palette in PALETTES:
            _, rgba = living_crop(png, body, palette) if body in LIVING_BODIES else resident_crop(png, body, palette)
            for mirrored in [False, True]:
                index = len(frames)
                ox, oy = index % 8 * cell, (index // 8 + (1 if index >= 32 else 0)) * cell
                solid = {}
                for y in range(12):
                    for x in range(12):
                        source = (y * 12 + (11-x if mirrored else x)) * 4
                        if not rgba[source+3]: continue
                        for dy in range(4):
                            for dx in range(4):
                                solid[(2+x*4+dx, 2+y*4+dy)] = rgba[source:source+4]
                pixels = {}
                for x, y in solid:
                    for dx, dy in [(-2, 0), (2, 0), (0, -2), (0, 2)]:
                        pixels[(x+dx, y+dy)] = bytes.fromhex('1b2b26ff')
                pixels.update(solid)
                for (x, y), color in pixels.items():
                    at = ((oy+y)*width+ox+x)*4
                    atlas[at:at+4] = color
                frames.append({'body':body,'palette':palette,'mirrored':mirrored,'rect':[ox,oy,cell,cell]})
    for y in range(7):
        for x in range(26):
            if ((x+.5-13)/13)**2+((y+.5-3.5)/3.5)**2 <= 1:
                at=((208+y)*width+x)*4
                atlas[at:at+4]=bytes.fromhex('17291fbf')
    data=encode_rgba_png(width,height,atlas)
    return data, {'file':'images/world-actors-atlas.png' if living else 'images/resident-atlas.png','sha256':hashlib.sha256(data).hexdigest(),
                  'cell':[52,52],'feet':[26,50],'world_scale':9/48,
                  'frames':frames,'shadow':[0,208,26,7],
                  'treatment':'4x nearest-neighbour copies of the same source/palette; two-pixel outline; explicit mirrored copies; procedural shadow'}


def products():
    png = Png(ROOT / SOURCE)
    frames, output = {}, {}
    for name, (row, col) in FRAMES.items():
        rect = [1 + col * 13, 1 + row * 13, 12, 12]
        rgba = bytearray(png.crop(rect))  # validates every exact source rectangle
        for i in range(0, len(rgba), 4):
            if max(rgba[i:i+3]) <= 8:
                rgba[i+3] = 0
        data = encode_rgba_png(12, 12, rgba)
        output[f'assets/images/{name}.png'] = data
        frames[name] = {
            'id': f'urizen-onebit-r{row:02d}-c{col:03d}',
            'source': SOURCE, 'rect': rect, 'footprint': [1, 1],
            'file': f'images/{name}.png', 'sha256': hashlib.sha256(data).hexdigest(),
        }
    masks = set()
    for body, row in BODIES.items():
        for palette in PALETTES:
            rect, rgba = resident_crop(png, body, palette)
            name = f'person-{body}-{palette}'
            data = encode_rgba_png(12, 12, rgba)
            output[f'assets/images/{name}.png'] = data
            frames[name] = {
                'id': f'urizen-onebit-r{row:02d}-c104', 'source': SOURCE,
                'rect': rect, 'file': f'images/{name}.png',
                'body': body, 'palette': palette, 'feet': [6, 12],
                'render_size': [9, 9], 'animation': 'static; procedural presentation',
                'treatment': 'black-key alpha; semantic hair/skin/coat/trousers palette; original holes and luminance retained',
                'sha256': hashlib.sha256(data).hexdigest(),
            }
            if palette == 'cream':
                mask = bytes(rgba[3::4])
                assert mask not in masks, 'cast contains duplicate silhouettes'
                masks.add(mask)
    for body in LIVING_BODIES:
        for palette in PALETTES:
            rect, rgba = living_crop(png, body, palette)
            name=f'person-{body}-{palette}'
            data=encode_rgba_png(12,12,rgba)
            output[f'assets/images/{name}.png']=data
            row,col=LIVING_BODIES[body]
            frames[name]={'id':f'urizen-onebit-r{row:02d}-c{col:03d}', 'source':SOURCE,
                          'rect':rect,'file':f'images/{name}.png','body':body,'palette':palette,
                          'treatment':'source silhouette and luminance retained; semantic living-world palette; static source drawing',
                          'sha256':hashlib.sha256(data).hexdigest()}
    for name in ICONS:
        path = f'assets/godot-node-icons/white/icon_{name}.png'
        image = Png(ROOT / path)
        data = (ROOT / path).read_bytes()
        output[f'assets/images/{name}.png'] = data
        frames[name] = {'source': path, 'rect': [0, 0, image.width, image.height],
                        'file': f'images/{name}.png', 'sha256': hashlib.sha256(data).hexdigest()}
    manifest = {'schema': 1, 'source_sha256': hashlib.sha256((ROOT/SOURCE).read_bytes()).hexdigest(),
                'extraction': 'RGB <= 8 keyed transparent; source pixels otherwise retained. Renderer applies semantic tint.',
                'frames': frames,
                'road_masks': {'5': 'road0', '10': 'road2', '12': 'road4', '9': 'road5',
                               '6': 'road6', '3': 'road7', '13': 'road8', '14': 'road9',
                               '7': 'road10', '11': 'road11', '15': 'road12'},
                'derived': 'Masks 0,1,2,4,8: dirt texture clipped to center and real cardinal ports; no fictitious connection.'}
    atlas, atlas_manifest=resident_atlas(png)
    output['assets/images/resident-atlas.png']=atlas
    manifest['resident_atlas']=atlas_manifest
    world_atlas, world_manifest=resident_atlas(png, living=True)
    output['assets/images/world-actors-atlas.png']=world_atlas
    manifest['world_atlas']=world_manifest
    manifest['living_world']={
        'version':1,
        'peoples':{'human-v1':list(BODIES),'imp-v1':['imp-body-v1'],'djinn-v1':['djinn-body-v1']},
        'visitors':{'merchant-v1':'merchant-body-v1','storyteller-v1':'storyteller-body-v1'},
        'entities':'permanent residents retain cottage ownership; visitors are separate temporary actors',
        'composites':{'winter-tree':'oak-winter is a source dead-tree drawing cast as bare winter branches; terrain stays forest',
                      'merchant-stop':'visitor-bag appears only during actual Market activity',
                      'story-circle':'visitor-torch appears only during actual storytelling'},
        'animation':'no source animation order verified; existing procedural travel and gestures only',
    }
    output['assets/manifest.json'] = (json.dumps(manifest, indent=2)+'\n').encode()
    output['assets/SourceCodePro-Regular.otf'] = (ROOT/'assets/fonts/SourceCodePro-Regular.otf').read_bytes()
    output['assets/LICENSE-SourceCodePro.md'] = (ROOT/'assets/fonts/LICENSE-SourceCodePro.md').read_bytes()
    return output


if __name__ == '__main__':
    check = '--check' in sys.argv
    for name, data in products().items():
        path = APP / name
        if check:
            if not path.exists() or path.read_bytes() != data:
                raise SystemExit(f'Mismatch: {path}')
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
    print(f'Verified {len(FRAMES)+len(ICONS)} source crops and {len(BODIES) + len(LIVING_BODIES)} actor bodies × {len(PALETTES)} palettes, manifest and unmodified font.' if check else 'Exported small settlement asset set.')
