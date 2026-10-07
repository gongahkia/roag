"""Original-style dungeon viewport and ranked card table inside Jomon."""

from __future__ import annotations

import curses
import textwrap

from .content import load_catalog
from .office_art import office_card_glyph, office_costume_name, rival_costumes
from .presentation import dd_text, map_symbols, office_sprites
from .expedition import (
    MAX_ROUNDS, ORDER_TICKS, ORDERS_PER_TURN, _ai_destination, _biome_at,
    _card_cost, _card_id, _card_upgraded, _file_position, _position, _team, choose_reward,
    _opponent_side, doctrine_compatible, end_turn, engage_if_touching, engage_neutral_if_touching,
    finish_match, infusion_compatible,
    move_to, patron_turn, path_to, pending_choice_labels, play_card, retreat, retreat_destinations,
    start_match, valid_targets,
)
from .office_content import (
    DOCTRINE_NAMES, OFFICE_BIOMES, OFFICE_ROLES, OFFICE_SQUADS, OFFICE_TARGETS, OFFICE_WORLDS,
    office_card_description, office_catalog,
)
from .tabletop import collection_for
from .tabletop_ui import _draw_editor, _draw_lobby
from .tavern_ui_base import TavernUIBase


class ExpeditionUI(TavernUIBase):
    def __init__(self, screen: curses.window, state):
        super().__init__(screen, load_catalog())
        self.jomon_state = state
        self.match = state.tabletop["active_match"]
        self.cursor = _position(self.match, 0) if self.match else (5, 17)
        self.selected_card = 0
        self.site = 0

    def _render_map(self, moving: tuple[int, int] | None = None,
                    rival_moving: tuple[int, int] | None = None) -> None:
        match = self.match
        assert match is not None
        symbols = map_symbols()
        team = _team(match, 0)
        title = dd_text("ui.map_title", game=dd_text("ui.game_title"), world=OFFICE_WORLDS[match["world_id"]].upper())
        self._begin(title)
        round_label = (dd_text("ui.map_overtime", round=match["round"] - MAX_ROUNDS) if match["round"] > MAX_ROUNDS else dd_text("ui.map_round", round=match["round"], maximum=MAX_ROUNDS))
        lead = dd_text("ui.map_patron_route") if rival_moving else round_label
        self._put(1, 2, dd_text("ui.map_status", lead=lead, score_a=match["scores"][0], score_b=match["scores"][1], patron=match.get("patron_name", "PATRON")[:20], orders=ORDERS_PER_TURN-team["orders"], maximum=ORDERS_PER_TURN), curses.A_BOLD)
        biome = _biome_at(match, *team["position"])
        self._put(2, 2, dd_text("ui.map_resources", biome=OFFICE_BIOMES[biome].upper(), light=team["light"], supplies=team["supplies"], boons=len(team["boons"]), items=sum(team["items"].values())))
        rows, columns = self.screen.getmaxyx()
        viewport_width = min(columns - 4, len(match["board"][0]))
        viewport_height = min(rows - 9, len(match["board"]))
        map_column = max(2, (columns - viewport_width) // 2)
        focus_x, focus_y = rival_moving or self.cursor
        left = max(0, min(len(match["board"][0]) - viewport_width, focus_x - viewport_width // 2))
        top = max(0, min(len(match["board"]) - viewport_height, focus_y - viewport_height // 2))
        for offset in range(viewport_height):
            self._put(4 + offset, map_column, match["board"][top + offset][left:left + viewport_width], curses.A_DIM)
        overlays: list[tuple[int, int, str, int]] = []
        route = path_to(match, 0, self.cursor) if rival_moving is None and self.cursor != _position(match, 0) else []
        costs = {terrain["glyph"]: int(terrain["cost"]) for terrain in self.catalog.terrains.values()}
        spent = 0
        for x, y in route:
            spent += costs[match["board"][y][x]]
            if spent > ORDER_TICKS:
                break
            overlays.append((x, y, symbols["route_preview"], curses.A_DIM))
        for landmark in match["landmarks"]:
            # Landmark geometry is mechanical; its visible stamp is pack-owned.
            symbol = symbols["station"]["event"]
            for cell in landmark["cells"]:
                overlays.append((cell[0], cell[1], symbol, self._attr(6) | curses.A_BOLD))
        for hazard in match["hazards"]:
            if hazard["active"] and f"hazards:{hazard['id']}" in team["known"]:
                overlays.extend((x, y, symbols["hazard"], self._attr(3) | curses.A_BOLD)
                                for x, y in hazard["cells"] if [x, y] not in hazard["triggered_cells"])
        for pickup in match["pickups"]:
            if not pickup["resolved"] and f"pickups:{pickup['id']}" in team["known"]:
                overlays.append((pickup["x"], pickup["y"], symbols["pickup"]["item" if pickup["kind"] == "item" else "other"], self._attr(2) | curses.A_BOLD))
        for facility in match["facilities"]:
            if not facility["used"] and f"facilities:{facility['id']}" in team["known"]:
                overlays.append((facility["x"], facility["y"], symbols["facility"], self._attr(4) | curses.A_BOLD))
        for station in match["stations"]:
            if not station["used"] and f"stations:{station['id']}" in team["known"]:
                overlays.append((station["x"], station["y"], symbols["station"][station["kind"]], self._attr(2) | curses.A_BOLD))
        for patrol in match.get("patrols", []):
            if patrol["active"]:
                overlays.append((*patrol["position"], symbols["patrol"][patrol["kind"]], self._attr(3) | curses.A_BOLD))
        for side in (0, 1):
            file = match["files"][side]
            if file["carrier"] is None:
                x, y = _file_position(match, side)
                overlays.append((x, y, symbols["files"]["courier" if side == 0 else "patron"], self._attr(2) | curses.A_BOLD))
        overlays.append((*(rival_moving or _position(match, 1)), symbols["parties"]["patron"], self._attr(3) | curses.A_BOLD))
        overlays.append((*(moving or _position(match, 0)), symbols["parties"]["courier"], self._attr(4) | curses.A_BOLD))
        if rival_moving is None:
            cursor_symbol = (symbols["parties"]["courier"] if self.cursor == _position(match, 0) else
                             symbols["parties"]["patron"] if self.cursor == _position(match, 1) else symbols["cursor"])
            overlays.append((*self.cursor, cursor_symbol, curses.A_REVERSE | curses.A_BOLD))
        for x, y, symbol, attr in overlays:
            if left <= x < left + viewport_width and top <= y < top + viewport_height:
                self._put(4 + y - top, map_column + x - left, symbol, attr)
        route_cost = sum(costs[match["board"][y][x]] for x, y in route)
        legend_row = 4 + viewport_height
        self._put(legend_row, 2, self._ellipsize(
            dd_text("ui.map_legend", courier=symbols["parties"]["courier"], patron=symbols["parties"]["patron"],
                    fight=symbols["patrol"]["fight"], elite=symbols["patrol"]["elite"], boss=symbols["patrol"]["boss"],
                    file_a=symbols["files"]["courier"], file_b=symbols["files"]["patron"], hazard=symbols["hazard"],
                    facility=symbols["facility"], camp=symbols["station"]["camp"], upgrade=symbols["station"]["upgrade"],
                    event=symbols["station"]["event"], cache=symbols["station"]["cache"], cost=route_cost), columns - 3))
        self._put(legend_row + 1, 2, self._ellipsize(self.message or match["log"][-1], columns - 3), self._attr(2))
        self._put(legend_row + 2, 2, dd_text("ui.map_objective"))
        self._footer(dd_text("ui.map_footer"))

    def _render_combat_match(self, target: str | None = None) -> None:
        match = self.match
        assert match is not None
        team = _team(match, 0)
        opponent = _opponent_side(match, 0)
        neutral = opponent == 2
        biome = _biome_at(match, *team["position"])
        label = OFFICE_BIOMES[biome]
        self._begin(dd_text("ui.combat_title_full", biome=label.upper(), kind="PATROL" if neutral else "PARTY", round=match["round"], energy=team["energy"], score_a=match["scores"][0], score_b=match["scores"][1]))
        opponent_name = dd_text("ui.combat_enemy_status", label="NEUTRAL OFFICE PATROL") if neutral else match.get("patron_name", "PATRON").upper()
        self._put(1, 2, dd_text("ui.combat_status", opponent=opponent_name, turns=match["combat_turns"]), self._attr(2) | curses.A_BOLD)
        self._put(2, self._combat_column(2), dd_text("ui.combat_party"), curses.A_BOLD)
        self._put(2, self._combat_column(43), dd_text("ui.combat_rival", label="PATROL" if neutral else "RIVAL PARTY"), curses.A_BOLD)
        self._put(3, self._combat_column(39), dd_text("ui.combat_divider"), curses.A_BOLD)
        hand = team["hand"]
        self.selected_card = max(0, min(self.selected_card, len(hand) - 1))
        legal_targets = valid_targets(match, self.selected_card) if hand else []
        active_role = self.catalog.cards[_card_id(hand[self.selected_card])]["hero"] if hand else None
        for side, column_base in ((0, 29), (opponent, 43)):
            party = _team(match, side)["actors"]
            for index, actor in enumerate(party):
                rank = actor["rank"]
                column = self._combat_column(column_base + ((1 - rank) * 9 if side == 0 else (rank - 1) * 9))
                attr = self._hp_attr(actor["hp"], actor["max_hp"]) if actor["hp"] else curses.A_DIM
                if actor["id"] == target or actor["id"] in legal_targets or side == 0 and actor["role"] == active_role:
                    attr |= curses.A_BOLD
                if actor["id"] == target:
                    attr |= curses.A_REVERSE
                portrait = (office_sprites()[actor["role"]] if side == 0 else
                            office_sprites()[actor["role"] if actor["role"] in OFFICE_ROLES else self.catalog.heroes["warden"]["id"]])
                self._draw_sprite(3, column, portrait, attr)
                if actor["id"] == target:
                    self._target_brackets(5, column, attr)
                label = OFFICE_ROLES[actor["role"]] if actor["role"] in OFFICE_ROLES else office_costume_name(actor["role"])
                self._put(8, column, dd_text("ui.combat_actor", rank=rank, label=label[:4].upper()), attr)
                self._put(9, column, dd_text("ui.combat_hp", hp=f"{actor['hp']:02}", maximum=f"{actor['max_hp']:02}"), attr)
                self._put(10, column, dd_text("ui.combat_stress", stress=f"{actor['stress']:02}", block=f"{actor['block']:02}"), attr)
                if actor["respawn"]:
                    self._put(11, column, dd_text("ui.combat_return", turns=actor["respawn"]), self._attr(3))
                elif side != 0:
                    self._put(11, column, dd_text("ui.combat_enemy_status", label="PATROL" if neutral else "COSTUME"), curses.A_DIM)
        self._put(12, 2, self._ellipsize(self.message or match["log"][-1], self.screen.getmaxyx()[1] - 4), self._attr(2))
        first = max(0, min(self.selected_card, len(hand) - 5))
        for slot, card in enumerate(hand[first:first + 5]):
            index = first + slot
            legal = bool(valid_targets(match, index)) and _card_cost(match, 0, card) <= team["energy"]
            self._draw_mini_office_card(13, self._combat_column(2 + slot * 15), card, index == self.selected_card, legal)
        if not hand:
            self._put(17, 28, dd_text("ui.empty_hand"), curses.A_DIM)
        self._footer(dd_text("ui.combat_footer"))

    def _mini_office_card_lines(self, instance: str | dict) -> list[str]:
        card_id = _card_id(instance)
        card = self.catalog.cards[card_id]
        office = office_catalog()[1][card_id]
        cost = _card_cost(self.match, 0, instance) if self.match else card["cost"]
        mark = map_symbols()["cursor"]
        glyph = office_card_glyph(card["hero"])
        description = textwrap.wrap(office_card_description(card_id, upgraded=_card_upgraded(instance)), 12)[:2]
        description += [""] * (2 - len(description))
        def framed(value: str = "") -> str:
            return "|" + value[:12].ljust(12) + "|"
        return ["+------------+", framed(f"{cost}E".ljust(11) + mark),
                framed(office.name.upper() + ("+" if _card_upgraded(instance) else "")),
                *(framed(line.center(12)) for line in glyph),
                framed(OFFICE_TARGETS[card["target"]].upper()),
                framed(description[0]), framed(description[1]), "+------------+"]

    def _draw_mini_office_card(self, row: int, column: int, instance: str | dict,
                               selected: bool, legal: bool) -> None:
        attr = curses.A_REVERSE | curses.A_BOLD if selected else (0 if legal else curses.A_DIM)
        for offset, line in enumerate(self._mini_office_card_lines(instance)):
            self._put(row + offset, column, line, attr)

    def _full_office_card_lines(self, instance: str | dict) -> list[str]:
        card_id = _card_id(instance)
        definition = self.catalog.cards[card_id]
        office = office_catalog()[1][card_id]
        role_id = definition["hero"]
        cost = _card_cost(self.match, 0, instance) if self.match else definition["cost"]
        mark = map_symbols()["cursor"]
        glyph = office_card_glyph(role_id)
        description = textwrap.wrap(office_card_description(card_id, upgraded=_card_upgraded(instance)), 18)[:4]
        description += [""] * (4 - len(description))
        def framed(value: str = "") -> str:
            return "|" + value[:20].ljust(20) + "|"
        border = "+" + "-" * 20 + "+"
        ranks = ",".join(map(str, definition["from_ranks"]))
        return [border, framed(f"{cost} ENERGY".ljust(19) + mark),
                framed((office.name + ("+" if _card_upgraded(instance) else "")).upper().center(20)),
                framed(OFFICE_ROLES[role_id].upper().center(20)),
                framed(dd_text("ui.technique")),
                *(framed(line.center(20)) for line in glyph),
                framed(), framed("FROM " + ranks),
                framed("TARGET " + OFFICE_TARGETS[definition["target"]].upper()),
                *(framed(line) for line in description),
                framed(mark + " " * 18 + mark), border]

    def _show_card(self) -> None:
        match = self.match
        assert match is not None
        hand = _team(match, 0)["hand"]
        if not hand:
            self._notice(dd_text("ui.card_empty_title"), dd_text("ui.card_empty_body"))
            return
        instance = hand[self.selected_card]
        card_id = _card_id(instance)
        definition = self.catalog.cards[card_id]
        office = office_catalog()[1][card_id]
        role = OFFICE_ROLES[definition["hero"]]
        self._begin(dd_text("ui.card_detail_title", card=office.name.upper(), role=role.upper()))
        lines = self._full_office_card_lines(instance)
        for row, line in enumerate(lines, 3):
            self._put(row, 4, line, curses.A_BOLD if row in (3, 5, 19) else 0)
        detail = [dd_text("ui.card_detail_ranks", ranks=definition["from_ranks"], targets=definition.get("target_ranks", "any")),
                  dd_text("ui.card_detail_cost", cost=_card_cost(match, 0, instance), role=role),
                  "", office_card_description(card_id, upgraded=_card_upgraded(instance)),
                  "", dd_text("ui.card_detail_rule")]
        col = 30
        row = 3
        for paragraph in detail:
            for line in textwrap.wrap(paragraph, max(20, self.screen.getmaxyx()[1] - col - 3)) or [""]:
                self._put(row, col, line)
                row += 1
        self._footer(dd_text("ui.card_return"))
        self.screen.getch()

    def _choose_pending(self) -> None:
        match = self.match
        assert match is not None
        pending = match["pending"]
        if not pending:
            return
        if pending["kind"] == "draft":
            choices = [dd_text("ui.pending_draft_choice", card=office_catalog()[1][card].name, role=OFFICE_ROLES[self.catalog.cards[card]["hero"]])
                       for card in pending["choices"]]
            title = dd_text("ui.choose_card")
            body = dd_text("ui.pending_draft")
        elif pending["kind"] == "boon":
            choices = [dd_text("ui.pending_boon_choice", index=index + 1, effects=", ".join(effect["key"].replace("_", " ") for effect in self.catalog.boons[boon]["effects"]))
                       for index, boon in enumerate(pending["choices"])]
            title = dd_text("ui.choose_boon")
            body = dd_text("ui.pending_boon")
        elif pending["kind"] == "recipient":
            choices = [OFFICE_ROLES[next(actor["role"] for actor in _team(match, 0)["actors"] if actor["id"] == identity)]
                       for identity in pending["choices"]]
            title = dd_text("ui.choose_recipient")
            body = dd_text("ui.pending_recipient")
        elif pending["kind"] == "facility":
            choices = [*pending_choice_labels(match, pending), dd_text("ui.leave")]
            facility = next(item for item in match["facilities"] if item["id"] == pending["facility"])
            title = dd_text("ui.pending_facility_title", biome=OFFICE_BIOMES[facility["biome_id"]]).upper()
            body = dd_text("ui.pending_facility")
        else:
            choices = pending_choice_labels(match, pending)
            if pending["kind"] in {"camp", "upgrade", "event"}:
                choices.append(dd_text("ui.leave"))
            title = dd_text(f"ui.pending_kind_{pending['kind']}")
            body = dd_text("ui.pending_room")
        selected = self._menu(title, choices, body, allow_cancel=False)
        assert selected is not None
        try:
            choose_reward(match, selected)
        except ValueError as exc:
            self._notice(dd_text("ui.procedure_unavailable"), str(exc))

    def _select_target(self, choices: list[str]) -> str | None:
        if len(choices) == 1:
            return choices[0]
        teams = [*self.match["teams"]]
        if self.match.get("neutral_team"):
            teams.append(self.match["neutral_team"])
        actors = {actor["id"]: actor for team in teams for actor in team["actors"]}
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
            save_game(self.jomon_state)
            self.message = dd_text("ui.save_complete")
        except SaveError as exc:
            self.message = str(exc)

    def _set_worker(self, collection: dict, slot: int, role: str) -> None:
        if role in collection["roles"] and collection["roles"][slot] != role:
            raise ValueError(dd_text("ui.worker_error"))
        collection["roles"][slot] = role
        collection["deck"] = [card for worker in collection["roles"]
                              for card in office_catalog()[0][worker]["starter_deck"]]
        for key in ("masteries", "infusions"):
            collection[key] = {card: value for card, value in collection[key].items()
                               if self.catalog.cards[card]["hero"] in collection["roles"]}
        if not doctrine_compatible(collection["roles"], collection["doctrine"]):
            collection["doctrine"] = next(doctrine for doctrine in self.catalog.doctrines
                                           if doctrine_compatible(collection["roles"], doctrine))

    def _choose_worker(self, collection: dict, slot: int) -> None:
        roles = list(OFFICE_ROLES)
        choices = [dd_text("ui.worker_choices", role=OFFICE_ROLES[role], ranks=",".join(map(str, self.catalog.heroes[role]["preferred_ranks"])), combat_role=self.catalog.heroes[role]["combat_role"]) for role in roles]
        selected = self._menu(dd_text("ui.worker_title"), choices, dd_text("ui.worker_body"))
        if selected is not None:
            self._set_worker(collection, slot, roles[selected])

    def _choose_formation(self, collection: dict) -> None:
        squads = list(self.catalog.squads.values())
        choices = [dd_text("ui.formation_choice", squad=OFFICE_SQUADS[squad["id"]], roles=", ".join(OFFICE_ROLES[role].split()[0] for role in squad["formation"])) for squad in squads]
        selected = self._menu(dd_text("ui.formation_title"), choices, dd_text("ui.formation_body"))
        if selected is None:
            return
        squad = squads[selected]
        plan = str(squad["playstyle"]).replace("enemies", "rivals").replace("enemy", "rival")
        body = dd_text("ui.formation_detail", roles=", ".join(OFFICE_ROLES[role] for role in squad["formation"]), plan=plan, strength=squad["strength"], liability=squad["weakness"])
        confirmed = self._menu(OFFICE_SQUADS[squad["id"]].upper(),
                               [dd_text("ui.formation_use"), dd_text("ui.formation_keep")], body)
        if confirmed != 0:
            return
        collection["roles"] = list(squad["formation"])
        collection["deck"] = [card for role in collection["roles"]
                              for card in office_catalog()[0][role]["starter_deck"]]
        collection["doctrine"] = squad["doctrine"]
        for key in ("masteries", "infusions"):
            collection[key] = {card: value for card, value in collection[key].items()
                               if self.catalog.cards[card]["hero"] in collection["roles"]}

    def _toggle_loadout(self, collection: dict, slot: int) -> str:
        role = collection["roles"][slot]
        current = [card for card in collection["deck"] if self.catalog.cards[card]["hero"] == role]
        if len(current) != 5:
            raise ValueError(dd_text("ui.loadout_error"))
        starter = list(self.catalog.heroes[role]["starter_deck"])
        loadout = next(item for item in self.catalog.loadouts.values() if item["hero"] == role)
        alternate = list(loadout["cards"])
        replacement = starter if current == alternate else alternate
        collection["deck"] = [card for card in collection["deck"]
                              if self.catalog.cards[card]["hero"] != role] + replacement
        name = "starter" if replacement == starter else "alternate"
        return dd_text("ui.loadout_changed", role=OFFICE_ROLES[role], kind=name)

    def _browse_archive(self) -> None:
        while True:
            category = self._menu(dd_text("ui.archive_title"), dd_text("ui.archive_categories").split("|"), dd_text("ui.archive_body"))
            if category is None:
                return
            if category == 0:
                roles = list(OFFICE_ROLES)
                selected = self._menu(dd_text("ui.archive_worker"), [OFFICE_ROLES[role] for role in roles])
                if selected is None:
                    continue
                role = roles[selected]
                hero = self.catalog.heroes[role]
                alternate = next(item for item in self.catalog.loadouts.values() if item["hero"] == role)
                self._begin(OFFICE_ROLES[role].upper())
                self._draw_sprite(4, 7, office_sprites()[role], curses.A_BOLD)
                self._put(4, 22, dd_text("ui.archive_worker_stats", role=hero["combat_role"].upper(), hp=hero["max_hp"], ranks=",".join(map(str, hero["preferred_ranks"]))))
                self._put(6, 22, dd_text("ui.archive_starter"))
                for row, card in enumerate(hero["starter_deck"], 7):
                    self._put(row, 22, office_catalog()[1][card].name)
                self._put(13, 22, dd_text("ui.archive_alternate"))
                for row, card in enumerate(alternate["cards"], 14):
                    self._put(row, 22, office_catalog()[1][card].name)
                self._footer(dd_text("ui.archive_return"))
                self.screen.getch()
            elif category == 1:
                card_ids = list(office_catalog()[1])
                selected = self._menu(dd_text("ui.office_technique"), [
                    dd_text("ui.archive_card", card=office_catalog()[1][card].name, role=OFFICE_ROLES[office_catalog()[1][card].role])
                    for card in card_ids])
                if selected is None:
                    continue
                card_id = card_ids[selected]
                self._begin(office_catalog()[1][card_id].name.upper())
                for row, line in enumerate(self._full_office_card_lines(card_id), 3):
                    self._put(row, 4, line, curses.A_BOLD if row in (3, 5, 19) else 0)
                for row, line in enumerate(textwrap.wrap(office_card_description(card_id),
                                                         max(20, self.screen.getmaxyx()[1] - 33)), 4):
                    self._put(row, 30, line)
                self._footer(dd_text("ui.archive_return"))
                self.screen.getch()
            elif category == 2:
                enemy_ids = list(self.catalog.enemies)
                selected = self._menu(dd_text("ui.sprite_archive"), [office_costume_name(enemy_id) for enemy_id in enemy_ids],
                                      dd_text("ui.archive_body"))
                if selected is None:
                    continue
                enemy_id = enemy_ids[selected]
                self._begin(office_costume_name(enemy_id).upper())
                self._draw_sprite(5, 9, office_sprites()["warden"], curses.A_BOLD)
                self._put(6, 23, dd_text("ui.archive_costume"))
                self._put(8, 23, dd_text("ui.archive_costume_body"))
                self._footer(dd_text("ui.archive_return"))
                self.screen.getch()
            elif category == 3:
                worlds = list(OFFICE_WORLDS)
                biomes = list(OFFICE_BIOMES)
                choices = [dd_text("ui.archive_floorplan", world=OFFICE_WORLDS[world]) for world in worlds]
                choices += [dd_text("ui.archive_department", biome=OFFICE_BIOMES[biome]) for biome in biomes]
                selected = self._menu(dd_text("ui.formation"), choices)
                if selected is None:
                    continue
                if selected < len(worlds):
                    world = worlds[selected]
                    detail = dd_text("ui.archive_world_detail", world=OFFICE_WORLDS[world], layout=self.catalog.worlds[world]["layout"])
                else:
                    biome = biomes[selected - len(worlds)]
                    detail = dd_text("ui.archive_biome_detail", biome=OFFICE_BIOMES[biome])
                self._notice(dd_text("ui.archive_floorplan_title"), detail)
            elif category == 4:
                doctrines = list(self.catalog.doctrines.values())
                selected = self._menu(dd_text("ui.archive_policy_title"), [DOCTRINE_NAMES[index] for index in range(len(doctrines))])
                if selected is not None:
                    doctrine = doctrines[selected]
                    strength = doctrine["strength"].replace("crew death", "worker knockout").replace("enemy", "rival")
                    liability = doctrine["liability"].replace("crew death", "worker knockout").replace("enemy", "rival")
                    self._notice(DOCTRINE_NAMES[selected].upper(),
                                 dd_text("ui.archive_policy", strength=strength, liability=liability))
            else:
                records = self.jomon_state.tabletop["records"]
                if not records:
                    self._notice(dd_text("ui.record_ledger"), dd_text("ui.archive_empty"))
                else:
                    body = "\n".join(
                        dd_text("ui.archive_record", season=record["season"], result=record["result"].upper(), score_a=record["score"][0], score_b=record["score"][1], department=record["department"], patron=record["patron"])
                        for record in reversed(records))
                    self._notice(dd_text("ui.record_ledger"), body)

    def _auto_walk_to(self, destination: tuple[int, int]) -> None:
        match = self.match
        assert match is not None
        if destination == _position(match, 0):
            raise ValueError(dd_text("ui.route_already"))
        while (match["phase"] == "map" and match["winner"] is None and not match["pending"]
               and _team(match, 0)["orders"] < ORDERS_PER_TURN
               and destination != _position(match, 0)):
            leg = _ai_destination(match, destination)
            if leg is None:
                raise ValueError(dd_text("ui.route_missing"))
            walked = move_to(match, leg)
            for point in walked[:-1]:
                self._render_map(point)
                curses.napms(self.MOVE_FRAME_MS)
            if (match["phase"] == "map" and not match["pending"]
                    and _team(match, 0)["orders"] < ORDERS_PER_TURN
                    and destination != _position(match, 0)
                    and hasattr(self.screen, "nodelay") and self._route_cancel_requested()):
                self.message = dd_text("ui.route_cancelled")
                break

    def _show_patron_step(self, point: tuple[int, int]) -> None:
        self._render_map(rival_moving=point)
        curses.napms(self.MOVE_FRAME_MS)

    def run_match(self) -> None:
        match = self.match
        assert match is not None
        self.cursor = _position(match, 0)
        while True:
            if match["winner"] is not None:
                result = finish_match(self.jomon_state)
                self._notice(dd_text("ui.match_end_title"),
                             dd_text("ui.match_end_body", result=result.upper(), score_a=match["scores"][0], score_b=match["scores"][1]))
                return
            if match["turn"] == 1:
                patron_turn(match, on_move=self._show_patron_step)
                continue
            if match["pending"]:
                self._choose_pending()
                continue
            engage_if_touching(match)
            engage_neutral_if_touching(match)
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
                             *(tuple(item["position"]) for item in match.get("patrols", []) if item["active"]),
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
                        self._notice(dd_text("ui.route_unavailable"), str(exc))
            else:
                hand = _team(match, 0)["hand"]
                if normalized in (curses.KEY_LEFT, curses.KEY_UP, ord("h"), ord("k")) and hand:
                    self.selected_card = (self.selected_card - 1) % len(hand)
                elif normalized in (curses.KEY_RIGHT, curses.KEY_DOWN, ord("l"), ord("j")) and hand:
                    self.selected_card = (self.selected_card + 1) % len(hand)
                elif normalized == ord("c"):
                    self._show_card()
                elif normalized == ord("r"):
                    options = retreat_destinations(match)
                    if not options:
                        self._notice(dd_text("ui.retreat_none_title"), dd_text("ui.retreat_none_body"))
                        continue
                    chosen = self._menu(dd_text("ui.retreat_title"),
                                        [dd_text("ui.retreat_option", x=f"{x:03}", y=f"{y:02}") for x, y in options],
                                        dd_text("ui.retreat_body"))
                    if chosen is not None:
                        retreat(match, options[chosen])
                        if not match["pending"]:
                            end_turn(match)
                elif normalized == ord("v"):
                    body = "\n".join(
                        dd_text("ui.roster_line", side=("COURIER", "PATRON", "PATROL")[side], rank=actor["rank"], actor=OFFICE_ROLES[actor["role"]] if actor["role"] in OFFICE_ROLES else office_costume_name(actor["role"]), costume=(" / " + office_costume_name(rival_costumes(match["world_seed"])[index]) + " costume") if side == 1 else "", hp=actor["hp"], maximum=actor["max_hp"], stress=actor["stress"], turns=actor["respawn"])
                        for side in (0, _opponent_side(match, 0)) for index, actor in enumerate(_team(match, side)["actors"]))
                    self._notice(dd_text("ui.roster_title"), body)
                elif normalized in (10, 13, curses.KEY_ENTER) and hand:
                    targets = valid_targets(match, self.selected_card)
                    if not targets:
                        self._notice(dd_text("ui.card_illegal_title"), dd_text("ui.card_illegal_body"))
                        continue
                    target = self._select_target(targets)
                    if target is None:
                        continue
                    try:
                        play_card(match, self.selected_card, target)
                        self._render_combat_match(target if ":" in target else None)
                        curses.napms(self.DAMAGE_FLASH_MS)
                    except ValueError as exc:
                        self._notice(dd_text("ui.card_illegal_title"), str(exc))


def run_expedition(screen: curses.window, state) -> None:
    if state.courier is None or not state.courier.alive:
        state.add_message(dd_text("ui.courier_required"))
        return
    active = state.tabletop["active_match"]
    if active and active["courier_id"] != state.courier.id:
        owner = next((person for person in state.household if person.id == active["courier_id"]), None)
        if owner and not owner.alive:
            active["winner"] = 1
            finish_match(state)
            state.add_message(dd_text("ui.match_archived", owner=owner.name))
            active = None
        else:
            state.add_message(dd_text("ui.match_owner_required", owner=owner.name if owner else "Another courier"))
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
            ui._put(1, 1, dd_text("ui.terminal_size"))
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
                role = next(roles[(current + step * offset) % len(roles)] for offset in range(1, len(roles))
                            if roles[(current + step * offset) % len(roles)] not in collection["roles"])
                ui._set_worker(collection, slot, role)
            elif normalized == ord("v"):
                collection = collection_for(state, state.courier.id)
                try:
                    ui._choose_worker(collection, slot)
                except ValueError as exc:
                    message = str(exc)
            elif normalized == ord("f"):
                ui._choose_formation(collection_for(state, state.courier.id))
            elif normalized == ord("l"):
                try:
                    message = ui._toggle_loadout(collection_for(state, state.courier.id), slot)
                except ValueError as exc:
                    message = str(exc)
            elif normalized == ord("b"):
                ui._browse_archive()
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
                    message = dd_text("ui.editor_full")
                else:
                    collection["deck"].append(card_id)
            elif normalized == ord("x") and available:
                card_id = available[deck_index % len(available)]
                if len(collection["deck"]) <= 20 or card_id not in collection["deck"]:
                    message = dd_text("ui.editor_minimum")
                else:
                    collection["deck"].remove(card_id)
            elif normalized == ord("m") and available:
                card_id = available[deck_index % len(available)]
                mastery = next((item for item in load_catalog().masteries.values() if item["card_id"] == card_id), None)
                if not mastery:
                    message = dd_text("ui.editor_no_mastery")
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
                    message = dd_text("ui.editor_no_treatment")
                    continue
                current = collection["infusions"].get(card_id)
                if current == infusions[-1]:
                    collection["infusions"].pop(card_id)
                else:
                    collection["infusions"][card_id] = infusions[0] if current is None else infusions[infusions.index(current) + 1]
