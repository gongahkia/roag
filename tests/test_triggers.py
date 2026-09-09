from __future__ import annotations

import unittest

from dumbest_dungeon.triggers import EventType as E, Limiter, LimitKind, Phase, TriggerSpec, strongly_connected, validate_trigger_graph


class TriggerContractTests(unittest.TestCase):
    def test_phase_order_is_explicit(self) -> None:
        self.assertEqual(["REPLACE", "BEFORE", "PRIMARY", "AFTER", "DEATH", "CLEANUP"], [phase.name for phase in Phase])

    def test_unlimited_cycle_is_rejected_and_finite_cycle_is_accepted(self) -> None:
        damage = TriggerSpec("echo", E.DAMAGE, (E.DRAW,))
        draw = TriggerSpec("spark", E.DRAW, (E.DAMAGE,))
        with self.assertRaisesRegex(ValueError, "unbounded"):
            validate_trigger_graph((damage, draw))
        bounded = TriggerSpec("spark", E.DRAW, (E.DAMAGE,), limiter=Limiter(LimitKind.RETRIGGERS, 3))
        self.assertEqual({"echo": ("spark",), "spark": ("echo",)}, validate_trigger_graph((bounded, damage)))

    def test_a_limiter_somewhere_in_component_does_not_prove_every_cycle_finite(self) -> None:
        specs = (
            TriggerSpec("bounded", E.DAMAGE, (E.DRAW,), limiter=Limiter(LimitKind.ROOT)),
            TriggerSpec("unbounded", E.DAMAGE, (E.DRAW,)),
            TriggerSpec("feedback", E.DRAW, (E.DAMAGE,)),
        )
        with self.assertRaisesRegex(ValueError, "unbounded"):
            validate_trigger_graph(specs)

    def test_proc_family_exclusion_must_break_its_own_ancestry(self) -> None:
        with self.assertRaisesRegex(ValueError, "own proc family"):
            TriggerSpec("echo", E.DAMAGE, (E.DAMAGE,), proc_family="echo",
                        limiter=Limiter(LimitKind.FAMILY, family="unrelated"))
        spec = TriggerSpec("echo", E.DAMAGE, (E.DAMAGE,), proc_family="echo",
                           limiter=Limiter(LimitKind.FAMILY, family="echo"))
        validate_trigger_graph((spec,))

    def test_invalid_limiter_and_duplicate_ids_fail_at_contract_boundary(self) -> None:
        for amount in (0, -1, True, 10001):
            with self.assertRaises(ValueError):
                Limiter(LimitKind.CHARGES, amount)
        spec = TriggerSpec("one", E.DAMAGE, ())
        with self.assertRaisesRegex(ValueError, "duplicate"):
            validate_trigger_graph((spec, spec))

    def test_large_dependency_chain_uses_no_python_recursion(self) -> None:
        graph = {str(index): (str(index + 1),) if index < 2999 else () for index in range(3000)}
        self.assertEqual(3000, len(strongly_connected(graph)))


if __name__ == "__main__":
    unittest.main()
