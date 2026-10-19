"""A keyboard-first curses control room, with mouse designation support."""

from __future__ import annotations

import curses
import textwrap
import time
from dataclasses import dataclass
from pathlib import Path

from .simulation import BLUEPRINTS, DEFAULT_SAVE, TICKS_PER_CYCLE, Simulation, Survivor
from .tutorial import LESSONS
from .world import NAMES, Terrain

# All glyphs use one terminal column; a space restores the map's aspect ratio.
TILES = {
    Terrain.VACUUM: ("·", 2), Terrain.ROCK: ("▓", 3),
    Terrain.ICE: ("≈", 5), Terrain.ORE: ("%", 6), Terrain.CRYSTAL: ("*", 7),
    Terrain.FLOOR: ("·", 4), Terrain.WALL: ("█", 4), Terrain.AIRLOCK: ("+", 6),
}
ASCII_TILES = {**TILES, Terrain.VACUUM: (".", 2), Terrain.ROCK: ("#", 3),
               Terrain.ICE: ("~", 5), Terrain.FLOOR: (".", 4), Terrain.WALL: ("#", 4)}
SPEEDS = (4, 12, 28)


@dataclass(frozen=True)
class Zoom:
    percent: int
    width: int
    height: int
    span: int = 1


ZOOMS = (Zoom(25, 2, 1, 4), Zoom(50, 2, 1, 2), Zoom(100, 2, 1),
         Zoom(200, 4, 2), Zoom(300, 6, 3))
DEFAULT_ZOOM = 2
WHEEL_UP = curses.BUTTON4_PRESSED
# Older macOS ncurses reports button 5 using the position-event bit. In that
# case setup explicitly disables motion reports so only wheel-down uses it.
LEGACY_WHEEL = not hasattr(curses, "BUTTON5_PRESSED")
WHEEL_DOWN = getattr(curses, "BUTTON5_PRESSED", curses.REPORT_MOUSE_POSITION)


class Terminal:
    def __init__(self, screen, sim: Simulation, save_path: Path = DEFAULT_SAVE,
                 ascii_only: bool = False, welcome: bool = True):
        self.screen, self.sim, self.save_path = screen, sim, save_path
        self.ascii = ascii_only
        self.paused, self.speed = True, 0
        self.cursor = list(sim.world.center)
        self.origin = [max(0, self.cursor[0] - 18), max(0, self.cursor[1] - 9)]
        self.zoom_index = DEFAULT_ZOOM
        self._camera_follow_cursor = True
        self._terminal_size = None
        self._map_targets: dict[tuple[int, int], tuple[int, int]] = {}
        self.tool, self.blueprint = "inspect", "wall"
        self.anchor: tuple[int, int] | None = None
        self.overlay = 0
        self.modal = "welcome" if welcome else ""
        self.tutorial_page = 0
        self.crew_index = 0
        self.build_index = 0
        self.scroll = 0
        self.message = "Click a stranded survivor's red ? to rescue. Press t for the tutorial."
        self.message_until = time.monotonic() + 15
        self.running = True
        self.follow: int | None = None
        self.map_x, self.map_y, self.map_cols, self.map_rows = 2, 6, 1, 1
        self.color = False
        self._setup()

    def _setup(self):
        try:
            curses.curs_set(0)
        except curses.error:
            pass
        self.screen.keypad(True)
        self.screen.timeout(40)
        try:
            curses.set_escdelay(25)
        except AttributeError:
            pass
        if curses.has_colors():
            curses.start_color()
            try:
                curses.use_default_colors()
                bg = -1
            except curses.error:
                bg = curses.COLOR_BLACK
            palette = [252, 239, 244, 108, 117, 215, 183, 203, 231, 114, 81, 220]
            fallback = [7, 0, 7, 2, 6, 3, 5, 1, 7, 2, 6, 3]
            for i, color in enumerate(palette, 1):
                curses.init_pair(i, color if curses.COLORS >= 256 else fallback[i - 1], bg)
            self.color = True
            self.screen.bkgd(" ", self.attr(1))
        try:
            curses.mousemask(curses.ALL_MOUSE_EVENTS | (curses.REPORT_MOUSE_POSITION if LEGACY_WHEEL else 0))
            curses.mouseinterval(0)
            if LEGACY_WHEEL:
                curses.putp(b"\x1b[?1003l\x1b[?1002l")
        except curses.error:
            pass

    def attr(self, color: int = 1, bold: bool = False, reverse: bool = False) -> int:
        return (curses.color_pair(color) if self.color else 0) | (curses.A_BOLD if bold else 0) | (curses.A_REVERSE if reverse else 0)

    def put(self, y: int, x: int, value: str, color: int = 1, bold: bool = False, reverse: bool = False, width: int | None = None):
        height, columns = self.screen.getmaxyx()
        if y < 0 or y >= height or x >= columns:
            return
        if x < 0:
            value, x = value[-x:], 0
        limit = max(0, columns - x - 1)
        if width is not None:
            limit = min(limit, width)
        if limit <= 0:
            return
        try:
            self.screen.addnstr(y, x, value, limit, self.attr(color, bold, reverse))
        except (curses.error, UnicodeError):
            pass

    def rule(self, y: int, x: int, width: int, color: int = 2):
        self.put(y, x, ("-" if self.ascii else "─") * max(0, width), color)

    def box(self, y: int, x: int, height: int, width: int, label: str = ""):
        if width < 3 or height < 3:
            return
        tl, tr, bl, br, hz, vt = ("+", "+", "+", "+", "-", "|") if self.ascii else ("┌", "┐", "└", "┘", "─", "│")
        self.put(y, x, tl + hz * (width - 2) + tr, 2)
        self.put(y + height - 1, x, bl + hz * (width - 2) + br, 2)
        for row in range(y + 1, y + height - 1):
            self.put(row, x, vt, 2)
            self.put(row, x + width - 1, vt, 2)
        if label:
            self.put(y, x + 2, " " + label + " ", 4, True, width=width - 5)

    def notice(self, message: str):
        self.message, self.message_until = message, time.monotonic() + 6

    def bar(self, y: int, x: int, name: str, value: float, maximum: float, width: int = 12, color: int = 4):
        percent = max(0.0, min(1.0, value / max(1, maximum)))
        self.put(y, x, name, 3)
        start = x + 10
        fill = int(percent * width)
        self.put(y, start, ("#" if self.ascii else "━") * width, 2)
        self.put(y, start, ("#" if self.ascii else "━") * fill, color)
        self.put(y, start + width + 1, f"{percent * 100:3.0f}%", color)

    def _layout(self):
        height, width = self.screen.getmaxyx()
        if self._terminal_size != (height, width):
            self._camera_follow_cursor = True
            self._terminal_size = (height, width)
        self.side_width = 33 if width >= 106 else 0
        self.frame_width = width - self.side_width - 2
        self.map_cols = max(1, (self.frame_width - 4) // self.zoom.width)
        self.map_rows = max(1, height - 17)
        self.view_rows = max(1, self.map_rows // self.zoom.height)
        self.world_cols = self.map_cols * self.zoom.span
        self.world_rows = self.view_rows * self.zoom.span
        self.map_x, self.map_y = 2, 6
        if self._camera_follow_cursor:
            for axis, extent, padding in ((0, self.world_cols, 3), (1, self.world_rows, 2)):
                margin = min(padding * self.zoom.span, max(0, (extent - 1) // 2))
                position = self.cursor[axis]
                if position < self.origin[axis] + margin:
                    self.origin[axis] = position - margin
                if position >= self.origin[axis] + extent - margin:
                    self.origin[axis] = position - extent + margin + 1
        self.origin[0] = max(0, min(max(0, self.sim.world.width - self.world_cols), self.origin[0]))
        self.origin[1] = max(0, min(max(0, self.sim.world.height - self.world_rows), self.origin[1]))
        self._camera_follow_cursor = False

    @property
    def zoom(self) -> Zoom:
        return ZOOMS[self.zoom_index]

    def _zoom_to(self, index: int, pointer: tuple[int, int] | None = None):
        index = max(0, min(len(ZOOMS) - 1, index))
        if index == self.zoom_index:
            return
        self._layout()
        old = self.zoom
        if pointer is not None:
            px, py = pointer[0] - self.map_x + 0.5, pointer[1] - self.map_y + 0.5
            wx = self.origin[0] + px * old.span / old.width
            wy = self.origin[1] + py * old.span / old.height
            self.follow = None
        else:
            wx, wy = self.cursor[0] + 0.5, self.cursor[1] + 0.5
            px = (wx - self.origin[0]) * old.width / old.span
            py = (wy - self.origin[1]) * old.height / old.span
            if not (0 <= px < self.map_cols * old.width and 0 <= py < self.view_rows * old.height):
                px, py = self.map_cols * old.width / 2, self.view_rows * old.height / 2
        self.zoom_index = index
        self.origin = [round(wx - px * self.zoom.span / self.zoom.width),
                       round(wy - py * self.zoom.span / self.zoom.height)]
        self._camera_follow_cursor = False
        self._layout()
        self._map_targets.clear()
        self.notice(f"View {self.zoom.percent}% / wheel at pointer, +/- at cursor, 0 resets.")

    def _map_bin(self, mx: int, my: int) -> tuple[int, int] | None:
        col = (mx - self.map_x) // self.zoom.width
        row = (my - self.map_y) // self.zoom.height
        if mx >= self.map_x and my >= self.map_y and 0 <= col < self.map_cols and 0 <= row < self.view_rows:
            return col, row
        return None

    def draw(self):
        self.screen.erase()
        height, width = self.screen.getmaxyx()
        if height < 27 or width < 76:
            self.put(2, 3, "REGOLITH", 4, True)
            self.put(4, 3, "Resize to at least 76 columns x 27 rows.", 6)
            self.put(6, 3, f"Current terminal: {width} x {height}. Simulation held.", 3)
            self.put(8, 3, "q saves and exits.", 1)
            self.screen.refresh()
            return
        self._layout()
        sim = self.sim
        state = "OUTPOST LOST" if sim.game_over else "PAUSED" if self.paused else f"LIVE {SPEEDS[self.speed] // SPEEDS[0]}x"
        self.put(0, 2, "R E G O L I T H", 4, True)
        self.put(0, 21, "/  A SMALL LIFE ON THE MOON", 3)
        self.put(0, width - 22, f"[ {state:^15} ]", 6 if self.paused else 10, True)
        phase = "PARTICLE STORM" if sim.storm else "SUNLIT" if sim.daylight > 0.25 else "LUNAR NIGHT"
        hours = int((sim.tick % TICKS_PER_CYCLE) / TICKS_PER_CYCLE * 24 * 60)
        self.put(1, 2, f"TRANQUILITY OUTPOST   /   CYCLE {sim.cycle:02d}  {hours // 60:02d}:{hours % 60:02d}", 3)
        self.put(1, max(44, width - 28), f"{phase:>18}  {sim.daylight * 100:3.0f}%", 6 if sim.storm else 3)
        self.rule(2, 2, width - 5)
        resource_x = 2
        for label, key, color in (("METAL", "metal", 6), ("ICE / H2O", "water", 5), ("FOOD", "food", 4), ("CRYSTAL", "crystal", 7)):
            self.put(3, resource_x, f"{label}  {sim.resources[key]:5.0f}", color, True)
            resource_x += 18
        if width > 108:
            self.put(3, width - 28, f"CREW {len(sim.living):02d}   RESCUED {sim.rescued:02d}", 3)
        label = ["SECTOR MAP", "OXYGEN / 0..9", "TEMPERATURE / C", "POWER NETWORK"][self.overlay]
        self.box(5, 0, self.map_rows + 2, self.frame_width, label)
        self.zoom_ui_x = self.frame_width - 17
        self.put(5, self.zoom_ui_x, f"[-] {self.zoom.percent:3}% [+]", 6, True)
        self._draw_map()
        if self.side_width:
            self._draw_side(self.frame_width + 1, 5, self.side_width, self.map_rows + 2)
        self._draw_bottom(height, width)
        if self.modal:
            self._draw_modal(height, width)
        self.screen.noutrefresh()
        curses.doupdate()

    def _draw_map(self):
        sim, world = self.sim, self.sim.world
        machines = {(m.x, m.y): m for m in sim.machines}
        crew = {(c.x, c.y): c for c in sim.colonists if c.alive}
        survivors = {(s.x, s.y): s for s in sim.distress}
        jobs = {(j.x, j.y): j for j in sim.jobs}
        self._map_targets.clear()
        span, width, height = self.zoom.span, self.zoom.width, self.zoom.height
        for row in range(self.view_rows):
            for col in range(self.map_cols):
                start_x, start_y = self.origin[0] + col * span, self.origin[1] + row * span
                chosen = None
                for y in range(start_y, min(world.height, start_y + span)):
                    for x in range(start_x, min(world.width, start_x + span)):
                        style = self._tile_visual(x, y, machines, crew, survivors, jobs)
                        if chosen is None or style[3] > chosen[2][3]:
                            chosen = (x, y, style)
                if chosen is None:
                    continue
                x, y, (glyph, color, bold, priority) = chosen
                self._map_targets[col, row] = (x, y)
                selected = start_x <= self.cursor[0] < start_x + span and start_y <= self.cursor[1] < start_y + span
                in_selection = False
                if self.anchor:
                    ax, ay = self.anchor
                    in_selection = (start_x <= max(ax, self.cursor[0]) and start_x + span > min(ax, self.cursor[0])
                                    and start_y <= max(ay, self.cursor[1]) and start_y + span > min(ay, self.cursor[1]))
                screen_x, screen_y = self.map_x + col * width, self.map_y + row * height
                if height == 1:
                    self.put(screen_y, screen_x, glyph + " " * (width - 1), color, bold, selected or in_selection)
                else:
                    solid = priority <= 4 and world.at(x, y).terrain in (Terrain.ROCK, Terrain.WALL)
                    for line in range(height):
                        self.put(screen_y + line, screen_x, (glyph if solid else " ") * width,
                                 color, bold, selected or in_selection)
                    if not solid:
                        self.put(screen_y + (height - 1) // 2, screen_x + (width - 1) // 2,
                                 glyph, color, bold, selected or in_selection)
        label = f" {self.cursor[0]:02d},{self.cursor[1]:02d} / {self.tool.upper()} "
        if span > 1:
            label += f"/ {span}x{span} OVERVIEW "
        self.put(self.map_y + self.map_rows, self.map_x + 2, label, 6 if self.tool != "inspect" else 3)

    def _tile_visual(self, x, y, machines, crew, survivors, jobs):
        """Priority preserves people, hazards, and work when summarizing terrain."""
        sim, world = self.sim, self.sim.world
        tiles = ASCII_TILES if self.ascii else TILES
        cell = world.at(x, y)
        survivor = survivors.get((x, y))
        if not cell.discovered and not survivor:
            return ("?" if [x, y] == self.cursor else " "), 2, False, 0
        glyph, color = tiles[cell.terrain]
        bold = False
        priority = {Terrain.VACUUM: 1, Terrain.FLOOR: 2, Terrain.ROCK: 3, Terrain.WALL: 4}.get(cell.terrain, 5)
        if cell.terrain == Terrain.VACUUM and (x * 73 + y * 37 + sim.seed) % 19:
            glyph = " "
        if self.overlay == 1 and cell.terrain in (Terrain.FLOOR, Terrain.VACUUM):
            glyph = str(min(9, int(cell.oxygen * 10))) if cell.oxygen > 0.01 else "." if self.ascii else "·"
            color = 8 if cell.oxygen < 0.30 else 6 if cell.oxygen < 0.65 else 5
        elif self.overlay == 2 and cell.terrain not in (Terrain.ROCK, Terrain.WALL):
            glyph = "+" if cell.heat >= 0 else "-"
            color = 8 if cell.heat > 45 else 4 if cell.heat > 5 else 5
        elif self.overlay == 3 and cell.terrain == Terrain.FLOOR:
            network = sim._network.get((x, y))
            connected = any(m.kind in ("rtg", "solar") and m.powered and sim._network.get((m.x, m.y)) == network
                            for m in sim.machines)
            glyph, color = ("+", 10) if connected else ("-", 8)
        machine = machines.get((x, y))
        if machine:
            glyph = "R" if machine.kind == "rtg" else BLUEPRINTS[machine.kind].glyph
            color = 3 if machine.kind == "bunk" else 10 if machine.powered else 8
            bold, priority = True, 6
        if (x, y) in jobs:
            job = jobs[(x, y)]
            glyph = "x" if job.kind in ("dig", "salvage") else "?" if job.kind == "survey" else BLUEPRINTS[job.blueprint].glyph
            color, bold, priority = 6, True, 7
        if cell.fire > 0.05:
            glyph, color, bold, priority = "!", 8, True, 8
        if (x, y) in crew:
            colonist = crew[(x, y)]
            glyph = colonist.name[0]
            color, bold, priority = (8 if colonist.health < 35 else 9 if colonist.rest else 11), True, 9
        if survivor:
            glyph, color, bold, priority = ("&", 6, True, 10) if survivor.status == "escorted" else ("?", 8, True, 11)
        return glyph, color, bold, priority

    def _draw_side(self, x: int, y: int, width: int, height: int):
        sim = self.sim
        self.box(y, x, height, width, "OUTPOST TELEMETRY")
        x += 2
        y += 2
        self.bar(y, x, "PRESSURE", sim.pressure, 1, 11, 8 if sim.pressure < 0.4 else 5)
        self.bar(y + 1, x, "BATTERY", sim.charge, sim.capacity, 11, 6)
        self.bar(y + 2, x, "MORALE", sim.morale, 100, 11, 4)
        self.put(y + 4, x, f"POWER   {sim.generated:4.1f} / {sim.demand:4.1f} kW", 10 if sim.generated >= sim.demand else 6)
        self.put(y + 5, x, f"CREW    {len(sim.living):02d} alive   {len(sim.jobs):02d} orders", 3)
        if height < 18:
            return
        self.rule(y + 7, x, width - 4)
        self.put(y + 8, x, "UNDER THE CURSOR", 4, True)
        cell = sim.world.at(*self.cursor)
        name = NAMES[cell.terrain] if cell.discovered else "Unexplored terrain"
        machine = sim.machine_at(*self.cursor) if cell.discovered else None
        if machine:
            name = sim.machine_name(machine)
        self.put(y + 10, x, name, 1, True, width=width - 4)
        if cell.discovered:
            self.put(y + 11, x, f"O2 {cell.oxygen * 100:3.0f}%   TEMP {cell.heat:5.1f} C", 3)
        crew = next((c for c in sim.living if [c.x, c.y] == self.cursor), None)
        survivor = sim.survivor_at(*self.cursor)
        detail = f"{crew.name} / {crew.role}" if crew else f"DISTRESS / {survivor.name}" if survivor else ("ONLINE" if machine and machine.powered else "OFFLINE" if machine else "")
        self.put(y + 12, x, detail, 11 if crew else 10)
        if crew:
            self.put(y + 13, x, crew.activity, 3, width=width - 4)
        elif survivor:
            self.put(y + 13, x, f"HP {survivor.health:.0f}  SUIT {survivor.suit:.0f}%", 8, width=width - 4)
            mission = sim.expedition_for(survivor.id)
            action = "Rescue crew on the way" if mission else "Click / Enter: rescue (2F/2W)"
            self.put(y + 14, x, action, 6, width=width - 4)
        else:
            job = sim.job_at(*self.cursor)
            if job:
                self.put(y + 13, x, f"WORK {job.progress / job.required * 100:3.0f}% / " + ("assigned" if job.assigned is not None else "queued"), 6)
        if height >= 23:
            self.rule(y + 15, x, width - 4)
            self.put(y + 16, x, "FOUNDING CHARTER", 4, True)
            goals = [(sim.rescued >= 3, f"Rescue 3 survivors ({sim.rescued}/3)"),
                     (sim.cycle >= 3 and len(sim.living) >= 4, "Cycle 3 with 4 living crew"),
                     (sim.resources["food"] >= 20, "Stock 20 food"),
                     (any(m.kind == "beacon" and m.powered and sim.world.at(m.x, m.y).oxygen >= 0.4 for m in sim.machines), "Power Earth uplink")]
            for i, (done, text) in enumerate(goals):
                self.put(y + 17 + i, x, ("[x] " if done else "[ ] ") + text, 4 if done else 3)

    def _draw_bottom(self, height: int, width: int):
        top = self.map_y + self.map_rows + 2
        self.put(top, 2, "MISSION LOG", 4, True)
        self.put(top, 16, "[e] expand", 3)
        entries = self.sim.log[-3:]
        for i, entry in enumerate(entries):
            tick = entry["tick"]
            stamp = f"{tick // TICKS_PER_CYCLE + 1:02d}.{tick % TICKS_PER_CYCLE:03d}"
            color = {"good": 4, "danger": 8, "warn": 6}.get(entry["level"], 3)
            self.put(top + 1 + i, 2, stamp, 2)
            self.put(top + 1 + i, 10, entry["text"], color, width=width - 13)
        self.rule(height - 5, 2, width - 5)
        survivor = self.sim.survivor_at(*self.cursor)
        if time.monotonic() < self.message_until:
            text = self.message
        elif survivor:
            text = self._rescue_hint(survivor)
        elif self.sim.distress and self.tool == "inspect":
            urgent = min(self.sim.distress, key=lambda s: s.time_left)
            text = f"DISTRESS / {urgent.name} / SUIT {urgent.suit:.0f}% / AT {urgent.x},{urgent.y} / r locate"
        elif self.anchor:
            text = "REGION SELECT / move to opposite corner, Enter confirms, Esc cancels."
        elif self.tool == "build":
            text = f"BUILD / {BLUEPRINTS[self.blueprint].name} / Enter or click places / v selects a region."
        elif self.tool == "dig":
            text = "MINE / Enter or click marks a cell / v marks a rectangle / x cancels orders."
        else:
            cell = self.sim.world.at(*self.cursor)
            text = f"{NAMES[cell.terrain] if cell.discovered else 'Unexplored'}  /  O2 {cell.oxygen * 100:.0f}%  /  {cell.heat:.0f} C  /  {len(self.sim.living)} crew"
        self.put(height - 4, 2, text, 6 if self.tool != "inspect" else 3, width=width - 5)
        if width >= 96:
            self.put(height - 2, 2, "SPACE pause  WHEEL / +/- zoom  0 reset  CLICK ? rescue  d mine  b build  o overlay", 1)
            self.put(height - 1, 2, "ARROWS move   ENTER order   ESC inspect   c crew   t tutorial   ? help   q save/quit", 3)
        else:
            self.put(height - 2, 2, "WHEEL / +/- zoom  0 reset  SPACE pause  CLICK ? rescue", 1)
            self.put(height - 1, 2, "ARROWS move  ENTER order  d mine  b build  ? help  q save/quit", 3)

    def _modal_base(self, height: int, width: int, title: str, lines: int = 24, columns: int = 72):
        box_h, box_w = min(height - 2, lines), min(width - 4, columns)
        y, x = (height - box_h) // 2, (width - box_w) // 2
        for yy in range(y, y + box_h):
            self.put(yy, x, " " * box_w)
        self.box(y, x, box_h, box_w, title)
        return y + 2, x + 3, box_h - 4, box_w - 6

    def _draw_modal(self, height: int, width: int):
        if self.modal == "welcome":
            self._draw_tutorial(height, width)
        elif self.modal == "help":
            y, x, available, cols = self._modal_base(height, width, "FIELD NOTES", 28, 78)
            lines = [
                ("THE CONTROLS", 4),
                ("Arrows / hjkl  Move cursor; camera follows at the edges", 1),
                ("Wheel / +/-    Zoom at pointer / cursor; 0 resets view", 1),
                ("Shift + arrows Move five cells     g  Return to habitat", 1),
                ("Space          Run / pause        .  Advance one tick", 1),
                ("1 / 2 / 3      Time speed         o  Cycle map overlays", 1),
                ("d              Mine / salvage     b  Construction menu", 1),
                ("Enter / click  Order work or rescue a stranded survivor", 1),
                ("x  Cancel orders    p  Survey terrain    Esc  Inspect", 1),
                ("r  Next distress signal   v  Region   t  Tutorial", 1),
                ("c              Crew manifest      f  Follow crew at cursor", 1),
                ("e              Expanded log       s  Save   L  Load save", 1),
                ("q              Save and exit      ?  Field notes", 1),
                ("", 1), ("READING THE MOON", 4),
                ("# hull / basalt   ~ ice   % ore   * crystal   + airlock", 3),
                ("R generator   O life support   H crops   S solar   B battery", 3),
                ("? distress   & escorted   ! fire   initials = your crew", 3),
                ("Amber tiles are orders. Crew wear EVA suits outside; they", 3),
                ("return to pressure when low. Unreachable orders wait.", 3),
                ("", 1), ("BUILDING A ROOM", 4),
                ("Surround deck with hull; use an airlock for the entrance.", 3),
                ("Deck and airlocks conduct power by edge adjacency. Oxygen", 3),
                ("diffuses through open cells, and airlocks block gas flow.", 3),
            ]
            for i, (line, color) in enumerate(lines[:available]):
                self.put(y + i, x, line, color, color == 4, width=cols)
            self.put(y + available, x, "[ Esc / Enter ] return", 4)
        elif self.modal == "build":
            y, x, available, cols = self._modal_base(height, width, "CONSTRUCTION / MATERIALS RESERVED ON ORDER", 25, 78)
            keys = list(BLUEPRINTS)
            for i, key in enumerate(keys):
                bp = BLUEPRINTS[key]
                affordable = self.sim.can_afford(bp.cost)
                cost = " + ".join(f"{v:g} {k}" for k, v in bp.cost.items())
                line = f" {i + 1:>2}  {bp.glyph}  {bp.name:<17} {cost}"
                self.put(y + i, x, line.ljust(cols), 1 if affordable else 2,
                         reverse=i == self.build_index, width=cols)
            bp = BLUEPRINTS[keys[self.build_index]]
            self.rule(y + 11, x, cols)
            for i, line in enumerate(textwrap.wrap(bp.description, cols)):
                self.put(y + 13 + i, x, line, 3)
            self.put(y + 16, x, "Deck and hull: place on surface. Machines: place on deck.", 6)
            self.put(y + available, x, "[ arrows / 1-9,0 ] choose    [ Enter ] equip    [ Esc ] return", 4)
        elif self.modal == "crew":
            y, x, available, cols = self._modal_base(height, width, "CREW MANIFEST / BUILT FROM SECOND CHANCES", 26, 84)
            slots = max(1, (available - 1) // 3)
            start = (self.crew_index // slots) * slots
            for i, c in enumerate(self.sim.colonists[start:start + slots]):
                row = y + i * 3
                self.put(row, x, f"{start + i + 1}  {c.name:<6} {c.role:<10} / {c.trait}", 11 if c.alive else 8,
                         True, reverse=start + i == self.crew_index, width=cols)
                self.put(row + 1, x + 3, f"HP {c.health:3.0f}   SUIT {c.suit:3.0f}   ENERGY {c.energy:3.0f}   HUNGER {c.hunger:3.0f}", 3, width=cols - 3)
                self.put(row + 2, x + 3, c.activity, 4 if c.alive else 8, width=cols - 3)
            self.put(y + available, x, "[ arrows ] choose  [ Enter / 1-9 ] locate  [ Esc ] return", 4)
        elif self.modal == "log":
            y, x, available, cols = self._modal_base(height, width, "MISSION LOG / arrows scroll", height - 4, 96)
            wrapped = []
            for entry in self.sim.log:
                stamp = f"C{entry['tick'] // TICKS_PER_CYCLE + 1:02d}.{entry['tick'] % TICKS_PER_CYCLE:03d} "
                color = {"danger": 8, "warn": 6, "good": 4}.get(entry["level"], 3)
                for i, line in enumerate(textwrap.wrap(entry["text"], max(10, cols - 9))):
                    wrapped.append(((stamp if i == 0 else " " * 8) + line, color))
            self.scroll = min(self.scroll, max(0, len(wrapped) - available))
            end = len(wrapped) - self.scroll
            for i, (line, color) in enumerate(wrapped[max(0, end - available):end]):
                self.put(y + i, x, line, color, width=cols)
            self.put(y + available, x, "[ up/down ] scroll    [ Esc / Enter ] return", 4)
        elif self.modal == "lost":
            y, x, available, cols = self._modal_base(height, width, "FINAL TRANSMISSION", 14, 68)
            self.put(y, x, "The lights are still on. Nobody is coming to work.", 8, True)
            self.put(y + 2, x, f"Your crew held Tranquility for {self.sim.tick} ticks.", 3)
            self.put(y + 4, x, "[ n ] new outpost, same moon", 4)
            self.put(y + 5, x, "[ L ] load last save", 4)
            self.put(y + 6, x, "[ q ] exit without overwriting the last save", 3)

    def _draw_tutorial(self, height: int, width: int):
        lesson = LESSONS[self.tutorial_page]
        y, x, available, cols = self._modal_base(height, width, "FIELD TUTORIAL / TIME HELD", 24, 76)
        self.put(y, x, f"SECTION {self.tutorial_page + 1} OF {len(LESSONS)}", 6, True)
        self.put(y + 2, x, lesson.title, 4, True, width=cols)
        yy = y + 4
        for paragraph in (lesson.story, lesson.instructions):
            for line in textwrap.wrap(paragraph, cols):
                self.put(yy, x, line, 1 if paragraph == lesson.story else 3, width=cols)
                yy += 1
            yy += 1
        for i, line in enumerate(textwrap.wrap(lesson.objective, cols)):
            self.put(y + available - 3 + i, x, line, 6, width=cols)
        action = "start" if self.tutorial_page == len(LESSONS) - 1 else "next"
        self.put(y + available, x, f"[ Enter ] {action}   [ left/right ] sections   [ Esc ] skip", 4)

    def _rescue_hint(self, survivor: Survivor) -> str:
        mission = self.sim.expedition_for(survivor.id)
        if mission:
            crew = next(c for c in self.sim.colonists if c.id == mission.crew_id)
            if mission.phase == "returning":
                return f"{crew.name} is escorting {survivor.name} home."
            return f"{crew.name} is on the way to rescue {survivor.name}."
        return f"Rescue {survivor.name}: click / Enter (2 food + 2 water)."

    def _focus_distress(self):
        contacts = sorted(self.sim.distress, key=lambda s: (self.sim.expedition_for(s.id) is not None,
                                                          s.time_left, s.id))
        if not contacts:
            self.notice("No active distress signals. Listening for survivors...")
            return
        current = next((i for i, s in enumerate(contacts) if [s.x, s.y] == self.cursor), None)
        survivor = contacts[0 if current is None else (current + 1) % len(contacts)]
        self.cursor, self.follow, self.anchor, self.tool = [survivor.x, survivor.y], None, None, "inspect"
        self._camera_follow_cursor = True
        self.notice(self._rescue_hint(survivor))

    def _place(self):
        survivor = self.sim.survivor_at(*self.cursor)
        if survivor:
            self.anchor = None
            if self.sim.expedition_for(survivor.id):
                self.notice(self._rescue_hint(survivor))
            else:
                _, message = self.sim.dispatch_expedition(survivor.id)
                self.notice(message)
            return
        if self.tool == "inspect":
            self.notice("Choose d to mine or b to build; ? opens field notes.")
            return
        if self.anchor:
            ax, ay = self.anchor
            x, y = self.cursor
            count, last = 0, "No suitable cells in this region."
            for yy in range(min(ay, y), max(ay, y) + 1):
                for xx in range(min(ax, x), max(ax, x) + 1):
                    ok, message = self.sim.designate(xx, yy, self.tool, self.blueprint)
                    if ok:
                        count += 1
                    else:
                        last = message
            self.anchor = None
            self.notice(f"{count} work orders updated." if count else last)
        else:
            _, message = self.sim.designate(*self.cursor, self.tool, self.blueprint)
            self.notice(message)

    def _save(self):
        self.sim.save(self.save_path)
        self.notice(f"Colony saved to {self.save_path}")

    def _load(self):
        try:
            loaded = Simulation.load(self.save_path)
        except (OSError, ValueError, KeyError, TypeError, OverflowError) as exc:
            self.notice(f"Cannot load colony: {exc}")
            return
        self.sim = loaded
        self.cursor = list(loaded.world.center)
        self.follow, self.anchor = None, None
        self._camera_follow_cursor = True
        self._map_targets.clear()
        self.modal, self.paused = "", True
        self.notice("Colony restored. Time paused.")

    def _modal_key(self, key: int):
        enter = key in (10, 13, curses.KEY_ENTER)
        if self.modal == "welcome":
            if key in (curses.KEY_LEFT, ord("p")):
                self.tutorial_page = max(0, self.tutorial_page - 1)
            elif key in (curses.KEY_RIGHT, ord("n")) or enter or key == ord(" "):
                if self.tutorial_page < len(LESSONS) - 1:
                    self.tutorial_page += 1
                elif enter or key in (ord(" "), ord("n")):
                    self.modal, self.paused = "", False
                    self.notice("Ada is alone. Click Mika's red ? to send a rescue. r locates distress signals.")
            elif key == ord("?"):
                self.modal = "help"
            elif key == 27:
                self.modal = ""
        elif self.modal == "build":
            if key in (curses.KEY_UP, ord("k")):
                self.build_index = (self.build_index - 1) % len(BLUEPRINTS)
            elif key in (curses.KEY_DOWN, ord("j")):
                self.build_index = (self.build_index + 1) % len(BLUEPRINTS)
            elif ord("1") <= key <= ord("9"):
                self.build_index = key - ord("1")
                enter = True
            elif key == ord("0"):
                self.build_index, enter = 9, True
            if enter:
                self.blueprint = list(BLUEPRINTS)[self.build_index]
                self.tool, self.modal, self.anchor = "build", "", None
                self.notice(f"Equipped {BLUEPRINTS[self.blueprint].name}. Enter or click to place.")
            elif key == 27:
                self.modal = ""
        elif self.modal == "crew":
            if key in (curses.KEY_UP, ord("k"), curses.KEY_DOWN, ord("j")):
                delta = -1 if key in (curses.KEY_UP, ord("k")) else 1
                self.crew_index = (self.crew_index + delta) % len(self.sim.colonists)
            elif enter or ord("1") <= key <= ord("9"):
                index = self.crew_index if enter else key - ord("1")
                if index < len(self.sim.colonists):
                    c = self.sim.colonists[index]
                    self.cursor, self.modal, self.follow = [c.x, c.y], "", None
                    self._camera_follow_cursor = True
            elif key in (27, ord("c")):
                self.modal = ""
        elif self.modal == "log" and key in (curses.KEY_UP, curses.KEY_DOWN, ord("k"), ord("j")):
            self.scroll = max(0, self.scroll + (2 if key in (curses.KEY_UP, ord("k")) else -2))
        elif self.modal == "lost":
            if key == ord("n"):
                seed = self.sim.seed
                self.sim = Simulation(seed)
                self.cursor, self.anchor, self.follow = list(self.sim.world.center), None, None
                self.modal, self.paused, self.tutorial_page = "welcome", True, 0
                self.crew_index = 0
                self.zoom_index, self._camera_follow_cursor = DEFAULT_ZOOM, True
                self._map_targets.clear()
            elif key == ord("L"):
                self._load()
        elif key == 27 or enter or key in (ord("?"), ord("c"), ord("e")):
            self.modal = ""

    def handle(self, key: int):
        if key == -1:
            return
        if key == ord("q"):
            if not self.sim.game_over:
                self._save()
            self.running = False
            return
        if self.modal:
            self._modal_key(key)
            return
        moves = {curses.KEY_LEFT: (-1, 0), ord("h"): (-1, 0), curses.KEY_RIGHT: (1, 0), ord("l"): (1, 0),
                 curses.KEY_UP: (0, -1), ord("k"): (0, -1), curses.KEY_DOWN: (0, 1), ord("j"): (0, 1),
                 curses.KEY_SLEFT: (-5, 0), curses.KEY_SRIGHT: (5, 0), ord("H"): (-5, 0),
                 curses.KEY_SR: (0, -5), curses.KEY_SF: (0, 5),
                 ord("J"): (0, 5), ord("K"): (0, -5)}
        if key in moves:
            dx, dy = moves[key]
            self.cursor[0] = min(self.sim.world.width - 1, max(0, self.cursor[0] + dx))
            self.cursor[1] = min(self.sim.world.height - 1, max(0, self.cursor[1] + dy))
            self.follow = None
            self._camera_follow_cursor = True
        elif key in (ord("+"), ord("="), curses.KEY_PPAGE):
            self._zoom_to(self.zoom_index + 1)
        elif key in (ord("-"), ord("_"), curses.KEY_NPAGE):
            self._zoom_to(self.zoom_index - 1)
        elif key == ord("0"):
            self._zoom_to(DEFAULT_ZOOM)
        elif key == ord(" "):
            self.paused = not self.paused
        elif key in (ord("1"), ord("2"), ord("3")):
            self.speed = key - ord("1")
            self.notice(f"Time {SPEEDS[self.speed] // SPEEDS[0]}x / {SPEEDS[self.speed]} ticks per second")
        elif key == ord("."):
            self.paused = True
            self.sim.step()
        elif key == ord("d"):
            self.tool, self.anchor = "dig", None
            self.notice("Mine / salvage tool. Enter places an order; v marks a region.")
        elif key == ord("p"):
            self.tool, self.anchor = "survey", None
            self.notice("Survey tool. Enter on open terrain sends a crew member to explore.")
        elif key == ord("b"):
            self.modal = "build"
        elif key == ord("x"):
            self.tool, self.anchor = "cancel", None
            self.notice("Cancel tool. Enter or click an order to refund its materials.")
        elif key == ord("v"):
            if self.tool == "inspect":
                self.notice("Equip d (mine), b (build), or x (cancel) before marking a region.")
            else:
                self.anchor = tuple(self.cursor) if self.anchor is None else None
                self.message_until = 0
        elif key in (10, 13, curses.KEY_ENTER):
            self._place()
        elif key == 27:
            self.tool, self.anchor, self.follow = "inspect", None, None
            self.notice("Inspect mode.")
        elif key == ord("o"):
            self.overlay = (self.overlay + 1) % 4
        elif key == ord("g"):
            self.cursor, self.follow = list(self.sim.world.center), None
            self._camera_follow_cursor = True
        elif key == ord("f"):
            c = next((c for c in self.sim.living if [c.x, c.y] == self.cursor), None)
            self.follow = c.id if c else None
            self._camera_follow_cursor = True
            self.notice(f"Following {c.name}. Move the cursor to stop." if c else "Place cursor on a crew member to follow.")
        elif key == ord("?"):
            self.modal = "help"
        elif key == ord("t"):
            self.modal, self.tutorial_page = "welcome", 0
        elif key == ord("r"):
            self._focus_distress()
        elif key == ord("c"):
            self.modal, self.crew_index = "crew", 0
        elif key == ord("e"):
            self.modal, self.scroll = "log", 0
        elif key == ord("s"):
            self._save()
        elif key == ord("L"):
            self._load()
        elif key == curses.KEY_MOUSE:
            self._mouse()

    def _mouse(self):
        try:
            _, mx, my, _, state = curses.getmouse()
        except curses.error:
            return
        pressed = state & (curses.BUTTON1_CLICKED | curses.BUTTON1_PRESSED)
        if pressed and my == 5 and self.zoom_ui_x <= mx < self.zoom_ui_x + 12:
            offset = mx - self.zoom_ui_x
            if offset < 3:
                self._zoom_to(self.zoom_index - 1)
            elif offset >= 9:
                self._zoom_to(self.zoom_index + 1)
            else:
                self._zoom_to(DEFAULT_ZOOM)
            return
        tile = self._map_bin(mx, my)
        if tile is None:
            return
        if state & WHEEL_UP:
            self._zoom_to(self.zoom_index + 1, (mx, my))
            return
        if state & WHEEL_DOWN:
            self._zoom_to(self.zoom_index - 1, (mx, my))
            return
        target = self._map_targets.get(tile)
        if target is None:
            return
        x, y = target
        self.cursor, self.follow = [x, y], None
        self._camera_follow_cursor = False
        if pressed and (self.tool != "inspect" or self.sim.survivor_at(x, y)):
            self._place()

    def run(self):
        last, budget = time.monotonic(), 0.0
        was_lost = self.sim.game_over
        try:
            while self.running:
                now = time.monotonic()
                elapsed, last = min(0.3, now - last), now
                h, w = self.screen.getmaxyx()
                if not self.paused and not self.modal and not self.sim.game_over and h >= 27 and w >= 76:
                    budget += elapsed * SPEEDS[self.speed]
                    count = min(8, int(budget))
                    if count:
                        self.sim.step(count)
                        budget -= count
                else:
                    budget = 0.0
                if self.follow is not None:
                    c = next((c for c in self.sim.living if c.id == self.follow), None)
                    if c:
                        if self.cursor != [c.x, c.y]:
                            self._camera_follow_cursor = True
                        self.cursor = [c.x, c.y]
                if self.sim.game_over and not was_lost:
                    self.modal, self.paused, was_lost = "lost", True, True
                if not self.sim.game_over:
                    was_lost = False
                self.draw()
                key = self.screen.getch()
                try:
                    self.handle(key)
                except OSError as exc:
                    self.notice(f"Save failed: {exc}. Colony is still running.")
        except KeyboardInterrupt:
            if not self.sim.game_over:
                self.sim.save(self.save_path)


def play(sim: Simulation, save_path: Path = DEFAULT_SAVE, ascii_only: bool = False, welcome: bool = True):
    try:
        curses.wrapper(lambda screen: Terminal(screen, sim, save_path, ascii_only, welcome).run())
    except curses.error as exc:
        raise OSError(f"Cannot initialize terminal: {exc}. Try TERM=xterm-256color or --ascii.") from exc
