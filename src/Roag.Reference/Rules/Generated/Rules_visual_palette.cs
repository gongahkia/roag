// Static C# port of src/visual_palette.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 7eb550a33f914706cdafe4d64cc2ca75175bc8a5300c10eddba98331a161cbc4
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_visual_palette { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"chest", R.Table(new Entry((V)1, (V)0.77d), new Entry((V)2, (V)0.62d), new Entry((V)3, (V)0.42d))), new Entry((V)"spent", R.Table(new Entry((V)1, (V)0.56d), new Entry((V)2, (V)0.44d), new Entry((V)3, (V)0.31d))), new Entry((V)"food", R.Table(new Entry((V)1, (V)0.78d), new Entry((V)2, (V)0.61d), new Entry((V)3, (V)0.43d))), new Entry((V)"gold", R.Table(new Entry((V)1, (V)0.88d), new Entry((V)2, (V)0.75d), new Entry((V)3, (V)0.46d))), new Entry((V)"enemy", R.Table(new Entry((V)1, (V)0.61d), new Entry((V)2, (V)0.7d), new Entry((V)3, (V)0.78d))), new Entry((V)"hostile", R.Table(new Entry((V)1, (V)0.94d), new Entry((V)2, (V)0.32d), new Entry((V)3, (V)0.3d))), new Entry((V)"friendly", R.Table(new Entry((V)1, (V)0.47d), new Entry((V)2, (V)0.82d), new Entry((V)3, (V)0.58d))), new Entry((V)"blocked", R.Table(new Entry((V)1, (V)0.48d), new Entry((V)2, (V)0.48d), new Entry((V)3, (V)0.48d))))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"ration_cache", (V)true), new Entry((V)"cleanse_flask", (V)true), new Entry((V)"escape_scroll", (V)true), new Entry((V)"return_token", (V)true), new Entry((V)"return_marker", (V)true), new Entry((V)"offering_font", (V)true), new Entry((V)"charm_well", (V)true), new Entry((V)"attunement_forge", (V)true), new Entry((V)"travelling_trader", (V)true), new Entry((V)"archive_contract", (V)true))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)0.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d), new Entry((V)3, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)1.0d))), new Entry((V)6, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d), new Entry((V)3, (V)1.0d))))));
V v_6 = values_5[0];
R.Set(v_2, (V)"binding", new V((RuleFunction)(args_7 => {
V v_8 = args_7[0];
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_8))).First != (V)"string"))) {
return Results.Empty;
}
if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Invoke(v_8, "match", R.Join(new V[] {}, new Results((V)"^core:boon/"))).First, () => R.Invoke(v_8, "match", R.Join(new V[] {}, new Results((V)"^core:drop/"))).First), () => R.Invoke(v_8, "match", R.Join(new V[] {}, new Results((V)"^core:ui/fate_"))).First), () => (V)(v_8 == (V)"core:ui/currency_icon")), () => (V)(v_8 == (V)"core:ui/xp_icon")), () => (V)(v_8 == (V)"core:ui/boon_effect")), () => (V)(v_8 == (V)"core:ui/cue_boon_effect")), () => (V)(v_8 == (V)"core:ui/status_heal_icon")))) {
return R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_8 == (V)"core:drop/food"), () => R.Get(v_2, (V)"food")), () => R.Get(v_2, (V)"gold"))));
}
if (R.Truth((V)(v_8 == (V)"core:object/chest_open"))) {
return R.Join(new V[] {}, new Results(R.Get(v_2, (V)"spent")));
}
if (R.Truth(R.Or(R.Invoke(v_8, "match", R.Join(new V[] {}, new Results((V)"^core:chest/"))).First, () => (V)(v_8 == (V)"core:object/idea_cache_dormant")))) {
return R.Join(new V[] {}, new Results(R.Get(v_2, (V)"chest")));
}
if (R.Truth(R.Get(v_4, R.Invoke(v_8, "match", R.Join(new V[] {}, new Results((V)"^core:object/interaction_(.+)$"))).First))) {
return R.Join(new V[] {}, new Results(R.Get(v_2, (V)"gold")));
}
if (R.Truth(R.Invoke(v_8, "match", R.Join(new V[] {}, new Results((V)"^core:enemy/"))).First)) {
return R.Join(new V[] {}, new Results(R.Get(v_2, (V)"enemy")));
}
return Results.Empty;
})));
R.Set(v_2, (V)"resolve", new V((RuleFunction)(args_9 => {
V v_10 = args_9[0];
V v_11 = args_9[1];
if (R.Truth((V)(!R.Truth(v_10)))) {
return Results.Empty;
}
var values_12 = R.Join(new V[] {}, R.Invoke(v_10, "resolve", R.Join(new V[] {}, new Results(v_11))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"binding"), R.Join(new V[] {}, new Results(v_11))));
V v_15 = values_14[0];
if (R.Truth(R.Or((V)(!R.Truth(v_13)), () => (V)(!R.Truth(v_15))))) {
return R.Join(new V[] {}, new Results(v_13));
}
var values_16 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_10, (V)"ink_handles"), () => R.Table())));
var obj_17 = v_10;
V key_18 = (V)"ink_handles";
R.Set(obj_17, key_18, values_16[0]);
var values_19 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_10, (V)"ink_handles"), v_11)));
V v_20 = values_19[0];
if (R.Truth(R.Or((V)(!R.Truth(v_20)), () => (V)(R.Get(v_20, (V)"base") != v_13)))) {
var values_21 = R.Join(new V[] {}, R.Call(rt.Global("setmetatable"), R.Join(new V[] {R.Table(new Entry((V)"ink", v_15), new Entry((V)"base", v_13))}, new Results(R.Table(new Entry((V)"__index", v_13))))));
v_20 = values_21[0];
var values_22 = R.Join(new V[] {}, new Results(v_20));
var obj_23 = R.Get(v_10, (V)"ink_handles");
V key_24 = v_11;
R.Set(obj_23, key_24, values_22[0]);
}
return R.Join(new V[] {}, new Results(v_20));
return Results.Empty;
})));
R.Set(v_2, (V)"attack", new V((RuleFunction)(args_25 => {
V v_26 = args_25[0];
V v_27 = args_25[1];
V v_28 = args_25[2];
if (R.Truth(R.Or(R.Get(v_27, (V)"support_kind"), () => R.Get(v_27, (V)"support")))) {
return R.Join(new V[] {}, new Results(R.Get(v_2, (V)"enemy")));
}
if (R.Truth(R.Or(R.Or(R.Get(v_27, (V)"terrain_heat"), () => R.Get(v_27, (V)"ember_pulse")), () => R.Get(v_27, (V)"fixture")))) {
return R.Join(new V[] {}, new Results(R.Get(v_2, (V)"hostile")));
}
if (R.Truth(R.Or(R.Or(R.Or((V)(R.Get(v_27, (V)"source_id") == v_26), () => (V)(R.Get(v_27, (V)"owner_id") == v_26)), () => R.Get(v_27, (V)"companion")), () => R.And(v_28, () => (V)(R.Get(v_28, (V)"owner_id") == v_26))))) {
return R.Join(new V[] {}, new Results(R.Get(v_2, (V)"friendly")));
}
return R.Join(new V[] {}, new Results(R.Get(v_2, (V)"hostile")));
return Results.Empty;
})));
R.Set(v_2, (V)"preview", new V((RuleFunction)(args_29 => {
V v_30 = args_29[0];
V v_31 = args_29[1];
if (R.Truth(R.And(R.Or((V)(v_30 == (V)"dash"), () => (V)(v_30 == (V)"use_utility")), () => (V)(v_31 != (V)"open")))) {
return R.Join(new V[] {}, new Results(R.Get(v_2, (V)"blocked")));
}
return R.Join(new V[] {}, new Results(R.Get(v_2, (V)"friendly")));
return Results.Empty;
})));
R.Set(v_2, (V)"damage", new V((RuleFunction)(args_32 => {
V v_33 = args_32[0];
if (R.Truth((V)(R.Get(v_33, (V)"amount") == (V)0.0d))) {
return R.Join(new V[] {}, new Results(R.Get(v_2, (V)"enemy")));
}
var values_34 = R.Join(new V[] {}, new Results((V)((V)((V)((V)((V)(R.Or(R.Get(v_33, (V)"x"), () => (V)0.0d) * (V)7.0d) + (V)(R.Or(R.Get(v_33, (V)"y"), () => (V)0.0d) * (V)11.0d)) + (V)(R.Get(v_33, (V)"amount") * (V)3.0d)) % (V)31.0d) / (V)31.0d)));
V v_35 = values_34[0];
var values_36 = R.Join(new V[] {}, new Results((V)((V)((V)((V)(R.Or(R.Get(v_33, (V)"age"), () => (V)0.0d) * (V)1.2d) + v_35) % (V)1.0d) * (V)6.0d)));
V v_37 = values_36[0];
var values_38 = R.Join(new V[] {}, new Results((V)((V)0.9d + (V)((V)0.1d * R.Call(R.Get(rt.Global("math"), (V)"sin"), R.Join(new V[] {}, new Results((V)((V)((V)(R.Or(R.Get(v_33, (V)"age"), () => (V)0.0d) * R.Get(rt.Global("math"), (V)"pi")) * (V)4.0d) + (V)((V)(v_35 * R.Get(rt.Global("math"), (V)"pi")) * (V)2.0d))))).First))));
V v_39 = values_38[0];
var values_40 = R.Join(new V[] {}, new Results((V)(v_39 * (V)0.8d)));
V v_41 = values_40[0];
var values_42 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results(v_37))));
V v_43 = values_42[0];
var values_44 = R.Join(new V[] {R.Get(v_6, (V)(v_43 + (V)1.0d))}, new Results(R.Get(v_6, (V)((V)((V)(v_43 + (V)1.0d) % (V)6.0d) + (V)1.0d))));
V v_45 = values_44[0];
V v_46 = values_44[1];
var values_47 = R.Join(new V[] {}, new Results((V)(v_37 - v_43)));
V v_48 = values_47[0];
var values_49 = R.Join(new V[] {}, new Results((V)(v_39 - v_41)));
V v_50 = values_49[0];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)(v_50 + (V)(v_41 * (V)(R.Get(v_45, (V)1.0d) + (V)((V)(R.Get(v_46, (V)1.0d) - R.Get(v_45, (V)1.0d)) * v_48))))), new Entry((V)2, (V)(v_50 + (V)(v_41 * (V)(R.Get(v_45, (V)2.0d) + (V)((V)(R.Get(v_46, (V)2.0d) - R.Get(v_45, (V)2.0d)) * v_48))))), new Entry((V)3, (V)(v_50 + (V)(v_41 * (V)(R.Get(v_45, (V)3.0d) + (V)((V)(R.Get(v_46, (V)3.0d) - R.Get(v_45, (V)3.0d)) * v_48))))))));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
