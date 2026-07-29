from __future__ import annotations

import unittest

from kenjaku.core import YAKU_DEFINITIONS, ActionKind, Meld, Tile, detect_yaku


class YakuDetectionTests(unittest.TestCase):
    def test_requested_yaku_positive_fixtures(self) -> None:
        fixtures = [
            ("pinfu", "1m 2m 3m 2p 3p 4p 4s 5s 6s 6m 7m 5p 5p", "8m", ()),
            ("iipeikou", "1m 2m 3m 1m 2m 3m 4p 5p 6p 6s 7s 8s 5p 5p", None, ()),
            ("ryanpeikou", "1m 2m 3m 1m 2m 3m 2m 3m 4m 2m 3m 4m 5p 5p", None, ()),
            ("sanshoku_doujun", "1m 2m 3m 1p 2p 3p 1s 2s 3s 5m 5m 5m E E", None, ()),
            ("sanshoku_doukou", "2m 2m 2m 2p 2p 2p 2s 2s 2s 4m 5m 6m E E", None, ()),
            ("ittsu", "1m 2m 3m 4m 5m 6m 7m 8m 9m 2p 3p 4p E E", None, ()),
            ("chanta", "1m 2m 3m 7p 8p 9p 1s 1s 1s E E E C C", None, ()),
            ("junchan", "1m 2m 3m 7p 8p 9p 1s 1s 1s 9m 9m 9m 1p 1p", None, ()),
            ("sanankou", "2m 2m 2m 5p 5p 5p 8s 8s 8s 3m 4m 5m E E", None, ()),
            ("sankantsu", "2m 3m 4m E E", None, (_ankan("5m"), _minkan("6p"), _kakan("7s"))),
            ("shousangen", "P P P F F F C C 1m 2m 3m 4p 5p 6p", None, ()),
            ("honitsu", "1m 2m 3m 4m 5m 6m 7m 8m 9m E E E P P", None, ()),
            ("chinitsu", "1m 2m 3m 2m 3m 4m 5m 6m 7m 7m 8m 9m 5m 5m", None, ()),
            ("chinroutou", "1m 1m 1m 9m 9m 9m 1p 1p 1p 9s 9s 9s 1s 1s", None, ()),
            ("tsuuiisou", "E E E S S S W W W P P P C C", None, ()),
            ("daisangen", "P P P F F F C C C 1m 2m 3m E E", None, ()),
            ("shousuushi", "E E E S S S W W W N N 1m 2m 3m", None, ()),
            ("daisuushi", "E E E S S S W W W N N N 1m 1m", None, ()),
            ("suuankou", "2m 2m 2m 5p 5p 5p 8s 8s 8s E E E C C", None, ()),
            ("suukantsu", "C C", None, (_ankan("1m"), _minkan("9m"), _ankan("1p"), _kakan("9s"))),
            ("ryuuiisou", "2s 3s 4s 2s 3s 4s 6s 6s 6s 8s 8s 8s F F", None, ()),
            ("chuuren", "1m 1m 1m 2m 3m 4m 5m 5m 6m 7m 8m 9m 9m 9m", None, ()),
            ("tenhou", "1m 2m 3m 4m 5m 6m 7m 8m 9m E E E P P", None, ()),
            ("chiihou", "1m 2m 3m 4m 5m 6m 7m 8m 9m E E E P P", None, ()),
        ]
        self.assertGreaterEqual(len(fixtures), 24)

        for name, tiles, win, melds in fixtures:
            with self.subTest(yaku=name):
                yaku = _names(
                    tiles,
                    win=win,
                    melds=melds,
                    tenhou=name == "tenhou",
                    chiihou=name == "chiihou",
                )
                self.assertIn(name, yaku)

    def test_requested_yaku_negative_fixtures(self) -> None:
        fixtures = [
            ("pinfu", "1m 2m 3m 2p 3p 4p 4s 5s 6s 6m 7m 8m 5p", "5p", ()),
            ("iipeikou", "1m 2m 3m 2m 3m 4m 4p 5p 6p 6s 7s 8s 5p 5p", None, ()),
            ("ryanpeikou", "1m 2m 3m 1m 2m 3m E E", None, (_chi("4p 5p 6p"), _chi("4p 5p 6p"))),
            ("sanshoku_doujun", "1m 2m 3m 1p 2p 3p 2s 3s 4s 5m 5m 5m E E", None, ()),
            ("sanshoku_doukou", "2m 2m 2m 2p 2p 2p 3s 3s 3s 4m 5m 6m E E", None, ()),
            ("ittsu", "1m 2m 3m 4m 5m 6m 6m 7m 8m 2p 3p 4p E E", None, ()),
            ("chanta", "1m 2m 3m 4p 5p 6p 1s 1s 1s E E E C C", None, ()),
            ("junchan", "1m 2m 3m 7p 8p 9p 1s 1s 1s 9m 9m 9m E E", None, ()),
            ("sanankou", "2m 2m 5p 5p 5p 8s 8s 8s 3m 4m 5m E E", "2m", ()),
            ("sankantsu", "2m 3m 4m E E", None, (_ankan("5m"), _minkan("6p"), _pon("7s"))),
            ("shousangen", "P P P F F F E E 1m 2m 3m 4p 5p 6p", None, ()),
            ("honitsu", "1m 2m 3m 4m 5m 6m 7m 8m 9m 2m 3m 4m 5m 5m", None, ()),
            ("chinitsu", "1m 2m 3m 4m 5m 6m 7m 8m 9m E E E P P", None, ()),
            ("chinroutou", "1m 1m 1m 9m 9m 9m 1p 1p 1p E E E 1s 1s", None, ()),
            ("tsuuiisou", "E E E S S S W W W P P P 1m 1m", None, ()),
            ("daisangen", "P P P F F F C C 1m 2m 3m 4p 5p 6p", None, ()),
            ("shousuushi", "E E E S S S N N 1m 2m 3m 4p 5p 6p", None, ()),
            ("daisuushi", "E E E S S S W W W N N 1m 2m 3m", None, ()),
            ("suuankou", "2m 2m 5p 5p 5p 8s 8s 8s E E E C C", "2m", ()),
            ("suukantsu", "C C", None, (_ankan("1m"), _minkan("9m"), _ankan("1p"), _pon("9s"))),
            ("ryuuiisou", "2s 3s 4s 2s 3s 4s 6s 6s 6s 8s 8s 8s 5s 5s", None, ()),
            ("chuuren", "1m 1m 4m 5m 5m 6m 7m 8m 9m 9m 9m", None, (_chi("1m 2m 3m"),)),
            ("tenhou", "1m 2m 3m 4m 5m 6m 7m 8m 9m E E E P P", None, ()),
            ("chiihou", "1m 2m 3m 4m 5m 6m 7m 8m 9m E E E P P", None, ()),
        ]

        for name, tiles, win, melds in fixtures:
            with self.subTest(yaku=name):
                self.assertNotIn(name, _names(tiles, win=win, melds=melds, win_kind="ron"))

    def test_open_closed_value_metadata(self) -> None:
        self.assertIsNone(YAKU_DEFINITIONS["pinfu"].open_han)
        self.assertEqual(YAKU_DEFINITIONS["ryanpeikou"].closed_han, 3)
        self.assertIsNone(YAKU_DEFINITIONS["ryanpeikou"].open_han)
        self.assertEqual(YAKU_DEFINITIONS["sanshoku_doujun"].closed_han, 2)
        self.assertEqual(YAKU_DEFINITIONS["sanshoku_doujun"].open_han, 1)
        self.assertEqual(YAKU_DEFINITIONS["sanshoku_doukou"].closed_han, 2)
        self.assertEqual(YAKU_DEFINITIONS["sanshoku_doukou"].open_han, 2)

        open_sanshoku = _results(
            "5m 5m 5m E E",
            melds=(_chi("1m 2m 3m"), _chi("1p 2p 3p"), _chi("1s 2s 3s")),
        )
        self.assertEqual(_first(open_sanshoku, "sanshoku_doujun").han, 1)

    def test_ryanpeikou_replaces_iipeikou(self) -> None:
        yaku = _results(
            "1m 2m 3m 1m 2m 3m 2m 3m 4m 2m 3m 4m 5p 5p",
        )
        names = tuple(result.name for result in yaku)

        self.assertIn("ryanpeikou", names)
        self.assertNotIn("iipeikou", names)
        self.assertEqual(_first(yaku, "ryanpeikou").han, 3)


def _results(
    tiles: str,
    *,
    win: str | None = None,
    win_kind: str = "tsumo",
    melds: tuple[Meld, ...] = (),
    tenhou: bool = False,
    chiihou: bool = False,
):
    return detect_yaku(
        _tiles(tiles),
        winning_tile=Tile.parse(win) if win is not None else None,
        win_kind=win_kind,
        melds=melds,
        seat_wind=Tile.parse("E").type,
        round_wind=Tile.parse("S").type,
        tenhou=tenhou,
        chiihou=chiihou,
    )


def _names(
    tiles: str,
    *,
    win: str | None = None,
    win_kind: str = "tsumo",
    melds: tuple[Meld, ...] = (),
    tenhou: bool = False,
    chiihou: bool = False,
) -> tuple[str, ...]:
    return tuple(
        result.name
        for result in _results(
            tiles,
            win=win,
            win_kind=win_kind,
            melds=melds,
            tenhou=tenhou,
            chiihou=chiihou,
        )
    )


def _first(results, name: str):
    for result in results:
        if result.name == name:
            return result
    raise AssertionError(name)


def _tiles(text: str) -> tuple[Tile, ...]:
    return tuple(Tile.parse(token) for token in text.split())


def _pon(tile: str) -> Meld:
    tiles = _tiles(f"{tile} {tile} {tile}")
    return Meld(ActionKind.PON, tiles, called_tile=tiles[0], from_seat=1)


def _minkan(tile: str) -> Meld:
    tiles = _tiles(f"{tile} {tile} {tile} {tile}")
    return Meld(ActionKind.MINKAN, tiles, called_tile=tiles[0], from_seat=1)


def _kakan(tile: str) -> Meld:
    tiles = _tiles(f"{tile} {tile} {tile} {tile}")
    return Meld(ActionKind.KAKAN, tiles, called_tile=tiles[0], from_seat=1)


def _ankan(tile: str) -> Meld:
    return Meld(ActionKind.ANKAN, _tiles(f"{tile} {tile} {tile} {tile}"))


def _chi(text: str) -> Meld:
    tiles = _tiles(text)
    return Meld(ActionKind.CHI, tiles, called_tile=tiles[0], from_seat=1)
