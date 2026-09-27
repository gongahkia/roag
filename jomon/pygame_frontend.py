"""Small optional Pygame-ce frontend using Jomon's headless application API."""
from __future__ import annotations

import argparse
from dataclasses import dataclass, field
from hashlib import sha256
from pathlib import Path
from typing import Any

from .assets import actor_assets, asset_resource, event_assets, terrain_assets
from .catalog import selected_content_pack
from .commands import AttackCommand, EquipItemCommand, InteractCommand, MoveCommand, TravelCommand, UnequipItemCommand
from .runtime_events import ActorDefeated, ActorMoved, AttackResolved, DamageApplied, RuntimeEvent
from .session import CommandOutcome, GameSession
from .state import Position
from .views import ActorView, CellView, InteractionOptionView, WorldView


def _pygame() -> Any:
    try:
        import pygame
    except ModuleNotFoundError as exc:
        raise RuntimeError("The graphical frontend requires optional dependency pygame-ce; install requirements-pygame.txt.") from exc
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


@dataclass(frozen=True)
class InspectionData:
    """Frontend-local rendering data assembled exclusively from immutable views."""
    position: Position
    terrain_id: str
    feature_ids: tuple[str, ...]
    actor: ActorView | None
    interaction: InteractionOptionView | None
    remembered: bool


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
    def __init__(self, session: GameSession, *, size: tuple[int, int] = (1100, 760), pygame: Any | None = None):
        self.pygame = pygame or _pygame(); self.session = session
        self.screen = self.pygame.display.set_mode(size, self.pygame.RESIZABLE)
        self.pygame.display.set_caption("Jomon — graphical slice")
        self.clock = self.pygame.time.Clock(); self.font = self.pygame.font.Font(None, 20)
        self.tile_size = 26; self.selected: Position | None = None; self.selected_actor_id: str | None = None
        self.motions: list[Motion] = []; self.feedback: list[Feedback] = []; self.resources = ResourceCache(self.pygame)
        self.running = True; self.last_result = "ready"; self.notification: Feedback | None = None
        self.panel: str | None = None; self.panel_cursor = 0

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
        if self.panel == "inventory": return self.session.inventory_view().items
        if self.panel == "travel": return self.session.travel_view().destinations
        if self.panel == "interaction": return self.session.interaction_view().options
        return ()

    def _open_panel(self, name: str) -> None:
        self.panel, self.panel_cursor = name, 0

    def _draw_panel(self) -> None:
        if self.panel is None: return
        width, height = self.screen.get_size(); box = self.pygame.Rect(18, 52, min(500, width - 36), min(380, height - 80))
        self.pygame.draw.rect(self.screen, (18, 23, 34), box); self.pygame.draw.rect(self.screen, (150, 180, 220), box, 2)
        if self.panel == "quests":
            lines = ["Quests"] + [f"{quest.title} — {quest.status_id} ({quest.objective})" for quest in self.session.quest_views()]
        else:
            rows = self._panel_rows(); self.panel_cursor = min(self.panel_cursor, max(0, len(rows) - 1))
            title = {"inventory": "Inventory  [U use / E equip / R unequip]", "travel": "Travel  [Enter confirms]", "interaction": "Interaction  [Enter confirms]"}[self.panel]
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

    def draw(self) -> None:
        self.screen.fill((8,10,16)); view=self.session.world_view(); camera=self._camera(view)
        for cell in view.cells: self._draw_cell(cell,camera)
        courier=self._rect(view.courier_position,camera); self.pygame.draw.circle(self.screen,(245,245,255),courier.center,max(5,self.tile_size//3))
        visible = {cell.position for cell in view.cells if cell.visible}
        for actor in self.session.actor_views():
            if actor.position in visible:
                self._draw_actor(actor,camera)
        if self.selected: self.pygame.draw.rect(self.screen,(255,230,90),self._rect(self.selected,camera),2)
        for note in self.feedback:
            if note.position: self.screen.blit(self.font.render(note.text,True,(255,220,120)),self._rect(note.position,camera).move(0,-10))
        panel=self.font.render(f"HP / result: {self.last_result}   arrows/WASD move · click inspect · E interact · F attack · Ctrl+S save",True,(240,240,240))
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
        elif event.type==p.MOUSEBUTTONDOWN and event.button==1: self._select_at(event.pos)
        elif event.type==p.KEYDOWN:
            if event.key==p.K_s and event.mod & p.KMOD_CTRL:
                if save_path is None:
                    self._notify("Save unavailable")
                else:
                    try:
                        saved = self.session.save(save_path)
                    except (OSError, ValueError):
                        self._notify("Save failed")
                    else:
                        self._notify(f"Saved {saved.name}")
                return
            if self.panel:
                rows = self._panel_rows()
                if event.key in {p.K_ESCAPE, p.K_i, p.K_q, p.K_t}:
                    self.panel = None; return
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
                    return
                if self.panel == "travel" and event.key in {p.K_RETURN, p.K_KP_ENTER}:
                    self.submit(TravelCommand(selected.destination_id)); self.panel = None; return
                if self.panel == "interaction" and event.key in {p.K_RETURN, p.K_KP_ENTER, p.K_e}:
                    self.submit(InteractCommand(selected.target_id, selected.interaction_id)); self.panel = None; return
                return
            directions={p.K_UP:(0,-1),p.K_w:(0,-1),p.K_DOWN:(0,1),p.K_s:(0,1),p.K_LEFT:(-1,0),p.K_a:(-1,0),p.K_RIGHT:(1,0),p.K_d:(1,0)}
            if event.key in directions: self.submit(MoveCommand(*directions[event.key]))
            elif event.key==p.K_e:
                choices=tuple(row for row in self.session.interaction_view().options if row.available)
                if len(choices) == 1: self.submit(InteractCommand(choices[0].target_id,choices[0].interaction_id))
                elif choices: self._open_panel("interaction")
            elif event.key==p.K_f: self.submit(AttackCommand(self.selected_actor_id))
            elif event.key==p.K_i: self._open_panel("inventory")
            elif event.key==p.K_q: self._open_panel("quests")
            elif event.key==p.K_t: self._open_panel("travel")

    def update(self, elapsed: float) -> None:
        for motion in self.motions: motion.elapsed += elapsed
        self.motions[:]=[row for row in self.motions if row.elapsed<row.duration]
        for note in self.feedback: note.elapsed += elapsed
        self.feedback[:]=[row for row in self.feedback if row.elapsed<row.duration]
        if self.notification:
            self.notification.elapsed += elapsed
            if self.notification.elapsed >= self.notification.duration:
                self.notification = None

    def run(self, save_path: Path | None = None) -> None:
        while self.running:
            elapsed=self.clock.tick(60)/1000
            for event in self.pygame.event.get(): self.handle_event(event,save_path)
            self.update(elapsed); self.draw()
        self.pygame.quit()


def main(argv: list[str] | None = None) -> None:
    parser=argparse.ArgumentParser(description="Jomon's optional Pygame-ce graphical slice")
    group=parser.add_mutually_exclusive_group(); group.add_argument("--new", action="store_true"); group.add_argument("--load", type=Path)
    parser.add_argument("--seed", default="pygame-jomon"); parser.add_argument("--save", type=Path, default=Path("jomon-pygame-save.json"))
    args=parser.parse_args(argv)
    try: pygame=_pygame()
    except RuntimeError as exc: parser.error(str(exc))
    session=GameSession.load(args.load) if args.load else GameSession.create(args.seed)
    pygame.init(); PygameFrontend(session,pygame=pygame).run(args.save)

if __name__ == '__main__': main()
