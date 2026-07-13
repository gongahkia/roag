# DecisionResultV1

`kenjaku-decision-result-v1` is the strict result boundary for a selected `ActionV1`. It records `decision_id`, `ruleset`, `model_id`, a calibrated `probability` when available, and a structured `rationale`.

Each rationale factor has a stable lowercase `factor` identifier, numeric `value`, signed `contribution`, and optional textual `evidence`. Positive contribution supports the selected action; negative contribution opposes it. Factors are ordered and unique, so later renderers can create text without parsing free-form explanations.

The schema accepts uncalibrated decisions with `probability: null`, rejects non-finite values and unknown fields, and validates that the selected action uses the result ruleset.

`extract_heuristic_rationale` converts the ordered factors from one heuristic candidate without fabricating a probability or evidence.
