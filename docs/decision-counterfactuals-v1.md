# DecisionCounterfactualsV1

`kenjaku-decision-counterfactuals-v1` pairs a source `DecisionResultV1` with its raw selected score and a sorted list of top alternative actions. Every alternative records its uncalibrated score, exact score delta from the selected action, and a `DecisionRationaleV1`.

Alternative actions must share the source ruleset, cannot repeat the selected action, and are unique. They are ordered by descending score, then canonical `ActionV1` fields, making tied rankings reproducible. `build_decision_counterfactuals` validates input actions and derives each delta without treating raw scores as probabilities.
