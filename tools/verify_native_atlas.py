#!/usr/bin/env python3
"""Compare executed native atlas smoke reports with the canonical artwork catalog.

Capture each actual graphical smoke run to a JSON file, then pass those files:
  python3 tools/verify_native_atlas.py artifacts/native-atlas/current.json artifacts/native-atlas/preview.json
No graphics or extra Python dependencies are needed to inspect existing reports.
"""
import argparse
import json
from pathlib import Path

from catalog_io import load_catalog
from query_assets import matches, parser as query_parser, sort_key

ROOT = Path(__file__).resolve().parents[1]
CASES = {
    '': [],
    'type:actor color:green sort:color': ['--type', 'actor', '--color', 'green', '--sort', 'color'],
    'subject:ghost confidence:high': ['--subject', 'ghost', '--subject-confidence', 'high'],
    'type:fragment layer:object composition:fragment_candidate sort:name desc:true':
        ['--type', 'fragment', '--layer', 'object', '--composition', 'fragment_candidate', '--sort', 'name', '--descending'],
    'type:input_prompt color:white coverage:0.3': ['--type', 'input_prompt', '--color', 'white', '--min-color-coverage', '.3'],
    'hex:#feffff': ['--hex', '#feffff'],
    'primary:blue sort:width': ['--primary-color', 'blue', '--sort', 'width'],
    'subject:goblin_like confidence:high': ['--subject', 'goblin_like', '--subject-confidence', 'high'],
    'interference': ['--search', 'interference'],
}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('reports', type=Path, nargs='+')
    args = p.parse_args()
    records = load_catalog()['sprites']
    index = json.loads((ROOT / 'assets/atlas-index.json').read_text())
    expected = {}
    for query, argv in CASES.items():
        options = query_parser().parse_args(argv)
        expected[query] = [r['id'] for r in sorted((r for r in records if matches(r, options)),
                                                  key=lambda r: sort_key(r, options.sort), reverse=options.descending)]
    expected['source:high'] = sorted(r['id'] for r in records if r['kind'] == 'sprite' and r['identity']['confidence'] == 'high')
    game_fields = ['name', 'lunarRole', 'lunarLimits', 'lunarIdeas', 'role', 'adaptation', 'generationNotes', 'ideas', 'usage', 'ithicaUsage']
    expected['interference game:true'] = sorted(r['id'] for r in index if 'interference' in json.dumps(
        {key:r[key] for key in game_fields}).lower())
    invalid = {'coverage:2', 'color:,,,', 'confidence:unknown', 'hex:green'}
    expected.update({query: [] for query in invalid})
    summary = []
    for path in args.reports:
        report = json.loads(path.read_text())
        if report.get('graphicalSmokeTest') != 'passed':
            raise AssertionError('Native smoke did not pass: '+str(path))
        observed = report.get('artworkQueries', [])
        if len(observed) != len(expected) or {q['query'] for q in observed} != set(expected):
            raise AssertionError('Missing or duplicate native query cases: '+str(path))
        for query in observed:
            text = query['query']
            if query['ids'] != expected[text]:
                raise AssertionError('Native/canonical query results or order differ: '+repr(text)+' in '+str(path))
            if bool(query['error']) != (text in invalid):
                raise AssertionError('Unexpected native query validation: '+repr(text))
        summary.append({'report':str(path), 'graphical_checks':len(report['checks']), 'canonical_queries':len(observed),
                        'query_counts':{q['query']:len(q['ids']) for q in observed}})
    print(json.dumps({'result':'passed','reports':summary},indent=2))


if __name__ == '__main__':
    main()
