import React from "react";

export interface DecisionActionView {
  readonly action: string;
  readonly tile: string | null;
}

export interface DecisionFactorView {
  readonly factor: string;
  readonly value: number;
  readonly contribution: number;
  readonly evidence: readonly string[];
}

export interface DecisionRationaleView {
  readonly factors: readonly DecisionFactorView[];
}

export interface DecisionAlternativeView {
  readonly action: DecisionActionView;
  readonly score: number;
  readonly score_delta: number;
  readonly rationale: DecisionRationaleView;
}

export interface DecisionCounterfactualsView {
  readonly decision: {
    readonly selected_action: DecisionActionView;
    readonly rationale: DecisionRationaleView;
  };
  readonly selected_score: number;
  readonly top_alternatives: readonly DecisionAlternativeView[];
}

interface DecisionRationaleDetailsProps {
  readonly counterfactuals: DecisionCounterfactualsView;
  readonly title?: string;
}

export function DecisionRationaleDetails({
  counterfactuals,
  title = "Rationale and alternatives"
}: DecisionRationaleDetailsProps) {
  const { decision, selected_score: selectedScore, top_alternatives: alternatives } = counterfactuals;
  return (
    <section aria-label={title} className="rationale-details panel">
      <h2>{title}</h2>
      <details open>
        <summary>
          {formatAction(decision.selected_action)} · {decision.rationale.factors.length} factors
        </summary>
        <p>Selected score: {formatNumber(selectedScore)}</p>
        <FactorList factors={decision.rationale.factors} />
        <details className="alternatives-details">
          <summary>Alternatives ({alternatives.length})</summary>
          {alternatives.length > 0 ? (
            <ol className="alternative-list">
              {alternatives.map((alternative, index) => (
                <li key={`${formatAction(alternative.action)}-${index}`}>
                  <details>
                    <summary>
                      {formatAction(alternative.action)} · score {formatNumber(alternative.score)} · Δ{" "}
                      {formatSignedNumber(alternative.score_delta)}
                    </summary>
                    <FactorList factors={alternative.rationale.factors} />
                  </details>
                </li>
              ))}
            </ol>
          ) : (
            <p>No ranked alternatives are available.</p>
          )}
        </details>
      </details>
    </section>
  );
}

export function formatAction(action: DecisionActionView): string {
  return action.tile === null ? action.action : `${action.action} ${action.tile}`;
}

export function formatNumber(value: number): string {
  if (!Number.isFinite(value)) return "—";
  return String(Math.round(value * 1_000_000) / 1_000_000);
}

export function formatSignedNumber(value: number): string {
  const number = formatNumber(value);
  return number === "—" || value < 0 ? number : `+${number}`;
}

function FactorList({ factors }: { readonly factors: readonly DecisionFactorView[] }) {
  if (factors.length === 0) return <p>No structured factors are available.</p>;
  return (
    <ol className="factor-list">
      {factors.map((factor) => (
        <li key={factor.factor}>
          <details>
            <summary>
              {factor.factor.replaceAll("_", " ")} · {formatSignedNumber(factor.contribution)}
            </summary>
            <dl>
              <div>
                <dt>Value</dt>
                <dd>{formatNumber(factor.value)}</dd>
              </div>
              <div>
                <dt>Contribution</dt>
                <dd>{formatSignedNumber(factor.contribution)}</dd>
              </div>
            </dl>
            {factor.evidence.length > 0 ? (
              <ul aria-label={`${factor.factor} evidence`} className="evidence-list">
                {factor.evidence.map((evidence, index) => (
                  <li key={`${evidence}-${index}`}>{evidence}</li>
                ))}
              </ul>
            ) : (
              <p>No evidence is attached.</p>
            )}
          </details>
        </li>
      ))}
    </ol>
  );
}
