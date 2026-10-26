// Static C# port of src/boon_patterns.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 739cb13ea1d871450edd5f61f54d420fda519960b4e35364e2c6fb0c5ef043cb
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_boon_patterns { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.targeting"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"names", R.Table(new Entry((V)"lance", (V)true), new Entry((V)"rook", (V)true), new Entry((V)"bishop", (V)true), new Entry((V)"knight", (V)true), new Entry((V)"crown", (V)true), new Entry((V)"crosswind", (V)true), new Entry((V)"fan", (V)true), new Entry((V)"backtrail", (V)true))))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"pommel_return", R.Table(new Entry((V)"class_id", (V)"core:actor/adventurer"), new Entry((V)"ability_id", (V)"core:ability/basic_attack"), new Entry((V)"hint", (V)"clear rear cell; aimed F only"))), new Entry((V)"needle_flight", R.Table(new Entry((V)"class_id", (V)"core:actor/ranger"), new Entry((V)"ability_id", (V)"core:ability/ranger_shot"), new Entry((V)"hint", (V)"piercing line; aimed F only"), new Entry((V)"route_first", (V)"piercing_line"), new Entry((V)"explicit_only", (V)true))), new Entry((V)"hollow_sigil", R.Table(new Entry((V)"class_id", (V)"core:actor/arcanist"), new Entry((V)"ability_id", (V)"core:ability/arcanist_sigil"), new Entry((V)"hint", (V)"hollow sigil; center spared"))), new Entry((V)"backswing", R.Table(new Entry((V)"class_id", (V)"core:actor/berserker"), new Entry((V)"ability_id", (V)"core:ability/berserker_cleave"), new Entry((V)"hint", (V)"cleave includes clear rear cells"))), new Entry((V)"distant_bastion", R.Table(new Entry((V)"class_id", (V)"core:actor/sentinel"), new Entry((V)"ability_id", (V)"core:ability/sentinel_pulse"), new Entry((V)"hint", (V)"two-step pulse; near cardinals spared"))), new Entry((V)"volatile_vial", R.Table(new Entry((V)"class_id", (V)"core:actor/alchemist"), new Entry((V)"ability_id", (V)"core:ability/alchemist_vial"), new Entry((V)"hint", (V)"cross splash on first contact"), new Entry((V)"route_first", (V)true))), new Entry((V)"split_thrust", R.Table(new Entry((V)"class_id", (V)"core:actor/duelist"), new Entry((V)"ability_id", (V)"core:ability/duelist_thrust"), new Entry((V)"hint", (V)"forked second step; aimed F only"))))));
var obj_10 = v_8;
V key_11 = (V)"weapon_forms";
R.Set(obj_10, key_11, values_9[0]);
var values_12 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d))))));
V v_15 = values_14[0];
V v_16 = V.Nil;
v_16 = new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
return R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_18 < (V)0.0d), () => (-(V)1.0d)), () => R.And((V)(v_18 > (V)0.0d), () => (V)1.0d)), () => (V)0.0d)));
return Results.Empty;
}));
R.Set(v_8, (V)"clear_segment", new V((RuleFunction)(args_19 => {
V v_20 = args_19[0];
V v_21 = args_19[1];
V v_22 = args_19[2];
V v_23 = args_19[3];
V v_24 = args_19[4];
V v_25 = args_19[5];
var values_26 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_20))));
V v_27 = values_26[0];
if (R.Truth(R.Or((V)(!R.Truth(v_27)), () => (V)(R.Get(v_27, (V)"floor_id") != v_21)))) {
return R.Join(new V[] {}, new Results((V)false));
}
V v_28 = V.Nil;
v_28 = new V((RuleFunction)(args_29 => {
V v_30 = args_29[0];
V v_31 = args_29[1];
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"projectile_passable"), R.Join(new V[] {v_27, v_30, v_31}, new Results(v_20))));
return Results.Empty;
}));
var values_32 = R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_24 - v_22)))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_25 - v_23)))));
V v_33 = values_32[0];
V v_34 = values_32[1];
var values_35 = R.Join(new V[] {R.Call(v_16, R.Join(new V[] {}, new Results((V)(v_24 - v_22)))).First}, R.Call(v_16, R.Join(new V[] {}, new Results((V)(v_25 - v_23)))));
V v_36 = values_35[0];
V v_37 = values_35[1];
var values_38 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_39 = values_38[0];
V v_40 = values_38[1];
while (R.Truth(R.Or((V)(v_39 < v_33), () => (V)(v_40 < v_34)))) {
var values_41 = R.Join(new V[] {(V)((V)((V)1.0d + (V)((V)2.0d * v_39)) * v_34)}, new Results((V)((V)((V)1.0d + (V)((V)2.0d * v_40)) * v_33)));
V v_42 = values_41[0];
V v_43 = values_41[1];
var values_44 = R.Join(new V[] {v_22}, new Results(v_23));
V v_45 = values_44[0];
V v_46 = values_44[1];
if (R.Truth((V)(v_42 == v_43))) {
var values_47 = R.Join(new V[] {(V)(v_22 + v_36)}, new Results((V)(v_23 + v_37)));
v_45 = values_47[0];
v_46 = values_47[1];

var values_48 = R.Join(new V[] {(V)(v_39 + (V)1.0d)}, new Results((V)(v_40 + (V)1.0d)));
v_39 = values_48[0];
v_40 = values_48[1];
} else if (R.Truth((V)(v_42 < v_43))) {
var values_49 = R.Join(new V[] {}, new Results((V)(v_22 + v_36)));
v_45 = values_49[0];

var values_50 = R.Join(new V[] {}, new Results((V)(v_39 + (V)1.0d)));
v_39 = values_50[0];
} else {
var values_51 = R.Join(new V[] {}, new Results((V)(v_23 + v_37)));
v_46 = values_51[0];

var values_52 = R.Join(new V[] {}, new Results((V)(v_40 + (V)1.0d)));
v_40 = values_52[0];
}
if (R.Truth(R.Or((V)(!R.Truth(R.Call(v_28, R.Join(new V[] {v_45}, new Results(v_46))).First)), () => R.And(R.And((V)(v_45 != v_22), () => (V)(v_46 != v_23)), () => R.Or((V)(!R.Truth(R.Call(v_28, R.Join(new V[] {v_22}, new Results(v_46))).First)), () => (V)(!R.Truth(R.Call(v_28, R.Join(new V[] {v_45}, new Results(v_23))).First))))))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_53 = R.Join(new V[] {v_45}, new Results(v_46));
v_22 = values_53[0];
v_23 = values_53[1];
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
R.Set(v_8, (V)"axis", new V((RuleFunction)(args_54 => {
V v_55 = args_54[0];
V v_56 = args_54[1];
V v_57 = args_54[2];
var values_58 = R.Join(new V[] {}, new Results(R.Or(R.Or(v_57, () => R.Get(v_55, (V)"pattern_origin")), () => R.Get(v_55, (V)"source_position"))));
V v_59 = values_58[0];
if (R.Truth(R.Or(R.Or(R.Or((V)(!R.Truth(v_59)), () => (V)(!R.Truth(R.Get(v_55, (V)"x")))), () => (V)(!R.Truth(R.Get(v_55, (V)"y")))), () => (V)(R.Get(v_59, (V)"floor_id") != R.Get(v_55, (V)"floor_id"))))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_60 = R.Join(new V[] {(V)(R.Get(v_55, (V)"x") - R.Get(v_59, (V)"x"))}, new Results((V)(R.Get(v_55, (V)"y") - R.Get(v_59, (V)"y"))));
V v_61 = values_60[0];
V v_62 = values_60[1];
if (R.Truth((V)(v_56 == (V)"cardinal"))) {
return R.Join(new V[] {}, new Results((V)((V)(v_61 == (V)0.0d) != (V)(v_62 == (V)0.0d))));
}
return R.Join(new V[] {}, new Results(R.And((V)(v_61 != (V)0.0d), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_61))).First == R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_62))).First))));
return Results.Empty;
})));
R.Set(v_8, (V)"cells", new V((RuleFunction)(args_63 => {
V v_64 = args_63[0];
V v_65 = args_63[1];
V v_66 = args_63[2];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(R.Get(v_8, (V)"names"), v_65)}, new Results((V)"unknown boon pattern")));
var values_67 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_66, (V)"pattern_origin"), () => R.Get(v_66, (V)"source_position"))));
V v_68 = values_67[0];
var values_69 = R.Join(new V[] {R.Or(R.Get(v_66, (V)"x"), () => R.Get(v_66, (V)"from_x"))}, new Results(R.Or(R.Get(v_66, (V)"y"), () => R.Get(v_66, (V)"from_y"))));
V v_70 = values_69[0];
V v_71 = values_69[1];
if (R.Truth((V)(v_65 == (V)"backtrail"))) {
var values_72 = R.Join(new V[] {R.Get(v_66, (V)"from_x")}, new Results(R.Get(v_66, (V)"from_y")));
v_70 = values_72[0];
v_71 = values_72[1];
}
var values_73 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_66, (V)"floor_id"), () => R.And(v_68, () => R.Get(v_68, (V)"floor_id")))));
V v_74 = values_73[0];
var values_75 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_64))));
V v_76 = values_75[0];
if (R.Truth(R.Or(R.Or(R.Or((V)(!R.Truth(v_70)), () => (V)(!R.Truth(v_71))), () => (V)(!R.Truth(v_76))), () => (V)(v_74 != R.Get(v_76, (V)"floor_id"))))) {
return R.Join(new V[] {}, new Results(R.Table()));
}
var values_77 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_78 = values_77[0];
V v_79 = values_77[1];
V v_80 = V.Nil;
v_80 = new V((RuleFunction)(args_81 => {
V v_82 = args_81[0];
V v_83 = args_81[1];
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"projectile_passable"), R.Join(new V[] {v_76, v_82, v_83}, new Results(v_64))));
return Results.Empty;
}));
V v_84 = V.Nil;
v_84 = new V((RuleFunction)(args_85 => {
V v_86 = args_85[0];
V v_87 = args_85[1];
var values_88 = R.Join(new V[] {}, new Results(R.Concat(v_86, R.Concat((V)":", v_87))));
V v_89 = values_88[0];
if (R.Truth(R.And((V)(!R.Truth(R.Get(v_79, v_89))), () => R.Call(v_80, R.Join(new V[] {v_86}, new Results(v_87))).First))) {
var values_90 = R.Join(new V[] {}, new Results((V)true));
var obj_91 = v_79;
V key_92 = v_89;
R.Set(obj_91, key_92, values_90[0]);

var values_93 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", v_74), new Entry((V)"x", v_86), new Entry((V)"y", v_87))));
var obj_94 = v_78;
V key_95 = (V)((V)R.Length(v_78) + (V)1.0d);
R.Set(obj_94, key_95, values_93[0]);
}
return Results.Empty;
}));
V v_96 = V.Nil;
v_96 = new V((RuleFunction)(args_97 => {
V v_98 = args_97[0];
V v_99 = args_97[1];
V v_100 = args_97[2];
V v_101 = args_97[3];
return R.Join(new V[] {}, new Results(R.And(R.Call(v_80, R.Join(new V[] {v_100}, new Results(v_101))).First, () => R.Or(R.Or((V)(v_98 == v_100), () => (V)(v_99 == v_101)), () => R.And(R.Call(v_80, R.Join(new V[] {v_98}, new Results(v_101))).First, () => R.Call(v_80, R.Join(new V[] {v_100}, new Results(v_99))).First)))));
return Results.Empty;
}));
V v_102 = V.Nil;
v_102 = new V((RuleFunction)(args_103 => {
V v_104 = args_103[0];
V v_105 = args_103[1];
V v_106 = args_103[2];
V v_107 = args_103[3];
return R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"clear_segment"), R.Join(new V[] {v_64, v_74, v_104, v_105, v_106}, new Results(v_107))));
return Results.Empty;
}));
V v_108 = V.Nil;
v_108 = new V((RuleFunction)(args_109 => {
V v_110 = args_109[0];
V v_111 = args_109[1];
V v_112 = args_109[2];
var values_113 = R.Join(new V[] {v_70}, new Results(v_71));
V v_114 = values_113[0];
V v_115 = values_113[1];
{ double end_118 = R.Num(v_112), step_119 = R.Num((V)1.0d);
for (double i_117 = R.Num((V)1.0d); step_119 > 0 ? i_117 <= end_118 : i_117 >= end_118; i_117 += step_119) {
V v_116 = i_117;
var values_120 = R.Join(new V[] {(V)(v_114 + v_110)}, new Results((V)(v_115 + v_111)));
V v_121 = values_120[0];
V v_122 = values_120[1];
if (R.Truth((V)(!R.Truth(R.Call(v_96, R.Join(new V[] {v_114, v_115, v_121}, new Results(v_122))).First)))) {
break;
}
R.Call(v_84, R.Join(new V[] {v_121}, new Results(v_122)));

var values_123 = R.Join(new V[] {v_121}, new Results(v_122));
v_114 = values_123[0];
v_115 = values_123[1];
}
}
return Results.Empty;
}));
var values_124 = R.Join(new V[] {R.Or(R.And(v_68, () => R.Call(v_16, R.Join(new V[] {}, new Results((V)(v_70 - R.Get(v_68, (V)"x"))))).First), () => (V)0.0d)}, new Results(R.Or(R.And(v_68, () => R.Call(v_16, R.Join(new V[] {}, new Results((V)(v_71 - R.Get(v_68, (V)"y"))))).First), () => (V)0.0d)));
V v_125 = values_124[0];
V v_126 = values_124[1];
if (R.Truth((V)(v_65 == (V)"lance"))) {
if (R.Truth(R.Or((V)(v_125 != (V)0.0d), () => (V)(v_126 != (V)0.0d)))) {
R.Call(v_108, R.Join(new V[] {v_125, v_126}, new Results((V)2.0d)));
}
} else if (R.Truth((V)(v_65 == (V)"rook"))) {
if (R.Truth(R.Call(R.Get(v_8, (V)"axis"), R.Join(new V[] {v_66}, new Results((V)"cardinal"))).First)) {
R.Call(v_108, R.Join(new V[] {(-v_126), v_125}, new Results((V)2.0d)));

R.Call(v_108, R.Join(new V[] {v_126, (-v_125)}, new Results((V)2.0d)));
}
} else if (R.Truth((V)(v_65 == (V)"bishop"))) {
if (R.Truth(R.Call(R.Get(v_8, (V)"axis"), R.Join(new V[] {v_66}, new Results((V)"diagonal"))).First)) {
foreach (var row_127 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_15)))))) {
V v_128 = row_127[0];
V v_129 = row_127[1];
R.Call(v_108, R.Join(new V[] {R.Get(v_129, (V)1.0d), R.Get(v_129, (V)2.0d)}, new Results((V)2.0d)));
}
}
} else if (R.Truth((V)(v_65 == (V)"knight"))) {
foreach (var row_130 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)2.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)2.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)4, R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (V)1.0d))), new Entry((V)5, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)2.0d))), new Entry((V)6, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)2.0d))), new Entry((V)7, R.Table(new Entry((V)1, (-(V)2.0d)), new Entry((V)2, (V)1.0d))), new Entry((V)8, R.Table(new Entry((V)1, (-(V)2.0d)), new Entry((V)2, (-(V)1.0d))))))))))) {
V v_131 = row_130[0];
V v_132 = row_130[1];
V v_133 = V.Nil;
v_133 = new V((RuleFunction)(args_134 => {
V v_135 = args_134[0];
var values_136 = R.Join(new V[] {v_70}, new Results(v_71));
V v_137 = values_136[0];
V v_138 = values_136[1];
{ double end_141 = R.Num((V)2.0d), step_142 = R.Num((V)1.0d);
for (double i_140 = R.Num((V)1.0d); step_142 > 0 ? i_140 <= end_141 : i_140 >= end_141; i_140 += step_142) {
V v_139 = i_140;
var values_143 = R.Join(new V[] {}, new Results((V)((V)(v_139 == (V)1.0d) == v_135)));
V v_144 = values_143[0];
var values_145 = R.Join(new V[] {}, new Results(R.Or(R.And(v_144, () => R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(R.Get(v_132, (V)1.0d)))).First), () => R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(R.Get(v_132, (V)2.0d)))).First)));
V v_146 = values_145[0];
{ double end_149 = R.Num(v_146), step_150 = R.Num((V)1.0d);
for (double i_148 = R.Num((V)1.0d); step_150 > 0 ? i_148 <= end_149 : i_148 >= end_149; i_148 += step_150) {
V v_147 = i_148;
var values_151 = R.Join(new V[] {}, new Results((V)(v_137 + R.Or(R.And(v_144, () => R.Call(v_16, R.Join(new V[] {}, new Results(R.Get(v_132, (V)1.0d)))).First), () => (V)0.0d))));
v_137 = values_151[0];
var values_152 = R.Join(new V[] {}, new Results((V)(v_138 + R.Or(R.And(v_144, () => (V)0.0d), () => R.Call(v_16, R.Join(new V[] {}, new Results(R.Get(v_132, (V)2.0d)))).First))));
v_138 = values_152[0];
if (R.Truth((V)(!R.Truth(R.Call(v_80, R.Join(new V[] {v_137}, new Results(v_138))).First)))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
}
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}));
if (R.Truth(R.Or(R.Call(v_133, R.Join(new V[] {}, new Results((V)true))).First, () => R.Call(v_133, R.Join(new V[] {}, new Results((V)false))).First))) {
R.Call(v_84, R.Join(new V[] {(V)(v_70 + R.Get(v_132, (V)1.0d))}, new Results((V)(v_71 + R.Get(v_132, (V)2.0d)))));
}
}
} else if (R.Truth((V)(v_65 == (V)"crown"))) {
{ double end_155 = R.Num((V)2.0d), step_156 = R.Num((V)1.0d);
for (double i_154 = R.Num((-(V)2.0d)); step_156 > 0 ? i_154 <= end_155 : i_154 >= end_155; i_154 += step_156) {
V v_153 = i_154;
{ double end_159 = R.Num((V)2.0d), step_160 = R.Num((V)1.0d);
for (double i_158 = R.Num((-(V)2.0d)); step_160 > 0 ? i_158 <= end_159 : i_158 >= end_159; i_158 += step_160) {
V v_157 = i_158;
if (R.Truth((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_157))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_153))))).First == (V)2.0d))) {
if (R.Truth(R.Call(v_102, R.Join(new V[] {v_70, v_71, (V)(v_70 + v_157)}, new Results((V)(v_71 + v_153)))).First)) {
R.Call(v_84, R.Join(new V[] {(V)(v_70 + v_157)}, new Results((V)(v_71 + v_153))));
}
}
}
}
}
}
} else if (R.Truth((V)(v_65 == (V)"crosswind"))) {
foreach (var row_161 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_13)))))) {
V v_162 = row_161[0];
V v_163 = row_161[1];
R.Call(v_108, R.Join(new V[] {R.Get(v_163, (V)1.0d), R.Get(v_163, (V)2.0d)}, new Results((V)2.0d)));
}
} else if (R.Truth((V)(v_65 == (V)"fan"))) {
if (R.Truth(R.Or((V)(v_125 != (V)0.0d), () => (V)(v_126 != (V)0.0d)))) {
{ double end_166 = R.Num((V)1.0d), step_167 = R.Num((V)1.0d);
for (double i_165 = R.Num((-(V)1.0d)); step_167 > 0 ? i_165 <= end_166 : i_165 >= end_166; i_165 += step_167) {
V v_164 = i_165;
var values_168 = R.Join(new V[] {(V)((V)(v_70 + v_125) - (V)(v_126 * v_164))}, new Results((V)((V)(v_71 + v_126) + (V)(v_125 * v_164))));
V v_169 = values_168[0];
V v_170 = values_168[1];
if (R.Truth(R.Call(v_102, R.Join(new V[] {v_70, v_71, v_169}, new Results(v_170))).First)) {
R.Call(v_84, R.Join(new V[] {v_169}, new Results(v_170)));
}
}
}
}
} else {
R.Call(v_84, R.Join(new V[] {v_70}, new Results(v_71)));
foreach (var row_171 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_13)))))) {
V v_172 = row_171[0];
V v_173 = row_171[1];
R.Call(v_108, R.Join(new V[] {R.Get(v_173, (V)1.0d), R.Get(v_173, (V)2.0d)}, new Results((V)1.0d)));
}
}
return R.Join(new V[] {}, new Results(v_78));
return Results.Empty;
})));
R.Set(v_8, (V)"weapon_cells", new V((RuleFunction)(args_174 => {
V v_175 = args_174[0];
V v_176 = args_174[1];
V v_177 = args_174[2];
V v_178 = args_174[3];
V v_179 = args_174[4];
V v_180 = args_174[5];
V v_181 = args_174[6];
V v_182 = args_174[7];
V v_183 = args_174[8];
V v_184 = args_174[9];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(R.Get(v_8, (V)"weapon_forms"), v_176)}, new Results((V)"unknown weapon form")));
var values_185 = R.Join(new V[] {}, new Results(R.Get(v_177, (V)"position")));
V v_186 = values_185[0];
if (R.Truth(R.And((V)(v_183 == (V)false), () => R.Or(R.Or((V)(v_176 == (V)"pommel_return"), () => (V)(v_176 == (V)"needle_flight")), () => (V)(v_176 == (V)"split_thrust"))))) {
return R.Join(new V[] {v_178}, new Results(v_182));
}
var values_187 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_188 = values_187[0];
V v_189 = values_187[1];
var values_190 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_175))));
V v_191 = values_190[0];
V v_192 = V.Nil;
v_192 = new V((RuleFunction)(args_193 => {
V v_194 = args_193[0];
V v_195 = args_193[1];
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"projectile_passable"), R.Join(new V[] {v_191, v_194, v_195}, new Results(v_175))));
return Results.Empty;
}));
V v_196 = V.Nil;
v_196 = new V((RuleFunction)(args_197 => {
V v_198 = args_197[0];
V v_199 = args_197[1];
V v_200 = args_197[2];
var values_201 = R.Join(new V[] {}, new Results(R.Concat(v_198, R.Concat((V)":", v_199))));
V v_202 = values_201[0];
if (R.Truth(R.And(R.And((V)(!R.Truth(R.Get(v_189, v_202))), () => R.Or((V)(v_198 != R.Get(v_186, (V)"x")), () => (V)(v_199 != R.Get(v_186, (V)"y")))), () => R.Or(v_200, () => R.Call(v_192, R.Join(new V[] {v_198}, new Results(v_199))).First)))) {
var values_203 = R.Join(new V[] {}, new Results((V)true));
var obj_204 = v_189;
V key_205 = v_202;
R.Set(obj_204, key_205, values_203[0]);

var values_206 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_186, (V)"floor_id")), new Entry((V)"x", v_198), new Entry((V)"y", v_199))));
var obj_207 = v_188;
V key_208 = (V)((V)R.Length(v_188) + (V)1.0d);
R.Set(obj_207, key_208, values_206[0]);
}
return Results.Empty;
}));
V v_209 = V.Nil;
v_209 = new V((RuleFunction)(args_210 => {

foreach (var row_211 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_178)))))) {
V v_212 = row_211[0];
V v_213 = row_211[1];
R.Call(v_196, R.Join(new V[] {R.Get(v_213, (V)"x"), R.Get(v_213, (V)"y")}, new Results((V)true)));
}
return Results.Empty;
}));
V v_214 = V.Nil;
v_214 = new V((RuleFunction)(args_215 => {
V v_216 = args_215[0];
V v_217 = args_215[1];
V v_218 = args_215[2];
V v_219 = args_215[3];
return R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"clear_segment"), R.Join(new V[] {v_175, R.Get(v_186, (V)"floor_id"), v_216, v_217, v_218}, new Results(v_219))));
return Results.Empty;
}));
if (R.Truth(R.Or((V)(v_176 == (V)"pommel_return"), () => (V)(v_176 == (V)"backswing")))) {
R.Call(v_209, Results.Empty);
var values_220 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_176 == (V)"pommel_return"), () => R.Table(new Entry((V)1, R.Table(new Entry((V)"x", (V)(R.Get(v_186, (V)"x") - v_180)), new Entry((V)"y", (V)(R.Get(v_186, (V)"y") - v_181)))))), () => R.Call(R.Get(v_4, (V)"cells"), R.Join(new V[] {(V)"sweeping_slash", R.Get(v_186, (V)"x"), R.Get(v_186, (V)"y"), (-v_180)}, new Results((-v_181)))).First)));
V v_221 = values_220[0];
foreach (var row_222 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_221)))))) {
V v_223 = row_222[0];
V v_224 = row_222[1];
if (R.Truth(R.Call(v_214, R.Join(new V[] {R.Get(v_186, (V)"x"), R.Get(v_186, (V)"y"), R.Get(v_224, (V)"x")}, new Results(R.Get(v_224, (V)"y")))).First)) {
R.Call(v_196, R.Join(new V[] {R.Get(v_224, (V)"x")}, new Results(R.Get(v_224, (V)"y"))));
}
}
} else if (R.Truth((V)(v_176 == (V)"needle_flight"))) {
var values_225 = Results.Empty;
V v_226 = values_225[0];
foreach (var row_227 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_179)))))) {
V v_228 = row_227[0];
V v_229 = row_227[1];
if (R.Truth((V)(!R.Truth(R.Call(v_192, R.Join(new V[] {R.Get(v_229, (V)"x")}, new Results(R.Get(v_229, (V)"y")))).First)))) {
var values_230 = R.Join(new V[] {}, new Results(v_229));
v_226 = values_230[0];

break;
}
R.Call(v_196, R.Join(new V[] {R.Get(v_229, (V)"x")}, new Results(R.Get(v_229, (V)"y"))));
}
foreach (var row_231 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_178)))))) {
V v_232 = row_231[0];
V v_233 = row_231[1];
if (R.Truth(R.Call(v_192, R.Join(new V[] {R.Get(v_233, (V)"x")}, new Results(R.Get(v_233, (V)"y")))).First)) {
R.Call(v_196, R.Join(new V[] {R.Get(v_233, (V)"x")}, new Results(R.Get(v_233, (V)"y"))));
}
}
if (R.Truth(v_226)) {
R.Call(v_196, R.Join(new V[] {R.Get(v_226, (V)"x"), R.Get(v_226, (V)"y")}, new Results((V)true)));
}
var values_234 = R.Join(new V[] {}, new Results((V)"piercing_line"));
v_182 = values_234[0];
} else if (R.Truth((V)(v_176 == (V)"hollow_sigil"))) {
var values_235 = R.Join(new V[] {(V)(R.Get(v_186, (V)"x") + (V)(v_180 * (V)2.0d))}, new Results((V)(R.Get(v_186, (V)"y") + (V)(v_181 * (V)2.0d))));
V v_236 = values_235[0];
V v_237 = values_235[1];
var values_238 = R.Join(new V[] {}, new Results((V)false));
V v_239 = values_238[0];
foreach (var row_240 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_179)))))) {
V v_241 = row_240[0];
V v_242 = row_240[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_242, (V)"x") == v_236), () => (V)(R.Get(v_242, (V)"y") == v_237)), () => R.Call(v_192, R.Join(new V[] {v_236}, new Results(v_237))).First))) {
var values_243 = R.Join(new V[] {}, new Results((V)true));
v_239 = values_243[0];
}
}
if (R.Truth((V)(!R.Truth(v_239)))) {
return R.Join(new V[] {v_178}, new Results(v_182));
}
foreach (var row_244 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_178)))))) {
V v_245 = row_244[0];
V v_246 = row_244[1];
if (R.Truth(R.Or((V)(R.Get(v_246, (V)"x") != v_236), () => (V)(R.Get(v_246, (V)"y") != v_237)))) {
R.Call(v_196, R.Join(new V[] {R.Get(v_246, (V)"x"), R.Get(v_246, (V)"y")}, new Results((V)true)));
}
}
} else if (R.Truth((V)(v_176 == (V)"distant_bastion"))) {
{ double end_249 = R.Num((V)2.0d), step_250 = R.Num((V)1.0d);
for (double i_248 = R.Num((-(V)2.0d)); step_250 > 0 ? i_248 <= end_249 : i_248 >= end_249; i_248 += step_250) {
V v_247 = i_248;
{ double end_253 = R.Num((V)2.0d), step_254 = R.Num((V)1.0d);
for (double i_252 = R.Num((-(V)2.0d)); step_254 > 0 ? i_252 <= end_253 : i_252 >= end_253; i_252 += step_254) {
V v_251 = i_252;
if (R.Truth(R.And((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_251))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_247))).First) == (V)2.0d), () => R.Call(v_214, R.Join(new V[] {R.Get(v_186, (V)"x"), R.Get(v_186, (V)"y"), (V)(R.Get(v_186, (V)"x") + v_251)}, new Results((V)(R.Get(v_186, (V)"y") + v_247)))).First))) {
R.Call(v_196, R.Join(new V[] {(V)(R.Get(v_186, (V)"x") + v_251)}, new Results((V)(R.Get(v_186, (V)"y") + v_247))));
}
}
}
}
}
var values_255 = R.Join(new V[] {}, new Results((V)"area"));
v_182 = values_255[0];
} else if (R.Truth((V)(v_176 == (V)"volatile_vial"))) {
var values_256 = Results.Empty;
V v_257 = values_256[0];
V v_258 = values_256[1];
foreach (var row_259 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_179)))))) {
V v_260 = row_259[0];
V v_261 = row_259[1];
if (R.Truth((V)(!R.Truth(R.Call(v_192, R.Join(new V[] {R.Get(v_261, (V)"x")}, new Results(R.Get(v_261, (V)"y")))).First)))) {
break;
}
foreach (var row_262 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_175, (V)"entities"))))))) {
V v_263 = row_262[0];
V v_264 = row_262[1];
var values_265 = R.Join(new V[] {}, new Results(R.Get(v_264, (V)"position")));
V v_266 = values_265[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_264, (V)"id") != R.Get(v_177, (V)"id")), () => R.Get(v_264, (V)"hp")), () => (V)(R.Get(v_264, (V)"hp") > (V)0.0d)), () => v_266), () => (V)(R.Get(v_266, (V)"floor_id") == R.Get(v_186, (V)"floor_id"))), () => (V)(R.Get(v_266, (V)"x") == R.Get(v_261, (V)"x"))), () => (V)(R.Get(v_266, (V)"y") == R.Get(v_261, (V)"y"))))) {
if (R.Truth(R.And(v_184, () => (V)(!R.Truth(R.Call(R.Get(v_6, (V)"is_visible"), R.Join(new V[] {v_175, R.Get(v_186, (V)"floor_id"), R.Get(v_266, (V)"x")}, new Results(R.Get(v_266, (V)"y")))).First))))) {
return R.Join(new V[] {v_178, v_182}, new Results(R.Get(R.Get(R.Get(v_8, (V)"weapon_forms"), v_176), (V)"hint")));
}
var values_267 = R.Join(new V[] {v_261}, new Results(v_260));
v_257 = values_267[0];
v_258 = values_267[1];

break;
}
}
if (R.Truth(v_257)) {
break;
}
}
if (R.Truth((V)(!R.Truth(v_257)))) {
return R.Join(new V[] {v_178, v_182}, new Results(R.Get(R.Get(R.Get(v_8, (V)"weapon_forms"), v_176), (V)"hint")));
}
{ double end_270 = R.Num(v_258), step_271 = R.Num((V)1.0d);
for (double i_269 = R.Num((V)1.0d); step_271 > 0 ? i_269 <= end_270 : i_269 >= end_270; i_269 += step_271) {
V v_268 = i_269;
R.Call(v_196, R.Join(new V[] {R.Get(R.Get(v_179, v_268), (V)"x")}, new Results(R.Get(R.Get(v_179, v_268), (V)"y"))));
}
}
foreach (var row_272 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_13)))))) {
V v_273 = row_272[0];
V v_274 = row_272[1];
var values_275 = R.Join(new V[] {(V)(R.Get(v_257, (V)"x") + R.Get(v_274, (V)1.0d))}, new Results((V)(R.Get(v_257, (V)"y") + R.Get(v_274, (V)2.0d))));
V v_276 = values_275[0];
V v_277 = values_275[1];
if (R.Truth(R.Call(v_214, R.Join(new V[] {R.Get(v_257, (V)"x"), R.Get(v_257, (V)"y"), v_276}, new Results(v_277))).First)) {
R.Call(v_196, R.Join(new V[] {v_276}, new Results(v_277)));
}
}
var values_278 = R.Join(new V[] {}, new Results((V)"area"));
v_182 = values_278[0];
} else if (R.Truth((V)(v_176 == (V)"split_thrust"))) {
var values_279 = R.Join(new V[] {}, new Results(R.Get(v_179, (V)1.0d)));
V v_280 = values_279[0];
if (R.Truth((V)(!R.Truth(v_280)))) {
return R.Join(new V[] {v_178}, new Results(v_182));
}
R.Call(v_196, R.Join(new V[] {R.Get(v_280, (V)"x"), R.Get(v_280, (V)"y")}, new Results((V)true)));
if (R.Truth(R.Call(v_192, R.Join(new V[] {R.Get(v_280, (V)"x")}, new Results(R.Get(v_280, (V)"y")))).First)) {
foreach (var row_281 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d)))))))) {
V v_282 = row_281[0];
V v_283 = row_281[1];
var values_284 = R.Join(new V[] {(V)((V)(R.Get(v_186, (V)"x") + (V)(v_180 * (V)2.0d)) - (V)(v_181 * v_283))}, new Results((V)((V)(R.Get(v_186, (V)"y") + (V)(v_181 * (V)2.0d)) + (V)(v_180 * v_283))));
V v_285 = values_284[0];
V v_286 = values_284[1];
if (R.Truth(R.Call(v_214, R.Join(new V[] {R.Get(v_280, (V)"x"), R.Get(v_280, (V)"y"), v_285}, new Results(v_286))).First)) {
R.Call(v_196, R.Join(new V[] {v_285}, new Results(v_286)));
}
}
}
var values_287 = R.Join(new V[] {}, new Results((V)"piercing_line"));
v_182 = values_287[0];
}
return R.Join(new V[] {v_188, v_182}, new Results(R.Get(R.Get(R.Get(v_8, (V)"weapon_forms"), v_176), (V)"hint")));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_8));
return Results.Empty;
} }
