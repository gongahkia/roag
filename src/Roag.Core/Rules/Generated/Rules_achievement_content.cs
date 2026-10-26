// Static C# port of src/achievement_content.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 9efa72a89b6d14e0bb0a42fd560b43244fc5d1d0759aab7f79ac8e519b9f6c2b
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_achievement_content { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"version", (V)"roag-achievements/1"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:achievement/first_guardian"), new Entry((V)2, (V)"core:achievement/uncovered_truth"), new Entry((V)3, (V)"core:achievement/chain_reaction"), new Entry((V)4, (V)"core:achievement/regents_fall"), new Entry((V)5, (V)"core:achievement/first_blood"), new Entry((V)6, (V)"core:achievement/pathfinder"), new Entry((V)7, (V)"core:achievement/treasure_hunter"), new Entry((V)8, (V)"core:achievement/coin_purse"), new Entry((V)9, (V)"core:achievement/first_level"), new Entry((V)10, (V)"core:achievement/veteran"), new Entry((V)11, (V)"core:achievement/boon_bearer"), new Entry((V)12, (V)"core:achievement/stonebreaker"), new Entry((V)13, (V)"core:achievement/ore_seeker"), new Entry((V)14, (V)"core:achievement/gate_opener"), new Entry((V)15, (V)"core:achievement/key_bearer"), new Entry((V)16, (V)"core:achievement/warded"), new Entry((V)17, (V)"core:achievement/torchbearer"), new Entry((V)18, (V)"core:achievement/long_heal"), new Entry((V)19, (V)"core:achievement/pressure_survivor"), new Entry((V)20, (V)"core:achievement/deeper_road"), new Entry((V)21, (V)"core:achievement/sanctum_reached"), new Entry((V)22, (V)"core:achievement/regent_awakened"))));
var obj_4 = v_2;
V key_5 = (V)"order";
R.Set(obj_4, key_5, values_3[0]);
var values_6 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Get(R.Get(v_2, (V)"order"), (V)1.0d), R.Table(new Entry((V)"name", (V)"First Guardian"), new Entry((V)"hint", (V)"Defeat a Thorn Warden."), new Entry((V)"condition", (V)"EliteDefeated in an eligible run"), new Entry((V)"reward", (V)"core:boon/guardians_vow"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)2.0d), R.Table(new Entry((V)"name", (V)"Uncovered Truth"), new Entry((V)"hint", (V)"Reveal a hidden cache through ordinary actions."), new Entry((V)"condition", (V)"SecretDiscovered in an eligible run"), new Entry((V)"reward", (V)"core:boon/prospectors_heart"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)3.0d), R.Table(new Entry((V)"name", (V)"Chain Reaction"), new Entry((V)"hint", (V)"Activate three different boons in one causal action chain."), new Entry((V)"condition", (V)"Three distinct BoonActivated IDs in one chain during one committed action"), new Entry((V)"reward", (V)"core:boon/circuit_chorus"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)4.0d), R.Table(new Entry((V)"name", (V)"Regent's Fall"), new Entry((V)"hint", (V)"Defeat the Hollow Regent and survive."), new Entry((V)"condition", (V)"Canonical Victory after Regent death, excluding mutual lethal Defeat"), new Entry((V)"reward", (V)"core:boon/crownfire"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)5.0d), R.Table(new Entry((V)"name", (V)"First Blood"), new Entry((V)"hint", (V)"Defeat your first enemy."), new Entry((V)"condition", (V)"Player kills a hostile"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)6.0d), R.Table(new Entry((V)"name", (V)"Pathfinder"), new Entry((V)"hint", (V)"Reach another floor."), new Entry((V)"condition", (V)"FloorChanged"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)7.0d), R.Table(new Entry((V)"name", (V)"Treasure Hunter"), new Entry((V)"hint", (V)"Claim a chest reward."), new Entry((V)"condition", (V)"ChestClaimed"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)8.0d), R.Table(new Entry((V)"name", (V)"Coin Purse"), new Entry((V)"hint", (V)"Carry at least 50 gold."), new Entry((V)"condition", (V)"CurrencyCollected to 50 gold"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)9.0d), R.Table(new Entry((V)"name", (V)"Growing Stronger"), new Entry((V)"hint", (V)"Reach level 2."), new Entry((V)"condition", (V)"LevelUp to level 2"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)10.0d), R.Table(new Entry((V)"name", (V)"Veteran"), new Entry((V)"hint", (V)"Reach level 5."), new Entry((V)"condition", (V)"LevelUp to level 5"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)11.0d), R.Table(new Entry((V)"name", (V)"Boon Bearer"), new Entry((V)"hint", (V)"Claim a boon."), new Entry((V)"condition", (V)"BoonGranted"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)12.0d), R.Table(new Entry((V)"name", (V)"Stonebreaker"), new Entry((V)"hint", (V)"Break a barricade."), new Entry((V)"condition", (V)"TerrainBroken"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)13.0d), R.Table(new Entry((V)"name", (V)"Ore Seeker"), new Entry((V)"hint", (V)"Break an ore deposit."), new Entry((V)"condition", (V)"OreBroken"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)14.0d), R.Table(new Entry((V)"name", (V)"Gate Opener"), new Entry((V)"hint", (V)"Open a timed gate."), new Entry((V)"condition", (V)"TimedGateOpened"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)15.0d), R.Table(new Entry((V)"name", (V)"Key Bearer"), new Entry((V)"hint", (V)"Find a vault key."), new Entry((V)"condition", (V)"VaultKeyCollected"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)16.0d), R.Table(new Entry((V)"name", (V)"Warded"), new Entry((V)"hint", (V)"Collect a ward flask."), new Entry((V)"condition", (V)"WardFlaskCollected"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)17.0d), R.Table(new Entry((V)"name", (V)"Torchbearer"), new Entry((V)"hint", (V)"Light a brazier."), new Entry((V)"condition", (V)"BrazierLit"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)18.0d), R.Table(new Entry((V)"name", (V)"Second Wind"), new Entry((V)"hint", (V)"Recover health from a supply drop."), new Entry((V)"condition", (V)"SupplyCollected"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)19.0d), R.Table(new Entry((V)"name", (V)"Under Pressure"), new Entry((V)"hint", (V)"Face an arriving pursuer."), new Entry((V)"condition", (V)"PursuerArrived"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)20.0d), R.Table(new Entry((V)"name", (V)"Deeper Road"), new Entry((V)"hint", (V)"Enter the second stage."), new Entry((V)"condition", (V)"StageEntered at stage 2"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)21.0d), R.Table(new Entry((V)"name", (V)"Sanctum Reached"), new Entry((V)"hint", (V)"Enter the Final Sanctum."), new Entry((V)"condition", (V)"FinalSanctumEntered"))), new Entry(R.Get(R.Get(v_2, (V)"order"), (V)22.0d), R.Table(new Entry((V)"name", (V)"Regent Awakened"), new Entry((V)"hint", (V)"Force the Hollow Regent into phase two."), new Entry((V)"condition", (V)"RegentPhaseChanged"))))));
var obj_7 = v_2;
V key_8 = (V)"definitions";
R.Set(obj_7, key_8, values_6[0]);
R.Set(v_2, (V)"get", new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
return R.Join(new V[] {}, new Results(R.Get(R.Get(v_2, (V)"definitions"), v_10)));
return Results.Empty;
})));
R.Set(v_2, (V)"validate", new V((RuleFunction)(args_11 => {

var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_15 = values_14[0];
V v_16 = values_14[1];
foreach (var row_17 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"order"))))))) {
V v_18 = row_17[0];
V v_19 = row_17[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_19))).First == (V)"string"), () => R.Invoke(v_19, "match", R.Join(new V[] {}, new Results((V)"^core:achievement/"))).First), () => (V)(!R.Truth(R.Get(v_15, v_19))))}, new Results((V)"invalid achievement ID")));
var values_20 = R.Join(new V[] {}, new Results((V)true));
var obj_21 = v_15;
V key_22 = v_19;
R.Set(obj_21, key_22, values_20[0]);
var values_23 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"get"), R.Join(new V[] {}, new Results(v_19))).First}, new Results((V)"missing achievement"))));
V v_24 = values_23[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_24, (V)"name")))).First == (V)"string"), () => (V)((V)R.Length(R.Get(v_24, (V)"name")) > (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_24, (V)"hint")))).First == (V)"string")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_24, (V)"condition")))).First == (V)"string")), () => R.Or((V)(R.Get(v_24, (V)"reward") == V.Nil), () => R.And(R.Call(R.Get(v_13, (V)"get"), R.Join(new V[] {}, new Results(R.Get(v_24, (V)"reward")))).First, () => (V)(!R.Truth(R.Get(v_16, R.Get(v_24, (V)"reward")))))))}, new Results((V)"invalid achievement content")));
if (R.Truth(R.Get(v_24, (V)"reward"))) {
var values_25 = R.Join(new V[] {}, new Results((V)true));
var obj_26 = v_16;
V key_27 = R.Get(v_24, (V)"reward");
R.Set(obj_26, key_27, values_25[0]);
}
}
foreach (var row_28 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_2, (V)"definitions"))))))) {
V v_29 = row_28[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_15, v_29)}, new Results((V)"unlisted achievement")));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
