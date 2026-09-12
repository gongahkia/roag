"""Original-style dungeon viewport and ranked card table inside Jomon."""

from __future__ import annotations

import curses
from pathlib import Path
import textwrap

from .content import load_catalog
from .engine import WALKABLE_TILES
from .expedition import (
    MAX_ROUNDS, ORDER_TICKS, ORDERS_PER_TURN, _ai_destination, _biome_at,
    _card_cost, _card_id, _card_upgraded, _distance, _file_position, _position, _team, choose_reward,
    doctrine_compatible, end_turn, engage_if_touching, finish_match, infusion_compatible,
    move_to, patron_turn, path_to, play_card,
    start_match, valid_targets,
)
from .office_content import OFFICE_BIOMES, OFFICE_ROLES, OFFICE_WORLDS, office_catalog
from .tabletop import collection_for, patrons
from .tabletop_ui import _draw_editor, _draw_lobby
from .ui import TerminalUI


def _office_sprite(role: str) -> list[str]:
    initials = "".join(word[0] for word in OFFICE_ROLES[role].split())[:2].upper().ljust(2)
    return [" .---. ", " |o o| ", f" |{initials} | ", " /|_|\\ ", " /   \\ "]


class ExpeditionUI(TerminalUI):
    def __init__(self, screen: curses.window, state):
        super().__init__(screen, load_catalog(), Path("/dev/null"), lambda: None)
        self.jomon_state = state
        self.match = state.tabletop["active_match"]
        self.cursor = _position(self.match, 0) if self.match else (5, 17)
        self.selected_card = 0
        self.site = 0

    def _render_map(self, moving: tuple[int, int] | None = None) -> None:
        match = self.match
        assert match is not None
        team = _team(match, 0)
        enemy = _team(match, 1)
        title = f"DULLEST DUNGEON  /  COMPANY OF NECESSARY COPIES  /  {OFFICE_WORLDS[match['world_id']].upper()}"
        self._begin(title)
        round_label = f"OT {match['round'] - MAX_ROUNDS}/4" if match["round"] > MAX_ROUNDS else f"ROUND {match['round']}/{MAX_ROUNDS}"
        self._put(1, 2, f"{round_label}  APPROVAL {match['scores'][0]}:{match['scores'][1]}  VS {match.get('patron_name', 'PATRON')[:20]}  ROUTES {ORDERS_PER_TURN - team['orders']}/{ORDERS_PER_TURN}", curses.A_BOLD)
        biome = _biome_at(match, *team["position"])
        self._put(2, 2, f"{OFFICE_BIOMES[biome].upper()}  LIGHT {team['light']}  SUPPLIES {team['supplies']}  BOONS {len(team['boons'])}  ITEMS {sum(team['items'].values())}")
        rows, columns = self.screen.getmaxyx()
        viewport_width = min(columns - 4, len(match["board"][0]))
        viewport_height = min(rows - 9, len(match["board"]))
        map_column = max(2, (columns - viewport_width) // 2)
        focus_x, focus_y = self.cursor
        left = max(0, min(len(match["board"][0]) - viewport_width, focus_x - viewport_width // 2))
        top = max(0, min(len(match["board"]) - viewport_height, focus_y - viewport_height // 2))
        for offset in range(viewport_height):
            self._put(4 + offset, map_column, match["board"][top + offset][left:left + viewport_width], curses.A_DIM)
        overlays: list[tuple[int, int, str, int]] = []
        route = path_to(match, 0, self.cursor) if self.cursor != _position(match, 0) else []
        costs = {terrain["glyph"]: int(terrain["cost"]) for terrain in self.catalog.terrains.values()}
        spent = 0
        for x, y in route:
            spent += costs[match["board"][y][x]]
            if spent > ORDER_TICKS:
                break
            overlays.append((x, y, ":", curses.A_DIM))
        for hazard in match["hazards"]:
            if hazard["active"] and f"hazards:{hazard['id']}" in team["known"]:
                overlays.extend((x, y, "^", self._attr(3) | curses.A_BOLD)
                                for x, y in hazard["cells"] if [x, y] not in hazard["triggered_cells"])
        for pickup in match["pickups"]:
            if not pickup["resolved"] and f"pickups:{pickup['id']}" in team["known"]:
                overlays.append((pickup["x"], pickup["y"], "$" if pickup["kind"] == "item" else "?", self._attr(2) | curses.A_BOLD))
        for facility in match["facilities"]:
            if not facility["used"] and f"facilities:{facility['id']}" in team["known"]:
                overlays.append((facility["x"], facility["y"], "H", self._attr(4) | curses.A_BOLD))
        station_symbols = {"event": "?", "camp": "C", "upgrade": "W", "cache": "$"}
        for station in match["stations"]:
            if not station["used"] and f"stations:{station['id']}" in team["known"]:
                overlays.append((station["x"], station["y"], station_symbols[station["kind"]], self._attr(2) | curses.A_BOLD))
        for side in (0, 1):
            file = match["files"][side]
            if file["carrier"] is None:
                x, y = _file_position(match, side)
                overlays.append((x, y, "F" if side == 0 else "f", self._attr(2) | curses.A_BOLD))
        overlays.append((*_position(match, 1), "P", self._attr(3) | curses.A_BOLD))
        overlays.append((*(moving or _position(match, 0)), "@", self._attr(4) | curses.A_BOLD))
        overlays.append((*self.cursor, "+", curses.A_REVERSE | curses.A_BOLD))
        for x, y, symbol, attr in overlays:
            if left <= x < left + viewport_width and top <= y < top + viewport_height:
                self._put(4 + y - top, map_column + x - left, symbol, attr)
        route_cost = sum(costs[match["board"][y][x]] for x, y in route)
        legend_row = 4 + viewport_height
        self._put(legend_row, 2, self._ellipsize(f"@ COURIER  P PATRON  F/f FILES  ^ HAZARD  H DESK  C REST  W WORKSHOP  ?/$ CACHE  ROUTE {route_cost}T", columns - 3))
        self._put(legend_row + 1, 2, self._ellipsize(self.message or match["log"][-1], columns - 3), self._attr(2))
        self._put(legend_row + 2, 2, "Steal their file, return within 2 of your home, hold through their turn. First to 2.")
        self._footer("Arrows aim  Enter auto-walk  Tab sites  1 home  2 rival file  3 patron  4 own file  E end  S save  Q leave")

    def _render_combat_match(self, target: str | None = None) -> None:
        match = self.match
        assert match is not None
        team = _team(match, 0)
        enemy = _team(match, 1)
        biome = _biome_at(match, *team["position"])
        label = OFFICE_BIOMES[biome]
        self._begin(f"{label.upper()} / RANKED CARD COMBAT — ROUND {match['round']} — ENERGY {team['energy']} — APPROVAL {match['scores'][0]}:{match['scores'][1]}")
        self._put(1, 2, f"COURIER VS {match.get('patron_name', 'PATRON').upper()}  |  {match['combat_turns']} COMBAT TURNS  |  TWO-TURN RETURNS", self._attr(2) | curses.A_BOLD)
        self._put(2, self._combat_column(2), "YOUR PARTY < BACK    FORMATION    FRONT >", curses.A_BOLD)
        self._put(2, self._combat_column(43), "RIVAL PARTY < FRONT   FORMATION  BACK >", curses.A_BOLD)
        self._put(3, self._combat_column(39), "||", curses.A_BOLD)
        hand = team["hand"]
        self.selected_card = max(0, min(self.selected_card, len(hand) - 1))
        legal_targets = valid_targets(match, self.selected_card) if hand else []
        active_role = self.catalog.cards[_card_id(hand[self.selected_card])]["hero"] if hand else None
        for side, column_base in ((0, 29), (1, 43)):
            party = _team(match, side)["actors"]
            for actor in party:
                rank = actor["rank"]
                column = self._combat_column(column_base + ((1 - rank) * 9 if side == 0 else (rank - 1) * 9))
                attr = self._hp_attr(actor["hp"], actor["max_hp"]) if actor["hp"] else curses.A_DIM
                if actor["id"] == target or actor["id"] in legal_targets or side == 0 and actor["role"] == active_role:
                    attr |= curses.A_BOLD
                if actor["id"] == target:
                    attr |= curses.A_REVERSE
                self._draw_sprite(3, column, _office_sprite(actor["role"]), attr)
                if actor["id"] == target:
                    self._target_brackets(5, column, attr)
                label = OFFICE_ROLES[actor["role"]]
                self._put(8, column, f"R{rank} {label[:4].upper():4}", attr)
                self._put(9, column, f"H{actor['hp']:02}/{actor['max_hp']:02}", attr)
                self._put(10, column, f"S{actor['stress']:02} B{actor['block']:02}", attr)
                if actor["respawn"]:
                    self._put(11, column, f"RETURN {actor['respawn']}", self._attr(3))
        self._put(12, 2, self._ellipsize(self.message or match["log"][-1], self.screen.getmaxyx()[1] - 4), self._attr(2))
        first = max(0, min(self.selected_card, len(hand) - 5))
        for slot, card in enumerate(hand[first:first + 5]):
            index = first + slot
            legal = bool(valid_targets(match, index)) and _card_cost(match, 0, card) <= team["energy"]
            self._draw_mini_office_card(13, self._combat_column(2 + slot * 15), card, index == self.selected_card, legal)
        if not hand:
            self._put(17, 28, "HAND EMPTY — PRESS E", curses.A_DIM)
        self._footer("Arrows/H/L card  Enter play  C full card  R roster  E end  S save  Q leave")

    def _draw_mini_office_card(self, row: int, column: int, instance: str | dict,
                               selected: bool, legal: bool) -> None:
        card_id = _card_id(instance)
        card = self.catalog.cards[card_id]
        office = office_catalog()[1][card_id]
        cost = _card_cost(self.match, 0, instance)
        initials = "".join(word[0] for word in OFFICE_ROLES[card["hero"]].split())[:2].upper()
        lines = ["+------------+", f"|{str(cost) + 'E':<12}|", f"|{(office.name + ('+' if _card_upgraded(instance) else ''))[:12]:<12}|",
                 f"| {initials:^10} |", "|  .----.    |", "|  |____|    |",
                 f"|{card['target'][:12]:<12}|", f"|FROM {','.join(map(str, card['from_ranks']))[:7]:<7}|",
                 "+------------+"]
        attr = curses.A_REVERSE | curses.A_BOLD if selected else (0 if legal else curses.A_DIM)
        for offset, line in enumerate(lines):
            self._put(row + offset, column, line, attr)

    def _show_card(self) -> None:
        match = self.match
        assert match is not None
        hand = _team(match, 0)["hand"]
        if not hand:
            self._notice("EMPTY HAND", "There are no cards to inspect this turn.")
            return
        instance = hand[self.selected_card]
        card_id = _card_id(instance)
        definition = self.catalog.cards[card_id]
        office = office_catalog()[1][card_id]
        role = OFFICE_ROLES[definition["hero"]]
        self._begin(f"{office.name.upper()} / {role.upper()}")
        width = 22
        def framed(value: str = "") -> str:
            return "|" + value[:width - 2].ljust(width - 2) + "|"
        summary = textwrap.wrap(office.description, 18)[:4]
        summary += [""] * (4 - len(summary))
        lines = ["+" + "-" * 20 + "+", framed(f"{_card_cost(match, 0, instance)} ENERGY"),
                 framed((office.name + ('+' if _card_upgraded(instance) else '')).upper().center(20)), framed(role.upper().center(20)),
                 framed("  .----------.  "), framed("  |  OFFICE  |  "),
                 framed("  '----------'  "), framed(),
                 framed("FROM " + ",".join(map(str, definition["from_ranks"]))),
                 framed("TARGET " + definition["target"].replace("_", " ")),
                 *(framed(line) for line in summary), "+" + "-" * 20 + "+" ]
        for row, line in enumerate(lines, 3):
            self._put(row, 4, line, curses.A_BOLD if row in (3, 5, 16) else 0)
        detail = [f"RANKS: {definition['from_ranks']}  TARGET RANKS: {definition.get('target_ranks', 'any')}",
                  f"COST: {_card_cost(match, 0, instance)}  ROLE: {role}",
                  "", office.description, "", "EFFECTS:"]
        detail.extend(str(dict(effect)) for effect in (definition["upgrade_effects"] if _card_upgraded(instance) else definition["effects"]))
        col = 30
        row = 3
        for paragraph in detail:
            for line in textwrap.wrap(paragraph, max(20, self.screen.getmaxyx()[1] - col - 3)) or [""]:
                self._put(row, col, line)
                row += 1
        self._footer("Any key returns to ranked combat")
        self.screen.getch()

    def _choose_pending(self) -> None:
        match = self.match
        assert match is not None
        pending = match["pending"]
        if not pending:
            return
        if pending["kind"] == "draft":
            choices = [f"{office_catalog()[1][card].name} — {OFFICE_ROLES[self.catalog.cards[card]['hero']]}"
                       for card in pending["choices"]]
            title = "CHOOSE AN OFFICE TECHNIQUE"
            body = "A neutral cache offers a lasting addition to your shared deck."
        elif pending["kind"] == "boon":
            choices = [f"Company perk {index + 1}: " + ", ".join(effect["key"].replace("_", " ") for effect in self.catalog.boons[boon]["effects"])
                       for index, boon in enumerate(pending["choices"])]
            title = "CHOOSE A COMPANY PERK"
            body = "A neutral cache offers one lasting perk. Choose its recipient next."
        elif pending["kind"] == "recipient":
            choices = [OFFICE_ROLES[next(actor["role"] for actor in _team(match, 0)["actors"] if actor["id"] == identity)]
                       for identity in pending["choices"]]
            title = "CHOOSE A RECIPIENT"
            body = "This company perk belongs to one specialist for the match."
        elif pending["kind"] == "facility":
            choices = [*pending["choices"], "Leave without using"]
            facility = next(item for item in match["facilities"] if item["id"] == pending["facility"])
            definition = self.catalog.facilities[facility["definition_id"]]
            title = f"{OFFICE_BIOMES[facility['biome_id']]} SERVICE DESK".upper()
            body = "A neutral company facility can alter the route or restore the party. Choose one procedure."
        else:
            choices = pending["choices"][:]
            if pending["kind"] in {"camp", "upgrade", "event"}:
                choices.append("Leave without using")
            title = {"camp": "REST OFFICE", "upgrade": "COPY WORKSHOP", "event": "DEPARTMENT INCIDENT",
                     "treatment": "TREAT A LIABILITY"}[pending["kind"]]
            body = "A neutral room from the generated expedition offers a choice."
        selected = self._menu(title, choices, body, allow_cancel=False)
        assert selected is not None
        try:
            choose_reward(match, selected)
        except ValueError as exc:
            self._notice("PROCEDURE UNAVAILABLE", str(exc))

    def _select_target(self, choices: list[str]) -> str | None:
        if len(choices) == 1:
            return choices[0]
        actors = {actor["id"]: actor for team in self.match["teams"] for actor in team["actors"]}
        ordered = sorted(choices, key=lambda identity: (int(identity.split(":", 1)[0]), actors[identity]["rank"]))
        selected = 0
        while True:
            self._render_combat_match(ordered[selected])
            key = self.screen.getch()
            if key in (curses.KEY_LEFT, curses.KEY_UP, ord("h"), ord("k")):
                selected = (selected - 1) % len(ordered)
            elif key in (curses.KEY_RIGHT, curses.KEY_DOWN, ord("l"), ord("j")):
                selected = (selected + 1) % len(ordered)
            elif key in (10, 13, curses.KEY_ENTER):
                return ordered[selected]
            elif key == 27:
                return None

    def _save(self) -> None:
        from jomon.save import SaveError, save_game

        try:
            self.message = f"Saved to {save_game(self.jomon_state)}."
        except SaveError as exc:
            self.message = str(exc)

    def _auto_walk_to(self, destination: tuple[int, int]) -> None:
        match = self.match
        assert match is not None
        if destination == _position(match, 0):
            raise ValueError("the party is already at that destination")
        while (match["phase"] == "map" and match["winner"] is None and not match["pending"]
               and _team(match, 0)["orders"] < ORDERS_PER_TURN
               and destination != _position(match, 0)):
            leg = _ai_destination(match, destination)
            if leg is None:
                raise ValueError("no route reaches the selected destination")
            walked = move_to(match, leg)
            for point in walked[:-1]:
                self._render_map(point)
                curses.napms(self.MOVE_FRAME_MS)
            if (match["phase"] == "map" and not match["pending"]
                    and _team(match, 0)["orders"] < ORDERS_PER_TURN
                    and destination != _position(match, 0)
                    and hasattr(self.screen, "nodelay") and self._route_cancel_requested()):
                self.message = "Route cancelled before the next leg."
                break

    def run_match(self) -> None:
        match = self.match
        assert match is not None
        self.cursor = _position(match, 0)
        while True:
            if match["winner"] is not None:
                result = finish_match(self.jomon_state)
                self._notice("THE COMPANY HAS REACHED A DECISION",
                             f"{result.upper()} — approval {match['scores'][0]}:{match['scores'][1]}. The files are returned to the cabinet.")
                return
            if match["turn"] == 1:
                patron_turn(match)
                continue
            if match["pending"]:
                self._choose_pending()
                continue
            engage_if_touching(match)
            if match["phase"] == "map":
                self._render_map()
            else:
                self._render_combat_match()
            key = self.screen.getch()
            normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
            if normalized in (ord("q"), 27):
                return
            if normalized == ord("s"):
                self._save()
                continue
            if normalized in (ord("e"), ord(" ")):
                try:
                    end_turn(match)
                except ValueError as exc:
                    self.message = str(exc)
                continue
            if match["phase"] == "map":
                x, y = self.cursor
                if normalized in (curses.KEY_LEFT, ord("a"), ord("h")):
                    self.cursor = max(0, x - 1), y
                elif normalized in (curses.KEY_RIGHT, ord("d"), ord("l")):
                    self.cursor = min(len(match["board"][0]) - 1, x + 1), y
                elif normalized in (curses.KEY_UP, ord("w"), ord("k")):
                    self.cursor = x, max(0, y - 1)
                elif normalized in (curses.KEY_DOWN, ord("x"), ord("j")):
                    self.cursor = x, min(len(match["board"]) - 1, y + 1)
                elif normalized == ord("1"):
                    self.cursor = tuple(match["files"][0]["home"])
                elif normalized == ord("2"):
                    self.cursor = _file_position(match, 1)
                elif normalized == ord("3"):
                    self.cursor = _position(match, 1)
                elif normalized == ord("4"):
                    self.cursor = _file_position(match, 0)
                elif normalized == 9:
                    sites = [tuple(match["files"][1]["home"]),
                             *((item["x"], item["y"]) for item in match["pickups"] if not item["resolved"] and f"pickups:{item['id']}" in _team(match, 0)["known"]),
                             *((item["x"], item["y"]) for item in match["facilities"] if not item["used"] and f"facilities:{item['id']}" in _team(match, 0)["known"]),
                             *((item["x"], item["y"]) for item in match["stations"] if not item["used"] and f"stations:{item['id']}" in _team(match, 0)["known"])]
                    if sites:
                        self.site = (self.site + 1) % len(sites)
                        self.cursor = sites[self.site]
                elif normalized in (10, 13, curses.KEY_ENTER):
                    try:
                        self._auto_walk_to(self.cursor)
                    except ValueError as exc:
                        self._notice("ROUTE NOT AVAILABLE", str(exc))
            else:
                hand = _team(match, 0)["hand"]
                if normalized in (curses.KEY_LEFT, curses.KEY_UP, ord("h"), ord("k")) and hand:
                    self.selected_card = (self.selected_card - 1) % len(hand)
                elif normalized in (curses.KEY_RIGHT, curses.KEY_DOWN, ord("l"), ord("j")) and hand:
                    self.selected_card = (self.selected_card + 1) % len(hand)
                elif normalized == ord("c"):
                    self._show_card()
                elif normalized == ord("r"):
                    body = "\n".join(f"{('COURIER', 'PATRON')[side]} R{actor['rank']} {OFFICE_ROLES[actor['role']]} — {actor['hp']}/{actor['max_hp']} HP, {actor['stress']} stress, return {actor['respawn']}"
                                      for side in (0, 1) for actor in _team(match, side)["actors"])
                    self._notice("PARTY ROSTERS", body)
                elif normalized in (10, 13, curses.KEY_ENTER) and hand:
                    targets = valid_targets(match, self.selected_card)
                    if not targets:
                        self._notice("CARD NOT LEGAL", "The specialist is unavailable or outside this card's origin rank.")
                        continue
                    target = self._select_target(targets)
                    if target is None:
                        continue
                    try:
                        play_card(match, self.selected_card, target)
                        self._render_combat_match(target if ":" in target else None)
                        curses.napms(self.DAMAGE_FLASH_MS)
                    except ValueError as exc:
                        self._notice("CARD NOT LEGAL", str(exc))


def run_expedition(screen: curses.window, state) -> None:
    if state.courier is None or not state.courier.alive:
        state.add_message("Choose a living courier before opening Dullest Dungeon.")
        return
    active = state.tabletop["active_match"]
    if active and active["courier_id"] != state.courier.id:
        owner = next((person for person in state.household if person.id == active["courier_id"]), None)
        if owner and not owner.alive:
            active["winner"] = 1
            finish_match(state)
            state.add_message(f"{owner.name}'s unfinished expedition is archived as a loss.")
            active = None
        else:
            state.add_message(f"{owner.name if owner else 'Another courier'} must finish the open expedition.")
            return
    ui = ExpeditionUI(screen, state)
    if active:
        ui.run_match()
        return
    selected_patron, slot, deck_index = 0, 0, 0
    mode = "lobby"
    message = ""
    while True:
        height, width = screen.getmaxyx()
        if height < 24 or width < 80:
            screen.erase()
            ui._put(1, 1, "Dullest Dungeon needs an 80x24 terminal. Resize or press Q to leave.")
            screen.refresh()
            if screen.getch() in (ord("q"), ord("Q"), 27):
                return
            continue
        if mode == "lobby":
            people = _draw_lobby(screen, state, selected_patron, slot, message)
        else:
            collection = collection_for(state, state.courier.id)
            available = _draw_editor(screen, collection, deck_index, message)
        key = screen.getch()
        normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
        message = ""
        if mode == "lobby":
            if normalized in (ord("q"), 27):
                return
            if normalized in (ord("j"), curses.KEY_DOWN) and people:
                selected_patron = (selected_patron + 1) % len(people)
            elif normalized in (ord("k"), curses.KEY_UP) and people:
                selected_patron = (selected_patron - 1) % len(people)
            elif ord("1") <= normalized <= ord("4"):
                slot = normalized - ord("1")
            elif normalized in (ord("["), ord("]")):
                collection = collection_for(state, state.courier.id)
                roles = list(office_catalog()[0])
                current = roles.index(collection["roles"][slot])
                step = 1 if normalized == ord("]") else -1
                collection["roles"][slot] = next(roles[(current + step * offset) % len(roles)] for offset in range(1, len(roles)) if roles[(current + step * offset) % len(roles)] not in collection["roles"])
                collection["deck"] = [card for role in collection["roles"] for card in office_catalog()[0][role]["starter_deck"]]
                for key in ("masteries", "infusions"):
                    collection[key] = {card: value for card, value in collection[key].items()
                                       if load_catalog().cards[card]["hero"] in collection["roles"]}
                if not doctrine_compatible(collection["roles"], collection["doctrine"]):
                    collection["doctrine"] = next(doctrine for doctrine in load_catalog().doctrines if doctrine_compatible(collection["roles"], doctrine))
            elif normalized == ord("d"):
                mode = "deck"
            elif normalized == ord("p"):
                collection = collection_for(state, state.courier.id)
                doctrines = [doctrine for doctrine in load_catalog().doctrines if doctrine_compatible(collection["roles"], doctrine)]
                collection["doctrine"] = doctrines[(doctrines.index(collection["doctrine"]) + 1) % len(doctrines)]
            elif normalized in (10, 13) and people:
                try:
                    ui.match = start_match(state, people[selected_patron % len(people)].id)
                    ui.run_match()
                    return
                except ValueError as exc:
                    message = str(exc)
        else:
            if normalized in (27, ord("q")):
                mode = "lobby"
            elif normalized in (ord("j"), curses.KEY_DOWN) and available:
                deck_index = (deck_index + 1) % len(available)
            elif normalized in (ord("k"), curses.KEY_UP) and available:
                deck_index = (deck_index - 1) % len(available)
            elif normalized == ord("a") and available:
                card_id = available[deck_index % len(available)]
                if len(collection["deck"]) >= 30 or collection["deck"].count(card_id) >= collection["cards"][card_id]:
                    message = "No room or no unused copy."
                else:
                    collection["deck"].append(card_id)
            elif normalized == ord("x") and available:
                card_id = available[deck_index % len(available)]
                if len(collection["deck"]) <= 20 or card_id not in collection["deck"]:
                    message = "Keep at least twenty cards and select one in the deck."
                else:
                    collection["deck"].remove(card_id)
            elif normalized == ord("m") and available:
                card_id = available[deck_index % len(available)]
                mastery = next((item for item in load_catalog().masteries.values() if item["card_id"] == card_id), None)
                if not mastery:
                    message = "This card has no mastery branches."
                else:
                    current = collection["masteries"].get(card_id)
                    if current == "coverage":
                        collection["masteries"].pop(card_id)
                    else:
                        collection["masteries"][card_id] = "engine" if current is None else "coverage"
            elif normalized == ord("i") and available:
                card_id = available[deck_index % len(available)]
                infusions = [infusion for infusion in load_catalog().infusions if infusion_compatible(card_id, infusion)]
                if not infusions:
                    message = "No treatment suits this technique."
                    continue
                current = collection["infusions"].get(card_id)
                if current == infusions[-1]:
                    collection["infusions"].pop(card_id)
                else:
                    collection["infusions"][card_id] = infusions[0] if current is None else infusions[infusions.index(current) + 1]
