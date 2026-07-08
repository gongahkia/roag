from __future__ import annotations

import unittest

from kenjaku.core import (
    ActionKind,
    Meld,
    Tile,
    calculate_fu,
    score_limit,
    score_riichi_hand,
)


class RiichiScoringTests(unittest.TestCase):
    def test_payment_and_limit_fixtures(self) -> None:
        fixtures = [
            (1, 30, False, "ron", 1000, None, None, None),
            (1, 40, False, "ron", 1300, None, None, None),
            (1, 50, False, "ron", 1600, None, None, None),
            (1, 60, False, "ron", 2000, None, None, None),
            (2, 25, False, "ron", 1600, None, None, None),
            (2, 30, False, "ron", 2000, None, None, None),
            (2, 40, False, "ron", 2600, None, None, None),
            (2, 50, False, "ron", 3200, None, None, None),
            (2, 60, False, "ron", 3900, None, None, None),
            (3, 25, False, "ron", 3200, None, None, None),
            (3, 30, False, "ron", 3900, None, None, None),
            (3, 40, False, "ron", 5200, None, None, None),
            (3, 50, False, "ron", 6400, None, None, None),
            (3, 60, False, "ron", 7700, None, None, None),
            (3, 70, False, "ron", 8000, None, None, "mangan"),
            (4, 25, False, "ron", 6400, None, None, None),
            (4, 30, False, "ron", 7700, None, None, None),
            (4, 40, False, "ron", 8000, None, None, "mangan"),
            (5, 30, False, "ron", 8000, None, None, "mangan"),
            (6, 30, False, "ron", 12000, None, None, "haneman"),
            (8, 30, False, "ron", 16000, None, None, "baiman"),
            (11, 30, False, "ron", 24000, None, None, "sanbaiman"),
            (13, 30, False, "ron", 32000, None, None, "yakuman"),
            (1, 30, False, "chankan", 1000, None, None, None),
            (1, 30, True, "ron", 1500, None, None, None),
            (2, 30, True, "ron", 2900, None, None, None),
            (3, 30, True, "ron", 5800, None, None, None),
            (4, 30, True, "ron", 11600, None, None, None),
            (5, 30, True, "ron", 12000, None, None, "mangan"),
            (6, 30, True, "ron", 18000, None, None, "haneman"),
            (8, 30, True, "ron", 24000, None, None, "baiman"),
            (11, 30, True, "ron", 36000, None, None, "sanbaiman"),
            (13, 30, True, "ron", 48000, None, None, "yakuman"),
            (1, 30, False, "tsumo", None, 300, 500, None),
            (2, 30, False, "tsumo", None, 500, 1000, None),
            (3, 30, False, "tsumo", None, 1000, 2000, None),
            (3, 40, False, "tsumo", None, 1300, 2600, None),
            (4, 30, False, "tsumo", None, 2000, 3900, None),
            (5, 30, False, "tsumo", None, 2000, 4000, "mangan"),
            (1, 30, True, "tsumo", None, 500, None, None),
            (2, 30, True, "tsumo", None, 1000, None, None),
            (3, 30, True, "tsumo", None, 2000, None, None),
            (5, 30, True, "tsumo", None, 4000, None, "mangan"),
        ]
        self.assertGreaterEqual(len(fixtures), 40)

        for han, fu, dealer, win_kind, ron, child, oya, limit in fixtures:
            with self.subTest(han=han, fu=fu, dealer=dealer, win_kind=win_kind):
                result = score_riichi_hand(
                    yaku_han=han,
                    fu=fu,
                    is_dealer=dealer,
                    win_kind=win_kind,
                )
                self.assertEqual(result.ron_payment, ron)
                self.assertEqual(result.tsumo_child_payment, child)
                self.assertEqual(result.tsumo_dealer_payment, oya)
                self.assertEqual(result.limit, limit)

    def test_special_limit_and_counter_fixtures(self) -> None:
        fixtures = [
            {"han": 4, "fu": 30, "kiriage": True, "ron": 8000, "limit": "mangan"},
            {"han": 3, "fu": 60, "kiriage": True, "ron": 8000, "limit": "mangan"},
            {"han": 13, "fu": 30, "counted_yakuman": False, "ron": 24000, "limit": "sanbaiman"},
            {"yakuman_multiplier": 2, "dealer": False, "ron": 64000, "limit": "yakuman"},
            {"yakuman_multiplier": 2, "dealer": True, "ron": 96000, "limit": "yakuman"},
        ]
        for fixture in fixtures:
            with self.subTest(fixture=fixture):
                result = score_riichi_hand(
                    yaku_han=fixture.get("han", 0),
                    fu=fixture.get("fu"),
                    is_dealer=fixture.get("dealer", False),
                    win_kind="ron",
                    kiriage=fixture.get("kiriage", False),
                    counted_yakuman=fixture.get("counted_yakuman", True),
                    yakuman_multiplier=fixture.get("yakuman_multiplier", 0),
                )
                self.assertEqual(result.ron_payment, fixture["ron"])
                self.assertEqual(result.limit, fixture["limit"])

        honba = score_riichi_hand(
            yaku_han=1,
            fu=30,
            is_dealer=False,
            win_kind="ron",
            honba=2,
            riichi_sticks=3,
        )
        self.assertEqual(honba.honba_payment, 600)
        self.assertEqual(honba.total_ron_payment, 1600)
        self.assertEqual(honba.riichi_stick_points, 3000)
        self.assertEqual(honba.winner_total_points, 4600)

        sanma = score_riichi_hand(
            yaku_han=2,
            fu=30,
            is_dealer=False,
            win_kind="tsumo",
            players=3,
        )
        self.assertEqual(sanma.tsumo_child_payment, 500)
        self.assertEqual(sanma.tsumo_dealer_payment, 1000)
        self.assertEqual(sanma.winner_total_points, 1500)

    def test_score_limit_fixtures(self) -> None:
        self.assertIsNone(score_limit(han=4, fu=30))
        self.assertEqual(score_limit(han=4, fu=30, kiriage=True), "mangan")
        self.assertEqual(score_limit(han=4, fu=40), "mangan")
        self.assertEqual(score_limit(han=6, fu=30), "haneman")
        self.assertEqual(score_limit(han=8, fu=30), "baiman")
        self.assertEqual(score_limit(han=11, fu=30), "sanbaiman")
        self.assertEqual(score_limit(han=13, fu=30), "yakuman")
        self.assertEqual(score_limit(han=13, fu=30, counted_yakuman=False), "sanbaiman")

    def test_tenhou_sanma_exact_payment_fixtures(self) -> None:
        fixtures = [
            {
                "name": "child ron",
                "yaku_han": 1,
                "fu": 30,
                "dealer": False,
                "win_kind": "ron",
                "ron": 1000,
                "total_ron": 1000,
                "winner_total": 1000,
            },
            {
                "name": "dealer ron",
                "yaku_han": 1,
                "fu": 30,
                "dealer": True,
                "win_kind": "ron",
                "ron": 1500,
                "total_ron": 1500,
                "winner_total": 1500,
            },
            {
                "name": "child tsumo tsumo loss",
                "yaku_han": 2,
                "fu": 30,
                "dealer": False,
                "win_kind": "tsumo",
                "child": 500,
                "dealer_payment": 1000,
                "total_child": 500,
                "total_dealer": 1000,
                "winner_total": 1500,
            },
            {
                "name": "dealer tsumo tsumo loss",
                "yaku_han": 2,
                "fu": 30,
                "dealer": True,
                "win_kind": "tsumo",
                "child": 1000,
                "dealer_payment": None,
                "total_child": 1000,
                "total_dealer": None,
                "winner_total": 2000,
            },
            {
                "name": "kita bonus han child ron",
                "yaku_han": 1,
                "bonus_han": 1,
                "fu": 30,
                "dealer": False,
                "win_kind": "ron",
                "ron": 2000,
                "total_ron": 2000,
                "winner_total": 2000,
                "han": 2,
            },
            {
                "name": "child ron with honba and riichi sticks",
                "yaku_han": 1,
                "fu": 30,
                "dealer": False,
                "win_kind": "ron",
                "honba": 2,
                "riichi_sticks": 1,
                "ron": 1000,
                "honba_payment": 600,
                "riichi_stick_points": 1000,
                "total_ron": 1600,
                "winner_total": 2600,
            },
            {
                "name": "child tsumo with honba",
                "yaku_han": 2,
                "fu": 30,
                "dealer": False,
                "win_kind": "tsumo",
                "honba": 1,
                "child": 500,
                "dealer_payment": 1000,
                "honba_payment": 100,
                "total_child": 600,
                "total_dealer": 1100,
                "winner_total": 1700,
            },
            {
                "name": "child mangan tsumo tsumo loss",
                "yaku_han": 5,
                "fu": 30,
                "dealer": False,
                "win_kind": "tsumo",
                "child": 2000,
                "dealer_payment": 4000,
                "total_child": 2000,
                "total_dealer": 4000,
                "winner_total": 6000,
                "limit": "mangan",
            },
            {
                "name": "dealer mangan tsumo tsumo loss",
                "yaku_han": 5,
                "fu": 30,
                "dealer": True,
                "win_kind": "tsumo",
                "child": 4000,
                "dealer_payment": None,
                "total_child": 4000,
                "total_dealer": None,
                "winner_total": 8000,
                "limit": "mangan",
            },
        ]

        for fixture in fixtures:
            with self.subTest(fixture=fixture["name"]):
                result = score_riichi_hand(
                    yaku_han=fixture["yaku_han"],
                    bonus_han=fixture.get("bonus_han", 0),
                    fu=fixture["fu"],
                    is_dealer=fixture["dealer"],
                    win_kind=fixture["win_kind"],
                    honba=fixture.get("honba", 0),
                    riichi_sticks=fixture.get("riichi_sticks", 0),
                    players=3,
                )
                self.assertEqual(result.han, fixture.get("han", fixture["yaku_han"]))
                self.assertEqual(result.ron_payment, fixture.get("ron"))
                self.assertEqual(result.tsumo_child_payment, fixture.get("child"))
                self.assertEqual(result.tsumo_dealer_payment, fixture.get("dealer_payment"))
                self.assertEqual(result.honba_payment, fixture.get("honba_payment", 0))
                self.assertEqual(
                    result.riichi_stick_points,
                    fixture.get("riichi_stick_points", 0),
                )
                self.assertEqual(result.total_ron_payment, fixture.get("total_ron"))
                self.assertEqual(result.total_tsumo_child_payment, fixture.get("total_child"))
                self.assertEqual(result.total_tsumo_dealer_payment, fixture.get("total_dealer"))
                self.assertEqual(result.winner_total_points, fixture["winner_total"])
                self.assertEqual(result.limit, fixture.get("limit"))

    def test_fu_fixtures(self) -> None:
        fixtures = [
            ("closed pinfu ron", _closed_pinfu(), "8m", "ron", (), (), None, None, 30, 30),
            (
                "closed pinfu tsumo",
                _closed_pinfu(),
                "8m",
                "tsumo",
                (),
                ("pinfu",),
                None,
                None,
                20,
                20,
            ),
            (
                "chiitoitsu fixed",
                _seven_pairs(),
                "E",
                "ron",
                (),
                ("chiitoitsu",),
                None,
                None,
                25,
                25,
            ),
            ("pair wait", _closed_pinfu(), "5p", "ron", (), (), None, None, 40, 32),
            ("closed wait", _closed_pinfu(), "2m", "ron", (), (), None, None, 40, 32),
            ("edge wait", _closed_pinfu(), "3m", "ron", (), (), None, None, 40, 32),
            ("dragon pair tsumo", _value_pair("P"), "8m", "tsumo", (), (), None, None, 30, 24),
            ("seat wind pair", _value_pair("E"), "8m", "ron", (), (), "E", None, 40, 32),
            ("round wind pair", _value_pair("E"), "8m", "ron", (), (), None, "E", 40, 32),
            ("double wind pair", _value_pair("E"), "8m", "ron", (), (), "E", "E", 40, 34),
            ("closed simple triplet", _triplet_hand("2m"), "8m", "ron", (), (), None, None, 40, 34),
            ("ron opens triplet", _triplet_hand("2m"), "2m", "ron", (), (), None, None, 40, 32),
            (
                "closed terminal triplet",
                _triplet_hand("1m"),
                "8m",
                "tsumo",
                (),
                (),
                None,
                None,
                30,
                30,
            ),
            ("open simple pon", _open_base(), "8m", "ron", (_pon("2m"),), (), None, None, 30, 22),
            ("open terminal pon", _open_base(), "8m", "ron", (_pon("1m"),), (), None, None, 30, 24),
            (
                "closed simple kan",
                _open_base(),
                "8m",
                "ron",
                (_ankan("2m"),),
                (),
                None,
                None,
                50,
                46,
            ),
            (
                "closed terminal kan",
                _open_base(),
                "8m",
                "ron",
                (_ankan("1m"),),
                (),
                None,
                None,
                70,
                62,
            ),
            (
                "open simple kan",
                _open_base(),
                "8m",
                "ron",
                (_minkan("2m"),),
                (),
                None,
                None,
                30,
                28,
            ),
            (
                "open terminal kan",
                _open_base(),
                "8m",
                "ron",
                (_minkan("1m"),),
                (),
                None,
                None,
                40,
                36,
            ),
            (
                "open pinfu shape",
                _open_pinfu_base(),
                "8m",
                "ron",
                (_chi("1m 2m 3m"),),
                (),
                None,
                None,
                30,
                22,
            ),
        ]
        self.assertGreaterEqual(len(fixtures), 20)

        for name, tiles, win, kind, melds, yaku, seat, round_, fu, unrounded in fixtures:
            with self.subTest(name=name):
                result = calculate_fu(
                    _tiles(tiles),
                    winning_tile=Tile.parse(win),
                    win_kind=kind,
                    melds=melds,
                    yaku=yaku,
                    seat_wind=seat,
                    round_wind=round_,
                )
                self.assertEqual(result.fu, fu)
                self.assertEqual(result.unrounded_fu, unrounded)


def _tiles(text: str) -> tuple[Tile, ...]:
    return tuple(Tile.parse(token) for token in text.split())


def _closed_pinfu() -> str:
    return "1m 2m 3m 2p 3p 4p 4s 5s 6s 6m 7m 8m 5p 5p"


def _seven_pairs() -> str:
    return "1m 1m 2m 2m 3p 3p 4p 4p 5s 5s 6s 6s E E"


def _value_pair(tile: str) -> str:
    return f"1m 2m 3m 2p 3p 4p 4s 5s 6s 6m 7m 8m {tile} {tile}"


def _triplet_hand(tile: str) -> str:
    return f"{tile} {tile} {tile} 2p 3p 4p 4s 5s 6s 6m 7m 8m 5p 5p"


def _open_base() -> str:
    return "2p 3p 4p 4s 5s 6s 6m 7m 8m 5p 5p"


def _open_pinfu_base() -> str:
    return "2p 3p 4p 4s 5s 6s 6m 7m 8m 5p 5p"


def _pon(tile: str) -> Meld:
    tiles = _tiles(f"{tile} {tile} {tile}")
    return Meld(ActionKind.PON, tiles, called_tile=tiles[0], from_seat=1)


def _minkan(tile: str) -> Meld:
    tiles = _tiles(f"{tile} {tile} {tile} {tile}")
    return Meld(ActionKind.MINKAN, tiles, called_tile=tiles[0], from_seat=1)


def _ankan(tile: str) -> Meld:
    return Meld(ActionKind.ANKAN, _tiles(f"{tile} {tile} {tile} {tile}"))


def _chi(text: str) -> Meld:
    tiles = _tiles(text)
    return Meld(ActionKind.CHI, tiles, called_tile=tiles[0], from_seat=1)
