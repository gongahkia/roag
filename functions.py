import random
import sys
import time
from collections import deque

try:
    import termios
except ImportError:
    termios = None


#GAME SETTINGS

BOARDWIDTH = 55
BOARDHEIGHT = 23
WORLDWIDTH = 160
WORLDHEIGHT = 80
SIDEBARWIDTH = 30
BOSS_ATTACK_WINDUP = 4
DASH_COOLDOWN = 3
EXPLOSION_LIGHT_RADIUS = 1
FLARE_FUSE = 2
FLARE_RADIUS = 1
FLARE_STUN_TURNS = 2

ASCIIICONS = {
    'player_up': '^',
    'player_left': '<',
    'player_down': 'V',
    'player_right': '>',
    'player_hit': '*',
    'player_dead': 'F',
    'bullet': '&',
    'bomb': 'B',
    'flare': 'f',
    'torch': 'T',
    'door': 'D',
    'ammo': 'a',
    'target': 't',
    'necromancer': 'N',
    'necromancer_stunned': 'Z',
    'attack_charge': '+',
    'attack_warning': '!',
    'attack_imminent': '#',
    'explosion': '*',
    'destroyed_wall': 'x',
    'threatcon': '',
    'curse': '',
    'boss': '',
    'health': '',
    'hud_ammo': '',
    'hud_bombs': '',
    'hud_flares': '',
    'score': '',
    'dash': '',
    'shop_health': 'O',
    'shop_ammo': '&',
    'shop_bombs': 'B',
    'shop_flares': 'f'
}

NERDICONS = {
    'player_up': '\uf062',
    'player_left': '\uf060',
    'player_down': '\uf063',
    'player_right': '\uf061',
    'player_hit': '\uf071',
    'player_dead': '\uf119',
    'bullet': '\uf111',
    'bomb': '\uf1e2',
    'flare': '\uf06d',
    'torch': '\uf0eb',
    'door': '\uf08b',
    'ammo': '\uf135',
    'target': '\uf05b',
    'necromancer': '\uf188',
    'necromancer_stunned': '\uf0e7',
    'attack_charge': '\uf0e7',
    'attack_warning': '\uf071',
    'attack_imminent': '\uf06d',
    'explosion': '\uf06d',
    'destroyed_wall': '\uf0c4',
    'threatcon': '\uf0c9',
    'curse': '\uf188',
    'boss': '\uf188',
    'health': '\uf004',
    'hud_ammo': '\uf135',
    'hud_bombs': '\uf1e2',
    'hud_flares': '\uf06d',
    'score': '\uf005',
    'dash': '\uf0e7',
    'shop_health': '\uf004',
    'shop_ammo': '\uf135',
    'shop_bombs': '\uf1e2',
    'shop_flares': '\uf06d'
}


ICONTHEME = 'ascii'
ICONS = dict(ASCIIICONS)


def icon (name):
    return ICONS[name]


def iconlabel (name, text):
    glyph = icon(name)
    if glyph == '':
        return text
    return f'{glyph} {text}'


def configureicontheme (enabled = False):
    global ICONTHEME,ICONS,COLOURS
    ICONTHEME = 'nerd' if enabled else 'ascii'
    ICONS = dict(NERDICONS if enabled else ASCIIICONS)
    COLOURS = {
        icon('player_up'): CYAN,
        icon('player_left'): CYAN,
        icon('player_down'): CYAN,
        icon('player_right'): CYAN,
        icon('player_dead'): RED,
        icon('target'): RED,
        icon('ammo'): YELLOW,
        icon('bullet'): YELLOW,
        icon('bomb'): MAGENTA,
        icon('flare'): YELLOW,
        icon('door'): MAGENTA,
        icon('torch'): YELLOW,
        icon('attack_charge'): YELLOW,
        icon('attack_warning'): RED,
        icon('attack_imminent'): BLUE,
        icon('destroyed_wall'): YELLOW,
        icon('explosion'): YELLOW,
        icon('necromancer'): MAGENTA,
        icon('necromancer_stunned'): YELLOW,
        '#': BLUE,
        '?': GREY,
        '.': GREY
    }
    return ICONTHEME

CURSES = [
    {'id': 'frail_body', 'name': 'FRAIL BODY', 'description': 'BEGIN WITH ONE HEALTH', 'modifiers': {'health': 1}},
    {'id': 'darkness', 'name': 'DARKNESS', 'description': 'REDUCED NATURAL VISION', 'modifiers': {'vision': -3}},
    {'id': 'hunted', 'name': 'HUNTED', 'description': 'ONE EXTRA NECROMANCER', 'modifiers': {'necromancers': 1}},
    {'id': 'relentless', 'name': 'RELENTLESS', 'description': 'TWO EXTRA NECROMANCERS', 'modifiers': {'necromancers': 2}},
    {'id': 'empty_chamber', 'name': 'EMPTY CHAMBER', 'description': 'START WITH LESS AMMO', 'modifiers': {'ammo': -1}},
    {'id': 'spent_bombs', 'name': 'SPENT BOMBS', 'description': 'START WITHOUT BOMBS', 'modifiers': {'bombs': 0}},
    {'id': 'long_hunt', 'name': 'LONG HUNT', 'description': 'MORE TARGETS MUST FALL', 'modifiers': {'score': 2}},
    {'id': 'blackout', 'name': 'BLACKOUT', 'description': 'NO TORCHES OR NATURAL LIGHT', 'modifiers': {'vision': -99, 'torches': -99}},
    {'id': 'guttering_torches', 'name': 'GUTTERING TORCHES', 'description': 'TORCHES BARELY REACH', 'modifiers': {'torchradius': 1}},
    {'id': 'slow_dash', 'name': 'SLOW DASH', 'description': 'DASH RECHARGES SLOWLY', 'modifiers': {'dashcooldown': 6}},
    {'id': 'spent_flares', 'name': 'SPENT FLARES', 'description': 'START WITHOUT FLARES', 'modifiers': {'flares': 0}},
    {'id': 'small_blast', 'name': 'SMALL BLAST', 'description': 'BOMBS HAVE LESS RANGE', 'modifiers': {'bombradius': 1}},
    {'id': 'short_fuse', 'name': 'SHORT FUSE', 'description': 'BOMBS DETONATE QUICKLY', 'modifiers': {'bombfuse': 1}},
    {'id': 'rusted_barrel', 'name': 'RUSTED BARREL', 'description': 'BULLETS FADE EARLY', 'modifiers': {'bulletrange': 3}},
    {'id': 'dry_reload', 'name': 'DRY RELOAD', 'description': 'KILLS RESTORE LESS AMMO', 'modifiers': {'reloadpenalty': 1}}
]
CURSEBYID = {item['id']: item for item in CURSES}


def cursemodifiers (curse):
    if curse is None:
        return {}
    return dict(CURSEBYID[curse]['modifiers'])


class cursebag:
    def __init__ (self):
        self.remaining = []
        self.refill()

    def refill (self):
        self.remaining = list(CURSES)
        random.shuffle(self.remaining)

    def draw (self, amount = 3):
        if len(self.remaining) < amount:
            self.refill()
        items = self.remaining[:amount]
        del self.remaining[:amount]
        return items
RESET = '\033[0m'
CYAN = '\033[96m'
BLUE = '\033[94m'
YELLOW = '\033[93m'
RED = '\033[91m'
MAGENTA = '\033[95m'
GREY = '\033[90m'
GREEN = '\033[92m'
LANDMARKS = {}
WHITE = '\033[97m'

configureicontheme()


def configureterrain (terrain):
    COLOURS['#'] = GREEN if terrain == 'forest' else GREY
    COLOURS['.'] = GREEN if terrain == 'forest' else GREY
    COLOURS['C'] = MAGENTA
    COLOURS['Y'] = GREEN
    COLOURS['^'] = GREY
    COLOURS['O'] = YELLOW
    COLOURS['w'] = RED
    COLOURS['b'] = MAGENTA
    COLOURS['*'] = CYAN
    COLOURS['+'] = GREY
    COLOURS['='] = YELLOW
    COLOURS['@'] = CYAN


#CLASS OBJECTS

class player:

    def __init__ (self, health = 2, location = None):
        self.health = health
        self.location = [WORLDWIDTH // 2,WORLDHEIGHT // 2] if location is None else list(location)
        self.direction = 'w'
        self.model = icon('player_up')
        self.ammo = 1
        self.score = 0
        self.bombs = 0
        self.flares = 1
        self.status = 'alive'
        self.classname = 'ADVENTURER'
        self.boonname = ''
        self.notice = ''
        self.impactframes = 0
        self.eventlog = []
        self.lastnotice = ''
        self.forecasts = []
        self.minimap = []
        self.dashcooldown = 0
        self.dashcooldownbase = DASH_COOLDOWN
        self.bombfuse = 3
        self.bombradius = 2
        self.bulletrange = None
        self.reloadpenalty = 0

    def face (self, face:str):
        if face == 'w':
            self.direction = face
            self.model = icon('player_up')
        elif face == 'a':
            self.direction = face
            self.model = icon('player_left')
        elif face == 's':
            self.direction = face
            self.model = icon('player_down')
        elif face == 'd':
            self.direction = face
            self.model = icon('player_right')

    def movement (self, direction:str):
        if direction == 'w':
            self.location[1] += 1
        elif direction == 'a':
            self.location[0] -= 1
        elif direction == 's':
            self.location[1] -= 1
        elif direction == 'd':
            self.location[0] += 1

    def shoot (self):
        if self.ammo <= 0:
            return False
        self.ammo -= 1
        return True

    def reload (self, amount = 1, cursed = True):
        penalty = self.reloadpenalty if cursed else 0
        self.ammo += max(0,amount - penalty)

    def attacked (self):
        self.health -= 1
        self.impactframes = 2
        self.eventlog.append('You were hit.')
        self.eventlog = self.eventlog[-5:]
        if self.health <= 0:
            self.health = 0
            self.model = icon('player_dead')
            self.status = 'dead'
        return True

    def deconstruct (self):
        self.model=''


class bullet:

    def __init__ (self, playermodel, playerlocation, maxrange = None):
        self.model = icon('bullet')
        self.location = list(playerlocation)
        self.active = False
        self.lightradius = 2
        self.maxrange = maxrange
        self.travelled = 1

        if playermodel == icon('player_up'):
            self.direction = 'up'
            self.location[1] += 1
        elif playermodel == icon('player_down'):
            self.direction = 'down'
            self.location[1] -= 1
        elif playermodel == icon('player_left'):
            self.direction = 'left'
            self.location[0] -= 1
        elif playermodel == icon('player_right'):
            self.direction = 'right'
            self.location[0] += 1

    def movement (self):
        if self.direction == 'up':
            self.location[1] += 1
        elif self.direction == 'down':
            self.location[1] -= 1
        elif self.direction == 'left':
            self.location[0] -= 1
        elif self.direction == 'right':
            self.location[0] += 1


class bomb:

    def __init__ (self, location, fuse = 3, radius = 2):
        self.location = list(location)
        self.model = icon('bomb')
        self.fuse = fuse
        self.radius = radius
        self.lightradius = 3

    def tick (self):
        self.fuse -= 1
        return self.fuse <= 0


class flare:

    def __init__ (self, location, fuse = FLARE_FUSE, radius = FLARE_RADIUS, stunturns = FLARE_STUN_TURNS):
        self.location = list(location)
        self.model = icon('flare')
        self.fuse = fuse
        self.radius = radius
        self.stunturns = stunturns
        self.lightradius = 3

    def tick (self):
        self.fuse -= 1
        return self.fuse <= 0


class torch:

    def __init__ (self, location, lightradius = 4):
        self.location = list(location)
        self.model = icon('torch')
        self.lightradius = lightradius


class door:

    def __init__ (self, location):
        self.location = list(location)
        self.model = icon('door')


class ammo:

    def __init__(self, location = None, model = None):
        self.location = randomlocation() if location is None else list(location)
        self.model = icon('ammo') if model is None else model

    def destroyed (self):
        self.model = ' '


class target:

    def __init__(self, location = None, model = None):
        self.location = randomlocation() if location is None else list(location)
        self.model = icon('target') if model is None else model

    def destroyed (self):
        self.model = ' '


class necromancer:

    def __init__ (self, location = None, model = None, health = 1):
        self.location = randomlocation() if location is None else list(location)
        self.normalmodel = icon('necromancer') if model is None else model
        self.model = self.normalmodel
        self.stunnedmodel = icon('necromancer_stunned')
        self.attackloadmodel = icon('attack_charge')
        self.attacksquaremodel1 = icon('attack_warning')
        self.attacksquaremodel2 = icon('attack_imminent')
        self.health = health
        self.attackcounter = 0
        self.attacklocation = None
        self.attackradius = 1
        self.stunnedturns = 0

    def prepareattack (self, playerlocation):
        self.attacklocation = list(playerlocation)
        self.attackcounter = 1

    def movement (self, location):
        self.location = list(location)

    def damaged (self, amount = 1):
        self.health -= amount

    def stun (self, turns):
        self.stunnedturns = max(self.stunnedturns,turns)
        self.attackcounter = 0
        self.attacklocation = None
        self.model = self.stunnedmodel

    def updatestun (self):
        if self.stunnedturns <= 0:
            return False
        self.stunnedturns -= 1
        if self.stunnedturns == 0:
            self.model = self.normalmodel
        return True

    def destroyed (self):
        self.model = ' '


class boss:

    def __init__ (self, health = 10, model = '~(〃￣ω￣〃)~'):
        self.location = [14,1]
        self.health = health
        self.model = model
        self.attacksqmodel1 = icon('attack_warning')
        self.attacksqmodel2 = icon('attack_imminent')
        self.lines = 'PuNy MoRtAl, yoU dArE cHalLenGE mE?'
        self.attackcounter = 0
        self.attacktype = 'crossfire'
        self.attackname = 'CROSSFIRE'
        self.attackradius = 3
        self.newattack()

    def availableattacks (self):
        attacks = ['crossfire']
        if self.health < 8:
            attacks.append('diagonal')
        if self.health < 4:
            attacks.append('pulse')
        return attacks

    def newattack (self, space = None):
        self.xcoord,self.ycoord = randomlocation(space = space)
        self.xcoord2,self.ycoord2 = randomlocation(space = space)
        self.attacktype = random.choice(self.availableattacks())
        self.attackradius = random.randint(2,4)
        if self.attacktype == 'crossfire':
            self.attackname = 'CROSSFIRE'
        elif self.attacktype == 'diagonal':
            self.attackname = 'DIAGONAL SWEEP'
        elif self.attacktype == 'pulse':
            self.attackname = 'ARCANE PULSE'
        self.updateface()

    def faceoptions (self):
        if self.attackcounter >= BOSS_ATTACK_WINDUP - 1:
            return ['~(!!!)~','~(O_O)~','~(>_<)~']
        if self.attackcounter > 0:
            if self.attacktype == 'crossfire':
                return ['~(>_>)~','~(o_o)~','~(0_0)~']
            if self.attacktype == 'diagonal':
                return ['~(/_/)~','~(<_<)~','~(>_>)~']
            return ['~(._.)~','~(o.o)~','~(O_O)~']
        if self.health < 2:
            return ['~(;_;)~','~(T_T)~','~(x_x)~']
        if self.health < 4:
            return ['~(._.)~','~(u_u)~','~(-_-)~']
        if self.health < 8:
            return ['~(>_>)~','~(o_o)~','~(0_0)~']
        return ['~(^_^)~','~(o_o)~','~(-_-)~']

    def updateface (self):
        self.model = random.choice(self.faceoptions())

    def damaged (self, amount = 1):
        self.health -= amount
        self.updateface()

    def phase2 (self):
        self.lines = 'pReParE FoR yOuR eTerNaL SuFFeRiNg!'
        self.updateface()

    def phase3 (self):
        self.lines = "Hold on, i need go toliet break pls "
        self.updateface()

    def phase4 (self):
        self.lines = 'Bro give chance pls I first time :('
        self.updateface()

    def destroyed (self):
        self.lines = 'NOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO!'
        self.model = '   (◕﹏◕✿)   '

    def attack (self, space = None):
        self.attackcounter += 1
        if self.attackcounter > BOSS_ATTACK_WINDUP:
            self.attackcounter = 0
            self.newattack(space)
            return
        self.updateface()


#GENERAL FUNCTIONS

def randomlocation (occupied = None, space = None):
    if occupied is None:
        occupied = set()

    if space is not None:
        locations = list(space.difference(occupied))
        if len(locations) == 0:
            raise ValueError('No open locations available.')
        location = random.choice(locations)
        return [location[0],location[1]]

    while True:
        location = [random.randint(1,BOARDWIDTH - 1),random.randint(1,BOARDHEIGHT - 1)]
        if tuple(location) not in occupied:
            return location


def carveroom (space, xcoord, ycoord, width, height):
    for x in range(xcoord,xcoord + width):
        for y in range(ycoord,ycoord + height):
            if x > 0 and x < WORLDWIDTH - 1 and y > 0 and y < WORLDHEIGHT - 1:
                space.add((x,y))


def carvecorridor (space, start, finish):
    xcoord,ycoord = start
    finishx,finishy = finish
    if random.choice([True,False]):
        while xcoord != finishx:
            space.add((xcoord,ycoord))
            xcoord += 1 if finishx > xcoord else -1
        while ycoord != finishy:
            space.add((xcoord,ycoord))
            ycoord += 1 if finishy > ycoord else -1
    else:
        while ycoord != finishy:
            space.add((xcoord,ycoord))
            ycoord += 1 if finishy > ycoord else -1
        while xcoord != finishx:
            space.add((xcoord,ycoord))
            xcoord += 1 if finishx > xcoord else -1
    space.add((finishx,finishy))


def wavefunctioncollapse (width, height, tiles, compatibility):
    """Collapse a small motif grid, preferring cells with fewest possibilities."""
    domains = {(x,y): set(tiles) for x in range(width) for y in range(height)}
    while True:
        unresolved = [coordinate for coordinate, options in domains.items() if len(options) > 1]
        if not unresolved:
            return {coordinate: next(iter(options)) for coordinate, options in domains.items()}
        coordinate = min(unresolved,key = lambda item: len(domains[item]))
        domains[coordinate] = {random.choice(tuple(domains[coordinate]))}
        changed = True
        while changed:
            changed = False
            for (x,y), options in domains.items():
                allowed = set(tiles)
                for adjacent in [(x + 1,y),(x - 1,y),(x,y + 1),(x,y - 1)]:
                    if adjacent in domains:
                        neighbour = domains[adjacent]
                        allowed &= {tile for tile in tiles if any(other in compatibility[tile] for other in neighbour)}
                reduced = options.intersection(allowed)
                if not reduced:
                    domains[(x,y)] = set(tiles)
                elif reduced != options:
                    domains[(x,y)] = reduced
                    changed = True


def wfcbiomelayer (space, terrain):
    tiles = {
        'forest': ['grove','ridge','clearing','pond'],
        'cave': ['chamber','narrow','crystal','sinkhole'],
        'dungeon': ['room','hall','pillar','cell']
    }[terrain]
    compatibility = {tile: set(tiles) for tile in tiles}
    compatibility[tiles[-1]] = set(tiles[:-1]) | {tiles[-1]}
    motifs = wavefunctioncollapse(WORLDWIDTH // 12,WORLDHEIGHT // 10,tiles,compatibility)
    for (cellx,celly), motif in motifs.items():
        x0,y0 = cellx * 12,celly * 10
        coordinates = [(x,y) for x in range(x0 + 1,min(x0 + 11,WORLDWIDTH - 1)) for y in range(y0 + 1,min(y0 + 9,WORLDHEIGHT - 1))]
        if motif in ['pond','sinkhole','cell']:
            for coordinate in random.sample(coordinates,random.randint(12,28)):
                space.discard(coordinate)
        elif motif in ['clearing','chamber','room']:
            for coordinate in coordinates:
                if random.random() < 0.18:
                    space.add(coordinate)
        glyph = {'grove':'Y','ridge':'^','clearing':'Y','pond':'O','chamber':'^','narrow':'^','crystal':'*','sinkhole':'O','room':'+','hall':'+','pillar':'=','cell':'O'}[motif]
        for coordinate in random.sample(coordinates,random.randint(1,3)):
            if coordinate not in space:
                LANDMARKS[coordinate] = glyph


def generatespace (start, required = None, roomcount = (3,5), roomwidth = (5,9), roomheight = (3,6), startroomsize = (11,7), arena = None, terrain = 'cave'):
    global LANDMARKS
    if arena is not None:
        space = set()
        carveroom(space,*arena)
        return space
    if terrain == 'dungeon':
        space = set()
        rooms = []
        for number in range(14):
            width,height = random.randint(5,11),random.randint(4,8)
            xcoord,ycoord = random.randint(1,WORLDWIDTH - width - 2),random.randint(1,WORLDHEIGHT - height - 2)
            carveroom(space,xcoord,ycoord,width,height)
            centre = (xcoord + width // 2,ycoord + height // 2)
            if rooms:
                carvecorridor(space,rooms[-1],centre)
            rooms.append(centre)
        carveroom(space,start[0] - 3,start[1] - 3,7,7)
        carvecorridor(space,tuple(start),rooms[0])
        LANDMARKS = {}
        walls = [(x,y) for x in range(1,WORLDWIDTH - 1) for y in range(1,WORLDHEIGHT - 1) if (x,y) not in space]
        for glyph,count in [('+',80),('=',20),('O',8)]:
            for coordinate in random.sample(walls,min(count,len(walls))):
                LANDMARKS[coordinate] = glyph
        wfcbiomelayer(space,terrain)
        carveroom(space,start[0] - 3,start[1] - 3,7,7)
        carvecorridor(space,tuple(start),rooms[0])
        return space
    for attempt in range(12):
        walls = {(x,y) for x in range(WORLDWIDTH) for y in range(WORLDHEIGHT)
                 if x in [0,WORLDWIDTH - 1] or y in [0,WORLDHEIGHT - 1] or random.random() < (0.25 if terrain == 'forest' else 0.43)}
        for step in range(2 if terrain == 'forest' else 5):
            walls = {(x,y) for x in range(WORLDWIDTH) for y in range(WORLDHEIGHT) if x in [0,WORLDWIDTH - 1] or y in [0,WORLDHEIGHT - 1] or sum((x + dx,y + dy) in walls for dx in [-1,0,1] for dy in [-1,0,1] if (dx,dy) != (0,0)) >= 5}
        space = {(x,y) for x in range(WORLDWIDTH) for y in range(WORLDHEIGHT) if (x,y) not in walls}
        carveroom(space,start[0] - 3,start[1] - 3,7,7)
        reachable = set(pathfind(tuple(start),tuple(next(iter(space))),space))
        if len(space) > 1000:
            LANDMARKS = {}
            blocked = [(x,y) for x in range(1,WORLDWIDTH - 1) for y in range(1,WORLDHEIGHT - 1) if (x,y) not in space]
            landmarks = [('Y',110),('^',35),('O',12)] if terrain == 'forest' else [('^',90),('*',45),('O',16)]
            for glyph,count in landmarks:
                for coordinate in random.sample(blocked,min(count,len(blocked))):
                    LANDMARKS[coordinate] = glyph
            wfcbiomelayer(space,terrain)
            carveroom(space,start[0] - 3,start[1] - 3,7,7)
            return space
    return space


def pathfind (start, finish, space, blocked = None):
    if blocked is None:
        blocked = set()
    blocked = set(blocked)
    blocked.discard(start)
    blocked.discard(finish)
    queue = deque([start])
    previous = {start: None}

    while len(queue) > 0:
        coordinate = queue.popleft()
        if coordinate == finish:
            path = []
            while coordinate is not None:
                path.append(coordinate)
                coordinate = previous[coordinate]
            return list(reversed(path))
        xcoord,ycoord = coordinate
        for adjacent in [(xcoord,ycoord + 1),(xcoord - 1,ycoord),(xcoord,ycoord - 1),(xcoord + 1,ycoord)]:
            if adjacent in space and adjacent not in blocked and adjacent not in previous:
                previous[adjacent] = coordinate
                queue.append(adjacent)
    return []


def clampplayer (player):
    if player.location[0] < 0:
        player.location[0] = 0
    if player.location[0] >= WORLDWIDTH:
        player.location[0] = WORLDWIDTH - 1
    if player.location[1] < 0:
        player.location[1] = 0
    if player.location[1] >= WORLDHEIGHT:
        player.location[1] = WORLDHEIGHT - 1


def moveplayer (player, user, space = None):
    previous = list(player.location)
    if player.model == icon('player_hit'):
        if user in ['w','a','s','d']:
            player.face(user)
            player.movement(user)
        elif user == 'e':
            player.notice = "~You are immobilized. Can't shoot!~"
        elif user == 'b':
            player.notice = "~You are immobilized. Can't plant bombs!~"
        clampplayer(player)
        if space is not None and tuple(player.location) not in space:
            player.location = previous
            player.notice = '~A wall blocks your path.~'
        return

    if player.direction == 'w' and user == 'w':
        player.movement(user)
    elif player.direction == 'a' and user == 'a':
        player.movement(user)
    elif player.direction == 's' and user == 's':
        player.movement(user)
    elif player.direction == 'd' and user == 'd':
        player.movement(user)
    player.face(user)
    clampplayer(player)
    if space is not None and tuple(player.location) not in space:
        player.location = previous
        if user in ['w','a','s','d']:
            player.notice = '~A wall blocks your path.~'


def dashplayer (player, space = None):
    if player.dashcooldown > 0:
        player.notice = '~Dash is recharging.~'
        return
    direction = player.direction
    if direction is None:
        player.notice = '~Choose a direction before dashing.~'
        return
    start = list(player.location)
    for step in range(2):
        previous = list(player.location)
        player.movement(direction)
        clampplayer(player)
        if space is not None and tuple(player.location) not in space:
            player.location = previous
            break
    if player.location == start:
        player.notice = '~A wall blocks your dash.~'
        return
    player.dashcooldown = player.dashcooldownbase
    player.notice = '~You dashed forward.~'


def tickplayerabilities (player):
    if player.dashcooldown > 0:
        player.dashcooldown -= 1


def attackcoordinates (enemy):
    attackdict = {}
    if enemy.attackcounter == 0:
        return attackdict

    if isinstance(enemy, necromancer):
        model = enemy.attackloadmodel
        if enemy.attackcounter == 2 or enemy.attackcounter == 3:
            model = enemy.attacksquaremodel1
        if enemy.attackcounter == 4:
            model = enemy.attacksquaremodel2
        if enemy.attacklocation is None:
            return attackdict
        for x in range(enemy.attacklocation[0] - enemy.attackradius,enemy.attacklocation[0] + enemy.attackradius + 1):
            for y in range(enemy.attacklocation[1] - enemy.attackradius,enemy.attacklocation[1] + enemy.attackradius + 1):
                if x >= 0 and x < BOARDWIDTH and y >= 0 and y < BOARDHEIGHT:
                    attackdict[(x,y)] = model

    if isinstance(enemy, boss):
        model = enemy.attacksqmodel1
        if enemy.attackcounter >= BOSS_ATTACK_WINDUP - 1:
            model = enemy.attacksqmodel2
        if enemy.attacktype == 'crossfire':
            for x in range(BOARDWIDTH):
                attackdict[(x,enemy.ycoord)] = model
                attackdict[(x,enemy.ycoord2)] = model
            for y in range(BOARDHEIGHT):
                attackdict[(enemy.xcoord,y)] = model
                attackdict[(enemy.xcoord2,y)] = model
        elif enemy.attacktype == 'diagonal':
            for y in range(BOARDHEIGHT):
                firstx = enemy.xcoord + (y - enemy.ycoord)
                secondx = enemy.xcoord - (y - enemy.ycoord)
                if firstx >= 0 and firstx < BOARDWIDTH:
                    attackdict[(firstx,y)] = model
                if secondx >= 0 and secondx < BOARDWIDTH:
                    attackdict[(secondx,y)] = model
        elif enemy.attacktype == 'pulse':
            for x in range(enemy.xcoord - enemy.attackradius,enemy.xcoord + enemy.attackradius + 1):
                for y in range(enemy.ycoord - enemy.attackradius,enemy.ycoord + enemy.attackradius + 1):
                    if x >= 0 and x < BOARDWIDTH and y >= 0 and y < BOARDHEIGHT:
                        if abs(x - enemy.xcoord) + abs(y - enemy.ycoord) == enemy.attackradius:
                            attackdict[(x,y)] = model
    return attackdict


def attackplayer (player, enemies, space = None):
    wolves = [enemy for enemy in enemies if isinstance(enemy,necromancer) and enemy.normalmodel == 'w']
    packleader = min(wolves,key = lambda enemy: abs(enemy.location[0] - player.location[0]) + abs(enemy.location[1] - player.location[1])) if wolves else None
    for enemy in enemies:
        if isinstance(enemy, necromancer):
            if enemy.updatestun():
                continue
            if enemy.attackcounter == 0:
                blocked = set()
                for other in enemies:
                    if other is not enemy and isinstance(other, necromancer):
                        blocked.add(tuple(other.location))
                huntlocation = player.location
                if enemy.normalmodel == 'w' and enemy is not packleader and abs(enemy.location[0] - packleader.location[0]) + abs(enemy.location[1] - packleader.location[1]) <= 10:
                    huntlocation = packleader.location
                path = pathfind(tuple(enemy.location),tuple(huntlocation),space,blocked)
                if enemy.normalmodel == 'b' and len(path) <= 2:
                    player.attacked()
                    enemy.destroyed()
                    enemies.remove(enemy)
                    player.notice = '~A bomber detonated beside you.~'
                    player.eventlog.append('A bomber exploded nearby!')
                    player.eventlog = player.eventlog[-5:]
                elif enemy.normalmodel == 'w' and abs(enemy.location[0] - player.location[0]) + abs(enemy.location[1] - player.location[1]) == 1:
                    player.attacked()
                    player.notice = '~A forest wolf tore into you.~'
                elif enemy.normalmodel == 'w' and len(path) > 2:
                    enemy.movement(path[1])
                elif len(path) > 0 and len(path) - 1 <= 4:
                    enemy.prepareattack(player.location)
                elif len(path) > 1:
                    enemy.movement(path[1])
            elif enemy.attackcounter == 1:
                enemy.attackcounter = 2
            elif enemy.attackcounter == 2:
                enemy.attackcounter = 3
            elif enemy.attackcounter == 3:
                enemy.attackcounter = 4
                if tuple(player.location) in attackcoordinates(enemy):
                    if player.attacked():
                        if player.health > 0:
                            player.model = icon('player_hit')
                        player.notice = '~Necromancer spell struck you.~'
            else:
                enemy.attackcounter = 0
                enemy.attacklocation = None
        else:
            enemy.attack(space)
            if enemy.attackcounter == BOSS_ATTACK_WINDUP:
                if tuple(player.location) in attackcoordinates(enemy):
                    if player.attacked():
                        if player.health > 0:
                            player.model = icon('player_hit')
                        player.notice = '~Boss attack struck you.~'


def bombcoordinates (item, space = None):
    blast = set()
    queue = deque([(tuple(item.location),0)])
    while len(queue) > 0:
        coordinate,distance = queue.popleft()
        if coordinate in blast or distance > item.radius:
            continue
        if coordinate[0] < 0 or coordinate[0] >= BOARDWIDTH or coordinate[1] < 0 or coordinate[1] >= BOARDHEIGHT:
            continue
        if space is not None and coordinate not in space:
            continue
        blast.add(coordinate)
        xcoord,ycoord = coordinate
        queue.append(((xcoord,ycoord + 1),distance + 1))
        queue.append(((xcoord - 1,ycoord),distance + 1))
        queue.append(((xcoord,ycoord - 1),distance + 1))
        queue.append(((xcoord + 1,ycoord),distance + 1))
    return blast


def destroyterrain (item, space):
    destroyed = set()
    for x in range(item.location[0] - item.radius,item.location[0] + item.radius + 1):
        for y in range(item.location[1] - item.radius,item.location[1] + item.radius + 1):
            coordinate = (x,y)
            if abs(x - item.location[0]) + abs(y - item.location[1]) <= item.radius:
                if x > 0 and x < BOARDWIDTH - 1 and y > 0 and y < BOARDHEIGHT - 1:
                    if coordinate not in space:
                        space.add(coordinate)
                        destroyed.add(coordinate)
    return destroyed


def bosshitbox (enemy):
    hitbox = set()
    for x in range(16,25):
        hitbox.add((x,1))
    return hitbox


def updateboss (enemy):
    if enemy.health < 2:
        enemy.phase4()
    elif enemy.health < 4:
        enemy.phase3()
    elif enemy.health < 8:
        enemy.phase2()


#ENTITY LOGGING AND PRINTING

def addentity (entitydict, location, model):
    if len(model) == 1:
        coordinate = tuple(location)
        if coordinate[0] >= 0 and coordinate[0] < WORLDWIDTH and coordinate[1] >= 0 and coordinate[1] < WORLDHEIGHT:
            entitydict[coordinate] = model
        return

    for index,char in enumerate(model):
        coordinate = (location[0] + index,location[1])
        if coordinate[0] >= 0 and coordinate[0] < WORLDWIDTH and coordinate[1] >= 0 and coordinate[1] < WORLDHEIGHT:
            entitydict[coordinate] = char


def updatedict (player, targets = None, necromancers = None, bullets = None, bombs = None, ammo = None, enemyboss = None, explosions = None, torches = None, exitdoor = None, flares = None):
    entitydict = {}
    targets = [] if targets is None else targets
    necromancers = [] if necromancers is None else necromancers
    bullets = [] if bullets is None else bullets
    bombs = [] if bombs is None else bombs
    flares = [] if flares is None else flares
    explosions = set() if explosions is None else explosions
    torches = [] if torches is None else torches

    for enemy in necromancers:
        entitydict.update(attackcoordinates(enemy))
    if enemyboss is not None:
        entitydict.update(attackcoordinates(enemyboss))
    for coordinate in explosions:
        entitydict[coordinate] = icon('explosion')
    if ammo is not None and ammo.model != ' ':
        addentity(entitydict,ammo.location,ammo.model)
    for item in torches:
        addentity(entitydict,item.location,item.model)
    if exitdoor is not None:
        addentity(entitydict,exitdoor.location,exitdoor.model)
    for item in targets:
        if item.model != ' ':
            addentity(entitydict,item.location,item.model)
    for enemy in necromancers:
        if enemy.model != ' ':
            addentity(entitydict,enemy.location,enemy.model)
    for item in bombs:
        addentity(entitydict,item.location,item.model)
    for item in flares:
        addentity(entitydict,item.location,item.model)
    if enemyboss is not None:
        addentity(entitydict,enemyboss.location,enemyboss.model)
    for item in bullets:
        addentity(entitydict,item.location,item.model)
    addentity(entitydict,player.location,player.model)
    return entitydict


def visiblecoordinates (origin, space, radius = 5):
    visible = set()
    location = origin.location if hasattr(origin,'location') else origin
    queue = deque([(tuple(location),0)])
    while len(queue) > 0:
        coordinate,distance = queue.popleft()
        if coordinate in visible or distance > radius:
            continue
        visible.add(coordinate)
        if coordinate not in space:
            continue
        xcoord,ycoord = coordinate
        for adjacent in [(xcoord,ycoord + 1),(xcoord - 1,ycoord),(xcoord,ycoord - 1),(xcoord + 1,ycoord)]:
            if adjacent[0] >= 0 and adjacent[0] < WORLDWIDTH and adjacent[1] >= 0 and adjacent[1] < WORLDHEIGHT:
                if adjacent not in visible:
                    queue.append((adjacent,distance + 1))
    return visible


def lightcoordinates (player, space, vision = 5, bullets = None, bombs = None, torches = None, explosions = None, flares = None):
    visible = visiblecoordinates(player,space,vision)
    sources = []
    sources.extend([] if bullets is None else bullets)
    sources.extend([] if bombs is None else bombs)
    sources.extend([] if flares is None else flares)
    sources.extend([] if torches is None else torches)
    for source in sources:
        visible.update(visiblecoordinates(source,space,source.lightradius))
    for coordinate in [] if explosions is None else explosions:
        visible.update(visiblecoordinates(coordinate,space,EXPLOSION_LIGHT_RADIUS))
    return visible


def fogdict (entitydict, space, visible, explored, destroyedwalls = None):
    foggeddict = {}
    destroyedwalls = set() if destroyedwalls is None else destroyedwalls
    for x in range(WORLDWIDTH):
        for y in range(WORLDHEIGHT):
            coordinate = (x,y)
            if coordinate in visible:
                if coordinate in entitydict:
                    foggeddict[coordinate] = entitydict[coordinate]
                elif coordinate in destroyedwalls:
                    foggeddict[coordinate] = icon('destroyed_wall')
                elif coordinate in LANDMARKS:
                    foggeddict[coordinate] = LANDMARKS[coordinate]
                elif coordinate not in space:
                    foggeddict[coordinate] = '#'
            elif coordinate in explored:
                if coordinate in destroyedwalls:
                    foggeddict[coordinate] = icon('destroyed_wall')
                elif coordinate in space:
                    foggeddict[coordinate] = '.'
                else:
                    foggeddict[coordinate] = '#'
            else:
                foggeddict[coordinate] = '?'
    return foggeddict


def mapdict (entitydict, space, destroyedwalls = None):
    mapentities = {}
    destroyedwalls = set() if destroyedwalls is None else destroyedwalls
    for x in range(WORLDWIDTH):
        for y in range(WORLDHEIGHT):
            coordinate = (x,y)
            if coordinate in entitydict:
                mapentities[coordinate] = entitydict[coordinate]
            elif coordinate in destroyedwalls:
                mapentities[coordinate] = icon('destroyed_wall')
            elif coordinate in LANDMARKS:
                mapentities[coordinate] = LANDMARKS[coordinate]
            elif coordinate not in space:
                mapentities[coordinate] = '#'
    return mapentities


def clearscreen ():
    print ('\033[2J\033[H',end = '')


def colourtext (text, colour):
    return f'{colour}{text}{RESET}'


def colourboardline (line):
    colouredline = ''
    for glyph in line:
        if glyph in COLOURS:
            colouredline += colourtext(glyph,COLOURS[glyph])
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
    clearscreen()
    print (colourtext('XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX',CYAN))
    for line in lines:
        line = line[:BOARDWIDTH].ljust(BOARDWIDTH)
        if gameboard:
            line = colourboardline(line)
        border = colourtext('X',CYAN)
        print (f'{border}{line}{border}')
    print (colourtext('XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX',CYAN))


def boardlines (entitydict):
    lines = []
    for y in reversed(range(BOARDHEIGHT)):
        string = ''
        for x in range(BOARDWIDTH):
            string += entitydict.get((x,y),' ')
        lines.append(string)
    return lines


def gameoverlines ():
    lines = ['' for row in range(BOARDHEIGHT)]
    lines[BOARDHEIGHT // 2 - 1] = '~GAME OVER~'.center(BOARDWIDTH)
    lines[BOARDHEIGHT // 2 + 1] = 'THANKS FOR PLAYING'.center(BOARDWIDTH)
    return lines


def printdict (entitydict):
    lines = boardlines(entitydict)
    printscreen(lines,True)


def centersidebar (lines):
    sidebar = []
    for line in lines:
        sidebar.extend(line.split('\n'))
    padding = max((BOARDHEIGHT - len(sidebar)) // 2,0)
    return [''] * padding + sidebar


def printgameframe (entitydict, sidebar, gameover = False):
    sidebar = centersidebar(sidebar)
    lines = gameoverlines() if gameover else boardlines(entitydict)
    clearscreen()
    border = colourtext('X',CYAN)
    topborder = colourtext('XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX',CYAN)
    print(topborder)
    for index,line in enumerate(lines):
        line = line[:BOARDWIDTH].ljust(BOARDWIDTH)
        side = sidebar[index] if index < len(sidebar) else ''
        boardline = colourtext(line,WHITE) if gameover else colourboardline(line)
        print(f'{border}{boardline}{border}  {colourtext(side.center(SIDEBARWIDTH),WHITE)}')
    print(topborder)


#HUD

def statbar (value, maximum):
    filled = min(max(value,0),maximum)
    return f"[{'X' * filled}{'-' * (maximum - filled)}]"


def hudlines (player, level = None, enemyboss = None, armedbombs = 0, scoregoal = 5, curse = None, armedflares = 0):
    lines = []
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
    lines.append('------------------------------')
    lines.extend(['EVENT LOG:'] + player.eventlog[-4:] + [''] * max(0,4 - len(player.eventlog)))
    if player.minimap:
        lines.extend(['------------------------------', 'MINIMAP:'] + [f'MAP:{line}' for line in player.minimap])
    return lines


def interface (player, level = None, enemyboss = None, armedbombs = 0, scoregoal = 5, curse = None):
    for line in hudlines(player,level,enemyboss,armedbombs,scoregoal,curse):
        for part in line.splitlines():
            print(colourtext(part.center(BOARDWIDTH),WHITE))


def interface2 (player, enemyboss, armedbombs = 0):
    interface(player,None,enemyboss,armedbombs)


#SHOP

class shop:
    def __init__ (self):
        self.pointer = 0
        self.items = ['HEALTH','AMMO','BOMBS','FLARES']

    def move (self, direction):
        if direction == 'w':
            self.pointer -= 1
        if direction == 's':
            self.pointer += 1
        if self.pointer < 0:
            self.pointer = 0
        if self.pointer >= len(self.items):
            self.pointer = len(self.items) - 1

    def itemvalue (self, player, item):
        if item == 'HEALTH':
            return player.health
        if item == 'AMMO':
            return player.ammo
        if item == 'BOMBS':
            return player.bombs
        return player.flares

    def changeitem (self, player, item, amount):
        if item == 'HEALTH':
            player.health += amount
        elif item == 'AMMO':
            player.ammo += amount
        elif item == 'BOMBS':
            player.bombs += amount
        else:
            player.flares += amount

    def buy (self, player, points):
        item = self.items[self.pointer]
        if points <= 0:
            print ('No more points left to spend. Sell [V] items to buy [B] others.')
            time.sleep(1.5)
            return points
        if self.itemvalue(player,item) >= 5:
            print (f'Max {item.lower()} of 5 reached.')
            time.sleep(0.75)
            return points
        self.changeitem(player,item,1)
        return points - 1

    def sell (self, player, points):
        item = self.items[self.pointer]
        minimum = 0 if item in ['BOMBS','FLARES'] else 1
        if self.itemvalue(player,item) <= minimum:
            if item in ['BOMBS','FLARES']:
                print (f'No {item.lower()} left to sell!')
            else:
                print (f'Base {item.lower()} {minimum}, cannot be sold!')
            time.sleep(0.75)
            return points
        self.changeitem(player,item,-1)
        return points + 1

    def screenlines (self, player, points):
        lines = ['','','                ~WELCOME~','               TO THE SHOP','','']
        for index,item in enumerate(self.items):
            pointer = ' --> ' if self.pointer == index else '     '
            value = self.itemvalue(player,item)
            markers = ''
            marker = {
                'HEALTH': icon('shop_health'),
                'AMMO': icon('shop_ammo'),
                'BOMBS': icon('shop_bombs'),
                'FLARES': icon('shop_flares')
            }[item]
            for number in range(value):
                markers += f'{marker} '
            lines.append(f'     {pointer} {item:<6} |{markers:<10}|')
            lines.append('')
        lines.append(f'           POINTS LEFT: {points}')
        return lines

    def printscreen (self, player, points):
        printscreen(self.screenlines(player,points))


def runshop (num, player):
    s = shop ()

    while True:
        s.printscreen(player,num)
        user = promptinput('[W/S/B/V/E]: ')
        if user == 'w' or user == 's':
            s.move(user)
        elif user == 'b':
            num = s.buy(player,num)
        elif user == 'v':
            num = s.sell(player,num)
        elif user == 'e':
            print ('Would you like to continue to the next stage?'.center(BOARDWIDTH))
            exitinput = promptinput('[Y/N]: ')
            if exitinput == 'y':
                return player
            if exitinput == 'n':
                print ('Okay. Continue browsing.')
                time.sleep(1.5)


class curseshop:
    def __init__ (self, items):
        self.pointer = 0
        self.items = items

    def move (self, direction):
        if direction == 'w':
            self.pointer -= 1
        if direction == 's':
            self.pointer += 1
        if self.pointer < 0:
            self.pointer = 0
        if self.pointer >= len(self.items):
            self.pointer = len(self.items) - 1

    def selected (self):
        return self.items[self.pointer]['id']

    def screenlines (self):
        lines = [iconlabel('curse','~CURSE SHOP~'),'CHOOSE A BURDEN TO DESCEND','']
        for index,item in enumerate(self.items):
            label = f'[ {item["name"]} ]' if self.pointer == index else item['name']
            lines.append(label)
            lines.append(item['description'])
            lines.append('')
        lines.append('[E] ACCEPTS YOUR CURSE')
        padding = (BOARDHEIGHT - len(lines)) // 2
        blank = ' ' * BOARDWIDTH
        return [blank] * padding + [line.center(BOARDWIDTH) for line in lines] + [blank] * padding

    def printscreen (self):
        printscreen(self.screenlines())


def runcurseshop (bag = None):
    bag = cursebag() if bag is None else bag
    s = curseshop(bag.draw())
    while True:
        s.printscreen()
        user = promptinput('[W/S/E]: ')
        if user == 'w' or user == 's':
            s.move(user)
        elif user == 'e':
            return s.selected()


# Compatibility exports. New UI and shop code lives in dedicated modules.
from ui import (boardlines, centersidebar, clearscreen, colourboardline, colourtext,
                gameoverlines, hudlines, interface, interface2, printdict,
                printgameframe, printscreen, promptinput, readkey, statbar)
from shops import curseshop, runcurseshop, runshop, shop
