#!/usr/bin/env python3
from __future__ import annotations

import argparse
import contextlib
import io
import json
import os
import shutil
import subprocess
import sys
import threading
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Any

from kenjaku.browser_game import write_browser_game
from kenjaku.cli import main as kenjaku_main
from kenjaku.replay_viewer import write_self_play_replay_viewer_html
from kenjaku.training.interpretability_overlay import (
    build_interpretability_overlay,
    write_interpretability_overlay_html,
)

VISUAL_QA_PAGES = (
    ("play", "play/index.html"),
    ("serve-index", "serve-index/index.html"),
    ("benchmark-dashboard", "benchmark-dashboard/index.html"),
    ("training-dashboard", "training-dashboard/index.html"),
    ("replay-viewer", "replay-viewer/index.html"),
    ("interpretability-overlay", "interpretability-overlay/index.html"),
)
VIEWPORTS = {
    "desktop": (1280, 800),
    "mobile": (390, 844),
}


def main(argv: list[str] | None = None) -> int:
    args = _parser().parse_args(argv)
    output_dir = args.output_dir
    pages_dir = output_dir / "pages"
    screenshots_dir = output_dir / "screenshots"
    output_dir.mkdir(parents=True, exist_ok=True)
    pages = generate_visual_qa_pages(pages_dir)
    manifest: dict[str, Any] = {
        "kind": "kenjaku-static-visual-qa-v0",
        "pages": pages,
        "screenshots": [],
    }
    if not args.generate_only:
        playwright_cli = _resolve_playwright_cli(args.playwright_cli)
        if screenshots_dir.exists():
            shutil.rmtree(screenshots_dir)
        screenshots_dir.mkdir(parents=True, exist_ok=True)
        manifest["screenshots"] = run_visual_qa(
            pages_dir,
            screenshots_dir=screenshots_dir,
            playwright_cli=playwright_cli,
            host=args.host,
            port=args.port,
            session=args.session,
        )
    manifest_path = output_dir / "visual-qa-manifest.json"
    manifest_path.write_text(
        json.dumps(manifest, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )
    print(f"wrote visual QA pages: {pages_dir}")
    if not args.generate_only:
        print(f"wrote screenshots: {screenshots_dir}")
    print(f"wrote manifest: {manifest_path}")
    return 0


def generate_visual_qa_pages(output_dir: Path) -> list[dict[str, str]]:
    if output_dir.exists():
        shutil.rmtree(output_dir)
    output_dir.mkdir(parents=True)
    write_browser_game(output_dir / "play")
    _write_serve_fixture(output_dir / "serve-index")
    _write_benchmark_dashboard_fixture(output_dir / "benchmark-dashboard" / "index.html")
    _write_training_dashboard_fixture(output_dir / "training-dashboard" / "index.html")
    _write_replay_viewer_fixture(output_dir / "replay-viewer" / "index.html")
    _write_interpretability_overlay_fixture(output_dir / "interpretability-overlay" / "index.html")
    pages = [
        {"slug": slug, "path": relative_path}
        for slug, relative_path in VISUAL_QA_PAGES
        if (output_dir / relative_path).is_file()
    ]
    missing = [
        relative_path
        for _slug, relative_path in VISUAL_QA_PAGES
        if not (output_dir / relative_path).is_file()
    ]
    if missing:
        raise RuntimeError(f"missing visual QA pages: {', '.join(missing)}")
    return pages


def run_visual_qa(
    pages_dir: Path,
    *,
    screenshots_dir: Path,
    playwright_cli: Path,
    host: str,
    port: int,
    session: str,
) -> list[dict[str, Any]]:
    errors: list[str] = []
    handler = partial(_RecordingHandler, directory=str(pages_dir.resolve()), errors=errors)
    server = ThreadingHTTPServer((host, port), handler)
    raw_host, raw_port = server.server_address[:2]
    if not isinstance(raw_host, str) or not isinstance(raw_port, int):
        raise RuntimeError(f"unexpected server address: {server.server_address!r}")
    server_host = raw_host
    server_port = raw_port
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    screenshots: list[dict[str, Any]] = []
    try:
        _run_pw(
            playwright_cli,
            "--session",
            session,
            "open",
            _url(server_host, server_port, "play/index.html"),
        )
        for viewport_name, (width, height) in VIEWPORTS.items():
            _run_pw(playwright_cli, "--session", session, "resize", str(width), str(height))
            for slug, relative_path in VISUAL_QA_PAGES:
                errors.clear()
                url = _url(server_host, server_port, relative_path)
                _run_pw(playwright_cli, "--session", session, "goto", url)
                metrics = _page_metrics(playwright_cli, session=session)
                _assert_visual_metrics(metrics, viewport_width=width, page=relative_path)
                if errors:
                    raise RuntimeError(f"{relative_path} requested missing assets: {errors}")
                screenshot_path = screenshots_dir / f"{slug}-{viewport_name}.png"
                _run_pw(
                    playwright_cli,
                    "--session",
                    session,
                    "screenshot",
                    "--filename",
                    str(screenshot_path),
                    "--full-page",
                )
                if not screenshot_path.is_file() or screenshot_path.stat().st_size < 1024:
                    raise RuntimeError(f"blank or missing screenshot: {screenshot_path}")
                screenshots.append(
                    {
                        "page": relative_path,
                        "viewport": viewport_name,
                        "width": width,
                        "height": height,
                        "screenshot": str(screenshot_path),
                        "metrics": metrics,
                    }
                )
    finally:
        with contextlib.suppress(Exception):
            _run_pw(playwright_cli, "--session", session, "close")
        server.shutdown()
        server.server_close()
        thread.join(timeout=2)
    return screenshots


class _RecordingHandler(SimpleHTTPRequestHandler):
    def __init__(self, *args: Any, errors: list[str], **kwargs: Any) -> None:
        self._visual_qa_errors = errors
        super().__init__(*args, **kwargs)

    def send_error(
        self,
        code: int,
        message: str | None = None,
        explain: str | None = None,
    ) -> None:
        if code >= 400:
            self._visual_qa_errors.append(f"{code} {self.path}")
        super().send_error(code, message, explain)

    def log_message(self, format: str, *args: Any) -> None:
        return


def _page_metrics(playwright_cli: Path, *, session: str) -> dict[str, Any]:
    script = (
        "JSON.stringify({"
        "viewport:window.innerWidth,"
        "doc:document.documentElement.scrollWidth,"
        "body:document.body.scrollWidth,"
        "text:document.body.innerText.trim().length,"
        "focusable:document.querySelectorAll('button,a,input,select,[tabindex]').length"
        "})"
    )
    output = _run_pw(playwright_cli, "--raw", "--session", session, "eval", script)
    return _parse_playwright_raw_json(output.stdout)


def _parse_playwright_raw_json(value: str) -> dict[str, Any]:
    payload: Any = value.strip()
    for _ in range(2):
        if isinstance(payload, str):
            payload = json.loads(payload)
    if not isinstance(payload, dict):
        raise ValueError("Playwright metrics did not return an object")
    return payload


def _assert_visual_metrics(
    metrics: dict[str, Any],
    *,
    viewport_width: int,
    page: str,
) -> None:
    text_length = int(metrics.get("text", 0))
    doc_width = int(metrics.get("doc", 0))
    body_width = int(metrics.get("body", 0))
    if text_length < 20:
        raise RuntimeError(f"{page} appears blank: text length {text_length}")
    if doc_width > viewport_width or body_width > viewport_width:
        raise RuntimeError(
            f"{page} overflows viewport {viewport_width}px: doc={doc_width} body={body_width}"
        )


def _run_pw(playwright_cli: Path, *args: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [str(playwright_cli), *args],
        text=True,
        capture_output=True,
        check=True,
    )


def _resolve_playwright_cli(value: str | None) -> Path:
    candidate = value or os.environ.get("PWCLI") or shutil.which("playwright-cli")
    if not candidate:
        raise SystemExit("set --playwright-cli or PWCLI to a local playwright-cli executable")
    path = Path(candidate).expanduser()
    if not path.exists() and "/" in candidate:
        raise SystemExit(f"playwright CLI not found: {path}")
    return path


def _url(host: str, port: int, relative_path: str) -> str:
    return f"http://{host}:{port}/{relative_path}"


def _write_serve_fixture(output_dir: Path) -> None:
    (output_dir / "nested path").mkdir(parents=True)
    (output_dir / "report.json").write_text("{}", encoding="utf-8")
    (output_dir / "nested path" / "data.jsonl").write_text("{}\n", encoding="utf-8")
    _call_kenjaku("serve", "--dir", str(output_dir), "--no-serve")


def _write_benchmark_dashboard_fixture(output: Path) -> None:
    output.parent.mkdir(parents=True)
    reports = [
        output.parent / "call-a.json",
        output.parent / "call-b.json",
        output.parent / "call-c.json",
    ]
    payloads = [
        _benchmark_report("fixture-call-a", eval_accuracy=0.78, balanced_accuracy=0.74),
        _benchmark_report("fixture-call-b", eval_accuracy=0.82, balanced_accuracy=0.80),
        _benchmark_report("fixture-call-c", eval_accuracy=0.74, balanced_accuracy=0.70),
    ]
    for path, payload in zip(reports, payloads, strict=True):
        path.write_text(json.dumps(payload, sort_keys=True), encoding="utf-8")
    _call_kenjaku("benchmark-dashboard", *(str(path) for path in reports), "--output", str(output))


def _write_training_dashboard_fixture(output: Path) -> None:
    output.parent.mkdir(parents=True)
    run_a = output.parent / "mlp-a.metrics.jsonl"
    run_b = output.parent / "transformer-b.metrics.jsonl"
    _write_jsonl(
        run_a,
        [
            _training_row("mlp-a", 1, 1.4, 1.6, 0.25, 3.5, "discard-mlp-v0"),
            _training_row("mlp-a", 2, 1.1, 1.3, 0.50, 3.2, "discard-mlp-v0"),
        ],
    )
    _write_jsonl(
        run_b,
        [_training_row("transformer-b", 1, 1.5, 1.4, 0.75, 7.0, "discard-transformer-policy-v0")],
    )
    _call_kenjaku("training-dashboard", str(run_a), str(run_b), "--output", str(output))


def _write_replay_viewer_fixture(output: Path) -> None:
    rows = [
        _replay_row(0, {"kind": "discard", "tile": "5m"}),
        _replay_row(1, {"kind": "discard", "tile": "E"}),
    ]
    write_self_play_replay_viewer_html(output, rows)


def _write_interpretability_overlay_fixture(output: Path) -> None:
    report = build_interpretability_overlay([_discard_snapshot()], min_decisions=1)
    write_interpretability_overlay_html(output, report)


def _call_kenjaku(*args: str) -> None:
    stdout = io.StringIO()
    with contextlib.redirect_stdout(stdout):
        exit_code = kenjaku_main(list(args))
    if exit_code != 0:
        raise RuntimeError(f"kenjaku {' '.join(args)} failed with exit code {exit_code}")


def _write_jsonl(path: Path, rows: list[dict[str, Any]]) -> None:
    path.write_text(
        "\n".join(json.dumps(row, sort_keys=True) for row in rows) + "\n",
        encoding="utf-8",
    )


def _training_row(
    run_id: str,
    epoch: int,
    train_loss: float,
    eval_loss: float,
    accuracy: float,
    seconds: float,
    model: str,
) -> dict[str, Any]:
    return {
        "run_id": run_id,
        "epoch": epoch,
        "metrics": {
            "train_loss": train_loss,
            "eval_loss": eval_loss,
            "eval_accuracy": accuracy,
            "epoch_seconds": seconds,
        },
        "hyperparameters": {
            "learning_rate": 0.001,
            "batch_size": 8,
            "model": model,
        },
    }


def _benchmark_report(
    label: str,
    *,
    eval_accuracy: float,
    balanced_accuracy: float,
) -> dict[str, Any]:
    return {
        "kind": "kenjaku-call-benchmark-report-v0",
        "source": {
            "label": label,
            "command": "kenjaku benchmark-call local/private/raw.xml",
            "date": "2026-06-12",
        },
        "xml_file_count": 1,
        "rounds": 2,
        "call_examples": 100,
        "call_examples_total": 200,
        "example_limit": 100,
        "example_limit_strategy": "balanced",
        "split": {
            "seed": "fixed",
            "eval_fraction": 0.2,
            "train_examples": 80,
            "eval_examples": 20,
        },
        "models": {
            "call_linear_v1_calibrated": {
                "kind": "call-linear-v1",
                "feature_dim": 137,
                "training": {"positive_class_weight": 1.0},
                "policy": {"threshold": 0.4, "threshold_source": "train-best"},
                "calibration": {
                    "train": {"best": {"threshold": 0.4}},
                    "eval": {"best": {"threshold": 0.45}},
                },
                "metrics": {
                    "train_accuracy": 0.8,
                    "eval_accuracy": eval_accuracy,
                    "eval_balanced_accuracy": balanced_accuracy,
                    "eval_pass_recall": 0.76,
                    "eval_call_recall": 0.72,
                },
            },
        },
    }


def _replay_row(index: int, action: dict[str, str]) -> dict[str, Any]:
    return {
        "kind": "kenjaku-self-play-match-trajectory-row-v0",
        "game": 0,
        "round": 0,
        "step": index,
        "seat": index % 4,
        "players": 4,
        "decision_type": "discard",
        "chosen_action": action,
        "legal_actions": [{"kind": "discard", "tile": "5m"}, {"kind": "discard", "tile": "E"}],
        "rewards": [0, 0, 0, 0],
        "state": {
            "current_seat": index % 4,
            "dealer_seat": 0,
            "wall_remaining": 60 - index,
            "points": [25000, 25000, 25000, 25000],
            "hand_sizes": [13, 13, 13, 13],
            "hands": [
                ["1m", "2m", "3m", "4m", "5m"],
                ["1p", "2p", "3p", "4p", "5p"],
                ["1s", "2s", "3s", "4s", "5s"],
                ["E", "S", "W", "N", "P"],
            ],
            "discards": [["5m"], ["E"], [], []],
            "pending_reaction_seats": [],
        },
    }


def _discard_snapshot() -> dict[str, Any]:
    hand_counts = [0] * 34
    for index in range(14):
        hand_counts[index] = 1
    visible_counts = [0] * 34
    river_counts_by_seat = [[0] * 34 for _seat in range(4)]
    river_counts_by_seat[1][0] = 1
    return {
        "kind": "kenjaku-decision-snapshot-v0",
        "row_id": "visual-qa:r0:e1:discard:s0",
        "decision_type": "discard",
        "source": {"label": "visual-qa"},
        "round_index": 0,
        "event_index": 1,
        "seat": 0,
        "hand_counts": hand_counts,
        "visible_counts": visible_counts,
        "dora_indicators": ["5m"],
        "active_riichi_seats": [False, True, False, False],
        "river_counts_by_seat": river_counts_by_seat,
        "legal_actions": [
            {"kind": "discard", "tile": _tile_name(index)}
            for index, count in enumerate(hand_counts)
            if count
        ],
        "actual_action": {"kind": "discard", "tile": "1m"},
    }


def _tile_name(index: int) -> str:
    if index < 9:
        return f"{index + 1}m"
    if index < 18:
        return f"{index - 8}p"
    if index < 27:
        return f"{index - 17}s"
    return ("E", "S", "W", "N", "P", "F", "C")[index - 27]


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Generate static frontend pages and screenshots.")
    parser.add_argument("--output-dir", type=Path, default=Path("runs/visual-qa"))
    parser.add_argument("--generate-only", action="store_true")
    parser.add_argument(
        "--playwright-cli",
        help="path to playwright-cli; defaults to PWCLI or PATH",
    )
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=0)
    parser.add_argument("--session", default="kenjaku-visual-qa")
    return parser


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
