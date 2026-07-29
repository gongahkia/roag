from __future__ import annotations

import os
import sys
import tomllib
from pathlib import Path


def project_version(pyproject_path: Path = Path("pyproject.toml")) -> str:
    with pyproject_path.open("rb") as file:
        payload = tomllib.load(file)
    return str(payload["project"]["version"])


def tag_version(tag: str) -> str:
    return tag[1:] if tag.startswith("v") else tag


def main(argv: list[str] | None = None) -> int:
    args = list(sys.argv[1:] if argv is None else argv)
    tag = args[0] if args else os.environ.get("GITHUB_REF_NAME", "")
    if not tag:
        print("release tag required", file=sys.stderr)
        return 2
    expected = tag_version(tag)
    actual = project_version()
    if actual != expected:
        print(f"release tag {tag!r} does not match pyproject version {actual!r}", file=sys.stderr)
        return 1
    print(f"release version ok: {tag} -> {actual}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
