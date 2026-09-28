from __future__ import annotations
import json, tempfile, unittest
from pathlib import Path
from jomon.catalog import select_content_pack, template_root
from jomon.commands import AttackCommand, CharacterSetupCommand, EquipItemCommand, InteractCommand, MoveCommand, RecoverRemainsItemCommand, SelectSuccessorCommand
from jomon.session import GameSession
from jomon.state import StateError, game_state_from_dict

ROOT=Path(__file__).parent; PACK=ROOT.parents[0]/'jomon'/'content_packs'/'first-playable'
SETUP=CharacterSetupCommand('crew.field-member','ancestry.baseline','origin.maintenance','trait.careful')
def move(s,dx,dy,n=1):
 for _ in range(n):
  o=s.submit(MoveCommand(dx,dy)); assert o.accepted,o.result_id
def save_dict(s):
 with tempfile.TemporaryDirectory() as d:
  p=Path(d)/'s.json';s.save(p);return json.loads(p.read_text())
def make():
 s,o=GameSession.create_configured('continuity',SETUP);assert o.accepted;return s
def kill_at_guard(s):
 if (s.world_view().courier_position.x,s.world_view().courier_position.y)!=(9,3): move(s,1,0,7)
 while s.world_view().courier_alive:
  move(s,-1,0); s.submit(MoveCommand(1,0))
def open_and_take(s):
 s.submit(EquipItemCommand('item.maintenance-tool'))
 move(s,0,-1,2);move(s,1,0,6);assert s.submit(InteractCommand('feature.maintenance-latch')).accepted
 move(s,1,0,8);move(s,0,1,2);assert s.submit(InteractCommand('feature.diagnostic-cache')).accepted
 move(s,0,-1,2);move(s,-1,0,7);move(s,0,1,2)

class ContinuityTests(unittest.TestCase):
 def setUp(self):select_content_pack(PACK)
 def tearDown(self):select_content_pack(template_root())
 def test_roster_is_authoritative_and_setup_only_affects_initial_member(self):
  s=make(); crew=s.crew_views();self.assertEqual([x.id for x in crew],['crew.initial-operative','crew.survivor-one','crew.survivor-two'])
  self.assertEqual(sum(x.active for x in crew),1);self.assertEqual(crew[1].health,9);self.assertEqual(crew[2].health,11)
 def test_natural_death_selection_and_rejections_are_atomic(self):
  s=make();kill_at_guard(s); self.assertFalse(s.world_view().courier_alive); before=save_dict(s)
  self.assertEqual(s.submit(MoveCommand(1,0)).result_id,'courier.dead');self.assertEqual(save_dict(s),before)
  self.assertEqual(s.submit(SelectSuccessorCommand('crew.missing')).result_id,'successor.unknown-member')
  o=s.submit(SelectSuccessorCommand('crew.survivor-one'));self.assertTrue(o.accepted);self.assertFalse(o.time_advanced)
  self.assertEqual((s.world_view().courier_position.x,s.world_view().courier_position.y),(1,2));self.assertEqual(s.actor_view('courier').health,9)
  self.assertEqual(s.submit(SelectSuccessorCommand('crew.survivor-two')).result_id,'successor.current-alive')
 def test_objective_stays_with_body_then_is_recovered_and_delivered(self):
  s=make();open_and_take(s); self.assertEqual(s.operation_views()[0].state_id,'resolved'); kill_at_guard(s)
  self.assertEqual(s.operation_views()[0].state_id,'resolved');self.assertTrue('objective.operation.recover-diagnostic' in s.crew_views()[0].item_ids)
  s.submit(SelectSuccessorCommand('crew.survivor-one'))
  move(s,1,0,8);move(s,0,1)
  bad=s.submit(RecoverRemainsItemCommand('crew.initial-operative','missing'));self.assertFalse(bad.accepted)
  got=s.submit(RecoverRemainsItemCommand('crew.initial-operative','objective.operation.recover-diagnostic'));self.assertTrue(got.accepted)
  self.assertTrue(any(x.kind_id=='item.diagnostic-module' for x in s.inventory_view()))
  move(s,0,-1,2);move(s,-1,0,7);move(s,0,1,2)
  self.assertEqual(s.submit(InteractCommand('feature.shared-base')).result_id,'interaction.operation-delivered')
 def test_save_load_and_multiple_generations_keep_one_item_custodian(self):
  s=make();kill_at_guard(s);s.submit(SelectSuccessorCommand('crew.survivor-one'));move(s,1,0,8);move(s,0,1)
  item='item.maintenance-tool';self.assertTrue(s.submit(RecoverRemainsItemCommand('crew.initial-operative',item)).accepted)
  with tempfile.TemporaryDirectory() as d:
   p=Path(d)/'dead.json';s.save(p);s=GameSession.load(p)
   self.assertEqual(s.world_view().courier_position.x,9); self.assertTrue(any(x.id==item for x in s.inventory_view()))
  state=save_dict(s);state['crew'][1]['health']=1;state['crew'][1]['alive']=False
  with self.assertRaises(StateError): game_state_from_dict(state) # health/alive contradiction deliberately corrupt
 def test_second_successor_recovers_the_same_instance_from_the_second_body(self):
  s=make();kill_at_guard(s);s.submit(SelectSuccessorCommand('crew.survivor-one'));move(s,1,0,8);move(s,0,1)
  item='item.maintenance-tool';self.assertTrue(s.submit(RecoverRemainsItemCommand('crew.initial-operative',item)).accepted)
  kill_at_guard(s);self.assertTrue(s.submit(SelectSuccessorCommand('crew.survivor-two')).accepted)
  move(s,1,0,8);move(s,0,-1)
  self.assertTrue(s.submit(RecoverRemainsItemCommand('crew.survivor-one',item)).accepted)
  self.assertEqual(sum(item in row.item_ids for row in s.crew_views()),1)
 def test_all_dead_has_no_successor_without_reset(self):
  s=make();kill_at_guard(s)
  s._state.crew[1].health=0;s._state.crew[1].alive=False;s._state.crew[2].health=0;s._state.crew[2].alive=False
  before=save_dict(s);self.assertEqual(s.submit(SelectSuccessorCommand('crew.survivor-one')).result_id,'successor.ineligible');self.assertEqual(save_dict(s),before)
