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


def run_27_to_28(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 27:
        raise MigrationError("migration 27->28 requires save version 27")
    state = snapshot.get("state")
    if not isinstance(state, dict) or "ledger" in state or type(state.get("travel_ticks")) is not int:
        raise MigrationError("malformed version-27 snapshot")
    result = deepcopy(snapshot)
    result["save_version"] = 28
    result["state"]["ledger"] = {"schema": 1, "incomplete_before_tick": state["travel_ticks"], "records": []}
    return result


def run_28_to_29(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 28:
        raise MigrationError("migration 28->29 requires save version 28")
    if "resolution_queue" in snapshot or not isinstance(snapshot.get("state"), dict):
        raise MigrationError("malformed version-28 snapshot")
    result = deepcopy(snapshot)
    result["save_version"] = 29
    result["resolution_queue"] = {"budget": 4096, "state": {
        "schema": 1, "next_event_id": 1, "next_root_id": 1, "root_id": None,
        "card_token": None, "combat_token": 0, "turn_token": 0, "pending": [],
        "active": None, "counters": {}, "chain_spent": {}, "sealed_chains": [],
        "trace": [], "seals": [],
    }}
    return result


def run_29_to_30(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 29:
        raise MigrationError("migration 29->30 requires save version 29")
    result = deepcopy(snapshot)
    try:
        queue = result["resolution_queue"]["state"]
        if type(queue["schema"]) is not int or queue["schema"] != 1:
            raise MigrationError("version-29 save requires queue schema 1")
        events = list(queue["pending"])
        if queue["active"] is not None:
            events.append(queue["active"]["event"])
        for event in events:
            if "raw_damage" in event["payload"]:
                raise MigrationError("version-29 payload cannot contain raw_damage")
            event["payload"]["raw_damage"] = False
        queue["schema"] = 2
    except (KeyError, TypeError) as exc:
        raise MigrationError("malformed version-29 resolution queue") from exc
    result["save_version"] = 30
    return result


RUN_MIGRATIONS = {26: run_26_to_27, 27: run_27_to_28, 28: run_28_to_29, 29: run_29_to_30}


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
