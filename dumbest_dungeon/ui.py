"""Keyboard-driven curses presentation for the game engine."""

from __future__ import annotations

import curses
import textwrap
from pathlib import Path
from typing import Callable

from .content import Catalog
from .engine import CardInstance, GameEngine, RuleError
from .save import SaveError, read_save, write_save


class TerminalUI:
    MIN_ROWS = 24
    MIN_COLS = 80

    def __init__(
        self,
        screen: curses.window,
        catalog: Catalog,
        save_path: Path,
        new_game: Callable[[], GameEngine],
    ):
        self.screen = screen
        self.catalog = catalog
        self.save_path = save_path
        self.new_game = new_game
        self.engine: GameEngine | None = None
        self.message = ""
        self.colour = False
        self._configure()

    def _configure(self) -> None:
        curses.curs_set(0)
        self.screen.keypad(True)
        if curses.has_colors():
            curses.start_color()
            curses.use_default_colors()
            curses.init_pair(1, curses.COLOR_CYAN, -1)
            curses.init_pair(2, curses.COLOR_YELLOW, -1)
            curses.init_pair(3, curses.COLOR_RED, -1)
            curses.init_pair(4, curses.COLOR_GREEN, -1)
            self.colour = True

    def run(self) -> None:
        while True:
            choices = ["New expedition"]
            if self.save_path.exists():
                choices.append("Load expedition")
            choices.extend(["How to play", "Quit"])
            picked = self._menu(
                "DUMBEST DUNGEON",
                choices,
                "\n".join(self.catalog.art["title"])
                + "\n\nSurvive the derelict survey ship Orison. Seeded runs, bad decisions.",
                allow_cancel=False,
            )
            choice = choices[picked]
            if choice == "New expedition":
                self.engine = self.new_game()
                self._game_loop()
            elif choice == "Load expedition":
                if self._load():
                    self._game_loop()
            elif choice == "How to play":
                self._help()
            else:
                return

    def _game_loop(self) -> None:
        while self.engine:
            phase = self.engine.state.phase
            try:
                if phase == "exploration":
                    self._exploration()
                elif phase == "combat":
                    self._combat()
                elif phase == "event":
                    self._event()
                elif phase == "reward":
                    self._reward()
                elif phase == "service":
                    self._service()
                elif phase in {"victory", "defeat"}:
                    self._ending(phase)
                    self.engine = None
                else:
                    raise RuleError(f"unknown game phase: {phase}")
            except RuleError as exc:
                self.message = str(exc)

    def _exploration(self) -> None:
        assert self.engine
        state = self.engine.state
        room = self.engine.room()
        adjacent = [self.engine.room(room_id) for room_id in room.neighbors]
        selected = 0
        while state.phase == "exploration":
            self._begin("SHIP MAP")
            self._resources(2)
            self._map(4)
            row = 14
            self._put(row, 2, f"Current: [{room.id:02}] {room.name}", self._attr(1) | curses.A_BOLD)
            row += 1
            for index, destination in enumerate(adjacent):
                marker = ">" if index == selected else " "
                known = destination.name if destination.visited else "Unscanned compartment"
                status = "cleared" if destination.resolved else "unresolved"
                self._put(row + index, 3, f"{marker} [{destination.id:02}] {known} ({status})", curses.A_REVERSE if index == selected else 0)
            self._footer("↑/↓ choose  Enter travel  U use supply  D deck  P pause  ? help")
            key = self._key()
            if key in (curses.KEY_UP, curses.KEY_LEFT, ord("k"), ord("h")):
                selected = (selected - 1) % len(adjacent)
            elif key in (curses.KEY_DOWN, curses.KEY_RIGHT, ord("j"), ord("l")):
                selected = (selected + 1) % len(adjacent)
            elif key in (10, 13, curses.KEY_ENTER):
                self.engine.move_to(adjacent[selected].id)
            elif key in (ord("u"), ord("U")):
                self._supply_menu()
            elif key in (ord("d"), ord("D")):
                self._deck_view()
            elif key in (ord("p"), ord("P"), 27):
                self._pause()
                return
            elif key == ord("?"):
                self._help()

    def _combat(self) -> None:
        assert self.engine
        selected = 0
        while self.engine and self.engine.state.phase == "combat":
            state = self.engine.state
            selected = min(selected, max(0, len(state.hand) - 1))
            self._begin(f"COMBAT — ROUND {state.round} — ENERGY {state.energy}")
            active_hero = None
            valid_targets: list[str] = []
            if state.hand:
                definition = self.catalog.cards[state.hand[selected].card_id]
                active_hero = definition["hero"]
                valid_targets = self.engine.valid_targets(selected)
            self._battlefield(2, active_hero, valid_targets)
            self._intents(11, 2)
            self._put(13, 2, "HAND", curses.A_BOLD)
            max_cards = min(len(state.hand), 5)
            for index, card in enumerate(state.hand[:max_cards]):
                definition = self.catalog.cards[card.card_id]
                actor = next(item for item in state.heroes if item.id == definition["hero"])
                legal = actor.rank in definition["from_ranks"] and self.engine.card_cost(card) <= state.energy
                marker = ">" if index == selected else " "
                upgraded = "+" if card.upgraded else ""
                line = f"{marker} {index + 1}. [{self.engine.card_cost(card)}] {definition['name']}{upgraded}"
                attr = curses.A_REVERSE if index == selected else (0 if legal else curses.A_DIM)
                self._put(14 + index, 3, line[:39], attr)
            if state.hand:
                self._draw_card(13, 45, state.hand[selected])
            self._put(20, 2, "LOG", curses.A_BOLD)
            for offset, entry in enumerate(state.log[-2:]):
                self._put(21 + offset, 3, entry[:39], curses.A_DIM)
            self._footer("↑/↓ choose  Enter play  E end turn  P pause  ? help")
            key = self._key()
            if key in (curses.KEY_UP, curses.KEY_LEFT, ord("k"), ord("h")) and state.hand:
                selected = (selected - 1) % len(state.hand)
            elif key in (curses.KEY_DOWN, curses.KEY_RIGHT, ord("j"), ord("l")) and state.hand:
                selected = (selected + 1) % len(state.hand)
            elif key in (10, 13, curses.KEY_ENTER) and state.hand:
                targets = self.engine.valid_targets(selected)
                if not targets:
                    self.message = "That card has no legal target from this formation."
                    continue
                target = targets[0]
                if len(targets) > 1:
                    labels = [self._actor_label(actor_id) for actor_id in targets]
                    choice = self._menu("CHOOSE TARGET", labels, allow_cancel=True)
                    if choice is None:
                        continue
                    target = targets[choice]
                self.engine.play_card(selected, target)
            elif key in (ord("e"), ord("E")):
                self.engine.end_turn()
            elif key in (ord("p"), ord("P"), 27):
                self._pause()
                return
            elif key == ord("?"):
                self._help()

    def _battlefield(self, row: int, active_hero: str | None, valid_targets: list[str]) -> None:
        assert self.engine
        self._put(row, 2, "CREW  < BACK     FORMATION     FRONT >", curses.A_BOLD)
        self._put(row, 43, "HOSTILES < FRONT   FORMATION  BACK >", curses.A_BOLD)
        self._put(row + 1, 39, "||", curses.A_BOLD)
        for rank in range(1, 5):
            hero = next((item for item in self.engine.living_heroes() if item.rank == rank), None)
            enemy = next((item for item in self.engine.living_enemies() if item.rank == rank), None)
            if hero:
                column = 29 - (rank - 1) * 9
                attr = self._hp_attr(hero.hp, hero.max_hp)
                if hero.id == active_hero:
                    attr |= curses.A_BOLD
                self._draw_sprite(row + 1, column, self.catalog.art["heroes"][hero.id], attr)
                self._put(row + 6, column, f"R{rank} {hero.hero_class[:4].upper():4}", attr)
                hp = "DD" if hero.deaths_door else f"{hero.hp:02}"
                self._put(row + 7, column, f"H{hp}/{hero.max_hp:02}", attr)
                self._put(row + 8, column, f"S{hero.stress:02} B{hero.block:02}", attr)
            if enemy:
                column = 43 + (rank - 1) * 9
                attr = self._hp_attr(enemy.hp, enemy.max_hp)
                if enemy.id in valid_targets or "all_enemies" in valid_targets:
                    attr |= curses.A_BOLD
                art_id = enemy.definition_id or enemy.id
                self._draw_sprite(row + 1, column, self.catalog.art["enemies"][art_id], attr)
                short_name = "".join(word[0] for word in enemy.name.split()).upper()[:4]
                self._put(row + 6, column, f"R{rank} {short_name:4}", attr)
                self._put(row + 7, column, f"H{enemy.hp:02}/{enemy.max_hp:02}", attr)
                status = "".join(name[0].upper() for name in enemy.statuses)[:3]
                self._put(row + 8, column, f"B{enemy.block:02} {status:3}", attr)

    def _intents(self, row: int, max_lines: int) -> None:
        assert self.engine
        descriptions = []
        for intent in self.engine.state.intents:
            enemy = next((actor for actor in self.engine.living_enemies() if actor.id == intent["enemy_id"]), None)
            if not enemy:
                continue
            actions = self.catalog.enemies[enemy.definition_id or enemy.id]["actions"]
            action = next(item for item in actions if item["name"] == intent["action"])
            effects = []
            for effect in action["effects"]:
                label = {"damage": "dmg", "stress": "stress", "block": "block", "heal": "heal"}.get(effect["op"], effect["op"])
                effects.append(f"{effect.get('amount', '')}{label}")
            descriptions.append(f"R{enemy.rank} {action['name']} ({action['target']}; {'+'.join(effects)})")
        text = "  |  ".join(descriptions)
        lines = textwrap.wrap(f"INTENTS: {text}", max(20, self.screen.getmaxyx()[1] - 4))
        for offset, line in enumerate(lines[:max_lines]):
            self._put(row + offset, 2, line, self._attr(2))

    def _event(self) -> None:
        assert self.engine and self.engine.state.current_event
        event = self.catalog.events[self.engine.state.current_event]
        choices = [item["label"] for item in event["choices"]]
        picked = self._menu(event["name"].upper(), choices, event["text"], allow_cancel=False)
        self.engine.choose_event(picked)

    def _reward(self) -> None:
        assert self.engine
        choices = []
        for card_id in self.engine.state.rewards:
            card = self.catalog.cards[card_id]
            choices.append(f"{card['name']} ({self.catalog.heroes[card['hero']]['role']}) — {card['description']}")
        choices.append("Skip reward")
        previews = [CardInstance(card_id) for card_id in self.engine.state.rewards] + [None]
        picked = self._menu(
            "RECOVERED TECHNIQUE",
            choices,
            "Add one card to the shared party deck.",
            allow_cancel=False,
            preview_cards=previews,
        )
        self.engine.choose_reward(None if picked == len(choices) - 1 else picked)

    def _service(self) -> None:
        assert self.engine
        service_type = self.engine.state.service_type
        choices = ["Upgrade a card", "Remove a card"]
        if service_type == "camp":
            choices.insert(0, "Recover: heal 7 and reduce 10 stress")
        picked = self._menu("CREW QUARTERS" if service_type == "camp" else "WORKSHOP", choices, "The room can be used once.", allow_cancel=False)
        action = choices[picked]
        if action.startswith("Recover"):
            self.engine.service("recover")
            return
        eligible = [
            index for index, card in enumerate(self.engine.state.deck)
            if action.startswith("Remove") or not card.upgraded
        ]
        labels = [self._card_label(self.engine.state.deck[index]) for index in eligible]
        previews = [self.engine.state.deck[index] for index in eligible]
        selected = self._menu(action.upper(), labels, allow_cancel=False, preview_cards=previews)
        self.engine.service("remove" if action.startswith("Remove") else "upgrade", eligible[selected])

    def _supply_menu(self) -> None:
        assert self.engine
        if self.engine.state.supplies < 1:
            self.message = "No supplies remain."
            return
        choices = ["Treat the most injured crew member", "Calm the most stressed crew member", "Ignite a flare (+25 light)"]
        picked = self._menu("USE ONE SUPPLY", choices, allow_cancel=True)
        if picked is not None:
            self.engine.use_supply(("heal", "calm", "light")[picked])

    def _deck_view(self) -> None:
        assert self.engine
        labels = [self._card_label(card) for card in self.engine.state.deck]
        self._menu(
            "PARTY DECK",
            labels,
            f"{len(labels)} cards. Upgraded cards have a +.",
            allow_cancel=True,
            view_only=True,
            preview_cards=list(self.engine.state.deck),
        )

    def _pause(self) -> None:
        if not self.engine:
            return
        choices = ["Resume", "Save game", "Load game", "How to play", "Abandon to title"]
        picked = self._menu("PAUSED", choices, f"Save file: {self.save_path}", allow_cancel=True)
        if picked in (None, 0):
            return
        if picked == 1:
            try:
                write_save(self.save_path, self.engine.snapshot())
                self.message = "Game saved."
            except SaveError as exc:
                self.message = str(exc)
        elif picked == 2:
            self._load()
        elif picked == 3:
            self._help()
        elif picked == 4:
            self.engine = None

    def _load(self) -> bool:
        try:
            self.engine = GameEngine.from_snapshot(self.catalog, read_save(self.save_path))
            self.message = "Game loaded."
            return True
        except (SaveError, RuleError) as exc:
            self.message = str(exc)
            self._notice("LOAD FAILED", self.message)
            return False

    def _ending(self, phase: str) -> None:
        assert self.engine
        title = "EVACUATION COMPLETE" if phase == "victory" else "EXPEDITION LOST"
        body = (
            "The Overseer is silent. The crew launches into the dark before the Orison can wake again."
            if phase == "victory"
            else "One voice drops from the comms. The survivors cannot finish the mission."
        )
        self._notice(title, body + f"\n\nSeed: {self.engine.state.seed}")

    def _help(self) -> None:
        text = (
            "Explore connected rooms and reach the Overseer Chamber. Every move drains light; darkness "
            "adds stress, increases ambushes, and offers a fourth card reward. In combat, spend shared "
            "energy on cards whose specialist occupies a valid rank. Enemy intents are shown before they act.\n\n"
            "At zero HP a crew member reaches Death's Door. Further damage may kill them and end the run. "
            "At 100 stress they gain an affliction; reaching 100 again causes collapse. Supplies heal, calm, "
            "or restore light. Camps recover crew or modify one card.\n\n"
            "Controls: arrows or hjkl navigate, Enter confirms, Escape cancels/pauses, E ends a combat turn, "
            "U uses a supply, D views the deck, P pauses, and ? opens this page."
        )
        self._notice("HOW TO PLAY", text)

    def _resources(self, row: int) -> None:
        assert self.engine
        state = self.engine.state
        self._put(row, 2, f"Seed {state.seed}   Light {state.light:3}/100   Supplies {state.supplies}", self._attr(2))
        crew = "  ".join(f"R{h.rank} {h.hero_class}: {h.hp}/{h.max_hp}hp {h.stress}s" for h in self.engine.living_heroes())
        self._put(row + 1, 2, crew)

    def _map(self, row: int) -> None:
        assert self.engine
        positions = {0:(0,0),1:(0,7),2:(-1,14),3:(1,14),4:(0,21),5:(-1,28),6:(1,28),7:(0,35),8:(-1,42),9:(1,42),10:(0,49),11:(0,57)}
        canvas = [[" " for _ in range(63)] for _ in range(5)]
        for room in self.engine.state.rooms:
            y, x = positions[room.id]
            y += 2
            if room.id == self.engine.state.current_room:
                token = f"<{room.id:02}>"
            elif room.visited:
                token = f"[{room.id:02}]"
            else:
                token = "[??]"
            for offset, character in enumerate(token):
                canvas[y][x + offset] = character
        for left, right in ((0,1),(1,2),(1,3),(2,4),(3,4),(4,5),(4,6),(5,7),(6,7),(7,8),(7,9),(8,10),(9,10),(10,11)):
            ly,lx=positions[left]; ry,rx=positions[right]; ly+=2; ry+=2
            if ly == ry:
                for x in range(lx+4, rx): canvas[ly][x] = "-"
            else:
                midx = (lx + rx) // 2
                for x in range(lx+4, midx+1): canvas[ly][x] = "-"
                for y in range(min(ly,ry)+1,max(ly,ry)): canvas[y][midx] = "|"
                for x in range(midx, rx): canvas[ry][x] = "-"
        for offset, line in enumerate(canvas):
            self._put(row + offset, 5, "".join(line))

    def _card_label(self, card: CardInstance) -> str:
        definition = self.catalog.cards[card.card_id]
        plus = "+" if card.upgraded else ""
        return f"{definition['name']}{plus} ({self.catalog.heroes[definition['hero']]['role']}) — {definition['description']}"

    def _actor_label(self, actor_id: str) -> str:
        assert self.engine
        actor = next(item for item in self.engine.state.heroes + self.engine.state.enemies if item.id == actor_id)
        return f"Rank {actor.rank}: {actor.name} ({actor.hp}/{actor.max_hp} HP)"

    def _draw_sprite(self, row: int, column: int, lines: list[str], attr: int = 0) -> None:
        for offset, line in enumerate(lines):
            self._put(row + offset, column, line.ljust(7), attr)

    def _draw_card(self, row: int, column: int, card: CardInstance) -> None:
        for offset, line in enumerate(self._card_lines(card)):
            attr = curses.A_BOLD | self._attr(1) if offset in {0, 1, 9} else 0
            self._put(row + offset, column, line, attr)

    def _card_lines(self, card: CardInstance) -> list[str]:
        assert self.engine
        definition = self.catalog.cards[card.card_id]
        width = 34
        inside = width - 2

        def framed(text: str = "") -> str:
            return "|" + text[:inside].ljust(inside) + "|"

        plus = "+" if card.upgraded else ""
        title = f"[{self.engine.card_cost(card)}] {definition['name'].upper()}{plus}"
        role = self.catalog.heroes[definition["hero"]]["role"].upper()
        glyph = self.catalog.art["card_glyphs"][definition["hero"]]
        ranks = ",".join(str(rank) for rank in definition["from_ranks"])
        target = definition["target"].replace("_", " ")
        if definition.get("target_ranks"):
            target += " " + ",".join(str(rank) for rank in definition["target_ranks"])
        description = textwrap.wrap(definition["description"], inside - 2)[:2]
        description += [""] * (2 - len(description))
        border = "+" + "-" * inside + "+"
        return [
            border,
            framed(title),
            framed(f"{role} // FROM {ranks}"),
            framed(glyph[0].center(inside)),
            framed(glyph[1].center(inside)),
            framed(glyph[2].center(inside)),
            framed(f"TARGET: {target}"),
            framed(" " + description[0]),
            framed(" " + description[1]),
            border,
        ]

    def _menu(
        self,
        title: str,
        choices: list[str],
        body: str = "",
        *,
        allow_cancel: bool = True,
        view_only: bool = False,
        preview_cards: list[CardInstance | None] | None = None,
    ) -> int | None:
        selected = 0
        scroll = 0
        while True:
            self._begin(title)
            row = 3
            body_width = 38 if preview_cards else max(20, self.screen.getmaxyx()[1] - 6)
            for paragraph in body.splitlines():
                for line in textwrap.wrap(paragraph, body_width) or [""]:
                    self._put(row, 3, line[:body_width])
                    row += 1
            row += 1
            available = max(1, self.screen.getmaxyx()[0] - row - 3)
            if selected < scroll:
                scroll = selected
            if selected >= scroll + available:
                scroll = selected - available + 1
            for shown, choice in enumerate(choices[scroll:scroll + available]):
                index = scroll + shown
                marker = ">" if index == selected else " "
                attr = curses.A_REVERSE if index == selected else 0
                width = 38 if preview_cards else self.screen.getmaxyx()[1] - 6
                self._put(row + shown, 3, f"{marker} {choice}"[:width], attr)
            if preview_cards and preview_cards[selected] is not None:
                self._draw_card(3, 45, preview_cards[selected])
            self._footer("↑/↓ choose  Enter confirm  Esc back" if not view_only else "↑/↓ scroll  Esc/Enter back")
            key = self._key()
            if key in (curses.KEY_UP, curses.KEY_LEFT, ord("k"), ord("h")):
                selected = (selected - 1) % len(choices)
            elif key in (curses.KEY_DOWN, curses.KEY_RIGHT, ord("j"), ord("l")):
                selected = (selected + 1) % len(choices)
            elif key in (10, 13, curses.KEY_ENTER):
                return None if view_only else selected
            elif key == 27 and allow_cancel:
                return None
            elif ord("1") <= key <= ord("9") and key - ord("1") < len(choices) and not view_only:
                return key - ord("1")

    def _notice(self, title: str, body: str) -> None:
        self._begin(title)
        row = 3
        for paragraph in body.splitlines():
            for line in textwrap.wrap(paragraph, max(20, self.screen.getmaxyx()[1] - 6)) or [""]:
                self._put(row, 3, line)
                row += 1
        self._footer("Press any key")
        self._key()

    def _begin(self, title: str) -> None:
        self._ensure_size()
        self.screen.erase()
        width = self.screen.getmaxyx()[1]
        self._put(0, max(0, (width - len(title)) // 2), title, curses.A_BOLD | self._attr(1))
        if self.message:
            self._put(1, 2, self.message, self._attr(2))
            self.message = ""

    def _footer(self, text: str) -> None:
        self._put(self.screen.getmaxyx()[0] - 1, 1, text, curses.A_DIM)
        self.screen.refresh()

    def _ensure_size(self) -> None:
        while True:
            rows, cols = self.screen.getmaxyx()
            if rows >= self.MIN_ROWS and cols >= self.MIN_COLS:
                return
            self.screen.erase()
            message = f"Terminal too small ({cols}x{rows}); resize to at least {self.MIN_COLS}x{self.MIN_ROWS}. Q quits."
            try:
                self.screen.addstr(0, 0, message[:max(1, cols - 1)])
            except curses.error:
                pass
            self.screen.refresh()
            if self.screen.getch() in (ord("q"), ord("Q")):
                raise KeyboardInterrupt

    def _key(self) -> int:
        try:
            return self.screen.getch()
        except curses.error:
            return -1

    def _put(self, row: int, col: int, text: str, attr: int = 0) -> None:
        rows, cols = self.screen.getmaxyx()
        if row < 0 or row >= rows or col >= cols:
            return
        try:
            self.screen.addstr(row, max(0, col), text[:max(0, cols - max(0, col) - 1)], attr)
        except curses.error:
            pass

    def _attr(self, pair: int) -> int:
        return curses.color_pair(pair) if self.colour else 0

    def _hp_attr(self, hp: int, maximum: int) -> int:
        if not self.colour:
            return 0
        ratio = hp / maximum
        return self._attr(4 if ratio > 0.5 else 2 if ratio > 0.25 else 3)
