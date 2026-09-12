"""The tavern's symmetric file-capture expedition on a generated Dullest map."""

from __future__ import annotations

from dataclasses import asdict
from heapq import heappop, heappush
import hashlib
from typing import Any

from .content import load_catalog
from .engine import GameEngine, WALKABLE_TILES
from .passives import persistent_effect
from .office_content import DEPARTMENTS, OFFICE_BIOMES, OFFICE_ROLES, office_catalog, office_facility_option
from .tabletop import collection_for, patrons, shifted_doctrine

MAX_ROUNDS = 18
OVERTIME_ROUNDS = 4
ORDERS_PER_TURN = 4
ORDER_TICKS = 18
MAX_PLAYS_PER_TURN = 16


def _roll(match: dict) -> int:
    value = match["rng"]
    value ^= (value << 13) & ((1 << 64) - 1)
    value ^= value >> 7
    value ^= (value << 17) & ((1 << 64) - 1)
    match["rng"] = value or 1
    return match["rng"]


def _shuffle(match: dict, cards: list[Any]) -> None:
    for index in range(len(cards) - 1, 0, -1):
        other = _roll(match) % (index + 1)
        cards[index], cards[other] = cards[other], cards[index]


def _draw(match: dict, side: int, amount: int) -> None:
    team = match["teams"][side]
    for _ in range(max(0, amount)):
        if len(team["hand"]) >= 8:
            return
        if not team["draw"]:
            team["draw"] = team["discard"]
            team["discard"] = []
            _shuffle(match, team["draw"])
        if team["draw"]:
            team["hand"].append(team["draw"].pop())


def _log(match: dict, message: str) -> None:
    match["log"].append(message)
    del match["log"][:-12]


def _card_id(card: str | dict) -> str:
    return card if isinstance(card, str) else card["id"]


def _card_upgraded(card: str | dict) -> bool:
    return bool(card.get("upgraded")) if isinstance(card, dict) else False


def _team(match: dict, side: int) -> dict:
    return match["teams"][side]


def _actor(match: dict, identity: str) -> dict:
    for team in match["teams"]:
        for actor in team["actors"]:
            if actor["id"] == identity:
                return actor
    raise ValueError("unknown specialist")


def _living(match: dict, side: int) -> list[dict]:
    return sorted((actor for actor in _team(match, side)["actors"] if actor["hp"] > 0), key=lambda actor: actor["rank"])


def _position(match: dict, side: int) -> tuple[int, int]:
    return tuple(_team(match, side)["position"])


def _distance(a: tuple[int, int], b: tuple[int, int]) -> int:
    return abs(a[0] - b[0]) + abs(a[1] - b[1])


def _terrain_costs() -> dict[str, int]:
    return {terrain["glyph"]: int(terrain["cost"]) for terrain in load_catalog().terrains.values()}


def doctrine_compatible(roles: list[str], doctrine_id: str) -> bool:
    catalog = load_catalog()
    doctrine = catalog.doctrines.get(doctrine_id)
    if doctrine is None or len(roles) != 4 or any(role not in catalog.heroes for role in roles):
        return False
    tags = {tag for role in roles for card in catalog.heroes[role]["starter_deck"]
            for tag in catalog.cards[card]["tags"]}
    combat_roles = {catalog.heroes[role]["combat_role"] for role in roles}
    return set(doctrine["requires_tags"]) <= tags and set(doctrine["requires_roles"]) <= combat_roles


def infusion_compatible(card_id: str, infusion_id: str) -> bool:
    catalog = load_catalog()
    card = catalog.cards.get(card_id)
    infusion = catalog.infusions.get(infusion_id)
    if card is None or infusion is None:
        return False
    targets = set(infusion["compatible_targets"])
    tags = set(infusion["requires_any_tags"])
    return (not targets or card["target"] in targets) and (not tags or bool(tags & set(card["tags"])))


def path_to(match: dict, side: int, destination: tuple[int, int]) -> list[tuple[int, int]]:
    """Use the original expedition's weighted, orthogonal destination routing."""
    board = match["board"]
    x, y = destination
    if not (0 <= y < len(board) and 0 <= x < len(board[y]) and board[y][x] in WALKABLE_TILES):
        return []
    start = _position(match, side)
    costs = _terrain_costs()
    frontier = [(0, *start)]
    best = {start: 0}
    previous: dict[tuple[int, int], tuple[int, int] | None] = {start: None}
    while frontier:
        cost, cx, cy = heappop(frontier)
        if cost != best[(cx, cy)]:
            continue
        if (cx, cy) == destination:
            break
        for nx, ny in ((cx, cy - 1), (cx - 1, cy), (cx + 1, cy), (cx, cy + 1)):
            if not (0 <= ny < len(board) and 0 <= nx < len(board[ny])):
                continue
            glyph = board[ny][nx]
            if glyph not in WALKABLE_TILES:
                continue
            neighbor = nx, ny
            next_cost = cost + costs[glyph]
            if next_cost < best.get(neighbor, 10**9):
                best[neighbor] = next_cost
                previous[neighbor] = cx, cy
                heappush(frontier, (next_cost, nx, ny))
    if destination not in previous:
        return []
    result = []
    current = destination
    while current != start:
        result.append(current)
        current = previous[current]
        assert current is not None
    return result[::-1]


def _biome_at(match: dict, x: int, y: int) -> str:
    glyph = match["board"][y][x]
    for terrain in load_catalog().terrains.values():
        if terrain["glyph"] == glyph and terrain.get("biome"):
            return str(terrain["biome"])
    room = min(range(12), key=lambda index: _distance((x, y), tuple(match["room_positions"][index])))
    return match["room_biomes"][room]


def _team_state(side: int, roles: list[str], deck: list[str], position: list[int],
                customization: dict | None = None) -> dict:
    catalog = load_catalog()
    actors = [{"id": f"{side}:{role}", "role": role, "rank": rank,
               "hp": catalog.heroes[role]["max_hp"], "max_hp": catalog.heroes[role]["max_hp"],
               "stress": 0, "block": 0, "statuses": {}, "guarded_by": None,
               "guard_turns": 0, "respawn": 0}
              for rank, role in enumerate(roles, 1)]
    instances = [{"id": card, "copy_id": index, "upgraded": False}
                 for index, card in enumerate(deck, 1)]
    return {"roles": roles[:], "actors": actors, "position": position[:],
            "deck": instances, "draw": [], "hand": [], "discard": [],
            "next_copy_id": len(instances) + 1,
            "energy": 3, "orders": 0, "plays": 0, "supplies": 4, "light": 100,
            "items": {}, "boons": [], "curses": [], "pending_hand": 0,
            "triggers": {},
            "combat_opened": False,
            "combat_used": {},
            "opening_effects": [],
            "masteries": dict((customization or {}).get("masteries", {})),
            "infusions": dict((customization or {}).get("infusions", {})),
            "doctrine": (customization or {}).get("doctrine", "base:mark_window")}


def new_match(seed: str, courier_id: str, patron_id: str, roles: list[str],
              patron_roles: list[str], deck: list[str] | None = None,
              match_number: int = 0, customization: dict | None = None) -> dict:
    catalog = load_catalog()
    if (len(roles) != 4 or len(patron_roles) != 4 or len(set(roles)) != 4
            or len(set(patron_roles)) != 4 or any(role not in catalog.heroes for role in roles + patron_roles)):
        raise ValueError("each side needs four distinct specialists")
    player_deck = deck or [card for role in roles for card in catalog.heroes[role]["starter_deck"]]
    patron_deck = [card for role in patron_roles for card in catalog.heroes[role]["starter_deck"]]
    if not 20 <= len(player_deck) <= 30 or any(card not in catalog.cards or catalog.cards[card]["hero"] not in roles for card in player_deck):
        raise ValueError("choose a legal 20–30 card deck for the selected party")
    if customization and not doctrine_compatible(roles, customization.get("doctrine", "base:mark_window")):
        raise ValueError("the selected party does not support that company policy")
    if customization and any(not infusion_compatible(card, infusion) for card, infusion in customization.get("infusions", {}).items()):
        raise ValueError("a card treatment is incompatible with its technique")
    digest = hashlib.sha256(f"dullest:expedition:{seed}:{courier_id}:{patron_id}:{match_number}".encode()).digest()
    world_seed = int.from_bytes(digest[:8], "big")
    generated = GameEngine.new(catalog, world_seed, start_in_hub=True).state
    base0, base1 = generated.room_positions[0], generated.room_positions[11]
    available = [doctrine for doctrine in catalog.doctrines if doctrine_compatible(patron_roles, doctrine)]
    patron_doctrine = available[shifted_doctrine(seed, patron_id) % len(available)]
    match = {"version": 2, "courier_id": courier_id, "patron_id": patron_id,
             "world_seed": world_seed, "world_id": generated.world_id,
             "room_positions": generated.room_positions, "room_biomes": [room.biome_id for room in generated.rooms],
             "biome_ids": generated.biome_ids, "board": generated.world_tiles,
             "rng": int.from_bytes(digest[8:16], "big") or 1,
             "round": 1, "turn": 0, "phase": "map", "scores": [0, 0],
             "winner": None, "pending_score": None, "combat_turns": 0,
             "contact_cooldown": 0,
             "teams": [_team_state(0, roles, player_deck, base0, customization),
                       _team_state(1, patron_roles, patron_deck, base1, {"doctrine": patron_doctrine})],
             "files": [{"home": base0[:], "carrier": None, "dropped": None},
                       {"home": base1[:], "carrier": None, "dropped": None}],
             "pickups": [asdict(item) for item in generated.pickups],
             "hazards": [asdict(item) for item in generated.hazards],
             "facilities": [asdict(item) for item in generated.facilities],
             "stations": [{"id": room.id, "kind": room.kind,
                           "x": generated.room_positions[room.id][0],
                           "y": generated.room_positions[room.id][1],
                           "biome_id": room.biome_id, "content_id": room.content_id,
                           "used": False, "outcome": None}
                          for room in generated.rooms if room.kind in {"event", "camp", "upgrade", "cache"}],
             "pending": None,
             "log": ["The Company of Necessary Copies opens a disputed expedition."]}
    return match


def _file_position(match: dict, owner: int) -> tuple[int, int]:
    file = match["files"][owner]
    if file["carrier"] is not None:
        return _position(match, 1 - owner)
    return tuple(file["dropped"] or file["home"])


def _knockout(match: dict, actor: dict) -> None:
    if actor["respawn"]:
        return
    actor["hp"] = 0
    actor["respawn"] = 2
    actor["block"] = 0
    actor["statuses"] = {}
    actor["guarded_by"] = None
    side = int(actor["id"].split(":", 1)[0])
    for rank, survivor in enumerate(_living(match, side), 1):
        survivor["rank"] = rank
    for file in match["files"]:
        if file["carrier"] == actor["id"]:
            file["carrier"] = None
            file["dropped"] = _team(match, side)["position"][:]
            match["pending_score"] = None
            _log(match, f"{OFFICE_ROLES[actor['role']]} drops the stolen file.")
    _log(match, f"{OFFICE_ROLES[actor['role']]} is sent on two-turn mandatory leave.")
    if not _living(match, side):
        if match["phase"] == "combat":
            match["phase"] = "map"
        _team(match, side)["position"] = match["files"][side]["home"][:]
        _log(match, "The depleted party regroups at its records office.")


def _passive(match: dict, side: int, owner: str | None, group: str, key: str) -> int:
    team = _team(match, side)
    catalog = getattr(load_catalog(), group)
    if group == "items":
        counts = team["items"]
    else:
        from collections import Counter

        counts = Counter(entry["id"] for entry in team[group] if entry["owner"] == owner)
    return sum(persistent_effect(effect).value(count)
               for identity, count in counts.items()
               for effect in catalog[identity]["effects"] if effect["key"] == key)


def _damage(match: dict, actor: dict, target: dict, amount: int) -> None:
    side = int(target["id"].split(":", 1)[0])
    attacker_side = int(actor["id"].split(":", 1)[0])
    guard = target["guarded_by"]
    if guard and guard != target["id"]:
        protector = _actor(match, guard)
        if protector["hp"] > 0:
            target = protector
    if target["statuses"].get("dodge"):
        target["statuses"].pop("dodge")
        return
    if actor["statuses"].get("weak"):
        amount = round(amount * .75)
    if actor["statuses"].get("focus"):
        amount = round(amount * 1.25)
    if target["statuses"].get("vulnerable"):
        amount = round(amount * 1.5)
    if attacker_side != side and target["statuses"].get("marked"):
        amount = round(amount * (1 + (_passive(match, attacker_side, actor["id"], "boons", "marked_damage_bonus")
                                     + _passive(match, attacker_side, None, "items", "marked_damage_bonus")) / 10000))
    amount = round(amount * (1 + _passive(match, side, target["id"], "curses", "incoming_damage_bonus") / 10000))
    amount = round(amount * (1 - _passive(match, side, None, "items", "incoming_damage_reduction") / 10000))
    amount = max(0, amount)
    absorbed = min(target["block"], max(0, amount))
    target["block"] -= absorbed
    target["hp"] = max(0, target["hp"] - max(0, amount - absorbed))
    if target["hp"] == 0:
        second_wind = _passive(match, side, target["id"], "boons", "second_wind")
        used = _team(match, side)["combat_used"]
        if second_wind and not used.get(f"second_wind:{target['id']}"):
            used[f"second_wind:{target['id']}"] = True
            target["hp"] = min(target["max_hp"], second_wind)
            _log(match, f"{OFFICE_ROLES[target['role']]} invokes a second-wind allowance.")
        else:
            _knockout(match, target)
    elif target["statuses"].get("riposte") and side != int(actor["id"].split(":", 1)[0]):
        actor["hp"] = max(0, actor["hp"] - 3)
        if actor["hp"] == 0:
            _knockout(match, actor)


def _apply_effect(match: dict, actor: dict, targets: list[dict], effect: dict, reference: dict | None = None) -> None:
    side = int(actor["id"].split(":", 1)[0])
    op, amount = effect["op"], int(effect.get("amount", 0))
    if op == "draw":
        _draw(match, side, amount)
        return
    if op == "discard":
        team = _team(match, side)
        for _ in range(min(amount, len(team["hand"]))):
            team["discard"].append(team["hand"].pop())
        return
    if op == "energy":
        team = _team(match, side)
        team["energy"] = max(0, min(9, team["energy"] + amount))
        return
    for target in targets:
        if target["hp"] <= 0:
            continue
        condition = effect.get("condition_status")
        checked = reference or target
        if condition and not checked["statuses"].get(condition):
            continue
        actor_state = effect.get("condition_actor_state")
        target_state = effect.get("condition_target_state")
        def has_state(piece: dict, state: str | None) -> bool:
            return (state is None or state == "healthy" and piece["hp"] == piece["max_hp"]
                    or state == "wounded" and piece["hp"] < piece["max_hp"]
                    or state == "stressed" and piece["stress"] >= 50
                    or state == "deaths_door" and piece["hp"] <= max(3, piece["max_hp"] // 5))
        if not has_state(actor, actor_state) or not has_state(checked, target_state):
            continue
        if op == "damage":
            bonus = int(effect.get("bonus", 0)) if effect.get("bonus_status") in target["statuses"] else 0
            _damage(match, actor, target, amount + bonus)
        elif op == "block":
            target["block"] = min(99, target["block"] + max(0, amount))
        elif op == "heal":
            bonus = _passive(match, side, actor["id"], "boons", "healing_bonus")
            penalty = _passive(match, int(target["id"].split(":", 1)[0]), target["id"], "curses", "healing_reduction")
            target["hp"] = min(target["max_hp"], target["hp"] + max(0, round(amount * (1 + bonus / 10000) * (1 - penalty / 10000))))
        elif op == "stress":
            target_side = int(target["id"].split(":", 1)[0])
            modifier = (_passive(match, target_side, target["id"], "curses", "stress_bonus")
                        - _passive(match, target_side, target["id"], "boons", "stress_reduction")
                        - _passive(match, target_side, None, "items", "stress_reduction")) if amount > 0 else 0
            target["stress"] = max(0, min(100, target["stress"] + round(amount * (1 + modifier / 10000))))
            if target["stress"] >= 100:
                _knockout(match, target)
        elif op == "move":
            party = _living(match, int(target["id"].split(":", 1)[0]))
            for _ in range(abs(amount)):
                next_rank = target["rank"] + (1 if amount > 0 else -1)
                if not 1 <= next_rank <= len(party):
                    break
                other = next((member for member in party if member["rank"] == next_rank), None)
                if other:
                    other["rank"] = target["rank"]
                target["rank"] = next_rank
        elif op == "guard" and target["id"] != actor["id"]:
            target["guarded_by"] = actor["id"]
            target["guard_turns"] = max(1, amount)
        elif op == "status":
            status = effect["status"]
            target["statuses"][status] = max(target["statuses"].get(status, 0), max(1, amount))
        elif op == "cleanse":
            for status in ("marked", "stun", "vulnerable", "weak", "wound"):
                target["statuses"].pop(status, None)


def valid_targets(match: dict, card_index: int) -> list[str]:
    if match["phase"] != "combat" or match["winner"] is not None:
        return []
    side = match["turn"]
    team = _team(match, side)
    if not 0 <= card_index < len(team["hand"]):
        return []
    definition = load_catalog().cards[_card_id(team["hand"][card_index])]
    actor = next(item for item in team["actors"] if item["role"] == definition["hero"])
    ranks = set(definition["from_ranks"])
    infusion = load_catalog().infusions.get(team["infusions"].get(definition["id"]))
    if team["masteries"].get(definition["id"]) == "coverage":
        ranks |= {rank + offset for rank in definition["from_ranks"] for offset in (-1, 1) if 1 <= rank + offset <= 4}
    if infusion and infusion["mode"] == "rank_access":
        ranks = {1, 2, 3, 4}
    if actor["hp"] <= 0 or actor["statuses"].get("stun") or actor["rank"] not in ranks:
        return []
    target = definition["target"]
    if target == "self":
        return [actor["id"]]
    if target == "ally":
        return [item["id"] for item in _living(match, side)]
    if target == "all_allies":
        return ["all_allies"]
    if target == "all_enemies":
        return ["all_enemies"] if _living(match, 1 - side) else []
    return [item["id"] for item in _living(match, 1 - side) if item["rank"] in definition["target_ranks"]]


def _card_cost(match: dict, side: int, card: str | dict) -> int:
    catalog = load_catalog()
    team = _team(match, side)
    card_id = _card_id(card)
    definition = catalog.cards[card_id]
    actor = next(item for item in team["actors"] if item["role"] == definition["hero"])
    cost = int(definition.get("upgrade_cost", definition["cost"]) if _card_upgraded(card) else definition["cost"])
    infusion_id = team["infusions"].get(card_id)
    infusion = catalog.infusions.get(infusion_id)
    if infusion:
        mode = infusion["mode"]
        if (mode == "front_discount" and actor["rank"] == 1
                or mode == "rear_discount" and actor["rank"] == 4
                or mode == "wounded_discount" and actor["statuses"].get("wound")
                or mode == "casualty_discount" and len(_living(match, side)) < 4):
            cost -= int(infusion["amount"])
    mode = catalog.doctrines[team["doctrine"]]["mode"]
    ops = {effect["op"] for effect in definition["effects"]}
    if mode == "dance" and "move" not in ops and not team["triggers"].get("dance_liability"):
        cost += 1
    elif mode == "guard" and "move" in ops:
        cost += 1
    elif mode == "death_door":
        if actor["hp"] <= max(3, actor["max_hp"] // 5):
            cost -= 1
        elif len(_living(match, side)) == 4 and all(item["hp"] == item["max_hp"] for item in _living(match, side)) and not team["triggers"].get("death_door_tax"):
            cost += 1
    elif mode == "casualty" and len(_living(match, side)) == 4 and not team["triggers"].get("casualty_tax"):
        cost += 1
    elif mode == "triage" and "damage" in ops and team["triggers"].get("triage_tax"):
        cost += 1
    return max(0, cost)


def play_card(match: dict, card_index: int, target_id: str | None = None) -> str:
    if match["phase"] != "combat" or match["winner"] is not None:
        raise ValueError("cards can only be played during the rival party encounter")
    side = match["turn"]
    team = _team(match, side)
    if team["plays"] >= MAX_PLAYS_PER_TURN or not 0 <= card_index < len(team["hand"]):
        raise ValueError("choose a card in the current hand")
    instance = team["hand"][card_index]
    card_id = _card_id(instance)
    definition = load_catalog().cards[card_id]
    actor = next(item for item in team["actors"] if item["role"] == definition["hero"])
    targets = valid_targets(match, card_index)
    target_id = target_id or (targets[0] if len(targets) == 1 else None)
    if target_id not in targets:
        raise ValueError("choose a legal ranked target")
    cost = _card_cost(match, side, instance)
    if cost > team["energy"]:
        raise ValueError("not enough energy")
    team["energy"] -= cost
    team["hand"].pop(card_index)
    infusion = load_catalog().infusions.get(team["infusions"].get(card_id))
    if not infusion or infusion["mode"] != "exhaust":
        team["discard"].append(instance)
    main = (_living(match, side) if target_id == "all_allies" else
            _living(match, 1 - side) if target_id == "all_enemies" else [_actor(match, target_id)])
    effects = [dict(effect) for effect in (definition["upgrade_effects"] if _card_upgraded(instance) else definition["effects"])]
    doctrine = load_catalog().doctrines[team["doctrine"]]["mode"]
    if infusion and infusion["mode"] == "pressure_bonus" and match["files"][1 - side]["carrier"] == actor["id"]:
        first = next((effect for effect in effects if effect["op"] in {"damage", "block", "heal"}), None)
        if first is not None:
            first["amount"] += int(infusion["amount"])
    branch_id = team["masteries"].get(card_id)
    mastery = next((item for item in load_catalog().masteries.values() if item["card_id"] == card_id), None)
    if mastery and branch_id:
        branch = next((item for item in mastery["branches"] if item["id"] == branch_id), None)
        if branch and branch["mode"] == "effect_bonus":
            effects[branch["effect_index"]]["amount"] += branch["amount"]
    if definition.get("biome") == _biome_at(match, *_position(match, side)):
        for effect in effects:
            if effect["op"] in {"damage", "block", "heal"}:
                effect["amount"] += int(definition.get("biome_bonus", 0))
    for effect in effects:
        if effect["op"] == "damage":
            if doctrine == "control" or doctrine == "mark" and effect.get("bonus_status") != "marked" or doctrine == "artillery" and definition["target"] != "all_enemies":
                effect["amount"] = max(0, effect["amount"] - 1)
            elif doctrine == "artillery" and definition["target"] == "all_enemies":
                effect["amount"] += 2
        elif effect["op"] == "heal" and doctrine == "wound":
            effect["amount"] = max(0, effect["amount"] - 1)
        elif effect["op"] == "stress" and effect["amount"] < 0 and doctrine == "stress":
            effect["amount"] = min(0, effect["amount"] + 2)
        elif effect["op"] == "block" and doctrine == "guard" and any(item["guarded_by"] for item in _living(match, side)):
            effect["amount"] += 2
        elif effect["op"] == "status" and effect.get("status") in {"stun", "weak"} and doctrine == "control" and not team["triggers"].get("control"):
            effect["amount"] += 1
            team["triggers"]["control"] = True
        elif effect["op"] == "status" and effect.get("status") == "marked" and doctrine == "mark" and not team["triggers"].get("mark"):
            effect["amount"] += 1
            team["triggers"]["mark"] = True
        elif effect["op"] == "status" and effect.get("status") == "wound" and doctrine == "wound":
            effect["amount"] += 1
    for effect in effects:
        kind = effect.get("target", definition["target"])
        recipients = ([actor] if kind == "self" else _living(match, side) if kind == "all_allies"
                      else _living(match, 1 - side) if kind == "all_enemies" else main)
        _apply_effect(match, actor, recipients, effect, main[0] if kind == "self" and main else None)
    if infusion:
        mode = infusion["mode"]
        if mode == "echo_first" and effects:
            _apply_effect(match, actor, main, effects[0])
        elif mode == "follow_draw":
            _draw(match, side, int(infusion["amount"]))
        elif mode == "movement_refund" and any(effect["op"] == "move" for effect in effects):
            team["energy"] += int(infusion["amount"])
        elif mode == "self_cleanse":
            _apply_effect(match, actor, [actor], {"op": "cleanse", "amount": 1})
        elif mode == "mark_after_damage" and any(effect["op"] == "damage" for effect in effects):
            for recipient in main:
                if recipient["hp"] > 0:
                    recipient["statuses"]["marked"] = max(int(infusion["amount"]), recipient["statuses"].get("marked", 0))
        elif mode == "front_focus" and actor["rank"] == 1:
            actor["statuses"]["focus"] = max(actor["statuses"].get("focus", 0), int(infusion["amount"]))
        elif mode == "wound_transfer" and main:
            source = next((item for item in main if item["id"] != actor["id"] and item["statuses"].get("wound")), None)
            if source is not None:
                source["statuses"]["wound"] = max(0, source["statuses"]["wound"] - int(infusion["amount"]))
                if not source["statuses"]["wound"]:
                    del source["statuses"]["wound"]
                actor["statuses"]["wound"] = max(int(infusion["amount"]), actor["statuses"].get("wound", 0))
    ops = {effect["op"] for effect in effects}
    if doctrine == "dance":
        if "move" in ops and not team["triggers"].get("dance"):
            _draw(match, side, 1)
            team["triggers"]["dance"] = True
        elif "move" not in ops:
            team["triggers"]["dance_liability"] = True
    elif doctrine == "discard" and "discard" in ops and not team["triggers"].get("discard"):
        _draw(match, side, 1)
        team["triggers"]["discard"] = True
    elif doctrine == "triage" and "heal" in ops and not team["triggers"].get("triage"):
        team["energy"] = min(9, team["energy"] + 1)
        team["triggers"]["triage"] = True
        team["triggers"]["triage_tax"] = True
    elif doctrine == "triage" and "damage" in ops:
        team["triggers"].pop("triage_tax", None)
    elif doctrine == "stress" and any(effect["op"] == "stress" and effect["amount"] > 0 for effect in effects) and not team["triggers"].get("stress"):
        actor["statuses"]["focus"] = max(1, actor["statuses"].get("focus", 0))
        team["triggers"]["stress"] = True
    if doctrine == "casualty" and len(_living(match, side)) == 4:
        team["triggers"]["casualty_tax"] = True
    if doctrine == "death_door":
        team["triggers"]["death_door_tax"] = True
    team["plays"] += 1
    name = office_catalog()[1][card_id].name
    _log(match, f"{OFFICE_ROLES[actor['role']]} files {name}.")
    return name


def _hazard(match: dict, side: int, hazard: dict) -> None:
    team = _team(match, side)
    definition = load_catalog().biomes[hazard["biome_id"]]["mechanics"]["hazard"]
    op, amount = definition["effect"], int(definition["amount"])
    living = _living(match, side)
    if op == "damage_all":
        for actor in living:
            _damage(match, actor, actor, amount)
    elif op == "damage_weakest" and living:
        target = min(living, key=lambda actor: actor["hp"] / actor["max_hp"])
        _damage(match, target, target, amount)
    elif op == "stress_highest" and living:
        target = max(living, key=lambda actor: actor["stress"])
        target["stress"] = min(100, target["stress"] + amount)
        if target["stress"] >= 100:
            _knockout(match, target)
    elif op == "light":
        team["light"] = max(0, min(100, team["light"] + amount))
    elif op == "supplies":
        team["supplies"] = max(0, team["supplies"] + amount)
    elif op in {"status_all", "status_random"}:
        targets = living if op == "status_all" else ([living[_roll(match) % len(living)]] if living else [])
        for actor in targets:
            status = definition["status"]
            actor["statuses"][status] = max(actor["statuses"].get(status, 0), amount)
    elif op == "wound_injured":
        for actor in living:
            if actor["hp"] < actor["max_hp"]:
                actor["statuses"]["wound"] = max(actor["statuses"].get("wound", 0), amount)
    elif op == "opening_hand":
        team["pending_hand"] += amount
    _log(match, f"A {OFFICE_BIOMES[hazard['biome_id']]} policy hazard catches the {('courier', 'patron')[side]} party.")


def _draft(match: dict, side: int) -> None:
    catalog = load_catalog()
    roles = _team(match, side)["roles"]
    candidates = sorted(card_id for card_id, card in catalog.cards.items() if card["hero"] in roles)
    picks = []
    for _ in range(3):
        choice = candidates[_roll(match) % len(candidates)]
        if choice not in picks:
            picks.append(choice)
    match["pending"] = {"side": side, "kind": "draft", "choices": picks}


def choose_reward(match: dict, choice: int) -> None:
    pending = match["pending"]
    if not pending or pending["side"] != match["turn"]:
        raise ValueError("there is no reward choice")
    if pending["kind"] in {"facility", "camp", "upgrade", "event"} and choice == len(pending["choices"]):
        match["pending"] = None
        _log(match, "The party leaves the neutral room untouched.")
        return
    if not 0 <= choice < len(pending["choices"]):
        raise ValueError("choose one of the offered rewards")
    side = match["turn"]
    if pending["kind"] == "draft":
        card = pending["choices"][choice]
        team = _team(match, side)
        instance = {"id": card, "copy_id": team["next_copy_id"], "upgraded": False}
        team["next_copy_id"] += 1
        team["deck"].append(instance)
        team["discard"].append(instance)
        _log(match, f"{office_catalog()[1][card].name} joins the {('courier', 'patron')[side]} deck.")
    elif pending["kind"] == "boon":
        boon = pending["choices"][choice]
        match["pending"] = {"side": side, "kind": "recipient", "boon": boon,
                            "choices": [actor["id"] for actor in _living(match, side)]}
        return
    elif pending["kind"] == "recipient":
        owner = pending["choices"][choice]
        _team(match, side)["boons"].append({"id": pending["boon"], "owner": owner})
        _log(match, f"{OFFICE_ROLES[_actor(match, owner)['role']]} receives a company perk.")
        _draft(match, side)
        return
    elif pending["kind"] == "facility":
        facility = next(item for item in match["facilities"] if item["id"] == pending["facility"])
        option = load_catalog().facilities[facility["definition_id"]]["options"][choice]
        cost = option["cost"]
        if cost["resource"] in {"supplies", "light"} and _team(match, side)[cost["resource"]] < cost["amount"]:
            raise ValueError(f"not enough {cost['resource']} for that facility option")
        if cost["resource"] == "supplies":
            _team(match, side)["supplies"] -= cost["amount"]
        elif cost["resource"] == "light":
            _team(match, side)["light"] -= cost["amount"]
        elif cost["resource"] == "stress_all":
            for actor in _living(match, side):
                actor["stress"] = min(100, actor["stress"] + cost["amount"])
                if actor["stress"] >= 100:
                    _knockout(match, actor)
        elif cost["resource"] == "health_all":
            for actor in _living(match, side):
                _damage(match, actor, actor, cost["amount"])
        for effect in option["effects"]:
            _facility_effect(match, side, facility, effect)
        facility["used"] = True
        facility["outcome"] = option["id"]
        _log(match, f"{office_facility_option({effect['op'] for effect in option['effects']}, option['cost'])} is complete.")
    elif pending["kind"] == "camp":
        station = next(item for item in match["stations"] if item["id"] == pending["station"])
        team = _team(match, side)
        if choice == 0:
            for actor in _living(match, side):
                actor["hp"] = min(actor["max_hp"], actor["hp"] + 7)
                actor["stress"] = max(0, actor["stress"] - 10)
            station["used"] = True
            station["outcome"] = "recover"
            _log(match, "The party rests: +7 HP and -10 stress each.")
        else:
            if team["supplies"] < 2 or not team["curses"]:
                raise ValueError("treatment needs a liability and two supplies")
            choices = [f"{OFFICE_ROLES[_actor(match, entry['owner'])['role']]} — liability {index + 1}"
                       for index, entry in enumerate(team["curses"])]
            match["pending"] = {"side": side, "kind": "treatment", "station": station["id"], "choices": choices}
            return
    elif pending["kind"] == "treatment":
        station = next(item for item in match["stations"] if item["id"] == pending["station"])
        team = _team(match, side)
        if team["supplies"] < 2:
            raise ValueError("treatment needs two supplies")
        team["supplies"] -= 2
        team["curses"].pop(choice)
        station["used"] = True
        station["outcome"] = "treat"
        _log(match, "The party treats one liability.")
    elif pending["kind"] == "upgrade":
        station = next(item for item in match["stations"] if item["id"] == pending["station"])
        copy_id = pending["copy_ids"][choice]
        card = next(item for item in _team(match, side)["deck"] if item["copy_id"] == copy_id)
        card["upgraded"] = True
        for zone in ("draw", "hand", "discard"):
            for instance in _team(match, side)[zone]:
                if instance["copy_id"] == copy_id:
                    instance["upgraded"] = True
        station["used"] = True
        station["outcome"] = "upgrade"
        _log(match, f"{office_catalog()[1][card['id']].name}+ is authorized by the workshop.")
    elif pending["kind"] == "event":
        station = next(item for item in match["stations"] if item["id"] == pending["station"])
        option = load_catalog().events[station["content_id"]]["choices"][choice]
        cost = int(option.get("cost_supplies", 0))
        team = _team(match, side)
        if team["supplies"] < cost:
            raise ValueError("not enough supplies for that event choice")
        team["supplies"] -= cost
        reward = False
        for effect in option["effects"]:
            if effect["op"] == "card_reward":
                reward = True
            else:
                _facility_effect(match, side, station, effect)
        station["used"] = True
        station["outcome"] = str(choice)
        _log(match, "The department incident is resolved.")
        if reward:
            _draft(match, side)
            return
    match["pending"] = None


def _facility_effect(match: dict, side: int, facility: dict, effect: dict) -> None:
    team = _team(match, side)
    op, amount = effect["op"], int(effect["amount"])
    living = _living(match, side)
    if op == "suppress_hazard":
        candidates = [hazard for hazard in match["hazards"] if hazard["biome_id"] == facility["biome_id"] and hazard["active"]]
        if candidates:
            nearest = min(candidates, key=lambda hazard: _distance((facility["x"], facility["y"]), (hazard["x"], hazard["y"])))
            nearest["active"] = False
            nearest["suppressed_by"] = facility["id"]
    elif op == "light":
        team["light"] = max(0, min(100, team["light"] + amount))
    elif op == "supplies":
        team["supplies"] = max(0, team["supplies"] + amount)
    elif op == "stress_all":
        for actor in living:
            actor["stress"] = max(0, min(100, actor["stress"] + amount))
            if actor["stress"] >= 100:
                _knockout(match, actor)
    elif op == "heal_all":
        for actor in living:
            actor["hp"] = min(actor["max_hp"], actor["hp"] + amount)
    elif op == "heal_weakest" and living:
        actor = min(living, key=lambda item: item["hp"] / item["max_hp"])
        actor["hp"] = min(actor["max_hp"], actor["hp"] + amount)
    elif op == "status_all":
        for actor in living:
            actor["statuses"][effect["status"]] = max(actor["statuses"].get(effect["status"], 0), amount)
    elif op == "cleanse_all":
        for actor in living:
            actor["statuses"].clear()
    elif op == "item_random":
        item = sorted(load_catalog().items)[_roll(match) % len(load_catalog().items)]
        team["items"][item] = team["items"].get(item, 0) + 1
    elif op == "remove_random" and team["curses"]:
        team["curses"].pop(_roll(match) % len(team["curses"]))
    elif op == "stabilize_terrain":
        # terrain remains visible; the facility clears nearby hazard pressure instead.
        for hazard in match["hazards"]:
            if _distance((facility["x"], facility["y"]), (hazard["x"], hazard["y"])) <= amount:
                hazard["active"] = False
    # reveal_biome and patrol-only operations are informational in this PvP mode.


def _arrival(match: dict, side: int) -> None:
    position = _position(match, side)
    for owner, file in enumerate(match["files"]):
        if file["carrier"] is not None or position != _file_position(match, owner):
            continue
        if owner == side and file["dropped"] is not None:
            file["dropped"] = None
            _log(match, "A misplaced file returns to its home tray.")
        elif owner != side and _living(match, side):
            carrier = _living(match, side)[0]
            file["carrier"] = carrier["id"]
            file["dropped"] = None
            _log(match, f"{OFFICE_ROLES[carrier['role']]} steals the rival file.")
    for hazard in match["hazards"]:
        if not hazard["active"] or list(position) not in hazard["cells"] or list(position) in hazard["triggered_cells"]:
            continue
        hazard["triggered_cells"].append(list(position))
        if len(hazard["triggered_cells"]) >= len(hazard["cells"]):
            hazard["active"] = False
            hazard["triggered"] = True
        _hazard(match, side, hazard)
        if not _living(match, side):
            return
    for pickup in match["pickups"]:
        if pickup["resolved"] or position != (pickup["x"], pickup["y"]):
            continue
        pickup["resolved"] = True
        kind = pickup["kind"]
        team = _team(match, side)
        if kind == "item":
            item = pickup["payload"]["item_id"]
            team["items"][item] = team["items"].get(item, 0) + 1
            _log(match, "The party salvages company-issued equipment.")
        elif kind == "trap":
            if _living(match, side):
                victim = _living(match, side)[_roll(match) % len(_living(match, side))]
                _damage(match, victim, victim, 5)
            _log(match, "A concealed administrative trap springs.")
        else:
            if kind == "boon" and _living(match, side):
                choices = sorted(load_catalog().boons)
                offered = []
                while len(offered) < min(3, len(choices)):
                    boon = choices[_roll(match) % len(choices)]
                    if boon not in offered:
                        offered.append(boon)
                match["pending"] = {"side": side, "kind": "boon", "choices": offered}
                _log(match, "The party finds three company perks; choose one and a recipient.")
                break
            if kind == "bargain":
                traits = sorted(curse_id for curse_id, definition in load_catalog().curses.items() if definition["kind"] == "trait")
                curse = traits[_roll(match) % len(traits)]
                owner = _living(match, side)[0]["id"] if _living(match, side) else team["actors"][0]["id"]
                team["curses"].append({"id": curse, "owner": owner})
            _draft(match, side)
            _log(match, "The party finds three office techniques; choose one.")
        break
    if match["pending"] is None:
        for facility in match["facilities"]:
            if not facility["used"] and position == (facility["x"], facility["y"]):
                definition = load_catalog().facilities[facility["definition_id"]]
                match["pending"] = {"side": side, "kind": "facility", "facility": facility["id"],
                                    "choices": [office_facility_option({effect['op'] for effect in option['effects']}, option['cost'])
                                                for option in definition["options"]]}
                _log(match, f"{OFFICE_BIOMES[facility['biome_id']]} service desk offers two procedures.")
                break
    if match["pending"] is None:
        for station in match["stations"]:
            if station["used"] or position != (station["x"], station["y"]):
                continue
            team = _team(match, side)
            if station["kind"] == "cache":
                team["supplies"] += 2
                team["light"] = min(100, team["light"] + 20)
                station["used"] = True
                station["outcome"] = "salvage"
                _log(match, "Emergency office stores yield two supplies and twenty light.")
            elif station["kind"] == "camp":
                match["pending"] = {"side": side, "kind": "camp", "station": station["id"],
                                    "choices": ["Recover all (+7 HP, -10 stress)", "Treat one liability (2 supplies)"]}
            elif station["kind"] == "upgrade":
                candidates = [card for card in team["deck"] if not card["upgraded"]]
                if candidates:
                    match["pending"] = {"side": side, "kind": "upgrade", "station": station["id"],
                                        "copy_ids": [card["copy_id"] for card in candidates],
                                        "choices": [f"{office_catalog()[1][card['id']].name} — copy {card['copy_id']}"
                                                    for card in candidates]}
            else:
                definition = load_catalog().events[station["content_id"]]
                labels = []
                for option in definition["choices"]:
                    cost = {"resource": "supplies" if option.get("cost_supplies") else "none",
                            "amount": int(option.get("cost_supplies", 0))}
                    labels.append(office_facility_option({effect["op"] for effect in option["effects"]}, cost))
                match["pending"] = {"side": side, "kind": "event", "station": station["id"], "choices": labels}
            break


def move_to(match: dict, destination: tuple[int, int]) -> list[tuple[int, int]]:
    if match["winner"] is not None or match["phase"] != "map":
        raise ValueError("the party cannot navigate right now")
    if match["pending"]:
        raise ValueError("resolve the discovered reward first")
    side = match["turn"]
    team = _team(match, side)
    if team["orders"] >= ORDERS_PER_TURN or not _living(match, side):
        raise ValueError("the party has no route orders remaining")
    path = path_to(match, side, destination)
    if not path:
        raise ValueError("choose a reachable floor destination")
    costs = _terrain_costs()
    total = sum(costs[match["board"][y][x]] for x, y in path)
    if total > ORDER_TICKS:
        raise ValueError(f"destination costs {total} ticks; an order reaches at most {ORDER_TICKS}")
    team["orders"] += 1
    walked = []
    for x, y in path:
        rival = _position(match, 1 - side)
        if (not match["contact_cooldown"] and _living(match, 1 - side)
                and _distance((x, y), rival) <= 1):
            team["position"] = [x, y]
            walked.append((x, y))
            engage_if_touching(match)
            break
        team["position"] = [x, y]
        walked.append((x, y))
        _arrival(match, side)
        if match["pending"] or match["winner"] is not None or not _living(match, side):
            break
    return walked


def engage_if_touching(match: dict) -> bool:
    if (match["phase"] != "map" or match["winner"] is not None or match["pending"] or match["contact_cooldown"]
            or not _living(match, 0) or not _living(match, 1)
            or _distance(_position(match, 0), _position(match, 1)) > 1):
        return False
    match["phase"] = "combat"
    _begin_combat(match)
    _log(match, "The rival parties make contact. Ranked card combat begins.")
    return True


def _begin_combat(match: dict) -> None:
    initiator = match["turn"]
    biome = _biome_at(match, *_position(match, initiator))
    environment = load_catalog().biomes[biome]["mechanics"]["combat"]
    for side in (0, 1):
        team = _team(match, side)
        team["draw"] = team["deck"][:]
        _shuffle(match, team["draw"])
        prioritized = [card for card in team["draw"] if (infusion := load_catalog().infusions.get(team["infusions"].get(_card_id(card)))) and infusion["mode"] == "opening_priority"]
        if prioritized:
            for card in prioritized:
                team["draw"].remove(card)
            team["draw"].extend(reversed(prioritized))
        team["discard"] = []
        team["hand"] = []
        team["plays"] = 0
        team["combat_opened"] = False
        team["combat_used"] = {}
        team["opening_effects"] = []
    for effect in environment["effects"]:
        target = effect["target"]
        sides = (0, 1) if target == "all" else (initiator,) if target in {"crew", "front_crew", "back_crew"} else (1 - initiator,)
        for side in sides:
            _team(match, side)["opening_effects"].append(dict(effect))
    match["combat_turns"] = 0
    _start_turn(match, match["turn"])


def _score_check(match: dict, side: int) -> None:
    rival = match["files"][1 - side]
    own = match["files"][side]
    carrier_id = rival["carrier"]
    eligible = (carrier_id is not None and own["carrier"] is None and own["dropped"] is None
                and _actor(match, carrier_id)["hp"] > 0
                and _distance(_position(match, side), tuple(own["home"])) <= 2)
    if match["pending_score"] == side:
        match["pending_score"] = None
        if eligible:
            match["scores"][side] += 1
            rival["carrier"] = None
            rival["dropped"] = None
            _log(match, f"{('Courier', 'Patron')[side]} earns a signed approval point.")
            if match["scores"][side] >= 2:
                match["winner"] = side
    elif eligible:
        match["pending_score"] = side
        _log(match, "The stolen file awaits approval through the rival's next turn.")


def _start_turn(match: dict, side: int) -> None:
    team = _team(match, side)
    first_combat_turn = match["phase"] == "combat" and not team["combat_opened"]
    team["energy"] = int(load_catalog().balance["energy"])
    if first_combat_turn:
        team["energy"] += _passive(match, side, None, "items", "first_round_energy")
    team["orders"] = 0
    team["plays"] = 0
    team["triggers"] = {}
    for actor in team["actors"]:
        if actor["respawn"]:
            actor["respawn"] -= 1
            if actor["respawn"] == 0:
                actor["hp"] = actor["max_hp"]
                actor["stress"] = 0
                actor["statuses"] = {}
                actor["block"] = 0
                actor["rank"] = len(_living(match, side))
                _log(match, f"{OFFICE_ROLES[actor['role']]} returns from mandatory leave.")
        elif actor["hp"] > 0:
            actor["block"] = 0
            if actor["statuses"].get("wound"):
                _damage(match, actor, actor, 2)
        if actor["guard_turns"]:
            actor["guard_turns"] -= 1
            if actor["guard_turns"] == 0:
                actor["guarded_by"] = None
        if first_combat_turn and actor["hp"] > 0:
            actor["block"] += (_passive(match, side, actor["id"], "boons", "start_block")
                               + _passive(match, side, None, "items", "stacked_start_block"))
            for status, group, key in (("dodge", "boons", "start_dodge"),
                                       ("focus", "items", "start_focus"),
                                       ("marked", "curses", "start_marked"),
                                       ("vulnerable", "curses", "start_vulnerable")):
                value = _passive(match, side, actor["id"] if group != "items" else None, group, key)
                if value:
                    actor["statuses"][status] = max(actor["statuses"].get(status, 0), value)
            actor["stress"] = max(0, actor["stress"] - _passive(match, side, actor["id"], "boons", "start_stress_relief"))
    if match["phase"] == "combat":
        opening = 5 + team["pending_hand"] + (_passive(match, side, None, "items", "opening_hand") if first_combat_turn else 0)
        if not team["combat_opened"] and load_catalog().doctrines[team["doctrine"]]["mode"] == "discard":
            opening -= 1
        if first_combat_turn:
            for effect in team["opening_effects"]:
                op, amount = effect["op"], int(effect["amount"])
                if op == "draw":
                    opening += amount
                    continue
                if op == "energy":
                    team["energy"] = max(0, team["energy"] + amount)
                    continue
                living = _living(match, side)
                target = effect["target"]
                targets = living[:1] if target.startswith("front_") else living[-1:] if target.startswith("back_") else living
                if op == "reverse":
                    for actor in living:
                        actor["rank"] = len(living) + 1 - actor["rank"]
                else:
                    for actor in targets:
                        _apply_effect(match, actor, [actor], effect)
            team["opening_effects"] = []
        _draw(match, side, opening)
        team["pending_hand"] = 0
        team["combat_opened"] = True


def end_turn(match: dict) -> None:
    if match["winner"] is not None:
        return
    if match["pending"]:
        raise ValueError("choose a reward or facility procedure before ending the turn")
    side = match["turn"]
    team = _team(match, side)
    if match["phase"] == "combat":
        retained = [card for card in team["hand"] if (infusion := load_catalog().infusions.get(team["infusions"].get(_card_id(card)))) and infusion["mode"] == "retain"]
        team["discard"].extend(card for card in team["hand"] if card not in retained)
        team["hand"] = retained
        match["combat_turns"] += 1
        if match["combat_turns"] >= 2:
            match["phase"] = "map"
            match["contact_cooldown"] = 2
            for combat_team in match["teams"]:
                combat_team["discard"].extend(combat_team["hand"])
                combat_team["hand"] = []
            _log(match, "The filing clash ends; both parties return to their routes.")
    elif match["contact_cooldown"]:
        match["contact_cooldown"] -= 1
    for actor in team["actors"]:
        if actor["hp"] > 0:
            for status in list(actor["statuses"]):
                actor["statuses"][status] -= 1
                if actor["statuses"][status] <= 0:
                    del actor["statuses"][status]
    _score_check(match, side)
    if match["winner"] is not None:
        return
    match["turn"] = 1 - side
    if match["pending_score"] == match["turn"]:
        _score_check(match, match["turn"])
        if match["winner"] is not None:
            return
    if match["turn"] == 0:
        match["round"] += 1
        if match["round"] > MAX_ROUNDS + OVERTIME_ROUNDS:
            a, b = match["scores"]
            match["winner"] = 0 if a > b else 1 if b > a else "draw"
            return
        if match["round"] > MAX_ROUNDS and match["scores"][0] != match["scores"][1]:
            match["winner"] = 0 if match["scores"][0] > match["scores"][1] else 1
            return
    _start_turn(match, match["turn"])


def _ai_destination(match: dict, goal: tuple[int, int]) -> tuple[int, int] | None:
    side = match["turn"]
    path = path_to(match, side, goal)
    if not path:
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            path = path_to(match, side, (goal[0] + dx, goal[1] + dy))
            if path:
                break
    cost = 0
    reachable = None
    costs = _terrain_costs()
    for x, y in path:
        step = costs[match["board"][y][x]]
        if cost + step > ORDER_TICKS:
            break
        cost += step
        reachable = x, y
    return reachable


def patron_turn(match: dict) -> None:
    if match["turn"] != 1 or match["winner"] is not None:
        raise ValueError("the patron has no turn")
    engage_if_touching(match)
    def clear_pending() -> None:
        pending = match["pending"]
        if pending is None:
            return
        if pending["kind"] == "facility":
            facility = next(item for item in match["facilities"] if item["id"] == pending["facility"])
            options = load_catalog().facilities[facility["definition_id"]]["options"]
            choices = [index for index, option in enumerate(options)
                       if option["cost"]["resource"] not in {"supplies", "light"}
                       or _team(match, 1)[option["cost"]["resource"]] >= option["cost"]["amount"]]
            if not choices:
                facility["used"] = True
                facility["outcome"] = "unavailable"
                match["pending"] = None
                return
            choose_reward(match, choices[0])
        elif pending["kind"] == "event":
            station = next(item for item in match["stations"] if item["id"] == pending["station"])
            options = load_catalog().events[station["content_id"]]["choices"]
            choices = [index for index, option in enumerate(options)
                       if _team(match, 1)["supplies"] >= int(option.get("cost_supplies", 0))]
            choose_reward(match, choices[0] if choices else len(pending["choices"]))
        else:
            choose_reward(match, 0)
    while match["pending"]:
        clear_pending()
    engage_if_touching(match)
    if match["phase"] == "map":
        for _ in range(ORDERS_PER_TURN):
            if match["phase"] != "map" or match["pending"] or not _living(match, 1):
                break
            own_file = match["files"][1]
            carrying = match["files"][0]["carrier"] is not None
            goal = (_file_position(match, 1) if own_file["carrier"] or own_file["dropped"]
                    else tuple(own_file["home"]) if carrying else _file_position(match, 0))
            destination = _ai_destination(match, goal)
            if destination is None:
                break
            try:
                move_to(match, destination)
            except ValueError:
                break
            while match["pending"]:
                clear_pending()
    if match["phase"] == "combat":
        for _ in range(MAX_PLAYS_PER_TURN):
            if match["winner"] is not None or _team(match, 1)["energy"] <= 0:
                break
            choices = []
            for index, card in enumerate(_team(match, 1)["hand"]):
                targets = valid_targets(match, index)
                if targets and _card_cost(match, 1, card) <= _team(match, 1)["energy"]:
                    definition = load_catalog().cards[_card_id(card)]
                    priority = int(any(effect["op"] == "damage" for effect in definition["effects"])) * 3
                    priority += int(any(effect["op"] == "heal" for effect in definition["effects"]))
                    choices.append((priority, -index, index, targets))
            if not choices:
                break
            _, _, index, targets = max(choices)
            play_card(match, index, targets[0])
            if match["phase"] != "combat":
                break
    if match["winner"] is None:
        end_turn(match)


def start_match(state: Any, patron_id: str) -> dict:
    from jomon.actions import _advance_world
    from jomon.calendar import calendar_at

    if state.location != "jomon" or state.jomon_space != "tavern" or state.courier is None or not state.courier.alive:
        raise ValueError("a living courier must be at the tavern")
    if state.tabletop["active_match"] is not None:
        raise ValueError("finish the current expedition first")
    patron = next((person for person in patrons(state) if person.id == patron_id), None)
    if patron is None:
        raise ValueError("that patron is not available in the tavern")
    collection = collection_for(state, state.courier.id)
    roles = collection["roles"]
    roster = list(load_catalog().heroes)
    shift = int.from_bytes(hashlib.sha256(patron_id.encode()).digest()[:2], "big") % len(roster)
    patron_roles = [roster[(shift + index) % len(roster)] for index in range(4)]
    match = new_match(state.seed, state.courier.id, patron_id, roles, patron_roles,
                      collection["deck"], len(state.tabletop["records"]), collection)
    match["patron_name"] = patron.name
    _advance_world(state)
    date = calendar_at(state)
    match["season"] = f"{date.year}:{date.season}"
    state.tabletop["active_match"] = match
    return match


def finish_match(state: Any) -> str:
    match = state.tabletop["active_match"]
    if not match or match["winner"] is None:
        raise ValueError("the expedition has not ended")
    result = "win" if match["winner"] == 0 else "draw" if match["winner"] == "draw" else "loss"
    collection = collection_for(state, match["courier_id"])
    collection[{"win": "wins", "loss": "losses", "draw": "draws"}[result]] += 1
    first_win = result == "win" and not any(
        row["courier"] == match["courier_id"] and row["patron"] == match["patron_id"]
        and row["season"] == match["season"] and row["result"] == "win"
        for row in state.tabletop["records"])
    if first_win:
        state.trade_credit += 1
        patron = next((person for person in [*state.household, *state.visitors, state.bartender] if person.id == match["patron_id"]), None)
        if patron:
            patron.relationships[match["courier_id"]] = min(3, patron.relationships.get(match["courier_id"], 0) + 1)
        courier = next((person for person in state.household if person.id == match["courier_id"]), None)
        if courier:
            courier.strategy = min(20, courier.strategy + 1)
    state.tabletop["records"].append({"courier": match["courier_id"], "patron": match["patron_id"],
                                      "season": match["season"], "result": result,
                                      "score": match["scores"][:],
                                      "department": DEPARTMENTS[list(load_catalog().worlds).index(match["world_id"]) % len(DEPARTMENTS)]})
    state.tabletop["active_match"] = None
    return result


def validate_expedition(state: Any) -> None:
    from .tabletop import validate_tabletop

    match = state.tabletop.get("active_match") if isinstance(state.tabletop, dict) else None
    original = state.tabletop
    state.tabletop = {**original, "active_match": None} if isinstance(original, dict) else original
    try:
        validate_tabletop(state)
    finally:
        state.tabletop = original
    if match is None:
        return
    if not isinstance(match, dict) or match.get("version") != 2:
        raise ValueError("invalid competitive expedition version")
    if match.get("courier_id") not in {person.id for person in state.household} or not isinstance(match.get("patron_id"), str):
        raise ValueError("competitive expedition participants are invalid")
    if match.get("turn") not in (0, 1) or match.get("phase") not in ("map", "combat") or match.get("winner") not in (None, 0, 1, "draw"):
        raise ValueError("competitive expedition turn is invalid")
    if (type(match.get("rng")) is not int or not 1 <= match["rng"] < 2**64
            or type(match.get("contact_cooldown")) is not int or not 0 <= match["contact_cooldown"] <= 2
            or type(match.get("combat_turns")) is not int or not 0 <= match["combat_turns"] <= 2
            or match.get("pending_score") not in (None, 0, 1)
            or not isinstance(match.get("patron_name"), str) or not match["patron_name"]
            or not isinstance(match.get("season"), str)):
        raise ValueError("competitive expedition counters or identity are invalid")
    if type(match.get("round")) is not int or not 1 <= match["round"] <= MAX_ROUNDS + OVERTIME_ROUNDS + 1:
        raise ValueError("competitive expedition round is invalid")
    if not isinstance(match.get("scores"), list) or len(match["scores"]) != 2 or any(type(score) is not int or not 0 <= score <= 2 for score in match["scores"]):
        raise ValueError("competitive expedition score is invalid")
    if len(match.get("teams", [])) != 2 or len(match.get("files", [])) != 2:
        raise ValueError("competitive expedition parties or files are invalid")
    generated = GameEngine.new(load_catalog(), match["world_seed"], start_in_hub=True).state
    if (match["world_id"] != generated.world_id or match["board"] != generated.world_tiles
            or match["room_positions"] != generated.room_positions or match["biome_ids"] != generated.biome_ids):
        raise ValueError("competitive expedition map differs from its generated seed")
    from collections import Counter

    for side, team in enumerate(match["teams"]):
        if len(team["actors"]) != 4 or len({actor["role"] for actor in team["actors"]}) != 4:
            raise ValueError("competitive expedition party is invalid")
        if team["roles"] != [actor["role"] for actor in team["actors"]]:
            raise ValueError("competitive expedition roster differs from its specialists")
        if any(actor["role"] not in load_catalog().heroes or actor["id"] != f"{side}:{actor['role']}"
               or type(actor["hp"]) is not int or not 0 <= actor["hp"] <= actor["max_hp"]
               or actor["max_hp"] != load_catalog().heroes[actor["role"]]["max_hp"]
               or type(actor["respawn"]) is not int or not 0 <= actor["respawn"] <= 2
               for actor in team["actors"]):
            raise ValueError("competitive expedition specialist is invalid")
        if (sorted(actor["rank"] for actor in team["actors"] if actor["hp"] > 0) != list(range(1, len(_living(match, side)) + 1))
                or any(type(actor["stress"]) is not int or not 0 <= actor["stress"] <= 100
                       or type(actor["block"]) is not int or not 0 <= actor["block"] <= 99
                       or not isinstance(actor["statuses"], dict)
                       or any(status not in {"marked", "wound", "weak", "vulnerable", "focus", "riposte", "dodge", "stun"}
                              or type(value) is not int or not 0 <= value <= 9
                              for status, value in actor["statuses"].items())
                       for actor in team["actors"])):
            raise ValueError("competitive expedition ranks or statuses are invalid")
        cards = team["deck"] + team["draw"] + team["hand"] + team["discard"]
        if any(not isinstance(card, dict) or set(card) != {"id", "copy_id", "upgraded"}
               or card["id"] not in load_catalog().cards
               or type(card["copy_id"]) is not int or card["copy_id"] < 1
               or type(card["upgraded"]) is not bool for card in cards):
            raise ValueError("competitive expedition card copies are invalid")
        durable = {card["copy_id"]: card for card in team["deck"]}
        zones = team["draw"] + team["hand"] + team["discard"]
        if (len(durable) != len(team["deck"])
                or any(durable.get(card["copy_id"]) != card for card in zones)
                or type(team["next_copy_id"]) is not int or team["next_copy_id"] <= max(durable, default=0)
                or team["next_copy_id"] > 1000):
            raise ValueError("competitive expedition card identity is invalid")
        if (not 20 <= len(team["deck"]) <= 60 or any(load_catalog().cards[card["id"]]["hero"] not in team["roles"] for card in team["deck"])
                or not (Counter(card["copy_id"] for card in zones) <= Counter(card["copy_id"] for card in team["deck"]))
                or type(team["energy"]) is not int or not 0 <= team["energy"] <= 9
                or type(team["orders"]) is not int or not 0 <= team["orders"] <= ORDERS_PER_TURN
                or type(team["plays"]) is not int or not 0 <= team["plays"] <= MAX_PLAYS_PER_TURN
                or type(team["light"]) is not int or not 0 <= team["light"] <= 100
                or type(team["supplies"]) is not int or team["supplies"] < 0
                or not doctrine_compatible(team["roles"], team["doctrine"])
                or any(not infusion_compatible(card, infusion) for card, infusion in team["infusions"].items())):
            raise ValueError("competitive expedition deck or resources are invalid")
        actor_ids = {actor["id"] for actor in team["actors"]}
        if (not isinstance(team["items"], dict)
                or any(item not in load_catalog().items or type(count) is not int or not 0 < count <= 99
                       for item, count in team["items"].items())
                or any(not isinstance(entry, dict) or set(entry) != {"id", "owner"}
                       or entry["id"] not in getattr(load_catalog(), group)
                       or entry["owner"] not in actor_ids
                       for group in ("boons", "curses") for entry in team[group])
                or not isinstance(team["triggers"], dict) or not isinstance(team["combat_used"], dict)
                or type(team["combat_opened"]) is not bool or not isinstance(team["opening_effects"], list)):
            raise ValueError("competitive expedition persistent effects are invalid")
        x, y = team["position"]
        if not (0 <= y < len(match["board"]) and 0 <= x < len(match["board"][y]) and match["board"][y][x] in WALKABLE_TILES):
            raise ValueError("competitive expedition party is off-map")
    for owner, file in enumerate(match["files"]):
        if file["home"] != generated.room_positions[0 if owner == 0 else 11]:
            raise ValueError("competitive expedition file home is invalid")
        if file["carrier"] is not None and file["carrier"] not in {actor["id"] for actor in match["teams"][1 - owner]["actors"] if actor["hp"] > 0}:
            raise ValueError("competitive expedition file carrier is invalid")
        if file["dropped"] is not None:
            if file["carrier"] is not None or not isinstance(file["dropped"], list) or len(file["dropped"]) != 2:
                raise ValueError("competitive expedition dropped file is invalid")
            x, y = file["dropped"]
            if not (0 <= y < len(match["board"]) and 0 <= x < len(match["board"][y]) and match["board"][y][x] in WALKABLE_TILES):
                raise ValueError("competitive expedition dropped file is off-map")
    for key, source in (("pickups", generated.pickups), ("hazards", generated.hazards), ("facilities", generated.facilities)):
        expected = {item.id: asdict(item) for item in source}
        current = match.get(key)
        if not isinstance(current, list) or {item.get("id") for item in current if isinstance(item, dict)} != set(expected) or len(current) != len(expected):
            raise ValueError(f"competitive expedition {key} differ from the generated map")
        for item in current:
            baseline = expected[item["id"]]
            fixed = set(baseline) - {"resolved", "triggered", "triggered_cells", "active", "suppressed_by", "used", "outcome"}
            if any(item.get(field) != baseline[field] for field in fixed):
                raise ValueError(f"competitive expedition {key} differ from the generated map")
            if key == "hazards" and any(cell not in baseline["cells"] for cell in item["triggered_cells"]):
                raise ValueError("competitive expedition hazard activation is invalid")
            if key == "pickups" and type(item.get("resolved")) is not bool:
                raise ValueError("competitive expedition pickup resolution is invalid")
            if key == "facilities" and (type(item.get("used")) is not bool or item.get("outcome") not in (None, "unavailable", *(option["id"] for option in load_catalog().facilities[item["definition_id"]]["options"]))):
                raise ValueError("competitive expedition facility outcome is invalid")
    expected_stations = {
        room.id: {"id": room.id, "kind": room.kind,
                  "x": generated.room_positions[room.id][0], "y": generated.room_positions[room.id][1],
                  "biome_id": room.biome_id, "content_id": room.content_id}
        for room in generated.rooms if room.kind in {"event", "camp", "upgrade", "cache"}
    }
    stations = match.get("stations")
    if not isinstance(stations, list) or len(stations) != len(expected_stations):
        raise ValueError("competitive expedition neutral rooms differ from the generated map")
    for station in stations:
        if not isinstance(station, dict) or station.get("id") not in expected_stations:
            raise ValueError("competitive expedition neutral room is invalid")
        if any(station.get(key) != value for key, value in expected_stations[station["id"]].items()):
            raise ValueError("competitive expedition neutral room differs from the generated map")
        allowed = {"event": {"0", "1"}, "camp": {"recover", "treat"},
                   "upgrade": {"upgrade"}, "cache": {"salvage"}}[station["kind"]]
        if (type(station.get("used")) is not bool or station.get("outcome") not in (allowed if station["used"] else {None})):
            raise ValueError("competitive expedition neutral room outcome is invalid")
    pending = match.get("pending")
    if pending is not None:
        if not isinstance(pending, dict) or pending.get("side") != match["turn"] or pending.get("kind") not in {"draft", "facility", "boon", "recipient", "camp", "treatment", "upgrade", "event"}:
            raise ValueError("competitive expedition reward choice is invalid")
        choices = pending.get("choices")
        if not isinstance(choices, list) or not 1 <= len(choices) <= 60 or any(not isinstance(choice, str) for choice in choices) or len(set(choices)) != len(choices):
            raise ValueError("competitive expedition reward offer is invalid")
        if pending["kind"] == "draft" and any(card not in load_catalog().cards or load_catalog().cards[card]["hero"] not in match["teams"][match["turn"]]["roles"] for card in choices):
            raise ValueError("competitive expedition card offer is invalid")
        if pending["kind"] == "boon" and any(boon not in load_catalog().boons for boon in choices):
            raise ValueError("competitive expedition perk offer is invalid")
        if pending["kind"] == "recipient" and (pending.get("boon") not in load_catalog().boons or choices != [actor["id"] for actor in _living(match, match["turn"]) ]):
            raise ValueError("competitive expedition perk recipient is invalid")
        if pending["kind"] == "facility":
            facility = next((item for item in match["facilities"] if item["id"] == pending.get("facility") and not item["used"]), None)
            if facility is None:
                raise ValueError("competitive expedition facility offer is invalid")
            options = load_catalog().facilities[facility["definition_id"]]["options"]
            expected = [office_facility_option({effect["op"] for effect in option["effects"]}, option["cost"])
                        for option in options]
            if choices != expected:
                raise ValueError("competitive expedition facility choices are invalid")
        if pending["kind"] in {"camp", "treatment", "upgrade", "event"}:
            station = next((item for item in stations if item["id"] == pending.get("station") and not item["used"]), None)
            if station is None or station["kind"] != ("camp" if pending["kind"] == "treatment" else pending["kind"]):
                raise ValueError("competitive expedition neutral room offer is invalid")
            if pending["kind"] == "camp" and choices != ["Recover all (+7 HP, -10 stress)", "Treat one liability (2 supplies)"]:
                raise ValueError("competitive expedition rest offer is invalid")
            if pending["kind"] == "treatment":
                expected = [f"{OFFICE_ROLES[_actor(match, entry['owner'])['role']]} — liability {index + 1}"
                            for index, entry in enumerate(_team(match, match["turn"])["curses"])]
                if choices != expected:
                    raise ValueError("competitive expedition treatment offer is invalid")
            if pending["kind"] == "upgrade":
                candidates = [card for card in _team(match, match["turn"])["deck"] if not card["upgraded"]]
                if (pending.get("copy_ids") != [card["copy_id"] for card in candidates]
                        or choices != [f"{office_catalog()[1][card['id']].name} — copy {card['copy_id']}" for card in candidates]):
                    raise ValueError("competitive expedition workshop offer is invalid")
            if pending["kind"] == "event":
                definition = load_catalog().events[station["content_id"]]
                expected = [office_facility_option({effect["op"] for effect in option["effects"]},
                                                   {"resource": "supplies" if option.get("cost_supplies") else "none",
                                                    "amount": int(option.get("cost_supplies", 0))})
                            for option in definition["choices"]]
                if choices != expected:
                    raise ValueError("competitive expedition incident offer is invalid")
    if not isinstance(match.get("log"), list) or len(match["log"]) > 12 or any(not isinstance(entry, str) for entry in match["log"]):
        raise ValueError("competitive expedition log is invalid")
