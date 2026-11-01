// Static C# port of src/combat_numbers.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 8907e382c1ef3623fadee7a7dbe9c82ca3cd071aa3d58158c5fcb1fade8b052c
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_combat_numbers { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table()));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results((V)0.78d));
var obj_4 = v_2;
V key_5 = (V)"lifetime";
R.Set(obj_4, key_5, values_3[0]);
var values_6 = R.Join(new V[] {}, new Results((V)24.0d));
var obj_7 = v_2;
V key_8 = (V)"limit";
R.Set(obj_7, key_8, values_6[0]);
R.Set(v_2, (V)"new", new V((RuleFunction)(args_9 => {

return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"items", R.Table()), new Entry((V)"active_item", V.Nil), new Entry((V)"floor_id", V.Nil))));
return Results.Empty;
})));
V v_10 = V.Nil;
v_10 = new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
V v_13 = args_11[1];
var values_14 = R.Join(new V[] {}, new Results((V)0.0d));
V v_15 = values_14[0];
foreach (var row_16 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_12, (V)"items"))))))) {
V v_17 = row_16[0];
V v_18 = row_16[1];
if (R.Truth(R.And((V)(R.Get(v_18, (V)"x") == R.Get(v_13, (V)"x")), () => (V)(R.Get(v_18, (V)"y") == R.Get(v_13, (V)"y"))))) {
var values_19 = R.Join(new V[] {}, new Results((V)(v_15 + (V)1.0d)));
v_15 = values_19[0];
}
}
var values_20 = R.Join(new V[] {}, new Results(R.Get(R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)), new Entry((V)3, (V)1.0d)), (V)((V)(v_15 % (V)3.0d) + (V)1.0d))));
var obj_21 = v_13;
V key_22 = (V)"lane";
R.Set(obj_21, key_22, values_20[0]);
var values_23 = R.Join(new V[] {}, new Results((V)0.0d));
var obj_24 = v_13;
V key_25 = (V)"age";
R.Set(obj_24, key_25, values_23[0]);
var values_26 = R.Join(new V[] {}, new Results(v_13));
var obj_27 = R.Get(v_12, (V)"items");
V key_28 = (V)((V)R.Length(R.Get(v_12, (V)"items")) + (V)1.0d);
R.Set(obj_27, key_28, values_26[0]);
if (R.Truth((V)((V)R.Length(R.Get(v_12, (V)"items")) > R.Get(v_2, (V)"limit")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_12, (V)"items")}, new Results((V)1.0d)));
}
return Results.Empty;
}));
V v_29 = V.Nil;
v_29 = new V((RuleFunction)(args_30 => {
V v_31 = args_30[0];
V v_32 = args_30[1];
V v_33 = args_30[2];
V v_34 = args_30[3];
var values_35 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_32, (V)"events"), () => R.Table(new Entry((V)1, R.Get(v_32, (V)"event"))))));
V v_36 = values_35[0];
var values_37 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_38 = values_37[0];
V v_39 = values_37[1];
foreach (var row_40 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_36)))))) {
V v_41 = row_40[0];
V v_42 = row_40[1];
if (R.Truth(R.And(R.And(R.And(R.Or((V)(R.Get(v_42, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Get(v_42, (V)"kind") == (V)"DamageBlocked")), () => R.Get(v_42, (V)"x")), () => R.Get(v_42, (V)"y")), () => R.Or((V)(!R.Truth(R.Get(v_42, (V)"floor_id"))), () => (V)(R.Get(v_42, (V)"floor_id") == v_34))))) {
var values_43 = R.Join(new V[] {}, new Results(R.Concat(R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_42, (V)"target_id"), () => (V)"")))).First, R.Concat((V)":", R.Concat(R.Get(v_42, (V)"x"), R.Concat((V)":", R.Get(v_42, (V)"y")))))));
V v_44 = values_43[0];
var values_45 = R.Join(new V[] {}, new Results(R.Get(v_39, v_44)));
V v_46 = values_45[0];
if (R.Truth((V)(!R.Truth(v_46)))) {
var values_47 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(v_42, (V)"x")), new Entry((V)"y", R.Get(v_42, (V)"y")), new Entry((V)"amount", (V)0.0d), new Entry((V)"blocked", (V)false), new Entry((V)"critical", (V)false), new Entry((V)"from_player", (V)false))));
v_46 = values_47[0];
var values_48 = R.Join(new V[] {}, new Results(v_46));
var obj_49 = v_39;
V key_50 = v_44;
R.Set(obj_49, key_50, values_48[0]);
var values_51 = R.Join(new V[] {}, new Results(v_46));
var obj_52 = v_38;
V key_53 = (V)((V)R.Length(v_38) + (V)1.0d);
R.Set(obj_52, key_53, values_51[0]);
}
if (R.Truth(R.And((V)(R.Get(v_42, (V)"kind") == (V)"DamageTaken"), () => (V)(R.Or(R.Get(v_42, (V)"actual_loss"), () => (V)0.0d) > (V)0.0d)))) {
var values_54 = R.Join(new V[] {}, new Results((V)(R.Get(v_46, (V)"amount") + R.Get(v_42, (V)"actual_loss"))));
var obj_55 = v_46;
V key_56 = (V)"amount";
R.Set(obj_55, key_56, values_54[0]);
var values_57 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_46, (V)"critical"), () => (V)(R.Get(v_42, (V)"critical") == (V)true))));
var obj_58 = v_46;
V key_59 = (V)"critical";
R.Set(obj_58, key_59, values_57[0]);
var values_60 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_46, (V)"from_player"), () => (V)(R.Get(v_42, (V)"source_id") == v_33))));
var obj_61 = v_46;
V key_62 = (V)"from_player";
R.Set(obj_61, key_62, values_60[0]);
} else if (R.Truth((V)(R.Get(v_42, (V)"kind") == (V)"DamageBlocked"))) {
var values_63 = R.Join(new V[] {}, new Results((V)true));
var obj_64 = v_46;
V key_65 = (V)"blocked";
R.Set(obj_64, key_65, values_63[0]);
}
}
}
foreach (var row_66 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_38)))))) {
V v_67 = row_66[0];
V v_68 = row_66[1];
if (R.Truth(R.Or((V)(R.Get(v_68, (V)"amount") > (V)0.0d), () => R.Get(v_68, (V)"blocked")))) {
R.Call(v_10, R.Join(new V[] {v_31}, new Results(v_68)));
}
}
return Results.Empty;
}));
R.Set(v_2, (V)"update", new V((RuleFunction)(args_69 => {
V v_70 = args_69[0];
V v_71 = args_69[1];
V v_72 = args_69[2];
V v_73 = args_69[3];
V v_74 = args_69[4];
if (R.Truth((V)(R.Get(v_70, (V)"floor_id") != v_74))) {
var values_75 = R.Join(new V[] {}, new Results(R.Table()));
var obj_76 = v_70;
V key_77 = (V)"items";
R.Set(obj_76, key_77, values_75[0]);
var values_78 = R.Join(new V[] {}, new Results(V.Nil));
var obj_79 = v_70;
V key_80 = (V)"active_item";
R.Set(obj_79, key_80, values_78[0]);
var values_81 = R.Join(new V[] {}, new Results(v_74));
var obj_82 = v_70;
V key_83 = (V)"floor_id";
R.Set(obj_82, key_83, values_81[0]);
}
{ double end_86 = R.Num((V)1.0d), step_87 = R.Num((-(V)1.0d));
for (double i_85 = R.Num((V)R.Length(R.Get(v_70, (V)"items"))); step_87 > 0 ? i_85 <= end_86 : i_85 >= end_86; i_85 += step_87) {
V v_84 = i_85;
var values_88 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_70, (V)"items"), v_84)));
V v_89 = values_88[0];
var values_90 = R.Join(new V[] {}, new Results((V)(R.Get(v_89, (V)"age") + R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)0.0d}, new Results(v_72))).First)));
var obj_91 = v_89;
V key_92 = (V)"age";
R.Set(obj_91, key_92, values_90[0]);
if (R.Truth((V)(R.Get(v_89, (V)"age") >= R.Get(v_2, (V)"lifetime")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_70, (V)"items")}, new Results(v_84)));
}
}
}
var values_93 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_71, (V)"items"), (V)1.0d)));
V v_94 = values_93[0];
if (R.Truth((V)(v_94 != R.Get(v_70, (V)"active_item")))) {
var values_95 = R.Join(new V[] {}, new Results(v_94));
var obj_96 = v_70;
V key_97 = (V)"active_item";
R.Set(obj_96, key_97, values_95[0]);
if (R.Truth(v_94)) {
R.Call(v_29, R.Join(new V[] {v_70, v_94, v_73}, new Results(v_74)));
}
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
