// Static C# port of src/ai.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 5c6aaad8df11856d1e21df9c3cccd7950f1900ff37372bebab73b4f19da35e2f
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_ai { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.terrain"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.elite_ai"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.regular_ai"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.squad_ai"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.idea_enemies"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.visibility"))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, new Results(R.Table()));
V v_16 = values_15[0];
var values_17 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_18 = values_17[0];
V v_19 = V.Nil;
v_19 = new V((RuleFunction)(args_20 => {
V v_21 = args_20[0];
V v_22 = args_20[1];
V v_23 = args_20[2];
V v_24 = args_20[3];
foreach (var row_25 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_21, (V)"entities"))))))) {
V v_26 = row_25[0];
V v_27 = row_25[1];
var values_28 = R.Join(new V[] {}, new Results(R.Get(v_27, (V)"position")));
V v_29 = values_28[0];
if (R.Truth(R.And(R.And(R.And(R.And(R.And((V)(R.Get(v_27, (V)"id") != v_22), () => R.Get(v_27, (V)"blocks_movement")), () => v_29), () => (V)(R.Get(v_29, (V)"floor_id") == R.Get(v_21, (V)"active_floor_id"))), () => (V)(R.Get(v_29, (V)"x") == v_23)), () => (V)(R.Get(v_29, (V)"y") == v_24)))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
V v_30 = V.Nil;
v_30 = new V((RuleFunction)(args_31 => {
V v_32 = args_31[0];
V v_33 = args_31[1];
V v_34 = args_31[2];
V v_35 = args_31[3];
return R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_32))).First, v_34}, new Results(v_35))).First, () => (V)(!R.Truth(R.Call(v_19, R.Join(new V[] {v_32, v_33, v_34}, new Results(v_35))).First)))));
return Results.Empty;
}));
V v_36 = V.Nil;
v_36 = new V((RuleFunction)(args_37 => {
V v_38 = args_37[0];
V v_39 = args_37[1];
V v_40 = args_37[2];
V v_41 = args_37[3];
V v_42 = args_37[4];
var values_43 = R.Join(new V[] {R.Get(v_39, (V)"position")}, new Results(R.Get(v_40, (V)"position")));
V v_44 = values_43[0];
V v_45 = values_43[1];
var values_46 = R.Join(new V[] {(V)(R.Get(v_45, (V)"x") - R.Get(v_44, (V)"x"))}, new Results((V)(R.Get(v_45, (V)"y") - R.Get(v_44, (V)"y"))));
V v_47 = values_46[0];
V v_48 = values_46[1];
if (R.Truth(R.And((V)(v_47 != (V)0.0d), () => (V)(v_48 != (V)0.0d)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_49 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_47))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_48))).First)));
V v_50 = values_49[0];
if (R.Truth(R.Or((V)(v_50 < v_41), () => (V)(v_50 > v_42)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_51 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_47 == (V)0.0d), () => (V)0.0d), () => (V)(v_47 / R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_47))).First))));
V v_52 = values_51[0];
var values_53 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_48 == (V)0.0d), () => (V)0.0d), () => (V)(v_48 / R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_48))).First))));
V v_54 = values_53[0];
var values_55 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_38))));
V v_56 = values_55[0];
var values_57 = R.Join(new V[] {}, new Results(R.Table()));
V v_58 = values_57[0];
{ double end_61 = R.Num(v_50), step_62 = R.Num((V)1.0d);
for (double i_60 = R.Num((V)1.0d); step_62 > 0 ? i_60 <= end_61 : i_60 >= end_61; i_60 += step_62) {
V v_59 = i_60;
var values_63 = R.Join(new V[] {(V)(R.Get(v_44, (V)"x") + (V)(v_52 * v_59))}, new Results((V)(R.Get(v_44, (V)"y") + (V)(v_54 * v_59))));
V v_64 = values_63[0];
V v_65 = values_63[1];
if (R.Truth(R.Or((V)(!R.Truth(R.Call(R.Get(v_2, (V)"projectile_passable"), R.Join(new V[] {v_56, v_64, v_65}, new Results(v_38))).First)), () => R.Call(R.Get(v_2, (V)"mist_blocks"), R.Join(new V[] {v_56, v_64, v_65}, new Results(v_38))).First))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_66 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_64), new Entry((V)"y", v_65))));
var obj_67 = v_58;
V key_68 = (V)((V)R.Length(v_58) + (V)1.0d);
R.Set(obj_67, key_68, values_66[0]);
}
}
return R.Join(new V[] {}, new Results(v_58));
return Results.Empty;
}));
V v_69 = V.Nil;
v_69 = new V((RuleFunction)(args_70 => {
V v_71 = args_70[0];
V v_72 = args_70[1];
V v_73 = args_70[2];
var values_74 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_71))));
V v_75 = values_74[0];
var values_76 = R.Join(new V[] {R.Get(v_72, (V)"x")}, new Results(R.Get(v_72, (V)"y")));
V v_77 = values_76[0];
V v_78 = values_76[1];
var values_79 = R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_73, (V)"x") - v_77)))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_73, (V)"y") - v_78)))));
V v_80 = values_79[0];
V v_81 = values_79[1];
var values_82 = R.Join(new V[] {R.Or(R.And((V)(R.Get(v_73, (V)"x") > v_77), () => (V)1.0d), () => (-(V)1.0d))}, new Results(R.Or(R.And((V)(R.Get(v_73, (V)"y") > v_78), () => (V)1.0d), () => (-(V)1.0d))));
V v_83 = values_82[0];
V v_84 = values_82[1];
var values_85 = R.Join(new V[] {}, new Results((V)(v_80 - v_81)));
V v_86 = values_85[0];
while (R.Truth(R.Or((V)(v_77 != R.Get(v_73, (V)"x")), () => (V)(v_78 != R.Get(v_73, (V)"y"))))) {
var values_87 = R.Join(new V[] {v_77}, new Results(v_78));
V v_88 = values_87[0];
V v_89 = values_87[1];
var values_90 = R.Join(new V[] {}, new Results((V)((V)2.0d * v_86)));
V v_91 = values_90[0];
if (R.Truth((V)(v_91 > (-v_81)))) {
var values_92 = R.Join(new V[] {}, new Results((V)(v_86 - v_81)));
v_86 = values_92[0];

var values_93 = R.Join(new V[] {}, new Results((V)(v_77 + v_83)));
v_77 = values_93[0];
}
if (R.Truth((V)(v_91 < v_80))) {
var values_94 = R.Join(new V[] {}, new Results((V)(v_86 + v_80)));
v_86 = values_94[0];

var values_95 = R.Join(new V[] {}, new Results((V)(v_78 + v_84)));
v_78 = values_95[0];
}
if (R.Truth(R.And(R.And((V)(v_77 != v_88), () => (V)(v_78 != v_89)), () => R.Or((V)(!R.Truth(R.Call(R.Get(v_2, (V)"projectile_passable"), R.Join(new V[] {v_75, v_77, v_89}, new Results(v_71))).First)), () => (V)(!R.Truth(R.Call(R.Get(v_2, (V)"projectile_passable"), R.Join(new V[] {v_75, v_88, v_78}, new Results(v_71))).First)))))) {
return R.Join(new V[] {}, new Results((V)false));
}
if (R.Truth(R.Or((V)(!R.Truth(R.Call(R.Get(v_2, (V)"projectile_passable"), R.Join(new V[] {v_75, v_77, v_78}, new Results(v_71))).First)), () => R.Call(R.Get(v_2, (V)"mist_blocks"), R.Join(new V[] {v_75, v_77, v_78}, new Results(v_71))).First))) {
return R.Join(new V[] {}, new Results((V)false));
}
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}));
V v_96 = V.Nil;
v_96 = new V((RuleFunction)(args_97 => {
V v_98 = args_97[0];
V v_99 = args_97[1];
V v_100 = args_97[2];
return R.Join(new V[] {}, new Results((V)((V)((V)(v_100 - (V)1.0d) * R.Get(v_98, (V)"width")) + v_99)));
return Results.Empty;
}));
V v_101 = V.Nil;
v_101 = new V((RuleFunction)(args_102 => {
V v_103 = args_102[0];
V v_104 = args_102[1];
V v_105 = args_102[2];
var values_106 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_103))));
V v_107 = values_106[0];
var values_108 = R.Join(new V[] {}, new Results(R.Get(v_104, (V)"position")));
V v_109 = values_108[0];
var values_110 = R.Join(new V[] {}, new Results(R.Table()));
V v_111 = values_110[0];
foreach (var row_112 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_103, (V)"entities"))))))) {
V v_113 = row_112[0];
V v_114 = row_112[1];
var values_115 = R.Join(new V[] {}, new Results(R.Get(v_114, (V)"position")));
V v_116 = values_115[0];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_114, (V)"id") != R.Get(v_104, (V)"id")), () => R.Get(v_114, (V)"blocks_movement")), () => v_116), () => (V)(R.Get(v_116, (V)"floor_id") == R.Get(v_103, (V)"active_floor_id"))))) {
var values_117 = R.Join(new V[] {}, new Results((V)true));
var obj_118 = v_111;
V key_119 = R.Call(v_96, R.Join(new V[] {v_107, R.Get(v_116, (V)"x")}, new Results(R.Get(v_116, (V)"y")))).First;
R.Set(obj_118, key_119, values_117[0]);
}
}
var values_120 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_109, (V)"x")), new Entry((V)"y", R.Get(v_109, (V)"y")))))));
V v_121 = values_120[0];
var values_122 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Call(v_96, R.Join(new V[] {v_107, R.Get(v_109, (V)"x")}, new Results(R.Get(v_109, (V)"y")))).First, (V)true))));
V v_123 = values_122[0];
var values_124 = R.Join(new V[] {}, new Results((V)1.0d));
V v_125 = values_124[0];
while (R.Truth((V)(v_125 <= (V)R.Length(v_121)))) {
var values_126 = R.Join(new V[] {}, new Results(R.Get(v_121, v_125)));
V v_127 = values_126[0];
var values_128 = R.Join(new V[] {}, new Results((V)(v_125 + (V)1.0d)));
v_125 = values_128[0];
foreach (var row_129 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_18)))))) {
V v_130 = row_129[0];
V v_131 = row_129[1];
var values_132 = R.Join(new V[] {(V)(R.Get(v_127, (V)"x") + R.Get(v_131, (V)1.0d))}, new Results((V)(R.Get(v_127, (V)"y") + R.Get(v_131, (V)2.0d))));
V v_133 = values_132[0];
V v_134 = values_132[1];
var values_135 = R.Join(new V[] {}, R.Call(v_96, R.Join(new V[] {v_107, v_133}, new Results(v_134))));
V v_136 = values_135[0];
if (R.Truth(R.And(R.And((V)(!R.Truth(R.Get(v_123, v_136))), () => R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_107, v_133}, new Results(v_134))).First), () => (V)(!R.Truth(R.Get(v_111, v_136)))))) {
var values_137 = R.Join(new V[] {}, new Results((V)true));
var obj_138 = v_123;
V key_139 = v_136;
R.Set(obj_138, key_139, values_137[0]);
var values_140 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_127, (V)"first_dx"), () => R.Get(v_131, (V)1.0d))));
V v_141 = values_140[0];
var values_142 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_127, (V)"first_dy"), () => R.Get(v_131, (V)2.0d))));
V v_143 = values_142[0];
if (R.Truth(R.Get(v_105, v_136))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_141), new Entry((V)"dy", v_143))));
}
var values_144 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_133), new Entry((V)"y", v_134), new Entry((V)"first_dx", v_141), new Entry((V)"first_dy", v_143))));
var obj_145 = v_121;
V key_146 = (V)((V)R.Length(v_121) + (V)1.0d);
R.Set(obj_145, key_146, values_144[0]);
}
}
}
return R.Join(new V[] {}, new Results(V.Nil));
return Results.Empty;
}));
V v_147 = V.Nil;
v_147 = new V((RuleFunction)(args_148 => {
V v_149 = args_148[0];
V v_150 = args_148[1];
V v_151 = args_148[2];
V v_152 = args_148[3];
var values_153 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_149))));
V v_154 = values_153[0];
var values_155 = R.Join(new V[] {}, new Results(R.Table()));
V v_156 = values_155[0];
var values_157 = R.Join(new V[] {}, new Results(R.Or(R.And(v_152, () => R.Table(new Entry((V)1, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d))))), () => v_18)));
V v_158 = values_157[0];
foreach (var row_159 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_158)))))) {
V v_160 = row_159[0];
V v_161 = row_159[1];
var values_162 = R.Join(new V[] {(V)(R.Get(R.Get(v_151, (V)"position"), (V)"x") + R.Get(v_161, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_151, (V)"position"), (V)"y") + R.Get(v_161, (V)2.0d))));
V v_163 = values_162[0];
V v_164 = values_162[1];
if (R.Truth(R.Call(v_30, R.Join(new V[] {v_149, R.Get(v_150, (V)"id"), v_163}, new Results(v_164))).First)) {
var values_165 = R.Join(new V[] {}, new Results((V)true));
var obj_166 = v_156;
V key_167 = R.Call(v_96, R.Join(new V[] {v_154, v_163}, new Results(v_164))).First;
R.Set(obj_166, key_167, values_165[0]);
}
}
return R.Join(new V[] {}, new Results(v_156));
return Results.Empty;
}));
V v_168 = V.Nil;
v_168 = new V((RuleFunction)(args_169 => {
V v_170 = args_169[0];
V v_171 = args_169[1];
V v_172 = args_169[2];
V v_173 = args_169[3];
var values_174 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_170))));
V v_175 = values_174[0];
var values_176 = R.Join(new V[] {}, new Results(R.Table()));
V v_177 = values_176[0];
foreach (var row_178 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_18)))))) {
V v_179 = row_178[0];
V v_180 = row_178[1];
{ double end_183 = R.Num(R.Get(v_173, (V)"range")), step_184 = R.Num((V)1.0d);
for (double i_182 = R.Num(R.Get(v_173, (V)"min_range")); step_184 > 0 ? i_182 <= end_183 : i_182 >= end_183; i_182 += step_184) {
V v_181 = i_182;
var values_185 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_172, (V)"position"), (V)"x") + (V)(R.Get(v_180, (V)1.0d) * v_181))));
V v_186 = values_185[0];
var values_187 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_172, (V)"position"), (V)"y") + (V)(R.Get(v_180, (V)2.0d) * v_181))));
V v_188 = values_187[0];
if (R.Truth(R.Call(v_30, R.Join(new V[] {v_170, R.Get(v_171, (V)"id"), v_186}, new Results(v_188))).First)) {
var values_189 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"position", R.Table(new Entry((V)"x", v_186), new Entry((V)"y", v_188))))));
V v_190 = values_189[0];
if (R.Truth(R.Call(v_36, R.Join(new V[] {v_170, v_190, v_172, R.Get(v_173, (V)"min_range")}, new Results(R.Get(v_173, (V)"range")))).First)) {
var values_191 = R.Join(new V[] {}, new Results((V)true));
var obj_192 = v_177;
V key_193 = R.Call(v_96, R.Join(new V[] {v_175, v_186}, new Results(v_188))).First;
R.Set(obj_192, key_193, values_191[0]);
}
}
}
}
}
return R.Join(new V[] {}, new Results(v_177));
return Results.Empty;
}));
R.Set(v_16, (V)"choose", new V((RuleFunction)(args_194 => {
V v_195 = args_194[0];
V v_196 = args_194[1];
var values_197 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_195, (V)"entities"), (V)1.0d)));
V v_198 = values_197[0];
foreach (var row_199 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_195, (V)"entities"))))))) {
V v_200 = row_199[0];
V v_201 = row_199[1];
if (R.Truth((V)(R.Get(v_201, (V)"id") == R.Get(v_195, (V)"player_id")))) {
var values_202 = R.Join(new V[] {}, new Results(v_201));
v_198 = values_202[0];

break;
}
}
var values_203 = R.Join(new V[] {}, new Results(R.And(R.Get(v_195, (V)"class_special"), () => R.Get(R.Get(v_195, (V)"class_special"), (V)"decoy"))));
V v_204 = values_203[0];
if (R.Truth(R.And(v_204, () => (V)(R.Get(v_204, (V)"floor_id") == R.Get(R.Get(v_196, (V)"position"), (V)"floor_id"))))) {
var values_205 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_198, (V)"id")), new Entry((V)"faction", R.Get(v_198, (V)"faction")), new Entry((V)"position", v_204), new Entry((V)"hp", R.Get(v_198, (V)"hp")), new Entry((V)"max_hp", R.Get(v_198, (V)"max_hp")))));
v_198 = values_205[0];
}
var values_206 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_2, (V)"get_enemy"), R.Join(new V[] {}, new Results(R.Get(v_196, (V)"definition_id")))).First}, new Results((V)"unknown AI content"))));
V v_207 = values_206[0];
var values_208 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_198, (V)"position"), (V)"x") - R.Get(R.Get(v_196, (V)"position"), (V)"x"))));
V v_209 = values_208[0];
var values_210 = R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_198, (V)"position"), (V)"y") - R.Get(R.Get(v_196, (V)"position"), (V)"y"))));
V v_211 = values_210[0];
var values_212 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_209))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_211))).First)));
V v_213 = values_212[0];
if (R.Truth((V)(R.Get(v_207, (V)"ai") == (V)"biome_elite"))) {
return R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"choose"), R.Join(new V[] {v_195, v_196, v_198}, new Results(R.Table(new Entry((V)"definition", v_207), new Entry((V)"map", R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_195))).First), new Entry((V)"projectile_passable", new V((RuleFunction)(args_214 => {
V v_215 = args_214[0];
V v_216 = args_214[1];
V v_217 = args_214[2];
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"projectile_passable"), R.Join(new V[] {v_215, v_216, v_217}, new Results(v_195))));
return Results.Empty;
}))), new Entry((V)"clear_line", v_69), new Entry((V)"next_step", v_101), new Entry((V)"adjacent_goals", v_147))))));
}
if (R.Truth(R.Or(R.Or((V)(R.Get(v_207, (V)"ai") == (V)"expansion_regular"), () => (V)(R.Get(v_207, (V)"ai") == (V)"squad")), () => (V)(R.Get(v_207, (V)"ai") == (V)"idea_regular")))) {
var values_218 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(R.Get(v_207, (V)"ai") == (V)"idea_regular"), () => v_12), () => R.And((V)(R.Get(v_207, (V)"ai") == (V)"squad"), () => v_10)), () => v_8)));
V v_219 = values_218[0];
return R.Join(new V[] {}, R.Call(R.Get(v_219, (V)"choose"), R.Join(new V[] {v_195, v_196, v_198}, new Results(R.Table(new Entry((V)"definition", v_207), new Entry((V)"map", R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_195))).First), new Entry((V)"projectile_passable", new V((RuleFunction)(args_220 => {
V v_221 = args_220[0];
V v_222 = args_220[1];
V v_223 = args_220[2];
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"projectile_passable"), R.Join(new V[] {v_221, v_222, v_223}, new Results(v_195))));
return Results.Empty;
}))), new Entry((V)"clear_line", v_69), new Entry((V)"next_step", v_101), new Entry((V)"adjacent_goals", v_147), new Entry((V)"free", v_30), new Entry((V)"visible", new V((RuleFunction)(args_224 => {
V v_225 = args_224[0];
V v_226 = args_224[1];
return R.Join(new V[] {}, R.Call(R.Get(v_14, (V)"can_see"), R.Join(new V[] {v_195, R.Get(v_195, (V)"active_floor_id"), v_225}, new Results(v_226))));
return Results.Empty;
}))))))));
}
if (R.Truth((V)(R.Get(v_207, (V)"ai") == (V)"hollow_regent"))) {
var values_227 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_196, (V)"ai_memory"), (V)"attacks"), () => (V)0.0d)));
V v_228 = values_227[0];
var values_229 = R.Join(new V[] {}, new Results((V)(v_228 % (V)3.0d)));
V v_230 = values_229[0];
var values_231 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {v_195, v_196, v_198, (V)1.0d}, new Results((V)5.0d))));
V v_232 = values_231[0];
if (R.Truth(R.And((V)(v_230 == (V)0.0d), () => v_232))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/regent_line"), new Entry((V)"cells", v_232), new Entry((V)"ranged", (V)"piercing_line"))));
}
if (R.Truth(R.And((V)(v_230 == (V)1.0d), () => (V)(v_213 <= (V)2.0d)))) {
var values_233 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_234 = values_233[0];
V v_235 = values_233[1];
if (R.Truth((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_209))).First >= R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_211))).First))) {
var values_236 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_209 >= (V)0.0d), () => (V)1.0d), () => (-(V)1.0d))));
v_234 = values_236[0];
} else {
var values_237 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_211 >= (V)0.0d), () => (V)1.0d), () => (-(V)1.0d))));
v_235 = values_237[0];
}
var values_238 = R.Join(new V[] {(-v_235)}, new Results(v_234));
V v_239 = values_238[0];
V v_240 = values_238[1];
var values_241 = R.Join(new V[] {}, new Results(R.Table()));
V v_242 = values_241[0];
var values_243 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_195))));
V v_244 = values_243[0];
{ double end_247 = R.Num((V)2.0d), step_248 = R.Num((V)1.0d);
for (double i_246 = R.Num((V)1.0d); step_248 > 0 ? i_246 <= end_247 : i_246 >= end_247; i_246 += step_248) {
V v_245 = i_246;
{ double end_251 = R.Num((V)1.0d), step_252 = R.Num((V)1.0d);
for (double i_250 = R.Num((-(V)1.0d)); step_252 > 0 ? i_250 <= end_251 : i_250 >= end_251; i_250 += step_252) {
V v_249 = i_250;
var values_253 = R.Join(new V[] {}, new Results((V)((V)(R.Get(R.Get(v_196, (V)"position"), (V)"x") + (V)(v_234 * v_245)) + (V)(v_239 * v_249))));
V v_254 = values_253[0];
var values_255 = R.Join(new V[] {}, new Results((V)((V)(R.Get(R.Get(v_196, (V)"position"), (V)"y") + (V)(v_235 * v_245)) + (V)(v_240 * v_249))));
V v_256 = values_255[0];
if (R.Truth(R.Call(R.Get(v_2, (V)"projectile_passable"), R.Join(new V[] {v_244, v_254, v_256}, new Results(v_195))).First)) {
var values_257 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_254), new Entry((V)"y", v_256))));
var obj_258 = v_242;
V key_259 = (V)((V)R.Length(v_242) + (V)1.0d);
R.Set(obj_258, key_259, values_257[0]);
}
}
}
}
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/regent_crown"), new Entry((V)"cells", v_242), new Entry((V)"ranged", (V)"area"))));
}
if (R.Truth(R.And((V)(v_213 <= R.Get(v_207, (V)"range")), () => R.Call(v_69, R.Join(new V[] {v_195, R.Get(v_196, (V)"position")}, new Results(R.Get(v_198, (V)"position")))).First))) {
var values_260 = R.Join(new V[] {}, new Results(R.Table()));
V v_261 = values_260[0];
var values_262 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_195))));
V v_263 = values_262[0];
{ double end_266 = R.Num((V)1.0d), step_267 = R.Num((V)1.0d);
for (double i_265 = R.Num((-(V)1.0d)); step_267 > 0 ? i_265 <= end_266 : i_265 >= end_266; i_265 += step_267) {
V v_264 = i_265;
{ double end_270 = R.Num((V)1.0d), step_271 = R.Num((V)1.0d);
for (double i_269 = R.Num((-(V)1.0d)); step_271 > 0 ? i_269 <= end_270 : i_269 >= end_270; i_269 += step_271) {
V v_268 = i_269;
var values_272 = R.Join(new V[] {(V)(R.Get(R.Get(v_198, (V)"position"), (V)"x") + v_268)}, new Results((V)(R.Get(R.Get(v_198, (V)"position"), (V)"y") + v_264)));
V v_273 = values_272[0];
V v_274 = values_272[1];
if (R.Truth(R.Call(R.Get(v_2, (V)"projectile_passable"), R.Join(new V[] {v_263, v_273, v_274}, new Results(v_195))).First)) {
var values_275 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_273), new Entry((V)"y", v_274))));
var obj_276 = v_261;
V key_277 = (V)((V)R.Length(v_261) + (V)1.0d);
R.Set(obj_276, key_277, values_275[0]);
}
}
}
}
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/regent_sigil"), new Entry((V)"cells", v_261), new Entry((V)"ranged", (V)"area"))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(v_101, R.Join(new V[] {v_195, v_196}, R.Call(v_147, R.Join(new V[] {v_195, v_196, v_198}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
if (R.Truth((V)(R.Get(v_207, (V)"ai") == (V)"pursuer"))) {
if (R.Truth((V)(v_213 == (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/pursuer_strike"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(R.Get(v_198, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_198, (V)"position"), (V)"y")))))), new Entry((V)"ranged", (V)false))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(v_101, R.Join(new V[] {v_195, v_196}, R.Call(v_147, R.Join(new V[] {v_195, v_196, v_198}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
if (R.Truth((V)(R.Get(v_207, (V)"ai") == (V)"warden"))) {
var values_278 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {v_195, v_196, v_198, (V)1.0d}, new Results(R.Get(v_207, (V)"range")))));
V v_279 = values_278[0];
var values_280 = R.Join(new V[] {}, new Results((V)(v_213 == (V)1.0d)));
V v_281 = values_280[0];
var values_282 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_196, (V)"ai_memory"), (V)"attacks"), () => (V)0.0d)));
V v_283 = values_282[0];
if (R.Truth(R.And(v_279, () => R.Or((V)(!R.Truth(v_281)), () => (V)((V)(v_283 % (V)2.0d) == (V)1.0d))))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/warden_roots"), new Entry((V)"cells", v_279), new Entry((V)"ranged", (V)true))));
}
if (R.Truth(v_281)) {
var values_284 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(R.Get(v_198, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_198, (V)"position"), (V)"y")))))));
V v_285 = values_284[0];
if (R.Truth((V)(v_209 != (V)0.0d))) {
var values_286 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(R.Get(v_198, (V)"position"), (V)"x")), new Entry((V)"y", (V)(R.Get(R.Get(v_198, (V)"position"), (V)"y") - (V)1.0d)))));
var obj_287 = v_285;
V key_288 = (V)((V)R.Length(v_285) + (V)1.0d);
R.Set(obj_287, key_288, values_286[0]);
var values_289 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(R.Get(v_198, (V)"position"), (V)"x")), new Entry((V)"y", (V)(R.Get(R.Get(v_198, (V)"position"), (V)"y") + (V)1.0d)))));
var obj_290 = v_285;
V key_291 = (V)((V)R.Length(v_285) + (V)1.0d);
R.Set(obj_290, key_291, values_289[0]);
} else {
var values_292 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)(R.Get(R.Get(v_198, (V)"position"), (V)"x") - (V)1.0d)), new Entry((V)"y", R.Get(R.Get(v_198, (V)"position"), (V)"y")))));
var obj_293 = v_285;
V key_294 = (V)((V)R.Length(v_285) + (V)1.0d);
R.Set(obj_293, key_294, values_292[0]);
var values_295 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)(R.Get(R.Get(v_198, (V)"position"), (V)"x") + (V)1.0d)), new Entry((V)"y", R.Get(R.Get(v_198, (V)"position"), (V)"y")))));
var obj_296 = v_285;
V key_297 = (V)((V)R.Length(v_285) + (V)1.0d);
R.Set(obj_296, key_297, values_295[0]);
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/warden_sweep"), new Entry((V)"cells", v_285), new Entry((V)"ranged", (V)false))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(v_101, R.Join(new V[] {v_195, v_196}, R.Call(v_147, R.Join(new V[] {v_195, v_196, v_198}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
if (R.Truth((V)(R.Get(v_207, (V)"ai") == (V)"guard"))) {
if (R.Truth((V)(v_213 == (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/guard_strike"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(R.Get(v_198, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_198, (V)"position"), (V)"y")))))), new Entry((V)"ranged", (V)false))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(v_101, R.Join(new V[] {v_195, v_196}, R.Call(v_147, R.Join(new V[] {v_195, v_196, v_198}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
if (R.Truth((V)(R.Get(v_207, (V)"ai") == (V)"spore_herald"))) {
if (R.Truth((V)(v_213 <= (V)1.0d))) {
foreach (var row_298 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_18)))))) {
V v_299 = row_298[0];
V v_300 = row_298[1];
var values_301 = R.Join(new V[] {(V)(R.Get(R.Get(v_196, (V)"position"), (V)"x") + R.Get(v_300, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_196, (V)"position"), (V)"y") + R.Get(v_300, (V)2.0d))));
V v_302 = values_301[0];
V v_303 = values_301[1];
if (R.Truth(R.And(R.And(R.Call(v_30, R.Join(new V[] {v_195, R.Get(v_196, (V)"id"), v_302}, new Results(v_303))).First, () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_198, (V)"position"), (V)"x") - v_302)))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_198, (V)"position"), (V)"y") - v_303)))).First) > v_213)), () => R.Call(v_69, R.Join(new V[] {v_195, R.Table(new Entry((V)"x", v_302), new Entry((V)"y", v_303))}, new Results(R.Get(v_198, (V)"position")))).First))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_300, (V)1.0d)), new Entry((V)"dy", R.Get(v_300, (V)2.0d)))));
}
}
}
if (R.Truth(R.And((V)(v_213 <= R.Get(v_207, (V)"range")), () => R.Call(v_69, R.Join(new V[] {v_195, R.Get(v_196, (V)"position")}, new Results(R.Get(v_198, (V)"position")))).First))) {
var values_304 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(R.Get(v_198, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_198, (V)"position"), (V)"y")))))));
V v_305 = values_304[0];
var values_306 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_209))).First >= R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_211))).First), () => R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))))), () => R.Table(new Entry((V)1, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d)))))));
V v_307 = values_306[0];
var values_308 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_195))));
V v_309 = values_308[0];
foreach (var row_310 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_307)))))) {
V v_311 = row_310[0];
V v_312 = row_310[1];
var values_313 = R.Join(new V[] {(V)(R.Get(R.Get(v_198, (V)"position"), (V)"x") + R.Get(v_312, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_198, (V)"position"), (V)"y") + R.Get(v_312, (V)2.0d))));
V v_314 = values_313[0];
V v_315 = values_313[1];
if (R.Truth(R.Call(R.Get(v_2, (V)"projectile_passable"), R.Join(new V[] {v_309, v_314, v_315}, new Results(v_195))).First)) {
var values_316 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_314), new Entry((V)"y", v_315))));
var obj_317 = v_305;
V key_318 = (V)((V)R.Length(v_305) + (V)1.0d);
R.Set(obj_317, key_318, values_316[0]);
}
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/spore_burst"), new Entry((V)"cells", v_305), new Entry((V)"ranged", (V)"area"))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(v_101, R.Join(new V[] {v_195, v_196}, R.Call(v_168, R.Join(new V[] {v_195, v_196, v_198}, new Results(R.Table(new Entry((V)"min_range", (V)2.0d), new Entry((V)"range", R.Get(v_207, (V)"range")))))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
if (R.Truth((V)(R.Get(v_207, (V)"ai") == (V)"canal_lancer"))) {
var values_319 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {v_195, v_196, v_198, (V)1.0d}, new Results((V)2.0d))));
V v_320 = values_319[0];
if (R.Truth(v_320)) {
var values_321 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_209 == (V)0.0d), () => (V)0.0d), () => (V)(v_209 / R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_209))).First))));
V v_322 = values_321[0];
var values_323 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_211 == (V)0.0d), () => (V)0.0d), () => (V)(v_211 / R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_211))).First))));
V v_324 = values_323[0];
var values_325 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_195))));
V v_326 = values_325[0];
var values_327 = R.Join(new V[] {(V)(R.Get(R.Get(v_196, (V)"position"), (V)"x") + (V)((V)2.0d * v_322))}, new Results((V)(R.Get(R.Get(v_196, (V)"position"), (V)"y") + (V)((V)2.0d * v_324))));
V v_328 = values_327[0];
V v_329 = values_327[1];
if (R.Truth(R.And((V)((V)R.Length(v_320) == (V)1.0d), () => R.Call(R.Get(v_2, (V)"projectile_passable"), R.Join(new V[] {v_326, v_328, v_329}, new Results(v_195))).First))) {
var values_330 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_328), new Entry((V)"y", v_329))));
var obj_331 = v_320;
V key_332 = (V)((V)R.Length(v_320) + (V)1.0d);
R.Set(obj_331, key_332, values_330[0]);
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/canal_thrust"), new Entry((V)"cells", v_320), new Entry((V)"ranged", (V)"piercing_line"))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(v_101, R.Join(new V[] {v_195, v_196}, R.Call(v_168, R.Join(new V[] {v_195, v_196, v_198}, new Results(R.Table(new Entry((V)"min_range", (V)1.0d), new Entry((V)"range", (V)2.0d))))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
if (R.Truth((V)(R.Get(v_207, (V)"ai") == (V)"cinder_bellkeeper"))) {
var values_333 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {v_195, v_196, v_198, (V)1.0d}, new Results(R.Get(v_207, (V)"range")))));
V v_334 = values_333[0];
if (R.Truth(v_334)) {
var values_335 = R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"map_for"), R.Join(new V[] {}, new Results(v_195))));
V v_336 = values_335[0];
var values_337 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(R.Get(v_198, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_198, (V)"position"), (V)"y")))))));
V v_338 = values_337[0];
foreach (var row_339 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_18)))))) {
V v_340 = row_339[0];
V v_341 = row_339[1];
var values_342 = R.Join(new V[] {(V)(R.Get(R.Get(v_198, (V)"position"), (V)"x") + R.Get(v_341, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_198, (V)"position"), (V)"y") + R.Get(v_341, (V)2.0d))));
V v_343 = values_342[0];
V v_344 = values_342[1];
if (R.Truth(R.Call(R.Get(v_2, (V)"projectile_passable"), R.Join(new V[] {v_336, v_343, v_344}, new Results(v_195))).First)) {
var values_345 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_343), new Entry((V)"y", v_344))));
var obj_346 = v_338;
V key_347 = (V)((V)R.Length(v_338) + (V)1.0d);
R.Set(obj_346, key_347, values_345[0]);
}
}
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/bell_cross"), new Entry((V)"cells", v_338), new Entry((V)"ranged", (V)"area"))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(v_101, R.Join(new V[] {v_195, v_196}, R.Call(v_168, R.Join(new V[] {v_195, v_196, v_198}, new Results(R.Table(new Entry((V)"min_range", (V)2.0d), new Entry((V)"range", R.Get(v_207, (V)"range")))))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
if (R.Truth((V)(R.Get(v_207, (V)"ai") == (V)"spitter"))) {
var values_348 = R.Join(new V[] {}, R.Call(v_36, R.Join(new V[] {v_195, v_196, v_198, R.Get(v_207, (V)"min_range")}, new Results(R.Get(v_207, (V)"range")))));
V v_349 = values_348[0];
if (R.Truth(v_349)) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/thorn_lane"), new Entry((V)"cells", v_349), new Entry((V)"ranged", (V)true))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(v_101, R.Join(new V[] {v_195, v_196}, R.Call(v_168, R.Join(new V[] {v_195, v_196, v_198}, new Results(v_207))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
if (R.Truth((V)(R.Get(v_207, (V)"ai") == (V)"skitter"))) {
if (R.Truth(R.And((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_209))).First == (V)1.0d), () => (V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_211))).First == (V)1.0d)))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/skitter_stab"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(R.Get(v_198, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_198, (V)"position"), (V)"y")))))), new Entry((V)"ranged", (V)false))));
}
var values_350 = R.Join(new V[] {}, R.Call(v_101, R.Join(new V[] {v_195, v_196}, R.Call(v_147, R.Join(new V[] {v_195, v_196, v_198}, new Results((V)true))))));
V v_351 = values_350[0];
if (R.Truth(v_351)) {
return R.Join(new V[] {}, new Results(v_351));
}
if (R.Truth((V)(R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_209))).First}, R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_211))))).First == (V)1.0d))) {
return R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", (V)"windup"), new Entry((V)"ability_id", (V)"core:ability/skitter_stab"), new Entry((V)"cells", R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(R.Get(v_198, (V)"position"), (V)"x")), new Entry((V)"y", R.Get(R.Get(v_198, (V)"position"), (V)"y")))))), new Entry((V)"ranged", (V)false))));
}
return R.Join(new V[] {}, new Results(R.Or(R.Call(v_101, R.Join(new V[] {v_195, v_196}, R.Call(v_147, R.Join(new V[] {v_195, v_196, v_198}, new Results((V)false))))).First, () => R.Table(new Entry((V)"kind", (V)"wait")))));
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results((V)"unknown AI behavior")));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_16));
return Results.Empty;
} }
