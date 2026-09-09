"""Reproducible local performance evidence; never touches a player's save."""

from __future__ import annotations

import argparse
import copy
import hashlib
import json
import math
import platform
import resource
import statistics
import subprocess
import sys
import tempfile
import time
from pathlib import Path

from .actions import _advance_world, depart, move
from .enemy_ai import next_path_step
from .inventory import InventoryTransaction, auto_pack, auto_place, create_item
from .regions import activate_region
from .route_chart import chart_move, route_preview
from .save import load_game, save_game
from .state import Position, create_world
from .terminal import RouteChartView, _draw_base, _draw_route_chart
from .world import field_of_view


class RenderSink:
    """Exercise production layout, excluding terminal-driver and human latency."""

    def __init__(self, width: int = 80, height: int = 24):
        self.width, self.height, self.cells = width, height, 0

    def getmaxyx(self):
        return self.height, self.width

    def addnstr(self, y, x, value, count, attr=0):
        self.cells += min(len(value), count)

    def addstr(self, y, x, value, attr=0):
        self.cells += len(value)

    def erase(self):
        self.cells = 0

    def refresh(self):
        pass


def distribution(samples: list[float]) -> dict[str, float | int]:
    ordered = sorted(samples)
    if not ordered:
        raise ValueError("a distribution needs at least one observation")
    return {
        "n": len(ordered), "median_ms": round(statistics.median(ordered), 3),
        "p95_ms": round(ordered[math.ceil(len(ordered) * .95) - 1], 3),
        "p99_ms": round(ordered[math.ceil(len(ordered) * .99) - 1], 3),
        "worst_ms": round(ordered[-1], 3),
    }


def measure(function, count: int, setup=None) -> dict:
    samples = []
    for index in range(count):
        argument = setup(index) if setup else None
        start = time.perf_counter_ns()
        function(argument)
        samples.append((time.perf_counter_ns() - start) / 1_000_000)
    return distribution(samples)


def replay_digest(seed: str) -> str:
    state = create_world(seed)
    depart(state)
    for dx, dy in ((1, 0), (0, 1), (-1, 0), (0, -1)) * 3:
        move(state, dx, dy)
    return hashlib.sha256(json.dumps(state.to_dict(), sort_keys=True).encode()).hexdigest()


def benchmark(samples: int = 12, seed: str = "systemic-benchmark") -> dict:
    if samples < 1:
        raise ValueError("samples must be positive")
    results = {
        "seed": seed, "python": platform.python_version(),
        "platform": platform.platform(), "units": "milliseconds",
        "render_scope": "production layout into a sink; no terminal-driver latency",
    }
    results["cold_import"] = measure(lambda _: subprocess.run(
        [sys.executable, "-c", "import jomon.main"], check=True,
        stdout=subprocess.DEVNULL, stderr=subprocess.PIPE,
    ), min(samples, 5))
    results["new_world"] = measure(lambda i: create_world(f"{seed}-{i}"), min(samples, 5), lambda i: i)
    aboard = create_world(seed)
    for kind in ("passive:reed sole wraps", "passive:rain cape", "consumable:willow dressing"):
        item = create_item(aboard, kind, "benchmark fixture", location="ground")
        if not auto_place(aboard, item.id, "pack", owner_id=aboard.active_courier_id):
            raise RuntimeError("benchmark pack fixture does not fit")
    expedition = copy.deepcopy(aboard)
    depart(expedition)
    clone = lambda _: copy.deepcopy(expedition)
    results["region_entry"] = measure(lambda state: activate_region(state, "greywash"), samples, clone)
    results["movement"] = measure(lambda state: move(state, 1, 0), samples, clone)
    results["fov"] = measure(lambda _: field_of_view(expedition, remember=False), samples)
    sink = RenderSink()
    results["render_80x24"] = measure(lambda _: _draw_base(sink, expedition), samples)
    results["input_to_layout"] = measure(lambda state: (move(state, 1, 0), _draw_base(sink, state)), samples, clone)

    def heavy_setup(_):
        state = copy.deepcopy(expedition)
        for index, actor in enumerate(state.threats):
            actor.position = Position(state.position.x + 5 + index, state.position.y + 2, 0)
            actor.status = "engaged"
            actor.last_known_position = state.position
        return state

    results["enemy_heavy_turn"] = measure(lambda state: _advance_world(state), samples, heavy_setup)

    def environment_setup(_):
        state = copy.deepcopy(expedition)
        state.smoke = {f"{x},{y},0": 4 for x in range(10, 22) for y in range(10, 22)}
        state.water = {f"{x},{y},0": 1 for x in range(12, 24) for y in range(12, 24)}
        return state

    results["environment_heavy_turn"] = measure(lambda state: _advance_world(state), samples, environment_setup)
    actor = expedition.threats[0]
    results["pathfinding"] = measure(lambda _: next_path_step(expedition, actor, expedition.position), samples)
    results["inventory_open"] = measure(lambda _: InventoryTransaction.begin(aboard), samples)
    results["auto_pack"] = measure(lambda state: auto_pack(state, "pack", owner_id=state.active_courier_id), samples, lambda _: copy.deepcopy(aboard))
    results["chart_open"] = measure(lambda _: _draw_route_chart(sink, aboard, RouteChartView(aboard.route_current_node)), samples)
    results["chart_navigation"] = measure(lambda _: (chart_move(aboard, aboard.route_current_node, 1, 0), route_preview(aboard, "reed-anchor")), samples)
    with tempfile.TemporaryDirectory(prefix="jomon-benchmark-") as directory:
        path = Path(directory) / "benchmark-save.json"
        results["save"] = measure(lambda _: save_game(expedition, path), samples)
        results["save_bytes"] = path.stat().st_size
        results["load"] = measure(lambda _: load_game(path), samples)
    results["replay_equal"] = replay_digest(seed) == replay_digest(seed)
    results["peak_rss_kib"] = resource.getrusage(resource.RUSAGE_SELF).ru_maxrss
    if sys.platform == "darwin":
        results["peak_rss_kib"] //= 1024
    return results


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--samples", type=int, default=12)
    parser.add_argument("--seed", default="systemic-benchmark")
    args = parser.parse_args()
    print(json.dumps(benchmark(args.samples, args.seed), indent=2, sort_keys=True))


if __name__ == "__main__":
    main()
