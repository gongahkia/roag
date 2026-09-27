"""The upgrade shop and curse-selection interface."""

import time

from functions import BOARDHEIGHT, BOARDWIDTH, cursebag, icon, iconlabel
from game_ui import printscreen, promptinput


class shop:
    def __init__ (self):
        self.pointer = 0
        self.items = ['HEALTH','AMMO','BOMBS','FLARES']

    def move (self, direction):
        if direction == 'w':
            self.pointer -= 1
        if direction == 's':
            self.pointer += 1
        self.pointer = max(0,min(self.pointer,len(self.items) - 1))

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
            message = f'No {item.lower()} left to sell!' if minimum == 0 else f'Base {item.lower()} {minimum}, cannot be sold!'
            print (message)
            time.sleep(0.75)
            return points
        self.changeitem(player,item,-1)
        return points + 1

    def screenlines (self, player, points):
        lines = ['','','                ~WELCOME~','               TO THE SHOP','','']
        markers = {
            'HEALTH': icon('shop_health'),
            'AMMO': icon('shop_ammo'),
            'BOMBS': icon('shop_bombs'),
            'FLARES': icon('shop_flares')
        }
        for index,item in enumerate(self.items):
            pointer = ' --> ' if self.pointer == index else '     '
            value = self.itemvalue(player,item)
            meter = ''.join(f'{markers[item]} ' for number in range(value))
            lines.append(f'     {pointer} {item:<6} |{meter:<10}|')
            lines.append('')
        lines.append(f'           POINTS LEFT: {points}')
        return lines

    def printscreen (self, player, points):
        printscreen(self.screenlines(player,points))


def runshop (num, player):
    s = shop()
    while True:
        s.printscreen(player,num)
        user = promptinput('[W/S/B/V/E]: ')
        if user in ['w','s']:
            s.move(user)
        elif user == 'b':
            num = s.buy(player,num)
        elif user == 'v':
            num = s.sell(player,num)
        elif user == 'e':
            print ('Would you like to continue to the next stage?'.center(BOARDWIDTH))
            if promptinput('[Y/N]: ') == 'y':
                return player
            print ('Okay. Continue browsing.')
            time.sleep(1.5)


class curseshop:
    def __init__ (self, items):
        self.pointer = 0
        self.items = items

    def move (self, direction):
        self.pointer += -1 if direction == 'w' else 1
        self.pointer = max(0,min(self.pointer,len(self.items) - 1))

    def selected (self):
        return self.items[self.pointer]['id']

    def screenlines (self):
        lines = [iconlabel('curse','~CURSE SHOP~'),'CHOOSE A BURDEN TO DESCEND','']
        for index,item in enumerate(self.items):
            lines.extend([f'[ {item["name"]} ]' if self.pointer == index else item['name'],item['description'],''])
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
        if user in ['w','s']:
            s.move(user)
        elif user == 'e':
            return s.selected()
