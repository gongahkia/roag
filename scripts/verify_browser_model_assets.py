#!/usr/bin/env python3
"""Verify browser model assets staged for static deployment."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any

MODEL_ASSET_MANIFEST_KIND = "kenjaku-browser-model-assets-v1"
_MANIFEST_FIELDS = frozenset({"kind", "assets"})
_ASSET_FIELDS = frozenset({"path", "sha256", "bytes"})
_SHA256 = re.compile(r"[0-9a-f]{64}")


def verify_browser_model_assets(site_dir: Path) -> int:
    """Verify each declared browser model asset and return its count."""
    site_dir = site_dir.resolve()
    manifest_path = site_dir / "models" / "manifest.json"
    payload = _read_manifest(manifest_path)
    _require_exact_fields(payload, _MANIFEST_FIELDS, "model asset manifest")
    if payload["kind"] != MODEL_ASSET_MANIFEST_KIND:
        raise ValueError("model asset manifest kind is unsupported")
    assets = payload["assets"]
    if not isinstance(assets, list):
        raise ValueError("model asset manifest assets must be an array")
    declared_paths: set[str] = set()
    for index, asset in enumerate(assets):
        _verify_asset(site_dir, asset, index, declared_paths)
    return len(assets)


def _read_manifest(path: Path) -> dict[str, Any]:
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as error:
        raise ValueError("browser model asset manifest is missing") from error
    except json.JSONDecodeError as error:
        raise ValueError("browser model asset manifest is invalid JSON") from error
    if not isinstance(payload, dict):
        raise ValueError("browser model asset manifest must be an object")
    return payload


def _verify_asset(
    site_dir: Path,
    asset: Any,
    index: int,
    declared_paths: set[str],
) -> None:
    if not isinstance(asset, dict):
        raise ValueError(f"model asset {index} must be an object")
    _require_exact_fields(asset, _ASSET_FIELDS, f"model asset {index}")
    relative_path = asset["path"]
    expected_hash = asset["sha256"]
    expected_bytes = asset["bytes"]
    if not isinstance(relative_path, str) or not _is_safe_relative_path(relative_path):
        raise ValueError(f"model asset {index} path is unsafe")
    if relative_path == "models/manifest.json":
        raise ValueError(f"model asset {index} cannot reference the manifest")
    if relative_path in declared_paths:
        raise ValueError(f"model asset {index} path is duplicated")
    declared_paths.add(relative_path)
    if not isinstance(expected_hash, str) or _SHA256.fullmatch(expected_hash) is None:
        raise ValueError(f"model asset {index} sha256 must be lowercase hexadecimal")
    if (
        isinstance(expected_bytes, bool)
        or not isinstance(expected_bytes, int)
        or expected_bytes <= 0
    ):
        raise ValueError(f"model asset {index} bytes must be a positive integer")
    asset_path = site_dir.joinpath(*relative_path.split("/"))
    if not asset_path.is_file() or asset_path.is_symlink():
        raise ValueError(f"model asset {index} is missing or not a regular file")
    try:
        asset_path.resolve().relative_to(site_dir)
    except ValueError as error:
        raise ValueError(f"model asset {index} escapes the site directory") from error
    if asset_path.stat().st_size != expected_bytes:
        raise ValueError(f"model asset {index} byte size differs")
    if _sha256(asset_path) != expected_hash:
        raise ValueError(f"model asset {index} sha256 differs")


def _is_safe_relative_path(value: str) -> bool:
    if not value or value.startswith("/") or "\\" in value or "\x00" in value:
        return False
    return all(part not in {"", ".", ".."} for part in value.split("/"))


def _require_exact_fields(payload: dict[str, Any], expected: frozenset[str], name: str) -> None:
    actual = set(payload)
    if actual == expected:
        return
    missing = sorted(expected - actual)
    unexpected = sorted(actual - expected)
    details = []
    if missing:
        details.append("missing=" + ",".join(missing))
    if unexpected:
        details.append("unexpected=" + ",".join(unexpected))
    raise ValueError(f"{name} fields must match v1 schema: {'; '.join(details)}")


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser(description="verify browser model assets")
    parser.add_argument("site_dir", type=Path)
    args = parser.parse_args()
    try:
        count = verify_browser_model_assets(args.site_dir)
    except ValueError as error:
        print(f"browser model asset verification failed: {error}", file=sys.stderr)
        return 1
    print(f"verified {count} browser model assets")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
