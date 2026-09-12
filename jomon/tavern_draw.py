"""Four-seat, fixed-limit five-card draw using Jomon's tavern credit."""

from __future__ import annotations

from collections import Counter
import hashlib
from typing import Callable

from .state import GameState, Person

ANTE = 1
BET = 1
MAX_RAISES = 1
MAX_EXPOSURE = ANTE + 2 * (BET + MAX_RAISES)
STARTING_NPC_CREDIT = 12
RANK_NAMES = (
    "High card", "One pair", "Two pair", "Three of a kind", "Straight",
    "Flush", "Full house", "Four of a kind", "Straight flush",
)


def available_opponents(state: GameState) -> list[Person]:
    people = [*state.household, *state.visitors, state.bartender]
    return [person for person in people if person.id != state.active_courier_id
            and person.alive and person.available
            and (schedule := state.actor_schedules.get(person.id)) is not None
            and schedule.area == "tavern"]


def _roll(hand: dict) -> int:
    value = hand["rng"]
    value ^= (value << 13) & ((1 << 64) - 1)
    value ^= value >> 7
    value ^= (value << 17) & ((1 << 64) - 1)
    hand["rng"] = value or 1
    return hand["rng"]


def _log(hand: dict, message: str) -> None:
    hand["log"].append(message)
    del hand["log"][:-12]


def _balance(state: GameState, hand: dict, seat: int) -> int:
    return state.trade_credit if seat == 0 else state.tavern_draw["bankrolls"][hand["players"][seat]]


def _change_balance(state: GameState, hand: dict, seat: int, amount: int) -> None:
    if seat == 0:
        state.trade_credit += amount
    else:
        state.tavern_draw["bankrolls"][hand["players"][seat]] += amount


def _pay(state: GameState, hand: dict, seat: int, amount: int) -> None:
    if amount < 0 or _balance(state, hand, seat) < amount:
        raise ValueError("the seat cannot cover that wager")
    _change_balance(state, hand, seat, -amount)
    hand["pot"] += amount
    hand["committed"][seat] += amount


def _first_after_dealer(hand: dict) -> int:
    return next((hand["dealer"] + offset) % 4 for offset in range(1, 5)
                if hand["active"][(hand["dealer"] + offset) % 4])


def evaluate(cards: list[int]) -> tuple[int, ...]:
    if len(cards) != 5 or len(set(cards)) != 5 or any(type(card) is not int or not 0 <= card < 52 for card in cards):
        raise ValueError("a poker hand needs five distinct cards")
    ranks = sorted((card % 13 + 2 for card in cards), reverse=True)
    counts = Counter(ranks)
    groups = sorted(((count, rank) for rank, count in counts.items()), reverse=True)
    flush = len({card // 13 for card in cards}) == 1
    unique = sorted(counts)
    straight = (5 if unique == [2, 3, 4, 5, 14] else
                unique[-1] if len(unique) == 5 and unique[-1] - unique[0] == 4 else 0)
    if straight and flush:
        return 8, straight
    if groups[0][0] == 4:
        return 7, groups[0][1], groups[1][1]
    if groups[0][0] == 3 and groups[1][0] == 2:
        return 6, groups[0][1], groups[1][1]
    if flush:
        return 5, *ranks
    if straight:
        return 4, straight
    if groups[0][0] == 3:
        return 3, groups[0][1], *sorted((rank for rank in ranks if rank != groups[0][1]), reverse=True)
    if groups[0][0] == groups[1][0] == 2:
        high, low = sorted((groups[0][1], groups[1][1]), reverse=True)
        return 2, high, low, next(rank for rank in ranks if rank not in (high, low))
    if groups[0][0] == 2:
        return 1, groups[0][1], *sorted((rank for rank in ranks if rank != groups[0][1]), reverse=True)
    return 0, *ranks


def start_hand(state: GameState, opponents: list[str], *, wagering: bool) -> dict:
    from .actions import _advance_world
    from .vessel import DRAW_NPC_SEATS, DRAW_PLAYER_SEAT, seat_draw_players

    if state.location != "jomon" or state.jomon_space != "tavern" or state.position != DRAW_PLAYER_SEAT:
        raise ValueError("sit at the marked draw table chair")
    if state.courier is None or not state.courier.alive or state.tavern_draw["active_hand"] is not None:
        raise ValueError("finish the current hand before dealing another")
    if state.tabletop["active_match"] is not None or state.tavern_dice["active_match"] is not None:
        raise ValueError("finish the other active tavern game first")
    if len(opponents) != 3 or len(set(opponents)) != 3:
        raise ValueError("invite three distinct tavern adults")
    available = {person.id: person for person in available_opponents(state)}
    if any(identity not in available for identity in opponents):
        raise ValueError("all three opponents must be in the tavern")
    occupied = {schedule.position for identity, schedule in state.actor_schedules.items()
                if schedule.area == "tavern" and identity not in opponents}
    occupied.add(state.position)
    if sum(point not in occupied for point in DRAW_NPC_SEATS) < 3:
        raise ValueError("the draw table needs three free opponent chairs")
    bankrolls = state.tavern_draw["bankrolls"]
    if wagering and (state.trade_credit < MAX_EXPOSURE
                    or any(bankrolls.get(identity, STARTING_NPC_CREDIT) < MAX_EXPOSURE for identity in opponents)):
        raise ValueError(f"each seat needs {MAX_EXPOSURE} credit to cover the capped hand")
    players = [state.courier.id, *opponents]
    names = [state.courier.name, *(available[identity].name for identity in opponents)]
    number = state.tavern_draw["hand_number"] + 1
    digest = hashlib.sha256(f"tavern-draw:{state.seed}:{number}:{':'.join(players)}".encode()).digest()
    hand = {"version": 1, "players": players, "names": names, "number": number,
            "rng": int.from_bytes(digest[:8], "big") or 1, "dealer": (number + 2) % 4,
            "hands": [[] for _ in range(4)], "deck": list(range(52)), "discards": [],
            "active": [True] * 4, "drawn": [False] * 4, "phase": "bet1" if wagering else "draw",
            "turn": 0, "wagering": wagering, "pot": 0, "final_pot": 0,
            "committed": [0] * 4, "round_paid": [0] * 4, "current_bet": 0,
            "raises": 0, "acted": [False] * 4, "winners": [], "payouts": [0] * 4,
            "log": ["Five cards each. One draw, then the final decision."]}
    for index in range(51, 0, -1):
        other = _roll(hand) % (index + 1)
        hand["deck"][index], hand["deck"][other] = hand["deck"][other], hand["deck"][index]
    for _ in range(5):
        for seat in range(4):
            hand["hands"][seat].append(hand["deck"].pop())
    for identity in opponents:
        bankrolls.setdefault(identity, STARTING_NPC_CREDIT)
    if wagering:
        for seat in range(4):
            _pay(state, hand, seat, ANTE)
        _log(hand, f"Each seat antes {ANTE} credit. No seat can lose more than {MAX_EXPOSURE} this hand.")
    hand["turn"] = _first_after_dealer(hand)
    state.tavern_draw["hand_number"] = number
    state.tavern_draw["active_hand"] = hand
    seat_draw_players(state, opponents)
    _advance_world(state)
    return hand


def _settle(state: GameState, hand: dict) -> None:
    survivors = [seat for seat in range(4) if hand["active"][seat]]
    scores = {seat: evaluate(hand["hands"][seat]) for seat in survivors}
    best = max(scores.values())
    winners = [seat for seat in survivors if scores[seat] == best]
    hand["winners"] = winners
    hand["phase"] = "complete"
    hand["turn"] = None
    hand["final_pot"] = hand["pot"]
    share, remainder = divmod(hand["pot"], len(winners))
    for seat in winners:
        hand["payouts"][seat] = share
    for offset in range(1, 5):
        seat = (hand["dealer"] + offset) % 4
        if seat in winners and remainder:
            hand["payouts"][seat] += 1
            remainder -= 1
    for seat, amount in enumerate(hand["payouts"]):
        _change_balance(state, hand, seat, amount)
    hand["pot"] = 0
    label = RANK_NAMES[best[0]] if len(survivors) > 1 else "uncontested pot"
    _log(hand, f"{', '.join(hand['names'][seat] for seat in winners)} win {hand['final_pot']} credit with {label}.")
    records = state.tavern_draw["records"]
    records.append({"number": hand["number"], "players": hand["players"][:],
                    "winners": [hand["players"][seat] for seat in winners],
                    "pot": hand["final_pot"], "wagering": hand["wagering"]})
    del records[:-40]


def bet_action(state: GameState, action: str) -> None:
    hand = state.tavern_draw["active_hand"]
    if hand is None or hand["phase"] not in {"bet1", "bet2"}:
        raise ValueError("there is no open betting decision")
    seat = hand["turn"]
    due = hand["current_bet"] - hand["round_paid"][seat]
    if action == "fold":
        hand["active"][seat] = False
        hand["acted"][seat] = True
        _log(hand, f"{hand['names'][seat]} folds.")
    elif action in {"call", "check"}:
        if action == "check" and due:
            raise ValueError("a live bet must be called or folded")
        _pay(state, hand, seat, due)
        hand["round_paid"][seat] += due
        hand["acted"][seat] = True
        _log(hand, f"{hand['names'][seat]} {'calls' if due else 'checks'}{f' {due}' if due else ''}.")
    elif action == "raise":
        opening = hand["current_bet"] == 0
        if not opening and hand["raises"] >= MAX_RAISES:
            raise ValueError("the house allows one raise in each betting round")
        _pay(state, hand, seat, due + BET)
        hand["current_bet"] += BET
        hand["round_paid"][seat] = hand["current_bet"]
        hand["raises"] += 0 if opening else 1
        hand["acted"] = [not active or index == seat for index, active in enumerate(hand["active"])]
        _log(hand, f"{hand['names'][seat]} {'bets' if opening else 'raises to'} {hand['current_bet']}.")
    else:
        raise ValueError("choose fold, check, call, or raise")
    if sum(hand["active"]) == 1:
        _settle(state, hand)
        return
    if all(not active or hand["acted"][index] and hand["round_paid"][index] == hand["current_bet"]
           for index, active in enumerate(hand["active"])):
        if hand["phase"] == "bet1":
            hand["phase"] = "draw"
            hand["turn"] = _first_after_dealer(hand)
            _log(hand, "The first betting round closes. Choose cards to exchange.")
        else:
            _settle(state, hand)
        return
    hand["turn"] = next(index for offset in range(1, 5)
                        if hand["active"][(index := (seat + offset) % 4)]
                        and (not hand["acted"][index] or hand["round_paid"][index] < hand["current_bet"]))


def draw_cards(state: GameState, discard_indices: list[int]) -> None:
    hand = state.tavern_draw["active_hand"]
    if hand is None or hand["phase"] != "draw":
        raise ValueError("the table is not drawing")
    seat = hand["turn"]
    if len(discard_indices) != len(set(discard_indices)) or any(type(index) is not int or not 0 <= index < 5 for index in discard_indices):
        raise ValueError("select each of the five card positions at most once")
    held = hand["hands"][seat]
    removed = [held[index] for index in discard_indices]
    hand["hands"][seat] = [card for index, card in enumerate(held) if index not in discard_indices]
    hand["discards"].extend(removed)
    hand["hands"][seat].extend(hand["deck"].pop() for _ in removed)
    hand["drawn"][seat] = True
    _log(hand, f"{hand['names'][seat]} exchanges {len(removed)} card(s).")
    if all(not active or hand["drawn"][index] for index, active in enumerate(hand["active"])):
        if hand["wagering"]:
            hand["phase"] = "bet2"
            hand["round_paid"] = [0] * 4
            hand["current_bet"] = hand["raises"] = 0
            hand["acted"] = [not active for active in hand["active"]]
            hand["turn"] = _first_after_dealer(hand)
            _log(hand, "The draw closes. Final betting begins.")
        else:
            _settle(state, hand)
        return
    hand["turn"] = next(index for offset in range(1, 5)
                        if hand["active"][(index := (seat + offset) % 4)] and not hand["drawn"][index])


def _npc_discards(hand: dict, seat: int) -> list[int]:
    cards = hand["hands"][seat]
    score = evaluate(cards)
    if score[0] >= 4 or score[0] == 6:
        return []
    counts = Counter(card % 13 + 2 for card in cards)
    if score[0] in {1, 2, 3, 7}:
        keep = {rank for rank, count in counts.items() if count >= 2}
        return [index for index, card in enumerate(cards) if card % 13 + 2 not in keep]
    suits = Counter(card // 13 for card in cards)
    if max(suits.values()) == 4:
        keep_suit = max(suits, key=suits.get)
        return [index for index, card in enumerate(cards) if card // 13 != keep_suit]
    strongest = sorted(range(5), key=lambda index: cards[index] % 13, reverse=True)[:2]
    return [index for index in range(5) if index not in strongest]


def _npc_bet(hand: dict) -> str:
    seat = hand["turn"]
    score = evaluate(hand["hands"][seat])
    due = hand["current_bet"] - hand["round_paid"][seat]
    roll = _roll(hand) % 100
    if (hand["current_bet"] == 0 or hand["raises"] < MAX_RAISES) and (score[0] >= 2 or score[0] == 1 and score[1] >= 12 and roll < 55 or roll < 4):
        return "raise"
    if due == 0:
        return "check"
    if score[0] >= 1 or score[1] >= 13 and due == 1 or roll < (24 if due == 1 else 8):
        return "call"
    return "fold"


def drive_npcs(state: GameState, on_action: Callable[[dict], None] | None = None) -> None:
    hand = state.tavern_draw["active_hand"]
    for _ in range(40):
        if hand is None or hand["phase"] == "complete" or hand["turn"] == 0 and hand["active"][0]:
            return
        if hand["phase"] == "draw":
            draw_cards(state, _npc_discards(hand, hand["turn"]))
        else:
            bet_action(state, _npc_bet(hand))
        if on_action:
            on_action(hand)
    raise RuntimeError("tavern draw could not settle within its action bound")


def close_hand(state: GameState) -> None:
    hand = state.tavern_draw["active_hand"]
    if hand is None or hand["phase"] != "complete":
        raise ValueError("the hand has not settled")
    state.tavern_draw["active_hand"] = None


def validate_tavern_draw(state: GameState) -> None:
    table = state.tavern_draw
    if not isinstance(table, dict) or set(table) != {"hand_number", "bankrolls", "active_hand", "records"}:
        raise ValueError("invalid tavern draw ledger")
    if type(table["hand_number"]) is not int or table["hand_number"] < 0 or not isinstance(table["bankrolls"], dict):
        raise ValueError("invalid tavern draw counts")
    named = {person.id for person in [*state.household, *state.visitors, state.bartender]}
    if any(identity not in named or type(amount) is not int or not 0 <= amount <= 100000
           for identity, amount in table["bankrolls"].items()):
        raise ValueError("invalid tavern draw bankroll")
    records = table["records"]
    if not isinstance(records, list) or len(records) > 40:
        raise ValueError("invalid tavern draw history")
    for row in records:
        if (not isinstance(row, dict) or set(row) != {"number", "players", "winners", "pot", "wagering"}
                or type(row["number"]) is not int or not 1 <= row["number"] <= table["hand_number"]
                or not isinstance(row["players"], list) or len(row["players"]) != 4
                or len(set(row["players"])) != 4 or any(identity not in named for identity in row["players"])
                or not isinstance(row["winners"], list) or not row["winners"]
                or any(identity not in row["players"] for identity in row["winners"])
                or type(row["pot"]) is not int or not 0 <= row["pot"] <= 20
                or type(row["wagering"]) is not bool):
            raise ValueError("invalid tavern draw record")
    hand = table["active_hand"]
    if hand is None:
        return
    keys = {"version", "players", "names", "number", "rng", "dealer", "hands", "deck", "discards",
            "active", "drawn", "phase", "turn", "wagering", "pot", "final_pot", "committed",
            "round_paid", "current_bet", "raises", "acted", "winners", "payouts", "log"}
    if not isinstance(hand, dict) or set(hand) != keys or hand["version"] != 1:
        raise ValueError("invalid tavern draw hand")
    if (hand["number"] != table["hand_number"] or not isinstance(hand["players"], list)
            or len(hand["players"]) != 4 or len(set(hand["players"])) != 4
            or hand["players"][0] not in {person.id for person in state.household}
            or any(identity not in named for identity in hand["players"][1:])
            or not isinstance(hand["names"], list) or len(hand["names"]) != 4
            or any(not isinstance(name, str) or not name for name in hand["names"])):
        raise ValueError("invalid tavern draw seats")
    if (type(hand["rng"]) is not int or not 1 <= hand["rng"] < 2**64
            or type(hand["dealer"]) is not int or not 0 <= hand["dealer"] < 4
            or hand["phase"] not in {"bet1", "draw", "bet2", "complete"}
            or hand["turn"] not in (None, 0, 1, 2, 3)
            or (hand["turn"] is None) != (hand["phase"] == "complete")
            or type(hand["wagering"]) is not bool
            or not hand["wagering"] and hand["phase"] in {"bet1", "bet2"}):
        raise ValueError("invalid tavern draw phase")
    for key in ("active", "drawn", "acted"):
        if not isinstance(hand[key], list) or len(hand[key]) != 4 or any(type(value) is not bool for value in hand[key]):
            raise ValueError("invalid tavern draw decisions")
    if not any(hand["active"]) or hand["phase"] != "complete" and not hand["active"][hand["turn"]]:
        raise ValueError("invalid tavern draw turn")
    cards = hand["deck"] + hand["discards"]
    if not isinstance(hand["hands"], list) or len(hand["hands"]) != 4 or any(not isinstance(row, list) or len(row) != 5 for row in hand["hands"]):
        raise ValueError("invalid tavern draw cards")
    cards += [card for row in hand["hands"] for card in row]
    if len(cards) != 52 or set(cards) != set(range(52)) or any(type(card) is not int for card in cards):
        raise ValueError("tavern draw deck is duplicated or incomplete")
    for key in ("committed", "round_paid", "payouts"):
        if not isinstance(hand[key], list) or len(hand[key]) != 4 or any(type(value) is not int or not 0 <= value <= 20 for value in hand[key]):
            raise ValueError("invalid tavern draw wagers")
    if (type(hand["pot"]) is not int or type(hand["final_pot"]) is not int
            or not 0 <= hand["pot"] <= 20 or not 0 <= hand["final_pot"] <= 20
            or type(hand["current_bet"]) is not int or not 0 <= hand["current_bet"] <= 2
            or type(hand["raises"]) is not int or not 0 <= hand["raises"] <= MAX_RAISES
            or any(paid > hand["current_bet"] for paid in hand["round_paid"])
            or any(amount > MAX_EXPOSURE for amount in hand["committed"])
            or not hand["wagering"] and (hand["pot"] or hand["final_pot"] or any(hand["committed"]) or any(hand["payouts"]))
            or sum(hand["committed"]) != (hand["final_pot"] if hand["phase"] == "complete" else hand["pot"])
            or hand["phase"] == "complete" and hand["pot"] != 0
            or hand["phase"] != "complete" and (hand["final_pot"] or any(hand["payouts"]))
            or hand["phase"] == "complete" and sum(hand["payouts"]) != hand["final_pot"]):
        raise ValueError("invalid tavern draw pot")
    if (not isinstance(hand["winners"], list) or any(type(seat) is not int or not 0 <= seat < 4 for seat in hand["winners"])
            or hand["phase"] == "complete" and not hand["winners"]
            or len(hand["winners"]) != len(set(hand["winners"]))
            or any(not hand["active"][seat] for seat in hand["winners"])
            or hand["phase"] != "complete" and hand["winners"]
            or not isinstance(hand["log"], list) or len(hand["log"]) > 12
            or any(not isinstance(line, str) for line in hand["log"])):
        raise ValueError("invalid tavern draw result")
