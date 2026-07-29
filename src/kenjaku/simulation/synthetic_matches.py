from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from kenjaku.core import TENHOU_3P, TENHOU_4P
from kenjaku.simulation.self_play import run_self_play_match_sandbox

SYNTHETIC_MATCH_MANIFEST_V1_KIND = "kenjaku-synthetic-match-manifest-v1"
SYNTHETIC_MATCH_MANIFEST_V1_FIELDS = (
    "kind",
    "ruleset",
    "players",
    "match_count",
    "max_rounds",
    "max_turns_per_round",
    "provenance",
    "final_summary",
    "matches",
)


def generate_synthetic_match_manifest(
    *,
    match_count: int,
    seed: int | str,
    ruleset: str = TENHOU_4P.name,
    max_rounds: int | None = None,
    max_turns_per_round: int = 512,
) -> dict[str, Any]:
    if isinstance(match_count, bool) or not isinstance(match_count, int) or match_count <= 0:
        raise ValueError("match_count must be a positive integer")
    resolved_max_rounds = _default_max_rounds(ruleset) if max_rounds is None else max_rounds
    report = run_self_play_match_sandbox(
        games=match_count,
        max_rounds=resolved_max_rounds,
        max_turns_per_round=max_turns_per_round,
        seed=str(seed),
        ruleset=ruleset,
        ron_policy="pass",
        include_trajectories=True,
    )
    matches = report["game_summaries"]
    incomplete_games = [str(match["game"]) for match in matches if not match["completed"]]
    if incomplete_games:
        raise ValueError(
            "synthetic generation did not complete games: " + ",".join(incomplete_games)
        )
    manifest = {
        "kind": SYNTHETIC_MATCH_MANIFEST_V1_KIND,
        "ruleset": report["ruleset"],
        "players": report["players"],
        "match_count": match_count,
        "max_rounds": resolved_max_rounds,
        "max_turns_per_round": max_turns_per_round,
        "provenance": {
            "source_kind": "local_synthetic",
            "generator": "kenjaku-synthetic-match-generator-v1",
            "root_seed": str(seed),
            "seed_derivation": report["seed_provenance"]["derivation"],
            "policies": report["policies"],
            "rule_config": report["rule_config"],
        },
        "final_summary": report["final_summary"],
        "matches": matches,
    }
    return json.loads(json.dumps(manifest))


def write_synthetic_match_manifest(path: Path, manifest: dict[str, Any]) -> None:
    if manifest.get("kind") != SYNTHETIC_MATCH_MANIFEST_V1_KIND:
        raise ValueError("not a synthetic match manifest v1")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def _default_max_rounds(ruleset: str) -> int:
    if ruleset == TENHOU_4P.name:
        return 12
    if ruleset == TENHOU_3P.name:
        return 9
    raise ValueError("unsupported synthetic match ruleset: " + ruleset)
