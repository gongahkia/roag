#!/usr/bin/env python3
"""Read-only review of verified inventory crops; never changes source/catalogue."""
import base64
import gzip
import html
import json
import sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT/'tools'))
from asset_catalog import Png, encode_rgba_png
out = ROOT/'docs/world-builder/evidence/goal-09'
out.mkdir(parents=True, exist_ok=True)
frames = {r['audit_id']: r for r in json.load(gzip.open(ROOT/'docs/world-builder/inventory.json.gz'))['frames']}
ids = ['urizen-onebit-r06-c053','urizen-onebit-r05-c027','urizen-onebit-r05-c030','urizen-onebit-r33-c003','urizen-onebit-r33-c002','urizen-onebit-r42-c000','urizen-onebit-r16-c003','urizen-onebit-r47-c006']
images = {};records = [];cards = []
for i in ids:
    f=frames[i];source=f['source'];images.setdefault(source,Png(ROOT/source))
    rgba=images[source].crop(f['rect'])
    width,height=f['native_size']
    data=encode_rgba_png(width,height,rgba)
    records.append({k:f[k] for k in ['audit_id','source','native_size','rect','rgba_sha256','foreground_mask_sha256','name','publisher_identity','historical_aliases','active_roles','animation','provenance']})
    url='data:image/png;base64,'+base64.b64encode(data).decode()
    cards.append(f'<article><b>{html.escape(i)}</b><p>{html.escape(f["name"])}</p><div><img src="{url}" width="12" height="12"><img src="{url}" width="72" height="72"></div><small>{f["rect"]}<br>mask {f["foreground_mask_sha256"][:12]}</small></article>')
page='''<!doctype html><meta charset="utf-8"><style>*{box-sizing:border-box}body{margin:16px;background:#202622;color:#e8dfbf;font:12px monospace}h1{font-size:18px}main{display:grid;grid-template-columns:repeat(5,190px);gap:8px}article{height:188px;padding:8px;border:1px solid #64705b}b{font-size:11px}p{height:34px;font-size:10px}article div{display:flex;gap:12px;align-items:center;height:78px;background:#64705b}img{image-rendering:pixelated}small{font-size:9px}</style><h1>Goal 09: verified preparedness, refuge/inn, fire and rabbit candidates</h1><p>Source RGBA at 1× and 6×; no invented animation order or species assertion. Labels resolve to the unchanged inventory.</p><main>'''+''.join(cards)+'</main>'
(out/'sprite-review.html').write_text(page)
(out/'review-crops.json').write_text(json.dumps(records,indent=2)+'\n')
print(f'Reviewed {len(ids)} recorded crops from {len(images)} original sheets.')
