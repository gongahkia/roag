#!/usr/bin/env python3
"""Query the canonical artwork catalog without inheriting a game's mechanics.

Examples:
  python3 tools/query_assets.py --subject ghost --format ids
  python3 tools/query_assets.py --type actor --color green --sort color --limit 10
  python3 tools/query_assets.py --color cyan --type fragment --facets
"""
from __future__ import annotations

import argparse
import collections
import csv
import io
import json
from pathlib import Path

from asset_catalog import COLOR_ORDER, COMPOSITIONS, CONFIDENCE, LAYERS, TYPES, neutral_record
from catalog_io import DEFAULT_CATALOG, load_catalog


def matches(record, args):
    asset = record['asset']
    if record['kind'] != 'sprite' and not args.include_non_assets:
        return False
    if args.pack and record['pack'] not in args.pack:
        return False
    if args.type and asset['type'] not in args.type:
        return False
    if args.layer and not set(args.layer).intersection(asset['suggested_layers']):
        return False
    if args.color and not all(asset['appearance']['color_coverage'].get(c, 0) >= args.min_color_coverage and
                              c in asset['appearance']['color_names'] for c in args.color):
        return False
    if args.primary_color and asset['appearance']['primary_color'] != args.primary_color:
        return False
    if args.hex and not any(p['hex'].lower() == args.hex.lower() for p in asset['appearance']['palette']):
        return False
    threshold = CONFIDENCE.index(args.subject_confidence)
    terms = {c['term'] for c in asset['subjects'] if CONFIDENCE.index(c['confidence']) >= threshold}
    if args.subject and not all(term.lower() in terms for term in args.subject):
        return False
    if args.composition and asset['composition']['kind'] != args.composition:
        return False
    if args.search:
        text = ' '.join([record['id'], record['source'], record['pack'], asset['name'], asset['description'],
                         *asset['tags'], *[o['description'] for o in asset['evidence']['observations']]]).lower()
        if not all(term in text for term in args.search.lower().split()):
            return False
    return True


def sort_key(record, key):
    asset = record['asset']
    values = {'id': record['id'], 'name': asset['name'].casefold(), 'type': asset['type'],
              'color': COLOR_ORDER.index(asset['appearance']['primary_color']) if asset['appearance']['primary_color'] else -1,
              'width': asset['appearance']['width'], 'height': asset['appearance']['height'],
              'confidence': CONFIDENCE.index(asset['classification_confidence'])}
    return values[key], record['id']


def facets(records):
    return {'types': dict(sorted(collections.Counter(r['asset']['type'] for r in records).items())),
            'primary_colors': dict(sorted(collections.Counter(r['asset']['appearance']['primary_color'] or 'none' for r in records).items())),
            'subjects': dict(sorted(collections.Counter(term for r in records for term in {s['term'] for s in r['asset']['subjects']}).items()))}


def parser():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('--catalog', type=Path, default=DEFAULT_CATALOG, help='Catalog JSON or JSON.gz (default: tracked catalog.json.gz).')
    p.add_argument('--search', default='')
    p.add_argument('--pack', action='append', help='Repeat to match any of the listed packs.')
    p.add_argument('--type', choices=TYPES, action='append')
    p.add_argument('--layer', choices=LAYERS, action='append')
    p.add_argument('--color', choices=COLOR_ORDER, action='append', help='Repeat to require every named foreground color.')
    p.add_argument('--primary-color', choices=COLOR_ORDER)
    p.add_argument('--min-color-coverage', type=float, default=.05, help='Minimum fraction of foreground pixels for --color (default .05).')
    p.add_argument('--hex', help='Exact foreground RGB hex, for example "#00ff00".')
    p.add_argument('--subject', action='append', help='Exact subject term; repeat to require every term.')
    p.add_argument('--subject-confidence', choices=CONFIDENCE, default='low', help='Minimum confidence for subject claims.')
    p.add_argument('--composition', choices=COMPOSITIONS)
    p.add_argument('--include-non-assets', action='store_true', help='Include blank slots, guides and annotations.')
    p.add_argument('--sort', choices=['id', 'name', 'type', 'color', 'width', 'height', 'confidence'], default='id')
    p.add_argument('--descending', action='store_true')
    p.add_argument('--offset', type=int, default=0)
    p.add_argument('--limit', type=int, default=50, help='Page size; use 0 for all matching records.')
    p.add_argument('--format', choices=['json', 'csv', 'ids'], default='json')
    p.add_argument('--full', action='store_true', help='Include original/game-specific fields as well as neutral artwork metadata.')
    p.add_argument('--facets', action='store_true', help='Print aggregate facets for all matches, ignoring pagination.')
    p.add_argument('--output', type=Path)
    return p


def main(argv=None):
    p = parser()
    args = p.parse_args(argv)
    if not 0 <= args.min_color_coverage <= 1 or args.offset < 0 or args.limit < 0:
        p.error('Color coverage must be 0–1; offset and limit must be nonnegative.')
    catalog = load_catalog(args.catalog)
    if catalog.get('schema_version', 0) < 5:
        p.error('A schema-5 artwork catalog is required; regenerate it with tools/build_game_catalog.py.')
    records = sorted((r for r in catalog['sprites'] if matches(r, args)),
                     key=lambda r: sort_key(r, args.sort), reverse=args.descending)
    page = records[args.offset:args.offset + args.limit] if args.limit else records[args.offset:]
    if args.facets:
        output = json.dumps({'total': len(records), **facets(records)}, indent=2) + '\n'
    elif args.format == 'ids':
        output = ''.join(r['id'] + '\n' for r in page)
    elif args.format == 'csv':
        stream = io.StringIO(newline='')
        writer = csv.DictWriter(stream, fieldnames=['id', 'pack', 'source', 'rect', 'kind', 'rgba_sha256', 'source_identity',
                                                   'name', 'type', 'classification_confidence', 'suggested_layers', 'composition',
                                                   'primary_color', 'color_names', 'color_coverage', 'subjects', 'subject_claims', 'description'])
        writer.writeheader()
        for r in page:
            a = r['asset']
            writer.writerow({'id': r['id'], 'pack': r['pack'], 'source': r['source'], 'rect': json.dumps(r['rect']),
                             'kind': r['kind'], 'rgba_sha256': r['rgba_sha256'], 'source_identity': json.dumps(r.get('identity', {})),
                             'name': a['name'], 'type': a['type'], 'primary_color': a['appearance']['primary_color'],
                             'classification_confidence': a['classification_confidence'],
                             'suggested_layers': '; '.join(a['suggested_layers']), 'composition': a['composition']['kind'],
                             'color_names': '; '.join(a['appearance']['color_names']),
                             'color_coverage': json.dumps(a['appearance']['color_coverage']), 'subject_claims': json.dumps(a['subjects']),
                             'subjects': '; '.join(c['term'] for c in a['subjects']), 'description': a['description']})
        output = stream.getvalue()
    else:
        rows = page if args.full else [neutral_record(r) for r in page]
        output = json.dumps({'catalog_schema': catalog['schema_version'], 'total': len(records), 'offset': args.offset,
                             'returned': len(rows), 'matches': rows}, ensure_ascii=False, indent=2) + '\n'
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(output)
    else:
        print(output, end='')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
