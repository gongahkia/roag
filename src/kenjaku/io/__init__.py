"""Input/output adapters for external mahjong formats."""

from kenjaku.io.tenhou_xml import (
    TenhouDiscard,
    TenhouDraw,
    TenhouEvent,
    TenhouGame,
    TenhouRound,
    parse_tenhou_xml,
    parse_tenhou_xml_file,
    tenhou_tile,
)

__all__ = [
    "TenhouDiscard",
    "TenhouDraw",
    "TenhouEvent",
    "TenhouGame",
    "TenhouRound",
    "parse_tenhou_xml",
    "parse_tenhou_xml_file",
    "tenhou_tile",
]
