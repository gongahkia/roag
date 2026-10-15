-- Authored, stable permanent progression content. Runtime progress lives elsewhere.
local A={version="roag-achievements/1"}
A.order={"core:achievement/first_guardian","core:achievement/uncovered_truth",
    "core:achievement/chain_reaction","core:achievement/regents_fall"}
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
            and type(d.condition)=="string" and Boons.get(d.reward)
            and not rewards[d.reward],"invalid achievement content")
        rewards[d.reward]=true
    end
    for id in pairs(A.definitions) do assert(seen[id],"unlisted achievement") end
    return true
end
return A
