from __future__ import annotations

import json
import math
import random
from collections.abc import Mapping
from pathlib import Path
from typing import Any

from kenjaku.reproducibility import derive_seed_int
from kenjaku.simulation.synthetic_matches import SYNTHETIC_MATCH_MANIFEST_V1_KIND

SYNTHETIC_MATCH_SPLIT_MANIFEST_V1_KIND = "kenjaku-synthetic-match-split-manifest-v1"
SYNTHETIC_MATCH_SPLIT_MANIFEST_V1_FIELDS = (
    "kind",
    "source",
    "split_seed",
    "fractions",
    "assignments",
)
_SPLIT_NAMES = ("train", "validation", "test")


def build_synthetic_match_split_manifest(
    source_manifest: Mapping[str, Any],
    *,
    split_seed: int | str,
    fractions: tuple[float, float, float] = (0.8, 0.1, 0.1),
) -> dict[str, Any]:
    source = _source_metadata(source_manifest)
    game_ids = _game_ids(source_manifest)
    if len(game_ids) < len(_SPLIT_NAMES):
        raise ValueError("synthetic split requires at least three matches")
    _validate_fractions(fractions)
    shuffled_ids = list(game_ids)
    random.Random(derive_seed_int(split_seed, "synthetic-match-split", source["ruleset"])).shuffle(
        shuffled_ids
    )
    counts = _split_counts(len(shuffled_ids), fractions)
    assignments: dict[str, list[int]] = {}
    offset = 0
    for name, count in zip(_SPLIT_NAMES, counts, strict=True):
        assignments[name] = sorted(shuffled_ids[offset : offset + count])
        offset += count
    return {
        "kind": SYNTHETIC_MATCH_SPLIT_MANIFEST_V1_KIND,
        "source": source,
        "split_seed": str(split_seed),
        "fractions": list(fractions),
        "assignments": assignments,
    }


def write_synthetic_match_split_manifest(path: Path, manifest: Mapping[str, Any]) -> None:
    if manifest.get("kind") != SYNTHETIC_MATCH_SPLIT_MANIFEST_V1_KIND:
        raise ValueError("not a synthetic match split manifest v1")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def _source_metadata(source_manifest: Mapping[str, Any]) -> dict[str, Any]:
    if source_manifest.get("kind") != SYNTHETIC_MATCH_MANIFEST_V1_KIND:
        raise ValueError("not a synthetic match manifest v1")
    ruleset = source_manifest.get("ruleset")
    players = source_manifest.get("players")
    match_count = source_manifest.get("match_count")
    provenance = source_manifest.get("provenance")
    if not isinstance(ruleset, str):
        raise ValueError("source ruleset must be a string")
    if type(players) is not int or type(match_count) is not int:
        raise ValueError("source players and match_count must be integers")
    if not isinstance(provenance, Mapping):
        raise ValueError("source provenance must be an object")
    return {
        "kind": SYNTHETIC_MATCH_MANIFEST_V1_KIND,
        "ruleset": ruleset,
        "players": players,
        "match_count": match_count,
        "provenance": dict(provenance),
    }


def _game_ids(source_manifest: Mapping[str, Any]) -> tuple[int, ...]:
    matches = source_manifest.get("matches")
    if not isinstance(matches, list):
        raise ValueError("source matches must be an array")
    game_ids = tuple(match.get("game") for match in matches if isinstance(match, Mapping))
    if len(game_ids) != len(matches) or any(type(game_id) is not int for game_id in game_ids):
        raise ValueError("source match game IDs must be integers")
    if len(set(game_ids)) != len(game_ids):
        raise ValueError("source match game IDs must be unique")
    if len(game_ids) != source_manifest.get("match_count"):
        raise ValueError("source match_count must match matches")
    return game_ids


def _validate_fractions(fractions: tuple[float, float, float]) -> None:
    if len(fractions) != len(_SPLIT_NAMES) or any(fraction <= 0 for fraction in fractions):
        raise ValueError("split fractions must contain three positive values")
    if not math.isclose(sum(fractions), 1.0, rel_tol=0.0, abs_tol=1e-9):
        raise ValueError("split fractions must sum to one")


def _split_counts(match_count: int, fractions: tuple[float, float, float]) -> tuple[int, int, int]:
    counts = [math.floor(match_count * fraction) for fraction in fractions]
    remaining = match_count - sum(counts)
    remainders = sorted(
        range(len(fractions)),
        key=lambda index: (match_count * fractions[index] - counts[index], -index),
        reverse=True,
    )
    for index in remainders[:remaining]:
        counts[index] += 1
    for index, count in enumerate(counts):
        if count == 0:
            donor = max(range(len(counts)), key=counts.__getitem__)
            counts[donor] -= 1
            counts[index] += 1
    return counts[0], counts[1], counts[2]
