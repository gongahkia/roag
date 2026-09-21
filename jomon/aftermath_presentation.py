"""Resolve selected-pack aftermath contract titles from stable contract identities."""
from __future__ import annotations
from .catalog import selected_content_pack


def aftermath_contract_title(contract_id: str) -> str:
    parts = contract_id.split(":")
    if len(parts) != 3 or parts[0] != "contract":
        raise KeyError(f"invalid aftermath contract id: {contract_id!r}")
    return selected_content_pack().aftermath_presentation(
        f"aftermath.contract.{parts[1]}.{parts[2]}"
    ).title


def aftermath_contract_cause(contract_id: str, /, **values: object) -> str:
    parts = contract_id.split(":")
    if len(parts) != 3 or parts[0] != "contract":
        raise KeyError(f"invalid aftermath contract id: {contract_id!r}")
    return selected_content_pack().aftermath_presentation(
        f"aftermath.contract.{parts[1]}.{parts[2]}"
    ).cause.format(**values)


def aftermath_opening(region_id: str) -> str:
    return selected_content_pack().aftermath_opening(region_id)


def aftermath_opening_format(region_id: str, field: str, /, **values: object) -> str:
    return selected_content_pack().aftermath_opening_text(region_id, field).format(**values)
