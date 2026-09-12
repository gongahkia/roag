"""Strict, inert JSON decoding shared by local content and persistence."""

from __future__ import annotations

import json
import math
from typing import Any


class JsonDataError(ValueError):
    pass


def _object(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result = {}
    for key, value in pairs:
        if key in result:
            raise JsonDataError(f"duplicate JSON key: {key}")
        result[key] = value
    return result


def _constant(value: str) -> None:
    raise JsonDataError(f"non-finite JSON number: {value}")


def _float(value: str) -> float:
    result = float(value)
    if not math.isfinite(result):
        raise JsonDataError(f"non-finite JSON number: {value}")
    return result


def loads(text: str) -> Any:
    try:
        return json.loads(text, object_pairs_hook=_object, parse_constant=_constant, parse_float=_float)
    except (ValueError, RecursionError) as exc:
        raise JsonDataError(str(exc)) from exc
