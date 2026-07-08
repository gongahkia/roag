from __future__ import annotations

import unittest

from kenjaku.frontend_theme import (
    KENJAKU_ARCADE_THEME_IP_NOTE,
    kenjaku_arcade_theme_contract,
    kenjaku_arcade_theme_css,
)


class FrontendThemeTests(unittest.TestCase):
    def test_theme_css_exposes_required_tokens_and_classes(self) -> None:
        css = kenjaku_arcade_theme_css()
        required = [
            "--kj-table-felt",
            "--kj-card",
            "--kj-chip-gold",
            "--kj-focus-ring",
            "--kj-motion-fast",
            ".kj-table-surface",
            ".kj-tile",
            ".kj-hud",
            ".kj-score-chip",
            ".kj-action-badge",
            ".kj-tile.is-selected",
            ".kj-card.is-error",
            ".kj-state--error",
            ".kj-score--positive",
            "prefers-reduced-motion",
        ]
        for token in required:
            with self.subTest(token=token):
                self.assertIn(token, css)

    def test_theme_css_is_static_and_network_free(self) -> None:
        css = kenjaku_arcade_theme_css().lower()
        for forbidden in ("url(", "http://", "https://", "@import"):
            with self.subTest(forbidden=forbidden):
                self.assertNotIn(forbidden, css)

    def test_disabled_and_error_states_have_accessible_hooks(self) -> None:
        css = kenjaku_arcade_theme_css()
        self.assertIn('.kj-action-badge[aria-disabled="true"]', css)
        self.assertIn("button:disabled", css)
        self.assertIn(".kj-tile.is-error", css)
        self.assertIn(".kj-state--danger", css)

    def test_contract_lists_frontend_refresh_surfaces(self) -> None:
        contract = kenjaku_arcade_theme_contract()
        surfaces = contract["surfaces"]
        self.assertGreaterEqual(
            set(surfaces),
            {
                "browser-demo",
                "replay-viewer",
                "benchmark-dashboard",
                "training-dashboard",
                "serve-index",
                "interpretability-overlay",
                "review-game",
            },
        )
        self.assertEqual(
            contract["integration"]["network"],
            "self-contained CSS; no browser network dependency",
        )

    def test_ip_note_rejects_balatro_asset_and_clone_usage(self) -> None:
        note = KENJAKU_ARCADE_THEME_IP_NOTE.lower()
        self.assertIn("do not use balatro assets", note)
        self.assertIn("exact layouts", note)


if __name__ == "__main__":
    unittest.main()
