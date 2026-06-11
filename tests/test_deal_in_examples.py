from __future__ import annotations

import unittest

from kenjaku.io import parse_tenhou_xml_file
from kenjaku.training import (
    DEAL_IN_LABEL_SOURCE,
    iter_deal_in_examples,
    summarize_deal_in_examples,
)


class DealInExampleTests(unittest.TestCase):
    def test_labels_only_terminal_ron_discard_as_positive(self) -> None:
        game = parse_tenhou_xml_file("data/fixtures/tenhou/events_4p.xml")

        examples = tuple(iter_deal_in_examples(game))

        self.assertEqual([example.discard.event_index for example in examples], [3, 5])
        self.assertEqual([example.dealt_in for example in examples], [False, True])
        self.assertTrue(all(example.label_source == DEAL_IN_LABEL_SOURCE for example in examples))

    def test_active_riichi_filter_keeps_only_defense_context_examples(self) -> None:
        game = parse_tenhou_xml_file("data/fixtures/tenhou/events_4p.xml")

        examples = tuple(iter_deal_in_examples(game, active_riichi_only=True))

        self.assertEqual(len(examples), 1)
        self.assertTrue(examples[0].dealt_in)
        self.assertEqual(examples[0].discard.seat, 1)

    def test_summarizes_direct_deal_in_labels(self) -> None:
        game = parse_tenhou_xml_file("data/fixtures/tenhou/events_4p.xml")
        examples = tuple(iter_deal_in_examples(game))

        summary = summarize_deal_in_examples(examples)

        self.assertEqual(summary["kind"], "kenjaku-deal-in-label-summary-v0")
        self.assertEqual(summary["label_source"], DEAL_IN_LABEL_SOURCE)
        self.assertEqual(summary["examples"], 2)
        self.assertEqual(summary["direct_deal_in_examples"], 1)
        self.assertEqual(summary["non_deal_in_examples"], 1)
        self.assertEqual(summary["positive_rate"], 0.5)
        self.assertEqual(summary["active_riichi_examples"], 1)
        self.assertEqual(summary["active_riichi_deal_in_examples"], 1)
        self.assertEqual(summary["outcome_kinds"], {"agari": 2})


if __name__ == "__main__":
    unittest.main()
