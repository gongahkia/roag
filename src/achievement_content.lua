-- Authored, stable permanent progression content. Runtime progress lives elsewhere.
local A={version="roag-achievements/1"}
A.order={"core:achievement/first_guardian","core:achievement/uncovered_truth",
    "core:achievement/chain_reaction","core:achievement/regents_fall",
    "core:achievement/first_blood","core:achievement/pathfinder",
    "core:achievement/treasure_hunter","core:achievement/coin_purse",
    "core:achievement/first_level","core:achievement/veteran",
    "core:achievement/boon_bearer","core:achievement/stonebreaker",
    "core:achievement/ore_seeker","core:achievement/gate_opener",
    "core:achievement/key_bearer","core:achievement/warded",
    "core:achievement/torchbearer","core:achievement/long_heal",
    "core:achievement/pressure_survivor","core:achievement/deeper_road",
    "core:achievement/sanctum_reached","core:achievement/regent_awakened"}
A.definitions={
    [A.order[1]]={name="First Guardian",hint="Defeat a Thorn Warden.",
        condition="EliteDefeated in an eligible run",reward="core:boon/guardians_vow"},
    [A.order[2]]={name="Uncovered Truth",hint="Reveal a hidden cache through ordinary actions.",
        condition="SecretDiscovered in an eligible run",reward="core:boon/prospectors_heart"},
    [A.order[3]]={name="Chain Reaction",hint="Activate three different boons in one causal action chain.",
        condition="Three distinct BoonActivated IDs in one chain during one committed action",
        reward="core:boon/circuit_chorus"},
    [A.order[4]]={name="Regent's Fall",hint="Defeat the Hollow Regent and survive.",
        condition="Canonical Victory after Regent death, excluding mutual lethal Defeat",
        reward="core:boon/crownfire"},
    [A.order[5]]={name="First Blood",hint="Defeat your first enemy.",condition="Player kills a hostile"},
    [A.order[6]]={name="Pathfinder",hint="Reach another floor.",condition="FloorChanged"},
    [A.order[7]]={name="Treasure Hunter",hint="Claim a chest reward.",condition="ChestClaimed"},
    [A.order[8]]={name="Coin Purse",hint="Carry at least 50 gold.",condition="CurrencyCollected to 50 gold"},
    [A.order[9]]={name="Growing Stronger",hint="Reach level 2.",condition="LevelUp to level 2"},
    [A.order[10]]={name="Veteran",hint="Reach level 5.",condition="LevelUp to level 5"},
    [A.order[11]]={name="Boon Bearer",hint="Claim a boon.",condition="BoonGranted"},
    [A.order[12]]={name="Stonebreaker",hint="Break a barricade.",condition="TerrainBroken"},
    [A.order[13]]={name="Ore Seeker",hint="Break an ore deposit.",condition="OreBroken"},
    [A.order[14]]={name="Gate Opener",hint="Open a timed gate.",condition="TimedGateOpened"},
    [A.order[15]]={name="Key Bearer",hint="Find a vault key.",condition="VaultKeyCollected"},
    [A.order[16]]={name="Warded",hint="Collect a ward flask.",condition="WardFlaskCollected"},
    [A.order[17]]={name="Torchbearer",hint="Light a brazier.",condition="BrazierLit"},
    [A.order[18]]={name="Second Wind",hint="Recover health from a supply drop.",condition="SupplyCollected"},
    [A.order[19]]={name="Under Pressure",hint="Face an arriving pursuer.",condition="PursuerArrived"},
    [A.order[20]]={name="Deeper Road",hint="Enter the second stage.",condition="StageEntered at stage 2"},
    [A.order[21]]={name="Sanctum Reached",hint="Enter the Final Sanctum.",condition="FinalSanctumEntered"},
    [A.order[22]]={name="Regent Awakened",hint="Force the Hollow Regent into phase two.",condition="RegentPhaseChanged"},
}
function A.get(id) return A.definitions[id] end
function A.validate()
    local Boons=require("src.boon_content")
    local seen,rewards={},{}
    for _,id in ipairs(A.order) do
        assert(type(id)=="string" and id:match("^core:achievement/") and not seen[id],
            "invalid achievement ID")
        seen[id]=true
        local d=assert(A.get(id),"missing achievement")
        assert(type(d.name)=="string" and #d.name>0 and type(d.hint)=="string"
            and type(d.condition)=="string" and
            (d.reward==nil or (Boons.get(d.reward) and not rewards[d.reward])),
            "invalid achievement content")
        if d.reward then rewards[d.reward]=true end
    end
    for id in pairs(A.definitions) do assert(seen[id],"unlisted achievement") end
    return true
end
return A
