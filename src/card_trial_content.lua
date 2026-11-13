-- Tranche 01 tuning only. Equipment maps recipes to existing Adventurer actions.
local C={version=1,seed=101,seeds={101,202,303},hand_size=5,healing_charges=2,
    suits={'Spades','Hearts','Clubs','Diamonds'},
    ranks={'A','2','3','4','5','6','7','8','9','10','J','Q','K'},
    recipes={single=1,pair=2},
    equipment={
        move={name='Boots',single='move',pair='dash'},
        attack={name='Adventurer sword',single='basic_attack',pair='sweeping_slash'},
        heal={name='Healing flask',single=4,pair=8},
    },
    preset_order={'bare','movement','burning'},
    presets={
        bare={name='Bare',description='No starting boons. Compare the card actions alone.',boons={}},
        movement={name='Movement',description='Move to prime Rhythm; leave a locked attack to ready Reversal.',
            boons={{'rhythm_of_steel','uncommon'},{'ring_of_reversal','uncommon'}}},
        burning={name='Burning',description='Ignite adjacent enemies; Shared Agony carries damage between them.',
            boons={{'emberbrand','uncommon'},{'shared_agony','uncommon'}}},
    },
    rewards={
        {boon_id='core:boon/pulse_step',rarity='rare'},
        {boon_id='core:boon/detonation_bloom',rarity='rare'},
        {boon_id='core:boon/grave_dividend',rarity='rare'},
    },
    -- Nine by nine interior. The central pillar has a west and an east route.
    arena={id='trial:map/card_arena',floor_id='trial:floor/card_arena',
        biome_id='core:biome/overgrown_ruins',width=11,height=11,start_x=6,start_y=8,
        rows={'###########','#.........#','#.........#','#.........#',
            '#....#....#','#....#....#','#.........#','#.........#',
            '#.........#','#.........#','###########'}},
    fights={
        {name='The split approach',enemies={
            {'core:enemy/mossbound_guard',4,5},
            {'core:enemy/mossbound_guard',5,5},
            {'core:enemy/thornspitter',8,3},
        }},
        {name='Crossing fire',enemies={
            {'core:enemy/mossbound_guard',3,8},
            {'core:enemy/mossbound_guard',9,4},
            {'core:enemy/thornspitter',3,2},
        }},
    },
}
return C
