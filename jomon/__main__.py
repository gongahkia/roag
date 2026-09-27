"""Run Jomon's Pygame application with ``python -m jomon``."""

from .catalog import select_content_pack_from_environment


def main() -> None:
    # Select the pack before frontend imports cache presentation data.
    select_content_pack_from_environment()
    from .pygame_frontend import main as pygame_main

    pygame_main()


if __name__ == "__main__":
    main()
