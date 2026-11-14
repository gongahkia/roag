#!/usr/bin/env python3
"""Read-only verifier for this frozen audit; never generates or migrates assets."""
import gzip
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import sys

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
AUDIT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tools'))
from asset_catalog import Png


def digest(data):
    return hashlib.sha256(data).hexdigest()


class Image:
    def __init__(self, path):
        raw = path.read_bytes()
        self.width, self.height = struct.unpack('>II', raw[16:24])
        try:
            self.rgba = bytes(Png(path).rgba)
        except ValueError:
            self.rgba = subprocess.check_output(['magick', str(path), '-depth', '8', 'rgba:-'])
        assert len(self.rgba) == self.width * self.height * 4

    def crop(self, rect):
        x, y, width, height = rect
        assert min(rect) >= 0 and x + width <= self.width and y + height <= self.height
        return b''.join(self.rgba[((y+j)*self.width+x)*4:((y+j)*self.width+x+width)*4]
                        for j in range(height))


def main():
    with gzip.open(AUDIT / 'inventory.json.gz', 'rt') as stream:
        inventory = json.load(stream)
    frames = {row['audit_id']: row for row in inventory['frames']}
    assert len(frames) == len(inventory['frames']) == 14356
    images = {}
    for source in inventory['sources']:
        path = ROOT / source['path']
        assert path.is_file(), str(path)
        assert digest(path.read_bytes()) == source['sha256'], str(path)
        if path.suffix == '.png':
            images[source['path']] = Image(path)
            assert [images[source['path']].width, images[source['path']].height] == source['native_size']
    for row in frames.values():
        assert digest(images[row['source']].crop(row['rect'])) == row['rgba_sha256'], row['audit_id']
        for page in row['contact_sheets']:
            assert (AUDIT / page).is_file(), page
    pages = json.loads((AUDIT / 'sheet-index.json').read_text())
    labelled = 0
    for page in pages:
        sheet = Image(AUDIT / page['path'])
        cards = []
        for cell in page['entries']:
            row = frames[cell['audit_id']]
            assert cell['label_id'] == row['label_id']
            assert page['path'] in row['contact_sheets']
            x, y, width, height = cell['card_rect']
            assert x >= 0 and y >= 0 and x + width <= sheet.width and y + height <= sheet.height
            assert all(x + width <= a or a + w <= x or y + height <= b or b + h <= y
                       for a, b, w, h in cards)
            cards.append(cell['card_rect'])
            # Opaque source pixels must survive exactly in both displayed scales.
            original = images[row['source']].crop(row['rect'])
            native = sheet.crop(cell['native_preview'])
            assert len(original) == len(native)
            for i in range(0, len(original), 4):
                if original[i+3] == 255:
                    assert native[i:i+3] == original[i:i+3], (row['audit_id'], 'native pixel')
            zoom = sheet.crop(cell['enlarged_preview'])
            size = row['native_size']; scale = cell['enlargement']
            for sy in range(size[1]):
                for sx in range(size[0]):
                    at = (sy * size[0] + sx) * 4
                    if original[at+3] == 255:
                        for dy in range(scale):
                            for dx in range(scale):
                                q = ((sy*scale+dy)*size[0]*scale+sx*scale+dx)*4
                                assert zoom[q:q+3] == original[at:at+3], (row['audit_id'], 'nearest pixel')
            labelled += 1
    layout = json.loads((AUDIT / 'assembly-layout.json').read_text())
    assert all(row['audit_id'] in frames for panel in layout['panels'] for row in panel['cells'])
    assert len(layout['asset_ids']) == 9 and len(layout['panels']) == 3
    assert digest((ROOT / 'README.md').read_bytes()) == '3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a'
    print(f'PASS: {len(inventory["sources"])} sources, {len(frames)} exact crops, '
          f'{len(pages)} sheets / {labelled} labelled appearances, nine assembly assets, README hash.')


if __name__ == '__main__':
    main()
