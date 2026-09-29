"""Opening title screen for Roag."""

from functions import BOARDHEIGHT, BOARDWIDTH, printscreen, promptinput


def titlescreen ():
    lines = [''] * BOARDHEIGHT
    lines[4] = '██████╗  ██████╗  █████╗  ██████╗'
    lines[5] = '██╔══██╗██╔═══██╗██╔══██╗██╔════╝'
    lines[6] = '██████╔╝██║   ██║███████║██║  ███╗'
    lines[7] = '██╔══██╗██║   ██║██╔══██║██║   ██║'
    lines[8] = '██║  ██║╚██████╔╝██║  ██║╚██████╔╝'
    lines[10] = 'A TERMINAL ROGUELIKE'
    lines[12] = 'BY @GONGAHKIA'
    lines[15] = '[E] BEGIN'
    while True:
        printscreen([line.center(BOARDWIDTH) for line in lines])
        if promptinput('[E]: ') == 'e':
            return
