#!/usr/bin/env python3
"""Recover conservative subject evidence from pinned historical author artwork.

Development-only, explicit network operation:
  uv run --with pillow python tools/match_upstream_silhouettes.py --download

Routine catalog regeneration reads the resulting JSON without network/Pillow.
Original artwork and frozen identity records are never changed. A shape match
is weaker than a visible-RGB match and can have multiple conflicting names.
"""
import argparse
import collections
import hashlib
import json
import urllib.request
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
REVISION = '28b1e60cf8fa2f15622537f6df0290e2ac890d5f'
BASE = f'https://raw.githubusercontent.com/vurmux/urizen/{REVISION}/urizen/data/tilesets'
SETS = ['urizen-onebit-basic', 'urizen-onebit-fantasy-medieval', 'urizen-onebit-fonts', 'urizen-onebit-modern']


def mask(image):
    return bytes(1 if alpha and max(r, g, b) > 8 else 0 for r, g, b, alpha in image.convert('RGBA').get_flattened_data())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--download', action='store_true', help='Download the pinned upstream evidence into the local ignored cache.')
    parser.add_argument('--cache', type=Path, default=ROOT / 'artifacts/upstream-urizen')
    args = parser.parse_args()
    args.cache.mkdir(parents=True, exist_ok=True)
    sources, known = [], collections.defaultdict(list)
    for tileset in SETS:
        paths = {}
        for extension in ['json', 'png']:
            url = f'{BASE}/{tileset}/colored.{extension}'
            path = args.cache / f'{tileset}.{extension}'
            if args.download:
                with urllib.request.urlopen(url, timeout=30) as response:
                    content = response.read()
                path.write_bytes(content)
            if not path.is_file():
                raise SystemExit(f'Missing cached evidence {path}; explicitly pass --download to fetch it.')
            paths[extension] = path
            sources.append({'url': url, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
        metadata = json.loads(paths['json'].read_text())
        image = Image.open(paths['png']).convert('RGBA')
        if metadata['tilewidth'] != 12 or metadata['tileheight'] != 12:
            raise ValueError('Unexpected upstream tile size')
        for entry in metadata['tiles']:
            properties = {p['name']: p.get('value') for p in entry.get('properties', [])}
            if properties.get('group1') in [None, '', 'unknown']:
                continue
            index = entry['id']
            x = metadata['margin'] + index % metadata['columns'] * (12 + metadata['spacing'])
            y = metadata['margin'] + index // metadata['columns'] * (12 + metadata['spacing'])
            value = mask(image.crop((x, y, x + 12, y + 12)))
            if sum(value) <= 2:
                continue
            known[value].append({'set': tileset, 'tile_id': index, 'properties': properties})
    frozen = json.loads((ROOT / 'assets/sprites.json').read_text())['records']
    images, matches = {}, {}
    for r in frozen:
        if r['pack'] != 'urizen' or r['kind'] != 'sprite':
            continue
        if r['source'] not in images:
            images[r['source']] = Image.open(ROOT / 'assets' / r['source']).convert('RGBA')
        x, y, w, h = r['rect']
        value = mask(images[r['source']].crop((x, y, x + w, y + h)))
        if value not in known:
            continue
        candidates = known[value]
        # All possible publisher names are retained, even when conflicting.
        matches[r['id']] = {'mask_sha256': hashlib.sha256(value).hexdigest(), 'matches': candidates,
                            'original_identity_confidence': r['identity']['confidence']}
    result = {'schema_version': 1, 'upstream_revision': REVISION, 'sources': sources,
              'license_evidence': 'CREDITS.txt; upstream LICENSE files at the pinned revision',
              'comparison': 'Exact 12x12 binary foreground mask after excluding alpha-zero / RGB <=8 pixels; colors ignored. No fuzzy similarity or coordinate transfer.',
              'confidence': 'medium',
              'limits': 'Silhouette equality is not publisher confirmation of a recolored crop. Keep all alternative matches; only shared subject terms may be promoted as silhouette-supported claims. Source identity and original RGB evidence remain unchanged.',
              'matches': matches}
    output = ROOT / 'assets/reference/urizen-silhouette-matches.json'
    output.write_text(json.dumps(result, indent=2) + '\n')
    unconfirmed = sum(m['original_identity_confidence'] != 'high' for m in matches.values())
    print(json.dumps({'matched_crops': len(matches), 'previously_unconfirmed_identity': unconfirmed,
                      'output': str(output)}, indent=2))


if __name__ == '__main__':
    main()
