-- Small experimental deck; no scripting language or normal-game equipment changes.
local C={version=2,seed=101,seeds={101,202,303},hand_size=3,healing_charges=2,
    deck={{kind='dash',count=4},{kind='sweep',count=4},{kind='mend',count=1}},
    opening={'dash','sweep','mend'},
    techniques={
        dash={name='Dash',action='dash',icon='core:ui/ability_move_icon',
            description='Travel 2 tiles straight, stopping at blockers. Terrain may extend travel.'},
        sweep={name='Sweep',action='sweeping_slash',icon='core:ui/ability_slash_icon',
            description='Strike a three-cell arc. Base damage 3 before defence.'},
        mend={name='Mend',action='trial_heal',heal=4,icon='core:ui/status_heal_icon',
            description='Restore 4 HP. Uses 1 charge. Click this card to heal.'},
        knight_hop={name='Knight Hop',action='knight_hop',icon='core:ui/ability_move_icon',
            description='Jump in an L. Ignores intervening tiles. Land on a free tile.'},
    },
    knight_offsets={{-2,-1},{-1,-2},{1,-2},{2,-1},{2,1},{1,2},{-1,2},{-2,1}},
    upgrade={id='trial:upgrade/knight_boots',name='Knight Boots',
        icon='core:boon/pulse_step/icon',description='Your Dash cards become Knight Hop cards.'},
    preset_order={'bare','movement','burning'},
    presets={
        bare={name='Bare',description='No starting boons. Compare the card actions alone.',boons={}},
        movement={name='Movement',description='Move to prime Rhythm; leave a locked attack to ready Reversal.',
            boons={{'rhythm_of_steel','uncommon'},{'ring_of_reversal','uncommon'}}},
        burning={name='Burning',description='Ignite adjacent enemies; Shared Agony carries damage between them.',
            boons={{'emberbrand','uncommon'},{'shared_agony','uncommon'}}},
    },
    rewards={
        {upgrade_id='trial:upgrade/knight_boots'},
        {boon_id='core:boon/pulse_step',rarity='rare'},
        {boon_id='core:boon/detonation_bloom',rarity='rare'},
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
