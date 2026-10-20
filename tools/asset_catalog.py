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
VISUAL_NOUNS = '''humanoid animal creature insect snail moth beetle spider snake cat dog bat frog fish lizard
reptile bird rodent wing cape ghost skeleton horn hood robe
sword blade knife axe bow spear staff shovel pickaxe hoe saw tool shield hammer fork spade rod scoop
flask bottle food fruit berry plant tree grass flower mushroom grain wheat corn pumpkin pear pod cheese meat
pastry sandwich bread bun clothing garment tunic coat jacket vest dress skirt trousers boot glove belt ring
pendant accessory crown helmet hat pot vessel container crate box bucket pouch bag toolbox ingot mineral
crystal geode grille lattice mesh panel wall masonry brick window door stairs rail track road building house
tower bridge hatch grate casing pipe conduit computer screen radar chip circuit can canister fire flame
spark smoke cloud star moon planet globe vehicle rocket ship boat aircraft car train horse tent sofa chair
table cabinet bookcase shelf book basket jar kettle teapot goblet tray plate mug dish utensil necklace
lock key padlock chain rope hook token coin trap net spike foliage texture pattern frame loop cross
branch vine leaf sprig shoot print mark ornament slit stripe band square rectangle diamond circle'''.split()


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
    mask = bytearray(width * height)
    bounds = [width, height, -1, -1]
    for i in range(0, len(rgba), 4):
        r, g, b, alpha = rgba[i:i + 4]
        if not alpha or max(r, g, b) <= 8:
            continue
        colors[(r, g, b, alpha)] += 1
        mask[i // 4] = 1
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
            'foreground_mask_sha256': hashlib.sha256(mask).hexdigest(),
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


def properties_terms(properties):
    terms = set()
    for key in ['group1', 'group2', 'group3', 'tags']:
        terms.update(words(str(properties.get(key, ''))))
    return terms - {'default', 'unknown', 'cell'}


def visual_claims(values, confidence, evidence):
    return [claim(value if isinstance(value, str) else value['term'],
                  confidence if isinstance(value, str) else value.get('confidence', confidence),
                  'visual_observation', evidence) for value in values]


def named_visual_subjects(name, confidence, evidence):
    """Index nouns the author explicitly put in the neutral name, not guesses from pixels.

    Only the positive display name is used: a description saying 'not a ghost'
    must never become a ghost subject. Qualified/disjunctive names remain low
    confidence *_like claims rather than silently becoming exact identities.
    """
    name = name.lower()
    ambiguous = ' or ' in name
    result = []
    for noun in VISUAL_NOUNS:
        plural = {'leaf': 'leaves', 'berry': 'berries', 'knife': 'knives', 'shelf': 'shelves'}.get(
            noun, noun[:-1] + 'ies' if noun.endswith('y') else noun + 'es' if noun.endswith(('s', 'x', 'ch', 'sh')) else noun + 's')
        if not re.search(r'\b(?:' + re.escape(noun) + '|' + re.escape(plural) + r')\b', name):
            continue
        qualified = ambiguous or bool(re.search(r'\b' + re.escape(noun) + r'[- ]like\b', name))
        result.append(claim(noun + '_like' if qualified else noun, 'low' if qualified else confidence,
                            'visual_observation', evidence))
    return result


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
    portable = {'accesscard', 'armor', 'bag', 'banknote', 'book', 'bullet', 'camera', 'canister', 'cannedfood',
                'cellphone', 'chip', 'cigarette', 'cigarettespack', 'clothes', 'explosive', 'firstaid', 'food',
                'gameconsole', 'gamepad', 'headphones', 'headset', 'helmet', 'idcard', 'key', 'material', 'mineral',
                'money', 'musical', 'necklace', 'patch', 'pda', 'phone', 'pill', 'pillsjar', 'plasticcard', 'potion',
                'ring', 'scroll', 'shield', 'smartphone', 'soda', 'sunglasses', 'syringe', 'testtube', 'tool',
                'toolbox', 'watch', 'weapon'}
    if tag in portable:
        return 'item', ['object', 'interface']
    if any(token in tag for token in ['flora', 'plant', 'flower', 'mushroom', 'farm']):
        return 'vegetation', ['object', 'ground']
    if tag in ['floor', 'water', 'river', 'road', 'street', 'railway', 'overworld'] or 'terrain textures' in tag:
        return 'terrain', ['ground']
    if tag in ['wall', 'door', 'stairs', 'building', 'district', 'multistory', 'stockade', 'bridge', 'footbridge',
               'hatch', 'awning', 'window', 'column', 'factory', 'chapel', 'cave', 'radiotower', 'powerstation',
               'gasstation', 'electricpole']:
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


def describe(record, measured, authored, duplicates, silhouette=None, inspections=()):
    if silhouette and silhouette['mask_sha256'] != measured['foreground_mask_sha256']:
        raise ValueError('Historical silhouette evidence disagrees with current crop: ' + record['id'])
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
    observations = []
    silhouette_supported = False
    if silhouette and not publisher:
        shared = set.intersection(*(properties_terms(m['properties']) for m in silhouette['matches']))
        reference = 'reference/urizen-silhouette-matches.json#matches/' + record['id']
        if shared:
            silhouette_supported = True
            subjects += [claim(term, 'medium', 'publisher_silhouette_match', reference) for term in sorted(shared)]
            evidence_refs.append(reference)
            groups = {m['properties']['group1'] for m in silhouette['matches']}
            if len(groups) == 1:
                group = next(iter(groups))
                asset_type, layers = base_type({**record, 'category': group})
                composition = 'fragment_candidate' if group == 'pipe' else 'standalone_candidate'
                if group == 'pipe':
                    asset_type, layers = 'fragment', ['object', 'structure']
                names = {tuple(str(m['properties'].get(k, '')) for k in ['group1', 'group2', 'group3']) for m in silhouette['matches']}
                if len(names) == 1:
                    display_name = ' / '.join(part for part in next(iter(names)) if part) + ' (silhouette match)'
                    description = 'Foreground silhouette matches the historical author label ' + display_name.removesuffix(' (silhouette match)') + '; source colors differ or its identity was previously unresolved.'
                confidence = 'medium'
            uncertainties.append('Historical binary-mask match ignores color and can have alternative identities; it does not confirm the publisher identity of this crop.')
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
    if record.get('character_matrix') and record['grid'][0] <= 201:
        matrix = record['character_matrix']
        held = matrix['held_prop_observation']
        description = f'Figure body variant {matrix["body_variant_row"]:02d}, prop/stance variant {matrix["profession_variant_column"]:02d}: {held}.'
        if not publisher and not silhouette:
            display_name = f'Figure variant {matrix["body_variant_row"]:02d}/{matrix["profession_variant_column"]:02d}: {held}'
        body = authored.get('character_rows', {}).get(str(record['grid'][1]))
        prop = authored.get('character_columns', {}).get(str(record['grid'][0]))
        if body:
            reference = 'reference/asset-semantics.json#character_rows/' + str(record['grid'][1])
            observations.append({'id': 'body-row-' + str(record['grid'][1]), 'scope': 'series', 'description': body['description']})
            evidence_refs.append(reference)
            subjects += visual_claims(body.get('subjects', ['humanoid']), 'medium', reference)
            description = body['description']
            if not publisher and not silhouette:
                display_name = measured['primary_color'].capitalize() + ' ' + body['name']
            confidence = 'high' if publisher else 'medium'
        if prop:
            reference = 'reference/asset-semantics.json#character_columns/' + str(record['grid'][0])
            observations.append({'id': 'prop-column-' + str(record['grid'][0]), 'scope': 'series', 'description': prop['description']})
            evidence_refs.append(reference)
            subjects += visual_claims(prop.get('subjects', []), 'medium', reference)
            if record['grid'][0] == 201:
                description = prop['description'] + f' It is aligned with body-variant row {record["grid"][1]:02d}; the depicted condition is unconfirmed.'
            else:
                description += ' ' + prop['description']
            if not publisher and not silhouette:
                display_name += ': ' + prop['name']
        uncertainties.append('Body row and held prop do not confirm species, profession, animation or a direction set.')
    if record['kind'] == 'sprite' and source_confidence != 'high':
        uncertainties.append('Publisher identity remains unconfirmed; visual descriptions are interpretations.')
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
        observations.append({'id': record['id'], 'scope': 'individual', 'description': override['description'],
                             'origin': override.get('review_origin', 'Neutral exact-crop visual review')})
        evidence_refs.append('reference/asset-semantics.json#sprites/' + record['id'])
        subjects += visual_claims(override.get('subjects', []), override.get('confidence', 'medium'),
                                 'reference/asset-semantics.json#sprites/' + record['id'])
        for subject in named_visual_subjects(override.get('name', ''), override.get('confidence', 'medium'),
                                             'reference/asset-semantics.json#sprites/' + record['id']):
            if subject['term'] not in {s['term'] for s in subjects}:
                subjects.append(subject)
        uncertainties += override.get('uncertainties', [])
    if record['kind'] == 'sprite' and measured['foreground_pixels'] <= 2 and not publisher:
        asset_type, layers, composition = 'unresolved', ['unresolved'], 'sparse_mark'
        uncertainties.append('Only one or two foreground pixels; may be a fragment or atlas residue.')
    if not subjects and record['kind'] == 'sprite':
        subjects = [claim(asset_type, 'low', 'category_inference', 'asset.type')]
    tags = sorted(set([asset_type, *layers, *measured['color_names'], *(c['term'] for c in subjects)]))
    review_status = 'publisher_identified' if publisher else 'visible_control_described' if basis == 'visible_control' else 'visually_described' if override else \
        'visual_series_described' if any(o['scope'] == 'series' for o in observations) else \
        'silhouette_supported' if silhouette_supported else 'inspected_unresolved' if inspections else 'category_only'
    if record['kind'] != 'sprite':
        review_status = 'non_asset'
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
                         'inspection_refs': list(inspections), 'review_status': review_status,
                         'uncertainties': uncertainties,
                         'scope': 'Artwork description; gameplay usage and suggestions are separate.'}}


def build_assets(records, assets_dir):
    assets_dir = Path(assets_dir)
    authored = load_json(assets_dir / 'reference/asset-semantics.json')
    silhouette_path = assets_dir / 'reference/urizen-silhouette-matches.json'
    silhouettes = load_json(silhouette_path)['matches'] if silhouette_path.exists() else {}
    if authored.get('schema_version') != 1:
        raise ValueError('Unsupported neutral semantic input version')
    ids = {r['id'] for r in records}
    if set(authored.get('sprites', {})) - ids:
        raise ValueError('Semantic input refers to missing sprite IDs')
    by_id = {r['id']: r for r in records}
    inspections = collections.defaultdict(list)
    for sid, entry in authored.get('sprites', {}).items():
        if by_id[sid]['kind'] != 'sprite' or not entry.get('description'):
            raise ValueError('Individual observation must describe an occupied sprite: ' + sid)
        if entry.get('type', 'prop') not in TYPES or not set(entry.get('layers', [])) <= set(LAYERS):
            raise ValueError('Invalid generic type/layers: ' + sid)
        if entry.get('confidence', 'medium') not in CONFIDENCE:
            raise ValueError('Invalid visual confidence: ' + sid)
        for value in entry.get('subjects', []):
            if isinstance(value, dict) and (not value.get('term') or value.get('confidence', 'medium') not in CONFIDENCE):
                raise ValueError('Invalid subject claim: ' + sid)
    for key in authored.get('character_rows', {}):
        if not 0 <= int(key) <= 35:
            raise ValueError('Character body row outside observed matrix: ' + key)
    for key in authored.get('character_columns', {}):
        if not 104 <= int(key) <= 201:
            raise ValueError('Prop column outside observed matrix: ' + key)
    for index, page in enumerate(authored.get('inspected_pages', [])):
        source = assets_dir / page['source']
        if hashlib.sha256(source.read_bytes()).hexdigest() != page['source_sha256']:
            raise ValueError('Inspected source hash changed: ' + page['source'])
        x, y, width, height = page['rect']
        if len(page['sprite_ids']) != len(set(page['sprite_ids'])):
            raise ValueError('Duplicate IDs in inspection page: ' + page['file'])
        for sid in page['sprite_ids']:
            r = by_id.get(sid)
            if r is None or r['kind'] != 'sprite' or r['source'] != page['source']:
                raise ValueError('Invalid inspection-page sprite: ' + sid)
            sx, sy, sw, sh = r['rect']
            if sx < x or sy < y or sx + sw > x + width or sy + sh > y + height:
                raise ValueError('Declared inspected sprite outside page: ' + sid)
            inspections[sid].append('reference/asset-semantics.json#inspected_pages/' + str(index))
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
        result[r['id']] = describe(r, appearance(images[source], r), authored, duplicates, silhouettes.get(r['id']), inspections[r['id']])
    return result
