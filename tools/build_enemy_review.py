#!/usr/bin/env python3
"""Build the self-contained, approval-only enemy combination review."""
import argparse
import base64
import copy
import hashlib
import json
from pathlib import Path
import subprocess

if __package__:
    from .sprite_audit import pixels
else:
    from sprite_audit import pixels

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT/'docs/sprite-audit/enemy-review.html'
SOURCE = ROOT/'docs/sprite-audit/enemy-combos.json'
TEMPLATE = ROOT/'tools/enemy_review_template.html'
CLIENT = ROOT/'tools/enemy_review_client.js'
ACCEPTANCE = ROOT/'docs/sprite-audit/enemy-combo-acceptance.json'


def accepted_archive():
    """Preserve the exact page the user reviewed after its candidates go live."""
    record = json.loads(ACCEPTANCE.read_text())
    assert record['schema'] == 'roag-enemy-combo-acceptance/1'
    for path, checksum in {**record['files'], **record['sources']}.items():
        assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest() == checksum, \
            f'Accepted review archive/source changed: {path}'
    feedback = ROOT/'docs/sprite-audit/enemy-combo-feedback.json'
    assert hashlib.sha256(feedback.read_bytes()).hexdigest() == record['feedback_sha256']
    subprocess.run(['node', '-e', """
const fs=require('fs');
const API=require('./tools/enemy_review_client.js');
const html=fs.readFileSync('docs/sprite-audit/enemy-review.html','utf8');
const data=JSON.parse(html.match(/<script id="enemy-data" type="application\\/json">([^<]+)<\\/script>/)[1]);
const state=API.checked(data,JSON.parse(fs.readFileSync('docs/sprite-audit/enemy-combo-feedback.json','utf8')));
if(API.progress(state).approved!==30)throw Error('Incomplete approval');
"""], cwd=ROOT, check=True)
    print('PASS accepted review archive and 30/30 approved user decisions preserved; live catalogue is separate')


def review_data():
    proposal = json.loads(SOURCE.read_text())
    assert proposal['schema'] == 'roag-enemy-combo-proposals/1'
    assert len(proposal['combos']) == 5 and len(proposal['roles']) == 10
    live = json.loads(subprocess.check_output(['luajit', 'tools/sprite_audit.lua'], cwd=ROOT))
    saved = json.loads((ROOT/'docs/sprite-audit/inventory.json').read_text())
    assert saved['mapping_checksum'] == live['mapping_checksum'], 'Run make sprite-audit first'
    roles = {role['id']: role for role in proposal['roles']}
    assert len(roles) == 10 and len({c['id'] for c in proposal['combos']}) == 5
    sheet_labels = {'micro':'Micro Roguelike', 'caves':'Caves & Dungeons',
                    'onebit':'1-Bit', 'rpg':'Roguelike/RPG', 'tiny':'Tiny Dungeon'}
    sources, decoded = {}, {}
    for name, meta in live['sheets'].items():
        path = ROOT/meta['path']
        raw = path.read_bytes()
        width, height, rgba = pixels(path)
        assert (width, height) == (meta['width'], meta['height'])
        decoded[name] = rgba
        sources[name] = {**meta, 'label':sheet_labels[name],
                         'sha256':hashlib.sha256(raw).hexdigest(),
                         'image':'data:image/png;base64,'+base64.b64encode(raw).decode()}
    data = copy.deepcopy(proposal)
    appearances = {}

    def candidate(cell, owner):
        sheet, index = cell['sheet'], cell['index']
        meta = sources[sheet]
        assert isinstance(index, int) and index >= 0
        x = index % meta['columns'] * meta['stride']
        y = index // meta['columns'] * meta['stride']
        assert x+meta['tile'] <= meta['width'] and y+meta['tile'] <= meta['height']
        rgba = decoded[sheet]
        count = sum(1 for yy in range(y, y+meta['tile'])
                    for xx in range(x, x+meta['tile'])
                    if rgba[yy][xx][3] and not (sheet == 'micro' and rgba[yy][xx][:3] == (34,35,35)))
        assert count > 5, f'Empty/unusable candidate {sheet}:{index}'
        uses = {pack: info['usage'].get(sheet, {}).get(str(index), [])
                for pack, info in live['packs'].items()}
        assert not any(uses.values()), f'Candidate already bound: {sheet}:{index} {uses}'
        cell.update({'key':f'{sheet}:{index}', 'nontransparent_pixels':count,
                     'runtime_uses':uses})
        appearances.setdefault(cell['key'], set()).add(owner)

    primary = {'kenney':set(), 'onebit':set()}
    for role in data['roles']:
        for pack in ('kenney', 'onebit'):
            options = role['candidates'][pack]
            assert options and len({(c['sheet'],c['index']) for c in options}) == len(options)
            for cell in options:
                assert (pack != 'onebit' or cell['sheet'] == 'onebit'), 'Standalone art must stay independent'
                candidate(cell, role['id'])
            assert options[0]['key'] not in primary[pack], 'Primary actor silhouettes must be distinct'
            primary[pack].add(options[0]['key'])
    assigned = []
    for combo in data['combos']:
        assert len(combo['roles']) == 2 and all(role in roles for role in combo['roles'])
        assigned.extend(combo['roles'])
        assert all(p['role'] in combo['roles'] and 0 <= p['x'] < 8 and 0 <= p['y'] < 5
                   for p in combo['roster'])
        assert len({(p['x'],p['y']) for p in combo['roster']}) == len(combo['roster'])
        for cell in combo['cue']['candidates']:
            candidate(cell, combo['id']+' cue')
    assert len(set(assigned)) == 10 and len(assigned) == 10
    for role in data['roles']:
        for options in role['candidates'].values():
            for cell in options:
                cell['other_proposals'] = sorted(appearances[cell['key']] - {role['id']})
    for combo in data['combos']:
        for cell in combo['cue']['candidates']:
            cell['other_proposals'] = sorted(appearances[cell['key']] - {combo['id']+' cue'})
    data['sources'] = sources
    data['audit'] = {'mapping_checksum':live['mapping_checksum'],
                     'requirements':len(live['requirements']),
                     'packs':{pack:{'resolved':len(info['bindings']), 'missing':len(info['missing'])}
                              for pack, info in live['packs'].items()},
                     'sheets':saved['pixel_inventory']}
    data['references'] = []
    for pack, ids in {'kenney':['core:actor/adventurer','core:actor/hookrunner'],
                      'onebit':['core:actor/adventurer']}.items():
        for identity in ids:
            binding = next(b for b in live['packs'][pack]['bindings'] if b['id'] == identity)
            data['references'].append({'pack':pack,'id':identity,'sheet':binding['sheet'],
                                       'index':binding['index']})
    identity = {'proposal':proposal,'sources':{name:meta['sha256'] for name,meta in sources.items()},
                'mapping_checksum':live['mapping_checksum']}
    data['fingerprint'] = hashlib.sha256(json.dumps(identity,sort_keys=True).encode()).hexdigest()
    return data


def render():
    data = review_data()
    encoded = json.dumps(data,ensure_ascii=False,separators=(',',':')).replace('<','\\u003c')
    template = TEMPLATE.read_text()
    assert template.count('@@DATA@@') == template.count('@@CLIENT@@') == 1
    return template.replace('@@DATA@@',encoded).replace('@@CLIENT@@',CLIENT.read_text())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check',action='store_true')
    args = parser.parse_args()
    if ACCEPTANCE.is_file():
        accepted_archive()
        return
    rendered = render().encode('utf-8')
    if args.check:
        if not OUTPUT.is_file() or OUTPUT.read_bytes() != rendered:
            parser.exit(1,'Stale enemy review; run make enemy-review\n')
        print('PASS enemy review matches proposals, current bindings and original PNGs')
    else:
        OUTPUT.write_bytes(rendered)
        print('Built 5 combination / 10 role review; gameplay untouched:',OUTPUT.relative_to(ROOT))


if __name__ == '__main__':
    main()
