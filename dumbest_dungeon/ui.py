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
    DAMAGE_FLASH_MS = 110
    ENEMY_ACTION_MS = 650
    ENEMY_ACTION_FAST_MS = 100
    MOVE_FRAME_MS = 55
    MAP_ROW = 5

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
        self._enemy_playback_fast = False
        self._skip_enemy_playback = False
        self._configure()

    def _configure(self) -> None:
        curses.curs_set(0)
        self.screen.keypad(True)
        try:
            curses.mousemask(curses.ALL_MOUSE_EVENTS)
            curses.mouseinterval(0)
        except curses.error:
            pass
        if curses.has_colors():
            curses.start_color()
            curses.use_default_colors()
            curses.init_pair(1, curses.COLOR_CYAN, -1)
            curses.init_pair(2, curses.COLOR_YELLOW, -1)
            curses.init_pair(3, curses.COLOR_RED, -1)
            curses.init_pair(4, curses.COLOR_GREEN, -1)
            curses.init_pair(5, curses.COLOR_WHITE, curses.COLOR_RED)
            curses.init_pair(6, curses.COLOR_WHITE, curses.COLOR_GREEN)
            curses.init_pair(7, curses.COLOR_BLACK, curses.COLOR_CYAN)
            self.colour = True

    def run(self) -> None:
        while True:
            choices = ["Tutorial expedition (recommended)", "New expedition"]
            if self.save_path.exists():
                choices.append("Load expedition")
            choices.extend(["How to play", "Quit"])
            picked = self._menu(
                "DULLEST DUNGEON",
                choices,
                "\n".join(self.catalog.art["title"])
                + "\n\nCross a shifting dead world. Seeded runs, bad decisions.",
                allow_cancel=False,
            )
            choice = choices[picked]
            if choice.startswith("Tutorial"):
                self.engine = GameEngine.tutorial(self.catalog)
                self._game_loop()
            elif choice == "New expedition":
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
                if phase == "hub":
                    self._hub()
                elif phase == "exploration":
                    self._exploration()
                elif phase == "discovery":
                    self._discovery()
                elif phase == "hazard":
                    self._hazard()
                elif phase == "objective":
                    self._objective()
                elif phase == "combat":
                    self._combat()
                elif phase == "event":
                    self._event()
                elif phase == "reward":
                    self._reward()
                elif phase == "service":
                    self._service()
                elif phase == "tutorial_complete":
                    self._notice(
                        "TRAINING COMPLETE",
                        "You navigated, repaired a broken formation, read coordinated intents, survived an enemy phase, chose a reward, and inspected the changed shared deck. Nothing from this lesson carries into a normal expedition.",
                    )
                    self.engine = None
                elif phase in {"victory", "defeat"}:
                    self._ending(phase)
                    self.engine = None
                else:
                    raise RuleError(f"unknown game phase: {phase}")
            except RuleError as exc:
                self.message = str(exc)

    def _hub(self) -> None:
        assert self.engine
        if not self._curated_squad_menu():
            self.engine = None
            return
        selected = 0
        roster = list(self.catalog.heroes.values())
        while self.engine and self.engine.state.phase == "hub":
            hero = roster[selected]
            selection = self.engine.state.hub_selection
            world_name = self.catalog.worlds[self.engine.state.world_id]["name"]
            self._begin(f"CREW THRESHOLD — {world_name.upper()}")
            self._put(2, 2, "Choose four crew. Manifest order becomes combat ranks 1 (front) to 4 (back).")
            visible_count = 15
            scroll = max(0, min(selected - visible_count // 2, len(roster) - visible_count))
            for shown, candidate in enumerate(roster[scroll:scroll + visible_count]):
                index = scroll + shown
                rank = selection.index(candidate["id"]) + 1 if candidate["id"] in selection else None
                marker = f"[{rank}]" if rank else "[ ]"
                line = f"{marker} {candidate['role']:<13} {candidate['name']}"
                attr = curses.A_REVERSE if index == selected else 0
                self._put(4 + shown, 3, line[:37], attr)

            self._put(3, 44, f"{hero['role'].upper()} // {hero['name']}", curses.A_BOLD | self._attr(1))
            self._draw_sprite(5, 55, self.catalog.art["heroes"][hero["id"]], curses.A_BOLD)
            complexity = "*" * hero["complexity"] + "." * (3 - hero["complexity"])
            self._put(
                11,
                44,
                f"ROLE {hero['combat_role'].upper()}  HP {hero['max_hp']}  COMPLEXITY {complexity}",
            )
            ranks = ",".join(str(rank) for rank in hero["preferred_ranks"])
            self._put(12, 44, f"PREFERRED RANKS {ranks}", curses.A_BOLD)
            detail_row = 14
            for label, field, limit in (
                ("SIGNATURE", "signature", 2),
                ("STRENGTH", "strength", 1),
                ("WEAKNESS", "weakness", 1),
            ):
                self._put(detail_row, 44, label, self._attr(2))
                detail_row += 1
                for line in textwrap.wrap(hero[field], 34)[:limit]:
                    self._put(detail_row, 44, line)
                    detail_row += 1
            builds = " / ".join(hero["builds"])
            self._put(21, 44, f"BUILDS {builds}"[:34], curses.A_DIM)
            ready = len(selection) == 4
            status = "READY TO DEPART" if ready else f"SELECT {4 - len(selection)} MORE"
            self._put(20, 3, status, self._attr(4 if ready else 2) | curses.A_BOLD)
            warnings = self.engine.party_warnings()
            if warnings:
                suffix = f" (+{len(warnings) - 1})" if len(warnings) > 1 else ""
                self._put(21, 3, f"! {warnings[0]}{suffix}"[:37], self._attr(3))
            self._footer("Up/Down browse  Space select  Left/Right rank  C class  D party deck  Enter depart")
            key = self._key()
            if key in (curses.KEY_UP, ord("k")):
                selected = (selected - 1) % len(roster)
            elif key in (curses.KEY_DOWN, ord("j")):
                selected = (selected + 1) % len(roster)
            elif key == ord(" "):
                self.engine.toggle_hub_crew(hero["id"])
            elif key in (curses.KEY_LEFT, ord("h")):
                self.engine.reorder_hub_crew(hero["id"], -1)
            elif key in (curses.KEY_RIGHT, ord("l")):
                self.engine.reorder_hub_crew(hero["id"], 1)
            elif key in (ord("c"), ord("C")):
                self._class_card_view(hero["id"])
            elif key in (ord("d"), ord("D")):
                self._hub_deck_view()
            elif key in (10, 13, curses.KEY_ENTER):
                self.engine.begin_expedition()
            elif key == 27:
                self.engine = None
                return

    def _curated_squad_menu(self) -> bool:
        assert self.engine
        squads = list(self.catalog.squads.values())
        selected = 0
        while True:
            self._begin("CREW APPROACH")
            self._put(2, 2, "Choose a prepared formation or open the complete roster.")
            choices = [squad["name"] for squad in squads] + ["Advanced custom selection"]
            for index, choice in enumerate(choices):
                marker = ">" if index == selected else " "
                attr = curses.A_REVERSE if index == selected else 0
                self._put(4 + index * 2, 3, f"{marker} {choice}"[:34], attr)
                if index < len(squads):
                    complexity = "*" * squads[index]["complexity"]
                    self._put(5 + index * 2, 5, f"complexity {complexity}", curses.A_DIM)

            if selected < len(squads):
                squad = squads[selected]
                row = 3
                self._put(row, 40, squad["name"].upper(), curses.A_BOLD | self._attr(1))
                row += 2
                for line in textwrap.wrap(squad["playstyle"], 37)[:3]:
                    self._put(row, 40, line)
                    row += 1
                row += 1
                formation = "  ".join(
                    f"R{rank} {self.catalog.heroes[hero_id]['role']}"
                    for rank, hero_id in enumerate(squad["formation"], 1)
                )
                for line in textwrap.wrap(formation, 37)[:2]:
                    self._put(row, 40, line, curses.A_BOLD)
                    row += 1
                for label, field in (
                    ("STRENGTH", "strength"),
                    ("WEAKNESS", "weakness"),
                    ("SIGNATURE", "signature"),
                ):
                    row += 1
                    self._put(row, 40, label, self._attr(2))
                    row += 1
                    for line in textwrap.wrap(squad[field], 37)[:2]:
                        self._put(row, 40, line)
                        row += 1
            else:
                self._put(4, 40, "ADVANCED CUSTOM SELECTION", curses.A_BOLD | self._attr(1))
                for offset, line in enumerate(
                    textwrap.wrap(
                        f"Browse all {len(self.catalog.heroes)} archetypes, inspect their cards, and assemble any four-person formation. Serious role and rank conflicts are warnings, not restrictions.",
                        37,
                    )[:6]
                ):
                    self._put(6 + offset, 40, line)
            self._footer("Up/Down choose  Enter continue  Esc title")
            key = self._key()
            if key in (curses.KEY_UP, ord("k")):
                selected = (selected - 1) % len(choices)
            elif key in (curses.KEY_DOWN, ord("j")):
                selected = (selected + 1) % len(choices)
            elif key in (10, 13, curses.KEY_ENTER):
                if selected < len(squads):
                    self.engine.select_curated_squad(squads[selected]["id"])
                return True
            elif key == 27:
                return False

    def _class_card_view(self, hero_id: str) -> None:
        cards = [CardInstance(card_id) for card_id, card in self.catalog.cards.items() if card["hero"] == hero_id]
        labels = [self._card_label(card) for card in cards]
        role = self.catalog.heroes[hero_id]["role"]
        self._menu(
            f"{role.upper()} CARD LIBRARY",
            labels,
            f"All {len(cards)} cards available to this archetype.",
            allow_cancel=True,
            view_only=True,
            preview_cards=cards,
            preview_notes=[self._card_tags_note(card) for card in cards],
        )

    def _hub_deck_view(self) -> None:
        assert self.engine
        cards: list[CardInstance] = []
        labels = []
        formation = []
        for rank, hero_id in enumerate(self.engine.state.hub_selection, 1):
            hero = self.catalog.heroes[hero_id]
            formation.append(f"R{rank} {hero['role']}")
            for card_id in hero["starter_deck"]:
                card = CardInstance(card_id)
                cards.append(card)
                labels.append(f"R{rank} {hero['role']} | {self._card_label(card)}")
        warnings = self.engine.party_warnings()
        warning_text = "No serious formation warnings." if not warnings else " ".join(
            f"! {warning}" for warning in warnings
        )
        body = (
            f"{' / '.join(formation) or 'No crew selected'}\n"
            f"{len(cards)} starting cards. {warning_text}"
        )
        if not cards:
            self._notice("COMBINED STARTER DECK", body)
            return
        self._menu(
            "COMBINED STARTER DECK",
            labels,
            body,
            allow_cancel=True,
            view_only=True,
            preview_cards=cards,
            preview_notes=[self._card_tags_note(card) for card in cards],
        )

    def _exploration(self) -> None:
        assert self.engine
        state = self.engine.state
        if state.tutorial and state.tutorial_stage == 0:
            self._notice(
                "TRAINING 1/5 — PATH CONFIRMATION",
                "The @ symbol is the crew and X is the destination cursor. Press Tab to select the nearby training contact, then Enter to confirm. Travel resolves one tile at a time and consumes light by weighted terrain cost; X or Escape cancels before the next tile.",
            )
            self.engine.advance_tutorial(0, 1)
        elif state.tutorial and state.tutorial_stage == 9:
            self._notice(
                "TRAINING 5/5 — DECK EVOLUTION",
                "The chosen technique now belongs to the shared party deck. Press D, inspect the resulting deck and card ownership, then close the deck view to finish training.",
            )
        cursor = (state.party_x, state.party_y)
        camera = cursor
        pending_click: tuple[int, int] | None = None
        cycle_index = -1
        while state.phase == "exploration":
            origin = self._render_exploration(cursor, focus=camera)
            key = self._key()
            movement = {
                curses.KEY_UP: (0, -1),
                curses.KEY_DOWN: (0, 1),
                curses.KEY_LEFT: (-1, 0),
                curses.KEY_RIGHT: (1, 0),
                ord("k"): (0, -1),
                ord("j"): (0, 1),
                ord("h"): (-1, 0),
                ord("l"): (1, 0),
            }
            if key in movement:
                delta_x, delta_y = movement[key]
                width = len(self.engine.world_tiles()[0])
                height = len(self.engine.world_tiles())
                cursor = (
                    max(0, min(width - 1, cursor[0] + delta_x)),
                    max(0, min(height - 1, cursor[1] + delta_y)),
                )
                left, top, viewport_width, viewport_height = origin
                if not (
                    left + 1 <= cursor[0] < left + viewport_width - 1
                    and top + 1 <= cursor[1] < top + viewport_height - 1
                ):
                    camera = cursor
                pending_click = None
            elif key in (10, 13, curses.KEY_ENTER):
                self._walk_to(cursor)
                camera = (state.party_x, state.party_y)
                pending_click = None
            elif key == curses.KEY_MOUSE:
                destination = self._mouse_destination(origin)
                if destination is not None:
                    if destination == pending_click:
                        self._walk_to(destination)
                        camera = (state.party_x, state.party_y)
                        pending_click = None
                    else:
                        cursor = destination
                        pending_click = destination
                        self.message = "Destination selected. Click it again or press Enter to move."
            elif key == 9:
                targets = self._exploration_targets()
                if targets:
                    cycle_index = (cycle_index + 1) % len(targets)
                    cursor = targets[cycle_index]
                    camera = cursor
                    pending_click = None
            elif key == ord(" "):
                cursor = (state.party_x, state.party_y)
                camera = cursor
                pending_click = None
            elif key in (ord("u"), ord("U")):
                self._supply_menu()
            elif key in (ord("d"), ord("D")):
                self._deck_view()
            elif key in (ord("i"), ord("I")):
                self._effects_view()
            elif key in (ord("b"), ord("B")):
                self._biome_view()
            elif key in (ord("p"), ord("P"), 27):
                self._pause()
                return
            elif key == ord("?"):
                self._help()

    def _render_exploration(
        self,
        cursor: tuple[int, int],
        focus: tuple[int, int] | None = None,
    ) -> tuple[int, int, int, int]:
        assert self.engine
        world = self.catalog.worlds[self.engine.state.world_id]
        self._begin(f"{world['name'].upper()} — TOP-DOWN EXPLORATION")
        self._resources(2)
        origin = self._world_map(self.MAP_ROW, cursor, focus)
        rows = self.screen.getmaxyx()[0]
        state = self.engine.state
        route = []
        if self.engine.is_walkable(*cursor):
            route = self.engine._find_path((state.party_x, state.party_y), cursor)
        route_length = self.engine.path_cost(route)
        maximum = self.engine.maximum_navigation_distance()
        reach = "READY" if route_length <= maximum and self.engine.is_walkable(*cursor) else "OUT OF REACH"
        biome = self.catalog.biomes[self.engine.current_biome()]["name"]
        mechanics = self.engine.biome_mechanics()
        access = f"ACCESS {self.engine.completed_objectives()}/{state.required_objectives}"
        core = "CORE OPEN" if self.engine.boss_unlocked() else "CORE SEALED"
        zone = self.engine.room().name
        self._put(
            rows - 4,
            2,
            (
                f"Crew ({state.party_x:03},{state.party_y:02})  "
                f"Target ({cursor[0]:03},{cursor[1]:02})  "
                f"Cost {route_length:2}/{maximum} {reach}  {access} {core}  "
                f"Biome: {biome}  Last: {zone}"
            )[: self.screen.getmaxyx()[1] - 3],
            self._attr(1 if reach == "READY" else 3),
        )
        self._put(
            rows - 3,
            2,
            f"@ crew X aim e/E/B foes K objective ^ hazard ?/C/W/$ sites | "
            f"T{mechanics['traversal']['cost']} {biome}"[
                : self.screen.getmaxyx()[1] - 3
            ],
            curses.A_DIM,
        )
        self._footer("Arrows aim Enter/2xclick go X/Esc/right-click stop Tab cycle B biome U supply")
        return origin

    def _world_map(
        self,
        row: int,
        cursor: tuple[int, int],
        focus: tuple[int, int] | None = None,
    ) -> tuple[int, int, int, int]:
        assert self.engine
        tiles = self.engine.world_tiles()
        screen_rows, screen_columns = self.screen.getmaxyx()
        viewport_width = min(screen_columns - 4, len(tiles[0]))
        viewport_height = min(screen_rows - row - 4, len(tiles))
        map_column = max(2, (screen_columns - viewport_width) // 2)
        focus_x, focus_y = focus or cursor
        left = max(0, min(len(tiles[0]) - viewport_width, focus_x - viewport_width // 2))
        top = max(0, min(len(tiles) - viewport_height, focus_y - viewport_height // 2))
        for offset in range(viewport_height):
            line = tiles[top + offset][left:left + viewport_width]
            self._put(row + offset, map_column, line, curses.A_DIM)

        state = self.engine.state
        overlays: list[tuple[int, int, str, int]] = []
        route: list[tuple[int, int]] = []
        if state.phase == "exploration" and self.engine.is_walkable(*cursor):
            route = self.engine._find_path((state.party_x, state.party_y), cursor)
            maximum = self.engine.maximum_navigation_distance()
            spent = 0
            for x, y in route:
                spent += self.engine.movement_cost(x, y)
                if spent > maximum:
                    break
                overlays.append((x, y, ":", curses.A_DIM))
        feature_symbols = {"start": "A", "event": "?", "camp": "C", "upgrade": "W", "cache": "$"}
        for room in state.rooms:
            if not room.resolved and room.kind in feature_symbols:
                x, y = self.engine.room_position(room.id)
                overlays.append((x, y, feature_symbols[room.kind], self._attr(2) | curses.A_BOLD))
        if not self.engine.boss_unlocked():
            boss = next(room for room in state.rooms if room.kind == "boss")
            x, y = self.engine.room_position(boss.id)
            overlays.append((x, y, "L", self._attr(3) | curses.A_BOLD))
        for objective in state.objectives:
            if not objective.completed:
                overlays.append((objective.x, objective.y, "K", self._attr(2) | curses.A_BOLD))
        for hazard in state.hazards:
            if not hazard.triggered and self.engine.is_hazard_visible(hazard):
                overlays.append((hazard.x, hazard.y, "^", self._attr(3) | curses.A_BOLD))
        for patrol in state.patrols:
            if not patrol.active or not self.engine.is_patrol_visible(patrol):
                continue
            kind = state.rooms[patrol.room_id].kind
            symbol = "B" if kind == "boss" else "E" if kind == "elite" else "e"
            overlays.append((patrol.x, patrol.y, symbol, self._attr(3) | curses.A_BOLD))
        pickup_symbols = {"boon": "+", "item": "*", "bargain": "!"}
        for pickup in state.pickups:
            if not pickup.resolved and not pickup.hidden:
                overlays.append(
                    (pickup.x, pickup.y, pickup_symbols[pickup.kind], self._attr(1) | curses.A_BOLD)
                )
        overlays.append((state.party_x, state.party_y, "@", self._attr(4) | curses.A_BOLD))
        cursor_symbol = "@" if cursor == (state.party_x, state.party_y) else "X"
        reachable = (
            self.engine.is_walkable(*cursor)
            and self.engine.path_cost(route) <= self.engine.maximum_navigation_distance()
        )
        if self.colour:
            cursor_attr = self._attr(7 if reachable else 5)
        else:
            cursor_attr = curses.A_REVERSE
        overlays.append((cursor[0], cursor[1], cursor_symbol, cursor_attr | curses.A_BOLD))
        for x, y, symbol, attribute in overlays:
            screen_x, screen_y = x - left + map_column, y - top + row
            if map_column <= screen_x < map_column + viewport_width and row <= screen_y < row + viewport_height:
                self._put(screen_y, screen_x, symbol, attribute)
        return left, top, viewport_width, viewport_height

    def _exploration_targets(self) -> list[tuple[int, int]]:
        assert self.engine
        state = self.engine.state
        targets = [
            (patrol.x, patrol.y)
            for patrol in state.patrols
            if patrol.active and self.engine.is_patrol_visible(patrol)
        ]
        targets.extend(
            (objective.x, objective.y)
            for objective in state.objectives
            if not objective.completed
        )
        targets.extend(
            (hazard.x, hazard.y)
            for hazard in state.hazards
            if not hazard.triggered and self.engine.is_hazard_visible(hazard)
        )
        targets.extend(
            self.engine.room_position(room.id)
            for room in state.rooms
            if not room.resolved and room.kind in {"event", "camp", "upgrade", "cache"}
        )
        targets.extend(
            (pickup.x, pickup.y)
            for pickup in state.pickups
            if not pickup.resolved and not pickup.hidden
        )
        party = (state.party_x, state.party_y)
        maximum = self.engine.maximum_navigation_distance()
        reachable = [
            tile
            for tile in set(targets)
            if self.engine.is_walkable(*tile)
            and self.engine.path_cost(self.engine._find_path(party, tile)) <= maximum
        ]
        return sorted(
            reachable,
            key=lambda tile: (self.engine.path_cost(self.engine._find_path(party, tile)), tile),
        )

    def _mouse_destination(self, origin: tuple[int, int, int, int]) -> tuple[int, int] | None:
        left, top, width, height = origin
        map_column = max(2, (self.screen.getmaxyx()[1] - width) // 2)
        try:
            _, mouse_x, mouse_y, _, buttons = curses.getmouse()
        except curses.error:
            return None
        clicked = (
            curses.BUTTON1_CLICKED
            | curses.BUTTON1_DOUBLE_CLICKED
            | curses.BUTTON1_TRIPLE_CLICKED
            | curses.BUTTON1_PRESSED
        )
        if not buttons & clicked or not (map_column <= mouse_x < map_column + width and self.MAP_ROW <= mouse_y < self.MAP_ROW + height):
            return None
        return left + mouse_x - map_column, top + mouse_y - self.MAP_ROW

    def _walk_to(self, destination: tuple[int, int]) -> None:
        assert self.engine
        try:
            path = self.engine.path_to(*destination)
        except RuleError as exc:
            self.message = str(exc)
            return
        if not path:
            self.message = "The crew is already there."
            return
        for x, y in path:
            if self.engine.state.phase != "exploration":
                break
            if self._route_cancel_requested():
                self.message = "Route cancelled. No further tile was resolved."
                break
            self.engine.step_exploration(x, y)
            self._render_exploration(destination, focus=(x, y))
            curses.napms(self.MOVE_FRAME_MS)

    def _route_cancel_requested(self) -> bool:
        try:
            self.screen.nodelay(True)
            key = self._key()
        finally:
            self.screen.nodelay(False)
        if key in (ord("x"), ord("X"), 27):
            return True
        if key != curses.KEY_MOUSE:
            return False
        try:
            _, _, _, _, buttons = curses.getmouse()
        except curses.error:
            return False
        right_click = getattr(curses, "BUTTON3_CLICKED", 0) | getattr(
            curses,
            "BUTTON3_PRESSED",
            0,
        )
        return bool(buttons & right_click)

    def _discovery(self) -> None:
        assert self.engine
        pickup = self.engine.current_pickup()
        if pickup.kind == "trap":
            message = self.engine.resolve_hidden_trap()
            self._notice("HIDDEN ANOMALY", message)
            return
        if pickup.kind == "item":
            item_id = str(pickup.payload["item_id"])
            item = self.catalog.items[item_id]
            self._menu(
                "SALVAGE CACHE",
                [f"Take {item['name']}"],
                item["description"],
                allow_cancel=False,
            )
            self.engine.resolve_item_pickup()
            return

        heroes = self.engine.living_heroes()
        hero_labels = [f"R{hero.rank} {hero.hero_class} — {hero.name}" for hero in heroes]
        selected_hero = self._menu(
            "CHOOSE A RECIPIENT",
            hero_labels,
            "Boons and curses belong to one crew member for the rest of this run.",
            allow_cancel=False,
        )
        assert selected_hero is not None
        hero = heroes[selected_hero]
        if pickup.kind == "boon":
            options = self.engine.boon_pickup_options(hero.id)
            labels = [
                f"{self.catalog.boons[boon_id]['name']} — "
                f"{self.catalog.boons[boon_id]['description']}"
                for boon_id in options
            ]
            picked = self._menu(
                "SIGNAL BENEDICTION",
                labels,
                f"Choose one boon for {hero.name}. Repeat copies stack.",
                allow_cancel=False,
            )
            assert picked is not None
            self.engine.resolve_boon_pickup(hero.id, options[picked])
            return

        options = self.engine.bargain_options(hero.id)
        labels = []
        for option in options:
            if option["reward_kind"] == "boon":
                reward = self.catalog.boons[str(option["reward_id"])]["name"]
            else:
                reward = f"{self.catalog.items[str(option['reward_id'])]['name']} x{option['copies']}"
            curse = self.catalog.curses[str(option["curse_id"])]
            labels.append(f"Take {reward} / suffer {curse['name']} — {curse['description']}")
        labels.append("Walk away")
        picked = self._menu(
            "ANOMALOUS BARGAIN",
            labels,
            f"Every offer binds its curse to {hero.name}.",
            allow_cancel=False,
        )
        assert picked is not None
        self.engine.resolve_bargain(hero.id, None if picked == len(options) else picked)

    def _hazard(self) -> None:
        assert self.engine
        hazard = self.engine.current_hazard()
        definition = self.engine.biome_mechanics(hazard.biome_id)["hazard"]
        biome = self.catalog.biomes[hazard.biome_id]["name"]
        self._notice(
            f"{biome.upper()} — {definition['name'].upper()}",
            definition["description"] + "\n\nThe confirmed route has stopped.",
        )
        self.engine.finish_hazard()

    def _objective(self) -> None:
        assert self.engine
        objective = self.engine.current_objective()
        definition = self.engine.biome_mechanics(objective.biome_id)["objective"]
        biome = self.catalog.biomes[objective.biome_id]["name"]
        safe = f"{definition['safe_label']} ({definition['safe_cost']} supply)"
        force = f"{definition['force_label']} (accept the consequence)"
        picked = self._menu(
            f"{biome.upper()} — ACCESS OBJECTIVE",
            [safe, force],
            definition["description"]
            + f"\n\nSecure any {self.engine.state.required_objectives} of the four biome signals. "
            + f"Current access: {self.engine.completed_objectives()}/{self.engine.state.required_objectives}.",
            allow_cancel=False,
        )
        assert picked is not None
        self.message = self.engine.resolve_objective("safe" if picked == 0 else "force")

    def _biome_view(self) -> None:
        assert self.engine
        biome = self.catalog.biomes[self.engine.current_biome()]
        mechanics = biome["mechanics"]
        objective = mechanics["objective"]
        body = (
            f"{biome['description']}\n\n"
            f"TRAVEL — {mechanics['traversal']['description']}\n"
            f"VISIBILITY — {mechanics['visibility']['description']}\n"
            f"PATROLS — {mechanics['patrol']['description']}\n"
            f"HAZARD: {mechanics['hazard']['name']} — {mechanics['hazard']['description']}\n"
            f"COMBAT: {mechanics['combat']['name']} — {mechanics['combat']['description']}\n"
            f"OBJECTIVE: {objective['name']} — {objective['description']}"
        )
        self._notice(biome["name"].upper(), body)

    def _combat(self) -> None:
        assert self.engine
        selected = 0
        while self.engine and self.engine.state.phase == "combat":
            self._tutorial_combat_lesson()
            state = self.engine.state
            selected = min(selected, max(0, len(state.hand) - 1))
            self._render_combat(selected)
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
                    target = self._target_selector(selected, targets)
                    if target is None:
                        continue
                enemies_before = self._enemy_flash_snapshot()
                heroes_before = self._hero_buff_snapshot()
                self.engine.play_card(selected, target)
                self._flash_damaged_enemies(enemies_before)
                self._flash_buffed_heroes(heroes_before)
            elif key in (ord("e"), ord("E")):
                enemies_before = self._enemy_flash_snapshot()
                self._skip_enemy_playback = False
                self.engine.end_turn(self._play_enemy_action)
                self._flash_damaged_enemies(enemies_before)
            elif key in (ord("i"), ord("I")):
                self._effects_view()
            elif key in (ord("c"), ord("C")):
                self._combat_hand_view()
            elif key in (ord("r"), ord("R")):
                self._crew_view()
            elif key in (ord("p"), ord("P"), 27):
                self._pause()
                return
            elif key == ord("?"):
                self._help()

    def _tutorial_combat_lesson(self) -> None:
        assert self.engine
        if not self.engine.state.tutorial:
            return
        stage = self.engine.state.tutorial_stage
        if stage == 2:
            self._notice(
                "TRAINING 2/5 — PARTY DECK AND RANKS",
                "All four crew share one hand and three energy. Every card still belongs to one specialist and lists the ranks where that owner can use it. The Engineer is stranded at R1 and the Warden is wounded at Death's Door, but both remain active. Inspect the hand with C and crew with R. Mag Boots can pull the Warden forward, restoring both preferred positions; Field Dressing+ can then heal and cleanse the Warden.",
            )
            self.engine.advance_tutorial(2, 3)
        elif stage == 4:
            self._notice(
                "TRAINING 3/5 — INTENTS AND SEQUENCING",
                "Enemy intents are frozen for this player turn. SET:MARKED prepares the displayed target; CASH:MARKED deals its larger conditional damage afterward. Killing the setup actor cancels its intent but does not retarget the other actor. Scan supplies your own MARKED setup and Arc Welder cashes it. Block absorbs health damage, but it clears at the next player turn.",
            )
            self.engine.advance_tutorial(4, 5)
        elif stage == 6:
            self._notice(
                "TRAINING 4/5 — ENEMY PHASE",
                "Enemy actions resolved front to back in separate frames. Those frames report the actor, frozen target, damage, movement, and statuses. F accelerates playback and Space skips its remaining frames without changing combat results. Forced movement can disable narrow cards, so preserve a movement tool or fallback action when the formation is exposed.",
            )
            self.engine.advance_tutorial(6, 8)

    def _combat_hand_view(self) -> None:
        assert self.engine
        cards = list(self.engine.state.hand)
        if not cards:
            self._notice("HAND INSPECTION", "The shared hand is empty.")
            return
        self._menu(
            "HAND INSPECTION",
            [self._card_label(card) for card in cards],
            "Cards remain owned by their named specialist. Dim cards are blocked by rank, stun, or energy on the combat screen.",
            allow_cancel=True,
            view_only=True,
            preview_cards=cards,
            preview_notes=[self._card_tags_note(card) for card in cards],
        )

    def _crew_view(self) -> None:
        assert self.engine
        lines = []
        for hero in sorted(self.engine.state.heroes, key=lambda actor: (not actor.alive, actor.rank)):
            definition = self.catalog.heroes[hero.id]
            state = "DEAD" if not hero.alive else "DEATH'S DOOR" if hero.deaths_door else f"{hero.hp}/{hero.max_hp} HP"
            statuses = ", ".join(f"{name} {amount}" for name, amount in hero.statuses.items()) or "none"
            preferred = ",".join(map(str, definition["preferred_ranks"]))
            lines.append(
                f"R{hero.rank} {hero.name} — {definition['role']} / {definition['combat_role']} — {state}, {hero.stress} stress, {hero.block} block. Preferred R{preferred}; statuses: {statuses}. {definition['signature']}"
            )
        self._notice("CREW INSPECTION", "\n\n".join(lines))

    def _render_combat(self, selected: int, selected_target: str | None = None) -> None:
        assert self.engine
        state = self.engine.state
        boons, curses_owned, items = self.engine.effect_counts()
        biome = self.catalog.biomes[self.engine.current_biome()]["name"]
        environment = self.engine.biome_mechanics()["combat"]["name"]
        self._begin(
            f"{biome.upper()} / {environment.upper()} — {self.engine.room().encounter_plan.upper()} — "
            f"ROUND {state.round} — ENERGY {state.energy} — B{boons} C{curses_owned} I{items}"
        )
        active_hero = None
        valid_targets: list[str] = []
        if state.hand:
            card = state.hand[selected]
            definition = self.engine.card_definition(card)
            active_hero = card.bound_hero_id if card.card_id in self.catalog.curses else definition["hero"]
            valid_targets = self.engine.valid_targets(selected)
        self._battlefield(2, active_hero, valid_targets, selected_target)
        self._intents(11, 2)
        first = max(0, min(selected, len(state.hand) - 5))
        for slot, card in enumerate(state.hand[first:first + 5]):
            index = first + slot
            definition = self.engine.card_definition(card)
            actor_id = card.bound_hero_id if card.card_id in self.catalog.curses else definition["hero"]
            actor = next(item for item in state.heroes if item.id == actor_id)
            legal = (
                card.card_id not in self.catalog.curses
                and actor.alive
                and actor.rank in definition["from_ranks"]
                and not actor.statuses.get("stun")
                and self.engine.card_cost(card) <= state.energy
            )
            self._draw_mini_card(
                13,
                self._combat_column(2 + slot * 15),
                card,
                index == selected,
                legal,
            )
        if not state.hand:
            self._put(17, 28, "HAND EMPTY — PRESS E", curses.A_DIM)
        footer = (
            "TARGET: Left/Right or H/L select on battlefield  Enter confirm  Esc cancel"
            if selected_target
            else "Left/Right card  Enter play  E enemy turn  C inspect card  R crew  I effects  P pause"
        )
        self._footer(footer)

    def _target_selector(self, hand_index: int, targets: list[str]) -> str | None:
        assert self.engine
        actors = {
            actor.id: actor
            for actor in self.engine.state.heroes + self.engine.state.enemies
        }
        ordered = sorted(
            targets,
            key=lambda actor_id: (
                43 + (actors[actor_id].rank - 1) * 9
                if actors[actor_id].side == "enemy"
                else 29 - (actors[actor_id].rank - 1) * 9
            ),
        )
        selected = 0
        while True:
            self._render_combat(hand_index, ordered[selected])
            key = self._key()
            if key in (curses.KEY_LEFT, curses.KEY_UP, ord("h"), ord("k")):
                selected = (selected - 1) % len(ordered)
            elif key in (curses.KEY_RIGHT, curses.KEY_DOWN, ord("l"), ord("j")):
                selected = (selected + 1) % len(ordered)
            elif key in (10, 13, curses.KEY_ENTER):
                return ordered[selected]
            elif key == 27:
                return None

    def _enemy_flash_snapshot(self) -> dict[str, tuple[int, int, list[str]]]:
        assert self.engine
        return {
            enemy.id: (
                enemy.hp,
                enemy.rank,
                self.catalog.art["enemies"][enemy.definition_id or enemy.id],
            )
            for enemy in self.engine.living_enemies()
        }

    def _hero_buff_snapshot(
        self,
    ) -> dict[str, tuple[int, int, int, int, frozenset[str], int, list[str]]]:
        assert self.engine
        return {
            hero.id: (
                hero.hp,
                hero.stress,
                hero.block,
                hero.guard_turns,
                frozenset(hero.statuses),
                hero.rank,
                self.catalog.art["heroes"][hero.id],
            )
            for hero in self.engine.living_heroes()
        }

    def _play_enemy_action(self, event: dict) -> None:
        if getattr(self, "_skip_enemy_playback", False):
            return
        self._render_combat(0)
        width = self.screen.getmaxyx()[1] - 4
        for row in range(11, 18):
            self._put(row, 2, " " * width)
        targets = ", ".join(event["target_labels"]) or "no target"
        combo = ""
        if event["setup"]:
            combo = " | SET " + "/".join(status.upper() for status in event["setup"])
        elif event["payoff"]:
            combo = " | CASH " + "/".join(status.upper() for status in event["payoff"])
        headline = (
            f"ENEMY ACTION {event['actor_name']} [R{event['actor_rank']}] — "
            f"{event['action']} -> {targets}{combo}"
        )
        self._put(11, 2, headline[:width], curses.A_REVERSE | curses.A_BOLD)
        change_text = " | ".join(event["changes"]) or "No visible state change."
        for offset, line in enumerate(textwrap.wrap(change_text, width)[:5]):
            self._put(13 + offset, 2, line, self._attr(3))
        speed = "FAST" if getattr(self, "_enemy_playback_fast", False) else "NORMAL"
        self._footer(f"Enemy playback {speed}  F toggle speed  Space skip remaining actions")
        delay = self.ENEMY_ACTION_FAST_MS if getattr(self, "_enemy_playback_fast", False) else self.ENEMY_ACTION_MS
        try:
            self.screen.timeout(delay)
            key = self._key()
        finally:
            self.screen.timeout(-1)
        if key == ord(" "):
            self._skip_enemy_playback = True
        elif key in (ord("f"), ord("F")):
            self._enemy_playback_fast = not getattr(self, "_enemy_playback_fast", False)

    def _flash_damaged_enemies(self, before: dict[str, tuple[int, int, list[str]]]) -> None:
        assert self.engine
        after = {enemy.id: enemy.hp for enemy in self.engine.state.enemies}
        damaged = [
            (rank, art, hp - after.get(enemy_id, hp))
            for enemy_id, (hp, rank, art) in before.items()
            if after.get(enemy_id, hp) < hp
        ]
        if not damaged:
            return
        attr = (self._attr(5) if self.colour else curses.A_REVERSE) | curses.A_BOLD
        for rank, art, damage in damaged:
            column = self._combat_column(43 + (rank - 1) * 9)
            self._draw_sprite(3, column, art, attr)
            label = f"-{damage}"
            self._put(5, column + max(0, (7 - len(label)) // 2), label, attr)
        self.screen.refresh()
        curses.napms(self.DAMAGE_FLASH_MS)

    def _flash_buffed_heroes(
        self,
        before: dict[str, tuple[int, int, int, int, frozenset[str], int, list[str]]],
    ) -> None:
        assert self.engine
        heroes = {hero.id: hero for hero in self.engine.living_heroes()}
        buffed: list[tuple[int, list[str], str]] = []
        for hero_id, (hp, stress, block, guard, statuses, rank, art) in before.items():
            hero = heroes.get(hero_id)
            if not hero:
                continue
            changes = []
            if hero.hp > hp:
                changes.append(f"H+{hero.hp - hp}")
            if hero.block > block:
                changes.append(f"B+{hero.block - block}")
            if hero.stress < stress:
                changes.append(f"S-{stress - hero.stress}")
            if hero.guard_turns > guard:
                changes.append("GUARD")
            changes.extend(status.upper() for status in hero.statuses.keys() - statuses)
            removed_statuses = statuses - hero.statuses.keys()
            if removed_statuses & {"marked", "stun", "vulnerable", "weak", "wound"}:
                changes.append("CLEANSE")
            if changes:
                buffed.append((rank, art, "/".join(changes)[:7]))
        if not buffed:
            return
        attr = (self._attr(6) if self.colour else curses.A_REVERSE) | curses.A_BOLD
        for rank, art, label in buffed:
            column = self._combat_column(29 - (rank - 1) * 9)
            self._draw_sprite(3, column, art, attr)
            self._put(5, column + max(0, (7 - len(label)) // 2), label, attr)
        self.screen.refresh()
        curses.napms(self.DAMAGE_FLASH_MS)

    def _battlefield(
        self,
        row: int,
        active_hero: str | None,
        valid_targets: list[str],
        selected_target: str | None = None,
    ) -> None:
        assert self.engine
        self._put(row, self._combat_column(2), "CREW  < BACK     FORMATION     FRONT >", curses.A_BOLD)
        self._put(row, self._combat_column(43), "HOSTILES < FRONT   FORMATION  BACK >", curses.A_BOLD)
        self._put(row + 1, self._combat_column(39), "||", curses.A_BOLD)
        fallen = [hero for hero in self.engine.state.heroes if not hero.alive]
        fallen_by_rank = {4 - index: hero for index, hero in enumerate(fallen)}
        for rank in range(1, 5):
            hero = next((item for item in self.engine.living_heroes() if item.rank == rank), None)
            enemy = next((item for item in self.engine.living_enemies() if item.rank == rank), None)
            if hero:
                column = self._combat_column(29 - (rank - 1) * 9)
                attr = self._hp_attr(hero.hp, hero.max_hp)
                if hero.id == active_hero or hero.id in valid_targets or "all_allies" in valid_targets:
                    attr |= curses.A_BOLD
                if hero.id == selected_target:
                    attr |= curses.A_REVERSE
                self._draw_sprite(row + 1, column, self.catalog.art["heroes"][hero.id], attr)
                if hero.id == selected_target:
                    self._target_brackets(row + 3, column, attr)
                self._put(row + 6, column, f"R{rank} {hero.hero_class[:4].upper():4}", attr)
                hp = "DD" if hero.deaths_door else f"{hero.hp:02}"
                self._put(row + 7, column, f"H{hp}/{hero.max_hp:02}", attr)
                self._put(row + 8, column, f"S{hero.stress:02} B{hero.block:02}", attr)
            elif rank in fallen_by_rank:
                dead = fallen_by_rank[rank]
                column = self._combat_column(29 - (rank - 1) * 9)
                self._draw_sprite(row + 1, column, self.catalog.art["heroes"][dead.id], curses.A_DIM)
                self._put(row + 6, column, f"-- {dead.hero_class[:4].upper():4}", curses.A_DIM)
                self._put(row + 7, column, "  DEAD ", curses.A_BOLD | self._attr(3))
            if enemy:
                column = self._combat_column(43 + (rank - 1) * 9)
                attr = self._hp_attr(enemy.hp, enemy.max_hp)
                if enemy.id in valid_targets or "all_enemies" in valid_targets:
                    attr |= curses.A_BOLD
                if enemy.id == selected_target:
                    attr |= curses.A_REVERSE
                art_id = enemy.definition_id or enemy.id
                self._draw_sprite(row + 1, column, self.catalog.art["enemies"][art_id], attr)
                if enemy.id == selected_target:
                    self._target_brackets(row + 3, column, attr)
                short_name = "".join(word[0] for word in enemy.name.split()).upper()[:4]
                self._put(row + 6, column, f"R{rank} {short_name:4}", attr)
                self._put(row + 7, column, f"H{enemy.hp:02}/{enemy.max_hp:02}", attr)
                status = "".join(name[0].upper() for name in enemy.statuses)[:3]
                self._put(row + 8, column, f"B{enemy.block:02} {status:3}", attr)

    def _combat_column(self, column: int) -> int:
        return column + max(0, (self.screen.getmaxyx()[1] - self.MIN_COLS) // 2)

    def _intents(self, row: int, max_lines: int) -> None:
        assert self.engine
        descriptions: list[tuple[str, str]] = []
        status_marks = {
            "marked": "MK",
            "stun": "ST",
            "vulnerable": "VU",
            "weak": "WK",
            "wound": "WN",
            "focus": "FO",
            "dodge": "DG",
            "riposte": "RP",
        }
        formation_exploits = set().union(
            *(
                self.engine._definition_exploit_statuses(
                    self.catalog.enemies[enemy.definition_id or enemy.id]
                )
                for enemy in self.engine.living_enemies()
            )
        ) if self.engine.living_enemies() else set()
        for intent in self.engine.state.intents:
            enemy = next((actor for actor in self.engine.living_enemies() if actor.id == intent["enemy_id"]), None)
            if not enemy:
                continue
            actions = self.catalog.enemies[enemy.definition_id or enemy.id]["actions"]
            action = next(item for item in actions if item["name"] == intent["action"])
            effects = []
            for effect in action["effects"]:
                if effect["op"] == "status":
                    status = status_marks.get(effect["status"], effect["status"][:2].upper())
                    effects.append(f"+{status}{effect.get('amount', '')}")
                elif effect["op"] == "move":
                    direction = "P" if effect.get("amount", 0) > 0 else "L"
                    effects.append(f"{direction}{abs(effect.get('amount', 0))}")
                elif effect["op"] == "damage" and effect.get("bonus_status"):
                    base = self.engine._outgoing_damage(enemy, int(effect.get("amount", 0)))
                    maximum = self.engine._outgoing_damage(
                        enemy,
                        int(effect.get("amount", 0)) + int(effect.get("bonus", 0)),
                    )
                    status = status_marks.get(
                        effect["bonus_status"], effect["bonus_status"][:2].upper()
                    )
                    effects.append(f"D{base}/{maximum}:{status}")
                else:
                    label = {
                        "damage": "D",
                        "stress": "S",
                        "block": "B",
                        "heal": "H",
                        "guard": "G",
                    }.get(effect["op"], effect["op"])
                    amount = effect.get("amount", "")
                    if effect["op"] == "damage":
                        amount = self.engine._outgoing_damage(enemy, int(amount))
                    effects.append(f"{label}{amount}")
            setup = self.engine._action_setup_statuses(action) & formation_exploits
            exploit = self.engine._action_exploit_statuses(action)
            combo = ""
            if setup:
                marks = "/".join(status_marks.get(item, item[:2].upper()) for item in sorted(setup))
                combo = f" SET:{marks}"
            elif exploit:
                marks = "/".join(status_marks.get(item, item[:2].upper()) for item in sorted(exploit))
                combo = f" CASH:{marks}"
            labels = intent.get("target_labels", [action["target"]])
            target = ",".join(
                "ALL" if label == "ALL CREW" else label.removeprefix("R").replace(" ", "")
                for label in labels
            )
            condition = {
                "weakest_enemy": "<LO",
                "weakest_ally": "<ALLY",
                "stressed": "<ST",
                "deaths_door": "<DD",
                "marked": "<MK",
                "wounded": "<WN",
            }.get(intent.get("target_rule", action["target"]), "")
            actor_mark = "".join(word[0] for word in enemy.name.split()).upper()[:4]
            prefix = f"R{intent['enemy_rank']}{actor_mark}>{target}{condition} "
            suffix = f" {','.join(effects)}{combo}"
            descriptions.append(((prefix + action["name"] + suffix).strip(), action["name"]))

        width = max(20, (self.screen.getmaxyx()[1] - 6) // 2)
        for index, (description, action_name) in enumerate(descriptions[: max_lines * 2]):
            line = index // 2
            column = 2 + (index % 2) * (width + 2)
            if len(description) > width:
                overflow = len(description) - width
                shortened = action_name[: max(3, len(action_name) - overflow - 1)] + "~"
                description = description.replace(action_name, shortened, 1)
            self._put(row + line, column, description[:width], self._attr(2))

    def _event(self) -> None:
        assert self.engine and self.engine.state.current_event
        event = self.catalog.events[self.engine.state.current_event]
        choices = [item["label"] for item in event["choices"]]
        picked = self._menu(event["name"].upper(), choices, event["text"], allow_cancel=False)
        self.engine.choose_event(picked)

    def _reward(self) -> None:
        assert self.engine
        if self.engine.state.tutorial and self.engine.state.tutorial_stage == 7:
            self._notice(
                "TRAINING — CARD REWARD",
                "A reward can strengthen an existing setup/payoff line, cover a weakness, open a new direction, duplicate a key card, or be skipped to keep the deck lean. The labels describe tradeoffs; they do not identify a correct choice.",
            )
            self.engine.advance_tutorial(7, 8)
        choices = []
        notes = []
        for card_id in self.engine.state.rewards:
            card = self.catalog.cards[card_id]
            kind, note = self.engine.reward_context(card_id)
            choices.append(
                f"[{kind}] {card['name']} ({self.catalog.heroes[card['hero']]['role']})"
            )
            notes.append(note)
        choices.append("Skip reward")
        notes.append("Keep the deck unchanged. No consolation reward is granted.")
        previews = [CardInstance(card_id) for card_id in self.engine.state.rewards] + [None]
        picked = self._menu(
            "RECOVERED TECHNIQUE",
            choices,
            "Choose a commitment, cover a weakness, open a new line, or keep the deck lean.",
            allow_cancel=False,
            preview_cards=previews,
            preview_notes=notes,
        )
        self.engine.choose_reward(None if picked == len(choices) - 1 else picked)

    def _service(self) -> None:
        assert self.engine
        service_type = self.engine.state.service_type
        choices = ["Upgrade a card", "Remove a card"]
        if service_type == "camp":
            choices.insert(0, "Recover: heal 7 and reduce 10 stress")
            if self.engine.state.curses:
                choices.insert(1, "Treat one curse (2 supplies)")
        else:
            choices.insert(1, "Transform a card")
        picked = self._menu("CREW QUARTERS" if service_type == "camp" else "WORKSHOP", choices, "The room can be used once.", allow_cancel=False)
        action = choices[picked]
        if action.startswith("Recover"):
            self.engine.service("recover")
            return
        if action.startswith("Treat"):
            curses_owned = [
                (hero_id, curse_id, count)
                for hero_id, effects in self.engine.state.curses.items()
                for curse_id, count in effects.items()
            ]
            labels = [
                f"{self.catalog.heroes[hero_id]['name']}: "
                f"{self.catalog.curses[curse_id]['name']} x{count} — "
                f"{self.catalog.curses[curse_id]['description']}"
                for hero_id, curse_id, count in curses_owned
            ]
            selected = self._menu(
                "CURSE TREATMENT",
                labels,
                f"Supplies {self.engine.state.supplies}. Remove one stack and its bound card, if any.",
                allow_cancel=False,
            )
            assert selected is not None
            hero_id, curse_id, _ = curses_owned[selected]
            self.engine.service("treat", hero_id=hero_id, curse_id=curse_id)
            return
        eligible = [
            index for index, card in enumerate(self.engine.state.deck)
            if action.startswith("Remove")
            or action.startswith("Transform") and card.card_id not in self.catalog.curses
            or (not card.upgraded and card.card_id not in self.catalog.curses)
        ]
        if not eligible:
            self.message = "No cards are eligible for that service."
            return
        labels = [self._card_label(self.engine.state.deck[index]) for index in eligible]
        previews = [self.engine.state.deck[index] for index in eligible]
        selected = self._menu(
            action.upper(),
            labels,
            allow_cancel=False,
            preview_cards=previews,
            preview_notes=[self._card_tags_note(card) for card in previews],
        )
        card_index = eligible[selected]
        if action.startswith("Transform"):
            options = self.engine.transformation_options(card_index)
            option_cards = [CardInstance(card_id) for card_id in options]
            source = self.engine.state.deck[card_index]
            source_definition = self.catalog.cards[source.card_id]
            source_tags = ", ".join(sorted(self.engine.card_tags(source.card_id))).upper()
            comparisons = [
                self.engine.transformation_comparison(source.card_id, card_id)
                for card_id in options
            ]
            labels = []
            notes = []
            for comparison in comparisons:
                old_cost, new_cost = comparison["cost"]
                old_ranks, new_ranks = comparison["ranks"]
                labels.append(
                    f"{comparison['destination']} | E{old_cost}->{new_cost} "
                    f"R{','.join(map(str, old_ranks))}->R{','.join(map(str, new_ranks))}"
                )
                old_effects, new_effects = comparison["effects"]
                added = ", ".join(comparison["added_tags"]) or "none"
                removed = ", ".join(comparison["removed_tags"]) or "none"
                notes.append(
                    f"EFFECTS {'+'.join(old_effects)} -> {'+'.join(new_effects)}\n"
                    f"TAGS + {added}\nTAGS - {removed}"
                )
            transformed = self._menu(
                "CHOOSE A NEW TECHNIQUE",
                labels,
                f"SOURCE: {source_definition['name']} | E{source_definition['cost']} | "
                f"FROM R{','.join(map(str, source_definition['from_ranks']))}\n"
                f"TAGS: {source_tags}",
                allow_cancel=False,
                preview_cards=option_cards,
                preview_notes=notes,
            )
            assert transformed is not None
            self.engine.service(
                "transform",
                card_index,
                replacement_id=options[transformed],
            )
        else:
            self.engine.service("remove" if action.startswith("Remove") else "upgrade", card_index)

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
            preview_notes=[self._card_tags_note(card) for card in self.engine.state.deck],
        )
        if self.engine.state.tutorial and self.engine.state.tutorial_stage == 9:
            self.engine.complete_tutorial()

    def _effects_view(self) -> None:
        assert self.engine
        entries: list[tuple[str, str]] = []
        for hero in self.engine.state.heroes:
            for boon_id, count in self.engine.state.boons.get(hero.id, {}).items():
                entries.append(
                    (
                        f"+ {hero.name}: {self.catalog.boons[boon_id]['name']} x{count}",
                        self.engine.effect_description("boon", boon_id, count),
                    )
                )
            for curse_id, count in self.engine.state.curses.get(hero.id, {}).items():
                entries.append(
                    (
                        f"! {hero.name}: {self.catalog.curses[curse_id]['name']} x{count}",
                        self.engine.effect_description("curse", curse_id, count),
                    )
                )
        for item_id, count in self.engine.state.items.items():
            entries.append(
                (
                    f"* PARTY: {self.catalog.items[item_id]['name']} x{count}",
                    self.engine.effect_description("item", item_id, count),
                )
            )
        if not entries:
            entries = [("No run effects acquired", "Explore visible signals and salvage to build the run.")]
        boon_count, curse_count, item_count = self.engine.effect_counts()
        selected = 0
        scroll = 0
        while True:
            self._begin("RUN EFFECTS")
            self._put(2, 3, f"Boons {boon_count}  Curses {curse_count}  Item stacks {item_count}")
            available = max(1, self.screen.getmaxyx()[0] - 7)
            if selected < scroll:
                scroll = selected
            elif selected >= scroll + available:
                scroll = selected - available + 1
            for shown, (label, _) in enumerate(entries[scroll:scroll + available]):
                index = scroll + shown
                attr = curses.A_REVERSE if index == selected else 0
                self._put(4 + shown, 3, ("> " if index == selected else "  ") + label[:32], attr)
            detail_width = max(20, self.screen.getmaxyx()[1] - 42)
            self._put(4, 41, entries[selected][0][:detail_width], curses.A_BOLD)
            for offset, line in enumerate(textwrap.wrap(entries[selected][1], detail_width)[:10]):
                self._put(6 + offset, 41, line)
            self._footer("Up/Down inspect  Esc/Enter back")
            key = self._key()
            if key in (curses.KEY_UP, ord("k")):
                selected = (selected - 1) % len(entries)
            elif key in (curses.KEY_DOWN, ord("j")):
                selected = (selected + 1) % len(entries)
            elif key in (10, 13, curses.KEY_ENTER, 27):
                return

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
            "The Overseer is silent. The crew escapes before the dead world can wake again."
            if phase == "victory"
            else "The last voice drops from the comms. No one remains to finish the mission."
        )
        self._notice(title, body + f"\n\nSeed: {self.engine.state.seed}")

    def _help(self) -> None:
        text = (
            "Explore the current world from above and reach its Overseer Core. Aim the X cursor with arrows or "
            "hjkl, then press Enter to auto-walk there. A first left-click selects and highlights a tile; "
            "click it again or press Enter to confirm. One order has limited reach; Survey Relays extend it. "
            "Tab cycles visible points of interest and Space recenters on the crew. Patrols move according "
            "to their biome cadence, and contact opens combat. Travel cost drains light; darkness adds stress, increases "
            "surprise attacks, and offers a fourth card reward. In combat, spend shared "
            "energy on cards whose specialist occupies a valid rank. Enemy intents are shown before they act.\n\n"
            "Visible +, *, and ! discoveries grant hero-bound boons, party-wide stackable items, or risky "
            "bargains. Hidden anomalies inflict curses when stepped on. Curse cards trigger when drawn and "
            "cannot be played. Press I during exploration or combat to inspect every active stack and its "
            "current scaled value.\n\n"
            "Each seed selects one of six world layouts and four of eleven biome types. Biomes alter travel "
            "cost, hazard visibility, patrol behavior, combat conditions, and recovery opportunities as well "
            "as formations. Press B in exploration for the current biome rules. Four K sites offer seeded "
            "access objectives; secure any two by a safe supply procedure or a dangerous forced procedure "
            "to open the L-marked Overseer Core. Specialist cards list an "
            "affinity biome and gain extra damage, block, healing, or stress relief while used there.\n\n"
            "At zero HP a crew member reaches Death's Door. Further damage may kill them permanently; "
            "their cards leave the shared deck, but survivors continue until a full-party wipe. "
            "At 100 stress they gain an affliction; reaching 100 again causes collapse. Supplies heal, calm, "
            "or restore light. Camps recover crew, modify one card, or remove one curse for 2 supplies.\n\n"
            "Controls: arrows or hjkl navigate, Enter confirms, X/Escape cancels an active route, right-click "
            "also cancels it, E ends a combat turn, U uses a supply, B views biome rules, D views the deck, "
            "C inspects the selected combat card, R inspects crew, I views effects, P pauses, and ? opens this page. "
            "During enemy-action frames, F toggles fast playback and Space skips the remaining presentation; "
            "neither key skips enemy game actions. Mouse input otherwise stops at exploration routing."
        )
        self._notice("HOW TO PLAY", text)

    def _resources(self, row: int) -> None:
        assert self.engine
        state = self.engine.state
        self._put(row, 2, f"Seed {state.seed}   Light {state.light:3}/100   Supplies {state.supplies}", self._attr(2))
        crew = "  ".join(
            f"R{h.rank} {h.hero_class[:4].upper()} {h.hp}/{h.max_hp} {h.stress}s"
            if h.alive else f"-- {h.hero_class[:4].upper()} DEAD"
            for h in sorted(state.heroes, key=lambda actor: (not actor.alive, actor.rank, actor.id))
        )
        self._put(row + 1, 2, crew)
        self._put(row + 2, 2, self.engine.compact_effect_summary()[: self.screen.getmaxyx()[1] - 3], curses.A_DIM)

    def _card_label(self, card: CardInstance) -> str:
        assert self.engine
        definition = self.engine.card_definition(card)
        plus = "+" if card.upgraded else ""
        if card.card_id in self.catalog.curses:
            hero = self.catalog.heroes.get(card.bound_hero_id or "", {}).get("name", "Unbound")
            return f"{definition['name']} (CURSE / {hero}) — {definition['description']}"
        resonance = ""
        if definition.get("biome"):
            resonance = f" [{self.catalog.biomes[definition['biome']]['name']} +{definition['biome_bonus']}]"
        description = (
            definition.get("upgrade_description", definition["description"])
            if card.upgraded
            else definition["description"]
        )
        return f"{definition['name']}{plus} ({self.catalog.heroes[definition['hero']]['role']}){resonance} — {description}"

    def _card_tags_note(self, card: CardInstance) -> str:
        if card.card_id in self.catalog.curses:
            return "TAGS: CURSE, UNPLAYABLE"
        definition = self.catalog.cards[card.card_id]
        return "TAGS: " + ", ".join(tag.upper() for tag in definition["tags"])

    def _draw_sprite(self, row: int, column: int, lines: list[str], attr: int = 0) -> None:
        for offset, line in enumerate(lines):
            self._put(row + offset, column, line.ljust(7), attr)

    def _target_brackets(self, row: int, column: int, attr: int) -> None:
        self._put(row, column - 1, ">", attr | curses.A_BOLD)
        self._put(row, column + 7, "<", attr | curses.A_BOLD)

    def _draw_card(self, row: int, column: int, card: CardInstance) -> None:
        lines = self._card_lines(card)
        for offset, line in enumerate(lines):
            attr = curses.A_BOLD | self._attr(1) if offset in {0, 1, len(lines) - 2, len(lines) - 1} else 0
            self._put(row + offset, column, line, attr)

    def _card_lines(self, card: CardInstance) -> list[str]:
        assert self.engine
        definition = self.engine.card_definition(card)
        width = 22
        inside = width - 2

        def framed(text: str = "") -> str:
            return "|" + text[:inside].ljust(inside) + "|"

        is_curse = card.card_id in self.catalog.curses
        plus = "+" if card.upgraded else ""
        cost = "X" if is_curse else (
            definition["cost"] if self.engine.state.phase == "hub" else self.engine.card_cost(card)
        )
        title = f"{definition['name'].upper()}{plus}"
        hero_id = card.bound_hero_id if is_curse else definition["hero"]
        role = f"CURSE/{self.catalog.heroes.get(hero_id or '', {}).get('role', 'UNBOUND')}" if is_curse else self.catalog.heroes[hero_id]["role"]
        role = role.upper()
        mark = self.catalog.art["curse_card_mark"] if is_curse else self.catalog.art["card_marks"][hero_id]
        glyph = self.catalog.art["curse_card_glyph"] if is_curse else self.catalog.art["card_glyphs"][hero_id]
        ranks = "--" if is_curse else ",".join(str(rank) for rank in definition["from_ranks"])
        target = "unplayable" if is_curse else definition["target"].replace("_", " ")
        if not is_curse and definition.get("target_ranks"):
            target += " " + ",".join(str(rank) for rank in definition["target_ranks"])
        description_text = (
            definition.get("upgrade_description", definition["description"])
            if card.upgraded
            else definition["description"]
        )
        if not is_curse and definition.get("biome"):
            biome_name = self.catalog.biomes[definition["biome"]]["name"]
            description_text = (
                f"{biome_name}: +{definition['biome_bonus']} potency. {description_text}"
            )
        description = textwrap.wrap(description_text, inside - 2)[:4]
        description += [""] * (4 - len(description))
        border = "+" + "-" * inside + "+"
        corners = f"{cost}" + " " * (inside - len(str(cost)) - len(mark)) + mark
        lower_corners = mark + " " * (inside - len(str(cost)) - len(mark)) + f"{cost}"
        return [
            border,
            framed(corners),
            framed(title.center(inside)),
            framed(role.center(inside)),
            framed(self._compact_card_tags(definition) if not is_curse else "TAGS CURSE"),
            framed(glyph[0].center(inside)),
            framed(glyph[1].center(inside)),
            framed(glyph[2].center(inside)),
            framed(),
            framed(f"FROM {ranks}"),
            framed(f"TARGET: {target}"),
            framed(description[0]),
            framed(description[1]),
            framed(description[2]),
            framed(description[3]),
            framed(lower_corners),
            border,
        ]

    @staticmethod
    def _compact_card_tags(definition: dict) -> str:
        def compact(tag: str) -> str:
            prefix, separator, value = tag.partition(":")
            if prefix == "setup" and separator:
                return f"SET-{value[:4].upper()}"
            if prefix == "payoff" and separator:
                return f"USE-{value[:4].upper()}"
            if prefix == "status" and separator:
                return value.upper()
            if prefix == "affinity" and separator:
                return f"@{value[:5].upper()}"
            return tag.replace("stress_", "S-").replace("displacement", "MOVE").upper()

        tags = sorted(
            definition["tags"],
            key=lambda tag: (not tag.startswith(("setup:", "payoff:", "status:")), tag),
        )
        return "TAGS " + "/".join(compact(tag) for tag in tags)

    def _draw_mini_card(
        self,
        row: int,
        column: int,
        card: CardInstance,
        selected: bool,
        legal: bool,
    ) -> None:
        attribute = curses.A_REVERSE | curses.A_BOLD if selected else (0 if legal else curses.A_DIM)
        for offset, line in enumerate(self._mini_card_lines(card)):
            self._put(row + offset, column, line, attribute)

    def _mini_card_lines(self, card: CardInstance) -> list[str]:
        assert self.engine
        definition = self.engine.card_definition(card)
        width = 14
        inside = width - 2

        def framed(text: str = "") -> str:
            return "|" + text[:inside].ljust(inside) + "|"

        is_curse = card.card_id in self.catalog.curses
        cost = "X" if is_curse else (
            definition["cost"] if self.engine.state.phase == "hub" else self.engine.card_cost(card)
        )
        hero_id = card.bound_hero_id if is_curse else definition["hero"]
        mark = self.catalog.art["curse_card_mark"] if is_curse else self.catalog.art["card_marks"][hero_id]
        plus = "+" if card.upgraded else ""
        title = f"{definition['name'].upper()}{plus}"
        glyph = self.catalog.art["curse_card_glyph"] if is_curse else self.catalog.art["card_glyphs"][hero_id]
        target = "UNPLAYABLE" if is_curse else definition["target"].replace("all_enemies", "all foes").replace("all_allies", "all crew")
        description = textwrap.wrap(definition["description"], inside)[:2]
        description += [""] * (2 - len(description))
        corners = f"{cost}" + " " * (inside - len(str(cost)) - len(mark)) + mark
        border = "+" + "-" * inside + "+"
        return [
            border,
            framed(corners),
            framed(title),
            framed(glyph[0].center(inside)),
            framed(glyph[1].center(inside)),
            framed(glyph[2].center(inside)),
            framed(target.upper()),
            framed(description[0]),
            framed(description[1]),
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
        preview_notes: list[str] | None = None,
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
            if preview_notes:
                note_width = max(20, self.screen.getmaxyx()[1] - 48)
                lines = [
                    wrapped
                    for paragraph in preview_notes[selected].splitlines()
                    for wrapped in (textwrap.wrap(paragraph, note_width) or [""])
                ]
                for offset, line in enumerate(lines[:3]):
                    self._put(20 + offset, 45, line, curses.A_DIM)
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
        width = max(20, self.screen.getmaxyx()[1] - 6)
        lines = [
            line
            for paragraph in body.splitlines()
            for line in (textwrap.wrap(paragraph, width) or [""])
        ]
        scroll = 0
        while True:
            self._begin(title)
            available = max(1, self.screen.getmaxyx()[0] - 5)
            scroll = max(0, min(scroll, max(0, len(lines) - available)))
            for offset, line in enumerate(lines[scroll:scroll + available]):
                self._put(3 + offset, 3, line)
            if len(lines) <= available:
                footer = "Press any key"
            else:
                end = min(len(lines), scroll + available)
                footer = f"Up/Down or PgUp/PgDn scroll  Home/End jump  Enter/Esc close  {scroll + 1}-{end}/{len(lines)}"
            self._footer(footer)
            key = self._key()
            if len(lines) <= available:
                return
            if key in (curses.KEY_UP, ord("k")):
                scroll -= 1
            elif key in (curses.KEY_DOWN, ord("j")):
                scroll += 1
            elif key == curses.KEY_PPAGE:
                scroll -= available
            elif key == curses.KEY_NPAGE:
                scroll += available
            elif key == curses.KEY_HOME:
                scroll = 0
            elif key == curses.KEY_END:
                scroll = len(lines) - available
            elif key in (10, 13, curses.KEY_ENTER, 27):
                return

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
