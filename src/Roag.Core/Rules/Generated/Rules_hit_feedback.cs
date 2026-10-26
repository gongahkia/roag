// Static C# port of src/hit_feedback.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 3739ba4b396f9ae5a96d515f34ad8599f33ff320e60b1b74dd983690e1fa6f70
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_hit_feedback { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results((V)0.16d));
var obj_4 = v_2;
V key_5 = (V)"shake_seconds";
R.Set(obj_4, key_5, values_3[0]);
var values_6 = R.Join(new V[] {}, new Results((V)0.22d));
var obj_7 = v_2;
V key_8 = (V)"flash_seconds";
R.Set(obj_7, key_8, values_6[0]);
R.Set(v_2, (V)"new", new V((RuleFunction)(args_9 => {

return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", V.Nil), new Entry((V)"active_item", V.Nil), new Entry((V)"shake_age", R.Get(v_2, (V)"shake_seconds")), new Entry((V)"flash_age", R.Get(v_2, (V)"flash_seconds")), new Entry((V)"strength", (V)0.0d))));
return Results.Empty;
})));
R.Set(v_2, (V)"update", new V((RuleFunction)(args_10 => {
V v_11 = args_10[0];
V v_12 = args_10[1];
V v_13 = args_10[2];
V v_14 = args_10[3];
V v_15 = args_10[4];
if (R.Truth((V)(R.Get(v_11, (V)"floor_id") != v_15))) {
var values_16 = R.Join(new V[] {}, new Results(v_15));
var obj_17 = v_11;
V key_18 = (V)"floor_id";
R.Set(obj_17, key_18, values_16[0]);
var values_19 = R.Join(new V[] {}, new Results(V.Nil));
var obj_20 = v_11;
V key_21 = (V)"active_item";
R.Set(obj_20, key_21, values_19[0]);
var values_22 = R.Join(new V[] {}, new Results(R.Get(v_2, (V)"shake_seconds")));
var obj_23 = v_11;
V key_24 = (V)"shake_age";
R.Set(obj_23, key_24, values_22[0]);
var values_25 = R.Join(new V[] {}, new Results(R.Get(v_2, (V)"flash_seconds")));
var obj_26 = v_11;
V key_27 = (V)"flash_age";
R.Set(obj_26, key_27, values_25[0]);
var values_28 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_29 = v_11;
V key_30 = (V)"strength";
R.Set(obj_29, key_30, values_28[0]);
}
var values_31 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_12, (V)"items"), (V)1.0d)));
V v_32 = values_31[0];
if (R.Truth((V)(v_32 != R.Get(v_11, (V)"active_item")))) {
var values_33 = R.Join(new V[] {}, new Results(v_32));
var obj_34 = v_11;
V key_35 = (V)"active_item";
R.Set(obj_34, key_35, values_33[0]);
var values_36 = R.Join(new V[] {}, new Results((V)0.0d));
V v_37 = values_36[0];
if (R.Truth(v_32)) {
foreach (var row_38 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_32, (V)"events"), () => R.Table(new Entry((V)1, R.Get(v_32, (V)"event")))))))))) {
V v_39 = row_38[0];
V v_40 = row_38[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_40, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_40, (V)"target_id") == v_14)), () => R.Or((V)(!R.Truth(R.Get(v_40, (V)"floor_id"))), () => (V)(R.Get(v_40, (V)"floor_id") == v_15))))) {
var values_41 = R.Join(new V[] {}, new Results((V)(v_37 + R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results(R.Or(R.Get(v_40, (V)"actual_loss"), () => (V)0.0d)))).First)));
v_37 = values_41[0];
}
}
}
if (R.Truth((V)(v_37 > (V)0.0d))) {
var values_42 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_43 = v_11;
V key_44 = (V)"shake_age";
R.Set(obj_43, key_44, values_42[0]);
var values_45 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_46 = v_11;
V key_47 = (V)"flash_age";
R.Set(obj_46, key_47, values_45[0]);
var values_48 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)5.0d}, new Results((V)((V)2.0d + R.Call(R.Get(rt.Global("math"), (V)"sqrt"), R.Join(new V[] {}, new Results(v_37))).First)))));
var obj_49 = v_11;
V key_50 = (V)"strength";
R.Set(obj_49, key_50, values_48[0]);
return Results.Empty;
}
}
var values_51 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results(v_13))));
V v_52 = values_51[0];
var values_53 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_2, (V)"shake_seconds")}, new Results((V)(R.Get(v_11, (V)"shake_age") + v_52)))));
var obj_54 = v_11;
V key_55 = (V)"shake_age";
R.Set(obj_54, key_55, values_53[0]);
var values_56 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {R.Get(v_2, (V)"flash_seconds")}, new Results((V)(R.Get(v_11, (V)"flash_age") + v_52)))));
var obj_57 = v_11;
V key_58 = (V)"flash_age";
R.Set(obj_57, key_58, values_56[0]);
return Results.Empty;
})));
R.Set(v_2, (V)"offset", new V((RuleFunction)(args_59 => {
V v_60 = args_59[0];
if (R.Truth((V)(R.Get(v_60, (V)"shake_age") >= R.Get(v_2, (V)"shake_seconds")))) {
return R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
}
var values_61 = R.Join(new V[] {}, new Results(R.Get(v_60, (V)"shake_age")));
V v_62 = values_61[0];
var values_63 = R.Join(new V[] {}, new Results((V)Math.Pow(R.Num((V)((V)1.0d - (V)(v_62 / R.Get(v_2, (V)"shake_seconds")))), R.Num((V)2.0d))));
V v_64 = values_63[0];
var values_65 = R.Join(new V[] {}, new Results((V)(R.Get(v_60, (V)"strength") * v_64)));
V v_66 = values_65[0];
var values_67 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"sin"), R.Join(new V[] {}, new Results((V)((V)((V)(v_62 * (V)2.0d) * R.Get(rt.Global("math"), (V)"pi")) * (V)12.0d)))).First * v_66) + (V)0.5d)))));
V v_68 = values_67[0];
var values_69 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"cos"), R.Join(new V[] {}, new Results((V)((V)((V)(v_62 * (V)2.0d) * R.Get(rt.Global("math"), (V)"pi")) * (V)17.0d)))).First * v_66) + (V)0.5d)))));
V v_70 = values_69[0];
return R.Join(new V[] {v_68}, new Results(v_70));
return Results.Empty;
})));
R.Set(v_2, (V)"flash_alpha", new V((RuleFunction)(args_71 => {
V v_72 = args_71[0];
if (R.Truth((V)(R.Get(v_72, (V)"flash_age") >= R.Get(v_2, (V)"flash_seconds")))) {
return R.Join(new V[] {}, new Results((V)0.0d));
}
var values_73 = R.Join(new V[] {}, new Results((V)((V)1.0d - (V)(R.Get(v_72, (V)"flash_age") / R.Get(v_2, (V)"flash_seconds")))));
V v_74 = values_73[0];
return R.Join(new V[] {}, new Results((V)((V)((V)0.17d * v_74) * v_74)));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
