import type { GameReview, ReviewSeverity } from "../game/analysis";

interface DecisionReviewProps {
  readonly review: GameReview;
  readonly onSelectFrame: (frameIndex: number) => void;
}

export function DecisionReview({ review, onSelectFrame }: DecisionReviewProps) {
  const maximumLoss = Math.max(0.1, review.totalPolicyLoss);
  const denominator = Math.max(1, review.decisions.length - 1);
  const points = review.decisions.map((decision, index) => ({
    decision,
    x: 6 + index * (88 / denominator),
    y: 7 + decision.cumulativePolicyLoss * (32 / maximumLoss)
  }));
  const path = points.map(({ x, y }, index) => `${index === 0 ? "M" : "L"}${x.toFixed(2)} ${y.toFixed(2)}`).join(" ");

  return (
    <section className="panel review-panel" aria-labelledby="review-heading">
      <div className="panel-heading">
        <div><p className="panel-kicker">Local policy audit</p><h2 id="review-heading">Decision curve</h2></div>
        <span>{review.decisions.length} reviewed</span>
      </div>
      <p className="status-line">Cumulative policy loss compares each recorded Seat 0 action with this browser policy’s top legal action at the prior snapshot. It is not win probability, ELO, or game-theoretic EV.</p>
      {review.decisions.length === 0 ? <p className="muted">Make a Seat 0 decision to start a local review.</p> : <>
        <dl className="review-stats">
          <div><dt>Loss</dt><dd>{review.totalPolicyLoss.toFixed(2)}u</dd></div>
          <div><dt>Best</dt><dd>{review.counts.best}</dd></div>
          <div><dt>Mistakes</dt><dd>{review.counts.mistake}</dd></div>
          <div><dt>Blunders</dt><dd>{review.counts.blunder}</dd></div>
        </dl>
        <div className="review-chart-wrap">
          <svg aria-label="Cumulative local policy loss across Seat 0 decisions" className="review-chart" role="img" viewBox="0 0 100 44">
            <line x1="5" x2="95" y1="7" y2="7" />
            <line x1="5" x2="95" y1="39" y2="39" />
            <path d={path} />
            {points.map(({ decision, x, y }) => <ReviewMarker key={decision.version} severity={decision.severity} x={x} y={y} />)}
            <text x="5" y="4">0 loss</text>
            <text x="5" y="43">more loss</text>
          </svg>
          <p><span>○ best</span><span>□ inaccuracy</span><span>△ mistake</span><span>◆ blunder</span></p>
        </div>
        <ol className="review-list" aria-label="Seat 0 decision review">
          {[...review.decisions].reverse().slice(0, 10).map((decision, index) => <li key={decision.version}>
            <button onClick={() => onSelectFrame(decision.frameIndex)} type="button">
              <span>{decision.severity} / v{decision.version}</span><b>{decision.policyLoss.toFixed(2)}u</b>
            </button>
            <p>{decision.actionLabel} <span>vs</span> {decision.recommendedLabel}</p>
            {index === 0 ? <em>Latest decision; select to inspect its replay frame.</em> : null}
          </li>)}
        </ol>
      </>}
    </section>
  );
}

function ReviewMarker({ severity, x, y }: { readonly severity: ReviewSeverity; readonly x: number; readonly y: number }) {
  if (severity === "best") return <circle className="review-marker best" cx={x} cy={y} r="1.6" />;
  if (severity === "inaccuracy") return <rect className="review-marker inaccuracy" height="3.2" width="3.2" x={x - 1.6} y={y - 1.6} />;
  if (severity === "mistake") return <path className="review-marker mistake" d={`M${x} ${y - 2} L${x + 2} ${y + 1.5} L${x - 2} ${y + 1.5} Z`} />;
  return <path className="review-marker blunder" d={`M${x} ${y - 2.2} L${x + 2.2} ${y} L${x} ${y + 2.2} L${x - 2.2} ${y} Z`} />;
}
