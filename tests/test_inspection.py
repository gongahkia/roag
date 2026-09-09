import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine
from dumbest_dungeon.inspection import resolution_lines


class InspectionTests(unittest.TestCase):
    def test_arithmetic_and_order_are_read_only_and_source_attributed(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        engine.start_combat("lost_shift")
        hero, enemy = engine.living_heroes()[0], engine.living_enemies()[0]
        hero.block = 3
        hero.statuses.update(vulnerable=2, riposte=2)
        engine._damage(hero, 8, enemy)
        before = engine.snapshot()
        text = "\n".join(resolution_lines(engine))
        self.assertIn("request 8; after modifiers 12; block 3; deflected 0; HP lost 9", text)
        self.assertIn("AFTER status:riposte", text)
        self.assertIn("PRIMARY status:riposte:warden depth 1 parent", text)
        self.assertEqual(before, engine.snapshot())
