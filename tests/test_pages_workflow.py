from __future__ import annotations

import unittest
from pathlib import Path


class PagesWorkflowTests(unittest.TestCase):
    def test_builds_tests_verifies_and_deploys_the_browser_site(self) -> None:
        workflow = (Path(__file__).parents[1] / ".github" / "workflows" / "pages.yml").read_text(
            encoding="utf-8"
        )

        for required in (
            "uses: actions/checkout@v6",
            "uses: actions/setup-node@v6",
            "node-version: \"24\"",
            "cache-dependency-path: web/package-lock.json",
            "run: npm ci",
            "run: npm test",
            "run: npm run build",
            "python3 scripts/verify_browser_model_assets.py web/dist",
            "uses: actions/configure-pages@v5",
            "uses: actions/upload-pages-artifact@v4",
            "uses: actions/deploy-pages@v4",
            "if: github.ref == 'refs/heads/main'",
            "pages: write",
            "id-token: write",
        ):
            self.assertIn(required, workflow)
        self.assertGreaterEqual(workflow.count("working-directory: web"), 3)


if __name__ == "__main__":
    unittest.main()
