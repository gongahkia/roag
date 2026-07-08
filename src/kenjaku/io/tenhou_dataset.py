from __future__ import annotations

from collections.abc import Iterator, Sequence
from concurrent.futures import ProcessPoolExecutor
from dataclasses import dataclass
from multiprocessing import get_context
from multiprocessing.context import BaseContext
from pathlib import Path
from typing import Any

from kenjaku.io.parse_cache import (
    parse_tenhou_xml_file_cached,
    tenhou_game_from_payload,
    tenhou_game_payload,
)
from kenjaku.io.tenhou_xml import TenhouGame, TenhouRound, parse_tenhou_xml_file


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


@dataclass(frozen=True, slots=True)
class _TenhouDatasetParseTask:
    path: Path
    file_index: int
    skip_errors: bool
    parse_cache_dir: Path | None


@dataclass(frozen=True, slots=True)
class _TenhouDatasetParseResult:
    path: Path
    file_index: int
    game_payload: dict[str, Any] | None = None
    failure: TenhouParseFailure | None = None


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
    jobs: int = 1,
) -> TenhouGame:
    return parse_tenhou_xml_dataset(paths, parse_cache_dir=parse_cache_dir, jobs=jobs).game


def iter_tenhou_xml_dataset(
    paths: Sequence[str | Path],
    *,
    skip_errors: bool = False,
    parse_cache_dir: str | Path | None = None,
    jobs: int = 1,
) -> Iterator[TenhouGame]:
    for parsed in iter_tenhou_xml_dataset_files(
        paths,
        skip_errors=skip_errors,
        parse_cache_dir=parse_cache_dir,
        jobs=jobs,
    ):
        yield parsed.game


def iter_tenhou_xml_dataset_files(
    paths: Sequence[str | Path],
    *,
    skip_errors: bool = False,
    failures: list[TenhouParseFailure] | None = None,
    parse_cache_dir: str | Path | None = None,
    jobs: int = 1,
) -> Iterator[TenhouDatasetFile]:
    if jobs <= 0:
        raise ValueError("jobs must be positive")
    files = tenhou_xml_files(paths)
    if not files:
        raise ValueError("no Tenhou XML files found")

    cache_dir = None if parse_cache_dir is None else Path(parse_cache_dir)
    if jobs > 1 and len(files) > 1:
        tasks = tuple(
            _TenhouDatasetParseTask(
                path=file,
                file_index=file_index,
                skip_errors=skip_errors,
                parse_cache_dir=cache_dir,
            )
            for file_index, file in enumerate(files)
        )
        with ProcessPoolExecutor(
            max_workers=jobs,
            mp_context=_process_pool_context(),
        ) as executor:
            for chunk in executor.map(_parse_tenhou_dataset_file_chunk, _task_chunks(tasks, jobs)):
                for result in chunk:
                    if result.failure is not None:
                        if failures is not None:
                            failures.append(result.failure)
                        continue
                    yield TenhouDatasetFile(
                        path=result.path,
                        file_index=result.file_index,
                        game=_game_from_parse_result(result),
                    )
        return

    for file_index, file in enumerate(files):
        try:
            if cache_dir is None:
                game = parse_tenhou_xml_file(file)
            else:
                game = parse_tenhou_xml_file_cached(file, cache_dir)
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
    jobs: int = 1,
) -> TenhouDataset:
    if jobs <= 0:
        raise ValueError("jobs must be positive")
    files = tenhou_xml_files(paths)
    if not files:
        raise ValueError("no Tenhou XML files found")

    rounds: list[TenhouRound] = []
    failures: list[TenhouParseFailure] = []
    cache_dir = None if parse_cache_dir is None else Path(parse_cache_dir)
    if jobs > 1 and len(files) > 1:
        tasks = tuple(
            _TenhouDatasetParseTask(
                path=file,
                file_index=file_index,
                skip_errors=skip_errors,
                parse_cache_dir=cache_dir,
            )
            for file_index, file in enumerate(files)
        )
        with ProcessPoolExecutor(
            max_workers=jobs,
            mp_context=_process_pool_context(),
        ) as executor:
            for chunk in executor.map(_parse_tenhou_dataset_file_chunk, _task_chunks(tasks, jobs)):
                for result in chunk:
                    if result.failure is not None:
                        failures.append(result.failure)
                        continue
                    rounds.extend(_game_from_parse_result(result).rounds)
        return TenhouDataset(
            game=TenhouGame(rounds=tuple(rounds)),
            files=files,
            failures=tuple(failures),
        )

    for parsed in iter_tenhou_xml_dataset_files(
        files,
        skip_errors=skip_errors,
        failures=failures,
        parse_cache_dir=cache_dir,
        jobs=jobs,
    ):
        rounds.extend(parsed.game.rounds)
    return TenhouDataset(
        game=TenhouGame(rounds=tuple(rounds)),
        files=files,
        failures=tuple(failures),
    )


def _parse_tenhou_dataset_file_task(
    task: _TenhouDatasetParseTask,
) -> _TenhouDatasetParseResult:
    try:
        if task.parse_cache_dir is None:
            game = parse_tenhou_xml_file(task.path)
        else:
            game = parse_tenhou_xml_file_cached(task.path, task.parse_cache_dir)
    except Exception as error:
        if not task.skip_errors:
            raise
        return _TenhouDatasetParseResult(
            path=task.path,
            file_index=task.file_index,
            failure=TenhouParseFailure(
                path=task.path,
                error_type=type(error).__name__,
                message=str(error),
            ),
        )
    return _TenhouDatasetParseResult(
        path=task.path,
        file_index=task.file_index,
        game_payload=tenhou_game_payload(game),
    )


def _parse_tenhou_dataset_file_chunk(
    tasks: tuple[_TenhouDatasetParseTask, ...],
) -> tuple[_TenhouDatasetParseResult, ...]:
    return tuple(_parse_tenhou_dataset_file_task(task) for task in tasks)


def _game_from_parse_result(result: _TenhouDatasetParseResult) -> TenhouGame:
    if result.game_payload is None:
        raise RuntimeError("parallel Tenhou parse returned no game")
    return tenhou_game_from_payload(result.game_payload)


def _task_chunks(
    tasks: Sequence[_TenhouDatasetParseTask],
    jobs: int,
) -> tuple[tuple[_TenhouDatasetParseTask, ...], ...]:
    chunk_size = max(1, (len(tasks) + jobs - 1) // jobs)
    return tuple(
        tuple(tasks[start : start + chunk_size]) for start in range(0, len(tasks), chunk_size)
    )


def _process_pool_context() -> BaseContext | None:
    try:
        return get_context("fork")
    except ValueError:
        return None
