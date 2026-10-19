"""Zoom must change coverage while keeping map actions and anchors accurate."""

import importlib.util
import os
import unittest
from unittest.mock import patch

from tests.test_terminal import ScreenCanvas


@unittest.skipUnless(os.name == "posix" and importlib.util.find_spec("curses"), "requires POSIX curses")
class ZoomTests(unittest.TestCase):
    def setUp(self):
        import curses
        from regolith.simulation import Simulation
        from regolith.tui import Terminal

        self.curses = curses
        for patcher in (patch.object(Terminal, "_setup"), patch("curses.doupdate")):
            patcher.start()
            self.addCleanup(patcher.stop)
        self.canvas = ScreenCanvas(40, 120)
        self.sim = Simulation()
        self.app = Terminal(self.canvas, self.sim, welcome=False)
        self.app.draw()

    def mouse(self, mx, my, state):
        with patch("curses.getmouse", return_value=(0, mx, my, 0, state)):
            self.app.handle(self.curses.KEY_MOUSE)

    def world_under(self, mx, my):
        app = self.app
        return (app.origin[0] + (mx - app.map_x + 0.5) * app.zoom.span / app.zoom.width,
                app.origin[1] + (my - app.map_y + 0.5) * app.zoom.span / app.zoom.height)

    def test_zoom_changes_both_dimensions_and_overview_fits_the_world(self):
        initial = (self.app.world_cols, self.app.world_rows)
        before = self.sim.to_dict()
        self.app.handle(ord("+"))
        self.app.draw()
        self.assertLess(self.app.world_cols, initial[0])
        self.assertLess(self.app.world_rows, initial[1])
        self.app.handle(ord("0"))
        self.assertEqual((self.app.world_cols, self.app.world_rows), initial)
        for _ in range(10):
            self.app.handle(ord("-"))
        self.app.draw()
        self.assertEqual(self.app.zoom.percent, 25)
        self.assertGreaterEqual(self.app.world_cols, self.sim.world.width)
        self.assertGreaterEqual(self.app.world_rows, self.sim.world.height)
        for _ in range(10):
            self.app.handle(ord("+"))
        self.assertEqual(self.app.zoom.percent, 300)
        self.assertEqual(self.sim.to_dict(), before)

    def test_wheel_preserves_pointer_anchor_without_changing_selection_or_work(self):
        from regolith.tui import WHEEL_DOWN, WHEEL_UP

        mx, my = self.app.map_x + 40, self.app.map_y + 12
        before = self.world_under(mx, my)
        cursor = self.app.cursor[:]
        self.app.tool, self.app.anchor = "build", tuple(cursor)
        self.mouse(mx, my, WHEEL_UP)
        self.app.draw()
        self.assertEqual(self.app.zoom.percent, 200)
        for first, second in zip(before, self.world_under(mx, my)):
            self.assertLessEqual(abs(first - second), 0.5)
        origin = self.app.origin[:]
        self.app.draw()
        self.assertEqual(self.app.origin, origin)
        self.mouse(mx, my, WHEEL_DOWN)
        self.app.draw()
        self.assertEqual(self.app.zoom.percent, 100)
        for first, second in zip(before, self.world_under(mx, my)):
            self.assertLessEqual(abs(first - second), 0.5)
        self.assertEqual(self.app.cursor, cursor)
        self.assertEqual(self.app.anchor, tuple(cursor))
        self.assertEqual(self.app.tool, "build")
        self.assertFalse(self.sim.jobs)
        self.assertFalse(self.sim.expeditions)

    def test_keyboard_zoom_is_anchored_to_cursor(self):
        cursor = self.app.cursor[:]
        px = (cursor[0] + 0.5 - self.app.origin[0]) * self.app.zoom.width / self.app.zoom.span
        py = (cursor[1] + 0.5 - self.app.origin[1]) * self.app.zoom.height / self.app.zoom.span
        self.app.handle(ord("="))
        self.app.draw()
        self.assertEqual(self.app.cursor, cursor)
        world_x = self.app.origin[0] + px * self.app.zoom.span / self.app.zoom.width
        world_y = self.app.origin[1] + py * self.app.zoom.span / self.app.zoom.height
        self.assertLessEqual(abs(world_x - cursor[0] - 0.5), 0.5)
        self.assertLessEqual(abs(world_y - cursor[1] - 0.5), 0.5)
        self.app.handle(self.curses.KEY_NPAGE)
        self.assertEqual(self.app.zoom.percent, 100)

    def test_clickable_zoom_controls_and_wheel_outside_map(self):
        from regolith.tui import WHEEL_UP

        x = self.app.zoom_ui_x
        self.mouse(x + 1, 5, self.curses.BUTTON1_PRESSED)
        self.assertEqual(self.app.zoom.percent, 50)
        self.app.draw()
        self.mouse(x + 5, 5, self.curses.BUTTON1_PRESSED)
        self.assertEqual(self.app.zoom.percent, 100)
        self.app.draw()
        self.mouse(x + 10, 5, self.curses.BUTTON1_PRESSED)
        self.assertEqual(self.app.zoom.percent, 200)
        self.app.draw()
        self.mouse(110, 10, WHEEL_UP)
        self.assertEqual(self.app.zoom.percent, 200)
        self.app.handle(ord("b"))
        self.mouse(self.app.map_x + 10, self.app.map_y + 3, WHEEL_UP)
        self.assertEqual(self.app.zoom.percent, 200)
        self.assertEqual(self.app.modal, "build")

    def test_rescue_marker_is_visible_and_clickable_at_every_zoom(self):
        from regolith.simulation import Simulation
        from regolith.tui import Terminal, ZOOMS

        for index, zoom in enumerate(ZOOMS):
            with self.subTest(zoom=zoom.percent):
                self.sim = Simulation()
                self.app = Terminal(self.canvas, self.sim, welcome=False)
                self.app.draw()
                self.app.handle(ord("r"))
                self.app._zoom_to(index)
                self.app.draw()
                survivor = self.sim.survivors[0]
                col = (survivor.x - self.app.origin[0]) // zoom.span
                row = (survivor.y - self.app.origin[1]) // zoom.span
                self.assertEqual(self.app._map_targets[col, row], (survivor.x, survivor.y))
                gx = self.app.map_x + col * zoom.width + (zoom.width - 1) // 2 if zoom.height > 1 else self.app.map_x + col * zoom.width
                gy = self.app.map_y + row * zoom.height + (zoom.height - 1) // 2
                self.assertEqual(self.canvas.lines[gy][gx], "?")
                mx = self.app.map_x + (col + 1) * zoom.width - 1
                my = self.app.map_y + (row + 1) * zoom.height - 1
                self.mouse(mx, my, self.curses.BUTTON1_PRESSED)
                self.assertEqual(len(self.sim.expeditions), 1)
                self.assertEqual(self.sim.expeditions[0].survivor_id, survivor.id)
                self.assertEqual(self.sim.resources["food"], 98)

    def test_mining_clicks_target_the_resource_shown_at_every_zoom(self):
        from regolith.simulation import Simulation
        from regolith.tui import Terminal, ZOOMS
        from regolith.world import MINEABLE, Terrain

        for index, zoom in enumerate(ZOOMS):
            with self.subTest(zoom=zoom.percent):
                self.sim = Simulation()
                cx, cy = self.sim.world.center
                self.sim.world.at(cx, cy - 1).terrain = Terrain.ORE
                self.app = Terminal(self.canvas, self.sim, welcome=False)
                self.app.draw()
                self.app._zoom_to(index)
                self.app.draw()
                tile, target = next((tile, target) for tile, target in self.app._map_targets.items()
                                    if self.sim.world.at(*target).discovered and self.sim.world.at(*target).terrain in MINEABLE)
                self.app.handle(ord("d"))
                self.mouse(self.app.map_x + tile[0] * zoom.width,
                           self.app.map_y + tile[1] * zoom.height, self.curses.BUTTON1_PRESSED)
                self.assertEqual(len(self.sim.jobs), 1)
                self.assertEqual((self.sim.jobs[0].x, self.sim.jobs[0].y), target)

    def test_close_view_tracks_keyboard_cursor_and_resizes_safely(self):
        self.app.handle(ord("+"))
        self.app.handle(ord("+"))
        for _ in range(25):
            self.app.handle(self.curses.KEY_RIGHT)
            self.app.draw()
            self.assertTrue(self.app.origin[0] <= self.app.cursor[0] < self.app.origin[0] + self.app.world_cols)
        for rows, columns in ((27, 76), (27, 120), (50, 150), (40, 120)):
            self.canvas.rows, self.canvas.columns = rows, columns
            self.app.draw()
            self.assertGreaterEqual(min(self.app.origin), 0)
            self.assertIn("300%", self.canvas.text)
            self.assertTrue(self.app.origin[1] <= self.app.cursor[1] < self.app.origin[1] + self.app.world_rows)

    def test_real_terminal_wheel_zoom_works_in_both_directions(self):
        import errno
        import fcntl
        import json
        import pty
        import select
        import struct
        import subprocess
        import sys
        import tempfile
        import termios
        import time
        from pathlib import Path

        self.curses.setupterm("xterm-256color")

        def drain(fd, seconds=0.12):
            end = time.monotonic() + seconds
            while time.monotonic() < end:
                if select.select([fd], [], [], 0.01)[0]:
                    try:
                        os.read(fd, 65536)
                    except OSError as exc:
                        if exc.errno == errno.EIO:
                            break
                        raise

        # Observe the actual game after its input handler has processed events.
        program = '''import json, sys
from regolith.__main__ import main
from regolith.tui import Terminal
original = Terminal._zoom_to
def record(self, *args, **kwargs):
    original(self, *args, **kwargs)
    with open(sys.argv[1], "a") as output:
        output.write(json.dumps({"zoom": self.zoom.percent, "tick": self.sim.tick,
                                 "jobs": len(self.sim.jobs), "missions": len(self.sim.expeditions)}) + "\\n")
Terminal._zoom_to = record
raise SystemExit(main(["--skip-intro", "--save", sys.argv[2]]))
'''
        with tempfile.TemporaryDirectory() as directory:
            trace = Path(directory) / "zoom.jsonl"
            slot = Path(directory) / "colony.json"
            master, slave = pty.openpty()
            fcntl.ioctl(slave, termios.TIOCSWINSZ, struct.pack("HHHH", 40, 120, 0, 0))
            child = None
            try:
                child = subprocess.Popen([sys.executable, "-c", program, str(trace), str(slot)],
                                         stdin=slave, stdout=slave, stderr=slave,
                                         env={**os.environ, "TERM": "xterm-256color"},
                                         cwd=Path(__file__).resolve().parent.parent, close_fds=True)
                os.close(slave)
                slave = -1
                drain(master, 0.3)
                for button in (64, 64, 65, 65):
                    if self.curses.tigetstr("kmous") == b"\x1b[<":
                        packet = f"\x1b[<{button};43;18M".encode()
                    else:
                        packet = b"\x1b[M" + bytes((button + 32, 43 + 32, 18 + 32))
                    os.write(master, packet)
                    drain(master)
                os.write(master, b"q")
                drain(master)
                child.wait(timeout=5)
                self.assertEqual(child.returncode, 0)
                self.assertTrue(trace.exists(), "The terminal did not report wheel events")
                events = [json.loads(line) for line in trace.read_text().splitlines()]
                self.assertEqual([event["zoom"] for event in events], [200, 300, 200, 100])
                self.assertTrue(all(event["tick"] == 0 and event["jobs"] == 0 and event["missions"] == 0 for event in events))
            finally:
                if child is not None and child.poll() is None:
                    child.kill()
                    child.wait()
                os.close(master)
                if slave >= 0:
                    os.close(slave)


if __name__ == "__main__":
    unittest.main()
