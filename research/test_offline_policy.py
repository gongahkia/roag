import json
import subprocess
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).parent
HARNESS = ROOT / "offline_policy.py"

def record(partition="development", command="h", history=None):
    return {"version": 1, "metadata": {"partition": partition, "featureVersion": 1, "informationMode": "visible"}, "observation": {"hero": {"health": 8}, "objective": {"id": "mine", "status": "active"}}, "history": history or [], "chosenAction": {"command": command}}

def write(path, records): path.write_text("".join(json.dumps(item) + "\n" for item in records))

class OfflinePolicyTests(unittest.TestCase):
    def test_deterministic_smoke_and_metadata(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary); data = root / "development.jsonl"; first = root / "first.json"; second = root / "second.json"; evaluation = root / "evaluation.json"
            write(data, [record(command="h"), record(command="h"), record(command="l")])
            command = ["python3", str(HARNESS), "train", "--dataset", str(data), "--algorithm", "behavior-cloning", "--seed", "9"]
            subprocess.run(command + ["--output", str(first)], check=True); subprocess.run(command + ["--output", str(second)], check=True)
            self.assertEqual(first.read_bytes(), second.read_bytes())
            model = json.loads(first.read_text())
            self.assertEqual(model["metadata"]["dataset"]["partition"], "development")
            self.assertEqual(model["metadata"]["seed"], 9)
            subprocess.run(["python3", str(HARNESS), "evaluate", "--checkpoint", str(first), "--dataset", str(data), "--partition", "development", "--output", str(evaluation)], check=True)
            self.assertEqual(json.loads(evaluation.read_text())["objective"]["id"], "campaign-clears-deaths-stalls-exploration-resources")
            subprocess.run(["python3", str(HARNESS), "train", "--dataset", str(data), "--algorithm", "recurrent-ppo-style", "--seed", "9", "--output", str(second)], check=True)
            self.assertTrue(json.loads(second.read_text())["model"]["recurrent"])

    def test_held_out_cannot_train_and_visible_hidden_state_is_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary); held = root / "held.jsonl"; output = root / "model.json"; hidden = root / "hidden.jsonl"
            write(held, [record(partition="held-out")]); write(hidden, [{**record(), "replay": {"layoutId": "hidden"}}])
            train = ["python3", str(HARNESS), "train", "--dataset"]
            self.assertNotEqual(subprocess.run(train + [str(held), "--algorithm", "behavior-cloning", "--output", str(output)], capture_output=True).returncode, 0)
            self.assertNotEqual(subprocess.run(train + [str(hidden), "--algorithm", "recurrent-ppo-style", "--output", str(output)], capture_output=True).returncode, 0)

if __name__ == "__main__": unittest.main()
