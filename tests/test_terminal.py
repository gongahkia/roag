"""Real curses/PTY checks. These are constructed UI fixtures, not natural runs."""

from __future__ import annotations

import json
import os
import select
import signal
import struct
import subprocess
import sys
import tempfile
import time
import unittest
from pathlib import Path


PROBE = """
import curses, json, sys
from pathlib import Path
from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine
from dumbest_dungeon.ui import TerminalUI
from dumbest_dungeon.history import history_lines, run_report
catalog = load_catalog()
def run(screen):
    ui = TerminalUI(screen, catalog, Path(sys.argv[1]), lambda: None)
    ui.engine = GameEngine.new(catalog, 3)
    if sys.argv[2] == 'resolution':
        ui.engine.start_combat('lost_shift')
        actor, target = ui.engine.living_heroes()[0], ui.engine.living_enemies()[0]
        for i in range(40):
            ui.engine._apply_effect(actor, [target], {'op': 'block', 'amount': 1}, source_id='probe:' + str(i))
        before = ui.engine.snapshot()
        ui._resolution_view()
        picked = 1
    elif sys.argv[2] == 'history':
        for i in range(40):
            ui.engine.record('item_acquired', 'probe:' + str(i), gained=1, count=1)
        before = ui.engine.snapshot()
        ui._notice('PTY MORGUE', '\\n'.join(history_lines(run_report(ui.engine))))
        picked = 1
    elif sys.argv[2] == 'pressure':
        from dumbest_dungeon.pressure import PressureSource
        for i in range(9):
            ui.engine._advance_pressure(PressureSource.ENEMY_ROUND, 1, 'combat round ' + str(i + 1))
        before = ui.engine.snapshot()
        ui._pressure_view()
        picked = 1
    else:
        before = ui.engine.snapshot()
        picked = ui._menu('PTY LONG CONTRACT', ['Accept', 'Walk away'],
                          '\\n'.join('Consequence ' + str(i) for i in range(90)))
    rows, cols = screen.getmaxyx()
    shown = '\\n'.join(screen.instr(row, 0, cols - 1).decode(errors='replace')
                      for row in range(rows))
    return {'picked': picked, 'unchanged': before == ui.engine.snapshot(),
            'size': [rows, cols], 'shown': shown}
result = curses.wrapper(run)
Path(sys.argv[1]).write_text(json.dumps(result))
"""


@unittest.skipUnless(os.name == "posix", "real PTY checks require a Unix terminal")
class RealTerminalTests(unittest.TestCase):
    def probe(self, rows: int, columns: int, *, resize: bool = False, history: bool = False,
              resolution: bool = False, pressure: bool = False) -> dict:
        import fcntl
        import termios

        master, slave = os.openpty()
        process = None
        try:
            fcntl.ioctl(slave, termios.TIOCSWINSZ, struct.pack("HHHH", rows, columns, 0, 0))
            with tempfile.TemporaryDirectory() as directory:
                result_path = Path(directory) / "result.json"
                process = subprocess.Popen(
                    [sys.executable, "-c", PROBE, str(result_path),
                     "resolution" if resolution else "history" if history else "pressure" if pressure else "menu"],
                    stdin=slave, stdout=slave, stderr=slave,
                    env={**os.environ, "TERM": "xterm-256color"},
                )
                deadline = time.monotonic() + 20
                captured = bytearray()
                sent = False
                while process.poll() is None and time.monotonic() < deadline:
                    ready, _, _ = select.select([master], [], [], 0.05)
                    if ready:
                        captured.extend(os.read(master, 65536))
                    marker = (b"COMBAT RESOLUTION" if resolution else b"PTY MORGUE" if history
                              else b"EXPEDITION PRESSURE" if pressure else b"PTY LONG CONTRACT")
                    if not sent and marker in captured:
                        if resize:
                            fcntl.ioctl(slave, termios.TIOCSWINSZ, struct.pack("HHHH", 60, 140, 0, 0))
                            process.send_signal(signal.SIGWINCH)
                        os.write(master, b"\x1bOF\n" if history or resolution or pressure else b"\x1b[6~\x1bOFj\n")
                        sent = True
                self.assertIsNotNone(process.poll(), "PTY timed out: " + captured[-1000:].decode(errors="replace"))
                self.assertEqual(0, process.returncode, captured[-2000:].decode(errors="replace"))
                result = json.loads(result_path.read_text())
                self.assertEqual(1, result["picked"])
                self.assertTrue(result["unchanged"])
                if pressure:
                    self.assertIn("ENEMY ROUND", result["shown"])
                elif history or resolution:
                    self.assertIn("probe:39", result["shown"])
                else:
                    self.assertIn("Consequence 89", result["shown"])
                    self.assertIn("> Walk away", result["shown"])
                return result
        finally:
            if process is not None and process.poll() is None:
                process.terminate()
                process.wait(timeout=5)
            os.close(master)
            os.close(slave)

    def test_decisions_remain_readable_at_80_by_24(self) -> None:
        self.assertEqual([24, 80], self.probe(24, 80)["size"])

    def test_decisions_remain_readable_at_140_by_60(self) -> None:
        self.assertEqual([60, 140], self.probe(60, 140)["size"])

    def test_resize_during_inspection_preserves_simulation(self) -> None:
        self.assertEqual([60, 140], self.probe(24, 80, resize=True)["size"])

    def test_resolution_arithmetic_scrolls_at_both_sizes(self) -> None:
        for rows, columns in ((24, 80), (60, 140)):
            result = self.probe(rows, columns, resolution=True)
            self.assertIn("block +1", result["shown"])

    def test_morgue_scrolls_at_80_by_24(self) -> None:
        self.assertEqual([24, 80], self.probe(24, 80, history=True)["size"])

    def test_morgue_scrolls_at_140_by_60(self) -> None:
        self.assertEqual([60, 140], self.probe(60, 140, history=True)["size"])

    def test_pressure_inspection_is_readable_and_state_neutral_at_both_sizes(self) -> None:
        for rows, columns in ((24, 80), (60, 140)):
            result = self.probe(rows, columns, pressure=True)
            self.assertIn("CURRENT QUIET", result["shown"])


if __name__ == "__main__":
    unittest.main()
