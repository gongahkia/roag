from __future__ import annotations

import argparse
import json
import os
import subprocess
from collections import Counter
from collections.abc import Callable, Iterable, Sequence
from dataclasses import dataclass
from datetime import UTC, datetime
from functools import partial
from hashlib import blake2b
from html import escape
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from time import perf_counter
from typing import Any, TypeVar
from urllib.parse import quote

from kenjaku import __version__
from kenjaku.browser_demo import write_browser_demo
from kenjaku.core import Action, ActionKind, Tile, TileType
from kenjaku.experiments import (
    DEAL_IN_BENCHMARK_REPORT_KIND,
    build_call_benchmark_report,
    build_discard_benchmark_report_from_models,
    build_discard_benchmark_summary,
    build_discard_disagreement_summary,
    build_discard_linear_report,
    build_discard_mlp_benchmark_report,
    build_discard_mlp_report,
    build_discard_transformer_benchmark_report,
    build_discard_transformer_report,
    build_public_benchmark_dashboard,
    build_riichi_benchmark_report,
    build_tenhou_inspect_report,
    format_discard_benchmark_summary,
    format_discard_disagreement_summary,
    format_public_benchmark_dashboard_html,
    write_json_report,
)
from kenjaku.hand_analysis import (
    build_hand_analysis,
    format_hand_analysis_html,
    format_hand_analysis_text,
)
from kenjaku.io import (
    TenhouDataset,
    TenhouGame,
    TenhouParseFailure,
    build_replay_public_summary_file,
    build_replay_share_plan_file,
    format_replay_intake_review,
    format_replay_public_summary,
    format_replay_share_plan,
    iter_tenhou_xml_dataset_files,
    parse_tenhou_xml_dataset,
    review_replay_manifest_file,
    tenhou_xml_files,
    write_accepted_replay_intake_jsonl,
    write_tenhou_mjai_files,
)
from kenjaku.models import (
    CALL_DECISION_KINDS,
    CALL_LINEAR_V1_FEATURE_PROFILE,
    DEAL_IN_LINEAR_MODEL_KIND,
    DEFENSE_CONTEXT_FEATURE_PROFILE,
    DEFENSE_CONTEXT_V1_FEATURE_PROFILE,
    PLACEMENT_DISCLAIMER,
    RAW_COUNT_FEATURE_PROFILE,
    RIICHI_DECISION_KINDS,
    RISK_CONTEXT_FEATURE_PROFILE,
    SHANTEN_FEATURE_PROFILE,
    CallFrequencyBaseline,
    CallLegalFrequencyBaseline,
    CallLinearModel,
    DealInLinearModel,
    DiscardFrequencyBaseline,
    DiscardLinearModel,
    PlacementModel,
    RiichiFrequencyBaseline,
    RiichiLinearModel,
    build_placement_checkpoint_metadata,
    evaluate_deal_in_probabilities,
    format_placement_probability_text,
    format_placement_training_report,
    heuristic_deal_in_probabilities,
    parse_kyoku,
    parse_scores,
    placement_examples_from_paths,
    placement_probability_payload,
)
from kenjaku.prediction import PREDICT_MODEL_TYPES, write_model_predictions
from kenjaku.replay_viewer import (
    read_self_play_trajectory_jsonl,
    write_self_play_match_trajectory_jsonl,
    write_self_play_replay_viewer_html,
)
from kenjaku.safety_advisor import (
    build_safety_advisor_report,
    format_safety_advisor_text,
)
from kenjaku.simulation import (
    SELF_PLAY_MATCH_ACTION_POLICIES,
    SELF_PLAY_MATCH_DISCARD_POLICIES,
    SELF_PLAY_MATCH_RON_POLICIES,
    SELF_PLAY_SANDBOX_POLICIES,
    SELF_PLAY_SANDBOX_REWARD_MODES,
    SELF_PLAY_SANDBOX_RULESETS,
    format_self_play_match_report,
    format_self_play_sandbox_report,
    run_self_play_match_sandbox,
    run_self_play_sandbox,
)
from kenjaku.status import build_status_payload, format_status_text
from kenjaku.training import (
    BC_DECISION_TYPES,
    BcExampleLoad,
    BcExampleShard,
    CallExample,
    DiscardExample,
    RiichiExample,
    actual_discard_has_kabe,
    actual_discard_has_one_chance,
    actual_discard_has_suji,
    actual_discard_is_genbutsu,
    actual_discard_seen_after_riichi,
    actual_discard_seen_before_riichi,
    build_bc_manifest,
    candidate_defense_risk,
    deterministic_split,
    discard_shanten_delta,
    has_active_riichi_opponent,
    iter_call_examples,
    iter_deal_in_examples,
    iter_discard_examples,
    iter_riichi_examples,
    parse_bc_decision_types,
    read_bc_examples,
    round_outcome,
    summarize_deal_in_examples,
    summarize_defense_risk_outcomes,
    summarize_defense_risks,
    summarize_discard_predictions,
    summarize_discard_shanten,
    write_bc_example_row,
    write_bc_manifest,
)
from kenjaku.training.decision_snapshots import (
    DECISION_SNAPSHOT_KIND,
    DECISION_SNAPSHOT_TYPES,
    build_decision_snapshots,
    write_decision_snapshots_jsonl,
)
from kenjaku.training.external_baselines import (
    DEFAULT_MINIMUM_COMPARABLE_DECISIONS,
    build_external_baseline_report,
    format_external_baseline_report,
    parse_external_baseline_spec,
)
from kenjaku.training.interpretability_overlay import (
    build_interpretability_overlay,
    read_interpretability_snapshots,
    write_interpretability_overlay_html,
)
from kenjaku.training_dashboard import (
    build_training_dashboard,
    format_training_dashboard_html,
)

DISCARD_BENCHMARK_MODEL_ORDER = (
    "frequency",
    "raw_count_linear",
    "linear",
    "risk_context_linear",
    "defense_context_linear",
    "defense_context_v1_linear",
)
DISCARD_BENCHMARK_FAST_MODELS = (
    "frequency",
    "linear",
    "risk_context_linear",
    "defense_context_linear",
)
DISCARD_LINEAR_FEATURE_PROFILES = {
    "raw_count_linear": RAW_COUNT_FEATURE_PROFILE,
    "linear": SHANTEN_FEATURE_PROFILE,
    "risk_context_linear": RISK_CONTEXT_FEATURE_PROFILE,
    "defense_context_linear": DEFENSE_CONTEXT_FEATURE_PROFILE,
    "defense_context_v1_linear": DEFENSE_CONTEXT_V1_FEATURE_PROFILE,
}
FEATURE_IMPORTANCE_KIND = "kenjaku-feature-importance-v0"
TRANSFORMER_ATTENTION_OVERLAY_KIND = "kenjaku-transformer-attention-overlay-v0"
FEATURE_IMPORTANCE_MODEL_ALIASES = {
    "raw_count": "raw_count_linear",
    "raw_count_linear": "raw_count_linear",
    "linear": "linear",
    "shanten": "linear",
    "risk_context": "risk_context_linear",
    "risk_context_linear": "risk_context_linear",
    "defense_context": "defense_context_linear",
    "defense_context_linear": "defense_context_linear",
    "defense_context_v1": "defense_context_v1_linear",
    "defense_context_v1_linear": "defense_context_v1_linear",
    "deal_in": "deal_in",
    "deal_in_linear": "deal_in",
}
DISAGREEMENT_REQUIRED_MODELS = (
    "risk_context_linear",
    "defense_context_linear",
    "defense_context_v1_linear",
)
DISAGREEMENT_TAGS = (
    "defense_signal",
    "efficiency_like",
    "close_logit",
    "active_riichi",
    "safe_tile_candidate",
    "no_obvious_signal",
)
DISAGREEMENT_SAFE_TILE_BUCKETS = (
    "genbutsu",
    "suji",
    "kabe",
    "one_chance",
    "seen_after_riichi",
)
DISAGREEMENT_CLOSE_LOGIT_MARGIN = 0.25
CALIBRATION_THRESHOLDS = tuple(round(index * 0.05, 2) for index in range(21))
PREDICTION_STUB_STRATEGIES = ("pass", "first-legal", "echo-actual")
CALL_LINEAR_V1_CALIBRATED_THRESHOLD = 0.40
RIICHI_LINEAR_CALIBRATED_THRESHOLD = 0.95
DEFAULT_POSITIVE_CLASS_WEIGHT = 2.0
DEFAULT_DEAL_IN_POSITIVE_CLASS_WEIGHT = 5.0
TORCH_EXTRA_HINT = "install with `pip install kenjaku[ml]`"
T = TypeVar("T")
TResult = TypeVar("TResult")
CALL_BENCHMARK_DEFAULT_MODELS = (
    "call_frequency",
    "call_legal_frequency",
    "call_linear",
    "call_linear_v1",
    "call_linear_v1_calibrated",
)
CALL_BENCHMARK_FAST_MODELS = (
    "call_frequency",
    "call_legal_frequency",
    "call_linear_v1",
    "call_linear_v1_calibrated",
)
CALL_BENCHMARK_WEIGHTED_MODEL = "call_linear_v1_weighted"
CALL_BENCHMARK_MODEL_ORDER = (
    *CALL_BENCHMARK_DEFAULT_MODELS,
    CALL_BENCHMARK_WEIGHTED_MODEL,
)
THRESHOLD_SOURCE_FIXED = "fixed"
THRESHOLD_SOURCE_TRAIN_BEST = "train-best"
THRESHOLD_SOURCE_CHOICES = (THRESHOLD_SOURCE_FIXED, THRESHOLD_SOURCE_TRAIN_BEST)
FIXED_THRESHOLD_SOURCE_LABEL = "tenhou-100-v0-eval-sweep"
CALL_EXAMPLE_LIMIT_STRATEGIES = ("prefix", "balanced")
CALL_EXAMPLE_CACHE_KIND = "kenjaku-call-example-cache-v0"
CALL_FEATURE_CACHE_KIND = "kenjaku-call-feature-cache-v0"
DECISION_SNAPSHOT_COMPARISON_KIND = "kenjaku-decision-snapshot-comparison-v0"
DEMO_FIXTURE_SOURCE = Path("data/fixtures/tenhou")
DEMO_SOURCE_DATE = "fixture-demo-v0"
DEMO_GENERATED_AT = "fixture-demo-v0"
ARTIFACT_TYPE_LABELS = {
    ".html": ("HTML", "page"),
    ".json": ("JSON", "data"),
    ".jsonl": ("JSONL", "data"),
    ".mp4": ("MP4", "video"),
    ".png": ("IMG", "image"),
    ".jpg": ("IMG", "image"),
    ".jpeg": ("IMG", "image"),
    ".gif": ("IMG", "image"),
    ".webp": ("IMG", "image"),
    ".svg": ("SVG", "image"),
    ".css": ("CSS", "style"),
    ".js": ("JS", "script"),
    ".txt": ("TXT", "text"),
    ".md": ("MD", "text"),
    ".csv": ("CSV", "table"),
    ".tsv": ("TSV", "table"),
    ".pt": ("PT", "model"),
    ".pth": ("PTH", "model"),
}


@dataclass(frozen=True, slots=True)
class _CachedCallExamples:
    examples: list[CallExample]
    parse_failures: tuple[TenhouParseFailure, ...]
    game_counts: dict[str, int]
    discard_examples: int


@dataclass(frozen=True, slots=True)
class _StreamedExamples:
    examples: list[Any]
    source_files: tuple[Path, ...]
    parsed_files: tuple[Path, ...]
    parse_failures: tuple[TenhouParseFailure, ...]
    game_counts: dict[str, int]
    source_complete: bool
    discard_examples: int | None = None
    call_examples: int | None = None


@dataclass(frozen=True, slots=True)
class _ArtifactDashboardEntry:
    rel_path: Path
    type_label: str
    type_name: str
    size: int
    modified_at: str


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="kenjaku",
        description="Riichi mahjong AI research toolkit.",
    )
    parser.add_argument("--version", action="store_true", help="print version and exit")
    subparsers = parser.add_subparsers(dest="command")

    status = subparsers.add_parser(
        "status",
        help="print implemented capabilities and local environment status",
    )
    status.add_argument(
        "--json",
        action="store_true",
        help="emit status as JSON instead of text",
    )
    status.set_defaults(func=_status)

    demo = subparsers.add_parser(
        "demo",
        help="run the fixture quickstart and write a linked artifact landing page",
    )
    demo.add_argument(
        "--output-dir",
        type=Path,
        default=Path("runs/demo"),
        help="directory for generated demo artifacts",
    )
    demo.set_defaults(func=_demo)

    browser_demo = subparsers.add_parser(
        "browser-demo",
        help="write and optionally serve the browser-playable demo",
    )
    browser_demo.add_argument(
        "--output-dir",
        type=Path,
        default=Path("runs/browser-demo"),
        help="directory for generated demo assets",
    )
    browser_demo.add_argument(
        "--host",
        default="127.0.0.1",
        help="host to bind when serving the demo",
    )
    browser_demo.add_argument(
        "--port",
        type=int,
        default=8765,
        help="port to bind when serving the demo",
    )
    browser_demo.add_argument(
        "--no-serve",
        action="store_true",
        help="write the demo assets without starting an HTTP server",
    )
    browser_demo.set_defaults(func=_browser_demo)

    serve = subparsers.add_parser(
        "serve",
        help="serve a local artifact directory with a generated landing page",
    )
    serve.add_argument(
        "--dir",
        dest="directory",
        type=Path,
        default=Path("runs"),
        help="artifact directory to index and serve",
    )
    serve.add_argument(
        "--host",
        default="127.0.0.1",
        help="host to bind when serving artifacts",
    )
    serve.add_argument(
        "--port",
        type=int,
        default=8766,
        help="port to bind when serving artifacts",
    )
    serve.add_argument(
        "--title",
        default="Kenjaku Artifact Dashboard",
        help="landing page title",
    )
    serve.add_argument(
        "--no-serve",
        action="store_true",
        help="write index.html without starting an HTTP server",
    )
    serve.set_defaults(func=_serve_artifacts)

    replay_intake = subparsers.add_parser(
        "replay-intake-review",
        help="review a permission-aware replay intake manifest",
    )
    replay_intake.add_argument(
        "manifest",
        type=Path,
        help="JSON replay manifest to review",
    )
    replay_intake.add_argument(
        "--report",
        type=Path,
        help="optional path for the full JSON intake review report",
    )
    replay_intake.add_argument(
        "--accepted-output",
        type=Path,
        help="optional JSONL output for accepted replay intake items",
    )
    replay_intake.add_argument(
        "--json",
        action="store_true",
        help="emit the review as JSON instead of text",
    )
    replay_intake.set_defaults(func=_replay_intake_review)

    replay_share = subparsers.add_parser(
        "replay-share-plan",
        help="build an offline shareability plan from accepted replay intake JSONL",
    )
    replay_share.add_argument(
        "accepted_items",
        type=Path,
        help="JSONL rows from replay-intake-review --accepted-output",
    )
    replay_share.add_argument(
        "--intent",
        choices=("demo", "redistribution"),
        default="demo",
        help="share intent to validate against permission scope",
    )
    replay_share.add_argument(
        "--report",
        type=Path,
        help="optional path for the JSON shareability plan",
    )
    replay_share.add_argument(
        "--json",
        action="store_true",
        help="emit the share plan as JSON instead of text",
    )
    replay_share.set_defaults(func=_replay_share_plan)

    replay_public_summary = subparsers.add_parser(
        "replay-public-summary",
        help="build a public-safe replay summary from accepted intake JSONL",
    )
    replay_public_summary.add_argument(
        "accepted_items",
        type=Path,
        help="JSONL rows from replay-intake-review --accepted-output",
    )
    replay_public_summary.add_argument(
        "--intent",
        choices=("demo", "redistribution"),
        default="demo",
        help="share intent to validate before summary generation",
    )
    replay_public_summary.add_argument(
        "--report",
        type=Path,
        help="optional path for the public-safe JSON summary",
    )
    replay_public_summary.add_argument(
        "--json",
        action="store_true",
        help="emit the public-safe summary as JSON instead of text",
    )
    replay_public_summary.set_defaults(func=_replay_public_summary)

    replay_viewer = subparsers.add_parser(
        "replay-viewer",
        help="render a self-play trajectory JSONL as a turn-by-turn HTML viewer",
    )
    replay_viewer.add_argument(
        "trajectory_jsonl",
        type=Path,
        help="self-play trajectory JSONL from self-play-match-sandbox",
    )
    replay_viewer.add_argument(
        "--output",
        type=Path,
        required=True,
        help="HTML viewer output path",
    )
    replay_viewer.add_argument(
        "--title",
        default="Kenjaku Self-Play Replay Viewer",
        help="HTML document title",
    )
    replay_viewer.set_defaults(func=_replay_viewer)

    self_play = subparsers.add_parser(
        "self-play-sandbox",
        help="run a deterministic offline draw/discard self-play sandbox",
    )
    self_play.add_argument(
        "--episodes",
        type=int,
        default=1,
        help="number of sandbox episodes to simulate",
    )
    self_play.add_argument(
        "--max-turns",
        type=int,
        default=64,
        help="maximum draw/discard turns per episode",
    )
    self_play.add_argument(
        "--seed",
        default="kenjaku-self-play-v0",
        help="stable seed for deterministic sandbox episodes",
    )
    self_play.add_argument(
        "--policy",
        choices=SELF_PLAY_SANDBOX_POLICIES,
        default="random",
        help="sandbox discard policy",
    )
    self_play.add_argument(
        "--ruleset",
        choices=SELF_PLAY_SANDBOX_RULESETS,
        default="tenhou-4p",
        help="sandbox static tile set and player count",
    )
    self_play.add_argument(
        "--reward-mode",
        choices=SELF_PLAY_SANDBOX_REWARD_MODES,
        default="terminal",
        help="reward vector to expose as the selected sandbox reward",
    )
    self_play.add_argument(
        "--include-trajectories",
        action="store_true",
        help="include full synthetic draw/discard trajectories in JSON output",
    )
    self_play.add_argument(
        "--stop-on-tsumo",
        action="store_true",
        help="stop an episode on basic closed-hand tsumo shape detection",
    )
    self_play.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON sandbox report artifact",
    )
    self_play.add_argument(
        "--json",
        action="store_true",
        help="emit the sandbox report as JSON instead of text",
    )
    self_play.set_defaults(func=_self_play_sandbox)

    self_play_match = subparsers.add_parser(
        "self-play-match-sandbox",
        help="run deterministic multi-round sandbox matches to final result",
    )
    self_play_match.add_argument(
        "--games",
        type=int,
        default=1,
        help="number of sandbox matches to simulate",
    )
    self_play_match.add_argument(
        "--max-rounds",
        type=int,
        default=32,
        help="maximum hands per match before reporting an incomplete match",
    )
    self_play_match.add_argument(
        "--max-turns-per-round",
        type=int,
        default=512,
        help="maximum action decisions per hand before max-turn termination",
    )
    self_play_match.add_argument(
        "--seed",
        default="kenjaku-self-play-match-v0",
        help="stable seed for deterministic sandbox matches",
    )
    self_play_match.add_argument(
        "--ruleset",
        choices=SELF_PLAY_SANDBOX_RULESETS,
        default="tenhou-4p",
        help="sandbox static tile set and player count",
    )
    self_play_match.add_argument(
        "--discard-policy",
        choices=SELF_PLAY_MATCH_DISCARD_POLICIES,
        default="drawn",
        help="discard policy used by every seat",
    )
    self_play_match.add_argument(
        "--call-policy",
        choices=SELF_PLAY_MATCH_ACTION_POLICIES,
        default="pass",
        help="call policy used during pending discard reactions",
    )
    self_play_match.add_argument(
        "--riichi-policy",
        choices=SELF_PLAY_MATCH_ACTION_POLICIES,
        default="pass",
        help="riichi declaration policy used on self turns",
    )
    self_play_match.add_argument(
        "--kan-policy",
        choices=SELF_PLAY_MATCH_ACTION_POLICIES,
        default="pass",
        help="kan policy used on self turns",
    )
    self_play_match.add_argument(
        "--kita-policy",
        choices=SELF_PLAY_MATCH_ACTION_POLICIES,
        default="pass",
        help="Sanma Kita policy used on self turns",
    )
    self_play_match.add_argument(
        "--ron-policy",
        choices=SELF_PLAY_MATCH_RON_POLICIES,
        default="win",
        help="ron/tsumo win policy",
    )
    self_play_match.add_argument(
        "--include-trajectories",
        action="store_true",
        help="include state/action/reward trajectories in JSON output",
    )
    self_play_match.add_argument(
        "--trajectory-jsonl",
        type=Path,
        help="optional JSONL output for flattened match trajectory rows",
    )
    self_play_match.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON match report artifact",
    )
    self_play_match.add_argument(
        "--json",
        action="store_true",
        help="emit the match report as JSON instead of text",
    )
    self_play_match.set_defaults(func=_self_play_match_sandbox)

    train_ppo = subparsers.add_parser(
        "train-ppo-sandbox",
        help="run a fixture-scale PPO smoke trainer on sandbox self-play trajectories",
    )
    train_ppo.add_argument(
        "--total-steps",
        type=int,
        default=1024,
        help="minimum additional environment decisions to train on",
    )
    train_ppo.add_argument(
        "--rollout-games",
        type=int,
        default=1,
        help="sandbox matches collected for each PPO update",
    )
    train_ppo.add_argument(
        "--max-rounds",
        type=int,
        default=12,
        help="maximum hands per collected sandbox match",
    )
    train_ppo.add_argument(
        "--max-turns-per-round",
        type=int,
        default=512,
        help="maximum action decisions per collected hand",
    )
    train_ppo.add_argument(
        "--seed",
        default="kenjaku-ppo-sandbox-v0",
        help="stable seed for deterministic PPO rollouts",
    )
    train_ppo.add_argument(
        "--ruleset",
        choices=SELF_PLAY_SANDBOX_RULESETS,
        default="tenhou-4p",
        help="sandbox static tile set and player count",
    )
    train_ppo.add_argument(
        "--rollout-discard-policy",
        choices=SELF_PLAY_MATCH_DISCARD_POLICIES,
        default="drawn",
        help="discard policy used to collect PPO trajectories",
    )
    train_ppo.add_argument(
        "--rollout-call-policy",
        choices=SELF_PLAY_MATCH_ACTION_POLICIES,
        default="pass",
        help="call policy used to collect PPO trajectories",
    )
    train_ppo.add_argument(
        "--rollout-riichi-policy",
        choices=SELF_PLAY_MATCH_ACTION_POLICIES,
        default="pass",
        help="riichi policy used to collect PPO trajectories",
    )
    train_ppo.add_argument(
        "--rollout-kan-policy",
        choices=SELF_PLAY_MATCH_ACTION_POLICIES,
        default="pass",
        help="kan policy used to collect PPO trajectories",
    )
    train_ppo.add_argument(
        "--rollout-kita-policy",
        choices=SELF_PLAY_MATCH_ACTION_POLICIES,
        default="pass",
        help="Kita policy used to collect PPO trajectories",
    )
    train_ppo.add_argument(
        "--rollout-ron-policy",
        choices=SELF_PLAY_MATCH_RON_POLICIES,
        default="pass",
        help="ron/tsumo policy used to collect PPO trajectories",
    )
    train_ppo.add_argument("--ppo-epochs", type=int, default=2, help="PPO epochs per rollout")
    train_ppo.add_argument("--batch-size", type=int, default=64, help="PPO mini-batch size")
    train_ppo.add_argument(
        "--learning-rate",
        type=float,
        default=0.001,
        help="Torch SGD learning rate",
    )
    train_ppo.add_argument("--hidden-dim", type=int, default=0, help="reserved model width")
    train_ppo.add_argument("--gamma", type=float, default=0.99, help="discount factor")
    train_ppo.add_argument("--gae-lambda", type=float, default=0.95, help="GAE lambda")
    train_ppo.add_argument(
        "--clip-epsilon",
        type=float,
        default=0.2,
        help="PPO probability-ratio clipping epsilon",
    )
    train_ppo.add_argument(
        "--entropy-coef",
        type=float,
        default=0.01,
        help="entropy regularization coefficient",
    )
    train_ppo.add_argument(
        "--value-coef",
        type=float,
        default=0.5,
        help="value loss coefficient",
    )
    train_ppo.add_argument(
        "--max-grad-norm",
        type=float,
        default=0.5,
        help="gradient clipping norm",
    )
    train_ppo.add_argument(
        "--reward-scale",
        type=float,
        default=100.0,
        help="divide final-score rewards by this value",
    )
    train_ppo.add_argument(
        "--supervised-warmup-epochs",
        type=int,
        default=0,
        help="optional behavior-cloning warmup epochs on collected rollout actions",
    )
    train_ppo.add_argument(
        "--model-seed",
        "--torch-seed",
        dest="torch_seed",
        type=int,
        default=0,
        help="model initialization and mini-batch random seed",
    )
    train_ppo.add_argument(
        "--device",
        choices=("auto", "cpu", "cuda", "mps"),
        default="auto",
        help="Torch device for PPO training",
    )
    train_ppo.add_argument(
        "--checkpoint",
        type=Path,
        help="optional path for the PPO checkpoint artifact",
    )
    train_ppo.add_argument(
        "--resume",
        type=Path,
        help="optional PPO checkpoint path to resume from",
    )
    train_ppo.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON PPO report artifact",
    )
    train_ppo.add_argument(
        "--json",
        action="store_true",
        help="emit the PPO report as JSON instead of text",
    )
    train_ppo.set_defaults(func=_train_ppo_sandbox)

    train_population = subparsers.add_parser(
        "train-population-sandbox",
        help="run population-pool PPO snapshot plumbing on sandbox trajectories",
    )
    train_population.add_argument(
        "--pool-size",
        type=int,
        default=4,
        help="number of active policy snapshots to maintain",
    )
    train_population.add_argument("--generations", type=int, default=1, help="generations to run")
    train_population.add_argument(
        "--candidates-per-generation",
        type=int,
        default=1,
        help="candidate snapshots trained per generation",
    )
    train_population.add_argument(
        "--matchups-per-candidate",
        type=int,
        default=2,
        help="sampled pool matchups per candidate or initial snapshot",
    )
    train_population.add_argument(
        "--total-steps",
        type=int,
        default=1024,
        help="minimum environment decisions per trained snapshot",
    )
    train_population.add_argument(
        "--max-rounds",
        type=int,
        default=12,
        help="maximum hands per PPO training rollout",
    )
    train_population.add_argument(
        "--max-turns-per-round",
        type=int,
        default=512,
        help="maximum action decisions per PPO training hand",
    )
    train_population.add_argument(
        "--seed",
        default="kenjaku-population-sandbox-v0",
        help="stable seed for deterministic population training",
    )
    train_population.add_argument(
        "--ruleset",
        choices=SELF_PLAY_SANDBOX_RULESETS,
        default="tenhou-4p",
        help="sandbox static tile set and player count",
    )
    train_population.add_argument(
        "--ppo-epochs",
        type=int,
        default=1,
        help="PPO epochs per snapshot rollout",
    )
    train_population.add_argument("--batch-size", type=int, default=64, help="PPO mini-batch size")
    train_population.add_argument(
        "--learning-rate",
        type=float,
        default=0.001,
        help="Torch SGD learning rate",
    )
    train_population.add_argument(
        "--hidden-dim",
        type=int,
        default=0,
        help="reserved model width",
    )
    train_population.add_argument(
        "--evaluation-games",
        type=int,
        default=1,
        help="sandbox games per sampled matchup",
    )
    train_population.add_argument(
        "--evaluation-max-rounds",
        type=int,
        help="maximum hands per sampled matchup; defaults to --max-rounds",
    )
    train_population.add_argument(
        "--evaluation-max-turns-per-round",
        type=int,
        help="maximum decisions per matchup hand; defaults to --max-turns-per-round",
    )
    train_population.add_argument(
        "--promotion-margin",
        type=float,
        default=0.0,
        help="average-score margin required to replace the pool floor",
    )
    train_population.add_argument(
        "--output-dir",
        type=Path,
        help="optional directory for population PPO checkpoint artifacts",
    )
    train_population.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON population report artifact",
    )
    train_population.add_argument(
        "--json",
        action="store_true",
        help="emit the population report as JSON instead of text",
    )
    train_population.set_defaults(func=_train_population_sandbox)

    training_dashboard = subparsers.add_parser(
        "training-dashboard",
        help="build a static dashboard from training metrics JSONL files",
    )
    training_dashboard.add_argument(
        "metrics_jsonl",
        nargs="+",
        type=Path,
        help="training epoch metrics JSONL files",
    )
    training_dashboard.add_argument(
        "--output",
        type=Path,
        required=True,
        help="path to the static HTML dashboard to write",
    )
    training_dashboard.add_argument(
        "--title",
        default="Kenjaku Training Dashboard",
        help="dashboard page title",
    )
    training_dashboard.set_defaults(func=_training_dashboard)

    inspect_tenhou = subparsers.add_parser(
        "inspect-tenhou",
        help="parse a Tenhou XML file and print Phase 0 dataset counts",
    )
    inspect_tenhou.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    inspect_tenhou.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON inspection report artifact",
    )
    inspect_tenhou.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_parse_cache_arg(inspect_tenhou)
    _add_source_args(inspect_tenhou)
    inspect_tenhou.set_defaults(func=_inspect_tenhou)

    tenhou_to_mjai = subparsers.add_parser(
        "tenhou-to-mjai",
        help="convert Tenhou XML files into MJAI JSONL streams",
    )
    tenhou_to_mjai.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    tenhou_to_mjai.add_argument(
        "--output",
        type=Path,
        required=True,
        help="directory for .mjson output files",
    )
    tenhou_to_mjai.set_defaults(func=_tenhou_to_mjai)

    defense_risk = subparsers.add_parser(
        "defense-risk-summary",
        help="summarize heuristic discard danger scores from Tenhou XML",
    )
    defense_risk.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    defense_risk.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON defense-risk summary report",
    )
    defense_risk.add_argument(
        "--json",
        action="store_true",
        help="emit the summary as JSON instead of text",
    )
    defense_risk.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_parse_cache_arg(defense_risk)
    _add_source_args(defense_risk)
    defense_risk.set_defaults(func=_defense_risk_summary)

    safety_advisor = subparsers.add_parser(
        "safety-advisor",
        help="rank hand tiles by riichi-defense safety signals",
    )
    safety_advisor.add_argument("--hand", required=True, help="candidate hand tiles")
    safety_advisor.add_argument(
        "--river",
        required=True,
        help="active opponent river tiles",
    )
    safety_advisor.add_argument(
        "--active-riichi",
        required=True,
        help="comma-separated active riichi seats, e.g. 1,3",
    )
    safety_advisor.add_argument("--seat", type=int, default=0, help="self seat 0..3")
    safety_advisor.add_argument(
        "--output",
        choices=("text", "json"),
        default="text",
        help="output format",
    )
    safety_advisor.set_defaults(func=_safety_advisor)

    deal_in = subparsers.add_parser(
        "benchmark-deal-in",
        help="train/evaluate a small direct ron-discard probability estimator",
    )
    deal_in.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    deal_in.add_argument(
        "--epochs",
        type=int,
        default=50,
        help="training epochs for the logistic estimator",
    )
    deal_in.add_argument(
        "--learning-rate",
        type=float,
        default=0.1,
        help="logistic estimator learning rate",
    )
    deal_in.add_argument(
        "--l2",
        type=float,
        default=0.0,
        help="L2 regularization strength",
    )
    deal_in.add_argument(
        "--positive-class-weight",
        type=float,
        default=DEFAULT_DEAL_IN_POSITIVE_CLASS_WEIGHT,
        help="weight applied to direct deal-in examples during training",
    )
    deal_in.add_argument(
        "--eval-fraction",
        type=float,
        default=0.25,
        help="fraction of examples assigned to eval split",
    )
    deal_in.add_argument(
        "--split-seed",
        default="kenjaku-deal-in-v0",
        help="stable seed for train/eval split",
    )
    deal_in.add_argument(
        "--threshold",
        type=float,
        default=0.5,
        help="probability threshold for binary metrics",
    )
    deal_in.add_argument(
        "--active-riichi-only",
        action="store_true",
        help="train/evaluate only discard examples with active riichi opponents",
    )
    deal_in.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON deal-in benchmark report",
    )
    deal_in.add_argument(
        "--json",
        action="store_true",
        help="emit the benchmark report as JSON instead of text",
    )
    deal_in.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_parse_cache_arg(deal_in)
    _add_source_args(deal_in)
    deal_in.set_defaults(func=_benchmark_deal_in)

    train_placement = subparsers.add_parser(
        "train-placement",
        help="train a sandbox final-placement probability estimator",
    )
    train_placement.add_argument(
        "--data",
        nargs="+",
        type=Path,
        required=True,
        help="Tenhou XML files or directories",
    )
    train_placement.add_argument(
        "--output",
        type=Path,
        required=True,
        help="path for the placement checkpoint JSON",
    )
    train_placement.add_argument(
        "--epochs",
        type=int,
        default=50,
        help="training epochs for the placement estimator",
    )
    train_placement.add_argument(
        "--learning-rate",
        type=float,
        default=0.05,
        help="softmax estimator learning rate",
    )
    train_placement.add_argument(
        "--l2",
        type=float,
        default=0.0,
        help="L2 regularization strength",
    )
    train_placement.add_argument(
        "--json",
        action="store_true",
        help="emit the training checkpoint metadata as JSON instead of text",
    )
    _add_parse_cache_arg(train_placement)
    train_placement.set_defaults(func=_train_placement)

    placement_probability = subparsers.add_parser(
        "placement-probability",
        help="estimate final-placement probabilities from current scores and round state",
    )
    placement_probability.add_argument(
        "--model",
        type=Path,
        help="optional placement checkpoint JSON from train-placement",
    )
    placement_probability.add_argument(
        "--scores",
        required=True,
        help="comma-separated current scores for seats 0..3",
    )
    placement_probability.add_argument(
        "--kyoku",
        required=True,
        help="round label such as E1, E4, S1, or South-2",
    )
    placement_probability.add_argument("--honba", type=int, default=0, help="honba count")
    placement_probability.add_argument("--kyotaku", type=int, default=0, help="riichi stick count")
    placement_probability.add_argument("--dealer", type=int, default=0, help="dealer seat 0..3")
    placement_probability.add_argument("--seat", type=int, default=0, help="seat to estimate 0..3")
    placement_probability.add_argument(
        "--output",
        choices=("text", "json"),
        default="text",
        help="output format",
    )
    placement_probability.set_defaults(func=_placement_probability)

    analyze_hand = subparsers.add_parser(
        "analyze-hand",
        help="rank discard candidates for a single hand position",
    )
    analyze_hand.add_argument("--hand", required=True, help="hand tiles, e.g. '234m 567p 22s'")
    analyze_hand.add_argument("--drawn", required=True, help="drawn tile")
    analyze_hand.add_argument("--seat", type=int, required=True, help="seat 0..3")
    analyze_hand.add_argument(
        "--round",
        dest="round_wind",
        required=True,
        help="round wind: E, S, W, N, East, South, West, or North",
    )
    analyze_hand.add_argument(
        "--dora",
        action="append",
        default=[],
        help="dora indicator tile; repeat for multiple indicators",
    )
    analyze_hand.add_argument(
        "--model",
        type=Path,
        help="optional discard linear checkpoint JSON",
    )
    analyze_hand.add_argument(
        "--output",
        choices=("text", "json", "html"),
        default="text",
        help="output format",
    )
    analyze_hand.set_defaults(func=_analyze_hand)

    export_snapshots = subparsers.add_parser(
        "export-decision-snapshots",
        help="export neutral JSONL decision snapshots from Tenhou XML",
    )
    export_snapshots.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    export_snapshots.add_argument(
        "--output",
        type=Path,
        required=True,
        help="JSONL path for exported decision snapshots",
    )
    export_snapshots.add_argument(
        "--decision-types",
        default=",".join(DECISION_SNAPSHOT_TYPES),
        help="comma-separated snapshot types: discard,call,riichi",
    )
    export_snapshots.add_argument(
        "--limit",
        type=int,
        help="maximum snapshots to write after deterministic ordering",
    )
    export_snapshots.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    export_snapshots.add_argument(
        "--include-outcome",
        action="store_true",
        help="include terminal score-delta labels; opt-in to avoid future outcome leakage",
    )
    _add_parse_cache_arg(export_snapshots)
    _add_source_args(export_snapshots)
    export_snapshots.set_defaults(func=_export_decision_snapshots)

    snapshot_summary = subparsers.add_parser(
        "decision-snapshot-summary",
        help="summarize neutral JSONL decision snapshot exports",
    )
    snapshot_summary.add_argument(
        "snapshots",
        nargs="+",
        type=Path,
        help="decision snapshot JSONL files",
    )
    snapshot_summary.add_argument(
        "--json",
        action="store_true",
        help="emit the summary as JSON instead of text",
    )
    snapshot_summary.set_defaults(func=_decision_snapshot_summary)

    interpretability_overlay = subparsers.add_parser(
        "interpretability-overlay",
        help="render discard interpretability HTML from decision snapshot JSONL",
    )
    interpretability_overlay.add_argument(
        "snapshots",
        type=Path,
        help="decision snapshot JSONL file",
    )
    interpretability_overlay.add_argument(
        "--output",
        type=Path,
        required=True,
        help="HTML output path",
    )
    interpretability_overlay.add_argument(
        "--limit",
        type=int,
        help="maximum discard decisions to render",
    )
    interpretability_overlay.add_argument(
        "--min-decisions",
        type=int,
        default=0,
        help="fail unless at least this many discard decisions render",
    )
    interpretability_overlay.add_argument(
        "--title",
        default="Kenjaku Interpretability Overlay",
        help="HTML document title",
    )
    interpretability_overlay.set_defaults(func=_interpretability_overlay)

    transformer_attention = subparsers.add_parser(
        "transformer-attention-overlay",
        help="render transformer attention heatmaps from checkpoint and discard snapshots",
    )
    transformer_attention.add_argument(
        "checkpoint",
        type=Path,
        help="discard transformer checkpoint path",
    )
    transformer_attention.add_argument(
        "snapshots",
        type=Path,
        help="decision snapshot JSONL file",
    )
    transformer_attention.add_argument(
        "--output",
        type=Path,
        required=True,
        help="HTML output path",
    )
    transformer_attention.add_argument(
        "--limit",
        type=int,
        default=20,
        help="maximum discard decisions to render",
    )
    transformer_attention.add_argument(
        "--max-heads",
        type=int,
        default=4,
        help="maximum attention heads per layer to render",
    )
    transformer_attention.add_argument(
        "--device",
        choices=("auto", "cpu", "mps", "cuda"),
        default="cpu",
        help="checkpoint inference device",
    )
    transformer_attention.add_argument(
        "--title",
        default="Kenjaku Transformer Attention Overlay",
        help="HTML document title",
    )
    transformer_attention.set_defaults(func=_transformer_attention_overlay)

    feature_importance = subparsers.add_parser(
        "feature-importance",
        help="rank linear-model features from a benchmark report",
    )
    feature_importance.add_argument(
        "model",
        type=Path,
        help="benchmark report JSON with weight_summary and feature_summary blocks",
    )
    feature_importance.add_argument(
        "--profile",
        required=True,
        help="linear profile/model: RISK_CONTEXT, DEFENSE_CONTEXT, raw_count, deal_in",
    )
    feature_importance.add_argument(
        "--top-k",
        type=int,
        default=20,
        help="maximum ranked features to emit",
    )
    feature_importance.add_argument(
        "--output",
        type=Path,
        required=True,
        help="feature-importance JSON output path",
    )
    feature_importance.set_defaults(func=_feature_importance)

    produce_predictions = subparsers.add_parser(
        "produce-decision-predictions",
        help="write stub prediction JSONL rows for decision snapshot protocol tests",
    )
    produce_predictions.add_argument(
        "snapshots",
        type=Path,
        help="decision snapshot JSONL file",
    )
    produce_predictions.add_argument(
        "--output",
        required=True,
        type=Path,
        help="prediction JSONL output path",
    )
    produce_predictions.add_argument(
        "--strategy",
        choices=PREDICTION_STUB_STRATEGIES,
        default="pass",
        help="stub prediction strategy; protocol tests only",
    )
    produce_predictions.set_defaults(func=_produce_decision_predictions)

    predict = subparsers.add_parser(
        "predict",
        help="run batch model inference over decision snapshot JSONL",
    )
    predict.add_argument(
        "--snapshots",
        type=Path,
        required=True,
        help="decision snapshot JSONL file",
    )
    predict.add_argument(
        "--model",
        choices=PREDICT_MODEL_TYPES,
        required=True,
        help="model family to run",
    )
    predict.add_argument(
        "--checkpoint",
        type=Path,
        help="model checkpoint path; required except for frequency",
    )
    predict.add_argument(
        "--output",
        type=Path,
        required=True,
        help="prediction JSONL output path",
    )
    predict.add_argument(
        "--device",
        default="cpu",
        help="PyTorch inference device for mlp-discard or transformer-discard",
    )
    predict.add_argument(
        "--batch-size",
        type=int,
        default=64,
        help="PyTorch inference batch size",
    )
    predict.set_defaults(func=_predict)

    snapshot_compare = subparsers.add_parser(
        "decision-snapshot-compare",
        help="compare decision snapshots against neutral JSONL predictions",
    )
    snapshot_compare.add_argument(
        "snapshots",
        type=Path,
        help="decision snapshot JSONL file",
    )
    snapshot_compare.add_argument(
        "predictions",
        type=Path,
        help="prediction JSONL file with row_id and predicted_action",
    )
    snapshot_compare.add_argument(
        "--json",
        action="store_true",
        help="emit the comparison as JSON instead of text",
    )
    snapshot_compare.set_defaults(func=_decision_snapshot_compare)

    external_baselines = subparsers.add_parser(
        "external-baseline-report",
        help="build an offline shared-log report for named external baseline predictions",
    )
    external_baselines.add_argument(
        "snapshots",
        type=Path,
        help="decision snapshot JSONL file shared by every baseline",
    )
    external_baselines.add_argument(
        "--baseline",
        action="append",
        required=True,
        help=(
            "baseline prediction JSONL as FAMILY:NAME=PATH; repeat for Kenjaku, "
            "Mortal-compatible, and akochan-compatible outputs"
        ),
    )
    external_baselines.add_argument(
        "--min-decisions",
        type=int,
        default=DEFAULT_MINIMUM_COMPARABLE_DECISIONS,
        help=(
            "minimum comparable decisions required per baseline "
            f"(default: {DEFAULT_MINIMUM_COMPARABLE_DECISIONS})"
        ),
    )
    external_baselines.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON external-baseline report",
    )
    external_baselines.add_argument(
        "--json",
        action="store_true",
        help="emit the report as JSON instead of text",
    )
    external_baselines.set_defaults(func=_external_baseline_report)

    external_producer = subparsers.add_parser(
        "run-external-prediction-producer",
        help="run a subprocess that converts decision snapshots into prediction JSONL",
    )
    external_producer.add_argument(
        "snapshots",
        type=Path,
        help="decision snapshot JSONL file",
    )
    external_producer.add_argument(
        "--output",
        required=True,
        type=Path,
        help="prediction JSONL path the external command must write",
    )
    external_producer.add_argument(
        "--timeout-seconds",
        type=float,
        help="optional subprocess timeout",
    )
    external_producer.add_argument(
        "--compare-report",
        type=Path,
        help="optional path for a decision-snapshot comparison JSON report",
    )
    external_producer.add_argument(
        "--command",
        nargs=argparse.REMAINDER,
        required=True,
        help=(
            "external command to run; put this option last. The command receives "
            "KENJAKU_SNAPSHOTS and KENJAKU_PREDICTIONS in its environment"
        ),
    )
    external_producer.set_defaults(func=_run_external_prediction_producer)

    train_baseline = subparsers.add_parser(
        "train-discard-baseline",
        help="fit the deterministic discard frequency baseline on one Tenhou XML file",
    )
    train_baseline.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    train_baseline.add_argument(
        "--skip-errors",
        action="store_true",
        help="skip files that fail Tenhou XML parsing",
    )
    _add_parse_cache_arg(train_baseline)
    train_baseline.set_defaults(func=_train_discard_baseline)

    train_linear = subparsers.add_parser(
        "train-discard-linear",
        help="fit the tiny dependency-free linear discard model on one Tenhou XML file",
    )
    train_linear.add_argument("paths", nargs="+", type=Path, help="Tenhou XML files or directories")
    train_linear.add_argument("--epochs", type=int, default=25, help="training epochs")
    train_linear.add_argument(
        "--learning-rate",
        type=float,
        default=0.1,
        help="SGD learning rate",
    )
    train_linear.add_argument(
        "--l2",
        type=float,
        default=0.0,
        help="L2 regularization strength",
    )
    train_linear.add_argument(
        "--eval-fraction",
        type=float,
        default=0.2,
        help="fraction of examples reserved for deterministic evaluation",
    )
    train_linear.add_argument(
        "--split-seed",
        default="kenjaku-v0",
        help="stable seed for deterministic train/eval split",
    )
    train_linear.add_argument(
        "--output",
        type=Path,
        help="optional path for the trained JSON model artifact",
    )
    train_linear.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON training report artifact",
    )
    train_linear.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_parse_cache_arg(train_linear)
    _add_source_args(train_linear)
    train_linear.set_defaults(func=_train_discard_linear)

    train_mlp = subparsers.add_parser(
        "train-discard-mlp",
        help="fit a small PyTorch masked-logit discard MLP",
    )
    train_mlp.add_argument("paths", nargs="+", type=Path, help="Tenhou XML files or directories")
    train_mlp.add_argument("--epochs", type=int, default=5, help="training epochs")
    train_mlp.add_argument(
        "--batch-size",
        type=int,
        default=64,
        help="mini-batch size",
    )
    train_mlp.add_argument(
        "--learning-rate",
        type=float,
        default=0.001,
        help="AdamW learning rate",
    )
    train_mlp.add_argument(
        "--hidden-dim",
        type=int,
        default=128,
        help="hidden layer width",
    )
    train_mlp.add_argument(
        "--eval-fraction",
        type=float,
        default=0.2,
        help="fraction of examples reserved for deterministic evaluation",
    )
    train_mlp.add_argument(
        "--split-seed",
        default="kenjaku-v0",
        help="stable seed for deterministic train/eval split",
    )
    train_mlp.add_argument(
        "--seed",
        type=int,
        default=0,
        help="torch and dataloader random seed",
    )
    train_mlp.add_argument(
        "--device",
        choices=("auto", "cpu", "mps", "cuda"),
        default="auto",
        help="training device",
    )
    train_mlp.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON training report artifact",
    )
    train_mlp.add_argument(
        "--checkpoint",
        type=Path,
        help="optional path for the best PyTorch checkpoint artifact",
    )
    train_mlp.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_parse_cache_arg(train_mlp)
    _add_source_args(train_mlp)
    train_mlp.set_defaults(func=_train_discard_mlp)

    train_transformer = subparsers.add_parser(
        "train-discard-transformer",
        help="fit a PyTorch transformer masked-logit discard policy",
    )
    train_transformer.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    train_transformer.add_argument("--epochs", type=int, default=5, help="training epochs")
    train_transformer.add_argument(
        "--batch-size",
        type=int,
        default=64,
        help="mini-batch size",
    )
    train_transformer.add_argument(
        "--learning-rate",
        type=float,
        default=0.001,
        help="AdamW learning rate",
    )
    train_transformer.add_argument(
        "--model-dim",
        type=int,
        default=64,
        help="transformer hidden width",
    )
    train_transformer.add_argument(
        "--num-heads",
        type=int,
        default=4,
        help="transformer attention heads",
    )
    train_transformer.add_argument(
        "--num-layers",
        type=int,
        default=2,
        help="transformer encoder layers",
    )
    train_transformer.add_argument(
        "--feedforward-dim",
        type=int,
        default=128,
        help="transformer feedforward width",
    )
    train_transformer.add_argument(
        "--dropout",
        type=float,
        default=0.1,
        help="transformer dropout",
    )
    train_transformer.add_argument(
        "--value-head",
        action="store_true",
        help="enable an auxiliary scalar value head",
    )
    train_transformer.add_argument(
        "--eval-fraction",
        type=float,
        default=0.2,
        help="fraction of examples reserved for deterministic evaluation",
    )
    train_transformer.add_argument(
        "--split-seed",
        default="kenjaku-v0",
        help="stable seed for deterministic train/eval split",
    )
    train_transformer.add_argument(
        "--seed",
        type=int,
        default=0,
        help="torch and dataloader random seed",
    )
    train_transformer.add_argument(
        "--device",
        choices=("auto", "cpu", "mps", "cuda"),
        default="auto",
        help="training device",
    )
    train_transformer.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON training report artifact",
    )
    train_transformer.add_argument(
        "--checkpoint",
        type=Path,
        help="optional path for the best PyTorch checkpoint artifact",
    )
    train_transformer.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_parse_cache_arg(train_transformer)
    _add_source_args(train_transformer)
    train_transformer.set_defaults(func=_train_discard_transformer)

    export_bc = subparsers.add_parser(
        "export-bc-examples",
        help="stream Tenhou XML into behavior-cloning JSONL shards",
    )
    export_bc.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    export_bc.add_argument(
        "--output-dir",
        required=True,
        type=Path,
        help="directory for ignored BC JSONL shards and manifest.json",
    )
    export_bc.add_argument(
        "--actions",
        default=",".join(BC_DECISION_TYPES),
        help="comma-separated decision types to export: discard,call,riichi",
    )
    export_bc.add_argument(
        "--shard-size",
        type=int,
        default=50000,
        help="maximum examples per JSONL shard",
    )
    export_bc.add_argument(
        "--limit-per-type",
        type=int,
        help="optional maximum examples to export per decision type",
    )
    export_bc.add_argument(
        "--overwrite",
        action="store_true",
        help="replace existing BC shard files in --output-dir",
    )
    export_bc.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_parse_cache_arg(export_bc)
    _add_source_args(export_bc)
    export_bc.set_defaults(func=_export_bc_examples)

    benchmark_discard_examples = subparsers.add_parser(
        "benchmark-discard-from-examples",
        help="benchmark discard baselines from exported BC JSONL shards",
    )
    benchmark_discard_examples.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="BC manifest, JSONL files, or shard directories",
    )
    benchmark_discard_examples.add_argument("--epochs", type=int, default=25)
    benchmark_discard_examples.add_argument("--learning-rate", type=float, default=0.1)
    benchmark_discard_examples.add_argument("--l2", type=float, default=0.0)
    benchmark_discard_examples.add_argument(
        "--models",
        default="all",
        help="discard benchmark models: all, fast, or comma-separated model names",
    )
    benchmark_discard_examples.add_argument("--eval-fraction", type=float, default=0.2)
    benchmark_discard_examples.add_argument("--split-seed", default="kenjaku-v0")
    benchmark_discard_examples.add_argument("--example-limit", type=int)
    benchmark_discard_examples.add_argument("--report", type=Path)
    _add_source_args(benchmark_discard_examples)
    benchmark_discard_examples.set_defaults(func=_benchmark_discard_from_examples)

    benchmark_discard = subparsers.add_parser(
        "benchmark-discard",
        help="compare deterministic discard baselines on one train/eval split",
    )
    benchmark_discard.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    benchmark_discard.add_argument("--epochs", type=int, default=25, help="linear model epochs")
    benchmark_discard.add_argument(
        "--learning-rate",
        type=float,
        default=0.1,
        help="linear model SGD learning rate",
    )
    benchmark_discard.add_argument(
        "--l2",
        type=float,
        default=0.0,
        help="linear model L2 regularization strength",
    )
    benchmark_discard.add_argument(
        "--models",
        default="all",
        help=(
            "discard benchmark models: all, fast, or comma-separated model names "
            "(frequency, raw_count_linear, linear, risk_context_linear, "
            "defense_context_linear, defense_context_v1_linear)"
        ),
    )
    benchmark_discard.add_argument(
        "--eval-fraction",
        type=float,
        default=0.2,
        help="fraction of examples reserved for deterministic evaluation",
    )
    benchmark_discard.add_argument(
        "--split-seed",
        default="kenjaku-v0",
        help="stable seed for deterministic train/eval split",
    )
    benchmark_discard.add_argument(
        "--example-limit",
        type=int,
        help="maximum discard examples to use after deterministic reconstruction",
    )
    benchmark_discard.add_argument(
        "--stream-examples",
        action="store_true",
        help=(
            "collect examples file-by-file and stop at --example-limit instead of "
            "retaining the full parsed dataset"
        ),
    )
    benchmark_discard.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON benchmark report artifact",
    )
    benchmark_discard.add_argument(
        "--disagreements",
        type=Path,
        help="optional path for a JSON model-disagreement diagnostic artifact",
    )
    benchmark_discard.add_argument(
        "--max-disagreements",
        type=int,
        default=100,
        help="maximum stored examples per disagreement category",
    )
    benchmark_discard.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_parse_cache_arg(benchmark_discard)
    _add_source_args(benchmark_discard)
    benchmark_discard.set_defaults(func=_benchmark_discard)

    benchmark_mlp = subparsers.add_parser(
        "benchmark-discard-mlp",
        help="compare a small PyTorch discard MLP against discard baseline anchors",
    )
    benchmark_mlp.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    benchmark_mlp.add_argument("--epochs", type=int, default=5, help="MLP training epochs")
    benchmark_mlp.add_argument(
        "--batch-size",
        type=int,
        default=64,
        help="MLP mini-batch size",
    )
    benchmark_mlp.add_argument(
        "--learning-rate",
        type=float,
        default=0.001,
        help="MLP AdamW learning rate",
    )
    benchmark_mlp.add_argument(
        "--hidden-dim",
        type=int,
        default=128,
        help="MLP hidden layer width",
    )
    benchmark_mlp.add_argument(
        "--linear-epochs",
        type=int,
        default=3,
        help="linear anchor training epochs",
    )
    benchmark_mlp.add_argument(
        "--linear-learning-rate",
        type=float,
        default=0.05,
        help="linear anchor SGD learning rate",
    )
    benchmark_mlp.add_argument(
        "--linear-l2",
        type=float,
        default=0.0,
        help="linear anchor L2 regularization strength",
    )
    benchmark_mlp.add_argument(
        "--eval-fraction",
        type=float,
        default=0.2,
        help="fraction of examples reserved for deterministic evaluation",
    )
    benchmark_mlp.add_argument(
        "--split-seed",
        default="kenjaku-v0",
        help="stable seed for deterministic train/eval split",
    )
    benchmark_mlp.add_argument(
        "--seed",
        type=int,
        default=0,
        help="torch and dataloader random seed",
    )
    benchmark_mlp.add_argument(
        "--device",
        choices=("auto", "cpu", "mps", "cuda"),
        default="auto",
        help="training device",
    )
    benchmark_mlp.add_argument(
        "--checkpoint",
        type=Path,
        help="optional path for the best PyTorch checkpoint artifact",
    )
    benchmark_mlp.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON MLP benchmark report artifact",
    )
    benchmark_mlp.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_parse_cache_arg(benchmark_mlp)
    _add_source_args(benchmark_mlp)
    benchmark_mlp.set_defaults(func=_benchmark_discard_mlp)

    benchmark_transformer = subparsers.add_parser(
        "benchmark-discard-transformer",
        help="compare a PyTorch discard transformer against discard baseline anchors",
    )
    benchmark_transformer.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    benchmark_transformer.add_argument(
        "--epochs",
        type=int,
        default=5,
        help="transformer training epochs",
    )
    benchmark_transformer.add_argument(
        "--batch-size",
        type=int,
        default=64,
        help="transformer mini-batch size",
    )
    benchmark_transformer.add_argument(
        "--learning-rate",
        type=float,
        default=0.001,
        help="transformer AdamW learning rate",
    )
    benchmark_transformer.add_argument(
        "--model-dim",
        type=int,
        default=64,
        help="transformer hidden width",
    )
    benchmark_transformer.add_argument(
        "--num-heads",
        type=int,
        default=4,
        help="transformer attention heads",
    )
    benchmark_transformer.add_argument(
        "--num-layers",
        type=int,
        default=2,
        help="transformer encoder layers",
    )
    benchmark_transformer.add_argument(
        "--feedforward-dim",
        type=int,
        default=128,
        help="transformer feedforward width",
    )
    benchmark_transformer.add_argument(
        "--dropout",
        type=float,
        default=0.1,
        help="transformer dropout",
    )
    benchmark_transformer.add_argument(
        "--value-head",
        action="store_true",
        help="enable an auxiliary scalar value head",
    )
    benchmark_transformer.add_argument(
        "--linear-epochs",
        type=int,
        default=3,
        help="linear anchor training epochs",
    )
    benchmark_transformer.add_argument(
        "--linear-learning-rate",
        type=float,
        default=0.05,
        help="linear anchor SGD learning rate",
    )
    benchmark_transformer.add_argument(
        "--linear-l2",
        type=float,
        default=0.0,
        help="linear anchor L2 regularization strength",
    )
    benchmark_transformer.add_argument(
        "--eval-fraction",
        type=float,
        default=0.2,
        help="fraction of examples reserved for deterministic evaluation",
    )
    benchmark_transformer.add_argument(
        "--split-seed",
        default="kenjaku-v0",
        help="stable seed for deterministic train/eval split",
    )
    benchmark_transformer.add_argument(
        "--seed",
        type=int,
        default=0,
        help="torch and dataloader random seed",
    )
    benchmark_transformer.add_argument(
        "--device",
        choices=("auto", "cpu", "mps", "cuda"),
        default="auto",
        help="training device",
    )
    benchmark_transformer.add_argument(
        "--checkpoint",
        type=Path,
        help="optional path for the best PyTorch checkpoint artifact",
    )
    benchmark_transformer.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON transformer benchmark report artifact",
    )
    benchmark_transformer.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_parse_cache_arg(benchmark_transformer)
    _add_source_args(benchmark_transformer)
    benchmark_transformer.set_defaults(func=_benchmark_discard_transformer)

    benchmark_summary = subparsers.add_parser(
        "benchmark-report-summary",
        help="summarize one or more benchmark JSON reports",
    )
    benchmark_summary.add_argument(
        "reports",
        nargs="+",
        type=Path,
        help="benchmark report JSON files",
    )
    benchmark_summary.add_argument(
        "--json",
        action="store_true",
        help="emit the summary as JSON instead of text",
    )
    benchmark_summary.set_defaults(func=_benchmark_report_summary)

    benchmark_dashboard = subparsers.add_parser(
        "benchmark-dashboard",
        help="build a static public dashboard from benchmark JSON reports",
    )
    benchmark_dashboard.add_argument(
        "reports",
        nargs="+",
        type=Path,
        help="benchmark report JSON files",
    )
    benchmark_dashboard.add_argument(
        "--output",
        required=True,
        type=Path,
        help="path to the static HTML dashboard to write",
    )
    benchmark_dashboard.add_argument(
        "--title",
        default="Kenjaku Offline Benchmark Dashboard",
        help="dashboard page title",
    )
    benchmark_dashboard.set_defaults(func=_benchmark_dashboard)

    disagreement_summary = subparsers.add_parser(
        "disagreement-report-summary",
        help="summarize one or more discard disagreement JSON reports",
    )
    disagreement_summary.add_argument(
        "reports",
        nargs="+",
        type=Path,
        help="discard disagreement report JSON files",
    )
    disagreement_summary.add_argument(
        "--json",
        action="store_true",
        help="emit the summary as JSON instead of text",
    )
    disagreement_summary.add_argument(
        "--examples",
        type=int,
        default=0,
        help="append this many stored representative examples per category in text mode",
    )
    disagreement_summary.add_argument(
        "--tags",
        action="store_true",
        help="append deterministic disagreement tag counts from stored examples",
    )
    disagreement_summary.add_argument(
        "--tag",
        choices=DISAGREEMENT_TAGS,
        help="when rendering examples, include only stored items with this deterministic tag",
    )
    disagreement_summary.set_defaults(func=_disagreement_report_summary)

    benchmark_call = subparsers.add_parser(
        "benchmark-call",
        help="compare deterministic call/pass baselines on one train/eval split",
    )
    benchmark_call.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    benchmark_call.add_argument(
        "--eval-fraction",
        type=float,
        default=0.2,
        help="fraction of examples reserved for deterministic evaluation",
    )
    benchmark_call.add_argument(
        "--split-seed",
        default="kenjaku-v0",
        help="stable seed for deterministic train/eval split",
    )
    benchmark_call.add_argument("--epochs", type=int, default=25, help="call linear model epochs")
    benchmark_call.add_argument(
        "--learning-rate",
        type=float,
        default=0.1,
        help="call linear model SGD learning rate",
    )
    benchmark_call.add_argument(
        "--l2",
        type=float,
        default=0.0,
        help="call linear model L2 regularization strength",
    )
    benchmark_call.add_argument(
        "--call-threshold",
        type=float,
        default=CALL_LINEAR_V1_CALIBRATED_THRESHOLD,
        help="non-pass probability threshold for call_linear_v1_calibrated",
    )
    benchmark_call.add_argument(
        "--call-threshold-source",
        choices=THRESHOLD_SOURCE_CHOICES,
        default=THRESHOLD_SOURCE_FIXED,
        help="threshold source for call_linear_v1_calibrated",
    )
    benchmark_call.add_argument(
        "--models",
        default="all",
        help=(
            "call benchmark models: all, fast, or comma-separated model names "
            "(call_frequency, call_legal_frequency, call_linear, call_linear_v1, "
            "call_linear_v1_calibrated, call_linear_v1_weighted)"
        ),
    )
    benchmark_call.add_argument(
        "--example-limit",
        type=int,
        help="maximum call examples to keep after deterministic reconstruction order",
    )
    benchmark_call.add_argument(
        "--stream-examples",
        action="store_true",
        help=(
            "collect examples file-by-file and stop at --example-limit instead of "
            "retaining the full parsed dataset"
        ),
    )
    benchmark_call.add_argument(
        "--example-limit-strategy",
        choices=CALL_EXAMPLE_LIMIT_STRATEGIES,
        default="prefix",
        help="call example limiting strategy",
    )
    benchmark_call.add_argument(
        "--profile-stages",
        action="store_true",
        help="print and record call benchmark stage timings",
    )
    benchmark_call.add_argument(
        "--example-cache",
        type=Path,
        help="optional ignored JSON cache for reconstructed call examples",
    )
    benchmark_call.add_argument(
        "--feature-cache",
        type=Path,
        help="optional ignored JSON feature cache for prepared call examples",
    )
    benchmark_call.add_argument(
        "--include-weighted",
        action="store_true",
        help="include positive class-weighted call linear v1 comparison variants",
    )
    benchmark_call.add_argument(
        "--call-positive-weight",
        type=float,
        default=DEFAULT_POSITIVE_CLASS_WEIGHT,
        help="positive example weight for --include-weighted call linear training",
    )
    benchmark_call.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON call benchmark report artifact",
    )
    benchmark_call.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_parse_cache_arg(benchmark_call)
    _add_source_args(benchmark_call)
    benchmark_call.set_defaults(func=_benchmark_call)

    benchmark_call_examples = subparsers.add_parser(
        "benchmark-call-from-examples",
        help="benchmark call/pass baselines from exported BC JSONL shards",
    )
    benchmark_call_examples.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="BC manifest, JSONL files, or shard directories",
    )
    benchmark_call_examples.add_argument("--eval-fraction", type=float, default=0.2)
    benchmark_call_examples.add_argument("--split-seed", default="kenjaku-v0")
    benchmark_call_examples.add_argument("--epochs", type=int, default=25)
    benchmark_call_examples.add_argument("--learning-rate", type=float, default=0.1)
    benchmark_call_examples.add_argument("--l2", type=float, default=0.0)
    benchmark_call_examples.add_argument(
        "--call-threshold",
        type=float,
        default=CALL_LINEAR_V1_CALIBRATED_THRESHOLD,
    )
    benchmark_call_examples.add_argument(
        "--call-threshold-source",
        choices=THRESHOLD_SOURCE_CHOICES,
        default=THRESHOLD_SOURCE_FIXED,
    )
    benchmark_call_examples.add_argument("--models", default="all")
    benchmark_call_examples.add_argument("--example-limit", type=int)
    benchmark_call_examples.add_argument(
        "--example-limit-strategy",
        choices=CALL_EXAMPLE_LIMIT_STRATEGIES,
        default="prefix",
    )
    benchmark_call_examples.add_argument("--include-weighted", action="store_true")
    benchmark_call_examples.add_argument(
        "--call-positive-weight",
        type=float,
        default=DEFAULT_POSITIVE_CLASS_WEIGHT,
    )
    benchmark_call_examples.add_argument("--report", type=Path)
    _add_source_args(benchmark_call_examples)
    benchmark_call_examples.set_defaults(func=_benchmark_call_from_examples)

    benchmark_riichi = subparsers.add_parser(
        "benchmark-riichi",
        help="compare deterministic riichi/pass baselines on one train/eval split",
    )
    benchmark_riichi.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="Tenhou XML files or directories",
    )
    benchmark_riichi.add_argument(
        "--eval-fraction",
        type=float,
        default=0.2,
        help="fraction of examples reserved for deterministic evaluation",
    )
    benchmark_riichi.add_argument(
        "--split-seed",
        default="kenjaku-v0",
        help="stable seed for deterministic train/eval split",
    )
    benchmark_riichi.add_argument(
        "--example-limit",
        type=int,
        help="maximum riichi/pass examples to use after deterministic reconstruction",
    )
    benchmark_riichi.add_argument(
        "--stream-examples",
        action="store_true",
        help=(
            "collect examples file-by-file and stop at --example-limit instead of "
            "retaining the full parsed dataset"
        ),
    )
    benchmark_riichi.add_argument(
        "--epochs",
        type=int,
        default=25,
        help="riichi linear model epochs",
    )
    benchmark_riichi.add_argument(
        "--learning-rate",
        type=float,
        default=0.1,
        help="riichi linear model SGD learning rate",
    )
    benchmark_riichi.add_argument(
        "--l2",
        type=float,
        default=0.0,
        help="riichi linear model L2 regularization strength",
    )
    benchmark_riichi.add_argument(
        "--riichi-threshold",
        type=float,
        default=RIICHI_LINEAR_CALIBRATED_THRESHOLD,
        help="riichi probability threshold for riichi_linear_calibrated",
    )
    benchmark_riichi.add_argument(
        "--riichi-threshold-source",
        choices=THRESHOLD_SOURCE_CHOICES,
        default=THRESHOLD_SOURCE_FIXED,
        help="threshold source for riichi_linear_calibrated",
    )
    benchmark_riichi.add_argument(
        "--include-weighted",
        action="store_true",
        help="include positive class-weighted riichi linear comparison variants",
    )
    benchmark_riichi.add_argument(
        "--riichi-positive-weight",
        type=float,
        default=DEFAULT_POSITIVE_CLASS_WEIGHT,
        help="positive example weight for --include-weighted riichi linear training",
    )
    benchmark_riichi.add_argument(
        "--report",
        type=Path,
        help="optional path for a JSON riichi benchmark report artifact",
    )
    benchmark_riichi.add_argument(
        "--skip-errors",
        action="store_true",
        help="record parse failures and continue with successfully parsed files",
    )
    _add_parse_cache_arg(benchmark_riichi)
    _add_source_args(benchmark_riichi)
    benchmark_riichi.set_defaults(func=_benchmark_riichi)

    benchmark_riichi_examples = subparsers.add_parser(
        "benchmark-riichi-from-examples",
        help="benchmark riichi/pass baselines from exported BC JSONL shards",
    )
    benchmark_riichi_examples.add_argument(
        "paths",
        nargs="+",
        type=Path,
        help="BC manifest, JSONL files, or shard directories",
    )
    benchmark_riichi_examples.add_argument("--eval-fraction", type=float, default=0.2)
    benchmark_riichi_examples.add_argument("--split-seed", default="kenjaku-v0")
    benchmark_riichi_examples.add_argument("--example-limit", type=int)
    benchmark_riichi_examples.add_argument("--epochs", type=int, default=25)
    benchmark_riichi_examples.add_argument("--learning-rate", type=float, default=0.1)
    benchmark_riichi_examples.add_argument("--l2", type=float, default=0.0)
    benchmark_riichi_examples.add_argument(
        "--riichi-threshold",
        type=float,
        default=RIICHI_LINEAR_CALIBRATED_THRESHOLD,
    )
    benchmark_riichi_examples.add_argument(
        "--riichi-threshold-source",
        choices=THRESHOLD_SOURCE_CHOICES,
        default=THRESHOLD_SOURCE_FIXED,
    )
    benchmark_riichi_examples.add_argument("--include-weighted", action="store_true")
    benchmark_riichi_examples.add_argument(
        "--riichi-positive-weight",
        type=float,
        default=DEFAULT_POSITIVE_CLASS_WEIGHT,
    )
    benchmark_riichi_examples.add_argument("--report", type=Path)
    _add_source_args(benchmark_riichi_examples)
    benchmark_riichi_examples.set_defaults(func=_benchmark_riichi_from_examples)
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    if args.version:
        print(f"kenjaku {__version__}")
        return 0

    if hasattr(args, "func"):
        return args.func(args)

    parser.print_help()
    return 0


def _status(args: argparse.Namespace) -> int:
    payload = build_status_payload()
    if args.json:
        print(json.dumps(payload, indent=2, sort_keys=True))
        return 0

    print(format_status_text(payload))
    return 0


def _demo(args: argparse.Namespace) -> int:
    output_dir = args.output_dir
    output_dir.mkdir(parents=True, exist_ok=True)
    source = {
        "label": "fixture-demo",
        "command": f"kenjaku demo --output-dir {output_dir}",
        "date": DEMO_SOURCE_DATE,
    }
    browser_dir = output_dir / "browser-demo"
    dashboard_path = output_dir / "benchmark-dashboard" / "index.html"
    snapshots_path = output_dir / "decision-snapshots.jsonl"
    summary_path = output_dir / "decision-snapshot-summary.json"
    predictions_path = output_dir / "decision-predictions.jsonl"
    comparison_path = output_dir / "snapshot-comparison.json"
    benchmark_path = output_dir / "discard-benchmark.json"
    manifest_path = output_dir / "manifest.json"
    landing_path = output_dir / "index.html"

    browser_manifest = write_browser_demo(browser_dir)
    dataset = parse_tenhou_xml_dataset([DEMO_FIXTURE_SOURCE], skip_errors=False)
    snapshots = build_decision_snapshots(
        dataset.game,
        decision_types=DECISION_SNAPSHOT_TYPES,
        limit=20,
        source=source,
        input_paths=[DEMO_FIXTURE_SOURCE],
        xml_file_count=len(dataset.files),
        include_outcome=False,
    )
    snapshot_count = write_decision_snapshots_jsonl(snapshots_path, snapshots)
    summary = _build_decision_snapshot_summary([snapshots_path])
    write_json_report(summary_path, summary)
    prediction_stats = _write_stub_decision_predictions(
        snapshots_path=snapshots_path,
        output_path=predictions_path,
        strategy="echo-actual",
    )
    comparison = _build_decision_snapshot_comparison(snapshots_path, predictions_path)
    write_json_report(comparison_path, comparison)

    _benchmark_discard(
        argparse.Namespace(
            paths=[DEMO_FIXTURE_SOURCE],
            epochs=25,
            learning_rate=0.1,
            l2=0.0,
            models="frequency",
            eval_fraction=0.2,
            split_seed="kenjaku-demo-v0",
            example_limit=None,
            stream_examples=False,
            report=benchmark_path,
            disagreements=None,
            max_disagreements=100,
            skip_errors=False,
            source_label=source["label"],
            source_command=source["command"],
            source_date=source["date"],
        )
    )
    dashboard = build_public_benchmark_dashboard(
        [benchmark_path],
        version=__version__,
        generated_at=DEMO_GENERATED_AT,
        title="Kenjaku Fixture Demo",
    )
    dashboard_path.parent.mkdir(parents=True, exist_ok=True)
    dashboard_path.write_text(
        format_public_benchmark_dashboard_html(
            dashboard,
            link_base_dir=dashboard_path.parent.resolve(),
        ),
        encoding="utf-8",
    )

    artifacts = {
        "landing_page": landing_path,
        "browser_demo": Path(browser_manifest["entrypoint"]),
        "decision_snapshots": snapshots_path,
        "decision_snapshot_summary": summary_path,
        "decision_predictions": predictions_path,
        "snapshot_comparison": comparison_path,
        "discard_benchmark": benchmark_path,
        "benchmark_dashboard": dashboard_path,
    }
    manifest = {
        "kind": "kenjaku-demo-manifest-v0",
        "version": __version__,
        "source": source,
        "fixture_source": str(DEMO_FIXTURE_SOURCE),
        "snapshot_count": snapshot_count,
        "prediction_count": prediction_stats["predictions"],
        "artifacts": {
            name: _artifact_link(path, output_dir=output_dir) for name, path in artifacts.items()
        },
    }
    write_json_report(manifest_path, manifest)
    artifacts["manifest"] = manifest_path
    _write_demo_landing_page(landing_path, artifacts, output_dir=output_dir)

    print(f"wrote demo: {landing_path}")
    print(f"Open {landing_path}")
    return 0


def _browser_demo(args: argparse.Namespace) -> int:
    if not 0 <= args.port <= 65535:
        raise SystemExit("--port must be between 0 and 65535")

    manifest = write_browser_demo(args.output_dir)
    output_dir = Path(manifest["output_dir"])
    entrypoint = Path(manifest["entrypoint"])
    print(f"wrote browser demo: {entrypoint}")
    for file_path in manifest["files"]:
        print(f"asset: {file_path}")

    if args.no_serve:
        print(f"open: {entrypoint}")
        return 0

    handler = partial(
        SimpleHTTPRequestHandler,
        directory=str(output_dir.resolve()),
    )
    server = ThreadingHTTPServer((args.host, args.port), handler)
    host, port = server.server_address[:2]
    print(f"serving browser demo: http://{host}:{port}/")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nstopped browser demo server")
    finally:
        server.server_close()
    return 0


def _serve_artifacts(args: argparse.Namespace) -> int:
    if not 0 <= args.port <= 65535:
        raise SystemExit("--port must be between 0 and 65535")

    root = args.directory
    if root.exists() and not root.is_dir():
        raise SystemExit(f"--dir is not a directory: {root}")
    root.mkdir(parents=True, exist_ok=True)

    index_path = root / "index.html"
    artifacts = _discover_dashboard_artifacts(root, index_path=index_path)
    _write_artifact_dashboard_index(
        index_path,
        title=args.title,
        artifacts=artifacts,
    )
    print(f"wrote artifact dashboard: {index_path}")

    if args.no_serve:
        print(f"open: {index_path}")
        return 0

    handler = partial(
        SimpleHTTPRequestHandler,
        directory=str(root.resolve()),
    )
    server = ThreadingHTTPServer((args.host, args.port), handler)
    host, port = server.server_address[:2]
    print(f"serving artifacts: http://{host}:{port}/")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nstopped artifact server")
    finally:
        server.server_close()
    return 0


def _discover_dashboard_artifacts(
    root: Path,
    *,
    index_path: Path,
) -> list[_ArtifactDashboardEntry]:
    root = root.resolve()
    index_path = index_path.resolve()
    entries: list[_ArtifactDashboardEntry] = []
    for path in sorted(root.rglob("*"), key=lambda item: item.as_posix().lower()):
        if not path.is_file() or path.resolve() == index_path:
            continue
        rel_path = path.resolve().relative_to(root)
        stat = path.stat()
        type_label, type_name = _artifact_dashboard_type(path)
        entries.append(
            _ArtifactDashboardEntry(
                rel_path=rel_path,
                type_label=type_label,
                type_name=type_name,
                size=stat.st_size,
                modified_at=_format_artifact_timestamp(stat.st_mtime),
            )
        )
    return entries


def _artifact_dashboard_type(path: Path) -> tuple[str, str]:
    suffix = path.suffix.lower()
    if suffix in ARTIFACT_TYPE_LABELS:
        return ARTIFACT_TYPE_LABELS[suffix]
    if suffix:
        label = suffix[1:].upper()[:8]
        return label, "file"
    return "FILE", "file"


def _format_artifact_timestamp(timestamp: float) -> str:
    return (
        datetime.fromtimestamp(timestamp, UTC)
        .replace(microsecond=0)
        .isoformat()
        .replace("+00:00", "Z")
    )


def _format_artifact_size(size: int) -> str:
    if size < 1024:
        return f"{size} B"
    value = float(size)
    for unit in ("KB", "MB", "GB", "TB"):
        value /= 1024
        if value < 1024 or unit == "TB":
            return f"{value:.1f} {unit}"
    return f"{value:.1f} TB"


def _write_artifact_dashboard_index(
    path: Path,
    *,
    title: str,
    artifacts: Sequence[_ArtifactDashboardEntry],
) -> None:
    generated_at = datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")
    artifact_count = len(artifacts)
    type_count = len({artifact.type_label for artifact in artifacts})
    if artifacts:
        items = "\n".join(_format_artifact_dashboard_item(item) for item in artifacts)
    else:
        items = '        <p class="empty">No artifacts found.</p>'

    path.write_text(
        f"""<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>{escape(title)}</title>
    <style>
      body {{
        margin: 0;
        font-family: system-ui, sans-serif;
        color: #18202a;
        background: #f5f7f8;
      }}
      main {{
        max-width: 960px;
        margin: 0 auto;
        padding: 40px 24px;
      }}
      header {{ margin-bottom: 24px; }}
      h1 {{ margin: 0 0 8px; font-size: 2rem; }}
      p {{ margin: 0; color: #4d5965; }}
      .list {{ display: grid; gap: 10px; }}
      .artifact {{
        display: grid;
        grid-template-columns: 72px 1fr auto;
        gap: 14px;
        align-items: center;
        padding: 12px 14px;
        border: 1px solid #d9e0e6;
        border-radius: 8px;
        background: #fff;
        color: inherit;
        text-decoration: none;
      }}
      .artifact:hover {{ border-color: #8ab4d8; }}
      .type-icon {{
        display: inline-flex;
        justify-content: center;
        align-items: center;
        min-width: 48px;
        padding: 4px 8px;
        border-radius: 4px;
        background: #eaf2f8;
        color: #174d78;
        font-size: 0.78rem;
        font-weight: 700;
      }}
      .path {{ overflow-wrap: anywhere; font-weight: 600; }}
      .meta {{ color: #64707d; font-size: 0.9rem; white-space: nowrap; }}
      .empty {{
        padding: 16px;
        border: 1px dashed #b8c3cc;
        border-radius: 8px;
        background: #fff;
      }}
      @media (max-width: 680px) {{
        .artifact {{ grid-template-columns: 64px 1fr; }}
        .meta {{ grid-column: 2; white-space: normal; }}
      }}
    </style>
  </head>
  <body>
    <main>
      <header>
        <h1>{escape(title)}</h1>
        <p>{artifact_count} artifacts, {type_count} types. Generated {generated_at}.</p>
      </header>
      <section class="list" aria-label="Artifacts">
{items}
      </section>
    </main>
  </body>
</html>
""",
        encoding="utf-8",
    )


def _format_artifact_dashboard_item(artifact: _ArtifactDashboardEntry) -> str:
    rel_path = artifact.rel_path.as_posix()
    href = escape(quote(rel_path, safe="/"), quote=True)
    meta = f"{artifact.type_name} | {_format_artifact_size(artifact.size)} | {artifact.modified_at}"
    return (
        f'        <a class="artifact" href="{href}">'
        f'<span class="type-icon">{escape(artifact.type_label)}</span>'
        f'<span class="path">{escape(rel_path)}</span>'
        f'<span class="meta">{escape(meta)}</span></a>'
    )


def _replay_intake_review(args: argparse.Namespace) -> int:
    try:
        review = review_replay_manifest_file(args.manifest)
    except (OSError, ValueError, json.JSONDecodeError) as error:
        raise SystemExit(str(error)) from error

    if args.report is not None:
        write_json_report(args.report, review)
    if args.accepted_output is not None:
        write_accepted_replay_intake_jsonl(args.accepted_output, review)

    if args.json:
        print(json.dumps(review, indent=2, sort_keys=True))
    else:
        print(format_replay_intake_review(review))
    if args.report is not None:
        print(f"report_path: {args.report}")
    if args.accepted_output is not None:
        print(f"accepted_output_path: {args.accepted_output}")
    return 0


def _replay_share_plan(args: argparse.Namespace) -> int:
    try:
        plan = build_replay_share_plan_file(args.accepted_items, intent=args.intent)
    except (OSError, ValueError, json.JSONDecodeError) as error:
        raise SystemExit(str(error)) from error

    if args.report is not None:
        write_json_report(args.report, plan)

    if args.json:
        print(json.dumps(plan, indent=2, sort_keys=True))
    else:
        print(format_replay_share_plan(plan))
    if args.report is not None:
        print(f"report_path: {args.report}")
    return 0


def _replay_public_summary(args: argparse.Namespace) -> int:
    try:
        summary = build_replay_public_summary_file(
            args.accepted_items,
            intent=args.intent,
        )
    except ValueError as error:
        raise SystemExit(str(error)) from error

    if args.report is not None:
        write_json_report(args.report, summary)

    if args.json:
        print(json.dumps(summary, indent=2, sort_keys=True))
    else:
        print(format_replay_public_summary(summary))
    if args.report is not None:
        print(f"report_path: {args.report}")
    return 0


def _replay_viewer(args: argparse.Namespace) -> int:
    try:
        rows = read_self_play_trajectory_jsonl(args.trajectory_jsonl)
    except (OSError, ValueError, json.JSONDecodeError) as error:
        raise SystemExit(str(error)) from error

    write_self_play_replay_viewer_html(args.output, rows, title=args.title)
    print(f"trajectory_rows: {len(rows)}")
    print(f"output_path: {args.output}")
    return 0


def _self_play_sandbox(args: argparse.Namespace) -> int:
    try:
        report = run_self_play_sandbox(
            episodes=args.episodes,
            max_turns=args.max_turns,
            seed=args.seed,
            policy=args.policy,
            ruleset=args.ruleset,
            reward_mode=args.reward_mode,
            include_trajectories=args.include_trajectories,
            stop_on_tsumo=args.stop_on_tsumo,
        )
    except ValueError as error:
        raise SystemExit(str(error)) from error

    if args.report is not None:
        write_json_report(args.report, report)

    if args.json:
        print(json.dumps(report, indent=2, sort_keys=True))
    else:
        print(format_self_play_sandbox_report(report))
    if args.report is not None:
        print(f"report_path: {args.report}")
    return 0


def _self_play_match_sandbox(args: argparse.Namespace) -> int:
    include_trajectories = args.include_trajectories or args.trajectory_jsonl is not None
    try:
        report = run_self_play_match_sandbox(
            games=args.games,
            max_rounds=args.max_rounds,
            max_turns_per_round=args.max_turns_per_round,
            seed=args.seed,
            ruleset=args.ruleset,
            discard_policy=args.discard_policy,
            call_policy=args.call_policy,
            riichi_policy=args.riichi_policy,
            kan_policy=args.kan_policy,
            kita_policy=args.kita_policy,
            ron_policy=args.ron_policy,
            include_trajectories=include_trajectories,
        )
    except ValueError as error:
        raise SystemExit(str(error)) from error

    if args.report is not None:
        write_json_report(args.report, report)
    if args.trajectory_jsonl is not None:
        trajectory_rows = write_self_play_match_trajectory_jsonl(
            args.trajectory_jsonl,
            report,
        )
    else:
        trajectory_rows = None

    if args.json:
        print(json.dumps(report, indent=2, sort_keys=True))
    else:
        print(format_self_play_match_report(report))
    if args.report is not None:
        print(f"report_path: {args.report}")
    if args.trajectory_jsonl is not None:
        print(f"trajectory_path: {args.trajectory_jsonl}")
        print(f"trajectory_rows: {trajectory_rows}")
    return 0


def _train_ppo_sandbox(args: argparse.Namespace) -> int:
    try:
        from kenjaku.training.ppo import (
            format_ppo_sandbox_report,
            save_ppo_sandbox_checkpoint,
            train_ppo_sandbox,
        )
    except ImportError as error:
        raise SystemExit("PPO sandbox trainer dependencies are unavailable") from error

    try:
        result = train_ppo_sandbox(
            total_steps=args.total_steps,
            rollout_games=args.rollout_games,
            max_rounds=args.max_rounds,
            max_turns_per_round=args.max_turns_per_round,
            seed=args.seed,
            ruleset=args.ruleset,
            rollout_discard_policy=args.rollout_discard_policy,
            rollout_call_policy=args.rollout_call_policy,
            rollout_riichi_policy=args.rollout_riichi_policy,
            rollout_kan_policy=args.rollout_kan_policy,
            rollout_kita_policy=args.rollout_kita_policy,
            rollout_ron_policy=args.rollout_ron_policy,
            ppo_epochs=args.ppo_epochs,
            batch_size=args.batch_size,
            learning_rate=args.learning_rate,
            hidden_dim=args.hidden_dim,
            gamma=args.gamma,
            gae_lambda=args.gae_lambda,
            clip_epsilon=args.clip_epsilon,
            entropy_coef=args.entropy_coef,
            value_coef=args.value_coef,
            max_grad_norm=args.max_grad_norm,
            reward_scale=args.reward_scale,
            supervised_warmup_epochs=args.supervised_warmup_epochs,
            device=args.device,
            torch_seed=args.torch_seed,
            resume_checkpoint=args.resume,
        )
    except (RuntimeError, ValueError) as error:
        raise SystemExit(str(error)) from error

    if args.checkpoint is not None:
        save_ppo_sandbox_checkpoint(result, args.checkpoint)
        print(f"checkpoint_path: {args.checkpoint}")
    if args.report is not None:
        write_json_report(args.report, result.report)

    if args.json:
        print(json.dumps(result.report, indent=2, sort_keys=True))
    else:
        print(format_ppo_sandbox_report(result.report))
    if args.report is not None:
        print(f"report_path: {args.report}")
    return 0


def _train_population_sandbox(args: argparse.Namespace) -> int:
    try:
        from kenjaku.training.population import (
            format_population_sandbox_report,
            train_population_sandbox,
        )
    except ImportError as error:
        raise SystemExit("population sandbox trainer dependencies are unavailable") from error

    try:
        report = train_population_sandbox(
            pool_size=args.pool_size,
            generations=args.generations,
            candidates_per_generation=args.candidates_per_generation,
            matchups_per_candidate=args.matchups_per_candidate,
            total_steps=args.total_steps,
            max_rounds=args.max_rounds,
            max_turns_per_round=args.max_turns_per_round,
            seed=args.seed,
            ruleset=args.ruleset,
            ppo_epochs=args.ppo_epochs,
            batch_size=args.batch_size,
            learning_rate=args.learning_rate,
            hidden_dim=args.hidden_dim,
            evaluation_games=args.evaluation_games,
            evaluation_max_rounds=args.evaluation_max_rounds,
            evaluation_max_turns_per_round=args.evaluation_max_turns_per_round,
            promotion_margin=args.promotion_margin,
            output_dir=args.output_dir,
        )
    except (RuntimeError, ValueError) as error:
        raise SystemExit(str(error)) from error

    if args.report is not None:
        write_json_report(args.report, report)

    if args.json:
        print(json.dumps(report, indent=2, sort_keys=True))
    else:
        print(format_population_sandbox_report(report))
    if args.output_dir is not None:
        print(f"output_dir: {args.output_dir}")
    if args.report is not None:
        print(f"report_path: {args.report}")
    return 0


def _training_dashboard(args: argparse.Namespace) -> int:
    generated_at = datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")
    try:
        dashboard = build_training_dashboard(
            args.metrics_jsonl,
            version=__version__,
            generated_at=generated_at,
            title=args.title,
        )
    except ValueError as error:
        raise SystemExit(str(error)) from error
    output = args.output
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(
        format_training_dashboard_html(
            dashboard,
            link_base_dir=output.parent.resolve(),
        ),
        encoding="utf-8",
    )
    print(f"wrote training dashboard: {output}")
    return 0


def _inspect_tenhou(args: argparse.Namespace) -> int:
    dataset = _parse_tenhou_dataset_from_args(args)
    game = dataset.game
    discards = sum(len(round_.discards) for round_ in game.rounds)
    discard_examples = list(iter_discard_examples(game))
    call_examples = sum(1 for _ in iter_call_examples(game))

    print(f"rounds: {len(game.rounds)}")
    print(f"discards: {discards}")
    print(f"discard_examples: {len(discard_examples)}")
    print(f"call_examples: {call_examples}")
    if dataset.failures:
        print(f"parse_failures: {len(dataset.failures)}")
    if args.report is not None:
        report = build_tenhou_inspect_report(
            input_paths=args.paths,
            xml_files=dataset.files,
            game=game,
            discard_examples=len(discard_examples),
            call_examples=call_examples,
            discard_shanten=summarize_discard_shanten(discard_examples),
            parse_failures=dataset.failures,
            source=_source_metadata(args),
        )
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


def _tenhou_to_mjai(args: argparse.Namespace) -> int:
    try:
        output_paths = write_tenhou_mjai_files(args.paths, args.output)
    except (FileNotFoundError, ValueError) as error:
        raise SystemExit(str(error)) from error
    print(f"files: {len(output_paths)}")
    print(f"output_dir: {args.output}")
    return 0


def _defense_risk_summary(args: argparse.Namespace) -> int:
    dataset = _parse_tenhou_dataset_from_args(args)
    examples = list(iter_discard_examples(dataset.game))
    summary = summarize_defense_risks(examples)
    outcomes = tuple(round_outcome(round_) for round_ in dataset.game.rounds)
    report = {
        **summary,
        "outcome_analysis": summarize_defense_risk_outcomes(examples, outcomes),
        "source": _source_metadata(args),
        "input_paths": [str(path) for path in args.paths],
        "xml_file_count": len(dataset.files),
        **_call_example_cache_game_counts(dataset.game),
        "discard_examples": len(examples),
        "parse_failures": _parse_failures_payload(dataset.failures),
    }
    if args.report is not None:
        write_json_report(args.report, report)

    if args.json:
        print(json.dumps(report, indent=2, sort_keys=True))
    else:
        print(f"examples: {report['examples']}")
        print(f"active_riichi_examples: {report['active_riichi_examples']}")
        actual_risk = report["actual_discard_risk"]
        highest_risk = report["highest_candidate_risk"]
        assert isinstance(actual_risk, dict)
        assert isinstance(highest_risk, dict)
        print(f"actual_mean_risk: {_format_optional_float(actual_risk.get('mean'))}")
        print(f"highest_candidate_mean_risk: {_format_optional_float(highest_risk.get('mean'))}")
        outcome_analysis = report["outcome_analysis"]
        assert isinstance(outcome_analysis, dict)
        outcome_buckets = outcome_analysis["buckets"]
        assert isinstance(outcome_buckets, dict)
        eventual_deal_in = outcome_buckets["eventual_deal_in"]
        active_deal_in = outcome_buckets["active_riichi_eventual_deal_in"]
        assert isinstance(eventual_deal_in, dict)
        assert isinstance(active_deal_in, dict)
        print(f"outcome_labeled_examples: {outcome_analysis['labeled_examples']}")
        print(f"eventual_deal_in_examples: {eventual_deal_in['examples']}")
        print(f"eventual_deal_in_mean_risk: {_format_optional_float(eventual_deal_in.get('mean'))}")
        print(f"active_riichi_deal_in_examples: {active_deal_in['examples']}")
        bands = report["actual_discard_risk_bands"]
        assert isinstance(bands, dict)
        print(
            "actual_risk_bands: "
            + " ".join(
                f"{name}={int(bucket.get('examples', 0))}"
                for name, bucket in bands.items()
                if isinstance(bucket, dict)
            )
        )
        if dataset.failures:
            print(f"parse_failures: {len(dataset.failures)}")
        if args.report is not None:
            print(f"report_path: {args.report}")
    return 0


def _export_bc_examples(args: argparse.Namespace) -> int:
    if args.shard_size < 1:
        raise SystemExit("--shard-size must be at least 1")
    if args.limit_per_type is not None and args.limit_per_type < 0:
        raise SystemExit("--limit-per-type must be non-negative")
    try:
        decision_types = parse_bc_decision_types(args.actions)
    except ValueError as error:
        raise SystemExit(str(error)) from error

    output_dir = args.output_dir
    if output_dir.exists() and any(output_dir.iterdir()) and not args.overwrite:
        raise SystemExit("--output-dir must be empty or use --overwrite")
    output_dir.mkdir(parents=True, exist_ok=True)
    if args.overwrite:
        for pattern in ("manifest.json", "discard-*.jsonl", "call-*.jsonl", "riichi-*.jsonl"):
            for path in output_dir.glob(pattern):
                if path.is_file():
                    path.unlink()

    source_files = tenhou_xml_files(args.paths)
    parsed_files: list[Path] = []
    parse_failures: list[TenhouParseFailure] = []
    game_counts = _empty_game_counts()
    decision_counts = {decision_type: 0 for decision_type in BC_DECISION_TYPES}
    shard_counts = {decision_type: 0 for decision_type in BC_DECISION_TYPES}
    shard_indexes = {decision_type: 0 for decision_type in BC_DECISION_TYPES}
    shard_handles: dict[str, Any] = {}
    shard_paths: dict[str, Path] = {}
    shards: list[BcExampleShard] = []
    source_complete = True

    def close_shard(decision_type: str) -> None:
        handle = shard_handles.pop(decision_type, None)
        if handle is None:
            return
        handle.close()
        count = shard_counts[decision_type]
        if count:
            shards.append(
                BcExampleShard(
                    path=shard_paths[decision_type],
                    decision_type=decision_type,  # type: ignore[arg-type]  # key is constrained by shard setup
                    examples=count,
                ),
            )
        shard_counts[decision_type] = 0

    def write_example(
        decision_type: str,
        example: DiscardExample | CallExample | RiichiExample,
        *,
        source_file: Path,
        source_file_index: int,
    ) -> None:
        limit = args.limit_per_type
        if limit is not None and decision_counts[decision_type] >= limit:
            return
        if decision_type not in shard_handles or shard_counts[decision_type] >= args.shard_size:
            close_shard(decision_type)
            shard_path = output_dir / f"{decision_type}-{shard_indexes[decision_type]:05d}.jsonl"
            shard_indexes[decision_type] += 1
            shard_paths[decision_type] = shard_path
            shard_handles[decision_type] = shard_path.open("w", encoding="utf-8")
        write_bc_example_row(
            shard_handles[decision_type],
            decision_type=decision_type,  # type: ignore[arg-type]  # key is constrained by shard setup
            source_file=source_file,
            source_file_index=source_file_index,
            sequence_index=decision_counts[decision_type],
            example=example,
        )
        decision_counts[decision_type] += 1
        shard_counts[decision_type] += 1

    parsed_iter = iter_tenhou_xml_dataset_files(
        source_files,
        skip_errors=args.skip_errors,
        failures=parse_failures,
        parse_cache_dir=_parse_cache_dir(args),
        jobs=_parse_jobs(args),
    )
    try:
        while True:
            if _bc_export_limits_reached(decision_types, decision_counts, args.limit_per_type):
                source_complete = False
                break
            try:
                parsed = next(parsed_iter)
            except StopIteration:
                break

            file = parsed.path
            file_index = parsed.file_index
            game = parsed.game
            parsed_files.append(file)
            _add_game_counts(game_counts, _call_example_cache_game_counts(game))
            if "discard" in decision_types:
                for example in iter_discard_examples(game):
                    write_example(
                        "discard",
                        example,
                        source_file=file,
                        source_file_index=file_index,
                    )
            if "call" in decision_types:
                for example in iter_call_examples(game):
                    write_example("call", example, source_file=file, source_file_index=file_index)
            if "riichi" in decision_types:
                for example in iter_riichi_examples(game):
                    write_example("riichi", example, source_file=file, source_file_index=file_index)
    finally:
        for decision_type in tuple(shard_handles):
            close_shard(decision_type)

    if _bc_export_limits_reached(decision_types, decision_counts, args.limit_per_type):
        source_complete = False

    manifest = build_bc_manifest(
        input_paths=args.paths,
        xml_files=source_files,
        parsed_files=parsed_files,
        parse_failures=parse_failures,
        game_counts=game_counts,
        decision_counts=decision_counts,
        shards=shards,
        source=_source_metadata(args),
        output_dir=output_dir,
        shard_size=args.shard_size,
        source_complete=source_complete,
    )
    manifest_path = output_dir / "manifest.json"
    write_bc_manifest(manifest_path, manifest)

    print(f"manifest_path: {manifest_path}")
    print(f"xml_files: {len(source_files)}")
    print(f"parsed_xml_files: {len(parsed_files)}")
    for decision_type in BC_DECISION_TYPES:
        print(f"{decision_type}_examples: {decision_counts[decision_type]}")
    if parse_failures:
        print(f"parse_failures: {len(parse_failures)}")
    return 0


def _bc_export_limits_reached(
    decision_types: Sequence[str],
    decision_counts: dict[str, int],
    limit: int | None,
) -> bool:
    if limit is None:
        return False
    return all(decision_counts[decision_type] >= limit for decision_type in decision_types)


def _benchmark_discard_from_examples(args: argparse.Namespace) -> int:
    if args.example_limit is not None and args.example_limit < 0:
        raise SystemExit("--example-limit must be non-negative")
    selected_model_names = _parse_discard_benchmark_models(args.models)
    load = _read_bc_example_load(args.paths, decision_type="discard", limit=args.example_limit)
    examples = load.examples
    if not examples:
        raise SystemExit("no discard examples found")

    train_examples, eval_examples = deterministic_split(
        examples,
        eval_fraction=args.eval_fraction,
        seed=args.split_seed,
    )
    model_payloads: dict[str, dict[str, Any]] = {}
    if "frequency" in selected_model_names:
        frequency_model = DiscardFrequencyBaseline.fit(train_examples)
        model_payloads["frequency"] = _discard_frequency_payload(
            frequency_model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            include_analysis=args.report is not None,
        )
    for model_name in selected_model_names:
        if model_name not in DISCARD_LINEAR_FEATURE_PROFILES:
            continue
        model = DiscardLinearModel.fit(
            train_examples,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
            feature_profile=DISCARD_LINEAR_FEATURE_PROFILES[model_name],
        )
        model_payloads[model_name] = _discard_linear_payload(
            model_name,
            model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
            include_analysis=args.report is not None,
        )

    print(f"examples: {len(examples)}")
    if len(examples) != load.total_examples:
        print(f"source_examples: {load.total_examples}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    _print_discard_benchmark_metrics(model_payloads)
    if args.report is not None:
        report = build_discard_benchmark_report_from_models(
            input_paths=args.paths,
            xml_files=load.source_files,
            game=None,
            game_counts=_bc_game_counts(load),
            discard_examples=len(examples),
            call_examples=_bc_decision_count(load, "call"),
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            models=model_payloads,
            discard_shanten=summarize_discard_shanten(examples),
            parse_failures=load.parse_failures,
            source=_bc_source_metadata(args, load),
        )
        report["discard_examples_total"] = load.total_examples
        report["example_limit"] = args.example_limit
        _apply_bc_example_report_metadata(report, load)
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


def _safety_advisor(args: argparse.Namespace) -> int:
    try:
        report = build_safety_advisor_report(
            hand=args.hand,
            river=args.river,
            active_riichi=args.active_riichi,
            seat=args.seat,
        )
    except ValueError as error:
        raise SystemExit(str(error)) from error
    if args.output == "json":
        print(json.dumps(report, indent=2, sort_keys=True))
    else:
        print(format_safety_advisor_text(report))
    return 0


def _benchmark_deal_in(args: argparse.Namespace) -> int:
    if args.epochs < 0:
        raise SystemExit("--epochs must be non-negative")
    if args.l2 < 0:
        raise SystemExit("--l2 must be non-negative")
    if not 0 <= args.eval_fraction < 1:
        raise SystemExit("--eval-fraction must be in the range [0, 1)")
    threshold = _validated_probability(args.threshold, "--threshold")
    positive_class_weight = _validated_positive_float(
        args.positive_class_weight,
        "--positive-class-weight",
    )

    dataset = _parse_tenhou_dataset_from_args(args)
    examples = tuple(
        iter_deal_in_examples(
            dataset.game,
            active_riichi_only=args.active_riichi_only,
        )
    )
    if not examples:
        raise SystemExit("no deal-in examples found")

    train_examples, eval_examples = _deal_in_train_eval_split(
        examples,
        eval_fraction=args.eval_fraction,
        seed=args.split_seed,
    )
    model = DealInLinearModel.fit(
        train_examples,
        epochs=args.epochs,
        learning_rate=args.learning_rate,
        l2=args.l2,
        positive_class_weight=positive_class_weight,
    )
    train_metrics = model.evaluate(train_examples, threshold=threshold)
    eval_metrics = model.evaluate(eval_examples, threshold=threshold)
    heuristic_train_metrics = evaluate_deal_in_probabilities(
        train_examples,
        heuristic_deal_in_probabilities(train_examples),
        threshold=threshold,
    )
    heuristic_eval_metrics = evaluate_deal_in_probabilities(
        eval_examples,
        heuristic_deal_in_probabilities(eval_examples),
        threshold=threshold,
    )
    calibration = {
        "target": "deal_in",
        "thresholds": list(CALIBRATION_THRESHOLDS),
        "train": _deal_in_threshold_sweep(model, train_examples),
        "eval": _deal_in_threshold_sweep(model, eval_examples),
    }
    report = {
        "kind": DEAL_IN_BENCHMARK_REPORT_KIND,
        "source": _source_metadata(args),
        "input_paths": [str(path) for path in args.paths],
        "xml_file_count": len(dataset.files),
        **_call_example_cache_game_counts(dataset.game),
        "deal_in_examples": len(examples),
        "label_summary": summarize_deal_in_examples(examples),
        "filters": {
            "active_riichi_only": args.active_riichi_only,
            "label_source": "terminal_ron_discard",
        },
        "split": {
            "strategy": "label-stratified",
            "seed": args.split_seed,
            "eval_fraction": args.eval_fraction,
            "train_examples": len(train_examples),
            "eval_examples": len(eval_examples),
        },
        "model": {
            "kind": DEAL_IN_LINEAR_MODEL_KIND,
            "feature_dim": model.feature_dim,
            "feature_names": list(model.feature_names),
        },
        "training": {
            "epochs": args.epochs,
            "learning_rate": args.learning_rate,
            "l2": args.l2,
            "positive_class_weight": positive_class_weight,
            "threshold": threshold,
        },
        "calibration": calibration,
        "metrics": {
            "train": train_metrics,
            "eval": eval_metrics,
        },
        "heuristic_risk_baseline": {
            "calibrated_probability": False,
            "train": heuristic_train_metrics,
            "eval": heuristic_eval_metrics,
        },
        "model_diagnostics": {
            "weights": model.weight_summary(),
            "features": model.feature_summary(examples),
        },
        "parse_failures": _parse_failures_payload(dataset.failures),
    }
    if args.report is not None:
        write_json_report(args.report, report)

    if args.json:
        print(json.dumps(report, indent=2, sort_keys=True))
    else:
        label_summary = report["label_summary"]
        split = report["split"]
        metrics = report["metrics"]
        heuristic = report["heuristic_risk_baseline"]
        assert isinstance(label_summary, dict)
        assert isinstance(split, dict)
        assert isinstance(metrics, dict)
        assert isinstance(heuristic, dict)
        eval_report = metrics["eval"]
        heuristic_eval = heuristic["eval"]
        assert isinstance(eval_report, dict)
        assert isinstance(heuristic_eval, dict)
        print(f"examples: {report['deal_in_examples']}")
        print(f"direct_deal_in_examples: {label_summary['direct_deal_in_examples']}")
        print(f"positive_rate: {_format_optional_float(label_summary.get('positive_rate'))}")
        print(f"active_riichi_examples: {label_summary['active_riichi_examples']}")
        print(f"train_examples: {split['train_examples']}")
        print(f"eval_examples: {split['eval_examples']}")
        print(f"model: {DEAL_IN_LINEAR_MODEL_KIND}")
        print(f"eval_accuracy: {_format_optional_float(eval_report.get('accuracy'))}")
        print(
            "eval_balanced_accuracy: "
            f"{_format_optional_float(eval_report.get('balanced_accuracy'))}"
        )
        print(f"eval_brier_score: {_format_optional_float(eval_report.get('brier_score'))}")
        print(
            "heuristic_eval_brier_score: "
            f"{_format_optional_float(heuristic_eval.get('brier_score'))}"
        )
        eval_best_threshold = _format_calibration_best(
            _calibration_best(calibration, "eval"),
            target_name="deal_in",
        )
        print(f"eval_best_threshold: {eval_best_threshold}")
        if dataset.failures:
            print(f"parse_failures: {len(dataset.failures)}")
        if args.report is not None:
            print(f"report_path: {args.report}")
    return 0


def _train_placement(args: argparse.Namespace) -> int:
    if args.epochs < 0:
        raise SystemExit("--epochs must be non-negative")
    if args.learning_rate <= 0:
        raise SystemExit("--learning-rate must be positive")
    if args.l2 < 0:
        raise SystemExit("--l2 must be non-negative")
    try:
        examples = placement_examples_from_paths(
            args.data,
            parse_cache_dir=_parse_cache_dir(args),
            jobs=_parse_jobs(args),
        )
    except (FileNotFoundError, ValueError) as error:
        raise SystemExit(str(error)) from error
    if not examples:
        raise SystemExit("no four-player placement examples found")
    model = PlacementModel.fit(
        examples,
        epochs=args.epochs,
        learning_rate=args.learning_rate,
        l2=args.l2,
    )
    metadata = build_placement_checkpoint_metadata(
        examples=examples,
        model=model,
        input_paths=args.data,
    )
    model.save(args.output, metadata=metadata)
    if args.json:
        payload = {
            "kind": "kenjaku-placement-training-report-v0",
            "checkpoint_path": str(args.output),
            "metadata": metadata,
            "model": model.to_dict(),
            "disclaimer": PLACEMENT_DISCLAIMER,
        }
        print(json.dumps(payload, indent=2, sort_keys=True))
    else:
        print(
            format_placement_training_report(
                checkpoint_path=args.output,
                metadata=metadata,
            )
        )
    return 0


def _placement_probability(args: argparse.Namespace) -> int:
    try:
        scores = parse_scores(args.scores)
        round_wind, kyoku = parse_kyoku(args.kyoku)
        model = (
            PlacementModel.load(args.model) if args.model is not None else PlacementModel.default()
        )
        payload = placement_probability_payload(
            model,
            scores=scores,
            round_wind=round_wind,
            kyoku=kyoku,
            honba=args.honba,
            kyotaku=args.kyotaku,
            dealer=args.dealer,
            seat=args.seat,
            model_path=args.model,
        )
    except (FileNotFoundError, ValueError) as error:
        raise SystemExit(str(error)) from error
    if args.output == "json":
        print(json.dumps(payload, indent=2, sort_keys=True))
    else:
        print(format_placement_probability_text(payload))
    return 0


def _analyze_hand(args: argparse.Namespace) -> int:
    try:
        payload = build_hand_analysis(
            hand=args.hand,
            drawn=args.drawn,
            seat=args.seat,
            round_wind=args.round_wind,
            dora=args.dora,
            model_path=args.model,
        )
    except (FileNotFoundError, ValueError) as error:
        raise SystemExit(str(error)) from error

    if args.output == "json":
        print(json.dumps(payload, indent=2, sort_keys=True))
    elif args.output == "html":
        print(format_hand_analysis_html(payload))
    else:
        print(format_hand_analysis_text(payload))
    return 0


def _deal_in_threshold_sweep(
    model: DealInLinearModel,
    examples: Sequence[Any],
) -> dict[str, Any]:
    records = [
        {
            "score": model.predict_probability(example),
            "actual_positive": example.dealt_in,
        }
        for example in examples
    ]
    return _binary_threshold_sweep(records, target_name="deal_in")


def _deal_in_train_eval_split(
    examples: Sequence[Any],
    *,
    eval_fraction: float,
    seed: str,
) -> tuple[list[Any], list[Any]]:
    indexed = list(enumerate(examples))
    positives = [(index, example) for index, example in indexed if example.dealt_in]
    negatives = [(index, example) for index, example in indexed if not example.dealt_in]
    positive_train, positive_eval = deterministic_split(
        positives,
        eval_fraction=eval_fraction,
        seed=f"{seed}:positive",
    )
    negative_train, negative_eval = deterministic_split(
        negatives,
        eval_fraction=eval_fraction,
        seed=f"{seed}:negative",
    )
    train = sorted((*positive_train, *negative_train), key=lambda item: item[0])
    evaluation = sorted((*positive_eval, *negative_eval), key=lambda item: item[0])
    return [example for _index, example in train], [example for _index, example in evaluation]


def _export_decision_snapshots(args: argparse.Namespace) -> int:
    if args.limit is not None and args.limit < 0:
        raise SystemExit("--limit must be non-negative")
    decision_types = _parse_decision_snapshot_types(args.decision_types)
    dataset = _parse_tenhou_dataset_from_args(args)
    snapshots = build_decision_snapshots(
        dataset.game,
        decision_types=decision_types,
        limit=args.limit,
        source=_source_metadata(args),
        input_paths=args.paths,
        xml_file_count=len(dataset.files),
        include_outcome=args.include_outcome,
    )
    count = write_decision_snapshots_jsonl(args.output, snapshots)
    print(f"snapshots: {count}")
    print(f"decision_types: {','.join(decision_types)}")
    if dataset.failures:
        print(f"parse_failures: {len(dataset.failures)}")
    print(f"output_path: {args.output}")
    return 0


def _parse_decision_snapshot_types(value: str) -> tuple[str, ...]:
    selected: list[str] = []
    for raw_decision_type in value.split(","):
        decision_type = raw_decision_type.strip()
        if not decision_type:
            continue
        if decision_type not in DECISION_SNAPSHOT_TYPES:
            raise SystemExit(f"unsupported decision type: {decision_type}")
        if decision_type not in selected:
            selected.append(decision_type)
    if not selected:
        raise SystemExit("--decision-types must select at least one type")
    return tuple(selected)


def _decision_snapshot_summary(args: argparse.Namespace) -> int:
    summary = _build_decision_snapshot_summary(args.snapshots)
    if args.json:
        print(json.dumps(summary, indent=2, sort_keys=True))
    else:
        print(_format_decision_snapshot_summary(summary))
    return 0


def _interpretability_overlay(args: argparse.Namespace) -> int:
    if args.limit is not None and args.limit < 0:
        raise SystemExit("--limit must be non-negative")
    if args.min_decisions < 0:
        raise SystemExit("--min-decisions must be non-negative")
    try:
        snapshots, stats = read_interpretability_snapshots(
            args.snapshots,
            limit=args.limit,
        )
        report = build_interpretability_overlay(
            snapshots,
            title=args.title,
            min_decisions=args.min_decisions,
        )
    except ValueError as error:
        raise SystemExit(str(error)) from error
    write_interpretability_overlay_html(args.output, report)
    print(f"decisions: {report['decision_count']}")
    print(f"policy_kind: {report['policy_kind']}")
    print(f"skipped_snapshot_rows: {stats['skipped_rows']}")
    print(f"malformed_snapshot_rows: {stats['malformed_rows']}")
    print(f"output_path: {args.output}")
    return 0


def _transformer_attention_overlay(args: argparse.Namespace) -> int:
    if args.limit is not None and args.limit < 0:
        raise SystemExit("--limit must be non-negative")
    if args.max_heads <= 0:
        raise SystemExit("--max-heads must be positive")
    try:
        from kenjaku.models.torch_discard import require_torch, resolve_torch_device
        from kenjaku.models.torch_transformer import (
            load_discard_transformer_checkpoint,
            transformer_state_payload,
            transformer_state_tensor,
        )

        require_torch()
    except ImportError as error:
        raise SystemExit(
            f"PyTorch is required for transformer-attention-overlay; {TORCH_EXTRA_HINT}"
        ) from error

    try:
        device = resolve_torch_device(args.device)
        model = load_discard_transformer_checkpoint(args.checkpoint, device=device)
        snapshots, stats = read_interpretability_snapshots(args.snapshots, limit=args.limit)
        report = _build_transformer_attention_overlay(
            model,
            snapshots,
            title=args.title,
            max_heads=args.max_heads,
            device=device,
            state_tensor=transformer_state_tensor,
            state_payload=transformer_state_payload,
        )
    except ValueError as error:
        raise SystemExit(str(error)) from error

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(_format_transformer_attention_overlay_html(report), encoding="utf-8")
    print(f"decisions: {report['decision_count']}")
    print(f"layers: {report['layer_count']}")
    print(f"heads_rendered_per_layer: {report['heads_rendered_per_layer']}")
    print(f"skipped_snapshot_rows: {stats['skipped_rows']}")
    print(f"malformed_snapshot_rows: {stats['malformed_rows']}")
    print(f"output_path: {args.output}")
    return 0


def _build_transformer_attention_overlay(
    model: Any,
    snapshots: Sequence[dict[str, Any]],
    *,
    title: str,
    max_heads: int,
    device: Any,
    state_tensor: Callable[[DiscardExample], Any],
    state_payload: Callable[[DiscardExample], dict[str, Any]],
) -> dict[str, Any]:
    decisions: list[dict[str, Any]] = []
    malformed_snapshots = 0
    for index, snapshot in enumerate(snapshots):
        try:
            example = _discard_example_from_snapshot(snapshot)
        except ValueError:
            malformed_snapshots += 1
            continue
        token_payload = state_payload(example)
        tokens = token_payload["tokens"]
        weights = model.attention_weights(state_tensor(example).to(device))
        layer_reports = [
            _attention_layer_report(layer_index, layer_weights, tokens, max_heads=max_heads)
            for layer_index, layer_weights in enumerate(weights)
        ]
        decisions.append(
            {
                "index": index,
                "row_id": snapshot.get("row_id") if isinstance(snapshot.get("row_id"), str) else "",
                "round_index": snapshot.get("round_index"),
                "event_index": snapshot.get("event_index"),
                "seat": snapshot.get("seat"),
                "actual_discard": _actual_discard_tile(snapshot),
                "layers": layer_reports,
            }
        )

    layer_count = len(decisions[0]["layers"]) if decisions else 0
    heads_rendered = (
        len(decisions[0]["layers"][0]["heads"]) if decisions and decisions[0]["layers"] else 0
    )
    return {
        "kind": TRANSFORMER_ATTENTION_OVERLAY_KIND,
        "title": title,
        "policy_kind": model.kind,
        "encoder_kind": model.encoder.kind,
        "decision_count": len(decisions),
        "malformed_snapshots": malformed_snapshots,
        "layer_count": layer_count,
        "heads_rendered_per_layer": heads_rendered,
        "public_artifact": True,
        "disclaimer": (
            "Attention weights are model internals for debugging, not causal explanations "
            "or playing-strength claims."
        ),
        "decisions": decisions,
    }


def _attention_layer_report(
    layer_index: int,
    layer_weights: Any,
    tokens: Sequence[dict[str, Any]],
    *,
    max_heads: int,
) -> dict[str, Any]:
    weights = layer_weights.detach().cpu()
    if weights.ndim == 4:
        weights = weights[0]
    heads = []
    for head_index in range(min(max_heads, int(weights.shape[0]))):
        key_attention = weights[head_index].mean(dim=0)
        values = [float(value) for value in key_attention]
        heads.append(
            {
                "head": head_index,
                "token_attention": [
                    {
                        "index": token_index,
                        "label": _attention_token_label(tokens[token_index]),
                        "value": values[token_index],
                    }
                    for token_index in range(len(values))
                ],
                "top_tokens": _top_attention_tokens(values, tokens, limit=8),
            }
        )
    return {"layer": layer_index, "heads": heads}


def _top_attention_tokens(
    values: Sequence[float],
    tokens: Sequence[dict[str, Any]],
    *,
    limit: int,
) -> list[dict[str, Any]]:
    top_indices = sorted(range(len(values)), key=lambda index: (-values[index], index))[:limit]
    return [
        {
            "index": index,
            "label": _attention_token_label(tokens[index]),
            "value": values[index],
        }
        for index in top_indices
    ]


def _attention_token_label(token: dict[str, Any]) -> str:
    token_type = str(token.get("token_type", "token"))
    tile = token.get("tile")
    seat = token.get("seat")
    if isinstance(tile, str):
        return f"{token_type}:{tile}"
    if isinstance(seat, int):
        return f"{token_type}:seat{seat}"
    return token_type


def _format_transformer_attention_overlay_html(report: dict[str, Any]) -> str:
    title = escape(str(report.get("title", "Kenjaku Transformer Attention Overlay")))
    parts = [
        "<!doctype html>",
        '<html lang="en">',
        "<head>",
        '<meta charset="utf-8">',
        '<meta name="viewport" content="width=device-width, initial-scale=1">',
        '<link rel="icon" href="data:,">',
        f"<title>{title}</title>",
        "<style>",
        _attention_overlay_css(),
        "</style>",
        "</head>",
        "<body>",
        "<main>",
        f"<h1>{title}</h1>",
        '<section class="summary">',
        f"<div><dt>Policy</dt><dd>{escape(str(report.get('policy_kind', 'unknown')))}</dd></div>",
        f"<div><dt>Encoder</dt><dd>{escape(str(report.get('encoder_kind', 'unknown')))}</dd></div>",
        f"<div><dt>Decisions</dt><dd>{int(report.get('decision_count', 0))}</dd></div>",
        f"<div><dt>Layers</dt><dd>{int(report.get('layer_count', 0))}</dd></div>",
        "</section>",
        f'<p class="disclaimer">{escape(str(report.get("disclaimer", "")))}</p>',
    ]
    for decision in report.get("decisions", []):
        if isinstance(decision, dict):
            parts.append(_attention_decision_html(decision))
    parts.extend(["</main>", "</body>", "</html>"])
    return "\n".join(parts)


def _attention_decision_html(decision: dict[str, Any]) -> str:
    heading = (
        f"Decision {int(decision.get('index', 0)) + 1}: "
        f"discard {escape(str(decision.get('actual_discard') or 'unknown'))}"
    )
    parts = [
        '<section class="decision">',
        f"<h2>{heading}</h2>",
        '<p class="meta">'
        f"row={escape(str(decision.get('row_id', '')))} "
        f"round={escape(str(decision.get('round_index', '')))} "
        f"event={escape(str(decision.get('event_index', '')))} "
        f"seat={escape(str(decision.get('seat', '')))}</p>",
    ]
    for layer in decision.get("layers", []):
        if isinstance(layer, dict):
            parts.append(_attention_layer_html(layer))
    parts.append("</section>")
    return "\n".join(parts)


def _attention_layer_html(layer: dict[str, Any]) -> str:
    parts = [f"<h3>Layer {int(layer.get('layer', 0))}</h3>"]
    for head in layer.get("heads", []):
        if isinstance(head, dict):
            parts.append(_attention_head_html(head))
    return "\n".join(parts)


def _attention_head_html(head: dict[str, Any]) -> str:
    cells = []
    values = head.get("token_attention")
    if isinstance(values, list):
        max_value = max(
            (float(item.get("value", 0.0)) for item in values if isinstance(item, dict)),
            default=0.0,
        )
        for item in values:
            if not isinstance(item, dict):
                continue
            value = float(item.get("value", 0.0))
            intensity = 0.0 if max_value <= 0.0 else value / max_value
            cells.append(
                '<span class="cell" '
                f'title="{escape(str(item.get("label", "")))} {value:.4f}" '
                f'style="--a:{intensity:.4f}"></span>'
            )
    top = head.get("top_tokens")
    top_text = ""
    if isinstance(top, list):
        top_text = ", ".join(
            f"{item.get('label')}={float(item.get('value', 0.0)):.4f}"
            for item in top
            if isinstance(item, dict)
        )
    return (
        '<div class="head">'
        f"<h4>Head {int(head.get('head', 0))}</h4>"
        f'<div class="heatmap">{"".join(cells)}</div>'
        f'<p class="top">{escape(top_text)}</p>'
        "</div>"
    )


def _attention_overlay_css() -> str:
    return """
:root {
  color-scheme: light;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
}
body { margin: 0; background: #f7f8fb; color: #15171c; }
main { max-width: 1180px; margin: 0 auto; padding: 24px; }
h1 { font-size: 28px; margin: 0 0 16px; }
h2 { font-size: 18px; margin: 0 0 8px; }
h3 { font-size: 15px; margin: 18px 0 8px; }
h4 { font-size: 13px; margin: 0 0 6px; }
.summary {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 8px;
  margin-bottom: 12px;
}
.summary div, .decision {
  background: #fff;
  border: 1px solid #d9dde7;
  border-radius: 8px;
  padding: 12px;
}
dt { color: #5c6472; font-size: 12px; }
dd { margin: 2px 0 0; font-weight: 700; }
.disclaimer, .meta, .top { color: #5c6472; font-size: 12px; }
.decision { margin-top: 14px; }
.head { margin: 10px 0; }
.heatmap { display: grid; grid-template-columns: repeat(38, minmax(6px, 1fr)); gap: 2px; }
.cell {
  aspect-ratio: 1;
  background: color-mix(in srgb, #2563eb calc(var(--a) * 100%), #edf2ff);
  border-radius: 2px;
}
""".strip()


def _discard_example_from_snapshot(snapshot: dict[str, Any]) -> DiscardExample:
    if snapshot.get("decision_type") != "discard":
        raise ValueError("snapshot is not a discard decision")
    hand_counts = _int_tuple_from_snapshot(snapshot, "hand_counts", length=34)
    visible_counts = _int_tuple_from_snapshot(snapshot, "visible_counts", length=34)
    action_tile = _actual_discard_tile(snapshot)
    if action_tile is None:
        raise ValueError("discard snapshot missing actual discard tile")
    return DiscardExample(
        round_index=int(snapshot.get("round_index", 0)),
        event_index=int(snapshot.get("event_index", 0)),
        seat=int(snapshot.get("seat", 0)),
        dealer=int(snapshot.get("dealer", 0)),
        scores=_int_tuple_from_snapshot(snapshot, "scores"),
        hand_counts=hand_counts,
        visible_counts=visible_counts,
        action=Action.discard(action_tile),
        active_riichi_seats=tuple(bool(value) for value in snapshot.get("active_riichi_seats", [])),
        river_counts_by_seat=_nested_int_tuple_from_snapshot(snapshot, "river_counts_by_seat"),
        dora_indicators=tuple(
            Tile.parse(str(tile))
            for tile in snapshot.get("dora_indicators", [])
            if isinstance(tile, str)
        ),
    )


def _actual_discard_tile(snapshot: dict[str, Any]) -> str | None:
    actual_action = snapshot.get("actual_action")
    if isinstance(actual_action, dict) and isinstance(actual_action.get("tile"), str):
        return str(actual_action["tile"])
    return None


def _int_tuple_from_snapshot(
    snapshot: dict[str, Any],
    key: str,
    *,
    length: int | None = None,
) -> tuple[int, ...]:
    value = snapshot.get(key)
    if not isinstance(value, list):
        raise ValueError(f"snapshot missing {key}")
    result = tuple(int(item) for item in value)
    if length is not None and len(result) != length:
        raise ValueError(f"snapshot {key} must have {length} entries")
    return result


def _nested_int_tuple_from_snapshot(
    snapshot: dict[str, Any],
    key: str,
) -> tuple[tuple[int, ...], ...]:
    value = snapshot.get(key, [])
    if not isinstance(value, list):
        raise ValueError(f"snapshot {key} must be a list")
    return tuple(tuple(int(item) for item in row) for row in value if isinstance(row, list))


def _feature_importance(args: argparse.Namespace) -> int:
    if args.top_k <= 0:
        raise SystemExit("--top-k must be positive")
    try:
        report = json.loads(args.model.read_text(encoding="utf-8"))
        payload = _build_feature_importance_report(
            report,
            source_path=args.model,
            profile=args.profile,
            top_k=args.top_k,
        )
    except (OSError, ValueError) as error:
        raise SystemExit(str(error)) from error
    write_json_report(args.output, payload)
    print(f"model: {payload['model_name']}")
    print(f"profile: {payload['profile']}")
    print(f"ranked_features: {len(payload['rankings'])}")
    print(f"output_path: {args.output}")
    return 0


def _build_feature_importance_report(
    report: Any,
    *,
    source_path: Path,
    profile: str,
    top_k: int,
) -> dict[str, Any]:
    if not isinstance(report, dict):
        raise ValueError("feature importance input must be a JSON object")
    model_name = _feature_importance_model_name(profile)
    model_payload = _feature_importance_model_payload(report, model_name)
    weight_rows = _feature_importance_weight_rows(model_payload)
    feature_rows = _feature_importance_feature_rows(model_payload)
    if len(weight_rows) != len(feature_rows):
        raise ValueError("weight and feature summaries have different feature counts")

    rankings: list[dict[str, Any]] = []
    for weight_row, feature_row in zip(weight_rows, feature_rows, strict=True):
        weight_index = int(weight_row["index"])
        feature_index = int(feature_row["index"])
        weight_name = str(weight_row["name"])
        feature_name = str(feature_row["name"])
        if weight_index != feature_index or weight_name != feature_name:
            raise ValueError("weight and feature summaries are not aligned")
        weight_value, weight_metric = _feature_importance_weight(weight_row)
        mean_value = _required_float(feature_row.get("mean"), "feature mean")
        rankings.append(
            {
                "index": feature_index,
                "name": feature_name,
                "importance": abs(weight_value * mean_value),
                "weight": weight_value,
                "weight_metric": weight_metric,
                "mean_feature_value": mean_value,
                "mean_abs_feature_value": _optional_float(feature_row.get("mean_abs")),
                "nonzero_rate": _optional_float(feature_row.get("nonzero_rate")),
            }
        )

    rankings.sort(key=lambda row: (-float(row["importance"]), int(row["index"])))
    rankings = rankings[:top_k]
    for rank, row in enumerate(rankings, start=1):
        row["rank"] = rank

    return {
        "kind": FEATURE_IMPORTANCE_KIND,
        "source": {
            "path": str(source_path),
            "report_kind": report.get("kind"),
        },
        "profile": profile,
        "model_name": model_name,
        "feature_count": len(feature_rows),
        "top_k": top_k,
        "ranking_formula": "abs(weight * mean_feature_value)",
        "rankings": rankings,
    }


def _feature_importance_model_name(profile: str) -> str:
    normalized = profile.strip().lower().replace("-", "_")
    try:
        return FEATURE_IMPORTANCE_MODEL_ALIASES[normalized]
    except KeyError as error:
        raise ValueError(f"unsupported feature importance profile: {profile}") from error


def _feature_importance_model_payload(report: dict[str, Any], model_name: str) -> dict[str, Any]:
    if model_name == "deal_in":
        if report.get("kind") != DEAL_IN_BENCHMARK_REPORT_KIND:
            raise ValueError("deal_in feature importance requires a deal-in benchmark report")
        diagnostics = report.get("model_diagnostics")
        if not isinstance(diagnostics, dict):
            raise ValueError("deal-in report missing model_diagnostics")
        return {
            "weight_summary": diagnostics.get("weights"),
            "feature_summary": diagnostics.get("features"),
        }

    models = report.get("models")
    if not isinstance(models, dict):
        raise ValueError("feature importance input missing models")
    model_payload = models.get(model_name)
    if not isinstance(model_payload, dict):
        raise ValueError(f"report missing model profile: {model_name}")
    return model_payload


def _feature_importance_weight_rows(model_payload: dict[str, Any]) -> list[dict[str, Any]]:
    summary = model_payload.get("weight_summary")
    if not isinstance(summary, dict):
        raise ValueError("model payload missing weight_summary")
    rows = summary.get("features")
    if not isinstance(rows, list):
        raise ValueError("model payload missing per-feature weights; regenerate the report")
    if not all(isinstance(row, dict) for row in rows):
        raise ValueError("weight_summary features must be objects")
    return rows


def _feature_importance_feature_rows(model_payload: dict[str, Any]) -> list[dict[str, Any]]:
    summary = model_payload.get("feature_summary")
    if not isinstance(summary, dict):
        raise ValueError("model payload missing feature_summary")
    rows = summary.get("features")
    if not isinstance(rows, list):
        raise ValueError("model payload missing feature_summary features")
    if not all(isinstance(row, dict) for row in rows):
        raise ValueError("feature_summary features must be objects")
    return rows


def _feature_importance_weight(row: dict[str, Any]) -> tuple[float, str]:
    if "weight" in row:
        return _required_float(row["weight"], "feature weight"), "weight"
    return _required_float(row.get("mean_abs"), "feature mean_abs weight"), "mean_abs"


def _required_float(value: Any, name: str) -> float:
    if not isinstance(value, int | float):
        raise ValueError(f"{name} must be numeric")
    return float(value)


def _optional_float(value: Any) -> float | None:
    if value is None:
        return None
    if not isinstance(value, int | float):
        raise ValueError("optional metric must be numeric or null")
    return float(value)


def _build_decision_snapshot_summary(paths: Sequence[Path]) -> dict[str, Any]:
    decision_types: Counter[str] = Counter()
    actions_by_decision_type: dict[str, Counter[str]] = {}
    sources: Counter[str] = Counter()
    path_summaries: list[dict[str, Any]] = []
    rows = 0
    snapshots = 0
    malformed_rows = 0
    mjai_present = 0
    mjai_missing = 0

    for path in paths:
        path_rows = 0
        path_snapshots = 0
        path_malformed = 0
        for line in path.read_text(encoding="utf-8").splitlines():
            rows += 1
            path_rows += 1
            try:
                payload = json.loads(line)
            except json.JSONDecodeError:
                malformed_rows += 1
                path_malformed += 1
                continue
            if not _is_decision_snapshot_payload(payload):
                malformed_rows += 1
                path_malformed += 1
                continue

            snapshots += 1
            path_snapshots += 1
            decision_type = payload["decision_type"]
            decision_types[decision_type] += 1
            action = _snapshot_action_kind(payload)
            actions_by_decision_type.setdefault(decision_type, Counter())[action] += 1
            sources[_snapshot_source_label(payload)] += 1
            if _snapshot_has_mjai_events(payload):
                mjai_present += 1
            else:
                mjai_missing += 1

        path_summaries.append(
            {
                "path": str(path),
                "rows": path_rows,
                "snapshots": path_snapshots,
                "malformed_rows": path_malformed,
            }
        )

    return {
        "kind": "kenjaku-decision-snapshot-summary-v0",
        "paths": path_summaries,
        "rows": rows,
        "snapshots": snapshots,
        "malformed_rows": malformed_rows,
        "decision_types": dict(sorted(decision_types.items())),
        "actions_by_decision_type": {
            decision_type: dict(sorted(actions.items()))
            for decision_type, actions in sorted(actions_by_decision_type.items())
        },
        "sources": dict(sorted(sources.items())),
        "mjai_events": {
            "present": mjai_present,
            "missing": mjai_missing,
        },
    }


def _is_decision_snapshot_payload(value: Any) -> bool:
    return (
        isinstance(value, dict)
        and value.get("kind") == DECISION_SNAPSHOT_KIND
        and isinstance(value.get("decision_type"), str)
    )


def _snapshot_action_kind(payload: dict[str, Any]) -> str:
    action = payload.get("actual_action")
    if isinstance(action, dict) and isinstance(action.get("kind"), str):
        return action["kind"]
    return "unknown"


def _snapshot_source_label(payload: dict[str, Any]) -> str:
    source = payload.get("source")
    if isinstance(source, dict) and isinstance(source.get("label"), str):
        return source["label"]
    return "unknown"


def _snapshot_has_mjai_events(payload: dict[str, Any]) -> bool:
    events = payload.get("mjai_events")
    return isinstance(events, list) and bool(events)


def _format_decision_snapshot_summary(summary: dict[str, Any]) -> str:
    lines = [
        f"snapshots: {summary['snapshots']}",
        f"rows: {summary['rows']}",
        f"malformed_rows: {summary['malformed_rows']}",
        "decision_types:",
    ]
    for decision_type, count in summary["decision_types"].items():
        lines.append(f"  {decision_type}: {count}")
    lines.append("actions:")
    for decision_type, actions in summary["actions_by_decision_type"].items():
        parts = [f"{action}={count}" for action, count in actions.items()]
        lines.append(f"  {decision_type}: " + " ".join(parts))
    lines.append("sources:")
    for source, count in summary["sources"].items():
        lines.append(f"  {source}: {count}")
    mjai_events = summary["mjai_events"]
    lines.append(f"mjai_events: present={mjai_events['present']} missing={mjai_events['missing']}")
    return "\n".join(lines)


def _produce_decision_predictions(args: argparse.Namespace) -> int:
    stats = _write_stub_decision_predictions(
        snapshots_path=args.snapshots,
        output_path=args.output,
        strategy=args.strategy,
    )
    print(f"strategy: {args.strategy}")
    print(f"predictions: {stats['predictions']}")
    print(f"malformed_snapshot_rows: {stats['malformed_snapshot_rows']}")
    print(f"output_path: {args.output}")
    return 0


def _predict(args: argparse.Namespace) -> int:
    try:
        stats = write_model_predictions(
            snapshots_path=args.snapshots,
            output_path=args.output,
            model_type=args.model,
            checkpoint_path=args.checkpoint,
            device=args.device,
            batch_size=args.batch_size,
        )
    except (FileNotFoundError, ValueError) as error:
        raise SystemExit(str(error)) from error
    print(f"model: {args.model}")
    print(f"predictions: {stats['predictions']}")
    print(f"malformed_snapshot_rows: {stats['malformed_snapshot_rows']}")
    print(f"unsupported_snapshot_rows: {stats['unsupported_snapshot_rows']}")
    print(f"output_path: {args.output}")
    return 0


def _write_stub_decision_predictions(
    *,
    snapshots_path: Path,
    output_path: Path,
    strategy: str,
) -> dict[str, int]:
    if strategy not in PREDICTION_STUB_STRATEGIES:
        raise ValueError(f"unsupported prediction strategy: {strategy}")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    predictions = 0
    malformed = 0
    with (
        snapshots_path.open(encoding="utf-8") as source,
        output_path.open(
            "w",
            encoding="utf-8",
        ) as target,
    ):
        for line in source:
            try:
                snapshot = json.loads(line)
            except json.JSONDecodeError:
                malformed += 1
                continue
            prediction = _stub_prediction_for_snapshot(snapshot, strategy=strategy)
            if prediction is None:
                malformed += 1
                continue
            target.write(json.dumps(prediction, sort_keys=True) + "\n")
            predictions += 1

    return {
        "predictions": predictions,
        "malformed_snapshot_rows": malformed,
    }


def _stub_prediction_for_snapshot(
    snapshot: Any,
    *,
    strategy: str,
) -> dict[str, Any] | None:
    if not _is_decision_snapshot_payload(snapshot):
        return None
    row_id = snapshot.get("row_id")
    if not isinstance(row_id, str):
        return None

    if strategy == "echo-actual":
        action = snapshot.get("actual_action")
        if not isinstance(action, dict):
            return None
        return {"row_id": row_id, "predicted_action": _normalized_action(action)}

    legal_actions = snapshot.get("legal_actions")
    if not isinstance(legal_actions, list):
        return None
    legal_action_dicts = [action for action in legal_actions if isinstance(action, dict)]
    if not legal_action_dicts:
        return None

    if strategy == "pass":
        selected = next(
            (
                action
                for action in legal_action_dicts
                if action.get("kind") == ActionKind.PASS.value
            ),
            legal_action_dicts[0],
        )
        return {"row_id": row_id, "predicted_action": _normalized_action(selected)}

    if strategy == "first-legal":
        return {
            "row_id": row_id,
            "predicted_action": _normalized_action(legal_action_dicts[0]),
        }

    raise ValueError(f"unsupported prediction strategy: {strategy}")


def _decision_snapshot_compare(args: argparse.Namespace) -> int:
    comparison = _build_decision_snapshot_comparison(args.snapshots, args.predictions)
    if args.json:
        print(json.dumps(comparison, indent=2, sort_keys=True))
    else:
        print(_format_decision_snapshot_comparison(comparison))
    return 0


def _external_baseline_report(args: argparse.Namespace) -> int:
    try:
        specs = [parse_external_baseline_spec(spec) for spec in args.baseline]
        report = build_external_baseline_report(
            args.snapshots,
            specs,
            minimum_comparable_decisions=args.min_decisions,
        )
    except ValueError as error:
        raise SystemExit(str(error)) from error

    if args.report is not None:
        write_json_report(args.report, report)
    if args.json:
        print(json.dumps(report, indent=2, sort_keys=True))
    else:
        print(format_external_baseline_report(report))
        if args.report is not None:
            print(f"report_path: {args.report}")
    return 0


def _run_external_prediction_producer(args: argparse.Namespace) -> int:
    command = _external_producer_command(args.command)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    env = os.environ.copy()
    env["KENJAKU_SNAPSHOTS"] = str(args.snapshots)
    env["KENJAKU_PREDICTIONS"] = str(args.output)
    try:
        completed = subprocess.run(
            command,
            capture_output=True,
            text=True,
            timeout=args.timeout_seconds,
            env=env,
            check=False,
        )
    except subprocess.TimeoutExpired as error:
        raise SystemExit(
            f"external prediction producer timed out after {args.timeout_seconds} seconds"
        ) from error
    if completed.returncode != 0:
        stderr = completed.stderr.strip()
        detail = f": {stderr}" if stderr else ""
        raise SystemExit(
            f"external prediction producer failed with code {completed.returncode}{detail}"
        )

    try:
        _predictions, prediction_stats = _read_decision_predictions(args.output)
    except FileNotFoundError as error:
        raise SystemExit(f"external prediction producer did not write {args.output}") from error

    print(f"command_returncode: {completed.returncode}")
    print(f"predictions: {prediction_stats['valid_predictions']}")
    print(f"malformed_prediction_rows: {prediction_stats['malformed_prediction_rows']}")
    print(f"duplicate_prediction_rows: {prediction_stats['duplicate_prediction_rows']}")
    print(f"output_path: {args.output}")
    if args.compare_report is not None:
        comparison = _build_decision_snapshot_comparison(args.snapshots, args.output)
        write_json_report(args.compare_report, comparison)
        print(f"compare_report_path: {args.compare_report}")
    return 0


def _external_producer_command(raw_command: Sequence[str]) -> list[str]:
    command = list(raw_command)
    if command and command[0] == "--":
        command = command[1:]
    if not command:
        raise SystemExit("--command requires at least one command token")
    return command


def _build_decision_snapshot_comparison(
    snapshots_path: Path,
    predictions_path: Path,
) -> dict[str, Any]:
    predictions, prediction_stats = _read_decision_predictions(predictions_path)
    snapshots = 0
    malformed_snapshot_rows = 0
    missing_predictions = 0
    overall = _empty_comparison_bucket()
    by_decision_type: dict[str, dict[str, int | float | None]] = {}
    binary_records = {
        "call": _empty_binary_comparison_bucket(),
        "riichi": _empty_binary_comparison_bucket(),
    }

    for line in snapshots_path.read_text(encoding="utf-8").splitlines():
        try:
            snapshot = json.loads(line)
        except json.JSONDecodeError:
            malformed_snapshot_rows += 1
            continue
        if not _is_decision_snapshot_payload(snapshot):
            malformed_snapshot_rows += 1
            continue
        row_id = snapshot.get("row_id")
        if not isinstance(row_id, str):
            malformed_snapshot_rows += 1
            continue
        actual_action = snapshot.get("actual_action")
        if not isinstance(actual_action, dict):
            malformed_snapshot_rows += 1
            continue

        snapshots += 1
        decision_type = snapshot["decision_type"]
        by_decision_type.setdefault(decision_type, _empty_comparison_bucket())
        predicted_action = predictions.get(row_id)
        if predicted_action is None:
            missing_predictions += 1

        correct = _actions_match(actual_action, predicted_action)
        _record_comparison_bucket(overall, correct)
        _record_comparison_bucket(by_decision_type[decision_type], correct)
        if decision_type in binary_records:
            _record_binary_comparison(
                binary_records[decision_type],
                target=decision_type,
                actual_action=actual_action,
                predicted_action=predicted_action,
            )

    return {
        "kind": DECISION_SNAPSHOT_COMPARISON_KIND,
        "snapshots_path": str(snapshots_path),
        "predictions_path": str(predictions_path),
        "snapshots": snapshots,
        "predictions": prediction_stats["valid_predictions"],
        "malformed_snapshot_rows": malformed_snapshot_rows,
        "malformed_prediction_rows": prediction_stats["malformed_prediction_rows"],
        "duplicate_prediction_rows": prediction_stats["duplicate_prediction_rows"],
        "missing_predictions": missing_predictions,
        "overall": _finalize_comparison_bucket(overall),
        "by_decision_type": {
            decision_type: _finalize_comparison_bucket(bucket)
            for decision_type, bucket in sorted(by_decision_type.items())
        },
        "binary": {
            decision_type: _finalize_binary_comparison(bucket, target=decision_type)
            for decision_type, bucket in binary_records.items()
            if bucket["examples"]
        },
    }


def _read_decision_predictions(path: Path) -> tuple[dict[str, dict[str, Any]], dict[str, int]]:
    predictions: dict[str, dict[str, Any]] = {}
    malformed = 0
    duplicates = 0
    for line in path.read_text(encoding="utf-8").splitlines():
        try:
            payload = json.loads(line)
        except json.JSONDecodeError:
            malformed += 1
            continue
        if not isinstance(payload, dict):
            malformed += 1
            continue
        row_id = payload.get("row_id")
        action = payload.get("predicted_action")
        if not isinstance(row_id, str) or not isinstance(action, dict):
            malformed += 1
            continue
        if row_id in predictions:
            duplicates += 1
            continue
        predictions[row_id] = action
    return predictions, {
        "valid_predictions": len(predictions),
        "malformed_prediction_rows": malformed,
        "duplicate_prediction_rows": duplicates,
    }


def _actions_match(
    actual_action: dict[str, Any],
    predicted_action: dict[str, Any] | None,
) -> bool:
    return predicted_action is not None and _normalized_action(actual_action) == _normalized_action(
        predicted_action
    )


def _normalized_action(action: dict[str, Any]) -> dict[str, Any]:
    normalized = {"kind": action.get("kind")}
    if "tile" in action:
        normalized["tile"] = action.get("tile")
    if "tsumogiri" in action:
        normalized["tsumogiri"] = bool(action.get("tsumogiri"))
    if "consumed" in action and isinstance(action["consumed"], list):
        normalized["consumed"] = list(action["consumed"])
    return normalized


def _empty_comparison_bucket() -> dict[str, int]:
    return {"examples": 0, "correct": 0}


def _record_comparison_bucket(bucket: dict[str, int | float | None], correct: bool) -> None:
    bucket["examples"] = int(bucket["examples"] or 0) + 1
    bucket["correct"] = int(bucket["correct"] or 0) + int(correct)


def _finalize_comparison_bucket(
    bucket: dict[str, int | float | None],
) -> dict[str, int | float | None]:
    examples = int(bucket["examples"] or 0)
    correct = int(bucket["correct"] or 0)
    return {
        "examples": examples,
        "correct": correct,
        "accuracy": None if examples == 0 else correct / examples,
    }


def _empty_binary_comparison_bucket() -> dict[str, int]:
    return {
        "examples": 0,
        "true_positive": 0,
        "false_positive": 0,
        "true_negative": 0,
        "false_negative": 0,
    }


def _record_binary_comparison(
    bucket: dict[str, int],
    *,
    target: str,
    actual_action: dict[str, Any],
    predicted_action: dict[str, Any] | None,
) -> None:
    actual_positive = _action_is_positive(actual_action, target=target)
    predicted_positive = predicted_action is not None and _action_is_positive(
        predicted_action, target=target
    )
    bucket["examples"] += 1
    if actual_positive and predicted_positive:
        bucket["true_positive"] += 1
    elif actual_positive:
        bucket["false_negative"] += 1
    elif predicted_positive:
        bucket["false_positive"] += 1
    else:
        bucket["true_negative"] += 1


def _action_is_positive(action: dict[str, Any], *, target: str) -> bool:
    kind = action.get("kind")
    if target == "call":
        return kind != ActionKind.PASS.value
    if target == "riichi":
        return kind == ActionKind.RIICHI.value
    return False


def _finalize_binary_comparison(bucket: dict[str, int], *, target: str) -> dict[str, Any]:
    true_positive = bucket["true_positive"]
    false_positive = bucket["false_positive"]
    true_negative = bucket["true_negative"]
    false_negative = bucket["false_negative"]
    target_examples = true_positive + false_negative
    pass_examples = true_negative + false_positive
    return {
        **bucket,
        "accuracy": _safe_ratio(true_positive + true_negative, bucket["examples"]),
        f"{target}_precision": _safe_ratio(true_positive, true_positive + false_positive),
        f"{target}_recall": _safe_ratio(true_positive, target_examples),
        "pass_recall": _safe_ratio(true_negative, pass_examples),
    }


def _format_decision_snapshot_comparison(comparison: dict[str, Any]) -> str:
    lines = [
        f"snapshots: {comparison['snapshots']}",
        f"predictions: {comparison['predictions']}",
        f"missing_predictions: {comparison['missing_predictions']}",
        f"malformed_snapshot_rows: {comparison['malformed_snapshot_rows']}",
        f"malformed_prediction_rows: {comparison['malformed_prediction_rows']}",
        f"duplicate_prediction_rows: {comparison['duplicate_prediction_rows']}",
        f"overall_accuracy: {_format_optional_accuracy(comparison['overall']['accuracy'])}",
        "by_decision_type:",
    ]
    for decision_type, bucket in comparison["by_decision_type"].items():
        lines.append(
            f"  {decision_type}: "
            f"accuracy={_format_optional_accuracy(bucket['accuracy'])} "
            f"examples={bucket['examples']}"
        )
    if comparison["binary"]:
        lines.append("binary:")
        for decision_type, bucket in comparison["binary"].items():
            lines.append(
                f"  {decision_type}: "
                f"accuracy={_format_optional_accuracy(bucket['accuracy'])} "
                f"pass_recall={_format_optional_accuracy(bucket['pass_recall'])} "
                f"{decision_type}_recall="
                f"{_format_optional_accuracy(bucket[f'{decision_type}_recall'])}"
            )
    return "\n".join(lines)


def _train_discard_baseline(args: argparse.Namespace) -> int:
    game = _parse_tenhou_dataset_from_args(args).game
    examples = list(iter_discard_examples(game))
    if not examples:
        raise SystemExit("no discard examples found")

    model = DiscardFrequencyBaseline.fit(examples)
    print(f"examples: {len(examples)}")
    print(f"top_discard: {model.top_tile.notation}")
    print(f"training_accuracy: {model.score(examples):.4f}")
    return 0


def _train_discard_linear(args: argparse.Namespace) -> int:
    dataset = _parse_tenhou_dataset_from_args(args)
    game = dataset.game
    examples = list(iter_discard_examples(game))
    if not examples:
        raise SystemExit("no discard examples found")

    train_examples, eval_examples = deterministic_split(
        examples,
        eval_fraction=args.eval_fraction,
        seed=args.split_seed,
    )
    model = DiscardLinearModel.fit(
        train_examples,
        epochs=args.epochs,
        learning_rate=args.learning_rate,
        l2=args.l2,
    )
    train_accuracy = model.score(train_examples)
    eval_accuracy = model.score(eval_examples) if eval_examples else None

    print(f"examples: {len(examples)}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    print(f"train_accuracy: {train_accuracy:.4f}")
    print(f"eval_accuracy: {_format_optional_accuracy(eval_accuracy)}")
    if args.output is not None:
        model.save(args.output)
        print(f"model_path: {args.output}")
    if args.report is not None:
        report = build_discard_linear_report(
            input_paths=args.paths,
            xml_files=dataset.files,
            game=game,
            discard_examples=len(examples),
            call_examples=sum(1 for _ in iter_call_examples(game)),
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            model_kind=model.kind,
            feature_dim=model.feature_dim,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
            train_accuracy=train_accuracy,
            eval_accuracy=eval_accuracy,
            discard_shanten=summarize_discard_shanten(examples),
            parse_failures=dataset.failures,
            model_path=args.output,
            source=_source_metadata(args),
        )
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


def _train_discard_mlp(args: argparse.Namespace) -> int:
    try:
        from kenjaku.models.torch_discard import (
            require_torch,
            save_discard_mlp_checkpoint,
            train_discard_mlp,
        )

        require_torch()
    except ImportError as error:
        raise SystemExit(
            f"PyTorch is required for train-discard-mlp; {TORCH_EXTRA_HINT}"
        ) from error

    dataset = _parse_tenhou_dataset_from_args(args)
    game = dataset.game
    examples = list(iter_discard_examples(game))
    if not examples:
        raise SystemExit("no discard examples found")

    train_examples, eval_examples = deterministic_split(
        examples,
        eval_fraction=args.eval_fraction,
        seed=args.split_seed,
    )
    try:
        result = train_discard_mlp(
            train_examples,
            eval_examples,
            epochs=args.epochs,
            batch_size=args.batch_size,
            learning_rate=args.learning_rate,
            hidden_dim=args.hidden_dim,
            device=args.device,
            seed=args.seed,
        )
    except ValueError as error:
        raise SystemExit(str(error)) from error

    print(f"examples: {len(examples)}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    print(f"device: {result.device}")
    print(f"train_accuracy: {_format_optional_accuracy(result.train_metrics['accuracy'])}")
    print(f"eval_accuracy: {_format_optional_accuracy(result.eval_metrics['accuracy'])}")
    if dataset.failures:
        print(f"parse_failures: {len(dataset.failures)}")
    if args.checkpoint is not None:
        save_discard_mlp_checkpoint(
            result,
            args.checkpoint,
            epochs=args.epochs,
            batch_size=args.batch_size,
            learning_rate=args.learning_rate,
            eval_fraction=args.eval_fraction,
            split_seed=args.split_seed,
            seed=args.seed,
        )
        print(f"checkpoint_path: {args.checkpoint}")
    if args.report is not None:
        report = build_discard_mlp_report(
            input_paths=args.paths,
            xml_files=dataset.files,
            game=game,
            discard_examples=len(examples),
            call_examples=sum(1 for _ in iter_call_examples(game)),
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            model_kind=result.model.kind,
            input_dim=result.model.input_dim,
            hidden_dim=result.model.hidden_dim,
            output_dim=result.model.output_dim,
            epochs=args.epochs,
            batch_size=args.batch_size,
            learning_rate=args.learning_rate,
            device=result.device,
            seed=args.seed,
            train_metrics=result.train_metrics,
            eval_metrics=result.eval_metrics,
            history=result.history,
            best_epoch=result.best_epoch,
            selection_split=result.selection_split,
            best_metrics=result.best_metrics,
            discard_shanten=summarize_discard_shanten(examples),
            parse_failures=dataset.failures,
            checkpoint_path=args.checkpoint,
            source=_source_metadata(args),
        )
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


def _train_discard_transformer(args: argparse.Namespace) -> int:
    try:
        from kenjaku.models.torch_discard import require_torch
        from kenjaku.models.torch_transformer import (
            MahjongTransformerConfig,
            save_discard_transformer_checkpoint,
            train_discard_transformer,
            transformer_config_payload,
        )

        require_torch()
    except ImportError as error:
        raise SystemExit(
            f"PyTorch is required for train-discard-transformer; {TORCH_EXTRA_HINT}"
        ) from error

    dataset = _parse_tenhou_dataset_from_args(args)
    game = dataset.game
    examples = list(iter_discard_examples(game))
    if not examples:
        raise SystemExit("no discard examples found")

    train_examples, eval_examples = deterministic_split(
        examples,
        eval_fraction=args.eval_fraction,
        seed=args.split_seed,
    )
    config = MahjongTransformerConfig(
        model_dim=args.model_dim,
        num_heads=args.num_heads,
        num_layers=args.num_layers,
        feedforward_dim=args.feedforward_dim,
        dropout=args.dropout,
    )
    try:
        result = train_discard_transformer(
            train_examples,
            eval_examples,
            config=config,
            epochs=args.epochs,
            batch_size=args.batch_size,
            learning_rate=args.learning_rate,
            device=args.device,
            seed=args.seed,
            value_head=args.value_head,
        )
    except ValueError as error:
        raise SystemExit(str(error)) from error

    print(f"examples: {len(examples)}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    print(f"device: {result.device}")
    print(f"model: {result.model.kind}")
    print(f"encoder: {result.model.encoder.kind}")
    print(f"value_head: {'yes' if result.model.has_value_head else 'no'}")
    print(f"input_tokens: {result.model.input_tokens}")
    print(f"train_accuracy: {_format_optional_accuracy(result.train_metrics['accuracy'])}")
    print(f"eval_accuracy: {_format_optional_accuracy(result.eval_metrics['accuracy'])}")
    if dataset.failures:
        print(f"parse_failures: {len(dataset.failures)}")
    if args.checkpoint is not None:
        save_discard_transformer_checkpoint(
            result,
            args.checkpoint,
            epochs=args.epochs,
            batch_size=args.batch_size,
            learning_rate=args.learning_rate,
            eval_fraction=args.eval_fraction,
            split_seed=args.split_seed,
            seed=args.seed,
        )
        print(f"checkpoint_path: {args.checkpoint}")
    if args.report is not None:
        report = build_discard_transformer_report(
            input_paths=args.paths,
            xml_files=dataset.files,
            game=game,
            discard_examples=len(examples),
            call_examples=sum(1 for _ in iter_call_examples(game)),
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            model_kind=result.model.kind,
            encoder_kind=result.model.encoder.kind,
            input_tokens=result.model.input_tokens,
            output_dim=result.model.output_dim,
            value_head=result.model.has_value_head,
            model_config=transformer_config_payload(result.model.encoder.config),
            epochs=args.epochs,
            batch_size=args.batch_size,
            learning_rate=args.learning_rate,
            device=result.device,
            seed=args.seed,
            train_metrics=result.train_metrics,
            eval_metrics=result.eval_metrics,
            history=result.history,
            best_epoch=result.best_epoch,
            selection_split=result.selection_split,
            best_metrics=result.best_metrics,
            discard_shanten=summarize_discard_shanten(examples),
            parse_failures=dataset.failures,
            checkpoint_path=args.checkpoint,
            source=_source_metadata(args),
        )
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


def _benchmark_discard(args: argparse.Namespace) -> int:
    if args.max_disagreements < 0:
        raise SystemExit("--max-disagreements must be non-negative")
    if args.example_limit is not None and args.example_limit < 0:
        raise SystemExit("--example-limit must be non-negative")
    if args.stream_examples and args.example_limit is None:
        raise SystemExit("--stream-examples requires --example-limit")
    selected_model_names = _parse_discard_benchmark_models(args.models)
    if args.disagreements is not None:
        missing = [
            model_name
            for model_name in DISAGREEMENT_REQUIRED_MODELS
            if model_name not in selected_model_names
        ]
        if missing:
            raise SystemExit("--disagreements requires selected models: " + ", ".join(missing))
    streamed_examples: _StreamedExamples | None = None
    game: TenhouGame | None
    if args.stream_examples:
        streamed_examples = _collect_streamed_examples(
            args.paths,
            example_iter=iter_discard_examples,
            limit=args.example_limit,
            skip_errors=args.skip_errors,
            parse_cache_dir=_parse_cache_dir(args),
            jobs=_parse_jobs(args),
            count_call=True,
        )
        game = None
        dataset_files = streamed_examples.source_files
        parse_failures = streamed_examples.parse_failures
        game_counts = streamed_examples.game_counts
        examples = streamed_examples.examples
        total_examples = len(examples)
        call_examples = int(streamed_examples.call_examples or 0)
    else:
        dataset = _parse_tenhou_dataset_from_args(args)
        game = dataset.game
        dataset_files = dataset.files
        parse_failures = dataset.failures
        game_counts = None
        examples, total_examples = _collect_limited_examples(
            iter_discard_examples(game),
            args.example_limit,
        )
        call_examples = sum(1 for _ in iter_call_examples(game))
    if not examples:
        raise SystemExit("no discard examples found")

    train_examples, eval_examples = deterministic_split(
        examples,
        eval_fraction=args.eval_fraction,
        seed=args.split_seed,
    )
    model_payloads: dict[str, dict[str, Any]] = {}
    linear_models: dict[str, DiscardLinearModel] = {}

    if "frequency" in selected_model_names:
        frequency_model = DiscardFrequencyBaseline.fit(train_examples)
        model_payloads["frequency"] = _discard_frequency_payload(
            frequency_model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            include_analysis=args.report is not None,
        )

    for model_name in selected_model_names:
        if model_name not in DISCARD_LINEAR_FEATURE_PROFILES:
            continue
        model = DiscardLinearModel.fit(
            train_examples,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
            feature_profile=DISCARD_LINEAR_FEATURE_PROFILES[model_name],
        )
        linear_models[model_name] = model
        model_payloads[model_name] = _discard_linear_payload(
            model_name,
            model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
            include_analysis=args.report is not None,
        )

    print(f"examples: {len(examples)}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    _print_discard_benchmark_metrics(model_payloads)
    if parse_failures:
        print(f"parse_failures: {len(parse_failures)}")
    if args.report is not None:
        report = build_discard_benchmark_report_from_models(
            input_paths=args.paths,
            xml_files=dataset_files,
            game=game,
            discard_examples=len(examples),
            call_examples=call_examples,
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            models=model_payloads,
            discard_shanten=summarize_discard_shanten(examples),
            parse_failures=parse_failures,
            source=_source_metadata(args),
            game_counts=game_counts,
        )
        report["discard_examples_total"] = total_examples
        report["example_limit"] = args.example_limit
        if streamed_examples is not None:
            _apply_streaming_report_metadata(report, streamed_examples)
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    if args.disagreements is not None:
        disagreement_report = _build_disagreement_report(
            eval_examples,
            linear_models=linear_models,
            max_per_category=args.max_disagreements,
        )
        write_json_report(args.disagreements, disagreement_report)
        print(f"disagreements_path: {args.disagreements}")
    return 0


def _benchmark_discard_mlp(args: argparse.Namespace) -> int:
    try:
        from kenjaku.models.torch_discard import (
            require_torch,
            save_discard_mlp_checkpoint,
            train_discard_mlp,
        )

        require_torch()
    except ImportError as error:
        raise SystemExit(
            f"PyTorch is required for benchmark-discard-mlp; {TORCH_EXTRA_HINT}"
        ) from error

    if args.linear_epochs <= 0:
        raise SystemExit("--linear-epochs must be positive")
    dataset = _parse_tenhou_dataset_from_args(args)
    game = dataset.game
    examples = list(iter_discard_examples(game))
    if not examples:
        raise SystemExit("no discard examples found")

    train_examples, eval_examples = deterministic_split(
        examples,
        eval_fraction=args.eval_fraction,
        seed=args.split_seed,
    )
    frequency_model = DiscardFrequencyBaseline.fit(train_examples)
    risk_model = DiscardLinearModel.fit(
        train_examples,
        epochs=args.linear_epochs,
        learning_rate=args.linear_learning_rate,
        l2=args.linear_l2,
        feature_profile=RISK_CONTEXT_FEATURE_PROFILE,
    )
    defense_model = DiscardLinearModel.fit(
        train_examples,
        epochs=args.linear_epochs,
        learning_rate=args.linear_learning_rate,
        l2=args.linear_l2,
        feature_profile=DEFENSE_CONTEXT_FEATURE_PROFILE,
    )
    try:
        mlp_result = train_discard_mlp(
            train_examples,
            eval_examples,
            epochs=args.epochs,
            batch_size=args.batch_size,
            learning_rate=args.learning_rate,
            hidden_dim=args.hidden_dim,
            device=args.device,
            seed=args.seed,
        )
    except ValueError as error:
        raise SystemExit(str(error)) from error

    if args.checkpoint is not None:
        save_discard_mlp_checkpoint(
            mlp_result,
            args.checkpoint,
            epochs=args.epochs,
            batch_size=args.batch_size,
            learning_rate=args.learning_rate,
            eval_fraction=args.eval_fraction,
            split_seed=args.split_seed,
            seed=args.seed,
        )

    model_payloads = {
        "frequency": _discard_frequency_payload(
            frequency_model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            include_analysis=False,
        ),
        "risk_context_linear": _discard_linear_payload(
            "risk_context_linear",
            risk_model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            epochs=args.linear_epochs,
            learning_rate=args.linear_learning_rate,
            l2=args.linear_l2,
            include_analysis=False,
        ),
        "defense_context_linear": _discard_linear_payload(
            "defense_context_linear",
            defense_model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            epochs=args.linear_epochs,
            learning_rate=args.linear_learning_rate,
            l2=args.linear_l2,
            include_analysis=False,
        ),
        "discard_mlp": _discard_mlp_benchmark_payload(
            mlp_result,
            epochs=args.epochs,
            batch_size=args.batch_size,
            learning_rate=args.learning_rate,
            seed=args.seed,
        ),
    }

    print(f"examples: {len(examples)}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    print(f"device: {mlp_result.device}")
    for model_name in ("frequency", "risk_context_linear", "defense_context_linear"):
        metrics = model_payloads[model_name]["metrics"]
        print(f"{model_name}_train_accuracy: {metrics['train_accuracy']:.4f}")
        print(f"{model_name}_eval_accuracy: {_format_optional_accuracy(metrics['eval_accuracy'])}")
    print(
        "discard_mlp_train_accuracy: "
        f"{_format_optional_accuracy(mlp_result.train_metrics['accuracy'])}"
    )
    print(
        "discard_mlp_eval_accuracy: "
        f"{_format_optional_accuracy(mlp_result.eval_metrics['accuracy'])}"
    )
    print(
        "discard_mlp_best_eval_accuracy: "
        f"{_format_optional_accuracy(mlp_result.best_metrics['eval']['accuracy'])}"
    )
    if dataset.failures:
        print(f"parse_failures: {len(dataset.failures)}")
    if args.checkpoint is not None:
        print(f"checkpoint_path: {args.checkpoint}")
    if args.report is not None:
        report = build_discard_mlp_benchmark_report(
            input_paths=args.paths,
            xml_files=dataset.files,
            game=game,
            discard_examples=len(examples),
            call_examples=sum(1 for _ in iter_call_examples(game)),
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            models=model_payloads,
            discard_shanten=summarize_discard_shanten(examples),
            parse_failures=dataset.failures,
            checkpoint_path=args.checkpoint,
            source=_source_metadata(args),
        )
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


def _benchmark_discard_transformer(args: argparse.Namespace) -> int:
    try:
        from kenjaku.models.torch_discard import require_torch
        from kenjaku.models.torch_transformer import (
            MahjongTransformerConfig,
            save_discard_transformer_checkpoint,
            train_discard_transformer,
            transformer_config_payload,
        )

        require_torch()
    except ImportError as error:
        raise SystemExit(
            f"PyTorch is required for benchmark-discard-transformer; {TORCH_EXTRA_HINT}"
        ) from error

    if args.linear_epochs <= 0:
        raise SystemExit("--linear-epochs must be positive")
    dataset = _parse_tenhou_dataset_from_args(args)
    game = dataset.game
    examples = list(iter_discard_examples(game))
    if not examples:
        raise SystemExit("no discard examples found")

    train_examples, eval_examples = deterministic_split(
        examples,
        eval_fraction=args.eval_fraction,
        seed=args.split_seed,
    )
    frequency_model = DiscardFrequencyBaseline.fit(train_examples)
    risk_model = DiscardLinearModel.fit(
        train_examples,
        epochs=args.linear_epochs,
        learning_rate=args.linear_learning_rate,
        l2=args.linear_l2,
        feature_profile=RISK_CONTEXT_FEATURE_PROFILE,
    )
    defense_model = DiscardLinearModel.fit(
        train_examples,
        epochs=args.linear_epochs,
        learning_rate=args.linear_learning_rate,
        l2=args.linear_l2,
        feature_profile=DEFENSE_CONTEXT_FEATURE_PROFILE,
    )
    config = MahjongTransformerConfig(
        model_dim=args.model_dim,
        num_heads=args.num_heads,
        num_layers=args.num_layers,
        feedforward_dim=args.feedforward_dim,
        dropout=args.dropout,
    )
    try:
        transformer_result = train_discard_transformer(
            train_examples,
            eval_examples,
            config=config,
            epochs=args.epochs,
            batch_size=args.batch_size,
            learning_rate=args.learning_rate,
            device=args.device,
            seed=args.seed,
            value_head=args.value_head,
        )
    except ValueError as error:
        raise SystemExit(str(error)) from error

    if args.checkpoint is not None:
        save_discard_transformer_checkpoint(
            transformer_result,
            args.checkpoint,
            epochs=args.epochs,
            batch_size=args.batch_size,
            learning_rate=args.learning_rate,
            eval_fraction=args.eval_fraction,
            split_seed=args.split_seed,
            seed=args.seed,
        )

    model_payloads = {
        "frequency": _discard_frequency_payload(
            frequency_model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            include_analysis=False,
        ),
        "risk_context_linear": _discard_linear_payload(
            "risk_context_linear",
            risk_model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            epochs=args.linear_epochs,
            learning_rate=args.linear_learning_rate,
            l2=args.linear_l2,
            include_analysis=False,
        ),
        "defense_context_linear": _discard_linear_payload(
            "defense_context_linear",
            defense_model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            epochs=args.linear_epochs,
            learning_rate=args.linear_learning_rate,
            l2=args.linear_l2,
            include_analysis=False,
        ),
        "discard_transformer": _discard_transformer_benchmark_payload(
            transformer_result,
            epochs=args.epochs,
            batch_size=args.batch_size,
            learning_rate=args.learning_rate,
            seed=args.seed,
            config=transformer_config_payload(transformer_result.model.encoder.config),
            value_head=transformer_result.model.has_value_head,
        ),
    }

    print(f"examples: {len(examples)}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    print(f"device: {transformer_result.device}")
    for model_name in ("frequency", "risk_context_linear", "defense_context_linear"):
        metrics = model_payloads[model_name]["metrics"]
        print(f"{model_name}_train_accuracy: {metrics['train_accuracy']:.4f}")
        print(f"{model_name}_eval_accuracy: {_format_optional_accuracy(metrics['eval_accuracy'])}")
    print(
        "discard_transformer_train_accuracy: "
        f"{_format_optional_accuracy(transformer_result.train_metrics['accuracy'])}"
    )
    print(
        "discard_transformer_eval_accuracy: "
        f"{_format_optional_accuracy(transformer_result.eval_metrics['accuracy'])}"
    )
    print(
        "discard_transformer_best_eval_accuracy: "
        f"{_format_optional_accuracy(transformer_result.best_metrics['eval']['accuracy'])}"
    )
    if dataset.failures:
        print(f"parse_failures: {len(dataset.failures)}")
    if args.checkpoint is not None:
        print(f"checkpoint_path: {args.checkpoint}")
    if args.report is not None:
        report = build_discard_transformer_benchmark_report(
            input_paths=args.paths,
            xml_files=dataset.files,
            game=game,
            discard_examples=len(examples),
            call_examples=sum(1 for _ in iter_call_examples(game)),
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            models=model_payloads,
            discard_shanten=summarize_discard_shanten(examples),
            parse_failures=dataset.failures,
            checkpoint_path=args.checkpoint,
            source=_source_metadata(args),
        )
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


def _discard_mlp_benchmark_payload(
    result: Any,
    *,
    epochs: int,
    batch_size: int,
    learning_rate: float,
    seed: int,
) -> dict[str, Any]:
    return {
        "kind": result.model.kind,
        "input_dim": result.model.input_dim,
        "hidden_dim": result.model.hidden_dim,
        "output_dim": result.model.output_dim,
        "training": {
            "epochs": epochs,
            "batch_size": batch_size,
            "learning_rate": learning_rate,
            "device": result.device,
            "seed": seed,
            "history": result.history,
            "best_epoch": result.best_epoch,
            "selection_split": result.selection_split,
        },
        "metrics": {
            "train": result.train_metrics,
            "eval": result.eval_metrics,
            "best": result.best_metrics,
        },
    }


def _discard_transformer_benchmark_payload(
    result: Any,
    *,
    epochs: int,
    batch_size: int,
    learning_rate: float,
    seed: int,
    config: dict[str, int | float],
    value_head: bool,
) -> dict[str, Any]:
    return {
        "kind": result.model.kind,
        "encoder_kind": result.model.encoder.kind,
        "input_tokens": result.model.input_tokens,
        "output_dim": result.model.output_dim,
        "value_head": value_head,
        "config": config,
        "training": {
            "epochs": epochs,
            "batch_size": batch_size,
            "learning_rate": learning_rate,
            "device": result.device,
            "seed": seed,
            "history": result.history,
            "best_epoch": result.best_epoch,
            "selection_split": result.selection_split,
        },
        "metrics": {
            "train": result.train_metrics,
            "eval": result.eval_metrics,
            "best": result.best_metrics,
        },
    }


def _parse_discard_benchmark_models(value: str) -> tuple[str, ...]:
    if value == "all":
        return DISCARD_BENCHMARK_MODEL_ORDER
    if value == "fast":
        return DISCARD_BENCHMARK_FAST_MODELS

    selected: list[str] = []
    for raw_name in value.split(","):
        model_name = raw_name.strip()
        if not model_name:
            continue
        if model_name not in DISCARD_BENCHMARK_MODEL_ORDER:
            raise SystemExit(f"unsupported discard benchmark model: {model_name}")
        if model_name not in selected:
            selected.append(model_name)
    if not selected:
        raise SystemExit("--models must select at least one model")
    return tuple(selected)


def _discard_frequency_payload(
    model: DiscardFrequencyBaseline,
    *,
    train_examples: list[DiscardExample],
    eval_examples: list[DiscardExample],
    include_analysis: bool,
) -> dict[str, Any]:
    train_metrics = _discard_classification_metrics(
        train_examples,
        lambda example: model.predict(example.hand_counts),
    )
    eval_metrics = _discard_classification_metrics(
        eval_examples,
        lambda example: model.predict(example.hand_counts),
    )
    payload: dict[str, Any] = {
        "metrics": {
            "loss_kind": "zero_one",
            "train_examples": train_metrics["examples"],
            "eval_examples": eval_metrics["examples"],
            "train_loss": train_metrics["loss"],
            "eval_loss": eval_metrics["loss"],
            "train_accuracy": train_metrics["accuracy"],
            "eval_accuracy": eval_metrics["accuracy"],
            "train_balanced_accuracy": train_metrics["balanced_accuracy"],
            "eval_balanced_accuracy": eval_metrics["balanced_accuracy"],
            "train_macro_recall": train_metrics["macro_recall"],
            "eval_macro_recall": eval_metrics["macro_recall"],
            "train_action_recall": train_metrics["action_recall"],
            "eval_action_recall": eval_metrics["action_recall"],
        },
    }
    if include_analysis:
        payload["eval_analysis"] = summarize_discard_predictions(
            eval_examples,
            lambda example: model.predict(example.hand_counts),
        )
    return payload


def _discard_linear_payload(
    model_name: str,
    model: DiscardLinearModel,
    *,
    train_examples: list[DiscardExample],
    eval_examples: list[DiscardExample],
    epochs: int,
    learning_rate: float,
    l2: float,
    include_analysis: bool,
) -> dict[str, Any]:
    def predict(example: DiscardExample) -> TileType:
        return _predict_discard_model(model_name, model, example)

    train_metrics = _discard_classification_metrics(train_examples, predict)
    eval_metrics = _discard_classification_metrics(eval_examples, predict)
    payload: dict[str, Any] = {
        "kind": model.kind,
        "feature_dim": model.feature_dim,
        "training": {
            "epochs": epochs,
            "learning_rate": learning_rate,
            "l2": l2,
        },
        "metrics": {
            "loss_kind": "zero_one",
            "train_examples": train_metrics["examples"],
            "eval_examples": eval_metrics["examples"],
            "train_loss": train_metrics["loss"],
            "eval_loss": eval_metrics["loss"],
            "train_accuracy": train_metrics["accuracy"],
            "eval_accuracy": eval_metrics["accuracy"],
            "train_balanced_accuracy": train_metrics["balanced_accuracy"],
            "eval_balanced_accuracy": eval_metrics["balanced_accuracy"],
            "train_macro_recall": train_metrics["macro_recall"],
            "eval_macro_recall": eval_metrics["macro_recall"],
            "train_action_recall": train_metrics["action_recall"],
            "eval_action_recall": eval_metrics["action_recall"],
        },
    }
    if include_analysis:
        payload["eval_analysis"] = summarize_discard_predictions(
            eval_examples,
            predict,
        )
        payload["weight_summary"] = model.weight_summary()
        payload["feature_summary"] = model.feature_summary(eval_examples)
    return payload


def _predict_discard_model(
    model_name: str,
    model: DiscardLinearModel,
    example: DiscardExample,
) -> TileType:
    if model_name in {"raw_count_linear", "linear"}:
        return model.predict(example.hand_counts, example.visible_counts)
    if model_name == "risk_context_linear":
        return model.predict(
            example.hand_counts,
            example.visible_counts,
            seat=example.seat,
            active_riichi_seats=example.active_riichi_seats,
            river_counts_by_seat=example.river_counts_by_seat,
        )
    if model_name == "defense_context_linear":
        return model.predict(
            example.hand_counts,
            example.visible_counts,
            seat=example.seat,
            active_riichi_seats=example.active_riichi_seats,
            river_counts_by_seat=example.river_counts_by_seat,
            rivers_by_seat=example.rivers_by_seat,
            riichi_declared_turns=example.riichi_declared_turns,
            riichi_declared_event_indices=example.riichi_declared_event_indices,
        )
    if model_name == "defense_context_v1_linear":
        return model.predict(
            example.hand_counts,
            example.visible_counts,
            seat=example.seat,
            active_riichi_seats=example.active_riichi_seats,
            river_counts_by_seat=example.river_counts_by_seat,
            rivers_by_seat=example.rivers_by_seat,
            riichi_declared_turns=example.riichi_declared_turns,
            riichi_declared_event_indices=example.riichi_declared_event_indices,
            meld_counts_by_seat=example.meld_counts_by_seat,
            dora_indicators=example.dora_indicators,
            last_discard_tsumogiri_by_seat=example.last_discard_tsumogiri_by_seat,
            ippatsu_active_seats=example.ippatsu_active_seats,
        )
    raise ValueError(f"unsupported discard linear model: {model_name}")


def _print_discard_benchmark_metrics(model_payloads: dict[str, dict[str, Any]]) -> None:
    for model_name in DISCARD_BENCHMARK_MODEL_ORDER:
        if model_name not in model_payloads:
            continue
        metrics = model_payloads[model_name]["metrics"]
        train_accuracy = metrics["train_accuracy"]
        eval_accuracy = metrics["eval_accuracy"]
        print(f"{model_name}_train_accuracy: {train_accuracy:.4f}")
        print(f"{model_name}_eval_accuracy: {_format_optional_accuracy(eval_accuracy)}")
        if model_name == "linear":
            delta = _optional_delta(
                eval_accuracy,
                _model_eval_accuracy(model_payloads, "raw_count_linear"),
            )
            print(f"linear_eval_lift_over_raw_count: {_format_optional_delta(delta)}")
        elif model_name == "risk_context_linear":
            delta = _optional_delta(
                eval_accuracy,
                _model_eval_accuracy(model_payloads, "linear"),
            )
            print(f"risk_context_linear_eval_lift_over_linear: {_format_optional_delta(delta)}")
        elif model_name == "defense_context_linear":
            delta = _optional_delta(
                eval_accuracy,
                _model_eval_accuracy(model_payloads, "risk_context_linear"),
            )
            print(
                "defense_context_linear_eval_lift_over_risk_context: "
                f"{_format_optional_delta(delta)}"
            )
        elif model_name == "defense_context_v1_linear":
            delta = _optional_delta(
                eval_accuracy,
                _model_eval_accuracy(model_payloads, "defense_context_linear"),
            )
            print(
                "defense_context_v1_linear_eval_lift_over_defense_context: "
                f"{_format_optional_delta(delta)}"
            )


def _model_eval_accuracy(
    model_payloads: dict[str, dict[str, Any]],
    model_name: str,
) -> float | None:
    model = model_payloads.get(model_name)
    if model is None:
        return None
    return model["metrics"]["eval_accuracy"]


def _discard_classification_metrics(
    examples: Sequence[DiscardExample],
    predict: Callable[[DiscardExample], TileType],
) -> dict[str, Any]:
    correct = 0
    examples_by_tile = [0] * 34
    correct_by_tile = [0] * 34
    for example in examples:
        if example.action.tile is None:
            raise ValueError("discard examples must have tile actions")
        actual = example.action.tile
        predicted = predict(example)
        is_correct = predicted == actual
        correct += int(is_correct)
        examples_by_tile[actual.index] += 1
        correct_by_tile[actual.index] += int(is_correct)

    count = len(examples)
    accuracy = None if count == 0 else correct / count
    action_recall = {
        TileType(index).notation: (
            None
            if examples_by_tile[index] == 0
            else correct_by_tile[index] / examples_by_tile[index]
        )
        for index in range(34)
    }
    macro_recall = _mean_defined(action_recall.values())
    return {
        "examples": count,
        "loss": _zero_one_loss(accuracy),
        "accuracy": accuracy,
        "balanced_accuracy": macro_recall,
        "macro_recall": macro_recall,
        "action_recall": action_recall,
    }


def _zero_one_loss(accuracy: float | None) -> float | None:
    return None if accuracy is None else 1.0 - accuracy


def _benchmark_report_summary(args: argparse.Namespace) -> int:
    summary = build_discard_benchmark_summary(args.reports)
    if args.json:
        print(json.dumps(summary, indent=2, sort_keys=True))
    else:
        print(format_discard_benchmark_summary(summary))
    return 0


def _benchmark_dashboard(args: argparse.Namespace) -> int:
    output = args.output
    generated_at = datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")
    dashboard = build_public_benchmark_dashboard(
        args.reports,
        version=__version__,
        generated_at=generated_at,
        title=args.title,
    )
    output.parent.mkdir(parents=True, exist_ok=True)
    html = format_public_benchmark_dashboard_html(
        dashboard,
        link_base_dir=output.parent.resolve(),
    )
    output.write_text(html, encoding="utf-8")
    print(f"wrote public benchmark dashboard: {output}")
    return 0


def _write_demo_landing_page(
    path: Path,
    artifacts: dict[str, Path],
    *,
    output_dir: Path,
) -> None:
    items = "\n".join(
        (
            f'        <li><a href="{escape(_artifact_link(artifact_path, output_dir=output_dir))}">'
            f"{escape(label.replace('_', ' ').title())}</a></li>"
        )
        for label, artifact_path in artifacts.items()
    )
    path.write_text(
        f"""<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Kenjaku Fixture Demo</title>
    <style>
      body {{ margin: 0; font-family: system-ui, sans-serif; color: #18202a; background: #f5f7f8; }}
      main {{ max-width: 760px; margin: 0 auto; padding: 40px 24px; }}
      h1 {{ margin: 0 0 12px; font-size: 2rem; }}
      p {{ margin: 0 0 24px; color: #4d5965; }}
      ul {{ padding-left: 20px; line-height: 1.9; }}
      a {{ color: #0958a5; }}
    </style>
  </head>
  <body>
    <main>
      <h1>Kenjaku Fixture Demo</h1>
      <p>Generated from checked-in synthetic fixtures.</p>
      <ul>
{items}
      </ul>
    </main>
  </body>
</html>
""",
        encoding="utf-8",
    )


def _artifact_link(path: Path, *, output_dir: Path) -> str:
    try:
        return path.relative_to(output_dir).as_posix()
    except ValueError:
        return path.as_posix()


def _disagreement_report_summary(args: argparse.Namespace) -> int:
    if args.examples < 0:
        raise SystemExit("--examples must be non-negative")
    if args.tag is not None and args.examples == 0:
        raise SystemExit("--tag requires --examples")
    summary = build_discard_disagreement_summary(args.reports)
    tag_summary = _build_disagreement_tag_summary(args.reports) if args.tags else None
    if tag_summary is not None:
        summary["tags"] = tag_summary
    if args.json:
        print(json.dumps(summary, indent=2, sort_keys=True))
    else:
        print(format_discard_disagreement_summary(summary))
        if tag_summary is not None:
            print()
            print(_format_disagreement_tag_summary(tag_summary))
        if args.examples:
            print()
            print(
                _format_disagreement_examples(
                    args.reports,
                    args.examples,
                    include_tags=args.tags or args.tag is not None,
                    tag_filter=args.tag,
                )
            )
    return 0


def _format_disagreement_examples(
    paths: Sequence[Path],
    examples_per_category: int,
    *,
    include_tags: bool = False,
    tag_filter: str | None = None,
) -> str:
    lines: list[str] = ["examples:"]
    for path in paths:
        payload = json.loads(path.read_text(encoding="utf-8"))
        lines.append(f"report: {path}")
        categories = payload.get("categories", {})
        if not isinstance(categories, dict):
            continue
        for category_name, category in categories.items():
            if not isinstance(category, dict):
                continue
            correct_model, wrong_model = _disagreement_category_models(category_name)
            items = category.get("items", [])
            if not isinstance(items, list) or not items:
                continue
            category_lines: list[str] = []
            rendered = 0
            for item in items:
                if not isinstance(item, dict):
                    continue
                tags = _disagreement_item_tags(
                    item,
                    correct_model=correct_model,
                    wrong_model=wrong_model,
                )
                if tag_filter is not None and tag_filter not in tags:
                    continue
                rendered += 1
                category_lines.extend(
                    _format_disagreement_item(
                        item,
                        index=rendered,
                        correct_model=correct_model,
                        wrong_model=wrong_model,
                        include_tags=include_tags,
                    )
                )
                if rendered >= examples_per_category:
                    break
            if category_lines:
                lines.append(f"{category_name}:")
                lines.extend(category_lines)
    return "\n".join(lines)


def _format_disagreement_item(
    item: dict[str, Any],
    *,
    index: int,
    correct_model: str,
    wrong_model: str,
    include_tags: bool = False,
) -> list[str]:
    predictions = item.get("predictions", {})
    if not isinstance(predictions, dict):
        predictions = {}
    actual = item.get("actual_discard", "unknown")
    lines = [
        (
            f"  {index}. round={item.get('round_index', 'n/a')} "
            f"event={item.get('event_index', 'n/a')} seat={item.get('seat', 'n/a')} "
            f"actual={actual} "
            f"correct={correct_model}:{predictions.get(correct_model, 'n/a')} "
            f"wrong={wrong_model}:{predictions.get(wrong_model, 'n/a')}"
        )
    ]
    buckets = item.get("defense_buckets", {})
    if isinstance(buckets, dict) and buckets:
        bucket_parts = [
            f"{name}={'yes' if enabled else 'no'}" for name, enabled in sorted(buckets.items())
        ]
        lines.append("     buckets: " + ", ".join(bucket_parts))
    defense_risk = _format_disagreement_defense_risk(item, correct_model, wrong_model)
    if defense_risk:
        lines.append("     defense_risk: " + defense_risk)
    if include_tags:
        tags = _disagreement_item_tags(item, correct_model=correct_model, wrong_model=wrong_model)
        lines.append("     tags: " + ", ".join(tags))
    logit_parts = [
        _format_top_logits(item, model_name) for model_name in (correct_model, wrong_model)
    ]
    logit_parts = [part for part in logit_parts if part]
    if logit_parts:
        lines.append("     logits: " + "; ".join(logit_parts))
    return lines


def _format_disagreement_defense_risk(
    item: dict[str, Any],
    correct_model: str,
    wrong_model: str,
) -> str:
    payload = item.get("defense_risk")
    if not isinstance(payload, dict):
        return ""

    parts: list[str] = []
    actual = _format_defense_risk_score(payload.get("actual_discard"))
    if actual:
        parts.append(f"actual={actual}")

    predictions = payload.get("predictions")
    if isinstance(predictions, dict):
        for model_name in (correct_model, wrong_model):
            rendered = _format_defense_risk_score(predictions.get(model_name))
            if rendered:
                parts.append(f"{model_name}={rendered}")
    return " ".join(parts)


def _format_defense_risk_score(payload: Any) -> str:
    if not isinstance(payload, dict):
        return ""
    tile = payload.get("tile")
    risk = payload.get("risk")
    if not isinstance(tile, str) or not isinstance(risk, int | float):
        return ""
    return f"{tile}:{risk:.3f}"


def _build_disagreement_tag_summary(paths: Sequence[Path]) -> dict[str, Any]:
    reports: list[dict[str, Any]] = []
    for path in paths:
        payload = json.loads(path.read_text(encoding="utf-8"))
        report_overall = _empty_disagreement_tag_counts()
        report_categories: dict[str, dict[str, int]] = {}
        categories = payload.get("categories", {})
        if isinstance(categories, dict):
            for category_name, category in categories.items():
                if not isinstance(category, dict):
                    continue
                counts = _empty_disagreement_tag_counts()
                correct_model, wrong_model = _disagreement_category_models(category_name)
                items = category.get("items", [])
                if isinstance(items, list):
                    for item in items:
                        if not isinstance(item, dict):
                            continue
                        tags = _disagreement_item_tags(
                            item,
                            correct_model=correct_model,
                            wrong_model=wrong_model,
                        )
                        _record_disagreement_tags(counts, tags)
                        _record_disagreement_tags(report_overall, tags)
                report_categories[category_name] = counts
        reports.append(
            {
                "path": str(path),
                "categories": report_categories,
                "overall": report_overall,
            }
        )
    return {
        "kind": "kenjaku-discard-disagreement-tags-v0",
        "reports": reports,
    }


def _format_disagreement_tag_summary(summary: dict[str, Any]) -> str:
    lines = ["tags:"]
    for report in summary.get("reports", []):
        if not isinstance(report, dict):
            continue
        lines.append(f"report: {report.get('path', 'unknown')}")
        categories = report.get("categories", {})
        if isinstance(categories, dict):
            for category_name, counts in categories.items():
                if isinstance(counts, dict):
                    lines.append(f"{category_name}: {_format_disagreement_tag_counts(counts)}")
        overall = report.get("overall", {})
        if isinstance(overall, dict):
            lines.append(f"overall: {_format_disagreement_tag_counts(overall)}")
    return "\n".join(lines)


def _format_disagreement_tag_counts(counts: dict[str, Any]) -> str:
    keys = ("stored", *DISAGREEMENT_TAGS)
    return " ".join(f"{key}={int(counts.get(key, 0))}" for key in keys)


def _empty_disagreement_tag_counts() -> dict[str, int]:
    return {
        "stored": 0,
        **{tag: 0 for tag in DISAGREEMENT_TAGS},
    }


def _record_disagreement_tags(counts: dict[str, int], tags: tuple[str, ...]) -> None:
    counts["stored"] += 1
    for tag in tags:
        counts[tag] += 1


def _disagreement_item_tags(
    item: dict[str, Any],
    *,
    correct_model: str,
    wrong_model: str,
) -> tuple[str, ...]:
    buckets = item.get("defense_buckets", {})
    if not isinstance(buckets, dict):
        buckets = {}
    active_riichi = bool(buckets.get("active_riichi_opponent"))
    safe_tile_candidate = any(bool(buckets.get(name)) for name in DISAGREEMENT_SAFE_TILE_BUCKETS)
    defense_signal = active_riichi and safe_tile_candidate
    efficiency_like = _disagreement_preserves_efficiency(item)
    close_logit = _disagreement_close_logit(
        item,
        correct_model=correct_model,
        wrong_model=wrong_model,
    )

    tags: list[str] = []
    if defense_signal:
        tags.append("defense_signal")
    if efficiency_like:
        tags.append("efficiency_like")
    if close_logit:
        tags.append("close_logit")
    if active_riichi:
        tags.append("active_riichi")
    if safe_tile_candidate:
        tags.append("safe_tile_candidate")
    if not tags:
        tags.append("no_obvious_signal")
    return tuple(tags)


def _disagreement_preserves_efficiency(item: dict[str, Any]) -> bool:
    shanten_delta = item.get("shanten_delta", {})
    if not isinstance(shanten_delta, dict):
        return False
    try:
        return float(shanten_delta.get("delta", 1.0)) <= 0.0
    except (TypeError, ValueError):
        return False


def _disagreement_close_logit(
    item: dict[str, Any],
    *,
    correct_model: str,
    wrong_model: str,
) -> bool:
    actual = item.get("actual_discard")
    if not isinstance(actual, str):
        return False
    predictions = item.get("predictions", {})
    if not isinstance(predictions, dict):
        predictions = {}
    return _model_actual_margin_is_close(
        item, correct_model, actual
    ) or _model_error_margin_is_close(
        item,
        wrong_model,
        actual,
        prediction=predictions.get(wrong_model),
    )


def _model_actual_margin_is_close(
    item: dict[str, Any],
    model_name: str,
    actual: str,
) -> bool:
    logits = _logits_by_tile(item, model_name)
    if actual not in logits or len(logits) < 2:
        return False
    best_other = max(logit for tile, logit in logits.items() if tile != actual)
    return abs(logits[actual] - best_other) <= DISAGREEMENT_CLOSE_LOGIT_MARGIN


def _model_error_margin_is_close(
    item: dict[str, Any],
    model_name: str,
    actual: str,
    *,
    prediction: Any,
) -> bool:
    if not isinstance(prediction, str):
        return False
    logits = _logits_by_tile(item, model_name)
    if actual not in logits or prediction not in logits:
        return False
    return abs(logits[prediction] - logits[actual]) <= DISAGREEMENT_CLOSE_LOGIT_MARGIN


def _logits_by_tile(item: dict[str, Any], model_name: str) -> dict[str, float]:
    candidate_logits = item.get("candidate_logits", {})
    if not isinstance(candidate_logits, dict):
        return {}
    entries = candidate_logits.get(model_name, [])
    if not isinstance(entries, list):
        return {}
    parsed: dict[str, float] = {}
    for entry in entries:
        if not isinstance(entry, dict) or "tile" not in entry or "logit" not in entry:
            continue
        try:
            parsed[str(entry["tile"])] = float(entry["logit"])
        except (TypeError, ValueError):
            continue
    return parsed


def _format_top_logits(item: dict[str, Any], model_name: str) -> str:
    candidate_logits = item.get("candidate_logits", {})
    if not isinstance(candidate_logits, dict):
        return ""
    entries = candidate_logits.get(model_name, [])
    if not isinstance(entries, list):
        return ""
    parsed = [
        (str(entry["tile"]), float(entry["logit"]))
        for entry in entries
        if isinstance(entry, dict) and "tile" in entry and "logit" in entry
    ]
    if not parsed:
        return ""
    top = sorted(parsed, key=lambda value: (-value[1], value[0]))[:3]
    return f"{model_name} " + " ".join(f"{tile}={logit:.4f}" for tile, logit in top)


def _disagreement_category_models(category_name: str) -> tuple[str, str]:
    if category_name == "risk_correct_defense_wrong":
        return "risk_context_linear", "defense_context_linear"
    if category_name == "risk_correct_defense_v1_wrong":
        return "risk_context_linear", "defense_context_v1_linear"
    if category_name == "defense_correct_risk_wrong":
        return "defense_context_linear", "risk_context_linear"
    if category_name == "defense_v1_correct_risk_wrong":
        return "defense_context_v1_linear", "risk_context_linear"
    return "correct_model", "wrong_model"


def _benchmark_call_from_examples(args: argparse.Namespace) -> int:
    if args.example_limit is not None and args.example_limit < 0:
        raise SystemExit("--example-limit must be non-negative")
    if args.epochs < 0:
        raise SystemExit("--epochs must be non-negative")
    call_threshold = _validated_probability(args.call_threshold, "--call-threshold")
    call_positive_weight = _validated_positive_float(
        args.call_positive_weight,
        "--call-positive-weight",
    )
    selected_model_names = _parse_call_benchmark_models(
        args.models,
        include_weighted=args.include_weighted,
    )
    load_limit = args.example_limit if args.example_limit_strategy == "prefix" else None
    load = _read_bc_example_load(args.paths, decision_type="call", limit=load_limit)
    all_examples = load.examples
    examples = _limit_call_examples(
        all_examples,
        args.example_limit,
        strategy=args.example_limit_strategy,
    )
    if not examples:
        raise SystemExit("no call examples found")

    train_examples, eval_examples = deterministic_split(
        examples,
        eval_fraction=args.eval_fraction,
        seed=args.split_seed,
    )
    call_models: dict[
        str,
        CallFrequencyBaseline | CallLegalFrequencyBaseline | CallLinearModel,
    ] = {}
    prepared_splits_by_profile: dict[str, tuple[tuple[Any, ...], tuple[Any, ...]]] = {}

    def prepared_split(feature_profile: str) -> tuple[tuple[Any, ...], tuple[Any, ...]]:
        prepared = prepared_splits_by_profile.get(feature_profile)
        if prepared is None:
            prepared = (
                CallLinearModel.prepare_examples_for_profile(
                    train_examples,
                    feature_profile=feature_profile,
                ),
                CallLinearModel.prepare_examples_for_profile(
                    eval_examples,
                    feature_profile=feature_profile,
                ),
            )
            prepared_splits_by_profile[feature_profile] = prepared
        return prepared

    if "call_frequency" in selected_model_names:
        call_models["call_frequency"] = CallFrequencyBaseline.fit(train_examples)
    if "call_legal_frequency" in selected_model_names:
        call_models["call_legal_frequency"] = CallLegalFrequencyBaseline.fit(train_examples)
    if "call_linear" in selected_model_names:
        train_prepared, _ = prepared_split("v0")
        call_models["call_linear"] = CallLinearModel.fit_prepared(
            train_prepared,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
            feature_profile="v0",
        )
    if (
        "call_linear_v1" in selected_model_names
        or "call_linear_v1_calibrated" in selected_model_names
    ):
        train_prepared, _ = prepared_split(CALL_LINEAR_V1_FEATURE_PROFILE)
        call_models["call_linear_v1"] = CallLinearModel.fit_prepared(
            train_prepared,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
            feature_profile=CALL_LINEAR_V1_FEATURE_PROFILE,
        )
    if CALL_BENCHMARK_WEIGHTED_MODEL in selected_model_names:
        train_prepared, _ = prepared_split(CALL_LINEAR_V1_FEATURE_PROFILE)
        call_models["call_linear_v1_weighted"] = CallLinearModel.fit_prepared(
            train_prepared,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
            feature_profile=CALL_LINEAR_V1_FEATURE_PROFILE,
            positive_class_weight=call_positive_weight,
        )

    model_payloads = _call_model_payloads(
        selected_model_names=selected_model_names,
        call_models=call_models,
        prepared_split=prepared_split,
        train_examples=train_examples,
        eval_examples=eval_examples,
        threshold_source=args.call_threshold_source,
        fixed_threshold=call_threshold,
    )

    print(f"examples: {len(examples)}")
    if len(examples) != load.total_examples:
        print(f"source_examples: {load.total_examples}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    _print_call_benchmark_metrics(model_payloads)
    if args.report is not None:
        report = build_call_benchmark_report(
            input_paths=args.paths,
            xml_files=load.source_files,
            game=None,
            game_counts=_bc_game_counts(load),
            discard_examples=_bc_decision_count(load, "discard"),
            call_examples=len(examples),
            call_examples_total=load.total_examples,
            example_limit=args.example_limit,
            example_limit_strategy=args.example_limit_strategy,
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            models=model_payloads,
            parse_failures=load.parse_failures,
            source=_bc_source_metadata(args, load),
        )
        _apply_bc_example_report_metadata(report, load)
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


def _benchmark_call(args: argparse.Namespace) -> int:
    if args.example_limit is not None and args.example_limit < 0:
        raise SystemExit("--example-limit must be non-negative")
    if args.stream_examples and args.example_limit is None:
        raise SystemExit("--stream-examples requires --example-limit")
    if args.stream_examples and args.example_cache is not None:
        raise SystemExit("--stream-examples cannot be combined with --example-cache")
    if args.epochs < 0:
        raise SystemExit("--epochs must be non-negative")
    call_threshold = _validated_probability(args.call_threshold, "--call-threshold")
    call_positive_weight = _validated_positive_float(
        args.call_positive_weight,
        "--call-positive-weight",
    )
    selected_model_names = _parse_call_benchmark_models(
        args.models,
        include_weighted=args.include_weighted,
    )
    timings: dict[str, float] | None = {} if args.profile_stages else None
    source_metadata = _source_metadata(args)
    dataset_files = _timed_stage(
        timings,
        "xml_files",
        lambda: tenhou_xml_files(args.paths),
    )
    example_cache_key = _call_example_cache_key(args=args, dataset_files=dataset_files)
    example_cache_payload = _timed_stage(
        timings,
        "example_cache_load",
        lambda: _read_call_example_cache(args.example_cache),
    )
    example_cache_report = _empty_call_example_cache_report(
        args.example_cache,
        cache_key=example_cache_key,
    )
    cached_examples = _call_example_cache_entry(
        example_cache_payload,
        cache_key=example_cache_key,
    )
    game: TenhouGame | None = None
    discard_examples_total: int | None = None
    streamed_examples: _StreamedExamples | None = None
    if cached_examples is not None:
        all_examples = cached_examples.examples
        parse_failures = cached_examples.parse_failures
        game_counts = cached_examples.game_counts
        discard_examples_total = cached_examples.discard_examples
        example_cache_report["hit"] = True
        example_cache_report["examples"] = len(all_examples)
    elif args.stream_examples:
        streamed_examples = _timed_stage(
            timings,
            "stream_examples",
            lambda: _collect_streamed_examples(
                args.paths,
                example_iter=iter_call_examples,
                limit=args.example_limit,
                skip_errors=args.skip_errors,
                parse_cache_dir=_parse_cache_dir(args),
                jobs=_parse_jobs(args),
                count_discard=True,
            ),
        )
        dataset_files = streamed_examples.source_files
        parse_failures = streamed_examples.parse_failures
        game_counts = streamed_examples.game_counts
        all_examples = streamed_examples.examples
        discard_examples_total = int(streamed_examples.discard_examples or 0)
        example_cache_report["examples"] = len(all_examples)
    else:
        dataset = _timed_stage(
            timings,
            "parse",
            lambda: _parse_tenhou_dataset_from_args(args),
        )
        game = dataset.game
        dataset_files = dataset.files
        parse_failures = dataset.failures
        game_counts = _call_example_cache_game_counts(game)
        all_examples = _timed_stage(
            timings,
            "call_examples",
            lambda: list(iter_call_examples(game)),
        )
        example_cache_report["examples"] = len(all_examples)
        if args.example_cache is not None:
            discard_examples_total = _timed_stage(
                timings,
                "discard_examples_for_cache",
                lambda: sum(1 for _ in iter_discard_examples(game)),
            )
            _timed_stage(
                timings,
                "example_cache_write",
                lambda: _write_call_example_cache(
                    args.example_cache,
                    cache_key=example_cache_key,
                    dataset_files=dataset_files,
                    examples=all_examples,
                    parse_failures=parse_failures,
                    game_counts=game_counts,
                    discard_examples=discard_examples_total,
                    report=example_cache_report,
                ),
            )
    examples = _limit_call_examples(
        all_examples,
        args.example_limit,
        strategy=args.example_limit_strategy,
    )
    if not examples:
        raise SystemExit("no call examples found")

    train_examples, eval_examples = _timed_stage(
        timings,
        "split",
        lambda: deterministic_split(
            examples,
            eval_fraction=args.eval_fraction,
            seed=args.split_seed,
        ),
    )
    feature_cache_payload = _timed_stage(
        timings,
        "feature_cache_load",
        lambda: _read_call_feature_cache(args.feature_cache),
    )
    feature_cache_key = _call_feature_cache_key(
        args=args,
        dataset_files=dataset_files,
        source=source_metadata,
        all_examples_count=len(all_examples),
        examples=examples,
        train_examples=train_examples,
        eval_examples=eval_examples,
    )
    feature_cache_report = _empty_call_feature_cache_report(args.feature_cache)
    call_models: dict[
        str,
        CallFrequencyBaseline | CallLegalFrequencyBaseline | CallLinearModel,
    ] = {}
    prepared_splits_by_profile: dict[str, tuple[tuple[Any, ...], tuple[Any, ...]]] = {}
    prepared_splits_to_cache: dict[str, tuple[tuple[Any, ...], tuple[Any, ...]]] = {}

    def prepared_split(feature_profile: str) -> tuple[tuple[Any, ...], tuple[Any, ...]]:
        prepared = prepared_splits_by_profile.get(feature_profile)
        if prepared is None:
            cached = _call_feature_cache_entry(
                feature_cache_payload,
                cache_key=feature_cache_key,
                feature_profile=feature_profile,
            )
            if cached is not None:
                feature_cache_report["hits"].append(feature_profile)
                prepared = cached
            else:
                feature_cache_report["misses"].append(feature_profile)
                prepared = _timed_stage(
                    timings,
                    f"feature_prepare_{feature_profile}",
                    lambda: (
                        CallLinearModel.prepare_examples_for_profile(
                            train_examples,
                            feature_profile=feature_profile,
                        ),
                        CallLinearModel.prepare_examples_for_profile(
                            eval_examples,
                            feature_profile=feature_profile,
                        ),
                    ),
                )
                if args.feature_cache is not None:
                    prepared_splits_to_cache[feature_profile] = prepared
            prepared_splits_by_profile[feature_profile] = prepared
        return prepared

    if "call_frequency" in selected_model_names:
        call_models["call_frequency"] = _timed_stage(
            timings,
            "train_call_frequency",
            lambda: CallFrequencyBaseline.fit(train_examples),
        )
    if "call_legal_frequency" in selected_model_names:
        call_models["call_legal_frequency"] = _timed_stage(
            timings,
            "train_call_legal_frequency",
            lambda: CallLegalFrequencyBaseline.fit(train_examples),
        )
    if "call_linear" in selected_model_names:
        train_prepared, _ = prepared_split("v0")
        call_models["call_linear"] = _timed_stage(
            timings,
            "train_call_linear",
            lambda: CallLinearModel.fit_prepared(
                train_prepared,
                epochs=args.epochs,
                learning_rate=args.learning_rate,
                l2=args.l2,
                feature_profile="v0",
            ),
        )
    if (
        "call_linear_v1" in selected_model_names
        or "call_linear_v1_calibrated" in selected_model_names
    ):
        train_prepared, _ = prepared_split(CALL_LINEAR_V1_FEATURE_PROFILE)
        call_models["call_linear_v1"] = _timed_stage(
            timings,
            "train_call_linear_v1",
            lambda: CallLinearModel.fit_prepared(
                train_prepared,
                epochs=args.epochs,
                learning_rate=args.learning_rate,
                l2=args.l2,
                feature_profile=CALL_LINEAR_V1_FEATURE_PROFILE,
            ),
        )
    if CALL_BENCHMARK_WEIGHTED_MODEL in selected_model_names:
        train_prepared, _ = prepared_split(CALL_LINEAR_V1_FEATURE_PROFILE)
        call_models["call_linear_v1_weighted"] = _timed_stage(
            timings,
            "train_call_linear_v1_weighted",
            lambda: CallLinearModel.fit_prepared(
                train_prepared,
                epochs=args.epochs,
                learning_rate=args.learning_rate,
                l2=args.l2,
                feature_profile=CALL_LINEAR_V1_FEATURE_PROFILE,
                positive_class_weight=call_positive_weight,
            ),
        )

    if args.feature_cache is not None and prepared_splits_to_cache:
        _timed_stage(
            timings,
            "feature_cache_write",
            lambda: _write_call_feature_cache(
                args.feature_cache,
                existing=feature_cache_payload,
                cache_key=feature_cache_key,
                profiles=prepared_splits_to_cache,
                report=feature_cache_report,
            ),
        )

    model_payloads: dict[str, dict[str, Any]] = _timed_stage(
        timings,
        "payloads",
        lambda: _call_model_payloads(
            selected_model_names=selected_model_names,
            call_models=call_models,
            prepared_split=prepared_split,
            train_examples=train_examples,
            eval_examples=eval_examples,
            threshold_source=args.call_threshold_source,
            fixed_threshold=call_threshold,
        ),
    )

    if timings is not None:
        for stage_name, seconds in timings.items():
            print(f"stage_{stage_name}_seconds: {seconds:.4f}")
    if args.example_cache is not None:
        print(f"example_cache_path: {args.example_cache}")
        print(f"example_cache_hit: {'yes' if example_cache_report['hit'] else 'no'}")
        print(f"example_cache_examples: {example_cache_report['examples']}")
    if args.feature_cache is not None:
        print(f"feature_cache_path: {args.feature_cache}")
        print(f"feature_cache_hits: {','.join(feature_cache_report['hits']) or 'none'}")
        print(f"feature_cache_misses: {','.join(feature_cache_report['misses']) or 'none'}")

    print(f"examples: {len(examples)}")
    if len(examples) != len(all_examples):
        print(f"source_examples: {len(all_examples)}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    _print_call_benchmark_metrics(model_payloads)
    if parse_failures:
        print(f"parse_failures: {len(parse_failures)}")
    if args.report is not None:
        if discard_examples_total is None:
            if game is None:
                raise RuntimeError("cached call examples must include discard example count")
            discard_examples_total = _timed_stage(
                timings,
                "discard_examples_for_report",
                lambda: sum(1 for _ in iter_discard_examples(game)),
            )
        report = build_call_benchmark_report(
            input_paths=args.paths,
            xml_files=dataset_files,
            game=game,
            game_counts=game_counts,
            discard_examples=discard_examples_total,
            call_examples=len(examples),
            call_examples_total=len(all_examples),
            example_limit=args.example_limit,
            example_limit_strategy=args.example_limit_strategy,
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            models=model_payloads,
            parse_failures=parse_failures,
            source=source_metadata,
            timing=timings,
            feature_cache=feature_cache_report if args.feature_cache is not None else None,
            example_cache=example_cache_report if args.example_cache is not None else None,
        )
        if streamed_examples is not None:
            _apply_streaming_report_metadata(report, streamed_examples)
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


def _call_model_payloads(
    *,
    selected_model_names: Sequence[str],
    call_models: dict[str, CallFrequencyBaseline | CallLegalFrequencyBaseline | CallLinearModel],
    prepared_split: Callable[[str], tuple[tuple[Any, ...], tuple[Any, ...]]],
    train_examples: list[CallExample],
    eval_examples: list[CallExample],
    threshold_source: str,
    fixed_threshold: float,
) -> dict[str, dict[str, Any]]:
    model_payloads: dict[str, dict[str, Any]] = {}
    prepared_examples: dict[str, tuple[tuple[Any, ...], tuple[Any, ...]]] = {}
    for model_name, model in call_models.items():
        if isinstance(model, CallLinearModel):
            prepared_examples[model_name] = prepared_split(model.feature_profile)
    for model_name in selected_model_names:
        if model_name == "call_linear_v1_calibrated":
            model = call_models["call_linear_v1"]
            base_payload = model_payloads.get("call_linear_v1")
            if base_payload is None:
                train_prepared, eval_prepared = prepared_examples["call_linear_v1"]
                base_payload = _call_model_payload(
                    model,
                    train_examples=train_examples,
                    eval_examples=eval_examples,
                    train_prepared=train_prepared,
                    eval_prepared=eval_prepared,
                )
            threshold, threshold_source = _selected_policy_threshold(
                source=threshold_source,
                fixed_threshold=fixed_threshold,
                calibration=base_payload.get("calibration", {}),
            )
            train_prepared, eval_prepared = prepared_examples["call_linear_v1"]
            model_payloads[model_name] = _call_model_payload(
                model,
                train_examples=train_examples,
                eval_examples=eval_examples,
                prepared_predict=_call_threshold_prepared_predictor(model, threshold),
                policy=_threshold_policy_metadata(
                    target="call",
                    base_model="call_linear_v1",
                    threshold=threshold,
                    threshold_source=threshold_source,
                ),
                train_prepared=train_prepared,
                eval_prepared=eval_prepared,
            )
            continue

        model = call_models.get(model_name)
        if model is None:
            continue
        train_prepared = None
        eval_prepared = None
        if isinstance(model, CallLinearModel):
            train_prepared, eval_prepared = prepared_examples[model_name]
        model_payloads[model_name] = _call_model_payload(
            model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            train_prepared=train_prepared,
            eval_prepared=eval_prepared,
        )
    return model_payloads


def _parse_call_benchmark_models(value: str, *, include_weighted: bool) -> tuple[str, ...]:
    if value == "all":
        selected = list(CALL_BENCHMARK_DEFAULT_MODELS)
        if include_weighted:
            selected.append(CALL_BENCHMARK_WEIGHTED_MODEL)
        return tuple(selected)
    if value == "fast":
        return CALL_BENCHMARK_FAST_MODELS

    selected: list[str] = []
    for raw_name in value.split(","):
        model_name = raw_name.strip()
        if not model_name:
            continue
        if model_name not in CALL_BENCHMARK_MODEL_ORDER:
            raise SystemExit(f"unsupported call benchmark model: {model_name}")
        if model_name == CALL_BENCHMARK_WEIGHTED_MODEL and not include_weighted:
            raise SystemExit("call_linear_v1_weighted requires --include-weighted")
        if model_name not in selected:
            selected.append(model_name)
    if not selected:
        raise SystemExit("--models must select at least one model")
    return tuple(selected)


def _timed_stage(
    timings: dict[str, float] | None,
    name: str,
    func: Callable[[], TResult],
) -> TResult:
    if timings is None:
        return func()
    start = perf_counter()
    try:
        return func()
    finally:
        timings[name] = perf_counter() - start


def _limit_call_examples(
    examples: Sequence[CallExample],
    limit: int | None,
    *,
    strategy: str,
) -> list[CallExample]:
    if limit is None or len(examples) <= limit:
        return list(examples)
    if strategy == "prefix":
        return list(examples[:limit])
    if strategy == "balanced":
        calls = [example for example in examples if example.action.kind != ActionKind.PASS]
        passes = [example for example in examples if example.action.kind == ActionKind.PASS]
        call_count = min(len(calls), (limit + 1) // 2)
        pass_count = min(len(passes), limit - call_count)
        remaining = limit - call_count - pass_count
        if remaining and call_count < len(calls):
            extra_calls = min(len(calls) - call_count, remaining)
            call_count += extra_calls
            remaining -= extra_calls
        if remaining and pass_count < len(passes):
            pass_count += min(len(passes) - pass_count, remaining)
        selected: list[CallExample] = []
        for index in range(max(call_count, pass_count)):
            if index < call_count:
                selected.append(calls[index])
            if index < pass_count:
                selected.append(passes[index])
        return selected[:limit]
    raise ValueError(f"unsupported call example limit strategy: {strategy}")


def _limit_examples(examples: Sequence[T], limit: int | None) -> list[T]:
    if limit is None or len(examples) <= limit:
        return list(examples)
    return list(examples[:limit])


def _collect_limited_examples(
    examples: Iterable[T],
    limit: int | None,
) -> tuple[list[T], int]:
    selected: list[T] = []
    total = 0
    for example in examples:
        if limit is None or len(selected) < limit:
            selected.append(example)
        total += 1
    return selected, total


def _collect_streamed_examples(
    paths: Sequence[Path],
    *,
    example_iter: Callable[[TenhouGame], Iterable[T]],
    limit: int,
    skip_errors: bool,
    parse_cache_dir: str | Path | None = None,
    jobs: int = 1,
    count_discard: bool = False,
    count_call: bool = False,
) -> _StreamedExamples:
    source_files = tenhou_xml_files(paths)
    if limit < 1:
        raise SystemExit("--example-limit must be at least 1 with --stream-examples")
    selected: list[Any] = []
    parsed_files: list[Path] = []
    parse_failures: list[TenhouParseFailure] = []
    game_counts = _empty_game_counts()
    discard_examples = 0 if count_discard else None
    call_examples = 0 if count_call else None

    for parsed in iter_tenhou_xml_dataset_files(
        source_files,
        skip_errors=skip_errors,
        failures=parse_failures,
        parse_cache_dir=parse_cache_dir,
        jobs=jobs,
    ):
        file_index = parsed.file_index
        file = parsed.path
        game = parsed.game

        parsed_files.append(file)
        _add_game_counts(game_counts, _call_example_cache_game_counts(game))
        if count_discard:
            discard_examples = int(discard_examples or 0) + sum(
                1 for _ in iter_discard_examples(game)
            )
        if count_call:
            call_examples = int(call_examples or 0) + sum(1 for _ in iter_call_examples(game))

        for example in example_iter(game):
            if len(selected) >= limit:
                return _StreamedExamples(
                    examples=selected,
                    source_files=source_files,
                    parsed_files=tuple(parsed_files),
                    parse_failures=tuple(parse_failures),
                    game_counts=game_counts,
                    source_complete=False,
                    discard_examples=discard_examples,
                    call_examples=call_examples,
                )
            selected.append(example)
        if len(selected) >= limit and file_index < len(source_files) - 1:
            return _StreamedExamples(
                examples=selected,
                source_files=source_files,
                parsed_files=tuple(parsed_files),
                parse_failures=tuple(parse_failures),
                game_counts=game_counts,
                source_complete=False,
                discard_examples=discard_examples,
                call_examples=call_examples,
            )

    return _StreamedExamples(
        examples=selected,
        source_files=source_files,
        parsed_files=tuple(parsed_files),
        parse_failures=tuple(parse_failures),
        game_counts=game_counts,
        source_complete=True,
        discard_examples=discard_examples,
        call_examples=call_examples,
    )


def _empty_game_counts() -> dict[str, int]:
    return {
        "rounds": 0,
        "draws": 0,
        "discards": 0,
        "reaches": 0,
        "calls": 0,
        "wins": 0,
        "exhaustive_draws": 0,
    }


def _add_game_counts(target: dict[str, int], counts: dict[str, int]) -> None:
    for key, value in counts.items():
        target[key] = target.get(key, 0) + int(value)


def _read_bc_example_load(
    paths: Sequence[Path],
    *,
    decision_type: str,
    limit: int | None,
) -> BcExampleLoad:
    try:
        return read_bc_examples(
            paths,
            decision_type=decision_type,  # type: ignore[arg-type]  # caller validates decision type
            limit=limit,
        )
    except (OSError, ValueError, json.JSONDecodeError) as error:
        raise SystemExit(str(error)) from error


def _bc_game_counts(load: BcExampleLoad) -> dict[str, int]:
    counts = _empty_game_counts()
    _add_game_counts(counts, load.game_counts)
    return counts


def _bc_decision_count(load: BcExampleLoad, decision_type: str) -> int:
    return int(load.decision_counts.get(decision_type, 0))


def _bc_source_metadata(
    args: argparse.Namespace,
    load: BcExampleLoad,
) -> dict[str, str | None]:
    source = _source_metadata(args)
    return {
        "label": source["label"] or load.source.get("label"),
        "command": source["command"] or load.source.get("command"),
        "date": source["date"] or load.source.get("date"),
    }


def _apply_bc_example_report_metadata(report: dict[str, Any], load: BcExampleLoad) -> None:
    report["bc_example_source"] = {
        "manifest_paths": [str(path) for path in load.manifest_paths],
        "jsonl_file_count": len(load.example_files),
        "source_xml_file_count": len(load.source_files),
        "decision_counts": {
            decision_type: int(load.decision_counts.get(decision_type, 0))
            for decision_type in BC_DECISION_TYPES
        },
    }


def _apply_streaming_report_metadata(
    report: dict[str, Any],
    streamed: _StreamedExamples,
) -> None:
    report["streaming_example_limit"] = True
    report["source_complete"] = streamed.source_complete
    report["source_xml_file_count"] = len(streamed.source_files)
    report["parsed_xml_file_count"] = len(streamed.parsed_files)


def _read_call_example_cache(path: Path | None) -> dict[str, Any] | None:
    if path is None or not path.exists():
        return None
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict) or payload.get("kind") != CALL_EXAMPLE_CACHE_KIND:
        raise SystemExit(f"not a call example cache: {path}")
    return payload


def _call_example_cache_key(
    *,
    args: argparse.Namespace,
    dataset_files: Sequence[Path],
) -> dict[str, Any]:
    return {
        "input_paths": [str(path) for path in args.paths],
        "xml_files": [_call_example_cache_file_key(path) for path in dataset_files],
        "skip_errors": bool(args.skip_errors),
    }


def _call_example_cache_file_key(path: Path) -> dict[str, int | str]:
    stat = path.stat()
    return {
        "path": str(path),
        "size": stat.st_size,
        "mtime_ns": stat.st_mtime_ns,
    }


def _empty_call_example_cache_report(
    path: Path | None,
    *,
    cache_key: dict[str, Any],
) -> dict[str, Any]:
    return {
        "path": None if path is None else str(path),
        "hit": False,
        "writes": False,
        "examples": 0,
        "cache_key": _call_example_cache_key_summary(cache_key),
    }


def _call_example_cache_key_summary(cache_key: dict[str, Any]) -> dict[str, Any]:
    xml_files = cache_key.get("xml_files", [])
    signature = blake2b(
        json.dumps(cache_key, sort_keys=True, separators=(",", ":")).encode("utf-8"),
        digest_size=16,
    ).hexdigest()
    return {
        "input_paths": list(cache_key.get("input_paths", [])),
        "xml_file_count": len(xml_files) if isinstance(xml_files, list) else 0,
        "skip_errors": bool(cache_key.get("skip_errors", False)),
        "signature": signature,
    }


def _call_example_cache_entry(
    payload: dict[str, Any] | None,
    *,
    cache_key: dict[str, Any],
) -> _CachedCallExamples | None:
    if payload is None or payload.get("cache_key") != cache_key:
        return None
    try:
        examples_payload = payload.get("examples")
        failures_payload = payload.get("parse_failures")
        game_counts_payload = payload.get("game_counts")
        discard_examples = int(payload["discard_examples"])
        if not isinstance(examples_payload, list):
            raise ValueError("call example cache examples must be a list")
        if not isinstance(failures_payload, list):
            raise ValueError("call example cache parse_failures must be a list")
        if not isinstance(game_counts_payload, dict):
            raise ValueError("call example cache game_counts must be an object")
        return _CachedCallExamples(
            examples=[_call_example_from_payload(item) for item in examples_payload],
            parse_failures=tuple(_parse_failure_from_payload(item) for item in failures_payload),
            game_counts={key: int(value) for key, value in game_counts_payload.items()},
            discard_examples=discard_examples,
        )
    except (KeyError, TypeError, ValueError):
        return None


def _write_call_example_cache(
    path: Path,
    *,
    cache_key: dict[str, Any],
    dataset_files: Sequence[Path],
    examples: Sequence[CallExample],
    parse_failures: Sequence[TenhouParseFailure],
    game_counts: dict[str, int],
    discard_examples: int,
    report: dict[str, Any],
) -> None:
    payload = {
        "kind": CALL_EXAMPLE_CACHE_KIND,
        "created_at": datetime.now(UTC).isoformat(),
        "cache_key": cache_key,
        "xml_files": [str(path) for path in dataset_files],
        "game_counts": game_counts,
        "discard_examples": discard_examples,
        "parse_failures": [_parse_failure_payload_for_cache(failure) for failure in parse_failures],
        "examples": [_call_example_to_payload(example) for example in examples],
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, sort_keys=True) + "\n", encoding="utf-8")
    report["writes"] = True


def _call_example_cache_game_counts(game: TenhouGame) -> dict[str, int]:
    return {
        "rounds": len(game.rounds),
        "draws": sum(len(round_.draws) for round_ in game.rounds),
        "discards": sum(len(round_.discards) for round_ in game.rounds),
        "reaches": sum(len(round_.reaches) for round_ in game.rounds),
        "calls": sum(len(round_.calls) for round_ in game.rounds),
        "wins": sum(len(round_.agari) for round_ in game.rounds),
        "exhaustive_draws": sum(round_.ryuukyoku is not None for round_ in game.rounds),
    }


def _call_example_to_payload(example: CallExample) -> dict[str, Any]:
    return {
        "round_index": example.round_index,
        "event_index": example.event_index,
        "call_event_index": example.call_event_index,
        "seat": example.seat,
        "from_seat": example.from_seat,
        "dealer": example.dealer,
        "scores": list(example.scores),
        "discarded_tile": example.discarded_tile.notation,
        "legal_call_kinds": [kind.value for kind in example.legal_call_kinds],
        "hand_counts": list(example.hand_counts),
        "visible_counts": list(example.visible_counts),
        "action": _action_to_call_example_cache_payload(example.action),
    }


def _call_example_from_payload(payload: Any) -> CallExample:
    if not isinstance(payload, dict):
        raise ValueError("call example cache rows must be objects")
    return CallExample(
        round_index=int(payload["round_index"]),
        event_index=int(payload["event_index"]),
        call_event_index=_optional_int(payload.get("call_event_index")),
        seat=int(payload["seat"]),
        from_seat=int(payload["from_seat"]),
        dealer=int(payload["dealer"]),
        scores=tuple(int(score) for score in _required_list(payload, "scores")),
        discarded_tile=Tile.parse(str(payload["discarded_tile"])),
        legal_call_kinds=tuple(
            ActionKind(str(kind)) for kind in _required_list(payload, "legal_call_kinds")
        ),
        hand_counts=_tile_counts_from_payload(payload, "hand_counts"),
        visible_counts=_tile_counts_from_payload(payload, "visible_counts"),
        action=_action_from_call_example_cache_payload(payload.get("action")),
    )


def _action_to_call_example_cache_payload(action: Action) -> dict[str, Any]:
    payload: dict[str, Any] = {"kind": action.kind.value}
    if action.tile is not None:
        payload["tile"] = action.tile.notation
    if action.kind == ActionKind.DISCARD:
        payload["tsumogiri"] = action.tsumogiri
    if action.consumed:
        payload["consumed"] = [tile.notation for tile in action.consumed]
    return payload


def _action_from_call_example_cache_payload(payload: Any) -> Action:
    if not isinstance(payload, dict):
        raise ValueError("call example cache action must be an object")
    kind = ActionKind(str(payload["kind"]))
    tile = None
    if payload.get("tile") is not None:
        tile = TileType.parse(str(payload["tile"]))
    consumed = tuple(Tile.parse(str(tile)) for tile in payload.get("consumed", []))
    return Action(
        kind=kind,
        tile=tile,
        tsumogiri=bool(payload.get("tsumogiri", False)),
        consumed=consumed,
    )


def _parse_failure_payload_for_cache(failure: TenhouParseFailure) -> dict[str, str]:
    return {
        "path": str(failure.path),
        "error_type": failure.error_type,
        "message": failure.message,
    }


def _parse_failure_from_payload(payload: Any) -> TenhouParseFailure:
    if not isinstance(payload, dict):
        raise ValueError("call example cache parse failures must be objects")
    return TenhouParseFailure(
        path=Path(str(payload["path"])),
        error_type=str(payload["error_type"]),
        message=str(payload["message"]),
    )


def _tile_counts_from_payload(payload: dict[str, Any], key: str) -> tuple[int, ...]:
    counts = tuple(int(count) for count in _required_list(payload, key))
    if len(counts) != 34:
        raise ValueError(f"{key} must contain 34 tile counts")
    return counts


def _required_list(payload: dict[str, Any], key: str) -> list[Any]:
    value = payload.get(key)
    if not isinstance(value, list):
        raise ValueError(f"{key} must be a list")
    return value


def _optional_int(value: Any) -> int | None:
    if value is None:
        return None
    return int(value)


def _read_call_feature_cache(path: Path | None) -> dict[str, Any] | None:
    if path is None or not path.exists():
        return None
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict) or payload.get("kind") != CALL_FEATURE_CACHE_KIND:
        raise SystemExit(f"not a call feature cache: {path}")
    return payload


def _call_feature_cache_key(
    *,
    args: argparse.Namespace,
    dataset_files: Sequence[Path],
    source: dict[str, str | None],
    all_examples_count: int,
    examples: Sequence[CallExample],
    train_examples: Sequence[CallExample],
    eval_examples: Sequence[CallExample],
) -> dict[str, Any]:
    return {
        "input_paths": [str(path) for path in args.paths],
        "xml_files": [str(path) for path in dataset_files],
        "source": source,
        "split_seed": args.split_seed,
        "eval_fraction": args.eval_fraction,
        "example_limit": args.example_limit,
        "example_limit_strategy": args.example_limit_strategy,
        "all_examples": all_examples_count,
        "examples": len(examples),
        "train_examples": len(train_examples),
        "eval_examples": len(eval_examples),
        "example_signature": _call_examples_signature(examples),
        "train_signature": _call_examples_signature(train_examples),
        "eval_signature": _call_examples_signature(eval_examples),
    }


def _call_examples_signature(examples: Sequence[CallExample]) -> str:
    digest = blake2b(digest_size=16)
    for example in examples:
        action_tile = None if example.action.tile is None else example.action.tile.notation
        digest.update(
            json.dumps(
                [
                    example.round_index,
                    example.event_index,
                    example.call_event_index,
                    example.seat,
                    example.from_seat,
                    example.discarded_tile.notation,
                    example.action.kind.value,
                    action_tile,
                ],
                separators=(",", ":"),
            ).encode("utf-8")
        )
    return digest.hexdigest()


def _empty_call_feature_cache_report(path: Path | None) -> dict[str, Any]:
    return {
        "path": None if path is None else str(path),
        "hits": [],
        "misses": [],
        "writes": [],
    }


def _call_feature_cache_entry(
    payload: dict[str, Any] | None,
    *,
    cache_key: dict[str, Any],
    feature_profile: str,
) -> tuple[tuple[Any, ...], tuple[Any, ...]] | None:
    if payload is None or payload.get("cache_key") != cache_key:
        return None
    profiles = payload.get("profiles")
    if not isinstance(profiles, dict):
        return None
    entry = profiles.get(feature_profile)
    if not isinstance(entry, dict):
        return None
    try:
        return (
            CallLinearModel.prepared_examples_from_payload(entry.get("train")),
            CallLinearModel.prepared_examples_from_payload(entry.get("eval")),
        )
    except ValueError:
        return None


def _write_call_feature_cache(
    path: Path,
    *,
    existing: dict[str, Any] | None,
    cache_key: dict[str, Any],
    profiles: dict[str, tuple[tuple[Any, ...], tuple[Any, ...]]],
    report: dict[str, Any],
) -> None:
    existing_profiles: dict[str, Any] = {}
    if existing is not None and existing.get("cache_key") == cache_key:
        raw_profiles = existing.get("profiles")
        if isinstance(raw_profiles, dict):
            existing_profiles.update(raw_profiles)
    for feature_profile, (train_prepared, eval_prepared) in profiles.items():
        existing_profiles[feature_profile] = {
            "train": CallLinearModel.prepared_examples_to_payload(train_prepared),
            "eval": CallLinearModel.prepared_examples_to_payload(eval_prepared),
        }
        report["writes"].append(feature_profile)
    payload = {
        "kind": CALL_FEATURE_CACHE_KIND,
        "cache_key": cache_key,
        "profiles": existing_profiles,
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, sort_keys=True) + "\n", encoding="utf-8")


def _selected_policy_threshold(
    *,
    source: str,
    fixed_threshold: float,
    calibration: dict[str, Any],
) -> tuple[float, str]:
    if source == THRESHOLD_SOURCE_FIXED:
        return fixed_threshold, FIXED_THRESHOLD_SOURCE_LABEL
    if source == THRESHOLD_SOURCE_TRAIN_BEST:
        best = _calibration_best(calibration, "train")
        if isinstance(best, dict) and best.get("threshold") is not None:
            return float(best["threshold"]), THRESHOLD_SOURCE_TRAIN_BEST
        return fixed_threshold, f"{THRESHOLD_SOURCE_TRAIN_BEST}-fallback-fixed"
    raise ValueError(f"unsupported threshold source: {source}")


CallPredictor = Callable[[CallExample], ActionKind]
PreparedCallPredictor = Callable[[Any], ActionKind]


def _call_threshold_predictor(
    model: CallLinearModel,
    threshold: float,
) -> CallPredictor:
    def predict(example: CallExample) -> ActionKind:
        probabilities = model.probabilities_for_example(example)
        non_pass_probabilities = {
            kind: probability
            for kind, probability in probabilities.items()
            if kind != ActionKind.PASS
        }
        score = sum(non_pass_probabilities.values()) if non_pass_probabilities else -1.0
        if score < threshold:
            return ActionKind.PASS
        return max(
            non_pass_probabilities,
            key=lambda kind: (
                non_pass_probabilities[kind],
                -CALL_DECISION_KINDS.index(kind),
            ),
        )

    return predict


def _call_threshold_prepared_predictor(
    model: CallLinearModel,
    threshold: float,
) -> PreparedCallPredictor:
    def predict(prepared: Any) -> ActionKind:
        probabilities = model.probabilities_for_prepared(prepared)
        non_pass_probabilities = {
            kind: probability
            for kind, probability in probabilities.items()
            if kind != ActionKind.PASS
        }
        score = sum(non_pass_probabilities.values()) if non_pass_probabilities else -1.0
        if score < threshold:
            return ActionKind.PASS
        return max(
            non_pass_probabilities,
            key=lambda kind: (
                non_pass_probabilities[kind],
                -CALL_DECISION_KINDS.index(kind),
            ),
        )

    return predict


def _call_model_payload(
    model: CallFrequencyBaseline | CallLegalFrequencyBaseline | CallLinearModel,
    *,
    train_examples: list[CallExample],
    eval_examples: list[CallExample],
    predict: CallPredictor | None = None,
    prepared_predict: PreparedCallPredictor | None = None,
    policy: dict[str, Any] | None = None,
    train_prepared: tuple[Any, ...] | None = None,
    eval_prepared: tuple[Any, ...] | None = None,
) -> dict[str, Any]:
    if isinstance(model, CallLinearModel):
        train_prepared = (
            model.prepare_examples(train_examples) if train_prepared is None else train_prepared
        )
        eval_prepared = (
            model.prepare_examples(eval_examples) if eval_prepared is None else eval_prepared
        )
        train_analysis = _summarize_call_predictions_from_prepared(
            train_examples,
            train_prepared,
            model=model,
            prepared_predict=prepared_predict,
        )
        eval_analysis = _summarize_call_predictions_from_prepared(
            eval_examples,
            eval_prepared,
            model=model,
            prepared_predict=prepared_predict,
        )
    else:
        predictor = model.predict if predict is None else predict
        train_analysis = _summarize_call_predictions(train_examples, predictor)
        eval_analysis = _summarize_call_predictions(eval_examples, predictor)
    train_metrics = _call_metrics(train_analysis)
    eval_metrics = _call_metrics(eval_analysis)
    payload: dict[str, Any] = {
        "kind": model.kind,
        "metrics": {
            "loss_kind": "zero_one",
            "train_loss": _zero_one_loss(train_metrics["accuracy"]),
            "eval_loss": _zero_one_loss(eval_metrics["accuracy"]),
            "train_accuracy": train_metrics["accuracy"],
            "eval_accuracy": eval_metrics["accuracy"],
            "train_balanced_accuracy": train_metrics["balanced_accuracy"],
            "eval_balanced_accuracy": eval_metrics["balanced_accuracy"],
            "train_macro_recall": train_metrics["macro_recall"],
            "eval_macro_recall": eval_metrics["macro_recall"],
            "train_pass_recall": train_metrics["pass_recall"],
            "eval_pass_recall": eval_metrics["pass_recall"],
            "train_call_recall": train_metrics["call_recall"],
            "eval_call_recall": eval_metrics["call_recall"],
            "train_action_recall": train_metrics["action_recall"],
            "eval_action_recall": eval_metrics["action_recall"],
        },
        "train_analysis": train_analysis,
        "eval_analysis": eval_analysis,
    }
    if policy is not None:
        payload["policy"] = policy
    if isinstance(model, CallLinearModel):
        payload["feature_dim"] = model.feature_dim
        payload["feature_profile"] = model.feature_profile
        payload["training"] = {
            "epochs": model.epochs,
            "learning_rate": model.learning_rate,
            "l2": model.l2,
            "positive_class_weight": model.positive_class_weight,
        }
        payload["calibration"] = {
            "target": "call",
            "thresholds": list(CALIBRATION_THRESHOLDS),
            "train": _call_threshold_sweep_prepared(model, train_prepared),
            "eval": _call_threshold_sweep_prepared(model, eval_prepared),
        }
    else:
        payload["counts"] = model.count_by_kind()
    return payload


def _print_call_benchmark_metrics(model_payloads: dict[str, dict[str, Any]]) -> None:
    for model_name, payload in model_payloads.items():
        metrics = payload["metrics"]
        print(f"{model_name}_train_accuracy: {metrics['train_accuracy']:.4f}")
        print(f"{model_name}_eval_accuracy: {_format_optional_accuracy(metrics['eval_accuracy'])}")
        print(
            f"{model_name}_eval_balanced_accuracy: "
            f"{_format_optional_accuracy(metrics['eval_balanced_accuracy'])}"
        )
        print(
            f"{model_name}_eval_pass_recall: "
            f"{_format_optional_accuracy(metrics['eval_pass_recall'])}"
        )
        print(
            f"{model_name}_eval_call_recall: "
            f"{_format_optional_accuracy(metrics['eval_call_recall'])}"
        )
        calibration = payload.get("calibration")
        if isinstance(calibration, dict):
            best = _calibration_best(calibration, "eval")
            print(
                f"{model_name}_eval_best_threshold: "
                f"{_format_calibration_best(best, target_name='call')}"
            )
        policy = payload.get("policy")
        if isinstance(policy, dict) and policy.get("kind") == "threshold-calibrated-v0":
            print(f"{model_name}_policy_threshold: {float(policy['threshold']):.2f}")


def _summarize_call_predictions(
    examples: Sequence[CallExample],
    predict: CallPredictor,
) -> dict[str, Any]:
    return _summarize_call_prediction_results(
        examples,
        (predict(example) for example in examples),
    )


def _summarize_call_predictions_from_prepared(
    examples: Sequence[CallExample],
    prepared_examples: Sequence[Any],
    *,
    model: CallLinearModel,
    prepared_predict: PreparedCallPredictor | None,
) -> dict[str, Any]:
    if prepared_predict is None:
        predictions = (model.predict_prepared(prepared) for prepared in prepared_examples)
    else:
        predictions = (prepared_predict(prepared) for prepared in prepared_examples)
    return _summarize_call_prediction_results(examples, predictions)


def _summarize_call_prediction_results(
    examples: Sequence[CallExample],
    predictions: Iterable[ActionKind],
) -> dict[str, Any]:
    buckets: dict[str, Any] = {
        "overall": _empty_call_bucket(),
        "by_call_or_pass": {
            "pass": _empty_call_bucket(),
            "call": _empty_call_bucket(),
        },
        "by_actual_action": {kind.value: _empty_call_bucket() for kind in CALL_DECISION_KINDS},
        "by_legal_call_kinds": {},
    }
    action_distribution = {kind.value: 0 for kind in CALL_DECISION_KINDS}

    for example, prediction in zip(examples, predictions, strict=True):
        actual = example.action.kind
        correct = prediction == actual
        action_distribution[actual.value] += 1
        call_or_pass = "pass" if actual == ActionKind.PASS else "call"
        legal_key = _legal_call_key(example)
        legal_buckets = buckets["by_legal_call_kinds"]
        legal_buckets.setdefault(legal_key, _empty_call_bucket())

        _record_call_bucket(buckets["overall"], correct)
        _record_call_bucket(buckets["by_call_or_pass"][call_or_pass], correct)
        _record_call_bucket(buckets["by_actual_action"][actual.value], correct)
        _record_call_bucket(legal_buckets[legal_key], correct)

    return {
        "action_distribution": action_distribution,
        **_finalize_call_buckets(buckets),
    }


def _call_metrics(analysis: dict[str, Any]) -> dict[str, Any]:
    action_recall = {
        kind.value: analysis["by_actual_action"][kind.value]["accuracy"]
        for kind in CALL_DECISION_KINDS
    }
    pass_recall = analysis["by_call_or_pass"]["pass"]["accuracy"]
    call_recall = analysis["by_call_or_pass"]["call"]["accuracy"]
    return {
        "accuracy": analysis["overall"]["accuracy"],
        "balanced_accuracy": _mean_defined((pass_recall, call_recall)),
        "macro_recall": _mean_defined(action_recall.values()),
        "pass_recall": pass_recall,
        "call_recall": call_recall,
        "action_recall": action_recall,
    }


def _call_threshold_sweep(
    model: CallLinearModel,
    examples: Sequence[CallExample],
) -> dict[str, Any]:
    return _call_threshold_sweep_prepared(model, model.prepare_examples(examples))


def _call_threshold_sweep_prepared(
    model: CallLinearModel,
    prepared_examples: Sequence[Any],
) -> dict[str, Any]:
    records = []
    for prepared in prepared_examples:
        probabilities = model.probabilities_for_prepared(prepared)
        non_pass_probabilities = {
            kind: probability
            for kind, probability in probabilities.items()
            if kind != ActionKind.PASS
        }
        score = sum(non_pass_probabilities.values()) if non_pass_probabilities else -1.0
        records.append(
            {
                "score": score,
                "actual_positive": prepared.target != ActionKind.PASS,
            }
        )
    return _binary_threshold_sweep(records, target_name="call")


def _binary_threshold_sweep(
    records: Sequence[dict[str, float | bool]],
    *,
    target_name: str,
) -> dict[str, Any]:
    thresholds = [
        _binary_threshold_entry(records, threshold=threshold, target_name=target_name)
        for threshold in CALIBRATION_THRESHOLDS
    ]
    if not records:
        best = None
    else:
        best = max(
            thresholds,
            key=lambda entry: (
                _metric_sort_value(entry["balanced_accuracy"]),
                _metric_sort_value(entry[f"{target_name}_recall"]),
                -float(entry["threshold"]),
            ),
        )
    return {
        "examples": len(records),
        "thresholds": thresholds,
        "best": best,
    }


def _binary_threshold_entry(
    records: Sequence[dict[str, float | bool]],
    *,
    threshold: float,
    target_name: str,
) -> dict[str, Any]:
    true_positive = 0
    false_positive = 0
    true_negative = 0
    false_negative = 0
    for record in records:
        score = float(record["score"])
        actual_positive = bool(record["actual_positive"])
        predicted_positive = score >= threshold
        if actual_positive and predicted_positive:
            true_positive += 1
        elif actual_positive:
            false_negative += 1
        elif predicted_positive:
            false_positive += 1
        else:
            true_negative += 1

    examples = len(records)
    target_predictions = true_positive + false_positive
    actual_target = true_positive + false_negative
    actual_pass = true_negative + false_positive
    target_recall = _safe_ratio(true_positive, actual_target)
    pass_recall = _safe_ratio(true_negative, actual_pass)
    return {
        "threshold": threshold,
        "examples": examples,
        "correct": true_positive + true_negative,
        "binary_accuracy": _safe_ratio(true_positive + true_negative, examples),
        "balanced_accuracy": _mean_defined((target_recall, pass_recall)),
        f"{target_name}_precision": _safe_ratio(true_positive, target_predictions),
        f"{target_name}_recall": target_recall,
        "pass_recall": pass_recall,
        f"predicted_{target_name}_examples": target_predictions,
        f"actual_{target_name}_examples": actual_target,
        "actual_pass_examples": actual_pass,
    }


def _safe_ratio(numerator: int, denominator: int) -> float | None:
    return None if denominator == 0 else numerator / denominator


def _metric_sort_value(value: Any) -> float:
    return -1.0 if value is None else float(value)


def _calibration_best(calibration: dict[str, Any], split: str) -> dict[str, Any] | None:
    split_payload = calibration.get(split)
    if not isinstance(split_payload, dict):
        return None
    best = split_payload.get("best")
    return best if isinstance(best, dict) else None


def _format_calibration_best(best: dict[str, Any] | None, *, target_name: str) -> str:
    if best is None:
        return "n/a"
    threshold = float(best["threshold"])
    return (
        f"{threshold:.2f} "
        f"balanced={_format_optional_accuracy(best['balanced_accuracy'])} "
        f"{target_name}_precision="
        f"{_format_optional_accuracy(best[f'{target_name}_precision'])} "
        f"{target_name}_recall={_format_optional_accuracy(best[f'{target_name}_recall'])} "
        f"pass_recall={_format_optional_accuracy(best['pass_recall'])}"
    )


def _threshold_policy_metadata(
    *,
    target: str,
    base_model: str,
    threshold: float,
    threshold_source: str,
) -> dict[str, Any]:
    return {
        "kind": "threshold-calibrated-v0",
        "target": target,
        "base_model": base_model,
        "threshold": threshold,
        "threshold_source": threshold_source,
    }


def _mean_defined(values: Iterable[float | None]) -> float | None:
    defined = [value for value in values if value is not None]
    if not defined:
        return None
    return sum(defined) / len(defined)


def _benchmark_riichi_from_examples(args: argparse.Namespace) -> int:
    if args.example_limit is not None and args.example_limit < 0:
        raise SystemExit("--example-limit must be non-negative")
    if args.epochs < 0:
        raise SystemExit("--epochs must be non-negative")
    riichi_threshold = _validated_probability(args.riichi_threshold, "--riichi-threshold")
    riichi_positive_weight = _validated_positive_float(
        args.riichi_positive_weight,
        "--riichi-positive-weight",
    )
    load = _read_bc_example_load(args.paths, decision_type="riichi", limit=args.example_limit)
    examples = load.examples
    if not examples:
        raise SystemExit("no riichi examples found")

    train_examples, eval_examples = deterministic_split(
        examples,
        eval_fraction=args.eval_fraction,
        seed=args.split_seed,
    )
    riichi_models = {
        "riichi_frequency": RiichiFrequencyBaseline.fit(train_examples),
        "riichi_linear": RiichiLinearModel.fit(
            train_examples,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
        ),
    }
    if args.include_weighted:
        riichi_models["riichi_linear_weighted"] = RiichiLinearModel.fit(
            train_examples,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
            positive_class_weight=riichi_positive_weight,
        )

    model_payloads: dict[str, dict[str, Any]] = {}
    prepared_examples: dict[str, tuple[tuple[Any, ...], tuple[Any, ...]]] = {}
    for model_name, model in riichi_models.items():
        if isinstance(model, RiichiLinearModel):
            prepared_examples[model_name] = (
                model.prepare_examples(train_examples),
                model.prepare_examples(eval_examples),
            )
    for model_name, model in riichi_models.items():
        train_prepared = None
        eval_prepared = None
        if isinstance(model, RiichiLinearModel):
            train_prepared, eval_prepared = prepared_examples[model_name]
        model_payloads[model_name] = _riichi_model_payload(
            model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            train_prepared=train_prepared,
            eval_prepared=eval_prepared,
        )
        if model_name == "riichi_linear":
            threshold, threshold_source = _selected_policy_threshold(
                source=args.riichi_threshold_source,
                fixed_threshold=riichi_threshold,
                calibration=model_payloads[model_name].get("calibration", {}),
            )
            train_prepared, eval_prepared = prepared_examples[model_name]
            model_payloads["riichi_linear_calibrated"] = _riichi_model_payload(
                model,
                train_examples=train_examples,
                eval_examples=eval_examples,
                prepared_predict=_riichi_threshold_prepared_predictor(model, threshold),
                policy=_threshold_policy_metadata(
                    target="riichi",
                    base_model=model_name,
                    threshold=threshold,
                    threshold_source=threshold_source,
                ),
                train_prepared=train_prepared,
                eval_prepared=eval_prepared,
            )

    print(f"examples: {len(examples)}")
    if len(examples) != load.total_examples:
        print(f"source_examples: {load.total_examples}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    _print_riichi_benchmark_metrics(model_payloads)
    if args.report is not None:
        report = build_riichi_benchmark_report(
            input_paths=args.paths,
            xml_files=load.source_files,
            game=None,
            game_counts=_bc_game_counts(load),
            discard_examples=_bc_decision_count(load, "discard"),
            call_examples=_bc_decision_count(load, "call"),
            riichi_examples=len(examples),
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            models=model_payloads,
            parse_failures=load.parse_failures,
            source=_bc_source_metadata(args, load),
        )
        report["riichi_examples_total"] = load.total_examples
        report["example_limit"] = args.example_limit
        _apply_bc_example_report_metadata(report, load)
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


def _benchmark_riichi(args: argparse.Namespace) -> int:
    if args.example_limit is not None and args.example_limit < 0:
        raise SystemExit("--example-limit must be non-negative")
    if args.stream_examples and args.example_limit is None:
        raise SystemExit("--stream-examples requires --example-limit")
    riichi_threshold = _validated_probability(args.riichi_threshold, "--riichi-threshold")
    riichi_positive_weight = _validated_positive_float(
        args.riichi_positive_weight,
        "--riichi-positive-weight",
    )
    streamed_examples: _StreamedExamples | None = None
    game: TenhouGame | None
    if args.stream_examples:
        streamed_examples = _collect_streamed_examples(
            args.paths,
            example_iter=iter_riichi_examples,
            limit=args.example_limit,
            skip_errors=args.skip_errors,
            parse_cache_dir=_parse_cache_dir(args),
            jobs=_parse_jobs(args),
            count_discard=True,
            count_call=True,
        )
        game = None
        dataset_files = streamed_examples.source_files
        parse_failures = streamed_examples.parse_failures
        game_counts = streamed_examples.game_counts
        examples = streamed_examples.examples
        total_examples = len(examples)
        discard_examples = int(streamed_examples.discard_examples or 0)
        call_examples = int(streamed_examples.call_examples or 0)
    else:
        dataset = _parse_tenhou_dataset_from_args(args)
        game = dataset.game
        dataset_files = dataset.files
        parse_failures = dataset.failures
        game_counts = None
        examples, total_examples = _collect_limited_examples(
            iter_riichi_examples(game),
            args.example_limit,
        )
        discard_examples = sum(1 for _ in iter_discard_examples(game))
        call_examples = sum(1 for _ in iter_call_examples(game))
    if not examples:
        raise SystemExit("no riichi examples found")

    train_examples, eval_examples = deterministic_split(
        examples,
        eval_fraction=args.eval_fraction,
        seed=args.split_seed,
    )
    riichi_models = {
        "riichi_frequency": RiichiFrequencyBaseline.fit(train_examples),
        "riichi_linear": RiichiLinearModel.fit(
            train_examples,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
        ),
    }
    if args.include_weighted:
        riichi_models["riichi_linear_weighted"] = RiichiLinearModel.fit(
            train_examples,
            epochs=args.epochs,
            learning_rate=args.learning_rate,
            l2=args.l2,
            positive_class_weight=riichi_positive_weight,
        )

    model_payloads: dict[str, dict[str, Any]] = {}
    prepared_examples: dict[str, tuple[tuple[Any, ...], tuple[Any, ...]]] = {}
    for model_name, model in riichi_models.items():
        if isinstance(model, RiichiLinearModel):
            prepared_examples[model_name] = (
                model.prepare_examples(train_examples),
                model.prepare_examples(eval_examples),
            )
    for model_name, model in riichi_models.items():
        train_prepared = None
        eval_prepared = None
        if isinstance(model, RiichiLinearModel):
            train_prepared, eval_prepared = prepared_examples[model_name]
        model_payloads[model_name] = _riichi_model_payload(
            model,
            train_examples=train_examples,
            eval_examples=eval_examples,
            train_prepared=train_prepared,
            eval_prepared=eval_prepared,
        )
        if model_name == "riichi_linear":
            threshold, threshold_source = _selected_policy_threshold(
                source=args.riichi_threshold_source,
                fixed_threshold=riichi_threshold,
                calibration=model_payloads[model_name].get("calibration", {}),
            )
            train_prepared, eval_prepared = prepared_examples[model_name]
            model_payloads["riichi_linear_calibrated"] = _riichi_model_payload(
                model,
                train_examples=train_examples,
                eval_examples=eval_examples,
                prepared_predict=_riichi_threshold_prepared_predictor(model, threshold),
                policy=_threshold_policy_metadata(
                    target="riichi",
                    base_model=model_name,
                    threshold=threshold,
                    threshold_source=threshold_source,
                ),
                train_prepared=train_prepared,
                eval_prepared=eval_prepared,
            )

    print(f"examples: {len(examples)}")
    print(f"train_examples: {len(train_examples)}")
    print(f"eval_examples: {len(eval_examples)}")
    _print_riichi_benchmark_metrics(model_payloads)
    if parse_failures:
        print(f"parse_failures: {len(parse_failures)}")
    if args.report is not None:
        report = build_riichi_benchmark_report(
            input_paths=args.paths,
            xml_files=dataset_files,
            game=game,
            discard_examples=discard_examples,
            call_examples=call_examples,
            riichi_examples=len(examples),
            split_seed=args.split_seed,
            eval_fraction=args.eval_fraction,
            train_examples=len(train_examples),
            eval_examples=len(eval_examples),
            models=model_payloads,
            parse_failures=parse_failures,
            source=_source_metadata(args),
            game_counts=game_counts,
        )
        report["riichi_examples_total"] = total_examples
        report["example_limit"] = args.example_limit
        if streamed_examples is not None:
            _apply_streaming_report_metadata(report, streamed_examples)
        write_json_report(args.report, report)
        print(f"report_path: {args.report}")
    return 0


RiichiPredictor = Callable[[RiichiExample], ActionKind]
PreparedRiichiPredictor = Callable[[Any], ActionKind]


def _riichi_threshold_predictor(
    model: RiichiLinearModel,
    threshold: float,
) -> RiichiPredictor:
    def predict(example: RiichiExample) -> ActionKind:
        probabilities = model.probabilities_for_example(example)
        score = probabilities.get(ActionKind.RIICHI, -1.0)
        if score >= threshold:
            return ActionKind.RIICHI
        return ActionKind.PASS

    return predict


def _riichi_threshold_prepared_predictor(
    model: RiichiLinearModel,
    threshold: float,
) -> PreparedRiichiPredictor:
    def predict(prepared: Any) -> ActionKind:
        probabilities = model.probabilities_for_prepared(prepared)
        score = probabilities.get(ActionKind.RIICHI, -1.0)
        if score >= threshold:
            return ActionKind.RIICHI
        return ActionKind.PASS

    return predict


def _riichi_model_payload(
    model: RiichiFrequencyBaseline | RiichiLinearModel,
    *,
    train_examples: list[RiichiExample],
    eval_examples: list[RiichiExample],
    predict: RiichiPredictor | None = None,
    prepared_predict: PreparedRiichiPredictor | None = None,
    policy: dict[str, Any] | None = None,
    train_prepared: tuple[Any, ...] | None = None,
    eval_prepared: tuple[Any, ...] | None = None,
) -> dict[str, Any]:
    if isinstance(model, RiichiLinearModel):
        train_prepared = (
            model.prepare_examples(train_examples) if train_prepared is None else train_prepared
        )
        eval_prepared = (
            model.prepare_examples(eval_examples) if eval_prepared is None else eval_prepared
        )
        train_analysis = _summarize_riichi_predictions_from_prepared(
            train_examples,
            train_prepared,
            model=model,
            prepared_predict=prepared_predict,
        )
        eval_analysis = _summarize_riichi_predictions_from_prepared(
            eval_examples,
            eval_prepared,
            model=model,
            prepared_predict=prepared_predict,
        )
    else:
        predictor = model.predict if predict is None else predict
        train_analysis = _summarize_riichi_predictions(train_examples, predictor)
        eval_analysis = _summarize_riichi_predictions(eval_examples, predictor)
    train_metrics = _riichi_metrics(train_analysis)
    eval_metrics = _riichi_metrics(eval_analysis)
    payload: dict[str, Any] = {
        "kind": model.kind,
        "metrics": {
            "loss_kind": "zero_one",
            "train_loss": _zero_one_loss(train_metrics["accuracy"]),
            "eval_loss": _zero_one_loss(eval_metrics["accuracy"]),
            "train_accuracy": train_metrics["accuracy"],
            "eval_accuracy": eval_metrics["accuracy"],
            "train_balanced_accuracy": train_metrics["balanced_accuracy"],
            "eval_balanced_accuracy": eval_metrics["balanced_accuracy"],
            "train_pass_recall": train_metrics["pass_recall"],
            "eval_pass_recall": eval_metrics["pass_recall"],
            "train_riichi_recall": train_metrics["riichi_recall"],
            "eval_riichi_recall": eval_metrics["riichi_recall"],
            "train_action_recall": train_metrics["action_recall"],
            "eval_action_recall": eval_metrics["action_recall"],
        },
        "train_analysis": train_analysis,
        "eval_analysis": eval_analysis,
    }
    if policy is not None:
        payload["policy"] = policy
    if isinstance(model, RiichiLinearModel):
        payload["feature_dim"] = model.feature_dim
        payload["training"] = {
            "epochs": model.epochs,
            "learning_rate": model.learning_rate,
            "l2": model.l2,
            "positive_class_weight": model.positive_class_weight,
        }
        payload["calibration"] = {
            "target": "riichi",
            "thresholds": list(CALIBRATION_THRESHOLDS),
            "train": _riichi_threshold_sweep_prepared(model, train_prepared),
            "eval": _riichi_threshold_sweep_prepared(model, eval_prepared),
        }
    else:
        payload["counts"] = model.count_by_kind()
    return payload


def _print_riichi_benchmark_metrics(model_payloads: dict[str, dict[str, Any]]) -> None:
    for model_name, payload in model_payloads.items():
        metrics = payload["metrics"]
        print(f"{model_name}_train_accuracy: {metrics['train_accuracy']:.4f}")
        print(f"{model_name}_eval_accuracy: {_format_optional_accuracy(metrics['eval_accuracy'])}")
        print(
            f"{model_name}_eval_balanced_accuracy: "
            f"{_format_optional_accuracy(metrics['eval_balanced_accuracy'])}"
        )
        print(
            f"{model_name}_eval_pass_recall: "
            f"{_format_optional_accuracy(metrics['eval_pass_recall'])}"
        )
        print(
            f"{model_name}_eval_riichi_recall: "
            f"{_format_optional_accuracy(metrics['eval_riichi_recall'])}"
        )
        calibration = payload.get("calibration")
        if isinstance(calibration, dict):
            best = _calibration_best(calibration, "eval")
            print(
                f"{model_name}_eval_best_threshold: "
                f"{_format_calibration_best(best, target_name='riichi')}"
            )
        policy = payload.get("policy")
        if isinstance(policy, dict) and policy.get("kind") == "threshold-calibrated-v0":
            print(f"{model_name}_policy_threshold: {float(policy['threshold']):.2f}")


def _summarize_riichi_predictions(
    examples: Sequence[RiichiExample],
    predict: RiichiPredictor,
) -> dict[str, Any]:
    return _summarize_riichi_prediction_results(
        examples,
        (predict(example) for example in examples),
    )


def _summarize_riichi_predictions_from_prepared(
    examples: Sequence[RiichiExample],
    prepared_examples: Sequence[Any],
    *,
    model: RiichiLinearModel,
    prepared_predict: PreparedRiichiPredictor | None,
) -> dict[str, Any]:
    if prepared_predict is None:
        predictions = (model.predict_prepared(prepared) for prepared in prepared_examples)
    else:
        predictions = (prepared_predict(prepared) for prepared in prepared_examples)
    return _summarize_riichi_prediction_results(examples, predictions)


def _summarize_riichi_prediction_results(
    examples: Sequence[RiichiExample],
    predictions: Iterable[ActionKind],
) -> dict[str, Any]:
    buckets: dict[str, Any] = {
        "overall": _empty_call_bucket(),
        "by_actual_action": {kind.value: _empty_call_bucket() for kind in RIICHI_DECISION_KINDS},
    }
    action_distribution = {kind.value: 0 for kind in RIICHI_DECISION_KINDS}

    for example, prediction in zip(examples, predictions, strict=True):
        actual = example.action.kind
        correct = prediction == actual
        action_distribution[actual.value] += 1
        _record_call_bucket(buckets["overall"], correct)
        _record_call_bucket(buckets["by_actual_action"][actual.value], correct)

    return {
        "action_distribution": action_distribution,
        **_finalize_call_buckets(buckets),
    }


def _riichi_metrics(analysis: dict[str, Any]) -> dict[str, Any]:
    action_recall = {
        kind.value: analysis["by_actual_action"][kind.value]["accuracy"]
        for kind in RIICHI_DECISION_KINDS
    }
    pass_recall = action_recall[ActionKind.PASS.value]
    riichi_recall = action_recall[ActionKind.RIICHI.value]
    return {
        "accuracy": analysis["overall"]["accuracy"],
        "balanced_accuracy": _mean_defined((pass_recall, riichi_recall)),
        "pass_recall": pass_recall,
        "riichi_recall": riichi_recall,
        "action_recall": action_recall,
    }


def _riichi_threshold_sweep(
    model: RiichiLinearModel,
    examples: Sequence[RiichiExample],
) -> dict[str, Any]:
    return _riichi_threshold_sweep_prepared(model, model.prepare_examples(examples))


def _riichi_threshold_sweep_prepared(
    model: RiichiLinearModel,
    prepared_examples: Sequence[Any],
) -> dict[str, Any]:
    records = []
    for prepared in prepared_examples:
        probabilities = model.probabilities_for_prepared(prepared)
        records.append(
            {
                "score": probabilities.get(ActionKind.RIICHI, 0.0),
                "actual_positive": prepared.target == ActionKind.RIICHI,
            }
        )
    return _binary_threshold_sweep(records, target_name="riichi")


def _legal_call_key(example: CallExample) -> str:
    if not example.legal_call_kinds:
        return "none"
    return ",".join(kind.value for kind in example.legal_call_kinds)


def _empty_call_bucket() -> dict[str, int | float | None]:
    return {
        "examples": 0,
        "correct": 0,
        "accuracy": None,
    }


def _record_call_bucket(bucket: dict[str, int | float | None], correct: bool) -> None:
    bucket["examples"] = int(bucket["examples"] or 0) + 1
    bucket["correct"] = int(bucket["correct"] or 0) + int(correct)


def _finalize_call_buckets(value: Any) -> Any:
    if _is_call_bucket(value):
        examples = int(value["examples"])
        correct = int(value["correct"])
        return {
            "examples": examples,
            "correct": correct,
            "accuracy": None if examples == 0 else correct / examples,
        }
    if isinstance(value, dict):
        return {key: _finalize_call_buckets(child) for key, child in value.items()}
    return value


def _is_call_bucket(value: Any) -> bool:
    return isinstance(value, dict) and set(value) == {"examples", "correct", "accuracy"}


def _build_disagreement_report(
    examples: Sequence[DiscardExample],
    *,
    linear_models: dict[str, DiscardLinearModel],
    max_per_category: int,
) -> dict[str, object]:
    categories: dict[str, dict[str, object]] = {
        "risk_correct_defense_wrong": {"count": 0, "items": []},
        "risk_correct_defense_v1_wrong": {"count": 0, "items": []},
        "defense_correct_risk_wrong": {"count": 0, "items": []},
        "defense_v1_correct_risk_wrong": {"count": 0, "items": []},
    }
    model_names = (
        "risk_context_linear",
        "defense_context_linear",
        "defense_context_v1_linear",
    )

    for example in examples:
        if example.action.tile is None:
            raise ValueError("discard examples must have tile actions")
        logits_by_model = {
            model_name: linear_models[model_name].logits_for_example(example)
            for model_name in model_names
        }
        predictions = {
            model_name: max(logits, key=logits.get)
            for model_name, logits in logits_by_model.items()
        }
        correct = {
            model_name: prediction == example.action.tile
            for model_name, prediction in predictions.items()
        }
        record: dict[str, object] | None = None
        category_matches = {
            "risk_correct_defense_wrong": (
                correct["risk_context_linear"] and not correct["defense_context_linear"]
            ),
            "risk_correct_defense_v1_wrong": (
                correct["risk_context_linear"] and not correct["defense_context_v1_linear"]
            ),
            "defense_correct_risk_wrong": (
                correct["defense_context_linear"] and not correct["risk_context_linear"]
            ),
            "defense_v1_correct_risk_wrong": (
                correct["defense_context_v1_linear"] and not correct["risk_context_linear"]
            ),
        }
        for category, matches in category_matches.items():
            if not matches:
                continue
            payload = categories[category]
            payload["count"] = int(payload["count"]) + 1
            items = payload["items"]
            assert isinstance(items, list)
            if len(items) < max_per_category:
                if record is None:
                    record = _disagreement_record(
                        example,
                        predictions=predictions,
                        correct=correct,
                        logits_by_model=logits_by_model,
                    )
                items.append(record)

    return {
        "kind": "kenjaku-discard-disagreements-v0",
        "examples": len(examples),
        "max_per_category": max_per_category,
        "categories": categories,
    }


def _disagreement_record(
    example: DiscardExample,
    *,
    predictions: dict[str, TileType],
    correct: dict[str, bool],
    logits_by_model: dict[str, dict[TileType, float]],
) -> dict[str, object]:
    if example.action.tile is None:
        raise ValueError("discard examples must have tile actions")
    shanten_delta = discard_shanten_delta(example)
    return {
        "round_index": example.round_index,
        "event_index": example.event_index,
        "seat": example.seat,
        "dealer": example.dealer,
        "scores": list(example.scores),
        "actual_discard": example.action.tile.notation,
        "predictions": {
            model_name: tile_type.notation for model_name, tile_type in predictions.items()
        },
        "correct": correct,
        "shanten_delta": {
            "before": shanten_delta.before,
            "after": shanten_delta.after,
            "delta": shanten_delta.delta,
        },
        "defense_buckets": _actual_discard_defense_buckets(example),
        "defense_risk": {
            "actual_discard": _defense_risk_payload(
                candidate_defense_risk(example, example.action.tile)
            ),
            "predictions": {
                model_name: _defense_risk_payload(candidate_defense_risk(example, tile_type))
                for model_name, tile_type in predictions.items()
            },
        },
        "hand_counts": _tile_count_payload(example.hand_counts),
        "visible_counts": _tile_count_payload(example.visible_counts),
        "active_riichi_seats": list(example.active_riichi_seats),
        "riichi_declared_turns": list(example.riichi_declared_turns),
        "rivers_by_seat": _tiles_by_seat_payload(example.rivers_by_seat),
        "meld_tiles_by_seat": _tiles_by_seat_payload(example.meld_tiles_by_seat),
        "dora_indicators": _tile_payload(example.dora_indicators),
        "last_discard_tsumogiri_by_seat": list(example.last_discard_tsumogiri_by_seat),
        "ippatsu_active_seats": list(example.ippatsu_active_seats),
        "candidate_logits": {
            model_name: _logits_payload(logits) for model_name, logits in logits_by_model.items()
        },
    }


def _actual_discard_defense_buckets(example: DiscardExample) -> dict[str, bool]:
    return {
        "active_riichi_opponent": has_active_riichi_opponent(example),
        "genbutsu": actual_discard_is_genbutsu(example),
        "suji": actual_discard_has_suji(example),
        "kabe": actual_discard_has_kabe(example),
        "one_chance": actual_discard_has_one_chance(example),
        "seen_before_riichi": actual_discard_seen_before_riichi(example),
        "seen_after_riichi": actual_discard_seen_after_riichi(example),
    }


def _defense_risk_payload(score: Any) -> dict[str, object]:
    return {
        "tile": score.tile.notation,
        "risk": score.risk,
        "calibrated_probability": score.calibrated_probability,
        "active_riichi_opponents": score.active_riichi_opponents,
        "safety_reasons": list(score.safety_reasons),
        "danger_reasons": list(score.danger_reasons),
    }


def _tile_count_payload(counts: tuple[int, ...]) -> list[dict[str, int | str]]:
    return [
        {
            "tile": TileType(index).notation,
            "count": count,
        }
        for index, count in enumerate(counts)
        if count
    ]


def _tiles_by_seat_payload(rivers_by_seat: tuple[tuple[Tile, ...], ...]) -> list[list[str]]:
    return [_tile_payload(tiles) for tiles in rivers_by_seat]


def _tile_payload(tiles: tuple[Tile, ...]) -> list[str]:
    return [tile.notation for tile in tiles]


def _logits_payload(logits: dict[TileType, float]) -> list[dict[str, float | str]]:
    return [
        {
            "tile": tile_type.notation,
            "logit": logits[tile_type],
        }
        for tile_type in sorted(logits)
    ]


def _add_parse_cache_arg(parser: argparse.ArgumentParser) -> None:
    parser.add_argument(
        "--parse-cache",
        type=Path,
        help="directory for opt-in content-addressed Tenhou XML parse cache",
    )
    parser.add_argument(
        "--jobs",
        type=int,
        default=1,
        help="parallel Tenhou XML parse worker processes",
    )


def _parse_tenhou_dataset_from_args(args: argparse.Namespace) -> TenhouDataset:
    return parse_tenhou_xml_dataset(
        args.paths,
        skip_errors=args.skip_errors,
        parse_cache_dir=_parse_cache_dir(args),
        jobs=_parse_jobs(args),
    )


def _parse_cache_dir(args: argparse.Namespace) -> Path | None:
    return getattr(args, "parse_cache", None)


def _parse_jobs(args: argparse.Namespace) -> int:
    return int(getattr(args, "jobs", 1))


def _add_source_args(parser: argparse.ArgumentParser) -> None:
    parser.add_argument(
        "--source-label",
        help="human-readable source label recorded in JSON reports",
    )
    parser.add_argument(
        "--source-command",
        help="local data command or manifest reference recorded in JSON reports",
    )
    parser.add_argument(
        "--source-date",
        help="source date or date range recorded in JSON reports",
    )


def _source_metadata(args: argparse.Namespace) -> dict[str, str | None]:
    return {
        "label": args.source_label,
        "command": args.source_command,
        "date": args.source_date,
    }


def _parse_failures_payload(failures: Sequence[TenhouParseFailure]) -> dict[str, Any]:
    return {
        "count": len(failures),
        "items": [
            {
                "path": str(failure.path),
                "error_type": failure.error_type,
                "message": failure.message,
            }
            for failure in failures
        ],
    }


def _validated_probability(value: float, option: str) -> float:
    if value < 0.0 or value > 1.0:
        raise SystemExit(f"{option} must be between 0.0 and 1.0")
    return value


def _validated_positive_float(value: float, option: str) -> float:
    if value <= 0.0:
        raise SystemExit(f"{option} must be positive")
    return value


def _format_optional_accuracy(accuracy: float | None) -> str:
    return "n/a" if accuracy is None else f"{accuracy:.4f}"


def _format_optional_float(value: Any) -> str:
    return "n/a" if value is None else f"{float(value):.4f}"


def _format_optional_delta(delta: float | None) -> str:
    return "n/a" if delta is None else f"{delta:+.4f}"


def _optional_delta(left: float | None, right: float | None) -> float | None:
    if left is None or right is None:
        return None
    return left - right
