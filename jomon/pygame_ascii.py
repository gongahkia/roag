"""ASCII skin over the same session, views, commands, and tile grid."""
from __future__ import annotations

from .assets import ascii_glyph
from .pygame_frontend import PygameFrontend


class AsciiPygameFrontend(PygameFrontend):
    renderer_id = "ascii"
    title_colour = (142, 210, 255)
    body_colour = (208, 224, 236)
    dim_colour = (114, 142, 164)
    accent_colour = (255, 222, 120)
    panel_colour = (7, 16, 29)
    background_colour = (4, 9, 17)

    def _draw_world(self) -> None:
        assert self.session is not None
        self.screen.fill(self.background_colour)
        view = self.session.world_view()
        camera = self._camera(view)
        for cell in view.cells:
            if not (cell.visible or cell.remembered):
                continue
            rect = self._rect(cell.position, camera)
            fallback = {
                "terrain.wall": "#",
                "terrain.floor": ".",
                "terrain.gate.closed": "+",
                "terrain.gate.open": "/",
            }.get(cell.terrain_id, ".")
            glyph = ascii_glyph(cell.terrain_id, fallback)
            colour = {
                "terrain.wall": (154, 132, 111),
                "terrain.floor": (130, 157, 179),
                "terrain.gate.closed": (234, 174, 79),
                "terrain.gate.open": (97, 201, 154),
            }.get(cell.terrain_id, self.body_colour)
            if not cell.visible:
                colour = tuple(max(22, channel * 3 // 5) for channel in colour)
            surface = self._render(glyph, colour)
            self.screen.blit(surface, surface.get_rect(center=rect.center))
        for feature in self.session.feature_views():
            if not (feature.visible or feature.remembered) or feature.kind_id == "access_gate":
                continue
            rect = self._rect(feature.position, camera)
            fallback = {"base": "H", "maintenance_latch": "L", "objective_cache": "O"}.get(feature.kind_id, "?")
            glyph = ascii_glyph(f"feature.{feature.kind_id}", fallback)
            colour = {"base": (102, 204, 244), "maintenance_latch": (244, 188, 87), "objective_cache": (255, 225, 123)}.get(feature.kind_id, self.body_colour)
            if not feature.visible:
                colour = tuple(max(22, channel * 3 // 5) for channel in colour)
            surface = self._render(glyph, colour)
            self.screen.blit(surface, surface.get_rect(center=rect.center))
        for actor in self.session.actor_views():
            if actor.actor_kind == "courier":
                continue
            rect = self._rect(actor.position, camera)
            glyph = self.font_stack.icon("threat") if actor.alive else "x"
            colour = (242, 111, 105) if actor.alive else (110, 110, 120)
            surface = self._render(glyph, colour, icon=actor.alive)
            self.screen.blit(surface, surface.get_rect(center=rect.center))
        courier_rect = self._rect(view.courier_position, camera)
        glyph = self.font_stack.icon("courier") if view.courier_alive else "x"
        surface = self._render(glyph, (238, 249, 255) if view.courier_alive else (175, 66, 66), icon=view.courier_alive)
        self.screen.blit(surface, surface.get_rect(center=courier_rect.center))
        if self.selected is not None:
            self.pygame.draw.rect(self.screen, self.accent_colour, self._rect(self.selected, camera), 1)
        self._draw_hud()
        self.pygame.display.flip()
