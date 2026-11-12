#!/usr/bin/env python3
"""Export explicit classic-game casting as Lua; never modify catalog or source art.

The catalog owns pixels and artwork evidence. classic-bindings.json owns local
roles. Repeated crops are static role reuse, never inferred animation frames.
Only the emitted Lua file is written; --check is completely read-only.
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path, PurePosixPath

from asset_catalog import Png, appearance
from catalog_io import load_catalog

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_BINDINGS = ROOT / 'assets/classic-bindings.json'
DEFAULT_OUTPUT = ROOT / 'src/classic_sprite_data.lua'
INVENTORY = ROOT / 'docs/sprite-audit/inventory.json'
SUPPORTED_PACKS = {
    'urizen': ('urizen/', 'black_to_transparent', 8),
    'godot-node-icons': ('godot-node-icons/', 'preserve', 0),
    'input-prompts': ('input-prompts/', 'preserve', 0),
}


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError('Duplicate JSON key: ' + key)
        result[key] = value
    return result


def read_json(path):
    return json.loads(Path(path).read_text(encoding='utf-8'), object_pairs_hook=unique_object)


def sha256(raw):
    return hashlib.sha256(raw).hexdigest()


def requirements(inventory):
    required = inventory.get('requirements')
    if not isinstance(required, list) or not required or any(not isinstance(v, str) or not v for v in required):
        raise ValueError('Inventory must contain explicit semantic requirements')
    if len(set(required)) != len(required):
        raise ValueError('Duplicate inventory requirement')
    packed = {r['id'] for pack in inventory['packs'].values() for r in pack['bindings']}
    if packed != set(required):
        raise ValueError('Inventory requirements and pack bindings disagree')
    return set(required)


def resolve_bindings(document, inventory, catalog, catalog_hash, assets_dir, bindings_hash):
    """Validate original pixels and resolve all roles, without writing any file."""
    if not isinstance(document, dict) or set(document) != {'schemaVersion', 'sourceCatalog', 'bindings'}:
        raise ValueError('Binding document must contain schemaVersion, sourceCatalog and bindings only')
    if type(document['schemaVersion']) is not int or document['schemaVersion'] != 1:
        raise ValueError('Unsupported classic binding schema')
    if document['sourceCatalog'] != 'catalog.json.gz':
        raise ValueError('Classic bindings must use the preserved catalog.json.gz')
    bindings = document['bindings']
    if not isinstance(bindings, dict):
        raise ValueError('Bindings must be an object')
    required = requirements(inventory)
    missing, extra = required - bindings.keys(), bindings.keys() - required
    if missing or extra:
        raise ValueError(f'Binding coverage mismatch: missing={sorted(missing)}, extra={sorted(extra)}')
    records = {r['id']: r for r in catalog['sprites']}
    images = {r['path']: r for r in catalog['images']}
    if len(records) != len(catalog['sprites']) or len(images) != len(catalog['images']):
        raise ValueError('Duplicate catalog sprite or source image identity')
    decoded, resolved, sources, crops = {}, {}, {}, {}
    assets_dir = Path(assets_dir).resolve()
    for role, binding in sorted(bindings.items()):
        if (not isinstance(binding, dict) or not {'sprite', 'notes'} <= binding.keys()
                or binding.keys() - {'sprite', 'notes', 'flipX'}):
            raise ValueError(f'{role}: explicit sprite and notes required; only optional flipX is supported')
        flip_x = binding.get('flipX', False)
        if type(flip_x) is not bool:
            raise ValueError(f'{role}: flipX must be a boolean')
        sprite, notes = binding['sprite'], binding['notes']
        if not isinstance(sprite, str) or not isinstance(notes, str) or not notes.strip():
            raise ValueError(f'{role}: sprite and nonempty local casting notes required')
        if sprite not in records:
            raise ValueError(f'{role}: unknown catalog sprite {sprite}')
        r = records[sprite]
        if r.get('kind') != 'sprite' or r.get('foreground_pixels', 0) <= 0 or r.get('asset', {}).get('type') == 'non_asset':
            raise ValueError(f'{role}: empty, guide, gutter or non-asset crop {sprite}')
        pack = r.get('pack')
        if pack not in SUPPORTED_PACKS:
            raise ValueError(f'{role}: unsupported source pack {pack}')
        source = r.get('source')
        prefix, background, threshold = SUPPORTED_PACKS[pack]
        if not isinstance(source, str) or PurePosixPath(source).is_absolute() or '..' in PurePosixPath(source).parts or '\\' in source:
            raise ValueError(f'{role}: unsafe source path')
        expected_image_packs = ('input-prompts/new', 'input-prompts/legacy') if pack == 'input-prompts' else (pack,)
        if not source.startswith(prefix) or source not in images or images[source].get('pack') not in expected_image_packs:
            raise ValueError(f'{role}: source does not belong to the recorded pack')
        path = assets_dir / source
        if not path.resolve().is_relative_to(assets_dir):
            raise ValueError(f'{role}: source escapes assets directory')
        metadata = images[source]
        runtime_source = 'assets/' + source
        if source not in decoded:
            raw = path.read_bytes()
            if sha256(raw) != metadata['sha256']:
                raise ValueError('Source PNG hash differs from preserved catalog: ' + source)
            image = Png(raw)
            if (image.width, image.height) != (metadata['width'], metadata['height']):
                raise ValueError('Source PNG dimensions differ from preserved catalog: ' + source)
            decoded[source] = image
            sources[runtime_source] = {'width': image.width, 'height': image.height, 'sha256': metadata['sha256'],
                                       'background': background, 'black_threshold': threshold}
        rect = r.get('rect')
        if not isinstance(rect, list) or len(rect) != 4 or any(type(v) is not int for v in rect):
            raise ValueError(f'{sprite}: invalid integer crop rectangle')
        grid = metadata.get('grid')
        if grid:
            x, y, width, height = rect
            column, row = r.get('grid', (None, None))
            if (type(column) is not int or type(row) is not int or not 0 <= column < grid['columns'] or
                    not 0 <= row < grid['rows'] or column in grid.get('guide_columns_zero_based', ()) or
                    rect != [grid['margin_x'] + column * (grid['tile_width'] + grid['spacing_x']),
                             grid['margin_y'] + row * (grid['tile_height'] + grid['spacing_y']),
                             grid['tile_width'], grid['tile_height']]):
                raise ValueError(f'{sprite}: crop enters a gutter or violates the recorded source grid')
        if sprite not in crops:
            measured = appearance(decoded[source], r)
            if measured['foreground_pixels'] <= 0 or measured['foreground_pixels'] != r['foreground_pixels']:
                raise ValueError(f'{sprite}: empty crop or foreground differs from preserved catalog')
            crops[sprite] = measured
        asset, identity = r['asset'], r.get('identity', {})
        x, y, width, height = rect
        resolved[role] = {
            'id': sprite, 'source': runtime_source, 'x': x, 'y': y, 'width': width, 'height': height,
            'rgba_sha256': r['rgba_sha256'], 'notes': notes, 'artwork_name': asset['name'],
            'identity_confidence': identity.get('confidence', 'unconfirmed'),
            'identity_basis': identity.get('basis', 'unconfirmed'),
            'subjects': asset['subjects'], 'composition': asset['composition'],
        }
        if flip_x:
            resolved[role]['flip_x'] = True
    return {'schema_version': 1, 'catalog_sha256': catalog_hash, 'bindings_sha256': bindings_hash,
            'requirements': sorted(required), 'requirement_count': len(required),
            'sources': sources, 'bindings': resolved}


def lua_string(value):
    escapes = {'\\': '\\\\', '"': '\\"', '\n': '\\n', '\r': '\\r', '\t': '\\t'}
    return '"' + ''.join(escapes.get(c, f'\\{ord(c):03d}' if ord(c) < 32 or ord(c) == 127 else c) for c in value) + '"'


def lua_value(value):
    if isinstance(value, str):
        return lua_string(value)
    if value is None:
        return 'nil'
    if type(value) is bool:
        return 'true' if value else 'false'
    if type(value) is int:
        return str(value)
    if isinstance(value, list):
        return '{' + ', '.join(lua_value(v) for v in value) + '}'
    if isinstance(value, dict):
        return '{' + ', '.join('[' + lua_string(k) + '] = ' + lua_value(v) for k, v in sorted(value.items())) + '}'
    raise ValueError('Unsupported Lua value type: ' + type(value).__name__)


def render_lua(data):
    lines = ['-- Generated by tools/build_classic_assets.py; edit assets/classic-bindings.json instead.',
             '-- Static local casting; source identity and unconfirmed animation evidence remain separate.', 'return {']
    for key in ['schema_version', 'catalog_sha256', 'bindings_sha256', 'requirement_count', 'requirements']:
        lines.append('  ' + key + ' = ' + lua_value(data[key]) + ',')
    for section in ['sources', 'bindings']:
        lines.append('  ' + section + ' = {')
        for key, value in sorted(data[section].items()):
            lines.append('    [' + lua_string(key) + '] = ' + lua_value(value) + ',')
        lines.append('  },')
    lines.append('}')
    return ('\n'.join(lines) + '\n').encode('utf-8')


def build(bindings_path=DEFAULT_BINDINGS, output_path=DEFAULT_OUTPUT, check=False, root=ROOT):
    root = Path(root)
    catalog_path = root / 'assets/catalog.json.gz'
    data = resolve_bindings(read_json(bindings_path), read_json(root / 'docs/sprite-audit/inventory.json'),
                            load_catalog(catalog_path), sha256(catalog_path.read_bytes()), root / 'assets',
                            sha256(Path(bindings_path).read_bytes()))
    expected = render_lua(data)
    output_path = Path(output_path)
    if check:
        if not output_path.exists() or output_path.read_bytes() != expected:
            raise ValueError(f'Stale classic sprite data: {output_path}; run tools/build_classic_assets.py')
    else:
        # Refuse an accidental output pointed at any preserved asset or audit input.
        target = output_path.resolve()
        if target.is_relative_to((root / 'assets').resolve()) or target == (root / 'docs/sprite-audit/inventory.json').resolve() or target == Path(bindings_path).resolve():
            raise ValueError('Output cannot overwrite preserved art, catalog or binding inputs')
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_bytes(expected)
    return data


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='verify generated data without writing')
    parser.add_argument('--bindings', type=Path, default=DEFAULT_BINDINGS)
    parser.add_argument('--output', type=Path, default=DEFAULT_OUTPUT)
    args = parser.parse_args(argv)
    try:
        data = build(args.bindings, args.output, args.check)
    except (OSError, ValueError, KeyError, TypeError) as error:
        print('Classic asset error: ' + str(error), file=sys.stderr)
        return 1
    print(f"{'Checked' if args.check else 'Generated'} {len(data['bindings'])} explicit bindings from {len(data['sources'])} unchanged source PNGs.")
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
