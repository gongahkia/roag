// Static C# port of src/idea_sentinel.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 078d3c56da4cd02be38f9981d22fe20fbaf3f7a72a558ce2c7ad26021e5308c7
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_sentinel { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"core:enemy/rotating_sentinel"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
var obj_4 = v_2;
V key_5 = (V)"directions";
R.Set(obj_4, key_5, values_3[0]);
R.Set(v_2, (V)"cells", new V((RuleFunction)(args_6 => {
V v_7 = args_6[0];
V v_8 = args_6[1];
V v_9 = args_6[2];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(R.Get(v_2, (V)"directions"), v_9)}, new Results((V)"invalid sentinel facing"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, new Results(R.Table()));
V v_13 = values_12[0];
{ double end_16 = R.Num((V)1.0d), step_17 = R.Num((V)1.0d);
for (double i_15 = R.Num((-(V)1.0d)); step_17 > 0 ? i_15 <= end_16 : i_15 >= end_16; i_15 += step_17) {
V v_14 = i_15;
var values_18 = R.Join(new V[] {(V)((V)(R.Get(v_8, (V)"x") + R.Get(v_11, (V)1.0d)) - (V)(R.Get(v_11, (V)2.0d) * v_14))}, new Results((V)((V)(R.Get(v_8, (V)"y") + R.Get(v_11, (V)2.0d)) + (V)(R.Get(v_11, (V)1.0d) * v_14))));
V v_19 = values_18[0];
V v_20 = values_18[1];
if (R.Truth(R.And(R.And(R.And(R.And((V)(v_19 >= (V)1.0d), () => (V)(v_19 <= R.Get(v_7, (V)"width"))), () => (V)(v_20 >= (V)1.0d)), () => (V)(v_20 <= R.Get(v_7, (V)"height"))), () => (V)(R.Invoke(R.Get(R.Get(v_7, (V)"rows"), v_20), "sub", R.Join(new V[] {v_19}, new Results(v_19))).First != (V)"#")))) {
var values_21 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_8, (V)"floor_id")), new Entry((V)"x", v_19), new Entry((V)"y", v_20))));
var obj_22 = v_13;
V key_23 = (V)((V)R.Length(v_13) + (V)1.0d);
R.Set(obj_22, key_23, values_21[0]);
}
}
}
return R.Join(new V[] {}, new Results(v_13));
return Results.Empty;
})));
R.Set(v_2, (V)"choose", new V((RuleFunction)(args_24 => {
V v_25 = args_24[0];
V v_26 = args_24[1];
V v_27 = args_24[2];
V v_28 = args_24[3];
var values_29 = R.Join(new V[] {R.Get(v_26, (V)"ai_memory"), R.Get(v_26, (V)"position")}, new Results(R.Get(v_27, (V)"position")));
V v_30 = values_29[0];
V v_31 = values_29[1];
V v_32 = values_29[2];
var values_33 = R.Join(new V[] {(V)(R.Get(v_32, (V)"x") - R.Get(v_31, (V)"x"))}, new Results((V)(R.Get(v_32, (V)"y") - R.Get(v_31, (V)"y"))));
V v_34 = values_33[0];
V v_35 = values_33[1];
var values_36 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_30, (V)"sentinel_strikes"), () => (V)0.0d)));
V v_37 = values_36[0];
if (R.Truth(R.And((V)(!R.Truth(R.Get(v_30, (V)"sentinel_finished"))), () => R.Or((V)(v_37 > (V)0.0d), () => R.And(R.And((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_34))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_35))).First) <= (V)4.0d), () => R.Call(R.Get(v_28, (V)"visible"), R.Join(new V[] {R.Get(v_31, (V)"x")}, new Results(R.Get(v_31, (V)"y")))).First), () => R.Call(R.Get(v_28, (V)"clear_line"), R.Join(new V[] {v_25, v_31}, new Results(v_32))).First))))) {
var values_38 = Results.Empty;
V v_39 = values_38[0];
if (R.Truth((V)(v_37 > (V)0.0d))) {
var values_40 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_30, (V)"sentinel_facing") % (V)4.0d) + (V)1.0d)));
v_39 = values_40[0];
} else if (R.Truth((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_34))).First >= R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_35))).First))) {
var values_41 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_34 >= (V)0.0d), () => (V)2.0d), () => (V)4.0d)));
v_39 = values_41[0];
} else {
var values_42 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_35 >= (V)0.0d), () => (V)3.0d), () => (V)1.0d)));
v_39 = values_42[0];
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/sentinel_quarter"), new Entry((V)"cells", R.Call(R.Get(v_2, (V)"cells"), R.Join(new V[] {R.Get(v_28, (V)"map"), v_31}, new Results(v_39))).First), new Entry((V)"sentinel_facing", v_39))));
}
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_34))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_35))).First) == (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/sentinel_touch"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_32, (V)"x")), new Entry((V)"y", R.Get(v_32, (V)"y")))))))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_28, (V)"next_step"), R.Join(new V[] {v_25, v_26}, R.Call(R.Get(v_28, (V)"adjacent_goals"), R.Join(new V[] {v_25, v_26, v_27}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
return Results.Empty;
})));
R.Set(v_2, (V)"commit", new V((RuleFunction)(args_43 => {
V v_44 = args_43[0];
V v_45 = args_43[1];
if (R.Truth((V)(!R.Truth(R.Get(v_45, (V)"sentinel_facing"))))) {
return Results.Empty;
}
var values_46 = R.Join(new V[] {}, new Results(R.Get(v_44, (V)"ai_memory")));
V v_47 = values_46[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Get(v_44, (V)"definition_id") == R.Get(v_2, (V)"id")), () => (V)(!R.Truth(R.Get(v_47, (V)"sentinel_finished")))), () => (V)(R.Or(R.Get(v_47, (V)"sentinel_strikes"), () => (V)0.0d) < (V)4.0d))}, new Results((V)"spent sentinel sequence")));
var values_48 = R.Join(new V[] {}, new Results(R.Get(v_45, (V)"sentinel_facing")));
var obj_49 = v_47;
V key_50 = (V)"sentinel_facing";
R.Set(obj_49, key_50, values_48[0]);
var values_51 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_47, (V)"sentinel_strikes"), () => (V)0.0d) + (V)1.0d)));
var obj_52 = v_47;
V key_53 = (V)"sentinel_strikes";
R.Set(obj_52, key_53, values_51[0]);
if (R.Truth((V)(R.Get(v_47, (V)"sentinel_strikes") == (V)4.0d))) {
var values_54 = R.Join(new V[] {}, new Results((V)true));
var obj_55 = v_47;
V key_56 = (V)"sentinel_finished";
R.Set(obj_55, key_56, values_54[0]);
}
return Results.Empty;
})));
R.Set(v_2, (V)"interrupt", new V((RuleFunction)(args_57 => {
V v_58 = args_57[0];
if (R.Truth(R.And((V)(R.Get(v_58, (V)"definition_id") == R.Get(v_2, (V)"id")), () => (V)(R.Or(R.Get(R.Get(v_58, (V)"ai_memory"), (V)"sentinel_strikes"), () => (V)0.0d) > (V)0.0d)))) {
var values_59 = R.Join(new V[] {}, new Results((V)true));
var obj_60 = R.Get(v_58, (V)"ai_memory");
V key_61 = (V)"sentinel_finished";
R.Set(obj_60, key_61, values_59[0]);
}
return Results.Empty;
})));
R.Set(v_2, (V)"optional_room", new V((RuleFunction)(args_62 => {
V v_63 = args_62[0];
V v_64 = args_62[1];
var values_65 = R.Join(new V[] {}, new Results(R.Table()));
V v_66 = values_65[0];
foreach (var row_67 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_63, (V)"graph"), () => R.Get(R.Get(v_63, (V)"graph"), (V)"critical")), () => R.Table()))))))) {
V v_68 = row_67[0];
V v_69 = row_67[1];
var values_70 = R.Join(new V[] {}, new Results((V)true));
var obj_71 = v_66;
V key_72 = v_69;
R.Set(obj_71, key_72, values_70[0]);
}
foreach (var row_73 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_63, (V)"rooms"), () => R.Table()))))))) {
V v_74 = row_73[0];
V v_75 = row_73[1];
if (R.Truth(R.And(R.And(R.And(R.And((V)(!R.Truth(R.Get(v_66, R.Get(v_75, (V)"id")))), () => (V)(R.Get(v_64, (V)"x") > R.Get(v_75, (V)"x"))), () => (V)(R.Get(v_64, (V)"x") < (V)((V)(R.Get(v_75, (V)"x") + R.Get(v_75, (V)"width")) - (V)1.0d))), () => (V)(R.Get(v_64, (V)"y") > R.Get(v_75, (V)"y"))), () => (V)(R.Get(v_64, (V)"y") < (V)((V)(R.Get(v_75, (V)"y") + R.Get(v_75, (V)"height")) - (V)1.0d))))) {
return R.Join(new V[] {}, new Results(R.Get(v_75, (V)"id")));
}
}
return Results.Empty;
})));
R.Set(v_2, (V)"validate", new V((RuleFunction)(args_76 => {
V v_77 = args_76[0];
V v_78 = args_76[1];
foreach (var row_79 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_77, (V)"entities"))))))) {
V v_80 = row_79[0];
V v_81 = row_79[1];
if (R.Truth((V)(R.Get(v_81, (V)"definition_id") == R.Get(v_2, (V)"id")))) {
var values_82 = R.Join(new V[] {}, new Results(R.Get(v_81, (V)"ai_memory")));
V v_83 = values_82[0];

var values_84 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_83, (V)"sentinel_strikes"), () => (V)0.0d)));
V v_85 = values_84[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_85))).First == (V)"number"), () => (V)((V)(v_85 % (V)1.0d) == (V)0.0d)), () => (V)(v_85 >= (V)0.0d)), () => (V)(v_85 <= (V)4.0d)), () => R.Or((V)(R.Get(v_83, (V)"sentinel_finished") == V.Nil), () => (V)(R.Get(v_83, (V)"sentinel_finished") == (V)true))), () => R.Or((V)(v_85 < (V)4.0d), () => R.Get(v_83, (V)"sentinel_finished")))}, new Results((V)"invalid sentinel sequence")));
if (R.Truth((V)(v_85 > (V)0.0d))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_83, (V)"sentinel_facing")))).First == (V)"number"), () => (V)((V)(R.Get(v_83, (V)"sentinel_facing") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_83, (V)"sentinel_facing") >= (V)1.0d)), () => (V)(R.Get(v_83, (V)"sentinel_facing") <= (V)4.0d))}, new Results((V)"invalid sentinel facing")));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_83, (V)"sentinel_facing") == V.Nil), () => (V)(!R.Truth(R.Get(v_83, (V)"sentinel_finished"))))}, new Results((V)"unstarted sentinel sequence")));
}
var values_86 = R.Join(new V[] {}, new Results(R.Get(v_81, (V)"pending_attack")));
V v_87 = values_86[0];
if (R.Truth(R.And(v_87, () => R.Or((V)(R.Get(v_87, (V)"ability_id") == (V)"core:ability/sentinel_quarter"), () => R.Get(v_87, (V)"sentinel_facing"))))) {
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(v_85 > (V)0.0d), () => (V)(R.Get(v_87, (V)"sentinel_facing") == R.Get(v_83, (V)"sentinel_facing"))), () => (V)(R.Get(v_87, (V)"ability_id") == (V)"core:ability/sentinel_quarter")), () => (V)(!R.Truth(R.Get(v_87, (V)"support"))))}, new Results((V)"invalid sentinel commitment")));
var values_88 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"cells"), R.Join(new V[] {R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(v_78, R.Get(v_87, (V)"floor_id"))))).First, R.Table(new Entry((V)"floor_id", R.Get(v_87, (V)"floor_id")), new Entry((V)"x", R.Get(v_87, (V)"source_x")), new Entry((V)"y", R.Get(v_87, (V)"source_y")))}, new Results(R.Get(v_87, (V)"sentinel_facing")))));
V v_89 = values_88[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_89) == (V)R.Length(R.Get(v_87, (V)"target_cells")))}, new Results((V)"invalid sentinel footprint")));
foreach (var row_90 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_89)))))) {
V v_91 = row_90[0];
V v_92 = row_90[1];
var values_93 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_87, (V)"target_cells"), v_91)));
V v_94 = values_93[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Get(v_92, (V)"floor_id") == R.Get(v_94, (V)"floor_id")), () => (V)(R.Get(v_92, (V)"x") == R.Get(v_94, (V)"x"))), () => (V)(R.Get(v_92, (V)"y") == R.Get(v_94, (V)"y")))}, new Results((V)"unlocked sentinel footprint")));
}
}
}
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
