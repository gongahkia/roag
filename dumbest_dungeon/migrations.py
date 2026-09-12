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


def run_37_to_38(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 37:
        raise MigrationError("migration 37->38 requires save version 37")
    state = snapshot.get("state")
    manifest = snapshot.get("content_manifest")
    zones = ("deck", "hand", "draw_pile", "discard_pile")
    if (
        not isinstance(state, dict)
        or "next_card_copy_id" in state
        or not isinstance(manifest, dict)
        or manifest.get("engine") != "0.6.0"
        or any(not isinstance(state.get(zone), list) for zone in zones)
    ):
        raise MigrationError("version-37 save requires the pre-card-identity contract")
    result = deepcopy(snapshot)
    result["save_version"] = 38
    result["content_manifest"]["engine"] = "0.7.0"

    available: dict[tuple[Any, Any, Any], list[int]] = {}
    for copy_id, card in enumerate(result["state"]["deck"], 1):
        if not isinstance(card, dict) or set(card) != {"card_id", "upgraded", "bound_hero_id"}:
            raise MigrationError("version-37 deck contains a malformed card")
        key = (card["card_id"], card["upgraded"], card["bound_hero_id"])
        available.setdefault(key, []).append(copy_id)
        card.update(copy_id=copy_id, mastery=None, infusion_id=None)

    active_ids: list[int] = []
    for zone in ("hand", "draw_pile", "discard_pile"):
        for card in result["state"][zone]:
            if not isinstance(card, dict) or set(card) != {"card_id", "upgraded", "bound_hero_id"}:
                raise MigrationError(f"version-37 {zone} contains a malformed card")
            key = (card["card_id"], card["upgraded"], card["bound_hero_id"])
            choices = available.get(key)
            if not choices:
                raise MigrationError("version-37 combat cards do not match its durable deck")
            copy_id = choices.pop(0)
            active_ids.append(copy_id)
            card.update(copy_id=copy_id, mastery=None, infusion_id=None)
    if state.get("phase") == "combat":
        if any(available.values()):
            raise MigrationError("version-37 combat zones omit a durable deck card")
    elif active_ids:
        raise MigrationError("version-37 non-combat save contains combat cards")
    result["state"]["next_card_copy_id"] = len(result["state"]["deck"]) + 1
    return result


def run_38_to_39(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 38:
        raise MigrationError("migration 38->39 requires save version 38")
    manifest = snapshot.get("content_manifest")
    queue = snapshot.get("resolution_queue", {}).get("state")
    if (
        not isinstance(manifest, dict)
        or manifest.get("engine") != "0.7.0"
        or not isinstance(queue, dict)
        or queue.get("schema") != 3
    ):
        raise MigrationError("version-38 save requires queue schema 3 and engine 0.7.0")
    result = deepcopy(snapshot)
    events = list(result["resolution_queue"]["state"]["pending"])
    active = result["resolution_queue"]["state"]["active"]
    if active is not None:
        events.append(active["event"])
    for event in events:
        payload = event.get("payload") if isinstance(event, dict) else None
        if not isinstance(payload, dict) or "card_mastery" in payload:
            raise MigrationError("version-38 queue contains a malformed event payload")
        payload["card_mastery"] = None
    result["resolution_queue"]["state"]["schema"] = 4
    result["save_version"] = 39
    result["content_manifest"]["engine"] = "0.8.0"
    return result


def run_39_to_40(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 39:
        raise MigrationError("migration 39->40 requires save version 39")
    manifest = snapshot.get("content_manifest")
    queue = snapshot.get("resolution_queue", {}).get("state")
    if (
        not isinstance(manifest, dict)
        or manifest.get("engine") != "0.8.0"
        or not isinstance(queue, dict)
        or queue.get("schema") != 4
    ):
        raise MigrationError("version-39 save requires queue schema 4 and engine 0.8.0")
    result = deepcopy(snapshot)
    events = list(result["resolution_queue"]["state"]["pending"])
    active = result["resolution_queue"]["state"]["active"]
    if active is not None:
        events.append(active["event"])
    for event in events:
        payload = event.get("payload") if isinstance(event, dict) else None
        if not isinstance(payload, dict) or {"card_copy_id", "card_infusion"} & payload.keys():
            raise MigrationError("version-39 queue contains a malformed event payload")
        payload.update(card_copy_id=0, card_infusion=None)
    result["resolution_queue"]["state"]["schema"] = 5
    result["save_version"] = 40
    result["content_manifest"]["engine"] = "0.9.0"
    return result


def run_40_to_41(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 40:
        raise MigrationError("migration 40->41 requires save version 40")
    manifest = snapshot.get("content_manifest")
    state = snapshot.get("state")
    queue = snapshot.get("resolution_queue", {}).get("state")
    if (
        not isinstance(manifest, dict)
        or manifest.get("engine") != "0.9.0"
        or not isinstance(state, dict)
        or {"hub_loadouts", "doctrine_id"} & state.keys()
        or not isinstance(queue, dict)
        or queue.get("schema") != 5
    ):
        raise MigrationError("version-40 save requires queue schema 5 and engine 0.9.0")
    result = deepcopy(snapshot)
    result["state"]["hub_loadouts"] = {}
    result["state"]["doctrine_id"] = None
    result["save_version"] = 41
    result["content_manifest"]["engine"] = "1.0.0"
    return result


def run_41_to_42(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 41:
        raise MigrationError("migration 41->42 requires save version 41")
    manifest = snapshot.get("content_manifest")
    state = snapshot.get("state")
    queue = snapshot.get("resolution_queue", {}).get("state")
    if (
        not isinstance(manifest, dict)
        or manifest.get("engine") != "1.0.0"
        or not isinstance(state, dict)
        or "recycler_credits" in state
        or not isinstance(queue, dict)
        or queue.get("schema") != 5
    ):
        raise MigrationError("version-41 save requires queue schema 5 and engine 1.0.0")
    result = deepcopy(snapshot)
    result["state"]["recycler_credits"] = 0
    result["save_version"] = 42
    result["content_manifest"]["engine"] = "1.1.0"
    return result


def run_42_to_43(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 42:
        raise MigrationError("migration 42->43 requires save version 42")
    manifest = snapshot.get("content_manifest")
    state = snapshot.get("state")
    if (
        not isinstance(manifest, dict)
        or manifest.get("engine") != "1.1.0"
        or not isinstance(state, dict)
        or "ladder_rank" in state
    ):
        raise MigrationError("version-42 save requires the pre-ladder engine 1.1.0 contract")
    result = deepcopy(snapshot)
    result["state"]["ladder_rank"] = 0
    result["save_version"] = 43
    result["content_manifest"]["engine"] = "1.2.0"
    return result


def run_43_to_44(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 43:
        raise MigrationError("migration 43->44 requires save version 43")
    manifest = snapshot.get("content_manifest")
    state = snapshot.get("state")
    if (
        not isinstance(manifest, dict)
        or manifest.get("engine") != "1.2.0"
        or not isinstance(state, dict)
        or any(key in state for key in ("expedition_mode", "active_modifiers", "enabled_packs"))
    ):
        raise MigrationError("version-43 save requires the pre-challenge engine 1.2.0 contract")
    result = deepcopy(snapshot)
    result["state"].update(
        expedition_mode="standard", active_modifiers=[], enabled_packs=["base:core"]
    )
    result["save_version"] = 44
    result["content_manifest"]["engine"] = "1.3.0"
    return result


def run_44_to_45(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 44:
        raise MigrationError("migration 44->45 requires save version 44")
    manifest = snapshot.get("content_manifest")
    state = snapshot.get("state")
    loop_fields = {
        "base_victory", "base_victory_archived", "loop_depth",
        "archived_loop_depth", "score", "boss_sequence",
    }
    if (
        not isinstance(manifest, dict)
        or manifest.get("engine") != "1.3.0"
        or not isinstance(state, dict)
        or loop_fields & set(state)
    ):
        raise MigrationError("version-44 save requires the pre-loop engine 1.3.0 contract")
    result = deepcopy(snapshot)
    result["state"].update(
        base_victory=False,
        base_victory_archived=False,
        loop_depth=0,
        archived_loop_depth=0,
        score=0,
        boss_sequence=[],
    )
    result["save_version"] = 45
    result["content_manifest"]["engine"] = "1.4.0"
    return result


def run_45_to_46(snapshot: dict[str, Any]) -> dict[str, Any]:
    if type(snapshot.get("save_version")) is not int or snapshot["save_version"] != 45:
        raise MigrationError("migration 45->46 requires save version 45")
    manifest = snapshot.get("content_manifest")
    state = snapshot.get("state")
    if (
        not isinstance(manifest, dict)
        or manifest.get("engine") != "1.4.0"
        or not isinstance(state, dict)
        or "encounter_budget_version" in state
    ):
        raise MigrationError("version-45 save requires the legacy HP-budget engine 1.4.0 contract")
    result = deepcopy(snapshot)
    result["state"]["encounter_budget_version"] = 0
    result["save_version"] = 46
    result["content_manifest"]["engine"] = "1.5.0"
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
    37: run_37_to_38,
    38: run_38_to_39,
    39: run_39_to_40,
    40: run_40_to_41,
    41: run_41_to_42,
    42: run_42_to_43,
    43: run_43_to_44,
    44: run_44_to_45,
    45: run_45_to_46,
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
