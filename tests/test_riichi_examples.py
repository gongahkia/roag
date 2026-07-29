from __future__ import annotations

import unittest
from pathlib import Path

from kenjaku.core import ActionKind
from kenjaku.io import parse_tenhou_xml, parse_tenhou_xml_file
from kenjaku.training.riichi_examples import iter_riichi_examples

EVENTS_FIXTURE = Path("data/fixtures/tenhou/events_4p.xml")
SANMA_DECISIONS_FIXTURE = Path("data/fixtures/sanma/sanma_decisions_3p.xml")


class RiichiExampleTests(unittest.TestCase):
    def test_builds_positive_riichi_example_from_fixture(self) -> None:
        game = parse_tenhou_xml_file(EVENTS_FIXTURE)
        examples = list(iter_riichi_examples(game))

        self.assertEqual(len(examples), 1)
        example = examples[0]
        self.assertEqual(example.event_index, 0)
        self.assertEqual(example.riichi_event_index, 0)
        self.assertEqual(example.seat, 0)
        self.assertEqual(example.action.kind, ActionKind.RIICHI)
        self.assertEqual(example.active_riichi_seats, (False, False, False, False))

    def test_builds_negative_closed_tenpai_discard_example(self) -> None:
        game = parse_tenhou_xml(
            """
            <mjloggm>
              <INIT
                seed="0,0,0,0,0,72"
                ten="250,250,250,250"
                oya="0"
                hai0="0,1,4,8,48,52,56,96,100,104,108,109,112"
                hai1="2,5,9,13,17,21,25,29,33,37,41,45,49"
                hai2="3,6,10,14,18,22,26,30,34,38,42,46,50"
                hai3="7,11,15,19,23,27,31,35,39,43,47,51,55"
              />
              <T12 />
              <D12 />
              <RYUUKYOKU />
            </mjloggm>
            """
        )

        examples = list(iter_riichi_examples(game))

        self.assertEqual(len(examples), 1)
        self.assertEqual(examples[0].event_index, 1)
        self.assertEqual(examples[0].seat, 0)
        self.assertEqual(examples[0].action.kind, ActionKind.PASS)
        self.assertEqual(sum(examples[0].hand_counts), 14)

    def test_builds_sanma_positive_riichi_example(self) -> None:
        game = parse_tenhou_xml_file(SANMA_DECISIONS_FIXTURE)

        examples = list(iter_riichi_examples(game))
        positives = [example for example in examples if example.action.kind is ActionKind.RIICHI]

        self.assertEqual(len(positives), 1)
        self.assertEqual(positives[0].seat, 0)
        self.assertEqual(positives[0].active_riichi_seats, (False, False, False))
        self.assertTrue(all(len(example.scores) == 3 for example in examples))


if __name__ == "__main__":
    unittest.main()
