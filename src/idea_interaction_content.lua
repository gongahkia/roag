-- Approved finite items and room services. Visual IDs do not encode sheet coordinates.
local C={version=1,action_cost=100,fixture_delay=100,fixture_damage=4,
    service_cap=2,assembly_cap=1,utility_range=3,return_range=4}
C.order={'offering_font','ration_cache','cleanse_flask','escape_scroll','return_token',
    'charm_well','attunement_forge','travelling_trader','memorial','bell',
    'falling_fixture','route_sign','camp','archive_contract'}
C.names={offering_font='Offering Font',ration_cache='Ration Cache',
    cleanse_flask='Cleansing Flask',escape_scroll='Escape Scroll',return_token='Return Token',
    charm_well='Charm Well',attunement_forge='Attunement Forge',travelling_trader='Supply Trader',
    memorial='Memorial',bell='Bell',bell_clue='Bell Plaque',falling_fixture='Falling Fixture',
    route_sign='Route Sign',camp='Camp',archive_contract='Archivist Contract',return_marker='Token Marker'}
C.art={
    offering_font={'tiny',74,690},ration_cache={'rpg',23,650},
    cleanse_flask={'tiny',114,580},escape_scroll={'rpg',898,139},return_token={'onebit',574,574},
    charm_well={'rpg',853,435},attunement_forge={'rpg',13,400},
    travelling_trader={'rpg',239,768},memorial={'rpg',564,72},
    bell={'rpg',681,867},bell_clue={'rpg',896,392},
    -- 1-Bit 549 is explicitly a falling stone slab, not a hanging chair.
    falling_fixture={'rpg',475,549},route_sign={'rpg',19,674},
    camp={'micro',117,508},archive_contract={'rpg',901,392},return_marker={'onebit',574,574},
}
function C.binding(kind)return 'core:object/interaction_'..kind end
return C
