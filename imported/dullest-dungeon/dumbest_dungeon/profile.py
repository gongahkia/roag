"""Versioned, local-only horizontal progression and discovery records."""

from __future__ import annotations

from copy import deepcopy
from pathlib import Path
from typing import Any

from .save import SaveError, read_save, write_save
from .versions import PROFILE_SCHEMA
from .challenges import CONTRACTS

DISCOVERY_KINDS = ("cards", "items", "boons", "curses", "guardians", "finales")


class ProfileError(ValueError):
    pass


def new_profile() -> dict[str, Any]:
    return {
        "profile_version": PROFILE_SCHEMA,
        "runs_archived": [],
        "base_victories": 0,
        "unlocked_rank": 1,
        "best_completed_rank": 0,
        "best_loop_depth": 0,
        "best_score": 0,
        "discoveries": {kind: [] for kind in DISCOVERY_KINDS},
        "completed_contracts": [],
        "graveyard": [],
    }


def migrate_profile(profile: dict[str, Any]) -> dict[str, Any]:
    if not isinstance(profile, dict) or type(profile.get("profile_version")) is not int:
        raise ProfileError("profile requires an integer version")
    result = deepcopy(profile)
    if result["profile_version"] == 1:
        expected = {
            "profile_version", "runs_archived", "base_victories", "unlocked_rank",
            "best_completed_rank", "discoveries", "completed_contracts", "graveyard",
        }
        if set(result) != expected:
            raise ProfileError("profile version 1 has unknown or missing fields")
        result.update(profile_version=2, best_loop_depth=0, best_score=0)
    if result["profile_version"] != PROFILE_SCHEMA:
        raise ProfileError(f"unsupported profile version {result['profile_version']}")
    return result


def validate_profile(profile: dict[str, Any]) -> dict[str, Any]:
    profile = migrate_profile(profile)
    expected = {
        "profile_version", "runs_archived", "base_victories", "unlocked_rank",
        "best_completed_rank", "best_loop_depth", "best_score", "discoveries",
        "completed_contracts", "graveyard",
    }
    if set(profile) != expected:
        raise ProfileError("profile has unknown or missing fields")
    if (
        type(profile["base_victories"]) is not int
        or profile["base_victories"] < 0
        or type(profile["unlocked_rank"]) is not int
        or not 1 <= profile["unlocked_rank"] <= 20
        or type(profile["best_completed_rank"]) is not int
        or not 0 <= profile["best_completed_rank"] <= profile["unlocked_rank"]
        or type(profile["best_loop_depth"]) is not int
        or profile["best_loop_depth"] < 0
        or type(profile["best_score"]) is not int
        or profile["best_score"] < 0
    ):
        raise ProfileError("profile progression counters are invalid")
    for key in ("runs_archived", "completed_contracts"):
        value = profile[key]
        if not isinstance(value, list) or value != sorted(set(value)) or any(
            not isinstance(item, str) or not item for item in value
        ):
            raise ProfileError(f"profile {key} must be sorted unique IDs")
    discoveries = profile["discoveries"]
    if not isinstance(discoveries, dict) or set(discoveries) != set(DISCOVERY_KINDS):
        raise ProfileError("profile discoveries have an invalid vocabulary")
    for kind, values in discoveries.items():
        if not isinstance(values, list) or values != sorted(set(values)) or any(
            not isinstance(item, str) or not item for item in values
        ):
            raise ProfileError(f"profile discovery {kind} must be sorted unique IDs")
    if not isinstance(profile["graveyard"], list) or any(
        not isinstance(row, dict)
        or set(row) != {"run_id", "seed", "hero_id", "tick"}
        or not isinstance(row["run_id"], str)
        or type(row["seed"]) is not int
        or not isinstance(row["hero_id"], str)
        or type(row["tick"]) is not int
        for row in profile["graveyard"]
    ):
        raise ProfileError("profile graveyard is malformed")
    return profile


def read_profile(path: Path) -> dict[str, Any]:
    if not path.exists():
        return new_profile()
    try:
        return validate_profile(read_save(path))
    except (SaveError, ProfileError) as exc:
        raise ProfileError(f"cannot read profile {path}: {exc}") from exc


def write_profile(path: Path, profile: dict[str, Any]) -> None:
    try:
        write_save(path, validate_profile(profile))
    except (SaveError, ProfileError) as exc:
        raise ProfileError(f"cannot write profile {path}: {exc}") from exc


def update_profile(profile: dict[str, Any], report: dict[str, Any]) -> dict[str, Any]:
    result = validate_profile(profile)
    run_id = str(report["id"])
    if run_id in result["runs_archived"]:
        return result
    result["runs_archived"] = sorted([*result["runs_archived"], run_id])
    if report["outcome"] == "victory":
        result["base_victories"] += 1
        rank = int(report.get("ladder_rank", 0))
        if 1 <= rank <= result["unlocked_rank"]:
            result["best_completed_rank"] = max(result["best_completed_rank"], rank)
            result["unlocked_rank"] = min(20, max(result["unlocked_rank"], rank + 1))
    result["best_loop_depth"] = max(result["best_loop_depth"], int(report.get("loop_depth", 0)))
    result["best_score"] = max(result["best_score"], int(report.get("score", 0)))
    if report.get("outcome") in {"victory", "loop_clear"} and report.get(
        "expedition_mode"
    ) in {"daily", "challenge"}:
        contract_ids = {
            contract.modifier: contract.id for contract in CONTRACTS
        }
        result["completed_contracts"] = sorted(
            set(result["completed_contracts"])
            | {
                contract_ids[modifier]
                for modifier in report.get("active_modifiers", [])
                if modifier in contract_ids
            }
        )
    discoveries = {key: set(values) for key, values in result["discoveries"].items()}
    discoveries["cards"].update(report["cards"])
    discoveries["cards"].update(card["card_id"] for card in report["deck"])
    discoveries["items"].update(report["items"])
    discoveries["boons"].update(
        boon_id for owned in report["boons"].values() for boon_id in owned
    )
    discoveries["curses"].update(
        curse_id for owned in report["curses"].values() for curse_id in owned
    )
    for encounter in report["encounters"]:
        if encounter.get("kind") == "guardian":
            discoveries["guardians"].add(encounter["id"])
        elif encounter.get("kind") == "boss":
            discoveries["finales"].add(encounter["id"])
    result["discoveries"] = {key: sorted(values) for key, values in discoveries.items()}
    existing_graves = {
        (row["seed"], row["hero_id"], row["tick"]) for row in result["graveyard"]
    }
    result["graveyard"].extend(
        row for row in (
            {
            "run_id": run_id,
            "seed": report["seed"],
            "hero_id": row["source_id"],
            "tick": row["tick"],
            }
            for row in report["decisions"]
            if row["kind"] == "crew_death"
        )
        if (row["seed"], row["hero_id"], row["tick"]) not in existing_graves
    )
    return validate_profile(result)
