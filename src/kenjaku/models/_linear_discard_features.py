from __future__ import annotations

from collections.abc import Sequence

from kenjaku.core import Action, Tile, TileType, shanten
from kenjaku.features.discard import FeatureProfile as _FeatureProfile
from kenjaku.models._linear_base import PreparedExample, prepared_softmax_example
from kenjaku.training import DiscardExample
from kenjaku.training.defense_features import (
    candidate_has_kabe,
    candidate_has_one_chance,
    candidate_has_sotogawa,
    candidate_has_suji,
    candidate_is_genbutsu,
    candidate_seen_after_riichi,
    candidate_seen_before_riichi,
    has_active_riichi_opponent,
    max_active_riichi_discards_elapsed,
    min_active_riichi_discards_elapsed,
)

_PreparedExample = PreparedExample[int]


def prepare_examples(
    examples: Sequence[DiscardExample],
    *,
    profile: _FeatureProfile,
) -> list[_PreparedExample]:
    prepared: list[_PreparedExample] = []
    for example in examples:
        if example.action.tile is None:
            raise ValueError("discard examples must have tile actions")
        legal_indices = legal_indices_for_hand(example.hand_counts)
        target = example.action.tile.index
        if target not in legal_indices:
            raise ValueError("discard action must be legal for the example hand")
        prepared.append(
            prepared_softmax_example(
                label=target,
                features_by_label=feature_vectors(
                    example.hand_counts,
                    example.visible_counts,
                    legal_indices,
                    profile=profile,
                    seat=example.seat,
                    active_riichi_seats=example.active_riichi_seats,
                    river_counts_by_seat=example.river_counts_by_seat,
                    rivers_by_seat=example.rivers_by_seat,
                    riichi_declared_turns=example.riichi_declared_turns,
                    riichi_declared_event_indices=example.riichi_declared_event_indices,
                    meld_counts_by_seat=example.meld_counts_by_seat,
                    dora_indicators=example.dora_indicators,
                    last_discard_tsumogiri_by_seat=example.last_discard_tsumogiri_by_seat,
                    ippatsu_active_seats=example.ippatsu_active_seats,
                ),
            )
        )
    return prepared


def feature_vectors(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    legal_indices: tuple[int, ...],
    *,
    profile: _FeatureProfile,
    seat: int = 0,
    active_riichi_seats: tuple[bool, ...] = (),
    river_counts_by_seat: tuple[tuple[int, ...], ...] = (),
    rivers_by_seat: tuple[tuple[Tile, ...], ...] = (),
    riichi_declared_turns: tuple[int | None, ...] = (),
    riichi_declared_event_indices: tuple[int | None, ...] = (),
    meld_counts_by_seat: tuple[tuple[int, ...], ...] = (),
    dora_indicators: tuple[Tile, ...] = (),
    last_discard_tsumogiri_by_seat: tuple[bool | None, ...] = (),
    ippatsu_active_seats: tuple[bool, ...] = (),
) -> dict[int, tuple[float, ...]]:
    if len(hand_counts) != 34:
        raise ValueError("hand counts must have length 34")
    if len(visible_counts) != 34:
        raise ValueError("visible counts must have length 34")
    if not profile.includes_tile_efficiency:
        features = _raw_features(hand_counts, visible_counts)
        return {tile_index: features for tile_index in legal_indices}

    before_shanten = shanten(hand_counts)
    return {
        tile_index: _features(
            hand_counts,
            visible_counts,
            tile_index=tile_index,
            before_shanten=before_shanten,
            profile=profile,
            seat=seat,
            active_riichi_seats=active_riichi_seats,
            river_counts_by_seat=river_counts_by_seat,
            rivers_by_seat=rivers_by_seat,
            riichi_declared_turns=riichi_declared_turns,
            riichi_declared_event_indices=riichi_declared_event_indices,
            meld_counts_by_seat=meld_counts_by_seat,
            dora_indicators=dora_indicators,
            last_discard_tsumogiri_by_seat=last_discard_tsumogiri_by_seat,
            ippatsu_active_seats=ippatsu_active_seats,
        )
        for tile_index in legal_indices
    }


def _features(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    *,
    tile_index: int,
    before_shanten: int,
    profile: _FeatureProfile,
    seat: int,
    active_riichi_seats: tuple[bool, ...],
    river_counts_by_seat: tuple[tuple[int, ...], ...],
    rivers_by_seat: tuple[tuple[Tile, ...], ...],
    riichi_declared_turns: tuple[int | None, ...],
    riichi_declared_event_indices: tuple[int | None, ...],
    meld_counts_by_seat: tuple[tuple[int, ...], ...],
    dora_indicators: tuple[Tile, ...],
    last_discard_tsumogiri_by_seat: tuple[bool | None, ...],
    ippatsu_active_seats: tuple[bool, ...],
) -> tuple[float, ...]:
    after_counts = list(hand_counts)
    after_counts[tile_index] -= 1
    after_shanten = shanten(tuple(after_counts))
    shanten_delta = after_shanten - before_shanten
    features = (
        1.0,
        *(count / 4.0 for count in hand_counts),
        *(count / 4.0 for count in visible_counts),
        hand_counts[tile_index] / 4.0,
        visible_counts[tile_index] / 4.0,
        1.0 if _is_terminal_or_honor(tile_index) else 0.0,
        before_shanten / 8.0,
        after_shanten / 8.0,
        float(shanten_delta),
        1.0 if shanten_delta <= 0 else 0.0,
    )
    if profile.includes_risk_context:
        features = (
            *features,
            *_risk_context_features(
                visible_counts,
                tile_index=tile_index,
                seat=seat,
                active_riichi_seats=active_riichi_seats,
                river_counts_by_seat=river_counts_by_seat,
            ),
        )
    if profile.includes_defense_context:
        features = (
            *features,
            *_defense_context_features(
                hand_counts,
                visible_counts,
                tile_index=tile_index,
                seat=seat,
                active_riichi_seats=active_riichi_seats,
                river_counts_by_seat=river_counts_by_seat,
                rivers_by_seat=rivers_by_seat,
                riichi_declared_turns=riichi_declared_turns,
                riichi_declared_event_indices=riichi_declared_event_indices,
            ),
        )
    if profile.includes_defense_context_v1:
        features = (
            *features,
            *_defense_context_v1_features(
                hand_counts,
                visible_counts,
                tile_index=tile_index,
                seat=seat,
                active_riichi_seats=active_riichi_seats,
                river_counts_by_seat=river_counts_by_seat,
                rivers_by_seat=rivers_by_seat,
                riichi_declared_turns=riichi_declared_turns,
                riichi_declared_event_indices=riichi_declared_event_indices,
                meld_counts_by_seat=meld_counts_by_seat,
                dora_indicators=dora_indicators,
                last_discard_tsumogiri_by_seat=last_discard_tsumogiri_by_seat,
                ippatsu_active_seats=ippatsu_active_seats,
            ),
        )
    return features


def _example_for_candidate_context(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    *,
    tile_index: int,
    seat: int,
    active_riichi_seats: tuple[bool, ...],
    river_counts_by_seat: tuple[tuple[int, ...], ...],
    rivers_by_seat: tuple[tuple[Tile, ...], ...],
    riichi_declared_turns: tuple[int | None, ...],
    riichi_declared_event_indices: tuple[int | None, ...],
    meld_counts_by_seat: tuple[tuple[int, ...], ...] = (),
    dora_indicators: tuple[Tile, ...] = (),
    last_discard_tsumogiri_by_seat: tuple[bool | None, ...] = (),
    ippatsu_active_seats: tuple[bool, ...] = (),
) -> DiscardExample:
    return DiscardExample(
        round_index=0,
        event_index=0,
        seat=seat,
        dealer=0,
        scores=(),
        hand_counts=hand_counts,
        visible_counts=visible_counts,
        action=Action.discard(TileType(tile_index)),
        active_riichi_seats=active_riichi_seats,
        river_counts_by_seat=river_counts_by_seat,
        rivers_by_seat=rivers_by_seat,
        riichi_declared_turns=riichi_declared_turns,
        riichi_declared_event_indices=riichi_declared_event_indices,
        meld_counts_by_seat=meld_counts_by_seat,
        dora_indicators=dora_indicators,
        last_discard_tsumogiri_by_seat=last_discard_tsumogiri_by_seat,
        ippatsu_active_seats=ippatsu_active_seats,
    )


def _defense_context_features(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    *,
    tile_index: int,
    seat: int,
    active_riichi_seats: tuple[bool, ...],
    river_counts_by_seat: tuple[tuple[int, ...], ...],
    rivers_by_seat: tuple[tuple[Tile, ...], ...],
    riichi_declared_turns: tuple[int | None, ...],
    riichi_declared_event_indices: tuple[int | None, ...],
) -> tuple[float, ...]:
    example = _example_for_candidate_context(
        hand_counts,
        visible_counts,
        tile_index=tile_index,
        seat=seat,
        active_riichi_seats=active_riichi_seats,
        river_counts_by_seat=river_counts_by_seat,
        rivers_by_seat=rivers_by_seat,
        riichi_declared_turns=riichi_declared_turns,
        riichi_declared_event_indices=riichi_declared_event_indices,
    )
    has_riichi = has_active_riichi_opponent(example)
    genbutsu = candidate_is_genbutsu(example, tile_index)
    suji = candidate_has_suji(example, tile_index)
    kabe = candidate_has_kabe(example, tile_index)
    one_chance = candidate_has_one_chance(example, tile_index)
    seen_after_riichi = candidate_seen_after_riichi(example, tile_index)
    seen_before_riichi = candidate_seen_before_riichi(example, tile_index)
    max_elapsed = max_active_riichi_discards_elapsed(example)
    min_elapsed = min_active_riichi_discards_elapsed(example)
    unseen_count = max(0, 4 - visible_counts[tile_index])
    tile_type = TileType(tile_index)

    return (
        1.0 if genbutsu else 0.0,
        1.0 if suji else 0.0,
        1.0 if kabe else 0.0,
        1.0 if one_chance else 0.0,
        1.0 if seen_after_riichi else 0.0,
        1.0 if seen_before_riichi else 0.0,
        max_elapsed / 18.0,
        min_elapsed / 18.0,
        1.0 if genbutsu or suji or kabe else 0.0,
        unseen_count / 4.0 if has_riichi and not (genbutsu or suji or kabe) else 0.0,
        1.0 if tile_type.is_honor else 0.0,
        1.0 if tile_type.is_terminal else 0.0,
    )


def _defense_context_v1_features(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    *,
    tile_index: int,
    seat: int,
    active_riichi_seats: tuple[bool, ...],
    river_counts_by_seat: tuple[tuple[int, ...], ...],
    rivers_by_seat: tuple[tuple[Tile, ...], ...],
    riichi_declared_turns: tuple[int | None, ...],
    riichi_declared_event_indices: tuple[int | None, ...],
    meld_counts_by_seat: tuple[tuple[int, ...], ...],
    dora_indicators: tuple[Tile, ...],
    last_discard_tsumogiri_by_seat: tuple[bool | None, ...],
    ippatsu_active_seats: tuple[bool, ...],
) -> tuple[float, ...]:
    example = _example_for_candidate_context(
        hand_counts,
        visible_counts,
        tile_index=tile_index,
        seat=seat,
        active_riichi_seats=active_riichi_seats,
        river_counts_by_seat=river_counts_by_seat,
        rivers_by_seat=rivers_by_seat,
        riichi_declared_turns=riichi_declared_turns,
        riichi_declared_event_indices=riichi_declared_event_indices,
        meld_counts_by_seat=meld_counts_by_seat,
        dora_indicators=dora_indicators,
        last_discard_tsumogiri_by_seat=last_discard_tsumogiri_by_seat,
        ippatsu_active_seats=ippatsu_active_seats,
    )
    active_opponents = active_riichi_opponents_for_context(example)
    active_denominator = max(1, len(active_opponents))
    tile_type = TileType(tile_index)
    safe = (
        candidate_is_genbutsu(example, tile_index)
        or candidate_has_suji(example, tile_index)
        or candidate_has_kabe(example, tile_index)
    )
    unseen_count = max(0, 4 - visible_counts[tile_index])
    active_riichi = bool(active_opponents)
    opponent_seats = _opponent_seats(example.seat, active_riichi_seats, river_counts_by_seat)

    return (
        _genbutsu_active_fraction(example, tile_index, active_opponents, active_denominator),
        _suji_active_fraction(example, tile_type, active_opponents, active_denominator),
        _seen_after_active_fraction(example, tile_index, active_opponents, active_denominator),
        _seen_before_active_fraction(example, tile_index, active_opponents, active_denominator),
        _kabe_adjacent_wall_fraction(visible_counts, tile_type),
        _one_chance_adjacent_fraction(visible_counts, tile_type),
        1.0 if candidate_has_sotogawa(example, tile_index) else 0.0,
        unseen_count / 4.0 if active_riichi and tile_type.is_terminal_or_honor else 0.0,
        unseen_count / 4.0 if active_riichi and not safe else 0.0,
        1.0 if tile_type in _dora_types(dora_indicators) else 0.0,
        1.0 if any(indicator.type == tile_type for indicator in dora_indicators) else 0.0,
        _ippatsu_active_fraction(ippatsu_active_seats, active_opponents, active_denominator),
        _active_tsumogiri_fraction(
            last_discard_tsumogiri_by_seat,
            active_opponents,
            active_denominator,
        ),
        _meld_tile_fraction(meld_counts_by_seat, opponent_seats),
    )


def _raw_features(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
) -> tuple[float, ...]:
    return (
        1.0,
        *(count / 4.0 for count in hand_counts),
        *(count / 4.0 for count in visible_counts),
    )


def _risk_context_features(
    visible_counts: tuple[int, ...],
    *,
    tile_index: int,
    seat: int,
    active_riichi_seats: tuple[bool, ...],
    river_counts_by_seat: tuple[tuple[int, ...], ...],
) -> tuple[float, ...]:
    players = _context_player_count(seat, active_riichi_seats, river_counts_by_seat)
    active_opponents = tuple(
        candidate_seat
        for candidate_seat in range(players)
        if candidate_seat != seat and _active_riichi_at(active_riichi_seats, candidate_seat)
    )
    opponent_seats = tuple(
        candidate_seat for candidate_seat in range(players) if candidate_seat != seat
    )
    active_riichi_river_count = _sum_river_count(
        river_counts_by_seat,
        active_opponents,
        tile_index,
    )
    opponent_river_count = _sum_river_count(river_counts_by_seat, opponent_seats, tile_index)
    self_river_count = _river_count(river_counts_by_seat, seat, tile_index)
    all_river_count = _sum_river_count(river_counts_by_seat, range(players), tile_index)
    unseen_count = max(0, 4 - visible_counts[tile_index])
    opponent_denominator = max(1, players - 1)

    return (
        1.0 if _active_riichi_at(active_riichi_seats, seat) else 0.0,
        len(active_opponents) / opponent_denominator,
        1.0 if active_opponents else 0.0,
        active_riichi_river_count / 4.0,
        1.0 if active_riichi_river_count > 0 else 0.0,
        opponent_river_count / 4.0,
        1.0 if opponent_river_count > 0 else 0.0,
        self_river_count / 4.0,
        all_river_count / 4.0,
        unseen_count / 4.0 if active_opponents else 0.0,
    )


def active_riichi_opponents_for_context(example: DiscardExample) -> tuple[int, ...]:
    return tuple(
        candidate_seat
        for candidate_seat in range(
            _context_player_count(
                example.seat,
                example.active_riichi_seats,
                example.river_counts_by_seat,
            )
        )
        if candidate_seat != example.seat
        and _active_riichi_at(example.active_riichi_seats, candidate_seat)
    )


def _opponent_seats(
    seat: int,
    active_riichi_seats: tuple[bool, ...],
    river_counts_by_seat: tuple[tuple[int, ...], ...],
) -> tuple[int, ...]:
    players = _context_player_count(seat, active_riichi_seats, river_counts_by_seat)
    return tuple(candidate_seat for candidate_seat in range(players) if candidate_seat != seat)


def _genbutsu_active_fraction(
    example: DiscardExample,
    tile_index: int,
    active_opponents: tuple[int, ...],
    denominator: int,
) -> float:
    return (
        sum(
            _river_count(example.river_counts_by_seat, seat, tile_index) > 0
            for seat in active_opponents
        )
        / denominator
    )


def _suji_active_fraction(
    example: DiscardExample,
    tile_type: TileType,
    active_opponents: tuple[int, ...],
    denominator: int,
) -> float:
    safe_indices = _suji_safe_indices(tile_type)
    if not safe_indices:
        return 0.0
    return (
        sum(
            any(
                _river_count(example.river_counts_by_seat, seat, safe_index) > 0
                for safe_index in safe_indices
            )
            for seat in active_opponents
        )
        / denominator
    )


def _seen_after_active_fraction(
    example: DiscardExample,
    tile_index: int,
    active_opponents: tuple[int, ...],
    denominator: int,
) -> float:
    return (
        sum(_river_has_tile_from_riichi(example, seat, tile_index) for seat in active_opponents)
        / denominator
    )


def _seen_before_active_fraction(
    example: DiscardExample,
    tile_index: int,
    active_opponents: tuple[int, ...],
    denominator: int,
) -> float:
    return (
        sum(_river_has_tile_before_riichi(example, seat, tile_index) for seat in active_opponents)
        / denominator
    )


def _kabe_adjacent_wall_fraction(
    visible_counts: tuple[int, ...],
    tile_type: TileType,
) -> float:
    adjacent = _adjacent_suited_indices(tile_type)
    if not adjacent:
        return 0.0
    return sum(visible_counts[index] >= 4 for index in adjacent) / len(adjacent)


def _one_chance_adjacent_fraction(
    visible_counts: tuple[int, ...],
    tile_type: TileType,
) -> float:
    adjacent = _adjacent_suited_indices(tile_type)
    if not adjacent:
        return 0.0
    return sum(visible_counts[index] == 3 for index in adjacent) / len(adjacent)


def _ippatsu_active_fraction(
    ippatsu_active_seats: tuple[bool, ...],
    active_opponents: tuple[int, ...],
    denominator: int,
) -> float:
    return (
        sum(
            seat < len(ippatsu_active_seats) and ippatsu_active_seats[seat]
            for seat in active_opponents
        )
        / denominator
    )


def _active_tsumogiri_fraction(
    last_discard_tsumogiri_by_seat: tuple[bool | None, ...],
    active_opponents: tuple[int, ...],
    denominator: int,
) -> float:
    return (
        sum(
            seat < len(last_discard_tsumogiri_by_seat)
            and last_discard_tsumogiri_by_seat[seat] is True
            for seat in active_opponents
        )
        / denominator
    )


def _meld_tile_fraction(
    meld_counts_by_seat: tuple[tuple[int, ...], ...],
    seats: tuple[int, ...],
) -> float:
    return min(1.0, sum(_meld_tile_count(meld_counts_by_seat, seat) for seat in seats) / 12.0)


def _meld_tile_count(
    meld_counts_by_seat: tuple[tuple[int, ...], ...],
    seat: int,
) -> int:
    if seat >= len(meld_counts_by_seat):
        return 0
    counts = meld_counts_by_seat[seat]
    if len(counts) != 34:
        raise ValueError("meld count rows must have length 34")
    return sum(counts)


def _river_has_tile_from_riichi(example: DiscardExample, seat: int, tile_index: int) -> bool:
    riichi_turn = _riichi_turn(example, seat)
    if riichi_turn is None or seat >= len(example.rivers_by_seat):
        return False
    return any(tile.type.index == tile_index for tile in example.rivers_by_seat[seat][riichi_turn:])


def _river_has_tile_before_riichi(example: DiscardExample, seat: int, tile_index: int) -> bool:
    riichi_turn = _riichi_turn(example, seat)
    if riichi_turn is None or seat >= len(example.rivers_by_seat):
        return False
    return any(tile.type.index == tile_index for tile in example.rivers_by_seat[seat][:riichi_turn])


def _riichi_turn(example: DiscardExample, seat: int) -> int | None:
    if seat >= len(example.riichi_declared_turns):
        return None
    return example.riichi_declared_turns[seat]


def _dora_types(dora_indicators: tuple[Tile, ...]) -> tuple[TileType, ...]:
    return tuple(_dora_type(indicator.type) for indicator in dora_indicators)


def _dora_type(indicator: TileType) -> TileType:
    rank = indicator.rank
    if rank is not None:
        suit_start = indicator.index - rank + 1
        return TileType(suit_start + (rank % 9))
    return TileType(
        {
            27: 28,
            28: 29,
            29: 30,
            30: 27,
            31: 32,
            32: 33,
            33: 31,
        }[indicator.index]
    )


def _suji_safe_indices(tile_type: TileType) -> tuple[int, ...]:
    rank = tile_type.rank
    if rank is None:
        return ()
    suit_start = tile_type.index - rank + 1
    safe_ranks = {
        1: (4,),
        2: (5,),
        3: (6,),
        4: (1, 7),
        5: (2, 8),
        6: (3, 9),
        7: (4,),
        8: (5,),
        9: (6,),
    }[rank]
    return tuple(suit_start + safe_rank - 1 for safe_rank in safe_ranks)


def _adjacent_suited_indices(tile_type: TileType) -> tuple[int, ...]:
    rank = tile_type.rank
    if rank is None:
        return ()
    suit_start = tile_type.index - rank + 1
    adjacent_ranks = tuple(candidate for candidate in (rank - 1, rank + 1) if 1 <= candidate <= 9)
    return tuple(suit_start + adjacent_rank - 1 for adjacent_rank in adjacent_ranks)


def legal_indices(hand_counts: tuple[int, ...]) -> tuple[int, ...]:
    legal_indices = tuple(index for index, count in enumerate(hand_counts) if count > 0)
    if not legal_indices:
        raise ValueError("cannot predict a discard from an empty hand")
    return legal_indices


legal_indices_for_hand = legal_indices


def _is_terminal_or_honor(tile_index: int) -> bool:
    return tile_index >= 27 or tile_index % 9 in {0, 8}


def _context_player_count(
    seat: int,
    active_riichi_seats: tuple[bool, ...],
    river_counts_by_seat: tuple[tuple[int, ...], ...],
) -> int:
    return max(4, seat + 1, len(active_riichi_seats), len(river_counts_by_seat))


def _active_riichi_at(active_riichi_seats: tuple[bool, ...], seat: int) -> bool:
    return seat < len(active_riichi_seats) and active_riichi_seats[seat]


def _sum_river_count(
    river_counts_by_seat: tuple[tuple[int, ...], ...],
    seats: Sequence[int],
    tile_index: int,
) -> int:
    return sum(_river_count(river_counts_by_seat, seat, tile_index) for seat in seats)


def _river_count(
    river_counts_by_seat: tuple[tuple[int, ...], ...],
    seat: int,
    tile_index: int,
) -> int:
    if seat >= len(river_counts_by_seat):
        return 0
    counts = river_counts_by_seat[seat]
    if len(counts) != 34:
        raise ValueError("river count rows must have length 34")
    return counts[tile_index]
