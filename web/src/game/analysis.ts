import { actionKey, actionLabel, legalActions } from "./engine";
import { decideAction } from "./policy";
import type { GameAction, GameState, PolicyArtifact } from "./types";

export type ReviewSeverity = "best" | "inaccuracy" | "mistake" | "blunder";

export interface DecisionReview {
  readonly frameIndex: number;
  readonly version: number;
  readonly action: GameAction;
  readonly recommendedAction: GameAction;
  readonly actionLabel: string;
  readonly recommendedLabel: string;
  readonly policyLoss: number;
  readonly probabilityLoss: number;
  readonly cumulativePolicyLoss: number;
  readonly severity: ReviewSeverity;
  readonly rationale: readonly string[];
  readonly recommendedRationale: readonly string[];
}

export interface GameReview {
  readonly decisions: readonly DecisionReview[];
  readonly totalPolicyLoss: number;
  readonly counts: Readonly<Record<ReviewSeverity, number>>;
}

export function analyzeHumanDecisions(state: GameState, policy: PolicyArtifact): GameReview {
  let cumulativePolicyLoss = 0;
  const counts: Record<ReviewSeverity, number> = { best: 0, inaccuracy: 0, mistake: 0, blunder: 0 };
  const decisions: DecisionReview[] = [];
  for (let frameIndex = 1; frameIndex < state.timeline.length; frameIndex += 1) {
    const event = state.timeline[frameIndex].event;
    const action = event.action;
    if (event.seat !== state.humanSeat || action === null) continue;
    const before = stateBeforeFrame(state, frameIndex);
    const legal = legalActions(before);
    if (legal.length === 0) continue;
    const decision = decideAction(before, legal, policy);
    const actual = decision.choices.find((choice) => actionKey(choice.action) === actionKey(action));
    if (actual === undefined) continue;
    const recommended = decision.selected;
    const policyLoss = Math.max(0, recommended.score - actual.score);
    const severity = severityFor(policyLoss);
    cumulativePolicyLoss += policyLoss;
    counts[severity] += 1;
    decisions.push(Object.freeze({
      frameIndex,
      version: event.version,
      action: actual.action,
      recommendedAction: recommended.action,
      actionLabel: actionLabel(actual.action),
      recommendedLabel: actionLabel(recommended.action),
      policyLoss,
      probabilityLoss: Math.max(0, recommended.probability - actual.probability),
      cumulativePolicyLoss,
      severity,
      rationale: actual.rationale,
      recommendedRationale: recommended.rationale
    }));
  }
  return Object.freeze({
    decisions: Object.freeze(decisions),
    totalPolicyLoss: cumulativePolicyLoss,
    counts: Object.freeze({ ...counts })
  });
}

function stateBeforeFrame(state: GameState, frameIndex: number): GameState {
  const previous = state.timeline[frameIndex - 1];
  return {
    ...previous.state,
    history: state.history.slice(0, frameIndex),
    timeline: state.timeline.slice(0, frameIndex)
  };
}

function severityFor(policyLoss: number): ReviewSeverity {
  if (policyLoss < 0.05) return "best";
  if (policyLoss < 0.4) return "inaccuracy";
  if (policyLoss < 0.9) return "mistake";
  return "blunder";
}
