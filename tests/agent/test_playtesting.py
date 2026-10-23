import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools'))
from agent_support.workspace import atomic_json
from agent_support import runner
from playtest_support import recordings, driver, model


class PlaytestingTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.root = Path(self.temp.name)
        self.session = self.root / 'fixture-session'; self.session.mkdir()
        atomic_json(self.session / 'session.json', {'schemaVersion': 1, 'id': self.session.name, 'started': 'fixture',
                    'identity': {'selectedSave': str(self.root / 'player-save-do-not-open.json'), 'requestedSeed': 42}})
        (self.root / 'player-save-do-not-open.json').write_text('intentionally invalid game data')

    def tearDown(self):
        self.temp.cleanup()

    def test_session_reports_never_open_player_save_and_tolerate_interrupted_tail(self):
        atomic_json(self.session / 'summary.json', {'status': 'running', 'droppedEvents': 2, 'metrics': {'commandsRejected': 4}, 'markers': []})
        atomic_json(self.session / 'latest.json', {'state': {'world': {'seed': 1337, 'tick': 10}}})
        (self.session / 'events-00.jsonl').write_text(json.dumps({'sequence': 4, 'kind': 'command'}) + '\n{"partial"')
        (self.session / 'events-01.jsonl').write_text(json.dumps({'sequence': 2, 'kind': 'navigation'}) + '\n')
        result = recordings.report(self.root, self.session.name)
        self.assertEqual([2, 4], [e['sequence'] for e in result['recentEvents']['events']])
        self.assertEqual(1, result['recentEvents']['malformedLines']); self.assertEqual(2, len(result['warnings']))
        self.assertEqual(1337, recordings.sessions(self.root)[0]['seed'])
        self.assertEqual('intentionally invalid game data', (self.root / 'player-save-do-not-open.json').read_text())
        self.assertEqual(4, recordings.events(self.root, self.session.name, after=2)['events'][0]['sequence'])
        with self.assertRaises(ValueError):
            recordings.events(self.root, self.session.name, limit=201)

    def test_recordings_reject_traversal_symlinks_and_oversized_files(self):
        for value in ['../escape', 'UPPER', '']:
            with self.assertRaises(ValueError):
                recordings.folder(self.root, value)
        (self.root / 'linked').symlink_to(self.session, target_is_directory=True)
        with self.assertRaises(ValueError):
            recordings.folder(self.root, 'linked')
        (self.session / 'summary.json').write_bytes(b'x' * 1048577)
        with self.assertRaises(ValueError):
            recordings.report(self.root, self.session.name)

    def test_mouse_actions_are_bounded_typed_and_have_no_rule_mutation_commands(self):
        valid = driver.validate_action({'kind': 'tile', 'x': 11, 'y': 12, 'label': None})
        self.assertNotIn('label', valid)
        for action in [{'kind': 'teleport'}, {'kind': 'control', 'cheat': True}, {'kind': 'wait', 'seconds': 60},
                       {'kind': 'wait', 'seconds': float('nan')}, {'kind': 'point', 'x': True},
                       {'kind': 'control', 'control': 42}, {'kind': 'mark', 'label': 42},
                       {'kind': 'tile', 'button': 2}, {'kind': 'mark', 'label': 'x' * 9000}, ['control']]:
            with self.assertRaises(ValueError):
                driver.validate_action(action)

    def test_compact_observations_only_map_supplied_visible_cells(self):
        observation = {'observation': 1, 'utc': 'fixture', 'screenshot': 'frame.png', 'ui': {
            'cells': [{'x': 10, 'y': 20, 'material': 'Grass'}, {'x': 12, 'y': 20, 'material': 'Wall'}],
            'position': {'x': 10, 'y': 20, 'floor': 1}, 'party': [{'id': 1, 'x': 10, 'y': 20, 'floor': 1}],
            'actors': [], 'controls': [], 'panel': '', 'paused': True, 'tick': 0, 'notice': '', 'routeCount': 0}}
        result = driver.compact(observation)
        self.assertEqual(['1?#'], result['map']['rows']); self.assertEqual(1, result['map']['floor'])
        observation['ui']['cells'].append({'x': 4095, 'y': 4095, 'material': 'Wall'})
        with self.assertRaises(ValueError):
            driver.compact(observation)

    def test_step_evidence_rotates_and_preserves_actual_action_and_outcome(self):
        snapshot = {'observation': 1, 'ui': {'tick': 0, 'position': {'x': 1, 'y': 2}, 'panel': '', 'notice': 'Moved'}}
        (self.session / 'frame.png').write_bytes(b'fixture-image')
        for index in range(1, 70):
            driver.retain_step(self.session, index, {'kind': 'capture'}, {'success': True}, snapshot, snapshot)
        files = list((self.session / 'steps').glob('slot-*.json'))
        self.assertEqual(driver.RETAINED_STEPS * 2, len(files))
        steps = [json.loads(p.read_text())['step'] for p in (self.session / 'steps').glob('slot-*.json') if not p.name.endswith('.observation.json')]
        self.assertEqual(list(range(6, 70)), sorted(steps))

    def test_codex_invocation_preserves_requested_model_and_read_only_sandbox(self):
        argv = model.command('/installed/codex', 'gpt-5.6-luna', 'low', self.root, self.root / 'schema.json', self.root / 'answer.json', self.root / 'frame.png')
        self.assertEqual('gpt-5.6-luna', argv[argv.index('--model') + 1]); self.assertIn('model_reasoning_effort=low', argv)
        self.assertEqual('read-only', argv[argv.index('--sandbox') + 1]); self.assertIn('--output-schema', argv); self.assertIn('--image', argv)
        self.assertIn('--ephemeral', argv)
        self.assertNotIn('--dangerously-bypass-approvals-and-sandbox', argv)
        model.validate_decision({'reason': 'Read the actual panel', 'done': False, 'action': {'kind': 'capture'}, 'finding': None})
        with self.assertRaises(ValueError):
            model.validate_decision({'reason': 'cheat', 'done': False, 'action': {'kind': 'set-health'}, 'finding': None})
        with self.assertRaises(ValueError):
            model.validate_decision({'reason': 'extra', 'done': True, 'action': None, 'finding': None, 'passed': True})

    def test_cli_errors_are_machine_readable_and_do_not_invoke_a_model(self):
        for args in [['playtest', 'agent', 'missing'], ['playtest', 'act', 'missing'], ['sessions', 'events', '../bad'], ['playtest', 'help', '--typo']]:
            process = subprocess.run([sys.executable, str(ROOT / 'tools/playtest_tools.py'), *args], capture_output=True, text=True)
            self.assertEqual(1, process.returncode); self.assertFalse(json.loads(process.stdout)['ok'])

    def test_managed_playtest_evidence_needs_actual_checks_and_both_scenes(self):
        folder = self.root / 'managed'; (folder / 'playtests').mkdir(parents=True)
        for report in [{'passed': True, 'checks': [], 'scenes': ['world', 'world-battle']},
                       {'passed': True, 'checks': [{'passed': False}], 'scenes': ['world', 'world-battle']},
                       {'passed': True, 'checks': [{'passed': True}], 'scenes': ['world']}]:
            atomic_json(folder / 'playtests/report.json', report)
            self.assertFalse(runner.inspect_evidence({'evidence': 'playtest'}, folder, None)['valid'])
        atomic_json(folder / 'playtests/report.json', {'passed': True, 'checks': [{'passed': True}], 'scenes': ['world', 'world-battle']})
        self.assertTrue(runner.inspect_evidence({'evidence': 'playtest'}, folder, None)['valid'])


if __name__ == '__main__':
    unittest.main()
