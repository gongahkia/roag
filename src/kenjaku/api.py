"""Stable public API for embedding Kenjaku parsing, examples, models, and reports."""

from __future__ import annotations

import json
from collections.abc import Iterable, Sequence
from pathlib import Path
from typing import Any

import kenjaku.io as _io
import kenjaku.models as _models
import kenjaku.training as _training
import kenjaku.training.decision_snapshots as _decision_snapshots
import kenjaku.training.interpretability_overlay as _interpretability_overlay
from kenjaku.artifact_registry import LocalArtifactRegistry as _LocalArtifactRegistry
from kenjaku.schema import ActionV1 as _ActionV1
from kenjaku.schema import CheckpointManifestV1 as _CheckpointManifestV1
from kenjaku.schema import DecisionResultV1 as _DecisionResultV1
from kenjaku.schema import LegalActionMaskV1 as _LegalActionMaskV1
from kenjaku.schema import ObservationV1 as _ObservationV1

TenhouGame = _io.TenhouGame  # stable since 0.2.0
TenhouParseFailure = _io.TenhouParseFailure  # stable since 0.2.0
DiscardExample = _training.DiscardExample  # stable since 0.2.0
CallExample = _training.CallExample  # stable since 0.2.0
RiichiExample = _training.RiichiExample  # stable since 0.2.0
iter_discard_examples = _training.iter_discard_examples  # stable since 0.2.0
iter_call_examples = _training.iter_call_examples  # stable since 0.2.0
iter_riichi_examples = _training.iter_riichi_examples  # stable since 0.2.0
RISK_CONTEXT_FEATURE_PROFILE = _models.RISK_CONTEXT_FEATURE_PROFILE  # stable since 0.2.0
DEFENSE_CONTEXT_FEATURE_PROFILE = _models.DEFENSE_CONTEXT_FEATURE_PROFILE  # stable since 0.2.0
SHANTEN_FEATURE_PROFILE = _models.SHANTEN_FEATURE_PROFILE  # stable since 0.2.0
RAW_COUNT_FEATURE_PROFILE = _models.RAW_COUNT_FEATURE_PROFILE  # stable since 0.2.0
CALL_LINEAR_V1_FEATURE_PROFILE = _models.CALL_LINEAR_V1_FEATURE_PROFILE  # stable since 0.2.0
DiscardLinearModel = _models.DiscardLinearModel  # stable since 0.2.0
CallLinearModel = _models.CallLinearModel  # stable since 0.2.0
RiichiLinearModel = _models.RiichiLinearModel  # stable since 0.2.0
DealInLinearModel = _models.DealInLinearModel  # stable since 0.2.0
DiscardFrequencyBaseline = _models.DiscardFrequencyBaseline  # stable since 0.2.0
RiichiFrequencyBaseline = _models.RiichiFrequencyBaseline  # stable since 0.2.0
CallFrequencyBaseline = _models.CallFrequencyBaseline  # stable since 0.2.0
ActionV1 = _ActionV1  # stable since 0.2.0
CheckpointManifestV1 = _CheckpointManifestV1  # stable since 0.2.0
DecisionResultV1 = _DecisionResultV1  # stable since 0.2.0
LegalActionMaskV1 = _LegalActionMaskV1  # stable since 0.2.0
LocalArtifactRegistry = _LocalArtifactRegistry  # stable since 0.2.0
ObservationV1 = _ObservationV1  # stable since 0.2.0


def parse_tenhou_xml_file(path: str | Path) -> TenhouGame:  # stable since 0.2.0
    """Parse one Tenhou XML file into a TenhouGame."""
    return _io.parse_tenhou_xml_file(path)


def parse_tenhou_xml_dataset(  # stable since 0.2.0
    paths: Sequence[str | Path],
    *,
    skip_errors: bool = False,
    parse_cache_dir: str | Path | None = None,
    jobs: int = 1,
) -> _io.TenhouDataset:
    """Parse Tenhou XML files into one dataset object."""
    return _io.parse_tenhou_xml_dataset(
        paths,
        skip_errors=skip_errors,
        parse_cache_dir=parse_cache_dir,
        jobs=jobs,
    )


def build_interpretability_overlay(  # stable since 0.2.0
    snapshots: Iterable[dict[str, Any]],
    *,
    title: str = "Kenjaku Interpretability Overlay",
    min_decisions: int = 0,
) -> dict[str, Any]:
    """Build an in-memory discard interpretability report."""
    return _interpretability_overlay.build_interpretability_overlay(
        snapshots,
        title=title,
        min_decisions=min_decisions,
    )


API_EXPORT_DOCS = {
    "parse_tenhou_xml_file": "Parse one Tenhou XML file into a TenhouGame.",
    "parse_tenhou_xml_dataset": "Parse a sequence of Tenhou XML paths into one dataset object.",
    "TenhouGame": "Parsed Tenhou game container.",
    "TenhouParseFailure": "Parse failure record for tolerant dataset loading.",
    "DiscardExample": "Reconstructed supervised discard decision example.",
    "CallExample": "Reconstructed supervised call/pass decision example.",
    "RiichiExample": "Reconstructed supervised riichi/pass decision example.",
    "iter_discard_examples": "Yield discard examples from a parsed game.",
    "iter_call_examples": "Yield call/pass examples from a parsed game.",
    "iter_riichi_examples": "Yield riichi/pass examples from a parsed game.",
    "RISK_CONTEXT_FEATURE_PROFILE": "Risk-context discard linear feature profile.",
    "DEFENSE_CONTEXT_FEATURE_PROFILE": "Defense-context discard linear feature profile.",
    "SHANTEN_FEATURE_PROFILE": "Shanten-aware discard linear feature profile.",
    "RAW_COUNT_FEATURE_PROFILE": "Raw-count discard linear feature profile.",
    "CALL_LINEAR_V1_FEATURE_PROFILE": "Additive call/pass linear feature profile.",
    "DiscardLinearModel": "Dependency-free linear discard classifier.",
    "CallLinearModel": "Dependency-free linear call/pass classifier.",
    "RiichiLinearModel": "Dependency-free linear riichi/pass classifier.",
    "DealInLinearModel": "Dependency-free deal-in probability estimator.",
    "DiscardFrequencyBaseline": "Frequency baseline for discard decisions.",
    "RiichiFrequencyBaseline": "Frequency baseline for riichi/pass decisions.",
    "CallFrequencyBaseline": "Frequency baseline for call/pass decisions.",
    "ActionV1": "Versioned ruleset-specific policy action.",
    "CheckpointManifestV1": "Versioned checkpoint identity and compatibility contract.",
    "DecisionResultV1": "Versioned selected action with structured rationale.",
    "LegalActionMaskV1": "Versioned shared fixed-width legal-action mask.",
    "LocalArtifactRegistry": "Local-only checkpoint, report, and ONNX artifact registry.",
    "ObservationV1": "Versioned actor-private/public-table policy observation.",
    "export_decision_snapshots": "Build decision snapshots and optionally write JSONL.",
    "load_decision_snapshots": "Load decision snapshot JSONL rows.",
    "build_interpretability_overlay": "Build an in-memory discard interpretability report.",
}


def export_decision_snapshots(  # stable since 0.2.0
    game: TenhouGame,
    path: str | Path | None = None,
    *,
    decision_types: Sequence[str] = _decision_snapshots.DECISION_SNAPSHOT_TYPES,
    limit: int | None = None,
    source: dict[str, str | None] | None = None,
    input_paths: Sequence[Path] = (),
    xml_file_count: int | None = None,
    include_outcome: bool = False,
) -> list[dict[str, Any]]:
    """Build decision snapshots and optionally write them to JSONL."""
    snapshots = _decision_snapshots.build_decision_snapshots(
        game,
        decision_types=decision_types,
        limit=limit,
        source=source,
        input_paths=input_paths,
        xml_file_count=xml_file_count,
        include_outcome=include_outcome,
    )
    if path is not None:
        _decision_snapshots.write_decision_snapshots_jsonl(path, snapshots)
    return snapshots


def load_decision_snapshots(path: str | Path) -> list[dict[str, Any]]:  # stable since 0.2.0
    """Load decision snapshots from a JSONL file."""
    snapshots: list[dict[str, Any]] = []
    with Path(path).open(encoding="utf-8") as handle:
        for line in handle:
            if line.strip():
                snapshots.append(json.loads(line))
    return snapshots


__all__ = [
    "parse_tenhou_xml_file",
    "parse_tenhou_xml_dataset",
    "TenhouGame",
    "TenhouParseFailure",
    "DiscardExample",
    "CallExample",
    "RiichiExample",
    "iter_discard_examples",
    "iter_call_examples",
    "iter_riichi_examples",
    "RISK_CONTEXT_FEATURE_PROFILE",
    "DEFENSE_CONTEXT_FEATURE_PROFILE",
    "SHANTEN_FEATURE_PROFILE",
    "RAW_COUNT_FEATURE_PROFILE",
    "CALL_LINEAR_V1_FEATURE_PROFILE",
    "DiscardLinearModel",
    "CallLinearModel",
    "RiichiLinearModel",
    "DealInLinearModel",
    "DiscardFrequencyBaseline",
    "RiichiFrequencyBaseline",
    "CallFrequencyBaseline",
    "ActionV1",
    "CheckpointManifestV1",
    "DecisionResultV1",
    "LegalActionMaskV1",
    "LocalArtifactRegistry",
    "ObservationV1",
    "export_decision_snapshots",
    "load_decision_snapshots",
    "build_interpretability_overlay",
]
