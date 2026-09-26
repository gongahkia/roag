"""Selected-pack wording for engine-owned circuit identities and results."""
from __future__ import annotations

from .catalog import selected_content_pack


def circuit_text(semantic_id: str) -> str:
    return selected_content_pack().circuit_presentation(semantic_id).text


def circuit_format(semantic_id: str, /, **values: object) -> str:
    return circuit_text(semantic_id).format(**values)


def circuit_part_name(kind: str) -> str:
    return circuit_text(f"circuit.part.{kind}.name")


def circuit_part_description(kind: str) -> str:
    return circuit_text(f"circuit.part.{kind}.description")


def circuit_layer_name(layer: str) -> str:
    return circuit_text(f"circuit.layer.{layer}")

def circuit_mode_name(mode: str) -> str:
    return circuit_text(f"circuit.mode.{mode}")

def circuit_facing_name(facing: str) -> str:
    return circuit_text(f"circuit.facing.{facing}")


def circuit_mode_title(mode: str) -> str:
    return circuit_text(f"circuit.mode_title.{mode}")
