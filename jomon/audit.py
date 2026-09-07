"""Print deterministic local encounter-composition evidence."""

from __future__ import annotations

import json

from .encounters import encounter_audit


def main() -> None:
    print(json.dumps(encounter_audit(100), indent=2, sort_keys=True))


if __name__ == "__main__":
    main()
