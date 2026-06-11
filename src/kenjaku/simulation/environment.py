from __future__ import annotations

import random
from dataclasses import dataclass
from hashlib import blake2b
from typing import Any

from kenjaku.core import (
    TENHOU_3P,
    TENHOU_4P,
    Action,
    ActionKind,
    Meld,
    RuleSet,
    Tile,
    TileType,
    shanten_for_tiles,
    winning_hand_shapes_for_tiles,
)

SANDBOX_ENVIRONMENT_KIND = "kenjaku-sandbox-environment-v0"
SANDBOX_RULESETS = ("tenhou-4p", "tenhou-3p")
SANDBOX_INITIAL_POINTS = 25000
RIICHI_DEPOSIT_POINTS = 1000
HONBA_RON_POINTS = 300
HONBA_TSUMO_POINTS_PER_LOSER = 100
SANDBOX_DEAD_WALL_TILES = 14
SANDBOX_INITIAL_DORA_INDICATORS = 1
SANDBOX_RULESET_BY_NAME = {
    TENHOU_4P.name: TENHOU_4P,
    TENHOU_3P.name: TENHOU_3P,
}


@dataclass(frozen=True, slots=True)
class SandboxEnvironmentState:
    ruleset: str
    players: int
    wall: tuple[Tile, ...]
    hands: tuple[tuple[Tile, ...], ...]
    dead_wall: tuple[Tile, ...] = ()
    dora_indicators: tuple[Tile, ...] = ()
    discards: tuple[tuple[Tile, ...], ...] = ()
    melds: tuple[tuple[Meld, ...], ...] = ()
    points: tuple[int, ...] = ()
    riichi_sticks: int = 0
    honba: int = 0
    current_seat: int = 0
    turn: int = 0
    drawn_tile: Tile | None = None
    rinshan_draw: bool = False
    needs_discard: bool = False
    pending_discard: Tile | None = None
    pending_discard_seat: int | None = None
    pending_chankan_tile: Tile | None = None
    pending_chankan_seat: int | None = None
    pending_chankan_kind: ActionKind | None = None
    pending_reaction_seats: tuple[int, ...] = ()
    temporary_furiten_seats: tuple[int, ...] = ()
    riichi_seats: tuple[int, ...] = ()
    riichi_pending_discard_seats: tuple[int, ...] = ()
    ippatsu_seats: tuple[int, ...] = ()
    riichi_furiten_seats: tuple[int, ...] = ()
    terminal_reason: str | None = None
    winner_seat: int | None = None
    winner_seats: tuple[int, ...] = ()
    winning_tile: Tile | None = None
    winning_shapes: tuple[str, ...] = ()
    winning_shapes_by_seat: tuple[tuple[int, tuple[str, ...]], ...] = ()
    winning_yaku: tuple[str, ...] = ()
    winning_yaku_by_seat: tuple[tuple[int, tuple[str, ...]], ...] = ()
    winning_ippatsu_seats: tuple[int, ...] = ()
    winning_rinshan_seats: tuple[int, ...] = ()
    terminal_rewards: tuple[float, ...] = ()

    def __post_init__(self) -> None:
        if self.ruleset not in SANDBOX_RULESET_BY_NAME:
            raise ValueError("unsupported sandbox environment ruleset: " + self.ruleset)
        rules = SANDBOX_RULESET_BY_NAME[self.ruleset]
        if self.players != rules.players:
            raise ValueError("players must match ruleset")
        if len(self.hands) != self.players:
            raise ValueError("hands must match player count")
        if len(self.dora_indicators) > len(self.dead_wall):
            raise ValueError("dora indicators cannot exceed dead wall size")
        if self.discards and len(self.discards) != self.players:
            raise ValueError("discards must match player count")
        if self.melds and len(self.melds) != self.players:
            raise ValueError("melds must match player count")
        if self.points and len(self.points) != self.players:
            raise ValueError("point count must match player count")
        if self.riichi_sticks < 0:
            raise ValueError("riichi sticks cannot be negative")
        if self.honba < 0:
            raise ValueError("honba cannot be negative")
        if not 0 <= self.current_seat < self.players:
            raise ValueError("current_seat outside player range")
        if self.needs_discard and self.drawn_tile is not None:
            raise ValueError("needs_discard cannot be set with a drawn tile")
        if self.rinshan_draw and self.drawn_tile is None:
            raise ValueError("rinshan draw requires a drawn tile")
        pending_windows = int(self.pending_discard is not None) + int(
            self.pending_chankan_tile is not None
        )
        if pending_windows > 1:
            raise ValueError("only one pending reaction window is supported")
        if self.needs_discard and pending_windows:
            raise ValueError("needs_discard cannot be set during pending reactions")
        if self.pending_discard is None and self.pending_discard_seat is not None:
            raise ValueError("pending discard seat requires a pending discard")
        if self.pending_chankan_tile is None and self.pending_chankan_seat is not None:
            raise ValueError("pending chankan seat requires a pending chankan tile")
        if self.pending_chankan_tile is None and self.pending_chankan_kind is not None:
            raise ValueError("pending chankan kind requires a pending chankan tile")
        if self.pending_chankan_tile is not None and self.pending_chankan_kind is None:
            raise ValueError("pending chankan kind is required")
        if self.pending_chankan_kind not in (None, ActionKind.ANKAN, ActionKind.KAKAN):
            raise ValueError("pending chankan kind must be ankan or kakan")
        if pending_windows == 0 and self.pending_reaction_seats:
            raise ValueError("pending reaction seats require a pending reaction window")
        if self.pending_discard is not None:
            if self.pending_discard_seat is None:
                raise ValueError("pending discard seat is required")
            if not 0 <= self.pending_discard_seat < self.players:
                raise ValueError("pending discard seat outside player range")
            if self.pending_discard_seat in self.pending_reaction_seats:
                raise ValueError("discarding seat cannot react to its own discard")
            if any(not 0 <= seat < self.players for seat in self.pending_reaction_seats):
                raise ValueError("pending reaction seat outside player range")
        if self.pending_chankan_tile is not None:
            if self.pending_chankan_seat is None:
                raise ValueError("pending chankan seat is required")
            if not 0 <= self.pending_chankan_seat < self.players:
                raise ValueError("pending chankan seat outside player range")
            if self.pending_chankan_seat in self.pending_reaction_seats:
                raise ValueError("kan seat cannot react to its own added kan")
            if any(not 0 <= seat < self.players for seat in self.pending_reaction_seats):
                raise ValueError("pending reaction seat outside player range")
        if any(not 0 <= seat < self.players for seat in self.temporary_furiten_seats):
            raise ValueError("temporary furiten seat outside player range")
        if len(set(self.temporary_furiten_seats)) != len(self.temporary_furiten_seats):
            raise ValueError("temporary furiten seats must be unique")
        if any(not 0 <= seat < self.players for seat in self.riichi_seats):
            raise ValueError("riichi seat outside player range")
        if len(set(self.riichi_seats)) != len(self.riichi_seats):
            raise ValueError("riichi seats must be unique")
        if any(not 0 <= seat < self.players for seat in self.riichi_pending_discard_seats):
            raise ValueError("riichi pending discard seat outside player range")
        if len(set(self.riichi_pending_discard_seats)) != len(
            self.riichi_pending_discard_seats
        ):
            raise ValueError("riichi pending discard seats must be unique")
        if any(seat not in self.riichi_seats for seat in self.riichi_pending_discard_seats):
            raise ValueError("riichi pending discard seats must also be riichi seats")
        if any(not 0 <= seat < self.players for seat in self.ippatsu_seats):
            raise ValueError("ippatsu seat outside player range")
        if len(set(self.ippatsu_seats)) != len(self.ippatsu_seats):
            raise ValueError("ippatsu seats must be unique")
        if any(seat not in self.riichi_seats for seat in self.ippatsu_seats):
            raise ValueError("ippatsu seats must also be riichi seats")
        if any(not 0 <= seat < self.players for seat in self.riichi_furiten_seats):
            raise ValueError("riichi furiten seat outside player range")
        if len(set(self.riichi_furiten_seats)) != len(self.riichi_furiten_seats):
            raise ValueError("riichi furiten seats must be unique")
        if any(seat not in self.riichi_seats for seat in self.riichi_furiten_seats):
            raise ValueError("riichi furiten seats must also be riichi seats")
        if any(not 0 <= seat < self.players for seat in self.winning_ippatsu_seats):
            raise ValueError("winning ippatsu seat outside player range")
        if len(set(self.winning_ippatsu_seats)) != len(self.winning_ippatsu_seats):
            raise ValueError("winning ippatsu seats must be unique")
        if any(seat not in self.winner_seats for seat in self.winning_ippatsu_seats):
            raise ValueError("winning ippatsu seats must also be winner seats")
        if any(not 0 <= seat < self.players for seat in self.winning_rinshan_seats):
            raise ValueError("winning rinshan seat outside player range")
        if len(set(self.winning_rinshan_seats)) != len(self.winning_rinshan_seats):
            raise ValueError("winning rinshan seats must be unique")
        if any(seat not in self.winner_seats for seat in self.winning_rinshan_seats):
            raise ValueError("winning rinshan seats must also be winner seats")
        if self.winning_yaku and not self.winner_seats:
            raise ValueError("winning yaku require winner seats")
        if any(not 0 <= seat < self.players for seat, _yaku in self.winning_yaku_by_seat):
            raise ValueError("winning yaku seat outside player range")
        if any(seat not in self.winner_seats for seat, _yaku in self.winning_yaku_by_seat):
            raise ValueError("winning yaku seats must also be winner seats")
        if self.terminal_rewards and len(self.terminal_rewards) != self.players:
            raise ValueError("terminal reward count must match player count")

    def current_hand(self) -> tuple[Tile, ...]:
        return self.hands[self.current_seat]

    def hand_sizes(self) -> list[int]:
        return [len(hand) for hand in self.hands]

    def to_payload(self) -> dict[str, Any]:
        return {
            "kind": SANDBOX_ENVIRONMENT_KIND,
            "ruleset": self.ruleset,
            "players": self.players,
            "turn": self.turn,
            "current_seat": self.current_seat,
            "wall_remaining": len(self.wall),
            "dead_wall_remaining": len(self.dead_wall),
            "dora_indicators": [tile.notation for tile in self.dora_indicators],
            "hand_sizes": self.hand_sizes(),
            "points": list(_points_by_seat(self)),
            "riichi_sticks": self.riichi_sticks,
            "honba": self.honba,
            "discards": [
                [tile.notation for tile in seat_discards]
                for seat_discards in _discards_by_seat(self)
            ],
            "melds": _meld_payloads(self),
            "drawn_tile": None if self.drawn_tile is None else self.drawn_tile.notation,
            "rinshan_draw": self.rinshan_draw,
            "needs_discard": self.needs_discard,
            "pending_discard": (
                None if self.pending_discard is None else self.pending_discard.notation
            ),
            "pending_discard_seat": self.pending_discard_seat,
            "pending_chankan_tile": (
                None if self.pending_chankan_tile is None else self.pending_chankan_tile.notation
            ),
            "pending_chankan_seat": self.pending_chankan_seat,
            "pending_chankan_kind": (
                None if self.pending_chankan_kind is None else self.pending_chankan_kind.value
            ),
            "pending_reaction_seats": list(self.pending_reaction_seats),
            "temporary_furiten_seats": list(self.temporary_furiten_seats),
            "riichi_seats": list(self.riichi_seats),
            "riichi_pending_discard_seats": list(self.riichi_pending_discard_seats),
            "ippatsu_seats": list(self.ippatsu_seats),
            "riichi_furiten_seats": list(self.riichi_furiten_seats),
            "terminal_reason": self.terminal_reason,
            "winner_seat": self.winner_seat,
            "winner_seats": list(self.winner_seats),
            "winning_tile": None if self.winning_tile is None else self.winning_tile.notation,
            "winning_shapes": list(self.winning_shapes),
            "winning_shapes_by_seat": [
                {"seat": seat, "shapes": list(shapes)}
                for seat, shapes in self.winning_shapes_by_seat
            ],
            "winning_yaku": list(self.winning_yaku),
            "winning_yaku_by_seat": [
                {"seat": seat, "yaku": list(yaku)}
                for seat, yaku in self.winning_yaku_by_seat
            ],
            "winning_ippatsu_seats": list(self.winning_ippatsu_seats),
            "winning_rinshan_seats": list(self.winning_rinshan_seats),
            "terminal_rewards": list(self.terminal_rewards),
        }


def initial_sandbox_environment(
    *,
    ruleset: str = "tenhou-4p",
    seed: str | int = "kenjaku-sandbox-v0",
) -> SandboxEnvironmentState:
    rules = resolve_sandbox_ruleset(ruleset)
    rng = random.Random(_seed_int(seed))
    wall = _shuffled_wall(rng, rules=rules)
    hands = tuple(
        tuple(wall.pop() for _tile in range(13))
        for _seat in range(rules.players)
    )
    dead_wall = tuple(wall.pop() for _tile in range(SANDBOX_DEAD_WALL_TILES))
    return SandboxEnvironmentState(
        ruleset=rules.name,
        players=rules.players,
        wall=tuple(wall),
        hands=hands,
        dead_wall=dead_wall,
        dora_indicators=dead_wall[:SANDBOX_INITIAL_DORA_INDICATORS],
        discards=tuple(() for _seat in range(rules.players)),
        melds=tuple(() for _seat in range(rules.players)),
        points=tuple(SANDBOX_INITIAL_POINTS for _seat in range(rules.players)),
    )


def draw_for_current_seat(
    state: SandboxEnvironmentState,
    *,
    stop_on_tsumo: bool = False,
) -> SandboxEnvironmentState:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("pending reactions must be resolved before drawing")
    if state.needs_discard:
        raise ValueError("current seat must discard before drawing")
    if state.drawn_tile is not None:
        raise ValueError("current seat has already drawn")
    if not state.wall:
        return _replace_state(
            state,
            terminal_reason="wall_exhausted",
            terminal_rewards=_neutral_rewards(state.players),
        )

    draw = state.wall[-1]
    next_wall = state.wall[:-1]
    hands = [list(hand) for hand in state.hands]
    hands[state.current_seat].append(draw)
    next_state = _replace_state(
        state,
        wall=next_wall,
        hands=tuple(tuple(hand) for hand in hands),
        drawn_tile=draw,
        rinshan_draw=False,
        temporary_furiten_seats=_without_seat(
            state.temporary_furiten_seats,
            state.current_seat,
        ),
    )
    if stop_on_tsumo:
        legal_tsumo = legal_tsumo_actions(next_state)
        if legal_tsumo:
            return apply_tsumo_action(next_state, legal_tsumo[0])
    return next_state


def legal_sandbox_actions(
    state: SandboxEnvironmentState,
    *,
    seat: int | None = None,
    include_tsumo: bool = True,
    include_riichi: bool = True,
    include_ankan: bool = True,
    include_kakan: bool = True,
    include_ron: bool = True,
    include_calls: bool = True,
) -> tuple[Action, ...]:
    _require_non_terminal(state)
    if state.pending_discard is not None:
        if seat is None:
            raise ValueError("seat is required for pending discard reactions")
        return legal_reaction_actions(
            state,
            seat=seat,
            include_ron=include_ron,
            include_calls=include_calls,
        )
    if state.pending_chankan_tile is not None:
        if seat is None:
            raise ValueError("seat is required for pending chankan reactions")
        return legal_chankan_reaction_actions(
            state,
            seat=seat,
            include_ron=include_ron,
        )
    if state.drawn_tile is None and not state.needs_discard:
        raise ValueError("current seat must draw before acting")
    if seat is not None and seat != state.current_seat:
        raise ValueError("seat does not match current acting seat")
    actions: list[Action] = []
    if include_tsumo:
        actions.extend(legal_tsumo_actions(state))
    if include_riichi:
        actions.extend(legal_riichi_actions(state))
    if include_ankan:
        actions.extend(legal_ankan_actions(state))
    if include_kakan:
        actions.extend(legal_kakan_actions(state))
    actions.extend(legal_discard_actions(state))
    return tuple(actions)


def legal_tsumo_actions(state: SandboxEnvironmentState) -> tuple[Action, ...]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot tsumo during a pending reaction")
    if state.drawn_tile is None:
        raise ValueError("current seat must draw before tsumo")
    shapes = _winning_shapes_for_state(state, seat=state.current_seat)
    if not shapes:
        return ()
    if not _winning_yaku_for_state(
        state,
        seat=state.current_seat,
        winning_tile=state.drawn_tile,
        shapes=shapes,
        win_kind="tsumo",
    ):
        return ()
    return (Action(ActionKind.TSUMO),)


def legal_riichi_actions(state: SandboxEnvironmentState) -> tuple[Action, ...]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot riichi during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before riichi")
    if _is_riichi(state, seat=state.current_seat):
        return ()
    if _melds_by_seat(state)[state.current_seat]:
        return ()
    if _points_by_seat(state)[state.current_seat] < RIICHI_DEPOSIT_POINTS:
        return ()
    if not _has_riichi_tenpai_discard(state):
        return ()
    return (Action(ActionKind.RIICHI),)


def legal_ankan_actions(state: SandboxEnvironmentState) -> tuple[Action, ...]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot closed kan during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before closed kan")

    rules = SANDBOX_RULESET_BY_NAME[state.ruleset]
    hand = state.current_hand()
    counts = _hand_type_counts(hand)
    actions: list[Action] = []
    for tile_type in rules.tile_types:
        if counts[tile_type.index] >= 4:
            if _is_riichi(
                state,
                seat=state.current_seat,
            ) and not _post_riichi_ankan_preserves_waits(state, tile_type=tile_type):
                continue
            actions.append(
                Action(
                    ActionKind.ANKAN,
                    tile_type,
                    consumed=_first_tiles_of_type(hand, tile_type, 4),
                )
            )
    return tuple(actions)


def legal_kakan_actions(state: SandboxEnvironmentState) -> tuple[Action, ...]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot added kan during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before added kan")
    if _is_riichi(state, seat=state.current_seat):
        return ()

    hand = state.current_hand()
    counts = _hand_type_counts(hand)
    promotable_types = sorted(
        {
            tile_type
            for meld in _melds_by_seat(state)[state.current_seat]
            if meld.kind is ActionKind.PON
            for tile_type in (_pon_meld_tile_type(meld),)
            if counts[tile_type.index] >= 1
        },
        key=lambda tile_type: tile_type.index,
    )
    return tuple(
        Action(
            ActionKind.KAKAN,
            tile_type,
            consumed=_first_tiles_of_type(hand, tile_type, 1),
        )
        for tile_type in promotable_types
    )


def legal_discard_actions(state: SandboxEnvironmentState) -> tuple[Action, ...]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("pending reactions must be resolved before discarding")
    if state.drawn_tile is None and not state.needs_discard:
        raise ValueError("current seat must draw before discarding")
    if _is_post_riichi_discard_locked(state, seat=state.current_seat):
        if state.drawn_tile is None:
            raise ValueError("post-riichi discard requires a drawn tile")
        return (Action.discard(state.drawn_tile.type, tsumogiri=True),)
    tile_types = sorted({tile.type for tile in state.current_hand()}, key=lambda tile: tile.index)
    return tuple(Action.discard(tile_type) for tile_type in tile_types)


def apply_riichi_action(
    state: SandboxEnvironmentState,
    action: Action,
) -> SandboxEnvironmentState:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot riichi during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before riichi")
    if action.kind is not ActionKind.RIICHI:
        raise ValueError("sandbox environment only supports riichi actions here")
    if action not in legal_riichi_actions(state):
        raise ValueError("riichi action is not legal for this state")
    points = list(_points_by_seat(state))
    points[state.current_seat] -= RIICHI_DEPOSIT_POINTS
    return _replace_state(
        state,
        points=tuple(points),
        riichi_sticks=state.riichi_sticks + 1,
        riichi_seats=_with_seat(state.riichi_seats, state.current_seat),
        riichi_pending_discard_seats=_with_seat(
            state.riichi_pending_discard_seats,
            state.current_seat,
        ),
        ippatsu_seats=_with_seat(state.ippatsu_seats, state.current_seat),
    )


def apply_ankan_action(
    state: SandboxEnvironmentState,
    action: Action,
) -> tuple[SandboxEnvironmentState, Meld]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot closed kan during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before closed kan")
    if action.kind is not ActionKind.ANKAN:
        raise ValueError("sandbox environment only supports closed kan actions here")
    if action not in legal_ankan_actions(state):
        raise ValueError("closed kan action is not legal for this state")

    hands = [list(hand) for hand in state.hands]
    for tile in action.consumed:
        _remove_tile(hands[state.current_seat], tile)

    meld = Meld(
        kind=ActionKind.ANKAN,
        tiles=action.consumed,
        called_tile=None,
        from_seat=None,
    )
    melds = [list(seat_melds) for seat_melds in _melds_by_seat(state)]
    melds[state.current_seat].append(meld)
    updates: dict[str, Any] = {
        "hands": tuple(tuple(hand) for hand in hands),
        "melds": tuple(tuple(seat_melds) for seat_melds in melds),
        "drawn_tile": None,
        "rinshan_draw": False,
        "needs_discard": False,
        "pending_discard": None,
        "pending_discard_seat": None,
        "pending_reaction_seats": (),
        "ippatsu_seats": (),
    }

    chankan_seats = _legal_kokushi_ron_seats_for_tile(
        state,
        tile=action.consumed[0],
        candidate_seats=tuple(seat for seat in range(state.players) if seat != state.current_seat),
    )
    if chankan_seats:
        updates["pending_chankan_tile"] = action.consumed[0]
        updates["pending_chankan_seat"] = state.current_seat
        updates["pending_chankan_kind"] = ActionKind.ANKAN
        updates["pending_reaction_seats"] = chankan_seats
        return _replace_state(state, **updates), meld

    _apply_kan_replacement_draw(
        state,
        hands=hands,
        updates=updates,
        seat=state.current_seat,
    )

    return _replace_state(state, **updates), meld


def apply_kakan_action(
    state: SandboxEnvironmentState,
    action: Action,
) -> tuple[SandboxEnvironmentState, Meld]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot added kan during a pending reaction")
    if state.drawn_tile is None or state.needs_discard:
        raise ValueError("current seat must draw before added kan")
    if action.kind is not ActionKind.KAKAN:
        raise ValueError("sandbox environment only supports added kan actions here")
    if action not in legal_kakan_actions(state):
        raise ValueError("added kan action is not legal for this state")

    hands = [list(hand) for hand in state.hands]
    for tile in action.consumed:
        _remove_tile(hands[state.current_seat], tile)

    melds = [list(seat_melds) for seat_melds in _melds_by_seat(state)]
    promoted_meld = _promote_pon_meld_to_kakan(
        melds[state.current_seat],
        tile_type=_action_tile(action),
        added_tile=action.consumed[0],
    )
    updates: dict[str, Any] = {
        "hands": tuple(tuple(hand) for hand in hands),
        "melds": tuple(tuple(seat_melds) for seat_melds in melds),
        "drawn_tile": None,
        "rinshan_draw": False,
        "needs_discard": False,
        "pending_discard": None,
        "pending_discard_seat": None,
        "pending_reaction_seats": (),
        "ippatsu_seats": (),
    }

    chankan_seats = _legal_ron_seats_for_tile(
        state,
        tile=action.consumed[0],
        candidate_seats=tuple(seat for seat in range(state.players) if seat != state.current_seat),
        win_kind="chankan",
    )
    if chankan_seats:
        updates["pending_chankan_tile"] = action.consumed[0]
        updates["pending_chankan_seat"] = state.current_seat
        updates["pending_chankan_kind"] = ActionKind.KAKAN
        updates["pending_reaction_seats"] = chankan_seats
        return _replace_state(state, **updates), promoted_meld

    _apply_kan_replacement_draw(
        state,
        hands=hands,
        updates=updates,
        seat=state.current_seat,
    )

    return _replace_state(state, **updates), promoted_meld


def apply_discard_action(
    state: SandboxEnvironmentState,
    action: Action,
) -> tuple[SandboxEnvironmentState, Tile]:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("pending reactions must be resolved before discarding")
    if state.drawn_tile is None and not state.needs_discard:
        raise ValueError("current seat must draw before discarding")
    if action.kind is not ActionKind.DISCARD or action.tile is None:
        raise ValueError("sandbox environment only supports discard actions")
    if _is_post_riichi_discard_locked(state, seat=state.current_seat):
        if state.drawn_tile is None:
            raise ValueError("post-riichi discard requires a drawn tile")
        if action.tile != state.drawn_tile.type or not action.tsumogiri:
            raise ValueError("post-riichi discard must be a tsumogiri of the drawn tile")

    hands = [list(hand) for hand in state.hands]
    hand = hands[state.current_seat]
    discard_index = _discard_index(hand, action.tile)
    discard = hand.pop(discard_index)
    discards = [list(seat_discards) for seat_discards in _discards_by_seat(state)]
    discards[state.current_seat].append(discard)
    next_seat = (state.current_seat + 1) % state.players
    ippatsu_seats = state.ippatsu_seats
    if _is_post_riichi_discard_locked(state, seat=state.current_seat):
        ippatsu_seats = _without_seat(ippatsu_seats, state.current_seat)
    next_state = _replace_state(
        state,
        hands=tuple(tuple(player_hand) for player_hand in hands),
        discards=tuple(tuple(seat_discards) for seat_discards in discards),
        current_seat=next_seat,
        turn=state.turn + 1,
        drawn_tile=None,
        rinshan_draw=False,
        needs_discard=False,
        pending_discard=discard,
        pending_discard_seat=state.current_seat,
        pending_reaction_seats=tuple(
            seat for seat in range(state.players) if seat != state.current_seat
        ),
        riichi_pending_discard_seats=_without_seat(
            state.riichi_pending_discard_seats,
            state.current_seat,
        ),
        ippatsu_seats=ippatsu_seats,
    )
    return next_state, discard


def legal_reaction_actions(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    include_ron: bool = True,
    include_calls: bool = True,
) -> tuple[Action, ...]:
    _require_non_terminal(state)
    _require_pending_discard(state)
    _require_reaction_seat(state, seat)
    actions: list[Action] = []
    if include_ron:
        actions.extend(legal_ron_actions(state, seat=seat))
    if include_calls:
        actions.extend(legal_call_actions(state, seat=seat))
    actions.append(Action.pass_())
    return tuple(actions)


def legal_chankan_reaction_actions(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    include_ron: bool = True,
) -> tuple[Action, ...]:
    _require_non_terminal(state)
    _require_pending_chankan(state)
    _require_reaction_seat(state, seat)
    actions: list[Action] = []
    if include_ron:
        actions.extend(legal_chankan_ron_actions(state, seat=seat))
    actions.append(Action.pass_())
    return tuple(actions)


def legal_call_actions(state: SandboxEnvironmentState, *, seat: int) -> tuple[Action, ...]:
    _require_non_terminal(state)
    _require_pending_discard(state)
    _require_reaction_seat(state, seat)
    pending_discard = _pending_discard(state)
    pending_discard_seat = _pending_discard_seat(state)
    if _is_riichi(state, seat=seat):
        return ()
    hand = state.hands[seat]
    counts = _hand_type_counts(hand)
    actions: list[Action] = []

    actions.extend(
        _legal_chi_actions(
            hand=hand,
            pending_discard=pending_discard,
            seat=seat,
            pending_discard_seat=pending_discard_seat,
            players=state.players,
        )
    )

    matching = counts[pending_discard.type.index]
    if matching >= 2:
        actions.append(
            Action(
                ActionKind.PON,
                pending_discard.type,
                consumed=_first_tiles_of_type(hand, pending_discard.type, 2),
            )
        )
    if matching >= 3:
        actions.append(
            Action(
                ActionKind.MINKAN,
                pending_discard.type,
                consumed=_first_tiles_of_type(hand, pending_discard.type, 3),
            )
        )
    return tuple(actions)


def legal_ron_actions(state: SandboxEnvironmentState, *, seat: int) -> tuple[Action, ...]:
    _require_non_terminal(state)
    _require_pending_discard(state)
    _require_reaction_seat(state, seat)
    pending_discard = _pending_discard(state)
    if not _can_ron_tile(state, seat=seat, tile=pending_discard, win_kind="ron"):
        return ()
    return (Action(ActionKind.RON, pending_discard.type),)


def legal_chankan_ron_actions(
    state: SandboxEnvironmentState,
    *,
    seat: int,
) -> tuple[Action, ...]:
    _require_non_terminal(state)
    _require_pending_chankan(state)
    _require_reaction_seat(state, seat)
    pending_chankan = _pending_chankan_tile(state)
    can_ron = (
        _can_kokushi_ron_tile(state, seat=seat, tile=pending_chankan)
        if state.pending_chankan_kind is ActionKind.ANKAN
        else _can_ron_tile(state, seat=seat, tile=pending_chankan, win_kind="chankan")
    )
    if not can_ron:
        return ()
    return (Action(ActionKind.RON, pending_chankan.type),)


def apply_call_action(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    action: Action,
) -> tuple[SandboxEnvironmentState, Meld]:
    _require_non_terminal(state)
    _require_pending_discard(state)
    _require_reaction_seat(state, seat)
    ron_seats = _pending_ron_seats(state)
    if ron_seats:
        raise ValueError(
            "call action is blocked while ron reactions are pending: "
            + ",".join(str(ron_seat) for ron_seat in ron_seats)
        )
    if action not in legal_call_actions(state, seat=seat):
        raise ValueError("call action is not legal for this pending discard")
    pending_discard = _pending_discard(state)
    pending_discard_seat = _pending_discard_seat(state)
    hands = [list(hand) for hand in state.hands]
    for tile in action.consumed:
        _remove_tile(hands[seat], tile)

    meld = Meld(
        kind=action.kind,
        tiles=(*action.consumed, pending_discard),
        called_tile=pending_discard,
        from_seat=pending_discard_seat,
    )
    melds = [list(seat_melds) for seat_melds in _melds_by_seat(state)]
    melds[seat].append(meld)
    updates: dict[str, Any] = {
        "hands": tuple(tuple(hand) for hand in hands),
        "melds": tuple(tuple(seat_melds) for seat_melds in melds),
        "current_seat": seat,
        "drawn_tile": None,
        "rinshan_draw": False,
        "needs_discard": action.kind is not ActionKind.MINKAN,
        "pending_discard": None,
        "pending_discard_seat": None,
        "pending_reaction_seats": (),
        "ippatsu_seats": (),
    }

    if action.kind is ActionKind.MINKAN:
        _apply_kan_replacement_draw(
            state,
            hands=hands,
            updates=updates,
            seat=seat,
        )

    return _replace_state(state, **updates), meld


def apply_reaction_pass_action(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    action: Action | None = None,
) -> SandboxEnvironmentState:
    _require_non_terminal(state)
    _require_pending_reaction(state)
    _require_reaction_seat(state, seat)
    if action is not None and action.kind is not ActionKind.PASS:
        raise ValueError("reaction pass requires a pass action")
    temporary_furiten_seats = state.temporary_furiten_seats
    riichi_furiten_seats = state.riichi_furiten_seats
    legal_ron = (
        legal_chankan_ron_actions(state, seat=seat)
        if state.pending_chankan_tile is not None
        else legal_ron_actions(state, seat=seat)
    )
    if legal_ron:
        if _is_riichi(state, seat=seat):
            riichi_furiten_seats = _with_seat(riichi_furiten_seats, seat)
        else:
            temporary_furiten_seats = _with_seat(temporary_furiten_seats, seat)
    remaining = tuple(
        reaction_seat for reaction_seat in state.pending_reaction_seats if reaction_seat != seat
    )
    if remaining:
        return _replace_state(
            state,
            pending_reaction_seats=remaining,
            temporary_furiten_seats=temporary_furiten_seats,
            riichi_furiten_seats=riichi_furiten_seats,
        )
    if state.pending_chankan_tile is not None:
        return _finish_chankan_reaction_window(
            state,
            temporary_furiten_seats=temporary_furiten_seats,
            riichi_furiten_seats=riichi_furiten_seats,
        )
    return _replace_state(
        state,
        pending_discard=None,
        pending_discard_seat=None,
        pending_reaction_seats=(),
        temporary_furiten_seats=temporary_furiten_seats,
        riichi_furiten_seats=riichi_furiten_seats,
    )


def apply_ron_action(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    action: Action,
) -> SandboxEnvironmentState:
    return apply_ron_actions(state, ((seat, action),))


def apply_ron_actions(
    state: SandboxEnvironmentState,
    seat_actions: tuple[tuple[int, Action], ...],
) -> SandboxEnvironmentState:
    _require_non_terminal(state)
    _require_pending_reaction(state)
    pending_tile = _pending_ron_tile(state)
    pending_source_seat = _pending_ron_source_seat(state)
    terminal_reason = "chankan" if state.pending_chankan_tile is not None else "ron"
    if not seat_actions:
        raise ValueError("at least one ron action is required")

    seen: set[int] = set()
    winner_seats: list[int] = []
    winning_shapes_by_seat: list[tuple[int, tuple[str, ...]]] = []
    winning_yaku_by_seat: list[tuple[int, tuple[str, ...]]] = []
    for seat, action in seat_actions:
        _require_reaction_seat(state, seat)
        if seat in seen:
            raise ValueError(f"duplicate ron reaction seat: {seat}")
        seen.add(seat)
        if action.kind is not ActionKind.RON or action.tile != pending_tile.type:
            raise ValueError("sandbox environment only supports matching ron actions here")
        shapes = _winning_shapes_for_state(state, seat=seat, winning_tile=pending_tile)
        if not shapes:
            raise ValueError("reacting hand is not a winning ron")
        if state.pending_chankan_kind is ActionKind.ANKAN and "kokushi" not in shapes:
            raise ValueError("concealed kan can only be robbed by kokushi")
        yaku = _winning_yaku_for_state(
            state,
            seat=seat,
            winning_tile=pending_tile,
            shapes=shapes,
            win_kind=terminal_reason,
        )
        if not yaku:
            raise ValueError("winning hand has no recognized sandbox yaku")
        if _is_discard_furiten(state, seat=seat):
            raise ValueError("reacting hand is in discard furiten")
        if _is_temporary_furiten(state, seat=seat):
            raise ValueError("reacting hand is in temporary furiten")
        if _is_riichi_furiten(state, seat=seat):
            raise ValueError("reacting hand is in riichi furiten")
        winner_seats.append(seat)
        winning_shapes_by_seat.append((seat, shapes))
        winning_yaku_by_seat.append((seat, yaku))

    point_updates = _terminal_win_point_updates(
        state,
        winner_seats=tuple(winner_seats),
        discarder_seat=pending_source_seat,
    )
    return _replace_state(
        state,
        pending_discard=None,
        pending_discard_seat=None,
        pending_chankan_tile=None,
        pending_chankan_seat=None,
        pending_chankan_kind=None,
        pending_reaction_seats=(),
        terminal_reason=terminal_reason,
        winner_seat=winner_seats[0],
        winner_seats=tuple(winner_seats),
        winning_tile=pending_tile,
        winning_shapes=winning_shapes_by_seat[0][1],
        winning_shapes_by_seat=tuple(winning_shapes_by_seat),
        winning_yaku=winning_yaku_by_seat[0][1],
        winning_yaku_by_seat=tuple(winning_yaku_by_seat),
        winning_ippatsu_seats=tuple(
            seat for seat in winner_seats if seat in state.ippatsu_seats
        ),
        winning_rinshan_seats=(),
        ippatsu_seats=(),
        terminal_rewards=_multi_ron_rewards(
            winner_seats=tuple(winner_seats),
            discarder_seat=pending_source_seat,
            players=state.players,
        ),
        **point_updates,
    )


def pass_pending_discard_reactions(state: SandboxEnvironmentState) -> SandboxEnvironmentState:
    _require_non_terminal(state)
    _require_pending_discard(state)
    temporary_furiten_seats = state.temporary_furiten_seats
    riichi_furiten_seats = state.riichi_furiten_seats
    for seat in state.pending_reaction_seats:
        if legal_ron_actions(state, seat=seat):
            if _is_riichi(state, seat=seat):
                riichi_furiten_seats = _with_seat(riichi_furiten_seats, seat)
            else:
                temporary_furiten_seats = _with_seat(temporary_furiten_seats, seat)
    return _replace_state(
        state,
        pending_discard=None,
        pending_discard_seat=None,
        pending_reaction_seats=(),
        temporary_furiten_seats=temporary_furiten_seats,
        riichi_furiten_seats=riichi_furiten_seats,
    )


def apply_tsumo_action(
    state: SandboxEnvironmentState,
    action: Action,
) -> SandboxEnvironmentState:
    _require_non_terminal(state)
    if _has_pending_reaction(state):
        raise ValueError("cannot tsumo during a pending reaction")
    if state.drawn_tile is None:
        raise ValueError("current seat must draw before tsumo")
    if action.kind is not ActionKind.TSUMO:
        raise ValueError("sandbox environment only supports tsumo actions here")
    shapes = _winning_shapes_for_state(state, seat=state.current_seat)
    if not shapes:
        raise ValueError("current hand is not a winning tsumo")
    yaku = _winning_yaku_for_state(
        state,
        seat=state.current_seat,
        winning_tile=state.drawn_tile,
        shapes=shapes,
        win_kind="tsumo",
    )
    if not yaku:
        raise ValueError("winning hand has no recognized sandbox yaku")
    point_updates = _terminal_win_point_updates(
        state,
        winner_seats=(state.current_seat,),
        discarder_seat=None,
    )
    return _replace_state(
        state,
        terminal_reason="tsumo",
        winner_seat=state.current_seat,
        winner_seats=(state.current_seat,),
        winning_tile=state.drawn_tile,
        winning_shapes=shapes,
        winning_shapes_by_seat=((state.current_seat, shapes),),
        winning_yaku=yaku,
        winning_yaku_by_seat=((state.current_seat, yaku),),
        winning_ippatsu_seats=(
            (state.current_seat,) if state.current_seat in state.ippatsu_seats else ()
        ),
        winning_rinshan_seats=((state.current_seat,) if state.rinshan_draw else ()),
        ippatsu_seats=(),
        terminal_rewards=_tsumo_rewards(state.current_seat, state.players),
        **point_updates,
    )


def resolve_sandbox_ruleset(ruleset: str) -> RuleSet:
    try:
        return SANDBOX_RULESET_BY_NAME[ruleset]
    except KeyError as error:
        raise ValueError("unsupported sandbox environment ruleset: " + ruleset) from error


def _discard_index(hand: list[Tile], tile_type: TileType) -> int:
    for index, tile in enumerate(hand):
        if tile.type == tile_type:
            return index
    raise ValueError(f"discard tile is not in current hand: {tile_type.notation}")


def _action_tile(action: Action) -> TileType:
    if action.tile is None:
        raise ValueError(f"{action.kind.value} action requires a tile")
    return action.tile


def _promote_pon_meld_to_kakan(
    melds: list[Meld],
    *,
    tile_type: TileType,
    added_tile: Tile,
) -> Meld:
    for index, meld in enumerate(melds):
        if meld.kind is not ActionKind.PON or _pon_meld_tile_type(meld) != tile_type:
            continue
        promoted = Meld(
            kind=ActionKind.KAKAN,
            tiles=(*meld.tiles, added_tile),
            called_tile=meld.called_tile,
            from_seat=meld.from_seat,
        )
        melds[index] = promoted
        return promoted
    raise ValueError(f"no pon meld can be promoted for {tile_type.notation}")


def _pon_meld_tile_type(meld: Meld) -> TileType:
    if meld.kind is not ActionKind.PON:
        raise ValueError("meld is not a pon")
    if meld.called_tile is not None:
        return meld.called_tile.type
    return meld.tiles[0].type


def _remove_tile(hand: list[Tile], tile: Tile) -> None:
    for index, candidate in enumerate(hand):
        if candidate == tile:
            del hand[index]
            return

    for index, candidate in enumerate(hand):
        if candidate.type == tile.type:
            del hand[index]
            return

    raise ValueError(f"tile is not in current hand: {tile.notation}")


def _hand_without_exact_tile(hand: tuple[Tile, ...], tile: Tile) -> tuple[Tile, ...] | None:
    remaining = list(hand)
    try:
        remaining.remove(tile)
    except ValueError:
        return None
    return tuple(remaining)


def _hand_without_tiles(
    hand: tuple[Tile, ...],
    tiles: tuple[Tile, ...],
) -> tuple[Tile, ...] | None:
    remaining = list(hand)
    for tile in tiles:
        try:
            remaining.remove(tile)
        except ValueError:
            for index, candidate in enumerate(remaining):
                if candidate.type == tile.type:
                    del remaining[index]
                    break
            else:
                return None
    return tuple(remaining)


def _winning_shapes_for_complete_tiles(tiles: tuple[Tile, ...]) -> tuple[str, ...]:
    if len(tiles) != 14:
        return ()
    return winning_hand_shapes_for_tiles(tiles)


def _winning_shapes_for_state(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    winning_tile: Tile | None = None,
) -> tuple[str, ...]:
    concealed_tiles = state.hands[seat]
    if winning_tile is not None:
        concealed_tiles = (*concealed_tiles, winning_tile)
    return _winning_shapes_for_concealed_and_melds(
        concealed_tiles,
        _melds_by_seat(state)[seat],
    )


def _winning_shapes_for_concealed_and_melds(
    concealed_tiles: tuple[Tile, ...],
    melds: tuple[Meld, ...],
) -> tuple[str, ...]:
    if not melds:
        return _winning_shapes_for_complete_tiles(concealed_tiles)

    full_tiles = (*concealed_tiles, *_standard_shape_tiles_for_melds(melds))
    if len(full_tiles) != 14:
        return ()
    shapes = winning_hand_shapes_for_tiles(full_tiles)
    return tuple(shape for shape in shapes if shape == "standard")


def _winning_yaku_for_state(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    winning_tile: Tile | None,
    shapes: tuple[str, ...],
    win_kind: str,
) -> tuple[str, ...]:
    yaku: list[str] = []
    melds = _melds_by_seat(state)[seat]
    full_tiles = _full_yaku_tiles(state, seat=seat, winning_tile=winning_tile)

    if "kokushi" in shapes:
        yaku.append("kokushi")
    if "chiitoitsu" in shapes:
        yaku.append("chiitoitsu")
    if _is_riichi(state, seat=seat):
        yaku.append("riichi")
    if seat in state.ippatsu_seats:
        yaku.append("ippatsu")
    if win_kind == "tsumo" and _is_closed_hand_for_yaku(melds):
        yaku.append("menzen_tsumo")
    if win_kind == "tsumo" and state.rinshan_draw:
        yaku.append("rinshan")
    if win_kind == "chankan":
        yaku.append("chankan")
    if _is_tanyao_yaku(full_tiles):
        yaku.append("tanyao")
    if _has_sandbox_yakuhai(full_tiles):
        yaku.append("yakuhai")
    return tuple(yaku)


def _full_yaku_tiles(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    winning_tile: Tile | None,
) -> tuple[Tile, ...]:
    concealed_tiles = state.hands[seat]
    if winning_tile is not None:
        concealed_tiles = (*concealed_tiles, winning_tile)
    meld_tiles = tuple(tile for meld in _melds_by_seat(state)[seat] for tile in meld.tiles)
    return (*concealed_tiles, *meld_tiles)


def _is_closed_hand_for_yaku(melds: tuple[Meld, ...]) -> bool:
    return all(meld.kind is ActionKind.ANKAN for meld in melds)


def _is_tanyao_yaku(tiles: tuple[Tile, ...]) -> bool:
    return bool(tiles) and all(not tile.type.is_terminal_or_honor for tile in tiles)


def _has_sandbox_yakuhai(tiles: tuple[Tile, ...]) -> bool:
    counts = _hand_type_counts(tiles)
    return any(
        TileType(index).is_honor and count >= 3
        for index, count in enumerate(counts)
    )


def _standard_shape_tiles_for_melds(melds: tuple[Meld, ...]) -> tuple[Tile, ...]:
    return tuple(tile for meld in melds for tile in meld.tiles[:3])


def _has_riichi_tenpai_discard(state: SandboxEnvironmentState) -> bool:
    hand = state.current_hand()
    if len(hand) != 14:
        return False
    for discard_type in {tile.type for tile in hand}:
        candidate = list(hand)
        discard_index = _discard_index(candidate, discard_type)
        candidate.pop(discard_index)
        if shanten_for_tiles(candidate) == 0:
            return True
    return False


def _post_riichi_ankan_preserves_waits(
    state: SandboxEnvironmentState,
    *,
    tile_type: TileType,
) -> bool:
    seat = state.current_seat
    if not _is_post_riichi_discard_locked(state, seat=seat):
        return False
    if state.drawn_tile is None or state.drawn_tile.type != tile_type:
        return False

    melds = _melds_by_seat(state)[seat]
    before_hand = _hand_without_exact_tile(state.current_hand(), state.drawn_tile)
    if before_hand is None:
        return False
    before_waits = _winning_wait_types_for_hand_and_melds(
        state,
        hand=before_hand,
        melds=melds,
    )
    if not before_waits:
        return False

    consumed = _first_tiles_of_type(state.current_hand(), tile_type, 4)
    after_hand = _hand_without_tiles(state.current_hand(), consumed)
    if after_hand is None:
        return False
    after_waits = _winning_wait_types_for_hand_and_melds(
        state,
        hand=after_hand,
        melds=(
            *melds,
            Meld(
                kind=ActionKind.ANKAN,
                tiles=consumed,
                called_tile=None,
                from_seat=None,
            ),
        ),
    )
    return set(before_waits) == set(after_waits)


def _require_non_terminal(state: SandboxEnvironmentState) -> None:
    if state.terminal_reason is not None:
        raise ValueError("environment is already terminal")


def _has_pending_reaction(state: SandboxEnvironmentState) -> bool:
    return state.pending_discard is not None or state.pending_chankan_tile is not None


def _can_ron_tile(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    tile: Tile,
    win_kind: str,
) -> bool:
    shapes = _winning_shapes_for_state(state, seat=seat, winning_tile=tile)
    if not shapes:
        return False
    if not _winning_yaku_for_state(
        state,
        seat=seat,
        winning_tile=tile,
        shapes=shapes,
        win_kind=win_kind,
    ):
        return False
    if _is_discard_furiten(state, seat=seat):
        return False
    if _is_temporary_furiten(state, seat=seat):
        return False
    return not _is_riichi_furiten(state, seat=seat)


def _legal_ron_seats_for_tile(
    state: SandboxEnvironmentState,
    *,
    tile: Tile,
    candidate_seats: tuple[int, ...],
    win_kind: str,
) -> tuple[int, ...]:
    return tuple(
        seat
        for seat in candidate_seats
        if _can_ron_tile(state, seat=seat, tile=tile, win_kind=win_kind)
    )


def _legal_kokushi_ron_seats_for_tile(
    state: SandboxEnvironmentState,
    *,
    tile: Tile,
    candidate_seats: tuple[int, ...],
) -> tuple[int, ...]:
    return tuple(
        seat
        for seat in candidate_seats
        if _can_kokushi_ron_tile(state, seat=seat, tile=tile)
    )


def _can_kokushi_ron_tile(
    state: SandboxEnvironmentState,
    *,
    seat: int,
    tile: Tile,
) -> bool:
    if "kokushi" not in _winning_shapes_for_state(state, seat=seat, winning_tile=tile):
        return False
    if _is_discard_furiten(state, seat=seat):
        return False
    if _is_temporary_furiten(state, seat=seat):
        return False
    return not _is_riichi_furiten(state, seat=seat)


def _apply_kan_replacement_draw(
    state: SandboxEnvironmentState,
    *,
    hands: list[list[Tile]],
    updates: dict[str, Any],
    seat: int,
) -> None:
    if not 0 <= seat < state.players:
        raise ValueError("replacement draw seat outside player range")
    if not _has_dead_wall_replacement_tile(state):
        updates["terminal_reason"] = "wall_exhausted"
        updates["terminal_rewards"] = _neutral_rewards(state.players)
        updates["rinshan_draw"] = False
        return

    dora_indicators = state.dora_indicators
    kan_dora = _next_kan_dora_indicator(state)
    if kan_dora is not None:
        dora_indicators = (*dora_indicators, kan_dora)

    replacement_draw = state.dead_wall[-1]
    hands[seat].append(replacement_draw)
    updates["dead_wall"] = state.dead_wall[:-1]
    updates["dora_indicators"] = dora_indicators
    updates["hands"] = tuple(tuple(hand) for hand in hands)
    updates["drawn_tile"] = replacement_draw
    updates["rinshan_draw"] = True


def _has_dead_wall_replacement_tile(state: SandboxEnvironmentState) -> bool:
    return len(state.dead_wall) > len(state.dora_indicators)


def _next_kan_dora_indicator(state: SandboxEnvironmentState) -> Tile | None:
    if len(state.dead_wall) - len(state.dora_indicators) < 2:
        return None
    return state.dead_wall[len(state.dora_indicators)]


def _finish_chankan_reaction_window(
    state: SandboxEnvironmentState,
    *,
    temporary_furiten_seats: tuple[int, ...],
    riichi_furiten_seats: tuple[int, ...],
) -> SandboxEnvironmentState:
    updates: dict[str, Any] = {
        "pending_chankan_tile": None,
        "pending_chankan_seat": None,
        "pending_chankan_kind": None,
        "pending_reaction_seats": (),
        "temporary_furiten_seats": temporary_furiten_seats,
        "riichi_furiten_seats": riichi_furiten_seats,
    }
    hands = [list(hand) for hand in state.hands]
    _apply_kan_replacement_draw(
        state,
        hands=hands,
        updates=updates,
        seat=state.current_seat,
    )
    return _replace_state(state, **updates)


def _replace_state(state: SandboxEnvironmentState, **updates: Any) -> SandboxEnvironmentState:
    payload = {
        "ruleset": state.ruleset,
        "players": state.players,
        "wall": state.wall,
        "hands": state.hands,
        "dead_wall": state.dead_wall,
        "dora_indicators": state.dora_indicators,
        "discards": state.discards,
        "melds": state.melds,
        "points": state.points,
        "riichi_sticks": state.riichi_sticks,
        "honba": state.honba,
        "current_seat": state.current_seat,
        "turn": state.turn,
        "drawn_tile": state.drawn_tile,
        "rinshan_draw": state.rinshan_draw,
        "needs_discard": state.needs_discard,
        "pending_discard": state.pending_discard,
        "pending_discard_seat": state.pending_discard_seat,
        "pending_chankan_tile": state.pending_chankan_tile,
        "pending_chankan_seat": state.pending_chankan_seat,
        "pending_chankan_kind": state.pending_chankan_kind,
        "pending_reaction_seats": state.pending_reaction_seats,
        "temporary_furiten_seats": state.temporary_furiten_seats,
        "riichi_seats": state.riichi_seats,
        "riichi_pending_discard_seats": state.riichi_pending_discard_seats,
        "ippatsu_seats": state.ippatsu_seats,
        "riichi_furiten_seats": state.riichi_furiten_seats,
        "terminal_reason": state.terminal_reason,
        "winner_seat": state.winner_seat,
        "winner_seats": state.winner_seats,
        "winning_tile": state.winning_tile,
        "winning_shapes": state.winning_shapes,
        "winning_shapes_by_seat": state.winning_shapes_by_seat,
        "winning_yaku": state.winning_yaku,
        "winning_yaku_by_seat": state.winning_yaku_by_seat,
        "winning_ippatsu_seats": state.winning_ippatsu_seats,
        "winning_rinshan_seats": state.winning_rinshan_seats,
        "terminal_rewards": state.terminal_rewards,
    }
    payload.update(updates)
    return SandboxEnvironmentState(**payload)


def _shuffled_wall(rng: random.Random, *, rules: RuleSet) -> list[Tile]:
    wall = [
        Tile(TileType(tile_type))
        for tile_type, count in enumerate(rules.type_counts)
        for _copy in range(count)
    ]
    rng.shuffle(wall)
    return wall


def _seed_int(seed: str | int) -> int:
    if isinstance(seed, int):
        return seed
    digest = blake2b(seed.encode(), digest_size=8).digest()
    return int.from_bytes(digest, "big")


def _melds_by_seat(state: SandboxEnvironmentState) -> tuple[tuple[Meld, ...], ...]:
    if state.melds:
        return state.melds
    return tuple(() for _seat in range(state.players))


def _discards_by_seat(state: SandboxEnvironmentState) -> tuple[tuple[Tile, ...], ...]:
    if state.discards:
        return state.discards
    return tuple(() for _seat in range(state.players))


def _points_by_seat(state: SandboxEnvironmentState) -> tuple[int, ...]:
    if state.points:
        return state.points
    return tuple(SANDBOX_INITIAL_POINTS for _seat in range(state.players))


def _terminal_win_point_updates(
    state: SandboxEnvironmentState,
    *,
    winner_seats: tuple[int, ...],
    discarder_seat: int | None,
) -> dict[str, Any]:
    if not winner_seats or (state.riichi_sticks == 0 and state.honba == 0):
        return {}
    points = list(_points_by_seat(state))
    if state.riichi_sticks:
        points[winner_seats[0]] += state.riichi_sticks * RIICHI_DEPOSIT_POINTS
    if state.honba:
        if discarder_seat is None:
            payment = state.honba * HONBA_TSUMO_POINTS_PER_LOSER
            winner_seat = winner_seats[0]
            for seat in range(state.players):
                if seat == winner_seat:
                    continue
                points[seat] -= payment
                points[winner_seat] += payment
        else:
            payment = state.honba * HONBA_RON_POINTS
            for winner_seat in winner_seats:
                points[winner_seat] += payment
                points[discarder_seat] -= payment
    return {
        "points": tuple(points),
        "riichi_sticks": 0,
    }


def _meld_payloads(state: SandboxEnvironmentState) -> list[list[dict[str, Any]]]:
    return [
        [
            {
                "kind": meld.kind.value,
                "tiles": [tile.notation for tile in meld.tiles],
                "called_tile": None if meld.called_tile is None else meld.called_tile.notation,
                "from_seat": meld.from_seat,
            }
            for meld in seat_melds
        ]
        for seat_melds in _melds_by_seat(state)
    ]


def _hand_type_counts(hand: tuple[Tile, ...]) -> tuple[int, ...]:
    counts = [0] * 34
    for tile in hand:
        counts[tile.type.index] += 1
    return tuple(counts)


def _legal_chi_actions(
    *,
    hand: tuple[Tile, ...],
    pending_discard: Tile,
    seat: int,
    pending_discard_seat: int,
    players: int,
) -> tuple[Action, ...]:
    discarded_type = pending_discard.type
    if seat != (pending_discard_seat + 1) % players or discarded_type.is_honor:
        return ()
    rank = discarded_type.rank
    if rank is None:
        return ()
    suit_start = discarded_type.index - rank + 1
    actions: list[Action] = []
    for start_rank in range(max(1, rank - 2), min(7, rank) + 1):
        needed_ranks = tuple(
            candidate for candidate in range(start_rank, start_rank + 3) if candidate != rank
        )
        needed_types = tuple(TileType(suit_start + needed_rank - 1) for needed_rank in needed_ranks)
        consumed = _maybe_first_tiles_of_types(hand, needed_types)
        if consumed is not None:
            actions.append(Action(ActionKind.CHI, discarded_type, consumed=consumed))
    return tuple(actions)


def _maybe_first_tiles_of_types(
    hand: tuple[Tile, ...],
    tile_types: tuple[TileType, ...],
) -> tuple[Tile, ...] | None:
    consumed: list[Tile] = []
    used_indices: set[int] = set()
    for tile_type in tile_types:
        for index, tile in enumerate(hand):
            if index in used_indices or tile.type != tile_type:
                continue
            consumed.append(tile)
            used_indices.add(index)
            break
        else:
            return None
    return tuple(consumed)


def _first_tiles_of_type(
    hand: tuple[Tile, ...],
    tile_type: TileType,
    count: int,
) -> tuple[Tile, ...]:
    tiles = tuple(tile for tile in hand if tile.type == tile_type)
    if len(tiles) < count:
        raise ValueError(f"not enough {tile_type.notation} tiles in hand")
    return tiles[:count]


def _tsumo_rewards(winner_seat: int, players: int) -> tuple[float, ...]:
    loser_reward = -1.0 / (players - 1)
    return tuple(1.0 if seat == winner_seat else loser_reward for seat in range(players))


def _multi_ron_rewards(
    *,
    winner_seats: tuple[int, ...],
    discarder_seat: int,
    players: int,
) -> tuple[float, ...]:
    winners = set(winner_seats)
    return tuple(
        1.0 if seat in winners else -float(len(winners)) if seat == discarder_seat else 0.0
        for seat in range(players)
    )


def _neutral_rewards(players: int) -> tuple[float, ...]:
    return tuple(0.0 for _seat in range(players))


def _pending_ron_seats(state: SandboxEnvironmentState) -> tuple[int, ...]:
    pending_discard = _pending_discard(state)
    return _legal_ron_seats_for_tile(
        state,
        tile=pending_discard,
        candidate_seats=state.pending_reaction_seats,
        win_kind="ron",
    )


def _is_discard_furiten(state: SandboxEnvironmentState, *, seat: int) -> bool:
    own_discard_types = {tile.type for tile in _discards_by_seat(state)[seat]}
    if not own_discard_types:
        return False
    wait_types = _winning_wait_types(state, seat=seat)
    return any(wait_type in own_discard_types for wait_type in wait_types)


def _is_temporary_furiten(state: SandboxEnvironmentState, *, seat: int) -> bool:
    return seat in state.temporary_furiten_seats


def _is_riichi(state: SandboxEnvironmentState, *, seat: int) -> bool:
    return seat in state.riichi_seats


def _is_post_riichi_discard_locked(state: SandboxEnvironmentState, *, seat: int) -> bool:
    return _is_riichi(state, seat=seat) and seat not in state.riichi_pending_discard_seats


def _is_riichi_furiten(state: SandboxEnvironmentState, *, seat: int) -> bool:
    return seat in state.riichi_furiten_seats


def _winning_wait_types(state: SandboxEnvironmentState, *, seat: int) -> tuple[TileType, ...]:
    hand = state.hands[seat]
    melds = _melds_by_seat(state)[seat]
    if len(hand) != 13 - 3 * len(melds):
        return ()
    return _winning_wait_types_for_hand_and_melds(state, hand=hand, melds=melds)


def _winning_wait_types_for_hand_and_melds(
    state: SandboxEnvironmentState,
    *,
    hand: tuple[Tile, ...],
    melds: tuple[Meld, ...],
) -> tuple[TileType, ...]:
    owned_counts = _hand_type_counts(
        (*hand, *(tile for meld in melds for tile in meld.tiles))
    )
    rules = SANDBOX_RULESET_BY_NAME[state.ruleset]
    waits: list[TileType] = []
    for tile_type in rules.tile_types:
        if owned_counts[tile_type.index] >= rules.type_counts[tile_type.index]:
            continue
        if _winning_shapes_for_concealed_and_melds((*hand, Tile(tile_type)), melds):
            waits.append(tile_type)
    return tuple(waits)


def _with_seat(seats: tuple[int, ...], seat: int) -> tuple[int, ...]:
    if seat in seats:
        return seats
    return tuple(sorted((*seats, seat)))


def _without_seat(seats: tuple[int, ...], seat: int) -> tuple[int, ...]:
    return tuple(candidate for candidate in seats if candidate != seat)


def _require_pending_reaction(state: SandboxEnvironmentState) -> None:
    if not _has_pending_reaction(state):
        raise ValueError("no pending reaction")


def _require_pending_discard(state: SandboxEnvironmentState) -> None:
    if state.pending_discard is None:
        raise ValueError("no pending discard reaction")


def _require_pending_chankan(state: SandboxEnvironmentState) -> None:
    if state.pending_chankan_tile is None:
        raise ValueError("no pending chankan reaction")


def _require_reaction_seat(state: SandboxEnvironmentState, seat: int) -> None:
    if not 0 <= seat < state.players:
        raise ValueError("reaction seat outside player range")
    if seat not in state.pending_reaction_seats:
        raise ValueError("seat cannot react to this pending discard")


def _pending_discard(state: SandboxEnvironmentState) -> Tile:
    if state.pending_discard is None:
        raise ValueError("no pending discard reaction")
    return state.pending_discard


def _pending_discard_seat(state: SandboxEnvironmentState) -> int:
    if state.pending_discard_seat is None:
        raise ValueError("no pending discard seat")
    return state.pending_discard_seat


def _pending_chankan_tile(state: SandboxEnvironmentState) -> Tile:
    if state.pending_chankan_tile is None:
        raise ValueError("no pending chankan reaction")
    return state.pending_chankan_tile


def _pending_chankan_seat(state: SandboxEnvironmentState) -> int:
    if state.pending_chankan_seat is None:
        raise ValueError("no pending chankan seat")
    return state.pending_chankan_seat


def _pending_ron_tile(state: SandboxEnvironmentState) -> Tile:
    if state.pending_discard is not None:
        return state.pending_discard
    return _pending_chankan_tile(state)


def _pending_ron_source_seat(state: SandboxEnvironmentState) -> int:
    if state.pending_discard is not None:
        return _pending_discard_seat(state)
    return _pending_chankan_seat(state)
