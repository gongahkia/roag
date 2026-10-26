import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools'))
from agent_support.workspace import Repository, atomic_json, identifier
from agent_support.discovery import impact

class WorkspaceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.root = Path(self.temp.name)
        subprocess.run(['git', 'init', '-q', str(self.root)], check=True)
        (self.root / '.gitignore').write_text('artifacts/\n')
        (self.root / 'source.cs').write_text('original')
        subprocess.run(['git', '-C', str(self.root), 'add', '.'], check=True)
        subprocess.run(['git', '-C', str(self.root), '-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.test', 'commit', '-qm', 'fixture'], check=True)
        self.repo = Repository(self.root)
        (self.root / 'devtools').mkdir(); (self.root / 'devtools/areas.json').write_bytes((ROOT / 'devtools/areas.json').read_bytes())
    def tearDown(self): self.temp.cleanup()
    def test_fingerprint_tracks_unstaged_untracked_deletion_and_mode_but_not_logs_or_docs(self):
        initial = self.repo.fingerprint()['sha256']
        (self.root / 'source.cs').write_text('changed'); self.assertNotEqual(initial, self.repo.fingerprint()['sha256'])
        (self.root / 'source.cs').write_text('original'); self.assertEqual(initial, self.repo.fingerprint()['sha256'])
        (self.root / 'new file; $(touch nope).cs').write_text('new'); self.assertNotEqual(initial, self.repo.fingerprint()['sha256'])
        (self.root / 'new file; $(touch nope).cs').unlink()
        (self.root / 'source.cs').unlink(); self.assertNotEqual(initial, self.repo.fingerprint()['sha256'])
        (self.root / 'source.cs').write_text('original'); (self.root / 'notes.md').write_text('notes')
        atomic_json(self.root / 'artifacts/ignored.json', {'passed': True}); self.assertEqual(initial, self.repo.fingerprint()['sha256'])
        (self.root / 'source.cs').chmod(0o755); self.assertNotEqual(initial, self.repo.fingerprint()['sha256'])
    def test_impact_never_silently_drops_unknown_paths(self):
        result = impact(self.repo, ['src/Roag.Client/WorldGame.cs'])
        self.assertIn('native', result['recommendedProfiles'])
        self.assertEqual(['full'], impact(self.repo, ['unmapped.config'])['recommendedProfiles'])
    def test_party_and_encounter_content_requests_its_actual_native_scenarios(self):
        for path, scene in [('content/talent/bulwark.json', 'world-progression'),
                            ('content/encounter/cinder-retinue.json', 'world-encounters'),
                            ('content/reward/cinder-retinue.json', 'world-boss')]:
            result = impact(self.repo, [path])
            self.assertIn('native', result['recommendedProfiles'])
            self.assertIn('world', result['recommendedProfiles'])
            self.assertIn(scene, result['nativeScenes'])
            self.assertFalse(result['unmapped'])
    def test_changes_include_staged_and_untracked_files(self):
        (self.root / 'source.cs').write_text('changed'); subprocess.run(['git','-C',str(self.root),'add','source.cs'],check=True)
        (self.root / 'untracked.cs').write_text('new')
        self.assertIn('source.cs', self.repo.changed()); self.assertIn('untracked.cs', self.repo.changed('HEAD'))
        with self.assertRaises(ValueError): self.repo.changed('--output=/tmp/nope')
    def test_identifiers_and_atomic_json(self):
        for value in ['../escape', '', 'a/b', 'UPPER', 'x' * 81]:
            with self.assertRaises(ValueError): identifier(value)
        p = self.root / 'artifacts/state.json'; atomic_json(p, {'version': 1}); atomic_json(p, {'version': 2})
        self.assertEqual(2, json.loads(p.read_text())['version']); self.assertEqual([p], list(p.parent.iterdir()))
    def test_unknown_cli_options_fail_as_json(self):
        p = subprocess.run([sys.executable, str(ROOT/'tools/agent_tools.py'),'context','--typo'],capture_output=True,text=True)
        self.assertEqual(1,p.returncode); self.assertFalse(json.loads(p.stdout)['ok'])

if __name__ == '__main__': unittest.main()
