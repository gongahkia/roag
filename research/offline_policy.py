#!/usr/bin/env python3
"""External-only, deterministic offline-policy research harness."""
import argparse
import hashlib
import json
import platform
import random
import sys
from collections import Counter, defaultdict
from pathlib import Path

HARNESS_VERSION = 1
DATASET_VERSION = 1
FEATURE_VERSION = 1
OBJECTIVE = "campaign-clears-deaths-stalls-exploration-resources"
DEFAULTS = {"epochs": 3, "history_window": 8, "learning_rate": 0.0003, "ppo_clip": 0.2}
HIDDEN_FIELDS = {"replay", "layoutId", "macroRecipeId", "routeContractId", "objectiveId", "escalation", "hiddenMap", "fullMap"}

def canonical(value): return json.dumps(value, sort_keys=True, separators=(",", ":"))
def sha256(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def has_hidden_field(value):
    if isinstance(value, list): return any(has_hidden_field(item) for item in value)
    if not isinstance(value, dict): return False
    return any(key in HIDDEN_FIELDS or has_hidden_field(child) for key, child in value.items())

def load_dataset(path, partition):
    records = [json.loads(line) for line in path.read_text().splitlines() if line]
    if not records: raise ValueError("dataset is empty")
    for record in records:
        metadata = record.get("metadata", {})
        if record.get("version") != DATASET_VERSION or metadata.get("featureVersion") != FEATURE_VERSION: raise ValueError("unsupported dataset or feature version")
        if metadata.get("partition") != partition: raise ValueError("dataset partition mismatch")
        if metadata.get("informationMode") != "visible": raise ValueError("research harness accepts visible records only")
        if has_hidden_field(record): raise ValueError("visible record contains hidden state")
        if not record.get("chosenAction", {}).get("command"): raise ValueError("record has no chosen action")
    return records

def encoder(record, recurrent):
    history = record["history"][-DEFAULTS["history_window"]:] if recurrent else []
    observation = record["observation"]
    return canonical({"hero": observation["hero"], "objective": observation["objective"], "history": history})

def train(records, algorithm, seed):
    random.seed(seed)
    recurrent = algorithm == "recurrent-ppo-style"
    counts = defaultdict(Counter)
    for record in records: counts[encoder(record, recurrent)][record["chosenAction"]["command"]] += 1
    policy = {state: sorted(actions.items(), key=lambda item: (-item[1], item[0]))[0][0] for state, actions in sorted(counts.items())}
    return {"algorithm": algorithm, "recurrent": recurrent, "visibleEncoder": True, "policy": policy}

def checkpoint(model, dataset, seed):
    return {"version": HARNESS_VERSION, "kind": "jomon-offline-policy-checkpoint", "model": model, "metadata": {"seed": seed, "hyperparameters": {**DEFAULTS, "seed": seed}, "dataset": {"version": DATASET_VERSION, "featureVersion": FEATURE_VERSION, "partition": "development", "sha256": sha256(dataset)}, "environment": {"python": sys.version.split()[0], "platform": platform.platform()}, "checkpoint": "deterministic-inline-policy"}}

def evaluate(model, records, partition):
    correct = sum(model["policy"].get(encoder(record, model["recurrent"])) == record["chosenAction"]["command"] for record in records)
    return {"version": 1, "kind": "jomon-offline-policy-evaluation", "partition": partition, "objective": {"id": OBJECTIVE, "version": 1}, "profile": {"informationMode": "visible", "algorithm": model["algorithm"]}, "summary": {"episodes": len(records), "actionAgreement": correct / len(records)}}

def main():
    parser = argparse.ArgumentParser()
    commands = parser.add_subparsers(dest="command", required=True)
    train_parser = commands.add_parser("train")
    train_parser.add_argument("--dataset", required=True, type=Path); train_parser.add_argument("--output", required=True, type=Path)
    train_parser.add_argument("--algorithm", required=True, choices=("behavior-cloning", "recurrent-ppo-style")); train_parser.add_argument("--seed", type=int, default=17)
    evaluate_parser = commands.add_parser("evaluate")
    evaluate_parser.add_argument("--checkpoint", required=True, type=Path); evaluate_parser.add_argument("--dataset", required=True, type=Path); evaluate_parser.add_argument("--output", required=True, type=Path)
    evaluate_parser.add_argument("--partition", required=True, choices=("development", "held-out"))
    args = parser.parse_args()
    if args.command == "train":
        records = load_dataset(args.dataset, "development")
        args.output.write_text(json.dumps(checkpoint(train(records, args.algorithm, args.seed), args.dataset, args.seed), sort_keys=True, indent=2) + "\n")
    else:
        state = json.loads(args.checkpoint.read_text())
        if state.get("kind") != "jomon-offline-policy-checkpoint": raise ValueError("unsupported checkpoint")
        args.output.write_text(json.dumps(evaluate(state["model"], load_dataset(args.dataset, args.partition), args.partition), sort_keys=True, indent=2) + "\n")

if __name__ == "__main__": main()
