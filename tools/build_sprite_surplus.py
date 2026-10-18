#!/usr/bin/env python3
"""Build a read-only, offline surplus shortlist from checked current artwork."""
import argparse
import base64
import hashlib
import html
import json
from pathlib import Path
import subprocess

from sprite_audit import pixels

ROOT = Path(__file__).resolve().parents[1]
DIRECTORY = ROOT/'docs/sprite-audit'
SOURCE = DIRECTORY/'surplus-candidates.json'
LABELS = {'micro':'Micro Roguelike', 'caves':'Caves & Dungeons',
          'onebit':'1-Bit', 'rpg':'Roguelike/RPG', 'tiny':'Tiny Dungeon'}


def build():
    data = json.loads(SOURCE.read_text())
    assert data['schema'] == 'roag-sprite-surplus-candidates/1'
    live = json.loads(subprocess.check_output(['luajit','tools/sprite_audit.lua'], cwd=ROOT))
    saved = json.loads((DIRECTORY/'inventory.json').read_text())
    assert saved['mapping_checksum'] == live['mapping_checksum'], 'Run make sprite-audit first'
    # The native mixed resolver loads five sources; the independent onebit
    # resolver reads symbols.png only. Unloaded sheets are not its spare art.
    loaded = {'kenney': sorted(live['sheets']), 'onebit':['onebit']}
    sources, decoded = {}, {}
    for name, meta in sorted(live['sheets'].items()):
        path = (ROOT/meta['path']).resolve()
        assert path.is_relative_to(ROOT/'assets/kenney') and path.suffix == '.png'
        width,height,rgba = pixels(path)
        assert (width,height) == (meta['width'],meta['height'])
        decoded[name] = rgba
        sources[name] = {**meta,'label':LABELS[name],
                         'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
    stats = {}
    for pack, names in {'union':sorted(sources),**loaded}.items():
        rows = [s for s in saved['pixel_inventory'] if s['sheet'] in names]
        stats[pack] = {'sheets':len(names),
            'nonempty':sum(s['nonempty'] for s in rows),
            'referenced':sum(s[pack] for s in rows)}
        stats[pack]['unused'] = stats[pack]['nonempty']-stats[pack]['referenced']
    ids, references = set(), {}
    for card in data['cards']:
        assert card['id'] not in ids and card['status'] == 'proposal'
        assert card['kind'] in ('visual','mechanic','enemy','expansion')
        ids.add(card['id'])
        for pack in loaded:
            for group in card[pack]:
                name = group['sheet']
                assert name in loaded[pack], f'{pack} cannot use unloaded source {name}'
                meta,rgba = sources[name],decoded[name]
                for index in group['indices']:
                    assert isinstance(index,int) and index>=0
                    x,y=index%meta['columns']*meta['stride'],index//meta['columns']*meta['stride']
                    assert x+meta['tile']<=meta['width'] and y+meta['tile']<=meta['height']
                    count=sum(1 for yy in range(y,y+meta['tile']) for xx in range(x,x+meta['tile'])
                              if rgba[yy][xx][3] and not (name=='micro' and rgba[yy][xx][:3]==(34,35,35)))
                    assert count>5,f'Empty or unusable candidate {name}:{index}'
                    for current in live['packs'].values():
                        assert not current['usage'].get(name,{}).get(str(index)), \
                            f'Candidate is already assigned: {name}:{index}; curate shortlist'
                    key=f'{name}:{index}'
                    references.setdefault(key,set()).add(card['id'])
    data.update(schema='roag-sprite-surplus/1',mapping_checksum=live['mapping_checksum'],
                sources=sources,loaded_sheets=loaded,summary=stats,
                sheet_inventory=saved['pixel_inventory'],
                candidate_reuse={k:sorted(v) for k,v in sorted(references.items()) if len(v)>1})
    data['fingerprint']=hashlib.sha256(json.dumps(data,sort_keys=True).encode()).hexdigest()
    css=[]
    for name,meta in sources.items():
        raw=(ROOT/meta['path']).read_bytes()
        encoded=base64.b64encode(raw).decode()
        scale=64/meta['tile']
        css.append(f'.sprite.{name}{{background-image:url(data:image/png;base64,{encoded});'
                   f'background-size:{meta["width"]*scale:g}px {meta["height"]*scale:g}px}}')
    def escape(value): return html.escape(str(value),quote=True)
    cards=[]
    for card in sorted(data['cards'],key=lambda c:(c['priority'],c['id'])):
        art=[]
        for pack in loaded:
            groups=[]
            for group in card[pack]:
                name=group['sheet'];meta=sources[name];scale=64/meta['tile']
                figures=[]
                for index in group['indices']:
                    x,y=index%meta['columns']*meta['stride']*scale,index//meta['columns']*meta['stride']*scale
                    key=f'{name}:{index}'
                    reused=data['candidate_reuse'].get(key,[])
                    note='Also offered in other ideas; not allocated.' if len(reused)>1 else 'Unused in both choices.'
                    figures.append(f'<a class="tile" href="index.html#cell-{name}-{index}" title="{note}">'
                        f'<span class="sprite {name}" style="background-position:-{x:g}px -{y:g}px"></span>'
                        f'<span>{name} #{index}</span></a>')
                groups.append(f'<p>{escape(group["label"])}</p><div class="tiles">{"".join(figures)}</div>')
            title='Mixed Kenney' if pack=='kenney' else 'Standalone 1-Bit'
            art.append(f'<section class="art" data-pack="{pack}"><h3>{title}</h3>{"".join(groups)}</section>')
        search=escape(' '.join(str(card[k]) for k in ('title','kind','idea','already','decisions')).lower())
        cards.append(f'<article id="{card["id"]}" data-kind="{card["kind"]}" data-search="{search}">'
            f'<div class="tags">{escape(card["kind"])} · effort: {escape(card["effort"])} · unassigned candidates</div>'
            f'<h2>{escape(card["title"])}</h2><p>{escape(card["idea"])}</p>'
            f'<p><strong>Existing game:</strong> {escape(card["already"])}</p>'
            f'<p><strong>Decision needed:</strong> {escape(card["decisions"])}</p>'
            f'<div class="looks">{"".join(art)}</div></article>')
    table=''.join(f'<tr><td>{escape(LABELS[s["sheet"]])}</td><td>{s["nonempty"]:,}</td>'
                  f'<td>{s["kenney"]:,}</td><td>{s["onebit"] if s["sheet"]=="onebit" else "not loaded"}</td>'
                  f'<td>{s["unused_union"]:,}</td></tr>' for s in data['sheet_inventory'])
    decisions=''.join(f'<li><strong>{escape(d["topic"])}</strong> — {escape(d["detail"])}</li>'
                      for d in data['prior_decisions'])
    payload=json.dumps(data,ensure_ascii=False,separators=(',',':')).replace('<','\\u003c')
    page='''<!doctype html><html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1"><title>ROAG unused art opportunities</title>
<style>body{margin:0;background:#151b18;color:#e5e3d1;font:16px/1.5 system-ui}main{max-width:1180px;margin:auto;padding:24px}
a{color:#9ce0cb}header{border-bottom:1px solid #64776b;padding-bottom:18px}h1,h2,h3{line-height:1.2}h1{font-size:32px}
small,.tags{color:#b1bcae}.tags{font-size:13px;text-transform:uppercase}table{border-collapse:collapse;width:100%}td,th{text-align:left;padding:8px;border-bottom:1px solid #425348}th{color:#b6d9c7}.scroll{overflow:auto}
article{padding:20px;margin:20px 0;border:1px solid #526057;border-radius:10px;scroll-margin-top:20px}article p{max-width:95ch}
.looks{display:grid;grid-template-columns:1fr 1fr;gap:18px}.art{min-width:0;background:#202923;padding:16px;border-radius:7px}
.tiles{display:flex;flex-wrap:wrap;gap:8px}.tile{width:84px;background:#151b18;border:1px solid #526057;text-decoration:none;font-size:11px;text-align:center;padding:7px 2px}.tile:hover{border-color:#d1c87e}.sprite{display:block;width:64px;height:64px;margin:0 auto 5px;background-repeat:no-repeat;image-rendering:pixelated;background-color:#222323}
.controls{display:flex;flex-wrap:wrap;gap:14px;padding:18px 0}select,input{padding:10px;background:#202923;color:inherit;border:1px solid #718779;font:inherit;max-width:100%;box-sizing:border-box}input{width:250px}label{display:flex;flex-direction:column;gap:5px}.note{padding:14px 20px;border-left:3px solid #c7b662;background:#252a20}[hidden]{display:none!important}@@CSS@@
@media(max-width:700px){main{padding:14px}h1{font-size:26px}.looks{grid-template-columns:1fr}article{padding:13px}.art{padding:12px}table{font-size:13px}td,th{padding:6px}}
</style></head><body><main><header><h1>Unused art: what can ROAG use next?</h1>
<p>Current source audit · <strong>@@UNUSED@@ nonempty source cells unused by either look</strong>.
Counts include fragments and variants. They are not a count of ready-made new mechanics.</p>
<p>@@MAPPING@@ · @@REQUIREMENTS@@ live sprite bindings per choice.</p>
<p><a href="index.html">Every source cell</a> · <a href="surplus.json">Checked JSON</a> · <a href="surplus.md">Reasoning and recommendation</a> · <a href="review-feedback.md">Earlier decisions</a></p></header>
<div class="scroll"><table><thead><tr><th>Source</th><th>Nonempty</th><th>Mixed uses</th><th>1-Bit uses</th><th>Unused by either</th></tr></thead><tbody>@@TABLE@@</tbody></table></div>
<p>Mixed loads all five sheets: <strong>@@MIXED_UNUSED@@ unused</strong> of @@MIXED_TOTAL@@ nonempty cells. Standalone loads its 1-Bit sheet only: <strong>@@ONEBIT_UNUSED@@ unused</strong> of @@ONEBIT_TOTAL@@. Those totals overlap; do not add them. All candidates below are checked unused in both current looks.</p>
<div class="note"><strong>Prior decisions preserved</strong><ul>@@DECISIONS@@</ul></div>
<p>These are proposals for discussion. Motif labels are visual readings; borrowed candidates appearing in several ideas are alternatives, not reserved assignments. Native pixels are enlarged to 64px without trimming or new artwork.</p>
<div class="controls"><label>Look<select id="pack"><option value="both">Both looks</option><option value="kenney">Mixed Kenney</option><option value="onebit">Standalone 1-Bit</option></select></label>
<label>Type<select id="kind"><option value="all">All directions</option><option value="visual">Presentation first</option><option value="mechanic">Room interaction</option><option value="enemy">Enemy idea</option><option value="expansion">Larger biome expansion</option></select></label>
<label>Search<input id="search" placeholder="e.g. crypt, HP, chest"></label></div><p id="count" role="status"></p>
<div id="cards">@@CARDS@@</div><script id="surplus-data" type="application/json">@@DATA@@</script>
<script>function filter(){const p=document.querySelector('#pack').value,k=document.querySelector('#kind').value,q=document.querySelector('#search').value.toLowerCase();let count=0;
document.querySelectorAll('article').forEach(a=>{a.hidden=(k!=='all'&&a.dataset.kind!==k)||!a.dataset.search.includes(q);if(!a.hidden)count++;});
document.querySelectorAll('.art').forEach(a=>a.hidden=p!=='both'&&a.dataset.pack!==p);
document.querySelector('#count').textContent=count+' of '+document.querySelectorAll('article').length+' directions shown';}
document.querySelectorAll('select,input').forEach(e=>e.addEventListener('input',filter));filter();</script></main></body></html>'''
    for key,value in {'CSS':''.join(css),'MAPPING':escape('Mapping '+live['mapping_checksum']),
                      'UNUSED':f'{stats["union"]["unused"]:,}',
                      'REQUIREMENTS':str(len(live['requirements'])),
                      'MIXED_UNUSED':f'{stats["kenney"]["unused"]:,}',
                      'MIXED_TOTAL':f'{stats["kenney"]["nonempty"]:,}',
                      'ONEBIT_UNUSED':f'{stats["onebit"]["unused"]:,}',
                      'ONEBIT_TOTAL':f'{stats["onebit"]["nonempty"]:,}',
                      'TABLE':table,'DECISIONS':decisions,'CARDS':''.join(cards),'DATA':payload}.items():
        page=page.replace('@@'+key+'@@',value)
    return {'surplus.html':page,'surplus.json':json.dumps(data,ensure_ascii=False,sort_keys=True,indent=2)+'\n'}


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check',action='store_true')
    args=parser.parse_args()
    outputs=build()
    for name,content in outputs.items():
        path=DIRECTORY/name
        if args.check:
            assert path.is_file() and path.read_bytes()==content.encode(),f'Stale {name}; run make surplus-audit'
        else:path.write_bytes(content.encode())
    print('PASS surplus shortlist: live source usage, candidate pixels, two-look compatibility and generated files')


if __name__=='__main__':main()
