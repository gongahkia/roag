"""Atomic, non-power-creeping progression shared between roguelike runs."""

from __future__ import annotations

from dataclasses import asdict, dataclass, field
import json
import os
from pathlib import Path


PROFILE_FORMAT = 1
PROFILE_NAME = "roag-profile.json"
RUN_RECORD_LIMIT = 40

INITIAL_REGIONS = frozenset({
    "hearthford", "greywash", "greenwold", "whitecairn", "dunmire",
    "marlbank",
})
ALL_REGIONS = INITIAL_REGIONS | {"rillscar", "frostmere"}


class ProfileError(ValueError):
    """Raised when profile persistence is missing or malformed."""


@dataclass(frozen=True)
class RunRecord:
    run_id: str
    seed: str
    result: str
    reason: str
    challenge_tier: int
    region_path: tuple[str, ...]
    actions: int
    boss_kills: tuple[str, ...]
    item_stacks: tuple[tuple[str, int], ...]


@dataclass
class PlayerProfile:
    profile_format: int = PROFILE_FORMAT
    unlocked_items: set[str] = field(default_factory=set)
    unlocked_regions: set[str] = field(default_factory=lambda: set(INITIAL_REGIONS))
    unlocked_challenge_tier: int = 0
    discoveries: set[str] = field(default_factory=set)
    run_records: list[RunRecord] = field(default_factory=list)

    def to_dict(self) -> dict[str, object]:
        return {
            "profile_format": self.profile_format,
            "unlocked_items": sorted(self.unlocked_items),
            "unlocked_regions": sorted(self.unlocked_regions),
            "unlocked_challenge_tier": self.unlocked_challenge_tier,
            "discoveries": sorted(self.discoveries),
            "run_records": [asdict(record) for record in self.run_records],
        }


def profile_from_dict(data: object) -> PlayerProfile:
    if not isinstance(data, dict) or data.get("profile_format") != PROFILE_FORMAT:
        raise ProfileError(f"incompatible profile format; expected {PROFILE_FORMAT}")
    try:
        profile = PlayerProfile(
            profile_format=PROFILE_FORMAT,
            unlocked_items=set(data["unlocked_items"]),
            unlocked_regions=set(data["unlocked_regions"]),
            unlocked_challenge_tier=data["unlocked_challenge_tier"],
            discoveries=set(data["discoveries"]),
            run_records=[RunRecord(
                run_id=row["run_id"], seed=row["seed"], result=row["result"],
                reason=row["reason"], challenge_tier=row["challenge_tier"],
                region_path=tuple(row["region_path"]), actions=row["actions"],
                boss_kills=tuple(row["boss_kills"]),
                item_stacks=tuple((key, value) for key, value in row["item_stacks"]),
            ) for row in data["run_records"]],
        )
    except (KeyError, TypeError, ValueError) as exc:
        raise ProfileError("malformed ROAG profile") from exc
    from .run_items import RUN_ITEMS

    if (
        type(profile.unlocked_challenge_tier) is not int
        or not 0 <= profile.unlocked_challenge_tier <= 5
        or not INITIAL_REGIONS <= profile.unlocked_regions
        or not profile.unlocked_regions <= ALL_REGIONS
        or not profile.unlocked_items <= set(RUN_ITEMS)
        or len(profile.run_records) > RUN_RECORD_LIMIT
        or len({record.run_id for record in profile.run_records}) != len(profile.run_records)
        or any(record.result not in {"victory", "defeat", "abandoned"}
               or record.actions < 0 or not 0 <= record.challenge_tier <= 5
               or any(item_id not in RUN_ITEMS or type(count) is not int or count < 1
                      for item_id, count in record.item_stacks)
               or any(region_id not in ALL_REGIONS for region_id in record.region_path)
               or any(region_id not in ALL_REGIONS for region_id in record.boss_kills)
               for record in profile.run_records)
    ):
        raise ProfileError("invalid ROAG profile values")
    return profile


def profile_path() -> Path:
    from .save import data_directory

    return data_directory() / PROFILE_NAME


def load_profile(path: Path | None = None) -> PlayerProfile:
    target = path or profile_path()
    try:
        with target.open("r", encoding="utf-8") as handle:
            return profile_from_dict(json.load(handle))
    except FileNotFoundError:
        return PlayerProfile()
    except (OSError, json.JSONDecodeError, ProfileError) as exc:
        raise ProfileError(f"could not load ROAG profile: {exc}") from exc


def save_profile(profile: PlayerProfile, path: Path | None = None) -> Path:
    target = path or profile_path()
    target.parent.mkdir(parents=True, exist_ok=True)
    temporary = target.with_name(f".{target.name}.tmp")
    try:
        with temporary.open("w", encoding="utf-8") as handle:
            json.dump(profile.to_dict(), handle, ensure_ascii=True, indent=2, sort_keys=True)
            handle.write("\n")
            handle.flush()
            os.fsync(handle.fileno())
        os.replace(temporary, target)
    except OSError as exc:
        try:
            temporary.unlink(missing_ok=True)
        except OSError:
            pass
        raise ProfileError(f"could not save ROAG profile: {exc}") from exc
    return target


def record_run(profile: PlayerProfile, state: object) -> RunRecord:
    run = state.run
    if run is None or run.status == "active":
        raise ValueError("only a completed run may enter the profile")
    existing = next(
        (record for record in profile.run_records if record.run_id == run.run_id),
        None,
    )
    if existing is not None:
        run.profile_recorded = True
        return existing
    record = RunRecord(
        run.run_id, state.seed, run.status, run.failure_reason or run.status,
        run.challenge_tier, tuple(run.region_path), run.run_actions,
        tuple(run.boss_kills), tuple(sorted(run.item_stacks.items())),
    )
    profile.run_records.append(record)
    del profile.run_records[:-RUN_RECORD_LIMIT]
    if run.status == "victory" and run.challenge_tier == profile.unlocked_challenge_tier:
        profile.unlocked_challenge_tier = min(5, profile.unlocked_challenge_tier + 1)
    if len(run.boss_kills) >= 2:
        profile.unlocked_regions.add("rillscar")
    if run.status == "victory":
        profile.unlocked_regions.add("frostmere")
    profile.discoveries.update(run.item_stacks)
    from .run_items import INITIAL_ITEM_IDS, RUN_ITEMS

    profile.unlocked_items.update(run.item_stacks)
    locked = sorted(set(RUN_ITEMS) - INITIAL_ITEM_IDS - profile.unlocked_items)
    unlock_count = min(len(locked), len(run.boss_kills) + int(run.status == "victory"))
    if unlock_count:
        from .state import stage_rng

        rng = stage_rng(state.seed, f"profile-unlocks:{run.run_id}")
        rng.shuffle(locked)
        profile.unlocked_items.update(locked[:unlock_count])
    run.profile_recorded = True
    return record


def persist_run_profile(state: object, path: Path | None = None) -> PlayerProfile:
    """Record a settled run once and atomically persist the profile."""
    profile = load_profile(path)
    if state.run is not None and state.run.status != "active":
        record_run(profile, state)
        save_profile(profile, path)
    return profile
