from __future__ import annotations

import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.core import Action, Tile, TileType, tile_counts
from kenjaku.models import (
    DEFENSE_CONTEXT_FEATURE_PROFILE,
    DEFENSE_CONTEXT_V1_FEATURE_PROFILE,
    RAW_COUNT_FEATURE_PROFILE,
    RISK_CONTEXT_FEATURE_PROFILE,
    SHANTEN_FEATURE_PROFILE,
    DiscardLinearModel,
)
from kenjaku.training import DiscardExample


class LinearDiscardModelTests(unittest.TestCase):
    def test_fit_predict_and_score(self) -> None:
        examples = [
            _example(["1m", "2m", "3m"], "1m"),
            _example(["1m", "2m", "3m"], "1m"),
            _example(["4p", "5p", "6p"], "6p"),
            _example(["4p", "5p", "6p"], "6p"),
        ]

        model = DiscardLinearModel.fit(examples, epochs=20, learning_rate=0.2)

        self.assertEqual(
            model.predict(examples[0].hand_counts, examples[0].visible_counts),
            TileType.parse("1m"),
        )
        self.assertEqual(
            model.predict(examples[2].hand_counts, examples[2].visible_counts),
            TileType.parse("6p"),
        )
        self.assertEqual(model.score(examples), 1.0)

    def test_rejects_empty_training_set(self) -> None:
        with self.assertRaises(ValueError):
            DiscardLinearModel.fit([])

    def test_rejects_empty_hand_prediction(self) -> None:
        model = DiscardLinearModel.fit([_example(["1m"], "1m")])

        with self.assertRaises(ValueError):
            model.predict(tuple([0] * 34), tuple([0] * 34))

    def test_round_trips_json_artifact(self) -> None:
        examples = [_example(["1m", "2m"], "1m"), _example(["1m", "2m"], "1m")]
        model = DiscardLinearModel.fit(examples, epochs=3, learning_rate=0.2)
        payload = model.to_dict()

        with TemporaryDirectory() as directory:
            path = Path(directory) / "discard-linear.json"
            model.save(path)
            loaded = DiscardLinearModel.load(path)

        self.assertEqual(payload["kind"], "discard-linear-v1")
        self.assertEqual(payload["feature_dim"], 76)
        self.assertEqual(model.kind, "discard-linear-v1")
        self.assertEqual(model.feature_dim, 76)
        self.assertEqual(loaded.to_dict(), model.to_dict())
        self.assertEqual(loaded.score(examples), model.score(examples))

    def test_l2_round_trips_json_artifact(self) -> None:
        examples = [_example(["1m", "2m"], "1m"), _example(["1m", "2m"], "1m")]
        model = DiscardLinearModel.fit(examples, epochs=3, learning_rate=0.2, l2=0.001)
        payload = model.to_dict()

        with TemporaryDirectory() as directory:
            path = Path(directory) / "discard-linear-l2.json"
            model.save(path)
            loaded = DiscardLinearModel.load(path)

        self.assertEqual(payload["l2"], 0.001)
        self.assertEqual(loaded.l2, 0.001)
        self.assertEqual(loaded.to_dict(), model.to_dict())

    def test_feature_names_match_profile_dimensions(self) -> None:
        profiles = {
            RAW_COUNT_FEATURE_PROFILE: 69,
            SHANTEN_FEATURE_PROFILE: 76,
            RISK_CONTEXT_FEATURE_PROFILE: 86,
            DEFENSE_CONTEXT_FEATURE_PROFILE: 98,
            DEFENSE_CONTEXT_V1_FEATURE_PROFILE: 112,
        }

        for profile, expected_count in profiles.items():
            with self.subTest(profile=profile):
                self.assertEqual(
                    len(DiscardLinearModel.feature_names_for_profile(profile)),
                    expected_count,
                )

    def test_raw_count_profile_round_trips_json_artifact(self) -> None:
        examples = [_example(["1m", "2m"], "1m"), _example(["1m", "2m"], "1m")]
        model = DiscardLinearModel.fit(
            examples,
            epochs=3,
            learning_rate=0.2,
            feature_profile=RAW_COUNT_FEATURE_PROFILE,
        )
        payload = model.to_dict()

        with TemporaryDirectory() as directory:
            path = Path(directory) / "discard-linear-raw-count.json"
            model.save(path)
            loaded = DiscardLinearModel.load(path)

        self.assertEqual(payload["kind"], "discard-linear-raw-count-v0")
        self.assertEqual(payload["feature_profile"], "raw-count")
        self.assertEqual(payload["feature_dim"], 69)
        self.assertEqual(loaded.to_dict(), model.to_dict())
        self.assertEqual(loaded.score(examples), model.score(examples))

    def test_risk_context_profile_round_trips_json_artifact(self) -> None:
        examples = [
            _example(
                ["1m", "2m"],
                "1m",
                active_riichi_seats=(False, True, False, False),
                opponent_river=["1m", "9m"],
            ),
            _example(
                ["1m", "2m"],
                "1m",
                active_riichi_seats=(False, True, False, False),
                opponent_river=["1m", "9m"],
            ),
        ]
        model = DiscardLinearModel.fit(
            examples,
            epochs=3,
            learning_rate=0.2,
            feature_profile=RISK_CONTEXT_FEATURE_PROFILE,
        )
        payload = model.to_dict()

        with TemporaryDirectory() as directory:
            path = Path(directory) / "discard-linear-risk-context.json"
            model.save(path)
            loaded = DiscardLinearModel.load(path)

        prediction = model.predict(
            examples[0].hand_counts,
            examples[0].visible_counts,
            seat=examples[0].seat,
            active_riichi_seats=examples[0].active_riichi_seats,
            river_counts_by_seat=examples[0].river_counts_by_seat,
        )

        self.assertEqual(payload["kind"], "discard-linear-risk-context-v0")
        self.assertEqual(payload["feature_profile"], "risk-context")
        self.assertEqual(payload["feature_dim"], 86)
        self.assertEqual(loaded.to_dict(), model.to_dict())
        self.assertEqual(prediction, TileType.parse("1m"))
        self.assertEqual(loaded.score(examples), model.score(examples))

    def test_defense_context_profile_round_trips_json_artifact(self) -> None:
        examples = [
            _example(
                ["1m", "2m"],
                "1m",
                active_riichi_seats=(False, True, False, False),
                opponent_river=["4m", "1m"],
                riichi_turn=1,
            ),
            _example(
                ["1m", "2m"],
                "1m",
                active_riichi_seats=(False, True, False, False),
                opponent_river=["4m", "1m"],
                riichi_turn=1,
            ),
        ]
        model = DiscardLinearModel.fit(
            examples,
            epochs=3,
            learning_rate=0.2,
            feature_profile=DEFENSE_CONTEXT_FEATURE_PROFILE,
        )
        payload = model.to_dict()

        with TemporaryDirectory() as directory:
            path = Path(directory) / "discard-linear-defense-context.json"
            model.save(path)
            loaded = DiscardLinearModel.load(path)

        prediction = model.predict(
            examples[0].hand_counts,
            examples[0].visible_counts,
            seat=examples[0].seat,
            active_riichi_seats=examples[0].active_riichi_seats,
            river_counts_by_seat=examples[0].river_counts_by_seat,
            rivers_by_seat=examples[0].rivers_by_seat,
            riichi_declared_turns=examples[0].riichi_declared_turns,
            riichi_declared_event_indices=examples[0].riichi_declared_event_indices,
        )

        self.assertEqual(payload["kind"], "discard-linear-defense-context-v0")
        self.assertEqual(payload["feature_profile"], "defense-context")
        self.assertEqual(payload["feature_dim"], 98)
        self.assertEqual(loaded.to_dict(), model.to_dict())
        self.assertEqual(prediction, TileType.parse("1m"))
        self.assertEqual(loaded.score(examples), model.score(examples))

    def test_defense_context_v1_profile_round_trips_json_artifact(self) -> None:
        examples = [
            _example(
                ["1m", "2m"],
                "1m",
                active_riichi_seats=(False, True, False, False),
                opponent_river=["4m", "1m"],
                riichi_turn=1,
                dora_indicators=["9m"],
                last_tsumogiri=(None, True, None, None),
                ippatsu_active=(False, True, False, False),
            ),
            _example(
                ["1m", "2m"],
                "1m",
                active_riichi_seats=(False, True, False, False),
                opponent_river=["4m", "1m"],
                riichi_turn=1,
                dora_indicators=["9m"],
                last_tsumogiri=(None, True, None, None),
                ippatsu_active=(False, True, False, False),
            ),
        ]
        model = DiscardLinearModel.fit(
            examples,
            epochs=3,
            learning_rate=0.2,
            feature_profile=DEFENSE_CONTEXT_V1_FEATURE_PROFILE,
        )
        payload = model.to_dict()

        with TemporaryDirectory() as directory:
            path = Path(directory) / "discard-linear-defense-context-v1.json"
            model.save(path)
            loaded = DiscardLinearModel.load(path)

        prediction = model.predict(
            examples[0].hand_counts,
            examples[0].visible_counts,
            seat=examples[0].seat,
            active_riichi_seats=examples[0].active_riichi_seats,
            river_counts_by_seat=examples[0].river_counts_by_seat,
            rivers_by_seat=examples[0].rivers_by_seat,
            riichi_declared_turns=examples[0].riichi_declared_turns,
            riichi_declared_event_indices=examples[0].riichi_declared_event_indices,
            meld_counts_by_seat=examples[0].meld_counts_by_seat,
            dora_indicators=examples[0].dora_indicators,
            last_discard_tsumogiri_by_seat=examples[0].last_discard_tsumogiri_by_seat,
            ippatsu_active_seats=examples[0].ippatsu_active_seats,
        )

        self.assertEqual(payload["kind"], "discard-linear-defense-context-v1")
        self.assertEqual(payload["feature_profile"], "defense-context-v1")
        self.assertEqual(payload["feature_dim"], 112)
        self.assertEqual(loaded.to_dict(), model.to_dict())
        self.assertEqual(prediction, TileType.parse("1m"))
        self.assertEqual(loaded.score(examples), model.score(examples))


def _example(
    hand: list[str],
    discard: str,
    *,
    active_riichi_seats: tuple[bool, ...] = (),
    opponent_river: list[str] | None = None,
    riichi_turn: int | None = None,
    dora_indicators: list[str] | None = None,
    last_tsumogiri: tuple[bool | None, ...] = (),
    ippatsu_active: tuple[bool, ...] = (),
) -> DiscardExample:
    tiles = tuple(Tile.parse(tile) for tile in hand)
    opponent_river_tiles = tuple(Tile.parse(tile) for tile in opponent_river or [])
    rivers_by_seat = (
        (),
        opponent_river_tiles,
        (),
        (),
    )
    river_counts_by_seat = (
        tuple([0] * 34),
        tile_counts(opponent_river_tiles),
        tuple([0] * 34),
        tuple([0] * 34),
    )
    return DiscardExample(
        round_index=0,
        event_index=0,
        seat=0,
        dealer=0,
        scores=(25000, 25000, 25000, 25000),
        hand_counts=tile_counts(tiles),
        visible_counts=tile_counts((*tiles, *opponent_river_tiles)),
        action=Action.discard(discard),
        active_riichi_seats=active_riichi_seats,
        river_counts_by_seat=river_counts_by_seat,
        rivers_by_seat=rivers_by_seat,
        riichi_declared_turns=(None, riichi_turn, None, None),
        riichi_declared_event_indices=(None, 0 if riichi_turn is not None else None, None, None),
        meld_counts_by_seat=tuple(tuple([0] * 34) for _ in range(4)),
        dora_indicators=tuple(Tile.parse(tile) for tile in dora_indicators or []),
        last_discard_tsumogiri_by_seat=last_tsumogiri,
        ippatsu_active_seats=ippatsu_active,
    )


if __name__ == "__main__":
    unittest.main()
