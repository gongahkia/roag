from __future__ import annotations

from collections.abc import Sequence
from pathlib import Path

from kenjaku.io.tenhou_xml import TenhouGame, parse_tenhou_xml_file


def tenhou_xml_files(paths: Sequence[str | Path]) -> tuple[Path, ...]:
    files: list[Path] = []
    for raw_path in paths:
        path = Path(raw_path)
        if path.is_dir():
            files.extend(
                sorted(candidate for candidate in path.rglob("*.xml") if candidate.is_file())
            )
            continue
        if path.is_file():
            files.append(path)
            continue
        raise FileNotFoundError(f"Tenhou XML path not found: {path}")

    unique_files = dict.fromkeys(path.resolve() for path in files)
    return tuple(sorted(unique_files))


def parse_tenhou_xml_paths(paths: Sequence[str | Path]) -> TenhouGame:
    files = tenhou_xml_files(paths)
    if not files:
        raise ValueError("no Tenhou XML files found")

    rounds = []
    for file in files:
        rounds.extend(parse_tenhou_xml_file(file).rounds)
    return TenhouGame(rounds=tuple(rounds))
