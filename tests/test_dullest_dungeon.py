"""The tavern expedition uses the imported map and symmetric ranked card rules."""

import copy
import curses
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine
import dumbest_dungeon.expedition as expedition
from dumbest_dungeon.expedition_ui import ExpeditionUI, run_expedition
from dumbest_dungeon.office_art import OFFICE_SPRITES, office_card_glyph, rival_costumes
from dumbest_dungeon.office_content import OFFICE_ROLES, OFFICE_SQUADS, office_catalog
from dumbest_dungeon.tabletop import collection_for, initial_collection, patrons
from jomon.actions import interact
from jomon.save import SaveError, load_game, save_game
from jomon.state import Position, SAVE_FORMAT, create_world, game_state_from_dict, validate_state
from jomon.world import is_walkable


class ExpeditionRulesTests(unittest.TestCase):
    def setUp(self):
        self.catalog = load_catalog()
        self.roles = list(self.catalog.heroes)
        self.match = expedition.new_match("expedition-rules", "crew-1", "crew-2", self.roles[:4], self.roles[4:8])

    def test_all_imported_specialists_and_cards_have_office_faces(self):
        roles, cards = office_catalog()
        self.assertEqual((len(roles), len(cards)), (25, 290))
        self.assertEqual(set(cards), set(self.catalog.cards))
        self.assertTrue(all(card.name and card.description and card.role in roles for card in cards.values()))
        self.assertTrue(all(roles[role]["max_hp"] == hero["max_hp"] for role, hero in self.catalog.heroes.items()))

    def test_office_portraits_and_rival_costumes_cover_the_art_catalog(self):
        self.assertEqual(set(OFFICE_SPRITES), set(OFFICE_ROLES))
        self.assertEqual(len({tuple(sprite) for sprite in OFFICE_SPRITES.values()}), 25)
        self.assertEqual(len({office_card_glyph(role) for role in OFFICE_SPRITES}), 25)
        self.assertTrue(all(len(sprite) == 5 and all(len(line) == 7 and line.isascii() for line in sprite)
                            for sprite in OFFICE_SPRITES.values()))
        seen = {costume for seed in range(800) for costume in rival_costumes(seed)}
        self.assertEqual(seen, set(self.catalog.art["enemies"]))

    def test_original_formations_and_alternate_kits_are_selectable(self):
        self.assertEqual(set(OFFICE_SQUADS), set(self.catalog.squads))
        ui = ExpeditionUI.__new__(ExpeditionUI)
        ui.catalog = self.catalog
        for squad in self.catalog.squads.values():
            collection = initial_collection()
            with patch.object(ui, "_menu", side_effect=[list(self.catalog.squads).index(squad["id"]), 0]):
                ui._choose_formation(collection)
            self.assertEqual(collection["roles"], squad["formation"])
            self.assertEqual(collection["doctrine"], squad["doctrine"])
        for role in self.catalog.heroes:
            collection = initial_collection()
            other = [item for item in self.catalog.heroes if item != role][:3]
            collection["roles"] = [role, *other]
            collection["deck"] = [card for worker in collection["roles"]
                                  for card in self.catalog.heroes[worker]["starter_deck"]]
            ui._toggle_loadout(collection, 0)
            advanced = next(item for item in self.catalog.loadouts.values() if item["hero"] == role)
            self.assertEqual([card for card in collection["deck"] if self.catalog.cards[card]["hero"] == role], advanced["cards"])
            ui._toggle_loadout(collection, 0)
            self.assertEqual([card for card in collection["deck"] if self.catalog.cards[card]["hero"] == role], self.catalog.heroes[role]["starter_deck"])

    def test_all_290_imported_cards_resolve_in_ranked_pvp(self):
        roster = self.roles
        played = set()
        for role in roster:
            team = [role] + [other for other in roster if other != role][:3]
            rival = [other for other in roster if other not in team][:4]
            match = expedition.new_match(role, "crew-1", "crew-2", team, rival)
            for card_id, definition in self.catalog.cards.items():
                if definition["hero"] != role:
                    continue
                match["phase"] = "combat"
                match["turn"] = 0
                match["teams"][0]["hand"] = [card_id]
                match["teams"][0]["energy"] = 9
                match["teams"][0]["plays"] = 0
                match["teams"][0]["triggers"] = {}
                for side in (0, 1):
                    for index, actor in enumerate(match["teams"][side]["actors"]):
                        actor.update(hp=actor["max_hp"], rank=index + 1, stress=0,
                                     block=0, statuses={}, respawn=0, guarded_by=None,
                                     guard_turns=0)
                origin = definition["from_ranks"][0]
                match["teams"][0]["actors"][0]["rank"] = origin
                if origin != 1:
                    match["teams"][0]["actors"][origin - 1]["rank"] = 1
                targets = expedition.valid_targets(match, 0)
                self.assertTrue(targets, card_id)
                expedition.play_card(match, 0, targets[0])
                played.add(card_id)
        self.assertEqual(played, set(self.catalog.cards))

    def test_treatments_and_policies_obey_catalog_compatibility(self):
        self.assertFalse(expedition.doctrine_compatible(self.roles[:4], "base:rolling_dance"))
        self.assertTrue(expedition.doctrine_compatible(self.roles[:4], "base:mark_window"))
        self.assertTrue(expedition.infusion_compatible("baton_strike", "base:hinged_grip"))
        match = self.match
        match["phase"] = "combat"
        match["teams"][0]["hand"] = ["baton_strike"]
        match["teams"][0]["actors"][0]["rank"] = 4
        match["teams"][0]["actors"][3]["rank"] = 1
        match["teams"][0]["infusions"]["baton_strike"] = "base:hinged_grip"
        self.assertTrue(expedition.valid_targets(match, 0))

    def test_original_map_and_neutral_features_are_used(self):
        match = self.match
        generated = GameEngine.new(self.catalog, match["world_seed"], start_in_hub=True).state
        self.assertEqual(match["board"], generated.world_tiles)
        self.assertEqual((len(match["board"]), len(match["board"][0])), (35, 117))
        self.assertEqual(match["room_positions"], generated.room_positions)
        self.assertEqual(len(match["hazards"]), len(generated.hazards))
        self.assertEqual(len(match["facilities"]), len(generated.facilities))
        self.assertEqual(len(match["pickups"]), len(generated.pickups))
        self.assertEqual(len(match["landmarks"]), len(generated.landmarks))
        self.assertEqual(len(match["stations"]), sum(room.kind in {"event", "camp", "upgrade", "cache"} for room in generated.rooms))
        self.assertEqual(len(match["teams"]), 2)
        self.assertTrue(all(len(team["actors"]) == 4 for team in match["teams"]))
        self.assertNotIn("patrols", match)
        self.assertNotIn("objectives", match)

    def test_all_six_original_layouts_remain_reachable(self):
        layouts = set()
        for seed in range(80):
            match = expedition.new_match(str(seed), "crew-1", "crew-2", self.roles[:4], self.roles[4:8])
            layouts.add(self.catalog.worlds[match["world_id"]]["layout"])
            self.assertTrue(expedition.path_to(match, 0, tuple(match["files"][1]["home"])))
            self.assertTrue(expedition.path_to(match, 1, tuple(match["files"][0]["home"])))
            if len(layouts) == 6:
                break
        self.assertEqual(len(layouts), 6)

    def test_weighted_destination_order_and_atomic_invalid_move(self):
        match = self.match
        destination = tuple(match["files"][1]["home"])
        before = copy.deepcopy(match)
        with self.assertRaisesRegex(ValueError, "at most"):
            expedition.move_to(match, destination)
        self.assertEqual(match, before)
        leg = expedition._ai_destination(match, destination)
        self.assertIsNotNone(leg)
        walked = expedition.move_to(match, leg)
        self.assertTrue(walked)
        self.assertEqual(match["teams"][0]["orders"], 1)
        self.assertEqual(expedition._position(match, 0), walked[-1])
        costs = {terrain["glyph"]: terrain["cost"] for terrain in self.catalog.terrains.values()}
        spent = sum(costs[match["board"][y][x]] for x, y in walked)
        self.assertLessEqual(spent, expedition.ORDER_TICKS)
        self.assertEqual(match["teams"][0]["travel_ticks"], spent)
        self.assertEqual(match["teams"][0]["exploration_steps"], len(walked))
        self.assertEqual(match["teams"][0]["light"], 100 - spent // self.catalog.balance["exploration_steps_per_light"])

    def test_exploration_reveals_nearby_features(self):
        match = self.match
        team = match["teams"][0]
        distant = next(item for item in match["pickups"]
                       if not item["hidden"] and expedition._distance(tuple(team["position"]), (item["x"], item["y"])) >
                       self.catalog.biomes[expedition._biome_at(match, item["x"], item["y"])]["mechanics"]["visibility"].get("feature_radius", 5))
        identity = f"pickups:{distant['id']}"
        self.assertNotIn(identity, team["known"])
        team["position"] = [distant["x"], distant["y"]]
        expedition._reveal_nearby(match, 0)
        self.assertIn(identity, team["known"])
        hidden = next(item for item in match["pickups"] if item["hidden"])
        team["position"] = [hidden["x"], hidden["y"]]
        expedition._reveal_nearby(match, 0)
        self.assertNotIn(f"pickups:{hidden['id']}", team["known"])

    def test_contact_tile_resolves_neutral_pickup_before_combat(self):
        match = self.match
        path = expedition.path_to(match, 0, tuple(match["files"][1]["home"]))
        self.assertGreaterEqual(len(path), 2)
        match["teams"][1]["position"] = list(path[1])
        for hazard in match["hazards"]:
            hazard["active"] = False
        for pickup in match["pickups"]:
            pickup["resolved"] = True
        for facility in match["facilities"]:
            facility["used"] = True
        for station in match["stations"]:
            station["used"] = True
        pickup = next(item for item in match["pickups"] if item["kind"] == "item")
        pickup.update(x=path[0][0], y=path[0][1], resolved=False)
        item = pickup["payload"]["item_id"]
        before = match["teams"][0]["items"].get(item, 0)
        expedition.move_to(match, path[0])
        self.assertEqual(match["teams"][0]["items"][item], before + 1)
        self.assertEqual(match["phase"], "combat")

    def test_contact_opens_ranked_combat_and_both_parties_use_cards(self):
        match = self.match
        origin = tuple(match["files"][0]["home"])
        match["teams"][1]["position"] = [origin[0] + 2, origin[1]]
        expedition.move_to(match, (origin[0] + 1, origin[1]))
        self.assertEqual(match["phase"], "combat")
        self.assertTrue(match["teams"][0]["hand"])
        expedition.end_turn(match)
        self.assertEqual(match["turn"], 1)
        self.assertTrue(match["teams"][1]["hand"])
        expedition.patron_turn(match)
        self.assertEqual(match["phase"], "combat")
        self.assertEqual(match["round"], 1)
        self.assertEqual(match["map_turns"], 1)

    def test_combat_continues_until_a_party_retreats_or_is_wiped(self):
        match = self.match
        path = expedition.path_to(match, 0, tuple(match["files"][1]["home"]))
        origin = tuple(match["teams"][0]["position"])
        match["teams"][0]["position"] = list(path[0])
        match["teams"][1]["position"] = list(path[1])
        match["phase"] = "combat"
        expedition._begin_combat(match)
        expedition.end_turn(match)
        expedition.end_turn(match)
        self.assertEqual(match["phase"], "combat")
        self.assertEqual(match["combat_turns"], 2)
        self.assertEqual(match["round"], 1)
        self.assertIn(origin, expedition.retreat_destinations(match))
        expedition.retreat(match, origin)
        self.assertEqual(match["phase"], "map")
        self.assertEqual(match["teams"][0]["orders"], expedition.ORDERS_PER_TURN)
        self.assertEqual(match["teams"][0]["position"], list(origin))

        other = expedition.new_match("patron-retreat", "crew-1", "crew-2", self.roles[:4], self.roles[4:8])
        path = expedition.path_to(other, 0, tuple(other["files"][1]["home"]))
        other["teams"][0]["position"] = list(path[0])
        other["teams"][1]["position"] = list(path[1])
        other["turn"] = 1
        other["phase"] = "combat"
        self.assertIn(path[2], expedition.retreat_destinations(other))
        expedition.retreat(other, path[2])
        self.assertEqual(other["teams"][1]["position"], list(path[2]))
        self.assertEqual(other["phase"], "map")

    def test_full_combat_exchanges_do_not_advance_the_match_clock(self):
        match = self.match
        match["teams"][1]["position"] = list(expedition.path_to(match, 0, tuple(match["files"][1]["home"]))[0])
        expedition.engage_if_touching(match)
        for _ in range(6):
            expedition.end_turn(match)
        self.assertEqual(match["phase"], "combat")
        self.assertEqual(match["combat_turns"], 6)
        self.assertEqual(match["map_turns"], 1)
        self.assertEqual(match["round"], 1)

    def test_full_party_wipe_ends_combat_and_routes_home(self):
        match = self.match
        path = expedition.path_to(match, 0, tuple(match["files"][1]["home"]))
        match["teams"][0]["position"] = list(path[0])
        match["teams"][1]["position"] = list(path[1])
        match["phase"] = "combat"
        expedition._begin_combat(match)
        attacker = match["teams"][0]["actors"][0]
        for target in match["teams"][1]["actors"]:
            expedition._damage(match, attacker, target, target["max_hp"] + 10)
        self.assertEqual(match["phase"], "map")
        self.assertEqual(match["teams"][1]["position"], match["files"][1]["home"])
        self.assertEqual([actor["respawn"] for actor in match["teams"][1]["actors"]], [2] * 4)
        self.assertEqual(match["contact_cooldown"], 2)

    def test_original_rank_and_target_rules_apply_to_both_sides(self):
        match = self.match
        match["phase"] = "combat"
        match["teams"][0]["doctrine"] = "base:guard_rotation"
        match["teams"][0]["hand"] = ["baton_strike"]
        targets = expedition.valid_targets(match, 0)
        self.assertEqual(targets, [actor["id"] for actor in match["teams"][1]["actors"][:2]])
        before = copy.deepcopy(match)
        with self.assertRaisesRegex(ValueError, "ranked target"):
            expedition.play_card(match, 0, match["teams"][1]["actors"][3]["id"])
        self.assertEqual(match, before)
        target = match["teams"][1]["actors"][0]
        hp = target["hp"]
        expedition.play_card(match, 0, target["id"])
        self.assertEqual(target["hp"], hp - 7)
        match["turn"] = 1
        match["teams"][1]["actors"][0]["role"] = "warden"
        match["teams"][1]["hand"] = ["baton_strike"]
        self.assertTrue(expedition.valid_targets(match, 0))

    def test_group_condition_uses_original_any_target_rule(self):
        roles = ["warden", "medic", "scout", "cryonaut"]
        rivals = [role for role in self.roles if role not in roles][:4]
        match = expedition.new_match("group-condition", "crew-1", "crew-2", roles, rivals)
        match["phase"] = "combat"
        match["teams"][0]["hand"] = ["absolute_zero"]
        match["teams"][1]["actors"][0]["statuses"]["vulnerable"] = 1
        expedition.play_card(match, 0, "all_enemies")
        self.assertTrue(all(actor["statuses"].get("stun") for actor in match["teams"][1]["actors"]))
        actor = match["teams"][0]["actors"][0]
        actor["hp"] = actor["max_hp"] // 2 + 1
        self.assertTrue(expedition._matches_state(actor, "healthy"))
        actor["statuses"]["wound"] = 1
        self.assertTrue(expedition._matches_state(actor, "wounded"))
        expedition._apply_effect(match, actor, [actor], {"op": "energy", "amount": 12})
        self.assertEqual(match["teams"][0]["energy"], 13)

    def test_file_hold_ko_drop_and_two_turn_return(self):
        match = self.match
        carrier = match["teams"][0]["actors"][0]
        match["files"][1]["carrier"] = carrier["id"]
        expedition.end_turn(match)
        self.assertEqual(match["pending_score"], 0)
        self.assertEqual(match["scores"], [0, 0])
        expedition._knockout(match, carrier)
        self.assertEqual(match["files"][1]["dropped"], match["files"][0]["home"])
        self.assertIsNone(match["pending_score"])
        expedition.end_turn(match)
        self.assertEqual(match["scores"], [0, 0])
        self.assertEqual(carrier["respawn"], 1)
        expedition.end_turn(match)
        expedition.end_turn(match)
        self.assertEqual(carrier["respawn"], 0)
        self.assertEqual(carrier["hp"], carrier["max_hp"])

    def test_knocked_out_specialists_wait_until_map_turns_to_return(self):
        match = self.match
        actor = match["teams"][0]["actors"][0]
        expedition._knockout(match, actor)
        match["phase"] = "combat"
        expedition.end_turn(match)
        expedition.end_turn(match)
        self.assertEqual(actor["respawn"], 2)
        self.assertEqual(actor["hp"], 0)
        expedition._close_combat(match, "The parties separate.")
        expedition.end_turn(match)
        expedition.end_turn(match)
        self.assertEqual(actor["respawn"], 1)
        expedition.end_turn(match)
        expedition.end_turn(match)
        self.assertEqual(actor["hp"], actor["max_hp"])

    def test_uninterrupted_file_scores_after_rival_turn(self):
        match = self.match
        match["files"][1]["carrier"] = match["teams"][0]["actors"][0]["id"]
        expedition.end_turn(match)
        self.assertEqual(match["scores"], [0, 0])
        expedition.end_turn(match)
        self.assertEqual(match["scores"], [1, 0])
        self.assertIsNone(match["files"][1]["carrier"])

    def test_generated_route_can_capture_and_return_a_file(self):
        match = expedition.new_match("score-route", "crew-1", "crew-2", self.roles[:4], self.roles[4:8])
        route = expedition.path_to(match, 0, tuple(match["files"][1]["home"]))
        floors = [(x, y) for y, row in enumerate(match["board"]) for x, glyph in enumerate(row)
                  if glyph in expedition.WALKABLE_TILES]
        away = max(floors, key=lambda cell: min(expedition._distance(cell, step) for step in route))
        match["teams"][1]["position"] = list(away)
        for hazard in match["hazards"]:
            hazard["active"] = False
        for pickup in match["pickups"]:
            pickup["resolved"] = True
        for facility in match["facilities"]:
            facility["used"] = True
        for station in match["stations"]:
            station["used"] = True
        for _ in range(12):
            if match["scores"][0]:
                break
            if match["turn"] == 0:
                goal = tuple(match["files"][0]["home"] if match["files"][1]["carrier"] else match["files"][1]["home"])
                for _ in range(expedition.ORDERS_PER_TURN):
                    destination = expedition._ai_destination(match, goal)
                    if destination is None:
                        break
                    expedition.move_to(match, destination)
                expedition.end_turn(match)
            else:
                expedition.end_turn(match)
        self.assertEqual(match["scores"], [1, 0])
        self.assertLess(match["round"], expedition.MAX_ROUNDS)

    def test_first_to_two_and_exact_18_plus_4_limit(self):
        match = self.match
        match["scores"][0] = 1
        match["files"][1]["carrier"] = match["teams"][0]["actors"][0]["id"]
        expedition.end_turn(match)
        expedition.end_turn(match)
        self.assertEqual(match["winner"], 0)
        draw = expedition.new_match("overtime", "crew-1", "crew-2", self.roles[:4], self.roles[4:8])
        while draw["winner"] is None:
            expedition.end_turn(draw)
        self.assertEqual(draw["winner"], "draw")
        self.assertEqual(draw["round"], expedition.MAX_ROUNDS + 4 + 1)

    def test_pickups_and_facilities_offer_choices(self):
        match = self.match
        pickup = next(item for item in match["pickups"] if item["kind"] in ("boon", "bargain"))
        match["teams"][0]["position"] = [pickup["x"], pickup["y"]]
        expedition._arrival(match, 0)
        if match["pending"]["kind"] == "boon":
            expedition.choose_reward(match, 0)
            self.assertEqual(match["pending"]["kind"], "recipient")
            expedition.choose_reward(match, 0)
        self.assertEqual(match["pending"]["kind"], "draft")
        before = len(match["teams"][0]["deck"])
        expedition.choose_reward(match, 0)
        self.assertEqual(len(match["teams"][0]["deck"]), before + 1)
        facility = next(item for item in match["facilities"] if not item["used"])
        match["teams"][0]["position"] = [facility["x"], facility["y"]]
        expedition._arrival(match, 0)
        self.assertEqual(match["pending"]["kind"], "facility")
        expedition.choose_reward(match, 0)
        self.assertTrue(facility["used"])

    def test_boon_owner_and_original_start_block_effect(self):
        match = self.match
        owner = match["teams"][0]["actors"][0]
        match["teams"][1]["position"] = [match["teams"][0]["position"][0] + 1, match["teams"][0]["position"][1]]
        baseline = copy.deepcopy(match)
        self.assertTrue(expedition.engage_if_touching(baseline))
        match["teams"][0]["boons"].append({"id": "iron_benediction", "owner": owner["id"]})
        self.assertTrue(expedition.engage_if_touching(match))
        self.assertEqual(owner["block"] - baseline["teams"][0]["actors"][0]["block"], 3)

    def test_unaffordable_facility_can_be_left_untouched(self):
        match = self.match
        facility = match["facilities"][0]
        match["teams"][0]["position"] = [facility["x"], facility["y"]]
        expedition._arrival(match, 0)
        self.assertEqual(match["pending"]["kind"], "facility")
        expedition.choose_reward(match, len(match["pending"]["choices"]))
        self.assertIsNone(match["pending"])
        self.assertFalse(facility["used"])

    def test_generated_cache_camp_workshop_and_event_are_playable(self):
        match = self.match
        team = match["teams"][0]
        cache = next(station for station in match["stations"] if station["kind"] == "cache")
        team["supplies"] = 0
        team["light"] = 50
        team["position"] = [cache["x"], cache["y"]]
        expedition._arrival(match, 0)
        self.assertEqual((team["supplies"], team["light"], cache["used"]), (2, 70, True))

        camp = next(station for station in match["stations"] if station["kind"] == "camp")
        team["actors"][0]["hp"] -= 8
        team["actors"][0]["stress"] = 12
        team["position"] = [camp["x"], camp["y"]]
        expedition._arrival(match, 0)
        self.assertEqual(match["pending"]["kind"], "camp")
        expedition.choose_reward(match, 0)
        self.assertEqual(team["actors"][0]["hp"], team["actors"][0]["max_hp"] - 1)
        self.assertEqual(team["actors"][0]["stress"], 2)

        workshop = next(station for station in match["stations"] if station["kind"] == "upgrade")
        team["position"] = [workshop["x"], workshop["y"]]
        expedition._arrival(match, 0)
        self.assertEqual(match["pending"]["kind"], "upgrade")
        chosen_copy = match["pending"]["copy_ids"][0]
        expedition.choose_reward(match, 0)
        upgraded = next(card for card in team["deck"] if card["copy_id"] == chosen_copy)
        self.assertTrue(upgraded["upgraded"])
        self.assertTrue(workshop["used"])
        self.assertEqual(sum(card["upgraded"] for card in team["deck"]), 1)

        event = next(station for station in match["stations"] if station["kind"] == "event")
        team["position"] = [event["x"], event["y"]]
        expedition._arrival(match, 0)
        self.assertEqual(match["pending"]["kind"], "event")
        expedition.choose_reward(match, len(match["pending"]["choices"]))
        self.assertFalse(event["used"])

    def test_facility_redacts_one_card_copy_and_preserves_buffs_on_cleanse(self):
        match = self.match
        team = match["teams"][0]
        team["draw"] = team["deck"][:]
        actor = team["actors"][0]
        actor["statuses"] = {"focus": 2, "marked": 2, "wound": 2}
        facility = match["facilities"][0]
        expedition._facility_effect(match, 0, facility, {"op": "cleanse_all", "amount": 1})
        self.assertEqual(actor["statuses"], {"focus": 2})
        expedition._facility_effect(match, 0, facility, {"op": "remove_random", "amount": 1})
        self.assertEqual(len(team["deck"]), 19)
        self.assertEqual(len(team["draw"]), 19)
        self.assertEqual({card["copy_id"] for card in team["draw"]},
                         {card["copy_id"] for card in team["deck"]})

    def test_upgraded_copy_uses_imported_upgrade_damage(self):
        match = self.match
        team = match["teams"][0]
        team["doctrine"] = "base:guard_rotation"
        upgraded = next(card for card in team["deck"] if card["id"] == "baton_strike")
        upgraded["upgraded"] = True
        target = match["teams"][1]["actors"][0]
        team["hand"] = [upgraded]
        team["energy"] = 9
        match["phase"] = "combat"
        hp = target["hp"]
        expedition.play_card(match, 0, target["id"])
        self.assertEqual(target["hp"], hp - 10)


class TavernIntegrationTests(unittest.TestCase):
    def setUp(self):
        self.state = create_world("expedition-integration")
        self.state.jomon_space = "tavern"
        self.state.position = Position(31, 4)

    def test_table_is_physical_and_match_freezes_jomon_time(self):
        self.assertTrue(is_walkable(self.state, self.state.position))
        self.assertEqual(interact(self.state).overlay, "tabletop")
        before = self.state.world_time
        match = expedition.start_match(self.state, patrons(self.state)[0].id)
        self.assertEqual(self.state.world_time, before + 1)
        expedition.end_turn(match)
        expedition.patron_turn(match)
        self.assertEqual(self.state.world_time, before + 1)

    def test_atomic_jomon_save_resumes_generated_match(self):
        match = expedition.start_match(self.state, patrons(self.state)[0].id)
        leg = expedition._ai_destination(match, tuple(match["files"][1]["home"]))
        self.assertIsNotNone(leg)
        expedition.move_to(match, leg)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "jomon.json"
            save_game(self.state, path)
            resumed = load_game(path)
        self.assertEqual(resumed.tabletop["active_match"], self.state.tabletop["active_match"])
        expedition.end_turn(self.state.tabletop["active_match"])
        expedition.patron_turn(self.state.tabletop["active_match"])
        expedition.end_turn(resumed.tabletop["active_match"])
        expedition.patron_turn(resumed.tabletop["active_match"])
        self.assertEqual(resumed.tabletop["active_match"], self.state.tabletop["active_match"])

    def test_stabilized_generated_terrain_survives_save_roundtrip(self):
        match = expedition.start_match(self.state, patrons(self.state)[0].id)
        station = match["stations"][0]
        expedition._facility_effect(match, 0, station, {"op": "stabilize_terrain", "amount": 4})
        self.assertTrue(match["stabilized"])
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "jomon.json"
            save_game(self.state, path)
            resumed = load_game(path)
        self.assertEqual(resumed.tabletop["active_match"], match)

    def test_redacted_shared_deck_survives_save_roundtrip(self):
        match = expedition.start_match(self.state, patrons(self.state)[0].id)
        expedition._facility_effect(match, 0, match["facilities"][0], {"op": "remove_random", "amount": 1})
        self.assertEqual(len(match["teams"][0]["deck"]), 19)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "jomon.json"
            save_game(self.state, path)
            resumed = load_game(path)
        self.assertEqual(resumed.tabletop["active_match"], match)

    def test_one_enter_auto_walks_remaining_route_orders(self):
        match = expedition.start_match(self.state, patrons(self.state)[0].id)
        match["pickups"] = []
        match["hazards"] = []
        match["facilities"] = []
        match["stations"] = []
        with patch("curses.curs_set"), patch("curses.has_colors", return_value=False):
            ui = ExpeditionUI(type("Screen", (), {"getmaxyx": lambda self: (24, 80),
                                                 "keypad": lambda self, enabled: None})(), self.state)
            with patch.object(ui, "_render_map"), patch("curses.napms"):
                ui._auto_walk_to(tuple(match["files"][1]["home"]))
        self.assertEqual(match["teams"][0]["orders"], expedition.ORDERS_PER_TURN)
        self.assertNotEqual(match["teams"][0]["position"], match["files"][0]["home"])

    def test_format_eight_discards_board_game_only(self):
        original = self.state.to_dict()
        original["save_format"] = 8
        original["tabletop"] = {"collections": {"old": "board"}, "records": ["old"], "active_match": {"version": 1}}
        migrated = game_state_from_dict(original)
        self.assertEqual(migrated.save_format, SAVE_FORMAT)
        self.assertEqual(migrated.region.levels, self.state.region.levels)
        self.assertEqual(migrated.items, self.state.items)
        self.assertEqual(migrated.tabletop, {"collections": {}, "records": [], "active_match": None})

    def test_first_win_reward_is_bounded_per_patron_and_season(self):
        patron = patrons(self.state)[0]
        courier = self.state.courier
        initial_credit = self.state.trade_credit
        initial_relation = patron.relationships.get(courier.id, 0)
        collection_for(self.state, courier.id)
        match = expedition.start_match(self.state, patron.id)
        match["winner"] = 0
        self.assertEqual(expedition.finish_match(self.state), "win")
        self.assertEqual(self.state.trade_credit, initial_credit + 1)
        self.assertEqual(courier.strategy, 1)
        self.assertEqual(patron.relationships[courier.id], min(3, initial_relation + 1))
        match = expedition.start_match(self.state, patron.id)
        match["winner"] = 0
        expedition.finish_match(self.state)
        self.assertEqual(self.state.trade_credit, initial_credit + 1)
        validate_state(self.state)

    def test_tampered_generated_map_is_rejected_at_load(self):
        match = expedition.start_match(self.state, patrons(self.state)[0].id)
        match["board"][17] = " " * 117
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "jomon.json"
            save_game(self.state, path)
            with self.assertRaisesRegex(SaveError, "generated seed"):
                load_game(path)

    def test_keyboard_starts_and_leaves_a_resumable_match_with_map(self):
        class Screen:
            def __init__(self):
                self.keys = iter((13, ord("2"), 13, ord("q")))
                self.drawn = []

            def getmaxyx(self):
                return 24, 80

            def erase(self):
                pass

            def refresh(self):
                pass

            def keypad(self, enabled):
                pass

            def addnstr(self, y, x, value, count, attr=0):
                self.drawn.append(value[:count])

            def addstr(self, y, x, value, attr=0):
                self.drawn.append(value)

            def getch(self):
                return next(self.keys)

        screen = Screen()
        with patch("curses.curs_set"), patch("curses.has_colors", return_value=False), patch("curses.napms"):
            run_expedition(screen, self.state)
        match = self.state.tabletop["active_match"]
        self.assertIsNotNone(match)
        self.assertEqual(len(match["board"][0]), 117)
        self.assertTrue(any("DULLEST DUNGEON" in line for line in screen.drawn))
        self.assertTrue(any("auto-walk" in line for line in screen.drawn))

    def test_ranked_screen_draws_both_parties_and_card_frames(self):
        class Screen:
            def __init__(self):
                self.drawn = []

            def getmaxyx(self):
                return 24, 80

            def erase(self):
                pass

            def refresh(self):
                pass

            def keypad(self, enabled):
                pass

            def addstr(self, y, x, value, attr=0):
                self.drawn.append(value)

            def getch(self):
                return ord("q")

        match = expedition.start_match(self.state, patrons(self.state)[0].id)
        origin = match["teams"][0]["position"]
        match["teams"][1]["position"] = [origin[0] + 2, origin[1]]
        expedition.move_to(match, (origin[0] + 1, origin[1]))
        screen = Screen()
        with patch("curses.curs_set"), patch("curses.has_colors", return_value=False):
            ui = ExpeditionUI(screen, self.state)
            ui._render_combat_match()
            hand_card = match["teams"][0]["hand"][0]
            mini = ui._mini_office_card_lines(hand_card)
            full = ui._full_office_card_lines(hand_card)
            ui._show_card()
        self.assertTrue(any("YOUR PARTY" in line for line in screen.drawn))
        self.assertTrue(any("RIVAL PARTY" in line for line in screen.drawn))
        self.assertTrue(any("+------------+" in line for line in screen.drawn))
        self.assertEqual((len(mini), len(full)), (10, 17))
        self.assertTrue(all(len(line) == 14 for line in mini))
        self.assertTrue(all(len(line) == 22 for line in full))
        catalog = load_catalog()
        role = catalog.cards[hand_card["id"]]["hero"]
        self.assertIn(office_card_glyph(role)[1].strip(), "".join(mini))
        self.assertTrue(any(OFFICE_SPRITES[match["teams"][0]["actors"][0]["role"]][0] in line for line in screen.drawn))
        costume = rival_costumes(match["world_seed"])[0]
        self.assertTrue(any(catalog.art["enemies"][costume][0] in line for line in screen.drawn))

    def test_keyboard_selects_a_ranked_target_and_plays_a_card(self):
        class Screen:
            def __init__(self):
                self.keys = iter((13, 13, ord("q")))

            def getmaxyx(self):
                return 24, 80

            def erase(self):
                pass

            def refresh(self):
                pass

            def keypad(self, enabled):
                pass

            def addstr(self, y, x, value, attr=0):
                pass

            def getch(self):
                return next(self.keys)

        match = expedition.start_match(self.state, patrons(self.state)[0].id)
        match["phase"] = "combat"
        match["teams"][0]["hand"] = [next(card for card in match["teams"][0]["deck"] if card["id"] == "baton_strike")]
        target = match["teams"][1]["actors"][0]
        before = target["hp"]
        with patch("curses.curs_set"), patch("curses.has_colors", return_value=False), patch("curses.napms"):
            ExpeditionUI(Screen(), self.state).run_match()
        self.assertLess(target["hp"], before)
        self.assertIs(self.state.tabletop["active_match"], match)


if __name__ == "__main__":
    unittest.main()
