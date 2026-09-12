"""A bounded, four-seat push-your-luck dice contest at Jomon's tavern."""

from __future__ import annotations

import hashlib
from typing import Callable

from .state import GameState, Person
from .tavern_draw import STARTING_NPC_CREDIT, available_opponents

STARTING_PURSE = 24
PRIZE = 2
ROUNDS = 3
MAX_ROLLS = 6


def available_dice_opponents(state: GameState) -> list[Person]:
    return available_opponents(state)


def _random(match: dict) -> int:
    value = match["rng"]
    value ^= (value << 13) & ((1 << 64) - 1)
    value ^= value >> 7
    value ^= (value << 17) & ((1 << 64) - 1)
    match["rng"] = value or 1
    return match["rng"]


def _log(match: dict, message: str) -> None:
    match["log"].append(message)
    del match["log"][:-12]


def start_match(state: GameState, opponents: list[str]) -> dict:
    from .actions import _advance_world
    from .vessel import DICE_NPC_SEATS, DICE_PLAYER_SEAT, seat_dice_players

    if state.location != "jomon" or state.jomon_space != "tavern" or state.position != DICE_PLAYER_SEAT:
        raise ValueError("sit at the marked bones table chair")
    if state.courier is None or not state.courier.alive or state.tavern_dice["active_match"] is not None:
        raise ValueError("finish the current bones contest first")
    if state.tabletop["active_match"] is not None or state.tavern_draw["active_hand"] is not None:
        raise ValueError("finish the other active tavern game first")
    if len(opponents) != 3 or len(set(opponents)) != 3:
        raise ValueError("invite three distinct tavern adults")
    available = {person.id: person for person in available_dice_opponents(state)}
    if any(identity not in available for identity in opponents):
        raise ValueError("all three opponents must be in the tavern")
    occupied = {schedule.position for identity, schedule in state.actor_schedules.items()
                if schedule.area == "tavern" and identity not in opponents}
    occupied.add(state.position)
    if sum(point not in occupied for point in DICE_NPC_SEATS) < 3:
        raise ValueError("the bones table needs three free opponent chairs")
    number = state.tavern_dice["match_number"] + 1
    players = [state.courier.id, *opponents]
    digest = hashlib.sha256(f"quay-bones:{state.seed}:{number}:{':'.join(players)}".encode()).digest()
    match = {"version": 1, "number": number, "players": players,
             "names": [state.courier.name, *(available[identity].name for identity in opponents)],
             "rng": int.from_bytes(digest[:8], "big") or 1, "phase": "rolling", "round": 0,
             "turn": 0, "scores": [0] * 4, "turn_total": 0, "roll_count": 0,
             "forced": False, "last_dice": [], "busts": [0] * 4,
             "winners": [], "prize": 0,
             "log": ["Three rounds. Roll two bones; hold, or risk the turn's points."]}
    for identity in opponents:
        state.tavern_draw["bankrolls"].setdefault(identity, STARTING_NPC_CREDIT)
    state.tavern_dice["match_number"] = number
    state.tavern_dice["active_match"] = match
    seat_dice_players(state, opponents)
    _advance_world(state)
    return match


def _settle(state: GameState, match: dict) -> None:
    best = max(match["scores"])
    winners = [seat for seat, score in enumerate(match["scores"]) if score == best]
    match["phase"] = "complete"
    match["turn"] = None
    match["winners"] = winners
    if len(winners) == 1:
        prize = min(PRIZE, state.tavern_dice["purse"])
        state.tavern_dice["purse"] -= prize
        match["prize"] = prize
        if winners[0] == 0:
            state.trade_credit += prize
        else:
            identity = match["players"][winners[0]]
            state.tavern_draw["bankrolls"][identity] += prize
    names = ", ".join(match["names"][seat] for seat in winners)
    _log(match, f"{names} finish on {best}. {'Prize: ' + str(match['prize']) + ' credit.' if len(winners) == 1 else 'Tie: no purse leaves the table.'}")
    records = state.tavern_dice["records"]
    records.append({"number": match["number"], "players": match["players"][:],
                    "scores": match["scores"][:], "winners": [match["players"][seat] for seat in winners],
                    "prize": match["prize"]})
    del records[:-40]


def _end_turn(state: GameState, match: dict, *, bust: bool = False) -> None:
    seat = match["turn"]
    if bust:
        match["busts"][seat] += 1
        _log(match, f"{match['names'][seat]} busts and banks nothing this turn.")
    else:
        match["scores"][seat] += match["turn_total"]
        _log(match, f"{match['names'][seat]} banks {match['turn_total']} points.")
    match["turn_total"] = 0
    match["roll_count"] = 0
    match["forced"] = False
    if seat == 3:
        match["round"] += 1
        if match["round"] == ROUNDS:
            _settle(state, match)
            return
    match["turn"] = (seat + 1) % 4


def roll(state: GameState) -> tuple[int, int]:
    match = state.tavern_dice["active_match"]
    if match is None or match["phase"] != "rolling":
        raise ValueError("there is no turn to roll")
    first, second = _random(match) % 6 + 1, _random(match) % 6 + 1
    match["last_dice"] = [first, second]
    match["roll_count"] += 1
    if (first == 1) != (second == 1):
        _end_turn(state, match, bust=True)
        return first, second
    match["turn_total"] += first + second
    match["forced"] = first == second
    _log(match, f"{match['names'][match['turn']]} rolls {first} + {second}; turn pot {match['turn_total']}.")
    if match["roll_count"] >= MAX_ROLLS:
        _end_turn(state, match)
    return first, second


def hold(state: GameState) -> None:
    match = state.tavern_dice["active_match"]
    if match is None or match["phase"] != "rolling":
        raise ValueError("there is no turn to bank")
    if match["roll_count"] == 0 or match["forced"]:
        raise ValueError("roll first; a double requires one more roll")
    _end_turn(state, match)


def drive_npcs(state: GameState, on_action: Callable[[dict], None] | None = None) -> None:
    match = state.tavern_dice["active_match"]
    for _ in range(80):
        if match is None or match["phase"] == "complete" or match["turn"] == 0:
            return
        seat = match["turn"]
        leader = max(match["scores"])
        target = 12 + (4 if match["round"] == ROUNDS - 1 and match["scores"][seat] < leader else 0)
        if match["roll_count"] and not match["forced"] and match["turn_total"] >= target:
            hold(state)
        else:
            roll(state)
        if on_action:
            on_action(match)
    raise RuntimeError("the bones contest exceeded its action bound")


def close_match(state: GameState) -> None:
    match = state.tavern_dice["active_match"]
    if match is None or match["phase"] != "complete":
        raise ValueError("the bones contest has not finished")
    state.tavern_dice["active_match"] = None


def validate_tavern_dice(state: GameState) -> None:
    data = state.tavern_dice
    if (not isinstance(data, dict) or set(data) != {"purse", "match_number", "active_match", "records"}
            or type(data["purse"]) is not int or not 0 <= data["purse"] <= STARTING_PURSE
            or type(data["match_number"]) is not int or data["match_number"] < 0
            or not isinstance(data["records"], list) or len(data["records"]) > 40):
        raise ValueError("invalid tavern bones ledger")
    known = {person.id for person in [*state.household, *state.visitors, state.bartender]}
    for record in data["records"]:
        if (not isinstance(record, dict) or set(record) != {"number", "players", "scores", "winners", "prize"}
                or type(record["number"]) is not int or not 1 <= record["number"] <= data["match_number"]
                or not isinstance(record["players"], list) or len(record["players"]) != 4
                or len(set(record["players"])) != 4 or any(identity not in known for identity in record["players"])
                or not isinstance(record["scores"], list) or len(record["scores"]) != 4
                or any(type(score) is not int or not 0 <= score <= 3 * MAX_ROLLS * 24 for score in record["scores"])
                or not isinstance(record["winners"], list) or not record["winners"]
                or any(identity not in record["players"] for identity in record["winners"])
                or len(record["winners"]) != len(set(record["winners"]))
                or record["winners"] != [record["players"][seat] for seat, score in enumerate(record["scores"])
                                          if score == max(record["scores"])]
                or type(record["prize"]) is not int or not 0 <= record["prize"] <= PRIZE):
            raise ValueError("invalid tavern bones history")
    match = data["active_match"]
    if match is None:
        return
    keys = {"version", "number", "players", "names", "rng", "phase", "round", "turn", "scores",
            "turn_total", "roll_count", "forced", "last_dice", "busts", "winners", "prize", "log"}
    if not isinstance(match, dict) or set(match) != keys or match["version"] != 1:
        raise ValueError("invalid tavern bones match")
    if (match["number"] != data["match_number"] or not isinstance(match["players"], list)
            or len(match["players"]) != 4 or len(set(match["players"])) != 4
            or match["players"][0] not in {person.id for person in state.household}
            or any(identity not in known for identity in match["players"][1:])
            or not isinstance(match["names"], list) or len(match["names"]) != 4
            or any(not isinstance(name, str) or not name for name in match["names"])
            or type(match["rng"]) is not int or not 1 <= match["rng"] < 2**64):
        raise ValueError("invalid tavern bones seats")
    if (match["phase"] not in {"rolling", "complete"}
            or type(match["round"]) is not int or not 0 <= match["round"] <= ROUNDS
            or match["phase"] == "complete" and match["round"] != ROUNDS
            or match["phase"] == "rolling" and match["round"] >= ROUNDS
            or type(match["turn"]) is not int and match["turn"] is not None
            or match["phase"] == "rolling" and match["turn"] not in range(4)
            or match["phase"] == "complete" and match["turn"] is not None
            or type(match["turn_total"]) is not int or not 0 <= match["turn_total"] <= MAX_ROLLS * 24
            or type(match["roll_count"]) is not int or not 0 <= match["roll_count"] < MAX_ROLLS + 1
            or type(match["forced"]) is not bool
            or not isinstance(match["last_dice"], list) or len(match["last_dice"]) not in {0, 2}
            or any(type(die) is not int or not 1 <= die <= 6 for die in match["last_dice"])
            or not isinstance(match["scores"], list) or len(match["scores"]) != 4
            or any(type(score) is not int or not 0 <= score <= 3 * MAX_ROLLS * 24 for score in match["scores"])
            or not isinstance(match["busts"], list) or len(match["busts"]) != 4
            or any(type(bust) is not int or not 0 <= bust <= ROUNDS for bust in match["busts"])
            or not isinstance(match["winners"], list)
            or any(type(seat) is not int or not 0 <= seat < 4 for seat in match["winners"])
            or len(match["winners"]) != len(set(match["winners"]))
            or match["phase"] == "rolling" and match["winners"]
            or match["phase"] == "complete" and not match["winners"]
            or match["phase"] == "complete" and match["winners"] != [seat for seat, score in enumerate(match["scores"])
                                                                  if score == max(match["scores"])]
            or match["phase"] == "complete" and (match["turn_total"] or match["roll_count"] or match["forced"])
            or type(match["prize"]) is not int or not 0 <= match["prize"] <= PRIZE
            or match["phase"] == "rolling" and match["prize"]
            or len(match["winners"]) > 1 and match["prize"]
            or not isinstance(match["log"], list) or len(match["log"]) > 12
            or any(not isinstance(line, str) for line in match["log"])):
        raise ValueError("invalid tavern bones result")
