from __future__ import annotations

import hashlib
from pathlib import Path
import tempfile
import unittest

from jomon.app_settings import AppSettings, load_app_settings, resolve_renderer, safe_save_name, save_app_settings
from jomon.font_stack import ASCII_ICONS, bundled_bigblue_font_path


class AppSettingsTests(unittest.TestCase):
    def test_settings_default_corruption_and_round_trip_are_frontend_only(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "settings.json"
            self.assertEqual(load_app_settings(path), AppSettings("debug"))
            path.write_text("not json", encoding="utf-8")
            self.assertEqual(load_app_settings(path), AppSettings("debug"))
            save_app_settings(AppSettings("ascii"), path)
            self.assertEqual(load_app_settings(path), AppSettings("ascii"))
            self.assertEqual(path.read_text(encoding="utf-8"), '{\n  "format": 1,\n  "renderer": "ascii"\n}\n')
        self.assertEqual(resolve_renderer(None, AppSettings("ascii")), "ascii")
        self.assertEqual(resolve_renderer("debug", AppSettings("ascii")), "debug")
        self.assertEqual(resolve_renderer("graphical", AppSettings("ascii")), "debug")
        self.assertIsNone(safe_save_name("../../escape"))
        self.assertEqual(safe_save_name("My Save 01"), "My Save 01")

    def test_bundled_bigblue_font_and_notices_are_pinned(self):
        font = Path(bundled_bigblue_font_path() or "")
        self.assertTrue(font.is_file())
        self.assertEqual(hashlib.sha256(font.read_bytes()).hexdigest(), "7dbbf3d473c77eb137eccf3f09c9c3c61b4cdef890c33110d4fb08ffbf637c7e")
        notice = Path("THIRD_PARTY_NOTICES.md").read_text(encoding="utf-8")
        self.assertIn("v3.5.1", notice)
        self.assertIn("CC BY-SA 4.0", notice)
        self.assertTrue(font.with_name("LICENSE-BigBlueTerminal-CC-BY-SA-4.0.txt").is_file())

    def test_required_icon_vocabulary_has_readable_fallbacks(self):
        for identity in ("health", "inventory", "quest", "travel", "vessel", "settings", "draw", "dice", "pause", "failure"):
            glyph, fallback = ASCII_ICONS[identity]
            self.assertTrue(glyph)
            self.assertTrue(fallback)
