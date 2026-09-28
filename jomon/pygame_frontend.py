"""Shared Pygame application shell and Debug renderer."""
from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from .app_settings import AppSettings, default_save_path, load_app_settings, resolve_renderer, save_app_settings
from .assets import asset_binding, asset_resource
from .catalog import selected_content_pack
from .commands import AttackCommand, CharacterSetupCommand, EquipItemCommand, IntegrateNeuralRecordsCommand, InteractCommand, MoveCommand, RecoverRemainsItemCommand, SelectSuccessorCommand, UnequipItemCommand
from .font_stack import FontStack
from .session import GameSession
from .state import ContentUnavailable, Position


def _pygame():
    try:
        import pygame
        return pygame
    except ModuleNotFoundError as exc:
        raise RuntimeError("Jomon requires pygame-ce; run `uv sync`.") from exc


@dataclass(frozen=True)
class MenuItem:
    action: str
    label: str
    enabled: bool = True
    detail: str = ""


class PygameFrontend:
    """Frontend-local controls over one shared semantic session."""

    renderer_id = "debug"
    title_colour = (130, 195, 246)
    body_colour = (225, 228, 235)
    dim_colour = (130, 142, 158)
    accent_colour = (255, 220, 100)
    panel_colour = (14, 23, 37)
    background_colour = (8, 12, 20)

    def __init__(
        self,
        session: GameSession | None,
        *,
        pygame: Any | None = None,
        size: tuple[int, int] = (1100, 760),
        shell_mode: str = "game",
        settings: AppSettings | None = None,
        seed: str = "jomon",
        save_path: Path | None = None,
        **_: Any,
    ):
        self.pygame = pygame or _pygame()
        self.screen = self.pygame.display.set_mode(size, self.pygame.RESIZABLE)
        self.pygame.display.set_caption("JOMON")
        self.clock = self.pygame.time.Clock()
        self.font_stack = FontStack(self.pygame, 20)
        self.font = self.font_stack.text
        self.session = session
        self.seed = seed
        self.settings = settings or load_app_settings()
        self.panel = "title" if session is None or shell_mode == "title" else ("setup" if shell_mode == "setup" else None)
        self.running = True
        self.selected: Position | None = None
        self.last_result = "ready"
        self.tile_size = 26
        self.requested_renderer: str | None = None
        self.panel_cursor = 0
        self.setup_category = 0
        self.setup_indices = {"crew": 0, "ancestries": 0, "origins": 0, "traits": 0}
        self.inventory_cursor = 0
        self.detail_item_id: str | None = None
        self.detail_scroll = 0
        # These are presentation drafts only.  They are deliberately never saved.
        self.neural_source_item_id: str | None = None
        self.neural_site_cursor = 0
        self.neural_candidate_cursor = 0
        self.neural_selected_record_ids: list[str] = []
        self.neural_scroll = 0
        self.neural_review_signature: tuple[object, ...] | None = None
        self._sprite_cache: dict[tuple[str, tuple[int, int, int, int], tuple[int, int]], Any] = {}
        self.save_path = Path(save_path or default_save_path())
        self._menu_rects: list[tuple[Any, MenuItem]] = []

    def _camera(self, view):
        width, height = self.screen.get_size()
        return width // 2 - view.courier_position.x * self.tile_size, height // 2 - view.courier_position.y * self.tile_size

    def _rect(self, position: Position, camera: tuple[int, int]):
        return self.pygame.Rect(camera[0] + position.x * self.tile_size, camera[1] + position.y * self.tile_size, self.tile_size, self.tile_size)

    def _sprite(self, category: str, identity: str, size: tuple[int, int]):
        """Load one pack-bound atlas frame for presentation, with no game-state role."""
        binding = asset_binding(category, identity)
        resource = asset_resource(binding.get("image", ""))
        rect_text = binding.get("rect")
        if resource is None or resource.kind != "image" or not isinstance(rect_text, str):
            return None
        try:
            rect_values = tuple(int(value) for value in rect_text.split(","))
        except ValueError:
            return None
        if len(rect_values) != 4 or any(value < 0 for value in rect_values) or rect_values[2] == 0 or rect_values[3] == 0:
            return None
        path = selected_content_pack().root / (resource.path or "")
        key = (str(path), rect_values, size)
        if key in self._sprite_cache:
            return self._sprite_cache[key]
        try:
            atlas = self.pygame.image.load(str(path)).convert_alpha()
            source = self.pygame.Rect(*rect_values)
            if not atlas.get_rect().contains(source):
                return None
            surface = atlas.subsurface(source).copy()
            if surface.get_size() != size:
                surface = self.pygame.transform.scale(surface, size)
        except self.pygame.error:
            return None
        self._sprite_cache[key] = surface
        return surface

    def _blit_sprite(self, category: str, identity: str, rect: Any, *, inset: int = 0) -> bool:
        target = rect.inflate(-inset * 2, -inset * 2)
        if target.width <= 0 or target.height <= 0:
            return False
        sprite = self._sprite(category, identity, target.size)
        if sprite is None:
            return False
        self.screen.blit(sprite, target)
        return True

    def _rows(self) -> tuple[MenuItem, ...]:
        playable = selected_content_pack().playable
        if self.panel == "title":
            return (
                MenuItem("join", "JOIN GAME", playable, "No playable content pack installed" if not playable else ""),
                MenuItem("settings", "SETTINGS"),
                MenuItem("quit", "QUIT"),
            )
        if self.panel == "settings":
            return (
                MenuItem("debug", "DEBUG", self.renderer_id != "debug"),
                MenuItem("ascii", "ASCII", self.renderer_id != "ascii"),
                MenuItem("back", "BACK"),
            )
        if self.panel == "continuation" and self.session is not None:
            return tuple(MenuItem(f"successor:{row.id}", f"CONTINUE AS {row.display_name}", row.alive and not row.active,
                                  f"HP {row.health}/{row.maximum_health}") for row in self.session.crew_views()) or (MenuItem("none", "NO SUCCESSOR AVAILABLE", False),)
        if self.panel == "remains" and self.session is not None and self.selected is not None:
            member = next((row for row in self.session.crew_views() if row.position == self.selected and not row.alive), None)
            if member:
                return tuple(MenuItem(f"recover:{member.id}:{item_id}", f"RECOVER {item_id}") for item_id in member.item_ids) or (MenuItem("none", "REMAINS EMPTY", False),)
        return ()

    def _setup_view(self):
        return GameSession.pending_character_setup_view(self.seed)

    def _setup_sections(self):
        view = self._setup_view()
        return (("crew", view.crew), ("ancestries", view.ancestries), ("origins", view.origins), ("traits", view.traits))

    def _begin_setup(self) -> None:
        self.session = None
        self.setup_category = 0
        self.setup_indices = {name: 0 for name, _ in self._setup_sections()}
        self.panel = "setup"
        self.last_result = "setup.select"

    def _confirm_setup(self) -> None:
        sections = dict(self._setup_sections())
        try:
            command = CharacterSetupCommand(
                sections["crew"][self.setup_indices["crew"]].id,
                sections["ancestries"][self.setup_indices["ancestries"]].id,
                sections["origins"][self.setup_indices["origins"]].id,
                sections["traits"][self.setup_indices["traits"]].id,
            )
            self.session, outcome = GameSession.create_configured(self.seed, command)
        except (ContentUnavailable, ValueError, IndexError):
            self.last_result = "setup.rejected"
            return
        self.last_result = outcome.result_id
        self.panel = None
        self.selected = None

    def _activate(self, action: str) -> None:
        if action == "quit":
            self.running = False
        elif action == "settings":
            self.panel = "settings"
            self.panel_cursor = 0
        elif action == "back":
            self.panel = "title"
            self.panel_cursor = 0
        elif action in {"debug", "ascii"}:
            self.settings = AppSettings(action)
            save_app_settings(self.settings)
            self.requested_renderer = action
        elif action == "join":
            if selected_content_pack().playable:
                self._begin_setup()
            else:
                self.last_result = "content.unavailable"
        elif action.startswith("successor:"):
            outcome = self.submit(SelectSuccessorCommand(action.split(":", 1)[1]))
            if outcome.accepted:
                self.panel = None
        elif action.startswith("recover:"):
            _, member_id, item_id = action.split(":", 2)
            if self.submit(RecoverRemainsItemCommand(member_id, item_id)).accepted:
                self.panel = None

    def submit(self, command):
        if self.session is None:
            raise RuntimeError("no active session")
        outcome = self.session.submit(command)
        self.last_result = outcome.result_id
        if not self.session.world_view().courier_alive:
            self.panel = "continuation"
        return outcome

    def _select_at(self, position: tuple[int, int]) -> None:
        if self.session is None:
            return
        view = self.session.world_view()
        camera = self._camera(view)
        x, y = (position[0] - camera[0]) // self.tile_size, (position[1] - camera[1]) // self.tile_size
        cell = next((row for row in view.cells if (row.position.x, row.position.y) == (x, y) and (row.visible or row.remembered)), None)
        if cell is not None:
            self.selected = cell.position

    def _selected_feature_id(self) -> str | None:
        if self.session is None or self.selected is None:
            return None
        return next((row.id for row in self.session.feature_views() if row.position == self.selected and (row.visible or row.remembered)), None)

    def _selected_actor_id(self) -> str | None:
        if self.session is None or self.selected is None:
            return None
        return next((row.id for row in self.session.actor_views() if row.position == self.selected and row.actor_kind != "courier" and row.alive), None)

    def _interact_selected(self) -> None:
        feature_id = self._selected_feature_id()
        if feature_id is None:
            self.last_result = "interaction.select-feature"
            return
        self.submit(InteractCommand(feature_id))

    def _toggle_inventory_item(self) -> None:
        if self.session is None:
            return
        items = self.session.inventory_view()
        if not items:
            self.last_result = "inventory.empty"
            return
        item = items[self.inventory_cursor % len(items)]
        self.submit(UnequipItemCommand(item.id) if item.equipped else EquipItemCommand(item.id))

    def _open_inventory_detail(self) -> None:
        if self.session is None:
            return
        items = self.session.inventory_view()
        if not items:
            self.last_result = "inventory.empty"
            return
        item_id = items[self.inventory_cursor % len(items)].id
        if self.session.item_detail_view(item_id) is None:
            self.last_result = "inventory.detail-unavailable"
            return
        self.detail_item_id = item_id
        self.detail_scroll = 0
        self.panel = "item-detail"
        self.last_result = "inventory.detail-open"

    def _clear_neural_draft(self) -> None:
        self.neural_source_item_id = None
        self.neural_site_cursor = 0
        self.neural_candidate_cursor = 0
        self.neural_selected_record_ids = []
        self.neural_scroll = 0
        self.neural_review_signature = None

    def _neural_preview(self):
        """Refresh a renderer-local draft through the shared non-mutating evaluator."""
        if self.session is None or self.neural_source_item_id is None:
            return None
        probe = self.session.neural_integration_preview("", self.neural_source_item_id, ())
        if not probe.sites:
            return probe
        self.neural_site_cursor %= len(probe.sites)
        site_id = probe.sites[self.neural_site_cursor].id
        return self.session.neural_integration_preview(
            site_id, self.neural_source_item_id, tuple(self.neural_selected_record_ids),
        )

    @staticmethod
    def _neural_signature(preview) -> tuple[object, ...]:
        return (
            preview.revision,
            preview.site_id,
            preview.recipient_member_id,
            preview.destination_item_id,
            preview.source_item_id,
            preview.selected_record_ids,
            tuple((record.id, record.origin_member_id, record.definition_id) for record in preview.resulting_records),
            preview.reason_id,
        )

    def _begin_neural_integration(self) -> None:
        if self.session is None or self.detail_item_id is None:
            self.last_result = "neural.integration.source-unavailable"
            return
        detail = self.session.item_detail_view(self.detail_item_id)
        if detail is None or detail.neural_payload_state_id == "neural.none":
            self.last_result = "neural.integration.source-unavailable"
            return
        if detail.neural_payload_state_id == "neural.empty":
            self.last_result = "neural.integration.source-empty"
            return
        if detail.installed_member_id is not None:
            self.last_result = "neural.integration.source-installed"
            return
        self._clear_neural_draft()
        self.neural_source_item_id = self.detail_item_id
        preview = self._neural_preview()
        if preview is None:
            self._clear_neural_draft()
            self.last_result = "neural.integration-unavailable"
            return
        # A carried source can be reviewed away from a site, but installed,
        # ordinary, and empty entries are never presented as usable sources.
        terminal_reasons = {
            "neural.integration-unavailable",
            "neural.integration.invalid-source",
            "neural.integration.source-unavailable",
            "neural.integration.source-empty",
            "neural.integration.source-installed",
            "neural.integration.recipient-unavailable",
        }
        if preview.reason_id in terminal_reasons:
            self._clear_neural_draft()
            self.last_result = preview.reason_id
            return
        self.neural_selected_record_ids = [record.id for record in preview.retained_records]
        self.neural_candidate_cursor = 0
        self.neural_scroll = 0
        self.neural_review_signature = None
        self.panel = "neural-select"
        self.last_result = "neural.integration.select"

    def _close_neural_integration(self) -> None:
        self._clear_neural_draft()
        self.panel = "item-detail" if self.detail_item_id is not None else "inventory"
        self.last_result = "neural.integration.cancelled"

    def _toggle_neural_candidate(self) -> None:
        preview = self._neural_preview()
        if preview is None or not preview.candidate_records:
            self.last_result = preview.reason_id if preview is not None else "neural.integration-unavailable"
            return
        self.neural_candidate_cursor %= len(preview.candidate_records)
        record_id = preview.candidate_records[self.neural_candidate_cursor].id
        if record_id in self.neural_selected_record_ids:
            self.neural_selected_record_ids.remove(record_id)
        else:
            self.neural_selected_record_ids.append(record_id)
        self.neural_review_signature = None
        self.last_result = "neural.integration.selection-changed"

    def _review_neural_integration(self) -> None:
        preview = self._neural_preview()
        if preview is None:
            self.last_result = "neural.integration-unavailable"
            return
        self.neural_review_signature = self._neural_signature(preview)
        self.neural_scroll = 0
        self.panel = "neural-review"
        self.last_result = "neural.integration.review"

    def _confirm_neural_integration(self) -> None:
        preview = self._neural_preview()
        if preview is None or self.neural_review_signature is None:
            self.panel = "neural-select"
            self.last_result = "neural.integration.review-stale"
            return
        if self._neural_signature(preview) != self.neural_review_signature:
            self.neural_review_signature = None
            self.panel = "neural-select"
            self.last_result = "neural.integration.review-stale"
            return
        if not preview.confirmable:
            self.last_result = preview.reason_id
            return
        outcome = self.submit(IntegrateNeuralRecordsCommand(
            preview.site_id, preview.source_item_id, tuple(self.neural_selected_record_ids),
        ))
        self._clear_neural_draft()
        if outcome.accepted and self.panel != "continuation":
            self.panel = "item-detail"
            self.detail_item_id = preview.source_item_id
            self.detail_scroll = 0

    def _save(self) -> None:
        if self.session is None:
            self.last_result = "save.no-session"
            return
        try:
            self.session.save(self.save_path)
            self.last_result = f"save.ok:{self.save_path.name}"
        except OSError:
            self.last_result = "save.failed"

    def _handle_setup_key(self, event: Any) -> None:
        sections = self._setup_sections()
        if event.key == self.pygame.K_ESCAPE:
            self.panel = "title"
            self.last_result = "setup.cancelled"
            return
        if event.key in {self.pygame.K_LEFT, self.pygame.K_a}:
            self.setup_category = (self.setup_category - 1) % len(sections)
            return
        if event.key in {self.pygame.K_RIGHT, self.pygame.K_d}:
            self.setup_category = (self.setup_category + 1) % len(sections)
            return
        name, options = sections[self.setup_category]
        if event.key in {self.pygame.K_UP, self.pygame.K_w}:
            self.setup_indices[name] = (self.setup_indices[name] - 1) % len(options)
            return
        if event.key in {self.pygame.K_DOWN, self.pygame.K_s}:
            self.setup_indices[name] = (self.setup_indices[name] + 1) % len(options)
            return
        if event.key in {self.pygame.K_RETURN, self.pygame.K_KP_ENTER}:
            self._confirm_setup()

    def _handle_panel_key(self, event: Any) -> bool:
        if self.panel == "setup":
            self._handle_setup_key(event)
            return True
        if self.panel in {"neural-select", "neural-review"}:
            # Saving while considering a transfer saves only canonical state, never the draft.
            if event.key == self.pygame.K_s and (event.mod & self.pygame.KMOD_CTRL):
                self._save()
                return True
            preview = self._neural_preview()
            if event.key in {self.pygame.K_ESCAPE, self.pygame.K_i, self.pygame.K_d}:
                if self.panel == "neural-review":
                    self.panel = "neural-select"
                    self.neural_review_signature = None
                    self.last_result = "neural.integration.review-back"
                else:
                    self._close_neural_integration()
                return True
            if event.key in {self.pygame.K_UP, self.pygame.K_w}:
                if self.panel == "neural-select" and preview is not None and preview.candidate_records:
                    self.neural_candidate_cursor = (self.neural_candidate_cursor - 1) % len(preview.candidate_records)
                else:
                    self.neural_scroll = max(0, self.neural_scroll - 1)
                return True
            if event.key in {self.pygame.K_DOWN, self.pygame.K_s}:
                if self.panel == "neural-select" and preview is not None and preview.candidate_records:
                    self.neural_candidate_cursor = (self.neural_candidate_cursor + 1) % len(preview.candidate_records)
                else:
                    self.neural_scroll += 1
                return True
            if self.panel == "neural-select":
                if event.key in {self.pygame.K_LEFT, self.pygame.K_a} and preview is not None and preview.sites:
                    self.neural_site_cursor = (self.neural_site_cursor - 1) % len(preview.sites)
                    self.neural_review_signature = None
                elif event.key in {self.pygame.K_RIGHT, self.pygame.K_d} and preview is not None and preview.sites:
                    self.neural_site_cursor = (self.neural_site_cursor + 1) % len(preview.sites)
                    self.neural_review_signature = None
                elif event.key == self.pygame.K_SPACE:
                    self._toggle_neural_candidate()
                elif event.key == self.pygame.K_r:
                    self._review_neural_integration()
                return True
            if event.key == self.pygame.K_c:
                self._confirm_neural_integration()
            elif event.key == self.pygame.K_r:
                self.panel = "neural-select"
                self.neural_review_signature = None
                self.last_result = "neural.integration.review-back"
            return True
        if self.panel == "item-detail":
            if self.session is None or self.session.item_detail_view(self.detail_item_id or "") is None:
                self.panel = "inventory"
                self.detail_item_id = None
                self.detail_scroll = 0
                self.last_result = "inventory.detail-unavailable"
            elif event.key in {self.pygame.K_ESCAPE, self.pygame.K_i, self.pygame.K_d}:
                self.panel = "inventory"
                self.detail_scroll = 0
            elif event.key in {self.pygame.K_UP, self.pygame.K_w}:
                self.detail_scroll = max(0, self.detail_scroll - 1)
            elif event.key in {self.pygame.K_DOWN, self.pygame.K_s}:
                self.detail_scroll += 1
            elif event.key == self.pygame.K_n:
                self._begin_neural_integration()
            return True
        if self.panel == "inventory":
            items = self.session.inventory_view() if self.session else ()
            if event.key == self.pygame.K_ESCAPE or event.key == self.pygame.K_i:
                self.panel = None
            elif items and event.key in {self.pygame.K_UP, self.pygame.K_w}:
                self.inventory_cursor = (self.inventory_cursor - 1) % len(items)
            elif items and event.key in {self.pygame.K_DOWN, self.pygame.K_s}:
                self.inventory_cursor = (self.inventory_cursor + 1) % len(items)
            elif items and event.key == self.pygame.K_d:
                self._open_inventory_detail()
            elif event.key in {self.pygame.K_e, self.pygame.K_RETURN, self.pygame.K_KP_ENTER}:
                self._toggle_inventory_item()
            return True
        if self.panel == "help":
            if event.key in {self.pygame.K_ESCAPE, self.pygame.K_h}:
                self.panel = None
            return True
        rows = self._rows()
        if not rows:
            return False
        if event.key == self.pygame.K_ESCAPE:
            self.panel = "title" if self.session is None else None
            self.panel_cursor = 0
        elif event.key in {self.pygame.K_UP, self.pygame.K_w}:
            self.panel_cursor = (self.panel_cursor - 1) % len(rows)
        elif event.key in {self.pygame.K_DOWN, self.pygame.K_s}:
            self.panel_cursor = (self.panel_cursor + 1) % len(rows)
        elif event.key in {self.pygame.K_RETURN, self.pygame.K_KP_ENTER}:
            row = rows[self.panel_cursor]
            if row.enabled:
                self._activate(row.action)
            else:
                self.last_result = "menu.unavailable"
        return True

    def handle_event(self, event: Any, save_path: Path | None = None) -> None:
        if save_path is not None:
            self.save_path = Path(save_path)
        pygame = self.pygame
        if event.type == pygame.QUIT:
            self.running = False
            return
        if event.type == pygame.KEYDOWN:
            if self.panel is not None and self._handle_panel_key(event):
                return
            if self.session is None:
                return
            if event.key == pygame.K_ESCAPE:
                self.panel = "help"
                return
            if event.key == pygame.K_h:
                self.panel = "help"
                return
            if event.key == pygame.K_i:
                self.panel = "inventory"
                self.inventory_cursor = 0
                return
            if event.key == pygame.K_r:
                member = next((row for row in self.session.crew_views() if row.position == self.selected and not row.alive), None) if self.selected else None
                if member is None:
                    self.last_result = "remains.select-body"
                else:
                    self.panel = "remains"; self.panel_cursor = 0
                return
            if event.key == pygame.K_s and (event.mod & pygame.KMOD_CTRL):
                self._save()
                return
            moves = {
                pygame.K_LEFT: (-1, 0), pygame.K_RIGHT: (1, 0), pygame.K_UP: (0, -1), pygame.K_DOWN: (0, 1),
                pygame.K_a: (-1, 0), pygame.K_d: (1, 0), pygame.K_w: (0, -1), pygame.K_s: (0, 1),
            }
            if event.key in moves:
                self.submit(MoveCommand(*moves[event.key]))
            elif event.key == pygame.K_f:
                actor_id = self._selected_actor_id()
                if actor_id is None:
                    self.last_result = "attack.select-target"
                else:
                    self.submit(AttackCommand(actor_id))
            elif event.key == pygame.K_e:
                self._interact_selected()
            return
        if event.type == pygame.MOUSEBUTTONDOWN and event.button == 1:
            if self.panel in {"title", "settings"}:
                for rect, row in self._menu_rects:
                    if rect.collidepoint(event.pos):
                        if row.enabled:
                            self._activate(row.action)
                        else:
                            self.last_result = "menu.unavailable"
                        return
            elif self.panel is None:
                self._select_at(event.pos)

    def _render(self, text: str, colour: tuple[int, int, int] | None = None, *, icon: bool = False):
        return self.font_stack.render(text, colour or self.body_colour, icon=icon)

    def _wrap(self, text: str, width: int) -> list[str]:
        words = text.split()
        if not words:
            return [""]
        lines: list[str] = []
        line = words.pop(0)
        for word in words:
            trial = f"{line} {word}"
            if self.font.size(trial)[0] <= width:
                line = trial
            else:
                lines.append(line)
                line = word
        lines.append(line)
        return lines

    def _draw_box(self, rect: Any, *, border: tuple[int, int, int] | None = None) -> None:
        self.pygame.draw.rect(self.screen, self.panel_colour, rect)
        self.pygame.draw.rect(self.screen, border or self.title_colour, rect, 2)

    def _draw_menu(self, heading: str, rows: tuple[MenuItem, ...], subtitle: str = "") -> None:
        self.screen.fill(self.background_colour)
        width, height = self.screen.get_size()
        panel_width = min(width - 80, 680)
        panel_height = min(height - 80, max(360, 220 + len(rows) * 62))
        panel = self.pygame.Rect((width - panel_width) // 2, (height - panel_height) // 2, panel_width, panel_height)
        self._draw_box(panel)
        title = self._render(heading, self.title_colour)
        self.screen.blit(title, (panel.centerx - title.get_width() // 2, panel.y + 42))
        if subtitle:
            y = panel.y + 88
            for line in self._wrap(subtitle, panel.width - 72):
                surface = self._render(line, self.dim_colour)
                self.screen.blit(surface, (panel.centerx - surface.get_width() // 2, y))
                y += surface.get_height() + 4
        start_y = panel.y + panel.height // 2 - len(rows) * 24
        self._menu_rects = []
        for index, row in enumerate(rows):
            rect = self.pygame.Rect(panel.x + 56, start_y + index * 58, panel.width - 112, 42)
            selected = index == self.panel_cursor
            fill = (41, 63, 89) if selected and row.enabled else (25, 34, 47)
            self.pygame.draw.rect(self.screen, fill, rect)
            self.pygame.draw.rect(self.screen, self.accent_colour if selected else self.dim_colour, rect, 1)
            colour = self.body_colour if row.enabled else self.dim_colour
            label = self._render(row.label, colour)
            self.screen.blit(label, (rect.centerx - label.get_width() // 2, rect.y + 10))
            self._menu_rects.append((rect, row))
            if row.detail:
                detail = self._render(row.detail, (230, 180, 90))
                self.screen.blit(detail, (panel.centerx - detail.get_width() // 2, rect.bottom + 4))
        hint = self._render("Arrows/WASD select · Enter confirm · Esc back", self.dim_colour)
        self.screen.blit(hint, (panel.centerx - hint.get_width() // 2, panel.bottom - 38))

    def _draw_setup(self) -> None:
        self.screen.fill(self.background_colour)
        width, height = self.screen.get_size()
        panel = self.pygame.Rect(48, 42, width - 96, height - 84)
        self._draw_box(panel)
        title = self._render("CHARACTER SETUP", self.title_colour)
        self.screen.blit(title, (panel.centerx - title.get_width() // 2, panel.y + 26))
        subtitle = self._render("Choose stable starting selections. World creation waits for confirmation.", self.dim_colour)
        self.screen.blit(subtitle, (panel.x + 36, panel.y + 66))
        sections = self._setup_sections()
        columns = max(1, min(2, (panel.width - 72) // 310))
        row_height = 142
        for index, (name, options) in enumerate(sections):
            col, row = index % columns, index // columns
            box_width = (panel.width - 72 - (columns - 1) * 22) // columns
            rect = self.pygame.Rect(panel.x + 30 + col * (box_width + 22), panel.y + 108 + row * row_height, box_width, 118)
            selected_section = index == self.setup_category
            self.pygame.draw.rect(self.screen, (34, 52, 72) if selected_section else (20, 30, 43), rect)
            self.pygame.draw.rect(self.screen, self.accent_colour if selected_section else self.dim_colour, rect, 2 if selected_section else 1)
            heading = self._render(name.upper(), self.accent_colour if selected_section else self.body_colour)
            self.screen.blit(heading, (rect.x + 14, rect.y + 12))
            option = options[self.setup_indices[name]]
            option_text = self._render(option.display_name, self.body_colour)
            self.screen.blit(option_text, (rect.x + 14, rect.y + 45))
            modifier = self._render(f"health +{option.health_bonus}", self.dim_colour)
            self.screen.blit(modifier, (rect.x + 14, rect.y + 74))
        selected = dict(sections)
        summary = " · ".join(selected[name][self.setup_indices[name]].display_name for name, _ in sections)
        y = panel.bottom - 112
        for line in self._wrap(summary, panel.width - 72):
            surface = self._render(line, self.body_colour)
            self.screen.blit(surface, (panel.x + 36, y))
            y += surface.get_height() + 4
        hint = self._render("Left/Right category · Up/Down option · Enter Join Game · Esc Back", self.dim_colour)
        self.screen.blit(hint, (panel.x + 36, panel.bottom - 42))

    def _draw_inventory(self) -> None:
        assert self.session is not None
        self.screen.fill(self.background_colour)
        width, height = self.screen.get_size()
        panel = self.pygame.Rect(max(40, width // 5), 60, min(width - 80, 700), height - 120)
        self._draw_box(panel)
        title = self._render("INVENTORY / EQUIPMENT", self.title_colour)
        self.screen.blit(title, (panel.centerx - title.get_width() // 2, panel.y + 28))
        items = self.session.inventory_view()
        for index, item in enumerate(items):
            rect = self.pygame.Rect(panel.x + 30, panel.y + 86 + index * 56, panel.width - 60, 46)
            selected = index == self.inventory_cursor % max(1, len(items))
            self.pygame.draw.rect(self.screen, (38, 57, 77) if selected else (20, 30, 43), rect)
            self.pygame.draw.rect(self.screen, self.accent_colour if selected else self.dim_colour, rect, 1)
            state = "EQUIPPED" if item.equipped else "CARRIED"
            label = self._render(f"{item.display_name} — {state}", self.body_colour)
            self.screen.blit(label, (rect.x + 12, rect.y + 6))
            detail = self._render(item.description, self.dim_colour)
            self.screen.blit(detail, (rect.x + 12, rect.y + 25))
        hint = self._render("Up/Down select · D details · E/Enter equip or unequip · I/Esc back", self.dim_colour)
        self.screen.blit(hint, (panel.x + 30, panel.bottom - 42))

    def _detail_lines(self, detail, width: int):
        lines: list[tuple[str, tuple[int, int, int]]] = [
            (detail.display_name, self.body_colour),
            (f"Instance: {detail.id}", self.dim_colour),
            (f"Holder: {detail.custodian_display_name} ({detail.custodian_member_id})", self.dim_colour),
        ]
        if detail.description:
            lines.insert(1, (detail.description, self.dim_colour))
        if detail.installed_member_id is None:
            lines.append(("Status: CARRIED — not installed", self.accent_colour))
        else:
            lines.append((
                f"Status: INSTALLED in {detail.installed_member_display_name} ({detail.installed_member_id})",
                self.accent_colour,
            ))
        if detail.neural_payload_state_id == "neural.none":
            lines.append(("Neural payload: none (ordinary item)", self.dim_colour))
        elif detail.neural_payload_state_id == "neural.empty":
            lines.append(("Neural device: 0 records", self.body_colour))
            lines.append(("No retained record capabilities. Empty devices cannot be transfer sources.", self.dim_colour))
        else:
            lines.append((f"Neural device: {len(detail.neural_records)} records", self.body_colour))
            for record in detail.neural_records:
                lines.extend((
                    (record.display_name, self.title_colour),
                    (f"Record: {record.id}", self.dim_colour),
                    (f"Definition: {record.definition_id}", self.dim_colour),
                    (f"Origin: {record.origin_display_name} ({record.origin_member_id})", self.dim_colour),
                    (f"Grants: {', '.join(record.capability_display_names) if record.capability_display_names else 'no capability'}", self.accent_colour),
                ))
            if detail.installed_member_id == detail.custodian_member_id:
                lines.append(("Installed records grant the listed capabilities. N opens the local transfer review for a carried source.", self.dim_colour))
            else:
                lines.append(("Carried records grant no capability until retained in an installed device. N opens the local transfer review.", self.dim_colour))
        wrapped: list[tuple[str, tuple[int, int, int]]] = []
        for text, colour in lines:
            wrapped.extend((line, colour) for line in self._wrap(text, width))
        return wrapped

    def _draw_item_detail(self) -> None:
        assert self.session is not None
        self.screen.fill(self.background_colour)
        width, height = self.screen.get_size()
        panel = self.pygame.Rect(max(40, width // 6), 40, min(width - 80, 820), height - 80)
        self._draw_box(panel)
        title = self._render("ITEM DETAIL", self.title_colour)
        self.screen.blit(title, (panel.centerx - title.get_width() // 2, panel.y + 24))
        detail = self.session.item_detail_view(self.detail_item_id or "")
        if detail is None:
            message = self._render("Selected item is no longer carried.", self.dim_colour)
            self.screen.blit(message, (panel.x + 30, panel.y + 88))
        else:
            lines = self._detail_lines(detail, panel.width - 60)
            line_height = self.font.get_height() + 5
            capacity = max(1, (panel.height - 128) // line_height)
            self.detail_scroll = min(self.detail_scroll, max(0, len(lines) - capacity))
            y = panel.y + 72
            for text, colour in lines[self.detail_scroll:self.detail_scroll + capacity]:
                surface = self._render(text, colour)
                self.screen.blit(surface, (panel.x + 30, y))
                y += line_height
        hint = self._render("Up/Down scroll · N transfer source · D/I/Esc back", self.dim_colour)
        self.screen.blit(hint, (panel.x + 30, panel.bottom - 38))

    def _integration_lines(self, preview, review: bool) -> list[tuple[str, tuple[int, int, int]]]:
        lines: list[tuple[str, tuple[int, int, int]]] = []
        if preview is None:
            return [("Integration preview is unavailable.", self.dim_colour)]
        site = preview.site_display_name or preview.site_id or "No configured integration site"
        lines.extend((
            (f"Source: {preview.source_display_name or preview.source_item_id} ({preview.source_item_id})", self.body_colour),
            (f"Site: {site}", self.body_colour if any(row.id == preview.site_id and row.in_range for row in preview.sites) else self.accent_colour),
            (f"Status: {preview.reason_id}", self.accent_colour if preview.confirmable else (235, 133, 110)),
        ))
        if preview.inherited_capacity is not None:
            lines.append((f"Foreign records: {preview.foreign_slots_used}/{preview.inherited_capacity}", self.body_colour))
        if preview.protected_records:
            lines.append(("Protected own-origin records (always retained):", self.title_colour))
            lines.extend((f"  {record.display_name} — {record.origin_display_name}", self.dim_colour) for record in preview.protected_records)
        if not review:
            lines.append(("Selectable records:", self.title_colour))
            selected = set(preview.selected_record_ids)
            retained = {record.id for record in preview.retained_records}
            source = {record.id for record in preview.source_records}
            for index, record in enumerate(preview.candidate_records):
                marker = "[x]" if record.id in selected else "[ ]"
                origin = "current retained" if record.id in retained else "source"
                if record.id in retained and record.id in source:
                    origin = "retained + source copy"
                prefix = ">" if index == self.neural_candidate_cursor % max(1, len(preview.candidate_records)) else " "
                lines.append((f"{prefix}{marker} {record.display_name} — {origin}; {record.origin_display_name}", self.accent_colour if prefix == ">" else self.body_colour))
                lines.append((f"    {record.id} · {record.definition_id}", self.dim_colour))
                lines.append((f"    Grants: {', '.join(record.capability_display_names) if record.capability_display_names else 'no capability'}", self.dim_colour))
            if not preview.candidate_records:
                lines.append(("No selectable records are available from this source.", self.dim_colour))
            lines.append(("Draft only: nothing changes until Review then C confirm.", self.dim_colour))
        else:
            lines.append(("Resulting installed records:", self.title_colour))
            if preview.resulting_records:
                for record in preview.resulting_records:
                    lines.append((f"  {record.display_name} — origin {record.origin_display_name}", self.body_colour))
                    lines.append((f"    Grants: {', '.join(record.capability_display_names) if record.capability_display_names else 'no capability'}", self.dim_colour))
            else:
                lines.append(("  None (deliberate discard of foreign records).", self.accent_colour))
            if preview.removed_destination_records:
                lines.append(("Removed from this destination payload:", (235, 133, 110)))
                lines.extend((f"  {record.display_name} ({record.id})", self.body_colour) for record in preview.removed_destination_records)
            if preview.omitted_source_records:
                lines.append(("Not retained from this source payload:", (235, 133, 110)))
                lines.extend((f"  {record.display_name} ({record.id})", self.body_colour) for record in preview.omitted_source_records)
            lines.append(("On confirmation the carried source becomes empty but remains carried.", self.accent_colour))
            lines.append(("This describes only the source and destination payloads; another physical copy may exist.", self.dim_colour))
            lines.append(("Confirmation costs one turn. Only retained installed records grant the listed capabilities.", self.dim_colour))
        wrapped: list[tuple[str, tuple[int, int, int]]] = []
        for text, colour in lines:
            wrapped.extend((line, colour) for line in self._wrap(text, 700))
        return wrapped

    def _draw_neural_integration(self, review: bool) -> None:
        self.screen.fill(self.background_colour)
        width, height = self.screen.get_size()
        panel = self.pygame.Rect(max(36, width // 7), 34, min(width - 72, 820), height - 68)
        self._draw_box(panel)
        title = self._render("NEURAL TRANSFER — REVIEW" if review else "NEURAL TRANSFER — SELECT", self.title_colour)
        self.screen.blit(title, (panel.centerx - title.get_width() // 2, panel.y + 22))
        preview = self._neural_preview()
        lines = self._integration_lines(preview, review)
        line_height = self.font.get_height() + 4
        capacity = max(1, (panel.height - 126) // line_height)
        self.neural_scroll = min(self.neural_scroll, max(0, len(lines) - capacity))
        y = panel.y + 62
        for text, colour in lines[self.neural_scroll:self.neural_scroll + capacity]:
            surface = self._render(text, colour)
            self.screen.blit(surface, (panel.x + 28, y))
            y += line_height
        if review:
            hint_text = "C confirm destructive transfer · R/Esc back · Up/Down scroll"
            if preview is None or not preview.confirmable:
                hint_text = "Confirmation disabled: correct the selection · R/Esc back · Up/Down scroll"
        else:
            hint_text = "Left/Right site · Up/Down record · Space toggle · R review · Esc cancel"
        hint = self._render(hint_text, self.dim_colour)
        self.screen.blit(hint, (panel.x + 28, panel.bottom - 38))

    def _draw_help(self) -> None:
        self.screen.fill(self.background_colour)
        width, height = self.screen.get_size()
        panel = self.pygame.Rect(max(36, width // 6), max(36, height // 7), min(width - 72, 780), min(height - 72, 520))
        self._draw_box(panel)
        title = self._render("OPERATION CONTROLS", self.title_colour)
        self.screen.blit(title, (panel.centerx - title.get_width() // 2, panel.y + 28))
        lines = (
            "Arrows or WASD: walk one cell. Living actors and closed gates block movement.",
            "Click a visible or remembered cell: select a feature or target.",
            "E: use the selected local feature. F: attack the selected adjacent actor.",
            "I: inventory and equipment; D opens details. N on a carried neural source opens selection; R reviews and C confirms it.",
            "Retained installed neural records grant their listed capabilities; carried sources grant none. Ctrl+S saves canonical state only.",
            "R: recover a selected nearby dead crew member's item. H/Escape: close. Load with --load PATH.",
        )
        y = panel.y + 84
        for text in lines:
            for line in self._wrap(text, panel.width - 60):
                surface = self._render(line, self.body_colour)
                self.screen.blit(surface, (panel.x + 30, y))
                y += surface.get_height() + 6
            y += 6

    def _draw_shell(self) -> None:
        if self.panel == "setup":
            self._draw_setup()
        elif self.panel == "inventory":
            self._draw_inventory()
        elif self.panel == "item-detail":
            self._draw_item_detail()
        elif self.panel == "neural-select":
            self._draw_neural_integration(False)
        elif self.panel == "neural-review":
            self._draw_neural_integration(True)
        elif self.panel == "help":
            self._draw_help()
        elif self.panel == "title":
            self._draw_menu("JOMON", self._rows(), "A local operation prototype. Select a playable content pack to join.")
        elif self.panel == "settings":
            self._draw_menu("SETTINGS", self._rows(), "Renderer style is frontend-only and never enters a save.")
        elif self.panel == "continuation":
            self._draw_menu("OPERATIVE LOST", self._rows(), "Choose an existing living crew member. This does not advance a turn.")
        elif self.panel == "remains":
            self._draw_menu("RECOVER REMAINS", self._rows(), "Select one local carried item to recover.")
        self.pygame.display.flip()

    def _feature_view_at(self, point: Position):
        assert self.session is not None
        return next((row for row in self.session.feature_views() if row.position == point and (row.visible or row.remembered)), None)

    def _draw_world(self) -> None:
        assert self.session is not None
        self.screen.fill(self.background_colour)
        view = self.session.world_view()
        camera = self._camera(view)
        for cell in view.cells:
            if not (cell.visible or cell.remembered):
                continue
            rect = self._rect(cell.position, camera)
            colours = {
                "terrain.floor": (76, 94, 111),
                "terrain.wall": (126, 105, 90),
                "terrain.gate.closed": (154, 120, 62),
                "terrain.gate.open": (86, 152, 132),
            }
            colour = colours.get(cell.terrain_id, (76, 94, 111))
            if not cell.visible:
                colour = tuple(channel // 2 for channel in colour)
            self.pygame.draw.rect(self.screen, colour, rect)
            if cell.visible:
                self._blit_sprite("terrain", cell.terrain_id, rect)
        for feature in self.session.feature_views():
            if not (feature.visible or feature.remembered):
                continue
            rect = self._rect(feature.position, camera)
            colour = {
                "base": (102, 198, 232),
                "maintenance_latch": (238, 180, 74),
                "objective_cache": (245, 217, 112),
            }.get(feature.kind_id, self.body_colour)
            self.pygame.draw.ellipse(self.screen, (5, 8, 14), rect.inflate(-5, -15).move(0, 6))
            if not self._blit_sprite("features", f"feature.{feature.kind_id}", rect, inset=2):
                self.pygame.draw.rect(self.screen, colour, rect.inflate(-10, -10))
        for actor in self.session.actor_views():
            if actor.actor_kind == "courier":
                continue
            rect = self._rect(actor.position, camera)
            if actor.alive:
                self.pygame.draw.ellipse(self.screen, (5, 8, 14), rect.inflate(-6, -16).move(0, 7))
                if not self._blit_sprite("actors", actor.id, rect, inset=2):
                    self.pygame.draw.circle(self.screen, (230, 90, 90), rect.center, 7)
            else:
                self.pygame.draw.line(self.screen, (110, 110, 115), rect.topleft, rect.bottomright, 3)
                self.pygame.draw.line(self.screen, (110, 110, 115), rect.topright, rect.bottomleft, 3)
        courier_rect = self._rect(view.courier_position, camera)
        courier_colour = (230, 245, 255) if view.courier_alive else (140, 50, 50)
        courier = next((actor for actor in self.session.actor_views() if actor.actor_kind == "courier"), None)
        if view.courier_alive and courier is not None:
            self.pygame.draw.ellipse(self.screen, (5, 8, 14), courier_rect.inflate(-6, -16).move(0, 7))
        if not (view.courier_alive and courier is not None and self._blit_sprite("actors", courier.id, courier_rect, inset=2)):
            self.pygame.draw.circle(self.screen, courier_colour, courier_rect.center, 7)
        if self.selected is not None:
            selected_rect = self._rect(self.selected, camera)
            self.pygame.draw.rect(self.screen, (255, 247, 180), selected_rect.inflate(4, 4), 1)
            self.pygame.draw.rect(self.screen, self.accent_colour, selected_rect, 2)
        self._draw_hud()
        self.pygame.display.flip()

    def _draw_hud(self) -> None:
        assert self.session is not None
        actor = self.session.actor_view("courier")
        operations = self.session.operation_views()
        operation = operations[0] if operations else None
        lines = [f"JOMON  turn:{self.session.world_view().turn}  HP:{actor.health}/{actor.maximum_health}  result:{self.last_result}"]
        if operation:
            evidence = ",".join(operation.evidence_method_ids) or "none"
            lines.append(f"{operation.display_name} [{operation.state_id}]  evidence:{evidence}")
            lines.extend(self._wrap(operation.objective, self.screen.get_width() - 20))
        selected = self._feature_view_at(self.selected) if self.selected else None
        if selected:
            lines.append(f"Selected: {selected.display_name} — {selected.availability_id}")
            lines.extend(self._wrap(selected.description, self.screen.get_width() - 20))
        lines.append("Arrows/WASD move · click select · E interact · F attack · I inventory · H help · Ctrl+S save")
        y = 8
        for line in lines[:6]:
            surface = self._render(line, self.body_colour if not line.startswith("Selected") else self.accent_colour)
            self.screen.blit(surface, (10, y))
            y += surface.get_height() + 2

    def draw(self) -> None:
        if self.panel is not None:
            self._draw_shell()
            return
        self._draw_world()

    def replacement_renderer(self):
        """Replace presentation only; preserve the live deterministic session and shell draft."""
        replacement = create_frontend(
            self.session,
            renderer=self.requested_renderer or self.renderer_id,
            pygame=self.pygame,
            size=self.screen.get_size(),
            shell_mode="game",
            settings=self.settings,
            seed=self.seed,
            save_path=self.save_path,
        )
        # An uncommitted destructive-transfer draft never survives a renderer replacement.
        replacement.panel = "item-detail" if self.panel in {"neural-select", "neural-review"} else self.panel
        replacement.selected = self.selected
        replacement.last_result = self.last_result
        replacement.panel_cursor = self.panel_cursor
        replacement.setup_category = self.setup_category
        replacement.setup_indices = dict(self.setup_indices)
        replacement.inventory_cursor = self.inventory_cursor
        replacement.detail_item_id = self.detail_item_id
        replacement.detail_scroll = self.detail_scroll
        return replacement

    def run(self) -> None:
        while self.running:
            for event in self.pygame.event.get():
                self.handle_event(event)
            if self.requested_renderer and self.requested_renderer != self.renderer_id:
                replacement = self.replacement_renderer()
                return replacement.run()
            self.draw()
            self.clock.tick(60)
        self.pygame.quit()


def create_frontend(session: GameSession | None, *, renderer: str = "debug", **kwargs: Any):
    if renderer == "ascii":
        from .pygame_ascii import AsciiPygameFrontend
        return AsciiPygameFrontend(session, **kwargs)
    return PygameFrontend(session, **kwargs)


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--renderer", choices=("debug", "ascii", "graphical"))
    parser.add_argument("--new", action="store_true")
    parser.add_argument("--load", type=Path)
    parser.add_argument("--seed", default="jomon")
    args = parser.parse_args(argv)
    renderer = resolve_renderer(args.renderer, load_app_settings())
    session = None
    try:
        if args.load:
            session = GameSession.load(args.load)
        elif args.new and not selected_content_pack().playable:
            raise ContentUnavailable("No playable content pack installed")
    except (ContentUnavailable, ValueError) as exc:
        raise SystemExit(str(exc))
    shell_mode = "game" if session is not None else "setup" if args.new else "title"
    create_frontend(session, renderer=renderer, shell_mode=shell_mode, seed=args.seed, save_path=args.load or default_save_path()).run()
