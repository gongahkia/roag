"""Load inert, packaged main-world content without executing Python from data files."""

from __future__ import annotations

import json
from importlib.resources import files
from typing import Any


class CatalogError(ValueError):
    pass


def _unique_object(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result = {}
    for key, value in pairs:
        if key in result:
            raise CatalogError(f"duplicate catalog key: {key}")
        result[key] = value
    return result


def _reject_constant(value: str) -> None:
    raise CatalogError(f"non-finite catalog number: {value}")


def decode_catalog(text: str, name: str, sections: tuple[str, ...]) -> dict[str, Any]:
    try:
        document = json.loads(text, object_pairs_hook=_unique_object, parse_constant=_reject_constant)
    except (ValueError, RecursionError) as exc:
        raise CatalogError(f"invalid {name}: {exc}") from exc
    if not isinstance(document, dict) or set(document) != set(sections):
        raise CatalogError(f"{name} must contain exactly {', '.join(sections)}")
    return document


def load_catalog(name: str, sections: tuple[str, ...]) -> dict[str, Any]:
    if not name.endswith(".json") or "/" in name or "\\" in name:
        raise CatalogError("catalog names must be local JSON files")
    try:
        text = files("jomon").joinpath("data", name).read_text(encoding="utf-8")
    except OSError as exc:
        raise CatalogError(f"cannot read {name}: {exc}") from exc
    return decode_catalog(text, name, sections)
