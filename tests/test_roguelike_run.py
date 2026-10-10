from __future__ import annotations

from collections import Counter
import copy
from pathlib import Path
import tempfile
import unittest

from roag.actions import apply_damage, interact
from roag.commands import (
    ChooseRunBranchCommand, ChooseRunRewardCommand, CollectRunItemCommand,
    TerrainActionCommand,
)
from roag.danger import evaluate_danger_step
from roag.production import site_position
from roag.profile import (
    PlayerProfile, ProfileError, load_profile, persist_run_profile,
    profile_from_dict,
)
from roag.regions import region_reachable
from roag.run_items import (
    INITIAL_ITEM_IDS, RUN_ITEMS, after_attack_hit, after_move,
    collect_run_item, effect_value,
)
from roag.run_loot import collect_at
from roag.run_progression import prepare_stage, record_boss_defeat, start_run
from roag.sanctums import shrine_choice
from roag.enemy_equipment import harm_enemy
from roag.save import load_game, save_game
from roag.session import GameSession
from roag.state import (
    SAVE_FORMAT, Position, StateError, Threat, create_world,
    game_state_from_dict,
)
from roag.world import distance


class RoguelikeRunTests(unittest.TestCase):
    def prepared(self, seed: str = "five stage run"):
        state = create_world(seed)
        start_run(state, profile=PlayerProfile())
        return state

    def choose_pending_rewards(self, state: object) -> None:
        session = GameSession(state)
        while state.run.pending_rewards:
            self.assertTrue(session.submit(ChooseRunRewardCommand(0)).changed)

    def test_catalog_has_sixty_stackable_items_and_forty_four_initial_unlocks(self):
        self.assertEqual(len(RUN_ITEMS), 60)
        self.assertEqual(len(INITIAL_ITEM_IDS), 44)
        self.assertEqual(
            Counter(row.tier for row in RUN_ITEMS.values()),
            Counter(common=24, uncommon=18, rare=10, boss=8),
        )
        self.assertEqual(
            {row.region for row in RUN_ITEMS.values() if row.tier == "boss"},
            {"hearthford", "greywash", "greenwold", "whitecairn",
             "dunmire", "marlbank", "rillscar", "frostmere"},
        )

    def test_each_stage_starts_with_bounded_loot_patrol_and_field_worksite(self):
        state = self.prepared("opening cadence")
        landing = state.region.landmarks["landing"]
        [(_drop_id, (_item_id, loot))] = list(state.run.dropped_items.items())
        active = [actor for actor in state.threats if actor.status == "watching"]

        self.assertTrue(6 <= distance(landing, loot) <= 12)
        self.assertTrue(any(10 <= distance(landing, actor.position) <= 18 for actor in active))
        self.assertTrue(18 <= distance(landing, site_position(state)) <= 30)
        self.assertIn(site_position(state), region_reachable(state.region))

    def test_item_collection_is_zero_time_deterministic_and_saveable(self):
        first = self.prepared("run item determinism")
        second = copy.deepcopy(first)
        drop_id, (item_id, point) = next(iter(first.run.dropped_items.items()))
        first.position = second.position = point
        before = first.world_time

        left = GameSession(first).submit(CollectRunItemCommand(drop_id))
        right = GameSession(second).submit(CollectRunItemCommand(drop_id))

        self.assertEqual(left, right)
        self.assertTrue(left.changed)
        self.assertFalse(left.time_advanced)
        self.assertEqual(first.world_time, before)
        self.assertEqual(first.run.item_stacks[item_id], 1)
        self.assertEqual(first.to_dict(), second.to_dict())
        with tempfile.TemporaryDirectory() as directory:
            restored = load_game(save_game(first, Path(directory) / "run.json"))
        self.assertEqual(restored.run, first.run)

    def test_five_bosses_and_physical_branches_finish_one_run(self):
        state = self.prepared("branching completion")
        session = GameSession(state)
        path = ["hearthford"]
        for stage in range(1, 6):
            result = record_boss_defeat(
                state, f"sanctum:{state.active_region_id}:boss",
            )
            self.assertTrue(result.changed)
            if stage == 5:
                break
            self.choose_pending_rewards(state)
            self.assertIsNotNone(state.run.threshold_level)
            region_id, point = sorted(state.run.branch_positions.items())[0]
            state.position = point
            outcome = session.submit(ChooseRunBranchCommand(region_id))
            self.assertTrue(outcome.changed)
            path.append(region_id)
            self.assertEqual(state.run.stage_index, stage + 1)

        self.assertEqual(state.run.status, "victory")
        self.assertTrue(state.world_ended)
        self.assertEqual(len(state.run.boss_kills), 5)
        self.assertEqual(state.run.region_path, path)
        self.assertEqual(sum(
            count for item_id, count in state.run.item_stacks.items()
            if RUN_ITEMS[item_id].tier == "boss"
        ), 5)

    def test_sanctum_activation_owned_boss_kills_and_reachable_five_stage_route(self):
        # Position jumps and low boss health keep this a route/ownership check,
        # not a claim of five complete player-controlled combat encounters.
        state = self.prepared("activated claimant route")
        for stage in range(1, 6):
            reachable = region_reachable(state.region)
            shrine = state.region.landmarks["sanctum_shrine"]
            entry = state.region.landmarks["sanctum_entry"]
            self.assertIn(shrine, reachable)
            self.assertIn(entry, reachable)
            state.position = shrine
            self.assertEqual(interact(state).overlay, "sanctum")
            self.assertTrue(shrine_choice(state, "b")[0])
            state.position = entry
            self.assertTrue(interact(state).time_advanced)
            boss = next(actor for actor in state.threats
                        if actor.id == f"sanctum:{state.active_region_id}:boss")
            boss.health = 1
            harm_enemy(state, boss, 2, "accepted final hit",
                       defeated_by_actor_id=state.active_courier_id)
            self.assertEqual(boss.status, "defeated")
            self.assertEqual(len(state.run.boss_kills), stage)
            if stage == 5:
                break
            self.choose_pending_rewards(state)
            region_id, point = sorted(state.run.branch_positions.items())[0]
            self.assertIn(point, region_reachable(state.region))
            state.position = point
            self.assertTrue(GameSession(state).submit(
                ChooseRunBranchCommand(region_id),
            ).changed)
        self.assertEqual(state.run.status, "victory")
        self.assertTrue(state.world_ended)
        self.assertFalse(state.run.pending_rewards)

    def test_pressure_emits_patrol_sized_fair_reinforcement_group(self):
        state = self.prepared("group pressure")
        state.noise = 20
        for actor in state.threats:
            actor.status = "defeated"
        actions = evaluate_danger_step(state)
        arrivals = [row for row in actions if row.action_id == "danger.spawn_reinforcement"]
        self.assertEqual(len(arrivals), 2)
        self.assertEqual(len({row.target_id for row in arrivals}), 2)
        self.assertEqual(len({row.position for row in arrivals}), 2)

    def test_destroying_required_threshold_causes_immediate_causal_loss(self):
        state = self.prepared("critical destruction")
        record_boss_defeat(state, "sanctum:hearthford:boss")
        self.choose_pending_rewards(state)
        state.threats.clear()
        state.region_threats[state.active_region_id] = state.threats
        target = state.run.threshold_entry
        state.position = Position(target.x - 1, target.y, target.z)
        collect_run_item(state, "hearthford-crown-tooth")
        session = GameSession(state)
        outcome = session.submit(TerrainActionCommand("break", target))
        self.assertEqual(outcome.result_id, "terrain.destroyed")
        self.assertEqual(state.run.status, "defeat")
        self.assertTrue(state.world_ended)
        self.assertIn("required route", state.run.failure_reason)

    def test_zero_health_ends_run_without_vessel_succession(self):
        state = self.prepared("run death")
        courier_id = state.active_courier_id
        state.courier.health = 1
        apply_damage(state, 99, "test impact")
        self.assertEqual(state.run.status, "defeat")
        self.assertEqual(state.active_courier_id, courier_id)
        self.assertEqual(state.location, "region")
        self.assertTrue(state.world_ended)

    def test_format_sixteen_only_and_settled_profile_is_idempotent(self):
        state = self.prepared("new save line")
        old = state.to_dict()
        old["save_format"] = 15
        with self.assertRaisesRegex(StateError, "expected 16"):
            game_state_from_dict(old)

        state.run.status = "victory"
        state.run.boss_kills = ["hearthford"]
        state.run.item_stacks = {"river-edge": 1}
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "profile.json"
            persist_run_profile(state, path)
            persist_run_profile(state, path)
            profile = load_profile(path)
        self.assertEqual(SAVE_FORMAT, 16)
        self.assertEqual(len(profile.run_records), 1)
        self.assertIn("river-edge", profile.discoveries)

    def test_profile_unlocks_enter_future_run_pool_and_unknown_ids_are_rejected(self):
        locked = sorted(set(RUN_ITEMS) - INITIAL_ITEM_IDS)[0]
        profile = PlayerProfile(unlocked_items={locked})
        state = create_world("horizontal item pool")
        start_run(state, profile=profile)
        self.assertEqual(
            set(state.run.allowed_item_ids), INITIAL_ITEM_IDS | {locked},
        )

        invalid = profile.to_dict()
        invalid["unlocked_items"] = ["not-a-run-item"]
        with self.assertRaises(ProfileError):
            profile_from_dict(invalid)

    def test_stage_transition_discards_unclaimed_prior_region_drops(self):
        state = self.prepared("stage drop ownership")
        first_drop_ids = set(state.run.dropped_items)
        record_boss_defeat(state, "sanctum:hearthford:boss")
        self.choose_pending_rewards(state)
        region_id, point = sorted(state.run.branch_positions.items())[0]
        state.position = point
        self.assertTrue(GameSession(state).submit(
            ChooseRunBranchCommand(region_id),
        ).changed)
        self.assertTrue(set(state.run.dropped_items).isdisjoint(first_drop_ids))
        self.assertEqual(len(state.run.dropped_items), 1)

    def test_trigger_scoping_chain_retaliation_discount_and_free_step_are_live(self):
        state = self.prepared("run item effect coverage")
        state.threats.clear()
        target = Threat(
            "target", "target", "melee", Position(41, 25), 9, 9,
            status="engaged",
        )
        second = Threat(
            "second", "second", "melee", Position(42, 25), 9, 9,
            status="engaged",
        )
        state.threats.extend((target, second))
        state.region_threats[state.active_region_id] = state.threats
        state.position = Position(40, 25)
        for item_id in (
            "forked-current", "barbed-account", "powder-echo",
            "walking-quarry", "crosswind-step",
        ):
            collect_run_item(state, item_id)

        chained = after_attack_hit(state, target)
        self.assertEqual(chained[:2], ("second", 1))
        before = target.health
        state.courier.health = state.courier.max_health
        apply_damage(state, 2, "test attacker", attacker_id=target.id)
        self.assertEqual(target.health, before - 1)
        self.assertEqual(
            effect_value(state, "blast_damage", trigger="actor.defeated"), 1,
        )
        self.assertEqual(
            effect_value(state, "blast_damage", trigger="terrain.destroyed"), 3,
        )

        state.pressure_elapsed = 0
        state.run.move_chain = 0
        for _ in range(6):
            after_move(state, wet=False)
            state.pressure_elapsed += 1
        self.assertEqual(state.pressure_elapsed, 5)

        for _ in range(4):
            collect_run_item(state, "cheap-key")
        state.run.challenge_tier = 5
        state.run.stage_salvage = 0
        state.run.dropped_items = {
            "paid-cache": ("river-edge", state.position),
        }
        self.assertTrue(collect_at(state, "paid-cache").changed)

    def test_revealed_stage_lead_does_not_change_stage_authority(self):
        state = self.prepared("mapped stage lead")
        collect_run_item(state, "backwater-map")
        before = state.to_dict()
        prepare_stage(state)
        self.assertTrue(any(
            "likely thresholds" in row for row in state.messages
        ))
        self.assertEqual(state.run.stage_index, before["run"]["stage_index"])


if __name__ == "__main__":
    unittest.main()
