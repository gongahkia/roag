from __future__ import annotations

from collections.abc import Sequence
from dataclasses import dataclass
from pathlib import Path

from kenjaku.io.tenhou_xml import TenhouGame, parse_tenhou_xml_file


@dataclass(frozen=True, slots=True)
class TenhouParseFailure:
    path: Path
    error_type: str
    message: str


@dataclass(frozen=True, slots=True)
class TenhouDataset:
    game: TenhouGame
    files: tuple[Path, ...]
    failures: tuple[TenhouParseFailure, ...] = ()


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
    return parse_tenhou_xml_dataset(paths).game


def parse_tenhou_xml_dataset(
    paths: Sequence[str | Path],
    *,
    skip_errors: bool = False,
) -> TenhouDataset:
    files = tenhou_xml_files(paths)
    if not files:
        raise ValueError("no Tenhou XML files found")

    rounds = []
    failures: list[TenhouParseFailure] = []
    for file in files:
        try:
            rounds.extend(parse_tenhou_xml_file(file).rounds)
        except Exception as error:
            if not skip_errors:
                raise
            failures.append(
                TenhouParseFailure(
                    path=file,
                    error_type=type(error).__name__,
                    message=str(error),
                )
            )
    return TenhouDataset(
        game=TenhouGame(rounds=tuple(rounds)),
        files=files,
        failures=tuple(failures),
    )
