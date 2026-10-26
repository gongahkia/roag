// Static C# port of src/idea_mirrors.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 23de06d35902ad69e282e5d6511edb6762a32c49a1de09977de698a3c88f36e7
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_mirrors { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"core:enemy/mirror_hexer"), new Entry((V)"duration", (V)400.0d), new Entry((V)"cap", (V)8.0d))));
V v_2 = values_1[0];
V v_3 = V.Nil;
v_3 = new V((RuleFunction)(args_4 => {
V v_5 = args_4[0];
V v_6 = args_4[1];
foreach (var row_7 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_5, (V)"entities"))))))) {
V v_8 = row_7[0];
V v_9 = row_7[1];
if (R.Truth((V)(R.Get(v_9, (V)"id") == v_6))) {
return R.Join(new V[] {}, new Results(v_9));
}
}
return Results.Empty;
}));
V v_10 = V.Nil;
v_10 = new V((RuleFunction)(args_11 => {
V v_12 = args_11[0];
V v_13 = args_11[1];
V v_14 = args_11[2];
V v_15 = args_11[3];
foreach (var row_16 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_12, (V)"entities"))))))) {
V v_17 = row_16[0];
V v_18 = row_16[1];
var values_19 = R.Join(new V[] {}, new Results(R.Get(v_18, (V)"position")));
V v_20 = values_19[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_18, (V)"id") != R.Get(v_13, (V)"id")), () => R.Get(v_18, (V)"hp")), () => (V)(R.Get(v_18, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_20, (V)"floor_id") == R.Get(R.Get(v_13, (V)"position"), (V)"floor_id"))), () => (V)(R.Get(v_20, (V)"x") == v_14)), () => (V)(R.Get(v_20, (V)"y") == v_15)))) {
return R.Join(new V[] {}, new Results(v_18));
}
}
return Results.Empty;
}));
V v_21 = V.Nil;
v_21 = new V((RuleFunction)(args_22 => {
V v_23 = args_22[0];
V v_24 = args_22[1];
V v_25 = args_22[2];
V v_26 = args_22[3];
foreach (var row_27 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_24, (V)"placements"), () => R.Get(R.Get(v_24, (V)"placements"), (V)"stairs")), () => R.Table()))))))) {
V v_28 = row_27[0];
V v_29 = row_27[1];
if (R.Truth(R.And((V)(R.Get(v_29, (V)"x") == v_25), () => (V)(R.Get(v_29, (V)"y") == v_26)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
foreach (var row_30 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_23, (V)"entities"))))))) {
V v_31 = row_30[0];
V v_32 = row_30[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_32, (V)"controller") == (V)"exit"), () => (V)(R.Get(R.Get(v_32, (V)"position"), (V)"floor_id") == R.Get(v_24, (V)"floor_id"))), () => (V)(R.Get(R.Get(v_32, (V)"position"), (V)"x") == v_25)), () => (V)(R.Get(R.Get(v_32, (V)"position"), (V)"y") == v_26)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
R.Set(v_2, (V)"fits", new V((RuleFunction)(args_33 => {
V v_34 = args_33[0];
V v_35 = args_33[1];
V v_36 = args_33[2];
V v_37 = args_33[3];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And(R.And(R.And(R.Get(R.Get(R.Get(v_37, (V)"map"), (V)"rows"), R.Get(v_36, (V)"y")), () => (V)(R.Invoke(R.Get(R.Get(R.Get(v_37, (V)"map"), (V)"rows"), R.Get(v_36, (V)"y")), "sub", R.Join(new V[] {R.Get(v_36, (V)"x")}, new Results(R.Get(v_36, (V)"x")))).First == (V)".")), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_35, (V)"position"), (V)"x") - R.Get(v_36, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_35, (V)"position"), (V)"y") - R.Get(v_36, (V)"y"))))).First) == (V)1.0d)), () => R.Call(R.Get(v_37, (V)"free"), R.Join(new V[] {v_34, R.Get(v_35, (V)"id"), R.Get(v_36, (V)"x")}, new Results(R.Get(v_36, (V)"y")))).First), () => R.Call(R.Get(v_37, (V)"visible"), R.Join(new V[] {R.Get(v_36, (V)"x")}, new Results(R.Get(v_36, (V)"y")))).First), () => R.Call(R.Get(v_37, (V)"visible"), R.Join(new V[] {R.Get(R.Get(v_35, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_35, (V)"position"), (V)"y")))).First), () => (V)(!R.Truth(R.Call(v_21, R.Join(new V[] {v_34, R.Get(v_37, (V)"map"), R.Get(v_36, (V)"x")}, new Results(R.Get(v_36, (V)"y")))).First)))));
return Results.Empty;
})));
R.Set(v_2, (V)"choose", new V((RuleFunction)(args_38 => {
V v_39 = args_38[0];
V v_40 = args_38[1];
V v_41 = args_38[2];
V v_42 = args_38[3];
var values_43 = R.Join(new V[] {R.Get(v_40, (V)"position")}, new Results(R.Get(v_41, (V)"position")));
V v_44 = values_43[0];
V v_45 = values_43[1];
var values_46 = R.Join(new V[] {(V)(R.Get(v_45, (V)"x") - R.Get(v_44, (V)"x"))}, new Results((V)(R.Get(v_45, (V)"y") - R.Get(v_44, (V)"y"))));
V v_47 = values_46[0];
V v_48 = values_46[1];

var values_49 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_47))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_48))).First)));
V v_50 = values_49[0];
var values_51 = R.Join(new V[] {}, new Results((V)false));
V v_52 = values_51[0];
foreach (var row_53 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_39, (V)"idea_mirrors"), () => R.Table()))))))) {
V v_54 = row_53[0];
V v_55 = row_53[1];
if (R.Truth(R.And((V)(R.Get(v_55, (V)"source_id") == R.Get(v_40, (V)"id")), () => (V)(R.Get(v_55, (V)"expires") > R.Get(v_39, (V)"clock"))))) {
var values_56 = R.Join(new V[] {}, new Results((V)true));
v_52 = values_56[0];
}
}
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And((V)(!R.Truth(v_52)), () => (V)(R.Or(R.Get(R.Get(v_40, (V)"ai_memory"), (V)"mirror_casts"), () => (V)0.0d) < (V)2.0d)), () => (V)((V)R.Length(R.Or(R.Get(v_39, (V)"idea_mirrors"), () => R.Table())) < R.Get(v_2, (V)"cap"))), () => (V)(v_50 >= (V)2.0d)), () => (V)(v_50 <= (V)4.0d)), () => R.Or((V)(v_47 == (V)0.0d), () => (V)(v_48 == (V)0.0d))), () => R.Call(R.Get(v_42, (V)"clear_line"), R.Join(new V[] {v_39, v_44}, new Results(v_45))).First))) {
var values_57 = R.Join(new V[] {R.Or(R.And((V)(v_47 == (V)0.0d), () => (V)0.0d), () => (V)(v_47 / R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_47))).First))}, new Results(R.Or(R.And((V)(v_48 == (V)0.0d), () => (V)0.0d), () => (V)(v_48 / R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_48))).First))));
V v_58 = values_57[0];
V v_59 = values_57[1];
var values_60 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"mirror"), new Entry((V)"x", (V)(R.Get(v_44, (V)"x") + v_58)), new Entry((V)"y", (V)(R.Get(v_44, (V)"y") + v_59)), new Entry((V)"in_dx", (-v_58)), new Entry((V)"in_dy", (-v_59)))));
V v_61 = values_60[0];
if (R.Truth(R.Call(R.Get(v_2, (V)"fits"), R.Join(new V[] {v_39, v_40, v_61}, new Results(v_42))).First)) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/hexer_mirror"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_61, (V)"x")), new Entry((V)"y", R.Get(v_61, (V)"y")))))), new Entry((V)"support", v_61))));
}
}
if (R.Truth(R.And(R.And(R.And((V)(v_50 >= (V)1.0d), () => (V)(v_50 <= (V)4.0d)), () => R.Or((V)(v_47 == (V)0.0d), () => (V)(v_48 == (V)0.0d))), () => R.Call(R.Get(v_42, (V)"clear_line"), R.Join(new V[] {v_39, v_44}, new Results(v_45))).First))) {
var values_62 = R.Join(new V[] {R.Or(R.And((V)(v_47 == (V)0.0d), () => (V)0.0d), () => (V)(v_47 / R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_47))).First))}, new Results(R.Or(R.And((V)(v_48 == (V)0.0d), () => (V)0.0d), () => (V)(v_48 / R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_48))).First))));
V v_63 = values_62[0];
V v_64 = values_62[1];
var values_65 = R.Join(new V[] {}, new Results(R.Table()));
V v_66 = values_65[0];
{ double end_69 = R.Num(v_50), step_70 = R.Num((V)1.0d);
for (double i_68 = R.Num((V)1.0d); step_70 > 0 ? i_68 <= end_69 : i_68 >= end_69; i_68 += step_70) {
V v_67 = i_68;
var values_71 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)(R.Get(v_44, (V)"x") + (V)(v_63 * v_67))), new Entry((V)"y", (V)(R.Get(v_44, (V)"y") + (V)(v_64 * v_67))))));
var obj_72 = v_66;
V key_73 = (V)((V)R.Length(v_66) + (V)1.0d);
R.Set(obj_72, key_73, values_71[0]);
}
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/hexer_bolt"), new Entry((V)"cells", v_66), new Entry((V)"ranged", (V)true))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(R.Get(v_42, (V)"next_step"), R.Join(new V[] {v_39, v_40}, R.Call(R.Get(v_42, (V)"adjacent_goals"), R.Join(new V[] {v_39, v_40, v_41}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
return Results.Empty;
})));
R.Set(v_2, (V)"create", new V((RuleFunction)(args_74 => {
V v_75 = args_74[0];
V v_76 = args_74[1];
V v_77 = args_74[2];
V v_78 = args_74[3];
var values_79 = R.Join(new V[] {}, new Results(R.Get(v_77, (V)"support")));
V v_80 = values_79[0];
if (R.Truth(R.Or((V)(!R.Truth(R.Call(R.Get(v_2, (V)"fits"), R.Join(new V[] {v_75, v_76, v_80}, new Results(v_78))).First)), () => (V)((V)R.Length(R.Or(R.Get(v_75, (V)"idea_mirrors"), () => R.Table())) >= R.Get(v_2, (V)"cap"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
foreach (var row_81 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_75, (V)"idea_mirrors"), () => R.Table()))))))) {
V v_82 = row_81[0];
V v_83 = row_81[1];
if (R.Truth(R.Or((V)(R.Get(v_83, (V)"source_id") == R.Get(v_76, (V)"id")), () => R.And(R.And((V)(R.Get(v_83, (V)"floor_id") == R.Get(R.Get(v_76, (V)"position"), (V)"floor_id")), () => (V)(R.Get(v_83, (V)"x") == R.Get(v_80, (V)"x"))), () => (V)(R.Get(v_83, (V)"y") == R.Get(v_80, (V)"y")))))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
var values_84 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_77, (V)"action_id")), new Entry((V)"source_id", R.Get(v_76, (V)"id")), new Entry((V)"floor_id", R.Get(R.Get(v_76, (V)"position"), (V)"floor_id")), new Entry((V)"x", R.Get(v_80, (V)"x")), new Entry((V)"y", R.Get(v_80, (V)"y")), new Entry((V)"source_x", R.Get(v_77, (V)"source_x")), new Entry((V)"source_y", R.Get(v_77, (V)"source_y")), new Entry((V)"in_dx", R.Get(v_80, (V)"in_dx")), new Entry((V)"in_dy", R.Get(v_80, (V)"in_dy")), new Entry((V)"expires", (V)(R.Get(v_75, (V)"clock") + R.Get(v_2, (V)"duration"))))));
V v_85 = values_84[0];
var values_86 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_75, (V)"idea_mirrors"), () => R.Table())));
var obj_87 = v_75;
V key_88 = (V)"idea_mirrors";
R.Set(obj_87, key_88, values_86[0]);
var values_89 = R.Join(new V[] {}, new Results(v_85));
var obj_90 = R.Get(v_75, (V)"idea_mirrors");
V key_91 = (V)((V)R.Length(R.Get(v_75, (V)"idea_mirrors")) + (V)1.0d);
R.Set(obj_90, key_91, values_89[0]);
return R.Join(new V[] {(V)true}, new Results(v_85));
return Results.Empty;
})));
R.Set(v_2, (V)"route", new V((RuleFunction)(args_92 => {
V v_93 = args_92[0];
V v_94 = args_92[1];
V v_95 = args_92[2];
V v_96 = args_92[3];
V v_97 = args_92[4];
if (R.Truth(R.Or(R.Or((V)((V)R.Length(R.Or(R.Get(v_93, (V)"idea_mirrors"), () => R.Table())) == (V)0.0d), () => R.And((V)(v_96 != (V)true), () => (V)(v_96 != (V)"piercing_line"))), () => (V)((V)R.Length(v_95) < (V)2.0d)))) {
return R.Join(new V[] {}, new Results(v_95));
}
var values_98 = R.Join(new V[] {}, new Results(R.Get(v_94, (V)"position")));
V v_99 = values_98[0];

var values_100 = R.Join(new V[] {(V)(R.Get(R.Get(v_95, (V)1.0d), (V)"x") - R.Get(v_99, (V)"x"))}, new Results((V)(R.Get(R.Get(v_95, (V)1.0d), (V)"y") - R.Get(v_99, (V)"y"))));
V v_101 = values_100[0];
V v_102 = values_100[1];
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_101))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_102))).First) != (V)1.0d))) {
return R.Join(new V[] {}, new Results(v_95));
}
foreach (var row_103 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_95)))))) {
V v_104 = row_103[0];
V v_105 = row_103[1];
if (R.Truth(R.Or((V)(R.Get(v_105, (V)"x") != (V)(R.Get(v_99, (V)"x") + (V)(v_101 * v_104))), () => (V)(R.Get(v_105, (V)"y") != (V)(R.Get(v_99, (V)"y") + (V)(v_102 * v_104)))))) {
return R.Join(new V[] {}, new Results(v_95));
}
}
var values_106 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_107 = values_106[0];

var values_108 = R.Join(new V[] {}, R.Call(R.Get(v_107, (V)"map_for"), R.Join(new V[] {}, new Results(v_93))));
V v_109 = values_108[0];
foreach (var row_110 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_95)))))) {
V v_111 = row_110[0];
V v_112 = row_110[1];
var values_113 = R.Join(new V[] {}, new Results(R.And((V)(v_96 == (V)true), () => R.Call(v_10, R.Join(new V[] {v_93, v_94, R.Get(v_112, (V)"x")}, new Results(R.Get(v_112, (V)"y")))).First)));
V v_114 = values_113[0];
var values_115 = R.Join(new V[] {}, new Results(R.And(v_114, () => R.Or((V)(!R.Truth(v_97)), () => R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))).First, (V)"is_visible"), R.Join(new V[] {v_93, R.Get(R.Get(v_114, (V)"position"), (V)"floor_id"), R.Get(v_112, (V)"x")}, new Results(R.Get(v_112, (V)"y")))).First))));
V v_116 = values_115[0];
if (R.Truth(R.Or((V)(!R.Truth(R.Call(R.Get(v_107, (V)"projectile_passable"), R.Join(new V[] {v_109, R.Get(v_112, (V)"x"), R.Get(v_112, (V)"y")}, new Results(v_93))).First)), () => v_116))) {
return R.Join(new V[] {}, new Results(v_95));
}
foreach (var row_117 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_93, (V)"idea_mirrors"))))))) {
V v_118 = row_117[0];
V v_119 = row_117[1];
var values_120 = R.Join(new V[] {}, R.Call(v_3, R.Join(new V[] {v_93}, new Results(R.Get(v_119, (V)"source_id")))));
V v_121 = values_120[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(v_111 < (V)R.Length(v_95)), () => (V)(R.Get(v_119, (V)"floor_id") == R.Get(v_99, (V)"floor_id"))), () => (V)(R.Get(v_119, (V)"expires") > R.Get(v_93, (V)"clock"))), () => (V)(R.Get(v_119, (V)"x") == R.Get(v_112, (V)"x"))), () => (V)(R.Get(v_119, (V)"y") == R.Get(v_112, (V)"y"))), () => (V)(R.Get(v_119, (V)"in_dx") == v_101)), () => (V)(R.Get(v_119, (V)"in_dy") == v_102)), () => v_121), () => (V)(R.Get(v_121, (V)"hp") > (V)0.0d)))) {
var values_122 = R.Join(new V[] {}, new Results(R.Table()));
V v_123 = values_122[0];
{ double end_126 = R.Num(v_111), step_127 = R.Num((V)1.0d);
for (double i_125 = R.Num((V)1.0d); step_127 > 0 ? i_125 <= end_126 : i_125 >= end_126; i_125 += step_127) {
V v_124 = i_125;
var values_128 = R.Join(new V[] {}, new Results(R.Get(v_95, v_124)));
var obj_129 = v_123;
V key_130 = (V)((V)R.Length(v_123) + (V)1.0d);
R.Set(obj_129, key_130, values_128[0]);
}
}
var values_131 = R.Join(new V[] {(-v_102)}, new Results(v_101));
V v_132 = values_131[0];
V v_133 = values_131[1];
{ double end_136 = R.Num((V)((V)R.Length(v_95) - v_111)), step_137 = R.Num((V)1.0d);
for (double i_135 = R.Num((V)1.0d); step_137 > 0 ? i_135 <= end_136 : i_135 >= end_136; i_135 += step_137) {
V v_134 = i_135;
var values_138 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_99, (V)"floor_id")), new Entry((V)"x", (V)(R.Get(v_119, (V)"x") + (V)(v_132 * v_134))), new Entry((V)"y", (V)(R.Get(v_119, (V)"y") + (V)(v_133 * v_134))))));
var obj_139 = v_123;
V key_140 = (V)((V)R.Length(v_123) + (V)1.0d);
R.Set(obj_139, key_140, values_138[0]);
}
}
return R.Join(new V[] {v_123}, new Results(R.Table(new Entry((V)"id", R.Get(v_119, (V)"id")), new Entry((V)"source_id", R.Get(v_119, (V)"source_id")), new Entry((V)"x", R.Get(v_119, (V)"x")), new Entry((V)"y", R.Get(v_119, (V)"y")), new Entry((V)"in_dx", v_101), new Entry((V)"in_dy", v_102), new Entry((V)"out_dx", v_132), new Entry((V)"out_dy", v_133), new Entry((V)"step", v_111))));
}
}
}
return R.Join(new V[] {}, new Results(v_95));
return Results.Empty;
})));
R.Set(v_2, (V)"consume", new V((RuleFunction)(args_141 => {
V v_142 = args_141[0];
V v_143 = args_141[1];
if (R.Truth((V)(!R.Truth(v_143)))) {
return Results.Empty;
}
foreach (var row_144 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_142, (V)"idea_mirrors"), () => R.Table()))))))) {
V v_145 = row_144[0];
V v_146 = row_144[1];
if (R.Truth((V)(R.Get(v_146, (V)"id") == R.Get(v_143, (V)"id")))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_142, (V)"idea_mirrors")}, new Results(v_145)));

return R.Join(new V[] {}, new Results((V)true));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"committed reflector disappeared before activation")));
return Results.Empty;
})));
R.Set(v_2, (V)"prune", new V((RuleFunction)(args_147 => {
V v_148 = args_147[0];
V v_149 = args_147[1];
{ double end_152 = R.Num((V)1.0d), step_153 = R.Num((-(V)1.0d));
for (double i_151 = R.Num((V)R.Length(R.Or(R.Get(v_148, (V)"idea_mirrors"), () => R.Table()))); step_153 > 0 ? i_151 <= end_152 : i_151 >= end_152; i_151 += step_153) {
V v_150 = i_151;
var values_154 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_148, (V)"idea_mirrors"), v_150)));
V v_155 = values_154[0];

var values_156 = R.Join(new V[] {}, R.Call(v_3, R.Join(new V[] {v_148}, new Results(R.Get(v_155, (V)"source_id")))));
V v_157 = values_156[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_157)), () => (V)(R.Get(v_157, (V)"hp") <= (V)0.0d)), () => R.And((V)(R.Get(v_155, (V)"floor_id") == R.Get(v_148, (V)"active_floor_id")), () => R.Or(R.Or(R.Or((V)(R.Get(v_155, (V)"expires") <= R.Get(v_148, (V)"clock")), () => (V)(!R.Truth(R.Get(R.Get(v_149, (V)"rows"), R.Get(v_155, (V)"y"))))), () => (V)(R.Invoke(R.Get(R.Get(v_149, (V)"rows"), R.Get(v_155, (V)"y")), "sub", R.Join(new V[] {R.Get(v_155, (V)"x")}, new Results(R.Get(v_155, (V)"x")))).First != (V)".")), () => R.Call(v_21, R.Join(new V[] {v_148, v_149, R.Get(v_155, (V)"x")}, new Results(R.Get(v_155, (V)"y")))).First))))) {
R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {R.Get(v_148, (V)"idea_mirrors")}, new Results(v_150)));
}
}
}
return Results.Empty;
})));
V v_158 = V.Nil;
v_158 = new V((RuleFunction)(args_159 => {
V v_160 = args_159[0];
V v_161 = args_159[1];
return R.Join(new V[] {}, new Results(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_160))).First == (V)"number"), () => (V)((V)(v_160 % (V)1.0d) == (V)0.0d)), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_161))).First == (V)"number")), () => (V)((V)(v_161 % (V)1.0d) == (V)0.0d)), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_160))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_161))).First) == (V)1.0d))));
return Results.Empty;
}));
R.Set(v_2, (V)"validate", new V((RuleFunction)(args_162 => {
V v_163 = args_162[0];
V v_164 = args_162[1];
var values_165 = R.Join(new V[] {R.Table(), R.Table()}, new Results(R.Table()));
V v_166 = values_165[0];
V v_167 = values_165[1];
V v_168 = values_165[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(R.Get(v_163, (V)"idea_mirrors") == V.Nil), () => R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_163, (V)"idea_mirrors")))).First == (V)"table"), () => (V)((V)R.Length(R.Get(v_163, (V)"idea_mirrors")) <= R.Get(v_2, (V)"cap"))))}, new Results((V)"invalid mirror store")));
foreach (var row_169 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_163, (V)"entities"))))))) {
V v_170 = row_169[0];
V v_171 = row_169[1];
if (R.Truth((V)(R.Get(v_171, (V)"definition_id") == R.Get(v_2, (V)"id")))) {
var values_172 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_171, (V)"ai_memory"), (V)"mirror_casts"), () => (V)0.0d)));
V v_173 = values_172[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_173))).First == (V)"number"), () => (V)((V)(v_173 % (V)1.0d) == (V)0.0d)), () => (V)(v_173 >= (V)0.0d)), () => (V)(v_173 <= (V)2.0d))}, new Results((V)"invalid mirror budget")));
}
var values_174 = R.Join(new V[] {}, new Results(R.Get(v_171, (V)"pending_attack")));
V v_175 = values_174[0];
if (R.Truth(R.And(v_175, () => R.Or((V)(R.Get(v_175, (V)"ability_id") == (V)"core:ability/hexer_mirror"), () => R.And(R.Get(v_175, (V)"support"), () => (V)(R.Get(R.Get(v_175, (V)"support"), (V)"kind") == (V)"mirror")))))) {
var values_176 = R.Join(new V[] {}, new Results(R.Get(v_175, (V)"support")));
V v_177 = values_176[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_171, (V)"definition_id") == R.Get(v_2, (V)"id")), () => (V)(R.Get(v_175, (V)"ability_id") == (V)"core:ability/hexer_mirror")), () => v_177), () => (V)(R.Get(v_177, (V)"kind") == (V)"mirror")), () => R.Call(v_158, R.Join(new V[] {R.Get(v_177, (V)"in_dx")}, new Results(R.Get(v_177, (V)"in_dy")))).First), () => R.Get(R.Get(v_171, (V)"ai_memory"), (V)"mirror_casts")), () => (V)(R.Get(R.Get(v_171, (V)"ai_memory"), (V)"mirror_casts") >= (V)1.0d)), () => (V)(R.Get(v_177, (V)"x") == (V)(R.Get(v_175, (V)"source_x") - R.Get(v_177, (V)"in_dx")))), () => (V)(R.Get(v_177, (V)"y") == (V)(R.Get(v_175, (V)"source_y") - R.Get(v_177, (V)"in_dy")))), () => (V)((V)R.Length(R.Get(v_175, (V)"target_cells")) == (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_175, (V)"target_cells"), (V)1.0d), (V)"x") == R.Get(v_177, (V)"x"))), () => (V)(R.Get(R.Get(R.Get(v_175, (V)"target_cells"), (V)1.0d), (V)"y") == R.Get(v_177, (V)"y")))}, new Results((V)"invalid committed mirror")));
}
if (R.Truth(R.And(v_175, () => R.Get(v_175, (V)"reflection")))) {
var values_178 = R.Join(new V[] {}, new Results(R.Get(v_175, (V)"reflection")));
V v_179 = values_178[0];

var values_180 = R.Join(new V[] {}, new Results(R.Get(v_179, (V)"step")));
V v_181 = values_180[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_179, (V)"id")))).First == (V)"number"), () => (V)((V)(R.Get(v_179, (V)"id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_179, (V)"id") >= (V)1.0d)), () => (V)(R.Get(v_179, (V)"id") < R.Get(v_163, (V)"next_action_id"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_179, (V)"source_id")))).First == (V)"number")), () => (V)((V)(R.Get(v_179, (V)"source_id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_179, (V)"source_id") >= (V)1.0d)), () => (V)(R.Get(v_179, (V)"source_id") < R.Get(v_163, (V)"next_entity_id"))), () => R.Call(v_158, R.Join(new V[] {R.Get(v_179, (V)"in_dx")}, new Results(R.Get(v_179, (V)"in_dy")))).First), () => (V)(R.Get(v_179, (V)"out_dx") == (-R.Get(v_179, (V)"in_dy")))), () => (V)(R.Get(v_179, (V)"out_dy") == R.Get(v_179, (V)"in_dx"))), () => R.Or((V)(R.Get(v_175, (V)"ranged") == (V)true), () => (V)(R.Get(v_175, (V)"ranged") == (V)"piercing_line"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_181))).First == (V)"number")), () => (V)((V)(v_181 % (V)1.0d) == (V)0.0d)), () => (V)(v_181 >= (V)1.0d)), () => (V)(v_181 < (V)R.Length(R.Get(v_175, (V)"target_cells"))))}, new Results((V)"invalid locked reflection")));
foreach (var row_182 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_175, (V)"target_cells"))))))) {
V v_183 = row_182[0];
V v_184 = row_182[1];
var values_185 = Results.Empty;
V v_186 = values_185[0];
V v_187 = values_185[1];
if (R.Truth((V)(v_183 <= v_181))) {
var values_188 = R.Join(new V[] {(V)(R.Get(v_175, (V)"source_x") + (V)(R.Get(v_179, (V)"in_dx") * v_183))}, new Results((V)(R.Get(v_175, (V)"source_y") + (V)(R.Get(v_179, (V)"in_dy") * v_183))));
v_186 = values_188[0];
v_187 = values_188[1];
} else {
var values_189 = R.Join(new V[] {(V)(R.Get(v_179, (V)"x") + (V)(R.Get(v_179, (V)"out_dx") * (V)(v_183 - v_181)))}, new Results((V)(R.Get(v_179, (V)"y") + (V)(R.Get(v_179, (V)"out_dy") * (V)(v_183 - v_181)))));
v_186 = values_189[0];
v_187 = values_189[1];
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And((V)(R.Get(v_184, (V)"x") == v_186), () => (V)(R.Get(v_184, (V)"y") == v_187)), () => R.Or((V)(v_183 != v_181), () => R.And((V)(R.Get(v_184, (V)"x") == R.Get(v_179, (V)"x")), () => (V)(R.Get(v_184, (V)"y") == R.Get(v_179, (V)"y")))))}, new Results((V)"unlocked reflected path")));
}
}
}
foreach (var row_190 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_163, (V)"idea_mirrors"), () => R.Table()))))))) {
V v_191 = row_190[0];
V v_192 = row_190[1];
var values_193 = R.Join(new V[] {}, R.Call(v_3, R.Join(new V[] {v_163}, new Results(R.Get(v_192, (V)"source_id")))));
V v_194 = values_193[0];

var values_195 = R.Join(new V[] {}, new Results(R.Get(v_164, R.Get(v_192, (V)"floor_id"))));
V v_196 = values_195[0];
var values_197 = R.Join(new V[] {}, new Results(R.And(R.Get(v_163, (V)"floor_pause"), () => R.Get(R.Get(v_163, (V)"floor_pause"), R.Get(v_192, (V)"floor_id")))));
V v_198 = values_197[0];
var values_199 = R.Join(new V[] {}, new Results(R.Or(R.And(v_198, () => R.Get(v_198, (V)"suspended_at")), () => R.Get(v_163, (V)"clock"))));
V v_200 = values_199[0];
var values_201 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_192, (V)"floor_id"), R.Concat((V)":", R.Concat(R.Get(v_192, (V)"x"), R.Concat((V)",", R.Get(v_192, (V)"y")))))));
V v_202 = values_201[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_192, (V)"id")))).First == (V)"number"), () => (V)((V)(R.Get(v_192, (V)"id") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_192, (V)"id") >= (V)1.0d)), () => (V)(R.Get(v_192, (V)"id") < R.Get(v_163, (V)"next_action_id"))), () => (V)(!R.Truth(R.Get(v_166, R.Get(v_192, (V)"id"))))), () => (V)(!R.Truth(R.Get(v_168, v_202)))), () => (V)(!R.Truth(R.Get(v_167, R.Get(v_192, (V)"source_id"))))), () => v_194), () => (V)(R.Get(v_194, (V)"hp") > (V)0.0d)), () => (V)(R.Get(v_194, (V)"definition_id") == R.Get(v_2, (V)"id"))), () => (V)(R.Get(R.Get(v_194, (V)"position"), (V)"floor_id") == R.Get(v_192, (V)"floor_id"))), () => (V)(R.Or(R.Get(R.Get(v_194, (V)"ai_memory"), (V)"mirror_casts"), () => (V)0.0d) >= (V)1.0d)), () => v_196), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_192, (V)"x")))).First == (V)"number")), () => (V)((V)(R.Get(v_192, (V)"x") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_192, (V)"x") >= (V)1.0d)), () => (V)(R.Get(v_192, (V)"x") <= R.Get(v_196, (V)"width"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_192, (V)"y")))).First == (V)"number")), () => (V)((V)(R.Get(v_192, (V)"y") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_192, (V)"y") >= (V)1.0d)), () => (V)(R.Get(v_192, (V)"y") <= R.Get(v_196, (V)"height"))), () => (V)(R.Invoke(R.Get(R.Get(v_196, (V)"rows"), R.Get(v_192, (V)"y")), "sub", R.Join(new V[] {R.Get(v_192, (V)"x")}, new Results(R.Get(v_192, (V)"x")))).First == (V)".")), () => R.Call(v_158, R.Join(new V[] {R.Get(v_192, (V)"in_dx")}, new Results(R.Get(v_192, (V)"in_dy")))).First), () => (V)(R.Get(v_192, (V)"source_x") == (V)(R.Get(v_192, (V)"x") + R.Get(v_192, (V)"in_dx")))), () => (V)(R.Get(v_192, (V)"source_y") == (V)(R.Get(v_192, (V)"y") + R.Get(v_192, (V)"in_dy")))), () => (V)(R.Get(v_192, (V)"source_x") >= (V)1.0d)), () => (V)(R.Get(v_192, (V)"source_x") <= R.Get(v_196, (V)"width"))), () => (V)(R.Get(v_192, (V)"source_y") >= (V)1.0d)), () => (V)(R.Get(v_192, (V)"source_y") <= R.Get(v_196, (V)"height"))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_192, (V)"expires")))).First == (V)"number")), () => (V)((V)(R.Get(v_192, (V)"expires") % (V)1.0d) == (V)0.0d)), () => (V)(R.Get(v_192, (V)"expires") > v_200)), () => (V)(R.Get(v_192, (V)"expires") <= (V)(v_200 + R.Get(v_2, (V)"duration"))))}, new Results((V)"invalid active mirror")));
var values_203 = R.Join(new V[] {}, new Results((V)true));
var obj_204 = v_166;
V key_205 = R.Get(v_192, (V)"id");
R.Set(obj_204, key_205, values_203[0]);

var values_206 = R.Join(new V[] {}, new Results((V)true));
var obj_207 = v_167;
V key_208 = R.Get(v_192, (V)"source_id");
R.Set(obj_207, key_208, values_206[0]);

var values_209 = R.Join(new V[] {}, new Results((V)true));
var obj_210 = v_168;
V key_211 = v_202;
R.Set(obj_210, key_211, values_209[0]);
}
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_2));
return Results.Empty;
} }
