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


def run_30_to_31(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 30:
        raise MigrationError("migration 30->31 requires save version 30")
    result = deepcopy(snapshot)
    try:
        queue = result["resolution_queue"]["state"]
        if type(queue["schema"]) is not int or queue["schema"] != 2:
            raise MigrationError("version-30 save requires queue schema 2")
        events = list(queue["pending"])
        if queue["active"] is not None:
            events.append(queue["active"]["event"])
        for event in events:
            if "deferred" in event or {"card_upgraded", "effect_index"} & event["payload"].keys():
                raise MigrationError("version-30 event contains continuation fields")
            event["deferred"] = False
            event["payload"].update(card_upgraded=False, effect_index=None)
        queue["schema"] = 3
    except (KeyError, TypeError, AttributeError) as exc:
        raise MigrationError("malformed version-30 resolution queue") from exc
    result["save_version"] = 31
    return result


def run_31_to_32(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 31:
        raise MigrationError("migration 31->32 requires save version 31")
    manifest = snapshot.get("content_manifest")
    if not isinstance(manifest, dict) or manifest.get("engine") != "0.1.0" or manifest.get("content_schema") != 20:
        raise MigrationError("version-31 migration requires the recorded engine/content contract")
    result = deepcopy(snapshot)
    result["save_version"] = 32
    result["content_manifest"]["engine"] = "0.2.0"
    return result


def run_32_to_33(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 32:
        raise MigrationError("migration 32->33 requires save version 32")
    manifest = snapshot.get("content_manifest")
    if ("content_rules" in snapshot or not isinstance(manifest, dict)
        or manifest.get("fingerprint") != LEGACY_20_FINGERPRINT or manifest.get("content_schema") != 20
        or manifest.get("engine") != "0.2.0"):
        raise MigrationError("version-32 migration requires its recorded historical content manifest")
    result = deepcopy(snapshot)
    result["save_version"] = 33
    result["content_rules"] = None
    return result


def run_33_to_34(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 33:
        raise MigrationError("migration 33->34 requires save version 33")
    state = snapshot.get("state")
    manifest = snapshot.get("content_manifest")
    fields = {"pressure", "pressure_recent", "pressure_incomplete_before_tick"}
    if (not isinstance(state, dict) or fields & state.keys()
        or type(state.get("travel_ticks")) is not int
        or not isinstance(manifest, dict) or manifest.get("engine") != "0.2.0"):
        raise MigrationError("version-33 migration requires the recorded pre-pressure contract")
    result = deepcopy(snapshot)
    result["save_version"] = 34
    result["content_manifest"]["engine"] = "0.3.0"
    result["state"].update(
        pressure=0,
        pressure_recent=[],
        pressure_incomplete_before_tick=state["travel_ticks"],
    )
    return result


def run_34_to_35(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 34:
        raise MigrationError("migration 34->35 requires save version 34")
    state = snapshot.get("state")
    manifest = snapshot.get("content_manifest")
    fields = {"encounter_pressure", "encounter_modules", "reinforcement_tickets"}
    if (not isinstance(state, dict) or fields & state.keys()
        or not isinstance(manifest, dict) or manifest.get("engine") != "0.3.0"
        or not isinstance(state.get("phase"), str)):
        raise MigrationError("version-34 migration requires the pre-director contract")
    result = deepcopy(snapshot)
    result["save_version"] = 35
    result["content_manifest"]["engine"] = "0.4.0"
    result["state"].update(
        # Existing combats began before the director existed. Freezing them at
        # QUIET preserves their displayed formation and arithmetic.
        encounter_pressure=0 if state["phase"] == "combat" else None,
        encounter_modules=[],
        reinforcement_tickets=0,
    )
    return result


def run_35_to_36(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 35:
        raise MigrationError("migration 35->36 requires save version 35")
    manifest = snapshot.get("content_manifest")
    if (not isinstance(manifest, dict) or manifest.get("engine") != "0.4.0"
        or manifest.get("rng_architecture") != 1):
        raise MigrationError("version-35 manifest requires RNG architecture 1")
    result = deepcopy(snapshot)
    result["save_version"] = 36
    result["content_manifest"]["engine"] = "0.5.0"
    result["content_manifest"]["rng_architecture"] = 2
    return result


def run_36_to_37(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 36:
        raise MigrationError("migration 36->37 requires save version 36")
    state = snapshot.get("state")
    manifest = snapshot.get("content_manifest")
    if (not isinstance(state, dict) or "reinforcement_reserve_id" in state
        or not isinstance(manifest, dict) or manifest.get("engine") != "0.5.0"
        or type(state.get("reinforcement_tickets")) is not int
        or state["reinforcement_tickets"] != 0):
        raise MigrationError("version-36 save requires the pre-reserve encounter contract")
    result = deepcopy(snapshot)
    result["save_version"] = 37
    result["content_manifest"]["engine"] = "0.6.0"
    result["state"]["reinforcement_reserve_id"] = None
    return result


RUN_MIGRATIONS = {
    26: run_26_to_27,
    27: run_27_to_28,
    28: run_28_to_29,
    29: run_29_to_30,
    30: run_30_to_31,
    31: run_31_to_32,
    32: run_32_to_33,
    33: run_33_to_34,
    34: run_34_to_35,
    35: run_35_to_36,
    36: run_36_to_37,
}


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
