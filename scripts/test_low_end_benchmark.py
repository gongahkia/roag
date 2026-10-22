import csv
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from low_end_benchmark import analyze


class BenchmarkReadinessTests(unittest.TestCase):
    def write(self, directory, ready=True, edits=True):
        path = Path(directory) / 'edit.csv'
        fields = ('elapsed_s', 'requested_chunks', 'ready_chunks', 'radius', 'draws', 'edit_latency_count', 'edit_latency_0', 'edit_latency_1')
        with path.open('w', newline='') as file:
            writer = csv.DictWriter(file, fieldnames=fields); writer.writeheader()
            for index in range(50):
                writer.writerow(dict(elapsed_s=index / 10, requested_chunks=40, radius=1,
                                     ready_chunks=40 if ready and index >= 5 else 0,
                                     draws=1 if index >= 3 else 0,
                                     edit_latency_count=2 if edits and index == 30 else 0,
                                     edit_latency_0=20, edit_latency_1=40))
        return path

    def summary(self):
        return dict(fluid_queue_overflow=0, fluid_refused=0)

    @patch('low_end_benchmark.benchmark.summarize')
    def test_waits_for_readiness_and_captures_each_latency(self, summarize):
        summarize.return_value = self.summary()
        with tempfile.TemporaryDirectory() as directory:
            result = analyze(self.write(directory), 'edit')
        self.assertEqual(result['first_terrain_ms'], 300)
        self.assertEqual(result['full_terrain_ms'], 500)
        self.assertEqual(result['edit_latency']['samples'], 2)
        self.assertEqual(result['edit_latency']['max_ms'], 40)
        self.assertEqual(summarize.call_args.kwargs['warmup'], 2.5)

    def test_incomplete_rendering_is_not_scored(self):
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaisesRegex(AssertionError, 'never became fully ready'):
                analyze(self.write(directory, ready=False), 'forest')

    def test_reduced_draw_distance_is_not_a_speedup(self):
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaisesRegex(AssertionError, 'Draw distance reduced'):
                analyze(self.write(directory), 'forest', expected_radius=2)

    @patch('low_end_benchmark.benchmark.summarize')
    def test_edit_scene_requires_completed_edits(self, summarize):
        summarize.return_value = self.summary()
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaisesRegex(AssertionError, 'No edited chunk'):
                analyze(self.write(directory, edits=False), 'edit')


if __name__ == '__main__':
    unittest.main()
