-- Tranche 03 playtest constants. Existing combat remains authoritative.
local C={version=3,seed=101,seeds={101,202,303},hand_size=8,exchange_limit=3,retain_limit=2,
    healing_charges=2,fixed_stamina=4,deck_add_count=3,
    payouts={single=3,pair=4,two_pair=5,three_kind=6,straight=7,flush=8,full_house=9,four_kind=10,straight_flush=11},
    poker_order={'single','pair','two_pair','three_kind','straight','flush','full_house','four_kind','straight_flush'},
    poker_names={single='Single',pair='Pair',two_pair='Two pair',three_kind='Three of a kind',straight='Straight',
        flush='Flush',full_house='Full house',four_kind='Four of a kind',straight_flush='Straight flush'},
    stamina_costs={move=1,strike=2,dash=2,sweep=3,mend=2,recover=0},
    actions={
        move={name='Step',kind='move',description='One cardinal tile. Bumping an enemy is a Strike.'},
        strike={name='Strike',kind='basic_attack',description='Aimed Sword Strike. Base damage 4 before defence.'},
        dash={name='Dash',kind='dash',description='Travel 2 tiles straight; stops at blockers. Terrain may extend travel.'},
        sweep={name='Sweep',kind='sweeping_slash',description='Strike a three-cell arc. Base damage 3 before defence.'},
        mend={name='Mend',kind='trial_heal',heal=4,description='Restore 4 HP, capped at maximum. Uses 1 healing charge.'},
        recover={name='Recover',kind='wait',description='Lose leftover stamina. Keep marked cards. Wait once; enemies advance.'},
        knight_hop={name='Knight Hop',kind='knight_hop',description='Jump in an L. Ignores intervening tiles. Land on a free tile.'},
    },
    knight_offsets={{-2,-1},{-1,-2},{1,-2},{2,-1},{2,1},{1,2},{-1,2},{-2,1}},
    upgrade={id='trial:upgrade/knight_boots',name='Knight Boots',
        icon='core:boon/pulse_step/icon',description='Your Dash action becomes Knight Hop.'},
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
