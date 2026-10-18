#!/usr/bin/env python3
"""Read-only sprite coverage + pixel inventory. Python standard library only.

Run from anywhere; generated files go to --output (default docs/sprite-audit).
PNG decoding accepts the checked repository sheets declared by the runtime audit.
"""
import argparse
import base64
import csv
import html
import io
import json
from pathlib import Path
import struct
import subprocess
import zlib

ROOT = Path(__file__).resolve().parents[1]


def pixels(path):
    data = path.read_bytes()
    assert data[:8] == b'\x89PNG\r\n\x1a\n', 'not PNG'
    at, compressed, palette, alpha = 8, bytearray(), [], b''
    while at < len(data):
        size = struct.unpack_from('>I', data, at)[0]
        kind, body = data[at+4:at+8], data[at+8:at+8+size]
        assert len(body) == size
        assert zlib.crc32(kind + body) & 0xffffffff == struct.unpack_from('>I', data, at+8+size)[0]
        at += size + 12
        if kind == b'IHDR':
            width, height, depth, color, compression, filtering, interlace = struct.unpack('>IIBBBBB', body)
            assert compression == filtering == interlace == 0
            assert (color == 6 and depth == 8) or (color == 3 and depth in (1, 2, 4, 8))
        elif kind == b'IDAT':
            compressed.extend(body)
        elif kind == b'PLTE':
            palette = [tuple(body[i:i+3]) for i in range(0, len(body), 3)]
        elif kind == b'tRNS':
            alpha = body
        elif kind == b'IEND':
            break
    channels = 4 if color == 6 else 1
    stride = (width * channels * depth + 7) // 8
    bpp = max(1, channels * depth // 8)
    decoded = zlib.decompress(compressed)
    assert len(decoded) == (stride+1) * height
    prior, output = bytearray(stride), []
    for y in range(height):
        filter_type = decoded[y*(stride+1)]
        row = bytearray(decoded[y*(stride+1)+1:(y+1)*(stride+1)])
        assert filter_type <= 4
        for i in range(stride):
            a = row[i-bpp] if i >= bpp else 0
            b = prior[i]
            c = prior[i-bpp] if i >= bpp else 0
            if filter_type == 1:
                predictor = a
            elif filter_type == 2:
                predictor = b
            elif filter_type == 3:
                predictor = (a+b)//2
            elif filter_type == 4:
                p = a+b-c
                pa, pb, pc = abs(p-a), abs(p-b), abs(p-c)
                predictor = a if pa <= pb and pa <= pc else b if pb <= pc else c
            else:
                predictor = 0
            row[i] = (row[i]+predictor) & 255
        if color == 6:
            output.append([tuple(row[x*4:x*4+4]) for x in range(width)])
        else:
            line = []
            for x in range(width):
                index = row[x*depth//8] >> (8-depth-(x*depth % 8)) & ((1 << depth)-1)
                line.append(palette[index] + (alpha[index] if index < len(alpha) else 255,))
            output.append(line)
        prior = row
    return width, height, output


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=ROOT/'docs/sprite-audit')
    parser.add_argument('--check', action='store_true',
                        help='fail if saved HTML/JSON/CSV differ; writes nothing')
    args = parser.parse_args()
    report = json.loads(subprocess.check_output(['luajit', 'tools/sprite_audit.lua'], cwd=ROOT))
    rows, summaries, sections = [], [], []
    for name, sheet in sorted(report['sheets'].items()):
        path = (ROOT/sheet['path']).resolve()
        assert path.is_relative_to(ROOT/'assets/kenney') and path.suffix == '.png'
        width, height, image = pixels(path)
        assert (width, height) == (sheet['width'], sheet['height'])
        columns, size, stride = sheet['columns'], sheet['tile'], sheet['stride']
        cells = []
        totals = {'grid': 0, 'nonempty': 0, 'empty': 0, 'kenney': 0, 'onebit': 0, 'union': 0}
        for y in range(0, height-size+1, stride):
            for x in range(0, width-size+1, stride):
                index = y//stride*columns+x//stride
                count = sum(1 for yy in range(y, y+size) for xx in range(x, x+size)
                            if image[yy][xx][3] and not (name == 'micro' and image[yy][xx][:3] == (34, 35, 35)))
                bindings = {pack: info['usage'].get(name, {}).get(str(index), [])
                            for pack, info in report['packs'].items()}
                used = {pack: bool(ids) for pack, ids in bindings.items()}
                assert count or not any(used.values()), f'empty source used: {name}/{index}'
                totals['grid'] += 1
                totals['nonempty' if count else 'empty'] += 1
                for pack in used:
                    totals[pack] += int(used[pack])
                totals['union'] += int(any(used.values()))
                rows.append({'sheet': name, 'index': index, 'x': x, 'y': y,
                             'nontransparent_pixels': count,
                             **{pack: '; '.join(ids) for pack, ids in bindings.items()}})
                labels = '\n'.join(pack+': '+', '.join(ids) for pack, ids in bindings.items() if ids)
                state = 'used' if any(used.values()) else 'unused' if count else 'empty'
                cells.append(f'<article data-state="{state}" data-kenney="{str(used["kenney"]).lower()}" '
                             f'data-onebit="{str(used["onebit"]).lower()}" title="{html.escape(labels, quote=True)}">'
                             f'<i class="{name}" style="background-position:-{x*3}px -{y*3}px"></i>'
                             f'<span>{index} · {state}</span><small>{html.escape(labels)}</small></article>')
        summaries.append({'sheet': name, **totals,
                          'unused_union': totals['nonempty']-totals['union'],
                          'unused_kenney': totals['nonempty']-totals['kenney'],
                          'unused_onebit': totals['nonempty']-totals['onebit']})
        sections.append(f'<section><h2>{html.escape(name)} — {totals["nonempty"]} nonempty / '
                        f'{totals["grid"]} grid cells</h2><p>{html.escape(sheet["path"])} · '
                        f'{size}px, stride {stride}. Mixed uses {totals["kenney"]}; '
                        f'1-Bit uses {totals["onebit"]}. Numbering is zero-based.</p>'
                        f'<div class="cells">{"".join(cells)}</div></section>')
    report['pixel_inventory'] = summaries
    outputs = {'inventory.json': json.dumps(report, sort_keys=True, indent=2)+'\n'}
    csv_text = io.StringIO(newline='')
    writer = csv.DictWriter(csv_text, fieldnames=list(rows[0]), lineterminator='\n')
    writer.writeheader()
    writer.writerows(rows)
    outputs['tiles.csv'] = csv_text.getvalue()
    styles = []
    for name, sheet in sorted(report['sheets'].items()):
        encoded = base64.b64encode((ROOT/sheet['path']).read_bytes()).decode()
        styles.append(f'i.{name}{{width:{sheet["tile"]*3}px;height:{sheet["tile"]*3}px;'
                      f'background-image:url(data:image/png;base64,{encoded});'
                      f'background-size:{sheet["width"]*3}px {sheet["height"]*3}px}}')
    document = '''<!doctype html><html lang="en"><meta charset="utf-8"><title>ROAG sprite audit</title>
<style>body{background:#151b18;color:#e5e3d1;font:16px system-ui;margin:24px}a{color:#8cd2c8}
header{position:sticky;top:0;background:#151b18;padding:12px;z-index:2}select{padding:8px}
.cells{display:flex;flex-wrap:wrap;gap:6px}article{width:110px;padding:8px;border:1px solid #526057}
i{display:block;image-rendering:pixelated;background-repeat:no-repeat;background-color:#222323}
span{display:block;font-size:12px}small{display:block;max-height:60px;overflow:auto;font-size:10px;overflow-wrap:anywhere}
[data-state=unused]{border-color:#988644}[data-state=empty]{opacity:.35}[hidden]{display:none}''' + ''.join(styles) + '''</style>
<header><h1>ROAG native sprite inventory</h1>''' + (
        '<p id="coverage">' + ' · '.join(
            f'{html.escape(pack)}: {len(info["bindings"])}/{len(report["requirements"])} bindings, '
            f'{len(info["missing"])} missing' for pack, info in sorted(report['packs'].items())) +
        f' · Mapping checksum: {report["mapping_checksum"]}</p>') + '''<p>Read-only, generated from current mappings and decoded PNG pixels.
Hover/scroll a cell for bindings. “Used” includes partial material sampling; unused art is not an implemented mechanic.</p>
<label>Pack <select id="pack"><option value="union">Both</option><option value="kenney">Kenney mix</option>
<option value="onebit">Standalone 1-Bit</option></select></label>
<label>Show <select id="filter"><option value="all">All</option><option value="unused">Unused nonempty</option>
<option value="used">Referenced</option><option value="empty">Empty</option></select></label>
<a href="inventory.json">JSON</a> · <a href="tiles.csv">CSV</a></header>''' + ''.join(sections) + '''
<script>function filter(){const p=document.querySelector('#pack').value,f=document.querySelector('#filter').value;
document.querySelectorAll('article').forEach(a=>{const empty=a.dataset.state==='empty',
used=p==='union'?a.dataset.state==='used':a.dataset[p]==='true';
a.hidden=f==='used'?!used:f==='unused'?(used||empty):f==='empty'?!empty:false;});}
document.querySelectorAll('select').forEach(s=>s.addEventListener('change',filter));</script></html>'''
    outputs['index.html'] = document
    output = args.output.resolve()
    if args.check:
        stale = [name for name, content in outputs.items()
                 if not (output/name).is_file() or (output/name).read_bytes() != content.encode('utf-8')]
        if stale:
            parser.exit(1, 'Stale sprite catalogue: '+', '.join(stale)+
                        '. Run make sprite-audit and include the regenerated files.\n')
        print('PASS sprite catalogue matches current mappings, pixels and generator (HTML/JSON/CSV)')
        return
    output.mkdir(parents=True, exist_ok=True)
    for name, content in outputs.items():
        (output/name).write_bytes(content.encode('utf-8'))
    print(json.dumps(summaries, indent=2))
    print(f'Coverage, every native cell and offline visual catalogue: {output}')


if __name__ == '__main__':
    main()
