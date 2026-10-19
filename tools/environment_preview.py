#!/usr/bin/env python3
"""Offline source-pixel evidence. No LÖVE process, GPU, extra Python packages.

The Lua half executes the actual main.lua world pass and native compositors.
The CPU rasterizer is intentionally limited: no text or GPU antialiasing.
"""
import argparse
import hashlib
import html
import json
from pathlib import Path
import struct
import subprocess
import tempfile
import zlib
from sprite_audit import pixels, ROOT


def png(w, h, rgba):
    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind+data)&0xffffffff)
    rows = b''.join(b'\0'+rgba[y*w*4:(y+1)*w*4] for y in range(h))
    return b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w,h,8,6,0,0,0)) + chunk(b'IDAT',zlib.compress(rows,9)) + chunk(b'IEND',b'')


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check',action='store_true')
    args=parser.parse_args()
    target=ROOT/'docs/visuals/environments'
    paths=['assets/kenney/micro-roguelike/tiles.png','assets/kenney/caves-dungeons/terrain.png',
           'assets/kenney/1-bit-pack/symbols.png','assets/kenney/approved/tiny-dungeon.png',
           'assets/kenney/approved/roguelike-rpg.png']
    hashes={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in paths}
    with tempfile.TemporaryDirectory(prefix='roag-environment-pixels-',dir='/var/tmp') as tmp:
        temp=Path(tmp)
        for i,p in enumerate(paths):
            w,h,rows=pixels(ROOT/p)
            (temp/f'{i+1}.rgba').write_bytes(bytes(v for row in rows for pixel in row for v in pixel))
        subprocess.run(['luajit','tools/environment_preview.lua',str(temp)],cwd=ROOT,check=True)
        entries=json.loads((temp/'index.json').read_text())
        files={}
        for e in entries:
            raw=(temp/(e['name']+'.rgba')).read_bytes()
            assert len(raw)==e['width']*e['height']*4
            files[e['name']+'.png']=png(e['width'],e['height'],raw)
        cards=''.join(f'<section><h2>{html.escape(e["label"])}</h2><p>{html.escape(e["detail"])}</p><img src="{e["name"]}.png" alt="{html.escape(e["label"])}" loading="lazy"></section>' for e in entries)
        page='''<!doctype html><html lang="en"><meta charset="utf-8"><title>ROAG approved environments</title>
<style>body{background:#101716;color:#e5e9dc;font:16px system-ui;margin:28px;max-width:1100px}section{border-top:1px solid #50645c;margin:28px 0}img{image-rendering:pixelated;max-width:100%;background:#111}p{line-height:1.5}code{color:#fe9}</style>
<h1>Approved environments — source-pixel verification</h1>
<p>These images execute the real main.lua world draw pass, native image resolvers and derived material compositors using a CPU raster adapter. They are <strong>not LÖVE screenshots or a human playtest</strong>. Text is omitted, GPU antialiasing is not reproduced, and spectator visibility is enabled to inspect art. The original generated maps, entities, RNG and saves remain unchanged.</p>
<p>In the game choose Settings → Sprite Pack → Kenney mix or Kenney 1-Bit. Classic keeps its original shapes. F5 refreshes presentation. No new biome or boon is introduced. Reproduce without windows: <code>python3 tools/environment_preview.py</code>; verify byte identity: add <code>--check</code>.</p>'''+cards+'</html>\n'
        files['index.html']=page.encode()
        files['evidence.json']=(json.dumps({'method':'actual main.lua world pass; offline CPU raster; text omitted; spectator fixture',
            'source_sha256':hashes,'views':entries},sort_keys=True,indent=2)+'\n').encode()
        for p,digest in hashes.items():
            assert hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==digest,'source art changed'
        if not args.check:
            target.mkdir(parents=True,exist_ok=True)
        for name,data in files.items():
            if args.check:
                assert (target/name).read_bytes()==data,'stale environment preview: '+name
            else:
                (target/name).write_bytes(data)
        print(f'{len(entries)} real-render-path CPU pixel views; source PNGs unchanged; '+('byte-identical' if args.check else str(target/'index.html')))


if __name__=='__main__':
    main()
