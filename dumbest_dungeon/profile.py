"""Versioned, local-only horizontal progression and discovery records."""

from __future__ import annotations

from copy import deepcopy
from pathlib import Path
from typing import Any

from .save import SaveError, read_save, write_save
from .versions import PROFILE_SCHEMA

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
        "discoveries": {kind: [] for kind in DISCOVERY_KINDS},
        "completed_contracts": [],
        "graveyard": [],
    }


def migrate_profile(profile: dict[str, Any]) -> dict[str, Any]:
    if not isinstance(profile, dict) or type(profile.get("profile_version")) is not int:
        raise ProfileError("profile requires an integer version")
    if profile["profile_version"] != PROFILE_SCHEMA:
        raise ProfileError(f"unsupported profile version {profile['profile_version']}")
    return deepcopy(profile)


def validate_profile(profile: dict[str, Any]) -> dict[str, Any]:
    profile = migrate_profile(profile)
    expected = {
        "profile_version", "runs_archived", "base_victories", "unlocked_rank",
        "best_completed_rank", "discoveries", "completed_contracts", "graveyard",
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
    result["graveyard"].extend(
        {
            "run_id": run_id,
            "seed": report["seed"],
            "hero_id": row["source_id"],
            "tick": row["tick"],
        }
        for row in report["decisions"]
        if row["kind"] == "crew_death"
    )
    return validate_profile(result)
