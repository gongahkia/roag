"""Pygame faux-terminal renderer over the shared graphical controller."""
from __future__ import annotations

from pathlib import Path
from typing import Any

from .assets import ascii_glyph
from .pygame_frontend import PygameFrontend
from .views import CellView, WorldView


_TERRAIN_FALLBACKS = {
    "terrain.vessel.wall": "#", "terrain.tavern.wall": "#",
    "terrain.vessel.floor": ".", "terrain.tavern.floor": ".",
}
_FEATURE_FALLBACKS = {
    "field.smoke": "~", "field.water": "≈", "feature.vessel.door_closed": "+",
    "feature.tavern.door_closed": "+",
}


class AsciiPygameFrontend(PygameFrontend):
    """Terminal-styled Pygame renderer; inherited input remains semantic."""
    renderer_id = "ascii"

    def __init__(self, *args: Any, font_path: str | Path | None = None,
                 icon_font_path: str | Path | None = None, **kwargs: Any):
        super().__init__(*args, font_path=font_path, icon_font_path=icon_font_path, **kwargs)

    def _cell_metrics(self) -> tuple[int, int]:
        width, height = self.font.size("M")
        return max(8, width), max(14, height)

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

    def _draw_text(self, value: str, point: tuple[int, int], colour=(225, 232, 241), *, icon: bool = False) -> None:
        self.screen.blit(self.font_stack.render(value, colour, icon=icon), point)

    def _draw_ascii_panel(self, lines: list[str]) -> None:
        if not lines:
            return
        width, height = self.screen.get_size()
        char_width, char_height = self._cell_metrics()
        max_columns = max(24, min((width - 40) // char_width, max(len(line) for line in lines) + 4))
        max_rows = min(len(lines), max(4, (height - 100) // char_height - 2))
        box = self.pygame.Rect(14, 50, max_columns * char_width, (max_rows + 2) * char_height)
        self.pygame.draw.rect(self.screen, (10, 18, 34), box)
        self.pygame.draw.rect(self.screen, (90, 165, 235), box, 1)
        self._draw_text("╔" + "═" * (max_columns - 2) + "╗", (box.x, box.y), (90, 165, 235))
        for index, line in enumerate(lines[:max_rows]):
            self._draw_text((line[:max_columns - 2]).ljust(max_columns - 2), (box.x + char_width, box.y + (index + 1) * char_height))
        self._draw_text("╚" + "═" * (max_columns - 2) + "╝", (box.x, box.y + (max_rows + 1) * char_height), (90, 165, 235))

    def _panel_lines(self) -> list[str]:
        if self.panel is None:
            return []
        if self.panel in {"title", "pause", "settings", "save", "load"}:
            title = {"title": "J O M O N", "pause": "PAUSED", "settings": "SETTINGS", "save": "SAVE GAME", "load": "LOAD GAME"}[self.panel]
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
            fields = self._setup_fields()
            return ["[ SETUP ]  arrows/WASD adjust · Enter begins", *(
                f"{'>' if index == draft.cursor else ' '} {self._semantic_label(field)}"
                for index, field in enumerate(fields)
            )]
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
        self.screen.fill((5, 10, 19))
        if self.panel == "title":
            self._draw_ascii_panel(self._panel_lines())
            self.pygame.display.flip()
            return
        view = self.session.world_view()
        camera = self._camera(view)
        visible = {cell.position for cell in view.cells if cell.visible}
        for cell in view.cells:
            if not (cell.visible or cell.remembered):
                continue
            rect = self._rect(cell.position, camera)
            colour = (105, 140, 168) if cell.visible else (45, 61, 77)
            self._draw_text(self._cell_glyph(cell), (rect.x, rect.y), colour)
        actors = {actor.id: actor for actor in self.session.actor_views()}
        for cell in view.cells:
            if cell.position not in visible or not cell.actor_ids:
                continue
            actor = actors.get(cell.actor_ids[0])
            if actor is None:
                continue
            icon = "npc" if actor.actor_kind == "person" else "threat"
            colour = (105, 226, 234) if actor.actor_kind == "person" else (245, 105, 105)
            rect = self._rect(cell.position, camera)
            self._draw_text(self.font_stack.icon(icon), (rect.x, rect.y), colour, icon=True)
        courier = self._rect(view.courier_position, camera)
        self._draw_text(self.font_stack.icon("courier"), (courier.x, courier.y), (245, 245, 255), icon=True)
        if self.selected:
            self.pygame.draw.rect(self.screen, (255, 214, 82), self._rect(self.selected, camera), 1)
        header = (f"{self.font_stack.icon('health')} JOMON  result:{self.last_result}  "
                  f"{self.font_stack.icon('inspect')} click inspect  "
                  f"{self.font_stack.icon('interact')} E interact  "
                  f"{self.font_stack.icon('inventory')} I inventory  Ctrl+S save")
        self._draw_text(header, (10, 8), (214, 231, 249), icon=True)
        for index, line in enumerate(self._inspection_lines()):
            self._draw_text(line, (10, 30 + index * self._cell_metrics()[1]), (190, 210, 226))
        if self.notification:
            self._draw_text(self.notification.text, (10, self.screen.get_height() - 26), (120, 240, 164))
        for note in self.feedback:
            if note.position:
                rect = self._rect(note.position, camera)
                self._draw_text(note.text, (rect.x, rect.y - self._cell_metrics()[1]), (255, 210, 104))
        self._draw_ascii_panel(self._panel_lines())
        self.pygame.display.flip()
