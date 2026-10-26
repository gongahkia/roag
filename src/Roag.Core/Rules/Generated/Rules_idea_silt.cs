// Static C# port of src/idea_silt.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 1854f69b35ba5d05d6be60b3839698b15b0f9f6cf670f58182e935a202ff8c18
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_silt { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.terrain"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"core:enemy/silt_worm"), new Entry((V)"duration", (V)300.0d), new Entry((V)"cap", (V)12.0d))));
V v_4 = values_3[0];
V v_5 = V.Nil;
v_5 = new V((RuleFunction)(args_6 => {
V v_7 = args_6[0];
V v_8 = args_6[1];
foreach (var row_9 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"entities"))))))) {
V v_10 = row_9[0];
V v_11 = row_9[1];
if (R.Truth((V)(R.Get(v_11, (V)"id") == v_8))) {
return R.Join(new V[] {}, new Results(v_11));
}
}
return Results.Empty;
}));
R.Set(v_4, (V)"protected", new V((RuleFunction)(args_12 => {
V v_13 = args_12[0];
V v_14 = args_12[1];
V v_15 = args_12[2];
V v_16 = args_12[3];
foreach (var row_17 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_14, (V)"placements"), () => R.Get(R.Get(v_14, (V)"placements"), (V)"stairs")), () => R.Table()))))))) {
V v_18 = row_17[0];
V v_19 = row_17[1];
if (R.Truth(R.And((V)(R.Get(v_19, (V)"x") == v_15), () => (V)(R.Get(v_19, (V)"y") == v_16)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
foreach (var row_20 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_13, (V)"entities"))))))) {
V v_21 = row_20[0];
V v_22 = row_20[1];
var values_23 = R.Join(new V[] {}, new Results(R.Get(v_22, (V)"position")));
V v_24 = values_23[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_24, (V)"floor_id") == R.Get(v_14, (V)"floor_id")), () => (V)(R.Get(v_24, (V)"x") == v_15)), () => (V)(R.Get(v_24, (V)"y") == v_16)), () => (V)(R.Get(v_22, (V)"controller") != (V)"player")), () => (V)(R.Get(v_22, (V)"controller") != (V)"ai")), () => R.Or(R.Get(v_22, (V)"blocks_movement"), () => (V)(R.Get(v_22, (V)"controller") == (V)"exit"))))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
})));
R.Set(v_4, (V)"choose", new V((RuleFunction)(args_25 => {
V v_26 = args_25[0];
V v_27 = args_25[1];
V v_28 = args_25[2];
V v_29 = args_25[3];
var values_30 = R.Join(new V[] {R.Get(v_27, (V)"position")}, new Results(R.Get(v_28, (V)"position")));
V v_31 = values_30[0];
V v_32 = values_30[1];
var values_33 = R.Join(new V[] {(V)(R.Get(v_32, (V)"x") - R.Get(v_31, (V)"x"))}, new Results((V)(R.Get(v_32, (V)"y") - R.Get(v_31, (V)"y"))));
V v_34 = values_33[0];
V v_35 = values_33[1];

var values_36 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_34))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_35))).First)));
V v_37 = values_36[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And((V)(R.Or(R.Get(R.Get(v_27, (V)"ai_memory"), (V)"silt_charges"), () => (V)0.0d) < (V)2.0d), () => (V)(v_37 >= (V)2.0d)), () => (V)(v_37 <= (V)3.0d)), () => R.Or((V)(v_34 == (V)0.0d), () => (V)(v_35 == (V)0.0d))), () => R.Call(R.Get(v_29, (V)"visible"), R.Join(new V[] {R.Get(v_31, (V)"x")}, new Results(R.Get(v_31, (V)"y")))).First), () => R.Call(R.Get(v_29, (V)"clear_line"), R.Join(new V[] {v_26, v_31}, new Results(v_32))).First))) {
var values_38 = R.Join(new V[] {R.Or(R.And((V)(v_34 == (V)0.0d), () => (V)0.0d), () => (V)(v_34 / R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_34))).First))}, new Results(R.Or(R.And((V)(v_35 == (V)0.0d), () => (V)0.0d), () => (V)(v_35 / R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_35))).First))));
V v_39 = values_38[0];
V v_40 = values_38[1];
var values_41 = R.Join(new V[] {R.Table()}, new Results((V)true));
V v_42 = values_41[0];
V v_43 = values_41[1];
{ double end_46 = R.Num(v_37), step_47 = R.Num((V)1.0d);
for (double i_45 = R.Num((V)1.0d); step_47 > 0 ? i_45 <= end_46 : i_45 >= end_46; i_45 += step_47) {
V v_44 = i_45;
var values_48 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)(R.Get(v_31, (V)"x") + (V)(v_39 * v_44))), new Entry((V)"y", (V)(R.Get(v_31, (V)"y") + (V)(v_40 * v_44))))));
V v_49 = values_48[0];
var values_50 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"at"), R.Join(new V[] {R.Get(v_29, (V)"map"), R.Get(v_49, (V)"x")}, new Results(R.Get(v_49, (V)"y")))));
V v_51 = values_50[0];
if (R.Truth(R.Or(R.Or(R.Or((V)(!R.Truth(v_51)), () => (V)(!R.Truth(R.Get(v_51, (V)"walkable")))), () => R.Call(R.Get(v_4, (V)"protected"), R.Join(new V[] {v_26, R.Get(v_29, (V)"map"), R.Get(v_49, (V)"x")}, new Results(R.Get(v_49, (V)"y")))).First), () => (V)(!R.Truth(R.Call(R.Get(v_29, (V)"visible"), R.Join(new V[] {R.Get(v_49, (V)"x")}, new Results(R.Get(v_49, (V)"y")))).First))))) {
var values_52 = R.Join(new V[] {}, new Results((V)false));
v_43 = values_52[0];
}
var values_53 = R.Join(new V[] {}, new Results(v_49));
var obj_54 = v_42;
V key_55 = (V)((V)R.Length(v_42) + (V)1.0d);
R.Set(obj_54, key_55, values_53[0]);
}
}
if (R.Truth(v_43)) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/worm_charge"), new Entry((V)"cells", v_42), new Entry((V)"ranged", (V)true), new Entry((V)"advance", R.Table(new Entry((V)"dx", v_39), new Entry((V)"dy", v_40), new Entry((V)"max_steps", (V)2.0d))))));
}
}
if (R.Truth((V)(v_37 == (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/worm_touch"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_32, (V)"x")), new Entry((V)"y", R.Get(v_32, (V)"y")))))))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_29, (V)"next_step"), R.Join(new V[] {v_26, v_27}, R.Call(R.Get(v_29, (V)"adjacent_goals"), R.Join(new V[] {v_26, v_27, v_28}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
return Results.Empty;
})));
R.Set(v_4, (V)"at", new V((RuleFunction)(args_56 => {
V v_57 = args_56[0];
V v_58 = args_56[1];
V v_59 = args_56[2];
foreach (var row_60 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_57, (V)"idea_silt"), () => R.Table()))))))) {
V v_61 = row_60[0];
V v_62 = row_60[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_62, (V)"floor_id") == R.Get(v_57, (V)"active_floor_id")), () => (V)(R.Get(v_62, (V)"expires") > R.Get(v_57, (V)"clock"))), () => (V)(R.Get(v_62, (V)"x") == v_58)), () => (V)(R.Get(v_62, (V)"y") == v_59)))) {
return R.Join(new V[] {}, new Results(v_62));
}
}
return Results.Empty;
})));
R.Set(v_4, (V)"move_extra", new V((RuleFunction)(args_63 => {
V v_64 = args_63[0];
V v_65 = args_63[1];
V v_66 = args_63[2];
var values_67 = R.Join(new V[] {}, new Results((V)0.0d));
V v_68 = values_67[0];
foreach (var row_69 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(v_66, () => R.Table()))))))) {
V v_70 = row_69[0];
V v_71 = row_69[1];
if (R.Truth(R.And((V)(R.Get(v_71, (V)"status") == (V)"open"), () => R.Call(R.Get(v_4, (V)"at"), R.Join(new V[] {v_64, R.Get(v_71, (V)"x")}, new Results(R.Get(v_71, (V)"y")))).First))) {
var values_72 = R.Join(new V[] {}, new Results((V)(v_68 + R.Or(R.And((V)(v_65 == (V)"dash"), () => (V)20.0d), () => (V)40.0d))));
v_68 = values_72[0];
}
}
return R.Join(new V[] {}, new Results(v_68));
return Results.Empty;
})));
R.Set(v_4, (V)"prune", new V((RuleFunction)(args_73 => {
V v_74 = args_73[0];
V v_75 = args_73[1];
{ double end_78 = R.Num((V)1.0d), step_79 = R.Num((-(V)1.0d));
for (double i_77 = R.Num((V)R.Length(R.Or(R.Get(v_74, (V)"idea_silt"), () => R.Table()))); step_79 > 0 ? i_77 <= end_78 : i_77 >= end_78; i_77 += step_79) {
V v_76 = i_77;
var values_80 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_74, (V)"idea_silt"), v_76)));
V v_81 = values_80[0];
if (R.Truth(R.And((V)(R.Get(v_81, (V)"floor_id") == R.Get(v_74, (V)"active_floor_id")), () => R.Or(R.Or(R.Or((V)(R.Get(v_81, (V)"expires") <= R.Get(v_74, (V)"clock")), () => (V)(!R.Truth(R.Get(R.Get(v_75, (V)"rows"), R.Get(v_81, (V)"y"))))), () => (V)(R.Invoke(R.Get(R.Get(v_75, (V)"rows"), R.Get(v_81, (V)"y")), "sub", R.Join(new V[] {R.Get(v_81, (V)"x")}, new Results(R.Get(v_81, (V)"x")))).First != (V)".")), () => R.Call(R.Get(v_4, (V)"protected"), R.Join(new V[] {v_74, v_75, R.Get(v_81, (V)"x")}, new Results(R.Get(v_81, (V)"y")))).First)))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_74, (V)"idea_silt")}, new Results(v_76)));
}
}
}
return Results.Empty;
})));
R.Set(v_4, (V)"leave", new V((RuleFunction)(args_82 => {
V v_83 = args_82[0];
V v_84 = args_82[1];
V v_85 = args_82[2];
V v_86 = args_82[3];
V v_87 = args_82[4];
V v_88 = args_82[5];
V v_89 = args_82[6];
V v_90 = args_82[7];
if (R.Truth((V)(R.Get(v_85, (V)"ability_id") != (V)"core:ability/worm_charge"))) {
return Results.Empty;
}
R.Call(R.Get(v_4, (V)"prune"), R.Join(new V[] {v_83}, new Results(v_89)));
var values_91 = R.Join(new V[] {v_86}, new Results(v_87));
V v_92 = values_91[0];
V v_93 = values_91[1];
{ double end_96 = R.Num(R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)2.0d}, new Results((V)R.Length(v_88)))).First), step_97 = R.Num((V)1.0d);
for (double i_95 = R.Num((V)1.0d); step_97 > 0 ? i_95 <= end_96 : i_95 >= end_96; i_95 += step_97) {
V v_94 = i_95;
if (R.Truth(R.And(R.And(R.And((V)((V)R.Length(R.Or(R.Get(v_83, (V)"idea_silt"), () => R.Table())) < R.Get(v_4, (V)"cap")), () => (V)(R.Invoke(R.Get(R.Get(v_89, (V)"rows"), v_93), "sub", R.Join(new V[] {v_92}, new Results(v_92))).First == (V)".")), () => (V)(!R.Truth(R.Call(R.Get(v_4, (V)"protected"), R.Join(new V[] {v_83, v_89, v_92}, new Results(v_93))).First))), () => (V)(!R.Truth(R.Call(R.Get(v_4, (V)"at"), R.Join(new V[] {v_83, v_92}, new Results(v_93))).First))))) {
var values_98 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_83, (V)"idea_silt"), () => R.Table())));
var obj_99 = v_83;
V key_100 = (V)"idea_silt";
R.Set(obj_99, key_100, values_98[0]);
var values_101 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"action_id", R.Get(v_85, (V)"action_id")), new Entry((V)"source_id", R.Get(v_84, (V)"id")), new Entry((V)"floor_id", R.Get(v_89, (V)"floor_id")), new Entry((V)"x", v_92), new Entry((V)"y", v_93), new Entry((V)"expires", (V)(R.Get(v_83, (V)"clock") + R.Get(v_4, (V)"duration"))))));
V v_102 = values_101[0];
var values_103 = R.Join(new V[] {}, new Results(v_102));
var obj_104 = R.Get(v_83, (V)"idea_silt");
V key_105 = (V)((V)R.Length(R.Get(v_83, (V)"idea_silt")) + (V)1.0d);
R.Set(obj_104, key_105, values_103[0]);
R.Call(v_90, R.Join(new V[] {(V)"SiltTrailCreated"}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_102, (V)"floor_id")), new Entry((V)"x", v_92), new Entry((V)"y", v_93), new Entry((V)"expires", R.Get(v_102, (V)"expires"))))));
}
var values_106 = R.Join(new V[] {R.Get(R.Get(v_88, v_94), (V)"x")}, new Results(R.Get(R.Get(v_88, v_94), (V)"y")));
v_92 = values_106[0];
v_93 = values_106[1];
}
}
return Results.Empty;
})));
R.Set(v_4, (V)"validate", new V((RuleFunction)(args_107 => {
V v_108 = args_107[0];
V v_109 = args_107[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_108, (V)"idea_silt") == V.Nil), () => R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_108, (V)"idea_silt")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_108, (V)"idea_silt")) <= R.Get(v_4, (V)"cap"))))}, new Results((V)"invalid silt trail store")));
var values_110 = R.Join(new V[] {R.Table(), R.Table()}, new Results(R.Table()));
V v_111 = values_110[0];
V v_112 = values_110[1];
V v_113 = values_110[2];
foreach (var row_114 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_108, (V)"entities"))))))) {
V v_115 = row_114[0];
V v_116 = row_114[1];
if (R.Truth((V)(R.Get(v_116, (V)"definition_id") == R.Get(v_4, (V)"id")))) {
var values_117 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_116, (V)"ai_memory"), (V)"silt_charges"), () => (V)0.0d)));
V v_118 = values_117[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_118))).First == (V)"number"), () => (V)((V)(v_118 % (V)1.0d) == (V)0.0d)), () => (V)(v_118 >= (V)0.0d)), () => (V)(v_118 <= (V)2.0d))}, new Results((V)"invalid silt charge budget")));
var values_119 = R.Join(new V[] {}, new Results(R.Get(v_116, (V)"pending_attack")));
V v_120 = values_119[0];
if (R.Truth(R.And(v_120, () => (V)(R.Get(v_120, (V)"ability_id") == (V)"core:ability/worm_charge")))) {
var values_121 = R.Join(new V[] {}, new Results(R.Get(v_120, (V)"advance")));
V v_122 = values_121[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And((V)(v_118 >= (V)1.0d), () => v_122), () => (V)(R.Get(v_122, (V)"max_steps") == (V)2.0d)), () => (V)(R.Get(v_120, (V)"ranged") == (V)true)), () => (V)((V)R.Length(R.Get(v_120, (V)"target_cells")) >= (V)2.0d)), () => (V)((V)R.Length(R.Get(v_120, (V)"target_cells")) <= (V)3.0d))}, new Results((V)"invalid locked silt charge")));
foreach (var row_123 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_120, (V)"target_cells"))))))) {
V v_124 = row_123[0];
V v_125 = row_123[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)(R.Get(v_125, (V)"x") == (V)(R.Get(v_120, (V)"source_x") + (V)(R.Get(v_122, (V)"dx") * v_124))), () => (V)(R.Get(v_125, (V)"y") == (V)(R.Get(v_120, (V)"source_y") + (V)(R.Get(v_122, (V)"dy") * v_124))))}, new Results((V)"unlocked silt lane")));
}
}
}
}
foreach (var row_126 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_108, (V)"idea_silt"), () => R.Table()))))))) {
V v_127 = row_126[0];
V v_128 = row_126[1];
var values_129 = R.Join(new V[] {}, new Results(R.Get(v_109, R.Get(v_128, (V)"floor_id"))));
V v_130 = values_129[0];

var values_131 = R.Join(new V[] {}, R.Call(v_5, R.Join(new V[] {v_108}, new Results(R.Get(v_128, (V)"source_id")))));
V v_132 = values_131[0];
var values_133 = R.Join(new V[] {}, new Results(R.And(R.Get(v_108, (V)"floor_pause"), () => R.Get(R.Get(v_108, (V)"floor_pause"), R.Get(v_128, (V)"floor_id")))));
V v_134 = values_133[0];
var values_135 = R.Join(new V[] {}, new Results(R.Or(R.And(v_134, () => R.Get(v_134, (V)"suspended_at")), () => R.Get(v_108, (V)"clock"))));
V v_136 = values_135[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(v_130, () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_128, (V)"x")))).First == (V)"number")), () => (V)((V)(R.Get(v_128, (V)"x") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_128, (V)"x") >= (V)1.0d)), () => (V)(R.Get(v_128, (V)"x") <= R.Get(v_130, (V)"width"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_128, (V)"y")))).First == (V)"number")), () => (V)((V)(R.Get(v_128, (V)"y") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_128, (V)"y") >= (V)1.0d)), () => (V)(R.Get(v_128, (V)"y") <= R.Get(v_130, (V)"height"))), () => (V)(R.Invoke(R.Get(R.Get(v_130, (V)"rows"), R.Get(v_128, (V)"y")), "sub", R.Join(new V[] {R.Get(v_128, (V)"x")}, new Results(R.Get(v_128, (V)"x")))).First == (V)".")), () => (V)(!R.Truth(R.Call(R.Get(v_4, (V)"protected"), R.Join(new V[] {v_108, v_130, R.Get(v_128, (V)"x")}, new Results(R.Get(v_128, (V)"y")))).First))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_128, (V)"source_id")))).First == (V)"number")), () => (V)((V)(R.Get(v_128, (V)"source_id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_128, (V)"source_id") >= (V)1.0d)), () => (V)(R.Get(v_128, (V)"source_id") < R.Get(v_108, (V)"next_entity_id"))), () => R.Or((V)(!R.Truth(v_132)), () => R.And((V)(R.Get(v_132, (V)"definition_id") == R.Get(v_4, (V)"id")), () => (V)(R.Get(R.Get(v_132, (V)"position"), (V)"floor_id") == R.Get(v_128, (V)"floor_id"))))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_128, (V)"action_id")))).First == (V)"number")), () => (V)((V)(R.Get(v_128, (V)"action_id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_128, (V)"action_id") >= (V)1.0d)), () => (V)(R.Get(v_128, (V)"action_id") < R.Get(v_108, (V)"next_action_id"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_128, (V)"expires")))).First == (V)"number")), () => (V)((V)(R.Get(v_128, (V)"expires") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_128, (V)"expires") > v_136)), () => (V)(R.Get(v_128, (V)"expires") <= (V)(v_136 + R.Get(v_4, (V)"duration"))))}, new Results((V)"invalid saved silt trail")));
var values_137 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_128, (V)"floor_id"), R.Concat((V)":", R.Concat(R.Get(v_128, (V)"x"), R.Concat((V)",", R.Get(v_128, (V)"y")))))));
V v_138 = values_137[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_111, v_138)))}, new Results((V)"overlapping silt trail")));

var values_139 = R.Join(new V[] {}, new Results((V)true));
var obj_140 = v_111;
V key_141 = v_138;
R.Set(obj_140, key_141, values_139[0]);
var values_142 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_112, R.Get(v_128, (V)"action_id")), () => (V)0.0d) + (V)1.0d)));
var obj_143 = v_112;
V key_144 = R.Get(v_128, (V)"action_id");
R.Set(obj_143, key_144, values_142[0]);
var values_145 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_113, R.Get(v_128, (V)"source_id")), () => (V)0.0d) + (V)1.0d)));
var obj_146 = v_113;
V key_147 = R.Get(v_128, (V)"source_id");
R.Set(obj_146, key_147, values_145[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Get(v_112, R.Get(v_128, (V)"action_id")) <= (V)2.0d), () => (V)(R.Get(v_113, R.Get(v_128, (V)"source_id")) <= (V)4.0d)), () => R.Or((V)(!R.Truth(v_132)), () => (V)(R.Get(v_113, R.Get(v_128, (V)"source_id")) <= (V)((V)2.0d * R.Or(R.Get(R.Get(v_132, (V)"ai_memory"), (V)"silt_charges"), () => (V)0.0d)))))}, new Results((V)"silt trail budget exceeded")));
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_4));
return Results.Empty;
} }
