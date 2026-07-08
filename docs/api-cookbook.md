# Kenjaku API Cookbook

These examples use bundled fixtures only. Run them from the repository root with
`PYTHONPATH=src`.

## Parse One Tenhou XML File

```python
from pathlib import Path

from kenjaku.api import parse_tenhou_xml_file

game = parse_tenhou_xml_file(Path("data/fixtures/tenhou/minimal_4p.xml"))
round_counts = [
    (round_index, len(round_.draws), len(round_.discards), len(round_.calls), len(round_.reaches))
    for round_index, round_ in enumerate(game.rounds)
]

print(round_counts)
assert round_counts == [(0, 2, 2, 0, 0)]
```

## Extract Discard Examples From A Fixture Directory

```python
from kenjaku.api import iter_discard_examples, parse_tenhou_xml_dataset

dataset = parse_tenhou_xml_dataset(["data/fixtures/tenhou"])
examples = list(iter_discard_examples(dataset.game))

print(len(examples))
assert examples
```

## Train And Query A Discard Frequency Baseline

```python
from kenjaku.api import DiscardFrequencyBaseline, iter_discard_examples, parse_tenhou_xml_file

game = parse_tenhou_xml_file("data/fixtures/tenhou/minimal_4p.xml")
examples = list(iter_discard_examples(game))
model = DiscardFrequencyBaseline.fit(examples)
prediction = model.predict(examples[0].hand_counts)

print(prediction.notation, model.score(examples))
assert prediction.notation in {"4p", "7p"}
assert 0.0 <= model.score(examples) <= 1.0
```

## Train And Evaluate A Discard Linear Model

```python
from kenjaku.api import (
    DiscardLinearModel,
    RAW_COUNT_FEATURE_PROFILE,
    iter_discard_examples,
    parse_tenhou_xml_file,
)

game = parse_tenhou_xml_file("data/fixtures/tenhou/minimal_4p.xml")
examples = list(iter_discard_examples(game))
model = DiscardLinearModel.fit(
    examples,
    epochs=3,
    learning_rate=0.1,
    feature_profile=RAW_COUNT_FEATURE_PROFILE,
)
accuracy = model.score(examples)

print(model.kind, round(accuracy, 3))
assert model.feature_profile == RAW_COUNT_FEATURE_PROFILE
assert 0.0 <= accuracy <= 1.0
```

## Export Decision Snapshots To JSONL

```python
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.api import export_decision_snapshots, parse_tenhou_xml_file

game = parse_tenhou_xml_file("data/fixtures/tenhou/minimal_4p.xml")

with TemporaryDirectory() as directory:
    output = Path(directory) / "snapshots.jsonl"
    snapshots = export_decision_snapshots(game, output, decision_types=("discard",), limit=1)
    text = output.read_text(encoding="utf-8")

print(len(snapshots), len(text.splitlines()))
assert len(snapshots) == 1
assert len(text.splitlines()) == 1
```

## Load Snapshots And Build An In-Memory Overlay

```python
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.api import (
    build_interpretability_overlay,
    export_decision_snapshots,
    load_decision_snapshots,
    parse_tenhou_xml_file,
)

game = parse_tenhou_xml_file("data/fixtures/tenhou/minimal_4p.xml")

with TemporaryDirectory() as directory:
    path = Path(directory) / "snapshots.jsonl"
    export_decision_snapshots(game, path, decision_types=("discard",), limit=1)
    snapshots = load_decision_snapshots(path)
    report = build_interpretability_overlay(snapshots, min_decisions=1)

print(report["decision_count"], report["policy_kind"])
assert report["decision_count"] == 1
assert report["decisions"]
```

## Score Deal-In Risk For Discards

```python
from kenjaku.api import DealInLinearModel, parse_tenhou_xml_file
from kenjaku.training import iter_deal_in_examples

game = parse_tenhou_xml_file("data/fixtures/tenhou/events_4p.xml")
examples = list(iter_deal_in_examples(game))
model = DealInLinearModel.fit(
    examples,
    epochs=3,
    learning_rate=0.1,
    positive_class_weight=2.0,
)
probabilities = [model.predict_probability(example) for example in examples]

print([round(probability, 3) for probability in probabilities])
assert probabilities
assert all(0.0 <= probability <= 1.0 for probability in probabilities)
```
