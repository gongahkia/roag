from __future__ import annotations

import json
from collections import Counter
from collections.abc import Iterable, Sequence
from dataclasses import dataclass
from pathlib import Path
from typing import Any

REPLAY_MANIFEST_KIND = "kenjaku-replay-manifest-v0"
REPLAY_INTAKE_REVIEW_KIND = "kenjaku-replay-intake-review-v0"
REPLAY_INTAKE_ACCEPTED_ITEM_KIND = "kenjaku-replay-intake-item-v0"
REPLAY_SHARE_PLAN_KIND = "kenjaku-replay-share-plan-v0"

PLATFORMS = ("tenhou", "mahjong_soul", "local_file", "synthetic", "other")
PERMISSION_STATUSES = (
    "explicit_permission",
    "user_provided",
    "public_replay",
    "local_synthetic",
    "unknown",
    "denied",
)
INTENDED_USES = ("analysis", "evaluation", "training", "demo", "redistribution")
SHARE_INTENTS = ("demo", "redistribution")
CONSERVATIVE_ALLOWED_USES = ("analysis", "evaluation")
UNRESTRICTED_ALLOWED_USES = INTENDED_USES


@dataclass(frozen=True, slots=True)
class ReplayManifestItem:
    item_id: str
    platform: str
    uri: str
    intended_uses: tuple[str, ...]
    permission_status: str
    permission_scope: tuple[str, ...]
    granted_by: str | None = None
    granted_at: str | None = None
    notes: str | None = None


@dataclass(frozen=True, slots=True)
class ReplayIntakeDecision:
    item: ReplayManifestItem | None
    accepted: bool
    reasons: tuple[str, ...]
    raw_index: int
    raw_id: str | None = None

    def to_payload(self) -> dict[str, Any]:
        item = self.item
        return {
            "index": self.raw_index,
            "id": self.raw_id if item is None else item.item_id,
            "accepted": self.accepted,
            "reasons": list(self.reasons),
            "item": None if item is None else replay_manifest_item_payload(item),
        }


def review_replay_manifest_file(path: str | Path) -> dict[str, Any]:
    manifest_path = Path(path)
    payload = json.loads(manifest_path.read_text(encoding="utf-8"))
    return review_replay_manifest(payload, manifest_path=manifest_path)


def review_replay_manifest(
    payload: dict[str, Any],
    *,
    manifest_path: Path | None = None,
) -> dict[str, Any]:
    if payload.get("kind") != REPLAY_MANIFEST_KIND:
        raise ValueError(f"manifest kind must be {REPLAY_MANIFEST_KIND}")
    raw_items = payload.get("items")
    if not isinstance(raw_items, list):
        raise ValueError("manifest items must be a list")

    decisions = [
        review_replay_manifest_item(raw_item, index=index)
        for index, raw_item in enumerate(raw_items)
    ]
    accepted = [decision for decision in decisions if decision.accepted]
    rejected = [decision for decision in decisions if not decision.accepted]
    platforms = Counter(
        decision.item.platform
        for decision in decisions
        if decision.item is not None
    )
    permission_statuses = Counter(
        decision.item.permission_status
        for decision in decisions
        if decision.item is not None
    )
    return {
        "kind": REPLAY_INTAKE_REVIEW_KIND,
        "manifest_path": None if manifest_path is None else str(manifest_path),
        "source": payload.get("source", {}),
        "items": len(decisions),
        "accepted": len(accepted),
        "rejected": len(rejected),
        "platforms": dict(sorted(platforms.items())),
        "permission_statuses": dict(sorted(permission_statuses.items())),
        "decisions": [decision.to_payload() for decision in decisions],
    }


def review_replay_manifest_item(raw_item: Any, *, index: int) -> ReplayIntakeDecision:
    raw_id = _raw_id(raw_item)
    try:
        item = parse_replay_manifest_item(raw_item)
    except ValueError as error:
        return ReplayIntakeDecision(
            item=None,
            accepted=False,
            reasons=(str(error),),
            raw_index=index,
            raw_id=raw_id,
        )

    reasons = list(_replay_item_rejection_reasons(item))
    return ReplayIntakeDecision(
        item=item,
        accepted=not reasons,
        reasons=tuple(reasons),
        raw_index=index,
        raw_id=item.item_id,
    )


def parse_replay_manifest_item(raw_item: Any) -> ReplayManifestItem:
    if not isinstance(raw_item, dict):
        raise ValueError("manifest item must be an object")

    item_id = _required_string(raw_item, "id")
    platform = _required_string(raw_item, "platform")
    if platform not in PLATFORMS:
        raise ValueError(f"unsupported platform: {platform}")
    uri = _required_string(raw_item, "uri")
    intended_uses = _string_tuple(raw_item.get("intended_uses", ("analysis",)))
    if not intended_uses:
        raise ValueError("intended_uses must not be empty")
    unsupported_uses = [use for use in intended_uses if use not in INTENDED_USES]
    if unsupported_uses:
        raise ValueError("unsupported intended uses: " + ", ".join(unsupported_uses))

    permission = raw_item.get("permission")
    if not isinstance(permission, dict):
        raise ValueError("permission must be an object")
    permission_status = _required_string(permission, "status")
    if permission_status not in PERMISSION_STATUSES:
        raise ValueError(f"unsupported permission status: {permission_status}")
    permission_scope = _permission_scope(permission, permission_status)
    unsupported_scope = [use for use in permission_scope if use not in INTENDED_USES]
    if unsupported_scope:
        raise ValueError("unsupported permission scope uses: " + ", ".join(unsupported_scope))

    return ReplayManifestItem(
        item_id=item_id,
        platform=platform,
        uri=uri,
        intended_uses=intended_uses,
        permission_status=permission_status,
        permission_scope=permission_scope,
        granted_by=_optional_string(permission.get("granted_by"), "granted_by"),
        granted_at=_optional_string(permission.get("granted_at"), "granted_at"),
        notes=_optional_string(permission.get("notes"), "notes"),
    )


def accepted_replay_intake_items(review: dict[str, Any]) -> list[dict[str, Any]]:
    if review.get("kind") != REPLAY_INTAKE_REVIEW_KIND:
        raise ValueError(f"review kind must be {REPLAY_INTAKE_REVIEW_KIND}")
    return [
        {
            "kind": REPLAY_INTAKE_ACCEPTED_ITEM_KIND,
            **decision["item"],
        }
        for decision in review["decisions"]
        if decision.get("accepted") and isinstance(decision.get("item"), dict)
    ]


def write_accepted_replay_intake_jsonl(path: str | Path, review: dict[str, Any]) -> None:
    output_path = Path(path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    rows = accepted_replay_intake_items(review)
    output_path.write_text(
        "".join(json.dumps(row, sort_keys=True) + "\n" for row in rows),
        encoding="utf-8",
    )


def build_replay_share_plan_file(path: str | Path, *, intent: str = "demo") -> dict[str, Any]:
    input_path = Path(path)
    rows = [
        json.loads(line)
        for line in input_path.read_text(encoding="utf-8").splitlines()
        if line.strip()
    ]
    return build_replay_share_plan(rows, input_path=input_path, intent=intent)


def build_replay_share_plan(
    rows: Sequence[Any],
    *,
    input_path: Path | None = None,
    intent: str = "demo",
) -> dict[str, Any]:
    if intent not in SHARE_INTENTS:
        raise ValueError("share intent must be one of: " + ", ".join(SHARE_INTENTS))

    decisions = [
        _replay_share_decision(row, index=index, intent=intent)
        for index, row in enumerate(rows)
    ]
    shareable = [decision for decision in decisions if decision["shareable"]]
    blocked = [decision for decision in decisions if not decision["shareable"]]
    platforms = Counter(
        decision["item"]["platform"]
        for decision in decisions
        if isinstance(decision.get("item"), dict)
    )
    return {
        "kind": REPLAY_SHARE_PLAN_KIND,
        "accepted_input_path": None if input_path is None else str(input_path),
        "intent": intent,
        "items": len(decisions),
        "shareable": len(shareable),
        "blocked": len(blocked),
        "platforms": dict(sorted(platforms.items())),
        "decisions": decisions,
    }


def format_replay_share_plan(plan: dict[str, Any]) -> str:
    if plan.get("kind") != REPLAY_SHARE_PLAN_KIND:
        raise ValueError(f"share plan kind must be {REPLAY_SHARE_PLAN_KIND}")
    lines = [
        f"intent: {plan['intent']}",
        f"items: {plan['items']}",
        f"shareable: {plan['shareable']}",
        f"blocked: {plan['blocked']}",
    ]
    if plan["platforms"]:
        lines.append("platforms: " + _format_counts(plan["platforms"]))
    blocked = [decision for decision in plan["decisions"] if not decision["shareable"]]
    if blocked:
        lines.append("blocked_items:")
        for decision in blocked:
            reasons = "; ".join(decision["reasons"])
            lines.append(f"  {decision.get('id') or '<missing>'}: {reasons}")
    return "\n".join(lines)


def format_replay_intake_review(review: dict[str, Any]) -> str:
    if review.get("kind") != REPLAY_INTAKE_REVIEW_KIND:
        raise ValueError(f"review kind must be {REPLAY_INTAKE_REVIEW_KIND}")
    lines = [
        f"items: {review['items']}",
        f"accepted: {review['accepted']}",
        f"rejected: {review['rejected']}",
    ]
    if review["platforms"]:
        lines.append("platforms: " + _format_counts(review["platforms"]))
    if review["permission_statuses"]:
        lines.append("permission_statuses: " + _format_counts(review["permission_statuses"]))
    rejected = [decision for decision in review["decisions"] if not decision["accepted"]]
    if rejected:
        lines.append("rejections:")
        for decision in rejected:
            reasons = "; ".join(decision["reasons"])
            lines.append(f"  {decision.get('id') or '<missing>'}: {reasons}")
    return "\n".join(lines)


def replay_manifest_item_payload(item: ReplayManifestItem) -> dict[str, Any]:
    return {
        "id": item.item_id,
        "platform": item.platform,
        "uri": item.uri,
        "intended_uses": list(item.intended_uses),
        "permission": {
            "status": item.permission_status,
            "scope": list(item.permission_scope),
            "granted_by": item.granted_by,
            "granted_at": item.granted_at,
            "notes": item.notes,
        },
    }


def _replay_item_rejection_reasons(item: ReplayManifestItem) -> Iterable[str]:
    if item.permission_status in {"unknown", "denied"}:
        yield f"permission status is {item.permission_status}"
    missing_scope = [use for use in item.intended_uses if use not in item.permission_scope]
    if missing_scope:
        yield "permission scope does not cover intended uses: " + ", ".join(missing_scope)
    if (
        item.platform == "mahjong_soul"
        and item.permission_status != "explicit_permission"
        and any(use in {"training", "demo", "redistribution"} for use in item.intended_uses)
    ):
        yield "mahjong_soul replay intake is limited to analysis/evaluation without explicit review"
    if item.platform == "tenhou" and "redistribution" in item.intended_uses:
        yield "tenhou replay redistribution is not allowed by this intake gate"
    if item.platform == "local_file" and not item.uri:
        yield "local_file uri must not be empty"


def _replay_share_decision(row: Any, *, index: int, intent: str) -> dict[str, Any]:
    row_id = row.get("id") if isinstance(row, dict) and isinstance(row.get("id"), str) else None
    if not isinstance(row, dict):
        return {
            "index": index,
            "id": None,
            "shareable": False,
            "reasons": ["accepted queue row must be an object"],
            "item": None,
        }
    if row.get("kind") != REPLAY_INTAKE_ACCEPTED_ITEM_KIND:
        return {
            "index": index,
            "id": row_id,
            "shareable": False,
            "reasons": [f"row kind must be {REPLAY_INTAKE_ACCEPTED_ITEM_KIND}"],
            "item": row,
        }

    permission = row.get("permission")
    intended_uses = _safe_string_tuple(row.get("intended_uses"))
    permission_scope = (
        _safe_string_tuple(permission.get("scope"))
        if isinstance(permission, dict)
        else ()
    )
    reasons: list[str] = []
    if intent not in intended_uses:
        reasons.append(f"accepted item intended_uses does not include {intent}")
    if intent not in permission_scope:
        reasons.append(f"permission scope does not include {intent}")
    if intent == "redistribution" and row.get("platform") == "tenhou":
        reasons.append("tenhou replay redistribution is not allowed by this share planner")
    return {
        "index": index,
        "id": row_id,
        "platform": row.get("platform"),
        "uri": row.get("uri"),
        "shareable": not reasons,
        "reasons": reasons,
        "item": row,
    }


def _permission_scope(permission: dict[str, Any], status: str) -> tuple[str, ...]:
    if "scope" in permission:
        return _string_tuple(permission["scope"])
    if status == "explicit_permission":
        raise ValueError("explicit_permission requires a permission scope")
    if status == "local_synthetic":
        return UNRESTRICTED_ALLOWED_USES
    if status in {"user_provided", "public_replay"}:
        return CONSERVATIVE_ALLOWED_USES
    return ()


def _required_string(payload: dict[str, Any], key: str) -> str:
    value = payload.get(key)
    if not isinstance(value, str) or not value.strip():
        raise ValueError(f"{key} must be a non-empty string")
    return value


def _optional_string(value: Any, key: str) -> str | None:
    if value is None:
        return None
    if not isinstance(value, str):
        raise ValueError(f"{key} must be a string")
    return value


def _string_tuple(value: Any) -> tuple[str, ...]:
    if isinstance(value, str):
        return (value,)
    if not isinstance(value, Sequence):
        raise ValueError("value must be a string or list of strings")
    values: list[str] = []
    for item in value:
        if not isinstance(item, str) or not item.strip():
            raise ValueError("value must contain only non-empty strings")
        values.append(item)
    return tuple(values)


def _safe_string_tuple(value: Any) -> tuple[str, ...]:
    try:
        return _string_tuple(value)
    except ValueError:
        return ()


def _raw_id(raw_item: Any) -> str | None:
    if isinstance(raw_item, dict) and isinstance(raw_item.get("id"), str):
        return raw_item["id"]
    return None


def _format_counts(counts: dict[str, int]) -> str:
    return " ".join(f"{key}={counts[key]}" for key in sorted(counts))
