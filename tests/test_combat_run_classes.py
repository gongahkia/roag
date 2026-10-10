from __future__ import annotations

import unittest
from unittest.mock import patch

from roag.actions import (
    apply_damage, attack, run_ability_targets, terrain_action, use_run_ability,
)
from roag.commands import ChooseRunRewardCommand, MoveCommand, UseRunAbilityCommand
from roag.enemy_ai import select_goal
from roag.enemy_equipment import harm_enemy
from roag.run_classes import RUN_CLASSES
from roag.regions import region_reachable
from roag.run_items import (
    RUN_ITEMS, after_move, collect_run_item, effect_value, stack_value,
)
from roag.run_progression import record_world_step, start_run
from roag.run_rewards import (
    award_experience, boon_effect_summary, eligible_boons, reward_lines,
)
from roag.session import GameSession
from roag.state import Position, Threat, create_world, game_state_from_dict
from roag.terrain import terrain_at
from roag.world import is_walkable, line_of_sight
from roag.terminal import (
    InputEvent, OverlayView, TargetView, _handle_overlay_view,
    _handle_targeting, targeting_lines,
)


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
        harm_enemy(state, actor, 3, "late repeated hit",
                   defeated_by_actor_id=state.active_courier_id)
        self.assertEqual((state.run.ordinary_kills, state.run.experience), (1, 1))

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

    def test_threshold_stack_improves_on_second_copy_and_queued_capped_offer_refreshes(self):
        state, _ = self.prepared("breaker")
        definition = RUN_ITEMS["crosswind-step"]
        self.assertLess(stack_value(definition, 1), stack_value(definition, 2))
        collect_run_item(state, definition.id)
        collect_run_item(state, definition.id)
        self.assertEqual(effect_value(state, "free_step"), 2)
        self.assertIn("next copy: one exposure refund every 4 moves",
                      boon_effect_summary(definition.id, 2))

        state.run.item_stacks["river-edge"] = 4
        state.run.pending_rewards = [
            {"source": "combat", "level": level,
             "choices": ["river-edge", "crosswind-step", "powder-echo"]}
            for level in (2, 3)
        ]
        session = GameSession(state)
        before = state.world_time
        self.assertTrue(session.submit(ChooseRunRewardCommand(0)).changed)
        self.assertNotIn("river-edge", state.run.pending_rewards[0]["choices"])
        self.assertNotIn("river-edge", " ".join(reward_lines(state)))
        self.assertTrue(session.submit(ChooseRunRewardCommand(0)).changed)
        self.assertEqual(state.world_time, before)
        self.assertEqual(len(state.run.pending_rewards), 0)

    def test_class_target_views_show_ability_legality_and_tab_uses_ability_range(self):
        for class_id in RUN_CLASSES:
            state, actor = self.prepared(class_id)
            if class_id == "marksman":
                state.region.tile_changes["41,25,0"] = "#"
                position = Position(42, 25)
            elif class_id == "sapper":
                position = Position(42, 25)
            elif class_id == "trickster":
                actor.position = Position(43, 25)
                position = actor.position
            else:
                position = actor.position
            view = TargetView.begin_run_ability(state, "movement")
            view.cursor = position
            lines = targeting_lines(state, view, 78)
            self.assertTrue(any("LEGAL" in line for line in lines), (class_id, lines))
            self.assertTrue(any("Enter commits one class action" in line for line in lines))
            before = state.world_time
            self.assertEqual(_handle_targeting(state, view, 27), (True, False))
            self.assertEqual(state.world_time, before)
            view = TargetView.begin_run_ability(state, "movement")
            if class_id in {"breaker", "trickster"}:
                self.assertEqual(_handle_targeting(state, view, 9), (False, False))
                self.assertEqual(view.cursor, actor.position)
            else:
                view.cursor = position
            self.assertEqual(_handle_targeting(state, view, 10), (True, True))
            self.assertEqual(state.world_time, before + 1)

    def test_piercing_target_picker_rejects_diagonal_and_wall_blocks_later_hit(self):
        state, actor = self.prepared("marksman")
        actor.position = Position(43, 24)
        self.assertNotIn(actor, run_ability_targets(state, "signature"))
        view = TargetView.begin_run_ability(state, "signature")
        view.cursor = actor.position
        self.assertTrue(any("BLOCKED" in line for line in targeting_lines(state, view, 78)))
        self.assertEqual(_handle_targeting(state, view, 10), (False, False))
        self.assertEqual(state.world_time, 0)

        actor.position = Position(43, 25)
        behind = Threat("behind-wall", "behind wall", "melee", Position(45, 25), 8, 8,
                        status="watching")
        state.threats.append(behind)
        state.region_threats[state.active_region_id] = state.threats
        state.region.tile_changes["44,25,0"] = "#"
        self.assertTrue(use_run_ability(state, "signature", actor.id).changed)
        self.assertLess(actor.health, actor.max_health)
        self.assertEqual(behind.health, behind.max_health)

    def test_reward_overlay_flushes_repeated_input_after_a_selection(self):
        state, _ = self.prepared("breaker")
        award_experience(state, 8, "combat")
        self.assertEqual(len(state.run.pending_rewards), 2)
        before = state.world_time
        with patch("roag.terminal.curses.flushinp") as flush:
            closed, quit_requested = _handle_overlay_view(
                state, OverlayView("run-reward"), InputEvent("key", key=ord("1")),
                session=GameSession(state),
            )
        self.assertFalse(closed)
        self.assertFalse(quit_requested)
        flush.assert_called_once()
        self.assertEqual(state.world_time, before)
        self.assertEqual(len(state.run.pending_rewards), 1)

    def test_terrain_breach_opens_collision_and_sight_without_erasing_objective(self):
        state, _ = self.prepared("marksman")
        point = Position(41, 25)
        beyond = Position(42, 25)
        state.region.tile_changes["41,25,0"] = "T"
        self.assertFalse(is_walkable(state, point))
        self.assertFalse(line_of_sight(state, state.position, beyond))
        self.assertNotIn(point, region_reachable(state.region, state.position))
        before = state.world_time
        for _ in range(3):
            self.assertTrue(terrain_action(state, "cut", point).time_advanced)
        self.assertEqual(state.world_time, before + 3)
        self.assertTrue(is_walkable(state, point))
        self.assertTrue(line_of_sight(state, state.position, beyond))
        self.assertIn(point, region_reachable(state.region, state.position))
        self.assertIn("sanctum_entry", state.region.landmarks)

    def test_repeated_terrain_boon_enables_a_bounded_kill_blast_combination(self):
        def scenario(copies):
            state, first = self.prepared("breaker")
            first.position = Position(42, 25)
            first.health = first.max_health = 2
            second = Threat("echo-victim", "echo victim", "melee",
                            Position(44, 25), 1, 1, status="watching")
            state.threats.append(second)
            state.region_threats[state.active_region_id] = state.threats
            for y in (24, 25, 26):
                for x in (39, 40, 41):
                    state.region.tile_changes[f"{x},{y},0"] = "."
            state.region.tile_changes["41,25,0"] = ";"
            for _ in range(copies):
                collect_run_item(state, "quarry-song")
            collect_run_item(state, "powder-echo")
            self.assertTrue(use_run_ability(state, "signature").changed)
            return state, first, second

        one, first, second = scenario(1)
        self.assertEqual((first.health, second.health), (1, 1))
        stacked, first, second = scenario(2)
        self.assertEqual((first.health, second.health), (0, 0))
        self.assertEqual(stacked.run.ordinary_kills, 2)
        self.assertEqual(stacked.run.experience, 2)
        self.assertTrue(any("Terrain boon shockwave" in row for row in stacked.messages))
        self.assertTrue(any("Defeat boon burst" in row for row in stacked.messages))
        self.assertEqual(one.world_time, stacked.world_time)

    def test_shared_reward_pool_has_no_unspendable_or_dormant_default_copy(self):
        for class_id in RUN_CLASSES:
            state, _ = self.prepared(class_id)
            offered = eligible_boons(state)
            self.assertNotIn("reclaiming-mark", offered)
            self.assertNotIn("cheap-key", offered)
            self.assertIn("greed-lure", offered)
            self.assertTrue(all(
                stack_value(RUN_ITEMS[item_id], 1) > 0
                for item_id in offered
            ))
        state, _ = self.prepared("breaker")
        state.run.challenge_tier = 2
        self.assertIn("cheap-key", eligible_boons(state))
        collect_run_item(state, "cheap-key")
        self.assertNotIn("cheap-key", eligible_boons(state))
        state.run.challenge_tier = 3
        self.assertIn("cheap-key", eligible_boons(state))

        collect_run_item(state, "backwater-map")
        collect_run_item(state, "backwater-map")
        self.assertNotIn("backwater-map", eligible_boons(state))
        self.assertIn("effect's limit", boon_effect_summary("backwater-map", 2))
        collect_run_item(state, "greed-lure")
        collect_run_item(state, "greed-lure")
        collect_run_item(state, "greed-lure")
        self.assertNotIn("greed-lure", eligible_boons(state))
        self.assertIn("ordinary drop every 2 kills", boon_effect_summary("greed-lure", 3))

    def test_wet_boss_reward_gains_a_larger_barrier_when_stacked(self):
        state, _ = self.prepared("sapper")
        collect_run_item(state, "dunmire-flood-hook")
        after_move(state, wet=True)
        self.assertTrue(state.guarded_step)
        self.assertEqual(state.run.barrier, 2)
        collect_run_item(state, "dunmire-flood-hook")
        after_move(state, wet=True)
        self.assertEqual(state.run.barrier, 4)

    def test_terminal_attack_and_signature_confirmation_cost_one_turn_per_class(self):
        for class_id in RUN_CLASSES:
            state, actor = self.prepared(class_id)
            if class_id in {"breaker", "trickster"}:
                actor.position = Position(41, 25)
            attack_view = TargetView.begin(state)
            self.assertEqual(attack_view.cursor, actor.position)
            self.assertEqual(_handle_targeting(state, attack_view, 9), (False, False))
            self.assertEqual(state.world_time, 0)
            self.assertEqual(_handle_targeting(state, attack_view, 10), (True, True))
            self.assertEqual(state.world_time, 1)

            state, actor = self.prepared(class_id)
            if class_id == "breaker":
                state.region.tile_changes["41,25,0"] = ";"
                outcome = GameSession(state).submit(UseRunAbilityCommand("signature"))
                self.assertTrue(outcome.time_advanced)
            else:
                view = TargetView.begin_run_ability(state, "signature")
                if class_id == "marksman":
                    view.cursor = actor.position
                elif class_id == "trickster":
                    view.cursor = Position(41, 25)
                else:
                    view.cursor = Position(42, 25)
                self.assertEqual(_handle_targeting(state, view, 10), (True, True))
            self.assertEqual(state.world_time, 1)

    def test_two_due_charges_queue_all_thresholds_before_another_action(self):
        state, first = self.prepared("sapper")
        positions = (
            Position(41, 25), Position(42, 24), Position(43, 25), Position(42, 26),
            Position(47, 25), Position(48, 24), Position(49, 25), Position(48, 26),
        )
        first.position = positions[0]
        first.health = first.max_health = 1
        for index, point in enumerate(positions[1:], 1):
            state.threats.append(Threat(
                f"charge-target-{index}", "charge target", "melee",
                point, 1, 1, status="watching",
            ))
        state.region_threats[state.active_region_id] = state.threats
        # Prepared simultaneous devices exercise one world-step resolution.
        state.run.placed_charges = [
            (Position(42, 25), 3), (Position(48, 25), 3),
        ]
        state.world_time = 3
        record_world_step(state)
        self.assertEqual(state.run.ordinary_kills, 8)
        self.assertEqual(len(state.run.pending_rewards), 2)
        self.assertEqual(state.run.run_actions, 1)
        self.assertFalse(state.run.placed_charges)
        blocked = GameSession(state).submit(MoveCommand(1, 0))
        self.assertFalse(blocked.time_advanced)
        while state.run.pending_rewards:
            self.assertFalse(GameSession(state).submit(
                ChooseRunRewardCommand(0),
            ).time_advanced)
        self.assertEqual(state.world_time, 3)

    def test_death_to_new_world_clears_temporary_effects_and_keeps_class_choice(self):
        old, _ = self.prepared("sapper")
        collect_run_item(old, "river-edge")
        award_experience(old, 3, "combat")
        old.run.placed_charges.append((Position(42, 25), 5))
        old.run.decoy_position = Position(41, 25)
        old.run.signature_cooldown = 3
        old.region.tile_changes["41,25,0"] = ";"
        old.courier.health = 1
        apply_damage(old, 99, "acceptance defeat")
        self.assertEqual(old.run.status, "defeat")

        fresh = create_world("different fresh retry")
        start_run(fresh, class_id=old.run.class_id)
        self.assertEqual(fresh.run.class_id, "sapper")
        self.assertNotEqual(fresh.seed, old.seed)
        self.assertFalse(fresh.run.item_stacks)
        self.assertFalse(fresh.run.pending_rewards)
        self.assertFalse(fresh.run.placed_charges)
        self.assertIsNone(fresh.run.decoy_position)
        self.assertEqual((fresh.run.movement_cooldown, fresh.run.signature_cooldown), (0, 0))
        self.assertNotEqual(fresh.region.tile_changes, old.region.tile_changes)


if __name__ == "__main__":
    unittest.main()
