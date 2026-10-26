// Static C# port of tests/exploration_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 43201b3c50a885433740eab24e81814cb824d8c92ccf418ab938d6fe78031e69
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_exploration_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.generation"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rooms"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_17 = values_16[0];
var values_18 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.world"))));
V v_19 = values_18[0];
V v_20 = V.Nil;
v_20 = new V((RuleFunction)(args_21 => {
V v_22 = args_21[0];
V v_23 = args_21[1];
var values_24 = R.Join(new V[] {}, new Results(R.Table()));
V v_25 = values_24[0];
foreach (var row_26 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_22)))))) {
V v_27 = row_26[0];
V v_28 = row_26[1];
if (R.Truth((V)(R.Get(v_28, (V)"kind") == v_23))) {
var values_29 = R.Join(new V[] {}, new Results(v_28));
var obj_30 = v_25;
V key_31 = (V)((V)R.Length(v_25) + (V)1.0d);
R.Set(obj_30, key_31, values_29[0]);
}
}
return R.Join(new V[] {}, new Results(v_25));
return Results.Empty;
}));
V v_32 = V.Nil;
v_32 = new V((RuleFunction)(args_33 => {
V v_34 = args_33[0];
foreach (var row_35 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_34, (V)"entities"))))))) {
V v_36 = row_35[0];
V v_37 = row_35[1];
if (R.Truth((V)(R.Get(v_37, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_15, (V)"remove_for_actor"), R.Join(new V[] {v_34}, new Results(R.Get(v_37, (V)"id"))));
}
}
return Results.Empty;
}));
var values_38 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_39 = values_38[0];
V v_40 = V.Nil;
v_40 = new V((RuleFunction)(args_41 => {
V v_42 = args_41[0];
V v_43 = args_41[1];
return R.Join(new V[] {}, new Results(R.Concat(v_42, R.Concat((V)",", v_43))));
return Results.Empty;
}));
V v_44 = V.Nil;
v_44 = new V((RuleFunction)(args_45 => {
V v_46 = args_45[0];
V v_47 = args_45[1];
V v_48 = args_45[2];
var values_49 = R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"map_for"), R.Join(new V[] {}, new Results(v_46))));
V v_50 = values_49[0];
var values_51 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_46))));
V v_52 = values_51[0];
var values_53 = R.Join(new V[] {}, new Results(R.Table()));
V v_54 = values_53[0];
foreach (var row_55 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_46, (V)"entities"))))))) {
V v_56 = row_55[0];
V v_57 = row_55[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_57, (V)"id") != R.Get(v_52, (V)"id")), () => R.Get(v_57, (V)"blocks_movement")), () => (V)(R.Get(v_57, (V)"controller") != (V)"door")))) {
var values_58 = R.Join(new V[] {}, new Results((V)true));
var obj_59 = v_54;
V key_60 = R.Call(v_40, R.Join(new V[] {R.Get(R.Get(v_57, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_57, (V)"position"), (V)"y")))).First;
R.Set(obj_59, key_60, values_58[0]);
}
}
var values_61 = R.Join(new V[] {}, new Results(R.Get(v_52, (V)"position")));
V v_62 = values_61[0];
var values_63 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_62, (V)"x")), new Entry((V)"y", R.Get(v_62, (V)"y")), new Entry((V)"path", R.Table()))))));
V v_64 = values_63[0];
var values_65 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Call(v_40, R.Join(new V[] {R.Get(v_62, (V)"x")}, new Results(R.Get(v_62, (V)"y")))).First, (V)true))));
V v_66 = values_65[0];
var values_67 = R.Join(new V[] {}, new Results((V)1.0d));
V v_68 = values_67[0];
var values_69 = Results.Empty;
V v_70 = values_69[0];
while (R.Truth((V)(v_68 <= (V)R.Length(v_64)))) {
var values_71 = R.Join(new V[] {}, new Results(R.Get(v_64, v_68)));
V v_72 = values_71[0];

var values_73 = R.Join(new V[] {}, new Results((V)(v_68 + (V)1.0d)));
v_68 = values_73[0];
if (R.Truth(R.And((V)(R.Get(v_72, (V)"x") == v_47), () => (V)(R.Get(v_72, (V)"y") == v_48)))) {
var values_74 = R.Join(new V[] {}, new Results(R.Get(v_72, (V)"path")));
v_70 = values_74[0];

break;
}
foreach (var row_75 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_39)))))) {
V v_76 = row_75[0];
V v_77 = row_75[1];
var values_78 = R.Join(new V[] {(V)(R.Get(v_72, (V)"x") + R.Get(v_77, (V)1.0d))}, new Results((V)(R.Get(v_72, (V)"y") + R.Get(v_77, (V)2.0d))));
V v_79 = values_78[0];
V v_80 = values_78[1];
var values_81 = R.Join(new V[] {}, R.Call(v_40, R.Join(new V[] {v_79}, new Results(v_80))));
V v_82 = values_81[0];
if (R.Truth(R.And(R.And((V)(!R.Truth(R.Get(v_66, v_82))), () => (V)(!R.Truth(R.Get(v_54, v_82)))), () => R.Call(R.Get(v_5, (V)"walkable"), R.Join(new V[] {v_50, v_79}, new Results(v_80))).First))) {
var values_83 = R.Join(new V[] {}, new Results((V)true));
var obj_84 = v_66;
V key_85 = v_82;
R.Set(obj_84, key_85, values_83[0]);
var values_86 = R.Join(new V[] {}, new Results(R.Table()));
V v_87 = values_86[0];
foreach (var row_88 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_72, (V)"path"))))))) {
V v_89 = row_88[0];
V v_90 = row_88[1];
var values_91 = R.Join(new V[] {}, new Results(v_90));
var obj_92 = v_87;
V key_93 = v_89;
R.Set(obj_92, key_93, values_91[0]);
}
var values_94 = R.Join(new V[] {}, new Results(v_77));
var obj_95 = v_87;
V key_96 = (V)((V)R.Length(v_87) + (V)1.0d);
R.Set(obj_95, key_96, values_94[0]);
var values_97 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_79), new Entry((V)"y", v_80), new Entry((V)"path", v_87))));
var obj_98 = v_64;
V key_99 = (V)((V)R.Length(v_64) + (V)1.0d);
R.Set(obj_98, key_99, values_97[0]);
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_70}, new Results((V)"required generated exploration route is unavailable")));
foreach (var row_100 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_70)))))) {
V v_101 = row_100[0];
V v_102 = row_100[1];
var values_103 = R.Join(new V[] {R.Get(R.Get(v_52, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_52, (V)"position"), (V)"y")));
V v_104 = values_103[0];
V v_105 = values_103[1];
var values_106 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_46}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_102, (V)1.0d)), new Entry((V)"dy", R.Get(v_102, (V)2.0d)))))));
V v_107 = values_106[0];
V v_108 = values_106[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_107}, new Results(v_108)));
if (R.Truth(R.And((V)(R.Get(R.Get(v_52, (V)"position"), (V)"x") == v_104), () => (V)(R.Get(R.Get(v_52, (V)"position"), (V)"y") == v_105)))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_46, (V)"last_action"), (V)"kind") == (V)"open_door"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_46}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_102, (V)1.0d)), new Entry((V)"dy", R.Get(v_102, (V)2.0d))))))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_52, (V)"position"), (V)"x") == v_47), () => (V)(R.Get(R.Get(v_52, (V)"position"), (V)"y") == v_48)))));
return R.Join(new V[] {}, new Results((V)R.Length(v_70)));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"initial FOV sees an opaque wall but not beyond it or sealed diagonal corners"}, new Results(new V((RuleFunction)(args_109 => {

var values_110 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_111 = values_110[0];
var values_112 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_111))));
V v_113 = values_112[0];
var values_114 = R.Join(new V[] {(V)4.0d}, new Results((V)2.0d));
var obj_115 = R.Get(v_113, (V)"position");
V key_116 = (V)"x";
var obj_117 = R.Get(v_113, (V)"position");
V key_118 = (V)"y";
R.Set(obj_115, key_116, values_114[0]);
R.Set(obj_117, key_118, values_114[1]);
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_111)));
var values_119 = R.Join(new V[] {}, new Results(R.Get(v_111, (V)"active_floor_id")));
V v_120 = values_119[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_111, v_120, (V)4.0d}, new Results((V)3.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_111, v_120, (V)4.0d}, new Results((V)4.0d))).First)))));
var values_121 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_111))));
V v_122 = values_121[0];
{ double end_125 = R.Num((V)100.0d), step_126 = R.Num((V)1.0d);
for (double i_124 = R.Num((V)1.0d); step_126 > 0 ? i_124 <= end_125 : i_124 >= end_125; i_124 += step_126) {
V v_123 = i_124;
R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_111, v_120, (V)4.0d}, new Results((V)3.0d)));
R.Call(R.Get(v_7, (V)"was_explored"), R.Join(new V[] {v_111, v_120, (V)4.0d}, new Results((V)3.0d)));
}
}
R.Call(v_3, R.Join(new V[] {v_122}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_111)))));
var values_127 = R.Join(new V[] {(V)4.0d}, new Results((V)4.0d));
var obj_128 = R.Get(v_113, (V)"position");
V key_129 = (V)"x";
var obj_130 = R.Get(v_113, (V)"position");
V key_131 = (V)"y";
R.Set(obj_128, key_129, values_127[0]);
R.Set(obj_130, key_131, values_127[1]);
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {v_111}, new Results((V)1.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_111, v_120, (V)4.0d}, new Results((V)3.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_111, v_120, (V)4.0d}, new Results((V)2.0d))).First)))));
var values_132 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_133 = values_132[0];
var values_134 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)"test:corner"), new Entry((V)"floor_id", R.Get(v_133, (V)"active_floor_id")), new Entry((V)"width", (V)5.0d), new Entry((V)"height", (V)5.0d), new Entry((V)"start_x", (V)2.0d), new Entry((V)"start_y", (V)2.0d), new Entry((V)"rows", R.Table(new Entry((V)1, (V)"#####"), new Entry((V)2, (V)"#.#.#"), new Entry((V)3, (V)"##..#"), new Entry((V)4, (V)"#...#"), new Entry((V)5, (V)"#####"))))));
var obj_135 = v_133;
V key_136 = (V)"generated_map";
R.Set(obj_135, key_136, values_134[0]);
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {v_133}, new Results((V)3.0d)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_133, v_120, (V)3.0d}, new Results((V)3.0d))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_133, v_120, (V)3.0d}, new Results((V)2.0d))).First, () => R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_133, v_120, (V)2.0d}, new Results((V)3.0d))).First))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"exploration memory survives movement and restore without world-time draws"}, new Results(new V((RuleFunction)(args_137 => {

var values_138 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new_generated"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_139 = values_138[0];
R.Call(v_32, R.Join(new V[] {}, new Results(v_139)));
var values_140 = R.Join(new V[] {}, new Results(R.Get(v_139, (V)"active_floor_id")));
V v_141 = values_140[0];
var values_142 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_139))));
V v_143 = values_142[0];
var values_144 = R.Join(new V[] {R.Get(R.Get(v_143, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_143, (V)"position"), (V)"y")));
V v_145 = values_144[0];
V v_146 = values_144[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_139, v_141, v_145}, new Results(v_146)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"was_explored"), R.Join(new V[] {v_139, v_141, R.Get(R.Get(R.Get(v_139, (V)"generated_map"), (V)"elite"), (V)"x")}, new Results(R.Get(R.Get(R.Get(v_139, (V)"generated_map"), (V)"elite"), (V)"y")))).First)))));
var values_147 = R.Join(new V[] {R.Get(R.Get(v_139, (V)"rng"), (V)"state")}, new Results(R.Get(v_139, (V)"clock")));
V v_148 = values_147[0];
V v_149 = values_147[1];
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_139)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_139, (V)"rng"), (V)"state") == v_148), () => (V)(R.Get(v_139, (V)"clock") == v_149)))));
var values_150 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_139, (V)"generated_map"), (V)"elite")));
V v_151 = values_150[0];
var values_152 = R.Join(new V[] {R.Get(v_151, (V)"x")}, new Results(R.Get(v_151, (V)"y")));
var obj_153 = R.Get(v_143, (V)"position");
V key_154 = (V)"x";
var obj_155 = R.Get(v_143, (V)"position");
V key_156 = (V)"y";
R.Set(obj_153, key_154, values_152[0]);
R.Set(obj_155, key_156, values_152[1]);
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_139)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_7, (V)"was_explored"), R.Join(new V[] {v_139, v_141, v_145}, new Results(v_146))).First, () => (V)(!R.Truth(R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_139, v_141, v_145}, new Results(v_146))).First))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_139, v_141, R.Get(v_151, (V)"x")}, new Results(R.Get(v_151, (V)"y"))))));
var values_157 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_139))));
V v_158 = values_157[0];
R.Call(v_3, R.Join(new V[] {v_158}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, new Results(v_158)))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"100 generated floors have two optional reachable transformed secrets and door"}, new Results(new V((RuleFunction)(args_159 => {

var values_160 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_161 = values_160[0];
V v_162 = values_160[1];
var values_163 = R.Join(new V[] {}, new Results(R.Table()));
V v_164 = values_163[0];
{ double end_167 = R.Num((V)100.0d), step_168 = R.Num((V)1.0d);
for (double i_166 = R.Num((V)1.0d); step_168 > 0 ? i_166 <= end_167 : i_166 >= end_167; i_166 += step_168) {
V v_165 = i_166;
var values_169 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"generate"), R.Join(new V[] {}, new Results(v_165))));
V v_170 = values_169[0];
var values_171 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"validate"), R.Join(new V[] {}, new Results(v_170))));
V v_172 = values_171[0];
V v_173 = values_171[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_172}, new Results(R.Concat(R.Or(R.And(R.Get(v_173, (V)1.0d), () => R.Get(R.Get(v_173, (V)1.0d), (V)"kind")), () => (V)"unknown"), R.Concat((V)" seed ", v_165)))));
if (R.Truth((V)((V)R.Length(R.Get(R.Get(v_170, (V)"diagnostics"), (V)"attempts")) > (V)1.0d))) {
var values_174 = R.Join(new V[] {}, new Results((V)(v_161 + (V)1.0d)));
v_161 = values_174[0];
}
if (R.Truth(R.Get(R.Get(v_170, (V)"diagnostics"), (V)"fallback"))) {
var values_175 = R.Join(new V[] {}, new Results((V)(v_162 + (V)1.0d)));
v_162 = values_175[0];
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(R.Get(v_170, (V)"placements"), (V)"secrets")) == (V)2.0d), () => (V)((V)R.Length(R.Get(R.Get(v_170, (V)"placements"), (V)"doors")) == (V)1.0d)))));
var values_176 = R.Join(new V[] {}, new Results(R.Table()));
V v_177 = values_176[0];
foreach (var row_178 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_170, (V)"graph"), (V)"critical"))))))) {
V v_179 = row_178[0];
V v_180 = row_178[1];
var values_181 = R.Join(new V[] {}, new Results((V)true));
var obj_182 = v_177;
V key_183 = v_180;
R.Set(obj_182, key_183, values_181[0]);
}
foreach (var row_184 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_170, (V)"placements"), (V)"secrets"))))))) {
V v_185 = row_184[0];
V v_186 = row_184[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_177, R.Get(v_186, (V)"room_id")))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"walkable"), R.Join(new V[] {v_170, R.Get(v_186, (V)"x")}, new Results(R.Get(v_186, (V)"y"))))));
var values_187 = R.Join(new V[] {}, new Results((V)true));
var obj_188 = v_164;
V key_189 = R.Call(v_40, R.Join(new V[] {R.Get(v_186, (V)"x")}, new Results(R.Get(v_186, (V)"y")))).First;
R.Set(obj_188, key_189, values_187[0]);
if (R.Truth(R.Get(v_186, (V)"wall_x"))) {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(R.Get(R.Get(v_170, (V)"rows"), R.Get(v_186, (V)"wall_y")), "sub", R.Join(new V[] {R.Get(v_186, (V)"wall_x")}, new Results(R.Get(v_186, (V)"wall_x")))).First == (V)"#"))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(R.Get(R.Get(v_170, (V)"visual_rows"), R.Get(v_186, (V)"wall_y")), "sub", R.Join(new V[] {R.Get(v_186, (V)"wall_x")}, new Results(R.Get(v_186, (V)"wall_x")))).First == (V)"k"))));
} else {
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(R.Get(R.Get(v_170, (V)"visual_rows"), R.Get(v_186, (V)"y")), "sub", R.Join(new V[] {R.Get(v_186, (V)"x")}, new Results(R.Get(v_186, (V)"x")))).First == (V)"p"))));
}
}
}
}
var values_190 = R.Join(new V[] {}, new Results((V)0.0d));
V v_191 = values_190[0];
foreach (var row_192 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_164)))))) {
V v_193 = row_192[0];
var values_194 = R.Join(new V[] {}, new Results((V)(v_191 + (V)1.0d)));
v_191 = values_194[0];
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_191 >= (V)12.0d))));
R.Call(rt.Global("print"), R.Join(new V[] {}, R.Invoke((V)"SECRETS 100 seeds: %d retries, %d fallbacks, %d clue positions", "format", R.Join(new V[] {v_161, v_162}, new Results(v_191)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"secret clue repair is local; protected fault still retries and falls back"}, new Results(new V((RuleFunction)(args_195 => {

var values_196 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"generate"), R.Join(new V[] {(V)301.0d}, new Results(R.Table(new Entry((V)"fault", (V)"secret_clue"), new Entry((V)"retry_limit", (V)1.0d))))));
V v_197 = values_196[0];
var values_198 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"generate"), R.Join(new V[] {(V)301.0d}, new Results(R.Table(new Entry((V)"fault", (V)"secret_clue"), new Entry((V)"retry_limit", (V)1.0d))))));
V v_199 = values_198[0];
R.Call(v_3, R.Join(new V[] {v_197}, new Results(v_199)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)((V)R.Length(R.Get(v_197, (V)"repairs")) == (V)1.0d), () => (V)(R.Get(R.Get(R.Get(v_197, (V)"repairs"), (V)1.0d), (V)"kind") == (V)"restore_secret_clue")), () => R.Get(R.Get(R.Get(v_197, (V)"repairs"), (V)1.0d), (V)"valid_after")), () => R.Call(R.Get(v_9, (V)"validate"), R.Join(new V[] {}, new Results(v_197))).First))));
var values_200 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"generate"), R.Join(new V[] {(V)301.0d}, new Results(R.Table(new Entry((V)"fault", (V)"protected"), new Entry((V)"retry_limit", (V)1.0d))))));
V v_201 = values_200[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(R.Get(v_201, (V)"diagnostics"), (V)"fallback"), () => R.Call(R.Get(v_9, (V)"validate"), R.Join(new V[] {}, new Results(v_201))).First), () => (V)((V)R.Length(R.Get(R.Get(v_201, (V)"placements"), (V)"secrets")) == (V)2.0d)))));
var values_202 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_13, (V)"definitions"), (V)"core:room/gallery")));
V v_203 = values_202[0];
var values_204 = R.Join(new V[] {}, R.Call(R.Get(v_13, (V)"transform"), R.Join(new V[] {v_203, (V)1.0d}, new Results((V)true))));
V v_205 = values_204[0];
var values_206 = R.Join(new V[] {R.Get(R.Get(R.Get(v_205, (V)"markers"), (V)"secret_anchor"), (V)1.0d)}, new Results(R.Get(R.Get(R.Get(v_205, (V)"markers"), (V)"secret_wall"), (V)1.0d)));
V v_207 = values_206[0];
V v_208 = values_206[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_207, (V)"x") - R.Get(v_208, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_207, (V)"y") - R.Get(v_208, (V)"y"))))).First) == (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(R.Get(R.Get(v_205, (V)"rows"), R.Get(v_208, (V)"y")), "sub", R.Join(new V[] {R.Get(v_208, (V)"x")}, new Results(R.Get(v_208, (V)"x")))).First == (V)"#"))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"wall secret needs explicit empty Sword and opens one cached reward alcove"}, new Results(new V((RuleFunction)(args_209 => {

var values_210 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new_generated"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_211 = values_210[0];
R.Call(v_32, R.Join(new V[] {}, new Results(v_211)));
var values_212 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_211, (V)"secrets"), (V)1.0d)));
V v_213 = values_212[0];
var values_214 = R.Join(new V[] {R.Get(v_213, (V)"wall_x")}, new Results(R.Get(v_213, (V)"wall_y")));
V v_215 = values_214[0];
V v_216 = values_214[1];
var values_217 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_211, (V)"rng"), (V)"state")));
V v_218 = values_217[0];
R.Call(v_44, R.Join(new V[] {v_211, R.Get(v_213, (V)"x")}, new Results(R.Get(v_213, (V)"y"))));
var values_219 = R.Join(new V[] {}, new Results(R.Get(v_211, (V)"clock")));
V v_220 = values_219[0];
var values_221 = R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_211, (V)"entities"))));
V v_222 = values_221[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_211, R.Get(v_211, (V)"active_floor_id"), R.Get(v_213, (V)"cache_x")}, new Results(R.Get(v_213, (V)"cache_y")))).First)))));
var values_223 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_211}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)(v_215 - R.Get(v_213, (V)"x"))), new Entry((V)"dy", (V)(v_216 - R.Get(v_213, (V)"y"))))))));
V v_224 = values_223[0];
V v_225 = values_223[1];
V v_226 = values_223[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_224}, new Results(v_225)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_211, (V)"last_action"), (V)"cost") == (V)100.0d), () => (V)(R.Get(v_211, (V)"clock") == (V)(v_220 + (V)100.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_20, R.Join(new V[] {v_226}, new Results((V)"AttackPerformed"))).First) == (V)1.0d), () => (V)((V)R.Length(R.Call(v_20, R.Join(new V[] {v_226}, new Results((V)"HitConfirmed"))).First) == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_20, R.Join(new V[] {v_226}, new Results((V)"SecretDiscovered"))).First) == (V)1.0d), () => (V)((V)R.Length(R.Call(v_20, R.Join(new V[] {v_226}, new Results((V)"SecretRewardSpawned"))).First) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_213, (V)"revealed"), () => (V)((V)R.Length(R.Get(v_211, (V)"entities")) == (V)(v_222 + (V)1.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_5, (V)"walkable"), R.Join(new V[] {R.Get(v_211, (V)"generated_map"), v_215}, new Results(v_216))).First, () => R.Call(R.Get(v_5, (V)"walkable"), R.Join(new V[] {R.Get(v_211, (V)"generated_map"), R.Get(v_213, (V)"cache_x")}, new Results(R.Get(v_213, (V)"cache_y")))).First))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_211, R.Get(v_211, (V)"active_floor_id"), R.Get(v_213, (V)"cache_x")}, new Results(R.Get(v_213, (V)"cache_y"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_211, (V)"rng"), (V)"state") == v_218))));
var values_227 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"entity"), R.Join(new V[] {v_211}, new Results(R.Get(v_213, (V)"reward_id")))));
V v_228 = values_227[0];
var values_229 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(R.Get(v_228, (V)"offers")))));
V v_230 = values_229[0];
var values_231 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_211}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)(v_215 - R.Get(v_213, (V)"x"))), new Entry((V)"dy", (V)(v_216 - R.Get(v_213, (V)"y"))))))));
V v_232 = values_231[0];
V v_233 = values_231[1];
V v_234 = values_231[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_20, R.Join(new V[] {v_234}, new Results((V)"SecretDiscovered"))).First) == (V)0.0d), () => (V)((V)R.Length(R.Get(v_211, (V)"entities")) == (V)(v_222 + (V)1.0d))))));
R.Call(v_3, R.Join(new V[] {v_230}, new Results(R.Get(v_228, (V)"offers"))));
var values_235 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_211))));
V v_236 = values_235[0];
R.Call(v_3, R.Join(new V[] {v_236}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, new Results(v_236)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_211}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (V)(v_215 - R.Get(v_213, (V)"x"))), new Entry((V)"dy", (V)(v_216 - R.Get(v_213, (V)"y")))))))));
var values_237 = R.Join(new V[] {(V)(R.Get(v_213, (V)"cache_x") - v_215)}, new Results((V)(R.Get(v_213, (V)"cache_y") - v_216)));
V v_238 = values_237[0];
V v_239 = values_237[1];
var values_240 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_211}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_238), new Entry((V)"dy", v_239))))));
V v_241 = values_240[0];
V v_242 = values_240[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_241)), () => (V)(v_242 == (V)"chest")))));
var values_243 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_211}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_228, (V)"id")), new Entry((V)"offer_index", (V)1.0d))))));
V v_244 = values_243[0];
V v_245 = values_243[1];
V v_246 = values_243[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_20, R.Join(new V[] {v_246}, new Results((V)"BoonGranted"))).First) == (V)1.0d), () => R.Get(v_228, (V)"claimed")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_211, (V)"currency") == (V)0.0d))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"movement sigil rewards exactly once on TileEntered and persists"}, new Results(new V((RuleFunction)(args_247 => {

var values_248 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new_generated"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_249 = values_248[0];
R.Call(v_32, R.Join(new V[] {}, new Results(v_249)));
var values_250 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_249, (V)"secrets"), (V)2.0d)));
V v_251 = values_250[0];
var values_252 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_249))));
V v_253 = values_252[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_249}, new Results(R.Table(new Entry((V)"kind", (V)"cancel"))))).First)))));
R.Call(v_3, R.Join(new V[] {v_253}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_249)))));
var values_254 = R.Join(new V[] {}, R.Call(v_44, R.Join(new V[] {v_249, R.Get(v_251, (V)"x")}, new Results(R.Get(v_251, (V)"y")))));
V v_255 = values_254[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(v_255 > (V)0.0d), () => R.Get(v_251, (V)"revealed")), () => (V)(R.Get(v_249, (V)"currency") == (V)4.0d)))));
var values_256 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_249, (V)"drops"), (V)R.Length(R.Get(v_249, (V)"drops")))));
V v_257 = values_256[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_257, (V)"secret_id") == R.Get(v_251, (V)"id")), () => R.Get(v_257, (V)"collected")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(R.Get(R.Get(R.Get(v_249, (V)"generated_map"), (V)"visual_rows"), R.Get(v_251, (V)"y")), "sub", R.Join(new V[] {R.Get(v_251, (V)"x")}, new Results(R.Get(v_251, (V)"x")))).First == (V)"m"))));
var values_258 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_249))));
V v_259 = values_258[0];
R.Call(v_3, R.Join(new V[] {v_259}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, new Results(v_259)))))));
R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_249}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_249, (V)"currency") == (V)4.0d))));
var values_260 = Results.Empty;
V v_261 = values_260[0];
foreach (var row_262 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_39)))))) {
V v_263 = row_262[0];
V v_264 = row_262[1];
if (R.Truth(R.Call(R.Get(v_5, (V)"walkable"), R.Join(new V[] {R.Get(v_249, (V)"generated_map"), (V)(R.Get(v_251, (V)"x") + R.Get(v_264, (V)1.0d))}, new Results((V)(R.Get(v_251, (V)"y") + R.Get(v_264, (V)2.0d))))).First)) {
var values_265 = R.Join(new V[] {}, new Results((V)false));
V v_266 = values_265[0];
foreach (var row_267 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_249, (V)"entities"))))))) {
V v_268 = row_267[0];
V v_269 = row_267[1];
if (R.Truth(R.And(R.And(R.Get(v_269, (V)"blocks_movement"), () => (V)(R.Get(R.Get(v_269, (V)"position"), (V)"x") == (V)(R.Get(v_251, (V)"x") + R.Get(v_264, (V)1.0d)))), () => (V)(R.Get(R.Get(v_269, (V)"position"), (V)"y") == (V)(R.Get(v_251, (V)"y") + R.Get(v_264, (V)2.0d)))))) {
var values_270 = R.Join(new V[] {}, new Results((V)true));
v_266 = values_270[0];
}
}
if (R.Truth((V)(!R.Truth(v_266)))) {
var values_271 = R.Join(new V[] {}, new Results(v_264));
v_261 = values_271[0];

break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_261)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_249}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_261, (V)1.0d)), new Entry((V)"dy", R.Get(v_261, (V)2.0d))))))));
var values_272 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_249}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (-R.Get(v_261, (V)1.0d))), new Entry((V)"dy", (-R.Get(v_261, (V)2.0d))))))));
V v_273 = values_272[0];
V v_274 = values_272[1];
V v_275 = values_272[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_20, R.Join(new V[] {v_275}, new Results((V)"SecretDiscovered"))).First) == (V)0.0d), () => (V)(R.Get(v_249, (V)"currency") == (V)4.0d)))));
var values_276 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new_generated"), R.Join(new V[] {}, new Results((V)2.0d))));
V v_277 = values_276[0];
R.Call(v_32, R.Join(new V[] {}, new Results(v_277)));
var values_278 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_277, (V)"secrets"), (V)2.0d)));
V v_279 = values_278[0];
var values_280 = R.Join(new V[] {R.Get(v_279, (V)"x")}, new Results(R.Get(v_279, (V)"y")));
var obj_281 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_277))).First, (V)"position");
V key_282 = (V)"x";
var obj_283 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_277))).First, (V)"position");
V key_284 = (V)"y";
R.Set(obj_281, key_282, values_280[0]);
R.Set(obj_283, key_284, values_280[1]);
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_277)));
R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_277}, new Results(R.Table(new Entry((V)"kind", (V)"wait")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(v_279, (V)"revealed"))), () => (V)(R.Get(v_277, (V)"currency") == (V)0.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"closed optional door blocks sight and projectiles; bump opens once"}, new Results(new V((RuleFunction)(args_285 => {

var values_286 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new_generated"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_287 = values_286[0];
R.Call(v_32, R.Join(new V[] {}, new Results(v_287)));
var values_288 = Results.Empty;
V v_289 = values_288[0];
foreach (var row_290 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_287, (V)"entities"))))))) {
V v_291 = row_290[0];
V v_292 = row_290[1];
if (R.Truth((V)(R.Get(v_292, (V)"controller") == (V)"door"))) {
var values_293 = R.Join(new V[] {}, new Results(v_292));
v_289 = values_293[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_289)));
var values_294 = Results.Empty;
V v_295 = values_294[0];
foreach (var row_296 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_287, (V)"generated_map"), (V)"rooms"))))))) {
V v_297 = row_296[0];
V v_298 = row_296[1];
if (R.Truth((V)(R.Get(v_298, (V)"id") == R.Get(R.Get(R.Get(R.Get(R.Get(v_287, (V)"generated_map"), (V)"placements"), (V)"doors"), (V)1.0d), (V)"room_id")))) {
var values_299 = R.Join(new V[] {}, new Results(v_298));
v_295 = values_299[0];
}
}
var values_300 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(R.Get(v_289, (V)"position"), (V)"x") == R.Get(v_295, (V)"x")), () => (-(V)1.0d)), () => R.Or(R.And((V)(R.Get(R.Get(v_289, (V)"position"), (V)"x") == (V)((V)(R.Get(v_295, (V)"x") + R.Get(v_295, (V)"width")) - (V)1.0d)), () => (V)1.0d), () => (V)0.0d))));
V v_301 = values_300[0];
var values_302 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(R.Get(v_289, (V)"position"), (V)"y") == R.Get(v_295, (V)"y")), () => (-(V)1.0d)), () => R.Or(R.And((V)(R.Get(R.Get(v_289, (V)"position"), (V)"y") == (V)((V)(R.Get(v_295, (V)"y") + R.Get(v_295, (V)"height")) - (V)1.0d)), () => (V)1.0d), () => (V)0.0d))));
V v_303 = values_302[0];
var values_304 = R.Join(new V[] {(V)(R.Get(R.Get(v_289, (V)"position"), (V)"x") + v_301)}, new Results((V)(R.Get(R.Get(v_289, (V)"position"), (V)"y") + v_303)));
V v_305 = values_304[0];
V v_306 = values_304[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"walkable"), R.Join(new V[] {R.Get(v_287, (V)"generated_map"), v_305}, new Results(v_306)))));
var values_307 = R.Join(new V[] {v_305}, new Results(v_306));
var obj_308 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_287))).First, (V)"position");
V key_309 = (V)"x";
var obj_310 = R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_287))).First, (V)"position");
V key_311 = (V)"y";
R.Set(obj_308, key_309, values_307[0]);
R.Set(obj_310, key_311, values_307[1]);
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_287)));
var values_312 = R.Join(new V[] {}, new Results(R.Get(v_287, (V)"generated_map")));
V v_313 = values_312[0];
var values_314 = R.Join(new V[] {(V)(R.Get(R.Get(v_289, (V)"position"), (V)"x") - v_301)}, new Results((V)(R.Get(R.Get(v_289, (V)"position"), (V)"y") - v_303)));
V v_315 = values_314[0];
V v_316 = values_314[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"walkable"), R.Join(new V[] {v_313, v_315}, new Results(v_316)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_287, R.Get(v_287, (V)"active_floor_id"), v_315}, new Results(v_316))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"blocks_vision"), R.Join(new V[] {v_313, R.Get(R.Get(v_289, (V)"position"), (V)"x"), R.Get(R.Get(v_289, (V)"position"), (V)"y")}, new Results(v_287)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"projectile_passable"), R.Join(new V[] {v_313, R.Get(R.Get(v_289, (V)"position"), (V)"x"), R.Get(R.Get(v_289, (V)"position"), (V)"y")}, new Results(v_287))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(R.Call(R.Get(v_7, (V)"preview"), R.Join(new V[] {v_287, (V)"dash", (-v_301)}, new Results((-v_303)))).First, (V)"valid"))))));
var values_317 = R.Join(new V[] {}, new Results(R.Get(v_287, (V)"clock")));
V v_318 = values_317[0];
var values_319 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_287}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (-v_301)), new Entry((V)"dy", (-v_303)))))));
V v_320 = values_319[0];
V v_321 = values_319[1];
V v_322 = values_319[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_320}, new Results(v_321)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_287, (V)"last_action"), (V)"kind") == (V)"open_door"), () => (V)(R.Get(v_287, (V)"clock") == (V)(v_318 + (V)100.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_287))).First, (V)"position"), (V)"x") == v_305), () => (V)(R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_287))).First, (V)"position"), (V)"y") == v_306)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Call(v_20, R.Join(new V[] {v_322}, new Results((V)"DoorOpened"))).First) == (V)1.0d), () => R.Get(v_289, (V)"open")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_5, (V)"blocks_vision"), R.Join(new V[] {v_313, R.Get(R.Get(v_289, (V)"position"), (V)"x"), R.Get(R.Get(v_289, (V)"position"), (V)"y")}, new Results(v_287))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_5, (V)"projectile_passable"), R.Join(new V[] {v_313, R.Get(R.Get(v_289, (V)"position"), (V)"x"), R.Get(R.Get(v_289, (V)"position"), (V)"y")}, new Results(v_287)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_287, R.Get(v_287, (V)"active_floor_id"), v_315}, new Results(v_316)))));
var values_323 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_287))))));
V v_324 = values_323[0];
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_287))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_324)))));
var values_325 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_287}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (-v_301)), new Entry((V)"dy", (-v_303)))))));
V v_326 = values_325[0];
V v_327 = values_325[1];
V v_328 = values_325[2];
var values_329 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_324}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", (-v_301)), new Entry((V)"dy", (-v_303)))))));
V v_330 = values_329[0];
V v_331 = values_329[1];
V v_332 = values_329[2];
R.Call(v_3, R.Join(new V[] {v_328}, new Results(v_332)));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_287))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_324)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_287))).First, (V)"position"), (V)"x") == R.Get(R.Get(v_289, (V)"position"), (V)"x")))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"off-FOV enemy still follows the strict authoritative AI schedule"}, new Results(new V((RuleFunction)(args_333 => {

var values_334 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new_generated"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_335 = values_334[0];
var values_336 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_335))));
V v_337 = values_336[0];
var values_338 = Results.Empty;
V v_339 = values_338[0];
foreach (var row_340 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_335, (V)"entities"))))))) {
V v_341 = row_340[0];
V v_342 = row_340[1];
if (R.Truth(R.And((V)(R.Get(v_342, (V)"controller") == (V)"ai"), () => (V)(!R.Truth(R.Call(R.Get(v_7, (V)"is_visible"), R.Join(new V[] {v_335, R.Get(v_335, (V)"active_floor_id"), R.Get(R.Get(v_342, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_342, (V)"position"), (V)"y")))).First))))) {
var values_343 = R.Join(new V[] {}, new Results(v_342));
v_339 = values_343[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_339)));
var values_344 = R.Join(new V[] {(V)1000.0d}, new Results((V)1000.0d));
var obj_345 = v_337;
V key_346 = (V)"hp";
var obj_347 = v_337;
V key_348 = (V)"max_hp";
R.Set(obj_345, key_346, values_344[0]);
R.Set(obj_347, key_348, values_344[1]);
var values_349 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_335}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_350 = values_349[0];
V v_351 = values_349[1];
V v_352 = values_349[2];
R.Call(rt.Global("assert"), R.Join(new V[] {v_350}, new Results(v_351)));
var values_353 = R.Join(new V[] {}, new Results((V)false));
V v_354 = values_353[0];
foreach (var row_355 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_352)))))) {
V v_356 = row_355[0];
V v_357 = row_355[1];
if (R.Truth(R.And((V)(R.Get(v_357, (V)"source_id") == R.Get(v_339, (V)"id")), () => R.Or(R.Or((V)(R.Get(v_357, (V)"kind") == (V)"Moved"), () => (V)(R.Get(v_357, (V)"kind") == (V)"WindupStarted")), () => (V)(R.Get(v_357, (V)"kind") == (V)"Waited"))))) {
var values_358 = R.Join(new V[] {}, new Results((V)true));
v_354 = values_358[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_354, () => (V)(R.Get(v_335, (V)"clock") == (V)100.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"secret rewards use isolated streams and continuation replays identically"}, new Results(new V((RuleFunction)(args_359 => {

var values_360 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new_generated"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_361 = values_360[0];
var values_362 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"new_generated"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_363 = values_362[0];
R.Call(v_32, R.Join(new V[] {}, new Results(v_361)));

R.Call(v_32, R.Join(new V[] {}, new Results(v_363)));
var values_364 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_361, (V)"secrets"), (V)1.0d)));
V v_365 = values_364[0];
var values_366 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_363, (V)"secrets"), (V)1.0d)));
V v_367 = values_366[0];
foreach (var row_368 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_361), new Entry((V)2, v_363)))))))) {
V v_369 = row_368[0];
V v_370 = row_368[1];
var values_371 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"player"), R.Join(new V[] {}, new Results(v_370))));
V v_372 = values_371[0];
var values_373 = R.Join(new V[] {R.Get(v_365, (V)"x")}, new Results(R.Get(v_365, (V)"y")));
var obj_374 = R.Get(v_372, (V)"position");
V key_375 = (V)"x";
var obj_376 = R.Get(v_372, (V)"position");
V key_377 = (V)"y";
R.Set(obj_374, key_375, values_373[0]);
R.Set(obj_376, key_377, values_373[1]);
R.Call(R.Get(v_17, (V)"recompute"), R.Join(new V[] {}, new Results(v_370)));
}
var values_378 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_361, (V)"rng"), (V)"state")));
V v_379 = values_378[0];
var values_380 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)(R.Get(v_365, (V)"wall_x") - R.Get(v_365, (V)"x"))), new Entry((V)"dy", (V)(R.Get(v_365, (V)"wall_y") - R.Get(v_365, (V)"y"))))));
V v_381 = values_380[0];
var values_382 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_361}, new Results(v_381))));
V v_383 = values_382[0];
V v_384 = values_382[1];
V v_385 = values_382[2];
var values_386 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_363}, new Results(v_381))));
V v_387 = values_386[0];
V v_388 = values_386[1];
V v_389 = values_386[2];
R.Call(v_3, R.Join(new V[] {v_385}, new Results(v_389)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_361, (V)"rng"), (V)"state") == v_379), () => (V)(R.Get(R.Get(v_363, (V)"rng"), (V)"state") == v_379)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_365, (V)"revealed"), () => R.Get(v_367, (V)"revealed")))));
var values_390 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_361))))));
V v_391 = values_390[0];
var values_392 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"kind", (V)"wait"))), new Entry((V)2, R.Table(new Entry((V)"kind", (V)"wait"))), new Entry((V)3, R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_393 = values_392[0];
foreach (var row_394 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_393)))))) {
V v_395 = row_394[0];
V v_396 = row_394[1];
var values_397 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_361}, new Results(v_396))));
V v_398 = values_397[0];
V v_399 = values_397[1];
V v_400 = values_397[2];
var values_401 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"submit"), R.Join(new V[] {v_391}, new Results(v_396))));
V v_402 = values_401[0];
V v_403 = values_401[1];
V v_404 = values_401[2];
R.Call(v_3, R.Join(new V[] {v_400}, new Results(v_404)));
}
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_361))).First}, R.Call(R.Get(v_7, (V)"snapshot"), R.Join(new V[] {}, new Results(v_391)))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
