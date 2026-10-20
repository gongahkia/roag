"""Game-independent, evidence-scoped descriptions of unchanged source artwork.

Only standard-library modules are needed to regenerate or query the catalog.
PNG decoding is deliberately limited to the format of all 664 imported images:
non-interlaced, 8-bit RGBA PNG. An unsupported replacement fails explicitly.
"""
from __future__ import annotations

import collections
import colorsys
import functools
import hashlib
import json
import re
import struct
import zlib
from pathlib import Path

COLOR_ORDER = ['black', 'gray', 'white', 'brown', 'red', 'orange', 'yellow', 'green', 'cyan', 'blue', 'purple', 'pink']
TYPES = ['actor', 'terrain', 'structure', 'vegetation', 'item', 'prop', 'vehicle', 'effect', 'symbol',
         'ui_icon', 'input_prompt', 'glyph', 'fragment', 'unresolved', 'non_asset']
LAYERS = ['ground', 'structure', 'object', 'actor', 'effect', 'interface', 'text', 'unresolved', 'reference']
CONFIDENCE = ['low', 'medium', 'high']


def load_json(path):
    return json.loads(Path(path).read_text())


@functools.lru_cache(maxsize=None)
def color_name(rgb):
    """Deterministic descriptive color buckets, independent of gameplay palettes."""
    r, g, b = rgb
    hue, saturation, value = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    if value <= .12:
        return 'black'
    if saturation <= .14:
        return 'white' if value >= .83 else 'gray'
    degrees = hue * 360
    if 10 <= degrees < 65 and value < .66:
        return 'brown'
    if degrees < 15 or degrees >= 345:
        return 'pink' if value >= .8 and saturation < .55 else 'red'
    if degrees < 45:
        return 'orange'
    if degrees < 70:
        return 'yellow'
    if degrees < 165:
        return 'green'
    if degrees < 200:
        return 'cyan'
    if degrees < 260:
        return 'blue'
    if degrees < 310:
        return 'purple'
    return 'pink'


class Png:
    def __init__(self, path):
        data = Path(path).read_bytes()
        if data[:8] != b'\x89PNG\r\n\x1a\n':
            raise ValueError(f'Not a PNG: {path}')
        offset, compressed, header = 8, [], None
        while offset < len(data):
            length = struct.unpack_from('>I', data, offset)[0]
            kind = data[offset + 4:offset + 8]
            payload = data[offset + 8:offset + 8 + length]
            expected_crc = struct.unpack_from('>I', data, offset + 8 + length)[0]
            if zlib.crc32(kind + payload) != expected_crc:
                raise ValueError(f'PNG chunk checksum mismatch: {path}')
            if kind == b'IHDR':
                header = struct.unpack('>IIBBBBB', payload)
            elif kind == b'IDAT':
                compressed.append(payload)
            offset += length + 12
            if kind == b'IEND':
                break
        if header is None or header[2:] != (8, 6, 0, 0, 0):
            raise ValueError(f'Expected non-interlaced 8-bit RGBA PNG: {path}')
        self.width, self.height = header[:2]
        stride = self.width * 4
        raw = zlib.decompress(b''.join(compressed))
        if len(raw) != (stride + 1) * self.height:
            raise ValueError(f'PNG decoded size mismatch: {path}')
        self.rgba = bytearray(stride * self.height)
        previous = bytearray(stride)
        for y in range(self.height):
            position = y * (stride + 1)
            filter_type = raw[position]
            row = bytearray(raw[position + 1:position + 1 + stride])
            if filter_type not in range(5):
                raise ValueError(f'Unknown PNG row filter: {path}')
            if filter_type:
                for i, value in enumerate(row):
                    left = row[i - 4] if i >= 4 else 0
                    above = previous[i]
                    diagonal = previous[i - 4] if i >= 4 else 0
                    if filter_type == 1:
                        prediction = left
                    elif filter_type == 2:
                        prediction = above
                    elif filter_type == 3:
                        prediction = (left + above) // 2
                    else:
                        p = left + above - diagonal
                        pa, pb, pc = abs(p - left), abs(p - above), abs(p - diagonal)
                        prediction = left if pa <= pb and pa <= pc else above if pb <= pc else diagonal
                    row[i] = (value + prediction) & 255
            self.rgba[y * stride:(y + 1) * stride] = row
            previous = row

    def crop(self, rect):
        x, y, width, height = rect
        if x < 0 or y < 0 or width <= 0 or height <= 0 or x + width > self.width or y + height > self.height:
            raise ValueError(f'Crop outside PNG: {rect}')
        stride = self.width * 4
        return b''.join(self.rgba[row * stride + x * 4:row * stride + (x + width) * 4] for row in range(y, y + height))


def appearance(image, record):
    """Measure every foreground RGBA value; preserve raw source/crop hashes."""
    width, height = record['rect'][2:]
    rgba = image.crop(record['rect'])
    if hashlib.sha256(rgba).hexdigest() != record['rgba_sha256']:
        raise ValueError('Source crop pixels differ from frozen import: ' + record['id'])
    colors = collections.Counter()
    bounds = [width, height, -1, -1]
    for i in range(0, len(rgba), 4):
        r, g, b, alpha = rgba[i:i + 4]
        if not alpha or max(r, g, b) <= 8:
            continue
        colors[(r, g, b, alpha)] += 1
        x, y = (i // 4) % width, (i // 4) // width
        bounds = [min(bounds[0], x), min(bounds[1], y), max(bounds[2], x), max(bounds[3], y)]
    count = sum(colors.values())
    families = collections.Counter()
    palette = []
    for color, amount in sorted(colors.items(), key=lambda item: (-item[1], item[0])):
        family = color_name(color[:3])
        families[family] += amount
        palette.append({'hex': '#%02x%02x%02x' % color[:3], 'alpha': color[3], 'name': family,
                        'pixels': amount, 'coverage': round(amount / count, 6)})
    ordered = sorted(families, key=lambda key: (-families[key], COLOR_ORDER.index(key)))
    return {'width': width, 'height': height, 'foreground_pixels': count,
            'foreground_fraction': round(count / (width * height), 6),
            'foreground_bounds': [bounds[0], bounds[1], bounds[2] - bounds[0] + 1, bounds[3] - bounds[1] + 1] if count else None,
            'primary_color': ordered[0] if ordered else None, 'primary_hex': palette[0]['hex'] if palette else None,
            'color_names': ordered, 'color_coverage': {key: round(families[key] / count, 6) for key in ordered},
            'palette': palette, 'color_count': len(palette),
            'style': 'pixel_art',
            'background_rule': 'Alpha zero and RGB channels all <=8 excluded from foreground; original pixels unchanged.'}


def claim(term, confidence, basis, evidence):
    return {'term': term, 'confidence': confidence, 'basis': basis, 'evidence': evidence}


def words(value):
    return sorted(set(re.findall(r'[a-z][a-z0-9_-]*', value.lower())))


def publisher_terms(record):
    terms = set()
    identity = record.get('identity', {})
    if identity.get('basis') == 'author_metadata_visible_rgb_match':
        for match in identity.get('upstream_matches', []):
            properties = match['properties']
            for key in ['group1', 'group2', 'group3', 'tags']:
                terms.update(words(str(properties.get(key, ''))))
        terms -= {'default', 'unknown', 'cell'}
    elif identity.get('basis') == 'publisher_filename':
        terms.update(words(record.get('semantic_family', record['name']).replace('_', ' ')))
    elif record['pack'] == 'input-prompts':
        terms.update(words(record.get('control', '')))
        terms.add(record['category'])
    return sorted(terms)


def base_type(record):
    if record['kind'] != 'sprite':
        return 'non_asset', ['reference']
    if record['pack'] == 'godot-node-icons':
        return 'ui_icon', ['interface']
    if record['pack'] == 'input-prompts':
        return 'input_prompt', ['interface']
    tag = record.get('category', '').lower()
    if 'font' in tag or 'bitmap glyph' in record['name'].lower():
        return 'glyph', ['text']
    if record.get('character_matrix') or any(token in tag for token in ['character', 'creature', 'figure', 'player', 'enemy', 'npc', 'animal']):
        return 'actor', ['actor']
    if any(token in tag for token in ['flora', 'plant', 'flower', 'mushroom', 'farm']):
        return 'vegetation', ['object', 'ground']
    if tag in ['floor', 'water', 'river', 'road', 'street', 'railway'] or 'terrain textures' in tag:
        return 'terrain', ['ground']
    if tag in ['wall', 'door', 'stairs', 'building', 'multistory', 'stockade', 'bridge', 'footbridge', 'hatch', 'awning', 'window', 'column', 'factory', 'chapel']:
        return 'structure', ['structure']
    if 'transport' in tag or tag == 'ship':
        return 'vehicle', ['object']
    if 'modular' in tag or 'fragment' in tag or 'conduit' in tag or 'large celestial' in tag:
        return 'fragment', ['unresolved']
    if tag in ['effect', 'magic', 'fire', 'bonfire', 'explosive'] or any(token in tag for token in ['projectile', 'spark', 'effects']):
        return 'effect', ['effect']
    if any(token in tag for token in ['weapon', 'armor', 'helmet', 'shield', 'clothes', 'clothing', 'food', 'consumable', 'potion', 'tool', 'blade', 'ranged']):
        return 'item', ['object', 'interface']
    if any(token in tag for token in ['heraldry', 'symbol', 'hud', 'numbered', 'patterned']):
        return 'symbol', ['interface', 'object']
    if 'sparse' in tag:
        return 'unresolved', ['unresolved']
    if any(token in tag for token in ['walls', 'floors', 'terrain', 'buildings']):
        return 'structure', ['structure', 'ground']
    return 'prop', ['object', 'interface']


def describe(record, measured, authored, duplicates):
    asset_type, layers = base_type(record)
    identity = record.get('identity', {})
    source_confidence = identity.get('confidence', 'low')
    terms = publisher_terms(record)
    publisher = identity.get('basis', '').startswith(('author_', 'publisher_filename'))
    basis = 'publisher_metadata' if publisher else 'visible_control' if record['pack'] == 'input-prompts' else 'preserved_visual_description'
    subjects = [claim(term, source_confidence, basis, 'identity' if publisher else 'control') for term in terms]
    confidence = source_confidence
    description = record['name']
    display_name = record['name']
    composition = 'uncertain'
    uncertainties = []
    evidence_refs = ['sprites.json#' + record['id']]
    if record['kind'] != 'sprite':
        composition = 'non_asset'
        confidence = 'high'
        subjects = []
    elif asset_type in ['ui_icon', 'input_prompt', 'glyph', 'item', 'prop', 'actor', 'vehicle', 'effect', 'vegetation', 'symbol']:
        composition = 'standalone_candidate'
    elif asset_type in ['structure', 'terrain']:
        composition = 'tile_candidate'
    elif asset_type == 'fragment':
        composition = 'fragment_candidate'
    if not subjects and record['kind'] == 'sprite':
        subjects = [claim(asset_type, 'low', 'category_inference', 'category')]
    if record.get('character_matrix'):
        matrix = record['character_matrix']
        held = matrix['held_prop_observation']
        description = f'Figure body variant {matrix["body_variant_row"]:02d}, prop/stance variant {matrix["profession_variant_column"]:02d}: {held}.'
        uncertainties.append('Body row and held prop do not confirm species, profession, animation or a direction set.')
    if record['kind'] == 'sprite' and source_confidence != 'high':
        uncertainties.append('Publisher identity remains unconfirmed; visual descriptions are interpretations.')
    observations = []
    for rule in authored.get('regions', []):
        match = rule['match']
        if match.get('source') and match['source'] != record['source']:
            continue
        if match.get('category') and match['category'] != record.get('category'):
            continue
        col, row = record.get('grid', [-1, -1])
        if 'rows' in match and not match['rows'][0] <= row <= match['rows'][1]:
            continue
        if 'columns' in match and not match['columns'][0] <= col <= match['columns'][1]:
            continue
        if record['kind'] != 'sprite':
            continue
        observations.append({'id': rule['id'], 'scope': 'region', 'description': rule['description']})
        evidence_refs.append('reference/asset-semantics.json#regions/' + rule['id'])
        asset_type = rule.get('type', asset_type)
        layers = rule.get('layers', layers)
        composition = rule.get('composition', composition)
    override = authored.get('sprites', {}).get(record['id'])
    if override:
        display_name = override.get('name', display_name)
        description = override.get('description', description)
        asset_type = override.get('type', asset_type)
        layers = override.get('layers', layers)
        composition = override.get('composition', composition)
        confidence = override.get('confidence', confidence)
        observations.append({'id': record['id'], 'scope': 'individual', 'description': override['description']})
        evidence_refs.append('reference/asset-semantics.json#sprites/' + record['id'])
        subjects += [claim(term, override.get('confidence', 'medium'), 'visual_observation',
                           'reference/asset-semantics.json#sprites/' + record['id']) for term in override.get('subjects', [])]
        uncertainties += override.get('uncertainties', [])
    if record['kind'] == 'sprite' and measured['foreground_pixels'] <= 2:
        asset_type, layers, composition = 'unresolved', ['unresolved'], 'sparse_mark'
        uncertainties.append('Only one or two foreground pixels; may be a fragment or atlas residue.')
    tags = sorted(set([asset_type, *layers, *measured['color_names'], *(c['term'] for c in subjects)]))
    return {'schema_version': 1, 'name': display_name, 'description': description, 'type': asset_type,
            'classification_confidence': confidence, 'suggested_layers': layers,
            'subjects': subjects, 'tags': tags, 'appearance': measured,
            'composition': {'kind': composition, 'animation': 'unconfirmed', 'directions': 'unconfirmed',
                            'sockets': 'unauthored', 'collision': 'game_defined'},
            'relations': {'exact_rgba_duplicates': duplicates.get(record['id'], []),
                          'source_grid': record.get('grid'), 'source_variant_style': record.get('style'),
                          'named_variant_group': 'godot:' + record['semantic_family'] if record.get('semantic_family') else
                              'input:' + record.get('control', '') if record['pack'] == 'input-prompts' else None,
                          'character_matrix': record.get('character_matrix')},
            'evidence': {'source_identity': 'identity', 'references': evidence_refs, 'observations': observations,
                         'uncertainties': uncertainties,
                         'scope': 'Artwork description; gameplay usage and suggestions are separate.'}}


def build_assets(records, assets_dir):
    assets_dir = Path(assets_dir)
    authored = load_json(assets_dir / 'reference/asset-semantics.json')
    if authored.get('schema_version') != 1:
        raise ValueError('Unsupported neutral semantic input version')
    ids = {r['id'] for r in records}
    if set(authored.get('sprites', {})) - ids:
        raise ValueError('Semantic input refers to missing sprite IDs')
    images = {}
    groups = collections.defaultdict(list)
    for r in records:
        if r['kind'] == 'sprite':
            groups[(tuple(r['rect'][2:]), r['rgba_sha256'])].append(r['id'])
    duplicates = {sid: [other for other in group if other != sid] for group in groups.values() if len(group) > 1 for sid in group}
    result = {}
    for r in records:
        source = r['source']
        if source not in images:
            images[source] = Png(assets_dir / source)
        result[r['id']] = describe(r, appearance(images[source], r), authored, duplicates)
    return result
