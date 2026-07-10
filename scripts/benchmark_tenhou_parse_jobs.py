from __future__ import annotations

import argparse
import json
import random
import statistics
import sys
import tempfile
import time
from hashlib import blake2b
from pathlib import Path

from kenjaku.io import parse_tenhou_xml_dataset

DRAW_TAGS = ("T", "U", "V", "W")
DISCARD_TAGS = ("D", "E", "F", "G")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Benchmark Tenhou XML parse jobs.")
    parser.add_argument("--files", type=int, default=50)
    parser.add_argument("--parallel-jobs", type=int, default=4)
    parser.add_argument("--ignored-tags-per-file", type=int, default=2500)
    parser.add_argument("--turn-cycles", type=int, default=4)
    parser.add_argument("--runs", type=int, default=3)
    parser.add_argument("--min-speedup", type=float, default=0.0)
    parser.add_argument("--seed", default="parallel-tenhou-parse-benchmark-v0")
    args = parser.parse_args(argv)

    if args.files < 1:
        raise SystemExit("--files must be positive")
    if args.parallel_jobs < 2:
        raise SystemExit("--parallel-jobs must be at least 2")
    if args.ignored_tags_per_file < 0:
        raise SystemExit("--ignored-tags-per-file must be non-negative")
    if args.turn_cycles < 1:
        raise SystemExit("--turn-cycles must be positive")
    if args.runs < 1:
        raise SystemExit("--runs must be positive")

    with tempfile.TemporaryDirectory() as directory:
        fixture_dir = Path(directory)
        _write_fixtures(
            fixture_dir,
            files=args.files,
            ignored_tags_per_file=args.ignored_tags_per_file,
            turn_cycles=args.turn_cycles,
            seed=args.seed,
        )
        files = tuple(sorted(fixture_dir.glob("*.xml")))
        sequential = _benchmark(files, jobs=1, runs=args.runs)
        parallel = _benchmark(files, jobs=args.parallel_jobs, runs=args.runs)

    speedup = sequential["median_seconds"] / parallel["median_seconds"]
    payload = {
        "kind": "kenjaku-tenhou-parse-jobs-benchmark-v0",
        "files": args.files,
        "ignored_tags_per_file": args.ignored_tags_per_file,
        "turn_cycles": args.turn_cycles,
        "runs": args.runs,
        "sequential": sequential,
        "parallel": parallel,
        "speedup": speedup,
        "min_speedup": args.min_speedup,
        "passed": speedup >= args.min_speedup,
    }
    print(json.dumps(payload, indent=2, sort_keys=True))
    return 0 if speedup >= args.min_speedup else 1


def _write_fixtures(
    fixture_dir: Path,
    *,
    files: int,
    ignored_tags_per_file: int,
    turn_cycles: int,
    seed: str,
) -> None:
    ignored_tags = "".join(f'  <GO type="{index}" />\n' for index in range(ignored_tags_per_file))
    for file_index in range(files):
        xml = _synthetic_game_xml(
            seed=seed,
            game_index=file_index,
            turn_cycles=turn_cycles,
        )
        if ignored_tags:
            xml = xml.replace("  <RYUUKYOKU", ignored_tags + "  <RYUUKYOKU", 1)
        (fixture_dir / f"tenhou-parse-benchmark-{file_index:03d}.xml").write_text(
            xml,
            encoding="utf-8",
        )


def _synthetic_game_xml(*, seed: str, game_index: int, turn_cycles: int) -> str:
    rng = random.Random(_seed_int(f"{seed}:{game_index}"))
    wall = list(range(136))
    rng.shuffle(wall)
    hands = [sorted(wall[seat * 13 : (seat + 1) * 13]) for seat in range(4)]
    cursor = 52
    dora = wall[cursor]
    cursor += 1

    lines = [
        "<mjloggm>",
        "  <INIT",
        f'    seed="{game_index},0,0,0,0,{dora}"',
        '    ten="250,250,250,250"',
        f'    oya="{game_index % 4}"',
        f'    hai0="{_ids(hands[0])}"',
        f'    hai1="{_ids(hands[1])}"',
        f'    hai2="{_ids(hands[2])}"',
        f'    hai3="{_ids(hands[3])}"',
        "  />",
        f'  <DORA hai="{dora}" />',
    ]

    for _cycle in range(turn_cycles):
        for seat in range(4):
            draw = wall[cursor]
            cursor += 1
            hands[seat].append(draw)
            discard = draw if rng.random() < 0.65 else rng.choice(hands[seat])
            hands[seat].remove(discard)
            lines.append(f"  <{DRAW_TAGS[seat]}{draw} />")
            lines.append(f"  <{DISCARD_TAGS[seat]}{discard} />")

    lines.extend(
        [
            '  <RYUUKYOKU type="synthetic" ten="250,250,250,250" />',
            "</mjloggm>",
            "",
        ]
    )
    return "\n".join(lines)


def _ids(tile_ids: list[int]) -> str:
    return ",".join(str(tile_id) for tile_id in tile_ids)


def _seed_int(seed: str) -> int:
    digest = blake2b(seed.encode(), digest_size=8).digest()
    return int.from_bytes(digest, "big")


def _benchmark(files: tuple[Path, ...], *, jobs: int, runs: int) -> dict[str, object]:
    elapsed: list[float] = []
    for _ in range(runs):
        start = time.perf_counter()
        dataset = parse_tenhou_xml_dataset(files, jobs=jobs)
        elapsed.append(time.perf_counter() - start)
        if len(dataset.files) != len(files):
            raise RuntimeError("parsed file count mismatch")
        if len(dataset.game.rounds) != len(files):
            raise RuntimeError("parsed round count mismatch")
    return {
        "jobs": jobs,
        "median_seconds": statistics.median(elapsed),
        "runs_seconds": elapsed,
    }


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
