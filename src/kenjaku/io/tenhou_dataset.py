from __future__ import annotations

from collections.abc import Iterator, Sequence
from dataclasses import dataclass
from pathlib import Path

from kenjaku.io.parse_cache import parse_tenhou_xml_file_cached
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


@dataclass(frozen=True, slots=True)
class TenhouDatasetFile:
    path: Path
    file_index: int
    game: TenhouGame


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


def parse_tenhou_xml_paths(
    paths: Sequence[str | Path],
    *,
    parse_cache_dir: str | Path | None = None,
) -> TenhouGame:
    return parse_tenhou_xml_dataset(paths, parse_cache_dir=parse_cache_dir).game


def iter_tenhou_xml_dataset(
    paths: Sequence[str | Path],
    *,
    skip_errors: bool = False,
    parse_cache_dir: str | Path | None = None,
) -> Iterator[TenhouGame]:
    for parsed in iter_tenhou_xml_dataset_files(
        paths,
        skip_errors=skip_errors,
        parse_cache_dir=parse_cache_dir,
    ):
        yield parsed.game


def iter_tenhou_xml_dataset_files(
    paths: Sequence[str | Path],
    *,
    skip_errors: bool = False,
    failures: list[TenhouParseFailure] | None = None,
    parse_cache_dir: str | Path | None = None,
) -> Iterator[TenhouDatasetFile]:
    files = tenhou_xml_files(paths)
    if not files:
        raise ValueError("no Tenhou XML files found")

    for file_index, file in enumerate(files):
        try:
            if parse_cache_dir is None:
                game = parse_tenhou_xml_file(file)
            else:
                game = parse_tenhou_xml_file_cached(file, parse_cache_dir)
        except Exception as error:
            if not skip_errors:
                raise
            if failures is not None:
                failures.append(
                    TenhouParseFailure(
                        path=file,
                        error_type=type(error).__name__,
                        message=str(error),
                    )
                )
            continue
        yield TenhouDatasetFile(path=file, file_index=file_index, game=game)


def parse_tenhou_xml_dataset(
    paths: Sequence[str | Path],
    *,
    skip_errors: bool = False,
    parse_cache_dir: str | Path | None = None,
) -> TenhouDataset:
    files = tenhou_xml_files(paths)
    if not files:
        raise ValueError("no Tenhou XML files found")

    rounds = []
    failures: list[TenhouParseFailure] = []
    for parsed in iter_tenhou_xml_dataset_files(
        files,
        skip_errors=skip_errors,
        failures=failures,
        parse_cache_dir=parse_cache_dir,
    ):
        rounds.extend(parsed.game.rounds)
    return TenhouDataset(
        game=TenhouGame(rounds=tuple(rounds)),
        files=files,
        failures=tuple(failures),
    )
