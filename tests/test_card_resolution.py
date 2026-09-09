"""Constructed boundary scenarios for complete owner-bound card transactions."""

import json
import random
import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import CardInstance, GameEngine, RuleError
from dumbest_dungeon.resolution import Payload
from dumbest_dungeon.triggers import EventType as E


class CardResolutionTests(unittest.TestCase):
    def fixture(self, *, finale=False):
        engine = GameEngine.new(load_catalog(), 42)
        hero = engine.living_heroes()[0]
        engine.acquire_boon(hero.id, "countercurrent")
        engine.acquire_boon(hero.id, "hunters_rhythm")
        engine.acquire_curse(hero.id, "frayed_focus")
        engine.start_combat("lost_shift")
        hero.rank, engine.state.heroes[1].rank = 2, 1
        target = engine.living_enemies()[0]
        if finale:
            for enemy in engine.state.enemies[1:]:
                enemy.hp = 0
            target.hp = 1
        else:
            target.statuses["riposte"] = 2
        engine.state.hand = [CardInstance("shield_rush"), CardInstance("brace")]
        engine.state.draw_pile = [CardInstance("baton_strike"), CardInstance("arc_welder")]
        return engine, target.id

    def test_every_card_phase_resumes_through_triggers_and_victory_cleanup(self) -> None:
        for finale in (False, True):
            ordinary, target = self.fixture(finale=finale)
            checkpoint = GameEngine.from_snapshot(ordinary.catalog, ordinary.snapshot())
            ordinary.play_card(0, target)
            checkpoint.play_card(0, target, resolve=False)
            root = checkpoint.resolution.state.root_id
            boundaries = 0
            while checkpoint.resolution.step(checkpoint._resolution_listeners, checkpoint._resolve_event, checkpoint._resolve_trigger):
                checkpoint = GameEngine.from_snapshot(checkpoint.catalog, json.loads(json.dumps(checkpoint.snapshot())))
                boundaries += 1
            checkpoint.resolve_pending()
            self.assertGreater(boundaries, 50)
            self.assertEqual(ordinary.snapshot(), checkpoint.snapshot())
            roots = [row for row in ordinary.state.ledger.records if row.kind == "resolution_root"]
            self.assertEqual([root - 1, root], [row.data["root"] for row in roots])
            self.assertEqual("round:draw", roots[0].data["trace"][0]["source_id"])
            if finale:
                self.assertEqual("reward", ordinary.state.phase)
            else:
                self.assertEqual(["brace", "baton_strike"], [card.card_id for card in ordinary.state.hand])

    def test_no_manual_action_can_interrupt_a_card_transaction(self) -> None:
        engine, target = self.fixture()
        engine.play_card(0, target, resolve=False)
        before = engine.snapshot()
        with self.assertRaises(RuleError):
            engine.end_turn()
        with self.assertRaises(RuleError):
            engine.play_card(0, engine.living_heroes()[0].id)
        self.assertEqual(before, engine.snapshot())
        engine.resolve_pending()

    def test_out_of_range_continuation_is_rejected_before_resume(self) -> None:
        engine, target = self.fixture()
        engine.play_card(0, target, resolve=False)
        raw = engine.snapshot()
        raw["resolution_queue"]["state"]["pending"][0]["payload"]["effect_index"] = 999
        with self.assertRaisesRegex(RuleError, "out-of-range"):
            GameEngine.from_snapshot(engine.catalog, raw)

    def test_counterattack_casualty_cancels_owner_continuations_and_survivors_continue(self) -> None:
        engine, target = self.fixture()
        hero = next(hero for hero in engine.state.heroes if hero.id == "warden")
        hero.hp, hero.block, hero.deaths_door = 0, 0, True
        engine.rng = random.Random(1)
        engine.play_card(0, target, resolve=False)
        while engine.resolution.step(engine._resolution_listeners, engine._resolve_event, engine._resolve_trigger):
            engine = GameEngine.from_snapshot(engine.catalog, engine.snapshot())
        engine.resolve_pending()
        self.assertEqual("combat", engine.state.phase)
        self.assertEqual(3, len(engine.living_heroes()))
        self.assertEqual([1, 2, 3], [actor.rank for actor in engine.living_heroes()])
        self.assertFalse(any(engine.catalog.cards[card.card_id]["hero"] == "warden" for card in engine.state.deck))
        self.assertEqual(["arc_welder"], [card.card_id for card in engine.state.draw_pile])
        engine.end_turn()
        self.assertEqual(2, engine.state.round)

    def test_each_bound_curse_resumes_at_every_dispatch_boundary(self) -> None:
        catalog = load_catalog()
        for identity, definition in catalog.curses.items():
            if definition["kind"] != "card":
                continue
            engine = GameEngine.new(catalog, 42)
            engine.start_combat("lost_shift")
            hero = engine.living_heroes()[0]
            engine.acquire_curse(hero.id, identity)
            event = E.CARD_HELD if identity == "dread_forecast" else E.CARD_DRAW
            engine.resolution.begin(combat_token=1, turn_token=1)
            engine.resolution.submit(event, identity, (hero.id,), Payload(actor_id=hero.id, card_id=identity))
            loaded = GameEngine.from_snapshot(catalog, engine.snapshot())
            engine.resolve_pending()
            while loaded.resolution.step(loaded._resolution_listeners, loaded._resolve_event, loaded._resolve_trigger):
                loaded = GameEngine.from_snapshot(catalog, loaded.snapshot())
            loaded.resolve_pending()
            self.assertEqual(engine.snapshot(), loaded.snapshot(), identity)
            if identity != "dead_channel":
                self.assertTrue(any(row.source_id == identity and row.kind in {"stress", "status", "energy", "effect"}
                                    for row in engine.state.ledger.records), identity)
