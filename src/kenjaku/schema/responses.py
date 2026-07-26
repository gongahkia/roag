from __future__ import annotations

import json
from collections.abc import Mapping
from dataclasses import dataclass
from typing import Any, ClassVar, Self


@dataclass(frozen=True, slots=True)
class ResponsePayload:
    payload: dict[str, Any]

    KIND: ClassVar[str] = ""

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        if cls.KIND and payload.get("kind") != cls.KIND:
            raise ValueError(f"response kind must be {cls.KIND}")
        return cls(dict(payload))

    def to_dict(self) -> dict[str, Any]:
        return dict(self.payload)

    def to_json(self, *, sort_keys: bool = True, indent: int | None = None) -> str:
        return json.dumps(self.to_dict(), indent=indent, sort_keys=sort_keys)


class StatusResponse(ResponsePayload):
    KIND = "kenjaku-status-v0"


class ReproReportResponse(ResponsePayload):
    KIND = "kenjaku-repro-report-v0"


class BrowserGameResponse(ResponsePayload):
    KIND = "kenjaku-browser-game-v0"


class TrainingDashboardResponse(ResponsePayload):
    KIND = "kenjaku-training-dashboard-v0"


class DemoManifestResponse(ResponsePayload):
    KIND = "kenjaku-demo-manifest-v0"


class ReplayIntakeReviewResponse(ResponsePayload):
    KIND = "kenjaku-replay-intake-review-v0"


class ReplaySharePlanResponse(ResponsePayload):
    KIND = "kenjaku-replay-share-plan-v0"


class ReplayPublicSummaryResponse(ResponsePayload):
    KIND = "kenjaku-replay-public-summary-v0"


class SelfPlaySandboxResponse(ResponsePayload):
    KIND = "kenjaku-self-play-sandbox-report-v0"


class SelfPlayMatchSandboxResponse(ResponsePayload):
    KIND = "kenjaku-self-play-match-report-v0"


class PpoSandboxTrainingResponse(ResponsePayload):
    KIND = "kenjaku-ppo-sandbox-report-v0"


class PopulationSandboxTrainingResponse(ResponsePayload):
    KIND = "kenjaku-population-sandbox-report-v0"


class TenhouInspectResponse(ResponsePayload):
    KIND = "kenjaku-tenhou-inspect-report-v0"


class DefenseRiskSummaryResponse(ResponsePayload):
    KIND = "kenjaku-defense-risk-summary-v0"


class SafetyAdvisorResponse(ResponsePayload):
    KIND = "kenjaku-safety-advisor-v0"


class BenchmarkDealInResponse(ResponsePayload):
    KIND = "kenjaku-deal-in-benchmark-report-v0"


class PlacementTrainingResponse(ResponsePayload):
    KIND = "kenjaku-placement-training-report-v0"


class PlacementProbabilityResponse(ResponsePayload):
    KIND = "kenjaku-placement-probability-v0"


class AnalyzeHandResponse(ResponsePayload):
    KIND = "kenjaku-hand-analysis-v0"


class DecisionSnapshotSummaryResponse(ResponsePayload):
    KIND = "kenjaku-decision-snapshot-summary-v0"


class InterpretabilityOverlayResponse(ResponsePayload):
    KIND = "kenjaku-interpretability-overlay-v0"


class ReviewGameResponse(ResponsePayload):
    KIND = "kenjaku-review-game-v0"


class TransformerAttentionOverlayResponse(ResponsePayload):
    KIND = "kenjaku-transformer-attention-overlay-v0"


class FeatureImportanceResponse(ResponsePayload):
    KIND = "kenjaku-feature-importance-v0"


class DecisionSnapshotComparisonResponse(ResponsePayload):
    KIND = "kenjaku-decision-snapshot-comparison-v0"


class ExternalBaselineResponse(ResponsePayload):
    KIND = "kenjaku-external-baseline-report-v0"


class DiscardLinearTrainingResponse(ResponsePayload):
    KIND = "kenjaku-discard-linear-report-v0"


class DiscardMlpTrainingResponse(ResponsePayload):
    KIND = "kenjaku-discard-mlp-report-v0"


class DiscardTransformerTrainingResponse(ResponsePayload):
    KIND = "kenjaku-discard-transformer-report-v0"


class DiscardBenchmarkResponse(ResponsePayload):
    KIND = "kenjaku-discard-benchmark-report-v0"


class BenchmarkDiscardResponse(DiscardBenchmarkResponse):
    pass


class DiscardMlpBenchmarkResponse(ResponsePayload):
    KIND = "kenjaku-discard-mlp-benchmark-report-v0"


class DiscardTransformerBenchmarkResponse(ResponsePayload):
    KIND = "kenjaku-discard-transformer-benchmark-report-v0"


class BenchmarkSummaryResponse(ResponsePayload):
    KIND = "kenjaku-benchmark-summary-v0"


class DiscardBenchmarkSummaryResponse(ResponsePayload):
    KIND = "kenjaku-discard-benchmark-summary-v0"


class PublicBenchmarkDashboardResponse(ResponsePayload):
    KIND = "kenjaku-public-benchmark-dashboard-v0"


class DiscardDisagreementSummaryResponse(ResponsePayload):
    KIND = "kenjaku-discard-disagreement-summary-v0"


class DiscardDisagreementReportResponse(ResponsePayload):
    KIND = "kenjaku-discard-disagreements-v0"


class CallBenchmarkResponse(ResponsePayload):
    KIND = "kenjaku-call-benchmark-report-v0"


class RiichiBenchmarkResponse(ResponsePayload):
    KIND = "kenjaku-riichi-benchmark-report-v0"


_RESPONSE_TYPES: dict[str, type[ResponsePayload]] = {
    response_type.KIND: response_type
    for response_type in (
        StatusResponse,
        ReproReportResponse,
        BrowserGameResponse,
        TrainingDashboardResponse,
        DemoManifestResponse,
        ReplayIntakeReviewResponse,
        ReplaySharePlanResponse,
        ReplayPublicSummaryResponse,
        SelfPlaySandboxResponse,
        SelfPlayMatchSandboxResponse,
        PpoSandboxTrainingResponse,
        PopulationSandboxTrainingResponse,
        TenhouInspectResponse,
        DefenseRiskSummaryResponse,
        SafetyAdvisorResponse,
        BenchmarkDealInResponse,
        PlacementTrainingResponse,
        PlacementProbabilityResponse,
        AnalyzeHandResponse,
        DecisionSnapshotSummaryResponse,
        InterpretabilityOverlayResponse,
        ReviewGameResponse,
        TransformerAttentionOverlayResponse,
        FeatureImportanceResponse,
        DecisionSnapshotComparisonResponse,
        ExternalBaselineResponse,
        DiscardLinearTrainingResponse,
        DiscardMlpTrainingResponse,
        DiscardTransformerTrainingResponse,
        DiscardBenchmarkResponse,
        DiscardMlpBenchmarkResponse,
        DiscardTransformerBenchmarkResponse,
        BenchmarkSummaryResponse,
        DiscardBenchmarkSummaryResponse,
        PublicBenchmarkDashboardResponse,
        DiscardDisagreementSummaryResponse,
        DiscardDisagreementReportResponse,
        CallBenchmarkResponse,
        RiichiBenchmarkResponse,
    )
}


def response_from_dict(payload: Mapping[str, Any]) -> ResponsePayload:
    kind = payload.get("kind")
    response_type = _RESPONSE_TYPES.get(kind) if isinstance(kind, str) else None
    if response_type is None:
        return ResponsePayload.from_dict(payload)
    return response_type.from_dict(payload)
