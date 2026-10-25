import os
from pathlib import Path
import subprocess
import sys
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'tools'))
import profile_game
from agent_support import runner
from agent_support.workspace import Repository
from playtest_support import driver


class ProfilingTests(unittest.TestCase):
    def setUp(self):
        self.repo = Repository(Path(__file__).resolve().parents[2])

    def test_wrapper_with_client_argument_cannot_be_mistaken_for_dotnet(self):
        dll = self.repo.root / 'src/Vagabonds.Client/bin/Release/net10.0/Vagabonds.Client.dll'
        process = subprocess.Popen([sys.executable, '-c', 'import time; time.sleep(10)', str(dll)])
        try:
            with self.assertRaisesRegex(ValueError, 'Could not identify'):
                profile_game.client_process(self.repo, runner.identity(process.pid))
        finally:
            process.terminate(); process.wait(timeout=5)

    def test_instrumented_driver_cannot_launch_on_host_display(self):
        with patch.dict(os.environ, {'DISPLAY': ':host'}), patch.object(subprocess, 'Popen') as launch:
            with self.assertRaisesRegex(ValueError, 'private display'):
                driver.start(self.repo, visible=True, no_build=True, launch_prefix=['renderdoccmd'])
            launch.assert_not_called()

    def test_graphics_success_requires_real_frames_and_timed_draws(self):
        with self.assertRaises(ValueError):
            profile_game.graphics_evidence({'FramesCount': 20}, '# profiling header only\n')
        with self.assertRaises(ValueError):
            profile_game.graphics_evidence({'FramesCount': 0}, 'call 14 1 2 3 4 0 0 0 0 0 0 glDrawElements')
        with self.assertRaises(ValueError):
            profile_game.graphics_evidence({'FramesCount': 1}, 'call 14 0 0 3 4 0 0 0 0 0 0 glDrawElements')
        evidence = profile_game.graphics_evidence({'FramesCount': 1}, 'call 14 1 2 3 4 0 0 0 0 0 0 glDrawElements')
        self.assertEqual(evidence['drawSamples'], 1)


if __name__ == '__main__':
    unittest.main()
