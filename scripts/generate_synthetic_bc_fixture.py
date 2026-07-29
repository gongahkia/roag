from __future__ import annotations

import argparse
import random
from hashlib import blake2b
from pathlib import Path

DRAW_TAGS = ("T", "U", "V", "W")
DISCARD_TAGS = ("D", "E", "F", "G")


def main() -> int:
    parser = argparse.ArgumentParser(
        description="generate deterministic Tenhou-shaped synthetic BC XML fixtures",
    )
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--games", type=int, default=200)
    parser.add_argument("--seed", default="synthetic-bc-v1")
    parser.add_argument("--turn-cycles", type=int, default=4)
    parser.add_argument("--overwrite", action="store_true")
    args = parser.parse_args()

    if args.games < 1:
        raise SystemExit("--games must be positive")
    if args.turn_cycles < 1:
        raise SystemExit("--turn-cycles must be positive")
    if args.output_dir.exists() and any(args.output_dir.iterdir()) and not args.overwrite:
        raise SystemExit("--output-dir must be empty or use --overwrite")

    args.output_dir.mkdir(parents=True, exist_ok=True)
    if args.overwrite:
        for path in args.output_dir.glob("*.xml"):
            path.unlink()

    for game_index in range(args.games):
        xml = _synthetic_game_xml(
            seed=args.seed,
            game_index=game_index,
            turn_cycles=args.turn_cycles,
        )
        path = args.output_dir / f"synthetic-bc-{game_index:03d}.xml"
        path.write_text(xml, encoding="utf-8")

    return 0


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


if __name__ == "__main__":
    raise SystemExit(main())
