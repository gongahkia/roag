# Final Evaluation

TODO-601 remains open until final supervised, self-play, Sanma, and interpretability metrics are
reproducible from frozen inputs. Keep restricted data, checkpoints, raw reports, and generated
tables under ignored paths unless a release review clears a public-safe artifact.

## Manifest

Write an ignored manifest such as `runs/todo-601/manifest.json`:

```json
{
  "kind": "kenjaku-final-evaluation-manifest-v0",
  "frozen_inputs": {
    "dataset_slices": [
      {
        "name": "tenhou-final-4p",
        "path": "runs/todo-601/dataset-slice-manifest.json",
        "source_command": "exact command that produced the slice"
      }
    ],
    "model_checkpoints": [
      {
        "name": "discard-transformer-s0",
        "path": "runs/todo-601/checkpoints/discard-transformer-s0.pt"
      }
    ],
    "evaluation_scripts": [
      {
        "name": "final-manifest-validator",
        "path": "scripts/validate_final_evaluation_manifest.py"
      }
    ]
  },
  "evaluations": {
    "supervised": {
      "reports": [
        {
          "name": "supervised-summary",
          "path": "runs/todo-601/supervised-summary.json",
          "command": "exact supervised evaluation command"
        }
      ]
    },
    "self_play": {
      "reports": [
        {
          "name": "self-play-summary",
          "path": "runs/todo-601/self-play-summary.json",
          "command": "exact self-play evaluation command"
        }
      ]
    },
    "sanma": {
      "reports": [
        {
          "name": "sanma-summary",
          "path": "runs/todo-601/sanma-summary.json",
          "command": "exact Sanma evaluation command"
        }
      ]
    },
    "interpretability": {
      "reports": [
        {
          "name": "interpretability-overlay",
          "path": "runs/todo-601/interpretability-overlay.html",
          "command": "exact interpretability command"
        }
      ]
    }
  },
  "metric_tables": [
    {
      "name": "final-table",
      "path": "runs/todo-601/final-metrics.json",
      "metrics": [
        "accuracy",
        "balanced_accuracy",
        "deal_in_calibration",
        "average_placement",
        "score_delta",
        "ablations"
      ]
    }
  ],
  "commands": [
    {
      "name": "supervised",
      "command": "exact supervised evaluation command"
    },
    {
      "name": "self-play",
      "command": "exact self-play evaluation command"
    },
    {
      "name": "sanma",
      "command": "exact Sanma evaluation command"
    },
    {
      "name": "interpretability",
      "command": "exact interpretability command"
    }
  ]
}
```

Validate it before closing TODO-601:

```bash
python3 scripts/validate_final_evaluation_manifest.py runs/todo-601/manifest.json
```

The validator checks that dataset slices, model checkpoints, generated evaluation reports, and
metric tables are non-empty files under ignored paths; evaluation scripts exist as non-empty tracked
repo files; every final evaluation section has a report and command; and metric tables cover
accuracy, balanced accuracy, deal-in calibration, average placement, score delta, and ablations.

## Closure Rule

Do not close TODO-601 from fixture smoke outputs. Close it only after the manifest validates and the
checked-in public summary links every final claim to the ignored manifest path and exact commands.
