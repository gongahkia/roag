import json
from pathlib import Path
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools'))
from agent_support.workspace import atomic_json
from playtest_support import memory, experience, driver, model


class ExperientialTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.path = Path(self.temp.name)
        self.state = memory.load(self.path, 'fixture', 'Reach a learned dungeon and comment on understanding.')

    def tearDown(self):
        self.temp.cleanup()

    def observation(self, number=1, **changes):
        return {'observation': number, 'position': {'x': 1, 'y': 1, 'floor': 0}, 'party': [{'id': 1}],
                'notice': 'An inhabitant points toward the old shelter.', 'panel': '', 'routeCount': 0,
                'discoveries': [{'id': 'discovery-1', 'name': 'Old shelter', 'x': 10, 'y': 1, 'visited': False}],
                'journal': [{'text': 'Look east for the old shelter.'}], 'localGround': [], **changes}

    def test_learned_leads_and_intent_survive_many_actions_and_controller_continuation(self):
        memory.remember(self.state, self.observation())
        memory.plan(self.state, {'goal': 'Explore the shelter', 'targetId': 'discovery-1', 'nextStep': 'Walk east', 'remember': 'I already asked for directions.'})
        for i in range(2, 40):
            memory.remember(self.state, self.observation(i, discoveries=[], journal=[], notice='Moved.'))
        memory.save(self.path, self.state)
        continued = memory.load(self.path, 'fixture', 'Continue the journey')
        self.assertEqual(10, continued['places']['discovery-1']['x'])
        self.assertEqual('Walk east', continued['plan']['nextStep'])
        self.assertEqual(['Look east for the old shelter.'], continued['journal'])
        self.assertEqual(1, continued['places']['discovery-1']['sourceObservation'])
        with self.assertRaises(ValueError):
            memory.load(self.path, 'different-session', 'New objective')

    def test_model_plan_cannot_add_an_unknown_destination_or_relabel_it_as_known(self):
        memory.remember(self.state, self.observation())
        with self.assertRaisesRegex(ValueError, 'player-observed'):
            memory.plan(self.state, {'goal': 'Skip ahead', 'targetId': 'site-secret', 'nextStep': '', 'remember': ''})
        self.assertNotIn('site-secret', self.state['places'])
        target = model.decision_schema(self.state)['properties']['plan']['anyOf'][1]['properties']['targetId']
        self.assertEqual([None, 'discovery-1'], target['enum'])
        self.assertNotIn('enum', model.DECISION_SCHEMA['properties']['plan']['anyOf'][1]['properties']['targetId'])

    def test_checkpoints_follow_observations_not_model_completion_claims(self):
        memory.remember(self.state, self.observation(party=[{}, {}, {}]))
        self.assertIn('recruited-two-companions', self.state['milestones'])
        self.assertNotIn('visited-discovery', self.state['milestones'])
        self.assertNotIn('entered-dungeon', self.state['milestones'])
        guide = memory.guidance(self.state, self.observation(party=[{}, {}, {}]))
        self.assertEqual('visited-discovery', guide['nextUnmetCheckpoint'])
        self.assertNotIn('visited-discovery', guide['completedCheckpoints'])
        memory.remember(self.state, self.observation(2, discoveries=[{'id': 'discovery-1', 'name': 'Old shelter', 'x': 10, 'y': 1, 'visited': True}]))
        memory.remember(self.state, self.observation(3, position={'x': 10, 'y': 1, 'floor': 1}))
        self.assertIn('visited-discovery', self.state['milestones'])
        self.assertEqual(1, self.state['milestones']['entered-dungeon']['position']['floor'])

    def test_walking_suggestion_avoids_occupied_cells_blocked_ground_and_covered_targets(self):
        cells = [[x, y, True, False, True] for x in range(5) for y in range(3)]
        for tile in cells:
            if tile[:2] == [2, 1]: tile[3] = True
            if tile[:2] == [3, 1]: tile[2] = False
            if tile[:2] == [4, 1]: tile[4] = False
        obs = self.observation(localGround=cells, party=[{}, {}, {}])
        memory.remember(self.state, obs)
        result = memory.guidance(self.state, obs)
        target = result['suggestedAction']
        self.assertIn([target['x'], target['y']], [c[:2] for c in cells if c[2] and not c[3] and c[4]])
        self.assertEqual('discovery-1', result['target']['id'])
        self.assertIn('not an unaided', result['assistance'])

    def test_navigation_never_uses_remembered_hidden_ground_as_a_current_click(self):
        obs = self.observation(localGround=[[1, 1, True, True, True]], party=[{}, {}, {}])
        memory.remember(self.state, obs)
        self.state['terrain'].append([2, 1, 0, True])
        result = memory.guidance(self.state, obs)
        self.assertIsNone(result['suggestedAction'])
        self.assertIn('No free observed', result['hint'])

    def test_formation_dead_end_inspects_last_companion_without_teleporting_or_swapping(self):
        party = [{'x': 1, 'y': 1}, {'x': 2, 'y': 1}, {'x': 2, 'y': 2}]
        obs = self.observation(party=party, localGround=[[1, 1, True, True, True], [2, 1, True, True, True], [2, 2, True, True, True]])
        memory.remember(self.state, obs)
        result = memory.guidance(self.state, obs)
        self.assertEqual({'kind': 'tile', 'x': 2, 'y': 2, 'button': 3}, result['suggestedAction'])
        self.assertEqual([{'x': 1, 'y': 1}, {'x': 2, 'y': 1}, {'x': 2, 'y': 2}], party)

    def test_active_travel_waits_and_dungeon_stairs_are_selected_only_when_seen(self):
        obs = self.observation(routeCount=6, party=[{}, {}, {}])
        memory.remember(self.state, obs)
        self.assertEqual('wait', memory.guidance(self.state, obs)['suggestedAction']['kind'])
        memory.remember(self.state, self.observation(2, party=[{}, {}, {}], discoveries=[{'id': 'discovery-1', 'name': 'Old shelter', 'x': 10, 'y': 1, 'visited': True}],
                                                   places=[{'id': 'site-1', 'name': 'Old dungeon', 'x': 20, 'y': 1}]))
        near = self.observation(3, localFeatures=[{'x': 3, 'y': 2, 'floor': 0, 'material': 'StairsDown'}])
        self.assertEqual({'kind': 'tile', 'x': 3, 'y': 2}, memory.guidance(self.state, near)['suggestedAction'])

    def test_compact_player_view_preserves_readable_clues_without_future_dialogue_or_bypass(self):
        ui = {'position': {'x': 1, 'y': 1, 'floor': 0}, 'panel': 'journal', 'paused': True, 'tick': 0, 'notice': '', 'routeCount': 0,
              'journal': [{'text': 'Read this clue'}], 'waypoint': {'x': 5, 'y': 6, 'floor': 0},
              'dialogue': {'text': 'Hello', 'fullText': 'Unrevealed words'},
              'discoveries': [{'id': 'known', 'name': 'Shelter', 'x': 5, 'y': 6, 'visited': False, 'bypass': {'x': 999, 'y': 999}}]}
        view = driver.compact({'observation': 1, 'utc': 'fixture', 'ui': ui})
        self.assertEqual(ui['journal'], view['journal'])
        self.assertEqual(ui['waypoint'], view['waypoint'])
        self.assertNotIn('fullText', view['dialogue'])
        self.assertNotIn('bypass', view['discoveries'][0])

    def test_notebook_distinguishes_impression_evidence_and_incomplete_coverage(self):
        memory.remember(self.state, self.observation()); memory.save(self.path, self.state)
        atomic_json(self.path / 'model-report.json', {'status': 'time-budget', 'journey': True, 'coverage': {'journeyComplete': False}})
        comment = {'topic': 'navigation', 'impression': 'mixed', 'observed': 'The map marks a shelter east of me.',
                   'thought': 'I know where I want to go, but expected the map click to start walking.',
                   'expected': 'Selecting the destination would move my party.', 'suggestion': 'Make marking versus travel clearer.', 'confidence': 'medium'}
        experience.validate_comment(comment)
        atomic_json(self.path / 'feedback-00.json', {'number': 1})
        atomic_json(self.path / 'feedback.json', [{**comment, 'kind': 'playtester-impression', 'number': 1, 'step': 5, 'observation': 4,
                                                'evidence': {'snapshot': 'feedback-00.json', 'screenshot': 'feedback-00.png'}}])
        experience.write(self.path)
        text = (self.path / 'playtester-notes.md').read_text()
        self.assertIn(comment['thought'], text)
        self.assertIn('**Observed:**', text)
        self.assertIn('No final reflection', text)
        self.assertIn('journeyComplete', text)
        self.assertTrue(experience.review(self.path)['comments'][0]['evidenceRetained'])
        context = experience.context(self.path, 6)
        self.assertFalse(context['commentDue'])
        self.assertEqual(comment['thought'], context['previousImpressions'][0]['thought'])
        self.assertTrue(experience.context(self.path, 13)['commentDue'])
        atomic_json(self.path / 'feedback-00.json', {'number': 17})
        self.assertFalse(experience.review(self.path)['comments'][0]['evidenceRetained'])

    def test_schema_requires_player_comment_fields_but_does_not_invent_missing_legacy_feedback(self):
        self.assertTrue({'plan', 'comments', 'debrief'} <= set(model.DECISION_SCHEMA['required']))
        model.validate_decision({'reason': 'Legacy stand-in', 'done': True, 'action': None, 'finding': None})
        with self.assertRaises(ValueError):
            model.validate_decision({'reason': '', 'done': True, 'action': None, 'finding': None, 'comments': [{}]})
        self.assertEqual([], experience.review(self.path)['comments'])

    def test_long_valid_expectation_keeps_the_full_impression_without_exceeding_transport_limits(self):
        path = self.path / 'fixture'; path.mkdir()
        atomic_json(path / 'playtest.json', {'schemaVersion': 1, 'id': 'fixture', 'steps': 1})
        atomic_json(path / 'observation.json', {'observation': 1, 'screenshot': None})
        comment = {'topic': 'world', 'impression': 'mixed', 'observed': 'An inhabitant named a distant cistern.',
                   'thought': 'I have a reason to explore, but want to understand the journey.',
                   'expected': 'x' * 1200, 'suggestion': '', 'confidence': 'medium'}
        experience.add(None, 'fixture', [comment], root=self.path)
        result = json.loads((path / 'feedback.json').read_text())[0]
        self.assertEqual(comment['expected'], result['expected'])
        self.assertEqual('playtester-impression', result['kind'])

    def test_player_memory_bounds_repeated_journal_and_ground(self):
        for n in range(90):
            memory.remember(self.state, self.observation(n, journal=[{'text': f'Observed clue {n}'}],
                                                       localGround=[[x, n, True, False, True] for x in range(60)]))
        self.assertEqual(80, len(self.state['journal']))
        self.assertEqual(4096, len(self.state['terrain']))
        memory.save(self.path, self.state)
        self.assertLess((self.path / 'player-memory.json').stat().st_size, 750000)


if __name__ == '__main__':
    unittest.main()
