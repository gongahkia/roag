// Static C# port of tests/idea_pattern_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: fd223371ede7a34a309582def1f6114bf9bedfb784e7aa8f3320febd4a01004f
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_idea_pattern_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_content"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.boon_patterns"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.persistence"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_15 = values_14[0];
V v_16 = V.Nil;
v_16 = new V((RuleFunction)(args_17 => {
V v_18 = args_17[0];
V v_19 = args_17[1];
V v_20 = args_17[2];
V v_21 = args_17[3];
R.Call(R.Get(v_5, (V)"set_boon_stacks"), R.Join(new V[] {v_18, R.Get(v_18, (V)"player_id"), R.Concat((V)"core:boon/", v_19), R.Or(v_20, () => (V)"rare")}, new Results(R.Or(v_21, () => (V)1.0d))));
return Results.Empty;
}));
V v_22 = V.Nil;
v_22 = new V((RuleFunction)(args_23 => {
V v_24 = args_23[0];
V v_25 = args_23[1];
V v_26 = args_23[2];
var values_27 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"new"), R.Join(new V[] {(V)37.0d, V.Nil}, new Results(R.Table(new Entry((V)"class_id", v_25))))));
V v_28 = values_27[0];

var values_29 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"player"), R.Join(new V[] {}, new Results(v_28))));
V v_30 = values_29[0];
var values_31 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"map_for"), R.Join(new V[] {}, new Results(v_28))));
V v_32 = values_31[0];
if (R.Truth((V)(!R.Truth(v_26)))) {
var values_33 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, new Results(v_32))))));
var obj_34 = v_28;
V key_35 = (V)"generated_map";
R.Set(obj_34, key_35, values_33[0]);

var values_36 = R.Join(new V[] {}, new Results(R.Get(v_28, (V)"generated_map")));
v_32 = values_36[0];
{ double end_39 = R.Num(R.Get(v_32, (V)"height")), step_40 = R.Num((V)1.0d);
for (double i_38 = R.Num((V)1.0d); step_40 > 0 ? i_38 <= end_39 : i_38 >= end_39; i_38 += step_40) {
V v_37 = i_38;
var values_41 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Or((V)(v_37 == (V)1.0d), () => (V)(v_37 == R.Get(v_32, (V)"height"))), () => R.Call(R.Get(rt.Global("string"), (V)"rep"), R.Join(new V[] {(V)"#"}, new Results(R.Get(v_32, (V)"width")))).First), () => R.Concat((V)"#", R.Concat(R.Call(R.Get(rt.Global("string"), (V)"rep"), R.Join(new V[] {(V)"."}, new Results((V)(R.Get(v_32, (V)"width") - (V)2.0d)))).First, (V)"#")))));
var obj_42 = R.Get(v_32, (V)"rows");
V key_43 = v_37;
R.Set(obj_42, key_43, values_41[0]);
}
}
}
var values_44 = R.Join(new V[] {(V)7.0d}, new Results((V)6.0d));
var obj_45 = R.Get(v_30, (V)"position");
V key_46 = (V)"x";
var obj_47 = R.Get(v_30, (V)"position");
V key_48 = (V)"y";
R.Set(obj_45, key_46, values_44[0]);
R.Set(obj_47, key_48, values_44[1]);
if (R.Truth(v_24)) {
R.Call(v_16, R.Join(new V[] {v_28}, new Results(v_24)));
}
R.Call(R.Get(v_15, (V)"recompute"), R.Join(new V[] {}, new Results(v_28)));

return R.Join(new V[] {v_28, v_30}, new Results(v_32));
return Results.Empty;
}));
V v_49 = V.Nil;
v_49 = new V((RuleFunction)(args_50 => {
V v_51 = args_50[0];
V v_52 = args_50[1];
V v_53 = args_50[2];
var values_54 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(R.Get(R.Get(v_51, (V)"rows"), v_53), "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(v_52 - (V)1.0d)))).First, R.Concat((V)"#", R.Invoke(R.Get(R.Get(v_51, (V)"rows"), v_53), "sub", R.Join(new V[] {}, new Results((V)(v_52 + (V)1.0d)))).First))));
var obj_55 = R.Get(v_51, (V)"rows");
V key_56 = v_53;
R.Set(obj_55, key_56, values_54[0]);
return Results.Empty;
}));
V v_57 = V.Nil;
v_57 = new V((RuleFunction)(args_58 => {
V v_59 = args_58[0];
V v_60 = args_58[1];
V v_61 = args_58[2];
V v_62 = args_58[3];
var values_63 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_enemy"), R.Join(new V[] {v_59, (V)"core:enemy/mossbound_guard", v_60, v_61}, new Results((V)100000.0d))));
V v_64 = values_63[0];
var values_65 = R.Join(new V[] {}, new Results(R.Or(v_62, () => (V)100.0d)));
var obj_66 = v_64;
V key_67 = (V)"hp";
R.Set(obj_66, key_67, values_65[0]);

var values_68 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_69 = v_64;
V key_70 = (V)"max_hp";
R.Set(obj_69, key_70, values_68[0]);

return R.Join(new V[] {}, new Results(v_64));
return Results.Empty;
}));
V v_71 = V.Nil;
v_71 = new V((RuleFunction)(args_72 => {
V v_73 = args_72[0];
V v_74 = args_72[1];
V v_75 = args_72[2];
V v_76 = args_72[3];
var values_77 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_73}, new Results(R.Table(new Entry((V)"kind", R.Or(v_74, () => (V)"basic_attack")), new Entry((V)"dx", R.Or(v_75, () => (V)1.0d)), new Entry((V)"dy", R.Or(v_76, () => (V)0.0d)))))));
V v_78 = values_77[0];
V v_79 = values_77[1];
V v_80 = values_77[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_78}, new Results(v_79)));

return R.Join(new V[] {}, new Results(v_80));
return Results.Empty;
}));
V v_81 = V.Nil;
v_81 = new V((RuleFunction)(args_82 => {
V v_83 = args_82[0];
V v_84 = args_82[1];
V v_85 = args_82[2];
V v_86 = args_82[3];
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", v_83), new Entry((V)"y", v_84), new Entry((V)"from_x", v_83), new Entry((V)"from_y", v_84), new Entry((V)"source_position", R.Table(new Entry((V)"floor_id", (V)"core:floor/test"), new Entry((V)"x", R.Or(v_85, () => (V)7.0d)), new Entry((V)"y", R.Or(v_86, () => (V)6.0d)))))));
return Results.Empty;
}));
V v_87 = V.Nil;
v_87 = new V((RuleFunction)(args_88 => {
V v_89 = args_88[0];
V v_90 = args_88[1];
V v_91 = args_88[2];
foreach (var row_92 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(v_89, () => R.Table()))))))) {
V v_93 = row_92[0];
V v_94 = row_92[1];
if (R.Truth(R.And((V)(R.Get(v_94, (V)"x") == v_90), () => (V)(R.Get(v_94, (V)"y") == v_91)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
V v_95 = V.Nil;
v_95 = new V((RuleFunction)(args_96 => {
V v_97 = args_96[0];
V v_98 = args_96[1];
var values_99 = R.Join(new V[] {}, new Results(R.Table()));
V v_100 = values_99[0];
foreach (var row_101 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_97)))))) {
V v_102 = row_101[0];
V v_103 = row_101[1];
if (R.Truth(R.And((V)(R.Get(v_103, (V)"kind") == (V)"EffectApplied"), () => (V)(R.Get(v_103, (V)"boon_id") == R.Concat((V)"core:boon/", v_98))))) {
var values_104 = R.Join(new V[] {}, new Results(v_103));
var obj_105 = v_100;
V key_106 = (V)((V)R.Length(v_100) + (V)1.0d);
R.Set(obj_105, key_106, values_104[0]);
}
}
return R.Join(new V[] {}, new Results(v_100));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"positional boons declare distinct native art, conditional previews and validated shapes"}, new Results(new V((RuleFunction)(args_107 => {

var values_108 = R.Join(new V[] {(V)0.0d}, new Results(R.Table()));
V v_109 = values_108[0];
V v_110 = values_108[1];
foreach (var row_111 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_7, (V)"order"))))))) {
V v_112 = row_111[0];
V v_113 = row_111[1];
var values_114 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"get"), R.Join(new V[] {}, new Results(v_113))));
V v_115 = values_114[0];
if (R.Truth(R.Get(v_115, (V)"pattern_preview"))) {
var values_116 = R.Join(new V[] {}, new Results((V)(v_109 + (V)1.0d)));
v_109 = values_116[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_110, R.Get(R.Get(v_115, (V)"art"), (V)"index")))))));

var values_117 = R.Join(new V[] {}, new Results((V)true));
var obj_118 = v_110;
V key_119 = R.Get(R.Get(v_115, (V)"art"), (V)"index");
R.Set(obj_118, key_119, values_117[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_9, (V)"names"), R.Get(R.Get(R.Get(v_115, (V)"effects"), (V)1.0d), (V)"pattern")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.kenney_symbols"))).First, (V)"boons"), v_113), (V)1.0d) == R.Get(R.Get(v_115, (V)"art"), (V)"index")))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_109 == (V)9.0d), () => R.Call(R.Get(v_7, (V)"validate"), Results.Empty).First))));
var values_120 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"get"), R.Join(new V[] {}, new Results((V)"core:boon/linebreaker_pawn"))))))));
V v_121 = values_120[0];
var values_122 = R.Join(new V[] {}, new Results((V)"whole_floor"));
var obj_123 = R.Get(R.Get(v_121, (V)"effects"), (V)1.0d);
V key_124 = (V)"pattern";
R.Set(obj_123, key_124, values_122[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_7, (V)"validate"), R.Table(new Entry(R.Get(v_121, (V)"id"), v_121))}, new Results(R.Table(new Entry((V)1, R.Get(v_121, (V)"id")))))).First)))));
var values_125 = R.Join(new V[] {}, new Results((V)"lance"));
var obj_126 = R.Get(R.Get(v_121, (V)"effects"), (V)1.0d);
V key_127 = (V)"pattern";
R.Set(obj_126, key_127, values_125[0]);

var values_128 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"op", (V)"pattern_axis"), new Entry((V)"value", (V)"hexagonal"))));
var obj_129 = v_121;
V key_130 = (V)"predicate";
R.Set(obj_129, key_130, values_128[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_7, (V)"validate"), R.Table(new Entry(R.Get(v_121, (V)"id"), v_121))}, new Results(R.Table(new Entry((V)1, R.Get(v_121, (V)"id")))))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"lance, rook, bishop, knight and hollow ring have exact distinct bounded geometry"}, new Results(new V((RuleFunction)(args_131 => {

var values_132 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_133 = values_132[0];

var values_134 = R.Join(new V[] {}, R.Call(v_81, R.Join(new V[] {(V)8.0d}, new Results((V)6.0d))));
V v_135 = values_134[0];
var values_136 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_133, (V)"lance"}, new Results(v_135))));
V v_137 = values_136[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_137) == (V)2.0d), () => R.Call(v_87, R.Join(new V[] {v_137, (V)9.0d}, new Results((V)6.0d))).First), () => R.Call(v_87, R.Join(new V[] {v_137, (V)10.0d}, new Results((V)6.0d))).First))));
var values_138 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_133, (V)"rook"}, new Results(v_135))));
V v_139 = values_138[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_139) == (V)4.0d), () => R.Call(v_87, R.Join(new V[] {v_139, (V)8.0d}, new Results((V)4.0d))).First), () => R.Call(v_87, R.Join(new V[] {v_139, (V)8.0d}, new Results((V)8.0d))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_133, (V)"rook"}, R.Call(v_81, R.Join(new V[] {(V)8.0d}, new Results((V)7.0d))))).First) == (V)0.0d))));
var values_140 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_133, (V)"bishop"}, R.Call(v_81, R.Join(new V[] {(V)8.0d}, new Results((V)7.0d))))));
V v_141 = values_140[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_141) == (V)8.0d), () => R.Call(v_87, R.Join(new V[] {v_141, (V)10.0d}, new Results((V)9.0d))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_133, (V)"bishop"}, new Results(v_135))).First) == (V)0.0d))));
var values_142 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_133, (V)"knight"}, new Results(v_135))));
V v_143 = values_142[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_143) == (V)8.0d), () => R.Call(v_87, R.Join(new V[] {v_143, (V)10.0d}, new Results((V)7.0d))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(v_87, R.Join(new V[] {v_143, (V)9.0d}, new Results((V)6.0d))).First)), () => (V)(!R.Truth(R.Call(v_87, R.Join(new V[] {v_143, (V)8.0d}, new Results((V)6.0d))).First))))));
var values_144 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_133, (V)"crown"}, new Results(v_135))));
V v_145 = values_144[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_145) == (V)16.0d), () => R.Call(v_87, R.Join(new V[] {v_145, (V)10.0d}, new Results((V)6.0d))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(v_87, R.Join(new V[] {v_145, (V)9.0d}, new Results((V)6.0d))).First)), () => (V)(!R.Truth(R.Call(v_87, R.Join(new V[] {v_145, (V)8.0d}, new Results((V)6.0d))).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_133, (V)"fan"}, new Results(v_135))).First) == (V)3.0d), () => (V)((V)R.Length(R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_133, (V)"crosswind"}, new Results(v_135))).First) == (V)8.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_133, (V)"backtrail"}, new Results(v_135))).First) == (V)5.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"walls, closed doors and diagonal stone corners clip the shared pattern geometry"}, new Results(new V((RuleFunction)(args_146 => {

var values_147 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_148 = values_147[0];
V v_149 = values_147[1];
V v_150 = values_147[2];

var values_151 = R.Join(new V[] {}, R.Call(v_81, R.Join(new V[] {(V)8.0d}, new Results((V)6.0d))));
V v_152 = values_151[0];
R.Call(v_49, R.Join(new V[] {v_150, (V)9.0d}, new Results((V)6.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_148, (V)"lance"}, new Results(v_152))).First) == (V)0.0d))));
var values_153 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_148, (V)"crown"}, new Results(v_152))));
V v_154 = values_153[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(v_87, R.Join(new V[] {v_154, (V)10.0d}, new Results((V)6.0d))).First)), () => R.Call(v_87, R.Join(new V[] {v_154, (V)6.0d}, new Results((V)6.0d))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(v_87, R.Join(new V[] {R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_148, (V)"knight"}, new Results(v_152))).First, (V)10.0d}, new Results((V)6.0d))).First)))));
var values_155 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"spawn_door"), R.Join(new V[] {v_148, (V)8.0d}, new Results((V)7.0d))));
V v_156 = values_155[0];
var values_157 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_148, (V)"rook"}, new Results(v_152))));
V v_158 = values_157[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Call(v_87, R.Join(new V[] {v_158, (V)8.0d}, new Results((V)8.0d))).First)), () => R.Call(v_87, R.Join(new V[] {v_158, (V)8.0d}, new Results((V)4.0d))).First))));
var values_159 = R.Join(new V[] {}, new Results((V)true));
var obj_160 = v_156;
V key_161 = (V)"open";
R.Set(obj_160, key_161, values_159[0]);

var values_162 = R.Join(new V[] {}, new Results((V)false));
var obj_163 = v_156;
V key_164 = (V)"blocks_projectiles";
R.Set(obj_163, key_164, values_162[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_87, R.Join(new V[] {R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_148, (V)"rook"}, new Results(v_152))).First, (V)8.0d}, new Results((V)8.0d)))));
R.Call(v_49, R.Join(new V[] {v_150, (V)9.0d}, new Results((V)7.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(v_87, R.Join(new V[] {R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_148, (V)"bishop"}, R.Call(v_81, R.Join(new V[] {(V)8.0d}, new Results((V)7.0d))))).First, (V)10.0d}, new Results((V)9.0d))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(v_87, R.Join(new V[] {R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_148, (V)"fan"}, new Results(v_152))).First, (V)9.0d}, new Results((V)7.0d))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"knight endpoints require at least one clear orthogonal bending route"}, new Results(new V((RuleFunction)(args_165 => {

var values_166 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_167 = values_166[0];
V v_168 = values_166[1];
V v_169 = values_166[2];

var values_170 = R.Join(new V[] {}, R.Call(v_81, R.Join(new V[] {(V)8.0d}, new Results((V)6.0d))));
V v_171 = values_170[0];
R.Call(v_49, R.Join(new V[] {v_169, (V)9.0d}, new Results((V)6.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(v_87, R.Join(new V[] {R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_167, (V)"knight"}, new Results(v_171))).First, (V)10.0d}, new Results((V)7.0d))).First}, new Results((V)"second leg order must remain usable")));
R.Call(v_49, R.Join(new V[] {v_169, (V)8.0d}, new Results((V)7.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Call(v_87, R.Join(new V[] {R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_167, (V)"knight"}, new Results(v_171))).First, (V)10.0d}, new Results((V)7.0d))).First))}, new Results((V)"both blocked routes cannot jump walls")));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"pattern clipping is rotationally symmetric, deterministic and independent of gameplay RNG"}, new Results(new V((RuleFunction)(args_172 => {

foreach (var row_173 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"lance"), new Entry((V)2, (V)"rook"), new Entry((V)3, (V)"bishop"), new Entry((V)4, (V)"knight"), new Entry((V)5, (V)"crown"), new Entry((V)6, (V)"crosswind"), new Entry((V)7, (V)"fan"), new Entry((V)8, (V)"backtrail")))))))) {
V v_174 = row_173[0];
V v_175 = row_173[1];
var values_176 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_177 = values_176[0];
V v_178 = values_176[1];
V v_179 = values_176[2];

R.Call(v_49, R.Join(new V[] {v_179, (V)9.0d}, new Results((V)7.0d)));

var values_180 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_177))));
V v_181 = values_180[0];
var values_182 = R.Join(new V[] {}, R.Call(v_81, R.Join(new V[] {(V)8.0d}, new Results((V)7.0d))));
V v_183 = values_182[0];
var values_184 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_177, v_175}, new Results(v_183))));
V v_185 = values_184[0];
{ double end_188 = R.Num((V)20.0d), step_189 = R.Num((V)1.0d);
for (double i_187 = R.Num((V)1.0d); step_189 > 0 ? i_187 <= end_188 : i_187 >= end_188; i_187 += step_189) {
V v_186 = i_187;
R.Call(v_3, R.Join(new V[] {v_185}, R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_177, v_175}, new Results(v_183)))));
}
}
R.Call(v_3, R.Join(new V[] {v_181}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_177)))));
var values_190 = R.Join(new V[] {}, R.Call(v_22, Results.Empty));
V v_191 = values_190[0];
V v_192 = values_190[1];
V v_193 = values_190[2];

R.Call(v_49, R.Join(new V[] {v_193, (V)7.0d}, new Results((V)5.0d)));
var values_194 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_191, v_175}, R.Call(v_81, R.Join(new V[] {(V)8.0d, (V)6.0d, (V)9.0d}, new Results((V)7.0d))))));
V v_195 = values_194[0];
if (R.Truth((V)(v_175 != (V)"backtrail"))) {
var values_196 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_177, v_175}, R.Call(v_81, R.Join(new V[] {(V)8.0d, (V)6.0d, (V)7.0d}, new Results((V)5.0d))))));
V v_197 = values_196[0];
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_197) == (V)R.Length(v_195))}, new Results(v_175)));
foreach (var row_198 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_197)))))) {
V v_199 = row_198[0];
V v_200 = row_198[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(v_87, R.Join(new V[] {v_195, (V)((V)16.0d - R.Get(v_200, (V)"x"))}, new Results((V)((V)12.0d - R.Get(v_200, (V)"y"))))).First}, new Results(v_175)));
}
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a real lance primary hit damages only the two cells behind the victim after the primary batch"}, new Results(new V((RuleFunction)(args_201 => {

var values_202 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results((V)"linebreaker_pawn"))));
V v_203 = values_202[0];

var values_204 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_203, (V)8.0d}, new Results((V)6.0d))));
V v_205 = values_204[0];
var values_206 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_203, (V)9.0d}, new Results((V)6.0d))));
V v_207 = values_206[0];

var values_208 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_203, (V)10.0d}, new Results((V)6.0d))));
V v_209 = values_208[0];

var values_210 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_203, (V)8.0d}, new Results((V)7.0d))));
V v_211 = values_210[0];
var values_212 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_203, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_213 = values_212[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_213, (V)"boon_cells")) == (V)2.0d), () => (V)(R.Get(v_213, (V)"boon_hint") == (V)"on hit")))));
var values_214 = R.Join(new V[] {}, R.Call(v_71, R.Join(new V[] {}, new Results(v_203))));
V v_215 = values_214[0];

var values_216 = R.Join(new V[] {}, R.Call(v_95, R.Join(new V[] {v_215}, new Results((V)"linebreaker_pawn"))));
V v_217 = values_216[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_217) == (V)1.0d))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_9, (V)"cells"), R.Join(new V[] {v_203, (V)"lance"}, R.Call(v_81, R.Join(new V[] {(V)8.0d}, new Results((V)6.0d))))).First}, new Results(R.Get(R.Get(v_217, (V)1.0d), (V)"target_cells"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_205, (V)"hp") == (V)97.0d), () => (V)(R.Get(v_207, (V)"hp") == (V)98.0d)), () => (V)(R.Get(v_209, (V)"hp") == (V)98.0d)), () => (V)(R.Get(v_211, (V)"hp") == (V)100.0d)))));
var values_218 = Results.Empty;
V v_219 = values_218[0];
V v_220 = values_218[1];
foreach (var row_221 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_215)))))) {
V v_222 = row_221[0];
V v_223 = row_221[1];
if (R.Truth(R.And((V)(R.Get(v_223, (V)"kind") == (V)"DamageBatchResolved"), () => (V)(!R.Truth(v_219))))) {
var values_224 = R.Join(new V[] {}, new Results(v_222));
v_219 = values_224[0];
}
if (R.Truth((V)(R.Get(v_223, (V)"kind") == (V)"BoonActivated"))) {
var values_225 = R.Join(new V[] {}, new Results(v_222));
v_220 = values_225[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_219 < v_220))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"rook and bishop require their actual primary strike axes and do not recurse on their own damage"}, new Results(new V((RuleFunction)(args_226 => {

foreach (var row_227 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)"rooks_wake"), new Entry((V)2, (V)8.0d), new Entry((V)3, (V)6.0d), new Entry((V)4, (V)0.0d), new Entry((V)5, (V)8.0d), new Entry((V)6, (V)5.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)"bishops_wake"), new Entry((V)2, (V)8.0d), new Entry((V)3, (V)7.0d), new Entry((V)4, (V)1.0d), new Entry((V)5, (V)9.0d), new Entry((V)6, (V)8.0d)))))))))) {
V v_228 = row_227[0];
V v_229 = row_227[1];
var values_230 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results(R.Get(v_229, (V)1.0d)))));
V v_231 = values_230[0];

R.Call(v_57, R.Join(new V[] {v_231, R.Get(v_229, (V)2.0d)}, new Results(R.Get(v_229, (V)3.0d))));

var values_232 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_231, R.Get(v_229, (V)5.0d)}, new Results(R.Get(v_229, (V)6.0d)))));
V v_233 = values_232[0];
var values_234 = R.Join(new V[] {}, R.Call(v_71, R.Join(new V[] {v_231, (V)"basic_attack", (V)1.0d}, new Results(R.Get(v_229, (V)4.0d)))));
V v_235 = values_234[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_95, R.Join(new V[] {v_235}, new Results(R.Get(v_229, (V)1.0d)))).First) == (V)1.0d), () => (V)(R.Get(v_233, (V)"hp") == (V)98.0d)))));
var values_236 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results(R.Get(v_229, (V)1.0d)))));
v_231 = values_236[0];

R.Call(v_57, R.Join(new V[] {v_231, (V)8.0d}, new Results(R.Or(R.And((V)(R.Get(v_229, (V)4.0d) == (V)0.0d), () => (V)7.0d), () => (V)6.0d))));
var values_237 = R.Join(new V[] {}, R.Call(v_71, R.Join(new V[] {v_231, (V)"basic_attack", (V)1.0d}, new Results(R.Or(R.And((V)(R.Get(v_229, (V)4.0d) == (V)0.0d), () => (V)1.0d), () => (V)0.0d)))));
v_235 = values_237[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_95, R.Join(new V[] {v_235}, new Results(R.Get(v_229, (V)1.0d)))).First) == (V)0.0d))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"special-only knight and fan leave basics unchanged and use the same cells as visible previews"}, new Results(new V((RuleFunction)(args_238 => {

foreach (var row_239 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"knights_gambit"), new Entry((V)2, (V)"fan_of_blades")))))))) {
V v_240 = row_239[0];
V v_241 = row_239[1];
var values_242 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results(v_241))));
V v_243 = values_242[0];

R.Call(v_57, R.Join(new V[] {v_243, (V)8.0d}, new Results((V)6.0d)));

var values_244 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_243, (V)10.0d}, new Results((V)7.0d))));
V v_245 = values_244[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_243, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))).First, (V)"boon_cells"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_95, R.Join(new V[] {R.Call(v_71, R.Join(new V[] {}, new Results(v_243))).First}, new Results(v_241))).First) == (V)0.0d), () => (V)(R.Get(v_245, (V)"hp") == (V)100.0d)))));
var values_246 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_243, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_247 = values_246[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_247, (V)"boon_hint") == (V)"on hit"), () => (V)((V)R.Length(R.Get(v_247, (V)"boon_cells")) > (V)0.0d)))));
var values_248 = R.Join(new V[] {}, R.Call(v_71, R.Join(new V[] {v_243, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_249 = values_248[0];

var values_250 = R.Join(new V[] {}, R.Call(v_95, R.Join(new V[] {v_249}, new Results(v_241))));
V v_251 = values_250[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_251) == (V)1.0d))));
foreach (var row_252 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_247, (V)"boon_cells"))))))) {
V v_253 = row_252[0];
V v_254 = row_252[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(v_87, R.Join(new V[] {R.Get(R.Get(v_251, (V)1.0d), (V)"target_cells"), R.Get(v_254, (V)"x")}, new Results(R.Get(v_254, (V)"y"))))));
}
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a hollow crown waits for a credited kill and spares the victim inner ring"}, new Results(new V((RuleFunction)(args_255 => {

var values_256 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results((V)"hollow_crown"))));
V v_257 = values_256[0];

var values_258 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_257, (V)8.0d, (V)6.0d}, new Results((V)5.0d))));
V v_259 = values_258[0];
var values_260 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_257, (V)9.0d}, new Results((V)6.0d))));
V v_261 = values_260[0];

var values_262 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_257, (V)10.0d}, new Results((V)6.0d))));
V v_263 = values_262[0];
var values_264 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_257, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_265 = values_264[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_265, (V)"boon_hint") == (V)"on kill"), () => (V)((V)R.Length(R.Get(v_265, (V)"boon_cells")) > (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_95, R.Join(new V[] {R.Call(v_71, R.Join(new V[] {}, new Results(v_257))).First}, new Results((V)"hollow_crown"))).First) == (V)0.0d), () => (V)(R.Get(v_259, (V)"hp") == (V)2.0d)))));
var values_266 = R.Join(new V[] {}, R.Call(v_71, R.Join(new V[] {}, new Results(v_257))));
V v_267 = values_266[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"entity"), R.Join(new V[] {v_257}, new Results(R.Get(v_259, (V)"id")))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Call(v_95, R.Join(new V[] {v_267}, new Results((V)"hollow_crown"))).First) == (V)1.0d), () => (V)(R.Get(v_263, (V)"hp") == (V)98.0d)), () => (V)(R.Get(v_261, (V)"hp") == (V)100.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Crosswind uses paid Wait time and cardinal lanes without self damage, recursion or diagonals"}, new Results(new V((RuleFunction)(args_268 => {

var values_269 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results((V)"crosswind"))));
V v_270 = values_269[0];
V v_271 = values_269[1];

var values_272 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_270, (V)8.0d}, new Results((V)6.0d))));
V v_273 = values_272[0];

var values_274 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_270, (V)9.0d}, new Results((V)6.0d))));
V v_275 = values_274[0];
var values_276 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_270, (V)8.0d}, new Results((V)7.0d))));
V v_277 = values_276[0];
var values_278 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_270))));
V v_279 = values_278[0];

var values_280 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_270}, new Results((V)"wait"))));
V v_281 = values_280[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_281, (V)"boon_hint") == (V)"Wait rays"), () => (V)((V)R.Length(R.Get(v_281, (V)"boon_cells")) > (V)0.0d)))));
R.Call(v_3, R.Join(new V[] {v_279}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_270)))));
var values_282 = R.Join(new V[] {}, R.Call(v_71, R.Join(new V[] {v_270}, new Results((V)"wait"))));
V v_283 = values_282[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_270, (V)"clock") == (V)100.0d), () => (V)(R.Get(v_271, (V)"hp") == R.Get(v_271, (V)"max_hp"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_273, (V)"hp") == (V)98.0d), () => (V)(R.Get(v_275, (V)"hp") == (V)98.0d)), () => (V)(R.Get(v_277, (V)"hp") == (V)100.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_95, R.Join(new V[] {v_283}, new Results((V)"crosswind"))).First) == (V)1.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Backtrail locks the original Dash cross until 150 time and hits moved-in enemies without tracking"}, new Results(new V((RuleFunction)(args_284 => {

var values_285 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results((V)"backtrail_mantle"))));
V v_286 = values_285[0];
V v_287 = values_285[1];

var values_288 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_286, (V)7.0d}, new Results((V)5.0d))));
V v_289 = values_288[0];
var values_290 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_286, (V)"dash", (V)1.0d}, new Results((V)0.0d))));
V v_291 = values_290[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_291, (V)"boon_hint") == (V)"Dash wake +150 time"), () => (V)((V)R.Length(R.Get(v_291, (V)"boon_cells")) == (V)5.0d)))));
R.Call(v_71, R.Join(new V[] {v_286, (V)"dash", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_286, (V)"clock") < (V)150.0d), () => (V)(R.Get(R.Get(v_287, (V)"position"), (V)"x") == (V)9.0d)), () => (V)(R.Get(v_289, (V)"hp") == (V)100.0d)))));
var values_292 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_286))));
V v_293 = values_292[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(v_293) == (V)1.0d), () => (V)(R.Get(R.Get(v_293, (V)1.0d), (V)"due") == (V)150.0d)))));
var values_294 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_286))));
V v_295 = values_294[0];
{ double end_298 = R.Num((V)30.0d), step_299 = R.Num((V)1.0d);
for (double i_297 = R.Num((V)1.0d); step_299 > 0 ? i_297 <= end_298 : i_297 >= end_298; i_297 += step_299) {
V v_296 = i_297;
R.Call(v_3, R.Join(new V[] {v_293}, R.Call(R.Get(v_5, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_286)))));
}
}
R.Call(v_3, R.Join(new V[] {v_295}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_286)))));
var values_300 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_286, (V)7.0d}, new Results((V)6.0d))));
V v_301 = values_300[0];
var values_302 = R.Join(new V[] {}, R.Call(v_71, R.Join(new V[] {v_286}, new Results((V)"wait"))));
V v_303 = values_302[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_289, (V)"hp") == (V)98.0d), () => (V)(R.Get(v_301, (V)"hp") == (V)98.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_5, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_286))).First) == (V)0.0d))));
var values_304 = Results.Empty;
V v_305 = values_304[0];
foreach (var row_306 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_303)))))) {
V v_307 = row_306[0];
V v_308 = row_306[1];
if (R.Truth((V)(R.Get(v_308, (V)"kind") == (V)"DelayedEffectResolved"))) {
var values_309 = R.Join(new V[] {}, new Results(v_308));
v_305 = values_309[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And(v_305, () => (V)(R.Get(v_305, (V)"time") == (V)150.0d)), () => R.Call(v_87, R.Join(new V[] {R.Get(v_305, (V)"target_cells"), (V)7.0d}, new Results((V)6.0d))).First), () => (V)(!R.Truth(R.Call(v_87, R.Join(new V[] {R.Get(v_305, (V)"target_cells"), (V)9.0d}, new Results((V)6.0d))).First))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"partial Dash still leaves one wake, but ordinary movement and escape-scroll movement cannot"}, new Results(new V((RuleFunction)(args_310 => {

var values_311 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results((V)"backtrail_mantle"))));
V v_312 = values_311[0];
V v_313 = values_311[1];

R.Call(v_57, R.Join(new V[] {v_312, (V)9.0d}, new Results((V)6.0d)));
R.Call(v_71, R.Join(new V[] {v_312, (V)"dash", (V)1.0d}, new Results((V)0.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_313, (V)"position"), (V)"x") == (V)8.0d), () => (V)((V)R.Length(R.Call(R.Get(v_5, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_312))).First) == (V)1.0d)))));
var values_314 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results((V)"backtrail_mantle"))));
v_312 = values_314[0];
v_313 = values_314[1];

R.Call(v_71, R.Join(new V[] {v_312, (V)"move", (V)1.0d}, new Results((V)0.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_5, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_312))).First) == (V)0.0d))));
var values_315 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"escape_scroll"))));
var obj_316 = v_313;
V key_317 = (V)"utility";
R.Set(obj_316, key_317, values_315[0]);
var values_318 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"submit"), R.Join(new V[] {v_312}, new Results(R.Table(new Entry((V)"kind", (V)"use_utility"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d), new Entry((V)"steps", (V)2.0d))))));
V v_319 = values_318[0];
V v_320 = values_318[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_319}, new Results(v_320)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_5, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_312))).First) == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"a Wayfinder beacon return cannot masquerade as a Dash wake"}, new Results(new V((RuleFunction)(args_321 => {

var values_322 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {(V)"backtrail_mantle"}, new Results((V)"core:actor/wayfinder"))));
V v_323 = values_322[0];
V v_324 = values_322[1];
R.Call(v_71, R.Join(new V[] {v_323, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d)));
R.Call(v_71, R.Join(new V[] {v_323, (V)"move", (-(V)1.0d)}, new Results((V)0.0d)));
var values_325 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_323, (V)"dash", (V)1.0d}, new Results((V)0.0d))));
V v_326 = values_325[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_326, (V)"valid"), () => R.Get(v_326, (V)"beacon_return")), () => (V)(!R.Truth(R.Get(v_326, (V)"boon_cells")))))));
R.Call(v_71, R.Join(new V[] {v_323, (V)"dash", (V)1.0d}, new Results((V)0.0d)));

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_5, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_323))).First) == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"pending wake and Temporal Echo keep separate locked cells and exact saved continuation"}, new Results(new V((RuleFunction)(args_327 => {

var values_328 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {(V)"backtrail_mantle", V.Nil}, new Results((V)true))));
V v_329 = values_328[0];

R.Call(v_16, R.Join(new V[] {v_329}, new Results((V)"temporal_echo")));

R.Call(v_57, R.Join(new V[] {v_329, (V)7.0d}, new Results((V)5.0d)));
R.Call(v_71, R.Join(new V[] {v_329, (V)"dash", (V)1.0d}, new Results((V)0.0d)));

R.Call(v_57, R.Join(new V[] {v_329, (V)10.0d}, new Results((V)6.0d)));
R.Call(v_71, R.Join(new V[] {}, new Results(v_329)));
var values_330 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"decode"), R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"encode"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_329))))))))));
V v_331 = values_330[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_5, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_329))).First) == (V)1.0d))));
foreach (var row_332 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"wait"), new Entry((V)2, (V)"basic_attack"), new Entry((V)3, (V)"wait")))))))) {
V v_333 = row_332[0];
V v_334 = row_332[1];
R.Call(v_3, R.Join(new V[] {R.Call(v_71, R.Join(new V[] {v_329}, new Results(v_334))).First}, R.Call(v_71, R.Join(new V[] {v_331}, new Results(v_334)))));

R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_329))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_331)))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"blocked hits, empty attacks and a rolled dice miss cannot activate primary-hit patterns"}, new Results(new V((RuleFunction)(args_335 => {

foreach (var row_336 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"linebreaker_pawn"), new Entry((V)2, (V)"rooks_wake"), new Entry((V)3, (V)"bishops_wake"), new Entry((V)4, (V)"knights_gambit"), new Entry((V)5, (V)"fan_of_blades")))))))) {
V v_337 = row_336[0];
V v_338 = row_336[1];
var values_339 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results(v_338))));
V v_340 = values_339[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_95, R.Join(new V[] {R.Call(v_71, R.Join(new V[] {}, new Results(v_340))).First}, new Results(v_338))).First) == (V)0.0d))));
var values_341 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_340, (V)8.0d}, new Results((V)6.0d))));
V v_342 = values_341[0];

var values_343 = R.Join(new V[] {}, new Results((V)100.0d));
var obj_344 = v_342;
V key_345 = (V)"defense";
R.Set(obj_344, key_345, values_343[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_95, R.Join(new V[] {R.Call(v_71, R.Join(new V[] {v_340}, new Results((V)"sweeping_slash"))).First}, new Results(v_338))).First) == (V)0.0d))));
}
var values_346 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results((V)"linebreaker_pawn"))));
V v_347 = values_346[0];
V v_348 = values_346[1];

var values_349 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_347, (V)8.0d}, new Results((V)6.0d))));
V v_350 = values_349[0];

var values_351 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_347, (V)9.0d}, new Results((V)6.0d))));
V v_352 = values_351[0];
R.Call(v_16, R.Join(new V[] {v_347}, new Results((V)"fateful_dice")));
{ double end_355 = R.Num((V)80.0d), step_356 = R.Num((V)1.0d);
for (double i_354 = R.Num((V)1.0d); step_356 > 0 ? i_354 <= end_355 : i_354 >= end_355; i_354 += step_356) {
V v_353 = i_354;
if (R.Truth((V)(R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))).First, (V)"next_int"), R.Join(new V[] {R.Table(new Entry((V)"state", v_353))}, new Results((V)8.0d))).First == (V)8.0d))) {
var values_357 = R.Join(new V[] {}, new Results(v_353));
var obj_358 = R.Get(v_347, (V)"rng");
V key_359 = (V)"state";
R.Set(obj_358, key_359, values_357[0]);

break;
}
}
}
var values_360 = R.Join(new V[] {}, R.Call(v_71, R.Join(new V[] {}, new Results(v_347))));
V v_361 = values_360[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_350, (V)"hp") == (V)100.0d), () => (V)(R.Get(v_352, (V)"hp") == (V)100.0d)), () => (V)((V)R.Length(R.Call(v_95, R.Join(new V[] {v_361}, new Results((V)"linebreaker_pawn"))).First) == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"all fifteen classes share pattern basics, including each real Duelist piercing victim"}, new Results(new V((RuleFunction)(args_362 => {

foreach (var row_363 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.classes"))).First, (V)"order"))))))) {
V v_364 = row_363[0];
V v_365 = row_363[1];
var values_366 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {(V)"linebreaker_pawn", v_365}, new Results((V)true))));
V v_367 = values_366[0];

R.Call(v_57, R.Join(new V[] {v_367, (V)8.0d}, new Results((V)6.0d)));

var values_368 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_367, (V)9.0d}, new Results((V)6.0d))));
V v_369 = values_368[0];
var values_370 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_367, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_371 = values_370[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(v_371, (V)"boon_cells"), () => R.Call(v_87, R.Join(new V[] {R.Get(v_371, (V)"boon_cells"), (V)9.0d}, new Results((V)6.0d))).First)}, new Results(v_365)));
var values_372 = R.Join(new V[] {}, R.Call(v_71, R.Join(new V[] {}, new Results(v_367))));
V v_373 = values_372[0];

var values_374 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_375 = values_374[0];
V v_376 = values_374[1];
foreach (var row_377 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_373)))))) {
V v_378 = row_377[0];
V v_379 = row_377[1];
if (R.Truth(R.And((V)(R.Get(v_379, (V)"kind") == (V)"DamageTaken"), () => R.Get(v_379, (V)"direct_attack")))) {
var values_380 = R.Join(new V[] {}, new Results((V)(v_375 + (V)1.0d)));
v_375 = values_380[0];
if (R.Truth((V)(R.Get(v_379, (V)"target_id") == R.Get(v_369, (V)"id")))) {
var values_381 = R.Join(new V[] {}, new Results(R.Get(v_379, (V)"actual_loss")));
v_376 = values_381[0];
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)((V)R.Length(R.Call(v_95, R.Join(new V[] {v_373}, new Results((V)"linebreaker_pawn"))).First) == v_375), () => (V)(R.Get(v_369, (V)"hp") == (V)((V)98.0d - v_376)))}, new Results(v_365)));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_367))).First}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_367)))))))));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"utility placement and parry cannot trigger special-only patterns or show on-hit cells"}, new Results(new V((RuleFunction)(args_382 => {

foreach (var row_383 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"core:actor/duelist"), new Entry((V)2, (V)"core:actor/geomancer"), new Entry((V)3, (V)"core:actor/tinkerer"), new Entry((V)4, (V)"core:actor/wayfinder"), new Entry((V)5, (V)"core:actor/spiritbinder")))))))) {
V v_384 = row_383[0];
V v_385 = row_383[1];
var values_386 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {(V)"knights_gambit"}, new Results(v_385))));
V v_387 = values_386[0];

R.Call(v_16, R.Join(new V[] {v_387}, new Results((V)"fan_of_blades")));
var values_388 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_387, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_389 = values_388[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.Get(v_389, (V)"valid"), () => (V)(!R.Truth(R.Get(v_389, (V)"boon_cells"))))}, new Results(v_385)));
var values_390 = R.Join(new V[] {}, R.Call(v_71, R.Join(new V[] {v_387}, new Results((V)"sweeping_slash"))));
V v_391 = values_390[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And((V)((V)R.Length(R.Call(v_95, R.Join(new V[] {v_391}, new Results((V)"knights_gambit"))).First) == (V)0.0d), () => (V)((V)R.Length(R.Call(v_95, R.Join(new V[] {v_391}, new Results((V)"fan_of_blades"))).First) == (V)0.0d))}, new Results(v_385)));
}
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"simultaneous primary sweep HP changes precede every overlapping rook pattern batch"}, new Results(new V((RuleFunction)(args_392 => {

var values_393 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results((V)"rooks_wake"))));
V v_394 = values_393[0];
var values_395 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_394, (V)8.0d}, new Results((V)5.0d))));
V v_396 = values_395[0];

var values_397 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_394, (V)8.0d}, new Results((V)6.0d))));
V v_398 = values_397[0];

var values_399 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_394, (V)8.0d}, new Results((V)7.0d))));
V v_400 = values_399[0];
var values_401 = R.Join(new V[] {}, R.Call(v_71, R.Join(new V[] {v_394}, new Results((V)"sweeping_slash"))));
V v_402 = values_401[0];
var values_403 = Results.Empty;
V v_404 = values_403[0];
V v_405 = values_403[1];
foreach (var row_406 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_402)))))) {
V v_407 = row_406[0];
V v_408 = row_406[1];
if (R.Truth(R.And((V)(R.Get(v_408, (V)"kind") == (V)"DamageBatchResolved"), () => (V)(!R.Truth(v_404))))) {
var values_409 = R.Join(new V[] {}, new Results(v_407));
v_404 = values_409[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_408, (V)"outcomes")) == (V)3.0d))));
}
if (R.Truth(R.And((V)(R.Get(v_408, (V)"kind") == (V)"BoonActivated"), () => (V)(!R.Truth(v_405))))) {
var values_410 = R.Join(new V[] {}, new Results(v_407));
v_405 = values_410[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_404 < v_405), () => (V)((V)R.Length(R.Call(v_95, R.Join(new V[] {v_402}, new Results((V)"rooks_wake"))).First) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_396, (V)"hp") == (V)96.0d), () => (V)(R.Get(v_398, (V)"hp") == (V)98.0d)), () => (V)(R.Get(v_400, (V)"hp") == (V)96.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"commanded Spirit patterns use the actual spirit origin in preview and damage"}, new Results(new V((RuleFunction)(args_411 => {

var values_412 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {(V)"rooks_wake"}, new Results((V)"core:actor/spiritbinder"))));
V v_413 = values_412[0];
V v_414 = values_412[1];
R.Call(R.Get(R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_companions"))).First, (V)"place"), R.Join(new V[] {v_413, v_414, R.Table(new Entry((V)"x", (V)9.0d), new Entry((V)"y", (V)5.0d))}, new Results(new V((RuleFunction)(args_415 => {


return Results.Empty;
})))));
R.Call(v_57, R.Join(new V[] {v_413, (V)9.0d}, new Results((V)6.0d)));

var values_416 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_413, (V)10.0d}, new Results((V)6.0d))));
V v_417 = values_416[0];

var values_418 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_413, (V)9.0d}, new Results((V)7.0d))));
V v_419 = values_418[0];
var values_420 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_413, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_421 = values_420[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_421, (V)"valid"), () => (V)(R.Get(v_421, (V)"companion_mode") == (V)"strike")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(v_87, R.Join(new V[] {R.Get(v_421, (V)"boon_cells"), (V)10.0d}, new Results((V)6.0d))).First, () => (V)(!R.Truth(R.Call(v_87, R.Join(new V[] {R.Get(v_421, (V)"boon_cells"), (V)9.0d}, new Results((V)7.0d))).First))))));
var values_422 = R.Join(new V[] {}, R.Call(v_71, R.Join(new V[] {v_413, (V)"sweeping_slash", (V)1.0d}, new Results((V)0.0d))));
V v_423 = values_422[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Call(v_95, R.Join(new V[] {v_423}, new Results((V)"rooks_wake"))).First) == (V)1.0d), () => (V)(R.Get(v_417, (V)"hp") == (V)98.0d)), () => (V)(R.Get(v_419, (V)"hp") == (V)100.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"mixed rarity uncapped copies increase damage without changing geometry or multiplying activations"}, new Results(new V((RuleFunction)(args_424 => {

var values_425 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results((V)"linebreaker_pawn"))));
V v_426 = values_425[0];
V v_427 = values_425[1];

R.Call(v_16, R.Join(new V[] {v_426, (V)"linebreaker_pawn", (V)"common"}, new Results((V)10.0d)));
R.Call(v_16, R.Join(new V[] {v_426, (V)"linebreaker_pawn", (V)"legendary"}, new Results((V)20.0d)));
R.Call(v_57, R.Join(new V[] {v_426, (V)8.0d}, new Results((V)6.0d)));

var values_428 = R.Join(new V[] {}, R.Call(v_57, R.Join(new V[] {v_426, (V)9.0d, (V)6.0d}, new Results((V)1000.0d))));
V v_429 = values_428[0];
var values_430 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_426))));
V v_431 = values_430[0];

var values_432 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_426, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_433 = values_432[0];
{ double end_436 = R.Num((V)30.0d), step_437 = R.Num((V)1.0d);
for (double i_435 = R.Num((V)1.0d); step_437 > 0 ? i_435 <= end_436 : i_435 >= end_436; i_435 += step_437) {
V v_434 = i_435;
R.Call(v_3, R.Join(new V[] {v_433}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_426, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d)))));
}
}
R.Call(v_3, R.Join(new V[] {v_431}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_426)))));
var values_438 = R.Join(new V[] {}, R.Call(v_71, R.Join(new V[] {}, new Results(v_426))));
V v_439 = values_438[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_95, R.Join(new V[] {v_439}, new Results((V)"linebreaker_pawn"))).First) == (V)1.0d), () => (V)(R.Get(v_429, (V)"hp") == (V)908.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"fog hides conditional pattern cells and unseen primary targets without changing gameplay state"}, new Results(new V((RuleFunction)(args_440 => {

var values_441 = R.Join(new V[] {}, R.Call(v_22, R.Join(new V[] {}, new Results((V)"linebreaker_pawn"))));
V v_442 = values_441[0];
V v_443 = values_441[1];

R.Call(v_57, R.Join(new V[] {v_442, (V)8.0d}, new Results((V)6.0d)));
var values_444 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_442, (V)"visible"), (V)"cells")));
V v_445 = values_444[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_445)));
var values_446 = R.Join(new V[] {}, new Results(R.Table()));
var obj_447 = R.Get(v_442, (V)"visible");
V key_448 = (V)"cells";
R.Set(obj_447, key_448, values_446[0]);
var values_449 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_442))));
V v_450 = values_449[0];
var values_451 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"preview"), R.Join(new V[] {v_442, (V)"basic_attack", (V)1.0d}, new Results((V)0.0d))));
V v_452 = values_451[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_452, (V)"boon_cells"))))));
R.Call(v_3, R.Join(new V[] {v_450}, R.Call(R.Get(v_5, (V)"snapshot"), R.Join(new V[] {}, new Results(v_442)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
