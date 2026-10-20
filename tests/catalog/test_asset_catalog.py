"""Meaningful contracts for the canonical artwork metadata and neutral query path."""
import copy
import base64
import gzip
import hashlib
import json
import struct
import subprocess
import sys
import tempfile
import unittest
import zlib
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools'))
from asset_catalog import COLOR_ORDER, COMPOSITIONS, CONFIDENCE, LAYERS, TYPES, Png, appearance, color_name, describe, named_visual_subjects, neutral_record
from query_assets import facets, matches, parser, sort_key
from catalog_io import DEFAULT_CATALOG, load_catalog


def chunk(kind, payload):
    return struct.pack('>I', len(payload)) + kind + payload + struct.pack('>I', zlib.crc32(kind + payload))


class CatalogStorageTests(unittest.TestCase):
    def test_tracked_catalog_stays_below_github_large_file_warning(self):
        self.assertLess(DEFAULT_CATALOG.stat().st_size, 50 * 1024 * 1024)

    def test_cli_queries_match_for_tracked_gzip_and_plain_json_export(self):
        command = [sys.executable, str(ROOT / 'tools/query_assets.py'), '--subject', 'ghost',
                   '--subject-confidence', 'high', '--format', 'ids', '--limit', '0']
        compressed = subprocess.check_output(command, cwd=ROOT)
        self.assertTrue(compressed.strip())
        with tempfile.TemporaryDirectory() as directory:
            export = Path(directory) / 'catalog.json'
            export.write_bytes(gzip.decompress(DEFAULT_CATALOG.read_bytes()))
            plain = subprocess.check_output([*command, '--catalog', str(export)], cwd=ROOT)
        self.assertEqual(compressed, plain)


class PixelTests(unittest.TestCase):
    def test_every_imported_png_matches_frozen_dimensions(self):
        catalog = load_catalog()
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
        self.assertEqual(measured['foreground_mask_sha256'], hashlib.sha256(bytes([0, 0, 0, 1, 1, 1])).hexdigest())

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
        cls.catalog = load_catalog()
        cls.records = cls.catalog['sprites']
        cls.by_id = {r['id']: r for r in cls.records}
        cls.original = json.loads((ROOT / 'assets/sprites.json').read_text())['records']

    def test_every_frozen_record_retains_identity_crop_and_provenance(self):
        self.assertEqual({r['id'] for r in self.original}, set(self.by_id))
        for original in self.original:
            current = self.by_id[original['id']]
            self.assertEqual({k: current[k] for k in original if k != 'runtime_status'},
                             {k: v for k, v in original.items() if k != 'runtime_status'}, original['id'])

    @unittest.skipUnless((ROOT / '.git').exists(), 'Tracked-media coverage requires the repository checkout')
    def test_all_tracked_media_are_originals_or_explicit_references(self):
        files = subprocess.check_output(['git', 'ls-files', '-z'], cwd=ROOT).decode().split('\0')
        media = {p for p in files if Path(p).suffix.lower() in {'.png', '.jpg', '.jpeg', '.gif', '.webp', '.bmp', '.tga', '.svg', '.ase', '.aseprite', '.zip'}}
        originals = {'assets/' + r['path'] for r in [*self.catalog['images'], *self.catalog['archives']]}
        references = self.catalog['reference_images']['images']
        self.assertEqual(media, originals | {r['path'] for r in references})
        self.assertEqual(len(references), 22)
        for r in references:
            self.assertEqual(r['usage'], 'reference_only')
            self.assertEqual(hashlib.sha256((ROOT / r['path']).read_bytes()).hexdigest(), r['sha256'])

    def test_every_archived_image_is_indexed_byte_for_byte(self):
        source_hashes = {r['sha256'] for r in self.catalog['images']}
        formats = {'.png', '.jpg', '.jpeg', '.gif', '.webp', '.bmp', '.tga', '.svg', '.tif', '.tiff', '.ico', '.ase', '.aseprite', '.psd', '.kra'}
        count = 0
        for archive in self.catalog['archives']:
            with zipfile.ZipFile(ROOT / 'assets' / archive['path']) as zipped:
                self.assertIsNone(zipped.testzip())
                for entry in zipped.infolist():
                    if entry.is_dir():
                        continue
                    raw = zipped.read(entry)
                    if Path(entry.filename).suffix.lower() in formats or raw.startswith(b'\x89PNG\r\n\x1a\n'):
                        count += 1
                        self.assertIn(hashlib.sha256(raw).hexdigest(), source_hashes, entry.filename)
        self.assertEqual(count, 662)

    def test_every_region_has_valid_generic_description_and_palette(self):
        for r in self.records:
            a = r['asset']
            self.assertIn(a['type'], TYPES)
            self.assertIn(a['classification_confidence'], CONFIDENCE)
            self.assertIn(a['composition']['kind'], COMPOSITIONS)
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

    def test_publisher_game_roles_remain_provenance_not_neutral_subjects(self):
        r = self.by_id['urizen-onebit-r42-c110']
        self.assertEqual(r['name'], 'enemy / ghost')
        self.assertEqual(r['identity']['upstream_matches'][0]['properties']['group1'], 'enemy')
        self.assertEqual(r['asset']['name'], 'Ghost')
        self.assertTrue(any(c['term'] == 'ghost' and c['confidence'] == 'high' for c in r['asset']['subjects']))
        for record in self.records:
            self.assertFalse(any(c['term'] in {'enemy', 'npc', 'player'} for c in record['asset']['subjects']), record['id'])

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

    def test_silhouette_evidence_does_not_upgrade_frozen_identity(self):
        r = self.by_id['urizen-onebit-r23-c104']
        self.assertEqual(r['identity']['confidence'], 'low')
        claims = [c for c in r['asset']['subjects'] if c['term'] == 'golem']
        self.assertTrue(claims)
        self.assertTrue(all(c['confidence'] == 'medium' and c['basis'] == 'publisher_silhouette_match' for c in claims))

    def test_wrong_silhouette_evidence_is_rejected(self):
        r = self.by_id['urizen-onebit-r23-c104']
        with self.assertRaisesRegex(ValueError, 'silhouette evidence'):
            describe(r, r['asset']['appearance'], {}, {}, {'mask_sha256': '0' * 64, 'matches': []})

    def test_conflicting_silhouette_names_do_not_become_asserted_subjects(self):
        r = self.by_id['urizen-twobit-r05-c010']
        evidence = {'mask_sha256': r['asset']['appearance']['foreground_mask_sha256'], 'matches': [
            {'properties': {'group1': 'animal', 'group2': 'fox'}},
            {'properties': {'group1': 'building', 'group2': 'house'}}]}
        asset = describe(r, r['asset']['appearance'], {}, {}, evidence)
        self.assertFalse(any(s['basis'] == 'publisher_silhouette_match' for s in asset['subjects']))
        self.assertEqual(asset['evidence']['review_status'], 'category_only')

    def test_two_bit_visual_description_corrects_working_label_separately(self):
        r = self.by_id['urizen-twobit-r06-c012']
        self.assertIn('chest', r['name'])
        self.assertIn('medical case', r['asset']['name'].lower())
        self.assertEqual(r['asset']['type'], 'item')
        self.assertTrue(r['asset']['evidence']['inspection_refs'])

    def test_matrix_series_do_not_turn_props_into_jobs_or_animations(self):
        r = self.by_id['urizen-onebit-r04-c105']
        a = r['asset']
        self.assertTrue(matches(r, parser().parse_args(['--search', 'hammer'])))
        self.assertTrue(any(o['scope'] == 'series' for o in a['evidence']['observations']))
        self.assertFalse(r['character_matrix']['species_and_profession_confirmed'])
        self.assertEqual(a['composition']['animation'], 'unconfirmed')
        self.assertTrue(any(c['term'] == 'goblin_like' and c['confidence'] == 'low' for c in a['subjects']))

    def test_exceptional_matrix_edge_does_not_inherit_wrong_body(self):
        r = self.by_id['urizen-onebit-r22-c203']
        self.assertTrue(any(c['term'] == 'ogre' for c in r['asset']['subjects']))
        self.assertFalse(any(o['id'] == 'body-row-22' for o in r['asset']['evidence']['observations']))

    def test_prone_pose_is_not_a_claim_of_death(self):
        a = self.by_id['urizen-onebit-r00-c201']['asset']
        self.assertIn('does not establish death', a['description'])
        self.assertEqual(a['composition']['animation'], 'unconfirmed')

    def test_non_assets_are_explicitly_opt_in(self):
        record = next(r for r in self.records if r['kind'] == 'blank')
        self.assertFalse(matches(record, parser().parse_args([])))
        self.assertTrue(matches(record, parser().parse_args(['--include-non-assets'])))

    def test_publisher_identified_tiny_glyph_is_not_discarded_as_residue(self):
        rows = [r for r in self.records if r['kind'] == 'sprite' and r['category'] == 'font' and r['foreground_pixels'] <= 2
                and r['identity']['basis'] == 'author_metadata_visible_rgb_match']
        self.assertTrue(rows)
        self.assertTrue(all(r['asset']['type'] == 'glyph' for r in rows))

    def test_reviewed_single_pixel_frame_corner_remains_a_fragment(self):
        r = self.by_id['urizen-onebit-r42-c026']
        self.assertEqual(r['foreground_pixels'], 1)
        self.assertEqual(r['asset']['type'], 'fragment')
        self.assertEqual(r['asset']['composition']['kind'], 'fragment_candidate')
        self.assertTrue(any('neighboring assembly' in note for note in r['asset']['evidence']['uncertainties']))

    def test_bottom_figure_does_not_inherit_unrelated_tool_matrix(self):
        r = self.by_id['urizen-onebit-r40-c105']
        self.assertIn('hammer', r['name'].lower())  # Frozen working label is preserved.
        self.assertIsNone(r['asset']['relations']['character_matrix'])
        self.assertNotIn('hammer', r['asset']['name'].lower())
        self.assertFalse(any(o['scope'] == 'series' for o in r['asset']['evidence']['observations']))

    def test_every_occupied_crop_has_inspection_and_useful_evidence(self):
        occupied = [r for r in self.records if r['kind'] == 'sprite']
        self.assertEqual(len(occupied), 7115)
        for r in occupied:
            self.assertTrue(r['asset']['evidence']['inspection_refs'], r['id'])
            self.assertNotIn(r['asset']['evidence']['review_status'], ['category_only', 'inspected_unresolved'], r['id'])

    def test_icons_are_searchable_by_visible_shape(self):
        r = self.by_id['godot-white-event']
        self.assertEqual(r['name'], 'event')
        self.assertTrue(matches(r, parser().parse_args(['--type', 'ui_icon', '--subject', 'exclamation', '--subject-confidence', 'medium'])))
        self.assertFalse(matches(r, parser().parse_args(['--subject', 'exclamation', '--subject-confidence', 'high'])))
        face = self.by_id['godot-color-character']['asset']
        self.assertIn('rounded face', face['name'])
        self.assertTrue(any(o['scope'] == 'individual' for o in face['evidence']['observations']))
        particle = self.by_id['godot-color-particle']['asset']
        self.assertIn('filled white disk', particle['description'].lower())

    def test_input_variant_groups_preserve_device_and_appearance(self):
        prompts = [r for r in self.records if r['kind'] == 'sprite' and r['pack'] == 'input-prompts']
        for r in prompts:
            self.assertEqual(r['asset']['relations']['input_prompt']['visual_state'], r['visual_state'])
        key = next(r for r in prompts if r['control'] == 'A' and r['category'] == 'keyboard')
        button = next(r for r in prompts if r['control'] == 'A' and r['category'] == 'controller')
        self.assertNotEqual(key['asset']['relations']['named_variant_group'], button['asset']['relations']['named_variant_group'])

    def test_font_codepoints_come_from_publisher_labels(self):
        glyphs = [r['asset']['relations']['glyph'] for r in self.records if r['asset']['relations']['glyph']]
        self.assertEqual(len(glyphs), 146)
        for g in glyphs:
            self.assertEqual(g['codepoint'], f"U+{ord(g['character']):04X}")
        question = next(g for g in glyphs if 'question' in g['publisher_labels'])
        self.assertEqual(question['character'], '?')
        self.assertIsNone(self.by_id['urizen-onebit-r49-c102']['asset']['relations']['glyph'])

    def test_neutral_exports_and_viewer_retain_canonical_evidence(self):
        index = json.loads((ROOT / 'assets/atlas-index.json').read_text())
        self.assertEqual({r['id'] for r in index}, {r['id'] for r in self.records if r['kind'] == 'sprite'})
        for r in index:
            self.assertEqual(neutral_record(r), neutral_record(self.by_id[r['id']]), r['id'])

    def test_every_downloadable_png_preserves_frozen_rgba(self):
        html = (ROOT / 'assets/atlas.html').read_text()
        embedded = json.loads(html.split('<script id="crops" type="application/json">', 1)[1].split('</script>', 1)[0])
        self.assertEqual(set(embedded), {r['id'] for r in self.records if r['kind'] == 'sprite'})
        for sid, encoded in embedded.items():
            image = Png(base64.b64decode(encoded))
            self.assertEqual([image.width, image.height], self.by_id[sid]['rect'][2:], sid)
            self.assertEqual(hashlib.sha256(image.rgba).hexdigest(), self.by_id[sid]['rgba_sha256'], sid)

    def test_qualified_visual_names_do_not_assert_exact_species(self):
        claims = named_visual_subjects('A gray snake-like creature', 'medium', 'fixture')
        self.assertTrue(any(c['term'] == 'snake_like' and c['confidence'] == 'low' for c in claims))
        self.assertFalse(any(c['term'] == 'snake' for c in claims))

    def test_paw_print_is_not_an_animal_actor(self):
        r = self.by_id['urizen-onebit-r14-c024']
        self.assertEqual(r['asset']['type'], 'symbol')
        self.assertEqual(r['identity']['confidence'], 'low')

    def test_portable_publisher_objects_use_consistent_item_type(self):
        for sid in ['urizen-onebit-r43-c002', 'urizen-onebit-r37-c001', 'urizen-onebit-r09-c065']:
            self.assertEqual(self.by_id[sid]['asset']['type'], 'item', sid)

    def test_empty_facets_and_stable_sort(self):
        self.assertEqual(facets([]), {'types': {}, 'primary_colors': {}, 'subjects': {}})
        rows = sorted(self.records[:10], key=lambda r: sort_key(r, 'color'))
        self.assertEqual(len(rows), 10)
        self.assertEqual(rows, sorted(rows, key=lambda r: sort_key(r, 'color')))


if __name__ == '__main__':
    unittest.main()
