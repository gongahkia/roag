import random
import time

from titlescreen import titlescreen
from functions import BOARDHEIGHT, BOARDWIDTH, WORLDWIDTH, WORLDHEIGHT, player, bullet, bomb, flare, torch, door, ammo, target, necromancer, boss, randomlocation, generatespace, configureterrain, moveplayer, dashplayer, tickplayerabilities, attackplayer, bombcoordinates, destroyterrain, bosshitbox, updateboss, updatedict, visiblecoordinates, lightcoordinates, fogdict, mapdict, printgameframe, hudlines, promptinput, printscreen, cursemodifiers, cursebag, runcurseshop, runshop, icon


#GAME SETTINGS

STAGES = [
    {'level': 0, 'terrain': 'forest', 'targets': 4, 'necromancers': 6, 'score': 5, 'ammo': 2, 'vision': 6, 'torches': 3, 'wilds': True},
    {'level': 1, 'terrain': 'cave', 'targets': 3, 'necromancers': 0, 'score': 4, 'ammo': 1, 'vision': 5, 'torches': 3},
    {'level': 2, 'terrain': 'dungeon', 'targets': 1, 'necromancers': 2, 'score': 5, 'ammo': 2, 'vision': 4, 'torches': 3, 'cultists': True}
]

PLAYER_CLASSES = [
    {
        'id': 'vanguard',
        'name': 'VANGUARD',
        'description': 'TOUGH AND WELL-ARMED',
        'modifiers': {'health': 2, 'bombs': 1, 'flares': -1}
    },
    {
        'id': 'gunslinger',
        'name': 'GUNSLINGER',
        'description': 'EXTRA AMMO, LESS HEALTH',
        'modifiers': {'health': -1, 'ammo': 3}
    },
    {
        'id': 'scout',
        'name': 'SCOUT',
        'description': 'BETTER SIGHT, FLARES, AND DASH',
        'modifiers': {'vision': 2, 'flares': 1, 'dashcooldown': -1}
    }
]
PLAYER_CLASS_BY_ID = {item['id']: item for item in PLAYER_CLASSES}

BOONS = [
    {
        'id': 'iron_heart',
        'name': 'IRON HEART',
        'description': 'BEGIN EACH DESCENT WITH MORE HEALTH',
        'modifiers': {'health': 1}
    },
    {
        'id': 'full_quiver',
        'name': 'FULL QUIVER',
        'description': 'BEGIN EACH DESCENT WITH EXTRA AMMO',
        'modifiers': {'ammo': 2}
    },
    {
        'id': 'demolition_kit',
        'name': 'DEMOLITION KIT',
        'description': 'BEGIN EACH DESCENT WITH AN EXTRA BOMB',
        'modifiers': {'bombs': 1}
    },
    {
        'id': 'flare_satchel',
        'name': 'FLARE SATCHEL',
        'description': 'BEGIN EACH DESCENT WITH TWO EXTRA FLARES',
        'modifiers': {'flares': 2}
    },
    {
        'id': 'eagle_eye',
        'name': 'EAGLE EYE',
        'description': 'SEE FURTHER THROUGH THE DARKNESS',
        'modifiers': {'vision': 2}
    },
    {
        'id': 'windwalker',
        'name': 'WINDWALKER',
        'description': 'YOUR DASH RECHARGES MORE QUICKLY',
        'modifiers': {'dashcooldown': -1}
    }
]
BOON_BY_ID = {item['id']: item for item in BOONS}


def chooseclass ():
    pointer = 0
    while True:
        lines = ['', '', 'CHOOSE YOUR CLASS', '']
        for index, item in enumerate(PLAYER_CLASSES):
            marker = '[ ' if index == pointer else '  '
            ending = ' ]' if index == pointer else '  '
            lines.append(f'{marker}{item["name"]}{ending}')
            lines.append(item['description'])
            lines.append('')
        lines.extend(['[W/S] SELECT     [E] CONFIRM', ''])
        printscreen([line.center(BOARDWIDTH) for line in lines])
        user = promptinput('[W/S/E]: ')
        if user == 'w':
            pointer = max(0,pointer - 1)
        elif user == 's':
            pointer = min(len(PLAYER_CLASSES) - 1,pointer + 1)
        elif user == 'e':
            return PLAYER_CLASSES[pointer]['id']


def chooseboon ():
    options = random.sample(BOONS,3)
    pointer = 0
    while True:
        lines = ['', '', 'CHOOSE A BOON', '']
        for index, item in enumerate(options):
            marker = '[ ' if index == pointer else '  '
            ending = ' ]' if index == pointer else '  '
            lines.append(f'{marker}{item["name"]}{ending}')
            lines.append(item['description'])
            lines.append('')
        lines.extend(['[W/S] SELECT     [E] ACCEPT BOON', ''])
        printscreen([line.center(BOARDWIDTH) for line in lines])
        user = promptinput('[W/S/E]: ')
        if user == 'w':
            pointer = max(0,pointer - 1)
        elif user == 's':
            pointer = min(len(options) - 1,pointer + 1)
        elif user == 'e':
            return options[pointer]['id']


def continuestage (level, bag = None):
    return runcurseshop(bag)


def occupiedcoordinates (play, targets, necromancers, bullets, bombs, ammopickup = None, blocked = None, torches = None, flares = None):
    occupied = {tuple(play.location)}
    flares = [] if flares is None else flares
    for item in targets + necromancers + bullets + bombs + flares:
        occupied.add(tuple(item.location))
    if ammopickup is not None:
        occupied.add(tuple(ammopickup.location))
    if blocked is not None:
        occupied.update(blocked)
    if torches is not None:
        for item in torches:
            occupied.add(tuple(item.location))
    return occupied


def openlocation (play, targets, necromancers, bullets, bombs, ammopickup = None, blocked = None, space = None, torches = None, flares = None):
    occupied = occupiedcoordinates(play,targets,necromancers,bullets,bombs,ammopickup,blocked,torches,flares)
    return randomlocation(occupied,space)


def targetspace (settings, play, space):
    if settings['level'] == 0:
        return space.intersection(visiblecoordinates(play,space,settings['vision']))
    return space


def createstageentities (settings, play, space):
    targets = []
    necromancers = []
    bullets = []
    bombs = []
    flares = []
    torches = []

    for number in range(settings['torches']):
        location = openlocation(play,targets,necromancers,bullets,bombs,space = space,torches = torches,flares = flares)
        torches.append(torch(location,settings.get('torchradius',4)))
    for number in range(settings['targets']):
        location = openlocation(play,targets,necromancers,bullets,bombs,space = targetspace(settings,play,space),torches = torches,flares = flares)
        targets.append(target(location))
    for number in range(settings['necromancers']):
        location = openlocation(play,targets,necromancers,bullets,bombs,space = space,torches = torches,flares = flares)
        model = 'b' if settings.get('wilds') and number % 3 == 0 else ('w' if settings.get('wilds') else ('C' if settings.get('cultists') else None))
        necromancers.append(necromancer(location,model))
    location = openlocation(play,targets,necromancers,bullets,bombs,space = space,torches = torches,flares = flares)
    ammopickup = ammo(location)
    return targets,necromancers,bullets,bombs,flares,ammopickup,torches


def refillstageentities (settings, play, targets, necromancers, bullets, bombs, ammopickup, space, torches, flares):
    while len(targets) < settings['targets']:
        location = openlocation(play,targets,necromancers,bullets,bombs,ammopickup,space = targetspace(settings,play,space),torches = torches,flares = flares)
        targets.append(target(location))
    while len(necromancers) < settings['necromancers']:
        location = openlocation(play,targets,necromancers,bullets,bombs,ammopickup,space = space,torches = torches,flares = flares)
        model = 'b' if settings.get('wilds') and len(necromancers) % 3 == 0 else ('w' if settings.get('wilds') else ('C' if settings.get('cultists') else None))
        necromancers.append(necromancer(location,model))
    if ammopickup is None:
        location = openlocation(play,targets,necromancers,bullets,bombs,space = space,torches = torches,flares = flares)
        ammopickup = ammo(location)
    return ammopickup


def collectammo (play, ammopickup):
    if play.location == ammopickup.location:
        play.reload(cursed = False)
        play.notice = '~Ammo collected.~'
        logevent(play,'Collected ammo.')
        return None
    return ammopickup


def playeraction (play, user, bullets, bombs, space, flares = None):
    flares = [] if flares is None else flares
    previous = list(play.location)
    moveplayer(play,user,space)
    if play.model == icon('player_hit') or play.health <= 0:
        return
    if user == 'q':
        dashplayer(play,space)
        logevent(play,'Dashed forward.' if play.location != previous else 'Dash blocked.')
    if user in ['w','a','s','d']:
        names = {'w':'north','a':'west','s':'south','d':'east'}
        logevent(play,f'Moved {names[user]}.' if play.location != previous else f'Faced {names[user]}.')
    if user == 'e':
        if play.shoot():
            bullets.append(bullet(play.model,play.location,play.bulletrange))
            logevent(play,'Fired a shot.')
        else:
            play.notice = '~No more ammo, find more to shoot.~'
    if user == 'b':
        if play.bombs <= 0:
            play.notice = '~No bombs left. Buy bombs in the shop.~'
        else:
            bombs.append(bomb(play.location,play.bombfuse,play.bombradius))
            logevent(play,'Armed a bomb.')
            play.bombs -= 1
            play.notice = '~Bomb armed. Move away before it explodes.~'
    if user == 'f':
        if play.flares <= 0:
            play.notice = '~No flares left. Buy flares in the shop.~'
        else:
            flares.append(flare(play.location))
            logevent(play,'Lit a flare.')
            play.flares -= 1
            play.notice = '~Flare lit. Necromancers will be stunned.~'


def targetdestroyed (play, item, targets):
    item.destroyed()
    targets.remove(item)
    play.score += 1
    play.reload()
    logevent(play,'Destroyed a target.')


def necromancerdestroyed (play, item, necromancers):
    item.destroyed()
    necromancers.remove(item)
    play.score += 1
    play.reload(2)
    logevent(play,'Defeated an enemy.')


def logevent (play, message):
    play.eventlog.append(message)
    play.eventlog = play.eventlog[-5:]


def updatebullets (play, bullets, targets, necromancers, space, enemyboss = None):
    remaining = []
    for item in bullets:
        if not item.active:
            item.active = True
        else:
            if item.maxrange is not None and item.travelled >= item.maxrange:
                continue
            item.movement()
            item.travelled += 1
        if tuple(item.location) not in space:
            continue

        hit = False
        for enemy in list(targets):
            if item.location == enemy.location:
                targetdestroyed(play,enemy,targets)
                hit = True
                break
        if hit:
            continue

        for enemy in list(necromancers):
            if item.location == enemy.location:
                enemy.damaged()
                if enemy.health <= 0:
                    necromancerdestroyed(play,enemy,necromancers)
                hit = True
                break
        if hit:
            continue

        if enemyboss is not None and tuple(item.location) in bosshitbox(enemyboss):
            enemyboss.damaged()
            updateboss(enemyboss)
            continue
        remaining.append(item)
    return remaining


def updatebombs (play, bombs, targets, necromancers, space, enemyboss = None, destroyedwalls = None):
    explosions = set()
    remaining = []
    for item in bombs:
        if not item.tick():
            remaining.append(item)
            continue

        destroyed = destroyterrain(item,space)
        if destroyedwalls is not None:
            destroyedwalls.update(destroyed)
        blast = bombcoordinates(item,space)
        explosions.update(blast)
        if tuple(play.location) in blast:
            if play.attacked():
                play.notice = '~You were caught in the blast.~'
        for enemy in list(targets):
            if tuple(enemy.location) in blast:
                targetdestroyed(play,enemy,targets)
        for enemy in list(necromancers):
            if tuple(enemy.location) in blast:
                enemy.damaged(2)
                if enemy.health <= 0:
                    necromancerdestroyed(play,enemy,necromancers)
        if enemyboss is not None:
            if len(blast.intersection(bosshitbox(enemyboss))) > 0:
                enemyboss.damaged(2)
                updateboss(enemyboss)
    return remaining,explosions


def updateflares (flares, necromancers, space):
    explosions = set()
    remaining = []
    for item in flares:
        if not item.tick():
            remaining.append(item)
            continue
        blast = bombcoordinates(item,space)
        explosions.update(blast)
        for enemy in necromancers:
            if tuple(enemy.location) in blast:
                enemy.stun(item.stunturns)
    return remaining,explosions


def enemyforecasts (necromancers, enemyboss = None):
    forecasts = []
    for enemy in necromancers:
        name = 'BOMBER' if enemy.normalmodel == 'b' else 'WOLF' if enemy.normalmodel == 'w' else 'CULTIST' if enemy.normalmodel == 'C' else 'NECROMANCER'
        intent = 'DETONATE' if enemy.normalmodel == 'b' else 'POUNCE' if enemy.normalmodel == 'w' else 'ADVANCE'
        if enemy.attackcounter == 1:
            intent = 'CHARGING SPELL'
        elif enemy.attackcounter in [2,3]:
            intent = 'AIMING STRIKE'
        elif enemy.attackcounter == 4:
            intent = 'RECOVERING'
        forecasts.append(f'{name}: {intent}')
    if enemyboss is not None:
        forecasts.insert(0,f'BOSS: {enemyboss.attackname} IN {max(0,4 - enemyboss.attackcounter)}')
    return forecasts


def compasslines (play, targets, necromancers, ammopickup, exitdoor):
    contacts = []
    if exitdoor is not None:
        contacts.append(('EXIT',exitdoor.location,2))
    else:
        contacts.extend([('ENEMY',item.location,0) for item in necromancers])
        contacts.extend([('TARGET',item.location,1) for item in targets])
        if ammopickup is not None:
            contacts.append(('AMMO',ammopickup.location,1))
    if not contacts:
        return []
    name,location,priority = min(contacts,key = lambda item: (abs(item[1][0] - play.location[0]) + abs(item[1][1] - play.location[1]),item[2]))
    dx,dy = location[0] - play.location[0],location[1] - play.location[1]
    direction = ('N' if dy > 0 else 'S' if dy < 0 else '') + ('E' if dx > 0 else 'W' if dx < 0 else '')
    arrows = {
        'N':'   ^   ', 'NE':'   /^  ', 'E':'@----> ', 'SE':'   \\v  ',
        'S':'   v   ', 'SW':'  v/   ', 'W':' <----@', 'NW':'  ^\\   ',
        '':'   @   '
    }
    distance = abs(dx) + abs(dy)
    return [f'{name} • {distance}',arrows[direction]]


def minimaplines (play, space, targets = None, necromancers = None, ammopickup = None, exitdoor = None, width = 15, height = 5):
    targets = [] if targets is None else targets
    necromancers = [] if necromancers is None else necromancers
    lines = []
    playerx = min(width - 1,max(0,int(play.location[0] * width / WORLDWIDTH)))
    playery = min(height - 1,max(0,int(play.location[1] * height / WORLDHEIGHT)))
    markers = {}
    for enemy in necromancers:
        markers[(min(width - 1,int(enemy.location[0] * width / WORLDWIDTH)),min(height - 1,int(enemy.location[1] * height / WORLDHEIGHT)))] = enemy.model
    if exitdoor is not None or len(necromancers) == 0:
        for item in targets:
            markers[(min(width - 1,int(item.location[0] * width / WORLDWIDTH)),min(height - 1,int(item.location[1] * height / WORLDHEIGHT)))] = item.model
        if ammopickup is not None:
            markers[(min(width - 1,int(ammopickup.location[0] * width / WORLDWIDTH)),min(height - 1,int(ammopickup.location[1] * height / WORLDHEIGHT)))] = ammopickup.model
        if exitdoor is not None:
            markers[(min(width - 1,int(exitdoor.location[0] * width / WORLDWIDTH)),min(height - 1,int(exitdoor.location[1] * height / WORLDHEIGHT)))] = exitdoor.model
    for row in reversed(range(height)):
        line = ''
        for column in range(width):
            world = (min(WORLDWIDTH - 1,int((column + 0.5) * WORLDWIDTH / width)),min(WORLDHEIGHT - 1,int((row + 0.5) * WORLDHEIGHT / height)))
            line += '@' if (column,row) == (playerx,playery) else markers.get((column,row),'.' if world in space else '#')
        lines.append(line)
    return lines


def printgame (play, level = None, targets = None, necromancers = None, bullets = None, bombs = None, ammopickup = None, enemyboss = None, explosions = None, space = None, explored = None, vision = 5, destroyedwalls = None, scoregoal = 5, torches = None, exitdoor = None, revealed = False, curse = None, flares = None):
    necromancers = [] if necromancers is None else necromancers
    play.forecasts = enemyforecasts(necromancers,enemyboss)
    play.minimap = [] if space is None else minimaplines(play,space,targets,necromancers,ammopickup,exitdoor)
    entitydict = updatedict(play,targets,necromancers,bullets,bombs,ammopickup,enemyboss,explosions,torches,exitdoor,flares)
    if space is not None:
        if revealed or explored is None:
            entitydict = mapdict(entitydict,space,destroyedwalls)
        else:
            visible = lightcoordinates(play,space,vision,bullets,bombs,torches,explosions,flares)
            explored.update(visible)
            entitydict = fogdict(entitydict,space,visible,explored,destroyedwalls)
    sidebar = hudlines(play,level,enemyboss,0 if bombs is None else len(bombs),scoregoal,curse,0 if flares is None else len(flares))
    effect = 'impact' if play.impactframes > 0 else 'explosion' if explosions else None
    if effect is not None:
        printgameframe(entitydict,sidebar,play.status == 'dead',play.location,effect)
        time.sleep(0.07)
        play.impactframes = max(0,play.impactframes - 1)
    printgameframe(entitydict,sidebar,play.status == 'dead',play.location)


#THREATCON LEVELS

def opendoor (play, space, torches):
    locations = {
        coordinate for coordinate in space
        if abs(coordinate[0] - play.location[0]) + abs(coordinate[1] - play.location[1]) >= 6
    }
    if len(locations) == 0:
        locations = space
    location = openlocation(play,[],[],[],[],space = locations,torches = torches)
    return door(location)


def exitstage (play, settings, space, torches, curse):
    exitdoor = opendoor(play,space,torches)
    while True:
        printgame(play,settings['level'],space = space,scoregoal = settings['score'],torches = torches,exitdoor = exitdoor,revealed = True,curse = curse)
        if play.location == exitdoor.location:
            return play
        user = promptinput('[W/A/S/D/Q]: ')
        tickplayerabilities(play)
        if user == 'q':
            dashplayer(play,space)
        else:
            moveplayer(play,user,space)


def cursedsettings (settings, curse):
    stage = dict(settings)
    modifiers = cursemodifiers(curse)
    stage['vision'] = max(1,stage['vision'] + modifiers.get('vision',0))
    stage['necromancers'] = max(0,stage['necromancers'] + modifiers.get('necromancers',0))
    stage['ammo'] = max(0,stage['ammo'] + modifiers.get('ammo',0))
    stage['score'] = max(1,stage['score'] + modifiers.get('score',0))
    stage['torches'] = max(0,stage['torches'] + modifiers.get('torches',0))
    stage['health'] = modifiers.get('health',2)
    stage['bombs'] = modifiers.get('bombs',1)
    stage['flares'] = modifiers.get('flares',1)
    stage['torchradius'] = modifiers.get('torchradius',4)
    stage['dashcooldown'] = modifiers.get('dashcooldown',3)
    stage['bombradius'] = modifiers.get('bombradius',2)
    stage['bombfuse'] = modifiers.get('bombfuse',3)
    stage['bulletrange'] = modifiers.get('bulletrange')
    stage['reloadpenalty'] = modifiers.get('reloadpenalty',0)
    return stage


def classsettings (settings, classid):
    stage = dict(settings)
    modifiers = PLAYER_CLASS_BY_ID[classid]['modifiers']
    return applymodifiers(stage,modifiers)


def boonsettings (settings, boonid):
    stage = dict(settings)
    modifiers = BOON_BY_ID[boonid]['modifiers']
    return applymodifiers(stage,modifiers)


def applymodifiers (stage, modifiers):
    stage['health'] = min(5,max(1,stage['health'] + modifiers.get('health',0)))
    stage['ammo'] = max(0,stage['ammo'] + modifiers.get('ammo',0))
    stage['bombs'] = max(0,stage['bombs'] + modifiers.get('bombs',0))
    stage['flares'] = max(0,stage['flares'] + modifiers.get('flares',0))
    stage['vision'] = max(1,stage['vision'] + modifiers.get('vision',0))
    stage['dashcooldown'] = max(1,stage['dashcooldown'] + modifiers.get('dashcooldown',0))
    return stage


def runstage (settings, curse = None, classid = 'vanguard', boonid = 'iron_heart'):
    settings = cursedsettings(settings,curse)
    settings = classsettings(settings,classid)
    settings = boonsettings(settings,boonid)
    play = player(settings['health'])
    play.classname = PLAYER_CLASS_BY_ID[classid]['name']
    play.boonname = BOON_BY_ID[boonid]['name']
    play.ammo = settings['ammo']
    play.bombs = settings['bombs']
    play.flares = settings['flares']
    play.dashcooldownbase = settings['dashcooldown']
    play.bombradius = settings['bombradius']
    play.bombfuse = settings['bombfuse']
    play.bulletrange = settings['bulletrange']
    play.reloadpenalty = settings['reloadpenalty']
    configureterrain(settings['terrain'])
    space = generatespace(play.location,terrain = settings['terrain'])
    explored = set()
    destroyedwalls = set()
    targets,necromancers,bullets,bombs,flares,ammopickup,torches = createstageentities(settings,play,space)
    printgame(play,settings['level'],targets,necromancers,bullets,bombs,ammopickup,space = space,explored = explored,vision = settings['vision'],destroyedwalls = destroyedwalls,scoregoal = settings['score'],torches = torches,curse = curse,flares = flares)

    while play.health > 0 and play.score < settings['score']:
        user = promptinput('[W/A/S/D/E/B/F/Q]: ')
        tickplayerabilities(play)
        play.notice = ''
        playeraction(play,user,bullets,bombs,space,flares)
        ammopickup = collectammo(play,ammopickup)
        bullets = updatebullets(play,bullets,targets,necromancers,space)
        bombs,bombexplosions = updatebombs(play,bombs,targets,necromancers,space,destroyedwalls = destroyedwalls)
        flares,flareexplosions = updateflares(flares,necromancers,space)
        explosions = bombexplosions.union(flareexplosions)
        if play.score < settings['score']:
            attackplayer(play,necromancers,space)
            ammopickup = refillstageentities(settings,play,targets,necromancers,bullets,bombs,ammopickup,space,torches,flares)
        printgame(play,settings['level'],targets,necromancers,bullets,bombs,ammopickup,None,explosions,space,explored,settings['vision'],destroyedwalls,settings['score'],torches,curse = curse,flares = flares)
    if play.health <= 0:
        return play
    return exitstage(play,settings,space,torches,curse)


#BOSS FIGHT

def resetplayer (play, classid = 'vanguard', boonid = 'iron_heart'):
    play.location = [20,9]
    play.direction = 'w'
    play.model = icon('player_up')
    play.score = 0
    if play.bombs < 1:
        play.bombs = 1
    if play.flares < 1:
        play.flares = 1
    play.status = 'alive'
    play.notice = ''
    play.dashcooldown = 0
    play.dashcooldownbase = max(1,3 + PLAYER_CLASS_BY_ID[classid]['modifiers'].get('dashcooldown',0) + BOON_BY_ID[boonid]['modifiers'].get('dashcooldown',0))
    play.bombradius = 2
    play.bombfuse = 3
    play.bulletrange = None
    play.reloadpenalty = 0


def bossminionlimit (enemyboss):
    if enemyboss.health < 4:
        return 3
    if enemyboss.health < 8:
        return 2
    return 1


def summonbossminion (play, enemyboss, targets, necromancers, bullets, bombs, ammopickup, space, flares):
    if enemyboss.attackcounter != 0 or len(necromancers) >= bossminionlimit(enemyboss):
        return
    spawnspace = {
        coordinate for coordinate in space
        if abs(coordinate[0] - play.location[0]) + abs(coordinate[1] - play.location[1]) >= 6
    }
    if len(spawnspace) == 0:
        return
    location = openlocation(play,targets,necromancers,bullets,bombs,ammopickup,bosshitbox(enemyboss),spawnspace,flares = flares)
    necromancers.append(necromancer(location))
    play.notice = '~The boss summoned a necromancer.~'


def runboss (play, classid = 'vanguard', boonid = 'iron_heart'):
    resetplayer(play,classid,boonid)
    enemyboss = boss()
    targets = []
    necromancers = []
    bullets = []
    bombs = []
    flares = []
    space = generatespace(play.location,bosshitbox(enemyboss),arena = (2,2,37,15))
    destroyedwalls = set()
    enemyboss.newattack(space)
    location = openlocation(play,targets,necromancers,bullets,bombs,None,bosshitbox(enemyboss),space,flares = flares)
    ammopickup = ammo(location)
    printgame(play,None,targets,necromancers,bullets,bombs,ammopickup,enemyboss,space = space,destroyedwalls = destroyedwalls,flares = flares)

    while play.health > 0 and enemyboss.health > 0:
        user = promptinput('[W/A/S/D/E/B/F/Q]: ')
        tickplayerabilities(play)
        play.notice = ''
        playeraction(play,user,bullets,bombs,space,flares)
        ammopickup = collectammo(play,ammopickup)
        if ammopickup is None:
            location = openlocation(play,targets,necromancers,bullets,bombs,None,bosshitbox(enemyboss),space,flares = flares)
            ammopickup = ammo(location)
        bullets = updatebullets(play,bullets,targets,necromancers,space,enemyboss)
        bombs,bombexplosions = updatebombs(play,bombs,targets,necromancers,space,enemyboss,destroyedwalls)
        flares,flareexplosions = updateflares(flares,necromancers,space)
        explosions = bombexplosions.union(flareexplosions)
        if enemyboss.health > 0:
            attackplayer(play,[enemyboss],space)
        if play.health > 0 and enemyboss.health > 0:
            attackplayer(play,necromancers,space)
            summonbossminion(play,enemyboss,targets,necromancers,bullets,bombs,ammopickup,space,flares)
        printgame(play,None,targets,necromancers,bullets,bombs,ammopickup,enemyboss,explosions,space,destroyedwalls = destroyedwalls,flares = flares)

    if enemyboss.health <= 0 and play.health > 0:
        enemyboss.destroyed()
        printgame(play,None,targets,necromancers,bullets,bombs,ammopickup,enemyboss,space = space,destroyedwalls = destroyedwalls,flares = flares)
        print ('          ~You have won the game~         ')
        return True
    return False


def rundebuglevel (level, classid = 'vanguard', boonid = 'iron_heart'):
    if level < len(STAGES):
        runstage(STAGES[level],classid = classid,boonid = boonid)
        return
    settings = boonsettings(classsettings(cursedsettings(STAGES[0],None),classid),boonid)
    play = player(settings['health'])
    play.classname = PLAYER_CLASS_BY_ID[classid]['name']
    play.boonname = BOON_BY_ID[boonid]['name']
    play.ammo = settings['ammo']
    play.bombs = settings['bombs']
    play.flares = settings['flares']
    runboss(play,classid,boonid)


def rungame (debuglevel = None):
    if debuglevel is not None:
        classid = chooseclass()
        rundebuglevel(debuglevel,classid,chooseboon())
        return
    titlescreen()
    totscore = 0
    play = None
    classid = chooseclass()
    boonid = chooseboon()
    bag = cursebag()
    for settings in STAGES:
        curse = None
        if settings['level'] > 0:
            curse = continuestage(settings['level'],bag)
            if curse is None:
                return
        play = runstage(settings,curse,classid,boonid)
        if play.health <= 0:
            return
        totscore += play.score

    play = runshop(totscore,play)
    runboss(play,classid,boonid)
