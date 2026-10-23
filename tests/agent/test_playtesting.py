import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

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
        structured = dict.fromkeys(model.ACTION_PROPERTIES); structured.update(kind='control', control='panel:inventory')
        self.assertEqual({'kind': 'control', 'control': 'panel:inventory'}, driver.validate_action(structured))
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

    def test_model_stdout_and_waiting_are_bounded_without_invoking_a_model(self):
        code, output = model.execute([sys.executable, '-c', 'import sys; sys.stdin.read(); print("x" * 2500000)'], 'fixture', 5)
        self.assertEqual(0, code); self.assertLessEqual(len(output.encode()), 1048576)
        with self.assertRaises(subprocess.TimeoutExpired):
            model.execute([sys.executable, '-c', 'import time; time.sleep(10)'], 'x' * 100000, .1)

    def run_budget_fixture(self, decision_seconds, *, model_timeout=False, proposed_plan=None, done=False, journey=False, max_steps=1, invalid_first=0, comments=None):
        clock = [0]
        observed = {'processAlive': True, 'screenshot': None, 'state': {'observation': 1, 'position': {'x': 1, 'y': 2, 'floor': 0}, 'party': [{}, {}, {}]}}
        result = {'step': 1, 'delivered': True, 'response': {'reason': 'delivered'},
                  'state': {'observation': 2, 'notice': 'Moved', 'panel': '', 'position': {'x': 1, 'y': 2, 'floor': 0}}}
        atomic_json(self.session / 'observation.json', {'observation': 1, 'screenshot': None, 'ui': observed['state']})

        attempts = [0]
        def execute(*args):
            attempts[0] += 1
            clock[0] = decision_seconds
            if model_timeout:
                raise subprocess.TimeoutExpired('fixture-model', decision_seconds)
            atomic_json(self.session / 'model/answer.json', {
                'reason': 'More action is needed', 'done': done,
                'action': None if done else {'kind': 'wait', 'seconds': 5, 'button': 0 if attempts[0] <= invalid_first else None}, 'finding': None, 'plan': proposed_plan, 'comments': comments or []})
            return 0, ''

        with patch.object(model.driver, 'location', return_value=(self.session, {'steps': 0})), \
             patch.object(model.driver, 'observe', return_value=observed), \
             patch.object(model.driver, 'act', return_value=result) as act, \
             patch.object(model.driver, 'stop') as stop, \
             patch.object(model.shutil, 'which', return_value='/installed/codex'), \
             patch.object(model.time, 'monotonic', side_effect=lambda: clock[0]), \
             patch.object(model, 'execute', side_effect=execute):
            report = model.run(None, 'fixture', 'gpt-5.6-luna', 'Test queued work', max_steps=max_steps, timeout=20, journey=journey)
            stop.assert_called_once()
        return report, act

    def test_model_budget_does_not_submit_wait_that_cannot_finish(self):
        report, act = self.run_budget_fixture(16)
        self.assertEqual('time-budget', report['status'])
        self.assertEqual({'kind': 'wait', 'seconds': 5}, report['deferredAction'])
        self.assertEqual(1, report['decisions'])
        act.assert_not_called()
        self.assertFalse((self.session / 'pending.json').exists())

    def test_model_budget_delivers_wait_when_receipt_fits(self):
        report, act = self.run_budget_fixture(5)
        self.assertEqual('step-budget', report['status'])
        self.assertEqual(1, report['lastStep'])
        act.assert_called_once()
        self.assertEqual(15, act.call_args.kwargs['timeout'])

    def test_unknown_planned_destination_preserves_evidence_and_does_not_abort_a_valid_action(self):
        report, act = self.run_budget_fixture(5, proposed_plan={
            'goal': 'Recruit a visible person', 'targetId': '1000', 'nextStep': 'Talk', 'remember': ''})
        self.assertEqual('step-budget', report['status'])
        self.assertEqual(1, report['recoveredPlanProposals'])
        act.assert_called_once()
        stored = json.loads((self.session / 'player-memory.json').read_text())
        self.assertIsNone(stored['plan'])
        self.assertIn('player-observed', stored['planFeedback'])

    def test_model_cannot_claim_journey_finished_from_an_unmet_micro_goal(self):
        report, act = self.run_budget_fixture(5, done=True, journey=True, max_steps=3)
        self.assertEqual(2, report['prematureFinishes'])
        self.assertEqual(3, report['decisions'])
        self.assertEqual('step-budget', report['status'])
        self.assertFalse(report['coverage']['journeyComplete'])
        act.assert_not_called()

    def test_bad_model_button_is_rejected_without_input_then_repaired(self):
        report, act = self.run_budget_fixture(5, invalid_first=1, max_steps=2)
        self.assertEqual('step-budget', report['status'])
        self.assertEqual(2, report['decisions'])
        self.assertEqual(1, report['rejectedDecisions'])
        act.assert_called_once()
        self.assertNotIn('decisionFeedback', json.loads((self.session / 'player-memory.json').read_text()))
        rejected = json.loads((self.session / 'model/decision-0.json').read_text())
        self.assertIn('button', rejected['rejected'])

    def test_repeated_invalid_model_proposals_stop_with_a_bounded_error(self):
        report, act = self.run_budget_fixture(5, invalid_first=10, max_steps=5)
        self.assertEqual('model-error', report['status'])
        self.assertEqual(3, report['decisions'])
        self.assertEqual(3, report['rejectedDecisions'])
        act.assert_not_called()

    def test_rejected_action_keeps_valid_playtester_comments(self):
        comment = {'topic': 'interaction', 'impression': 'mixed', 'observed': 'A clear place name is displayed.',
                   'thought': 'I know where I want to go but am unsure how the map starts travel.',
                   'expected': 'The marker would start movement.', 'suggestion': '', 'confidence': 'medium'}
        report, act = self.run_budget_fixture(5, invalid_first=1, max_steps=2, comments=[comment])
        notes = json.loads((self.session / 'feedback.json').read_text())
        self.assertEqual(1, len(notes))
        self.assertEqual(comment['thought'], notes[0]['thought'])
        self.assertEqual(1, notes[0]['observation'])
        self.assertEqual(1, report['coverage']['experientialComments'])
        act.assert_called_once()

    def test_model_timeout_distinguishes_overall_budget_from_slow_decision(self):
        for elapsed, expected in [(20, 'time-budget'), (5, 'model-error')]:
            with self.subTest(elapsed=elapsed):
                report, act = self.run_budget_fixture(elapsed, model_timeout=True)
                self.assertEqual(expected, report['status'])
                self.assertEqual(expected == 'model-error', 'error' in report)
                act.assert_not_called()

    def test_disconnected_controller_recovers_completed_actions_exactly_once(self):
        run_id = 'fixture-run'; path = self.root / run_id; path.mkdir()
        atomic_json(path / 'playtest.json', {'schemaVersion': 1, 'id': run_id, 'steps': 0, 'process': None})
        before = {'observation': 1, 'ui': {'tick': 0, 'position': {'x': 1, 'y': 2}, 'panel': ''}}
        after = {'observation': 2, 'screenshot': None, 'ui': {**before['ui'], 'notice': 'Moved'}}
        atomic_json(path / 'observation.json', after)
        atomic_json(path / 'pending.json', {'step': 1, 'request': {'id': 'one-action', 'kind': 'tile'}, 'before': before})
        with patch.object(driver.runner, 'alive', return_value=True):
            with self.assertRaisesRegex(ValueError, 'still pending'):
                driver.act(type('Repo', (), {'root': self.root})(), run_id, {'kind': 'capture'}, root=self.root)
        self.assertFalse((path / 'request.json').exists())
        atomic_json(path / 'response.json', {'id': 'one-action', 'success': True, 'observation': 2})
        result = driver.reconcile(path); self.assertEqual(1, result['step'])
        self.assertIsNone(driver.reconcile(path)); self.assertEqual(1, json.loads((path / 'playtest.json').read_text())['steps'])
        self.assertFalse((path / 'pending.json').exists())

    def test_failed_capture_cannot_reuse_an_older_steps_screenshot(self):
        observation = {'observation': 1, 'screenshot': 'frame.png', 'ui': {'tick': 0, 'position': {'x': 1, 'y': 2}, 'panel': '', 'notice': ''}}
        (self.session / 'frame.png').write_bytes(b'old image')
        first = driver.retain_step(self.session, 1, {}, {}, observation, observation)
        self.assertTrue(Path(first['screenshot']).is_file())
        failed = {**observation, 'screenshot': None}
        later = driver.retain_step(self.session, driver.RETAINED_STEPS + 1, {}, {}, observation, failed)
        self.assertIsNone(later['screenshot']); self.assertFalse(Path(first['screenshot']).exists())


if __name__ == '__main__':
    unittest.main()
