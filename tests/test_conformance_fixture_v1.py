from __future__ import annotations

import json
import unittest

from kenjaku.schema import (
    CONFORMANCE_FIXTURE_V1_KIND,
    ActionV1,
    ConformanceFixtureV1,
)


class ConformanceFixtureV1Tests(unittest.TestCase):
    def test_round_trips_self_contained_four_player_fixture(self) -> None:
        fixture = ConformanceFixtureV1(
            fixture_id="four-player-discard-window",
            ruleset="tenhou-4p",
            scenario="legal_actions",
            seed="conformance:4p:discard-window",
            tags=("discard", "priority"),
            setup={"current_seat": 0, "hand": ["1m", "2m", "3m", "4m"]},
            actions=(ActionV1(ruleset="tenhou-4p", action="discard", tile="4m"),),
            expected={"legal_actions": ["discard:4m"]},
        )
        payload = fixture.to_dict()

        self.assertEqual(payload["kind"], CONFORMANCE_FIXTURE_V1_KIND)
        self.assertEqual(ConformanceFixtureV1.from_dict(payload), fixture)
        self.assertEqual(json.loads(fixture.to_json()), payload)
        payload["setup"]["current_seat"] = 1
        self.assertEqual(fixture.setup["current_seat"], 0)

    def test_supports_deterministic_sanma_kita_scenarios(self) -> None:
        fixture = ConformanceFixtureV1(
            fixture_id="sanma-kita-window",
            ruleset="tenhou-3p",
            scenario="kita_timing",
            seed="conformance:3p:kita-window",
            tags=("kita", "sanma"),
            setup={"current_seat": 1, "hand": ["N"]},
            actions=(ActionV1(ruleset="tenhou-3p", action="kita", tile="N"),),
            expected={"reaction_window": "kita"},
        )

        self.assertEqual(fixture.to_dict()["actions"][0]["action"], "kita")

    def test_rejects_cross_ruleset_actions_and_malformed_json_objects(self) -> None:
        with self.assertRaisesRegex(ValueError, "must match fixture ruleset"):
            ConformanceFixtureV1(
                fixture_id="invalid-ruleset-action",
                ruleset="tenhou-4p",
                scenario="legal_actions",
                seed="fixed",
                tags=(),
                setup={"state": "synthetic"},
                actions=(ActionV1(ruleset="tenhou-3p", action="kita", tile="N"),),
                expected={"legal": False},
            )
        payload = _fixture_payload()
        payload["expected"] = {"score": float("nan")}
        with self.assertRaisesRegex(ValueError, "finite JSON values"):
            ConformanceFixtureV1.from_dict(payload)
        payload = _fixture_payload()
        payload["extra"] = True
        with self.assertRaisesRegex(ValueError, "unexpected=extra"):
            ConformanceFixtureV1.from_dict(payload)


def _fixture_payload() -> dict[str, object]:
    return ConformanceFixtureV1(
        fixture_id="fixture",
        ruleset="tenhou-4p",
        scenario="legal_actions",
        seed="fixed",
        tags=(),
        setup={"state": "synthetic"},
        actions=(),
        expected={"legal": True},
    ).to_dict()
