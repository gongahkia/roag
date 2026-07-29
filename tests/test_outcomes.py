from __future__ import annotations

import unittest

from kenjaku.io import parse_tenhou_xml
from kenjaku.training import round_outcome, round_outcome_payload

AGARI_XML = """
<mjloggm>
  <INIT
    seed="0,0,0,0,0,72"
    ten="250,250,250,250"
    oya="0"
    hai0="0,4,8,12,16,20,24,28,32,36,40,44,48"
    hai1="1,5,9,13,17,21,25,29,33,37,41,45,49"
    hai2="2,6,10,14,18,22,26,30,34,38,42,46,50"
    hai3="3,7,11,15,19,23,27,31,35,39,43,47,51"
  />
  <AGARI who="2" fromWho="1" sc="250,0,230,-20,270,20,250,0" />
</mjloggm>
"""


RYUUKYOKU_XML = """
<mjloggm>
  <INIT
    seed="0,0,0,0,0,72"
    ten="250,250,250,250"
    oya="0"
    hai0="0,4,8,12,16,20,24,28,32,36,40,44,48"
    hai1="1,5,9,13,17,21,25,29,33,37,41,45,49"
    hai2="2,6,10,14,18,22,26,30,34,38,42,46,50"
    hai3="3,7,11,15,19,23,27,31,35,39,43,47,51"
  />
  <RYUUKYOKU sc="260,10,240,-10,260,10,240,-10" />
</mjloggm>
"""


class OutcomeTests(unittest.TestCase):
    def test_agari_outcome_labels_winner_and_deal_in(self) -> None:
        round_ = parse_tenhou_xml(AGARI_XML).rounds[0]
        outcome = round_outcome(round_)

        self.assertIsNotNone(outcome)
        assert outcome is not None
        self.assertEqual(outcome.kind, "agari")
        self.assertEqual(outcome.winner_seats, (2,))
        self.assertEqual(outcome.from_seats, (1,))
        self.assertEqual(outcome.score_deltas, (0, -2000, 2000, 0))
        self.assertEqual(outcome.win_flags, (False, False, True, False))
        self.assertEqual(outcome.deal_in_flags, (False, True, False, False))
        self.assertEqual(outcome.draw_flags, (False, False, False, False))

    def test_ryuukyoku_outcome_labels_draw(self) -> None:
        round_ = parse_tenhou_xml(RYUUKYOKU_XML).rounds[0]
        payload = round_outcome_payload(round_)

        self.assertIsNotNone(payload)
        assert payload is not None
        self.assertEqual(payload["kind"], "ryuukyoku")
        self.assertEqual(payload["score_deltas"], [1000, -1000, 1000, -1000])
        self.assertEqual(payload["win_flags"], [False, False, False, False])
        self.assertEqual(payload["deal_in_flags"], [False, False, False, False])
        self.assertEqual(payload["draw_flags"], [True, True, True, True])


if __name__ == "__main__":
    unittest.main()
