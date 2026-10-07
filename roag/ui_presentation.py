"""Resolve safe, selected-pack terminal presentation text."""

from __future__ import annotations

from .catalog import selected_content_pack, ui_contract


def ui_text(semantic_id: str) -> str:
    """Return one immutable-pack literal or validated template."""
    return selected_content_pack().ui_presentation(semantic_id).text


def ui_format(semantic_id: str, /, **values: object) -> str:
    """Format only the engine-owned placeholder surface for a UI key."""
    slots = {slot.id: slot for slot in ui_contract()}
    try:
        slot = slots[semantic_id]
    except KeyError as exc:
        raise KeyError(f"unknown UI semantic id: {semantic_id}") from exc
    if set(values) != set(slot.placeholders):
        raise ValueError(
            f"UI text {semantic_id!r} needs exactly placeholders {', '.join(slot.placeholders) or 'none'}"
        )
    return ui_text(semantic_id).format(**values)


_NOTICE_PREFIXES = {
    "rumour": ("RUMOUR", "RUMOR", "CLAIM", "TESTIMONY", "INFERRED"),
    "fact": ("FACT", "OBSERVED", "CURRENTLY VISIBLE", "PHYSICAL KIT", "BODY"),
    "forecast": ("FORECAST", "PREDICTION", "WEATHER", "SEASON"),
    "warning": ("WARNING", "DANGER", "BLOCKED", "G BLOCKED", "COLLAPSE", "UNLOADED", "UNREADY"),
}


def notice_kind(text: str) -> str | None:
    """Recognize legacy persisted prefixes without treating visible labels as protocol."""
    upper = text.strip().upper()
    for kind, prefixes in _NOTICE_PREFIXES.items():
        if any(upper == prefix or upper.startswith(prefix + ":") for prefix in prefixes):
            return kind
    return None


def notice_label(kind: str) -> str:
    if kind not in _NOTICE_PREFIXES:
        raise KeyError(f"unknown notice kind: {kind}")
    return ui_text(f"ui.notice.{kind}.label")


def render_notice(text: str) -> str:
    """Replace a recognized legacy prefix with selected-pack visible presentation."""
    kind = notice_kind(text)
    if kind is None:
        return text
    stripped = text.strip()
    prefix, separator, remainder = stripped.partition(":")
    return f"{notice_label(kind)}:{separator and ' '}{remainder.lstrip()}" if separator else notice_label(kind)
