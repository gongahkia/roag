#!/usr/bin/env python3
"""Build the offline idea/assignment review; never publish or edit game data."""
import argparse
import base64
import hashlib
import json
from pathlib import Path
import re
import subprocess

from sprite_audit import pixels
from sprite_idea_proposals import proposals

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs/sprite-audit'
LABELS = {'micro':'Micro Roguelike', 'caves':'Caves & Dungeons',
          'onebit':'1-Bit', 'rpg':'Roguelike/RPG', 'tiny':'Tiny Dungeon'}
KINDS = {'enemy','class','item','room','terrain','environment','presentation','held'}


def build():
    live = json.loads(subprocess.check_output(['luajit','tools/sprite_audit.lua'], cwd=ROOT))
    inventory = json.loads((OUT/'inventory.json').read_text())
    assert inventory['mapping_checksum'] == live['mapping_checksum'], 'Run make sprite-audit first'
    sources, cells = {}, {}
    for name, meta in sorted(live['sheets'].items()):
        path = (ROOT/meta['path']).resolve()
        assert path.is_relative_to(ROOT/'assets/kenney') and path.suffix == '.png'
        raw = path.read_bytes()
        width,height,rgba = pixels(path)
        assert (width,height) == (meta['width'],meta['height'])
        sources[name] = {**meta, 'label':LABELS[name], 'sha256':hashlib.sha256(raw).hexdigest()}
        for y in range(0,height-meta['tile']+1,meta['stride']):
            for x in range(0,width-meta['tile']+1,meta['stride']):
                index = y//meta['stride']*meta['columns']+x//meta['stride']
                count = sum(bool(rgba[yy][xx][3]) and not
                            (name=='micro' and rgba[yy][xx][:3]==(34,35,35))
                            for yy in range(y,y+meta['tile']) for xx in range(x,x+meta['tile']))
                used = any(p['usage'].get(name,{}).get(str(index)) for p in live['packs'].values())
                if count and not used:
                    cells[f'{name}:{index}'] = dict(owner=None, proposals=[], nontransparent_pixels=count)
    expected = sum(s['unused_union'] for s in inventory['pixel_inventory'])
    assert len(cells) == expected
    cards = proposals()
    ids = set()
    held = set()
    for card in cards:
        assert re.fullmatch('[a-z][a-z0-9-]+',card['id']) and card['id'] not in ids
        assert card['kind'] in KINDS and card['excluded'] == (card['kind']=='held')
        ids.add(card['id'])
        groups = []
        for group in card['groups']:
            meta = sources[group['sheet']]
            selected = []
            assert len(group['indices']) == len(set(group['indices']))
            for index in group['indices']:
                assert type(index) is int and index>=0
                x,y = index%meta['columns']*meta['stride'], index//meta['columns']*meta['stride']
                assert x+meta['tile']<=meta['width'] and y+meta['tile']<=meta['height']
                key = f'{group["sheet"]}:{index}'
                if key not in cells or (key in held and not card['excluded']):
                    continue
                cell = cells[key]
                if card['remainder'] and cell['owner'] and not group.get('counterpart'):
                    continue
                selected.append(key)
                if card['excluded']: held.add(key)
                if not cell['owner']: cell['owner'] = card['id']
                if card['id'] not in cell['proposals']:cell['proposals'].append(card['id'])
            if selected:
                groups.append({k:group[k] for k in ('sheet','label','role')} | {'cells':selected})
        if not groups and card['remainder']:
            card['groups'] = []
            continue
        assert groups, f'Proposal has no unused candidate art: {card["id"]}'
        card['groups'] = groups
        card['owned_count'] = sum(c['owner']==card['id'] for c in cells.values())
        card['candidate_count'] = len({k for g in groups for k in g['cells']})
        card['art'] = {'kenney':groups, 'onebit':[g for g in groups if g['sheet']=='onebit']}
        card['needs_counterpart'] = not card['art']['onebit']
        del card['remainder']
    cards = [c for c in cards if c['groups']]
    assert all(c['owner'] for c in cells.values()), 'Unaccounted unused cells: '+str([k for k,v in cells.items() if not v['owner']])
    assert all(set(c['dependencies']).issubset(ids) for c in cards)
    count = len([c for c in cards if not c['excluded']])
    data = dict(schema='roag-sprite-ideas/1',mapping_checksum=live['mapping_checksum'],
                requirements=len(live['requirements']),sources=sources,cards=cards,cells=cells,
                earlier_decisions=json.loads((OUT/'surplus-candidates.json').read_text())['prior_decisions'],
                summary=dict(proposals=count,held_groups=len(cards)-count,unused=len(cells),
                             proposed_cells=len(cells)-len(held),held_cells=len(held),
                             needs_counterpart=sum(c['needs_counterpart'] for c in cards if not c['excluded']),
                             categories={k:sum(c['kind']==k for c in cards) for k in sorted(KINDS)}))
    data['fingerprint'] = hashlib.sha256(json.dumps(data,sort_keys=True,separators=(',',':')).encode()).hexdigest()
    payload = json.loads(json.dumps(data))
    for name, source in payload['sources'].items():
        source['image'] = 'data:image/png;base64,'+base64.b64encode((ROOT/source['path']).read_bytes()).decode()
    template = (ROOT/'tools/sprite_ideas_template.html').read_text()
    client = (ROOT/'tools/sprite_ideas_client.js').read_text()
    assert '</script' not in client.lower()
    html = template.replace('@@DATA@@',json.dumps(payload,ensure_ascii=False,separators=(',',':')).replace('<','\\u003c')).replace('@@CLIENT@@',client)
    return {'ideas.json':json.dumps(data,ensure_ascii=False,sort_keys=True,indent=2)+'\n', 'ideas.html':html}, data


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check',action='store_true')
    args=parser.parse_args()
    # Applying approved art deliberately makes the original "unused" snapshot
    # historical. Never regenerate it and reinterpret the user's old votes.
    from sprite_ideas_archive import RECORD, verify
    if RECORD.is_file() and json.loads(RECORD.read_bytes()).get('review_inventory'):
        verify()
        return
    outputs,data=build()
    for name,value in outputs.items():
        path=OUT/name
        if args.check:
            assert path.is_file() and path.read_bytes()==value.encode(),f'Stale {name}; run make sprite-ideas'
        else:path.write_bytes(value.encode())
    print('PASS idea review: '+json.dumps(data['summary'],sort_keys=True))


if __name__=='__main__':main()
