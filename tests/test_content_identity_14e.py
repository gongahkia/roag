from __future__ import annotations

import json
from pathlib import Path
import tempfile
import unittest

from jomon.people import normalize_personal_return_state, record_personal_return
from jomon.state import combat_seed_identity, create_world, game_state_from_dict


class ContentIdentity14ETests(unittest.TestCase):
    def test_combat_seed_identity_ignores_rendered_threat_name(self):
        state = create_world("14e combat identity")
        for threat in state.threats[:3]:
            token = combat_seed_identity(threat)
            threat.name = "rewritten selected-pack threat"
            self.assertEqual(combat_seed_identity(threat), token)

    def test_personal_return_recovery_is_exact_and_runtime_is_state_only(self):
        state = create_world("14e personal recovery")
        person = state.household[0]
        person.memories.append("Courier return: totally unrelated prose")
        normalize_personal_return_state(state)
        self.assertNotIn(f"personal-return:{person.id}", state.vessel_changes)
        person.memories.append("Courier return: survived working passage through Hearthford.")
        normalize_personal_return_state(state)
        self.assertEqual(state.vessel_changes[f"personal-return:{person.id}"], 1)
        person.memories.append("Courier return: totally unrelated prose")
        record_personal_return(state, person, "hearthford")
        self.assertEqual(state.vessel_changes[f"personal-return:{person.id}"], 2)

    def test_people_catalog_prose_is_not_runtime_authority(self):
        # A new state is built from the validated selected presentation fields;
        # raw people.json text no longer supplies household or visitor wording.
        state = create_world("14e people presentation")
        self.assertTrue(state.household[0].name)
        self.assertTrue(state.visitors[0].background)
        self.assertTrue(state.region.condition)

    def test_normal_load_does_not_add_zero_personal_return_fields(self):
        state = create_world("14e no zero migration")
        payload = state.to_dict()
        restored = game_state_from_dict(payload)
        self.assertEqual(restored.to_dict(), payload)

    def test_people_slots_survive_pack_switch_and_rerender_current_state(self):
        """Pack text changes without regenerating selected people identities."""
        import os
        import subprocess
        from tests.test_content_packs import alternate_pack
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "alternate")
            # The fixture helper deliberately changes a seed word for unrelated
            # presentation tests; restore it to keep this switch mechanical-only.
            (root / "data" / "world_text.json").write_text((Path(__file__).parents[1] / "jomon" / "data" / "world_text.json").read_text(encoding="utf-8"), encoding="utf-8")
            characters = json.loads((root / "characters.json").read_text(encoding="utf-8"))
            characters["people"]["first_names"]["first_0"] = "Alternate"
            characters["people"]["contact_names"]["contact_0"] = "Alternate Contact"
            characters["people"]["contexts"]["context_0"]["condition"] = "Alternate current context."
            characters["people"]["recruits"]["recruit-maelin"]["name"] = "Alternate Recruit"
            (root / "characters.json").write_text(json.dumps(characters), encoding="utf-8")
            save = Path(directory) / "state.json"
            source = '''
import json
from pathlib import Path
from jomon.save import save_game
from jomon.state import create_world
s=create_world("14e-pack-switch")
save_game(s, Path(r"%s"))
print(json.dumps({"household": [(p.id,p.role,p.given_name_slot,p.family_name_slot,sorted(p.relationships.items())) for p in s.household], "visitor": [(p.id,p.people_presentation_id) for p in s.visitors], "contact": (s.contact.id,s.contact.name_slot,s.contact.role_slot), "context": s.region.people_context_slot}))
''' % save
            before = json.loads(subprocess.check_output(["python", "-c", source], text=True))
            environment = dict(os.environ, JOMON_CONTENT_PACK=str(root))
            target = '''
import json
from pathlib import Path
from jomon.save import load_game, save_game
s=load_game(Path(r"%s"))
print(json.dumps({"household": [(p.id,p.role,p.given_name_slot,p.family_name_slot,sorted(p.relationships.items())) for p in s.household], "visitor": [(p.id,p.people_presentation_id) for p in s.visitors], "contact": (s.contact.id,s.contact.name_slot,s.contact.role_slot), "context": s.region.people_context_slot, "rendered": [s.household[0].name,s.contact.name,s.region.condition,s.visitors[0].name]}))
''' % save
            after = json.loads(subprocess.check_output(["python", "-c", target], text=True, env=environment))
        self.assertEqual(after["household"], before["household"])
        self.assertEqual(after["visitor"], before["visitor"])
        self.assertEqual(after["contact"], before["contact"])
        self.assertEqual(after["context"], before["context"])
        self.assertTrue(any("Alternate" in value for value in after["rendered"]))

    def test_combat_seed_keeps_default_damage_locations_for_each_path(self):
        from jomon.actions import _hit_location, legacy_combat_damage_seed
        state = create_world("14e combat continuity")
        by_archetype = {threat.archetype_id: threat for threat in state.threats}
        cases = (
            ("hearth-roof-keeper", "threat.ranged", {"weapon": "longbow"}, "watch-roof crossbow keeper"),
            ("hearth-mill-protector", "threat.melee", {}, "displaced mill levy"),
            ("hearth-reed-boar", "threat.animal_charge", {}, "bristleback reed boar"),
        )
        for archetype, source_id, values, historical_name in cases:
            with self.subTest(archetype=archetype):
                threat = by_archetype[archetype]
                self.assertEqual(combat_seed_identity(threat), historical_name)
                old_seed = legacy_combat_damage_seed(source_id, threat=historical_name, **values)
                new_seed = legacy_combat_damage_seed(source_id, threat=combat_seed_identity(threat), **values)
                self.assertEqual(new_seed, old_seed)
                self.assertEqual(_hit_location(state, "pierce", new_seed), _hit_location(state, "pierce", old_seed))
                # The same source token produces the same actual damage state,
                # not merely a matching helper return value.
                from copy import deepcopy
                from jomon.actions import apply_damage
                old_state, new_state = deepcopy(state), deepcopy(state)
                apply_damage(old_state, 2, "historical", damage_kind="pierce", source_seed=old_seed)
                apply_damage(new_state, 2, "historical", damage_kind="pierce", source_seed=new_seed)
                self.assertEqual((old_state.courier.health, old_state.courier.injury, old_state.courier.injuries), (new_state.courier.health, new_state.courier.injury, new_state.courier.injuries))
                threat.name = "alternate visible wording"
                renamed_seed = legacy_combat_damage_seed(source_id, threat=combat_seed_identity(threat), **values)
                self.assertEqual(renamed_seed, old_seed)
                self.assertEqual(_hit_location(state, "pierce", renamed_seed), _hit_location(state, "pierce", old_seed))

    def test_people_presentation_does_not_change_mechanical_fingerprint(self):
        import os
        import subprocess
        from tests.test_content_packs import alternate_pack
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "alternate")
            (root / "data" / "world_text.json").write_text((Path(__file__).parents[1] / "jomon" / "data" / "world_text.json").read_text(encoding="utf-8"), encoding="utf-8")
            characters = json.loads((root / "characters.json").read_text(encoding="utf-8"))
            characters["people"]["first_names"]["first_0"] = "Different"
            characters["people"]["recruits"]["recruit-maelin"]["background"] = "Different current fiction."
            (root / "characters.json").write_text(json.dumps(characters), encoding="utf-8")
            command = ["python", "-c", "from jomon.mechanical_compatibility import main_world_mechanical_fingerprint; print(main_world_mechanical_fingerprint())"]
            default = subprocess.check_output(command, text=True).strip()
            alternate = subprocess.check_output(command, text=True, env=dict(os.environ, JOMON_CONTENT_PACK=str(root))).strip()
        self.assertEqual(default, alternate)

    def test_current_item_descriptions_are_pack_authoritative(self):
        import os
        import subprocess
        from tests.test_content_packs import alternate_pack
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "alternate")
            (root / "data" / "world_text.json").write_text((Path(__file__).parents[1] / "jomon" / "data" / "world_text.json").read_text(encoding="utf-8"), encoding="utf-8")
            items = json.loads((root / "items.json").read_text(encoding="utf-8"))
            slot = "item.goods_050"
            items["items"][slot]["description"] = "Alternate passive description."
            (root / "items.json").write_text(json.dumps(items), encoding="utf-8")
            command = ["python", "-c", "from jomon.inventory import item_spec; print(item_spec('passive:rain cape').description)"]
            default = subprocess.check_output(command, text=True).strip()
            alternate = subprocess.check_output(command, text=True, env=dict(os.environ, JOMON_CONTENT_PACK=str(root))).strip()
        self.assertNotEqual(default, alternate)
        self.assertEqual(alternate, "Alternate passive description.")

    def test_special_hearthford_threat_rerenders_from_its_stable_identity(self):
        import os
        import subprocess
        from tests.test_content_packs import alternate_pack

        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "alternate")
            (root / "data" / "world_text.json").write_text(
                (Path(__file__).parents[1] / "jomon" / "data" / "world_text.json").read_text(encoding="utf-8"),
                encoding="utf-8",
            )
            ecology = json.loads((root / "ecology_text.json").read_text(encoding="utf-8"))
            ecology["text"]["ecology.special.wheel-train.name"] = "alternate sluice engine"
            ecology["text"]["ecology.special.wheel-train.goal"] = "guard the alternate channel"
            ecology["text"]["ecology.special.floodgate-claimant.name"] = "alternate gate speaker"
            ecology["text"]["ecology.special.floodgate-claimant.goal"] = "open the alternate spillway"
            ecology["text"]["ecology.special.floodgate-claimant.capability"] = "alternate flood signal"
            (root / "ecology_text.json").write_text(json.dumps(ecology), encoding="utf-8")
            seed = "14e special hearthford threat"
            command = '''
import json
from jomon.state import create_world
s=create_world(%r)
t=next(x for x in s.threats if x.id in {"wheel-train", "floodgate-claimant"})
print(json.dumps({"mechanics": [t.id,t.archetype_id,t.profile,t.health,t.max_health,t.goal_id,t.morale,t.status], "display": [t.name,t.goal,t.capabilities]}))
''' % seed
            before = json.loads(subprocess.check_output(["python", "-c", command], text=True))
            after = json.loads(subprocess.check_output(
                ["python", "-c", command], text=True,
                env=dict(os.environ, JOMON_CONTENT_PACK=str(root)),
            ))
        self.assertEqual(before["mechanics"], after["mechanics"])
        self.assertNotEqual(before["display"], after["display"])

    def test_ammunition_description_uses_pack_semantic_identity(self):
        import os
        import subprocess
        from tests.test_content_packs import alternate_pack

        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "alternate")
            (root / "data" / "world_text.json").write_text(
                (Path(__file__).parents[1] / "jomon" / "data" / "world_text.json").read_text(encoding="utf-8"),
                encoding="utf-8",
            )
            equipment = json.loads((root / "equipment_text.json").read_text(encoding="utf-8"))
            equipment["text"]["equipment.ammunition.bolts.description"] = "Alternate bolt account."
            (root / "equipment_text.json").write_text(json.dumps(equipment), encoding="utf-8")
            command = ["python", "-c", "from jomon.inventory import item_spec; print(item_spec('consumable:crossbow bolts').description)"]
            default = subprocess.check_output(command, text=True).strip()
            alternate = subprocess.check_output(command, text=True, env=dict(os.environ, JOMON_CONTENT_PACK=str(root))).strip()
        self.assertNotEqual(default, alternate)
        self.assertEqual(alternate, "Alternate bolt account.")

    def test_history_catalog_prose_is_not_current_ledger_authority(self):
        from jomon import regional_history
        from jomon.regional_history import ledger_lines, reconcile_network

        state = create_world("14e history presentation authority")
        reconcile_network(state)
        before = "\n".join(ledger_lines(state))
        original = regional_history.WORKING_ACCOUNTS["hearthford"]
        regional_history.WORKING_ACCOUNTS["hearthford"] = (
            original[0], original[1], "Raw catalog override", *original[3:]
        )
        try:
            reconcile_network(state)
            after = "\n".join(ledger_lines(state))
        finally:
            regional_history.WORKING_ACCOUNTS["hearthford"] = original
        self.assertEqual(before, after)
        self.assertNotIn("Raw catalog override", after)
