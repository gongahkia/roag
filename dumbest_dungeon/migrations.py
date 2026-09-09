"""Pure one-version migrations. Historical metadata is authored, never regenerated."""

from __future__ import annotations

from copy import deepcopy
from typing import Any

from .versions import RUN_SAVE_SCHEMA

LEGACY_20_FINGERPRINT = "b6b8c6fe837b9035b498cd867ffe29c80620429dd56d50bc6c29197389f5502b"


class MigrationError(ValueError):
    pass


def run_26_to_27(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 26:
        raise MigrationError("migration 26->27 requires save version 26")
    if type(snapshot.get("content_schema_version")) is not int or snapshot["content_schema_version"] != 20:
        raise MigrationError("no historical manifest for this content schema")
    if "content_manifest" in snapshot or not isinstance(snapshot.get("state"), dict) or "rng_state" not in snapshot:
        raise MigrationError("malformed version-26 snapshot")
    result = deepcopy(snapshot)
    result["save_version"] = 27
    result["content_manifest"] = {
        "schema": 1, "engine": "0.1.0", "content_schema": 20, "rng_architecture": 1,
        "fingerprint": LEGACY_20_FINGERPRINT, "enabled_packs": ["base:core"],
    }
    return result


RUN_MIGRATIONS = {26: run_26_to_27}


def migrate_run(snapshot: dict[str, Any]) -> dict[str, Any]:
    if not isinstance(snapshot, dict) or type(snapshot.get("save_version")) is not int:
        raise MigrationError("save requires an integer version")
    current = snapshot
    while current["save_version"] != RUN_SAVE_SCHEMA:
        version = current["save_version"]
        migration = RUN_MIGRATIONS.get(version)
        if migration is None:
            raise MigrationError(f"unsupported save version {version}")
        current = migration(current)
        if current.get("save_version") != version + 1:
            raise MigrationError("migration did not advance exactly one version")
    return current
