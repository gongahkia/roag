"""Run Jomon with ``python -m jomon``."""

from .catalog import select_content_pack_from_environment


def main() -> None:
    # ``main`` imports modules that cache catalog data. Establish a selected
    # pack before that import so JOMON_CONTENT_PACK affects the whole run.
    select_content_pack_from_environment()
    import curses

    from .main import run

    curses.wrapper(run)


if __name__ == "__main__":
    main()
