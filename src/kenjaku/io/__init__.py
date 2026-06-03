"""Input/output adapters for external mahjong formats."""

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
    tenhou_tile,
)

__all__ = [
    "TenhouAgari",
    "TenhouCall",
    "TenhouDiscard",
    "TenhouDraw",
    "TenhouEvent",
    "TenhouGame",
    "TenhouReach",
    "TenhouRound",
    "TenhouRyuukyoku",
    "parse_tenhou_xml",
    "parse_tenhou_xml_file",
    "tenhou_tile",
]
