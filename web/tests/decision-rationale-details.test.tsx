import assert from "node:assert/strict";
import test from "node:test";

import React from "react";
import { renderToStaticMarkup } from "react-dom/server";

import {
  DecisionRationaleDetails,
  formatAction,
  formatNumber,
  formatSignedNumber,
  type DecisionCounterfactualsView
} from "../src/components/DecisionRationaleDetails";

const payload: DecisionCounterfactualsView = {
  decision: {
    selected_action: { action: "kita", tile: "N" },
    rationale: {
      factors: [
        {
          factor: "replacement_draw",
          value: 1,
          contribution: 0.25,
          evidence: ["North replacement draw"]
        },
        { factor: "defense_risk", value: 0.12, contribution: -0.2, evidence: [] }
      ]
    }
  },
  selected_score: 0.5,
  top_alternatives: [
    {
      action: { action: "discard", tile: "9m" },
      score: 0.3,
      score_delta: -0.2,
      rationale: { factors: [] }
    }
  ]
};

test("renders expandable Sanma rationale factors, evidence, and alternatives", () => {
  const markup = renderToStaticMarkup(<DecisionRationaleDetails counterfactuals={payload} />);

  assert.match(markup, /Rationale and alternatives/);
  assert.match(markup, /kita N · 2 factors/);
  assert.match(markup, /replacement draw · \+0.25/);
  assert.match(markup, /North replacement draw/);
  assert.match(markup, /Alternatives \(1\)/);
  assert.match(markup, /discard 9m · score 0.3 · Δ -0.2/);
  assert.match(markup, /<details open=""/);
});

test("formats stable action and signed numeric summaries", () => {
  assert.equal(formatAction({ action: "pass", tile: null }), "pass");
  assert.equal(formatNumber(0.123456789), "0.123457");
  assert.equal(formatSignedNumber(0), "+0");
  assert.equal(formatSignedNumber(-0.25), "-0.25");
  assert.equal(formatNumber(Number.NaN), "—");
});
