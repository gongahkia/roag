#!/usr/bin/env python3
"""Store native actor casting using unchanged Vagabonds universal-atlas IDs.

Selections are game roles, not claims about the publisher's species/professions.
Regenerate the runtime atlas with tools/build_game_catalog.py afterwards.
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CLASSES = {
    'adventurer': (0, 107), 'ranger': (1, 119), 'arcanist': (0, 187),
    'berserker': (3, 180), 'sentinel': (0, 113), 'alchemist': (9, 112),
    'shade': (13, 107), 'hookrunner': (2, 196), 'trapper': (15, 119),
    'duelist': (12, 192), 'geomancer': (19, 174), 'spiritbinder': (17, 169),
    'tinkerer': (2, 105), 'stormcaller': (7, 187), 'wayfinder': (21, 108),
}
ENEMIES = {
    'alarm_keeper': (23, 175), 'ash_marshal': (16, 192),
    'banner_captain': (6, 173), 'banner_follower': (4, 107),
    'bastion_bulwark': (19, 113), 'bodyguard_brute': (30, 174),
    'bone_fragment': 'bones', 'bone_splitter': (10, 104),
    'canal_lancer': (11, 108), 'canal_lockmaster': (29, 108),
    'cinder_bellkeeper': (21, 175), 'cinder_mender': (17, 112),
    'crystal_bombardier': (23, 112), 'ember_crucible': (25, 174),
    'ember_moth': 'bee', 'extraction_pursuer': (35, 104),
    'forge_conjurer': (16, 187), 'glass_charger': (22, 108),
    'gloom_strider': (7, 107), 'grave_cantor': (10, 169),
    'gulp_toad': (31, 104), 'haunted_cache': 'chest',
    'heavy_rammer': (30, 104), 'hollow_regent': (28, 169),
    'hollow_wraith': (26, 104), 'index_keeper': (27, 177),
    'mend_acolyte': (21, 112), 'mire_binder': (33, 177),
    'mirror_hexer': (17, 177), 'mossbound_guard': (6, 113),
    'mossbreaker': (29, 174), 'mycelial_matriarch': (18, 187),
    'oath_twin_a': (20, 108), 'oath_twin_b': (20, 107),
    'pack_chorister': (8, 175), 'pin_archer': (4, 119),
    'prism_curator': (24, 187), 'rotating_sentinel': (19, 192),
    'ruin_skitter': 'spider', 'rushwing': 'bat',
    'shield_bearer': (5, 113), 'siege_wisp': (26, 112),
    'silt_worm': (34, 104), 'snare_scribe': (27, 112),
    'spore_herald': (18, 108), 'thorn_warden': (25, 187),
    'thornspitter': (32, 104), 'vault_lurker': (14, 107),
    'ward_weaver': (17, 187), 'web_weaver': (11, 169),
}

def main():
    path = ROOT / 'assets/ithica-expedition-bindings.json'
    document = json.loads(path.read_text())
    manifest = json.loads((ROOT / 'assets/ithica-expedition-sprites.json').read_text())
    definitions = json.loads((ROOT / 'assets/tactics/enemies.json').read_text())['definitions']
    assert set(ENEMIES) == {e['id'].removeprefix('core:enemy/') for e in definitions}
    records = []
    for family, choices in [('class', CLASSES), ('enemy', ENEMIES)]:
        for key, selection in choices.items():
            sprite = document['bindings'][selection] if isinstance(selection, str) else f'urizen-onebit-r{selection[0]:02d}-c{selection[1]:03d}'
            assert sprite in manifest['sprites'], sprite
            binding = f'roag-{family}-{key.replace("_", "-")}'
            document['bindings'][binding] = sprite
            records.append(dict(content_id=f'core:{"actor" if family == "class" else "enemy"}/{key}',
                binding=binding, sprite=sprite, **manifest['sprites'][sprite]))
    path.write_text(json.dumps(document, indent=2, ensure_ascii=False) + '\n')
    report = {'schema_version': 1, 'source': 'Vagabonds universal atlas; original IDs and crops unchanged',
        'casting_note': 'Gameplay casting; publisher species/profession identity remains the evidence in catalog.json.gz.', 'actors': records}
    (ROOT / 'docs/migration/native-actor-bindings.json').write_text(json.dumps(report, indent=2) + '\n')
    print(f'Stored {len(CLASSES)} class and {len(ENEMIES)} enemy atlas bindings.')

if __name__ == '__main__': main()
