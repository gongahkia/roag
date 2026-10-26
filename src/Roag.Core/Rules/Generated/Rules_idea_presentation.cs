// Static C# port of src/idea_presentation.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 8e51bbaee37b006c3ae7d4ca689acc5d34c333e49d11a011d413b6fb57323a09
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_presentation { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_environment_content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table()));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"adventurer", R.Table(new Entry((V)1, (V)"sword"), new Entry((V)2, (V)"slash"))), new Entry((V)"ranger", R.Table(new Entry((V)1, (V)"bow"), new Entry((V)2, (V)"volley"))), new Entry((V)"arcanist", R.Table(new Entry((V)1, (V)"staff"), new Entry((V)2, (V)"wand"))), new Entry((V)"berserker", R.Table(new Entry((V)1, (V)"mace"), new Entry((V)2, (V)"axe"))), new Entry((V)"sentinel", R.Table(new Entry((V)1, (V)"shield"), new Entry((V)2, (V)"shield"))), new Entry((V)"alchemist", R.Table(new Entry((V)1, (V)"vial"), new Entry((V)2, (V)"vial"))), new Entry((V)"shade", R.Table(new Entry((V)1, (V)"knife"), new Entry((V)2, (V)"wand"))), new Entry((V)"hookrunner", R.Table(new Entry((V)1, (V)"knife"), new Entry((V)2, (V)"hook"))), new Entry((V)"trapper", R.Table(new Entry((V)1, (V)"knife"), new Entry((V)2, (V)"trap"))), new Entry((V)"duelist", R.Table(new Entry((V)1, (V)"knife"), new Entry((V)2, (V)"shield"))), new Entry((V)"geomancer", R.Table(new Entry((V)1, (V)"staff"), new Entry((V)2, (V)"stone"))), new Entry((V)"spiritbinder", R.Table(new Entry((V)1, (V)"wand"), new Entry((V)2, (V)"spirit"))), new Entry((V)"tinkerer", R.Table(new Entry((V)1, (V)"bow"), new Entry((V)2, (V)"sentry"))), new Entry((V)"stormcaller", R.Table(new Entry((V)1, (V)"staff"), new Entry((V)2, (V)"spark"))), new Entry((V)"wayfinder", R.Table(new Entry((V)1, (V)"knife"), new Entry((V)2, (V)"beacon"))))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"trap", (V)"core:ui/signal_armed_effect"), new Entry((V)"stone", (V)"core:object/geomancer_bulwark"), new Entry((V)"spirit", (V)"core:object/bound_spirit"), new Entry((V)"sentry", (V)"core:object/bolt_sentry"), new Entry((V)"spark", (V)"core:ui/cue_spark_effect"), new Entry((V)"beacon", (V)"core:object/return_beacon"))));
V v_8 = values_7[0];
R.Set(v_4, (V)"weapon", new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
V v_11 = args_9[1];
if (R.Truth((V)(v_11 == (V)"dash"))) {
return R.Join(new V[] {}, new Results((V)"core:ui/ability_move_icon"));
}
var values_12 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_6, R.Invoke(R.Or(v_10, () => (V)""), "match", R.Join(new V[] {}, new Results((V)"/([^/]+)$"))).First), () => R.Get(v_6, (V)"adventurer"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, new Results(R.Get(v_13, R.Or(R.And((V)(v_11 == (V)"sweeping_slash"), () => (V)2.0d), () => (V)1.0d))));
V v_15 = values_14[0];
return R.Join(new V[] {}, new Results(R.Or(R.Get(v_8, v_15), () => R.Concat((V)"core:ui/ability_", R.Concat(v_15, (V)"_icon")))));
return Results.Empty;
})));
R.Set(v_4, (V)"effect", new V((RuleFunction)(args_16 => {
V v_17 = args_16[0];
if (R.Truth((V)(R.Get(v_17, (V)"kind") == (V)"DamageTaken"))) {
if (R.Truth((V)(R.Or(R.Get(v_17, (V)"actual_loss"), () => (V)0.0d) <= (V)0.0d))) {
return Results.Empty;
}
var values_18 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.Or(R.And((V)(R.Get(v_17, (V)"damage_type") == (V)"fire"), () => (V)"fire"), () => R.And((V)(R.Get(v_17, (V)"damage_type") == (V)"lightning"), () => (V)"spark")), () => R.And(R.Or((V)(R.Get(v_17, (V)"damage_type") == (V)"echo"), () => (V)(R.Get(v_17, (V)"damage_type") == (V)"spirit")), () => (V)"echo")), () => (V)"slash")));
V v_19 = values_18[0];
return R.Join(new V[] {}, new Results(R.Concat((V)"core:ui/cue_", R.Concat(v_19, (V)"_effect"))));
} else if (R.Truth((V)(R.Get(v_17, (V)"kind") == (V)"DamageBlocked"))) {
return R.Join(new V[] {}, new Results((V)"core:ui/cue_block_effect"));
} else if (R.Truth((V)(R.Get(v_17, (V)"kind") == (V)"BoonActivated"))) {
return R.Join(new V[] {}, new Results((V)"core:ui/cue_boon_effect"));
} else if (R.Truth(R.And(R.Or((V)(R.Get(v_17, (V)"kind") == (V)"Healed"), () => (V)(R.Get(v_17, (V)"kind") == (V)"SupplyCollected")), () => (V)(R.Or(R.Or(R.Get(v_17, (V)"actual_gain"), () => R.Get(v_17, (V)"amount")), () => (V)0.0d) > (V)0.0d)))) {
return R.Join(new V[] {}, new Results((V)"core:ui/status_heal_icon"));
}
return Results.Empty;
})));
R.Set(v_4, (V)"statuses", new V((RuleFunction)(args_20 => {
V v_21 = args_20[0];
V v_22 = args_20[1];
var values_23 = R.Join(new V[] {}, new Results(R.Table()));
V v_24 = values_23[0];
if (R.Truth(R.And(R.Get(v_21, (V)"statuses"), () => R.Get(R.Get(v_21, (V)"statuses"), (V)"burning")))) {
var values_25 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"core:ui/burning_effect"), new Entry((V)"label", (V)"Burning"))));
var obj_26 = v_24;
V key_27 = (V)((V)R.Length(v_24) + (V)1.0d);
R.Set(obj_26, key_27, values_25[0]);
}
var values_28 = R.Join(new V[] {}, new Results(R.Or(R.And(v_22, () => R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boons"))).First, (V)"ward_total"), R.Join(new V[] {R.Table(new Entry((V)"clock", v_22))}, new Results(v_21))).First), () => (V)0.0d)));
V v_29 = values_28[0];
if (R.Truth(R.Or(R.Or((V)(R.Or(R.Get(v_21, (V)"pickup_ward_actions"), () => (V)0.0d) > (V)0.0d), () => R.Get(v_21, (V)"storm_ward")), () => (V)(v_29 > (V)0.0d)))) {
var values_30 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"core:ui/status_ward_icon"), new Entry((V)"label", R.Or(R.And((V)(v_29 > (V)0.0d), () => R.Concat((V)"Ward ", v_29)), () => (V)"Ward")))));
var obj_31 = v_24;
V key_32 = (V)((V)R.Length(v_24) + (V)1.0d);
R.Set(obj_31, key_32, values_30[0]);
}
if (R.Truth((V)(R.Or(R.Get(v_21, (V)"supply_focus_actions"), () => (V)0.0d) > (V)0.0d))) {
var values_33 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"core:ui/status_focus_icon"), new Entry((V)"label", (V)"Focus"))));
var obj_34 = v_24;
V key_35 = (V)((V)R.Length(v_24) + (V)1.0d);
R.Set(obj_34, key_35, values_33[0]);
}
if (R.Truth((V)(R.Get(v_21, (V)"definition_id") == (V)"core:actor/stormcaller"))) {
var values_36 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)3.0d}, new Results(R.Or(R.Get(v_21, (V)"storm_charge"), () => (V)0.0d)))))));
V v_37 = values_36[0];
var values_38 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Concat((V)"core:ui/status_charge_", R.Concat(v_37, (V)"_icon"))), new Entry((V)"label", R.Concat((V)"Charge ", v_37)))));
var obj_39 = v_24;
V key_40 = (V)((V)R.Length(v_24) + (V)1.0d);
R.Set(obj_39, key_40, values_38[0]);
}
return R.Join(new V[] {}, new Results(v_24));
return Results.Empty;
})));
R.Set(v_4, (V)"last_roll", new V((RuleFunction)(args_41 => {
V v_42 = args_41[0];
var values_43 = R.Join(new V[] {}, new Results(R.And(R.Get(v_42, (V)"boon_memory"), () => R.Get(R.Get(v_42, (V)"boon_memory"), (V)"core:boon/fateful_dice"))));
V v_44 = values_43[0];
if (R.Truth(R.And(R.And(R.And(v_44, () => R.Get(v_44, (V)"face")), () => R.Get(v_42, (V)"boons")), () => R.Get(R.Get(v_42, (V)"boons"), (V)"core:boon/fateful_dice")))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Concat((V)"core:ui/fate_", R.Concat(R.Get(v_44, (V)"face"), (V)"_icon"))), new Entry((V)"label", R.Concat((V)"Last roll: ", R.Get(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_boon_content"))).First, (V)"faces"), R.Get(v_44, (V)"face")))))));
}
return Results.Empty;
})));
R.Set(v_4, (V)"signal", new V((RuleFunction)(args_45 => {
V v_46 = args_45[0];
return R.Join(new V[] {}, new Results(R.Or(R.Or(R.And(R.Or((V)(v_46 == (V)"warning"), () => (V)(v_46 == (V)"armed")), () => (V)"core:ui/signal_armed_effect"), () => R.And((V)(v_46 == (V)"closed"), () => (V)"core:ui/signal_spent_effect")), () => (V)"core:ui/signal_safe_effect")));
return Results.Empty;
})));
R.Set(v_4, (V)"mural", new V((RuleFunction)(args_47 => {
V v_48 = args_47[0];
V v_49 = args_47[1];
V v_50 = args_47[2];
var values_51 = R.Join(new V[] {}, new Results((V)((V)((V)((V)(v_49 * (V)3.0d) + (V)(v_50 * (V)5.0d)) + R.Or(R.Get(v_48, (V)"seed"), () => (V)1.0d)) % (V)3.0d)));
V v_52 = values_51[0];
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"prop_id"), R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_52 == (V)0.0d), () => (V)"mural_face"), () => R.And((V)(v_52 == (V)1.0d), () => (V)"mural_sigil")), () => (V)"mural_leaf")))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_4));
return Results.Empty;
} }
