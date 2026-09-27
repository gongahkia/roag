"""Terminal rendering, input, and HUD helpers for Roag."""

import sys

try:
    import termios
except ImportError:
    termios = None

import functions as core

from functions import BOARDHEIGHT, BOARDWIDTH, SIDEBARWIDTH, CYAN, RESET, WHITE, icon, iconlabel


def clearscreen ():
    print ('\033[2J\033[H',end = '')


def colourtext (text, colour):
    return f'{colour}{text}{RESET}'


def colourboardline (line):
    colouredline = ''
    for glyph in line:
        if glyph in core.COLOURS:
            colouredline += colourtext(glyph,core.COLOURS[glyph])
        else:
            colouredline += glyph
    return colouredline


def readkey ():
    if termios is None or not sys.stdin.isatty():
        return input().lower()

    filedescriptor = sys.stdin.fileno()
    oldsettings = termios.tcgetattr(filedescriptor)
    newsettings = termios.tcgetattr(filedescriptor)
    newsettings[3] &= ~(termios.ICANON | termios.ECHO)
    newsettings[6][termios.VMIN] = 1
    newsettings[6][termios.VTIME] = 0
    try:
        termios.tcsetattr(filedescriptor,termios.TCSADRAIN,newsettings)
        user = sys.stdin.read(1)
    finally:
        termios.tcsetattr(filedescriptor,termios.TCSADRAIN,oldsettings)
    print (colourtext(user,WHITE))
    return user.lower()


def promptinput (prompt):
    print (colourtext(f'{prompt:^{BOARDWIDTH}}',WHITE),end = '',flush = True)
    return readkey()


def printscreen (lines, gameboard = False):
    lines = list(lines)
    if len(lines) < BOARDHEIGHT:
        padding = BOARDHEIGHT - len(lines)
        lines = [''] * (padding // 2) + lines + [''] * (padding - padding // 2)
    else:
        lines = lines[:BOARDHEIGHT]
    clearscreen()
    print (colourtext('X' * (BOARDWIDTH + 2),CYAN))
    for line in lines:
        line = line[:BOARDWIDTH].ljust(BOARDWIDTH)
        if gameboard:
            line = colourboardline(line)
        border = colourtext('X',CYAN)
        print (f'{border}{line}{border}')
    print (colourtext('X' * (BOARDWIDTH + 2),CYAN))


def boardlines (entitydict, camera = None):
    lines = []
    if camera is None:
        camera = (0,0)
    left = max(0,min(camera[0] - BOARDWIDTH // 2,core.WORLDWIDTH - BOARDWIDTH))
    bottom = max(0,min(camera[1] - BOARDHEIGHT // 2,core.WORLDHEIGHT - BOARDHEIGHT))
    for y in reversed(range(BOARDHEIGHT)):
        string = ''
        for x in range(BOARDWIDTH):
            string += entitydict.get((left + x,bottom + y),' ')
        lines.append(string)
    return lines


def gameoverlines ():
    lines = ['' for row in range(BOARDHEIGHT)]
    lines[BOARDHEIGHT // 2 - 1] = '~GAME OVER~'.center(BOARDWIDTH)
    lines[BOARDHEIGHT // 2 + 1] = 'THANKS FOR PLAYING'.center(BOARDWIDTH)
    return lines


def printdict (entitydict):
    printscreen(boardlines(entitydict),True)


def centersidebar (lines):
    sidebar = []
    for line in lines:
        sidebar.extend(line.split('\n'))
    padding = max((BOARDHEIGHT - len(sidebar)) // 2,0)
    return [''] * padding + sidebar


def printgameframe (entitydict, sidebar, gameover = False, camera = None):
    sidebar = centersidebar(sidebar)
    lines = gameoverlines() if gameover else boardlines(entitydict,camera)
    clearscreen()
    border = colourtext('X',CYAN)
    topborder = colourtext('X' * (BOARDWIDTH + 2),CYAN)
    print(topborder)
    for index,line in enumerate(lines):
        line = line[:BOARDWIDTH].ljust(BOARDWIDTH)
        side = sidebar[index] if index < len(sidebar) else ''
        boardline = colourtext(line,WHITE) if gameover else colourboardline(line)
        print(f'{border}{boardline}{border}  {colourtext(side.center(SIDEBARWIDTH),WHITE)}')
    print(topborder)


def statbar (value, maximum):
    filled = min(max(value,0),maximum)
    return f"[{'X' * filled}{'-' * (maximum - filled)}]"


def hudlines (player, level = None, enemyboss = None, armedbombs = 0, scoregoal = 5, curse = None, armedflares = 0):
    lines = []
    if level is not None:
        lines.append(iconlabel('threatcon',f'THREATCON: {statbar(level + 1,3)}'))
    if curse is not None:
        lines.append(iconlabel('curse',f'CURSE: {curse.replace("_"," ").upper()}'))
    lines.append(f'CLASS: {player.classname}')
    if player.boonname != '':
        lines.append(f'BOON: {player.boonname}')
    if enemyboss is not None:
        lines.append(iconlabel('boss',f'BOSS HEALTH: {statbar(enemyboss.health,10)}'))
        if enemyboss.attackcounter > 0:
            lines.append(iconlabel('attack_charge',f'BOSS WINDUP: {enemyboss.attackname}'))
    lines.append(iconlabel('health',f'HEALTH: {statbar(player.health,5)}'))
    lines.append(iconlabel('hud_ammo',f'AMMO: {statbar(player.ammo,5)}'))
    lines.append(iconlabel('hud_bombs',f'BOMBS: {statbar(player.bombs,5)}  ARMED: {statbar(armedbombs,3)}'))
    lines.append(iconlabel('hud_flares',f'FLARES: {statbar(player.flares,5)}  LIT: {statbar(armedflares,3)}'))
    lines.append(iconlabel('score',f'SCORE: {statbar(player.score,scoregoal)}'))
    dashstatus = 'READY' if player.dashcooldown == 0 else 'RECHARGING'
    lines.append(iconlabel('dash',f'Q DASH: {dashstatus}'))
    lines.append(f'PLAYER: {player.status}')
    return lines


def interface (player, level = None, enemyboss = None, armedbombs = 0, scoregoal = 5, curse = None):
    for line in hudlines(player,level,enemyboss,armedbombs,scoregoal,curse):
        for part in line.splitlines():
            print(colourtext(part.center(BOARDWIDTH),WHITE))


def interface2 (player, enemyboss, armedbombs = 0):
    interface(player,None,enemyboss,armedbombs)
