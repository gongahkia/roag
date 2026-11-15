#!/usr/bin/env python3
"""Export the small, verified settlement set without changing source artwork."""
import hashlib
import json
from pathlib import Path
import shutil
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
    **{f'road{i}': (11, i) for i in range(13)},
}


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
    for name in ['hammer', 'grid', 'clear']:
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
    print(f'Verified {len(FRAMES)+3} bounded crops, manifest and unmodified font.' if check else 'Exported small settlement asset set.')
