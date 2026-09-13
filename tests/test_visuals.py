from __future__ import annotations

from importlib.resources import files
import string
import unittest

from dumbest_dungeon.content import load_catalog as load_dungeon_catalog
from dumbest_dungeon.json_data import loads
from dumbest_dungeon.office_art import EXPEDITION_MAP_SYMBOLS, OFFICE_SPRITES, office_card_glyph
from jomon.catalog import VISUAL_SECTIONS, load_catalog
from jomon.tavern_draw_ui import card_frame
from jomon.tavern_games_ui import dice_face
from jomon.terminal import semantic_role
from jomon.vessel import TAVERN_MAP, VESSEL_LEVELS
from jomon.visuals import SEMANTIC_GLYPH_ROLES


class VisualCatalogTests(unittest.TestCase):
    def test_jomon_maps_roles_cards_and_dice_are_packaged_visuals(self):
        visual = load_catalog("visuals.json", VISUAL_SECTIONS)
        self.assertEqual(visual["vessel_levels"], {str(level): list(rows) for level, rows in VESSEL_LEVELS.items()})
        self.assertEqual(visual["tavern_map"], list(TAVERN_MAP))
        for aboard, scope in ((False, "region"), (True, "aboard")):
            self.assertEqual(visual["semantic_roles"][scope], SEMANTIC_GLYPH_ROLES[scope])
            for glyph in string.printable[:95]:
                self.assertEqual(semantic_role(glyph, aboard=aboard),
                                 visual["semantic_roles"][scope].get(glyph, "terrain"))
        self.assertEqual(visual["tavern_dice"], {str(face): list(dice_face(face)) for face in range(1, 7)})
        frame = visual["tavern_cards"]
        self.assertEqual(card_frame(0), tuple(
            row.format(edge=frame["edge"], label_left="2S", suit="S", label_right="2S")
            for row in frame["frame"]
        ))
        self.assertEqual(card_frame(0, selected=True)[0], frame["selected_edge"])

    def test_dungeon_office_portraits_and_map_symbols_are_packaged(self):
        data = loads(files("dumbest_dungeon").joinpath("data", "office_visuals.json").read_text(encoding="utf-8"))
        self.assertEqual(data["office_sprites"], {role: list(rows) for role, rows in OFFICE_SPRITES.items()})
        self.assertEqual(data["expedition_map_symbols"], EXPEDITION_MAP_SYMBOLS)
        self.assertEqual(set(OFFICE_SPRITES), set(load_dungeon_catalog().heroes))
        self.assertTrue(all(len(office_card_glyph(role)) == 3 for role in OFFICE_SPRITES))
