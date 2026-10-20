#!/usr/bin/env python3
"""Create coordinate-labelled review sheets without changing source artwork.

Development-only Pillow dependency:
  uv run --with pillow python tools/make_asset_review.py

Outputs go to ignored artifacts/asset-review/. The manifest records exact source
hashes and IDs so visual authoring can identify what was actually inspected.
"""
import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'assets'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=ROOT / 'artifacts/asset-review')
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    records = json.loads((ASSETS / 'sprites.json').read_text())['records']
    manifest = []
    try:
        font = ImageFont.truetype('/usr/share/fonts/dejavu-sans-fonts/DejaVuSans.ttf', 12)
    except OSError:
        font = ImageFont.load_default(size=12)
    for source, columns, rows, style in [('urizen/urizen_onebit_tileset__v2d0.png', 206, 50, 'onebit'),
                                          ('urizen/urizen__2bit__free.png', 20, 7, 'twobit')]:
        with Image.open(ASSETS / source) as original:
            original = original.convert('RGBA')
            source_hash = hashlib.sha256((ASSETS / source).read_bytes()).hexdigest()
            starts = [0, 26, 52, 78, 104, 130, 156, 182] if style == 'onebit' else [0]
            for column in starts:
                end_col = min(columns, column + (25 if column < 104 else 26) if style == 'onebit' else 20)
                for row in range(0, rows, 10):
                    end_row = min(rows, row + 10)
                    rect = [column * 13 + 1, row * 13 + 1, (end_col - column) * 13 - 1, (end_row - row) * 13 - 1]
                    crop = original.crop((rect[0], rect[1], rect[0] + rect[2], rect[1] + rect[3]))
                    crop = crop.resize((crop.width * 4, crop.height * 4), Image.Resampling.NEAREST)
                    sheet = Image.new('RGB', (crop.width + 44, crop.height + 54), '#152029')
                    sheet.paste(crop, (42, 52), crop)
                    draw = ImageDraw.Draw(sheet)
                    label = f'{style} rows {row:02d}–{end_row-1:02d}, columns {column:03d}–{end_col-1:03d} / original pixels at 4×'
                    draw.text((8, 4), label, font=font, fill='#e4e9dc')
                    for c in range(column, end_col):
                        draw.text((44 + (c - column) * 52, 30), f'{c:03d}', font=font, fill='#b5c6c6')
                    for r in range(row, end_row):
                        draw.text((8, 68 + (r - row) * 52), f'{r:02d}', font=font, fill='#b5c6c6')
                    filename = f'{style}-r{row:02d}-{end_row-1:02d}-c{column:03d}-{end_col-1:03d}.png'
                    sheet.save(args.output / filename)
                    ids = [r['id'] for r in records if r['source'] == source and r['kind'] == 'sprite' and
                           column <= r['grid'][0] < end_col and row <= r['grid'][1] < end_row]
                    manifest.append({'file': filename, 'source': source, 'source_sha256': source_hash, 'rect': rect,
                                     'scale': 4, 'ids': ids})
    # Contact sheets of named icons and controls preserve exact per-record crop
    # dimensions. Identified visual variants remain separate stable IDs.
    cache = {}
    for pack in ['godot-node-icons', 'input-prompts']:
        selected = [r for r in records if r['kind'] == 'sprite' and r['pack'] == pack]
        for start in range(0, len(selected), 120):
            batch = selected[start:start + 120]
            sheet = Image.new('RGB', (1200, 1040), '#152029')
            draw = ImageDraw.Draw(sheet)
            draw.text((10, 4), f'{pack} exact crops {start}–{start+len(batch)-1}', font=font, fill='#e4e9dc')
            for i, r in enumerate(batch):
                x, y = (i % 12) * 100 + 10, (i // 12) * 100 + 32
                if r['source'] not in cache:
                    cache[r['source']] = Image.open(ASSETS / r['source']).convert('RGBA')
                sx, sy, w, h = r['rect']
                crop = cache[r['source']].crop((sx, sy, sx + w, sy + h))
                scale = max(1, min(64 // w, 64 // h))
                crop = crop.resize((w * scale, h * scale), Image.Resampling.NEAREST)
                sheet.paste(crop, (x, y), crop)
                draw.text((x, y + 67), str(start + i), font=font, fill='#a1c9c0')
                draw.text((x, y + 81), r['name'][:13], font=font, fill='#c7c5ac')
            filename = f'{pack}-{start:04d}-{start+len(batch)-1:04d}.png'
            sheet.save(args.output / filename)
            manifest.append({'file': filename, 'pack': pack, 'ids': [r['id'] for r in batch],
                             'crops': [{'id': r['id'], 'source': r['source'], 'rect': r['rect'],
                                        'source_sha256': hashlib.sha256((ASSETS / r['source']).read_bytes()).hexdigest()}
                                       for r in batch]})
    (args.output / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
    covered = {sid for page in manifest for sid in page['ids']}
    expected = {r['id'] for r in records if r['kind'] == 'sprite'}
    if covered != expected:
        raise ValueError('Review page coverage differs: ' + str(sorted(expected - covered)))
    print(json.dumps({'pages': len(manifest), 'unique_occupied_crops': len(covered), 'output': str(args.output)}, indent=2))


if __name__ == '__main__':
    main()
