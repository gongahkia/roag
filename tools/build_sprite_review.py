#!/usr/bin/env python3
"""Refresh the review page's embedded PNG sheets for offline/local-server use."""

import base64
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PAGE = ROOT / 'docs/sprite-audit/review.html'
BEGIN = '<!-- BEGIN EMBEDDED SPRITE SHEETS -->'
END = '<!-- END EMBEDDED SPRITE SHEETS -->'
SHEETS = {
    'micro': ROOT / 'assets/kenney/micro-roguelike/tiles.png',
    'caves': ROOT / 'assets/kenney/caves-dungeons/terrain.png',
    'onebit': ROOT / 'assets/kenney/1-bit-pack/symbols.png',
    'rpg': ROOT / 'docs/sprite-audit/review-art/roguelike-rpg-sheet.png',
    'tiny': ROOT / 'docs/sprite-audit/review-art/tiny-dungeon-tilemap.png',
}


def main():
    html = PAGE.read_text()
    assert html.count(BEGIN) == html.count(END) == 1
    before, _, rest = html.partition(BEGIN)
    _, _, after = rest.partition(END)
    rules = []
    for name, path in SHEETS.items():
        image = path.read_bytes()
        assert image.startswith(b'\x89PNG\r\n\x1a\n'), path
        encoded = base64.b64encode(image).decode('ascii')
        rules.append(f'--{name}-sheet:url("data:image/png;base64,{encoded}");')
    embedded = f'{BEGIN}\n  <style id="embedded-sprite-sheets">:root{{{"".join(rules)}}}</style>\n  {END}'
    PAGE.write_text(before + embedded + after)
    print(f'Embedded {len(SHEETS)} PNG sheets in {PAGE.relative_to(ROOT)}')


if __name__ == '__main__':
    main()
