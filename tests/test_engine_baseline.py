from __future__ import annotations
import json, os, tempfile, unittest
from pathlib import Path
from jomon.catalog import ContentError, load_content_pack, mechanical_fingerprint, select_content_pack
from jomon.commands import ActivityCommand, AttackCommand, CharacterSetupCommand, CraftCommand, EquipItemCommand, MoveCommand, TravelCommand
from jomon.session import GameSession
from jomon.state import ContentUnavailable, StateError
ROOT=Path(__file__).parent
SYNTHETIC=ROOT/'fixtures'/'synthetic_content_pack'
TEMPLATE=Path(__file__).parents[1]/'jomon'/'content_packs'/'template'
class BaselineTests(unittest.TestCase):
 def tearDown(self):select_content_pack(TEMPLATE)
 def test_template_is_strict_and_non_playable(self):
  pack=select_content_pack(TEMPLATE);self.assertFalse(pack.playable)
  with self.assertRaises(ContentUnavailable):GameSession.create('no-content')
 def test_synthetic_pack_exercises_generic_systems_and_save(self):
  select_content_pack(SYNTHETIC);session,out=GameSession.create_configured('seed',CharacterSetupCommand('test-courier','test-ancestry','test-origin','test-trait'))
  self.assertTrue(out.changed);self.assertEqual(session.submit(MoveCommand(1,0)).result_id,'move.ok')
  hostile=next(row for row in session.actor_views() if row.id=='test-actor-hostile');self.assertEqual(session.submit(AttackCommand(hostile.id)).result_id,'attack.resolved')
  self.assertEqual(session.submit(EquipItemCommand('test-item-tool')).result_id,'item.equipped')
  self.assertEqual(session.submit(TravelCommand('test-route')).result_id,'travel.resolved')
  self.assertEqual(session.submit(CraftCommand('test-recipe')).result_id,'craft.resolved')
  self.assertEqual(session.submit(ActivityCommand('production','test-production')).result_id,'activity.resolved')
  self.assertTrue(session.quest_views())
  with tempfile.TemporaryDirectory() as d:
   path=Path(d)/'save.json';session.save(path);self.assertEqual(GameSession.load(path).world_view(),session.world_view())
 def test_lore_and_connections_do_not_change_mechanical_fingerprint(self):
  original=json.loads((SYNTHETIC/'lore.json').read_text());connections=json.loads((SYNTHETIC/'connections.json').read_text())
  with tempfile.TemporaryDirectory() as d:
   root=Path(d);[ (root/name).write_text((SYNTHETIC/name).read_text()) for name in ('manifest.json','systems.json','assets.json') ]
   original['entries']['lore.test-topic']['body']='Entirely different authored wording.';connections['connections'][0]['relation']='remembers'
   (root/'lore.json').write_text(json.dumps(original));(root/'connections.json').write_text(json.dumps(connections))
   self.assertEqual(mechanical_fingerprint(load_content_pack(SYNTHETIC)),mechanical_fingerprint(load_content_pack(root)))
 def test_connections_require_stable_declared_endpoints(self):
  with tempfile.TemporaryDirectory() as d:
   root=Path(d);[ (root/name).write_text((SYNTHETIC/name).read_text()) for name in ('manifest.json','systems.json','lore.json','assets.json') ]
   (root/'connections.json').write_text('{"connections":[{"id":"connection.bad","from":"missing","relation":"knows","to":"lore.test-topic","tags":[]}]}')
   with self.assertRaises(ContentError):load_content_pack(root)
 def test_pre_reset_saves_reject_clearly(self):
  select_content_pack(SYNTHETIC)
  with self.assertRaises(StateError):__import__('jomon.state',fromlist=['game_state_from_dict']).game_state_from_dict({'format':15})
 def test_same_pack_seed_setup_and_commands_are_deterministic(self):
  select_content_pack(SYNTHETIC)
  command=CharacterSetupCommand('test-courier','test-ancestry','test-origin','test-trait')
  first,_=GameSession.create_configured('repeatable',command);second,_=GameSession.create_configured('repeatable',command)
  for session in (first,second):
   session.submit(MoveCommand(1,0));session.submit(AttackCommand('test-actor-hostile'))
  self.assertEqual(first._state.to_dict(),second._state.to_dict())
