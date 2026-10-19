"""Exercise the real curses program through a pseudo-terminal, without packages."""

import errno
import importlib.util
import json
import os
import select
import signal
import subprocess
import sys
import tempfile
import time
import unittest
from pathlib import Path
from unittest.mock import patch


class ScreenCanvas:
    """Record what a player can read, without starting a terminal session."""

    def __init__(self, rows, columns):
        self.rows, self.columns = rows, columns
        self.erase()

    def erase(self):
        self.lines = [[" "] * self.columns for _ in range(self.rows)]

    def getmaxyx(self):
        return self.rows, self.columns

    def addnstr(self, y, x, text, limit, attributes):
        for offset, char in enumerate(text[:limit]):
            self.lines[y][x + offset] = char

    def noutrefresh(self):
        pass

    @property
    def text(self):
        return "\n".join("".join(line) for line in self.lines)


@unittest.skipUnless(os.name == "posix" and importlib.util.find_spec("curses"), "requires POSIX curses")
class TerminalTests(unittest.TestCase):
    def test_clicking_survivor_dispatches_from_map_in_inspect_and_work_modes(self):
        import curses
        from regolith.simulation import Simulation
        from regolith.tui import Terminal

        for rows, columns, tool, half in ((27, 76, "inspect", 0), (40, 120, "inspect", 1),
                                           (40, 120, "build", 0), (27, 76, "dig", 1),
                                           (40, 120, "cancel", 0)):
            with self.subTest(size=f"{columns}x{rows}", tool=tool), patch.object(Terminal, "_setup"), patch("curses.doupdate"):
                sim = Simulation(42, 48, 32)
                app = Terminal(ScreenCanvas(rows, columns), sim, welcome=False)
                app.draw()
                survivor = sim.survivors[0]
                mx = app.map_x + (survivor.x - app.origin[0]) * 2 + half
                my = app.map_y + survivor.y - app.origin[1]
                app.tool, app.follow = tool, 0
                if tool == "build":
                    app.anchor = sim.world.center
                before = sim.resources.copy()
                event = (0, mx, my, 0, curses.BUTTON1_PRESSED)
                with patch("curses.getmouse", return_value=event):
                    app.handle(curses.KEY_MOUSE)
                    self.assertEqual(len(sim.expeditions), 1)
                    self.assertEqual(sim.resources["food"], before["food"] - 2)
                    self.assertEqual(sim.resources["water"], before["water"] - 2)
                    self.assertEqual(sim.resources["metal"], before["metal"])
                    self.assertFalse(sim.jobs)
                    self.assertFalse(app.modal)
                    self.assertIsNone(app.follow)
                    self.assertIsNone(app.anchor)
                    self.assertTrue(app.paused)
                    spent = sim.resources.copy()
                    app.handle(curses.KEY_MOUSE)
                    self.assertEqual(len(sim.expeditions), 1)
                    self.assertEqual(sim.resources, spent)
                    self.assertIn("on the way", app.message)

    def test_unsuccessful_map_rescue_shows_reason_without_spending(self):
        import curses
        from regolith.simulation import Simulation
        from regolith.tui import Terminal

        with patch.object(Terminal, "_setup"), patch("curses.doupdate"):
            sim = Simulation(42, 48, 32)
            sim.resources["food"] = 1
            app = Terminal(ScreenCanvas(40, 120), sim, welcome=False)
            app.draw()
            survivor = sim.survivors[0]
            mx = app.map_x + (survivor.x - app.origin[0]) * 2
            my = app.map_y + survivor.y - app.origin[1]
            before = sim.resources.copy()
            with patch("curses.getmouse", return_value=(0, mx, my, 0, curses.BUTTON1_CLICKED)):
                app.handle(curses.KEY_MOUSE)
            self.assertIn("2 food and 2 water", app.message)
            self.assertEqual(sim.resources, before)
            self.assertFalse(sim.expeditions)
            self.assertFalse(app.modal)

    def test_mouse_release_motion_and_empty_tiles_do_not_dispatch_rescues(self):
        import curses
        from regolith.simulation import Simulation
        from regolith.tui import Terminal

        with patch.object(Terminal, "_setup"), patch("curses.doupdate"):
            sim = Simulation(42, 48, 32)
            app = Terminal(ScreenCanvas(40, 120), sim, welcome=False)
            app.draw()
            survivor = sim.survivors[0]
            mx = app.map_x + (survivor.x - app.origin[0]) * 2
            my = app.map_y + survivor.y - app.origin[1]
            for state in (curses.BUTTON1_RELEASED, curses.REPORT_MOUSE_POSITION, curses.BUTTON3_PRESSED):
                with patch("curses.getmouse", return_value=(0, mx, my, 0, state)):
                    app.handle(curses.KEY_MOUSE)
            cx, cy = sim.world.center
            event = (0, app.map_x + (cx - app.origin[0]) * 2,
                     app.map_y + cy - app.origin[1], 0, curses.BUTTON1_PRESSED)
            with patch("curses.getmouse", return_value=event):
                app.handle(curses.KEY_MOUSE)
            self.assertFalse(sim.expeditions)
            self.assertFalse(sim.jobs)

    def test_r_cycles_distress_on_map_and_enter_sends_rescue(self):
        from regolith.simulation import Simulation
        from regolith.tui import Terminal

        with patch.object(Terminal, "_setup"), patch("curses.doupdate"):
            sim = Simulation(42, 48, 32)
            sim._spawn_survivor()
            app = Terminal(ScreenCanvas(40, 120), sim, welcome=False)
            before = sim.resources.copy()
            positions = set()
            for _ in range(2):
                app.handle(ord("r"))
                positions.add(tuple(app.cursor))
                self.assertFalse(app.modal)
            self.assertEqual(len(positions), 2)
            self.assertEqual(sim.resources, before)
            self.assertFalse(sim.expeditions)
            app.handle(10)
            self.assertEqual(len(sim.expeditions), 1)
            self.assertFalse(app.modal)
            app.handle(ord("r"))
            self.assertEqual(app.cursor, [sim.survivors[0].x, sim.survivors[0].y])

    def test_tutorial_sections_navigation_and_skip_hold_time(self):
        import curses
        from regolith.simulation import Simulation
        from regolith.tui import Terminal
        from regolith.tutorial import LESSONS

        for rows, columns in ((27, 76), (40, 120)):
            with self.subTest(size=f"{columns}x{rows}"), patch.object(Terminal, "_setup"), patch("curses.doupdate"):
                canvas = ScreenCanvas(rows, columns)
                app = Terminal(canvas, Simulation(42, 48, 32))
                for index, lesson in enumerate(LESSONS):
                    app.draw()
                    self.assertIn(f"SECTION {index + 1} OF {len(LESSONS)}", canvas.text)
                    self.assertIn(lesson.title, canvas.text)
                    self.assertIn("TIME HELD", canvas.text)
                    self.assertEqual(app.sim.tick, 0)
                    app.handle(10)
                self.assertFalse(app.modal)
                self.assertFalse(app.paused)
                app.handle(ord("t"))
                app.handle(curses.KEY_RIGHT)
                self.assertEqual(app.tutorial_page, 1)
                app.handle(curses.KEY_LEFT)
                self.assertEqual(app.tutorial_page, 0)
                app.handle(27)
                self.assertFalse(app.modal)
                self.assertEqual(app.sim.tick, 0)
                self.assertEqual(app.sim.survivors[0].suit, 68)

    def test_manifest_handles_more_than_six_crew_and_unknown_shortcuts(self):
        import curses
        from regolith.simulation import Colonist, Simulation
        from regolith.tui import Terminal

        with patch.object(Terminal, "_setup"), patch("curses.doupdate"):
            canvas = ScreenCanvas(27, 76)
            sim = Simulation(42, 48, 32)
            app = Terminal(canvas, sim, welcome=False)
            app.handle(ord("c"))
            app.handle(ord("9"))
            self.assertEqual(app.modal, "crew")
            cx, cy = sim.world.center
            for index in range(1, 10):
                sim.colonists.append(Colonist(index, f"Crew {index}", "Miner", "Survivor", cx + index % 4, cy))
            for _ in range(9):
                app.handle(curses.KEY_DOWN)
            app.draw()
            self.assertIn("Crew 9", canvas.text)
            app.handle(10)
            self.assertFalse(app.modal)
            self.assertEqual(app.cursor, [sim.colonists[9].x, sim.colonists[9].y])

    def test_controls_save_load_and_resize_at_both_supported_sizes(self):
        import curses
        import fcntl
        import pty
        import struct
        import termios

        curses.setupterm("xterm-256color")

        def drain(fd, seconds=0.08):
            output = bytearray()
            end = time.monotonic() + seconds
            while time.monotonic() < end:
                if select.select([fd], [], [], 0.01)[0]:
                    try:
                        output.extend(os.read(fd, 65536))
                    except OSError as exc:
                        if exc.errno == errno.EIO:
                            break
                        raise
            return bytes(output)

        for rows, cols in ((40, 120), (27, 76)):
            with self.subTest(size=f"{cols}x{rows}"), tempfile.TemporaryDirectory() as directory:
                master, slave = pty.openpty()
                fcntl.ioctl(slave, termios.TIOCSWINSZ, struct.pack("HHHH", rows, cols, 0, 0))
                slot = Path(directory) / "colony.json"
                process = None
                try:
                    process = subprocess.Popen(
                        [sys.executable, "-m", "regolith", "--save", str(slot)],
                        stdin=slave, stdout=slave, stderr=slave,
                        env={**os.environ, "TERM": "xterm-256color"}, close_fds=True,
                        cwd=Path(__file__).resolve().parent.parent,
                    )
                    os.close(slave)
                    slave = -1
                    output = bytearray(drain(master, 0.3))
                    # Read all six tutorial sections, then start, pause, equip
                    # and order solar, inspect all overlays,
                    # inspect crew/log/help, save, reload, and exit cleanly.
                    for keys in (b"\r", b"\r", b"\r", b"\r", b"\r", b"\r",
                                 b" ", b"b", b"4", b"\r", b"\x1b", b"o", b"o", b"o",
                                 b"c", b"9", b"1", b"e", b"\r", b"?", b"\r",
                                 b"t", b"\x1b", b"r"):
                        os.write(master, keys)
                        output.extend(drain(master))
                    if cols == 120:
                        os.write(master, b"\r")
                    else:
                        # Send a real mouse click in the protocol negotiated
                        # by curses, using the live terminal's viewport layout.
                        from regolith.simulation import Simulation
                        from regolith.tui import Terminal
                        with patch.object(Terminal, "_setup"), patch("curses.doupdate"):
                            reference = Terminal(ScreenCanvas(rows, cols), Simulation(), welcome=False)
                            reference.draw()
                            reference.handle(ord("r"))
                            reference.draw()
                        sx, sy = reference.cursor
                        mx = reference.map_x + (sx - reference.origin[0]) * 2 + 1
                        my = reference.map_y + sy - reference.origin[1]
                        if curses.tigetstr("kmous") == b"\x1b[<":
                            mouse = f"\x1b[<0;{mx + 1};{my + 1}M\x1b[<0;{mx + 1};{my + 1}m".encode()
                        else:
                            mouse = b"\x1b[M" + bytes((32, mx + 33, my + 33))
                            mouse += b"\x1b[M" + bytes((35, mx + 33, my + 33))
                        os.write(master, mouse)
                    output.extend(drain(master))
                    for keys in (b"s", b"L"):
                        os.write(master, keys)
                        output.extend(drain(master))
                    # Terminal resize below the minimum must hold time and
                    # preserve a working quit/save path.
                    fcntl.ioctl(master, termios.TIOCSWINSZ, struct.pack("HHHH", 20, 60, 0, 0))
                    os.kill(process.pid, signal.SIGWINCH)
                    output.extend(drain(master, 0.15))
                    self.assertIn(b"Resize to at least", output)
                    os.write(master, b"q")
                    output.extend(drain(master, 0.15))
                    process.wait(timeout=5)
                    self.assertEqual(process.returncode, 0, output[-2000:].decode(errors="replace"))
                    self.assertNotIn(b"Traceback", output)
                    saved = json.loads(slot.read_text())
                    self.assertEqual(len(saved["colonists"]), 1)
                    self.assertEqual(saved["jobs"][0]["blueprint"], "solar")
                    self.assertEqual(saved["expeditions"][0]["phase"], "outbound")
                    self.assertIsNotNone(saved["colonists"][0]["expedition"])
                finally:
                    if process is not None and process.poll() is None:
                        process.kill()
                        process.wait()
                    os.close(master)
                    if slave >= 0:
                        os.close(slave)


if __name__ == "__main__":
    unittest.main()
