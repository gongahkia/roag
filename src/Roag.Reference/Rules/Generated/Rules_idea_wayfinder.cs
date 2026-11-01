// Static C# port of src/idea_wayfinder.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: b169e70e25a0e7ec50dec5e0b787491fbc30c6ae15bd63fa9e7fc2f5dea1f7db
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_wayfinder { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"core:actor/wayfinder"), new Entry((V)"beacon_id", (V)"core:object/return_beacon"), new Entry((V)"range", (V)6.0d))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_2, (V)"id")), new Entry((V)"name", (V)"Wayfinder"), new Entry((V)"weapon_icon", (V)"dagger"), new Entry((V)"description", (V)"Thrown knife, explored-room beacon, bounded return or short Dash"), new Entry((V)"hp", (V)22.0d), new Entry((V)"damage", (V)3.0d), new Entry((V)"slash_damage", (V)0.0d), new Entry((V)"defense", (V)0.0d), new Entry((V)"speed", (V)110.0d), new Entry((V)"crit_chance_ppm", (V)0.0d), new Entry((V)"crit_damage_percent", (V)150.0d), new Entry((V)"costs", R.Table(new Entry((V)"move", (V)100.0d), new Entry((V)"basic_attack", (V)100.0d), new Entry((V)"sweeping_slash", (V)100.0d), new Entry((V)"dash", (V)110.0d))), new Entry((V)"dash_distance", (V)2.0d), new Entry((V)"basic_id", (V)"core:ability/wayfinder_knife"), new Entry((V)"special_id", (V)"core:ability/wayfinder_plant"), new Entry((V)"basic_name", (V)"Scout Knife"), new Entry((V)"special_name", (V)"Plant Beacon"), new Entry((V)"dash_name", (V)"Return / Dash"), new Entry((V)"ui_controls", (V)"F Knife \u00c2\u00b7 R Plant beacon \u00c2\u00b7 X Return / Dash"))));
var obj_4 = v_2;
V key_5 = (V)"definition";
R.Set(obj_4, key_5, values_3[0]);
R.Set(v_2, (V)"room", new V((RuleFunction)(args_6 => {
V v_7 = args_6[0];
V v_8 = args_6[1];
V v_9 = args_6[2];
if (R.Truth((V)(!R.Truth(R.Get(v_7, (V)"rooms"))))) {
return R.Join(new V[] {}, new Results((V)0.0d));
}
foreach (var row_10 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"rooms"))))))) {
V v_11 = row_10[0];
V v_12 = row_10[1];
if (R.Truth(R.And(R.And(R.And((V)(v_8 >= R.Get(v_12, (V)"x")), () => (V)(v_8 < (V)(R.Get(v_12, (V)"x") + R.Get(v_12, (V)"width")))), () => (V)(v_9 >= R.Get(v_12, (V)"y"))), () => (V)(v_9 < (V)(R.Get(v_12, (V)"y") + R.Get(v_12, (V)"height")))))) {
return R.Join(new V[] {}, new Results(R.Get(v_12, (V)"id")));
}
}
return Results.Empty;
})));
R.Set(v_2, (V)"plant_preview", new V((RuleFunction)(args_13 => {
V v_14 = args_13[0];
V v_15 = args_13[1];
V v_16 = args_13[2];
V v_17 = args_13[3];
V v_18 = args_13[4];
V v_19 = args_13[5];
V v_20 = args_13[6];
if (R.Truth(R.Or(R.Or(R.Or((V)(!R.Truth(v_17)), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_17, (V)"x") - R.Get(R.Get(v_16, (V)"position"), (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_17, (V)"y") - R.Get(R.Get(v_16, (V)"position"), (V)"y"))))).First) != (V)1.0d)), () => (V)(!R.Truth(R.Get(R.Get(v_15, (V)"rows"), R.Get(v_17, (V)"y"))))), () => (V)(R.Invoke(R.Get(R.Get(v_15, (V)"rows"), R.Get(v_17, (V)"y")), "sub", R.Join(new V[] {R.Get(v_17, (V)"x")}, new Results(R.Get(v_17, (V)"x")))).First != (V)".")))) {
return R.Join(new V[] {(V)false}, new Results((V)"choose adjacent cardinal plain floor"));
}
var values_21 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"room"), R.Join(new V[] {v_15, R.Get(v_17, (V)"x")}, new Results(R.Get(v_17, (V)"y")))));
V v_22 = values_21[0];
if (R.Truth(R.Or((V)(v_22 == V.Nil), () => (V)(v_22 != R.Call(R.Get(v_2, (V)"room"), R.Join(new V[] {v_15, R.Get(R.Get(v_16, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_16, (V)"position"), (V)"y")))).First)))) {
return R.Join(new V[] {(V)false}, new Results((V)"plant within your current room"));
}
if (R.Truth(R.Or((V)(!R.Truth(R.Call(v_18, R.Join(new V[] {v_14, R.Get(R.Get(v_16, (V)"position"), (V)"floor_id"), R.Get(v_17, (V)"x")}, new Results(R.Get(v_17, (V)"y")))).First)), () => (V)(!R.Truth(R.Call(v_19, R.Join(new V[] {v_14, R.Get(R.Get(v_16, (V)"position"), (V)"floor_id"), R.Get(v_17, (V)"x")}, new Results(R.Get(v_17, (V)"y")))).First))))) {
return R.Join(new V[] {(V)false}, new Results((V)"beacon needs explored visible ground"));
}
if (R.Truth(R.Call(v_20, R.Join(new V[] {R.Get(v_17, (V)"x")}, new Results(R.Get(v_17, (V)"y")))).First)) {
return R.Join(new V[] {(V)false}, new Results((V)"beacon cell occupied"));
}
foreach (var row_23 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_15, (V)"placements"), () => R.Get(R.Get(v_15, (V)"placements"), (V)"stairs")), () => R.Table()))))))) {
V v_24 = row_23[0];
V v_25 = row_23[1];
if (R.Truth(R.And((V)(R.Get(v_25, (V)"x") == R.Get(v_17, (V)"x")), () => (V)(R.Get(v_25, (V)"y") == R.Get(v_17, (V)"y"))))) {
return R.Join(new V[] {(V)false}, new Results((V)"stairs must stay clear"));
}
}
return R.Join(new V[] {(V)true, V.Nil}, new Results(v_22));
return Results.Empty;
})));
R.Set(v_2, (V)"plant", new V((RuleFunction)(args_26 => {
V v_27 = args_26[0];
V v_28 = args_26[1];
V v_29 = args_26[2];
V v_30 = args_26[3];
var values_31 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"owner_id", R.Get(v_28, (V)"id")), new Entry((V)"floor_id", R.Get(R.Get(v_28, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(v_29, (V)"x")), new Entry((V)"y", R.Get(v_29, (V)"y")), new Entry((V)"room_id", v_30))));
var obj_32 = R.Get(v_27, (V)"class_special");
V key_33 = (V)"beacon";
R.Set(obj_32, key_33, values_31[0]);
return Results.Empty;
})));
R.Set(v_2, (V)"return_preview", new V((RuleFunction)(args_34 => {
V v_35 = args_34[0];
V v_36 = args_34[1];
V v_37 = args_34[2];
V v_38 = args_34[3];
V v_39 = args_34[4];
V v_40 = args_34[5];
V v_41 = args_34[6];
var values_42 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_35, (V)"class_special"), (V)"beacon")));
V v_43 = values_42[0];
if (R.Truth(R.Or((V)(!R.Truth(v_43)), () => (V)(R.Get(v_43, (V)"floor_id") != R.Get(R.Get(v_37, (V)"position"), (V)"floor_id"))))) {
return R.Join(new V[] {(V)false}, new Results((V)"beacon is on another floor or absent"));
}
if (R.Truth(R.And((V)(R.Get(R.Get(v_37, (V)"position"), (V)"x") == R.Get(v_43, (V)"x")), () => (V)(R.Get(R.Get(v_37, (V)"position"), (V)"y") == R.Get(v_43, (V)"y"))))) {
return R.Join(new V[] {(V)false}, new Results((V)"already at beacon"));
}
if (R.Truth(R.Or((V)(R.Call(R.Get(v_2, (V)"room"), R.Join(new V[] {v_36, R.Get(R.Get(v_37, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_37, (V)"position"), (V)"y")))).First != R.Get(v_43, (V)"room_id")), () => (V)(R.Call(R.Get(v_2, (V)"room"), R.Join(new V[] {v_36, R.Get(v_43, (V)"x")}, new Results(R.Get(v_43, (V)"y")))).First != R.Get(v_43, (V)"room_id"))))) {
return R.Join(new V[] {(V)false}, new Results((V)"return is limited to the beacon room"));
}
if (R.Truth(R.Or(R.Call(v_41, R.Join(new V[] {R.Get(v_43, (V)"x")}, new Results(R.Get(v_43, (V)"y")))).First, () => (V)(!R.Truth(R.Call(v_39, R.Join(new V[] {v_35, R.Get(v_43, (V)"floor_id"), R.Get(v_43, (V)"x")}, new Results(R.Get(v_43, (V)"y")))).First))))) {
return R.Join(new V[] {(V)false}, new Results((V)"beacon landing blocked or out of sight"));
}
foreach (var row_44 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_36, (V)"placements"), () => R.Get(R.Get(v_36, (V)"placements"), (V)"stairs")), () => R.Table()))))))) {
V v_45 = row_44[0];
V v_46 = row_44[1];
if (R.Truth(R.And((V)(R.Get(v_46, (V)"x") == R.Get(v_43, (V)"x")), () => (V)(R.Get(v_46, (V)"y") == R.Get(v_43, (V)"y"))))) {
return R.Join(new V[] {(V)false}, new Results((V)"stairs must stay clear"));
}
}
var values_47 = R.Join(new V[] {}, new Results(R.Table()));
V v_48 = values_47[0];
foreach (var row_49 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_35, (V)"entities"))))))) {
V v_50 = row_49[0];
V v_51 = row_49[1];
var values_52 = R.Join(new V[] {}, new Results(R.Get(v_51, (V)"position")));
V v_53 = values_52[0];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_53, (V)"floor_id") == R.Get(v_43, (V)"floor_id")), () => R.Get(v_51, (V)"blocks_movement")), () => (V)(R.Get(v_51, (V)"controller") != (V)"player")), () => (V)(R.Get(v_51, (V)"controller") != (V)"ai")))) {
var values_54 = R.Join(new V[] {}, new Results((V)true));
var obj_55 = v_48;
V key_56 = R.Concat(R.Get(v_53, (V)"x"), R.Concat((V)",", R.Get(v_53, (V)"y")));
R.Set(obj_55, key_56, values_54[0]);
}
}
var values_57 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(R.Get(v_37, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_37, (V)"position"), (V)"y")), new Entry((V)"d", (V)0.0d))))));
V v_58 = values_57[0];

var values_59 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Concat(R.Get(R.Get(v_37, (V)"position"), (V)"x"), R.Concat((V)",", R.Get(R.Get(v_37, (V)"position"), (V)"y"))), (V)true))));
V v_60 = values_59[0];

var values_61 = R.Join(new V[] {}, new Results((V)1.0d));
V v_62 = values_61[0];
while (R.Truth((V)(v_62 <= (V)R.Length(v_58)))) {
var values_63 = R.Join(new V[] {}, new Results(R.Get(v_58, v_62)));
V v_64 = values_63[0];

var values_65 = R.Join(new V[] {}, new Results((V)(v_62 + (V)1.0d)));
v_62 = values_65[0];
if (R.Truth(R.And((V)(R.Get(v_64, (V)"x") == R.Get(v_43, (V)"x")), () => (V)(R.Get(v_64, (V)"y") == R.Get(v_43, (V)"y"))))) {
return R.Join(new V[] {(V)true, V.Nil, v_43}, new Results(R.Get(v_64, (V)"d")));
}
if (R.Truth((V)(R.Get(v_64, (V)"d") < R.Get(v_2, (V)"range")))) {
foreach (var row_66 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d)))))))))) {
V v_67 = row_66[0];
V v_68 = row_66[1];
var values_69 = R.Join(new V[] {(V)(R.Get(v_64, (V)"x") + R.Get(v_68, (V)1.0d))}, new Results((V)(R.Get(v_64, (V)"y") + R.Get(v_68, (V)2.0d))));
V v_70 = values_69[0];
V v_71 = values_69[1];

var values_72 = R.Join(new V[] {}, new Results(R.Concat(v_70, R.Concat((V)",", v_71))));
V v_73 = values_72[0];
if (R.Truth(R.And(R.And(R.And(R.And((V)(!R.Truth(R.Get(v_60, v_73))), () => (V)(!R.Truth(R.Get(v_48, v_73)))), () => R.Call(R.Get(v_38, (V)"walkable"), R.Join(new V[] {v_36, v_70}, new Results(v_71))).First), () => (V)(R.Call(R.Get(v_2, (V)"room"), R.Join(new V[] {v_36, v_70}, new Results(v_71))).First == R.Get(v_43, (V)"room_id"))), () => R.Call(v_40, R.Join(new V[] {v_35, R.Get(v_43, (V)"floor_id"), v_70}, new Results(v_71))).First))) {
var values_74 = R.Join(new V[] {}, new Results((V)true));
var obj_75 = v_60;
V key_76 = v_73;
R.Set(obj_75, key_76, values_74[0]);

var values_77 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_70), new Entry((V)"y", v_71), new Entry((V)"d", (V)(R.Get(v_64, (V)"d") + (V)1.0d)))));
var obj_78 = v_58;
V key_79 = (V)((V)R.Length(v_58) + (V)1.0d);
R.Set(obj_78, key_79, values_77[0]);
}
}
}
}
return R.Join(new V[] {(V)false}, new Results((V)"beacon needs an explored route of at most six tiles"));
return Results.Empty;
})));
R.Set(v_2, (V)"validate", new V((RuleFunction)(args_80 => {
V v_81 = args_80[0];
V v_82 = args_80[1];
V v_83 = args_80[2];
var values_84 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_81, (V)"class_special"), (V)"beacon")));
V v_85 = values_84[0];

if (R.Truth((V)(!R.Truth(v_85)))) {
return Results.Empty;
}
var values_86 = R.Join(new V[] {}, new Results(R.Get(v_82, R.Get(v_85, (V)"floor_id"))));
V v_87 = values_86[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_83, (V)"definition_id") == R.Get(v_2, (V)"id")), () => (V)(R.Get(v_85, (V)"owner_id") == R.Get(v_83, (V)"id"))), () => v_87), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_85, (V)"x")))).First == (V)"number")), () => (V)((V)(R.Get(v_85, (V)"x") % (V)1.0d) == (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_85, (V)"y")))).First == (V)"number")), () => (V)((V)(R.Get(v_85, (V)"y") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_85, (V)"x") >= (V)1.0d)), () => (V)(R.Get(v_85, (V)"x") <= R.Get(v_87, (V)"width"))), () => (V)(R.Get(v_85, (V)"y") >= (V)1.0d)), () => (V)(R.Get(v_85, (V)"y") <= R.Get(v_87, (V)"height"))), () => (V)(R.Invoke(R.Get(R.Get(v_87, (V)"rows"), R.Get(v_85, (V)"y")), "sub", R.Join(new V[] {R.Get(v_85, (V)"x")}, new Results(R.Get(v_85, (V)"x")))).First == (V)".")), () => (V)(R.Call(R.Get(v_2, (V)"room"), R.Join(new V[] {v_87, R.Get(v_85, (V)"x")}, new Results(R.Get(v_85, (V)"y")))).First == R.Get(v_85, (V)"room_id")))}, new Results((V)"invalid return beacon")));
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(R.Get(v_81, (V)"explored"), R.Get(v_85, (V)"floor_id")), () => R.Get(R.Get(R.Get(R.Get(v_81, (V)"explored"), R.Get(v_85, (V)"floor_id")), (V)"cells"), (V)((V)((V)(R.Get(v_85, (V)"y") - (V)1.0d) * R.Get(v_87, (V)"width")) + R.Get(v_85, (V)"x"))))}, new Results((V)"unexplored beacon")));
foreach (var row_88 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_87, (V)"placements"), () => R.Get(R.Get(v_87, (V)"placements"), (V)"stairs")), () => R.Table()))))))) {
V v_89 = row_88[0];
V v_90 = row_88[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_90, (V)"x") != R.Get(v_85, (V)"x")), () => (V)(R.Get(v_90, (V)"y") != R.Get(v_85, (V)"y")))}, new Results((V)"beacon on stairs")));
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
