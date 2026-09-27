"""Pygame faux-terminal renderer over the shared graphical controller."""
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Any

from .assets import ascii_glyph
from .pygame_frontend import PygameFrontend
from .session import GameSession
from .ui_presentation import ui_text
from .views import ActorView, CellView, WorldView


_TERRAIN_FALLBACKS = {
    "terrain.vessel.wall": "#", "terrain.tavern.wall": "#",
    "terrain.vessel.floor": ".", "terrain.tavern.floor": ".",
}
_FEATURE_FALLBACKS = {
    "field.smoke": "~", "field.water": "≈", "feature.vessel.door_closed": "+",
    "feature.tavern.door_closed": "+",
}


@dataclass(frozen=True)
class AsciiTheme:
    """Presentation-only semantic colour roles for the faux-terminal mode."""
    background: tuple[int, int, int] = (5, 10, 19)
    foreground: tuple[int, int, int] = (218, 230, 241)
    dim: tuple[int, int, int] = (98, 115, 132)
    title: tuple[int, int, int] = (125, 191, 246)
    terrain: tuple[int, int, int] = (130, 157, 179)
    wall: tuple[int, int, int] = (154, 132, 111)
    path: tuple[int, int, int] = (169, 178, 139)
    water: tuple[int, int, int] = (78, 156, 205)
    interactable: tuple[int, int, int] = (232, 185, 91)
    item: tuple[int, int, int] = (84, 191, 161)
    vessel: tuple[int, int, int] = (220, 145, 89)
    travel: tuple[int, int, int] = (103, 167, 233)
    player: tuple[int, int, int] = (238, 249, 255)
    friendly: tuple[int, int, int] = (103, 225, 229)
    neutral: tuple[int, int, int] = (196, 206, 215)
    hostile: tuple[int, int, int] = (242, 111, 105)
    disabled: tuple[int, int, int] = (104, 96, 105)
    objective: tuple[int, int, int] = (240, 204, 103)
    health: tuple[int, int, int] = (111, 226, 154)
    armour: tuple[int, int, int] = (139, 185, 225)
    magic: tuple[int, int, int] = (187, 138, 233)
    chemistry: tuple[int, int, int] = (114, 201, 128)
    production: tuple[int, int, int] = (225, 164, 88)
    circuit: tuple[int, int, int] = (234, 149, 83)
    success: tuple[int, int, int] = (111, 226, 154)
    warning: tuple[int, int, int] = (240, 188, 92)
    failure: tuple[int, int, int] = (242, 111, 105)
    selected_fg: tuple[int, int, int] = (255, 242, 166)
    selected_bg: tuple[int, int, int] = (52, 77, 104)
    target: tuple[int, int, int] = (255, 137, 126)


ASCII_THEME = AsciiTheme()


def _remembered(colour: tuple[int, int, int]) -> tuple[int, int, int]:
    """Dim a semantic colour without changing what the view reveals."""
    return tuple(max(18, value * 42 // 100) for value in colour)


class AsciiPygameFrontend(PygameFrontend):
    """Terminal-styled Pygame renderer; inherited input remains semantic."""
    renderer_id = "ascii"

    def __init__(self, *args: Any, font_path: str | Path | None = None,
                 icon_font_path: str | Path | None = None, **kwargs: Any):
        super().__init__(*args, font_path=font_path, icon_font_path=icon_font_path, **kwargs)
        self.theme = ASCII_THEME

    def _cell_metrics(self) -> tuple[int, int]:
        width, height = self.font.size("M")
        return max(8, width), max(14, height)

    def _title_row_at(self, mouse: tuple[int, int]) -> int:
        """Map clicks into the centered ASCII landing-page menu."""
        width, height = self.screen.get_size()
        char_width, char_height = self._cell_metrics()
        max_columns = max(32, (width - 80) // char_width)
        max_rows = max(14, (height - 80) // char_height - 2)
        box = self.pygame.Rect(
            (width - max_columns * char_width) // 2,
            (height - (max_rows + 2) * char_height) // 2,
            max_columns * char_width,
            (max_rows + 2) * char_height,
        )
        menu_y = box.y + max_rows * char_height // 2
        return (mouse[1] - menu_y) // (2 * char_height)

    def _camera(self, view: WorldView) -> tuple[int, int]:
        cell_width, cell_height = self._cell_metrics()
        width, height = self.screen.get_size()
        return width // 2 - view.courier_position.x * cell_width, height // 2 - view.courier_position.y * cell_height

    def _rect(self, point, camera):
        cell_width, cell_height = self._cell_metrics()
        return self.pygame.Rect(camera[0] + point.x * cell_width, camera[1] + point.y * cell_height, cell_width, cell_height)

    @staticmethod
    def _region_token(cell: CellView) -> str:
        prefix = "terrain.region.token."
        if cell.terrain_id.startswith(prefix):
            try:
                return chr(int(cell.terrain_id.removeprefix(prefix), 16))
            except ValueError:
                pass
        return _TERRAIN_FALLBACKS.get(cell.terrain_id, ".")

    def _cell_glyph(self, cell: CellView) -> str:
        if cell.feature_ids:
            for feature in cell.feature_ids:
                if feature in _FEATURE_FALLBACKS:
                    return _FEATURE_FALLBACKS[feature]
        if cell.topology_id:
            return ascii_glyph(cell.topology_id, _TERRAIN_FALLBACKS.get(cell.terrain_id, "."))
        return self._region_token(cell)

    def _cell_colour(self, cell: CellView) -> tuple[int, int, int]:
        """Choose a renderer-only role from semantic terrain and features."""
        identities = (cell.terrain_id, cell.topology_id or "", *cell.feature_ids)
        joined = " ".join(identities).lower()
        if "water" in joined or "flood" in joined:
            colour = self.theme.water
        elif "wall" in joined or "structure" in joined:
            colour = self.theme.wall
        elif "path" in joined or "passage" in joined or "floor" in joined:
            colour = self.theme.path
        elif any(token in joined for token in ("exit", "gangplank", "route", "travel", "door")):
            colour = self.theme.travel
        elif any(token in joined for token in ("vessel", "engine", "station")):
            colour = self.theme.vessel
        elif any(token in joined for token in ("item", "loot", "container")):
            colour = self.theme.item
        elif cell.feature_ids:
            colour = self.theme.interactable
        else:
            colour = self.theme.terrain
        return colour if cell.visible else _remembered(colour)

    def _actor_colour(self, actor: ActorView) -> tuple[int, int, int]:
        if not actor.alive:
            return self.theme.disabled
        if actor.actor_kind == "person":
            return self.theme.friendly
        if actor.actor_kind in {"neutral", "wildlife"}:
            return self.theme.neutral
        return self.theme.hostile

    def _result_colour(self, result_id: str) -> tuple[int, int, int]:
        if result_id.endswith((".ok", ".resolved", ".completed")):
            return self.theme.success
        if result_id.endswith((".rejected", ".invalid", ".failed")):
            return self.theme.failure
        return self.theme.foreground

    def _draw_text(self, value: str, point: tuple[int, int], colour: tuple[int, int, int] | None = None, *, icon: bool = False) -> None:
        colour = colour or self.theme.foreground
        self.screen.blit(self.font_stack.render(value, colour, icon=icon), point)

    def _draw_runs(self, runs: tuple[tuple[str, tuple[int, int, int], bool], ...], point: tuple[int, int]) -> None:
        x, y = point
        for value, colour, icon in runs:
            surface = self.font_stack.render(value, colour, icon=icon)
            self.screen.blit(surface, (x, y))
            x += surface.get_width()

    def _draw_ascii_panel(self, lines: list[str]) -> None:
        if not lines:
            return
        width, height = self.screen.get_size()
        char_width, char_height = self._cell_metrics()
        full_page = self.panel in {"title", "setup"}
        if full_page:
            # Application pages intentionally occupy the available Pygame
            # window; terminal styling is a composition, not an 80×24 box.
            max_columns = max(32, (width - 80) // char_width)
            max_rows = max(14, (height - 80) // char_height - 2)
            box = self.pygame.Rect((width - max_columns * char_width) // 2, (height - (max_rows + 2) * char_height) // 2,
                                   max_columns * char_width, (max_rows + 2) * char_height)
        else:
            max_columns = max(24, min((width - 40) // char_width, max(len(line) for line in lines) + 4))
            max_rows = min(len(lines), max(4, (height - 100) // char_height - 2))
            box = self.pygame.Rect(14, 50, max_columns * char_width, (max_rows + 2) * char_height)
        self.pygame.draw.rect(self.screen, (10, 18, 34), box)
        self.pygame.draw.rect(self.screen, self.theme.title, box, 1)
        self._draw_text("╔" + "═" * (max_columns - 2) + "╗", (box.x, box.y), self.theme.title)
        if self.panel == "title":
            # The landing page is deliberately composed across the full page
            # instead of treating the terminal motif as a small upper-left widget.
            heading = ui_text("ui.title.game")
            subtitle = ui_text("ui.title.subtitle")
            heading_surface = self.font_stack.render(heading, self.theme.title)
            subtitle_surface = self.font_stack.render(subtitle, self.theme.dim)
            heading_y = box.y + max_rows * char_height // 5
            self.screen.blit(heading_surface, (box.centerx - heading_surface.get_width() // 2, heading_y))
            self.screen.blit(subtitle_surface, (box.centerx - subtitle_surface.get_width() // 2, heading_y + 2 * char_height))
            rows = self._panel_rows()
            menu_y = box.y + max_rows * char_height // 2
            for index, row in enumerate(rows):
                marker = ">" if index == self.panel_cursor else " "
                suffix = "" if row.enabled else " [locked]"
                value = f"{marker} {row.label}{suffix}"
                colour = self.theme.selected_fg if index == self.panel_cursor and row.enabled else self.theme.dim if not row.enabled else self.theme.foreground
                surface = self.font_stack.render(value, colour)
                self.screen.blit(surface, (box.centerx - surface.get_width() // 2, menu_y + index * 2 * char_height))
                if row.detail and index == self.panel_cursor:
                    detail = self.font_stack.render(row.detail, self.theme.dim)
                    self.screen.blit(detail, (box.centerx - detail.get_width() // 2, menu_y + (len(rows) + 1) * 2 * char_height))
            hint = self.font_stack.render("Enter selects · Esc remains here", self.theme.dim)
            self.screen.blit(hint, (box.centerx - hint.get_width() // 2, box.bottom - 2 * char_height))
        else:
            for index, line in enumerate(lines[:max_rows]):
                colour = self.theme.title if index == 0 else self.theme.selected_fg if line.startswith(">") else self.theme.dim if ("Enter" in line or "Esc" in line or "locked" in line) else self.theme.foreground
                self._draw_text((line[:max_columns - 2]).ljust(max_columns - 2), (box.x + char_width, box.y + (index + 1) * char_height), colour)
        self._draw_text("╚" + "═" * (max_columns - 2) + "╝", (box.x, box.y + (max_rows + 1) * char_height), self.theme.title)

    def _panel_lines(self) -> list[str]:
        if self.panel is None:
            return []
        if self.panel in {"title", "pause", "settings", "save", "load"}:
            title = {"title": ui_text("ui.title.game"), "pause": "PAUSED", "settings": "SETTINGS", "save": "SAVE GAME", "load": "LOAD GAME"}[self.panel]
            rows = self._panel_rows(); self.panel_cursor = min(self.panel_cursor, max(0, len(rows) - 1))
            lines = [f"[ {title} ]", ""]
            for index, row in enumerate(rows):
                marker = ">" if index == self.panel_cursor else " "
                lines.append(f"{marker} {row.label}{'' if row.enabled else ' [locked]'}")
                if row.detail and index == self.panel_cursor: lines.append(f"  {row.detail}")
            return [*lines, "Enter selects · Esc backs"]
        if self.panel == "setup":
            draft = self.setup_draft
            if draft is None:
                return []
            view = GameSession.pending_character_setup_view(self.new_game_seed, draft.crew_id)
            fields = self._setup_fields()
            crew = next(row for row in view.crew if row.crew_id == draft.crew_id)
            values = {
                "crew": f"{crew.display_name} / {crew.role_label}",
                "ancestry": view.option(draft.ancestry_id).display_name if view.option(draft.ancestry_id) else draft.ancestry_id,
                "origin": view.option(draft.origin_id).display_name if view.option(draft.origin_id) else draft.origin_id,
                "trait": view.option(draft.trait_id).display_name if view.option(draft.trait_id) else draft.trait_id,
                **{key: str(value) for key, value in draft.attributes.items()},
                **{key: str(value) for key, value in draft.competencies.items()},
                "begin": "Join game",
            }
            lines = ["[ CHARACTER SETUP ]", "arrows/WASD adjust · Enter joins · Esc returns"]
            lines.extend(
                f"{'>' if index == draft.cursor else ' '} {self._semantic_label(field)}: {values[field]}"
                for index, field in enumerate(fields)
            )
            field = fields[draft.cursor] if fields else ""
            selected_id = {"ancestry": draft.ancestry_id, "origin": draft.origin_id, "trait": draft.trait_id}.get(field)
            option = view.option(selected_id) if selected_id else None
            if option and option.description:
                lines += ["", option.description]
            return lines
        if self.panel == "quests":
            return ["[ QUESTS ]", *(f"{self.font_stack.icon('quest')} {row.title}: {row.objective}" for row in self.session.quest_views()), "Esc closes"]
        if self.panel == "tavern-draw":
            view = self.session.tavern_draw_view()
            if not view.active:
                return [f"[ {self.font_stack.icon('draw')} DRAW ]", "Enter begins · W wagers · Esc returns"]
            cards = " ".join(f"{card.rank_label}{card.suit_label}" for card in view.hand)
            return [f"[ {self.font_stack.icon('draw')} DRAW ] credit {view.credit} pot {view.pot}", cards, f"Actions: {', '.join(view.legal_actions) or 'waiting'}"]
        if self.panel == "tavern-dice":
            view = self.session.tavern_dice_view()
            return [f"[ {self.font_stack.icon('dice')} DICE ] credit {view.credit} purse {view.purse}",
                    f"Dice: {' '.join(map(str, view.dice)) or 'hidden'}  total {view.turn_total}",
                    f"Actions: {', '.join(view.legal_actions) or 'waiting'}"]
        if self.panel == "activity" and self.activity_context:
            view = self.session.activity_view(self.activity_context)
            rows = view.options
            self.panel_cursor = min(self.panel_cursor, max(0, len(rows) - 1))
            return [f"[ {view.title.upper()} ]", *(
                f"{'>' if index == self.panel_cursor else ' '} {row.label} {'[ready]' if row.available else '[locked]'}"
                for index, row in enumerate(rows)
            ), "Enter resolves · Esc closes"]
        rows = self._panel_rows()
        self.panel_cursor = min(self.panel_cursor, max(0, len(rows) - 1))
        title = {"inventory": "inventory", "travel": "travel", "interaction": "interact"}.get(self.panel, self.panel)
        lines = [f"[ {self.font_stack.icon('inventory' if self.panel == 'inventory' else 'travel' if self.panel == 'travel' else 'interact')} {title.upper()} ]"]
        for index, row in enumerate(rows):
            marker = ">" if index == self.panel_cursor else " "
            if self.panel == "inventory":
                lines.append(f"{marker} {row.display_name} x{row.quantity} [{row.location_id}]")
            elif self.panel == "travel":
                lines.append(f"{marker} {row.display_name} {'ready' if row.available else 'blocked'}")
            else:
                lines.append(f"{marker} {row.label or self._semantic_label(row.interaction_id)}")
        lines.append("Enter confirms · Esc closes")
        return lines

    def draw(self) -> None:
        self.screen.fill(self.theme.background)
        # Landing and initial setup deliberately have no session/world behind
        # them.  They are application pages rendered from shell/draft state.
        if self.panel in {"title", "setup"}:
            self._draw_ascii_panel(self._panel_lines())
            self.pygame.display.flip()
            return
        session = self._require_session()
        view = session.world_view()
        camera = self._camera(view)
        visible = {cell.position for cell in view.cells if cell.visible}
        for cell in view.cells:
            if not (cell.visible or cell.remembered):
                continue
            rect = self._rect(cell.position, camera)
            if self.selected == cell.position:
                self.pygame.draw.rect(self.screen, self.theme.selected_bg, rect)
            colour = self._cell_colour(cell)
            self._draw_text(self._cell_glyph(cell), (rect.x, rect.y), colour)
        actors = {actor.id: actor for actor in session.actor_views()}
        for cell in view.cells:
            if cell.position not in visible or not cell.actor_ids:
                continue
            actor = actors.get(cell.actor_ids[0])
            if actor is None:
                continue
            icon = "npc" if actor.actor_kind == "person" else "threat"
            colour = self._actor_colour(actor)
            rect = self._rect(cell.position, camera)
            self._draw_text(self.font_stack.icon(icon), (rect.x, rect.y), colour, icon=True)
        courier = self._rect(view.courier_position, camera)
        self._draw_text(self.font_stack.icon("courier"), (courier.x, courier.y), self.theme.player, icon=True)
        if self.selected:
            self.pygame.draw.rect(self.screen, self.theme.selected_fg, self._rect(self.selected, camera), 1)
        self._draw_runs((
            (self.font_stack.icon("health") + " ", self.theme.health, True),
            (ui_text("ui.title.game") + "  ", self.theme.title, False),
            (f"result:{self.last_result}  ", self._result_colour(self.last_result), False),
            (self.font_stack.icon("inspect") + " click inspect  ", self.theme.interactable, True),
            (self.font_stack.icon("interact") + " E interact  ", self.theme.objective, True),
            (self.font_stack.icon("inventory") + " I inventory  Ctrl+S save", self.theme.item, True),
        ), (10, 8))
        for index, line in enumerate(self._inspection_lines()):
            colour = self.theme.objective if line.startswith("Interaction:") else self.theme.health if line.startswith("HP:") else self.theme.dim
            self._draw_text(line, (10, 30 + index * self._cell_metrics()[1]), colour)
        if self.notification:
            self._draw_text(self.notification.text, (10, self.screen.get_height() - 26), self._result_colour(self.notification.text.lower().replace(" ", ".")))
        for note in self.feedback:
            if note.position:
                rect = self._rect(note.position, camera)
                self._draw_text(note.text, (rect.x, rect.y - self._cell_metrics()[1]), self.theme.warning)
        self._draw_ascii_panel(self._panel_lines())
        self.pygame.display.flip()
