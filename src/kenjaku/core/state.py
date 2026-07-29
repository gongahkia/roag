from __future__ import annotations

from dataclasses import dataclass
from itertools import chain

from kenjaku.core.actions import ActionKind
from kenjaku.core.rules import TENHOU_4P, RuleSet
from kenjaku.core.tiles import Tile


def _check_seat(seat: int, players: int) -> None:
    if not 0 <= seat < players:
        raise ValueError(f"seat {seat} outside player range 0..{players - 1}")


@dataclass(frozen=True, slots=True)
class Discard:
    tile: Tile
    tsumogiri: bool = False
    riichi_declared: bool = False


@dataclass(frozen=True, slots=True)
class Meld:
    kind: ActionKind
    tiles: tuple[Tile, ...]
    called_tile: Tile | None = None
    from_seat: int | None = None

    def __post_init__(self) -> None:
        if self.kind not in {
            ActionKind.CHI,
            ActionKind.PON,
            ActionKind.MINKAN,
            ActionKind.ANKAN,
            ActionKind.KAKAN,
            ActionKind.KITA,
        }:
            raise ValueError(f"{self.kind.value} is not a meld kind")
        if not self.tiles:
            raise ValueError("melds must contain at least one visible tile")


@dataclass(frozen=True, slots=True)
class PlayerState:
    seat: int
    hand: tuple[Tile, ...] = ()
    discards: tuple[Discard, ...] = ()
    melds: tuple[Meld, ...] = ()
    riichi_turn: int | None = None

    def visible_tiles(self, *, include_hand: bool) -> tuple[Tile, ...]:
        hand = self.hand if include_hand else ()
        discards = tuple(discard.tile for discard in self.discards)
        melds = tuple(chain.from_iterable(meld.tiles for meld in self.melds))
        return (*hand, *discards, *melds)


@dataclass(frozen=True, slots=True)
class RoundState:
    """A player-perspective round snapshot.

    Only the perspective player's hand should be populated from real games.
    Other hands must stay empty unless the data source is explicitly omniscient.
    """

    rules: RuleSet = TENHOU_4P
    dealer: int = 0
    honba: int = 0
    kyotaku: int = 0
    scores: tuple[int, ...] = (25000, 25000, 25000, 25000)
    dora_indicators: tuple[Tile, ...] = ()
    players: tuple[PlayerState, ...] = (
        PlayerState(0),
        PlayerState(1),
        PlayerState(2),
        PlayerState(3),
    )
    turn: int = 0
    current_seat: int = 0

    def __post_init__(self) -> None:
        if len(self.players) != self.rules.players:
            raise ValueError("player state count must match ruleset")
        if len(self.scores) != self.rules.players:
            raise ValueError("score count must match ruleset")
        _check_seat(self.dealer, self.rules.players)
        _check_seat(self.current_seat, self.rules.players)
        seen = {player.seat for player in self.players}
        if seen != set(range(self.rules.players)):
            raise ValueError("players must cover each seat exactly once")

    def player(self, seat: int) -> PlayerState:
        _check_seat(seat, self.rules.players)
        for player in self.players:
            if player.seat == seat:
                return player
        raise ValueError(f"missing player state for seat {seat}")

    def visible_tiles(self, perspective: int) -> tuple[Tile, ...]:
        _check_seat(perspective, self.rules.players)
        table_tiles = tuple(self.dora_indicators)
        player_tiles = tuple(
            chain.from_iterable(
                player.visible_tiles(include_hand=player.seat == perspective)
                for player in self.players
            )
        )
        return (*table_tiles, *player_tiles)

    def unseen_type_counts(self, perspective: int) -> tuple[int, ...]:
        counts = list(self.rules.type_counts)
        for tile in self.visible_tiles(perspective):
            counts[tile.type.index] -= 1
            if counts[tile.type.index] < 0:
                raise ValueError(f"visible tile count exceeds wall count for {tile.type.notation}")
        return tuple(counts)
