"""Input/output adapters for external mahjong formats."""

from kenjaku.io.tenhou_meld import TenhouMeld, decode_tenhou_meld
from kenjaku.io.tenhou_tiles import tenhou_tile
from kenjaku.io.tenhou_xml import (
    TenhouAgari,
    TenhouCall,
    TenhouDiscard,
    TenhouDraw,
    TenhouEvent,
    TenhouGame,
    TenhouReach,
    TenhouRound,
    TenhouRyuukyoku,
    parse_tenhou_xml,
    parse_tenhou_xml_file,
)

__all__ = [
    "TenhouAgari",
    "TenhouCall",
    "TenhouDiscard",
    "TenhouDraw",
    "TenhouEvent",
    "TenhouGame",
    "TenhouMeld",
    "TenhouReach",
    "TenhouRound",
    "TenhouRyuukyoku",
    "decode_tenhou_meld",
    "parse_tenhou_xml",
    "parse_tenhou_xml_file",
    "tenhou_tile",
]
