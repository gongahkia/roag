#!/usr/bin/env python3
"""Build a separate, offline detail vote. Never rewrite the original review."""
import argparse
import base64
import copy
import hashlib
import json
from pathlib import Path
import subprocess

from sprite_audit import pixels
from sprite_ideas_archive import verify

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs/sprite-audit'
EXCLUDED = {
    'escape-scroll': {'onebit:525'},
    'charm-well': {'rpg:855'},
    'memorial-choice': {'onebit:24'},
    'signposted-routes': {f'rpg:{i}' for i in (22, 425, 737, 794, 851, 908)},
    'camp-choice': {f'rpg:{i}' for i in (616, 617, 618, 619, 674, 675)},
}
VISUAL_OMISSIONS = {'cleanse-flask': {'tiny:117'}}  # Hammer, not a bottle.


def build():
    verify()
    original = json.loads((OUT/'ideas.json').read_bytes())
    feedback_bytes = (OUT/'ideas-feedback.json').read_bytes()
    feedback = json.loads(feedback_bytes)
    plans = json.loads((ROOT/'tools/item_room_proposals.json').read_bytes())
    live = json.loads(subprocess.check_output(['luajit', 'tools/sprite_audit.lua'], cwd=ROOT))
    inventory = json.loads((OUT/'inventory.json').read_bytes())
    assert live['mapping_checksum'] == inventory['mapping_checksum'], 'Run make sprite-audit'
    cards, cells = [], {}
    old = {c['id']: c for c in original['cards']}
    votes = {r['id']: r for r in feedback['cards']}
    for id, plan in plans.items():
        card, prior = copy.deepcopy(old[id]), votes[id]
        assert prior['vote'] == 'approve' and card['kind'] in ('item', 'room')
        user_banned = EXCLUDED.get(id, set())
        omitted = VISUAL_OMISSIONS.get(id, set())
        banned = user_banned | omitted
        card.update(plan)
        card['idea'], card['decisions'] = card.pop('summary'), card.pop('risk')
        card['dependencies'] = []  # The implemented Wayfinder is context, not another vote.
        card['history'] = {
            'vote': prior['vote'],
            'note': 'The broad concept was approved previously. This separate vote covers the concrete proposal below; blank answers do not erase that earlier approval.',
            'comments': ([prior['comment']] if prior['comment'] else []) + [
                f'{pack}: {prior["art"][pack]["comment"]}'
                for pack in ('kenney', 'onebit') if prior['art'][pack]['comment']],
        }
        card['excluded_cells'] = sorted(user_banned)
        card['visual_omissions'] = sorted(omitted)
        for pack, groups in card['art'].items():
            for group in groups:
                group['cells'] = [k for k in group['cells'] if k not in banned]
            card['art'][pack] = [g for g in groups if g['cells']]
            assert card['suggested'][pack] is None or card['suggested'][pack] in {k for g in card['art'][pack] for k in g['cells']}
        card['groups'] = copy.deepcopy(card['art']['kenney'])
        card.pop('owned_count')  # Historical unused-cell ownership is not current usage.
        keys = {k for g in card['groups'] for k in g['cells']}
        card['candidate_count'] = len(keys)
        for key in sorted(keys):
            if key not in cells:
                sheet, index = key.split(':')
                usage = {p: sorted(b['id'] for b in info['bindings']
                                   if b['sheet'] == sheet and b['index'] == int(index)
                                   and not b['pose_alias']) for p, info in live['packs'].items()}
                cells[key] = {'proposals': [], 'usage': usage}
            cells[key]['proposals'].append(id)
        cards.append(card)
    assert len(cards) == 14 and sum(c['kind'] == 'item' for c in cards) == 6
    # Context changed after the classes shipped. Keep the original archive as-is.
    next(c for c in cards if c['id'] == 'return-token')['existing'] = (
        'Wayfinder now has a saved same-room beacon and a paid six-step return. '
        'Shade also has a decoy swap. This proposal must earn its place as a finite utility, not duplicate a class for free.')
    sources = {}
    for name in sorted({k.split(':')[0] for k in cells}):
        meta = original['sources'][name]
        path = (ROOT/meta['path']).resolve()
        assert path.is_relative_to(ROOT/'assets/kenney') and path.suffix == '.png'
        raw = path.read_bytes()
        assert hashlib.sha256(raw).hexdigest() == meta['sha256']
        width, height, rgba = pixels(path)
        assert (width, height) == (meta['width'], meta['height'])
        sources[name] = meta
        for key, cell in cells.items():
            sheet, index = key.split(':')
            if sheet != name:
                continue
            x, y = int(index) % meta['columns']*meta['stride'], int(index)//meta['columns']*meta['stride']
            assert x+meta['tile'] <= width and y+meta['tile'] <= height
            cell['nontransparent_pixels'] = sum(bool(rgba[yy][xx][3]) and not
                (name == 'micro' and rgba[yy][xx][:3] == (34, 35, 35))
                for yy in range(y, y+meta['tile']) for xx in range(x, x+meta['tile']))
            assert cell['nontransparent_pixels'] > 0, key
    reused = sum(any(c['usage'].values()) for c in cells.values())
    data = dict(schema='roag-item-room-review/1', review_kind='items-rooms',
        feedback_schema='roag-item-room-feedback/1', feedback_filename='roag-items-rooms-feedback.json',
        mapping_checksum=live['mapping_checksum'], requirements=len(live['requirements']),
        origin=dict(proposal_fingerprint=original['fingerprint'],
                    feedback_path='docs/sprite-audit/ideas-feedback.json',
                    feedback_sha256=hashlib.sha256(feedback_bytes).hexdigest()),
        sources=sources, cards=cards, cells=cells,
        candidate_label='candidate cells (current reuse labelled)',
        summary_text=f'14 detail votes · 6 items + 8 room interactions · both Kenney looks · {len(cells)} candidate cells, {reused} already used in game',
        queue_note='earlier approvals preserved',
        summary=dict(proposals=14, held_groups=0, unused=len(cells)-reused,
                     reused=reused, proposed_cells=len(cells), held_cells=0, needs_counterpart=0,
                     categories={'item': 6, 'room': 8}))
    data['fingerprint'] = hashlib.sha256(json.dumps(data, sort_keys=True, separators=(',', ':')).encode()).hexdigest()
    payload = copy.deepcopy(data)
    for source in payload['sources'].values():
        source['image'] = 'data:image/png;base64,'+base64.b64encode((ROOT/source['path']).read_bytes()).decode()
    common = (ROOT/'tools/sprite_ideas_template.html').read_text()
    style = common.split('<style>', 1)[1].split('</style>', 1)[0]
    client = (ROOT/'tools/item_room_review_client.js').read_text()
    assert '</script' not in client.lower()
    html = (ROOT/'tools/item_room_review_template.html').read_text().replace('@@STYLE@@', style)
    html = html.replace('@@DATA@@', json.dumps(payload, ensure_ascii=False, separators=(',', ':')).replace('<', '\\u003c'))
    html = html.replace('@@CLIENT@@', client)
    return {'items-rooms.json': json.dumps(data, ensure_ascii=False, sort_keys=True, indent=2)+'\n',
            'items-rooms.html': html}, data


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    approval = OUT/'items-rooms-approval.json'
    if approval.is_file():
        verify()
        record = json.loads(approval.read_bytes())
        assert record['authority'] == 'explicit_user_message'
        for name, checksum in record['review_files'].items():
            path = (ROOT/name).resolve()
            assert path.is_relative_to(ROOT) and hashlib.sha256(path.read_bytes()).hexdigest() == checksum, 'Approved item/room review changed: '+name
        print('PASS preserved all-14 explicit user approval; reviewed mappings remain historical')
        raise SystemExit(0)
    outputs, data = build()
    for name, content in outputs.items():
        path = OUT/name
        if args.check:
            assert path.is_file() and path.read_bytes() == content.encode(), f'Stale {name}; run make item-room-review'
        else:
            path.write_bytes(content.encode())
    print('PASS item/room detail review: '+data['summary_text'])
