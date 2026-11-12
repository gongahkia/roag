"""Explicit classic casting coverage, source preservation and fail-closed export."""
import copy
import gzip
import json
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools'))
from asset_catalog import encode_rgba_png
from build_classic_assets import build, read_json, render_lua, resolve_bindings, sha256
from catalog_io import load_catalog


class ClassicFixtureTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = 'godot-node-icons/white/fixture.png'
        self.path = self.root / 'assets' / self.source
        self.path.parent.mkdir(parents=True)
        self.rgba = bytes([255, 255, 255, 255, 255, 0, 0, 255])
        self.path.write_bytes(encode_rgba_png(2, 1, self.rgba))
        self.record = {'id': 'fixture-crop', 'pack': 'godot-node-icons', 'kind': 'sprite',
                       'source': self.source, 'rect': [0, 0, 2, 1], 'foreground_pixels': 2,
                       'rgba_sha256': sha256(self.rgba), 'identity': {'confidence': 'low', 'basis': 'visual_observation'},
                       'asset': {'name': 'uncertain motif', 'type': 'ui_icon',
                                 'subjects': [{'term': 'motif', 'confidence': 'low', 'basis': 'visual_observation'}],
                                 'composition': {'kind': 'fragment_candidate', 'animation': 'unconfirmed',
                                                 'directions': 'unconfirmed', 'sockets': 'unauthored', 'collision': 'game_defined'}}}
        self.catalog = {'sprites': [self.record], 'images': [{'path': self.source, 'pack': 'godot-node-icons',
                        'width': 2, 'height': 1, 'sha256': sha256(self.path.read_bytes())}]}
        self.inventory = {'requirements': ['core:fixture', 'core:fixture/attack'], 'packs': {'onebit': {
                          'bindings': [{'id': 'core:fixture'}, {'id': 'core:fixture/attack'}]}}}
        self.document = {'schemaVersion': 1, 'sourceCatalog': 'catalog.json.gz', 'bindings': {
                        role: {'sprite': 'fixture-crop', 'notes': 'Local "casting"; static\\repeat\nuncertain\t\x01'}
                        for role in self.inventory['requirements']}}
        self.bindings = self.root / 'assets/classic-bindings.json'
        self.output = self.root / 'src/classic_sprite_data.lua'
        self.catalog_path = self.root / 'assets/catalog.json.gz'
        self.inventory_path = self.root / 'docs/sprite-audit/inventory.json'
        self.inventory_path.parent.mkdir(parents=True)
        self.flush()

    def flush(self):
        self.bindings.write_text(json.dumps(self.document))
        self.catalog_path.write_bytes(gzip.compress(json.dumps(self.catalog).encode(), mtime=0))
        self.inventory_path.write_text(json.dumps(self.inventory))

    def resolve(self):
        return resolve_bindings(self.document, self.inventory, self.catalog, sha256(self.catalog_path.read_bytes()), self.root / 'assets', sha256(self.bindings.read_bytes()))

    def test_deterministic_lua_preserves_static_reuse_uncertainty_and_control_characters(self):
        first = self.resolve()
        self.document['bindings'] = dict(reversed(list(self.document['bindings'].items())))
        self.assertEqual(render_lua(first), render_lua(self.resolve()))
        a, b = first['bindings'].values()
        self.assertEqual(a, b)
        self.assertEqual(a['identity_confidence'], 'low')
        self.assertEqual(a['composition']['animation'], 'unconfirmed')
        self.assertEqual(a['composition']['kind'], 'fragment_candidate')
        self.flush()
        build(self.bindings, self.output, root=self.root)
        executable = shutil.which('luajit') or shutil.which('lua')
        if executable:
            code = 'local d=dofile(arg[0]); local b=d.bindings["core:fixture"]; assert(d.requirement_count==2); assert(b.notes:find(string.char(1),1,true)); assert(b.id==d.bindings["core:fixture/attack"].id); assert(b.composition.animation=="unconfirmed")'
            subprocess.run([executable, '-e', code, '--', str(self.output)], check=True, capture_output=True)

    def test_binding_edit_changes_fingerprint_and_check_rejects_stale_generated_data(self):
        original = build(self.bindings, self.output, root=self.root)
        first = self.output.read_bytes()
        self.document['bindings']['core:fixture']['notes'] = 'Changed local casting only'
        self.flush()
        with self.assertRaisesRegex(ValueError, 'Stale classic sprite data'):
            build(self.bindings, self.output, check=True, root=self.root)
        changed = build(self.bindings, self.output, root=self.root)
        self.assertNotEqual(original['bindings_sha256'], changed['bindings_sha256'])
        self.assertEqual(original['catalog_sha256'], changed['catalog_sha256'])
        self.assertEqual(changed['bindings_sha256'], sha256(self.bindings.read_bytes()))
        self.assertEqual(changed['requirements'], sorted(self.inventory['requirements']))
        self.assertNotEqual(first, self.output.read_bytes())

    def test_missing_extra_unknown_and_alias_bindings_fail_without_fallback(self):
        original = copy.deepcopy(self.document)
        cases = [lambda: self.document['bindings'].pop('core:fixture'),
                 lambda: self.document['bindings'].update({'surprise': original['bindings']['core:fixture']}),
                 lambda: self.document['bindings']['core:fixture'].update(sprite='unknown'),
                 lambda: self.document['bindings']['core:fixture'].update(aliasOf='core:fixture/attack'),
                 lambda: self.document.update(sourceCatalog='sprites.json')]
        for mutate in cases:
            self.document = copy.deepcopy(original)
            mutate()
            with self.assertRaises(ValueError):
                self.resolve()

    def test_empty_guide_unsupported_pack_and_foreground_mismatch_fail(self):
        original = copy.deepcopy(self.record)
        for changes in [{'kind': 'guide'}, {'foreground_pixels': 0}, {'pack': 'kenney'}, {'foreground_pixels': 1}]:
            self.record.clear()
            self.record.update(copy.deepcopy(original))
            self.record.update(changes)
            with self.assertRaises(ValueError):
                self.resolve()

    def test_source_hash_dimensions_crop_hash_bounds_and_path_escape_fail(self):
        original = copy.deepcopy(self.catalog)
        changes = [lambda: self.catalog['images'][0].update(sha256='0' * 64),
                   lambda: self.catalog['images'][0].update(width=3),
                   lambda: self.catalog['sprites'][0].update(rgba_sha256='0' * 64),
                   lambda: self.catalog['sprites'][0].update(rect=[0, 0, 3, 1]),
                   lambda: self.catalog['sprites'][0].update(rect=[True, 0, 1, 1]),
                   lambda: self.catalog['sprites'][0].update(source='../outside.png')]
        for mutate in changes:
            self.catalog = copy.deepcopy(original)
            mutate()
            with self.assertRaises(ValueError):
                self.resolve()
        self.catalog = copy.deepcopy(original)
        self.path.write_bytes(self.path.read_bytes() + b'tampered')
        with self.assertRaisesRegex(ValueError, 'Source PNG hash'):
            self.resolve()

    def test_source_grid_gutters_and_duplicate_json_keys_fail(self):
        self.catalog['images'][0]['grid'] = {'columns': 1, 'rows': 1, 'margin_x': 1, 'margin_y': 0,
                                           'tile_width': 1, 'tile_height': 1, 'spacing_x': 1, 'spacing_y': 1}
        self.record['grid'] = [0, 0]
        with self.assertRaisesRegex(ValueError, 'gutter'):
            self.resolve()
        self.bindings.write_text('{"bindings": {}, "bindings": {}}')
        with self.assertRaisesRegex(ValueError, 'Duplicate JSON key'):
            read_json(self.bindings)

    def test_check_never_writes_and_rejects_stale_lua_or_changed_catalog_evidence(self):
        build(self.bindings, self.output, root=self.root)
        originals = {p: p.read_bytes() for p in [self.path, self.bindings, self.catalog_path, self.inventory_path]}
        built = self.output.read_bytes()
        build(self.bindings, self.output, check=True, root=self.root)
        self.assertEqual(built, self.output.read_bytes())
        self.output.write_bytes(built + b'-- stale\n')
        with self.assertRaisesRegex(ValueError, 'Stale classic sprite data'):
            build(self.bindings, self.output, check=True, root=self.root)
        self.assertEqual(self.output.read_bytes(), built + b'-- stale\n')
        self.output.write_bytes(built)
        self.catalog['sprites'][0]['identity']['confidence'] = 'medium'
        self.catalog_path.write_bytes(gzip.compress(json.dumps(self.catalog).encode(), mtime=0))
        with self.assertRaisesRegex(ValueError, 'Stale classic sprite data'):
            build(self.bindings, self.output, check=True, root=self.root)
        self.assertEqual(self.output.read_bytes(), built)
        for path in [self.path, self.bindings, self.inventory_path]:
            self.assertEqual(originals[path], path.read_bytes())
        with self.assertRaisesRegex(ValueError, 'cannot overwrite'):
            build(self.bindings, self.path, root=self.root)
        self.assertEqual(originals[self.path], self.path.read_bytes())


class ClassicOriginalTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.bindings_path = ROOT / 'assets/classic-bindings.json'
        cls.catalog = load_catalog()
        cls.document = read_json(cls.bindings_path)
        cls.inventory = read_json(ROOT / 'docs/sprite-audit/inventory.json')
        cls.selected = {r['source'] for r in cls.catalog['sprites']
                        if r['id'] in {b['sprite'] for b in cls.document['bindings'].values()}}
        cls.paths = [ROOT / 'assets/catalog.json.gz', cls.bindings_path,
                     *[ROOT / 'assets' / p for p in sorted(cls.selected)]]
        cls.before = {p: sha256(p.read_bytes()) for p in cls.paths}
        cls.data = resolve_bindings(cls.document, cls.inventory, cls.catalog,
                                   cls.before[ROOT / 'assets/catalog.json.gz'], ROOT / 'assets', cls.before[cls.bindings_path])

    def test_all_599_requirements_are_explicit_original_crops_with_original_evidence(self):
        self.assertEqual(599, self.data['requirement_count'])
        self.assertEqual(sorted(self.inventory['requirements']), self.data['requirements'])
        self.assertEqual(sha256(self.bindings_path.read_bytes()), self.data['bindings_sha256'])
        self.assertEqual(set(self.inventory['requirements']), set(self.data['bindings']))
        records = {r['id']: r for r in self.catalog['sprites']}
        for role, binding in self.data['bindings'].items():
            original = records[self.document['bindings'][role]['sprite']]
            self.assertEqual([binding[k] for k in ['x', 'y', 'width', 'height']], original['rect'])
            self.assertEqual(binding['rgba_sha256'], original['rgba_sha256'])
            self.assertEqual(binding['subjects'], original['asset']['subjects'])
            self.assertEqual(binding['composition'], original['asset']['composition'])
            self.assertEqual(binding['source'], 'assets/' + original['source'])
            self.assertNotIn('kenney', binding['source'].lower())

    def test_generated_output_is_current_and_source_background_rules_are_explicit(self):
        expected = render_lua(self.data)
        self.assertEqual((ROOT / 'src/classic_sprite_data.lua').read_bytes(), expected)
        self.assertEqual(expected, render_lua(self.data))
        for path, source in self.data['sources'].items():
            is_urizen = path.startswith('assets/urizen/')
            self.assertEqual(source['background'], 'black_to_transparent' if is_urizen else 'preserve')
            self.assertEqual(source['black_threshold'], 8 if is_urizen else 0)
        self.assertEqual(self.before, {p: sha256(p.read_bytes()) for p in self.paths})


if __name__ == '__main__':
    unittest.main()
