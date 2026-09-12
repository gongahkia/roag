"""Deterministic 1v1 office-board game played at Jomon's tavern table."""

from __future__ import annotations

from collections import deque
import hashlib
from typing import Any

from .content import load_catalog
from .office_content import DEPARTMENTS, office_catalog
from .resolution import EventQueue, Payload
from .triggers import EventType

WIDTH, HEIGHT = 41, 15
MAX_ROUNDS, OVERTIME_ROUNDS = 18, 4
FALLBACK = "commute"


def _roll(match: dict) -> int:
    # Persisting one integer is enough to resume every shuffle and pickup exactly.
    value = match["rng"]
    value ^= (value << 13) & ((1 << 64) - 1)
    value ^= value >> 7
    value ^= (value << 17) & ((1 << 64) - 1)
    match["rng"] = value or 1
    return match["rng"]


def _shuffle(match: dict, items: list[str]) -> None:
    for index in range(len(items) - 1, 0, -1):
        other = _roll(match) % (index + 1)
        items[index], items[other] = items[other], items[index]


def _board(layout: int) -> list[str]:
    grid = [["." for _ in range(WIDTH)] for _ in range(HEIGHT)]
    for x in range(WIDTH):
        grid[0][x] = grid[-1][x] = "#"
    for y in range(HEIGHT):
        grid[y][0] = grid[y][-1] = "#"
    # Six mirrored furniture plans. Row 7 is always an open route between files.
    for x in range(4, 20):
        for y in range(2, 13):
            if y == 7 or x in {7, 8, 9, 17} or y in {4, 10}:
                continue
            if ((x * (layout + 3) + y * (layout + 5) + x * y) % (7 + layout % 3)) in {0, 1}:
                grid[y][x] = grid[y][WIDTH - 1 - x] = "#"
    return ["".join(row) for row in grid]


def _pieces(roles: list[str], side: int) -> list[dict]:
    catalog, _ = office_catalog()
    x = 4 if side == 0 else WIDTH - 5
    return [
        {"id": f"{side}:{index}", "side": side, "role": role, "x": x,
         "y": 5 + index, "hp": catalog[role]["max_hp"],
         "max_hp": catalog[role]["max_hp"], "block": 0,
         "guard": False, "statuses": {}, "respawn": 0}
        for index, role in enumerate(roles)
    ]


def _side_deck(roles: list[str]) -> list[str]:
    catalog, _ = office_catalog()
    return [card for role in roles for card in catalog[role]["starter_deck"]]


def _draw(match: dict, side: int, count: int) -> None:
    hand, draw, discard = (match["sides"][side][key] for key in ("hand", "draw", "discard"))
    for _ in range(max(0, count)):
        if len(hand) >= 8:
            break
        if not draw:
            draw.extend(discard)
            discard.clear()
            _shuffle(match, draw)
        if draw:
            hand.append(draw.pop())


def new_match(seed: str, courier_id: str, patron_id: str, roles: list[str],
              patron_roles: list[str], deck: list[str] | None = None,
              match_number: int = 0, customization: dict | None = None) -> dict:
    role_catalog, card_catalog = office_catalog()
    if len(roles) != 4 or len(patron_roles) != 4 or any(role not in role_catalog for role in roles + patron_roles):
        raise ValueError("each side needs four known office workers")
    player_deck = deck or _side_deck(roles)
    if not 20 <= len(player_deck) <= 30 or any(card not in card_catalog or card_catalog[card].role not in roles for card in player_deck):
        raise ValueError("a legal deck has 20–30 cards for its four workers")
    patron_deck = _side_deck(patron_roles)
    digest = hashlib.sha256(f"dullest:{seed}:{courier_id}:{patron_id}:{match_number}".encode()).digest()
    rng = int.from_bytes(digest[:8], "big") or 1
    layout, theme = rng % 6, (rng // 6) % len(DEPARTMENTS)
    match = {
        "version": 1, "courier_id": courier_id, "patron_id": patron_id,
        "layout": layout, "department": DEPARTMENTS[theme], "board": _board(layout),
        "rng": rng, "round": 1, "turn": 0, "energy": 3,
        "scores": [0, 0], "winner": None, "pending_score": None,
        "pieces": _pieces(roles, 0) + _pieces(patron_roles, 1),
        "files": [{"home": [12, 7], "carrier": None, "dropped": None},
                  {"home": [WIDTH - 13, 7], "carrier": None, "dropped": None}],
        "pickups": [{"x": 16, "y": 7, "kind": "coffee"},
                    {"x": WIDTH - 17, "y": 7, "kind": "coffee"},
                    {"x": 20, "y": 3, "kind": "staples"},
                    {"x": 20, "y": 11, "kind": "staples"}],
        "sides": [{"roles": roles, "draw": player_deck[:], "hand": [], "discard": [],
                   "masteries": dict((customization or {}).get("masteries", {})),
                   "infusions": dict((customization or {}).get("infusions", {})),
                   "doctrine": (customization or {}).get("doctrine", "base:rolling_dance"), "plays": 0},
                  {"roles": patron_roles, "draw": patron_deck[:], "hand": [], "discard": [],
                   "masteries": {}, "infusions": {},
                   "doctrine": list(load_catalog().doctrines)[shifted_doctrine(seed, patron_id)], "plays": 0}],
        "queue": EventQueue().snapshot(), "log": [f"The {DEPARTMENTS[theme]} department opens for business."],
    }
    for side in (0, 1):
        _shuffle(match, match["sides"][side]["draw"])
        _draw(match, side, 5)
    return match


def shifted_doctrine(seed: str, patron_id: str) -> int:
    count = len(load_catalog().doctrines)
    return int.from_bytes(hashlib.sha256(f"{seed}:{patron_id}:doctrine".encode()).digest()[:2], "big") % count


def _piece(match: dict, identity: str) -> dict:
    return next(piece for piece in match["pieces"] if piece["id"] == identity)


def _distance(a: tuple[int, int], b: tuple[int, int]) -> int:
    return abs(a[0] - b[0]) + abs(a[1] - b[1])


def _path(match: dict, start: tuple[int, int], goal: tuple[int, int], actor_id: str) -> list[tuple[int, int]]:
    occupied = {(piece["x"], piece["y"]) for piece in match["pieces"] if piece["hp"] > 0 and piece["id"] != actor_id}
    frontier = deque([start])
    prior = {start: None}
    while frontier:
        point = frontier.popleft()
        if point == goal:
            break
        for dx, dy in ((1, 0), (0, 1), (-1, 0), (0, -1)):
            neighbor = point[0] + dx, point[1] + dy
            x, y = neighbor
            if neighbor not in prior and 0 <= x < WIDTH and 0 <= y < HEIGHT and match["board"][y][x] != "#" and neighbor not in occupied:
                prior[neighbor] = point
                frontier.append(neighbor)
    if goal not in prior:
        return []
    result = []
    while goal != start:
        result.append(goal)
        goal = prior[goal]
    return result[::-1]


def _file_position(match: dict, side: int) -> tuple[int, int]:
    file = match["files"][side]
    if file["carrier"]:
        piece = _piece(match, file["carrier"])
        return piece["x"], piece["y"]
    return tuple(file["dropped"] or file["home"])


def _log(match: dict, message: str) -> None:
    match["log"].append(message)
    del match["log"][:-8]


def _after_move(match: dict, actor: dict) -> None:
    position = actor["x"], actor["y"]
    for index, file in enumerate(match["files"]):
        if position != _file_position(match, index) or file["carrier"]:
            continue
        if index == actor["side"] and file["dropped"]:
            file["dropped"] = None
            _log(match, "A misplaced file returns to its tray.")
        elif index != actor["side"]:
            file["carrier"] = actor["id"]
            file["dropped"] = None
            _log(match, f"{actor['role']} steals the rival file.")
    for pickup in match["pickups"][:]:
        if position == (pickup["x"], pickup["y"]):
            if pickup["kind"] == "coffee":
                match["energy"] = min(5, match["energy"] + 1)
            else:
                actor["block"] += 3
                _draw(match, actor["side"], 1)
            match["pickups"].remove(pickup)
            _log(match, f"{actor['role']} collects {pickup['kind']}.")


def _knockout(match: dict, target: dict) -> None:
    target["hp"] = 0
    target["respawn"] = 2
    target["block"] = 0
    for file in match["files"]:
        if file["carrier"] == target["id"]:
            file["carrier"] = None
            file["dropped"] = [target["x"], target["y"]]
    _log(match, f"{target['role']} is sent to mandatory leave; the file drops.")


def _apply_effect(match: dict, actor: dict, target: dict, destination: tuple[int, int] | None, effect: dict) -> None:
    op, amount = effect["op"], effect.get("amount", 0)
    doctrine = load_catalog().doctrines[match["sides"][actor["side"]]["doctrine"]]["mode"]
    condition = effect.get("condition_status")
    if condition and not target["statuses"].get(condition):
        return
    actor_state = effect.get("condition_actor_state")
    if actor_state == "healthy" and actor["hp"] < actor["max_hp"]:
        return
    if actor_state == "wounded" and actor["hp"] == actor["max_hp"]:
        return
    if op == "damage" and target["hp"] > 0:
        if target["guard"] is False:
            protector = next((piece for piece in match["pieces"] if piece["side"] == target["side"] and piece["id"] != target["id"] and piece["guard"] and piece["hp"] > 0 and _distance((piece["x"], piece["y"]), (target["x"], target["y"])) <= 2), None)
            if protector:
                target = protector
        amount += effect.get("bonus", 0) if effect.get("bonus_status") in target["statuses"] else 0
        amount = max(0, amount + (2 if actor["statuses"].get("focus") else 0) - (2 if actor["statuses"].get("weak") else 0) + (2 if target["statuses"].get("vulnerable") else 0) + (2 if doctrine == "mark" and target["statuses"].get("marked") else 0) + (1 if doctrine == "artillery" else 0))
        if target["statuses"].get("dodge"):
            target["statuses"]["dodge"] -= 1
            amount = 0
        blocked = min(amount, target["block"])
        amount -= blocked
        target["block"] -= blocked
        target["hp"] = max(0, target["hp"] - amount)
        if target["hp"] == 0:
            _knockout(match, target)
        elif amount and target["statuses"].get("riposte") and target["id"] != actor["id"] and actor["hp"] > 0:
            actor["hp"] = max(0, actor["hp"] - 3)
            if actor["hp"] == 0:
                _knockout(match, actor)
    elif op == "block":
        target["block"] = min(30, target["block"] + amount + (1 if doctrine == "guard" else 0))
    elif op == "heal":
        target["hp"] = min(target["max_hp"], target["hp"] + amount + (2 if doctrine == "triage" else 0))
    elif op == "stress":
        target["statuses"]["stress"] = min(9, target["statuses"].get("stress", 0) + amount)
        if target["statuses"]["stress"] >= 9:
            _knockout(match, target)
    elif op == "move":
        mover = target
        if mover["hp"] > 0:
            if mover["side"] == actor["side"] and destination:
                path = _path(match, (mover["x"], mover["y"]), destination, mover["id"])
                if path:
                    mover["x"], mover["y"] = path[min(len(path), max(1, min(3, abs(amount)))) - 1]
                    _after_move(match, mover)
            else:
                dx = (1 if mover["x"] >= actor["x"] else -1) * (1 if amount > 0 else -1)
                for _ in range(max(1, min(3, abs(amount)))):
                    next_point = mover["x"] + dx, mover["y"]
                    if not _path(match, (mover["x"], mover["y"]), next_point, mover["id"]):
                        break
                    mover["x"], mover["y"] = next_point
                    _after_move(match, mover)
    elif op == "guard":
        target["guard"] = True
        target["block"] = min(30, target["block"] + 2)
    elif op == "status":
        status = effect.get("status", "marked")
        target["statuses"][status] = max(1, amount or 1) + (1 if doctrine == "wound" and status == "wound" else 0)
    elif op == "draw":
        _draw(match, actor["side"], amount)
    elif op == "discard":
        hand = match["sides"][actor["side"]]["hand"]
        for _ in range(min(amount, len(hand))):
            match["sides"][actor["side"]]["discard"].append(hand.pop())
    elif op == "energy":
        match["energy"] = min(5, match["energy"] + amount)
    elif op == "cleanse":
        target["statuses"].clear()


def play_card(match: dict, card_index: int, actor_id: str, target_id: str | None = None,
              destination: tuple[int, int] | None = None) -> str:
    """Play a hand card, or index -1 for the always available Commute card."""
    if match["winner"] is not None:
        raise ValueError("the match is over")
    side = match["turn"]
    actor = _piece(match, actor_id)
    if actor["side"] != side or actor["hp"] <= 0:
        raise ValueError("choose an active worker on the current side")
    if actor["statuses"].get("stun", 0) > 0:
        raise ValueError("that worker is stunned for this turn")
    side_state = match["sides"][side]
    hand = side_state["hand"]
    infusion_mode = None
    mastery_mode = None
    if card_index == -1:
        card_id, cost, target_kind, effects = FALLBACK, 1, "self", ({"op": "move", "amount": 1},)
    else:
        _, cards = office_catalog()
        if not 0 <= card_index < len(hand):
            raise ValueError("the card is not in hand")
        card_id = hand[card_index]
        card = cards[card_id]
        if card.role not in match["sides"][side]["roles"]:
            raise ValueError("the worker is not on this team")
        cost, target_kind, effects = card.cost, card.target, card.effects
        source = load_catalog()
        infusion_id = side_state["infusions"].get(card_id)
        infusion_mode = source.infusions[infusion_id]["mode"] if infusion_id else None
        mastery_mode = side_state["masteries"].get(card_id)
        if mastery_mode == "engine":
            mastery = next((item for item in source.masteries.values() if item["card_id"] == card_id), None)
            if mastery:
                branch = mastery["branches"][0]
                effects = tuple({**effect, "amount": effect.get("amount", 0) + (branch["amount"] if index == branch["effect_index"] else 0)} for index, effect in enumerate(effects))
        if infusion_mode == "front_discount" and actor["x"] >= 15 if side == 0 else actor["x"] <= 25:
            cost = max(0, cost - 1)
        if infusion_mode == "rear_discount" and (actor["x"] <= 15 if side == 0 else actor["x"] >= 25):
            cost = max(0, cost - 1)
        if infusion_mode == "wounded_discount" and actor["hp"] < actor["max_hp"]:
            cost = max(0, cost - 1)
        if infusion_mode == "casualty_discount" and any(piece["side"] == side and piece["hp"] == 0 for piece in match["pieces"]):
            cost = max(0, cost - 1)
        if source.doctrines[side_state["doctrine"]]["mode"] == "dance" and side_state["plays"] == 0 and not any(effect["op"] == "move" for effect in effects):
            cost += 1
    if cost > match["energy"]:
        raise ValueError("not enough energy")
    if target_kind in {"enemy", "all_enemies"}:
        enemies = [piece for piece in match["pieces"] if piece["side"] != side and piece["hp"] > 0]
        target = _piece(match, target_id) if target_id else min(enemies, key=lambda item: _distance((actor["x"], actor["y"]), (item["x"], item["y"])))
        reach = 5 + int(mastery_mode == "coverage") + int(infusion_mode == "rank_access")
        if target["side"] == side or target["hp"] <= 0 or _distance((actor["x"], actor["y"]), (target["x"], target["y"])) > reach:
            raise ValueError("select a rival within five desks")
    elif target_kind in {"ally", "all_allies"}:
        target = _piece(match, target_id) if target_id else actor
        reach = 5 + int(mastery_mode == "coverage") + int(infusion_mode == "rank_access")
        if target["side"] != side or target["hp"] <= 0 or _distance((actor["x"], actor["y"]), (target["x"], target["y"])) > reach:
            raise ValueError("select a colleague within five desks")
    else:
        target = actor
    if any(effect["op"] == "move" and effect.get("target", target_kind) not in {"enemy", "all_enemies"} for effect in effects):
        if destination is None:
            raise ValueError("choose a destination for a movement card")
        mover = target if target_kind in {"self", "ally"} else actor
        if not _path(match, (mover["x"], mover["y"]), destination, mover["id"]):
            raise ValueError("the route is blocked")
    match["energy"] -= cost
    if card_index != -1:
        played_card = hand.pop(card_index)
        if infusion_mode != "exhaust":
            side_state["discard"].append(played_card)
    queue = EventQueue.from_snapshot(match["queue"])
    queue.begin(card_token=card_id, combat_token=1, turn_token=(match["round"] - 1) * 2 + side)
    def targets(effect: dict) -> list[dict]:
        kind = effect.get("target", target_kind)
        if kind == "self":
            return [actor]
        if kind == "all_allies":
            return [piece for piece in match["pieces"] if piece["side"] == side and piece["hp"] > 0]
        if kind == "all_enemies":
            return [piece for piece in match["pieces"] if piece["side"] != side and piece["hp"] > 0]
        return [target]

    for index, effect in enumerate(effects):
        for recipient in targets(effect):
            queue.submit(EventType.CARD_STEP, actor_id, (recipient["id"],), Payload(actor_id=actor_id, card_id=card_id, effect_index=index))
    queue.drain(lambda event: (), lambda event, queue: _apply_effect(match, actor, _piece(match, event.target_ids[0]), destination, effects[event.payload.effect_index]), lambda listener, event, queue: None)
    match["queue"] = queue.snapshot()
    if infusion_mode == "echo_first" and side_state["plays"] == 0:
        for effect in effects:
            _apply_effect(match, actor, target, destination, effect)
    if infusion_mode in {"follow_draw", "opening_priority"} and (infusion_mode != "opening_priority" or match["round"] == 1):
        _draw(match, side, 1)
    if infusion_mode == "movement_refund" and any(effect["op"] == "move" for effect in effects):
        match["energy"] = min(5, match["energy"] + 1)
    if infusion_mode == "self_cleanse":
        actor["statuses"].clear()
    if infusion_mode == "point_lead" and actor["x"] in range(15, 26):
        actor["statuses"]["focus"] = max(1, actor["statuses"].get("focus", 0))
    if infusion_mode == "mark_after_damage" and any(effect["op"] == "damage" for effect in effects):
        target["statuses"]["marked"] = max(1, target["statuses"].get("marked", 0))
    if infusion_mode == "burden_thread" and actor["statuses"].pop("wound", None) and target["id"] != actor["id"]:
        target["statuses"]["wound"] = max(1, target["statuses"].get("wound", 0))
    if load_catalog().doctrines[side_state["doctrine"]]["mode"] == "dance" and side_state["plays"] == 0 and any(effect["op"] == "move" for effect in effects):
        _draw(match, side, 1)
    side_state["plays"] += 1
    name = "Commute" if card_id == FALLBACK else office_catalog()[1][card_id].name
    _log(match, f"{actor['role']} files {name}.")
    return name


def _score_check(match: dict, side: int) -> None:
    rival_file = match["files"][1 - side]
    own_file = match["files"][side]
    carrier_id = rival_file["carrier"]
    eligible = bool(carrier_id and own_file["carrier"] is None and own_file["dropped"] is None)
    if eligible:
        carrier = _piece(match, carrier_id)
        eligible = carrier["side"] == side and carrier["hp"] > 0 and _distance((carrier["x"], carrier["y"]), tuple(own_file["home"])) <= 2
    if match["pending_score"] == side:
        match["pending_score"] = None
        if eligible:
            match["scores"][side] += 1
            rival_file["carrier"] = None
            rival_file["dropped"] = None
            _log(match, f"Side {side + 1} receives a signed approval point.")
            if match["scores"][side] >= 2:
                match["winner"] = side
    elif eligible:
        match["pending_score"] = side
        _log(match, "A stolen file awaits approval through the rival's next turn.")


def end_turn(match: dict) -> None:
    if match["winner"] is not None:
        return
    side = match["turn"]
    _score_check(match, side)
    if match["winner"] is not None:
        return
    pile = match["sides"][side]
    retained = [card for card in pile["hand"] if pile["infusions"].get(card) and load_catalog().infusions[pile["infusions"][card]]["mode"] == "retain"]
    pile["discard"].extend(card for card in pile["hand"] if card not in retained)
    pile["hand"] = retained
    for piece in match["pieces"]:
        if piece["side"] == side and piece["hp"] > 0:
            for status in list(piece["statuses"]):
                if status != "stress":
                    piece["statuses"][status] -= 1
                    if piece["statuses"][status] <= 0:
                        del piece["statuses"][status]
    match["turn"] = 1 - side
    if match["pending_score"] == match["turn"]:
        _score_check(match, match["turn"])
        if match["winner"] is not None:
            return
    if match["turn"] == 0:
        match["round"] += 1
        if match["round"] > MAX_ROUNDS + OVERTIME_ROUNDS:
            match["winner"] = 0 if match["scores"][0] > match["scores"][1] else 1 if match["scores"][1] > match["scores"][0] else "draw"
            return
        if match["round"] > MAX_ROUNDS and match["scores"][0] != match["scores"][1]:
            match["winner"] = 0 if match["scores"][0] > match["scores"][1] else 1
            return
    match["energy"] = 3
    match["sides"][match["turn"]]["plays"] = 0
    for piece in match["pieces"]:
        if piece["side"] != match["turn"]:
            continue
        piece["block"] = 0
        piece["guard"] = False
        if piece["hp"] > 0 and piece["statuses"].get("wound"):
            piece["hp"] = max(0, piece["hp"] - min(3, piece["statuses"]["wound"]))
            if piece["hp"] == 0:
                _knockout(match, piece)
        if piece["respawn"]:
            piece["respawn"] -= 1
            if piece["respawn"] == 0:
                piece["hp"] = piece["max_hp"]
                piece["statuses"] = {}
                piece["x"] = 4 if piece["side"] == 0 else WIDTH - 5
                piece["y"] = 5 + int(piece["id"].split(":")[1])
                _log(match, f"{piece['role']} returns from mandatory leave.")
    _draw(match, match["turn"], 5)


def _next_step(match: dict, actor: dict, goal: tuple[int, int]) -> tuple[int, int] | None:
    path = _path(match, (actor["x"], actor["y"]), goal, actor["id"])
    if path:
        return path[min(2, len(path) - 1)]
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        neighbor = goal[0] + dx, goal[1] + dy
        path = _path(match, (actor["x"], actor["y"]), neighbor, actor["id"])
        if path:
            return path[min(2, len(path) - 1)]
    return None


def patron_turn(match: dict) -> None:
    """A named patron uses the same play_card and end_turn rules as the courier."""
    if match["turn"] != 1 or match["winner"] is not None:
        raise ValueError("the patron has no turn")
    _, cards = office_catalog()
    for _ in range(12):
        if match["energy"] <= 0 or match["winner"] is not None:
            break
        actors = [piece for piece in match["pieces"] if piece["side"] == 1 and piece["hp"] > 0]
        if not actors:
            break
        carrier = next((piece for piece in actors if match["files"][0]["carrier"] == piece["id"]), None)
        goal = tuple(match["files"][1]["home"] if carrier else _file_position(match, 0))
        actor = carrier or min(actors, key=lambda item: _distance((item["x"], item["y"]), goal))
        enemies = [piece for piece in match["pieces"] if piece["side"] == 0 and piece["hp"] > 0]
        nearest = min(enemies, key=lambda item: _distance((actor["x"], actor["y"]), (item["x"], item["y"]))) if enemies else None
        move_goal = _next_step(match, actor, goal)
        choices = list(enumerate(match["sides"][1]["hand"]))
        choices.sort(key=lambda pair: (not any(e["op"] == "move" for e in cards[pair[1]].effects), pair[0]))
        played = False
        for index, card_id in choices:
            card = cards[card_id]
            if card.cost > match["energy"]:
                continue
            move = any(effect["op"] == "move" and effect.get("target", card.target) not in {"enemy", "all_enemies"} for effect in card.effects)
            if not move and card.target not in {"enemy", "all_enemies"}:
                continue
            if card.target in {"enemy", "all_enemies"} and (nearest is None or _distance((actor["x"], actor["y"]), (nearest["x"], nearest["y"])) > 5):
                continue
            if move and move_goal is None:
                continue
            try:
                play_card(match, index, actor["id"], nearest["id"] if card.target in {"enemy", "all_enemies"} else None, move_goal if move else None)
                played = True
                break
            except ValueError:
                continue
        if not played:
            if move_goal is None:
                break
            try:
                play_card(match, -1, actor["id"], destination=move_goal)
            except ValueError:
                break
    end_turn(match)


def initial_collection() -> dict:
    roles, catalog = office_catalog()
    cards = {card_id: 1 for card_id in catalog}
    for role in roles.values():
        for card_id in role["starter_deck"]:
            cards[card_id] = max(cards.get(card_id, 0), role["starter_deck"].count(card_id))
    selected = list(roles)[:4]
    return {"roles": selected, "cards": cards, "deck": _side_deck(selected),
            "masteries": {}, "infusions": {}, "doctrine": "base:rolling_dance",
            "wins": 0, "losses": 0, "draws": 0}


def collection_for(state: Any, courier_id: str) -> dict:
    collections = state.tabletop["collections"]
    if courier_id not in collections:
        collections[courier_id] = initial_collection()
    return collections[courier_id]


def patrons(state: Any) -> list[Any]:
    """Adults currently in the tavern may play; no anonymous or remote opponent."""
    people = [*state.household, *state.visitors, state.bartender]
    return [person for person in people if person.id != state.active_courier_id and person.alive and person.available and person.id in state.tavern_positions]


def start_match(state: Any, patron_id: str) -> dict:
    from jomon.actions import _advance_world

    if state.location != "jomon" or state.jomon_space != "tavern" or state.courier is None or not state.courier.alive:
        raise ValueError("a living courier must be at the tavern")
    if state.tabletop["active_match"] is not None:
        raise ValueError("finish the current match first")
    patron = next((person for person in patrons(state) if person.id == patron_id), None)
    if patron is None:
        raise ValueError("that patron is not available in the tavern")
    collection = collection_for(state, state.courier.id)
    roles = collection["roles"]
    role_catalog, _ = office_catalog()
    patron_roles = list(role_catalog)
    shift = int.from_bytes(hashlib.sha256(patron_id.encode()).digest()[:2], "big") % len(patron_roles)
    patron_roles = [patron_roles[(shift + index) % len(patron_roles)] for index in range(4)]
    match = new_match(state.seed, state.courier.id, patron_id, roles, patron_roles, collection["deck"], len(state.tabletop["records"]), collection)
    _advance_world(state)
    state.tabletop["active_match"] = match
    return match


def finish_match(state: Any) -> str:
    from jomon.calendar import calendar_at

    match = state.tabletop["active_match"]
    if not match or match["winner"] is None:
        raise ValueError("the match has not ended")
    result = "win" if match["winner"] == 0 else "draw" if match["winner"] == "draw" else "loss"
    collection = collection_for(state, match["courier_id"])
    collection[{"win": "wins", "loss": "losses", "draw": "draws"}[result]] += 1
    date = calendar_at(state)
    season_key = f"{date.year}:{date.season}"
    first_win = result == "win" and not any(
        row["courier"] == match["courier_id"] and row["patron"] == match["patron_id"]
        and row["season"] == season_key and row["result"] == "win"
        for row in state.tabletop["records"]
    )
    if first_win:
        state.trade_credit += 1
        patron = next((person for person in [*state.household, *state.visitors, state.bartender] if person.id == match["patron_id"]), None)
        if patron:
            patron.relationships[match["courier_id"]] = min(3, patron.relationships.get(match["courier_id"], 0) + 1)
        courier = next((person for person in state.household if person.id == match["courier_id"]), None)
        if courier:
            courier.strategy = min(20, courier.strategy + 1)
    state.tabletop["records"].append({
        "courier": match["courier_id"], "patron": match["patron_id"],
        "season": season_key, "result": result, "score": match["scores"][:],
        "department": match["department"],
    })
    state.tabletop["active_match"] = None
    return result


def validate_tabletop(state: Any) -> None:
    data = state.tabletop
    if not isinstance(data, dict) or set(data) != {"collections", "records", "active_match"}:
        raise ValueError("invalid Dullest Dungeon ledger")
    if not isinstance(data["collections"], dict) or not isinstance(data["records"], list):
        raise ValueError("invalid Dullest Dungeon collection or record")
    all_ids = {person.id for person in [*state.household, *state.visitors, state.bartender]}
    roles, cards = office_catalog()
    source = load_catalog()
    for courier_id, collection in data["collections"].items():
        if courier_id not in {person.id for person in state.household}:
            raise ValueError("collection has no household owner")
        if len(collection["roles"]) != 4 or any(role not in roles for role in collection["roles"]):
            raise ValueError("invalid worker roster")
        if any(card not in cards or type(count) is not int or count < 0 for card, count in collection["cards"].items()):
            raise ValueError("invalid card collection")
        if not 20 <= len(collection["deck"]) <= 30 or any(collection["deck"].count(card) > collection["cards"].get(card, 0) or cards[card].role not in collection["roles"] for card in collection["deck"]):
            raise ValueError("invalid office deck")
        if collection["doctrine"] not in source.doctrines or any(card not in cards or branch not in {"engine", "coverage"} or not any(item["card_id"] == card for item in source.masteries.values()) for card, branch in collection["masteries"].items()):
            raise ValueError("invalid office mastery or doctrine")
        if any(card not in cards or infusion not in source.infusions for card, infusion in collection["infusions"].items()):
            raise ValueError("invalid office infusion")
    match = data["active_match"]
    if match is None:
        return
    if match["courier_id"] not in all_ids or match["patron_id"] not in all_ids:
        raise ValueError("match participants are missing")
    if len(match["board"]) != HEIGHT or any(len(row) != WIDTH for row in match["board"]):
        raise ValueError("invalid office board")
    if len(match["pieces"]) != 8 or len(match["files"]) != 2 or len(match["sides"]) != 2:
        raise ValueError("invalid office match")
    if match["turn"] not in (0, 1) or not 1 <= match["round"] <= MAX_ROUNDS + OVERTIME_ROUNDS + 1:
        raise ValueError("invalid office turn")
    EventQueue.from_snapshot(match["queue"])
    for piece in match["pieces"]:
        if piece["role"] not in roles or not 0 <= piece["x"] < WIDTH or not 0 <= piece["y"] < HEIGHT:
            raise ValueError("invalid office worker")
