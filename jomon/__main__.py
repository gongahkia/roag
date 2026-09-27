"""Pygame-only Jomon entrypoint."""
from .catalog import select_content_pack_from_environment
from .pygame_frontend import main
def run(): select_content_pack_from_environment(); main()
if __name__=="__main__":run()
