"""Print deterministic local encounter-composition evidence."""

from __future__ import annotations

import json

from .encounters import encounter_audit
from .quests import quest_reachability_audit


def main() -> None:
    print(json.dumps({
        "encounters": encounter_audit(100),
        "quests": quest_reachability_audit(25),
    }, indent=2, sort_keys=True))


if __name__ == "__main__":
    main()
