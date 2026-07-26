from __future__ import annotations

import contextlib
import io
import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.browser_game import BROWSER_GAME_KIND
from kenjaku.cli import main
from kenjaku.experiments import (
    BENCHMARK_SUMMARY_KIND,
    CALL_BENCHMARK_REPORT_KIND,
    DEAL_IN_BENCHMARK_REPORT_KIND,
    DISCARD_BENCHMARK_REPORT_KIND,
    DISCARD_BENCHMARK_SUMMARY_KIND,
    DISCARD_DISAGREEMENT_REPORT_KIND,
    DISCARD_DISAGREEMENT_SUMMARY_KIND,
    DISCARD_LINEAR_REPORT_KIND,
    DISCARD_MLP_BENCHMARK_REPORT_KIND,
    DISCARD_MLP_REPORT_KIND,
    DISCARD_TRANSFORMER_BENCHMARK_REPORT_KIND,
    DISCARD_TRANSFORMER_REPORT_KIND,
    PUBLIC_BENCHMARK_DASHBOARD_KIND,
    RIICHI_BENCHMARK_REPORT_KIND,
    TENHOU_INSPECT_REPORT_KIND,
    write_json_report,
)
from kenjaku.hand_analysis import ANALYZE_HAND_KIND
from kenjaku.io.replay_manifest import (
    REPLAY_INTAKE_REVIEW_KIND,
    REPLAY_PUBLIC_SUMMARY_KIND,
    REPLAY_SHARE_PLAN_KIND,
)
from kenjaku.repro_report import REPRO_REPORT_KIND
from kenjaku.review_game import REVIEW_GAME_REPORT_KIND
from kenjaku.safety_advisor import SAFETY_ADVISOR_KIND
from kenjaku.schema import (
    AnalyzeHandResponse,
    BenchmarkDealInResponse,
    BenchmarkSummaryResponse,
    BrowserGameResponse,
    CallBenchmarkResponse,
    DecisionSnapshotComparisonResponse,
    DecisionSnapshotSummaryResponse,
    DefenseRiskSummaryResponse,
    DemoManifestResponse,
    DiscardBenchmarkResponse,
    DiscardBenchmarkSummaryResponse,
    DiscardDisagreementReportResponse,
    DiscardDisagreementSummaryResponse,
    DiscardLinearTrainingResponse,
    DiscardMlpBenchmarkResponse,
    DiscardMlpTrainingResponse,
    DiscardTransformerBenchmarkResponse,
    DiscardTransformerTrainingResponse,
    ExternalBaselineResponse,
    FeatureImportanceResponse,
    InterpretabilityOverlayResponse,
    PlacementProbabilityResponse,
    PlacementTrainingResponse,
    PopulationSandboxTrainingResponse,
    PpoSandboxTrainingResponse,
    PublicBenchmarkDashboardResponse,
    ReplayIntakeReviewResponse,
    ReplayPublicSummaryResponse,
    ReplaySharePlanResponse,
    ReproReportResponse,
    ResponsePayload,
    ReviewGameResponse,
    RiichiBenchmarkResponse,
    SafetyAdvisorResponse,
    SelfPlayMatchSandboxResponse,
    SelfPlaySandboxResponse,
    StatusResponse,
    TenhouInspectResponse,
    TrainingDashboardResponse,
    TransformerAttentionOverlayResponse,
    response_from_dict,
)
from kenjaku.simulation import SELF_PLAY_MATCH_REPORT_KIND, SELF_PLAY_SANDBOX_REPORT_KIND
from kenjaku.status import STATUS_KIND, build_status_payload
from kenjaku.training.external_baselines import EXTERNAL_BASELINE_REPORT_KIND
from kenjaku.training.interpretability_overlay import INTERPRETABILITY_OVERLAY_KIND
from kenjaku.training.population import POPULATION_SANDBOX_REPORT_KIND
from kenjaku.training.ppo import PPO_SANDBOX_REPORT_KIND
from kenjaku.training_dashboard import TRAINING_DASHBOARD_KIND

RESPONSE_CASES = (
    (StatusResponse, STATUS_KIND),
    (ReproReportResponse, REPRO_REPORT_KIND),
    (BrowserGameResponse, BROWSER_GAME_KIND),
    (TrainingDashboardResponse, TRAINING_DASHBOARD_KIND),
    (DemoManifestResponse, "kenjaku-demo-manifest-v0"),
    (ReplayIntakeReviewResponse, REPLAY_INTAKE_REVIEW_KIND),
    (ReplaySharePlanResponse, REPLAY_SHARE_PLAN_KIND),
    (ReplayPublicSummaryResponse, REPLAY_PUBLIC_SUMMARY_KIND),
    (SelfPlaySandboxResponse, SELF_PLAY_SANDBOX_REPORT_KIND),
    (SelfPlayMatchSandboxResponse, SELF_PLAY_MATCH_REPORT_KIND),
    (PpoSandboxTrainingResponse, PPO_SANDBOX_REPORT_KIND),
    (PopulationSandboxTrainingResponse, POPULATION_SANDBOX_REPORT_KIND),
    (TenhouInspectResponse, TENHOU_INSPECT_REPORT_KIND),
    (DefenseRiskSummaryResponse, "kenjaku-defense-risk-summary-v0"),
    (SafetyAdvisorResponse, SAFETY_ADVISOR_KIND),
    (BenchmarkDealInResponse, DEAL_IN_BENCHMARK_REPORT_KIND),
    (PlacementTrainingResponse, "kenjaku-placement-training-report-v0"),
    (PlacementProbabilityResponse, "kenjaku-placement-probability-v0"),
    (AnalyzeHandResponse, ANALYZE_HAND_KIND),
    (DecisionSnapshotSummaryResponse, "kenjaku-decision-snapshot-summary-v0"),
    (InterpretabilityOverlayResponse, INTERPRETABILITY_OVERLAY_KIND),
    (ReviewGameResponse, REVIEW_GAME_REPORT_KIND),
    (TransformerAttentionOverlayResponse, "kenjaku-transformer-attention-overlay-v0"),
    (FeatureImportanceResponse, "kenjaku-feature-importance-v0"),
    (DecisionSnapshotComparisonResponse, "kenjaku-decision-snapshot-comparison-v0"),
    (ExternalBaselineResponse, EXTERNAL_BASELINE_REPORT_KIND),
    (DiscardLinearTrainingResponse, DISCARD_LINEAR_REPORT_KIND),
    (DiscardMlpTrainingResponse, DISCARD_MLP_REPORT_KIND),
    (DiscardTransformerTrainingResponse, DISCARD_TRANSFORMER_REPORT_KIND),
    (DiscardBenchmarkResponse, DISCARD_BENCHMARK_REPORT_KIND),
    (DiscardMlpBenchmarkResponse, DISCARD_MLP_BENCHMARK_REPORT_KIND),
    (DiscardTransformerBenchmarkResponse, DISCARD_TRANSFORMER_BENCHMARK_REPORT_KIND),
    (BenchmarkSummaryResponse, BENCHMARK_SUMMARY_KIND),
    (DiscardBenchmarkSummaryResponse, DISCARD_BENCHMARK_SUMMARY_KIND),
    (PublicBenchmarkDashboardResponse, PUBLIC_BENCHMARK_DASHBOARD_KIND),
    (DiscardDisagreementSummaryResponse, DISCARD_DISAGREEMENT_SUMMARY_KIND),
    (DiscardDisagreementReportResponse, DISCARD_DISAGREEMENT_REPORT_KIND),
    (CallBenchmarkResponse, CALL_BENCHMARK_REPORT_KIND),
    (RiichiBenchmarkResponse, RIICHI_BENCHMARK_REPORT_KIND),
)


class ResponseSchemaTests(unittest.TestCase):
    def test_response_kinds_match_existing_constants(self) -> None:
        for response_type, expected_kind in RESPONSE_CASES:
            with self.subTest(response_type=response_type.__name__):
                self.assertEqual(response_type.KIND, expected_kind)

    def test_from_dict_roundtrips_existing_shape(self) -> None:
        for response_type, _expected_kind in RESPONSE_CASES:
            with self.subTest(response_type=response_type.__name__):
                payload = {
                    "kind": response_type.KIND,
                    "z": [3, {"x": None}],
                    "a": True,
                }
                response = response_type.from_dict(payload)

                self.assertIsInstance(response_from_dict(payload), response_type)
                self.assertEqual(response.to_dict(), payload)
                self.assertEqual(
                    response.to_json(sort_keys=True),
                    json.dumps(payload, sort_keys=True),
                )
                self.assertEqual(
                    response.to_json(indent=2, sort_keys=True),
                    json.dumps(payload, indent=2, sort_keys=True),
                )

    def test_from_dict_rejects_wrong_kind(self) -> None:
        with self.assertRaisesRegex(ValueError, STATUS_KIND):
            StatusResponse.from_dict({"kind": "other"})

    def test_unknown_kind_uses_generic_response(self) -> None:
        payload = {"kind": "local-unit-v0", "value": 1}
        response = response_from_dict(payload)

        self.assertIs(type(response), ResponsePayload)
        self.assertEqual(response.to_dict(), payload)

    def test_write_json_report_accepts_response_payload(self) -> None:
        with TemporaryDirectory() as directory:
            path = Path(directory) / "nested" / "report.json"
            payload = {"kind": STATUS_KIND, "value": 1}
            write_json_report(path, StatusResponse.from_dict(payload))

            written = json.loads(path.read_text(encoding="utf-8"))

        written.pop("provenance")
        self.assertEqual(written, payload)

    def test_cli_status_json_keeps_legacy_dump_shape(self) -> None:
        stdout = io.StringIO()
        with contextlib.redirect_stdout(stdout):
            exit_code = main(["status", "--json"])

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            stdout.getvalue(),
            json.dumps(build_status_payload(), indent=2, sort_keys=True) + "\n",
        )

    def test_cli_uses_response_emitters_for_json_boundaries(self) -> None:
        source = Path("src/kenjaku/cli.py").read_text(encoding="utf-8")

        self.assertNotIn("print(json.dumps(", source)
        self.assertNotIn("write_json_report(", source)


if __name__ == "__main__":
    unittest.main()
