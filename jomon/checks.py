"""Serial developer checks; the full suite remains unittest discovery."""

import argparse
import unittest


FAST_FILES = (
    "test_performance_contracts.py", "test_inventory.py", "test_enemy_ai.py",
    "test_terminal.py", "test_integrity_v6.py",
    "test_situations.py", "test_manoeuvres.py", "test_interference.py",
    "test_echoes.py", "test_household_stories.py",
)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("suite", choices=("fast", "full"), default="fast", nargs="?")
    parser.add_argument("--pattern", help="run a focused unittest filename pattern instead")
    args = parser.parse_args()
    patterns = (args.pattern,) if args.pattern else FAST_FILES if args.suite == "fast" else ("test*.py",)
    suite = unittest.TestSuite()
    for pattern in patterns:
        suite.addTests(unittest.defaultTestLoader.discover("tests", pattern=pattern))
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    raise SystemExit(0 if result.wasSuccessful() else 1)


if __name__ == "__main__":
    main()
