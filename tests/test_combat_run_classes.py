from __future__ import annotations

import unittest

from roag.actions import attack, use_run_ability
from roag.commands import ChooseRunRewardCommand, MoveCommand, UseRunAbilityCommand
from roag.enemy_ai import select_goal
from roag.enemy_equipment import harm_enemy
from roag.run_classes import RUN_CLASSES
from roag.run_items import collect_run_item
from roag.run_progression import record_world_step, start_run
from roag.run_rewards import award_experience, eligible_boons
from roag.session import GameSession
from roag.state import Position, Threat, create_world, game_state_from_dict
from roag.terrain import terrain_at


class CombatRunClassTests(unittest.TestCase):
    def prepared(self, class_id: str):
        state = create_world(f"class contract {class_id}")
        start_run(state, class_id=class_id)
        actor = state.threats[0]
        actor.status = "watching"
        actor.health = actor.max_health = 30
        actor.position = Position(43, 25)
        actor.home_position = actor.position
        state.threats = [actor]
        state.region_threats[state.active_region_id] = state.threats
        state.position = Position(40, 25)
        for x in range(36, 52):
            state.region.tile_changes[f"{x},25,0"] = "."
        return state, actor

    def test_every_class_starts_with_a_fixed_complete_kit(self):
        for class_id, definition in RUN_CLASSES.items():
            state, _ = self.prepared(class_id)
            self.assertEqual(state.run.class_id, class_id)
            self.assertEqual((state.weapon, state.gear), (definition.weapon, definition.secondary))
            self.assertEqual(state.run.movement_cooldown, 0)
            self.assertEqual(state.run.signature_cooldown, 0)
            self.assertGreater(state.courier.health, 0)

    def test_each_class_exposes_attack_movement_and_signature_actions(self):
        breaker, target = self.prepared("breaker")
        breaker.position, target.position = Position(40, 25), Position(41, 25)
        self.assertTrue(attack(breaker, target.id).changed)
        breaker, target = self.prepared("breaker")
        self.assertTrue(use_run_ability(breaker, "movement", target.id).changed)
        breaker, _ = self.prepared("breaker")
        breaker.region.tile_changes["41,25,0"] = ";"
        self.assertTrue(use_run_ability(breaker, "signature").changed)

        marksman, target = self.prepared("marksman")
        self.assertTrue(attack(marksman, target.id).changed)
        marksman, _ = self.prepared("marksman")
        marksman.region.tile_changes["41,25,0"] = "#"
        self.assertTrue(use_run_ability(marksman, "movement", target_position=Position(42, 25)).changed)
        marksman, target = self.prepared("marksman")
        self.assertTrue(use_run_ability(marksman, "signature", target.id).changed)

        trickster, target = self.prepared("trickster")
        trickster.position, target.position = Position(40, 25), Position(41, 25)
        self.assertTrue(attack(trickster, target.id).changed)
        trickster, target = self.prepared("trickster")
        self.assertTrue(use_run_ability(trickster, "movement", target.id).changed)
        trickster, _ = self.prepared("trickster")
        self.assertTrue(use_run_ability(trickster, "signature", target_position=Position(41, 25)).changed)
        self.assertIsNotNone(trickster.run.decoy_position)

        sapper, target = self.prepared("sapper")
        self.assertTrue(attack(sapper, target.id).changed)
        sapper, _ = self.prepared("sapper")
        self.assertTrue(use_run_ability(sapper, "movement", target_position=Position(42, 25)).changed)
        sapper, target = self.prepared("sapper")
        placed = use_run_ability(sapper, "signature", target_position=Position(42, 25))
        self.assertTrue(placed.changed)
        self.assertTrue(sapper.run.placed_charges)

    def test_class_actions_commit_one_world_step_and_cool_down(self):
        state, target = self.prepared("breaker")
        before = state.world_time
        outcome = GameSession(state).submit(UseRunAbilityCommand("movement", target.id))
        self.assertTrue(outcome.time_advanced)
        self.assertEqual(state.world_time, before + 1)
        self.assertEqual(state.run.movement_cooldown, RUN_CLASSES["breaker"].movement_cooldown)
        self.assertIn("actor.moved", [event.event_id for event in outcome.events])
        self.assertFalse(GameSession(state).submit(UseRunAbilityCommand("movement", target.id)).changed)

    def test_xp_choices_are_zero_time_queued_and_stack_visible_boons(self):
        state, _ = self.prepared("breaker")
        self.assertGreaterEqual(len(eligible_boons(state)), 20)
        self.assertTrue(all(
            RUN_CLASSES[class_id] and eligible_boons(self.prepared(class_id)[0]) == eligible_boons(state)
            for class_id in RUN_CLASSES
        ))
        self.assertEqual(award_experience(state, 30, "test"), 4)
        self.assertEqual(len(state.run.pending_rewards), 4)
        self.assertTrue(all(len(row["choices"]) == 3 for row in state.run.pending_rewards))
        restored = game_state_from_dict(state.to_dict())
        self.assertEqual(restored.run.pending_rewards, state.run.pending_rewards)
        before = state.world_time
        session = GameSession(state)
        while state.run.pending_rewards:
            outcome = session.submit(ChooseRunRewardCommand(0))
            self.assertTrue(outcome.changed)
            self.assertFalse(outcome.time_advanced)
        self.assertEqual(state.world_time, before)
        item_id = next(iter(state.run.item_stacks))
        first = state.run.item_stacks[item_id]
        collect_run_item(state, item_id)
        self.assertEqual(state.run.item_stacks[item_id], first + 1)

    def test_pending_choice_blocks_authoritative_actions_until_selected(self):
        state, _ = self.prepared("breaker")
        award_experience(state, 3, "test")
        before = state.world_time
        outcome = GameSession(state).submit(MoveCommand(1, 0))
        self.assertFalse(outcome.changed)
        self.assertEqual(outcome.result_id, "run.reward.pending")
        self.assertEqual(state.world_time, before)
        self.assertTrue(GameSession(state).submit(ChooseRunRewardCommand(0)).changed)

    def test_a_retry_discards_prior_run_boons_and_class_cooldowns(self):
        state, target = self.prepared("trickster")
        collect_run_item(state, "river-edge")
        use_run_ability(state, "movement", target.id)
        start_run(state, class_id="sapper")
        self.assertEqual(state.run.class_id, "sapper")
        self.assertFalse(state.run.item_stacks)
        self.assertEqual((state.run.movement_cooldown, state.run.signature_cooldown), (0, 0))

    def test_marksman_vault_requires_an_obstruction_and_pierces_a_lane(self):
        state, target = self.prepared("marksman")
        self.assertFalse(use_run_ability(
            state, "movement", target_position=Position(42, 25),
        ).changed)
        state.region.tile_changes["41,25,0"] = "#"
        self.assertTrue(use_run_ability(
            state, "movement", target_position=Position(42, 25),
        ).changed)

        state, target = self.prepared("marksman")
        target.position = Position(43, 25)
        second = Threat(
            "behind", "behind", "melee", Position(45, 25), 8, 8,
            status="watching",
        )
        state.threats.append(second)
        state.region_threats[state.active_region_id] = state.threats
        self.assertTrue(use_run_ability(state, "signature", target.id).changed)
        self.assertLess(target.health, target.max_health)
        self.assertLess(second.health, second.max_health)

    def test_breaker_slam_mutates_soft_cover_and_marks_terrain_discovery(self):
        state, _ = self.prepared("breaker")
        point = Position(41, 25)
        state.region.tile_changes["41,25,0"] = ";"
        self.assertTrue(use_run_ability(state, "signature").changed)
        self.assertEqual(terrain_at(state.region, point).glyph, ".")
        self.assertIn("terrain", state.run.stage_reward_sources)
        self.assertTrue(any(drop_id.startswith("terrain-") for drop_id in state.run.dropped_items))

    def test_trickster_decoy_is_a_visible_ai_goal(self):
        state, actor = self.prepared("trickster")
        actor.position = Position(43, 25)
        self.assertTrue(use_run_ability(
            state, "signature", target_position=Position(41, 25),
        ).changed)
        decision = select_goal(state, actor)
        self.assertEqual(decision.goal, "investigate decoy")
        self.assertEqual(decision.target, state.run.decoy_position)

    def test_sapper_charge_credits_a_delayed_player_kill_once(self):
        state, actor = self.prepared("sapper")
        charge = Position(42, 25)
        self.assertTrue(use_run_ability(
            state, "signature", target_position=charge,
        ).changed)
        actor.position = charge
        actor.health = actor.max_health = 2
        actor.status = "watching"
        while state.world_time < 3:
            state.world_time += 1
            record_world_step(state)
        self.assertEqual(actor.status, "defeated")
        self.assertEqual(state.run.ordinary_kills, 1)
        self.assertEqual(state.run.experience, 1)
        self.assertFalse(state.run.placed_charges)

    def test_elite_and_secondary_kills_keep_one_owner_and_bounded_effects(self):
        state, elite = self.prepared("breaker")
        elite.elite = True
        elite.health = 1
        harm_enemy(
            state, elite, 3, "test elite", defeated_by_actor_id=state.active_courier_id,
        )
        self.assertEqual(state.run.elite_kills, 1)
        self.assertIn("elite", state.run.stage_reward_sources)
        self.assertEqual(state.run.pending_rewards[0]["source"], "elite")

        state, first = self.prepared("breaker")
        second = Threat(
            "secondary", "secondary", "melee", Position(44, 25), 1, 1,
            status="watching",
        )
        state.threats.append(second)
        state.region_threats[state.active_region_id] = state.threats
        collect_run_item(state, "powder-echo")
        first.health = 1
        harm_enemy(
            state, first, 3, "test primary", defeated_by_actor_id=state.active_courier_id,
        )
        self.assertEqual(state.run.ordinary_kills, 2)
        self.assertEqual(state.run.experience, 2)

    def test_capped_boons_are_not_offered_or_collected_as_noops(self):
        state, _ = self.prepared("breaker")
        state.run.item_stacks["river-edge"] = 5
        self.assertNotIn("river-edge", eligible_boons(state, "common"))
        with self.assertRaisesRegex(ValueError, "maximum effect"):
            collect_run_item(state, "river-edge")


if __name__ == "__main__":
    unittest.main()
