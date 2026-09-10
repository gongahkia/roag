from __future__ import annotations

from datetime import date
import unittest

from dumbest_dungeon.challenges import (
    CONTRACTS, ChallengeError, ExpeditionConfig, daily_config, daily_seed,
    decode_code, encode_code, validate_config,
)
from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine, RuleError
from dumbest_dungeon.pressure import PressureSource


class ChallengeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def test_twenty_contracts_teach_distinct_runtime_modifiers(self) -> None:
        self.assertEqual(20, len(CONTRACTS))
        self.assertEqual(len(CONTRACTS), len({item.id for item in CONTRACTS}))
        self.assertEqual(len(CONTRACTS), len({item.modifier for item in CONTRACTS}))
        self.assertTrue(all(item.id.startswith("base:") for item in CONTRACTS))
        self.assertTrue(all(len(item.rule) <= 80 and item.lesson for item in CONTRACTS))

    def test_daily_seed_is_stable_and_date_sensitive(self) -> None:
        self.assertEqual(8404185418283187962, daily_seed(date(2026, 9, 10)))
        self.assertNotEqual(daily_seed(date(2026, 9, 10)), daily_seed(date(2026, 9, 11)))
        self.assertEqual(daily_config(date(2026, 9, 10)), daily_config(date(2026, 9, 10)))

    def test_code_round_trips_full_custom_configuration(self) -> None:
        party = tuple(list(self.catalog.heroes)[:4])
        loadout = next(item for item in self.catalog.loadouts.values() if item["hero"] == party[0])
        config = ExpeditionConfig(
            seed=2**63 + 91, party=party, loadouts=((party[0], loadout["id"]),),
            biomes=tuple(list(self.catalog.biomes)[:4]), layout="ring",
            starting_pressure=480, ladder_rank=7,
            modifiers=("accelerated_pressure", "elite_weather"),
        )
        code = encode_code(config, self.catalog)
        self.assertLess(len(code), 400)
        self.assertEqual(config, decode_code(code, self.catalog))

    def test_strict_code_rejects_tampering_and_incompatible_fields(self) -> None:
        code = encode_code(ExpeditionConfig(seed=9), self.catalog)
        with self.assertRaises(ChallengeError):
            decode_code(code[:-1] + ("0" if code[-1] != "0" else "1"), self.catalog)
        with self.assertRaises(ChallengeError):
            validate_config(ExpeditionConfig(seed=True), self.catalog)
        with self.assertRaises(ChallengeError):
            validate_config(ExpeditionConfig(seed=1, modifiers=("unknown",)), self.catalog)
        with self.assertRaises(ChallengeError):
            validate_config(ExpeditionConfig(seed=1, content_packs=("base:future",)), self.catalog)

    def test_custom_world_party_and_pressure_are_frozen_and_saved(self) -> None:
        party = tuple(list(self.catalog.heroes)[4:8])
        config = ExpeditionConfig(
            seed=91, party=party, biomes=tuple(list(self.catalog.biomes)[3:7]),
            layout="ring", starting_pressure=480,
            modifiers=("bright_but_loud", "hazardous_routes"),
        )
        engine = GameEngine.custom(self.catalog, config)
        self.assertEqual("ring", self.catalog.worlds[engine.state.world_id]["layout"])
        self.assertEqual(list(config.biomes), engine.state.biome_ids)
        self.assertEqual(list(party), engine.state.hub_selection)
        self.assertEqual(480, engine.state.pressure)
        self.assertEqual("custom", engine.state.expedition_mode)
        self.assertEqual(engine.snapshot(), GameEngine.from_snapshot(self.catalog, engine.snapshot()).snapshot())

    def test_custom_modifiers_have_disclosed_foundation_effects(self) -> None:
        plain = GameEngine.custom(self.catalog, ExpeditionConfig(seed=92))
        altered = GameEngine.custom(
            self.catalog,
            ExpeditionConfig(seed=92, modifiers=("objective_sprint", "scarce_supply")),
        )
        self.assertEqual(1, altered.state.required_objectives)
        self.assertEqual(600, altered.state.pressure)
        self.assertEqual(plain.state.supplies - 2, altered.state.supplies)
        with self.assertRaises(RuleError):
            GameEngine.custom(self.catalog, ExpeditionConfig(seed=92), mode="networked")

    def configured(self, modifier: str, seed: int = 93) -> GameEngine:
        return GameEngine.custom(
            self.catalog, ExpeditionConfig(seed=seed, modifiers=(modifier,)),
            start_in_hub=False, mode="challenge",
        )

    def test_route_and_director_contracts_change_only_simulated_state(self) -> None:
        plain = GameEngine.new(self.catalog, 93)
        fast = self.configured("accelerated_pressure")
        self.assertGreater(
            fast._advance_pressure(PressureSource.TRAVEL, 1, "test"),
            plain._advance_pressure(PressureSource.TRAVEL, 1, "test"),
        )
        bright = self.configured("bright_but_loud")
        self.assertEqual(GameEngine.new(self.catalog, 93).state.light + 15, bright.state.light)
        hungry = self.configured("hungry_light")
        self.assertEqual(GameEngine.new(self.catalog, 93).state.light - 25, hungry.state.light)
        clock = self.configured("guarded_clock")
        unguarded = clock._advance_pressure(PressureSource.ENEMY_ROUND, 1, "open")
        clock.state.heroes[0].guarded_by = clock.state.heroes[1].id
        guarded = clock._advance_pressure(PressureSource.ENEMY_ROUND, 1, "guarded")
        self.assertEqual(4, unguarded - guarded)
        hazard = self.configured("hazardous_routes")
        self.assertEqual(plain.world_director().hazard_reach + 1, hazard.world_director().hazard_reach)
        elite = self.configured("elite_weather")
        elite.start_combat("lost_shift", "elite")
        self.assertGreaterEqual(len(elite.state.encounter_modules), 1)
        reaction = self.configured("third_card_reaction")
        reaction.start_combat("lost_shift")
        self.assertIn("base:third_bell", reaction.state.encounter_modules)

    def test_burden_and_salvage_contracts_are_real_not_labels(self) -> None:
        debt = self.configured("curse_bargain", 94)
        self.assertEqual(1, sum(map(sum, (owned.values() for owned in debt.state.curses.values()))))
        self.assertEqual(GameEngine.new(self.catalog, 94).state.supplies + 2, debt.state.supplies)
        casualty = self.configured("casualty_cache", 95)
        before = casualty.state.supplies
        fallen = casualty.living_heroes()[0]
        fallen.hp = 0
        casualty._hero_died(fallen)
        self.assertEqual(before + 1, casualty.state.supplies)
        wound = self.configured("wound_dividend", 96)
        hero = wound.living_heroes()[0]
        hero.statuses["wound"] = 2
        wound._tick_wound(hero)
        self.assertEqual(1, hero.statuses["focus"])

    def test_combat_contracts_modify_the_disclosed_arithmetic(self) -> None:
        cash = self.configured("cash_marks", 97)
        cash.start_combat("lost_shift")
        actor, enemy = cash.living_heroes()[0], cash.living_enemies()[0]
        enemy.statuses["marked"] = 1
        self.assertEqual(6, cash._outgoing_damage(actor, 5, enemy))

        furnace = self.configured("stress_furnace", 98)
        furnace.start_combat("lost_shift")
        actor = furnace.living_heroes()[0]
        actor.stress = 60
        self.assertEqual(6, furnace._outgoing_damage(actor, 5, furnace.living_enemies()[0]))
        furnace._change_stress(actor, -4)
        self.assertEqual(57, actor.stress)

        margin = self.configured("zero_margin", 99)
        hero = margin.living_heroes()[0]
        hero.hp -= 10
        margin._heal(hero, 4)
        self.assertEqual(hero.max_hp - 5, hero.hp)

        guard = self.configured("fragile_guard", 100)
        guard.start_combat("lost_shift")
        actor = guard.living_heroes()[0]
        guard._apply_effect_primary(actor, [actor], {"op": "block", "amount": 3})
        self.assertEqual(5, actor.block)

        recoil = self.configured("control_recoil", 101)
        recoil.start_combat("lost_shift")
        actor, enemy = recoil.living_heroes()[0], recoil.living_enemies()[0]
        recoil._apply_effect_primary(actor, [enemy], {"op": "status", "status": "weak", "amount": 1})
        self.assertEqual(2, actor.stress)

    def test_velocity_position_and_reward_contracts_activate(self) -> None:
        current = self.configured("discard_current", 102)
        current.start_combat("lost_shift")
        before = len(current.state.hand)
        current._apply_effect_primary(current.living_heroes()[0], [], {"op": "discard", "amount": 1})
        self.assertEqual(before, len(current.state.hand))

        narrow = self.configured("narrow_fire", 103)
        narrow.start_combat("lost_shift")
        card = next(
            card for card in narrow.state.deck
            if len(narrow.catalog.cards[card.card_id]["from_ranks"]) <= 2
        )
        actor = next(hero for hero in narrow.living_heroes() if hero.id == narrow.catalog.cards[card.card_id]["hero"])
        narrow.state.effect_counters[f"challenge:moved:{actor.id}:{narrow.state.round}"] = 1
        with narrow.attribution(card.card_id):
            self.assertEqual(7, narrow._outgoing_damage(actor, 5, narrow.living_enemies()[0]))

        rewards = self.configured("pressured_rewards", 104)
        rewards.state.pressure = 480
        rewards.start_combat("lost_shift")
        for enemy in rewards.state.enemies:
            enemy.hp = 0
        rewards._combat_victory()
        self.assertGreaterEqual(len(rewards.state.rewards), 4)


if __name__ == "__main__":
    unittest.main()
