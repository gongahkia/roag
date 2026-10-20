"""Meaningful contracts for the canonical artwork metadata and neutral query path."""
import copy
import hashlib
import json
import struct
import sys
import tempfile
import unittest
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools'))
from asset_catalog import COLOR_ORDER, CONFIDENCE, LAYERS, TYPES, Png, appearance, color_name
from query_assets import facets, matches, parser, sort_key


def chunk(kind, payload):
    return struct.pack('>I', len(payload)) + kind + payload + struct.pack('>I', zlib.crc32(kind + payload))


class PixelTests(unittest.TestCase):
    def test_every_imported_png_matches_frozen_dimensions(self):
        catalog = json.loads((ROOT / 'assets/catalog.json').read_text())
        for item in catalog['images']:
            with self.subTest(item=item['path']):
                image = Png(ROOT / 'assets' / item['path'])
                self.assertEqual((item['width'], item['height']), (image.width, image.height))

    def test_rgba_transparency_background_and_accent_coverage(self):
        rgba = bytes([0, 0, 0, 255, 8, 8, 8, 255, 255, 0, 0, 0, 0, 255, 0, 255,
                      0, 255, 0, 255, 255, 255, 255, 128])
        image = Png.__new__(Png)
        image.width, image.height, image.rgba = 3, 2, bytearray(rgba)
        record = {'id': 'fixture', 'rect': [0, 0, 3, 2], 'rgba_sha256': hashlib.sha256(rgba).hexdigest()}
        measured = appearance(image, record)
        self.assertEqual(measured['foreground_pixels'], 3)
        self.assertEqual(measured['primary_color'], 'green')
        self.assertEqual(measured['color_coverage'], {'green': .666667, 'white': .333333})
        self.assertEqual(measured['foreground_bounds'], [0, 1, 3, 1])
        self.assertEqual(measured['palette'][1]['alpha'], 128)

    def test_all_png_row_filters_decode_known_bytes(self):
        rows = [bytes([0, 10, 200, 255, 30, 80, 10, 128]), bytes([5, 20, 150, 255, 100, 90, 40, 64])]
        for filter_type in range(5):
            encoded = bytearray()
            prior = bytes(8)
            for row in rows:
                encoded.append(filter_type)
                for i, value in enumerate(row):
                    left, above, diagonal = row[i - 4] if i >= 4 else 0, prior[i], prior[i - 4] if i >= 4 else 0
                    if filter_type == 0:
                        prediction = 0
                    elif filter_type == 1:
                        prediction = left
                    elif filter_type == 2:
                        prediction = above
                    elif filter_type == 3:
                        prediction = (left + above) // 2
                    else:
                        p = left + above - diagonal
                        distances = [abs(p - x) for x in [left, above, diagonal]]
                        prediction = [left, above, diagonal][distances.index(min(distances))]
                    encoded.append((value - prediction) % 256)
                prior = row
            data = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', 2, 2, 8, 6, 0, 0, 0)) + \
                   chunk(b'IDAT', zlib.compress(encoded)) + chunk(b'IEND', b'')
            with tempfile.TemporaryDirectory() as directory:
                path = Path(directory) / 'fixture.png'
                path.write_bytes(data)
                self.assertEqual(Png(path).crop([0, 0, 2, 2]), b''.join(rows))

    def test_changed_source_crop_is_rejected(self):
        image = Png.__new__(Png)
        image.width, image.height, image.rgba = 1, 1, bytearray([1, 2, 3, 255])
        with self.assertRaisesRegex(ValueError, 'frozen import'):
            appearance(image, {'id': 'changed', 'rect': [0, 0, 1, 1], 'rgba_sha256': '0' * 64})

    def test_out_of_bounds_crop_is_rejected(self):
        image = Png.__new__(Png)
        image.width, image.height, image.rgba = 1, 1, bytearray(4)
        for rect in [[-1, 0, 1, 1], [0, 0, 2, 1], [0, 0, 1, 0]]:
            with self.assertRaises(ValueError):
                image.crop(rect)

    def test_common_color_words(self):
        for rgb, name in [((255, 255, 255), 'white'), ((120, 120, 120), 'gray'), ((0, 255, 0), 'green'),
                          ((0, 255, 255), 'cyan'), ((255, 0, 0), 'red'), ((0, 0, 255), 'blue'),
                          ((130, 70, 20), 'brown'), ((255, 255, 0), 'yellow')]:
            self.assertEqual(color_name(rgb), name)


class CatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.catalog = json.loads((ROOT / 'assets/catalog.json').read_text())
        cls.records = cls.catalog['sprites']
        cls.by_id = {r['id']: r for r in cls.records}
        cls.original = json.loads((ROOT / 'assets/sprites.json').read_text())['records']

    def test_every_frozen_record_retains_identity_crop_and_provenance(self):
        self.assertEqual({r['id'] for r in self.original}, set(self.by_id))
        for original in self.original:
            current = self.by_id[original['id']]
            self.assertEqual({k: current[k] for k in original if k != 'runtime_status'},
                             {k: v for k, v in original.items() if k != 'runtime_status'}, original['id'])

    def test_every_region_has_valid_generic_description_and_palette(self):
        for r in self.records:
            a = r['asset']
            self.assertIn(a['type'], TYPES)
            self.assertIn(a['classification_confidence'], CONFIDENCE)
            self.assertTrue(a['name'])
            self.assertTrue(a['description'])
            self.assertTrue(set(a['suggested_layers']) <= set(LAYERS))
            self.assertEqual(a['appearance']['foreground_pixels'], r['foreground_pixels'], r['id'])
            self.assertEqual(sum(p['pixels'] for p in a['appearance']['palette']), r['foreground_pixels'])
            for p in a['appearance']['palette']:
                self.assertIn(p['name'], COLOR_ORDER)
            for subject in a['subjects']:
                self.assertTrue(subject['term'])
                self.assertIn(subject['confidence'], CONFIDENCE)
                self.assertTrue(subject['basis'])
                self.assertTrue(subject['evidence'])

    def test_game_binding_counts_remain_unchanged(self):
        counts = self.catalog['catalog_summary']
        for key, value in [('occupied_sprites', 7115), ('records', 11887), ('used_ithica', 166), ('preview_only', 75), ('unused_sprites', 6874)]:
            self.assertEqual(counts[key], value)

    def test_known_ghost_can_be_queried_without_game_casting(self):
        args = parser().parse_args(['--subject', 'ghost', '--subject-confidence', 'high'])
        results = [r['id'] for r in self.records if matches(r, args)]
        self.assertIn('urizen-onebit-r42-c110', results)
        args = parser().parse_args(['--search', 'Interference'])
        self.assertNotIn('urizen-onebit-r42-c110', [r['id'] for r in self.records if matches(r, args)])

    def test_color_and_type_query_uses_measured_palette(self):
        args = parser().parse_args(['--type', 'actor', '--color', 'green', '--min-color-coverage', '.5'])
        rows = [r for r in self.records if matches(r, args)]
        self.assertTrue(rows)
        self.assertTrue(all(r['asset']['type'] == 'actor' and r['asset']['appearance']['color_coverage']['green'] >= .5 for r in rows))

    def test_subject_confidence_threshold_is_per_claim(self):
        record = copy.deepcopy(self.by_id['urizen-onebit-r42-c110'])
        record['asset']['subjects'].append({'term': 'uncertain-subject', 'confidence': 'low', 'basis': 'visual_observation', 'evidence': 'fixture'})
        args = parser().parse_args(['--subject', 'uncertain-subject', '--subject-confidence', 'medium'])
        self.assertFalse(matches(record, args))
        args.subject_confidence = 'low'
        self.assertTrue(matches(record, args))

    def test_non_assets_are_explicitly_opt_in(self):
        record = next(r for r in self.records if r['kind'] == 'blank')
        self.assertFalse(matches(record, parser().parse_args([])))
        self.assertTrue(matches(record, parser().parse_args(['--include-non-assets'])))

    def test_empty_facets_and_stable_sort(self):
        self.assertEqual(facets([]), {'types': {}, 'primary_colors': {}, 'subjects': {}})
        rows = sorted(self.records[:10], key=lambda r: sort_key(r, 'color'))
        self.assertEqual(len(rows), 10)
        self.assertEqual(rows, sorted(rows, key=lambda r: sort_key(r, 'color')))


if __name__ == '__main__':
    unittest.main()
