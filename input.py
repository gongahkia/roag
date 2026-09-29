import argparse

from main import rungame
from functions import configureicontheme


def parseargs ():
    parser = argparse.ArgumentParser(description = 'Play the terminal roguelike.')
    parser.add_argument('--level',choices = ['0','1','2','3','boss'],help = 'Start a level directly for debugging.')
    parser.add_argument('--nerd-font',action = 'store_true',help = 'Use Nerd Font icons instead of ASCII glyphs.')
    return parser.parse_args()


def main ():
    arguments = parseargs()
    configureicontheme(arguments.nerd_font)
    if arguments.level is None:
        rungame()
        return
    if arguments.level == 'boss':
        rungame(3)
        return
    rungame(int(arguments.level))


if __name__ == '__main__':
    main()
