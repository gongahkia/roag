import copy
import curses
import unittest

from jomon.actions import interact
from jomon.inventory import auto_place, create_item
from jomon.regional_history import account_for, ledger_lines, network_institution_for_contact
from jomon.state import Position, create_world
from jomon.terminal import InputEvent, OverlayView, _handle_overlay_view, _overlay, _overlay_lines, information_lines


class PanelSink:
    def __init__(self, height=24, width=80):
        self.height, self.width, self.writes = height, width, []

    def getmaxyx(self):
        return self.height, self.width

    def addstr(self, y, x, text, *attributes):
        if not (0 <= y < self.height and 0 <= x < self.width and x + len(text) < self.width):
            raise AssertionError((y, x, text))
        self.writes.append(text)

    def refresh(self):
        pass

    def addnstr(self, y, x, text, count, *attributes):
        self.addstr(y, x, text[:count], *attributes)


class InformationPanelTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("readable work ledger")

    def setUp(self):
        self.state = copy.deepcopy(self.base)

    def test_wrap_preserves_every_word_including_long_evidence_ids(self):
        original = ["TESTIMONY " + "work and water " * 12, "EVIDENCE " + "a" * 180]
        for width in (72, 92):
            lines = information_lines(original, width)
            self.assertTrue(all(len(line) <= width for line in lines))
            self.assertEqual("".join("".join(lines).split()), "".join("".join(original).split()))

    def test_keyboard_and_mouse_scroll_to_final_forecast_without_time(self):
        state, view = self.state, OverlayView("regional-ledger")
        before = state.to_dict()
        sink = PanelSink()
        _overlay(sink, "LEDGER", ledger_lines(state), view)
        self.assertGreater(view.line_count, view.page_rows)
        _handle_overlay_view(state, view, InputEvent("key", key=curses.KEY_END))
        sink.writes.clear()
        _overlay(sink, "LEDGER", ledger_lines(state), view)
        self.assertTrue(any("FORECAST" in text for text in sink.writes))
        old = view.scroll_offset
        _handle_overlay_view(state, view, InputEvent("mouse", button="wheel-up"))
        self.assertLess(view.scroll_offset, old)
        _handle_overlay_view(state, view, InputEvent("key", key=curses.KEY_HOME))
        self.assertEqual(view.scroll_offset, 0)
        self.assertTrue(_handle_overlay_view(state, view, InputEvent("key", key=27))[0])
        self.assertEqual(state.to_dict(), before)

    def test_resize_clamps_scroll_and_has_readable_controls(self):
        view, sink = OverlayView("regional-ledger"), PanelSink()
        for height, width in ((24, 80), (32, 100), (24, 80)):
            sink.height, sink.width = height, width
            view.scroll_offset = 9999
            _overlay(sink, "LEDGER", ledger_lines(self.state), view)
            self.assertLessEqual(view.scroll_offset, max(0, view.line_count - view.page_rows))
            self.assertTrue(any("Esc close" in text for text in sink.writes))

    def test_physical_contact_delivery_choice_dispatches_real_contract(self):
        state = self.state
        contact = state.contacts[state.active_region_id][1]
        schedule = state.actor_schedules[contact.id]
        state.location = "region"
        state.position = Position(schedule.position.x - 1, schedule.position.y, schedule.position.z)
        account = account_for(state)
        state.market[account.dependency].stock = 0
        supply = create_item(state, f"commodity:{account.dependency}", "delivered test supply")
        self.assertTrue(auto_place(state, supply.id, "pack", owner_id=state.active_courier_id))
        result = interact(state)
        self.assertEqual(result.overlay, f"contact-service:{contact.id}")
        before = state.world_time
        closed, quit_requested = _handle_overlay_view(state, OverlayView(result.overlay), InputEvent("key", key=ord("d")))
        self.assertTrue(closed)
        self.assertFalse(quit_requested)
        self.assertEqual(supply.location, "destroyed")
        self.assertEqual(account.trust, 1)
        self.assertGreater(state.world_time, before)

    def test_network_contact_dispatches_its_own_supply_and_route_service(self):
        state = self.state
        contact = state.contacts[state.active_region_id][-1]
        network = network_institution_for_contact(state, contact.id)
        self.assertIsNotNone(network)
        supply = create_item(
            state, f"commodity:{network.dependency}", "network panel lot",
        )
        self.assertTrue(auto_place(
            state, supply.id, "pack", owner_id=state.active_courier_id,
        ))
        state.location = "region"
        state.position = state.actor_schedules[contact.id].position
        result = interact(state)
        self.assertEqual(result.overlay, f"contact-service:{contact.id}")
        closed, _ = _handle_overlay_view(
            state, OverlayView(result.overlay), InputEvent("key", key=ord("d")),
        )
        self.assertTrue(closed)
        self.assertEqual((supply.location, network.trust), ("destroyed", 1))
        self.assertTrue(any(
            option.key == "C"
            for option in __import__(
                "jomon.terminal", fromlist=["dialogue_choices"]
            ).dialogue_choices(state, result.overlay)
        ))

    def test_contacts_teach_one_current_system_without_repeated_prose(self):
        state = self.state
        state.location = "region"
        title, lines = _overlay_lines(state, "contact")
        self.assertEqual(title, state.contact.name.upper())
        advice = [line for line in lines if line.startswith("ADVICE")]
        self.assertEqual(len(advice), 1)
        self.assertIn(state.contact.name, advice[0])
        self.assertTrue("inspect with ;" in advice[0] or "is active now" in advice[0])

        _, bartender = _overlay_lines(state, "bartender")
        self.assertEqual(bartender.count(state.bartender.background), 1)
