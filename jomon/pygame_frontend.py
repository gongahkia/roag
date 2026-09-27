"""Small optional Pygame-ce frontend using Jomon's headless application API."""
from __future__ import annotations

import argparse
from dataclasses import dataclass, field
from hashlib import sha256
from pathlib import Path
from typing import Any

from .assets import actor_assets, asset_resource, event_assets, tavern_assets, terrain_assets
from .app_settings import AppSettings, available_saves, default_save_path, load_app_settings, resolve_renderer, save_app_settings, save_directory
from .catalog import selected_content_pack
from .font_stack import FontStack
from .commands import (
    AttackCommand, CloseTavernGameCommand, DiceActionCommand, DrawBetCommand,
    DrawExchangeCommand, DropItemCommand, EquipItemCommand, InteractCommand, MoveItemCommand, MoveCommand,
    StartTavernGameCommand, TravelCommand, UnequipItemCommand, UseGearCommand,
    ActivityCommand, CharacterSetupCommand, GuardCommand, NegotiateCommand, RetreatCommand, SetAutoPlaceCommand,
)
from .runtime_events import (
    ActorDefeated, ActorMoved, AttackResolved, DamageApplied, RuntimeEvent,
    TavernCardsExchanged, TavernDiceRolled, TavernGameSettled,
)
from .session import CommandOutcome, GameSession
from .state import Position
from .tavern_presentation import draw_phase_name, tavern_text
from .views import ActorView, CellView, InteractionOptionView, WorldView


def _pygame() -> Any:
    try:
        import pygame
    except ModuleNotFoundError as exc:
        raise RuntimeError("The Pygame frontend requires pygame-ce; run `uv sync`.") from exc
    return pygame


@dataclass
class Motion:
    actor_id: str
    start: Position
    end: Position
    elapsed: float = 0.0
    duration: float = 0.14


@dataclass
class Feedback:
    position: Position | None
    text: str
    elapsed: float = 0.0
    duration: float = 0.35


@dataclass
class CharacterSetupDraft:
    """Frontend-local draft; only its stable values cross the session boundary."""
    crew_id: str
    name: str
    ancestry_id: str
    origin_id: str
    trait_id: str
    attributes: dict[str, int]
    competencies: dict[str, int]
    cursor: int = 0


@dataclass(frozen=True)
class InspectionData:
    """Frontend-local rendering data assembled exclusively from immutable views."""
    position: Position
    terrain_id: str
    feature_ids: tuple[str, ...]
    actor: ActorView | None
    interaction: InteractionOptionView | None
    remembered: bool


@dataclass(frozen=True)
class ShellMenuItem:
    action_id: str
    label: str
    enabled: bool = True
    detail: str = ""


class ResourceCache:
    """Pygame-local media cache. Missing media produces no engine side effects."""
    def __init__(self, pygame: Any):
        self.pygame, self.images, self.sounds = pygame, {}, {}
        self.audio_ready = False
        try:
            pygame.mixer.init()
            self.audio_ready = True
        except pygame.error:
            pass

    def image(self, asset_id: str, size: int) -> Any | None:
        key = asset_id, size
        if key in self.images: return self.images[key]
        resource = asset_resource(asset_id)
        if resource is None or resource.kind != "image" or resource.path is None: return None
        try:
            surface = self.pygame.image.load(str(selected_content_pack().root / resource.path)).convert_alpha()
            surface = self.pygame.transform.smoothscale(surface, (size, size))
        except (self.pygame.error, OSError):
            return None
        self.images[key] = surface
        return surface

    def play(self, asset_id: str) -> None:
        if not self.audio_ready: return
        if asset_id in self.sounds:
            sound = self.sounds[asset_id]
        else:
            resource = asset_resource(asset_id)
            if resource is None or resource.kind != "audio" or resource.path is None: return
            try: sound = self.pygame.mixer.Sound(str(selected_content_pack().root / resource.path))
            except (self.pygame.error, OSError): return
            self.sounds[asset_id] = sound
        try: sound.play()
        except self.pygame.error: pass


class PygameFrontend:
    """Frontend-local camera, selection, resources, and transient feedback."""
    renderer_id = "debug"
    def __init__(self, session: GameSession, *, size: tuple[int, int] = (1100, 760), pygame: Any | None = None,
                 require_character_setup: bool = False, font_path: Path | None = None,
                 icon_font_path: Path | None = None, shell_mode: str = "game",
                 settings: AppSettings | None = None, settings_file: Path | None = None,
                 save_root: Path | None = None, save_path: Path | None = None, seed: str = "pygame-jomon"):
        self.pygame = pygame or _pygame(); self.session = session
        self.screen = self.pygame.display.set_mode(size, self.pygame.RESIZABLE)
        self.pygame.display.set_caption("Jomon — graphical slice")
        self.clock = self.pygame.time.Clock()
        self.font_stack = FontStack(self.pygame, 20, font_path=font_path, icon_font_path=icon_font_path)
        self.font = self.font_stack.text
        self.tile_size = 26; self.selected: Position | None = None; self.selected_actor_id: str | None = None
        self.motions: list[Motion] = []; self.feedback: list[Feedback] = []; self.resources = ResourceCache(self.pygame)
        self.running = True; self.last_result = "ready"; self.notification: Feedback | None = None
        self.panel: str | None = None; self.panel_cursor = 0; self.activity_context: str | None = None
        self.settings = settings or load_app_settings(settings_file); self.settings_file = settings_file
        self.save_root = save_root or save_directory(); self.save_path = save_path or default_save_path(); self.new_game_seed = seed
        self.settings_return_panel: str | None = None
        self.requested_renderer: str | None = None
        self.inventory_source: str | None = None
        self.marked_draw_cards: set[str] = set(); self.setup_draft: CharacterSetupDraft | None = None
        if require_character_setup:
            self._begin_character_setup()
        elif shell_mode == "title":
            self._open_panel("title")

    def _colour(self, semantic: str) -> tuple[int, int, int]:
        raw=sha256(semantic.encode()).digest(); return 45+raw[0]//3, 45+raw[1]//3, 45+raw[2]//3

    def _camera(self, view: WorldView) -> tuple[int, int]:
        width, height = self.screen.get_size(); return width//2-view.courier_position.x*self.tile_size, height//2-view.courier_position.y*self.tile_size

    def _rect(self, point: Position, camera: tuple[int,int]) -> Any:
        return self.pygame.Rect(camera[0]+point.x*self.tile_size, camera[1]+point.y*self.tile_size, self.tile_size, self.tile_size)

    def submit(self, command: object) -> CommandOutcome:
        outcome=self.session.submit(command); self.last_result=outcome.result_id; self.consume_events(outcome.events); return outcome

    def consume_events(self, events: tuple[RuntimeEvent, ...]) -> None:
        for event in events:
            if isinstance(event, ActorMoved): self.motions.append(Motion(event.actor_id,event.from_position,event.to_position))
            elif isinstance(event, DamageApplied):
                actor=self.session.actor_view(event.target_actor_id); self.feedback.append(Feedback(actor.position if actor else None, str(event.amount)))
            elif isinstance(event, (AttackResolved, ActorDefeated)):
                actor_id=event.target_id if isinstance(event, AttackResolved) else event.actor_id; actor=self.session.actor_view(actor_id)
                self.feedback.append(Feedback(actor.position if actor else None, "hit" if isinstance(event, AttackResolved) else "defeated"))
            elif isinstance(event, TavernCardsExchanged):
                self.feedback.append(Feedback(None, f"Exchanged {len(event.card_ids)} card(s)"))
            elif isinstance(event, TavernDiceRolled):
                self.feedback.append(Feedback(None, f"Rolled {event.dice[0]} + {event.dice[1]}"))
            elif isinstance(event, TavernGameSettled):
                self.feedback.append(Feedback(None, "Tavern game settled"))
            binding=event_assets(event.event_id)
            if "audio" in binding: self.resources.play(binding["audio"])

    def _draw_cell(self, cell: CellView, camera: tuple[int,int]) -> None:
        if not (cell.visible or cell.remembered): return
        rect=self._rect(cell.position,camera); colour=self._colour(cell.terrain_id)
        if not cell.visible: colour=tuple(value//3 for value in colour)
        self.pygame.draw.rect(self.screen,colour,rect)
        asset=terrain_assets(cell.terrain_id).get("image"); image=self.resources.image(asset,self.tile_size) if asset else None
        if image: self.screen.blit(image,rect)
        if cell.feature_ids: self.pygame.draw.rect(self.screen,(235,190,80),rect,1)

    def _draw_actor(self, actor: ActorView, camera: tuple[int,int]) -> None:
        if actor.position is None or not actor.alive: return
        rect=self._rect(actor.position,camera); binding=actor_assets(actor.presentation_id); image=self.resources.image(binding.get("image",""),self.tile_size)
        if image: self.screen.blit(image,rect)
        else:
            colour=(80,210,250) if actor.actor_kind=="person" else (225,75,75)
            self.pygame.draw.circle(self.screen,colour,rect.center,max(4,self.tile_size//3))
        if actor.id==self.selected_actor_id: self.pygame.draw.rect(self.screen,(255,255,255),rect,2)

    @staticmethod
    def _semantic_label(identity: str) -> str:
        """Readable frontend formatting of a stable identity, not catalog prose."""
        return identity.replace(".", " ").replace("_", " ")

    def inspection_data(self) -> InspectionData | None:
        """Project a selected known cell without querying mutable engine internals."""
        if self.selected is None:
            return None
        world = self.session.world_view()
        cell = next((row for row in world.cells if row.position == self.selected), None)
        if cell is None or not (cell.visible or cell.remembered):
            return None
        # Actors and dynamic features are current information and must not leak
        # from a remembered cell merely because the underlying state changed.
        actor = self.session.actor_view(self.selected_actor_id) if cell.visible and self.selected_actor_id in cell.actor_ids else None
        interaction = None
        if cell.visible:
            current = self.session.interaction_view()
            if current.position == cell.position:
                interaction = next((option for option in current.options if option.available), None)
        return InspectionData(
            cell.position, cell.terrain_id,
            cell.feature_ids if cell.visible else (), actor, interaction,
            cell.remembered and not cell.visible,
        )

    def _inspection_lines(self) -> tuple[str, ...]:
        data = self.inspection_data()
        if data is None:
            return ()
        lines = [f"Inspect {data.position.x}, {data.position.y}, {data.position.z:+d}",
                 f"Terrain: {self._semantic_label(data.terrain_id)}"]
        if data.feature_ids:
            lines.append("Features: " + ", ".join(self._semantic_label(value) for value in data.feature_ids))
        if data.remembered:
            lines.append("Remembered terrain; current occupants are unknown.")
        if data.actor:
            actor = data.actor
            lines.append(f"Actor: {actor.display_name} ({self._semantic_label(actor.presentation_id)})")
            if actor.role_label:
                lines.append(f"Role: {actor.role_label}")
            lines.append(f"HP: {actor.health}/{actor.max_health}")
            if actor.status_id:
                lines.append(f"Status: {self._semantic_label(actor.status_id)}")
        if data.interaction:
            lines.append(f"Interaction: {self._semantic_label(data.interaction.interaction_id)}")
        return tuple(lines)

    def _notify(self, text: str) -> None:
        self.notification = Feedback(None, text, duration=2.0)

    def _panel_rows(self):
        if self.panel in {"title", "pause", "settings", "save", "load"}:
            return self._shell_rows()
        if self.panel == "inventory": return self.session.inventory_view(self.inventory_source).items
        if self.panel == "travel": return self.session.travel_view().destinations
        if self.panel == "interaction": return self.session.interaction_view().options
        if self.panel == "tavern-draw": return self.session.tavern_draw_view().available_opponents
        if self.panel == "tavern-dice": return self.session.tavern_dice_view().available_opponents
        if self.panel == "activity" and self.activity_context:
            return self.session.activity_view(self.activity_context).options
        return ()

    def _shell_rows(self) -> tuple[ShellMenuItem, ...]:
        if self.panel == "title":
            return (
                ShellMenuItem("join", "JOIN GAME"),
                ShellMenuItem("continue", "CONTINUE", self.save_path.is_file(), "Most recent Jomon save"),
                ShellMenuItem("load", "LOAD GAME"), ShellMenuItem("settings", "SETTINGS"),
                ShellMenuItem("quit", "QUIT"),
            )
        if self.panel == "pause":
            return (ShellMenuItem("resume", "RESUME"), ShellMenuItem("save", "SAVE GAME"),
                    ShellMenuItem("settings", "SETTINGS"), ShellMenuItem("title", "RETURN TO TITLE"),
                    ShellMenuItem("quit", "QUIT"))
        if self.panel == "settings":
            return (ShellMenuItem("renderer.debug", "RENDERER: DEBUG", self.settings.renderer != "debug"),
                    ShellMenuItem("renderer.ascii", "RENDERER: ASCII", self.settings.renderer != "ascii"),
                    ShellMenuItem("back", "BACK"))
        if self.panel == "save":
            slots = tuple(ShellMenuItem(f"save.path:{path.name}", path.stem) for path in available_saves(self.save_root))
            return (ShellMenuItem("save.continue", "SAVE CONTINUE"), ShellMenuItem("save.new", "CREATE NEW SAVE"), *slots, ShellMenuItem("back", "BACK"))
        if self.panel == "load":
            slots = tuple(ShellMenuItem(f"load.path:{path.name}", path.stem) for path in available_saves(self.save_root))
            return (*slots, ShellMenuItem("back", "BACK"))
        return ()

    def _save_to(self, path: Path) -> bool:
        try:
            path.parent.mkdir(parents=True, exist_ok=True)
            saved = self.session.save(path)
        except (OSError, ValueError):
            self._notify("Save failed"); return False
        self.save_path = saved; self._notify(f"Saved {saved.name}"); return True

    def _new_save_path(self) -> Path:
        self.save_root.mkdir(parents=True, exist_ok=True)
        for number in range(1, 1000):
            path = self.save_root / f"jomon-{number:03d}.json"
            if not path.exists(): return path
        return self.save_root / "jomon-new.json"

    def _activate_shell(self, action_id: str) -> None:
        if action_id == "join":
            self.session = GameSession.create(self.new_game_seed); self._begin_character_setup(); return
        if action_id == "continue":
            try: self.session = GameSession.load(self.save_path)
            except (OSError, ValueError): self._notify("Continue save could not be loaded")
            else: self.panel = None
            return
        if action_id == "load": self._open_panel("load"); return
        if action_id == "save": self._open_panel("save"); return
        if action_id == "resume": self.panel = None; return
        if action_id == "title": self.session = GameSession.create(self.new_game_seed); self._open_panel("title"); return
        if action_id == "quit": self.running = False; return
        if action_id == "settings": self._open_panel("settings"); return
        if action_id == "back": self._open_panel(self.settings_return_panel or "title"); return
        if action_id.startswith("renderer."):
            renderer = action_id.split(".", 1)[1]
            if renderer in {"debug", "ascii"}:
                self.settings = AppSettings(renderer)
                try: save_app_settings(self.settings, self.settings_file)
                except OSError: self._notify("Renderer selected; settings could not be saved")
                self.requested_renderer = renderer; self._notify(f"Renderer set to {renderer.title()}")
            return
        if action_id == "save.continue": self._save_to(self.save_path); return
        if action_id == "save.new": self._save_to(self._new_save_path()); return
        if action_id.startswith("save.path:"):
            self._save_to(self.save_root / action_id.split(":", 1)[1]); return
        if action_id.startswith("load.path:"):
            try: self.session = GameSession.load(self.save_root / action_id.split(":", 1)[1])
            except (OSError, ValueError): self._notify("Save could not be loaded")
            else: self.panel = None

    def _begin_character_setup(self, crew_id: str | None = None) -> None:
        """Open the new-game panel using only the session's immutable setup view."""
        view = self.session.character_setup_view(crew_id)
        if not view.available or not view.crew:
            self._notify("Character setup is not available for this game")
            return
        crew = next((row for row in view.crew if row.crew_id == crew_id), view.crew[0])
        self.setup_draft = CharacterSetupDraft(
            crew.crew_id, crew.display_name, view.ancestry_ids[0], view.origin_ids[0], view.trait_ids[0],
            dict(view.default_attributes), dict(view.default_competencies),
        )
        self.panel, self.activity_context = "setup", None

    def _setup_fields(self) -> tuple[str, ...]:
        if self.setup_draft is None:
            return ()
        view = self.session.character_setup_view(self.setup_draft.crew_id)
        return ("crew", "ancestry", "origin", "trait", *view.attribute_ids, *view.competency_ids, "begin")

    def _cycle_setup_choice(self, values: tuple[str, ...], current: str, step: int) -> str:
        return values[(values.index(current) + step) % len(values)]

    def _handle_setup_key(self, event: Any) -> bool:
        """Keep character point-buy presentation local and submit one stable command."""
        draft = self.setup_draft
        if draft is None:
            return False
        p = self.pygame; view = self.session.character_setup_view(draft.crew_id); fields = self._setup_fields()
        if event.key in {p.K_UP, p.K_w}:
            draft.cursor = (draft.cursor - 1) % len(fields); return True
        if event.key in {p.K_DOWN, p.K_s}:
            draft.cursor = (draft.cursor + 1) % len(fields); return True
        field = fields[draft.cursor]
        if event.key in {p.K_LEFT, p.K_a, p.K_RIGHT, p.K_d}:
            step = -1 if event.key in {p.K_LEFT, p.K_a} else 1
            if field == "crew":
                next_id = self._cycle_setup_choice(tuple(row.crew_id for row in view.crew), draft.crew_id, step)
                self._begin_character_setup(next_id)
            elif field == "ancestry":
                draft.ancestry_id = self._cycle_setup_choice(view.ancestry_ids, draft.ancestry_id, step)
            elif field == "origin":
                draft.origin_id = self._cycle_setup_choice(view.origin_ids, draft.origin_id, step)
            elif field == "trait":
                draft.trait_id = self._cycle_setup_choice(view.trait_ids, draft.trait_id, step)
            elif field in draft.attributes:
                proposed = draft.attributes[field] + step
                spent = sum(draft.attributes.values()) - 6 * len(draft.attributes) + step
                if 4 <= proposed <= 10 and 0 <= spent <= view.attribute_points:
                    draft.attributes[field] = proposed
                else:
                    self._notify("Attribute point limit reached")
            elif field in draft.competencies:
                proposed = draft.competencies[field] + step
                spent = sum(draft.competencies.values()) + step
                if 0 <= proposed <= 5 and 0 <= spent <= view.competency_points:
                    draft.competencies[field] = proposed
                else:
                    self._notify("Competency point limit reached")
            return True
        if event.key in {p.K_RETURN, p.K_KP_ENTER} and field == "begin":
            outcome = self.submit(CharacterSetupCommand(
                draft.crew_id, draft.name, draft.ancestry_id, draft.origin_id, draft.trait_id,
                tuple(draft.attributes.items()), tuple(draft.competencies.items()),
            ))
            if outcome.accepted:
                self.panel = None; self.setup_draft = None; self._notify("Courier ready")
            else:
                self._notify("Spend every character point before beginning")
            return True
        return True

    def _open_panel(self, name: str) -> None:
        if name == "settings":
            self.settings_return_panel = self.panel
        self.panel, self.panel_cursor = name, 0
        if name != "activity": self.activity_context = None

    def _open_inventory(self, source_id: str | None = None) -> None:
        """Open a current physical source without exposing its mutable records."""
        self.inventory_source = source_id
        self._open_panel("inventory")

    def _open_current_inventory(self) -> None:
        """Prefer recoverable ground items; normal pack access remains one close/reopen away."""
        self._open_inventory("ground" if self.session.inventory_view("ground").items else None)

    def _open_activity(self, context_id: str) -> None:
        """Open a reducer-backed semantic choice surface."""
        view = self.session.activity_view(context_id)
        if not view.options:
            self._notify(f"No available {view.title.lower()} choices")
            return
        self.panel, self.panel_cursor, self.activity_context = "activity", 0, context_id

    def _draw_panel(self) -> None:
        if self.panel is None: return
        width, height = self.screen.get_size(); box = self.pygame.Rect(18, 52, min(500, width - 36), min(380, height - 80))
        self.pygame.draw.rect(self.screen, (18, 23, 34), box); self.pygame.draw.rect(self.screen, (150, 180, 220), box, 2)
        if self.panel in {"title", "pause", "settings", "save", "load"}:
            title = {"title": "J O M O N", "pause": "PAUSED", "settings": "SETTINGS", "save": "SAVE GAME", "load": "LOAD GAME"}[self.panel]
            lines = [title, ""]
            rows = self._panel_rows(); self.panel_cursor = min(self.panel_cursor, max(0, len(rows) - 1))
            for index, row in enumerate(rows):
                marker = ">" if index == self.panel_cursor else " "
                status = "" if row.enabled else " [unavailable]"
                lines.append(f"{marker} {row.label}{status}")
                if row.detail and index == self.panel_cursor: lines.append(f"   {row.detail}")
            lines.append("Enter: select · Esc: back")
            for index, line in enumerate(lines[:15]):
                self.screen.blit(self.font.render(line, True, (238, 238, 238)), (box.x + 12, box.y + 12 + index * 21))
            return
        if self.panel == "setup":
            self._draw_character_setup(box)
            return
        if self.panel == "tavern-draw":
            self._draw_tavern_draw(box)
            return
        if self.panel == "tavern-dice":
            self._draw_tavern_dice(box)
            return
        if self.panel == "quests":
            lines = ["Quests"] + [f"{quest.title} — {quest.status_id} ({quest.objective})" for quest in self.session.quest_views()]
        elif self.panel == "activity" and self.activity_context:
            activity = self.session.activity_view(self.activity_context)
            rows = activity.options; self.panel_cursor = min(self.panel_cursor, max(0, len(rows) - 1))
            lines = [activity.title]
            for index, row in enumerate(rows):
                marker = ">" if index == self.panel_cursor else " "
                state = "ready" if row.available else "unavailable"
                target = f" · target {row.target_kind_id}" if row.target_kind_id else ""
                lines.append(f"{marker} {row.label} [{state}]{target}")
            if rows:
                selected = rows[self.panel_cursor]
                lines += ["", selected.description or self._semantic_label(selected.action_id)]
                if selected.reason_id: lines.append(f"Requires: {self._semantic_label(selected.reason_id)}")
            lines.append("Enter: resolve · Esc: close")
        else:
            rows = self._panel_rows(); self.panel_cursor = min(self.panel_cursor, max(0, len(rows) - 1))
            title = {"inventory": "Inventory  [U use / E equip / R unequip / T transfer / D drop / O auto-place]", "travel": "Travel  [Enter confirms]", "interaction": "Interaction  [Enter confirms]"}[self.panel]
            lines = [title]
            for index, row in enumerate(rows):
                marker = ">" if index == self.panel_cursor else " "
                if self.panel == "inventory": lines.append(f"{marker} {row.display_name} x{row.quantity} [{row.location_id}]")
                elif self.panel == "travel": lines.append(f"{marker} {row.display_name} — {row.travel_time} turns {'ready' if row.available else 'blocked'}")
                else: lines.append(f"{marker} {row.label or self._semantic_label(row.interaction_id)}")
            if rows and self.panel == "inventory":
                selected = rows[self.panel_cursor]; lines += ["", selected.description, "Operations: " + (", ".join(selected.legal_operations) or "inspect only")]
            if rows and self.panel == "travel":
                selected = rows[self.panel_cursor]; lines += ["", selected.description, f"Hazard: {selected.hazard}; supply: {selected.supply_cost}"]
        for index, line in enumerate(lines[:15]):
            self.screen.blit(self.font.render(line, True, (238, 238, 238)), (box.x + 12, box.y + 12 + index * 21))

    def _draw_character_setup(self, box: Any) -> None:
        draft = self.setup_draft
        if draft is None:
            return
        view = self.session.character_setup_view(draft.crew_id); fields = self._setup_fields()
        crew = next(row for row in view.crew if row.crew_id == draft.crew_id)
        attribute_spent = sum(draft.attributes.values()) - 6 * len(draft.attributes)
        competency_spent = sum(draft.competencies.values())
        values: dict[str, str] = {
            "crew": f"{crew.display_name} / {crew.role_label}",
            "ancestry": self._semantic_label(draft.ancestry_id),
            "origin": self._semantic_label(draft.origin_id),
            "trait": self._semantic_label(draft.trait_id),
            **{key: str(value) for key, value in draft.attributes.items()},
            **{key: str(value) for key, value in draft.competencies.items()},
            "begin": "Start ordinary play",
        }
        lines = ["Courier setup — arrows/WASD adjust; Enter begins",
                 f"Attributes {attribute_spent}/{view.attribute_points}; competencies {competency_spent}/{view.competency_points}"]
        for index, field in enumerate(fields):
            marker = ">" if index == draft.cursor else " "
            lines.append(f"{marker} {self._semantic_label(field)}: {values[field]}")
        for index, line in enumerate(lines):
            self.screen.blit(self.font.render(line, True, (238, 238, 238)), (box.x + 12, box.y + 12 + index * 18))

    def _draw_tavern_draw(self, box: Any) -> None:
        view = self.session.tavern_draw_view()
        if not view.active:
            names = ", ".join(row.display_name for row in view.available_opponents[:3]) or "no eligible opponents"
            lines = [tavern_text("draw.ui.title.active"), "Enter: free practice    W: wagered hand", f"First three available: {names}", "Escape: return to tavern"]
            for index, line in enumerate(lines):
                self.screen.blit(self.font.render(line, True, (238, 238, 238)), (box.x + 12, box.y + 12 + index * 22))
            return
        acting = next((row.display_name for row in view.participants if row.actor_id == view.current_player_id), "settled")
        lines = [f"{tavern_text('draw.ui.title.active')} — {draw_phase_name(view.phase_id.removeprefix('draw.phase.'))}",
                 f"Credit {view.credit}; pot {view.pot}; acting {acting}",
                 " / ".join(f"{row.display_name}{'' if row.active else ' (folded)'}" for row in view.participants)]
        for index, line in enumerate(lines):
            self.screen.blit(self.font.render(line, True, (238, 238, 238)), (box.x + 12, box.y + 12 + index * 22))
        card_binding = tavern_assets("tavern.draw.card")
        image = self.resources.image(card_binding.get("image", ""), 54) if card_binding else None
        for index, card in enumerate(view.hand):
            rect = self.pygame.Rect(box.x + 14 + index * 65, box.y + 92, 56, 82)
            self.pygame.draw.rect(self.screen, (238, 238, 224), rect)
            if image: self.screen.blit(image, rect)
            self.pygame.draw.rect(self.screen, (255, 215, 80) if card.card_id in self.marked_draw_cards else (40, 40, 48), rect, 2)
            label = self.font.render(f"{index + 1}: {card.rank_label}{card.suit_label}", True, (18, 18, 24))
            self.screen.blit(label, (rect.x + 3, rect.y + 30))
        actions = ", ".join(view.legal_actions) or "waiting for the other seats"
        lines = [f"Actions: {actions}"]
        if "draw.exchange" in view.legal_actions:
            lines.append("1–5 mark cards; Enter exchanges marked cards")
        elif "draw.close" in view.legal_actions:
            winners = ", ".join(view.winner_ids) or "none"
            lines.append(f"Winners: {winners}; Enter clears result")
        else:
            lines.append("C check/call · R raise · F fold · Escape pauses")
        for index, line in enumerate(lines):
            self.screen.blit(self.font.render(line, True, (238, 238, 238)), (box.x + 12, box.y + 194 + index * 22))

    def _draw_tavern_dice(self, box: Any) -> None:
        view = self.session.tavern_dice_view()
        if not view.active:
            names = ", ".join(row.display_name for row in view.available_opponents[:3]) or "no eligible opponents"
            lines = [tavern_text("dice.ui.title.complete"), "Enter: begin", f"First three available: {names}", "Escape: return to tavern"]
        else:
            actors = " / ".join(f"{row.display_name}: {view.scores[index]}" for index, row in enumerate(view.participants))
            dice = "  ".join(str(value) for value in view.dice) or "hidden"
            lines = [f"{tavern_text('dice.ui.title.complete')} — round {view.round_number}", f"Credit {view.credit}; purse {view.purse}", actors,
                     f"Dice: {dice}; turn pot {view.turn_total}", f"Actions: {', '.join(view.legal_actions) or 'waiting for other seats'}"]
            if "dice.close" in view.legal_actions:
                lines.append("Enter clears result")
            else:
                lines.append("R roll · H hold · Escape pauses")
        for index, line in enumerate(lines):
            self.screen.blit(self.font.render(line, True, (238, 238, 238)), (box.x + 12, box.y + 12 + index * 24))
        if view.active and view.dice:
            binding = tavern_assets("tavern.dice.die")
            image = self.resources.image(binding.get("image", ""), 48) if binding else None
            for index, value in enumerate(view.dice):
                rect = self.pygame.Rect(box.x + 14 + index * 62, box.y + 164, 50, 50)
                self.pygame.draw.rect(self.screen, (238, 238, 224), rect)
                if image: self.screen.blit(image, rect)
                self.pygame.draw.rect(self.screen, (40, 40, 48), rect, 2)
                self.screen.blit(self.font.render(str(value), True, (18, 18, 24)), (rect.x + 20, rect.y + 15))

    def _handle_tavern_key(self, event: Any) -> bool:
        """Handle graphical tavern UI with only stable view records and commands."""
        p = self.pygame
        if self.panel == "tavern-draw":
            view = self.session.tavern_draw_view()
            if event.key == p.K_ESCAPE:
                self.panel = None; self.marked_draw_cards.clear(); return True
            if not view.active:
                if event.key in {p.K_RETURN, p.K_KP_ENTER, p.K_w}:
                    opponents = tuple(row.actor_id for row in view.available_opponents[:3])
                    self.submit(StartTavernGameCommand("draw", opponents, event.key == p.K_w))
                return True
            if "draw.exchange" in view.legal_actions:
                if p.K_1 <= event.key <= p.K_5:
                    index = event.key - p.K_1
                    if index < len(view.hand):
                        card_id = view.hand[index].card_id
                        if card_id in self.marked_draw_cards: self.marked_draw_cards.remove(card_id)
                        else: self.marked_draw_cards.add(card_id)
                elif event.key in {p.K_RETURN, p.K_KP_ENTER}:
                    ordered = tuple(card.card_id for card in view.hand if card.card_id in self.marked_draw_cards)
                    self.submit(DrawExchangeCommand(ordered)); self.marked_draw_cards.clear()
                return True
            if "draw.close" in view.legal_actions and event.key in {p.K_RETURN, p.K_KP_ENTER}:
                self.submit(CloseTavernGameCommand("draw")); self.panel = None; return True
            actions = {p.K_c: "draw.bet.call" if "draw.bet.call" in view.legal_actions else "draw.bet.check",
                       p.K_r: "draw.bet.raise", p.K_f: "draw.bet.fold"}
            if event.key in actions:
                self.submit(DrawBetCommand(actions[event.key]))
            return True
        if self.panel == "tavern-dice":
            view = self.session.tavern_dice_view()
            if event.key == p.K_ESCAPE:
                self.panel = None; return True
            if not view.active:
                if event.key in {p.K_RETURN, p.K_KP_ENTER}:
                    self.submit(StartTavernGameCommand("dice", tuple(row.actor_id for row in view.available_opponents[:3])))
                return True
            if "dice.close" in view.legal_actions and event.key in {p.K_RETURN, p.K_KP_ENTER}:
                self.submit(CloseTavernGameCommand("dice")); self.panel = None; return True
            if event.key == p.K_r: self.submit(DiceActionCommand("dice.roll"))
            elif event.key == p.K_h: self.submit(DiceActionCommand("dice.hold"))
            return True
        return False

    def draw(self) -> None:
        self.screen.fill((8,10,16))
        if self.panel == "title":
            self._draw_panel(); self.pygame.display.flip(); return
        view=self.session.world_view(); camera=self._camera(view)
        for cell in view.cells: self._draw_cell(cell,camera)
        courier=self._rect(view.courier_position,camera); self.pygame.draw.circle(self.screen,(245,245,255),courier.center,max(5,self.tile_size//3))
        visible = {cell.position for cell in view.cells if cell.visible}
        for actor in self.session.actor_views():
            if actor.position in visible:
                self._draw_actor(actor,camera)
        if self.selected: self.pygame.draw.rect(self.screen,(255,230,90),self._rect(self.selected,camera),2)
        for note in self.feedback:
            if note.position: self.screen.blit(self.font.render(note.text,True,(255,220,120)),self._rect(note.position,camera).move(0,-10))
        panel=self.font.render(f"HP / result: {self.last_result}   arrows/WASD move · click inspect · E interact · F attack · G guard · R retreat · V negotiate/vessel · C craft · P progress · M magic · H manoeuvres · L loadout · Ctrl+S save",True,(240,240,240))
        self.screen.blit(panel,(10,10))
        for index, line in enumerate(self._inspection_lines()):
            self.screen.blit(self.font.render(line, True, (235,235,235)), (10, 38 + index * 19))
        if self.notification:
            notice = self.font.render(self.notification.text, True, (130,240,160))
            self.screen.blit(notice, (10, self.screen.get_height() - 28))
        self._draw_panel()
        self.pygame.display.flip()

    def _select_at(self, mouse: tuple[int,int]) -> None:
        view=self.session.world_view(); camera=self._camera(view); x=(mouse[0]-camera[0])//self.tile_size; y=(mouse[1]-camera[1])//self.tile_size
        cell=next((row for row in view.cells if row.position.x==x and row.position.y==y),None)
        if cell and (cell.visible or cell.remembered):
            self.selected=cell.position
            self.selected_actor_id=cell.actor_ids[0] if cell.visible and cell.actor_ids else None

    def handle_event(self,event: Any, save_path: Path | None = None) -> None:
        p=self.pygame
        if event.type==p.QUIT: self.running=False
        elif event.type==p.MOUSEBUTTONDOWN and event.button==1:
            if self.panel in {"title", "pause", "settings", "save", "load"}:
                rows = self._panel_rows(); index = (event.pos[1] - 94) // 21
                if 0 <= index < len(rows) and rows[index].enabled: self._activate_shell(rows[index].action_id)
            else: self._select_at(event.pos)
        elif event.type==p.KEYDOWN:
            if event.key==p.K_s and event.mod & p.KMOD_CTRL:
                self._save_to(save_path or self.save_path)
                return
            if self.panel == "setup":
                self._handle_setup_key(event)
                return
            if self.panel in {"title", "pause", "settings", "save", "load"}:
                rows = self._panel_rows()
                if event.key in {p.K_UP, p.K_w}: self.panel_cursor = max(0, self.panel_cursor - 1); return
                if event.key in {p.K_DOWN, p.K_s}: self.panel_cursor = min(max(0, len(rows) - 1), self.panel_cursor + 1); return
                if event.key in {p.K_RETURN, p.K_KP_ENTER} and rows:
                    row = rows[self.panel_cursor]
                    if row.enabled: self._activate_shell(row.action_id)
                    return
                if event.key == p.K_ESCAPE:
                    if self.panel == "title": return
                    if self.panel == "settings": self._open_panel(self.settings_return_panel or "title")
                    elif self.panel in {"save", "load"}: self._open_panel("pause" if self.settings_return_panel == "pause" else "title")
                    else: self.panel = None
                    return
            if self._handle_tavern_key(event):
                return
            if self.panel:
                rows = self._panel_rows()
                if event.key in {p.K_ESCAPE, p.K_i, p.K_q}:
                    self.panel = None; self.activity_context = None; self.inventory_source = None; return
                if self.panel == "inventory" and event.key == p.K_o:
                    outcome = self.submit(SetAutoPlaceCommand(not self.session.auto_place_enabled))
                    self._notify("Auto-place enabled" if outcome.changed and self.session.auto_place_enabled else
                                 "Auto-place disabled" if outcome.changed else "Auto-place unchanged")
                    return
                if event.key in {p.K_UP, p.K_w}: self.panel_cursor = max(0, self.panel_cursor - 1); return
                if event.key in {p.K_DOWN, p.K_s}: self.panel_cursor = min(max(0, len(rows) - 1), self.panel_cursor + 1); return
                if not rows: return
                selected = rows[self.panel_cursor]
                if self.panel == "inventory":
                    if event.key == p.K_u and "use" in selected.legal_operations:
                        preparation = selected.kind_id.split(":", 1)[1] if selected.kind_id.startswith("consumable:preparation.") else None
                        self.submit(UseGearCommand(preparation))
                    elif event.key == p.K_e and "equip" in selected.legal_operations: self.submit(EquipItemCommand(selected.id))
                    elif event.key == p.K_r and "unequip" in selected.legal_operations: self.submit(UnequipItemCommand(selected.location_id))
                    elif event.key == p.K_t and "move" in selected.legal_operations:
                        self.submit(MoveItemCommand(selected.id, "locker" if selected.location_id == "pack" else "pack"))
                    elif event.key == p.K_d and "drop" in selected.legal_operations: self.submit(DropItemCommand(selected.id))
                    return
                if self.panel == "travel" and event.key in {p.K_RETURN, p.K_KP_ENTER}:
                    self.submit(TravelCommand(selected.destination_id)); self.panel = None; return
                if self.panel == "interaction" and event.key in {p.K_RETURN, p.K_KP_ENTER, p.K_e}:
                    outcome = self.submit(InteractCommand(selected.target_id, selected.interaction_id)); self.panel = None
                    if outcome.overlay_id in {"equipment", "hold"}:
                        self._open_inventory()
                    elif (outcome.overlay_id or "").startswith("inventory:container:"):
                        self._open_inventory(outcome.overlay_id.removeprefix("inventory:"))
                    elif outcome.overlay_id == "route-chart": self._open_panel("travel")
                    elif outcome.overlay_id: self._open_activity(outcome.overlay_id)
                    return
                if self.panel == "activity" and event.key in {p.K_RETURN, p.K_KP_ENTER, p.K_e}:
                    if not selected.available or self.activity_context is None:
                        self._notify("That operation is unavailable"); return
                    target_position = None
                    target_actor_id = None
                    if selected.target_kind_id == "self":
                        target_position = self.session.world_view().courier_position
                    elif selected.target_kind_id == "cell":
                        target_position = self.selected
                    elif selected.target_kind_id == "actor":
                        target_actor_id = self.selected_actor_id
                        actor = self.session.actor_view(target_actor_id) if target_actor_id else None
                        target_position = actor.position if actor else None
                    if selected.target_kind_id and target_position is None and target_actor_id is None:
                        self._notify("Select a visible target first"); return
                    if selected.action_id.startswith("story.open:"):
                        self._open_activity("household-story:" + selected.action_id.split(":", 1)[1]); return
                    if selected.action_id == "aftermath.open":
                        self._open_activity("aftermath"); return
                    if selected.action_id.startswith("aftermath.open:"):
                        self._open_activity("aftermath-contract:" + selected.action_id.split(":", 1)[1]); return
                    if selected.action_id == "workline.open":
                        self._open_activity("workline"); return
                    if selected.action_id == "support.open":
                        self._open_activity("support"); return
                    if selected.action_id == "passives.open":
                        self._open_activity("passives"); return
                    if selected.action_id == "loadout.open":
                        self._open_activity("loadout"); return
                    if selected.action_id.startswith("teach.open:"):
                        self._open_activity("teaching:" + selected.action_id.split(":", 1)[1]); return
                    outcome = self.submit(ActivityCommand(self.activity_context, selected.action_id, target_position, target_actor_id))
                    if not outcome.accepted: self._notify("That operation could not be resolved")
                    return
                return
            if event.key == p.K_ESCAPE:
                self._open_panel("pause"); return
            directions={p.K_UP:(0,-1),p.K_w:(0,-1),p.K_DOWN:(0,1),p.K_s:(0,1),p.K_LEFT:(-1,0),p.K_a:(-1,0),p.K_RIGHT:(1,0),p.K_d:(1,0)}
            if event.key in directions: self.submit(MoveCommand(*directions[event.key]))
            elif event.key==p.K_e:
                choices=tuple(row for row in self.session.interaction_view().options if row.available)
                if len(choices) == 1:
                    outcome = self.submit(InteractCommand(choices[0].target_id,choices[0].interaction_id))
                    if outcome.overlay_id == "tavern-draw": self._open_panel("tavern-draw")
                    elif outcome.overlay_id == "tavern-dice": self._open_panel("tavern-dice")
                    elif outcome.overlay_id in {"equipment", "hold"}:
                        self._open_inventory()
                    elif outcome.overlay_id and outcome.overlay_id.startswith("inventory:container:"):
                        self._open_inventory(outcome.overlay_id.removeprefix("inventory:"))
                    elif outcome.overlay_id == "route-chart": self._open_panel("travel")
                    elif outcome.overlay_id: self._open_activity(outcome.overlay_id)
                elif choices: self._open_panel("interaction")
            elif event.key==p.K_f: self.submit(AttackCommand(self.selected_actor_id, self.selected))
            elif event.key==p.K_g: self.submit(GuardCommand(self.selected_actor_id))
            elif event.key==p.K_r: self.submit(RetreatCommand())
            elif event.key==p.K_i: self._open_current_inventory()
            elif event.key==p.K_q: self._open_panel("quests")
            elif event.key==p.K_t: self._open_panel("travel")
            elif event.key==p.K_c: self._open_activity("production")
            elif event.key==p.K_p: self._open_activity("progression")
            elif event.key==p.K_m: self._open_activity("magic")
            elif event.key==p.K_h: self._open_activity("mastery")
            elif event.key==p.K_x: self._open_activity("preparation")
            elif event.key==p.K_v:
                if any(row.actor_kind == "threat" and row.status_id == "engaged" for row in self.session.actor_views()):
                    self.submit(NegotiateCommand())
                else:
                    self._open_activity("vessel")
            elif event.key==p.K_b: self._open_activity("vehicle")
            elif event.key==p.K_z: self._open_activity("materials")
            elif event.key==p.K_l: self._open_activity("loadout")
            elif event.key==p.K_BACKSLASH: self._open_activity("circuits")

    def update(self, elapsed: float) -> None:
        for motion in self.motions: motion.elapsed += elapsed
        self.motions[:]=[row for row in self.motions if row.elapsed<row.duration]
        for note in self.feedback: note.elapsed += elapsed
        self.feedback[:]=[row for row in self.feedback if row.elapsed<row.duration]
        if self.notification:
            self.notification.elapsed += elapsed
            if self.notification.elapsed >= self.notification.duration:
                self.notification = None

    def _replacement_renderer(self, renderer: str) -> "PygameFrontend":
        replacement = create_frontend(
            self.session, renderer=renderer, pygame=self.pygame, font_path=self.font_stack.resolution.text_path,
            icon_font_path=self.font_stack.resolution.icon_path, settings=self.settings,
            settings_file=self.settings_file, save_root=self.save_root, save_path=self.save_path,
            seed=self.new_game_seed,
        )
        replacement.panel, replacement.panel_cursor = self.panel, self.panel_cursor
        replacement.activity_context, replacement.inventory_source = self.activity_context, self.inventory_source
        replacement.selected, replacement.selected_actor_id = self.selected, self.selected_actor_id
        replacement.setup_draft, replacement.settings_return_panel = self.setup_draft, self.settings_return_panel
        replacement.motions, replacement.feedback, replacement.notification = self.motions, self.feedback, self.notification
        replacement.last_result = self.last_result
        return replacement

    def run(self, save_path: Path | None = None) -> None:
        active: PygameFrontend = self
        while active.running:
            elapsed=active.clock.tick(60)/1000
            for event in active.pygame.event.get(): active.handle_event(event, save_path or active.save_path)
            if active.requested_renderer and active.requested_renderer != active.renderer_id:
                active = active._replacement_renderer(active.requested_renderer)
                continue
            active.update(elapsed); active.draw()
        active.pygame.quit()


def create_frontend(session: GameSession, *, renderer: str = "debug", pygame: Any | None = None,
                    font_path: Path | None = None, icon_font_path: Path | None = None,
                    require_character_setup: bool = False, shell_mode: str = "game",
                    settings: AppSettings | None = None, settings_file: Path | None = None,
                    save_root: Path | None = None, save_path: Path | None = None,
                    seed: str = "pygame-jomon") -> PygameFrontend:
    """Create one presentation over the shared session/controller boundary."""
    if renderer in {"debug", "graphical"}:
        return PygameFrontend(session, pygame=pygame, font_path=font_path, icon_font_path=icon_font_path,
                              require_character_setup=require_character_setup, shell_mode=shell_mode, settings=settings,
                              settings_file=settings_file, save_root=save_root, save_path=save_path, seed=seed)
    if renderer == "ascii":
        from .pygame_ascii import AsciiPygameFrontend

        return AsciiPygameFrontend(session, pygame=pygame, font_path=font_path,
                                   icon_font_path=icon_font_path,
                                   require_character_setup=require_character_setup, shell_mode=shell_mode, settings=settings,
                                   settings_file=settings_file, save_root=save_root, save_path=save_path, seed=seed)
    raise ValueError(f"unsupported renderer {renderer!r}")


def main(argv: list[str] | None = None) -> None:
    parser=argparse.ArgumentParser(description="Jomon's Pygame frontend")
    group=parser.add_mutually_exclusive_group(); group.add_argument("--new", action="store_true"); group.add_argument("--load", type=Path)
    parser.add_argument("--seed", default="pygame-jomon"); parser.add_argument("--save", type=Path)
    parser.add_argument("--renderer", choices=("debug", "graphical", "ascii"))
    parser.add_argument("--font", type=Path, help="local preferred text font; never persisted")
    parser.add_argument("--icon-font", type=Path, help="local preferred icon font; never persisted")
    args=parser.parse_args(argv)
    try: pygame=_pygame()
    except RuntimeError as exc: parser.error(str(exc))
    settings = load_app_settings()
    renderer = resolve_renderer(args.renderer, settings)
    save_path = args.save or default_save_path()
    session=GameSession.load(args.load) if args.load else GameSession.create(args.seed)
    pygame.init()
    create_frontend(session, renderer=renderer, pygame=pygame, font_path=args.font,
                    icon_font_path=args.icon_font, require_character_setup=args.new,
                    shell_mode="game" if args.new or args.load else "title", settings=settings,
                    save_path=save_path, seed=args.seed).run(save_path)

if __name__ == '__main__': main()
