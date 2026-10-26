-- M6 authored tactical and class transformation boons. The resolver owns the
-- generic predicates/effects; no content-specific branch belongs in Game.
local E={}
local function parameters(powers,chances)
    local result={}
    local tiers={"common","uncommon","rare","legendary"}
    for i,rarity in ipairs(tiers) do
        result[rarity]={power=powers[i],chance=chances and chances[i] or 0}
    end
    return result
end
local function entry(id,name,description,triggers,predicate,effects,powers,other)
    local def={name=name,description=description,tags={"expansion"},
        triggers=triggers,scope={kind="floor"},predicate=predicate,
        stack_policy="add_power",parameters=parameters(powers),effects=effects}
    for key,value in pairs(other or {}) do def[key]=value end
    E.order[#E.order+1]=id
    E.definitions[id]=def
end
local owner_hit={all={{op="source_owner"},{op="target_hostile"},
    {op="positive"},{op="direct_attack"}}}
local ranger={"core:actor/ranger"}
local arcanist={"core:actor/arcanist"}
local berserker={"core:actor/berserker"}
E.order={}
E.definitions={}

entry("core:boon/stonewake","Stonewake",
    "Leaving sticky bog primes your next direct strike with stone damage.",
    {"TileEntered","DamageTaken"},
    {any={{all={{op="event_kind",value="TileEntered"},{op="source_owner"},
        {op="from_terrain",value="g"}}},
        {all={{op="event_kind",value="DamageTaken"},{op="source_owner"},
            {op="target_hostile"},{op="positive"},{op="direct_attack"},
            {op="primed"}}}}},
    {{kind="prime",duration=400,when={op="event_kind",value="TileEntered"}},
        {kind="damage_target",damage_type="stone",consume_prime=true,
            when={op="event_kind",value="DamageTaken"}}},{1,2,3,4})
entry("core:boon/bramble_compact","Bramble Compact",
    "A bramble contact lashes a nearby hostile for bonus thorn damage.",
    {"TerrainTriggered"},{all={{op="target_owner"},
        {op="terrain_id",value="core:terrain/bramble_bed"}}},
    {{kind="damage_nearest",radius=3,damage_type="thorns"}},{2,3,4,6})
entry("core:boon/froststep_catalyst","Froststep Catalyst",
    "Dashing across ice cuts hostiles beside the traversed path.",
    {"Moved"},{all={{op="source_owner"},{op="ability_id",value="core:ability/dash"},
        {op="path_terrain",value="i"}}},
    {{kind="path_adjacent_hostiles",damage_type="ice"}},{1,2,3,4})
entry("core:boon/fracture_dividend","Fracture Dividend",
    "Breaking a barricade scatters a one-cell shard burst.",
    {"TerrainBroken"},{op="source_owner"},
    {{kind="area",radius=1,damage_type="shard"}},{1,2,3,4})
entry("core:boon/mist_veil","Mist Veil",
    "Direct hits from obscuring mist gain surprise damage.",
    {"DamageTaken"},{all={owner_hit,{op="source_on_terrain",value="m"}}},
    {{kind="damage_target",damage_type="mist"}},{1,2,3,4})
entry("core:boon/crystal_harmony","Crystal Harmony",
    "Lightning hits beside a resonant crystal arc to another nearby foe.",
    {"DamageTaken"},{all={{op="source_owner"},{op="target_hostile"},
        {op="positive"},{op="damage_type",value="lightning"},
        {op="target_near_terrain",value="c"}}},
    {{kind="damage_nearest",radius=3,damage_type="lightning"}},{1,2,3,4})
entry("core:boon/sunderers_mark","Sunderer's Mark",
    "Repeated direct hits on one foe build a short capped fracture mark.",
    {"DamageTaken"},owner_hit,
    {{kind="mark_damage",duration=400,threshold=2,cap=3,
        damage_type="physical"}},{1,2,3,4})
entry("core:boon/counterweight","Counterweight",
    "A fully blocked hostile hit primes bonus damage on your next special hit.",
    {"DamageBlocked","DamageTaken"},
    {any={{all={{op="event_kind",value="DamageBlocked"},{op="target_owner"},
        {op="source_hostile"}}},
        {all={{op="event_kind",value="DamageTaken"},owner_hit,
            {op="special_attack"},{op="primed"}}}}},
    {{kind="prime",duration=400,when={op="event_kind",value="DamageBlocked"}},
        {kind="damage_target",damage_type="physical",consume_prime=true,
            when={op="event_kind",value="DamageTaken"}}},{2,3,4,6})
entry("core:boon/bloodrush","Bloodrush",
    "Credited kills grant a brief scheduled-action speed surge.",
    {"EntityKilled"},{all={{op="source_owner"},{op="target_hostile"}}},
    {{kind="speed_boost",duration=300}},{10,15,20,25})
entry("core:boon/pain_dividend","Pain Dividend",
    "Taking real damage primes bonus damage on your next direct hit.",
    {"DamageTaken"},
    {any={{all={{op="target_owner"},{op="source_hostile"},{op="positive"}}},
        {all={owner_hit,{op="primed"}}}}},
    {{kind="prime",duration=400,when={op="target_owner"}},
        {kind="damage_target",damage_type="physical",consume_prime=true,
            when={op="source_owner"}}},{1,2,3,4})
entry("core:boon/executioners_thread","Executioner's Thread",
    "Direct hits against foes at or below half health cut deeper.",
    {"DamageTaken"},{all={owner_hit,{op="target_hp_percent_before",max=50}}},
    {{kind="damage_target",damage_type="physical"}},{1,2,3,4})
entry("core:boon/shared_agony","Shared Agony",
    "Damaging a Burning foe shares pain with adjacent Burning foes.",
    {"DamageTaken"},{all={{op="positive"},{op="target_hostile"},
        {op="burning_pair"}}},
    {{kind="status_neighbors",status="burning",damage_type="fire"}},{1,2,3,4})
entry("core:boon/windup_hunter","Windup Hunter",
    "A hostile locking a strike on your cell takes a retaliatory hit.",
    {"WindupStarted"},{all={{op="source_hostile"},{op="visible_source"},
        {op="owner_in_locked_cells"},{op="unique_windup"}}},
    {{kind="damage_source",damage_type="physical"}},{1,2,3,4},
    {scope={kind="radius",radius=9}})
entry("core:boon/crossfire_omen","Crossfire Omen",
    "Overlapping hostile windups burst around the newest attacker.",
    {"WindupStarted"},{all={{op="source_hostile"},{op="visible_source"},
        {op="overlapping_windups"},{op="unique_windup"}}},
    {{kind="area",radius=1,damage_type="arcane"}},{1,2,3,4},
    {scope={kind="radius",radius=9}})
entry("core:boon/pursuit_spark","Pursuit Spark",
    "A hostile moving closer within six tiles draws a spark.",
    {"Moved"},{all={{op="source_hostile"},{op="visible_source"},
        {op="approaching_owner"}}},
    {{kind="damage_source",damage_type="lightning"}},{1,2,3,4},
    {scope={kind="radius",radius=6}})
entry("core:boon/ordered_ranks","Ordered Ranks",
    "Direct hits in a line of three living hostiles gain formation damage.",
    {"DamageTaken"},{all={owner_hit,{op="aligned_hostiles",min=3}}},
    {{kind="damage_target",damage_type="physical"}},{1,2,3,4})
entry("core:boon/far_witness","Far Witness",
    "Hostile damage observed four to nine tiles away sends a distant spark.",
    {"DamageTaken"},{all={{op="positive"},{op="target_hostile"},
        {op="source_not_owner"},{op="visible_source"},{op="visible_event"},
        {op="event_distance",min=4,max=9}}},
    {{kind="damage_target",damage_type="lightning"}},{1,2,3,4})
entry("core:boon/betrayal_ward","Betrayal Ward",
    "Enemy friendly fire grants a short one-hit damage ward.",
    {"DamageTaken"},{all={{op="positive"},{op="target_hostile"},
        {op="source_target_same_faction"},{op="source_not_owner"}}},
    {{kind="ward",duration=300}},{1,2,3,4})
entry("core:boon/rhythm_of_steel","Rhythm of Steel",
    "Moving primes one bonus direct strike; attacks consume the rhythm.",
    {"Moved","DamageTaken"},
    {any={{all={{op="event_kind",value="Moved"},{op="source_owner"}}},
        {all={{op="event_kind",value="DamageTaken"},owner_hit,
            {op="primed"}}}}},
    {{kind="prime",duration=250,when={op="event_kind",value="Moved"}},
        {kind="damage_target",damage_type="physical",consume_prime=true,
            when={op="event_kind",value="DamageTaken"}}},{1,2,3,4})
entry("core:boon/cluster_rupture","Cluster Rupture",
    "Each three distinct hostiles damaged in one chain release a blast.",
    {"DamageTaken"},{all={{op="positive"},{op="target_hostile"},
        {op="distinct_chain_group",size=3}}},
    {{kind="area",radius=1,damage_type="explosion"}},{1,2,3,4})

entry("core:boon/splitflight","Splitflight",
    "Ranger Bow Shot branches into two nearby clear hostile cells.",{},nil,{},
    {1,2,3,4},{eligible_classes=ranger,
        modifiers={{ability_id="core:ability/ranger_shot",kind="branch_near"},
            {ability_id="core:ability/ranger_shot",kind="add_damage"}},incompatible={}})
entry("core:boon/recurve","Recurve",
    "Ranger Piercing Volley extends one more clear cell.",{},nil,{},
    {1,2,3,4},{eligible_classes=ranger,
        modifiers={{ability_id="core:ability/ranger_volley",kind="line_extend"},
            {ability_id="core:ability/ranger_volley",kind="add_damage"}},incompatible={}})
entry("core:boon/expanding_sigil","Expanding Sigil",
    "Arcanist Fire Sigil gains an outer two-cell ring.",{},nil,{},
    {1,2,3,4},{eligible_classes=arcanist,
        modifiers={{ability_id="core:ability/arcanist_sigil",kind="area_expand"},
            {ability_id="core:ability/arcanist_sigil",kind="add_damage"}},incompatible={}})
entry("core:boon/alchemical_flip","Alchemical Flip",
    "Arc Bolt striking Burning links to nearby Burning foes.",{},nil,{},
    {1,2,3,4},{eligible_classes=arcanist,
        modifiers={{ability_id="core:ability/arcanist_bolt",kind="status_chain"},
            {ability_id="core:ability/arcanist_bolt",kind="add_damage"}},incompatible={}})
entry("core:boon/earthshaker","Earthshaker",
    "Berserker Crushing Blow shocks adjacent cells beyond its first impact.",
    {},nil,{}, {1,2,3,4},{eligible_classes=berserker,
        modifiers={{ability_id="core:ability/berserker_crush",kind="shockwave"},
            {ability_id="core:ability/berserker_crush",kind="add_damage"}},incompatible={}})
entry("core:boon/battle_march","Battle March",
    "Berserker Heavy Lunge primes a stronger next Crushing Blow.",
    {"Moved","DamageTaken"},
    {any={{all={{op="event_kind",value="Moved"},{op="source_owner"},
        {op="ability_id",value="core:ability/dash"}}},
        {all={{op="event_kind",value="DamageTaken"},owner_hit,
            {op="ability_id",value="core:ability/berserker_crush"},
            {op="primed"}}}}},
    {{kind="prime",duration=300,when={op="event_kind",value="Moved"}},
        {kind="damage_target",damage_type="physical",consume_prime=true,
            when={op="event_kind",value="DamageTaken"}}},{1,2,3,4},
    {eligible_classes=berserker,
        modifiers={{ability_id="core:ability/dash",kind="dash_prime"}},
        incompatible={}})

local units={
    stonewake="stone damage on primed strike",
    bramble_compact="thorn damage per bramble contact",
    froststep_catalyst="ice damage per path-adjacent foe",
    fracture_dividend="shard damage in burst",
    mist_veil="bonus mist damage from obscuring tile",
    crystal_harmony="bonus lightning arc damage",
    sunderers_mark="damage at mark count 2–3",
    counterweight="bonus special-hit damage after block",
    bloodrush="temporary speed for 300 world time",
    pain_dividend="bonus next-hit damage after harm",
    executioners_thread="bonus wounded-target damage",
    shared_agony="damage to each adjacent Burning foe",
    windup_hunter="attacker retaliation damage",
    crossfire_omen="overlap burst damage",
    pursuit_spark="approaching-enemy spark damage",
    ordered_ranks="formation bonus damage",
    far_witness="distant witnessed damage",
    betrayal_ward="damage absorbed on next positive hit",
    rhythm_of_steel="bonus next-hit damage after movement",
    cluster_rupture="three-target chain burst damage",
    splitflight="bow damage bonus; branches stay at two",
    recurve="volley damage bonus; adds one clear cell",
    expanding_sigil="sigil damage bonus; two-cell ring",
    alchemical_flip="bolt damage bonus; Burning link",
    earthshaker="crush damage bonus; adjacent shockwave",
    battle_march="bonus Crush damage after Heavy Lunge",
}
for _,id in ipairs(E.order) do
    local key=id:match("/([^/]+)$")
    E.definitions[id].unit=assert(units[key],"missing boon magnitude unit")
end
for _,key in ipairs({"stonewake","bramble_compact","froststep_catalyst",
    "fracture_dividend","mist_veil","crystal_harmony","sunderers_mark",
    "counterweight","bloodrush","pain_dividend","executioners_thread",
    "rhythm_of_steel","battle_march"}) do
    E.definitions["core:boon/"..key].scope={kind="owner"}
end

assert(#E.order==26,"M6 needs exactly 26 new boons")
return E
