// Static C# port of src/idea_duelist.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 098d80352cf215963d8cf79db00cad01d06b66e34c3c9fd38d24b4c7cdfb2581
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_duelist { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.targeting"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"core:actor/duelist"), new Entry((V)"bonus", (V)2.0d), new Entry((V)"tiny", (V)99.0d), new Entry((V)"onebit", (V)124.0d))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_4, (V)"id")), new Entry((V)"name", (V)"Duelist"), new Entry((V)"description", (V)"Two-cell thrust, timed frontal parry, one-cell sidestep"), new Entry((V)"hp", (V)20.0d), new Entry((V)"damage", (V)4.0d), new Entry((V)"slash_damage", (V)0.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)115.0d), new Entry((V)"crit_chance_ppm", (V)0.0d), new Entry((V)"crit_damage_percent", (V)150.0d), new Entry((V)"costs", R.Table(new Entry((V)"move", (V)100.0d), new Entry((V)"basic_attack", (V)100.0d), new Entry((V)"sweeping_slash", (V)100.0d), new Entry((V)"dash", (V)85.0d))), new Entry((V)"dash_distance", (V)1.0d), new Entry((V)"basic_id", (V)"core:ability/duelist_thrust"), new Entry((V)"special_id", (V)"core:ability/duelist_parry"), new Entry((V)"basic_name", (V)"Thrust"), new Entry((V)"special_name", (V)"Timed Parry"), new Entry((V)"dash_name", (V)"Sidestep"), new Entry((V)"ui_controls", (V)"F Thrust \u00c2\u00b7 R Parry \u00c2\u00b7 X Step"))));
var obj_6 = v_4;
V key_7 = (V)"definition";
R.Set(obj_6, key_7, values_5[0]);
R.Set(v_4, (V)"bonus_for", new V((RuleFunction)(args_8 => {
V v_9 = args_8[0];
V v_10 = args_8[1];
return R.Join(new V[] {}, new Results(R.Or(R.And(R.And(R.And((V)(R.Get(v_9, (V)"definition_id") == R.Get(v_4, (V)"id")), () => (V)(v_10 == (V)"basic_attack")), () => R.Get(v_9, (V)"duelist_riposte")), () => R.Get(v_4, (V)"bonus")), () => (V)0.0d)));
return Results.Empty;
})));
R.Set(v_4, (V)"raise", new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
V v_13 = args_11[1];
V v_14 = args_11[2];
V v_15 = args_11[3];
V v_16 = args_11[4];
var values_17 = R.Join(new V[] {}, new Results(R.Get(v_13, (V)"position")));
V v_18 = values_17[0];
var values_19 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_18, (V)"floor_id")), new Entry((V)"x", R.Get(v_18, (V)"x")), new Entry((V)"y", R.Get(v_18, (V)"y")), new Entry((V)"dx", v_14), new Entry((V)"dy", v_15), new Entry((V)"expires", (V)(R.Get(v_12, (V)"clock") + v_16)))));
var obj_20 = R.Get(v_12, (V)"class_special");
V key_21 = (V)"duelist_guard";
R.Set(obj_20, key_21, values_19[0]);
return Results.Empty;
})));
R.Set(v_4, (V)"parry", new V((RuleFunction)(args_22 => {
V v_23 = args_22[0];
V v_24 = args_22[1];
V v_25 = args_22[2];
V v_26 = args_22[3];
V v_27 = args_22[4];
V v_28 = args_22[5];
var values_29 = R.Join(new V[] {}, new Results(R.And(R.Get(v_23, (V)"class_special"), () => R.Get(R.Get(v_23, (V)"class_special"), (V)"duelist_guard"))));
V v_30 = values_29[0];
if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or((V)(!R.Truth(v_30)), () => (V)(R.Get(v_24, (V)"id") != R.Get(v_23, (V)"player_id"))), () => (V)(R.Get(v_24, (V)"definition_id") != R.Get(v_4, (V)"id"))), () => (V)(!R.Truth(v_26))), () => v_27), () => (V)(v_28 <= (V)0.0d)), () => (V)(!R.Truth(R.Get(v_25, (V)"position")))), () => (V)(R.Get(v_25, (V)"faction") == R.Get(v_24, (V)"faction"))), () => (V)(R.Get(v_25, (V)"faction") != (V)"enemy")), () => (V)(R.Get(v_23, (V)"clock") > R.Get(v_30, (V)"expires"))), () => (V)(R.Get(R.Get(v_24, (V)"position"), (V)"floor_id") != R.Get(v_30, (V)"floor_id"))), () => (V)(R.Get(R.Get(v_24, (V)"position"), (V)"x") != R.Get(v_30, (V)"x"))), () => (V)(R.Get(R.Get(v_24, (V)"position"), (V)"y") != R.Get(v_30, (V)"y"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_31 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"cells"), R.Join(new V[] {(V)"sweeping_slash", R.Get(v_30, (V)"x"), R.Get(v_30, (V)"y"), R.Get(v_30, (V)"dx")}, new Results(R.Get(v_30, (V)"dy"))))))))) {
V v_32 = row_31[0];
V v_33 = row_31[1];
if (R.Truth(R.And(R.And((V)(R.Get(R.Get(v_25, (V)"position"), (V)"floor_id") == R.Get(v_30, (V)"floor_id")), () => (V)(R.Get(R.Get(v_25, (V)"position"), (V)"x") == R.Get(v_33, (V)"x"))), () => (V)(R.Get(R.Get(v_25, (V)"position"), (V)"y") == R.Get(v_33, (V)"y"))))) {
var values_34 = R.Join(new V[] {}, new Results(V.Nil));
var obj_35 = R.Get(v_23, (V)"class_special");
V key_36 = (V)"duelist_guard";
R.Set(obj_35, key_36, values_34[0]);

var values_37 = R.Join(new V[] {}, new Results((V)true));
var obj_38 = v_24;
V key_39 = (V)"duelist_riposte";
R.Set(obj_38, key_39, values_37[0]);
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})));
R.Set(v_4, (V)"validate", new V((RuleFunction)(args_40 => {
V v_41 = args_40[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(!R.Truth(R.Get(v_41, (V)"class_special"))), () => (V)(R.Get(R.Get(v_41, (V)"class_special"), (V)"duelist_guard") == V.Nil))}, new Results((V)"a paid parry cannot outlive its committed action")));
foreach (var row_42 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_41, (V)"entities"))))))) {
V v_43 = row_42[0];
V v_44 = row_42[1];
if (R.Truth((V)(R.Get(v_44, (V)"duelist_riposte") != V.Nil))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Get(v_44, (V)"id") == R.Get(v_41, (V)"player_id")), () => (V)(R.Get(v_44, (V)"definition_id") == R.Get(v_4, (V)"id"))), () => (V)(R.Get(v_44, (V)"duelist_riposte") == (V)true))}, new Results((V)"invalid riposte charge")));
}
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_4));
return Results.Empty;
} }
