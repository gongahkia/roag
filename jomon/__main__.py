"""Run Jomon with ``python -m jomon``."""

import curses

from .main import run


def main() -> None:
    curses.wrapper(run)


if __name__ == "__main__":
    main()
